namespace CavesOfOoo.Core
{
    /// <summary>
    /// Followers F.2.2 — the recruitment effect installed by
    /// <c>Persuasion_Recruit</c> (F.2.3) on a target NPC. While owned,
    /// the target follows <see cref="Recruiter"/> via
    /// <see cref="BrainPart.PartyLeader"/> (the F.1.2 substrate) and a
    /// <see cref="FollowLeaderGoal"/> on its goal stack. Removal (via
    /// <see cref="Dismiss"/> or external <c>RemoveEffect</c>) reverses
    /// both — clears the leader and pops the goal.
    ///
    /// <para><b>Qud parity:</b> mirrors <c>Proselytized</c>
    /// (<c>/Users/steven/qud-decompiled-project/XRL.World.Effects/Proselytized.cs</c>).
    /// CoO simplifications versus Qud: no opinion-map, no sound
    /// playback, no AllyProselytize typed-allegiance (deferred to F.5+).</para>
    ///
    /// <para><b>OnApply contract:</b> calls
    /// <c>BrainPart.SetPartyLeader(Recruiter)</c> — F.1.2's primitive
    /// handles the bidirectional mirror, cycle check, and Forgive step.
    /// Then pushes a <see cref="FollowLeaderGoal"/> with
    /// <c>Leader = Recruiter</c> so the AI moves the recruit toward the
    /// recruiter each turn.</para>
    ///
    /// <para><b>OnRemove contract:</b> pops the matching
    /// FollowLeaderGoal (located by walking the stack and matching
    /// <c>Leader == Recruiter</c>, rather than by saved reference, so the
    /// behavior survives save/load and goal-stack reordering). Clears
    /// <see cref="BrainPart.PartyLeader"/> IFF the recruiter is still
    /// the current leader (defends against the "someone else took over"
    /// path).</para>
    ///
    /// <para><b>Dismiss contract:</b> public dispatch point for any UI
    /// surface (activated ability in F.2.4, future right-click menu,
    /// future conversation action). Authorization gate: only the
    /// original recruiter can dismiss this specific effect. Triggers
    /// <c>Owner.RemoveEffect(this)</c> which fires OnRemove.</para>
    /// </summary>
    public class RecruitedEffect : Effect
    {
        public override string DisplayName => "recruited";

        /// <summary>The entity that recruited this follower. Saved by
        /// reflection via WriteEntityReference (SL.8 token system) — if
        /// the recruiter is in the save graph, post-load
        /// <c>Recruiter</c> resolves to the SAME loaded instance as any
        /// other ref to that entity (e.g. <see cref="BrainPart.PartyLeader"/>).</summary>
        public Entity Recruiter;

        public RecruitedEffect() { Duration = DURATION_INDEFINITE; }

        public RecruitedEffect(Entity recruiter) : this()
        {
            Recruiter = recruiter;
        }

        public override void OnApply(Entity target)
        {
            if (Recruiter == null || target == null) return;
            var brain = target.GetPart<BrainPart>();
            if (brain == null) return;

            // F.1.2's SetPartyLeader handles bidirectional mirror,
            // cycle detection, and Forgive (PersonalEnemies.Remove).
            // Re-use; don't duplicate. If SetPartyLeader rejects (self-ref,
            // cycle), DON'T push the goal — the goal would target an entity
            // that isn't actually the leader, leaving the follower visibly
            // pursuing a non-leader. The Persuasion_Recruit veto chain
            // catches most rejections upstream (Veto #3 self-target,
            // Veto #6 follows-another); this is defense-in-depth for any
            // future caller that bypasses the skill (other recruitment
            // paths, direct ApplyEffect from mods, etc.).
            // [Surfaced by post-F.2.7 audit, Finding #2.]
            if (!brain.SetPartyLeader(Recruiter)) return;

            // Push the goal that drives moment-to-moment follow behavior.
            // F.1.5's FollowLeaderGoal handles termination on
            // null/destroyed/cross-zone leader (with F.2.6 persistent-
            // follow semantics: idles when close, doesn't pop).
            brain.PushGoal(new FollowLeaderGoal { Leader = Recruiter });

            MessageLog.Add(target.GetDisplayName() + " joins " + Recruiter.GetDisplayName() + "!");
        }

        public override void OnRemove(Entity target)
        {
            if (target == null) return;
            var brain = target.GetPart<BrainPart>();
            if (brain == null) return;

            // Find and pop the FollowLeaderGoal we pushed. Search by
            // predicate rather than by saved reference because (a) the
            // goal stack may have layered other goals on top since
            // OnApply, and (b) after save/load a saved goal reference
            // would be stale (CanSerializeType allows Entity refs but
            // not arbitrary GoalHandler refs — see SaveSystem.cs:1626).
            for (int i = 0; i < brain.GoalCount; i++)
            {
                var g = brain.PeekGoalAt(i);
                if (g is FollowLeaderGoal flg && flg.Leader == Recruiter)
                {
                    brain.RemoveGoal(g);
                    break;
                }
            }

            // Only clear leader if the recruiter is still in charge.
            // Defends against the sequence: A recruits B, then C takes
            // over B (SetPartyLeader(C)), then A's effect is removed —
            // B should still follow C, not become leaderless.
            if (brain.PartyLeader == Recruiter)
                brain.SetPartyLeader(null);
        }

        /// <summary>
        /// Public dispatch point for all dismiss surfaces (activated
        /// ability, right-click menu, conversation action). Authorization:
        /// only the original recruiter can dismiss this specific effect.
        /// On authorized call, removes the effect (which fires OnRemove
        /// and tears down the leader link + follow goal).
        /// </summary>
        public void Dismiss(Entity dismisser)
        {
            if (dismisser == null || dismisser != Recruiter) return;
            // Route through StatusEffectsPart — Entity.RemoveEffect has
            // (Type), (Predicate<Effect>), and generic <T> overloads but
            // no direct (Effect) overload; StatusEffectsPart has the
            // direct one (StatusEffectsPart.cs:205).
            Owner?.GetPart<StatusEffectsPart>()?.RemoveEffect(this);
        }

        /// <summary>
        /// Non-stacking. Re-apply is normally vetoed at the skill level
        /// (F.2.3 Veto #5: "already_recruited"); if a re-apply slips
        /// through, returning false here means the new effect is added
        /// alongside the existing one, which the skill layer treats as
        /// an invariant violation rather than a duplicate-handle case.
        /// </summary>
        public override bool OnStack(Effect incoming) => false;
    }
}
