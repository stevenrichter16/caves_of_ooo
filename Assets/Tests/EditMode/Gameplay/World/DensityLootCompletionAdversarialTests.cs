using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    // Post-GREEN cold-eye sweep: boundaries, ownership, persistence, suppression,
    // natural/friendly provenance and random replay of the actual authored kits.
    public class DensityLootCompletionAdversarialTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() => scope = new DensityLootTestScope();
        [TearDown] public void Cleanup() => scope.Dispose();

        [TestCase("Sack", 1)] [TestCase("Sack", 2)] [TestCase("Sack", 3)]
        [TestCase("WovenBasket", 1)] [TestCase("WovenBasket", 2)] [TestCase("WovenBasket", 3)]
        [TestCase("HollowLog", 1)] [TestCase("HollowLog", 2)] [TestCase("HollowLog", 3)]
        public void Adversarial_NaturalChoirSourcesStayNaturalThroughActualStocking(string name, int tier)
        {
            int seen = 0;
            for (int seed = 0; seed < 64; seed++)
            {
                var container = Factory.CreateEntity(name);
                string tablePrefix = name == "WovenBasket" ? "Basket" : name;
                LootStocker.StockContainer(container, tablePrefix + "T" + tier, Factory, new Random(seed));
                foreach (var item in container.GetPart<ContainerPart>().Contents)
                {
                    seen++;
                    Assert.That(new[] { "weapon", "armor", "offense" }, Does.Not.Contain(DensityLootCensusTests.Category(item)), item.BlueprintName);
                }
            }
            Assert.Greater(seen, 0, "an empty/missing table is not a provenance control");
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void Adversarial_AddedChanceBoundaryIsExclusiveAndCapacityCannotInventArmor(int tier)
        {
            var table = LootTableRegistry.Get("CrateT" + tier);
            var reference = table.Entries.Single(e => e.TableRef == "FindArmorT" + tier);
            table.Entries = new List<LootEntryData> { reference };
            foreach (bool succeeds in new[] { true, false })
            {
                var crate = Factory.CreateEntity("Crate");
                int added = LootStocker.StockContainer(crate, table.Name, Factory,
                    new DensityLootCompletionTests.GateRandom(reference.Chance - (succeeds ? 1 : 0)));
                Assert.AreEqual(succeeds ? 1 : 0, added);
                Assert.AreEqual(added, crate.GetPart<ContainerPart>().Contents.Count);
            }
            var full = Factory.CreateEntity("Crate"); var cp = full.GetPart<ContainerPart>();
            cp.MaxItems = 1; var sentinel = Factory.CreateEntity("GoldCoin"); Assert.IsTrue(cp.AddItem(sentinel));
            Assert.AreEqual(0, LootStocker.StockContainer(full, table.Name, Factory, new DensityLootCompletionTests.GateRandom(0)));
            CollectionAssert.AreEqual(new[] { sentinel }, cp.Contents);
            Assert.AreSame(full, sentinel.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void Adversarial_StockedArmorRoundTripsWithIdentityOwnerAndMechanics(int tier)
        {
            var crate = Factory.CreateEntity("Crate");
            Assert.AreEqual(1, LootStocker.StockContainer(crate, "FindArmorT" + tier, Factory, new Random(3)));
            var original = crate.GetPart<ContainerPart>().Contents.Single();
            var loaded = RoundTrip(crate); var item = loaded.GetPart<ContainerPart>().Contents.Single();
            Assert.AreEqual(original.ID, item.ID); Assert.AreEqual(original.BlueprintName, item.BlueprintName);
            Assert.AreEqual(original.GetPart<ArmorPart>().AV, item.GetPart<ArmorPart>().AV);
            Assert.AreEqual(original.GetPart<ArmorPart>().DV, item.GetPart<ArmorPart>().DV);
            Assert.AreSame(loaded, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
        }

        [TestCase("MarlbackWallkeeper")] [TestCase("MarlbackBreacher")]
        public void Adversarial_SavedVisibleGearStaysEquippedAndDropsOnceWithoutRespawningLoadout(string name)
        {
            LoadoutPart.Rng = new DensityLootCompletionTests.GateRandom(0);
            var actor = Factory.CreateEntity(name); var before = DensityLootTestScope.Gear(actor).ToArray();
            int av = CombatSystem.GetAV(actor), dv = CombatSystem.GetDV(actor), speed = actor.GetStatValue("Speed");
            var loaded = RoundTrip(actor); var gear = DensityLootTestScope.Gear(loaded).ToArray();
            CollectionAssert.AreEquivalent(before.Select(i => i.ID), gear.Select(i => i.ID));
            Assert.AreEqual(gear.Length, gear.Select(i => i.ID).Distinct().Count());
            Assert.IsEmpty(loaded.GetPart<InventoryPart>().Objects);
            foreach (var item in gear) Assert.IsTrue(InventorySystem.IsEquipped(loaded, item));
            Assert.AreEqual(av, CombatSystem.GetAV(loaded)); Assert.AreEqual(dv, CombatSystem.GetDV(loaded));
            Assert.AreEqual(speed, loaded.GetStatValue("Speed"));
            var zone = new Zone("saved-loot"); Assert.IsTrue(zone.AddEntity(loaded, 12, 12));
            CombatSystem.HandleDeath(loaded, null, zone); CombatSystem.HandleDeath(loaded, null, zone);
            foreach (var item in gear) Assert.AreEqual(1, zone.GetCell(12, 12).Objects.Count(i => ReferenceEquals(i, item)));
        }

        [TestCase("Temporary")] [TestCase("NoDropOnDeath")]
        public void Adversarial_SuppressedHostileDoesNotBecomeAnArmorFarm(string tag)
        {
            LoadoutPart.Rng = new DensityLootCompletionTests.GateRandom(0);
            var actor = Factory.CreateEntity("MarlbackBreacher"); var gear = DensityLootTestScope.Gear(actor).ToArray();
            Assert.AreEqual(4, gear.Length); actor.SetTag(tag);
            var zone = new Zone("suppressed-loot"); Assert.IsTrue(zone.AddEntity(actor, 12, 12));
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(actor, null, zone));
            CombatSystem.HandleDeath(actor, null, zone);
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Item")), "neither gear nor hidden death supplies spill");
            var control = Factory.CreateEntity("MarlbackBreacher"); var controlGear = DensityLootTestScope.Gear(control).ToArray();
            Assert.IsTrue(zone.AddEntity(control, 14, 12)); CombatSystem.HandleDeath(control, null, zone);
            foreach (var item in controlGear) Assert.That(zone.GetCell(14, 12).Objects, Does.Contain(item));
        }

        [TestCase("MarlbackScrabbler")] [TestCase("MarlbackBreacher")]
        public void Adversarial_KillingOneSpawnCannotStealItsSiblingsGearAndSeedReplayIsReal(string name)
        {
            scope.Seed(43); var actor = Factory.CreateEntity(name); var gear = DensityLootTestScope.Gear(actor).ToArray();
            scope.Seed(43); var sibling = Factory.CreateEntity(name); var other = DensityLootTestScope.Gear(sibling).ToArray();
            CollectionAssert.AreEquivalent(gear.Select(i => i.BlueprintName), other.Select(i => i.BlueprintName));
            Assert.IsFalse(gear.Any(other.Contains)); Assert.IsFalse(gear.Select(i => i.ID).Intersect(other.Select(i => i.ID)).Any());
            var zone = new Zone("siblings"); zone.AddEntity(actor, 12, 12); zone.AddEntity(sibling, 14, 12);
            CombatSystem.HandleDeath(actor, null, zone);
            CollectionAssert.AreEquivalent(other, DensityLootTestScope.Gear(sibling));
            foreach (var item in other) Assert.IsTrue(InventorySystem.IsEquipped(sibling, item));
            Assert.IsFalse(zone.GetCell(12, 12).Objects.Any(other.Contains));
        }

        [Test]
        public void Adversarial_SentryNaturalBladeNeverBecomesAHiddenManufacturedReward()
        {
            var sentry = Factory.CreateEntity("SkeletalSentry");
            CollectionAssert.AreEquivalent(new[] { "IronHelmet" }, DensityLootTestScope.Gear(sentry).Select(i => i.BlueprintName));
            var zone = new Zone("natural-sentry"); zone.AddEntity(sentry, 12, 12); CombatSystem.HandleDeath(sentry, null, zone);
            Assert.IsFalse(zone.GetAllEntities().Any(i => i.HasPart<MeleeWeaponPart>()), "natural BoneBlade is not a carried item");
        }

        [Test]
        public void Adversarial_AllSeventeenFriendlyAndServiceLoadoutsKeepTheirAuthoredEquipment()
        {
            TraderPart.Factory = null; // Shop stock is not personal equipment.
            var expected = new[]
            {
                new[] { "Elder", "LeatherCap;LeatherBoots", "", "" },
                new[] { "Weaponsmith", "LeatherGloves;LeatherBoots", "", "" },
                new[] { "Tinker", "Dagger;LeatherGloves", "", "" },
                new[] { "Merchant", "ShortSword;LeatherBoots", "", "" },
                new[] { "Quartermaster", "Spear;LeatherArmor;LeatherBoots", "", "" },
                new[] { "Warden", "LongSword;LeatherArmor;LeatherBoots", "", "" },
                new[] { "WellKeeper", "LeatherBoots;LeatherCap", "", "" },
                new[] { "Farmer", "LeatherBoots;LeatherGloves", "", "" },
                new[] { "Scribe", "LeatherGloves;LeatherCap", "", "" },
                new[] { "TentRightHost", "LeatherBoots;LeatherCap", "Dagger", "" },
                new[] { "SaltMaster", "LeatherGloves;LeatherBoots", "", "" },
                new[] { "RecensionScribe", "LeatherGloves;LeatherBoots", "", "" },
                new[] { "StillleafSearcher", "LeatherGloves;LeatherBoots", "", "" },
                new[] { "CurationSorter", "LeatherGloves;LeatherCap", "", "" },
                new[] { "StillleafIndexer", "LeatherGloves;LeatherCap", "", "" },
                new[] { "PeatCutter", "LeatherBoots;LeatherGloves", "Dagger", "" },
                new[] { "GantryRegistrar", "LeatherGloves;LeatherBoots", "", "" },
            };
            foreach (var row in expected)
            {
                var actor = Factory.CreateEntity(row[0]); var kit = actor.GetPart<LoadoutPart>();
                Assert.AreEqual(row[1], kit.Equip, row[0]); Assert.AreEqual(row[2], kit.Carry, row[0]); Assert.AreEqual(row[3], kit.Pick, row[0]);
                CollectionAssert.AreEquivalent(row[1].Split(';'), actor.GetPart<InventoryPart>().EquippedItems.Values.Distinct().Select(i => i.BlueprintName), row[0]);
                CollectionAssert.AreEquivalent(row[2].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), actor.GetPart<InventoryPart>().Objects.Select(i => i.BlueprintName), row[0]);
            }
        }

        Entity RoundTrip(Entity entity)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(entity); writer.WriteQueuedEntityBodies();
                stream.Position = 0; var reader = new SaveReader(stream, Factory);
                var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies(); return loaded;
            }
        }
    }
}
