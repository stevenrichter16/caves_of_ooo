using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ThrowTrajectoryTests : FiftyWorldFixture
    {
        Zone zone; Entity actor, item, target;
        [SetUp] public void SetUpFlight()
        {
            zone = new Zone("throw-flight"); actor = Factory.CreateEntity("Player");
            item = Factory.CreateEntity("Dagger"); target = Factory.CreateEntity("MarlbackScrabbler");
            zone.AddEntity(actor, 10, 10); zone.AddEntity(target, 16, 10);
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
            { zone.GetCell(x,y).IsVisible=true; zone.GetCell(x,y).Explored=true; }
        }
        ThrowTraceResult Body(bool known=false) => ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.AtEntity(target,16,10),20,known);
        Entity Obstacle(int x,int y,float bottom,float height,bool solid=false)
        {
            var entity = new Entity(); entity.AddPart(new PhysicsPart { Solid=solid,ThrowBase=bottom,ThrowHeight=height,ThrowIntercept=1 });
            entity.AddPart(new RenderPart{Visible=true});zone.AddEntity(entity,x,y); return entity;
        }
        [Test] public void LowFloorItemClearsButDeliberateLowAimHits()
        {
            var dagger=Factory.CreateEntity("Dagger");zone.AddEntity(dagger,13,10);
            Assert.AreSame(target,Body().HitEntity);
            Assert.AreSame(dagger,ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.AtEntity(dagger,13,10),20).HitEntity);
        }
        [Test] public void EntryEdgeIntersectionIsNotMissedByCenterHeight()
        {
            // Body flight descends1.04→.8. At cell13 entry .94; center .92.
            var shelf=Obstacle(13,10,.98f,.2f);
            Assert.AreSame(shelf,Body().HitEntity,"Vertical radius reaches the shelf at cell entry, although center does not.");
            shelf.GetPart<PhysicsPart>().ThrowBase=1.2f;
            Assert.AreSame(target,Body().HitEntity,"Same shelf lifted beyond the swept tube clears.");
        }
        [Test] public void LowMovementBlockerClearsFlightButCannotReceiveRestingItem()
        {
            zone.RemoveEntity(target);var low=Obstacle(16,10,0,.1f,true);
            var trace=ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.Ground(16,10),20);
            Assert.AreSame(low,trace.HitEntity);Assert.AreEqual(15,trace.RestCell.X);
            Assert.AreSame(trace.RestCell,trace.BurstCell);
        }
        [Test] public void ExactCornerSealedSideCellBlocksBothDirections()
        {
            zone.RemoveEntity(target);zone.AddEntity(target,14,14);var wall=Obstacle(11,10,0,2.4f,true);
            var trace=ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.AtEntity(target,14,14),20);
            Assert.AreSame(wall,trace.HitEntity);Assert.AreEqual(10,trace.RestCell.X);
            var reverse=ThrowTrajectory.Trace(zone,target,item,ThrowIntent.AtEntity(actor,10,10),20);
            Assert.AreSame(wall,reverse.HitEntity);
        }
        [Test] public void StableTiePrefersSealedCoverOverBodyRegardlessOfInsertion()
        {
            var wall=Obstacle(16,10,0,2.4f,true);
            Assert.AreSame(wall,Body().HitEntity);zone.RemoveEntity(target);zone.AddEntity(target,16,10);
            Assert.AreSame(wall,Body().HitEntity);
        }
        [Test] public void HiddenCellStopsPreviewWithoutInspectingItsOccupants()
        {
            zone.GetCell(13,10).IsVisible=false;var wall=Obstacle(13,10,0,2.4f,true);
            var before=Body(true);Assert.True(before.Uncertain);Assert.Null(before.HitEntity);
            Assert.AreEqual(12,before.ImpactCell.X);zone.RemoveEntity(wall);
            var after=Body(true);Assert.True(after.Uncertain);Assert.AreEqual(before.Path.Count,after.Path.Count);
            Assert.AreSame(target,Body().HitEntity,"Full execution sees the actual unobstructed world.");
        }
        [Test] public void InvisibleOwnerCannotChangeKnownPreview()
        {
            var blocker=Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(blocker,13,10);blocker.GetPart<RenderPart>().Visible=false;
            Assert.AreSame(target,Body(true).HitEntity);Assert.AreSame(blocker,Body().HitEntity);
        }
        [Test] public void GroundAndBodyIntentDifferAtDestination()
        {
            zone.RemoveEntity(target);var low=Obstacle(16,10,0,.15f);
            var aimed=ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.Ground(16,10),20);
            Assert.AreSame(low,aimed.HitEntity);
            zone.AddEntity(target,16,10);Assert.AreSame(target,Body().HitEntity);
        }
        [Test] public void MalformedProfilesFallbackConservativelyAndExplicitPermeableWins()
        {
            var obstacle=Obstacle(13,10,float.NaN,float.PositiveInfinity,true);
            Assert.AreSame(obstacle,Body().HitEntity);obstacle.GetPart<PhysicsPart>().ThrowIntercept=0;
            Assert.AreSame(target,Body().HitEntity);
        }
        [Test] public void SameCellGroundHasNoFlightAndNoImplicitSelfHit()
        {
            var result=ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.Ground(10,10),20);
            Assert.IsEmpty(result.Path);Assert.Null(result.HitEntity);Assert.AreEqual(10,result.RestCell.X);
        }
        [Test] public void LegacyMeleeTraceStillStopsAtFloorObjects()
        {
            var floor=Factory.CreateEntity("Dagger");zone.AddEntity(floor,11,10);
            Assert.AreSame(floor,LineTargeting.TraceFirstImpact(zone,actor,10,10,1,0,2).HitEntity);
        }
        [Test] public void OpenDoorIsPermeableAndClosedDoorBlocksWithoutChangingLegacyTrace()
        {
            var door=new Entity{ID="flight-door"};door.AddPart(new PhysicsPart());var part=new DoorPart{IsOpen=false};door.AddPart(part);
            zone.AddEntity(door,13,10);Assert.AreSame(door,Body().HitEntity);
            part.IsOpen=true;Assert.AreSame(target,Body().HitEntity,"An open aperture cannot remain a body-height interceptor.");
        }
        [Test] public void WideOwnerIsTargetedAtOccupiedContactNotEmptyAnchor()
        {
            zone.RemoveEntity(target);target.AddPart(new SpatialFootprintPart{CellsRaw="0,-1;0,1"});zone.AddEntity(target,16,10);
            Assert.False(ThrowTrajectory.ValidateIntent(zone,actor,ThrowIntent.AtEntity(target,16,10),false,out _));
            var contact=ThrowTrajectory.Trace(zone,actor,item,ThrowIntent.AtEntity(target,16,9),20);
            Assert.AreSame(target,contact.HitEntity);Assert.AreEqual(9,contact.ImpactCell.Y);
        }
        [Test] public void ResolvedFlightEndsAtActualContactHeightAndDoesNotBorrowMutableTraceLists()
        {
            var trace=Body();Assert.AreEqual(trace.Path.Count,trace.FlightPath.Count);
            var last=trace.FlightPath[trace.FlightPath.Count-1];
            Assert.That(last.Height,Is.EqualTo(trace.ContactHeight).Within(.00001));
            Assert.That(last.X,Is.EqualTo(10+6*trace.ContactFraction).Within(.00001));
            AsciiFxBus.Clear();AsciiFxBus.EmitThrow(zone,trace);var requests=AsciiFxBus.Drain();
            try
            {
                Assert.AreEqual(1,requests.Count);var request=requests[0];
                Assert.AreEqual(last.Height,request.ThrowFlight[request.ThrowFlight.Count-1].Height);
                trace.FlightPath.Clear();trace.Path.Clear();Assert.Greater(request.ThrowFlight.Count,0);Assert.Greater(request.Path.Count,0);
                request.Reset();Assert.Null(request.ThrowFlight,"Pooled requests must not retain the last throw's geometry.");
            }
            finally{foreach(var r in requests)AsciiFxBus.Release(r);}
        }
        [Test] public void PreviewRestUsesTheWholeThrownFootprint()
        {
            item.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Obstacle(17,10,0,2.4f,true);
            Assert.AreEqual(15,Body(true).RestCell.X,"The anchor is clear but the other projectile cell is blocked.");
        }
        [Test] public void EqualPriorityDuplicateIdentityCannotPickAnInsertionOrderVictim()
        {
            var first=Obstacle(13,10,0,1.6f);var second=Obstacle(13,10,0,1.6f);
            first.ID=second.ID="duplicate";
            Assert.Null(Body().HitEntity,"Ambiguous identity must stop without damaging an arbitrary object.");
            zone.RemoveEntity(first);zone.AddEntity(first,13,10);Assert.Null(Body().HitEntity);
            second.ID="unique";Assert.AreSame(first,Body().HitEntity,"Unique ordinal identities resolve the same contact.");
        }
        [Test] public void LooseItemMayRestWithCreatureButNotSolidFurnitureAcrossItsFootprint()
        {
            Assert.True(ThrowTrajectory.CanRestItem(zone,item,zone.GetCell(16,10),actor));
            item.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});var wall=Obstacle(17,10,0,2.4f,true);
            Assert.False(ThrowTrajectory.CanRestItem(zone,item,zone.GetCell(16,10),actor));
            zone.RemoveEntity(wall);Assert.True(ThrowTrajectory.CanRestItem(zone,item,zone.GetCell(16,10),actor));
        }
    }
}
