using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>One optional recovered kit in the equipment shed of a newly generated Peat Works.</summary>
    internal static class PeatWorksStillRecovery
    {
        private const string CaseBlueprint = "PeatWorksStillCase";
        private const string StillBlueprint = "CampStill";
        private const string CatchRecipe = "field-case-catch";

        internal static void TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (zone.ZoneID != SoddenDistrictPlan.WorksZoneID || manager.CachedZones.ContainsKey(zone.ZoneID)
                || !manager.Factory.Blueprints.ContainsKey(CaseBlueprint) || !manager.Factory.Blueprints.ContainsKey(StillBlueprint)
                || RepairRecipeRegistry.Get(CatchRecipe) == null) return;

            var cell = zone.GetCell(63, 6);
            var approach = zone.GetCell(63, 7);
            if (!cell.IsInterior || cell.Objects.Count != 1 || cell.Objects[0].BlueprintName != "StoneFloor"
                || cell.BlocksMovement() || zone.TileState.Has(cell.X, cell.Y)
                || !approach.IsPassable() || approach.BlocksMovement() || zone.TileState.Has(approach.X, approach.Y)
                || !zone.GetCell(61, 6).Objects.Any(owner => owner.BlueprintName == "SoddenWorksLocker"
                    && owner.GetProperty(SoddenDistrictBuilder.RoleKey) == "locker")
                || zone.GetReadOnlyEntities().Any(owner => owner.BlueprintName == CaseBlueprint)) return;

            var fieldCase = manager.Factory.CreateEntity(CaseBlueprint);
            if (fieldCase?.GetPart<ContainerPart>() is not { MaxItems: 1 } container || container.Contents.Count != 0
                || fieldCase.GetPart<RepairablePart>() is not { RecipeId: CatchRecipe, Repaired: false, PortableEquipment: false }
                || fieldCase.GetPart<PhysicsPart>() is not { Solid: true, Takeable: false, InInventory: null, Equipped: null }
                || fieldCase.HasPart<DestructiblePart>() || fieldCase.HasPart<LockPart>() || fieldCase.SpatialZone != null) return;

            var still = manager.Factory.CreateEntity(StillBlueprint);
            if (still?.GetPart<PhysicsPart>() is not { Takeable: true, InInventory: null, Equipped: null }
                || still.SpatialZone != null || !PackableFixturePart.IsInertKit(still)
                || !still.HasPart<LiquidSettlingPart>() || !still.HasPart<AlchemyStillPart>()) return;

            if (container.AddItem(still)) zone.AddEntity(fieldCase, cell.X, cell.Y);
        }
    }
}
