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
        private static readonly string[] AudioPolish02Checks={
            "polish02_imported_initial_progress","polish02_native_audio_menu",
            "polish02_mute_retires_waveform","polish02_long_interruption_stays_silent",
            "polish02_unmute_resumes_bookmark","polish02_imported_return_signal",
            "polish02_fixed_audio_owner_and_pool","polish02_idle_resume_work_bound",
            "polish02_menu_costs_no_game_turn","polish02_loaded_composition_continues",
            "polish02_ordinary_finish","polish02_preferences_restored"};
        private const string AudioPolish02CanVerify="Fresh ordinary glade boot, actual imported streamed composition advances, native keyboard audio-menu mute retires its waveform for more than two seconds, native unmute resumes near its previously observed PCM position with nonzero per-source output. Actual F5/F6 replaces the gameplay graph while the same score continues. Fixed16-source pool, idle sampling bounds, no menu turn and exact restoration of temporary audio preferences.";
        private const string AudioPolish02CannotVerify="Live gate exercises an actual settings interruption, not a combat encounter or region change. Completed danger fades, alias identity, independent clips, bounded eviction and new lifetime reset have separate native unit coverage. Frame observations allow scheduling tolerance; no audit playhead seek, direct audio context/cue call, forced game outcome or travel shortcut. No subjective listening, physical speakers, continuous clipping guarantee or Deck performance claim. Audio-menu keys temporarily persist settings; exact prior key presence/values and effective levels are restored.";

        private sealed class AudioPolish02Samples
        {
            public AudioSource[] Sources;public AudioClip Clip;
            public int LastBefore=-1,FirstReturn=-1;public bool Retired;
        }
        private IEnumerator ObserveAudioPolish02(AudioPolish02Samples window)
        {
            while(true)
            {
                var current=window.Sources.FirstOrDefault(s=>s!=null&&s.name.StartsWith("Music ",StringComparison.Ordinal)
                    &&s.clip==window.Clip&&s.isPlaying&&s.volume>0);
                if(!window.Retired)
                {
                    if(current!=null)window.LastBefore=current.timeSamples;
                    else if(WorldAudioSettings.MusicVolume==0&&window.LastBefore>=0
                        &&window.Sources.All(s=>s==null||!s.name.StartsWith("Music ",StringComparison.Ordinal)||s.clip==null))window.Retired=true;
                }
                else if(current!=null&&window.FirstReturn<0)window.FirstReturn=current.timeSamples;
                yield return null;
            }
        }
        private IEnumerator RunAudioPolish02Bookmarks()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;
            bool oldPause=AudioListener.pause;string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            Coroutine monitor=null;
            _audioPolishCleanup=()=>{
                if(monitor!=null)StopCoroutine(monitor);
                for(int i=0;i<keys.Length;i++)if(had[i])PlayerPrefs.SetFloat(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool restored=true;for(int i=0;i<keys.Length;i++)restored&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("polish02_preferences_restored",restored&&WorldAudioSettings.MusicVolume==oldMusic
                    &&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player?.Root!=null,"actual bootstrap-owned audio player");
            var player=audio.Player;var clip=WorldAudioLibrary.LoadResources().Music("organic")?.Clip;
            var sources=player.Root.GetComponentsInChildren<AudioSource>();Require(clip!=null&&sources.Length==16,"imported regional score and fixed pool");
            var actor=_input.PlayerEntity;string actorID=actor.ID;var cell=Cell();int tick=_input.TurnManager.TickCount;
            int energy=_input.TurnManager.GetEnergy(actor);string stats=CombatStats(actor),gear=CombatGear(actor);
            yield return new WaitForSecondsRealtime(8);
            var original=sources.FirstOrDefault(s=>s.name.StartsWith("Music ")&&s.clip==clip&&s.isPlaying&&s.volume>0);
            Check("polish02_imported_initial_progress",audio.Context.MusicId=="organic"&&original!=null
                &&clip.loadType==AudioClipLoadType.Streaming&&clip.loadState==AudioDataLoadState.Loaded&&original.timeSamples>clip.frequency*3);
            Require(original!=null,"real playing original stream before input");
            var window=new AudioPolish02Samples{Sources=sources,Clip=clip};monitor=StartCoroutine(ObserveAudioPolish02(window));
            var menu=FindFirstObjectByType<PauseMenuUI>();Require(menu!=null,"actual pause menu");
            yield return Tap(Key.Tab);for(int i=0;i<PauseMenuController.AudioIndex;i++)yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
            Check("polish02_native_audio_menu",menu.Controller.IsAudioOpen&&menu.Controller.SelectedIndex==PauseMenuController.MusicVolumeIndex);
            for(int i=0;i<11;i++)yield return Tap(Key.LeftArrow);
            yield return null;
            Check("polish02_mute_retires_waveform",WorldAudioSettings.MusicVolume==0&&window.Retired&&window.LastBefore>clip.frequency*3
                &&player.ActiveMusicSources==0&&sources.Where(s=>s.name.StartsWith("Music ")).All(s=>s.clip==null&&!s.isPlaying));
            double silenceBegan=Time.realtimeSinceStartupAsDouble;yield return new WaitForSecondsRealtime(3);
            Check("polish02_long_interruption_stays_silent",Time.realtimeSinceStartupAsDouble-silenceBegan>=3
                &&player.ActiveMusicSources==0&&window.FirstReturn<0&&audio.Context.MusicId=="organic");
            yield return Capture("polish02-02-actual-music-mute");
            yield return Tap(Key.RightArrow);yield return new WaitForSecondsRealtime(2.2f);
            var resumed=sources.FirstOrDefault(s=>s.name.StartsWith("Music ")&&s.clip==clip&&s.isPlaying&&s.volume>0);
            Check("polish02_unmute_resumes_bookmark",WorldAudioSettings.MusicVolume>0&&resumed!=null&&window.FirstReturn>=window.LastBefore-clip.frequency*.15f
                &&window.FirstReturn<=window.LastBefore+clip.frequency*2&&window.FirstReturn>clip.frequency*3);
            float peak=0;int sampled=0;var samples=new float[1024];double until=Time.realtimeSinceStartupAsDouble+3;
            while(Time.realtimeSinceStartupAsDouble<until)
            {
                if(resumed!=null&&resumed.clip==clip&&resumed.isPlaying&&resumed.volume>0)
                {resumed.GetOutputData(samples,0);foreach(float v in samples){peak=Mathf.Max(peak,Mathf.Abs(v));sampled++;}}
                yield return null;
            }
            Check("polish02_imported_return_signal",sampled>0&&peak>.000001f&&resumed!=null&&resumed.timeSamples>window.FirstReturn);
            Check("polish02_fixed_audio_owner_and_pool",ReferenceEquals(player,audio.Player)&&player.AllocatedSources==16
                &&player.Root.GetComponentsInChildren<AudioSource>().Length==16&&player.ActiveMusicSources==1);
            int scans=audio.Soundscape.ScanCount,checks=audio.ContextCheckCount,starts=player.MusicStartCount;
            yield return new WaitForSecondsRealtime(.6f);
            Check("polish02_idle_resume_work_bound",scans==audio.Soundscape.ScanCount&&checks==audio.ContextCheckCount&&starts==player.MusicStartCount);
            yield return Capture("polish02-03-real-resumed-composition");
            yield return Tap(Key.Escape);yield return Tap(Key.Escape);
            Check("polish02_menu_costs_no_game_turn",!menu.Controller.IsOpen&&State()=="Normal"&&ReferenceEquals(actor,_input.PlayerEntity)
                &&ReferenceEquals(cell,Cell())&&tick==_input.TurnManager.TickCount&&energy==_input.TurnManager.GetEnergy(actor)
                &&stats==CombatStats(actor)&&gear==CombatGear(actor));
            int beforeLoad=resumed!=null?resumed.timeSamples:-1;starts=player.MusicStartCount;
            yield return Tap(Key.F5);yield return Tap(Key.F6);until=Time.realtimeSinceStartupAsDouble+10;
            while(ReferenceEquals(actor,_input.PlayerEntity)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Check("polish02_loaded_composition_continues",!ReferenceEquals(actor,_input.PlayerEntity)&&_input.PlayerEntity.ID==actorID
                &&ReferenceEquals(player,audio.Player)&&audio.Context.MusicId=="organic"&&player.MusicStartCount==starts
                &&resumed!=null&&resumed.isPlaying&&resumed.clip==clip&&resumed.timeSamples>=beforeLoad);
            Check("polish02_ordinary_finish",State()=="Normal"&&_input.PlayerEntity.GetStatValue("Hitpoints")>0
                &&_input.PlayerEntity.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()
                &&!DebugInvincibility.IsEnabled(_input.PlayerEntity)&&_biomeShortcuts==0&&player.AllocatedSources==16);
            _descriptions.Add(new Description{subject="Actual imported score continuation",source=clip.name,text="Last observed pre-mute PCM sample="+window.LastBefore
                +"; first observed resumed sample="+window.FirstReturn+"; sample rate="+clip.frequency+"; muted wall seconds>=3; return sampled peak="+peak
                +"; sampled floats="+sampled+"; source count="+player.AllocatedSources+". Actual menu keys and F5/F6; no playhead assignment or direct sound/context call. Combat interruption has unit coverage only in this pass."});
            yield return Capture("polish02-04-loaded-world-continuation");
        }
    }
}
