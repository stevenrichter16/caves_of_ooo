using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Five functional shelters around Wellmeet's native well commons.
    /// Logical cell data only; services and village residents retain native Parts.</summary>
    public sealed class WellmeetCompositionPlan
    {
        public const string ZoneID="Overworld.8.16.0";
        public sealed class Tent
        {
            public readonly string Role;
            public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Tent(string role,int x,int y,int width,int height,bool south)
            {Role=role;X=x;Y=y;Width=width;Height=height;DoorX=x+width/2;DoorY=south?y+height-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        private readonly List<Tent> tents=new List<Tent>();
        private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public IReadOnlyList<Tent> Tents=>tents;
        public IReadOnlyList<ProfilePlacement> Profile=>profile;
        public int HostX=>profile[0].X;public int HostY=>profile[0].Y;
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] shade=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static WellmeetCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("The exact Wellmeet surface address is required.",nameof(id));
            return new WellmeetCompositionPlan(seed);
        }
        private WellmeetCompositionPlan(int seed)
        {
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            AddTent("GuestReception",10+rng.Next(4),3,10+rng.Next(3),6,true);
            AddTent("ServiceShelter",28+rng.Next(2),2,8+rng.Next(2),5,true);
            AddTent("SaltExchange",57+rng.Next(3),3,10+rng.Next(3),6,true);
            AddTent("Household",12+rng.Next(4),17,12+rng.Next(3),6,false);
            AddTent("RestingHouse",51+rng.Next(4),17,11+rng.Next(3),6,false);
            var guest=tents[0];var salt=tents[2];
            profile.Add(new ProfilePlacement("TentRightHost",guest.DoorX+2,guest.DoorY+2));
            profile.Add(new ProfilePlacement("SaltMaster",salt.DoorX+2,salt.DoorY+2));
            profile.Add(new ProfilePlacement("GuestClothPole",HostX-1,HostY+1));
            profile.Add(new ProfilePlacement("Well",HostX+3,HostY+1));
            foreach(var owner in profile)
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)reserved[owner.X+dx,owner.Y+dy]=true;
            // The native main well/markers are added later. Leave a generous
            // open court, and route around the future solid well itself.
            for(int y=9;y<=15;y++)for(int x=35;x<=45;x++)
                if(x!=40||y!=12){approach[x,y]=true;reserved[x,y]=true;}
            reserved[40,12]=true;
            // Destination links bend around receiving yards and share short
            // trunks; household branches join the nearest useful public route.
            Chain(new[]{(0,12),(9,14),(25,13),(32,11),(35,11)});
            Chain(new[]{(79,12),(71,14),(58,13),(49,11),(45,11)});
            Chain(new[]{(40,0),(44,3),(46,7),(44,9)});
            Chain(new[]{(40,24),(36,22),(33,18),(36,15)});
            foreach(var tent in tents)JoinRoute(tent.DoorX,tent.DoorY);
            foreach(var owner in profile)JoinRoute(owner.X-1,owner.Y);
            AddFringes(rng);

        }
        private void AddTent(string role,int x,int y,int w,int h,bool south)
        {
            var t=new Tent(role,x,y,w,h,south);tents.Add(t);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;
                bool door=yy==t.DoorY&&Math.Abs(xx-t.DoorX)<=1;
                if(edge&&!door){objects[xx,yy]="TentWall";reserved[xx,yy]=true;}
                else if(!edge)shade[xx,yy]=true;
                if(door){approach[xx,yy]=true;reserved[xx,yy]=true;}
            }
            objects[x+2,y+2]=role=="ServiceShelter"||role=="SaltExchange"?"Crate":"Bed";objects[x+w-3,y+h-3]="Chair";
            reserved[x+2,y+2]=true;reserved[x+w-3,y+h-3]=true;
        }
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;
            foreach(var owner in profile)if(owner.X==x&&owner.Y==y)return false;
            return true;
        }
        private void Route(int sx,int sy,int tx,int ty,bool wide=false)
        {
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int x,int y),(int x,int y)>();
            q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty)))
            {
                var c=q.Dequeue();foreach(var d in new[]{(0,-1),(-1,0),(1,0),(0,1)})
                {var n=(c.x+d.Item1,c.y+d.Item2);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}
            }
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Wellmeet semantic destination is inaccessible.");
            for(var c=(x:tx,y:ty);;c=prev[c])
            {
                foreach(var d in wide?new[]{(0,0),(0,1)}:new[]{(0,0)})
                {int x=c.x+d.Item1,y=c.y+d.Item2;if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}}
                if(c==(sx,sy))break;
            }
        }
        private void Chain((int x,int y)[] points)
        {for(int i=1;i<points.Length;i++)Route(points[i-1].x,points[i-1].y,points[i].x,points[i].y,true);}
        private void JoinRoute(int x,int y)
        {
            // Ignore the doorway's own reserved apron while finding a trunk.
            int bx=-1,by=-1,best=int.MaxValue;
            for(int yy=0;yy<25;yy++)for(int xx=0;xx<80;xx++)
            {
                int distance=Math.Abs(xx-x)+Math.Abs(yy-y);
                if(!approach[xx,yy]||distance<2||shade[xx,yy]||!CanRoute(xx,yy))continue;
                bool door=false;foreach(var t in tents)if(yy==t.DoorY&&Math.Abs(xx-t.DoorX)<=1)door=true;
                if(!door&&distance<best){best=distance;bx=xx;by=yy;}
            }
            if(bx<0)throw new InvalidOperationException("No public Wellmeet route.");
            Route(x,y,bx,by);
        }
        private void AddFringes(Random rng)
        {
            // Dry native colonies on four outer shoulders, not uniform clutter.
            foreach(var center in new[]{(5,4),(74,4),(6,20),(73,20)})
            {
                int brush=0,rock=0;
                for(int attempt=0;attempt<120&&(brush<8||rock<2);attempt++)
                {
                    int x=center.Item1+rng.Next(-3,4),y=center.Item2+rng.Next(-2,3);
                    if(!InBounds(x,y)||reserved[x,y]||shade[x,y]||objects[x,y]!=null)continue;
                    objects[x,y]=brush<8?"DryBrush":"Rock";reserved[x,y]=true;
                    if(brush<8)brush++;else rock++;
                }
            }
        }
        public string GroundAt(int x,int y)=>InBounds(x,y)?(shade[x,y]?"StoneFloor":approach[x,y]?"RoadStone":"Sand"):null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&shade[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID);foreach(var t in tents)s.Append('|').Append(t.Role).Append(':').Append(t.X).Append(',').Append(t.Width);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");
            return s.ToString();
        }
    }
}
