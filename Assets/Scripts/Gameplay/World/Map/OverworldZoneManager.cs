using CavesOfOoo.Data;
using UnityEngine;

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
        public SettlementManager SettlementManager { get; private set; }
        private System.Func<int> _turnProvider;

        public OverworldZoneManager(EntityFactory factory, int worldSeed = 0)
            : base(factory, worldSeed)
        {
            WorldMap = WorldGenerator.Generate(WorldSeed);
            SettlementManager = new SettlementManager(
                currentTurnProvider: null,
                poiResolver: ResolvePointOfInterestForSettlement);
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

            // Check for POI -- villages, lairs, and river chunks get special pipelines
            var poi = WorldMap.GetPOI(wx, wy);
            if (poi != null)
            {
                switch (poi.Type)
                {
                    case POIType.Village:
                        return CreateVillagePipeline(biome, poi, zoneID);
                    case POIType.Lair:
                        return CreateLairPipeline(biome, poi);
                    case POIType.MerchantCamp:
                        // Merchant camps use normal biome pipeline + trade stock
                        break;
                    case POIType.RiverChunk:
                        return CreateRiverChunkPipeline();
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
            pipeline.AddBuilder(new StartingNeighborhoodBuilder());
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
            pipeline.AddBuilder(new StartingNeighborhoodBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Desert, tier)));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateJunglePipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new JungleBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new StartingNeighborhoodBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Jungle, tier)));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateRuinsPipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new RuinsBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new StartingNeighborhoodBuilder());
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Ruins, tier)));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateVillagePipeline(BiomeType biome, PointOfInterest poi, string zoneID)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new VillageBuilder(biome, poi, SettlementManager));
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            // Narrow HTML-style water channel running west → east along the
            // BOTTOM of the village. halfWidth=2.0 gives a ~4-cell-tall
            // channel; crossCenterOffset=+8 places the centerline around
            // y=20 (zone midpoint 12.5 + 8), with the river occupying roughly
            // rows 17–23. skipBanks=true keeps cells outside the channel as
            // normal village ground. clearSolidEntities=true bulldozes any
            // walls / wells / ovens / fences in the river's path — the
            // river takes priority over village layout. Priority 3850
            // runs before VillagePopulationBuilder (4000) so NPCs don't
            // spawn in water (though they may still spawn on cells where
            // we just removed structures — acceptable collateral).
            pipeline.AddBuilder(new RiverChunkBuilder(
                halfWidthBase: 2.0f,
                skipBanks: true,
                direction: RiverFlowDirection.East,
                crossCenterOffset: 8,
                clearSolidEntities: true));
            pipeline.AddBuilder(new VillagePopulationBuilder(poi, SettlementManager));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));

            // Seed a House Drama into this village if any dramas are loaded.
            // Uses WorldSeed XOR'd with the zone string ID hash (matching ZoneManager's
            // own RNG seed formula) to deterministically assign a drama per village.
            var dramaIds = HouseDramaRuntime.GetAllDramaIds();
            if (dramaIds.Count > 0)
            {
                int zoneSeed = WorldSeed ^ zoneID.GetHashCode();
                int pick = (zoneSeed & int.MaxValue) % dramaIds.Count;
                pipeline.AddBuilder(new HouseDramaZoneBuilder(dramaIds[pick]));
            }

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
        /// Pipeline for POIType.RiverChunk zones — the entire 80×25 grid
        /// is the river.ascii demo. No village buildings, no NPCs, no
        /// connectivity pass (every cell is water or bank, both passable).
        /// Just the faithful-port builder.
        /// </summary>
        private ZoneGenerationPipeline CreateRiverChunkPipeline()
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new RiverChunkBuilder());
            return pipeline;
        }

        /// <summary>
        /// Get the population table for a given biome type and tier.
        /// </summary>
        public static PopulationTable GetPopulationForBiome(BiomeType biome, int tier = 1)
        {
            return PopulationTable.GetBiomeTable(biome, tier);
        }

        public void SetTurnProvider(System.Func<int> turnProvider)
        {
            _turnProvider = turnProvider;
            SettlementManager.SetCurrentTurnProvider(turnProvider);
        }

        protected override void OnZoneGenerated(Zone zone, string zoneID)
        {
            if (!WorldMap.IsOverworldZoneID(zoneID))
                return;

            var (wx, wy, wz) = WorldMap.FromZoneID(zoneID);

            if (wz > 0)
            {
                // Underground: deeper = cooler blue tint
                float depth = Mathf.Min(wz * 0.03f, 0.15f);
                zone.AmbientTint = new Color(0.85f - depth, 0.9f - depth * 0.5f, 1f);

                // Mark all cells as interior. Extracted so a future zone-
                // hydration path (save/load) can call it too without
                // re-running the generator pipeline.
                MarkDungeonInterior(zone);
                return;
            }

            if (!WorldMap.InBounds(wx, wy))
                return;

            zone.AmbientTint = GetBiomeTint(WorldMap.GetBiome(wx, wy));
        }

        /// <summary>
        /// Mark every cell in a dungeon (wz &gt; 0) zone as IsInterior=true.
        /// Mirrors Qud's <c>Zone.IsInside()</c> returning true for
        /// <c>Z &gt; 10</c>. We tag walls AND floors here — distinguishing
        /// cheaply without walking entity contents isn't worth the cost;
        /// any "is cell under a roof" consumer should still check
        /// <c>IsPassable</c> for walk-to purposes.
        /// </summary>
        private static void MarkDungeonInterior(Zone zone)
        {
            if (zone == null) return;
            for (int cx = 0; cx < Zone.Width; cx++)
            {
                for (int cy = 0; cy < Zone.Height; cy++)
                {
                    var cell = zone.GetCell(cx, cy);
                    if (cell != null) cell.IsInterior = true;
                }
            }
        }

        private static Color GetBiomeTint(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave:    return new Color(0.85f, 0.9f, 1.0f);
                case BiomeType.Desert:  return new Color(1.0f, 0.93f, 0.8f);
                case BiomeType.Jungle:  return new Color(0.85f, 1.0f, 0.85f);
                case BiomeType.Ruins:   return new Color(0.9f, 0.85f, 0.95f);
                default:                return Color.white;
            }
        }

        protected override void PrepareZoneForAccess(string zoneID)
        {
            if (!WorldMap.IsOverworldZoneID(zoneID))
                return;

            var (wx, wy, wz) = WorldMap.FromZoneID(zoneID);
            if (!WorldMap.InBounds(wx, wy) || wz != 0)
                return;

            var poi = WorldMap.GetPOI(wx, wy);
            if (poi == null || poi.Type != POIType.Village)
                return;

            SettlementManager.GetOrCreateSettlement(zoneID, poi);
            bool changed = SettlementManager.AdvanceSettlement(zoneID, GetCurrentTurn());
            if (changed && (ActiveZone == null || ActiveZone.ZoneID != zoneID))
                UnloadZone(zoneID);
        }

        private PointOfInterest ResolvePointOfInterestForSettlement(string settlementId)
        {
            if (!WorldMap.IsOverworldZoneID(settlementId))
                return null;

            var (x, y, z) = WorldMap.FromZoneID(settlementId);
            if (z != 0 || !WorldMap.InBounds(x, y))
                return null;

            return WorldMap.GetPOI(x, y);
        }

        private int GetCurrentTurn()
        {
            return _turnProvider != null ? _turnProvider() : 0;
        }
    }
}
