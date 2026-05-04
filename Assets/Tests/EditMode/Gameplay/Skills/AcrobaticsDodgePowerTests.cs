using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.5 — AcrobaticsDodgePower end-to-end integration tests. Pins:
    /// <list type="bullet">
    ///   <item>Buying Acrobatics tree-root attaches the marker part.</item>
    ///   <item>Buying Dodge applies +2 DV to Entity.Statistics["DV"].</item>
    ///   <item>Removing Dodge restores DV to its pre-shift value.</item>
    ///   <item>Idempotent: re-adding Dodge after removal applies +2 once,
    ///         not multiple times.</item>
    ///   <item>Counter-check: removing a Dodge that was never added is a
    ///         clean no-op (orphaned-power scenario, plan-mandated).</item>
    /// </list>
    ///
    /// <para>Combat-side consumption of DV is OUT OF SCOPE — see the
    /// 🟡 finding in ST.5 commit body. v1 verifies the stat-shift round-
    /// trips on Entity.Statistics; combat hit-roll integration is a
    /// follow-on.</para>
    /// </summary>
    public class AcrobaticsDodgePowerTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => Diag.ResetAll();

        // ====================================================================
        // 1. Buying Acrobatics tree-root attaches the marker
        // ====================================================================

        [Test]
        public void AddAcrobaticsSkill_AttachesMarkerToEntity()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var skills = new SkillsPart();
            player.AddPart(skills);

            bool added = skills.AddSkill(new AcrobaticsSkill());

            Assert.IsTrue(added, "AddSkill(AcrobaticsSkill) should succeed.");
            Assert.IsTrue(skills.HasSkill(nameof(AcrobaticsSkill)),
                "AcrobaticsSkill should be in SkillsPart.");
            Assert.IsNotNull(player.GetPart<AcrobaticsSkill>(),
                "Marker part should be attached to entity.Parts.");
        }

        // ====================================================================
        // 2. Buying Dodge applies +2 DV
        // ====================================================================

        [Test]
        public void AddDodgePower_AppliesPlusTwoDV()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var skills = new SkillsPart();
            player.AddPart(skills);
            int dvBefore = player.GetStatValue("DV", 0);

            skills.AddSkill(new AcrobaticsDodgePower());

            int dvAfter = player.GetStatValue("DV", 0);
            Assert.AreEqual(dvBefore + AcrobaticsDodgePower.DV_BONUS, dvAfter,
                $"DV should rise from {dvBefore} to {dvBefore + AcrobaticsDodgePower.DV_BONUS} " +
                "after AcrobaticsDodgePower attaches.");
        }

        // ====================================================================
        // 3. Removing Dodge restores DV
        // ====================================================================

        [Test]
        public void RemoveDodgePower_RestoresDV()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var skills = new SkillsPart();
            player.AddPart(skills);
            int dvBefore = player.GetStatValue("DV", 0);

            var dodge = new AcrobaticsDodgePower();
            skills.AddSkill(dodge);
            Assert.AreEqual(dvBefore + AcrobaticsDodgePower.DV_BONUS,
                player.GetStatValue("DV", 0),
                "Precondition: shift applied.");

            skills.RemoveSkill(dodge);

            Assert.AreEqual(dvBefore, player.GetStatValue("DV", 0),
                "DV should be back to its pre-shift value after RemoveSkill.");
        }

        // ====================================================================
        // 4. Re-add after remove: net is single shift (no accumulation)
        // ====================================================================

        [Test]
        public void AddRemoveAdd_NetsToSinglePlusTwoShift()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var skills = new SkillsPart();
            player.AddPart(skills);
            int dvBefore = player.GetStatValue("DV", 0);

            var first = new AcrobaticsDodgePower();
            skills.AddSkill(first);
            skills.RemoveSkill(first);
            // After this, DV should be back to dvBefore.

            // New instance — duplicate-by-Type is checked, so re-adding the
            // SAME instance after remove won't work; use a new instance.
            var second = new AcrobaticsDodgePower();
            skills.AddSkill(second);

            Assert.AreEqual(dvBefore + AcrobaticsDodgePower.DV_BONUS,
                player.GetStatValue("DV", 0),
                "Second add should apply exactly +2 DV (a fresh shift), " +
                "NOT +4 (which would mean the first shift wasn't fully rolled back).");
        }

        // ====================================================================
        // 5. Counter-check: removing a never-attached skill is a no-op
        //    (plan-mandated counter-check from ST.5 spec)
        // ====================================================================

        [Test]
        public void RemoveDodge_NeverAttached_DoesNotCrashAndDoesNotMutateDV()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var skills = new SkillsPart();
            player.AddPart(skills);
            int dvBefore = player.GetStatValue("DV", 0);

            var orphan = new AcrobaticsDodgePower();
            // Note: NEVER added to skills. Calling Remove must be safe
            // AND must NOT subtract DV (because we never added it).

            bool removed = skills.RemoveSkill(orphan);

            Assert.IsFalse(removed,
                "Remove of an un-added skill should return false.");
            Assert.AreEqual(dvBefore, player.GetStatValue("DV", 0),
                "DV must NOT change when removing a skill that was never " +
                "added — otherwise removing a power could subtract a " +
                "shift that was never applied, leaving DV negative.");
        }

        // ====================================================================
        // 6. Player blueprint sanity: DV stat exists with default 0
        //    (pins the blueprint contract that the shift system depends on)
        // ====================================================================

        [Test]
        public void PlayerBlueprint_HasDVStat_WithDefaultZero()
        {
            var player = _harness.Factory.CreateEntity("Player");
            var dv = player.GetStat("DV");
            Assert.IsNotNull(dv,
                "Player blueprint must declare a DV stat — AcrobaticsDodgePower " +
                "depends on it being present. If you removed DV from the " +
                "Player blueprint, AcrobaticsDodgePower.AddSkill becomes a " +
                "silent no-op and the +2 DV grant is invisible.");
            Assert.AreEqual(0, dv.BaseValue,
                "DV must default to 0 — players don't start with dodge bonus.");
        }
    }
}
