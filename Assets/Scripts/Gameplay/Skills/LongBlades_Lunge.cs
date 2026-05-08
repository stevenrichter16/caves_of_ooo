using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// LongBlades-class active ability: extend the blade up to
    /// <see cref="LUNGE_RANGE"/> cells in the player-chosen direction
    /// and strike the first creature in that line. Actor does NOT
    /// move — Lunge is a reach-extension swing, not a charge.
    ///
    /// <para><b>Mechanic (CoO):</b> requires a LongBlades-attribute
    /// weapon equipped (LongSword / Greatsword / Claymore / ShortSword
    /// / etc.). Reads <c>ctx.DirectionX</c>/<c>DirectionY</c>
    /// (plumbed in by <see cref="SkillsPart.HandleEvent"/> from the
    /// InputHandler's AwaitingDirection capture) and uses
    /// <see cref="LineTargeting.TraceFirstImpact"/> to walk the line.
    /// First creature found within <see cref="LUNGE_RANGE"/> takes a
    /// normal weapon swing via
    /// <see cref="CombatSystem.PerformSingleAttack"/> with the
    /// <c>"(Lunge)"</c> attack-source tag (mirrors Shank's
    /// <c>"(Shank)"</c>). Walls and other solids stop the trace —
    /// creatures behind walls are unhit. Cooldown is applied by
    /// <see cref="SkillsPart.TryRouteSkillCommand"/> after this returns,
    /// regardless of whether a target was struck (a missed lunge still
    /// costs the cooldown — same convention as Slam).</para>
    ///
    /// <para>Per the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §LongBlades_Lunge):
    /// "the only ability that extends ATTACK REACH without moving the
    /// actor." ChargingStrike (Cudgel) moves the actor; Lunge keeps
    /// them planted and lets the blade do the reach.</para>
    ///
    /// <para>Classification: <b>Match (mechanic family) + Divergent
    /// (no stance dependency)</b> per CLAUDE.md §4.2 — Qud's
    /// <c>LongBladesLunge</c> requires being in a duelist's stance;
    /// CoO v1 simplifies to "have a LongBlades weapon equipped" since
    /// CoO doesn't have a stance system yet (LongBlades_EnGarde is
    /// deferred to Tier 2 of the brainstorm). Magnitude
    /// (<see cref="LUNGE_RANGE"/> = 2 cells, 25T cooldown) lifted from
    /// the brainstorm tuning, not Qud's specific values.</para>
    /// </summary>
    public class LongBlades_Lunge : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Lunge);

        public const int COOLDOWN = 25;
        public const int LUNGE_RANGE = 2;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Lunge",
                Command = "CommandLunge",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = LUNGE_RANGE,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng instead of falling back to a
            // wall-clock-seeded one — mirrors Slam's WSP4.4 fix.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require a LongBlades-class weapon equipped (mirrors Slam's
            // Cudgel gate, Shank's Piercing gate). The substring match
            // catches the "Cutting LongBlades" composite attribute that
            // every long-blade-class weapon carries per the WSP-attribute
            // backfill.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "LongBlades");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a long-blade-class weapon to lunge.");
                return;
            }

            // Lunge needs Zone for line-trace + position lookup. Defense-
            // in-depth: tests + scenarios sometimes pass null in failure-
            // path coverage (Lunge_WithNullZone_NoOps_NoCrash).
            if (ctx.Zone == null) return;

            // Direction was set by SkillsPart.HandleEvent from the
            // GameEvent's DirectionX/Y params (set by
            // InputHandler.ResolveAbilityCommand after AwaitingDirection
            // captures the player's keypress). dx=dy=0 means no direction
            // was supplied — bail rather than no-op-iterate
            // LineTargeting (which itself short-circuits zero-direction
            // but we want to log the user-visible reason).
            int dx = ctx.DirectionX;
            int dy = ctx.DirectionY;
            if (dx == 0 && dy == 0)
            {
                MessageLog.Add(actor.GetDisplayName() + " hesitates — no direction chosen.");
                return;
            }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) return;

            // Line-trace LUNGE_RANGE cells in the chosen direction.
            // TraceFirstImpact stops on the first creature, targetable
            // object, or solid cell — exactly the semantics Lunge wants
            // (you can't lunge THROUGH a wall to hit the snapjaw behind
            // it). The trace already ignores the caster, so we don't
            // self-hit if we're somehow in our own line.
            var trace = LineTargeting.TraceFirstImpact(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, LUNGE_RANGE);

            var target = trace.HitEntity;
            if (target == null || !target.Tags.ContainsKey("Creature"))
            {
                // Either the line is clear (no creature within range) or
                // the line was blocked by a wall / non-creature object.
                // The cooldown is still spent (applied by
                // SkillsPart.TryRouteSkillCommand after we return) — a
                // missed lunge is a missed lunge, no refund.
                MessageLog.Add(actor.GetDisplayName() + "'s lunge finds nothing.");
                return;
            }

            // Normal swing. PerformSingleAttack handles to-hit roll,
            // damage roll, on-hit hooks, status applications, dismember
            // checks — everything a regular melee swing does. The
            // "(Lunge)" tag in the message log + future diag records
            // surfaces that this swing came from the active ability
            // rather than a bump-attack or AI auto-attack.
            CombatSystem.PerformSingleAttack(
                attacker: actor, defender: target,
                weapon: weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(Lunge)");
        }
    }
}
