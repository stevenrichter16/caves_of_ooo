using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    /// <summary>Actual cached door meshes, exact native owners, and ordinary live state changes.
    /// Run natively: the standalone runner cannot prove Unity model and voxel bindings.</summary>
    public sealed class DensityGeneratedDoorRenderingTests
    {
        const string ZoneID="Overworld.4.6.0";
        static SpawnRing3DLibrary Library=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
        static Entity Door(Zone zone,bool open,int quarter)
        {
            var owner=GrovelandsCompositionTests.Factory().CreateEntity("VillageDoor");Assert.NotNull(owner,"reviewed real blueprint");
            owner.GetPart<DoorPart>().IsOpen=open;owner.GetPart<DoorPart>().QuarterTurns=quarter;
            owner.GetPart<RenderPart>().RenderString=open?"/":"+";Assert.True(zone.AddEntity(owner,20,10));return owner;
        }
        [TestCase(false,0)][TestCase(false,1)][TestCase(false,2)][TestCase(false,3)]
        [TestCase(true,0)][TestCase(true,1)][TestCase(true,2)][TestCase(true,3)]
        public void ExactNativeDoorUsesItsSavedStateAndAxisWithoutMutatingAnything(bool open,int quarter)
        {
            var zone=new Zone(ZoneID);var owner=Door(zone,open,quarter);int version=zone.EntityVersion;string tiles=zone.TileState.ToSaveString();
            var recipe=SpawnRing3DRecipes.Resolve(zone,owner,Library.Definition);
            Assert.NotNull(recipe.ModelId,recipe.Failure);StringAssert.StartsWith(open?"stillleaf-open-door-":"stillleaf-door-",recipe.ModelId);
            Assert.AreSame(owner,recipe.Owner);Assert.False(recipe.Batched);Assert.False(recipe.Transient);Assert.AreEqual(quarter,recipe.QuarterTurns);
            Assert.AreEqual(Village3DProjection.CellCentre(20,10),recipe.Position);
            Assert.NotNull(Library.FindModel(recipe.ModelId));Assert.AreSame(Library.FindModel(recipe.ModelId),Library.FindModel(recipe.ModelId));
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(tiles,zone.TileState.ToSaveString());Assert.AreEqual(open,owner.GetPart<DoorPart>().IsOpen);Assert.AreEqual(quarter,owner.GetPart<DoorPart>().QuarterTurns);
        }
        [TestCase("missing-part")][TestCase("hidden")][TestCase("removed")][TestCase("foreign")]
        [TestCase("takeable")][TestCase("carried")][TestCase("equipped")][TestCase("solid")]
        [TestCase("creature")][TestCase("authored-hybrid")][TestCase("archive-hybrid")]
        [TestCase("glyph")][TestCase("visual-id")][TestCase("invalid-quarter")][TestCase("footprint")]
        public void InvalidOwnerNeverReceivesTheOrdinaryDoorModel(string fault)
        {
            var zone=new Zone(ZoneID);var owner=Door(zone,true,0);var physics=owner.GetPart<PhysicsPart>();var part=owner.GetPart<DoorPart>();
            // The exact same valid fixture is the positive state/axis matrix above.
            if(fault=="missing-part")owner.RemovePart(part);
            if(fault=="hidden")owner.GetPart<RenderPart>().Visible=false;
            if(fault=="removed")zone.RemoveEntity(owner);
            if(fault=="foreign"){zone.RemoveEntity(owner);new Zone("foreign").AddEntity(owner,20,10);}
            if(fault=="takeable")physics.Takeable=true;
            if(fault=="carried")physics.InInventory=new Entity();
            if(fault=="equipped")physics.Equipped=new Entity();
            if(fault=="solid")physics.Solid=true;
            if(fault=="creature")owner.Tags["Creature"]="";
            if(fault=="authored-hybrid")owner.AddPart(new MorrowfastDoorPart());
            if(fault=="archive-hybrid")owner.AddPart(new SealedLibraryBarrierPart());
            if(fault=="glyph")owner.GetPart<RenderPart>().RenderString="?";
            if(fault=="visual-id")owner.GetPart<RenderPart>().VisualID="some-other-art";
            if(fault=="invalid-quarter")part.QuarterTurns=4;
            if(fault=="footprint")owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,Library.Definition).ModelId,fault);
        }
        [Test]public void AnUnrelatedBlueprintCannotClaimTheDoorMeshThroughMatchingParts()
        {
            var zone=new Zone(ZoneID);var owner=Door(zone,true,0);
            owner.BlueprintName="UnrelatedDoorLookalike";Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,owner,Library.Definition).ModelId);
        }
        [TestCase(0)][TestCase(1)]
        public void RealCloseAndOpenUpdateVisibleModelAndKeepNativeOwnerOutsideStillleaf(int quarter)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ZoneID))
            {
                f.Zone=new Zone(ZoneID);var owner=Door(f.Zone,true,quarter);f.Player=f.Factory.CreateEntity("Player");Assert.True(f.Zone.AddEntity(f.Player,20,11));
                f.Bind(f.Zone);f.Set("FullReveal",true);f.Refresh();int version=f.Zone.EntityVersion;
                Assert.True(f.Authored(owner));Assert.True(f.Rendered(owner));Assert.True(f.Find(owner,out var openView,out string openModel));StringAssert.StartsWith("stillleaf-open-door-",openModel);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0,quarter*90,0),openView.transform.localRotation),.01f);
                Assert.True(owner.GetPart<DoorPart>().TrySetOpen(f.Player,f.Zone,false));f.Refresh(f.Dirty(owner));
                Assert.True(f.Find(owner,out var closedView,out string closedModel));StringAssert.StartsWith("stillleaf-door-",closedModel);Assert.AreNotSame(openView,closedView);Assert.True(f.Rendered(owner));
                Assert.True(owner.GetPart<DoorPart>().TrySetOpen(f.Player,f.Zone,true));f.Refresh(f.Dirty(owner));
                Assert.True(f.Find(owner,out var again,out string againModel));Assert.AreEqual(openModel,againModel);Assert.True(f.Rendered(owner));
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreSame(owner,f.Zone.GetCell(20,10).Objects.Single());Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                f.Zone.RemoveEntity(owner);f.Refresh(SpawnRing3DIntegrationFixture.Dirty(20,10));Assert.False(f.Authored(owner));Assert.False(f.Find(owner,out _,out _));
            }
        }
        [TestCase(ZoneID)][TestCase("Overworld.2.5.0")]
        public void AllEightBorrowedDoorMeshesAreAlreadyNativeVoxelsAcrossSupportedZones(string zoneId)
        {
            bool old=Village3DSettings.Enabled;Village3DSettings.Enabled=true;
            try{
            var bridge=VoxelWorldPresentation.ForZone(new Zone(zoneId));Assert.NotNull(bridge);
            var kit=StillleafVoxelLibrary.Load();
            foreach(string family in new[]{"door","open-door"})for(int i=0;i<4;i++)
            {
                var id=StillleafVoxelLibrary.ModelId(family,i);var prefab=Library.FindModel(id);Assert.NotNull(prefab,id);
                var borrowed=kit.Find(id).Mesh;var owned=Object.Instantiate(prefab);
                try{Assert.AreSame(borrowed,owned.GetComponent<MeshFilter>().sharedMesh);bridge.Apply(owned);Assert.AreSame(borrowed,owned.GetComponent<MeshFilter>().sharedMesh);}
                finally{Object.DestroyImmediate(owned);}
            }
            Assert.AreEqual(0,bridge.MissingMeshCount);
            }finally{Village3DSettings.Enabled=old;}
        }
    }
}
