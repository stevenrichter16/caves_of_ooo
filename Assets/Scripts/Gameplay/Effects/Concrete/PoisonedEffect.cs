namespace CavesOfOoo.Core
{
    /// <summary>
    /// Poison: deals damage each owner turn. Reapplication keeps the longer
    /// remaining duration, preserving an indefinite dose and the first dose's potency.
    /// </summary>
    public class PoisonedEffect : Effect, IAuraProvider
    {
        public override string DisplayName => "poisoned";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>Saved inflictor of the first active dose; Owner remains the patient.</summary>
        public Entity DamageSource;
        public string DamageDice;
        public System.Random Rng;

        public PoisonedEffect(int duration = 5, string damageDice = "1d3", System.Random rng = null)
        {
            Duration = duration;
            DamageDice = damageDice;
            Rng = rng ?? new System.Random();
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is poisoned!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer poisoned.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            int damage = DiceRoller.Roll(DamageDice, Rng);
            if (damage > 0)
            {
                Zone zone = context?.GetParameter<Zone>("Zone");
                CombatSystem.ApplyDamage(target, damage, DamageSource, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + damage + " poison damage.");
            }
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is PoisonedEffect poison)
            {
                // A new dose refreshes exposure without banking an additive tail.
                // Keep existing dice/RNG: reapplication changes duration, not potency.
                Duration = Duration == DURATION_INDEFINITE || poison.Duration == DURATION_INDEFINITE
                    ? DURATION_INDEFINITE : System.Math.Max(Duration, poison.Duration);
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&*G"; // HDR — see GRAPHICS.md §3.B.3

        public AsciiFxTheme GetAuraTheme() => AsciiFxTheme.Poison;
    }
}
