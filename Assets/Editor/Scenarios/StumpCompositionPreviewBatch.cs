using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Disposable batch-process preview. Never saves a scene or changes
    /// assets/preferences. Renders actual native voxel models at gameplay pitch.</summary>
    public static class StumpCompositionPreviewBatch
    {
        [Serializable] public sealed class Row
        {
            public string formation,band,image,zoneId;
            public bool nativePipeline;
            public int seed,walls,grain,domes,pools,veins,tanks,trees,singers,sentinels,cascadeFathers,meshes,missing;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        public static void BuildAndRun(){StumpVoxelKitBuilder.Run();Run();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("STUMP_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("STUMP_PREVIEW_OUT required.");
            Directory.CreateDirectory(output);
            var f=new EntityFactory();
            f.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool old=Village3DSettings.Enabled; Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            bool native=Environment.GetEnvironmentVariable("STUMP_PREVIEW_NATIVE")=="1";
            try
            {
                foreach(var form in new[]{Formation.CascadeGorge,Formation.Grainfield,Formation.ButtressRidge,Formation.SummitScrub,Formation.RimForest})
                    foreach(int seed in new[]{64,1729,729490642})
                    {
                        var watch=System.Diagnostics.Stopwatch.StartNew();
                        Zone z;
                        StumpCompositionPlan plan;
                        string character;
                        if(native)
                        {
                            var manager=new OverworldZoneManager(f,seed);string nativeId=null;
                            for(int x=0;x<20&&nativeId==null;x++)for(int y=0;y<20&&nativeId==null;y++)
                            {
                                string id=WorldMap.ToZoneID(x,y);
                                if(StumpCompositionPlan.IsWildernessZone(id)&&manager.WorldMap.GetPOI(x,y)==null
                                    &&FormationSelector.ForStump(StumpBands.BandAt(x,y),id)==form)nativeId=id;
                            }
                            if(nativeId==null)throw new Exception("No native formation example.");
                            z=manager.GetZone(nativeId);
                            watch.Stop();
                            plan=StumpCompositionPlan.Create(nativeId,seed);
                        }
                        else throw new InvalidOperationException("Stump previews require STUMP_PREVIEW_NATIVE=1.");
                        character=plan.Band.ToString();
                        var root=new GameObject("Temporary Stump preview");
                        var source=new GameObject("Temporary preview source").AddComponent<Camera>();
                        var sourceTarget=new RenderTexture(1440,540,24);sourceTarget.Create();
                        source.targetTexture=sourceTarget;source.enabled=false;source.orthographic=true;
                        source.orthographicSize=15.5f;source.aspect=1440f/540;
                        source.transform.position=new Vector3(40,12.5f,-10);
                        var presenter=root.AddComponent<SpawnRing3DPresenter>();presenter.FullReveal=true;
                        Texture2D image=null;
                        var previous=RenderTexture.active;
                        try
                        {
                            presenter.Bind(z,source);
                            if(!presenter.IsReady||!presenter.VoxelPresentationActive)throw new Exception(presenter.Failure??"Voxel presenter inactive");
                            presenter.Refresh(null);
                            presenter.WorldCamera.Render();
                            var target=presenter.WorldCamera.targetTexture;
                            RenderTexture.active=target;
                            image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                            string name=form+"-"+seed+".png";
                            File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
                            File.WriteAllText(Path.Combine(output,form+"-"+seed+"-plan.txt"),plan.Signature());
                            rows.Add(new Row{formation=form.ToString(),seed=seed,band=character,image=name,zoneId=z.ZoneID,nativePipeline=native,
                                walls=Count(z,"TepuiWall"),grain=Count(z,"GrainRidge"),domes=Count(z,"StoneDome"),
                                pools=Count(z,"SprayPool"),veins=Count(z,"TepuiboneVein"),tanks=Count(z,"TankBrocchinia"),trees=Count(z,"Tree"),
                                singers=Count(z,"SummitSinger"),sentinels=Count(z,"BrocchiniaSentinel"),cascadeFathers=Count(z,"CascadeFather"),
                                meshes=presenter.VoxelAppliedMeshCount,missing=presenter.VoxelMissingMeshCount,
                                generationMilliseconds=watch.Elapsed.TotalMilliseconds});
                        }
                        finally
                        {
                            RenderTexture.active=previous;
                            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                            UnityEngine.Object.DestroyImmediate(root);
                            source.targetTexture=null;sourceTarget.Release();UnityEngine.Object.DestroyImmediate(sourceTarget);
                            UnityEngine.Object.DestroyImmediate(source.gameObject);
                        }
                    }
                File.WriteAllText(Path.Combine(output,"receipt.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
            }
            finally { Village3DSettings.Enabled=old; }
        }
        private static int Count(Zone z,string bp)=>z.GetAllEntities().Count(e=>e.BlueprintName==bp);
    }
}
