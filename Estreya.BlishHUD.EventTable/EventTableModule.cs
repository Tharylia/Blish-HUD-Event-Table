namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.EventTable.UI.Container;
    using Gw2Sharp.Models;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class EventTableModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<EventTableModule>();

        internal static EventTableModule ModuleInstance;

        private EventTableContainer Container { get; set; }

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        internal ModuleSettings ModuleSettings;

        private WindowTab ManageEventTab { get; set; }

        internal TabbedWindow2 SettingsWindow { get; private set; }

        internal bool Debug => this.ModuleSettings.DebugEnabled.Value;

        private BitmapFont _font;

        internal BitmapFont Font
        {
            get
            {
                if (this._font == null)
                {
                    this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.ModuleSettings.EventFontSize.Value, ContentService.FontStyle.Regular);
                }

                return this._font;
            }
        }

        internal int EventHeight => this.ModuleSettings?.EventHeight?.Value ?? 30;
        internal DateTime DateTimeNow => DateTime.Now;


        private TimeSpan _eventTimeSpan = TimeSpan.Zero;

        internal TimeSpan EventTimeSpan
        {
            get
            {
                if (this._eventTimeSpan == TimeSpan.Zero)
                {
                    if (double.TryParse(this.ModuleSettings.EventTimeSpan.Value, out double timespan))
                    {
                        if (timespan > 1440)
                        {
                            timespan = 1440;
                            Logger.Warn($"Event Timespan over 1440. Cap at 1440 for performance reasons.");
                        }

                        this._eventTimeSpan = TimeSpan.FromMinutes(timespan);
                    }
                    else
                    {
                        Logger.Error($"Event Timespan '{this.ModuleSettings.EventTimeSpan.Value}' no real number, default to 120");
                        this._eventTimeSpan = TimeSpan.FromMinutes(120);
                    }
                }

                return this._eventTimeSpan;
            }
        }

        internal float EventTimeSpanRatio
        {
            get
            {
                float ratio = 0.5f + ((this.ModuleSettings.EventHistorySplit.Value / 100f) - 0.5f);
                return ratio;
            }
        }

        internal DateTime EventTimeMin
        {
            get
            {
                var millis = this.EventTimeSpan.TotalMilliseconds * (this.EventTimeSpanRatio);
                var timespan = TimeSpan.FromMilliseconds(millis);
                DateTime min = EventTableModule.ModuleInstance.DateTimeNow.Subtract(timespan);
                return min;
            }
        }

        internal DateTime EventTimeMax
        {
            get
            {
                var millis = this.EventTimeSpan.TotalMilliseconds * (1f - this.EventTimeSpanRatio);
                var timespan = TimeSpan.FromMilliseconds(millis);
                DateTime max = EventTableModule.ModuleInstance.DateTimeNow.Add(timespan);
                return max;
            }
        }

        private List<EventCategory> _eventCategories;

        internal List<EventCategory> EventCategories
        {
            get => _eventCategories.Where(ec => !ec.IsDisabled()).ToList();
            set => this._eventCategories = value;
        }

        internal Collection<ManagedState> States { get; private set; } = new Collection<ManagedState>();

        public HiddenState HiddenState { get; private set; }
        public WorldbossState WorldbossState { get; private set; }

        [ImportingConstructor]
        public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            this.ModuleSettings = new ModuleSettings(settings);
        }

        protected override void Initialize()
        {
        }

        protected override async Task LoadAsync()
        {

            using (StreamReader eventsReader = new StreamReader(this.ContentsManager.GetFileStream("events.json")))
            {
                string json = await eventsReader.ReadToEndAsync();
                this.EventCategories = JsonConvert.DeserializeObject<List<EventCategory>>(json);
            }


            this.ModuleSettings.InitializeEventSettings(this._eventCategories);


            this.Container = new EventTableContainer()
            {
                Parent = GameService.Graphics.SpriteScreen,
                BackgroundColor = Microsoft.Xna.Framework.Color.Transparent,
                Opacity = 0f,
                Visible = false
            };

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.Width):
                    //case nameof(this.ModuleSettings.Height):
                        this.Container.UpdateSize(this.ModuleSettings.Width.Value, -1);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    case nameof(ModuleSettings.EventTimeSpan):
                        this._eventTimeSpan = TimeSpan.Zero;
                        break;
                    case nameof(ModuleSettings.EventFontSize):
                        this._font = null;
                        break;
                    default:
                        break;
                }
            };

            await InitializeStates();
        }

        private async Task InitializeStates()
        {
            string eventsDirectory = this.DirectoriesManager.GetFullDirectoryPath("events");

            this.HiddenState = new HiddenState(eventsDirectory);
            this.WorldbossState = new WorldbossState(this.Gw2ApiManager);

            lock (this.States)
            {
                this.States.Add(this.HiddenState);
                this.States.Add(this.WorldbossState);
            }

            foreach (ManagedState state in this.States)
            {
                await state.Start();
            }
        }

        private void ToggleContainer(bool show)
        {
            if (this.Container == null) return;

            if (!this.ModuleSettings.GlobalEnabled.Value)
            {
                if (this.Container.Visible)
                {
                    this.Container.Hide();
                }

                return;
            }

            if (show)
            {
                if (!this.Container.Visible)
            {
                this.Container.Show();
                }
            }
            else
            {
                if (this.Container.Visible)
                {
                    this.Container.Hide();
                }
            }
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.ModuleSettingsView();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            // Base handler must be called
            base.OnModuleLoaded(e);

            this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
            this.Container.UpdateSize(this.ModuleSettings.Width.Value, -1);

            this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", this.ContentsManager.GetIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this._eventCategories, this.ModuleSettings.AllEvents));

            Rectangle settingsWindowSize = new Rectangle(0, 30, 1100, 720);
            Rectangle contentRegion = new Rectangle(settingsWindowSize.X + 46, settingsWindowSize.Y, settingsWindowSize.Width - 46, settingsWindowSize.Height);
            Texture2D windowBackground = this.ContentsManager.GetIcon(@"controls/window/502049", false);
            this.SettingsWindow = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Event Table",
                Emblem = this.ContentsManager.GetIcon(@"images\event_boss.png"),
                Subtitle = "Settings",
                SavesPosition = true,
                Id = $"{nameof(EventTableModule)}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.ContentsManager.GetIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this._eventCategories, this.ModuleSettings.AllEvents), "Manage Events"));
            this.SettingsWindow.Tabs.Add(new Tab(this.ContentsManager.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings), "General Settings"));
            this.SettingsWindow.Tabs.Add(new Tab(this.ContentsManager.GetIcon(@"155052"), () => new UI.Views.Settings.GraphicsSettingsView(this.ModuleSettings), "Graphic Settings"));
            this.SettingsWindow.Tabs.Add(new Tab(this.ContentsManager.GetIcon(@"155052"), () => new UI.Views.Settings.EventSettingsView(this.ModuleSettings), "Event Settings"));

            if (this.ModuleSettings.GlobalEnabled.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            this.CheckMumble();
            this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            this.CheckContainerSizeAndPosition();

            foreach (ManagedState state in this.States)
            {
                state.Update(gameTime);
            }
        }

        private void CheckContainerSizeAndPosition()
        {
            bool buildFromBottom = this.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - this.Container.Width;
            int minLocationY = buildFromBottom ? this.Container.Height : 0;
            int maxLocationY = buildFromBottom ? maxResY : maxResY - this.Container.Height;
            int minWidth = 0;
            int maxWidth = maxResX - this.ModuleSettings.LocationX.Value;

            this.ModuleSettings.LocationX.SetRange(minLocationX, maxLocationX);
            this.ModuleSettings.LocationY.SetRange(minLocationY, maxLocationY);
            this.ModuleSettings.Width.SetRange(minWidth, maxWidth);

#if !DEBUG
            return;
#endif

            if (this.ModuleSettings.LocationX.Value < minLocationX)
            {
                Logger.Debug($"LocationX unter min, set to: {minLocationX}");
                this.ModuleSettings.LocationX.Value = minLocationX;
            }

            if (this.ModuleSettings.LocationX.Value > maxLocationX)
            {
                Logger.Debug($"LocationX over max, set to: {maxLocationX}");
                this.ModuleSettings.LocationX.Value = maxLocationX;
            }

            if (this.ModuleSettings.LocationY.Value < minLocationY)
            {
                Logger.Debug($"LocationY unter min, set to: {minLocationY}");
                this.ModuleSettings.LocationY.Value = minLocationY;
            }

            if (this.ModuleSettings.LocationY.Value > maxLocationY)
            {
                Logger.Debug($"LocationY over max, set to: {maxLocationY}");
                this.ModuleSettings.LocationY.Value = maxLocationY;
            }

            if (this.ModuleSettings.Width.Value < minWidth)
            {
                Logger.Debug($"Width under min, set to: {minWidth}");
                this.ModuleSettings.Width.Value = minWidth;
            }

            if (this.ModuleSettings.Width.Value > maxWidth)
            {
                Logger.Debug($"Width over max, set to: {maxWidth}");
                this.ModuleSettings.Width.Value = maxWidth;
            }
        }

        private void CheckMumble()
        {
            if (GameService.Gw2Mumble.IsAvailable)
            {
            if (this.Container != null)
            {
                    bool show = true;

                    show &= !GameService.Gw2Mumble.UI.IsMapOpen;

                    if (this.ModuleSettings.HideOnMissingMumbleTicks.Value)
                {
                        show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
                    }

                    if (this.ModuleSettings.HideInCombat.Value)
                    {
                        show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
                    }

                    show &= GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate;

                    this.ToggleContainer(show);
                }
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            if (this.ManageEventTab != null)
            {
                GameService.Overlay.BlishHudWindow.RemoveTab(this.ManageEventTab);
            }

            if (this.Container != null)
            {
                this.Container.Dispose();
            }

            if (this.SettingsWindow != null)
            {
                this.SettingsWindow.Hide();
            }

            Logger.Debug("Unloading states...");
            Task.WaitAll(this.States.ToList().Select(state => state.Unload()).ToArray());
            Logger.Debug("Finished unloading states.");
        }
    }
}
