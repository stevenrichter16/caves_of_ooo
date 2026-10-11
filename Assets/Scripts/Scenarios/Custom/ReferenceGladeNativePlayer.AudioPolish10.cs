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
        static readonly string[] AudioPolish10Checks={"audio10_original_vessel_acquired","audio10_finite_fill_conserved",
            "audio10_fill_imported_recording","audio10_drink_one_unit","audio10_drink_imported_recording",
            "audio10_refused_full_fill_silent","audio10_fixed_pool","audio10_preferences_restored"};
        const string AudioPolish10CanVerify="Ordinary glade actor and originally placed waterskin/draw point. Real keyboard pickup, finite water fill, refused full fill and one drink through existing inventory UI. Exact owned vessel/source/contact receipts, actual imported water clip playback and sampled per-source signal,16-source pool and temporary in-memory audio restoration.";
        const string AudioPolish10CannotVerify="This phase does not live-exercise general flask pouring or vessel-to-vessel transfer; separate native tests cover them. No vessel, water, HP, enemy, turn or RNG grants. Script-selected local route keeps ordinary scheduling; it can fail from actual danger. Source samples do not establish subjective listening, continuous unclipped output, physical speakers or Steam Deck performance. WaterDrink is a quieter shared water-movement recording, not a human gulp. Saved audio preferences are untouched.";
        WorldAudioRuntime _waterNativeRuntime;
        WorldAudioLibrary _waterNativeLibrary;
        WaterNativeEvidence _waterNativePending;
        int _waterNativeReceiptCount;
        float[] _waterNativeSamples;
        sealed class WaterNativeEvidence
        {
            public Entity Actor,Vessel,Source;public Zone Zone;public Point Contact;public WorldAudioCue Cue;
            public int Receipts,BeforeAccepted,SampleCount;public bool Exact,Playing;
            public AudioSource Voice;public AudioClip Clip;public float Peak;
        }
        IEnumerator RunAudioPolish10()
        {
            float oldSfx=WorldAudioSettings.SfxVolume;bool oldPause=AudioListener.pause;Coroutine monitor=null;
            string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] stored=keys.Select(k=>PlayerPrefs.GetFloat(k)).ToArray();
            _audioPolishCleanup=()=>
            {
                WorldAudioUtility.Callback-=ObserveWaterNative;
                if(monitor!=null)StopCoroutine(monitor);_waterNativePending=null;
                WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool restored=WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause;
                for(int i=0;i<keys.Length;i++)restored&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==stored[i]);
                Check("audio10_preferences_restored",restored);
            };
            _waterNativeRuntime=FindFirstObjectByType<WorldAudioRuntime>();Require(_waterNativeRuntime?.Player!=null,"bootstrap audio owner");
            _waterNativeLibrary=WorldAudioLibrary.LoadResources();_waterNativeSamples=new float[1024];
            foreach(var cue in new[]{WorldAudioCue.WaterFill,WorldAudioCue.WaterDrink,WorldAudioCue.WaterPour})
                Require(_waterNativeLibrary.Sound(cue,0)?.Clip!=null,"imported water family "+cue);
            WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;WorldAudioUtility.Callback+=ObserveWaterNative;
            monitor=StartCoroutine(WaterNativeMonitor());
            var actor=_input.PlayerEntity;var zone=_input.CurrentZone;
            Require(zone==_stagedZone&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled,"ordinary glade graph");
            var vessel=zone.GetCell(37,10).Objects.Single(e=>e.BlueprintName=="Waterskin");
            var source=zone.GetCell(36,9).Objects.Single(e=>e.BlueprintName=="SpreadDrawPoint");
            Require(vessel.GetPart<WaterskinPart>().Charges==0&&source.GetPart<LiquidPoolPart>().Volume==3,"original finite authored water setup");
            yield return WalkTo(37,10);yield return Tap(Key.G);if(State()=="PickupOpen")yield return Tap(Key.Tab);
            if(State()!="Normal")yield return Tap(Key.Escape);
            Check("audio10_original_vessel_acquired",actor.GetPart<InventoryPart>().Objects.Contains(vessel)
                &&vessel.GetPart<PhysicsPart>().InInventory==actor&&vessel.GetPart<WaterskinPart>().Charges==0&&zone.GetEntityCell(vessel)==null);
            yield return WalkTo(36,10);
            BeginWaterNative(vessel,source,WorldAudioCue.WaterFill);
            yield return ItemAction(vessel,"FillWaterskin");yield return FinishWaterNative("audio10_fill_imported_recording");
            yield return ResidentCloseMenus();
            Check("audio10_finite_fill_conserved",vessel.GetPart<WaterskinPart>().Charges==3
                &&source.GetPart<LiquidPoolPart>().Volume==0&&zone.GetEntityCell(source)==zone.GetCell(36,9));
            yield return Capture("water-01-actual-filled-vessel");
            // Wait out the preceding real phrase before proving a refused full fill.
            yield return new WaitForSecondsRealtime(2.4f);
            int receipts=_waterNativeReceiptCount,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            yield return ItemAction(vessel,"FillWaterskin");
            yield return ResidentCloseMenus();yield return new WaitForEndOfFrame();
            var fillClip=_waterNativeLibrary.Sound(WorldAudioCue.WaterFill,0).Clip;
            Check("audio10_refused_full_fill_silent",_waterNativeReceiptCount==receipts
                &&vessel.GetPart<WaterskinPart>().Charges==3&&source.GetPart<LiquidPoolPart>().Volume==0
                &&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy
                &&!_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying&&s.volume>0&&s.clip==fillClip));
            BeginWaterNative(vessel,null,WorldAudioCue.WaterDrink);
            yield return ItemAction(vessel,"DrinkWaterskin");yield return FinishWaterNative("audio10_drink_imported_recording");
            yield return ResidentCloseMenus();
            Check("audio10_drink_one_unit",vessel.GetPart<WaterskinPart>().Charges==2&&source.GetPart<LiquidPoolPart>().Volume==0
                &&actor.GetPart<InventoryPart>().Objects.Contains(vessel)&&vessel.GetPart<PhysicsPart>().InInventory==actor);
            Check("audio10_fixed_pool",_waterNativeRuntime.Player.AllocatedSources==16
                &&_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().Length==16&&_waterNativeReceiptCount==2);
            yield return Capture("water-02-after-real-drink");
        }
        void BeginWaterNative(Entity vessel,Entity source,WorldAudioCue cue)
        {
            var contact=source==null?Cell():_input.CurrentZone.GetEntityCell(source);Require(contact!=null,"actual water contact");
            _waterNativePending=new WaterNativeEvidence {Actor=_input.PlayerEntity,Vessel=vessel,Source=source,Zone=_input.CurrentZone,
                Contact=new Point(contact.X,contact.Y),Cue=cue,BeforeAccepted=_waterNativeRuntime.Player.AcceptedSoundCount};
        }
        void ObserveWaterNative(WorldAudioUtilityReceipt receipt)
        {
            if(receipt.Actor!=_input.PlayerEntity)return;_waterNativeReceiptCount++;
            var e=_waterNativePending;if(e==null)return;e.Receipts++;
            e.Exact=receipt.Actor==e.Actor&&receipt.Vessel==e.Vessel&&receipt.Source==e.Source&&receipt.Zone==e.Zone
                &&receipt.Contact.Equals(e.Contact)&&receipt.Cue==e.Cue;
            if(!e.Exact)return;
            var clip=_waterNativeLibrary.Sound(e.Cue,0)?.Clip;
            e.Voice=_waterNativeRuntime.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==64
                &&s.isPlaying&&s.volume>0&&s.clip!=null&&s.clip==clip);
            e.Clip=e.Voice?.clip;e.Playing=e.Clip!=null&&e.Clip.loadState==AudioDataLoadState.Loaded
                &&e.Clip.loadType==AudioClipLoadType.DecompressOnLoad&&_waterNativeRuntime.Player.AcceptedSoundCount>e.BeforeAccepted;
        }
        IEnumerator WaterNativeMonitor()
        {
            while(true)
            {
                yield return new WaitForEndOfFrame();var e=_waterNativePending;
                if(e?.Voice==null||!e.Playing||e.Voice.clip!=e.Clip||!e.Voice.isPlaying||e.Voice.volume<=0)continue;
                e.Voice.GetOutputData(_waterNativeSamples,0);
                foreach(float value in _waterNativeSamples){e.Peak=Mathf.Max(e.Peak,Mathf.Abs(value));e.SampleCount++;}
            }
        }
        IEnumerator FinishWaterNative(string check)
        {
            var e=_waterNativePending;Require(e!=null,"pending actual water observation");
            double until=Time.realtimeSinceStartupAsDouble+.3;
            while(e.Peak<=.000001f&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            Check(check,e.Receipts==1&&e.Exact&&e.Playing&&e.Peak>.000001f&&e.SampleCount>0);
            _descriptions.Add(new Description{subject=check,source=e.Vessel?.ID??e.Source?.ID??e.Actor.ID,text="Cue="+e.Cue+"; original source="+e.Source?.ID
                +"; exact receipt="+e.Exact+"; count="+e.Receipts+"; imported playing="+e.Playing+"; clip="+e.Clip?.name
                +"; sampled peak="+e.Peak+"; floats="+e.SampleCount+". Noncontiguous source samples, not a subjective listening or physical output claim."});
            _waterNativePending=null;
        }
    }
}
