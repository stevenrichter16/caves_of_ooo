using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    public sealed class DensityDoorInputTests : ShortcutFixture
    {
        [TestCase(true)] [TestCase(false)]
        public void SuccessfulBumpUnlockCostsOneActionAndNeverAlsoStrikes(bool breakable)
        {
            Player.RemovePart(Player.GetPart<BitLockerPart>());
            var door=Item("LockedDoor");var structure=door.GetPart<DestructiblePart>();structure.Hardness=0;
            if(!breakable)door.RemovePart(door.GetPart<DestructiblePart>());
            Assert.True(Zone.AddEntity(door,11,10));Give(Player,"IronKey");
            var input=MakeInput();int hp=structure.HP,energy=input.TurnManager.GetEnergy(Player),tick=input.TurnManager.TickCount;
            Press(Key.D,()=>Call(input,"Update"));
            Assert.False(door.GetPart<LockPart>().IsLocked);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));
            Assert.AreEqual(hp,structure.HP,"Unlocking must never fall through to bump-to-break on the same press.");
            Assert.AreEqual(energy-TurnManager.ActionThreshold+(input.TurnManager.TickCount-tick)*Player.GetStatValue("Speed",100),input.TurnManager.GetEnergy(Player));
            Assert.Greater(input.TurnManager.TickCount,tick,"Unlock must be a paid ordinary action even without a destruction fallback.");
        }
        [Test]public void MissingKeyUnbreakableControlRemainsStationaryAndFree()
        {
            Player.RemovePart(Player.GetPart<BitLockerPart>());var door=Item("LockedDoor");door.RemovePart(door.GetPart<DestructiblePart>());Assert.True(Zone.AddEntity(door,11,10));
            var input=MakeInput();int energy=input.TurnManager.GetEnergy(Player),tick=input.TurnManager.TickCount;
            Press(Key.D,()=>Call(input,"Update"));Assert.True(door.GetPart<LockPart>().IsLocked);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));
        }
        [Test]public void OrdinaryBreakableBlockerStillTakesOneStrike()
        {
            Player.RemovePart(Player.GetPart<BitLockerPart>());var target=Item("LockedDoor");target.RemovePart(target.GetPart<LockPart>());target.GetPart<DestructiblePart>().Hardness=0;Assert.True(Zone.AddEntity(target,11,10));
            var input=MakeInput();int hp=target.GetPart<DestructiblePart>().HP,tick=input.TurnManager.TickCount;
            Press(Key.D,()=>Call(input,"Update"));Assert.Less(target.GetPart<DestructiblePart>().HP,hp);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));Assert.Greater(input.TurnManager.TickCount,tick);
        }
        InputHandler MakeInput()
        {
            var input=Component<InputHandler>();input.PlayerEntity=Player;input.CurrentZone=Zone;input.MoveRepeatDelay=0;Set(input,"_lastMoveTime",-999f);
            var turns=new TurnManager();turns.AddEntity(Player);turns.ProcessUntilPlayerTurn();input.TurnManager=turns;return input;
        }
    }
}
