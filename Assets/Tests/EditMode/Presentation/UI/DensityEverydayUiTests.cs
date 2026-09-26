using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class DensityEverydayUiTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        HotbarSaveFixture scope; GameObject host; InventoryUI ui; Entity actor;
        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            host = new GameObject("Everyday inventory tests"); host.transform.SetParent(scope.Root.transform,false);
            var grid = new GameObject("Grid"); grid.transform.SetParent(host.transform,false); grid.AddComponent<Grid>();
            var tiles = new GameObject("Tiles"); tiles.transform.SetParent(grid.transform,false); var tilemap=tiles.AddComponent<Tilemap>(); tiles.AddComponent<TilemapRenderer>();
            actor=new Entity { ID="everyday-ui-player", BlueprintName="Player" }; actor.SetTag("Player"); actor.AddPart(new InventoryPart());
            ui=host.AddComponent<InventoryUI>(); ui.Tilemap=tilemap; ui.PlayerEntity=actor; ui.CurrentZone=new Zone("EverydayUI");
            ui.CurrentZone.AddEntity(actor,10,10); ui.Open();
        }
        [TearDown] public void TearDown() { if(ui!=null)ui.Close(); if(host!=null)Object.DestroyImmediate(host); scope?.Dispose(); }
        object Get(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
        object Call(object target,string name,params object[] args)
        { var method=target.GetType().GetMethod(name,Flags); Assert.NotNull(method,name); return method.Invoke(target,args); }
        Entity Select(string command,bool accept)
        {
            var item=new Entity { ID="everyday-ui-item",BlueprintName="TestFood" }; item.SetTag("Item");
            item.AddPart(new RenderPart { DisplayName="test supply" }); item.AddPart(new PhysicsPart { Takeable=true });
            item.AddPart(new RoutedActionPart { Command=command,Accept=accept }); actor.GetPart<InventoryPart>().AddObject(item);
            Assert.True(ui.ReopenItemActionPopupFor(item));
            var actions=(IList)Get(Get(ui,"_itemActionPopup"),"Actions");
            int index=-1; for(int i=0;i<actions.Count;i++)if((string)Get(actions[i],"Command")==command)index=i;
            Assert.That(index,Is.GreaterThanOrEqualTo(0)); Call(ui,"ExecuteItemAction",index); return item;
        }
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")][TestCase("Cook")][TestCase("LightTorch")][TestCase("ExtinguishTorch")]
        public void SuccessfulEverydayAction_ClosesInventoryAndQueuesExactlyOneTurn(string command)
        { Select(command,true); Assert.False(ui.IsOpen); Assert.True((bool)Call(ui,"ConsumePendingEverydayTurn")); Assert.False((bool)Call(ui,"ConsumePendingEverydayTurn")); }
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")][TestCase("Cook")][TestCase("LightTorch")][TestCase("ExtinguishTorch")]
        public void Refusal_RetainsMenuAndQueuesNoTurn(string command)
        { Select(command,false); Assert.True(ui.IsOpen); Assert.NotNull(Get(ui,"_itemActionPopup")); Assert.That((string)Get(ui,"_actionStatus"),Is.Not.Empty); Assert.False((bool)Call(ui,"ConsumePendingEverydayTurn")); }
        [Test] public void ExistingUnrelatedAction_KeepsItsTiming()
        { Select("TestNoTurn",true); Assert.True(ui.IsOpen); Assert.False((bool)Call(ui,"ConsumePendingEverydayTurn")); }
        [Test] public void ReopeningInventory_DiscardsUnconsumedSignal()
        { Select("Cook",true); ui.Open(); Assert.False((bool)Call(ui,"ConsumePendingEverydayTurn")); }
        [TestCase(0,0,"Dawn")][TestCase(300,0,"Height")][TestCase(600,0,"Dusk")][TestCase(900,0,"Dark")]
        [TestCase(0,1,"Bright")][TestCase(300,1,"Half-Bright")][TestCase(600,1,"Dim-Down")][TestCase(900,1,"Dark-Watch")]
        public void Sidebar_ShowsCurrentBandInDepthVocabulary(int tick,int depth,string band)
        { var clock=new TurnManager(); clock.AdvanceClock(tick); var snapshot=SidebarStateBuilder.Build(actor,new Zone("Overworld.1.1."+depth),null); Assert.Contains("TIME "+band,snapshot.VitalLines.ToList()); Assert.That(snapshot.VitalLines.Count,Is.EqualTo(5)); }
        public class RoutedActionPart : Part
        {
            public string Command; public bool Accept; public override string Name=>"EverydayRoutedTestAction";
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="GetInventoryActions")e.GetParameter<InventoryActionList>("Actions")?.AddAction(Command,Command,Command,'c',25);
                if(e.ID=="InventoryAction"&&e.GetStringParameter("Command")==Command&&Accept){e.Handled=true;return false;}
                return true;
            }
        }
    }
}
