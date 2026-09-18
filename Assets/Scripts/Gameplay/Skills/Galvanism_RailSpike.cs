using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism active: a lance of current that runs the full length of
    /// a rank, wounding everything it passes and grounding out in the
    /// last body it reaches.
    ///
    /// <para>SPELLCRAFT SM3 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.1) —
    /// the reach variant of the Ground Surge family. It exists for one
    /// specific problem: the enemy you actually want to prime is the
    /// caster at the back, and there is a wall of bodies in the
    /// way.</para>
    ///
    /// <para><b>Why the charge lands on the LAST target.</b> The spike
    /// grounds where it stops. Mechanically this is what makes the power
    /// a targeting decision rather than a bigger Ground Surge: you line
    /// the shot up so the creature you want Electrified is the furthest
    /// one the spike can reach. Kill the front rank first and the charge
    /// lands somewhere else — position is the skill.</para>
    ///
    /// <para><b>It does not push.</b> Deliberate: shoving would scatter
    /// the back-line target the power exists to reach, and the family
    /// already has two shoves.</para>
    /// </summary>
    public class Galvanism_RailSpike : SpellSkillPart
    {
        public override string Name => nameof(Galvanism_RailSpike);

        public const int COOLDOWN = 45;
        public const int SPIKE_RANGE = 6;
        public const int SPIKE_DAMAGE = 5;

        /// <summary>Charge grounded into the last body reached. Higher
        /// than <see cref="Galvanism_GroundSurge.ELECTRIFY_CHARGE"/>
        /// because it is guaranteed rather than a roll, and lands on
        /// exactly one target instead of the whole line.</summary>
        public const float SPIKE_CHARGE = 1.5f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Rail Spike",
                Command = "CommandRailSpike",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = SPIKE_RANGE,
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

            bool blockedByWall;
            var targets = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, SPIKE_RANGE,
                out blockedByWall);

            if (targets.Count == 0)
            {
                // "nothing was there" and "a wall was in the way" are
                // different answers to a player asking why the cast did
                // nothing, so they get different reason strings.
                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName()
                    + "'s rail spike finds nothing to ground against.");
                return false;
            }

            // The charge grounds in the furthest body the spike REACHED
            // — which is the last entry of the snapshot, whether that is
            // the tenth creature in a rank or the only one there.
            // Resolved before damage so a lethal hit cannot move the
            // "last" target out from under the charge mid-loop.
            Entity ground = targets[targets.Count - 1];
            SpellFxCapture.EndPathAt(ctx.Zone, ground);

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                var dmg = new Damage(SPIKE_DAMAGE);
                dmg.AddAttribute("Electric");
                dmg.AddAttribute("Lightning");
                // RouteDamage, not ApplyDamage: scenery keeps its hitpoints on a
                // DestructiblePart, and ApplyDamage deliberately early-returns
                // on anything with no Hitpoints stat — so elemental damage aimed
                // at a tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(target, dmg, actor, ctx.Zone);
            }

            // "bodies", not "bodys" — the siblings dodge this by stem
            // luck ("targets", "conductors"); this one needs the real
            // plural.
            string body = targets.Count + (targets.Count == 1 ? " body" : " bodies");
            bool grounded = ground.GetStatValue("Hitpoints") > 0;

            if (grounded)
            {
                ground.ApplyEffect(
                    new ElectrifiedEffect(charge: SPIKE_CHARGE), actor, ctx.Zone);
                MessageLog.Add(actor.GetDisplayName() + "'s rail spike runs through "
                    + body + " and grounds in " + ground.GetDisplayName() + "!");
            }
            else
            {
                // The spike killed the very target it meant to charge.
                // Say so in BOTH channels. The whole reason to pick Rail
                // Spike is to prime a specific body at the back of a
                // rank; telling the player it "grounds in <corpse>" would
                // have them plan their next turn around a charge that
                // does not exist. Latent bug surfaced by adversarial
                // review.
                EmitSkillRejectedDiag(ctx, "ground_target_died");
                MessageLog.Add(actor.GetDisplayName() + "'s rail spike runs through "
                    + body + ", but " + ground.GetDisplayName()
                    + " falls before the charge can ground!");
            }
        
            return true;
        }
    }
}
