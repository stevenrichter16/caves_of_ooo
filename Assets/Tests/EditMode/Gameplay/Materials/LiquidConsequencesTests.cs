using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.5 — consequences (generalizes plan gap **c**). A liquid coat
    /// now changes how elemental damage lands:
    ///   - water coat amplifies Lightning, dampens Fire
    ///   - oil coat amplifies Fire
    ///   - acid coat ticks Acid damage each turn
    ///   - a conductive coat doubles an ElectrifiedEffect's charge the
    ///     same way WetEffect does (OnApply generalization, additive OR)
    ///
    /// Pins the review-corrected contracts:
    ///   - divergence #6: LiquidCovered YIELDS Lightning amplification
    ///     to a present ElectrifiedEffect (no 4× double-amplify)
    ///   - divergence #3 stays intact: water coat still applies
    ///     WetEffect, so the pinned ElectrifiedEffectDamageTests are
    ///     untouched (none of them add a LiquidCoveredEffect → my
    ///     OnBeforeTakeDamage never runs there)
    ///
    /// Test discipline (plan §B1): bare Entity + inline JSON; the REAL
    /// CombatSystem.ApplyDamage fires BeforeTakeDamage so
    /// LiquidCoveredEffect.OnBeforeTakeDamage runs through the real
    /// per-effect dispatch (StatusEffectsPart.HandleBeforeTakeDamage).
    /// </summary>
    public class LiquidConsequencesTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.Initialize(@"{
              ""Liquids"": [
                { ""Id"": ""water"", ""DisplayName"": ""water"", ""Adjective"": ""wet"",
                  ""Conductivity"": 100, ""Combustibility"": -50, ""FireDampen"": 40,
                  ""Fluidity"": 30, ""Evaporativity"": 20 },
                { ""Id"": ""oil"", ""DisplayName"": ""oil"", ""Adjective"": ""oily"",
                  ""Combustibility"": 90, ""FlameTemperature"": 250,
                  ""Fluidity"": 5, ""Evaporativity"": 2 },
                { ""Id"": ""acid"", ""DisplayName"": ""acid"", ""Adjective"": ""acid-covered"",
                  ""Fluidity"": 20, ""Evaporativity"": 15,
                  ""PerTurnDamage"": { ""Amount"": 3, ""Type"": ""Acid"" } },
                { ""Id"": ""brine"", ""DisplayName"": ""brine"", ""Adjective"": ""briny"",
                  ""Conductivity"": 100, ""Fluidity"": 25, ""Evaporativity"": 10 },
                { ""Id"": ""weakdampen"", ""DisplayName"": ""weakdampen"", ""Adjective"": ""damp"",
                  ""FireDampen"": -30, ""Fluidity"": 20, ""Evaporativity"": 10 },
                { ""Id"": ""fulldampen"", ""DisplayName"": ""fulldampen"", ""Adjective"": ""soaked"",
                  ""FireDampen"": 100, ""Fluidity"": 20, ""Evaporativity"": 10 }
              ]
            }");
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        // ── Fixture ─────────────────────────────────────────────

        private static Entity MakeCreature(int hp = 200)
        {
            var e = new Entity { ID = "c", BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
            { Owner = e, Name = "Toughness", BaseValue = 10 };
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static int Hit(Entity c, int amount, string attribute)
        {
            int before = c.GetStatValue("Hitpoints");
            var d = new Damage(amount);
            if (!string.IsNullOrEmpty(attribute)) d.AddAttribute(attribute);
            CombatSystem.ApplyDamage(c, d, source: null, zone: null);
            return before - c.GetStatValue("Hitpoints");
        }

        private static GameEvent TickCtx()
        {
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Zone", (object)null);
            return ev;
        }

        // ── Positive: damage modification ───────────────────────

        [Test]
        public void WaterCoat_AmplifiesLightningDamage()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int coatedDmg = Hit(coated, 20, "Lightning");

            var bare = MakeCreature();
            int bareDmg = Hit(bare, 20, "Lightning");

            Assert.Greater(coatedDmg, bareDmg,
                "Water coat (Conductivity 100) must amplify Lightning damage.");
        }

        [Test]
        public void WaterCoat_DampensFireDamage()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int coatedDmg = Hit(coated, 40, "Fire");

            var bare = MakeCreature();
            int bareDmg = Hit(bare, 40, "Fire");

            Assert.Less(coatedDmg, bareDmg,
                "Water coat (FireDampen 40) must reduce Fire damage.");
            Assert.Greater(coatedDmg, 0, "Dampen is partial, not immunity.");
        }

        [Test]
        public void OilCoat_AmplifiesFireDamage()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("oil", 30));
            int coatedDmg = Hit(coated, 20, "Fire");

            var bare = MakeCreature();
            int bareDmg = Hit(bare, 20, "Fire");

            Assert.Greater(coatedDmg, bareDmg,
                "Oil coat (Combustibility 90) must amplify Fire damage.");
        }

        [Test]
        public void AcidCoat_TicksAcidDamage_OnTurnStart()
        {
            var c = MakeCreature();
            var coat = new LiquidCoveredEffect("acid", 30);
            c.ApplyEffect(coat);
            int before = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, TickCtx());
            Assert.Less(c.GetStatValue("Hitpoints"), before,
                "Acid coat must deal PerTurnDamage on OnTurnStart.");
        }

        [Test]
        public void ConductiveCoat_DoublesElectrifiedCharge_LikeWet()
        {
            // Generalization (additive OR): a conductive, NON-water coat
            // (id != "water" so divergence #3 does NOT apply a WetEffect)
            // still doubles ElectrifiedEffect charge on apply.
            var dry = MakeCreature();
            var dElec = new ElectrifiedEffect(charge: 1.0f);
            dry.ApplyEffect(dElec);
            dElec.OnTurnStart(dry, TickCtx());
            int dryDmg = 200 - dry.GetStatValue("Hitpoints");

            var briny = MakeCreature();
            briny.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.IsFalse(briny.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "brine is not water — no WetEffect (proves the OR clause, not div #3).");
            var bElec = new ElectrifiedEffect(charge: 1.0f);
            briny.ApplyEffect(bElec); // OnApply must double charge via conductive coat
            bElec.OnTurnStart(briny, TickCtx());
            int brinyDmg = 200 - briny.GetStatValue("Hitpoints");

            Assert.Greater(brinyDmg, dryDmg,
                "A conductive coat must double Electrified charge like WetEffect does.");
        }

        // ── Counter-checks ──────────────────────────────────────

        [Test]
        public void DryCreature_NoLightningAmplification()
        {
            var bare = MakeCreature();
            int dmg = Hit(bare, 20, "Lightning");
            Assert.AreEqual(20, dmg,
                "No coat → OnBeforeTakeDamage never runs → Lightning unmodified.");
        }

        [Test]
        public void WaterCoat_NonElementalDamage_Unchanged()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int dmg = Hit(coated, 25, "Bludgeoning");
            Assert.AreEqual(25, dmg,
                "Water coat must not touch non-Lightning, non-Fire damage.");
        }

        [Test]
        public void WaterCoat_DoesNotTickDamage_OnTurnStart()
        {
            // Counter to AcidCoat_TicksAcidDamage: water has no
            // PerTurnDamage so OnTurnStart is damage-silent.
            var c = MakeCreature();
            var coat = new LiquidCoveredEffect("water", 30);
            c.ApplyEffect(coat);
            int before = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, TickCtx());
            Assert.AreEqual(before, c.GetStatValue("Hitpoints"),
                "Water coat must NOT tick damage (no PerTurnDamage).");
        }

        [Test]
        public void Fire_OnWater_Reduced_AND_OnOil_Increased()
        {
            // Plan-mandated: both opposite Fire branches in one test.
            var water = MakeCreature();
            water.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int waterDmg = Hit(water, 40, "Fire");

            var oil = MakeCreature();
            oil.ApplyEffect(new LiquidCoveredEffect("oil", 30));
            int oilDmg = Hit(oil, 40, "Fire");

            var bare = MakeCreature();
            int bareDmg = Hit(bare, 40, "Fire");

            Assert.Less(waterDmg, bareDmg, "Water dampens Fire.");
            Assert.Greater(oilDmg, bareDmg, "Oil amplifies Fire.");
        }

        [Test]
        public void ElectrifiedPlusWaterCoat_DoesNotDoubleAmplify()
        {
            // Divergence #6: with an ElectrifiedEffect present,
            // LiquidCovered must NOT additionally multiply the
            // Electrified tick's Lightning damage. The water coat still
            // applies WetEffect (div #3) so OnApply doubles charge —
            // that single doubling is the ONLY amplification. Compare
            // against wet-but-uncoated: must be equal (not 2× larger).
            var wetOnly = MakeCreature();
            wetOnly.ApplyEffect(new WetEffect(1.0f));
            var e1 = new ElectrifiedEffect(charge: 2.0f);
            wetOnly.ApplyEffect(e1);
            e1.OnTurnStart(wetOnly, TickCtx());
            int wetOnlyDmg = 200 - wetOnly.GetStatValue("Hitpoints");

            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30)); // → WetEffect too
            var e2 = new ElectrifiedEffect(charge: 2.0f);
            coated.ApplyEffect(e2);
            e2.OnTurnStart(coated, TickCtx());
            int coatedDmg = 200 - coated.GetStatValue("Hitpoints");

            Assert.AreEqual(wetOnlyDmg, coatedDmg,
                "Electrified+watercoat must equal electrified+wet — no double-amplify (div #6).");
        }

        // ── Adversarial ─────────────────────────────────────────

        [Test]
        public void OnBeforeTakeDamage_ZeroAmount_NoOp()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int dmg = Hit(coated, 0, "Lightning");
            Assert.AreEqual(0, dmg, "Zero-amount damage stays zero (no phantom amplification).");
        }

        [Test]
        public void NegativeFireDampen_Ignored_FireUnchanged()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("weakdampen", 30));
            int coatedDmg = Hit(coated, 30, "Fire");
            Assert.AreEqual(30, coatedDmg,
                "Negative FireDampen must be ignored (no Fire change, no heal).");
        }

        [Test]
        public void FullDampen_NeverHeals_ClampedAtZero()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("fulldampen", 30));
            int dmg = Hit(coated, 50, "Fire");
            Assert.GreaterOrEqual(dmg, 0,
                "FireDampen 100 fully absorbs but must never heal (Amount clamps ≥ 0).");
            Assert.LessOrEqual(dmg, 1,
                "FireDampen 100 → ~zero Fire damage.");
        }

        [Test]
        public void RegistryUninitialized_OnBeforeTakeDamage_NoCrash()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            LiquidRegistry.ResetForTests(); // def now unresolvable
            int dmg = 0;
            Assert.DoesNotThrow(() => dmg = Hit(coated, 20, "Lightning"));
            Assert.AreEqual(20, dmg,
                "No def → no modification, no crash (graceful degradation).");
        }
    }
}
