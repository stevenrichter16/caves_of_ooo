using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    public class MultiCellHandlingTests
    {
        private static Entity Actor(string shape=null)
        {
            var e=MultiCellSpatialTests.Body(shape);e.Tags["Creature"]="";e.Tags["Ally"]="";
            e.Statistics["Strength"]=new Stat {Owner=e,Name="Strength",BaseValue=30};
            e.Statistics["Hitpoints"]=new Stat {Owner=e,Name="Hitpoints",BaseValue=100,Min=0,Max=100};
            return e;
        }
        private static Entity Pipe()
        {
            var e=MultiCellSpatialTests.Body("0,0;1,0;2,0");
            e.GetPart<PhysicsPart>().Weight=60;e.AddPart(new HandlingPart {Weight=60,Carryable=false});return e;
        }
        [TestCase(false)][TestCase(true)]
        public void HaulFromEitherEndMovesOneStepWithoutSnappingAnchorToHand(bool east)
        {
            var z=new Zone();var actor=Actor();var pipe=Pipe();z.AddEntity(pipe,10,10);
            z.AddEntity(actor,east?13:9,10);
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(actor,pipe,z));
            Assert.IsTrue(MovementSystem.TryMove(actor,z,east?1:-1,0));
            Assert.AreEqual((east?11:9,10),z.GetEntityPosition(pipe));
            Assert.IsTrue(DragSystem.IsDragging(actor));
        }
        [Test] public void BlockedFarCornerBreaksGripWithoutLosingLoad()
        {
            var z=new Zone();var actor=Actor();var pipe=Pipe();z.AddEntity(pipe,10,10);z.AddEntity(actor,13,10);
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(actor,pipe,z));
            var blocker=MultiCellSpatialTests.Body(null);z.AddEntity(blocker,11,11);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,0,1));
            Assert.AreEqual((11,10),z.GetEntityPosition(pipe));Assert.IsTrue(DragSystem.IsDragging(actor));
            Assert.IsTrue(MovementSystem.TryMove(actor,z,0,1));
            Assert.AreEqual((11,10),z.GetEntityPosition(pipe));Assert.IsFalse(DragSystem.IsDragging(actor));
        }
        [Test] public void TumbleRefusesUnequalBodiesWhoseDestinationFootprintsOverlap()
        {
            var z=new Zone();var actor=Actor();var target=Actor("0,0;1,0;0,1;1,1");
            z.AddEntity(actor,9,10);z.AddEntity(target,10,10);
            var result=new Acrobatics_Tumble().OnCommand(new SkillEventContext {Attacker=actor,Defender=actor,Zone=z,Rng=new System.Random(1)});
            Assert.IsFalse(result);Assert.AreEqual((9,10),z.GetEntityPosition(actor));
            Assert.AreEqual((10,10),z.GetEntityPosition(target));Assert.IsTrue(z.GetOccupants(11,11).Contains(target));
        }
        [Test] public void EqualLargeBodiesCanTumbleUsingTheirAdjacentEdges()
        {
            var z=new Zone();var actor=Actor("0,0;1,0;0,1;1,1");var target=Actor("0,0;1,0;0,1;1,1");
            z.AddEntity(actor,10,10);z.AddEntity(target,12,10);
            var result=new Acrobatics_Tumble().OnCommand(new SkillEventContext {Attacker=actor,Defender=actor,Zone=z,Rng=new System.Random(1)});
            Assert.IsTrue(result);Assert.AreEqual((12,10),z.GetEntityPosition(actor));Assert.AreEqual((10,10),z.GetEntityPosition(target));
            Assert.AreEqual(2,z.EntityCount);Assert.AreEqual(1,z.GetOccupants(13,11).Count);
        }
        private sealed class SwapEntryProbe : Part
        {
            public int Count; public Entity Last;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="EntityEnteredCell") {Count++;Last=e.GetParameter<Entity>("Actor");}
                return true;
            }
        }
        [TestCase(false)][TestCase(true)]
        public void SuccessfulTumbleFiresBodyEntryForEachSwappedOwner(bool wide)
        {
            var z=new Zone();var actor=Actor(wide?"0,0;1,0;0,1;1,1":null);
            var target=Actor(wide?"0,0;1,0;0,1;1,1":null);
            int tx=wide?12:11;z.AddEntity(actor,10,10);z.AddEntity(target,tx,10);
            var first=MultiCellSpatialTests.Body(null,false);var firstProbe=new SwapEntryProbe();first.AddPart(firstProbe);z.AddEntity(first,10,10);
            var second=MultiCellSpatialTests.Body(null,false);var secondProbe=new SwapEntryProbe();second.AddPart(secondProbe);z.AddEntity(second,tx,wide?11:10);
            Assert.IsTrue(new Acrobatics_Tumble().OnCommand(new SkillEventContext {Attacker=actor,Zone=z,Rng=new System.Random(1)}));
            Assert.AreEqual(1,firstProbe.Count);Assert.AreSame(target,firstProbe.Last);
            Assert.AreEqual(1,secondProbe.Count);Assert.AreSame(actor,secondProbe.Last);
        }
        [Test] public void RefusedTumbleFiresNoBodyEntry()
        {
            var z=new Zone();var actor=Actor();var target=Actor("0,0;1,0;0,1;1,1");z.AddEntity(actor,9,10);z.AddEntity(target,10,10);
            var rune=MultiCellSpatialTests.Body(null,false);var probe=new SwapEntryProbe();rune.AddPart(probe);z.AddEntity(rune,9,10);
            Assert.IsFalse(new Acrobatics_Tumble().OnCommand(new SkillEventContext {Attacker=actor,Zone=z,Rng=new System.Random(1)}));
            Assert.AreEqual(0,probe.Count);
        }
        [TestCase(false)] [TestCase(true)]
        public void TumbleArchiveBarrierUsesPhysicalBodiesInsteadOfEmptyAnchorHoles(bool atFoot)
        {
            var z = new Zone(); var actor = Actor("1,0"); var target = Actor("1,0");
            Assert.IsTrue(z.AddEntity(actor, 10, 10));
            Assert.IsTrue(z.AddEntity(target, 11, 10));
            var barrier = MultiCellSpatialTests.Body(null, false);
            barrier.AddPart(new SealedLibraryBarrierPart());
            Assert.IsTrue(z.AddEntity(barrier, atFoot ? 11 : 10, 10));
            Assert.IsTrue(barrier.GetPart<SealedLibraryBarrierPart>().IsClosed);
            Assert.IsFalse(z.GetOccupants(10, 10).Contains(actor));
            Assert.IsTrue(z.GetOccupants(11, 10).Contains(actor));

            Assert.AreEqual(!atFoot, new Acrobatics_Tumble().OnCommand(new SkillEventContext
                { Attacker = actor, Zone = z, Rng = new System.Random(1) }));
            Assert.AreEqual((atFoot ? 10 : 11, 10), z.GetEntityPosition(actor));
            Assert.AreEqual((atFoot ? 11 : 10, 10), z.GetEntityPosition(target));
            Assert.IsTrue(z.GetOccupants(atFoot ? 11 : 12, 10).Contains(actor));
            Assert.IsTrue(z.GetOccupants(atFoot ? 12 : 11, 10).Contains(target));
        }

        [TestCase("scenery",false)][TestCase("movable",true)]
        public void PilotStationaryBodiesRejectDisplacementWhileMovableBodiesMove(string role,bool moves)
        {
            var z=new Zone();var owner=MultiCellSpatialTests.Body();
            owner.AddPart(new MultiCellPilotPropPart {OwnerId="test",ModelId="test",Role=role});z.AddEntity(owner,10,10);
            Assert.AreEqual(moves,MovementSystem.ForceMoveTo(owner,z,11,10));
            Assert.AreEqual((moves?11:10,10),z.GetEntityPosition(owner));
        }
        [Test] public void ProjectileEffectEndsAtStruckBodySurfaceInsteadOfExtendingPastIt()
        {
            SpellFxBus.Clear();
            try
            {
                var z=new Zone();var caster=Actor();var target=Actor("0,0;1,0;0,1;1,1");
                z.AddEntity(caster,8,11);z.AddEntity(target,10,10);
                Assert.IsTrue(new Pyromancy_Kindle().OnCommand(new SkillEventContext
                    {Attacker=caster,Zone=z,DirectionX=1,DirectionY=0,Rng=new System.Random(1)}));
                var sequence=SpellFxBus.Drain()[0];var last=sequence.Path[sequence.Path.Count-1];
                Assert.AreEqual(10,last.X);Assert.AreEqual(11,last.Y);
            }
            finally {SpellFxBus.Clear();}
        }
        [Test] public void TileReactionReachesRemoteBodySurfaceWithoutItsAnchor()
        {
            TileReactionSystem.Initialize("{\"Reactions\":[{\"ID\":\"edge_test\",\"InputEnergy\":\"charge\",\"MinEnergy\":1,\"OccupantDamage\":5}]}");
            try
            {
                var z=new Zone();var body=Actor("0,0;1,0;0,1;1,1");z.AddEntity(body,10,10);
                z.TileState.AddCharge(11,11,3);TileReactionSystem.ResolveZone(z);
                Assert.AreEqual(95,body.GetStatValue("Hitpoints"));
            }
            finally {TileReactionSystem.ResetForTests();}
        }
    }
}
