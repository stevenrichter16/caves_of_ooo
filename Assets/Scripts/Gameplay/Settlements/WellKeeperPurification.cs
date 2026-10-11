using System;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A real purification copy trains the local wellkeeper. The rite
    /// keeps water clean; it does not rebuild or improve the filtration ring.</summary>
    public static class WellKeeperPurification
    {
        public const string Condition = "WellKeeperKnowsPurification";
        public const string Action = "TeachWellPurification";
        public const string Predicate = "IfCanTeachWellPurification";

        public static bool CanTeach(Entity keeper, Entity player)
            => Context(keeper, player, out _, out _, out _) && FindCopy(player) != null;

        public static string TryTeach(Entity keeper, Entity player, string argument)
        {
            if (!Context(keeper, player, out var manager, out var state, out var site)) return "well_care_unavailable";
            var copy = FindCopy(player); if (copy == null) return "purification_copy_required";
            var inventory = player.GetPart<InventoryPart>(); var payload = copy.GetPart<GrimoirePart>();
            var stage = site.Stage; var deadline = site.RelapseAtTurn;
            var tx = new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(keeper, player, Action) || !tx.TryClaim(player, player, Action) || !tx.TryClaim(copy, player, Action)) return "teaching_in_progress";
                var payment = InventoryTransferSnapshot.Capture(inventory, copy); tx.Do(null, payment.Restore);
                if (!payment.Apply(() => inventory.TryConsumeOne(copy)) || !payment.ClaimChanges(tx, player, Action)) return "copy_handover_refused";
                tx.BeforeCommit(() => Context(keeper, player, out var current, out var currentState, out var currentSite)
                    && current == manager && currentState == state && currentSite == site
                    && site.Stage == stage && site.RelapseAtTurn == deadline
                    && player.GetPart<InventoryPart>() == inventory && copy.GetPart<GrimoirePart>() == payload
                    && payload.KnowledgeProperty == SettlementSiteDefinitions.StartingVillageKnowledgeProperty
                    && string.IsNullOrEmpty(payload.SkillClassName));
                tx.AfterCommit(() => manager.EstablishPurificationCare(state, site));
                tx.AfterCommit(() => manager.RefreshActiveZonePresentation(SettlementRuntime.ActiveZone));
                tx.AfterCommit(SettlementRuntime.MarkZoneDirty);
                tx.AfterCommit(() => MessageLog.AddAnnouncement("The well-keeper learns the purification rite. The water will stay clean under their care; the filtration ring still needs repair."));
                tx.Commit(); return null;
            }
            catch (InvalidOperationException) { return "teaching_changed"; }
            finally { tx.Rollback(); }
        }

        static bool Context(Entity keeper, Entity player, out SettlementManager manager, out SettlementState state, out RepairableSiteState site)
        {
            manager = SettlementManager.Current; state = null; site = null;
            if (keeper?.BlueprintName != "WellKeeper" || player?.GetPart<InventoryPart>() == null || manager == null
                || CombatSystem.IsDeathHandled(keeper) || CombatSystem.IsDeathHandled(player)
                || keeper.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || player.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return false;
            var zone = keeper.SpatialZone;
            if (zone == null || player.SpatialZone != zone || zone.GetEntityCell(keeper) == null
                || zone.GetEntityCell(player) == null || SpatialQuery.Distance(zone, keeper, player) > 1) return false;
            string id = keeper.GetProperty("SettlementId");
            if (string.IsNullOrEmpty(id) || id != zone.ZoneID) return false;
            site = manager.GetSite(id, SettlementSiteDefinitions.MainWellSiteId);
            if (site == null || site.SiteType != RepairableSiteType.Well
                || (site.Stage != RepairStage.Fouled && site.Stage != RepairStage.TemporarilyPurified)) return false;
            if (!manager.GetAllSettlementsSnapshot().TryGetValue(id, out state)) return false;
            return !state.HasCondition(Condition);
        }

        static Entity FindCopy(Entity player)
        {
            var inventory = player?.GetPart<InventoryPart>(); if (inventory == null) return null;
            foreach (var item in inventory.Objects)
                if (item != null && item.SpatialZone == null && item.GetPart<PhysicsPart>() is PhysicsPart physical
                    && physical.ParentEntity == item && physical.Takeable && physical.InInventory == player && physical.Equipped == null
                    && !inventory.EquippedItems.ContainsValue(item) && !HasBodyEquipmentAlias(player, item)
                    && item.HasTag("GrimoireCopy") && item.GetPart<GrimoirePart>() is GrimoirePart book
                    && book.ParentEntity == item && book.KnowledgeProperty == SettlementSiteDefinitions.StartingVillageKnowledgeProperty
                    && string.IsNullOrEmpty(book.SkillClassName) && inventory.CanConsumeOne(item)) return item;
            return null;
        }

        static bool HasBodyEquipmentAlias(Entity player, Entity item)
        {
            var parts = player.GetPart<Body>()?.GetParts();
            if (parts != null)
                foreach (var part in parts)
                    if (ReferenceEquals(part._Equipped, item)) return true;
            return false;
        }
    }
}
