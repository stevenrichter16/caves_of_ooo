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
        bool _worldAudioOnly,_worldAudioComplete;
        Action _worldAudioCleanup;
        public void ConfigureWorldAudioAudit()=>_worldAudioOnly=true;
        IEnumerator RunWorldAudioAudit()
        {
            string[] keys={"CavesOfOoo.Audio.Music","CavesOfOoo.Audio.Ambience","CavesOfOoo.Audio.Sfx"};
            var had=new bool[3];var values=new float[3];
            for(int i=0;i<3;i++){had[i]=PlayerPrefs.HasKey(keys[i]);values[i]=PlayerPrefs.GetFloat(keys[i]);}
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;
            var oldPad=Gamepad.current;var pad=InputSystem.AddDevice<Gamepad>();
            _worldAudioCleanup=()=>{
                if(pad.added)InputSystem.RemoveDevice(pad);if(oldPad!=null&&oldPad.added)oldPad.MakeCurrent();
                for(int i=0;i<3;i++)if(had[i])PlayerPrefs.SetFloat(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;
                bool restored=true;for(int i=0;i<3;i++)restored&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==values[i]);
                Check("audio_preferences_restored",restored);Check("audio_synthetic_gamepad_removed",!pad.added);
            };
            try
            {
                // Audio-only test preconditions, independent of world/save state.
                WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;
                pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"bootstrap owns world audio");
                yield return new WaitForSecondsRealtime(3);
                Check("audio_fixed_pool_live",audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
                var music=audio.Player.Root.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Music ")&&s.isPlaying&&s.clip!=null).ToArray();
                Check("imported_streamed_music_plays",music.Length>0&&music.All(s=>s.clip.loadType==AudioClipLoadType.Streaming&&s.clip.loadState==AudioDataLoadState.Loaded&&s.volume>0));
                Check("ambience_plays",audio.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>s.name.StartsWith("Ambience ")&&s.isPlaying&&s.volume>0));
                var samples=new float[1024];double squares=0;float peak=0;int count=0;
                double until=Time.realtimeSinceStartupAsDouble+2;
                while(Time.realtimeSinceStartupAsDouble<until)
                {
                    AudioListener.GetOutputData(samples,0);foreach(float v in samples){squares+=v*v;peak=Mathf.Max(peak,Mathf.Abs(v));count++;}yield return null;
                }
                double rms=Math.Sqrt(squares/Math.Max(1,count));
                _descriptions.Add(new Description{subject="Mixed output samples",source="AudioListener.GetOutputData",text="RMS="+rms+" peak="+peak+" samples="+count+"; noncontiguous observer windows, not a continuous recording or listening review."});
                Check("observed_audio_windows_nonzero_and_below_full_scale",rms>0.000001&&peak<1);
                Check("place_snapshot_is_bounded",audio.Soundscape!=null&&audio.Soundscape.LastVisitedOwners<=512&&audio.Soundscape.SourceCount>0);
                int placeScans=audio.Soundscape.ScanCount,detailStart=audio.Player.AcceptedDetailCount;
                until=Time.realtimeSinceStartupAsDouble+26;bool heardDetail=false;
                while(Time.realtimeSinceStartupAsDouble<until&&!heardDetail)
                {
                    heardDetail=audio.Player.AcceptedDetailCount>detailStart&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.clip!=null&&s.volume>0);
                    yield return null;
                }
                Check("real_place_detail_recording_plays",heardDetail&&audio.Player.AcceptedDetailCount>detailStart);
                Check("place_idle_wait_never_rescans_or_grows_pool",audio.Soundscape.ScanCount==placeScans&&audio.Player.AllocatedSources==16);
                _descriptions.Add(new Description{subject="Place soundscape",source="Real generated glade and audio output",text="Bed="+audio.Context.AmbienceId+"; retained sources="+audio.Soundscape.SourceCount+"; last detail="+audio.Player.LastDetailId+"; visited owners="+audio.Soundscape.LastVisitedOwners+". No direct cue injection or world edits."});
                int habitatBefore=audio.Soundscape.HabitatPhrases,materialBefore=audio.Soundscape.MaterialPhrases;
                until=Time.realtimeSinceStartupAsDouble+45;
                while(Time.realtimeSinceStartupAsDouble<until&&(audio.Soundscape.HabitatPhrases==habitatBefore||audio.Soundscape.MaterialPhrases==materialBefore))yield return null;
                Check("habitat_and_local_material_both_get_live_phrases",audio.Soundscape.HabitatPhrases>habitatBefore&&audio.Soundscape.MaterialPhrases>materialBefore);
                Check("independent_phrase_lanes_keep_idle_scan_bound",audio.Soundscape.ScanCount==placeScans&&audio.Player.AllocatedSources==16);
                _descriptions.Add(new Description{subject="Iteration 1 live phrase lanes",source="Real generated glade",text="Habitat="+audio.Soundscape.HabitatPhrases+"; material="+audio.Soundscape.MaterialPhrases+"; wildlife="+audio.Soundscape.WildlifePhrases+". No animal introduced to force a call; animal fairness has separate native unit counters."});
                int context=audio.ContextCheckCount,starts=audio.Player.MusicStartCount;
                yield return new WaitForSecondsRealtime(.6f);
                Check("idle_does_not_rescan_or_restart_music",audio.ContextCheckCount==context&&audio.Player.MusicStartCount==starts);
                int steps=audio.AcceptedPlayerSteps;var cell=Cell();int x=cell.X,y=cell.Y;
                var playingMusic=music.FirstOrDefault();int beforeSamples=playingMusic!=null?playingMusic.timeSamples:0;
                yield return Tap(Key.D);yield return Tap(Key.A);
                Check("real_walk_emits_two_player_steps",Cell().X==x&&Cell().Y==y&&audio.AcceptedPlayerSteps==steps+2);
                Check("same_biome_steps_do_not_restart_music",audio.Player.MusicStartCount==starts);
                Check("imported_music_playhead_advances_during_walk",playingMusic!=null&&playingMusic.isPlaying&&playingMusic.timeSamples>beforeSamples);
                var menu=FindFirstObjectByType<PauseMenuUI>();Require(menu!=null,"pause UI");
                yield return PadTap(pad,GamepadButton.Select);
                for(int i=0;i<PauseMenuController.AudioIndex;i++)yield return PadTap(pad,GamepadButton.DpadDown);
                yield return PadTap(pad,GamepadButton.South);
                Check("gamepad_opens_audio_page",menu.Controller.IsAudioOpen);
                float before=WorldAudioSettings.MusicVolume;yield return PadTap(pad,GamepadButton.DpadRight);
                Check("gamepad_adjusts_only_music",WorldAudioSettings.MusicVolume>before&&WorldAudioSettings.AmbienceVolume==.45f&&WorldAudioSettings.SfxVolume==1);
                Check("audio_page_keeps_music_running",audio.Player.MusicStartCount==starts&&music.Any(s=>s!=null&&s.isPlaying));
                yield return Capture("02-audio-settings");
                for(int i=0;i<11;i++)yield return PadTap(pad,GamepadButton.DpadLeft);
                Check("music_mute_keeps_ambience",WorldAudioSettings.MusicVolume==0&&audio.Player.ActiveMusicSources==0&&audio.Player.ActiveAmbienceSources>0);
                yield return PadTap(pad,GamepadButton.DpadRight);yield return new WaitForSecondsRealtime(2.2f);
                Check("music_unmute_resumes_a_single_voice",audio.Player.ActiveMusicSources==1);
                yield return PadTap(pad,GamepadButton.DpadDown);
                for(int i=0;i<11;i++)yield return PadTap(pad,GamepadButton.DpadLeft);
                Check("ambience_mute_clears_details_but_keeps_music",WorldAudioSettings.AmbienceVolume==0&&audio.Player.ActiveDetailSources==0&&audio.Player.ActiveAmbienceSources==0&&audio.Player.ActiveMusicSources==1);
                for(int i=0;i<5;i++)yield return PadTap(pad,GamepadButton.DpadRight);
                yield return PadTap(pad,GamepadButton.East);yield return PadTap(pad,GamepadButton.East);
                Check("audio_back_returns_to_game",!menu.Controller.IsOpen&&State()=="Normal");
                var old=_input.PlayerEntity;yield return Tap(Key.F5);yield return Tap(Key.F6);
                until=Time.realtimeSinceStartupAsDouble+10;
                while(ReferenceEquals(old,_input.PlayerEntity)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
                yield return new WaitForSecondsRealtime(.5f);
                Check("loaded_world_has_one_audio_owner",!ReferenceEquals(old,_input.PlayerEntity)&&FindObjectsByType<WorldAudioRuntime>(FindObjectsSortMode.None).Length==1&&audio.Player.AllocatedSources==16);
                yield return Capture("03-audio-after-load");_worldAudioComplete=true;
            }
            finally{_worldAudioCleanup?.Invoke();_worldAudioCleanup=null;}
        }
    }
}
