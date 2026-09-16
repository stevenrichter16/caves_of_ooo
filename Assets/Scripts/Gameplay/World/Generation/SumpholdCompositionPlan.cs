using System;
using System.Collections.Generic;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>Logical shallow-cut boatyard; dry work fingers are native land,
    /// not bridges with a hidden immunity or vertical traversal rule.</summary>
    public sealed class SumpholdCompositionPlan
    {
        public const string ZoneID="Overworld.15.6.0";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int w,int h){Role=role;X=x;Y=y;Width=w;Height=h;DoorX=x+w/2;DoorY=y+h-1;}
        }
        public readonly struct WorkArea
        {
            public readonly string Role;public readonly int X,Y;
            internal WorkArea(string role,int x,int y){Role=role;X=x;Y=y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        private readonly List<Room> rooms=new List<Room>();private readonly List<WorkArea> work=new List<WorkArea>();private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public IReadOnlyList<Room> Rooms=>rooms;public IReadOnlyList<WorkArea> WorkAreas=>work;public IReadOnlyList<ProfilePlacement> Profile=>profile;
        private readonly string[,] objects=new string[80,25];private readonly bool[,] interior=new bool[80,25],wet=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static SumpholdCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("Exact Sumphold surface address required.",nameof(id));return new SumpholdCompositionPlan(seed);}
        private SumpholdCompositionPlan(int seed)
        {
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            AddRoom("CuttersHouse",7+rng.Next(3),2,15,7);
            AddRoom("ServiceHouse",32+rng.Next(3),1,12,7);
            AddRoom("RecordsShelter",60+rng.Next(3),3,13,6);
            int left=18+rng.Next(-1,2),middle=42+rng.Next(-1,2),right=66+rng.Next(-1,2);
            work.Add(new WorkArea("HullTrestles",left,15));work.Add(new WorkArea("LoadingFinger",middle,15));work.Add(new WorkArea("PeatWork",right,15));
            profile.Add(new ProfilePlacement("BoatFrame",left-1,19));profile.Add(new ProfilePlacement("BoatFrame",right+1,19));
            profile.Add(new ProfilePlacement("PeatCutter",left+1,17));profile.Add(new ProfilePlacement("PeatCutter",right-1,17));
            profile.Add(new ProfilePlacement("TollRolls",middle+2,16));
            // Four unequal cuts leave three dry working fingers. The irregular
            // end faces alter with the seed; water never lies beneath a board.
            Cut(4,left-5,16+rng.Next(2),23,rng);
            Cut(left+5,middle-6,15+rng.Next(2),24,rng);
            Cut(middle+6,right-5,16+rng.Next(2),24,rng);
            Cut(right+5,77,17,23,rng);
            foreach(var owner in profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)reserved[owner.X+dx,owner.Y+dy]=true;
            for(int y=10;y<=14;y++)for(int x=35;x<=45;x++)if(x!=40||y!=12)MarkApproach(x,y,false);
            reserved[40,12]=true;
            Route(0,12,39,12,false);Route(79,12,41,12,false);
            Route(40,0,40,10,false);Route(middle,24,middle,14,true);
            // A seed can align the service doorway with the future main well.
            // Join its clear northern frontage, never route into the solid well.
            foreach(var r in rooms)Route(r.DoorX,r.DoorY,r.DoorX,r.DoorX==40?11:12,false);
            foreach(var w in work){Route(w.X,12,w.X,23,true);Route(w.X,w.Y,40,11,false);}
            foreach(var owner in profile)Route(owner.X-1,owner.Y,owner.X-1,14,true);
            // Reed colonies collect beside real cut margins, not evenly over
            // the working ground. Peat faces themselves remain dry and solid.
            for(int y=14;y<24;y++)for(int x=3;x<78;x++)
                if(!wet[x,y]&&!reserved[x,y]&&objects[x,y]==null&&NearWater(x,y)&&rng.Next(4)==0){objects[x,y]="Reeds";reserved[x,y]=true;}
        }
        private void AddRoom(string role,int x,int y,int w,int h)
        {
            var r=new Room(role,x,y,w,h);rooms.Add(r);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;bool door=yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=1;
                if(edge&&!door){objects[xx,yy]="SandstoneWall";reserved[xx,yy]=true;}
                else if(!edge)interior[xx,yy]=true;
                if(door)MarkApproach(xx,yy,false);
            }
            objects[x+2,y+2]=role=="CuttersHouse"?"Bed":"Crate";reserved[x+2,y+2]=true;
            objects[x+w-3,y+h-3]="Chair";reserved[x+w-3,y+h-3]=true;
        }
        private void Cut(int x0,int x1,int top,int bottom,Random rng)
        {
            for(int y=top;y<=bottom;y++)
            {
                int inset=y==top||y==bottom?1:0;int end=x1-(y%3==0?rng.Next(2):0);
                for(int x=x0+inset;x<=end-inset;x++){wet[x,y]=true;objects[x,y]="WaterPuddle";reserved[x,y]=true;}
            }
            // The cut's tool-marked head is a coherent solid face, not isolated
            // props. Its dry side is reachable from the common work apron.
            for(int x=x0+1;x<x1;x++){objects[x,top-1]="PeatBank";reserved[x,top-1]=true;}
        }
        private bool NearWater(int x,int y)=>IsWet(x-1,y)||IsWet(x+1,y)||IsWet(x,y-1)||IsWet(x,y+1);
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||wet[x,y]||(objects[x,y]!=null&&objects[x,y]!="Duckboard")||(x==40&&y==12))return false;
            foreach(var p in profile)if(p.X==x&&p.Y==y)return false;return true;
        }
        private void MarkApproach(int x,int y,bool board)
        {
            if(!CanRoute(x,y))return;approach[x,y]=true;reserved[x,y]=true;
            if(board&&!interior[x,y])objects[x,y]="Duckboard";
        }
        private void Route(int sx,int sy,int tx,int ty,bool board)
        {
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int,int),(int,int)>();q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty)))
            {
                var c=q.Dequeue();foreach(var d in new[]{(0,-1),(-1,0),(1,0),(0,1)})
                {var n=(c.x+d.Item1,c.y+d.Item2);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}
            }
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Sumphold dry destination is inaccessible.");
            for(var c=(tx,ty);;c=prev[c]){MarkApproach(c.Item1,c.Item2,board);if(c==(sx,sy))break;}
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string GroundAt(int x,int y)=>InBounds(x,y)?interior[x,y]?"StoneFloor":"Floor":null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];public bool IsWet(int x,int y)=>InBounds(x,y)&&wet[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature(){var s=new StringBuilder(ZoneID);for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);return s.ToString();}
    }
}
