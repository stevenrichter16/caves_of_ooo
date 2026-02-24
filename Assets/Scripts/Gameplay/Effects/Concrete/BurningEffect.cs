namespace CavesOfOoo.Core
{
    /// <summary>
    /// Burning: deals fire damage each turn. Stacking resets duration.
    /// </summary>
    public class BurningEffect : Effect
    {
        public override string DisplayName => "burning";

        public string DamageDice;
        public System.Random Rng;

        public BurningEffect(int duration = 3, string damageDice = "1d4", System.Random rng = null)
        {
            Duration = duration;
            DamageDice = damageDice;
            Rng = rng ?? new System.Random();
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " catches fire!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer burning.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            int damage = DiceRoller.Roll(DamageDice, Rng);
            if (damage > 0)
            {
                Zone zone = context?.GetParameter<Zone>("Zone");
                CombatSystem.ApplyDamage(target, damage, null, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + damage + " fire damage.");
            }
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is BurningEffect burn)
            {
                // Reset duration to the new one (refreshes the burn)
                Duration = burn.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&R";
    }
}
