using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Transactional fresh-zone terrain realization. Native mouth,
    /// stairs and the late sealed-library stamp remain independent authorities.</summary>
    public sealed class StillleafCompositionBuilder:IZoneBuilder
    {
        public string Name=>"StillleafComposition";
        public int Priority=>1000;
        public StillleafCompositionPlan Plan {get;private set;}
        private readonly int seed;
        private static readonly string[] Supplies={"Torch","DriedMeat","HealingTonic"};
        public StillleafCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory==null||rng==null||factory.Blueprints==null)return Reject("missing-dependency");
            if(!StillleafCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject("outside-stillleaf");
            if(zone.EntityCount!=0)return Reject("nonempty-zone");
            var p=StillleafCompositionPlan.Create(zone.ZoneID,seed);
            var required=new HashSet<string>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {required.Add(p.GroundAt(x,y));if(p.ObjectAt(x,y)!=null)required.Add(p.ObjectAt(x,y));}
            if(p.Depth==1)foreach(var bp in Supplies)required.Add(bp);
            foreach(var bp in required)
                if(!factory.Blueprints.ContainsKey(bp)||!Valid(factory.CreateEntity(bp),bp))return Reject("invalid-native:"+bp);
            var staged=new List<(Entity e,int x,int y)>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                foreach(var bp in new[]{p.GroundAt(x,y),p.ObjectAt(x,y)})
                {
                    if(bp==null)continue;var e=factory.CreateEntity(bp);
                    if(!Valid(e,bp))return Reject("invalid-placement:"+bp);
                    if(bp=="Sack")foreach(var supply in Supplies)
                    {
                        var item=factory.CreateEntity(supply);
                        if(!Valid(item,supply)||!e.GetPart<ContainerPart>().AddItem(item))return Reject("invalid-supply:"+supply);
                    }
                    staged.Add((e,x,y));
                }
            }
            var placed=new List<Entity>(staged.Count);
            foreach(var placement in staged)
            {
                if(!zone.AddEntity(placement.e,placement.x,placement.y))
                {foreach(var e in placed)zone.RemoveEntity(e);return Reject("placement-refused");}
                placed.Add(placement.e);
            }
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(p.IsApproach(x,y)||p.ObjectAt(x,y)!=null)zone.GenReservedCells.Add((x,y));
            Plan=p;Diag.Record("worldgen","StillleafCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,depth=p.Depth});return true;
        }
        private static bool Valid(Entity e,string bp)
        {
            var render=e?.GetPart<RenderPart>();var physics=e?.GetPart<PhysicsPart>();
            if(e==null||e.BlueprintName!=bp||render==null||!render.Visible||physics==null||e.GetPart<ExaminablePart>()==null)return false;
            bool wall=bp=="TepuiWall"||bp=="SandstoneWall";
            bool supply=bp=="Torch"||bp=="DriedMeat"||bp=="HealingTonic";
            if(physics.Solid!=wall||physics.Takeable!=supply||(!wall&&e.HasTag("Solid")))return false;
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(wall)
            {
                var d=e.GetPart<DestructiblePart>();
                return e.HasTag("Wall")&&d!=null&&d.HP>0&&d.MaxHP>=d.HP&&!d.Gone&&!d.Indestructible;
            }
            if(bp=="TepuiStone"||bp=="SandstoneFloor")return e.HasTag("Terrain")&&!e.HasTag("Plantable");
            if(bp=="Bush")
            {
                var d=e.GetPart<DestructiblePart>();var material=e.GetPart<MaterialPart>();
                return d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible&&material?.MaterialID=="Plant"&&e.HasPart<ThermalPart>();
            }
            if(bp=="Sack")return e.GetPart<ContainerPart>()!=null&&e.GetPart<ContainerPart>().MaxItems>=Supplies.Length;
            if(bp=="Torch")return e.GetPart<LightSourcePart>()?.Radius>0&&e.HasPart<FuelPart>();
            if(bp=="DriedMeat")return e.HasPart<FoodPart>();
            if(bp=="HealingTonic")return e.HasPart<TonicPart>();
            return bp=="DescentLedge"||bp=="RopeAnchor"||bp=="Bones";
        }
        private static bool Reject(string reason){Diag.Record("worldgen","StillleafCompositionRejected",payload:new{reason});return false;}
    }
    /// <summary>Protect existing staircase cells after the archive has carved
    /// its exterior aisle. No displacement, new stair, or barrier repair.</summary>
    public sealed class StillleafArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"StillleafArrivalReservations";
        public int Priority=>3660;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||!StillleafCompositionPlan.IsSupportedZone(zone.ZoneID))return false;
            foreach(var e in zone.GetAllEntities())if(e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>())
            {var c=zone.GetEntityCell(e);zone.GenReservedCells.Add((c.X,c.Y));}
            return true;
        }
    }
}
