using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Gives a Grovelands chunk its shape — the ringed groves, the
    /// tendril fens, the fruiting walls, the composting fields
    /// (Docs/FELLING-W4-PLAN.md W4.1; design doc §3.4).
    ///
    /// <para>Same contract as its three siblings: priority 2500,
    /// decorate-don't-dig, FNV-stable selection off the zone id,
    /// self-repair that removes only what this builder placed, and the
    /// every-open-cell-reachable property (W2's lesson). Canon does the
    /// reachability argument for us here: "Walk anywhere. Everything is
    /// path" (Lore/Codex/11_ChoirGroveSign.md) — a grove ring that
    /// seals its own seep would be a lore bug as much as a code
    /// bug.</para>
    ///
    /// <para>Per plan R9(a): every non-solid scatter placement goes
    /// through <see cref="BuilderSpawn.TryPlaceOnce"/> (the OpenMire
    /// stacking lesson); solids are gated by IsOpenGround, which a
    /// solid occupant fails naturally.</para>
    /// </summary>
    public sealed class GrovelandsFormationBuilder : IZoneBuilder
    {
        public string Name => "GrovelandsFormation";
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
                : FormationSelector.For(BiomeType.Grovelands, zone.ZoneID);
            LastFormation = formation;

            switch (formation)
            {
                case Formation.Grove: Grove(zone, factory, rng); break;
                case Formation.TendrilFen: TendrilFen(zone, factory, rng); break;
                case Formation.FruitingWall: FruitingWall(zone, factory, rng); break;
                case Formation.CompostingField: CompostingField(zone, factory, rng); break;
            }

            Diag.Record("worldgen", "FormationApplied", payload: new
            {
                zoneID = zone.ZoneID,
                biome = nameof(BiomeType.Grovelands),
                formation = formation.ToString(),
            });
            return true;
        }

        // ════════════════════════════════════════════════════════
        // The formations
        // ════════════════════════════════════════════════════════

        /// <summary>Radial: glowing columns ringing a clean seep, the
        /// grown sign at the east entry (canon: the sign stands "at the
        /// east entry of a Choir grove"). The ring is deliberately
        /// gapped — placement rolls per point — and the repair then
        /// PROVES every open cell reachable, because "everything is
        /// path" is the law here, not a hope. Lone columns scatter
        /// beyond the ring so the grove glows from across the chunk.</summary>
        private void Grove(Zone zone, EntityFactory factory, System.Random rng)
        {
            var placed = new List<Entity>();

            int cx = 18 + rng.Next(Zone.Width - 36);
            int cy = 8 + rng.Next(Zone.Height - 16);
            int rx = 6 + rng.Next(3);
            int ry = 3 + rng.Next(2);

            // The OPEN FLOOR — the design table's own words. Look-pass
            // finding: in real forest the ring drowned in trees and the
            // center could be BLOCKED, so the seep silently never placed
            // (a phantom-neutral pass the open-grass tests can't see).
            // The grove clears its own floor — the Causeway's ClearFor
            // precedent, and the one dig this builder is allowed.
            for (int x = cx - rx; x <= cx + rx; x++)
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    double nx = (x - cx) / (double)rx, ny = (y - cy) / (double)ry;
                    if (nx * nx + ny * ny > 1.0) continue;
                    ClearVegetation(zone, x, y);
                }

            // The ring: ellipse points (cells are taller than wide on
            // screen, so the x-radius runs longer), ~2/3 density.
            for (int step = 0; step < 28; step++)
            {
                double a = step * (System.Math.PI * 2.0 / 28.0);
                int x = cx + (int)System.Math.Round(rx * System.Math.Cos(a));
                int y = cy + (int)System.Math.Round(ry * System.Math.Sin(a));
                if (!IsOpenGround(zone, x, y)) continue;
                if (rng.Next(100) >= 65) continue;
                var column = BuilderSpawn.TryPlace(zone, factory, "MycelialColumn", x, y);
                if (column != null) placed.Add(column);
            }

            // The centre is the reason you came — GUARANTEED. W4.1
            // review: the clearing skips WALLS, and production terrain
            // is JungleBuilder VineWall — a center rolled inside a wall
            // blob (~1 grove in 16) entombed the seep. The one allowed
            // dig extends by exactly one cell: the water finds its way
            // up through anything.
            var centerCell = zone.GetCell(cx, cy);
            if (centerCell != null && !IsOpenGround(zone, cx, cy))
            {
                for (int i = centerCell.Objects.Count - 1; i >= 0; i--)
                {
                    var o = centerCell.Objects[i];
                    if (o != null && (o.HasTag("Wall") || o.HasTag("Solid")
                        || (o.GetPart<PhysicsPart>()?.Solid ?? false)))
                        zone.RemoveEntity(o);
                }
            }
            BuilderSpawn.TryPlaceOnce(zone, factory, "GroveSeep", cx, cy);

            // W4.2 — red growth at the grove edge, where the sign can
            // see it: "GroveRed ... Choir grove edges" (WORLD-
            // INGREDIENTS.md). Non-solid forage; the warning is on the
            // sign, not the ground.
            int reds = 2 + rng.Next(3);
            for (int i = 0; i < reds; i++)
            {
                for (int attempt = 0; attempt < 25; attempt++)
                {
                    double a = rng.NextDouble() * System.Math.PI * 2.0;
                    int x = cx + (int)System.Math.Round((rx + 1 + rng.Next(3)) * System.Math.Cos(a));
                    int y = cy + (int)System.Math.Round((ry + 1 + rng.Next(2)) * System.Math.Sin(a));
                    if (!IsOpenGround(zone, x, y)) continue;
                    BuilderSpawn.TryPlaceOnce(zone, factory, "GroveRedGrowth", x, y);
                    break;
                }
            }

            // Lone columns beyond the ring — the glow you steer by.
            int lone = 3 + rng.Next(4);
            for (int i = 0; i < lone; i++)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    int x = 3 + rng.Next(Zone.Width - 6);
                    int y = 2 + rng.Next(Zone.Height - 4);
                    int ddx = x - cx, ddy = y - cy;
                    if (ddx * ddx + ddy * ddy < rx * rx) continue;   // outside the grove proper
                    if (!IsOpenGround(zone, x, y)) continue;
                    var column = BuilderSpawn.TryPlace(zone, factory, "MycelialColumn", x, y);
                    if (column != null) placed.Add(column);
                    break;
                }
            }

            EnsureAllReachable(zone, placed);

            // The sign at the east entry, AFTER the repair — a solid the
            // repair must never remove (a grove without its law fails
            // the signature contract), so it proves its own placement:
            // place, flood, keep only if every open cell stays reachable.
            // Look-pass finding: three candidate cells all sat in forest
            // at real zones and the sign silently vanished. Now the
            // entry clears its own doorpost (the sign is GROWN from the
            // grove; the grove makes room for its law), walking an
            // eastern arc of candidates — and if rock refuses every one,
            // the sign stands on the cleared floor itself, where an open
            // disc guarantees some cell that seals nothing.
            bool signPlaced = false;
            for (int dy = 0; System.Math.Abs(dy) <= 2 && !signPlaced; dy = dy <= 0 ? -dy + 1 : -dy)
                for (int dx = rx + 1; dx <= rx + 4 && !signPlaced; dx++)
                {
                    ClearVegetation(zone, cx + dx, cy + dy);
                    signPlaced = PlaceSolidIfHarmless(zone, factory, "GroveSign", cx + dx, cy + dy) != null;
                }
            for (int x = cx + rx; x >= cx - rx && !signPlaced; x--)
                for (int y = cy - ry; y <= cy + ry && !signPlaced; y++)
                {
                    double nx = (x - cx) / (double)rx, ny = (y - cy) / (double)ry;
                    if (nx * nx + ny * ny > 1.0) continue;
                    if (x == cx && y == cy) continue;   // never on the seep
                    signPlaced = PlaceSolidIfHarmless(zone, factory, "GroveSign", x, y) != null;
                }
        }

        /// <summary>Braid: tendrils tracing old water-veins — sinuous
        /// permanent water lines with fruiting growth crowding their
        /// banks, and 1-3 ChoirTendrils seated on the lanes. The
        /// 37-node dialogue tree has existed since Phase E; this is
        /// where it finally spawns (the MawToad-in-the-copse pattern:
        /// the fen GUARANTEES its residents, tables may add more).</summary>
        private void TendrilFen(Zone zone, EntityFactory factory, System.Random rng)
        {
            var placed = new List<Entity>();
            // W4.1 review: one sine strand is a lane, not the design's
            // BRAID. Two phase-offset strands at different frequencies
            // cross and part — the braid geometry the word means.
            double phase1 = rng.NextDouble() * 6.28;
            double phase2 = rng.NextDouble() * 6.28;

            for (int x = 2; x < Zone.Width - 2; x++)
                for (int y = 2; y < Zone.Height - 2; y++)
                {
                    double vein1 = System.Math.Sin(x * 0.22 + phase1) * 6.0 + Zone.Height * 0.5;
                    double vein2 = System.Math.Sin(x * 0.15 + phase2) * 7.0 + Zone.Height * 0.5;
                    double dist = System.Math.Min(
                        System.Math.Abs(y - vein1), System.Math.Abs(y - vein2));
                    if (dist < 1.2)
                    {
                        // The old water-veins themselves.
                        if (IsOpenGround(zone, x, y))
                            zone.TileState.WriteCoating(x, y, "water", ZoneTileState.Permanent);
                    }
                    else if (dist < 3.0 && rng.Next(100) < 18 && IsOpenGround(zone, x, y))
                    {
                        // Growth crowds the banks.
                        var body = BuilderSpawn.TryPlace(zone, factory, "FruitingBody", x, y);
                        if (body != null) placed.Add(body);
                    }
                    else if (dist < 3.0 && rng.Next(100) < 4 && IsOpenGround(zone, x, y))
                    {
                        // W4.2 — and so does the red, along the veins.
                        BuilderSpawn.TryPlaceOnce(zone, factory, "GroveRedGrowth", x, y);
                    }
                }

            // The temptation (W4.2 reachability fix): choir iron
            // surfaces where the tendrils trace the old veins — the ONLY
            // surface source of the dig-law's own trigger. Rare, and the
            // sign has already told you.
            if (rng.Next(100) < 30)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    int x = 4 + rng.Next(Zone.Width - 8);
                    int y = 3 + rng.Next(Zone.Height - 6);
                    if (!IsOpenGround(zone, x, y)) continue;
                    var vein = BuilderSpawn.TryPlace(zone, factory, "ChoirIronVein", x, y);
                    if (vein != null) placed.Add(vein);
                    break;
                }
            }

            EnsureAllReachable(zone, placed);

            // The residents — seated AFTER the repair, because they are
            // the fen's guarantee and the repair must never breach a
            // pocket by deleting a resident. Each seat proves itself:
            // a stationary solid in a one-wide lane can seal it, so
            // place, flood, keep only if every open cell stays reachable
            // (else remove and try another spot).
            int tendrils = 1 + rng.Next(3);
            for (int t = 0; t < tendrils; t++)
            {
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    int x = 4 + rng.Next(Zone.Width - 8);
                    int y = 3 + rng.Next(Zone.Height - 6);
                    if (PlaceSolidIfHarmless(zone, factory, "ChoirTendril", x, y) != null)
                        break;
                }
            }
        }

        /// <summary>Rubble: short dense lines of vertical growth —
        /// harvest country. Solid walls in broken segments, so the
        /// repair earns its keep.</summary>
        private void FruitingWall(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Look-pass tuning: 7-10 lines at 85% survived forest
            // rejection as thin scatter, not "dense vertical growth".
            // More lines, fuller shelves.
            var placed = new List<Entity>();
            int lines = 10 + rng.Next(4);
            for (int l = 0; l < lines; l++)
            {
                int x = 5 + rng.Next(Zone.Width - 10);
                int y0 = 2 + rng.Next(Zone.Height - 12);
                int len = 4 + rng.Next(5);
                for (int y = y0; y < y0 + len && y < Zone.Height - 2; y++)
                {
                    if (!IsOpenGround(zone, x, y)) continue;
                    if (rng.Next(100) >= 90) continue;   // broken shelves
                    var body = BuilderSpawn.TryPlace(zone, factory, "FruitingBody", x, y);
                    if (body != null) placed.Add(body);
                }
            }
            EnsureAllReachable(zone, placed);
        }

        /// <summary>Ordered rows of the half-taken-back. The rows are
        /// straight; nothing made them straight. Non-solid — you can
        /// walk the rows, which is the point and the problem.</summary>
        private static void CompostingField(Zone zone, EntityFactory factory, System.Random rng)
        {
            int y0 = 4 + rng.Next(3);
            for (int y = y0; y < Zone.Height - 3; y += 4)
            {
                int start = 6 + rng.Next(8);
                int end = Zone.Width - 6 - rng.Next(8);
                for (int x = start; x < end; x++)
                {
                    if (rng.Next(100) >= 70) continue;
                    if (!IsOpenGround(zone, x, y)) continue;
                    BuilderSpawn.TryPlaceOnce(zone, factory, "CompostRow", x, y);
                }
            }
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

        /// <summary>Remove non-wall solids (trees, bushes, scrub) from a
        /// cell — the grove's floor is open BY IDENTITY (design table:
        /// "columns ringing a seep, open floor"). Walls stay: the grove
        /// grows around rock, it does not eat it.</summary>
        private static void ClearVegetation(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.IsWall()) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                var physics = e.GetPart<PhysicsPart>();
                bool solid = e.HasTag("Solid") || (physics != null && physics.Solid);
                if (solid) zone.RemoveEntity(e);
            }
        }

        /// <summary>Place a SOLID entity only if the placement does not
        /// make reachability WORSE — the post-repair placement rule for
        /// solids the repair must never remove (the sign, the seated
        /// tendrils). A DELTA contract, not a global one: the zone may
        /// already carry pockets this builder didn't cause and cannot
        /// fix (upstream scatter without a connectivity pass), and a
        /// global everything-reachable demand would veto every solid
        /// forever on such a zone. The placement is kept when the count
        /// of unreached open cells after is no higher than before
        /// (occupying one open cell is not sealing it). Returns the
        /// entity, or null when the cell was closed, the blueprint
        /// unknown, or the placement sealed something.</summary>
        private static Entity PlaceSolidIfHarmless(Zone zone, EntityFactory factory,
            string blueprint, int x, int y)
        {
            if (!IsOpenGround(zone, x, y)) return null;

            // W4.1 review: never occupy an ISOLATED open cell — filling
            // a one-cell pocket "improves" the unreached count while
            // entombing the placed thing (a sign nobody can ever read).
            bool hasOpenNeighbor = false;
            for (int dx = -1; dx <= 1 && !hasOpenNeighbor; dx++)
                for (int dy = -1; dy <= 1 && !hasOpenNeighbor; dy++)
                    if ((dx != 0 || dy != 0) && IsOpenGround(zone, x + dx, y + dy))
                        hasOpenNeighbor = true;
            if (!hasOpenNeighbor) return null;

            int before = UnreachedOpenCells(zone);
            var e = BuilderSpawn.TryPlace(zone, factory, blueprint, x, y);
            if (e == null) return null;
            if (UnreachedOpenCells(zone) <= before) return e;
            zone.RemoveEntity(e);
            return null;
        }

        private static int UnreachedOpenCells(Zone zone)
        {
            var reached = FloodFromWest(zone);
            int n = 0;
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (IsOpenGround(zone, x, y) && !reached[x, y]) n++;
            return n;
        }

        /// <summary>Every open cell must be reachable from the west
        /// flood. Breach by removing OWN placements only, preferring
        /// those that border both a reached and an unreached cell.</summary>
        private static void EnsureAllReachable(Zone zone, List<Entity> placed)
        {
            for (int guard = 0; guard < 200; guard++)
            {
                var reached = FloodFromWest(zone);
                if (FullyReached(zone, reached)) return;

                Entity best = null;
                foreach (var e in placed)
                {
                    var cell = zone.GetEntityCell(e);
                    if (cell == null) continue;
                    bool touchesReached = false, touchesUnreached = false;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = cell.X + dx, ny = cell.Y + dy;
                            if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                            var n = zone.GetCell(nx, ny);
                            if (n == null || n.BlocksMovement()) continue;
                            if (reached[nx, ny]) touchesReached = true;
                            else touchesUnreached = true;
                        }
                    if (touchesReached && touchesUnreached) { best = e; break; }
                    if (best == null && touchesUnreached) best = e;
                }

                if (best == null) return;   // nothing of ours borders the pocket
                zone.RemoveEntity(best);
                placed.Remove(best);
            }
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

        private static bool FullyReached(Zone zone, bool[,] reached)
        {
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (IsOpenGround(zone, x, y) && !reached[x, y]) return false;
            return true;
        }
    }
}
