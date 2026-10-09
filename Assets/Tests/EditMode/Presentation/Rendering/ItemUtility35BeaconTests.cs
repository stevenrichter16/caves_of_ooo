using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35BeaconTests
    {
        [TestCase(0)] [TestCase(1)]
        public void CrackedQuartzHasItsOwnFiniteGroundLightModel(int variant)
        {
            using (var f = new DensityLootTestScope())
            {
                var owner = f.Factory.CreateEntity("CrackedGlowQuartz");
                Assert.NotNull(owner);
                Assert.NotNull(owner.GetPart<LifespanPart>());
                Assert.True(SpreadSceneryRecipes.TryModel(owner, variant, out var model));
                Assert.AreEqual("spread-scenery-crackedglowquartz-" + variant, model);
                Assert.True(SpreadScenerySource.IsModelId(model));
                Assert.NotNull(SpreadScenery3DLibrary.Load().Find(model));
                owner.GetPart<PhysicsPart>().Takeable = true;
                Assert.False(SpreadSceneryRecipes.TryModel(owner, variant, out _));
            }
        }
        [Test] public void LiveGroundLightHasNativeGeometryThenLeavesWhenConsumed()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("CrackedGlowQuartz", 20, 10); f.Refresh();
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var proof), proof.Failure);
                StringAssert.StartsWith("spread-scenery-crackedglowquartz-", proof.ModelId);
                Assert.True(f.Zone.RemoveEntity(owner)); f.Refresh(); Assert.False(f.Rendered(owner));
            }
        }
    }
}
