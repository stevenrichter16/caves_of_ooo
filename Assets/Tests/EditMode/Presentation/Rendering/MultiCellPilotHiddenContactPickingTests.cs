using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MultiCellPilotHiddenContactPickingTests
    {
        [Test]
        public void RingNativeCornerWithoutMeshRemainsSelectableOnlyWhileItsFlatCellIsVisible()
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                var pipe=f.Zone.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId=="movable-copper-pipe");
                Assert.IsTrue(MovementSystem.ForceMoveTo(pipe,f.Zone,42,10));f.Refresh();
                Assert.IsTrue(f.Find(pipe,out var root,out _));Physics.SyncTransforms();var colliders=root.GetComponentsInChildren<Collider>();Assert.IsNotEmpty(colliders);
                var corner=new Vector2(root.transform.position.x-.48f,root.transform.position.z+.45f);
                var ray=f.Get<Camera>("WorldCamera").ViewportPointToRay(f.Source.WorldToViewportPoint(new Vector3(corner.x,corner.y,0)));
                Assert.IsTrue(colliders.All(c=>!c.Raycast(ray,out _,120)));object[] args={corner,null,0,0};
                Assert.IsTrue((bool)f.Call("TryPickWorld",args));Assert.AreSame(pipe,args[1]);var anchor=f.Zone.GetEntityPosition(pipe);Assert.AreEqual(anchor,((int)args[2],(int)args[3]));
                f.Zone.GetCell(anchor.x,anchor.y).IsVisible=false;f.Refresh();Assert.IsTrue(f.Rendered(pipe));
                object[] hidden={corner,null,0,0};bool picked=(bool)f.Call("TryPickWorld",hidden);
                Assert.IsFalse(picked&&ReferenceEquals(pipe,hidden[1]),"A different visible owner behind the corner may still be selected.");
            }
        }
        [Test]
        public void TownPortableVisibleFlatCellCannotRescueItsHiddenMeshContact()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var source=f.Manager.GetZone(MultiCellPilotRuntime.ZoneID);
                var maw=source.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId=="large-maw-toad");int ax=-1,ay=-1;
                for(int y=2;y<22&&ax<0;y++)for(int x=2;x<77&&ax<0;x++)if(f.Zone.CanPlaceFootprint(maw,x,y)){ax=x;ay=y;}
                Assert.GreaterOrEqual(ax,0);Assert.IsTrue(source.TryTransferEntityTo(maw,f.Zone,ax,ay));f.Refresh();
                Assert.IsTrue(f.Presenter.TryGetOwnerView("large-maw-toad",out var owner,out var root));Assert.AreSame(maw,owner);Physics.SyncTransforms();
                // This is the real imported actor's registered selection collider,
                // unchanged in size or placement. Its raised top projects over
                // the owner's northern cell while physically touching its south.
                var collider=root.GetComponent<BoxCollider>();Assert.NotNull(collider);
                var point=new Vector2(root.transform.position.x+.6f,root.transform.position.z-.4f);
                var ray=f.Presenter.WorldCamera.ViewportPointToRay(f.Source.WorldToViewportPoint(new Vector3(point.x,point.y,0)));
                Assert.IsTrue(collider.Raycast(ray,out var hit,120));Assert.IsTrue(Village3DProjection.TryWorldToCell(hit.point,out int cx,out int cy));
                Assert.IsTrue(Village3DProjection.TryWorldToCell(new Vector3(point.x,0,point.y),out int flatX,out int flatY));Assert.AreNotEqual((flatX,flatY),(cx,cy));
                Assert.Contains(maw,f.Zone.GetCell(cx,cy).Occupants.ToList());Assert.Contains(maw,f.Zone.GetCell(flatX,flatY).Occupants.ToList());
                Assert.IsTrue(f.Presenter.TryPickWorld(point,out var selected,out int sx,out int sy));Assert.AreSame(maw,selected);Assert.AreEqual((cx,cy),(sx,sy));
                f.Zone.GetCell(cx,cy).IsVisible=false;f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(maw));Assert.IsTrue(f.Zone.GetCell(flatX,flatY).IsVisible);
                bool picked=f.Presenter.TryPickWorld(point,out var hiddenOwner,out _,out _);
                Assert.IsFalse(picked&&ReferenceEquals(maw,hiddenOwner),"Visible flat ownership must not override hidden physical contact.");
            }
        }
        [Test]
        public void TownPortablePipePreservesARealNoMeshCornerFallback()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var source=f.Manager.GetZone(MultiCellPilotRuntime.ZoneID);
                var pipe=source.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId=="movable-copper-pipe");int ax=-1,ay=-1;
                for(int y=2;y<22&&ax<0;y++)for(int x=2;x<76&&ax<0;x++)if(f.Zone.CanPlaceFootprint(pipe,x,y)){ax=x;ay=y;}
                Assert.GreaterOrEqual(ax,0);Assert.IsTrue(source.TryTransferEntityTo(pipe,f.Zone,ax,ay));f.Refresh();
                Assert.IsTrue(f.Presenter.TryGetOwnerView("movable-copper-pipe",out var owner,out var root));Assert.AreSame(pipe,owner);Physics.SyncTransforms();
                var collider=root.GetComponent<BoxCollider>();Assert.NotNull(collider);
                var corner=new Vector2(root.transform.position.x-.48f,root.transform.position.z+.45f);
                var ray=f.Presenter.WorldCamera.ViewportPointToRay(f.Source.WorldToViewportPoint(new Vector3(corner.x,corner.y,0)));
                Assert.IsFalse(collider.Raycast(ray,out _,120),"Actual imported pipe must leave a native corner without selection geometry.");
                Assert.IsTrue(f.Presenter.TryPickWorld(corner,out var selected,out int sx,out int sy));Assert.AreSame(pipe,selected);Assert.AreEqual((ax,ay),(sx,sy));
                f.Zone.GetCell(ax,ay).IsVisible=false;f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(pipe));
                bool picked=f.Presenter.TryPickWorld(corner,out var hiddenOwner,out _,out _);
                Assert.IsFalse(picked&&ReferenceEquals(pipe,hiddenOwner));
            }
        }
    }
}
