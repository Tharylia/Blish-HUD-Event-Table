namespace Estreya.BlishHUD.EventTable.State
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    internal abstract class ManagedState : IDisposable
    {
        protected string BasePath { get; private set; }
        protected string FileName { get; private set; }
        private int SaveInternal { get; set; }

        private TimeSpan TimeSinceSave { get; set; } = TimeSpan.Zero;

        private string _path;

        protected string Path
        {
            get
            {
                if (this._path == null)
                {
                    this._path = System.IO.Path.Combine(this.BasePath, this.FileName);
                }

                return this._path;
            }
        }

        public bool Running { get; private set; } = false;


        protected ManagedState(string basePath, string fileName, int saveInterval = 60000)
        {
            this.BasePath = basePath;
            this.FileName = fileName;
            this.SaveInternal = saveInterval;
        }

        public async Task Start()
        {
            if (this.Running)
            {
                return;
            }

            await this.Initialize();
            await this.Load();

            this.Running = true;
        }

        public void Stop()
        {
            if (!this.Running)
            {
                return;
            }

            this.Running = false;
        }

        public void Update(GameTime gameTime)
        {
            if (!this.Running)
            {
                return;
            }

            this.TimeSinceSave += gameTime.ElapsedGameTime;

            if (this.TimeSinceSave.TotalMilliseconds >= this.SaveInternal)
            {
                Task.Run(async () =>
                {
                    await this.Save();
                    this.TimeSinceSave = TimeSpan.Zero;
                });
            }

            this.InternalUpdate(gameTime);
        }

        public abstract Task Reload();

        public async Task Unload()
        {
            await this.Save();
        }

        protected abstract Task InternalUnload();

        protected abstract Task Initialize();

        protected abstract void InternalUpdate(GameTime gameTime);

        protected abstract Task Save();
        protected abstract Task Load();


        public void Dispose()
        {
            this.Stop();
        }
    }
}
