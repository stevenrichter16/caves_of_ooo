using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active: seizes one target at range and locks it in ice.
    ///
    /// <para>SPELLCRAFT SM6 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.4) —
    /// the cold branch of the same rule that makes soaked targets
    /// conduct. A wet target freezes deeper, because
    /// <see cref="FrozenEffect"/> now amplifies on moisture exactly as
    /// <see cref="ElectrifiedEffect"/> does. The power gets that for
    /// free by applying the effect normally.</para>
    ///
    /// <para>Single target on purpose: <see cref="FrozenEffect"/> blocks
    /// ALL action while present, so a version that froze a whole line
    /// would end fights outright rather than shape them.</para>
    /// </summary>
    public class Cryomancy_RimeGrip : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_RimeGrip);

        public const int COOLDOWN = 35;
        public const int GRIP_RANGE = 5;
        public const int GRIP_DAMAGE = 4;

        /// <summary>Base freeze. Modest, because a wet target multiplies
        /// it and because any positive Cold already blocks action.</summary>
        public const float GRIP_COLD = 0.5f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Rime Grip",
                Command = "CommandRimeGrip",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = GRIP_RANGE,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            bool blockedByWall;
            List<Entity> line = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, GRIP_RANGE,
                out blockedByWall);

            if (line.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s rime grip closes on nothing.");
                return;
            }

            var target = line[0];   // nearest — the grip takes ONE

            var dmg = new Damage(GRIP_DAMAGE);
            dmg.AddAttribute("Cold");
            CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

            if (target.GetStatValue("Hitpoints") <= 0)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s rime grip shatters "
                    + target.GetDisplayName() + "!");
                return;
            }

            target.ApplyEffect(new FrozenEffect(GRIP_COLD), actor, ctx.Zone);
            MessageLog.Add(actor.GetDisplayName() + "'s rime grip seizes "
                + target.GetDisplayName() + "!");
        }
    }
}
