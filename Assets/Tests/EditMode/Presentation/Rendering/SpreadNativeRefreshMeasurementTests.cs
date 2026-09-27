#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Debug=UnityEngine.Debug;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeRefreshMeasurementTests
 {
  [TestCase("Overworld.11.10.0")][TestCase("Overworld.12.10.0")]
  public void MeasureStableNativeRefreshAndLookupCostsWithoutSimulationOrThresholdClaims(string address)
  {
   using(var f=new SpawnRing3DIntegrationFixture(address))
   {
    var p=(SpawnRing3DPresenter)f.Presenter;Assert.True(p.IsReady,p.Failure);f.Refresh();
    var owners=f.Zone.GetReadOnlyEntities().ToArray();var catalog=f.Library.Definition;
    var recipes=owners.Select(e=>SpawnRing3DRecipes.Resolve(f.Zone,e,catalog)).Where(r=>r.ModelId!=null).ToArray();
    Assert.Greater(recipes.Length,100);
    var ids=recipes.Select(r=>r.ModelId).Distinct().ToArray();var frozen=new Dictionary<string,SpawnRing3DCatalog.Model>();
    foreach(string id in ids){var model=catalog.FindModel(id);Assert.NotNull(model,id);frozen.Add(id,model);}
    var native=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(native);native.Validate();
    var dirty=new HashSet<int>();string before=Graph(f);int builds=p.GroundBuildCount;
    int sink=0;var rows=new List<Measurement>();
    rows.Add(Measure("resolve-every-current-owner",owners.Length,()=>{foreach(var e in owners)if(SpawnRing3DRecipes.Resolve(f.Zone,e,catalog).ModelId!=null)sink++;}));
    rows.Add(Measure("catalog-find-every-recipe",recipes.Length,()=>{foreach(var r in recipes)if(catalog.FindModel(r.ModelId)!=null)sink++;}));
    // Diagnostic same-identity dictionary only, not a runtime caching patch.
    rows.Add(Measure("frozen-identical-model-map-control",recipes.Length,()=>{foreach(var r in recipes)if(frozen[r.ModelId]!=null)sink++;}));
    rows.Add(Measure("native-static-current-owner-selection",recipes.Length,()=>{foreach(var r in recipes)if(native.ForOwner(f.Zone,r)!=null)sink++;}));
    rows.Add(Measure("light-cache-compute",1,()=>f.Light.Compute(f.Zone)));
    rows.Add(Measure("presenter-empty-dirty-refresh",1,()=>p.Refresh(f.Light,dirty)));
    rows.Add(Measure("presenter-complete-fingerprint-refresh",1,()=>p.Refresh(f.Light,null)));
    rows.Add(Measure("same-zone-bind-fast-path",1,()=>p.Bind(f.Zone,f.Source)));
    Assert.Greater(sink,0);Assert.AreEqual(builds,p.GroundBuildCount,"stable refresh must not rebuild geometry");Assert.AreEqual(before,Graph(f));
    foreach(string id in ids)Assert.AreSame(frozen[id],catalog.FindModel(id));
    var rt=p.WorldCamera.targetTexture;var report=new Report{zone=address,owners=owners.Length,recipes=recipes.Length,uniqueModels=ids.Length,groundBuilds=builds,
      targetWidth=rt.width,targetHeight=rt.height,msaa=rt.antiAliasing,filter=rt.filterMode.ToString(),playing=Application.isPlaying,
      focused=Application.isFocused,runInBackground=Application.runInBackground,vSync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,
      measurements=rows.ToArray(),canVerify="Warmed stable native fixture CPU lookup/refresh wall times and per-thread managed allocation; exact unchanged native IDs/positions/HP/equipment/tile state and patch build count. No simulated actions or persistent asset changes.",
      cannotVerify="Not live game-frame, GPU, input/turn, editor throttling or actual keyboard movement performance. Frozen lookup map is only a diagnostic reference, not a proposed invalidation policy. No absolute performance acceptance threshold."};
    string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/SpreadBiome/Performance/NativeRefresh"));Directory.CreateDirectory(folder);
    string path=Path.Combine(folder,address+"-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("[SpreadRefreshMeasurement] "+path);
   }
  }
  private static Measurement Measure(string name,int operations,Action action)
  {
   action();const int count=5;var elapsed=new double[count];var allocations=new long[count];
   for(int i=0;i<count;i++){long allocated=GC.GetAllocatedBytesForCurrentThread();long began=Stopwatch.GetTimestamp();action();elapsed[i]=(Stopwatch.GetTimestamp()-began)*1000d/Stopwatch.Frequency;allocations[i]=GC.GetAllocatedBytesForCurrentThread()-allocated;}
   return new Measurement{name=name,operationsPerSample=operations,milliseconds=elapsed,allocatedBytes=allocations};
  }
  private static string Graph(SpawnRing3DIntegrationFixture f)=>f.Zone.TileState.ToSaveString()+"|"+f.Zone.EntityVersion+"|"+string.Join("\n",f.Zone.GetReadOnlyEntities().Select(e=>e.ID+"|"+f.Zone.GetEntityPosition(e)+"|"+e.GetStatValue("Hitpoints")+"|"+string.Join(",",e.GetPart<InventoryPart>()?.GetAllEquipped().Select(i=>i.ID).OrderBy(x=>x)??Enumerable.Empty<string>())).OrderBy(x=>x));
  [Serializable] private sealed class Measurement{public string name;public int operationsPerSample;public double[] milliseconds;public long[] allocatedBytes;}
  [Serializable] private sealed class Report{public string zone,filter,canVerify,cannotVerify;public int owners,recipes,uniqueModels,groundBuilds,targetWidth,targetHeight,msaa,vSync,targetFrameRate;public bool playing,focused,runInBackground;public Measurement[] measurements;}
 }
}
#endif
