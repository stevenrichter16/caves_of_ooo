using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityModifiedFindsAdversarialTests
    {
        private DensityLootTestScope scope;
        private DensityFoundEnhancementScope enhancements;
        private static readonly Type[] Allowed = { typeof(EnhancementSerrated), typeof(EnhancementPaleSalt), typeof(EnhancementChoirIron), typeof(EnhancementLacquered), typeof(EnhancementGlowQuartz) };

        [SetUp]
        public void Setup()
        {
            scope = new DensityLootTestScope();
            enhancements = new DensityFoundEnhancementScope();
            EnhancementFactory.ResetForTests();
            foreach (var type in Allowed) EnhancementFactory.Register(type);
        }

        [TearDown]
        public void Cleanup()
        {
            enhancements.Dispose(); scope.Dispose();
        }

        private Entity Fresh(out Entity item, string blueprint = "TemperedLongSword")
        {
            var chest = scope.Factory.CreateEntity("LockedChest"); item = scope.Factory.CreateEntity(blueprint);
            Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item));
            return chest;
        }

        private sealed class CountingRandom : Random
        {
            public int Draws;
            public override int Next(int maxValue) { Draws++; return 0; }
        }

        [TestCase("wrong-table")] [TestCase("natural-owner")]
        [TestCase("owner-no-lock")] [TestCase("owner-no-container")]
        [TestCase("owner-no-physics")] [TestCase("owner-takeable")]
        [TestCase("owner-carried")] [TestCase("owner-equipped")]
        [TestCase("item-not-contained")] [TestCase("item-wrong-owner")]
        [TestCase("item-equipped")] [TestCase("item-no-physics")]
        [TestCase("item-not-item")] [TestCase("item-tier3")]
        [TestCase("item-unique")] [TestCase("item-quest")]
        [TestCase("item-crafted")] [TestCase("item-rentable")]
        [TestCase("item-rented")] [TestCase("item-no-equip")]
        [TestCase("item-no-weapon")] [TestCase("item-mixed-shape")]
        [TestCase("item-foreign-blueprint")] [TestCase("item-stack2")]
        [TestCase("item-stack0")]
        public void MutatedContextRefusesWithoutMarkerRandomOrEnhancement(string mutation)
        {
            var positive = Fresh(out var positiveItem);
            Assert.IsTrue(FoundEquipmentEnhancements.TryApply(positive, "DeepReliquaryT4", positiveItem, new CountingRandom(), 100));
            var chest = Fresh(out var item); string table = "DeepReliquaryT4";
            switch (mutation)
            {
                case "wrong-table": table = "FindDeepEquipmentT4"; break;
                case "natural-owner": chest.BlueprintName = "HollowLog"; break;
                case "owner-no-lock": chest.Parts.Remove(chest.GetPart<LockPart>()); break;
                case "owner-no-container": chest.Parts.Remove(chest.GetPart<ContainerPart>()); break;
                case "owner-no-physics": chest.Parts.Remove(chest.GetPart<PhysicsPart>()); break;
                case "owner-takeable": chest.GetPart<PhysicsPart>().Takeable = true; break;
                case "owner-carried": chest.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "owner-equipped": chest.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                case "item-not-contained": chest.GetPart<ContainerPart>().Contents.Remove(item); break;
                case "item-wrong-owner": item.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "item-equipped": item.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                case "item-no-physics": item.Parts.Remove(item.GetPart<PhysicsPart>()); break;
                case "item-not-item": item.Tags.Remove("Item"); break;
                case "item-tier3": item.Tags["Tier"] = "3"; break;
                case "item-unique": item.Tags["Unique"] = ""; break;
                case "item-quest": item.Tags["QuestItem"] = ""; break;
                case "item-crafted": item.Tags["Crafted"] = ""; break;
                case "item-rentable": item.Tags["Rentable"] = ""; break;
                case "item-rented": item.AddPart(new RentalPart()); break;
                case "item-no-equip": item.Parts.Remove(item.GetPart<EquippablePart>()); break;
                case "item-no-weapon": item.Parts.Remove(item.GetPart<MeleeWeaponPart>()); break;
                case "item-mixed-shape": item.AddPart(new ArmorPart()); break;
                case "item-foreign-blueprint": item.BlueprintName = "ForgedWeapon"; break;
                case "item-stack2": item.GetPart<StackerPart>().StackCount = 2; break;
                case "item-stack0": item.GetPart<StackerPart>().StackCount = 0; break;
                default: Assert.Fail("Unknown mutation"); break;
            }
            var rng = new CountingRandom(); int parts = item.Parts.Count;
            Assert.IsFalse(FoundEquipmentEnhancements.TryApply(chest, table, item, rng, 100), mutation);
            Assert.AreEqual(0, rng.Draws); Assert.AreEqual(parts, item.Parts.Count);
            Assert.IsFalse(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item));
        }

        [TestCase(false)] [TestCase(true)]
        public void SavedSuccessfulAndMissedOpportunitiesCannotBeRolledAgain(bool success)
        {
            var chest = Fresh(out var item); var rng = new CountingRandom();
            int value = TradeSystem.GetItemValue(item);
            Assert.AreEqual(success, FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, success ? 100 : 0));
            Assert.AreEqual(success ? 1 : 0, rng.Draws);
            Assert.IsTrue(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            Entity copy;
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(chest); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, scope.Factory); copy = reader.ReadEntityReference(); reader.ReadEntityBodies();
            }
            var saved = copy.GetPart<ContainerPart>().Contents.Single(); var retry = new CountingRandom();
            Assert.AreEqual(item.ID, saved.ID); Assert.AreSame(copy, saved.GetPart<PhysicsPart>().InInventory);
            Assert.IsTrue(copy.GetPart<LockPart>().IsLocked); Assert.AreEqual(value, TradeSystem.GetItemValue(saved));
            Assert.AreEqual(item.GetPart<ExaminablePart>().BuildExamineLine(), saved.GetPart<ExaminablePart>().BuildExamineLine());
            Assert.IsFalse(FoundEquipmentEnhancements.TryApply(copy, "DeepReliquaryT4", saved, retry, 100));
            Assert.AreEqual(0, retry.Draws); Assert.AreEqual(success ? 1 : 0, ItemEnhancing.CountEnhancements(saved));
            var newChest = Fresh(out var fresh); Assert.IsTrue(FoundEquipmentEnhancements.TryApply(newChest, "DeepReliquaryT4", fresh, new CountingRandom(), 100));
        }

        [TestCase(-1)] [TestCase(101)]
        public void InvalidChanceThrowsBeforeChangingTheItem(int chance)
        {
            var chest = Fresh(out var item); var rng = new CountingRandom();
            Assert.Throws<ArgumentOutOfRangeException>(() => FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, chance));
            Assert.AreEqual(0, rng.Draws); Assert.IsFalse(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
        }

        [TestCase("TemperedLongSword", 4)] [TestCase("CounterweightMaul", 3)]
        [TestCase("FineRingMail", 2)] [TestCase("RivetedPlate", 2)]
        public void EveryCompatibleChoiceCanOccurButNoOtherEnhancementLeaks(string blueprint, int expectedChoices)
        {
            var seen = new HashSet<Type>();
            for (int seed = 0; seed < 48; seed++)
            {
                var chest = Fresh(out var item, blueprint);
                Assert.IsTrue(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, new Random(seed), 100));
                var mod = item.Parts.OfType<IItemEnhancement>().Single(); seen.Add(mod.GetType());
                Assert.IsTrue(mod.Applicable(item)); Assert.AreEqual(1, mod.Tier);
                Assert.IsFalse(mod is EnhancementEngraved);
                if (blueprint == "CounterweightMaul") Assert.IsFalse(mod is EnhancementSerrated);
            }
            Assert.AreEqual(expectedChoices, seen.Count);
        }

        [Test]
        public void MissingRegistryAndExistingEnhancementRefuseWithoutLosingState()
        {
            var chest = Fresh(out var item); var rng = new CountingRandom();
            EnhancementFactory.ResetForTests();
            Assert.IsFalse(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, 100));
            Assert.AreEqual(0, rng.Draws); Assert.IsFalse(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
            EnhancementFactory.Register(typeof(EnhancementGlowQuartz));
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(EnhancementGlowQuartz), 2));
            Assert.IsFalse(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, rng, 100));
            Assert.AreEqual(2, item.GetPart<EnhancementGlowQuartz>().Tier); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(item));
            Assert.AreEqual(0, rng.Draws); Assert.IsFalse(item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker));
        }

        [TestCase(false)] [TestCase(true)]
        public void AcquiredModifiedEquipmentAppliesAndReversesItsActualEquipEffect(bool armor)
        {
            EnhancementFactory.ResetForTests(); EnhancementFactory.Register(armor ? typeof(EnhancementLacquered) : typeof(EnhancementGlowQuartz));
            var chest = Fresh(out var item, armor ? "FineRingMail" : "TemperedLongSword");
            int value = TradeSystem.GetItemValue(item), baseAv = item.GetPart<ArmorPart>()?.AV ?? 0;
            Assert.IsTrue(FoundEquipmentEnhancements.TryApply(chest, "DeepReliquaryT4", item, new CountingRandom(), 100));
            Assert.AreEqual(value, TradeSystem.GetItemValue(item));
            Assert.IsTrue(chest.GetPart<ContainerPart>().RemoveItem(item));
            var actor = scope.Factory.CreateEntity("Player"); int before = CombatSystem.GetAV(actor);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); Assert.IsTrue(InventorySystem.Equip(actor, item));
            if (armor) Assert.AreEqual(before + baseAv + 1, CombatSystem.GetAV(actor));
            else Assert.AreEqual(1, item.GetPart<LightSourcePart>().Radius);
            Assert.IsTrue(InventorySystem.UnequipItem(actor, item));
            Assert.AreEqual(before, CombatSystem.GetAV(actor));
            if (armor) Assert.AreEqual(baseAv, item.GetPart<ArmorPart>().AV);
            else Assert.AreEqual(0, item.GetPart<LightSourcePart>().Radius);
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count(e => ReferenceEquals(e, item)));
        }
    }
}
