namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Utils;
    using Gw2Sharp.WebApi.Exceptions;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventFileState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<EventFileState>();
        private TimeSpan updateInterval = TimeSpan.FromHours(1);
        private double timeSinceUpdate = 0;

        private static object _lockObject = new object();
        private string Directory { get; set; }
        private string FileName { get; set; }

        private string FilePath { get => Path.Combine(Directory, FileName); }

        private bool _notified = false;

        private ContentsManager ContentsManager { get; set; }

        public EventFileState(ContentsManager contentsManager, string directory, string fileName)
        {
            this.ContentsManager = contentsManager;
            this.Directory = directory;
            this.FileName = fileName;
        }

        public override async Task Reload()
        {
            await this.CheckAndNotify(null);
        }

        protected override async Task Initialize()
        {
            if (!this.ExternalFileExists() || (EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile.Value && await this.IsNewFileVersionAvaiable()))
            {
                await this.ExportFile();
            }

            timeSinceUpdate = updateInterval.TotalMilliseconds;
        }

        protected override Task InternalUnload()
        {
            return Task.CompletedTask;
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            UpdateCadenceUtil.UpdateAsyncWithCadence(CheckAndNotify, gameTime, updateInterval.TotalMilliseconds, ref timeSinceUpdate);
        }

        protected override Task Load()
        {
            return Task.CompletedTask;
        }

        protected override Task Save()
        {
            return Task.CompletedTask;
        }

        private async Task CheckAndNotify(GameTime gameTime)
        {
            lock (_lockObject)
            {
                if (_notified) return;
            }

            if (await this.IsNewFileVersionAvaiable())
            {
                ScreenNotification.ShowNotification("Please update it from the settings window.", duration: 10);
                ScreenNotification.ShowNotification("A new version of the event file is available.", duration: 10);

                lock (_lockObject)
                {
                    _notified = true;
                }
            }
        }

        private bool ExternalFileExists()
        {
            try
            {
                return File.Exists(this.FilePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Check for existing external file failed: {ex.Message}");
                throw ex;
            }
        }

        private async Task<string> GetInternalFileContent()
        {
            using StreamReader eventsReader = new StreamReader(this.ContentsManager.GetFileStream("events.json"));
            string json = await eventsReader.ReadToEndAsync();
            return json;
        }

        public async Task<string> GetExternalFileContent()
        {
            using StreamReader eventsReader = new StreamReader(this.FilePath);
            string json = await eventsReader.ReadToEndAsync();
            return json;
        }

        private async Task<bool> IsNewFileVersionAvaiable()
        {
            try
            {
                EventSettingsFile internalEventFile = JsonConvert.DeserializeObject<EventSettingsFile>(await this.GetInternalFileContent());
                EventSettingsFile externalEventFile = JsonConvert.DeserializeObject<EventSettingsFile>(await this.GetExternalFileContent());

                return internalEventFile.Version > externalEventFile.Version;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check for new file version: {ex.Message}");
                return false;
            }
        }

        public async Task ExportFile()
        {
            string json = await this.GetInternalFileContent();
            File.WriteAllText(this.FilePath, json);
        }

    }
}
