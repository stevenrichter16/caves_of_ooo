using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.Rendering.Universal;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadEffectiveShadowBiasTests
 {
  private static NativeZone3DRenderSurface Surface(SpawnRing3DIntegrationFixture f)=>((SpawnRing3DPresenter)f.Presenter).ActiveSurface;
  [TestCase("Overworld.11.10.0")][TestCase("Overworld.12.10.0")][TestCase("Overworld.10.10.0")]
  public void ActualApprovedProfileUsesItsOwnedShadowBiasRatherThanPipelineDefaults(string zone)
  {
   using(var f=new SpawnRing3DIntegrationFixture(zone)){
    Assert.True(SpreadPresentationScope.IsActive(f.Zone));f.Refresh();var surface=Surface(f);
    Assert.False(surface.Sun.GetComponent<UniversalAdditionalLightData>().usePipelineSettings,"URP otherwise discards the configured local fields.");
    Assert.AreEqual(.008f,surface.Sun.shadowBias);Assert.AreEqual(.018f,surface.Sun.shadowNormalBias);
   }
  }
  [Test] public void OrdinaryForeignProfileKeepsInheritedPipelineShadowPolicy()
  {
   using(var f=new SpawnRing3DIntegrationFixture()){
    Assert.False(SpreadPresentationScope.IsActive(f.Zone));f.Refresh();Assert.True(Surface(f).Sun.GetComponent<UniversalAdditionalLightData>().usePipelineSettings);
   }
  }
  [Test] public void AuthorityLossAndReturnRebuildOnlyTheOwnedLightPolicy()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){
    f.Refresh();var before=Surface(f);Assert.False(before.Sun.GetComponent<UniversalAdditionalLightData>().usePipelineSettings);
    f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;f.Refresh();var foreign=Surface(f);
    Assert.AreNotSame(before,foreign);Assert.True(foreign.Sun.GetComponent<UniversalAdditionalLightData>().usePipelineSettings);
    f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;f.Refresh();var restored=Surface(f);
    Assert.AreNotSame(foreign,restored);Assert.False(restored.Sun.GetComponent<UniversalAdditionalLightData>().usePipelineSettings);
   }
  }
 }
}
