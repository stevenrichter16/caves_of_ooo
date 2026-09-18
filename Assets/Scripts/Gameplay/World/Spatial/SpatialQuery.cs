using System;

namespace CavesOfOoo.Core
{
    /// <summary>Physical contact queries. Anchors remain identity/save/render
    /// origins; range is the nearest occupied body cell using Chebyshev distance.</summary>
    public static class SpatialQuery
    {
        public static int Distance(Zone zone,Entity a,Entity b)
        {
            if(zone==null || a==null || b==null) return int.MaxValue;
            int best=int.MaxValue;
            foreach(var cell in zone.GetOccupiedCells(a))
                if(cell!=null) best=Math.Min(best,DistanceToCell(zone,b,cell.X,cell.Y));
            return best;
        }
        public static int DistanceAt(Zone zone,Entity actor,int x,int y,Entity target)
        {
            if(zone==null || actor==null || target==null) return int.MaxValue;
            int best=int.MaxValue;
            foreach(var cell in zone.GetOccupiedCells(actor,x,y))
                if(cell!=null) best=Math.Min(best,DistanceToCell(zone,target,cell.X,cell.Y));
            return best;
        }
        public static int DistanceToCell(Zone zone,Entity entity,int x,int y)
        {
            var cell=ClosestCell(zone,entity,x,y);
            return cell==null ? int.MaxValue : Math.Max(Math.Abs(cell.X-x),Math.Abs(cell.Y-y));
        }
        public static Cell ClosestCell(Zone zone,Entity entity,int x,int y)
        {
            if(zone==null || entity==null) return null;
            Cell best=null; int distance=int.MaxValue;
            foreach(var cell in zone.GetOccupiedCells(entity))
            {
                if(cell==null) continue;
                int d=Math.Max(Math.Abs(cell.X-x),Math.Abs(cell.Y-y));
                if(d < distance) { distance=d; best=cell; }
            }
            return best;
        }
    }
}
