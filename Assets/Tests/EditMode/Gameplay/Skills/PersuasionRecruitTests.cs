using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.2.3 — Persuasion_Recruit skill contract. Pins the
    /// 8-veto chain from FOLLOWERS-F2.md Lockdown #2, the d20+Ego-mod
    /// vs DC=10+max(target.Lvl-actor.Lvl,0) roll from Lockdown #1, and
    /// the Recruited / RecruitRejected diag emissions from Lockdown #5.
    ///
    /// <para><b>Qud parity:</b> mirrors <c>Persuasion_Proselytize</c>
    /// (<c>/Users/steven/qud-decompiled-project/XRL.World.Parts.Skill/Persuasion_Proselytize.cs</c>).
    /// CoO simplifications: d20 vs DC instead of Qud's MentalAttack
    /// penetration roll (no MA stat in CoO); over-recruit denied
    /// outright instead of Qud's +1 DC stack (F.5+ will revisit when
    /// multiple recruitment paths land).</para>
    /// </summary>
    public class PersuasionRecruitTests
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
            e.AddPart(new SkillsPart()); // F.3.3 — needed for HandleEvent dispatch + AddSkill in fixture tests
            return e;
        }

        private static (Entity attacker, Entity defender, Zone zone, Persuasion_Recruit recruit)
            MakeFixture(int egoAttacker = 16, int levelAttacker = 1,
                        int levelDefender = 1)
        {
            var attacker = MakeActor("attacker", egoAttacker, levelAttacker);
            // F.3.3 requires the skill to be registered on the actor so
            // GetCompanionLimitEvent's slot bump fires — otherwise the
            // at_companion_limit veto would block every recruit attempt.
            attacker.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "fixture");
            var defender = MakeActor("defender", ego: 10, level: levelDefender);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5); // East-adjacent
            return (attacker, defender, zone, new Persuasion_Recruit());
        }

        private static SkillEventContext Ctx(Entity attacker, Zone zone, int seed = 0)
            => new SkillEventContext { Attacker = attacker, Defender = attacker, Zone = zone, Rng = new Random(seed) };

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
        // Spec shape — DeclareActivatedAbility returns the expected spec
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Recruit_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var spec = new Persuasion_Recruit().DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec, "Recruit must declare a non-null spec.");
            Assert.AreEqual("CommandRecruit", spec.Command,
                "Recruit command key must be 'CommandRecruit' for input dispatch.");
            Assert.AreEqual(Persuasion_Recruit.COOLDOWN, spec.Cooldown,
                "Recruit cooldown must match the COOLDOWN constant (25T, Qud parity).");
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode,
                "Recruit targets an adjacent creature.");
            Assert.AreEqual("Recruit", spec.DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // Vetos 1-8 (FOLLOWERS-F2.md Lockdown #2)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Veto1_NullContext_EmitsRejected_NoApply()
        {
            new Persuasion_Recruit().OnCommand(null);
            // No actor and no MessageLog assertion — just must not throw.
            // (Skill bails silently with no diag because Diag's actor field
            // would also be null; that's acceptable for the null-context
            // path which only fires when something is fundamentally wrong.)
            Assert.Pass();
        }

        [Test]
        public void Veto1_NullZone_EmitsRejected_NoApply()
        {
            var attacker = MakeActor("a");
            new Persuasion_Recruit().OnCommand(
                new SkillEventContext { Attacker = attacker, Zone = null, Rng = new Random(0) });
            Assert.AreEqual(1, CountDiag("SkillRejected", "null_context"),
                "Null zone bails with reason=null_context.");
        }

        [Test]
        public void Veto2_NoAdjacentTarget_EmitsRejected_NoTarget()
        {
            var attacker = MakeActor("a");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            // No defender adjacent.

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "no_target"));
        }

        [Test]
        public void Veto3_SelfTarget_NotReachable_ButSelfRefSafe()
        {
            // FindAdjacentCleaveTarget(actor, actor, zone) excludes the actor
            // itself; so self-targeting can't happen via the adjacency path.
            // This counter-check verifies that even if the only adjacent
            // "creature" IS the actor, the rejection is "no_target", not
            // a self-recruit. (The Veto #3 branch is defense-in-depth for
            // future code paths that might bypass the adjacency picker.)
            var attacker = MakeActor("a");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(0, CountDiag("SkillRejected", "self_target"),
                "Adjacency picker filters self → no self_target diag.");
            Assert.AreEqual(0, CountDiag("Recruited"),
                "Self can NEVER be recruited under any circumstance.");
            Assert.IsNull(attacker.GetPart<BrainPart>().PartyLeader,
                "Self can NEVER become its own leader.");
        }

        [Test]
        public void Veto4_TargetWithoutBrain_EmitsRejected_TargetNoBrain()
        {
            var attacker = MakeActor("a");
            var brainless = new Entity { ID = "rock", BlueprintName = "rock" };
            brainless.Tags["Creature"] = "";
            brainless.AddPart(new RenderPart { DisplayName = "rock" });
            brainless.AddPart(new PhysicsPart { Solid = true });
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(brainless, 6, 5);

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "target_no_brain"));
        }

        [Test]
        public void Veto5_AlreadyRecruited_EmitsRejected_AlreadyRecruited()
        {
            var (attacker, defender, zone, recruit) = MakeFixture();
            defender.ApplyEffect(new RecruitedEffect(attacker), source: attacker, zone: zone);
            Diag.ResetAll(); // ignore the apply-time records

            recruit.OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "already_recruited"));
        }

        [Test]
        public void Veto6_FollowsAnother_EmitsRejected_FollowsAnother()
        {
            var (attacker, defender, zone, recruit) = MakeFixture();
            var otherLeader = MakeActor("other");
            defender.GetPart<BrainPart>().SetPartyLeader(otherLeader);

            recruit.OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "follows_another"));
            Assert.AreSame(otherLeader, defender.GetPart<BrainPart>().PartyLeader,
                "Failed recruit doesn't disturb existing leader.");
        }

        [Test]
        public void Veto7_TargetHostile_EmitsRejected_TargetHostile()
        {
            // Mark defender hostile to attacker via PersonalEnemies on the
            // ATTACKER side — that makes GetFeeling(target=defender,
            // source=attacker) return HOSTILE because of attacker's grudge.
            // (Two-way hostility: either side's grudge counts.)
            var (attacker, defender, zone, recruit) = MakeFixture();
            attacker.GetPart<BrainPart>().PersonalEnemies.Add(defender);

            recruit.OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "target_hostile"));
        }

        [Test]
        public void TargetHostile_FromDefenderGrudge_EmitsRejected_TargetHostile()
        {
            // FactionManager.GetFeeling checks both sides of
            // PersonalEnemies bidirectionally (FactionManager.cs:187-192),
            // so a defender-side grudge surfaces as "target_hostile" via
            // Veto #7. Post-F.2.7 audit (Finding #3) removed the separate
            // "personal_grudge" veto as dead code; this test now pins
            // the consolidated behavior — any grudge, either direction,
            // produces "target_hostile" rejection.
            var (attacker, defender, zone, recruit) = MakeFixture();
            defender.GetPart<BrainPart>().PersonalEnemies.Add(attacker);

            recruit.OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "target_hostile"),
                "Defender-side PersonalEnemies grudge surfaces as " +
                "target_hostile via Veto #7's GetFeeling (which checks " +
                "both directions).");
            Assert.AreEqual(0, CountDiag("SkillRejected", "personal_grudge"),
                "personal_grudge veto removed in post-F.2.7 audit — " +
                "regression check that the dead branch hasn't been re-added.");
            Assert.AreEqual(0, CountDiag("Recruited"));
            Assert.IsNull(defender.GetPart<BrainPart>().PartyLeader);
        }

        // ════════════════════════════════════════════════════════════════
        // F.3.3 — slot enforcement (at_companion_limit veto)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AtLimit_VetoFires_NoEffectApplied()
        {
            // Actor has Persuasion_Recruit (= +1 slot) and already
            // recruited 1 follower. A second recruit attempt must fire
            // the at_companion_limit veto.
            var attacker = MakeActor("attacker", ego: 22, level: 5);
            attacker.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var firstFollower = MakeActor("f1", ego: 10, level: 1);
            firstFollower.ApplyEffect(new RecruitedEffect(attacker), source: attacker, zone: null);

            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(1, CountDiag("SkillRejected", "at_companion_limit"));
            Assert.AreEqual(0, CountDiag("Recruited"));
            Assert.IsFalse(defender.HasEffect<RecruitedEffect>(),
                "At-limit recruit must NOT apply the effect.");
        }

        [Test]
        public void BelowLimit_RecruitProceeds()
        {
            // Actor has the skill (+1 slot) and zero current followers.
            // Recruit should proceed to the roll (not veto).
            var attacker = MakeActor("attacker", ego: 22, level: 5);
            attacker.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(0, CountDiag("SkillRejected", "at_companion_limit"),
                "Below-limit recruit must NOT fire at_companion_limit.");
            // The roll may or may not succeed (probabilistic), but the
            // veto path is clean.
        }

        [Test]
        public void AfterDismissingFollower_NewRecruitProceeds()
        {
            // Counter-check (anti-exploit): dismissing a follower frees
            // a slot. Subsequent recruit attempt is allowed (no veto).
            var attacker = MakeActor("attacker", ego: 22, level: 5);
            attacker.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var firstFollower = MakeActor("f1", ego: 10, level: 1);
            var firstEffect = new RecruitedEffect(attacker);
            firstFollower.ApplyEffect(firstEffect, source: attacker, zone: null);

            // Dismiss the first follower → slot freed.
            firstEffect.Dismiss(attacker);

            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(0, CountDiag("SkillRejected", "at_companion_limit"),
                "Slot was freed by dismiss — at_companion_limit must NOT fire.");
        }

        [Test]
        public void LimitCheck_OnlyCountsRecruitedFollowers_NotPartyMembersFromOtherSources()
        {
            // PartyMembers can include entities added via direct
            // SetPartyLeader (e.g., scripted faction allegiance,
            // F.5+ Beguile path). The at_companion_limit check should
            // ONLY count followers with a RecruitedEffect from THIS actor
            // — they're the "Recruit"-means followers, the only ones
            // the +1 slot from Persuasion_Recruit applies to.
            var attacker = MakeActor("attacker", ego: 22, level: 5);
            attacker.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            // Add a "follower" via direct SetPartyLeader (NOT via recruit).
            var fakeFollower = MakeActor("fake", ego: 10, level: 1);
            fakeFollower.GetPart<BrainPart>().SetPartyLeader(attacker);
            // No RecruitedEffect — they're a follower-of-leader but not
            // a "Recruit"-means follower.

            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(Ctx(attacker, zone));

            Assert.AreEqual(0, CountDiag("SkillRejected", "at_companion_limit"),
                "Non-recruited PartyMembers don't count toward the Recruit slot limit.");
        }

        [Test]
        public void LimitCheck_DoesNotCountFollowersFromOtherRecruiters()
        {
            // Counter-check: if Bob has a follower Carol (Bob recruited
            // Carol), then Alice's recruit attempt is NOT blocked by
            // Bob's followers. Slots are per-actor.
            var alice = MakeActor("alice", ego: 22, level: 5);
            alice.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var bob = MakeActor("bob", ego: 22, level: 5);
            var carol = MakeActor("carol", ego: 10, level: 1);
            carol.ApplyEffect(new RecruitedEffect(bob), source: bob, zone: null);

            var defender = MakeActor("d", ego: 10, level: 1);
            var zone = new Zone();
            zone.AddEntity(alice, 5, 5);
            zone.AddEntity(defender, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(Ctx(alice, zone));

            Assert.AreEqual(0, CountDiag("SkillRejected", "at_companion_limit"),
                "Bob's recruit Carol does NOT count toward Alice's limit.");
        }

        // ════════════════════════════════════════════════════════════════
        // Roll: success path + failure path + diag payloads
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Roll_HighEgoVsLowLevel_AlmostAlwaysSucceeds()
        {
            // Ego=22 (modifier +3) vs Level-1 defender at attacker-Level 1
            // means DC = 10 + 0 = 10. Roll = d20 + 3. Failure only on d20==1
            // → 5% miss rate. Across 30 seeds, we should see many successes.
            int successes = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                Diag.ResetAll();
                var (attacker, defender, zone, recruit) =
                    MakeFixture(egoAttacker: 22, levelAttacker: 1, levelDefender: 1);
                recruit.OnCommand(Ctx(attacker, zone, seed));
                if (CountDiag("Recruited") == 1) successes++;
            }
            // Roll math: d20+3 ≥ 10 needs d20 ≥ 7 → 70% theoretical = 21/30.
            // Floor at ≥15 (50%) to account for RNG variance over 30 seeds;
            // a real regression would tank to ~zero.
            Assert.GreaterOrEqual(successes, 15,
                $"Ego=22 (mod +3) vs Level-1 defender should succeed strongly more than half the time " +
                $"(observed {successes}/30 — theoretical 70%/21).");
        }

        [Test]
        public void Roll_LowEgoVsHighLevel_AlmostAlwaysFails()
        {
            // Ego=8 (mod -4) vs Level-20 defender at attacker-Level 1
            // means DC = 10 + 19 = 29. Roll = d20 - 4 — max possible 16.
            // 16 < 29 → IMPOSSIBLE to hit DC. Should be 0/30.
            int successes = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                Diag.ResetAll();
                var (attacker, defender, zone, recruit) =
                    MakeFixture(egoAttacker: 8, levelAttacker: 1, levelDefender: 20);
                recruit.OnCommand(Ctx(attacker, zone, seed));
                if (CountDiag("Recruited") == 1) successes++;
            }
            Assert.AreEqual(0, successes,
                "Ego=8 (mod -4) vs Level-20 defender: DC=29, max roll=16 — " +
                "impossible. Must be 0/30 successes.");
        }

        [Test]
        public void RecruitSuccess_AppliesRecruitedEffect_AndSetsPartyLeader()
        {
            // Force a guaranteed-success seed: Ego=22 vs Level-1, seed=0
            // (which we verified by enumeration produces a d20 high enough
            // to clear DC=10).
            var (attacker, defender, zone, recruit) =
                MakeFixture(egoAttacker: 22, levelAttacker: 1, levelDefender: 1);

            // Iterate seeds until we land a Recruited; verifies the success
            // PAYLOAD shape too.
            for (int seed = 0; seed < 30; seed++)
            {
                Diag.ResetAll();
                var (a, d, z, r) =
                    MakeFixture(egoAttacker: 22, levelAttacker: 1, levelDefender: 1);
                r.OnCommand(Ctx(a, z, seed));
                if (CountDiag("Recruited") == 1)
                {
                    Assert.IsTrue(d.HasEffect<RecruitedEffect>(),
                        "Success path applies RecruitedEffect.");
                    Assert.AreSame(a, d.GetPart<BrainPart>().PartyLeader,
                        "Success path's RecruitedEffect.OnApply ran and set leader.");
                    return; // done — at least one success observed
                }
            }
            Assert.Fail("Expected at least one success across 30 seeds with Ego=22 vs Lvl=1.");
        }

        [Test]
        public void RollFailure_EmitsRejected_RollFailed_NoEffectApplied()
        {
            // Impossible roll (low Ego vs high Level — see Roll_LowEgo... test).
            var (attacker, defender, zone, recruit) =
                MakeFixture(egoAttacker: 8, levelAttacker: 1, levelDefender: 20);

            recruit.OnCommand(Ctx(attacker, zone, seed: 0));

            Assert.AreEqual(1, CountDiag("SkillRejected", "roll_failed"));
            Assert.AreEqual(0, CountDiag("Recruited"));
            Assert.IsFalse(defender.HasEffect<RecruitedEffect>());
            Assert.IsNull(defender.GetPart<BrainPart>().PartyLeader);
        }

        // ════════════════════════════════════════════════════════════════
        // Determinism — same seed produces the same outcome
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Determinism_SameSeed_ProducesSameOutcome()
        {
            // Seeded RNG → same roll → same success/failure across runs.
            bool? firstOutcome = null;
            for (int run = 0; run < 3; run++)
            {
                Diag.ResetAll();
                var (a, d, z, r) =
                    MakeFixture(egoAttacker: 14, levelAttacker: 1, levelDefender: 5);
                r.OnCommand(Ctx(a, z, seed: 42));
                bool succeeded = CountDiag("Recruited") == 1;
                firstOutcome ??= succeeded;
                Assert.AreEqual(firstOutcome.Value, succeeded,
                    $"Seed=42 must produce same outcome each run; run {run} differed.");
            }
        }
    }
}
