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
        public readonly SpreadExplorationTopology Topology;
        /// <summary>Cold-built local landscape vocabulary. Null retains the
        /// original formation grammar; saved entity graphs never reconstruct it.</summary>
        public readonly string Landscape;
        private readonly bool[,] road;
        private readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        private readonly Parcel[] parcels;
        private readonly bool mirrored;
        // Activated only after the original connectivity repair, so subtracting
        // northern hedge fragments cannot change existing lanes or other planned terrain cells.
        private readonly bool northernFragments;
        private readonly NellaFrontage? nellaFrontage;
        // Cold-only bits: original flower substrate (1), retained flower (2).
        // No loaded or cached world reconstructs this presentation-era mask.
        private readonly byte[,] openingFlowers;
        private const double OpeningFlowerRetention=.64;
        public int ParcelCount => parcels.Length;
        public Parcel GetParcel(int index) => parcels[index];
        public readonly struct Parcel
        {
            public readonly int X,Y,Width,Height;
            public Parcel(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}
            public bool Contains(int x,int y)=>x>=X&&x<X+Width&&y>=Y&&y<Y+Height;
            public bool Boundary(int x,int y)=>Contains(x,y)&&(x==X||y==Y||x==X+Width-1||y==Y+Height-1);
        }
        /// <summary>Cold-only shared field frontage. The resident builder still proves the
        /// complete stamp against the actual world; this descriptor never moves live owners.</summary>
        public readonly struct NellaFrontage
        {
            public readonly int PrimaryParcelIndex, X, Y, Rotation;
            internal readonly int SecondaryParcelIndex;
            internal NellaFrontage(int primary,int secondary,int x,int y)
            {PrimaryParcelIndex=primary;SecondaryParcelIndex=secondary;X=x;Y=y;Rotation=0;}
            public bool ContainsServiceBay(int x,int y)=>x>=X-2&&x<=X+2&&y>=Y-3&&y<=Y+1;
        }
        public bool TryGetNellaFrontage(out NellaFrontage frontage)
        {frontage=nellaFrontage.GetValueOrDefault();return nellaFrontage.HasValue;}
        // The ten connected plot owners and its two planting gaps, in native rotation zero.
        private static readonly (int x,int y)[] NellaStampAndGaps={
            (0,0),(-2,-1),(-1,-1),(-2,1),(-1,1),(2,0),
            (-2,-3),(-1,-3),(2,-2),(1,-3),(0,-1),(0,1)};
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
            return new SpreadCompositionPlan(id,seed,p.x,p.y,formation,SpreadExplorationTopology.Legacy);
        }
        /// <summary>Fresh opted-in geography; legacy calls retain their exact original output.</summary>
        public static SpreadCompositionPlan Create(string id,int seed,Formation formation,SpreadExplorationTopology topology)
        {
            var p=WorldMap.FromZoneID(id);
            if(!WorldMapAuthoring.InBounds(p.x,p.y)||p.z!=0)throw new ArgumentException("Spread plans require an in-bounds surface address.",nameof(id));
            if(topology<SpreadExplorationTopology.Legacy||topology>SpreadExplorationTopology.BankCrossing)throw new ArgumentOutOfRangeException(nameof(topology));
            return new SpreadCompositionPlan(id,seed,p.x,p.y,formation,topology);
        }
        private SpreadCompositionPlan(string id,int seed,int wx,int wy,Formation form,SpreadExplorationTopology topology)
        {
            Topology=topology;
            ZoneID=id;Seed=seed;WorldX=wx;WorldY=wy;
            Formation=form==Formation.None?FormationSelector.For(BiomeType.Spread,id):form;
            if(Formation<Formation.Hedgerow||Formation>Formation.RiverMeadow)throw new ArgumentException("Unsupported Spread formation.");
            if(topology!=SpreadExplorationTopology.Legacy&&IsWildernessZone(id)&&id!=ReferenceGladePlan.ZoneID)
            {
                if(Formation==Formation.Fallow)Landscape="overgrown crofts";
                else if(Formation==Formation.FlowerMeadow)
                    Landscape=id=="Overworld.12.10.0"?"flower avenues":id=="Overworld.11.11.0"?"crescent hollow"
                        :Hash(wx,wy,137)%2==0?"flower avenues":"crescent hollow";
            }
            var rng=new Random(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
            Condition=new[]{"tended","after harvest","returning scrub"}[rng.Next(3)];
            mirrored=rng.Next(2)==0;FocalX=33+rng.Next(15);FocalY=10+rng.Next(5);
            // Hash the shared boundary, not this chunk's local RNG history.
            WestY=6+Hash(wx,wy,11)%13;EastY=6+Hash(wx+1,wy,11)%13;
            NorthX=12+Hash(wx,wy,23)%56;SouthX=12+Hash(wx,wy+1,23)%56;
            if(topology==SpreadExplorationTopology.Legacy)
            {
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
            }
            else
            {
                road=new bool[Zone.Width,Zone.Height];
                parcels=CreateExplorationParcels(topology);
                BuildExplorationRoutes(topology);
            }
            ConnectOpenPockets();
            northernFragments=topology!=SpreadExplorationTopology.Legacy
                &&id=="Overworld.11.9.0"&&Landscape=="overgrown crofts";
            // Find against the original repaired plan, then activate the field mask. Neither
            // the connectivity repair nor unrelated addresses can observe the new mask.
            if(topology!=SpreadExplorationTopology.Legacy&&id=="Overworld.11.8.0"&&Formation==Formation.FieldStrips)
                nellaFrontage=FindNellaFrontage();
            // Capture after the original connectivity repair, before assigning
            // this mask. ObjectAt therefore observes the original grammar here.
            if(OpeningFlowerDrifts)openingFlowers=ComposeOpeningFlowers();
        }
        /// <summary>Original cold substrate exclusion for the optional eastern workpad.
        /// This is not a live permission or a persisted flower/resource allowance.</summary>
        internal bool HadOriginalOpeningFlower(int x,int y)=>openingFlowers!=null&&InBounds(x,y)
            &&(openingFlowers[x,y]&1)!=0;
        private byte[,] ComposeOpeningFlowers()
        {
            var mask=new byte[Zone.Width,Zone.Height];
            var bodies=new[]{new List<(int x,int y,double score)>(),
                new List<(int x,int y,double score)>(),new List<(int x,int y,double score)>()};
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(ObjectAt(x,y)!="CharmFlowers")continue;
                mask[x,y]=1;
                // The existing separated upper/lower bodies and small eastern
                // echo each retain a substantial presence; no new cell is filled.
                bodies[y>=12?1:x>=55?2:0].Add((x,y,OpeningFlowerAffinity(x,y)));
            }
            foreach(var body in bodies)
            {
                body.Sort((a,b)=>{int n=b.score.CompareTo(a.score);if(n!=0)return n;
                    n=a.y.CompareTo(b.y);return n!=0?n:a.x.CompareTo(b.x);});
                int retained=(int)Math.Ceiling(body.Count*OpeningFlowerRetention);
                for(int i=0;i<retained;i++){var p=body[i];mask[p.x,p.y]|=2;}
            }
            return mask;
        }
        private double OpeningFlowerAffinity(int x,int y)
        {
            // Smooth spatial variation opens connected interiors; the smaller
            // independent term feathers their edges without a uniform scatter.
            // Coordinate hashes do not consume population/loadout RNG.
            double u=(x+Hash(WorldX,WorldY,773)%4)/5.0,v=(y+Hash(WorldX,WorldY,779)%3)/3.0;
            int gx=(int)u,gy=(int)v;double a=u-gx,b=v-gy;
            a=a*a*(3-2*a);b=b*b*(3-2*b);
            double Noise(int nx,int ny)=>Hash(nx,ny,787)%100;
            double top=Noise(gx,gy)*(1-a)+Noise(gx+1,gy)*a;
            double bottom=Noise(gx,gy+1)*(1-a)+Noise(gx+1,gy+1)*a;
            return top*(1-b)+bottom*b+.4*(Hash(x,y,797)%100);
        }
        private NellaFrontage? FindNellaFrontage()
        {
            bool left=SpreadExplorationPlan.Rank(Seed,ZoneID,"resident-anchor")%2==0;
            int anchorX=left?22:58,primary=-1,best=int.MaxValue;
            for(int i=0;i<parcels.Length;i++)
            {
                var p=parcels[i];int distance=Math.Abs(p.X+p.Width/2-anchorX);
                if(p.Y<12&&distance<best){primary=i;best=distance;}
            }
            if(primary<0)return null;
            var main=parcels[primary];int secondary=-1;best=-1;
            for(int i=0;i<parcels.Length;i++)
            {
                if(i==primary)continue;var p=parcels[i];
                int distance=Math.Max(Math.Abs(p.X+p.Width/2-main.X-main.Width/2),Math.Abs(p.Y+p.Height/2-main.Y-main.Height/2));
                if(distance>best){secondary=i;best=distance;}
            }
            int preferredX=main.X+main.Width/2+(left?4:-4),bottom=main.Y+main.Height;
            // Match native y/x tie order. Rotation zero is intentional: a merely
            // aesthetic rotation preference must not disagree with native Fits' first trial.
            for(int distance=0;distance<Zone.Width;distance++)
                for(int y=Math.Max(4,bottom-3);y<=Math.Min(20,bottom+3);y++)
                    for(int x=main.X+2;x<main.X+main.Width-2;x++)
                    {
                        if(Math.Max(Math.Abs(x-preferredX),Math.Abs(y-bottom))!=distance)continue;
                        var front=new NellaFrontage(primary,secondary,x,y);
                        if(NellaStampIsPlanSafe(front)&&KeepsPrimaryFurrows(front))return front;
                    }
            return null;
        }
        private bool NellaStampIsPlanSafe(NellaFrontage front)
        {
            foreach(var offset in NellaStampAndGaps)
            {
                int x=front.X+offset.x,y=front.Y+offset.y;
                if(!InBounds(x,y)||IsApproach(x,y)||IsLawn(x,y)||IsWater(x,y)
                    ||Math.Max(Math.Abs(x-40),Math.Abs(y-12))<=6)return false;
                string owner=ObjectAt(x,y);
                if(owner!=null&&owner!="CropRow")return false;
            }
            return true;
        }
        private bool KeepsPrimaryFurrows(NellaFrontage front)
        {
            var p=parcels[front.PrimaryParcelIndex];bool previous=false;
            for(int group=0;group*3<p.Width;group++)
            {
                bool any=false,whole=true;
                for(int y=p.Y;y<p.Y+p.Height;y++)
                    for(int x=p.X+group*3;x<Math.Min(p.X+p.Width,p.X+group*3+3);x++)
                        if(ObjectAt(x,y)=="CropRow")
                        {any=true;if(front.ContainsServiceBay(x,y))whole=false;}
                bool retained=any&&whole;if(previous&&retained)return true;previous=retained;
            }
            return false;
        }
        private bool KeepNellaRow(int index,int x,int y)
        {
            if(!nellaFrontage.HasValue)return true;
            var front=nellaFrontage.Value;
            if(index==front.PrimaryParcelIndex)return !front.ContainsServiceBay(x,y);
            var p=parcels[index];var main=parcels[front.PrimaryParcelIndex];
            // Retain substantial remote working pieces, anchored away from the
            // service frontage. Remove whole furrow-group widths rather than tiny tails.
            int v=p.Y<12?y-p.Y:p.Y+p.Height-1-y;
            bool secondary=index==front.SecondaryParcelIndex;
            int group=(x-p.X)/3,clippedGroups=secondary?1:1+index%2;
            // A partial last interior group must not shift the cut into its
            // neighboring pair. The final mod-three gap is not a furrow group.
            bool withinSpan=p.X<main.X?group<=(p.Width-3)/3-clippedGroups:group>=clippedGroups;
            int depth=secondary?4:3;
            return withinSpan&&v<=depth;
        }
        private Parcel[] CreateExplorationParcels(SpreadExplorationTopology topology)
        {
            if(topology==SpreadExplorationTopology.OffsetLanes)
                return new[]{new Parcel(5,3,23,6),new Parcel(38,3,29,6),new Parcel(9,15,27,6),new Parcel(49,15,23,6)};
            if(topology==SpreadExplorationTopology.BrokenEnclosures)
                return new[]{new Parcel(5,3,18,7),new Parcel(31,3,21,6),new Parcel(61,3,14,6),new Parcel(10,15,23,6),new Parcel(46,15,27,6)};
            return new[]{new Parcel(6,3,25,7),new Parcel(43,4,29,7),new Parcel(18,15,34,7)};
        }
        // Three deliberately different dry route networks join the same border ports.
        // Their layouts are native plan data, never regenerated by a renderer.
        private void BuildExplorationRoutes(SpreadExplorationTopology topology)
        {
            if(topology==SpreadExplorationTopology.OffsetLanes)
            {
                Route(0,WestY,5,12);Route(5,12,74,12);Route(74,12,79,EastY);
                Route(NorthX,0,NorthX,1);Route(NorthX,1,35,1);Route(35,1,35,12);
                Route(SouthX,24,SouthX,23);Route(SouthX,23,43,23);Route(43,23,43,12);
            }
            else if(topology==SpreadExplorationTopology.BrokenEnclosures)
            {
                Route(0,WestY,2,12);Route(2,12,27,12);Route(27,12,27,1);
                Route(27,1,57,1);Route(57,1,57,13);Route(57,13,77,13);Route(77,13,79,EastY);
                Route(NorthX,0,NorthX,1);Route(SouthX,24,SouthX,23);Route(SouthX,23,38,23);Route(38,23,38,13);Route(38,13,57,13);
            }
            else
            {
                Route(0,WestY,3,12);Route(3,12,76,12);Route(76,12,79,EastY);
                Route(NorthX,0,NorthX,1);Route(NorthX,1,36,1);Route(36,1,36,12);
                Route(SouthX,24,SouthX,23);Route(SouthX,23,58,23);Route(58,23,58,12);
                Route(3,12,3,22);Route(3,22,58,22);
            }
            // Only designated working lanes become road surface. Subsequent pocket
            // repairs may open ground but must not paint an invented second road.
            if(Formation==Formation.OldRoad)
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)road[x,y]=approach[x,y];
            // Open a connection to each bounded plot without restoring the old focal star.
            foreach(var p in parcels)
            {
                int x=p.X+p.Width/2;
                Route(x,p.Y+p.Height/2,x,p.Y<12?12:13);
            }
            // The legacy focal point remains a reachable repair anchor without
            // requiring all normal approaches to converge on it.
            Route(FocalX,FocalY,FocalX,12);
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
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&(approach[x,y] || (Formation==Formation.OldRoad && Topology==SpreadExplorationTopology.Legacy && Math.Abs(y-(WestY+(EastY-WestY)*x/79.0))<1.6));
        public bool IsLawn(int x,int y)=>Topology==SpreadExplorationTopology.Legacy
            ?Math.Pow((x-FocalX)/8.0,2)+Math.Pow((y-FocalY)/3.5,2)<1
            :Topology==SpreadExplorationTopology.BrokenEnclosures&&((x>24&&x<30&&y>9&&y<15)||(x>54&&x<61&&y>10&&y<16));
        public double WaterDistance(int x,int y)
        {
            double bank=4.5+Math.Sin(x*.075+Hash(WorldX,WorldY,51)*.001)*1.6;
            if(mirrored)bank=24-bank;
            return Math.Abs(y-bank);
        }
        public bool IsWater(int x,int y)=>Formation==Formation.RiverMeadow&&x>3&&x<76&&y>1&&y<23
            &&!IsApproach(x,y)&&!IsLawn(x,y)&&WaterDistance(x,y)<1.35;
        public string GroundAt(int x,int y)=>Formation==Formation.OldRoad&&(Topology==SpreadExplorationTopology.Legacy
            ?Math.Abs(y-(WestY+(EastY-WestY)*x/79.0))<1.6:InBounds(x,y)&&road[x,y])?"RoadStone":"Grass";
        /// <summary>Exactly zero or one above-ground placement per cell. Ground,
        /// water and native activity effects remain distinct, independently mutable.</summary>
        public string ObjectAt(int x,int y)
        {
            if(!InBounds(x,y)||x<2||x>77||y<2||y>22||IsApproach(x,y)||IsLawn(x,y)||IsWater(x,y))return null;
            if(HadOriginalOpeningFlower(x,y))return (openingFlowers[x,y]&2)!=0?"CharmFlowers":null;
            if(Landscape!=null)return LandscapeObjectAt(x,y);
            for(int i=0;i<parcels.Length;i++)
            {
                var p=parcels[i];
                if(!p.Contains(x,y))continue;
                if(Formation==Formation.Hedgerow&&p.Boundary(x,y))
                {
                    // Two explicit wide opposing gates complement the work lane.
                    bool gate=x>=p.X+p.Width/2-1&&x<=p.X+p.Width/2+1;
                    if(gate)return null;
                    // Only this cold opening site breaks the old parcel edges.
                    // Subtraction preserves every existing route/gate; loaded
                    // graphs and unrelated Hedgerow parcels never use this mask.
                    if(OpeningKitchen&&!KeepKitchenHedge(p,i,x,y))return null;
                    return "Hedge";
                }
                if(Formation==Formation.FieldStrips)
                    // Only the principal field completes old random cut-row holes.
                    // Existing cell exclusions and the small service bay still win;
                    // the builder selects the same finite gleaning budget afterwards.
                    return !p.Boundary(x,y)&&(x-p.X)%3!=0
                        &&(nellaFrontage.HasValue&&i==nellaFrontage.Value.PrimaryParcelIndex
                            ||Roll(x,y,61)<(Condition=="after harvest"?62:94))
                        &&KeepNellaRow(i,x,y)?"CropRow":null;
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
        private bool OpeningKitchen=>Topology!=SpreadExplorationTopology.Legacy
            &&ZoneID=="Overworld.12.11.0"&&Formation==Formation.Hedgerow;
        private bool OpeningFlowerDrifts=>Topology!=SpreadExplorationTopology.Legacy
            &&ZoneID=="Overworld.12.10.0"&&Formation==Formation.FlowerMeadow;
        private bool KeepKitchenHedge(Parcel p,int index,int x,int y)
        {
            int u=x-p.X,v=y-p.Y,w=p.Width-1,h=p.Height-1;
            if(mirrored)u=w-u;
            // Distinct perimeter fragments, not five versions of the same L.
            // They are subsets of the old boundary after all public openings.
            switch(index)
            {
                case 0:return (v==0&&u<=w*4/5)||(u==0&&v<=h/2);
                case 1:return v==h&&u>=2&&u<=w-1&&(u<w/3||u>w/3+3);
                case 2:return (v==0&&u<=w/2)||(v==h&&u>=w*2/3);
                case 3:return (u==w&&v>=1&&v<=h-1)||(v==0&&u>=2&&u<=4);
                default:return (v==h&&u>=w/4)||(u==w&&v>=h*2/3);
            }
        }
        private bool InOpeningFlowerDrift(int x,int y)
        {
            // Broad unequal bodies with a bite out of the larger one. These
            // don't repeat a centerline across the world; real grass separates
            // them and the existing approach/tree exclusions still take priority.
            int shift=Hash(WorldX,WorldY,761)%5-2;
            double upper=Math.Min(Ellipse(x,y,24+shift,6,16,4.8),Ellipse(x,y,38+shift,8,9,3.8));
            double bite=Ellipse(x,y,30+shift,3,9,3.6);
            double lower=Ellipse(x,y-.20*(x-56),56-shift,18,11,4.2);
            double echo=Ellipse(x,y,67,5+Hash(WorldX,WorldY,769)%2,4,2.4);
            return (upper<1&&bite>1)||lower<1||echo<1;
        }
        private string LandscapeObjectAt(int x,int y)
        {
            if(Landscape=="overgrown crofts")
            {
                for(int i=0;i<parcels.Length;i++)
                {
                    var p=parcels[i];
                    if(!p.Contains(x,y))continue;
                    // Long surviving hedge runs with broad collapsed gaps, and
                    // a coherent belt of trees at one end of each former plot.
                    if(p.Boundary(x,y))return Roll(x/4,y/2,701)<76
                        &&(!northernFragments||KeepNorthernHedge(p,i,x,y))?"Hedge":null;
                    // Surviving work paths thread the regrowth. These are bare
                    // ground, not reserved approach lanes, so native actors can
                    // use both sides of tree cover without adding any owners.
                    if((x-p.X)%3==1)return null;
                    double cx=p.X+p.Width*(mirrored?.76:.24),cy=p.Y+p.Height*.5;
                    if(Ellipse(x,y,cx,cy,3.4,2.2)<1&&Roll(x,y,703)<78)return "Tree";
                    if(Ellipse(x,y,cx,cy,5.4,3.1)<1&&Roll(x,y,709)<38)return "Bush";
                    return null;
                }
            }
            else if(Landscape=="flower avenues")
            {
                if(Ellipse(x,y,12,10,3,2.3)<1||Ellipse(x,y,67,15,3,2.3)<1)
                    return Roll(x,y,727)<72?"Tree":"Bush";
                if(OpeningFlowerDrifts)
                {
                    if(InOpeningFlowerDrift(x,y)&&Roll(x,y,733)<70)return "CharmFlowers";
                }
                else
                {
                    double wave=Math.Sin((x+Hash(WorldX,WorldY,719)%25)*.095)*1.8;
                    double band=Math.Min(Math.Abs(y-5-wave),Math.Min(Math.Abs(y-12-wave),Math.Abs(y-19-wave)));
                    if(x>5&&x<75&&band<1.65&&Roll(x,y,733)<70)return "CharmFlowers";
                }
            }
            else
            {
                double ring=Ellipse(x,y,FocalX,FocalY,20,7.8);
                double opening=(mirrored?-1:1)*(x-FocalX);
                if(Ellipse(x,y,FocalX+(mirrored?-4:4),FocalY,5.7,3.3)<1)
                    return Roll(x,y,739)<70?"Tree":"Bush";
                if(ring>.44&&ring<1.18&&!(opening>0&&Math.Abs(y-FocalY)<2.4)&&Roll(x,y,743)<83)
                    return "CharmFlowers";
            }
            // A few shrubs feather the outer edges instead of filling every
            // empty cell. Open ground remains a meaningful part of the layout.
            double px=mirrored?79-x:x;
            if((Ellipse(px,y,8,19,9,3)<1||Ellipse(px,y,71,5,8,3)<1)&&Roll(x,y,751)<27)return "Bush";
            return null;
        }
        private bool KeepNorthernHedge(Parcel p,int index,int x,int y)
        {
            int u=x-p.X,v=y-p.Y,w=p.Width-1,h=p.Height-1;
            if(mirrored)u=w-u;
            // A substantial tree-side bracket, opposed remnants and a broken
            // working-field edge. Existing gaps/rolls always take precedence.
            switch(index)
            {
                case 0:return v==0||u==0||(v==h&&u<=w/4);
                case 1:return (v==0&&u<=w*3/5)||(v==h&&u>=w*2/5)||u==w;
                case 2:return (v==0&&(u<=w*2/5||u>=w*3/5))
                    ||(v==h&&u>=w/5&&u<=w*3/5)||u==0;
                case 3:return v==h||u==0||(v==0&&u<=w/5);
                default:return (v==0&&u>=w/4)||(v==h&&u<=w/2)||u==w;
            }
        }
        private static double Ellipse(double x,double y,double cx,double cy,double rx,double ry)
            =>(x-cx)*(x-cx)/(rx*rx)+(y-cy)*(y-cy)/(ry*ry);

        /// <summary>Local geometric cues only; never promises unrolled forage,
        /// actors or loot. Native object text and action menus remain intact.</summary>
        public string LandscapeContext(string blueprint)
        {
            if(blueprint!="Tree"&&blueprint!="Hedge"&&blueprint!="FlowerField"&&blueprint!="CharmFlowers")return null;
            if(OpeningFlowerDrifts&&Landscape=="flower avenues")
                return "Unequal drifts of blooms curl around open grass. Small stands of trees interrupt the flower banks; broad gaps lead toward the worked clearings.";
            switch(Landscape)
            {
                case "overgrown crofts":return "The old crofts survive as broken hedge runs and crowded tree belts. Broad gaps lead around the surviving boundaries; trunks obstruct the shorter passages.";
                case "flower avenues":return "Wind-combed blooms form long avenues across the meadow. The open lanes between them widen around small stands of trees.";
                case "crescent hollow":return "A crescent of blooms curls around this wooded hollow. Gaps through the trees offer a closer passage; open ground follows the outer flower bank.";
                default:return null;
            }
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
