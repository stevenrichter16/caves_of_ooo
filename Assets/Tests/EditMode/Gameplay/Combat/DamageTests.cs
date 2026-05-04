using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase C of the Qud-parity port: tests that pin the expected behavior of
    /// the new <see cref="Damage"/> class, mirroring <c>XRL.World.Damage</c>.
    ///
    /// These tests are written RED-first (before implementation). They define
    /// the contract that `Damage.cs` must satisfy.
    ///
    /// Scope:
    ///   • Constructor + Amount clamp (≥ 0)
    ///   • Attributes list semantics (Add, Has, HasAny)
    ///   • String attribute parser (AddAttributes with space-separated input)
    ///   • Damage-type alias helpers (IsColdDamage, IsHeatDamage, etc.)
    /// </summary>
    public class DamageTests
    {
        // ====================================================================
        // Constructor + Amount clamp
        // ====================================================================

        [Test]
        public void Constructor_SetsAmount()
        {
            var d = new Damage(7);
            Assert.AreEqual(7, d.Amount);
        }

        [Test]
        public void Constructor_NegativeAmount_ClampsToZero()
        {
            // Mirroring Qud: Amount setter does Math.Max(value, 0).
            var d = new Damage(-5);
            Assert.AreEqual(0, d.Amount, "Negative initial amount must clamp to 0");
        }

        [Test]
        public void Amount_Setter_NegativeValue_ClampsToZero()
        {
            var d = new Damage(10);
            d.Amount = -3;
            Assert.AreEqual(0, d.Amount, "Negative assignment must clamp to 0");
        }

        [Test]
        public void Amount_Setter_PositiveValue_PassesThrough()
        {
            var d = new Damage(0);
            d.Amount = 12;
            Assert.AreEqual(12, d.Amount);
        }

        // ====================================================================
        // Attributes list semantics
        // ====================================================================

        [Test]
        public void AddAttribute_Appends_HasAttributeReturnsTrue()
        {
            var d = new Damage(1);
            d.AddAttribute("Cutting");
            Assert.IsTrue(d.HasAttribute("Cutting"));
        }

        [Test]
        public void HasAttribute_Unmatched_ReturnsFalse()
        {
            var d = new Damage(1);
            d.AddAttribute("Fire");
            Assert.IsFalse(d.HasAttribute("Cold"));
        }

        [Test]
        public void HasAnyAttribute_Intersection_ReturnsTrue()
        {
            var d = new Damage(1);
            d.AddAttribute("Acid");
            d.AddAttribute("Melee");
            var query = new List<string> { "Cold", "Acid", "Heat" };
            Assert.IsTrue(d.HasAnyAttribute(query));
        }

        [Test]
        public void HasAnyAttribute_Disjoint_ReturnsFalse()
        {
            var d = new Damage(1);
            d.AddAttribute("Acid");
            var query = new List<string> { "Cold", "Heat" };
            Assert.IsFalse(d.HasAnyAttribute(query));
        }

        [Test]
        public void HasAnyAttribute_NullQuery_ReturnsFalse()
        {
            var d = new Damage(1);
            d.AddAttribute("Acid");
            Assert.IsFalse(d.HasAnyAttribute(null));
        }

        [Test]
        public void AddAttribute_Same_TwiceAppendsTwice()
        {
            // Qud's AddAttribute does NOT dedupe — it just appends. Mirror that.
            var d = new Damage(1);
            d.AddAttribute("Fire");
            d.AddAttribute("Fire");
            Assert.AreEqual(2, d.Attributes.Count);
        }

        // ====================================================================
        // AddAttributes (space-separated string parser)
        // ====================================================================

        [Test]
        public void AddAttributes_SpaceSeparated_AddsEach()
        {
            var d = new Damage(1);
            d.AddAttributes("Cutting LongBlades Strength");
            Assert.IsTrue(d.HasAttribute("Cutting"));
            Assert.IsTrue(d.HasAttribute("LongBlades"));
            Assert.IsTrue(d.HasAttribute("Strength"));
        }

        [Test]
        public void AddAttributes_SingleWord_AddsAsOneAttribute()
        {
            var d = new Damage(1);
            d.AddAttributes("Acid");
            Assert.IsTrue(d.HasAttribute("Acid"));
            Assert.AreEqual(1, d.Attributes.Count);
        }

        [Test]
        public void AddAttributes_NullOrEmpty_NoEffect()
        {
            var d = new Damage(1);
            d.AddAttributes(null);
            d.AddAttributes("");
            Assert.AreEqual(0, d.Attributes.Count);
        }

        // ====================================================================
        // Damage-type alias helpers
        // ====================================================================

        [Test]
        public void IsColdDamage_MatchesAliasAttributes()
        {
            // Cold OR Ice OR Freeze → cold damage
            var coldDmg = new Damage(1); coldDmg.AddAttribute("Cold");
            var iceDmg = new Damage(1); iceDmg.AddAttribute("Ice");
            var freezeDmg = new Damage(1); freezeDmg.AddAttribute("Freeze");
            var heatDmg = new Damage(1); heatDmg.AddAttribute("Fire");

            Assert.IsTrue(coldDmg.IsColdDamage());
            Assert.IsTrue(iceDmg.IsColdDamage());
            Assert.IsTrue(freezeDmg.IsColdDamage());
            Assert.IsFalse(heatDmg.IsColdDamage());
        }

        [Test]
        public void IsHeatDamage_MatchesFireOrHeat()
        {
            var fire = new Damage(1); fire.AddAttribute("Fire");
            var heat = new Damage(1); heat.AddAttribute("Heat");
            var cold = new Damage(1); cold.AddAttribute("Cold");

            Assert.IsTrue(fire.IsHeatDamage());
            Assert.IsTrue(heat.IsHeatDamage());
            Assert.IsFalse(cold.IsHeatDamage());
        }

        [Test]
        public void IsElectricDamage_MatchesAllElectricAliases()
        {
            // Electric, Shock, Lightning, Electricity all qualify
            foreach (var alias in new[] { "Electric", "Shock", "Lightning", "Electricity" })
            {
                var d = new Damage(1);
                d.AddAttribute(alias);
                Assert.IsTrue(d.IsElectricDamage(), $"'{alias}' must be recognized as electric damage");
            }
            var poison = new Damage(1); poison.AddAttribute("Poison");
            Assert.IsFalse(poison.IsElectricDamage());
        }

        [Test]
        public void IsBludgeoningDamage_MatchesCudgelOrBludgeoning()
        {
            var cudgel = new Damage(1); cudgel.AddAttribute("Cudgel");
            var blunt = new Damage(1); blunt.AddAttribute("Bludgeoning");
            var cutting = new Damage(1); cutting.AddAttribute("Cutting");

            Assert.IsTrue(cudgel.IsBludgeoningDamage());
            Assert.IsTrue(blunt.IsBludgeoningDamage());
            Assert.IsFalse(cutting.IsBludgeoningDamage());
        }

        [Test]
        public void IsAcidDamage_MatchesAcid()
        {
            var acid = new Damage(1); acid.AddAttribute("Acid");
            var fire = new Damage(1); fire.AddAttribute("Fire");

            Assert.IsTrue(acid.IsAcidDamage());
            Assert.IsFalse(fire.IsAcidDamage());
        }

        [Test]
        public void IsLightDamage_MatchesLightOrLaser()
        {
            var light = new Damage(1); light.AddAttribute("Light");
            var laser = new Damage(1); laser.AddAttribute("Laser");
            var fire = new Damage(1); fire.AddAttribute("Fire");

            Assert.IsTrue(light.IsLightDamage());
            Assert.IsTrue(laser.IsLightDamage());
            Assert.IsFalse(fire.IsLightDamage());
        }

        [Test]
        public void IsDisintegrationDamage_MatchesDisintegrateOrDisintegration()
        {
            var d1 = new Damage(1); d1.AddAttribute("Disintegrate");
            var d2 = new Damage(1); d2.AddAttribute("Disintegration");
            var fire = new Damage(1); fire.AddAttribute("Fire");

            Assert.IsTrue(d1.IsDisintegrationDamage());
            Assert.IsTrue(d2.IsDisintegrationDamage());
            Assert.IsFalse(fire.IsDisintegrationDamage());
        }

        [Test]
        public void TypeChecks_OnEmptyAttributes_AllReturnFalse()
        {
            var d = new Damage(1);
            Assert.IsFalse(d.IsColdDamage());
            Assert.IsFalse(d.IsHeatDamage());
            Assert.IsFalse(d.IsElectricDamage());
            Assert.IsFalse(d.IsBludgeoningDamage());
            Assert.IsFalse(d.IsAcidDamage());
            Assert.IsFalse(d.IsLightDamage());
            Assert.IsFalse(d.IsDisintegrationDamage());
        }
    }
}
