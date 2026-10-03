using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One actual carried unit tends one planted crop on prepared soil.
    /// Moisture remains CropPart state; tending never creates a pool or coating.</summary>
    public static class CropWateringService
    {
        public const int MoisturePerUnit = 40;
        const string Prefix = "WaterCrop|";

        /// <summary>Recognizes the crop's exact-vessel selection command.</summary>
        public static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>Pure menu projection: neither the clock nor old moisture is
        /// reconciled while browsing. Each row names one physical carried owner.</summary>
        public static void AddActions(Entity actor, Entity owner, Zone zone, InventoryActionList actions)
        {
            if (actions == null || CropInvalid(actor, owner, zone, out var crop) != null
                || crop.GrowthStage == 2 || crop.MoistureTicks >= MoisturePerUnit) return;
            foreach (var vessel in actor.GetPart<InventoryPart>().Objects)
            {
                if (VesselInvalid(actor, vessel, out var skin, out var liquid) != null || FindVessel(actor, vessel.ID) != vessel) continue;
                int available = skin != null ? skin.Charges : liquid.Volume;
                actions.AddAction("WaterCrop", "water with " + vessel.GetDisplayName() + " (1 of " + available + ")",
                    Prefix + Uri.EscapeDataString(vessel.ID), '\0', 19);
            }
        }

        /// <summary>Reconciles old elapsed time, then joins the caller's receipt
        /// for one new unit of water. A failed new action never undoes old growth.</summary>
        public static bool TryWater(Entity actor, Entity owner, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (!IsCommand(command)) return false;
            Entity vessel = null;
            string[] fields = command.Split('|');
            if (fields.Length != 2 || string.IsNullOrEmpty(fields[1])) return Reject(actor, owner, vessel, "invalid-selection");
            string id;
            try { id = Uri.UnescapeDataString(fields[1]); }
            catch (UriFormatException) { return Reject(actor, owner, vessel, "invalid-selection"); }
            if (string.IsNullOrEmpty(id) || (vessel = FindVessel(actor, id)) == null)
                return Reject(actor, owner, vessel, "vessel-unavailable");
            string invalid = CropInvalid(actor, owner, zone, out var crop)
                ?? VesselInvalid(actor, vessel, out _, out _);
            if (invalid != null) return Reject(actor, owner, vessel, invalid);
            var cell = zone.GetEntityCell(owner);

            // This interval predates the paid action. It may publish a legacy
            // harvest and remove the owner, so it must precede both snapshots
            // and transfer claims (legacy yield owns its own transaction).
            if (!CropTime.Reconcile(crop, zone, WorldClock.CurrentTick)) return Reject(actor, owner, vessel, "invalid-crop");
            invalid = CropInvalid(actor, owner, zone, out var current)
                ?? VesselInvalid(actor, vessel, out _, out _);
            if (invalid != null || current != crop || zone.GetEntityCell(owner) != cell || FindVessel(actor, id) != vessel)
                return Reject(actor, owner, vessel, invalid ?? "changed-owner");
            if (crop.GrowthStage == 2) return Reject(actor, owner, vessel, "ripe");
            if (crop.MoistureTicks >= MoisturePerUnit) return Reject(actor, owner, vessel, "already-watered");
            VesselInvalid(actor, vessel, out var skin, out var liquid);

            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, "WaterCrop") || !transaction.TryClaim(owner, actor, "WaterCrop")
                    || !transaction.TryClaim(vessel, actor, "WaterCrop")) return Reject(actor, owner, vessel, "in-progress");
                int waterBefore = skin != null ? skin.Charges : liquid.Volume;
                int moistureBefore = crop.MoistureTicks;
                string liquidBefore = liquid?.LiquidId;
                var render = owner.GetPart<RenderPart>();
                string backgroundBefore = render?.BackgroundColor;
                transaction.Do(null, () =>
                {
                    if (skin != null) skin.Charges = waterBefore;
                    else { liquid.Volume = waterBefore; liquid.LiquidId = liquidBefore; }
                    crop.MoistureTicks = moistureBefore;
                    if (render != null) render.BackgroundColor = backgroundBefore;
                    if (CropTime.IsCurrentOwner(crop, zone) && zone.GetEntityCell(owner) == cell)
                        ZoneRenderHooks.MarkCellDirty(cell, "CropHandWaterRollback");
                });
                if (skin != null) skin.Charges--;
                else { liquid.Volume--; if (liquid.Volume == 0) liquid.LiquidId = ""; }
                // The old interval is settled; no growth or fractional-time
                // field changes here. Like rain, supply is a maximum top-up.
                crop.MoistureTicks = MoisturePerUnit;
                if (render != null) render.BackgroundColor = CropPart.WET_SOIL_BG;
                transaction.AfterCommit(() =>
                {
                    if (CropTime.IsCurrentOwner(crop, zone) && zone.GetEntityCell(owner) == cell)
                    {
                        ZoneRenderHooks.MarkCellDirty(cell, "CropHandWatered");
                        EntityVisualHooks.EmitInteraction(actor, owner, zone);
                    }
                });
                transaction.AfterCommit(() => Diag.Record("crop", "HandWatered", actor, owner,
                    new { vesselId = vessel.ID, unitsSpent = 1, remaining = waterBefore - 1, moistureBefore, moistureAfter = MoisturePerUnit }));
                transaction.AfterCommit(() => MessageLog.Add("You water the " + owner.GetDisplayName() + " with the "
                    + vessel.GetDisplayName() + ". (" + (waterBefore - 1) + " water remain)"));
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        static string CropInvalid(Entity actor, Entity owner, Zone zone, out CropPart crop)
        {
            crop = owner?.GetPart<CropPart>();
            if (actor == null || zone == null) return "actor-unavailable";
            var inventory = actor?.GetPart<InventoryPart>();
            var actorCell = zone?.GetEntityCell(actor);
            var actorPhysics = actor?.GetPart<PhysicsPart>();
            if (actor == null || !actor.HasTag("Player") || inventory?.ParentEntity != actor || actorCell == null
                || actor.SpatialZone != zone || !actorCell.Objects.Contains(actor)
                || actorPhysics == null || actorPhysics.ParentEntity != actor || actorPhysics.InInventory != null
                || actorPhysics.Equipped != null) return "actor-unavailable";
            if (CombatSystem.IsDeathHandled(actor) || actor.GetStatValue("Hitpoints", 0) <= 0) return "actor-dead";
            if (actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "actor-blocked";
            if (!CropTime.IsCurrentOwner(crop, zone)) return "crop-unavailable";
            if (SpatialQuery.Distance(zone, actor, owner) > 1) return "out-of-reach";
            if (!CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(owner))) return "unprepared-bed";
            if (crop.GrowthStage < 0 || crop.GrowthStage > (crop.HarvestAtMaturity ? 2 : 1)
                || crop.MoistureTicks < 0 || crop.TicksInStage < 0 || crop.TicksPerStage <= 0) return "invalid-crop";
            if (crop.GrowthTimingVersion == 0)
            {
                if (crop.LastGrowthWorldTick != -1 || crop.GrowthWetTickRemainder != 0) return "invalid-crop";
            }
            else if (crop.GrowthTimingVersion != CropTime.CurrentVersion || crop.LastGrowthWorldTick < 0
                || crop.LastGrowthWorldTick > WorldClock.CurrentTick || crop.GrowthWetTickRemainder < 0
                || crop.GrowthWetTickRemainder >= CropTime.WorldTicksPerUnit
                || (crop.MoistureTicks == 0 && crop.GrowthWetTickRemainder != 0)) return "invalid-crop";
            return null;
        }

        static Entity FindVessel(Entity actor, string id)
        {
            var inventory = actor?.GetPart<InventoryPart>();
            if (inventory == null || string.IsNullOrEmpty(id)) return null;
            Entity found = null;
            foreach (var item in inventory.Objects)
            {
                if (item?.ID != id) continue;
                if (found != null) return null;
                found = item;
            }
            return found;
        }

        static string VesselInvalid(Entity actor, Entity vessel, out WaterskinPart skin, out LiquidVesselPart liquid)
        {
            skin = vessel?.GetPart<WaterskinPart>(); liquid = vessel?.GetPart<LiquidVesselPart>();
            var physics = vessel?.GetPart<PhysicsPart>();
            var inventory = actor?.GetPart<InventoryPart>();
            if (vessel == null || string.IsNullOrEmpty(vessel.ID) || inventory?.Objects.Contains(vessel) != true
                || vessel.SpatialZone != null || physics == null || physics.ParentEntity != vessel || !physics.Takeable
                || physics.InInventory != actor || physics.Equipped != null || inventory.EquippedItems.ContainsValue(vessel)
                || inventory.FindEquippedBodyPart(vessel) != null || (vessel.GetPart<StackerPart>()?.StackCount ?? 1) != 1)
                return "vessel-unavailable";
            if ((skin == null) == (liquid == null)) return "invalid-vessel";
            if (skin != null)
                return skin.ParentEntity != vessel || skin.Capacity <= 0 || skin.Charges < 0 || skin.Charges > skin.Capacity
                    ? "invalid-vessel" : skin.Charges == 0 ? "empty" : null;
            if (liquid.ParentEntity != vessel || liquid.Capacity <= 0 || liquid.Volume < 0 || liquid.Volume > liquid.Capacity
                || (liquid.Volume == 0 && !string.IsNullOrEmpty(liquid.LiquidId))) return "invalid-vessel";
            if (liquid.Volume == 0) return "empty";
            return liquid.LiquidId == "water" ? null : "not-water";
        }

        static bool Reject(Entity actor, Entity owner, Entity vessel, string reason)
        {
            Diag.Record("crop", "HandWaterRejected", actor, owner, new { vesselId = vessel?.ID, reason });
            string message = reason switch
            {
                "actor-dead" => "You cannot tend crops while dead.",
                "actor-blocked" => "You cannot tend crops while unable to act.",
                "out-of-reach" => "Stand beside the planted crop to water it.",
                "unprepared-bed" => "Hand watering needs a planted crop on prepared, unflooded soil.",
                "ripe" => "That crop is already ripe and needs no more water.",
                "already-watered" => "That crop already has enough water.",
                "empty" => "That water vessel is empty.",
                "not-water" => "Only clean water can tend this crop.",
                "in-progress" => "That crop or vessel is already in use.",
                "vessel-unavailable" => "You must still carry the selected water vessel.",
                _ => "You cannot water that crop with this selection."
            };
            MessageLog.Add(message);
            return false;
        }
    }
}
