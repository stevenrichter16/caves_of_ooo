using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Hydromancy active: a surge of water that soaks a line and drags
    /// what it catches TOWARD the caster.
    ///
    /// <para>SPELLCRAFT SM5 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.3) —
    /// the family's odd one out, and the only pull in the game. Every
    /// other movement power in every tree shoves things AWAY. Undertow
    /// exists to answer a specific problem the others cannot: the enemy
    /// worth soaking is the caster at the back, safely behind its own
    /// front rank.</para>
    ///
    /// <para><b>Why a pull is not just a reversed push.</b> Dragging an
    /// enemy toward you is a genuine cost — you end the turn closer to
    /// the thing you pulled, and to everything standing near it. The
    /// power is strongest for a character who WANTS that, which makes it
    /// a build decision rather than a strictly better shove.</para>
    /// </summary>
    public class Hydromancy_Undertow : BaseSkillPart
    {
        public override string Name => nameof(Hydromancy_Undertow);

        public const int COOLDOWN = 25;
        public const int UNDERTOW_RANGE = 3;
        public const int UNDERTOW_DAMAGE = 2;

        /// <summary>Lightest soaking in the tree — the drag is what you
        /// are paying for.</summary>
        public const float UNDERTOW_MOISTURE = 0.5f;

        public const int UNDERTOW_PULL_CELLS = 1;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Undertow",
                Command = "CommandUndertow",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = UNDERTOW_RANGE,
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
            List<Entity> targets = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, UNDERTOW_RANGE,
                out blockedByWall);

            if (targets.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s undertow finds nothing to drag.");
                return;
            }

            float moisture = HydromancySkill.ApplyMoistureBonus(actor, UNDERTOW_MOISTURE);
            int survivors = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                var dmg = new Damage(UNDERTOW_DAMAGE);
                // NOTE: "Water" is a descriptive tag only — it maps to
                // DamageAttributeFlags.None (Damage.cs:144-173), so this
                // damage is untyped and no elemental resistance reduces
                // it. That is intended: water is not an element you
                // resist here, it is a setup. Pinned by
                // HydromancyJetBlastTests.WaterDamage_IsUntyped.
                dmg.AddAttribute("Water");
                // RouteDamage, not ApplyDamage: scenery keeps its hitpoints on a
                // DestructiblePart, and ApplyDamage deliberately early-returns
                // on anything with no Hitpoints stat — so elemental damage aimed
                // at a tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(target, dmg, actor, ctx.Zone);

                if (target.GetStatValue("Hitpoints") <= 0) continue;
                survivors++;
                target.ApplyEffect(new WetEffect(moisture), actor, ctx.Zone);
            }

            // NEAREST-first for a pull — the mirror of the shove rule.
            // Each target vacates the cell the one behind it is about to
            // occupy, so the whole line closes in. Dragging the far end
            // first would jam it against the body in front.
            int dragged = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target.GetStatValue("Hitpoints") <= 0) continue;
                if (SkillCombatHelpers.TryPull(actor, target, ctx.Zone, UNDERTOW_PULL_CELLS))
                    dragged++;
            }

            if (dragged == 0 && survivors > 0)
                EmitSkillRejectedDiag(ctx, "pull_blocked");
            else if (survivors == 0)
                EmitSkillRejectedDiag(ctx, "all_targets_died");

            MessageLog.Add(actor.GetDisplayName() + "'s undertow hauls "
                + dragged + " of " + targets.Count + " closer!");
        }
    }
}
