using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Realizes a fresh native Olderdeep plan, with detached, validated
    /// owners before any world mutation. The ordinary floor stamp, stairs and
    /// later content keep their own responsibilities and native mechanics.</summary>
    public sealed class OlderdeepCompositionBuilder:IZoneBuilder
    {
        public string Name=>"OlderdeepComposition";
        public int Priority=>1000;
        public OlderdeepCompositionPlan Plan{get;private set;}
        private readonly int seed;
        private static readonly string[] Supplies={"Torch","DriedMeat","HealingTonic"};
        public OlderdeepCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!OlderdeepCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-olderdeep");
            if(zone.EntityCount!=0)return Reject(zone,"nonempty-zone");
            var plan=OlderdeepCompositionPlan.Create(zone.ZoneID,seed);
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
            Diag.Record("worldgen","OlderdeepCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,depth=plan.Depth,owners=added.Count});
            return true;
        }
        internal static bool Valid(Entity e,string blueprint)
        {
            var render=e?.GetPart<RenderPart>();var physics=e?.GetPart<PhysicsPart>();
            if(e==null||e.BlueprintName!=blueprint||render==null||!render.Visible||physics==null||e.GetPart<ExaminablePart>()==null)return false;
            string glyph;
            switch(blueprint)
            {
                case "Grass":case "StoneFloor":glyph=".";break;
                case "Tree":glyph="T";break;case "Bush":glyph=";";break;
                case "SandstoneWall":glyph="#";break;case "BeetleJar":glyph="j";break;
                case "DescentLedge":glyph="=";break;case "RopeAnchor":glyph="|";break;
                case "Sack":glyph="u";break;case "Bones":case "DriedMeat":glyph="%";break;
                case "Torch":glyph="/";break;case "HealingTonic":glyph="!";break;
                default:return false;
            }
            if(render.RenderString!=glyph)return false;
            bool supply=blueprint=="Torch"||blueprint=="DriedMeat"||blueprint=="HealingTonic";
            bool solid=blueprint=="Tree"||blueprint=="SandstoneWall";
            if(physics.Takeable!=supply||physics.Solid!=solid||(!solid&&e.HasTag("Solid")))return false;
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(blueprint=="Grass"||blueprint=="StoneFloor")
                return e.HasTag("Terrain")&&e.HasTag("Plantable")==(blueprint=="Grass");
            if(blueprint=="Tree"||blueprint=="Bush")
            {
                var material=e.GetPart<MaterialPart>();
                return Breakable(e)&&material!=null&&material.MaterialID==(blueprint=="Tree"?"Wood":"Plant")&&e.GetPart<ThermalPart>()!=null;
            }
            if(blueprint=="SandstoneWall")return e.HasTag("Wall")&&Breakable(e)&&e.GetPart<DestructiblePart>().WreckageBlueprint=="Rubble";
            if(blueprint=="BeetleJar")
            {
                var light=e.GetPart<LightSourcePart>();
                return light!=null&&light.Radius==2&&light.Intensity==.5f&&light.LightColor=="&Y"&&!e.HasPart<DestructiblePart>();
            }
            if(blueprint=="Sack")return e.GetPart<ContainerPart>()!=null&&e.GetPart<ContainerPart>().MaxItems>=Supplies.Length;
            if(blueprint=="Torch")return e.GetPart<LightSourcePart>()?.Radius>0&&e.GetPart<FuelPart>()!=null;
            if(blueprint=="DriedMeat")return e.HasPart<FoodPart>();
            if(blueprint=="HealingTonic")return e.HasPart<TonicPart>();
            return true;
        }
        private static bool Breakable(Entity e)
        {var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&d.MaxHP>=d.HP&&!d.Indestructible&&!d.Gone;}
        private bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","OlderdeepCompositionRejected",payload:new{zoneId=zone?.ZoneID,seed,reason});return false;}
    }

    /// <summary>Fresh-generation passages across the later founding shell.
    /// The base's finite entry corridors are restored only where the native
    /// stamp placed ordinary stone. No sacred owner, east wall, or gap is
    /// removed; an already inhabited zone is never repaired on access.</summary>
    public sealed class OlderdeepArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"OlderdeepArrivalReservations";
        public int Priority=>3660;
        private readonly int seed;
        public OlderdeepArrivalReservationBuilder(int seed=64){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null)return Reject(zone,"missing-dependency");
            if(!OlderdeepCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-olderdeep");
            int count=0,opened=0;
            if(zone.ZoneID=="Overworld.4.6.2")
            {
                Entity body=null;
                foreach(var e in zone.GetReadOnlyEntities())if(e.BlueprintName=="TheRooted"){body=e;break;}
                if(body==null)return Reject(zone,"missing-founding-stamp");
                int bodyY=zone.GetEntityCell(body).Y;
                var plan=OlderdeepCompositionPlan.Create(zone.ZoneID,seed);
                var removals=new List<Entity>();
                var floors=new List<(Entity entity,int x,int y)>();
                var reservations=new List<(int x,int y)>();
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(!plan.IsApproach(x,y))continue;
                    // Preserve the authored eastern root wall and its embrace
                    // even when unusual stairs force the body onto another row.
                    if(x>=55&&x<=67&&Math.Abs(y-bodyY)<=6)continue;
                    var cell=zone.GetCell(x,y);
                    bool hasFloor=false,wall=false;
                    foreach(var e in cell.Objects)
                    {if(e.BlueprintName=="SandstoneWall"){removals.Add(e);wall=true;}if(e.BlueprintName=="StoneFloor")hasFloor=true;}
                    if(wall&&!hasFloor)
                    {
                        var floor=factory.CreateEntity("StoneFloor");
                        if(!OlderdeepCompositionBuilder.Valid(floor,"StoneFloor"))return Reject(zone,"invalid-entry-floor");
                        floors.Add((floor,x,y));
                    }
                    reservations.Add((x,y));
                }
                foreach(var e in removals){zone.RemoveEntity(e);opened++;}
                foreach(var p in floors)zone.AddEntity(p.entity,p.x,p.y);
                foreach(var p in reservations)zone.GenReservedCells.Add(p);
            }
            foreach(var e in zone.GetReadOnlyEntities())if(e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>())
            {
                var c=zone.GetEntityCell(e);zone.GenReservedCells.Add((c.X,c.Y));count++;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {var n=zone.GetCell(c.X+dx,c.Y+dy);if(n!=null&&!n.BlocksMovement())zone.GenReservedCells.Add((n.X,n.Y));}
            }
            Diag.Record("worldgen","OlderdeepArrivalsReserved",payload:new{zoneId=zone.ZoneID,count,opened});return true;
        }
        private bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","OlderdeepArrivalsRejected",payload:new{zoneId=zone?.ZoneID,seed,reason});return false;}
    }
}
