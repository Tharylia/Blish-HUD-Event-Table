namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class HiddenState : ManagedState
    {
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
            lock (Instances)
            {
                Instances.Clear();
            }

            await Load();
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            lock (Instances)
            {
                for (int i = Instances.Count - 1; i >= 0; i--)
                {
                    var instance = Instances.ElementAt(i);
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
            lock (Instances)
            {
                if (Instances.ContainsKey(name))
                {
                    Instances.Remove(name);
                }

                if (!isUTC)
                {
                    hideUntil = hideUntil.ToUniversalTime();
                }

                Instances.Add(name, hideUntil);
                dirty = true;
            }
        }

        public void Remove(string name)
        {
            lock (Instances)
            {
                if (!Instances.ContainsKey(name)) return;

                Instances.Remove(name);
                dirty = true;
            }
        }

        public bool IsHidden(string name)
        {
            lock (Instances)
            {
                return Instances.ContainsKey(name);
            }
        }

        protected override  Task Initialize()
        {
            return Task.CompletedTask;
        }

        protected override async Task Load()
        {
            if (!File.Exists(this.Path)) return;

            string[] lines = await FileUtil.ReadLinesAsync(this.Path);

            if (lines == null ||  lines.Length == 0) return;

            lock (Instances)
            {
                foreach (string line in lines)
                {
                    var parts = line.Split(new[] { LINE_SPLIT }, StringSplitOptions.None);

                    var name = parts[0];
                    DateTime hiddenUntil = DateTime.Parse(parts[1]);

                    Instances.Add(name, hiddenUntil);
                }
            }
        }

        protected override async Task Save()
        {
            if (!dirty) return;

            Collection<string> lines = new Collection<string>();

            lock (Instances)
            {
                foreach (var instance in Instances)
                {
                    lines.Add($"{instance.Key}{LINE_SPLIT}{instance.Value}");
                }
            }

            await FileUtil.WriteLinesAsync(this.Path, lines.ToArray());
            dirty = false;
        }

        protected override Task InternalUnload()
        {
            return Task.CompletedTask;
        }
    }
}
