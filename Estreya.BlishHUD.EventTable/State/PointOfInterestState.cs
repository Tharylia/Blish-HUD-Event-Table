namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Extensions;
using Estreya.BlishHUD.EventTable.Helpers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.Models.GW2API;
using Estreya.BlishHUD.EventTable.Utils;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class PointOfInterestState : ManagedState
{
    private static readonly Logger Logger = Logger.GetLogger<PointOfInterestState>();
    private const string BASE_FOLDER_STRUCTURE = "pois";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private readonly Gw2ApiManager _apiManager;
    private readonly string _baseFolderPath;
    private AsyncLock _pointOfInterestsLock = new AsyncLock();

    private string FullPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public bool Loading { get; private set; }

    public List<PointOfInterest> PointOfInterests { get; } = new List<PointOfInterest>();

    public PointOfInterestState(Gw2ApiManager apiManager, string baseFolderPath) : base(true, -1) // Don't save in interval, must be manually triggered
    {
        this._apiManager = apiManager;
        this._baseFolderPath = baseFolderPath;
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
        bool loadFromApi = false;

        if (Directory.Exists(this.FullPath))
        {
            bool continueLoadingFiles = true;

            string lastUpdatedFilePath = Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME);
            if (!System.IO.File.Exists(lastUpdatedFilePath))
            {
                await this.CreateLastUpdatedFile();
            }

            string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
            if (!DateTime.TryParse(dateString, out DateTime lastUpdated))
            {
                Logger.Debug("Failed parsing last updated.");
            }
            else
            {
                if (DateTime.UtcNow - new DateTime(lastUpdated.Ticks, DateTimeKind.Utc) > TimeSpan.FromDays(5))
                {
                    continueLoadingFiles = false;
                    loadFromApi = true;
                }
            }

            if (continueLoadingFiles)
            {
                string[] files = Directory.GetFiles(this.FullPath, "*.*", SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                    await this.LoadFromFiles(files);
                }
                else
                {
                    loadFromApi = true;
                }
            }
        }
        else
        {
            loadFromApi = true;
        }

        if (loadFromApi)
        {
            await this.LoadFromAPI();
            await this.Save();
        }

        Logger.Debug("Loaded {0} point of interests.", this.PointOfInterests.Count);
    }

    private async Task LoadFromFiles(string[] files)
    {
        List<Task<string>> loadTasks = files.ToList().Select(file =>
        {
            if (!System.IO.File.Exists(file))
            {
                Logger.Warn("Could not find file \"{0}\"", file);
                return Task.FromResult((string)null);
            }

            return FileUtil.ReadStringAsync(file);
        }).ToList();

        _ = await Task.WhenAll(loadTasks);

        using (await this._pointOfInterestsLock.LockAsync())
        {
            foreach (Task<string> loadTask in loadTasks)
            {
                string result = loadTask.Result;

                if (string.IsNullOrWhiteSpace(result))
                {
                    continue;
                }

                PointOfInterest poi = JsonConvert.DeserializeObject<PointOfInterest>(result);

                this.PointOfInterests.Add(poi);
            }
        }
    }

    protected override async Task Save()
    {
        if (Directory.Exists(this.FullPath))
        {
            Directory.Delete(this.FullPath, true);
        }

        _ = Directory.CreateDirectory(this.FullPath);

        using (await this._pointOfInterestsLock.LockAsync())
        {
            IEnumerable<Task> fileWriteTasks = this.PointOfInterests.Select(poi =>
            {
                string landmarkPath = Path.Combine(this.FullPath, FileUtil.SanitizeFileName(poi.Continent.Name), FileUtil.SanitizeFileName(poi.Floor.Id.ToString()), FileUtil.SanitizeFileName(poi.Region.Name), FileUtil.SanitizeFileName(poi.Map.Name), FileUtil.SanitizeFileName(poi.Name) + ".txt");

                Directory.CreateDirectory(Path.GetDirectoryName(landmarkPath));

                string landmarkData = JsonConvert.SerializeObject(poi, Formatting.Indented);

                return FileUtil.WriteStringAsync(landmarkPath, landmarkData);
            });

            await Task.WhenAll(fileWriteTasks);
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString());
    }

    public PointOfInterest GetPointOfInterest(string chatCode)
    {
        using (this._pointOfInterestsLock.Lock())
        {
            IEnumerable<PointOfInterest> foundPointOfInterests = this.PointOfInterests.Where(wp => wp.ChatLink == chatCode);

            return foundPointOfInterests.Any() ? foundPointOfInterests.First() : null;
        }
    }

    private async Task LoadFromAPI()
    {
        try
        {
            this.Loading = true;

            await this.Clear();

            using (await this._pointOfInterestsLock.LockAsync())
            {
                List<PointOfInterest> pointOfInterests = new List<PointOfInterest>();

                // Continent 1 = Tyria
                // Continent 2 = Mists

                Gw2Sharp.WebApi.V2.IApiV2ObjectList<Continent> continents = await this._apiManager.Gw2ApiClient.V2.Continents.AllAsync();

                foreach (ContinentDetails continent in continents.Select(x => new ContinentDetails(x)))
                {
                    Gw2Sharp.WebApi.V2.IApiV2ObjectList<ContinentFloor> floors = await this._apiManager.Gw2ApiClient.V2.Continents[continent.Id].Floors.AllAsync();

                    foreach (ContinentFloor floor in floors)
                    {
                        ContinentFloorDetails floorDetails = new ContinentFloorDetails(floor);

                        foreach (ContinentFloorRegion region in floor.Regions.Values)
                        {
                            ContinentFloorRegionDetails regionDetails = new ContinentFloorRegionDetails(region);

                            foreach (ContinentFloorRegionMap map in region.Maps.Values)
                            {
                                ContinentFloorRegionMapDetails mapDetails = new ContinentFloorRegionMapDetails(map);

                                foreach (ContinentFloorRegionMapPoi pointOfInterest in map.PointsOfInterest.Values.Where(poi => poi.Name != null))
                                {
                                    PointOfInterest landmark = new PointOfInterest(pointOfInterest)
                                    {
                                        Continent = continent,
                                        Floor = floorDetails,
                                        Region = regionDetails,
                                        Map = mapDetails
                                    };

                                    pointOfInterests.Add(landmark);
                                }
                            }
                        }
                    }
                }

                this.PointOfInterests.AddRange(pointOfInterests.DistinctBy(poi => new { poi.Name })); // POIS can exists on multiple levels with the same data
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
