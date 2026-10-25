using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
    public abstract class ActionFeedbackFixture : CraftingFlowFixture
    {
        EntityFactory _oldSeed;
        [SetUp] public void SetupFeedback() { _oldSeed=SeedPart.Factory;SeedPart.Factory=Factory; }
        [TearDown] public void CleanupFeedback() { SeedPart.Factory=_oldSeed; }
        protected string Status
        { get { var field=typeof(InventoryUI).GetField("_actionStatus",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(field,"Visible command status must exist.");return (string)field.GetValue(UI); } }
        protected Entity Sack(int entries=6)
        {
            var sack=Item("Sack");Assert.AreEqual(6,sack.GetPart<ContainerPart>().MaxItems);
            foreach(var bp in new[]{"Torch","SilverSand","FireClay","WardOil","HealingTonic","OvenBuildersGuide"}.Take(entries))Assert.IsTrue(sack.GetPart<ContainerPart>().AddItem(Item(bp)));
            Assert.IsTrue(Zone.AddEntity(sack,10,10));return sack;
        }
        protected object OpenAction(Entity item,string command,out int index)
        {
            Assert.IsTrue(UI.ReopenItemActionPopupFor(item));var popup=Get("_itemActionPopup");var actions=(IList)Field(popup,"Actions");index=-1;
            for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")==command){index=i;break;}
            Assert.GreaterOrEqual(index,0,"Actual item menu offers "+command);SetField(popup,"CursorIndex",index);return popup;
        }
        protected static void SetField(object owner,string field,object value) => owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(owner,value);
        protected void SelectRecipe(string id,bool mod=false)
        {
            Player.GetPart<BitLockerPart>().LearnRecipe(id);Set("_panel",2);
            var f=typeof(InventoryUI).GetField("_tinkeringMode",BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(UI,Enum.Parse(f.FieldType,mod?"Mod":"Build"));Call("Rebuild");
            var rows=(IList)Get("_tinkerRows");int index=-1;for(int i=0;i<rows.Count;i++)if(((TinkerRecipe)Field(rows[i],"Recipe")).ID==id){index=i;break;}
            Assert.GreaterOrEqual(index,0);Set("_tinkerCursorIndex",index);
        }
        protected void Tiles()
        { var grid=new GameObject("Feedback test grid");grid.transform.SetParent(UI.transform);grid.AddComponent<Grid>();var tiles=new GameObject("Feedback tiles");tiles.transform.SetParent(grid.transform);UI.Tilemap=tiles.AddComponent<Tilemap>(); }
        protected SkillScreenText[] LedgerLabels()
            => UI.GetComponentsInChildren<SkillScreenText>(true).Where(t=>t.isActiveAndEnabled&&!string.IsNullOrWhiteSpace(t.Content)).ToArray();
        protected Rect LedgerBounds(RectTransform transform)
        {
            var content=(RectTransform)Get("_ledgerContent");
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(content,transform);
            return new Rect(bounds.min.x-content.rect.xMin,content.rect.yMax-bounds.max.y,bounds.size.x,bounds.size.y);
        }
        protected bool CoveredByLaterPanel(SkillScreenText label)
        {
            var bounds=LedgerBounds(label.rectTransform);
            return UI.GetComponentsInChildren<UnityEngine.UI.Image>(true).Any(image=>image.isActiveAndEnabled
                &&image.transform.parent==label.transform.parent&&image.transform.GetSiblingIndex()>label.transform.GetSiblingIndex()
                &&image.color.a>.5f&&LedgerBounds(image.rectTransform).Overlaps(bounds));
        }
        protected void AssertRetainedLabelFits(SkillScreenText label)
        {
            var lines=label.Content.Split('\n');
            Assert.Greater(label.GlyphHeight,0);
            Assert.LessOrEqual(lines.Max(line=>line.Length)*label.GlyphHeight*.5f,label.rectTransform.rect.width+.1f,"Visible text must fit its own glyph mesh.");
            Assert.LessOrEqual(lines.Length*label.GlyphHeight+(lines.Length-1)*4,label.rectTransform.rect.height+.1f);
            var bounds=LedgerBounds(label.rectTransform);var content=(RectTransform)Get("_ledgerContent");
            Assert.GreaterOrEqual(bounds.xMin,-.1f);Assert.GreaterOrEqual(bounds.yMin,-.1f);
            Assert.LessOrEqual(bounds.xMax,content.rect.width+.1f);Assert.LessOrEqual(bounds.yMax,content.rect.height+.1f);
            Assert.IsFalse(CoveredByLaterPanel(label),"An active label beneath the modal scrim is not readable command feedback.");
        }
        protected SkillScreenText AssertStatusView(string text)
        {
            Call("Render");Canvas.ForceUpdateCanvases();Assert.Zero(UI.Tilemap.GetUsedTilesCount());
            var matches=LedgerLabels().Where(t=>t.Content==text).ToArray();Assert.AreEqual(1,matches.Length,"Show the exact failure once in the retained view.");
            var label=matches[0];AssertRetainedLabelFits(label);
            Color failureColor=(Color)typeof(InventoryUI).GetField("LRed",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(failureColor,label.color);return label;
        }
        protected SkillScreenText AssertActionLegendRemainsReadable(SkillScreenText status)
        {
            var legends=LedgerLabels().Where(t=>(t.Content.Contains("Enter:")||t.Content.Contains("A:"))&&!CoveredByLaterPanel(t)).ToArray();
            Assert.IsNotEmpty(legends,"The current keyboard/controller confirmation legend must survive the failure.");
            var legend=legends.OrderByDescending(t=>LedgerBounds(t.rectTransform).yMin).First();
            AssertRetainedLabelFits(legend);Assert.IsFalse(LedgerBounds(status.rectTransform).Overlaps(LedgerBounds(legend.rectTransform)),"Failure feedback must not replace or cover the action legend.");
            return legend;
        }
    }
    public class GameAuditActionFeedbackTests : ActionFeedbackFixture
    {
        [TestCase(false)] [TestCase(true)] public void ActualSackRefusalPreservesPopupWhileSuccessClosesIt(bool full)
        {
            var sack=Sack(full?6:5);var dagger=Carry("Dagger",1);var popup=OpenAction(dagger,"put_container",out int action);var before=Inventory.Objects.ToArray();
            Call("ExecuteItemAction",action);
            if(full){Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual(action,Field(popup,"CursorIndex"));Assert.AreEqual("Container is full.",Status);CollectionAssert.AreEqual(before,Inventory.Objects);Assert.AreSame(Player,dagger.GetPart<PhysicsPart>().InInventory);}
            else{Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsFalse(Inventory.Contains(dagger));Assert.IsTrue(sack.GetPart<ContainerPart>().Contents.Contains(dagger));}
        }
        [Test] public void SameFailedPopupRetriesAfterOneContainerSlotIsFreed()
        {
            var sack=Sack();var box=sack.GetPart<ContainerPart>();var dagger=Carry("Dagger",1);var popup=OpenAction(dagger,"put_container",out int action);
            Call("ExecuteItemAction",action);Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual("Container is full.",Status);
            Assert.IsTrue(box.RemoveItem(box.Contents[0]));Call("Rebuild");Call("Render");Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual("Container is full.",Status);
            Call("ExecuteItemAction",action);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(6,box.Contents.Count);Assert.AreEqual(1,box.Contents.Count(e=>e==dagger));Assert.IsFalse(Inventory.Contains(dagger));
        }
        [Test] public void LockedContainerShowsSpecificReasonAndCanRetryAfterUnlock()
        {
            var box=Sack(5).GetPart<ContainerPart>();box.Locked=true;var dagger=Carry("Dagger",1);var popup=OpenAction(dagger,"put_container",out int action);Call("ExecuteItemAction",action);
            Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual("Container is locked.",Status);Assert.IsTrue(Inventory.Contains(dagger));box.Locked=false;Call("ExecuteItemAction",action);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsTrue(box.Contents.Contains(dagger));
        }
        [Test] public void FullButCompletelyMergeableSackRemainsAValidAction()
        {
            var box=Sack().GetPart<ContainerPart>();var salt=Carry("SilverSand",1);var target=box.Contents.Single(e=>e.BlueprintName=="SilverSand");int before=target.GetPart<StackerPart>().StackCount;
            OpenAction(salt,"put_container",out int action);Call("ExecuteItemAction",action);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(6,box.Contents.Count);Assert.AreEqual(before+1,target.GetPart<StackerPart>().StackCount);
        }
        [TestCase("Dagger","equip_auto")] [TestCase("Dagger","drop")] [TestCase("Dagger","disassemble")] [TestCase("SteelBladeComponent","toggle_craftmark")] [TestCase("CandyCarrotSeed","PlantSeed")]
        public void StaleActionKeepsItsContextAndCannotConsumeAnotherItem(string bp,string command)
        {
            var item=bp=="SteelBladeComponent"?Steel:Carry(bp,1);var popup=OpenAction(item,command,out int action);Assert.IsTrue(Inventory.RemoveObject(item));var remaining=Inventory.Objects.ToArray();
            Call("ExecuteItemAction",action);Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual(action,Field(popup,"CursorIndex"));Assert.IsNotEmpty(Status);CollectionAssert.AreEqual(remaining,Inventory.Objects);
        }
        [TestCase(false)] [TestCase(true)] public void ActualSeedCommandFailureAndSuccessHaveTruthfulPopupOutcomes(bool plantable)
        {
            Assert.IsTrue(Zone.AddEntity(Item(plantable?"Grass":"StoneFloor"),10,10));var seed=Carry("CandyCarrotSeed",2);var popup=OpenAction(seed,"PlantSeed",out int action);Call("ExecuteItemAction",action);
            Assert.AreEqual(plantable?1:2,seed.GetPart<StackerPart>().StackCount);if(plantable){Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));}else{Assert.AreSame(popup,Get("_itemActionPopup"));Assert.IsNotEmpty(Status);}
        }
        [Test] public void IncompleteForgeShowsReasonAndCompletedKitClearsIt()
        {
            Call("ExecuteCraft",false);Assert.IsNotEmpty(Status);Assert.IsFalse(Inventory.Objects.Any(e=>e.HasPart<WeaponAssemblyPart>()));Pick(Steel);Pick(Oak);Pick(Leather);Call("ExecuteCraft",false);Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(1,Steel.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)] [TestCase(true)] public void OffStationBatchRefusesVisiblyWhileSingleForgeRemainsValid(bool batch)
        {
            Pick(Steel);Pick(Oak);Pick(Leather);Assert.IsTrue(Zone.RemoveEntity(Station));Call("Rebuild");Call("ExecuteCraft",batch);
            if(batch){Assert.IsNotEmpty(Status);Assert.AreEqual(2,Steel.GetPart<StackerPart>().StackCount);}else{Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(1,Steel.GetPart<StackerPart>().StackCount);}
        }
        [TestCase(false)] [TestCase(true)] public void BrewPreviewGateShowsFailureWithoutConsumingInvalidReagents(bool valid)
        {
            var reagent=Carry(valid?"GlimmerBrine":"FireMoss",1);BrewMode();Pick(reagent);Call("ExecuteCraft",false);
            if(valid){Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsFalse(Inventory.Contains(reagent));}else{Assert.IsNotEmpty(Status);Assert.IsTrue(Inventory.Contains(reagent));Assert.AreEqual(1,reagent.GetPart<StackerPart>().StackCount);}
        }
        [Test] public void TinkerMissingBitsShowsReasonThenRetryClearsIt()
        {
            SelectRecipe("craft_dagger");Assert.IsFalse((bool)Call("TryCraftSelectedRecipeViaCommand"));Assert.IsNotEmpty(Status);Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("craft_dagger",out var recipe));Player.GetPart<BitLockerPart>().AddBits(recipe.Cost);Call("Rebuild");Assert.IsTrue((bool)Call("TryCraftSelectedRecipeViaCommand"));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void ModificationFailureRetainsTargetAndRetryAppliesOnce()
        {
            var dagger=Carry("Dagger",1);int beforePen=dagger.GetPart<MeleeWeaponPart>().PenBonus;int beforeMods=dagger.GetIntProperty("ModificationCount");SelectRecipe("mod_sharp_melee",true);Call("OpenModTargetPopupForSelectedRecipe");var popup=Get("_modTargetPopup");Assert.NotNull(popup);var targets=(IList)Field(popup,"Targets");int row=targets.IndexOf(dagger);Assert.GreaterOrEqual(row,0);
            Call("ApplySelectedModToPopupTarget",row);Assert.AreSame(popup,Get("_modTargetPopup"));Assert.IsNotEmpty(Status);Assert.IsFalse(dagger.HasTag("ModSharp"));Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_sharp_melee",out var recipe));Player.GetPart<BitLockerPart>().AddBits(recipe.Cost);Call("ApplySelectedModToPopupTarget",row);Assert.IsNull(Get("_modTargetPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsTrue(dagger.HasTag("ModSharp"));Assert.AreEqual(beforePen+1,dagger.GetPart<MeleeWeaponPart>().PenBonus);Assert.AreEqual(beforeMods+1,dagger.GetIntProperty("ModificationCount"));
        }
        [Test] public void FailureIsDrawnInTheSharedDetailRowWithoutReplacingActionLegend()
        {
            var box=Sack().GetPart<ContainerPart>();var dagger=Carry("Dagger",1);var popup=OpenAction(dagger,"put_container",out int action);Tiles();
            var inventoryBefore=Inventory.Objects.ToArray();var contentsBefore=box.Contents.ToArray();Call("ExecuteItemAction",action);
            var status=AssertStatusView("Container is full.");var legend=AssertActionLegendRemainsReadable(status);StringAssert.Contains("select",legend.Content);
            Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual(action,Field(popup,"CursorIndex"));
            var hits=((IList)Get("_ledgerHits")).Cast<object>().ToArray();
            var retry=hits.Single(h=>Field(h,"Kind").ToString()=="PopupRow"&&(int)Field(h,"Index")==action);
            var cancel=hits.Single(h=>Field(h,"Kind").ToString()=="Cancel");
            Assert.IsFalse(((Rect)Field(retry,"Bounds")).Overlaps(LedgerBounds(status.rectTransform)));
            Assert.IsTrue(LedgerLabels().Any(t=>t.Content.Contains("Back")&&LedgerBounds(t.rectTransform).Overlaps((Rect)Field(cancel,"Bounds"))&&!CoveredByLaterPanel(t)),"The retained retry menu still exposes a readable Back action.");
            CollectionAssert.AreEqual(inventoryBefore,Inventory.Objects);CollectionAssert.AreEqual(contentsBefore,box.Contents);Assert.AreSame(Player,dagger.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase("panel")] [TestCase("popup")] [TestCase("close")] public void DeliberateContextExitClearsOldFailure(string transition)
        {
            Sack();var dagger=Carry("Dagger",1);OpenAction(dagger,"put_container",out int action);Call("ExecuteItemAction",action);Assert.IsNotEmpty(Status);
            if(transition=="panel"){Set("_itemActionPopup",null);Set("_panel",4);Call("Render");}else if(transition=="popup"){Assert.IsTrue(UI.ReopenItemActionPopupFor(Steel));}else UI.Close();Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        private sealed class EquipmentVeto : Part
        {
            public bool Block;public string Event;
            public override string Name=>"FeedbackEquipmentVeto";
            public override bool HandleEvent(GameEvent e)=>!(Block&&e.ID==Event);
        }
        private object OpenSlot(Entity item,bool equipped=false)
        {
            Set("_panel",0);Call("Rebuild");var slots=(IList)Get("_equipSlots");var type=item.GetPart<EquippablePart>().GetSlotArray()[0].Trim();object selected=null;
            foreach(var slot in slots)if(equipped?Field(slot,"EquippedItem")==item:(string)Field(slot,"SlotName")==type){selected=slot;break;}
            Assert.NotNull(selected);Call("OpenEquipPopup",selected);return Get("_equipPopup");
        }
        [TestCase(false)] [TestCase(true)] public void EquipSlotRefusalRetainsSelectionForDirectRetry(bool blocked)
        {
            var item=Carry("Dagger",1);var popup=OpenSlot(item);int row=((IList)Field(popup,"Items")).IndexOf(item);SetField(popup,"CursorIndex",row);var veto=new EquipmentVeto{Block=blocked,Event="BeforeEquip"};Player.AddPart(veto);Call("EquipFromPopup",row);
            if(blocked){Assert.AreSame(popup,Get("_equipPopup"));Assert.AreEqual(row,Field(popup,"CursorIndex"));Assert.IsNotEmpty(Status);Assert.IsFalse(InventorySystem.IsEquipped(Player,item));veto.Block=false;Call("EquipFromPopup",row);}
            Assert.IsNull(Get("_equipPopup"));Assert.IsTrue(InventorySystem.IsEquipped(Player,item));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)] public void ManualBodyPickerRefusalPreservesExactTargetForRetry(bool blocked)
        {
            var item=Carry("Dagger",1);var popup=OpenAction(item,"equip_manual",out int action);Call("ExecuteItemAction",action);Assert.IsTrue((bool)Field(popup,"InBodyPartPicker"));var part=(BodyPart)((IList)Field(popup,"BodyParts"))[0];var veto=new EquipmentVeto{Block=blocked,Event="BeforeEquip"};Player.AddPart(veto);Call("EquipToBodyPart",0);
            if(blocked){Assert.AreSame(popup,Get("_itemActionPopup"));Assert.IsTrue((bool)Field(popup,"InBodyPartPicker"));Assert.IsNotEmpty(Status);Assert.IsNull(part._Equipped);veto.Block=false;Call("EquipToBodyPart",0);}
            Assert.IsNull(Get("_itemActionPopup"));Assert.AreSame(item,part._Equipped);Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)] public void UnequipSlotRefusalRetainsEquipmentAndRetryContext(bool blocked)
        {
            var item=Carry("Dagger",1);Assert.IsTrue(InventorySystem.Equip(Player,item));var popup=OpenSlot(item,true);var veto=new EquipmentVeto{Block=blocked,Event="BeforeUnequip"};Player.AddPart(veto);Call("UnequipFromPopup");
            if(blocked){Assert.AreSame(popup,Get("_equipPopup"));Assert.IsNotEmpty(Status);Assert.IsTrue(InventorySystem.IsEquipped(Player,item));veto.Block=false;Call("UnequipFromPopup");}
            Assert.IsNull(Get("_equipPopup"));Assert.IsFalse(InventorySystem.IsEquipped(Player,item));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)] public void DisplacementRefusalKeepsConfirmationAndOriginForRetry(bool manual,bool blocked)
        {
            var old=Carry("Dagger",1);Assert.IsTrue(InventorySystem.Equip(Player,old));var target=Player.GetPart<Body>().GetParts().First(p=>p._Equipped==old);var replacement=Carry("Dagger",1);Assert.AreNotSame(old,replacement);object origin;
            if(manual){origin=OpenAction(replacement,"equip_manual",out int action);Call("ExecuteItemAction",action);var parts=(IList)Field(origin,"BodyParts");int row=parts.IndexOf(target);Assert.GreaterOrEqual(row,0);SetField(origin,"BodyPartCursor",row);Call("EquipToBodyPart",row);}
            else{origin=OpenSlot(old,true);int row=((IList)Field(origin,"Items")).IndexOf(replacement);Assert.GreaterOrEqual(row,0);Call("EquipFromPopup",row);}
            var confirm=Get("_displaceConfirm");Assert.NotNull(confirm);Assert.Greater(((IList)Field(confirm,"Displacements")).Count,0);var veto=new EquipmentVeto{Block=blocked,Event="BeforeEquip"};Player.AddPart(veto);Call("ConfirmDisplacement");
            if(blocked){Assert.AreSame(confirm,Get("_displaceConfirm"));Assert.AreSame(origin,Get(manual?"_itemActionPopup":"_equipPopup"));Assert.IsNotEmpty(Status);Assert.AreSame(old,target._Equipped);veto.Block=false;Call("ConfirmDisplacement");}
            Assert.IsNull(Get("_displaceConfirm"));Assert.IsNull(Get(manual?"_itemActionPopup":"_equipPopup"));Assert.AreSame(replacement,target._Equipped);Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void ReopeningInventoryStartsWithoutPreviousFailure()
        { Sack();var dagger=Carry("Dagger",1);OpenAction(dagger,"put_container",out int action);Call("ExecuteItemAction",action);Assert.IsNotEmpty(Status);UI.Open();Assert.IsTrue(string.IsNullOrEmpty(Status)); }
    }
}
