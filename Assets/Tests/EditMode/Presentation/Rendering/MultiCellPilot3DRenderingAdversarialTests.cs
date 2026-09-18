using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Player-flow hypotheses: classify initial results as confirmed bugs or pins.
    public sealed class MultiCellPilot3DRenderingAdversarialTests
    {
        const string Pilot = MultiCellPilotRuntime.ZoneID;
        static Entity Owner(Zone zone,string id) => zone.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId==id);
        static GameObject View(SpawnRing3DIntegrationFixture f,Entity owner)
        {Assert.IsTrue(f.Find(owner,out var root,out var id),owner.GetPart<MultiCellPilotPropPart>().OwnerId);Assert.AreEqual(owner.GetPart<MultiCellPilotPropPart>().ModelId,id);return root;}
        static void Hide(Zone z){for(int y=0;y<25;y++)for(int x=0;x<80;x++)z.GetCell(x,y).IsVisible=z.GetCell(x,y).Explored=false;}
        static (int x,int y) Free(Zone zone,Entity owner)
        {for(int y=1;y<23;y++)for(int x=1;x<77;x++)if(zone.CanPlaceFootprint(owner,x,y))return(x,y);Assert.Fail("No complete destination body fits");return(-1,-1);}
        static bool Pick(SpawnRing3DIntegrationFixture f,int x,int y,out Entity owner)
        {Physics.SyncTransforms();object[] args={new Vector2(x+.5f,24.5f-y),null,0,0};bool ok=(bool)f.Call("TryPickWorld",args);owner=args[1]as Entity;return ok;}

        [Test] public void All149OwnersRemainIndependentWithoutNativeWritesOnRepeatedRefresh()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var owners=f.Zone.GetReadOnlyEntities().Where(e=>e.HasPart<MultiCellPilotPropPart>()).ToArray();Assert.AreEqual(149,owners.Length);
                var roots=owners.Select(e=>View(f,e)).ToArray();Assert.AreEqual(149,roots.Distinct().Count());
                int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();var positions=owners.Select(f.Zone.GetEntityPosition).ToArray();
                for(int i=0;i<3;i++)f.Refresh();
                CollectionAssert.AreEqual(roots,owners.Select(e=>View(f,e)));CollectionAssert.AreEqual(positions,owners.Select(f.Zone.GetEntityPosition));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
            }
        }
        [TestCase("large-maw-toad")][TestCase("movable-copper-pipe")]
        public void PortableOwnerRetainsExact3DBodyAfterNativeTransferToAdjacentRing(string id)
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var e=Owner(f.Zone,id);var old=View(f,e);var source=f.Zone;var destination=f.Manager.GetZone("Overworld.2.7.0");var at=Free(destination,e);
                Assert.IsTrue(source.TryTransferEntityTo(e,destination,at.x,at.y));f.Refresh();Assert.IsFalse(f.Find(e,out _,out _));SpawnRing3DIntegrationFixture.Hidden(old);
                f.Zone=destination;f.Manager.SetActiveZone(destination);f.Reveal();f.Bind(destination);f.Refresh();Assert.IsTrue(f.Rendered(e),"Portable native owner lost its authored whole-body art at the chunk border.");View(f,e);
                Assert.IsFalse(MultiCellPilotRuntime.IsActive(destination),"Presentation must not install pilot content into its neighbour.");
            }
        }
        [TestCase("large-maw-toad")][TestCase("movable-copper-pipe")]
        public void PortableOwnerArrivingInAlreadyBoundTownHasOneBodyAndLeavesNoViewOnReturn(string id)
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var source=f.Manager.GetZone(Pilot);var e=Owner(source,id);var at=Free(f.Zone,e);int count=f.Zone.GetReadOnlyEntities().Count;
                Assert.IsTrue(source.TryTransferEntityTo(e,f.Zone,at.x,at.y));f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(e),"Town must render arriving multi-cell owners without a scene rebind.");
                Assert.IsTrue(f.Presenter.TryGetOwnerView(id,out var bound,out var root));Assert.AreSame(e,bound);Assert.AreEqual(count+1,f.Zone.GetReadOnlyEntities().Count);
                Assert.AreEqual(1,f.Root.GetComponentsInChildren<Transform>(true).Count(t=>t.gameObject==root));
                Hide(f.Zone);var edge=f.Zone.GetOccupiedCells(e).Last();edge.IsVisible=edge.Explored=true;f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(e));
                Physics.SyncTransforms();Assert.IsTrue(f.Presenter.TryPickWorld(new Vector2(edge.X+.5f,24.5f-edge.Y),out var picked,out int x,out int y));Assert.AreSame(e,picked);Assert.AreEqual(edge.X,x);Assert.AreEqual(edge.Y,y);
                edge.IsVisible=false;f.Refresh();Assert.IsFalse(f.Presenter.TryPickWorld(new Vector2(edge.X+.5f,24.5f-edge.Y),out _,out _,out _));
                var back=Free(source,e);Assert.IsTrue(f.Zone.TryTransferEntityTo(e,source,back.x,back.y));f.Refresh();Assert.IsFalse(f.Presenter.IsAuthoredEntity(e));Assert.IsFalse(f.Presenter.TryGetOwnerView(id,out _,out _));Village3DIntegrationFixture.Hidden(root);
            }
        }
        [Test] public void InvalidModelMutationReleasesOnlyThatOwnerAndRestorationRebuildsIt()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var e=Owner(f.Zone,"PilotRidgeN-37-11");var neighbour=Owner(f.Zone,"PilotRidgeN-37-15");var old=View(f,e);var stable=View(f,neighbour);var prop=e.GetPart<MultiCellPilotPropPart>();string model=prop.ModelId;
                prop.ModelId="missing-pilot-model";f.Refresh();Assert.IsFalse(f.Authored(e));SpawnRing3DIntegrationFixture.Hidden(old);Assert.AreSame(stable,View(f,neighbour));
                Assert.IsFalse(Pick(f,38,13,out _),"An unrepresented native owner must retain native picking priority.");
                prop.ModelId=model;f.Refresh();Assert.IsTrue(f.Rendered(e));Assert.AreNotSame(old,View(f,e));
            }
        }
        [Test] public void NativeRenderHiddenReleasesBodyAndPickingDespiteVisibleCells()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var e=Owner(f.Zone,"PilotRidgeN-37-11");var old=View(f,e);e.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.IsFalse(f.Authored(e));Assert.IsFalse(f.Rendered(e));SpawnRing3DIntegrationFixture.Hidden(old);
                Pick(f,38,13,out var selected);Assert.AreNotSame(e,selected);e.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.IsTrue(f.Rendered(e));
            }
        }
        [Test] public void SharedPaletteKeepsIndependentActorFogPolicyAndPerCellVisibilityTexture()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var actor=Owner(f.Zone,"large-maw-toad");var ridge=Owner(f.Zone,"PilotRidgeN-37-11");var a=View(f,actor).GetComponentInChildren<Renderer>();var b=View(f,ridge).GetComponentInChildren<Renderer>();Assert.AreSame(a.sharedMaterial,b.sharedMaterial);
                var block=new MaterialPropertyBlock();a.GetPropertyBlock(block);Assert.AreEqual(1,block.GetFloat("_Transient"));block.Clear();b.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_Transient"));
                Hide(f.Zone);f.Zone.GetCell(11,3).Explored=f.Zone.GetCell(11,3).IsVisible=true;f.Zone.GetCell(38,13).Explored=true;f.Refresh();Assert.IsTrue(f.Rendered(actor));Assert.IsTrue(f.Rendered(ridge));
                var fog=(Texture2D)a.sharedMaterial.GetTexture("_FogLight");Assert.AreNotEqual(fog.GetPixel(11,21),fog.GetPixel(10,22));Assert.AreNotEqual(fog.GetPixel(38,11),fog.GetPixel(37,13));
                f.Zone.GetCell(11,3).IsVisible=false;f.Refresh();Assert.IsFalse(f.Rendered(actor));Assert.IsTrue(f.Rendered(ridge),"Static memory may remain while living-body memory must not.");
            }
        }
        [Test] public void DisablingPresentationReleasesClaimsAndPickingThenReusesTheSameOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var e=Owner(f.Zone,"PilotRidgeN-37-11");var root=View(f,e);f.Call("SetPresentationVisible",false);Assert.IsFalse(f.Rendered(e));Assert.IsFalse((bool)f.Call("ClaimsCell",38,13));Assert.IsFalse(Pick(f,38,13,out _));SpawnRing3DIntegrationFixture.Hidden(root);
                f.Call("SetPresentationVisible",true);f.Refresh();Assert.AreSame(root,View(f,e));Assert.IsTrue(Pick(f,38,13,out var selected));Assert.AreSame(e,selected);
            }
        }
        [Test] public void SurfaceDisposalDestroysPilotMaterialClonesButPreservesBorrowedAssets()
        {
            var f=new SpawnRing3DIntegrationFixture(Pilot);Material[] clones=null;RenderTexture target=null;Texture fog=null;
            var library=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);var palette=library.WorldMaterial;var texture=palette.GetTexture("_BaseMap");var prefab=library.FindModel("PilotMawToad");
            try{clones=f.Root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();Assert.IsTrue(clones.All(m=>m!=palette));target=f.Get<Camera>("WorldCamera").targetTexture;fog=View(f,Owner(f.Zone,"large-maw-toad")).GetComponentInChildren<Renderer>().sharedMaterial.GetTexture("_FogLight");}
            finally{f.Dispose();}
            Assert.IsTrue(clones.All(m=>m==null),"Owned material clones leaked past surface disposal.");Assert.IsTrue(target==null);Assert.IsTrue(fog==null);Assert.IsTrue(palette!=null&&texture!=null&&prefab!=null);Assert.AreSame(texture,palette.GetTexture("_BaseMap"));
        }
        [Test] public void ChangingNativeBodyContractFallsBackUntilTheAuthoredShapeReturns()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var e=Owner(f.Zone,"large-maw-toad");string original=e.GetPart<SpatialFootprintPart>().CellsRaw;var root=View(f,e);
                Assert.IsTrue(f.Zone.TryChangeFootprint(e,"0,0;1,0"));f.Refresh();Assert.IsFalse(f.Authored(e));SpawnRing3DIntegrationFixture.Hidden(root);
                Assert.IsTrue(f.Zone.TryChangeFootprint(e,original));f.Refresh();Assert.IsTrue(f.Rendered(e));View(f,e);
            }
        }
    }
}
