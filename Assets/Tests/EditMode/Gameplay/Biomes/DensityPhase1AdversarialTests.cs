using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 1 hypothesis-driven sweep: nested loot and capacity, save/load,
    /// independent instances, effect aggregation, grouped RNG/world-state gates
    /// and diagnostic coverage. These are bounded probes, not a fuzzing claim.
    /// </summary>
    public class DensityPhase1AdversarialTests
    {
        private EntityFactory _factory;
        private NarrativeStatePart _previousNarrative;

        [OneTimeSetUp]
        public void LoadBlueprints()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            _previousNarrative = NarrativeStatePart.Current;
            NarrativeStatePart.Current = null;
            MessageLog.Clear();
            Diag.ResetAll();
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void Cleanup()
        {
            NarrativeStatePart.Current = _previousNarrative;
            LootTableRegistry.ResetForTests();
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ═══════════ Loot: nesting, capacity, provenance, save ═══════════

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void Adversarial_NestedFinds_ReplayFromSeed_WithoutMutatingTables(int tier)
        {
            // Opening another crate must not consume or reorder a shared pool.
            var source = LootTableRegistry.Get("CrateT" + tier);
            var pool = LootTableRegistry.Get("FindWeaponT" + tier);
            string before = JsonUtility.ToJson(pool);
            for (int seed = 0; seed < 40; seed++)
            {
                var first = LootTableRegistry.Roll(source.Name, new Random(seed));
                LootTableRegistry.Roll(source.Name, new Random(seed + 1));
                CollectionAssert.AreEqual(first, LootTableRegistry.Roll(source.Name, new Random(seed)));
            }
            Assert.AreEqual(before, JsonUtility.ToJson(pool));
        }

        [TestCase(4)] [TestCase(99)]
        public void Adversarial_DeepTier_ReusesRealTierThreeFinds(int depthTier)
        {
            int tier = ContainerPlacementService.ClampTableTier(depthTier);
            Assert.AreEqual(3, tier);
            var table = LootTableRegistry.Get("CrateT" + tier);
            Assert.IsTrue(table.Entries.Exists(e => e.TableRef == "FindWeaponT3"));
            Assert.IsNull(LootTableRegistry.Get("CrateT" + depthTier));
        }

        [Test]
        public void Adversarial_FullContainer_RejectsFindWithoutReplacingExistingItem()
        {
            var crate = _factory.CreateEntity("Crate");
            var container = crate.GetPart<ContainerPart>();
            container.MaxItems = 1;
            var existing = _factory.CreateEntity("Torch");
            Assert.IsTrue(container.AddItem(existing));
            Assert.AreEqual(0, LootStocker.StockContainer(crate, "FindWeaponT3", _factory, new Random(5)));
            Assert.AreSame(existing, container.Contents.Single());
            container.MaxItems = 2;
            Assert.AreEqual(1, LootStocker.StockContainer(crate, "FindWeaponT3", _factory, new Random(5)));
        }

        [Test]
        public void Adversarial_NewFinds_SaveLoadRetainsActualItemAndOnHitRecipe()
        {
            var crate = _factory.CreateEntity("Crate");
            int seed = Enumerable.Range(0, 1024).First(s =>
                LootTableRegistry.Roll("FindWeaponT2", new Random(s)).Contains("FlamingSword"));
            LootStocker.StockContainer(crate, "FindWeaponT2", _factory, new Random(seed));
            var original = crate.GetPart<ContainerPart>().Contents.Single();
            var loaded = RoundTrip(crate).GetPart<ContainerPart>().Contents.Single();
            Assert.AreEqual("FlamingSword", loaded.BlueprintName);
            Assert.AreEqual(original.ID, loaded.ID);
            Assert.AreEqual(original.GetPart<MeleeWeaponPart>().OnHitEffectsRaw,
                loaded.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            Assert.AreNotSame(original, loaded);
        }

        [Test]
        public void Adversarial_NestedPoolCycle_IsRejectedBeforeUse()
        {
            var pool = LootTableRegistry.Get("FindWeaponT1");
            Assert.IsEmpty(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp)));
            pool.Entries.Add(new LootEntryData { TableRef = "CrateT1" });
            Assert.IsTrue(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp))
                .Exists(p => p.Contains("cycle")), "child-to-parent recursion must be caught at validation");
        }

        [Test]
        public void Adversarial_SiblingReferencesToSamePool_AreNotTreatedAsCycles()
        {
            LootTableRegistry.Get("CrateT1").Entries = new List<LootEntryData>
            {
                new LootEntryData { TableRef = "FindWeaponT1" },
                new LootEntryData { TableRef = "FindWeaponT1" }
            };
            Assert.IsEmpty(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp)));
            Assert.AreEqual(2, LootTableRegistry.Roll("CrateT1", new Random(7)).Count);
        }

        // ═══════════ DV: stacking, removal, isolation and save ═══════════

        [Test]
        public void Adversarial_ForcedConfusionDuplicate_DoesNotDoubleTheNewEnemyPenalty()
        {
            var actor = _factory.CreateEntity("Viper");
            var effects = Effects(actor);
            int before = CombatSystem.GetDV(actor);
            effects.ForceApplyEffect(new ConfusedEffect(3));
            effects.ForceApplyEffect(new ConfusedEffect(7));
            Assert.AreEqual(1, effects.EffectCount);
            Assert.AreEqual(before - 3, CombatSystem.GetDV(actor));
            effects.RemoveAllEffects();
            Assert.AreEqual(before, CombatSystem.GetDV(actor));
        }

        [TestCase("Stun", 4)] [TestCase("Hobble", 3)]
        public void Adversarial_DurationStacking_ChargesDVPenaltyOnlyOnce(string kind, int penalty)
        {
            var actor = _factory.CreateEntity("Viper");
            var effects = Effects(actor);
            int before = CombatSystem.GetDV(actor);
            effects.ApplyEffect(MakeEffect(kind, 3));
            effects.ApplyEffect(MakeEffect(kind, 5));
            Assert.AreEqual(1, effects.EffectCount);
            Assert.AreEqual(8, effects.GetAllEffects()[0].Duration);
            Assert.AreEqual(before - penalty, CombatSystem.GetDV(actor));
            effects.RemoveAllEffects();
            Assert.AreEqual(before, CombatSystem.GetDV(actor));
        }

        [TestCase(true)] [TestCase(false)]
        public void Adversarial_DodgeAndStun_RemoveInEitherOrderWithoutLeakingDV(bool removeDodgeFirst)
        {
            var actor = _factory.CreateEntity("Viper");
            var skills = actor.GetPart<SkillsPart>();
            if (skills == null) { skills = new SkillsPart(); actor.AddPart(skills); }
            var dodge = new AcrobaticsDodgePower();
            var effects = Effects(actor);
            int before = CombatSystem.GetDV(actor);
            Assert.IsTrue(skills.AddSkill(dodge));
            Assert.IsTrue(effects.ApplyEffect(new StunnedEffect(4)));
            Assert.AreEqual(before - 2, CombatSystem.GetDV(actor));
            if (removeDodgeFirst)
            {
                Assert.IsTrue(skills.RemoveSkill(dodge));
                Assert.AreEqual(before - 4, CombatSystem.GetDV(actor));
                effects.RemoveAllEffects();
            }
            else
            {
                effects.RemoveAllEffects();
                Assert.AreEqual(before + 2, CombatSystem.GetDV(actor));
                Assert.IsTrue(skills.RemoveSkill(dodge));
            }
            Assert.AreEqual(before, CombatSystem.GetDV(actor));
        }

        [Test]
        public void Adversarial_SameBlueprint_StatsAndNaturalArmorAreIndependent()
        {
            var first = _factory.CreateEntity("Viper");
            var second = _factory.CreateEntity("Viper");
            int before = CombatSystem.GetDV(second);
            first.GetStat("DV").Bonus += 3;
            first.GetPart<ArmorPart>().DV += 2;
            Assert.AreEqual(before + 5, CombatSystem.GetDV(first));
            Assert.AreEqual(before, CombatSystem.GetDV(second));
            Assert.AreNotSame(first.GetStat("DV"), second.GetStat("DV"));
        }

        [TestCase("Stun", 4)] [TestCase("Hobble", 3)] [TestCase("Confuse", 3)]
        public void Adversarial_DVEffect_SaveLoadDoesNotApplyPenaltyTwice_AndRemovalRestores(string kind, int penalty)
        {
            var actor = _factory.CreateEntity("Viper");
            int before = CombatSystem.GetDV(actor);
            Effects(actor).ApplyEffect(MakeEffect(kind, 8));
            var loaded = RoundTrip(actor);
            Assert.AreEqual(before - penalty, CombatSystem.GetDV(loaded));
            Effects(loaded).RemoveAllEffects();
            Assert.AreEqual(before, CombatSystem.GetDV(loaded));
            Assert.AreEqual(before - penalty, CombatSystem.GetDV(actor), "loading/removing cannot mutate the original");
        }

        [Test]
        public void Adversarial_RemovingNaturalArmor_PreservesWornArmorAndStatusAdjustment()
        {
            var actor = _factory.CreateEntity("Creature");
            var natural = actor.GetPart<ArmorPart>();
            natural.DV = 5;
            var armor = _factory.CreateEntity("LeatherArmor");
            actor.GetPart<InventoryPart>().AddObject(armor);
            Assert.IsTrue(InventorySystem.Equip(actor, armor));
            Effects(actor).ApplyEffect(new HobbledEffect());
            int before = CombatSystem.GetDV(actor);
            Assert.IsTrue(actor.RemovePart(natural));
            Assert.AreEqual(before - 5, CombatSystem.GetDV(actor));
            Effects(actor).RemoveAllEffects();
            Assert.AreEqual(before - 2, CombatSystem.GetDV(actor));
        }

        // ═══════════ Population: independent groups and state gates ═══════════

        [Test]
        public void Adversarial_GroupsAreIndependent_AndDoNotSuppressAmbientMinimums()
        {
            var table = Table(Group("A", "first"), Group("B", "first"),
                Group("C", "second"), Group("D", "second"), new PopulationEntry
                { BlueprintName = "Ambient", Weight = 1, MinCount = 2, MaxCount = 2 });
            for (int seed = 0; seed < 64; seed++)
            {
                var result = table.Roll(new Random(seed));
                Assert.AreEqual(1, result.Count(x => x == "A" || x == "B"));
                Assert.AreEqual(1, result.Count(x => x == "C" || x == "D"));
                Assert.AreEqual(2, result.Count(x => x == "Ambient"));
            }
        }

        [Test]
        public void Adversarial_GroupWeights_ReachBothEdgesWithoutSelectingTwoRows()
        {
            var table = Table(Group("First", "encounter"), Group("Last", "encounter"));
            CollectionAssert.AreEqual(new[] { "First" }, table.Roll(new EdgeRandom(false)));
            CollectionAssert.AreEqual(new[] { "Last" }, table.Roll(new EdgeRandom(true)));
        }

        [TestCase(0)] [TestCase(-1)]
        public void Adversarial_NonpositiveGroupWeight_NeverSpawnsEvenWithMinimum(int weight)
        {
            var invalid = Group("Invalid", "encounter");
            invalid.Weight = weight;
            var table = Table(invalid, Group("Valid", "encounter"));
            for (int seed = 0; seed < 24; seed++)
                CollectionAssert.AreEqual(new[] { "Valid" }, table.Roll(new Random(seed)));
        }

        [Test]
        public void Adversarial_RequiredFlag_FailsClosed_ThenFlippingItEnablesOnlyItsRow()
        {
            var gated = Group("Awake", "encounter"); gated.RequiresWorldFlag = "TestAwake";
            var quiet = Group("Quiet", "encounter"); quiet.ForbidsWorldFlag = "TestAwake";
            var table = Table(gated, quiet);
            CollectionAssert.AreEqual(new[] { "Quiet" }, table.Roll(new Random(1)));
            NarrativeStatePart.Current = new NarrativeStatePart();
            NarrativeStatePart.Current.SetFact("TestAwake", 1);
            CollectionAssert.AreEqual(new[] { "Awake" }, table.Roll(new Random(1)));
            NarrativeStatePart.Current.SetFact("TestAwake", 0);
            CollectionAssert.AreEqual(new[] { "Quiet" }, table.Roll(new Random(1)));
        }

        [Test]
        public void Adversarial_AllGroupRowsGated_OutDoesNotFallBackToForbiddenCreature()
        {
            var gated = Group("Forbidden", "encounter"); gated.RequiresWorldFlag = "Missing";
            Assert.IsEmpty(Table(gated).Roll(new Random(1)));
            gated.RequiresWorldFlag = null;
            CollectionAssert.AreEqual(new[] { "Forbidden" }, Table(gated).Roll(new Random(1)));
        }

        [Test]
        public void Adversarial_GroupCount_RespectsBothInclusiveEndpoints()
        {
            var row = Group("Pack", "encounter"); row.MinCount = 2; row.MaxCount = 4;
            Assert.AreEqual(2, Table(row).Roll(new EdgeRandom(false)).Count);
            Assert.AreEqual(4, Table(row).Roll(new EdgeRandom(true)).Count);
        }

        [Test]
        public void Adversarial_GroupRoll_SameSeedReplaysAfterInterveningWorldFlagChange()
        {
            var first = Group("A", "encounter"); first.RequiresWorldFlag = "Flag";
            var table = Table(first, Group("B", "encounter"));
            NarrativeStatePart.Current = new NarrativeStatePart();
            NarrativeStatePart.Current.SetFact("Flag", 1);
            var before = table.Roll(new Random(77));
            NarrativeStatePart.Current.SetFact("Flag", 0);
            table.Roll(new Random(88));
            NarrativeStatePart.Current.SetFact("Flag", 1);
            CollectionAssert.AreEqual(before, table.Roll(new Random(77)));
        }

        [Test]
        public void Adversarial_PopulationDiag_RecordsSelectedUnselectedAndGatedRows()
        {
            var gated = Group("Gated", "encounter"); gated.RequiresWorldFlag = "Missing";
            var table = Table(Group("First", "encounter"), Group("Second", "encounter"), gated);
            table.Roll(new EdgeRandom(false));
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "PopulationRolled", Limit = 20 }).Records;
            Assert.AreEqual(3, records.Count, "each authored row needs a forensic outcome, including zero spawns");
            foreach (string blueprint in new[] { "First", "Second", "Gated" })
                Assert.AreEqual(1, records.Count(r => r.PayloadJson.Contains("\"blueprint\":\"" + blueprint + "\"")));
            foreach (var record in records)
                foreach (string field in new[] { "table", "blueprint", "group", "count", "roll", "reason", "threshold" })
                    StringAssert.Contains("\"" + field + "\":", record.PayloadJson);
            StringAssert.Contains("\"reason\":\"selected\"", records.Single(r => r.PayloadJson.Contains("\"First\"")).PayloadJson);
            StringAssert.Contains("\"reason\":\"not_selected\"", records.Single(r => r.PayloadJson.Contains("\"Second\"")).PayloadJson);
            StringAssert.Contains("\"reason\":\"world_flag\"", records.Single(r => r.PayloadJson.Contains("\"Gated\"")).PayloadJson);
        }

        [TestCase(null, 1, 1)] [TestCase(" ", 1, 1)]
        [TestCase("Invalid", -1, 1)] [TestCase("Invalid", 2, 1)]
        public void Adversarial_MalformedGroupedRow_IsSkippedAndExplained(string blueprint, int min, int max)
        {
            var bad = Group(blueprint, "encounter"); bad.MinCount = min; bad.MaxCount = max;
            var table = Table(bad, Group("Valid", "encounter"));
            CollectionAssert.AreEqual(new[] { "Valid" }, table.Roll(new Random(5)));
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "PopulationRolled", Limit = 20 }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual(1, records.Count(r => r.PayloadJson.Contains("\"reason\":\"invalid_entry\"")));
        }

        [Test]
        public void Adversarial_NullPopulationRow_DoesNotPreventValidGroupFromRolling()
        {
            CollectionAssert.AreEqual(new[] { "Valid" },
                Table(null, Group("Valid", "encounter")).Roll(new Random(3)));
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "PopulationRolled", Limit = 20 }).Records;
            Assert.AreEqual(1, records.Count(r => r.PayloadJson.Contains("\"reason\":\"invalid_entry\"")));
        }

        [Test]
        public void Adversarial_MaximumWeights_DoNotOverflowTheGroupSelection()
        {
            var first = Group("First", "encounter"); first.Weight = int.MaxValue;
            var last = Group("Last", "encounter"); last.Weight = int.MaxValue;
            CollectionAssert.AreEqual(new[] { "First" }, Table(first, last).Roll(new EdgeRandom(false)));
            CollectionAssert.AreEqual(new[] { "Last" }, Table(first, last).Roll(new EdgeRandom(true)));
        }

        [Test]
        public void Adversarial_DisabledPopulationDiagnostics_DoNotChangeSpawnResults()
        {
            var table = Table(Group("First", "encounter"), Group("Last", "encounter"));
            var enabled = table.Roll(new Random(17));
            Assert.IsNotEmpty(DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "PopulationRolled", Limit = 20 }).Records);
            Diag.ResetAll();
            Diag.SetChannel("worldgen", false);
            CollectionAssert.AreEqual(enabled, table.Roll(new Random(17)));
            Assert.IsEmpty(DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "PopulationRolled", Limit = 20 }).Records);
        }

        [Test]
        public void Adversarial_GroupSelection_DoesNotDiluteOptionalAmbientChance()
        {
            var ambient = new PopulationEntry { BlueprintName = "Ambient", Weight = 1, MinCount = 0, MaxCount = 1 };
            var group = Group("Encounter", "encounter"); group.Weight = int.MaxValue;
            var rolled = Table(group, ambient).Roll(new EdgeRandom(true));
            CollectionAssert.AreEquivalent(new[] { "Encounter", "Ambient" }, rolled,
                "the sole ambient row still has 100% optional chance, regardless of group weight");
        }

        private Entity RoundTrip(Entity source)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream);
                writer.WriteEntityReference(source);
                writer.WriteQueuedEntityBodies();
                stream.Position = 0;
                var reader = new SaveReader(stream, _factory);
                var loaded = reader.ReadEntityReference();
                reader.ReadEntityBodies();
                return loaded;
            }
        }

        private static StatusEffectsPart Effects(Entity actor)
        {
            var effects = actor.GetPart<StatusEffectsPart>();
            if (effects == null) { effects = new StatusEffectsPart(); actor.AddPart(effects); }
            return effects;
        }

        private static Effect MakeEffect(string kind, int duration)
        {
            if (kind == "Stun") return new StunnedEffect(duration);
            if (kind == "Hobble") return new HobbledEffect(duration);
            return new ConfusedEffect(duration);
        }

        private static PopulationEntry Group(string blueprint, string group)
        {
            var row = new PopulationEntry { BlueprintName = blueprint, Weight = 1, MinCount = 1, MaxCount = 1 };
            var field = typeof(PopulationEntry).GetField("EncounterGroup");
            Assert.IsNotNull(field, "population rows need an explicit encounter group");
            field.SetValue(row, group);
            return row;
        }

        private static PopulationTable Table(params PopulationEntry[] rows)
            => new PopulationTable { Name = "AdversarialDensity", Entries = new List<PopulationEntry>(rows) };

        private sealed class EdgeRandom : Random
        {
            private readonly bool _last;
            public EdgeRandom(bool last) { _last = last; }
            public override int Next(int maxValue) => _last ? maxValue - 1 : 0;
            public override int Next(int minValue, int maxValue) => _last ? maxValue - 1 : minValue;
            public override double NextDouble() => _last ? 0.999999 : 0;
        }
    }
}
