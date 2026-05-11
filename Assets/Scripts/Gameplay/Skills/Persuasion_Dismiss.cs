using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Followers F.2.4 — Persuasion_Dismiss active ability. Symmetric
    /// inverse of <see cref="Persuasion_Recruit"/>. Targets an adjacent
    /// follower that the actor recruited and releases them.
    ///
    /// <para><b>Mechanic:</b> activated ability, NO cooldown, adjacent-
    /// cell targeting. The actor picks the first adjacent Creature in
    /// 8-dir order. The target must (a) have a <see cref="RecruitedEffect"/>
    /// AND (b) that effect's <c>Recruiter</c> must be the actor (you
    /// can only dismiss YOUR followers — Veto #2 covers stranger-side
    /// dismiss attempts). On success, calls
    /// <see cref="RecruitedEffect.Dismiss"/>, which routes through F.2.2's
    /// effect-as-dispatcher pattern: removes the effect, which fires
    /// OnRemove, which pops the FollowLeaderGoal and clears
    /// <see cref="BrainPart.PartyLeader"/>.</para>
    ///
    /// <para><b>Qud parity:</b> Qud's dismiss is an inventory action
    /// on the <c>Proselytized</c> effect itself (line 67 of
    /// Proselytized.cs adds it via <c>GetInventoryActionsEvent</c>).
    /// CoO lacks an inventory-actions-on-NPCs surface, so this skill
    /// is the v1 trigger surface. When the inventory-actions surface
    /// lands in F.5+, it can call the same
    /// <see cref="RecruitedEffect.Dismiss"/> method with no re-wiring.
    /// Qud's dismiss is <c>WorksAtDistance: true</c>; F.2 v1 keeps it
    /// adjacent for symmetry with Recruit.</para>
    /// </summary>
    public class Persuasion_Dismiss : BaseSkillPart
    {
        public override string Name => nameof(Persuasion_Dismiss);
        public override string DisplayName => "Dismiss";

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = DisplayName,
                Command = "CommandDismiss",
                Class = "Persuasion",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = 0, // No cooldown — dismiss is a free action
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "null_context");
                return;
            }
            var actor = ctx.Attacker;

            var target = SkillCombatHelpers.FindAdjacentCleaveTarget(actor, actor, ctx.Zone);
            if (target == null)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Target must have a RecruitedEffect installed by THIS actor.
            // (Generic "no_recruited_effect" covers both no-effect and
            // wrong-recruiter cases at the user-visible level; the diag
            // payload would disambiguate if we wanted to surface them
            // separately — F.2.5 may revisit.)
            var effect = target.GetEffect<RecruitedEffect>();
            if (effect == null)
            {
                EmitSkillRejectedDiag(ctx, "no_recruited_effect");
                return;
            }
            if (effect.Recruiter != actor)
            {
                EmitSkillRejectedDiag(ctx, "not_your_follower");
                return;
            }

            // Authorized dismiss — delegate to the effect's dispatcher,
            // which removes itself (triggers OnRemove → goal pop + clear
            // PartyLeader if still us).
            effect.Dismiss(actor);

            if (Diag.IsChannelEnabled("skill"))
            {
                Diag.Record(
                    category: "skill",
                    kind: "Dismissed",
                    actor: actor,
                    target: target,
                    payload: new
                    {
                        skillClass = nameof(Persuasion_Dismiss),
                    });
            }

            MessageLog.Add(target.GetDisplayName() + " is dismissed from " + actor.GetDisplayName() + "'s service.");
        }
    }
}
