using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MultiCellPilot3DRenderingTests
    {
        const string Pilot = "Overworld.3.7.0";
        static object Prop(Entity e) => e.Parts.FirstOrDefault(p => p.GetType().Name == "MultiCellPilotPropPart");
        static string Field(object p,string field) => (string)p.GetType().GetField(field).GetValue(p);
        static Entity Owner(SpawnRing3DIntegrationFixture f,string id)
        {
            var e=f.Zone.GetReadOnlyEntities().FirstOrDefault(e=>Prop(e)!=null&&Field(Prop(e),"OwnerId")==id);
            Assert.NotNull(e,"Authored native pilot owner missing: "+id);return e;
        }
        static GameObject View(SpawnRing3DIntegrationFixture f,Entity e)
        {Assert.IsTrue(f.Find(e,out var root,out string model));Assert.AreEqual(Field(Prop(e),"ModelId"),model);return root;}
        static void HideAll(Zone zone)
        {for(int y=0;y<25;y++)for(int x=0;x<80;x++)zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=false;}

        [Test] public void RealPilotLibraryExistsIndependentlyOfExistingRingLibrary()
        {
            var type=typeof(SpawnRing3DLibrary).Assembly.GetType("CavesOfOoo.Rendering.MultiCellPilot3DLibrary");
            Assert.NotNull(type,"Missing pilot-library RED must precede rendering implementation.");
            var pilot=Resources.Load("MultiCellPilot3D/Library",type);
            Assert.NotNull(pilot,"The 51 real FBX exports must be imported before rendering GREEN.");
            var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            Assert.NotNull(ring);Assert.AreNotSame(ring,pilot);ring.Validate();
            type.GetMethod("Validate").Invoke(pilot,null);
        }
        [Test] public void LargeSceneryAndCreatureBindToIndependentNativeOwnerViews()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var ridge=Owner(f,"PilotRidgeN-37-11");var other=Owner(f,"PilotRidgeN-37-15");var maw=Owner(f,"large-maw-toad");
                Assert.AreNotSame(View(f,ridge),View(f,other));Assert.AreNotSame(View(f,ridge),View(f,maw));
                foreach(var e in new[]{ridge,other,maw})Assert.IsTrue(f.Rendered(e));
            }
        }
        [Test] public void RemovingOneRidgeDisposesItsWholeViewAndPreservesNeighbour()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var ridge=Owner(f,"PilotRidgeN-37-11");var other=Owner(f,"PilotRidgeN-37-15");var own=View(f,ridge);var untouched=View(f,other);
                f.Zone.RemoveEntity(ridge);f.Refresh();Assert.IsFalse(f.Find(ridge,out _,out _));SpawnRing3DIntegrationFixture.Hidden(own);
                Assert.AreSame(untouched,View(f,other));Assert.IsTrue(f.Rendered(other));
            }
        }
        [Test] public void VisibleBodyCellDrawsLargeCreatureWhileHiddenAnchorDoesNotHideIt()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var maw=Owner(f,"large-maw-toad");HideAll(f.Zone);f.Zone.GetCell(11,3).IsVisible=f.Zone.GetCell(11,3).Explored=true;f.Refresh();Assert.IsTrue(f.Rendered(maw));
                f.Zone.GetCell(11,3).IsVisible=false;f.Refresh();Assert.IsFalse(f.Rendered(maw),"A remembered living creature must not become a static ghost.");
            }
        }
        [Test] public void VisibleBoundingBoxHoleDoesNotRevealAnIrregularOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var ridge=Owner(f,"PilotRidgeNE-18-1");HideAll(f.Zone);f.Zone.GetCell(18,1).IsVisible=f.Zone.GetCell(18,1).Explored=true;f.Refresh();Assert.IsFalse(f.Rendered(ridge));
                f.Zone.GetCell(20,1).IsVisible=f.Zone.GetCell(20,1).Explored=true;f.Refresh();Assert.IsTrue(f.Rendered(ridge));
            }
        }
        [Test] public void FootprintPickingReturnsContactCellRatherThanAnchor()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var ridge=Owner(f,"PilotRidgeN-37-11");var root=View(f,ridge);Physics.SyncTransforms();
                var point=new Vector2(38.35f,24.5f-13);
                var ray=f.Get<Camera>("WorldCamera").ViewportPointToRay(f.Source.WorldToViewportPoint(new Vector3(point.x,point.y,0)));
                var hits=new List<RaycastHit>();foreach(var collider in root.GetComponentsInChildren<Collider>())if(collider.Raycast(ray,out var hit,120))hits.Add(hit);
                Assert.IsNotEmpty(hits,"Use an actual imported ridge surface at the current camera angle.");
                var contact=hits.OrderBy(h=>h.distance).First();Assert.IsTrue(Village3DProjection.TryWorldToCell(contact.point,out int cx,out int cy));
                Assert.AreNotEqual(f.Zone.GetEntityPosition(ridge),(cx,cy));Assert.Contains(ridge,f.Zone.GetCell(cx,cy).Occupants.ToList());
                Assert.AreNotEqual(13,cy,"The raised mesh contact must differ from the flat ground cell under the tilted cursor.");
                object[] args={point,null,0,0};Assert.IsTrue((bool)f.Call("TryPickWorld",args));Assert.AreSame(ridge,args[1]);Assert.AreEqual(cx,args[2]);Assert.AreEqual(cy,args[3]);
                f.Zone.GetCell(cx,cy).IsVisible=false;f.Refresh();Assert.IsTrue(f.Rendered(ridge));Assert.IsTrue(f.Zone.GetCell(38,13).IsVisible);
                object[] hidden={point,null,0,0};bool picked=(bool)f.Call("TryPickWorld",hidden);
                Assert.IsFalse(picked&&ReferenceEquals(ridge,hidden[1]),"A visible flat cell of the same owner must not rescue its unseen physical mesh hit.");
            }
        }
        [Test] public void ForcedPipeMovementKeepsTheOwnedRootAndReleasesOldContact()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var pipe=Owner(f,"movable-copper-pipe");var view=View(f,pipe);
                Assert.IsTrue(MovementSystem.ForceMoveTo(pipe,f.Zone,42,10));f.Refresh();Assert.AreSame(view,View(f,pipe));
                Assert.Less(Vector3.Distance(view.transform.position,Village3DProjection.CellCentre(42,10)),.001f);
                Assert.AreEqual(Quaternion.identity,view.transform.rotation,"Fixed footprint presentation must not rotate around an asymmetric anchor.");
            }
        }
        [Test] public void PilotGroundUsesContinuousWorldUVsWithNoGroundRebuildForFovOnlyChange()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                var meshes=f.Root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh!=null&&m.sharedMesh.name=="Pilot world-grain ground").ToArray();
                Assert.IsNotEmpty(meshes);foreach(var filter in meshes)
                {
                    var vertices=filter.sharedMesh.vertices;var uv=filter.sharedMesh.uv;Assert.AreEqual(vertices.Length,uv.Length);
                    for(int i=0;i<vertices.Length;i++){var p=filter.transform.TransformPoint(vertices[i]);Assert.Less(Mathf.Abs(uv[i].x-p.x/80),.0001f);Assert.Less(Mathf.Abs(uv[i].y-p.z/25),.0001f);}
                }
                int builds=f.Get<int>("GroundBuildCount");HideAll(f.Zone);f.Refresh(new HashSet<int>());Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
            }
        }
        [Test] public void PilotGroundIsRemovedPerCellWhenNativeTerrainChanges()
        {
            using(var f=new SpawnRing3DIntegrationFixture(Pilot))
            {
                int x=40,y=10;var cell=f.Zone.GetCell(x,y);var terrain=cell.Objects.First(e=>e.BlueprintName=="TepuiStone");int before=f.Revision(x,y);
                f.Zone.RemoveEntity(terrain);f.Add("Grass",x,y);f.Refresh(SpawnRing3DIntegrationFixture.Dirty(x,y));Assert.Greater(f.Revision(x,y),before);
                foreach(var filter in f.Root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh!=null&&m.sharedMesh.name=="Pilot world-grain ground"))
                {
                    var v=filter.sharedMesh.vertices;var t=filter.sharedMesh.triangles;
                    for(int i=0;i<t.Length;i+=3){var p=filter.transform.TransformPoint((v[t[i]]+v[t[i+1]]+v[t[i+2]])/3);Assert.IsFalse(p.x>=x&&p.x<x+1&&p.z>=24-y&&p.z<25-y,"Old pilot ground still covers changed native terrain.");}
                }
            }
        }
    }
}
