using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Cold-generation protection for existing underground travel paths.
    /// Reserves a finite BFS tree after stairs/connectivity and before scenery,
    /// hazards, population and containers. It never carves or moves an owner.</summary>
    public sealed class UndergroundRouteReservationBuilder : IZoneBuilder
    {
        public string Name=>"UndergroundRouteReservation";
        public int Priority=>3700;
        public bool BuildZone(Zone zone,EntityFactory factory,System.Random rng)
        {
            if(zone==null)return Reject(zone,"missing-zone");
            var stairs=new List<Cell>();
            foreach(var entity in zone.GetReadOnlyEntities())
                if(entity.HasPart<StairsUpPart>()||entity.HasPart<StairsDownPart>())
                    foreach(var cell in zone.GetOccupiedCells(entity))
                    {
                        if(cell.BlocksMovement())return Reject(zone,"blocked-stair");
                        stairs.Add(cell);
                    }
            if(stairs.Count==0)return Reject(zone,"no-stairs");
            var origin=stairs[0];
            var visited=new bool[Zone.Width,Zone.Height];
            var parent=new (int x,int y)[Zone.Width,Zone.Height];
            var queue=new Queue<Cell>();
            var exits=new Cell[4];
            visited[origin.X,origin.Y]=true;queue.Enqueue(origin);
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();
                if(cell.X==0&&exits[0]==null)exits[0]=cell;
                if(cell.X==Zone.Width-1&&exits[1]==null)exits[1]=cell;
                if(cell.Y==0&&exits[2]==null)exits[2]=cell;
                if(cell.Y==Zone.Height-1&&exits[3]==null)exits[3]=cell;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dy==0)continue;
                    var next=zone.GetCell(cell.X+dx,cell.Y+dy);
                    if(next==null||visited[next.X,next.Y]||next.BlocksMovement())continue;
                    visited[next.X,next.Y]=true;parent[next.X,next.Y]=(cell.X,cell.Y);queue.Enqueue(next);
                }
            }
            foreach(var exit in exits)if(exit==null)return Reject(zone,"missing-edge-route");
            foreach(var stair in stairs)if(!visited[stair.X,stair.Y])return Reject(zone,"unreachable-stair");
            var pending=new HashSet<(int x,int y)>();
            void ReservePath(Cell target)
            {
                int x=target.X,y=target.Y;pending.Add((x,y));
                while(x!=origin.X||y!=origin.Y)
                {var previous=parent[x,y];x=previous.x;y=previous.y;pending.Add((x,y));}
            }
            foreach(var exit in exits)ReservePath(exit);
            foreach(var stair in stairs)
            {
                ReservePath(stair);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    var approach=zone.GetCell(stair.X+dx,stair.Y+dy);
                    if(approach!=null&&!approach.BlocksMovement())pending.Add((approach.X,approach.Y));
                }
            }
            // Publish only once every required endpoint has a real route.
            foreach(var cell in pending)zone.GenReservedCells.Add(cell);
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","UndergroundRoutesReserved",
                payload:new{zone=zone.ZoneID,cells=pending.Count,stairCells=stairs.Count});
            return true;
        }
        private static bool Reject(Zone zone,string reason)
        {
            if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","UndergroundRoutesRejected",payload:new{zone=zone?.ZoneID,reason});
            return false;
        }
    }
}
