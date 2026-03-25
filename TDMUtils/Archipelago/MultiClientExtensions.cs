using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TDMUtils.Archipelago
{
    public static class MultiClientExtensions
    {
        public sealed class APGameData
        {
            public Dictionary<long, string> ItemIdToName { get; init; } = [];
            public Dictionary<string, long> ItemNameToId { get; init; } = [];
            public Dictionary<long, string> LocationIdToName { get; init; } = [];
            public Dictionary<string, long> LocationNameToId { get; init; } = [];
        }
        public sealed class APPlayerInfo
        {
            public int Team { get; set; }
            public int Slot { get; set; }
            public string Alias { get; set; } = "";
            public string Name { get; set; } = "";
            public string Game { get; set; } = "";

            public static APPlayerInfo FromPlayer(PlayerInfo info) => new()
            {
                Team = info.Team,
                Slot = info.Slot,
                Alias = info.Alias ?? "",
                Name = info.Name ?? "",
                Game = info.Game ?? ""
            };
        }

        public sealed class APItemInfo
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Game { get; set; } = "";
            public ItemFlags Flags { get; set; } = ItemFlags.None;

            public static APItemInfo FromItemInfo(ItemInfo info) => new()
            {
                Id = info.ItemId,
                Name = info.ItemName ?? "",
                DisplayName = info.ItemDisplayName ?? "",
                Game = info.ItemGame ?? "",
                Flags = info.Flags
            };
        }

        public sealed class APLocationInfo
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Game { get; set; } = "";

            public static APLocationInfo FromItemInfo(ItemInfo info) => new()
            {
                Id = info.LocationId,
                Name = info.LocationName ?? "",
                DisplayName = info.LocationDisplayName ?? "",
                Game = info.LocationGame ?? ""
            };
        }

        public abstract class SpoilerData
        {
            [Newtonsoft.Json.JsonIgnore]
            public APPlayerInfo SendingPlayer { get; set; } = new();
            [Newtonsoft.Json.JsonIgnore]
            public APPlayerInfo ReceivingPlayer { get; set; } = new();
            [Newtonsoft.Json.JsonIgnore]
            public APLocationInfo Location { get; set; } = new();
            public APItemInfo Item { get; set; } = new();

            [Newtonsoft.Json.JsonProperty]
            private APPlayerInfo? _sendingPlayer;
            [Newtonsoft.Json.JsonProperty]
            private APPlayerInfo? _recievingPlayer;
            [Newtonsoft.Json.JsonProperty]
            private long? _locationId;
            [Newtonsoft.Json.JsonProperty]
            private APLocationInfo? _foreignLocation;

            public void RebuildFromSerialize(APSeedPlayerData seedPlayerData, APPlayerInfo activePlayer)
            {
                SendingPlayer = _sendingPlayer ?? activePlayer;
                ReceivingPlayer = _recievingPlayer ?? activePlayer;
                Location = _foreignLocation ?? seedPlayerData.GetLocation(_locationId!.Value)!;
            }
            public void BuildSerilizeValues(PlayerInfo activePlayer)
            {
                _sendingPlayer = SendingPlayer.Slot == activePlayer.Slot ? null : SendingPlayer;
                _recievingPlayer = ReceivingPlayer.Slot == activePlayer.Slot ? null : ReceivingPlayer;
                bool ForeignLocation = Location.Game != activePlayer.Game;
                _foreignLocation = ForeignLocation ? Location : null;
                _locationId = ForeignLocation ? null : Location.Id;
            }
        }

        public sealed class LocationScoutData : SpoilerData
        {
            public static LocationScoutData FromLocationScout(ScoutedItemInfo info, PlayerInfo activePlayer)
            {
                LocationScoutData Result = new()
                {
                    SendingPlayer = APPlayerInfo.FromPlayer(activePlayer),
                    ReceivingPlayer = APPlayerInfo.FromPlayer(info.Player),
                    Item = APItemInfo.FromItemInfo(info),
                    Location = APLocationInfo.FromItemInfo(info),
                };
                Result.BuildSerilizeValues(activePlayer);
                return Result;
            }
        }

        public sealed class RecievedItemData : SpoilerData
        {
            public static RecievedItemData FromRecievedItemData(ItemInfo info, PlayerInfo activePlayer)
            {
                RecievedItemData Result = new()
                {
                    SendingPlayer = APPlayerInfo.FromPlayer(info.Player),
                    ReceivingPlayer = APPlayerInfo.FromPlayer(activePlayer),
                    Item = APItemInfo.FromItemInfo(info),
                    Location = APLocationInfo.FromItemInfo(info),
                };
                Result.BuildSerilizeValues(activePlayer);
                return Result;
            }
        }

        public sealed class APClientRuntimeData
        {
            public List<long> CheckedLocations { get; set; } = [];
            public List<RecievedItemData> ReceivedItems { get; set; } = [];
            public ArchipelagoClientState ClientStatus { get; set; } = ArchipelagoClientState.ClientUnknown;
            public static async Task<APClientRuntimeData> FromSessionAsync(ArchipelagoSession session)
            {
                return new APClientRuntimeData
                {
                    CheckedLocations = [.. session.Locations.AllLocationsChecked],
                    ReceivedItems = [.. session.Items.AllItemsReceived.Select(x => RecievedItemData.FromRecievedItemData(x, session.Players.ActivePlayer))],
                    ClientStatus = await session.DataStorage.GetClientStatusAsync()
                };
            }
        }
        public sealed class APSeedPlayerData
        {
            public APPlayerInfo Player { get; set; } = new();
            public Dictionary<long, string> ItemIdToName { get; set; } = [];
            public Dictionary<string, object> SlotData { get; set; } = [];
            public List<LocationScoutData> Locations { get; set; } = [];

            [Newtonsoft.Json.JsonIgnore] 
            public Dictionary<string, long> ItemNameToId { get; private set; } = [];
            [Newtonsoft.Json.JsonIgnore] 
            public Dictionary<long, string> LocationIdToName { get; private set; } = [];
            [Newtonsoft.Json.JsonIgnore] 
            public Dictionary<string, long> LocationNameToId { get; private set; } = [];

            public static async Task<APSeedPlayerData> FromSessionAsync(ArchipelagoSession s, Dictionary<string, object>? d = null)
            {
                var loc = await s.Locations.ScoutLocationsAsync([.. s.Locations.AllLocations]);
                var x = new APSeedPlayerData
                {
                    Player = APPlayerInfo.FromPlayer(s.Players.ActivePlayer),
                    ItemIdToName = GetCurrentGameData(s).ItemIdToName,
                    SlotData = d ?? await s.DataStorage.GetSlotDataAsync(),
                    Locations = [.. loc.Values.Select(v => LocationScoutData.FromLocationScout(v, s.Players.ActivePlayer))]
                };
                x.RebuildCaches();
                return x;
            }

            public void RebuildCaches()
            {
                ItemNameToId = ItemIdToName.ToDictionary(x => x.Value, x => x.Key);
                LocationIdToName = Locations.Select(x => x.Location).GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Name);
                LocationNameToId = LocationIdToName.ToDictionary(x => x.Value, x => x.Key);
            }

            [OnDeserialized] 
            void OnDeserialized(StreamingContext _) => RebuildCaches();

            public APLocationInfo? GetLocation(long id) => Locations.FirstOrDefault(x => x.Location.Id == id)?.Location;
        }

        public static APGameData GetCurrentGameData(ArchipelagoSession session)
        {
            object?[] args = [null];
            object cache = ReflectionTools.GetMemberValue(
                ReflectionTools.GetMemberValue(session.Locations, "itemInfoResolver")!,
                "cache")!;

            if (!(bool)ReflectionTools.GetMethodInfo(cache.GetType(), "TryGetDataPackageFromCache")!.Invoke(cache, args)! || args[0] == null)
                throw new Exception("Failed to get datapackage.");

            object gameData = ((System.Collections.IDictionary)args[0]!)[session.ConnectionInfo.Game]
                ?? throw new Exception($"No datapackage found for game '{session.ConnectionInfo.Game}'.");

            return new APGameData
            {
                ItemNameToId = ToNameToIdDict(ReflectionTools.GetMemberValue(gameData, "Items")!),
                LocationNameToId = ToNameToIdDict(ReflectionTools.GetMemberValue(gameData, "Locations")!),
                ItemIdToName = ToIdToNameDict(ReflectionTools.GetMemberValue(gameData, "Items")!),
                LocationIdToName = ToIdToNameDict(ReflectionTools.GetMemberValue(gameData, "Locations")!)
            };
        }

        private static Dictionary<string, long> ToNameToIdDict(object lookup) =>
            ((System.Collections.IEnumerable)lookup).Cast<object>().ToDictionary(
                x => ReflectionTools.GetPropertyValue<string>(x, "Key")!,
                x => ReflectionTools.GetPropertyValue<long>(x, "Value"));

        private static Dictionary<long, string> ToIdToNameDict(object lookup) =>
            ((System.Collections.IEnumerable)lookup).Cast<object>().ToDictionary(
                x => ReflectionTools.GetPropertyValue<long>(x, "Value"),
                x => ReflectionTools.GetPropertyValue<string>(x, "Key")!);


    }
    public static class APDataPackageCache
    {
        static readonly string CacheFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Archipelago",
                "Cache",
                "datapackage");

        public static bool TryGetCachedGameData(string game, string checksum, out GameData gameData)
        {
            string folderPath = Path.Combine(CacheFolder, MakeSafeFileName(game));
            string filePath = Path.Combine(folderPath, $"{MakeSafeFileName(checksum)}.json");

            if (!File.Exists(filePath))
            {
                gameData = null!;
                return false;
            }

            try
            {
                gameData = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(filePath))!;
                return gameData != null && string.Equals(gameData.Checksum, checksum, StringComparison.Ordinal);
            }
            catch
            {
                gameData = null!;
                return false;
            }
        }

        public static async Task<GameData?> GetGameData(string game, string checksum, Func<string, string, Task<GameData?>> getter)
        {
            if (TryGetCachedGameData(game, checksum, out var cached))
                return cached;

            var gameData = await getter(game, checksum);

            if (gameData != null)
                SaveCachedGameData(game, gameData);

            return gameData;
        }

        public static void SaveCachedGameData(string game, GameData gameData)
        {
            string folderPath = Path.Combine(CacheFolder, MakeSafeFileName(game));
            string filePath = Path.Combine(folderPath, $"{MakeSafeFileName(gameData.Checksum)}.json");

            Directory.CreateDirectory(folderPath);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(gameData));
        }

        static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c.ToString(), string.Empty);
            return value;
        }
    }
}
