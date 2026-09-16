using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Disposable native-manager previews, including actual arriving
    /// player, current liquid owners and resident population. Never saves scenes.</summary>
    public static class SanctumCompositionPreviewBatch
    {
        [Serializable] public sealed class Row
        {
            public string area,formation,zoneId,image;
            public int seed,entities,solids,water,creatures,meshes,missing,unmodeled;
            public string[] unmodeledBlueprints,nativeCensus;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        [UnityEditor.MenuItem("Caves of Ooo/Composition/Render Cathedral and Stillleaf previews")]
        public static void RenderFromMenu()
        {
            string old=Environment.GetEnvironmentVariable("SANCTUM_PREVIEW_OUT");
            try
            {
                Environment.SetEnvironmentVariable("SANCTUM_PREVIEW_OUT",Path.GetFullPath("Docs/Verification/VoxelWorld/Sanctum-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")));
                Run();
            }
            finally{Environment.SetEnvironmentVariable("SANCTUM_PREVIEW_OUT",old);}
        }
        public static void BuildKits(){CathedralVoxelKitBuilder.Run();StillleafVoxelKitBuilder.Run();}
        public static void BuildAndRun(){BuildKits();Run();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("SANCTUM_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("SANCTUM_PREVIEW_OUT is required.");
            Directory.CreateDirectory(output);
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool enabled=Village3DSettings.Enabled;Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            try
            {
                foreach(string id in new[]{"Overworld.5.4.0","Overworld.5.4.1","Overworld.5.4.2",
                    "Overworld.2.4.0","Overworld.2.4.1","Overworld.2.4.2"})
                foreach(int seed in new[]{64,1729,729490642})
                {
                    var manager=new OverworldZoneManager(factory,seed);
                    // The manager's existing parent-first guard owns travel order.
                    var watch=System.Diagnostics.Stopwatch.StartNew();var zone=manager.GetZone(id);watch.Stop();
                    AddArrivingPlayer(zone,factory);
                    bool cathedral=CathedralCompositionPlan.IsSupportedZone(id);
                    string form=cathedral
                        ?id.EndsWith(".0",StringComparison.Ordinal)?"PilgrimageMouth":id.EndsWith(".1",StringComparison.Ordinal)?"MemoryDescent":"GrownNave"
                        :id.EndsWith(".0",StringComparison.Ordinal)?"WindcutThreshold":id.EndsWith(".1",StringComparison.Ordinal)?"ArchiveDescent":"SealedArchive";
                    var root=new GameObject("Disposable area preview");
                    var source=new GameObject("Disposable source camera").AddComponent<Camera>();
                    var sourceTarget=new RenderTexture(1600,650,24);sourceTarget.Create();
                    source.targetTexture=sourceTarget;source.enabled=false;source.orthographic=true;
                    source.orthographicSize=15.5f;source.aspect=1600f/650;
                    source.transform.position=new Vector3(40,12.5f,-10);
                    var presenter=root.AddComponent<SpawnRing3DPresenter>();presenter.FullReveal=true;
                    Texture2D image=null;var previous=RenderTexture.active;
                    try
                    {
                        presenter.Bind(zone,source);presenter.Refresh(null);
                        if(!presenter.IsReady||!presenter.VoxelPresentationActive)throw new InvalidOperationException(presenter.Failure??"Voxel presenter inactive.");
                        presenter.WorldCamera.Render();var target=presenter.WorldCamera.targetTexture;
                        RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                        string name=form+"-"+seed+".png";File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
                        var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
                        int unmodeled=zone.GetAllEntities().Count(e=>e.GetPart<RenderPart>()?.Visible==true&&SpawnRing3DRecipes.Resolve(zone,e,catalog).ModelId==null);
                        rows.Add(new Row{area=cathedral?"Deepest Cathedral":"Stillleaf",formation=form,zoneId=id,image=name,seed=seed,
                            entities=zone.EntityCount,solids=zone.GetAllEntities().Count(e=>e.HasTag("Solid")),
                            water=zone.GetAllEntities().Count(e=>e.BlueprintName=="MirePool"),creatures=zone.GetAllEntities().Count(e=>e.HasTag("Creature")),
                            meshes=presenter.VoxelAppliedMeshCount,missing=presenter.VoxelMissingMeshCount,unmodeled=unmodeled,generationMilliseconds=watch.Elapsed.TotalMilliseconds,
                            unmodeledBlueprints=zone.GetAllEntities().Where(e=>e.GetPart<RenderPart>()?.Visible==true&&SpawnRing3DRecipes.Resolve(zone,e,catalog).ModelId==null).Select(e=>e.BlueprintName).Distinct().OrderBy(n=>n).ToArray(),
                            nativeCensus=zone.GetAllEntities().GroupBy(e=>e.BlueprintName).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()).ToArray()});
                    }
                    finally
                    {
                        RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                        UnityEngine.Object.DestroyImmediate(root);source.targetTexture=null;
                        sourceTarget.Release();UnityEngine.Object.DestroyImmediate(sourceTarget);UnityEngine.Object.DestroyImmediate(source.gameObject);
                    }
                }
                File.WriteAllText(Path.Combine(output,"preview-receipt.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
            }
            finally{Village3DSettings.Enabled=enabled;}
        }
        private static void AddArrivingPlayer(Zone zone,EntityFactory factory)
        {
            var stair=zone.GetAllEntities().FirstOrDefault(e=>e.HasPart<StairsUpPart>())
                ??zone.GetAllEntities().FirstOrDefault(e=>e.HasPart<StairsDownPart>());
            var cell=stair==null?zone.GetCell(40,12):zone.GetEntityCell(stair);
            for(int radius=0;radius<20;radius++)
                for(int dx=-radius;dx<=radius;dx++)for(int dy=-radius;dy<=radius;dy++)
                {
                    var at=zone.GetCell(cell.X+dx,cell.Y+dy);
                    if(at==null||at.BlocksMovement()||!at.Objects.Any(e=>e.HasTag("Terrain"))||at.Objects.Any(e=>e.HasTag("ExcludeZoneArrival")))continue;
                    zone.AddEntity(factory.CreateEntity("Player"),at.X,at.Y);return;
                }
            throw new InvalidOperationException("No native arrival ground exists.");
        }
    }
}
