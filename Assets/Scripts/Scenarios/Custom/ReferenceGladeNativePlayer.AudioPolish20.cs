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
        static readonly string[] AudioPolish20Checks={"gain20_imported_steady_playback","gain20_native_audio_menu","gain20_music_ramp_has_audible_source_midpoints",
            "gain20_ambience_ramp_has_audible_source_midpoints","gain20_zero_mutes_immediately","gain20_native_unmute_returns","gain20_actual_imported_output",
            "gain20_idle_work_bound","gain20_fixed_pool","gain20_menu_costs_no_turn_or_actor_changes","gain20_preferences_restored"};
        const string AudioPolish20CanVerify="Actual native audio-menu settings on the ordinary glade: source-volume intermediate frames for positive Music and Ambience adjustments, zero retirement at the first end-of-frame observing mute, unmute with real imported streamed PCM and unchanged voice/idle work bounds. Twelve checks including common boot, at leastfour screenshots, exact saved and in-memory preference restoration.";
        const string AudioPolish20CannotVerify="Observed source gains/output demonstrate the ramp is wired to real playback, not a subjective listening or click-free DAC/speaker guarantee. No forced context, injected cue, synthetic replacement, volume adjustment in place of menu input, playhead seek, grants or travel shortcut. Initial in-memory levels are normalized before the route; menu changes temporarily write preferences and cleanup restores exact previous presence/values. No Steam Deck performance claim.";
        sealed class AudioPolish20Ramp
        {
            public AudioSource Voice;public float Before,After,Setting;public Func<float> Current;public int MidFrames;public bool Reached;public bool IsMusic;
        }
        IEnumerator ObserveAudioPolish20(AudioPolish20Ramp w)
        {
            while(true)
            {
                yield return new WaitForEndOfFrame();
                if(w.Voice==null||!w.Voice.isPlaying||Mathf.Abs(w.Current()-w.Setting)>.0001f)continue;
                float value=w.Voice.volume;
                if(value>w.After+.00001f&&value<w.Before-.00001f)w.MidFrames++;
                if(Mathf.Abs(value-w.After)<.00001f)w.Reached=true;
            }
        }
        IEnumerator RunAudioPolish20Gains()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;bool oldPause=AudioListener.pause;
            string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            Coroutine monitor=null,muteMonitor=null;_audioPolishCleanup=()=>{
                if(monitor!=null)StopCoroutine(monitor);if(muteMonitor!=null)StopCoroutine(muteMonitor);
                for(int i=0;i<keys.Length;i++)if(had[i])PlayerPrefs.SetFloat(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);PlayerPrefs.Save();
                WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool same=true;for(int i=0;i<keys.Length;i++)same&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("gain20_preferences_restored",same&&WorldAudioSettings.MusicVolume==oldMusic&&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"ordinary live audio owner");var owner=audio.Player;var actor=_input.PlayerEntity;string actorID=actor.ID;
            var cell=Cell();int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);string stats=CombatStats(actor),gear=CombatGear(actor);
            yield return new WaitForSecondsRealtime(3);
            var sources=owner.Root.GetComponentsInChildren<AudioSource>();var music=sources.SingleOrDefault(v=>v.name.StartsWith("Music ")&&v.clip!=null&&v.isPlaying&&v.volume>0);
            var ambience=sources.SingleOrDefault(v=>v.name.StartsWith("Ambience ")&&v.clip!=null&&v.isPlaying&&v.volume>0);
            Check("gain20_imported_steady_playback",music!=null&&ambience!=null&&audio.Context.MusicId=="organic"&&owner.AtmosphereMix==1&&music.clip.loadType==AudioClipLoadType.Streaming);
            Require(music!=null&&ambience!=null,"actual separate score and field bed");var score=music.clip;
            var menu=FindFirstObjectByType<PauseMenuUI>();yield return Tap(Key.Tab);for(int i=0;i<PauseMenuController.AudioIndex;i++)yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
            Check("gain20_native_audio_menu",menu.Controller.IsAudioOpen&&menu.Controller.SelectedIndex==PauseMenuController.MusicVolumeIndex);
            var first=new AudioPolish20Ramp{Voice=music,Before=music.volume,After=music.volume*.45f/.55f,Setting=.45f,Current=()=>WorldAudioSettings.MusicVolume};
            monitor=StartCoroutine(ObserveAudioPolish20(first));yield return Tap(Key.LeftArrow);yield return new WaitForSecondsRealtime(.5f);StopCoroutine(monitor);monitor=null;
            Check("gain20_music_ramp_has_audible_source_midpoints",first.MidFrames>0&&first.Reached&&Mathf.Abs(owner.EffectiveMusicVolume-.45f)<.0001f&&music.clip==score);
            yield return Capture("gain20-02-actual-music-ramp");yield return Tap(Key.DownArrow);
            var second=new AudioPolish20Ramp{Voice=ambience,Before=ambience.volume,After=ambience.volume*.35f/.45f,Setting=.35f,Current=()=>WorldAudioSettings.AmbienceVolume};
            monitor=StartCoroutine(ObserveAudioPolish20(second));yield return Tap(Key.LeftArrow);yield return new WaitForSecondsRealtime(.5f);StopCoroutine(monitor);monitor=null;
            Check("gain20_ambience_ramp_has_audible_source_midpoints",second.MidFrames>0&&second.Reached&&Mathf.Abs(owner.EffectiveAmbienceVolume-.35f)<.0001f&&music.clip==score);
            yield return Capture("gain20-03-actual-ambience-ramp");yield return Tap(Key.UpArrow);
            bool muteSeen=false,muteImmediate=false;IEnumerator ObserveMute(){while(!muteSeen){yield return new WaitForEndOfFrame();if(WorldAudioSettings.MusicVolume==0){muteSeen=true;muteImmediate=owner.ActiveMusicSources==0&&sources.Where(v=>v.name.StartsWith("Music ")).All(v=>v.clip==null&&v.volume==0);}}}
            muteMonitor=StartCoroutine(ObserveMute());for(int i=0;i<5;i++)yield return Tap(Key.LeftArrow);yield return null;
            Check("gain20_zero_mutes_immediately",muteSeen&&muteImmediate&&WorldAudioSettings.MusicVolume==0&&owner.ActiveMusicSources==0&&ambience.isPlaying);
            yield return Tap(Key.RightArrow);yield return new WaitForSecondsRealtime(2.3f);
            music=sources.SingleOrDefault(v=>v.name.StartsWith("Music ")&&v.clip==score&&v.isPlaying&&v.volume>0);
            Check("gain20_native_unmute_returns",music!=null&&Mathf.Abs(WorldAudioSettings.MusicVolume-.1f)<.0001f&&Mathf.Abs(owner.EffectiveMusicVolume-.1f)<.0001f&&owner.ActiveMusicSources==1);
            float peak=0;var data=new float[1024];int before=music!=null?music.timeSamples:-1;double until=Time.realtimeSinceStartupAsDouble+2;
            while(Time.realtimeSinceStartupAsDouble<until){if(music!=null&&music.isPlaying){music.GetOutputData(data,0);foreach(float v in data)peak=Mathf.Max(peak,Mathf.Abs(v));}yield return null;}
            Check("gain20_actual_imported_output",music!=null&&music.timeSamples>before&&peak>.000001f);
            int checks=audio.ContextCheckCount,scans=audio.Soundscape.ScanCount,starts=owner.MusicStartCount;yield return new WaitForSecondsRealtime(.5f);
            Check("gain20_idle_work_bound",audio.ContextCheckCount==checks&&audio.Soundscape.ScanCount==scans&&owner.MusicStartCount==starts);
            Check("gain20_fixed_pool",ReferenceEquals(owner,audio.Player)&&owner.AllocatedSources==16&&owner.Root.GetComponentsInChildren<AudioSource>().Length==16);
            yield return Tap(Key.Escape);yield return Tap(Key.Escape);
            Check("gain20_menu_costs_no_turn_or_actor_changes",State()=="Normal"&&!menu.Controller.IsOpen&&ReferenceEquals(actor,_input.PlayerEntity)&&actor.ID==actorID&&ReferenceEquals(cell,Cell())
                &&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy&&CombatStats(actor)==stats&&CombatGear(actor)==gear&&_biomeShortcuts==0);
            _descriptions.Add(new Description{subject="Actual menu gain ramps",source=score.name,text="Music intermediate source frames="+first.MidFrames+"; ambience intermediate source frames="+second.MidFrames
                +"; first mute observation immediately retired="+muteImmediate+"; returned imported sampled peak="+peak+". All adjustments after initial levels came from real menu keyboard input."});
            yield return Capture("gain20-04-native-game-after-settings");
        }
    }
}
