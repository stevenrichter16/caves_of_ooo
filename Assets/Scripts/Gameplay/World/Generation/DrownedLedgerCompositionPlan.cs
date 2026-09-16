using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original excavation grammar. Wet bays are real native water;
    /// working boards lie on dry land. The three silent witnesses and reading
    /// table remain independent owners, with no new body-reading simulation.</summary>
    public sealed class DrownedLedgerCompositionPlan
    {
        public const string ZoneID="Overworld.17.5.0";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int w,int h){Role=role;X=x;Y=y;Width=w;Height=h;DoorX=x+w/2;DoorY=y+h-1;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        public readonly struct ExcavationBay
        {
            public readonly int X,Y,Width,Height;
            internal ExcavationBay(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}
        }
        public IReadOnlyList<Room> Rooms{get;}
        public IReadOnlyList<ProfilePlacement> Profile{get;}
        public IReadOnlyList<ExcavationBay> Bays{get;}
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interior=new bool[80,25],wet=new bool[80,25],approach=new bool[80,25],reserved=new bool[80,25];
        private static readonly (int x,int y)[] steps={(0,-1),(-1,0),(1,0),(0,1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static DrownedLedgerCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("Exact Drowned Ledger surface address required.",nameof(id));return new DrownedLedgerCompositionPlan(seed);}
        private DrownedLedgerCompositionPlan(int seed)
        {
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            var rooms=new List<Room>{new Room("ReadingTent",8+rng.Next(3),2,18+rng.Next(3),8),
                new Room("SupplyHouse",37+rng.Next(3),2,13,7),new Room("WorkersHouse",62+rng.Next(2),3,12,6)};
            Rooms=rooms.AsReadOnly();
            foreach(var room in rooms)AddRoom(room);
            var bays=new List<ExcavationBay>{new ExcavationBay(6+rng.Next(2),17,17+rng.Next(2),7),
                new ExcavationBay(34+rng.Next(2),18,13+rng.Next(2),6),new ExcavationBay(58+rng.Next(2),14,16+rng.Next(2),7)};
            Bays=bays.AsReadOnly();foreach(var bay in bays)AddBay(bay,rng);
            var tent=rooms[0];var left=bays[0];var right=bays[2];
            var profile=new List<ProfilePlacement>{
                new ProfilePlacement("PreFellingBody",tent.X+8,tent.Y+4),
                new ProfilePlacement("PreFellingBody",left.X+left.Width/2,left.Y-2),
                new ProfilePlacement("PreFellingBody",right.X+right.Width/2,right.Y-2),
                new ProfilePlacement("SurveyStake",bays[0].X-1,bays[0].Y-1),
                new ProfilePlacement("SurveyStake",bays[1].X-1,bays[1].Y-1),
                new ProfilePlacement("SurveyStake",bays[2].X-1,bays[2].Y-1),
                new ProfilePlacement("ReadingTable",tent.X+8,tent.Y+3),
                new ProfilePlacement("RecensionScribe",tent.X+12,tent.Y+4),
                new ProfilePlacement("CurationSorter",tent.DoorX+4,tent.DoorY+2)};
            Profile=profile.AsReadOnly();
            foreach(var p in profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)reserved[p.X+dx,p.Y+dy]=true;
            // Low foreground cuts expose the two bodies from the actual camera.
            // Water stays intact behind the three-cell break in the dry head face.
            foreach(var p in profile)if(p.Blueprint=="PreFellingBody"&&!interior[p.X,p.Y])
                for(int x=p.X-1;x<=p.X+1;x++)if(objects[x,p.Y+1]=="PeatBank")objects[x,p.Y+1]=null;
            // Native village population installs the main well at exactly40,12.
            // Keep its commons dry and routes on its reachable standing sides.
            for(int y=10;y<=14;y++)for(int x=35;x<=45;x++)MarkApproach(x,y,false);
            reserved[40,12]=true;
            Route(0,12,39,12,false,1);Route(79,12,41,12,false,1);
            Route(40,0,40,10,false,1);Route(30,24,30,12,true,1);
            foreach(var r in rooms)Route(r.DoorX,r.DoorY,r.DoorX==40?39:r.DoorX,12,false,1);
            foreach(var p in profile)Route(p.X-1,p.Y,39,11,p.Blueprint=="PreFellingBody"&&!interior[p.X,p.Y],0);
            // One survey route reaches the unoccupied middle bay too; no invented
            // fourth witness is added to make its excavation visually busy.
            Route(bays[1].X+bays[1].Width/2,bays[1].Y-2,41,13,true,0);
            // Three activity-free wet margins, not uniform whole-map scatter.
            AddColony(bays[0].X-2,bays[0].Y+3,rng);
            AddColony(bays[1].X+bays[1].Width+1,bays[1].Y+2,rng);
            AddColony(bays[2].X+bays[2].Width+1,bays[2].Y+3,rng);
            for(int y=14;y<24;y++)for(int x=3;x<78;x++)
                if(!wet[x,y]&&!reserved[x,y]&&objects[x,y]==null&&NearWater(x,y)&&rng.Next(8)==0){objects[x,y]="Reeds";reserved[x,y]=true;}
        }
        private void AddRoom(Room r)
        {
            bool tent=r.Role=="ReadingTent";
            for(int y=r.Y;y<r.Y+r.Height;y++)for(int x=r.X;x<r.X+r.Width;x++)
            {
                bool edge=x==r.X||x==r.X+r.Width-1||y==r.Y||y==r.Y+r.Height-1;
                bool door=y==r.DoorY&&Math.Abs(x-r.DoorX)<=1;
                if(edge&&!door){objects[x,y]=tent?"TentWall":"SandstoneWall";reserved[x,y]=true;}
                if(!edge){interior[x,y]=true;if(tent)reserved[x,y]=true;}
                if(door){approach[x,y]=true;reserved[x,y]=true;}
            }
            objects[r.X+2,r.Y+2]=r.Role=="WorkersHouse"?"Bed":"Crate";reserved[r.X+2,r.Y+2]=true;
            if(tent)
            {
                objects[r.X+3,r.Y+2]="Crate";reserved[r.X+3,r.Y+2]=true;
                objects[r.X+2,r.Y+3]="Crate";reserved[r.X+2,r.Y+3]=true;
            }
            objects[r.X+r.Width-3,r.Y+r.Height-3]="Chair";reserved[r.X+r.Width-3,r.Y+r.Height-3]=true;
        }
        private void AddBay(ExcavationBay b,Random rng)
        {
            for(int y=b.Y;y<b.Y+b.Height;y++)
            {
                int inset=y==b.Y||y==b.Y+b.Height-1?1:0;
                int eastInset=(y-b.Y)%3==0?rng.Next(2):0;
                for(int x=b.X+inset;x<b.X+b.Width-inset-eastInset;x++)
                {objects[x,y]="WaterPuddle";wet[x,y]=true;reserved[x,y]=true;}
            }
            // A single tool-cut head face leaves the two ends open. This avoids
            // dry pockets and does not imply depth/bridge physics not in the game.
            for(int x=b.X+2;x<b.X+b.Width-2;x++){objects[x,b.Y-1]="PeatBank";reserved[x,b.Y-1]=true;}
        }
        private bool NearWater(int x,int y)=>IsWet(x-1,y)||IsWet(x+1,y)||IsWet(x,y-1)||IsWet(x,y+1);
        private void AddColony(int x,int y,Random rng)
        {
            if(!InBounds(x,y)||wet[x,y]||reserved[x,y]||objects[x,y]!=null)throw new InvalidOperationException("Ledger colony overlaps work.");
            objects[x,y]="DeadTree";reserved[x,y]=true;
            var candidates=new List<(int x,int y)>();
            for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
            {
                int xx=x+dx,yy=y+dy;
                if(InBounds(xx,yy)&&!wet[xx,yy]&&!reserved[xx,yy]&&!interior[xx,yy]&&objects[xx,yy]==null)candidates.Add((xx,yy));
            }
            int count=Math.Min(8,candidates.Count);
            for(int i=0;i<count;i++)
            {int at=rng.Next(candidates.Count);var c=candidates[at];candidates.RemoveAt(at);objects[c.x,c.y]="Reeds";reserved[c.x,c.y]=true;}
        }
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||wet[x,y]||(objects[x,y]!=null&&objects[x,y]!="Duckboard")||(x==40&&y==12))return false;
            foreach(var p in Profile)if(p.X==x&&p.Y==y)return false;return true;
        }
        private void MarkApproach(int x,int y,bool board)
        {if(!CanRoute(x,y))return;approach[x,y]=true;reserved[x,y]=true;if(board&&!interior[x,y]&&LocalBoardArea(x,y))objects[x,y]="Duckboard";}
        private bool LocalBoardArea(int x,int y)
        {
            if(x>=35&&x<=45&&y>=10&&y<=14)return false;
            if(x>=28&&x<=32&&y>=14)return true;
            foreach(var b in Bays)if(x>=b.X-2&&x<=b.X+b.Width+1&&y>=b.Y-3&&y<=b.Y-1)return true;
            return false;
        }
        private void Route(int sx,int sy,int tx,int ty,bool board,int radius)
        {
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Ledger destination overlaps a protected owner.");
            var q=new Queue<(int x,int y)>();var prev=new Dictionary<(int,int),(int,int)>();q.Enqueue((sx,sy));prev[(sx,sy)]=(sx,sy);
            while(q.Count>0&&!prev.ContainsKey((tx,ty)))
            {
                var c=q.Dequeue();foreach(var d in steps)
                {var n=(c.x+d.x,c.y+d.y);if(!CanRoute(n.Item1,n.Item2)||prev.ContainsKey(n))continue;prev[n]=c;q.Enqueue(n);}
            }
            if(!prev.ContainsKey((tx,ty)))throw new InvalidOperationException("Ledger dry destination is inaccessible.");
            for(var c=(tx,ty);;c=prev[c])
            {for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)MarkApproach(c.Item1+dx,c.Item2+dy,board);if(c==(sx,sy))break;}
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string GroundAt(int x,int y)=>InBounds(x,y)?interior[x,y]?"StoneFloor":"Floor":null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];
        public bool IsWet(int x,int y)=>InBounds(x,y)&&wet[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public string Signature()
        {var s=new StringBuilder(ZoneID);for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");foreach(var p in Profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);return s.ToString();}
    }
}
