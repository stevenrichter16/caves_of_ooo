using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.2.4 — Persuasion_Dismiss skill contract. Symmetric
    /// inverse of F.2.3's Persuasion_Recruit; pins the 4-veto chain
    /// (null context, no adjacent target, target has no
    /// RecruitedEffect, target's RecruitedEffect was installed by
    /// someone else) and the Dismissed diag emission.
    ///
    /// <para><b>Qud parity note:</b> Qud's dismiss is an inventory
    /// action on the Proselytized effect itself (Proselytized.cs:67).
    /// CoO ships this as an activated ability since the inventory-
    /// actions-on-NPCs surface doesn't yet exist; the underlying
    /// dispatcher
    /// (<see cref="RecruitedEffect.Dismiss"/>) is the same and can be
    /// called from future UI surfaces (F.5+) without re-wiring.</para>
    /// </summary>
    public class PersuasionDismissTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        // ── Fixture ──────────────────────────────────────────────

        private static Entity MakeActor(string id)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new BrainPart());
            return e;
        }

        private static (Entity recruiter, Entity follower, Zone zone, Persuasion_Dismiss dismiss)
            MakeRecruitedFixture()
        {
            var recruiter = MakeActor("recruiter");
            var follower = MakeActor("follower");
            var zone = new Zone();
            zone.AddEntity(recruiter, 5, 5);
            zone.AddEntity(follower, 6, 5);
            follower.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: zone);
            Diag.ResetAll(); // ignore the apply-time records
            return (recruiter, follower, zone, new Persuasion_Dismiss());
        }

        private static SkillEventContext Ctx(Entity actor, Zone zone)
            => new SkillEventContext { Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0) };

        private static int CountDiag(string kind, string reasonContains = null)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = kind,
                Limit = 50,
            }).Records;
            if (reasonContains == null) return recs.Count;
            int n = 0;
            for (int i = 0; i < recs.Count; i++)
                if (recs[i].PayloadJson != null && recs[i].PayloadJson.Contains(reasonContains)) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismiss_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var spec = new Persuasion_Dismiss().DeclareActivatedAbility(null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandDismiss", spec.Command);
            Assert.AreEqual(0, spec.Cooldown, "Dismiss has no cooldown.");
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
            Assert.AreEqual("Dismiss", spec.DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // Vetos
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Veto_NullContext_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new Persuasion_Dismiss().OnCommand(null));
        }

        [Test]
        public void Veto_NullZone_EmitsRejected_NullContext()
        {
            var actor = MakeActor("a");
            new Persuasion_Dismiss().OnCommand(
                new SkillEventContext { Attacker = actor, Zone = null });
            Assert.AreEqual(1, CountDiag("SkillRejected", "null_context"));
        }

        [Test]
        public void Veto_NoAdjacentTarget_EmitsRejected_NoTarget()
        {
            var actor = MakeActor("a");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);

            new Persuasion_Dismiss().OnCommand(Ctx(actor, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "no_target"));
        }

        [Test]
        public void Veto_TargetHasNoRecruitedEffect_EmitsRejected_NoRecruitedEffect()
        {
            var actor = MakeActor("a");
            var stranger = MakeActor("s");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(stranger, 6, 5);

            new Persuasion_Dismiss().OnCommand(Ctx(actor, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "no_recruited_effect"));
        }

        [Test]
        public void Veto_TargetFollowsSomeoneElse_EmitsRejected_NotYourFollower()
        {
            // Bob recruits Carol. Alice tries to dismiss Carol. Should fail
            // with not_your_follower, leaving Carol's link intact.
            var alice = MakeActor("alice");
            var bob = MakeActor("bob");
            var carol = MakeActor("carol");
            var zone = new Zone();
            zone.AddEntity(alice, 5, 5);
            zone.AddEntity(carol, 6, 5);
            zone.AddEntity(bob, 8, 5); // out of the picture
            carol.ApplyEffect(new RecruitedEffect(bob), source: bob, zone: zone);
            Diag.ResetAll();

            new Persuasion_Dismiss().OnCommand(Ctx(alice, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "not_your_follower"));
            Assert.IsTrue(carol.HasEffect<RecruitedEffect>(),
                "Unauthorized dismiss must not remove Bob's effect on Carol.");
            Assert.AreSame(bob, carol.GetPart<BrainPart>().PartyLeader,
                "Unauthorized dismiss must not break Carol's leader link to Bob.");
        }

        // ════════════════════════════════════════════════════════════════
        // Success path
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DismissYourFollower_RemovesEffect_AndClearsLeader()
        {
            var (recruiter, follower, zone, dismiss) = MakeRecruitedFixture();
            Assert.IsTrue(follower.HasEffect<RecruitedEffect>(),
                "Precondition: follower has RecruitedEffect.");
            Assert.AreSame(recruiter, follower.GetPart<BrainPart>().PartyLeader,
                "Precondition: PartyLeader is the recruiter.");

            dismiss.OnCommand(Ctx(recruiter, zone));

            Assert.IsFalse(follower.HasEffect<RecruitedEffect>(),
                "After dismiss, RecruitedEffect is gone.");
            Assert.IsNull(follower.GetPart<BrainPart>().PartyLeader,
                "After dismiss, PartyLeader is cleared.");
            CollectionAssert.DoesNotContain(
                recruiter.GetPart<BrainPart>().PartyMembers, follower,
                "Bidirectional unmirror: recruiter no longer claims follower.");
            Assert.AreEqual(1, CountDiag("Dismissed"));
        }

        [Test]
        public void DismissThenReRecruit_Succeeds_ContractIntactPostCycle()
        {
            // Counter-check (anti-exploit / re-entry): a recruit who has
            // been dismissed is a clean recruit-target again — no residual
            // state, no second-dismiss issues.
            var (recruiter, follower, zone, dismiss) = MakeRecruitedFixture();
            dismiss.OnCommand(Ctx(recruiter, zone));
            Assert.IsNull(follower.GetPart<BrainPart>().PartyLeader);

            // Re-recruit via the effect-apply path (sidesteps the
            // skill's roll for cleanliness — we're testing dismiss
            // doesn't leave residue, not the recruit skill).
            follower.ApplyEffect(new RecruitedEffect(recruiter), source: recruiter, zone: zone);

            Assert.AreSame(recruiter, follower.GetPart<BrainPart>().PartyLeader,
                "Re-recruit installs the leader link cleanly post-dismiss.");
            Assert.IsTrue(follower.HasEffect<RecruitedEffect>(),
                "Re-recruit installs a fresh RecruitedEffect.");
        }

        // ════════════════════════════════════════════════════════════════
        // FollowLeaderGoal removal (verifies the full F.2.2 contract
        // through the dismiss-skill path, not via Effect.Dismiss directly)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void DismissYourFollower_PopsFollowLeaderGoal()
        {
            var (recruiter, follower, zone, dismiss) = MakeRecruitedFixture();
            Assert.IsTrue(follower.GetPart<BrainPart>().HasGoal<FollowLeaderGoal>(),
                "Precondition: FollowLeaderGoal is on the stack from RecruitedEffect.OnApply.");

            dismiss.OnCommand(Ctx(recruiter, zone));

            Assert.IsFalse(follower.GetPart<BrainPart>().HasGoal<FollowLeaderGoal>(),
                "Dismiss tears down the goal via RecruitedEffect.OnRemove.");
        }
    }
}
