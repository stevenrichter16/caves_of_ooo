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
        public void TheCompostRows_GiveThingsUp()
        {
            // The W4.1 review assigned CompostRow's "loot" half here:
            // the pile is where travelers' things end up, and harvest is
            // how they come back out.
            var row = _factory.CreateEntity("CompostRow");
            var harvest = row.GetPart<HarvestablePart>();
            Assert.IsNotNull(harvest, "loot and horror in one pile — now actually loot");
            Assert.AreEqual("GoldCoin", harvest.YieldBlueprint);
            Assert.IsNotNull(_factory.CreateEntity("GoldCoin"), "and the coin is real");
        }
    }
}
