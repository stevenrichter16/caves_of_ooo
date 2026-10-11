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
  static readonly string[] AudioPolish09Checks={"footing_original_vessel_acquired","footing_wet_step_plays","footing_real_draw_empties_source","footing_empty_source_step_plays_ground","footing_audio_pool_bound","footing_preferences_restored"};
  const string AudioPolish09CanVerify="Actual original glade waterskin pickup, real movement onto the positive basin and the same basin after actual drawing, exact imported wet/dry footstep playing sources and sampled PCM, preserved charge conservation and sixteen voices.";
  const string AudioPolish09CannotVerify="Script-selected short route, no world or cue injection. Source/output observations are not subjective listening, physical speakers or Deck performance. Only SFX level is temporarily set and restored; simulation remains active. Ground choice is the actual grass basin tile; stone-ground counterpart has native unit coverage.";
  IEnumerator RunAudioPolish09()
  {
   float old=WorldAudioSettings.SfxVolume;WorldAudioSettings.SfxVolume=1;
   EntityMovedVisualHandler observer=null;Coroutine sample=null;
   _audioPolishCleanup=()=>{EntityVisualHooks.MovedCallback-=observer;if(sample!=null)StopCoroutine(sample);WorldAudioSettings.SfxVolume=old;Check("footing_preferences_restored",WorldAudioSettings.SfxVolume==old);};
   var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null&&!AudioListener.pause,"actual active world audio");
   var actor=_input.PlayerEntity;var zone=_input.CurrentZone;var inv=actor.GetPart<InventoryPart>();
   var basin=zone.GetCell(36,9).Objects.Single(e=>e.BlueprintName=="SpreadDrawPoint");var pool=basin.GetPart<LiquidPoolPart>();Require(pool.Volume==3,"original three-dram source");
   var skin=zone.GetCell(37,10).Objects.Single(e=>e.BlueprintName=="Waterskin");
   yield return WalkTo(37,10);yield return Tap(Key.G);if(State()=="PickupOpen")yield return Tap(Key.Tab);if(State()!="Normal")yield return Tap(Key.Escape);
   Check("footing_original_vessel_acquired",inv.Objects.Contains(skin)&&skin.GetPart<WaterskinPart>().Charges==0);
   yield return WalkTo(36,10);
   string expected=null,heard=null;float peak=0;bool witnessed=false;
   observer=(entity,where,ox,oy,nx,ny,forced)=>{
    if(expected==null||forced||entity!=actor||where!=zone||nx!=36||ny!=9)return;
    var voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==64&&s.isPlaying&&s.volume>0&&s.clip!=null&&s.clip.name.StartsWith(expected,StringComparison.Ordinal));
    if(voice==null)return;heard=voice.clip.name;witnessed=true;sample=StartCoroutine(AudioPolish09Sample(voice,p=>peak=Mathf.Max(peak,p)));
   };
   EntityVisualHooks.MovedCallback+=observer;
   expected="step_water_";yield return Tap(Key.W);yield return new WaitForSecondsRealtime(.15f);
   Check("footing_wet_step_plays",Cell().X==36&&Cell().Y==9&&pool.Volume==3&&witnessed&&peak>0.000001f);
   _descriptions.Add(new Description{subject="Positive basin footstep",source=basin.ID,text="Clip="+heard+"; sampled peak="+peak+"; actual liquid="+pool.Volume});
   expected=null;yield return Tap(Key.S);yield return ItemAction(skin,"FillWaterskin");if(State()=="InventoryOpen")yield return Tap(Key.I);
   Check("footing_real_draw_empties_source",inv.Objects.Contains(skin)&&skin.GetPart<WaterskinPart>().Charges==3&&pool.Volume==0&&zone.GetEntityCell(basin)!=null);
   yield return Capture("footing-02-actually-drained-basin");
   witnessed=false;heard=null;peak=0;expected="step_soft_";yield return Tap(Key.W);yield return new WaitForSecondsRealtime(.15f);
   Check("footing_empty_source_step_plays_ground",Cell().X==36&&Cell().Y==9&&pool.Volume==0&&witnessed&&peak>0.000001f);
   _descriptions.Add(new Description{subject="Same basin after drawing",source=basin.ID,text="Clip="+heard+"; sampled peak="+peak+"; actual liquid="+pool.Volume+". No emitter calls or injected volumes."});
   Check("footing_audio_pool_bound",audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16&&actor.GetStatValue("Hitpoints")>0);
   yield return Capture("footing-03-dry-ground-step");
  }
  IEnumerator AudioPolish09Sample(AudioSource voice,Action<float> observe)
  {
   var clip=voice.clip;var data=new float[512];double until=Time.realtimeSinceStartupAsDouble+.25;
   while(Time.realtimeSinceStartupAsDouble<until){yield return new WaitForEndOfFrame();if(voice==null||voice.clip!=clip||!voice.isPlaying)yield break;voice.GetOutputData(data,0);float peak=0;foreach(float v in data)peak=Mathf.Max(peak,Mathf.Abs(v));observe(peak);}
  }
 }
}
