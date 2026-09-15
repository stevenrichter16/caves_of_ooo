using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Fresh native terrain for exactly Ginmere mouth/descent/floor.
    /// Existing stair builders own actual travel. No engine-height simulation.</summary>
    public sealed class GinmereCompositionBuilder:IZoneBuilder
    {
        public string Name=>"GinmereComposition";
        public int Priority=>1000;
        public GinmereCompositionPlan Plan {get;private set;}
        private readonly int seed;
        public GinmereCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory==null||rng==null)return Reject("missing-dependency");
            if(!GinmereCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject("outside-ginmere");
            if(zone.EntityCount!=0)return Reject("nonempty-zone");
            var p=GinmereCompositionPlan.Create(zone.ZoneID,seed);
            var required=new HashSet<string>{p.GroundAt(0,0)};
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {required.Add(p.GroundAt(x,y));if(p.ObjectAt(x,y)!=null)required.Add(p.ObjectAt(x,y));}
            if(p.Depth==1){required.Add("Torch");required.Add("DriedMeat");required.Add("HealingTonic");}
            if(p.Depth==2){required.Add("GinFrog");required.Add("PricklebrowNest");required.Add("PrickleBrowGecko");}
            foreach(string bp in required)if(factory.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return Reject("missing-blueprint:"+bp);
            // Create and validate detached owners before touching the zone.
            // BuilderSpawn and Part parameter loading fail soft; mere presence
            // in the JSON dictionary does not establish a usable native object.
            var staged=new List<(Entity entity,int x,int y)>();
            foreach(string bp in required)
                if(!Valid(factory.CreateEntity(bp),bp))return Reject("invalid-native-contract:"+bp);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                string ground=p.GroundAt(x,y);
                var floor=factory.CreateEntity(ground);
                if(!Valid(floor,ground))return Reject("invalid-ground:"+ground);
                staged.Add((floor,x,y));
                string bp=p.ObjectAt(x,y);if(bp==null)continue;
                var e=factory.CreateEntity(bp);
                if(!Valid(e,bp))return Reject("invalid-object:"+bp);
                if(bp=="Sack")
                    foreach(string supply in new[]{"Torch","DriedMeat","HealingTonic"})
                    {
                        var item=factory.CreateEntity(supply);
                        if(!Valid(item,supply)||!e.GetPart<ContainerPart>().AddItem(item))return Reject("invalid-supply:"+supply);
                    }
                staged.Add((e,x,y));
            }
            if(p.Depth==2)
            {
                var faunaRng=new Random(FormationSelector.StableIndex(zone.ZoneID+":frogs:"+seed,int.MaxValue));
                int count=2+faunaRng.Next(3);
                var banks=new List<(int x,int y)>();
                for(int y=2;y<Zone.Height-2;y++)for(int x=3;x<Zone.Width-3;x++)
                {
                    if(p.ObjectAt(x,y)!=null||p.IsApproach(x,y))continue;
                    bool near=false;
                    for(int dy=-3;dy<=3&&!near;dy++)for(int dx=-3;dx<=3;dx++)
                        if(p.ObjectAt(x+dx,y+dy)=="MirePool"){near=true;break;}
                    if(near)banks.Add((x,y));
                }
                for(int i=banks.Count-1;i>0;i--)
                {int j=faunaRng.Next(i+1);var swap=banks[i];banks[i]=banks[j];banks[j]=swap;}
                var selected=new List<(int x,int y)>();
                foreach(var candidate in banks)
                {
                    bool close=false;
                    foreach(var prior in selected)
                        if(prior.y==candidate.y||Math.Max(Math.Abs(prior.x-candidate.x),Math.Abs(prior.y-candidate.y))<2){close=true;break;}
                    if(close)continue;
                    var frog=factory.CreateEntity("GinFrog");
                    if(!Valid(frog,"GinFrog"))return Reject("invalid-frog");
                    staged.Add((frog,candidate.x,candidate.y));selected.Add(candidate);
                    if(selected.Count==count)break;
                }
                if(selected.Count<count)return Reject("insufficient-frog-bank");
            }
            var placed=new List<Entity>(staged.Count);
            foreach(var placement in staged)
            {
                if(!zone.AddEntity(placement.entity,placement.x,placement.y))
                {
                    foreach(var owner in placed)zone.RemoveEntity(owner);
                    return Reject("native-placement");
                }
                placed.Add(placement.entity);
            }
            // Publish reservations only after every staged native owner landed.
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(p.IsApproach(x,y)||p.ObjectAt(x,y)!=null)zone.GenReservedCells.Add((x,y));
            foreach(var placement in staged)
                placement.entity.GetPart<TileStateSourcePart>()?.Seed(zone,placement.x,placement.y);
            Plan=p;Diag.Record("worldgen","GinmereCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,depth=p.Depth});return true;
        }
        private static bool Valid(Entity e,string bp)
        {
            if(e==null||e.GetPart<RenderPart>()==null)return false;
            if(bp=="Sack")return e.GetPart<ContainerPart>()!=null;
            if(bp=="PricklebrowNest")return e.GetPart<PricklebrowNestPart>()!=null;
            if(bp=="PrickleBrowGecko"||bp=="GinFrog")
                return e.GetPart<BrainPart>()!=null&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0;
            if(bp=="MirePool")
            {
                var liquid=e.GetPart<LiquidPoolPart>();var source=e.GetPart<TileStateSourcePart>();
                return liquid!=null&&liquid.LiquidId=="bog-mire"&&liquid.Volume>0
                    &&source!=null&&source.Coating=="water"&&source.CoatingTurns>0
                    &&e.GetPart<ThermalPart>()!=null&&e.GetPart<DestructiblePart>()!=null&&e.GetPart<BurnOffGasPart>()!=null;
            }
            return true;
        }
        private static bool Reject(string reason){Diag.Record("worldgen","GinmereCompositionRejected",payload:new{reason});return false;}
    }

    /// <summary>Reserves actual staircase cells before ordinary population and
    /// containers. Does not move stairs or manufacture connection coordinates.</summary>
    public sealed class GinmereArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"GinmereArrivalReservations";
        public int Priority=>3650;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||!GinmereCompositionPlan.IsSupportedZone(zone.ZoneID))return false;
            int count=0;
            foreach(var e in zone.GetAllEntities())if(e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>())
            {var c=zone.GetEntityCell(e);zone.GenReservedCells.Add((c.X,c.Y));count++;}
            Diag.Record("worldgen","GinmereArrivalsReserved",payload:new{zoneId=zone.ZoneID,count});return true;
        }
    }

    /// <summary>Runs native nest placement after residents and props; its actual
    /// sixteen-defender capacity is checked against the completed native zone.</summary>
    public sealed class GinmereNestFinalizer:IZoneBuilder
    {
        public string Name=>"GinmereNestFinalizer";
        public int Priority=>4400;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||zone.ZoneID!="Overworld.2.7.2"||factory==null||rng==null)return false;
            return new SimaNestBuilder().BuildZone(zone,factory,rng);
        }
    }
}
