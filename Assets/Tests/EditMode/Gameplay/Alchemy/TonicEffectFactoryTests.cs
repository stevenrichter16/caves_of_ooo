using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// TonicEffectFactory — the canonical name→Effect table shared by
    /// StatusTonicPart and BrewItemPart. Includes the M1.1 cold-eye F5 pin:
    /// every effect name the production brew rules can emit must actually be
    /// dispatchable, so a rule author can't ship a name that silently
    /// resolves to nothing.
    /// </summary>
    public class TonicEffectFactoryTests
    {
        [TearDown]
        public void TearDown()
        {
            BrewRuleRegistry.ResetForTests();
        }

        [Test]
        public void Production_BrewRuleEffectNames_AreAllDispatchable()
        {
            // F5 pin. "Healing" is the one deliberate exception — it maps to
            // TonicPart.Healing dice in BrewingService, not to a status
            // effect (see BrewingService.HealingEffectName).
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.EnsureInitialized();

            var rules = BrewRuleRegistry.GetAllRules();
            Assert.Greater(rules.Count, 0, "production BrewRules.json must load.");

            foreach (BrewRule rule in rules)
            {
                if (string.Equals(rule.Effect, BrewingService.HealingEffectName,
                        System.StringComparison.OrdinalIgnoreCase))
                    continue;

                Effect effect = TonicEffectFactory.Create(rule.Effect, 0, "", 1f, null);
                Assert.IsNotNull(effect,
                    $"Brew rule '{rule.ID}' names effect '{rule.Effect}', which " +
                    "TonicEffectFactory cannot create — the rule would silently do nothing.");
            }
        }

        [Test]
        public void MagnitudeFlowsThrough_UnclampedFamily()
        {
            // First live Unity run (M3-L3) falsified the original version of
            // this test: it asserted Corrosion==3, but AcidicEffect's ctor
            // CLAMPS Corrosion to [0,1] by design (it's a coating fraction;
            // tick damage = 1 + floor(Corrosion*4), AcidicEffect.cs:23).
            // Magnitude flows UNCLAMPED only for the intensity-style effects:
            var burn = TonicEffectFactory.Create("Burning", 0, "", 3f, null) as BurningEffect;
            Assert.IsNotNull(burn);
            Assert.AreEqual(3f, burn.Intensity, 0.001f, "Burning intensity is unclamped");

            var shock = TonicEffectFactory.Create("Electrified", 0, "", 3f, null) as ElectrifiedEffect;
            Assert.IsNotNull(shock);
            Assert.AreEqual(3f, shock.Charge, 0.001f, "Electrified charge is unclamped above 0");
        }

        [Test]
        public void Magnitude_CoatingFractionFamily_SaturatesAtFullCoating()
        {
            // Counter-pin to the above: Acidic/Wet/Frozen magnitudes are 0..1
            // coating fractions — below 1 the magnitude flows, above 1 it
            // saturates at full coating. Brew potency (integer ≥1) therefore
            // always means "fully coated" for these effects; that is the
            // designed ceiling, not a plumbing bug.
            var acidLow = TonicEffectFactory.Create("Acidic", 0, "", 0.5f, null) as AcidicEffect;
            Assert.IsNotNull(acidLow);
            Assert.AreEqual(0.5f, acidLow.Corrosion, 0.001f, "sub-1 magnitude flows through");

            var acidHigh = TonicEffectFactory.Create("Acidic", 0, "", 3f, null) as AcidicEffect;
            Assert.IsNotNull(acidHigh);
            Assert.AreEqual(1f, acidHigh.Corrosion, 0.001f,
                "magnitude 3 saturates at the designed full-coating clamp (AcidicEffect.cs:23)");
        }

        [Test]
        public void UnknownAndNullNames_ReturnNull()
        {
            Assert.IsNull(TonicEffectFactory.Create("UltraMegaQuantumBurst", 0, "", 1f, null));
            Assert.IsNull(TonicEffectFactory.Create(null, 0, "", 1f, null));
            Assert.IsNull(TonicEffectFactory.Create("   ", 0, "", 1f, null));
        }

        [Test]
        public void NameMatching_IsTrimmedAndCaseInsensitive()
        {
            // Same contract StatusTonicPart had before the extraction —
            // pinned so the refactor can't have changed it.
            Assert.IsInstanceOf<BurningEffect>(TonicEffectFactory.Create("  FIRE  ", 0, "", 1f, null));
            Assert.IsInstanceOf<FrozenEffect>(TonicEffectFactory.Create("frost", 0, "", 1f, null));
        }
    }
}
