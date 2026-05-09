using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// ShortBlades-class active ability: a "kick when they're down"
    /// melee strike that gains
    /// <see cref="PEN_PER_NEG_EFFECT"/> bonus penetration per negative
    /// status effect on the target. Per Qud's <c>ShortBlades_Shank</c>
    /// (XRL.World.Parts.Skill/ShortBlades_Shank.cs:46-135) — Qud's Cast
    /// computes <c>num = GetShankEffectCount(target) * 2</c> and passes
    /// it as the swing's pen bonus.
    ///
    /// <para><b>Mechanic (CoO):</b> requires a Piercing-attribute
    /// weapon equipped (Dagger / Spear / ChoirSpine / TemporalShard /
    /// GlassblownStiletto / etc.) AND an adjacent Creature. Counts
    /// effects on the target whose <see cref="Effect.GetEffectType"/>
    /// includes <see cref="Effect.TYPE_NEGATIVE"/> (Qud-parity flag —
    /// the WSP6.16 backfill ensures every standard CoO debuff carries
    /// it). The pen bonus is applied via the
    /// <see cref="BaseSkillPart.OnGetPenetrationModifier"/> hook for
    /// the duration of the Shank swing only — the
    /// <see cref="_activePenBonus"/> field is set before
    /// <see cref="CombatSystem.PerformSingleAttack"/> and reset in
    /// <c>finally</c>, so it never leaks to a non-Shank swing.</para>
    ///
    /// <para>Cooldown <see cref="COOLDOWN"/> turns (Qud parity for
    /// primary-hand). Marker tag <c>(Shank)</c> in the message log so
    /// players + tests can see when the ability fired.</para>
    ///
    /// <para>Classification: <b>Match</b> per CLAUDE.md §4.2 — mechanic
    /// (count-negative-effects, pen-per-effect, swing-with-bonus) and
    /// magnitude (×2 per effect, 10T cooldown) verbatim from Qud. The
    /// CoO-specific twist is the use of the WSP6.6 pen-modifier hook
    /// (instead of Qud's `Combat.MeleeAttackWithWeapon` per-call
    /// pen-bonus parameter) to thread the temporary buff cleanly.</para>
    /// </summary>
    public class ShortBlades_Shank : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Shank);

        public const int COOLDOWN = 10;
        public const int PEN_PER_NEG_EFFECT = 2;

        // Transient per-swing pen-bonus. Set in OnCommand BEFORE
        // PerformSingleAttack and reset in its finally block. Read by
        // OnGetPenetrationModifier during the swing's pen calculation.
        // [NonSerialized] because save/load must never preserve a
        // mid-swing buff value (post-load this MUST be 0).
        [System.NonSerialized]
        private int _activePenBonus = 0;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Shank",
                Command = "CommandShank",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        /// <summary>
        /// Returns the per-swing pen bonus for this skill. Active only
        /// during the Shank swing's call to PerformSingleAttack — see
        /// <see cref="OnCommand"/> for the try/finally that owns the
        /// lifetime of <see cref="_activePenBonus"/>.
        /// </summary>
        public override int OnGetPenetrationModifier(Entity actor, MeleeWeaponPart weapon)
        {
            return _activePenBonus;
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require a Piercing-class weapon equipped.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Piercing");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a piercing-class weapon to shank.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

            // Need Zone to find adjacent target.
            if (ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "no_zone");
                return;
            }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return;
            }

            // Find adjacent Creature (mirrors Cudgel_Slam's lookup).
            Entity target = null;
            for (int dir = 0; dir < 8 && target == null; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    target = e;
                    break;
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to shank.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Count negative effects on target. Qud uses `effect.IsOfType(33554432)`
            // — that's TYPE_NEGATIVE (which CoO carries as the same value;
            // Effect.cs:31). The WSP6.16 backfill on every standard CoO
            // debuff makes this query honest.
            int negCount = CountNegativeEffects(target);

            // Compute + thread the pen bonus through the swing. The
            // finally block guarantees we reset even if PerformSingleAttack
            // throws (defense-in-depth — leak-free per-swing buff).
            try
            {
                _activePenBonus = PEN_PER_NEG_EFFECT * negCount;
                CombatSystem.PerformSingleAttack(
                    attacker: actor, defender: target,
                    weapon: weapon, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Shank)");
            }
            finally
            {
                _activePenBonus = 0;
            }
        }

        /// <summary>
        /// Count effects on <paramref name="target"/> whose
        /// <see cref="Effect.GetEffectType"/> includes
        /// <see cref="Effect.TYPE_NEGATIVE"/>. Public + static so tests
        /// can pin the count behavior independently of the OnCommand path.
        /// </summary>
        public static int CountNegativeEffects(Entity target)
        {
            var part = target?.GetPart<StatusEffectsPart>();
            if (part == null) return 0;
            int count = 0;
            var effects = part.GetAllEffects();
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].IsOfType(Effect.TYPE_NEGATIVE)) count++;
            }
            return count;
        }
    }
}
