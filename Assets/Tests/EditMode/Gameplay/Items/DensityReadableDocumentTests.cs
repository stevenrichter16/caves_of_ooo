using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityReadableDocumentTests
    {
        private EntityFactory factory;
        private Entity actor;
        private Zone zone;

        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            actor = factory.CreateEntity("Player");
            zone = new Zone("Documents.Test");
            Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            ReadableDocumentCatalog.ResetForTests();
            MessageLog.Clear();
        }

        [TearDown] public void Cleanup()
        { ReadableDocumentCatalog.ResetForTests(); MessageLog.Clear(); }

        [TestCase("01")][TestCase("02")][TestCase("03")][TestCase("04")]
        [TestCase("05")][TestCase("06")][TestCase("07")][TestCase("08")]
        [TestCase("09")][TestCase("10")][TestCase("11")][TestCase("12")][TestCase("13")]
        public void FactoryBook_HasCompleteReadActionAndCanBeReread(string suffix)
        {
            var entry = ReadableDocumentCatalog.Get("codex-" + suffix);
            Assert.IsNotNull(entry);
            var book = factory.CreateEntity(entry.Blueprint);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(book));
            var part = book.GetPart<ReadableDocumentPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(entry.Id, part.DocumentId);
            Assert.AreEqual(1, WorldInteractionSystem.GatherActions(book, actor)
                .Count(a => a.Command == "ReadDocument"));
            int hp = actor.GetStatValue("Hitpoints");
            int properties = actor.Properties.Count;
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(part.TryRead(actor, zone));
                string message = MessageLog.ConsumeAnnouncement();
                Assert.AreEqual(entry.Title + "\n\n" + entry.Text, message);
                Assert.Greater(message.Length, 150);
                Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(book));
            }
            Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(properties, actor.Properties.Count);
        }

        [Test] public void Catalog_HasThirteenCanonicalCopiesAndTwoDistinctLocalRecords()
        {
            Assert.AreEqual(15, ReadableDocumentCatalog.All.Count);
            CollectionAssert.IsEmpty(ReadableDocumentCatalog.Validate());
            Assert.AreEqual(15, ReadableDocumentCatalog.All.Select(x => x.Id).Distinct().Count());
            Assert.AreEqual(15, ReadableDocumentCatalog.All.Select(x => x.Blueprint).Distinct().Count());
            var canonical = Enumerable.Range(1, 13).Select(i => "Codex" + i.ToString("00")).ToArray();
            CollectionAssert.AreEquivalent(canonical.Concat(new[] { "CurationTransferDocket", "CurationDiscrepancyReport" }),
                ReadableDocumentCatalog.All.Select(x => x.Blueprint));
            foreach (var entry in ReadableDocumentCatalog.All)
            {
                if (canonical.Contains(entry.Blueprint)) StringAssert.StartsWith("Lore/Codex/", entry.Source);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Title));
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Source));
                Assert.Greater(entry.Text.Length, 100);
            }
        }

        [TestCase(0, true)][TestCase(1, true)][TestCase(2, false)][TestCase(8, false)]
        public void WorldRead_RequiresActualNearbyMembership(int distance, bool allowed)
        {
            var book = factory.CreateEntity("Codex01");
            Assert.IsTrue(zone.AddEntity(book, 10 + distance, 10));
            Assert.AreEqual(allowed, book.GetPart<ReadableDocumentPart>().TryRead(actor, zone));
            Assert.AreEqual(allowed, MessageLog.HasPendingAnnouncement);
            Assert.IsNotNull(zone.GetEntityCell(book));
        }

        [Test] public void RemovedBook_RefusesAfterActionMenuWasBuilt()
        {
            var book = factory.CreateEntity("Codex01");
            var inventory = actor.GetPart<InventoryPart>();
            Assert.IsTrue(inventory.AddObject(book));
            Assert.IsTrue(WorldInteractionSystem.GatherActions(book, actor).Any(a => a.Command == "ReadDocument"));
            inventory.RemoveObject(book);
            Assert.IsFalse(book.GetPart<ReadableDocumentPart>().TryRead(actor, zone));
            Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }

        [Test] public void OrdinaryItem_OffersNoDocumentAction()
        {
            var dagger = factory.CreateEntity("Dagger");
            Assert.IsFalse(WorldInteractionSystem.GatherActions(dagger, actor).Any(a => a.Command == "ReadDocument"));
        }

        [Test] public void UnknownDocument_RefusesWithoutInventingText()
        {
            var book = factory.CreateEntity("Codex01");
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(book));
            book.GetPart<ReadableDocumentPart>().DocumentId = "not-a-document";
            Assert.IsFalse(book.GetPart<ReadableDocumentPart>().TryRead(actor, zone));
            Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }

        [Test] public void InventoryAction_DispatchesReadAndLeavesOtherCommandsAlone()
        {
            var book = factory.CreateEntity("Codex01");
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(book));
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Actor", (object)actor); e.SetParameter("Zone", (object)zone);
            e.SetParameter("Command", "ReadDocument");
            Assert.IsFalse(book.FireEventAndRelease(e));
            Assert.IsTrue(MessageLog.HasPendingAnnouncement);
            MessageLog.Clear();
            e = GameEvent.New("InventoryAction"); e.SetParameter("Command", "NotRead");
            Assert.IsTrue(book.GetPart<ReadableDocumentPart>().HandleEvent(e)); e.Release();
            Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }
    }
}
