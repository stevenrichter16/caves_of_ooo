using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried meal, one Eat action: 3d4 healing and removal of one
    /// ordinary bleed. Both effects enlist in the caller's inventory receipt.</summary>
    public sealed class FieldMealPart : Part
    {
        public override string Name => "FieldMeal";
        bool Available(Entity actor)
        {
            var pack = actor?.GetPart<InventoryPart>(); var meal = ParentEntity;
            return actor != null && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && pack?.ParentEntity == actor
                && meal?.GetPart<FieldMealPart>() == this && meal.BlueprintName == "FieldMeal"
                && meal.GetPart<PhysicsPart>()?.InInventory == actor && meal.GetPart<PhysicsPart>().Equipped == null
                && meal.SpatialZone == null && pack.CanConsumeOne(meal) && !pack.EquippedItems.ContainsValue(meal)
                && pack.FindEquippedBodyPart(meal) == null;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            if (e.ID == "GetInventoryActions")
            {
                if (Available(actor)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("Eat", "eat (3d4 healing; stop bleeding)", "Eat", 'e', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Eat") return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (tx == null || !Available(actor) || !tx.TryClaim(actor, actor, "EatFieldMeal") || !tx.TryClaim(ParentEntity, actor, "EatFieldMeal")) return true;
            var meal = ParentEntity; var pack = actor.GetPart<InventoryPart>(); var hp = actor.GetStat("Hitpoints");
            var status = actor.GetPart<StatusEffectsPart>(); Effect bleed = null; int bleedIndex = -1;
            if (status != null)
                for (int i = 0; i < status.GetAllEffects().Count; i++)
                    if (status.GetAllEffects()[i].GetType() == typeof(BleedingEffect)) { bleed = status.GetAllEffects()[i]; bleedIndex = i; break; }
            var receipt = InventoryTransferSnapshot.Capture(pack, meal); tx.Do(null, receipt.Restore);
            if (!receipt.Apply(() => pack.TryConsumeOne(meal))) return true;
            int before = hp.BaseValue; tx.Do(null, () => hp.BaseValue = before);
            int healing = DiceRoller.Roll("3d4", e.GetParameter<Random>("Random") ?? new Random());
            hp.BaseValue = (int)Math.Min((long)hp.Max, (long)hp.BaseValue + healing);
            int healed = hp.BaseValue - before;
            if (bleed != null)
            {
                string cause = bleed.LastRemovalCause;
                tx.Do(null, () => { bleed.LastRemovalCause = cause; status.RestoreRemovedEffectForInventoryUndo(bleed, bleedIndex); });
                status.RemoveEffect(bleed);
            }
            tx.AfterCommit(() => { MessageLog.Add("You eat the wrapped field meal." + (healed > 0 ? " You recover " + healed + " HP." : ""));
                Diag.Record("event", "FieldMealEaten", actor, meal, new { healed, stoppedBleeding = bleed != null }); });
            e.Handled = true; return false;
        }
    }
}
