using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    public class MultiCellMovementTests
    {
        private static Entity Body(string shape = "0,0;1,0;0,1;1,1", bool solid = true)
            => MultiCellSpatialTests.Body(shape,solid);
        private sealed class EntryCounter : Part
        {
            public int Count; public Cell Contact; public System.Action<GameEvent> Action;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="EntityEnteredCell") { Count++; Contact=e.GetParameter<Cell>("Cell");Action?.Invoke(e); }
                return true;
            }
        }
        [Test] public void SingleMoverBumpsLargeBodyAtRemoteEdgeWithOwnerAsBlocker()
        {
            var z=new Zone();var body=Body();var actor=Body(null);
            z.AddEntity(body,10,10);z.AddEntity(actor,12,11);
            var move=MovementSystem.TryMoveEx(actor,z,-1,0);
            Assert.IsFalse(move.moved);Assert.AreSame(body,move.blockedBy);
            Assert.AreEqual((12,11),z.GetEntityPosition(actor));
            z.RemoveEntity(body);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,-1,0));
        }
        [Test] public void LargeMoverReportsBlockerAtFarCornerBeforeMoving()
        {
            var z=new Zone();var body=Body();var wall=Body(null);
            z.AddEntity(body,10,10);z.AddEntity(wall,12,11);
            var move=MovementSystem.TryMoveEx(body,z,1,0);
            Assert.IsFalse(move.moved);Assert.AreSame(wall,move.blockedBy);
            z.RemoveEntity(wall);Assert.IsTrue(MovementSystem.TryMove(body,z,1,0));
        }
        [TestCase(true)][TestCase(false)]
        public void NarrowPassageFitsSingleCellButRejectsWideBody(bool wide)
        {
            var z=new Zone();var actor=Body(wide?"0,0;1,0;0,1;1,1":null);
            z.AddEntity(actor,39,8);
            for(int x=0;x<Zone.Width;x++) if(x!=40)
            {var wall=Body(null);wall.Tags["Solid"]="";z.AddEntity(wall,x,10);}
            var path=FindPath.Search(z,39,8,39,12,actor:actor);
            Assert.AreEqual(!wide,path.Usable);
        }
        [Test] public void AlternateWidePassageProducesOnlyLegalWholeBodySteps()
        {
            var z=new Zone();var actor=Body();z.AddEntity(actor,39,8);
            for(int x=0;x<Zone.Width;x++) if(x!=40&&x!=50&&x!=51)
            {var wall=Body(null);wall.Tags["Solid"]="";z.AddEntity(wall,x,10);}
            var path=FindPath.Search(z,39,8,39,12,actor:actor);
            Assert.IsTrue(path.Usable);
            int cx=39,cy=8;
            foreach(var step in path.Steps)
            {cx+=step.dx;cy+=step.dy;Assert.IsTrue(z.CanPlaceFootprint(actor,cx,cy),$"illegal ({cx},{cy})");}
            Assert.AreEqual((39,12),(cx,cy));
        }
        [Test] public void ForcedMoveIgnoresStunButNeverFarCornerCollision()
        {
            var z=new Zone();var actor=Body();actor.AddPart(new StatusEffectsPart());
            z.AddEntity(actor,10,10);actor.ApplyEffect(new StunnedEffect(3),actor,z);
            Assert.IsFalse(MovementSystem.TryMove(actor,z,1,0));
            Assert.IsTrue(MovementSystem.ForceMoveTo(actor,z,11,10));
            var wall=Body(null);z.AddEntity(wall,13,11);
            Assert.IsFalse(MovementSystem.ForceMoveTo(actor,z,12,10));
            Assert.AreEqual((11,10),z.GetEntityPosition(actor));
        }
        [Test] public void RemoteBodyCellEntryTriggersOnceAndKeepsOldOverlapQuiet()
        {
            var z=new Zone();var actor=Body();var rune=Body("0,0;0,1",false);var counter=new EntryCounter();rune.AddPart(counter);
            z.AddEntity(actor,10,10);z.AddEntity(rune,12,10);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,1,0));
            Assert.AreEqual(1,counter.Count);Assert.AreEqual(12,counter.Contact.X);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,0,1));
            Assert.AreEqual(1,counter.Count,"still touching same rune without entering a new rune cell");
            Assert.IsTrue(MovementSystem.TryMove(actor,z,0,0));Assert.AreEqual(1,counter.Count);
        }
        [Test] public void CellEntryHoleDoesNotTriggerAndSeparateMoveTriggersAgain()
        {
            var z=new Zone();var actor=Body("0,0;2,0");var rune=Body(null,false);var c=new EntryCounter();rune.AddPart(c);
            z.AddEntity(actor,10,10);z.AddEntity(rune,12,11);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,0,1));Assert.AreEqual(1,c.Count);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,1,0));Assert.AreEqual(1,c.Count);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,-1,0));Assert.AreEqual(2,c.Count);
        }
        [Test] public void PushSlidesSolidBodyThroughItsOwnOldCells()
        {
            var z=new Zone();var pusher=Body(null);var actor=Body();actor.Tags["Solid"]="";
            z.AddEntity(pusher,9,10);z.AddEntity(actor,10,10);
            Assert.IsTrue(SkillCombatHelpers.TryPush(pusher,actor,z,2));
            Assert.AreEqual((12,10),z.GetEntityPosition(actor));
        }
    }
}
