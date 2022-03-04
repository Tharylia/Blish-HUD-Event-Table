namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
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
            this.Panel = new Panel
            {
                Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
                Width = buildPanel.ContentRegion.Width - MAIN_PADDING.X * 2, // Why * 2?
                Height = buildPanel.ContentRegion.Height - MAIN_PADDING.Y,
                CanScroll = true,
                Parent = buildPanel
            };
            Rectangle contentRegion = Panel.ContentRegion;

            TextBox searchBox = new TextBox()
            {
                Parent = Panel,
                Width = Panel.MenuStandard.Size.X,
                Location = new Point(0, contentRegion.Y),
                PlaceholderText = "Search"
            };

            Panel eventCategoriesPanel = new Panel();
            eventCategoriesPanel.Title = "Event Categories";
            eventCategoriesPanel.Parent = Panel;
            eventCategoriesPanel.CanScroll = true;
            eventCategoriesPanel.ShowBorder = true;
            eventCategoriesPanel.Location = new Point(0, searchBox.Bottom + Panel.MenuStandard.ControlOffset.Y);
            eventCategoriesPanel.Size = new Point(Panel.MenuStandard.Size.X, contentRegion.Height - eventCategoriesPanel.Location.Y);
            Menu eventCategories = new Menu();
            eventCategories.Parent = eventCategoriesPanel;
            eventCategories.Size = eventCategoriesPanel.ContentRegion.Size;
            eventCategories.MenuItemHeight = 40;

            FlowPanel eventPanel = new FlowPanel();
            eventPanel.FlowDirection = ControlFlowDirection.LeftToRight;
            eventPanel.CanScroll = true;
            eventPanel.ShowBorder = true;
            eventPanel.Parent = Panel;
            eventPanel.Location = new Point(eventCategoriesPanel.Right + Panel.ControlStandard.ControlOffset.X, contentRegion.Y);
            eventPanel.Size = new Point(contentRegion.Width - eventPanel.Left, contentRegion.Height  - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

            searchBox.TextChanged += (s, e) =>
            {
                eventPanel.FilterChildren<DetailsButton>(detailsButton =>
                {
                    return detailsButton.Text.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
                });
            };

            #region Register Categories

            Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

            MenuItem allEvents = eventCategories.AddMenuItem("All Events");
            allEvents.Select();
            menus.Add(nameof(allEvents), allEvents);

            foreach (EventCategory category in EventCategories.GroupBy(ec => ec.Name).Select(ec => ec.First()))
            {
                menus.Add(category.Key, eventCategories.AddMenuItem(category.Name));
            }

            menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
            {
                MenuItem menuItem = s as MenuItem;
                eventPanel.FilterChildren<DetailsButton>(detailsButton =>
                {
                    IEnumerable<EventCategory> categories = EventCategories.Where(ec => ec.Events.Any(ev => ev.Name == detailsButton.Text));
                    return menuItem == menus[nameof(allEvents)] || categories.Any(ec => ec.Name == menuItem.Text);
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
                Text = "Check all",
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

                    DetailsButton detailsButton = control as DetailsButton;

                    if (detailsButton.Visible)
                    {
                        GlowButton glowButton = detailsButton.Children.Last() as GlowButton;
                        glowButton.Checked = true;
                    }
                });
            };

            StandardButton uncheckAllButton = new StandardButton()
            {
                Text = "Uncheck all",
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

                    DetailsButton detailsButton = control as DetailsButton;

                    if (detailsButton.Visible)
                    {
                        GlowButton glowButton = detailsButton.Children.Last() as GlowButton;
                        glowButton.Checked = false;
                    }
                });
            };

            foreach (EventCategory category in EventCategories)
            {
                IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Name).Select(eg => eg.First()) : category.Events;
                foreach (Event e in events)
                {
                    if (e.Filler) continue;

                    IEnumerable<SettingEntry<bool>> settings = this.EventSettings.FindAll(eventSetting => eventSetting.EntryKey == e.Name);

                    SettingEntry<bool> setting = settings.First();
                    bool enabled = setting.Value;

                    AsyncTexture2D icon = EventTableModule.ModuleInstance.ContentsManager.GetIcon(e.Icon);

                    var button = new DetailsButton()
                    {
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
                            Tooltip = new Tooltip(new TooltipView("Waypoint", "Click to Copy", icon: "images\\waypoint.png")),
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
                            Tooltip = new Tooltip(new TooltipView("Wiki", "Click to Open", icon: "images\\wiki.png")),
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
                            settings.Where(x => x.EntryKey != setting.EntryKey).ToList().ForEach(x => x.Value = setting.Value);
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
