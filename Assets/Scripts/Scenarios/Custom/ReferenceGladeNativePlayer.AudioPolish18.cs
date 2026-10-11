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
        static readonly string[] AudioPolish18Checks={"quiet18_actual_starting_composition","quiet18_native_walk_before_wait",
            "quiet18_complete_natural_first_piece","quiet18_distinct_next_piece_starts_once","quiet18_full_rest_without_music","quiet18_environment_continues_during_rest",
            "quiet18_next_imported_signal","quiet18_no_idle_world_rescan","quiet18_fixed_voices",
            "quiet18_native_walk_after_wait","quiet18_ordinary_finish","quiet18_preferences_restored"};
        const string AudioPolish18CanVerify="Fresh ordinary glade and actual input before and after a full imported Ephemera natural completion. No playhead seeks, synthetic replacements or wall-clock acceleration. PCM/DSP observations, exactly one natural completion and one newly started distinct Meanwhile stream, positive per-source output, fixed16 sources and bounded idle world work. Thirteen total checks include shared ordinary boot; an actual twelve-second score rest leaves environmental audio running.";
        const string AudioPolish18CannotVerify="A finite scripted wait in a real game, not a human musical or long-session enjoyment review. Only the first natural sequence transition and its subsequent rest are observed live; future rotation, loading/early-stop counters and outgoing completion have separate sequence/watch helper coverage; private outgoing live-loop integration is not claimed. Prior native tests cover interruption bookmarks and actual mute. No physical speakers, global clipping or Deck performance claim. No gameplay grants, travel shortcuts or direct audio selection/cue calls. In-memory levels are temporarily initialized and restored; saved preferences stay unchanged.";
        IEnumerator RunAudioPolish18Quiet()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;bool oldPause=AudioListener.pause;
            string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            _audioPolishCleanup=()=>{
                WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool same=true;for(int i=0;i<keys.Length;i++)same&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("quiet18_preferences_restored",same&&WorldAudioSettings.MusicVolume==oldMusic&&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"bootstrap audio owner");var owner=audio.Player;
            var library=WorldAudioLibrary.LoadResources();Require(library.MusicCount("organic")==2,"real curated pair only");
            var first=library.Music("organic",0).Clip;var second=library.Music("organic",1).Clip;
            var sources=owner.Root.GetComponentsInChildren<AudioSource>();var actor=_input.PlayerEntity;string actorID=actor.ID;
            yield return new WaitForSecondsRealtime(3);
            var voice=sources.SingleOrDefault(s=>s.name.StartsWith("Music ")&&s.clip==first&&s.isPlaying&&s.volume>0);
            Check("quiet18_actual_starting_composition",audio.Context.MusicId=="organic"&&voice!=null&&!voice.loop
                &&first.name=="Ephemera"&&second.name=="Meanwhile"&&first!=second&&first.loadType==AudioClipLoadType.Streaming&&first.length>300);
            Require(voice!=null,"real original composition before native movement");
            var at=Cell();int priorSteps=audio.AcceptedPlayerSteps;yield return Tap(Key.D);yield return Tap(Key.A);
            Check("quiet18_native_walk_before_wait",Cell().X==at.X&&Cell().Y==at.Y&&audio.AcceptedPlayerSteps==priorSteps+2&&State()=="Normal");
            yield return Capture("quiet18-02-before-complete-composition");
            int sampleAtStart=voice.timeSamples,latestSample=sampleAtStart,starts=owner.MusicStartCount,completions=owner.MusicCompletionCount;
            int scans=audio.Soundscape.ScanCount,contexts=audio.ContextCheckCount,tick=_input.TurnManager.TickCount;
            double began=Time.realtimeSinceStartupAsDouble,expectedRemaining=(first.samples-sampleAtStart)/(double)first.frequency;
            double deadline=began+expectedRemaining+45;AudioSource next=null;int frames=0;
            double quietBegan=-1,quietEnded=-1;int quietFrames=0;bool noMusicDuringQuiet=true;float environmentPeak=0;var quietBuffer=new float[1024];
            while(Time.realtimeSinceStartupAsDouble<deadline&&next==null)
            {
                Require(ReferenceEquals(owner,audio.Player)&&audio.Context.MusicId=="organic"&&State()=="Normal"
                    &&_input.PlayerEntity.GetStatValue("Hitpoints")>0,"unchanged actual game and score context during natural completion");
                if(voice!=null&&voice.clip==first&&voice.isPlaying)latestSample=Math.Max(latestSample,voice.timeSamples);
                if(owner.MusicQuietRemaining>0)
                {
                    if(quietBegan<0)quietBegan=Time.realtimeSinceStartupAsDouble;
                    quietFrames++;noMusicDuringQuiet&=owner.ActiveMusicSources==0;
                    foreach(var environmental in sources.Where(v=>v.name.StartsWith("Ambience ")&&v.clip!=null&&v.isPlaying&&v.volume>0))
                    {environmental.GetOutputData(quietBuffer,0);foreach(float v in quietBuffer)environmentPeak=Mathf.Max(environmentPeak,Mathf.Abs(v));}
                }
                else if(quietBegan>=0&&quietEnded<0)quietEnded=Time.realtimeSinceStartupAsDouble;
                next=sources.FirstOrDefault(s=>s.name.StartsWith("Music ")&&s.clip==second&&s.isPlaying&&s.volume>0);
                frames++;yield return null;
            }
            double elapsed=Time.realtimeSinceStartupAsDouble-began;int firstNextSample=next!=null?next.timeSamples:-1;
            Check("quiet18_complete_natural_first_piece",next!=null&&elapsed>=expectedRemaining-2&&latestSample>=first.samples-first.frequency*2
                &&owner.MusicCompletionCount==completions+1&&frames>100&&_input.TurnManager.TickCount==tick);
            Check("quiet18_full_rest_without_music",quietBegan>=0&&quietEnded-quietBegan>=11.75&&quietEnded-quietBegan<=13&&quietFrames>100&&noMusicDuringQuiet);
            Check("quiet18_environment_continues_during_rest",environmentPeak>.000001f);
            Check("quiet18_distinct_next_piece_starts_once",next!=null&&next.clip==second&&!next.loop&&firstNextSample>=0
                &&firstNextSample<second.frequency*3&&owner.MusicStartCount==starts+1&&owner.ActiveMusicSources==1);
            Require(next!=null,"actual full first composition completed and second is playing");
            float peak=0;int count=0;var buffer=new float[1024];double until=Time.realtimeSinceStartupAsDouble+5;
            while(Time.realtimeSinceStartupAsDouble<until)
            {
                if(next!=null&&next.clip==second&&next.isPlaying&&next.volume>0){next.GetOutputData(buffer,0);foreach(float v in buffer){peak=Mathf.Max(peak,Mathf.Abs(v));count++;}}
                yield return null;
            }
            Check("quiet18_next_imported_signal",second.loadType==AudioClipLoadType.Streaming&&second.loadState==AudioDataLoadState.Loaded
                &&next.timeSamples>firstNextSample&&count>0&&peak>.000001f);
            Check("quiet18_no_idle_world_rescan",audio.Soundscape.ScanCount==scans&&audio.ContextCheckCount==contexts&&_input.TurnManager.TickCount==tick);
            Check("quiet18_fixed_voices",ReferenceEquals(owner,audio.Player)&&owner.AllocatedSources==16&&owner.Root.GetComponentsInChildren<AudioSource>().Length==16);
            yield return Capture("quiet18-03-actual-natural-next-piece");
            priorSteps=audio.AcceptedPlayerSteps;at=Cell();yield return Tap(Key.D);yield return Tap(Key.A);
            Check("quiet18_native_walk_after_wait",Cell().X==at.X&&Cell().Y==at.Y&&audio.AcceptedPlayerSteps==priorSteps+2
                &&audio.Context.MusicId=="organic"&&next.clip==second&&owner.MusicStartCount==starts+1);
            Check("quiet18_ordinary_finish",_input.PlayerEntity.ID==actorID&&State()=="Normal"&&_input.PlayerEntity.GetStatValue("Hitpoints")>0
                &&_input.PlayerEntity.GetStat("Hitpoints").Max==40&&!DebugInvincibility.IsEnabled(_input.PlayerEntity)&&!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&_biomeShortcuts==0);
            _descriptions.Add(new Description{subject="Full natural composition transition",source=first.name+" -> "+second.name,text="Starting PCM="+sampleAtStart
                +"; latest observed first PCM="+latestSample+"/"+first.samples+"; remaining duration="+expectedRemaining+"; actual elapsed="+elapsed
                +"; observation frames="+frames+"; first next PCM="+firstNextSample+"; next sampled peak="+peak+"; completion delta="+(owner.MusicCompletionCount-completions)
                +"; actual quiet duration="+(quietEnded-quietBegan)+"; quiet frames="+quietFrames+"; environmental peak="+environmentPeak+". All sample positions are observations; no seek or accelerated clock occurred."});
            yield return Capture("quiet18-04-gameplay-after-complete-piece");
        }
    }
}
