using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Gives a Sodden chunk its shape — mire archipelagos, peat-cut
    /// trenches, reed mazes, drowned copses, the causeway, the bog-face
    /// (Docs/FELLING-W3-PLAN.md W3.1; design doc §3.2).
    ///
    /// <para>Same contract as its two siblings: priority 2500,
    /// decorate-don't-dig (except the causeway, which was here before
    /// the scrub), FNV-stable selection off the zone id, self-repair
    /// that removes only what this builder placed, and the
    /// every-open-cell-reachable property learned the hard way in W2's
    /// mid-review (a sealed pocket is invisible to a crossing test).</para>
    /// </summary>
    public sealed class SoddenFormationBuilder : IZoneBuilder
    {
        public string Name => "SoddenFormation";
        public int Priority => 2500;

        /// <summary>Set by tests to force a formation; otherwise derived
        /// from the zone ID.</summary>
        public Formation Override = Formation.None;

        public Formation LastFormation { get; private set; }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            Formation formation = Override != Formation.None
                ? Override
                : FormationSelector.For(BiomeType.Sodden, zone.ZoneID);
            LastFormation = formation;

            switch (formation)
            {
                case Formation.OpenMire: OpenMire(zone, factory, rng); break;
                case Formation.PeatCuts: PeatCuts(zone, factory, rng); break;
                case Formation.ReedMaze: ReedMaze(zone, factory, rng); break;
                case Formation.DrownedCopse: DrownedCopse(zone, factory, rng); break;
                case Formation.Causeway: Causeway(zone, factory, rng); break;
                case Formation.BogFace: BogFace(zone, factory, rng); break;
            }

            Diag.Record("worldgen", "FormationApplied", payload: new
            {
                zoneID = zone.ZoneID,
                biome = nameof(BiomeType.Sodden),
                formation = formation.ToString(),
            });
            return true;
        }

        // ════════════════════════════════════════════════════════
        // The formations
        // ════════════════════════════════════════════════════════

        /// <summary>Tussock paths over deep mire — route-picking, cell
        /// by cell. Mire is WALKABLE (a non-solid pool that coats and
        /// slows), so reachability is never at stake; the decision is
        /// whether to wade.</summary>
        private static void OpenMire(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Look-pass tuning (W3.1): the first cut's 5-8 small blobs
            // survived tree/rock rejection as ~2% of the zone — a forest
            // with puddles, not a bog. More and bigger blobs, plus a fine
            // scatter of lone pools, put standing water everywhere the
            // eye lands while the ground stays mostly crossable dry.
            int blobs = 9 + rng.Next(4);
            for (int b = 0; b < blobs; b++)
            {
                int cx = 5 + rng.Next(Zone.Width - 10);
                int cy = 3 + rng.Next(Zone.Height - 6);
                int radius = 2 + rng.Next(4);
                for (int x = cx - radius; x <= cx + radius; x++)
                    for (int y = cy - radius; y <= cy + radius; y++)
                    {
                        int dx = x - cx, dy = y - cy;
                        if (dx * dx + dy * dy > radius * radius) continue;
                        if (!IsOpenGround(zone, x, y)) continue;
                        if (rng.Next(100) < 80)
                            BuilderSpawn.TryPlace(zone, factory, "MirePool", x, y);
                    }
            }

            // The flood never fully drained: lone pools between the blobs.
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (rng.Next(100) < 3 && IsOpenGround(zone, x, y))
                        BuilderSpawn.TryPlace(zone, factory, "MirePool", x, y);
        }

        /// <summary>Harvested trenches: water-filled cuts between worked
        /// banks. W3.2 seats the Bog-Taken in the faces.</summary>
        private void PeatCuts(Zone zone, EntityFactory factory, System.Random rng)
        {
            var placed = new List<Entity>();
            int combs = 3 + rng.Next(3);
            int y0 = 4 + rng.Next(4);
            for (int c = 0; c < combs; c++)
            {
                int y = y0 + c * 5;
                if (y >= Zone.Height - 4) break;
                int start = 4 + rng.Next(8);
                int end = Zone.Width - 4 - rng.Next(8);

                for (int x = start; x < end; x++)
                {
                    // The bank above the cut, with worked gaps.
                    if (rng.Next(100) < 85 && IsOpenGround(zone, x, y))
                    {
                        var bank = BuilderSpawn.TryPlace(zone, factory, "PeatBank", x, y);
                        if (bank != null) placed.Add(bank);
                    }
                    // The flooded trench below it.
                    if (IsOpenGround(zone, x, y + 1))
                        BuilderSpawn.TryPlace(zone, factory, "MirePool", x, y + 1);
                }
            }
            EnsureAllReachable(zone, placed);
            SeatBogTaken(zone, factory, rng, placed);
        }

        /// <summary>One-cell channels braided through tall reeds. Reeds
        /// are non-solid (they hide, they don't block), so this is a
        /// sightline maze, not a wall maze.</summary>
        private static void ReedMaze(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int x = 2; x < Zone.Width - 2; x++)
                for (int y = 2; y < Zone.Height - 2; y++)
                {
                    if (!IsOpenGround(zone, x, y)) continue;
                    // Braid: channels are sinuous bands of clear ground;
                    // everything else is reeds with occasional water.
                    double band = System.Math.Sin(x * 0.35 + y * 0.9) + System.Math.Sin(y * 0.5);
                    if (band > -0.3)
                    {
                        if (rng.Next(100) < 78)
                            BuilderSpawn.TryPlace(zone, factory, "Reeds", x, y);
                        else if (rng.Next(100) < 25)
                            BuilderSpawn.TryPlace(zone, factory, "MirePool", x, y);
                    }
                }
        }

        /// <summary>Dead trees standing in shallow water — open but
        /// obstructed. Maw-Toads den here (W3.4 keys population on the
        /// formation's signature).</summary>
        private static void DrownedCopse(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int x = 2; x < Zone.Width - 2; x++)
                for (int y = 2; y < Zone.Height - 2; y++)
                {
                    if (!IsOpenGround(zone, x, y)) continue;
                    int roll = rng.Next(100);
                    if (roll < 7) BuilderSpawn.TryPlace(zone, factory, "DeadTree", x, y);
                    else if (roll < 30)
                        zone.TileState.WriteCoating(x, y, "water", ZoneTileState.Permanent);
                }
        }

        /// <summary>THE safe line. One duckboard walk, west to east,
        /// with mire pressing on both sides. The causeway is allowed to
        /// clear what stands on it — it was built first.</summary>
        private static void Causeway(Zone zone, EntityFactory factory, System.Random rng)
        {
            int y = 8 + rng.Next(Zone.Height - 16);
            int wobble = 0;
            for (int x = 1; x < Zone.Width - 1; x++)
            {
                if (rng.Next(100) < 15) wobble += rng.Next(3) - 1;
                int cy = y + System.Math.Clamp(wobble, -3, 3);
                ClearFor(zone, cy, x);
                BuilderSpawn.TryPlace(zone, factory, "Duckboard", x, cy);

                // Mire laps at the boards.
                for (int dy = -3; dy <= 3; dy++)
                {
                    if (dy >= -1 && dy <= 1) continue;
                    if (rng.Next(100) < 45 && IsOpenGround(zone, x, cy + dy))
                        BuilderSpawn.TryPlace(zone, factory, "MirePool", x, cy + dy);
                }
            }
        }

        /// <summary>A tall cut showing strata — the bank as a book.
        /// W3.2 seats bodies at every depth.</summary>
        private void BogFace(Zone zone, EntityFactory factory, System.Random rng)
        {
            var placed = new List<Entity>();
            int y = 6 + rng.Next(Zone.Height - 12);
            int start = 3 + rng.Next(6);
            int end = Zone.Width - 3 - rng.Next(6);
            for (int x = start; x < end; x++)
            {
                // The face itself: two rows of bank with worked breaks.
                if (rng.Next(100) < 90 && IsOpenGround(zone, x, y))
                {
                    var bank = BuilderSpawn.TryPlace(zone, factory, "PeatBank", x, y);
                    if (bank != null) placed.Add(bank);
                }
                if (rng.Next(100) < 70 && IsOpenGround(zone, x, y + 1))
                {
                    var bank = BuilderSpawn.TryPlace(zone, factory, "PeatBank", x, y + 1);
                    if (bank != null) placed.Add(bank);
                }
                // Water pools at the foot of the cut.
                if (rng.Next(100) < 50 && IsOpenGround(zone, x, y + 2))
                    BuilderSpawn.TryPlace(zone, factory, "MirePool", x, y + 2);
            }
            EnsureAllReachable(zone, placed);
            SeatBogTaken(zone, factory, rng, placed);
        }

        /// <summary>W3.2 — the Bog-Taken surface where the peat is
        /// worked: "a centuries-deep cemetery whose contents are
        /// visible" (Lore/History/02_Geography.md:69). 0-2 per cut
        /// zone, seated on open ground against a SURVIVING bank (the
        /// reachability repair may have removed some). Runs after the
        /// repair on purpose: a body must lie where the face still
        /// stands, and a body is non-solid — you stop at it, it never
        /// blocks. Ordinary drowned only; the three pre-Felling bodies
        /// are authored to the Drowned Ledger by hand.</summary>
        private static void SeatBogTaken(Zone zone, EntityFactory factory,
            System.Random rng, List<Entity> banks)
        {
            int bodies = rng.Next(3);   // 0-2 — the bog gives sparingly
            if (bodies == 0 || banks.Count == 0) return;

            var candidates = new List<(int x, int y)>();
            var seen = new HashSet<(int, int)>();
            foreach (var bank in banks)
            {
                var cell = zone.GetEntityCell(bank);
                if (cell == null) continue;   // breached by the repair
                Consider(zone, cell.X + 1, cell.Y, candidates, seen);
                Consider(zone, cell.X - 1, cell.Y, candidates, seen);
                Consider(zone, cell.X, cell.Y + 1, candidates, seen);
                Consider(zone, cell.X, cell.Y - 1, candidates, seen);
            }
            for (int b = 0; b < bodies && candidates.Count > 0; b++)
            {
                int i = rng.Next(candidates.Count);
                BuilderSpawn.TryPlace(zone, factory, "BogTakenBody",
                    candidates[i].x, candidates[i].y);
                candidates.RemoveAt(i);
            }
        }

        private static void Consider(Zone zone, int x, int y,
            List<(int x, int y)> candidates, HashSet<(int, int)> seen)
        {
            if (!IsOpenGround(zone, x, y)) return;
            if (!seen.Add((x, y))) return;
            candidates.Add((x, y));
        }

        // ════════════════════════════════════════════════════════
        // Shared guards (the W2 mid-review's full-reachability rule)
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

        /// <summary>Every open cell reachable, repaired by removing only
        /// banks this builder placed — the property W2's sealed-room
        /// finding proved crossing-tests cannot see.</summary>
        private static void EnsureAllReachable(Zone zone, List<Entity> placed)
        {
            for (int attempt = 0; attempt < 80 && placed.Count > 0; attempt++)
            {
                var reached = FloodFromWest(zone);
                if (FullyReached(zone, reached)) return;

                Entity breach = null;
                for (int i = placed.Count - 1; i >= 0 && breach == null; i--)
                {
                    var bank = placed[i];
                    var pos = zone.GetEntityPosition(bank);
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
                    if (bordersReached && bordersUnreached) breach = bank;
                }

                if (breach == null) return;
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
