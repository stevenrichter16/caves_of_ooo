using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A small real stone apron for a newly installed archive. Never repairs cached
    /// towns, moves furniture, or changes the reserved doorway and its immediate neighbors.</summary>
    internal static class SaltVaultArchiveApproach
    {
        static readonly (int x, int y)[] Four = { (0,-1), (-1,0), (1,0), (0,1) };
        sealed class Plan
        {
            internal List<(int x,int y,Entity old)> Cells;
            internal Entity File, Indexer, Door;
            internal WorldMap Map;
            internal EntityFactory Factory;
            internal HashSet<(int,int)> Room;
            internal int DoorX,DoorY;
        }
        internal static bool TryApply(Zone zone, OverworldZoneManager manager)
        {
            if(!TryPlan(zone,manager,out var before)||!before.Factory.Blueprints.ContainsKey("StoneFloor"))return false;
            var floors=new List<Entity>();var ids=new HashSet<string>();
            try
            {
                foreach(var c in before.Cells)
                {var floor=before.Factory.CreateEntity("StoneFloor");if(!PlainGround(floor,"StoneFloor"))return false;floors.Add(floor);}
            }
            catch(Exception ex)
            {
                // Optional presentation must not abort fresh zone access. This
                // refuses our paving; it cannot undo arbitrary callback effects.
                Diag.Record("worldgen","SaltVaultApproachRefused",payload:new { zone=zone.ZoneID,reason="floor_creation_failed",error=ex.GetType().Name });
                return false;
            }
            // ObjectCreated handlers can change the source graph. Re-derive the entire
            // authority and doorway after staging, before removing any old ground.
            if(!TryPlan(zone,manager,out var after)||!ReferenceEquals(before.Map,after.Map)
                ||!ReferenceEquals(before.Factory,after.Factory)||!ReferenceEquals(before.File,after.File)
                ||!ReferenceEquals(before.Indexer,after.Indexer)||!ReferenceEquals(before.Door,after.Door)
                ||before.DoorX!=after.DoorX||before.DoorY!=after.DoorY||!before.Room.SetEquals(after.Room)
                ||before.Cells.Count!=after.Cells.Count)return false;
            var cells=before.Cells;
            for(int i=0;i<cells.Count;i++)
                if(cells[i].x!=after.Cells[i].x||cells[i].y!=after.Cells[i].y||!ReferenceEquals(cells[i].old,after.Cells[i].old)
                    ||!PlainGround(floors[i],"StoneFloor")||floors[i].SpatialZone!=null
                    ||string.IsNullOrEmpty(floors[i].ID)||!ids.Add(floors[i].ID))return false;
            int done=0;
            for(int i=0;i<cells.Count;i++)
            {
                var c=cells[i];
                if(!zone.RemoveEntity(c.old))break;
                if(!zone.AddEntity(floors[i],c.x,c.y)){zone.AddEntity(c.old,c.x,c.y);break;}
                done++;
            }
            if(done==cells.Count)return true;
            for(int i=0;i<done;i++){zone.RemoveEntity(floors[i]);zone.AddEntity(cells[i].old,cells[i].x,cells[i].y);}
            return false;
        }
        static bool TryPlan(Zone zone, OverworldZoneManager manager, out Plan plan)
        {
            plan=null;
            if (zone?.ZoneID != StillleafSaltVault.ZoneId || manager?.Factory == null
                || manager.CachedZones.ContainsKey(zone.ZoneID)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(15,15);
            if (map.GetBiome(15,15) != BiomeType.Beating || poi == null || poi.Type != POIType.Village
                || poi.Name != "the Salt-Vault" || poi.Faction != "PaleCuration" || !string.IsNullOrEmpty(poi.Profile)
                || LairStacks.HasSavedColumn(manager,zone.ZoneID)) return false;
            Entity file = null, indexer = null;
            foreach (var e in zone.GetReadOnlyEntities())
            {
                if (e.ID == StillleafSaltVault.CabinetId) { if (file != null) return false; file = e; }
                if (e.ID == StillleafSaltVault.IndexerId) { if (indexer != null) return false; indexer = e; }
            }
            if (file?.BlueprintName != StillleafSaltVault.CabinetBlueprint || indexer?.BlueprintName != StillleafSaltVault.IndexerBlueprint
                || file.GetPart<ContainerPart>()?.Locked != true) return false;
            var at = zone.GetEntityCell(file); var ic = zone.GetEntityCell(indexer);
            if (at == null || ic == null || Math.Abs(at.X-ic.X)+Math.Abs(at.Y-ic.Y) != 1 || !Interior(zone,at.X,at.Y)) return false;
            var room = new HashSet<(int,int)>(); var queue = new Queue<(int x,int y)>();
            room.Add((at.X,at.Y)); queue.Enqueue((at.X,at.Y));
            while (queue.Count > 0)
            {
                var c=queue.Dequeue(); foreach(var d in Four)
                { var n=(c.x+d.x,c.y+d.y); if(Interior(zone,n.Item1,n.Item2)&&room.Add(n)) queue.Enqueue(n); }
            }
            if (!room.Contains((ic.X,ic.Y))) return false;
            var doors = new List<(int x,int y,int dx,int dy,int distance,Entity owner)>();
            foreach (var e in zone.GetReadOnlyEntities())
            {
                if (e.BlueprintName != "VillageDoor" || !e.HasPart<DoorPart>()) continue;
                var c=zone.GetEntityCell(e); if(c==null) continue;
                foreach(var d in Four)
                {
                    var outside=zone.GetCell(c.X+d.x,c.Y+d.y);
                    if(room.Contains((c.X-d.x,c.Y-d.y)) && outside!=null && !outside.IsInterior
                        && Wall(zone,c.X-d.y,c.Y+d.x) && Wall(zone,c.X+d.y,c.Y-d.x))
                        doors.Add((c.X,c.Y,d.x,d.y,Math.Abs(c.X-at.X)+Math.Abs(c.Y-at.Y),e));
                }
            }
            if(doors.Count==0) return false;
            doors.Sort((a,b)=>a.distance!=b.distance?a.distance.CompareTo(b.distance):a.y!=b.y?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            var door=doors[0]; var cells=new List<(int x,int y,Entity old)>();
            for(int distance=2;distance<=3;distance++) for(int side=-1;side<=1;side++)
            {
                int x=door.x+door.dx*distance-door.dy*side, y=door.y+door.dy*distance+door.dx*side;
                var sand=BareSand(zone,x,y); if(sand==null) return false; cells.Add((x,y,sand));
            }
            for(int distance=4;distance<=6;distance++)
            {
                int x=door.x+door.dx*distance,y=door.y+door.dy*distance;
                var sand=BareSand(zone,x,y); if(sand==null) break; cells.Add((x,y,sand));
            }
            plan=new Plan { Cells=cells, File=file, Indexer=indexer, Door=door.owner, Map=map,
                Factory=manager.Factory, Room=room, DoorX=door.x, DoorY=door.y };
            return true;
        }
        static bool Interior(Zone z,int x,int y)
        {var c=z.GetCell(x,y);if(c?.IsInterior!=true)return false;foreach(var e in c.Objects)if(e.BlueprintName=="StoneFloor"&&ReferenceEquals(z.GetEntityCell(e),c))return true;return false;}
        static bool Wall(Zone z,int x,int y)
        {var c=z.GetCell(x,y);if(c==null)return false;foreach(var e in c.Objects)if(e.BlueprintName=="SandstoneWall"&&e.GetPart<PhysicsPart>()?.Solid==true)return true;return false;}
        static Entity BareSand(Zone z,int x,int y)
        {
            var c=z.GetCell(x,y);if(c==null||c.IsInterior||z.GenReservedCells.Contains((x,y))||c.Occupants.Count!=1)return null;
            var state=z.TileState.Get(x,y);if(state!=null&&!state.IsEmpty)return null;
            var e=c.Occupants[0];return ReferenceEquals(z.GetEntityCell(e),c)&&PlainGround(e,"Sand")?e:null;
        }
        static bool PlainGround(Entity e,string name)
        {
            if(e?.BlueprintName!=name||e.Parts.Count!=3||e.Tags.Count!=1||!e.HasTag("Terrain")
                ||e.Properties.Count!=0||e.IntProperties.Count!=0||e.Statistics.Count!=0)return false;
            var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();var x=e.GetPart<ExaminablePart>();
            return r!=null&&p!=null&&x!=null&&r.ParentEntity==e&&p.ParentEntity==e&&x.ParentEntity==e
                &&r.DisplayName==(name=="Sand"?"sand":"stone floor")
                &&x.Text==(name=="Sand"?"Fine grains collect in the shallow hollows; the higher ridges have been swept nearly smooth.":"The exposed stone has been worn smoother along the places feet keep finding.")
                &&string.IsNullOrEmpty(r.Tile)&&r.VisualFacing==EntityVisualFacing.South
                &&r.Visible&&r.RenderLayer==0&&r.RenderString=="."&&r.GlyphVariants=="...,.'`"
                &&r.ColorString==(name=="Sand"?"&W":"&y")&&(name=="Sand"?r.BackgroundColor=="^w":string.IsNullOrEmpty(r.BackgroundColor))
                &&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)&&string.IsNullOrEmpty(r.DetailColor)&&string.IsNullOrEmpty(r.TileColor)
                &&!p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null;
        }
    }
}
