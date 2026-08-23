using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.5 — the Choir Cathedral floor: a substrate-grown vault and a
    /// node in the network that ties every Cathedral to every other
    /// (Lore/Factions/01_RotChoir.md:126-129).
    ///
    /// <para><b>Scope, from canon.</b> Two different things share this
    /// address. The Cathedral ARCHETYPE is generic — "some are large
    /// and old, some small and recent" (:126). The DEEPEST Cathedral is
    /// Tier 5, holds the Wedded's own body, and is "the
    /// most-difficult-to-earn audience with any of the Six gods"
    /// (:122, :218). This builder is the archetype. The god is W8's
    /// god-room work and lands on top of this, the same way Olderdeep's
    /// founding-village identity waits for W6.</para>
    ///
    /// <para>Shape: a nave down the middle with grown walls either side,
    /// the node at the head of it, elders held in the wall facing in.
    /// The nave stays walkable — a vault you cannot cross is a
    /// screenshot.</para>
    /// </summary>
    public sealed class ChoirCathedralBuilder : IZoneBuilder
    {
        public string Name => "ChoirCathedral";

        /// <summary>3100 — after ConnectivityBuilder, like every other
        /// floor archetype.</summary>
        public int Priority => 3100;

        public const int NaveTop = 9;
        public const int NaveBottom = 15;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            // ── The vault: grown walls above and below the nave, so the
            //    room reads as a long hall rather than a cave.
            int vault = 0;
            for (int x = 6; x <= 73; x++)
                foreach (int y in new[] { NaveTop - 2, NaveTop - 1,
                                          NaveBottom + 1, NaveBottom + 2 })
                {
                    if (!IsOpenGround(zone, x, y)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, "SubstrateVault", x, y) != null)
                        vault++;
                }

            // ── The node, at the head of the nave.
            int nodeX = 68, nodeY = (NaveTop + NaveBottom) / 2;
            for (int attempt = 0; attempt < 20; attempt++, nodeX--)
                if (IsOpenGround(zone, nodeX, nodeY)
                    && BuilderSpawn.TryPlaceOnce(zone, factory, "ChoirNode", nodeX, nodeY) != null)
                    break;

            // ── The elders, held in the wall, facing the nave.
            int elders = 0, want = 2 + rng.Next(2);
            for (int attempt = 0; attempt < 80 && elders < want; attempt++)
            {
                int x = 12 + rng.Next(50);
                int y = (rng.Next(2) == 0) ? NaveTop - 1 : NaveBottom + 1;
                // Stand them just inside the nave, against the grown wall.
                int fy = (y < nodeY) ? y + 1 : y - 1;
                if (!IsOpenGround(zone, x, fy)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "EncasedElder", x, fy) != null)
                    elders++;
            }

            // ── The grove's own face reaches this deep. ChoirTendril
            //    already ships with its 37-node tree (W4.1) — reuse, do
            //    not reinvent.
            int tendrils = 0;
            for (int attempt = 0; attempt < 60 && tendrils < 3; attempt++)
            {
                int x = 10 + rng.Next(55);
                int y = NaveTop + rng.Next(NaveBottom - NaveTop + 1);
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "ChoirTendril", x, y) != null)
                    tendrils++;
            }

            // ── Claim the vault against later spawners (the W5.4 lesson:
            //    PopulationBuilder runs after this and rolls snapjaws).
            int reserved = 0;
            for (int x = 6; x <= 73; x++)
                for (int y = NaveTop - 2; y <= NaveBottom + 2; y++)
                    if (zone.GenReservedCells.Add((x, y))) reserved++;

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "FloorArchetype", null, null,
                    new { zoneId = zone.ZoneID, archetype = "ChoirCathedral",
                          vault, elders, tendrils, reserved });
            return true;
        }
    }
}
