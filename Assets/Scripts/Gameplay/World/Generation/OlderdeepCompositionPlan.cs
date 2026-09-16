using System;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original journey through an inhabited descent to the
    /// founding chamber. This plan describes native cells, never display-only
    /// collision or a second writer for the Rooted and its living plume.</summary>
    public sealed class OlderdeepCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Depth;
        private readonly string[,] objects=new string[Zone.Width,Zone.Height];
        private readonly bool[,] approaches=new bool[Zone.Width,Zone.Height];
        public static bool IsSupportedZone(string id)=>id=="Overworld.4.6.0"||id=="Overworld.4.6.1"||id=="Overworld.4.6.2";
        public static OlderdeepCompositionPlan Create(string id,int seed)
        {
            if(!IsSupportedZone(id))throw new ArgumentException("Outside Olderdeep's three-level founding journey.",nameof(id));
            return new OlderdeepCompositionPlan(id,seed);
        }
        private OlderdeepCompositionPlan(string id,int seed)
        {
            ZoneID=id;Depth=id[id.Length-1]-'0';
            var rng=new Random(FormationSelector.StableIndex(id+":"+seed,int.MaxValue));
            if(Depth==2)FoundingApproach(rng);
            else
            {
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    approaches[x,y]=Math.Abs(y-12)<=1||Math.Abs(x-40)<=1||Depth==0&&Math.Abs(x-52)<=1;
                if(Depth==0)ShelteredMouth(rng);else UsedDescent(rng);
            }
        }
        private void ShelteredMouth(Random rng)
        {
            // Two broken, crescent-like shelter belts frame the true mouth.
            // Sparse trunks have physical breathing room; dense growth is
            // walkable brush, not random impassable vegetation pockets.
            int phase=rng.Next(3),bend=rng.Next(-3,4);
            for(int y=2;y<23;y++)for(int x=3;x<77;x++)
            {
                if(IsApproach(x,y))continue;
                double lip=(x-40)*(x-40)/225.0+(y-12)*(y-12)/64.0;
                if(lip<1.25)continue;
                double band=Math.Abs(y-(x<40?5:19))-Math.Abs(x-(x<40?21+bend:61-bend))/15.0;
                if(Math.Abs(band)>2.5||rng.Next(100)>70)continue;
                objects[x,y]=(x+phase)%3==0&&(y+phase)%3==0?"Tree":"Bush";
            }
        }
        private void UsedDescent(Random rng)
        {
            int phase=rng.Next(6);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                if(IsApproach(x,y))continue;
                int top=2+(x/17+phase)%2,bottom=2+(x/21+phase)%2;
                if(y<top||y>=25-bottom||x<4||x>=76)objects[x,y]="SandstoneWall";
            }
            // A broad upper-left stopping court and lower-right turning
            // court are shallow cutbacks of actual stone. Each has a solid
            // shoulder and a broad open face onto the cross-zone passage.
            int upperStart=11+rng.Next(4),lowerStart=54+rng.Next(4);
            Court(upperStart,4,14+rng.Next(4),6,true);
            Court(lowerStart,15,15+rng.Next(3),6,false);
            objects[upperStart+3,6]="RopeAnchor";objects[upperStart+10,8]="RopeAnchor";
            objects[lowerStart+11,17]="RopeAnchor";
            objects[upperStart+7,5]="BeetleJar";objects[lowerStart+4,19]="BeetleJar";
            objects[lowerStart+7,18]="Sack";objects[lowerStart+8,18]="Bones";
        }
        private void Court(int x0,int y0,int width,int height,bool north)
        {
            for(int x=x0;x<x0+width;x++)
            {
                int shoulder=north?y0-1:y0+height;
                objects[x,shoulder]="SandstoneWall";
                if(north)for(int y=0;y<shoulder;y++)objects[x,y]="SandstoneWall";
                else for(int y=shoulder+1;y<Zone.Height;y++)objects[x,y]="SandstoneWall";
                for(int y=y0;y<y0+height;y++)objects[x,y]="DescentLedge";
            }
        }
        private void FoundingApproach(Random rng)
        {
            // Earth hugs the unchanged native main chamber. It does not leave
            // a decorative open halo beyond the later closed oval shell.
            // The eastern route stays at the northern edge, never through the
            // body's hands or its actual eastern root wall.
            int entryHalfWidth=1+rng.Next(2),westHalfWidth=1+rng.Next(2);
            int porchDepth=5+rng.Next(5),porchHalfHeight=3+rng.Next(3);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                bool main=(x-29)*(x-29)/729.0+(y-12)*(y-12)/100.0<=1;
                bool west=x<=40&&Math.Abs(y-12)<=westHalfWidth||x<=porchDepth&&Math.Abs(y-12)<=porchHalfHeight;
                bool vertical=Math.Abs(x-40)<=entryHalfWidth;
                bool east=x>=40&&y<=2;
                approaches[x,y]=west||vertical||east;
                if(!main&&!approaches[x,y])objects[x,y]="SandstoneWall";
            }
        }
        public string GroundAt(int x,int y)
        {
            if(!InBounds(x,y))return null;
            if(Depth>0)return "StoneFloor";
            double d=(x-40)*(x-40)/169.0+(y-12)*(y-12)/36.0;
            return d<=.72?"StoneFloor":"Grass";
        }
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        public string Signature()
        {
            var result=new StringBuilder(ZoneID);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)result.Append('|').Append(GroundAt(x,y)).Append('/').Append(objects[x,y]??".").Append(approaches[x,y]?'A':'-');
            return result.ToString();
        }
    }
}
