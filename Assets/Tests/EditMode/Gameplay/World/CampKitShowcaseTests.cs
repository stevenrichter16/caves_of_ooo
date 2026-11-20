using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The "Camp Kits" scenario is a hand-play fixture, so what it must not do is quietly rot: a renamed blueprint would hand the player nothing (the builder only logs
    /// and skips), and a pack too heavy to hold the kits would drop the later ones without a word. These pin the loadout against the real content.
    /// </summary>
    public sealed class CampKitShowcaseTests : FiftyWorldFixture
    {
        static readonly string[] Kits = { "CampBox", "CampHearth", "CampBench", "CampStill" };

        [SetUp] public void SetupShowcase() => PackableFixtureRegistry.ResetForTests();
        [TearDown] public void TearDownShowcase() => PackableFixtureRegistry.ResetForTests();

        [Test] public void EveryLoadoutBlueprintExistsInTheRealContent()
        {
            foreach (var (blueprint, count) in CampKitsShowcase.Loadout)
            { Assert.NotNull(Factory.CreateEntity(blueprint), blueprint + " must be a real blueprint"); Assert.Greater(count, 0, blueprint); }
            Assert.NotNull(Factory.CreateEntity(CampKitsShowcase.FireBlueprint), "the fire the torch is lit at");
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NoSuchBlueprintForTheShowcase'");
            Assert.IsNull(Factory.CreateEntity("NoSuchBlueprintForTheShowcase"), "counter: the check is real, an unknown name is null");
        }

        [Test] public void AllFourKitsAreGivenAndEachIsAShippedPackableKind()
        {
            foreach (var kit in Kits)
            {
                Assert.True(CampKitsShowcase.Loadout.Any(l => l.Blueprint == kit && l.Count == 1), kit + " is in the loadout exactly once");
                var part = Factory.CreateEntity(kit).GetPart<PackableFixturePart>();
                Assert.NotNull(part, kit + " is a packable fixture");
                Assert.NotNull(PackableFixtureRegistry.Get(part.Kind), kit + " has a registered kind, so set down finds a definition");
            }
        }

        [Test] public void TheLoadoutIsLightEnoughForTheSmallestPlayerToCarry()
        {
            int weight = CampKitsShowcase.Loadout.Sum(l => (Factory.CreateEntity(l.Blueprint).GetPart<PhysicsPart>()?.Weight ?? 0) * l.Count);
            Assert.LessOrEqual(weight, 240, "Strength 16 carries 240; a heavier fixture would silently drop its last items");
            Assert.GreaterOrEqual(weight, 66, "counter: the four kits alone weigh 66, so a zero here means the weight read is broken");
        }

        [Test] public void TheLoadoutCarriesWhatEachKitNeedsToBeTried()
        {
            bool Has(string bp, int n = 1) => CampKitsShowcase.Loadout.Any(l => l.Blueprint == bp && l.Count >= n);
            Assert.True(Has("SalvagedTimber", 4), "hearth fuel (one timber is ten fuel)");
            Assert.True(Has("Torch"), "the carried flame that relights the hearth");
            Assert.True(Has("IronSpikeComponent") && Has("OakHaftComponent") && Has("LeatherBindingComponent"), "a craftable set for the bench");
            Assert.True(Has("MendleafSprig", 2) && Has("FireMoss"), "reagents for the still");
            Assert.True(Has("Dagger"), "something to put in the box");
            Assert.True(Has("FlintStriker"), "a flame where none burns (the spark path)");
        }
    }
}
