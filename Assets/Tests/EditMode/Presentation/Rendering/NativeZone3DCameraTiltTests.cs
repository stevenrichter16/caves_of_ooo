using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class NativeZone3DCameraTiltTests
    {
        const float Angle=56;
        static float Cot=>1/Mathf.Tan(Angle*Mathf.Deg2Rad);
        static Entity Maw(Zone z)=>z.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId=="large-maw-toad");
        static void Hide(Zone z){for(int y=0;y<25;y++)for(int x=0;x<80;x++)z.GetCell(x,y).IsVisible=z.GetCell(x,y).Explored=false;}
        static void Visible(Zone z,int x,int y){z.GetCell(x,y).IsVisible=z.GetCell(x,y).Explored=true;}
        static GameObject View(SpawnRing3DIntegrationFixture f,Entity e){Assert.IsTrue(f.Find(e,out var root,out _));return root;}
        static Vector2 TallPart(GameObject root)
        {
            // Controlled tall selection geometry under a real registered owner.
            // No sprite/art assertion is made by this projection contract test.
            var collider=root.GetComponent<BoxCollider>();Assert.NotNull(collider);
            collider.center=new Vector3(.6f,2,.4f);collider.size=new Vector3(.8f,1,.16f);
            Physics.SyncTransforms();var hit=root.transform.TransformPoint(new Vector3(.6f,2.5f,.4f));
            return new Vector2(hit.x,hit.z+hit.y*Cot);
        }
        static bool Pick(SpawnRing3DIntegrationFixture f,Vector2 point,out Entity e,out int x,out int y)
        {object[] args={point,null,0,0};bool hit=(bool)f.Call("TryPickWorld",args);e=args[1]as Entity;x=(int)args[2];y=(int)args[3];return hit;}
        static void GroundMatches(Camera world,Camera source)
        {
            foreach(var p in new[]{new Vector2(.5f,.5f),new Vector2(79.5f,24.5f),new Vector2(40,12.5f),new Vector2(12.25f,18.75f)})
            {
                var actual=world.WorldToViewportPoint(new Vector3(p.x,0,p.y));
                var expected=source.WorldToViewportPoint(new Vector3(p.x,p.y,0));
                Assert.Less(Vector2.Distance(actual,expected),.0001f,"Ground lost its exact2D registration.");
                var ray=world.ViewportPointToRay(actual);var plane=new Plane(Vector3.up,Vector3.zero);
                Assert.IsTrue(plane.Raycast(ray,out float enter));Assert.Less(Vector3.Distance(new Vector3(p.x,0,p.y),ray.GetPoint(enter)),.001f);
                var picking=NativeZone3DRenderSurface.GroundPointRay(p);
                Assert.Greater(Vector3.Dot(ray.direction,picking.direction),.999999f);
                Assert.Less(Vector3.Cross(ray.origin-picking.origin,picking.direction).magnitude,.001f,"Picking ray differs from actual camera projection.");
            }
        }
        [Test] public void SharedCameraUses56DegreesAndKeepsGroundCentered()
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var camera=f.Get<Camera>("WorldCamera");Assert.Less(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(Angle,0,0)),.001f);
                Assert.Less(Vector3.Distance(camera.transform.position,new Vector3(f.Source.transform.position.x,35,f.Source.transform.position.y-35*Cot)),.001f);
                GroundMatches(camera,f.Source);Assert.AreEqual(new Vector3(40,12.75f,-10),f.Source.transform.position);Assert.AreEqual(Quaternion.identity,f.Source.transform.rotation);
            }
        }
        [Test] public void RepeatedSyncAndCameraPanZoomKeepGroundAndBorrowedCameraRegistered()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                f.Source.transform.position=new Vector3(33.2f,10.4f,-10);f.Source.orthographicSize=9.6f;f.Source.aspect=2.1f;
                for(int i=0;i<20;i++)Village3DIntegrationFixture.TickFrame(f.Presenter);
                GroundMatches(f.Presenter.WorldCamera,f.Source);var projection=f.Presenter.WorldCamera.projectionMatrix;
                for(int i=0;i<20;i++)Village3DIntegrationFixture.TickFrame(f.Presenter);
                Assert.AreEqual(projection,f.Presenter.WorldCamera.projectionMatrix);Assert.AreEqual(9.6f,f.Source.orthographicSize);Assert.AreEqual(2.1f,f.Source.aspect);
            }
        }
        [Test] public void RaisedGeometryShowsExpectedHeightParallaxWhileGroundStaysFixed()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var camera=f.Presenter.WorldCamera;var low=camera.WorldToViewportPoint(new Vector3(40,0,12));var high=camera.WorldToViewportPoint(new Vector3(40,3,12));
                float expected=3*Cot/(2*f.Source.orthographicSize);Assert.Greater(expected,.01f);
                Assert.AreEqual(low.x,high.x,.0001f);Assert.AreEqual(expected,high.y-low.y,.0001f);GroundMatches(camera,f.Source);
            }
        }
        [Test] public void RingRaisedBodyPickUsesPhysicalCellEvenWhenFlatCellIsUnseen()
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var e=Maw(f.Zone);var point=TallPart(View(f,e));Hide(f.Zone);Visible(f.Zone,11,2);f.Refresh();Assert.IsTrue(f.Rendered(e));
                Assert.IsTrue(Pick(f,point,out var owner,out int x,out int y));Assert.AreSame(e,owner);Assert.AreEqual(11,x);Assert.AreEqual(2,y);
                f.Zone.GetCell(11,2).IsVisible=false;Visible(f.Zone,11,3);f.Refresh();Assert.IsTrue(f.Rendered(e));
                Assert.IsFalse(Pick(f,point,out _,out _,out _),"A visible far body cell must not make an unseen hit selectable.");
            }
        }
        [Test] public void VisibleNativeFallbackUnderRaisedBodyProjectionKeepsFlatGridPriority()
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var e=Maw(f.Zone);var point=TallPart(View(f,e));Hide(f.Zone);Visible(f.Zone,11,2);int flatY=24-Mathf.FloorToInt(point.y);Assert.That(flatY,Is.InRange(0,24));Assert.AreNotEqual(2,flatY);Visible(f.Zone,11,flatY);f.Refresh();
                Assert.IsTrue(Pick(f,point,out var first,out _,out _));Assert.AreSame(e,first);
                var fallback=f.Factory.CreateEntity("Tepuibone");fallback.BlueprintName="UnmodeledNativePickup";fallback.GetPart<RenderPart>().RenderLayer=1000;Assert.IsTrue(f.Zone.AddEntity(fallback,11,flatY));f.Refresh();
                Assert.IsFalse(f.Authored(fallback));Assert.IsFalse(Pick(f,point,out _,out _,out _));
                f.Zone.GetCell(11,flatY).IsVisible=false;f.Refresh();Assert.IsTrue(Pick(f,point,out var visibleBody,out _,out _));Assert.AreSame(e,visibleBody);
            }
        }
        [Test] public void TownRaisedPortablePickUsesPhysicalContactAcrossFlatFogBoundary()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var source=f.Manager.GetZone(MultiCellPilotRuntime.ZoneID);var e=Maw(source);int ax=-1,ay=-1;
                for(int y=2;y<22&&ax<0;y++)for(int x=2;x<77&&ax<0;x++)if(f.Zone.CanPlaceFootprint(e,x,y)){ax=x;ay=y;}
                Assert.GreaterOrEqual(ax,0);Assert.IsTrue(source.TryTransferEntityTo(e,f.Zone,ax,ay));f.Refresh();
                Assert.IsTrue(f.Presenter.TryGetOwnerView("large-maw-toad",out _,out var root));var point=TallPart(root);Hide(f.Zone);Visible(f.Zone,ax+1,ay);f.Refresh();
                Assert.IsTrue(f.Presenter.TryPickWorld(point,out var owner,out int cx,out int cy));Assert.AreSame(e,owner);Assert.AreEqual(ax+1,cx);Assert.AreEqual(ay,cy);
                f.Zone.GetCell(ax+1,ay).IsVisible=false;Visible(f.Zone,ax+1,ay+1);f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(e));Assert.IsFalse(f.Presenter.TryPickWorld(point,out _,out _,out _));
            }
        }
    }
}
