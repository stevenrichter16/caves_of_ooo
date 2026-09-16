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
    /// <summary>On-demand native census of a copied save and same-seed fresh
    /// destinations. Never binds the live save service or writes a save.</summary>
    public static class VoxelWorldAccessibilityAuditBatch
    {
        [Serializable] public sealed class Area
        {
            public string id, biome, name, type, profile, error;
            public bool cachedBeforeAudit, visited, voxelAddress, scopeAllowed, inspectedGraph;
            public int entities, creatures, walls, doors, traders, conversationOwners, unmodeled;
            public string[] census, missingModels, recipeFailures;
        }
        [Serializable] public sealed class Route
        {
            public string source, target, kind, error;
            public bool success;
            public int x, y;
        }
        [Serializable] public sealed class Report
        {
            public int seed, cachedCount;
            public string activeZone;
            public Area[] cached, savedDestinations, freshDestinations, worldMap;
            public Route[] routes;
        }
        private static SpawnRing3DCatalog catalog;
        private static MultiCellPilot3DCatalog pilotCatalog;

        public static void Run()
        {
            string input=Environment.GetEnvironmentVariable("VOXEL_ACCESS_SAVE_COPY");
            string output=Environment.GetEnvironmentVariable("VOXEL_ACCESS_OUT");
            if(string.IsNullOrEmpty(input)||string.IsNullOrEmpty(output))throw new InvalidOperationException("Explicit copied save and audit output required.");
            Directory.CreateDirectory(output);
            LootTableRegistry.Initialize(File.ReadAllText("Assets/Resources/Content/Data/Loot/LootTables.json"));
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText("Assets/Resources/Content/Blueprints/Objects.json"));
            catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            pilotCatalog=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath).Definition;
            var state=SaveGameService.LoadState(input,factory);
            var manager=state.ZoneManager;
            var cachedIds=new HashSet<string>(manager.CachedZones.Keys);
            var report=new Report {seed=manager.WorldSeed,activeZone=state.ActiveZoneID,cachedCount=cachedIds.Count,
                cached=manager.CachedZones.OrderBy(p=>p.Key).Select(p=>Inspect(manager,p.Key,p.Value,true)).ToArray()};
            var map=new List<Area>();
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
                map.Add(Inspect(manager,WorldMap.ToZoneID(x,y,0),null,cachedIds.Contains(WorldMap.ToZoneID(x,y,0))));
            report.worldMap=map.ToArray();
            Write(output,report);

            var ids=new HashSet<string>(WorldMapAuthoring.Places.Select(p=>WorldMap.ToZoneID(p.X,p.Y,0)));
            foreach(var at in new[]{(2,7),(5,4),(2,4),(4,6)})for(int z=0;z<3;z++)ids.Add(WorldMap.ToZoneID(at.Item1,at.Item2,z));
            foreach(string id in new[]{"Overworld.2.6.0","Overworld.3.7.0","Overworld.4.7.0","Overworld.8.8.0","Overworld.16.6.0","Overworld.8.18.0","Overworld.3.2.0","Overworld.1.10.0"})ids.Add(id);
            var saved=new List<Area>();var fresh=new List<Area>();
            var freshManager=OverworldZoneManager.CreateDetached(factory,manager.WorldSeed);
            foreach(string id in ids.OrderBy(n=>n,StringComparer.Ordinal))
            {
                saved.Add(Generate(manager,id,cachedIds.Contains(id)));
                fresh.Add(Generate(freshManager,id,false));
                report.savedDestinations=saved.ToArray();report.freshDestinations=fresh.ToArray();Write(output,report);
            }

            var routes=new List<Route>();
            // Separate in-memory players isolate traversal experiments from the
            // copied player's state. This checks arrival transfer, not the walk
            // from the player's current local cell to the source chunk's edge.
            Edge(manager,factory,"Overworld.16.6.0",TransitionDirection.West,routes);
            Edge(freshManager,factory,"Overworld.2.6.0",TransitionDirection.East,routes);
            foreach(string id in ids.Where(id=>WorldMap.FromZoneID(id).z==0).OrderBy(id=>id,StringComparer.Ordinal))
            {
                var destination=WorldMap.FromZoneID(id);
                var start=manager.GetZone("Overworld.16.6.0");var actor=factory.CreateEntity("Player");
                var cell=EmptyCell(start);start.AddEntity(actor,cell.x,cell.y);
                var up=WorldMapTraversal.TryWorldMapVertical(actor,start,false,manager);
                if(!up.Success){routes.Add(new Route{source=start.ZoneID,target=id,kind="map",error=up.ErrorReason});start.RemoveEntity(actor);continue;}
                var target=WorldMap.WorldCellToZoneCell(destination.x,destination.y);
                if(!up.NewZone.MoveEntity(actor,target.zoneX,target.zoneY))throw new InvalidOperationException("Could not select audit map destination: "+id);
                var down=WorldMapTraversal.TryWorldMapVertical(actor,up.NewZone,true,manager);
                routes.Add(new Route{source=start.ZoneID,target=id,kind="map",success=down.Success&&down.NewZone?.ZoneID==id,
                    error=down.ErrorReason,x=down.NewPlayerX,y=down.NewPlayerY});
                (down.Success?down.NewZone:up.NewZone).RemoveEntity(actor);
            }
            report.routes=routes.ToArray();Write(output,report);
        }

        private static void Write(string output,Report report)=>File.WriteAllText(Path.Combine(output,"native-accessibility.json"),JsonUtility.ToJson(report,true));
        private static Area Generate(OverworldZoneManager manager,string id,bool cached)
        {
            try{return Inspect(manager,id,manager.GetZone(id),cached);}
            catch(Exception e){var row=Inspect(manager,id,null,cached);row.error=e.ToString();return row;}
        }
        private static Area Inspect(OverworldZoneManager manager,string id,Zone zone,bool cached)
        {
            var at=WorldMap.FromZoneID(id);var poi=manager.WorldMap.GetPOI(at.x,at.y);
            var row=new Area{id=id,biome=manager.WorldMap.GetBiome(at.x,at.y).ToString(),name=poi?.Name,type=poi?.Type.ToString(),profile=poi?.Profile,
                cachedBeforeAudit=cached,visited=manager.WorldMap.IsVisited(at.x,at.y),voxelAddress=VoxelWorldPresentation.IsSupported(id),scopeAllowed=zone!=null&&AreaCompositionScope.Allows(zone)};
            if(zone==null)return row;
            row.inspectedGraph=true;
            var owners=zone.GetAllEntities().ToArray();row.entities=owners.Length;
            row.creatures=owners.Count(e=>e.HasTag("Creature"));row.walls=owners.Count(e=>e.HasTag("Wall"));
            row.doors=owners.Count(e=>e.HasTag("Door"));row.traders=owners.Count(e=>e.HasPart<TraderPart>());
            row.conversationOwners=owners.Count(e=>e.Parts.Any(p=>p.Name=="Conversation"));
            row.census=owners.GroupBy(e=>e.BlueprintName).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()).ToArray();
            if(id!="Overworld.3.6.0"&&row.voxelAddress)
            {
                var missing=owners.Where(e=>e.GetPart<RenderPart>()?.Visible==true&&SpawnRing3DRecipes.Resolve(zone,e,catalog,pilotCatalog).ModelId==null).ToArray();
                row.unmodeled=missing.Length;row.missingModels=missing.Select(e=>e.BlueprintName).Distinct().OrderBy(n=>n).ToArray();
                row.recipeFailures=missing.Select(e=>e.BlueprintName+":"+SpawnRing3DRecipes.Resolve(zone,e,catalog,pilotCatalog).Failure).Distinct().OrderBy(n=>n).ToArray();
            }
            return row;
        }
        private static (int x,int y) EmptyCell(Zone zone)
        {
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(zone.GetCell(x,y).IsPassable())return(x,y);
            throw new InvalidOperationException("No passable audit source: "+zone.ZoneID);
        }
        private static void Edge(OverworldZoneManager manager,EntityFactory factory,string id,TransitionDirection direction,List<Route> rows)
        {
            var zone=manager.GetZone(id);var cell=EmptyCell(zone);var actor=factory.CreateEntity("Player");zone.AddEntity(actor,cell.x,cell.y);
            var result=ZoneTransitionSystem.TransitionPlayer(actor,zone,direction,direction==TransitionDirection.West?0:79,12,manager,manager.WorldMap);
            rows.Add(new Route{source=id,target=result.NewZone?.ZoneID,kind="edge",success=result.Success,error=result.ErrorReason,x=result.NewPlayerX,y=result.NewPlayerY});
            (result.Success?result.NewZone:zone).RemoveEntity(actor);
        }
    }
}
