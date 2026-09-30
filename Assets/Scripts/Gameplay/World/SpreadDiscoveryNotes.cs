using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Eight bounded historical records in ordinary saved player properties.
    /// Reading never consults the current world, informant, or destination graph.</summary>
    public static class SpreadDiscoveryNotes
    {
        public const string Prefix = "SpreadDiscoveryNote.v1:";
        internal const string Pair = "ditch-cutters";
        internal const string Viper = "chalk-ring-viper";
        internal const string Wayhouse = "turnbank-wayhouse";

        // Same v1 record schema; five explicit original field-place families extend
        // the catalog without rewriting the earlier three historical records.
        internal static string FieldFamily(SpreadExplorationFamily family)
        {
            switch (family)
            {
                case SpreadExplorationFamily.FieldAlembic: return "field-alembic";
                case SpreadExplorationFamily.TemperingShelter: return "tempering-shelter";
                case SpreadExplorationFamily.TrappersStore: return "trappers-store";
                case SpreadExplorationFamily.SeedKeepersPlot: return "seed-keepers-plot";
                case SpreadExplorationFamily.WaysideKitchen: return "wayside-kitchen";
                default: return null;
            }
        }
        internal static string FieldPlaceName(string family)
        {
            switch (family)
            {
                case "field-alembic": return "a field alembic";
                case "tempering-shelter": return "a tempering shelter";
                case "trappers-store": return "a trapper's store";
                case "seed-keepers-plot": return "a seed keeper's plot";
                case "wayside-kitchen": return "a wayside kitchen";
                default: return null;
            }
        }

        [Serializable]
        internal sealed class Record
        {
            public int Version;
            public string Family, OriginZoneID, DestinationZoneID;
            public string InformantID, InformantName, Formation;
        }

        internal static bool Remember(Entity player, Record record)
        {
            if (player == null || !Valid(record, record?.Family)) return false;
            string wire = JsonUtility.ToJson(record);
            if (wire.Length > 4096) return false;
            player.Properties[Prefix + record.Family] = wire;
            return true;
        }

        public static IReadOnlyList<string> Read(Entity player)
        {
            var result = new List<string>(8);
            if (player == null) return result;
            ReadFamily(player, Pair, result);
            ReadFamily(player, Viper, result);
            ReadFamily(player, Wayhouse, result);
            foreach (var family in new[] { "field-alembic", "tempering-shelter", "trappers-store", "seed-keepers-plot", "wayside-kitchen" })
                ReadFamily(player, family, result);
            return result;
        }

        private static void ReadFamily(Entity player, string family, List<string> result)
        {
            if (!player.Properties.TryGetValue(Prefix + family, out string wire)
                || string.IsNullOrEmpty(wire) || wire.Length > 4096) return;
            try
            {
                var record = JsonUtility.FromJson<Record>(wire);
                if (Valid(record, family)) result.Add(Describe(record));
            }
            catch (ArgumentException) { /* Optional corrupt notes are ignored, never repaired here. */ }
        }

        internal static bool CanonicalSurface(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 32) return false;
            var at = WorldMap.FromZoneID(id);
            return WorldMapAuthoring.InBounds(at.x, at.y) && at.z == 0
                && id == WorldMap.ToZoneID(at.x, at.y, 0);
        }

        internal static bool BoundedText(string text, int maximum)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > maximum) return false;
            foreach (char c in text) if (char.IsControl(c)) return false;
            return true;
        }

        private static bool Valid(Record record, string family)
        {
            return record != null && record.Version == 1 && record.Family == family
                && (family == Pair || family == Viper || family == Wayhouse || FieldPlaceName(family) != null)
                && CanonicalSurface(record.OriginZoneID) && CanonicalSurface(record.DestinationZoneID)
                && BoundedText(record.InformantID, 128) && BoundedText(record.InformantName, 160)
                && (family == Pair ? record.Formation == "Hedgerow" || record.Formation == "OldRoad"
                    : family == Viper ? record.Formation == "Hedgerow"
                    : family == Wayhouse ? record.Formation == "OldRoad" || record.Formation == "Fallow"
                    : record.Formation == "OldRoad" || record.Formation == "Fallow" || record.Formation == "Hedgerow"
                        || record.Formation == "FieldStrips" || record.Formation == "RiverMeadow" || record.Formation == "FlowerMeadow");
        }

        internal static string Describe(Record record)
        {
            var from = WorldMap.FromZoneID(record.OriginZoneID);
            var to = WorldMap.FromZoneID(record.DestinationZoneID);
            var bearings = new List<string>(2);
            int dx = to.x - from.x, dy = to.y - from.y;
            if (dx != 0) bearings.Add(Math.Abs(dx) + (dx > 0 ? " east" : " west"));
            if (dy != 0) bearings.Add(Math.Abs(dy) + (dy > 0 ? " south" : " north"));
            string route = bearings.Count == 0 ? "in this world-map cell" : string.Join(", ", bearings) + " world-map cells";
            string report = record.Family == Pair
                ? "A carrier reported ditch-cutters " + (record.Formation == "OldRoad" ? "by an old road" : "in the hedgerows") + "."
                : record.Family == Viper ? "Old warnings mention chalk-ring vipers in the hedgerows. Their bites are poisonous."
                : record.Family == Wayhouse ? "Old reports place Turnbank wayhouse " + (record.Formation == "OldRoad" ? "by an old road." : "among fallow fields.")
                : "Old reports mention " + FieldPlaceName(record.Family) + ".";
            return report + " Destination (" + to.x + "," + to.y + "), " + route
                + " from (" + from.x + "," + from.y + "). Reported by " + record.InformantName
                + " at (" + from.x + "," + from.y + "); unconfirmed when heard. This report does not say what remains there now.";
        }
    }
}
