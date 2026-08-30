using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W6.2 (Docs/FELLING-W6-PLAN.md §3) — the tepui's terrain builder
    /// family. The mountain must read as ONE OBJECT: the Grainfield's
    /// ridges run the same compass direction in every slope chunk
    /// (east–west, a world constant — R6: the direction is not rolled),
    /// and the foothill gorges terrace the same way water actually
    /// falls.
    ///
    /// <para>Formation CHOICE is <see cref="FormationSelector.ForStump"/>
    /// — pure in the zone id. Intra-chunk layout uses the builder rng,
    /// like every other formation family.</para>
    ///
    /// <para>Priority 3100 — after ConnectivityBuilder (3000), the W5.1
    /// mouth lesson applied from the start; delta-contract repair in the
    /// W5.7-corrected shape (whole-zone when the pipeline delivered
    /// whole-zone, crossing otherwise; targeted victim selection).</para>
    /// </summary>
    public sealed class StumpFormationBuilder : IZoneBuilder
    {
        public string Name => "StumpFormation";
        public int Priority => 3100;

        /// <summary>Rows between ridge lines. The grain of a mile-wide
        /// bole is broad; five rows reads as grain, not as a maze.</summary>
        public const int RidgeSpacing = 5;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;
            if (!WorldMap.IsOverworldZoneID(zone.ZoneID)) return true;
            var (wx, wy, wz) = WorldMap.FromZoneID(zone.ZoneID);
            if (wz != 0) return true;

            var band = StumpBands.BandAt(wx, wy);
            var formation = FormationSelector.ForStump(band, zone.ZoneID);
            if (formation == Formation.None) return true;

            var before = FloodFromWest(zone, out bool crossedBefore);
            bool fullBefore = FullyReached(zone, before);
            var placed = new System.Collections.Generic.List<(Entity e, int x, int y)>();

            switch (formation)
            {
                case Formation.Grainfield:   BuildGrainfield(zone, factory, rng, placed); break;
                case Formation.CascadeGorge: BuildCascadeGorge(zone, factory, rng, placed); break;
            }

            // Delta contract, the W5.7-corrected shape: repair to the
            // strongest invariant that held before us, with TARGETED
            // victim selection (a cell bordering the sealed region).
            for (int attempt = 0; attempt < 300 && placed.Count > 0; attempt++)
            {
                var reached = FloodFromWest(zone, out bool crossed);
                bool intact = fullBefore ? FullyReached(zone, reached)
                                         : (!crossedBefore || crossed);
                if (intact) break;

                int victim = -1;
                for (int i = placed.Count - 1; i >= 0 && victim < 0; i--)
                {
                    var (_, px, py) = placed[i];
                    for (int dx = -1; dx <= 1 && victim < 0; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = px + dx, ny = py + dy;
                            if (nx < 1 || ny < 1 || nx >= Zone.Width - 1
                                || ny >= Zone.Height - 1) continue;
                            if (IsOpenGround(zone, nx, ny) && !reached[nx, ny])
                            { victim = i; break; }
                        }
                }
                if (victim < 0) victim = placed.Count - 1;
                zone.RemoveEntity(placed[victim].e);
                placed.RemoveAt(victim);
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "StumpFormation", null, null,
                    new { zoneId = zone.ZoneID, band = band.ToString(),
                          formation = formation.ToString(), placed = placed.Count });
            return true;
        }

        /// <summary>Parallel east–west ridge lines with rng-placed gaps
        /// — the grain. Solid GrainRidge cells; every ridge line keeps
        /// at least two gaps so the repair loop is a fence, not the
        /// mechanism.</summary>
        private static void BuildGrainfield(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            for (int y = 3; y < Zone.Height - 2; y += RidgeSpacing)
            {
                // Two 2-3 cell gaps per ridge, never at the borders.
                int gapA = 6 + rng.Next(25);
                int gapB = 42 + rng.Next(28);
                for (int x = 2; x < Zone.Width - 2; x++)
                {
                    if (x >= gapA && x <= gapA + 2) continue;
                    if (x >= gapB && x <= gapB + 2) continue;
                    if (!IsOpenGround(zone, x, y)) continue;
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "GrainRidge", x, y);
                    if (e != null) placed.Add((e, x, y));
                }
            }
        }

        /// <summary>The gorge: a terraced cut down the chunk's middle
        /// third, shelves stepping east, spray pools clustered where
        /// the water lands.</summary>
        private static void BuildCascadeGorge(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            int gorgeX = 25 + rng.Next(20);
            // Shelves: short east-west ledges stepping down the cut.
            for (int y = 3; y < Zone.Height - 3; y += 4)
            {
                int span = 4 + rng.Next(4);
                for (int i = 0; i < span; i++)
                {
                    int x = gorgeX + i - span / 2;
                    if (!IsOpenGround(zone, x, y)) continue;
                    // Ledges are WALKABLE — no reachability stake, no
                    // placed-list entry.
                    BuilderSpawn.TryPlaceOnce(zone, factory, "DescentLedge", x, y);
                }
            }
            // Spray pools: wet clusters at the gorge's foot.
            int pools = 0;
            for (int attempt = 0; attempt < 120 && pools < 10; attempt++)
            {
                int x = gorgeX - 6 + rng.Next(13);
                int y = Zone.Height / 2 + rng.Next(Zone.Height / 2 - 3);
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "SprayPool", x, y) != null)
                    pools++;
            }
        }
    }
}
