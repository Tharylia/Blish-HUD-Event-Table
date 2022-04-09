namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.EventTable.UI.Container;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
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

        internal ModuleSettings ModuleSettings { get; private set; }

        private CornerIcon CornerIcon { get; set; }

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
                double millis = this.EventTimeSpan.TotalMilliseconds * (this.EventTimeSpanRatio);
                TimeSpan timespan = TimeSpan.FromMilliseconds(millis);
                DateTime min = EventTableModule.ModuleInstance.DateTimeNow.Subtract(timespan);
                return min;
            }
        }

        internal DateTime EventTimeMax
        {
            get
            {
                double millis = this.EventTimeSpan.TotalMilliseconds * (1f - this.EventTimeSpanRatio);
                TimeSpan timespan = TimeSpan.FromMilliseconds(millis);
                DateTime max = EventTableModule.ModuleInstance.DateTimeNow.Add(timespan);
                return max;
            }
        }

        private SemaphoreSlim EventCategorySemaphore = new SemaphoreSlim(1, 1);

        private List<EventCategory> _eventCategories = new List<EventCategory>();

        public List<EventCategory> EventCategories
        {
            get
            {
                    return this._eventCategories.Where(ec => !ec.IsDisabled()).ToList();
                }
            }
        #region States

        private readonly AsyncLock _stateLock = new AsyncLock();
        internal Collection<ManagedState> States { get; private set; } = new Collection<ManagedState>();

        public HiddenState HiddenState { get; private set; }
        public WorldbossState WorldbossState { get; private set; }
        public MapchestState MapchestState { get; private set; }
        public EventFileState EventFileState { get; private set; }
        public IconState IconState { get; private set; }
        #endregion

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
            this.Container = new EventTableContainer()
            {
                Parent = GameService.Graphics.SpriteScreen,
                BackgroundColor = Microsoft.Xna.Framework.Color.Transparent,
                Opacity = 0f,
                Visible = false
            };
        }

        protected override async Task LoadAsync()
        {
            Logger.Debug("Load module settings.");
            await this.ModuleSettings.LoadAsync();

            await this.InitializeStates(true);

            await this.LoadEvents();

            lock (this._eventCategories)
            {
                this.ModuleSettings.InitializeEventSettings(this._eventCategories);
            }

            await this.InitializeStates(false);

            await this.Container.LoadAsync();

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.Width):
                        this.Container.UpdateSize(this.ModuleSettings.Width.Value, -1);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    case nameof(this.ModuleSettings.EventTimeSpan):
                        this._eventTimeSpan = TimeSpan.Zero;
                        break;
                    case nameof(this.ModuleSettings.EventFontSize):
                        this._font = null;
                        break;
                    case nameof(this.ModuleSettings.RegisterCornerIcon):
                        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
                        break;
                    case nameof(this.ModuleSettings.BackgroundColor):
                    case nameof(this.ModuleSettings.BackgroundColorOpacity):
                        this.Container.UpdateBackgroundColor();
                        break;
                    default:
                        break;
                }
            };

        }

        /// <summary>
        /// Reloads all events.
        /// </summary>
        /// <returns></returns>
        public async Task LoadEvents()
        {
            string threadName = $"{Thread.CurrentThread.ManagedThreadId}";
            Logger.Debug("Try loading events from thread: {0}", threadName);

            await EventCategorySemaphore.WaitAsync();

            Logger.Debug("Thread \"{0}\" started loading", threadName);

            try
            {
                if (this._eventCategories != null)
                {
                    lock (this._eventCategories)
                    {
                        foreach (EventCategory ec in _eventCategories)
                        {
                            ec.Unload();
                        }

                        this._eventCategories.Clear();
                    }
                }

                EventSettingsFile eventSettingsFile = await this.EventFileState.GetExternalFile();

                if (eventSettingsFile == null)
                {
                    Logger.Error($"Failed to load event file.");
                    return;
                }

                Logger.Info($"Loaded event file version: {eventSettingsFile.Version}");

                List<EventCategory> categories = eventSettingsFile.EventCategories ?? new List<EventCategory>();

                int eventCategoryCount = categories.Count;
                int eventCount = categories.Sum(ec => ec.Events.Count);

                Logger.Info($"Loaded {eventCategoryCount} Categories with {eventCount} Events.");

                /*
                foreach (EventCategory ec in categories)
                    {
                    await ec.LoadAsync();
                        }
                */

                var eventCategoryLoadTasks = categories.Select(ec =>
                        {
                    return ec.LoadAsync();
                    });

                await Task.WhenAll(eventCategoryLoadTasks);

                lock (this._eventCategories)
                {
                    Logger.Debug("Overwrite current categories with newly loaded.");
                    this._eventCategories = categories;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed loading events.");
                throw ex;
            }
            finally
            {
                EventCategorySemaphore.Release();
                Logger.Debug("Thread \"{0}\" released loading lock", threadName);
            }
        }

        private async Task InitializeStates(bool beforeFileLoaded = false)
        {
            string eventsDirectory = this.DirectoriesManager.GetFullDirectoryPath("events");

            var hideEventAction = (string apiCode) =>
            {
                if (this.ModuleSettings.EventCompletedAcion.Value == EventCompletedAction.Hide)
                {
                    lock (this._eventCategories)
                    {
                        List<Event> events = this._eventCategories.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
                        events.ForEach(ev => ev.Finish());
                    }
                }
            };

            if (!beforeFileLoaded)
            {
                this.HiddenState = new HiddenState(eventsDirectory);
                this.WorldbossState = new WorldbossState(this.Gw2ApiManager);
                this.WorldbossState.WorldbossCompleted += (s, e) =>
                {
                    hideEventAction.Invoke(e);
                };
                this.MapchestState = new MapchestState(this.Gw2ApiManager);
                this.MapchestState.MapchestCompleted += (s, e) =>
                {
                    hideEventAction.Invoke(e);
                };
            }
            else
            {
                this.EventFileState = new EventFileState(this.ContentsManager, eventsDirectory, "events.json");
                this.IconState = new IconState(this.ContentsManager, eventsDirectory);
            }

            lock (this.States)
            {
                if (!beforeFileLoaded)
                {
                    this.States.Add(this.HiddenState);
                    this.States.Add(this.WorldbossState);
                    this.States.Add(this.MapchestState);
                }
                else
                {
                    this.States.Add(this.EventFileState);
                    this.States.Add(this.IconState);
                }
            }

            using (await _stateLock.LockAsync())
            {
                try
                {
            foreach (ManagedState state in this.States)
            {
                        Logger.Debug("Starting managed state: {0}", state.GetType().Name);
                await state.Start();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed starting states.");
                }
            }
        }

        private void HandleCornerIcon(bool show)
        {
            if (show)
            {
                this.CornerIcon = new CornerIcon()
                {
                    IconName = "Event Table",
                    Icon = this.ContentsManager.GetTexture(@"images\event_boss_grey.png"),
                };

                this.CornerIcon.Click += (s, ea) =>
                {
                    this.SettingsWindow.ToggleWindow();
                };
            }
            else
            {
                if (this.CornerIcon != null)
                {
                    this.CornerIcon.Dispose();
                    this.CornerIcon = null;
                }
            }
        }

        private void ToggleContainer(bool show)
        {
            if (this.Container == null)
            {
                return;
            }

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

            //this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", this.ContentsManager.GetIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this._eventCategories, this.ModuleSettings.AllEvents));

            Texture2D windowBackground = this.IconState.GetIcon(@"images\502049.png", false);

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X + 46;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

            this.SettingsWindow = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = Strings.SettingsWindow_Title,
                Emblem = this.IconState.GetIcon(@"images\event_boss.png"),
                Subtitle = Strings.SettingsWindow_Subtitle,
                SavesPosition = true,
                Id = $"{nameof(EventTableModule)}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\event_boss_grey.png"), () => new UI.Views.ManageEventsView(), Strings.SettingsWindow_ManageEvents_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\bars.png"), () => new UI.Views.ReorderEventsView(), "Reorder"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings), Strings.SettingsWindow_GeneralSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView(this.ModuleSettings), Strings.SettingsWindow_GraphicSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"155052"), () => new UI.Views.Settings.EventSettingsView(this.ModuleSettings), Strings.SettingsWindow_EventSettings_Title));


            this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);

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

            using (_stateLock.Lock())
            {
            foreach (ManagedState state in this.States)
            {
                state.Update(gameTime);
                }
            }

            lock (this._eventCategories)
            {
                this._eventCategories.ForEach(ec =>
                {
                    ec.Update(gameTime);
                });
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

            /*
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
            */
        }

        private void CheckMumble()
        {
            if (GameService.Gw2Mumble.IsAvailable)
            {
                if (this.Container != null)
                {
                    bool show = true;

                    if (this.ModuleSettings.HideOnOpenMap.Value)
                    {
                        show &= !GameService.Gw2Mumble.UI.IsMapOpen;
                    }

                    if (this.ModuleSettings.HideOnMissingMumbleTicks.Value)
                    {
                        show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
                    }

                    if (this.ModuleSettings.HideInCombat.Value)
                    {
                        show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
                    }

                    //show &= GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate;

                    this.ToggleContainer(show);
                }
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            base.Unload();

            foreach (EventCategory ec in _eventCategories)
            {
                ec.Unload();
            }

            if (this.Container != null)
            {
                this.Container.Dispose();
            }

            if (this.SettingsWindow != null)
            {
                this.SettingsWindow.Hide();
            }

            this.HandleCornerIcon(false);

            Logger.Debug("Unloading states...");
            using (this._stateLock.Lock())
            {
            Task.WaitAll(this.States.ToList().Select(state => state.Unload()).ToArray());
            }
            Logger.Debug("Finished unloading states."); ;
        }
    }
}
