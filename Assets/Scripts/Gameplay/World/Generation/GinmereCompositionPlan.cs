using System;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Deterministic native-cell layout for Ginmere's three existing
    /// levels. Height is presentation only; the stair registry owns travel.</summary>
    public sealed class GinmereCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Depth;
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly bool[,] approaches=new bool[Zone.Width,Zone.Height];
        public static bool IsSupportedZone(string id)=>id=="Overworld.2.7.0"||id=="Overworld.2.7.1"||id=="Overworld.2.7.2";
        public static GinmereCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("Outside Ginmere's authored three-level slice.",nameof(id));
            return new GinmereCompositionPlan(id,seed);
        }
        private GinmereCompositionPlan(string id,int seed)
        {
            ZoneID=id;Depth=id[id.Length-1]-'0';
            var rng=new Random(FormationSelector.StableIndex(id+":"+seed,int.MaxValue));
            // A broad cross links wilderness exits and the centre-biased native
            // stair search. The eastern lane approaches the mouth's native gap.
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                approaches[x,y]=Math.Abs(y-12)<=1||Math.Abs(x-40)<=1||Math.Abs(x-52)<=1;
            if(Depth==0)Mouth(rng);else
            {
                CliffMasses(rng);
                if(Depth==1)Terraces(rng);else Basin(rng);
            }
        }
        private void Mouth(Random rng)
        {
            // A green collar around the known native mouth, rather than
            // scattering trees across its bare-stone landing or eastern gap.
            for(int y=2;y<23;y++)for(int x=3;x<77;x++)
            {
                double d=Math.Pow((x-40)/18.0,2)+Math.Pow((y-12)/9.0,2);
                if(d<1.02||d>2.7||IsApproach(x,y))continue;
                int chance=d<1.8?32:12;
                if(rng.Next(100)<chance)objects[x,y]=rng.Next(4)==0?"Tree":"Bush";
            }
        }
        private void Terraces(Random rng)
        {
            // Shelves grow inward from alternating cliff sides. Every shelf
            // remains connected to stone instead of floating as a display strip.
            int[] rows={4,7,15,18};
            for(int row=0;row<rows.Length;row++)
            {
                bool left=row%2==0;int reach=23+rng.Next(6),y0=rows[row];
                for(int dy=0;dy<3;dy++)
                {
                    int tip=reach-(dy==0?2:dy==2?1:0);
                    for(int step=2;step<tip;step++)
                    {
                        int x=left?step:79-step,y=y0+dy;
                        if(!IsApproach(x,y)&&objects[x,y]==null)objects[x,y]="DescentLedge";
                    }
                }
            }
            objects[9,7]="RopeAnchor";objects[66,10]="RopeAnchor";objects[23,18]="RopeAnchor";
            int cacheX=20+rng.Next(9);objects[cacheX,20]="Sack";objects[cacheX+1,20]="Bones";
        }
        private void Basin(Random rng)
        {
            // The joined basin stays west of the native centre stair search.
            // An eastern bank is intentionally broad enough for sixteen native
            // defenders; it is not blanket-reserved by this terrain builder.
            int cx=21+rng.Next(4),cy=6+rng.Next(2),rx=12+rng.Next(3),ry=4+rng.Next(2);
            for(int y=1;y<11;y++)for(int x=3;x<38;x++)
                if(Math.Pow((x-cx)/(double)rx,2)+Math.Pow((y-cy)/(double)ry,2)<=1)
                    if(objects[x,y]==null)objects[x,y]="MirePool";
            for(int y=2;y<23;y++)for(int x=4;x<77;x++)
            {
                if(objects[x,y]!=null||IsApproach(x,y))continue;
                double d=Math.Pow((x-cx)/(double)(rx+3),2)+Math.Pow((y-cy)/(double)(ry+3),2);
                if(d>=.9&&d<1.5&&rng.Next(100)<20)objects[x,y]="Bush";
            }
        }
        private void CliffMasses(Random rng)
        {
            int phase=rng.Next(7);
            // Long, coherent edge masses vary their inward face over broad
            // segments. Cross routes remain open through the enclosing stone.
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(IsApproach(x,y))continue;
                int top=2+(x/13+phase)%3,bottom=2+(x/17+phase+1)%3;
                int left=3+(y/7+phase)%2,right=3+(y/8+phase+1)%2;
                if(y<top||y>=Zone.Height-bottom||x<left||x>=Zone.Width-right)
                    objects[x,y]="SandstoneWall";
            }
        }
        /// <summary>The mouth centre is a walkable bare-stone landing. Its
        /// appearance does not invent an abyss, falling, or absent collision.</summary>
        public string GroundAt(int x,int y)
        {
            if(!InBounds(x,y))return null;
            if(Depth>0)return "SandstoneFloor";
            double d=Math.Pow((x-40)/13.0,2)+Math.Pow((y-12)/6.0,2);
            return d<=.72?"SandstoneFloor":"Grass";
        }
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)s.Append('|').Append(objects[x,y]??".");
            return s.ToString();
        }
    }
}
