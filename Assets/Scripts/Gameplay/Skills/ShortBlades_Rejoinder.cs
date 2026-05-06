using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class defender-side riposte passive. Per Qud's
    /// <c>ShortBlades_Rejoinder.cs:21-92</c> — when an attack on the
    /// defender misses AND the defender has a ShortBlades weapon
    /// equipped, <see cref="CHANCE_PERCENT"/> chance to immediately
    /// counter-attack the original attacker.
    ///
    /// <para><b>Recursion guard (CoO):</b> instance-level
    /// <see cref="_recurring"/> flag — set true during the counter-
    /// attack, reset in <c>finally</c>. A Rejoinder-triggered swing
    /// that itself misses won't re-trigger via this skill.</para>
    ///
    /// <para>Weapon lookup: iterates <see cref="Body.ForeachEquippedObject"/>
    /// and picks the FIRST equipped item whose <see cref="MeleeWeaponPart.Attributes"/>
    /// contains "Piercing". Defender with no Piercing weapon equipped
    /// → no-op.</para>
    /// </summary>
    public class ShortBlades_Rejoinder : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Rejoinder);

        public const int CHANCE_PERCENT = 60;

        [System.NonSerialized]
        private bool _recurring;

        public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)
        {
            if (_recurring) return;
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;
            if (ctx.Rng == null || ctx.Zone == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            // Find a Piercing-class weapon equipped on the defender.
            var body = ctx.Defender.GetPart<Body>();
            if (body == null) return;

            MeleeWeaponPart found = null;
            body.ForeachEquippedObject((item, bp) =>
            {
                if (found != null || item == null) return;
                var w = item.GetPart<MeleeWeaponPart>();
                if (w != null && !string.IsNullOrEmpty(w.Attributes)
                    && w.Attributes.Contains("Piercing"))
                {
                    found = w;
                }
            });
            if (found == null) return;

            _recurring = true;
            try
            {
                // Defender swings at the original attacker.
                CombatSystem.PerformSingleAttack(
                    attacker: ctx.Defender, defender: ctx.Attacker,
                    weapon: found, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Rejoinder)");
            }
            finally
            {
                _recurring = false;
            }
        }
    }
}
