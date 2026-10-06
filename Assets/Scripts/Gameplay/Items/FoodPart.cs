using System;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Consumable food item. Mirrors Qud's Food part.
    /// Declares an "Eat" inventory action. When eaten, heals HP and is consumed.
    /// Set via blueprint params: Healing (dice), Message, Cooking (flavor tag).
    /// </summary>
    public class FoodPart : Part
    {
        public override string Name => "Food";

        /// <summary>Healing dice roll (e.g., "2d4", "1d6+2"). Empty = no healing.</summary>
        public string Healing = "";

        /// <summary>Flavor text shown when consumed.</summary>
        public string Message = "";

        /// <summary>Cooking tag for the food system (e.g., "Meal", "Snack").</summary>
        public string Cooking = "";

        /// <summary>Optional expedition preparation; one prepared meal at a time.</summary>
        public string MealStat = "";
        public int MealBonus;
        public int MealDuration = 100;

        public static string DescribeMeal(FoodPart food)
        {
            return food != null && PreparedMealEffect.Valid(food.MealStat, food.MealBonus, food.MealDuration)
                ? "+" + food.MealBonus + " " + PreparedMealEffect.Label(food.MealStat) + " for "
                    + food.MealDuration + " of your turns. Replaces your previous prepared meal."
                : "";
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                var actor = e.GetParameter<Entity>("Actor");
                if (actions != null && (actor == null
                    || actor.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) == true))
                    actions.AddAction("Eat", "eat", "Eat", 'e', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "Eat") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                return DoEat(actor, e);
            }

            return true;
        }

        private bool DoEat(Entity actor, GameEvent e)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat alive && alive.Value <= 0))
                return true;
            if (!InventoryPart.TryConsumeOne(actor, ParentEntity))
                return true; // Leave the consuming action unhandled on refusal.

            // Heal if healing dice specified
            if (!string.IsNullOrEmpty(Healing))
            {
                var rng = e.GetParameter<Random>("Random") ?? new Random();
                int healed = DiceRoller.Roll(Healing, rng);
                if (healed > 0)
                {
                    var hp = actor.GetStat("Hitpoints");
                    if (hp != null)
                    {
                        int before = hp.Value;
                        hp.BaseValue = Math.Min(hp.BaseValue + healed, hp.Max);
                        int actual = hp.Value - before;
                        if (actual > 0)
                            MessageLog.Add($"{actor.GetDisplayName()} heals {actual} HP.");
                    }
                }
            }

            if (PreparedMealEffect.Valid(MealStat, MealBonus, MealDuration))
            {
                // A failed outer inventory action restores the food and stats.
                // Defer replacing the old meal until that action actually commits.
                string stat = MealStat; int bonus = MealBonus, duration = MealDuration;
                string description = DescribeMeal(this);
                Action prepare = () =>
                {
                    if (!CombatSystem.IsDeathHandled(actor)
                        && !(actor.GetStat("Hitpoints") is Stat hpNow && hpNow.Value <= 0)
                        && actor.ApplyEffect(new PreparedMealEffect(stat, bonus, duration)))
                        MessageLog.Add("Prepared meal: " + description);
                };
                var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (transaction != null) transaction.AfterCommit(prepare);
                else prepare();
            }

            // Show flavor message
            string displayMsg = !string.IsNullOrEmpty(Message) ? Message
                : $"{actor.GetDisplayName()} eats {ParentEntity.GetDisplayName()}.";
            MessageLog.Add(displayMsg);

            CavesOfOoo.Diagnostics.Diag.Record("event", "FoodEaten", actor: actor, target: ParentEntity,
                payload: new { healing = Healing });
            e.Handled = true;
            return false;
        }

    }
}
