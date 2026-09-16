using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Fresh-zone realization of Cinderhold's native stone rooms,
    /// working ground and forest shoulders. No rendering or saved-graph repair.</summary>
    public sealed class CinderholdCompositionBuilder:IZoneBuilder
    {
        public string Name=>"CinderholdComposition";
        public int Priority=>1000;
        public CinderholdCompositionPlan Plan {get;private set;}
        internal Zone RealizedZone {get;private set;}
        internal bool ProfileRealized;
        private readonly int seed;
        public CinderholdCompositionBuilder(int seed){this.seed=seed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null)return Reject(zone,"missing-dependency");
            if(!CinderholdCompositionPlan.IsSupportedZone(zone.ZoneID))return Reject(zone,"outside-cinderhold");
            if(zone.EntityCount!=0||(RealizedZone!=null&&!ReferenceEquals(RealizedZone,zone)))return Reject(zone,"nonempty-or-foreign-reuse");
            var p=CinderholdCompositionPlan.Create(zone.ZoneID,seed);
            var required=new HashSet<string>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {required.Add(p.GroundAt(x,y));if(p.ObjectAt(x,y)!=null)required.Add(p.ObjectAt(x,y));}
            foreach(var owner in p.Profile)required.Add(owner.Blueprint);
            foreach(var bp in required)if(!Valid(Create(factory,bp),bp))return Reject(zone,"invalid-required-native:"+bp);
            if(!StockDependenciesValid(factory))return Reject(zone,"invalid-profile-stock");

            var staged=new List<(Entity e,int x,int y)>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var ground=Create(factory,p.GroundAt(x,y));if(!Valid(ground,p.GroundAt(x,y)))return Reject(zone,"invalid-ground");staged.Add((ground,x,y));
                var bp=p.ObjectAt(x,y);if(bp==null)continue;
                var e=Create(factory,bp);if(!Valid(e,bp))return Reject(zone,"invalid-object:"+bp);staged.Add((e,x,y));
            }
            var added=new List<Entity>();
            foreach(var s in staged)
            {if(!zone.AddEntity(s.e,s.x,s.y)){foreach(var e in added)zone.RemoveEntity(e);return Reject(zone,"placement-refused");}added.Add(s.e);}
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {zone.GetCell(x,y).IsInterior=p.IsInterior(x,y);if(p.IsReserved(x,y))zone.GenReservedCells.Add((x,y));}
            Plan=p;RealizedZone=zone;ProfileRealized=false;
            Diag.Record("worldgen","CinderholdCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,rooms=p.Rooms.Count});return true;
        }
        internal static Entity Create(EntityFactory factory,string bp)
        {
            if(factory?.Blueprints==null||string.IsNullOrEmpty(bp)||!factory.Blueprints.ContainsKey(bp))return null;
            try{return factory.CreateEntity(bp);}catch(Exception){return null;}
        }
        internal static bool Valid(Entity e,string bp)
        {
            if(e==null||e.BlueprintName!=bp)return false;
            var physics=e.GetPart<PhysicsPart>();var render=e.GetPart<RenderPart>();
            if(physics==null||physics.Takeable||render==null||!render.Visible||!e.HasPart<ExaminablePart>())return false;
            string glyph;
            switch(bp)
            {
                case "Grass":case "StoneFloor":glyph=".";break;
                case "RoadStone":case "Bed":case "Chest":glyph="=";break;
                case "SandstoneWall":glyph="#";break;case "Tree":glyph="T";break;case "Bush":glyph=";";break;
                case "Chair":glyph="h";break;case "Crate":glyph="0";break;
                case "ConcordFactor":case "Weaponsmith":glyph="@";break;
                case "CinderholdNoticeBoard":glyph="I";break;case "Campfire":glyph="*";break;
                case "TinkersForge":case "SmithAnvil":glyph="n";break;default:return false;
            }
            if(render.RenderString!=glyph)return false;
            bool actor=bp=="ConcordFactor"||bp=="Weaponsmith";
            bool solid=actor||bp=="SandstoneWall"||bp=="Tree"||bp=="Crate"||bp=="Chest"||bp=="CinderholdNoticeBoard"||bp=="SmithAnvil";
            if(physics.Solid!=solid||(!solid&&e.HasTag("Solid")))return false;
            if(actor)
            {
                if(!e.HasTag("Creature")||!e.HasPart<BrainPart>()||e.GetStatValue("Hitpoints")<=0||!e.HasPart<InventoryPart>())return false;
                if(bp=="ConcordFactor")return e.GetTag("Faction")=="SaccharineConcord"&&e.GetPart<ConversationPart>()?.ConversationID=="ConcordFactor_1";
                var trader=e.GetPart<TraderPart>();
                return e.GetTag("Faction")=="Villagers"&&e.GetPart<ConversationPart>()?.ConversationID=="Shop_Weaponsmith"
                    &&trader!=null&&trader.StockTable=="WeaponsmithStock"&&trader.Drams>0&&e.HasTag("NoRandomStock");
            }
            if(e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(bp=="Grass"||bp=="StoneFloor"||bp=="RoadStone")return e.HasTag("Terrain");
            if(bp=="SandstoneWall")return Breakable(e)&&e.HasTag("Wall")&&e.GetPart<MaterialPart>()?.MaterialID=="Stone"&&e.GetPart<DestructiblePart>().WreckageBlueprint=="Rubble";
            if(bp=="Tree"||bp=="Bush")return Breakable(e)&&e.GetPart<MaterialPart>()?.MaterialID==(bp=="Tree"?"Wood":"Plant")&&e.HasPart<ThermalPart>();
            if(bp=="Chest"||bp=="Crate")return Breakable(e)&&e.HasPart<ContainerPart>()&&e.GetPart<MaterialPart>()?.MaterialID=="Wood";
            if(bp=="Bed")return e.HasPart<BedPart>();
            if(bp=="Chair")return e.HasPart<ChairPart>();
            if(bp=="TinkersForge")return e.HasPart<ForgePart>()&&!e.HasPart<LightSourcePart>()&&!e.HasPart<ThermalPart>();
            if(bp=="SmithAnvil")
            {
                var h=e.GetPart<HandlingPart>();return h!=null&&h.Weight==120&&h.MinLiftStrength==18&&!h.Carryable&&!h.Throwable&&physics.Weight==120&&!e.HasPart<ForgePart>();
            }
            if(bp=="Campfire")return e.HasPart<CampfirePart>()&&e.HasPart<FuelPart>()&&e.HasPart<LightSourcePart>()&&e.HasPart<ThermalPart>()&&e.GetPart<MaterialPart>()?.MaterialID=="Wood";
            return bp=="CinderholdNoticeBoard";
        }
        private static bool Breakable(Entity e)
        {var d=e.GetPart<DestructiblePart>();return d!=null&&d.HP>0&&d.MaxHP>0&&!d.Gone&&!d.Indestructible;}
        internal static bool StockDependenciesValid(EntityFactory factory)
        {
            // Native headless callers may intentionally omit the registry, as
            // with existing LandmarkBuilder. Do not mutate the global registry.
            if(!LootTableRegistry.IsInitialized)return true;
            foreach(var table in new[]{"CampGoodsT1","WeaponsmithStock"})
                if(!ValidateTable(table,factory,new HashSet<string>()))return false;
            return true;
        }
        private static bool ValidateTable(string table,EntityFactory factory,HashSet<string> stack)
        {
            var data=LootTableRegistry.Get(table);if(data?.Entries==null||data.Entries.Count==0||!stack.Add(table))return false;
            foreach(var entry in data.Entries)
            {
                if(entry==null){stack.Remove(table);return false;}
                if(!string.IsNullOrEmpty(entry.TableRef))
                {if(!ValidateTable(entry.TableRef,factory,stack)){stack.Remove(table);return false;}}
                else
                {var item=Create(factory,entry.Blueprint);if(item?.GetPart<RenderPart>()==null||item.GetPart<PhysicsPart>()?.Takeable!=true){stack.Remove(table);return false;}}
            }
            stack.Remove(table);return true;
        }
        private bool Reject(Zone zone,string reason)
        {
            // A refused foreign or repeated call cannot revoke the original
            // successful zone's still-pending profile/arrival capability.
            Diag.Record("worldgen","CinderholdCompositionRejected",payload:new{zoneId=zone?.ZoneID,seed,reason});return false;
        }
    }

    /// <summary>The original pruning-post owners, plus a native equipment shop
    /// and working forge, committed together at their semantic frontages.</summary>
    public sealed class CinderholdProfileBuilder:IZoneBuilder
    {
        public string Name=>"CinderholdProfile";public int Priority=>3860;
        private readonly CinderholdCompositionBuilder terrain;
        public CinderholdProfileBuilder(CinderholdCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone)||terrain.ProfileRealized)return Reject(zone,"wrong-or-replayed-owner");
            if(!CinderholdCompositionBuilder.StockDependenciesValid(factory))return Reject(zone,"invalid-profile-stock");
            var staged=new List<(Entity e,int x,int y)>();
            foreach(var p in terrain.Plan.Profile)
            {
                var c=zone.GetCell(p.X,p.Y);if(c==null||c.BlocksMovement())return Reject(zone,"blocked-profile-cell");
                foreach(var current in c.Objects)
                    if(current.HasPart<StairsDownPart>()||current.HasPart<StairsUpPart>()||current.HasPart<LiquidPoolPart>()||current.BlueprintName==p.Blueprint)return Reject(zone,"occupied-profile-cell");
                var owner=CinderholdCompositionBuilder.Create(factory,p.Blueprint);
                if(!CinderholdCompositionBuilder.Valid(owner,p.Blueprint))return Reject(zone,"invalid-profile:"+p.Blueprint);
                owner.Properties["SettlementId"]=zone.ZoneID;
                if(LootTableRegistry.IsInitialized)
                {
                    if(p.Blueprint=="Chest")LootStocker.StockContainer(owner,"CampGoodsT1",factory,rng);
                    else if(p.Blueprint=="Weaponsmith")
                    {
                        // TraderPart may already have stocked this actual owner
                        // during ObjectCreated. Never roll its opening shelf twice.
                        var inv=owner.GetPart<InventoryPart>();
                        if(inv.Objects.Count==0)foreach(var bp in LootTableRegistry.Roll("WeaponsmithStock",rng))
                        {var item=CinderholdCompositionBuilder.Create(factory,bp);if(item==null||!inv.AddObject(item))return Reject(zone,"shop-stock-refused");}
                    }
                }
                staged.Add((owner,p.X,p.Y));
            }
            var added=new List<Entity>();
            foreach(var s in staged)
            {if(!zone.AddEntity(s.e,s.x,s.y)){foreach(var owner in added)zone.RemoveEntity(owner);return Reject(zone,"profile-placement-refused");}added.Add(s.e);}
            terrain.ProfileRealized=true;Diag.Record("worldgen","CinderholdProfilePlaced",payload:new{zoneId=zone.ZoneID,owners=added.Count});return true;
        }
        private static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","CinderholdProfileRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }

    /// <summary>Reserves actual native cave arrivals before villagers and loose
    /// containers are placed. Never fabricates stairs, clears owners or carves.</summary>
    public sealed class CinderholdArrivalReservationBuilder:IZoneBuilder
    {
        public string Name=>"CinderholdArrivalReservation";public int Priority=>3870;
        private readonly CinderholdCompositionBuilder terrain;
        public CinderholdArrivalReservationBuilder(CinderholdCompositionBuilder terrain){this.terrain=terrain;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||factory?.Blueprints==null||rng==null||terrain?.Plan==null||!ReferenceEquals(terrain.RealizedZone,zone))
            {Diag.Record("worldgen","CinderholdArrivalsRejected",payload:new{zoneId=zone?.ZoneID,reason="wrong-or-unrealized-owner"});return false;}
            int stairs=0;
            foreach(var e in zone.GetAllEntities())
            {
                if(!e.HasPart<StairsUpPart>()&&!e.HasPart<StairsDownPart>())continue;
                var c=zone.GetEntityCell(e);if(c==null)continue;stairs++;zone.GenReservedCells.Add((c.X,c.Y));
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {int x=c.X+dx,y=c.Y+dy;if(zone.InBounds(x,y)&&!zone.GetCell(x,y).BlocksMovement())zone.GenReservedCells.Add((x,y));}
            }
            Diag.Record("worldgen","CinderholdArrivalsReserved",payload:new{zoneId=zone.ZoneID,stairs});return true;
        }
    }
}
