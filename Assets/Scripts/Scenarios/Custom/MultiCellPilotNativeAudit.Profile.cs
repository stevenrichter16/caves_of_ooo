using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    // Mandatory profile for this explicitly launched audit; no automatic runtime/Editor hook.
    public sealed partial class MultiCellPilotNativeAudit
    {
        const int ProfileCapacity=200000;
        const double ProfilePhaseSeconds=20;
        const double ProfileInputIntervalSeconds=0.8;
        static readonly string[] ProfileNames={"COO.ZoneRenderer.LateUpdate","COO.Input.Update","Main Thread","Render Thread",
            "GC Allocated In Frame","Draw Calls Count","Triangles Count","SetPass Calls Count","Batches Count",
            "Render Textures Count","Render Textures Bytes","Texture Memory","GPU Frame Time"};
        readonly List<ProfilePhase> profilePhases=new List<ProfilePhase>(24);
        readonly List<ProfileStep> profileSteps=new List<ProfileStep>(400);
        ProfilerRecorder[] profileRecorders;
        bool[] profileValid;
        string[] profileUnits,profileDataTypes;
        double[][] profileSamples;
        double[] profileTimes,profileWall;
        int[] profilePhaseIds,profileUnityFrames,profileTicks,profilePlayerX,profilePlayerY;
        string[] profileAvailable;
        int profileFrames,profileCurrentPhase=-1,profileBadResolutionFrames;
        bool profileOverflow,profileComplete;
        string profileMode,profileFailure;
        double profileStart,profileEnd;
        ProfileReport profileResult;
        double ProfileNow=>Time.realtimeSinceStartupAsDouble;

        IEnumerator RunPilotProfilePair(string stage)
        {
            if(profileRecorders==null)
            {
                profileMode="full";AllocateRingProfile();
            }
            RequireProfileView(MultiCellPilotRuntime.ZoneID);
            var zone=input.CurrentZone;var lane=ProfileLane(zone);FixturePlace(zone,lane.a);yield return WaitForRing();
            BeginProfilePhase(zone.ZoneID,stage+"_idle");double end=ProfileNow+ProfilePhaseSeconds;
            while(ProfileNow<end)yield return null;
            EndProfilePhase();
            var walk=BeginProfilePhase(zone.ZoneID,stage+"_walk");end=ProfileNow+ProfilePhaseSeconds;
            var preferred=lane.b;var excluded=(-1,-1);int consecutiveUnmoved=0;
            while(ProfileNow<end)
            {
                Require(Alive()&&State()=="Normal"&&ReferenceEquals(input.CurrentZone,zone),"Native profile remains healthy/current; no AI workaround.");
                var from=Position();
                if(!ProfileWalkCell(zone,preferred)||preferred==excluded)
                {
                    var rejected=ProfileAttempt(zone,from,preferred,false,preferred==excluded);
                    rejected.outcome="target_rejected";walk.rejectedTargets++;
                }
                Require(TryProfileWalkTarget(zone,input.PlayerEntity,from,preferred,excluded,out var target),"No native clear cardinal neighbor; profile incomplete.");
                double nextInput=ProfileNow+ProfileInputIntervalSeconds;
                var step=ProfileAttempt(zone,from,target,true,false);walk.inputAttempts++;
                yield return Tap((Key)Enum.Parse(typeof(Key),step.key));
                var after=Position();step.afterX=after.x;step.afterY=after.y;step.afterTick=input.TurnManager.TickCount;
                step.afterHp=input.PlayerEntity.GetStatValue("Hitpoints");step.inputState=State();
                bool healthy=Alive()&&step.inputState=="Normal"&&ReferenceEquals(input.CurrentZone,zone);
                step.pass=after==target&&healthy&&step.afterTick>step.beforeTick;
                step.outcome=step.pass?"moved":after==from&&healthy?"input_without_movement":"invalid_native_result";
                if(step.pass){nativeSteps++;walk.successfulSteps++;preferred=from;excluded=(-1,-1);consecutiveUnmoved=0;}
                else
                {
                    Require(healthy&&after==from&&step.afterTick>=step.beforeTick,"Native movement displaced player or failed health/state.");
                    walk.unmovedInputs++;excluded=target;consecutiveUnmoved++;
                    Require(consecutiveUnmoved<4,"Four consecutive ordinary inputs failed to move; profile incomplete.");
                }
                while(ProfileNow<nextInput&&ProfileNow<end)yield return null;
            }
            EndProfilePhase();Require(walk.successfulSteps>=2,"Native walking must actually advance positions and turns.");
        }
        void CompletePilotProfile()
        {
            double steady=profilePhases.Sum(p=>p.seconds);
            Require(profilePhases.Count==4&&profilePhases.All(p=>p.seconds>=20&&p.frames>1)
                &&steady>=80&&steady<=90&&!profileOverflow&&profileBadResolutionFrames==0,"Four20s native phases must provide80–90s complete capture.");
            Require(profilePhases.Where(p=>p.kind.EndsWith("_idle",StringComparison.Ordinal)).All(p=>p.startTick==p.endTick&&p.startX==p.endX&&p.startY==p.endY),"Idle is a no-input control.");
            profileComplete=true;profileEnd=ProfileNow;DisposeRingProfile();
            Check("native_pilot_80second_profile_complete",true,"Four20s ordinary FOV phases: pristine/restored idle and live keyboard walking; setup/capture excluded.");
        }

        ((int x,int y) a,(int x,int y) b) ProfileLane(Zone zone)
        {
            // Deterministic centre-nearest two-cell native lane; no terrain, faction,
            // brain, health, energy, FOV or actor edits to manufacture a safe path.
            for(int distance=0;distance<Zone.Width+Zone.Height;distance++)
                for(int y=1;y<Zone.Height-1;y++)for(int x=1;x<Zone.Width-1;x++)
                {
                    var a=(x,y);if(Math.Abs(x-40)+Math.Abs(y-12)!=distance||!ProfileWalkCell(zone,a))continue;
                    var east=(x+1,y);if(ProfileWalkCell(zone,east))return(a,east);
                    var south=(x,y+1);if(ProfileWalkCell(zone,south))return(a,south);
                }
            throw new InvalidOperationException("No unblocked native two-cell lane in "+zone.ZoneID+"; representative walking profile is blocked.");
        }
        bool ProfileWalkCell(Zone zone,(int x,int y) at)=>ProfileWalkCell(zone,input.PlayerEntity,at);
        static bool ProfileWalkCell(Zone zone,Entity player,(int x,int y) at)
        {
            if(at.x<=0||at.y<=0||at.x>=Zone.Width-1||at.y>=Zone.Height-1)return false;
            var cell=zone.GetCell(at.x,at.y);
            return cell!=null&&!cell.BlocksMovement(player)
                &&!HasOtherCreature(cell,player);
        }
        static bool HasOtherCreature(Cell cell,Entity player)
        {foreach(var entity in cell.Occupants)if(!ReferenceEquals(entity,player)&&entity.HasTag("Creature"))return true;return false;}
        static bool TryProfileWalkTarget(Zone zone,Entity player,(int x,int y) from,(int x,int y) preferred,(int x,int y) excluded,out (int x,int y) target)
        {
            // Prefer the previous cell to stay local, adapting only to current
            // native occupancy. Reading candidates never changes the world.
            foreach(var at in new[]{preferred,(from.x+1,from.y),(from.x,from.y+1),(from.x-1,from.y),(from.x,from.y-1)})
                if(at!=excluded&&Math.Abs(at.Item1-from.x)+Math.Abs(at.Item2-from.y)==1&&ProfileWalkCell(zone,player,at))
                {target=at;return true;}
            target=default;return false;
        }
        ProfileStep ProfileAttempt(Zone zone,(int x,int y) from,(int x,int y) target,bool issued,bool excluded)
        {
            int dx=target.x-from.x,dy=target.y-from.y;
            Require(Math.Abs(dx)+Math.Abs(dy)==1,"Every proposed profile target must be one cardinal native step.");
            int tick=input.TurnManager.TickCount,hp=input.PlayerEntity.GetStatValue("Hitpoints");
            var step=new ProfileStep{zoneId=zone.ZoneID,phase=profileCurrentPhase,key=(dx==1?Key.D:dx==-1?Key.A:dy==1?Key.S:Key.W).ToString(),
                seconds=ProfileNow-profileStart,beforeX=from.x,beforeY=from.y,afterX=from.x,afterY=from.y,beforeTick=tick,afterTick=tick,beforeHp=hp,afterHp=hp,
                targetX=target.x,targetY=target.y,inputIssued=issued,targetAvailable=ProfileWalkCell(zone,target),targetExcluded=excluded,inputState=State(),outcome="pending_input"};
            profileSteps.Add(step);return step;
        }
        void AllocateRingProfile()
        {
            profileStart=ProfileNow;profileRecorders=new ProfilerRecorder[ProfileNames.Length];profileValid=new bool[ProfileNames.Length];
            profileUnits=new string[ProfileNames.Length];profileDataTypes=new string[ProfileNames.Length];profileSamples=new double[ProfileNames.Length][];
            profileTimes=new double[ProfileCapacity];profileWall=new double[ProfileCapacity];profilePhaseIds=new int[ProfileCapacity];
            profileUnityFrames=new int[ProfileCapacity];profileTicks=new int[ProfileCapacity];profilePlayerX=new int[ProfileCapacity];profilePlayerY=new int[ProfileCapacity];
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions=handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            profileAvailable=descriptions.Select(d=>d.Category.Name+" | "+d.Name+" | "+d.UnitType+" | "+d.DataType).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            for(int i=0;i<ProfileNames.Length;i++)
            {
                profileSamples[i]=new double[ProfileCapacity];var candidates=descriptions.Where(d=>d.Name==ProfileNames[i]).ToArray();
                if(candidates.Length!=1){profileUnits[i]="unavailable";profileDataTypes[i]="unavailable";continue;}
                var d=candidates[0];profileUnits[i]=d.UnitType.ToString();profileDataTypes[i]=d.DataType.ToString();
                var options=ProfilerRecorderOptions.Default;
                if(i<2)options|=ProfilerRecorderOptions.SumAllSamplesInFrame;
                profileRecorders[i]=ProfilerRecorder.StartNew(d.Category,d.Name,2,options);profileValid[i]=profileRecorders[i].Valid;
            }
            foreach(int required in new[]{0,1,2,4})Require(profileValid[required],"Required native profile recorder unavailable/ambiguous: "+ProfileNames[required]);
        }
        ProfilePhase BeginProfilePhase(string zoneId,string kind)
        {
            Require(profileCurrentPhase<0,"No overlapping profile phase.");
            if(kind!="first_binding")RequireProfileView(zoneId);
            var at=Position();
            var p=new ProfilePhase{zoneId=zoneId,kind=kind,startSeconds=ProfileNow-profileStart,startTick=input.TurnManager.TickCount,startX=at.x,startY=at.y,
                startHp=input.PlayerEntity.GetStatValue("Hitpoints"),startGc0=GC.CollectionCount(0),startGc1=GC.CollectionCount(1),startGc2=GC.CollectionCount(2),firstFrame=profileFrames};
            profilePhases.Add(p);profileCurrentPhase=profilePhases.Count-1;return p;
        }
        void EndProfilePhase(bool validate=true)
        {
            var p=profilePhases[profileCurrentPhase];profileCurrentPhase=-1;
            p.seconds=ProfileNow-profileStart-p.startSeconds;p.endGc0=GC.CollectionCount(0);p.endGc1=GC.CollectionCount(1);p.endGc2=GC.CollectionCount(2);p.endTick=input.TurnManager.TickCount;p.endHp=input.PlayerEntity.GetStatValue("Hitpoints");
            var at=Position();p.endX=at.x;p.endY=at.y;
            if(validate)RequireProfileView(p.zoneId);
            var presenter=Presenter();
            if(presenter!=null&&presenter.IsReady&&ReferenceEquals(presenter.CurrentZone,input.CurrentZone))
            {
                p.groundPatches=presenter.GroundPatchCount;p.groundBuildCount=presenter.GroundBuildCount;
                var camera=presenter.WorldCamera;var rt=camera!=null?camera.targetTexture:null;
                p.rtWidth=rt!=null?rt.width:0;p.rtHeight=rt!=null?rt.height:0;p.cameraAspect=camera!=null?camera.aspect:0;p.cameraHalfHeight=camera!=null?camera.orthographicSize:0;
                p.entityCount=input.CurrentZone.GetReadOnlyEntities().Count;p.creatureCount=input.CurrentZone.GetEntitiesWithTag("Creature").Count;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(input.CurrentZone.GetCell(x,y).IsVisible)p.visibleCells++;
            }
        }
        void RequireProfileView(string zoneId)
        {
            var p=Presenter();Require(Alive()&&State()=="Normal"&&Screen.width==1920&&Screen.height==1080&&input.CurrentZone.ZoneID==zoneId
                &&p!=null&&p.IsReady&&p.PresentationVisible&&ReferenceEquals(p.CurrentZone,input.CurrentZone)&&!p.FullReveal
                &&Village3DSettings.Enabled&&Village3DSettings.LowDetail==(profileMode=="low")&&p.WorldCamera!=null
                &&p.WorldCamera.targetTexture!=null&&p.WorldCamera.targetTexture.IsCreated(),"Actual native profile view/state contract failed: "+zoneId);
        }
        void LateUpdate()
        {
            if(profileCurrentPhase<0||profileRecorders==null)return;
            if(profileFrames>=ProfileCapacity){profileOverflow=true;return;}
            int f=profileFrames++;profileTimes[f]=ProfileNow-profileStart;profileWall[f]=Time.unscaledDeltaTime*1000.0;
            profilePhaseIds[f]=profileCurrentPhase;profileUnityFrames[f]=Time.frameCount;profileTicks[f]=input.TurnManager.TickCount;
            var at=Position();profilePlayerX[f]=at.x;profilePlayerY[f]=at.y;
            if(Screen.width!=1920||Screen.height!=1080)profileBadResolutionFrames++;
            for(int m=0;m<profileRecorders.Length;m++)
            {
                profileValid[m]&=profileRecorders[m].Valid;
                profileSamples[m][f]=profileValid[m]&&profileRecorders[m].Count>0
                    ?((profileDataTypes[m]=="Double"||profileDataTypes[m]=="Float")?profileRecorders[m].LastValueAsDouble:profileRecorders[m].LastValue):double.NaN;
            }
            profilePhases[profileCurrentPhase].frames++;
        }
        void DisposeRingProfile()
        {
            if(profileRecorders==null)return;
            for(int i=0;i<profileRecorders.Length;i++)if(profileRecorders[i].Valid)profileRecorders[i].Dispose();
            profileRecorders=null;
        }
        ProfileReport FinishPilotProfile()
        {
            if(string.IsNullOrEmpty(profileMode))return null;
            if(profileResult!=null)return profileResult;
            if(profileCurrentPhase>=0)EndProfilePhase(false);
            DisposeRingProfile();
            var metrics=new List<ProfileMetric>();
            for(int phase=0;phase<profilePhases.Count;phase++)
                for(int m=0;m<=ProfileNames.Length;m++)
                {
                    var values=new List<double>();int missing=0;
                    for(int f=profilePhases[phase].firstFrame;f<profileFrames&&profilePhaseIds[f]==phase;f++)
                    {double value=m==ProfileNames.Length?profileWall[f]:profileSamples[m][f];if(double.IsNaN(value)||double.IsInfinity(value))missing++;else values.Add(value);}
                    values.Sort();bool available=values.Count>0&&(m==ProfileNames.Length||profileValid[m]);
                    metrics.Add(new ProfileMetric{phase=phase,name=m==ProfileNames.Length?"Engine frame duration":ProfileNames[m],unit=m==ProfileNames.Length?"milliseconds":profileUnits[m],
                        available=available,count=values.Count,missing=missing,positive=values.Count(v=>v>0),mean=available?values.Average():-1,
                        max=available?values[values.Count-1]:-1,p95=available?values[(int)Math.Ceiling(values.Count*.95)-1]:-1,p99=available?values[(int)Math.Ceiling(values.Count*.99)-1]:-1});
                }
            string csvPath=Path.Combine(Dir,Stem+"-profile-"+profileMode+"-frames.csv.gz");
            if(profileSamples!=null)
            {
                Require(!File.Exists(csvPath),"Profile raw output must be unique to this run.");
                using(var file=File.Create(csvPath))using(var gzip=new GZipStream(file,System.IO.Compression.CompressionLevel.Optimal))using(var writer=new StreamWriter(gzip))
                {
                    writer.WriteLine("sample,unity_frame,seconds,phase_id,zone,kind,engine_frame_ms,tick,player_x,player_y,"+string.Join(",",ProfileNames));
                    for(int f=0;f<profileFrames;f++)
                    {
                        var p=profilePhases[profilePhaseIds[f]];
                        writer.Write(f+","+profileUnityFrames[f]+","+PF(profileTimes[f])+","+profilePhaseIds[f]+","+p.zoneId+","+p.kind+","+PF(profileWall[f])+","+profileTicks[f]+","+profilePlayerX[f]+","+profilePlayerY[f]);
                        for(int m=0;m<ProfileNames.Length;m++)writer.Write(","+PF(profileSamples[m][f]));writer.WriteLine();
                    }
                }
            }
            bool requiredMeasured=profilePhases.Select((p,i)=>new{p,i}).Where(x=>x.p.kind!="first_binding").All(x=>new[]{0,1,2,4}.All(m=>metrics.Any(r=>r.phase==x.i&&r.name==ProfileNames[m]&&r.available&&r.count>1&&(m==4||r.positive>0))));
            bool valid=profileComplete&&!profileOverflow&&profileBadResolutionFrames==0&&requiredMeasured;
            if(!valid){profileFailure=fatal??"Incomplete workload, required counters or frame coverage.";Check("native_pilot_profile_valid",false,profileFailure);}
            profileResult=new ProfileReport{runId=RunId,saveRoot=saveRoot,mode=profileMode,valid=valid,complete=profileComplete,overflow=profileOverflow,failure=profileFailure,
                frames=profileFrames,badResolutionFrames=profileBadResolutionFrames,worldSeed=input?.WorldMap?.Seed??0,screenWidth=Screen.width,screenHeight=Screen.height,
                targetFrameRate=Application.targetFrameRate,vSyncCount=QualitySettings.vSyncCount,unityVersion=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,
                graphicsApi=SystemInfo.graphicsDeviceType.ToString(),os=SystemInfo.operatingSystem,steadySeconds=profilePhases.Where(p=>p.kind!="first_binding").Sum(p=>p.seconds),
                bindingSeconds=profilePhases.Where(p=>p.kind=="first_binding").Sum(p=>p.seconds),elapsedWithGaps=(profileEnd>0?profileEnd:ProfileNow)-profileStart,phases=profilePhases.ToArray(),steps=profileSteps.ToArray(),
                metrics=metrics.ToArray(),availableRecorderDescriptions=profileAvailable,rawFrames=csvPath,rawSha256=ProfileFileHash(csvPath),
                bounds="Actual native1920x1080 GameView and private seeded N game; four20s segments on the one authored ridge chunk, before and after gameplay/save stimuli. Ordinary native idle and paced keyboard walking retain AI, health, FOV, speed, vsync and current-zone state. Rejected and unmoved attempts are explicit; no obstacles are removed to improve profiling. Setup/bind/capture gaps are excluded. Counters are latest completed samples and may lag the logged Unity frame; units/availability come from descriptors, unavailable is NA. Measurements include Editor/UI/observer, establish no process-cold result, stable frame rate, production-build budget or whole-game performance."};
            File.WriteAllText(Path.Combine(Dir,Stem+"-profile-"+profileMode+".json"),JsonUtility.ToJson(profileResult,true));return profileResult;
        }
        static string ProfileFileHash(string path)
        {if(!File.Exists(path))return null;using(var sha=System.Security.Cryptography.SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
        static string PF(double value)=>double.IsNaN(value)?"NA":value.ToString("R",CultureInfo.InvariantCulture);
        [Serializable] public sealed class ProfilePhase
        {public string zoneId,kind;public int firstFrame,frames,startTick,endTick,startX,startY,endX,endY,startHp,endHp,startGc0,startGc1,startGc2,endGc0,endGc1,endGc2,successfulSteps,inputAttempts,rejectedTargets,unmovedInputs,groundPatches,groundBuildCount,rtWidth,rtHeight,entityCount,creatureCount,visibleCells;public float cameraAspect,cameraHalfHeight;public double startSeconds,seconds,bindCallMilliseconds;}
        [Serializable] public sealed class ProfileStep
        {public string zoneId,key,inputState,outcome;public bool pass,inputIssued,targetAvailable,targetExcluded;public double seconds;public int phase,targetX,targetY,beforeX,beforeY,afterX,afterY,beforeTick,afterTick,beforeHp,afterHp;}
        [Serializable] public sealed class ProfileMetric
        {public string name,unit;public int phase,count,missing,positive;public bool available;public double mean,max,p95,p99;}
        [Serializable] public sealed class ProfileReport
        {public string runId,saveRoot,mode,failure,unityVersion,cpu,gpu,graphicsApi,os,rawFrames,rawSha256,bounds;public bool valid,complete,overflow;public int frames,badResolutionFrames,worldSeed,screenWidth,screenHeight,targetFrameRate,vSyncCount;
            public double steadySeconds,bindingSeconds,elapsedWithGaps;public ProfilePhase[] phases;public ProfileStep[] steps;public ProfileMetric[] metrics;public string[] availableRecorderDescriptions;}
    }
}
