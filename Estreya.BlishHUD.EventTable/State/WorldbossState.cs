namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class WorldbossState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<WorldbossState>();
        private Gw2ApiManager ApiManager { get; set; }
        private TimeSpan updateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
        private double timeSinceUpdate = 0;
        private List<string> completedWorldbosses = new List<string>();

        public WorldbossState(Gw2ApiManager apiManager)
        {
            this.ApiManager = apiManager;
            this.ApiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
        }

        private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
        {
            Task.Run(async () => await this.Reload());
        }

        public bool IsCompleted(string apiCode)
        {
            return completedWorldbosses.Contains(apiCode);
        }

        public override async Task Reload()
        {
            await this.UpdateCompletedWorldbosses(null);
        }

        private async Task UpdateCompletedWorldbosses(GameTime gameTime)
        {
            try
            {
                lock (this.completedWorldbosses)
                {
                    this.completedWorldbosses.Clear();
                }

                if (this.ApiManager.HasPermissions(new[] { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression }))
                {
                    Gw2Sharp.WebApi.V2.IApiV2ObjectList<string> bosses = await this.ApiManager.Gw2ApiClient.V2.Account.WorldBosses.GetAsync();
                    lock (this.completedWorldbosses)
                    {
                        this.completedWorldbosses.AddRange(bosses);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating completed worldbosses: {ex.Message}");
            }
        }

        protected override Task Initialize()
        {
            return Task.CompletedTask;
        }

        protected override Task InternalUnload()
        {
            lock (this.completedWorldbosses)
            {
                completedWorldbosses.Clear();
            }

            return Task.CompletedTask;
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            UpdateCadenceUtil.UpdateAsyncWithCadence(UpdateCompletedWorldbosses, gameTime, updateInterval.TotalMilliseconds, ref timeSinceUpdate);
        }

        protected override async Task Load()
        {
            await this.UpdateCompletedWorldbosses(null);
        }

        protected override Task Save()
        {
            return Task.CompletedTask;
        }
    }
}
