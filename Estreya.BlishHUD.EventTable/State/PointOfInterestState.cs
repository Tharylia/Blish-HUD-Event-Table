namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Helpers;
using Estreya.BlishHUD.EventTable.Utils;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PointOfInterestState : ManagedState
{
    private static readonly Logger Logger = Logger.GetLogger<PointOfInterestState>();

    private readonly Gw2ApiManager _apiManager;

    private AsyncLock _pointOfInterestsLock = new AsyncLock();

    public bool Loading { get; private set; }

    public List<ContinentFloorRegionMapPoi> PointOfInterests { get; } = new List<ContinentFloorRegionMapPoi>();

    public PointOfInterestState(Gw2ApiManager apiManager) : base(false)
    {
        this._apiManager = apiManager;
    }

    public override Task Clear()
    {
        using (this._pointOfInterestsLock.Lock())
        {
            this.PointOfInterests.Clear();
        }

        return Task.CompletedTask;
    }

    public override async Task InternalReload()
    {
        await this.Clear();
        await this.Load();
    }

    protected override Task Initialize()
    {
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        AsyncHelper.RunSync(this.Clear);
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
    }

    protected override async Task Load()
    {
        await this.LoadPointOfInterests();
    }

    protected override Task Save()
    {
        return Task.CompletedTask;
    }

    public ContinentFloorRegionMapPoi GetPointOfInterest(string chatCode)
    {
        using (this._pointOfInterestsLock.Lock())
        {
            IEnumerable<ContinentFloorRegionMapPoi> foundPointOfInterests = this.PointOfInterests.Where(wp => wp.ChatLink == chatCode);

            return foundPointOfInterests.Any() ? foundPointOfInterests.First() : null;
        }
    }

    private async Task LoadPointOfInterests()
    {
        using (await this._pointOfInterestsLock.LockAsync())
        {
            try
            {
                this.Loading = true;
                // Continent 1 = Tyria
                // Continent 2 = Mists
                // Fetching a single floor will return all nested subresources as well, so fetch all floors
                Gw2Sharp.WebApi.V2.IApiV2ObjectList<int> floors = await this._apiManager.Gw2ApiClient.V2.Continents[1].Floors.IdsAsync();

                foreach (int floorId in floors)
                {
                    //progress(string.Format(Strings.Common.SearchHandler_Landmarks_FloorLoading, floorId));
                    ContinentFloor floor = await this._apiManager.Gw2ApiClient.V2.Continents[1].Floors[floorId].GetAsync();
                    foreach (ContinentFloorRegion region in floor.Regions.Values)
                    {
                        foreach (ContinentFloorRegionMap map in region.Maps.Values)
                        {
                            foreach (ContinentFloorRegionMapPoi waypoint in map.PointsOfInterest.Values.Where(poi => poi.Name != null))
                            {
                                this.PointOfInterests.Add(waypoint);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not load point of interests:");   
            }
            finally
            {
                this.Loading = false;
            }
        }
    }
}
