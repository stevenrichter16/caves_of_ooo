using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Five optional, usable furnishings on a fresh boatyard's final graph.
    /// Residents, original furniture, stock, routes and reservations retain first claim.</summary>
    public sealed class SumpholdFurnishingBuilder : IZoneBuilder
    {
        public string Name => "SumpholdFurnishing";
        public int Priority => 4600;
        readonly OverworldZoneManager manager;
        readonly WorldMap map;
        readonly EntityFactory sourceFactory;
        readonly SumpholdCompositionBuilder terrain;
        SumpholdCompositionPlan completed;
        public SumpholdFurnishingBuilder(OverworldZoneManager manager, SumpholdCompositionBuilder terrain)
        { this.manager=manager; map=manager?.WorldMap; sourceFactory=manager?.Factory; this.terrain=terrain; }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            var plan=terrain?.Plan;
            bool Current() => zone!=null && manager!=null && plan!=null
                && SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID)
                && ReferenceEquals(map,manager.WorldMap) && map.Tiles[15,6]==BiomeType.Spread
                && AreaCompositionScope.IsSumpholdSite(map.GetPOI(15,6))
                && ReferenceEquals(sourceFactory,factory) && ReferenceEquals(factory,manager.Factory)
                && ReferenceEquals(terrain.SourceFactory,factory) && terrain.Seed==manager.WorldSeed
                && ReferenceEquals(terrain.RealizedZone,zone) && ReferenceEquals(terrain.Plan,plan) && terrain.ProfileRealized
                && !manager.CachedZones.ContainsKey(zone.ZoneID) && !ReferenceEquals(manager.ActiveZone,zone);
            if(!Current() || ReferenceEquals(completed,plan))return true;
            completed=plan;
            var rest=plan.Rooms.Single(r=>r.Role=="CuttersHouse");
            var records=plan.Rooms.Single(r=>r.Role=="RecordsShelter");
            var left=plan.WorkAreas.Single(w=>w.Role=="HullTrestles");
            var right=plan.WorkAreas.Single(w=>w.Role=="PeatWork");
            var groups=new[] {
                new[]{(x:rest.X+3,y:rest.Y+2,bp:"Chair"),(x:rest.X+rest.Width-4,y:rest.Y+rest.Height-3,bp:"Bed")},
                new[]{(x:records.X+3,y:records.Y+3,bp:"Chair")},
                new[]{(x:left.X+1,y:left.Y+4,bp:"Crate")},
                new[]{(x:right.X-1,y:right.Y+4,bp:"Crate")} };
            var anchors=new[] {
                new[]{(rest.X+2,rest.Y+2,"Bed"),(rest.X+rest.Width-3,rest.Y+rest.Height-3,"Chair")},
                new[]{(records.X+2,records.Y+2,"Crate")},
                new[]{(left.X-1,left.Y+4,"BoatFrame"),(left.X+1,left.Y+2,"PeatCutter")},
                new[]{(right.X+1,right.Y+4,"BoatFrame"),(right.X-1,right.Y+2,"PeatCutter")} };
            var originals=zone.GetReadOnlyEntities().ToArray();
            var intact=SpreadGenerationReceipt.CaptureFinalState(zone,originals);
            string tiles=zone.TileState.ToSaveString(); var reserves=new HashSet<(int,int)>(zone.GenReservedCells);
            bool Preserved() => Current() && intact() && tiles==zone.TileState.ToSaveString() && reserves.SetEquals(zone.GenReservedCells);
            var ids=new HashSet<string>(originals.Select(e=>e.ID),StringComparer.Ordinal);
            // Stage and validate before AddEntity: no Barren/footprint/tile-state payload
            // may alter an old owner as a side effect of this optional addition.
            var staged=new Entity[groups.Length][];
            for(int g=0;g<groups.Length;g++)
            {
                staged[g]=new Entity[groups[g].Length]; bool valid=true;
                for(int i=0;i<groups[g].Length;i++)
                {
                    string bp=groups[g][i].bp;
                    if(!factory.Blueprints.ContainsKey(bp)){valid=false;break;}
                    var e=factory.CreateEntity(bp);
                    if(!Preserved())return false;
                    if(!Payload(e,bp)||!ids.Add(e.ID)){valid=false;break;}
                    staged[g][i]=e;
                }
                if(!valid)staged[g]=null;
            }
            for(int g=0;g<groups.Length;g++)
            {
                if(staged[g]==null || !Preserved())continue;
                if(!anchors[g].All(a=>zone.GetCell(a.Item1,a.Item2).Objects.Any(e=>SumpholdCompositionBuilder.Valid(e,a.Item3))))continue;
                if(!groups[g].All(p=>Fits(zone,plan,p.x,p.y,g<2)))continue;
                var geometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
                var solids=groups[g].Where(p=>p.bp=="Crate").Select(p=>(p.x,p.y)).ToArray();
                if(!geometry.PreservesRoutes(solids))continue;
                var blocked=new HashSet<(int,int)>(solids);
                var reach=geometry.Flood(blocked,null,int.MaxValue,null);
                bool connected=true;
                for(int y=0;y<Zone.Height&&connected;y++)for(int x=0;x<Zone.Width;x++)
                    if(geometry.BorderReach[x,y]&&!reach[x,y]&&!blocked.Contains((x,y))){connected=false;break;}
                if(!connected || !groups[g].All(p=>Adjacent(reach,p.x,p.y)))continue;
                // In addition to preserving reached cells, preserve each old solid
                // owner's last reachable frontage if the candidate occupied it.
                if(originals.Any(e=> {
                    var c=zone.GetEntityCell(e);
                    return c!=null && Adjacent(geometry.BorderReach,c.X,c.Y) && !Adjacent(reach,c.X,c.Y);
                }))continue;
                var added=new List<Entity>(); bool complete=true;
                for(int i=0;i<groups[g].Length;i++)
                {
                    var p=groups[g][i];
                    if(!Payload(staged[g][i],p.bp)||!zone.AddEntity(staged[g][i],p.x,p.y)){complete=false;break;}
                    added.Add(staged[g][i]);
                }
                if(!complete || !Preserved())foreach(var e in added)zone.RemoveEntity(e);
            }
            return true;
        }
        static bool Adjacent(bool[,] reach,int x,int y) =>
            (x>0&&reach[x-1,y])||(x+1<Zone.Width&&reach[x+1,y])||(y>0&&reach[x,y-1])||(y+1<Zone.Height&&reach[x,y+1]);
        static bool Fits(Zone z,SumpholdCompositionPlan p,int x,int y,bool interior)
        {
            var c=z.GetCell(x,y);
            if(c==null || c.IsInterior!=interior || p.IsInterior(x,y)!=interior || p.IsWet(x,y)
                || p.IsApproach(x,y)||p.IsReserved(x,y)||z.GenReservedCells.Contains((x,y))||p.ObjectAt(x,y)!=null
                ||z.TileState.Get(x,y)?.IsEmpty==false||c.Objects.Count!=1||c.Occupants.Count()!=1)return false;
            var e=c.Objects[0];
            return SumpholdCompositionBuilder.Valid(e,interior?"StoneFloor":"Floor") && Safe(e)
                &&!e.HasPart<ContainerPart>()&&!e.HasPart<BedPart>()&&!e.HasPart<ChairPart>();
        }
        static bool Safe(Entity e) => !e.HasTag("Barren")&&!e.HasTag("Creature")&&!e.HasTag("Item")
            &&!e.HasPart<BrainPart>()&&!e.HasPart<SpatialFootprintPart>()&&!e.HasPart<TileStateSourcePart>()
            &&!e.HasPart<LiquidPoolPart>()&&!e.HasPart<GasPoolPart>()&&!e.HasPart<TriggerOnStepPart>()
            &&!e.HasPart<CropPart>()&&!e.HasPart<CultivatedSoilPart>()&&!e.HasPart<FieldHarvestPart>()&&!e.HasPart<HarvestablePart>()
            &&!e.HasPart<InventoryPart>()&&!e.HasPart<TraderPart>()&&!e.HasPart<ConversationPart>()&&!e.HasPart<ThrowingStatePart>()
            &&!e.HasPart<DoorPart>()&&!e.HasPart<SealedLibraryBarrierPart>()&&!e.HasPart<StairsUpPart>()&&!e.HasPart<StairsDownPart>();
        static bool Payload(Entity e,string bp)
        {
            if(!SumpholdCompositionBuilder.Valid(e,bp)||!Safe(e)||string.IsNullOrEmpty(e.ID)||e.SpatialZone!=null
                ||e.Parts.Any(p=>p==null||p.ParentEntity!=e))return false;
            var render=e.GetPart<RenderPart>();
            if(render.RenderLayer!=(bp=="Crate"?1:5)||render.RenderString!=(bp=="Crate"?"0":bp=="Chair"?"h":"=")
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants)
                ||e.HasTag("Player")||e.HasTag("Terrain")||e.HasTag("Crop")||e.HasTag("Plantable")||e.HasTag("Restable")
                ||e.HasTag("WellGroundMarker")||e.HasTag("OvenGroundMarker")||e.HasTag("LanternGroundMarker")
                ||e.HasPart<Body>()||e.GetStat("Hitpoints")!=null||e.HasPart<WellPart>()||e.HasPart<RepairablePart>()
                ||e.HasPart<RegionalSignpostPart>()||e.HasPart<CampfirePart>()||e.HasPart<AlchemyStillPart>()
                ||e.HasPart<SoddenPreparationPart>()||e.HasPart<KitchenBatchPart>()||e.HasPart<ForgePart>()
                ||e.HasPart<FiniteLiquidFilmPart>()||e.HasPart<BurnOffGasPart>()||e.HasPart("Restable")||e.HasPart<LockPart>())return false;
            var physics=e.GetPart<PhysicsPart>();
            if(physics.InInventory!=null||physics.Equipped!=null)return false;
            if(bp=="Crate")
            {
                var c=e.GetPart<ContainerPart>();var d=e.GetPart<DestructiblePart>();
                return !e.HasPart<BedPart>()&&!e.HasPart<ChairPart>()&&c!=null&&c.MaxItems==8&&!c.IsLocked&&c.Contents.Count==0
                    &&d!=null&&d.HP==10&&d.MaxHP==10&&d.Hardness==0&&string.IsNullOrEmpty(d.WreckageBlueprint)
                    &&!e.HasPart<ThermalPart>()&&!d.Gone&&!d.Indestructible;
            }
            if(e.HasPart<ContainerPart>()||!e.HasTag("Furniture")||e.HasPart<MaterialPart>()||e.HasPart<DestructiblePart>()||e.HasPart<ThermalPart>())return false;
            if(bp=="Bed")return !e.HasPart<ChairPart>()&&string.IsNullOrEmpty(e.GetPart<BedPart>().Owner)&&!e.GetPart<BedPart>().Occupied;
            var seat=e.GetPart<ChairPart>();
            return !e.HasPart<BedPart>()&&seat!=null&&string.IsNullOrEmpty(seat.Owner)&&!seat.Occupied&&seat.Occupant==null;
        }
    }
}
