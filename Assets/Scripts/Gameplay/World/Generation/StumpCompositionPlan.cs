using System;
using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Cold-generation landform and habitat data. All coordinates name
    /// native cells; band is an authored ecology key, not simulated elevation.</summary>
    public sealed class StumpCompositionPlan
    {
        public readonly string ZoneID;
        public readonly int Seed, FocalX, FocalY, WestY, EastY, NorthX, SouthX;
        public readonly StumpBand Band;
        public readonly Formation Formation;
        private readonly int worldX, worldY;
        private readonly double phase;
        private readonly string[,] objects = new string[Zone.Width, Zone.Height];
        private readonly bool[,] approach = new bool[Zone.Width, Zone.Height];
        private readonly bool[,] habitat = new bool[Zone.Width, Zone.Height];
        private static readonly HashSet<string> wilderness = BuildIndex();

        public static bool IsWildernessZone(string id) => id != null && wilderness.Contains(id);
        private static HashSet<string> BuildIndex()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump
                    && !WorldMapAuthoring.PlaceAt(x, y).HasValue && !SinkholeSites.IsMouth(x, y)
                    && id != MultiCellPilotRuntime.ZoneID && id != FellingSiteBuilder.ZoneID
                    && !(x == 3 && y == 3)) ids.Add(id);
            }
            return ids;
        }

        /// <summary>Only canonical ordinary Stump surface addresses are valid.
        /// Preview overrides must remain in the address's authored band.</summary>
        public static StumpCompositionPlan Create(string id, int seed, Formation formation = Formation.None)
        {
            if (!IsWildernessZone(id)) throw new ArgumentException("An ordinary Stump surface address is required.", nameof(id));
            return new StumpCompositionPlan(id, seed, formation);
        }

        private StumpCompositionPlan(string id, int seed, Formation form)
        {
            ZoneID = id; Seed = seed;
            var pos = WorldMap.FromZoneID(id); worldX = pos.x; worldY = pos.y;
            Band = StumpBands.BandAt(pos.x, pos.y);
            Formation = form == Formation.None ? FormationSelector.ForStump(Band, id) : form;
            if (!(Band == StumpBand.Foothills && Formation == Formation.CascadeGorge)
                && !(Band == StumpBand.Slopes && (Formation == Formation.Grainfield || Formation == Formation.ButtressRidge))
                && !(Band == StumpBand.Summit && (Formation == Formation.SummitScrub || Formation == Formation.RimForest)))
                throw new ArgumentException("Formation does not belong to the authored Stump band.", nameof(form));
            var rng = new Random(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            phase = rng.NextDouble() * Math.PI * 2;
            FocalX = 35 + rng.Next(11); FocalY = 10 + rng.Next(5);
            WestY = 6 + Hash(worldX, worldY, 11) % 13; EastY = 6 + Hash(worldX + 1, worldY, 11) % 13;
            NorthX = 12 + Hash(worldX, worldY, 23) % 56; SouthX = 12 + Hash(worldX, worldY + 1, 23) % 56;

            Route(0, WestY, FocalX, FocalY); Route(79, EastY, FocalX, FocalY);
            Route(NorthX, 0, FocalX, FocalY); Route(SouthX, 24, FocalX, FocalY);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (Ellipse(x, y, FocalX, FocalY, 6, 3) < 1) approach[x, y] = true;

            switch (Formation)
            {
                case Formation.CascadeGorge:
                    Array.Clear(approach,0,approach.Length);
                    Gorge(rng);
                    var dry=FindDryFocal(); FocalX=dry.x; FocalY=dry.y;
                    RouteAroundSpray();
                    break;
                case Formation.Grainfield: Grain(rng); break;
                case Formation.ButtressRidge: Buttresses(rng); break;
                case Formation.SummitScrub: Domes(rng); break;
                case Formation.RimForest: Forest(rng); break;
            }
            if (Band == StumpBand.Summit) SummitHabitats(rng);
            if (Band == StumpBand.Slopes) Veins(rng);
            RepairOpenPockets();
            MarkHabitats();
        }

        private void Gorge(Random rng)
        {
            // Three intact unequal spray basins follow one drainage. Dry necks
            // and low ledges separate them; we do not invent bridges or water
            // simulation for this descriptive native terrain.
            for (int y = 2; y < 23; y++) for (int x = 3; x < 77; x++)
            {
                double centre = 12 + Math.Sin(x * .065 + phase) * 5;
                double distance = Math.Abs(y - centre);
                double basin = Math.Min(Ellipse(x,y,17,12+Math.Sin(17*.065+phase)*5,9,3),
                    Math.Min(Ellipse(x,y,42,12+Math.Sin(42*.065+phase)*5,11,3.8),
                        Ellipse(x,y,66,12+Math.Sin(66*.065+phase)*5,8,3)));
                if (basin < 1) Put(x,y,"SprayPool");
                else if (distance > 4 && distance < 6) Put(x,y,"TepuiWall");
                else if (distance > 2 && distance < 4 && x % 13 < 6 && y % 3 == 0) Put(x,y,"DescentLedge");
                else if (distance < 4.5 && Roll(x,y,19) < 10) Put(x,y,"Bush");
            }
        }

        private void Grain(Random rng)
        {
            // East-west is invariant across seeds and addresses: these are the
            // fossil Tree's aligned fibers, not randomly rotated desert dunes.
            for (int row = 0; row < 3; row++)
            {
                int cy = 4 + row * 8 + rng.Next(2), gapA = 13 + rng.Next(17), gapB = 49 + rng.Next(18);
                for (int x = 3 + rng.Next(5); x < 76 - rng.Next(3); x++)
                {
                    if (Math.Abs(x-gapA) < 4 || Math.Abs(x-gapB) < 4) continue;
                    Put(x,cy,"GrainRidge");
                    if (x > 7 && x < 73) Put(x,cy+1,"GrainRidge");
                }
            }
            SlopeLife();
        }

        private void Buttresses(Random rng)
        {
            for (int finger = 0; finger < 4; finger++)
            {
                int start = 12 + finger * 18 + rng.Next(4), end = 17 + rng.Next(5);
                double lean = (finger - 1.5) * .24;
                for (int y = 2; y <= end; y++)
                {
                    double centre = start + (y-2) * lean + Math.Sin(y*.18+phase)*1.1;
                    double half = y < 10 ? 2.3 : y < 17 ? 1.6 : .65;
                    for (int x = (int)(centre-3); x <= centre+3; x++)
                        if (Math.Abs(x-centre) < half) Put(x,y,"GrainRidge");
                }
            }
            SlopeLife();
        }

        private void SlopeLife()
        {
            // Root recesses hold small living pockets; clear expanses remain
            // readable. A Tree stays a real destructible/flammable Tree.
            for (int y = 3; y < 22; y++) for (int x = 4; x < 76; x++)
            {
                if (objects[x,y] != null || !Near(x,y,"GrainRidge",2)) continue;
                if (x % 4 == 0 && y % 4 == 0 && Roll(x,y,31) < 58) Put(x,y,"Tree");
                else if (Roll(x,y,37) < 9) Put(x,y,"Bush");
            }
        }

        private void Domes(Random rng)
        {
            for (int i = 0; i < 4; i++)
            {
                int cx = 10 + i * 19 + rng.Next(6), cy = (i % 2 == 0 ? 6 : 18) + rng.Next(2);
                double rx = 4 + rng.Next(3), ry = 2.5 + rng.NextDouble();
                for (int y = 2; y < 23; y++) for (int x = 2; x < 78; x++)
                    if (Ellipse(x,y,cx,cy,rx,ry) < 1) Put(x,y,"StoneDome");
            }
        }

        private void Forest(Random rng)
        {
            for (int y = 3; y < 22; y++) for (int x = 3; x < 77; x++)
            {
                double centre = 12 + Math.Sin(x * .07 + phase) * 5;
                double distance = Math.Abs(y-centre);
                if (distance > 5.5) continue;
                // Unequal islands and an open central crack replace twin fences.
                if (distance > 1.5 && x % 3 == 0 && y % 3 == 0 && Roll(x,y,41) < 82) Put(x,y,"Tree");
                else if (distance > 2 && Roll(x,y,43) < 20) Put(x,y,"Bush");
            }
            for (int i=0;i<3;i++)
            {
                int cx=9+i*27+rng.Next(5),cy=i%2==0?3:21;
                for(int dy=-1;dy<=1;dy++)for(int dx=-2;dx<=2;dx++)Put(cx+dx,cy+dy,"StoneDome");
            }
        }

        private void SummitHabitats(Random rng)
        {
            // Trees establish the sheltered pockets first. Tanks belong to
            // the lee of stone (scrub) or living cover (rim), never random dots.
            int trees=0;
            for(int attempt=0;attempt<1000 && trees<6;attempt++)
            {
                int x=5+rng.Next(70),y=4+rng.Next(17);
                if(approach[x,y] || NearApproach(x,y,1) || objects[x,y]!=null)continue;
                if(Near(x,y,"Tree",2))continue;
                if(Formation==Formation.SummitScrub&&!Near(x,y,"StoneDome",3))continue;
                Put(x,y,"Tree");trees++;
            }
            var candidates=new List<(int x,int y)>();
            for(int y=3;y<22;y++)for(int x=3;x<77;x++)
            {
                if(approach[x,y]||objects[x,y]!=null)continue;
                bool shelter=Formation==Formation.SummitScrub?Near(x,y,"StoneDome",2):Near(x,y,"Tree",3);
                if(shelter)candidates.Add((x,y));
            }
            for(int count=0;count<8&&candidates.Count>0;count++)
            {
                int pick=rng.Next(candidates.Count);var c=candidates[pick];candidates.RemoveAt(pick);
                Put(c.x,c.y,"TankBrocchinia");
            }
        }

        private bool DryCentre(int x,int y)
        {
            if(!InBounds(x,y))return false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                if(InBounds(x+dx,y+dy)&&objects[x+dx,y+dy]=="SprayPool")return false;
            return true;
        }
        private (int x,int y) FindDryFocal()
        {
            int best=int.MaxValue,bx=-1,by=-1;
            for(int y=3;y<22;y++)for(int x=3;x<77;x++)
            {
                int d=(x-FocalX)*(x-FocalX)+(y-FocalY)*(y-FocalY);
                if(d<best&&DryCentre(x,y)){best=d;bx=x;by=y;}
            }
            if(bx<0)throw new InvalidOperationException("Stump drainage has no dry arrival clearing.");
            return(bx,by);
        }
        private void RouteAroundSpray()
        {
            var previous=new int[Zone.Width,Zone.Height];
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)previous[x,y]=-1;
            var q=new Queue<(int x,int y)>();q.Enqueue((FocalX,FocalY));previous[FocalX,FocalY]=FocalY*Zone.Width+FocalX;
            while(q.Count>0)
            {
                var c=q.Dequeue();
                for(int d=0;d<4;d++)
                {
                    int x=c.x+(d==0?1:d==1?-1:0),y=c.y+(d==2?1:d==3?-1:0);
                    if(!DryCentre(x,y)||previous[x,y]>=0)continue;
                    previous[x,y]=c.y*Zone.Width+c.x;q.Enqueue((x,y));
                }
            }
            foreach(var entry in new[]{(x:0,y:WestY),(x:79,y:EastY),(x:NorthX,y:0),(x:SouthX,y:24)})
            {
                int x=entry.x,y=entry.y;
                if(previous[x,y]<0)throw new InvalidOperationException("No dry route around native spray basin.");
                while(true)
                {
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(x+dx,y+dy))
                        {approach[x+dx,y+dy]=true;objects[x+dx,y+dy]=null;}
                    if(x==FocalX&&y==FocalY)break;
                    int p=previous[x,y];x=p%Zone.Width;y=p/Zone.Width;
                }
            }
        }

        private void Veins(Random rng)
        {
            int placed=0;
            for(int attempt=0;attempt<500 && placed<3;attempt++)
            {
                int x=4+rng.Next(69),y=3+rng.Next(19);bool clear=true;
                for(int dx=-1;dx<=4;dx++)for(int dy=-1;dy<=1;dy++)
                    if(approach[x+dx,y+dy] || Solid(objects[x+dx,y+dy]))clear=false;
                if(!clear)continue;
                for(int dx=0;dx<4;dx++)objects[x+dx,y]="TepuiboneVein";
                placed++;
            }
        }

        private void MarkHabitats()
        {
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                habitat[x,y]=!approach[x,y] && !Solid(objects[x,y])
                    && (objects[x,y]=="SprayPool" || Band==StumpBand.Summit
                        && (Near(x,y,"Tree",2)||Near(x,y,"TankBrocchinia",2)));
        }

        private bool Near(int x,int y,string bp,int radius)
        {
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
                if(InBounds(x+dx,y+dy)&&objects[x+dx,y+dy]==bp)return true;
            return false;
        }
        private bool NearApproach(int x,int y,int radius)
        {
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
                if(InBounds(x+dx,y+dy)&&approach[x+dx,y+dy])return true;
            return false;
        }
        private void Put(int x,int y,string bp)
        {if(x>=2&&y>=2&&x<78&&y<23&&!approach[x,y])objects[x,y]=bp;}
        private void Route(int ax,int ay,int bx,int by)
        {
            int steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=steps;i++)
            {
                double t=steps==0?0:i/(double)steps;
                int x=(int)Math.Round(ax+(bx-ax)*t),y=(int)Math.Round(ay+(by-ay)*t);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(x+dx,y+dy))
                    {approach[x+dx,y+dy]=true;objects[x+dx,y+dy]=null;}
            }
        }
        private static bool Solid(string bp)=>bp=="TepuiWall"||bp=="GrainRidge"||bp=="StoneDome"||bp=="Tree"||bp=="TepuiboneVein";
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
            throw new InvalidOperationException("Stump reachability repair exceeded finite cell budget.");
        }
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approach[x,y];
        /// <summary>Walkable protected native habitat, temporarily exposed only
        /// during the normal band population pass.</summary>
        public bool IsHabitat(int x,int y)=>InBounds(x,y)&&habitat[x,y];
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height;
        private static double Ellipse(double x,double y,double cx,double cy,double rx,double ry)
            =>(x-cx)*(x-cx)/(rx*rx)+(y-cy)*(y-cy)/(ry*ry);
        private int Roll(int x,int y,int salt)=>Hash(worldX*Zone.Width+x,worldY*Zone.Height+y,salt)%100;
        private int Hash(int x,int y,int salt)
        {
            unchecked{uint h=(uint)Seed^2166136261u;h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;
                h=(h^(uint)salt)*16777619u;h^=h>>16;h*=2246822519u;h^=h>>13;return (int)(h&0x7fffffffu);}
        }
        public string Signature()
        {
            var s=new StringBuilder(Band+":"+Formation+":");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                s.Append(objects[x,y]??(approach[x,y]?"_":".")).Append(habitat[x,y]?"h|":"|");
            return s.ToString();
        }
    }
}
