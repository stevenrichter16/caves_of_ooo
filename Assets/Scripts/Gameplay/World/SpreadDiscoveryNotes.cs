using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Bounded historical reports in ordinary saved player properties.
    /// V1 remains readable without world context; v2 requires a caller-supplied
    /// trusted world identity. Neither reader consults a destination graph.</summary>
    public static class SpreadDiscoveryNotes
    {
        public const string Prefix = "SpreadDiscoveryNote.v1:";
        public const string ExpeditionPrefix = "SpreadDiscoveryNote.v2:";
        internal const int ExpeditionCapacity = 32;
        private const int MaxWireLength = 4096;
        internal const string PeatWorks = "sodden-peat-works";
        internal const string PlaceSubject = "place", PeatMalletSubject = "peat-mallet";
        internal const string WorksFormation = "SoddenDistrict";
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
            public string WorldKey, Subject;
        }

        internal static bool Remember(Entity player, Record record)
        {
            if (player == null || !Valid(record, record?.Family)) return false;
            string wire = JsonUtility.ToJson(record);
            if (wire.Length > MaxWireLength) return false;
            player.Properties[Prefix + record.Family] = wire;
            return true;
        }

        /// <summary>A report is authorized by its caller's current conversation;
        /// this boundary validates saved identity/shape and refuses new slots at
        /// capacity. Existing v1 bytes and malformed v2 properties are untouched.</summary>
        internal static bool Remember(Entity player, Record record, string currentWorldKey, out string refusal)
        {
            refusal = "discovery_note_invalid";
            if (player == null || !ValidExpedition(record, currentWorldKey)) return false;
            string key = ExpeditionKey(record);
            string wire = JsonUtility.ToJson(record);
            if (wire.Length > MaxWireLength) return false;
            if (!player.Properties.ContainsKey(key))
            {
                int stored = 0;
                foreach (string candidate in player.Properties.Keys)
                    if (candidate.StartsWith(ExpeditionPrefix, StringComparison.Ordinal) && ++stored >= ExpeditionCapacity)
                    { refusal = "discovery_note_capacity"; return false; }
            }
            player.Properties[key] = wire;
            refusal = null;
            return true;
        }

        /// <summary>Includes legacy notes and up to 32 validated reports belonging
        /// to the supplied saved world. Invalid context reveals only v1 notes.
        /// Reading neither repairs properties nor generates/inspects remote zones.</summary>
        public static IReadOnlyList<string> Read(Entity player, string currentWorldKey)
        {
            var result = new List<string>(Read(player));
            if (player == null || !ValidWorldKey(currentWorldKey)) return result;
            // Keep a bounded, deterministic selection even for an externally
            // malformed save containing more slots than the writer can create.
            var selected = new SortedDictionary<string, Record>(StringComparer.Ordinal);
            foreach (var property in player.Properties)
            {
                if (!property.Key.StartsWith(ExpeditionPrefix, StringComparison.Ordinal)
                    || string.IsNullOrEmpty(property.Value) || property.Value.Length > MaxWireLength) continue;
                try
                {
                    var record = JsonUtility.FromJson<Record>(property.Value);
                    if (!ValidExpedition(record, currentWorldKey) || property.Key != ExpeditionKey(record)) continue;
                    selected[property.Key] = record;
                    if (selected.Count > ExpeditionCapacity)
                    {
                        string last = null;
                        foreach (string key in selected.Keys) last = key;
                        selected.Remove(last);
                    }
                }
                catch (ArgumentException) { /* Optional corrupt history is ignored, never repaired. */ }
            }
            foreach (var record in selected.Values) result.Add(Describe(record));
            return result;
        }

        internal static bool ValidWorldKey(string key)
            => key != null && key.Length == 32 && Guid.TryParseExact(key, "N", out _);

        private static string ExpeditionKey(Record record)
            => ExpeditionPrefix + record.WorldKey + ":" + record.DestinationZoneID + ":" + record.Family;

        private static bool ValidExpedition(Record record, string currentWorldKey)
        {
            if (record == null || record.Version != 2 || !ValidWorldKey(currentWorldKey)
                || record.WorldKey != currentWorldKey || !ValidCommon(record)) return false;
            if (record.Family == PeatWorks)
                return record.Subject == PeatMalletSubject && record.DestinationZoneID == SoddenDistrictPlan.WorksZoneID
                    && record.Formation == WorksFormation;
            return record.Subject == PlaceSubject && ValidFamily(record, record.Family);
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
                || string.IsNullOrEmpty(wire) || wire.Length > MaxWireLength) return;
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
            => record != null && record.Version == 1 && record.Family == family
                && ValidCommon(record) && ValidFamily(record, family);

        private static bool ValidCommon(Record record)
            => CanonicalSurface(record.OriginZoneID) && CanonicalSurface(record.DestinationZoneID)
                && BoundedText(record.InformantID, 128) && BoundedText(record.InformantName, 160);

        private static bool ValidFamily(Record record, string family)
        {
            return (family == Pair || family == Viper || family == Wayhouse || FieldPlaceName(family) != null)
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
            string report = record.Family == PeatWorks
                ? "Old peat packers kept peat-packing mallet heads, oak hafts and leather bindings at the abandoned works beyond Sumphold. "
                    + "An assembled mallet can stagger foes it hurts, but deals little damage and struggles against armor. Cudgel techniques must still be learned. "
                    + "The dressing shelter lies one world-map cell south of Sumphold; the works lie two east of it, beyond the cutbank crossing."
                : record.Family == Pair
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
