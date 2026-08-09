using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Hydromancy active: a heavy sphere of water lobbed down a line,
    /// bursting on the first body it reaches and soaking everything
    /// around the impact.
    ///
    /// <para>SPELLCRAFT SM5 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.3) —
    /// the reach variant. <see cref="Hydromancy_JetBlast"/> soaks what is
    /// already on top of you; Drench Lob primes a clump BEFORE it
    /// closes, which is the difference between setting up a combo and
    /// reacting to one.</para>
    ///
    /// <para><b>No damage, no push.</b> Deliberate: it is pure setup, and
    /// the trade for its reach. Shoving here would scatter the very
    /// cluster it just soaked.</para>
    ///
    /// <para><b>SCOPE DIVERGENCE.</b> The plan called this "lobbed,
    /// radius 2 at range 6" with a freely chosen impact point. The
    /// targeting system has three modes — AdjacentCell, DirectionLine,
    /// SelfCentered (AbilityTargetingMode.cs:8-10) — and none of them
    /// lets a player pick an arbitrary cell at range. Rather than grow
    /// the input/UI layer inside a content milestone, the lob flies
    /// along an aimed line and bursts where it stops: on the first
    /// creature it meets, or at maximum range. Same radius, same reach,
    /// aimed the way every other ranged power in the game is aimed.</para>
    /// </summary>
    public class Hydromancy_DrenchLob : BaseSkillPart
    {
        public override string Name => nameof(Hydromancy_DrenchLob);

        public const int COOLDOWN = 30;
        public const int LOB_RANGE = 6;
        public const int LOB_RADIUS = 2;

        /// <summary>Lighter than a point-blank jet: it is spread over an
        /// area and delivered at range.</summary>
        public const float LOB_MOISTURE = 0.6f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Drench Lob",
                Command = "CommandDrenchLob",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = LOB_RANGE,
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

            // Fly until we hit a body or run out of range. Unlike a line
            // power we want the IMPACT CELL, not the creature list, so
            // the walk is here rather than in SkillLine.
            int impactX = actorPos.x, impactY = actorPos.y;
            bool foundBody = false;
            for (int step = 0; step < LOB_RANGE; step++)
            {
                int nx = impactX + dx, ny = impactY + dy;
                if (!ctx.Zone.InBounds(nx, ny)) break;
                var cell = ctx.Zone.GetCell(nx, ny);
                if (cell == null) break;
                if (cell.IsSolid()) break;   // splashes against the wall, short

                impactX = nx; impactY = ny;

                bool creatureHere = false;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (e.Tags.ContainsKey("Creature")) { creatureHere = true; break; }
                }
                if (creatureHere) { foundBody = true; break; }
            }

            if (impactX == actorPos.x && impactY == actorPos.y)
            {
                // Never left the caster's cell — walled in immediately.
                EmitSkillRejectedDiag(ctx, "line_blocked");
                MessageLog.Add(actor.GetDisplayName() + "'s lob has nowhere to fly.");
                return;
            }

            var caught = SpellTargeting.GetCreaturesInRadius(
                ctx.Zone, impactX, impactY, LOB_RADIUS, exclude: actor);

            if (caught.Count == 0)
            {
                // A burst on empty ground is a wasted turn, and the
                // reason is worth recording: "I aimed at nothing" reads
                // very differently from "the lob was blocked".
                EmitSkillRejectedDiag(ctx, foundBody ? "no_target" : "burst_on_empty_ground");
                MessageLog.Add(actor.GetDisplayName() + "'s lob bursts harmlessly.");
                return;
            }

            float moisture = HydromancySkill.ApplyMoistureBonus(actor, LOB_MOISTURE);
            for (int i = 0; i < caught.Count; i++)
                caught[i].ApplyEffect(new WetEffect(moisture), actor, ctx.Zone);

            MessageLog.Add(actor.GetDisplayName() + "'s drench lob bursts, soaking "
                + caught.Count + " target" + (caught.Count == 1 ? "" : "s") + "!");
        }
    }
}
