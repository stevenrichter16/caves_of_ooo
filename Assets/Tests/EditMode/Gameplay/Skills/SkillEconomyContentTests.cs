using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M1.e — the skill economy. Pre-M1.e every tree was Cost 1
    /// throughout (a vending machine, no build shape) except Acrobatics,
    /// which was Qud-scale 100/50x4 — unpurchasable against the ~49 SP
    /// earnable by level 50. M1.e re-costs the economy (roots 1, basics
    /// 1-2, mid 2, capstones 3-4; ~125 SP to buy everything) and authors
    /// the first Requires chains through the validation path
    /// BuySkillAction has carried unused since it shipped.
    /// </summary>
    public class SkillEconomyContentTests
    {
        [SetUp]
        public void SetUp()
        {
            SkillRegistry.ResetForTests();
            SkillRegistry.EnsureInitialized();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SkillRegistry.ResetForTests();
            MessageLog.Clear();
        }

        private static Entity MakeBuyer(int sp)
        {
            var e = new Entity { ID = "buyer", BlueprintName = "buyer" };
            e.Tags["Creature"] = "";
            e.Statistics["SP"] = new Stat { Owner = e, Name = "SP", BaseValue = sp, Min = 0, Max = 999 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            e.AddPart(new SkillsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            return e;
        }

        // ====================================================================
        // Cost pins — the M1.e economy table.
        // ====================================================================

        [Test]
        [TestCase("AcrobaticsDodgePower", 2)]
        [TestCase("Acrobatics_EvasiveRoll", 3)]
        [TestCase("Axe_Decapitate", 4)]
        [TestCase("ShortBlades_Backstab", 4)]
        [TestCase("Pyromancy_HeartFlame", 4)]
        [TestCase("Spellcraft_ArcaneSurge", 4)]
        [TestCase("Pyromancy_Pyroclasm", 3)]
        [TestCase("Cudgel_GroundPound", 3)]
        public void PowerCosts_MatchEconomyTable(string className, int expected)
        {
            Assert.IsTrue(SkillRegistry.TryGetPowerByClass(className, out var power), className);
            Assert.AreEqual(expected, power.Cost, $"{className} M1.e cost.");
        }

        [Test]
        public void AcrobaticsRoot_IsFinallyPurchasable()
        {
            // Pre-M1.e: root 100 SP vs ~49 earnable by L50 — the whole tree
            // was dead content.
            Assert.IsTrue(SkillRegistry.TryGetSkillByClass("AcrobaticsSkill", out var data));
            Assert.AreEqual(1, data.Cost,
                "Acrobatics root must cost 1 like every other tree root.");
        }

        [Test]
        [TestCase("Pyromancy_Pyroclasm", "Pyromancy_Cinder")]
        [TestCase("Pyromancy_HeartFlame", "Pyromancy_ScorchRetort")]
        [TestCase("Axe_Decapitate", "Axe_Dismember")]
        [TestCase("ShortBlades_Backstab", "ShortBlades_Shank")]
        [TestCase("Spellcraft_ArcaneSurge", "Spellcraft_Empower")]
        [TestCase("Acrobatics_EvasiveRoll", "Acrobatics_Tumble")]
        public void CapstoneRequires_MatchEconomyTable(string className, string requires)
        {
            Assert.IsTrue(SkillRegistry.TryGetPowerByClass(className, out var power), className);
            Assert.AreEqual(requires, power.Requires,
                $"{className} must require {requires} — trees read as builds, " +
                "not vending machines.");
        }

        // ====================================================================
        // The Requires gate, end-to-end through BuySkillAction.
        // ====================================================================

        [Test]
        public void BuyCapstone_WithoutPrereq_RejectedAsMissingPrereq()
        {
            var buyer = MakeBuyer(sp: 20);
            Assert.AreEqual(BuySkillAction.FailureReason.None,
                BuySkillAction.Execute(buyer, "PyromancySkill").Reason,
                "Buying the tree root must succeed.");

            var result = BuySkillAction.Execute(buyer, "Pyromancy_Pyroclasm");

            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, result.Reason,
                "Pyroclasm without Cinder must reject via the Requires gate — " +
                "the validation path BuySkillAction carried unused until M1.e.");
        }

        [Test]
        public void BuyCapstone_WithPrereq_Succeeds()
        {
            // Counter-check: same setup, prereq purchased first — the gate
            // must open, proving the rejection above is the Requires check
            // and not some other failure.
            var buyer = MakeBuyer(sp: 20);
            BuySkillAction.Execute(buyer, "PyromancySkill");
            Assert.AreEqual(BuySkillAction.FailureReason.None,
                BuySkillAction.Execute(buyer, "Pyromancy_Cinder").Reason,
                "Cinder (no prereq) must purchase cleanly.");

            var result = BuySkillAction.Execute(buyer, "Pyromancy_Pyroclasm");

            Assert.AreEqual(BuySkillAction.FailureReason.None, result.Reason,
                "Pyroclasm with Cinder owned must purchase cleanly.");
        }
    }
}
