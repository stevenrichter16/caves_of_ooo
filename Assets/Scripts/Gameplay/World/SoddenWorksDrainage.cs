using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Cold optional mire at an existing break in the Peat Works shed.
    /// Ordinary liquid owners and movement govern everything after placement.</summary>
    internal static class SoddenWorksDrainage
    {
        private const int FirstVersion = 27;
        private const int NorthWall = 3;
        private const int Outside = 2;
        private const int Volume = 12;
        private const int MinimumSaving = 3;
        private const string Mire = "bog-mire";
        private static readonly (int x, int y) NorthApproach = (40, 0);
        private static readonly (int x, int y) DoorApproach = (54, 12);
        private static readonly (int x, int y) Rig = (56, 5);
        private static readonly HashSet<OverworldZoneManager> Installing = new();

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (!Eligible(zone, manager) || !TryChooseGap(zone, out int gap) || !Installing.Add(manager)) return;

            var plan = manager.Exploration;
            var original = new HashSet<Entity>(zone.GetReadOnlyEntities());
            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, original);
            var template = MaterialProcessTemplate.Capture(manager.Factory, LiquidVesselService.PoolBlueprint);
            Entity left = null, right = null;
            bool installed = false;
            try
            {
                left = CreatePool(manager.Factory, template);
                right = CreatePool(manager.Factory, template);
                if (left == null || right == null || left.ID == right.ID
                    || original.Any(owner => owner.ID == left.ID || owner.ID == right.ID)
                    || manager.Exploration != plan || !Eligible(zone, manager)
                    || !unchanged() || !original.SetEquals(zone.GetReadOnlyEntities())
                    || !TryChooseGap(zone, out int current) || current != gap) return;

                if (!zone.AddEntity(left, gap - 1, NorthWall) || !zone.AddEntity(right, gap, NorthWall)) return;
                installed = unchanged() && At(zone, left, gap - 1) && At(zone, right, gap)
                    && original.Append(left).Append(right).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
            }
            catch (Exception error)
            {
                Diag.Record("worldgen", "PeatWorksMireRefused", payload: new { error = error.GetType().Name });
            }
            finally
            {
                if (!installed)
                {
                    if (left?.SpatialZone == zone && !original.Contains(left)) zone.RemoveEntity(left);
                    if (right?.SpatialZone == zone && !original.Contains(right)) zone.RemoveEntity(right);
                }
                Installing.Remove(manager);
            }
        }

        private static bool Eligible(Zone zone, OverworldZoneManager manager) =>
            zone?.ZoneID == SoddenDistrictPlan.WorksZoneID
            && manager?.Exploration is { Enabled: true, Version: >= FirstVersion }
            && !manager.CachedZones.ContainsKey(zone.ZoneID) && SoddenDistrict.Eligible(manager, zone.ZoneID)
            && LiquidRegistry.IsInitialized && LiquidRegistry.Get(Mire) != null
            && manager.Factory.Blueprints.ContainsKey(LiquidVesselService.PoolBlueprint)
            && zone.GetCell(Rig.x, Rig.y).Objects.Any(e => e.BlueprintName == FieldWorkRigRules.Blueprint
                && e.GetProperty(SoddenDistrictBuilder.RoleKey) == "field-work-rig");

        private static Entity CreatePool(CavesOfOoo.Data.EntityFactory factory, MaterialProcessTemplate template)
        {
            if (template == null) return null;
            var owner = factory.CreateEntity(LiquidVesselService.PoolBlueprint);
            if (owner?.SpatialZone != null || string.IsNullOrEmpty(owner?.ID) || template.Snapshot(owner) == null
                || owner.GetPart<PhysicsPart>() is not { Solid: false, Takeable: false, InInventory: null, Equipped: null }
                || !owner.HasTag("Terrain") || owner.GetPart<LiquidPoolPart>() is not { Volume: 0, LiquidId: "" } pool
                || owner.GetPart<RenderPart>() is not { Visible: true } render) return null;
            pool.LiquidId = Mire;
            pool.Volume = Volume;
            pool.Initialize();
            render.DisplayName = "shallow bog mire";
            return owner;
        }

        private static bool At(Zone zone, Entity pool, int x) =>
            zone.GetEntityPosition(pool) == (x, NorthWall)
            && pool.GetPart<LiquidPoolPart>() is { Volume: Volume, LiquidId: Mire }
            && zone.TileState.HasCoating(x, NorthWall, Mire);

        private static bool Bare(Zone zone, int x, int y, string blueprint)
        {
            var cell = zone.GetCell(x, y);
            return cell.Objects.Count == 1 && cell.Objects[0].BlueprintName == blueprint && !cell.IsInterior
                && !zone.GenReservedCells.Contains((x, y)) && Dry(zone, cell);
        }

        private static bool Dry(Zone zone, Cell cell) => cell.IsPassable() && !cell.BlocksMovement()
            && !zone.TileState.Has(cell.X, cell.Y)
            && !cell.Occupants.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasTag("Creature"));

        private static bool TryChooseGap(Zone zone, out int gap)
        {
            gap = 0;
            var dry = new bool[Zone.Width, Zone.Height];
            foreach (var cell in zone.Cells) dry[cell.X, cell.Y] = Dry(zone, cell);
            int doorWalk = Distance(dry, DoorApproach, Rig, true);
            if (doorWalk < 0 || !dry[54, 10]) return false;

            for (int x = 50; x <= 60; x++)
            {
                if (!Bare(zone, x - 1, NorthWall, "StoneFloor") || !Bare(zone, x, NorthWall, "StoneFloor")
                    || !Bare(zone, x, Outside, "Grass") || !Bare(zone, x + 1, Outside, "Grass")) continue;

                var left = (x - 1, NorthWall);
                var right = (x, NorthWall);
                var stand = (x, Outside);
                var outlet = (x + 1, Outside);
                int detour = Distance(dry, NorthApproach, Rig, true, left, right);
                int walkToWork = Distance(dry, NorthApproach, stand, false, left, right);
                int clearedWalk = Distance(dry, stand, Rig, true, left, outlet);
                if (detour < 0 || walkToWork < 0 || clearedWalk < 0
                    || detour - (walkToWork + 1 + clearedWalk) < MinimumSaving
                    || Distance(dry, DoorApproach, Rig, true, left, right) != doorWalk
                    || Distance(dry, DoorApproach, Rig, true, left, outlet) != doorWalk) continue;
                gap = x;
                return true;
            }
            return false;
        }

        private static int Distance(bool[,] dry, (int x, int y) start, (int x, int y) goal, bool adjacent,
            (int x, int y)? firstWet = null, (int x, int y)? secondWet = null)
        {
            if (!dry[start.x, start.y] || start == firstWet || start == secondWet) return -1;
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<((int x, int y) at, int length)>();
            seen[start.x, start.y] = true;
            queue.Enqueue((start, 0));
            while (queue.Count > 0)
            {
                var (at, length) = queue.Dequeue();
                if (Math.Max(Math.Abs(at.x - goal.x), Math.Abs(at.y - goal.y)) <= (adjacent ? 1 : 0)) return length;
                for (int y = at.y - 1; y <= at.y + 1; y++)
                    for (int x = at.x - 1; x <= at.x + 1; x++)
                    {
                        var next = (x, y);
                        if (x < 0 || x >= Zone.Width || y < 0 || y >= Zone.Height || seen[x, y] || !dry[x, y]
                            || next == firstWet || next == secondWet) continue;
                        seen[x, y] = true;
                        queue.Enqueue((next, length + 1));
                    }
            }
            return -1;
        }
    }
}
