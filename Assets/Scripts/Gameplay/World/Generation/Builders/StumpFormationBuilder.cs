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
                case Formation.Grainfield:    BuildGrainfield(zone, factory, rng, placed); break;
                case Formation.CascadeGorge:  BuildCascadeGorge(zone, factory, rng, placed); break;
                case Formation.ButtressRidge: BuildButtressRidge(zone, factory, rng, placed); break;
                case Formation.SummitScrub:   BuildSummitScrub(zone, factory, rng, placed); break;
                case Formation.RimForest:     BuildRimForest(zone, factory, rng, placed); break;
            }

            if (band == StumpBand.Slopes)
                StampTepuibone(zone, factory, rng, placed);

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

        private static void StampTepuibone(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            // Short seams follow the same east-west grain. They participate
            // in the formation's existing connectivity repair, and respect
            // authored reservations even in standalone/retry fixtures.
            for (int seam = 0; seam < 3; seam++)
            {
                int x = 4 + rng.Next(Zone.Width - 12);
                int y = 3 + rng.Next(Zone.Height - 6);
                for (int offset = 0; offset < 4; offset++)
                {
                    int px = x + offset;
                    if (!IsOpenGround(zone, px, y) || zone.GenReservedCells.Contains((px, y))) continue;
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "TepuiboneVein", px, y);
                    if (e != null) placed.Add((e, px, y));
                }
            }
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
        /// <summary>Petrified roots radiating downhill: ridge spokes
        /// fanning from the chunk's uphill (north) edge, gaps between
        /// the fingers. Reuses GrainRidge — a buttress IS grain, bent.</summary>
        private static void BuildButtressRidge(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            int spokes = 4 + rng.Next(3);
            for (int sIdx = 0; sIdx < spokes; sIdx++)
            {
                int originX = 8 + sIdx * (Zone.Width - 16) / spokes + rng.Next(5);
                double slope = -1.2 + sIdx * (2.4 / System.Math.Max(1, spokes - 1));
                for (int y = 2; y < Zone.Height - 2; y++)
                {
                    int x = originX + (int)System.Math.Round(slope * (y - 2));
                    if (x < 2 || x >= Zone.Width - 2) break;
                    if ((y - 2) % 6 == 5) continue;         // a gap per finger-length
                    if (!IsOpenGround(zone, x, y)) continue;
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "GrainRidge", x, y);
                    if (e != null) placed.Add((e, x, y));
                }
            }
        }

        /// <summary>Bromeliad scrub on stone domes: radial dome blobs
        /// with tank-brocchinia scattered in the lee of them.</summary>
        private static void BuildSummitScrub(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            int blobs = 4 + rng.Next(3);
            for (int b = 0; b < blobs; b++)
            {
                int cx = 8 + rng.Next(Zone.Width - 16);
                int cy = 4 + rng.Next(Zone.Height - 8);
                int r = 2 + rng.Next(2);
                for (int x = cx - r; x <= cx + r; x++)
                    for (int y = cy - r; y <= cy + r; y++)
                    {
                        double d = ((x - cx) * (x - cx)) / (double)(r * r)
                                 + ((y - cy) * (y - cy)) / (double)(r * r);
                        if (d > 1.0) continue;
                        if (!IsOpenGround(zone, x, y)) continue;
                        var e = BuilderSpawn.TryPlaceOnce(zone, factory, "StoneDome", x, y);
                        if (e != null) placed.Add((e, x, y));
                    }
            }
            int tanks = 0;
            for (int attempt = 0; attempt < 100 && tanks < 6; attempt++)
            {
                int x = 3 + rng.Next(Zone.Width - 6);
                int y = 3 + rng.Next(Zone.Height - 6);
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "TankBrocchinia", x, y) != null)
                    tanks++;
            }
        }

        /// <summary>The green crack: a winding band of dwarf forest —
        /// two rough tree lines with scrub between, crossable through
        /// the scrub (trees are solid; bushes are the way through).</summary>
        private static void BuildRimForest(Zone zone, EntityFactory factory,
            Random rng, System.Collections.Generic.List<(Entity, int, int)> placed)
        {
            int midY = Zone.Height / 2;
            double phase = rng.NextDouble() * System.Math.PI * 2;
            for (int x = 2; x < Zone.Width - 2; x++)
            {
                int wave = (int)System.Math.Round(3.0 * System.Math.Sin(phase + x * 0.18));
                int yTop = midY + wave - 3;
                int yBot = midY + wave + 3;
                // The two tree lines, broken every few cells.
                if (x % 4 != 0 && IsOpenGround(zone, x, yTop))
                {
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "Tree", x, yTop);
                    if (e != null) placed.Add((e, x, yTop));
                }
                if (x % 4 != 2 && IsOpenGround(zone, x, yBot))
                {
                    var e = BuilderSpawn.TryPlaceOnce(zone, factory, "Tree", x, yBot);
                    if (e != null) placed.Add((e, x, yBot));
                }
                // Scrub between: walkable green.
                int yMid = midY + wave + (rng.Next(3) - 1);
                if (IsOpenGround(zone, x, yMid))
                    BuilderSpawn.TryPlaceOnce(zone, factory, "Bush", x, yMid);
            }
        }
    }
}