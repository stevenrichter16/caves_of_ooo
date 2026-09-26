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
    public class DensityHigherTierFindsAdversarialTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() => scope = new DensityLootTestScope();
        [TearDown] public void Cleanup() => scope.Dispose();
        [TestCase("TemperedLongSword")] [TestCase("CounterweightMaul")] [TestCase("FineRingMail")] [TestCase("RivetedPlate")]
        public void ActualEquipAndUnequipPreserveOneItemAndReverseArmor(string name)
        {
            var actor = Factory.CreateEntity("Player"); var item = Factory.CreateEntity(name); int av = CombatSystem.GetAV(actor), dv = CombatSystem.GetDV(actor);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); Assert.IsTrue(InventorySystem.Equip(actor, item));
            Assert.IsTrue(InventorySystem.IsEquipped(actor, item)); Assert.AreEqual(1, DensityLootTestScope.Gear(actor).Count(i => i.ID == item.ID));
            if (item.HasPart<ArmorPart>()) { Assert.AreEqual(av + item.GetPart<ArmorPart>().AV, CombatSystem.GetAV(actor)); Assert.AreEqual(dv + item.GetPart<ArmorPart>().DV, CombatSystem.GetDV(actor)); }
            Assert.IsTrue(InventorySystem.UnequipItem(actor, item)); Assert.IsFalse(InventorySystem.IsEquipped(actor, item));
            Assert.AreEqual(av, CombatSystem.GetAV(actor)); Assert.AreEqual(dv, CombatSystem.GetDV(actor));
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count(i => i.ID == item.ID));
            Assert.AreEqual(0, item.Parts.OfType<IItemEnhancement>().Count(), "authored tiers are not secret random enhancements");
        }
        [TestCase("TemperedLongSword")] [TestCase("CounterweightMaul")] [TestCase("FineRingMail")] [TestCase("RivetedPlate")]
        public void FullContainerCannotInventOrReplaceTheDeepFind(string name)
        {
            var pool = LootTableRegistry.Get("FindDeepEquipmentT4"); foreach (var entry in pool.Entries) entry.Weight = entry.Blueprint == name ? 1 : 0;
            var full = Factory.CreateEntity("LockedChest"); var cp = full.GetPart<ContainerPart>(); cp.MaxItems = 1;
            var sentinel = Factory.CreateEntity("Dagger"); Assert.IsTrue(cp.AddItem(sentinel));
            Assert.AreEqual(0, LootStocker.StockContainer(full, pool.Name, Factory, new Random(1)));
            CollectionAssert.AreEqual(new[] { sentinel }, cp.Contents); Assert.AreSame(full, sentinel.GetPart<PhysicsPart>().InInventory);
            cp.MaxItems = 2; Assert.AreEqual(1, LootStocker.StockContainer(full, pool.Name, Factory, new Random(1)));
            Assert.AreEqual(name, cp.Contents[1].BlueprintName); Assert.AreEqual(1, cp.Contents.Count(i => i.BlueprintName == name));
        }
        [TestCase(false)] [TestCase(true)]
        public void RegistryRejectsUnknownContentOrCycleAndValidDataPasses(bool cycle)
        {
            Assert.IsEmpty(LootTableRegistry.Validate(Factory.Blueprints.ContainsKey));
            var table = LootTableRegistry.Get("FindDeepEquipmentT4"); var row = table.Entries[0];
            if (cycle) { row.Blueprint = null; row.TableRef = "DeepReliquaryT4"; }
            else row.Blueprint = "MissingDeepItem";
            Assert.IsNotEmpty(LootTableRegistry.Validate(Factory.Blueprints.ContainsKey));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualStampedKeyUnlocksItsChestAndAbsentKeyCannotOpenOrTake(bool pickUpKey)
        {
            var stamp = DensityHigherTierFindsTests.Catalog(9, BiomeType.Spread, true).Single(s => s.Name == "Reliquary");
            var zone = new Zone("deep-reliquary-key"); new LandmarkBuilder(BiomeType.Cave, 4, new[] { StampCatalog.Forced(stamp) }).BuildZone(zone, Factory, new Random(12));
            var chest = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "LockedChest");
            var key = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "IronKey");
            var keyPos = zone.GetEntityPosition(key); var chestPos = zone.GetEntityPosition(chest);
            var actor = Factory.CreateEntity("Player"); var probe = new DensityContainerOpenProbe(); actor.AddPart(probe);
            Assert.IsTrue(zone.AddEntity(actor, keyPos.x + 1, keyPos.y));
            // The authored entry/key/chest approaches are floor cells inside the stamp.
            Assert.IsTrue(zone.MoveEntity(actor, keyPos.x, keyPos.y));
            if (pickUpKey) Assert.IsTrue(InventorySystem.Pickup(actor, key, zone));
            Assert.IsTrue(zone.MoveEntity(actor, chestPos.x + 1, chestPos.y + 1));
            Fire(chest, actor, zone, "Unlock"); Assert.AreEqual(!pickUpKey, chest.GetPart<LockPart>().IsLocked);
            Fire(chest, actor, zone, "OpenContainer"); Assert.AreEqual(pickUpKey ? 1 : 0, probe.Opens);
            var reward = chest.GetPart<ContainerPart>().Contents.Single(i => DensityHigherTierFindsTests.Names.Contains(i.BlueprintName));
            Assert.AreEqual(pickUpKey, InventorySystem.TakeFromContainer(actor, chest, reward));
            Assert.AreSame(pickUpKey ? actor : chest, reward.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(pickUpKey, actor.GetPart<InventoryPart>().Objects.Contains(key), "keys are reusable");
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "VaultSentinel"));
        }
        static void Fire(Entity target, Entity actor, Zone zone, string command)
        {
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", (object)actor); e.SetParameter("Zone", (object)zone); e.SetParameter("Command", command); target.FireEventAndRelease(e);
        }
        [TestCase(false)] [TestCase(true)]
        public void AStaleOpenMenuReadsTheCurrentKeyLockAtExecution(bool relock)
        {
            var chest = Factory.CreateEntity("LockedChest"); var actor = Factory.CreateEntity("Player"); var probe = new DensityContainerOpenProbe(); actor.AddPart(probe);
            var keyLock = chest.GetPart<LockPart>(); keyLock.IsLocked = false;
            var actions = new InventoryActionList(); var e = GameEvent.New("GetInventoryActions"); e.SetParameter("Actions", (object)actions); chest.FireEventAndRelease(e);
            Assert.AreEqual(1, actions.Actions.Count(a => a.Command == "OpenContainer")); keyLock.IsLocked = relock;
            Fire(chest, actor, null, "OpenContainer"); Assert.AreEqual(relock ? 0 : 1, probe.Opens);
        }
        [TestCase(false)] [TestCase(true)]
        public void SavedKeyLockRemainsTheComputedAccessAuthority(bool locked)
        {
            var chest = Factory.CreateEntity("LockedChest"); chest.GetPart<LockPart>().IsLocked = locked;
            var item = Factory.CreateEntity("FineRingMail"); chest.GetPart<ContainerPart>().AddItem(item);
            using (var stream = new MemoryStream())
            {
                var w = new SaveWriter(stream); w.WriteEntityReference(chest); w.WriteQueuedEntityBodies(); stream.Position = 0;
                var r = new SaveReader(stream, Factory); var loaded = r.ReadEntityReference(); r.ReadEntityBodies();
                Assert.AreEqual(locked, loaded.GetPart<ContainerPart>().IsLocked); Assert.IsFalse(loaded.GetPart<ContainerPart>().Locked);
                var saved = loaded.GetPart<ContainerPart>().Contents.Single(); Assert.AreEqual(item.ID, saved.ID); Assert.AreSame(loaded, saved.GetPart<PhysicsPart>().InInventory);
                loaded.GetPart<LockPart>().IsLocked = !locked; Assert.AreEqual(!locked, loaded.GetPart<ContainerPart>().IsLocked);
            }
        }
    }
}
