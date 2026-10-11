using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Scenarios.Custom
{
 public sealed partial class ReferenceGladeNativePlayer
 {
  static readonly string[] AudioPolish11Checks={"audio11_original_grain_harvested","audio11_actual_station","audio11_exact_conversion","audio11_imported_preparation","audio11_one_action","audio11_fixed_pool","audio11_preferences_restored"};
  const string AudioPolish11CanVerify="Original glade grain harvested and cooked through actual world/inventory controls at the existing finite campfire. Exact ingredient/product conservation, paid cooking action, station-correlated committed receipt, imported preparation recording sampled while playing, fixed16 audio sources and preference restoration.";
  const string AudioPolish11CannotVerify="No ingredient, heat, fuel, HP or actor grants. The ordinary local route retains scheduling and hostile risk. Recorded bowl/spoon preparation is a restrained material cue, not proof of sizzling/boiling or a simulated kitchen. Script-observed source samples are not subjective listening, complete output capture or hardware performance. Cold/refused/rollback cases are separate native unit coverage. Saved preferences are untouched.";
  IEnumerator RunAudioPolish11()
  {
   StartUtilityNative("audio11_preferences_restored",WorldAudioCue.CookPrepare);
   var actor=_input.PlayerEntity;var zone=_input.CurrentZone;var inventory=actor.GetPart<InventoryPart>();
   Require(zone==_stagedZone&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled,"ordinary unchanged glade actor");
   var row=zone.GetCell(44,10).Objects.Single(e=>e.BlueprintName=="RipeCropRow");
   int grainBefore=ResidentUnits(actor,"Emberwheat");
   yield return WalkTo(43,10);yield return WorldAction(row,"Harvest");yield return ResidentCloseMenus();
   Check("audio11_original_grain_harvested",row.GetPart<FieldHarvestPart>().Harvested&&ResidentUnits(actor,"Emberwheat")==grainBefore+1);
   var food=inventory.Objects.Single(e=>e.BlueprintName=="Emberwheat");int units=food.GetPart<StackerPart>()?.StackCount??1;
   string cooked=food.GetPart<CookablePart>().Into;int before=ResidentUnits(actor,cooked);
   yield return WalkTo(42,9);
   var station=zone.GetReadOnlyEntities().Single(e=>e.GetPart<CampfirePart>()!=null&&SpatialQuery.Distance(zone,actor,e)<=1);
   Check("audio11_actual_station",station.GetPart<CampfirePart>().FiniteCooking&&station.GetPart<FuelPart>().FuelMass>0
    &&station.GetPart<ThermalPart>().Temperature>=CookingService.MinimumFiniteCookingTemperature&&BiomeDrawn(station));
   yield return Capture("cook-01-actual-grain-and-shelter");
   int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor),receipts=_waterNativeReceiptCount;
   BeginWaterNative(null,station,WorldAudioCue.CookPrepare);
   yield return ItemAction(food,"Cook");yield return FinishWaterNative("audio11_imported_preparation");yield return ResidentCloseMenus();
   Check("audio11_exact_conversion",!inventory.Objects.Contains(food)&&ResidentUnits(actor,cooked)==before+units);
   Check("audio11_one_action",BiomeOneAction(tick,energy));
   Check("audio11_fixed_pool",_waterNativeReceiptCount==receipts+1&&_waterNativeRuntime.Player.AllocatedSources==16
    &&_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
   yield return Capture("cook-02-real-cooked-grain");
  }
  // Shared only by subsequent utility routes; phase10's established observer
  // remains the imported-cue/EndOfFrame sampler. Each route owns one fresh setup.
  void StartUtilityNative(string restored,params WorldAudioCue[] cues)
  {
   float oldSfx=WorldAudioSettings.SfxVolume;bool oldPause=AudioListener.pause;Coroutine monitor=null;
   string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
   bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(k=>PlayerPrefs.GetFloat(k)).ToArray();
   _audioPolishCleanup=()=>
   {
    WorldAudioUtility.Callback-=ObserveWaterNative;if(monitor!=null)StopCoroutine(monitor);_waterNativePending=null;
    WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
    bool valid=WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause;
    for(int i=0;i<keys.Length;i++)valid&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
    Check(restored,valid);
   };
   _waterNativeRuntime=FindFirstObjectByType<WorldAudioRuntime>();Require(_waterNativeRuntime?.Player!=null,"existing bootstrap audio");
   _waterNativeLibrary=WorldAudioLibrary.LoadResources();_waterNativeSamples=new float[1024];_waterNativeReceiptCount=0;
   foreach(var cue in cues)Require(_waterNativeLibrary.Sound(cue,0)?.Clip!=null,"actual imported utility cue "+cue);
   WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;WorldAudioUtility.Callback+=ObserveWaterNative;
   monitor=StartCoroutine(WaterNativeMonitor());
  }
 }
}
