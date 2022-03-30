namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Utils;
    using Gw2Sharp.WebApi.Exceptions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class WorldbossState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<WorldbossState>();
        private Gw2ApiManager ApiManager { get; set; }
        private TimeSpan updateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
        private double timeSinceUpdate = 0;
        private List<string> completedWorldbosses = new List<string>();

        public event EventHandler<string> WorldbossCompleted;

        public WorldbossState(Gw2ApiManager apiManager)
        {
            this.ApiManager = apiManager;
        }

        private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
        {
            Task.Run(async () => await this.Reload());
        }

        public bool IsCompleted(string apiCode)
        {
            return this.completedWorldbosses.Contains(apiCode);
        }

        public override async Task Reload()
        {
            await this.UpdateCompletedWorldbosses(null);
        }

        private async Task UpdateCompletedWorldbosses(GameTime gameTime)
        {
            Logger.Info($"Check for completed worldbosses.");
            try
            {
                List<string> oldCompletedWorldbosses;
                lock (this.completedWorldbosses)
                {
                    oldCompletedWorldbosses = this.completedWorldbosses.ToArray().ToList();
                    this.completedWorldbosses.Clear();
                }

                if (this.ApiManager.HasPermissions(new[] { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression }))
                {
                    Gw2Sharp.WebApi.V2.IApiV2ObjectList<string> bosses = await this.ApiManager.Gw2ApiClient.V2.Account.WorldBosses.GetAsync();
                    lock (this.completedWorldbosses)
                    {
                        this.completedWorldbosses.AddRange(bosses);
                    }

                    foreach (string boss in bosses)
                    {
                        if (!oldCompletedWorldbosses.Contains(boss))
                        {
                            Logger.Info($"Completed worldboss: {boss}");
                            try
                            {
                                this.WorldbossCompleted?.Invoke(this, boss);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Error handling complete worldboss event: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (MissingScopesException msex)
            {
                Logger.Warn($"Could not update completed worldbosses due to missing scopes: {msex.Message}");
            }
            catch (InvalidAccessTokenException iatex)
            {
                Logger.Warn($"Could not update completed worldbosses due to invalid access token: {iatex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating completed worldbosses: {ex.Message}");
            }
        }

        protected override Task Initialize()
        {
            this.ApiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
            return Task.CompletedTask;
        }

        protected override void InternalUnload()
        {
            this.ApiManager.SubtokenUpdated -= this.ApiManager_SubtokenUpdated;

            lock (this.completedWorldbosses)
            {
                this.completedWorldbosses.Clear();
            }
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            UpdateCadenceUtil.UpdateAsyncWithCadence(this.UpdateCompletedWorldbosses, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
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
