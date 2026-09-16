using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>Quillhold's public copying and storage district. Physical archive
    /// circulation only: no Reader channel, sealed library or expedition quest.</summary>
    public sealed class QuillholdCompositionPlan
    {
        public const string ZoneID="Overworld.14.9.0",ProfileID="PrimaryArchive";
        public sealed class Room
        {
            public readonly string Role; public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal readonly char Facing;
            internal Room(string role,int x,int y,int w,int h,char facing)
            {Role=role;X=x;Y=y;Width=w;Height=h;Facing=facing;DoorX=facing=='E'?x+w-1:x+w/2;DoorY=facing=='E'?y+h/2:facing=='S'?y+h-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        private readonly List<Room> rooms=new List<Room>();
        private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public ReadOnlyCollection<Room> Rooms{get;}
        public ReadOnlyCollection<ProfilePlacement> Profile{get;}
        public string FormationName{get;}
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interiors=new bool[80,25],approaches=new bool[80,25],reserved=new bool[80,25];
        private static readonly (int x,int y)[] Directions={(0,-1),(-1,0),(1,0),(0,1)};
        private int scribeX,scribeY;
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static QuillholdCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("The exact Quillhold surface address is required.",nameof(id));return new QuillholdCompositionPlan(seed);}
        private QuillholdCompositionPlan(int seed)
        {
            Rooms=rooms.AsReadOnly();Profile=profile.AsReadOnly();var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            int formation=FormationSelector.StableIndex(ZoneID+":"+seed+":formation",3);
            FormationName=formation==0?"CrossAisleScriptorium":formation==1?"PairedReadingCourts":"LongArchiveSpine";
            if(formation<2)
            {
                AddRoom("CopyHall",7+rng.Next(3),2,20+rng.Next(3),8,'S');
                AddRoom("Stacks",47+rng.Next(3),2,21+rng.Next(3),9,'S');
                AddRoom("Refectory",12+rng.Next(3),17,15+rng.Next(3),7,'N');
                AddRoom("Receiving",56+rng.Next(3),17,12+rng.Next(3),6,'N');
            }
            else
            {
                AddRoom("CopyHall",49,2,23+rng.Next(3),9,'S');
                AddRoom("Stacks",7+rng.Next(2),5,14+rng.Next(2),15,'E');
                AddRoom("Refectory",49,17,14+rng.Next(2),7,'N');
                AddRoom("Receiving",68,17,10,7,'N');
            }
            var copy=rooms[0];var stacks=rooms[1];scribeX=copy.X+4;scribeY=copy.Y+4;
            // Two three-cell bookcase runs define the crossing aisle. The
            // shelf art spans local X, so both runs join along X rather than
            // leaving gaps along its shallow depth. Six owners/stock unchanged.
            foreach(int yy in new[]{stacks.Y+stacks.Height/2-2,stacks.Y+stacks.Height/2+2})
                foreach(int xx in new[]{stacks.X+stacks.Width/2-1,stacks.X+stacks.Width/2,stacks.X+stacks.Width/2+1})
                {profile.Add(new ProfilePlacement("QuillholdArchiveShelf",xx,yy));ReserveApron(xx,yy);}
            for(int y=10;y<=14;y++)for(int x=38;x<=43;x++)reserved[x,y]=true;
            // Routes connect actual destinations. Broad central commons stays
            // mostly quiet ground; only useful approaches are paved.
            Route(0,12,38,12,true);Route(43,12,79,12,true);Route(38,12,43,12,true);
            Route(40,0,38,12,true);Route(40,24,43,12,true);
            foreach(var r in rooms)JoinPublicRoute(r.DoorX,r.DoorY);
            // The native Scribe is seeded here later. Keep it eligible for the
            // population service hook and its open standing cell protected.
            reserved[scribeX,scribeY+1]=true;
            if(formation==1)ReflectNorthSouth();
        }
        private void AddRoom(string role,int x,int y,int w,int h,char facing)
        {
            var r=new Room(role,x,y,w,h,facing);rooms.Add(r);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;
                bool door=facing=='E'?xx==r.DoorX&&Math.Abs(yy-r.DoorY)<=1:yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=1;
                if(edge&&!door)Put(xx,yy,"QuillholdArchiveWall");
                else if(!edge)interiors[xx,yy]=true;
                if(door)approaches[xx,yy]=reserved[xx,yy]=true;
            }
            if(role=="CopyHall")
            {Put(x+4,y+2,"QuillholdCopyDesk");Put(x+w-5,y+2,"QuillholdCopyDesk");Put(x+w-5,y+3,"Chair");}
            if(role=="Refectory")
            {Put(x+5,y+3,"QuillholdRefectoryTable");Put(x+6,y+3,"QuillholdRefectoryTable");
                Put(x+4,y+3,"Chair");Put(x+7,y+3,"Chair");Put(x+5,y+2,"Chair");Put(x+6,y+4,"Chair");}
            if(role=="Receiving")
            {Put(x+2,y+2,"Crate");Put(x+w-3,y+2,"Crate");Put(x+2,y+h-3,"Crate");Put(x+w-3,y+h-3,"Bed");}
        }
        private void Put(int x,int y,string bp){objects[x,y]=bp;reserved[x,y]=true;}
        private void ReserveApron(int x,int y)
        {for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(x+dx,y+dy))reserved[x+dx,y+dy]=true;}
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12)||(x==scribeX&&y==scribeY))return false;
            foreach(var p in profile)if(p.X==x&&p.Y==y)return false;return true;
        }
        private void Route(int sx,int sy,int tx,int ty,bool wide)
        {
            var start=(x:sx,y:sy);var target=(x:tx,y:ty);var previous=new Dictionary<(int x,int y),(int x,int y)>();var q=new Queue<(int x,int y)>();
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Quillhold route endpoint intersects an owner.");
            previous[start]=start;q.Enqueue(start);
            while(q.Count>0&&!previous.ContainsKey(target))
            {
                var at=q.Dequeue();foreach(var d in Directions){var next=(x:at.x+d.x,y:at.y+d.y);if(!CanRoute(next.x,next.y)||previous.ContainsKey(next))continue;previous[next]=at;q.Enqueue(next);}
            }
            if(!previous.ContainsKey(target))throw new InvalidOperationException("Quillhold destination cannot reach the public route.");
            for(var at=target;;at=previous[at])
            {
                approaches[at.x,at.y]=reserved[at.x,at.y]=true;
                if(wide&&CanRoute(at.x,at.y+1)&&!interiors[at.x,at.y+1])approaches[at.x,at.y+1]=reserved[at.x,at.y+1]=true;
                if(at==start)break;
            }
        }
        private void JoinPublicRoute(int x,int y)
        {
            int bx=-1,by=-1,best=int.MaxValue;
            for(int yy=0;yy<25;yy++)for(int xx=0;xx<80;xx++)
            {int distance=Math.Abs(xx-x)+Math.Abs(yy-y);if(distance<2||distance>=best||!approaches[xx,yy]||interiors[xx,yy]||!CanRoute(xx,yy))continue;best=distance;bx=xx;by=yy;}
            if(bx<0)throw new InvalidOperationException("Quillhold requires a public receiving route.");Route(x,y,bx,by,false);
        }
        private void ReflectNorthSouth()
        {
            for(int y=0;y<12;y++)for(int x=0;x<80;x++)
            {int other=24-y;var o=objects[x,y];objects[x,y]=objects[x,other];objects[x,other]=o;Swap(interiors,x,y,other);Swap(approaches,x,y,other);Swap(reserved,x,y,other);}
            for(int i=0;i<rooms.Count;i++){var r=rooms[i];rooms[i]=new Room(r.Role,r.X,25-r.Y-r.Height,r.Width,r.Height,r.Facing=='S'?'N':r.Facing=='N'?'S':'E');}
            for(int i=0;i<profile.Count;i++){var p=profile[i];profile[i]=new ProfilePlacement(p.Blueprint,p.X,24-p.Y);}scribeY=24-scribeY;
        }
        private static void Swap(bool[,] grid,int x,int y,int other){bool v=grid[x,y];grid[x,y]=grid[x,other];grid[x,other]=v;}
        /// <summary>Only the native guaranteed Scribe uses this exact unreserved
        /// interior anchor. Unknown services retain their ordinary village choice.</summary>
        public bool TryGetServiceCell(string blueprint,out int x,out int y)
        {x=scribeX;y=scribeY;return blueprint=="Scribe";}
        public string GroundAt(int x,int y)=>!InBounds(x,y)?null:interiors[x,y]?"StoneFloor":approaches[x,y]?"RoadStone":"Floor";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interiors[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID).Append('|').Append(FormationName);
            foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");return s.ToString();
        }
    }
}
