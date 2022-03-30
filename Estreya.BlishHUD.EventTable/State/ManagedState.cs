namespace Estreya.BlishHUD.EventTable.State
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public abstract class ManagedState : IDisposable
    {
        private int SaveInternal { get; set; }

        private TimeSpan TimeSinceSave { get; set; } = TimeSpan.Zero;

        public bool Running { get; private set; } = false;


        protected ManagedState(int saveInterval = 60000)
        {
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

        protected abstract void InternalUnload();

        protected abstract Task Initialize();

        protected abstract void InternalUpdate(GameTime gameTime);

        protected abstract Task Save();
        protected abstract Task Load();

        public void Dispose()
        {
            this.Stop();
            this.InternalUnload();
        }
    }
}
