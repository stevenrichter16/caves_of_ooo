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
    public static class GrovelandsCompositionPreviewBatch
    {
        [Serializable] public sealed class Row
        {
            public string formation,character,image;
            public int seed,trees,bushes,columns,compost,cache,meshes,missing;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("GROVELANDS_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("GROVELANDS_PREVIEW_OUT required.");
            Directory.CreateDirectory(output);
            var f=new EntityFactory();
            f.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool old=Village3DSettings.Enabled; Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            try
            {
                foreach(var form in new[]{Formation.Grove,Formation.TendrilFen,Formation.FruitingWall,Formation.CompostingField})
                    foreach(int seed in new[]{64,1729,729490642})
                    {
                        var watch=System.Diagnostics.Stopwatch.StartNew();
                        var z=new Zone("Overworld.2.6.0");
                        var terrain=new GrovelandsCompositionBuilder(seed){FormationOverride=form};
                        if(!terrain.BuildZone(z,f,new System.Random(seed)))throw new Exception("Terrain failed.");
                        new GrovelandsFormationBuilder{Composition=terrain,Override=form}.BuildZone(z,f,new System.Random(seed));
                        new ConnectivityBuilder{FloorBlueprint="Grass"}.BuildZone(z,f,new System.Random(seed));
                        watch.Stop();
                        var root=new GameObject("Temporary Grovelands preview");
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
                            File.WriteAllText(Path.Combine(output,form+"-"+seed+"-plan.txt"),terrain.Plan.Signature());
                            rows.Add(new Row{formation=form.ToString(),seed=seed,character=terrain.Plan.Character,image=name,
                                trees=Count(z,"Tree"),bushes=Count(z,"Bush"),columns=Count(z,"MycelialColumn"),
                                compost=Count(z,"CompostRow"),cache=Count(z,"CompostCache"),
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
