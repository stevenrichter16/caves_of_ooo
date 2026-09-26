using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Density Phase 1 (Docs/DENSITY-PHASE-1.md §T1.1). The three shipped
    /// gas grenades sit in 7 loot tables and 2 shops, but every one of
    /// them inherited plain <c>Item</c>, which carries no
    /// <see cref="HandlingPart"/>, so <see cref="HandlingService.IsThrowable"/>
    /// said no and the Throw action never appeared.
    ///
    /// User-visible invariant: "a gas grenade you find or buy can be
    /// thrown, and it bursts into gas where it lands."
    ///
    /// Why the existing suite missed it: <c>GasGrenadePartTests</c>
    /// hand-builds a grenade entity and calls <c>Detonate</c> directly,
    /// and <c>ThrowableTonicTests</c> hand-adds a HandlingPart before
    /// throwing. Nothing threw a FACTORY-BUILT grenade. Every test here
    /// does, and none adds a HandlingPart by hand.
    /// </summary>
    public class ShippedGrenadeThrowTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown() => GasRegistry.ResetForTests();

        [TestCase("PoisonGasGrenade")]
        [TestCase("SleepGasGrenade")]
        [TestCase("StunGasGrenade")]
        public void ShippedGrenade_IsThrowable(string blueprint)
        {
            var grenade = _harness.Factory.CreateEntity(blueprint);
            Assert.IsNotNull(grenade, blueprint + " must exist");
            Assert.IsNotNull(grenade.GetPart<GasGrenadePart>(),
                blueprint + " must still carry its gas payload");
            Assert.IsTrue(HandlingService.IsThrowable(grenade),
                blueprint + " is sold and dropped as a thrown weapon; it must be throwable");
        }

        [TestCase("PoisonGasGrenade")]
        [TestCase("SleepGasGrenade")]
        [TestCase("StunGasGrenade")]
        public void ShippedGrenade_KeepsItsAuthoredWeight(string blueprint)
        {
            // Adversarial: HandlingService.GetWeight prefers Handling.Weight
            // when it is > 0. The new Handling part leaves it at 0 so the
            // Physics weight (2) still governs carry load and throw range; a
            // future edit that sets Handling.Weight must do so deliberately.
            var grenade = _harness.Factory.CreateEntity(blueprint);
            Assert.AreEqual(2, HandlingService.GetWeight(grenade));
        }

        [Test]
        public void PlainItemWithoutHandling_IsStillNotThrowable()
        {
            // Counter-check: the fix belongs on the grenades, not on the
            // Item base. If someone "fixes" this by giving Item a Handling
            // part, every coin and scrap in the game becomes throwable and
            // this test goes red.
            var coin = _harness.Factory.CreateEntity("GoldCoin");
            Assert.IsNotNull(coin);
            Assert.IsNull(coin.GetPart<HandlingPart>(), "GoldCoin inherits bare Item");
            Assert.IsFalse(HandlingService.IsThrowable(coin));
        }

        [Test]
        public void ShippedPoisonGrenade_ThrownAtAnEmptyCell_BurstsIntoGas_AndIsUsedUp()
        {
            var ctx = _harness.CreateContext();
            var thrower = Thrower(ctx);
            var grenade = _harness.Factory.CreateEntity("PoisonGasGrenade");
            thrower.GetPart<InventoryPart>().AddObject(grenade);

            var result = InventorySystem.ExecuteCommand(
                new ThrowItemCommand(grenade, 13, 10), thrower, ctx.Zone);

            Assert.IsTrue(result.Success, "throw must succeed: " + result.ErrorMessage);
            Assert.Greater(ctx.Zone.GetEntitiesWithTag("Gas").Count, 0,
                "the grenade bursts into gas where it lands");
            Assert.IsNull(ctx.Zone.GetEntityCell(grenade),
                "a detonated grenade does not also lie on the ground");
            Assert.IsFalse(thrower.GetPart<InventoryPart>().Objects.Contains(grenade),
                "and it has left the thrower's pack");
        }

        [Test]
        public void ThrowingOneGrenadeFromAStackOfThree_LeavesTwo()
        {
            // Grenades inherit Item's Stacker, so loot hands them out in
            // stacks. A throw must spend one, not the stack.
            var ctx = _harness.CreateContext();
            var thrower = Thrower(ctx);
            var stack = _harness.Factory.CreateEntity("PoisonGasGrenade");
            stack.GetPart<StackerPart>().StackCount = 3;
            thrower.GetPart<InventoryPart>().AddObject(stack);

            var result = InventorySystem.ExecuteCommand(
                new ThrowItemCommand(stack, 13, 10), thrower, ctx.Zone);

            Assert.IsTrue(result.Success, "throw must succeed: " + result.ErrorMessage);
            Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(thrower.GetPart<InventoryPart>().Objects.Contains(stack));
            Assert.Greater(ctx.Zone.GetEntitiesWithTag("Gas").Count, 0);
        }

        [Test]
        public void ThrownDagger_LandsOnTheGround_AndReleasesNoGas()
        {
            // Counter-check for the integration test above: the gas comes
            // from the grenade's payload, not from the act of throwing.
            var ctx = _harness.CreateContext();
            var thrower = Thrower(ctx);
            var dagger = _harness.Factory.CreateEntity("Dagger");
            thrower.GetPart<InventoryPart>().AddObject(dagger);

            var result = InventorySystem.ExecuteCommand(
                new ThrowItemCommand(dagger, 13, 10), thrower, ctx.Zone);

            Assert.IsTrue(result.Success, "throw must succeed: " + result.ErrorMessage);
            Assert.AreEqual(0, ctx.Zone.GetEntitiesWithTag("Gas").Count);
            Assert.IsNotNull(ctx.Zone.GetEntityCell(dagger), "a dagger lands and stays");
        }

        private static Entity Thrower(ScenarioContext ctx)
        {
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var str = thrower.GetStat("Strength");
            if (str != null) str.BaseValue = 16;
            if (thrower.GetPart<InventoryPart>() == null)
                thrower.AddPart(new InventoryPart { MaxWeight = 50 });
            return thrower;
        }
    }
}
