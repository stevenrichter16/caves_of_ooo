using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioRoomBoundaryAdversarialTests : WorldAudioRoomBoundaryFixture
    {
        [TestCase("hidden")][TestCase("unexplored")][TestCase("render")][TestCase("removed")]
        [TestCase("inventory")][TestCase("equipped")][TestCase("portable")][TestCase("foreign-material")]
        [TestCase("foreign-physics")][TestCase("foreign-render")][TestCase("destroyed")][TestCase("not-structure")]
        public void EnclosureExceptionKeepsExactCurrentOwnedVisibleFixedProof(string mutation)
        {
            var owner=Structure();Start("wood-settle");int scans=Scene.ScanCount;
            if(mutation=="hidden")Zone.GetEntityCell(owner).IsVisible=false;
            if(mutation=="unexplored")Zone.GetEntityCell(owner).Explored=false;
            if(mutation=="render")owner.GetPart<RenderPart>().Visible=false;
            if(mutation=="removed")Zone.RemoveEntity(owner);
            if(mutation=="inventory")owner.GetPart<PhysicsPart>().InInventory=Actor;
            if(mutation=="equipped")owner.GetPart<PhysicsPart>().Equipped=Actor;
            if(mutation=="portable")owner.GetPart<PhysicsPart>().Takeable=true;
            if(mutation=="foreign-material")owner.GetPart<MaterialPart>().ParentEntity=Actor;
            if(mutation=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=Actor;
            if(mutation=="foreign-render")owner.GetPart<RenderPart>().ParentEntity=Actor;
            if(mutation=="destroyed")owner.AddPart(new DestructiblePart{HP=0,MaxHP=10});
            if(mutation=="not-structure")owner.Tags.Remove("Wall");
            Scene.Tick(.01f,true,false);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [TestCase(11,11)][TestCase(12,10)][TestCase(10,12)]
        public void DistantOrDiagonalOtherRoomBoundaryCannotCrossShelter(int x,int y)
        {Structure(x:x,y:y);Observe();Assert.AreEqual(0,Scene.SourceCount);}
        [TestCase("animal")][TestCase("water")][TestCase("tree")][TestCase("fire")]
        public void OutsideNeighborCannotBorrowAnEnclosingWallFlagForAnotherSound(string kind)
        {
            var owner=Structure();
            if(kind=="animal"){owner.BlueprintName="Bandfrog";owner.SetTag("Creature");owner.AddPart(new BrainPart());owner.Statistics["Hitpoints"]=new Stat{Owner=owner,Name="Hitpoints",BaseValue=10,Max=10};}
            if(kind=="water")owner.AddPart(new LiquidPoolPart{Volume=3,LiquidId="water"});
            if(kind=="tree"){owner.Tags.Remove("Wall");owner.BlueprintName="Tree";}
            if(kind=="fire"){owner.AddPart(new CampfirePart());owner.AddPart(new FuelPart{FuelMass=20});owner.AddPart(new ThermalPart{Temperature=600,FlameTemperature=500});}
            Observe();Assert.AreEqual(0,Scene.SourceCount);Scene.Tick(30,true,false);Assert.AreEqual(0,Audio.AcceptedDetailCount);
        }
        [Test] public void ExteriorListenerCannotHearAnInteriorThroughTheReverseException()
        {var owner=Structure();Zone.GetEntityCell(owner).IsInterior=true;Zone.GetEntityCell(Actor).IsInterior=false;Observe();Assert.AreEqual(0,Scene.SourceCount);}
        [Test] public void StaleExteriorSnapshotDoesNotRevealNewBoundaryUntilObserved()
        {Observe();int scans=Scene.ScanCount;Structure();Scene.Tick(30,true,false);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(scans,Scene.ScanCount);Start("wood-settle");}
        [TestCase("Steel")][TestCase("Plant")]
        public void UnsupportedBoundaryMaterialsStayQuiet(string material)
        {Structure(material);Observe();Assert.AreEqual(0,Scene.SourceCount);}
        [Test] public void AForeignDoorPartCannotQualifyTheBoundary()
        {var owner=Structure(door:true);owner.GetPart<DoorPart>().ParentEntity=Actor;Observe();Assert.AreEqual(0,Scene.SourceCount);owner.GetPart<DoorPart>().ParentEntity=owner;Start("wood-settle");}
    }
}
