using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Acrobatics active ability: swap positions with an adjacent
    /// creature. Works on enemies AND allies. On hostile targets, applies
    /// <see cref="ConfusedEffect"/> for <see cref="CONFUSED_DURATION"/>
    /// turns (the disorientation of being shoved past in a single
    /// motion). On allies, the swap is a clean position-trade with no
    /// debuff — useful for combat repositioning ("trade places with the
    /// scribe so he can reach the well").
    ///
    /// <para><b>Mechanic (CoO):</b> no weapon class required — Acrobatics
    /// is martial-arts-y by design (Dodge already shipped with no weapon
    /// gate). Adjacent target lookup mirrors Slam's 8-dir scan. The swap
    /// uses a three-phase
    /// <see cref="Zone.RemoveEntity"/>/<see cref="Zone.MoveEntity"/>/<see cref="Zone.AddEntity"/>
    /// sequence rather than a single MoveEntity, because MoveEntity's
    /// destination-cell occupancy check would refuse a target-on-target
    /// move. Phase order (target out → actor moves → target in) keeps
    /// every intermediate state having a vacant destination.</para>
    ///
    /// <para>The "is the target hostile" heuristic uses the absence of
    /// the <c>"Ally"</c> tag — same pattern ChainLightning's friendly-
    /// fire check uses. CoO doesn't have a robust faction system in the
    /// active code path yet (the SocialReputationPart is consultative,
    /// not authoritative for "is this a friend"), so the tag heuristic
    /// is the v1 answer.</para>
    ///
    /// <para>Per the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §Acrobatics_Tumble): "the
    /// only ability that EXCHANGES cells with another creature." Vault
    /// (proposed) crosses obstacles; Disengage (proposed) moves through
    /// open cells; Tumble swaps.</para>
    ///
    /// <para>Classification: <b>Match (mechanic family)</b> per
    /// CLAUDE.md §4.2 — Qud has <c>Acrobatics_Tumble</c> with similar
    /// swap-and-confuse semantics. Magnitude (1-turn confusion, 20T
    /// cooldown) per the CoO brainstorm tuning.</para>
    /// </summary>
    public class Acrobatics_Tumble : BaseSkillPart
    {
        public override string Name => nameof(Acrobatics_Tumble);

        public const int COOLDOWN = 20;
        public const int CONFUSED_DURATION = 1;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Tumble",
                Command = "CommandTumble",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Tumble doesn't roll dice for the swap (it's deterministic
            // movement) — but ConfusedEffect's CanApply path doesn't need
            // an Rng either, and the determinism rule is universal: a
            // null Rng signals a misconfigured caller. Bail like the
            // other actives.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

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

            // Find adjacent creature (mirrors Slam's 8-dir lookup).
            // Remember which direction we found them in so we know where
            // the actor is moving to (= target's current cell).
            Entity target = null;
            int targetDir = -1;
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
                    targetDir = dir;
                    break;
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has no one to tumble with.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            var targetPos = ctx.Zone.GetEntityPosition(target);
            if (targetPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "target_not_in_zone");
                return;
            }

            // Three-phase swap. Pull target OUT first so the actor's
            // destination is vacant; move actor; then re-add target on
            // the (now vacant) old actor cell. If any phase fails, undo
            // the prior phase to keep the zone in a consistent state.
            if (!ctx.Zone.RemoveEntity(target)) return;
            if (!ctx.Zone.MoveEntity(actor, targetPos.x, targetPos.y))
            {
                // Rollback: actor couldn't move (defense-in-depth — this
                // shouldn't fire because we just vacated the cell, but
                // future Zone changes might add validation that rejects).
                ctx.Zone.AddEntity(target, targetPos.x, targetPos.y);
                return;
            }
            ctx.Zone.AddEntity(target, actorPos.x, actorPos.y);

            // Hostile target → Confused 1T (per brainstorm). Allies get
            // a clean swap. The "Ally" tag is the v1 hostility flag —
            // mirrors ChainLightning's friendly-fire check.
            if (!target.Tags.ContainsKey("Ally"))
            {
                target.ApplyEffect(new ConfusedEffect(CONFUSED_DURATION),
                    actor, ctx.Zone);
            }

            MessageLog.Add(actor.GetDisplayName() + " tumbles past "
                + target.GetDisplayName() + ".");
        }
    }
}
