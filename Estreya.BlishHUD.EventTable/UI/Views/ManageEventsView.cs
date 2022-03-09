namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Resources;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ManageEventsView : View
    {
        private static Point MAIN_PADDING = new Point(20, 20);

        private static readonly Logger Logger = Logger.GetLogger<ManageEventsView>();

        public Panel Panel { get; private set; }

        private IEnumerable<EventCategory> EventCategories { get; set; }
        private List<SettingEntry<bool>> EventSettings { get; set; }

        public ManageEventsView(IEnumerable<EventCategory> categories, List<SettingEntry<bool>> settings)
        {
            this.EventCategories = categories;
            this.EventSettings = settings;
        }

        private void UpdateToggleButton(GlowButton button)
        {
            button.Icon = button.Checked ? EventTableModule.ModuleInstance.ContentsManager.GetIcon("images\\minus.png") : EventTableModule.ModuleInstance.ContentsManager.GetIcon("images\\plus.png"); // TODO: Own icon
        }

        protected override void Build(Container buildPanel)
        {
            this.Panel = new Panel();
            Panel.Parent = buildPanel;
            Panel.Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y);
            Panel.Width = buildPanel.ContentRegion.Width - MAIN_PADDING.Y * 2;
            Panel.Height = buildPanel.ContentRegion.Height - MAIN_PADDING.X;
            Panel.CanScroll = true;

            Rectangle contentRegion = Panel.ContentRegion;

            TextBox searchBox = new TextBox()
            {
                Parent = Panel,
                Width = Panel.MenuStandard.Size.X,
                Location = new Point(0, contentRegion.Y),
                PlaceholderText = Strings.ManageEventsView_SearchBox_Placeholder
            };

            Panel eventCategoriesPanel = new Panel
            {
                Title = Strings.ManageEventsView_EventCategories_Title,
                Parent = Panel,
                CanScroll = true,
                ShowBorder = true,
                Location = new Point(0, searchBox.Bottom + Panel.MenuStandard.ControlOffset.Y)
            };

            eventCategoriesPanel.Size = new Point(Panel.MenuStandard.Size.X, contentRegion.Height - eventCategoriesPanel.Location.Y);

            Menu eventCategories = new Menu
            {
                Parent = eventCategoriesPanel,
                Size = eventCategoriesPanel.ContentRegion.Size,
                MenuItemHeight = 40
            };

            FlowPanel eventPanel = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.LeftToRight,
                CanScroll = true,
                ShowBorder = true,
                Parent = Panel,
                Location = new Point(eventCategoriesPanel.Right + Panel.ControlStandard.ControlOffset.X, contentRegion.Y)
            };

            eventPanel.Size = new Point(contentRegion.Width - eventPanel.Left, contentRegion.Height  - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

            searchBox.TextChanged += (s, e) =>
            {
                eventPanel.FilterChildren<EventDetailsButton>(detailsButton =>
                {
                    return detailsButton.Text.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
                });
            };

            #region Register Categories

            Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

            MenuItem allEvents = eventCategories.AddMenuItem(Strings.ManageEventsView_AllEvents);
            allEvents.Select();
            menus.Add(nameof(allEvents), allEvents);

            foreach (EventCategory category in EventCategories.GroupBy(ec => ec.Key).Select(ec => ec.First()))
            {
                menus.Add(category.Key, eventCategories.AddMenuItem(category.Name));
            }

            menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
            {
                MenuItem menuItem = s as MenuItem;

                var category = EventCategories.Where(ec => ec.Name == menuItem.Text).FirstOrDefault();

                eventPanel.FilterChildren<EventDetailsButton>(detailsButton =>
                {
                    if (menuItem == menus[nameof(allEvents)]) return true;

                    //IEnumerable<EventCategory> categories = EventCategories.Where(ec => ec.Events.Any(ev => ev.Name == detailsButton.Text));
                    return category.Events.Any(ev => ev.EventCategory.Key == detailsButton.Event.EventCategory.Key && ev.Key == detailsButton.Event.Key);
                });
            });

            #endregion

            Panel buttons = new Panel()
            {
                Parent = Panel,
                Location = new Point(eventPanel.Left, eventPanel.Bottom),
                Size = new Point(eventPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT),
            };

            StandardButton checkAllButton = new StandardButton()
            {
                Text = Strings.ManageEventsView_CheckAll,
                Parent = buttons,
                Right = buttons.Width,
                Bottom = buttons.Height
            };
            checkAllButton.Click += (s, e) =>
            {
                eventPanel.Children.ToList().ForEach(control =>
                {
                    if (menus[nameof(allEvents)].Selected)
                    {
                        // Check Yes - No
                    }

                    EventDetailsButton detailsButton = control as EventDetailsButton;

                    if (detailsButton.Visible)
                    {
                        GlowButton glowButton = detailsButton.Children.Last() as GlowButton;
                        glowButton.Checked = true;
                    }
                });
            };

            StandardButton uncheckAllButton = new StandardButton()
            {
                Text = Strings.ManageEventsView_UncheckAll,
                Parent = buttons,
                Right = checkAllButton.Left,
                Bottom = buttons.Height
            };
            uncheckAllButton.Click += (s, e) =>
            {
                eventPanel.Children.ToList().ForEach(control =>
                {
                    if (menus[nameof(allEvents)].Selected)
                    {
                        // Check Yes - No
                    }

                    EventDetailsButton detailsButton = control as EventDetailsButton;

                    if (detailsButton.Visible)
                    {
                        GlowButton glowButton = detailsButton.Children.Last() as GlowButton;
                        glowButton.Checked = false;
                    }
                });
            };

            foreach (EventCategory category in EventCategories)
            {
                IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Key).Select(eg => eg.First()) : category.Events;
                foreach (Event e in events)
                {
                    if (e.Filler) continue;

                    // Check with .ToLower() because settings define is case insensitive
                    IEnumerable<SettingEntry<bool>> settings = this.EventSettings.FindAll(eventSetting => eventSetting.EntryKey.ToLowerInvariant() == e.SettingKey.ToLowerInvariant());

                    SettingEntry<bool> setting = settings.First();
                    bool enabled = setting.Value;

                    AsyncTexture2D icon = EventTableModule.ModuleInstance.ContentsManager.GetIcon(e.Icon);

                    var button = new EventDetailsButton()
                    {
                        Event = e,
                        Parent = eventPanel,
                        Text = e.Name,
                        Icon = icon,
                        ShowToggleButton = true,
                        FillColor = Color.LightBlue,
                        //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                    };

                    if (!string.IsNullOrWhiteSpace(e.Waypoint))
                    {
                        var waypointButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false,
                            Tooltip = new Tooltip(new TooltipView(Strings.ManageEventsView_Waypoint_Title, Strings.ManageEventsView_Waypoint_Description, icon: "images\\waypoint.png")),
                            Icon = EventTableModule.ModuleInstance.ContentsManager.GetIcon("images\\waypoint.png") // TODO: Own icon
                        };

                        waypointButton.Click += (s, eventArgs) =>
                        {
                            e.CopyWaypoint();
                        };
                    }

                    if (!string.IsNullOrWhiteSpace(e.Wiki))
                    {
                        var wikiButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false,
                            Tooltip = new Tooltip(new TooltipView(Strings.ManageEventsView_Wiki_Title, Strings.ManageEventsView_Wiki_Description, icon: "images\\wiki.png")),
                            Icon = EventTableModule.ModuleInstance.ContentsManager.GetIcon("images\\wiki.png") // TODO: Own icon
                        };

                        wikiButton.Click += (s, eventArgs) =>
                        {
                            e.OpenWiki();
                        };
                    }

                    var toggleButton = new GlowButton()
                    {
                        Parent = button,
                        Checked = enabled,
                        ToggleGlow = false
                    };

                    UpdateToggleButton(toggleButton);

                    toggleButton.CheckedChanged += (s, eventArgs) =>
                    {
                        if (setting != null)
                        {
                            setting.Value = eventArgs.Checked;
                            toggleButton.Checked = setting.Value;
                            //settings.Where(x => x.EntryKey != setting.EntryKey).ToList().ForEach(x => x.Value = setting.Value);
                            UpdateToggleButton(toggleButton);
                        }
                    };

                    toggleButton.Click += (s, eventArgs) =>
                    {
                        toggleButton.Checked = !toggleButton.Checked;
                    };
                }
            }
        }
    }
}
