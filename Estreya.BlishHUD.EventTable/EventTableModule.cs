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
    using Estreya.BlishHUD.EventTable.UI.Container;
    using Gw2Sharp.Models;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class EventTableModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<EventTableModule>();
        private const double INTERVAL_UPDATE_WORLDBOSSES = 300010; // 5 minutes + 10ms
        private TimeSpan TIME_SINCE_LAST_UPDATE_WORLDBOSSES = TimeSpan.FromMilliseconds(INTERVAL_UPDATE_WORLDBOSSES);

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

        public TabbedWindow2 SettingsWindow { get; private set; }

        private IEnumerable<EventCategory> EventCategories { get; set; }

        private bool visibleStateFromTick = true;

        internal bool Debug => this.ModuleSettings.DebugEnabled.Value;

        public List<string> CompletedWorldbosses { get; set; } = new List<string>();

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
            Gw2ApiManager.SubtokenUpdated += this.Gw2ApiManager_SubtokenUpdated;
        }

        private void Gw2ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
        {
            Task.Run(async () =>
            {
                await this.UpdateCompletedWorldbosses(null);
            });
        }

        protected override async Task LoadAsync()
        {
            using (StreamReader eventsReader = new StreamReader(this.ContentsManager.GetFileStream("events.json")))
            {
                //string eventsJson = await eventsReader.ReadToEndAsync();

                string json = await eventsReader.ReadToEndAsync();
                this.EventCategories = await Task.Run(() => JsonConvert.DeserializeObject<List<EventCategory>>(json));

            }

            this.ModuleSettings.InitializeEventSettings(this.EventCategories);

            this.Container = new EventTableContainer(this.EventCategories, this.ModuleSettings)
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
                    case nameof(this.ModuleSettings.Height):
                        this.Container.UpdateSize(this.ModuleSettings.Width.Value, this.ModuleSettings.Height.Value);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    default:
                        break;
                }
            };
        }

        private async Task UpdateCompletedWorldbosses(GameTime gameTime)
        {
            if (gameTime != null)
            {
                TIME_SINCE_LAST_UPDATE_WORLDBOSSES += gameTime.ElapsedGameTime;
            }
            if (gameTime == null || TIME_SINCE_LAST_UPDATE_WORLDBOSSES.TotalMilliseconds >= INTERVAL_UPDATE_WORLDBOSSES)
            {
                try
                {
                    lock (this.CompletedWorldbosses)
                    {
                        this.CompletedWorldbosses.Clear();
                    }

                    if (this.Gw2ApiManager.HasPermissions(new[] { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression }))
                    {
                        Gw2Sharp.WebApi.V2.IApiV2ObjectList<string> bosses = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.WorldBosses.GetAsync();
                        lock (this.CompletedWorldbosses)
                        {
                            this.CompletedWorldbosses.AddRange(bosses);
                        }

                        TIME_SINCE_LAST_UPDATE_WORLDBOSSES = TimeSpan.Zero;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void ToggleContainer(bool show)
        {
            if (this.ModuleSettings.GlobalEnabled.Value && show)
            {
                this.Container.Show();
            }
            else
            {
                if (this.Container != null)
                {
                    this.Container.Hide();
                }
            }
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.ModuleSettingsView(this.ModuleSettings);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {

            // Base handler must be called
            base.OnModuleLoaded(e);

            this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
            this.Container.UpdateSize(this.ModuleSettings.Width.Value, this.ModuleSettings.Height.Value);

            this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", this.ContentsManager.GetRenderIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this.EventCategories, this.ModuleSettings.AllEvents));

            Rectangle settingsWindowSize = new Rectangle(24, 30, 1000, 630);
            this.SettingsWindow = new TabbedWindow2(this.ContentsManager.GetRenderIcon(@"images\windowBackground.png"), settingsWindowSize, new Rectangle(settingsWindowSize.X + 46, settingsWindowSize.Y, settingsWindowSize.Width - 46, settingsWindowSize.Height))
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "TabbedWindow",
                Emblem = this.ContentsManager.GetRenderIcon(@"images\event_boss.png"),
                Subtitle = "Example Subtitle",
                SavesPosition = true,
                Id = $"{nameof(EventTableModule)}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.SettingsWindow.Emblem, () => new UI.Views.ManageEventsView(this.EventCategories, this.ModuleSettings.AllEvents), "Events"));

            if (this.ModuleSettings.GlobalEnabled.Value)
            {
                this.ToggleContainer(true);
            }

            GameService.Gw2Mumble.UI.IsMapOpenChanged += (s, eventArgs) => this.ToggleContainer(!eventArgs.Value);
            GameService.Gw2Mumble.CurrentMap.MapChanged += (s, eventArgs) => this.ToggleContainer(GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate);

        }

        protected override void Update(GameTime gameTime)
        {
            this.CheckMumble();
            //this.Container.Update(gameTime);
            this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            Task.Run(async () =>
            {
                await UpdateCompletedWorldbosses(gameTime);
            });
        }

        private void CheckMumble()
        {
            if (this.Container != null)
            {
                if (GameService.Gw2Mumble.IsAvailable && this.ModuleSettings.HideOnMissingMumbleTicks.Value)
                {
                    bool tickState = GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
                    if (tickState != this.visibleStateFromTick)
                    {
                        this.visibleStateFromTick = tickState;
                        this.ToggleContainer(this.visibleStateFromTick);
                    }
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

            if (this.SettingsWindow != null)
            {
                this.SettingsWindow.Dispose();
            }

            if (this.Container != null)
            {
                this.Container.Dispose();
            }
        }
    }
}
