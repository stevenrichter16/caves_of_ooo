using System;
using System.Runtime.CompilerServices;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Breadth B4: tier-keyed sensory log ambience, with saved first
    /// arrivals and the four enacted ending states. No cosmological explanation.</summary>
    public static class SariAmbience
    {
        public const string ArrivalPrefix = "SariArrival:";
        public const string SoftLine = "A soft sound slips through the air: sari... sari...";
        public const string ClearLine = "Sari... sari... The sound is clear in the air.";
        public const string LoudLine = "Sari... sari... The syllables carry through the place.";
        public const string PracticeLine = "Sari... sari... The pitch has changed; the sound carries on.";

        private sealed class Place
        {
            public string SourceId, CanonicalId, ArrivalKey;
            public int Tier;
            public uint Hash;
        }
        private static readonly ConditionalWeakTable<Zone, Place> Places = new ConditionalWeakTable<Zone, Place>();

        /// <summary>Immediate entry seam after successful player transfer.
        /// The first valid tier-five arrival per canonical zone bypasses spacing
        /// once; saved memory prevents replay on reentry or restoration.</summary>
        public static bool OnZoneEntered(Entity player, Zone zone)
        {
            if (!WorldAmbience.IsPresentPlayer(player, zone) || !AudibleEnding(player)) return false;
            var place = GetPlace(zone);
            if (place.Tier != 5 || player.GetIntProperty(place.ArrivalKey) != 0) return false;
            player.IntProperties[place.ArrivalKey] = 1;
            Emit(player, place, "arrival");
            return true;
        }

        /// <summary>Authored surface tier, plus one below ground, capped at five.
        /// Invalid, out-of-world and world-map IDs have no ambient tier.</summary>
        public static int TierFor(Zone zone) => zone == null ? 0 : GetPlace(zone).Tier;

        internal static bool TryPeriodic(Entity player, Zone zone, int turn)
        {
            if (!AudibleEnding(player)) return false;
            var place = GetPlace(zone);
            int chance = place.Tier == 3 ? 2 : place.Tier == 4 ? 5 : place.Tier == 5 ? 10 : 0;
            if (chance == 0 || Sample(place.Hash, turn) >= chance) return false;
            Emit(player, place, "periodic");
            return true;
        }

        private static bool AudibleEnding(Entity player)
        {
            int ending = EndingSpine.Enacted(player);
            return ending == 0 || ending == EndingSpine.PracticePath || ending == EndingSpine.KeptPath;
        }

        private static Place GetPlace(Zone zone)
        {
            var place = Places.GetValue(zone, _ => new Place());
            if (place.SourceId == zone.ZoneID) return place;
            place.SourceId = zone.ZoneID; place.Tier = 0; place.CanonicalId = place.ArrivalKey = null;
            var (x, y, depth) = WorldMap.FromZoneID(zone.ZoneID);
            if (!WorldMapAuthoring.InBounds(x, y) || depth < 0) return place;
            place.Tier = Math.Min(5, WorldMapAuthoring.TierAt(x, y) + (depth > 0 ? 1 : 0));
            place.CanonicalId = WorldMap.ToZoneID(x, y, depth);
            place.ArrivalKey = ArrivalPrefix + place.CanonicalId;
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < place.CanonicalId.Length; i++) hash = (hash ^ place.CanonicalId[i]) * 16777619;
                place.Hash = hash;
            }
            return place;
        }

        private static uint Sample(uint placeHash, int turn)
        {
            unchecked
            {
                uint value = placeHash ^ ((uint)turn * 0x9e3779b9u);
                value ^= value >> 16; value *= 0x7feb352du;
                value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
                return value % 100;
            }
        }

        private static void Emit(Entity player, Place place, string reason)
        {
            bool practice = EndingSpine.Enacted(player) == EndingSpine.PracticePath;
            string variant = practice ? "practice" : place.Tier == 3 ? "soft" : place.Tier == 4 ? "clear" : "loud";
            string line = practice ? PracticeLine : place.Tier == 3 ? SoftLine : place.Tier == 4 ? ClearLine : LoudLine;
            WorldAmbience.MarkEmission(player);
            Diag.Record("event", "SariHeard", actor: player,
                payload: new { zone = place.CanonicalId, tier = place.Tier, variant, reason });
            try { MessageLog.Add(line); }
            catch (Exception error)
            {
                // MessageLog already stored the line before invoking observers.
                // Keep its receipt/memory and avoid replaying a cosmetic event.
                Diag.Record("event", "AmbientMessageObserverFailed", actor: player,
                    payload: new { kind = "SariHeard", exception = error.GetType().Name });
            }
        }
    }
}
