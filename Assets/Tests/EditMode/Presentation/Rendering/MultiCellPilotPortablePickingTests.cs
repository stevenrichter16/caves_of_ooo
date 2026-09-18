using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MultiCellPilotPortablePickingTests
    {
        [Test] public void NativeFallbackItemOnPortableBodyRetainsTownPickingPriority()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var source=f.Manager.GetZone(MultiCellPilotRuntime.ZoneID);
                var actor=source.GetReadOnlyEntities().Single(e=>e.GetPart<MultiCellPilotPropPart>()?.OwnerId=="large-maw-toad");
                bool placed=false;
                for(int y=1;y<23&&!placed;y++)for(int x=1;x<77&&!placed;x++)
                    if(f.Zone.CanPlaceFootprint(actor,x,y))placed=source.TryTransferEntityTo(actor,f.Zone,x,y);
                Assert.IsTrue(placed);f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(actor));
                var edge=f.Zone.GetOccupiedCells(actor).Last();var point=new Vector2(edge.X+.5f,24.5f-edge.Y);Physics.SyncTransforms();
                Assert.IsTrue(f.Presenter.TryPickWorld(point,out var first,out _,out _));Assert.AreSame(actor,first);
                var item=f.Factory.CreateEntity("Tepuibone");Assert.NotNull(item);item.GetPart<RenderPart>().RenderLayer=1000;
                Assert.IsTrue(f.Zone.AddEntity(item,edge.X,edge.Y));f.Refresh();Assert.AreSame(item,edge.GetTopVisibleObject());Assert.IsFalse(f.Presenter.IsAuthoredEntity(item));
                Assert.IsFalse(f.Presenter.TryPickWorld(point,out _,out _,out _),"An ordinary native item drawn above the multi-cell body must retain native click priority.");
                f.Zone.RemoveEntity(item);f.Refresh();Assert.IsTrue(f.Presenter.TryPickWorld(point,out var restored,out int cx,out int cy));Assert.AreSame(actor,restored);Assert.AreEqual(edge.X,cx);Assert.AreEqual(edge.Y,cy);
            }
        }
    }
}
