using ECommons;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Artisan.Universalis
{
    public static class DataCenters
    {
        private const string Endpoint = "https://universalis.app/api/v2/";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);
        private static readonly object SyncRoot = new();
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        private static DateTimeOffset lastRefresh = DateTimeOffset.MinValue;
        private static Dictionary<uint, string> worldNames = new();
        private static Dictionary<uint, DataCenterMetadata> worldToDataCenter = new();
        private static Dictionary<string, Dictionary<string, List<uint[]>>> regionsByName = new(StringComparer.Ordinal);

        public static uint[]? GetDataCenterByWorld(uint world)
        {
            EnsureMetadata();
            return worldToDataCenter.TryGetValue(world, out var data)
                ? data.Worlds
                : null;
        }

        public static string? GetDataCenterName(uint world)
        {
            EnsureMetadata();
            return worldToDataCenter.TryGetValue(world, out var data)
                ? data.Name
                : null;
        }

        public static string? GetWorldName(uint world)
        {
            EnsureMetadata();
            if (worldNames.TryGetValue(world, out var name))
                return name;

            var fallback = Svc.Data.GetExcelSheet<World>()?.FirstOrDefault(x => x.RowId == world).Name;
            return fallback?.ExtractText();
        }

        internal static string? GetRegionNameByWorld(uint world)
        {
            EnsureMetadata();
            return worldToDataCenter.TryGetValue(world, out var data)
                ? data.Region
                : null;
        }

        private static void EnsureMetadata()
        {
            if (DateTimeOffset.UtcNow - lastRefresh < CacheDuration && worldToDataCenter.Count > 0)
                return;

            lock (SyncRoot)
            {
                if (DateTimeOffset.UtcNow - lastRefresh < CacheDuration && worldToDataCenter.Count > 0)
                    return;

                try
                {
                    var worldsResponse = HttpClient.GetStringAsync($"{Endpoint}worlds").GetAwaiter().GetResult();
                    var dataCentersResponse = HttpClient.GetStringAsync($"{Endpoint}data-centers").GetAwaiter().GetResult();

                    var worlds = JsonConvert.DeserializeObject<List<WorldEntry>>(worldsResponse) ?? new();
                    var dataCenters = JsonConvert.DeserializeObject<List<DataCenterEntry>>(dataCentersResponse) ?? new();

                    var nextWorldNames = worlds
                        .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Name))
                        .GroupBy(x => x.Id)
                        .ToDictionary(x => x.Key, x => x.Last().Name!, EqualityComparer<uint>.Default);

                    var nextWorldToDataCenter = new Dictionary<uint, DataCenterMetadata>();
                    var nextRegions = new Dictionary<string, Dictionary<string, List<uint[]>>>(StringComparer.Ordinal);

                    foreach (var dataCenter in dataCenters.Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Region) && x.Worlds != null))
                    {
                        var worldsInDc = dataCenter.Worlds
                            .Where(x => x > 0)
                            .Distinct()
                            .ToArray();

                        if (worldsInDc.Length == 0)
                            continue;

                        var metadata = new DataCenterMetadata(dataCenter.Name!, dataCenter.Region!, worldsInDc);
                        foreach (var world in worldsInDc)
                            nextWorldToDataCenter[world] = metadata;

                        if (!nextRegions.TryGetValue(metadata.Region, out var region))
                        {
                            region = new Dictionary<string, List<uint[]>>(StringComparer.Ordinal);
                            nextRegions[metadata.Region] = region;
                        }

                        region[metadata.Name] = new() { worldsInDc };
                    }

                    if (nextWorldToDataCenter.Count == 0)
                        return;

                    worldNames = nextWorldNames;
                    worldToDataCenter = nextWorldToDataCenter;
                    regionsByName = nextRegions;
                    lastRefresh = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        private sealed record DataCenterMetadata(string Name, string Region, uint[] Worlds);

        private sealed class WorldEntry
        {
            [JsonProperty("id")]
            public uint Id { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }
        }

        private sealed class DataCenterEntry
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("region")]
            public string? Region { get; set; }

            [JsonProperty("worlds")]
            public uint[]? Worlds { get; set; }
        }

        internal static Dictionary<string, List<uint[]>>? GetRegionData(string region)
        {
            EnsureMetadata();
            return regionsByName.TryGetValue(region, out var data)
                ? data
                : null;
        }
    }

    public static class Regions
    {
        public static string? GetRegionByWorld(uint world)
            => DataCenters.GetRegionNameByWorld(world);

        public static Dictionary<string, List<uint[]>>? GetRegionByString(string region)
            => DataCenters.GetRegionData(region);
    }
}
