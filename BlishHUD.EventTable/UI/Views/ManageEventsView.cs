namespace BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings;
    using BlishHUD.EventTable.Models;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ManageEventsView : View
    {
        private static readonly Logger Logger = Logger.GetLogger<ManageEventsView>();

        public FlowPanel FlowPanel { get; private set; }

        private IEnumerable<EventCategory> EventCategories { get; set; }
        private List<SettingEntry<bool>> EventSettings { get; set; }

        public ManageEventsView(IEnumerable<EventCategory> categories, List<SettingEntry<bool>> settings)
        {
            this.EventCategories = categories;
            this.EventSettings = settings;
        }

        private void UpdateToggleButton(GlowButton button)
        {
            button.Icon = button.Checked ? GameService.Content.GetTexture("minus") : GameService.Content.GetTexture("plus");
        }

        protected override void Build(Container buildPanel)
        {
            this.FlowPanel = new FlowPanel
            {
                Width = buildPanel.Width,
                Height = buildPanel.Height,
                Top = 0,
                CanScroll = true,
                ControlPadding = new Vector2(0, 15),
                OuterControlPadding = new Vector2(20, 5),
                Parent = buildPanel
            };
            Rectangle contentRegion = FlowPanel.ContentRegion;

            Panel eventCategoriesPanel = new Panel();
            eventCategoriesPanel.Title = "Event Categories";
            eventCategoriesPanel.Parent = FlowPanel;
            eventCategoriesPanel.Location = new Point(Panel.MenuStandard.PanelOffset.X, Panel.MenuStandard.PanelOffset.Y);
            eventCategoriesPanel.Size = Panel.MenuStandard.Size - new Point(0, Panel.MenuStandard.PanelOffset.Y);
            eventCategoriesPanel.CanScroll = true;
            eventCategoriesPanel.ShowBorder = true;
            Menu eventCategories = new Menu();
            eventCategories.Parent = eventCategoriesPanel;
            eventCategories.Size = eventCategoriesPanel.ContentRegion.Size;
            eventCategories.MenuItemHeight = 40;

            FlowPanel eventPanel = new FlowPanel();
            eventPanel.FlowDirection = ControlFlowDirection.LeftToRight;
            eventPanel.CanScroll = true;
            eventPanel.ShowBorder = true;
            eventPanel.Parent = FlowPanel;
            eventPanel.Size = new Point(contentRegion.Width - eventCategoriesPanel.Width - (Panel.MenuStandard.PanelOffset.X * 3), contentRegion.Height - Panel.MenuStandard.PanelOffset.Y);
            eventPanel.Location = new Point(eventCategoriesPanel.Location.X + eventCategoriesPanel.Width + Panel.MenuStandard.PanelOffset.X, Panel.MenuStandard.PanelOffset.Y);

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

            foreach (EventCategory category in EventCategories)
            {
                foreach (Event e in category.Events)
                {
                    IEnumerable<SettingEntry<bool>> settings = this.EventSettings.FindAll(eventSetting => eventSetting.EntryKey.Contains(e.Name));

                    SettingEntry<bool> setting = settings.First();
                    bool enabled = setting.Value;

                    var button = new DetailsButton()
                    {
                        Parent = eventPanel,
                        Text = e.Name,
                        Icon = e.Icon == null ? null : GameService.Content.GetRenderServiceTexture(e.Icon),
                        ShowToggleButton = true,
                        FillColor = Color.LightBlue,
                        //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                    };
                    var toggleButton = new GlowButton()
                    {
                        Parent = button,
                        Checked = enabled,
                        ToggleGlow = false
                    };

                    UpdateToggleButton(toggleButton);

                    toggleButton.Click += (s, eventArgs) =>
                    {
                        if (setting != null)
                        {
                            setting.Value = !setting.Value;
                            toggleButton.Checked = setting.Value;
                            settings.Where(x => x.EntryKey != setting.EntryKey).ToList().ForEach(x => x.Value = setting.Value);
                            UpdateToggleButton(toggleButton);
                        }
                    };

                    if (category.ShowCombined) break;
                }
            }
        }
    }
}
