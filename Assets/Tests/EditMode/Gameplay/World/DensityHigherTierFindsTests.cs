using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public class DensityHigherTierFindsTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        internal static readonly string[] Names = { "TemperedLongSword", "CounterweightMaul", "FineRingMail", "RivetedPlate" };
        [SetUp] public void Setup() => scope = new DensityLootTestScope();
        [TearDown] public void Cleanup() => scope.Dispose();
        Entity Item(string name) { Assert.IsTrue(Factory.Blueprints.ContainsKey(name), "Missing authored T4: " + name); return Factory.CreateEntity(name); }
        internal static IReadOnlyList<StructureStamp> Catalog(int depth, BiomeType biome, bool ordinary)
        {
            var method = typeof(StampCatalog).GetMethod("Underground", new[] { typeof(int), typeof(BiomeType), typeof(bool) });
            Assert.NotNull(method, "Source needs explicit depth/surface-biome/ordinary-column gate");
            return (IReadOnlyList<StructureStamp>)method.Invoke(null, new object[] { depth, biome, ordinary });
        }
        [TestCase("TemperedLongSword", "1d10+1", 4, 9, 90, "Hand", "")]
        [TestCase("CounterweightMaul", "2d6+2", 6, 22, 110, "Hand", "Hand,Hand")]
        public void AuthoredWeaponsHaveDistinctActualMechanics(string name, string damage, int penetration, int weight, int value, string slot, string slots)
        {
            var item = Item(name); var weapon = item.GetPart<MeleeWeaponPart>();
            Assert.AreEqual("4", item.GetTag("Tier")); Assert.IsTrue(item.HasTag("Item"));
            Assert.AreEqual(damage, weapon.BaseDamage); Assert.AreEqual(penetration, weapon.PenBonus);
            Assert.AreEqual(name == Names[0] ? "Cutting LongBlades" : "Bludgeoning Cudgel", weapon.Attributes);
            Assert.AreEqual(slot, item.GetPart<EquippablePart>().Slot); Assert.AreEqual(slots, item.GetPart<EquippablePart>().UsesSlots ?? "");
            Assert.AreEqual(weight, item.GetPart<PhysicsPart>().Weight); Assert.AreEqual(value, TradeSystem.GetItemValue(item));
            Assert.IsEmpty(Factory.ValidateAsciiWorldBlueprint(name)); Assert.IsEmpty(Factory.ValidateHandlingBlueprint(name));
            string line = item.GetPart<ExaminablePart>().BuildExamineLine(); Assert.That(line, Does.Contain(damage)); Assert.That(line, Does.Contain("Penetration bonus: +" + penetration));
            Assert.That(item.GetPart<ExaminablePart>().Description.Length, Is.InRange(30, 160));
            var control = Factory.CreateEntity(name == Names[0] ? "LongSword" : "Warhammer");
            Assert.Less(int.Parse(control.GetTag("Tier")), 4); Assert.AreNotEqual(weapon.BaseDamage, control.GetPart<MeleeWeaponPart>().BaseDamage);
        }
        [TestCase("FineRingMail", 6, -1, 24, 125)] [TestCase("RivetedPlate", 9, -4, 46, 140)]
        public void AuthoredArmorKeepsItsMobilityAndWeightTradeoff(string name, int av, int dv, int weight, int value)
        {
            var item = Item(name); Assert.AreEqual("4", item.GetTag("Tier"));
            Assert.AreEqual(av, item.GetPart<ArmorPart>().AV); Assert.AreEqual(dv, item.GetPart<ArmorPart>().DV);
            Assert.AreEqual("Body", item.GetPart<EquippablePart>().Slot); Assert.AreEqual(weight, item.GetPart<PhysicsPart>().Weight);
            Assert.AreEqual(value, TradeSystem.GetItemValue(item)); Assert.IsEmpty(Factory.ValidateAsciiWorldBlueprint(name));
            var plate = Factory.CreateEntity("PlateArmor");
            if (name == "FineRingMail") { Assert.Less(av, plate.GetPart<ArmorPart>().AV); Assert.Greater(dv, plate.GetPart<ArmorPart>().DV); Assert.Less(weight, plate.GetPart<PhysicsPart>().Weight); }
            else { Assert.Greater(av, plate.GetPart<ArmorPart>().AV); Assert.Less(dv, plate.GetPart<ArmorPart>().DV); Assert.Greater(weight, plate.GetPart<PhysicsPart>().Weight); }
        }
        [Test]
        public void PoolIsExactlyOneEqualWeightedOrdinaryPiece_ZeroWeightsProduceNone()
        {
            var pool = LootTableRegistry.Get("FindDeepEquipmentT4"); Assert.NotNull(pool);
            Assert.IsTrue(pool.PickOne); Assert.AreEqual(1, pool.MinPicks); Assert.AreEqual(1, pool.MaxPicks);
            CollectionAssert.AreEquivalent(Names, pool.Entries.Select(e => e.Blueprint));
            Assert.AreEqual(1, pool.Entries.Select(e => e.Weight).Distinct().Count()); Assert.Greater(pool.Entries[0].Weight, 0);
            Assert.IsTrue(pool.Entries.All(e => e.MinCount == 1 && e.MaxCount == 1 && string.IsNullOrEmpty(e.TableRef)));
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 128; seed++) { var roll = LootTableRegistry.Roll(pool.Name, new Random(seed)); Assert.AreEqual(1, roll.Count); seen.Add(roll[0]); }
            CollectionAssert.AreEquivalent(Names, seen);
            foreach (var entry in pool.Entries) entry.Weight = 0;
            Assert.IsEmpty(LootTableRegistry.Roll(pool.Name, new Random(10)));
        }
        [Test]
        public void DeepSourceRetainsOldVaultRollAndAddsExactlyOne_WithChanceZeroControl()
        {
            var table = LootTableRegistry.Get("DeepReliquaryT4"); Assert.NotNull(table); Assert.IsFalse(table.PickOne);
            CollectionAssert.AreEquivalent(new[] { "SealedVaultT3", "FindDeepEquipmentT4" }, table.Entries.Select(e => e.TableRef));
            Assert.IsTrue(table.Entries.All(e => e.Chance == 100 && e.MinCount == 1 && e.MaxCount == 1));
            for (int seed = 0; seed < 64; seed++)
            {
                var old = LootTableRegistry.Roll("SealedVaultT3", new Random(seed)); var added = LootTableRegistry.Roll(table.Name, new Random(seed));
                CollectionAssert.AreEqual(old, added.Take(old.Count)); Assert.AreEqual(old.Count + 1, added.Count);
                Assert.AreEqual(1, added.Count(Names.Contains));
            }
            table.Entries.Single(e => e.TableRef == "FindDeepEquipmentT4").Chance = 0;
            var control = LootTableRegistry.Roll(table.Name, new Random(8));
            CollectionAssert.AreEqual(LootTableRegistry.Roll("SealedVaultT3", new Random(8)), control);
        }
        [TestCase("TemperedLongSword")] [TestCase("CounterweightMaul")] [TestCase("FineRingMail")] [TestCase("RivetedPlate")]
        public void RealStockingReachesEveryPieceAndSavePreservesItsOwnerAndMechanics(string name)
        {
            Item(name); var pool = LootTableRegistry.Get("FindDeepEquipmentT4"); Assert.NotNull(pool);
            int seed = Enumerable.Range(0, 4096).First(s => LootTableRegistry.Roll("DeepReliquaryT4", new Random(s)).Contains(name));
            var chest = Factory.CreateEntity("LockedChest"); int added = LootStocker.StockContainer(chest, "DeepReliquaryT4", Factory, new Random(seed));
            Assert.AreEqual(LootTableRegistry.Roll("DeepReliquaryT4", new Random(seed)).Count, added); var item = chest.GetPart<ContainerPart>().Contents.Single(i => i.BlueprintName == name);
            Assert.AreSame(chest, item.GetPart<PhysicsPart>().InInventory);
            string before = item.GetPart<ExaminablePart>().BuildExamineLine();
            Entity loaded;
            using (var stream = new MemoryStream())
            {
                var w = new SaveWriter(stream); w.WriteEntityReference(chest); w.WriteQueuedEntityBodies(); stream.Position = 0;
                var r = new SaveReader(stream, Factory); loaded = r.ReadEntityReference(); r.ReadEntityBodies();
            }
            var copy = loaded.GetPart<ContainerPart>().Contents.Single(i => i.BlueprintName == name);
            Assert.AreEqual(item.ID, copy.ID); Assert.AreSame(loaded, copy.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(before, copy.GetPart<ExaminablePart>().BuildExamineLine()); Assert.AreEqual(TradeSystem.GetItemValue(item), TradeSystem.GetItemValue(copy));
            pool.Entries.Single(e => e.Blueprint == name).Weight = 0;
            var control = Factory.CreateEntity("LockedChest"); LootStocker.StockContainer(control, "DeepReliquaryT4", Factory, new Random(seed));
            Assert.IsFalse(control.GetPart<ContainerPart>().Contents.Any(i => i.BlueprintName == name));
        }
        [TestCase(9, BiomeType.Spread, true, true)] [TestCase(12, BiomeType.Sodden, true, true)] [TestCase(24, BiomeType.Beating, true, true)]
        [TestCase(8, BiomeType.Spread, true, false)] [TestCase(9, BiomeType.Spread, false, false)]
        [TestCase(9, BiomeType.Grovelands, true, false)] [TestCase(9, BiomeType.Overwrit, true, false)] [TestCase(9, BiomeType.Stump, true, false)]
        [TestCase(9, BiomeType.Cave, true, false)] [TestCase(9, BiomeType.Desert, true, false)]
        public void ContextGateRestrictsDeepFindsToOrdinarySupportedDepths(int depth, BiomeType biome, bool ordinary, bool expected)
        {
            var stamp = Catalog(depth, biome, ordinary).Single(s => s.Name == "Reliquary");
            Assert.AreEqual(expected ? "lockedchest:DeepReliquaryT4" : "lockedchest:SealedVaultT3", stamp.Legend['L']);
            Assert.AreEqual(4, stamp.MinTier); Assert.AreEqual(20, stamp.Chance);
            Assert.AreEqual("spawn:VaultSentinel", stamp.Legend['V']); Assert.AreEqual("IronKey", stamp.Legend['k']);
        }
        [Test]
        public void ReturnedEligibleCatalogDoesNotMutateLegacyOrOtherCalls()
        {
            var before = StampCatalog.Underground(9).Single(s => s.Name == "Reliquary");
            var first = Catalog(9, BiomeType.Spread, true).Single(s => s.Name == "Reliquary");
            Assert.AreNotSame(before, first); first.Legend['L'] = "mutated"; first.Rows[0] = "mutated";
            var again = Catalog(9, BiomeType.Spread, true).Single(s => s.Name == "Reliquary");
            Assert.AreEqual("lockedchest:DeepReliquaryT4", again.Legend['L']); Assert.AreEqual("######", again.Rows[0]);
            Assert.AreEqual("lockedchest:SealedVaultT3", before.Legend['L']); Assert.AreEqual("######", before.Rows[0]);
        }
        [TestCase(true)] [TestCase(false)]
        public void ActualStampedVaultKeepsLockKeyGuardAndSingleBoundedFind(bool eligible)
        {
            var stamp = Catalog(9, eligible ? BiomeType.Spread : BiomeType.Grovelands, true).Single(s => s.Name == "Reliquary");
            var zone = new Zone("deep-find-stamp");
            Assert.IsTrue(new LandmarkBuilder(BiomeType.Cave, 4, new[] { StampCatalog.Forced(stamp) }).BuildZone(zone, Factory, new Random(12)));
            var chest = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "LockedChest");
            Assert.IsTrue(chest.GetPart<LockPart>().IsLocked); Assert.AreEqual("iron", chest.GetPart<LockPart>().KeyId);
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "IronKey"));
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "VaultSentinel"));
            Assert.AreEqual(eligible ? 1 : 0, chest.GetPart<ContainerPart>().Contents.Count(i => Names.Contains(i.BlueprintName)));
            var pos = zone.GetEntityPosition(chest); Assert.IsTrue(zone.GetCell(pos.x + 1, pos.y).IsPassable());
        }
        [Test]
        public void ExistingSourcesHaveNoNewTierFourReferences()
        {
            foreach (string table in new[] { "CrateT1", "CrateT2", "CrateT3", "SackT3", "BasketT3", "HollowLogT3", "SealedVaultT3", "MorrowfastMenderStock", "MorrowfastProvisionerStock", "DeathHumanoidT3" })
            {
                Assert.NotNull(LootTableRegistry.Get(table), table);
                for (int seed = 0; seed < 16; seed++) Assert.IsFalse(LootTableRegistry.Roll(table, new Random(seed)).Any(Names.Contains), table);
            }
            Assert.AreEqual(3, ContainerPlacementService.ClampTableTier(4));
        }
    }
}
