using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active: a quick gob of fire at one target.
    ///
    /// <para>SPELLCRAFT SM4 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.2) —
    /// the family's filler. Its identity is not power, it is
    /// AVAILABILITY: the shortest cooldown in the tree, so it is the
    /// cast you make on the turns when <see cref="Pyromancy_FlameJet"/>
    /// is still cooling. A prime-and-detonate loop needs something cheap
    /// to keep the stack topped up between big turns, or the rhythm
    /// collapses into waiting.</para>
    ///
    /// <para>Single target on purpose: it stops at the first body it
    /// hits. A piercing version would make <c>Galvanism_RailSpike</c>'s
    /// shape free and the family's geometry would stop meaning
    /// anything.</para>
    /// </summary>
    public class Pyromancy_EmberSpit : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_EmberSpit);

        /// <summary>Deliberately the cheapest in the tree — this is the
        /// power's whole identity, so it is pinned by a test that
        /// compares it against Flame Jet's.</summary>
        public const int COOLDOWN = 8;
        public const int SPIT_RANGE = 4;
        public const int SPIT_DAMAGE = 3;

        /// <summary>Lightest burn in the family. Cheap to cast, cheap in
        /// effect.</summary>
        public const float SPIT_INTENSITY = 0.6f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ember Spit",
                Command = "CommandEmberSpit",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = SPIT_RANGE,
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
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, SPIT_RANGE,
                out blockedByWall);

            if (line.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s ember sputters out.");
                return;
            }

            // FIRST body only. SkillLine returns nearest-first.
            var target = line[0];

            var dmg = new Damage(SPIT_DAMAGE);
            dmg.AddAttribute("Fire");
            dmg.AddAttribute("Heat");
            CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

            if (target.GetStatValue("Hitpoints") <= 0)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s ember drops "
                    + target.GetDisplayName() + "!");
                return;
            }

            bool lit = PyroIgnition.TryIgnite(
                target, SPIT_INTENSITY, actor, ctx.Zone, ctx.Rng);
            if (!lit) EmitSkillRejectedDiag(ctx, "target_too_wet");

            MessageLog.Add(actor.GetDisplayName() + "'s ember spatters across "
                + target.GetDisplayName() + (lit ? ", and catches!" : "."));
        }
    }
}
