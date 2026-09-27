using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Tests
{
    public sealed class DensityHarvestNativeUiTests : ShortcutFixture
    {
        EntityFactory old;
        [SetUp]public void SetupHarvest(){old=HarvestablePart.Factory;HarvestablePart.Factory=Factory;}
        [TearDown]public void RestoreHarvest(){HarvestablePart.Factory=old;}
        InputHandler Input(){var input=Component<InputHandler>();input.PlayerEntity=Player;input.CurrentZone=Zone;var turns=new TurnManager();turns.AddEntity(Player);turns.ProcessUntilPlayerTurn();input.TurnManager=turns;return input;}
        int Count()=>Inventory.Objects.Where(e=>e.BlueprintName=="Bone").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        bool Paid(InputHandler input,int tick,int energy)=>input.TurnManager.TickCount>tick&&input.TurnManager.GetEnergy(Player)==energy-TurnManager.ActionThreshold+(input.TurnManager.TickCount-tick)*Player.GetStatValue("Speed",TurnManager.DefaultSpeed);
        [TestCase("live")][TestCase("spent")][TestCase("remote")][TestCase("removed")]
        public void WorldHarvestPaysExactlyOnCommittedFiniteYield(string state)
        {
            var source=Item("Bones");Assert.True(Zone.AddEntity(source,11,10));var part=source.GetPart<HarvestablePart>();
            if(state=="spent")part.Harvested=true;if(state=="remote")Assert.True(Zone.MoveEntity(source,18,10));if(state=="removed")Assert.True(Zone.RemoveEntity(source));
            var input=Input();int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player),before=Count();
            Call(input,"ExecuteWorldActionSelection",new InventoryAction("Harvest","harvest","Harvest",'h',20),source,Zone.GetCell(11,10),false);
            if(state=="live"){Assert.That(Count()-before,Is.InRange(1,2));Assert.True(part.Harvested);Assert.IsNull(Zone.GetEntityCell(source));Assert.True(Paid(input,tick,energy));}
            else{Assert.AreEqual(before,Count());Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));if(state!="removed")Assert.NotNull(Zone.GetEntityCell(source));}
        }
        [TestCase(false)][TestCase(true)]
        public void CarriedHarvestUsesSamePaymentAndStaleOwnershipStaysFree(bool removedAfterMenu)
        {
            var source=Item("CreatureCorpse");source.AddPart(new HarvestablePart{YieldBlueprint="Bone",YieldMin=1,YieldMax=1});Assert.True(Inventory.AddObject(source));
            var ui=Component<InventoryUI>();ui.Tilemap=Tiles();ui.PlayerEntity=Player;ui.CurrentZone=Zone;ui.Open();Assert.True(ui.ReopenItemActionPopupFor(source));
            var popup=Get(ui,"_itemActionPopup");var actions=(IList)Get(popup,"Actions");int index=-1;for(int i=0;i<actions.Count;i++)if((string)Get(actions[i],"Command")=="Harvest")index=i;Assert.GreaterOrEqual(index,0);Set(popup,"CursorIndex",index);
            if(removedAfterMenu)Assert.True(InventorySystem.Drop(Player,source,Zone));
            var input=Input();input.InventoryUI=ui;State(input,"_inputState","InventoryOpen");int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player),before=Count();
            Press(Key.Enter,()=>Call(input,"HandleInventoryInput"));
            if(!removedAfterMenu){Assert.AreEqual(before+1,Count());Assert.False(Inventory.Contains(source));Assert.False(ui.IsOpen);Assert.True(Paid(input,tick,energy));}
            else{Assert.AreEqual(before,Count());Assert.True(ui.IsOpen);Assert.AreSame(popup,Get(ui,"_itemActionPopup"));Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));Assert.NotNull(Zone.GetEntityCell(source));}
        }
    }
}
