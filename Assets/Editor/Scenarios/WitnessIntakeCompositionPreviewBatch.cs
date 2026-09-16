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
    /// <summary>Native census and disposable previews for the excavation camp
    /// and intake station. Does not modify or save the user's active scene.</summary>
    public static class WitnessIntakeCompositionPreviewBatch
    {
        [Serializable] public sealed class CensusOwner
        {
            public string blueprint,glyph;
            public int count;
            public bool visible,creature,solid,takeable,destructible,indestructible,light,liquid;
        }
        [Serializable] public sealed class CensusRow
        {
            public string zoneId,biome,profile;
            public int seed,entities;
            public bool mapRiver;
            public CensusOwner[] owners;
        }
        [Serializable] public sealed class CensusReport { public CensusRow[] rows; }

        public static void Census()
        {
            string output=Environment.GetEnvironmentVariable("WITNESS_INTAKE_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("WITNESS_INTAKE_PREVIEW_OUT is required.");
            Directory.CreateDirectory(output);
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            var rows=new List<CensusRow>();
            foreach(string id in new[]{"Overworld.17.5.0","Overworld.12.12.0"})
            foreach(int seed in new[]{64,1729,729490642})
            {
                var manager=OverworldZoneManager.CreateDetached(factory,seed);var zone=manager.GetZone(id);
                var at=WorldMap.FromZoneID(id);
                rows.Add(new CensusRow{zoneId=id,seed=seed,entities=zone.EntityCount,
                    biome=manager.WorldMap.GetBiome(at.x,at.y).ToString(),profile=manager.WorldMap.GetPOI(at.x,at.y)?.Profile,
                    mapRiver=WorldMapAuthoring.IsRiver(at.x,at.y),
                    owners=zone.GetAllEntities().GroupBy(e=>new{e.BlueprintName,Glyph=e.GetPart<RenderPart>()?.RenderString})
                        .OrderBy(g=>g.Key.BlueprintName).ThenBy(g=>g.Key.Glyph).Select(g=>
                        {
                            var e=g.First();var d=e.GetPart<DestructiblePart>();var p=e.GetPart<PhysicsPart>();
                            return new CensusOwner{blueprint=e.BlueprintName,glyph=e.GetPart<RenderPart>()?.RenderString,
                                count=g.Count(),visible=e.GetPart<RenderPart>()?.Visible==true,creature=e.HasTag("Creature"),
                                solid=p?.Solid==true,takeable=p?.Takeable==true,destructible=d!=null,
                                indestructible=d?.Indestructible==true,light=e.HasPart<LightSourcePart>(),liquid=e.HasPart<LiquidPoolPart>()};
                        }).ToArray()});
            }
            File.WriteAllText(Path.Combine(output,"native-census.json"),JsonUtility.ToJson(new CensusReport{rows=rows.ToArray()},true));
        }
        [Serializable] public sealed class Row
        {
            public string area,formation,zoneId,image;
            public int seed,entities,solids,water,creatures,meshes,missing,unmodeled;
            public string[] unmodeledBlueprints,nativeCensus;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report { public Row[] rows; }
        [UnityEditor.MenuItem("Caves of Ooo/Composition/Render Drowned Ledger and Marrowstye previews")]
        public static void RenderFromMenu()
        {
            string old=Environment.GetEnvironmentVariable("WITNESS_INTAKE_PREVIEW_OUT");
            try
            {
                Environment.SetEnvironmentVariable("WITNESS_INTAKE_PREVIEW_OUT",Path.GetFullPath("Docs/Verification/VoxelWorld/WitnessIntake-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")));
                Run();
            }
            finally{Environment.SetEnvironmentVariable("WITNESS_INTAKE_PREVIEW_OUT",old);}
        }
        public static void BuildKits(){DrownedLedgerVoxelKitBuilder.Run();MarrowstyeVoxelKitBuilder.Run();}
        public static void BuildAndRun(){BuildKits();Run();}
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("WITNESS_INTAKE_PREVIEW_OUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("WITNESS_INTAKE_PREVIEW_OUT is required.");
            Directory.CreateDirectory(output);
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            bool enabled=Village3DSettings.Enabled;Village3DSettings.Enabled=true;
            var rows=new List<Row>();
            try
            {
                foreach(string id in new[]{"Overworld.17.5.0","Overworld.12.12.0"})
                foreach(int seed in new[]{64,1729,729490642})
                {
                    var manager=OverworldZoneManager.CreateDetached(factory,seed);
                    // The manager's existing parent-first guard owns travel order.
                    var watch=System.Diagnostics.Stopwatch.StartNew();var zone=manager.GetZone(id);watch.Stop();
                    AddArrivingPlayer(zone,factory);
                    bool ledger=DrownedLedgerCompositionPlan.IsSupportedZone(id);
                    string form=ledger?"DrownedLedger":"Marrowstye";
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
                        rows.Add(new Row{area=form,formation=form,zoneId=id,image=name,seed=seed,
                            entities=zone.EntityCount,solids=zone.GetAllEntities().Count(e=>e.HasTag("Solid")),
                            water=zone.GetAllEntities().Count(e=>e.HasPart<LiquidPoolPart>()),creatures=zone.GetAllEntities().Count(e=>e.HasTag("Creature")),
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
