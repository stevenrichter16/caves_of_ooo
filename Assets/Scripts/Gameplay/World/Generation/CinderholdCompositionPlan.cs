using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>The named Concord work town, expressed entirely as native cell
    /// intentions. Its four buildings have public and domestic roles; this is
    /// not a general biome override or a reconstruction recipe for saved owners.</summary>
    public sealed class CinderholdCompositionPlan
    {
        public const string ZoneID="Overworld.6.6.0";
        public sealed class Room
        {
            public readonly string Role;
            public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int width,int height,int doorOffset,bool south)
            {Role=role;X=x;Y=y;Width=width;Height=height;DoorX=x+doorOffset;DoorY=south?y+height-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;
            public readonly int X,Y;
            internal ProfilePlacement(string blueprint,int x,int y){Blueprint=blueprint;X=x;Y=y;}
        }
        private readonly List<Room> rooms=new List<Room>();
        private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public IReadOnlyList<Room> Rooms {get;}
        public IReadOnlyList<ProfilePlacement> Profile {get;}
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly bool[,] interior=new bool[Zone.Width,Zone.Height];
        private readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        private readonly bool[,] reserved=new bool[Zone.Width,Zone.Height];
        private static readonly (int x,int y)[] Directions={(0,-1),(-1,0),(1,0),(0,1)};

        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static CinderholdCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("The exact Cinderhold surface address is required.",nameof(id));
            return new CinderholdCompositionPlan(seed);
        }
        private CinderholdCompositionPlan(int seed)
        {
            Rooms=rooms.AsReadOnly();Profile=profile.AsReadOnly();
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            AddRoom("LedgerOffice",17+rng.Next(3),2,13+rng.Next(3),7,5,true);
            AddRoom("ForgeWorkshop",50+rng.Next(3),2,17+rng.Next(3),8,5,true);
            AddRoom("WorkersHouse",17+rng.Next(3),17,12+rng.Next(3),6,5,false);
            AddRoom("RestHouse",52+rng.Next(3),18,10+rng.Next(3),5,4,false);
            var office=rooms[0];var work=rooms[1];
            AddProfile("ConcordFactor",office.X+3,office.Y+3);
            AddProfile("CinderholdNoticeBoard",office.DoorX-3,office.DoorY+2);
            AddProfile("Chest",office.X+office.Width-3,office.Y+2);
            AddProfile("Campfire",office.X+office.Width+2,office.DoorY+3);
            AddProfile("TinkersForge",work.X+5,work.Y+3);
            AddProfile("SmithAnvil",work.X+10,work.Y+3);
            AddProfile("Weaponsmith",work.X+5,work.Y+5);

            // Reserve the later native well and four ground markers without
            // inventing a second well or placing its solid owner during base.
            for(int y=9;y<=15;y++)for(int x=35;x<=45;x++)
            {reserved[x,y]=true;if(x!=40||y!=12)approach[x,y]=true;}
            // Wide receiving ground belongs to the workshop; the office's
            // frontage is smaller, so the two functions read differently.
            for(int y=10;y<=14;y++)for(int x=work.X+1;x<work.X+work.Width-1;x++)
                if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}
            Chain(new[]{(0,11),(10,13),(30,12),(35,13)});
            Chain(new[]{(79,14),(71,13),(59,12),(45,13)});
            Chain(new[]{(40,0),(42,6),(42,9)});
            Chain(new[]{(40,24),(36,21),(39,16),(39,15)});
            foreach(var r in rooms)JoinPublicRoute(r.DoorX,r.DoorY);
            foreach(var p in profile)
            {
                // A standing cell next to the real object, not a route drawn
                // through the anvil, crate or talking occupant itself.
                int x=p.X+1,y=p.Y;
                if(!CanRoute(x,y)){x=p.X;y=p.Y+1;}
                JoinPublicRoute(x,y);
            }
            AddForestShoulders(rng);
        }
        private void AddRoom(string role,int x,int y,int width,int height,int offset,bool south)
        {
            var room=new Room(role,x,y,width,height,offset,south);rooms.Add(room);
            for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)
            {
                bool edge=xx==x||xx==x+width-1||yy==y||yy==y+height-1;
                bool door=edge&&yy==room.DoorY&&Math.Abs(xx-room.DoorX)<=1;
                if(edge&&!door){objects[xx,yy]="SandstoneWall";reserved[xx,yy]=true;}
                else if(!edge)interior[xx,yy]=true;
                if(door){approach[xx,yy]=true;reserved[xx,yy]=true;}
            }
            if(role=="WorkersHouse"||role=="RestHouse")
            {Place("Bed",x+2,y+2);Place("Chair",x+width-3,y+height-3);}
            else
            {Place("Chair",x+2,y+height-3);Place("Crate",x+width-3,y+height-3);}
        }
        private void AddProfile(string bp,int x,int y)
        {
            profile.Add(new ProfilePlacement(bp,x,y));
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(x+dx,y+dy))reserved[x+dx,y+dy]=true;
        }
        private void Place(string bp,int x,int y){objects[x,y]=bp;reserved[x,y]=true;}
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;
            foreach(var p in profile)if(p.X==x&&p.Y==y)return false;
            return true;
        }
        private void Route(int sx,int sy,int tx,int ty,bool wide)
        {
            var previous=new Dictionary<(int x,int y),(int x,int y)>();var queue=new Queue<(int x,int y)>();
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Cinderhold route endpoint is occupied.");
            queue.Enqueue((sx,sy));previous[(sx,sy)]=(sx,sy);
            while(queue.Count>0&&!previous.ContainsKey((tx,ty)))
            {
                var c=queue.Dequeue();foreach(var d in Directions)
                {var n=(x:c.x+d.x,y:c.y+d.y);if(!CanRoute(n.x,n.y)||previous.ContainsKey(n))continue;previous[n]=c;queue.Enqueue(n);}
            }
            if(!previous.ContainsKey((tx,ty)))throw new InvalidOperationException("Cinderhold service lacks a public route.");
            for(var c=(x:tx,y:ty);;c=previous[c])
            {
                int radius=wide?1:0;
                for(int dx=-radius;dx<=radius;dx++)for(int dy=-radius;dy<=radius;dy++)
                {int x=c.x+dx,y=c.y+dy;if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}}
                if(c==(sx,sy))break;
            }
        }
        private void Chain((int x,int y)[] points)
        {for(int i=1;i<points.Length;i++)Route(points[i-1].x,points[i-1].y,points[i].x,points[i].y,true);}
        private void JoinPublicRoute(int x,int y)
        {
            int targetX=-1,targetY=-1,best=int.MaxValue;
            for(int yy=0;yy<Zone.Height;yy++)for(int xx=0;xx<Zone.Width;xx++)
            {
                int distance=Math.Abs(x-xx)+Math.Abs(y-yy);
                if(distance<3||!approach[xx,yy]||interior[xx,yy]||!CanRoute(xx,yy))continue;
                bool door=false;foreach(var r in rooms)if(r.DoorY==yy&&Math.Abs(r.DoorX-xx)<=1)door=true;
                if(!door&&distance<best){best=distance;targetX=xx;targetY=yy;}
            }
            if(targetX<0)throw new InvalidOperationException("Cinderhold public frontage is missing.");
            Route(x,y,targetX,targetY,false);
        }
        private void AddForestShoulders(Random rng)
        {
            // Coarse, separated trunks make a sheltered edge without pinching
            // off one-cell pockets. Bushes are native walkable undergrowth.
            foreach(var center in new[]{(x:6,y:4),(x:73,y:4),(x:6,y:20),(x:73,y:20)})
            {
                var slots=new List<(int x,int y)>();
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)slots.Add((center.x+dx*3,center.y+dy*2));
                int count=0;
                while(slots.Count>0&&count<5)
                {int i=rng.Next(slots.Count);var c=slots[i];slots.RemoveAt(i);if(reserved[c.x,c.y]||interior[c.x,c.y]||objects[c.x,c.y]!=null)continue;Place("Tree",c.x,c.y);count++;}
                int bushes=0;
                for(int n=0;n<100&&bushes<9;n++)
                {
                    int x=center.x+rng.Next(-4,5),y=center.y+rng.Next(-2,3);
                    if(!InBounds(x,y)||reserved[x,y]||interior[x,y]||objects[x,y]!=null)continue;
                    Place("Bush",x,y);bushes++;
                }
            }
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string GroundAt(int x,int y)=>InBounds(x,y)?interior[x,y]?"StoneFloor":approach[x,y]?"RoadStone":"Grass":null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature()
        {
            var s=new StringBuilder(ZoneID);
            foreach(var r in rooms)s.Append('|').Append(r.Role).Append(':').Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width).Append(',').Append(r.Height);
            foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");
            return s.ToString();
        }
    }
}
