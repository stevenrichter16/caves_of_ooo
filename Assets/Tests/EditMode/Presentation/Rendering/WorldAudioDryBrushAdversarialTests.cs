using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioDryBrushAdversarialTests : WorldAudioDryBrushFixture
    {
        [TestCase("hidden")][TestCase("unexplored")][TestCase("removed")][TestCase("render")]
        [TestCase("inventory")][TestCase("equipped")][TestCase("portable")]
        [TestCase("hp")][TestCase("gone")][TestCase("foreign-destructible")]
        [TestCase("material")][TestCase("foreign-material")][TestCase("missing-material")]
        public void ExactVisibleFixedLivingDryMaterialMustRemainCurrent(string mutation)
        {
            var owner=Place("DryBrush");Start();int scans=Scene.ScanCount;
            if(mutation=="hidden")Zone.GetEntityCell(owner).IsVisible=false;if(mutation=="unexplored")Zone.GetEntityCell(owner).Explored=false;
            if(mutation=="removed")Zone.RemoveEntity(owner);if(mutation=="render")owner.GetPart<RenderPart>().Visible=false;
            if(mutation=="inventory")owner.GetPart<PhysicsPart>().InInventory=Actor;if(mutation=="equipped")owner.GetPart<PhysicsPart>().Equipped=Actor;
            if(mutation=="portable")owner.GetPart<PhysicsPart>().Takeable=true;if(mutation=="hp")owner.GetPart<DestructiblePart>().HP=0;
            if(mutation=="gone")owner.GetPart<DestructiblePart>().Gone=true;if(mutation=="foreign-destructible")owner.GetPart<DestructiblePart>().ParentEntity=Actor;
            if(mutation=="material")owner.GetPart<MaterialPart>().MaterialID="Steel";if(mutation=="foreign-material")owner.GetPart<MaterialPart>().ParentEntity=Actor;
            if(mutation=="missing-material")owner.RemovePart(owner.GetPart<MaterialPart>());
            Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [TestCase("interior")][TestCase("depth")][TestCase("cave")]
        public void StandingBrushStillRequiresTheExistingOutdoorPolicy(string context)
        {Place("DryBrush");if(context=="interior")Zone.GetEntityCell(Actor).IsInterior=Zone.GetCell(11,10).IsInterior=true;Observe(context=="cave"?BiomeType.Cave:BiomeType.Beating,context=="depth"?1:0);Assert.AreEqual(0,Scene.SourceCount);}
        [TestCase("Grass")][TestCase("FireMoss")][TestCase("unregistered brush")]
        public void DryPlantMaterialAndVegetationTagAloneNeverInventAnApprovedOwner(string blueprint)
        {var owner=Place("DryBrush");owner.BlueprintName=blueprint;Observe();Assert.AreEqual(0,Scene.SourceCount);owner.BlueprintName="DryBrush";Start();}
        [Test] public void HiddenNewBrushIsNotDiscoveredByIdleWhenItBecomesVisible()
        {var owner=Place("DryBrush");Zone.GetEntityCell(owner).IsVisible=false;Observe();int scans=Scene.ScanCount;Zone.GetEntityCell(owner).IsVisible=true;Scene.Tick(30,true,false);Assert.AreEqual(0,Audio.AcceptedDetailCount);Assert.AreEqual(scans,Scene.ScanCount);Start();}
        [Test] public void EmptyDryRegionNeverInventsBrushFromBiomeOrDryWindBed()
        {Observe();for(int i=0;i<60;i++)Scene.Tick(1,true,false);Assert.AreEqual("dry-wind",Scene.AmbienceId);Assert.AreEqual(0,Audio.AcceptedDetailCount);}
        [Test] public void MutedBrushDoesNotQueueAnImmediateResumeBurst()
        {Place("DryBrush");Observe();CavesOfOoo.Rendering.WorldAudioSettings.AmbienceVolume=0;Scene.Tick(30,true,false);Assert.AreEqual(0,Audio.AcceptedDetailCount);CavesOfOoo.Rendering.WorldAudioSettings.AmbienceVolume=1;Scene.Tick(1,true,false);Assert.AreEqual(0,Audio.AcceptedDetailCount);Scene.Tick(10,true,false);Assert.AreEqual(1,Audio.AcceptedDetailCount);}
    }
}
