using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Extends ZoneManager to route overworld zone IDs through the WorldMap
    /// for biome-specific generation pipelines.
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

            var (wx, wy) = WorldMap.FromZoneID(zoneID);
            if (!WorldMap.InBounds(wx, wy))
                return base.GetPipelineForZone(zoneID);

            BiomeType biome = WorldMap.GetBiome(wx, wy);

            switch (biome)
            {
                case BiomeType.Desert:
                    return CreateDesertPipeline();
                case BiomeType.Jungle:
                    return CreateJunglePipeline();
                case BiomeType.Ruins:
                    return CreateRuinsPipeline();
                case BiomeType.Cave:
                default:
                    return CreateCavePipeline();
            }
        }

        private ZoneGenerationPipeline CreateCavePipeline()
        {
            return ZoneGenerationPipeline.CreateCavePipeline(PopulationTable.CaveTier1());
        }

        private ZoneGenerationPipeline CreateDesertPipeline()
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new DesertBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.DesertTier1()));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateJunglePipeline()
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new JungleBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.JungleTier1()));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateRuinsPipeline()
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new RuinsBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.RuinsTier1()));
            return pipeline;
        }

        /// <summary>
        /// Get the population table for a given biome type.
        /// </summary>
        public static PopulationTable GetPopulationForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return PopulationTable.DesertTier1();
                case BiomeType.Jungle: return PopulationTable.JungleTier1();
                case BiomeType.Ruins: return PopulationTable.RuinsTier1();
                case BiomeType.Cave:
                default: return PopulationTable.CaveTier1();
            }
        }
    }
}
