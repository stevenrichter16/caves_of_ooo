using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CavesOfOoo.Scenarios.Custom
{
 public sealed partial class ReferenceGladeNativePlayer
 {
  static readonly string[] AudioPolish15Checks={"audio15_actual_book_purchase","audio15_authored_reader","audio15_keyboard_page","audio15_keyboard_imported_paper","audio15_deck_previous_page","audio15_deck_imported_paper","audio15_reading_is_free","audio15_cancel_silent","audio15_previous_gamepad_restored","audio15_preferences_restored"};
  const string AudioPolish15CanVerify="Actual existing merchant copy of an authored multipage codex, ordinary trade payment and native read command; exact unchanged canonical text, keyboard next-page and injected Deck-equivalent LB previous-page through real input, imported Page waveform observed from typed physical context, free reading and silent B close, gamepad and preference restoration.";
  const string AudioPolish15CannotVerify="One disclosed BiomeTravel shortcut reaches the unchanged vendor; this is not natural discovery or travel balance. A bounded32-village search may legitimately fail if no affordable safe original copy exists. No book or text is injected or expanded. The gamepad is a native synthetic device, not physical Deck hardware. This does not add grimoire-learning, journal or generic-announcement paper effects. Source samples are not subjective listening, global output or performance. Saved audio preferences remain untouched.";
  sealed class PaperNativeSite{internal Zone Zone;internal Cell Approach;internal Entity Trader,Book;}
  IEnumerator RunAudioPolish15()
  {
   StartUtilityNative("audio15_preferences_restored",WorldAudioCue.Page);
   Gamepad pad=null,previous=Gamepad.current;Action restore=_audioPolishCleanup;
   _audioPolishCleanup=()=>{if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(previous!=null&&previous.added)previous.MakeCurrent();Check("audio15_previous_gamepad_restored",(pad==null||!pad.added)&&(previous==null||!previous.added||Gamepad.current==previous));restore?.Invoke();};
   var site=FindPaperNativeSite();Require(site!=null,"bounded existing affordable merchant reading copy");
   yield return BiomeTravel(site.Zone,site.Approach,"audio15: unchanged original merchant/codex; ordinary trade follows");
   yield return ResidentTrade(site.Trader,site.Book.BlueprintName,"audio15_actual_book_purchase");
   Require(_input.PlayerEntity.GetPart<InventoryPart>().Contains(site.Book),"exact purchased source owner");
   var document=ReadableDocumentCatalog.Get(site.Book.GetPart<ReadableDocumentPart>().DocumentId);
   int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(_input.PlayerEntity),receipts=_waterNativeReceiptCount;
   yield return ItemAction(site.Book,"ReadDocument");
   Require(State()=="AnnouncementOpen"&&_input.AnnouncementUI.IsOpen,"real physical reader via inventory");
   Check("audio15_authored_reader",_input.AnnouncementUI.PageCount>1&&(string)Field(_input.AnnouncementUI,"_message")==document.Title+"\n\n"+document.Text&&_waterNativeReceiptCount==receipts);
   yield return Capture("pages-01-original-authored-first-page");
   BeginWaterNative(null,null,WorldAudioCue.Page);yield return Tap(Key.PageDown);yield return FinishWaterNative("audio15_keyboard_imported_paper");
   Check("audio15_keyboard_page",(int)Field(_input.AnnouncementUI,"_pageIndex")==1);yield return Capture("pages-02-native-next-page");
   pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();yield return ControllerHold(pad,new GamepadState(),.05f);
   BeginWaterNative(null,null,WorldAudioCue.Page);yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder));yield return FinishWaterNative("audio15_deck_imported_paper");
   Check("audio15_deck_previous_page",(int)Field(_input.AnnouncementUI,"_pageIndex")==0&&_input.AnnouncementUI.IsOpen);
   Check("audio15_reading_is_free",_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy&&_input.PlayerEntity.GetPart<InventoryPart>().Contains(site.Book));
   receipts=_waterNativeReceiptCount;yield return ControllerPulse(pad,new GamepadState().WithButton(GamepadButton.East));
   Check("audio15_cancel_silent",!_input.AnnouncementUI.IsOpen&&_waterNativeReceiptCount==receipts);yield return ResidentCloseMenus();
   yield return Capture("pages-03-ordinary-reader-return");
  }
  PaperNativeSite FindPaperNativeSite()
  {
   int inspected=0;var actor=_input.PlayerEntity;
   for(int y=0;y<WorldMap.Height&&inspected<32;y++)for(int x=0;x<WorldMap.Width&&inspected<32;x++)
   {
    if(BiomeManager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village)continue;inspected++;
    var zone=BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0));
    foreach(var seller in zone.GetReadOnlyEntities().Where(e=>e.HasPart<TraderPart>()&&e.HasPart<ConversationPart>()&&e.HasPart<InventoryPart>()&&!FactionManager.IsHostile(e,actor)))
    {
     var near=SpecialistApproach(zone,seller);if(near==null)continue;
     foreach(var book in seller.GetPart<InventoryPart>().Objects)
     {
      var doc=ReadableDocumentCatalog.Get(book.GetPart<ReadableDocumentPart>()?.DocumentId);
      if(doc==null||doc.Text.Count(c=>c=='\n')<34||(book.GetPart<StackerPart>()?.StackCount??1)!=1)continue;
      int price=TradeSystem.GetBuyPrice(book,TradeSystem.GetTradePerformance(actor),seller);
      if(price>0&&TradeSystem.GetDrams(actor)>=price)return new PaperNativeSite{Zone=zone,Approach=near,Trader=seller,Book=book};
     }
    }
   }
   return null;
  }
 }
}
