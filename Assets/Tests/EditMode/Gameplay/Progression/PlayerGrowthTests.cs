using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P2b (Docs/PLACE-WORK-AND-PROGRESSION.md): a level must make the player stronger,
    /// not only +2 HP. Qud parity (XRL.World.Parts/Leveler.cs:149-151, 262-275):
    /// HP per level includes the Toughness modifier (min 1); an attribute point at
    /// levels 3, 9, 15…; +1 to every attribute at 6, 12, 18; raising Toughness
    /// retroactively adds the modifier gain for every level already taken. Attribute
    /// points are spent from an Attributes group on the skills screen.
    /// </summary>
    public sealed class PlayerGrowthTests
    {
        static readonly string[] Attributes = { "Strength", "Agility", "Toughness", "Intelligence", "Willpower", "Ego" };
        [SetUp] public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        static Entity Player(int toughness = 16, int ap = 0)
        {
            var p = new Entity { BlueprintName = "Player", ID = "player" };
            p.SetTag("Player"); p.SetTag("Creature");
            void S(string n, int v, int max = 999) => p.Statistics[n] = new Stat { Name = n, BaseValue = v, Value = v, Min = 0, Max = max };
            S("Level", 1, 99); S("Experience", 0, 999999); S("Hitpoints", 40, 40); S("MP", 0); S("SP", 0); S("AP", ap);
            foreach (var a in Attributes) S(a, 16, 99);
            p.Statistics["Toughness"].BaseValue = toughness;
            return p;
        }
        static void LevelTo(Entity p, int level)
        {
            p.GetStat("Experience").BaseValue += ProgressionCurve.CumulativeXPToReach(level) - ProgressionCurve.CumulativeXPToReach(p.GetStatValue("Level"));
            LevelingSystem.CheckLevelUp(p, null);
            Assert.AreEqual(level, p.GetStatValue("Level"), "precondition: reached level " + level);
        }

        // ════════════════ hit points ════════════════

        [TestCase(16, 2)]  // modifier 0: the old flat +2 is kept
        [TestCase(20, 4)]  // modifier +2
        [TestCase(6, 1)]   // modifier -5: never below 1
        public void EachLevelAddsTwoPlusTheToughnessModifier(int toughness, int perLevel)
        {
            var p = Player(toughness); LevelTo(p, 2);
            Assert.AreEqual(40 + perLevel, p.GetStat("Hitpoints").Max);
        }

        // ════════════════ attribute points and bonuses ════════════════

        [Test] public void AttributePointsArriveAtLevelsThreeNineAndFifteen()
        {
            var p = Player();
            LevelTo(p, 2); Assert.AreEqual(0, p.GetStatValue("AP"), "counter-check: none at 2");
            LevelTo(p, 3); Assert.AreEqual(1, p.GetStatValue("AP"));
            LevelTo(p, 6); Assert.AreEqual(1, p.GetStatValue("AP"), "6 is a bonus level, not a point");
            LevelTo(p, 15); Assert.AreEqual(3, p.GetStatValue("AP"), "3, 9, 15");
        }

        [Test] public void EveryAttributeRisesAtLevelSix()
        {
            var p = Player(); LevelTo(p, 5);
            foreach (var a in Attributes) Assert.AreEqual(16, p.GetStatValue(a), "counter-check: nothing before 6 (" + a + ")");
            LevelTo(p, 6);
            foreach (var a in Attributes) Assert.AreEqual(17, p.GetStatValue(a), a);
        }

        [Test] public void TheLevelSixToughnessBonusPaysBackEveryEarlierLevel()
        {
            var p = Player(toughness: 17); LevelTo(p, 5);
            int before = p.GetStat("Hitpoints").Max;               // 40 + 4 levels at +2 (modifier 0)
            LevelTo(p, 6);                                          // Toughness 17 → 18: modifier 0 → 1
            Assert.AreEqual(18, p.GetStatValue("Toughness"));
            Assert.AreEqual(before + 3 + 4, p.GetStat("Hitpoints").Max, "this level's +3, plus +1 for each of the four earlier levels");
        }

        // ════════════════ spending points ════════════════

        [Test] public void SpendingAPointRaisesTheAttribute()
        {
            var p = Player(ap: 1);
            Assert.True(AttributeAdvancement.TrySpend(p, "Agility", out _));
            Assert.AreEqual(17, p.GetStatValue("Agility")); Assert.AreEqual(0, p.GetStatValue("AP"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "progression", Kind = "AttributeRaised", Limit = 5 }).Records.Count);
        }

        [Test] public void SpendingWithoutAPointChangesNothing()
        {
            var p = Player(ap: 0);
            Assert.False(AttributeAdvancement.TrySpend(p, "Agility", out string why));
            StringAssert.Contains("attribute point", why);
            Assert.AreEqual(16, p.GetStatValue("Agility"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "progression", Kind = "AttributeRefused", Limit = 5 }).Records.Count);
        }

        [Test] public void OnlyTheSixAttributesCanBeRaised()
        {
            var p = Player(ap: 1);
            Assert.False(AttributeAdvancement.TrySpend(p, "Hitpoints", out _));
            Assert.False(AttributeAdvancement.TrySpend(p, null, out _));
            Assert.AreEqual(1, p.GetStatValue("AP"), "nothing spent");
        }

        [Test] public void RaisingToughnessPaysBackEveryEarlierLevel()
        {
            var p = Player(toughness: 17, ap: 1); LevelTo(p, 4);
            int before = p.GetStat("Hitpoints").Max, current = p.GetStatValue("Hitpoints");
            Assert.True(AttributeAdvancement.TrySpend(p, "Toughness", out _)); // 17 → 18: modifier 0 → 1
            Assert.AreEqual(before + 3, p.GetStat("Hitpoints").Max, "+1 for each of the three levels taken");
            Assert.AreEqual(current + 3, p.GetStatValue("Hitpoints"), "and the new health is usable now");
        }

        [Test] public void RaisingToughnessWithinAModifierStepAddsNoHealth()
        {
            var p = Player(toughness: 16, ap: 1); LevelTo(p, 4);
            int before = p.GetStat("Hitpoints").Max;
            Assert.True(AttributeAdvancement.TrySpend(p, "Toughness", out _)); // 16 → 17: modifier stays 0
            Assert.AreEqual(before, p.GetStat("Hitpoints").Max);
        }

        // ════════════════ the real player and the skills screen ════════════════

        [Test] public void ThePlayerStartsWithAnAttributePointStat()
        {
            using (var h = new ScenarioTestHarness())
            {
                var p = h.Factory.CreateEntity("Player");
                Assert.NotNull(p.GetStat("AP")); Assert.AreEqual(0, p.GetStatValue("AP"));
                foreach (var a in Attributes) Assert.NotNull(p.GetStat(a), a);
            }
        }

        [Test] public void TheSkillsScreenOffersAttributesFirstAndSpendsAP()
        {
            var p = Player(ap: 1);
            var model = new SkillBrowserModel(); model.Open(p);
            Assert.AreEqual(AttributeAdvancement.DisciplineClass, model.Disciplines[0].Class);
            Assert.AreEqual(6, model.Disciplines[0].Rows.Count);
            Assert.AreEqual("AP", model.Unit); Assert.AreEqual(1, model.Points);
            Assert.True(model.SelectClass(AttributeAdvancement.RowClass("Willpower")));
            Assert.True(model.ReviewPurchase()); Assert.True(model.ConfirmPurchase());
            Assert.AreEqual(17, p.GetStatValue("Willpower")); Assert.AreEqual(0, model.Points);
            Assert.False(model.ReviewPurchase(), "counter-check: no point left to review");
        }
    }
}
