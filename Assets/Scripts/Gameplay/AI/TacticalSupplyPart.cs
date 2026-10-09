using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Authored, finite emergency water. Literal carried inventory is
    /// the only stock authority; this policy never creates or refills a supply.</summary>
    public sealed class TacticalSupplyPart : Part
    {
        public override string Name => "TacticalSupply";
        /// <summary>Saved opt-in. Presence enables the policy; false disables it.</summary>
        public bool SelfDousing = true;

        /// <summary>Pure inventory query, including before a loaded actor is
        /// placed. Health, control and combat eligibility are separate gates.</summary>
        public Entity FindCarriedWater()
        {
            var actor = ParentEntity;
            var pack = actor?.GetPart<InventoryPart>();
            if (actor?.GetPart<TacticalSupplyPart>() != this || pack?.ParentEntity != actor || pack?.Objects == null)
                return null;
            foreach (var item in pack.Objects)
            {
                if (item?.BlueprintName == "SunbladderShell" && item.GetPart<WaterskinPart>() != null
                    && WaterTransferActions.Vessel(actor, item, out int units, out _) && units > 0)
                    return item;
            }
            return null;
        }

        /// <summary>True replaces this whole AI opportunity. The existing
        /// scheduler owns energy and pre-action events; neither is sent here.
        /// Player visibility controls feedback only, never self-treatment.</summary>
        public bool TryUseEmergencyWater(Entity threat, Zone zone)
        {
            var actor = ParentEntity;
            if (actor == null || actor.GetPart<TacticalSupplyPart>() != this || !SelfDousing || zone == null)
                return Reject(threat, "disabled-or-missing-context");
            var brain = actor.GetPart<BrainPart>(); var hp = actor.GetStat("Hitpoints");
            var body = actor.GetPart<PhysicsPart>();
            if (!actor.HasTag("Creature") || actor.HasTag("Player") || hp == null || hp.Value <= 0 || hp.Max <= 0
                || CombatSystem.IsDeathHandled(actor) || actor.SpatialZone != zone || zone.GetEntityCell(actor) == null
                || body?.ParentEntity != actor || body.InInventory != null || body.Equipped != null
                || brain?.ParentEntity != actor || brain.CurrentZone != zone)
                return Reject(threat, "invalid-actor");
            if (brain.InConversation || brain.PartyLeader != null || brain.HasGoal<NoFightGoal>()
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true)
                return Reject(threat, "actor-controlled");
            var otherBody = threat?.GetPart<PhysicsPart>();
            if (threat == null || threat == actor || !threat.HasTag("Creature") || threat.GetStatValue("Hitpoints") <= 0
                || CombatSystem.IsDeathHandled(threat) || otherBody?.ParentEntity != threat
                || otherBody.InInventory != null || otherBody.Equipped != null
                || threat.SpatialZone != zone || zone.GetEntityCell(threat) == null
                || BrainPart.ArePartyAligned(actor, threat) || !FactionManager.IsHostile(actor, threat))
                return Reject(threat, "invalid-threat");
            var burn = actor.GetEffect<BurningEffect>();
            if (burn == null || burn.Intensity <= 0 || burn.Duration == 0)
                return Reject(threat, "not-burning");
            var water = FindCarriedWater();
            if (water == null || !EmergencyDousingActions.TryBuildSelfDousingCommand(actor, water, zone, out string command))
                return Reject(threat, "no-usable-water");
            var result = new InventoryCommandExecutor().Execute(new PerformInventoryActionCommand(water, command),
                new InventoryContext(actor, zone));
            if (!result.Success) return Reject(threat, "command-refused");

            Diag.Record("ai", "TacticalWaterUsed", actor, water,
                new { threatId = threat.ID, remainingWater = water.GetPart<WaterskinPart>()?.Charges ?? 0 });
            // The inventory action has committed. A presentation observer may
            // fail, but that must never grant a second attack or movement action.
            try { EntityVisualHooks.EmitSelfUse(actor, zone); }
            catch (Exception error)
            { Diag.Record("ai", "TacticalWaterVisualFailed", actor, water, new { reason = error.Message }); }
            return true;
        }

        /// <summary>Describe actual supply, not a promised future refill.</summary>
        public string Describe()
        {
            var water = FindCarriedWater();
            if (water == null) return "Emergency water: the sunbladder shell is empty or unavailable.";
            return "Emergency water: carries " + InventoryPart.GetUnitDisplayName(water)
                + (SelfDousing ? "; when burning in combat, spends one water and its turn to put out the flames. The wet body conducts electricity."
                    : "; emergency self-dousing is disabled.")
                + " Unspent water can be recovered after its death; the empty shell remains reusable.";
        }

        bool Reject(Entity threat, string reason)
        { Diag.Record("ai", "TacticalWaterRejected", ParentEntity, threat, new { reason }); return false; }
    }
}
