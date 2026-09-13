using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Generation-only land use: parcels, headlands, shelter and backwater.
    /// Native entities realize this intent once; presentation never regenerates it.</summary>
    public sealed class SpreadCompositionPlan
    {
        public readonly string ZoneID, Condition;
        public readonly int Seed, WorldX, WorldY, FocalX, FocalY, WestY, EastY, NorthX, SouthX;
        public readonly Formation Formation;
        private readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        private readonly Parcel[] parcels;
        private readonly bool mirrored;
        public int ParcelCount => parcels.Length;
        public Parcel GetParcel(int index) => parcels[index];
        public readonly struct Parcel
        {
            public readonly int X,Y,Width,Height;
            public Parcel(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}
            public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
            public bool Boundary(int x,int y)=>Contains(x,y)&&(x==X||y==Y||x==X+Width-1||y==Y+Height-1);
        }
        private static readonly HashSet<string> wilderness=BuildIndex();
        public static bool IsWildernessZone(string id)=>id!=null&&wilderness.Contains(id);
        private static HashSet<string> BuildIndex()
        {
            var set=new HashSet<string>(StringComparer.Ordinal);
            for(int x=0;x<20;x++)for(int y=0;y<20;y++)
                if(WorldMapAuthoring.BiomeAt(x,y)==BiomeType.Spread&&!WorldMapAuthoring.PlaceAt(x,y).HasValue
                    &&!SinkholeSites.IsMouth(x,y))set.Add(WorldMap.ToZoneID(x,y));
            return set;
        }
        public static SpreadCompositionPlan Create(string id,int seed,Formation formation=Formation.None)
        {
            var p=WorldMap.FromZoneID(id);
            if(!WorldMapAuthoring.InBounds(p.x,p.y)||p.z!=0)
                throw new ArgumentException("Spread plans require an in-bounds surface address.",nameof(id));
            return new SpreadCompositionPlan(id,seed,p.x,p.y,formation);
        }
        private SpreadCompositionPlan(string id,int seed,int wx,int wy,Formation form)
        {
            ZoneID=id;Seed=seed;WorldX=wx;WorldY=wy;
            Formation=form==Formation.None?FormationSelector.For(BiomeType.Spread,id):form;
            if(Formation<Formation.Hedgerow||Formation>Formation.RiverMeadow)throw new ArgumentException("Unsupported Spread formation.");
            var rng=new Random(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
            Condition=new[]{"tended","after harvest","returning scrub"}[rng.Next(3)];
            mirrored=rng.Next(2)==0;FocalX=33+rng.Next(15);FocalY=10+rng.Next(5);
            // Hash the shared boundary, not this chunk's local RNG history.
            WestY=6+Hash(wx,wy,11)%13;EastY=6+Hash(wx+1,wy,11)%13;
            NorthX=12+Hash(wx,wy,23)%56;SouthX=12+Hash(wx,wy+1,23)%56;
            Route(0,WestY,FocalX,FocalY);Route(79,EastY,FocalX,FocalY);
            Route(NorthX,0,FocalX,FocalY);Route(SouthX,24,FocalX,FocalY);
            parcels=new Parcel[3];
            for(int i=0;i<3;i++)
            {
                int width=16+rng.Next(5),height=8+rng.Next(3);
                int x=5+i*25+rng.Next(4),y=(i%2==0?3:12)+rng.Next(2);
                if(mirrored)x=Zone.Width-x-width;
                parcels[i]=new Parcel(x,y,width,height);
                // Working entrance opens the near headland, never a random
                // gap outside the actual boundary run.
                int gateX=x+width/2,gateY=y+height/2;
                Route(gateX,gateY,FocalX,FocalY);
            }
            ConnectOpenPockets();
        }
        // A hedge corner plus a shelter tree can enclose a pocket (seed 35).
        // Repair intent, before creating entities, rather than relying on the
        // legacy connectivity pass to understand every physics-solid blueprint.
        private void ConnectOpenPockets()
        {
            var reached=new bool[Zone.Width,Zone.Height];
            var queue=new Queue<(int x,int y)>();
            for(int pass=0;pass<Zone.Width*Zone.Height;pass++)
            {
                Array.Clear(reached,0,reached.Length);queue.Clear();
                reached[FocalX,FocalY]=true;queue.Enqueue((FocalX,FocalY));
                while(queue.Count>0)
                {
                    var c=queue.Dequeue();
                    for(int d=0;d<4;d++)
                    {
                        int x=c.x+(d==0?1:d==1?-1:0),y=c.y+(d==2?1:d==3?-1:0);
                        if(!InBounds(x,y)||reached[x,y])continue;
                        string bp=ObjectAt(x,y);if(bp=="Hedge"||bp=="Tree")continue;
                        reached[x,y]=true;queue.Enqueue((x,y));
                    }
                }
                bool repaired=false;
                for(int y=0;y<Zone.Height&&!repaired;y++)for(int x=0;x<Zone.Width;x++)
                {
                    string bp=ObjectAt(x,y);
                    if(reached[x,y]||bp=="Hedge"||bp=="Tree")continue;
                    Route(x,y,FocalX,FocalY);repaired=true;break;
                }
                if(!repaired)return;
            }
            throw new InvalidOperationException("Spread approach repair exceeded finite cell budget.");
        }
        private void Route(int ax,int ay,int bx,int by)
        {
            int steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=steps;i++)
            {
                double t=steps==0?0:i/(double)steps;
                int x=(int)Math.Round(ax+(bx-ax)*t),y=(int)Math.Round(ay+(by-ay)*t);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                    if(InBounds(x+dx,y+dy))approach[x+dx,y+dy]=true;
            }
        }
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&(approach[x,y] || (Formation==Formation.OldRoad && Math.Abs(y-(WestY+(EastY-WestY)*x/79.0))<1.6));
        public bool IsLawn(int x,int y)=>Math.Pow((x-FocalX)/8.0,2)+Math.Pow((y-FocalY)/3.5,2)<1;
        public double WaterDistance(int x,int y)
        {
            double bank=4.5+Math.Sin(x*.075+Hash(WorldX,WorldY,51)*.001)*1.6;
            if(mirrored)bank=24-bank;
            return Math.Abs(y-bank);
        }
        public bool IsWater(int x,int y)=>Formation==Formation.RiverMeadow&&x>3&&x<76&&y>1&&y<23
            &&!IsApproach(x,y)&&!IsLawn(x,y)&&WaterDistance(x,y)<1.35;
        public string GroundAt(int x,int y)=>Formation==Formation.OldRoad&&Math.Abs(y-(WestY+(EastY-WestY)*x/79.0))<1.6?"RoadStone":"Grass";
        /// <summary>Exactly zero or one above-ground placement per cell. Ground,
        /// water and native activity effects remain distinct, independently mutable.</summary>
        public string ObjectAt(int x,int y)
        {
            if(!InBounds(x,y)||x<2||x>77||y<2||y>22||IsApproach(x,y)||IsLawn(x,y)||IsWater(x,y))return null;
            foreach(var p in parcels)
            {
                if(!p.Contains(x,y))continue;
                if(Formation==Formation.Hedgerow&&p.Boundary(x,y))
                {
                    // Two explicit wide opposing gates complement the work lane.
                    bool gate=x>=p.X+p.Width/2-1&&x<=p.X+p.Width/2+1;
                    return gate?null:"Hedge";
                }
                if(Formation==Formation.FieldStrips)
                    return !p.Boundary(x,y)&&(x-p.X)%3!=0&&Roll(x,y,61)<(Condition=="after harvest"?62:94)?"CropRow":null;
                if(Formation==Formation.FlowerMeadow)
                {
                    double d=Math.Pow((x-p.X-p.Width*.5)/(p.Width*.48),2)+Math.Pow((y-p.Y-p.Height*.5)/(p.Height*.48),2);
                    return d<1&&Roll(x,y,67)<72?"FlowerField":null;
                }
                if(Formation==Formation.Fallow)
                {
                    if(p.Boundary(x,y)&&Roll(x,y,71)<42)return "Hedge";
                    return Roll(x,y,73)<(x-p.X)*2?"Bush":null;
                }
            }
            if(Formation==Formation.RiverMeadow&&x>3&&x<76&&WaterDistance(x,y)<3.1)
                return Roll(x,y,79)<55?"Reeds":null;
            // Coherent shelter masses at opposite corners, never an even scatter.
            double px=mirrored?79-x:x;
            double shelter=Math.Min(Math.Pow((px-10)/13,2)+Math.Pow((y-20)/4.0,2),
                Math.Pow((px-68)/12,2)+Math.Pow((y-4)/4.0,2));
            if(shelter<1.2&&Roll(x,y,83)<(Condition=="returning scrub"?23:15))return "Tree";
            if(shelter<1.6&&Roll(x,y,89)<14)return "Bush";
            return null;
        }
        public int Roll(int x,int y,int salt)=>Hash(WorldX*Zone.Width+x,WorldY*Zone.Height+y,salt)%100;
        private int Hash(int x,int y,int salt)
        {
            unchecked {uint h=(uint)Seed^2166136261u;h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;
                h=(h^(uint)salt)*16777619u;h^=h>>16;h*=2246822519u;h^=h>>13;return (int)(h&0x7fffffffu);}
        }
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(Formation+":"+Condition+":");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append(IsApproach(x,y)?'p':IsWater(x,y)?'~':ObjectAt(x,y)==null?'.':ObjectAt(x,y)[0]);
            return s.ToString();
        }
    }
}
