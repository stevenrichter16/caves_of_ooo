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
        static readonly string[] AudioPolish19Checks={"settlement19_real_authored_arrival","settlement19_imported_town_signal","settlement19_native_paid_travel",
            "settlement19_no_idle_scan_or_restart","settlement19_native_exit_restores_region","settlement19_native_return_keeps_piece","settlement19_fixed_voices","settlement19_ordinary_finish","settlement19_preferences_restored"};
        const string AudioPolish19CanVerify="Actual paid world-map route to authored, installed Morrowfast, real ground descent, Hiraeth streamed source sample/output, real ascent restores Grovelands composition, return to Morrowfast resumes the same composition. Ten total checks including shared boot; four screenshots; fixed16 sources and idle sampling bounds.";
        const string AudioPolish19CannotVerify="Known authored-place musical identity, not live NPC census, a guarantee of active work, or dynamically detecting depopulation. No hidden owner discovery or NPC-generated cues. No shortcuts, grants, forced context, playhead seek or direct audio call. PCM is not human listening, speakers, global clipping or Deck performance. Other towns and the dressing shelter retain regional treatment. Temporary in-memory preferences are restored; saved values stay untouched.";
        IEnumerator RunAudioPolish19Settlement()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;
            bool oldPause=AudioListener.pause;string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            _audioPolishCleanup=()=>{
                WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool unchanged=true;for(int i=0;i<keys.Length;i++)unchanged&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("settlement19_preferences_restored",unchanged&&WorldAudioSettings.MusicVolume==oldMusic&&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null&&BiomeManager?.WorldSeed==64,"ordinary live bootstrap");
            var owner=audio.Player;string actorID=_input.PlayerEntity.ID;var library=WorldAudioLibrary.LoadResources();var town=library.Music("village")?.Clip;var region=library.Music("grovelands")?.Clip;
            Require(town!=null&&region!=null&&town!=region,"distinct prepared full town composition");int tick=_input.TurnManager.TickCount,steps=_audio16MapSteps;
            yield return Tap(Key.LeftShift,Key.Comma);yield return AudioPolish16WalkMap(MorrowfastSceneRuntime.WorldX,MorrowfastSceneRuntime.WorldY);
            yield return Tap(Key.LeftShift,Key.Period);yield return new WaitForSecondsRealtime(2.5f);
            Check("settlement19_real_authored_arrival",_input.CurrentZone.ZoneID==MorrowfastSceneRuntime.ZoneID&&MorrowfastSceneRuntime.IsActive(_input.CurrentZone)
                &&WorldAudioSettlement.IsPreparationStop(_input.CurrentZone,BiomeManager.WorldMap)&&audio.Context.MusicId=="village"&&!audio.Context.Danger);
            var sources=owner.Root.GetComponentsInChildren<AudioSource>();var voice=sources.SingleOrDefault(v=>v.name.StartsWith("Music ")&&v.clip==town&&v.isPlaying&&v.volume>0);
            Require(voice!=null,"actual calm inhabited town score");int firstSample=voice.timeSamples;float peak=0;var buffer=new float[1024];double until=Time.realtimeSinceStartupAsDouble+5;
            while(Time.realtimeSinceStartupAsDouble<until){if(voice.clip==town&&voice.isPlaying&&voice.volume>0){voice.GetOutputData(buffer,0);foreach(float v in buffer)peak=Mathf.Max(peak,Mathf.Abs(v));}yield return null;}
            Check("settlement19_imported_town_signal",town.name=="Hiraeth"&&town.loadType==AudioClipLoadType.Streaming&&town.length>300&&voice.timeSamples>firstSample&&peak>.000001f);
            Check("settlement19_native_paid_travel",_audio16MapSteps>steps&&_input.TurnManager.TickCount>=tick+(_audio16MapSteps-steps)*10);
            int scans=audio.Soundscape.ScanCount,contexts=audio.ContextCheckCount,starts=owner.MusicStartCount;yield return new WaitForSecondsRealtime(.6f);
            Check("settlement19_no_idle_scan_or_restart",audio.Soundscape.ScanCount==scans&&audio.ContextCheckCount==contexts&&owner.MusicStartCount==starts);
            yield return Capture("settlement19-02-actual-town-score");int beforeExit=voice.timeSamples;
            yield return Tap(Key.LeftShift,Key.Comma);yield return new WaitForSecondsRealtime(2.3f);
            Check("settlement19_native_exit_restores_region",WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID)&&audio.Context.MusicId=="grovelands"
                &&sources.Any(v=>v.name.StartsWith("Music ")&&v.clip==region&&v.isPlaying&&v.volume>0)&&!sources.Any(v=>v.clip==town));
            yield return Capture("settlement19-03-actual-map-region");
            yield return Tap(Key.LeftShift,Key.Period);yield return new WaitForSecondsRealtime(2.3f);
            voice=sources.SingleOrDefault(v=>v.name.StartsWith("Music ")&&v.clip==town&&v.isPlaying&&v.volume>0);
            Check("settlement19_native_return_keeps_piece",_input.CurrentZone.ZoneID==MorrowfastSceneRuntime.ZoneID&&audio.Context.MusicId=="village"&&voice!=null&&voice.timeSamples>=beforeExit);
            Check("settlement19_fixed_voices",ReferenceEquals(owner,audio.Player)&&owner.AllocatedSources==16&&owner.Root.GetComponentsInChildren<AudioSource>().Length==16);
            Check("settlement19_ordinary_finish",_input.PlayerEntity.ID==actorID&&State()=="Normal"&&_input.PlayerEntity.GetStatValue("Hitpoints")>0
                &&_input.PlayerEntity.GetStat("Hitpoints").Max==40&&!DebugInvincibility.IsEnabled(_input.PlayerEntity)&&!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&_biomeShortcuts==0);
            _descriptions.Add(new Description{subject="Authored preparation-town score",source=MorrowfastSceneRuntime.ZoneID,text="Hiraeth sampled peak="+peak+"; native paid map steps="+(_audio16MapSteps-steps)
                +"; pre-exit observed PCM="+beforeExit+"; returned PCM="+(voice!=null?voice.timeSamples:-1)+". Installed authored metadata drives musical place identity; no NPC census or work cue is inferred."});
            yield return Capture("settlement19-04-actual-town-return");
        }
    }
}
