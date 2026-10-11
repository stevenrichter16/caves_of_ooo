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
        static readonly string[] AudioPolish07Checks={"tail_actual_generated_room","tail_paid_waits_approach_clock_boundary","tail_actual_imported_phrase",
            "tail_paid_input_crosses_real_band","tail_same_voice_continues","tail_retained_voice_outputs_signal","tail_real_save_load_keeps_clock",
            "tail_idle_keeps_scan_and_voice_bounds","tail_ordinary_finish","tail_audio_preferences_restored"};
        const string AudioPolish07CanVerify="A valid imported stone settling phrase continues on the same native AudioSource beyond its prior sample while one ordinary paid wait crosses the actual saved day band. The existing generated room/wall, positive signal, actual F5/F6 clock restoration, source budget and ordinary actor remain verified. Eleven checks include shared boot.";
        const string AudioPolish07CannotVerify="One disclosed travel shortcut reaches an existing exterior room approach, followed by actual steps and at most80 actual waits. The clock is never assigned/advanced directly; no cue or playhead is injected. This live gate covers one actual structural tail across a band. Fire, daytime wildlife retirement and habitat-policy counters are native unit coverage, not claimed live observations. Noncontiguous signal samples do not establish subjective listening, physical output devices, whole-run clipping or Deck performance.";
        IEnumerator RunAudioPolish07Tails()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("tail_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;Require(!AudioListener.pause,"normal isolated listener");
            var actor=_input.PlayerEntity;string id=actor.ID;var site=Room05FindSite();Require(site!=null,"actual generated stone room for clock observation");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"existing bootstrap audio");
            yield return River03Travel(site.Zone,site.Outside,"actual generated room; valid phrase tail across saved clock band");
            foreach(var cell in new[]{site.Gap,site.Inside,site.Listen})
            {if(Cell()==cell)continue;Require(Room05Safe(site.Zone,cell),"safe actual room step");var before=Cell();yield return Tap(Direction(cell.X-before.X,cell.Y-before.Y));Require(Cell()==cell,"real room entry");}
            var at=site.Zone.GetEntityCell(site.Wall);
            Check("tail_actual_generated_room",Cell()==site.Listen&&Cell().IsInterior&&at.IsVisible&&!at.IsInterior&&site.Wall.GetPart<MaterialPart>()?.MaterialID=="Stone");
            int began=_input.TurnManager.TickCount,waits=0;
            while(!Tail07NextWaitCrossesBand()&&waits<80)
            {
                Require(Cell()==site.Listen&&actor.GetStatValue("Hitpoints")>10&&Room05Safe(site.Zone,Cell()),"actual room remains safe without suppressing NPCs");
                yield return Tap(Key.Period);yield return ResidentCloseMenus();waits++;
            }
            Check("tail_paid_waits_approach_clock_boundary",waits>0&&waits<=80&&_input.TurnManager.TickCount>began&&Tail07NextWaitCrossesBand());
            Require(Tail07NextWaitCrossesBand(),"one genuine wait must cross current saved clock band");
            yield return Capture("tail-01-before-real-band-boundary");
            var lib=WorldAudioLibrary.LoadResources();var clips=new[]{lib.Detail("stone-shift",0)?.Clip,lib.Detail("stone-shift",1)?.Clip};Require(clips.All(c=>c!=null),"real imported structural phrase");
            AudioSource voice=null;double until=Time.realtimeSinceStartupAsDouble+45;
            while(voice==null&&Time.realtimeSinceStartupAsDouble<until)
            {
                voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip)
                    &&s.clip.length-(float)s.timeSamples/s.clip.frequency>.3f
                    &&Time.time-(float)Field(_input,"_lastMoveTime")>=_input.MoveRepeatDelay);yield return null;
            }
            Check("tail_actual_imported_phrase",voice!=null&&Cell()==site.Listen&&at.IsVisible);Require(voice!=null,"long-enough natural phrase, no seek or replay");
            var clip=voice.clip;int sample=voice.timeSamples,count=audio.Player.AcceptedDetailCount,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);var beforeBand=WorldClock.GetBand(tick);
            // Same native keyboard route as Tap, without its post-action .13s dwell:
            // imported stone takes are only .50/.65s, and the existing tail is the proof.
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Period));yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForEndOfFrame();
            var afterBand=WorldClock.GetBand(_input.TurnManager.TickCount);
            Check("tail_paid_input_crosses_real_band",afterBand!=beforeBand&&River03OneTurn(tick,energy)&&Cell()==site.Listen&&at.IsVisible);
            Check("tail_same_voice_continues",voice!=null&&voice.clip==clip&&voice.isPlaying&&voice.timeSamples>sample&&audio.Player.AcceptedDetailCount==count);
            float peak=0;int n=0;var samples=new float[1024];until=Time.realtimeSinceStartupAsDouble+.2;
            while(voice!=null&&voice.isPlaying&&voice.clip==clip&&Time.realtimeSinceStartupAsDouble<until)
            {voice.GetOutputData(samples,0);foreach(float value in samples){peak=Mathf.Max(peak,Mathf.Abs(value));n++;}yield return null;}
            Check("tail_retained_voice_outputs_signal",n>0&&peak>.000001f);yield return Capture("tail-02-after-real-band-boundary");
            int savedTick=_input.TurnManager.TickCount;var beforeLoad=actor;yield return Tap(Key.F5);yield return Tap(Key.F6);until=Time.realtimeSinceStartupAsDouble+10;
            while(ReferenceEquals(_input.PlayerEntity,beforeLoad)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            yield return ResidentCloseMenus();
            Check("tail_real_save_load_keeps_clock",!ReferenceEquals(_input.PlayerEntity,beforeLoad)&&_input.PlayerEntity.ID==id&&_input.TurnManager.TickCount==savedTick&&WorldClock.GetBand(_input.TurnManager.TickCount)==afterBand);
            // Load/menu completion can resume before this frame's LateUpdate.
            yield return new WaitForEndOfFrame();
            Require((int)Field(audio,"tick")==_input.TurnManager.TickCount
                &&ReferenceEquals(Field(audio,"zone"),_input.CurrentZone)&&ReferenceEquals(Field(audio,"actor"),_input.PlayerEntity),"loaded context sampled before idle baseline");
            int scans=audio.Soundscape.ScanCount;yield return new WaitForSecondsRealtime(2);
            Check("tail_idle_keeps_scan_and_voice_bounds",audio.Soundscape.ScanCount==scans&&audio.Soundscape.LastVisitedOwners<=512&&audio.Player.AllocatedSources==16);
            Check("tail_ordinary_finish",_input.PlayerEntity.ID==id&&_input.PlayerEntity.GetStatValue("Hitpoints")>0&&_input.PlayerEntity.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&State()=="Normal"&&_river03Shortcuts==1);
            _descriptions.Add(new Description{subject="Actual clock-tail continuation",source=site.Zone.ZoneID+":"+site.Wall.ID,text="Paid waits="+waits+"; band="+beforeBand+" -> "+afterBand+"; original sample="+sample+"; retained source peak="+peak+"; saved/restored tick="+savedTick+". Native callback timing does not prove aesthetic quality."});
        }
        bool Tail07NextWaitCrossesBand()
        {
            var actor=_input.PlayerEntity;int speed=actor.GetStatValue("Speed",TurnManager.DefaultSpeed),energy=_input.TurnManager.GetEnergy(actor),tick=_input.TurnManager.TickCount;
            if(speed<=0||energy<TurnManager.ActionThreshold)return false;
            int need=Math.Max(0,2*TurnManager.ActionThreshold-energy),ticks=(need+speed-1)/speed;
            return ticks>0&&WorldClock.GetBand(tick)!=WorldClock.GetBand(tick+ticks);
        }
    }
}
