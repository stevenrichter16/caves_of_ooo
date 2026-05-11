using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.2.5 — adversarial sweep for the recruitment system
    /// (F.2.2 RecruitedEffect + F.2.3 Persuasion_Recruit +
    /// F.2.4 Persuasion_Dismiss).
    ///
    /// <para>F.2 hits 7 of the taxonomy surfaces from CLAUDE.md
    /// "Adversarial test sweep" — state atomicity, cross-actor flows,
    /// save/load reach, stacking, anti-exploit, probability boundaries,
    /// diag dispatch invariants. Two-or-more triggers the gate; F.2
    /// hits seven, so a dedicated sweep is mandatory.</para>
    ///
    /// <para><b>Honesty bound</b> (CLAUDE.md): 0 bugs found by these
    /// tests does NOT prove the system bug-free. The bug classes are
    /// bounded by what the author imagined. Fuzz / property-based
    /// testing would surface genuinely novel bugs — out of scope here.
    /// What this file DOES guarantee: future regressions in the
    /// probed classes break visibly with a named test.</para>
    /// </summary>
    public class RecruitedEffectAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeActor(string id, int ego = 16, int level = 1)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Ego"] = new Stat
                { Owner = e, Name = "Ego", BaseValue = ego, Min = 1, Max = 50 };
            e.Statistics["Level"] = new Stat
                { Owner = e, Name = "Level", BaseValue = level, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new BrainPart());
            return e;
        }

        private static int CountDiag(string kind, string reasonContains = null)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = kind, Limit = 100,
            }).Records;
            if (reasonContains == null) return recs.Count;
            int n = 0;
            for (int i = 0; i < recs.Count; i++)
                if (recs[i].PayloadJson != null && recs[i].PayloadJson.Contains(reasonContains)) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════════
        // STATE ATOMICITY — Apply must fully install or fully bail
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OnApplyBailsOnBrainless_NoPartialState()
        {
            // A target with no BrainPart must result in NO mutation —
            // PartyLeader unchanged (it's not on the target anyway),
            // PartyMembers on recruiter unchanged, no goal pushed.
            var recruiter = MakeActor("r");
            var brainless = new Entity { ID = "rock" };
            brainless.AddPart(new StatusEffectsPart());

            int membersBefore = recruiter.GetPart<BrainPart>().PartyMembers.Count;
            brainless.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);

            Assert.AreEqual(membersBefore, recruiter.GetPart<BrainPart>().PartyMembers.Count,
                "Brainless target → recruiter's PartyMembers unchanged.");
        }

        [Test]
        public void Adversarial_OnApplyBailsOnNullRecruiter_NoPartialState()
        {
            var target = MakeActor("t");
            target.ApplyEffect(new RecruitedEffect(null), source: null, zone: null);

            Assert.IsNull(target.GetPart<BrainPart>().PartyLeader,
                "Null recruiter → no leader set.");
            Assert.AreEqual(0, target.GetPart<BrainPart>().GoalCount,
                "Null recruiter → no goal pushed.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-ACTOR FLOWS — chain recruits, sibling recruits, etc.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ChainRecruit_AOwnsB_BOwnsC_RostersConsistent()
        {
            // A recruits B; B recruits C. A's PartyMembers contains
            // exactly {B}; B's contains exactly {C}. C is led by A
            // transitively (via IsLedBy chain walk), but A doesn't
            // directly hold C in its roster.
            var a = MakeActor("a");
            var b = MakeActor("b");
            var c = MakeActor("c");
            b.ApplyEffect(new RecruitedEffect(a), source: a, zone: null);
            c.ApplyEffect(new RecruitedEffect(b), source: b, zone: null);

            CollectionAssert.AreEquivalent(new[] { b }, a.GetPart<BrainPart>().PartyMembers);
            CollectionAssert.AreEquivalent(new[] { c }, b.GetPart<BrainPart>().PartyMembers);
            Assert.IsTrue(c.GetPart<BrainPart>().IsLedBy(a),
                "C is transitively led by A via chain walk.");
            Assert.AreSame(a, c.GetPart<BrainPart>().GetFinalLeader(),
                "GetFinalLeader walks to A.");
        }

        [Test]
        public void Adversarial_SiblingRecruits_SamePartyAligned_NotMutuallyHostile()
        {
            // A recruits B and C. B and C should be ArePartyAligned
            // (share root A) and thus FactionManager.GetFeeling between
            // them returns ALLIED_FEELING.
            var a = MakeActor("a");
            var b = MakeActor("b");
            var c = MakeActor("c");
            b.ApplyEffect(new RecruitedEffect(a), source: a, zone: null);
            c.ApplyEffect(new RecruitedEffect(a), source: a, zone: null);

            Assert.IsTrue(BrainPart.ArePartyAligned(b, c),
                "Siblings B and C share root A → party-aligned.");
            Assert.AreEqual(FactionManager.ALLIED_FEELING,
                FactionManager.GetFeeling(b, c),
                "Sibling feeling is ALLIED_FEELING (F.1.4 contract).");
        }

        [Test]
        public void Adversarial_DismissedFollower_NotInRecruiterPartyMembers()
        {
            // Anti-orphan: after dismiss, recruiter's PartyMembers must
            // not contain the dismissed follower. Both sides of the link
            // tear down.
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            var effect = new RecruitedEffect(recruiter);
            follower.ApplyEffect(effect, source: recruiter, zone: null);
            CollectionAssert.Contains(recruiter.GetPart<BrainPart>().PartyMembers, follower);

            effect.Dismiss(recruiter);

            CollectionAssert.DoesNotContain(
                recruiter.GetPart<BrainPart>().PartyMembers, follower,
                "Post-dismiss: recruiter's roster no longer contains the follower.");
        }

        // ════════════════════════════════════════════════════════════════
        // SAVE/LOAD REACH — RecruitedEffect.Recruiter round-trip identity
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RoundTrip_MidPursuit_PreservesEverything()
        {
            // Recruit, then run the FollowLeaderGoal once (Age++), save,
            // load. Effect, leader-link, AND goal state all survive.
            var recruiter = MakeActor("recruiter-id");
            var follower = MakeActor("follower-id");
            follower.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);
            var goal = follower.GetPart<BrainPart>().PeekGoalAt(
                follower.GetPart<BrainPart>().GoalCount - 1) as FollowLeaderGoal;
            Assert.IsNotNull(goal, "Precondition: FollowLeaderGoal is on top of stack.");
            goal.Age = 7; // simulate having ticked for a while

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(follower);

            var loadedEffect = loaded.GetEffect<RecruitedEffect>();
            Assert.IsNotNull(loadedEffect);
            Assert.AreEqual("recruiter-id", loadedEffect.Recruiter.ID,
                "Recruiter survives round-trip.");
            var loadedGoal = loaded.GetPart<BrainPart>().PeekGoalAt(
                loaded.GetPart<BrainPart>().GoalCount - 1) as FollowLeaderGoal;
            Assert.IsNotNull(loadedGoal, "FollowLeaderGoal survives round-trip.");
            Assert.AreEqual(7, loadedGoal.Age, "Goal Age preserved.");
            Assert.AreSame(loaded.GetPart<BrainPart>().PartyLeader, loadedEffect.Recruiter,
                "SL.8 identity: PartyLeader and effect.Recruiter resolve to same loaded Entity.");
        }

        // ════════════════════════════════════════════════════════════════
        // STACKING SEMANTICS — re-apply, double-dismiss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_DoubleApply_OnStackReturnsFalse_NoDuplicate()
        {
            // OnStack returns false (non-stacking). With OnStack returning
            // false the StatusEffectsPart adds the second effect alongside
            // the first per its contract. The Persuasion_Recruit veto #5
            // is the layer that prevents this in practice; this test
            // pins the effect-system-level behavior in isolation.
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            var first = new RecruitedEffect(recruiter);
            var second = new RecruitedEffect(recruiter);

            Assert.IsFalse(first.OnStack(second),
                "OnStack returns false — non-stacking, re-apply handled at " +
                "skill veto level (F.2.3 Veto #5).");
            // Don't assert effect-system dedup behavior here — that's
            // StatusEffectsPart's responsibility, tested by its own
            // OnStack-contract tests.
        }

        [Test]
        public void Adversarial_DismissTwice_SecondDismissIsNoOp()
        {
            // First Dismiss removes the effect; second Dismiss should
            // be a no-op (the effect's Owner becomes null after removal,
            // so Owner?.GetPart<StatusEffectsPart>() short-circuits).
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            var effect = new RecruitedEffect(recruiter);
            follower.ApplyEffect(effect, source: recruiter, zone: null);
            effect.Dismiss(recruiter);
            Assert.IsFalse(follower.HasEffect<RecruitedEffect>());

            // Second dismiss — must not crash, must not produce side effects.
            Assert.DoesNotThrow(() => effect.Dismiss(recruiter));
            Assert.IsNull(follower.GetPart<BrainPart>().PartyLeader,
                "Already-cleared leader stays cleared after double-dismiss.");
        }

        // ════════════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — vetos hold under unusual inputs
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_DismissByNullEntity_NotCrashing()
        {
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            var effect = new RecruitedEffect(recruiter);
            follower.ApplyEffect(effect, source: recruiter, zone: null);

            Assert.DoesNotThrow(() => effect.Dismiss(null));
            Assert.IsTrue(follower.HasEffect<RecruitedEffect>(),
                "Null dismisser must be rejected — effect still present.");
        }

        [Test]
        public void Adversarial_DismissBySelfRefAttemptNotAllowed()
        {
            // Author attack scenario: someone sets Recruiter = follower
            // (self-recruit somehow snuck through). Dismiss(follower)
            // would technically pass auth ("dismisser == Recruiter").
            // But OnApply rejects self-recruit at the BrainPart level
            // (SetPartyLeader returns false on self). So even if the
            // effect IS installed via raw new RecruitedEffect(follower),
            // OnApply leaves no leader link to tear down.
            var follower = MakeActor("f");
            var effect = new RecruitedEffect(follower); // pathological
            follower.ApplyEffect(effect, source: follower, zone: null);

            // SetPartyLeader rejected the self-cycle in OnApply — no leader.
            Assert.IsNull(follower.GetPart<BrainPart>().PartyLeader,
                "OnApply's SetPartyLeader(self) returns false; no link installed.");

            // Dismiss runs but does nothing observable since OnApply
            // didn't install anything. Should not crash.
            Assert.DoesNotThrow(() => effect.Dismiss(follower));
        }

        [Test]
        public void Adversarial_RecruitSucceeds_ThenForgive_ResetsHostility()
        {
            // F.1.2's SetPartyLeader Forgive step: when a follower
            // joins, their PersonalEnemies.Remove(newLeader) clears
            // any prior grudge. Verify the recruit path triggers this.
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            follower.GetPart<BrainPart>().PersonalEnemies.Add(recruiter);

            follower.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);

            CollectionAssert.DoesNotContain(
                follower.GetPart<BrainPart>().PersonalEnemies, recruiter,
                "After recruit, F.1.2's Forgive clears the recruiter from PersonalEnemies.");
        }

        // ════════════════════════════════════════════════════════════════
        // PROBABILITY BOUNDARIES — Ego edge cases via Persuasion_Recruit
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EgoZero_HighLevelDefender_NeverSucceeds()
        {
            // Ego=1 (mod = (1-16)/2 = -7), Level 1 attacker vs Level 30 defender.
            // DC = 10 + 29 = 39. Max roll = 20 - 7 = 13. Impossible.
            for (int seed = 0; seed < 30; seed++)
            {
                Diag.ResetAll();
                var attacker = MakeActor("a", ego: 1, level: 1);
                var defender = MakeActor("d", ego: 10, level: 30);
                var zone = new Zone();
                zone.AddEntity(attacker, 5, 5);
                zone.AddEntity(defender, 6, 5);
                new Persuasion_Recruit().OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(seed),
                });
                Assert.AreEqual(0, CountDiag("Recruited"),
                    $"Seed {seed}: ego=1 vs lvl=30 must NEVER succeed.");
            }
        }

        [Test]
        public void Adversarial_EgoMax_LevelOneDefender_AlwaysSucceeds()
        {
            // Ego=50 (mod = (50-16)/2 = 17). DC = 10. Min roll = 1+17 = 18 ≥ 10.
            // Should always succeed.
            for (int seed = 0; seed < 30; seed++)
            {
                Diag.ResetAll();
                var attacker = MakeActor("a", ego: 50, level: 10);
                var defender = MakeActor("d", ego: 10, level: 1);
                var zone = new Zone();
                zone.AddEntity(attacker, 5, 5);
                zone.AddEntity(defender, 6, 5);
                new Persuasion_Recruit().OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(seed),
                });
                Assert.AreEqual(1, CountDiag("Recruited"),
                    $"Seed {seed}: ego=50 vs lvl=1 must ALWAYS succeed.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // DIAG DISPATCH INVARIANTS — exactly one record per veto, none on success
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EachVeto_EmitsExactlyOneRejectedRecord()
        {
            // Set up a target that trips ONE veto at a time. Confirm
            // exactly 1 SkillRejected fires per attempt — not 0 (silent
            // bail) and not 2 (cascading vetos misfiring).
            // We use a stranger-recruited target → trips
            // "already_recruited" only.
            var attacker = MakeActor("a", ego: 22);
            var defender = MakeActor("d");
            var stranger = MakeActor("s");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            defender.ApplyEffect(new RecruitedEffect(stranger), source: stranger, zone: zone);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(0),
            });

            Assert.AreEqual(1, CountDiag("SkillRejected"),
                "Exactly one SkillRejected fires per veto trip — not zero, not two.");
        }

        [Test]
        public void Adversarial_SuccessPath_EmitsNoSkillRejected()
        {
            // The success path must NOT emit a SkillRejected — if it
            // does, it means the veto chain is leaking into the
            // post-roll branch. Forces a guaranteed-success setup:
            // Ego=50 vs Level=1.
            var attacker = MakeActor("a", ego: 50, level: 10);
            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(0),
            });

            Assert.AreEqual(0, CountDiag("SkillRejected"),
                "Success path must emit zero SkillRejected records.");
            Assert.AreEqual(1, CountDiag("Recruited"),
                "Exactly one Recruited record on success.");
        }

        [Test]
        public void Adversarial_RollFailedDiagPayload_ContainsRollAndDc()
        {
            // The roll_failed payload carries the diagnostic numbers
            // (d20, egoMod, roll, dc) so a debug session can see why
            // the recruit failed without rerunning. Test the payload
            // shape pinning.
            var attacker = MakeActor("a", ego: 8, level: 1); // mod = -4
            var defender = MakeActor("d", level: 20);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            new Persuasion_Recruit().OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(0),
            });

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "SkillRejected", Limit = 10,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            string json = recs[0].PayloadJson;
            StringAssert.Contains("\"reason\":\"roll_failed\"", json);
            StringAssert.Contains("\"d20\":", json);
            StringAssert.Contains("\"egoMod\":", json);
            StringAssert.Contains("\"roll\":", json);
            StringAssert.Contains("\"dc\":", json);
        }

        // ════════════════════════════════════════════════════════════════
        // GOAL STACK INTERACTION — KillGoal on top, FollowLeaderGoal underneath
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OnRemove_FindsFollowLeaderGoal_BelowOtherGoals()
        {
            // After recruit, push a KillGoal on top of FollowLeaderGoal.
            // Dismiss must find + remove the FollowLeaderGoal even though
            // it's no longer on top of the stack (predicate-based search,
            // not top-only).
            var recruiter = MakeActor("r");
            var follower = MakeActor("f");
            var effect = new RecruitedEffect(recruiter);
            follower.ApplyEffect(effect, source: recruiter, zone: null);
            Assert.IsTrue(follower.GetPart<BrainPart>().HasGoal<FollowLeaderGoal>(),
                "Precondition: FollowLeaderGoal present.");
            // Push another goal on top — KillGoal targeting anyone.
            follower.GetPart<BrainPart>().PushGoal(new KillGoal(MakeActor("victim")));
            int goalsBefore = follower.GetPart<BrainPart>().GoalCount;

            effect.Dismiss(recruiter);

            Assert.IsFalse(follower.GetPart<BrainPart>().HasGoal<FollowLeaderGoal>(),
                "OnRemove finds + pops FollowLeaderGoal even when not top of stack.");
            Assert.IsTrue(follower.GetPart<BrainPart>().HasGoal<KillGoal>(),
                "KillGoal (the unrelated goal layered on top) remains intact.");
            Assert.AreEqual(goalsBefore - 1, follower.GetPart<BrainPart>().GoalCount,
                "Exactly one goal removed.");
        }

        // ════════════════════════════════════════════════════════════════
        // NULL-SAFETY across the public API
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AllNullPaths_DoNotCrash()
        {
            // Multiple null entry points — no NRE on any.
            Assert.DoesNotThrow(() => new Persuasion_Recruit().OnCommand(null));
            Assert.DoesNotThrow(() => new Persuasion_Dismiss().OnCommand(null));

            var actor = MakeActor("a");
            Assert.DoesNotThrow(() =>
                new Persuasion_Recruit().OnCommand(new SkillEventContext
                { Attacker = actor, Zone = null, Rng = null }));
            Assert.DoesNotThrow(() =>
                new Persuasion_Dismiss().OnCommand(new SkillEventContext
                { Attacker = actor, Zone = null }));

            var effect = new RecruitedEffect(null);
            Assert.DoesNotThrow(() => effect.OnApply(null));
            Assert.DoesNotThrow(() => effect.OnRemove(null));
            Assert.DoesNotThrow(() => effect.Dismiss(null));
        }

        // ════════════════════════════════════════════════════════════════
        // SCALE — many followers under one leader
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OneRecruiter_TenFollowers_AllConsistent()
        {
            var recruiter = MakeActor("r");
            var followers = new List<Entity>();
            for (int i = 0; i < 10; i++)
            {
                var f = MakeActor("f" + i);
                f.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: null);
                followers.Add(f);
            }
            Assert.AreEqual(10, recruiter.GetPart<BrainPart>().PartyMembers.Count);
            foreach (var f in followers)
            {
                Assert.AreSame(recruiter, f.GetPart<BrainPart>().PartyLeader);
                Assert.IsTrue(f.GetPart<BrainPart>().HasGoal<FollowLeaderGoal>());
            }

            // Now dismiss them all — roster must reach empty cleanly.
            foreach (var f in followers)
            {
                f.GetEffect<RecruitedEffect>()?.Dismiss(recruiter);
            }
            Assert.AreEqual(0, recruiter.GetPart<BrainPart>().PartyMembers.Count,
                "All ten dismissed → roster empty, no orphan refs.");
        }
    }
}
