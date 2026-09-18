using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Diagnostic route selection only. Actual failed native profile
    /// ee8303b92cc344c9860afc21e19422a4 is the blocked-lane RED; these controls
    /// do not claim to execute input or measure runtime performance.</summary>
    public sealed class SpawnRing3DProfileWalkTests
    {
        static bool Choose(Zone zone,Entity player,(int,int) from,(int,int) preferred,(int,int) excluded,out (int,int) target)
        {
            var method=typeof(SpawnRing3DNativeAudit).GetMethod("TryProfileWalkTarget",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method,"Adaptive native-neighbor selection is required.");
            object[] args={zone,player,from,preferred,excluded,null};
            bool okay=(bool)method.Invoke(null,args);target=((int,int))args[5];return okay;
        }
        [TestCase("clear")][TestCase("creature")][TestCase("solid")]
        public void PreferredCellUsesCurrentNativeOccupancyWithoutMutatingIt(string obstruction)
        {
            using(var f=new EntityEquipmentContentFixture())
            {
                var zone=new Zone();var player=f.Factory.CreateEntity("Player");Assert.IsTrue(zone.AddEntity(player,10,10));
                Entity blocker=null;if(obstruction!="clear")
                {blocker=f.Factory.CreateEntity(obstruction=="creature"?"Snapjaw":"WatchLantern");Assert.IsTrue(zone.AddEntity(blocker,11,10));}
                int version=zone.EntityVersion;var entities=zone.GetReadOnlyEntities().ToArray();var positions=entities.Select(zone.GetEntityPosition).ToArray();
                Assert.IsTrue(Choose(zone,player,(10,10),(11,10),(-1,-1),out var target));
                Assert.AreEqual(obstruction=="clear",target==(11,10));Assert.AreEqual(1,Math.Abs(target.Item1-10)+Math.Abs(target.Item2-10));
                Assert.AreEqual(version,zone.EntityVersion);CollectionAssert.AreEqual(entities,zone.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,entities.Select(zone.GetEntityPosition));
                if(blocker!=null)Assert.AreSame(blocker,zone.GetCell(11,10).Objects.Single());
            }
        }
        [Test] public void EnclosedPlayerReportsNoRouteWithoutClearingNativeBlockers()
        {
            using(var f=new EntityEquipmentContentFixture())
            {
                var zone=new Zone();var player=f.Factory.CreateEntity("Player");zone.AddEntity(player,10,10);
                foreach(var p in new[]{(11,10),(10,11),(9,10),(10,9)})zone.AddEntity(f.Factory.CreateEntity("WatchLantern"),p.Item1,p.Item2);
                int version=zone.EntityVersion;Assert.IsFalse(Choose(zone,player,(10,10),(11,10),(-1,-1),out _));Assert.AreEqual(version,zone.EntityVersion);
            }
        }
        [Test] public void RejectedInputTargetIsNotImmediatelyRetriedEvenIfStillApparentlyClear()
        {
            using(var f=new EntityEquipmentContentFixture())
            {
                var zone=new Zone();var player=f.Factory.CreateEntity("Player");zone.AddEntity(player,10,10);
                Assert.IsTrue(Choose(zone,player,(10,10),(11,10),(11,10),out var target));Assert.AreNotEqual((11,10),target);
                Assert.IsTrue(Choose(zone,player,(10,10),(11,10),(-1,-1),out target));Assert.AreEqual((11,10),target);
            }
        }
        [TestCase(0,1)][TestCase(20,20)]
        public void BorderOrDistantPreferenceCannotCauseTransitionOrNonadjacentInput(int x,int y)
        {
            using(var f=new EntityEquipmentContentFixture())
            {
                var zone=new Zone();var player=f.Factory.CreateEntity("Player");zone.AddEntity(player,1,1);
                Assert.IsTrue(Choose(zone,player,(1,1),(x,y),(-1,-1),out var target));Assert.AreNotEqual((x,y),target);
                Assert.That(target.Item1,Is.InRange(1,Zone.Width-2));Assert.That(target.Item2,Is.InRange(1,Zone.Height-2));
                Assert.AreEqual(1,Math.Abs(target.Item1-1)+Math.Abs(target.Item2-1));Assert.AreEqual((1,1),zone.GetEntityPosition(player));
            }
        }
    }
}
