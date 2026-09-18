using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Explicit native acceptance, never ordinary bootstrap content.
    /// One keyboard cast plus labelled deterministic command fixtures use the real
    /// Main scene, native presenters and sole WorldFxCoordinator consumer.</summary>
    public sealed class StarterSpell3DNativeAudit : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => failures + unexpected;
        public string RunId { get; private set; }
        const int Seed=729490642, Capacity=64000;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string[] SpellIds={nameof(Pyromancy_EmberSpit),nameof(Pyromancy_FlamingHands),nameof(Hydromancy_JetBlast),nameof(Galvanism_GroundSurge),nameof(Cryomancy_RimeGrip),nameof(Spellcraft_Calm),nameof(Hydromancy_ConjureRain)};
        static readonly Type[] SkillTypes={typeof(Pyromancy_EmberSpit),typeof(Pyromancy_FlamingHands),typeof(Hydromancy_JetBlast),typeof(Galvanism_GroundSurge),typeof(Cryomancy_RimeGrip),typeof(Spellcraft_Calm),typeof(Hydromancy_ConjureRain)};
        static readonly string[] Counters={"COO.ZoneRenderer.LateUpdate","COO.Input.Update","Main Thread","GC Allocated In Frame","Draw Calls Count","Triangles Count"};
        readonly List<CheckRow> checks=new List<CheckRow>();
        readonly List<CastRow> casts=new List<CastRow>();
        readonly List<Phase> phases=new List<Phase>();
        readonly List<string> screenshots=new List<string>(),loopFrames=new List<string>();
        readonly HashSet<string> exercised=new HashSet<string>(StringComparer.Ordinal);
        readonly List<Entity> fixtureEntities=new List<Entity>();
        readonly List<MeshFilter> visibleMeshScratch=new List<MeshFilter>(384);
        readonly List<BaseSkillPart> addedSkills=new List<BaseSkillPart>();
        readonly Frame[] frames=new Frame[Capacity];
        InputHandler input; Keyboard keyboard,oldKeyboard; InputSettings originalSettings,ownedSettings;
        SpellFxMode originalMode; float originalSpeed,originalFlash,originalShake;
        bool originalBackground,originalEnabled,originalLow,initialized,cleaned,completed;
        readonly bool[] prefPresent=new bool[2];readonly int[] prefValue=new int[2];
        readonly string[] prefKeys={Village3DSettings.PreferenceKey,Village3DSettings.LowDetailPreferenceKey};
        int failures,unexpected,nativeKeyboardCasts,currentPhase=-1,frameCount,activeFrames;
        string saveRoot,markerId,gameId,fatal;byte[] markerBytes;
        Entity dummy,crop;Zone fixtureZone;ZoneTileState fixtureGroundBaseline; (int x,int y) lane;
        Stack<IEnumerator> activeSteps;System.Diagnostics.Stopwatch watch;Report report;
        readonly ColdSample[] coldFrames=new ColdSample[512];
        bool coldArmed;int coldCount;ColdReport cold;NativeSpellFxPreparation preparation;
        ProfilerRecorder[] recorders; bool[] counterAvailable;string[] counterUnits;
        string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/StarterSpell3D/Integration/NativeAudit"));
        string Stem=>"SSN-"+RunId;
        public string ReportPath=>Path.Combine(Dir,Stem+"-native.json");
        WorldFxCoordinator Fx=>input?.ZoneRenderer?.WorldFx;
        NativeSpellFxRenderer Native=>Fx?.NativeRenderer;
        double Now=>Time.realtimeSinceStartupAsDouble;

        public void Initialize(string runId)
        {
            Require(Guid.TryParseExact(runId,"N",out var id)&&id!=Guid.Empty,"Explicit unique native run ID required.");
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"NativeSaveIsolation must precede bootstrap.");
            RunId=runId;saveRoot=SaveGameService.SaveRootOverride;markerId=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            markerBytes=File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz"));Directory.CreateDirectory(Dir);
            watch=System.Diagnostics.Stopwatch.StartNew();originalMode=SpellFxSettings.Mode;originalSpeed=SpellFxSettings.AnimationSpeed;originalFlash=SpellFxSettings.FlashIntensity;originalShake=SpellFxSettings.ShakeIntensity;
            originalEnabled=Village3DSettings.Enabled;originalLow=Village3DSettings.LowDetail;
            for(int i=0;i<2;i++){prefPresent[i]=PlayerPrefs.HasKey(prefKeys[i]);prefValue[i]=PlayerPrefs.GetInt(prefKeys[i]);}
            originalBackground=Application.runInBackground;originalSettings=InputSystem.settings;oldKeyboard=Keyboard.current;
            initialized=true;ownedSettings=Instantiate(originalSettings);ownedSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            ownedSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=ownedSettings;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }
        IEnumerator RunSafely(IEnumerator root)
        {
            activeSteps=new Stack<IEnumerator>();activeSteps.Push(root);
            try
            {
                while(activeSteps.Count>0)
                {
                    bool moved=false;object current=null;Exception error=null;
                    try{moved=activeSteps.Peek().MoveNext();if(moved)current=activeSteps.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){fatal=error.ToString();Check("fatal",false,fatal);break;}
                    if(!moved){DisposeStep(activeSteps.Pop());continue;}
                    if(current is IEnumerator nested){activeSteps.Push(nested);continue;}
                    yield return current;
                }
            }
            finally{DisposePendingSteps();}
            Finish();
        }
        void DisposeStep(IEnumerator step){try{(step as IDisposable)?.Dispose();}catch(Exception e){Check("iterator_cleanup",false,e.ToString());}}
        void DisposePendingSteps(){while(activeSteps!=null&&activeSteps.Count>0)DisposeStep(activeSteps.Pop());}

        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);input=FindFirstObjectByType<InputHandler>();Require(input!=null,"Ordinary input exists.");
            Require(Screen.width==1920&&Screen.height==1080,"Actual1920x1080 GameView required.");
            var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Private).GetValue(input);Require(boot.IsActive,"Private marker opens ordinary boot menu.");
            yield return Tap(Key.N);
            Check("native_N_real_new_game",!boot.IsActive&&State()=="Normal"&&input.WorldMap.Seed==Seed&&input.CurrentZone.ZoneID=="Overworld.3.6.0");Require(checks.Last().pass,"Ordinary seeded N bootstrap failed.");
            gameId=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.ShakeIntensity=0;
            lane=FindLane(input.CurrentZone);FixturePlace(input.CurrentZone,lane);yield return WaitForNative();
            Check("native_town_ready",input.ZoneRenderer.Village3D!=null&&input.ZoneRenderer.Village3D.IsReady&&input.ZoneRenderer.Village3D.PresentationVisible);
            PrepareFixtures();yield return null;input.ZoneRenderer.RenderZone();
            BeginCounters();yield return KeyboardCast();
            for(int i=0;i<7;i++)yield return CommandCase(i,"showcase",true);
            Check("all_seven_command_outcomes",exercised.SetEquals(SpellIds),string.Join(",",exercised));
            SpellFxSettings.Mode=SpellFxMode.Reduced;yield return WaitForFx();yield return null;
            yield return CommandCase(4,"reduced-success",false);
            yield return CommandCase(4,"deny-frozen",true);Check("rime_denied_reduced_feedback",casts.Last().rejectedFrozen&&casts.Last().peakMeshes>0&&casts.Last().damage>0);
            yield return CommandCase(5,"reduced-success",false);
            yield return CommandCase(5,"peaceful",false);Check("calm_peaceful_no_new_status",casts.Last().rejectedPacified&&casts.Last().damage==0);
            yield return CommandCase(6,"empty-rain",false);Check("rain_empty_no_crop_mesh",casts.Last().targets==0&&casts.Last().peakMeshes==0);
            yield return ProfilePair();
            RemoveFixtures();yield return WaitForFx();FixturePlace(input.CurrentZone,(40,23));yield return WaitForNative();
            yield return Tap(Key.S);Require(Position()==(40,24),"Ordinary first south road step.");yield return Tap(Key.S);
            Check("native_south_entry",input.CurrentZone.ZoneID=="Overworld.3.7.0"&&Position()==(40,0));Require(checks.Last().pass,"Ordinary south seam transition failed.");
            lane=FindLane(input.CurrentZone);FixturePlace(input.CurrentZone,lane);yield return WaitForNative();PrepareFixtures();input.ZoneRenderer.RenderZone();yield return null;
            yield return CommandCase(0,"south-showcase",true);yield return ProfilePair();RemoveFixtures();
            double total=phases.Sum(p=>p.seconds);Check("paired_80second_profile_complete",phases.Count==4&&phases.All(p=>p.seconds>=20&&p.seconds<=23&&p.casts>=7&&p.activeFrames>0)&&total>=80&&total<=90);
            Check("private_boot_marker_unchanged",File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz")).SequenceEqual(markerBytes));
            Check("owned_save_only",SaveGameService.SaveRootOverride==saveRoot&&!string.IsNullOrEmpty(gameId)&&gameId!=markerId&&Directory.Exists(Path.Combine(saveRoot,gameId))&&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",gameId)));
            yield return WaitForFiles();
            foreach(var cast in casts) foreach(var frame in cast.frames) frame.sha256=FileHash(frame.path);
            completed=true;
        }
        IEnumerator KeyboardCast()
        {
            var abilities=input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();int slot=-1;
            for(int i=0;i<ActivatedAbilitiesPart.SlotCount;i++)if(abilities.GetAbilityBySlot(i)?.Command=="CommandFlamingHands"){slot=i;break;}
            Require(slot>=0,"Shipped starter Flaming Hands must actually be bound.");ResetTarget(1,"keyboard");yield return null;input.ZoneRenderer.RenderZone();
            var ability=abilities.GetAbilityBySlot(slot);Require(ability.CooldownRemaining==0,"Ordinary starter ability is ready, no cooldown fixture reset.");
            int hp=dummy.GetStatValue("Hitpoints"),tick=input.TurnManager.TickCount;var source=Position();
            yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));
            Require(State()=="AwaitingDirection","Actual hotbar opens direction targeting.");
            preparation=Native.LastPreparation;
            Check("native_spell_prepared_before_cast",Native.IsPrepared&&preparation!=null&&preparation.ViewCount==NativeSpellFxRenderer.MaximumFullMeshes&&Native.ActiveCount==0&&Native.LastEntry==null);
            Require(checks.Last().pass,"Native art and bounded pool must be ready before the first direction press.");
            cold=new ColdReport{libraryInstancesBefore=Resources.FindObjectsOfTypeAll<NativeSpellFxLibrary>().Length,allocatedMemoryBefore=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),poolBefore=Native.AllocatedViewCount,startSeconds=Now};coldArmed=true;
            yield return Tap(Key.D);
            double end=Now+5;
            while(Fx.HasBlockingFx||State()=="WaitingForFxResolution") {Require(Now<end,"Keyboard cast failed to release turn.");yield return null;}
            coldArmed=false;cold.wallSeconds=Now-cold.startSeconds;cold.libraryInstancesAfter=Resources.FindObjectsOfTypeAll<NativeSpellFxLibrary>().Length;cold.allocatedMemoryAfter=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();cold.poolAfter=Native.AllocatedViewCount;cold.samples=coldFrames.Take(coldCount).ToArray();
            cold.maxFrameSeconds=cold.samples.Length==0?0:cold.samples.Max(f=>f.delta);cold.maxInputNanoseconds=cold.samples.Length==0?0:cold.samples.Max(f=>f.input);cold.maxMainNanoseconds=cold.samples.Length==0?0:cold.samples.Max(f=>f.main);cold.maxGcBytes=cold.samples.Length==0?0:cold.samples.Max(f=>f.gc);
            // Observe from BEFORE the actual direction press. Tap deliberately waits for
            // key release/debounce; a short effect can finish before that helper returns.
            cold.peakMeshes=cold.samples.Length==0?0:cold.samples.Max(f=>f.meshes);
            cold.sawBlocking=cold.samples.Any(f=>f.blocking);
            cold.damage=hp-dummy.GetStatValue("Hitpoints");cold.cooldown=ability.CooldownRemaining;
            cold.ticks=input.TurnManager.TickCount-tick;cold.sourceUnchanged=Position()==source;
            cold.nativeEntry=Native.LastEntry?.SpellId;cold.finalState=State();
            Check("first_cast_reuses_prepared_assets",cold.libraryInstancesBefore==cold.libraryInstancesAfter&&cold.libraryInstancesBefore>0&&cold.poolBefore==cold.poolAfter&&cold.poolBefore==NativeSpellFxRenderer.MaximumFullMeshes);
            bool pass=cold.damage>0&&cold.cooldown>0&&cold.sourceUnchanged&&cold.ticks>0&&cold.nativeEntry==SpellIds[1]&&cold.peakMeshes>0&&cold.sawBlocking&&cold.finalState=="Normal";
            Check("native_keyboard_starter_cast",pass,JsonUtility.ToJson(new KeyboardEvidence{slot=slot,damage=cold.damage,cooldown=cold.cooldown,ticks=cold.ticks,peakMeshes=cold.peakMeshes,sawBlocking=cold.sawBlocking,sourceUnchanged=cold.sourceUnchanged,nativeEntry=cold.nativeEntry,finalState=cold.finalState}));Require(pass,"Ordinary keyboard starter cast must reach native art and finish its turn.");nativeKeyboardCasts++;
        }
        IEnumerator CommandCase(int index,string mode,bool capture)
        {
            yield return WaitForFx();
            // A normal next action may interrupt the previous gesture's short settling
            // tail. This isolated restoration comparison instead starts from real Idle;
            // otherwise it mistakes that preceding cast's private override for the base.
            if(mode!="profile"){yield return WaitForIdleGesture();ResetFixtureGround();}
            ResetTarget(index,mode);yield return null;input.ZoneRenderer.RenderZone();
            // Observe the live Animator, never SampleAnimation or a replacement rig.
            // These per-cast allocations and bone reads stay outside steady profiling.
            GestureObservation gesture=mode=="profile"?null:ObserveGesture(index);
            var skill=EnsureSkill(index);var ability=input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            ability.CooldownRemaining=0; // Explicit deterministic fixture reset; ordinary keyboard proof above never resets it.
            var rng=new FixedRng(mode=="deny-charge"?99:0);int tick=input.TurnManager.TickCount;
            Require(SpellFxBus.PendingCount==0,"Fixture cannot mix an older queued sequence.");
            var source=input.CurrentZone.GetEntityCell(input.PlayerEntity);var selected=input.CurrentZone.GetCell(source.X+1,source.Y);
            bool consumed=input.PlayerEntity.GetPart<SkillsPart>().TryRouteSkillCommand(ability.Command,input.CurrentZone,rng,1,0,source,selected,ability.Range,out bool blocks);
            // Observation only, synchronously before yielding. The real coordinator alone drains Pending.
            var pending=(IList)typeof(SpellFxBus).GetField("Pending",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Require(consumed&&blocks&&pending.Count==1,"Actual command must emit one accepted sequence.");var sequence=(SpellFxSequence)pending[0];
            Require(sequence.SpellId==SpellIds[index]&&ReferenceEquals(sequence.Zone,input.CurrentZone),"Copied outcome belongs to this exact skill/zone.");
            var row=new CastRow{spell=sequence.SpellId,mode=mode,zoneId=input.CurrentZone.ZoneID,fxMode=SpellFxSettings.Mode.ToString(),sourceX=sequence.Source.X,sourceY=sequence.Source.Y,
                targets=sequence.Targets.Count,path=sequence.Path.Select(p=>p.X+","+p.Y).ToArray(),affected=sequence.AffectedCells.Select(p=>p.X+","+p.Y).ToArray(),
                damage=sequence.Targets.Sum(t=>t.Damage),applied=sequence.Targets.SelectMany(t=>t.AppliedEffects).ToArray(),rejected=sequence.Targets.SelectMany(t=>t.RejectedEffects).ToArray(),
                copiedTargetIds=sequence.Targets.Select(t=>t.TargetId).ToArray(),targetCells=sequence.Targets.Select(t=>t.Cell.X+","+t.Cell.Y+"->"+t.FinalCell.X+","+t.FinalCell.Y).ToArray(),
                rejectedFrozen=sequence.Targets.Any(t=>t.RejectedEffects.Contains(nameof(FrozenEffect))),rejectedPacified=sequence.Targets.Any(t=>t.RejectedEffects.Contains("Pacified")),
                cooldown=ability.CooldownRemaining,rngCalls=rng.Calls,groundFixtureReset=mode!="profile"};casts.Add(row);
            Require(ability.CooldownRemaining>0,"Actual command applies its normal cooldown.");
            if(mode=="showcase"||mode=="south-showcase")
            {
                Require(sequence.Targets.Any(t=>t.TargetId==(index==6?crop.ID:dummy.ID)),"Showcase must resolve on its actual intended recipient.");
                if(index<=4)Require(row.damage>0,"Damage spell showcase must hit, not merely display an empty lane.");
                if(index==2)Require(row.applied.Contains(nameof(WetEffect))&&sequence.Targets.Any(t=>t.Moved),"Jet showcase must wet and actually push its recipient.");
                if(index==3)Require(sequence.Targets.Any(t=>t.Moved),"Surge showcase must exercise actual displacement.");
                if(index==4)Require(row.applied.Contains(nameof(FrozenEffect)),"Rime showcase must apply its actual freeze.");
                if(index==5)Require(row.applied.Contains("Pacified")&&dummy.GetPart<BrainPart>().HasGoal<NoFightGoal>(),"Calm showcase must pacify its real brain-bearing recipient.");
                if(index==6)Require(crop.GetPart<CropPart>().MoistureTicks>0,"Rain showcase must actually water its crop.");
            }
            bool observeConditional=(index==4||index==5)&&mode!="profile";
            var conditional=new Dictionary<Mesh,string>();var observedConditional=new HashSet<string>(StringComparer.Ordinal);
            if(observeConditional)
                foreach(var piece in Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath).Find(SpellIds[index]).Pieces)
                    if(piece.Condition==(index==4?NativeSpellFxCondition.FrozenApplied:NativeSpellFxCondition.PacifiedApplied)
                        &&(SpellFxSettings.Mode==SpellFxMode.Full||piece.ReducedEssential)) conditional.Add(piece.Mesh,piece.Id);
            if(observeConditional)Require(conditional.Count>0,"Conditional art must exist in the actual selected mode before testing its rejection.");
            string resolved=TargetDigest();double start=Now;int sample=0;double next=start;
            // Native1x timing. Screen capture work is outside the steady profile; timestamps are recorded separately.
            while(Now-start<1.2||Fx.HasBlockingFx)
            {
                Require(Now-start<4,"Native fixture effect exceeded bounded lifetime.");int meshes=Native.ActiveMeshCount;
                gesture?.Sample();
                if(meshes>row.peakMeshes)row.peakMeshes=meshes;
                if(observeConditional&&Native.Root!=null)
                {
                    visibleMeshScratch.Clear();Native.Root.GetComponentsInChildren<MeshFilter>(true,visibleMeshScratch);
                    foreach(var mesh in visibleMeshScratch)
                        if(mesh.gameObject.activeInHierarchy&&mesh.GetComponent<MeshRenderer>().enabled&&conditional.TryGetValue(mesh.sharedMesh,out string id))observedConditional.Add(id);
                }
                if(capture&&Now>=next&&sample<12)
                {
                    string file=Path.Combine(Dir,Stem+"-"+mode+"-"+index+"-"+sample.ToString("D3")+".png");
                    yield return new WaitForEndOfFrame();gesture?.Sample();ScreenCapture.CaptureScreenshot(file);loopFrames.Add(file);row.frames.Add(new CaptureFrame{path=file,wallSeconds=Now-start,meshes=Native.ActiveMeshCount});sample++;next=start+sample*.1;
                }
                else yield return null;
            }
            if(capture)
            {
                var bestFrame=row.frames.OrderByDescending(f=>f.meshes).FirstOrDefault();if(bestFrame!=null)screenshots.Add(bestFrame.path);
            }
            row.observedConditionalMeshes=observedConditional.ToArray();
            if(gesture!=null)
            {
                row.gestureClip=gesture.Clip.name;row.sawGesture=gesture.SawExpectedClip;row.gesturePeakDegrees=gesture.PeakDegrees;row.gestureSamples=gesture.Samples;
                row.gestureRestored=gesture.Restored;row.gestureControllerBefore=gesture.ControllerBefore;row.gestureControllerAfter=gesture.ControllerAfter;
                Require(row.sawGesture&&row.gestureSamples>=2&&row.gesturePeakDegrees>3&&row.gestureRestored,"Actual caster bones must move under the evaluated imported override and restore the borrowed controller/speed/update mode and Idle.");
            }
            if(observeConditional)
                Require(mode=="deny-frozen"||mode=="peaceful"?observedConditional.Count==0:observedConditional.Count==conditional.Count,
                    "Actual drawn conditional meshes must match the applied/rejected status; neutral fragments are insufficient proof.");
            row.seconds=Now-start;row.nativeEntry=Native.LastEntry?.SpellId;row.resultStable=resolved==TargetDigest()&&rng.Calls==row.rngCalls&&input.TurnManager.TickCount==tick;
            row.pass=row.nativeEntry==SpellIds[index]&&row.resultStable&&(mode=="empty-rain"?row.peakMeshes==0:row.peakMeshes>0)&&Native.ActiveCount==0&&!Fx.HasBlockingFx;
            Check("command_"+casts.Count+"_"+mode+"_"+index,row.pass,"Real command/copy/native/cleanup; peak="+row.peakMeshes);Require(row.pass,"Native fixture mismatch for"+row.spell+":"+mode);
            exercised.Add(row.spell);
        }
        GestureObservation ObserveGesture(int index)
        {
            var root=PlayerView();
            var animator=root.GetComponentInChildren<Animator>(true);
            var clip=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath).Find(SpellIds[index]).FindCastClip(animator);
            var bones=root.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
            Require(animator!=null&&clip!=null&&bones.Length>0,"Actual native rig and imported gesture binding required.");
            return new GestureObservation(animator,clip,bones);
        }
        GameObject PlayerView()
        {
            GameObject root;
            if(input.CurrentZone.ZoneID=="Overworld.3.6.0")
            {Require(input.ZoneRenderer.Village3D.TryGetOwnerView("$player",out var owner,out root)&&ReferenceEquals(owner,input.PlayerEntity),"Observe the actual town player view.");}
            else Require(input.ZoneRenderer.SpawnRing3D.TryGetEntityView(input.PlayerEntity,out root,out _),"Observe the actual south player view.");
            return root;
        }
        IEnumerator WaitForIdleGesture()
        {
            var animator=PlayerView().GetComponentInChildren<Animator>(true);Require(animator!=null,"Actual player Animator required.");
            double start=Now;
            while(animator.GetCurrentAnimatorStateInfo(0).shortNameHash!=Animator.StringToHash("Idle")||animator.IsInTransition(0))
            {Require(Now-start<2,"Previous cast must naturally finish settling to Idle before the isolated gesture comparison.");yield return null;}
        }
        sealed class GestureObservation
        {
            readonly Animator animator;readonly Transform[] bones;readonly Quaternion[] before;
            readonly RuntimeAnimatorController controller;readonly float speed;readonly AnimatorUpdateMode updateMode;
            readonly List<AnimatorClipInfo> clips=new List<AnimatorClipInfo>(4);int lastFrame=-1;
            public readonly AnimationClip Clip;public bool SawExpectedClip;public float PeakDegrees;public int Samples;
            public GestureObservation(Animator actor,AnimationClip clip,Transform[] rig)
            {animator=actor;Clip=clip;bones=rig;before=rig.Select(b=>b.localRotation).ToArray();controller=actor.runtimeAnimatorController;speed=actor.speed;updateMode=actor.updateMode;}
            public void Sample()
            {
                if(Time.frameCount==lastFrame||!(animator.runtimeAnimatorController is AnimatorOverrideController active)||active["Interact"]!=Clip
                    ||animator.GetCurrentAnimatorStateInfo(0).shortNameHash!=Animator.StringToHash("Interact"))return;
                clips.Clear();animator.GetCurrentAnimatorClipInfo(0,clips);bool evaluated=false;
                for(int i=0;i<clips.Count;i++)if(clips[i].clip==Clip&&clips[i].weight>.5f)evaluated=true;
                if(!evaluated)return;
                SawExpectedClip=true;
                if(Samples==0)for(int i=0;i<bones.Length;i++)before[i]=bones[i].localRotation;
                else for(int i=0;i<bones.Length;i++)PeakDegrees=Math.Max(PeakDegrees,Quaternion.Angle(before[i],bones[i].localRotation));
                Samples++;lastFrame=Time.frameCount;
            }
            public bool Restored=>animator.runtimeAnimatorController==controller&&Mathf.Approximately(animator.speed,speed)&&animator.updateMode==updateMode
                &&animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Animator.StringToHash("Idle");
            public string ControllerBefore=>controller!=null?controller.name:"null";
            public string ControllerAfter=>animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.name:"null";
        }
        BaseSkillPart EnsureSkill(int index)
        {
            var skills=input.PlayerEntity.GetPart<SkillsPart>();var skill=skills.GetSkill(SpellIds[index]);
            if(skill==null){skill=(BaseSkillPart)Activator.CreateInstance(SkillTypes[index]);Require(skills.AddSkill(skill),"Fixture skill learning succeeds.");addedSkills.Add(skill);}return skill;
        }
        void PrepareFixtures()
        {
            Require(fixtureEntities.Count==0,"Previous fixture must be removed before preparing a zone.");fixtureZone=input.CurrentZone;
            fixtureGroundBaseline=new ZoneTileState();fixtureGroundBaseline.LoadFromString(fixtureZone.TileState.ToSaveString());
            var canonical=input.EntityFactory.CreateEntity("CaveHermit").GetPart<RenderPart>();
            dummy=new Entity{ID="native-spell-target-"+RunId,BlueprintName="CaveHermit"};dummy.SetTag("Creature");dummy.AddPart(new RenderPart{DisplayName="spell study recipient",RenderString=canonical.RenderString,ColorString=canonical.ColorString,RenderLayer=canonical.RenderLayer});
            dummy.AddPart(new PhysicsPart{Solid=false});dummy.AddPart(new StatusEffectsPart());dummy.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});dummy.AddPart(new MultiCellPilotPropPart{OwnerId="spell-study-"+RunId,ModelId="PilotHermit",Role="actor"});
            dummy.Statistics["Hitpoints"]=new Stat{Owner=dummy,Name="Hitpoints",BaseValue=1000,Min=0,Max=1000};
            Require(fixtureZone.AddEntity(dummy,lane.x+3,lane.y),"Fixture recipient placement must be real.");fixtureEntities.Add(dummy);
            crop=input.EntityFactory.CreateEntity("CandyCarrotCrop");crop.ID="native-spell-crop-"+RunId;
            Require(fixtureZone.AddEntity(crop,lane.x-1,lane.y+1),"Fixture crop placement must be real.");fixtureEntities.Add(crop);
        }
        void ResetTarget(int spell,string mode)
        {
            Require(fixtureZone==input.CurrentZone&&dummy!=null&&crop!=null,"Current native fixture entities required.");
            dummy.GetPart<StatusEffectsPart>().RemoveAllEffects();dummy.GetStat("Hitpoints").BaseValue=1000;
            var brain=dummy.GetPart<BrainPart>();if(brain!=null)dummy.RemovePart(brain);var veto=dummy.GetPart<FrozenVeto>();if(veto!=null)dummy.RemovePart(veto);
            if(spell==5){brain=new BrainPart();dummy.AddPart(brain);if(mode=="peaceful")brain.PushGoal(new NoFightGoal(50,false));}
            if(mode=="deny-frozen")dummy.AddPart(new FrozenVeto());
            if(fixtureZone.GetEntityCell(dummy)!=null)Require(fixtureZone.RemoveEntity(dummy),"Reset existing fixture target membership.");
            Require(fixtureZone.AddEntity(dummy,lane.x+(spell==1?1:spell==2?2:3),lane.y),"Reset target must occupy its exact actual cell.");
            bool present=fixtureZone.GetEntityCell(crop)!=null;
            if(mode=="empty-rain"){if(present)Require(fixtureZone.RemoveEntity(crop),"Remove only owned crop for empty control.");}
            else if(!present)Require(fixtureZone.AddEntity(crop,lane.x-1,lane.y+1),"Restore owned crop membership.");
            if(crop.GetPart<CropPart>()!=null)crop.GetPart<CropPart>().MoistureTicks=0;
        }
        void ResetFixtureGround()
        {
            Require(fixtureZone==input.CurrentZone&&fixtureGroundBaseline!=null,"Owned showcase lane snapshot required.");
            // Only the bounded command-fixture lane returns to its original writing.
            // Other cells, terrain sources and current spell writes remain authoritative.
            // Steady profile phases deliberately retain ordinary accumulating reactions.
            for(int y=lane.y-3;y<=lane.y+3;y++)for(int x=lane.x-3;x<=lane.x+5;x++)
            {
                var original=fixtureGroundBaseline.Get(x,y);var current=fixtureZone.TileState.Get(x,y);
                if(JsonUtility.ToJson(original)==JsonUtility.ToJson(current))continue;
                fixtureZone.TileState.Clear(x,y);
                if(original==null)continue;
                foreach(var layer in original.Coatings)fixtureZone.TileState.WriteCoating(x,y,layer.Id,layer.Turns);
                foreach(var layer in original.Residues)fixtureZone.TileState.WriteResidue(x,y,layer.Id,layer.Turns);
                fixtureZone.TileState.AddHeat(x,y,original.Heat);fixtureZone.TileState.AddCold(x,y,original.Cold);fixtureZone.TileState.AddCharge(x,y,original.Charge);
                fixtureZone.TileState.WriteCloud(x,y,original.Cloud,original.CloudTurns);
                Require(JsonUtility.ToJson(original)==JsonUtility.ToJson(fixtureZone.TileState.Get(x,y)),"Restore the original fixture-lane ground exactly.");
            }
        }
        string TargetDigest()=>dummy.ID+":"+dummy.GetStatValue("Hitpoints")+":"+fixtureZone.GetEntityPosition(dummy)+":"+string.Join(",",dummy.GetPart<StatusEffectsPart>().GetAllEffects().Select(e=>e.GetType().Name+"="+e.Duration))+":"+(crop.GetPart<CropPart>()?.MoistureTicks??0);
        void RemoveFixtures(bool refresh=true)
        {
            if(fixtureZone!=null)foreach(var entity in fixtureEntities){input?.TurnManager?.RemoveEntity(entity);if(fixtureZone.GetEntityCell(entity)!=null)fixtureZone.RemoveEntity(entity);}
            fixtureEntities.Clear();dummy=null;crop=null;fixtureZone=null;fixtureGroundBaseline=null;if(refresh&&input!=null&&input.ZoneRenderer!=null)input.ZoneRenderer.RenderZone();
        }
        (int x,int y) FindLane(Zone zone)
        {
            for(int distance=0;distance<Zone.Width+Zone.Height;distance++)for(int y=3;y<Zone.Height-3;y++)for(int x=3;x<Zone.Width-6;x++)
            {
                if(Math.Abs(x-40)+Math.Abs(y-12)!=distance)continue;bool clear=true;
                for(int dy=-1;dy<=1&&clear;dy++)for(int dx=-1;dx<=5;dx++)
                {var cell=zone.GetCell(x+dx,y+dy);if(cell.IsSolid()||cell.BlocksMovement(input.PlayerEntity)||cell.Occupants.Any(e=>e!=input.PlayerEntity&&AbilityTargeting.IsElementalTarget(e,input.PlayerEntity))){clear=false;break;}}
                if(clear)for(int cy=y-3;cy<=y+3&&clear;cy++)for(int cx=x-3;cx<=x+3;cx++)if(zone.GetCell(cx,cy).Occupants.Any(e=>e.HasPart<CropPart>())){clear=false;break;}
                if(clear)return(x,y);
            }
            throw new InvalidOperationException("No native empty fixture lane; audit refuses to erase town/pilot scenery.");
        }
        void FixturePlace(Zone zone,(int x,int y) at)
        {
            Require(State()=="Normal"&&input.PlayerEntity.GetStatValue("Hitpoints")>0&&!zone.GetCell(at.x,at.y).BlocksMovement(input.PlayerEntity),"Fixture position requires a live unblocked player.");
            Require(input.CurrentZone.RemoveEntity(input.PlayerEntity)&&zone.AddEntity(input.PlayerEntity,at.x,at.y),"Exact existing player placement.");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(input,new object[]{new ZoneTransitionResult{Success=true,NewZone=zone,NewPlayerX=at.x,NewPlayerY=at.y}});
        }
        IEnumerator WaitForNative()
        {
            double start=Now;
            while(true)
            {
                bool town=input.CurrentZone.ZoneID=="Overworld.3.6.0";
                bool ready=town?input.ZoneRenderer.Village3D!=null&&input.ZoneRenderer.Village3D.IsReady&&input.ZoneRenderer.Village3D.PresentationVisible:input.ZoneRenderer.SpawnRing3D!=null&&input.ZoneRenderer.SpawnRing3D.IsReady&&input.ZoneRenderer.SpawnRing3D.PresentationVisible;
                if(ready&&Native!=null&&input.CurrentZone.GetEntityCell(input.PlayerEntity).IsVisible)break;
                Require(Now-start<15,"Native presenter/FOV readiness timeout.");yield return null;
            }
            yield return new WaitForSecondsRealtime(.4f);
        }
        IEnumerator WaitForFx(){double start=Now;while(Fx!=null&&(Fx.HasBlockingFx||Native.ActiveCount>0||State()=="WaitingForFxResolution")){Require(Now-start<5,"World FX failed to clear.");yield return null;}}
        IEnumerator Tap(params Key[] keys)
        {
            Require(watch.Elapsed.TotalSeconds<540,"Native audit time is bounded.");double start=Now;
            while(input!=null&&Time.time-(float)typeof(InputHandler).GetField("_lastMoveTime",Private).GetValue(input)<input.MoveRepeatDelay){Require(Now-start<3,"Input rate gate stalled.");yield return null;}
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }
        IEnumerator WaitForFiles(){double start=Now;while(loopFrames.Any(p=>!File.Exists(p)||new FileInfo(p).Length<1000)){Require(Now-start<15,"Native screenshot writing incomplete.");yield return null;}}
        string State()=>typeof(InputHandler).GetField("_inputState",Private).GetValue(input).ToString();
        (int x,int y) Position()=>input.CurrentZone.GetEntityPosition(input.PlayerEntity);
        void Check(string name,bool pass,string detail=null){checks.Add(new CheckRow{name=name,pass=pass,detail=detail});if(!pass)failures++;}
        static void Require(bool okay,string message){if(!okay)throw new InvalidOperationException(message);}

        IEnumerator ProfilePair()
        {
            if(recorders==null)BeginCounters();
            foreach(var mode in new[]{SpellFxMode.Full,SpellFxMode.Reduced})
            {
                yield return WaitForFx();SpellFxSettings.Mode=mode;yield return null;yield return new WaitForSecondsRealtime(.5f);
                var phase=new Phase{zoneId=input.CurrentZone.ZoneID,mode=mode.ToString(),firstFrame=frameCount};phases.Add(phase);currentPhase=phases.Count-1;
                double start=Now,next=start;int spell=0;
                while(Now-start<20)
                {
                    if(Now>=next&&!Fx.HasBlockingFx){yield return CommandCase(spell%7,"profile",false);phase.casts++;spell++;next=start+spell*1.6;}
                    else yield return null;
                }
                currentPhase=-1;phase.seconds=Now-start;
                Require(phase.seconds>=20&&phase.seconds<=23&&phase.casts>=7&&phase.activeFrames>0,"Complete paired20s native workload required.");
            }
        }
        void BeginCounters()
        {
            recorders=new ProfilerRecorder[Counters.Length];counterAvailable=new bool[Counters.Length];counterUnits=new string[Counters.Length];
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);var available=handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            for(int i=0;i<Counters.Length;i++)
            {
                var options=available.Where(d=>d.Name==Counters[i]).ToArray();if(options.Length!=1){counterUnits[i]="unavailable";continue;}
                var d=options[0];counterUnits[i]=d.UnitType.ToString();recorders[i]=ProfilerRecorder.StartNew(d.Category,d.Name,2,ProfilerRecorderOptions.Default|(i<2?ProfilerRecorderOptions.SumAllSamplesInFrame:0));counterAvailable[i]=recorders[i].Valid;
            }
            for(int i=0;i<4;i++)Require(counterAvailable[i],"Required actual profiler counter unavailable:"+Counters[i]);
        }
        void LateUpdate()
        {
            if(recorders==null)return;
            if(coldArmed&&coldCount<coldFrames.Length)coldFrames[coldCount++]=new ColdSample{frame=Time.frameCount,seconds=Now,delta=Time.unscaledDeltaTime,meshes=Native?.ActiveMeshCount??0,pool=Native?.AllocatedViewCount??0,blocking=Fx?.HasBlockingFx??false,input=recorders[1].LastValue,main=recorders[2].LastValue,gc=recorders[3].LastValue};
            if(currentPhase<0)return;
            if(frameCount>=Capacity){Check("profile_overflow",false);currentPhase=-1;return;}
            var p=phases[currentPhase];int meshes=Native?.ActiveMeshCount??0;p.frames++;p.maxMeshes=Math.Max(p.maxMeshes,meshes);if(meshes>0){p.activeFrames++;activeFrames++;}
            frames[frameCount++]=new Frame{phase=currentPhase,frame=Time.frameCount,seconds=Now,dt=Time.unscaledDeltaTime,tick=input.TurnManager.TickCount,meshes=meshes,pool=Native.AllocatedViewCount,
                zone=counterAvailable[0]?recorders[0].LastValue:-1,input=counterAvailable[1]?recorders[1].LastValue:-1,main=counterAvailable[2]?recorders[2].LastValue:-1,gc=counterAvailable[3]?recorders[3].LastValue:-1,
                draws=counterAvailable[4]?recorders[4].LastValue:-1,triangles=counterAvailable[5]?recorders[5].LastValue:-1};
        }
        void DisposeCounters(){currentPhase=-1;coldArmed=false;if(recorders!=null)for(int i=0;i<recorders.Length;i++)if(recorders[i].Valid)recorders[i].Dispose();recorders=null;}
        string WriteFrames()
        {
            string path=Path.Combine(Dir,Stem+"-frames.csv");using(var writer=new StreamWriter(path))
            {
                writer.WriteLine("phase,unity_frame,wall_seconds,unscaled_delta,tick,native_meshes,allocated_views,zone_renderer,input,main_thread,gc_bytes,draw_calls,triangles");
                for(int i=0;i<frameCount;i++){var f=frames[i];writer.WriteLine(string.Join(",",f.phase,f.frame,f.seconds.ToString("R",CultureInfo.InvariantCulture),f.dt.ToString("R",CultureInfo.InvariantCulture),f.tick,f.meshes,f.pool,f.zone,f.input,f.main,f.gc,f.draws,f.triangles));}
            }return path;
        }
        public void SetUnexpectedErrors(int count){unexpected=count;if(report!=null)WriteReport();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();DisposePendingSteps();fatal=reason;Check("aborted",false,reason);Finish();}
        void Finish()
        {
            if(Finished)return;DisposeCounters();string raw=WriteFrames();
            report=new Report{runId=RunId,saveRoot=saveRoot,gameId=gameId,markerId=markerId,fatal=fatal,worldSeed=input?.WorldMap?.Seed??0,screenWidth=Screen.width,screenHeight=Screen.height,
                wallSeconds=watch?.Elapsed.TotalSeconds??0,workloadComplete=completed,nativeKeyboardCasts=nativeKeyboardCasts,profileSeconds=phases.Sum(p=>p.seconds),profileFrames=frameCount,activeFrames=activeFrames,
                firstCast=cold,preparation=preparation,phases=phases.ToArray(),casts=casts.ToArray(),spells=exercised.ToArray(),screenshots=screenshots.ToArray(),loopFrames=loopFrames.ToArray(),rawFrames=raw,rawSha256=FileHash(raw),counterNames=Counters,counterUnits=counterUnits,counterAvailable=counterAvailable,
                unityVersion=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,targetFrameRate=Application.targetFrameRate,vSyncCount=QualitySettings.vSyncCount,
                bounds="Actual native1920x1080 Main GameView. One real current-hotbar Flaming Hands/direction cast and ordinary N/south seam input. Other casts are explicitly reset deterministic SkillsPart commands using the real player and disposable unfactioned recipient/crop fixtures; their copied queue records are observed without draining. Showcase/control cases restore only their bounded lane's original ground before each command, so prior fixture writing is not mistaken for the next spell; ordinary accumulated writing remains during profile phases. Profile is four20s native render phases, paired Full/Reduced in town and south; startup/bind/screenshots excluded, fixture setup/observer/counter overhead included. World simulation does not advance during command-fixture playback. Captures use1x settings with measured wall timestamps; capture overhead may lower sampling rate. This establishes observable presentation and cleanup, not subjective comfort, process-cold/build performance or long-session leak confidence."};
            WriteReport();Finished=true;
        }
        void WriteReport(){report.failures=Failures;report.unexpectedErrors=unexpected;report.checks=checks.ToArray();File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));}
        static string FileHash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
        void CleanupInput()
        {
            if(!initialized||cleaned)return;cleaned=true;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            if(originalSettings!=null)InputSystem.settings=originalSettings;if(ownedSettings!=null)Destroy(ownedSettings);Application.runInBackground=originalBackground;
            SpellFxSettings.Mode=originalMode;SpellFxSettings.AnimationSpeed=originalSpeed;SpellFxSettings.FlashIntensity=originalFlash;SpellFxSettings.ShakeIntensity=originalShake;
            Village3DSettings.Enabled=originalEnabled;Village3DSettings.LowDetail=originalLow;
            for(int i=0;i<2;i++)if(prefPresent[i])PlayerPrefs.SetInt(prefKeys[i],prefValue[i]);else PlayerPrefs.DeleteKey(prefKeys[i]);PlayerPrefs.Save();
            if(report!=null){report.displayPreferencesRestored=Village3DSettings.Enabled==originalEnabled&&Village3DSettings.LowDetail==originalLow&&SpellFxSettings.Mode==originalMode&&SpellFxSettings.AnimationSpeed==originalSpeed&&SpellFxSettings.FlashIntensity==originalFlash&&SpellFxSettings.ShakeIntensity==originalShake;
                report.inputSettingsRestored=ReferenceEquals(InputSystem.settings,originalSettings)&&Application.runInBackground==originalBackground&&(keyboard==null||!keyboard.added);if(!report.displayPreferencesRestored||!report.inputSettingsRestored)failures++;}
        }
        void OnDestroy()
        {
            if(!initialized)return;
            try
            {
                DisposePendingSteps();if(!Finished){fatal="Play ended before native workload completion.";Check("premature_shutdown",false);Finish();}
                if(report!=null){report.shutdownObserved=true;report.shutdownRootHeld=SaveGameService.SaveRootOverride==saveRoot;
                    const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static;report.shutdownSavingUnregistered=typeof(SaveGameService).GetField("_captureCurrent",flags).GetValue(null)==null&&typeof(SaveGameService).GetField("_applyLoaded",flags).GetValue(null)==null;
                    if(!report.shutdownRootHeld||!report.shutdownSavingUnregistered)failures++;}
            }
            finally{try{DisposeCounters();RemoveFixtures(false);}finally{try{CleanupInput();}finally{watch?.Stop();if(report!=null)WriteReport();}}}
        }
        sealed class FixedRng:System.Random
        {readonly int value;public int Calls;public FixedRng(int n){value=n;}public override int Next(int max){Calls++;return value%max;}public override int Next(int min,int max){Calls++;return min+value%(max-min);}}
        sealed class FrozenVeto:Part
        {public override string Name=>"NativeAuditFrozenVeto";public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeApplyEffect"||!(e.GetParameter<object>("Effect") is FrozenEffect);}
        [Serializable] public struct ColdSample{public int frame,meshes,pool;public double seconds;public float delta;public long input,main,gc;public bool blocking;}
        [Serializable] public sealed class ColdReport{public int libraryInstancesBefore,libraryInstancesAfter,poolBefore,poolAfter,peakMeshes,damage,cooldown,ticks;public bool sawBlocking,sourceUnchanged;public string nativeEntry,finalState;public long allocatedMemoryBefore,allocatedMemoryAfter,maxInputNanoseconds,maxMainNanoseconds,maxGcBytes;public double startSeconds,wallSeconds;public float maxFrameSeconds;public ColdSample[] samples;}
        [Serializable] public sealed class KeyboardEvidence{public int slot,damage,cooldown,ticks,peakMeshes;public bool sawBlocking,sourceUnchanged;public string nativeEntry,finalState;}
        [Serializable] public sealed class CheckRow{public string name,detail;public bool pass;}
        [Serializable] public sealed class Phase{public string zoneId,mode;public double seconds;public int firstFrame,frames,casts,activeFrames,maxMeshes;}
        [Serializable] public sealed class CaptureFrame{public string path,sha256;public double wallSeconds;public int meshes;}
        [Serializable] public sealed class CastRow
        {public string spell,mode,zoneId,fxMode,nativeEntry,gestureClip,gestureControllerBefore,gestureControllerAfter;public float gesturePeakDegrees;public int sourceX,sourceY,targets,damage,cooldown,rngCalls,peakMeshes,gestureSamples;public string[] path,affected,applied,rejected,copiedTargetIds,targetCells,observedConditionalMeshes;public bool rejectedFrozen,rejectedPacified,resultStable,pass,sawGesture,gestureRestored,groundFixtureReset;public double seconds;public List<CaptureFrame> frames=new List<CaptureFrame>();}
        struct Frame{public int phase,frame,tick,meshes,pool;public double seconds;public float dt;public long zone,input,main,gc,draws,triangles;}
        [Serializable] public sealed class Report
        {public string runId,saveRoot,gameId,markerId,fatal,bounds,rawFrames,rawSha256,unityVersion,gpu,cpu;public int failures,unexpectedErrors,worldSeed,screenWidth,screenHeight,profileFrames,activeFrames,nativeKeyboardCasts,targetFrameRate,vSyncCount;
            public double wallSeconds,profileSeconds;public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;public CheckRow[] checks;public ColdReport firstCast;public NativeSpellFxPreparation preparation;public Phase[] phases;public CastRow[] casts;public string[] spells,screenshots,loopFrames,counterNames,counterUnits;public bool[] counterAvailable;}
    }
}
