using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>Native InputHandler world-menu payment; core Part tests cannot establish this route.</summary>
    public sealed class DensityGeneratedDoorMenuTests : ShortcutFixture
    {
        InputHandler Input()
        {
            var input=Component<InputHandler>();input.PlayerEntity=Player;input.CurrentZone=Zone;
            var turns=new TurnManager();turns.AddEntity(Player);turns.ProcessUntilPlayerTurn();input.TurnManager=turns;return input;
        }
        void Execute(InputHandler input,Entity door,string command)=>Call(input,"ExecuteWorldActionSelection",new InventoryAction("Door","door",command,'o',30),door,Zone.GetEntityCell(door),false);
        [TestCase(true)][TestCase(false)]
        public void ExplicitOrdinaryDoorStateChangeCostsExactlyOneAction(bool open)
        {
            var door=Item("VillageDoor");var part=door.GetPart<DoorPart>();part.IsOpen=!open;Assert.True(Zone.AddEntity(door,11,10));
            var input=Input();int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player);
            Execute(input,door,open?DoorPart.OpenCommand:DoorPart.CloseCommand);
            Assert.AreEqual(open,part.IsOpen);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));Assert.Greater(input.TurnManager.TickCount,tick);
            Assert.AreEqual(energy-TurnManager.ActionThreshold+(input.TurnManager.TickCount-tick)*Player.GetStatValue("Speed",100),input.TurnManager.GetEnergy(Player));
        }
        [TestCase("occupied")][TestCase("foreign-owner")]
        public void RefusedOrdinaryDoorMenuRemainsFree(string fault)
        {
            var door=Item("VillageDoor");Assert.True(Zone.AddEntity(door,11,10));var part=door.GetPart<DoorPart>();
            if(fault=="occupied")Assert.True(Zone.AddEntity(Item("Dagger"),11,10));else part.OwnerId="someone-else";
            var input=Input();int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player);
            Execute(input,door,DoorPart.CloseCommand);Assert.True(part.IsOpen);Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));
        }
    }
}
