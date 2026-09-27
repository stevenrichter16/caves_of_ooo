using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CavesOfOoo.Diagnostics;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    // Diagnostic-only companion. It observes the already existing finite native
    // movement interval; it issues no input and changes no render/profiler setting.
    public sealed partial class ReferenceGladeNativePlayer
    {
        private const int MarkerCapacity=60000;
        private static readonly string[] MarkerNames={"COO.ZoneRenderer.LateUpdate","COO.Input.Update",
            "COO.ZoneRenderer.RenderZone","COO.ZoneRenderer.RenderCell",
            "COO.ZoneRenderer.RenderCells","COO.NativePresenter.Refresh","COO.GroundContact.Refresh","COO.GroundContact.Rasterize","COO.GroundContact.Upload","COO.ZoneRenderer.ComputeFOV","COO.ZoneRenderer.ComputeLightMap",
            "COO.ZoneRenderer.UpdateAmbientAnimations","COO.ZoneRenderer.RenderSidebar","COO.ZoneRenderer.RenderHotbar",
            "COO.EnvSprites.PostRender","COO.UI.Sidebar.Render","COO.UI.Hotbar.Render",
            "COO.Turns.ProcessUntilPlayerTurn","COO.Turns.Tick","COO.Turns.AI.TakeTurn","COO.Turns.EndTurn",
            "Main Thread","Render Thread","GC Allocated In Frame","GC.Collect","Draw Calls Count","Triangles Count","GPU Frame Time"};
        private ProfilerRecorder[] markerRecorders;
        private MarkerDescription[] markerDescriptions;
        private double[][] markerValues;
        private bool[][] markerHasValue;
        private double[] markerTimes,markerWall,markerRendererMs;
        private int[] markerUnityFrames,markerTicks,markerX,markerY,markerSnapshotFrames,markerDirty,markerBuilds,markerFocus;
        private int markerFrames;
        private bool markerOverflow;
        private string markerBeganUtc;
        private double markerBeganRealtime;
        private MarkerSettings markerSettingsBefore;
        private string[] markerAvailable;

        private void BeginNativeMarkerProfile()
        {
            if(markerRecorders!=null)throw new InvalidOperationException("Overlapping marker observation.");
            markerSettingsBefore=ObserveMarkerSettings();markerBeganUtc=DateTime.UtcNow.ToString("O");
            markerRecorders=new ProfilerRecorder[MarkerNames.Length];markerDescriptions=new MarkerDescription[MarkerNames.Length];
            markerValues=new double[MarkerNames.Length][];markerHasValue=new bool[MarkerNames.Length][];
            markerTimes=new double[MarkerCapacity];markerWall=new double[MarkerCapacity];markerRendererMs=new double[MarkerCapacity];
            markerUnityFrames=new int[MarkerCapacity];markerTicks=new int[MarkerCapacity];markerX=new int[MarkerCapacity];markerY=new int[MarkerCapacity];
            markerSnapshotFrames=new int[MarkerCapacity];markerDirty=new int[MarkerCapacity];markerBuilds=new int[MarkerCapacity];markerFocus=new int[MarkerCapacity];
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var available=handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            markerAvailable=available.Select(d=>d.Category.Name+" | "+d.Name+" | "+d.UnitType+" | "+d.DataType).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            try {
                for(int i=0;i<MarkerNames.Length;i++)
                {
                    markerValues[i]=new double[MarkerCapacity];markerHasValue[i]=new bool[MarkerCapacity];
                    var row=new MarkerDescription{name=MarkerNames[i]};markerDescriptions[i]=row;
                    var candidates=available.Where(d=>d.Name==row.name).ToArray();row.matchingHandles=candidates.Length;
                    if(candidates.Length!=1){row.failure="Unavailable or ambiguous exact native marker";continue;}
                    var d=candidates[0];row.category=d.Category.Name;row.units=d.UnitType.ToString();row.dataType=d.DataType.ToString();
                    var options=ProfilerRecorderOptions.Default;if(row.name.StartsWith("COO.",StringComparison.Ordinal))options|=ProfilerRecorderOptions.SumAllSamplesInFrame;
                    markerRecorders[i]=ProfilerRecorder.StartNew(d.Category,d.Name,2,options);row.valid=markerRecorders[i].Valid;
                    if(!row.valid)row.failure="Native recorder invalid";
                }
                markerBeganRealtime=Time.realtimeSinceStartupAsDouble;
            } catch { DisposeNativeMarkerRecorders();throw; }
        }

        private void LateUpdate()
        {
            if(!_profiling||markerRecorders==null)return;
            if(markerFrames>=MarkerCapacity){markerOverflow=true;return;}
            int f=markerFrames++;markerTimes[f]=Time.realtimeSinceStartupAsDouble;markerWall[f]=Time.unscaledDeltaTime*1000d;markerUnityFrames[f]=Time.frameCount;
            markerTicks[f]=_input.TurnManager.TickCount;var cell=Cell();markerX[f]=cell.X;markerY[f]=cell.Y;
            markerFocus[f]=Application.isFocused?1:0;markerBuilds[f]=_input.ZoneRenderer.SpawnRing3D?.GroundBuildCount??-1;
            var snapshot=PerformanceDiagnostics.LastCompletedFrameSnapshot;markerSnapshotFrames[f]=snapshot.UnityFrame;
            markerRendererMs[f]=snapshot.ZoneRendererLateUpdateMs;markerDirty[f]=snapshot.RendererDirty?1:0;
            for(int i=0;i<markerRecorders.Length;i++)
            {
                var row=markerDescriptions[i];if(!row.valid)continue;
                if(!markerRecorders[i].Valid){row.valid=false;row.failure="Recorder became invalid";continue;}
                if(markerRecorders[i].Count==0)continue;
                markerHasValue[i][f]=true;markerValues[i][f]=row.dataType=="Double"||row.dataType=="Float"?markerRecorders[i].LastValueAsDouble:markerRecorders[i].LastValue;
            }
        }

        private void EndNativeMarkerProfile()
        {
            if(markerRecorders==null)return;
            double end=Time.realtimeSinceStartupAsDouble;
            try {
                Directory.CreateDirectory(DirectoryPath);var invariant=CultureInfo.InvariantCulture;
                string raw=Path.Combine(DirectoryPath,"profile-markers.csv.gz");
                using(var file=File.Create(raw))using(var gzip=new GZipStream(file,System.IO.Compression.CompressionLevel.Optimal))using(var writer=new StreamWriter(gzip))
                {
                    writer.Write("unityFrame,realtimeSeconds,wallMs,tick,x,y,focused,groundBuildCount,rendererSnapshotFrame,rendererSnapshotMs,rendererDirty");
                    foreach(string name in MarkerNames)writer.Write(","+name);writer.WriteLine();
                    for(int f=0;f<markerFrames;f++)
                    {
                        writer.Write(string.Join(",",markerUnityFrames[f].ToString(),markerTimes[f].ToString("R",invariant),markerWall[f].ToString("R",invariant),markerTicks[f].ToString(),markerX[f].ToString(),markerY[f].ToString(),markerFocus[f].ToString(),markerBuilds[f].ToString(),markerSnapshotFrames[f].ToString(),markerRendererMs[f].ToString("R",invariant),markerDirty[f].ToString()));
                        for(int i=0;i<MarkerNames.Length;i++){writer.Write(',');if(markerHasValue[i][f])writer.Write(markerValues[i][f].ToString("R",invariant));}writer.WriteLine();
                    }
                }
                var report=new MarkerReport{runId=RunId,beganUtc=markerBeganUtc,endedUtc=DateTime.UtcNow.ToString("O"),beganRealtime=markerBeganRealtime,endedRealtime=end,
                    sampledFrames=markerFrames,overflow=markerOverflow,normalIntervalComplete=_profileSeconds>=60,rawFile=Path.GetFileName(raw),
                    before=markerSettingsBefore,after=ObserveMarkerSettings(),markers=markerDescriptions,available=markerAvailable,
                    scope="Existing native60s movement interval, exact available native recorder names/units; no gameplay, rendering, focus, time, profiler or editor-setting changes. Raw empty value means no available completed sample, not zero cost. Allocation for recorder buffers/setup precedes the timed loop; writing/compression occurs after it.",
                    sampling="LateUpdate observer frame is recorded. ProfilerRecorder.LastValue is the latest completed sample and may correspond to the preceding frame; no false same-frame attribution. ZoneRenderer snapshot has its own UnityFrame for exact correlation. Other LateUpdate order is not forced. Markers overlap/nest and must not be summed as exclusive total time; editor wall time includes waits/GPU/editor scheduling. This is not standalone build performance."};
                File.WriteAllText(Path.Combine(DirectoryPath,"profile-markers.json"),JsonUtility.ToJson(report,true));
            } finally { DisposeNativeMarkerRecorders(); }
        }
        private void DisposeNativeMarkerRecorders()
        { if(markerRecorders!=null){for(int i=0;i<markerRecorders.Length;i++)markerRecorders[i].Dispose();markerRecorders=null;} }
        private MarkerSettings ObserveMarkerSettings()
        {
            var view=_input?.ZoneRenderer?.SpawnRing3D;var rt=view?.WorldCamera?.targetTexture;
            return new MarkerSettings{screenWidth=Screen.width,screenHeight=Screen.height,targetWidth=rt?.width??0,targetHeight=rt?.height??0,
                msaa=rt?.antiAliasing??0,filter=rt==null?"none":rt.filterMode.ToString(),vSync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,
                runInBackground=Application.runInBackground,focused=Application.isFocused,timeScale=Time.timeScale,profilerEnabled=UnityEngine.Profiling.Profiler.enabled,
                zone=_input?.CurrentZone?.ZoneID,groundBuildCount=view?.GroundBuildCount??-1,gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2)};
        }
        [Serializable] private sealed class MarkerDescription{public string name,category,units,dataType,failure;public int matchingHandles;public bool valid;}
        [Serializable] private sealed class MarkerSettings{public int screenWidth,screenHeight,targetWidth,targetHeight,msaa,vSync,targetFrameRate,groundBuildCount,gc0,gc1,gc2;public string filter,zone;public bool runInBackground,focused,profilerEnabled;public float timeScale;}
        [Serializable] private sealed class MarkerReport{public string runId,beganUtc,endedUtc,rawFile,scope,sampling;public double beganRealtime,endedRealtime;public int sampledFrames;public bool overflow,normalIntervalComplete;public MarkerSettings before,after;public MarkerDescription[] markers;public string[] available;}
    }
}
