using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Gives a Beating chunk its shape — salt pans, exposed ruins, dune
    /// belts, the caravan road, wind barrens, brine lenses
    /// (Docs/FELLING-W1-W2-PLAN.md W2.1; design doc §3.3).
    ///
    /// <para>Same contract as <see cref="SpreadFormationBuilder"/>:
    /// priority 2500 (after the terrain has carved the ground, before
    /// connectivity), formations decorate open ground and never dig —
    /// except the road, which was here before the sand was. Selection
    /// is FNV-stable off the zone id.</para>
    ///
    /// <para><b>Self-repair discipline, sharpened.</b> The Spread's
    /// repair pass breaches hedges by blueprint name; that is safe there
    /// because only the formation places hedges. The RUIN FIELD's walls
    /// share a blueprint with the biome's own terrain (SandstoneWall),
    /// so breaching by name could dig through the zone's bones. This
    /// builder therefore only ever removes walls it PLACED — it tracks
    /// them and, if the chunk stopped being crossable, deletes its own
    /// easternmost frontier wall until the crossing is back.</para>
    /// </summary>
    public sealed class BeatingFormationBuilder : IZoneBuilder
    {
        public string Name => "BeatingFormation";
        public int Priority => 2500;

        /// <summary>Set by tests to force a formation; otherwise derived
        /// from the zone ID.</summary>
        public Formation Override = Formation.None;

        /// <summary>Authored-zone knob: the tenth fire's pan carries no
        /// salt veins — every vein shares the fire's '*' glyph (the
        /// house vein convention), and THAT zone's one glyph must mean
        /// one thing. Set only by CreateTenthFirePipeline.</summary>
        public bool OmitSaltVeins = false;

        public Formation LastFormation { get; private set; }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            Formation formation = Override != Formation.None
                ? Override
                : FormationSelector.For(BiomeType.Beating, zone.ZoneID);
            LastFormation = formation;

            switch (formation)
            {
                case Formation.SaltPan: SaltPan(zone, factory, rng, OmitSaltVeins); break;
                case Formation.RuinField: RuinField(zone, factory, rng); break;
                case Formation.DuneBelt: DuneBelt(zone, factory, rng); break;
                case Formation.CaravanRoad: CaravanRoad(zone, factory, rng); break;
                case Formation.WindBarrens: WindBarrens(zone, factory, rng); break;
                case Formation.BrineLens: BrineLens(zone, factory, rng); break;
            }

            Diag.Record("worldgen", "FormationApplied", payload: new
            {
                zoneID = zone.ZoneID,
                biome = nameof(BiomeType.Beating),
                formation = formation.ToString(),
            });
            return true;
        }

        // ════════════════════════════════════════════════════════
        // The formations
        // ════════════════════════════════════════════════════════

        /// <summary>White glare and nothing to hide behind. Scatter is
        /// CLEARED (the pan's identity is exposure), crust ridges mark the
        /// polygon cracks, and the salt itself stands in minable outcrops
        /// — the supply half of the W2.7 economy.</summary>
        private static void SaltPan(Zone zone, EntityFactory factory, System.Random rng,
            bool omitVeins = false)
        {
            for (int x = 2; x < Zone.Width - 2; x++)
                for (int y = 2; y < Zone.Height - 2; y++)
                    ClearScatter(zone, x, y);

            // Crust ridgelines: short runs tracing the plate edges.
            int ridges = 6 + rng.Next(5);
            for (int r = 0; r < ridges; r++)
            {
                int x = 2 + rng.Next(Zone.Width - 6);
                int y = 2 + rng.Next(Zone.Height - 5);
                int len = 3 + rng.Next(4);
                bool horizontal = rng.Next(2) == 0;
                for (int i = 0; i < len; i++)
                {
                    int cx = horizontal ? x + i : x;
                    int cy = horizontal ? y : y + i;
                    if (IsOpenGround(zone, cx, cy))
                        BuilderSpawn.TryPlace(zone, factory, "SaltCrust", cx, cy);
                }
            }

            // The outcrops: 2-4, wherever the pan allows.
            int veins = omitVeins ? 0 : 2 + rng.Next(3);
            for (int v = 0; v < veins; v++)
            {
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int x = 3 + rng.Next(Zone.Width - 6);
                    int y = 3 + rng.Next(Zone.Height - 6);
                    if (!IsOpenGround(zone, x, y)) continue;
                    BuilderSpawn.TryPlace(zone, factory, "PaleSaltVein", x, y);
                    break;
                }
            }
        }

        /// <summary>The pre-Felling street grid, waist-high (canon: less
        /// flood-mud here, so the ruins were never buried —
        /// Lore/History/03_History.md:29). Rectangular footprints with
        /// door gaps; walls only ever go on open ground, and the builder
        /// removes its OWN walls if the chunk stops being crossable.</summary>
        private void RuinField(Zone zone, EntityFactory factory, System.Random rng)
        {
            var placed = new List<Entity>();
            int rooms = 3 + rng.Next(3);
            for (int r = 0; r < rooms; r++)
            {
                int w = 4 + rng.Next(5);
                int h = 3 + rng.Next(3);
                int x0 = 2 + rng.Next(Zone.Width - w - 4);
                int y0 = 2 + rng.Next(Zone.Height - h - 4);

                // Two door gaps over DISTINCT perimeter cells. The first
                // cut walked 2(w+h) steps over 2(w+h)-4 cells — each
                // corner visited twice — and gapB's antipodal offset
                // (w+h) mapped the corner indices exactly onto each
                // other, so a gap landing on any corner voided BOTH
                // doors and ~41% of rooms sealed (the W2 mid-review's
                // adversarial verifier measured it). Third occurrence of
                // the gaps-vs-walls coordinate-system bug class in this
                // codebase; the rule, again: a gap must be expressed in
                // the same coordinates as the thing it is a gap in —
                // and those coordinates must not alias.
                int perimeter = 2 * (w + h) - 4;
                int gapA = rng.Next(perimeter);
                int gapB = (gapA + perimeter / 2) % perimeter;

                int step = 0;
                for (int x = x0; x < x0 + w; x++)
                    TryWall(zone, factory, x, y0, ref step, gapA, gapB, placed);
                for (int y = y0 + 1; y < y0 + h; y++)
                    TryWall(zone, factory, x0 + w - 1, y, ref step, gapA, gapB, placed);
                for (int x = x0 + w - 2; x >= x0; x--)
                    TryWall(zone, factory, x, y0 + h - 1, ref step, gapA, gapB, placed);
                for (int y = y0 + h - 2; y >= y0 + 1; y--)
                    TryWall(zone, factory, x0, y, ref step, gapA, gapB, placed);
            }

            EnsureCrossable(zone, placed);
        }

        private static void TryWall(Zone zone, EntityFactory factory, int x, int y,
            ref int step, int gapA, int gapB, List<Entity> placed)
        {
            int at = step++;
            if (at == gapA || at == gapB) return;
            if (!IsOpenGround(zone, x, y)) return;
            var wall = BuilderSpawn.TryPlace(zone, factory, "SandstoneWall", x, y);
            if (wall != null) placed.Add(wall);
        }

        /// <summary>Wavy dune crest-lines, one per height band, with
        /// random standing gaps — occluded sightlines in soft country.
        ///
        /// <para>The first cut used a modulo for the gaps and free-rolled
        /// the band heights; the look pass showed why that fails: modulo
        /// gaps align into vertical seams across bands (a fence texture,
        /// not landforms) and free rolls clumped every band into one third
        /// of the zone. Bands are now stratified and the gaps are rolled.</para></summary>
        private static void DuneBelt(Zone zone, EntityFactory factory, System.Random rng)
        {
            int bands = 3 + rng.Next(2);
            for (int b = 0; b < bands; b++)
            {
                // Stratified: one crest per horizontal slice, jittered.
                int slice = (Zone.Height - 6) / bands;
                int y = 3 + b * slice + rng.Next(System.Math.Max(1, slice - 2));

                // A crest is a long but not full-width line.
                int start = 2 + rng.Next(10);
                int end = Zone.Width - 2 - rng.Next(10);

                int wobble = 0;
                int gapLeft = 0;
                int nextGapIn = 6 + rng.Next(8);
                for (int x = start; x < end; x++)
                {
                    if (gapLeft > 0) { gapLeft--; continue; }
                    if (--nextGapIn <= 0)
                    {
                        gapLeft = 2 + rng.Next(2);            // a 2-3 cell pass
                        nextGapIn = 6 + rng.Next(8);
                        continue;
                    }
                    if (rng.Next(100) < 35) wobble += rng.Next(3) - 1;
                    int cy = y + System.Math.Clamp(wobble, -2, 2);
                    if (IsOpenGround(zone, x, cy))
                        BuilderSpawn.TryPlace(zone, factory, "DuneCrest", x, cy);
                }
            }
        }

        /// <summary>The well-to-well lane: packed stones, waymarkers, and
        /// what the road cost. The road is the one formation allowed to
        /// cut through — it was here before the sand drifted over.</summary>
        private static void CaravanRoad(Zone zone, EntityFactory factory, System.Random rng)
        {
            bool eastWest = rng.Next(100) < 70;
            if (eastWest)
            {
                int y = 6 + rng.Next(Zone.Height - 12);
                for (int x = 1; x < Zone.Width - 1; x++)
                {
                    ClearFor(zone, y, x); ClearFor(zone, y + 1, x);
                    BuilderSpawn.TryPlace(zone, factory, "RoadStone", x, y);
                    BuilderSpawn.TryPlace(zone, factory, "RoadStone", x, y + 1);
                    if (x % 12 == 6 && IsOpenGround(zone, x, y - 1))
                        BuilderSpawn.TryPlace(zone, factory, "Signpost", x, y - 1);
                    if (rng.Next(100) < 4 && IsOpenGround(zone, x, y + 2))
                        BuilderSpawn.TryPlace(zone, factory, "Bones", x, y + 2);
                }
            }
            else
            {
                int x = 15 + rng.Next(Zone.Width - 30);
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    ClearFor(zone, y, x); ClearFor(zone, y, x + 1);
                    BuilderSpawn.TryPlace(zone, factory, "RoadStone", x, y);
                    BuilderSpawn.TryPlace(zone, factory, "RoadStone", x + 1, y);
                    if (y % 8 == 4 && IsOpenGround(zone, x - 1, y))
                        BuilderSpawn.TryPlace(zone, factory, "Signpost", x - 1, y);
                    if (rng.Next(100) < 4 && IsOpenGround(zone, x + 2, y))
                        BuilderSpawn.TryPlace(zone, factory, "Bones", x + 2, y);
                }
            }
        }

        /// <summary>Rock and dry brush — cover that burns, and the
        /// scorpions that live under it.</summary>
        private static void WindBarrens(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    if (!IsOpenGround(zone, x, y)) continue;
                    int roll = rng.Next(100);
                    if (roll < 6) BuilderSpawn.TryPlace(zone, factory, "Rock", x, y);
                    else if (roll < 14) BuilderSpawn.TryPlace(zone, factory, "DryBrush", x, y);
                }
        }

        /// <summary>Standing brine in the driest country — pools ringed
        /// with crust. The shipped BrinePool does the structural work
        /// (conductive, freezable, permanent coating).</summary>
        private static void BrineLens(Zone zone, EntityFactory factory, System.Random rng)
        {
            int pools = 2 + rng.Next(3);
            for (int p = 0; p < pools; p++)
            {
                int cx = 6 + rng.Next(Zone.Width - 12);
                int cy = 4 + rng.Next(Zone.Height - 8);
                int radius = 2 + rng.Next(2);
                for (int x = cx - radius - 1; x <= cx + radius + 1; x++)
                    for (int y = cy - radius - 1; y <= cy + radius + 1; y++)
                    {
                        int dx = x - cx, dy = y - cy;
                        int d2 = dx * dx + dy * dy;
                        if (!IsOpenGround(zone, x, y)) continue;
                        if (d2 <= radius * radius)
                        {
                            if (rng.Next(100) < 70)
                                BuilderSpawn.TryPlaceOnce(zone, factory, "BrinePool", x, y);
                        }
                        else if (d2 <= (radius + 1) * (radius + 1) && rng.Next(100) < 40)
                        {
                            BuilderSpawn.TryPlace(zone, factory, "SaltCrust", x, y);
                        }
                    }
            }
        }

        // ════════════════════════════════════════════════════════
        // Shared guards (mirrors SpreadFormationBuilder's, with the
        // remove-only-what-you-placed sharpening)
        // ════════════════════════════════════════════════════════

        private static bool IsOpenGround(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }

        private static void ClearFor(Zone zone, int y, int x)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                var physics = e.GetPart<PhysicsPart>();
                bool solid = e.HasTag("Solid") || (physics != null && physics.Solid);
                if (solid) zone.RemoveEntity(e);
            }
        }

        /// <summary>The pan clears LOOSE scatter only (cactus, rock,
        /// brush) — never terrain walls, which are the zone's bones and
        /// connectivity's business.</summary>
        private static void ClearScatter(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                if (e.BlueprintName == "Cactus" || e.BlueprintName == "Rock"
                    || e.BlueprintName == "DryBrush")
                    zone.RemoveEntity(e);
            }
        }

        /// <summary>The REAL property, learned twice over: not "the zone
        /// can be crossed" but "every open cell is reachable". Per-room
        /// door rules cannot see compositions — two overlapping rooms can
        /// seal a pocket even when each has its doors (a door can open
        /// flush into the other room's wall; the sealed-room detector
        /// test caught seed 21 doing exactly this after the corner fix).
        /// Repaired by removing ONLY walls this builder placed: any
        /// placed wall that borders both reached and unreached open
        /// ground is a valid breach.</summary>
        private static void EnsureCrossable(Zone zone, List<Entity> placed)
        {
            for (int attempt = 0; attempt < 80 && placed.Count > 0; attempt++)
            {
                var reached = FloodFromWest(zone);
                if (FullyReached(zone, reached)) return;

                Entity breach = null;
                for (int i = placed.Count - 1; i >= 0 && breach == null; i--)
                {
                    var wall = placed[i];
                    var pos = zone.GetEntityPosition(wall);
                    if (pos.x < 0) { placed.RemoveAt(i); continue; }
                    bool bordersReached = false, bordersUnreached = false;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = pos.x + dx, ny = pos.y + dy;
                            if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1)
                                continue;
                            if (reached[nx, ny]) bordersReached = true;
                            else if (IsOpenGround(zone, nx, ny)) bordersUnreached = true;
                        }
                    if (bordersReached && bordersUnreached) breach = wall;
                }

                if (breach == null) return;   // sealed by terrain, not by us
                zone.RemoveEntity(breach);
                placed.Remove(breach);
            }
        }

        private static bool FullyReached(Zone zone, bool[,] reached)
        {
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (IsOpenGround(zone, x, y) && !reached[x, y]) return false;
            return true;
        }

        private static bool[,] FloodFromWest(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            for (int y = 1; y < Zone.Height - 1; y++)
                if (IsOpenGround(zone, 1, y)) { seen[1, y] = true; queue.Enqueue((1, y)); }
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny] || !IsOpenGround(zone, nx, ny)) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return seen;
        }
    }
}
