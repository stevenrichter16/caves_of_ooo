using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Persistent native owners and geometry for the settlement south of the Felling.</summary>
    public static class MorrowfastSceneRuntime
    {
        public const string ZoneID="Overworld.3.6.0", TerrainTag="MorrowfastAuthoredTerrain";
        public const int WorldX=3,WorldY=6;
        private const string StateId="morrowfast-scene-state",OwnerPrefix="morrowfast-owner:";
        public static MorrowfastSceneStatePart GetState(Zone zone)
        {var c=zone?.GetCell(0,0);if(c!=null)foreach(var e in c.Objects)if(e.ID==StateId)return e.GetPart<MorrowfastSceneStatePart>();return null;}
        public static bool IsActive(Zone zone)=>zone!=null&&zone.ZoneID==ZoneID&&GetState(zone)?.Revision==1&&MorrowfastSceneDefinition.Load()!=null;
        public static Entity FindOwner(Zone zone,string id)
        {
            if(zone==null||zone.ZoneID!=ZoneID||string.IsNullOrEmpty(id))return null;
            var s=GetState(zone);if(s==null||s.WasRemoved(id))return null;
            if(s.Owners==null)
            {
                s.Owners=new Dictionary<string,Entity>(StringComparer.Ordinal);
                foreach(var e in zone.GetReadOnlyEntities())
                {var p=e.GetPart<MorrowfastPropPart>();if(p!=null&&e.ID==OwnerPrefix+p.ComponentId&&!s.Owners.ContainsKey(p.ComponentId))s.Owners.Add(p.ComponentId,e);}
            }
            if(!s.Owners.TryGetValue(id,out var owner))return null;
            var c=zone.GetEntityCell(owner);return c!=null&&c.Objects.Contains(owner)?owner:null;
        }
        public static bool IsPresent(Zone zone,string id)=>IsActive(zone)&&FindOwner(zone,id)!=null;
        public static bool IsDoorOpen(Zone zone,string id)=>IsActive(zone)&&GetState(zone).DoorIsOpen(id);
        public static bool IsRoofLifted(Zone zone,string id)=>IsActive(zone)&&GetState(zone).RoofIsLifted(id);
        public static string GetRoomAt(Zone zone,int x,int y)=>IsActive(zone)?MorrowfastSceneDefinition.Load().RoomAt(x,y):null;
        public static Vector2Int GetAuthoredAnchor(string id)
        {var o=MorrowfastSceneDefinition.Load()?.FindOwner(id);if(o!=null)return new Vector2Int(o.anchorX,o.anchorY);foreach(var r in MorrowfastContent.AdditionalResidents)if(r.Id==id)return new Vector2Int(r.X,r.Y);return new Vector2Int(-1,-1);}
        public static bool IsValidActor(Entity actor,Zone zone)=>IsActive(zone)&&actor!=null&&actor.HasTag("Player")&&actor.GetStatValue("Hitpoints",0)>0&&zone.GetEntityCell(actor)?.Objects.Contains(actor)==true;
        public static bool WithinOwnerReach(Entity actor,Entity target,Zone zone)
        {
            if(actor==null||target==null||!IsActive(zone))return false;
            var a=zone.GetEntityCell(actor);var t=zone.GetEntityCell(target);var p=target.GetPart<MorrowfastPropPart>();
            if(a==null||t==null||p==null||FindOwner(zone,p.ComponentId)!=target)return false;
            var o=MorrowfastSceneDefinition.Load().FindOwner(p.ComponentId);
            if(o!=null&&o.kind!="npc"&&o.kind!="creature")
                foreach(var c in o.kind=="bridge"?o.bridgeSupport:o.footprint)if(Math.Abs(a.X-(c.x+t.X-o.anchorX))<=1&&Math.Abs(a.Y-(c.y+t.Y-o.anchorY))<=1)return true;
            return Math.Abs(a.X-t.X)<=1&&Math.Abs(a.Y-t.Y)<=1;
        }
        // Lists are built once and reused. Collision follows the live owner, never hidden proxy entities.
        public static Entity BlockingOwner(Cell cell,Entity ignoring=null,bool opaqueOnly=false)
        {
            var zone=cell?.ParentZone;if(!IsActive(zone))return null;
            var s=GetState(zone);
            if(s.Footprints==null)
            {
                s.Footprints=new List<MorrowfastSceneDefinition.OwnerSpec>[Zone.Width*Zone.Height];
                foreach(var o in MorrowfastSceneDefinition.Load().owners)
                {
                    if(o.kind=="npc"||o.kind=="creature")continue;
                    var points=o.kind=="bridge"?o.bridgeSupport:o.footprint;
                    foreach(var p in points){int k=p.y*Zone.Width+p.x;var l=s.Footprints[k]??(s.Footprints[k]=new List<MorrowfastSceneDefinition.OwnerSpec>());l.Add(o);}
                }
            }
            var list=s.Footprints[cell.Y*Zone.Width+cell.X];if(list==null)return null;
            foreach(var o in list)
            {
                var owner=FindOwner(zone,o.id);
                if(o.kind=="bridge")
                {if(owner==null&&!opaqueOnly)return s.ParentEntity;continue;}
                if(owner==null||owner==ignoring)continue;
                if(o.kind=="door"){if(!s.DoorIsOpen(o.id))return owner;continue;}
                if(opaqueOnly||!o.blocksMovement)continue;
                var at=zone.GetEntityPosition(owner);
                if(at==(o.anchorX,o.anchorY))return owner;
                // Moved furniture occupies its actual cell through PhysicsPart.
            }
            return null;
        }
        public static bool TryMoveFurniture(Entity actor,Zone zone,string id,int x,int y)
        {
            var owner=FindOwner(zone,id);var o=MorrowfastSceneDefinition.Load()?.FindOwner(id);
            if(o?.kind!="stool"||!IsValidActor(actor,zone)||owner==null||!WithinOwnerReach(actor,owner,zone))return false;
            var cell=zone.GetCell(x,y);var old=zone.GetEntityCell(owner);
            if(cell==null||old==cell||MorrowfastSceneDefinition.Load().RoomAt(x,y)!=o.roomId||cell.BlocksMovement(owner))return false;
            foreach(var e in cell.Objects)if(e!=owner&&!e.HasTag(TerrainTag))return false;
            if(!FurniturePreservesRoomAccess(zone,owner,o,x,y))return false;
            if(!zone.MoveEntity(owner,x,y))return false;
            owner.GetPart<PhysicsPart>().Solid=true;
            ZoneRenderHooks.MarkCellDirty(old.X,old.Y,"MorrowfastFurniture");ZoneRenderHooks.MarkCellDirty(x,y,"MorrowfastFurniture");return true;
        }
        // Prediction happens only for an explicit furniture action. No speculative world moves,
        // actor relocation, turn events or renderer mutations occur before the candidate is accepted.
        private static bool FurniturePreservesRoomAccess(Zone zone,Entity moving,MorrowfastSceneDefinition.OwnerSpec spec,int x,int y)
        {
            var definition=MorrowfastSceneDefinition.Load();MorrowfastSceneDefinition.BuildingSpec room=null;
            foreach(var b in definition.buildings)if(b.id==spec.roomId){room=b;break;}
            if(room==null)return false;
            var door=FindOwner(zone,room.doorId);var blockedBefore=new bool[Zone.Width,Zone.Height];
            for(int cx=0;cx<Zone.Width;cx++)for(int cy=0;cy<Zone.Height;cy++)
                blockedBefore[cx,cy]=FurniturePredictionBlocked(zone.GetCell(cx,cy),moving,door);
            var blockedAfter=(bool[,])blockedBefore.Clone();var old=zone.GetEntityPosition(moving);
            blockedBefore[old.x,old.y]=true;
            if(old==(spec.anchorX,spec.anchorY))foreach(var point in spec.footprint)blockedBefore[point.x,point.y]=true;
            blockedAfter[x,y]=true; // Moved stools use their actual single-cell Physics body.
            var before=FurnitureFlood(blockedBefore,room.entryX,room.entryY);
            var after=FurnitureFlood(blockedAfter,room.entryX,room.entryY);bool reachedRoom=false;
            foreach(var point in room.interior)
            {
                if(!before[point.x,point.y])continue;reachedRoom=true;
                if((point.x!=x||point.y!=y)&&!after[point.x,point.y])return false;
            }
            if(!reachedRoom)return false;
            foreach(var item in definition.owners)
            {
                if(item.roomId!=room.id)continue;var owner=FindOwner(zone,item.id);if(owner==null)continue;
                var position=zone.GetEntityPosition(owner);
                if(!FurnitureHasApproach(before,item,position.x,position.y))continue;
                if(owner==moving)position=(x,y);
                if(!FurnitureHasApproach(after,item,position.x,position.y))return false;
            }
            return true;
        }
        private static bool FurniturePredictionBlocked(Cell cell,Entity moving,Entity roomDoor)
        {
            // Predict with the house door open so closing it temporarily cannot hide a future trap.
            var authored=BlockingOwner(cell,moving);
            if(authored!=null&&authored!=roomDoor)return true;
            foreach(var item in cell.Objects)
            {
                if(item==moving||item==roomDoor||item.HasTag("Player")||item.HasTag("Creature")||item.HasPart<BrainPart>())continue;
                if(item.HasTag("Solid")||item.GetPart<PhysicsPart>()?.Solid==true||item.GetPart<SealedLibraryBarrierPart>()?.IsClosed==true)return true;
            }
            return false;
        }
        private static bool[,] FurnitureFlood(bool[,] blocked,int x,int y)
        {
            var seen=new bool[Zone.Width,Zone.Height];if(blocked[x,y])return seen;
            var queue=new Queue<Vector2Int>();seen[x,y]=true;queue.Enqueue(new Vector2Int(x,y));
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();
                FurnitureVisit(cell.x-1,cell.y,blocked,seen,queue);FurnitureVisit(cell.x+1,cell.y,blocked,seen,queue);
                FurnitureVisit(cell.x,cell.y-1,blocked,seen,queue);FurnitureVisit(cell.x,cell.y+1,blocked,seen,queue);
            }
            return seen;
        }
        private static void FurnitureVisit(int x,int y,bool[,] blocked,bool[,] seen,Queue<Vector2Int> queue)
        {if(!MorrowfastSceneDefinition.InBounds(x,y)||blocked[x,y]||seen[x,y])return;seen[x,y]=true;queue.Enqueue(new Vector2Int(x,y));}
        private static bool FurnitureHasApproach(bool[,] seen,MorrowfastSceneDefinition.OwnerSpec spec,int x,int y)
        {
            if(FurnitureAdjacentSeen(seen,x,y))return true;
            foreach(var point in spec.footprint)if(FurnitureAdjacentSeen(seen,point.x+x-spec.anchorX,point.y+y-spec.anchorY))return true;
            return false;
        }
        private static bool FurnitureAdjacentSeen(bool[,] seen,int x,int y)
            =>FurnitureSeen(seen,x-1,y)||FurnitureSeen(seen,x+1,y)||FurnitureSeen(seen,x,y-1)||FurnitureSeen(seen,x,y+1);
        private static bool FurnitureSeen(bool[,] seen,int x,int y)=>MorrowfastSceneDefinition.InBounds(x,y)&&seen[x,y];
        public static bool UpgradeCachedZone(Zone zone,EntityFactory factory)=>zone!=null&&zone.ZoneID==ZoneID&&Install(zone,factory,true);
        public static bool Install(Zone zone,EntityFactory factory,bool preserveExisting=false)
        {
            if(zone==null||zone.ZoneID!=ZoneID||factory==null)return false;
            var existing=GetState(zone);
            if(existing!=null){if(existing.Revision!=1)return false;MorrowfastContent.EnsureRegistered();RestoreInteriors(zone);NameTerrain(zone,MorrowfastSceneDefinition.Load());return MorrowfastFaunaPart.EnsureHabitat(zone);}
            var d=MorrowfastSceneDefinition.Load();if(d==null)return false;
            foreach(string bp in new[]{"TepuiStone","WaterPuddle","Creature","GlasspaneFrog","YellowfootWayfarer","Tepuibone","Mushroom","FireClay"})if(!factory.Blueprints.ContainsKey(bp))return false;
            var staged=new List<(Entity e,int x,int y)>(2080);var state=new MorrowfastSceneStatePart{Owners=new Dictionary<string,Entity>(StringComparer.Ordinal)};
            foreach(var c in d.cells)
            {
                var e=factory.CreateEntity(c.water?"WaterPuddle":"TepuiStone");if(e==null)return false;
                e.ID="morrowfast-terrain:"+c.x+":"+c.y;e.SetTag(TerrainTag);
                var p=e.GetPart<PhysicsPart>();p.Solid=c.solid;p.Takeable=false;
                if(c.solid)e.SetTag("Solid");else e.Tags.Remove("Solid");if(c.opaque)e.SetTag("Wall");else e.Tags.Remove("Wall");
                e.GetPart<RenderPart>().RenderLayer=0;NameTerrain(e,c.solid,c.opaque,c.interior);
                if(c.x==0&&c.y==0){e.ID=StateId;e.AddPart(state);}staged.Add((e,c.x,c.y));
            }
            foreach(var o in d.owners)
            {
                Entity e;
                if(o.kind=="npc")e=MorrowfastContent.CreateResident(o.id,factory);
                else if(o.kind=="creature")e=factory.CreateEntity(o.id=="western-bank-frog"?"GlasspaneFrog":"YellowfootWayfarer");
                else e=CreateProp(o,factory);
                if(e==null)return false;
                e.ID=OwnerPrefix+o.id;if(!e.HasPart<MorrowfastPropPart>())e.AddPart(new MorrowfastPropPart{ComponentId=o.id});
                if(!e.HasPart<ExaminablePart>())e.AddPart(new ExaminablePart{Text=o.description??o.name});
                if(o.kind=="creature")
                {
                    e.GetPart<RenderPart>().DisplayName=o.name;
                    // These are the settlement's peaceful pond and pack animals.
                    // Ordinary hostile Beasts elsewhere retain their faction.
                    e.SetTag("Faction", "Stillcord");
                    e.AddPart(new MorrowfastFaunaPart { ComponentId=o.id });
                }
                MorrowfastQuests.AttachProp(e,o.id);state.Owners.Add(o.id,e);staged.Add((e,o.anchorX,o.anchorY));
            }
            foreach(var r in MorrowfastContent.AdditionalResidents)
            {
                var e=MorrowfastContent.CreateResident(r.Id,factory);if(e==null)return false;
                e.ID=OwnerPrefix+r.Id;e.AddPart(new MorrowfastPropPart{ComponentId=r.Id});e.GetPart<RenderPart>().VisualID=r.Id=="farra-sprig"?"actor.morrowfast_farra":"actor.morrowfast_edden";
                state.Owners.Add(r.Id,e);staged.Add((e,r.X,r.Y));
            }
            var before=zone.GetAllEntities();
            foreach(var e in before)if(!preserveExisting||IsLegacyTerrain(e))zone.RemoveEntity(e);
            zone.GenReservedCells.Clear();
            foreach(var v in staged)
            {
                zone.AddEntity(v.e,v.x,v.y);zone.GenReservedCells.Add((v.x,v.y));
                var b=v.e.GetPart<BrainPart>();if(b!=null){b.CurrentZone=zone;b.StartingCellX=v.x;b.StartingCellY=v.y;b.Rng=new System.Random(7183+v.x*97+v.y);}
            }
            RestoreInteriors(zone);
            if(preserveExisting)foreach(var e in before)
            {var c=zone.GetEntityCell(e);if(c!=null&&(e.HasTag("Player")||e.HasTag("Creature"))&&c.BlocksMovement(e)){var p=FindArrival(zone,c.X,c.Y,e);if(p.x>=0)zone.MoveEntity(e,p.x,p.y);}}
            return true;
        }
        // B.2 (Docs/BREADTH-PASS.md): Morrowfast stands on the Grovelands' edge (tier 3), not on the Stump.
        // Its land cells stay TepuiStone for the voxel mapping, but they read as what they show:
        // grass outdoors, pale flagstones indoors, pale stone where a wall stands. Applied on fresh
        // install and on every upgrade of a cached zone, so older saves read the same.
        public const string GroundName="grove turf",FloorName="flagstone floor",WallName="house wall";
        public const string GroundText="Short grass over hard-packed ground, worn to bare earth along the lanes between the houses.";
        public const string FloorText="Pale flagstones, swept, a little warmer than the ground outside.";
        public const string WallText="Pale stone, laid thick against the weather.";
        private static void NameTerrain(Entity e,bool solid,bool opaque,bool interior)
        {
            if(e==null||e.BlueprintName!="TepuiStone")return;
            bool wall=solid&&opaque;
            var r=e.GetPart<RenderPart>();if(r!=null)r.DisplayName=wall?WallName:interior?FloorName:GroundName;
            var x=e.GetPart<ExaminablePart>();if(x!=null)x.Text=wall?WallText:interior?FloorText:GroundText;
        }
        /// <summary>Name every authored land cell of a Morrowfast zone; returns how many were named.</summary>
        public static int NameTerrain(Zone zone,MorrowfastSceneDefinition d)
        {
            if(zone==null||d?.cells==null)return 0;int n=0;
            foreach(var c in d.cells)
            {
                if(c.water)continue;var cell=zone.GetCell(c.x,c.y);if(cell==null)continue;
                for(int i=0;i<cell.Objects.Count;i++){var o=cell.Objects[i];if(o.HasTag(TerrainTag)&&o.BlueprintName=="TepuiStone"){NameTerrain(o,c.solid,c.opaque,c.interior);n++;}}
            }
            return n;
        }
        private static Entity CreateProp(MorrowfastSceneDefinition.OwnerSpec o,EntityFactory factory)
        {
            var e=new Entity{BlueprintName="MorrowfastSceneProp"};
            e.AddPart(new RenderPart{DisplayName=o.name,RenderString=o.kind=="door"?"+":"*",ColorString="&y",RenderLayer=3});
            e.AddPart(new PhysicsPart{Solid=false,Takeable=false});e.AddPart(new ExaminablePart{Text=string.IsNullOrEmpty(o.description)?o.name:o.description});
            if(o.kind=="door")e.AddPart(new MorrowfastDoorPart{ComponentId=o.id});
            if(o.kind=="container")
            {
                var box=new ContainerPart();e.AddPart(box);
                if(o.id=="keeper-supply-chest")box.AddItem(factory.CreateEntity("FireClay"));
                if(o.id.StartsWith("kitchen-produce-barrel",StringComparison.Ordinal))box.AddItem(factory.CreateEntity("Mushroom"));
                if(o.id=="western-shop-crate")box.AddItem(factory.CreateEntity("Tepuibone"));
            }
            if(o.kind=="cistern")e.AddPart(new WellPart());
            if(o.kind=="bed")e.AddPart(new BedPart());
            if(o.kind=="stool"||o.kind=="bench")e.AddPart(new ChairPart());
            if(o.kind=="hearth"||o.kind=="oven"||o.id=="inn-outdoor-stove")e.AddPart(new CampfirePart());
            return e;
        }
        private static bool IsLegacyTerrain(Entity e)=>e.HasTag(TerrainTag)||e.BlueprintName=="TepuiStone"||e.BlueprintName=="WaterPuddle"||e.HasTag("Terrain")
            ||e.BlueprintName=="VineWall"||e.BlueprintName=="Tree"||e.BlueprintName=="MycelialColumn";
        private static void RestoreInteriors(Zone zone)
        {
            foreach(var c in MorrowfastSceneDefinition.Load().cells)zone.GetCell(c.x,c.y).IsInterior=c.interior;
            foreach(var extra in MorrowfastContent.AdditionalResidents)
            {
                var render=FindOwner(zone,extra.Id)?.GetPart<RenderPart>();
                if(render!=null)render.VisualID=extra.Id=="farra-sprig"?"actor.morrowfast_farra":"actor.morrowfast_edden";
            }
            foreach(var b in MorrowfastSceneDefinition.Load().buildings)
            {
                var door=FindOwner(zone,b.doorId)?.GetPart<MorrowfastDoorPart>();
                if(door!=null)door.IsOpen=GetState(zone).DoorIsOpen(b.doorId);
            }
        }
        private static (int x,int y) FindArrival(Zone z,int x,int y,Entity e)
        {for(int r=1;r<Zone.Width;r++)for(int dy=-r;dy<=r;dy++)for(int dx=-r;dx<=r;dx++){if(Math.Abs(dx)!=r&&Math.Abs(dy)!=r)continue;var c=z.GetCell(x+dx,y+dy);if(c!=null&&!c.BlocksMovement(e))return(c.X,c.Y);}return(-1,-1);}
        public static void RegisterActiveActors(Zone zone,TurnManager turns)
        {if(turns==null||!IsActive(zone))return;foreach(var e in zone.GetReadOnlyEntities())if(e.HasPart<MorrowfastPropPart>()&&e.HasPart<BrainPart>()&&e.GetStatValue("Hitpoints")>0)turns.AddEntity(e);}
        public static void MarkOwnerDirty(Zone zone,string id)
        {
            var o=MorrowfastSceneDefinition.Load()?.FindOwner(id);if(o==null)return;
            ZoneRenderHooks.MarkCellDirty(o.anchorX,o.anchorY,"MorrowfastOwner");
            foreach(var p in o.footprint)ZoneRenderHooks.MarkCellDirty(p.x,p.y,"MorrowfastOwner");
            foreach(var p in o.bridgeSupport)ZoneRenderHooks.MarkCellDirty(p.x,p.y,"MorrowfastBridge");
        }
    }
}
