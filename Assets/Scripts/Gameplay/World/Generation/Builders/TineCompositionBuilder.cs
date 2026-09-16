using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Fresh native lakeside settlement construction, staged before committing
    /// owners, interior markers or reservations. Never reconstructs a live graph.</summary>
    public sealed class TineCompositionBuilder:IZoneBuilder
    {
        public string Name=>"TineComposition";public int Priority=>1000;
        public TineCompositionPlan Plan{get;private set;}
        public Zone RealizedZone{get;private set;}
        internal bool ProfileRealized;
        private readonly int seed;
        public TineCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!TineCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-tine");
            if(zone.EntityCount!=0||(RealizedZone!=null&&!ReferenceEquals(RealizedZone,zone)))return Reject(zone,"nonempty-or-foreign-reuse");
            var plan=TineCompositionPlan.Create(zone.ZoneID,seed);var required=new HashSet<string>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++){required.Add(plan.GroundAt(x,y));if(plan.ObjectAt(x,y)!=null)required.Add(plan.ObjectAt(x,y));}
            foreach(var p in plan.Profile)required.Add(p.Blueprint);
            foreach(var bp in required)if(!Valid(Create(factory,bp),bp))return Reject(zone,"invalid-required-native:"+bp);
            var staged=new List<(Entity e,int x,int y)>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)foreach(var bp in new[]{plan.GroundAt(x,y),plan.ObjectAt(x,y)})
            {if(bp==null)continue;var e=Create(factory,bp);if(!Valid(e,bp))return Reject(zone,"invalid-placement:"+bp);staged.Add((e,x,y));}
            var added=new List<Entity>();foreach(var p in staged)
            {if(!zone.AddEntity(p.e,p.x,p.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"placement-refused");}added.Add(p.e);}
            for(int y=0;y<25;y++)for(int x=0;x<80;x++){zone.GetCell(x,y).IsInterior=plan.IsInterior(x,y);if(plan.IsReserved(x,y))zone.GenReservedCells.Add((x,y));}
            Plan=plan;RealizedZone=zone;ProfileRealized=false;
            Diag.Record("worldgen","TineCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,rooms=plan.Rooms.Count,profile=plan.Profile.Count});return true;
        }
        /// <summary>Native cave roll may use only current unreserved dry exterior
        /// ground. A detached cell or same-address foreign graph is not authority.</summary>
        public bool CanPlaceCaveEntrance(Zone zone,Cell cell)
        {
            if(zone==null||cell==null||Plan==null||!ReferenceEquals(RealizedZone,zone)||!ReferenceEquals(zone.GetCell(cell.X,cell.Y),cell))return false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                int x=cell.X+dx,y=cell.Y+dy;var c=zone.GetCell(x,y);
                if(c==null||c.IsInterior||Plan.IsApproach(x,y)||zone.GenReservedCells.Contains((x,y))||c.BlocksMovement())return false;
                // Read live owners too: a later/native source can differ from
                // the original plan, and the roll must not create a wet landing.
                bool ground=false;foreach(var owner in c.Objects){if(owner.HasPart<LiquidPoolPart>())return false;if(owner.HasTag("Terrain")&&owner.GetPart<PhysicsPart>()?.Solid==false)ground=true;}if(!ground)return false;
            }
            return true;
        }
        internal static Entity Create(EntityFactory factory,string bp)
        {if(factory?.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return null;try{return factory.CreateEntity(bp);}catch{return null;}}
        internal static bool Valid(Entity e,string bp)
        {
            var physics=e?.GetPart<PhysicsPart>();var render=e?.GetPart<RenderPart>();
            if(e==null||e.BlueprintName!=bp||physics==null||render==null||!render.Visible||render.RenderString!=Glyph(bp)||!e.HasPart<ExaminablePart>())return false;
            bool solid=bp=="SandstoneWall"||bp=="Tree"||bp=="Crate"||bp=="BoatFrame";
            if(physics.Solid!=solid||physics.Takeable||(!solid&&e.HasTag("Solid")))return false;
            if(e.HasTag("Creature")||e.HasPart<BrainPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(bp=="WaterPuddle")return e.GetPart<LiquidPoolPart>()?.LiquidId=="water"&&e.GetPart<LiquidPoolPart>().Volume>0&&e.GetPart<MaterialPart>()?.MaterialID=="Water"&&e.HasPart<ThermalPart>();
            if(e.HasPart<LiquidPoolPart>())return false;
            switch(bp)
            {
                case "Floor":case "StoneFloor":case "RoadStone":return e.HasTag("Terrain");
                case "SandstoneWall":return e.HasTag("Wall")&&Healthy(e);
                case "Tree":return e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "Bush":return e.HasTag("Vegetation")&&e.GetPart<MaterialPart>()?.MaterialID=="Plant"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "Reeds":return e.HasTag("Vegetation");
                case "Duckboard":return e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "Crate":return e.HasPart<ContainerPart>()&&e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&Healthy(e);
                case "Bed":return e.HasPart<BedPart>();case "Chair":return e.HasPart<ChairPart>();
                case "BoatFrame":return !e.HasPart<DestructiblePart>()&&!e.HasPart<ContainerPart>()&&!e.HasPart<ConversationPart>();default:return false;
            }
        }
        private static bool Healthy(Entity e){var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible;}
        private static string Glyph(string bp)
        {
            switch(bp){case "Floor":case "StoneFloor":return ".";case "RoadStone":case "Bed":case "Duckboard":return "=";case "SandstoneWall":return "#";
                case "Chair":return "h";case "Crate":return "0";case "Reeds":return "|";case "Tree":return "T";case "Bush":return ";";
                case "WaterPuddle":return "~";case "BoatFrame":return ")";default:return null;}
        }
        /// <summary>Forward the logical service anchor; native population still
        /// checks current occupancy and performs its own stocking and wiring.</summary>
        public bool TryGetServiceCell(string blueprint,out int x,out int y)
        {x=y=-1;return Plan!=null&&Plan.TryGetServiceCell(blueprint,out x,out y);}
        private bool Reject(Zone z,string reason)
        {Diag.Record("worldgen","TineCompositionRejected",payload:new{zoneId=z?.ZoneID,seed,reason});return false;}
    }

    /// <summary>Place the two native hull work frames once. Both
    /// validate before any of them becomes visible in the realized zone.</summary>
    public sealed class TineProfileBuilder:IZoneBuilder
    {
        public string Name=>"TineProfile";public int Priority=>3860;
        private readonly TineCompositionBuilder terrain;
        public TineProfileBuilder(TineCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone)||terrain.ProfileRealized)return Reject(zone,"wrong-or-replayed-owner");
            var staged=new List<(Entity e,int x,int y)>();
            foreach(var p in terrain.Plan.Profile)
            {
                var c=zone.GetCell(p.X,p.Y);if(c==null||c.BlocksMovement())return Reject(zone,"blocked-profile-cell");
                foreach(var owner in c.Objects)if(owner.HasPart<StairsUpPart>()||owner.HasPart<StairsDownPart>()||owner.HasPart<LiquidPoolPart>()||owner.BlueprintName==p.Blueprint)return Reject(zone,"occupied-profile-cell");
                var e=TineCompositionBuilder.Create(factory,p.Blueprint);if(!TineCompositionBuilder.Valid(e,p.Blueprint))return Reject(zone,"invalid-profile:"+p.Blueprint);
                e.Properties["SettlementId"]=zone.ZoneID;staged.Add((e,p.X,p.Y));
            }
            var added=new List<Entity>();foreach(var p in staged)
            {if(!zone.AddEntity(p.e,p.x,p.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"profile-placement-refused");}added.Add(p.e);}
            terrain.ProfileRealized=true;Diag.Record("worldgen","TineProfilePlaced",payload:new{zoneId=zone.ZoneID,owners=added.Count});return true;
        }
        private static bool Reject(Zone z,string reason)
        {Diag.Record("worldgen","TineProfileRejected",payload:new{zoneId=z?.ZoneID,reason});return false;}
    }

    /// <summary>Protect actual stairs and their dry standing neighbors after the
    /// native roll, before ordinary population. No carving or owner recreation.</summary>
    public sealed class TineArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"TineArrivalReservation";public int Priority=>3870;
        private readonly TineCompositionBuilder terrain;
        public TineArrivalReservationBuilder(TineCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone))
            {Diag.Record("worldgen","TineArrivalsRejected",payload:new{zoneId=zone?.ZoneID,reason="wrong-owner-or-dependency"});return false;}
            int stairs=0;foreach(var e in zone.GetAllEntities())
            {
                if(!e.HasPart<StairsDownPart>()&&!e.HasPart<StairsUpPart>())continue;var c=zone.GetEntityCell(e);if(c==null)continue;stairs++;
                zone.GenReservedCells.Add((c.X,c.Y));for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {int x=c.X+dx,y=c.Y+dy;var n=zone.GetCell(x,y);if(n==null||n.BlocksMovement())continue;bool wet=false;foreach(var o in n.Objects)if(o.HasPart<LiquidPoolPart>())wet=true;if(!wet)zone.GenReservedCells.Add((x,y));}
            }
            Diag.Record("worldgen","TineArrivalsReserved",payload:new{zoneId=zone.ZoneID,stairs});return true;
        }
    }
}
