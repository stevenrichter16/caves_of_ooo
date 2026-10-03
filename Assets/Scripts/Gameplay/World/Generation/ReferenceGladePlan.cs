using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
namespace CavesOfOoo.Core
{
    /// <summary>Fixed glade composition with seed-varied small dressing. Native
    /// owners realize it once; rendering and restored zones never regenerate it.</summary>
    public sealed class ReferenceGladePlan
    {
        public const string ZoneID="Overworld.11.10.0";
        public const int StartX=40,StartY=12;
        private sealed class Authority { public WorldMap Map; public bool Supported; }
        private static readonly ConditionalWeakTable<Zone,Authority> authorities=new ConditionalWeakTable<Zone,Authority>();
        internal static void Attach(Zone zone,WorldMap map,bool supported)
        {
            if(zone?.ZoneID!=ZoneID)return;
            var authority=authorities.GetValue(zone,_=>new Authority());
            authority.Map=map;authority.Supported=supported;
        }
        /// <summary>Standalone graphs honor the exact address; managed and saved
        /// graphs also require their current Spread/no-POI and content-pack authority.</summary>
        public static bool IsActive(Zone zone)
            =>zone?.ZoneID==ZoneID&&(!authorities.TryGetValue(zone,out var a)
                ||a.Supported&&a.Map!=null&&a.Map.GetBiome(11,10)==BiomeType.Spread&&a.Map.GetPOI(11,10)==null);
        public readonly struct Placement
        {
            public readonly string Blueprint,VisualID;public readonly int X,Y;
            public Placement(string blueprint,int x,int y,string visual=null)
            {Blueprint=blueprint;X=x;Y=y;VisualID=visual==null?"":"reference-glade-"+visual;}
        }
        private readonly List<Placement> placements=new List<Placement>();
        public IReadOnlyList<Placement> Placements {get;private set;}
        private readonly bool[,] occupied=new bool[Zone.Width,Zone.Height];
        private readonly int seed;
        private readonly bool fieldwork;
        public static ReferenceGladePlan Create(int seed)=>new ReferenceGladePlan(seed,false);
        public static ReferenceGladePlan Create(int seed,bool fieldwork)=>new ReferenceGladePlan(seed,fieldwork);
        private ReferenceGladePlan(int seed,bool fieldwork)
        {
            this.seed=seed;this.fieldwork=fieldwork;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                placements.Add(new Placement("Grass",x,y,"ground"));
            for(int y=0;y<=9;y++)Add("Wall",53,y,"low-wall");
            for(int y=18;y<25;y++)Add("Wall",47,y,"low-wall");
            Add("Wall",48,18,"low-wall");
            for(int x=20;x<=30;x++){Add("Wall",x,11,"dark-ruin");Add("Wall",x,22,"dark-ruin");}
            for(int y=12;y<22;y++)if(y!=16&&y!=17)Add("Wall",23,y,"dark-ruin");
            for(int y=11;y<=15;y++)Add("Wall",31,y,"lit-wall");
            for(int y=17;y<=21;y++)Add("Wall",30,y,"lit-wall");
            Add("GlowQuartzVein",31,10,"lit-wall");Add("GlowQuartzVein",31,22,"lit-wall");
            Add("Chest",51,8);Add("WoodenBarrel",32,22);Add("WoodenBarrel",31,23);
            Add("Torch",52,7);Add("Signpost",42,3);
            Add("MushroomRing",34,19);Add("MushroomRing",50,21);
            Add("MarlbackScrabbler",42,19);Add("MarlbackGleaner",44,20);Add("MarlbackScrabbler",22,5);
            Add("Warden",35,15);Add("Villager",38,21);Add("PetDog",33,23);
            AddDiscoveries();
            // A few remnant blocks give the left silhouette depth without an
            // invisible barrier: BrokenColumn is deliberately passable rubble.
            for(int y=12;y<24;y++)for(int x=18;x<28;x++)
                if(!IsApproach(x,y)&&!IsDiscoveryGround(x,y)&&Hash(x,y,7)%100<34)Add("BrokenColumn",x,y,"dark-ruin");
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                if(occupied[x,y]||IsApproach(x,y)||IsDiscoveryGround(x,y)||Math.Max(Math.Abs(x-40),Math.Abs(y-12))<=2)continue;
                bool pale=(x>=27&&x<=35&&y<8)||(x>=32&&x<=36&&y>=21)||(x>=55&&y>=10)||(x>=40&&x<=43&&y<6);
                bool gravel=Ellipse(x,y,48,3,6,4)||Ellipse(x,y,29,8,4,2)||Ellipse(x,y,36,14,5,2)||Ellipse(x,y,32,24,4,3);
                uint h=Hash(x,y,11);
                if(pale&&h%100<63)Add("Reeds",x,y,"pale-reeds");
                else if(gravel&&h%100<76)Add("Rubble",x,y,"gravel");
                else if(h%100<19)Add("Bush",x,y,"green-grass");
                else if(h%173==0)Add("Reeds",x,y,"pale-reeds");
            }
            Placements=placements.AsReadOnly();
        }
        private void AddDiscoveries()
        {
            // Broad silhouettes make the uses legible at the actual game scale.
            // Water is scenery; the separate basin owns the finite drink supply.
            for(int y=1;y<=9;y++)for(int x=28;x<=39;x++)
            {
                if(y<9&&Ellipse(x,y,34,5,5.5,4.5)&&!IsPond(x,y))
                    Add("Reeds",x,y,"pale-reeds");
            }
            for(int x=29;x<=38;x++)if(x!=36)Add("RoadStone",x,9);
            Add("SpreadDrawPoint",36,9);
            Add("Waterskin",37,10);

            // Choose the new owner before staging, never replace a saved/live wall.
            for(int x=43;x<=49;x++)
                if(fieldwork&&x==46)Add("GleanersBuckledWicket",x,6);
                else Add("Wall",x,6,"low-wall");
            // This cell would otherwise receive seed-varied gravel dressing.
            if(fieldwork)Add("DrawgourdCrop",46,4);
            Add("Wall",50,6,"low-wall");Add("Wall",50,7,"low-wall");
            Add("Campfire",42,8);
            for(int x=41;x<=51;x++)Add("RoadStone",x,9);
            for(int x=44;x<=50;x+=2)for(int y=10;y<=14;y++)
                Add((x==44&&y==10)||(x==46&&y==12)||(x==50&&y==14)?"RipeCropRow":"CropRow",x,y);
            Add("Signpost",43,11);

            // The beam obstructs a short doorway, never the main y16 route.
            for(int y=14;y<=21;y++)if(y!=16&&y!=19)Add("Wall",26,y,"dark-ruin");
            Add("FallenBeam",26,19);
            for(int x=24;x<=29;x++)if(x!=26)Add("RoadStone",x,19);
        }
        /// <summary>Native wet ground for the cold-built reed pond. Does not
        /// imply a refillable source; SpreadDrawPoint owns the three drams.</summary>
        public static bool IsPond(int x,int y)=>x>=29&&x<=38&&y>=2&&y<=8&&Ellipse(x,y,34,5,4.6,3.4);
        private static bool IsDiscoveryGround(int x,int y)=>(x>=28&&x<=39&&y>=1&&y<=10)
            ||(x>=41&&x<=51&&y>=6&&y<=14)||(x>=24&&x<=29&&y>=14&&y<=21);
        /// <summary>Open border band and central cross for ordinary world travel.</summary>
        public static bool IsApproach(int x,int y)=>x<2||x>77||y==12&&x<20||x==40&&y>=6&&y<=18||y==16;
        private void Add(string bp,int x,int y,string visual=null)
        {if(occupied[x,y])return;occupied[x,y]=true;placements.Add(new Placement(bp,x,y,visual));}
        private static bool Ellipse(int x,int y,int cx,int cy,double rx,double ry)
            =>Math.Pow((x-cx)/rx,2)+Math.Pow((y-cy)/ry,2)<1;
        private uint Hash(int x,int y,uint salt)
        {unchecked{uint h=(uint)seed^2166136261u;h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;h=(h^salt)*16777619u;h^=h>>16;return h;}}
        public string Signature()
        {var s=new StringBuilder();foreach(var p in placements)s.Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y).Append(';');return s.ToString();}
    }
}
