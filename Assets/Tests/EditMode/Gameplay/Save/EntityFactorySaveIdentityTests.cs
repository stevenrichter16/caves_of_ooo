using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class LoadedFactoryIdentityProbePart : Part
    {
        public Entity Created;
        public override void OnAfterLoad(SaveReader reader)
            => Created = reader.Factory.CreateEntity("FreshItem");
    }

    public sealed class EntityFactorySaveIdentityTests
    {
        static EntityFactory Factory()
        {
            var factory = new EntityFactory();
            factory.Blueprints.Add("FreshItem", new Blueprint { Name = "FreshItem" });
            return factory;
        }

        static byte[] SaveGraph(Entity owner)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            writer.WriteEntityReference(owner);
            writer.WriteQueuedEntityBodies();
            return stream.ToArray();
        }

        static Entity LoadGraph(byte[] bytes, EntityFactory factory)
        {
            using var stream = new MemoryStream(bytes);
            var reader = new SaveReader(stream, factory);
            var owner = reader.ReadEntityReference();
            reader.ReadEntityBodies();
            return owner;
        }

        static long Numeric(Entity owner)
        {
            Assert.True(long.TryParse(owner.ID, out long value), owner.ID);
            return value;
        }

        [Test] public void LoadedNumericIdentityAdvancesANewFactoryWithoutRewritingTheOwner()
        {
            var factory = Factory();
            var source = new Entity { ID = "7197", BlueprintName = "FreshItem" };
            var loaded = LoadGraph(SaveGraph(source), factory);
            Assert.AreEqual("7197", loaded.ID);
            Assert.AreEqual("7198", factory.CreateEntity("FreshItem").ID);
            Assert.AreEqual("7199", factory.CreateEntity("FreshItem").ID);
            Assert.AreEqual("7197", source.ID);
        }

        [Test] public void APreviouslyHigherLiveFactoryNeverRewindsAcrossRepeatedLoads()
        {
            var factory = Factory();
            for (int i = 0; i < 8; i++) factory.CreateEntity("FreshItem");
            byte[] lower = SaveGraph(new Entity { ID = "3" });
            Assert.AreEqual("3", LoadGraph(lower, factory).ID);
            Assert.AreEqual("9", factory.CreateEntity("FreshItem").ID);
            LoadGraph(lower, factory);
            Assert.AreEqual("10", factory.CreateEntity("FreshItem").ID);
        }

        [TestCase("custom:item/7197")]
        [TestCase("f72c8cc150b44a28a15bb5c56d40d806")]
        [TestCase("-5")]
        public void OpaqueSavedIdentityIsUnchangedAndDoesNotResetTheNumericAllocator(string id)
        {
            var factory = Factory();
            Assert.AreEqual("1", factory.CreateEntity("FreshItem").ID);
            Assert.AreEqual(id, LoadGraph(SaveGraph(new Entity { ID = id }), factory).ID);
            Assert.AreEqual("2", factory.CreateEntity("FreshItem").ID);
        }

        [Test] public void AllReferencedBodiesAreReservedBeforeTheFirstLoadHookAllocates()
        {
            var root = new Entity { ID = "7" };
            root.AddPart(new LoadedFactoryIdentityProbePart());
            var inventory = new InventoryPart(); root.AddPart(inventory);
            var bag = new Entity { ID = "12" };
            var contents = new ContainerPart(); bag.AddPart(contents);
            contents.Contents.Add(new Entity { ID = "8100" });
            inventory.Objects.Add(bag);
            inventory.EquippedItems.Add("Hand", new Entity { ID = "9200" });
            var loaded = LoadGraph(SaveGraph(root), Factory());
            Assert.AreEqual("9201", loaded.GetPart<LoadedFactoryIdentityProbePart>().Created.ID);
            Assert.AreEqual("8100", loaded.GetPart<InventoryPart>().Objects[0].GetPart<ContainerPart>().Contents[0].ID);
        }

        [Test] public void ARejectedTokenGraphCannotAdvanceTheBorrowedLiveFactory()
        {
            var factory = Factory();
            Assert.AreEqual("1", factory.CreateEntity("FreshItem").ID);
            byte[] bytes = SaveGraph(new Entity { ID = "8000" });
            bytes[bytes.Length - 1] ^= 0x40;
            Assert.Throws<InvalidDataException>(() => LoadGraph(bytes, factory));
            Assert.AreEqual("2", factory.CreateEntity("FreshItem").ID);
        }

        [TestCase(false)] [TestCase(true)]
        public void SessionReservesInactiveZoneContainerOnlyAfterTheFinalParseCheck(bool corrupt)
        {
            using var globals = new HotbarSaveFixture(false, false);
            var state = HotbarSaveFixture.MakeState(0);
            var remote = new Zone("Overworld.9.9.0");
            var chest = new Entity { ID = "500", BlueprintName = "SavedChest" };
            var contents = new ContainerPart(); chest.AddPart(contents);
            contents.Contents.Add(new Entity { ID = "9000", BlueprintName = "SavedGoods" });
            Assert.True(remote.AddEntity(chest, 5, 5));
            state.ZoneManager.CachedZones.Add(remote.ZoneID, remote);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            byte[] bytes = stream.ToArray();
            if (corrupt) bytes[bytes.Length - 1] ^= 0x40;
            var factory = Factory();
            Assert.AreEqual("1", factory.CreateEntity("FreshItem").ID);
            using var input = new MemoryStream(bytes);
            if (corrupt)
            {
                Assert.Throws<InvalidDataException>(() => GameSessionState.Load(new SaveReader(input, factory)));
                Assert.AreEqual("2", factory.CreateEntity("FreshItem").ID);
            }
            else
            {
                var loaded = GameSessionState.Load(new SaveReader(input, factory));
                Assert.AreEqual("9000", loaded.ZoneManager.CachedZones[remote.ZoneID].GetCell(5, 5)
                    .Objects.Single().GetPart<ContainerPart>().Contents.Single().ID);
                Assert.Greater(Numeric(factory.CreateEntity("FreshItem")), 9000);
            }
        }

        [TestCase("2147483646")] [TestCase("2147483647")]
        public void NumericExhaustionCannotWrapOrReissueTheSavedMaximum(string id)
        {
            var factory = Factory();
            Assert.AreEqual(id, LoadGraph(SaveGraph(new Entity { ID = id }), factory).ID);
            string first = factory.CreateEntity("FreshItem").ID;
            string second = factory.CreateEntity("FreshItem").ID;
            Assert.True(Guid.TryParseExact(first, "N", out _), first);
            Assert.True(Guid.TryParseExact(second, "N", out _), second);
            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(id, first);
        }

        [Test] public void ContinuedHarvestKeepsTheRealConcentrationActionAndExactPayment()
        {
            using var globals = new HotbarSaveFixture(false, false);
            EntityFactory beforeHarvest = HarvestablePart.Factory;
            try
            {
                string json = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json"));
                var oldFactory = new EntityFactory(); oldFactory.LoadBlueprints(json);
                oldFactory.CreateEntity("MendleafSprig"); // First historical allocation is no longer in the save.
                var carrot = oldFactory.CreateEntity("CandyCarrot");
                Assert.AreEqual("2", carrot.ID);
                var actor = new Entity { ID = "saved-player", BlueprintName = "Player" };
                actor.SetTag("Player"); actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart());
                Assert.True(actor.GetPart<InventoryPart>().AddObject(carrot));
                var factory = new EntityFactory(); factory.LoadBlueprints(json);
                var loaded = LoadGraph(SaveGraph(actor), factory);
                var zone = new Zone("saved-identity-preparation");
                Assert.True(zone.AddEntity(loaded, 10, 10)); SettlementRuntime.ActiveZone = zone;
                HarvestablePart.Factory = SeedPart.Factory = factory;
                var plant = factory.CreateEntity("MendleafPlant");
                plant.GetPart<HarvestablePart>().YieldMin = plant.GetPart<HarvestablePart>().YieldMax = 2;
                Assert.True(zone.AddEntity(plant, 11, 10));
                Assert.True(InventorySystem.PerformAction(loaded, plant, "Harvest", zone));
                var pack = loaded.GetPart<InventoryPart>();
                var sprig = pack.Objects.Single(e => e.BlueprintName == "MendleafSprig");
                Assert.AreEqual(2, sprig.GetPart<StackerPart>().StackCount);
                Assert.True(zone.AddEntity(factory.CreateEntity("AlchemyStill"), 10, 9));
                const string command = "PrepareRecipe|concentrate_mendleaf";
                Assert.True(InventorySystem.GetActions(loaded, sprig).Any(a => a.Command == command),
                    "Continued-game harvest must remain discoverable through the real inventory action list.");
                Assert.AreEqual(pack.Objects.Count, pack.Objects.Select(e => e.ID).Distinct().Count());
                Assert.True(InventorySystem.PerformAction(loaded, sprig, command, zone));
                Assert.False(pack.Objects.Any(e => e.BlueprintName == "MendleafSprig"));
                Assert.AreEqual(1, pack.Objects.Single(e => e.BlueprintName == "ConcentratedMendleaf").GetPart<StackerPart>().StackCount);
                Assert.AreEqual("2", pack.Objects.Single(e => e.BlueprintName == "CandyCarrot").ID);
                Assert.False(InventorySystem.PerformAction(loaded, sprig, command, zone), "Spent source stays rejected.");
            }
            finally { HarvestablePart.Factory = beforeHarvest; }
        }
    }
}
