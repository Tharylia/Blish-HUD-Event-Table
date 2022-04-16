namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class HiddenState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<HiddenState>();
        private const string FILE_NAME = "hidden.txt";
        private const string LINE_SPLIT = "<-->";
        private bool dirty;

        private string BasePath { get; set; }

        private string _path;

        private string Path
        {
            get
            {
                if (this._path == null)
                {
                    this._path = System.IO.Path.Combine(this.BasePath, FILE_NAME);
                }

                return this._path;
            }
        }

        private Dictionary<string, DateTime> Instances { get; set; } = new Dictionary<string, DateTime>();

        public HiddenState(string basePath) : base(30000)
        {
            this.BasePath = basePath;
        }

        public override async Task Reload()
        {
            lock (this.Instances)
            {
                this.Instances.Clear();
            }

            await this.Load();
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            lock (this.Instances)
            {
                for (int i = this.Instances.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<string, DateTime> instance = this.Instances.ElementAt(i);
                    string name = instance.Key;
                    DateTime hiddenUntil = instance.Value;

                    bool remove = now >= hiddenUntil;

                    if (remove)
                    {
                        this.Remove(name);
                    }
                }
            }
        }

        public void Add(string name, DateTime hideUntil, bool isUTC)
        {
            lock (this.Instances)
            {
                if (this.Instances.ContainsKey(name))
                {
                    this.Instances.Remove(name);
                }

                if (!isUTC)
                {
                    hideUntil = hideUntil.ToUniversalTime();
                }

                Logger.Info($"Add hidden state for \"{name}\" until {hideUntil} UTC.");

                this.Instances.Add(name, hideUntil);
                this.dirty = true;
            }
        }

        public void Remove(string name)
        {
            lock (this.Instances)
            {
                if (!this.Instances.ContainsKey(name))
                {
                    return;
                }

                Logger.Info($"Remove hidden state for \"{name}\".");

                this.Instances.Remove(name);
                this.dirty = true;
            }
        }

        public override Task Clear()
        {
            lock (this.Instances)
            {
                Logger.Info($"Remove all hidden states.");

                this.Instances.Clear();
                this.dirty = true;
            }

            return Task.CompletedTask;
        }

        public bool IsHidden(string name)
        {
            lock (this.Instances)
            {
                return this.Instances.ContainsKey(name);
            }
        }

        protected override Task Initialize()
        {
            return Task.CompletedTask;
        }

        protected override async Task Load()
        {
            if (!File.Exists(this.Path))
            {
                return;
            }

            string[] lines = await FileUtil.ReadLinesAsync(this.Path);

            if (lines == null || lines.Length == 0)
            {
                return;
            }

            lock (this.Instances)
            {
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] { LINE_SPLIT }, StringSplitOptions.None);

                    string name = parts[0];
                    DateTime hiddenUntil = DateTime.Parse(parts[1]);

                    this.Instances.Add(name, hiddenUntil);
                }
            }
        }

        protected override async Task Save()
        {
            if (!this.dirty)
            {
                return;
            }

            Collection<string> lines = new Collection<string>();

            lock (this.Instances)
            {
                foreach (KeyValuePair<string, DateTime> instance in this.Instances)
                {
                    lines.Add($"{instance.Key}{LINE_SPLIT}{instance.Value}");
                }
            }

            await FileUtil.WriteLinesAsync(this.Path, lines.ToArray());
            this.dirty = false;
        }

        protected override void InternalUnload()
        {
            AsyncHelper.RunSync(this.Save);
            AsyncHelper.RunSync(this.Clear);
        }
    }
}
