using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M1.1 — pure brew-resolution core. Each positive assertion is paired
    /// with a counter-check (§3.4): an identical-shaped setup with the
    /// triggering property removed/changed that must NOT produce the effect,
    /// so "the test passes" can't mean "the precondition was vacuous."
    ///
    /// Uses an embedded deterministic rule set via InitializeFromJson so the
    /// unit behavior is pinned independent of the shipped production JSON.
    /// One separate test exercises the production auto-load.
    /// </summary>
    public class BrewResolverTests
    {
        // Deterministic test rule table. Mirrors the production shape but is
        // self-contained so resolution semantics are pinned here.
        private const string TestRulesJson = @"{
            ""Rules"": [
                { ""ID"":""r_burn"",  ""RequireAll"":""heat combustible"", ""Effect"":""Burning"",     ""Form"":""Coating"", ""Priority"":10 },
                { ""ID"":""r_acid"",  ""RequireAll"":""corrosive"",        ""Effect"":""Acidic"",      ""Form"":""Tonic"",   ""Priority"":5 },
                { ""ID"":""r_shock"", ""RequireAll"":""conductive"",       ""Effect"":""Electrified"", ""Form"":""Tonic"",   ""Priority"":5 },
                { ""ID"":""r_frost"", ""RequireAll"":""cold"", ""ForbidAny"":""heat"", ""Effect"":""Frozen"", ""Form"":""Coating"", ""Priority"":5 }
            ]
        }";

        [SetUp]
        public void Setup()
        {
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.InitializeFromJson(TestRulesJson);
        }

        [TearDown]
        public void TearDown()
        {
            // Leave the registry clean-but-uninitialized so the next caller
            // (test or live Play) gets a fresh auto-load from production JSON.
            BrewRuleRegistry.ResetForTests();
        }

        private static IReadOnlyList<BrewPropertyAmount> Reagent(params (string prop, int potency)[] props)
        {
            var list = new List<BrewPropertyAmount>(props.Length);
            foreach (var p in props)
                list.Add(new BrewPropertyAmount(p.prop, p.potency));
            return list;
        }

        // ════════════════ Single + combinatorial reactions ════════════════

        [Test]
        public void HeatPlusCombustible_ProducesBurning()
        {
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 2)),
                Reagent(("combustible", 3)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Burning"), "heat + combustible must ignite.");
        }

        [Test]
        public void HeatAlone_DoesNotProduceBurning()
        {
            // Counter-check to HeatPlusCombustible: drop the combustible and
            // the same heat reagent must NOT brew fire (no other rule matches,
            // no volatile → inert sludge).
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 2)));

            Assert.IsFalse(result.HasEffect("Burning"), "heat without fuel must not ignite.");
            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
        }

        [Test]
        public void CombustibleAlone_DoesNotProduceBurning()
        {
            // The other half of the counter-check pair.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("combustible", 3)));

            Assert.IsFalse(result.HasEffect("Burning"));
            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
        }

        [Test]
        public void CorrosivePlusConductive_ProducesBothAcidAndShock()
        {
            // Emergent multi-effect: two independent rules fire on one mix.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("corrosive", 2)),
                Reagent(("conductive", 1)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Acidic"), "corrosive contributes acid.");
            Assert.IsTrue(result.HasEffect("Electrified"), "conductive contributes shock.");
            Assert.AreEqual(2, result.Effects.Count, "exactly the two matched effects, no extras.");
        }

        // ════════════════ Forbid / veto semantics ════════════════

        [Test]
        public void ColdAlone_Freezes()
        {
            BrewResult result = BrewResolver.Resolve(Reagent(("cold", 2)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Frozen"));
        }

        [Test]
        public void ColdWithHeat_DoesNotFreeze()
        {
            // Counter-check: the frost rule forbids heat. Adding heat (with no
            // combustible, so it can't burn either) vetoes the freeze → no
            // effect fires.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("cold", 2)),
                Reagent(("heat", 1)));

            Assert.IsFalse(result.HasEffect("Frozen"), "heat must veto the frost rule.");
            Assert.IsFalse(result.HasEffect("Burning"), "no combustible, so no fire either.");
            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
        }

        // ════════════════ Potency = MAX, count-insensitive (§6.4) ════════════════

        [Test]
        public void Potency_IsMaxOfRequiredProperties_NotSummed()
        {
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 3)),
                Reagent(("combustible", 1)));

            // max(heat=3, combustible=1) = 3
            Assert.AreEqual(3, FindPotency(result, "Burning"));
        }

        [Test]
        public void Potency_DoesNotScaleWithDuplicateReagents()
        {
            // Three weak reagents must NOT out-potency-scale one strong one.
            BrewResult stacked = BrewResolver.Resolve(
                Reagent(("heat", 1), ("combustible", 1)),
                Reagent(("heat", 1), ("combustible", 1)),
                Reagent(("heat", 1), ("combustible", 1)));

            BrewResult single = BrewResolver.Resolve(
                Reagent(("heat", 1), ("combustible", 1)));

            Assert.AreEqual(1, FindPotency(stacked, "Burning"), "stacking duplicates must not raise potency.");
            Assert.AreEqual(FindPotency(single, "Burning"), FindPotency(stacked, "Burning"));
        }

        // ════════════════ Failure outcomes — never silent (§6.3) ════════════════

        [Test]
        public void VolatileWithNoReaction_IsTelegraphedMishap()
        {
            BrewResult result = BrewResolver.Resolve(Reagent(("volatile", 2)));

            Assert.AreEqual(BrewOutcomeKind.Mishap, result.Kind);
            Assert.IsNotEmpty(result.Reason, "a mishap must explain itself, never fizzle silently.");
        }

        [Test]
        public void NonReactingNonVolatile_IsInertSludge()
        {
            // "bitter" has no rule and isn't volatile → inert, not a mishap.
            BrewResult result = BrewResolver.Resolve(Reagent(("bitter", 2)));

            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
            Assert.IsNotEmpty(result.Reason);
        }

        [Test]
        public void NoReagents_IsInvalid()
        {
            BrewResult result = BrewResolver.Resolve(new IReadOnlyList<BrewPropertyAmount>[0]);

            Assert.AreEqual(BrewOutcomeKind.Invalid, result.Kind);
        }

        // ════════════════ Form resolution ════════════════

        [Test]
        public void Form_TakesHighestPriorityMatchedRule()
        {
            // r_burn (priority 10, Coating) beats r_acid (priority 5, Tonic)
            // when both fire.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 1), ("combustible", 1)),
                Reagent(("corrosive", 1)));

            Assert.AreEqual("Coating", result.Form);
        }

        [Test]
        public void Form_VolatileOverridesToThrowable()
        {
            // A reacting brew that is ALSO volatile becomes throwable
            // regardless of the matched rule's declared form.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 2), ("volatile", 1)),
                Reagent(("combustible", 2)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Burning"));
            Assert.AreEqual("Throwable", result.Form, "volatile must override the form to Throwable.");
        }

        // ════════════════ Robustness pins ════════════════

        [Test]
        public void NullAndEmptyProperties_AreIgnored()
        {
            BrewResult result = BrewResolver.Resolve(
                Reagent((null, 2), ("", 3), ("heat", 2)),
                Reagent(("combustible", 2)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Burning"), "garbage properties must be skipped, valid ones still react.");
        }

        [Test]
        public void ZeroOrNegativePotency_DoesNotContribute()
        {
            // heat present but at potency 0 → not a real property → no burn.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 0)),
                Reagent(("combustible", 2)));

            Assert.IsFalse(result.HasEffect("Burning"));
        }

        [Test]
        public void PropertyMatching_IsCaseInsensitive()
        {
            BrewResult result = BrewResolver.Resolve(
                Reagent(("HEAT", 2)),
                Reagent(("Combustible", 2)));

            Assert.IsTrue(result.HasEffect("Burning"));
        }

        // ════════════════ M1.1 cold-eye findings, pinned ════════════════

        [Test]
        public void Registry_DuplicateRuleId_LastDefinitionWins_InBothAccessPaths()
        {
            // Cold-eye F1: with duplicate IDs, TryGetRule served the LAST
            // definition while GetAllRules served the FIRST — two public
            // surfaces disagreeing about what the rule is. Pin last-wins in
            // BOTH paths.
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [
                    { ""ID"":""r_dup"", ""RequireAll"":""heat"", ""Effect"":""Burning"", ""Form"":""Tonic"", ""Priority"":1 },
                    { ""ID"":""r_dup"", ""RequireAll"":""heat"", ""Effect"":""Frozen"",  ""Form"":""Tonic"", ""Priority"":1 }
                ]
            }");

            Assert.IsTrue(BrewRuleRegistry.TryGetRule("r_dup", out BrewRule byId));
            Assert.AreEqual("Frozen", byId.Effect, "TryGetRule must serve the last definition.");

            var all = BrewRuleRegistry.GetAllRules();
            Assert.AreEqual(1, all.Count, "duplicate IDs must not produce two iteration entries.");
            Assert.AreEqual("Frozen", all[0].Effect, "GetAllRules must serve the SAME definition as TryGetRule.");
        }

        [Test]
        public void Form_EqualPriority_FirstListedRuleWins()
        {
            // Cold-eye F4: '>=' made the LAST-listed rule win priority ties,
            // which no author would predict from reading the JSON. Pin
            // first-listed-wins (file order = precedence order).
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [
                    { ""ID"":""r_first"",  ""RequireAll"":""heat"", ""Effect"":""Burning"",     ""Form"":""Coating"", ""Priority"":5 },
                    { ""ID"":""r_second"", ""RequireAll"":""heat"", ""Effect"":""Electrified"", ""Form"":""Tonic"",   ""Priority"":5 }
                ]
            }");

            BrewResult result = BrewResolver.Resolve(Reagent(("heat", 2)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.AreEqual("Coating", result.Form,
                "On a priority tie, the first-listed rule's form must win.");
        }

        [Test]
        public void Effects_CarryTheMatchedRuleId()
        {
            // Discovery is keyed on rule ID (learn the RULE, not the reagent
            // pair) — the resolver must report which rule contributed each
            // effect.
            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 2)),
                Reagent(("combustible", 3)));

            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.AreEqual(1, result.Effects.Count);
            Assert.AreEqual("r_burn", result.Effects[0].RuleId);
        }

        // ════════════════ Production content load ════════════════

        [Test]
        public void Production_BrewRules_LoadAndResolveFire()
        {
            // Exercise the shipped JSON (not the embedded test set).
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.EnsureInitialized();

            Assert.IsTrue(BrewRuleRegistry.TryGetRule("brew_burning", out _),
                "production BrewRules.json must define brew_burning.");

            BrewResult result = BrewResolver.Resolve(
                Reagent(("heat", 2)),
                Reagent(("combustible", 2)));
            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsTrue(result.HasEffect("Burning"));
        }

        private static int FindPotency(BrewResult result, string effect)
        {
            for (int i = 0; i < result.Effects.Count; i++)
            {
                if (string.Equals(result.Effects[i].Effect, effect, System.StringComparison.OrdinalIgnoreCase))
                    return result.Effects[i].Potency;
            }

            return -1;
        }
    }
}
