namespace BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Settings;
    using BlishHUD.EventTable.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ModuleSettings
    {
        public event EventHandler<ModuleSettingsChangedEventArgs> ModuleSettingsChanged;

        public SettingCollection Settings { get; private set; }
        #region Global Settings
        private const string GLOBAL_SETTINGS = "event-table-global-settings";
        public SettingCollection GlobalSettings { get; private set; }
        public SettingEntry<bool> GlobalEnabled { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }

        #region Location
        private const string LOCATION_SETTINGS = "event-table-location-settings";
        public SettingCollection LocationSettings { get; private set; }
        public SettingEntry<int> LocationX { get; private set; }
        public SettingEntry<int> LocationY { get; private set; }
        public SettingEntry<int> Height { get; private set; }
        public SettingEntry<bool> SnapHeight { get; private set; }
        public SettingEntry<int> Width { get; private set; }
        #endregion

        #region Events
        private const string EVENT_SETTINGS = "event-table-event-settings";
        private const string EVENT_LIST_SETTINGS = "event-table-event-list-settings";
        public SettingCollection EventSettings { get; private set; }
        public SettingEntry<int> EventTimeSpan { get; private set; } // Is listed in global
        public List<SettingEntry<bool>> AllEvents { get; private set; } = new List<SettingEntry<bool>>();
        #endregion

        #endregion
        public ModuleSettings(SettingCollection settings)
        {
            this.Settings = settings;
            InitializeGlobalSettings(settings);
            InitializeLocationSettings(settings);
        }

        public void InitializeEventSettings(IEnumerable<EventCategory> eventCategories)
        {
            this.EventSettings = this.Settings.AddSubCollection(EVENT_SETTINGS);

            SettingCollection eventList = this.EventSettings.AddSubCollection(EVENT_LIST_SETTINGS);
            foreach (EventCategory category in eventCategories)
            {
                foreach (Event e in category.Events)
                {
                    SettingEntry<bool> setting = eventList.DefineSetting<bool>(e.Name, true);
                    setting.SettingChanged += SettingChanged;

                    this.AllEvents.Add(setting);

                    if (category.ShowCombined) break;
                }
            }
        }

        private void InitializeGlobalSettings(SettingCollection settings)
        {
            this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

            this.GlobalEnabled = GlobalSettings.DefineSetting(nameof(this.GlobalEnabled), true, () => "Event Table Enabled", () => "Whether the event table should be displayed.");
            this.GlobalEnabled.SettingChanged += SettingChanged;

            this.EventTimeSpan = GlobalSettings.DefineSetting(nameof(this.EventTimeSpan), 120, () => "Event Timespan", () => "The timespan the event table should cover.");
            this.EventTimeSpan.SetRange(60, 240);
            this.EventTimeSpan.SettingChanged += SettingChanged;

            this.DebugEnabled = GlobalSettings.DefineSetting(nameof(this.DebugEnabled), true, () => "Debug Enabled", () => "Whether the event table should be running in debug mode.");
            this.DebugEnabled.SettingChanged += SettingChanged;
        }

        private void InitializeLocationSettings(SettingCollection settings)
        {
            this.LocationSettings = settings.AddSubCollection(LOCATION_SETTINGS);

            this.LocationX = LocationSettings.DefineSetting(nameof(this.LocationX), (int)(GameService.Graphics.Resolution.X * 0.1), () => "Location X", () => "Where the event table should be displayed on the X axis.");
            this.LocationX.SetRange(0, 2000);// (int)(GameService.Graphics.Resolution.X * 0.8));
            this.LocationX.SettingChanged += SettingChanged;

            this.LocationY = LocationSettings.DefineSetting(nameof(this.LocationY), (int)(GameService.Graphics.Resolution.Y * 0.1), () => "Location Y", () => "Where the event table should be displayed on the Y axis.");
            this.LocationY.SetRange(0, 2000);// (int)(GameService.Graphics.Resolution.Y * 0.8));
            this.LocationY.SettingChanged += SettingChanged;

            this.Height = LocationSettings.DefineSetting(nameof(this.Height), GameService.Graphics.Resolution.Y, () => "Height", () => "The height of the event table.");
            this.Height.SetRange(0, 2000);// GameService.Graphics.Resolution.Y);
            this.Height.SetDisabled(true);
            this.Height.SettingChanged += SettingChanged;

            this.SnapHeight = LocationSettings.DefineSetting(nameof(this.SnapHeight), true, () => "Snap Height", () => "Whether the event table should auto resize height to content.");
            this.SnapHeight.SettingChanged += (s, e) =>
            {
                this.Height.SetDisabled(e.NewValue);
                SettingChanged(s, e);
            };

            this.Width = LocationSettings.DefineSetting(nameof(this.Width), GameService.Graphics.Resolution.X, () => "Width", () => "The width of the event table.");
            this.Width.SetRange(0, 2000);// GameService.Graphics.Resolution.X);
            this.Width.SettingChanged += SettingChanged;
        }

        private void SettingChanged<T>(object sender, ValueChangedEventArgs<T> e)
        {
            SettingEntry<T> settingEntry = (SettingEntry<T>)sender;
            ModuleSettingsChanged?.Invoke(this, new ModuleSettingsChangedEventArgs() { Name = settingEntry.EntryKey, Value = e.NewValue });
        }

        public class ModuleSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

    }
}
