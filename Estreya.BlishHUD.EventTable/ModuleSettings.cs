namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.UI.Container;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;

    public class ModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();
        public event EventHandler<ModuleSettingsChangedEventArgs> ModuleSettingsChanged;

        public SettingCollection Settings { get; private set; }
        #region Global Settings
        private const string GLOBAL_SETTINGS = "event-table-global-settings";
        public SettingCollection GlobalSettings { get; private set; }
        public SettingEntry<bool> GlobalEnabled { get; private set; }
        public SettingEntry<KeyBinding> GlobalEnabledHotkey { get; private set; }
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> BackgroundColor { get; private set; }
        public SettingEntry<float> BackgroundColorOpacity { get; private set; }
        public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideInCombat { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }
        public SettingEntry<bool> ShowTooltips { get; private set; }
        public SettingEntry<bool> CopyWaypointOnClick { get; private set; }
        public SettingEntry<bool> ShowContextMenuOnClick { get; private set; }
        public SettingEntry<BuildDirection> BuildDirection { get; private set; }
        public SettingEntry<float> Opacity { get; set; }
        #endregion

        #region Location
        private const string LOCATION_SETTINGS = "event-table-location-settings";
        public SettingCollection LocationSettings { get; private set; }
        public SettingEntry<int> LocationX { get; private set; }
        public SettingEntry<int> LocationY { get; private set; }
        //public SettingEntry<int> Height { get; private set; }
        //public SettingEntry<bool> SnapHeight { get; private set; }
        public SettingEntry<int> Width { get; private set; }
        #endregion

        #region Events
        private const string EVENT_SETTINGS = "event-table-event-settings";
        private const string EVENT_LIST_SETTINGS = "event-table-event-list-settings";
        public SettingCollection EventSettings { get; private set; }
        public SettingEntry<string> EventTimeSpan { get; private set; } // Is listed in global
        public SettingEntry<int> EventHistorySplit { get; private set; } // Is listed in global
        public SettingEntry<int> EventHeight { get; private set; } // Is listed in global
        public SettingEntry<bool> DrawEventBorder { get; private set; } // Is listed in global
        public SettingEntry<EventTableContainer.FontSize/*ContentService.FontSize*/> EventFontSize { get; private set; } // Is listed in global
        public SettingEntry<bool> UseFiller { get; private set; } // Is listed in global
        public SettingEntry<bool> UseFillerEventNames { get; private set; } // Is listed in global
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> TextColor { get; private set; } // Is listed in global
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> FillerTextColor { get; private set; } // Is listed in global
        public List<SettingEntry<bool>> AllEvents { get; private set; } = new List<SettingEntry<bool>>();
        #endregion

        public ModuleSettings(SettingCollection settings)
        {
            this.Settings = settings;
            this.InitializeGlobalSettings(settings);
            this.InitializeLocationSettings(settings);
        }

        public void InitializeEventSettings(IEnumerable<EventCategory> eventCategories)
        {
            this.EventSettings = this.Settings.AddSubCollection(EVENT_SETTINGS);

            SettingCollection eventList = this.EventSettings.AddSubCollection(EVENT_LIST_SETTINGS);
            foreach (EventCategory category in eventCategories)
            {
                IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Name).Select(eg => eg.First()) : category.Events;
                foreach (Event e in events)
                {
                    SettingEntry<bool> setting = eventList.DefineSetting<bool>(e.Name, true);
                    setting.SettingChanged += this.SettingChanged;

                    this.AllEvents.Add(setting);
                }
            }
        }

        private void InitializeGlobalSettings(SettingCollection settings)
        {
            this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

            this.GlobalEnabled = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabled), true, () => "Event Table Enabled", () => "Whether the event table should be displayed.");
            this.GlobalEnabled.SettingChanged += this.SettingChanged;

            this.GlobalEnabledHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabledHotkey), new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.E), () => "Event Table Hotkey", () => "The keybinding which will toggle the event table.");
            this.GlobalEnabledHotkey.SettingChanged += this.SettingChanged;
            this.GlobalEnabledHotkey.Value.Enabled = true;
            this.GlobalEnabledHotkey.Value.Activated += (s,e) => this.GlobalEnabled.Value = !this.GlobalEnabled.Value;
            this.GlobalEnabledHotkey.Value.BlockSequenceFromGw2 = true;

            this.HideOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideOnMissingMumbleTicks), true, () => "Hide on cutscenes", () => "Whether the event table should hide when cutscenes are played.");
            this.HideOnMissingMumbleTicks.SettingChanged += this.SettingChanged;

            this.HideInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideInCombat), false, () => "Hide in Combat", () => "Whether the event table should hide when the player is in combat.");
            this.HideInCombat.SettingChanged += this.SettingChanged;

            this.BackgroundColor = this.GlobalSettings.DefineSetting(nameof(BackgroundColor), EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.GetAsync(1).Result, () => "Background Color", () => "Defines the background color.");
            this.BackgroundColor.SettingChanged += this.SettingChanged;

            this.BackgroundColorOpacity = this.GlobalSettings.DefineSetting(nameof(BackgroundColorOpacity), 0.0f, () => "Background Color Opacity", () => "Defines the opacity of the background.");
            this.BackgroundColorOpacity.SetRange(0.0f, 1f);
            this.BackgroundColorOpacity.SettingChanged += this.SettingChanged;

            this.EventTimeSpan = this.GlobalSettings.DefineSetting(nameof(this.EventTimeSpan), "120", () => "Event Timespan", () => "The timespan the event table should cover.");
            //this.EventTimeSpan.SetRange(30, 60 * 5);
            this.EventTimeSpan.SettingChanged += this.SettingChanged;

            this.EventHistorySplit = this.GlobalSettings.DefineSetting(nameof(this.EventHistorySplit), 50, () => "Event History Split", () => "Defines how much history the timespan should contain.");
            this.EventHistorySplit.SetRange(0,75);
            this.EventHistorySplit.SettingChanged += this.SettingChanged;

            this.EventHeight = this.GlobalSettings.DefineSetting(nameof(this.EventHeight), 20, () => "Event Height", () => "Defines the height of a single event row.");
            this.EventHeight.SetRange(5, 50);
            this.EventHeight.SettingChanged += this.SettingChanged;

            this.EventFontSize = this.GlobalSettings.DefineSetting(nameof(this.EventFontSize), EventTableContainer.FontSize.Size16 /*ContentService.FontSize.Size16*/, () => "Event Font Size", () => "Defines the size of the font used for events.");
            this.EventFontSize.SettingChanged += this.SettingChanged;

            this.DrawEventBorder = this.GlobalSettings.DefineSetting(nameof(this.DrawEventBorder), true, () => "Draw Event Border", () => "Whether the events should have a small border.");
            this.DrawEventBorder.SettingChanged += this.SettingChanged;

            this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => "Debug Enabled", () => "Whether the event table should be running in debug mode.");
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            this.ShowTooltips = this.GlobalSettings.DefineSetting(nameof(this.ShowTooltips), true, () => "Show Tooltips", () => "Whether the event table should display event information on hover.");
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            this.CopyWaypointOnClick = this.GlobalSettings.DefineSetting(nameof(this.CopyWaypointOnClick), true, () => "Copy Waypoints", () => "Whether the event table should copy waypoints to clipboard if event has been left clicked.");
            this.CopyWaypointOnClick.SettingChanged += this.SettingChanged;

            this.ShowContextMenuOnClick = this.GlobalSettings.DefineSetting(nameof(this.ShowContextMenuOnClick), true, () => "Show Context Menu", () => "Whether the event table should show a context menu if an event has been right clicked.");
            this.ShowContextMenuOnClick.SettingChanged += this.SettingChanged;

            this.BuildDirection = this.GlobalSettings.DefineSetting(nameof(this.BuildDirection), Models.BuildDirection.Top, () => "Build Direction", () => "Whether the event table should be build from the top or the bottom.");
            this.BuildDirection.SettingChanged += this.SettingChanged;

            this.Opacity = this.GlobalSettings.DefineSetting(nameof(this.Opacity), 1f, () => "Opacity", () => "Defines the opacity of the event table.");
            this.Opacity.SetRange(0.1f, 1f);
            this.Opacity.SettingChanged += this.SettingChanged;

            this.UseFiller = this.GlobalSettings.DefineSetting(nameof(this.UseFiller), false, () => "Use Filler Events", () => "Whether the event table should fill empty spaces with filler events.");
            this.UseFiller.SettingChanged += this.SettingChanged;

            this.UseFillerEventNames = this.GlobalSettings.DefineSetting(nameof(this.UseFillerEventNames), false, () => "Use Filler Event Names", () => "Whether the event fillers should have names.");
            this.UseFillerEventNames.SettingChanged += this.SettingChanged;

            this.TextColor = this.GlobalSettings.DefineSetting(nameof(TextColor), EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.GetAsync(1).Result, () => "Text Color", () => "Defines the text color of events.");
            this.TextColor.SettingChanged += this.SettingChanged;

            this.FillerTextColor = this.GlobalSettings.DefineSetting(nameof(FillerTextColor), EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.GetAsync(1).Result, () => "Filler Text Color", () => "Defines the text color of filler events.");
            this.FillerTextColor.SettingChanged += this.SettingChanged;
        }

        private void InitializeLocationSettings(SettingCollection settings)
        {
            this.LocationSettings = settings.AddSubCollection(LOCATION_SETTINGS);

            var height = 1080;
            var width = 1920;

            this.LocationX = this.LocationSettings.DefineSetting(nameof(this.LocationX), (int)(width * 0.1), () => "Location X", () => "Where the event table should be displayed on the X axis.");
            this.LocationX.SetRange(0, (int)width);// (int)(GameService.Graphics.Resolution.X * 0.8));
            this.LocationX.SettingChanged += this.SettingChanged;

            this.LocationY = this.LocationSettings.DefineSetting(nameof(this.LocationY), (int)(height * 0.1), () => "Location Y", () => "Where the event table should be displayed on the Y axis.");
            this.LocationY.SetRange(0, (int)height);// (int)(GameService.Graphics.Resolution.Y * 0.8));
            this.LocationY.SettingChanged += this.SettingChanged;

            //this.Height = this.LocationSettings.DefineSetting(nameof(this.Height), (int)(height * 0.2), () => "Height", () => "The height of the event table.");
            //this.Height.SetRange(0, (int)height);// GameService.Graphics.Resolution.Y);
            //this.Height.SetDisabled(true);
            //this.Height.SettingChanged += this.SettingChanged;

            /*
            this.SnapHeight = this.LocationSettings.DefineSetting(nameof(this.SnapHeight), true, () => "Snap Height", () => "Whether the event table should auto resize height to content.");
            this.SnapHeight.SettingChanged += (s, e) =>
            {
                this.Height.SetDisabled(e.NewValue);
                this.SettingChanged(s, e);
            };
            */

            this.Width = this.LocationSettings.DefineSetting(nameof(this.Width), (int)(width * 0.5), () => "Width", () => "The width of the event table.");
            this.Width.SetRange(0, (int)width);// GameService.Graphics.Resolution.X);
            this.Width.SettingChanged += this.SettingChanged;
        }

        private void SettingChanged<T>(object sender, ValueChangedEventArgs<T> e)
        {
            SettingEntry<T> settingEntry = (SettingEntry<T>)sender;
            var prevValue = JsonConvert.SerializeObject(e.PreviousValue);
            var newValue = JsonConvert.SerializeObject(e.NewValue);
            Logger.Debug($"Changed setting \"{settingEntry.EntryKey}\" from \"{prevValue}\" to \"{newValue}\"");

            ModuleSettingsChanged?.Invoke(this, new ModuleSettingsChangedEventArgs() { Name = settingEntry.EntryKey, Value = e.NewValue });
        }

        public class ModuleSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

    }
}
