using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>A cold-generation layout choice. The selector reads current
    /// column authority and a stable private hash; it creates no entities and
    /// never consumes the zone, population, combat or loot RNG streams.</summary>
    public sealed class UndergroundLayoutPlan
    {
        public const int RoomedChancePercent = 25;
        public readonly bool IsRoomed;
        public readonly int Depth;
        public readonly BiomeType SurfaceBiome;
        public readonly bool IsOrdinaryColumn;
        public readonly string ZoneID, WallBlueprint, FloorBlueprint, Reason;
        private UndergroundLayoutPlan(string zoneID,int depth,bool roomed,string reason,BiomeType biome,bool ordinary)
        {
            ZoneID=zoneID;Depth=depth;IsRoomed=roomed;Reason=reason;SurfaceBiome=biome;IsOrdinaryColumn=ordinary;
            var palette=SolidEarthBuilder.GetMaterialsForDepth(depth);
            WallBlueprint=palette.wallBP;FloorBlueprint=palette.floorBP;
        }
        public static UndergroundLayoutPlan Select(int worldSeed,string zoneID,BiomeType biome,PointOfInterest poi,EntityFactory factory)
        {
            var (x,y,depth)=WorldMap.FromZoneID(zoneID);
            bool valid=WorldMapAuthoring.InBounds(x,y)&&depth>=0;
            bool authored=false;
            if(valid)
            {
                string surface=WorldMap.ToZoneID(x,y,0);
                authored=surface==MultiCellPilotRuntime.ZoneID;
                foreach(string reserved in OverworldZoneManager.AuthoredWildernessZoneIDs)
                    if(surface==reserved){authored=true;break;}
            }
            UndergroundLayoutPlan Result(string id,int z,bool rooms,string why)
                =>new UndergroundLayoutPlan(id,z,rooms,why,biome,valid&&depth>0&&poi==null&&!authored);
            if(!WorldMapAuthoring.InBounds(x,y)||depth<0)return Result(zoneID,depth,false,"invalid-zone");
            string canonical=WorldMap.ToZoneID(x,y,depth);
            if(depth<3)return Result(canonical,depth,false,"shallow");
            if(poi!=null||authored)return Result(canonical,depth,false,"authored-column");
            if(biome!=BiomeType.Spread&&biome!=BiomeType.Sodden&&biome!=BiomeType.Beating)
                return Result(canonical,depth,false,"preserved-biome");
            if(Sample(worldSeed,x,y,depth)%100>=RoomedChancePercent)
                return Result(canonical,depth,false,"natural-roll");
            var materials=SolidEarthBuilder.GetMaterialsForDepth(depth);
            // RuinsBuilder also uses these three existing decoration owners.
            // Reduced/modded content packs retain their natural strata recipe.
            if(factory==null||!factory.Blueprints.ContainsKey(materials.wallBP)||!factory.Blueprints.ContainsKey(materials.floorBP)
                ||!factory.Blueprints.ContainsKey("Rubble")||!factory.Blueprints.ContainsKey("Pillar")||!factory.Blueprints.ContainsKey("BrokenColumn"))
                return Result(canonical,depth,false,"room-content-unavailable");
            return Result(canonical,depth,true,"roomed-roll");
        }
        public IZoneBuilder CreateGeometryBuilder()
        {
            if(!IsRoomed)return new StrataBuilder(Depth,WallBlueprint,FloorBlueprint);
            return new RuinsBuilder
            {
                StoneWallBlueprint=WallBlueprint,StoneFloorBlueprint=FloorBlueprint,
                MinRooms=4,MaxRooms=7,MinRoomSize=4,MaxRoomSize=10
            };
        }
        public void RecordSelection()
        {
            if(Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen","UndergroundLayoutSelected",payload:new
                {zone=ZoneID,depth=Depth,layout=IsRoomed?"roomed":"natural",wall=WallBlueprint,floor=FloorBlueprint,reason=Reason});
        }
        private static uint Sample(int seed,int x,int y,int depth)
        {
            unchecked
            {
                uint h=(uint)seed^2166136261u;
                h=(h^(uint)x)*16777619u;h=(h^(uint)y)*16777619u;h=(h^(uint)depth)*16777619u;
                h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16);
            }
        }
    }
}
