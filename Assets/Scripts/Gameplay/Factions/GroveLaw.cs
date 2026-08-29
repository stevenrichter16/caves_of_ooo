using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W4.2 (Docs/FELLING-W4-PLAN.md §3) — the grove-edge sign's law as
    /// mechanics. The sign asks two things with teeth: "Do not dig"
    /// and — implicit in what the grove IS — do not burn it. Both are
    /// RotChoir standing hits on the player, immediate, with a diag
    /// record naming the act ("Choir reputation is act-based and never
    /// forgets", design §3.4).
    ///
    /// <para><b>Only the player answers to the law</b> (PlayerReputation
    /// is the player's ledger; NPC arson is the Choir's own problem),
    /// and only on GROVELANDS SURFACE ground — the biome read statically
    /// off the zone id, so the law needs no manager instance.</para>
    ///
    /// <para><b>Fire charges per ignition.</b> A spreading blaze charges
    /// only the first arson (propagation events carry the burning
    /// entity as Source, not the player) — but a multi-target fire
    /// spell charges once per thing set alight. Deliberate: the Choir
    /// counts acts, and burning five columns is five acts.</para>
    /// </summary>
    public static class GroveLaw
    {
        /// <summary>Digging is an insult.</summary>
        public const int DigRepLoss = -15;

        /// <summary>Fire is a crime.</summary>
        public const int FireRepLoss = -40;

        /// <summary>Is this zone Choir ground — a Grovelands SURFACE
        /// chunk? False for null, non-overworld ids, underground
        /// levels, and every other biome.</summary>
        public static bool IsGroveGround(Zone zone)
        {
            if (zone == null || string.IsNullOrEmpty(zone.ZoneID)) return false;
            if (!WorldMap.IsOverworldZoneID(zone.ZoneID)) return false;
            var (x, y, z) = WorldMap.FromZoneID(zone.ZoneID);
            if (z != 0) return false;
            if (!WorldMapAuthoring.InBounds(x, y)) return false;
            return WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Grovelands;
        }

        /// <summary>Close-out hypothesis H7 — the z != 0 gate above
        /// meant torching the Deepest Cathedral's own substrate billed
        /// NOTHING while a campfire in any surface grove billed −40.
        /// The Choir's law reaches everything the Choir IS: grove
        /// surface, or a Cathedral floor (z=2 of a mouth whose
        /// archetype is ChoirCathedral — a pure static read, like the
        /// biome).</summary>
        public static bool IsChoirGround(Zone zone)
        {
            if (IsGroveGround(zone)) return true;
            if (zone == null || string.IsNullOrEmpty(zone.ZoneID)) return false;
            if (!WorldMap.IsOverworldZoneID(zone.ZoneID)) return false;
            var (x, y, z) = WorldMap.FromZoneID(zone.ZoneID);
            if (z != 2) return false;
            foreach (var (name, mx, my) in SinkholeSites.All)
                if (mx == x && my == y)
                    return SinkholeArchetypes.For(name) == SinkholeArchetype.ChoirCathedral;
            return false;
        }

        /// <summary>The actor harvested something out of the ground.
        /// Charges only when the target is a MINERAL VEIN (tagged) —
        /// foraging a growth is not digging — and only the player,
        /// and only on grove ground.</summary>
        public static void OnDig(Entity actor, Zone zone, Entity target)
        {
            if (actor == null || !actor.HasTag("Player")) return;
            if (target == null || !target.HasTag("MineralVein")) return;
            if (!IsChoirGround(zone)) return;

            PlayerReputation.Modify("RotChoir", DigRepLoss);
            MessageLog.Add("The ground closes very slowly over the wound. Something has noticed the digging.");
            if (Diag.IsChannelEnabled("faction"))
                Diag.Record("faction", "GroveDug", actor, target,
                    new { repLoss = DigRepLoss, zoneID = zone.ZoneID });
        }

        /// <summary>Something the player set alight caught, on grove
        /// ground. Propagation is not re-charged: spread events carry
        /// the burning entity as their source, which fails the player
        /// gate here.</summary>
        public static void OnIgnite(Entity source, Entity burned, Zone zone)
        {
            if (source == null || !source.HasTag("Player")) return;
            if (!IsChoirGround(zone)) return;

            PlayerReputation.Modify("RotChoir", FireRepLoss);
            MessageLog.Add("Fire, in a grove. Every column's light leans toward it. The singing does not stop, which is worse.");
            if (Diag.IsChannelEnabled("faction"))
                Diag.Record("faction", "GroveBurned", source, burned,
                    new { repLoss = FireRepLoss, zoneID = zone.ZoneID });
        }
    }
}
