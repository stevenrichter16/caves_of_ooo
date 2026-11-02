using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    /// <summary>Optional cold ruin architecture after existing population/stock.
    /// One broken colonnade and passable fragments; never retrofits cached saves.</summary>
    public sealed class BeatingRuinFragmentsBuilder : IZoneBuilder
    {
        public string Name=>"BeatingRuinFragments";
        public int Priority=>4350;
        readonly OverworldZoneManager manager;readonly BeatingCompositionBuilder terrain;
        readonly WorldMap map;readonly EntityFactory factory;readonly int seed;readonly string zoneID;
        BeatingCompositionPlan completed;
        public BeatingRuinFragmentsBuilder(OverworldZoneManager manager,BeatingCompositionBuilder terrain,string zoneID)
        {this.manager=manager;this.terrain=terrain;map=manager?.WorldMap;factory=manager?.Factory;seed=manager?.WorldSeed??0;this.zoneID=zoneID;}
        static readonly (int x,int y)[][] Shapes={
            new[]{(0,0),(1,0),(0,1),(1,1)},new[]{(0,0),(1,0),(0,1)},new[]{(0,0),(1,0)}};
        static readonly (int x,int y)[] Colonnade={(0,0),(1,0),(0,1),(1,1),(0,2),(1,2),(2,2)};
        public bool BuildZone(Zone zone,EntityFactory supplied,Random rng)
        {
            var plan=terrain?.Plan;
            bool Current()
            {
                if(zone==null||manager==null||map==null||supplied!=factory||manager.Factory!=factory
                    ||manager.WorldMap!=map||manager.WorldSeed!=seed||zone.ZoneID!=zoneID
                    ||!BeatingCompositionPlan.IsWildernessZone(zoneID)||terrain.SourceZone!=zone||terrain.SourceFactory!=factory
                    ||terrain.Plan!=plan||plan==null||plan.ZoneID!=zoneID||plan.Seed!=seed||plan.Formation!=Formation.RuinField
                    ||terrain.FormationOverride!=Formation.None
                    ||manager.CachedZones.ContainsKey(zoneID)||manager.ActiveZone==zone||LairStacks.HasSavedColumn(manager,zoneID)
                    ||map.Tiles==null||map.Tiles.GetLength(0)!=WorldMap.Width||map.Tiles.GetLength(1)!=WorldMap.Height
                    ||map.POIs==null||map.POIs.GetLength(0)!=WorldMap.Width||map.POIs.GetLength(1)!=WorldMap.Height)return false;
                var at=WorldMap.FromZoneID(zoneID);
                return map.GetBiome(at.x,at.y)==BiomeType.Beating&&map.GetPOI(at.x,at.y)==null;
            }
            if(!Current()||completed==plan)return true;
            completed=plan;
            if(factory.Blueprints==null||!factory.Blueprints.ContainsKey("BrokenColumn")||!factory.Blueprints.ContainsKey("Pillar"))return true;
            var original=new HashSet<Entity>(zone.GetReadOnlyEntities());var intact=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            var reserves=new HashSet<(int x,int y)>(zone.GenReservedCells);string state=zone.TileState.ToSaveString();
            bool Baseline()=>Current()&&intact()&&reserves.SetEquals(zone.GenReservedCells)&&state==zone.TileState.ToSaveString();
            var geometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            var positions=new List<(int x,int y,string bp)>();
            var reach=WaysiteBuilder.Reach(zone);bool colonnadePlaced=false;
            for(int room=0;room<Math.Min(plan.RuinCount,Shapes.Length);room++)
            {
                var ruin=plan.GetRuin(room);bool found=false;
                // Prefer surviving wall shoulders; uneven compact groups leave
                // the original approach and the middle of each room quiet.
                var candidates=from y in Enumerable.Range(ruin.Y+1,ruin.Height-2)
                    from x in Enumerable.Range(ruin.X+1,ruin.Width-2)
                    orderby Math.Min(x-ruin.X,ruin.X+ruin.Width-1-x),y,x select(x,y);
                foreach(var shape in colonnadePlaced?new[]{Shapes[room]}:new[]{Colonnade,Shapes[room]})
                {
                    bool isColonnade=ReferenceEquals(shape,Colonnade);
                    foreach(var at in candidates)
                    {
                        for(int turn=0;turn<4;turn++)
                        {
                            var group=shape.Select((offset,i)=>
                            {var s=Rotate(offset,turn);return(x:at.x+s.x,y:at.y+s.y,bp:isColonnade&&(i==0||i==4)?"Pillar":"BrokenColumn");}).ToArray();
                            if(!group.All(p=>Fits(zone,plan,ruin,geometry,p.x,p.y)))continue;
                            var blocked=group.Where(p=>p.bp=="Pillar").Select(p=>(p.x,p.y)).ToArray();
                            if(blocked.Length>0&&(!blocked.All(p=>geometry.BorderReach[p.x,p.y])||!geometry.PreservesRoutes(blocked)))continue;
                            positions.AddRange(group);found=true;if(isColonnade)colonnadePlaced=true;break;
                        }
                        if(found)break;
                    }
                    if(found)break;
                }
            }
            if(positions.Count==0)return true;
            var staged=new Entity[positions.Count];
            for(int n=0;n<staged.Length;n++)
            {
                staged[n]=factory.CreateEntity(positions[n].bp);
                if(!Baseline()||!original.SetEquals(zone.GetReadOnlyEntities()))return false;
            }
            var ids=new HashSet<string>(original.Select(e=>e.ID),StringComparer.Ordinal);
            for(int n=0;n<staged.Length;n++)if(!Payload(staged[n],positions[n].bp)||!ids.Add(staged[n].ID))return true;
            var added=new List<Entity>();bool success=true;
            for(int n=0;n<staged.Length;n++)
            {var p=positions[n];if(!zone.AddEntity(staged[n],p.x,p.y)){success=false;break;}added.Add(staged[n]);}
            var after=WaysiteBuilder.Reach(zone);
            if(!success||!Baseline()||zone.EntityCount!=original.Count+added.Count
                ||after.parts>reach.parts||after.cells<reach.cells-staged.Count(e=>e.GetPart<PhysicsPart>().Solid))
            {foreach(var e in added)if(zone.GetEntityCell(e)!=null)zone.RemoveEntity(e);return Baseline()&&original.SetEquals(zone.GetReadOnlyEntities());}
            return true;
        }
        static (int x,int y) Rotate((int x,int y) p,int turn)
            =>turn==0?p:turn==1?(-p.y,p.x):turn==2?(-p.x,-p.y):(p.y,-p.x);
        static bool Fits(Zone z,BeatingCompositionPlan plan,BeatingCompositionPlan.Ruin ruin,SpreadWildernessSituationBuilder.Geometry geometry,int x,int y)
        {
            if(!ruin.Contains(x,y)||ruin.Boundary(x,y)||!geometry.Place(x,y)||plan.ObjectAt(x,y)!=null
                ||plan.GroundAt(x,y)!="SandstoneFloor"||plan.IsRoad(x,y))return false;
            if(Math.Abs(x-(ruin.X+ruin.Width/2))<=1&&(y<=ruin.Y+2||y>=ruin.Y+ruin.Height-3))return false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(plan.IsApproach(x+dx,y+dy))return false;
            var c=z.GetCell(x,y);if(c.Occupants.Count!=1||c.Objects.Count!=1||c.Objects[0].BlueprintName!="SandstoneFloor")return false;
            var ground=c.Objects[0];
            if(ground.HasPart<CultivatedSoilPart>()||ground.HasPart<FieldHarvestPart>()||ground.HasPart<HarvestablePart>()
                ||ground.HasPart<CropPart>()||ground.HasPart<SpatialFootprintPart>()||ground.HasPart<TileStateSourcePart>())return false;
            bool wall=false;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                foreach(var e in z.GetCell(x+dx,y+dy).Occupants)
                {
                    if(e.BlueprintName=="SandstoneWall")wall=true;
                    else if(e.BlueprintName!="DryBrush"&&!DoorPart.IsBareGround(e))return false;
                }
            return wall;
        }
        static bool Payload(Entity e,string intended)
        {
            if(e?.BlueprintName!=intended)return false;
            if(intended=="BrokenColumn")return FragmentPayload(e);
            var p=e.GetPart<PhysicsPart>();var r=e.GetPart<RenderPart>();
            var d=e.GetPart<DestructiblePart>();var m=e.GetPart<MaterialPart>();var t=e.GetPart<ThermalPart>();
            return intended=="Pillar"&&!string.IsNullOrEmpty(e.ID)&&e.SpatialZone==null
                &&p!=null&&p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
                &&e.HasTag("Solid")&&e.HasTag("Stone")&&e.HasTag("Mineral")
                &&!e.HasTag("Terrain")&&!e.HasTag("Wall")&&!e.HasTag("Player")&&!e.HasTag("Creature")&&!e.HasTag("Item")
                &&r!=null&&r.Visible&&r.RenderLayer==1&&r.RenderString=="I"&&r.ColorString=="&w"
                &&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)&&string.IsNullOrEmpty(r.GlyphVariants)&&string.IsNullOrEmpty(r.BackgroundColor)
                &&d!=null&&!d.Gone&&!d.Indestructible&&d.HP==50&&d.MaxHP==50&&d.Hardness==4&&d.WreckageBlueprint=="Rubble"
                &&m!=null&&m.MaterialID=="Stone"&&m.Combustibility==0f&&m.HasMaterialTag("Stone")&&m.HasMaterialTag("Mineral")
                &&t!=null&&float.IsFinite(t.Temperature)&&float.IsFinite(t.FreezeTemperature)
                &&t.Temperature>t.FreezeTemperature&&t.Temperature<t.FlameTemperature&&t.FlameTemperature==1200f&&t.HeatCapacity==3f
                &&e.GetPart<ExaminablePart>()!=null&&e.Parts.Count==6&&e.Parts.All(part=>part!=null&&part.ParentEntity==e)
                &&e.Parts.All(part=>part is PhysicsPart||part is RenderPart||part is ExaminablePart||part is DestructiblePart||part is MaterialPart||part is ThermalPart);
        }
        static bool FragmentPayload(Entity e)
        {
            var p=e?.GetPart<PhysicsPart>();var r=e?.GetPart<RenderPart>();
            return e!=null&&!string.IsNullOrEmpty(e.ID)&&e.BlueprintName=="BrokenColumn"&&e.SpatialZone==null
                &&p!=null&&p.ParentEntity==e&&!p.Takeable&&!p.Solid&&p.InInventory==null&&p.Equipped==null
                &&e.HasTag("Terrain")&&!e.HasTag("Solid")&&!e.HasTag("Wall")&&!e.HasTag("Player")&&!e.HasTag("Creature")&&!e.HasTag("Item")
                &&r!=null&&r.Visible&&r.RenderLayer==0&&r.RenderString==","&&r.ColorString=="&w"
                &&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)&&string.IsNullOrEmpty(r.GlyphVariants)&&string.IsNullOrEmpty(r.BackgroundColor)
                &&e.GetPart<ExaminablePart>()!=null&&e.Parts.Count==3&&e.Parts.All(part=>part!=null&&part.ParentEntity==e)
                &&e.Parts.All(part=>part is PhysicsPart||part is RenderPart||part is ExaminablePart);
        }
    }
}
