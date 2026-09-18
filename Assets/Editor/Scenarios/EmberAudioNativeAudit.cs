using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit isolated native audio acceptance. Real main scene, real skill
    /// commands, normal coordinator and DSP output; no replacement gameplay/audio backend.</summary>
    [InitializeOnLoad]
    public static class EmberAudioNativeAudit
    {
        const string Prefix="EmberAudio.Audit.";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static InputHandler input;
        static Entity dummy;
        static double readyAt,phaseStart,lastCast,profileStart;
        static int stage,acceptedBefore,impactsBefore,frames,lastFrame;
        static readonly float[] output=new float[1024];
        static AudioSource[] sources;
        static ProfilerRecorder recorder;
        static Report report;
        static float oldVolume,oldSpeed;
        static SpellFxMode oldMode;
        static bool oldBackground,oldNative,settingsCaptured;
        static readonly System.Random rng=new System.Random(7301);
        [Serializable] sealed class Report
        {
            public string runId,privateRoot,error,scope="Actual main scene command routing and Unity mixer samples; no claim of headphone output or subjective listening.";
            public bool passed,bootAccepted,nativeReady,threeAssignedLayers,bodyPlaying,impactPlaying,mutedSilent,oneHitPerCast,poolStable,tailCleared;
            public int commands,accepted,impacts,peakSources,profileFrames,profileSamples,unexpectedErrors;
            public double bodyPeak,impactPeak,mutedPeak,profileSeconds,updateMeanMicroseconds,updateMaxMicroseconds,dspStart,dspEnd;
        }
        static EmberAudioNativeAudit()
        { if(SessionState.GetBool(Prefix+"active",false)) Subscribe(); }
        [MenuItem("Caves Of Ooo/Scenarios/Magic/Ember Spit Audio Acceptance")]
        public static void Launch()=>Run(false);
        public static void RunFromCommandLine()=>Run(true);
        static void Run(bool exit)
        {
            if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start native audio acceptance from an idle native Editor.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Audio audit refuses to discard dirty scenes.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Main/SampleScene.unity")
                throw new InvalidOperationException("Open the Main SampleScene first.");
            SessionState.SetString(Prefix+"id",Guid.NewGuid().ToString("N"));
            SessionState.SetInt(Prefix+"seed",NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetInt(Prefix+"errors",0);
            string token=NativeSaveIsolation.Begin(Prefix,"ember-audio-private-marker",exit);
            SessionState.SetString(Prefix+"token",token);SessionState.SetBool(Prefix+"active",true);
            SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+240);
            Subscribe();EditorApplication.isPlaying=true;
        }
        static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix,SessionState.GetString(Prefix+"token",""),Finish);
            NativeAuditBootstrapSettings.RequestedSeed=729490642;
            GameBootstrap.OnAfterBootstrap-=Boot;GameBootstrap.OnAfterBootstrap+=Boot;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string trace,LogType kind)
        {if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)SessionState.SetInt(Prefix+"errors",SessionState.GetInt(Prefix+"errors",0)+1);}
        static void Boot(Zone zone,EntityFactory factory,Entity player,TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap-=Boot;
            report=new Report{runId=SessionState.GetString(Prefix+"id",""),privateRoot=SaveGameService.SaveRootOverride};
            readyAt=EditorApplication.timeSinceStartup+1;stage=0;
        }
        sealed class NewGameProbe:IInputProbe {public bool GetKeyDown(KeyCode key)=>key==KeyCode.N;}
        static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false))return;
            try
            {
                double now=EditorApplication.timeSinceStartup;
                Require(now<SessionState.GetFloat(Prefix+"deadline",0),"Native audio audit watchdog expired.");
                if(report==null||now<readyAt||!EditorApplication.isPlaying)return;
                if(stage==0)
                {
                    input=UnityEngine.Object.FindFirstObjectByType<InputHandler>();Require(input!=null,"Main input missing.");
                    oldVolume=SpellFxSettings.SoundVolume;oldSpeed=SpellFxSettings.AnimationSpeed;oldMode=SpellFxSettings.Mode;
                    oldBackground=Application.runInBackground;oldNative=Village3DSettings.Enabled;settingsCaptured=true;
                    Application.runInBackground=true;Village3DSettings.Enabled=true;
                    SpellFxSettings.SoundVolume=1;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.Mode=SpellFxMode.Full;
                    var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Private).GetValue(input);
                    var service=(ISaveLoadService)typeof(InputHandler).GetField("_saveLoadService",Private).GetValue(null);
                    Require(boot.IsActive,"Private marker must select the normal boot menu.");boot.Tick(new NewGameProbe(),service,MessageLog.Add);
                    report.bootAccepted=!boot.IsActive;Require(report.bootAccepted,"New game dispatch failed.");
                    PlaceFixture();stage=1;readyAt=now+2;return;
                }
                if(stage==1)
                {
                    var fx=input.ZoneRenderer.WorldFx;
                    if(!fx.NativeRenderer.IsPrepared){Require(now-readyAt<15,"Native surface did not prepare.");return;}
                    report.nativeReady=true;Require(fx.EmberAudio.IsPrepared,"Audio did not prepare before cast.");
                    sources=fx.EmberAudio.Root.GetComponentsInChildren<AudioSource>();report.peakSources=sources.Length;
                    Require(sources.Length==12,"Audio pool not twelve sources.");report.dspStart=AudioSettings.dspTime;
                    Cast();phaseStart=now;stage=2;return;
                }
                var audio=input.ZoneRenderer.WorldFx.EmberAudio;
                double peak=0;AudioListener.GetOutputData(output,0);for(int i=0;i<output.Length;i++)peak=Math.Max(peak,Math.Abs(output[i]));
                bool body=false,impact=false;int assigned=0;
                foreach(var source in sources)
                    if(source!=null && source.clip!=null)
                    {assigned++;if(source.isPlaying&&source.timeSamples>0){if(source.clip.name.Contains("impact"))impact=true;else body=true;}}
                if(stage==2)
                {
                    report.threeAssignedLayers|=assigned==3;
                    report.bodyPlaying|=body;report.impactPlaying|=impact;
                    if(body)report.bodyPeak=Math.Max(report.bodyPeak,peak);
                    if(impact)report.impactPeak=Math.Max(report.impactPeak,peak);
                    if(now-phaseStart<2)return;
                    Require(audio.AcceptedCount==acceptedBefore+1&&audio.ImpactCount==impactsBefore+1,"One command must play exactly one impact.");
                    Require(report.bodyPlaying&&report.impactPlaying&&report.bodyPeak>1e-5&&report.impactPeak>1e-5,"Real PCM source/mixer output missing.");
                    Require(audio.ActiveVoices==0,"Natural tail failed to finish.");
                    SpellFxSettings.SoundVolume=0;Cast();phaseStart=now;stage=3;return;
                }
                if(stage==3)
                {
                    report.mutedPeak=Math.Max(report.mutedPeak,peak);
                    if(now-phaseStart<2)return;
                    report.mutedSilent=audio.AcceptedCount==acceptedBefore&&audio.ImpactCount==impactsBefore&&report.mutedPeak<1e-6;
                    Require(report.mutedSilent,"Muted real command produced audio.");
                    SpellFxSettings.SoundVolume=1;profileStart=lastCast=now;frames=0;lastFrame=-1;
                    recorder=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"COO.EmberAudio.Update",1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame);
                    Require(recorder.Valid,"Audio profiler marker unavailable.");stage=4;return;
                }
                if(stage==4)
                {
                    if(Time.frameCount!=lastFrame)
                    {
                        lastFrame=Time.frameCount;frames++;
                        if(recorder.Count>0){double us=recorder.LastValue/1000.0;report.updateMeanMicroseconds+=us;report.updateMaxMicroseconds=Math.Max(report.updateMaxMicroseconds,us);report.profileSamples++;}
                    }
                    Require(audio.AllocatedSources==12,"Audio pool grew during repeated casts.");
                    if(now-lastCast>=2&&now-profileStart<59){Cast();lastCast=now;}
                    if(now-profileStart<61)return;
                    report.profileSeconds=now-profileStart;report.profileFrames=frames;
                    if(report.profileSamples>0)report.updateMeanMicroseconds/=report.profileSamples;
                    report.accepted=audio.AcceptedCount;report.impacts=audio.ImpactCount;
                    report.oneHitPerCast=report.accepted==report.commands-1&&report.impacts==report.accepted;
                    report.poolStable=audio.AllocatedSources==12;report.tailCleared=audio.ActiveVoices==0;
                    report.dspEnd=AudioSettings.dspTime;
                    Require(report.oneHitPerCast&&report.poolStable&&report.tailCleared&&report.profileSamples>30,"Repeated-cast/audio lifetime/profile gate failed.");
                    Finish(0);
                }
            }
            catch(Exception error){if(report==null)report=new Report{runId=SessionState.GetString(Prefix+"id","")};report.error=error.ToString();Finish(1);}
        }
        static void PlaceFixture()
        {
            var zone=input.CurrentZone;var actor=input.PlayerEntity;int x0=-1,y0=-1;
            for(int distance=0;distance<Zone.Width+Zone.Height&&x0<0;distance++)for(int y=3;y<Zone.Height-3&&x0<0;y++)for(int x=3;x<Zone.Width-6;x++)
            {
                if(Math.Abs(x-40)+Math.Abs(y-12)!=distance)continue;bool clear=true;
                for(int k=0;k<=3;k++){var c=zone.GetCell(x+k,y);if(c.IsSolid()||c.BlocksMovement(actor)||c.Occupants.Any(e=>e!=actor&&AbilityTargeting.IsElementalTarget(e,actor))){clear=false;break;}}
                if(clear){x0=x;y0=y;break;}
            }
            Require(x0>=0,"No existing open lane; audit will not erase scenery.");
            Require(zone.RemoveEntity(actor)&&zone.AddEntity(actor,x0,y0),"Fixture player placement failed.");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(input,new object[]{new ZoneTransitionResult{Success=true,NewZone=zone,NewPlayerX=x0,NewPlayerY=y0}});
            dummy=new Entity{ID="ember-audio-owned-target",BlueprintName="CaveHermit"};dummy.SetTag("Creature");
            dummy.AddPart(new RenderPart{DisplayName="audio study target",RenderString="h",ColorString="&W"});dummy.AddPart(new PhysicsPart{Solid=false});dummy.AddPart(new StatusEffectsPart());
            dummy.Statistics["Hitpoints"]=new Stat{Owner=dummy,Name="Hitpoints",BaseValue=10000,Min=0,Max=10000};
            Require(zone.AddEntity(dummy,x0+3,y0),"Target placement failed.");input.ZoneRenderer.RenderZone();
            var skills=actor.GetPart<SkillsPart>();if(skills.GetSkill(nameof(Pyromancy_EmberSpit))==null)Require(skills.AddSkill(new Pyromancy_EmberSpit()),"Ember learning failed.");
        }
        static void Cast()
        {
            var zone=input.CurrentZone;var actor=input.PlayerEntity;var skills=actor.GetPart<SkillsPart>();
            var skill=skills.GetSkill(nameof(Pyromancy_EmberSpit));var ability=actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            ability.CooldownRemaining=0;dummy.GetPart<StatusEffectsPart>().RemoveAllEffects();dummy.GetStat("Hitpoints").BaseValue=10000;
            var source=zone.GetEntityCell(actor);var target=zone.GetEntityCell(dummy);
            Require(source.IsVisible&&target.IsVisible,"Real FOV must contain caster and target.");
            Require(!input.ZoneRenderer.WorldFx.HasBlockingFx&&SpellFxBus.PendingCount==0,"Previous visual cast must finish.");
            var audio=input.ZoneRenderer.WorldFx.EmberAudio;acceptedBefore=audio.AcceptedCount;impactsBefore=audio.ImpactCount;
            bool consumed=skills.TryRouteSkillCommand(ability.Command,zone,rng,1,0,source,zone.GetCell(source.X+1,source.Y),ability.Range,out bool blocks);
            Require(consumed&&blocks&&SpellFxBus.PendingCount==1&&ability.CooldownRemaining>0,"Real Ember command must resolve and queue normally.");report.commands++;
        }
        static void Finish(int code)
        {
            if(!SessionState.GetBool(Prefix+"active",false))return;
            EditorApplication.update-=Poll;GameBootstrap.OnAfterBootstrap-=Boot;Application.logMessageReceived-=Log;
            if(recorder.Valid)recorder.Dispose();
            if(settingsCaptured){input?.ZoneRenderer?.WorldFx?.CancelAll();SpellFxSettings.SoundVolume=oldVolume;SpellFxSettings.AnimationSpeed=oldSpeed;SpellFxSettings.Mode=oldMode;Village3DSettings.Enabled=oldNative;Application.runInBackground=oldBackground;}
            NativeAuditBootstrapSettings.RequestedSeed=SessionState.GetInt(Prefix+"seed",0);
            if(report!=null)
            {
                report.unexpectedErrors=SessionState.GetInt(Prefix+"errors",0);if(report.unexpectedErrors>0)code=1;report.passed=code==0;
                string dir=Path.GetFullPath("Docs/Verification/EmberAudioIntegration/Native");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,report.runId+".json"),JsonUtility.ToJson(report,true));
            }
            SessionState.SetBool(Prefix+"active",false);SaveGameService.RegisterRuntime(null,null);
            NativeSaveIsolation.Finish(Prefix,SessionState.GetString(Prefix+"token",""),code);
        }
        static void Require(bool pass,string message){if(!pass)throw new InvalidOperationException(message);}
    }
}
