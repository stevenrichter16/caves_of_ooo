using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Optional versioned section inside a format-7 GameSession, immediately
    /// before its existing End check. Old format-7 sessions have no section;
    /// standalone zone/entity graph formats are deliberately unchanged.
    /// </summary>
    internal static class SessionTileStateSerializer
    {
        internal const int Version = 1;
        internal const int MaxZones = 4096;
        internal const int MaxLayers = 256;
        internal const int MaxStringBytes = 1024;
        private const int CellCount = Zone.Width * Zone.Height;
        private const string Begin = "TileState.Begin", End = "TileState.End", SessionEnd = "GameSession.End";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static void Write(OverworldZoneManager manager, SaveWriter writer)
        {
            // Validate and copy first: later writes cannot observe a partial
            // mutation of a public layer list, and invalid data is not clamped.
            Snapshot snapshot = Capture(manager);
            writer.WriteCheck(Begin);
            writer.Write(Version);
            writer.Write(snapshot.Zones.Count);
            foreach (var zone in snapshot.Zones)
            {
                WriteText(writer, zone.Zone.ZoneID);
                writer.Write(zone.Tiles.Count);
                foreach (var tile in zone.Tiles)
                {
                    writer.Write(tile.Key);
                    WriteLayers(writer, tile.Coatings);
                    WriteLayers(writer, tile.Residues);
                    writer.Write(tile.Heat); writer.Write(tile.Cold); writer.Write(tile.Charge);
                    WriteText(writer, tile.Cloud); writer.Write(tile.CloudTurns);
                }
            }
            writer.WriteCheck(End);
        }

        /// <summary>
        /// Consumes either the legacy End check, or the entire tile section
        /// and End check, without seeking or reading past the session boundary.
        /// No candidate state or process globals change before all checks pass.
        /// </summary>
        internal static Snapshot ReadOptionalAndEnd(OverworldZoneManager manager, SaveReader reader)
        {
            int marker = reader.ReadInt();
            if (marker == SaveWriter.CheckValue(SessionEnd)) return null;
            Require(marker == SaveWriter.CheckValue(Begin), "Unknown session footer section.");
            Require(reader.ReadInt() == Version, "Unsupported tile-state section version.");
            int count = ReadCount(reader, MaxZones, "zone");
            int expected = manager?.CachedZones?.Count ?? 0;
            Require(count == expected, "Tile-state section must describe every cached zone exactly once.");
            var records = new List<ZoneRecord>(count);
            var seenZones = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                string id = ReadText(reader, false);
                Require(seenZones.Add(id), "Duplicate tile-state zone.");
                Require(manager != null && manager.CachedZones.TryGetValue(id, out var candidate)
                    && candidate != null && string.Equals(candidate.ZoneID, id, StringComparison.Ordinal), "Unknown or mismatched tile-state zone.");
                // Re-resolve after the guard to keep definite assignment explicit.
                var zone = manager.CachedZones[id];
                int tileCount = ReadCount(reader, CellCount, "tile");
                var record = new ZoneRecord(zone);
                var seenTiles = new HashSet<int>();
                for (int t = 0; t < tileCount; t++)
                {
                    int key = reader.ReadInt();
                    Require(key >= 0 && key < CellCount && seenTiles.Add(key), "Invalid or duplicate tile coordinate.");
                    var tile = new TileRecord
                    {
                        Key = key, Coatings = ReadLayers(reader), Residues = ReadLayers(reader),
                        Heat = reader.ReadInt(), Cold = reader.ReadInt(), Charge = reader.ReadInt(),
                        Cloud = ReadText(reader, true), CloudTurns = reader.ReadInt()
                    };
                    ValidateTile(tile);
                    record.Tiles.Add(tile);
                }
                records.Add(record);
            }
            reader.ExpectCheck(End);
            reader.ExpectCheck(SessionEnd);
            return new Snapshot(records);
        }

        internal static Snapshot Capture(OverworldZoneManager manager)
        {
            var zones = new List<ZoneRecord>();
            if (manager == null) return new Snapshot(zones);
            Require(manager.CachedZones != null && manager.CachedZones.Count <= MaxZones, "Invalid cached-zone count for tile-state save.");
            var ids = new List<string>(manager.CachedZones.Keys);
            ids.Sort(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                ValidateText(id, false);
                var zone = manager.CachedZones[id];
                Require(zone != null && string.Equals(id, zone.ZoneID, StringComparison.Ordinal), "Mismatched cached-zone identity in tile-state save.");
                var record = new ZoneRecord(zone);
                var keys = new List<int>(); zone.TileState.CollectWrittenKeys(keys); keys.Sort();
                Require(keys.Count <= CellCount, "Too many written tiles.");
                foreach (int key in keys)
                {
                    Require(key >= 0 && key < CellCount, "Invalid tile coordinate in save.");
                    var state = zone.TileState.Get(key % Zone.Width, key / Zone.Width);
                    Require(state != null, "Missing written tile state.");
                    var tile = new TileRecord
                    {
                        Key = key, Coatings = CopyLayers(state.Coatings), Residues = CopyLayers(state.Residues),
                        Heat = state.Heat, Cold = state.Cold, Charge = state.Charge,
                        Cloud = state.Cloud ?? "", CloudTurns = state.CloudTurns
                    };
                    ValidateTile(tile);
                    record.Tiles.Add(tile);
                }
                zones.Add(record);
            }
            return new Snapshot(zones);
        }

        private static LayerRecord[] CopyLayers(List<ZoneTileState.Layer> layers)
        {
            Require(layers != null && layers.Count <= MaxLayers, "Invalid tile layer count.");
            var result = new LayerRecord[layers.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                Require(layer != null, "Null tile layer.");
                ValidateText(layer.Id, false);
                Require(layer.Turns > 0 && ids.Add(layer.Id), "Invalid duration or duplicate tile layer.");
                result[i] = new LayerRecord(layer.Id, layer.Turns);
            }
            return result;
        }

        private static LayerRecord[] ReadLayers(SaveReader reader)
        {
            int count = ReadCount(reader, MaxLayers, "layer");
            var layers = new LayerRecord[count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                string id = ReadText(reader, false);
                int turns = reader.ReadInt();
                Require(turns > 0 && ids.Add(id), "Invalid duration or duplicate tile layer.");
                layers[i] = new LayerRecord(id, turns);
            }
            return layers;
        }

        private static void WriteLayers(SaveWriter writer, LayerRecord[] layers)
        {
            writer.Write(layers.Length);
            foreach (var layer in layers) { WriteText(writer, layer.Id); writer.Write(layer.Turns); }
        }

        private static void ValidateTile(TileRecord tile)
        {
            Require(ValidEnergy(tile.Heat) && ValidEnergy(tile.Cold) && ValidEnergy(tile.Charge), "Invalid tile energy.");
            ValidateText(tile.Cloud, true);
            Require(tile.Cloud.Length == 0 ? tile.CloudTurns == 0 : tile.CloudTurns > 0, "Incoherent tile cloud duration.");
            Require(tile.Coatings.Length > 0 || tile.Residues.Length > 0 || tile.Heat > 0
                || tile.Cold > 0 || tile.Charge > 0 || tile.Cloud.Length > 0, "Empty written tile record.");
        }

        private static bool ValidEnergy(int value) => value >= 0 && value <= ZoneTileState.MaxEnergy;
        private static int ReadCount(SaveReader reader, int maximum, string kind)
        {
            int count = reader.ReadInt();
            Require(count >= 0 && count <= maximum, "Invalid tile-state " + kind + " count.");
            return count;
        }

        // Only the extension uses these int-length UTF-8 strings. Bounds are
        // checked before allocating bytes; the legacy WriteString format stays intact.
        private static void WriteText(SaveWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            writer.Write(bytes.Length); writer.WriteBytes(bytes);
        }
        private static string ReadText(SaveReader reader, bool emptyAllowed)
        {
            int length = ReadCount(reader, MaxStringBytes, "UTF-8 byte");
            Require(emptyAllowed || length > 0, "Empty tile-state identifier.");
            try { return Utf8.GetString(reader.ReadBytesExactly(length)); }
            catch (DecoderFallbackException e) { throw new InvalidDataException("Invalid tile-state UTF-8.", e); }
        }
        private static void ValidateText(string value, bool emptyAllowed)
        {
            Require(value != null && (emptyAllowed || value.Length > 0) && value.Length <= MaxStringBytes, "Invalid tile-state identifier length.");
            try { Require(Utf8.GetByteCount(value) <= MaxStringBytes, "Tile-state identifier is too long in UTF-8."); }
            catch (EncoderFallbackException e) { throw new InvalidDataException("Invalid tile-state identifier Unicode.", e); }
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidDataException(message); }

        internal sealed class Snapshot
        {
            internal readonly List<ZoneRecord> Zones;
            internal Snapshot(List<ZoneRecord> zones) { Zones = zones; }

            // Apply saved state before hooks, then the post-hook capture after
            // repair. Exact absence wins over reconstruction pool projections.
            // Equal state is left alone, preserving references observed by hooks.
            internal void Apply()
            {
                foreach (var record in Zones)
                {
                    ZoneTileState target = record.Zone.TileState;
                    if (Matches(target, record.Tiles)) continue;
                    var callback = target.OnCellChanged;
                    target.OnCellChanged = null;
                    try
                    {
                        // This existing API clears without a render callback.
                        // No untrusted JSON is parsed: the section was validated above.
                        target.LoadFromString(null);
                        foreach (var tile in record.Tiles)
                        {
                            int x = tile.Key % Zone.Width, y = tile.Key / Zone.Width;
                            foreach (var layer in tile.Coatings) target.WriteCoating(x, y, layer.Id, layer.Turns);
                            foreach (var layer in tile.Residues) target.WriteResidue(x, y, layer.Id, layer.Turns);
                            target.AddHeat(x, y, tile.Heat); target.AddCold(x, y, tile.Cold); target.AddCharge(x, y, tile.Charge);
                            if (tile.Cloud.Length > 0) target.WriteCloud(x, y, tile.Cloud, tile.CloudTurns);
                        }
                    }
                    finally { target.OnCellChanged = callback; }
                }
            }

            private static bool Matches(ZoneTileState state, List<TileRecord> tiles)
            {
                if (state.WrittenCount != tiles.Count) return false;
                foreach (var tile in tiles)
                {
                    var actual = state.Get(tile.Key % Zone.Width, tile.Key / Zone.Width);
                    if (actual == null || actual.Heat != tile.Heat || actual.Cold != tile.Cold || actual.Charge != tile.Charge
                        || (actual.Cloud ?? "") != tile.Cloud || actual.CloudTurns != tile.CloudTurns
                        || !SameLayers(actual.Coatings, tile.Coatings) || !SameLayers(actual.Residues, tile.Residues)) return false;
                }
                return true;
            }
            private static bool SameLayers(List<ZoneTileState.Layer> actual, LayerRecord[] saved)
            {
                if (actual == null || actual.Count != saved.Length) return false;
                for (int i = 0; i < saved.Length; i++)
                    if (actual[i] == null || actual[i].Id != saved[i].Id || actual[i].Turns != saved[i].Turns) return false;
                return true;
            }
        }
        internal sealed class ZoneRecord
        {
            internal readonly Zone Zone;
            internal readonly List<TileRecord> Tiles = new List<TileRecord>();
            internal ZoneRecord(Zone zone) { Zone = zone; }
        }
        internal sealed class TileRecord
        {
            internal int Key, Heat, Cold, Charge, CloudTurns;
            internal LayerRecord[] Coatings, Residues;
            internal string Cloud;
        }
        internal sealed class LayerRecord
        {
            internal readonly string Id;
            internal readonly int Turns;
            internal LayerRecord(string id, int turns) { Id = id; Turns = turns; }
        }
    }
}
