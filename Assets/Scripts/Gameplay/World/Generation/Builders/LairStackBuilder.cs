using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>One staged floor. No global edge or durable owner is published
    /// until ZoneManager accepts the complete generation result.</summary>
    public sealed class LairStackBuilder : IZoneBuilder
    {
        private readonly OverworldZoneManager manager;
        private readonly LairStackRecord plan;
        public string Name => "LairStack";
        public int Priority => 2000;
        public LairStackBuilder(OverworldZoneManager manager, LairStackRecord plan)
        {
            this.manager = manager;
            this.plan = plan;
        }
        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            try
            {
                return Build(zone, factory, rng);
            }
            catch (Exception e) { return Reject(zone, "generation-exception:" + e.GetType().Name); }
        }
        private bool Build(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null || plan == null)
                return Reject(zone, "missing-context");
            int depth = WorldMap.GetDepth(zone.ZoneID);
            if (depth < 0 || depth > plan.FinalDepth || zone.ZoneID != plan.ZoneAt(depth) || (plan.GeneratedMask & (1 << depth)) != 0)
                return Reject(zone, "already-generated-or-wrong-zone");
            if (string.IsNullOrEmpty(plan.BossBlueprint) || !factory.Blueprints.ContainsKey(plan.BossBlueprint)
                || !factory.Blueprints.ContainsKey("StairsUp") || !factory.Blueprints.ContainsKey("StairsDown"))
                return Reject(zone, "missing-required-content");
            zone.GenReservedCells.Clear();
            string floor = plan.Biome == BiomeType.Beating ? "Sand" : "Grass";
            string wall = plan.Biome == BiomeType.Beating ? "SandstoneWall" : "VineWall";
            if (!factory.Blueprints.ContainsKey(floor) || !factory.Blueprints.ContainsKey(wall))
                return Reject(zone, "missing-palette");
            if (!new LairBuilder(plan.Biome, null).BuildZone(zone, factory, rng)
                || !new ConnectivityBuilder { FloorBlueprint = floor }.BuildZone(zone, factory, rng))
                return Reject(zone, "terrain-failed");
            Entity up = null, down = null, boss = null, reward = null, loose = null;
            if (depth > 0 && LairStacks.CounterpartExists(manager, plan, depth, true))
            {
                up = factory.CreateEntity("StairsUp");
                if (!PlaceAt(zone, up, 36, 12))
                    return Reject(zone, "up-placement-failed");
            }
            if (depth < plan.FinalDepth && LairStacks.CounterpartExists(manager, plan, depth, false))
            {
                down = factory.CreateEntity("StairsDown");
                if (!PlaceAt(zone, down, 44, 12))
                    return Reject(zone, "down-placement-failed");
            }
            if (up != null || down != null)
            {
                if (!new UndergroundRouteReservationBuilder().BuildZone(zone, factory, rng))
                    return Reject(zone, "arrival-path-failed");
            }
            else if (!ReserveClosedFloorRoutes(zone))
                return Reject(zone, "closed-floor-path-failed");
            if (depth == plan.FinalDepth)
            {
                boss = factory.CreateEntity(plan.BossBlueprint);
                if (boss == null || !boss.HasTag("Creature") || !TryPlace(zone, boss, rng, true))
                    return Reject(zone, "boss-placement-failed");
                reward = LairRewardBudget.Create(plan.Biome, plan.Tier, factory, rng, plan.FinalDepth);
                if (reward == null || !TryPlace(zone, reward, rng, true))
                    return Reject(zone, "reward-placement-failed");
                loose = LairRewardBudget.CreateLooseFind(plan.Biome, plan.Tier, factory, rng);
                if (loose != null)
                {
                    loose.Properties["LairLooseRewardSurface"] = plan.SurfaceID;
                    if (!TryPlace(zone, loose, rng, true))
                        return Reject(zone, "loose-reward-placement-failed");
                }
            }
            else
            {
                reward = LairRewardBudget.CreateApproach(plan.Biome, plan.Tier, depth, factory, rng);
                if (reward == null || !TryPlace(zone, reward, rng, false))
                    return Reject(zone, "approach-cache-placement-failed");
                reward.Properties["LairApproachCacheSurface"] = plan.SurfaceID;
            }
            // One finite encounter budget per stack: at most two guards per
            // floor, selected from the existing biome roll, without extra loot.
            var guards = PopulationTable.LairGuards(plan.Biome).Roll(rng, zone.ZoneID);
            int budget = Math.Min(2, guards.Count);
            for (int i = 0; i < budget; i++)
            {
                int index = rng.Next(guards.Count);
                string blueprint = guards[index];
                guards.RemoveAt(index);
                var guard = factory.CreateEntity(blueprint);
                if (guard != null && !TryPlace(zone, guard, rng, false))
                    return Reject(zone, "guard-placement-failed");
            }
            if (depth == plan.FinalDepth)
            {
                new LairPopulationBuilder(plan.Biome, null).BuildAmbushesOnly(zone, factory, rng);
                if (!new TrapPlacementBuilder().BuildZone(zone, factory, rng))
                    return Reject(zone, "trap-placement-failed");
            }
            if ((up != null && zone.GetEntityCell(up) == null) || (down != null && zone.GetEntityCell(down) == null)
                || (boss != null && zone.GetEntityCell(boss) == null) || (reward != null && zone.GetEntityCell(reward) == null)
                || (loose != null && zone.GetEntityCell(loose) == null))
                return Reject(zone, "owner-removed-during-generation");
            LairStacks.Stage(zone, plan, depth, boss, reward, up, down, loose);
            return true;
        }
        private static bool Safe(Zone zone, Entity entity, int x, int y)
        {
            if (entity == null || !zone.CanPlaceFootprint(entity, x, y))
                return false;
            var cells = zone.GetOccupiedCells(entity, x, y);
            if (cells.Count == 0)
                return false;
            foreach (var c in cells)
            {
                if (c == null || c.BlocksMovement() || zone.GenReservedCells.Contains((c.X, c.Y)))
                    return false;
                foreach (var owner in c.Occupants)
                    if (!owner.HasTag("Terrain") || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<LiquidPoolPart>())
                        return false;
            }
            return true;
        }
        private static bool PlaceAt(Zone zone, Entity entity, int x, int y) => Safe(zone, entity, x, y) && zone.AddEntity(entity, x, y);
        private static bool TryPlace(Zone zone, Entity entity, Random rng, bool central)
        {
            var candidates = new List<(int x, int y)>();
            for (int x = 2; x < Zone.Width - 2; x++)
                for (int y = 2; y < Zone.Height - 2; y++)
                    if (Safe(zone, entity, x, y) && (!central || x >= 35 && x <= 44 && y >= 9 && y <= 14))
                        candidates.Add((x, y));
            if (candidates.Count == 0)
                return false;
            var p = candidates[rng.Next(candidates.Count)];
            return Safe(zone, entity, p.x, p.y) && zone.AddEntity(entity, p.x, p.y);
        }
        // A removed counterpart intentionally leaves no local stair. Protect a
        // genuine floor route for lateral visitors, without inventing a stair.
        private static bool ReserveClosedFloorRoutes(Zone zone)
        {
            var origin = zone.GetCell(40, 12);
            if (origin.BlocksMovement())
                return false;
            var parents = new Dictionary<Cell, Cell>();
            var queue = new Queue<Cell>();
            var exits = new Cell[4];
            parents[origin] = null;
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.X == 0 && exits[0] == null)
                    exits[0] = c;
                if (c.X == 79 && exits[1] == null)
                    exits[1] = c;
                if (c.Y == 0 && exits[2] == null)
                    exits[2] = c;
                if (c.Y == 24 && exits[3] == null)
                    exits[3] = c;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var n = zone.GetCell(c.X + dx, c.Y + dy);
                        if (n != null && !parents.ContainsKey(n) && !n.BlocksMovement())
                        {
                            parents[n] = c;
                            queue.Enqueue(n);
                        }
                    }
            }
            if (exits.Any(e => e == null))
                return false;
            foreach (var exit in exits)
                for (var c = exit; c != null; c = parents[c])
                    zone.GenReservedCells.Add((c.X, c.Y));
            return true;
        }
        private static bool Reject(Zone zone, string reason)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "LairStackRejected", payload: new
                {
                    zone = zone?.ZoneID,
                    reason
                });
            return false;
        }
    }
}
