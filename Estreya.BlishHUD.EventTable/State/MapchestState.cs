namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Utils;
    using Gw2Sharp.WebApi.Exceptions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MapchestState : ManagedState
    {
        private static readonly Logger Logger = Logger.GetLogger<MapchestState>();
        private Gw2ApiManager ApiManager { get; set; }
        private TimeSpan updateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
        private double timeSinceUpdate = 0;
        private List<string> completedMapchests = new List<string>();

        public event EventHandler<string> MapchestCompleted;

        public MapchestState(Gw2ApiManager apiManager)
        {
            this.ApiManager = apiManager;
        }

        private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
        {
            Task.Run(async () => await this.Reload());
        }

        public bool IsCompleted(string apiCode)
        {
            return this.completedMapchests.Contains(apiCode);
        }

        public override async Task InternalReload()
        {
            await this.UpdatedCompletedMapchests(null);
        }

        private async Task UpdatedCompletedMapchests(GameTime gameTime)
        {
            Logger.Info($"Check for completed mapchests.");
            try
            {
                List<string> oldCompletedMapchests;
                lock (this.completedMapchests)
                {
                    oldCompletedMapchests = this.completedMapchests.ToArray().ToList();
                    this.completedMapchests.Clear();
                }

                if (this.ApiManager.HasPermissions(new[] { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression }))
                {
                    Gw2Sharp.WebApi.V2.IApiV2ObjectList<string> mapchests = await this.ApiManager.Gw2ApiClient.V2.Account.MapChests.GetAsync();
                    lock (this.completedMapchests)
                    {
                        this.completedMapchests.AddRange(mapchests);
                    }

                    foreach (string mapchest in mapchests)
                    {
                        if (!oldCompletedMapchests.Contains(mapchest))
                        {
                            Logger.Info($"Completed mapchest: {mapchest}");
                            try
                            {
                                this.MapchestCompleted?.Invoke(this, mapchest);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Error handling complete mapchest event: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (MissingScopesException msex)
            {
                Logger.Warn($"Could not update completed mapchests due to missing scopes: {msex.Message}");
            }
            catch (InvalidAccessTokenException iatex)
            {
                Logger.Warn($"Could not update completed mapchests due to invalid access token: {iatex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating completed mapchests: {ex.Message}");
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

            AsyncHelper.RunSync(this.Clear);
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            UpdateUtil.UpdateAsync(this.UpdatedCompletedMapchests, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
        }

        protected override async Task Load()
        {
            await this.UpdatedCompletedMapchests(null);
        }

        protected override Task Save()
        {
            return Task.CompletedTask;
        }

        public override Task Clear()
        {
            lock (this.completedMapchests)
            {
                this.completedMapchests.Clear();
            }

            return Task.CompletedTask;
        }
    }
}
