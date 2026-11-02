using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    /// <summary>Optional cold SummitScrub shoulder shelter. Runs after existing
    /// creatures, loot and crop placement; never changes the immutable terrain
    /// plan, consumes generation RNG or retrofits a cached/active graph.</summary>
    public sealed class StumpSummitPocketBuilder : IZoneBuilder
    {
        public string Name=>"StumpSummitPocket";
        public int Priority=>4350;
        readonly OverworldZoneManager manager;readonly StumpCompositionBuilder terrain;
        readonly WorldMap map;readonly EntityFactory factory;readonly int seed;readonly string zoneID;
        StumpCompositionPlan completed;
        public StumpSummitPocketBuilder(OverworldZoneManager manager,StumpCompositionBuilder terrain,string zoneID)
        {this.manager=manager;this.terrain=terrain;map=manager?.WorldMap;factory=manager?.Factory;seed=manager?.WorldSeed??0;this.zoneID=zoneID;}
        static readonly (int x,int y,string bp)[] Shape={
            (0,0,"StoneDome"),(1,0,"StoneDome"),(2,0,"StoneDome"),(1,1,"StoneDome"),(-1,1,"Tree"),
            (-3,0,"Bush"),(-2,0,"Bush"),(-2,1,"Bush"),(-1,2,"Bush"),(0,2,"Bush"),(1,2,"Bush")};
        public bool BuildZone(Zone zone,EntityFactory supplied,Random rng)
        {
            var plan=terrain?.Plan;
            bool Current()
            {
                if(zone==null||manager==null||map==null||supplied!=factory||manager.Factory!=factory
                    ||manager.WorldMap!=map||manager.WorldSeed!=seed||zone.ZoneID!=zoneID
                    ||!StumpCompositionPlan.IsWildernessZone(zoneID)||terrain.SourceZone!=zone||terrain.SourceFactory!=factory
                    ||terrain.Plan!=plan||plan==null||plan.ZoneID!=zoneID||plan.Seed!=seed
                    ||plan.Band!=StumpBand.Summit||plan.Formation!=Formation.SummitScrub
                    ||manager.CachedZones.ContainsKey(zoneID)||manager.ActiveZone==zone||LairStacks.HasSavedColumn(manager,zoneID)
                    ||map.Tiles==null||map.Tiles.GetLength(0)!=WorldMap.Width||map.Tiles.GetLength(1)!=WorldMap.Height
                    ||map.POIs==null||map.POIs.GetLength(0)!=WorldMap.Width||map.POIs.GetLength(1)!=WorldMap.Height)return false;
                var at=WorldMap.FromZoneID(zoneID);
                return map.GetBiome(at.x,at.y)==BiomeType.Stump&&map.GetPOI(at.x,at.y)==null;
            }
            if(!Current()||completed==plan)return true;
            completed=plan;
            if(factory.Blueprints==null||Shape.Any(s=>!factory.Blueprints.ContainsKey(s.bp)))return true;
            var original=new HashSet<Entity>(zone.GetReadOnlyEntities());var intact=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            var reserves=new HashSet<(int x,int y)>(zone.GenReservedCells);string state=zone.TileState.ToSaveString();
            bool Baseline()=>Current()&&intact()&&reserves.SetEquals(zone.GenReservedCells)&&state==zone.TileState.ToSaveString();
            var staged=new Entity[Shape.Length];
            for(int n=0;n<staged.Length;n++)
            {
                staged[n]=factory.CreateEntity(Shape[n].bp);
                if(!Baseline()||!original.SetEquals(zone.GetReadOnlyEntities()))return false;
                if(!Payload(staged[n],Shape[n].bp))return true;
            }
            var ids=new HashSet<string>(original.Select(e=>e.ID),StringComparer.Ordinal);
            for(int n=0;n<staged.Length;n++)if(!Payload(staged[n],Shape[n].bp)||!ids.Add(staged[n].ID))return true;
            var geometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            // Southeast shoulder: the original open approach and its halo retain
            // first claim. Bounded alternatives decline rather than fill elsewhere.
            var candidates=from y in Enumerable.Range(15,3) from x in Enumerable.Range(45,5)
                orderby Math.Abs(x-47)+Math.Abs(y-16),y,x select (x,y);
            foreach(var at in candidates)
            {
                var positions=Shape.Select(s=>(x:at.x+s.x,y:at.y+s.y)).ToArray();
                if(!positions.All(p=>Fits(zone,plan,geometry,p.x,p.y)))continue;
                var solids=positions.Where((p,n)=>staged[n].GetPart<PhysicsPart>().Solid).ToArray();
                if(!geometry.PreservesRoutes(solids))continue;
                var reach=geometry.Flood(new HashSet<(int x,int y)>(solids),null,int.MaxValue,null);bool connected=true;
                for(int y=0;y<Zone.Height&&connected;y++)for(int x=0;x<Zone.Width;x++)
                    if(geometry.BorderReach[x,y]&&!reach[x,y]&&!solids.Contains((x,y))){connected=false;break;}
                if(!connected||!Baseline())continue;
                var added=new List<Entity>();bool success=true;
                for(int n=0;n<staged.Length;n++)
                {var p=positions[n];if(!zone.AddEntity(staged[n],p.x,p.y)){success=false;break;}added.Add(staged[n]);}
                if(!success||!Baseline()||zone.EntityCount!=original.Count+added.Count)
                {foreach(var e in added)if(zone.GetEntityCell(e)!=null)zone.RemoveEntity(e);return Baseline()&&original.SetEquals(zone.GetReadOnlyEntities());}
                return true;
            }
            return true;
        }
        static bool Fits(Zone z,StumpCompositionPlan plan,SpreadWildernessSituationBuilder.Geometry geometry,int x,int y)
        {
            if(!geometry.Place(x,y)||plan.IsHabitat(x,y)||plan.ObjectAt(x,y)!=null)return false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(plan.IsApproach(x+dx,y+dy))return false;
            var c=z.GetCell(x,y);
            if(c.Occupants.Count!=1||c.Objects.Count!=1||c.Objects[0].BlueprintName!="TepuiStone")return false;
            var ground=c.Objects[0];
            if(ground.HasPart<CultivatedSoilPart>()||ground.HasPart<FieldHarvestPart>()||ground.HasPart<HarvestablePart>()
                ||ground.HasPart<CropPart>()||ground.HasPart<SpatialFootprintPart>()||ground.HasPart<TileStateSourcePart>())return false;
            // Leave two clear cells around real actors, entrances, services and
            // other supported props; existing rock/vegetation is ecology, not service.
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                foreach(var e in z.GetCell(x+dx,y+dy).Occupants)
                    if(e.BlueprintName!="StoneDome"&&e.BlueprintName!="Tree"&&e.BlueprintName!="Bush"&&!DoorPart.IsBareGround(e))return false;
            return true;
        }
        static bool Payload(Entity e,string bp)
        {
            var p=e?.GetPart<PhysicsPart>();
            return e!=null&&!string.IsNullOrEmpty(e.ID)&&e.BlueprintName==bp&&e.SpatialZone==null&&p!=null&&p.ParentEntity==e
                &&!p.Takeable&&p.InInventory==null&&p.Equipped==null&&p.Solid==(bp!="Bush")
                &&!e.HasTag("Barren")&&!e.HasTag("Creature")&&!e.HasTag("Item")&&!e.HasTag("Wall")&&e.HasTag("Solid")== (bp!="Bush")
                &&e.GetPart<RenderPart>()?.Visible==true&&e.Parts.All(part=>part!=null&&part.ParentEntity==e)
                &&!e.HasPart<CropPart>()&&!e.HasPart<CultivatedSoilPart>()&&!e.HasPart<FieldHarvestPart>()&&!e.HasPart<HarvestablePart>()
                &&!e.HasPart<InventoryPart>()&&!e.HasPart<ContainerPart>()&&!e.HasPart<TraderPart>()&&!e.HasPart<ConversationPart>()
                &&!e.HasPart<SpatialFootprintPart>()&&!e.HasPart<ThrowingStatePart>()&&!e.HasPart<TileStateSourcePart>()
                &&!e.HasPart<LiquidPoolPart>()&&!e.HasPart<GasPoolPart>()&&!e.HasPart<TriggerOnStepPart>()&&!e.HasPart<DoorPart>()
                &&!e.HasPart<SealedLibraryBarrierPart>()&&!e.HasPart<StairsUpPart>()&&!e.HasPart<StairsDownPart>()&&!e.HasPart<WellPart>();
        }
    }
}
