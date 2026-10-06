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
    public sealed class FiftySecondActionUiTests
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
        [TestCase("PrepareRecipe|shape_haft", true)]
        [TestCase("PrepareRecipe|shape_haft", false)]
        [TestCase("SalvageForgedWeapon", true)]
        [TestCase("SalvageForgedWeapon", false)]
        [TestCase("RefuelTorch|torch-owner", true)]
        [TestCase("RefuelTorch|torch-owner", false)]
        [TestCase("CompostCrop|crop-owner", true)]
        [TestCase("CompostCrop|crop-owner", false)]
        [TestCase("HarvestCropSeeds", true)]
        [TestCase("HarvestCropSeeds", false)]
        [TestCase("TransplantCrop|zone|10|11", true)]
        [TestCase("TransplantCrop|zone|10|11", false)]
        [TestCase("InfuseMineral|weapon-owner", true)]
        [TestCase("InfuseMineral|weapon-owner", false)]
        [TestCase("CopyVolume|book-owner", true)]
        [TestCase("CopyVolume|book-owner", false)]
        [TestCase("StripClothScreen", true)]
        [TestCase("StripClothScreen", false)]
        [TestCase("BorrowVolume|book-owner", true)]
        [TestCase("BorrowVolume|book-owner", false)]
        public void NewPhysicalCommandsQueueOneTurnOnlyAfterActualSuccess(string command, bool accept)
        {
            Select(command, accept);
            Assert.AreEqual(!accept, ui.IsOpen);
            Assert.AreEqual(accept, (bool)Call(ui, "ConsumePendingEverydayTurn"));
            Assert.False((bool)Call(ui, "ConsumePendingEverydayTurn"), "Receipt consumed exactly once.");
            if (!accept) Assert.NotNull(Get(ui, "_itemActionPopup"), "Refusal retains retry context.");
        }
        [TestCase("TestNoTurn")]
        [TestCase("CopyVolumeUnknown|book-owner")]
        [TestCase("PrepareRecipeUnknown|shape_haft")]
        public void UnknownCommandsRetainTheirOriginalFreeTiming(string command)
        { Select(command, true); Assert.True(ui.IsOpen); Assert.False((bool)Call(ui, "ConsumePendingEverydayTurn")); }
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
