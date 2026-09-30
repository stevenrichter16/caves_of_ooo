using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Editor
{
    /// <summary>Disposable batch-process preview. Never saves a scene or changes
    /// assets/preferences. Renders actual native voxel models at gameplay pitch.</summary>
    public static class SpreadCompositionPreviewBatch
    {
        [Serializable] public sealed class Row
        {
            public string formation,character,image,zoneId,topology,landscape;
            public bool nativePipeline;
            public int seed,trees,bushes,columns,compost,cache,meshes,missing,worksiteOwners,gatheringSources,specialists;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        public static void BuildAndRun(){SpreadVoxelKitBuilder.Run();Run();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("SPREAD_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("SPREAD_PREVIEW_OUT required.");
            Directory.CreateDirectory(output);
            var f=new EntityFactory();
            f.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool old=Village3DSettings.Enabled; Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            bool native=Environment.GetEnvironmentVariable("SPREAD_PREVIEW_NATIVE")=="1";
            try
            {
                foreach(var form in new[]{Formation.Hedgerow,Formation.FieldStrips,Formation.OldRoad,Formation.FlowerMeadow,Formation.Fallow,Formation.RiverMeadow})
                    foreach(int seed in new[]{64,1729,729490642})
                    {
                        var watch=System.Diagnostics.Stopwatch.StartNew();
                        var z=new Zone("Overworld.8.4.0");
                        var terrain=new SpreadCompositionBuilder(seed){FormationOverride=form};
                        if(!terrain.BuildZone(z,f,new System.Random(seed)))throw new Exception("Terrain failed.");
                        new ConnectivityBuilder{FloorBlueprint="Grass"}.BuildZone(z,f,new System.Random(seed));
                        var plan=terrain.Plan;
                        if(native)
                        {
                            var manager=new OverworldZoneManager(f,seed);string nativeId=null;
                            for(int x=0;x<20&&nativeId==null;x++)for(int y=0;y<20&&nativeId==null;y++)
                            {
                                string id=WorldMap.ToZoneID(x,y);
                                if(SpreadCompositionPlan.IsWildernessZone(id)&&manager.WorldMap.GetPOI(x,y)==null
                                    &&FormationSelector.For(BiomeType.Spread,id)==form)nativeId=id;
                            }
                            if(nativeId==null)throw new Exception("No native formation example.");
                            z=manager.GetZone(nativeId);
                            bool placed=manager.Exploration.TryGetPlacement(manager,nativeId,out var entry);
                            plan=SpreadCompositionPlan.Create(nativeId,seed,form,placed?entry.Topology:SpreadExplorationTopology.Legacy);
                        }
                        watch.Stop();
                        rows.Add(Capture(output,form+"-"+seed,z,plan,native,watch.Elapsed.TotalMilliseconds));
                    }
                File.WriteAllText(Path.Combine(output,"receipt.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
            }
            finally { Village3DSettings.Enabled=old; }
        }

        /// <summary>Fresh-generation landscape and worksite views, with fog revealed for visual
        /// inspection only. Uses the ordinary native pipeline, no player or save state.</summary>
        public static void CaptureLandscapes(string output)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Landscape capture requires Edit Mode.");
            if(string.IsNullOrWhiteSpace(output))throw new ArgumentException("Output directory required.",nameof(output));
            Directory.CreateDirectory(output);
            using(var state=new ContentScope())
            {
                var factory=new EntityFactory();
                factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
                LootTableRegistry.InitializeFromJsonSources(Texts("Content/Data/Loot"));
                LiquidRegistry.InitializeFromJsonSources(Texts("Content/Data/LiquidDefinitions"));
                GasRegistry.InitializeFromJsonSources(Texts("Content/Data/GasDefinitions"));
                MaterialReactionResolver.InitializeFromJsonSources(Texts("Content/Data/MaterialReactions"));
                FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
                LoadoutPart.Factory=ContainerPlacementService.Factory=TraderPart.Factory=LootDropSystem.Factory=factory;
                MaterialReactionResolver.Factory=CorpsePart.Factory=HarvestablePart.Factory=factory;
                NarrativeStatePart.Current=new NarrativeStatePart();
                MessageLog.OnMessage=null;MessageLog.TickProvider=null;TurnManager.World=null;ZoneRenderHooks.Reset();
                Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;
                var errors=LootTableRegistry.Validate(bp=>factory.Blueprints.ContainsKey(bp));
                if(errors.Count!=0)throw new InvalidOperationException(string.Join("; ",errors));
                var rows=new List<Row>();
                foreach(int seed in new[]{64,1729})
                {
                    // Private RNGs never advance a borrowed session's stock stream.
                    LoadoutPart.Rng=new System.Random(seed);TraderPart.Rng=new System.Random(seed^0x7135);
                    LootDropSystem.Rng=new System.Random(seed^0x2143);
                    var manager=OverworldZoneManager.CreateDetached(factory,seed,true);
                    foreach(var site in new[]{("north",11,9),("east",12,10),("south",11,11),("cindercaller",15,10),("soursprayer",10,1)})
                    {
                        string id=WorldMap.ToZoneID(site.Item2,site.Item3);
                        var watch=System.Diagnostics.Stopwatch.StartNew();
                        var zone=manager.GetZone(id);watch.Stop();
                        bool placed=manager.Exploration.TryGetPlacement(manager,id,out var entry);
                        var plan=SpreadCompositionPlan.Create(id,seed,FormationSelector.For(BiomeType.Spread,id),
                            placed?entry.Topology:SpreadExplorationTopology.Legacy);
                        rows.Add(Capture(output,site.Item1+"-"+seed,zone,plan,true,watch.Elapsed.TotalMilliseconds));
                        var focus=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.GetProperty("SpreadWorksite.Role")=="still"||e.GetProperty("SpreadWorksite.Role")=="forge"||e.GetProperty("SpreadWorksite.Role")=="trap");
                        if(focus!=null)rows.Add(Capture(output,site.Item1+"-"+seed+"-site",zone,plan,true,watch.Elapsed.TotalMilliseconds,focus));
                    }
                }
                File.WriteAllText(Path.Combine(output,"receipt.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
            }
        }
        private static IEnumerable<string> Texts(string path)=>Resources.LoadAll<TextAsset>(path)
            .OrderBy(a=>a.name,StringComparer.Ordinal).Select(a=>a.text);

        // The same camera, model presenter and readback are shared by both previews.
        // A preview scene prevents temporary objects from dirtying the user's scene.
        private static Row Capture(string output,string name,Zone zone,SpreadCompositionPlan plan,bool native,double milliseconds,Entity focus=null)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            GameObject root=null,sourceObject=null;RenderTexture sourceTarget=null;Texture2D image=null;
            var previous=RenderTexture.active;
            try
            {
                root=new GameObject("Temporary Spread preview");SceneManager.MoveGameObjectToScene(root,scene);
                sourceObject=new GameObject("Temporary preview source");SceneManager.MoveGameObjectToScene(sourceObject,scene);
                var source=sourceObject.AddComponent<Camera>();
                sourceTarget=new RenderTexture(1440,540,24);sourceTarget.Create();
                source.targetTexture=sourceTarget;source.enabled=false;source.orthographic=true;
                source.orthographicSize=15.5f;source.aspect=1440f/540;
                source.transform.position=new Vector3(40,12.5f,-10);
                if(focus!=null){var at=zone.GetEntityPosition(focus);source.orthographicSize=5.5f;float halfWidth=source.orthographicSize*source.aspect;source.transform.position=new Vector3(Mathf.Clamp(at.x+.5f,halfWidth,Zone.Width-halfWidth),Mathf.Clamp(Zone.Height-at.y-.5f,5.5f,Zone.Height-5.5f),-10);}
                var presenter=root.AddComponent<SpawnRing3DPresenter>();presenter.FullReveal=true;
                presenter.Bind(zone,source);
                if(!presenter.IsReady||!presenter.VoxelPresentationActive)throw new Exception(presenter.Failure??"Voxel presenter inactive");
                presenter.Refresh(null);
                presenter.WorldCamera.scene=scene;
                presenter.WorldCamera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
                presenter.WorldCamera.Render();
                var target=presenter.WorldCamera.targetTexture;RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
                File.WriteAllText(Path.Combine(output,name+"-plan.txt"),plan.Signature());
                return new Row{formation=plan.Formation.ToString(),seed=plan.Seed,character=plan.Condition,
                    topology=plan.Topology.ToString(),landscape=plan.Landscape,image=name+".png",zoneId=zone.ZoneID,nativePipeline=native,
                    trees=Count(zone,"Tree"),bushes=Count(zone,"Bush"),columns=Count(zone,"Hedge"),
                    compost=Count(zone,"CropRow"),cache=Count(zone,"FlowerField")+Count(zone,"CharmFlowers"),
                    worksiteOwners=zone.GetReadOnlyEntities().Count(e=>e.Properties.ContainsKey("SpreadWorksite.Role")),
                    gatheringSources=Count(zone,"StoneburrPatch")+Count(zone,"FrostLichenPatch"),
                    specialists=Count(zone,"MarlbackCindercaller")+Count(zone,"MarlbackSoursprayer"),
                    meshes=presenter.VoxelAppliedMeshCount,missing=presenter.VoxelMissingMeshCount,generationMilliseconds=milliseconds};
            }
            finally
            {
                RenderTexture.active=previous;
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);
                if(sourceObject!=null)UnityEngine.Object.DestroyImmediate(sourceObject);
                if(sourceTarget!=null){sourceTarget.Release();UnityEngine.Object.DestroyImmediate(sourceTarget);}
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Exact previous registry objects and factory/RNG references survive capture.
        // No test assembly, bootstrap, scene swap, preference or save-root dependency.
        private sealed class ContentScope:IDisposable
        {
            readonly List<Action> restore=new List<Action>();
            public ContentScope()
            {
                foreach(var type in new[]{typeof(LootTableRegistry),typeof(LiquidRegistry),typeof(GasRegistry),
                    typeof(MaterialReactionResolver),typeof(FactionManager),typeof(PlayerReputation),typeof(LoadoutPart),
                    typeof(ContainerPlacementService),typeof(TraderPart),typeof(LootDropSystem),typeof(CorpsePart),
                    typeof(HarvestablePart),typeof(NarrativeStatePart),typeof(MessageLog),typeof(Village3DSettings),typeof(ZoneRenderHooks)})
                    foreach(var field in type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
                    {
                        if(field.IsLiteral)continue;var value=field.GetValue(null);
                        if(!field.IsInitOnly)restore.Add(()=>field.SetValue(null,value));
                        if(value is IDictionary dict)
                        {var copy=dict.Keys.Cast<object>().Select(key=>new DictionaryEntry(key,dict[key])).ToArray();restore.Add(()=>{dict.Clear();foreach(var entry in copy)dict.Add(entry.Key,entry.Value);});}
                        else if(value is IList list&&!list.IsReadOnly&&!list.IsFixedSize)
                        {var copy=list.Cast<object>().ToArray();restore.Add(()=>{list.Clear();foreach(var item in copy)list.Add(item);});}
                        else if(value is HashSet<string> set)
                        {var copy=set.ToArray();restore.Add(()=>{set.Clear();foreach(var item in copy)set.Add(item);});}
                        else if(value is Queue<string> queue)
                        {var copy=queue.ToArray();restore.Add(()=>{queue.Clear();foreach(var item in copy)queue.Enqueue(item);});}
                    }
                var world=TurnManager.World;restore.Add(()=>TurnManager.World=world);
            }
            public void Dispose(){for(int i=restore.Count-1;i>=0;i--)restore[i]();}
        }
        private static int Count(Zone z,string bp)=>z.GetAllEntities().Count(e=>e.BlueprintName==bp);
    }
}
