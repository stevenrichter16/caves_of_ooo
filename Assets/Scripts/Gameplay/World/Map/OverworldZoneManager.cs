using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Extends ZoneManager to route overworld zone IDs through the WorldMap
    /// for biome-specific generation pipelines. POI zones (villages, lairs)
    /// get specialized pipelines.
    /// </summary>
    public class OverworldZoneManager : ZoneManager
    {
        public WorldMap WorldMap { get; private set; }

        public OverworldZoneManager(EntityFactory factory, int worldSeed = 0)
            : base(factory, worldSeed)
        {
            WorldMap = WorldGenerator.Generate(WorldSeed);
        }

        protected override ZoneGenerationPipeline GetPipelineForZone(string zoneID)
        {
            if (!WorldMap.IsOverworldZoneID(zoneID))
                return base.GetPipelineForZone(zoneID);

            var (wx, wy, wz) = WorldMap.FromZoneID(zoneID);
            if (!WorldMap.InBounds(wx, wy))
                return base.GetPipelineForZone(zoneID);

            // Underground zones use a dedicated pipeline
            if (wz > 0)
                return CreateUndergroundPipeline(wz);

            BiomeType biome = WorldMap.GetBiome(wx, wy);

            // Check for POI -- villages and lairs get special pipelines
            var poi = WorldMap.GetPOI(wx, wy);
            if (poi != null)
            {
                switch (poi.Type)
                {
                    case POIType.Village:
                        return CreateVillagePipeline(biome, poi);
                    case POIType.Lair:
                        return CreateLairPipeline(biome, poi);
                    case POIType.MerchantCamp:
                        // Merchant camps use normal biome pipeline + trade stock
                        break;
                }
            }

            // Determine tier from distance to center
            int tier = GetTierForCoords(wx, wy);

            switch (biome)
            {
                case BiomeType.Desert:
                    return CreateDesertPipeline(tier);
                case BiomeType.Jungle:
                    return CreateJunglePipeline(tier);
                case BiomeType.Ruins:
                    return CreateRuinsPipeline(tier);
                case BiomeType.Cave:
                default:
                    return CreateCavePipeline(tier);
            }
        }

        private int GetTierForCoords(int wx, int wy)
        {
            int centerX = WorldMap.Width / 2;
            int centerY = WorldMap.Height / 2;
            int dist = System.Math.Abs(wx - centerX) + System.Math.Abs(wy - centerY);
            if (dist <= 4) return 1;
            if (dist <= 8) return 2;
            return 3;
        }

        private ZoneGenerationPipeline CreateCavePipeline(int tier = 1)
        {
            var pipeline = ZoneGenerationPipeline.CreateCavePipeline(PopulationTable.GetBiomeTable(BiomeType.Cave, tier));
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateUndergroundPipeline(int depth)
        {
            var (wallBP, floorBP) = SolidEarthBuilder.GetMaterialsForDepth(depth);

            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new SolidEarthBuilder(wallBP));
            pipeline.AddBuilder(new StrataBuilder(depth, wallBP, floorBP));
            pipeline.AddBuilder(new ConnectivityBuilder { FloorBlueprint = floorBP });
            pipeline.AddBuilder(new StairsUpBuilder(this));
            pipeline.AddBuilder(new StairsDownBuilder(this));
            pipeline.AddBuilder(new StairConnectorBuilder(floorBP));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.UndergroundTier(depth)));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateDesertPipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new DesertBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Desert, tier)));
            pipeline.AddBuilder(new TradeStockBuilder());
            return pipeline;
        }

        private ZoneGenerationPipeline CreateJunglePipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new JungleBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Jungle, tier)));
            pipeline.AddBuilder(new TradeStockBuilder());
            return pipeline;
        }

        private ZoneGenerationPipeline CreateRuinsPipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new RuinsBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Ruins, tier)));
            pipeline.AddBuilder(new TradeStockBuilder());
            return pipeline;
        }

        private ZoneGenerationPipeline CreateVillagePipeline(BiomeType biome, PointOfInterest poi)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new VillageBuilder(biome, poi));
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new VillagePopulationBuilder(poi));
            pipeline.AddBuilder(new TradeStockBuilder());
            return pipeline;
        }

        private ZoneGenerationPipeline CreateLairPipeline(BiomeType biome, PointOfInterest poi)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new LairBuilder(biome, poi));
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new LairPopulationBuilder(biome, poi));
            return pipeline;
        }

        /// <summary>
        /// Get the population table for a given biome type and tier.
        /// </summary>
        public static PopulationTable GetPopulationForBiome(BiomeType biome, int tier = 1)
        {
            return PopulationTable.GetBiomeTable(biome, tier);
        }
    }
}
