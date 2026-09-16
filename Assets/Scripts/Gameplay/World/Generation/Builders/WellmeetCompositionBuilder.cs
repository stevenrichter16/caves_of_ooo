using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Staged fresh-zone shelter/route construction. The village's
    /// own later population supplies repairable services, residents and stock.</summary>
    public sealed class WellmeetCompositionBuilder:IZoneBuilder
    {
        public string Name=>"WellmeetComposition";public int Priority=>1000;
        public WellmeetCompositionPlan Plan {get;private set;}
        internal Zone RealizedZone {get;private set;}
        internal bool ProfileRealized;
        private readonly int seed;
        public WellmeetCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!WellmeetCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-wellmeet");
            if(zone.EntityCount!=0||(RealizedZone!=null&&!ReferenceEquals(RealizedZone,zone)))return Reject(zone,"nonempty-or-foreign-reuse");
            var p=WellmeetCompositionPlan.Create(zone.ZoneID,seed);
            var required=new HashSet<string>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++){required.Add(p.GroundAt(x,y));if(p.ObjectAt(x,y)!=null)required.Add(p.ObjectAt(x,y));}
            foreach(var owner in p.Profile)required.Add(owner.Blueprint);
            foreach(var bp in required)if(!factory.Blueprints.ContainsKey(bp)||!Valid(factory.CreateEntity(bp),bp))return Reject(zone,"invalid-required-native:"+bp);
            var staged=new List<(Entity e,int x,int y)>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)foreach(var bp in new[]{p.GroundAt(x,y),p.ObjectAt(x,y)})
            {if(bp==null)continue;var e=factory.CreateEntity(bp);if(!Valid(e,bp))return Reject(zone,"invalid-placement:"+bp);staged.Add((e,x,y));}
            var added=new List<Entity>();
            foreach(var s in staged)
            {if(!zone.AddEntity(s.e,s.x,s.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"placement-refused");}added.Add(s.e);}
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {zone.GetCell(x,y).IsInterior=p.IsInterior(x,y);if(p.IsReserved(x,y))zone.GenReservedCells.Add((x,y));}
            Plan=p;RealizedZone=zone;ProfileRealized=false;Diag.Record("worldgen","WellmeetCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,tents=p.Tents.Count});return true;
        }
        internal static bool Valid(Entity e,string bp)
        {
            var physics=e?.GetPart<PhysicsPart>();var render=e?.GetPart<RenderPart>();
            if(e==null||e.BlueprintName!=bp||physics==null||render==null||!render.Visible||!e.HasPart<ExaminablePart>())return false;
            bool actor=bp=="TentRightHost"||bp=="SaltMaster";bool solid=actor||bp=="TentWall"||bp=="Well"||bp=="Crate"||bp=="Rock";
            if(physics.Solid!=solid||physics.Takeable||(!solid&&e.HasTag("Solid")))return false;
            if(actor)
            {
                if(!e.HasTag("Creature")||!e.HasPart<BrainPart>()||e.GetStatValue("Hitpoints")<=0||e.GetPart<ConversationPart>()?.ConversationID!=bp+"_1")return false;
                if(bp=="SaltMaster")
                {var wants=e.GetPart<WantsMineralPart>();return wants!=null&&wants.Wants("PaleSalt")&&wants.Faction=="TentRight"&&wants.RepReward==5;}
                return true;
            }
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(bp=="Sand"||bp=="StoneFloor"||bp=="RoadStone")return e.HasTag("Terrain");
            if(bp=="TentWall")
            {var d=e.GetPart<DestructiblePart>();return e.GetPart<MaterialPart>()?.MaterialID=="Cloth"&&e.HasPart<ThermalPart>()&&d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible&&e.HasTag("Wall");}
            if(bp=="DryBrush")
            {var d=e.GetPart<DestructiblePart>();return e.HasTag("Terrain")&&e.GetPart<MaterialPart>()?.MaterialID=="DryPlant"&&e.HasPart<ThermalPart>()&&d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible;}
            if(bp=="Rock")return e.GetPart<MaterialPart>()?.MaterialID=="Stone"&&e.HasPart<ThermalPart>();
            if(bp=="Crate")
            {var d=e.GetPart<DestructiblePart>();return e.HasPart<ContainerPart>()&&e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible;}
            if(bp=="Well")return e.HasPart<WellPart>();
            if(bp=="Bed")return e.HasPart<BedPart>();
            if(bp=="Chair")return e.HasPart<ChairPart>();
            return bp=="GuestClothPole";
        }
        private bool Reject(Zone z,string reason){Diag.Record("worldgen","WellmeetCompositionRejected",payload:new{zoneId=z?.ZoneID,seed,reason});return false;}
    }
    /// <summary>The TentCamp's existing four service owners at their semantic
    /// receiving/salt courts. No re-stamping, moving a current owner or cloning.</summary>
    public sealed class WellmeetProfileBuilder:IZoneBuilder
    {
        public string Name=>"WellmeetProfile";public int Priority=>3860;
        private readonly WellmeetCompositionBuilder terrain;
        public WellmeetProfileBuilder(WellmeetCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone)||terrain.ProfileRealized)return Reject(zone,"wrong-or-replayed-owner");
            var staged=new List<(Entity e,int x,int y)>();
            foreach(var p in terrain.Plan.Profile)
            {
                var cell=zone.GetCell(p.X,p.Y);
                if(cell==null||cell.BlocksMovement())return Reject(zone,"blocked-profile-cell");
                foreach(var current in cell.Objects)
                    if(current.HasPart<StairsUpPart>()||current.HasPart<StairsDownPart>()||current.HasPart<LiquidPoolPart>()||current.BlueprintName==p.Blueprint)return Reject(zone,"occupied-profile-cell");
                if(!factory.Blueprints.ContainsKey(p.Blueprint))return Reject(zone,"missing-profile:"+p.Blueprint);
                var e=factory.CreateEntity(p.Blueprint);if(!WellmeetCompositionBuilder.Valid(e,p.Blueprint))return Reject(zone,"invalid-profile:"+p.Blueprint);
                e.Properties["SettlementId"]=zone.ZoneID;staged.Add((e,p.X,p.Y));
            }
            var added=new List<Entity>();
            foreach(var p in staged)
            {if(!zone.AddEntity(p.e,p.x,p.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"profile-placement-refused");}added.Add(p.e);}
            terrain.ProfileRealized=true;Diag.Record("worldgen","WellmeetProfilePlaced",payload:new{zoneId=zone.ZoneID,owners=added.Count});return true;
        }
        private static bool Reject(Zone zone,string reason){Diag.Record("worldgen","WellmeetProfileRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
    /// <summary>Protect only native stairs actually placed by the cave roll.
    /// Reservations prevent later residents from occupying their arrival cells.</summary>
    public sealed class WellmeetArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"WellmeetArrivalReservation";public int Priority=>3870;
        private readonly WellmeetCompositionBuilder terrain;
        public WellmeetArrivalReservationBuilder(WellmeetCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone))return false;
            foreach(var owner in zone.GetAllEntities())
            {
                if(!owner.HasPart<StairsUpPart>()&&!owner.HasPart<StairsDownPart>())continue;
                var c=zone.GetEntityCell(owner);if(c==null)continue;
                zone.GenReservedCells.Add((c.X,c.Y));
                foreach(var d in new[]{(0,-1),(-1,0),(1,0),(0,1)})
                {
                    int x=c.X+d.Item1,y=c.Y+d.Item2;
                    if(zone.InBounds(x,y)&&!zone.GetCell(x,y).BlocksMovement())zone.GenReservedCells.Add((x,y));
                }
            }
            return true;
        }
    }
}
