using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.3 (Docs/FELLING-W4-PLAN.md §3) — Choir country's own fauna.
    /// The Grovelands stops borrowing the jungle's roster; what stays
    /// (rotlings, mosshulks) stays because it is FUNGAL fauna, Choir-
    /// adjacent by nature. The new residents: glow-moths that navigate
    /// by the groves, spore shamblers that used to be somebody, and
    /// wine-leaf sundews whose leaf color is a danger read — the
    /// design doc's "Shamblers ... (all ship)" was a false premise
    /// caught by the plan's §1 sweep; the Shambler ships HERE.
    /// </summary>
    public class GrovelandsBestiaryTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            HarvestablePart.Factory = _factory;
        }

        [OneTimeTearDown]
        public void TearDownOnce() => HarvestablePart.Factory = null;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown() => GreatdewSnarePart.TestRng = null;

        // ════════════════════════════════════════════════════════
        // The tables
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheGrovelands_HasItsOwnTables_AtEveryTier()
        {
            Assert.AreEqual("GrovelandsTier1", PopulationTable.GetBiomeTable(BiomeType.Grovelands, 1).Name);
            Assert.AreEqual("GrovelandsTier2", PopulationTable.GetBiomeTable(BiomeType.Grovelands, 2).Name);
            Assert.AreEqual("GrovelandsTier3", PopulationTable.GetBiomeTable(BiomeType.Grovelands, 3).Name);
            Assert.AreEqual("GrovelandsLairGuards", PopulationTable.LairGuards(BiomeType.Grovelands).Name);
        }

        [Test]
        public void EveryGrovelandsTableEntry_IsARealBlueprint()
        {
            // The fail-loud content gate, per house rule.
            var tables = new[]
            {
                PopulationTable.GetBiomeTable(BiomeType.Grovelands, 1),
                PopulationTable.GetBiomeTable(BiomeType.Grovelands, 2),
                PopulationTable.GetBiomeTable(BiomeType.Grovelands, 3),
                PopulationTable.LairGuards(BiomeType.Grovelands),
            };
            foreach (var table in tables)
                foreach (var entry in table.Entries)
                    Assert.IsNotNull(_factory.CreateEntity(entry.BlueprintName),
                        $"{table.Name} names '{entry.BlueprintName}', which does not exist");
        }

        [Test]
        public void NoTrueJungleCreature_HauntsChoirCountry()
        {
            // Rotling and Mosshulk stay (fungal fauna); the jungle's own
            // predators do not.
            var jungle = new HashSet<string> { "GiantSpider", "JungleStalker", "Viper" };
            foreach (int tier in new[] { 1, 2, 3 })
                foreach (var entry in PopulationTable.GetBiomeTable(BiomeType.Grovelands, tier).Entries)
                    Assert.IsFalse(jungle.Contains(entry.BlueprintName),
                        $"tier {tier} still carries {entry.BlueprintName}");
        }

        // ════════════════════════════════════════════════════════
        // The residents
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheShambler_UsedToBeSomebody_AndGivesUpItsSpores()
        {
            var shambler = _factory.CreateEntity("Shambler");
            Assert.IsNotNull(shambler, "the design doc said it shipped; NOW it does");
            Assert.AreEqual("ShamblerSporeSac",
                shambler.GetPart<CorpsePart>().HarvestBlueprint);

            var sac = _factory.CreateEntity("ShamblerSporeSac");
            var reagent = sac.GetPart<ReagentPart>();
            int toxic = 0, volatile_ = 0;
            foreach (var p in reagent.GetProperties())
            {
                if (p.Property == BrewProperties.Toxic) toxic = p.Potency;
                if (p.Property == BrewProperties.Volatile) volatile_ = p.Potency;
            }
            Assert.AreEqual(2, toxic, "keep it from your face");
            Assert.AreEqual(1, volatile_, "and from flame");
            Assert.AreEqual(13, sac.GetPart<CommercePart>().Value);
        }

        [Test]
        public void TheGlowMoth_IsGentle_AndCarriesNoLight()
        {
            // The perf rule (plan §4): no LightSourcePart on a MOVING
            // entity — every wander step would dirty the lightmap. The
            // moth is dusted the columns' color; the render carries the
            // idea, the part stays off.
            var moth = _factory.CreateEntity("GlowMoth");
            Assert.IsTrue(moth.GetPart<BrainPart>().Passive, "it has never hurt anything");
            Assert.IsNull(moth.GetPart<LightSourcePart>(),
                "a wandering light source is a per-turn lightmap recompute — the glow is paint");
        }

        [Test]
        public void TheSundew_IsAGreatdewWithItsOwnTuning()
        {
            // Plan R5: content reuse of the snare part, not new code —
            // and the diag identity rides the blueprint name, so a
            // sundew grab and a greatdew grab stay distinguishable.
            var sundew = _factory.CreateEntity("WineLeafSundew");
            var snare = sundew.GetPart<GreatdewSnarePart>();
            Assert.IsNotNull(snare);
            Assert.AreEqual(45, snare.GrabChance);
            Assert.AreEqual(3, snare.HoldTurns, "a quicker, hungrier hold than the greatdew");
        }

        [Test]
        public void SteppingIntoTheSundew_GetsHeld()
        {
            var zone = new Zone("Z");
            var sundew = _factory.CreateEntity("WineLeafSundew");
            sundew.GetPart<GreatdewSnarePart>().GrabChance = 100;
            zone.AddEntity(sundew, 11, 10);
            var walker = new Entity { ID = "w", BlueprintName = "Walker" };
            walker.Tags["Creature"] = "";
            walker.AddPart(new RenderPart { DisplayName = "walker" });
            walker.AddPart(new PhysicsPart { Solid = true });
            walker.Statistics["Hitpoints"] = new Stat { Owner = walker, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(walker, 10, 10);

            Assert.IsTrue(MovementSystem.TryMove(walker, zone, 1, 0));
            Assert.IsTrue(walker.HasEffect<RootedEffect>(), "the leaves go darker");
        }

        [Test]
        public void TheCompost_GivesThingsUp_ButIsNotAMint()
        {
            // W4.3 adversarial review (RED pre-fix): the first cut put a
            // 60% GoldCoin harvest on EVERY row — ~200 rows per field ≈
            // 240 coins per zone, strip-mineable. The loot half now
            // lives in a separate CompostCache: a FEW spots per field
            // where what comes back up still has pockets; the rows
            // themselves are texture, not currency.
            var row = _factory.CreateEntity("CompostRow");
            Assert.IsNull(row.GetPart<HarvestablePart>(),
                "the rows are the horror; they are not the loot");

            var cache = _factory.CreateEntity("CompostCache");
            Assert.IsNotNull(cache, "the finds exist");
            var harvest = cache.GetPart<HarvestablePart>();
            Assert.IsNotNull(harvest);
            Assert.AreEqual("GoldCoin", harvest.YieldBlueprint,
                "found possessions — the pile holds what the grove took");
        }

        [Test]
        public void ACompostingField_HoldsOnlyAFewFinds()
        {
            // The economy bound, per zone, pinned across seeds.
            for (int seed = 0; seed < 12; seed++)
            {
                var zone = new Zone("Overworld.0.0.0");
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        var g = _factory.CreateEntity("Grass");
                        if (g != null) zone.AddEntity(g, x, y);
                    }
                new GrovelandsFormationBuilder { Override = Formation.CompostingField }
                    .BuildZone(zone, _factory, new Random(seed));

                int caches = 0, rows = 0;
                foreach (var e in zone.GetAllEntities())
                {
                    if (e.BlueprintName == "CompostCache") caches++;
                    if (e.BlueprintName == "CompostRow") rows++;
                }
                Assert.That(caches, Is.InRange(2, 4),
                    $"seed {seed}: a few finds, not a payroll ({caches})");
                Assert.Greater(rows, 20,
                    $"seed {seed}: the rows still carry the horror");
            }
        }

        [Test]
        public void TierOne_StaysGentle()
        {
            // W4.3 adversarial review (RED pre-fix): the Shambler sat in
            // Tier 1 against the Sodden precedent (the MawToad entered
            // its tables at Tier 2). The gentle ring stays gentle; the
            // slow shapes start where the strangeness does.
            foreach (var entry in PopulationTable.GetBiomeTable(BiomeType.Grovelands, 1).Entries)
                Assert.AreNotEqual("Shambler", entry.BlueprintName,
                    "tier 1 is moths and warnings, not the half-taken");
            bool atTwo = false;
            foreach (var entry in PopulationTable.GetBiomeTable(BiomeType.Grovelands, 2).Entries)
                if (entry.BlueprintName == "Shambler") atTwo = true;
            Assert.IsTrue(atTwo, "and they arrive with tier 2, like the toads did");
        }
    }
}
