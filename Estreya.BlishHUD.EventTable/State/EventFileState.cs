namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class EventFileState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<EventFileState>();
        private TimeSpan updateInterval = TimeSpan.FromHours(1);
        private double timeSinceUpdate = 0;

        private static object _lockObject = new object();
        private string Directory { get; set; }
        private string FileName { get; set; }

        private string FilePath => Path.Combine(this.Directory, this.FileName);

        private bool _notified = false;

        private ContentsManager ContentsManager { get; set; }

        public EventFileState(ContentsManager contentsManager, string directory, string fileName)
        {
            this.ContentsManager = contentsManager;
            this.Directory = directory;
            this.FileName = fileName;
        }

        public override async Task InternalReload()
        {
            await this.CheckAndNotify(null);
        }

        protected override async Task Initialize()
        {
            if (!this.ExternalFileExists() || (EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile.Value && await this.IsNewFileVersionAvaiable()))
            {
                await this.ExportFile();
            }

            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        protected override void InternalUnload()
        {
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            UpdateUtil.UpdateAsync(this.CheckAndNotify, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
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
                if (this._notified)
                {
                    return;
                }
            }

            if (await this.IsNewFileVersionAvaiable())
            {
                ScreenNotification.ShowNotification("Please update it from the settings window.", duration: 10);
                ScreenNotification.ShowNotification("A new version of the event file is available.", duration: 10);

                lock (_lockObject)
                {
                    this._notified = true;
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
            using Stream stream = this.ContentsManager.GetFileStream("events.json");
            return await FileUtil.ReadStringAsync(stream);
        }

        private async Task<string> GetExternalFileContent()
        {
            return await FileUtil.ReadStringAsync(this.FilePath);
        }

        public async Task<EventSettingsFile> GetInternalFile()
        {
            try
            {
                string content = await this.GetInternalFileContent();
                return JsonConvert.DeserializeObject<EventSettingsFile>(content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load internal file.");
            }

            return null;
        }
        public async Task<EventSettingsFile> GetExternalFile()
        {
            try
            {
                string content = await this.GetExternalFileContent();
                return JsonConvert.DeserializeObject<EventSettingsFile>(content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load external file.");
            }

            return null;
        }

        private async Task<bool> IsNewFileVersionAvaiable()
        {
            try
            {
                EventSettingsFile internalEventFile = await this.GetInternalFile();
                EventSettingsFile externalEventFile = await this.GetExternalFile();

                return internalEventFile?.Version > externalEventFile?.Version;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check for new file version: {ex.Message}");
                return false;
            }
        }

        internal async Task ExportFile(EventSettingsFile eventSettingsFile)
        {
            string content = JsonConvert.SerializeObject(eventSettingsFile, Formatting.Indented);
            await FileUtil.WriteStringAsync(this.FilePath, content);
        }

        public async Task ExportFile()
        {
            EventSettingsFile eventSettingsFile = await this.GetInternalFile();
            await this.ExportFile(eventSettingsFile);
        }

        public override Task Clear()
        {
            lock (_lockObject)
            {
                this._notified = false;
            }

            return Task.CompletedTask;
        }
    }
}
