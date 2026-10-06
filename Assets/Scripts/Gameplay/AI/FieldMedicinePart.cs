using System;
using System.Collections.Generic;
using System.Globalization;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Opt-in combat self-treatment from finite carried supplies. This part never
    /// grants medicine or owns a second stock counter: inventory survives saves,
    /// transfers and death, and remains the sole authority for availability.
    /// </summary>
    public sealed class FieldMedicinePart : Part
    {
        public override string Name => "FieldMedicine";

        /// <summary>Exact authored supply accepted for autonomous self-treatment.</summary>
        public string TonicBlueprint = "HealingTonic";

        /// <summary>Inclusive health percentage, 1–100. Invalid configuration disables use.</summary>
        public int UseAtOrBelowPercent = 40;
        /// <summary>Opt-in exact Antidote/BurnSalve supplies; no independent stock or innate immunity.</summary>
        public string CureBlueprints = "";

        /// <summary>
        /// Pure local-inventory query shared by decisions, Examine and presentation.
        /// Returns an actual positive carried healing tonic, never a floor item,
        /// equipped item, foreign-owned reference or independently manufactured supply.
        /// Actor health/control gates are deliberately separate: a calm or healthy
        /// creature still visibly carries its unspent medicine.
        /// </summary>
        public Entity FindCarriedMedicine()
        {
            var inventory = OwnedInventory();
            if (inventory == null) return null;
            foreach (var item in inventory.Objects)
                if (IsCarriedMedicine(inventory, item)) return item;
            return null;
        }

        private InventoryPart OwnedInventory()
        {
            var actor = ParentEntity;
            var inventory = actor?.GetPart<InventoryPart>();
            return actor != null && actor.GetPart<FieldMedicinePart>() == this && inventory?.ParentEntity == actor
                && inventory.Objects != null && !string.IsNullOrWhiteSpace(TonicBlueprint) ? inventory : null;
        }

        private bool IsCarriedMedicine(InventoryPart inventory, Entity item)
        {
            if (item == null || !string.Equals(item.BlueprintName, TonicBlueprint, StringComparison.Ordinal)
                || !inventory.CanConsumeOne(item) || item.SpatialZone != null) return false;
            var physical = item.GetPart<PhysicsPart>(); var tonic = item.GetPart<TonicPart>();
            return physical?.ParentEntity == item && physical.InInventory == ParentEntity && physical.Equipped == null
                && tonic?.ParentEntity == item && !string.IsNullOrWhiteSpace(tonic.Healing);
        }

        private Entity FindCarriedCure(bool needsTreatment)
        {
            var inventory = OwnedInventory(); if (inventory == null) return null;
            foreach (var raw in (CureBlueprints ?? "").Split(';'))
            {
                string name = raw.Trim();
                if (name != "Antidote" && name != "BurnSalve") continue;
                foreach (var item in inventory.Objects)
                {
                    if (item?.BlueprintName != name || !inventory.CanConsumeOne(item) || item.SpatialZone != null) continue;
                    var physical = item.GetPart<PhysicsPart>(); var cure = item.GetPart<CureTonicPart>();
                    if (physical?.ParentEntity != item || physical.InInventory != ParentEntity || physical.Equipped != null
                        || item.GetPart<TonicPart>()?.ParentEntity != item || cure?.ParentEntity != item) continue;
                    bool poison = name == "Antidote" && cure.CureEffect == nameof(PoisonedEffect);
                    bool burn = name == "BurnSalve" && cure.CureEffect == nameof(BurningEffect);
                    if ((poison && (!needsTreatment || ParentEntity.HasEffect<PoisonedEffect>() || ParentEntity.HasEffect<PoisonedByGasEffect>()))
                        || (burn && (!needsTreatment || ParentEntity.HasEffect<BurningEffect>()))) return item;
                }
            }
            return null;
        }

        private long CountCarriedMedicine(string blueprint)
        {
            var inventory = OwnedInventory();
            if (inventory == null) return 0;
            long count = 0;
            var counted = new HashSet<Entity>();
            foreach (var item in inventory.Objects)
                if (item?.BlueprintName == blueprint && inventory.CanConsumeOne(item) && item.SpatialZone == null
                    && item.GetPart<PhysicsPart>()?.InInventory == ParentEntity && item.GetPart<PhysicsPart>()?.Equipped == null && counted.Add(item))
                    count += item.GetPart<StackerPart>()?.StackCount ?? 1;
            return count;
        }

        /// <summary>
        /// Use at most one actual carried tonic against the pressure of this live,
        /// hostile threat. True replaces the caller's entire movement/attack action;
        /// false preserves ordinary AI behavior. The scheduler owns action energy
        /// and BeginTakeAction, so this method never dispatches either a second time.
        /// </summary>
        public bool TryUseMedicine(Entity threat, Zone zone, Random rng)
        {
            var actor = ParentEntity;
            if (actor == null || zone == null || rng == null)
                return Reject(threat, "missing-context");
            var brain = actor.GetPart<BrainPart>();
            var hp = actor.GetStat("Hitpoints");
            var physical = actor.GetPart<PhysicsPart>();
            if (!actor.HasTag("Creature") || actor.HasTag("Player") || hp == null || hp.Value <= 0 || hp.Max <= 0
                || CombatSystem.IsDeathHandled(actor)
                || actor.SpatialZone != zone || zone.GetEntityCell(actor) == null
                || physical?.ParentEntity != actor || physical.InInventory != null || physical.Equipped != null
                || brain?.ParentEntity != actor || brain.CurrentZone != zone)
                return Reject(threat, "invalid-actor");
            if (brain.InConversation || brain.PartyLeader != null || brain.HasGoal<NoFightGoal>()
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true)
                return Reject(threat, "actor-controlled");
            var threatPhysics = threat?.GetPart<PhysicsPart>();
            if (threat == null || threat == actor || !threat.HasTag("Creature") || threat.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(threat) || threatPhysics?.ParentEntity != threat
                || threatPhysics.InInventory != null || threatPhysics.Equipped != null
                || threat.SpatialZone != zone || zone.GetEntityCell(threat) == null
                || BrainPart.ArePartyAligned(actor, threat) || !FactionManager.IsHostile(actor, threat))
                return Reject(threat, "invalid-threat");
            var medicine = FindCarriedCure(needsTreatment: true);
            if (medicine == null)
            {
                if (UseAtOrBelowPercent < 1 || UseAtOrBelowPercent > 100 || hp.Value >= hp.Max
                    || (long)hp.Value * 100 > (long)hp.Max * UseAtOrBelowPercent)
                    return Reject(threat, "health-threshold");
                medicine = FindCarriedMedicine();
            }
            if (medicine == null) return Reject(threat, "no-carried-medicine");
            int before = hp.Value;
            if (!medicine.GetPart<TonicPart>().ApplyTo(actor, actor, zone, rng,
                consumeItem: true, showUseMessage: true)) return Reject(threat, "consumption-refused");

            Diag.Record("ai", "FieldMedicineUsed", actor, medicine,
                new { threatId = threat.ID, hpBefore = before, hpAfter = hp.Value,
                    thresholdPercent = UseAtOrBelowPercent, medicineBlueprint = medicine.BlueprintName, remainingUnits = CountCarriedMedicine(medicine.BlueprintName) });
            ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(actor), "FieldMedicine.Used");
            EntityVisualHooks.EmitSelfUse(actor, zone);
            return true;
        }

        /// <summary>Read-only live stock and policy for ordinary world Examine.</summary>
        public string Describe()
        {
            var medicine = FindCarriedMedicine();
            var cure = FindCarriedCure(needsTreatment: false);
            string cureNote = cure == null ? "" : " Carries " + InventoryPart.GetUnitDisplayName(cure)
                + "; uses one matching cure for its turn when afflicted, before healing.";
            if (medicine == null) return cure == null ? "Field medicine: the bottle harness is empty."
                : "Field medicine:" + cureNote + " Unused medicine can be recovered after its death.";
            string stock = "Field medicine: carries " + InventoryPart.GetUnitDisplayName(medicine) + ". ";
            if (UseAtOrBelowPercent < 1 || UseAtOrBelowPercent > 100)
                return stock + "Its treatment threshold is invalid.";
            return stock + "In combat at " + UseAtOrBelowPercent.ToString(CultureInfo.InvariantCulture)
                + "% health or below, uses one tonic for its turn instead of moving or attacking."
                + " Unused medicine can be recovered after its death." + cureNote;
        }

        private bool Reject(Entity threat, string reason)
        {
            Diag.Record("ai", "FieldMedicineRejected", ParentEntity, threat, new { reason });
            return false;
        }
    }
}
