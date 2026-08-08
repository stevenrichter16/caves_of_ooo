using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase B — adversarial sweep
    /// (log in Docs/BIOME-OVERHAUL-LOG.md). Surfaces: the RestAtInn
    /// "cost[:site]" PARSER (malformed forms must fall back safely and
    /// never over/under-charge), CureEffect boundaries, and camp
    /// PLACEMENT ROBUSTNESS — the B1 end-to-end regression proved
    /// guaranteed stamps live or die by terrain; this pins placement
    /// across all four biomes and many seeds so a future terrain tweak
    /// can't quietly re-break the promise of the $ POI.
    /// </summary>
    [TestFixture]
    public class BiomePhaseBAdversarialTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown() => LootTableRegistry.ResetForTests();

        // ════════════════ RestAtInn arg parser ════════════════

        private static Entity Patient(int drams, int hp = 5, int maxHp = 30)
        {
            var p = new Entity { ID = "p" };
            p.AddPart(new RenderPart { DisplayName = "p" });
            p.AddPart(new StatusEffectsPart());
            p.SetIntProperty(TradeSystem.CURRENCY_PROP, drams);
            var stat = new Stat { Owner = p, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = maxHp };
            p.Statistics["Hitpoints"] = stat;
            return p;
        }

        [Test]
        public void Adversarial_RestAtInn_MalformedCosts_FallBackToTen()
        {
            foreach (var arg in new[] { "abc", "", ":", ":fireside", "abc:fireside" })
            {
                var p = Patient(drams: 50);
                ConversationActions.Execute("RestAtInn", null, p, arg);
                Assert.AreEqual(40, p.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                    $"'{arg}': unparseable cost falls back to the default 10 — never free, never ruinous");
                Assert.AreEqual(30, p.GetStat("Hitpoints").Value, $"'{arg}': still heals");
            }
        }

        [Test]
        public void Adversarial_RestAtInn_NegativeCost_NeverPays()
        {
            var p = Patient(drams: 50);
            ConversationActions.Execute("RestAtInn", null, p, "-5:scam corner");
            Assert.AreEqual(40, p.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                "negative costs clamp to the default — resting never MINTS drams");
        }

        // ════════════════ CureEffect boundaries ════════════════

        [Test]
        public void Adversarial_CureEffect_GibberishAndNulls_NoCrash()
        {
            var p = Patient(drams: 0);
            ConversationActions.Execute("CureEffect", null, p, "NoSuchEffectEver");
            ConversationActions.Execute("CureEffect", null, p, "   ");
            ConversationActions.Execute("CureEffect", null, null, "Poisoned");
            // Surviving the calls is the assertion; the polite-no-op
            // message path is pinned in BiomeHermitTests.
        }

        // ════════════════ Camp placement robustness ════════════════

        [Test]
        public void Adversarial_MerchantCamp_PlacesInEveryBiome_AcrossSeeds()
        {
            // The promise of the $ POI: a camp ALWAYS exists. 4 biomes ×
            // 6 seeds of real terrain. This catches both strict-footprint
            // fragility (the jungle-trees regression) and any future
            // terrain change that starves guaranteed stamps of space.
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
            {
                for (int seed = 1; seed <= 6; seed++)
                {
                    var zone = new Zone($"Overworld.5.5.0");
                    IZoneBuilder terrain;
                    switch (biome)
                    {
                        case BiomeType.Desert: terrain = new DesertBuilder(); break;
                        case BiomeType.Jungle: terrain = new JungleBuilder(); break;
                        case BiomeType.Ruins: terrain = new RuinsBuilder(); break;
                        default: terrain = new CaveBuilder(); break;
                    }
                    // RuinsBuilder can legitimately return false (<2 rooms)
                    // and the pipeline retries; mirror that here.
                    bool built = false;
                    for (int attempt = 0; attempt < 5 && !built; attempt++)
                        built = terrain.BuildZone(zone, _factory, new Random(seed * 100 + attempt));
                    Assert.IsTrue(built, $"{biome} seed {seed}: terrain");

                    new LandmarkBuilder(biome, 1,
                        new List<StructureStamp> { StampCatalog.MerchantCamp(biome) }, priority: 3790)
                        .BuildZone(zone, _factory, new Random(seed));

                    int merchants = 0;
                    foreach (var e in zone.GetAllEntities())
                        if (e.BlueprintName == "Merchant") merchants++;
                    Assert.AreEqual(1, merchants,
                        $"{biome} seed {seed}: the guaranteed camp must place");
                }
            }
        }

        [Test]
        public void Adversarial_VegetationClearing_NeverEatsWalls()
        {
            // Counter-check on the B1 fix: a clearing stamp fells trees
            // but must never carve through authored/terrain WALLS.
            var zone = new Zone("T");
            Assert.IsTrue(new JungleBuilder().BuildZone(zone, _factory, new Random(3)));
            int wallsBefore = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.HasTag("Wall")) wallsBefore++;

            new LandmarkBuilder(BiomeType.Jungle, 1,
                new List<StructureStamp> { StampCatalog.MerchantCamp(BiomeType.Jungle) }, priority: 3790)
                .BuildZone(zone, _factory, new Random(3));

            // The camp stamp ADDS exactly 12 '#' walls of its own and its
            // footprint must have been rejected anywhere existing walls
            // stood — so the total is exactly before + 12, never fewer.
            int wallsAfter = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.HasTag("Wall")) wallsAfter++;
            Assert.AreEqual(wallsBefore + 12, wallsAfter,
                "clearing must fell trees only — never terrain walls");
        }
    }
}
