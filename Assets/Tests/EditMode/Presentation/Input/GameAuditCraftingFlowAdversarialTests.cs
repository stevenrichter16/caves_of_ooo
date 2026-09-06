using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
    public class GameAuditCraftingFlowAdversarialTests : CraftingFlowFixture
    {
        [TestCase(false)] [TestCase(true)] public void LegacyWinnerUsesMarkedOrderAndRetainsExactPayload(bool reversed)
        {
            var willow=Carry("WillowHaftComponent");
            CraftingMarkPart.Toggle(Steel);CraftingMarkPart.Toggle(Iron);CraftingMarkPart.Toggle(Oak);CraftingMarkPart.Toggle(willow);
            if(reversed){Inventory.Objects.Remove(Steel);Inventory.Objects.Add(Steel);}
            var expected=reversed?Steel:Iron;var marker=expected.GetPart<CraftingMarkPart>();var order=Inventory.Objects.ToArray();var counts=order.Select(e=>e.GetPart<StackerPart>().StackCount).ToArray();
            Call("Rebuild");Assert.AreSame(expected,Get("_pickedBlade"));Assert.AreSame(marker,expected.GetPart<CraftingMarkPart>());Assert.IsFalse(CraftingMarkPart.IsMarked(reversed?Iron:Steel));
            Assert.AreSame(willow,Get("_pickedHaft"));Assert.IsFalse(CraftingMarkPart.IsMarked(Oak));CollectionAssert.AreEqual(order,Inventory.Objects);CollectionAssert.AreEqual(counts,order.Select(e=>e.GetPart<StackerPart>().StackCount));Assert.IsTrue(order.All(e=>e.GetPart<PhysicsPart>().InInventory==Player));
        }
        [TestCase(false)] [TestCase(true)] public void SerializedMarkGraphNormalizesOnlyWhenInventoryUiRebuilds(bool duplicate)
        {
            CraftingMarkPart.Toggle(Iron);if(duplicate)CraftingMarkPart.Toggle(Steel);
            var restored=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Player);
            var restoredInv=restored.GetPart<InventoryPart>();var steel=restoredInv.Objects.Single(e=>e.BlueprintName=="SteelBladeComponent");var iron=restoredInv.Objects.Single(e=>e.BlueprintName=="IronSpikeComponent");
            Assert.AreEqual(duplicate,CraftingMarkPart.IsMarked(steel));Assert.IsTrue(CraftingMarkPart.IsMarked(iron));var marker=iron.GetPart<CraftingMarkPart>();
            UI.PlayerEntity=restored;Call("Rebuild");Assert.IsFalse(CraftingMarkPart.IsMarked(steel));Assert.AreSame(marker,iron.GetPart<CraftingMarkPart>());Assert.AreSame(iron,Get("_pickedBlade"));
            Assert.AreEqual(2,steel.GetPart<StackerPart>().StackCount);Assert.AreEqual(2,iron.GetPart<StackerPart>().StackCount);Assert.IsTrue(restoredInv.Objects.All(e=>e.GetPart<PhysicsPart>().InInventory==restored));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] public void RebuildModeAndReopenKeepSurvivingMarks(int transition)
        {
            Pick(Iron);Pick(Oak);var marker=Iron.GetPart<CraftingMarkPart>();
            if(transition==0)for(int i=0;i<20;i++)Call("Rebuild");
            else if(transition==1){BrewMode();var f=typeof(InventoryUI).GetField("_craftingMode",BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(UI,Enum.Parse(f.FieldType,"Forge"));Call("Rebuild");}
            else{UI.Close();UI.Open();Set("_panel",4);}
            Assert.AreSame(marker,Iron.GetPart<CraftingMarkPart>());Assert.IsTrue(CraftingMarkPart.IsMarked(Oak));Assert.AreSame(Iron,Get("_pickedBlade"));
        }
        [Test] public void UnmarkedLaterAlternativeDoesNotEvictMarkedWinner()
        { Pick(Steel);Call("Rebuild");Assert.AreSame(Steel,Get("_pickedBlade"));Assert.IsFalse(CraftingMarkPart.IsMarked(Iron)); }
        [TestCase(false)] [TestCase(true)] public void TransferredStaleRowCannotToggleAnotherActorsMark(bool marked)
        {
            if(marked)Pick(Steel);int row=Row(Steel);var marker=Steel.GetPart<CraftingMarkPart>();var other=Actor();Assert.IsTrue(Inventory.RemoveObject(Steel));Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(Steel));
            Set("_craftCursorIndex",row);Call("ToggleCraftPickUnderCursor");Assert.AreEqual(marked,CraftingMarkPart.IsMarked(Steel));Assert.AreSame(marker,Steel.GetPart<CraftingMarkPart>());Assert.AreSame(other,Steel.GetPart<PhysicsPart>().InInventory);Assert.IsFalse(CraftingMarkPart.IsMarked(Iron));
        }
        [TestCase(false)] [TestCase(true)] public void StaleCompletePreviewPaysNothingWhenOneComponentLeaves(bool removed)
        {
            Pick(Steel);Pick(Oak);Pick(Leather);if(removed)Assert.IsTrue(Inventory.RemoveObject(Oak));Call("ExecuteCraft",false);
            Assert.AreEqual(removed?2:1,Steel.GetPart<StackerPart>().StackCount);Assert.AreEqual(removed?2:1,Leather.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(removed?0:1,Inventory.Objects.Count(e=>e.GetPart<WeaponAssemblyPart>()!=null));Assert.AreEqual(removed?2:1,Oak.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)] [TestCase(true)] public void BatchPaysTwoKitsAndOptionalQuenchChangesOnlyOneWeapon(bool quench)
        {
            var medium=quench?Brew(Player,false,true):null;Call("Rebuild");Pick(Steel);Pick(Oak);Pick(Leather);if(quench)Pick(medium);Call("ExecuteCraft",true);
            var weapons=Inventory.Objects.Where(e=>e.GetPart<WeaponAssemblyPart>()!=null).ToArray();Assert.AreEqual(2,weapons.Sum(e=>e.GetPart<StackerPart>().StackCount));
            Assert.AreEqual(quench?1:0,weapons.Where(e=>e.GetPart<WeaponTemperPart>()?.TemperCount>0).Sum(e=>e.GetPart<StackerPart>().StackCount));Assert.AreEqual(quench?1:2,weapons.Where(e=>!(e.GetPart<WeaponTemperPart>()?.TemperCount>0)).Sum(e=>e.GetPart<StackerPart>().StackCount));Assert.IsFalse(Inventory.Contains(Steel));Assert.IsFalse(Inventory.Contains(Oak));Assert.IsFalse(Inventory.Contains(Leather));
            Assert.AreEqual(2,Iron.GetPart<StackerPart>().StackCount);Assert.IsNull(Get("_pickedBlade"));if(quench)Assert.IsFalse(Inventory.Contains(medium));
        }
        void ManyBlades(int count)
        {
            foreach(var item in Inventory.Objects.ToArray())Inventory.RemoveObject(item);
            for(int i=0;i<count;i++){var blade=Item("SteelBladeComponent");blade.GetPart<StackerPart>().MaxStack=1;Assert.IsTrue(Inventory.AddObject(blade));}
            Oak=Carry("OakHaftComponent");Leather=Carry("LeatherBindingComponent");Call("Rebuild");
        }
        [TestCase(31)] [TestCase(32)] [TestCase(35)] public void KeyboardScrollKeepsFinalSectionVisibleDespiteSpacers(int blades)
        {
            ManyBlades(blades);Set("_craftCursorIndex",Row(Leather));Call("ClampCraftCursor");Assert.AreEqual(Row(Leather),Hit(13,40));Assert.Greater((int)Get("_craftScrollOffset"),0);
            Call("HandleCraftingClick",new Vector2Int(13,40));Assert.IsTrue(CraftingMarkPart.IsMarked(Leather));Assert.IsFalse(CraftingMarkPart.IsMarked(Oak));
            Call("MoveCraftCursor",-1);Assert.AreEqual(Row(Oak),Get("_craftCursorIndex"));Assert.IsTrue((bool)Call("CraftCursorIsVisible"));
        }
        [TestCase(35,false)] [TestCase(36,false)] [TestCase(37,true)] public void ExactFitAndOverflowArrowsReflectActualRemainingRows(int reagents,bool more)
        {
            foreach(var item in Inventory.Objects.ToArray())Inventory.RemoveObject(item);
            for(int i=0;i<reagents;i++){var item=Item("FireMoss");item.GetPart<StackerPart>().MaxStack=1;Assert.IsTrue(Inventory.AddObject(item));}
            BrewMode();Set("_craftScrollOffset",0);Call("BuildCraftingLayout");Assert.AreEqual(more,Get("_craftHasMoreBelow"));
            Assert.AreEqual(reagents>=36?36:-1,Hit(13,40));Assert.AreEqual(-1,Hit(47,40));
        }
        [TestCase(false)] [TestCase(true)] public void EmptyModeOrRemovedInventoryClearsAllStaleHits(bool removeInventory)
        {
            ManyBlades(40);Set("_craftCursorIndex",Row(Leather));Call("ClampCraftCursor");
            if(removeInventory){Player.RemovePart(Inventory);Call("BuildCraftingRows");}else BrewMode();
            for(int y=4;y<=40;y++)Assert.AreEqual(-1,Hit(13,y));Assert.IsFalse((bool)Get("_craftHasMoreBelow"));
        }
        [Test] public void ShrinkingInventoryClampsOldScrollAndSelection()
        {
            ManyBlades(40);Set("_craftCursorIndex",Row(Leather));Call("ClampCraftCursor");foreach(var item in Inventory.Objects.ToArray())if(item!=Oak)Inventory.RemoveObject(item);
            Call("Rebuild");Assert.AreEqual(Row(Oak),Get("_craftCursorIndex"));Assert.IsTrue((bool)Call("CraftCursorIsVisible"));Assert.IsTrue(((int[])Get("_craftScreenRows")).All(i=>i<((IList)Get("_craftRows")).Count));
        }
        [TestCase(1,5,true)] [TestCase(46,5,true)] [TestCase(47,5,false)] [TestCase(13,3,false)] [TestCase(13,41,false)]
        public void HitBoundariesFollowTheVisibleList(int x,int y,bool valid)
        {Assert.AreEqual(valid?Row(Steel):-1,Hit(x,y));}
        [TestCase(-1,-1)] [TestCase(52,9)] public void LeavingThenReturningToPreviousCellCountsAsPointerMovement(int x,int y)
        {
            PointerAt(13,5);Call("UpdateMouseHover");Call("MoveCraftCursor",1);Assert.AreEqual(Row(Iron),Get("_craftCursorIndex"));
            PointerAt(x,y);Call("UpdateMouseHover");Assert.AreEqual(4,Get("_panel"));PointerAt(13,5);Call("UpdateMouseHover");Assert.AreEqual(Row(Steel),Get("_craftCursorIndex"));
        }
        [TestCase(false)] [TestCase(true)] public void ParkedPointerCannotUndoTabBetweenInventoryAndEquipment(bool overEquipment)
        {
            int x=13,y=9;
            if(!overEquipment){var rows=(IList)Get("_rows");int row=-1;for(int i=0;i<rows.Count;i++)if(!(bool)Field(rows[i],"IsHeader")){row=i;break;}Assert.GreaterOrEqual(row,0);x=40;y=3+row;}
            Set("_panel",overEquipment?0:1);PointerAt(x,y);Call("UpdateMouseHover");Assert.AreEqual(overEquipment?0:1,Get("_panel"));
            Set("_panel",overEquipment?1:0);Call("UpdateMouseHover");Assert.AreEqual(overEquipment?1:0,Get("_panel"));
            PointerAt(-1,-1);Call("UpdateMouseHover");PointerAt(x,y);Call("UpdateMouseHover");Assert.AreEqual(overEquipment?0:1,Get("_panel"));
        }
        [Test] public void ParkedPointerCannotUndoPopupKeyboardCursor()
        {
            Assert.IsTrue(UI.ReopenItemActionPopupFor(Steel));var popup=Get("_itemActionPopup");int count=((IList)Field(popup,"Actions")).Count;Assert.Greater(count,1);
            int y=(45-(Math.Min(count,25)+4))/2+3;PointerAt(20,y);Call("UpdateMouseHover");Assert.AreEqual(0,Field(popup,"CursorIndex"));
            popup.GetType().GetField("CursorIndex").SetValue(popup,1);Call("UpdateMouseHover");Assert.AreEqual(1,Field(popup,"CursorIndex"));
            PointerAt(-1,-1);Call("UpdateMouseHover");PointerAt(20,y);Call("UpdateMouseHover");Assert.AreEqual(0,Field(popup,"CursorIndex"));
        }
        [Test] public void EnteringCraftingUnderParkedPointerDoesNotOverrideKeyboardSelection()
        {
            Set("_panel",0);PointerAt(13,9);Call("UpdateMouseHover");Set("_panel",4);Set("_craftCursorIndex",Row(Iron));Call("UpdateMouseHover");Assert.AreEqual(Row(Iron),Get("_craftCursorIndex"));
            PointerAt(13,5);Call("UpdateMouseHover");Assert.AreEqual(Row(Steel),Get("_craftCursorIndex"));
        }
        [TestCase(false)] [TestCase(true)] public void PopupOwnsHoverAndDismissalDoesNotInventMovement(bool popup)
        {
            if(popup){Assert.IsTrue(UI.ReopenItemActionPopupFor(Steel));Set("_panel",4);}Set("_craftCursorIndex",Row(Iron));PointerAt(13,9);Call("UpdateMouseHover");
            Assert.AreEqual(popup?Row(Iron):Row(Oak),Get("_craftCursorIndex"));if(popup){Set("_itemActionPopup",null);Call("UpdateMouseHover");Assert.AreEqual(Row(Iron),Get("_craftCursorIndex"));}
        }
        [Test] public void RebuildDoesNotRestoreParkedPointerSelection()
        {PointerAt(13,5);Call("UpdateMouseHover");Pick(Iron);Call("UpdateMouseHover");Assert.AreEqual(Row(Iron),Get("_craftCursorIndex"));Assert.IsFalse(CraftingMarkPart.IsMarked(Steel));}
        [Test] public void UnchangedHoverAllocatesNothingAndDoesNotRedraw()
        {
            PointerAt(13,5);var method=typeof(InventoryUI).GetMethod("UpdateMouseHover",BindingFlags.Instance|BindingFlags.NonPublic);var hover=(Action)Delegate.CreateDelegate(typeof(Action),UI,method);
            for(int i=0;i<100;i++)hover();int renders=PerformanceDiagnostics.CurrentInventorySessionRenderCount;long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)hover();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(0,allocated);Assert.AreEqual(renders,PerformanceDiagnostics.CurrentInventorySessionRenderCount);
            PointerAt(13,9);hover();Assert.AreEqual(renders+1,PerformanceDiagnostics.CurrentInventorySessionRenderCount);
        }
        [TestCase(false)] [TestCase(true)] public void RenderedSelectionTileMatchesHitMapBeforeAndAfterScroll(bool scroll)
        {
            if(scroll){ManyBlades(31);Set("_craftCursorIndex",Row(Leather));Call("ClampCraftCursor");}else Set("_craftCursorIndex",Row(Oak));
            var grid=new GameObject("Flow tile test grid");grid.transform.SetParent(UI.transform);grid.AddComponent<Grid>();var tiles=new GameObject("Flow tile test");tiles.transform.SetParent(grid.transform);UI.Tilemap=tiles.AddComponent<Tilemap>();
            Call("Render");int y=scroll?40:9;Assert.AreEqual((int)Get("_craftCursorIndex"),Hit(13,y));Assert.AreSame(CP437TilesetGenerator.GetUiTile('>'),UI.Tilemap.GetTile(new Vector3Int(1,44-y,0)));
            Assert.AreNotSame(CP437TilesetGenerator.GetUiTile('>'),UI.Tilemap.GetTile(new Vector3Int(1,44-(y-1),0)));
        }
    }
}
