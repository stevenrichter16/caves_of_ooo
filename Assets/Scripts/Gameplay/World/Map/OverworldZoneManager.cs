using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
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
            // World-map zone: a singular Zone the player physically
            // inhabits when they ascend. ZoneID has no dots — see
            // WorldMap.WorldMapZoneID. Mirrors Qud's IsWorldMap pattern.
            if (WorldMap.IsWorldMapZoneID(zoneID))
                return CreateWorldMapPipeline();

            if (!WorldMap.IsOverworldZoneID(zoneID))
                return base.GetPipelineForZone(zoneID);

            var (wx, wy, wz) = WorldMap.FromZoneID(zoneID);
            if (!WorldMap.InBounds(wx, wy))
                return base.GetPipelineForZone(zoneID);

            BiomeType biome = WorldMap.GetBiome(wx, wy);
            var poi = WorldMap.GetPOI(wx, wy);

            // W5.1 (Docs/FELLING-W5-PLAN.md sweep row 1) — a sinkhole is
            // the ONE POI type that means something below z=0, so it is
            // resolved BEFORE the generic depth branch. Every other POI
            // describes a surface chunk only; before this, `wz > 0`
            // returned the anonymous cave pipeline without ever reading
            // the POI, and a sinkhole's own descent and floor generated
            // as generic caves.
            if (poi != null && poi.Type == POIType.Sinkhole)
                return CreateSinkholePipeline(biome, poi, wx, wy, wz);

            // Underground zones use a dedicated pipeline
            if (wz > 0)
                return CreateUndergroundPipeline(wz);

            // Check for POI -- villages, lairs, and river chunks get special pipelines
            if (poi != null)
            {
                switch (poi.Type)
                {
                    case POIType.Village:
                        return CreateVillagePipeline(biome, poi, zoneID);
                    case POIType.Lair:
                        return CreateLairPipeline(biome, poi);
                    case POIType.MerchantCamp:
                        // BIOME-OVERHAUL B1: the $ on the map is now a
                        // real camp — biome pipeline + a GUARANTEED camp
                        // stamp (previously this fell through to plain
                        // wilderness and the camp POI meant nothing).
                        return CreateMerchantCampPipeline(biome, GetTierForCoords(wx, wy));
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

                // The Felling world. W0.6 gives every canon biome a
                // PLAYABLE zone by borrowing its nearest legacy terrain
                // generator; each biome's own formations land in its own
                // phase (W1 Spread, W2 Beating, W3 Sodden, W4 Grovelands,
                // W6 Stump, W7 Overwrit). The point of W0 is that the
                // authored map ships walkable, not that it ships
                // finished.
                case BiomeType.Spread:
                    return CreateSpreadPipeline(tier);
                case BiomeType.Sodden:
                    return CreateSoddenPipeline(tier);
                case BiomeType.Beating:
                    if (zoneID == TenthFireZoneID)
                        return CreateTenthFirePipeline(tier);
                    return CreateBeatingPipeline(tier, zoneID);
                case BiomeType.Grovelands:
                {
                    // W4.4 SM-D (R8): the Bloom-front registers HERE —
                    // the wilderness arm, reached only when poi == null —
                    // and never inside CreateGrovelandsPipeline, which
                    // the MerchantCamp pipeline reuses. Villages (all
                    // authored Places incl. Cinderhold), lairs, river
                    // chunks, and camps are excluded structurally.
                    // W4.5 — design gate 4: the doll's grove is built
                    // BARE, the tenth fire's lesson applied (W2.8: "the
                    // mystery needs emptiness" — its first look pass put
                    // the fire beside a hermit's hut). No ambient
                    // stamps, no containers, no Bloom-front; the grove
                    // formation forced, the doll, and Choir-country
                    // fauna, which belongs.
                    if (zoneID == WovenDollZoneID)
                        return CreateWovenDollPipeline(tier);
                    var grove = CreateGrovelandsPipeline(tier);
                    grove.AddBuilder(new BloomFrontBuilder());
                    return grove;
                }
                case BiomeType.Overwrit:
                    return CreateOverwritPipeline(tier);
                case BiomeType.Stump:
                    return CreateStumpPipeline(tier);

                case BiomeType.Cave:
                default:
                    return CreateCavePipeline(tier);
            }
        }

        /// <summary>
        /// BIOME-OVERHAUL B1 — a MerchantCamp POI zone is its biome's
        /// normal wilderness PLUS a guaranteed camp stamp (second
        /// LandmarkBuilder with a catalog override; the ambient one
        /// still runs from the biome pipeline). TradeStockBuilder in
        /// the biome pipeline auto-stocks the camp's Villagers-faction
        /// NPCs, so the Merchant arrives with goods and a wallet.
        /// </summary>
        private ZoneGenerationPipeline CreateMerchantCampPipeline(BiomeType biome, int tier)
        {
            ZoneGenerationPipeline pipeline;
            switch (biome)
            {
                case BiomeType.Desert: pipeline = CreateDesertPipeline(tier); break;
                case BiomeType.Jungle: pipeline = CreateJunglePipeline(tier); break;
                case BiomeType.Ruins: pipeline = CreateRuinsPipeline(tier); break;
                case BiomeType.Spread: pipeline = CreateSpreadPipeline(tier); break;
                case BiomeType.Sodden: pipeline = CreateSoddenPipeline(tier); break;
                case BiomeType.Beating: pipeline = CreateBeatingPipeline(tier); break;
                case BiomeType.Grovelands: pipeline = CreateGrovelandsPipeline(tier); break;
                case BiomeType.Overwrit: pipeline = CreateOverwritPipeline(tier); break;
                case BiomeType.Stump: pipeline = CreateStumpPipeline(tier); break;
                case BiomeType.Cave:
                default: pipeline = CreateCavePipeline(tier); break;
            }
            // Priority 3790: the guaranteed camp claims open space
            // BEFORE the ambient wilderness stamps (3800) — see the
            // LandmarkBuilder priority docstring.
            pipeline.AddBuilder(new LandmarkBuilder(biome, tier,
                new[] { StampCatalog.MerchantCamp(biome) }, priority: 3790));
            return pipeline;
        }

        /// <summary>
        /// The zone's Strangeness Tier, read from the authored table
        /// (Felling W0.6). This used to be a Manhattan-distance formula
        /// duplicated here AND in WorldGenerator — and tier was computed
        /// twice per zone besides (stamped into the POI at generation,
        /// recomputed at pipeline time), so the two could drift. One
        /// table now answers both.
        ///
        /// <para>Canon's rule, kept: tier is authored, not radial. Slip
        /// is a Tier-3 wound a day's walk from Tier-1 farmland, and that
        /// is the point (Lore/History/02_Geography.md).</para>
        /// </summary>
        private int GetTierForCoords(int wx, int wy)
            => WorldMapAuthoring.TierAt(wx, wy);

        private ZoneGenerationPipeline CreateCavePipeline(int tier = 1)
        {
            var pipeline = ZoneGenerationPipeline.CreateCavePipeline(PopulationTable.GetBiomeTable(BiomeType.Cave, tier));
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Cave, tier));
            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Cave));
            // LOOT OVERHAUL SM6 — every zone type gets containers now.
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Cave, tier,
                ContainerPlacementService.ZoneKind.Wilderness));
            return pipeline;
        }

        /// <summary>
        /// Pipeline for the singular world-map zone the player ascends
        /// into via <c>WorldMapTraversal.Ascend</c>. The builder embeds
        /// the 20×20 logical worldmap inside the 80×25 zone with
        /// impassable wall borders. Mirrors Qud's pattern (the world
        /// map IS a Zone, not a modal UI).
        /// </summary>
        private ZoneGenerationPipeline CreateWorldMapPipeline()
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new WorldMapZoneBuilder(WorldMap));
            return pipeline;
        }

        /// <summary>W5.1 — the sinkhole stack. One POI, three bespoke
        /// levels: the Mouth you find (z=0), the Descent you survive
        /// (z=1), the Floor that is a different place per sinkhole
        /// (z=2+, generic for now — W5.3 gives it archetypes).
        ///
        /// <para><b>R5 decided:</b> a sinkhole floor's tier is the
        /// SURFACE tier + 1 (canon's rule, FELLING-WORLD-DESIGN §3),
        /// not the generic <c>depth/3 + 1</c> the anonymous cave stack
        /// uses. The two formulas would have disagreed; this is the one
        /// that holds for holes.</para></summary>
        private ZoneGenerationPipeline CreateSinkholePipeline(
            BiomeType biome, PointOfInterest poi, int wx, int wy, int wz)
        {
            int surfaceTier = GetTierForCoords(wx, wy);
            if (wz == 0)
            {
                // The mouth is its own biome's wilderness, plus the hole.
                var mouth = CreateSurfaceWildernessFor(biome, surfaceTier);
                // Cold-eye H10: the biome's wilderness carries
                // CaveEntranceBuilder, which fires on a 50% roll — so
                // half of all worlds gave a sinkhole mouth a SECOND,
                // random staircase into the same descent. A hole in the
                // world is the way down; it does not need a cave door
                // beside it.
                mouth.RemoveBuilders<CaveEntranceBuilder>();
                mouth.AddBuilder(new SinkholeMouthBuilder(this));
                return mouth;
            }

            // Cold-eye H9: the Floor is ONE level. Canon puts
            // "Z=3+ CATACOMBS / ROOTWAYS — below the floors, where
            // placed" — different content, not a second copy. Routing
            // every level below the descent through the archetype
            // branch gave a Drowned Sima a Drowned Sima under it,
            // forever, because the floor also gets a StairsDownBuilder.
            if (wz > 2)
                return CreateUndergroundPipeline(wz);

            int floorTier = System.Math.Min(surfaceTier + 1, 8);
            var pipeline = new ZoneGenerationPipeline();
            var (wallBP, floorBP) = SolidEarthBuilder.GetMaterialsForDepth(wz);
            pipeline.AddBuilder(new SolidEarthBuilder(wallBP));
            pipeline.AddBuilder(new StrataBuilder(wz, wallBP, floorBP));
            pipeline.AddBuilder(new ConnectivityBuilder { FloorBlueprint = floorBP });
            pipeline.AddBuilder(new StairsUpBuilder(this));
            pipeline.AddBuilder(new StairsDownBuilder(this));
            pipeline.AddBuilder(new StairConnectorBuilder(floorBP));

            if (wz == 1)
            {
                pipeline.AddBuilder(new SinkholeDescentBuilder(this));
            }
            else
            {
                // W5.3 — the floor is a different place per sinkhole.
                // The archetype is a pure function of the hole's name
                // (R3), so it is fixed forever without a saved field.
                switch (SinkholeArchetypes.For(poi.Name))
                {
                    case SinkholeArchetype.DrownedSima:
                        pipeline.AddBuilder(new DrownedSimaBuilder());
                        break;
                    case SinkholeArchetype.StrandedSettlement:
                        pipeline.AddBuilder(new StrandedSettlementBuilder(this));
                        break;
                    case SinkholeArchetype.ChoirCathedral:
                        pipeline.AddBuilder(new ChoirCathedralBuilder());
                        break;
                }
            }

            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Cave, underground: true));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.UndergroundTier(wz)));
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Cave, floorTier,
                ContainerPlacementService.ZoneKind.Underground));
            if (Diag.IsChannelEnabled("worldmap"))
                Diag.Record("worldmap", "SinkholeRouted", null, null,
                    new { sinkhole = poi.Name, x = wx, y = wy, z = wz,
                          level = wz == 1 ? "Descent" : "Floor", floorTier });
            return pipeline;
        }

        /// <summary>The plain wilderness pipeline for a biome, without
        /// any POI treatment — the base a sinkhole mouth builds its hole
        /// into.</summary>
        private ZoneGenerationPipeline CreateSurfaceWildernessFor(BiomeType biome, int tier)
        {
            switch (biome)
            {
                case BiomeType.Desert:     return CreateDesertPipeline(tier);
                case BiomeType.Jungle:     return CreateJunglePipeline(tier);
                case BiomeType.Ruins:      return CreateRuinsPipeline(tier);
                case BiomeType.Spread:     return CreateSpreadPipeline(tier);
                case BiomeType.Sodden:     return CreateSoddenPipeline(tier);
                case BiomeType.Beating:    return CreateBeatingPipeline(tier);
                case BiomeType.Grovelands: return CreateGrovelandsPipeline(tier);
                case BiomeType.Overwrit:   return CreateOverwritPipeline(tier);
                case BiomeType.Stump:      return CreateStumpPipeline(tier);
                default:                   return CreateCavePipeline(tier);
            }
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
            // BIOME-OVERHAUL G: underground landmarks by depth band
            // (galleries, the Curation's rest stop, reliquaries).
            // Tier formula mirrors ZoneManager.GetZoneTier's depth
            // band: depth/3 + 1, capped at 8.
            int undergroundTier = System.Math.Min(depth / 3 + 1, 8);
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Cave,
                undergroundTier, StampCatalog.Underground(depth)));
            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Cave, underground: true));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.UndergroundTier(depth)));
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Cave, undergroundTier,
                ContainerPlacementService.ZoneKind.Underground));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateDesertPipeline(int tier = 1)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new DesertBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Desert, tier));
            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Desert));
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Desert, tier,
                ContainerPlacementService.ZoneKind.Wilderness));
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
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Jungle, tier));
            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Jungle));
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Jungle, tier,
                ContainerPlacementService.ZoneKind.Wilderness));
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
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Ruins, tier));
            pipeline.AddBuilder(new HazardTerrainBuilder(BiomeType.Ruins));
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Ruins, tier,
                ContainerPlacementService.ZoneKind.Wilderness));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Ruins, tier)));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));
            return pipeline;
        }

        // ════════════════════════════════════════════════════════
        // The Felling biomes — W0.6 placeholders
        // ════════════════════════════════════════════════════════
        //
        // Each borrows a shipped terrain generator, tuned toward the
        // canon description, so the authored map is WALKABLE today. The
        // formations that make each biome itself (a hedgerow lane, a
        // salt pan, a duckboard causeway, a grove colonnade, a bleed)
        // belong to the biome's own phase.

        /// <summary>
        /// The Spread — recovered river country, and the first biome with
        /// real FORMATIONS (W1): each chunk is a hedged field, crop strips,
        /// the old road, a flower meadow, gone-back scrub, or water-meadow,
        /// picked deterministically from the zone ID.
        ///
        /// <para>The base terrain is still open meadow-and-coppice; the
        /// formation runs on top of it at priority 2500 and decides what
        /// KIND of place this chunk is. That is the fix for the complaint
        /// that started the overhaul: every Spread chunk used to be the
        /// same clearing with the grass moved around.</para>
        /// </summary>
        private ZoneGenerationPipeline CreateSpreadPipeline(int tier = 1)
        {
            var pipeline = CreateSurfacePipeline(BiomeType.Spread, tier,
                new JungleBuilder { SeedChance = 40, TreeChance = 0.04f });
            pipeline.AddBuilder(new SpreadFormationBuilder());
            return pipeline;
        }

        /// <summary>The Drowned Ledger's zone — "(17,5)" on the authored
        /// map. NOTE the routing: the Ledger is an authored Place, and
        /// every Place is installed as a Village POI (WorldGenerator.
        /// PlacePOIs), so this zone reaches CreateVillagePipeline's
        /// profile seam — NOT the Sodden biome case, which never runs
        /// for it. The three pre-Felling preserved are added there.</summary>
        public const string DrownedLedgerZoneID = "Overworld.17.5.0";

        /// <summary>The Sodden — the flood's country. Wetter and more
        /// choked than the Spread; W3 brings the mires and the
        /// Bog-Taken.</summary>
        private ZoneGenerationPipeline CreateSoddenPipeline(int tier = 1)
        {
            var pipeline = CreateSurfacePipeline(BiomeType.Sodden, tier,
                new JungleBuilder { SeedChance = 52, TreeChance = 0.14f });
            // W3.1 (Docs/FELLING-W3-PLAN.md): the anti-sameness machine
            // reaches the bog — mires, cuts, reeds, the causeway.
            pipeline.AddBuilder(new SoddenFormationBuilder());
            return pipeline;
        }

        /// <summary>The Beating — raw sun and salt. Open, exposed, and
        /// stony.</summary>
        /// <summary>The Last Counter has been pulled back twice; its
        /// abandoned predecessors stand further out, past where the
        /// guarantee now ends (Lore/Factions/04_SaccharineConcord.md:97).
        /// Authored zone ids, not procedural — time-depth is placed by
        /// hand.</summary>
        public const string AbandonedCounterZoneA = "Overworld.19.18.0";
        public const string AbandonedCounterZoneB = "Overworld.19.19.0";

        /// <summary>Somewhere in the deep wasteland a fire burns that no
        /// one tends. It appears on no map, in no POI list, and this
        /// constant's name is as much explanation as will ever exist
        /// (Lore/MYSTERY-LEDGER.md §4 — forbidden from answering:
        /// everything and everyone).</summary>
        public const string TenthFireZoneID = "Overworld.2.19.0";

        /// <summary>W4.5 — design gate 4: the doll in the wall. One
        /// authored wilderness Grovelands zone, never a POI, never a
        /// Bloom-front. WovenDollTests pins the scene the way the
        /// tenth fire's gate test pins the fire.</summary>
        public const string WovenDollZoneID = "Overworld.1.6.0";

        /// <summary>W4.7 close-out 🟡 — authored wilderness scenes whose
        /// cells opportunistic POIs must never claim. PlacePOIs rolls
        /// lairs and camps onto any cell far enough from the authored
        /// Places, and these zones have no Place to space them from:
        /// at seed 18 a POI took the doll's cell and design-gate-4 canon
        /// simply did not exist in that world. Future authored zones
        /// join this list.</summary>
        public static readonly string[] AuthoredWildernessZoneIDs =
            { TenthFireZoneID, WovenDollZoneID };

        private ZoneGenerationPipeline CreateBeatingPipeline(int tier = 1, string zoneID = null)
        {
            var pipeline = CreateSurfacePipeline(BiomeType.Beating, tier,
                new DesertBuilder { WallThreshold = 0.88f, RockChance = 0.06f });
            // W2.1 (Docs/FELLING-W1-W2-PLAN.md §7.6): the anti-sameness
            // machine reaches the wasteland — pans, ruins, dunes, the
            // caravan road, barrens, brine.
            pipeline.AddBuilder(new BeatingFormationBuilder());
            // W2.6: the two ruins the Concord left behind.
            if (zoneID == AbandonedCounterZoneA || zoneID == AbandonedCounterZoneB)
            {
                pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Beating, tier,
                    new List<StructureStamp> { StampCatalog.AbandonedCounter() },
                    priority: 3790, maxStructures: 1));
            }
            return pipeline;
        }

        /// <summary>W2.8: the tenth fire's zone is built BARE on purpose.
        /// The first look pass placed the fire in a zone that had rolled
        /// a hermit's hut (a TENDED fire thirty cells from the untended
        /// one) and a sentried tomb — the mystery needs emptiness. So:
        /// the open pan formation forced, NO ambient stamps, no
        /// containers — crust, salt, distance, and one fire.</summary>
        private ZoneGenerationPipeline CreateTenthFirePipeline(int tier)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new DesertBuilder { WallThreshold = 0.88f, RockChance = 0.06f });
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new BeatingFormationBuilder
            { Override = Formation.SaltPan, OmitSaltVeins = true });
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Beating, tier,
                new List<StructureStamp> { StampCatalog.TenthFire() },
                priority: 3790, maxStructures: 1));
            pipeline.AddBuilder(new PopulationBuilder(
                PopulationTable.GetBiomeTable(BiomeType.Beating, tier)));
            return pipeline;
        }

        /// <summary>W4.5 — the doll's grove, built bare on the tenth
        /// fire's precedent (CreateTenthFirePipeline): terrain,
        /// connectivity, the grove formation forced, the doll stamp,
        /// and the biome's own creatures. NO ambient landmark stamps
        /// (a hermit's hut beside the scene is exactly what W2.8
        /// caught), no containers, no Bloom-front.</summary>
        private ZoneGenerationPipeline CreateWovenDollPipeline(int tier)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new JungleBuilder { SeedChance = 50, TreeChance = 0.16f });
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new GrovelandsFormationBuilder { Override = Formation.Grove });
            pipeline.AddBuilder(new LandmarkBuilder(BiomeType.Grovelands, tier,
                new List<StructureStamp> { StampCatalog.WovenDoll() },
                priority: 3790, maxStructures: 1));
            pipeline.AddBuilder(new PopulationBuilder(
                PopulationTable.GetBiomeTable(BiomeType.Grovelands, tier)));
            return pipeline;
        }

        /// <summary>The Grovelands — Choir country. Dense growth with
        /// open cathedral floors between.</summary>
        private ZoneGenerationPipeline CreateGrovelandsPipeline(int tier = 1)
        {
            var pipeline = CreateSurfacePipeline(BiomeType.Grovelands, tier,
                new JungleBuilder { SeedChance = 50, TreeChance = 0.16f });
            // W4.1 (Docs/FELLING-W4-PLAN.md): the anti-sameness machine
            // reaches Choir country — groves, fens, walls, the fields.
            pipeline.AddBuilder(new GrovelandsFormationBuilder());
            return pipeline;
        }

        /// <summary>
        /// The Overwrit — the scraped region. Near-empty ground with
        /// almost nothing on it, which is the horror: a region this old
        /// should be FULL. W7 adds the bleeds; the emptiness is already
        /// the point.
        /// </summary>
        private ZoneGenerationPipeline CreateOverwritPipeline(int tier = 1)
            => CreateSurfacePipeline(BiomeType.Overwrit, tier,
                new DesertBuilder { WallThreshold = 0.97f, RockChance = 0.005f });

        /// <summary>The Stump — the petrified tepui. Rock and
        /// fissure.</summary>
        private ZoneGenerationPipeline CreateStumpPipeline(int tier = 1)
        {
            // W6.2a — the W0 placeholder (CaveBuilder 50/0.44, "rock
            // and fissure") produced ~86% wall and zones that often
            // could not be crossed at all (probe: open=245 of 1794,
            // crossed=false). Canon's bands are WALKABLE country —
            // fungal forest over ridges, bromeliad scrub, spray pools —
            // so the base is open stone with outcrops, and the W6
            // formation family carves the identity into it.
            // Cold-eye 🔴 — the first cut passed only the thresholds
            // and inherited DesertBuilder's STOCK CONTENT: "Sand"
            // floor (the Beating's own ground material, blueprint-keyed
            // with no biome awareness) and a hard-coded cactus
            // scatter. The petrified god-tree read as a cactus desert
            // while GrainRidge's own examine text said "pink-grey
            // stone". Canon: "Pink-grey sandstone (the real tepui is
            // pink sandstone)" — so the mountain gets its own floor,
            // its own wall, and no cacti.
            var pipeline = CreateSurfacePipeline(BiomeType.Stump, tier,
                new DesertBuilder
                {
                    WallThreshold = 0.90f,
                    RockChance = 0.05f,
                    CactusChance = 0f,
                    SandBlueprint = "TepuiStone",
                    SandstoneWallBlueprint = "TepuiWall",
                });
            // W6.2 — the tepui's formation family (band-keyed inside
            // the builder; off-mountain Stump-biome cells no-op).
            pipeline.AddBuilder(new StumpFormationBuilder());
            return pipeline;
        }

        /// <summary>
        /// The shared surface-wilderness spine: terrain, connectivity,
        /// a cave mouth, landmarks, hazards, containers, population,
        /// trade stock. Identical in shape to the four legacy biome
        /// pipelines — extracted so the six new ones cannot drift out of
        /// step with them by omission.
        /// </summary>
        private ZoneGenerationPipeline CreateSurfacePipeline(
            BiomeType biome, int tier, IZoneBuilder terrain)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(terrain);
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.AddBuilder(new CaveEntranceBuilder(this));
            pipeline.AddBuilder(new LandmarkBuilder(biome, tier));
            pipeline.AddBuilder(new HazardTerrainBuilder(biome));
            pipeline.AddBuilder(new ContainerBuilder(biome, tier,
                ContainerPlacementService.ZoneKind.Wilderness));
            pipeline.AddBuilder(new PopulationBuilder(
                PopulationTable.GetBiomeTable(biome, tier)));
            pipeline.AddBuilder(new TradeStockBuilder(SettlementManager));
            // The heavy things the drag verb exists for. Sparse by design —
            // see HaulablePropBuilder.
            pipeline.AddBuilder(new HaulablePropBuilder(biome));
            return pipeline;
        }

        private ZoneGenerationPipeline CreateVillagePipeline(BiomeType biome, PointOfInterest poi, string zoneID)
        {
            var pipeline = new ZoneGenerationPipeline();
            bool isStartingTown = zoneID == VillagePopulationBuilder.StartingVillageZoneId;
            pipeline.AddBuilder(new VillageBuilder(biome, poi, SettlementManager, largeTown: isStartingTown));

            // W2.6 (Docs/FELLING-W1-W2-PLAN.md §7.6, decision D5): the
            // first consumer of Place.Faction — until now all sixteen
            // named places generated as one identical village. Minimal
            // profile, not bespoke builders: Tent-Right places get the
            // guaranteed camp (tents + well + the cloth + a host to
            // speak the oath); the First Tent adds its monument; the
            // Last Counter gets the Concord post with the notice-board.
            // Priority 3860 mirrors the starting town's stamps — after
            // the river (3850), before population (4000).
            // W4.6 — the W2 R1 rule, fired: Cinderhold would have been
            // the THIRD name-keyed branch, so profiles promoted to DATA
            // (Place.Profile → PointOfInterest.Profile). The old
            // faction/name chain migrated case-for-case; PlaceProfileTests
            // and SoddenProfileTests re-pin unchanged behavior, and
            // PlaceProfileTests pins the table itself.
            var profileStamps = new List<StructureStamp>();
            switch (poi.Profile)
            {
                case "TentCamp":
                    profileStamps.Add(StampCatalog.TentRightProfileCamp());
                    break;
                case "TentCampFirst":
                    profileStamps.Add(StampCatalog.TentRightProfileCamp());
                    profileStamps.Add(StampCatalog.FirstTentMonument());
                    break;
                case "ConcordPost":
                    profileStamps.Add(StampCatalog.LastCounterPost());
                    break;
                // W3.2/W3.5: the Drowned Ledger — an expedition site,
                // not a market town (the three pre-Felling preserved
                // live here and nowhere else).
                case "ExcavationCamp":
                    profileStamps.Add(StampCatalog.ExcavationCamp());
                    break;
                // W3.6: Marrowstye — the body-courier's destination.
                case "Intake":
                    profileStamps.Add(StampCatalog.CurationIntake());
                    break;
                case "Boatyard":
                    profileStamps.Add(StampCatalog.SumpholdBoatyard());
                    break;
                // W4.6: Cinderhold — the Concord's Grovelands post and
                // the pruning contract's home.
                case "PruningPost":
                    profileStamps.Add(StampCatalog.PruningPost());
                    break;
                case null:
                case "":
                    break; // a plain village, by design
                default:
                    // A typo'd profile would otherwise generate a plain
                    // village in silence (close-out 🔵).
                    Debug.LogWarning($"[Worldgen] Unknown village profile " +
                        $"'{poi.Profile}' on {poi.Name} — generating plain.");
                    break;
            }
            if (profileStamps.Count > 0)
                pipeline.AddBuilder(new LandmarkBuilder(biome, 1, profileStamps,
                    priority: 3860, maxStructures: profileStamps.Count));
            // STARTING TOWN (Docs/STARTING-TOWN.md): five guaranteed
            // shop stamps at priority 3860 — AFTER the river (3850) so
            // water is on the map before footprint checks, before
            // population (4000) which now respects the claimed cells.
            if (isStartingTown)
            {
                pipeline.AddBuilder(new LandmarkBuilder(biome, 1,
                    StampCatalog.Town(), priority: 3860, maxStructures: 5));
            }
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
            pipeline.AddBuilder(new ContainerBuilder(BiomeType.Cave, 1,
                ContainerPlacementService.ZoneKind.Village));

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
            // Lairs previously got ZERO containers of any kind.
            pipeline.AddBuilder(new ContainerBuilder(biome, 2, ContainerPlacementService.ZoneKind.Lair));
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

        public void ReplaceLoadedOverworldState(
            WorldMap worldMap,
            SettlementManager settlementManager,
            System.Func<int> turnProvider)
        {
            if (worldMap != null)
                WorldMap = worldMap;

            if (settlementManager != null)
                SettlementManager = settlementManager;

            SetTurnProvider(turnProvider);
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
                zone.AmbientLevel = GetDepthAmbient(wz);
                // Close-out hypothesis H12 — the drowned sima shipped
                // at catacomb darkness with zero light sources: the one
                // floor archetype with no glow of its own was also the
                // one at the bottom of an OPEN HOLE. A real sima is a
                // light well — daylight falling down the shaft is why
                // anything grows at the bottom at all (it is the whole
                // premise of the Time-Locked Forest archetype). The
                // floor of a sinkhole stack sits under open sky, so it
                // reads brighter than sealed stone at the same depth.
                // Archetype-gated: the village floor's darkness is
                // AUTHORED (canon's reveal order — "the GLOW before the
                // people" needs the dark around the patch), and the
                // cathedral's vault likewise. Only the sima is canon's
                // light-well.
                var poiAbove = WorldMap.GetPOI(wx, wy);
                if (poiAbove != null && poiAbove.Type == POIType.Sinkhole && wz == 2
                    && SinkholeArchetypes.For(poiAbove.Name) == SinkholeArchetype.DrownedSima)
                    zone.AmbientLevel = ShaftLightAmbient;

                // Mark all cells as interior. Extracted so a future zone-
                // hydration path (save/load) can call it too without
                // re-running the generator pipeline.
                MarkDungeonInterior(zone);
                return;
            }

            if (!WorldMap.InBounds(wx, wy))
                return;

            zone.AmbientTint = GetBiomeTint(WorldMap.GetBiome(wx, wy));
            zone.AmbientLevel = Zone.DefaultAmbientLevel;
            // W6.1 — on the tepui, altitude shifts the light: warm at
            // the base, cool and bright at the petrified canopy.
            zone.AmbientTint = StumpBands.TintFor(
                StumpBands.BandAt(wx, wy), zone.AmbientTint);
        }

        /// <summary>
        /// How dark it is down there, by depth (Felling W0.2).
        ///
        /// <para>W0 is plumbing: this returns the historical flat value
        /// so nothing changes on screen yet. The shape is here — and
        /// tested — because W5 (catacombs) replaces the body with the
        /// authored ladder from Docs/FELLING-WORLD-DESIGN.md §6
        /// (surface 0.40 → sinkhole floor 0.22 → catacomb 0.12 → dead
        /// zone 0.02), and that phase must not also be inventing where
        /// the number comes from.</para>
        ///
        /// <para>Two constraints W5 inherits: the introspection doc's
        /// quit-trigger warning (a room the player cannot navigate at
        /// all is a bad room, so the floor stays navigable and the
        /// *content* is what demands carried light), and the
        /// remembered-cell floor — <c>RememberedColor</c> is a flat 0.2
        /// gray applied unmodulated, so an ambient below it renders
        /// visible cells darker than remembered ones.</para>
        /// </summary>
        /// <summary>Ambient on a sinkhole FLOOR (z=2): the shaft is
        /// open to the sky, so daylight reaches the bottom — dimmer
        /// than the surface (0.40), brighter than sealed stone at the
        /// same depth (0.22).</summary>
        public const float ShaftLightAmbient = 0.34f;

        public static float GetDepthAmbient(int depth)
        {
            // The authored ladder (FELLING-WORLD-DESIGN.md §6). Light is
            // the underground's terrain: the shaft still catches day
            // from the mouth, the floor keeps just enough to move by,
            // and the catacombs make carried light the point. Nothing
            // reaches zero — a room you cannot navigate at all is a bad
            // room, so it is the CONTENT (plaque-reading, dead-zone
            // traversal) that demands a lamp, never the floor itself.
            if (depth <= 0) return Zone.DefaultAmbientLevel; // 0.40, the day
            if (depth == 1) return 0.28f;  // the descent, lit from above
            if (depth == 2) return 0.22f;  // the floor
            return 0.12f;                  // catacombs and below
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
                // Felling W0.6 — see Docs/FELLING-WORLD-DESIGN.md §6.
                case BiomeType.Spread:     return new Color(1.0f, 1.0f, 0.94f);
                case BiomeType.Sodden:     return new Color(0.82f, 0.86f, 0.80f);
                case BiomeType.Beating:    return new Color(1.0f, 0.98f, 0.88f);
                case BiomeType.Grovelands: return new Color(0.88f, 0.92f, 0.85f);
                // The Overwrit is grey on purpose: a scraped page.
                case BiomeType.Overwrit:   return new Color(0.9f, 0.9f, 0.9f);
                case BiomeType.Stump:      return StumpBands.BaseTint;
                default:                return Color.white;
            }
        }

        protected override void PrepareZoneForAccess(string zoneID)
        {
            if (!WorldMap.IsOverworldZoneID(zoneID))
                return;

            var (wx, wy, wz) = WorldMap.FromZoneID(zoneID);

            // Close-out hypothesis H1 — the stack generates TOP-DOWN.
            // A sinkhole floor reached LATERALLY (walking underground
            // from the next chunk over) used to generate before its
            // own descent existed: StairsUpBuilder READS the connection
            // the level above registers, finds nothing, and the floor
            // is born with no way up — a soft-lock in a recoverable-
            // death RPG. Generating the level above first (recursively
            // to the mouth) guarantees every connection exists before
            // the level that consumes it builds. Sequential, not
            // reentrant: the recursive GetZone completes before this
            // zone generates; the cache makes repeat calls free.
            if (wz > 0 && wz <= 2 && WorldMap.InBounds(wx, wy)
                && !CachedZones.ContainsKey(zoneID))
            {
                var poiHere = WorldMap.GetPOI(wx, wy);
                if (poiHere != null && poiHere.Type == POIType.Sinkhole)
                    GetZone($"Overworld.{wx}.{wy}.{wz - 1}");
            }

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
