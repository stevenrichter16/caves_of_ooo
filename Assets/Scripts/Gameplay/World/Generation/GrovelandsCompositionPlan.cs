using System;
using System.Text;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Generation-only spatial intent. No entities, Unity objects or mutable
    /// world state: inspect, compare and render previews before realizing a chunk.</summary>
    public sealed class GrovelandsCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Seed, WorldX, WorldY, FocalX, FocalY, RadiusX, RadiusY;
        public readonly int WestY, EastY, NorthX, SouthX;
        public readonly Formation Formation;
        public readonly string Character;
        private readonly bool[,] approaches = new bool[Zone.Width,Zone.Height];
        private readonly bool[,] openings = new bool[Zone.Width,Zone.Height];
        private readonly Bed[] beds;
        private readonly bool mirror;
        private readonly int copseShift;
        public int BedCount => beds.Length;
        public Bed GetBed(int index) => beds[index];
        public readonly struct Bed
        {
            public readonly int X,Y,Width,Height,Stage;
            public Bed(int x,int y,int width,int height,int stage)
            { X=x;Y=y;Width=width;Height=height;Stage=stage; }
            public bool Contains(int x,int y) => x>=X && x<X+Width && y>=Y && y<Y+Height;
        }

        public static GrovelandsCompositionPlan Create(string id,int seed,Formation formation=Formation.None)
        {
            var at=WorldMap.FromZoneID(id);
            if(!WorldMapAuthoring.InBounds(at.x,at.y)||at.z!=0)
                throw new ArgumentException("Composition requires an in-bounds surface chunk.",nameof(id));
            return new GrovelandsCompositionPlan(id,seed,at.x,at.y,formation);
        }
        /// <summary>Only authored wilderness, never POIs or subterranean sites.</summary>
        private static readonly HashSet<string> Wilderness = BuildWildernessIndex();
        public static bool IsWildernessZone(string id) => id != null && Wilderness.Contains(id);
        private static HashSet<string> BuildWildernessIndex()
        {
            var result=new HashSet<string>(StringComparer.Ordinal);
            for(int x=0;x<20;x++)for(int y=0;y<20;y++)
                if(WorldMapAuthoring.BiomeAt(x,y)==BiomeType.Grovelands && !WorldMapAuthoring.PlaceAt(x,y).HasValue
                    && !SinkholeSites.IsMouth(x,y) && WorldMap.ToZoneID(x,y)!=OverworldZoneManager.WovenDollZoneID)
                    result.Add(WorldMap.ToZoneID(x,y));
            return result;
        }
        private GrovelandsCompositionPlan(string id,int seed,int wx,int wy,Formation formation)
        {
            ZoneID=id;Seed=seed;WorldX=wx;WorldY=wy;
            Formation=formation==Formation.None?FormationSelector.For(BiomeType.Grovelands,id):formation;
            if(Formation<Formation.Grove || Formation>Formation.CompostingField)
                throw new ArgumentException("Unsupported Grovelands formation.");
            var rng=new Random(unchecked(seed ^ FormationSelector.StableIndex(id,int.MaxValue)));
            FocalX=27+rng.Next(26); FocalY=9+rng.Next(7);
            RadiusX=10+rng.Next(4); RadiusY=4+rng.Next(2);
            mirror=rng.Next(2)==0; copseShift=rng.Next(7)-3;
            Character=new[]{"young fringe","mature sanctuary","reclaiming thicket"}[rng.Next(3)];
            // Both sides hash the SAME world boundary; chunk-local RNG never
            // chooses portals. Three-cell mouths work with broad footprints.
            WestY=5+Hash(wx,wy,11)%15; EastY=5+Hash(wx+1,wy,11)%15;
            NorthX=12+Hash(wx,wy,23)%56; SouthX=12+Hash(wx,wy+1,23)%56;
            Ellipse(FocalX,FocalY,RadiusX+2,RadiusY+1);
            Ellipse(12+rng.Next(8),6+rng.Next(12),7,3);
            Ellipse(61+rng.Next(8),6+rng.Next(12),7,3);
            Route(0,WestY,FocalX,FocalY);
            Route(Zone.Width-1,EastY,FocalX,FocalY);
            Route(NorthX,0,FocalX,FocalY);
            Route(SouthX,Zone.Height-1,FocalX,FocalY);
            // Three separate reclamation beds retain unnatural straight rows,
            // but never become a screen-wide barcode. Stages vary fill density.
            beds=new[]{new Bed(7+rng.Next(5),3+rng.Next(3),15+rng.Next(6),6,rng.Next(3)),
                new Bed(FocalX-9,FocalY-3,18+rng.Next(5),7,rng.Next(3)),
                new Bed(57+rng.Next(3),14+rng.Next(3),15+rng.Next(5),6,rng.Next(3))};
            if(mirror)
                for(int i=0;i<beds.Length;i++)
                {
                    var b=beds[i];beds[i]=new Bed(Zone.Width-b.X-b.Width,b.Y,b.Width,b.Height,b.Stage);
                }
            if(Formation==Formation.CompostingField || Formation==Formation.FruitingWall)
                foreach(var bed in beds)
                    for(int x=bed.X-1;x<=bed.X+bed.Width;x++)
                        for(int y=bed.Y-1;y<=bed.Y+bed.Height;y++)
                            if(InBounds(x,y)) openings[x,y]=true;
        }
        private void Ellipse(int cx,int cy,int rx,int ry)
        {
            for(int x=0;x<Zone.Width;x++) for(int y=0;y<Zone.Height;y++)
                if(Math.Pow((x-cx)/(double)rx,2)+Math.Pow((y-cy)/(double)ry,2)<=1) openings[x,y]=true;
        }
        private void Route(int ax,int ay,int bx,int by)
        {
            int steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=steps;i++)
            {
                double t=steps==0?0:i/(double)steps;
                int x=(int)Math.Round(ax+(bx-ax)*t), y=(int)Math.Round(ay+(by-ay)*t);
                for(int dx=-1;dx<=1;dx++) for(int dy=-1;dy<=1;dy++)
                    if(InBounds(x+dx,y+dy)) approaches[x+dx,y+dy]=true;
            }
        }
        // The grove's one-cell seep is the destination, not dry approach ground.
        public bool IsApproach(int x,int y) => InBounds(x,y) && approaches[x,y]
            && !(Formation==Formation.Grove && x==FocalX && y==FocalY);
        public bool IsOpening(int x,int y) => InBounds(x,y) && openings[x,y];
        public bool IsReserved(int x,int y) => IsApproach(x,y)||IsOpening(x,y)
            || (Formation==Formation.TendrilFen && WaterDistance(x,y)<3);
        public double WaterDistance(int x,int y)
        {
            double t=x/(double)(Zone.Width-1);
            double mid=9+Hash(WorldX,WorldY,53)%6;
            double a=mid+Math.Sin(t*Math.PI*2+Seed*.017)*4;
            double b=mid+Math.Sin(t*Math.PI*3+Seed*.013+1.7)*5;
            return Math.Min(Math.Abs(y-a),Math.Abs(y-b));
        }
        /// <summary>Smooth world-space influences. Adjacent chunk edges sample
        /// consecutive positions, rather than restarting the noise in every chunk.</summary>
        public double Growth(int x,int y)
        {
            double gx=WorldX*Zone.Width+x, gy=WorldY*Zone.Height+y;
            return .5+.24*Math.Sin(gx*.105+gy*.13+Seed*.007)
                +.19*Math.Cos(gx*.057-gy*.27+Seed*.011);
        }
        /// <summary>Local canopy groups frame the feature spaces. Their centers
        /// mirror with the bed arrangement; the regional field remains continuous.</summary>
        public double Canopy(int x,int y)
        {
            double px=mirror?Zone.Width-1-x:x;
            double a=Math.Pow((px-14-copseShift)/12,2)+Math.Pow((y-20)/5.0,2);
            double b=Math.Pow((px-66+copseShift)/12,2)+Math.Pow((y-5)/5.0,2);
            double c=Math.Pow((px-38-copseShift)/10,2)+Math.Pow((y-3)/4.0,2);
            double local=.99-.22*Math.Min(a,Math.Min(b,c));
            return Math.Max(Growth(x,y),local);
        }
        public double Moisture(int x,int y)
        {
            double gx=WorldX*Zone.Width+x,gy=WorldY*Zone.Height+y;
            return .5+.3*Math.Sin(gx*.025+gy*.048+Seed*.003);
        }
        public int Roll(int x,int y,int salt) => Hash(WorldX*Zone.Width+x,WorldY*Zone.Height+y,salt)%100;
        private int Hash(int x,int y,int salt)
        {
            unchecked { uint h=(uint)Seed ^ 2166136261u;h=(h^(uint)x)*16777619u;
                h=(h^(uint)y)*16777619u;h=(h^(uint)salt)*16777619u;
                h^=h>>16;h*=2246822519u;h^=h>>13;return (int)(h&0x7fffffffu); }
        }
        private static bool InBounds(int x,int y) => x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder();s.Append(Formation).Append(':').Append(Character).Append(':');
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append(IsApproach(x,y)?'p':IsReserved(x,y)?'.':Growth(x,y)>.65?'T':'_');
            return s.ToString();
        }
    }
}
