using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One optional clay source for the connected wayside kitchen.</summary>
    internal static class WaysideShipmentSite
    {
        private static readonly (int x, int y)[] Cardinal = { (1, 0), (0, 1), (-1, 0), (0, -1) };
        private const int SupplyBudget = 8;

        internal static bool TryInstall(Zone zone, OverworldZoneManager manager)
        {
            if (zone?.ZoneID != KitchenBatchPart.KitchenZoneID || manager?.Exploration is not { Enabled: true, Version: >= 18 }
                || manager.Exploration.DispositionFor(zone.ZoneID) != 2 || manager.CachedZones.ContainsKey(zone.ZoneID)) return false;
            var factory = manager.Factory;
            var original = zone.GetReadOnlyEntities().ToHashSet();
            var merchants = original.Where(e => e.BlueprintName == "SpreadWaysideCook"
                && e.GetProperty("SpreadResident.Role") == "resident" && e.HasPart<CookIntroductionPart>()).Take(2).ToArray();
            var chairs = original.Where(e => e.GetProperty("SpreadResident.Role") == "chair"
                && e.HasPart<ChairPart>()).Take(2).ToArray();
            if (factory == null || merchants.Length != 1 || !LocalGatheringClaims.Alive(merchants[0])
                || chairs.Length != 1
                || original.Any(e => e.HasPart<LocalShipmentReceiverPart>() || e.HasPart<LocalShipmentSponsorPart>())) return false;
            var merchant = merchants[0];
            var at = zone.GetEntityPosition(merchant);
            var seat = zone.GetEntityPosition(chairs[0]);
            if (manager.Exploration.Version >= 19
                && SpreadExplorationPlan.Rank(manager.WorldSeed, zone.ZoneID, "wayside-clay-work-v19") % 2 == 0)
                return TryInstallSortingLot(zone, manager, merchant, original);
            var shelfAt = Cardinal.Select(d => Add(at, d)).FirstOrDefault(p => Plain(zone, p, court: true)
                && Math.Abs(p.x - seat.x) <= 1 && Math.Abs(p.y - seat.y) <= 1);
            if (shelfAt == default) return false;
            var proof = SpreadGenerationReceipt.CaptureFinalState(zone, original);
            var names = new[] { "WaysideClayLoadingMarker", "WaysideClayPorter", "WaysideClaySupplier",
                "WaysideClayReceivingShelf", "LiningClayParcel", "PreparedLiningClay" };
            if (names.Any(name => !factory.Blueprints.ContainsKey(name))) return false;
            var placed = new Entity[5];
            bool committed = false;
            try
            {
                var marker = placed[0] = factory.CreateEntity(names[0]);
                var worker = placed[1] = factory.CreateEntity(names[1]);
                var supplier = placed[2] = factory.CreateEntity(names[2]);
                var shelf = placed[3] = factory.CreateEntity(names[3]);
                var parcel = placed[4] = factory.CreateEntity(names[4]);
                var clay = factory.CreateEntity(names[5]);
                var staged = placed.Append(clay).ToArray();
                if (staged.Any(e => !Fresh(e)) || staged.Select(e => e.ID).Distinct().Count() != staged.Length
                    || staged.Any(e => original.Any(owner => owner.ID == e.ID)) || !proof()
                    || !original.SetEquals(zone.GetReadOnlyEntities())
                    || marker.GetPart<LocalPorterLedgerPart>() is not { } order
                    || worker.GetPart<LocalPorterPart>() is not { } porter
                    || supplier.GetPart<LocalShipmentSponsorPart>() is not { } sponsor
                    || shelf.GetPart<DeliveryStoragePart>() is not { ReceivesParcels: true } receiving
                    || shelf.GetPart<ContainerPart>() is not { IsLocked: false } shelfStorage
                    || !receiving.Intact(shelf, shelfStorage) || shelfStorage.Contents.Count != 0
                    || shelf.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false }
                    || marker.GetPart<PhysicsPart>() is not { Takeable: false, Solid: false }
                    || !LocalGatheringClaims.Alive(worker) || !LocalGatheringClaims.Alive(supplier)
                    || parcel.GetPart<ContainerPart>() is not { IsLocked: false } cargo || cargo.Contents.Count != 0
                    || clay.GetPart<StackerPart>() is not { StackCount: 1, MaxStack: >= 2 } stack
                    || clay.GetPart<CommercePart>() is not { Value: > 0 } || !Plain(zone, shelfAt, court: true)) return false;

                stack.StackCount = 2;
                if (!cargo.AddItem(clay) || DeliveryStoragePart.ParcelWeightOrZero(parcel, zone) == 0
                    || !zone.AddEntity(shelf, shelfAt.x, shelfAt.y)) return false;
                foreach (var placement in LoadingPlaces(zone, at))
                {
                    if (!zone.AddEntity(supplier, placement.supplier.x, placement.supplier.y)) continue;
                    if (zone.AddEntity(worker, placement.worker.x, placement.worker.y)
                        && LocalPorterPart.Route(worker, shelf, zone) is { Count: > 0 }
                        && zone.AddEntity(marker, placement.loading.x, placement.loading.y)
                        && zone.AddEntity(parcel, placement.loading.x, placement.loading.y))
                    {
                        order.Worker = worker; order.Box = parcel; order.Shelf = shelf;
                        porter.Ledger = sponsor.Ledger = marker;
                        shelf.AddPart(new LocalShipmentReceiverPart { Ledger = marker, Merchant = merchant, Sponsor = supplier, FirstMeasure = clay });
                        parcel.AddPart(new LocalPorterReservationPart { Ledger = marker });
                        TradeSystem.SetDrams(supplier, SupplyBudget);
                        committed = proof() && original.Concat(placed).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
                        return committed;
                    }
                    foreach (var owner in placed.Where(e => e != shelf && e.SpatialZone == zone))
                        zone.RemoveEntity(owner);
                }
                return false;
            }
            catch (Exception error)
            {
                Diag.Record("worldgen", "WaysideShipmentRefused", payload: new { zone = zone.ZoneID, error = error.GetType().Name });
                return false;
            }
            finally
            {
                if (!committed)
                    foreach (var owner in placed.Where(e => e?.SpatialZone == zone && !original.Contains(e)))
                        zone.RemoveEntity(owner);
            }
        }

        private static bool TryInstallSortingLot(Zone zone, OverworldZoneManager manager, Entity cook, HashSet<Entity> original)
        {
            const string blueprint = "DryClaySpoil";
            var factory = manager.Factory;
            if (!manager.Exploration.RetainsGeneratedGraph(manager, zone) || !factory.Blueprints.ContainsKey(blueprint)
                || original.Any(e => e.BlueprintName == blueprint)
                || original.Count(e => e.GetPart<KitchenBatchPart>() is { Configured: true }) != 1) return false;

            var cookAt = zone.GetEntityPosition(cook);
            var cells = from y in Enumerable.Range(cookAt.y - 4, 9)
                        from x in Enumerable.Range(cookAt.x - 4, 9)
                        let distance = Math.Max(Math.Abs(x - cookAt.x), Math.Abs(y - cookAt.y))
                        where distance >= 2
                        orderby distance, y, x
                        select (x, y);
            var proof = SpreadGenerationReceipt.CaptureFinalState(zone, original);
            Entity lot = null;
            bool committed = false;
            try
            {
                lot = factory.CreateEntity(blueprint);
                if (!Fresh(lot)) return false;
                string lotID = lot.ID;
                bool ValidLot() => lot.BlueprintName == blueprint && lot.ID == lotID
                    && lot.GetPart<PhysicsPart>() is { Takeable: true, Solid: false, InInventory: null, Equipped: null }
                    && lot.GetPart<StackerPart>() is { StackCount: 1 }
                    && lot.GetPart<SiftableStockPart>() is { Contents: "FireClay:2:0;CoarseGrit:1:2" } stock
                    && stock.Definition(factory) != null;
                if (!ValidLot() || original.Any(e => e.ID == lotID) || !proof()
                    || !original.SetEquals(zone.GetReadOnlyEntities())) return false;

                foreach (var at in cells)
                {
                    if (!Plain(zone, at, court: false) || zone.GetCell(at.x, at.y).IsInterior
                        || !zone.AddEntity(lot, at.x, at.y)) continue;
                    if (!ValidLot() || !proof() || zone.GetEntityPosition(lot) != at
                        || !original.Append(lot).ToHashSet().SetEquals(zone.GetReadOnlyEntities())) return false;
                    if (LocalPorterPart.Route(cook, lot, zone) is { Count: > 0 and <= 5 })
                    {
                        committed = true;
                        return true;
                    }
                    zone.RemoveEntity(lot);
                }
                return false;
            }
            catch (Exception error)
            {
                Diag.Record("worldgen", "WaysideSortingLotRefused", payload: new { zone = zone.ZoneID, error = error.GetType().Name });
                return false;
            }
            finally
            {
                if (!committed && lot?.SpatialZone == zone && !original.Contains(lot)) zone.RemoveEntity(lot);
            }
        }

        private static IEnumerable<((int x, int y) loading, (int x, int y) supplier, (int x, int y) worker)>
            LoadingPlaces(Zone zone, (int x, int y) merchant)
        {
            foreach (int distance in new[] { 10, 8, 12 })
                foreach (var direction in Cardinal)
                {
                    var loading = (x: merchant.x + direction.x * distance, y: merchant.y + direction.y * distance);
                    if (!Plain(zone, loading, court: false)) continue;
                    var workers = Cardinal.Select(d => Add(loading, d))
                        .Where(p => Plain(zone, p, court: false)).ToArray();
                    foreach (var worker in workers)
                    {
                        var supplier = workers.FirstOrDefault(p => p != worker
                            && Math.Abs(p.x - worker.x) <= 1 && Math.Abs(p.y - worker.y) <= 1);
                        if (supplier == default) continue;
                        yield return (loading, supplier, worker);
                        break;
                    }
                }
        }

        private static bool Fresh(Entity owner) => owner != null && !string.IsNullOrEmpty(owner.ID)
            && owner.SpatialZone == null && owner.GetPart<PhysicsPart>() is { InInventory: null, Equipped: null }
            && owner.Parts.All(p => p?.ParentEntity == owner);

        private static bool Plain(Zone zone, (int x, int y) p, bool court) => zone.InBounds(p.x, p.y)
            && !zone.GenReservedCells.Contains(p) && !zone.GetCell(p.x, p.y).BlocksMovement()
            && zone.TileState.Get(p.x, p.y)?.IsEmpty != false
            && zone.GetCell(p.x, p.y).Objects.All(e => e.BlueprintName == "Grass"
                || court && e.BlueprintName is "StoneFloor" or "RoadStone");

        private static (int x, int y) Add((int x, int y) p, (int x, int y) d) => (p.x + d.x, p.y + d.y);
    }
}
