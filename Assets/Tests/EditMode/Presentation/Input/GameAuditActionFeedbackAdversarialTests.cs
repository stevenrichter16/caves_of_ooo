using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class GameAuditActionFeedbackAdversarialTests : ActionFeedbackFixture
    {
        [TestCase(1)] [TestCase(4)] public void RepeatedFullSackRefusalNeverChangesItemsOrSelection(int attempts)
        {
            var box=Sack().GetPart<ContainerPart>();var item=Carry("Dagger",2);var popup=OpenAction(item,"put_container",out int action);var before=Inventory.Objects.ToArray();var contents=box.Contents.ToArray();
            for(int i=0;i<attempts;i++){Call("ExecuteItemAction",action);Call("Rebuild");Call("Render");Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual(action,Field(popup,"CursorIndex"));Assert.AreEqual("Container is full.",Status);CollectionAssert.AreEqual(before,Inventory.Objects);CollectionAssert.AreEqual(contents,box.Contents);Assert.AreEqual(2,item.GetPart<StackerPart>().StackCount);}
        }
        [TestCase(false)] [TestCase(true)] public void EquippedPutRefusalKeepsBodyAndRetryTransfersTheExactWeapon(bool locked)
        {
            var box=Sack(locked?5:6).GetPart<ContainerPart>();box.Locked=locked;var item=Carry("Dagger",1);Assert.IsTrue(InventorySystem.Equip(Player,item));var parts=Player.GetPart<Body>().GetParts().Where(p=>p._Equipped==item).ToArray();Assert.IsNotEmpty(parts);var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);
            Assert.AreSame(popup,Get("_itemActionPopup"));Assert.IsTrue(parts.All(p=>p._Equipped==item));Assert.AreEqual(locked?"Container is locked.":"Container is full.",Status);if(locked)box.Locked=false;else Assert.IsTrue(box.RemoveItem(box.Contents[0]));Call("ExecuteItemAction",action);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(parts.All(p=>p._Equipped!=item));Assert.IsTrue(box.Contents.Contains(item));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)] public void NewRefusalReplacesThePreviousReason(bool lockedFirst)
        {
            var box=Sack().GetPart<ContainerPart>();box.Locked=lockedFirst;var item=Carry("Dagger",1);var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);Assert.AreEqual(lockedFirst?"Container is locked.":"Container is full.",Status);box.Locked=!lockedFirst;Call("ExecuteItemAction",action);Assert.AreSame(popup,Get("_itemActionPopup"));Assert.AreEqual(lockedFirst?"Container is full.":"Container is locked.",Status);
        }
        [TestCase(null)] [TestCase("")] [TestCase("   ")] [TestCase("Specific refusal.")]
        public void MissingCommandReasonHasSafeVisibleFallback(string message)
        { Call("LogCommandFailure","FeedbackFixture",InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,message));Assert.AreEqual(string.IsNullOrWhiteSpace(message)?"That action could not be completed.":message,Status); }
        [Test] public void NullCommandResultAlsoProducesSafeVisibleFallback()
        { Call("LogCommandFailure","FeedbackFixture",(object)null);Assert.AreEqual("That action could not be completed.",Status); }
        [Test] public void PopupCursorMovementRetainsTheFailureUntilAnotherAction()
        {
            Sack();var item=Carry("Dagger",1);var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);SetField(popup,"CursorIndex",0);Call("Render");Assert.AreEqual("Container is full.",Status);Assert.AreSame(popup,Get("_itemActionPopup"));
        }
        [Test] public void CraftCursorMovementRetainsFailureButModeChangeClearsIt()
        {
            Call("ExecuteCraft",false);string failure=Status;Assert.IsNotEmpty(failure);Call("MoveCraftCursor",1);Call("Render");Assert.AreEqual(failure,Status);BrewMode();Call("Render");Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void DifferentTinkerRecipeClearsThePreviousRecipeFailure()
        {
            SelectRecipe("craft_dagger");Call("TryCraftSelectedRecipeViaCommand");Assert.IsNotEmpty(Status);SelectRecipe("craft_short_sword");Call("Render");Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void SuccessfulDifferentActionFromRetainedPopupClearsFailure()
        {
            Sack();var item=Carry("Dagger",1);var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);var actions=(IList)Field(popup,"Actions");int drop=-1;for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")=="drop")drop=i;Assert.GreaterOrEqual(drop,0);Call("ExecuteItemAction",drop);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsFalse(Inventory.Contains(item));Assert.AreSame(Zone.GetEntityCell(Player),Zone.GetEntityCell(item));
        }
        [Test] public void ExamineHandoffStillQueuesAnnouncementAndClearsFailure()
        {
            Sack();var item=Carry("HealingTonic",1);var box=Zone.GetAllEntities().Single(e=>e.BlueprintName=="Sack").GetPart<ContainerPart>();box.Locked=true;var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);var actions=(IList)Field(popup,"Actions");int examine=-1;for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")=="examine_tonic")examine=i;Assert.GreaterOrEqual(examine,0);Call("ExecuteItemAction",examine);Assert.IsNull(Get("_itemActionPopup"));Assert.IsTrue(UI.IsOpen);Assert.IsTrue(MessageLog.HasPendingAnnouncement);Assert.IsTrue(Inventory.Contains(item));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void ThrowHandoffStillClosesInventoryWithExactPendingItem()
        {
            Sack();var item=Carry("Dagger",1);var popup=OpenAction(item,"put_container",out int action);Call("ExecuteItemAction",action);var actions=(IList)Field(popup,"Actions");int throwing=-1;for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")=="throw")throwing=i;Assert.GreaterOrEqual(throwing,0);Call("ExecuteItemAction",throwing);Assert.IsFalse(UI.IsOpen);Assert.AreSame(item,UI.ConsumePendingThrowRequest().Item);Assert.IsNull(UI.ConsumePendingThrowRequest());Assert.IsTrue(Inventory.Contains(item));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)] public void MissingCraftFactoryRefusesVisiblyAndRestoredFactoryRetries(bool brew)
        {
            Entity reagent=null;if(brew){reagent=Carry("GlimmerBrine",1);BrewMode();Pick(reagent);AlchemyStillPart.Factory=null;}else{Pick(Steel);Pick(Oak);Pick(Leather);ForgePart.Factory=null;}
            Call("ExecuteCraft",false);Assert.IsNotEmpty(Status);if(brew)Assert.IsTrue(Inventory.Contains(reagent));else Assert.AreEqual(2,Steel.GetPart<StackerPart>().StackCount);ForgePart.Factory=AlchemyStillPart.Factory=Factory;Call("ExecuteCraft",false);Assert.IsTrue(string.IsNullOrEmpty(Status));if(brew)Assert.IsFalse(Inventory.Contains(reagent));else Assert.AreEqual(1,Steel.GetPart<StackerPart>().StackCount);
        }
        [Test] public void MissingTinkerFactoryDoesNotSpendBitsAndRestoredFactoryRetries()
        {
            SelectRecipe("craft_dagger");var bits=Player.GetPart<BitLockerPart>();bits.AddBits("BC");Call("Rebuild");UI.EntityFactory=null;Assert.IsFalse((bool)Call("TryCraftSelectedRecipeViaCommand"));Assert.IsNotEmpty(Status);UI.EntityFactory=Factory;Assert.IsTrue((bool)Call("TryCraftSelectedRecipeViaCommand"));Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(1,Inventory.Objects.Count(e=>e.BlueprintName=="Dagger"));
        }
        [Test] public void EmptyBrewAndEmptyTinkerMenusHaveVisibleRefusals()
        {
            BrewMode();Call("ExecuteCraft",false);Assert.AreEqual("Pick reagents to brew with.",Status);Set("_panel",2);Call("Rebuild");Assert.IsFalse((bool)Call("TryCraftSelectedRecipeViaCommand"));Assert.AreEqual("Select a recipe to craft.",Status);
        }
        [TestCase(false)] [TestCase(true)] public void CachedBatchRequestReportsNoFalseFailureForPaidPartialSuccess(bool exhaust)
        {
            Pick(Steel);Pick(Oak);Pick(Leather);Assert.AreEqual(2,Get("_craftBatchMax"));if(exhaust)Assert.IsTrue(Inventory.TryConsumeOne(Steel));Call("ExecuteCraft",true);
            int units=Inventory.Objects.Where(e=>e.HasPart<WeaponAssemblyPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);Assert.AreEqual(exhaust?1:2,units);Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(exhaust,Inventory.Contains(Oak));Assert.AreEqual(exhaust,Inventory.Contains(Leather));if(exhaust){Assert.AreEqual(1,Oak.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Leather.GetPart<StackerPart>().StackCount);}
        }
        [TestCase(false)] [TestCase(true)] public void ForgeThenQuenchFailureReportsTheCommittedPartialOutcome(bool dropQuench)
        {
            var quench=Brew(Player,false,false);Call("Rebuild");Pick(Steel);Pick(Oak);Pick(Leather);Pick(quench);var previous=MessageLog.OnMessage;bool armed=true,dropped=false,owned=false;int callbacks=0,outputAtCallback=0;
            MessageLog.OnMessage=message=>{previous?.Invoke(message);if(!armed||!message.Contains(" forges "))return;armed=false;callbacks++;owned=Inventory.Contains(quench);outputAtCallback=Inventory.Objects.Count(e=>e.HasPart<WeaponAssemblyPart>());if(dropQuench)dropped=InventorySystem.Drop(Player,quench,Zone);};
            try{Call("ExecuteCraft",false);}finally{MessageLog.OnMessage=previous;}
            Assert.AreEqual(1,callbacks);Assert.IsTrue(owned);Assert.AreEqual(1,outputAtCallback);Assert.AreEqual(1,Steel.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Oak.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Leather.GetPart<StackerPart>().StackCount);var weapon=Inventory.Objects.Single(e=>e.HasPart<WeaponAssemblyPart>());
            if(dropQuench){Assert.IsTrue(dropped);Assert.IsNull(weapon.GetPart<WeaponTemperPart>());Assert.AreSame(Zone.GetEntityCell(Player),Zone.GetEntityCell(quench));Assert.AreEqual(1,quench.GetPart<StackerPart>().StackCount);StringAssert.Contains("Forged",Status);StringAssert.Contains("quench failed",Status);}else{Assert.IsNotNull(weapon.GetPart<WeaponTemperPart>());Assert.IsFalse(Inventory.Contains(quench));Assert.IsTrue(string.IsNullOrEmpty(Status));}
        }
        [TestCase(false)] [TestCase(true)] public void CraftingMarkFailureUsesTheSameVisibleOutcomeAsItemMenu(bool removed)
        {
            int row=Row(Steel);if(removed)Assert.IsTrue(Inventory.RemoveObject(Steel));Set("_craftCursorIndex",row);Call("ToggleCraftPickUnderCursor");Assert.AreEqual(!removed,CraftingMarkPart.IsMarked(Steel));if(removed)Assert.IsNotEmpty(Status);else Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [Test] public void ExplicitClearPicksClearsPreviousFailureAsANewAction()
        { Pick(Steel);Call("ExecuteCraft",false);Assert.IsNotEmpty(Status);Call("ClearCraftPicks");Assert.IsFalse(CraftingMarkPart.IsMarked(Steel));Assert.IsTrue(string.IsNullOrEmpty(Status)); }
        [Test] public void SuccessfulCraftingPickReplacesOldFailure()
        { Call("ExecuteCraft",false);Assert.IsNotEmpty(Status);Pick(Steel);Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.IsTrue(CraftingMarkPart.IsMarked(Steel)); }
        [TestCase(false)] [TestCase(true)] public void RemovedModificationTargetCanRetryAfterOwnershipReturns(bool removed)
        {
            var dagger=Carry("Dagger",1);Player.GetPart<BitLockerPart>().AddBits("BC");SelectRecipe("mod_sharp_melee",true);Call("OpenModTargetPopupForSelectedRecipe");var popup=Get("_modTargetPopup");int row=((IList)Field(popup,"Targets")).IndexOf(dagger);Assert.GreaterOrEqual(row,0);if(removed)Assert.IsTrue(Inventory.RemoveObject(dagger));Call("ApplySelectedModToPopupTarget",row);
            if(removed){Assert.AreSame(popup,Get("_modTargetPopup"));Assert.IsNotEmpty(Status);Assert.IsFalse(dagger.HasTag("ModSharp"));Assert.IsTrue(Inventory.AddObject(dagger));Call("ApplySelectedModToPopupTarget",row);}Assert.IsNull(Get("_modTargetPopup"));Assert.IsTrue(dagger.HasTag("ModSharp"));Assert.AreEqual(1,dagger.GetIntProperty("ModificationCount"));Assert.IsTrue(string.IsNullOrEmpty(Status));
        }
        [TestCase(false)] [TestCase(true)] public void TinkerCapacityRefusalRefundsBitsAndRetriesAfterRealDrop(bool full)
        {
            SelectRecipe("craft_dagger");var bits=Player.GetPart<BitLockerPart>();bits.AddBits("BC");Call("Rebuild");Inventory.MaxWeight=full?Inventory.GetCarriedWeight():-1;int row=(int)Get("_tinkerCursorIndex");bool success=(bool)Call("TryCraftSelectedRecipeViaCommand");
            if(full){Assert.IsFalse(success);Assert.IsNotEmpty(Status);Assert.AreEqual(1,bits.GetBitCount('B'));Assert.AreEqual(1,bits.GetBitCount('C'));Assert.AreEqual(row,Get("_tinkerCursorIndex"));Assert.IsFalse(Inventory.Objects.Any(e=>e.BlueprintName=="Dagger"));Assert.IsTrue(InventorySystem.Drop(Player,Steel,Zone));Call("Rebuild");Assert.IsTrue((bool)Call("TryCraftSelectedRecipeViaCommand"));}else Assert.IsTrue(success);
            Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreEqual(0,bits.GetBitCount('B'));Assert.AreEqual(0,bits.GetBitCount('C'));Assert.AreEqual(1,Inventory.Objects.Where(e=>e.BlueprintName=="Dagger").Sum(e=>e.GetPart<StackerPart>().StackCount));
        }
        [TestCase(false)] [TestCase(true)] public void ModMenuShowsMissingTargetRatherThanSilentlyReturning(bool singleton)
        {
            Carry("Dagger",singleton?1:2);SelectRecipe("mod_sharp_melee",true);Call("OpenModTargetPopupForSelectedRecipe");if(singleton){Assert.NotNull(Get("_modTargetPopup"));Assert.IsTrue(string.IsNullOrEmpty(Status));}else{Assert.IsNull(Get("_modTargetPopup"));Assert.AreEqual("No compatible target item for this mod.",Status);}
        }
        [TestCase(false)] [TestCase(true)] public void PartialBrewSuccessDoesNotBecomeAFalseAllOrNothingFailure(bool removeRemaining)
        {
            Assert.IsTrue(Zone.AddEntity(Item("AlchemyStill"),9,10));var reagent=Carry("GlimmerBrine",2);BrewMode();Pick(reagent);Assert.AreEqual(2,Get("_craftBatchMax"));var previous=MessageLog.OnMessage;bool armed=true,dropped=false;int callbacks=0;
            MessageLog.OnMessage=message=>{previous?.Invoke(message);if(!armed||!message.Contains(" brews "))return;armed=false;callbacks++;if(removeRemaining)dropped=InventorySystem.Drop(Player,reagent,Zone);};
            try{Call("ExecuteCraft",true);}finally{MessageLog.OnMessage=previous;}
            Assert.AreEqual(1,callbacks);Assert.AreEqual(removeRemaining,dropped);Assert.AreEqual(removeRemaining?1:2,Inventory.Objects.Where(e=>e.HasPart<BrewItemPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));Assert.IsTrue(string.IsNullOrEmpty(Status));if(removeRemaining){Assert.AreSame(Zone.GetEntityCell(Player),Zone.GetEntityCell(reagent));Assert.AreEqual(1,reagent.GetPart<StackerPart>().StackCount);}else Assert.IsFalse(Inventory.Contains(reagent));
        }
        [Test] public void LongFailureTextIsClippedToItsOwnDetailRow()
        {
            Tiles();Call("LogCommandFailure","FeedbackFixture",InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,new string('X',120)));Call("Render");for(int x=1;x<80;x++)Assert.AreSame(CP437TilesetGenerator.GetUiTile('X'),UI.Tilemap.GetTile(new Vector3Int(x,0,0)));Assert.AreNotSame(CP437TilesetGenerator.GetUiTile('X'),UI.Tilemap.GetTile(new Vector3Int(0,1,0)));
        }
        [Test] public void FailureSurvivesRepeatedRedrawAndSuccessfulRetryRemovesItsGlyphs()
        {
            var box=Sack().GetPart<ContainerPart>();var item=Carry("Dagger",1);OpenAction(item,"put_container",out int action);Tiles();Call("ExecuteItemAction",action);for(int i=0;i<3;i++)AssertStatusTiles("Container is full.");Assert.IsTrue(box.RemoveItem(box.Contents[0]));Call("ExecuteItemAction",action);Assert.IsTrue(string.IsNullOrEmpty(Status));Assert.AreNotSame(CP437TilesetGenerator.GetUiTile('C'),UI.Tilemap.GetTile(new Vector3Int(1,0,0)));
        }
    }
}
