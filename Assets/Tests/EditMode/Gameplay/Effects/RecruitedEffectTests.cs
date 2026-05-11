using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.2.2 — RecruitedEffect contract: produces the same
    /// shape as Qud's <c>Proselytized</c> effect
    /// (<c>/Users/steven/qud-decompiled-project/XRL.World.Effects/Proselytized.cs</c>).
    ///
    /// <para><b>What's pinned here</b> — the contracts F.2.3 will rely on:</para>
    /// <list type="bullet">
    ///   <item><c>OnApply</c>: <c>BrainPart.SetPartyLeader(Recruiter)</c>
    ///         + push <c>FollowLeaderGoal { Leader = Recruiter }</c>.</item>
    ///   <item><c>OnRemove</c>: pop the matching FollowLeaderGoal and clear
    ///         <c>PartyLeader</c> IFF the recruiter is still the current leader.</item>
    ///   <item><c>Dismiss(dismisser)</c>: public dispatch point — authorization
    ///         requires <c>dismisser == Recruiter</c>; only then removes the
    ///         effect (which fires OnRemove).</item>
    ///   <item><c>OnStack</c>: returns false — re-apply is denied at the skill-veto
    ///         level (F.2.3 Veto #5), so the effect is non-stacking at the
    ///         effect-system level too.</item>
    ///   <item>Save/load round-trip preserves <c>Recruiter</c> identity per
    ///         the SL.8 contract — if recruiter is in the save graph, after
    ///         round-trip <c>loaded(effect).Recruiter</c> is the same
    ///         instance as the loaded recruiter (AreSame, not AreEqual).</item>
    /// </list>
    /// </summary>
    public class RecruitedEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity ActorWithBrain(string id)
        {
            var actor = new Entity { ID = id, BlueprintName = "Test" };
            actor.AddPart(new BrainPart());
            actor.AddPart(new StatusEffectsPart());
            return actor;
        }

        private static BrainPart Brain(Entity actor) => actor.GetPart<BrainPart>();

        // ── OnApply ──────────────────────────────────────────────

        [Test]
        public void OnApply_SetsPartyLeader_ToRecruiter()
        {
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");

            recruit.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);

            Assert.AreSame(recruiter, Brain(recruit).PartyLeader,
                "After RecruitedEffect applies, the recruit's PartyLeader is the recruiter.");
            CollectionAssert.Contains(Brain(recruiter).PartyMembers, recruit,
                "Bidirectional mirror — recruiter's PartyMembers contains the recruit.");
        }

        [Test]
        public void OnApply_PushesFollowLeaderGoal_OntoBrain()
        {
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");

            int goalsBefore = Brain(recruit).GoalCount;
            recruit.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);

            Assert.IsTrue(Brain(recruit).HasGoal<FollowLeaderGoal>(),
                "OnApply pushes a FollowLeaderGoal onto the recruit's brain.");
            Assert.AreEqual(goalsBefore + 1, Brain(recruit).GoalCount,
                "Exactly one goal added (not zero, not two).");

            // The pushed goal targets the recruiter, not some random entity.
            FollowLeaderGoal pushed = FindFollowLeaderGoal(Brain(recruit));
            Assert.IsNotNull(pushed, "FollowLeaderGoal is discoverable on the stack.");
            Assert.AreSame(recruiter, pushed.Leader,
                "The pushed goal's Leader field is the recruiter.");
        }

        [Test]
        public void OnApply_NullRecruiter_DoesNotInstallLink()
        {
            // Defensive: an effect with no Recruiter shouldn't crash AND
            // shouldn't install a bogus link. F.2.3's Veto #1 will normally
            // catch this before it gets here, but defense in depth matters.
            var recruit = ActorWithBrain("r");

            recruit.ApplyEffect(new RecruitedEffect(null), source: null, zone: null);

            Assert.IsNull(Brain(recruit).PartyLeader,
                "Null recruiter leaves PartyLeader unset.");
            Assert.IsFalse(Brain(recruit).HasGoal<FollowLeaderGoal>(),
                "Null recruiter does NOT push a FollowLeaderGoal.");
        }

        [Test]
        public void OnApply_SetPartyLeaderRejects_DoesNotPushGoal()
        {
            // Post-F.2.7 audit Finding #2 (🔴): if SetPartyLeader rejects
            // (cycle, self-ref), OnApply must NOT push the FollowLeaderGoal.
            // Pre-fix behavior pushed the goal anyway, leaving the follower
            // visibly pursuing an entity who wasn't actually their leader.
            //
            // Trigger: self-recruit. SetPartyLeader rejects newLeader == self.
            // The Persuasion_Recruit veto chain catches this at Veto #3
            // upstream, but the effect must be independently safe for any
            // future caller that bypasses the skill (mods, tests, etc.).
            var actor = ActorWithBrain("a");

            actor.ApplyEffect(new RecruitedEffect(actor), source: actor, zone: null);

            Assert.IsNull(Brain(actor).PartyLeader,
                "Self-recruit: SetPartyLeader rejected — leader stays null.");
            Assert.IsFalse(Brain(actor).HasGoal<FollowLeaderGoal>(),
                "OnApply must NOT push FollowLeaderGoal when SetPartyLeader " +
                "rejects. Regression test for post-F.2.7 audit Finding #2.");
        }

        [Test]
        public void OnApply_TargetWithoutBrain_NoOp()
        {
            // A target with no BrainPart can't be a follower. The effect
            // should bail in OnApply without crashing. F.2.3's Veto #4
            // catches this at the skill level, but the effect must be
            // independently safe.
            var brainless = new Entity { ID = "b" };
            brainless.AddPart(new StatusEffectsPart());
            var recruiter = ActorWithBrain("R");

            // Should not throw.
            Assert.DoesNotThrow(() =>
                brainless.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null));
        }

        // ── OnRemove ─────────────────────────────────────────────

        [Test]
        public void OnRemove_ClearsPartyLeader_IfRecruiterStillLeader()
        {
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var effect = new RecruitedEffect(recruiter);

            recruit.ApplyEffect(effect, source: recruiter, zone: null);
            Assert.AreSame(recruiter, Brain(recruit).PartyLeader);

            recruit.GetPart<StatusEffectsPart>().RemoveEffect(effect);

            Assert.IsNull(Brain(recruit).PartyLeader,
                "OnRemove clears PartyLeader.");
            CollectionAssert.DoesNotContain(Brain(recruiter).PartyMembers, recruit,
                "Bidirectional unmirror — recruiter no longer claims recruit as member.");
        }

        [Test]
        public void OnRemove_DoesNotClearLeader_IfReRecruitedByOther()
        {
            // Counter-check: if Alice recruits Bob, then Carol takes over
            // (Bob.PartyLeader becomes Carol), then Alice's RecruitedEffect
            // is removed — Bob should still follow Carol, not become leaderless.
            // This pins the "only clear leader if recruiter is still leader"
            // half of the OnRemove contract.
            var bob = ActorWithBrain("bob");
            var alice = ActorWithBrain("alice");
            var carol = ActorWithBrain("carol");
            var aliceEffect = new RecruitedEffect(alice);

            bob.ApplyEffect(aliceEffect, source: alice, zone: null);
            // Carol "takes over" — bypasses recruit skill, just calls the primitive.
            Brain(bob).SetPartyLeader(carol);
            Assert.AreSame(carol, Brain(bob).PartyLeader);

            // Now remove Alice's effect.
            bob.GetPart<StatusEffectsPart>().RemoveEffect(aliceEffect);

            Assert.AreSame(carol, Brain(bob).PartyLeader,
                "OnRemove must not clear leader when someone else has taken over.");
        }

        [Test]
        public void OnRemove_PopsFollowLeaderGoal_FromStack()
        {
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var effect = new RecruitedEffect(recruiter);

            recruit.ApplyEffect(effect, source: recruiter, zone: null);
            Assert.IsTrue(Brain(recruit).HasGoal<FollowLeaderGoal>());

            recruit.GetPart<StatusEffectsPart>().RemoveEffect(effect);

            Assert.IsFalse(Brain(recruit).HasGoal<FollowLeaderGoal>(),
                "OnRemove pops the FollowLeaderGoal that OnApply pushed.");
        }

        // ── Dismiss ──────────────────────────────────────────────

        [Test]
        public void Dismiss_ByRecruiter_RemovesEffect()
        {
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var effect = new RecruitedEffect(recruiter);
            recruit.ApplyEffect(effect, source: recruiter, zone: null);

            effect.Dismiss(recruiter);

            Assert.IsFalse(recruit.HasEffect<RecruitedEffect>(),
                "Dismiss by the recruiter removes the effect from the recruit.");
            Assert.IsNull(Brain(recruit).PartyLeader,
                "Dismiss triggers OnRemove which clears PartyLeader.");
        }

        [Test]
        public void Dismiss_ByNonRecruiter_NoOp()
        {
            // Counter-check (authorization): only the recruiter can dismiss
            // their own recruit. A stranger pressing Dismiss does nothing.
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var stranger = ActorWithBrain("s");
            var effect = new RecruitedEffect(recruiter);
            recruit.ApplyEffect(effect, source: recruiter, zone: null);

            effect.Dismiss(stranger);

            Assert.IsTrue(recruit.HasEffect<RecruitedEffect>(),
                "Dismiss by a non-recruiter must NOT remove the effect.");
            Assert.AreSame(recruiter, Brain(recruit).PartyLeader,
                "Leadership chain remains intact when dismiss is unauthorized.");
        }

        [Test]
        public void Dismiss_ByNull_NoOp()
        {
            // Null-safety.
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var effect = new RecruitedEffect(recruiter);
            recruit.ApplyEffect(effect, source: recruiter, zone: null);

            Assert.DoesNotThrow(() => effect.Dismiss(null));
            Assert.IsTrue(recruit.HasEffect<RecruitedEffect>(),
                "Null dismisser must be treated as unauthorized — effect remains.");
        }

        // ── OnStack ──────────────────────────────────────────────

        [Test]
        public void OnStack_ReturnsFalse_NonStacking()
        {
            // The effect is non-stacking at the effect-system level.
            // Re-apply is normally denied at the skill-veto layer (F.2.3
            // Veto #5), but if it slips through, the underlying contract
            // is: don't stack, don't extend duration.
            var recruit = ActorWithBrain("r");
            var recruiter = ActorWithBrain("R");
            var first = new RecruitedEffect(recruiter);
            recruit.ApplyEffect(first, source: recruiter, zone: null);

            // Attempt re-apply.
            var second = new RecruitedEffect(recruiter);
            recruit.ApplyEffect(second, source: recruiter, zone: null);

            // StatusEffectsPart will keep the first effect; OnStack returning
            // false means the second IS added as a duplicate. Pin that
            // OnStack itself returns false (the design choice — re-apply
            // handled at skill level), not the dedup behavior.
            Assert.IsFalse(first.OnStack(second),
                "RecruitedEffect.OnStack returns false — re-apply handled at skill veto level.");
        }

        // ── Save/load round-trip ─────────────────────────────────

        [Test]
        public void RoundTrip_PreservesRecruiterIdentity_SL8Contract()
        {
            // SL.8 token identity: if Recruiter is referenced both as
            // BrainPart.PartyLeader AND as RecruitedEffect.Recruiter, after
            // a single round-trip, both loaded references resolve to the
            // SAME Entity instance.
            var recruit = ActorWithBrain("recruit-id");
            var recruiter = ActorWithBrain("recruiter-id");
            recruit.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);

            Entity loaded = PartRoundTripHelper.RoundTripEntityWithBodies(recruit);

            var loadedEffect = loaded.GetEffect<RecruitedEffect>();
            Assert.IsNotNull(loadedEffect, "RecruitedEffect survives round-trip.");
            Assert.IsNotNull(loadedEffect.Recruiter, "Recruiter ref is not null after load.");
            Assert.AreEqual("recruiter-id", loadedEffect.Recruiter.ID,
                "Recruiter's ID is preserved.");
            Assert.AreSame(loaded.GetPart<BrainPart>().PartyLeader, loadedEffect.Recruiter,
                "SL.8 token identity: PartyLeader and Effect.Recruiter resolve to the " +
                "same loaded Entity instance (single graph load shares one token table).");
        }

        // ── Helpers ──────────────────────────────────────────────

        private static FollowLeaderGoal FindFollowLeaderGoal(BrainPart brain)
        {
            for (int i = 0; i < brain.GoalCount; i++)
            {
                if (brain.PeekGoalAt(i) is FollowLeaderGoal flg) return flg;
            }
            return null;
        }
    }
}
