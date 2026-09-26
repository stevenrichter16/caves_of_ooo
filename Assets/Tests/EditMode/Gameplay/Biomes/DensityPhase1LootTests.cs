using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 1 reconnects finished items to ordinary, tiered finds.
    /// These tests start at a live container/death table, rather than
    /// proving only that an otherwise-unreferenced pool can roll items.
    /// Tuning is CoO-original; the source rules are Docs/LOOT-FINDS.md.
    /// </summary>
    [TestFixture]
    public class DensityPhase1LootTests
    {
        private EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprints()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown()
        {
            LootTableRegistry.ResetForTests();
            Diag.ResetAll();
        }

        [TestCase("OldWorldPipe", 1, "FindWeapon")]
        [TestCase("FlamingSword", 2, "FindWeapon")]
        [TestCase("IceSword", 2, "FindWeapon")]
        [TestCase("CryoLance", 2, "FindWeapon")]
        [TestCase("EmberSpear", 2, "FindWeapon")]
        [TestCase("AcidicDagger", 2, "FindWeapon")]
        [TestCase("ThunderHammer", 2, "FindWeapon")]
        [TestCase("DissolutionMaul", 3, "FindWeapon")]
        [TestCase("AcidTonic", 1, "FindOffense")]
        [TestCase("LightningTonic", 1, "FindOffense")]
        [TestCase("FrostTonic", 1, "FindOffense")]
        [TestCase("BleedTonic", 1, "FindOffense")]
        public void StrandedItem_CanBeFoundInAStockedCrate_OnlyWhenFindRollLands(
            string blueprint, int tier, string poolPrefix)
        {
            string tableName = "CrateT" + tier;
            int seed = FindSeedContaining(tableName, blueprint);
            var crate = _factory.CreateEntity("Crate");
            LootStocker.StockContainer(crate, tableName, _factory, new Random(seed));
            Assert.IsTrue(crate.GetPart<ContainerPart>().Contents.Exists(
                e => e.BlueprintName == blueprint), "the rolled item must materialize in a real container");

            // Identical source, with just the new find gate turned off.
            // A disconnected pool or a legacy bypass cannot satisfy this pair.
            var entry = LootTableRegistry.Get(tableName).Entries.Find(
                e => e.TableRef == poolPrefix + "T" + tier);
            Assert.IsNotNull(entry, "live crate must reference the find pool");
            entry.Chance = 0;
            var control = _factory.CreateEntity("Crate");
            LootStocker.StockContainer(control, tableName, _factory, new Random(seed));
            Assert.IsFalse(control.GetPart<ContainerPart>().Contents.Exists(
                e => e.BlueprintName == blueprint), "disabling the find roll removes this source");
        }

        [TestCase("FindWeapon", 1)]
        [TestCase("FindWeapon", 2)]
        [TestCase("FindWeapon", 3)]
        [TestCase("FindOffense", 1)]
        [TestCase("FindOffense", 2)]
        [TestCase("FindOffense", 3)]
        public void FindPools_ProduceOneUsableTierAppropriateItem(string prefix, int tier)
        {
            string name = prefix + "T" + tier;
            var table = LootTableRegistry.Get(name);
            Assert.IsNotNull(table, "tiered pool is shipped");
            Assert.IsTrue(table.PickOne, "one selected item, not every pool member");
            foreach (var blueprint in Reachable(name))
            {
                var item = _factory.CreateEntity(blueprint);
                Assert.IsNotNull(item);
                int itemTier = int.TryParse(item.GetTag("Tier"), out int parsed) ? parsed : 1;
                Assert.LessOrEqual(itemTier, tier, blueprint + " cannot leak into a lower tier");
                if (prefix == "FindWeapon")
                    Assert.IsNotNull(item.GetPart<MeleeWeaponPart>(), blueprint);
                else
                    Assert.IsTrue(item.GetPart<TonicPart>() != null || item.GetPart<GasGrenadePart>() != null, blueprint);
            }
            for (int seed = 0; seed < 64; seed++)
                Assert.AreEqual(1, LootTableRegistry.Roll(name, new Random(seed)).Count);

            // Weight-zero is the counter-check for the weighted selection gate.
            foreach (var entry in table.Entries) entry.Weight = 0;
            Assert.IsEmpty(LootTableRegistry.Roll(name, new Random(4)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ManufacturedSources_UseTieredFindChances_WithoutChangingNaturalSources(int tier)
        {
            AssertFind("CrateT", "FindWeaponT", tier, 20 + 5 * tier);
            AssertFind("UrnT", "FindWeaponT", tier, 10 + 5 * tier);
            AssertFind("BoneCacheT", "FindWeaponT", tier, 20 + 5 * tier);
            AssertFind("StrongBoxT", "FindWeaponT", tier, 15 + 5 * tier);
            AssertFind("ReliquaryT", "FindWeaponT", tier, 10 + 5 * tier);
            AssertFind("CrateT", "FindOffenseT", tier, 15 + 5 * tier);
            AssertFind("StrongBoxT", "FindOffenseT", tier, 15 + 5 * tier);
            AssertFind("ReliquaryT", "FindOffenseT", tier, 5 + 5 * tier);
            AssertFind("AlchemyShelfT", "FindOffenseT", tier, 25 + 5 * tier);
            AssertFind("DeathHumanoidT", "FindOffenseT", tier, 15 + 5 * tier);

            foreach (string prefix in new[] { "SackT", "BasketT", "HollowLogT", "DeathBeastT" })
                Assert.IsFalse(LootTableRegistry.Get(prefix + tier).Entries.Exists(
                    e => e.TableRef != null && e.TableRef.StartsWith("Find")), prefix);
            foreach (var kind in ContainerPlacementService.PoolFor(
                BiomeType.Grovelands, ContainerPlacementService.ZoneKind.Wilderness))
                Assert.IsFalse(Reachable(kind.TablePrefix + tier).Overlaps(GeneralFinds()), kind.TablePrefix);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DeathTables_OfferPocketOffense_ButNeverHiddenWeaponsOrArmor(int tier)
        {
            var humanoid = Reachable("DeathHumanoidT" + tier);
            Assert.IsTrue(humanoid.Contains("AcidTonic"), "offense is available in humanoid pockets");
            foreach (string prefix in new[] { "DeathHumanoidT", "DeathBeastT", "DeathConstructT" })
            {
                if (LootTableRegistry.Get(prefix + tier) == null) continue;
                foreach (string blueprint in Reachable(prefix + tier))
                {
                    var item = _factory.CreateEntity(blueprint);
                    Assert.IsNull(item.GetPart<MeleeWeaponPart>(), prefix + tier + ":" + blueprint);
                    Assert.IsNull(item.GetPart<ArmorPart>(), prefix + tier + ":" + blueprint);
                }
                if (prefix != "DeathHumanoidT")
                    Assert.IsFalse(Reachable(prefix + tier).Contains("AcidTonic"), "animals/constructs get no new offense");
            }
        }

        [Test]
        public void GeneralFinds_ExcludeRentalsCraftedOutputsAndSpecialProvenance()
        {
            var finds = GeneralFinds();
            for (int tier = 1; tier <= 3; tier++) finds.UnionWith(Reachable("WeaponRackT" + tier));
            foreach (string excluded in new[] { "LoanerDagger", "LoanerSpear", "LoanerLongsword",
                "ForgedWeapon", "BrewedTonic", "TemporalShard", "PalimpsestBlade", "SeveranceEdge",
                "FirstRootGlaive", "Sporeblade", "ChoirSpine", "EchoKnife", "GlassblownStiletto" })
                Assert.IsFalse(finds.Contains(excluded), excluded + " keeps its existing source or crafting role");
            Assert.IsTrue(finds.Contains("DissolutionMaul"), "an ordinary finished T3 find is still available");
        }

        [Test]
        public void ShippedLoot_Validates_AndRejectsABrokenNewReference()
        {
            Assert.IsEmpty(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp)));
            var crate = LootTableRegistry.Get("CrateT1");
            crate.Entries.Add(new LootEntryData { TableRef = "MissingDensityFindPool" });
            Assert.IsTrue(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp))
                .Exists(problem => problem.Contains("MissingDensityFindPool")));
        }

        private static int FindSeedContaining(string table, string blueprint)
        {
            for (int seed = 0; seed < 2048; seed++)
                if (LootTableRegistry.Roll(table, new Random(seed)).Contains(blueprint)) return seed;
            Assert.Fail(blueprint + " never rolled from live source " + table + " across 2048 seeds");
            return -1;
        }

        private static void AssertFind(string source, string pool, int tier, int chance)
        {
            var table = LootTableRegistry.Get(source + tier);
            Assert.IsFalse(table.PickOne, "source Chance must be honored");
            var entry = table.Entries.Find(e => e.TableRef == pool + tier);
            Assert.IsNotNull(entry, source + tier + " must reach " + pool + tier);
            Assert.AreEqual(chance, entry.Chance, source + tier);
            Assert.AreEqual(1, entry.MinCount);
            Assert.AreEqual(1, entry.MaxCount);
        }

        private static HashSet<string> GeneralFinds()
        {
            var result = new HashSet<string>();
            for (int tier = 1; tier <= 3; tier++)
            {
                result.UnionWith(Reachable("FindWeaponT" + tier));
                result.UnionWith(Reachable("FindOffenseT" + tier));
            }
            return result;
        }

        private static HashSet<string> Reachable(string name, HashSet<string> visited = null)
        {
            var found = new HashSet<string>();
            visited = visited ?? new HashSet<string>();
            if (!visited.Add(name)) return found;
            var table = LootTableRegistry.Get(name);
            Assert.IsNotNull(table, "reachable table must exist: " + name);
            foreach (var entry in table.Entries)
            {
                if (table.PickOne ? entry.Weight <= 0 : entry.Chance <= 0) continue;
                if (entry.MaxCount <= 0) continue;
                if (!string.IsNullOrEmpty(entry.TableRef)) found.UnionWith(Reachable(entry.TableRef, visited));
                else found.Add(entry.Blueprint);
            }
            return found;
        }
    }
}
