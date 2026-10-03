using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Provenance for still-uncollected produce from an exact marked
    /// bed. First committed pickup releases it; this is not a stolen-goods law.</summary>
    public sealed class ReserveYieldPart : Part
    {
        public override string Name => "ReserveYield";
        public Entity Keeper, Soil, AlreadyWitnessedPlayer;
        public string KeeperID, WorldKey;
        public int SourceX, SourceY;
        public bool Released;
    }

    /// <summary>Physical source and post-commit witness seams. Never scans or
    /// generates another zone, and never deduces a claim from an item blueprint.</summary>
    public static class LocalGatheringClaims
    {
        internal sealed class Receipt
        {
            internal LocalGatheringClaimPart Claim;
            internal Entity Actor, Item;
            internal Zone Zone;
            internal Cell Source;
            internal ReserveYieldPart Marker;
            internal bool Allowed;
        }

        internal static string WorldKeyFor(Zone zone) => WorldLocationContext.For(zone)?.Exploration?.WorldKey;
        internal static bool Ground(Entity owner, Zone zone, string blueprint = null)
        {
            if (owner == null || zone == null) return false;
            var physics = owner?.GetPart<PhysicsPart>(); var cell = zone?.GetEntityCell(owner);
            var render = owner?.GetPart<RenderPart>();
            return owner != null && !string.IsNullOrEmpty(owner.ID) && (blueprint == null || owner.BlueprintName == blueprint)
                && owner.SpatialZone == zone && cell?.ParentZone == zone && cell.Objects.Contains(owner)
                && physics?.ParentEntity == owner && !physics.Takeable && physics.InInventory == null && physics.Equipped == null
                && !owner.HasPart<SpatialFootprintPart>() && render?.ParentEntity == owner && render.Visible;
        }
        internal static bool Alive(Entity actor) => actor != null && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor);
        internal static bool PlayerAvailable(Entity actor, Zone zone) => Alive(actor) && actor.HasTag("Player")
            && actor.SpatialZone == zone && zone?.GetEntityCell(actor)?.Objects.Contains(actor) == true
            && actor.GetPart<InventoryPart>()?.ParentEntity == actor && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;
        internal static bool Friendly(Entity a, Entity b) => a != null && b != null
            && !FactionManager.IsHostile(a, b) && !FactionManager.IsHostile(b, a)
            && a.GetPart<BrainPart>()?.IsPersonallyHostileTo(b) != true && b.GetPart<BrainPart>()?.IsPersonallyHostileTo(a) != true;
        internal static bool Carried(Entity item, Entity actor, InventoryPart pack)
        {
            var physics = item?.GetPart<PhysicsPart>(); var stack = item?.GetPart<StackerPart>();
            return item != null && !string.IsNullOrEmpty(item.ID) && item.SpatialZone == null
                && physics?.ParentEntity == item && physics.Takeable && physics.InInventory == actor && physics.Equipped == null
                && (stack == null || stack.ParentEntity == item && stack.StackCount > 0)
                && pack?.ParentEntity == actor && pack.Objects.Count(e => e == item) == 1
                && !pack.EquippedItems.ContainsValue(item) && pack.FindEquippedBodyPart(item) == null;
        }
        static LocalGatheringClaimPart ClaimFor(Zone zone, Entity target)
        {
            if (zone == null || target == null) return null;
            var cell = zone.GetEntityCell(target);
            foreach (var owner in zone.GetReadOnlyEntities())
            {
                var claim = owner.GetPart<LocalGatheringClaimPart>();
                if (claim == null || !claim.Bound(zone)) continue;
                if (claim.Tray == target || claim.FirstSoil == target || claim.SecondSoil == target) return claim;
                if (target.GetPart<CropPart>()?.ParentEntity == target && Ground(target, zone) && claim.OwnsBed(zone, cell)) return claim;
            }
            return null;
        }
        static LocalGatheringClaimPart ClaimForYield(Zone zone, Entity item, ReserveYieldPart marker)
        {
            if (marker == null || marker.ParentEntity != item || marker.Released || item?.GetPart<ReserveYieldPart>() != marker
                || marker.Keeper == null || marker.Keeper.ID != marker.KeeperID || item.SpatialZone != zone) return null;
            var claim = marker.Keeper.GetPart<LocalGatheringClaimPart>();
            var source = zone?.GetCell(marker.SourceX, marker.SourceY);
            return claim != null && claim.WorldKey == marker.WorldKey && claim.OwnsBed(zone, source)
                && (claim.FirstSoil == marker.Soil || claim.SecondSoil == marker.Soil)
                && zone.GetEntityCell(marker.Soil) == source && zone.GetEntityCell(item) == source ? claim : null;
        }
        public static string WarningFor(Entity actor, Entity target, Zone zone)
        {
            var claim = ClaimFor(zone, target) ?? ClaimForYield(zone, target, target?.GetPart<ReserveYieldPart>());
            if (claim == null || claim.GetState(actor) == ReserveAccessState.Granted) return null;
            return "This is Nella's tied reserve. Taking without leave can suspend local gathering rights if she sees you. The open beds remain public.";
        }
        internal static Receipt CaptureHarvest(Entity actor, Entity crop, Zone zone)
        {
            if (actor != null && !PlayerAvailable(actor, zone)) return null;
            var claim = ClaimFor(zone, crop);
            var plant = crop?.GetPart<CropPart>();
            if (claim == null || plant == null || actor != null && !plant.HarvestAtMaturity) return null;
            // Automatic maturity has no culprit. Its still-uncollected yield
            // retains this exact bed's claim until a real pickup is witnessed.
            return new Receipt { Claim = claim, Actor = actor, Item = crop, Zone = zone,
                Source = zone.GetEntityCell(crop), Allowed = actor != null && claim.GetState(actor) == ReserveAccessState.Granted };
        }
        /// <summary>Called after staged yield placement succeeds but before the
        /// command commits. Removing those outputs on rollback removes their
        /// new provenance too. Breach knowledge is published only after commit.</summary>
        internal static void RecordHarvest(Receipt receipt, IReadOnlyList<Entity> outputs, InventoryTransaction tx)
        {
            if (receipt == null || tx == null) return;
            var markers = new List<ReserveYieldPart>();
            if (!receipt.Allowed && receipt.Claim.OwnsBed(receipt.Zone, receipt.Source))
            {
                Entity soil = receipt.Zone.GetEntityCell(receipt.Claim.FirstSoil) == receipt.Source ? receipt.Claim.FirstSoil : receipt.Claim.SecondSoil;
                foreach (var output in outputs)
                {
                    var marker = new ReserveYieldPart { Keeper = receipt.Claim.ParentEntity, KeeperID = receipt.Claim.KeeperID,
                        Soil = soil, WorldKey = receipt.Claim.WorldKey, SourceX = receipt.Source.X, SourceY = receipt.Source.Y };
                    output.AddPart(marker); markers.Add(marker);
                }
            }
            tx.AfterCommit(() =>
            {
                if (!receipt.Allowed && receipt.Claim.RecordBreach(receipt.Actor, receipt.Zone, receipt.Source, "harvest"))
                    foreach (var marker in markers) marker.AlreadyWitnessedPlayer = receipt.Actor;
            });
        }
        internal static Receipt CaptureTake(Entity actor, Entity item, Zone zone, Entity container = null)
        {
            if (!PlayerAvailable(actor, zone)) return null;
            var marker = container == null ? item?.GetPart<ReserveYieldPart>() : null;
            var claim = container == null ? ClaimForYield(zone, item, marker) : ClaimFor(zone, container);
            if (claim == null || container != null && claim.Tray != container) return null;
            return new Receipt { Claim = claim, Actor = actor, Item = item, Zone = zone, Marker = marker,
                Source = zone.GetEntityCell(container ?? item), Allowed = claim.GetState(actor) == ReserveAccessState.Granted };
        }
        internal static void RecordTake(Receipt receipt, InventoryTransaction tx)
        {
            if (receipt == null || tx == null) return;
            tx.AfterCommit(() =>
            {
                if (!receipt.Allowed && receipt.Marker?.AlreadyWitnessedPlayer != receipt.Actor)
                    receipt.Claim.RecordBreach(receipt.Actor, receipt.Zone, receipt.Source, "take");
                // Ownership ends at acquisition, including an unseen take. A
                // later drop or sale must not create a global stolen-goods tag.
                if (receipt.Marker != null)
                { receipt.Marker.Released = true; receipt.Item.RemovePart(receipt.Marker); }
            });
        }
    }
}
