using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Density Phase 1 (Docs/DENSITY-PHASE-1.md §T1.3–T1.5): content
    /// repairs that each close a silent gap between what a blueprint
    /// promised and what the player meets.
    ///
    /// <list type="bullet">
    /// <item>The maw-toad, the bandfrog and the spore shambler are
    /// placed in live tables and lairs but carried no natural weapon, so
    /// all three punched with the humanoid default fist (1d2).</item>
    /// <item>The item literally called "cudgel" lacked the <c>Cudgel</c>
    /// weapon-family tag, so Slam, Ground Pound, Backswing and Cudgel
    /// Expertise refused it (they gate on that tag).</item>
    /// <item>The loaner longsword declared <c>Slashing</c>, a word nothing
    /// in the damage model reads, so it had no physical class and did
    /// not count as a long blade.</item>
    /// <item>Signposts, shrines, bookshelves and graveyards examined as a
    /// bare "You see a signpost."</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class DensityPhase1ContentTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            FactionManager.Initialize();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [OneTimeTearDown]
        public void TearDown() => FactionManager.Reset();

        [SetUp]
        public void Setup() => MessageLog.Clear();

        private static Entity Create(string blueprint)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint + " must exist");
            return e;
        }

        // ════════════════════════════════════════════════════════
        // §T1.3 — natural weapons for the three unarmed hostiles
        // ════════════════════════════════════════════════════════

        [TestCase("MawToad", "DefaultBite", "1d3+1")]
        [TestCase("Bandfrog", "DefaultBite", "1d3+1")]
        [TestCase("Shambler", "DefaultTendril", "1d3")]
        public void UnarmedHostile_NowFightsWithItsOwnNaturalWeapon(string blueprint, string recipe, string dice)
        {
            var creature = Create(blueprint);
            var hands = creature.GetPart<Body>().GetPartsByType("Hand");
            Assert.AreEqual(2, hands.Count, blueprint + " keeps the default two-handed frame");
            foreach (var hand in hands)
            {
                Assert.AreEqual(recipe, hand.DefaultBehaviorBlueprint, blueprint);
                Assert.IsNotNull(hand._DefaultBehavior, blueprint + " natural weapon materialized at creation");
                Assert.AreEqual(dice, hand._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage, blueprint);
            }
        }

        [Test]
        public void ACreatureWithNoAuthoredWeapon_StillPunchesWithAFist()
        {
            // Counter-check: the three fixes are per-blueprint props. If a
            // change instead rewrote the default Hand recipe, every villager
            // would start biting, and this goes red.
            var villager = Create("Villager");
            foreach (var hand in villager.GetPart<Body>().GetPartsByType("Hand"))
            {
                Assert.AreEqual("DefaultFist", hand.DefaultBehaviorBlueprint);
                Assert.AreEqual("1d2", hand._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage);
            }
        }

        [Test]
        public void TheSporeShambler_KeepsItsSporeTouch()
        {
            // Counter-check: the plain Shambler got a tendril, not
            // SporeTouch (that would silently give it a disease vector).
            // The fungal SporeShambler must be untouched.
            var hand = Create("SporeShambler").GetPart<Body>().GetPartsByType("Hand")[0];
            Assert.AreEqual("SporeTouch", hand.DefaultBehaviorBlueprint);
            var plain = Create("Shambler").GetPart<Body>().GetPartsByType("Hand")[0];
            Assert.IsTrue(string.IsNullOrEmpty(plain._DefaultBehavior.GetPart<MeleeWeaponPart>().EmitGasOnHitRaw),
                "the plain shambler's tendril releases no spores");
        }

        // ════════════════════════════════════════════════════════
        // §T1.4 — weapon-family tags the skills actually read
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheCudgel_CountsAsACudgel_ForCudgelSkills()
        {
            var actor = Create("Villager");
            var cudgel = Create("Cudgel");
            actor.GetPart<InventoryPart>().AddObject(cudgel);
            Assert.IsTrue(InventorySystem.Equip(actor, cudgel), "villager can wield the cudgel");

            var found = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            Assert.AreSame(cudgel.GetPart<MeleeWeaponPart>(), found,
                "Slam and Ground Pound look up the wielded weapon by this exact tag");
            StringAssert.Contains("Bludgeoning", cudgel.GetPart<MeleeWeaponPart>().Attributes,
                "and it keeps its physical class");
        }

        [Test]
        public void AWieldedDagger_IsNotACudgel()
        {
            // Counter-check for the lookup above: the helper really
            // discriminates by tag, so the positive test is not vacuous.
            var actor = Create("Villager");
            var dagger = Create("Dagger");
            actor.GetPart<InventoryPart>().AddObject(dagger);
            Assert.IsTrue(InventorySystem.Equip(actor, dagger));
            Assert.IsNull(SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel"));
        }

        [Test]
        public void TheLoanerLongsword_IsACuttingLongBlade()
        {
            var w = Create("LoanerLongsword").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes,
                "same family string as LongSword, Greatsword and ShortSword");
        }

        [Test]
        public void NoShippedMeleeWeapon_DeclaresTheInertSlashingTag()
        {
            // Adversarial sweep over all content: "Slashing" is read by no
            // resistance, skill or effect. Any weapon declaring it has
            // silently lost its physical class.
            foreach (var bp in _factory.Blueprints.Values)
            {
                if (!bp.Parts.TryGetValue("MeleeWeapon", out var part)) continue;
                if (!part.TryGetValue("Attributes", out var attrs) || attrs == null) continue;
                StringAssert.DoesNotContain("Slashing", attrs, bp.Name + " declares an attribute nothing reads");
            }
        }

        // ════════════════════════════════════════════════════════
        // §T1.5 — silent objects get words
        // ════════════════════════════════════════════════════════

        [TestCase("Signpost")]
        [TestCase("Shrine")]
        [TestCase("Bookshelf")]
        [TestCase("Graveyard")]
        public void SilentFixture_NowSaysSomethingWhenExamined(string blueprint)
        {
            var fixture = Create(blueprint);
            var exam = fixture.GetPart<ExaminablePart>();
            Assert.IsNotNull(exam);
            Assert.IsFalse(string.IsNullOrWhiteSpace(exam.Text), blueprint + " must carry examine prose");

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Examine");
            fixture.FireEvent(e);

            bool logged = false;
            foreach (var line in MessageLog.GetMessages())
                if (line.Contains(exam.Text.Trim())) logged = true;
            Assert.IsTrue(logged, blueprint + ": the prose reaches the message log when examined");
        }

        [Test]
        public void ThingsLeftSilentOnPurpose_StaySilent()
        {
            // Counter-check: the prose is added per blueprint, not as a
            // default on PhysicalObject. If PhysicalObject grew a default
            // description, the gin frog (silent by canon gate 7,
            // SoddenBestiaryTests.TheGinFrog_ExplainsNothing) would inherit
            // it and this goes red.
            var exam = Create("GinFrog").GetPart<ExaminablePart>();
            Assert.IsTrue(exam == null || string.IsNullOrEmpty(exam.Text),
                "the gin frog must still explain nothing");
        }
    }
}
