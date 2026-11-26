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
        internal const int CargoVersion = 30;
        const int CargoShellWeight = 72;
        public string Name => "SumpholdFurnishing";
        public int Priority => 4600;
        readonly OverworldZoneManager manager;
        readonly WorldMap map;
        readonly EntityFactory sourceFactory;
        readonly SumpholdCompositionBuilder terrain;
        SumpholdCompositionPlan completed;
        Entity cargoCrate;
        Func<bool> cargoUnchanged;
        bool westCargo;
        public SumpholdFurnishingBuilder(OverworldZoneManager manager, SumpholdCompositionBuilder terrain)
        { this.manager=manager; map=manager?.WorldMap; sourceFactory=manager?.Factory; this.terrain=terrain; }

        internal static bool RetainsCargoSite(OverworldZoneManager manager, string zoneID)
            => SumpholdCompositionPlan.IsSupportedZone(zoneID) && manager?.Exploration?.Enabled == true
                && manager.Exploration.Version >= CargoVersion && manager.WorldMap?.Tiles[15,6] == BiomeType.Spread
                && AreaCompositionScope.IsSumpholdSite(manager.WorldMap.GetPOI(15,6));

        bool Current(Zone zone, EntityFactory factory, SumpholdCompositionPlan plan)
            => zone!=null && manager!=null && plan!=null
                && SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID)
                && ReferenceEquals(map,manager.WorldMap) && map.Tiles[15,6]==BiomeType.Spread
                && AreaCompositionScope.IsSumpholdSite(map.GetPOI(15,6))
                && ReferenceEquals(sourceFactory,factory) && ReferenceEquals(factory,manager.Factory)
                && ReferenceEquals(terrain.SourceFactory,factory) && terrain.Seed==manager.WorldSeed
                && ReferenceEquals(terrain.RealizedZone,zone) && ReferenceEquals(terrain.Plan,plan) && terrain.ProfileRealized
                && !manager.CachedZones.ContainsKey(zone.ZoneID) && !ReferenceEquals(manager.ActiveZone,zone);

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            var plan=terrain?.Plan;
            if(!Current(zone,factory,plan) || ReferenceEquals(completed,plan))return true;
            completed=plan;
            cargoCrate=null;
            cargoUnchanged=null;
            int cargoGroup = -1;
            if (RetainsCargoSite(manager, zone.ZoneID))
            {
                westCargo = SpreadExplorationPlan.Rank(manager.WorldSeed, zone.ZoneID, "sumphold-public-freight-v30") % 2 == 0;
                cargoGroup = westCargo ? 2 : 3;
            }
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
            bool Preserved() => Current(zone,factory,plan) && intact() && tiles==zone.TileState.ToSaveString() && reserves.SetEquals(zone.GenReservedCells);
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
                else if (g == cargoGroup)
                {
                    cargoCrate = added.Single();
                    cargoUnchanged = SpreadGenerationReceipt.CaptureFinalState(zone, added);
                }
            }
            return true;
        }

        /// <summary>Promote only the selected furnishing on the final, uncached graph.</summary>
        internal void InstallCargo(Zone zone)
        {
            var crate = cargoCrate;
            var unchanged = cargoUnchanged;
            cargoCrate = null;
            cargoUnchanged = null;
            if (crate == null || unchanged == null || !RetainsCargoSite(manager, zone?.ZoneID)
                || !Current(zone, sourceFactory, completed) || !unchanged()
                || !CargoLane(zone, crate, westCargo) || !sourceFactory.Blueprints.ContainsKey("SalvagedTimber")) return;

            var owners = zone.GetReadOnlyEntities().ToArray();
            var graph = SpreadGenerationReceipt.CaptureFinalState(zone, owners);
            string tileState = zone.TileState.ToSaveString();
            var reservations = new HashSet<(int,int)>(zone.GenReservedCells);
            var timber = sourceFactory.CreateEntity("SalvagedTimber");
            if (!ValidTimber(timber) || !Current(zone, sourceFactory, completed) || !unchanged() || !graph()
                || !owners.SequenceEqual(zone.GetReadOnlyEntities()) || tileState != zone.TileState.ToSaveString()
                || !reservations.SetEquals(zone.GenReservedCells) || HasLocalID(owners, timber.ID)
                || !CargoLane(zone, crate, westCargo)) return;

            var container = crate.GetPart<ContainerPart>();
            timber.GetPart<StackerPart>().StackCount = 2;
            if (!container.AddItem(timber)) return;
            crate.AddPart(new HandlingPart { Weight = CargoShellWeight, Carryable = false, Throwable = false, BulkClass = "Massive" });
            crate.AddPart(new ContainerLoadPart());
            crate.AddPart(new PublicCargoStoragePart());
            crate.GetPart<ExaminablePart>().Description = "The yard leaves its spare timber here for anyone who can use it. Take the loose stock, or haul the whole crate to dry ground nearer the road and keep it there as storage.";
        }

        bool CargoLane(Zone zone, Entity crate, bool west)
        {
            if (!Payload(crate, "Crate", allowPlaced: true) || crate.SpatialZone != zone
                || crate.HasPart<HandlingPart>() || crate.HasPart<ContainerLoadPart>() || crate.HasPart<PublicCargoStoragePart>()
                || crate.HasPart<InstallationMarkerPart>() || LocalPorterReservationPart.Blocks(crate)) return false;
            var source = zone.GetEntityCell(crate);
            if (source == null || !source.Objects.Contains(crate) || !DryLaneCell(zone, source, crate)) return false;

            int side = west ? 1 : -1;
            int berthY = west ? 15 : 13;
            int laneX = source.X + side;
            int berthX = laneX + side;
            var berth = (x: berthX, y: berthY);
            // The load follows each vacated cell. Verify the standing cell,
            // the straight pull and both cells of the turn past the berth.
            for (int y = source.Y; y >= berthY; y--)
                if (!DryLaneCell(zone, zone.GetCell(laneX, y))) return false;
            if (!DryLaneCell(zone, zone.GetCell(berthX, berthY))
                || !DryLaneCell(zone, zone.GetCell(berthX, berthY - 1)) || completed.IsApproach(berthX, berthY)
                || completed.IsReserved(berthX, berthY) || zone.GenReservedCells.Contains(berth)) return false;

            var current = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>());
            var clear = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity> { crate });
            if (!current.BorderReach[laneX, source.Y] || !clear.PreservesAgainst(current, new[] { berth })) return false;
            var reached = clear.Flood(new HashSet<(int,int)> { berth }, null, int.MaxValue, null);
            return reached[berthX, berthY - 1] && !zone.GetReadOnlyEntities().Any(owner =>
            {
                if (owner == crate || owner.GetPart<PhysicsPart>()?.Solid != true) return false;
                var cell = zone.GetEntityCell(owner);
                return cell != null && Adjacent(current.BorderReach, cell.X, cell.Y) && !Adjacent(reached, cell.X, cell.Y);
            });
        }

        bool DryLaneCell(Zone zone, Cell cell, Entity ignored = null)
            => cell != null && !cell.IsInterior && !completed.IsWet(cell.X, cell.Y)
                && zone.TileState.Get(cell.X, cell.Y)?.IsEmpty != false
                && cell.Objects.Count(e => e.BlueprintName == "Floor") == 1
                && cell.Occupants.All(e => e == ignored || Safe(e)
                    && (e.BlueprintName == "Floor" || e.BlueprintName == "Duckboard")
                    && SumpholdCompositionBuilder.Valid(e, e.BlueprintName));

        static bool HasLocalID(IEnumerable<Entity> owners, string id)
            => owners.Any(e => e.ID == id || e.GetPart<ContainerPart>()?.Contents.Any(item => item?.ID == id) == true
                || e.GetPart<InventoryPart>() is InventoryPart pack
                    && (pack.Objects.Any(item => item?.ID == id) || pack.EquippedItems.Values.Any(item => item?.ID == id)));

        static bool ValidTimber(Entity timber)
            => timber?.BlueprintName == "SalvagedTimber" && !string.IsNullOrEmpty(timber.ID) && timber.SpatialZone == null
                && timber.Tags.Count == 1 && timber.HasTag("Item")
                && timber.GetPart<StackerPart>() is { StackCount: 1, MaxStack: 20 }
                && timber.GetPart<PhysicsPart>() is { Takeable: true, Solid: false, Weight: 1, InInventory: null, Equipped: null }
                && timber.GetPart<CommercePart>()?.Value == 3 && timber.GetPart<PreparationRecipePart>()?.RecipeIds == "shape_haft"
                && timber.Parts.Count == 6 && timber.Parts.All(p => p?.ParentEntity == timber
                    && (p is RenderPart || p is PhysicsPart || p is StackerPart || p is CommercePart
                        || p is ExaminablePart || p is PreparationRecipePart));
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
        static bool Payload(Entity e,string bp,bool allowPlaced = false)
        {
            if(!SumpholdCompositionBuilder.Valid(e,bp)||!Safe(e)||string.IsNullOrEmpty(e.ID)||!allowPlaced&&e.SpatialZone!=null
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
