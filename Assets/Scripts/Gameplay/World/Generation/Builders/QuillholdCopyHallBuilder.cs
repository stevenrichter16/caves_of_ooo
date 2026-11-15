using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Complete two existing copying bays with three optional ordinary seats.
    /// Runs after population/stock so existing chair ownership and service placement win.
    /// Never changes the base plan or retrofits cached rooms.</summary>
    public sealed class QuillholdCopyHallBuilder : IZoneBuilder
    {
        public string Name=>"QuillholdCopyHall";
        public int Priority=>4600;
        readonly OverworldZoneManager manager;
        readonly WorldMap map;
        readonly EntityFactory sourceFactory;
        readonly QuillholdCompositionBuilder terrain;
        QuillholdCompositionPlan completed;
        public QuillholdCopyHallBuilder(OverworldZoneManager manager,QuillholdCompositionBuilder terrain)
        {this.manager=manager;map=manager?.WorldMap;sourceFactory=manager?.Factory;this.terrain=terrain;}

        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            var plan=terrain?.Plan;
            bool Current()=>zone!=null&&manager!=null&&plan!=null&&rng!=null
                &&QuillholdCompositionPlan.IsSupportedZone(zone.ZoneID)
                &&map?.Tiles!=null&&map.POIs!=null&&ReferenceEquals(map,manager.WorldMap)
                &&map.Tiles.GetLength(0)==WorldMap.Width&&map.Tiles.GetLength(1)==WorldMap.Height
                &&map.POIs.GetLength(0)==WorldMap.Width&&map.POIs.GetLength(1)==WorldMap.Height
                &&map.Tiles[14,9]==BiomeType.Spread&&AreaCompositionScope.IsQuillholdSite(map.GetPOI(14,9))
                &&ReferenceEquals(sourceFactory,factory)&&factory?.Blueprints!=null&&ReferenceEquals(factory,manager.Factory)
                &&ReferenceEquals(terrain.SourceFactory,factory)&&terrain.Seed==manager.WorldSeed
                &&ReferenceEquals(terrain.RealizedZone,zone)&&ReferenceEquals(terrain.Plan,plan)&&terrain.ProfileRealized
                &&!manager.CachedZones.ContainsKey(zone.ZoneID)&&!ReferenceEquals(manager.ActiveZone,zone)
                &&!LairStacks.HasSavedColumn(manager,zone.ZoneID);
            if(!Current()||ReferenceEquals(completed,plan))return true;
            completed=plan;
            var room=plan.Rooms.Single(r=>r.Role=="CopyHall");
            var desks=new List<(int x,int y,Entity owner)>();
            var seats=new List<(int x,int y)>();
            for(int y=room.Y+1;y<room.Y+room.Height-1;y++)for(int x=room.X+1;x<room.X+room.Width-1;x++)
            {
                if(plan.ObjectAt(x,y)!="QuillholdCopyDesk")continue;
                var owners=zone.GetCell(x,y).Objects.Where(e=>QuillholdCompositionBuilder.Valid(e,"QuillholdCopyDesk")).ToArray();
                if(owners.Length!=1)return true;
                desks.Add((x,y,owners[0]));
                foreach(int dy in new[]{-1,1})if(plan.ObjectAt(x,y+dy)==null)seats.Add((x,y+dy));
            }
            // Both desk anchors and the original fourth seat remain fixed. A blocked
            // candidate refuses the coherent group rather than moving any old owner.
            if(desks.Count!=2||seats.Count!=3||!seats.All(p=>Fits(zone,plan,p.x,p.y)))return true;
            var geometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            if(!seats.All(p=>geometry.BorderReach[p.x,p.y]))return true;
            var originals=zone.GetReadOnlyEntities().ToArray();
            var intact=SpreadGenerationReceipt.CaptureFinalState(zone,originals);
            string tiles=zone.TileState.ToSaveString();var reserves=new HashSet<(int,int)>(zone.GenReservedCells);
            bool Preserved()=>Current()&&intact()&&tiles==zone.TileState.ToSaveString()&&reserves.SetEquals(zone.GenReservedCells);
            var ids=new HashSet<string>(originals.Select(e=>e.ID),StringComparer.Ordinal);
            var staged=new List<Entity>();
            if(!factory.Blueprints.ContainsKey("Chair"))return true;
            foreach(var p in seats)
            {
                var chair=QuillholdCompositionBuilder.Create(factory,"Chair");
                if(!Preserved())return false;
                if(!Payload(chair,"Chair")||!ids.Add(chair.ID))return true;
                staged.Add(chair);
            }
            if(!Preserved()||!seats.All(p=>Fits(zone,plan,p.x,p.y))
                ||desks.Any(d=>!zone.GetCell(d.x,d.y).Objects.Contains(d.owner)||!QuillholdCompositionBuilder.Valid(d.owner,"QuillholdCopyDesk")))return true;
            var added=new List<Entity>();
            for(int i=0;i<seats.Count;i++)
            {
                if(!Payload(staged[i],"Chair")||!zone.AddEntity(staged[i],seats[i].x,seats[i].y))break;
                added.Add(staged[i]);
            }
            if(added.Count!=seats.Count||!Preserved())foreach(var e in added)zone.RemoveEntity(e);
            return true;
        }
        static bool Fits(Zone zone,QuillholdCompositionPlan plan,int x,int y)
        {
            var c=zone.GetCell(x,y);
            if(c==null||!c.IsInterior||!plan.IsInterior(x,y)||plan.IsApproach(x,y)||plan.IsReserved(x,y)
                ||zone.GenReservedCells.Contains((x,y))||plan.ObjectAt(x,y)!=null||c.Objects.Count!=1||c.Occupants.Count()!=1
                ||zone.TileState.Get(x,y)?.IsEmpty==false)return false;
            return QuillholdCompositionBuilder.Valid(c.Objects[0],"StoneFloor")&&Safe(c.Objects[0]);
        }
        static bool Safe(Entity e) => !e.HasTag("Barren")&&!e.HasTag("Creature")&&!e.HasTag("Item")
            &&!e.HasPart<BrainPart>()&&!e.HasPart<SpatialFootprintPart>()&&!e.HasPart<TileStateSourcePart>()
            &&!e.HasPart<LiquidPoolPart>()&&!e.HasPart<GasPoolPart>()&&!e.HasPart<TriggerOnStepPart>()
            &&!e.HasPart<CropPart>()&&!e.HasPart<CultivatedSoilPart>()&&!e.HasPart<FieldHarvestPart>()&&!e.HasPart<HarvestablePart>()
            &&!e.HasPart<InventoryPart>()&&!e.HasPart<TraderPart>()&&!e.HasPart<ConversationPart>()&&!e.HasPart<ThrowingStatePart>()
            &&!e.HasPart<DoorPart>()&&!e.HasPart<SealedLibraryBarrierPart>()&&!e.HasPart<StairsUpPart>()&&!e.HasPart<StairsDownPart>();
        static bool Payload(Entity e,string bp)
        {
            if(!QuillholdCompositionBuilder.Valid(e,bp)||!Safe(e)||string.IsNullOrEmpty(e.ID)||e.SpatialZone!=null
                ||e.Parts.Any(p=>p==null||p.ParentEntity!=e)
                ||e.Parts.Count(p=>p is ChairPart)!=1||e.Parts.Count(p=>p is RenderPart)!=1
                ||e.Parts.Count(p=>p is PhysicsPart)!=1||e.Parts.Count(p=>p is ExaminablePart)!=1
                ||e.Parts.Count(p=>p is StatusEffectsPart)>1||e.Parts.Count(p=>p is StackerPart)>1
                ||(e.GetPart<StackerPart>()?.StackCount??1)<=0)return false;
            var render=e.GetPart<RenderPart>();
            if(render.RenderLayer!=5||render.RenderString!="h"
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants)
                ||e.HasTag("Player")||e.HasTag("Terrain")||e.HasTag("Crop")||e.HasTag("Plantable")||e.HasTag("Restable")
                ||e.HasTag("WellGroundMarker")||e.HasTag("OvenGroundMarker")||e.HasTag("LanternGroundMarker")
                ||e.HasPart<Body>()||e.GetStat("Hitpoints")!=null||e.HasPart<WellPart>()||e.HasPart<RepairablePart>()
                ||e.HasPart<RegionalSignpostPart>()||e.HasPart<CampfirePart>()||e.HasPart<AlchemyStillPart>()
                ||e.HasPart<SoddenPreparationPart>()||e.HasPart<KitchenBatchPart>()||e.HasPart<ForgePart>()
                ||e.HasPart<FiniteLiquidFilmPart>()||e.HasPart<BurnOffGasPart>()||e.HasPart("Restable")||e.HasPart<LockPart>())return false;
            var physics=e.GetPart<PhysicsPart>();
            if(physics.InInventory!=null||physics.Equipped!=null)return false;
            if(e.HasPart<ContainerPart>()||!e.HasTag("Furniture")||e.HasPart<MaterialPart>()||e.HasPart<DestructiblePart>()||e.HasPart<ThermalPart>())return false;
            var seat=e.GetPart<ChairPart>();
            return !e.HasPart<BedPart>()&&seat!=null&&string.IsNullOrEmpty(seat.Owner)&&!seat.Occupied&&seat.Occupant==null;
        }
    }
}
