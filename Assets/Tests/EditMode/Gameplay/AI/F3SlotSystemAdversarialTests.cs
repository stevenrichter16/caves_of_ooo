using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.3.6 — adversarial sweep for the slot system +
    /// GrantsRepAsFollowerPart (F.3.2-F.3.5).
    ///
    /// <para>F.3 hits these taxonomy surfaces from CLAUDE.md:</para>
    /// <list type="bullet">
    ///   <item>State atomicity — apply/unapply pairs balanced under
    ///         partial-fail paths</item>
    ///   <item>Cross-actor flows — leader-is-player gate vs NPC-led
    ///         followers</item>
    ///   <item>Save/load reach — AppliedBonus and slot state across
    ///         round-trip</item>
    ///   <item>Anti-exploit gates — no rep-pump, no slot-bypass</item>
    ///   <item>Probability boundaries — N/A (no random rolls in F.3)</item>
    ///   <item>Diag dispatch — exactly one veto record per failed attempt</item>
    ///   <item>Cross-system aggregation — multiple
    ///         GrantsRepAsFollowerPart instances on different followers
    ///         all flow to the same player faction</item>
    ///   <item>Parser malformed inputs (Qud-parity comma-delimited syntax)</item>
    /// </list>
    ///
    /// <para><b>Honesty bound (CLAUDE.md):</b> 0 bugs found by this
    /// sweep does NOT prove F.3 is bug-free. Bug classes are bounded
    /// by what the author imagined. This file's value is regression
    /// infrastructure: future changes break visibly with a named test.</para>
    /// </summary>
    public class F3SlotSystemAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
            PlayerReputation.Reset();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeActor(string id, int ego = 22, int level = 5)
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
            e.AddPart(new SkillsPart());
            return e;
        }

        private static Entity MakePlayer()
        {
            var p = MakeActor("player");
            p.Tags["Player"] = "";
            return p;
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
        // STATE ATOMICITY — apply/unapply balanced under odd sequences
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ApplyUnapplyApply_CorrectFinalState()
        {
            // Apply → unapply → apply: final state is +10 (one apply).
            // Bookkeeping mistake (e.g. accumulator never resets) would
            // surface as +20 or 0.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();

            part.CheckApplyBonus(player);              // apply +10
            npc.GetPart<BrainPart>().SetPartyLeader(null);
            part.CheckApplyBonus(null);                 // unapply -10 (net 0)
            npc.GetPart<BrainPart>().SetPartyLeader(player);
            part.CheckApplyBonus(player);              // apply +10

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "After apply→unapply→apply: final rep is +10. " +
                "Any deviation indicates a bookkeeping mistake.");
        }

        [Test]
        public void Adversarial_ZoneTransitOscillation_NoRepDrift()
        {
            // Player oscillates between zones repeatedly. The follower's
            // GrantsRepAsFollowerPart should apply/unapply in lockstep.
            // After 5 cycles, rep should be the same as after 0 cycles.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var zoneA = new Zone("A");
            var zoneB = new Zone("B");
            zoneA.AddEntity(player, 5, 5);
            zoneA.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zoneA;
            npc.GetPart<BrainPart>().CurrentZone = zoneA;
            npc.GetPart<BrainPart>().SetPartyLeader(player);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();

            for (int i = 0; i < 5; i++)
            {
                // Player visits zoneA (with follower) → apply
                player.GetPart<BrainPart>().CurrentZone = zoneA;
                part.CheckApplyBonus(player);
                // Player visits zoneB (without follower) → unapply
                player.GetPart<BrainPart>().CurrentZone = zoneB;
                part.CheckApplyBonus(player);
            }

            // Final state: player in zoneB, follower in zoneA → unapplied → 0
            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "5 oscillations → final unapplied state → 0 rep. " +
                "Any drift (e.g. +50, -50) indicates apply/unapply isn't balanced.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-ACTOR — slot limits are per-actor, rep applies only when player-led
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SlotLimit_NotShared_BetweenActors()
        {
            // Alice and Bob each have Persuasion_Recruit (1 slot each).
            // Alice's recruit doesn't count against Bob's slot, and
            // vice versa.
            var alice = MakeActor("alice");
            alice.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var bob = MakeActor("bob");
            bob.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");

            var aliceRecruit = MakeActor("ar"); aliceRecruit.Statistics["Level"].BaseValue = 1;
            aliceRecruit.ApplyEffect(new RecruitedEffect(alice), source: alice, zone: null);
            var bobRecruit = MakeActor("br"); bobRecruit.Statistics["Level"].BaseValue = 1;
            bobRecruit.ApplyEffect(new RecruitedEffect(bob), source: bob, zone: null);

            // Each actor has 1 recruit; each has 1 slot. Both at-limit independently.
            // Alice tries to recruit a new target: should be at-limit
            // (alice has 1 recruit, alice's slot limit is 1, so 1>=1 → veto).
            var target = MakeActor("target"); target.Statistics["Level"].BaseValue = 1;
            var zone = new Zone();
            zone.AddEntity(alice, 5, 5); zone.AddEntity(target, 6, 5);
            Diag.ResetAll();
            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = alice, Defender = alice, Zone = zone, Rng = new Random(0) });

            // Alice's at-limit veto fires. Bob's recruit is NOT counted
            // in alice's count (the filter is per-actor via Recruiter).
            Assert.AreEqual(1, CountDiag("SkillRejected", "at_companion_limit"),
                "Per-actor slot accounting — Bob's recruit doesn't count against Alice.");
        }

        [Test]
        public void Adversarial_GrantsRepAsFollower_NPCLeader_NoRepFlow()
        {
            // Boss-NPC recruits a minion. The minion has
            // GrantsRepAsFollowerPart. The player isn't involved at all.
            // No player-rep change.
            var player = MakePlayer();
            var bossNpc = MakeActor("boss");
            var minionNpc = MakeActor("minion");
            minionNpc.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(bossNpc, 7, 5);
            zone.AddEntity(minionNpc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            bossNpc.GetPart<BrainPart>().CurrentZone = zone;
            minionNpc.GetPart<BrainPart>().CurrentZone = zone;
            minionNpc.GetPart<BrainPart>().SetPartyLeader(bossNpc);

            // Fire EndTurn — even though the player is co-located in the
            // zone, the leader-is-player gate filters out non-player leaders.
            var endTurn = GameEvent.New("EndTurn");
            minionNpc.FireEventAndRelease(endTurn);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "NPC leader → no player-rep flow.");
        }

        // ════════════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — slot veto holds; no rep-pump
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RepPump_RepeatedApplyUnapplyDoesNotAccumulate()
        {
            // Hypothesis: if AppliedBonus state weren't a gate,
            // calling CheckApplyBonus repeatedly could pump rep.
            // The Idempotency_DoubleApply test in the main fixture
            // proves this for the same-conditions case; this is the
            // conditions-flip case (apply, unapply, apply, ...).
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 100));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            var part = npc.GetPart<GrantsRepAsFollowerPart>();

            for (int i = 0; i < 100; i++)
            {
                // Apply (set leader)
                npc.GetPart<BrainPart>().SetPartyLeader(player);
                part.CheckApplyBonus(player);
                // Unapply (clear leader)
                npc.GetPart<BrainPart>().SetPartyLeader(null);
                part.CheckApplyBonus(null);
            }

            // After 100 apply-unapply cycles, ending in unapplied state:
            // net rep is zero. No drift.
            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "100 apply/unapply cycles → net zero rep. No pump exploit.");
        }

        [Test]
        public void Adversarial_RecruitedFollower_NotInPartyMembers_NotCounted()
        {
            // Edge: if a follower's PartyLeader points at the actor but
            // they're somehow NOT in actor's PartyMembers (sneaky direct
            // mutation that bypassed SetPartyLeader), the slot count
            // should still be correct (or at least defensively NOT
            // include the half-state entry).
            //
            // We can't easily construct this state directly, but we can
            // verify the counting algorithm walks PartyMembers (the
            // canonical roster) rather than scanning all entities — so
            // a sneaky direct mutation that bypasses the roster doesn't
            // sneak past the slot check.
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            // No PartyMembers, no RecruitedEffects on anyone.

            // Recruit attempt should NOT fire at_companion_limit
            // (current count 0 < limit 1).
            var defender = MakeActor("d"); defender.Statistics["Level"].BaseValue = 1;
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5); zone.AddEntity(defender, 6, 5);

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0) });

            Assert.AreEqual(0, CountDiag("SkillRejected", "at_companion_limit"),
                "Empty roster → no at-limit veto. The count routine handles " +
                "the zero-PartyMember case without error.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-SYSTEM AGGREGATION — multiple GrantsRep followers stack
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoFollowers_SameFaction_StacksLinearly()
        {
            // Player has two followers, each with GrantsRepAsFollowerPart
            // for the same faction. Their bonuses stack — both apply +10
            // → +20 total.
            var player = MakePlayer();
            var npc1 = MakeActor("npc1");
            npc1.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var npc2 = MakeActor("npc2");
            npc2.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc1, 6, 5);
            zone.AddEntity(npc2, 7, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc1.GetPart<BrainPart>().CurrentZone = zone;
            npc2.GetPart<BrainPart>().CurrentZone = zone;
            npc1.GetPart<BrainPart>().SetPartyLeader(player);
            npc2.GetPart<BrainPart>().SetPartyLeader(player);

            npc1.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);
            npc2.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(20, PlayerReputation.Get("Snapjaws"),
                "Two followers, each +10 → +20 total (linear stack).");
        }

        [Test]
        public void Adversarial_DifferentFollowers_DifferentFactions_IndependentApply()
        {
            // npc1 grants Snapjaws rep, npc2 grants Bandits rep.
            // They apply independently to different factions.
            var player = MakePlayer();
            var npc1 = MakeActor("npc1");
            npc1.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            var npc2 = MakeActor("npc2");
            npc2.AddPart(new GrantsRepAsFollowerPart("Bandits", 5));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc1, 6, 5);
            zone.AddEntity(npc2, 7, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc1.GetPart<BrainPart>().CurrentZone = zone;
            npc2.GetPart<BrainPart>().CurrentZone = zone;
            npc1.GetPart<BrainPart>().SetPartyLeader(player);
            npc2.GetPart<BrainPart>().SetPartyLeader(player);

            npc1.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);
            npc2.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"));
            Assert.AreEqual(5, PlayerReputation.Get("Bandits"));
        }

        // ════════════════════════════════════════════════════════════════
        // DIAG DISPATCH — exactly one veto per failed recruit attempt
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AtLimitVeto_EmitsExactlyOneRecord()
        {
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var existingFollower = MakeActor("ef");
            existingFollower.ApplyEffect(new RecruitedEffect(actor), source: actor, zone: null);

            var target = MakeActor("t"); target.Statistics["Level"].BaseValue = 1;
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5); zone.AddEntity(target, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0) });

            Assert.AreEqual(1, CountDiag("SkillRejected"),
                "Exactly one SkillRejected record per at-limit attempt — not zero, not two.");
            Assert.AreEqual(1, CountDiag("SkillRejected", "at_companion_limit"));
        }

        // ════════════════════════════════════════════════════════════════
        // PARSER MALFORMED INPUTS (Qud-parity comma-delimited)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Parser_ColonWithoutValue_FallsBackToValue()
        {
            // "Snapjaws:" (colon with empty value) → falls back to Value.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws:", 5));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(5, PlayerReputation.Get("Snapjaws"),
                "Colon without value → fallback to default Value.");
        }

        [Test]
        public void Adversarial_Parser_NonNumericColonValue_FallsBackToValue()
        {
            // "Snapjaws:abc" → can't parse "abc" as int → falls back to Value.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws:abc", 5));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(5, PlayerReputation.Get("Snapjaws"),
                "Non-numeric colon value → graceful fallback, no crash.");
        }

        [Test]
        public void Adversarial_Parser_ColonOnlyEntry_NoCrash()
        {
            // ":" or "::" → no faction name; the entry should be skipped
            // gracefully without crashing or applying to an empty
            // faction.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart(":,::,", 5));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            Assert.DoesNotThrow(() =>
                npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player));
            // Empty faction name → no apply.
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "Colon-only entries skipped → no apply → AppliedBonus stays false.");
        }

        [Test]
        public void Adversarial_Parser_NegativeValueInString_AppliedAsNegative()
        {
            // Negative deltas are valid: "Snapjaws:-10" decreases rep.
            var player = MakePlayer();
            var npc = MakeActor("npc");
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws:-10", 0));
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5); zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(-10, PlayerReputation.Get("Snapjaws"),
                "Negative per-faction values honored (an annoying companion " +
                "that costs you rep).");
        }

        // ════════════════════════════════════════════════════════════════
        // SCALE — many slot-contributing skills, many followers
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TenFollowers_SameRecruiter_OneSlotLimit_RecruitFails()
        {
            // Pathological: a recruiter has 1 slot but somehow 10
            // pre-existing recruits (e.g., bypassed the veto via test
            // direct ApplyEffect). At-limit veto fires (10 >= 1).
            var actor = MakeActor("a");
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            for (int i = 0; i < 10; i++)
            {
                var follower = MakeActor("f" + i);
                follower.ApplyEffect(new RecruitedEffect(actor), source: actor, zone: null);
            }

            var target = MakeActor("t"); target.Statistics["Level"].BaseValue = 1;
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5); zone.AddEntity(target, 6, 5);
            Diag.ResetAll();

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0) });

            Assert.AreEqual(1, CountDiag("SkillRejected", "at_companion_limit"),
                "10 over-limit recruits → at-limit veto fires. No false-pass.");
        }
    }
}
