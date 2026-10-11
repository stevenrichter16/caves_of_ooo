using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Scenarios.Custom
{
 public sealed partial class ReferenceGladeNativePlayer
 {
  static readonly string[] AudioPolish14Checks={"audio14_original_seam_spent","audio14_exact_real_yield","audio14_imported_mineral_phrase","audio14_paid_harvest","audio14_checkpoint_preserves_depletion","audio14_load_does_not_replay","audio14_fixed_pool","audio14_preferences_restored"};
  const string AudioPolish14CanVerify="Ordinary glade's original GlowQuartzVein harvested through actual world menu. Its real product range, consumed owner, one paid action and original contact-correlated mineral receipt; exact imported source output, actual F5/F6 graph replacement preserving depletion without a second harvest phrase, fixed16 sources.";
  const string AudioPolish14CannotVerify="No mineral/tool/product grants or altered RNG. The recorded stone manipulation does not pretend a pickaxe exists. Plant/no-yield/rollback/multicell negatives are separate native tests. The script-selected local walk retains normal danger. The isolated checkpoint is genuinely written and loaded; ordinary saves are preserved by the launcher. Samples do not prove subjective listening, full speaker output or Deck performance.";
  IEnumerator RunAudioPolish14()
  {
   StartUtilityNative("audio14_preferences_restored",WorldAudioCue.HarvestMineral);
   Action restore=_audioPolishCleanup;_audioPolishCleanup=()=>{WorldAudioWork.Callback-=ObserveMineralNative;restore?.Invoke();};
   WorldAudioWork.Callback+=ObserveMineralNative;
   var actor=_input.PlayerEntity;var zone=_input.CurrentZone;var vein=zone.GetCell(31,10).Objects.Single(e=>e.BlueprintName=="GlowQuartzVein");string id=vein.ID;
   int before=ResidentUnits(actor,"GlowQuartz");yield return WalkTo(32,10);
   int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
   BeginWaterNative(null,vein,WorldAudioCue.HarvestMineral);yield return WorldAction(vein,"Harvest");yield return FinishWaterNative("audio14_imported_mineral_phrase");yield return ResidentCloseMenus();
   int earned=ResidentUnits(actor,"GlowQuartz")-before;
   Check("audio14_original_seam_spent",vein.GetPart<HarvestablePart>().Harvested&&zone.GetEntityCell(vein)==null);
   Check("audio14_exact_real_yield",earned>=1&&earned<=2);Check("audio14_paid_harvest",BiomeOneAction(tick,energy));
   yield return Capture("mineral-01-real-harvested-seam");yield return new WaitForSecondsRealtime(1.5f);
   int receipts=_waterNativeReceiptCount;var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"ordinary initial isolated checkpoint");
   string path=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),oldHash=CheckpointHash(path);long serial=MessageLog.NextSerialValue;
   tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);var position=Cell();int hp=actor.GetStatValue("Hitpoints");
   yield return Tap(Key.F5);string hash=CheckpointHash(path);Require(hash!=oldHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved.","native new checkpoint written");
   yield return Tap(Key.F6);double end=Time.realtimeSinceStartupAsDouble+8;
   while(ReferenceEquals(actor,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble<end,"F6 replaces actual player graph");yield return null;}
   Check("audio14_checkpoint_preserves_depletion",_input.PlayerEntity.ID==actor.ID&&_input.CurrentZone.ZoneID==zone.ZoneID
    &&!_input.CurrentZone.GetReadOnlyEntities().Any(e=>e.ID==id)&&ResidentUnits(_input.PlayerEntity,"GlowQuartz")==before+earned
    &&Cell().X==position.X&&Cell().Y==position.Y&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy
    &&_input.PlayerEntity.GetStatValue("Hitpoints")==hp&&CheckpointHash(path)==hash);
   yield return new WaitForSecondsRealtime(.4f);
   var clip=_waterNativeLibrary.Sound(WorldAudioCue.HarvestMineral,0).Clip;
   Check("audio14_load_does_not_replay",_waterNativeReceiptCount==receipts&&!_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying&&s.clip==clip));
   Check("audio14_fixed_pool",_waterNativeRuntime.Player.AllocatedSources==16&&_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
   yield return Capture("mineral-02-actual-reloaded-depletion");
  }
  void ObserveMineralNative(WorldAudioWorkReceipt receipt)
  {
   if(receipt.Cue!=WorldAudioCue.HarvestMineral)return;
   // Observation bridge only; it never submits either callback or a sound.
   ObserveWaterNative(new WorldAudioUtilityReceipt(receipt.Actor,null,receipt.Source,receipt.Zone,receipt.Contact,receipt.Cue));
  }
 }
}
