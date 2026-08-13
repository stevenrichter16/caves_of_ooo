using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active: a spray of flame filling a cone ahead of the
    /// caster, setting everything caught in it alight.
    ///
    /// <para>SPELLCRAFT SM4 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.2) —
    /// the request's flamethrower, and the first power in the game to
    /// use the SM2 cone. Every other spell is a bolt, a line, a nova or
    /// a chain; the cone is what lets one cast prime a CLUMP rather than
    /// a file.</para>
    ///
    /// <para><b>Role in the grammar.</b> A skill spell PRIMES. Flame Jet
    /// is the family's heavy primer: long cooldown, hottest burn, widest
    /// coverage. It never reads a status to spend it — that is a rite's
    /// job (SM7). Note the tree's older
    /// <see cref="Pyromancy_Pyroclasm"/> DOES consume Burning; it
    /// predates this design and is grandfathered, not a precedent.</para>
    ///
    /// <para><b>Water beats fire.</b> Ignition routes through
    /// <see cref="PyroIgnition"/>, so a drenched target takes the heat
    /// but does not catch. That anti-synergy is deliberate: it is half
    /// of what teaches the player that soaking sets a target up for
    /// lightning and shields it from flame.</para>
    /// </summary>
    public class Pyromancy_FlameJet : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_FlameJet);

        public const int COOLDOWN = 35;
        public const int JET_LENGTH = 3;
        public const int JET_DAMAGE = 5;

        /// <summary>Hottest burn in the family — this is the cast you
        /// spend a long cooldown on.</summary>
        public const float JET_INTENSITY = 1.5f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Flame Jet",
                Command = "CommandFlameJet",
                Class = "Skills",
                // A cone is aimed exactly like a line: the player picks a
                // facing and the shape opens from there.
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = JET_LENGTH,
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

            List<Entity> targets = SpellTargeting.GetCreaturesInCone(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, JET_LENGTH);

            // PALIMPSEST: a flamethrower must light an oil slick. Heat
            // goes on the GROUND along the spray, not only into bodies —
            // the same gap that made FlamingHands unable to ignite tile
            // oil (reported from play).
            ZoneTileStateSystem.ApplyFireToTiles(
                ctx.Zone,
                SkillLine.CollectCells(ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, JET_LENGTH),
                actor, Name);


            if (targets.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s flame jet roars into empty air.");
                return;
            }

            int lit = 0, doused = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                var dmg = new Damage(JET_DAMAGE);
                dmg.AddAttribute("Fire");
                dmg.AddAttribute("Heat");
                // RouteDamage, not ApplyDamage: scenery keeps its hitpoints on a
                // DestructiblePart, and ApplyDamage deliberately early-returns
                // on anything with no Hitpoints stat — so elemental damage aimed
                // at a tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(target, dmg, actor, ctx.Zone);

                // Only the living catch fire.
                if (target.GetStatValue("Hitpoints") <= 0) continue;

                if (PyroIgnition.TryIgnite(target, JET_INTENSITY, actor, ctx.Zone, ctx.Rng))
                    lit++;
                else
                    doused++;
            }

            // A cast that hit only soaked targets is a real and
            // learnable outcome, not a malfunction — but it is worth a
            // record, because "why didn't they burn?" is exactly the
            // question a player will ask.
            if (lit == 0 && doused > 0)
                EmitSkillRejectedDiag(ctx, "all_targets_too_wet");

            MessageLog.Add(actor.GetDisplayName() + "'s flame jet engulfs "
                + targets.Count + " target" + (targets.Count == 1 ? "" : "s")
                + (lit > 0 ? ", setting " + lit + " alight!" : "!"));
        }
    }
}
