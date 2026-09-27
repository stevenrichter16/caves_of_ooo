using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadSceneryAdversarialTests
    {
        private DensityLootTestScope scope;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); }
        [TearDown] public void Teardown() { scope.Dispose(); }
        private sealed class NamedImpostor : Part
        {
            private readonly string name;
            public NamedImpostor(string name) { this.name = name; }
            public override string Name => name;
        }
        // A part's string registration name is not its gameplay type contract.
        [TestCase("BerryBush", "Harvestable")]
        [TestCase("Signpost", "RegionalSignpost")]
        [TestCase("RiverShrine", "Sanctuary")]
        [TestCase("FlowerField", "FlowerCharm")]
        [TestCase("Chair", "Chair")]
        [TestCase("Bed", "Bed")]
        [TestCase("Well", "Well")]
        [TestCase("AlchemyShelf", "Container")]
        [TestCase("AlchemyStill", "AlchemyStill")]
        [TestCase("TinkersForge", "Forge")]
        [TestCase("PressurePlate", "PressurePlateTriggerPart")]
        [TestCase("BearTrap", "BearTrapTriggerPart")]
        [TestCase("FireTrap", "FireTrapTriggerPart")]
        [TestCase("SpikeTrap", "SpikeTrapTriggerPart")]
        public void MatchingStringPartNameCannotImpersonateNativeContract(string blueprint, string name)
        {
            var owner = scope.Factory.CreateEntity(blueprint);
            Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out _));
            Assert.True(owner.RemovePart(owner.GetPart(name)));
            owner.AddPart(new NamedImpostor(name));
            Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out var model));
            Assert.Null(model);
        }
        [TestCase("missing")][TestCase("foreign")]
        public void RequiredPartMustStillBelongToThisOwner(string change)
        {
            var owner = scope.Factory.CreateEntity("Chair");
            Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out _));
            var part = owner.GetPart<ChairPart>();
            if (change == "missing") owner.RemovePart(part); else part.ParentEntity = new Entity();
            Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out _));
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)][TestCase(-1f)]
        public void InvalidLanternLightCannotClaimEitherState(float intensity)
        {
            var owner = scope.Factory.CreateEntity("WatchLantern");
            Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out _));
            owner.GetPart<LightSourcePart>().Intensity = intensity;
            Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out _));
        }
        [TestCase("footprint")][TestCase("multicell")][TestCase("solid")][TestCase("solid-tag")]
        public void ChangedGeometryContractDoesNotClaimSingleCellModel(string change)
        {
            var owner = scope.Factory.CreateEntity("BerryBush");
            Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out _));
            if (change == "footprint") owner.AddPart(new SpatialFootprintPart());
            else if (change == "multicell") owner.AddPart(new MultiCellPilotPropPart());
            else if (change == "solid-tag") owner.Tags["Solid"] = "";
            else owner.GetPart<PhysicsPart>().Solid = true;
            Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out _));
        }
    }
}
