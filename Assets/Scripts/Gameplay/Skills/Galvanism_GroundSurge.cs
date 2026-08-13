using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism active: a shockwave that rolls along the ground in a
    /// chosen direction, damaging every creature it passes, shoving each
    /// one a cell further away, and often leaving them Electrified.
    ///
    /// <para>SPELLCRAFT SM3 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.1) —
    /// the request's shockwave, verbatim: <i>"a shockwave that has a
    /// chance to electrocute and pushes the victim one tile."</i></para>
    ///
    /// <para><b>Role in the grammar.</b> A skill spell PRIMES: it applies
    /// a status and never consumes one. Ground Surge leaves targets
    /// Electrified so a later grimoire rite can cash that charge in
    /// (SM7). It deliberately does not read what is already on the
    /// target — spending statuses is what makes a rite a rite.</para>
    ///
    /// <para><b>Contrast with <see cref="Galvanism_Overload"/>.</b>
    /// Overload chains only through conductors and is stopped cold by
    /// the first dry body. Ground Surge is a physical wave: it hits
    /// everything in the line regardless of conductivity, and its job is
    /// to CREATE the conductors Overload needs.</para>
    /// </summary>
    public class Galvanism_GroundSurge : BaseSkillPart
    {
        public override string Name => nameof(Galvanism_GroundSurge);

        public const int COOLDOWN = 30;
        public const int SURGE_RANGE = 4;
        public const int SURGE_DAMAGE = 6;

        /// <summary>Chance per target to leave <see cref="ElectrifiedEffect"/>.
        /// The shove is unconditional; only the prime is a roll.</summary>
        public const int ELECTRIFY_PERCENT = 40;

        /// <summary>Charge left on a successful roll. Doubled by
        /// <see cref="ElectrifiedEffect.OnApply"/> if the target is
        /// already Wet — the soak-then-shock payoff.</summary>
        public const float ELECTRIFY_CHARGE = 1.0f;

        /// <summary>Charge left in the ground (the coarse 0..2 tile
        /// channel, not ElectrifiedEffect's float).</summary>
        public const int GroundCharge = 1;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ground Surge",
                Command = "CommandGroundSurge",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = SURGE_RANGE,
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

            // Collect the whole line BEFORE touching anything. Pushing a
            // target mid-walk would move it into a cell the walk has not
            // reached yet and hit it a second time from one cast.
            // PALIMPSEST P2 — the surge leaves charge in the ground it
            // rolled across, not only in the bodies it hit. Written
            // BEFORE the no-target check on purpose: a wave that hits
            // nobody still crosses the floor, and a charged puddle with
            // no one standing in it is exactly the setup this system
            // exists to allow. (The first draft wrote this after the
            // early return, so a miss charged nothing — caught by
            // GroundSurge_ChargesTheGroundItRollsAcross.)
            var lineCells = SkillLine.CollectCells(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, SURGE_RANGE);
            for (int i = 0; i < lineCells.Count; i++)
                ZoneTileStateSystem.AddCharge(ctx.Zone, lineCells[i].X, lineCells[i].Y,
                    GroundCharge, actor, Name);

            bool blockedByWall;
            var targets = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, SURGE_RANGE,
                out blockedByWall);

            if (targets.Count == 0)
            {
                // "nothing was there" and "a wall was in the way" are
                // different answers to a player asking why the cast did
                // nothing, so they get different reason strings.
                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName()
                    + "'s ground surge rolls away into nothing.");
                return;
            }

            int shoved = 0, primed = 0, survivors = 0;

            // Pass 1, nearest-first: damage and prime.
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                var dmg = new Damage(SURGE_DAMAGE);
                dmg.AddAttribute("Electric");
                dmg.AddAttribute("Lightning");
                // RouteDamage, not ApplyDamage: scenery keeps its hitpoints on a
                // DestructiblePart, and ApplyDamage deliberately early-returns
                // on anything with no Hitpoints stat — so elemental damage aimed
                // at a tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(target, dmg, actor, ctx.Zone);

                // Effects only land on the living. A corpse cannot be
                // primed — there is nothing left to detonate.
                if (target.GetStatValue("Hitpoints") <= 0) continue;
                survivors++;

                if (ctx.Rng != null && ctx.Rng.Next(100) < ELECTRIFY_PERCENT)
                {
                    target.ApplyEffect(
                        new ElectrifiedEffect(charge: ELECTRIFY_CHARGE), actor, ctx.Zone);
                    primed++;
                }
            }

            // Pass 2, FURTHEST-first: shove. Order is load-bearing.
            // TryPush refuses a step into a cell holding another
            // creature, so pushing nearest-first would slam every target
            // into the body behind it and only the last one in the rank
            // would actually move — in a packed corridor, the canonical
            // use of a line AoE, the shockwave would shove exactly one
            // enemy. Going outward-in vacates each destination before
            // the target behind it steps into it.
            // Latent bug surfaced by adversarial review — see
            // GalvanismGroundSurgeTests.GroundSurge_ShovesEveryTargetInAPackedRank_NotJustTheFurthest.
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.GetStatValue("Hitpoints") <= 0) continue;
                if (SkillCombatHelpers.TryPush(actor, target, ctx.Zone)) shoved++;
            }

            // Only blame the geometry when something was actually
            // standing there to be shoved. If the surge killed the whole
            // line, "push_blocked" would send a future debugger hunting
            // a wall that was never there.
            if (shoved == 0 && survivors > 0)
                EmitSkillRejectedDiag(ctx, "push_blocked");
            else if (survivors == 0)
                EmitSkillRejectedDiag(ctx, "all_targets_died");

            // P3 — resolve what the writes just created, now rather than
            // at end of turn: lightning into a puddle must electrify it
            // on the cast, or the rule reads as a bug.
            ZoneTileStateSystem.ResolveAfterAbility(ctx.Zone, actor);

            MessageLog.Add(actor.GetDisplayName() + "'s ground surge slams through "
                + targets.Count + " target" + (targets.Count == 1 ? "" : "s")
                + (primed > 0 ? ", leaving " + primed + " crackling!" : "!"));
        }
    }
}
