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
    public static class AreaCompositionPreviewBatch
    {
        [Serializable] public sealed class Row
        {
            public string area,formation,zoneId,image;
            public int seed,entities,solids,water,creatures,meshes,missing,unmodeled;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        public static void BuildKits(){OverwritVoxelKitBuilder.Run();GinmereVoxelKitBuilder.Run();}
        public static void BuildAndRun(){BuildKits();Run();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("AREA_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("AREA_PREVIEW_OUT is required.");
            Directory.CreateDirectory(output);
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool enabled=Village3DSettings.Enabled;Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            try
            {
                foreach(string id in new[]{"Overworld.1.10.0","Overworld.0.9.0","Overworld.0.13.0","Overworld.4.11.0",
                    "Overworld.2.7.0","Overworld.2.7.1","Overworld.2.7.2"})
                foreach(int seed in new[]{64,1729,729490642})
                {
                    var manager=new OverworldZoneManager(factory,seed);
                    // Generate the upper levels in travel order, just as walking
                    // down does, so registered arrivals inform lower stair sites.
                    if(id=="Overworld.2.7.1"||id=="Overworld.2.7.2")manager.GetZone("Overworld.2.7.0");
                    if(id=="Overworld.2.7.2")manager.GetZone("Overworld.2.7.1");
                    var watch=System.Diagnostics.Stopwatch.StartNew();var zone=manager.GetZone(id);watch.Stop();
                    AddArrivingPlayer(zone,factory);
                    bool overwrit=OverwritCompositionPlan.IsWildernessZone(id);
                    string form=overwrit?OverwritCompositionPlan.Create(id,seed).Formation.ToString()
                        :id.EndsWith(".0",StringComparison.Ordinal)?"OvergrownLip":id.EndsWith(".1",StringComparison.Ordinal)?"ExpeditionTerraces":"DrownedBasin";
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
                        rows.Add(new Row{area=overwrit?"Overwrit":"Ginmere",formation=form,zoneId=id,image=name,seed=seed,
                            entities=zone.EntityCount,solids=zone.GetAllEntities().Count(e=>e.HasTag("Solid")),
                            water=zone.GetAllEntities().Count(e=>e.BlueprintName=="MirePool"),creatures=zone.GetAllEntities().Count(e=>e.HasTag("Creature")),
                            meshes=presenter.VoxelAppliedMeshCount,missing=presenter.VoxelMissingMeshCount,unmodeled=unmodeled,generationMilliseconds=watch.Elapsed.TotalMilliseconds});
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
                    if(at==null||at.BlocksMovement()||!at.Objects.Any(e=>e.HasTag("Terrain")))continue;
                    zone.AddEntity(factory.CreateEntity("Player"),at.X,at.Y);return;
                }
            throw new InvalidOperationException("No native arrival ground exists.");
        }
    }
}
