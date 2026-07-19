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
        public void MagnitudeFlowsThrough()
        {
            var acid = TonicEffectFactory.Create("Acidic", 0, "", 3f, null) as AcidicEffect;

            Assert.IsNotNull(acid);
            Assert.AreEqual(3f, acid.Corrosion, 0.001f);
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
