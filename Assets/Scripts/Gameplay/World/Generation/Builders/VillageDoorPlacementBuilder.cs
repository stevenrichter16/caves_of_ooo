using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Place only initially-open doors in surviving apertures recorded
    /// by this exact ordinary VillageBuilder run. Authored compositions have no
    /// source and retain their own architecture and permissions.</summary>
    public sealed class VillageDoorPlacementBuilder : IZoneBuilder
    {
        public string Name=>"VillageDoorPlacementBuilder";
        public int Priority=>3900;
        readonly VillageBuilder source;
        public VillageDoorPlacementBuilder(VillageBuilder source)=>this.source=source;
        public bool BuildZone(Zone zone,EntityFactory factory,System.Random rng)
        {
            if(zone==null||source==null||!ReferenceEquals(source.ApertureSourceZone,zone)
                ||factory?.Blueprints?.ContainsKey("VillageDoor")!=true)return true;
            int placed=0,refused=0,revision=source.ApertureRevision;
            var recorded=new System.Collections.Generic.List<VillageBuilder.DoorAperture>(source.DoorApertures);
            foreach(var aperture in recorded)
            {
                if(!Valid(zone,aperture)){refused++;continue;}
                var owner=factory.CreateEntity("VillageDoor");
                if(source.ApertureRevision!=revision||!ReferenceEquals(source.ApertureSourceZone,zone)){refused++;break;}
                var part=owner?.GetPart<DoorPart>();var physics=owner?.GetPart<PhysicsPart>();
                // Factory initialization may invoke callbacks. Revalidate source,
                // local geometry, ownership and shape before committing anything.
                if(!ReferenceEquals(source.ApertureSourceZone,zone)||!Valid(zone,aperture)
                    ||owner==null||owner.SpatialZone!=null||owner.BlueprintName!="VillageDoor"
                    ||part==null||part.ParentEntity!=owner||!part.IsOpen||part.IsClosed||!string.IsNullOrEmpty(part.OwnerId)
                    ||physics==null||physics.Solid||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                    ||owner.HasTag("Creature")||owner.HasTag("Solid")||owner.HasTag("Item")
                    ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MorrowfastDoorPart>()||owner.HasPart<SealedLibraryBarrierPart>()
                    ){refused++;continue;}
                part.QuarterTurns=aperture.InDX!=0?1:0;
                if(!zone.AddEntity(owner,aperture.X,aperture.Y)){refused++;continue;}
                zone.GenReservedCells.Add((aperture.X,aperture.Y));
                zone.GenReservedCells.Add((aperture.X+aperture.InDX,aperture.Y+aperture.InDY));
                zone.GenReservedCells.Add((aperture.X-aperture.InDX,aperture.Y-aperture.InDY));
                placed++;
            }
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","VillageDoorsPlaced",payload:new{zone=zone.ZoneID,recorded=recorded.Count,placed,refused});
            return true;
        }
        static bool Valid(Zone zone,VillageBuilder.DoorAperture p)
        {
            var center=zone.GetCell(p.X,p.Y);var inside=zone.GetCell(p.X+p.InDX,p.Y+p.InDY);var outside=zone.GetCell(p.X-p.InDX,p.Y-p.InDY);
            var a=zone.GetCell(p.X-p.InDY,p.Y+p.InDX);var b=zone.GetCell(p.X+p.InDY,p.Y-p.InDX);
            if(center==null||inside==null||outside==null||a==null||b==null||!inside.IsInterior||outside.IsInterior
                ||zone.GetEntityCell(p.FlankA)!=a||zone.GetEntityCell(p.FlankB)!=b||!a.IsWall()||!b.IsWall())return false;
            return Clear(zone,center)&&Clear(zone,inside)&&Clear(zone,outside);
        }
        static bool Clear(Zone zone,Cell cell)
        {
            if(cell.BlocksMovement()||zone.GenReservedCells.Contains((cell.X,cell.Y))||zone.TileState.Heat(cell.X,cell.Y)>0)return false;
            var state=zone.TileState.Get(cell.X,cell.Y);
            if(state?.Coatings!=null)foreach(var coating in state.Coatings)if(coating.Turns>0)return false;
            foreach(var owner in cell.Occupants)
                if(!DoorPart.IsBareGround(owner))return false;
            return true;
        }
    }
}
