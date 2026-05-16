using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.2 — data layer for the liquid-coating system.
    /// Pins <see cref="LiquidRegistry"/> (JSON load, Get round-trip,
    /// unknown→null, malformed tolerated, ResetForTests pollution
    /// guard) and the canonical seed definitions (water/oil/acid).
    ///
    /// Per the plan's B1 test-cascade discipline: pure data, no Zone /
    /// EntityFactory / OverworldZoneManager. Inline JSON only.
    /// </summary>
    public class LiquidRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            // Mirror the TinkerRecipeRegistry pollution guard: a test
            // that Initialize()s the registry must not leak its
            // definitions into a subsequent Play session or test.
            LiquidRegistry.ResetForTests();
        }

        private const string ThreeLiquidsJson = @"{
          ""Liquids"": [
            {
              ""Id"": ""water"", ""DisplayName"": ""water"", ""Adjective"": ""wet"",
              ""Glyph"": ""~"", ""Color"": ""&c"",
              ""Conductivity"": 100, ""Combustibility"": -50, ""FireDampen"": 40,
              ""Adsorbence"": 100, ""Fluidity"": 30, ""Evaporativity"": 20
            },
            {
              ""Id"": ""oil"", ""DisplayName"": ""oil"", ""Adjective"": ""oily"",
              ""Glyph"": ""~"", ""Color"": ""&K"",
              ""Conductivity"": 0, ""Combustibility"": 90, ""FlameTemperature"": 250,
              ""Adsorbence"": 100, ""Fluidity"": 5, ""Evaporativity"": 2, ""Staining"": 1
            },
            {
              ""Id"": ""acid"", ""DisplayName"": ""acid"", ""Adjective"": ""acid-covered"",
              ""Glyph"": ""~"", ""Color"": ""&G"",
              ""Conductivity"": 0, ""Combustibility"": 0,
              ""Adsorbence"": 100, ""Fluidity"": 20, ""Evaporativity"": 15,
              ""PerTurnDamage"": { ""Amount"": 3, ""Type"": ""Acid"" }
            }
          ]
        }";

        // ── Load + Get ─────────────────────────────────────────

        [Test]
        public void Initialize_LoadsAllThree()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            Assert.IsTrue(LiquidRegistry.IsInitialized);
            Assert.AreEqual(3, LiquidRegistry.Count);
        }

        [Test]
        public void Get_KnownId_ReturnsDefinition()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            var water = LiquidRegistry.Get("water");
            Assert.IsNotNull(water);
            Assert.AreEqual("water", water.Id);
            Assert.AreEqual("wet", water.Adjective);
        }

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            Assert.IsNull(LiquidRegistry.Get("plasma"));
            Assert.IsNull(LiquidRegistry.Get(null));
            Assert.IsNull(LiquidRegistry.Get(""));
        }

        // ── Canonical definitions (parity knobs) ───────────────

        [Test]
        public void Water_IsConductive_AndFireDamping()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            var w = LiquidRegistry.Get("water");
            Assert.GreaterOrEqual(w.Conductivity, 100,
                "Water must be highly conductive (drives wet→electric).");
            Assert.Less(w.Combustibility, 0,
                "Water Combustibility must be negative (damps fire).");
            Assert.Greater(w.FireDampen, 0,
                "Water must reduce incoming fire damage.");
        }

        [Test]
        public void Oil_IsHighlyFlammable_NotConductive()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            var o = LiquidRegistry.Get("oil");
            Assert.GreaterOrEqual(o.Combustibility, 50,
                "Oil must be highly combustible (amplifies fire).");
            Assert.AreEqual(0, o.Conductivity,
                "Oil is an electrical insulator.");
            Assert.Less(o.FlameTemperature, 99999,
                "Oil must have a low ignition point.");
        }

        [Test]
        public void Acid_HasPerTurnDamage()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            var a = LiquidRegistry.Get("acid");
            Assert.IsNotNull(a.PerTurnDamage);
            Assert.Greater(a.PerTurnDamage.Amount, 0);
            Assert.AreEqual("Acid", a.PerTurnDamage.Type);
        }

        [Test]
        public void NonAcid_HasNoPerTurnDamage()
        {
            // Counter-check: water/oil must NOT carry per-turn damage
            // (only acid-class liquids tick).
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            Assert.IsTrue(LiquidRegistry.Get("water").PerTurnDamage == null
                || LiquidRegistry.Get("water").PerTurnDamage.Amount == 0);
            Assert.IsTrue(LiquidRegistry.Get("oil").PerTurnDamage == null
                || LiquidRegistry.Get("oil").PerTurnDamage.Amount == 0);
        }

        // ── Multi-source merge ─────────────────────────────────

        [Test]
        public void InitializeFromJsonSources_MergesMultipleFiles()
        {
            LiquidRegistry.InitializeFromJsonSources(new List<string>
            {
                @"{ ""Liquids"": [ { ""Id"": ""water"", ""Adjective"": ""wet"" } ] }",
                @"{ ""Liquids"": [ { ""Id"": ""oil"", ""Adjective"": ""oily"" } ] }",
            });
            Assert.AreEqual(2, LiquidRegistry.Count);
            Assert.IsNotNull(LiquidRegistry.Get("water"));
            Assert.IsNotNull(LiquidRegistry.Get("oil"));
        }

        [Test]
        public void LaterSource_OverridesSameId()
        {
            LiquidRegistry.InitializeFromJsonSources(new List<string>
            {
                @"{ ""Liquids"": [ { ""Id"": ""water"", ""Adjective"": ""wet"" } ] }",
                @"{ ""Liquids"": [ { ""Id"": ""water"", ""Adjective"": ""soaked"" } ] }",
            });
            Assert.AreEqual(1, LiquidRegistry.Count);
            Assert.AreEqual("soaked", LiquidRegistry.Get("water").Adjective);
        }

        // ── Adversarial ────────────────────────────────────────

        [Test]
        public void Initialize_NullJson_DoesNotThrow_EmptyRegistry()
        {
            Assert.DoesNotThrow(() => LiquidRegistry.Initialize(null));
            Assert.AreEqual(0, LiquidRegistry.Count);
            Assert.IsTrue(LiquidRegistry.IsInitialized);
        }

        [Test]
        public void Initialize_MalformedJson_DoesNotThrow_NoEntries()
        {
            Assert.DoesNotThrow(() =>
                LiquidRegistry.Initialize("{ this is not valid json ]["));
            Assert.AreEqual(0, LiquidRegistry.Count);
        }

        [Test]
        public void Initialize_EmptyLiquidsArray_NoEntries()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"": [] }");
            Assert.AreEqual(0, LiquidRegistry.Count);
            Assert.IsTrue(LiquidRegistry.IsInitialized);
        }

        [Test]
        public void Initialize_LiquidWithEmptyId_IsSkipped()
        {
            LiquidRegistry.Initialize(
                @"{ ""Liquids"": [ { ""Id"": """", ""Adjective"": ""ghost"" },
                                   { ""Id"": ""water"" } ] }");
            Assert.AreEqual(1, LiquidRegistry.Count);
            Assert.IsNotNull(LiquidRegistry.Get("water"));
        }

        [Test]
        public void ResetForTests_ClearsRegistry()
        {
            LiquidRegistry.Initialize(ThreeLiquidsJson);
            Assert.AreEqual(3, LiquidRegistry.Count);
            LiquidRegistry.ResetForTests();
            Assert.AreEqual(0, LiquidRegistry.Count);
            Assert.IsFalse(LiquidRegistry.IsInitialized);
        }

        [Test]
        public void StatModifiers_RoundTripFromJson()
        {
            // Forward-looking: LQ.6's stat liquids are JSON-only. Pin
            // the StatModifiers / ResistanceModifiers shape now so the
            // data layer is proven before LQ.6 adds content.
            LiquidRegistry.Initialize(@"{
              ""Liquids"": [ {
                ""Id"": ""brine"", ""Adjective"": ""briny"",
                ""ResistanceModifiers"": [
                  { ""Stat"": ""HeatResistance"", ""Delta"": 15 },
                  { ""Stat"": ""ElectricResistance"", ""Delta"": -15 }
                ],
                ""StatModifiers"": [ { ""Stat"": ""Agility"", ""Delta"": -1 } ]
              } ]
            }");
            var b = LiquidRegistry.Get("brine");
            Assert.IsNotNull(b);
            Assert.AreEqual(2, b.ResistanceModifiers.Count);
            Assert.AreEqual("HeatResistance", b.ResistanceModifiers[0].Stat);
            Assert.AreEqual(15, b.ResistanceModifiers[0].Delta);
            Assert.AreEqual(-15, b.ResistanceModifiers[1].Delta);
            Assert.AreEqual(1, b.StatModifiers.Count);
            Assert.AreEqual("Agility", b.StatModifiers[0].Stat);
            Assert.AreEqual(-1, b.StatModifiers[0].Delta);
        }
    }
}
