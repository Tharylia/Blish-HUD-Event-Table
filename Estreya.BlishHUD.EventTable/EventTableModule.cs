namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Input;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Blish_HUD.Settings.UI.Views;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Json;
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

        private IEnumerable<EventCategory> EventCategories { get; set; }

        private bool visibleStateFromTick = true;

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
            using (var eventsReader = new StreamReader(ContentsManager.GetFileStream("events.json")))
            {
                //string eventsJson = await eventsReader.ReadToEndAsync();

                string json = await eventsReader.ReadToEndAsync();
                EventCategories = await Task.Run(() => JsonConvert.DeserializeObject<List<EventCategory>>(json));

            }

            this.ModuleSettings.InitializeEventSettings(EventCategories);

            this.Container = new EventTableContainer(EventCategories, this.ModuleSettings)
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
                    case nameof(ModuleSettings.LocationX):
                    case nameof(ModuleSettings.LocationY):
                    case nameof(ModuleSettings.Width):
                    case nameof(ModuleSettings.Height):
                        this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
                        this.Container.UpdateSize(ModuleSettings.Width.Value, this.ModuleSettings.Height.Value);
                        break;
                    case nameof(ModuleSettings.GlobalEnabled):
                        ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    default:
                        break;
                }
            };
        }

        private void ToggleContainer(bool show)
        {
            if (show)
            {
                Container.Show();
            }
            else
            {
                if (Container != null)
                {
                    Container.Hide();
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
            this.Container.UpdateSize(ModuleSettings.Width.Value, this.ModuleSettings.Height.Value);

            this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", ContentsManager.GetRenderIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(EventCategories, this.ModuleSettings.AllEvents));

            if (this.ModuleSettings.GlobalEnabled.Value)
                ToggleContainer(true);

            GameService.Gw2Mumble.UI.IsMapOpenChanged += (s, eventArgs) => ToggleContainer(!eventArgs.Value);
            GameService.Gw2Mumble.CurrentMap.MapChanged += (s, eventArgs) => ToggleContainer(GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate);

        }

        protected override void Update(GameTime gameTime)
        {
            CheckMumble();
            //this.Container.Update(gameTime);
        }

        private void CheckMumble()
        {
            if (Container != null)
            {
                if (GameService.Gw2Mumble.IsAvailable && this.ModuleSettings.HideOnMissingMumbleTicks.Value)
                {
                    bool tickState = GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
                    if (tickState != visibleStateFromTick)
                    {
                        visibleStateFromTick = tickState;
                        ToggleContainer(visibleStateFromTick);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            if (this.ManageEventTab != null)
                GameService.Overlay.BlishHudWindow.RemoveTab(this.ManageEventTab);

            if (Container != null)
                Container.Dispose();
        }
    }
}
