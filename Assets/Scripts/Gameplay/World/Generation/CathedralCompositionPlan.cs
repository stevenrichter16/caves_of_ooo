using System;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Finite, deterministic physical layout of the Deepest Cathedral's
    /// approach, expedition descent and nave foundations. The native cathedral
    /// stamp still owns its residents and node. Height never grants travel.</summary>
    public sealed class CathedralCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Depth;
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly bool[,] approaches=new bool[Zone.Width,Zone.Height];
        public static bool IsSupportedZone(string id)=>id=="Overworld.5.4.0"||id=="Overworld.5.4.1"||id=="Overworld.5.4.2";
        public static CathedralCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("Outside the Deepest Cathedral's three-level slice.",nameof(id));
            return new CathedralCompositionPlan(id,seed);
        }
        private CathedralCompositionPlan(string id,int seed)
        {
            ZoneID=id;Depth=id[id.Length-1]-'0';
            var rng=new Random(FormationSelector.StableIndex(id+":"+seed,int.MaxValue));
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                // Main cross admits native stairs. The mouth's eastern lane
                // meets its actual lip opening; floor side doors instead align
                // with the existing grown nave's three authored thresholds.
                approaches[x,y]=Math.Abs(y-12)<=1||Math.Abs(x-40)<=1
                    ||Depth==0&&Math.Abs(x-52)<=1
                    ||Depth==2&&(Math.Abs(x-21)<=1||Math.Abs(x-57)<=1);
            }
            if(Depth==0)PilgrimageMouth(rng);
            else
            {
                EnclosingStone(rng);
                if(Depth==1)ExpeditionShelves(rng);else MemoryAlcoves(rng);
            }
        }
        private void PilgrimageMouth(Random rng)
        {
            // Four loose groves frame the destination, leaving broad quiet
            // routes. A green collar belongs outside the native mouth's lip.
            int shift=rng.Next(-3,4);
            var groves=new[]{(x:17+shift,y:5),(x:62-shift,y:5),(x:16-shift,y:19),(x:63+shift,y:19)};
            for(int y=2;y<23;y++)for(int x=3;x<77;x++)
            {
                if(IsApproach(x,y))continue;
                double lip=(x-40)*(x-40)/225.0+(y-12)*(y-12)/64.0;
                if(lip<1.2)continue;
                double influence=0;
                foreach(var g in groves)
                {double d=(x-g.x)*(x-g.x)/100.0+(y-g.y)*(y-g.y)/16.0;if(d<1)influence=Math.Max(influence,1-d);}
                if(influence<=0||rng.Next(100)>=10+influence*45)continue;
                objects[x,y]=rng.Next(4)==0?"Tree":"Bush";
            }
        }
        private void EnclosingStone(Random rng)
        {
            int phase=rng.Next(12);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(IsApproach(x,y))continue;
                int top=2+(x/15+phase)%2,bottom=2+(x/19+phase+1)%2;
                int left=4+(y/8+phase)%2,right=4+(y/9+phase+1)%2;
                if(y<top||y>=Zone.Height-bottom||x<left||x>=Zone.Width-right)objects[x,y]="SandstoneWall";
            }
        }
        private void ExpeditionShelves(Random rng)
        {
            int[] rows={4,8,15,19};
            int[] tips=new int[4];
            for(int i=0;i<rows.Length;i++)
            {
                int reach=24+rng.Next(6);bool left=i%2==0;tips[i]=left?reach-2:81-reach;
                for(int dy=0;dy<3;dy++)for(int step=3;step<reach-(dy==0?1:0);step++)
                {
                    int x=left?step:79-step,y=rows[i]+dy;
                    if(!IsApproach(x,y)&&objects[x,y]==null)objects[x,y]="DescentLedge";
                }
            }
            // The pins and cache occupy their actual shelves; ordinary native
            // items retain their own verbs. A rope anchor isn't a new climb API.
            for(int i=0;i<3;i++)objects[tips[i],rows[i]+1]="RopeAnchor";
            int sackX=Math.Max(60,tips[3]+4);
            objects[sackX,20]="Sack";objects[sackX+1,20]="Bones";
        }
        private void MemoryAlcoves(Random rng)
        {
            // Paired grown buttresses thicken the side chambers into memorable
            // bays, while the old nave stamp still writes rows7/8 and16/17.
            // These four intervals lie between its actual doorway columns.
            int[] centres={10,30,48,68};
            for(int i=0;i<centres.Length;i++)
            {
                int width=2+rng.Next(2),centre=centres[i]+rng.Next(-1,2);
                // Grown bays join the nave wall instead of stopping as short
                // detached fingers. Width and offset vary within each bay;
                // the connection to the fixed native nave is structural.
                for(int x=centre-width/2;x<centre+(width+1)/2;x++)
                {
                    for(int y=1;y<=6;y++)if(!IsApproach(x,y))objects[x,y]="SubstrateVault";
                    for(int y=18;y<Zone.Height-1;y++)if(!IsApproach(x,y))objects[x,y]="SubstrateVault";
                }
            }
        }
        public string GroundAt(int x,int y)
        {
            if(!InBounds(x,y))return null;
            if(Depth>0)return "SandstoneFloor";
            double d=(x-40)*(x-40)/169.0+(y-12)*(y-12)/36.0;
            return d<=.72?"SandstoneFloor":"Grass";
        }
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var result=new StringBuilder(ZoneID);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                result.Append('|').Append(GroundAt(x,y)).Append('/').Append(objects[x,y]??".").Append(approaches[x,y]?'A':'-');
            return result.ToString();
        }
    }
}
