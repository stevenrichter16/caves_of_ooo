using System;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Three bounded cold-generation layouts in Sumphold's southern
    /// circuit. This plan describes construction only; native owners hold all
    /// damage, stock, harvest, crop and repair state after publication.</summary>
    public sealed class SoddenDistrictPlan
    {
        public const string StopZoneID="Overworld.15.7.0";
        public const string CrossingZoneID="Overworld.16.7.0";
        public const string WorksZoneID="Overworld.17.7.0";
        public string ZoneID {get;}
        public string Title {get;}
        readonly string[,] ground=new string[Zone.Width,Zone.Height];
        readonly string[,] objects=new string[Zone.Width,Zone.Height];
        readonly string[,] roles=new string[Zone.Width,Zone.Height];
        readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        readonly bool[,] interior=new bool[Zone.Width,Zone.Height];
        readonly int variation;

        public static bool IsSupportedZone(string id)=>id==StopZoneID||id==CrossingZoneID||id==WorksZoneID;
        public static SoddenDistrictPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("Sodden district plans require an exact circuit surface address.",nameof(id));
            return new SoddenDistrictPlan(id,seed);
        }
        SoddenDistrictPlan(string id,int seed)
        {
            ZoneID=id;variation=(int)(unchecked((uint)seed)%5u);
            Title=id==StopZoneID?"the dressing shelter":id==CrossingZoneID?"the cutbank crossing":"the abandoned peat works";
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)ground[x,y]="Grass";
            if(id==StopZoneID)Shelter();else if(id==CrossingZoneID)Crossing();else Works();
            // Adjacent chunks accept the actual arrival coordinate. Whole dry
            // edges and their inner shoulders preserve every existing transfer.
            for(int x=0;x<Zone.Width;x++){Lane(x,0,x,1);Lane(x,23,x,24);}
            for(int y=0;y<Zone.Height;y++){Lane(0,y,1,y);Lane(78,y,79,y);}
        }
        void Shelter()
        {
            Pool(5+variation,4,22,20,true);Pool(60,7,74-variation,21,true);Pool(47,3,64,6,true);
            Room(28,3,41,10,34,10,false);Room(44,16,58,22,50,16,false);
            Lane(0,12,79,12);Lane(40,0,40,2);Lane(42,2,42,15);Lane(40,2,42,2);
            Lane(42,15,40,15);Lane(40,15,40,24);Lane(34,10,34,12);Lane(50,12,50,16);
            Board(25,12,25,19);Board(25,19,40,19);Board(58,12,69,12);
            Put("SoddenRouteNotice",24,11,"notice");Put("SoddenDressingBench",34,6,"bench");Put("PeatCutter",35,6,"keeper");
            Put("SumpsieveCrop",47,9,"ripe-sumpsieve");Put("SumpsieveCrop",50,9,"dry-sumpsieve");
            Lane(46,10,51,10);Lane(47,10,47,12);Lane(50,10,50,12);
            Put("Duckboard",34,11);Put("Duckboard",50,15);
            Put("DeadTree",12+variation,3);Put("DeadTree",70-variation,5);
            Put("Bed",47,19,"bed");Put("Chair",55,19,"chair");
            ReedColonies((2,5),(2,16),(23,5),(23,15),(75,8),(75,17),(54,7));
        }
        void Crossing()
        {
            // The direct forty-cell crossing is wet. A continuous band, rather
            // than isolated puddles, makes the raised detour materially longer.
            Pool(21,5,59,19);
            for(int y=6;y<=18;y++)if(y<11||y>13){Put("PeatBank",26,y);Put("PeatBank",54,y);}
            Lane(0,12,20,12);Lane(60,12,79,12);
            Board(20,12,20,3);Board(20,3,60,3);Board(60,3,60,12);
            Lane(20,12,20,21);Lane(20,21,60,21);Lane(60,21,60,12);
            Lane(40,0,40,2);Lane(40,21,40,24);
            Pool(5+variation,4,14+variation,7);Pool(65-variation,17,74-variation,20);
            Put("DeadTree",64+variation,7);
            Put("SoddenRouteNotice",18,11,"notice");Put("Bandfrog",40,13,"frog");
            ReedColonies((7+variation,8),(12+variation,8),(18,14),(62,4),(62,13),(69-variation,14),(75-variation,18));
        }
        void Works()
        {
            // Staggered long cuts and a broken, stone-sided equipment shed form
            // a recognisable work yard rather than another residential court.
            Pool(7+variation,5,35,8);Pool(17,18,66-variation,21);
            for(int x=7+variation;x<=35;x++)Put("PeatBank",x,4);
            for(int x=17;x<=66-variation;x++)Put("PeatBank",x,17);
            Room(43,3,67,10,54,10,true);Room(11,14,31,20,20,14,true);
            Lane(0,12,79,12);Lane(40,0,40,24);Lane(54,10,54,12);Lane(20,12,20,14);
            Board(34,12,34,16);Board(34,16,59,16);Board(54,11,60,11);
            Put("SoddenRouteNotice",8,11,"notice");Put("SoddenWorksLocker",61,6,"locker");
            Put("SoddenWorksSalvage",27,16,"salvage");Put("MawToad",68,20,"toad");
            Put("DeadTree",70,17);Put("DeadTree",75,18);Put("DeadTree",72,22);
            Put("DeadTree",44,2);Put("DeadTree",69,9);
            ReedColonies((5+variation,5),(9+variation,9),(20,9),(31,9),(61,14),(67-variation,18));
        }
        void Room(int x0,int y0,int x1,int y1,int doorX,int doorY,bool damaged)
        {
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                ground[x,y]="StoneFloor";objects[x,y]=null;
                bool wall=x==x0||x==x1||y==y0||y==y1;
                bool gap=x==doorX&&y==doorY||damaged&&wall&&((x+variation*3+y)%11<2);
                if(wall&&!gap)Put("StoneWall",x,y);else interior[x,y]=!wall;
            }
        }
        void Pool(int x0,int y0,int x1,int y1,bool stepped=false)
        {
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                if(!stepped||Math.Min(x-x0,x1-x)+Math.Min(y-y0,y1-y)>=2)Put("MirePool",x,y);
        }
        // Small irregular native stands on already dry shore. Never replace
        // water, a useful owner, an interior or a route with decorative plants.
        void ReedColonies(params (int x,int y)[] anchors)
        {
            int[] shapes={53,58,45,29,61};
            for(int i=0;i<anchors.Length;i++)for(int trial=0;trial<shapes.Length;trial++)
            {
                int shape=shapes[(variation+i+trial)%shapes.Length];var at=anchors[i];bool fits=true;
                for(int dy=0;dy<3;dy++)for(int dx=0;dx<2;dx++)if((shape&(1<<(dy*2+dx)))!=0)
                    fits&=InBounds(at.x+dx,at.y+dy)&&GroundAt(at.x+dx,at.y+dy)=="Grass"
                        &&ObjectAt(at.x+dx,at.y+dy)==null&&!IsReserved(at.x+dx,at.y+dy);
                if(!fits)continue;
                for(int dy=0;dy<3;dy++)for(int dx=0;dx<2;dx++)if((shape&(1<<(dy*2+dx)))!=0)Put("Reeds",at.x+dx,at.y+dy);
                break;
            }
        }
        void Put(string bp,int x,int y,string role=null)
        {objects[x,y]=bp;roles[x,y]=role;}
        void Board(int x0,int y0,int x1,int y1)=>Lane(x0,y0,x1,y1,true);
        void Lane(int x0,int y0,int x1,int y1,bool board=false)
        {
            int count=Math.Max(Math.Abs(x1-x0),Math.Abs(y1-y0));
            for(int i=0;i<=count;i++)
            {
                int x=x0+(count==0?0:(x1-x0)*i/count),y=y0+(count==0?0:(y1-y0)*i/count);
                objects[x,y]=board?"Duckboard":null;roles[x,y]=null;approach[x,y]=true;
            }
        }
        public string GroundAt(int x,int y)=>InBounds(x,y)?ground[x,y]:null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public string RoleAt(int x,int y)=>InBounds(x,y)?roles[x,y]:null;
        public bool IsWet(int x,int y)=>ObjectAt(x,y)=="MirePool";
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsReserved(int x,int y)=>IsApproach(x,y)||RoleAt(x,y)!=null||IsWet(x,y)||IsInterior(x,y);
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interior[x,y];
        static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID+":");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append(ground[x,y]).Append('/').Append(objects[x,y]??"_").Append(IsApproach(x,y)?'!':'.').Append('|');
            return s.ToString();
        }
    }
}
