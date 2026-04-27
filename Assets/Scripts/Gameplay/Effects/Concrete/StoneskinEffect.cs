namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stoneskin: reduces incoming damage by a fixed amount via Phase F's
    /// <c>BeforeTakeDamage</c> event. The reduction applies BEFORE elemental
    /// resistance (Phase E), so Stoneskin and resistance stack additively in
    /// game-feel terms (10 dmg − 2 Stoneskin = 8, then × 0.5 fire resist = 4).
    ///
    /// First Tier 2 production listener for the Phase F event hook. Future
    /// status effects (e.g., MagicShield, ScaleBarkPotion) can follow the
    /// same pattern by overriding <see cref="Effect.OnBeforeTakeDamage"/>.
    ///
    /// Stacking: two Stoneskin instances stack additively — each effect's
    /// <see cref="OnBeforeTakeDamage"/> independently subtracts its own
    /// <see cref="Reduction"/>. This matches the BleedingEffect precedent
    /// of treating multiple applications as cumulative.
    /// </summary>
    public class StoneskinEffect : Effect
    {
        public override string DisplayName => "stoneskin";

        /// <summary>
        /// Damage reduction per incoming hit. Defaults to 2. Future content
        /// (mutations, equipment, bigger spell ranks) can vary this.
        /// </summary>
        public int Reduction;

        public StoneskinEffect(int reduction = 2, int duration = DURATION_INDEFINITE)
        {
            Reduction = reduction;
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s skin hardens to stone.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s stoneskin fades.");
        }

        public override void OnBeforeTakeDamage(Entity target, GameEvent e)
        {
            if (e.GetParameter("Damage") is Damage damage)
            {
                // Damage.Amount setter clamps to ≥ 0 — over-reduction is safe.
                damage.Amount -= Reduction;
            }
        }
    }
}
