using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Staged fresh-zone shelter/route construction. The village's
    /// own later population supplies its existing services, residents and stock.</summary>
    public sealed class MarrowstyeCompositionBuilder:IZoneBuilder
    {
        public string Name=>"MarrowstyeComposition";public int Priority=>1000;
        public MarrowstyeCompositionPlan Plan {get;private set;}
        internal Zone RealizedZone {get;private set;}
        internal bool ProfileRealized;
        private readonly int seed;
        public MarrowstyeCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!MarrowstyeCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-marrowstye");
            if(zone.EntityCount!=0||(RealizedZone!=null&&!ReferenceEquals(RealizedZone,zone)))return Reject(zone,"nonempty-or-foreign-reuse");
            var p=MarrowstyeCompositionPlan.Create(zone.ZoneID,seed);
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
            Plan=p;RealizedZone=zone;ProfileRealized=false;Diag.Record("worldgen","MarrowstyeCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,rooms=p.Rooms.Count});return true;
        }
        /// <summary>Generation-only cave placement guard tied to this realized
        /// zone. A real stair also needs safe unreserved cardinal arrival space.</summary>
        public bool CanPlaceCaveEntrance(Zone zone,Cell cell)
        {
            if(zone==null||cell==null||Plan==null||!ReferenceEquals(RealizedZone,zone)||!ReferenceEquals(zone.GetCell(cell.X,cell.Y),cell))return false;
            if(cell.IsInterior||cell.BlocksMovement()||zone.GenReservedCells.Contains((cell.X,cell.Y)))return false;
            foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)})
            {
                int x=cell.X+d.Item1,y=cell.Y+d.Item2;
                if(!zone.InBounds(x,y)||zone.GetCell(x,y).BlocksMovement()||zone.GetCell(x,y).IsInterior||zone.GenReservedCells.Contains((x,y)))return false;
            }
            return true;
        }
        internal static bool Valid(Entity e,string bp)
        {
            var physics=e?.GetPart<PhysicsPart>();var render=e?.GetPart<RenderPart>();
            if(e==null||e.BlueprintName!=bp||physics==null||render==null||!render.Visible||!e.HasPart<ExaminablePart>())return false;
            bool actor=bp=="FilerClerk";
            bool solid=actor||bp=="SandstoneWall"||bp=="StoneCoffer"||bp=="SaltCuredBody"||bp=="Crate"||bp=="Tree";
            if(physics.Solid!=solid||physics.Takeable||(!solid&&e.HasTag("Solid")))return false;
            if(actor)return render.RenderString=="@"&&e.HasTag("Creature")&&e.HasPart<BrainPart>()&&e.GetStatValue("Hitpoints")>0&&e.GetPart<ConversationPart>()?.ConversationID=="FilerClerk_1"&&e.HasPart<AISelfPreservationPart>();
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(bp=="Floor"||bp=="RoadStone"||bp=="StoneFloor"||bp=="Bones"||bp=="Rubble")return e.HasTag("Terrain");
            if(bp=="Tree"||bp=="Bush")return e.HasTag("Vegetation")&&e.GetPart<MaterialPart>()?.MaterialID==(bp=="Tree"?"Wood":"Plant")&&e.HasPart<ThermalPart>()&&HealthyDestructible(e);
            if(bp=="SandstoneWall")return e.HasTag("Wall")&&HealthyDestructible(e);
            if(bp=="StoneCoffer"||bp=="SaltCuredBody")
            {
                int weight=bp=="StoneCoffer"?110:90;var h=e.GetPart<HandlingPart>();
                return h!=null&&h.Weight==weight&&physics.Weight==weight&&h.MinLiftStrength==0&&!h.Carryable&&!h.Throwable
                    &&!e.HasPart<ContainerPart>()&&!e.HasPart<DestructiblePart>();
            }
            if(bp=="Crate")return e.HasPart<ContainerPart>()&&e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&HealthyDestructible(e);
            if(bp=="Bed")return e.HasPart<BedPart>();if(bp=="Chair")return e.HasPart<ChairPart>();
            return false;
        }
        private static bool HealthyDestructible(Entity e)
        {var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible;}
        private bool Reject(Zone z,string reason){Diag.Record("worldgen","MarrowstyeCompositionRejected",payload:new{zoneId=z?.ZoneID,seed,reason});return false;}
    }
    /// <summary>The Intake profile retains its three native owners at dry work
    /// frontages. No re-stamping, moving a current owner or cloning.</summary>
    public sealed class MarrowstyeProfileBuilder:IZoneBuilder
    {
        public string Name=>"MarrowstyeProfile";public int Priority=>3860;
        private readonly MarrowstyeCompositionBuilder terrain;
        public MarrowstyeProfileBuilder(MarrowstyeCompositionBuilder terrain){this.terrain=terrain;}
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
                var e=factory.CreateEntity(p.Blueprint);if(!MarrowstyeCompositionBuilder.Valid(e,p.Blueprint))return Reject(zone,"invalid-profile:"+p.Blueprint);
                e.Properties["SettlementId"]=zone.ZoneID;staged.Add((e,p.X,p.Y));
            }
            var added=new List<Entity>();
            foreach(var p in staged)
            {if(!zone.AddEntity(p.e,p.x,p.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"profile-placement-refused");}added.Add(p.e);}
            terrain.ProfileRealized=true;Diag.Record("worldgen","MarrowstyeProfilePlaced",payload:new{zoneId=zone.ZoneID,owners=added.Count});return true;
        }
        private static bool Reject(Zone zone,string reason){Diag.Record("worldgen","MarrowstyeProfileRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
    /// <summary>Protect only native stairs actually placed by the cave roll.
    /// Reservations prevent later residents from occupying their arrival cells.</summary>
    public sealed class MarrowstyeArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"MarrowstyeArrivalReservation";public int Priority=>3870;
        private readonly MarrowstyeCompositionBuilder terrain;
        public MarrowstyeArrivalReservationBuilder(MarrowstyeCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone)){Diag.Record("worldgen","MarrowstyeArrivalsRejected",payload:new{zoneId=zone?.ZoneID,reason="wrong-owner"});return false;}
            int stairs=0;
            foreach(var owner in zone.GetAllEntities())
            {
                if(!owner.HasPart<StairsUpPart>()&&!owner.HasPart<StairsDownPart>())continue;
                var c=zone.GetEntityCell(owner);if(c==null)continue;stairs++;
                zone.GenReservedCells.Add((c.X,c.Y));
                foreach(var d in new[]{(0,-1),(-1,0),(1,0),(0,1)})
                {
                    int x=c.X+d.Item1,y=c.Y+d.Item2;
                    if(zone.InBounds(x,y)&&!zone.GetCell(x,y).BlocksMovement())zone.GenReservedCells.Add((x,y));
                }
            }
            Diag.Record("worldgen","MarrowstyeArrivalsReserved",payload:new{zoneId=zone.ZoneID,stairs});
            return true;
        }
    }
}
