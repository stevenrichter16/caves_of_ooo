using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// ShortBlades-class active ability: a strike whose damage is
    /// multiplied by <see cref="BACKSTAB_DAMAGE_PERCENT"/>% if the
    /// target is "flanked" — i.e., a creature directly opposite the
    /// attacker (so the target is between attacker and ally). Distinct
    /// from Shank (pen bonus), Flurry (multi-strike), and Tumble
    /// (cell swap) — Backstab gates damage on positional geometry.
    ///
    /// <para><b>Mechanic:</b> requires a Piercing-class weapon equipped.
    /// Finds an adjacent creature (mirrors Shank's 8-dir scan). Computes
    /// the cell directly opposite the attacker through the target — if
    /// that cell contains a Creature (any non-attacker non-target
    /// Creature counts as a flanker), the swing fires with bonus damage
    /// via a transient damage modifier on the BeforeTakeDamage hook.
    /// Otherwise: normal weapon swing.</para>
    ///
    /// <para>The flanking detection uses Chebyshev geometry: for the
    /// 8 cardinal/diagonal offsets, the "opposite" of (dx, dy) is
    /// (-dx, -dy). Looking from the target's cell toward (-dx, -dy)
    /// reaches a candidate flanker cell.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §ShortBlades_Backstab</c>):
    /// "the only ability that gates on positional geometry (flanking)."</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2.</para>
    /// </summary>
    public class ShortBlades_Backstab : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Backstab);

        public const int COOLDOWN = 20;
        public const int BACKSTAB_DAMAGE_PERCENT = 200; // ×2 on flank

        // Transient flanking-detected flag. Read by Shank-style threading
        // pattern: SET before PerformSingleAttack, RESET in finally. The
        // damage multiplier applies via a temporary CombatSystem hook
        // (BeforeTakeDamage) that this skill registers — but to keep the
        // change small for v1, we apply the bonus AFTER the swing as a
        // guaranteed-hit follow-up via SkillCombatHelpers.DealGuaranteedHitDamage
        // (Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM8/B2), summing with
        // the swing's natural damage.
        [System.NonSerialized]
        private bool _isFlanked = false;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Backstab",
                Command = "CommandBackstab",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Piercing");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a piercing-class weapon to backstab.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return false;
            }

            if (ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "no_zone");
                return false;
            }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return false;
            }

            // Find adjacent target + remember the direction we found
            // them in (so we can compute the opposite cell for flanking).
            var target = MultiCellAbilityQueries.FirstAdjacentCreature(ctx.Zone, actor, out var contact);
            int targetDir = MultiCellAbilityQueries.ContactDirection(ctx.Zone, actor, contact);

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to backstab.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            // Flanking is the first cell beyond the target's physical
            // body in the contact direction. The target never flanks itself.
            var targetPos = ctx.Zone.GetEntityPosition(target);
            bool isFlanked = false;
            if (targetPos.x >= 0)
            {
                // Follow the attack direction through this body. Its next
                // occupied cell cannot count as its own flanker.
                var flankerCell = ctx.Zone.GetCellInDirection(contact.X, contact.Y, targetDir);
                while (flankerCell != null && flankerCell.Occupants.Contains(target))
                    flankerCell = ctx.Zone.GetCellInDirection(flankerCell.X, flankerCell.Y, targetDir);
                if (flankerCell != null)
                {
                    for (int i = 0; i < flankerCell.Occupants.Count; i++)
                    {
                        var e = flankerCell.Occupants[i];
                        if (e == null || e == actor || e == target) continue;
                        if (e.Tags.ContainsKey("Creature"))
                        {
                            isFlanked = true;
                            break;
                        }
                    }
                }
            }

            // Normal swing first. If flanked, apply bonus damage as a
            // guaranteed-hit follow-up AFTER the swing — this is simpler
            // than threading a multiplier through PerformSingleAttack and
            // works whether the swing hit or missed (a flanked target
            // takes the bonus damage as a "you turned your back" tax).
            // Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM8/B2 -- routes
            // through the on-hit dispatch chain (SkillCombatHelpers.
            // DealGuaranteedHitDamage) instead of the raw ApplyDamage(int)
            // overload, which skipped it entirely (the flank bonus is a
            // second hit-sized chunk of damage and deserves its own
            // on-hit proc roll, same as the base swing).
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.PerformSingleAttack(
                attacker: actor, defender: target,
                weapon: weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(Backstab)");

            if (isFlanked && target.GetStatValue("Hitpoints") > 0)
            {
                int damageDealt = hpBefore - target.GetStatValue("Hitpoints");
                // Bonus = (multiplier - 100%) × damage_dealt.
                // BACKSTAB_DAMAGE_PERCENT = 200 → bonus = 100% = damage_dealt.
                int bonus = (damageDealt * (BACKSTAB_DAMAGE_PERCENT - 100)) / 100;
                if (bonus >= 1)
                {
                    SkillCombatHelpers.DealGuaranteedHitDamage(
                        actor, target, weapon, bonus, ctx.Zone, ctx.Rng);
                    MessageLog.Add(actor.GetDisplayName() + " strikes from a flank! +" + bonus + " damage.");
                }
            }
        
            return true;
        }
    }
}
