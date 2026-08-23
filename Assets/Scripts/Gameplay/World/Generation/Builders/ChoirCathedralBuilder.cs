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

        /// <summary>Three ways in, evenly spaced. A vault that cannot be
        /// entered is a wall with ambitions.</summary>
        private static bool IsDoorway(int x) =>
            (x >= 20 && x <= 22) || (x >= 38 && x <= 40) || (x >= 56 && x <= 58);

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            // ── The vault: grown walls above and below the nave, so
            //    the room reads as a long hall rather than a cave.
            //
            //    Cold-eye 🔴: these were four UNBROKEN solid rows laid
            //    after ConnectivityBuilder had already certified the
            //    zone, which walled the nave off from everything north
            //    and south of it — a vault with no way in. A vault has
            //    doors; three gaps per band, and a delta-contract
            //    repair behind them (the SinkholeMouthBuilder lesson:
            //    only OUR damage is ours to repair).
            //
            //    Verify-pass correction: the first repair here asked
            //    FloodFromWest's CROSSED question — the wrong axis. The
            //    bands run east-west, so west→east crossing survives
            //    through the open margins even while the nave is sealed
            //    from the outer strips NORTH-SOUTH (e.g. terrain the
            //    strata left in the doorway columns). The contract is
            //    whole-zone reachability: if the zone had no walled-off
            //    pocket before us, it has none after us. Repair is
            //    TARGETED — remove a vault cell bordering the sealed
            //    region — because tail-popping walks the placement
            //    order, and the sealing cell may have been placed first.
            var before = FloodFromWest(zone, out bool crossedBefore);
            bool fullBefore = FullyReached(zone, before);
            var placed = new System.Collections.Generic.List<(Entity e, int x, int y)>();
            int vault = 0;
            foreach (int y in new[] { NaveTop - 2, NaveTop - 1,
                                      NaveBottom + 1, NaveBottom + 2 })
                for (int x = 6; x <= 73; x++)
                {
                    if (IsDoorway(x)) continue;
                    if (!IsOpenGround(zone, x, y)) continue;
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "SubstrateVault", x, y);
                    if (e != null) { vault++; placed.Add((e, x, y)); }
                }

            for (int attempt = 0; attempt < 300 && placed.Count > 0; attempt++)
            {
                var reached = FloodFromWest(zone, out bool crossed);
                // Repair to the strongest invariant that held BEFORE us:
                // whole-zone if the pipeline delivered whole-zone,
                // west-east crossing if it only delivered that.
                bool intact = fullBefore ? FullyReached(zone, reached)
                                         : (!crossedBefore || crossed);
                if (intact) break;

                int victim = -1;
                for (int i = placed.Count - 1; i >= 0; i--)
                {
                    var (_, px, py) = placed[i];
                    // A vault cell bordering open-but-unreached ground
                    // is part of the seal; removing it opens the wound.
                    for (int dx = -1; dx <= 1 && victim < 0; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = px + dx, ny = py + dy;
                            if (nx < 1 || ny < 1 || nx >= Zone.Width - 1
                                || ny >= Zone.Height - 1) continue;
                            if (IsOpenGround(zone, nx, ny) && !reached[nx, ny])
                            { victim = i; break; }
                        }
                    if (victim >= 0) break;
                }
                if (victim < 0) victim = placed.Count - 1; // no border found: fall back
                zone.RemoveEntity(placed[victim].e);
                placed.RemoveAt(victim);
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
