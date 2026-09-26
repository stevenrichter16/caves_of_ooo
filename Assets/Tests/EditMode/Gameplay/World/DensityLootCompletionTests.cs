using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public class DensityLootCompletionTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() => scope = new DensityLootTestScope();
        [TearDown] public void Cleanup() => scope.Dispose();
        internal static readonly string[] Armor = { "LeatherArmor", "Buckler", "LeatherBoots", "LeatherGloves", "LeatherCap", "Cloak",
            "ChainMail", "IronHelmet", "IronshodBoots", "WardedCloak", "IronBuckler", "PlateArmor" };
        internal static readonly string[] ChangedHostiles = { "MarlbackScrabbler", "MarlbackGleaner", "MarlbackTunnelguard", "DesertBandit", "RuinScavenger",
            "MarlbackWallkeeper", "MarlbackBreacher", "AmbushBandit", "RuneCultist" };

        [TestCase("Crate", 1, 20)] [TestCase("Crate", 2, 25)] [TestCase("Crate", 3, 30)]
        [TestCase("Urn", 1, 10)] [TestCase("Urn", 2, 15)] [TestCase("Urn", 3, 20)]
        [TestCase("BoneCache", 1, 20)] [TestCase("BoneCache", 2, 25)] [TestCase("BoneCache", 3, 30)]
        [TestCase("StrongBox", 1, 25)] [TestCase("StrongBox", 2, 30)] [TestCase("StrongBox", 3, 35)]
        [TestCase("Reliquary", 1, 15)] [TestCase("Reliquary", 2, 20)] [TestCase("Reliquary", 3, 25)]
        public void LiveContainerAddsOneTieredArmorRoll_WithZeroAndCertainControls(string blueprint, int tier, int chance)
        {
            var table = LootTableRegistry.Get(blueprint + "T" + tier);
            Assert.IsFalse(table.PickOne, "the source must honor independent chance");
            var added = table.Entries.SingleOrDefault(e => e.TableRef == "FindArmorT" + tier);
            Assert.NotNull(added, "live source must reach the new pool"); Assert.AreEqual(chance, added.Chance);
            Assert.AreEqual(1, added.MinCount); Assert.AreEqual(1, added.MaxCount);
            // Isolate only the new reference, retaining the actual source/table
            // and stocker path. Baseline and other old rows are checked separately.
            table.Entries = new List<LootEntryData> { added };
            added.Chance = 100;
            var container = Factory.CreateEntity(blueprint);
            Assert.AreEqual(1, LootStocker.StockContainer(container, table.Name, Factory, new Random(6)));
            var item = container.GetPart<ContainerPart>().Contents.Single();
            Assert.NotNull(item.GetPart<ArmorPart>());
            Assert.LessOrEqual(int.Parse(item.GetTag("Tier", "1")), tier);
            added.Chance = 0;
            var control = Factory.CreateEntity(blueprint);
            Assert.AreEqual(0, LootStocker.StockContainer(control, table.Name, Factory, new Random(6)));
            Assert.IsEmpty(control.GetPart<ContainerPart>().Contents);
        }

        [TestCase("LeatherArmor", 1)] [TestCase("Buckler", 1)] [TestCase("LeatherBoots", 1)]
        [TestCase("LeatherGloves", 1)] [TestCase("LeatherCap", 1)] [TestCase("Cloak", 1)]
        [TestCase("ChainMail", 2)] [TestCase("IronHelmet", 2)] [TestCase("IronshodBoots", 2)]
        [TestCase("WardedCloak", 2)] [TestCase("IronBuckler", 2)] [TestCase("PlateArmor", 3)]
        public void EveryConcreteArmorHasAnActualStockedCrateSource(string blueprint, int tier)
        {
            var table = LootTableRegistry.Get("CrateT" + tier);
            var reference = table.Entries.SingleOrDefault(e => e.TableRef == "FindArmorT" + tier);
            Assert.NotNull(reference);
            // Retain all ordinary rows: prove real stocking/capacity, not only
            // an orphan pool roll. Disabling just this ref is the counter-check.
            int seed = Enumerable.Range(0, 4096).First(s => LootTableRegistry.Roll(table.Name, new Random(s)).Contains(blueprint));
            var crate = Factory.CreateEntity("Crate");
            LootStocker.StockContainer(crate, table.Name, Factory, new Random(seed));
            Assert.That(crate.GetPart<ContainerPart>().Contents.Any(i => i.BlueprintName == blueprint), Is.True);
            var pool = LootTableRegistry.Get(reference.TableRef);
            foreach (var entry in pool.Entries) entry.Weight = 0;
            var control = Factory.CreateEntity("Crate");
            LootStocker.StockContainer(control, table.Name, Factory, new Random(seed));
            Assert.That(control.GetPart<ContainerPart>().Contents.Any(i => i.BlueprintName == blueprint), Is.False,
                "this minimum-tier crate has no legacy row for the target armor");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ArmorPoolOffersRealEquipmentAndExactlyOnePick(int tier)
        {
            var pool = LootTableRegistry.Get("FindArmorT" + tier); Assert.NotNull(pool);
            Assert.IsTrue(pool.PickOne); Assert.AreEqual(1, pool.MinPicks); Assert.AreEqual(1, pool.MaxPicks);
            foreach (var entry in pool.Entries)
            {
                var item = Factory.CreateEntity(entry.Blueprint);
                Assert.That(Armor, Does.Contain(item.BlueprintName));
                Assert.NotNull(item.GetPart<ArmorPart>()); Assert.NotNull(item.GetPart<EquippablePart>());
                Assert.LessOrEqual(int.Parse(item.GetTag("Tier", "1")), tier);
            }
            for (int seed = 0; seed < 64; seed++) Assert.AreEqual(1, LootTableRegistry.Roll(pool.Name, new Random(seed)).Count);
            foreach (var entry in pool.Entries) entry.Weight = 0;
            Assert.IsEmpty(LootTableRegistry.Roll(pool.Name, new Random(4)));
        }

        [TestCase("MarlbackScrabbler", "Dagger", "LeatherCap;LeatherGloves", "")]
        [TestCase("MarlbackGleaner", "Dagger", "LeatherGloves;LeatherArmor", "")]
        [TestCase("MarlbackTunnelguard", "Spear", "LeatherBoots;LeatherCap", "")]
        [TestCase("DesertBandit", "ShortSword", "LeatherCap;LeatherBoots", "")]
        [TestCase("RuinScavenger", "Dagger", "LeatherGloves;LeatherCap", "")]
        [TestCase("MarlbackWallkeeper", "LongSword", "LeatherArmor;LeatherCap;LeatherBoots", "LeatherArmor;LeatherCap")]
        [TestCase("MarlbackBreacher", "BreacherCleaver", "LeatherArmor;IronHelmet;IronshodBoots", "LeatherArmor;IronHelmet;IronshodBoots")]
        [TestCase("AmbushBandit", "ShortSword", "LeatherArmor;LeatherBoots", "")]
        [TestCase("RuneCultist", "Dagger", "LeatherGloves;Cloak", "")]
        public void HostileKitActuallyEquipsVisibleGear_AndChanceFailureKeepsItsWeapon(string blueprint, string weapon,
            string armor, string guaranteedArmor)
        {
            LoadoutPart.Rng = new GateRandom(0);
            var actor = Factory.CreateEntity(blueprint);
            var gear = DensityLootTestScope.Gear(actor).ToArray();
            foreach (var item in gear) Assert.IsTrue(InventorySystem.IsEquipped(actor, item), item.BlueprintName + " must not silently fall back to carried");
            Assert.IsEmpty(actor.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(weapon, gear.Single(i => i.HasPart<MeleeWeaponPart>()).BlueprintName);
            CollectionAssert.AreEquivalent(armor.Split(';'), gear.Where(i => i.HasPart<ArmorPart>()).Select(i => i.BlueprintName));
            LoadoutPart.Rng = new GateRandom(99);
            var control = Factory.CreateEntity(blueprint);
            var controlGear = DensityLootTestScope.Gear(control).ToArray();
            Assert.AreEqual(weapon, controlGear.Single(i => i.HasPart<MeleeWeaponPart>()).BlueprintName);
            CollectionAssert.AreEquivalent(guaranteedArmor.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries),
                controlGear.Where(i => i.HasPart<ArmorPart>()).Select(i => i.BlueprintName));
        }

        [TestCase("MarlbackScrabbler", "Dagger;Hatchet;Cudgel")]
        [TestCase("RuinScavenger", "Dagger;Cudgel;OldWorldPipe")]
        public void VariantsOccurAsActualHeldWeapons_AndDisappearWithoutThePick(string blueprint, string expected)
        {
            var observed = new HashSet<string>();
            for (int seed = 0; seed < 64; seed++)
            {
                scope.Seed(seed); var actor = Factory.CreateEntity(blueprint);
                observed.UnionWith(DensityLootTestScope.Gear(actor).Where(i => i.HasPart<MeleeWeaponPart>() && InventorySystem.IsEquipped(actor, i)).Select(i => i.BlueprintName));
            }
            CollectionAssert.AreEquivalent(expected.Split(';'), observed);
            Factory.Blueprints[blueprint].Parts["Loadout"]["Pick"] = "";
            var control = Factory.CreateEntity(blueprint);
            Assert.IsFalse(DensityLootTestScope.Gear(control).Any(i => i.HasPart<MeleeWeaponPart>()));
        }

        [TestCase("MarlbackScrabbler")] [TestCase("MarlbackGleaner")] [TestCase("MarlbackTunnelguard")]
        [TestCase("DesertBandit")] [TestCase("RuinScavenger")] [TestCase("MarlbackWallkeeper")]
        [TestCase("MarlbackBreacher")] [TestCase("AmbushBandit")] [TestCase("RuneCultist")]
        public void WhatWasVisiblyEquippedFallsAsTheSameItemOnceOnRealDeath(string blueprint)
        {
            LoadoutPart.Rng = new GateRandom(0);
            var actor = Factory.CreateEntity(blueprint);
            var gear = DensityLootTestScope.Gear(actor).ToArray();
            Assert.That(gear.Length, Is.GreaterThan(0));
            foreach (var item in gear) Assert.IsTrue(InventorySystem.IsEquipped(actor, item));
            var zone = new Zone("density-loot-death"); Assert.IsTrue(zone.AddEntity(actor, 12, 12));
            CombatSystem.ApplyDamage(actor, actor.GetStatValue("Hitpoints") + 1000, null, zone);
            foreach (var item in gear)
            {
                Assert.AreEqual(1, zone.GetCell(12, 12).Objects.Count(i => ReferenceEquals(i, item)), item.BlueprintName);
                Assert.IsNull(item.GetPart<PhysicsPart>().InInventory); Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
            }
            Assert.IsEmpty(DensityLootTestScope.Gear(actor));
        }

        [Test]
        public void ShippedRegistryStillValidates()
            => Assert.IsEmpty(LootTableRegistry.Validate(b => Factory.Blueprints.ContainsKey(b)));

        internal sealed class GateRandom : Random
        {
            readonly int chance; public GateRandom(int chance) { this.chance = chance; }
            public override int Next(int maxValue) => maxValue == 100 ? chance : 0;
            public override int Next(int minValue, int maxValue) => minValue;
        }
    }
}
