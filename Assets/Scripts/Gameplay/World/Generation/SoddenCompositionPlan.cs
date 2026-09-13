using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Generation-only floodland intent. Cached cell masks describe
    /// water, work faces and dry approaches; native entities own all later changes.</summary>
    public sealed class SoddenCompositionPlan
    {
        public readonly string ZoneID, Condition;
        public readonly int Seed, FocalX, FocalY, WestY, EastY, NorthX, SouthX;
        public readonly Formation Formation;
        private readonly int worldX, worldY;
        private readonly bool mirrored;
        private readonly double phase;
        private readonly bool[,] approach = new bool[Zone.Width, Zone.Height];
        private readonly bool[,] wet = new bool[Zone.Width, Zone.Height];
        private readonly string[,] objects = new string[Zone.Width, Zone.Height];
        private readonly Basin[] basins;
        private readonly struct Basin
        {
            public readonly int X, Y;
            public Basin(int x, int y) { X = x; Y = y; }
            public double Distance(int x, int y) => Math.Pow((x-X)/16.0,2)+Math.Pow((y-Y)/6.0,2);
        }
        private static readonly HashSet<string> wilderness = BuildIndex();
        public static bool IsWildernessZone(string id) => id != null && wilderness.Contains(id);
        private static HashSet<string> BuildIndex()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            for(int x=0;x<20;x++) for(int y=0;y<20;y++)
                if(WorldMapAuthoring.BiomeAt(x,y)==BiomeType.Sodden && !WorldMapAuthoring.PlaceAt(x,y).HasValue
                    && !SinkholeSites.IsMouth(x,y)) result.Add(WorldMap.ToZoneID(x,y));
            return result;
        }
        /// <summary>Accepts surface addresses for preview overrides. Runtime
        /// eligibility is stricter and uses IsWildernessZone plus POI routing.</summary>
        public static SoddenCompositionPlan Create(string id,int seed,Formation formation=Formation.None)
        {
            var p=WorldMap.FromZoneID(id);
            if(!WorldMapAuthoring.InBounds(p.x,p.y)||p.z!=0)
                throw new ArgumentException("Sodden plans require an in-bounds surface address.",nameof(id));
            return new SoddenCompositionPlan(id,seed,p.x,p.y,formation);
        }
        private SoddenCompositionPlan(string id,int seed,int wx,int wy,Formation form)
        {
            ZoneID=id; Seed=seed; worldX=wx; worldY=wy;
            Formation=form==Formation.None?FormationSelector.For(BiomeType.Sodden,id):form;
            if(Formation<Formation.OpenMire||Formation>Formation.BogFace)
                throw new ArgumentException("Unsupported Sodden formation.",nameof(form));
            var rng=new Random(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
            Condition=new[]{"high water","receding water","old workings"}[rng.Next(3)];
            mirrored=rng.Next(2)==0; phase=rng.NextDouble()*Math.PI*2;
            FocalX=34+rng.Next(13); FocalY=10+rng.Next(5);
            // Shared-boundary hashing matches the recovered-country portal convention.
            WestY=6+Hash(wx,wy,11)%13; EastY=6+Hash(wx+1,wy,11)%13;
            NorthX=12+Hash(wx,wy,23)%56; SouthX=12+Hash(wx,wy+1,23)%56;
            basins=new Basin[3];
            for(int i=0;i<3;i++) basins[i]=new Basin(14+i*25+rng.Next(5),4+(i%2)*11+rng.Next(4));
            Route(0,WestY,18,WestY); Route(18,WestY,FocalX,FocalY);
            Route(79,EastY,62,EastY); Route(62,EastY,FocalX,FocalY);
            Route(NorthX,0,NorthX,5); Route(NorthX,5,FocalX,FocalY);
            Route(SouthX,24,SouthX,19); Route(SouthX,19,FocalX,FocalY);
            for(int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
            {
                if(Formation==Formation.Causeway && Math.Abs(y-BoardY(x))<1.5) approach[x,y]=true;
                if(IsIsland(x,y)) approach[x,y]=true;
                ComposeCell(x,y);
            }
            RepairOpenPockets();
            SeatResidents(rng);
        }
        private double BoardY(int x) => WestY+(EastY-WestY)*x/79.0+Math.Sin(x*Math.PI/79)*Math.Sin(x*.075+phase)*1.6;
        private int LocalY(int y) => mirrored?24-y:y;
        private bool IsIsland(int x,int y) => Math.Pow((x-FocalX)/7.0,2)+Math.Pow((y-FocalY)/3.0,2)<1;
        private void ComposeCell(int x,int y)
        {
            if(Formation==Formation.Causeway && Math.Abs(y-BoardY(x))<1.5)
            { objects[x,y]="Duckboard"; return; }
            if(approach[x,y] || x<2||x>77||y<2||y>22) return;
            int ly=LocalY(y); double basin=double.MaxValue;
            foreach(var b in basins) basin=Math.Min(basin,b.Distance(x,ly));
            double edge=1+Math.Sin(x*.2+ly*.3+phase)*.12;
            double waterline=Condition=="high water"?1.2:Condition=="receding water"?.87:1.02;
            switch(Formation)
            {
                case Formation.OpenMire:
                    wet[x,y]=basin<edge*waterline;
                    if(!wet[x,y] && basin<edge*waterline+.35 && Roll(x,y,41)<32) objects[x,y]="Reeds";
                    break;
                case Formation.PeatCuts:
                    for(int i=0;i<3;i++)
                    {
                        int cut=4+i*7, start=6+i*4+Hash(worldX,worldY,51+i)%7, end=72-i*5;
                        if(x<start||x>end)continue;
                        if(ly==cut)objects[x,y]="PeatBank";
                        if(ly>cut&&ly<=cut+2)wet[x,y]=true;
                    }
                    if(objects[x,y]==null&&!wet[x,y]&&basin<.7&&Roll(x,y,61)<10)objects[x,y]="Reeds";
                    break;
                case Formation.ReedMaze:
                    double braid=Math.Sin(x*.13+ly*.36+phase)+Math.Cos(ly*.52-phase)*.55;
                    wet[x,y]=basin<waterline && braid<.35;
                    if(basin<1.7&&braid>.1&&Roll(x,y,67)<70)objects[x,y]="Reeds";
                    break;
                case Formation.DrownedCopse:
                    wet[x,y]=basin<edge*1.2;
                    if(basin<1.3 && x%3==Hash(worldX,worldY,71)%3 && y%2==0 && Roll(x,y,73)<53)objects[x,y]="DeadTree";
                    else if(basin>1&&basin<1.6&&Roll(x,y,79)<20)objects[x,y]="Reeds";
                    break;
                case Formation.Causeway:
                    wet[x,y]=Math.Abs(y-BoardY(x))<6.5 && (basin<1.8||Math.Abs(y-BoardY(x))<3.5);
                    if(!wet[x,y]&&basin<1.7&&Roll(x,y,83)<32)objects[x,y]="Reeds";
                    break;
                case Formation.BogFace:
                    int face=6+(int)Math.Floor(x/18.0)+Hash(worldX,worldY,89)%3;
                    if(x>5&&x<74)
                    {
                        if(ly==face||ly==face+1)objects[x,y]="PeatBank";
                        if(ly>=face+2&&ly<=face+4)wet[x,y]=true;
                    }
                    if(objects[x,y]==null&&!wet[x,y]&&ly>17&&Roll(x,y,97)<14)objects[x,y]="Reeds";
                    break;
            }
            // A native pool owns its surface, exposure and destruction. Reeds
            // use separate shoreline cells, avoiding hidden hazardous co-occupancy.
            if(wet[x,y]&&Formation!=Formation.DrownedCopse)objects[x,y]="MirePool";
        }
        private void Route(int ax,int ay,int bx,int by)
        {
            int steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=steps;i++)
            {
                double t=steps==0?0:i/(double)steps;
                int x=(int)Math.Round(ax+(bx-ax)*t), y=(int)Math.Round(ay+(by-ay)*t);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(InBounds(x+dx,y+dy))
                {
                    approach[x+dx,y+dy]=true;wet[x+dx,y+dy]=false;
                    if(objects[x+dx,y+dy]!="Duckboard")objects[x+dx,y+dy]=null;
                }
            }
        }
        private static bool Solid(string bp) => bp=="PeatBank"||bp=="DeadTree";
        private void RepairOpenPockets()
        {
            var seen=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();
            for(int pass=0;pass<Zone.Width*Zone.Height;pass++)
            {
                Array.Clear(seen,0,seen.Length);queue.Clear();queue.Enqueue((FocalX,FocalY));seen[FocalX,FocalY]=true;
                while(queue.Count>0)
                {
                    var c=queue.Dequeue();
                    for(int d=0;d<4;d++)
                    {
                        int x=c.x+(d==0?1:d==1?-1:0),y=c.y+(d==2?1:d==3?-1:0);
                        if(!InBounds(x,y)||seen[x,y]||Solid(objects[x,y]))continue;
                        seen[x,y]=true;queue.Enqueue((x,y));
                    }
                }
                bool repaired=false;
                for(int y=0;y<Zone.Height&&!repaired;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(seen[x,y]||Solid(objects[x,y]))continue;
                    Route(x,y,FocalX,FocalY);repaired=true;break;
                }
                if(!repaired)return;
            }
            throw new InvalidOperationException("Sodden pocket repair exceeded finite cell budget.");
        }
        private void SeatResidents(Random rng)
        {
            bool copse=Formation==Formation.DrownedCopse;
            if(!copse&&Formation!=Formation.PeatCuts&&Formation!=Formation.BogFace)return;
            var candidates=new List<(int x,int y)>();
            for(int y=2;y<23;y++)for(int x=2;x<78;x++)
            {
                if(approach[x,y]||objects[x,y]!=null||(!copse&&wet[x,y]))continue;
                string neighbor=copse?"DeadTree":"PeatBank";
                if(objects[x-1,y]==neighbor||objects[x+1,y]==neighbor||objects[x,y-1]==neighbor||objects[x,y+1]==neighbor)
                    candidates.Add((x,y));
            }
            int count=copse?1+rng.Next(2):rng.Next(3);
            for(int i=0;i<count&&candidates.Count>0;i++)
            {
                int pick=rng.Next(candidates.Count);var c=candidates[pick];candidates.RemoveAt(pick);
                objects[c.x,c.y]=copse?"MawToad":"BogTakenBody";
            }
        }
        public bool IsApproach(int x,int y) => InBounds(x,y)&&approach[x,y];
        public bool IsWet(int x,int y) => InBounds(x,y)&&wet[x,y];
        public bool IsShallowWater(int x,int y) => Formation==Formation.DrownedCopse&&IsWet(x,y);
        public string ObjectAt(int x,int y) => InBounds(x,y)?objects[x,y]:null;
        public string GroundAt(int x,int y) => "Grass";
        private int Roll(int x,int y,int salt) => Hash(worldX*Zone.Width+x,worldY*Zone.Height+y,salt)%100;
        private int Hash(int x,int y,int salt)
        {
            unchecked {uint h=(uint)Seed^2166136261u;h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;
                h=(h^(uint)salt)*16777619u;h^=h>>16;h*=2246822519u;h^=h>>13;return (int)(h&0x7fffffffu);}
        }
        private static bool InBounds(int x,int y) => x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(Formation+":"+Condition+":");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append(objects[x,y]??(wet[x,y]?"~":approach[x,y]?"_":".")).Append('|');
            return s.ToString();
        }
    }
}
