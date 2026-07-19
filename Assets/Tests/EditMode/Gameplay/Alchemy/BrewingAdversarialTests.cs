using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Dedicated adversarial sweep for the alchemy system (CLAUDE.md
    /// §Adversarial test sweep — alchemy touches 4+ taxonomy surfaces:
    /// state atomicity, parser, stacking semantics, diag dispatch
    /// contracts). Each test names the bug class it probes and why a buggy
    /// implementation would fail it.
    ///
    /// This sweep already caught ONE real latent bug during authoring:
    /// TonicPart.HasThrowablePayload didn't know about BrewItemPart, so a
    /// volatile "Throwable"-form brew landed like an inert rock instead of
    /// shattering. Fixed in the same commit; pinned in the THROWABLE
    /// section below.
    /// </summary>
    public class BrewingAdversarialTests
    {
        private const string TestRulesJson = @"{
            ""Rules"": [
                { ""ID"":""r_burn"", ""RequireAll"":""heat combustible"", ""Effect"":""Burning"", ""Form"":""Coating"", ""Priority"":10 },
                { ""ID"":""r_acid"", ""RequireAll"":""corrosive"", ""Effect"":""Acidic"", ""Form"":""Tonic"", ""Priority"":5 }
            ]
        }";

        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""item"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""BrewedTonic"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""strange brew"" } ] },
        { ""Name"": ""Tonic"", ""Params"": [ { ""Key"": ""Drink"", ""Value"": ""true"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""InertSludge"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""inert sludge"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""FireMoss"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""heat:2"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""LampOil"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""combustible:3"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""InertPebble"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""bitter:1"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""BlastPowder"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""volatile:2"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""EmptyReagent"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": """" } ] } ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        /// <summary>Fixture missing InertSludge — forces the sludge-creation rollback path.</summary>
        private const string BlueprintsWithoutSludgeJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [ { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""InertPebble"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""bitter:1"" } ] } ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.InitializeFromJson(TestRulesJson);
        }

        [TearDown]
        public void TearDown()
        {
            BrewRuleRegistry.ResetForTests();
            Diag.ResetAll();
        }

        private static EntityFactory CreateFactory(string json = TestBlueprintsJson)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(json);
            return factory;
        }

        private static Entity CreateCrafter()
        {
            var crafter = new Entity { ID = "crafter", BlueprintName = "Player" };
            crafter.AddPart(new RenderPart { DisplayName = "crafter" });
            crafter.AddPart(new InventoryPart());
            return crafter;
        }

        private static Entity GiveItem(Entity crafter, EntityFactory factory, string blueprint)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static IReadOnlyList<BrewPropertyAmount> Props(params (string p, int n)[] pairs)
        {
            var list = new List<BrewPropertyAmount>();
            foreach (var (p, n) in pairs)
                list.Add(new BrewPropertyAmount(p, n));
            return list;
        }

        // ════════════════ PARSER — malformed raw strings ════════════════
        // Bug class: a parser that treats int.TryParse's out-0 as a value,
        // or crashes on garbage, silently corrupts brew inputs.

        [Test]
        public void Adversarial_Parser_DoubleColonEntry_Dropped()
        {
            // "heat:2:3" — potency substring "2:3" fails TryParse. A buggy
            // impl that split on ALL colons would read potency 2 and brew
            // fire from a malformed blueprint.
            var part = new ReagentPart { PropertiesRaw = "heat:2:3, cold:1" };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("cold", props[0].Property);
        }

        [Test]
        public void Adversarial_Parser_WhitespaceAroundColon_Parses()
        {
            var part = new ReagentPart { PropertiesRaw = "  heat : 2  " };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("heat", props[0].Property);
            Assert.AreEqual(2, props[0].Potency);
        }

        [Test]
        public void Adversarial_Parser_HugePotency_NoOverflowCrash()
        {
            // 2,000,000,000 fits int; resolver magnitude math must not
            // overflow into a negative potency.
            var part = new ReagentPart { PropertiesRaw = "heat:2000000000" };
            Assert.AreEqual(2000000000, part.GetProperties()[0].Potency);

            BrewResult result = BrewResolver.Resolve(
                part.GetProperties(), Props(("combustible", 1)));
            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.Greater(result.Effects[0].Potency, 0, "magnitude must not overflow negative.");
        }

        [Test]
        public void Adversarial_Parser_ExplicitPlusSign_Accepted_MinusDropped()
        {
            // int.TryParse accepts "+2"; "-2" parses but potency<=0 filters it.
            var part = new ReagentPart { PropertiesRaw = "heat:+2, cold:-2" };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("heat", props[0].Property);
        }

        [Test]
        public void Adversarial_Parser_UnicodePropertyName_InertNotCrash()
        {
            // Unknown non-ASCII property matches no rule — must degrade to
            // sludge, never throw.
            var part = new ReagentPart { PropertiesRaw = "héat:2" };
            BrewResult result = BrewResolver.Resolve(part.GetProperties());

            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
        }

        // ════════════════ RESOLVER — rule-table edge cases ════════════════
        // Bug class: rules with degenerate fields matching everything (or
        // crashing) turn typos in content into gameplay corruption.

        [Test]
        public void Adversarial_Resolver_RuleMissingRequireAllAndEffect_NeverFires()
        {
            BrewRuleRegistry.InitializeFromJson(@"{ ""Rules"": [ { ""ID"":""r_degenerate"" } ] }");

            BrewResult result = BrewResolver.Resolve(Props(("heat", 2)));

            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind,
                "a rule with no RequireAll/Effect must match nothing, not everything.");
        }

        [Test]
        public void Adversarial_Resolver_UnknownRequiredProperty_NeverFires()
        {
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [ { ""ID"":""r_x"", ""RequireAll"":""unobtainium"", ""Effect"":""Burning"", ""Form"":""Tonic"", ""Priority"":1 } ]
            }");

            Assert.AreEqual(BrewOutcomeKind.InertSludge,
                BrewResolver.Resolve(Props(("heat", 2))).Kind);
        }

        [Test]
        public void Adversarial_Resolver_MagnitudeScaleZero_FallsBackToOne()
        {
            // JsonUtility writes 0 for an explicit 0; the guard must treat
            // <=0 as 1, or every such rule emits potency-0 effects.
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [ { ""ID"":""r_z"", ""RequireAll"":""heat"", ""Effect"":""Burning"", ""Form"":""Tonic"", ""Priority"":1, ""MagnitudeScale"":0 } ]
            }");

            BrewResult result = BrewResolver.Resolve(Props(("heat", 3)));

            Assert.AreEqual(3, result.Effects[0].Potency);
        }

        [Test]
        public void Adversarial_Resolver_FractionalScale_RoundsAwayFromZero()
        {
            // potency 3 × 0.5 = 1.5 → 2 (AwayFromZero), floored at 1.
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [ { ""ID"":""r_h"", ""RequireAll"":""heat"", ""Effect"":""Burning"", ""Form"":""Tonic"", ""Priority"":1, ""MagnitudeScale"":0.5 } ]
            }");

            Assert.AreEqual(2, BrewResolver.Resolve(Props(("heat", 3))).Effects[0].Potency);
        }

        [Test]
        public void Adversarial_Resolver_ForbidAny_IsCaseInsensitive()
        {
            BrewRuleRegistry.InitializeFromJson(@"{
                ""Rules"": [ { ""ID"":""r_c"", ""RequireAll"":""cold"", ""ForbidAny"":""HEAT"", ""Effect"":""Frozen"", ""Form"":""Tonic"", ""Priority"":1 } ]
            }");

            BrewResult result = BrewResolver.Resolve(Props(("cold", 2), ("heat", 1)));

            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind,
                "veto matching must be case-insensitive like everything else.");
        }

        [Test]
        public void Adversarial_Resolver_EmptyRuleTable_SludgeNotCrash()
        {
            BrewRuleRegistry.InitializeFromJson(@"{ ""Rules"": [] }");

            Assert.AreEqual(BrewOutcomeKind.InertSludge,
                BrewResolver.Resolve(Props(("heat", 2))).Kind);
        }

        // ════════════════ ATOMICITY — rollback shapes ════════════════
        // Bug class: partial consumption surviving a failed brew silently
        // eats the player's reagents.

        [Test]
        public void Adversarial_Atomicity_SludgeBlueprintMissing_RestoresStackAndItem()
        {
            // Two consumption shapes in one rollback: a stacked reagent
            // (stack decrement) and a plain one (inventory removal). The
            // sludge blueprint is missing, so creation fails AFTER both were
            // consumed — the ledger must restore both shapes.
            var factory = CreateFactory(BlueprintsWithoutSludgeJson);
            var crafter = CreateCrafter();
            var stacked = GiveItem(crafter, factory, "InertPebble");
            stacked.AddPart(new StackerPart { StackCount = 3 });
            var plain = GiveItem(crafter, factory, "InertPebble");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { stacked, plain },
                out _, out _, out _);

            Assert.IsFalse(ok);
            Assert.AreEqual(3, stacked.GetPart<StackerPart>().StackCount,
                "stack-consumed reagent must be restored to its full count.");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(plain),
                "removed reagent must be restored to inventory.");
        }

        [Test]
        public void Adversarial_Atomicity_StackAtExactlyOne_ConsumedWhole()
        {
            // Boundary: StackCount==1 must take the RemoveObject path, not
            // decrement to a phantom 0-count stack.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            moss.AddPart(new StackerPart { StackCount = 1 });
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, oil },
                out _, out _, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(moss),
                "a 1-count stack is the whole item; it must leave the inventory.");
        }

        [Test]
        public void Adversarial_Atomicity_EmptyPropertyReagent_IsSludgeNotInvalid()
        {
            // A reagent whose PropertiesRaw parses to nothing still COUNTS
            // as a reagent — the mix resolves to sludge ("no usable
            // properties"), it is not a validation failure. Consumption
            // must happen.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var husk = GiveItem(crafter, factory, "EmptyReagent");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { husk },
                out Entity produced, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
            Assert.IsNotNull(produced);
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(husk));
        }

        // ════════════════ CROSS-INSTANCE — same blueprint, two entities ════════════════

        [Test]
        public void Adversarial_CrossInstance_TwoSameBlueprintReagents_BothConsumed_PotencyUnchanged()
        {
            // Two distinct FireMoss ENTITIES are legal (unlike the same
            // entity twice) — and per §6.4 the duplicate property must NOT
            // raise potency.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var mossA = GiveItem(crafter, factory, "FireMoss");
            var mossB = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { mossA, mossB, oil },
                out _, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsFalse(inventory.Contains(mossA));
            Assert.IsFalse(inventory.Contains(mossB));
            Assert.AreEqual(3, result.Effects[0].Potency,
                "max(heat:2, heat:2, combustible:3) = 3 — duplicates must not sum.");
        }

        // ════════════════ DIAG — dispatch contract invariants ════════════════
        // Bug class: wrong-kind emissions poison future "why didn't X
        // happen?" debugging (the WSP3-7 lesson in CLAUDE.md).

        [Test]
        public void Adversarial_Diag_Success_EmitsResolved_NeverRejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { moss, oil }, out _, out _, out _);

            Assert.AreEqual(1, Count("BrewResolved"));
            Assert.AreEqual(0, Count("BrewRejected"),
                "a successful brew must not also emit a rejection record.");
        }

        [Test]
        public void Adversarial_Diag_ValidationFailure_EmitsRejected_NeverResolved()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();

            BrewingService.TryBrew(crafter, factory, new List<Entity>(), out _, out _, out _);

            Assert.AreEqual(1, Count("BrewRejected"));
            Assert.AreEqual(0, Count("BrewResolved"),
                "a rejected brew never reached resolution — no Resolved record.");
        }

        [Test]
        public void Adversarial_Diag_MishapOutcome_InResolvedPayload()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var powder = GiveItem(crafter, factory, "BlastPowder");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { powder }, out _, out _, out _);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy", Kind = "BrewResolved", Limit = 5
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("Mishap", records[0].PayloadJson);
        }

        [Test]
        public void Adversarial_Diag_ChannelDisabled_BrewStillWorks_NoRecords()
        {
            // The diag layer is observability, never behavior: disabling the
            // channel must not change brew outcomes.
            Diag.SetChannel("alchemy", false);
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, oil },
                out Entity brew, out _, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsNotNull(brew);
            Assert.AreEqual(0, Count("BrewResolved"));
            Assert.AreEqual(0, Count("BrewDiscovered"));
        }

        // ════════════════ THROWABLE — the bug this sweep caught ════════════════

        [Test]
        public void Adversarial_Throwable_BrewItemPart_MakesTonicShatterable()
        {
            // THE latent bug: HasThrowablePayload didn't check BrewItemPart,
            // so a volatile Throwable-form brew landed like an inert rock.
            // ThrowItemCommand gates its whole shatter/AoE path on this.
            var brew = new Entity { ID = "b", BlueprintName = "BrewedTonic" };
            var tonic = new TonicPart();
            brew.AddPart(tonic);
            brew.AddPart(new BrewItemPart { EffectsRaw = "Burning:2", Form = "Throwable" });

            Assert.IsTrue(tonic.HasThrowablePayload(),
                "a status-effect brew is a shatterable payload; without this the thrown flask does nothing.");
        }

        [Test]
        public void Adversarial_Throwable_BareTonic_NotShatterable()
        {
            // Counter-check: an empty TonicPart (no healing, no boost, no
            // payload parts) must remain non-shatterable, or every thrown
            // bottle becomes an AoE.
            var item = new Entity { ID = "t", BlueprintName = "EmptyBottle" };
            var tonic = new TonicPart();
            item.AddPart(tonic);

            Assert.IsFalse(tonic.HasThrowablePayload());
        }

        [Test]
        public void Adversarial_Throwable_HealingBrew_ShatterableViaHealingDice()
        {
            // The healing-brew path (service writes TonicPart.Healing) was
            // already shatterable pre-fix; pin it so the fix can't have
            // regressed it.
            var brew = new Entity { ID = "h", BlueprintName = "BrewedTonic" };
            var tonic = new TonicPart { Healing = "2d4" };
            brew.AddPart(tonic);

            Assert.IsTrue(tonic.HasThrowablePayload());
        }

        // ════════════════ DISCOVERY — knowledge abuse ════════════════

        [Test]
        public void Adversarial_Discovery_PreexistingKnowledge_MergedNotClobbered()
        {
            // The service auto-attaches BrewKnowledgePart when missing; when
            // one EXISTS (old save), it must add to it, never replace it.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var existing = new BrewKnowledgePart();
            existing.Discover("r_acid");
            crafter.AddPart(existing);
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { moss, oil }, out _, out _, out _);

            var knowledge = crafter.GetPart<BrewKnowledgePart>();
            Assert.AreSame(existing, knowledge, "the service must reuse the attached part.");
            Assert.IsTrue(knowledge.Knows("r_acid"), "prior discoveries must survive.");
            Assert.IsTrue(knowledge.Knows("r_burn"), "the new discovery must be added.");
        }

        [Test]
        public void Adversarial_Discovery_SludgeAndMishap_DiscoverNothing()
        {
            // Only a real Brew outcome carries matched rules; failed
            // experiments must not teach phantom rules.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var pebble = GiveItem(crafter, factory, "InertPebble");
            var powder = GiveItem(crafter, factory, "BlastPowder");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { pebble }, out _, out _, out _);
            BrewingService.TryBrew(crafter, factory, new List<Entity> { powder }, out _, out _, out _);

            var knowledge = crafter.GetPart<BrewKnowledgePart>();
            if (knowledge != null)
                Assert.AreEqual(0, knowledge.GetDiscoveredRules().Count);
            Assert.AreEqual(0, Count("BrewDiscovered"));
        }

        // ════════════════ BREW ITEM — event robustness ════════════════

        [Test]
        public void Adversarial_BrewItem_ApplyTonicWithoutActor_NoCrash()
        {
            var brew = new Entity { ID = "b", BlueprintName = "BrewedTonic" };
            brew.AddPart(new BrewItemPart { EffectsRaw = "Burning:2" });

            var e = GameEvent.New("ApplyTonic");
            Assert.DoesNotThrow(() => brew.FireEventAndRelease(e));
        }

        [Test]
        public void Adversarial_BrewItem_EffectsRawMutatedBetweenUses_FreshEffectsApply()
        {
            // Cache-invalidation under live mutation: a stale cache would
            // apply the OLD effect list after EffectsRaw changed (the same
            // bug class as the MeleeWeaponPart raw-string caches guard).
            var brew = new Entity { ID = "b", BlueprintName = "BrewedTonic" };
            var part = new BrewItemPart { EffectsRaw = "Burning:2" };
            brew.AddPart(part);

            var first = MakeTarget("t1");
            FireApplyTonic(brew, first);
            Assert.IsTrue(first.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>());

            part.EffectsRaw = "Frozen:2";
            var second = MakeTarget("t2");
            FireApplyTonic(brew, second);

            var effects = second.GetPart<StatusEffectsPart>();
            Assert.IsTrue(effects.HasEffect<FrozenEffect>(), "the NEW effect list must apply.");
            Assert.IsFalse(effects.HasEffect<BurningEffect>(), "the OLD effect list must not.");
        }

        // ════════════════ CONTENT INTEGRITY — production data pins ════════════════
        // Bug class: content typos (a mis-spelled property or form) fail
        // silently at runtime; these pins turn them into test failures.

        private static readonly string[] ProductionReagentNames =
        {
            "FireMoss", "LampOil", "EmberFruit", "FrostLichen", "GlacierSalt",
            "GlimmerBrine", "SparkRoot", "VenomGland", "BogSap",
            "CandyHeartRoot", "MendleafSprig", "StoneburrSeed", "BlastcapSpore"
        };

        private static readonly HashSet<string> KnownProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BrewProperties.Heat, BrewProperties.Cold, BrewProperties.Combustible,
            BrewProperties.Conductive, BrewProperties.Corrosive, BrewProperties.Volatile,
            BrewProperties.Viscous, BrewProperties.Vital, BrewProperties.Toxic,
            BrewProperties.Bitter, BrewProperties.Sweet, BrewProperties.Luminous,
            BrewProperties.Numbing, BrewProperties.Binding
        };

        [Test]
        public void Adversarial_Content_ProductionReagents_ParseToKnownVocabulary()
        {
            // A typo like "vollatile:2" in Objects.json would parse fine and
            // then match no rule forever — invisible at runtime, caught here.
            TextAsset asset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(asset, "production Objects.json must be loadable.");
            var factory = new EntityFactory();
            factory.LoadBlueprints(asset.text);

            foreach (string name in ProductionReagentNames)
            {
                Entity reagent = factory.CreateEntity(name);
                Assert.IsNotNull(reagent, $"production reagent '{name}' must exist.");

                var part = reagent.GetPart<ReagentPart>();
                Assert.IsNotNull(part, $"'{name}' must carry ReagentPart.");

                var props = part.GetProperties();
                Assert.Greater(props.Count, 0, $"'{name}' must have at least one property.");
                foreach (var p in props)
                {
                    Assert.IsTrue(KnownProperties.Contains(p.Property),
                        $"'{name}' declares unknown property '{p.Property}' — typo in Objects.json?");
                }

                Assert.IsFalse(string.IsNullOrWhiteSpace(part.FlavorText),
                    $"'{name}' must carry §6.1 hinted-discovery FlavorText.");
            }
        }

        [Test]
        public void Adversarial_Content_ProductionRules_FormsAndPropertiesValid()
        {
            // Forms outside the known set silently fall back to the "tonic"
            // noun; required properties outside the vocabulary can never
            // fire. Both are content typos this pin catches.
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.EnsureInitialized();

            var knownForms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Tonic", "Coating", "Throwable", "Food" };

            var rules = BrewRuleRegistry.GetAllRules();
            Assert.Greater(rules.Count, 0);

            foreach (BrewRule rule in rules)
            {
                Assert.IsTrue(knownForms.Contains(rule.Form),
                    $"rule '{rule.ID}' has unknown form '{rule.Form}'.");

                foreach (var prop in SplitProps(rule.RequireAll))
                    Assert.IsTrue(KnownProperties.Contains(prop),
                        $"rule '{rule.ID}' requires unknown property '{prop}'.");
                foreach (var prop in SplitProps(rule.ForbidAny))
                    Assert.IsTrue(KnownProperties.Contains(prop),
                        $"rule '{rule.ID}' forbids unknown property '{prop}'.");
            }
        }

        // ════════════════ helpers ════════════════

        private static int Count(string kind)
        {
            return DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy", Kind = kind, Limit = 50
            }).Records.Count;
        }

        private static Entity MakeTarget(string id)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Statistics["Hitpoints"] = new Stat
            {
                Owner = e, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30
            };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void FireApplyTonic(Entity brew, Entity target)
        {
            var applyEvent = GameEvent.New("ApplyTonic");
            applyEvent.SetParameter("Actor", (object)target);
            applyEvent.SetParameter("Tonic", (object)brew);
            applyEvent.SetParameter("Zone", (object)null);
            applyEvent.SetParameter("Random", (object)new Random(3));
            applyEvent.SetParameter("Source", (object)target);
            brew.FireEventAndRelease(applyEvent);
        }

        private static IEnumerable<string> SplitProps(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;
            foreach (var s in raw.Split(new[] { ' ', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
                yield return s.Trim();
        }
    }
}
