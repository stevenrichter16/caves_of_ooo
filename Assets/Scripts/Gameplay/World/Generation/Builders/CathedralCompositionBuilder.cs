using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Realizes a fresh native Cathedral plan, with detached, validated
    /// owners before any world mutation. The ordinary floor stamp, stairs and
    /// later content keep their own responsibilities and native mechanics.</summary>
    public sealed class CathedralCompositionBuilder:IZoneBuilder
    {
        public string Name=>"CathedralComposition";
        public int Priority=>1000;
        public CathedralCompositionPlan Plan{get;private set;}
        private readonly int seed;
        private static readonly string[] Supplies={"Torch","DriedMeat","HealingTonic"};
        public CathedralCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!CathedralCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-cathedral");
            if(zone.EntityCount!=0)return Reject(zone,"nonempty-zone");
            var plan=CathedralCompositionPlan.Create(zone.ZoneID,seed);
            var required=new HashSet<string>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {required.Add(plan.GroundAt(x,y));string bp=plan.ObjectAt(x,y);if(bp!=null)required.Add(bp);}
            if(plan.Depth==1)foreach(string bp in Supplies)required.Add(bp);
            foreach(string bp in required)if(!factory.Blueprints.ContainsKey(bp))return Reject(zone,"missing-blueprint:"+bp);
            var staged=new List<(Entity entity,int x,int y)>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                string ground=plan.GroundAt(x,y);var floor=factory.CreateEntity(ground);
                if(!Valid(floor,ground))return Reject(zone,"invalid-ground:"+ground);
                staged.Add((floor,x,y));string bp=plan.ObjectAt(x,y);if(bp==null)continue;
                var owner=factory.CreateEntity(bp);
                if(!Valid(owner,bp))return Reject(zone,"invalid-owner:"+bp);
                if(bp=="Sack")foreach(string supply in Supplies)
                {
                    var item=factory.CreateEntity(supply);
                    if(!Valid(item,supply)||!owner.GetPart<ContainerPart>().AddItem(item))return Reject(zone,"invalid-supply:"+supply);
                }
                staged.Add((owner,x,y));
            }
            var added=new List<Entity>(staged.Count);
            foreach(var placement in staged)
            {
                if(!zone.AddEntity(placement.entity,placement.x,placement.y))
                {foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"native-placement");}
                added.Add(placement.entity);
            }
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(plan.IsApproach(x,y)||plan.ObjectAt(x,y)!=null)zone.GenReservedCells.Add((x,y));
            Plan=plan;
            Diag.Record("worldgen","CathedralCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,depth=plan.Depth,owners=added.Count});
            return true;
        }
        private static bool Valid(Entity e,string blueprint)
        {
            var render=e?.GetPart<RenderPart>();var physics=e?.GetPart<PhysicsPart>();
            if(e==null||e.BlueprintName!=blueprint||render==null||!render.Visible||physics==null||e.GetPart<ExaminablePart>()==null)return false;
            string glyph;
            switch(blueprint)
            {
                case "Grass":case "SandstoneFloor":glyph=".";break;
                case "Tree":glyph="T";break;case "Bush":glyph=";";break;
                case "SandstoneWall":case "SubstrateVault":glyph="#";break;
                case "DescentLedge":glyph="=";break;case "RopeAnchor":glyph="|";break;
                case "Sack":glyph="u";break;case "Bones":case "DriedMeat":glyph="%";break;
                case "Torch":glyph="/";break;case "HealingTonic":glyph="!";break;
                default:return false;
            }
            if(render.RenderString!=glyph)return false;
            bool supply=blueprint=="Torch"||blueprint=="DriedMeat"||blueprint=="HealingTonic";
            bool solid=blueprint=="Tree"||blueprint=="SandstoneWall"||blueprint=="SubstrateVault";
            if(physics.Takeable!=supply||physics.Solid!=solid||(!solid&&e.HasTag("Solid")))return false;
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(blueprint=="Grass"||blueprint=="SandstoneFloor")
                return e.HasTag("Terrain")&&e.HasTag("Plantable")==(blueprint=="Grass");
            if(blueprint=="Tree"||blueprint=="Bush")
            {
                var material=e.GetPart<MaterialPart>();
                return Breakable(e)&&material!=null&&material.MaterialID==(blueprint=="Tree"?"Wood":"Plant")&&e.GetPart<ThermalPart>()!=null;
            }
            if(blueprint=="SandstoneWall")return e.HasTag("Wall")&&Breakable(e)&&e.GetPart<DestructiblePart>().WreckageBlueprint=="Rubble";
            if(blueprint=="SubstrateVault")return e.HasTag("Vegetation")&&!e.HasPart<DestructiblePart>();
            if(blueprint=="Sack")return e.GetPart<ContainerPart>()!=null&&e.GetPart<ContainerPart>().MaxItems>=Supplies.Length;
            if(blueprint=="Torch")return e.GetPart<LightSourcePart>()?.Radius>0&&e.GetPart<FuelPart>()!=null;
            if(blueprint=="DriedMeat")return e.HasPart<FoodPart>();
            if(blueprint=="HealingTonic")return e.HasPart<TonicPart>();
            return true;
        }
        private static bool Breakable(Entity e)
        {var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&d.MaxHP>=d.HP&&!d.Indestructible&&!d.Gone;}
        private bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","CathedralCompositionRejected",payload:new{zoneId=zone?.ZoneID,seed,reason});return false;}
    }

    /// <summary>Protects the actual completed stair cells before later native
    /// hazards/population/containers. Never moves a stair or repairs a world.</summary>
    public sealed class CathedralArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"CathedralArrivalReservations";
        public int Priority=>3660;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||!CathedralCompositionPlan.IsSupportedZone(zone.ZoneID))return false;
            int count=0;
            foreach(var e in zone.GetReadOnlyEntities())if(e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>())
            {var c=zone.GetEntityCell(e);zone.GenReservedCells.Add((c.X,c.Y));count++;}
            Diag.Record("worldgen","CathedralArrivalsReserved",payload:new{zoneId=zone.ZoneID,count});return true;
        }
    }
}
