using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class active ability: smash the ground in place — every
    /// adjacent creature takes weapon-damage scaled by
    /// <see cref="GROUND_POUND_DAMAGE_MULTIPLIER"/>, gets Stunned for
    /// <see cref="STUN_DURATION"/> turns, and is knocked back 1 cell
    /// away from the attacker. Distinct from Whirlwind (full damage,
    /// no knockback) — GroundPound trades raw damage for AOE control.
    ///
    /// <para><b>Mechanic:</b> requires a Cudgel-class weapon equipped.
    /// Iterates 8 directions (N→NE→E→SE→S→SW→W→NW), strikes each
    /// adjacent creature with rolled-from-weapon damage × 0.75
    /// (defaulting to 1 floor on tiny weapons), applies StunnedEffect,
    /// and pushes the target 1 cell directly away from the attacker.
    /// Knockback uses Zone.MoveEntity — if the destination is solid or
    /// occupied, the target stays put but the damage + stun still
    /// land.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cudgel_GroundPound</c>):
    /// "the only self-AOE knockback in the table." Whirlwind does
    /// self-AOE damage but no knockback. Pyroclasm and Overload do
    /// AOE but consume stacks rather than radiate from self.</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2.</para>
    /// </summary>
    public class Cudgel_GroundPound : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_GroundPound);

        public const int COOLDOWN = 40;
        public const int STUN_DURATION = 1;
        // 0.75 = 75% of weapon damage. Fixed-point as a 100-divided ratio
        // to dodge floating-point determinism issues in tests.
        public const int GROUND_POUND_DAMAGE_PERCENT = 75;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ground Pound",
                Command = "CommandGroundPound",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a cudgel-class weapon to ground pound.");
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

            // Snapshot adjacent creatures + their direction-from-actor
            // (used to push them away). Using a parallel list rather
            // than a dict so the order stays N→NE→E→... for determinism.
            var targets = new List<(Entity creature, int dirFromActor)>(8);
            for (int dir = 0; dir < 8; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    targets.Add((e, dir));
                    break;
                }
            }

            if (targets.Count == 0)
            {
                MessageLog.Add(actor.GetDisplayName() + " pounds the ground — nothing nearby!");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Damage roll per target (independent rolls keep the per-
            // hit RNG variance honest — a low-roll target still gets
            // stunned + knocked back, which is the real value).
            for (int i = 0; i < targets.Count; i++)
            {
                var (target, dirFromActor) = targets[i];

                // Roll the weapon's base damage, scale to 75%, floor at 1.
                int rolled = !string.IsNullOrEmpty(weapon.BaseDamage)
                    ? DiceRoller.Roll(weapon.BaseDamage, ctx.Rng)
                    : 0;
                int dmg = (rolled * GROUND_POUND_DAMAGE_PERCENT) / 100;
                if (dmg < 1) dmg = 1;
                CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

                // Stun + knockback (only if defender survived).
                if (target.GetStatValue("Hitpoints") > 0)
                {
                    target.ApplyEffect(new StunnedEffect(STUN_DURATION),
                        actor, ctx.Zone);

                    // Push 1 cell away from actor. Direction-from-actor
                    // is the dir lookup index (0..7); GetCellInDirection
                    // works on absolute positions so we re-resolve from
                    // the target's CURRENT position (they may have moved).
                    var targetPos = ctx.Zone.GetEntityPosition(target);
                    if (targetPos.x >= 0)
                    {
                        var pushTo = ctx.Zone.GetCellInDirection(
                            targetPos.x, targetPos.y, dirFromActor);
                        if (pushTo != null && !pushTo.IsSolid()
                            && !CellHasOtherCreature(pushTo, target))
                        {
                            ctx.Zone.MoveEntity(target, pushTo.X, pushTo.Y);
                        }
                    }
                }
            }

            MessageLog.Add(actor.GetDisplayName() + " pounds the ground!");
        }

        private static bool CellHasOtherCreature(Cell cell, Entity exclude)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == null || e == exclude) continue;
                if (e.Tags.ContainsKey("Creature")) return true;
            }
            return false;
        }
    }
}
