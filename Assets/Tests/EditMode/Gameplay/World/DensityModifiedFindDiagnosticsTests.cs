using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public sealed class DensityModifiedFindDiagnosticsTests
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly Type[] Allowed = { typeof(EnhancementSerrated), typeof(EnhancementPaleSalt), typeof(EnhancementChoirIron), typeof(EnhancementLacquered), typeof(EnhancementGlowQuartz) };
        private Dictionary<string, Type> classes, names;
        private bool initialized, oldLoot, tablesInitialized;
        private Dictionary<string, LootTableData> tables;
        private EntityFactory factory;
        private static Dictionary<string, Type> Registry(string name)
            => (Dictionary<string, Type>)typeof(EnhancementFactory).GetField(name, Flags).GetValue(null);
        [SetUp] public void Setup()
        {
            tables = new Dictionary<string, LootTableData>((Dictionary<string, LootTableData>)typeof(LootTableRegistry).GetField("_byName", Flags).GetValue(null));
            tablesInitialized = LootTableRegistry.IsInitialized;
            classes = new Dictionary<string, Type>(Registry("_byClassName"));
            names = new Dictionary<string, Type>(Registry("_byDisplayName"));
            initialized = (bool)typeof(EnhancementFactory).GetField("_initialized", Flags).GetValue(null);
            EnhancementFactory.ResetForTests(); foreach (var type in Allowed) EnhancementFactory.Register(type);
            oldLoot = Diag.IsChannelEnabled("loot"); Diag.SetChannel("loot", true);
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [TearDown] public void Cleanup()
        {
            Registry("_byClassName").Clear(); foreach (var pair in classes) Registry("_byClassName").Add(pair.Key, pair.Value);
            Registry("_byDisplayName").Clear(); foreach (var pair in names) Registry("_byDisplayName").Add(pair.Key, pair.Value);
            typeof(EnhancementFactory).GetField("_initialized", Flags).SetValue(null, initialized);
            Diag.SetChannel("loot", oldLoot);
            var liveTables = (Dictionary<string, LootTableData>)typeof(LootTableRegistry).GetField("_byName", Flags).GetValue(null);
            liveTables.Clear(); foreach (var pair in tables) liveTables.Add(pair.Key, pair.Value);
            typeof(LootTableRegistry).GetField("_initialized", Flags).SetValue(null, tablesInitialized);
        }
        private sealed class CountingRandom : Random
        {
            public int Draws; public bool Miss; public Action OnDraw;
            public override int Next(int maximum) { Draws++; OnDraw?.Invoke(); return Miss ? maximum - 1 : 0; }
        }
        private Entity Fresh(out Entity item, string blueprint = "TemperedLongSword")
        {
            var chest = factory.CreateEntity("LockedChest"); item = factory.CreateEntity(blueprint);
            Assert.True(chest.GetPart<ContainerPart>().AddItem(item)); return chest;
        }
        private HashSet<string> Before() => new HashSet<string>(Diag.Snapshot(Diag.BufferCapacity).Select(row => row.TraceId));
        private Diag.Entry[] Outcomes(HashSet<string> before) => Diag.Snapshot(Diag.BufferCapacity)
            .Where(row => !before.Contains(row.TraceId) && row.Category == "loot" && row.Kind.StartsWith("FoundEquipment", StringComparison.Ordinal)).ToArray();
        private static void Reason(Diag.Entry row, string reason) => StringAssert.Contains("\"reason\":\"" + reason + "\"", row.PayloadJson);

        [TestCase("null-rng", "random_required")] [TestCase("wrong-table", "wrong_table")]
        [TestCase("null-owner", "owner_required")] [TestCase("natural-owner", "owner_blueprint")]
        [TestCase("no-lock", "owner_lock_required")] [TestCase("null-item", "item_required")]
        [TestCase("no-owner-physics", "owner_physics_required")] [TestCase("takeable-owner", "owner_takeable")]
        [TestCase("carried-owner", "owner_carried")] [TestCase("equipped-owner", "owner_equipped")]
        [TestCase("no-container", "container_required")] [TestCase("not-retained", "item_not_retained")]
        [TestCase("no-item-physics", "item_physics_required")] [TestCase("wrong-backref", "item_owner_mismatch")]
        [TestCase("equipped-item", "item_equipped")] [TestCase("no-item-tag", "item_tag_required")]
        [TestCase("tier3", "item_tier")] [TestCase("unique", "item_unique")]
        [TestCase("quest", "item_quest")] [TestCase("crafted", "item_crafted")]
        [TestCase("rentable", "item_rentable")] [TestCase("rented", "item_rented")]
        [TestCase("no-equip", "item_not_equippable")] [TestCase("already-rolled", "already_rolled")]
        [TestCase("already-enhanced", "already_enhanced")] [TestCase("stack2", "item_stack")]
        [TestCase("unknown-blueprint", "item_blueprint")] [TestCase("weapon-no-melee", "weapon_shape")]
        [TestCase("weapon-has-armor", "weapon_shape")] [TestCase("armor-no-armor", "armor_shape")]
        [TestCase("armor-has-melee", "armor_shape")] [TestCase("registry-empty", "no_candidates")]
        public void EachRejectingGateNamesItsReasonWithoutSpendingTheOpportunity(string mutation, string reason)
        {
            var positive = Fresh(out var successful); var positiveBefore = Before();
            Assert.True(FoundEquipmentEnhancements.TryApply(positive, "DeepReliquaryT4", successful, new CountingRandom(), 100));
            Assert.AreEqual("FoundEquipmentEnhanced", Outcomes(positiveBefore).Single().Kind);
            var chest = Fresh(out var item, mutation.StartsWith("armor", StringComparison.Ordinal) ? "FineRingMail" : "TemperedLongSword");
            var rng = new CountingRandom(); Random passedRandom = rng; string table = "DeepReliquaryT4";
            switch (mutation)
            {
                case "null-rng": passedRandom = null; break;
                case "wrong-table": table = "SealedVaultT3"; break;
                case "null-owner": chest = null; break;
                case "natural-owner": chest.BlueprintName = "HollowLog"; break;
                case "no-lock": chest.Parts.Remove(chest.GetPart<LockPart>()); break;
                case "null-item": item = null; break;
                case "no-owner-physics": chest.Parts.Remove(chest.GetPart<PhysicsPart>()); break;
                case "takeable-owner": chest.GetPart<PhysicsPart>().Takeable = true; break;
                case "carried-owner": chest.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "equipped-owner": chest.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                case "no-container": chest.Parts.Remove(chest.GetPart<ContainerPart>()); break;
                case "not-retained": chest.GetPart<ContainerPart>().Contents.Remove(item); break;
                case "no-item-physics": item.Parts.Remove(item.GetPart<PhysicsPart>()); break;
                case "wrong-backref": item.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "equipped-item": item.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                case "no-item-tag": item.Tags.Remove("Item"); break;
                case "tier3": item.Tags["Tier"] = "3"; break;
                case "unique": item.Tags["Unique"] = ""; break;
                case "quest": item.Tags["QuestItem"] = ""; break;
                case "crafted": item.Tags["Crafted"] = ""; break;
                case "rentable": item.Tags["Rentable"] = ""; break;
                case "rented": item.AddPart(new RentalPart()); break;
                case "no-equip": item.Parts.Remove(item.GetPart<EquippablePart>()); break;
                case "already-rolled": item.Properties[FoundEquipmentEnhancements.RollMarker] = "1"; break;
                case "already-enhanced": Assert.True(ItemEnhancing.Apply(item, nameof(EnhancementGlowQuartz), 1)); break;
                case "stack2": item.GetPart<StackerPart>().StackCount = 2; break;
                case "unknown-blueprint": item.BlueprintName = "OtherSword"; break;
                case "weapon-no-melee": item.Parts.Remove(item.GetPart<MeleeWeaponPart>()); break;
                case "weapon-has-armor": item.AddPart(new ArmorPart()); break;
                case "armor-no-armor": item.Parts.Remove(item.GetPart<ArmorPart>()); break;
                case "armor-has-melee": item.AddPart(new MeleeWeaponPart()); break;
                case "registry-empty": EnhancementFactory.ResetForTests(); break;
            }
            int parts = item?.Parts.Count ?? 0; bool marked = item?.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker) ?? false;
            var before = Before();
            Assert.False(FoundEquipmentEnhancements.TryApply(chest, table, item, passedRandom, 100));
            Assert.AreEqual(0, rng.Draws); Assert.AreEqual(parts, item?.Parts.Count ?? 0);
            Assert.AreEqual(marked, item?.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker) ?? false);
            var outcome = Outcomes(before).Single(); Assert.AreEqual("FoundEquipmentEnhancementRejected", outcome.Kind); Reason(outcome, reason);
            Assert.AreEqual(chest?.ID, outcome.ActorId); Assert.AreEqual(item?.ID, outcome.TargetId);
            StringAssert.Contains("\"table\":\"" + table + "\"", outcome.PayloadJson);
        }

        [TestCase(-1)] [TestCase(101)] public void InvalidChanceIsDiagnosedBeforeExistingException(int chance)
        {
            var chest = Fresh(out var item); var before = Before(); var rng = new CountingRandom();
            Assert.Throws<ArgumentOutOfRangeException>(() => FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, chance));
            Assert.AreEqual(0, rng.Draws); Assert.False(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            Reason(Outcomes(before).Single(), "invalid_chance");
        }
        [TestCase(0, 0)] [TestCase(25, 1)] public void ValidMissIsMarkedAndDistinctFromRegistryRefusal(int chance, int draws)
        {
            var chest = Fresh(out var item); var before = Before(); var rng = new CountingRandom { Miss = true };
            Assert.False(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, chance));
            Assert.AreEqual(draws, rng.Draws); Assert.True(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            var outcome = Outcomes(before).Single(); Assert.AreEqual("FoundEquipmentEnhancementMissed", outcome.Kind);
            StringAssert.Contains(chance == 0 ? "\"roll\":null" : "\"roll\":99", outcome.PayloadJson);
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item));
        }
        [TestCase(25, 2)] [TestCase(100, 1)] public void SuccessKeepsItsExistingKindAndExactDrawCount(int chance, int draws)
        {
            var chest = Fresh(out var item); var before = Before(); var rng = new CountingRandom();
            Assert.True(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, chance));
            Assert.AreEqual(draws, rng.Draws); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(item));
            var outcome = Outcomes(before).Single(); Assert.AreEqual("FoundEquipmentEnhanced", outcome.Kind);
            Assert.AreEqual(chest.ID, outcome.ActorId); Assert.AreEqual(item.ID, outcome.TargetId);
        }
        [Test] public void ApplicationRefusalAfterSelectionIsAnExplicitSpentOutcome()
        {
            var chest = Fresh(out var item); var before = Before();
            var rng = new CountingRandom { OnDraw = () => EnhancementFactory.ResetForTests() };
            Assert.False(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, 100));
            Assert.AreEqual(1, rng.Draws); Assert.True(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item));
            Reason(Outcomes(before).Single(), "apply_refused");
        }
        [TestCase(0)] [TestCase(25)] [TestCase(100)] public void DisabledChannelChangesNeitherGameplayNorRandomStream(int chance)
        {
            var first = Fresh(out var a); var second = Fresh(out var b); var rngA = new Random(715); var rngB = new Random(715);
            bool yes = FoundEquipmentEnhancements.TryApply(first, "DeepReliquaryT4", a, rngA, chance);
            Diag.SetChannel("loot", false); var before = Before();
            bool no = FoundEquipmentEnhancements.TryApply(second, "DeepReliquaryT4", b, rngB, chance);
            Assert.AreEqual(yes, no); Assert.AreEqual(rngA.Next(), rngB.Next());
            Assert.AreEqual(a.Properties[FoundEquipmentEnhancements.RollMarker], b.Properties[FoundEquipmentEnhancements.RollMarker]);
            CollectionAssert.AreEqual(a.Parts.OfType<IItemEnhancement>().Select(e => e.Name), b.Parts.OfType<IItemEnhancement>().Select(e => e.Name));
            Assert.IsEmpty(Outcomes(before));
        }
        [TestCase("SealedVaultT3")] [TestCase("BasketT3")] public void UnrelatedStockingDoesNotDispatchIrrelevantRefusalRecords(string table)
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            var chest = factory.CreateEntity("LockedChest"); var before = Before(); var expected = new Random(81); var actual = new Random(81);
            var names = LootTableRegistry.Roll(table, expected);
            Assert.Greater(LootStocker.StockContainer(chest, table, factory, actual), 0);
            Assert.AreEqual(expected.Next(), actual.Next()); Assert.IsEmpty(Outcomes(before));
            Assert.True(chest.GetPart<ContainerPart>().Contents.All(e => names.Contains(e.BlueprintName)));
        }
    }
}
