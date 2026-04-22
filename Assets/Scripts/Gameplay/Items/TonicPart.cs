using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Consumable tonic/potion item. Mirrors Qud's Tonic part.
    /// Declares an "Apply" inventory action. When applied, fires ApplyTonic event
    /// on the item for sub-parts to handle, then consumes the item.
    /// Blueprint params: Effect (string tag), Duration (turns), Healing (dice),
    /// Message (flavor text), Drink (if true, uses "drink" instead of "apply").
    /// </summary>
    public class TonicPart : Part
    {
        public override string Name => "Tonic";

        /// <summary>Effect tag applied to the actor (e.g., "Regeneration", "SpeedBoost").</summary>
        public string Effect = "";

        /// <summary>Duration in turns for the effect. 0 = instant.</summary>
        public int Duration = 0;

        /// <summary>Instant healing dice (e.g., "4d4"). Applied immediately on use.</summary>
        public string Healing = "";

        /// <summary>Stat boost applied for Duration turns (e.g., "Strength:4").</summary>
        public string StatBoost = "";

        /// <summary>Flavor text shown when consumed.</summary>
        public string Message = "";

        /// <summary>If true, UI says "drink" instead of "apply".</summary>
        public bool Drink = false;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    if (Drink)
                        actions.AddAction("Drink", "drink", "ApplyTonic", 'd', 20);
                    else
                        actions.AddAction("Apply", "apply", "ApplyTonic", 'a', 20);
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "ApplyTonic") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                return DoApply(actor, e);
            }

            return true;
        }

        private bool DoApply(Entity actor, GameEvent e)
        {
            var rng = e.GetParameter<Random>("Random") ?? new Random();
            Zone zone = e.GetParameter<Zone>("Zone");

            ApplyTo(actor, actor, zone, rng, consumeItem: true, showUseMessage: true);
            e.Handled = true;
            return false;
        }

        public bool HasThrowablePayload()
        {
            return !string.IsNullOrWhiteSpace(Healing)
                || !string.IsNullOrWhiteSpace(StatBoost)
                || ParentEntity?.GetPart<CureTonicPart>() != null
                || ParentEntity?.GetPart<StatusTonicPart>() != null;
        }

        public bool ApplyTo(
            Entity target,
            Entity user,
            Zone zone = null,
            Random rng = null,
            bool consumeItem = false,
            bool showUseMessage = true)
        {
            if (target == null)
                return false;

            rng = rng ?? new Random();

            ApplyHealing(target, rng);

            if (!string.IsNullOrEmpty(StatBoost))
                ApplyStatBoost(target);

            var applyEvent = GameEvent.New("ApplyTonic");
            applyEvent.SetParameter("Actor", (object)target);
            applyEvent.SetParameter("Tonic", (object)ParentEntity);
            applyEvent.SetParameter("Effect", Effect);
            applyEvent.SetParameter("Duration", Duration);
            applyEvent.SetParameter("Zone", (object)zone);
            applyEvent.SetParameter("Random", (object)rng);
            applyEvent.SetParameter("Source", (object)user);
            ParentEntity.FireEvent(applyEvent);

            if (showUseMessage)
            {
                string verb = Drink ? "drinks" : "applies";
                string displayMsg = !string.IsNullOrEmpty(Message) ? Message
                    : $"{target.GetDisplayName()} {verb} {ParentEntity.GetDisplayName()}.";
                MessageLog.Add(displayMsg);
            }

            if (consumeItem && user != null)
                ConsumeItem(user);

            return true;
        }

        private void ApplyHealing(Entity target, Random rng)
        {
            if (string.IsNullOrEmpty(Healing))
                return;

            int healed = DiceRoller.Roll(Healing, rng);
            if (healed <= 0)
                return;

            var hp = target.GetStat("Hitpoints");
            if (hp == null)
                return;

            int before = hp.Value;
            hp.BaseValue = Math.Min(hp.BaseValue + healed, hp.Max);
            int actual = hp.Value - before;
            if (actual > 0)
                MessageLog.Add($"{target.GetDisplayName()} heals {actual} HP.");
        }

        private void ApplyStatBoost(Entity target)
        {
            // Format: "StatName:Amount" (e.g., "Strength:4")
            int colon = StatBoost.IndexOf(':');
            if (colon < 0) return;

            string statName = StatBoost.Substring(0, colon);
            if (!int.TryParse(StatBoost.Substring(colon + 1), out int amount)) return;

            var stat = target.GetStat(statName);
            if (stat == null) return;

            stat.Boost += amount;
            MessageLog.Add($"{target.GetDisplayName()} feels a surge of {statName}!");
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
                var inv = actor.GetPart<InventoryPart>();
                if (inv != null)
                    inv.RemoveObject(ParentEntity);
            }
        }
    }
}
