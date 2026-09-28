using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Three bounded historical records in ordinary saved player properties.
    /// Reading never consults the current world, informant, or destination graph.</summary>
    public static class SpreadDiscoveryNotes
    {
        public const string Prefix = "SpreadDiscoveryNote.v1:";
        internal const string Pair = "ditch-cutters";
        internal const string Viper = "chalk-ring-viper";
        internal const string Wayhouse = "turnbank-wayhouse";

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
            var result = new List<string>(3);
            if (player == null) return result;
            ReadFamily(player, Pair, result);
            ReadFamily(player, Viper, result);
            ReadFamily(player, Wayhouse, result);
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
                && (family == Pair || family == Viper || family == Wayhouse)
                && CanonicalSurface(record.OriginZoneID) && CanonicalSurface(record.DestinationZoneID)
                && BoundedText(record.InformantID, 128) && BoundedText(record.InformantName, 160)
                && (family == Pair ? record.Formation == "Hedgerow" || record.Formation == "OldRoad"
                    : family == Viper ? record.Formation == "Hedgerow"
                    : record.Formation == "OldRoad" || record.Formation == "Fallow");
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
                : "Old reports place Turnbank wayhouse " + (record.Formation == "OldRoad" ? "by an old road." : "among fallow fields.");
            return report + " Destination (" + to.x + "," + to.y + "), " + route
                + " from (" + from.x + "," + from.y + "). Reported by " + record.InformantName
                + " at (" + from.x + "," + from.y + "); unconfirmed when heard. This report does not say what remains there now.";
        }
    }
}
