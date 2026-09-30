using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent read ownership, physical-contact, persistence,
    /// malformed catalog and no-gameplay-side-effect probes.</summary>
    public sealed class DensityReadableDocumentAdversarialTests
    {
        EntityFactory factory;
        Entity actor;
        Zone zone;
        System.Random previousLoadoutRng;
        bool previousEventChannel;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            actor = factory.CreateEntity("Player"); actor.ID = "read-actor-" + Guid.NewGuid().ToString("N");
            zone = new Zone("read-adversarial"); Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            previousLoadoutRng = LoadoutPart.Rng; previousEventChannel = Diag.IsChannelEnabled("event");
            Diag.SetChannel("event", true); ReadableDocumentCatalog.ResetForTests(); MessageLog.Clear();
        }
        [TearDown] public void TearDown()
        {
            LoadoutPart.Rng = previousLoadoutRng; Diag.SetChannel("event", previousEventChannel);
            ReadableDocumentCatalog.ResetForTests(); MessageLog.Clear();
        }
        Entity Book(bool carried = true)
        {
            var book = factory.CreateEntity("Codex01"); book.ID = "read-book-" + Guid.NewGuid().ToString("N");
            if (carried) Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(book));
            return book;
        }
        bool Read(Entity book, Entity reader = null, Zone location = null)
            => book.GetPart<ReadableDocumentPart>().TryRead(reader ?? actor, location ?? zone);
        static void Load(string json) => typeof(ReadableDocumentCatalog).GetMethod("Load",
            BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { json });
        static string Row(string id = "a", string blueprint = "BookA", string title = "A title",
            string text = "A complete text.", string source = "Lore/Codex/test.md")
            => "{\"Id\":" + Quote(id) + ",\"Blueprint\":" + Quote(blueprint) + ",\"Title\":" + Quote(title)
                + ",\"Text\":" + Quote(text) + ",\"Source\":" + Quote(source) + "}";
        static string Quote(string s) => s == null ? "null" : "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
        static void Rows(params string[] rows) => Load("{\"Documents\":[" + string.Join(",", rows) + "]}");

        [TestCase("null")] [TestCase("zero-hp")] [TestCase("death-committed")]
        public void Adversarial_InvalidReaderCannotReadEvenTheirCarriedBook(string reason)
        {
            var book = Book(); var reader = actor;
            if (reason == "null") reader = null;
            if (reason == "zero-hp") actor.Statistics["Hitpoints"].BaseValue = 0;
            if (reason == "death-committed") actor.SetTag("_DeathHandled");
            Assert.IsFalse(book.GetPart<ReadableDocumentPart>().TryRead(reader, zone));
            Assert.IsFalse(MessageLog.HasPendingAnnouncement); Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(book));
        }
        [Test] public void Adversarial_DetachedReadablePartRejectsWithoutThrowing()
        {
            Assert.IsFalse(new ReadableDocumentPart { DocumentId = "codex-01" }.TryRead(actor, zone));
            Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_TransferredBookUsesCurrentInventoryOwner()
        {
            var book = Book(); var other = factory.CreateEntity("Player");
            WorldInteractionSystem.GatherActions(book, actor);
            Assert.IsTrue(actor.GetPart<InventoryPart>().RemoveObject(book)); Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(book));
            Assert.IsFalse(Read(book)); Assert.IsFalse(MessageLog.HasPendingAnnouncement);
            Assert.IsTrue(Read(book, other)); Assert.IsTrue(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_PhysicsBackreferenceAloneDoesNotConferOwnership()
        {
            var book = Book(false); book.GetPart<PhysicsPart>().InInventory = actor;
            Assert.IsFalse(Read(book)); Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_RemovedWorldBookRefusesAfterBuildingMenu()
        {
            var book = Book(false); zone.AddEntity(book, 11, 10); WorldInteractionSystem.GatherActions(book, actor);
            zone.RemoveEntity(book); Assert.IsFalse(Read(book)); Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_ForeignZoneCannotReadSameCoordinates()
        {
            var book = Book(false); var foreign = new Zone("foreign"); foreign.AddEntity(book, 11, 10);
            Assert.IsFalse(Read(book)); Assert.IsFalse(book.GetPart<ReadableDocumentPart>().TryRead(actor, foreign));
            Assert.IsFalse(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_NullZoneInfersPhysicalZoneButWrongExplicitZoneDoesNot()
        {
            var book = Book(false); zone.AddEntity(book, 11, 10);
            Assert.IsFalse(book.GetPart<ReadableDocumentPart>().TryRead(actor, new Zone("wrong")));
            Assert.IsTrue(book.GetPart<ReadableDocumentPart>().TryRead(actor, null));
        }
        [Test] public void Adversarial_CarriedBookIsReadableWithoutAWorldZone()
        {
            var book = Book(); zone.RemoveEntity(actor);
            Assert.IsTrue(book.GetPart<ReadableDocumentPart>().TryRead(actor, null));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(book));
        }
        [Test] public void Adversarial_EquippedReadingCopyUsesActualEquipmentMembership()
        {
            var book = Book(); book.AddPart(new EquippablePart { Slot = "Hand" });
            Assert.IsTrue(InventorySystem.Equip(actor, book));
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(book));
            Assert.IsTrue(Read(book)); Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(book));
        }
        [TestCase("0,0;1,0;2,0", 13, true)] [TestCase("3,0", 11, false)]
        public void Adversarial_WorldReadingUsesPhysicalContactInsteadOfSaveAnchor(string footprint, int bookX, bool expected)
        {
            zone.RemoveEntity(actor); actor.AddPart(new SpatialFootprintPart { CellsRaw = footprint });
            Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            var book = Book(false); Assert.IsTrue(zone.AddEntity(book, bookX, 10));
            Assert.AreEqual(expected, Read(book), "A nearby physical body, not an arbitrary save anchor, determines reach.");
            Assert.AreEqual(expected, MessageLog.HasPendingAnnouncement);
        }
        [Test] public void Adversarial_RereadingPreservesStacksStateAndRandomStream()
        {
            var book = Book(false); book.GetPart<StackerPart>().StackCount = 3;
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(book));
            actor.Properties["protected-mystery"] = "unresolved"; actor.IntProperties["quest-stage"] = 7;
            var hp = actor.GetStatValue("Hitpoints"); var stats = actor.Statistics.ToDictionary(k => k.Key, v => v.Value.Value);
            var props = new Dictionary<string, string>(actor.Properties); var flags = new Dictionary<string, int>(actor.IntProperties);
            var rng = new CountingRandom(); LoadoutPart.Rng = rng;
            var text = ReadableDocumentCatalog.Get("codex-01");
            for (int i = 0; i < 3; i++) Assert.IsTrue(Read(book));
            for (int i = 0; i < 3; i++) Assert.AreEqual(text.Title + "\n\n" + text.Text, MessageLog.ConsumeAnnouncement());
            Assert.IsFalse(MessageLog.HasPendingAnnouncement); Assert.AreEqual(3, book.GetPart<StackerPart>().StackCount);
            CollectionAssert.AreEquivalent(props, actor.Properties); CollectionAssert.AreEquivalent(flags, actor.IntProperties);
            CollectionAssert.AreEquivalent(stats, actor.Statistics.ToDictionary(k => k.Key, v => v.Value.Value));
            Assert.AreEqual(hp, actor.GetStatValue("Hitpoints")); Assert.AreEqual(0, rng.Calls);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(book));
        }
        sealed class CountingRandom : System.Random
        {
            public int Calls;
            public override int Next() { Calls++; return 0; }
            public override int Next(int maxValue) { Calls++; return 0; }
            public override int Next(int minValue, int maxValue) { Calls++; return minValue; }
            public override double NextDouble() { Calls++; return 0; }
        }
        [Test] public void Adversarial_SaveRestoresOwnershipAndStableDocumentIdWithoutReadFlags()
        {
            var book = Book(); Assert.IsTrue(Read(book)); MessageLog.Clear();
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var restored = loaded.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Codex01");
            Assert.AreEqual("codex-01", restored.GetPart<ReadableDocumentPart>().DocumentId);
            Assert.IsTrue(restored.GetPart<ReadableDocumentPart>().TryRead(loaded, null));
            Assert.AreEqual(ReadableDocumentCatalog.Get("codex-01").Title + "\n\n" + ReadableDocumentCatalog.Get("codex-01").Text,
                MessageLog.ConsumeAnnouncement());
            CollectionAssert.AreEquivalent(actor.IntProperties, loaded.IntProperties);
        }
        [Test] public void Adversarial_CatalogReloadDoesNotLeakStaleTextAfterMenuBuilt()
        {
            var book = Book(); WorldInteractionSystem.GatherActions(book, actor);
            Rows(Row()); Assert.IsFalse(Read(book)); Assert.IsFalse(MessageLog.HasPendingAnnouncement);
            ReadableDocumentCatalog.ResetForTests(); Assert.IsTrue(Read(book));
        }
        [Test] public void Adversarial_ReadAndRefusalDiagnosticsIdentifyActualReaderAndBook()
        {
            var book = Book(); Assert.IsTrue(Read(book)); actor.GetPart<InventoryPart>().RemoveObject(book); Assert.IsFalse(Read(book));
            var success = DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "DocumentRead", Actor = actor.ID, Target = book.ID }).Records;
            var failure = DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "DocumentReadRejected", Actor = actor.ID, Target = book.ID }).Records;
            Assert.AreEqual(1, success.Count); Assert.AreEqual(1, failure.Count);
            StringAssert.Contains("codex-01", success[0].PayloadJson); StringAssert.Contains("inaccessible-document", failure[0].PayloadJson);
        }

        [TestCase(null)] [TestCase("")] [TestCase("{")] [TestCase("{}")] [TestCase("{\"Documents\":null}")]
        public void Adversarial_MissingOrMalformedCatalogReportsIssuesWithoutStaleEntries(string json)
        {
            var canonical = Enumerable.Range(1, 13).Select(i => "Codex" + i.ToString("00")).ToArray();
            var local = new[] { "CurationTransferDocket", "CurationDiscrepancyReport" };
            CollectionAssert.AreEquivalent(canonical.Concat(local), ReadableDocumentCatalog.All.Select(entry => entry.Blueprint));
            foreach (var entry in ReadableDocumentCatalog.All.Where(entry => canonical.Contains(entry.Blueprint)))
                StringAssert.StartsWith("Lore/Codex/", entry.Source);
            foreach (var entry in ReadableDocumentCatalog.All.Where(entry => local.Contains(entry.Blueprint)))
                StringAssert.Contains("original Marrowstye receiving record", entry.Source);
            Load(json);
            Assert.IsEmpty(ReadableDocumentCatalog.All); Assert.IsNotEmpty(ReadableDocumentCatalog.Validate());
            Assert.IsNull(ReadableDocumentCatalog.Get("codex-01"));
            foreach (string id in local) Assert.IsNull(ReadableDocumentCatalog.Get(id), "Malformed replacement must also clear authored local records.");
        }
        [TestCase("Id")] [TestCase("Blueprint")] [TestCase("Title")] [TestCase("Text")] [TestCase("Source")]
        public void Adversarial_MissingFieldSkipsOnlyThatRow(string field)
        {
            var row = Row(id: field == "Id" ? " " : "bad", blueprint: field == "Blueprint" ? null : "BadBook",
                title: field == "Title" ? " " : "Bad", text: field == "Text" ? "" : "Text", source: field == "Source" ? "\t" : "Source");
            Rows(row, Row()); Assert.AreEqual(1, ReadableDocumentCatalog.All.Count);
            Assert.AreEqual("a", ReadableDocumentCatalog.All[0].Id); Assert.AreEqual(1, ReadableDocumentCatalog.Validate().Count);
        }
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_DuplicatedIdentityCannotOverwriteFirstValidArtifact(bool duplicateId)
        {
            Rows(Row(), Row(id: duplicateId ? "a" : "b", blueprint: duplicateId ? "BookB" : "BookA", text: "Replacement"), Row("c", "BookC"));
            Assert.AreEqual(2, ReadableDocumentCatalog.All.Count); Assert.AreEqual(1, ReadableDocumentCatalog.Validate().Count);
            Assert.AreEqual("A complete text.", ReadableDocumentCatalog.Get("a").Text);
            Assert.IsNotNull(ReadableDocumentCatalog.Get("c"));
        }
        [Test] public void Adversarial_InvalidRowDoesNotReserveItsIdOrBlueprint()
        {
            Rows("null", Row(text: ""), Row()); Assert.AreEqual(1, ReadableDocumentCatalog.All.Count);
            Assert.IsNotNull(ReadableDocumentCatalog.Get("a")); Assert.AreEqual(2, ReadableDocumentCatalog.Validate().Count);
        }
        [Test] public void Adversarial_ReadOnlyCatalogAndValidationCopyCannotMutateCache()
        {
            Rows(Row()); var list = (IList<ReadableDocumentCatalog.Document>)ReadableDocumentCatalog.All;
            Assert.Throws<NotSupportedException>(() => list.Clear()); Assert.AreEqual(1, ReadableDocumentCatalog.All.Count);
            Load("{}"); var copy = (string[])ReadableDocumentCatalog.Validate(); copy[0] = "tampered";
            Assert.AreNotEqual("tampered", ReadableDocumentCatalog.Validate()[0]);
        }
        [Test] public void Adversarial_UnicodeAndParagraphsRoundTripWithoutMarkupRewriting()
        {
            const string text = "a\n\nβ — ‘quoted’\n  indented\tend";
            Rows(Row(text: text)); Assert.AreEqual(text, ReadableDocumentCatalog.Get("a").Text);
            Assert.IsNull(ReadableDocumentCatalog.Get(null)); Assert.IsNull(ReadableDocumentCatalog.Get("missing"));
        }
    }
}
