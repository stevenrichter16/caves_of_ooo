using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Optional fittings for a freshly built Gleaners' district.</summary>
    internal static class GleanersReceivingSite
    {
        private const string ParcelBlueprint = "LiningClayParcel";
        private const string ShelfBlueprint = "LiningClayReceivingShelf";

        internal static bool TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (zone?.ZoneID != GleanersDistrict.SurfaceID || manager?.Exploration is not { Version: >= 17 }
                || manager?.Factory == null || manager.CachedZones.ContainsKey(zone.ZoneID)
                || !manager.Factory.Blueprints.ContainsKey(ParcelBlueprint) || !manager.Factory.Blueprints.ContainsKey(ShelfBlueprint)
                || zone.GetReadOnlyEntities().Any(e => e.HasPart<DeliveryStoragePart>())) return false;

            var stairs = (x: GleanersDistrict.EntranceX, y: GleanersDistrict.EntranceY);
            var well = (x: 41, y: 8);
            if (!zone.GetCell(stairs.x, stairs.y).Objects.Any(e => e.GetProperty(GleanersDistrict.RoleKey) == "stairs")
                || !zone.GetCell(well.x, well.y).Objects.Any(e => e.GetProperty(GleanersDistrict.RoleKey) == "well")) return false;

            var loading = Neighbors(stairs).FirstOrDefault(p => Clear(zone, p));
            if (loading == default) return false;
            var receiving = Neighbors(well).FirstOrDefault(p => Clear(zone, p)
                && FindPath.Search(zone, loading.x, loading.y, p.x, p.y).Usable);
            if (receiving == default) return false;

            var original = new HashSet<Entity>(zone.GetReadOnlyEntities());
            var proof = SpreadGenerationReceipt.CaptureFinalState(zone, original);
            Entity parcel = null, shelf = null;
            bool committed = false;
            try
            {
                parcel = manager.Factory.CreateEntity(ParcelBlueprint);
                shelf = manager.Factory.CreateEntity(ShelfBlueprint);
                if (!Fresh(parcel, ParcelBlueprint, false) || !Fresh(shelf, ShelfBlueprint, true) || parcel.ID == shelf.ID
                    || original.Any(e => e.ID == parcel.ID || e.ID == shelf.ID) || !proof()
                    || !original.SetEquals(zone.GetReadOnlyEntities()) || !Clear(zone, loading) || !Clear(zone, receiving)) return false;

                if (!zone.AddEntity(parcel, loading.x, loading.y)) return false;
                var parcelProof = SpreadGenerationReceipt.CaptureFinalState(zone, new[] { parcel });
                if (!Clear(zone, receiving) || !zone.AddEntity(shelf, receiving.x, receiving.y)) return false;
                committed = proof() && parcelProof() && Fitting(shelf, ShelfBlueprint, true) && Fitting(parcel, ParcelBlueprint, false)
                    && zone.GetEntityPosition(parcel) == loading && zone.GetEntityPosition(shelf) == receiving
                    && original.Append(parcel).Append(shelf).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
                return committed;
            }
            catch (System.Exception error)
            {
                CavesOfOoo.Diagnostics.Diag.Record("worldgen", "ReceivingFittingsRefused", payload: new { zone = zone.ZoneID, error = error.GetType().Name });
                return false;
            }
            finally
            {
                if (!committed)
                {
                    if (parcel?.SpatialZone == zone && !original.Contains(parcel)) zone.RemoveEntity(parcel);
                    if (shelf?.SpatialZone == zone && !original.Contains(shelf)) zone.RemoveEntity(shelf);
                }
            }
        }

        private static bool Fresh(Entity owner, string blueprint, bool shelf) => owner?.SpatialZone == null && Fitting(owner, blueprint, shelf);

        private static bool Fitting(Entity owner, string blueprint, bool shelf) => owner != null && !string.IsNullOrEmpty(owner.ID)
            && owner.BlueprintName == blueprint && owner.GetPart<DeliveryStoragePart>() is DeliveryStoragePart delivery
            && delivery.ReceivesParcels == shelf && delivery.Intact(owner, owner.GetPart<ContainerPart>())
            && owner.GetPart<PhysicsPart>() is PhysicsPart p && p.ParentEntity == owner && p.Takeable != shelf
            && p.InInventory == null && p.Equipped == null && !p.Solid && !owner.HasTag("Solid")
            && owner.GetPart<RenderPart>()?.Visible == true && Empty(owner)
            && (shelf || owner.GetPart<ContainerLoadPart>()?.ParentEntity == owner && !owner.HasPart<StackerPart>());

        private static bool Empty(Entity owner) => owner.GetPart<ContainerPart>() is ContainerPart storage
            && storage.ParentEntity == owner && !storage.IsLocked && storage.Contents.Count == 0;

        private static bool Clear(Zone zone, (int x, int y) cell) => zone.InBounds(cell.x, cell.y)
            && !zone.GenReservedCells.Contains(cell) && !zone.GetCell(cell.x, cell.y).BlocksMovement()
            && zone.GetCell(cell.x, cell.y).Objects.All(e => e.BlueprintName is "Grass" or "RoadStone")
            && zone.TileState.Get(cell.x, cell.y)?.IsEmpty != false;

        private static (int x, int y)[] Neighbors((int x, int y) at) => new[]
        {
            (at.x + 1, at.y), (at.x - 1, at.y), (at.x, at.y + 1), (at.x, at.y - 1),
            (at.x + 1, at.y + 1), (at.x - 1, at.y + 1), (at.x + 1, at.y - 1), (at.x - 1, at.y - 1)
        };
    }
}
