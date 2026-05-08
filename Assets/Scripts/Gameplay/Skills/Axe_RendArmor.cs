using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class active ability: a deliberate cleave that doesn't deal
    /// damage but applies <see cref="REND_STACKS"/> stacks of
    /// <see cref="ShatterArmorEffect"/> directly to an adjacent target.
    /// Cudgel_ShatteringBlows is the passive variant (chance on hit);
    /// RendArmor is the guaranteed-on-cast active. Distinct from
    /// Disarm (equipment-slot mutation) — Rend is AV-stat reduction.
    ///
    /// <para><b>Mechanic:</b> requires an Axe-class weapon equipped.
    /// Finds an adjacent creature (mirrors Slam's 8-dir scan), then
    /// constructs a ShatterArmorEffect with
    /// <see cref="ShatterArmorEffect.StackCount"/> = REND_STACKS and
    /// applies it. The defender's effective AV drops by
    /// <see cref="ShatterArmorEffect.AV_REDUCTION"/> × REND_STACKS for
    /// the effect's duration.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Axe_RendArmor</c>): "the
    /// only ability that DIRECTLY applies armor-reduction stacks."</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2 — composes the existing ShatterArmorEffect's stacking model.</para>
    /// </summary>
    public class Axe_RendArmor : BaseSkillPart
    {
        public override string Name => nameof(Axe_RendArmor);

        public const int COOLDOWN = 30;
        public const int REND_STACKS = 3;
        public const int REND_DURATION = 6;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Rend Armor",
                Command = "CommandRendArmor",
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

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to rend armor.");
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
                MessageLog.Add(actor.GetDisplayName() + " has nothing to rend.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Apply REND_STACKS as a single effect with StackCount set
            // (rather than calling ApplyEffect REND_STACKS times — the
            // OnStack pattern would extend duration too, which we don't
            // want). The effect's GetAV consumer reads StackCount × 2
            // for the AV reduction.
            var effect = new ShatterArmorEffect(REND_DURATION) { StackCount = REND_STACKS };
            target.ApplyEffect(effect, actor, ctx.Zone);
        }
    }
}
