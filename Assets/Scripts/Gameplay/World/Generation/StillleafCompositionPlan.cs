using System;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Native layouts for Stillleaf's wind-cut threshold, descent,
    /// and outer archive chamber. The later sealed-library stamp owns its seal.</summary>
    public sealed class StillleafCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Depth;
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly bool[,] approach=new bool[Zone.Width,Zone.Height];
        public static bool IsSupportedZone(string id)=>id=="Overworld.2.4.0"||id=="Overworld.2.4.1"||id=="Overworld.2.4.2";
        public static StillleafCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("An exact Stillleaf stack address is required.",nameof(id));
            return new StillleafCompositionPlan(id,seed);
        }
        private StillleafCompositionPlan(string id,int seed)
        {
            ZoneID=id;Depth=id[id.Length-1]-'0';
            var rng=new Random(FormationSelector.StableIndex(id+":"+seed,int.MaxValue));
            // The native mouth opens east. Native stair searches and the sealed
            // archive's exterior connectors share these broad circulation lanes.
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                approach[x,y]=Math.Abs(y-12)<=1||Math.Abs(x-40)<=1||Math.Abs(x-52)<=1;
            if(Depth==0)Threshold(rng);else
            {Chamber(rng);if(Depth==1)ArchiveDescent(rng);else OuterBays(rng);}
        }
        private void Threshold(Random rng)
        {
            int phase=rng.Next(8);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(IsApproach(x,y))continue;
                // The centre is real walkable pink stone beneath the native
                // lip, not an invented empty shaft or new falling mechanic.
                double collar=Math.Pow((x-40)/18.0,2)+Math.Pow((y-12)/9.0,2);
                if(collar<1.12)continue;
                int left=8+(y/5+phase)%4,right=6+(y/6+phase+1)%5;
                int upper=2+(x/11+phase)%3,lower=2+(x/15+phase)%3;
                if(x<left||x>=80-right||y<upper||y>=25-lower)objects[x,y]="TepuiWall";
            }
            LipColonies(rng);
        }
        private void LipColonies(Random rng)
        {
            // Sparse at the landscape scale, contiguous at the plant scale:
            // three sheltered growth pockets outside the bare native lip.
            var centres=new[]{(x:28+rng.Next(3),y:6),(x:33+rng.Next(3),y:20),(x:47+rng.Next(3),y:5)};
            foreach(var c in centres)
                for(int y=c.y-2;y<=c.y+2;y++)for(int x=c.x-4;x<=c.x+4;x++)
                {
                    if(!InBounds(x,y)||IsApproach(x,y)||objects[x,y]!=null)continue;
                    double patch=(x-c.x)*(x-c.x)/16.0+(y-c.y)*(y-c.y)/4.0;
                    double lip=(x-40)*(x-40)/169.0+(y-12)*(y-12)/36.0;
                    if(patch<=1&&lip>1.0)objects[x,y]="Bush";
                }
        }
        private void Chamber(Random rng)
        {
            int phase=rng.Next(11);
            // Broad broken bedrock shoulders enclose negative space. The floor
            // stamp may choose any of its three native stair-free vault anchors;
            // no irreversible composition claims an interior before that choice.
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(IsApproach(x,y))continue;
                int top=2+(x/12+phase)%4,bottom=2+(x/15+phase+2)%4;
                int left=3+(y/6+phase)%4,right=3+(y/7+phase+1)%4;
                if(y<top||y>=25-bottom||x<left||x>=80-right)objects[x,y]="SandstoneWall";
            }
        }
        private void ArchiveDescent(Random rng)
        {
            // Unequal, one-sided standing shelves distinguish this quiet
            // archive approach from the Cathedral's paired procession rhythm.
            int[] rows={5,7,15},heights={3,2,5},reaches={22+rng.Next(3),25+rng.Next(2),33+rng.Next(3)};
            for(int shelf=0;shelf<rows.Length;shelf++)
            {
                bool left=shelf!=1;
                for(int dy=0;dy<heights[shelf];dy++)
                {
                    int tip=reaches[shelf]-(dy==0?2:dy==heights[shelf]-1?1:0);
                    for(int step=2;step<tip;step++)
                    {
                        int x=left?step:79-step,y=rows[shelf]+dy;
                        if(!IsApproach(x,y)&&objects[x,y]==null)objects[x,y]="DescentLedge";
                    }
                }
            }
            objects[17,6]="RopeAnchor";objects[65,8]="RopeAnchor";objects[18,16]="RopeAnchor";
            int cacheX=23+rng.Next(4);objects[cacheX,18]="Sack";objects[cacheX+1,18]="Bones";
        }
        private void OuterBays(Random rng)
        {
            // Two unequal bedrock projections shape useful outer chambers.
            // They stop short of the east-west circulation lane and never
            // assume which stair-free anchor the later native archive chooses.
            int northX=63+rng.Next(3),southX=32+rng.Next(3);
            for(int y=0;y<=9;y++)
            {
                int half=7-y/3;
                for(int x=northX-half;x<=northX+half;x++)
                    if(!IsApproach(x,y))objects[x,y]="SandstoneWall";
            }
            for(int y=16;y<25;y++)
            {
                int half=4+(y-16)/3;
                for(int x=southX-half;x<=southX+half;x++)
                    if(!IsApproach(x,y))objects[x,y]="SandstoneWall";
            }
        }
        public string GroundAt(int x,int y)=>InBounds(x,y)?(Depth==0?"TepuiStone":"SandstoneFloor"):null;
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".").Append(approach[x,y]?'!':' ');
            return s.ToString();
        }
    }
}
