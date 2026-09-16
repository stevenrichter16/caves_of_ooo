using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Fresh native crossroads settlement construction, staged before committing
    /// owners, interior markers or reservations. Never reconstructs a live graph.</summary>
    public sealed class GantryCompositionBuilder:IZoneBuilder
    {
        public string Name=>"GantryComposition";public int Priority=>1000;
        public GantryCompositionPlan Plan{get;private set;}
        public Zone RealizedZone{get;private set;}
        internal bool ProfileRealized;
        private readonly int seed;
        public GantryCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!GantryCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-gantry");
            if(zone.EntityCount!=0||(RealizedZone!=null&&!ReferenceEquals(RealizedZone,zone)))return Reject(zone,"nonempty-or-foreign-reuse");
            var plan=GantryCompositionPlan.Create(zone.ZoneID,seed);var required=new HashSet<string>();
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
            Diag.Record("worldgen","GantryCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,rooms=plan.Rooms.Count,profile=plan.Profile.Count});return true;
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
                foreach(var owner in c.Objects)if(owner.HasPart<LiquidPoolPart>())return false;
            }
            return true;
        }
        internal static Entity Create(EntityFactory factory,string bp)
        {if(factory?.Blueprints==null||!factory.Blueprints.ContainsKey(bp))return null;try{return factory.CreateEntity(bp);}catch{return null;}}
        internal static bool Valid(Entity e,string bp)
        {
            var physics=e?.GetPart<PhysicsPart>();var render=e?.GetPart<RenderPart>();
            if(e==null||e.BlueprintName!=bp||physics==null||render==null||!render.Visible||render.RenderString!=Glyph(bp)||!e.HasPart<ExaminablePart>())return false;
            bool actor=bp=="TentRightHost"||bp=="GantryRegistrar";
            bool solid=actor||bp=="GantryTimberWall"||bp=="TentWall"||bp=="GantryRegistryDesk"||bp=="GantryExchangeCounter"||bp=="GantryWayboard"||bp=="Crate"||bp=="Rock";
            if(physics.Solid!=solid||physics.Takeable||(!solid&&e.HasTag("Solid")))return false;
            if(actor)
                return e.HasTag("Creature")&&e.HasPart<BrainPart>()&&e.HasPart<InventoryPart>()&&e.GetStatValue("Hitpoints")>0
                    &&e.GetPart<ConversationPart>()?.ConversationID==bp+"_1"&&e.HasPart<AISelfPreservationPart>()&&!e.HasPart<TraderPart>()
                    &&e.GetTag("Faction")== (bp=="TentRightHost"?"TentRight":"PaleCuration");
            if(e.HasTag("Creature")||e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            switch(bp)
            {
                case "Grass":case "StoneFloor":case "RoadStone":return e.HasTag("Terrain");
                case "GantryTimberWall":return e.HasTag("Wall")&&e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "TentWall":return e.HasTag("Wall")&&e.GetPart<MaterialPart>()?.MaterialID=="Cloth"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "Bush":return e.HasTag("Vegetation")&&e.GetPart<MaterialPart>()?.MaterialID=="Plant"&&e.HasPart<ThermalPart>()&&Healthy(e);
                case "Rock":return e.GetPart<MaterialPart>()?.MaterialID=="Stone"&&e.HasPart<ThermalPart>();
                case "Crate":case "GantryExchangeCounter":return e.GetPart<ContainerPart>()?.MaxItems==8&&e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&Healthy(e);
                case "GantryRegistryDesk":case "GantryWayboard":return e.GetPart<MaterialPart>()?.MaterialID=="Wood"&&Healthy(e)&&!e.HasPart<ContainerPart>();
                case "Bed":return e.HasPart<BedPart>();case "Chair":return e.HasPart<ChairPart>();
                case "GuestClothPole":return e.HasTag("Furniture")&&!e.HasPart<DestructiblePart>()&&!e.HasPart<ConversationPart>();default:return false;
            }
        }
        private static bool Healthy(Entity e){var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&!d.Gone&&!d.Indestructible;}
        private static string Glyph(string bp)
        {
            switch(bp){case "Grass":case "StoneFloor":return ".";case "RoadStone":case "Bed":case "GantryRegistryDesk":case "GantryExchangeCounter":return "=";
                case "GantryTimberWall":case "TentWall":return "#";case "Chair":return "h";case "Crate":return "0";case "Bush":return ";";case "Rock":return "o";
                case "GantryWayboard":case "GuestClothPole":return "|";case "TentRightHost":case "GantryRegistrar":return "@";default:return null;}
        }
        public bool TryGetServiceCell(string blueprint,out int x,out int y)
        {if(Plan!=null)return Plan.TryGetServiceCell(blueprint,out x,out y);x=y=-1;return false;}
        private bool Reject(Zone z,string reason)
        {Diag.Record("worldgen","GantryCompositionRejected",payload:new{zoneId=z?.ZoneID,seed,reason});return false;}
    }

    /// <summary>Place the four current native crossroads owners once. All four
    /// validate before any of them becomes visible in the realized zone.</summary>
    public sealed class GantryProfileBuilder:IZoneBuilder
    {
        public string Name=>"GantryProfile";public int Priority=>3860;
        private readonly GantryCompositionBuilder terrain;
        public GantryProfileBuilder(GantryCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone)||terrain.ProfileRealized)return Reject(zone,"wrong-or-replayed-owner");
            var staged=new List<(Entity e,int x,int y)>();
            foreach(var p in terrain.Plan.Profile)
            {
                var c=zone.GetCell(p.X,p.Y);if(c==null||c.BlocksMovement())return Reject(zone,"blocked-profile-cell");
                foreach(var owner in c.Objects)if(owner.HasPart<StairsUpPart>()||owner.HasPart<StairsDownPart>()||owner.HasPart<LiquidPoolPart>()||owner.BlueprintName==p.Blueprint)return Reject(zone,"occupied-profile-cell");
                var e=GantryCompositionBuilder.Create(factory,p.Blueprint);if(!GantryCompositionBuilder.Valid(e,p.Blueprint))return Reject(zone,"invalid-profile:"+p.Blueprint);
                e.Properties["SettlementId"]=zone.ZoneID;staged.Add((e,p.X,p.Y));
            }
            var added=new List<Entity>();foreach(var p in staged)
            {if(!zone.AddEntity(p.e,p.x,p.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"profile-placement-refused");}added.Add(p.e);}
            terrain.ProfileRealized=true;Diag.Record("worldgen","GantryProfilePlaced",payload:new{zoneId=zone.ZoneID,owners=added.Count});return true;
        }
        private static bool Reject(Zone z,string reason)
        {Diag.Record("worldgen","GantryProfileRejected",payload:new{zoneId=z?.ZoneID,reason});return false;}
    }

    /// <summary>Protect actual stairs and their dry standing neighbors after the
    /// native roll, before ordinary population. No carving or owner recreation.</summary>
    public sealed class GantryArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"GantryArrivalReservation";public int Priority=>3870;
        private readonly GantryCompositionBuilder terrain;
        public GantryArrivalReservationBuilder(GantryCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone))
            {Diag.Record("worldgen","GantryArrivalsRejected",payload:new{zoneId=zone?.ZoneID,reason="wrong-owner-or-dependency"});return false;}
            int stairs=0;foreach(var e in zone.GetAllEntities())
            {
                if(!e.HasPart<StairsDownPart>()&&!e.HasPart<StairsUpPart>())continue;var c=zone.GetEntityCell(e);if(c==null)continue;stairs++;
                zone.GenReservedCells.Add((c.X,c.Y));for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {int x=c.X+dx,y=c.Y+dy;if(zone.InBounds(x,y)&&!zone.GetCell(x,y).BlocksMovement())zone.GenReservedCells.Add((x,y));}
            }
            Diag.Record("worldgen","GantryArrivalsReserved",payload:new{zoneId=zone.ZoneID,stairs});return true;
        }
    }
}
