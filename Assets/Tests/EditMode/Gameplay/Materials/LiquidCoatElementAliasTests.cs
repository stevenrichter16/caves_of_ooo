using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Regression for the element-alias bug surfaced while building the
    /// spell test bench: LQ.5 <c>LiquidCoveredEffect.OnBeforeTakeDamage</c>
    /// detected elements with literal <c>HasAttribute("Lightning")</c> /
    /// <c>HasAttribute("Fire")</c>, but every real spell/weapon tags the
    /// canonical aliases (<c>ArcBolt → "Electric"</c>,
    /// <c>Conflagration → "Heat"</c>, <c>IceSword → "Ice"</c>). The
    /// damage layer uses an alias-collapsing flag system and the
    /// canonical detector is <see cref="Damage.IsHeatDamage"/> /
    /// <see cref="Damage.IsElectricDamage"/> (this is exactly how
    /// <c>CombatSystem.ApplyResistances:983-985</c> routes resistance).
    ///
    /// These tests are RED on the pre-fix string-match impl: an
    /// "Electric"/"Heat"-tagged hit on a coated creature is NOT modified
    /// because the literal strings don't match. They go GREEN once the
    /// hook switches to the flag predicates. Pre-fix LQ.5 tests stay
    /// green because "Lightning"/"Fire" collapse to the same flags.
    /// </summary>
    public class LiquidCoatElementAliasTests
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
                  ""Combustibility"": 90, ""Fluidity"": 5, ""Evaporativity"": 2 }
              ]
            }");
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static Entity MakeCreature(int hp = 400)
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

        // ── The bug: real spell aliases must interact ──────────────

        [Test]
        public void WaterCoat_Amplifies_ElectricAlias_LikeArcBolt()
        {
            // ArcBoltMutation tags "Electric", NOT "Lightning".
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int coatedDmg = Hit(coated, 20, "Electric");
            int bareDmg = Hit(MakeCreature(), 20, "Electric");
            Assert.Greater(coatedDmg, bareDmg,
                "Water coat must amplify \"Electric\"-tagged damage (ArcBolt), not just literal \"Lightning\".");
        }

        [Test]
        public void WaterCoat_Dampens_HeatAlias_LikeConflagration()
        {
            // ConflagrationMutation deals "Heat", NOT "Fire".
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            int coatedDmg = Hit(coated, 40, "Heat");
            int bareDmg = Hit(MakeCreature(), 40, "Heat");
            Assert.Less(coatedDmg, bareDmg,
                "Water coat must dampen \"Heat\"-tagged damage (Conflagration), not just literal \"Fire\".");
            Assert.Greater(coatedDmg, 0, "partial dampen, not immunity");
        }

        [Test]
        public void OilCoat_Amplifies_HeatAlias()
        {
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("oil", 30));
            int coatedDmg = Hit(coated, 20, "Heat");
            int bareDmg = Hit(MakeCreature(), 20, "Heat");
            Assert.Greater(coatedDmg, bareDmg,
                "Oil coat must amplify \"Heat\"-tagged damage (real fire spells/weapons).");
        }

        [Test]
        public void WaterCoat_Amplifies_ShockAndElectricityAliases()
        {
            foreach (var alias in new[] { "Shock", "Electricity" })
            {
                var coated = MakeCreature();
                coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
                int coatedDmg = Hit(coated, 20, alias);
                int bareDmg = Hit(MakeCreature(), 20, alias);
                Assert.Greater(coatedDmg, bareDmg,
                    $"\"{alias}\" collapses to the Electric flag and must amplify on a water coat.");
            }
        }

        // ── Backward-compat: literal strings still work ────────────

        [Test]
        public void LiteralLightningAndFire_StillWork_AfterFlagSwitch()
        {
            // "Lightning"→Electric flag, "Fire"→Heat flag, so the pre-fix
            // LQ.5 suite (which used the literals) stays green.
            var w1 = MakeCreature(); w1.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.Greater(Hit(w1, 20, "Lightning"), Hit(MakeCreature(), 20, "Lightning"),
                "literal \"Lightning\" still amplifies (alias-collapses to Electric flag)");
            var w2 = MakeCreature(); w2.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.Less(Hit(w2, 40, "Fire"), Hit(MakeCreature(), 40, "Fire"),
                "literal \"Fire\" still dampens (alias-collapses to Heat flag)");
        }

        // ── Counter-checks ────────────────────────────────────────

        [Test]
        public void WaterCoat_ColdAlias_NotTouchedByHeatOrElectricBranch()
        {
            // Cold damage must not be amplified/damped by the water coat
            // (no Cold knob on LiquidDefinition; cold interaction is via
            // LQ.6 ColdResistance only). Counter-check that a buggy
            // over-broad flag check would fail.
            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.AreEqual(25, Hit(coated, 25, "Ice"),
                "Water coat must NOT modify Cold/Ice damage.");
        }

        [Test]
        public void ElectricAlias_WithElectrifiedPresent_StillYields_Div6()
        {
            // Divergence #6 must survive the alias fix: with an
            // ElectrifiedEffect present, an "Electric" hit is NOT
            // additionally amplified by the coat.
            var wetOnly = MakeCreature();
            wetOnly.ApplyEffect(new WetEffect(1.0f));
            var e1 = new ElectrifiedEffect(2.0f);
            wetOnly.ApplyEffect(e1);
            e1.OnTurnStart(wetOnly, GameEvent.New("BeginTakeAction"));
            int wetOnlyDmg = 400 - wetOnly.GetStatValue("Hitpoints");

            var coated = MakeCreature();
            coated.ApplyEffect(new LiquidCoveredEffect("water", 30)); // → WetEffect
            var e2 = new ElectrifiedEffect(2.0f);
            coated.ApplyEffect(e2);
            e2.OnTurnStart(coated, GameEvent.New("BeginTakeAction"));
            int coatedDmg = 400 - coated.GetStatValue("Hitpoints");

            Assert.AreEqual(wetOnlyDmg, coatedDmg,
                "Div #6 holds under the alias fix: Electrified owns electric amp, coat yields.");
        }
    }
}
