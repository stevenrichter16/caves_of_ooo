using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    public interface ISaveSerializable
    {
        void Save(SaveWriter writer);
        void Load(SaveReader reader);
    }

    public sealed class SaveWriter
    {
        public const int Magic = 0x304F4F43; // COO0
        public const int FormatVersion = 2;

        private readonly BinaryWriter _writer;
        private readonly Dictionary<Entity, int> _entityTokens = new Dictionary<Entity, int>();
        private readonly List<Entity> _entityQueue = new List<Entity>();
        private int _nextEntityToken = 1;
        private int _bodyWriteIndex;

        public SaveWriter(Stream stream)
        {
            _writer = new BinaryWriter(stream);
        }

        public void WriteHeader(string gameVersion)
        {
            Write(Magic);
            Write(FormatVersion);
            WriteString(gameVersion);
        }

        public void WriteCheck(string name)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < name.Length; i++)
                    hash = hash * 31 + name[i];
                Write(hash);
            }
        }

        public void Write(int value) => _writer.Write(value);
        public void Write(long value) => _writer.Write(value);
        public void Write(float value) => _writer.Write(value);
        public void Write(double value) => _writer.Write(value);
        public void Write(bool value) => _writer.Write(value);
        public void Write(char value) => _writer.Write(value);

        public void WriteString(string value)
        {
            _writer.Write(value != null);
            if (value != null)
                _writer.Write(value);
        }

        public void WriteGuid(Guid value) => WriteString(value.ToString("N"));

        public void WriteColor(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        public void WriteEntityReference(Entity entity)
        {
            if (entity == null)
            {
                Write(0);
                return;
            }

            if (!_entityTokens.TryGetValue(entity, out int token))
            {
                token = _nextEntityToken++;
                _entityTokens[entity] = token;
                _entityQueue.Add(entity);
            }

            Write(token);
        }

        public void WriteQueuedEntityBodies()
        {
            WriteCheck("EntityBodies.Begin");
            while (_bodyWriteIndex < _entityQueue.Count)
            {
                Entity entity = _entityQueue[_bodyWriteIndex++];
                Write(_entityTokens[entity]);
                SaveGraphSerializer.SaveEntityBody(entity, this);
            }
            Write(0);
            WriteCheck("EntityBodies.End");
        }
    }

    public sealed class SaveReader
    {
        private readonly BinaryReader _reader;
        private readonly Dictionary<int, Entity> _entityTokens = new Dictionary<int, Entity>();
        private readonly List<Entity> _loadedEntities = new List<Entity>();

        public readonly EntityFactory Factory;
        public OverworldZoneManager ZoneManager { get; private set; }

        public SaveReader(Stream stream, EntityFactory factory)
        {
            _reader = new BinaryReader(stream);
            Factory = factory;
        }

        public void ReadHeader()
        {
            int magic = ReadInt();
            if (magic != SaveWriter.Magic)
                throw new InvalidDataException("Save file has an invalid magic header.");

            int version = ReadInt();
            if (version != SaveWriter.FormatVersion)
                throw new InvalidDataException($"Unsupported save format version {version}.");

            ReadString();
        }

        public void ExpectCheck(string name)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < name.Length; i++)
                    hash = hash * 31 + name[i];

                int actual = ReadInt();
                if (actual != hash)
                    throw new InvalidDataException($"Save section check failed: {name}.");
            }
        }

        public int ReadInt() => _reader.ReadInt32();
        public long ReadLong() => _reader.ReadInt64();
        public float ReadFloat() => _reader.ReadSingle();
        public double ReadDouble() => _reader.ReadDouble();
        public bool ReadBool() => _reader.ReadBoolean();
        public char ReadChar() => _reader.ReadChar();

        public string ReadString()
        {
            return _reader.ReadBoolean() ? _reader.ReadString() : null;
        }

        public Guid ReadGuid()
        {
            string value = ReadString();
            return string.IsNullOrEmpty(value) ? Guid.Empty : new Guid(value);
        }

        public Color ReadColor()
        {
            return new Color(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }

        public Entity ReadEntityReference()
        {
            int token = ReadInt();
            if (token == 0)
                return null;

            if (!_entityTokens.TryGetValue(token, out Entity entity))
            {
                entity = new Entity();
                _entityTokens[token] = entity;
            }

            return entity;
        }

        public void ReadEntityBodies()
        {
            ExpectCheck("EntityBodies.Begin");
            while (true)
            {
                int token = ReadInt();
                if (token == 0)
                    break;

                if (!_entityTokens.TryGetValue(token, out Entity entity))
                {
                    entity = new Entity();
                    _entityTokens[token] = entity;
                }

                SaveGraphSerializer.LoadEntityBody(entity, this);
                _loadedEntities.Add(entity);
            }
            ExpectCheck("EntityBodies.End");

            for (int i = 0; i < _loadedEntities.Count; i++)
            {
                var parts = _loadedEntities[i].Parts;
                for (int p = 0; p < parts.Count; p++)
                    parts[p].OnAfterLoad(this);

                var effects = _loadedEntities[i].GetPart<StatusEffectsPart>()?.GetAllEffects();
                if (effects != null)
                {
                    for (int e = 0; e < effects.Count; e++)
                        effects[e].OnAfterLoad(this);
                }
            }

            for (int i = 0; i < _loadedEntities.Count; i++)
            {
                var parts = _loadedEntities[i].Parts;
                for (int p = 0; p < parts.Count; p++)
                    parts[p].FinalizeLoad(this);

                var effects = _loadedEntities[i].GetPart<StatusEffectsPart>()?.GetAllEffects();
                if (effects != null)
                {
                    for (int e = 0; e < effects.Count; e++)
                        effects[e].FinalizeLoad(this);
                }
            }
        }

        public void SetZoneManager(OverworldZoneManager zoneManager)
        {
            ZoneManager = zoneManager;
        }

        public Zone FindZone(string zoneID)
        {
            if (string.IsNullOrEmpty(zoneID) || ZoneManager == null)
                return null;
            ZoneManager.CachedZones.TryGetValue(zoneID, out Zone zone);
            return zone;
        }
    }

    [Serializable]
    public class SaveGameInfo
    {
        public int SaveVersion;
        public string GameVersion;
        public string GameID;
        public string PlayerName;
        public string PlayerDisplayName;
        public int PlayerLevel;
        public string ActiveZoneID;
        public string ActiveZoneDisplayLabel;
        public int Turn;
        public string SaveTimestampUtc;
        public string HPSummary;
        public string Glyph;
        public string Color;
    }

    public sealed class GameSessionState
    {
        public int SaveVersion = SaveWriter.FormatVersion;
        public string GameID;
        public string GameVersion;
        public int WorldSeed;
        public string ActiveZoneID;
        public Entity Player;
        public Entity World;
        public OverworldZoneManager ZoneManager;
        public TurnManager TurnManager;
        public int SelectedHotbarSlot;

        public static GameSessionState Capture(
            string gameID,
            string gameVersion,
            OverworldZoneManager zoneManager,
            TurnManager turnManager,
            Entity player,
            int selectedHotbarSlot = 0,
            Entity world = null)
        {
            return new GameSessionState
            {
                GameID = string.IsNullOrEmpty(gameID) ? Guid.NewGuid().ToString("N") : gameID,
                GameVersion = gameVersion ?? Application.version,
                WorldSeed = zoneManager != null ? zoneManager.WorldSeed : 0,
                ActiveZoneID = zoneManager?.ActiveZone?.ZoneID,
                Player = player,
                World = world,
                ZoneManager = zoneManager,
                TurnManager = turnManager,
                SelectedHotbarSlot = selectedHotbarSlot
            };
        }

        public void Save(SaveWriter writer)
        {
            writer.WriteHeader(GameVersion);
            writer.WriteCheck("GameSession.Begin");
            writer.Write(SaveVersion);
            writer.WriteString(GameID);
            writer.WriteString(GameVersion);
            writer.Write(WorldSeed);
            writer.WriteString(ActiveZoneID);
            writer.Write(SelectedHotbarSlot);

            writer.WriteCheck("Player");
            writer.WriteEntityReference(Player);

            writer.WriteCheck("World");
            writer.WriteEntityReference(World);

            writer.WriteCheck("ZoneManager");
            SaveGraphSerializer.SaveOverworldZoneManager(ZoneManager, writer);

            writer.WriteCheck("TurnManager");
            SaveGraphSerializer.SaveTurnManager(TurnManager, writer);

            writer.WriteCheck("MessageLog");
            SaveGraphSerializer.SaveMessageLog(writer);

            writer.WriteCheck("PlayerReputation");
            SaveGraphSerializer.SavePlayerReputation(writer);

            writer.WriteQueuedEntityBodies();
            writer.WriteCheck("GameSession.End");
        }

        public static GameSessionState Load(SaveReader reader)
        {
            reader.ReadHeader();
            reader.ExpectCheck("GameSession.Begin");

            var state = new GameSessionState();
            state.SaveVersion = reader.ReadInt();
            state.GameID = reader.ReadString();
            state.GameVersion = reader.ReadString();
            state.WorldSeed = reader.ReadInt();
            state.ActiveZoneID = reader.ReadString();
            state.SelectedHotbarSlot = reader.ReadInt();

            reader.ExpectCheck("Player");
            state.Player = reader.ReadEntityReference();

            reader.ExpectCheck("World");
            state.World = reader.ReadEntityReference();

            reader.ExpectCheck("ZoneManager");
            state.ZoneManager = SaveGraphSerializer.LoadOverworldZoneManager(reader);

            reader.ExpectCheck("TurnManager");
            state.TurnManager = SaveGraphSerializer.LoadTurnManager(reader);

            reader.ExpectCheck("MessageLog");
            SaveGraphSerializer.LoadMessageLog(reader);

            reader.ExpectCheck("PlayerReputation");
            SaveGraphSerializer.LoadPlayerReputation(reader);

            reader.ReadEntityBodies();
            SaveGraphSerializer.RebuildLoadedWorld(state);

            reader.ExpectCheck("GameSession.End");
            return state;
        }

        public SaveGameInfo CreateInfo()
        {
            string display = Player?.GetDisplayName() ?? "Player";
            Stat hp = Player?.GetStat("Hitpoints");
            return new SaveGameInfo
            {
                SaveVersion = SaveVersion,
                GameVersion = GameVersion,
                GameID = GameID,
                PlayerName = Player?.BlueprintName ?? "Player",
                PlayerDisplayName = display,
                PlayerLevel = Player?.GetStatValue("Level", 1) ?? 1,
                ActiveZoneID = ActiveZoneID,
                ActiveZoneDisplayLabel = ActiveZoneID,
                Turn = TurnManager?.TickCount ?? 0,
                SaveTimestampUtc = DateTime.UtcNow.ToString("O"),
                HPSummary = hp != null ? $"{hp.Value}/{hp.Max}" : "",
                Glyph = Player?.GetPart<RenderPart>()?.RenderString ?? "",
                Color = Player?.GetPart<RenderPart>()?.ColorString ?? ""
            };
        }
    }

    public static class SaveGameService
    {
        private const string PrimaryName = "Primary";
        private const string QuickName = "Quick";
        private const string DefaultGameID = "Default";

        private static Func<GameSessionState> _captureCurrent;
        private static Action<GameSessionState> _applyLoaded;
        private static string _activeGameID = DefaultGameID;

        public static void RegisterRuntime(Func<GameSessionState> captureCurrent, Action<GameSessionState> applyLoaded)
        {
            _captureCurrent = captureCurrent;
            _applyLoaded = applyLoaded;
        }

        public static void SetActiveGameID(string gameID)
        {
            if (!string.IsNullOrEmpty(gameID))
                _activeGameID = gameID;
        }

        public static bool SavePrimary() => SaveSlot(PrimaryName);
        public static bool QuickSave() => SaveSlot(QuickName);
        public static bool LoadPrimary() => LoadSlot(PrimaryName);
        public static bool QuickLoad() => LoadSlot(QuickName);
        public static bool HasPrimarySave() => HasSave(PrimaryName);
        public static bool HasQuickSave() => HasSave(QuickName);

        public static SaveGameInfo GetSaveInfo(string name)
        {
            string path = GetMetadataPath(name);
            if (!File.Exists(path))
                return null;
            return JsonUtility.FromJson<SaveGameInfo>(File.ReadAllText(path));
        }

        public static bool SaveSlot(string name)
        {
            if (_captureCurrent == null)
                return false;

            GameSessionState state = _captureCurrent();
            if (state == null)
                return false;

            if (string.IsNullOrEmpty(state.GameID))
                state.GameID = _activeGameID;
            _activeGameID = state.GameID;

            string savePath = GetSavePath(name, state.GameID);
            string metadataPath = GetMetadataPath(name, state.GameID);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            WriteSaveAtomically(savePath, stream =>
            {
                using (var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
                {
                    var writer = new SaveWriter(gzip);
                    state.Save(writer);
                }
            });

            WriteTextAtomically(metadataPath, JsonUtility.ToJson(state.CreateInfo(), prettyPrint: true));
            return true;
        }

        public static bool LoadSlot(string name)
        {
            if (_applyLoaded == null)
                return false;

            string path = GetSavePath(name);
            if (!File.Exists(path))
                return false;

            EntityFactory factory = _captureCurrent?.Invoke()?.ZoneManager?.Factory;
            GameSessionState state = LoadState(path, factory);
            _activeGameID = string.IsNullOrEmpty(state.GameID) ? _activeGameID : state.GameID;
            _applyLoaded(state);
            return true;
        }

        public static GameSessionState LoadState(string path, EntityFactory factory)
        {
            ValidateGzipHeader(path);
            using (var file = File.OpenRead(path))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                var reader = new SaveReader(gzip, factory);
                return GameSessionState.Load(reader);
            }
        }

        public static bool HasSave(string name)
        {
            return File.Exists(GetSavePath(name));
        }

        private static string GetSavePath(string name)
        {
            return GetSavePath(name, _activeGameID);
        }

        private static string GetMetadataPath(string name)
        {
            return GetMetadataPath(name, _activeGameID);
        }

        private static string GetSavePath(string name, string gameID)
        {
            return Path.Combine(Application.persistentDataPath, "Saves", gameID ?? DefaultGameID, name + ".sav.gz");
        }

        private static string GetMetadataPath(string name, string gameID)
        {
            return Path.Combine(Application.persistentDataPath, "Saves", gameID ?? DefaultGameID, name + ".json");
        }

        private static void WriteSaveAtomically(string path, Action<Stream> write)
        {
            string tmp = path + ".tmp";
            string bak = path + ".bak";

            try
            {
                using (var file = File.Create(tmp))
                    write(file);

                ValidateGzipHeader(tmp);

                if (File.Exists(path))
                {
                    if (File.Exists(bak))
                        File.Delete(bak);
                    File.Copy(path, bak);
                }

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmp, path);
                ValidateGzipHeader(path);
            }
            catch
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);

                if (File.Exists(bak))
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Copy(bak, path);
                }

                throw;
            }
        }

        private static void WriteTextAtomically(string path, string contents)
        {
            string tmp = path + ".tmp";
            string bak = path + ".bak";

            try
            {
                File.WriteAllText(tmp, contents ?? string.Empty);

                if (File.Exists(path))
                {
                    if (File.Exists(bak))
                        File.Delete(bak);
                    File.Copy(path, bak);
                }

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmp, path);
            }
            catch
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);

                if (File.Exists(bak))
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Copy(bak, path);
                }

                throw;
            }
        }

        private static void ValidateGzipHeader(string path)
        {
            using (var file = File.OpenRead(path))
            {
                if (file.Length < 2 || file.ReadByte() != 0x1f || file.ReadByte() != 0x8b)
                    throw new InvalidDataException("Save file is not gzip-compressed.");
            }
        }
    }

    public static class SaveGraphSerializer
    {
        public static void SaveEntityBody(Entity entity, SaveWriter writer)
        {
            writer.WriteCheck("Entity.Begin");
            writer.WriteString(entity.ID);
            writer.WriteString(entity.BlueprintName);
            WriteStringDictionary(entity.Tags, writer);
            WriteStringDictionary(entity.Properties, writer);
            WriteIntDictionary(entity.IntProperties, writer);

            writer.Write(entity.Statistics.Count);
            foreach (var kvp in entity.Statistics)
            {
                writer.WriteString(kvp.Key);
                SaveStat(kvp.Value, writer);
            }

            writer.Write(entity.Parts.Count);
            for (int i = 0; i < entity.Parts.Count; i++)
                SavePart(entity.Parts[i], writer);
            writer.WriteCheck("Entity.End");
        }

        public static void LoadEntityBody(Entity entity, SaveReader reader)
        {
            reader.ExpectCheck("Entity.Begin");
            entity.ID = reader.ReadString();
            entity.BlueprintName = reader.ReadString();
            entity.Tags = ReadStringDictionary(reader);
            entity.Properties = ReadStringDictionary(reader);
            entity.IntProperties = ReadIntDictionary(reader);

            entity.Statistics.Clear();
            int statCount = reader.ReadInt();
            for (int i = 0; i < statCount; i++)
            {
                string key = reader.ReadString();
                Stat stat = LoadStat(reader);
                stat.Owner = entity;
                entity.Statistics[key] = stat;
            }

            entity.Parts.Clear();
            int partCount = reader.ReadInt();
            for (int i = 0; i < partCount; i++)
            {
                Part part = LoadPart(reader);
                if (part == null)
                    continue;
                part.ParentEntity = entity;
                entity.Parts.Add(part);
            }
            reader.ExpectCheck("Entity.End");
        }

        public static void SaveOverworldZoneManager(OverworldZoneManager manager, SaveWriter writer)
        {
            writer.Write(manager != null);
            if (manager == null)
                return;

            writer.Write(manager.WorldSeed);
            writer.WriteString(manager.ActiveZone?.ZoneID);
            SaveWorldMap(manager.WorldMap, writer);

            writer.Write(manager.CachedZones.Count);
            foreach (var kvp in manager.CachedZones)
            {
                writer.WriteString(kvp.Key);
                SaveZone(kvp.Value, writer);
            }

            var connections = manager.GetConnectionSnapshot();
            writer.Write(connections.Count);
            foreach (var kvp in connections)
            {
                writer.WriteString(kvp.Key);
                writer.Write(kvp.Value.Count);
                for (int i = 0; i < kvp.Value.Count; i++)
                    SaveZoneConnection(kvp.Value[i], writer);
            }

            SaveSettlementManager(manager.SettlementManager, writer);
        }

        public static OverworldZoneManager LoadOverworldZoneManager(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            int seed = reader.ReadInt();
            string activeZoneID = reader.ReadString();
            WorldMap worldMap = LoadWorldMap(reader);

            int zoneCount = reader.ReadInt();
            var zones = new Dictionary<string, Zone>(zoneCount);
            for (int i = 0; i < zoneCount; i++)
            {
                string key = reader.ReadString();
                zones[key] = LoadZone(reader);
            }

            int connKeys = reader.ReadInt();
            var connections = new Dictionary<string, List<ZoneConnection>>(connKeys);
            for (int i = 0; i < connKeys; i++)
            {
                string key = reader.ReadString();
                int count = reader.ReadInt();
                var list = new List<ZoneConnection>(count);
                for (int c = 0; c < count; c++)
                    list.Add(LoadZoneConnection(reader));
                connections[key] = list;
            }

            SettlementManager settlements = LoadSettlementManager(reader);
            var manager = new OverworldZoneManager(reader.Factory, seed);
            manager.ReplaceLoadedOverworldState(worldMap, settlements, null);
            manager.ReplaceLoadedState(zones, activeZoneID, connections);
            reader.SetZoneManager(manager);
            return manager;
        }

        public static void RebuildLoadedWorld(GameSessionState state)
        {
            if (state.ZoneManager == null)
                return;

            foreach (var kvp in state.ZoneManager.CachedZones)
                kvp.Value.RebuildEntityCellsFromCells();
        }

        public static void SaveTurnManager(TurnManager turnManager, SaveWriter writer)
        {
            writer.Write(turnManager != null);
            if (turnManager == null)
                return;

            writer.Write(turnManager.TickCount);
            writer.Write(turnManager.WaitingForInput);
            writer.WriteEntityReference(turnManager.CurrentActor);

            List<TurnManager.SavedTurnEntry> entries = turnManager.GetSavedEntries();
            writer.Write(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                writer.WriteEntityReference(entries[i].Entity);
                writer.Write(entries[i].Energy);
            }
        }

        public static TurnManager LoadTurnManager(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            int tick = reader.ReadInt();
            bool waiting = reader.ReadBool();
            Entity current = reader.ReadEntityReference();
            int count = reader.ReadInt();
            var entries = new List<TurnManager.SavedTurnEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new TurnManager.SavedTurnEntry
                {
                    Entity = reader.ReadEntityReference(),
                    Energy = reader.ReadInt()
                });
            }

            var turnManager = new TurnManager();
            turnManager.RestoreSavedState(tick, waiting, current, entries);
            return turnManager;
        }

        public static void SaveMessageLog(SaveWriter writer)
        {
            List<MessageLog.Entry> entries = MessageLog.GetAllEntries();
            writer.Write(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                writer.WriteString(entries[i].Text);
                writer.Write(entries[i].Tick);
                writer.Write(entries[i].Serial);
            }

            List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            writer.Write(announcements.Count);
            for (int i = 0; i < announcements.Count; i++)
                writer.WriteString(announcements[i]);

            writer.Write(MessageLog.FlashStamp);
            writer.Write(MessageLog.NextSerialValue);
        }

        public static void LoadMessageLog(SaveReader reader)
        {
            int count = reader.ReadInt();
            var entries = new List<MessageLog.Entry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new MessageLog.Entry(
                    reader.ReadString(),
                    reader.ReadInt(),
                    reader.ReadInt()));
            }

            int announcementCount = reader.ReadInt();
            var announcements = new List<string>(announcementCount);
            for (int i = 0; i < announcementCount; i++)
                announcements.Add(reader.ReadString());

            int flash = reader.ReadInt();
            int nextSerial = reader.ReadInt();
            MessageLog.Restore(entries, announcements, flash, nextSerial);
        }

        public static void SavePlayerReputation(SaveWriter writer)
        {
            WriteIntDictionary(PlayerReputation.GetAll(), writer);
        }

        public static void LoadPlayerReputation(SaveReader reader)
        {
            PlayerReputation.Restore(ReadIntDictionary(reader));
        }

        private static void SaveZone(Zone zone, SaveWriter writer)
        {
            writer.WriteString(zone.ZoneID);
            writer.WriteColor(zone.AmbientTint);
            writer.Write(zone.EntityVersion);
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                    SaveCell(zone.Cells[x, y], writer);
            }
        }

        private static Zone LoadZone(SaveReader reader)
        {
            string zoneID = reader.ReadString();
            var zone = new Zone(zoneID);
            zone.AmbientTint = reader.ReadColor();
            int version = reader.ReadInt();
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                    LoadCell(zone.Cells[x, y], reader);
            }
            zone.SetEntityVersionForLoad(version);
            zone.RebuildEntityCellsFromCells();
            return zone;
        }

        private static void SaveCell(Cell cell, SaveWriter writer)
        {
            writer.Write(cell.Explored);
            writer.Write(cell.IsVisible);
            writer.Write(cell.IsInterior);
            writer.Write(cell.Objects.Count);
            for (int i = 0; i < cell.Objects.Count; i++)
                writer.WriteEntityReference(cell.Objects[i]);
        }

        private static void LoadCell(Cell cell, SaveReader reader)
        {
            cell.Explored = reader.ReadBool();
            cell.IsVisible = reader.ReadBool();
            cell.IsInterior = reader.ReadBool();
            cell.Objects.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
                cell.Objects.Add(reader.ReadEntityReference());
        }

        private static void SaveWorldMap(WorldMap map, SaveWriter writer)
        {
            writer.Write(map != null);
            if (map == null)
                return;

            writer.Write(map.Seed);
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    writer.Write((int)map.Tiles[x, y]);
                    SavePointOfInterest(map.POIs[x, y], writer);
                }
            }
        }

        private static WorldMap LoadWorldMap(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            var map = new WorldMap(reader.ReadInt());
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    map.Tiles[x, y] = (BiomeType)reader.ReadInt();
                    map.POIs[x, y] = LoadPointOfInterest(reader);
                }
            }
            return map;
        }

        private static void SavePointOfInterest(PointOfInterest poi, SaveWriter writer)
        {
            writer.Write(poi != null);
            if (poi == null)
                return;
            writer.Write((int)poi.Type);
            writer.WriteString(poi.Name);
            writer.WriteString(poi.Faction);
            writer.Write(poi.Tier);
            writer.WriteString(poi.BossBlueprint);
        }

        private static PointOfInterest LoadPointOfInterest(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;
            return new PointOfInterest(
                (POIType)reader.ReadInt(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadInt(),
                reader.ReadString());
        }

        private static void SaveZoneConnection(ZoneConnection conn, SaveWriter writer)
        {
            writer.WriteString(conn.SourceZoneID);
            writer.Write(conn.SourceX);
            writer.Write(conn.SourceY);
            writer.WriteString(conn.TargetZoneID);
            writer.Write(conn.TargetX);
            writer.Write(conn.TargetY);
            writer.WriteString(conn.Type);
        }

        private static ZoneConnection LoadZoneConnection(SaveReader reader)
        {
            return new ZoneConnection
            {
                SourceZoneID = reader.ReadString(),
                SourceX = reader.ReadInt(),
                SourceY = reader.ReadInt(),
                TargetZoneID = reader.ReadString(),
                TargetX = reader.ReadInt(),
                TargetY = reader.ReadInt(),
                Type = reader.ReadString()
            };
        }

        private static void SaveSettlementManager(SettlementManager manager, SaveWriter writer)
        {
            writer.Write(manager != null);
            if (manager == null)
                return;

            var settlements = manager.GetAllSettlementsSnapshot();
            writer.Write(settlements.Count);
            foreach (var kvp in settlements)
            {
                writer.WriteString(kvp.Key);
                SaveSettlementState(kvp.Value, writer);
            }
        }

        private static SettlementManager LoadSettlementManager(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            int count = reader.ReadInt();
            var settlements = new Dictionary<string, SettlementState>(count);
            for (int i = 0; i < count; i++)
                settlements[reader.ReadString()] = LoadSettlementState(reader);

            var manager = new SettlementManager();
            manager.RestoreSettlements(settlements);
            return manager;
        }

        private static void SaveSettlementState(SettlementState state, SaveWriter writer)
        {
            writer.WriteString(state.SettlementId);
            writer.WriteString(state.SettlementName);
            writer.Write(state.LastAdvancedTurn);

            writer.Write(state.Sites.Count);
            foreach (var kvp in state.Sites)
            {
                writer.WriteString(kvp.Key);
                SaveRepairableSiteState(kvp.Value, writer);
            }

            List<string> conditions = state.GetConditionsSnapshot();
            writer.Write(conditions.Count);
            for (int i = 0; i < conditions.Count; i++)
                writer.WriteString(conditions[i]);

            List<string> pending = state.GetPendingMessagesSnapshot();
            writer.Write(pending.Count);
            for (int i = 0; i < pending.Count; i++)
                writer.WriteString(pending[i]);
        }

        private static SettlementState LoadSettlementState(SaveReader reader)
        {
            var state = new SettlementState
            {
                SettlementId = reader.ReadString(),
                SettlementName = reader.ReadString(),
                LastAdvancedTurn = reader.ReadInt()
            };

            int siteCount = reader.ReadInt();
            var sites = new Dictionary<string, RepairableSiteState>(siteCount);
            for (int i = 0; i < siteCount; i++)
                sites[reader.ReadString()] = LoadRepairableSiteState(reader);

            int conditionCount = reader.ReadInt();
            var conditions = new List<string>(conditionCount);
            for (int i = 0; i < conditionCount; i++)
                conditions.Add(reader.ReadString());

            int pendingCount = reader.ReadInt();
            var pending = new List<string>(pendingCount);
            for (int i = 0; i < pendingCount; i++)
                pending.Add(reader.ReadString());

            state.RestoreCollections(sites, conditions, pending);
            return state;
        }

        private static void SaveRepairableSiteState(RepairableSiteState site, SaveWriter writer)
        {
            writer.WriteString(site.SiteId);
            writer.Write((int)site.SiteType);
            writer.Write((int)site.ProblemType);
            writer.Write((int)site.Stage);
            writer.Write(site.Severity);
            writer.Write((int)site.ResolvedByMethod);
            writer.Write(site.ResolvedAtTurn);
            writer.Write(site.RelapseAtTurn.HasValue);
            if (site.RelapseAtTurn.HasValue)
                writer.Write(site.RelapseAtTurn.Value);
            writer.Write((int)site.OutcomeTier);
        }

        private static RepairableSiteState LoadRepairableSiteState(SaveReader reader)
        {
            var site = new RepairableSiteState
            {
                SiteId = reader.ReadString(),
                SiteType = (RepairableSiteType)reader.ReadInt(),
                ProblemType = (RepairProblemType)reader.ReadInt(),
                Stage = (RepairStage)reader.ReadInt(),
                Severity = reader.ReadInt(),
                ResolvedByMethod = (RepairMethodId)reader.ReadInt(),
                ResolvedAtTurn = reader.ReadInt()
            };
            site.RelapseAtTurn = reader.ReadBool() ? reader.ReadInt() : (int?)null;
            site.OutcomeTier = (RepairOutcomeTier)reader.ReadInt();
            return site;
        }

        private static void SaveStat(Stat stat, SaveWriter writer)
        {
            writer.WriteString(stat.Name);
            writer.WriteString(stat.sValue);
            writer.Write(stat.BaseValue);
            writer.Write(stat.Bonus);
            writer.Write(stat.Penalty);
            writer.Write(stat.Boost);
            writer.Write(stat.Min);
            writer.Write(stat.Max);
        }

        private static Stat LoadStat(SaveReader reader)
        {
            return new Stat
            {
                Name = reader.ReadString(),
                sValue = reader.ReadString(),
                BaseValue = reader.ReadInt(),
                Bonus = reader.ReadInt(),
                Penalty = reader.ReadInt(),
                Boost = reader.ReadInt(),
                Min = reader.ReadInt(),
                Max = reader.ReadInt()
            };
        }

        private static void SavePart(Part part, SaveWriter writer)
        {
            writer.WriteString(GetTypeName(part.GetType()));
            part.OnBeforeSave(writer);

            if (part is StatusEffectsPart status)
                SaveStatusEffectsPart(status, writer);
            else if (part is BitLockerPart bits)
                SaveBitLockerPart(bits, writer);
            else if (part is InventoryPart inventory)
                SaveInventoryPart(inventory, writer);
            else if (part is ActivatedAbilitiesPart abilities)
                SaveActivatedAbilitiesPart(abilities, writer);
            else if (part is Body body)
                SaveBody(body, writer);
            else if (part is BrainPart brain)
                SaveBrainPart(brain, writer);
            else if (part is MutationsPart mutations)
                SaveMutationsPart(mutations, writer);
            else if (part is ISaveSerializable serializable)
                serializable.Save(writer);
            else
                WritePublicFields(part, writer);

            part.OnAfterSave(writer);
        }

        private static Part LoadPart(SaveReader reader)
        {
            Type type = ResolveType(reader.ReadString());
            if (type == null || type.IsAbstract || !typeof(Part).IsAssignableFrom(type))
                return null;

            var part = (Part)Activator.CreateInstance(type);
            if (part is StatusEffectsPart status)
                LoadStatusEffectsPart(status, reader);
            else if (part is BitLockerPart bits)
                LoadBitLockerPart(bits, reader);
            else if (part is InventoryPart inventory)
                LoadInventoryPart(inventory, reader);
            else if (part is ActivatedAbilitiesPart abilities)
                LoadActivatedAbilitiesPart(abilities, reader);
            else if (part is Body body)
                LoadBody(body, reader);
            else if (part is BrainPart brain)
                LoadBrainPart(brain, reader);
            else if (part is MutationsPart mutations)
                LoadMutationsPart(mutations, reader);
            else if (part is ISaveSerializable serializable)
                serializable.Load(reader);
            else
                ReadPublicFields(part, reader);

            return part;
        }

        private static void SaveStatusEffectsPart(StatusEffectsPart part, SaveWriter writer)
        {
            IReadOnlyList<Effect> effects = part.GetAllEffects();
            writer.Write(effects.Count);
            for (int i = 0; i < effects.Count; i++)
                SaveEffect(effects[i], writer);
        }

        private static void LoadStatusEffectsPart(StatusEffectsPart part, SaveReader reader)
        {
            int count = reader.ReadInt();
            var effects = new List<Effect>(count);
            for (int i = 0; i < count; i++)
                effects.Add(LoadEffect(reader));
            part.RestoreEffectsForLoad(effects);
        }

        private static void SaveEffect(Effect effect, SaveWriter writer)
        {
            writer.WriteString(GetTypeName(effect.GetType()));
            effect.OnBeforeSave(writer);
            writer.Write(effect.Duration);
            WritePublicFields(effect, writer, field => field.Name != nameof(Effect.Owner) && field.Name != nameof(Effect.Duration));
            effect.OnAfterSave(writer);
        }

        private static Effect LoadEffect(SaveReader reader)
        {
            Type type = ResolveType(reader.ReadString());
            if (type == null || type.IsAbstract || !typeof(Effect).IsAssignableFrom(type))
                return null;

            // Bypass constructors via FormatterServices — many concrete effects
            // (SmolderingEffect, FrozenEffect, PoisonedEffect, ...) declare
            // parameterized constructors only. Activator.CreateInstance(type)
            // would throw MissingMethodException for those, breaking save load
            // for any creature with a status effect. Field-level deserialization
            // below restores the state the constructor would have set, so
            // skipping ctor invocation is safe.
            var effect = (Effect)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            effect.Duration = reader.ReadInt();
            ReadPublicFields(effect, reader);
            return effect;
        }

        private static void SaveBitLockerPart(BitLockerPart part, SaveWriter writer)
        {
            var bits = part.GetBitsSnapshot();
            writer.Write(bits.Count);
            foreach (var kvp in bits)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            var recipes = new List<string>(part.GetKnownRecipes());
            writer.Write(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
                writer.WriteString(recipes[i]);
        }

        private static void LoadBitLockerPart(BitLockerPart part, SaveReader reader)
        {
            int bitCount = reader.ReadInt();
            var bits = new Dictionary<char, int>(bitCount);
            for (int i = 0; i < bitCount; i++)
                bits[reader.ReadChar()] = reader.ReadInt();

            int recipeCount = reader.ReadInt();
            var recipes = new List<string>(recipeCount);
            for (int i = 0; i < recipeCount; i++)
                recipes.Add(reader.ReadString());

            part.RestoreBitsAndRecipes(bits, recipes);
        }

        private static void SaveInventoryPart(InventoryPart part, SaveWriter writer)
        {
            writer.Write(part.MaxWeight);
            writer.Write(part.Objects.Count);
            for (int i = 0; i < part.Objects.Count; i++)
                writer.WriteEntityReference(part.Objects[i]);

            writer.Write(part.EquippedItems.Count);
            foreach (var kvp in part.EquippedItems)
            {
                writer.WriteString(kvp.Key);
                writer.WriteEntityReference(kvp.Value);
            }
        }

        private static void LoadInventoryPart(InventoryPart part, SaveReader reader)
        {
            part.MaxWeight = reader.ReadInt();
            part.Objects.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
                part.Objects.Add(reader.ReadEntityReference());

            part.EquippedItems.Clear();
            int equippedCount = reader.ReadInt();
            for (int i = 0; i < equippedCount; i++)
                part.EquippedItems[reader.ReadString()] = reader.ReadEntityReference();
        }

        private static void SaveActivatedAbilitiesPart(ActivatedAbilitiesPart part, SaveWriter writer)
        {
            writer.Write(part.AbilityList.Count);
            for (int i = 0; i < part.AbilityList.Count; i++)
                WritePublicFields(part.AbilityList[i], writer);

            writer.Write(part.SlotAssignments?.Length ?? 0);
            if (part.SlotAssignments != null)
            {
                for (int i = 0; i < part.SlotAssignments.Length; i++)
                    writer.WriteGuid(part.SlotAssignments[i]);
            }
        }

        private static void LoadActivatedAbilitiesPart(ActivatedAbilitiesPart part, SaveReader reader)
        {
            part.AbilityByGuid.Clear();
            part.AbilityList.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var ability = new ActivatedAbility();
                ReadPublicFields(ability, reader);
                part.AbilityList.Add(ability);
                part.AbilityByGuid[ability.ID] = ability;
            }

            int slots = reader.ReadInt();
            part.SlotAssignments = new Guid[ActivatedAbilitiesPart.SlotCount];
            for (int i = 0; i < slots; i++)
            {
                Guid id = reader.ReadGuid();
                if (i < part.SlotAssignments.Length)
                    part.SlotAssignments[i] = id;
            }
            part.MigrateLegacyAssignments();
        }

        private static void SaveBody(Body body, SaveWriter writer)
        {
            SaveBodyPart(body.GetBody(), writer);
            writer.Write(body.DismemberedParts.Count);
            for (int i = 0; i < body.DismemberedParts.Count; i++)
            {
                SaveBodyPart(body.DismemberedParts[i].Part, writer);
                writer.Write(body.DismemberedParts[i].ParentPartID);
                writer.Write(body.DismemberedParts[i].OriginalPosition);
            }
        }

        private static void LoadBody(Body body, SaveReader reader)
        {
            body.SetBody(LoadBodyPart(reader));
            body.DismemberedParts.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                body.DismemberedParts.Add(new DismemberedPart
                {
                    Part = LoadBodyPart(reader),
                    ParentPartID = reader.ReadInt(),
                    OriginalPosition = reader.ReadInt()
                });
            }
        }

        private static void SaveBodyPart(BodyPart part, SaveWriter writer)
        {
            writer.Write(part != null);
            if (part == null)
                return;

            writer.WriteString(part.Type);
            writer.WriteString(part.VariantType);
            writer.WriteString(part.Description);
            writer.WriteString(part.DescriptionPrefix);
            writer.WriteString(part.Name);
            writer.WriteString(part.SupportsDependent);
            writer.WriteString(part.DependsOn);
            writer.WriteString(part.RequiresType);
            writer.WriteString(part.Manager);
            writer.Write(part.Category);
            writer.Write(part._Laterality);
            writer.Write(part.RequiresLaterality);
            writer.Write(part.Mobility);
            writer.Write(part.TargetWeight);
            writer.Write(part.Flags);
            writer.Write(part.Position);
            writer.WriteString(part.DefaultBehaviorBlueprint);
            writer.Write(part.ID);
            writer.WriteEntityReference(part._Equipped);
            writer.WriteEntityReference(part._Cybernetics);
            writer.WriteEntityReference(part._DefaultBehavior);

            writer.Write(part.Parts?.Count ?? 0);
            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    SaveBodyPart(part.Parts[i], writer);
            }
        }

        private static BodyPart LoadBodyPart(SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            var part = new BodyPart
            {
                Type = reader.ReadString(),
                VariantType = reader.ReadString(),
                Description = reader.ReadString(),
                DescriptionPrefix = reader.ReadString(),
                Name = reader.ReadString(),
                SupportsDependent = reader.ReadString(),
                DependsOn = reader.ReadString(),
                RequiresType = reader.ReadString(),
                Manager = reader.ReadString(),
                Category = reader.ReadInt(),
                _Laterality = reader.ReadInt(),
                RequiresLaterality = reader.ReadInt(),
                Mobility = reader.ReadInt(),
                TargetWeight = reader.ReadInt(),
                Flags = reader.ReadInt(),
                Position = reader.ReadInt(),
                DefaultBehaviorBlueprint = reader.ReadString()
            };
            part.ID = reader.ReadInt();
            part._Equipped = reader.ReadEntityReference();
            part._Cybernetics = reader.ReadEntityReference();
            part._DefaultBehavior = reader.ReadEntityReference();

            int childCount = reader.ReadInt();
            if (childCount > 0)
                part.Parts = new List<BodyPart>(childCount);

            for (int i = 0; i < childCount; i++)
            {
                BodyPart child = LoadBodyPart(reader);
                if (child == null)
                    continue;
                child.ParentPart = part;
                part.Parts.Add(child);
            }
            return part;
        }

        private static void SaveBrainPart(BrainPart brain, SaveWriter writer)
        {
            writer.Write(brain.SightRadius);
            writer.Write(brain.Wanders);
            writer.Write(brain.WandersRandomly);
            writer.Write(brain.FleeThreshold);
            writer.Write(brain.Passive);
            writer.Write((int)brain.CurrentState);
            writer.WriteEntityReference(brain.Target);
            writer.Write(brain.InConversation);
            writer.Write(brain.PersonalEnemies.Count);
            foreach (Entity enemy in brain.PersonalEnemies)
                writer.WriteEntityReference(enemy);
            writer.WriteString(brain.CurrentZone?.ZoneID);
            writer.Write(brain.StartingCellX);
            writer.Write(brain.StartingCellY);
            writer.Write(brain.Staying);
            writer.WriteString(brain.LastThought);
            writer.Write(brain.ThinkOutLoud);

            List<GoalHandler> goals = brain.GetGoalsSnapshot();
            int serializableCount = 0;
            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i] != null && goals[i].GetType().Name != "DelegateGoal")
                    serializableCount++;
            }

            writer.Write(serializableCount);
            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i] == null || goals[i].GetType().Name == "DelegateGoal")
                    continue;
                SaveGoal(goals[i], writer);
            }
        }

        private static void LoadBrainPart(BrainPart brain, SaveReader reader)
        {
            brain.SightRadius = reader.ReadInt();
            brain.Wanders = reader.ReadBool();
            brain.WandersRandomly = reader.ReadBool();
            brain.FleeThreshold = reader.ReadFloat();
            brain.Passive = reader.ReadBool();
            brain.CurrentState = (AIState)reader.ReadInt();
            brain.Target = reader.ReadEntityReference();
            brain.InConversation = reader.ReadBool();
            brain.PersonalEnemies.Clear();
            int enemyCount = reader.ReadInt();
            for (int i = 0; i < enemyCount; i++)
            {
                Entity enemy = reader.ReadEntityReference();
                if (enemy != null)
                    brain.PersonalEnemies.Add(enemy);
            }
            brain.CurrentZone = reader.FindZone(reader.ReadString());
            brain.StartingCellX = reader.ReadInt();
            brain.StartingCellY = reader.ReadInt();
            brain.Staying = reader.ReadBool();
            brain.LastThought = reader.ReadString();
            brain.ThinkOutLoud = reader.ReadBool();
            brain.Rng = new System.Random();

            int goalCount = reader.ReadInt();
            var goals = new List<GoalHandler>(goalCount);
            for (int i = 0; i < goalCount; i++)
            {
                GoalHandler goal = LoadGoal(reader);
                if (goal == null)
                    continue;
                goal.ParentHandler = goals.Count > 0 ? goals[goals.Count - 1] : null;
                goals.Add(goal);
            }
            brain.RestoreGoalsForLoad(goals);
        }

        private static void SaveGoal(GoalHandler goal, SaveWriter writer)
        {
            writer.WriteString(GetTypeName(goal.GetType()));
            writer.Write(goal.Age);
            WritePublicFields(goal, writer, field =>
                field.Name != nameof(GoalHandler.ParentBrain) &&
                field.Name != nameof(GoalHandler.ParentHandler) &&
                !typeof(Delegate).IsAssignableFrom(field.FieldType));
        }

        private static GoalHandler LoadGoal(SaveReader reader)
        {
            Type type = ResolveType(reader.ReadString());
            if (type == null || type.IsAbstract || !typeof(GoalHandler).IsAssignableFrom(type) || type.Name == "DelegateGoal")
                return null;

            // Bypass constructors via FormatterServices — most concrete goals
            // (LayRuneGoal, MoveToGoal, WaitGoal, KillGoal, FleeGoal,
            // DisposeOfCorpseGoal, ...) declare parameterized constructors
            // only. Activator.CreateInstance(type) would throw
            // MissingMethodException for those, breaking save load for any
            // NPC with goals on the stack. Field-level deserialization below
            // restores the state the constructor would have set, so skipping
            // ctor invocation is safe.
            var goal = (GoalHandler)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            goal.Age = reader.ReadInt();
            ReadPublicFields(goal, reader);
            return goal;
        }

        private static void SaveMutationsPart(MutationsPart part, SaveWriter writer)
        {
            writer.WriteString(part.StartingMutations);

            writer.Write(part.MutationList.Count);
            for (int i = 0; i < part.MutationList.Count; i++)
                writer.WriteString(GetTypeName(part.MutationList[i].GetType()));

            writer.Write(part.MutationMods.Count);
            for (int i = 0; i < part.MutationMods.Count; i++)
                WritePublicFields(part.MutationMods[i], writer);

            writer.Write(part.MutationGeneratedEquipment.Count);
            for (int i = 0; i < part.MutationGeneratedEquipment.Count; i++)
                WritePublicFields(part.MutationGeneratedEquipment[i], writer);
        }

        private static void LoadMutationsPart(MutationsPart part, SaveReader reader)
        {
            part.StartingMutations = reader.ReadString();
            part.MutationList.Clear();

            int mutationCount = reader.ReadInt();
            for (int i = 0; i < mutationCount; i++)
                reader.ReadString();

            part.MutationMods.Clear();
            int modCount = reader.ReadInt();
            for (int i = 0; i < modCount; i++)
            {
                var tracker = new MutationModifierTracker();
                ReadPublicFields(tracker, reader);
                part.MutationMods.Add(tracker);
            }

            part.MutationGeneratedEquipment.Clear();
            int generatedCount = reader.ReadInt();
            for (int i = 0; i < generatedCount; i++)
            {
                var tracker = new MutationGeneratedEquipmentTracker();
                ReadPublicFields(tracker, reader);
                part.MutationGeneratedEquipment.Add(tracker);
            }
        }

        private static void WritePublicFields(object obj, SaveWriter writer, Func<FieldInfo, bool> include = null)
        {
            FieldInfo[] fields = GetSerializablePublicFields(obj.GetType(), include);
            writer.Write(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                writer.WriteString(fields[i].Name);
                WriteFieldValue(fields[i].FieldType, fields[i].GetValue(obj), writer);
            }
        }

        private static void ReadPublicFields(object obj, SaveReader reader)
        {
            int count = reader.ReadInt();
            Type type = obj.GetType();
            for (int i = 0; i < count; i++)
            {
                string name = reader.ReadString();
                FieldInfo field = GetField(type, name);
                Type fieldType = field != null ? field.FieldType : typeof(object);
                object value = ReadFieldValue(fieldType, reader);
                if (field != null && !field.IsInitOnly && value != SkipValue.Instance)
                    field.SetValue(obj, value);
            }
        }

        private static FieldInfo[] GetSerializablePublicFields(Type type, Func<FieldInfo, bool> include)
        {
            var result = new List<FieldInfo>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.IsStatic || field.IsInitOnly || Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                    continue;
                if (field.Name == nameof(Part.ParentEntity) || field.Name == nameof(Effect.Owner))
                    continue;
                if (include != null && !include(field))
                    continue;
                if (!CanSerializeType(field.FieldType))
                    continue;
                result.Add(field);
            }
            return result.ToArray();
        }

        private static FieldInfo GetField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        private static bool CanSerializeType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type.IsEnum)
                return true;
            if (type == typeof(Entity))
                return true;
            if (typeof(Delegate).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type))
                return false;
            if (type.IsArray)
                return CanSerializeType(type.GetElementType());
            if (typeof(IList).IsAssignableFrom(type))
                return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                return true;
            if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)
                return true;
            return false;
        }

        private static void WriteFieldValue(Type type, object value, SaveWriter writer)
        {
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                writer.Write(value != null);
                if (value != null)
                    WriteFieldValue(nullable, value, writer);
                return;
            }

            if (type == typeof(int)) writer.Write((int)value);
            else if (type == typeof(long)) writer.Write((long)value);
            else if (type == typeof(float)) writer.Write((float)value);
            else if (type == typeof(double)) writer.Write((double)value);
            else if (type == typeof(bool)) writer.Write((bool)value);
            else if (type == typeof(char)) writer.Write((char)value);
            else if (type == typeof(string)) writer.WriteString((string)value);
            else if (type == typeof(Guid)) writer.WriteGuid((Guid)value);
            else if (type.IsEnum) writer.Write(Convert.ToInt32(value));
            else if (type == typeof(Entity)) writer.WriteEntityReference((Entity)value);
            else if (type.IsArray)
            {
                Array array = (Array)value;
                writer.Write(array?.Length ?? -1);
                if (array != null)
                {
                    Type elementType = type.GetElementType();
                    for (int i = 0; i < array.Length; i++)
                        WriteFieldValue(elementType, array.GetValue(i), writer);
                }
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                IList list = (IList)value;
                writer.Write(list?.Count ?? -1);
                if (list != null)
                {
                    Type elementType = GetListElementType(type);
                    for (int i = 0; i < list.Count; i++)
                        WriteCollectionElement(elementType, list[i], writer);
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                IEnumerable set = (IEnumerable)value;
                if (set == null)
                {
                    writer.Write(-1);
                }
                else
                {
                    var values = new List<object>();
                    foreach (object item in set)
                        values.Add(item);

                    Type elementType = type.GetGenericArguments()[0];
                    writer.Write(values.Count);
                    for (int i = 0; i < values.Count; i++)
                        WriteCollectionElement(elementType, values[i], writer);
                }
            }
            else
            {
                WriteTypedObject(type, value, writer);
            }
        }

        private static object ReadFieldValue(Type type, SaveReader reader)
        {
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
                return reader.ReadBool() ? ReadFieldValue(nullable, reader) : null;

            if (type == typeof(object))
                return SkipValue.Instance;
            if (type == typeof(int)) return reader.ReadInt();
            if (type == typeof(long)) return reader.ReadLong();
            if (type == typeof(float)) return reader.ReadFloat();
            if (type == typeof(double)) return reader.ReadDouble();
            if (type == typeof(bool)) return reader.ReadBool();
            if (type == typeof(char)) return reader.ReadChar();
            if (type == typeof(string)) return reader.ReadString();
            if (type == typeof(Guid)) return reader.ReadGuid();
            if (type.IsEnum) return Enum.ToObject(type, reader.ReadInt());
            if (type == typeof(Entity)) return reader.ReadEntityReference();
            if (type.IsArray)
            {
                int count = reader.ReadInt();
                if (count < 0)
                    return null;
                Type elementType = type.GetElementType();
                Array array = Array.CreateInstance(elementType, count);
                for (int i = 0; i < count; i++)
                    array.SetValue(ReadFieldValue(elementType, reader), i);
                return array;
            }
            if (typeof(IList).IsAssignableFrom(type))
            {
                int count = reader.ReadInt();
                if (count < 0)
                    return null;
                Type elementType = GetListElementType(type);
                IList list = (IList)Activator.CreateInstance(type);
                for (int i = 0; i < count; i++)
                    list.Add(ReadCollectionElement(elementType, reader));
                return list;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                int count = reader.ReadInt();
                if (count < 0)
                    return null;
                Type elementType = type.GetGenericArguments()[0];
                object set = Activator.CreateInstance(type);
                MethodInfo add = type.GetMethod("Add", new[] { elementType });
                for (int i = 0; i < count; i++)
                    add.Invoke(set, new[] { ReadCollectionElement(elementType, reader) });
                return set;
            }
            return ReadTypedObject(type, reader);
        }

        private static void WriteCollectionElement(Type elementType, object value, SaveWriter writer)
        {
            if (IsSimpleValueType(elementType) || elementType == typeof(Entity))
                WriteFieldValue(elementType, value, writer);
            else
                WriteTypedObject(elementType, value, writer);
        }

        private static object ReadCollectionElement(Type elementType, SaveReader reader)
        {
            if (IsSimpleValueType(elementType) || elementType == typeof(Entity))
                return ReadFieldValue(elementType, reader);
            return ReadTypedObject(elementType, reader);
        }

        private static bool IsSimpleValueType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type.IsEnum;
        }

        private static void WriteTypedObject(Type declaredType, object value, SaveWriter writer)
        {
            writer.Write(value != null);
            if (value == null)
                return;

            Type actualType = value.GetType();
            bool writesConcreteType = declaredType == null || declaredType.IsAbstract || declaredType.IsInterface || actualType != declaredType;
            writer.Write(writesConcreteType);
            if (writesConcreteType)
                writer.WriteString(GetTypeName(actualType));

            if (value is Entity entity)
                writer.WriteEntityReference(entity);
            else
                WritePublicFields(value, writer);
        }

        private static object ReadTypedObject(Type declaredType, SaveReader reader)
        {
            if (!reader.ReadBool())
                return null;

            Type type = declaredType;
            if (reader.ReadBool())
                type = ResolveType(reader.ReadString());

            if (type == typeof(Entity))
                return reader.ReadEntityReference();
            if (type == null || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                return null;

            object obj = Activator.CreateInstance(type);
            ReadPublicFields(obj, reader);
            return obj;
        }

        private static Type GetListElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            if (type.IsGenericType)
                return type.GetGenericArguments()[0];
            return typeof(object);
        }

        private static void WriteStringDictionary(Dictionary<string, string> dict, SaveWriter writer)
        {
            writer.Write(dict?.Count ?? 0);
            if (dict == null)
                return;
            foreach (var kvp in dict)
            {
                writer.WriteString(kvp.Key);
                writer.WriteString(kvp.Value);
            }
        }

        private static Dictionary<string, string> ReadStringDictionary(SaveReader reader)
        {
            int count = reader.ReadInt();
            var dict = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
                dict[reader.ReadString()] = reader.ReadString();
            return dict;
        }

        private static void WriteIntDictionary(Dictionary<string, int> dict, SaveWriter writer)
        {
            writer.Write(dict?.Count ?? 0);
            if (dict == null)
                return;
            foreach (var kvp in dict)
            {
                writer.WriteString(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        private static Dictionary<string, int> ReadIntDictionary(SaveReader reader)
        {
            int count = reader.ReadInt();
            var dict = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
                dict[reader.ReadString()] = reader.ReadInt();
            return dict;
        }

        private static string GetTypeName(Type type) => type.AssemblyQualifiedName;

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = Type.GetType(typeName);
            if (type != null)
                return type;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        private sealed class SkipValue
        {
            public static readonly SkipValue Instance = new SkipValue();
        }
    }
}
