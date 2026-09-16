using System;
using System.Collections.Generic;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>Native dry intake architecture. Cargo bays and wide reserved
    /// aisles make actual dragging possible without changing hauling rules.</summary>
    public sealed class MarrowstyeCompositionPlan
    {
        public const string ZoneID="Overworld.12.12.0";
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
        private readonly List<Room> rooms=new List<Room>();private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public IReadOnlyList<Room> Rooms=>rooms;public IReadOnlyList<ProfilePlacement> Profile=>profile;
        private readonly string[,] objects=new string[80,25];private readonly bool[,] interior=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static MarrowstyeCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("Exact Marrowstye surface address required.",nameof(id));return new MarrowstyeCompositionPlan(seed);}
        private MarrowstyeCompositionPlan(int seed)
        {
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            AddRoom("IntakeHall",10+rng.Next(3),1,32+rng.Next(3),10,true);
            AddRoom("SupplyWing",53+rng.Next(3),2,15,7,true);
            AddRoom("DomesticWing",54+rng.Next(3),17,13,7,false);
            AddRoom("DisusedWing",9+rng.Next(3),17,19,6,false);
            var hall=rooms[0];
            profile.Add(new ProfilePlacement("StoneCoffer",hall.X+7,hall.Y+2));
            profile.Add(new ProfilePlacement("StoneCoffer",hall.X+17,hall.Y+2));
            profile.Add(new ProfilePlacement("FilerClerk",hall.X+hall.Width-5,hall.Y+4));
            objects[hall.X+7,hall.Y+5]="SaltCuredBody";objects[hall.X+17,hall.Y+5]="SaltCuredBody";
            // Two short masonry fingers distinguish receiving bays without
            // closing the lower three-row haul aisle or inventing locked rooms.
            foreach(int x in new[]{hall.X+12,hall.X+22})
                for(int y=hall.Y+1;y<=hall.Y+3;y++)objects[x,y]="SandstoneWall";
            objects[hall.X+hall.Width-3,hall.Y+2]="Crate";
            objects[hall.X+hall.Width-3,hall.Y+4]="Crate";
            foreach(var p in profile)reserved[p.X,p.Y]=true;
            // Shared longitudinal aisles give a hauler and load room to turn.
            // The cargo owners themselves never become approach cells.
            for(int y=hall.Y+1;y<hall.Y+hall.Height-1;y++)for(int x=hall.X+2;x<hall.X+hall.Width-2;x++)
            {reserved[x,y]=true;if(CanRoute(x,y))approach[x,y]=true;}
            for(int y=10;y<=14;y++)for(int x=36;x<=44;x++)Mark(x,y);
            reserved[40,12]=true;
            Route(0,12,39,12);Route(79,12,41,12);Route(40,0,40,11);Route(40,24,40,13);
            foreach(var room in rooms)Route(room.DoorX,room.DoorY,40,11);
            foreach(var p in profile)Route(p.X,p.Y+1,rooms[0].DoorX,rooms[0].DoorY);
            AddBorderColonies(rng);
        }
        private void AddRoom(string role,int x,int y,int w,int h,bool south)
        {
            var r=new Room(role,x,y,w,h,south);rooms.Add(r);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;bool door=yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=1;
                if(edge&&!door){objects[xx,yy]="SandstoneWall";reserved[xx,yy]=true;}
                else if(!edge)interior[xx,yy]=true;
                if(door){approach[xx,yy]=true;reserved[xx,yy]=true;}
            }
            if(role=="DisusedWing")
            {
                objects[x,y+2]="Rubble";objects[x,y+3]="Rubble";
                // Disuse is an initial placement contract, not an invisible
                // runtime barrier; actors and the player may enter later.
                for(int yy=y+1;yy<y+h-1;yy++)for(int xx=x+1;xx<x+w-1;xx++)reserved[xx,yy]=true;
            }
            if(role=="IntakeHall")return;
            objects[x+2,y+2]=role=="DomesticWing"?"Bed":role=="DisusedWing"?"Bones":"Crate";reserved[x+2,y+2]=true;
            objects[x+w-3,y+h-3]=role=="DisusedWing"?"Rubble":"Chair";reserved[x+w-3,y+h-3]=true;
        }
        private void AddBorderColonies(Random rng)
        {
            foreach(var c in new[]{(4,4),(75,4),(75,20)})
            {
                int facing=rng.Next(2)==0?1:-1;
                foreach(var d in new[]{(-1,0),(1,1)})Plant(c.Item1+d.Item1*facing,c.Item2+d.Item2,"Tree");
                foreach(var d in new[]{(-2,-1),(-1,2),(2,0),(0,-1),(2,2)})Plant(c.Item1+d.Item1*facing,c.Item2+d.Item2,"Bush");
            }
        }
        private void Plant(int x,int y,string blueprint)
        {if(InBounds(x,y)&&!reserved[x,y]&&!interior[x,y]&&objects[x,y]==null){objects[x,y]=blueprint;reserved[x,y]=true;}}
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;
            foreach(var p in profile)if(p.X==x&&p.Y==y)return false;return true;
        }
        private void Mark(int x,int y){if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}}
        private void Route(int sx,int sy,int tx,int ty)
        {
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int,int),(int,int)>();q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty)))
            {
                var c=q.Dequeue();foreach(var d in new[]{(0,1),(-1,0),(1,0),(0,-1)})
                {var n=(c.x+d.Item1,c.y+d.Item2);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}
            }
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Marrowstye public frontage is unreachable.");
            for(var c=(tx,ty);;c=prev[c]){Mark(c.Item1,c.Item2);Mark(c.Item1+1,c.Item2);if(c==(sx,sy))break;}
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string GroundAt(int x,int y)=>InBounds(x,y)?interior[x,y]?"StoneFloor":approach[x,y]?"RoadStone":"Floor":null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature(){var s=new StringBuilder(ZoneID);for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);return s.ToString();}
    }
}
