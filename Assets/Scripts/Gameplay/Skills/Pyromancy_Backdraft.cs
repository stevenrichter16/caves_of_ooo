using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active: a short, violent bloom of flame that both burns
    /// and shoves.
    ///
    /// <para>SPELLCRAFT SM4 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.2) —
    /// the family's space-maker. It is the only fire power that MOVES
    /// anything, which is its whole reason to exist: you use it with
    /// something already on top of you, aimed at whatever you are
    /// retreating from, and it leaves them burning at arm's length.</para>
    ///
    /// <para>It trades reach for that: a shorter cone than
    /// <see cref="Pyromancy_FlameJet"/> and a cooler burn, in exchange
    /// for the shove.</para>
    ///
    /// <para><b>SCOPE DIVERGENCE.</b> The plan (§6.2) specified a cone
    /// "behind the caster". Implemented as an ordinary aimed cone
    /// instead: firing opposite the aimed direction would mean the
    /// player points one way and the flame goes the other, which is a UI
    /// trap rather than a tactic. The intended USE — rear-guard while
    /// falling back — is preserved by aiming at the pursuer, which is
    /// what a player does naturally.</para>
    /// </summary>
    public class Pyromancy_Backdraft : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_Backdraft);

        public const int COOLDOWN = 25;
        public const int DRAFT_LENGTH = 2;
        public const int DRAFT_DAMAGE = 4;
        public const float DRAFT_INTENSITY = 1.0f;

        /// <summary>Cells each target is shoved.</summary>
        public const int DRAFT_PUSH_CELLS = 1;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Backdraft",
                Command = "CommandBackdraft",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = DRAFT_LENGTH,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            List<Entity> targets = SpellTargeting.GetCreaturesInCone(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, DRAFT_LENGTH);

            // PALIMPSEST: a flamethrower must light an oil slick. Heat
            // goes on the GROUND along the spray, not only into bodies —
            // the same gap that made FlamingHands unable to ignite tile
            // oil (reported from play).
            ZoneTileStateSystem.ApplyFireToTiles(
                ctx.Zone,
                SkillLine.CollectCells(ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, DRAFT_LENGTH),
                actor, Name);


            if (targets.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s backdraft finds nothing to push.");
                return false;
            }

            int lit = 0, survivors = 0;

            // Pass 1: damage and ignite.
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                var dmg = new Damage(DRAFT_DAMAGE);
                dmg.AddAttribute("Fire");
                dmg.AddAttribute("Heat");
                CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

                if (target.GetStatValue("Hitpoints") <= 0) continue;
                survivors++;
                if (PyroIgnition.TryIgnite(target, DRAFT_INTENSITY, actor, ctx.Zone, ctx.Rng))
                    lit++;
            }

            // Pass 2, FURTHEST-first: shove. Same ordering rule Ground
            // Surge learned the hard way — TryPush refuses a step into an
            // occupied cell, so shoving nearest-first would slam each
            // target into the body behind it and only the last would
            // move. SpellTargeting.GetCreaturesInCone walks outward, so
            // the tail of the list is the far edge of the cone.
            int shoved = 0;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.GetStatValue("Hitpoints") <= 0) continue;
                if (SkillCombatHelpers.TryPush(actor, target, ctx.Zone, DRAFT_PUSH_CELLS))
                    shoved++;
            }

            if (shoved == 0 && survivors > 0)
                EmitSkillRejectedDiag(ctx, "push_blocked");
            else if (survivors == 0)
                EmitSkillRejectedDiag(ctx, "all_targets_died");

            MessageLog.Add(actor.GetDisplayName() + "'s backdraft blooms across "
                + targets.Count + " target" + (targets.Count == 1 ? "" : "s")
                + (lit > 0 ? ", " + lit + " alight" : "")
                + (shoved > 0 ? " and driven back!" : "!"));
        
            return true;
        }
    }
}
