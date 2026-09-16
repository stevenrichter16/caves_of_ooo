using System;
using System.Collections.Generic;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>Finite central exchange: modular trading shelters and meaningful
    /// dry loading routes. The plan never simulates caravans or a Counter audience.</summary>
    public sealed class TallyCompositionPlan
    {
        public const string ZoneID="Overworld.10.14.0",ProfileID="CentralExchange";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int w,int h,bool south){Role=role;X=x;Y=y;Width=w;Height=h;DoorX=x+w/2;DoorY=south?y+h-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        public string FormationName{get;}
        public IReadOnlyList<Room> Rooms{get;}
        public IReadOnlyList<ProfilePlacement> Profile{get;}
        readonly string[,] objects=new string[80,25];
        readonly bool[,] interior=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        readonly Dictionary<string,(int x,int y)> services=new Dictionary<string,(int,int)>();
        static readonly (int x,int y)[] steps={(0,1),(-1,0),(1,0),(0,-1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static TallyCompositionPlan Create(string id,int seed){if(!IsSupportedZone(id))throw new ArgumentException("Exact Tally surface address required.",nameof(id));return new TallyCompositionPlan(seed);}
        TallyCompositionPlan(int seed)
        {
            int form=FormationSelector.StableIndex(ZoneID+":formation:"+seed,3);
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            string[][] roles={new[]{"ExchangeHall","GoodsStore","RestCourt","RentalFrontage"},new[]{"RestCourt","ExchangeHall","GoodsStore","RentalFrontage"},new[]{"RentalFrontage","GoodsStore","ExchangeHall","RestCourt"}};
            FormationName=new[]{"ForkedExchangeApron","OffsetLoadingCourts","CoveredExchangeSpine"}[form];
            var rooms=new List<Room>();
            for(int i=0;i<4;i++)
            {
                bool upper=i<2,left=i%2==0;string role=roles[form][i];
                int w=role=="ExchangeHall"?25:role=="GoodsStore"?22:role=="RentalFrontage"?19:18;
                int h=upper?8:7;int x=(left?5:50)+rng.Next(3);int y=upper?1:17;
                var r=new Room(role,x,y,w,h,upper);rooms.Add(r);AddRoom(r);
            }
            Rooms=rooms.AsReadOnly();var goods=rooms.Find(r=>r.Role=="GoodsStore");var rest=rooms.Find(r=>r.Role=="RestCourt");
            Profile=new List<ProfilePlacement>{new ProfilePlacement("Chest",goods.X+goods.Width-4,goods.Y+2),new ProfilePlacement("Campfire",rest.X+rest.Width-4,rest.Y+rest.Height-3)}.AsReadOnly();
            foreach(var p in Profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)reserved[p.X+dx,p.Y+dy]=true;
            reserved[40,12]=true;
            // All four world edges meet the actual well through offset broad
            // approaches; longitudinal room connections add useful side forks.
            Route(0,12,36,11);Route(79,12,44,13);Route(40,0,39,11);Route(40,24,41,13);Route(36,11,44,13);
            foreach(var r in rooms)
            {
                Route(r.DoorX,r.DoorY,39,13);
                int iy=r.Y+3;
                Route(r.DoorX,r.DoorY,r.X+r.Width-3,iy);
                Route(r.X+2,iy,r.X+r.Width-3,iy);
            }
            foreach(var p in Profile)Route(p.X-1,p.Y,39,13);
            // This formation changes circulation, not just the role labels:
            // an open loading hall meets a wide exterior apron directly.
            if(FormationName=="CoveredExchangeSpine")
            {
                var hall=rooms.Find(r=>r.Role=="ExchangeHall");int outward=hall.DoorY==hall.Y?-1:1;
                for(int x=hall.X+2;x<hall.X+hall.Width-2;x++)for(int depth=0;depth<=3;depth++)Mark(x,hall.DoorY+outward*depth);
            }
            foreach(var r in rooms)
            {
                string bp=r.Role=="ExchangeHall"?"Merchant":r.Role=="RentalFrontage"?"Quartermaster":r.Role=="RestCourt"?"Innkeeper":null;
                if(bp==null)continue;
                bool found=false;
                for(int y=r.Y+2;y<r.Y+r.Height-2&&!found;y++)for(int x=r.X+5;x<r.X+r.Width-2&&!found;x++)
                    if(objects[x,y]==null&&!reserved[x,y]&&CanRoute(x,y)&&HasApproachNeighbor(x,y)){services[bp]=(x,y);found=true;}
                if(!found)throw new InvalidOperationException("Tally lacks a safe service frontage: "+bp);
            }
        }
        void AddRoom(Room r)
        {
            for(int y=r.Y;y<r.Y+r.Height;y++)for(int x=r.X;x<r.X+r.Width;x++)
            {
                bool edge=x==r.X||x==r.X+r.Width-1||y==r.Y||y==r.Y+r.Height-1;
                if(edge){bool loadingFace=r.Role=="ExchangeHall"&&FormationName=="CoveredExchangeSpine"&&x>=r.X+2&&x<r.X+r.Width-2;
                    if(y!=r.DoorY||(!loadingFace&&Math.Abs(x-r.DoorX)>1))Put(x,y,"TentWall");}
                else interior[x,y]=true;
            }
            if(r.Role=="GoodsStore")for(int k=0;k<4;k++)Put(r.X+2+k,r.Y+2,"Crate");
            else if(r.Role=="RestCourt"){Put(r.X+2,r.Y+2,"Bed");Put(r.X+4,r.Y+2,"Bed");Put(r.X+2,r.Y+4,"Chair");}
            else if(r.Role=="RentalFrontage"){Put(r.X+2,r.Y+2,"Crate");Put(r.X+3,r.Y+2,"Crate");Put(r.X+2,r.Y+4,"Chair");}
            else {Put(r.X+2,r.Y+2,"Crate");Put(r.X+3,r.Y+2,"Crate");Put(r.X+2,r.Y+4,"Chair");}
        }
        bool HasApproachNeighbor(int x,int y){foreach(var d in steps)if(IsApproach(x+d.x,y+d.y))return true;return false;}
        void Put(int x,int y,string bp){objects[x,y]=bp;reserved[x,y]=true;}
        bool CanRoute(int x,int y){if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;foreach(var p in Profile)if(p.X==x&&p.Y==y)return false;return true;}
        void Mark(int x,int y){if(CanRoute(x,y)){approach[x,y]=true;reserved[x,y]=true;}}
        void Route(int sx,int sy,int tx,int ty)
        {
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Tally route crosses fixture: "+sx+","+sy+" to "+tx+","+ty);
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int,int),(int,int)>();q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty))){var c=q.Dequeue();foreach(var d in steps){var n=(c.x+d.x,c.y+d.y);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}}
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Tally disconnected route.");
            for(var c=(tx,ty);;c=prev[c]){Mark(c.Item1,c.Item2);if(!interior[c.Item1,c.Item2]){Mark(c.Item1+1,c.Item2);Mark(c.Item1,c.Item2+1);}if(c==(sx,sy))break;}
        }
        public bool TryGetServiceCell(string blueprint,out int x,out int y){x=y=0;if(blueprint==null||!services.TryGetValue(blueprint,out var c))return false;x=c.x;y=c.y;return true;}
        static bool InBounds(int x,int y)=>x>=0&&x<80&&y>=0&&y<25;
        public string GroundAt(int x,int y)=>!InBounds(x,y)?null:interior[x,y]?"StoneFloor":approach[x,y]?"RoadStone":"Floor";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature(){var s=new StringBuilder(ZoneID).Append(':').Append(FormationName);for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");foreach(var p in Profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);return s.ToString();}
    }
}
