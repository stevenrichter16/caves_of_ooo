using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Connects authored art to persistent game entities. All writes
    /// occur on generation, one-time legacy upgrade or an explicit player action.</summary>
    public static class FellingSceneRuntime
    {
        public const string TerrainTag="FellingAuthoredTerrain";
        private const string StateId="felling-scene-state";
        private const string OwnerPrefix="felling-owner:";
        private static readonly string[] Required={"TepuiStone","WaterPuddle","FellingScar","FellingBarePosition","SeventhPosition"};
        public static FellingSceneStatePart GetState(Zone zone)
        {
            var cell=zone?.GetCell(0,0);if(cell==null)return null;
            for(int i=0;i<cell.Objects.Count;i++)
                if(cell.Objects[i].ID==StateId)return cell.Objects[i].GetPart<FellingSceneStatePart>();
            return null;
        }
        public static bool IsActive(Zone zone)
            =>zone!=null&&zone.ZoneID==FellingSiteBuilder.ZoneID&&GetState(zone)?.Revision==1&&FellingSceneDefinition.Load()!=null;
        public static Entity FindOwner(Zone zone,string id)
        {
            if(zone==null||string.IsNullOrEmpty(id))return null;
            var state=GetState(zone);if(state==null||state.WasRemoved(id))return null;
            if(state.Owners==null)
            {
                state.Owners=new Dictionary<string,Entity>(StringComparer.Ordinal);
                foreach(var entity in zone.GetReadOnlyEntities())
                {
                    var part=entity.GetPart<FellingScenePropPart>();
                    if(part!=null&&entity.ID==OwnerPrefix+part.ComponentId&&!state.Owners.ContainsKey(part.ComponentId))state.Owners.Add(part.ComponentId,entity);
                }
            }
            if(!state.Owners.TryGetValue(id,out var owner))return null;
            var cell=zone.GetEntityCell(owner);
            return cell!=null&&cell.Objects.Contains(owner)?owner:null;
        }
        public static bool IsPresent(Zone zone,string id)=>IsActive(zone)&&FindOwner(zone,id)!=null;

        /// <summary>Upgrade only the canonical zone; callers additionally gate
        /// its saved POI type. Unknown occupants and durable IDs are retained.</summary>
        public static bool UpgradeCachedZone(Zone zone,EntityFactory factory)
        {
            if(zone==null||zone.ZoneID!=FellingSiteBuilder.ZoneID)return false;
            return Install(zone,factory,preserveExisting:true);
        }
        /// <summary>Stages required content before changing the zone. Fresh
        /// builds replace old content; upgrades replace known legacy scenery only.</summary>
        public static bool Install(Zone zone,EntityFactory factory,bool preserveExisting=false)
        {
            if(zone==null||factory==null)return Refuse(zone,"MissingArgument");
            var state=GetState(zone);
            if(state!=null)
            {
                if(state.Revision!=1)return false;
                // Explicit builder benches can author lore geometry in a
                // disposable zone. Habitat migration belongs to the real POI.
                if(zone.ZoneID!=FellingSiteBuilder.ZoneID)return true;
                bool fauna=FellingScenePopulation.EnsurePopulation(zone,factory);
                bool dressing=FellingScenePopulation.EnsureDressing(zone,factory);
                return fauna&&dressing;
            }
            var definition=FellingSceneDefinition.Load();if(definition==null)return Refuse(zone,"MissingDefinition");
            foreach(string blueprint in Required)if(!factory.Blueprints.ContainsKey(blueprint))return Refuse(zone,"MissingBlueprint:"+blueprint);
            var before=zone.GetAllEntities();var oldBare=new List<Entity>();Entity oldSeventh=null;bool oldCircle=false;
            foreach(var entity in before)
            {
                if(entity.BlueprintName=="FellingBarePosition"){oldBare.Add(entity);oldCircle=true;}
                if(entity.BlueprintName=="FellingScar")oldCircle=true;
                if(entity.HasPart<SeventhPositionPart>()){oldSeventh=entity;oldCircle=true;}
            }
            bool preserveMissing=preserveExisting&&oldCircle&&oldSeventh==null;
            var staged=new List<(Entity entity,int x,int y)>(2100);
            foreach(var spec in definition.cells)
            {
                var terrain=factory.CreateEntity(spec.water?"WaterPuddle":"TepuiStone");
                if(terrain==null)return Refuse(zone,"TerrainFactoryFailure");
                terrain.ID="felling-terrain:"+spec.x+":"+spec.y;terrain.SetTag(TerrainTag);
                var physics=terrain.GetPart<PhysicsPart>();if(physics==null){physics=new PhysicsPart();terrain.AddPart(physics);}
                physics.Solid=spec.solid;physics.Takeable=false;
                if(spec.solid)terrain.SetTag("Solid");else terrain.Tags.Remove("Solid");
                if(spec.opaque)terrain.SetTag("Wall");else terrain.Tags.Remove("Wall");
                terrain.GetPart<RenderPart>().RenderLayer=0;
                if(spec.water)
                {
                    terrain.GetPart<RenderPart>().DisplayName="blackwater river";
                    var examine=terrain.GetPart<ExaminablePart>();if(examine!=null)examine.Text="Cold water runs beneath the petrified roots. The deep current offers no safe footing.";
                }
                staged.Add((terrain,spec.x,spec.y));
            }
            int bareIndex=0;
            foreach(var landmark in definition.landmarks)
            {
                if(landmark.kind=="seventh"&&preserveMissing)continue;
                Entity entity;
                if(landmark.kind=="seventh")entity=preserveExisting&&oldSeventh!=null?oldSeventh:factory.CreateEntity("SeventhPosition");
                else entity=preserveExisting&&bareIndex<oldBare.Count?oldBare[bareIndex++]:factory.CreateEntity("FellingBarePosition");
                if(entity==null)return Refuse(zone,"LandmarkFactoryFailure");
                staged.Add((entity,landmark.x,landmark.y));
            }
            // State rides on ordinary ground: a separate invisible entity
            // would still leak into the game's unfiltered object picker.
            Entity marker=null;
            foreach(var entry in staged)if(entry.x==0&&entry.y==0){marker=entry.entity;break;}
            marker.ID=StateId;
            state=new FellingSceneStatePart{Revision=definition.revision,PreserveMissingSeventh=preserveMissing,Owners=new Dictionary<string,Entity>(StringComparer.Ordinal)};
            marker.AddPart(state);
            foreach(var layer in definition.layers)
            {
                var owner=new Entity{ID=OwnerPrefix+layer.id,BlueprintName="FellingSceneProp"};
                owner.AddPart(new RenderPart{DisplayName=layer.name,RenderString=layer.mutable?"*":"#",ColorString="&y",RenderLayer=3});
                owner.AddPart(new PhysicsPart{Solid=layer.blocksMovement,Takeable=false});
                if(layer.blocksMovement)owner.SetTag("Solid");
                owner.AddPart(new ExaminablePart{Text=layer.mutable?"A separate growth or loose stone among the old roots. Clearing it will expose the ground beneath.":"This is part of the enduring shape of the Felling-Site. Its roots and stone remain in place."});
                owner.AddPart(new FellingScenePropPart{ComponentId=layer.id,Mutable=layer.mutable,ClearLabel=layer.kind.IndexOf("boulder",StringComparison.OrdinalIgnoreCase)>=0||layer.kind.IndexOf("stone",StringComparison.OrdinalIgnoreCase)>=0?"remove loose stone":"clear growth"});
                state.Owners.Add(layer.id,owner);staged.Add((owner,layer.anchorX,layer.anchorY));
            }
            // Mutation begins only after content has been staged successfully.
            foreach(var entity in before)
                if(!preserveExisting||IsKnownLegacy(entity))zone.RemoveEntity(entity);
            zone.GenReservedCells.Clear();
            foreach(var entry in staged)
            {
                // All positions are definition-validated, and source flora owners
                // intentionally carry no vegetation tag on sacred barren cells.
                zone.AddEntity(entry.entity,entry.x,entry.y);
                zone.GenReservedCells.Add((entry.x,entry.y));
            }
            if(preserveExisting)
            {
                // A player saved on newly authored geology must not start trapped.
                // Unknown fixed scenery/items retain their exact saved positions.
                foreach(var entity in before)
                {
                    if(!entity.HasTag("Player")&&!entity.HasTag("Creature"))continue;
                    var cell=zone.GetEntityCell(entity);if(cell==null||!cell.BlocksMovement(entity))continue;
                    var safe=FindArrival(zone,cell.X,cell.Y,entity);
                    if(safe.x>=0)zone.MoveEntity(entity,safe.x,safe.y);
                }
            }
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","FellingSceneInstalled",payload:new{zone=zone.ZoneID,revision=definition.revision,layers=definition.layers.Length,upgrade=preserveExisting,preserveMissingSeventh=preserveMissing});
            if(zone.ZoneID!=FellingSiteBuilder.ZoneID)return true;
            bool populated=FellingScenePopulation.EnsurePopulation(zone,factory);
            bool dressed=FellingScenePopulation.EnsureDressing(zone,factory);
            return populated&&dressed;
        }
        private static bool IsKnownLegacy(Entity entity)
            =>entity.HasTag(TerrainTag)||entity.BlueprintName=="TepuiStone"||entity.BlueprintName=="FellingScar"||entity.BlueprintName=="FellingBarePosition"||entity.BlueprintName=="SeventhPosition";
        private static (int x,int y) FindArrival(Zone zone,int x,int y,Entity actor)
        {
            for(int r=1;r<Zone.Width;r++)for(int dy=-r;dy<=r;dy++)for(int dx=-r;dx<=r;dx++)
            {if(Math.Abs(dx)!=r&&Math.Abs(dy)!=r)continue;var cell=zone.GetCell(x+dx,y+dy);if(cell!=null&&!cell.BlocksMovement(actor))return(cell.X,cell.Y);}
            return(-1,-1);
        }
        private static bool Refuse(Zone zone,string reason)
        {
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","FellingSceneRefused",payload:new{zone=zone?.ZoneID,reason});
            return false;
        }
    }
}
