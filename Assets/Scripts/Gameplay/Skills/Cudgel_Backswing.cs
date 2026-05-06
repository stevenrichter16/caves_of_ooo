using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class on-miss re-attack passive. Per Qud's
    /// <c>Cudgel_Backswing.cs:21-50</c> — when a Cudgel-class melee
    /// swing misses, <see cref="CHANCE_PERCENT"/> chance to immediately
    /// re-attack with the same weapon. Qud throttles to once per
    /// Game.Segments tick to prevent infinite recursion.
    ///
    /// <para><b>Recursion guard (CoO):</b> instance-level
    /// <see cref="_recurring"/> flag — set to true while the re-attack
    /// is in flight, reset in a <c>finally</c>. If a Backswing-triggered
    /// re-attack itself misses + invokes this skill's override, the
    /// flag short-circuits before the chance roll.</para>
    /// </summary>
    public class Cudgel_Backswing : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Backswing);

        public const int CHANCE_PERCENT = 25;

        // Recursion guard. Per-skill-instance because skills are
        // owned by their actor for life — same-actor recursion is
        // the only failure mode, and same-actor uses the same instance.
        [System.NonSerialized]
        private bool _recurring;

        public override void OnAttackerMeleeMiss(SkillEventContext ctx)
        {
            if (_recurring) return;  // bail on Backswing-of-a-Backswing
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;
            if (ctx.Weapon == null || string.IsNullOrEmpty(ctx.Weapon.Attributes)) return;
            if (!ctx.Weapon.Attributes.Contains("Cudgel")) return;
            if (ctx.Rng == null || ctx.Zone == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            _recurring = true;
            try
            {
                CombatSystem.PerformSingleAttack(
                    attacker: ctx.Attacker, defender: ctx.Defender,
                    weapon: ctx.Weapon, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Backswing)");
            }
            finally
            {
                _recurring = false;
            }
        }
    }
}
