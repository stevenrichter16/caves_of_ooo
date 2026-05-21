namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8c — gas-induced sleep effect. Distinct from
    /// <see cref="HibernatingEffect"/> (which is a self-buff: heals +
    /// max resistances) — this is a debuff applied by inhaling sleep
    /// gas. Blocks action, wakes on damage, refreshes Duration on
    /// re-application.
    ///
    /// <para>Mirrors Qud's <c>XRL.World.Effects.Asleep</c> pattern
    /// from <c>GasSleep</c> but adapted to CoO's event vocabulary.</para>
    /// </summary>
    public class AsleepByGasEffect : Effect
    {
        public override string DisplayName => "gas-asleep";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public AsleepByGasEffect(int duration = 5)
        {
            Duration = duration;
        }

        /// <summary>Sleeping creatures cannot act. Same contract as
        /// <see cref="StunnedEffect"/> and <see cref="HibernatingEffect"/>
        /// (both also return false). Pin: the action-block IS the
        /// effect's point.</summary>
        public override bool AllowAction(Entity target) => false;

        public override void OnApply(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " falls asleep from the gas.");
        }

        public override void OnRemove(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " wakes up.");
        }

        /// <summary>Wake-on-damage — canonical sleep behavior. Any
        /// non-zero damage drops Duration to 0; the next
        /// <see cref="StatusEffectsPart"/> EndTurn sweep removes the
        /// effect. Zero-damage (fully resisted) hits do NOT wake — pin
        /// via the Counter test.</summary>
        public override void OnTakeDamage(Entity target, GameEvent e)
        {
            if (target == null || e == null) return;
            var damage = e.GetParameter<Damage>("Damage");
            if (damage == null || damage.Amount <= 0) return;
            if (Duration > 0)
            {
                Duration = 0;
                MessageLog.Add(target.GetDisplayName() + " is jolted awake!");
            }
        }

        /// <summary>Refresh-on-reapply: take the larger Duration.
        /// Mirrors <see cref="PoisonedByGasEffect.OnStack"/> shape.</summary>
        public override bool OnStack(Effect incoming)
        {
            if (incoming is AsleepByGasEffect other)
            {
                if (other.Duration > Duration) Duration = other.Duration;
                return true;
            }
            return false;
        }
    }
}
