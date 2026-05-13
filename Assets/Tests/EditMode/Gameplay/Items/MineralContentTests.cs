using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.2 — content-validation pin for the three
    /// new mineral blueprints in Objects.json.
    ///
    /// <para>Each mineral is a takeable Item with a "Mineral" tag (so
    /// future code can filter "is this an enhancement substrate?")
    /// + a "Tier" tag (so Tinker recipes can match by tier) + a
    /// display name + Commerce value. This fixture pins the shape
    /// against accidental tag drops + display-name typos.</para>
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Each mineral instantiates via EntityFactory.</item>
    ///   <item>Has the "Item" tag (inherited from the Item blueprint).</item>
    ///   <item>Has the "Mineral" tag (the new discoverability gate).</item>
    ///   <item>Has the expected display name.</item>
    ///   <item>Has the expected Tier tag.</item>
    ///   <item>Has PhysicsPart with Takeable=true.</item>
    ///   <item>Has StackerPart so multiple stack in inventory.</item>
    /// </list>
    ///
    /// <para><b>Counter-check:</b> A canonical non-mineral item
    /// (e.g. <c>LongSword</c>) does NOT have the "Mineral" tag,
    /// preventing accidental tag-namespace collision.</para>
    /// </summary>
    public class MineralContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        // ── Shape pins per mineral ────────────────────────────────

        [TestCase("PaleSalt", "pale-salt", "2")]
        [TestCase("ChoirIron", "choir-iron ore", "3")]
        [TestCase("GlowQuartz", "glow-quartz", "2")]
        public void MineralBlueprint_HasItemAndMineralTags_AndExpectedDisplayName(
            string blueprint, string expectedDisplay, string expectedTier)
        {
            var e = _harness.Factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, $"Blueprint '{blueprint}' must be registered in Objects.json.");

            Assert.IsTrue(e.Tags.ContainsKey("Item"),
                "Inherits the 'Item' tag from the Item parent blueprint.");
            Assert.IsTrue(e.Tags.ContainsKey("Mineral"),
                "Has the 'Mineral' tag — the discoverability gate for " +
                "WantsMineralPart and Tinker enhancement-apply recipes.");
            Assert.AreEqual(expectedTier, e.Tags["Tier"],
                "Tier tag pins the substrate-quality used by Tinker recipes.");

            var render = e.GetPart<RenderPart>();
            Assert.IsNotNull(render);
            Assert.AreEqual(expectedDisplay, render.DisplayName,
                "Display name pins the player-facing string.");
        }

        [TestCase("PaleSalt")]
        [TestCase("ChoirIron")]
        [TestCase("GlowQuartz")]
        public void MineralBlueprint_IsTakeable_AndStacks(string blueprint)
        {
            var e = _harness.Factory.CreateEntity(blueprint);
            var physics = e.GetPart<PhysicsPart>();
            Assert.IsNotNull(physics, "Inherits PhysicsPart from Item.");
            Assert.IsTrue(physics.Takeable,
                "Minerals must be takeable — pickup is the player's only " +
                "acquisition path.");

            var stacker = e.GetPart<StackerPart>();
            Assert.IsNotNull(stacker,
                "Inherits StackerPart from Item — so the player can carry " +
                "stacks of 3+ pale-salt in one slot.");
        }

        [TestCase("PaleSalt", 12)]
        [TestCase("ChoirIron", 18)]
        [TestCase("GlowQuartz", 15)]
        public void MineralBlueprint_HasCommerceValue(string blueprint, int expectedValue)
        {
            var e = _harness.Factory.CreateEntity(blueprint);
            var commerce = e.GetPart<CommercePart>();
            Assert.IsNotNull(commerce,
                "Commerce Part pins NPC trade value (vendors + WantsMineralPart bonus).");
            Assert.AreEqual(expectedValue, commerce.Value,
                $"Commerce value pins the {blueprint} trade-floor — tuning lockdown.");
        }

        // ── Counter-check: non-mineral item must NOT have Mineral tag ──

        [Test]
        public void NonMineralItem_DoesNotHaveMineralTag()
        {
            // Counter-check pair to the +Mineral-tag tests. Without this
            // a future change accidentally tagging "Item" parents with
            // Mineral would pass the positive tests and silently corrupt
            // discoverability.
            var sword = _harness.Factory.CreateEntity("LongSword");
            Assert.IsNotNull(sword);
            Assert.IsTrue(sword.Tags.ContainsKey("Item"),
                "Precondition: LongSword is an Item.");
            Assert.IsFalse(sword.Tags.ContainsKey("Mineral"),
                "A weapon is NOT a mineral — Mineral tag must stay narrow.");
        }
    }
}
