using System;

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

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
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

            // Show flavor message
            string displayMsg = !string.IsNullOrEmpty(Message) ? Message
                : $"{actor.GetDisplayName()} eats {ParentEntity.GetDisplayName()}.";
            MessageLog.Add(displayMsg);

            // Consume the item
            ConsumeItem(actor);

            e.Handled = true;
            return false;
        }

        private void ConsumeItem(Entity actor)
        {
            var stacker = ParentEntity.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount--;
            }
            else
            {
                // Remove from inventory
                var inv = actor.GetPart<InventoryPart>();
                if (inv != null)
                    inv.RemoveObject(ParentEntity);
            }
        }
    }
}
