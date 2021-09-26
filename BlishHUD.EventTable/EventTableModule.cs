﻿namespace BlishHUD.EventTable
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
    using BlishHUD.EventTable.Json;
    using BlishHUD.EventTable.Models;
    using BlishHUD.EventTable.UI.Container;
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

        private IEnumerable<EventCategory> EventCategories { get; set; }

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

            this.Container.SetDebugMode(this.ModuleSettings.DebugEnabled.Value);
            this.Container.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
            this.Container.UpdateSize(ModuleSettings.Width.Value, this.ModuleSettings.Height.Value);

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
                    case nameof(ModuleSettings.DebugEnabled):
                        Container.SetDebugMode(this.ModuleSettings.DebugEnabled.Value);
                        break;
                    case nameof(ModuleSettings.EventTimeSpan):
                        this.Container.UpdateEventTimeSpan(TimeSpan.FromMinutes(this.ModuleSettings.EventTimeSpan.Value));
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
                    Container.Hide();/*.OnComplete(() =>
                    {
                        Container.Dispose();
                        Container = null;
                    });*/
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

            GameService.Overlay.BlishHudWindow.AddTab("Event Table", ContentsManager.GetTexture(""), () => new UI.Views.ManageEventsView(EventCategories, this.ModuleSettings.AllEvents));

            if (this.ModuleSettings.GlobalEnabled.Value)
                ToggleContainer(true);
        }

        protected override void Update(GameTime gameTime)
        {
            //this.Container.Update(gameTime);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            if (Container != null)
                Container.Dispose();
        }
    }
}
