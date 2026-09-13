using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Generation-only arid land use. Native entities realize the plan
    /// once; rendering never replays it over player-modified terrain.</summary>
    public sealed class BeatingCompositionPlan
    {
        public readonly string ZoneID, Condition;
        public readonly int Seed, FocalX, FocalY, WestY, EastY, NorthX, SouthX;
        public readonly Formation Formation;
        public readonly bool RoadEastWest;
        private readonly int worldX,worldY,wind;
        private readonly double phase;
        private readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        private readonly bool[,] road=new bool[Zone.Width,Zone.Height];
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly Ruin[] ruins;
        private readonly (int x,int y)[] plates;
        private readonly (int x,int y,double rx,double ry)[] lenses;
        public int RuinCount => Formation==Formation.RuinField?ruins.Length:0;
        public Ruin GetRuin(int index) => ruins[index];
        public readonly struct Ruin
        {
            public readonly int X,Y,Width,Height;
            public Ruin(int x,int y,int width,int height){X=x;Y=y;Width=width;Height=height;}
            public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
            public bool Boundary(int x,int y)=>Contains(x,y)&&(x==X||x==X+Width-1||y==Y||y==Y+Height-1);
        }
        private static readonly HashSet<string> wilderness=BuildIndex();
        public static bool IsWildernessZone(string id)=>id!=null&&wilderness.Contains(id);
        private static HashSet<string> BuildIndex()
        {
            var set=new HashSet<string>(StringComparer.Ordinal);
            for(int x=0;x<20;x++)for(int y=0;y<20;y++)
            {
                string id=WorldMap.ToZoneID(x,y);
                if(WorldMapAuthoring.BiomeAt(x,y)==BiomeType.Beating&&!WorldMapAuthoring.PlaceAt(x,y).HasValue
                    &&!SinkholeSites.IsMouth(x,y)&&id!=OverworldZoneManager.TenthFireZoneID
                    &&id!=OverworldZoneManager.AbandonedCounterZoneA&&id!=OverworldZoneManager.AbandonedCounterZoneB)set.Add(id);
            }
            return set;
        }
        /// <summary>Surface addresses support forced preview formations;
        /// runtime routing additionally enforces wilderness and POI eligibility.</summary>
        public static BeatingCompositionPlan Create(string id,int seed,Formation formation=Formation.None)
        {
            var p=WorldMap.FromZoneID(id);
            if(!WorldMapAuthoring.InBounds(p.x,p.y)||p.z!=0)
                throw new ArgumentException("Beating plans require an in-bounds surface address.",nameof(id));
            return new BeatingCompositionPlan(id,seed,p.x,p.y,formation);
        }
        private BeatingCompositionPlan(string id,int seed,int wx,int wy,Formation form)
        {
            ZoneID=id;Seed=seed;worldX=wx;worldY=wy;
            Formation=form==Formation.None?FormationSelector.For(BiomeType.Beating,id):form;
            if(Formation<Formation.SaltPan||Formation>Formation.BrineLens)throw new ArgumentException("Unsupported Beating formation.",nameof(form));
            var rng=new Random(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
            Condition=new[]{"saltward wind","scoured ground","drift-laden"}[rng.Next(3)];
            wind=rng.Next(2)==0?-1:1;phase=rng.NextDouble()*Math.PI*2;RoadEastWest=rng.Next(100)<70;
            FocalX=34+rng.Next(13);FocalY=10+rng.Next(5);
            WestY=6+Hash(wx,wy,11)%13;EastY=6+Hash(wx+1,wy,11)%13;
            NorthX=12+Hash(wx,wy,23)%56;SouthX=12+Hash(wx,wy+1,23)%56;
            Route(0,WestY,18,WestY);Route(18,WestY,FocalX,FocalY);
            Route(79,EastY,62,EastY);Route(62,EastY,FocalX,FocalY);
            Route(NorthX,0,FocalX,FocalY);Route(SouthX,24,FocalX,FocalY);
            ruins=new Ruin[3];
            for(int i=0;i<3;i++)
            {
                int x=5+i*25+rng.Next(4),y=(i%2==0?3:13)+rng.Next(2);
                ruins[i]=new Ruin(x,y,11+rng.Next(5),6+rng.Next(3));
                if(Formation==Formation.RuinField)Route(x+ruins[i].Width/2,y+ruins[i].Height/2,FocalX,FocalY,paved:true);
            }
            plates=new (int,int)[10];
            for(int i=0;i<plates.Length;i++)plates[i]=(5+i%5*15+rng.Next(8),4+i/5*13+rng.Next(5));
            lenses=new (int,int,double,double)[3];
            for(int i=0;i<3;i++)lenses[i]=(16+i*24+rng.Next(4),7+(i%2)*10+rng.Next(2),8+rng.Next(5),3+rng.Next(2));
            BuildRoad();
            // Water establishes the landform before dry travel is routed.
            // Generic arrival spokes must not slice native basins into strips.
            if(Formation==Formation.BrineLens)Array.Clear(approach,0,approach.Length);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(IsRoad(x,y)||(Formation!=Formation.BrineLens&&IsFocalClearing(x,y)))approach[x,y]=true;
                objects[x,y]=ComposeCell(x,y);
            }
            if(Formation==Formation.BrineLens)
            {
                // Choose the nearest dry arrival centre with a three-cell
                // clearance; the immutable focal fields then describe this site.
                int best=int.MaxValue,bx=FocalX,by=FocalY;
                for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
                {
                    int d=(x-FocalX)*(x-FocalX)+(y-FocalY)*(y-FocalY);
                    if(d<best&&DryCentre(x,y)){best=d;bx=x;by=y;}
                }
                if(best==int.MaxValue)throw new InvalidOperationException("No dry arrival space between brine basins.");
                FocalX=bx;FocalY=by;
                RouteAroundBrine();
            }
            RepairOpenPockets();
            PlaceActivity(rng);
        }
        private bool IsFocalClearing(int x,int y)=>Math.Pow((x-FocalX)/6.0,2)+Math.Pow((y-FocalY)/2.5,2)<1;
        private double RoadCentre(int along)
        {
            double t=along/(RoadEastWest?79.0:24.0);
            return RoadEastWest?WestY+(EastY-WestY)*t+Math.Sin(t*Math.PI)*Math.Sin(phase+along*.065)*1.3
                :NorthX+(SouthX-NorthX)*t+Math.Sin(t*Math.PI)*Math.Sin(phase+along*.15)*2;
        }
        // Rasterize a connected polyline rather than sampling one cross-section
        // per row: steep N/S roads otherwise skip cells (adversarial seed38).
        // Store paving once so native placement and cosmetic queries agree.
        private void BuildRoad()
        {
            if(Formation!=Formation.CaravanRoad)return;
            int end=RoadEastWest?Zone.Width-1:Zone.Height-1;
            int px=RoadEastWest?0:NorthX,py=RoadEastWest?WestY:0;
            for(int along=0;along<=end;along++)
            {
                int centre=(int)Math.Round(RoadCentre(along));
                int x=RoadEastWest?along:centre,y=RoadEastWest?centre:along;
                int steps=Math.Max(Math.Abs(x-px),Math.Abs(y-py));
                for(int step=0;step<=steps;step++)
                {
                    double t=steps==0?0:step/(double)steps;
                    int sx=(int)Math.Round(px+(x-px)*t),sy=(int)Math.Round(py+(y-py)*t);
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                        if(InBounds(sx+dx,sy+dy))road[sx+dx,sy+dy]=true;
                }
                px=x;py=y;
            }
            // A passing bay belongs on the road itself, not merely in the
            // general arrival clearing. Its paved five-cell core stays dry.
            int midpoint=end/2;
            int cx=RoadEastWest?midpoint:(int)Math.Round(RoadCentre(midpoint));
            int cy=RoadEastWest?(int)Math.Round(RoadCentre(midpoint)):midpoint;
            double rx=RoadEastWest?6:3.8,ry=RoadEastWest?3.8:5;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(Math.Pow((x-cx)/rx,2)+Math.Pow((y-cy)/ry,2)<1)road[x,y]=true;
        }
        /// <summary>Connected native paving, including the local passing bay.</summary>
        public bool IsRoad(int x,int y)=>InBounds(x,y)&&road[x,y];
        private string ComposeCell(int x,int y)
        {
            if(approach[x,y]||x<2||x>77||y<2||y>22)return null;
            switch(Formation)
            {
                case Formation.SaltPan:
                    double first=double.MaxValue,second=double.MaxValue;
                    foreach(var p in plates)
                    {
                        double d=Math.Pow(x-p.x,2)+Math.Pow((y-p.y)*1.5,2);
                        if(d<first){second=first;first=d;}else if(d<second)second=d;
                    }
                    return second-first<18&&Roll(x,y,31)<72?"SaltCrust":null;
                case Formation.RuinField:
                    foreach(var r in ruins)
                    {
                        if(!r.Contains(x,y))continue;
                        if(!r.Boundary(x,y))return null;
                        bool gate=(Math.Abs(x-(r.X+r.Width/2))<=1&&(y==r.Y||y==r.Y+r.Height-1));
                        return !gate&&Roll(x,y,37)<88?"SandstoneWall":null;
                    }
                    return null;
                case Formation.DuneBelt:
                    for(int i=0;i<3;i++)
                    {
                        int start=5+i*4,end=76-i*5;
                        if(x<start||x>end)continue;
                        double ridge=5+i*7+Math.Sin(x*.085+phase+i*1.4)*1.6*wind;
                        double taper=Math.Min(1,Math.Min((x-start+1)/5.0,(end-x+1)/5.0));
                        double width=(1.85+.6*Math.Pow(Math.Sin(x*.11+phase+i),2))*taper;
                        int saddle=14+i*22+Hash(worldX,worldY,41+i)%9;
                        if(Math.Abs(y-ridge)<width&&Math.Abs(x-saddle)>2)return "DuneCrest";
                    }
                    return null;
                case Formation.CaravanRoad: return null;
                case Formation.WindBarrens:
                    for(int i=0;i<4;i++)
                    {
                        int cx=10+i*19,cy=5+(i%2)*13;
                        double core=Math.Pow((x-cx)/6.0,2)+Math.Pow((y-cy)/3.0,2);
                        double lee=Math.Pow((x-cx-wind*4)/8.0,2)+Math.Pow((y-cy)/3.5,2);
                        if(core<1&&Roll(x,y,47)<23)return "Rock";
                        if(lee<1.1&&Roll(x,y,53)<(Condition=="scoured ground"?26:42))return "DryBrush";
                    }
                    return null;
                case Formation.BrineLens:
                    double basin=double.MaxValue;
                    foreach(var p in lenses)basin=Math.Min(basin,Math.Pow((x-p.x)/p.rx,2)+Math.Pow((y-p.y)/p.ry,2));
                    if(basin<.82)return "BrinePool";
                    if(basin<1.3&&Roll(x,y,59)<78)return "SaltCrust";
                    return null;
                default: return null;
            }
        }
        private void PlaceActivity(Random rng)
        {
            if(Formation!=Formation.SaltPan&&Formation!=Formation.CaravanRoad)return;
            var candidates=new List<(int x,int y)>();
            for(int y=3;y<22;y++)for(int x=3;x<77;x++)
            {
                if(approach[x,y]||objects[x,y]!=null)continue;
                if(Formation==Formation.CaravanRoad && Math.Abs((RoadEastWest?y:x)-RoadCentre(RoadEastWest?x:y))>4)continue;
                candidates.Add((x,y));
            }
            int count=Formation==Formation.SaltPan?2+rng.Next(3):RoadEastWest?4:2;
            for(int i=0;i<count&&candidates.Count>0;i++)
            {
                int pick=rng.Next(candidates.Count);var c=candidates[pick];
                objects[c.x,c.y]=Formation==Formation.SaltPan?"PaleSaltVein":"Signpost";
                // Spaced activity objects cannot create a four-solid ring around
                // an open cell after terrain reachability repair.
                candidates.RemoveAll(p=>Math.Abs(p.x-c.x)<4&&Math.Abs(p.y-c.y)<4);
            }
            if(Formation==Formation.CaravanRoad)
                for(int i=0;i<3&&candidates.Count>0;i++)
                {int pick=rng.Next(candidates.Count);var c=candidates[pick];candidates.RemoveAt(pick);objects[c.x,c.y]="Bones";}
        }
        private void Route(int ax,int ay,int bx,int by,bool paved=false)
        {
            int steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=steps;i++)
            {
                double t=steps==0?0:i/(double)steps;int x=(int)Math.Round(ax+(bx-ax)*t),y=(int)Math.Round(ay+(by-ay)*t);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(InBounds(x+dx,y+dy))
                {approach[x+dx,y+dy]=true;objects[x+dx,y+dy]=null;if(paved)road[x+dx,y+dy]=true;}
            }
        }
        private bool DryCentre(int x,int y)
        {
            if(!InBounds(x,y))return false;
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                if(InBounds(x+dx,y+dy)&&objects[x+dx,y+dy]=="BrinePool")return false;
            return true;
        }
        private void RouteAroundBrine()
        {
            var previous=new int[Zone.Width,Zone.Height];
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)previous[x,y]=-1;
            var queue=new Queue<(int x,int y)>();queue.Enqueue((FocalX,FocalY));
            previous[FocalX,FocalY]=FocalY*Zone.Width+FocalX;
            while(queue.Count>0)
            {
                var c=queue.Dequeue();
                for(int d=0;d<4;d++)
                {
                    int x=c.x+(d==0?1:d==1?-1:0),y=c.y+(d==2?1:d==3?-1:0);
                    if(!DryCentre(x,y)||previous[x,y]>=0)continue;
                    previous[x,y]=c.y*Zone.Width+c.x;queue.Enqueue((x,y));
                }
            }
            foreach(var entry in new[]{(x:0,y:WestY),(x:79,y:EastY),(x:NorthX,y:0),(x:SouthX,y:24)})
            {
                int x=entry.x,y=entry.y;
                if(previous[x,y]<0)throw new InvalidOperationException("Brine basins leave no dry entry route.");
                while(true)
                {
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                        if(InBounds(x+dx,y+dy))approach[x+dx,y+dy]=true;
                    if(x==FocalX&&y==FocalY)break;
                    int p=previous[x,y];x=p%Zone.Width;y=p/Zone.Width;
                }
            }
        }
        private static bool Solid(string bp)=>bp=="SandstoneWall"||bp=="DuneCrest"||bp=="Rock";
        private void RepairOpenPockets()
        {
            var seen=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y)>();
            for(int pass=0;pass<Zone.Width*Zone.Height;pass++)
            {
                Array.Clear(seen,0,seen.Length);q.Clear();seen[FocalX,FocalY]=true;q.Enqueue((FocalX,FocalY));
                while(q.Count>0)
                {
                    var c=q.Dequeue();
                    for(int d=0;d<4;d++)
                    {
                        int x=c.x+(d==0?1:d==1?-1:0),y=c.y+(d==2?1:d==3?-1:0);
                        if(!InBounds(x,y)||seen[x,y]||Solid(objects[x,y]))continue;
                        seen[x,y]=true;q.Enqueue((x,y));
                    }
                }
                bool repaired=false;
                for(int y=0;y<Zone.Height&&!repaired;y++)for(int x=0;x<Zone.Width;x++)
                {if(seen[x,y]||Solid(objects[x,y]))continue;Route(x,y,FocalX,FocalY);repaired=true;break;}
                if(!repaired)return;
            }
            throw new InvalidOperationException("Beating repair exceeded finite cell budget.");
        }
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        public bool IsWater(int x,int y)=>ObjectAt(x,y)=="BrinePool";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public string GroundAt(int x,int y)
        {
            if(Formation==Formation.RuinField)foreach(var room in ruins)if(room.Contains(x,y))return "SandstoneFloor";
            if(IsRoad(x,y))return "RoadStone";
            return "Sand";
        }
        private int Roll(int x,int y,int salt)=>Hash(worldX*Zone.Width+x,worldY*Zone.Height+y,salt)%100;
        private int Hash(int x,int y,int salt)
        {
            unchecked{uint h=(uint)Seed^2166136261u;h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;
                h=(h^(uint)salt)*16777619u;h^=h>>16;h*=2246822519u;h^=h>>13;return (int)(h&0x7fffffffu);}
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(Formation+":"+Condition+":"+RoadEastWest+":");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)s.Append(GroundAt(x,y)).Append('/').Append(objects[x,y]??(approach[x,y]?"_":".")).Append('|');
            return s.ToString();
        }
    }
}
