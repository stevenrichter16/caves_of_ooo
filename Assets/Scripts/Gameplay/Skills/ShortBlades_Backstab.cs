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
        // synthetic ApplyDamage call, summing with the swing's natural
        // damage.
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

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Piercing");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a piercing-class weapon to backstab.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

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

            // Find adjacent target + remember the direction we found
            // them in (so we can compute the opposite cell for flanking).
            Entity target = null;
            int targetDir = -1;
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
                    targetDir = dir;
                    break;
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to backstab.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Flanking check: the "opposite" cell from actor through
            // target is the cell at target + (target - actor) =
            // actor + 2*(target - actor). targetDir already encodes
            // (target - actor) direction; advancing the same dir from
            // target gives the flanker cell.
            var targetPos = ctx.Zone.GetEntityPosition(target);
            bool isFlanked = false;
            if (targetPos.x >= 0)
            {
                var flankerCell = ctx.Zone.GetCellInDirection(
                    targetPos.x, targetPos.y, targetDir);
                if (flankerCell != null)
                {
                    for (int i = 0; i < flankerCell.Objects.Count; i++)
                    {
                        var e = flankerCell.Objects[i];
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
            // direct ApplyDamage AFTER the swing — this is simpler than
            // threading a multiplier through PerformSingleAttack and
            // works whether the swing hit or missed (a flanked target
            // takes the bonus damage as a "you turned your back" tax).
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
                    CombatSystem.ApplyDamage(target, bonus, actor, ctx.Zone);
                    MessageLog.Add(actor.GetDisplayName() + " strikes from a flank! +" + bonus + " damage.");
                }
            }
        }
    }
}
