using System;
using System.Collections.Generic;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>A finite inhabited western supply post faces an empty eastern
    /// frontier. Routes are walkable local ground, not an authored world road.</summary>
    public sealed class LastCounterCompositionPlan
    {
        public const string ZoneID="Overworld.18.18.0";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int w,int h,bool south)
            {Role=role;X=x;Y=y;Width=w;Height=h;DoorX=x+w/2;DoorY=south?y+h-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        public string FormationName{get;}
        public IReadOnlyList<Room> Rooms{get;}
        public IReadOnlyList<ProfilePlacement> Profile{get;}
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interior=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        private static readonly (int x,int y)[] steps={(0,1),(-1,0),(1,0),(0,-1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static LastCounterCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("Exact Last Counter surface address required.",nameof(id));return new LastCounterCompositionPlan(seed);}
        private LastCounterCompositionPlan(int seed)
        {
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            int form=FormationSelector.StableIndex(ZoneID+":formation:"+seed,3);
            var rooms=new List<Room>();
            if(form==0)
            {
                FormationName="UpperSupplyCourt";
                rooms.Add(new Room("SupplyPost",6+rng.Next(2),1,23+rng.Next(2),8,true));
                rooms.Add(new Room("RestShelter",7+rng.Next(2),18,18,6,false));
                rooms.Add(new Room("WorkersHouse",36+rng.Next(2),2,15,7,true));
            }
            else if(form==1)
            {
                FormationName="LowerSupplyCourt";
                rooms.Add(new Room("SupplyPost",7+rng.Next(2),17,24+rng.Next(2),7,false));
                rooms.Add(new Room("RestShelter",6+rng.Next(2),1,18,7,true));
                rooms.Add(new Room("WorkersHouse",35+rng.Next(2),2,15,8,true));
            }
            else
            {
                FormationName="SteppedPost";
                rooms.Add(new Room("SupplyPost",27+rng.Next(2),2,25,8,true));
                rooms.Add(new Room("RestShelter",5+rng.Next(2),2,18,7,true));
                rooms.Add(new Room("WorkersHouse",7+rng.Next(2),18,15,6,false));
            }
            Rooms=rooms.AsReadOnly();foreach(var r in rooms)AddRoom(r);
            var post=rooms[0];var rest=rooms[1];
            AddForecourt(post);
            Profile=new List<ProfilePlacement>{new ProfilePlacement("SaccharineEnvoy",post.X+6,post.Y+4),
                new ProfilePlacement("Chest",post.X+post.Width-5,post.Y+3),new ProfilePlacement("Campfire",rest.X+7,rest.Y+3),
                new ProfilePlacement("LastCounterSign",56+rng.Next(3),11)}.AsReadOnly();
            foreach(var p in Profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)reserved[p.X+dx,p.Y+dy]=true;
            for(int y=0;y<25;y++)for(int x=62;x<80;x++)reserved[x,y]=true;
            // The empty frontier is only an initial seeding exclusion, never
            // an invisible travel barrier. The native well keeps its old site.
            reserved[40,12]=true;
            for(int y=10;y<=14;y++)for(int x=36;x<=44;x++)Mark(x,y);
            Route(0,12,39,12);Route(79,12,41,12);Route(40,0,40,11);Route(40,24,40,13);
            foreach(var r in rooms)Route(r.DoorX,r.DoorY,39,12);
            foreach(var p in Profile)Route(p.X,p.Y+1,41,13);
            DressWesternShoulders(rng);
        }
        private void AddRoom(Room r)
        {
            for(int y=r.Y;y<r.Y+r.Height;y++)for(int x=r.X;x<r.X+r.Width;x++)
            {
                bool edge=x==r.X||x==r.X+r.Width-1||y==r.Y||y==r.Y+r.Height-1;
                bool door=y==r.DoorY&&Math.Abs(x-r.DoorX)<=1;
                if(edge&&!door){objects[x,y]="SandstoneWall";reserved[x,y]=true;}
                if(!edge)interior[x,y]=true;
                if(door){approach[x,y]=true;reserved[x,y]=true;}
            }
            if(r.Role=="RestShelter")
            {
                Put(r.X+3,r.Y+2,"Bed");Put(r.X+6,r.Y+2,"Bed");
                Put(r.X+3,r.Y+r.Height-3,"Chair");Put(r.X+r.Width-3,r.Y+r.Height-3,"Chair");
            }
            else if(r.Role=="SupplyPost")
            {
                Put(r.X+2,r.Y+2,"Crate");Put(r.X+3,r.Y+2,"Crate");
                Put(r.X+4,r.Y+2,"Crate");Put(r.X+2,r.Y+3,"Crate");
                Put(r.X+r.Width-3,r.Y+r.Height-3,"Chair");
            }
            else{Put(r.X+2,r.Y+2,"Bed");Put(r.X+r.Width-3,r.Y+r.Height-3,"Chair");}
        }
        private void AddForecourt(Room post)
        {
            int y=FormationName=="LowerSupplyCourt"?post.Y-6:post.Y+post.Height+1;
            int west=post.X-1,east=post.X+post.Width+1;
            // Unequal open returns shelter real loading goods. Neither return
            // encloses a second room or duplicates the abandoned posts outside.
            for(int dy=0;dy<5;dy++)Put(west,y+dy,"SandstoneWall");
            Put(west+1,y+4,"SandstoneWall");
            for(int dy=0;dy<3;dy++)Put(east,y+dy,"SandstoneWall");
            Put(east-1,y+2,"SandstoneWall");
            Put(post.X+4,y+1,"Crate");Put(post.X+5,y+1,"Crate");Put(post.X+post.Width-4,y+1,"Crate");
        }
        private void DressWesternShoulders(Random rng)
        {
            foreach(var center in new[]{(2,3),(3,21),(55,20)})
            {
                int brush=0,rubble=0;
                for(int attempt=0;attempt<160&&(brush<6||rubble<2);attempt++)
                {
                    int x=center.Item1+rng.Next(-2,3),y=center.Item2+rng.Next(-2,3);
                    if(!InBounds(x,y)||x>=58||reserved[x,y]||interior[x,y]||objects[x,y]!=null)continue;
                    if(brush<6){Put(x,y,"DryBrush");brush++;}else{Put(x,y,"Rubble");rubble++;}
                }
            }
        }
        private void Put(int x,int y,string bp){objects[x,y]=bp;reserved[x,y]=true;}
        private bool CanRoute(int x,int y)
        {if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;foreach(var p in Profile)if(p.X==x&&p.Y==y)return false;return true;}
        private void Mark(int x,int y){if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}}
        private void Route(int sx,int sy,int tx,int ty)
        {
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Last Counter frontage overlaps owner.");
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int,int),(int,int)>();q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty)))
            {var c=q.Dequeue();foreach(var d in steps){var n=(c.x+d.x,c.y+d.y);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}}
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Last Counter frontage unreachable.");
            for(var c=(tx,ty);;c=prev[c]){Mark(c.Item1,c.Item2);Mark(c.Item1+1,c.Item2);if(c==(sx,sy))break;}
        }
        private static bool InBounds(int x,int y)=>x>=0&&x<80&&y>=0&&y<25;
        public string GroundAt(int x,int y)=>InBounds(x,y)?interior[x,y]?"StoneFloor":"Floor":null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature(){var s=new StringBuilder(ZoneID).Append(':').Append(FormationName);for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");foreach(var p in Profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);return s.ToString();}
    }
}
