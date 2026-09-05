using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Stillleaf's sealed archive, after stair placement. The
    /// enclosure avoids existing stairs; exterior approaches never cut it.
    /// Its key and readable contents belong to the later quest arc.</summary>
    public sealed class SealedLibraryBuilder : IZoneBuilder
    {
        public string Name => "SealedLibrary";
        public int Priority => 3650;
        public const string SiteName = "Stillleaf";
        public const string ZoneID = "Overworld.2.4.2";
        public const string KeyID = "coo.sealed-library.stillleaf";
        private const int Span = 23, Top = 8, Bottom = 16, DoorY = 12;
        private static readonly string[] Required = { "StoneFloor", "SealedLibraryFloor", "SealedLibraryDoor",
            "LibraryTepuiboneWall", "LibraryMemoryMarbleWall", "LibraryChoirIronWall", "SealedArchiveShelf" };
        private static readonly (int x, int y)[] Steps = { (0,-1), (0,1), (-1,0), (1,0) };

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null) return false;
            foreach (string bp in Required) if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone,"missing:"+bp);
            foreach (var e in zone.GetReadOnlyEntities()) if (e.BlueprintName == "SealedLibraryDoor") return true;
            var stairs = new List<(int x, int y)>();
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>()) stairs.Add(zone.GetEntityPosition(e));
            int left = -1;
            // Disjoint anchors guarantee room for the normal pair of stairs.
            foreach (int candidate in new[] { 28, 2, 54 })
            {
                bool conflict = false;
                foreach (var p in stairs) if (p.x >= candidate && p.x <= candidate + Span && p.y >= Top && p.y <= Bottom)
                { conflict = true; break; }
                if (!conflict) { left = candidate; break; }
            }
            if (left < 0) return Refuse(zone,"no_stair_free_anchor");
            int right = left + Span;
            bool Inside(int x,int y) => x >= left && x <= right && y >= Top && y <= Bottom;
            var floors = new HashSet<(int x,int y)>();
            // One-cell exterior aisle; library floor is a separate persistent
            // arrival exclusion, not a barrier to ordinary walking.
            for (int x=left-1;x<=right+1;x++) for(int y=Top-1;y<=Bottom+1;y++) floors.Add((x,y));
            var starts = new List<(int x,int y)>(stairs) { (0,12), (79,12), (40,0), (40,24) };
            foreach (var start in starts)
            {
                // Carve a shortest exterior approach irrespective of random
                // cave walls, without ever routing through the sealed box.
                var q = new Queue<(int x,int y)>(); var prev = new Dictionary<(int x,int y),(int x,int y)>();
                q.Enqueue(start);prev[start]=start; var goal=(left-1,DoorY);
                while(q.Count>0 && !prev.ContainsKey(goal))
                {
                    var p=q.Dequeue();foreach(var d in Steps)
                    {
                        var n=(p.x+d.x,p.y+d.y);
                        if(!zone.InBounds(n.Item1,n.Item2)||Inside(n.Item1,n.Item2)||prev.ContainsKey(n))continue;
                        prev[n]=p;q.Enqueue(n);
                    }
                }
                if(!prev.ContainsKey(goal))return Refuse(zone,"unreachable_exterior");
                for(var p=goal;;p=prev[p]) { floors.Add(p);if(p==start)break; }
            }
            // Stage every factory result before clearing any cells. Fail-soft
            // blueprint creation cannot leave a half-built enclosure.
            var staged=new List<(Entity e,int x,int y)>();
            bool Stage(string bp,int x,int y)
            {var e=factory.CreateEntity(bp);if(e==null)return false;staged.Add((e,x,y));return true;}
            foreach(var p in floors)
            {
                bool interior=p.x>left&&p.x<right&&p.y>Top&&p.y<Bottom;
                if(!Stage(interior?"SealedLibraryFloor":"StoneFloor",p.x,p.y))return Refuse(zone,"floor_creation");
                if(Inside(p.x,p.y)&&!interior)
                {
                    string bp=p.x==left&&p.y==DoorY?"SealedLibraryDoor":
                        p.y==Top||p.y==Bottom?"LibraryTepuiboneWall":
                        p.y%2==0?"LibraryChoirIronWall":"LibraryMemoryMarbleWall";
                    if(!Stage(bp,p.x,p.y))return Refuse(zone,"barrier_creation");
                }
            }
            // Sealed bundles, not random loot containers. Keep a broad center aisle.
            for(int x=left+3;x<right-2;x+=3)foreach(int y in new[]{Top+2,Bottom-2})
                if(!Stage("SealedArchiveShelf",x,y))return Refuse(zone,"shelf_creation");
            foreach(var p in floors)
            {
                var cell=zone.GetCell(p.x,p.y);
                for(int i=cell.Objects.Count-1;i>=0;i--)
                    if(!cell.Objects[i].HasPart<StairsUpPart>()&&!cell.Objects[i].HasPart<StairsDownPart>())zone.RemoveEntity(cell.Objects[i]);
                zone.GenReservedCells.Add(p);
            }
            foreach(var p in staged)zone.AddEntity(p.e,p.x,p.y);
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SealedLibraryBuilt",payload:new { zone=zone.ZoneID,left,right,top=Top,bottom=Bottom,stairs=stairs.Count });
            return true;
        }
        private static bool Refuse(Zone zone,string reason)
        {
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SealedLibraryRefused",payload:new { zone=zone.ZoneID,reason });
            return false;
        }
    }
}
