// Milestone B native RED: the coordinator runs this before production changes.
// New APIs are reached by reflection so missing contracts fail assertions, not compilation.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal static class ExpeditionReportDraftContract
    {
        internal const string Prefix = "SpreadDiscoveryNote.v2:";
        internal const string World = "0123456789abcdef0123456789abcdef";
        internal const string ForeignWorld = "fedcba9876543210fedcba9876543210";
        internal const string WorksFamily = "sodden-peat-works";
        internal const string WorksSubject = "peat-mallet";
        const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal static Type RecordType => typeof(SpreadDiscoveryNotes).GetNestedType("Record", BindingFlags.NonPublic);

        internal static object Record(string destination = "Overworld.11.9.0", string family = "field-alembic",
            string world = World, string informant = "Mera", string subject = "place")
        {
            Assert.NotNull(RecordType, "Extend the current saved record; no second quest framework is needed.");
            var record = Activator.CreateInstance(RecordType, true);
            Set(record, "Version", 2); Set(record, "WorldKey", world); Set(record, "Subject", subject);
            Set(record, "Family", family); Set(record, "OriginZoneID", "Overworld.11.8.0");
            Set(record, "DestinationZoneID", destination); Set(record, "InformantID", "keeper");
            Set(record, "InformantName", informant); Set(record, "Formation", family == WorksFamily ? "SoddenDistrict" : "OldRoad");
            return record;
        }
        internal static void Set(object record, string field, object value)
        {
            var member = RecordType.GetField(field, Hidden);
            Assert.NotNull(member, "Missing v2 field " + field + "; first native run should expose the absent contract.");
            member.SetValue(record, value);
        }
        internal static bool Remember(Entity player, object record, string currentWorld, out string refusal)
        {
            var method = typeof(SpreadDiscoveryNotes).GetMethod("Remember", Hidden, null,
                new[] { typeof(Entity), RecordType, typeof(string), typeof(string).MakeByRefType() }, null);
            Assert.NotNull(method, "Missing explicit-world v2 write boundary.");
            object[] args = { player, record, currentWorld, null };
            bool result = (bool)method.Invoke(null, args); refusal = (string)args[3]; return result;
        }
        internal static string[] Read(Entity player, string currentWorld)
        {
            var method = typeof(SpreadDiscoveryNotes).GetMethod("Read", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Entity), typeof(string) }, null);
            Assert.NotNull(method, "Missing explicit-world historical reader.");
            return ((IEnumerable<string>)method.Invoke(null, new object[] { player, currentWorld })).ToArray();
        }
        internal static void Put(Entity player, object record, string currentWorld = World)
            => Assert.True(Remember(player, record, currentWorld, out var refusal), refusal);
        internal static Dictionary<string, string> Snapshot(Entity player)
            => player.Properties.ToDictionary(p => p.Key, p => p.Value);
        internal static void Unchanged(Entity player, Dictionary<string, string> before)
            => CollectionAssert.AreEquivalent(before, player.Properties);
        internal static Entity RoundTrip(Entity player)
        {
            using var stream = new MemoryStream();
            SaveGraphSerializer.SaveEntityBody(player, new SaveWriter(stream)); stream.Position = 0;
            var copy = new Entity(); SaveGraphSerializer.LoadEntityBody(copy, new SaveReader(stream, null)); return copy;
        }
        internal static string LegacyWire => "{\"Version\":1,\"Family\":\"field-alembic\",\"OriginZoneID\":\"Overworld.11.8.0\","
            + "\"DestinationZoneID\":\"Overworld.11.9.0\",\"InformantID\":\"keeper\",\"InformantName\":\"old keeper\",\"Formation\":\"OldRoad\"}";
    }

    public sealed class ExpeditionReportLedgerTests
    {
        [Test]
        public void TwoSameFamilyDestinationsSurviveAndRehearingChangesOnlyTheSelectedPlace()
        {
            var player = new Entity();
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record());
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record("Overworld.12.9.0"));
            var before = ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World);
            Assert.AreEqual(2, before.Length);
            Assert.That(before.Single(n => n.Contains("Destination (11,9)")), Does.Contain("Mera"));
            string other = before.Single(n => n.Contains("Destination (12,9)"));
            var update = ExpeditionReportDraftContract.Record(informant: "new keeper");
            ExpeditionReportDraftContract.Set(update, "OriginZoneID", "Overworld.12.8.0");
            ExpeditionReportDraftContract.Put(player, update);
            var after = ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World);
            Assert.AreEqual(2, after.Length);
            Assert.That(after.Single(n => n.Contains("Destination (11,9)")), Does.Contain("new keeper").And.Contain("(12,8)"));
            Assert.Contains(other, after);
            Assert.AreEqual(2, player.Properties.Keys.Count(k => k.StartsWith(ExpeditionReportDraftContract.Prefix, StringComparison.Ordinal)));
        }

        [Test]
        public void NativeEntitySaveKeepsV1BytesAndBothV2DestinationsWithoutRewritingOnRead()
        {
            var player = new Entity(); string legacyKey = SpreadDiscoveryNotes.Prefix + "field-alembic";
            player.Properties[legacyKey] = ExpeditionReportDraftContract.LegacyWire;
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record("Overworld.12.9.0"));
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record("Overworld.13.9.0"));
            var copy = ExpeditionReportDraftContract.RoundTrip(player); var before = ExpeditionReportDraftContract.Snapshot(copy);
            CollectionAssert.AreEquivalent(ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World),
                ExpeditionReportDraftContract.Read(copy, ExpeditionReportDraftContract.World));
            Assert.AreEqual(3, ExpeditionReportDraftContract.Read(copy, ExpeditionReportDraftContract.World).Length);
            Assert.AreEqual(ExpeditionReportDraftContract.LegacyWire, copy.Properties[legacyKey]);
            Assert.That(SpreadDiscoveryNotes.Read(copy).Single(), Does.Contain("old keeper"), "The v1 API remains source-compatible.");
            ExpeditionReportDraftContract.Unchanged(copy, before);
        }

        [TestCase(null)][TestCase("")][TestCase("same-seed-is-not-identity")]
        [TestCase(ExpeditionReportDraftContract.ForeignWorld)]
        public void InvalidOrForeignCurrentWorldCannotReadOrWriteV2(string current)
        {
            var player = new Entity(); var record = ExpeditionReportDraftContract.Record();
            ExpeditionReportDraftContract.Put(player, record);
            var before = ExpeditionReportDraftContract.Snapshot(player);
            Assert.IsEmpty(ExpeditionReportDraftContract.Read(player, current));
            Assert.False(ExpeditionReportDraftContract.Remember(player, record, current, out _));
            ExpeditionReportDraftContract.Unchanged(player, before);
        }

        [TestCase("WorldKey", "not-a-guid")][TestCase("Subject", "invented-reward")]
        [TestCase("Family", "invented-family")][TestCase("OriginZoneID", "Overworld.011.8.0")]
        [TestCase("DestinationZoneID", "Overworld.20.9.0")][TestCase("DestinationZoneID", "Overworld.11.9.1")]
        [TestCase("Formation", "invented-formation")][TestCase("InformantName", "name\nforged-line")]
        [TestCase("InformantID", "")]
        public void MalformedRecordIsRejectedBesideARealAcceptedControl(string field, string value)
        {
            var player = new Entity(); ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record());
            var record = ExpeditionReportDraftContract.Record("Overworld.12.9.0");
            ExpeditionReportDraftContract.Set(record, field, value); var before = ExpeditionReportDraftContract.Snapshot(player);
            Assert.False(ExpeditionReportDraftContract.Remember(player, record, ExpeditionReportDraftContract.World, out _));
            Assert.AreEqual(1, ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World).Length);
            ExpeditionReportDraftContract.Unchanged(player, before);
        }

        [Test]
        public void PeatSubjectCannotBeAttachedToAnUnrelatedFamilyOrDestination()
        {
            var player = new Entity();
            var good = ExpeditionReportDraftContract.Record(SoddenDistrictPlan.WorksZoneID,
                ExpeditionReportDraftContract.WorksFamily, subject: ExpeditionReportDraftContract.WorksSubject);
            ExpeditionReportDraftContract.Put(player, good);
            var other = ExpeditionReportDraftContract.Record("Overworld.12.9.0",
                ExpeditionReportDraftContract.WorksFamily, subject: ExpeditionReportDraftContract.WorksSubject);
            Assert.False(ExpeditionReportDraftContract.Remember(player, other, ExpeditionReportDraftContract.World, out _));
            other = ExpeditionReportDraftContract.Record(subject: ExpeditionReportDraftContract.WorksSubject);
            Assert.False(ExpeditionReportDraftContract.Remember(player, other, ExpeditionReportDraftContract.World, out _));
            Assert.AreEqual(1, ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World).Length);
        }

        [Test]
        public void FullLedgerRefusesANewPlaceButCanUpdateAnExistingRecordWithoutEviction()
        {
            var player = new Entity();
            for (int i = 0; i < 32; i++)
                ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record($"Overworld.{i % 16}.{i / 16}.0"));
            Assert.AreEqual(32, ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World).Length);
            var before = ExpeditionReportDraftContract.Snapshot(player);
            Assert.False(ExpeditionReportDraftContract.Remember(player,
                ExpeditionReportDraftContract.Record("Overworld.17.2.0"), ExpeditionReportDraftContract.World, out var reason));
            Assert.AreEqual("discovery_note_capacity", reason); ExpeditionReportDraftContract.Unchanged(player, before);
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record("Overworld.0.0.0", informant: "later keeper"));
            Assert.AreEqual(32, ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World).Length);
            Assert.That(ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World).Single(n => n.Contains("Destination (0,0)")), Does.Contain("later keeper"));
            foreach (var row in before.Where(row => !row.Value.Contains("Overworld.0.0.0"))) Assert.AreEqual(row.Value, player.Properties[row.Key]);
        }

        [Test]
        public void CapacityCountsStoredV2SlotsWithoutDeletingMalformedOrForeignHistory()
        {
            var player = new Entity();
            for (int i = 0; i < 32; i++) player.Properties[ExpeditionReportDraftContract.Prefix + "invalid-" + i] = "{broken";
            var before = ExpeditionReportDraftContract.Snapshot(player);
            Assert.False(ExpeditionReportDraftContract.Remember(player, ExpeditionReportDraftContract.Record(),
                ExpeditionReportDraftContract.World, out var reason));
            Assert.AreEqual("discovery_note_capacity", reason);
            Assert.IsEmpty(ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World));
            ExpeditionReportDraftContract.Unchanged(player, before);
        }

        [TestCase("payload-world")][TestCase("property-key")][TestCase("oversized-wire")][TestCase("broken-json")]
        public void CorruptSavedV2RowIsIgnoredWithoutHidingAValidOtherPlaceOrRewritingEither(string corruption)
        {
            var player = new Entity();
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record());
            ExpeditionReportDraftContract.Put(player, ExpeditionReportDraftContract.Record("Overworld.12.9.0"));
            string key = player.Properties.Single(p => p.Value.Contains("Overworld.11.9.0")).Key;
            if (corruption == "payload-world") player.Properties[key] = player.Properties[key].Replace(ExpeditionReportDraftContract.World, ExpeditionReportDraftContract.ForeignWorld);
            if (corruption == "property-key") { string value = player.Properties[key]; player.Properties.Remove(key); player.Properties[key + ":foreign"] = value; }
            if (corruption == "oversized-wire") player.Properties[key] = new string('x', 4097);
            if (corruption == "broken-json") player.Properties[key] = "{not-json";
            var before = ExpeditionReportDraftContract.Snapshot(player);
            var notes = ExpeditionReportDraftContract.Read(player, ExpeditionReportDraftContract.World);
            Assert.AreEqual(1, notes.Length); Assert.That(notes[0], Does.Contain("Destination (12,9)"));
            ExpeditionReportDraftContract.Unchanged(player, before);
        }
    }

    internal sealed class ExpeditionReportFixture : IDisposable
    {
        const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        readonly HaulingContentScope content = new HaulingContentScope();
        readonly object offers = typeof(SpreadDiscoveryReports).GetField("offers", Hidden).GetValue(null);
        readonly object revision = typeof(SpreadDiscoveryReports).GetField("revision", Hidden).GetValue(null);
        readonly StoryletPart oldStory = StoryletPart.Current;
        readonly Entity oldPlayer = StoryletPart.LocalPlayer;
        readonly Zone oldActive = SettlementRuntime.ActiveZone;
        public EntityFactory Factory => content.Factory;
        public readonly OverworldZoneManager Manager;
        public readonly Zone Zone;
        public readonly Entity Player, Speaker;
        public ExpeditionReportFixture(string blueprint = "SpreadSeedKeeper", bool modern = true)
        {
            content.Seed(64); Manager = OverworldZoneManager.CreateDetached(Factory, 64, modern);
            Zone = new Zone(blueprint == "SpreadWaysideCook" ? "Overworld.12.11.0" : "Overworld.11.8.0");
            Manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
            Player = Factory.CreateEntity("Player"); Speaker = Factory.CreateEntity(blueprint);
            Assert.True(Zone.AddEntity(Player, 10, 10)); Assert.True(Zone.AddEntity(Speaker, 11, 10));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = Player;
            ConversationManager.Speaker = Speaker; ConversationManager.Listener = Player;
            ConversationManager.CurrentConversation = new ConversationData { ID = Speaker.GetPart<ConversationPart>().ConversationID };
            ConversationManager.CurrentNode = new NodeData { ID = "Nearby", Text = "What lies beyond the fields?" };
            Refresh();
        }
        public string Key => Manager.Exploration.WorldKey;
        public void Refresh() => ConversationManager.RefreshVisibleChoices();
        public ChoiceData[] Reports => ConversationManager.VisibleChoices.Where(c => c.Actions?.Any(a => a.Key == SpreadDiscoveryReports.ActionName) == true).ToArray();
        public ChoiceData Works => Reports.SingleOrDefault(c => c.Actions.Any(a => a.Key == SpreadDiscoveryReports.ActionName
            && a.Value.StartsWith(ExpeditionReportDraftContract.WorksFamily + "|", StringComparison.Ordinal)));
        public bool Remember(ChoiceData choice)
        {
            Assert.NotNull(choice, "The real resident menu must expose the bounded peat-works lead.");
            return ConversationActions.TryExecuteAll(choice.Actions, Speaker, Player);
        }
        public string[] Read() => ExpeditionReportDraftContract.Read(Player, Key);
        public void Dispose()
        {
            ConversationManager.EndConversation(); StoryletPart.Current = oldStory; StoryletPart.LocalPlayer = oldPlayer;
            SettlementRuntime.ActiveZone = oldActive;
            typeof(SpreadDiscoveryReports).GetField("offers", Hidden).SetValue(null, offers);
            typeof(SpreadDiscoveryReports).GetField("revision", Hidden).SetValue(null, revision);
            content.Dispose();
        }
    }

    public sealed class ExpeditionReportDialogueTests
    {
        [TestCase("SpreadSeedKeeper")][TestCase("SpreadWaysideCook")]
        public void ActualResidentOffersOneUsefulHistoricalLeadWithoutGeneratingItsDestination(string blueprint)
        {
            using var f = new ExpeditionReportFixture(blueprint); int cached = f.Manager.CachedZoneCount;
            Assert.True(SoddenDistrict.Eligible(f.Manager, SoddenDistrictPlan.WorksZoneID));
            Assert.NotNull(f.Works); Assert.AreEqual(3, f.Reports.Length, "Two bounded nearby reports and one explicit regional lead.");
            string text = ConversationManager.CurrentText.ToLowerInvariant();
            Assert.That(text, Does.Contain("peat").And.Contain("mallet").And.Contain("armor").And.Contain("sumphold"));
            Assert.True(f.Remember(f.Works)); string note = f.Read().Single();
            Assert.That(note, Does.Contain("17,7").And.Contain("unconfirmed when heard").And.Contain("does not say what remains"));
            Assert.AreEqual(cached, f.Manager.CachedZoneCount); Assert.False(f.Manager.CachedZones.ContainsKey(SoddenDistrictPlan.WorksZoneID));
            Assert.IsEmpty(StoryletPart.Current.GetActiveQuests());
        }

        [TestCase("works-biome")][TestCase("works-poi")][TestCase("missing-component")]
        [TestCase("manifest")][TestCase("origin-biome")][TestCase("hostile")][TestCase("wrong-node")]
        public void ChangedRealAdmissionRefusesAnAlreadyOfferedWorksReport(string mutation)
        {
            using var f = new ExpeditionReportFixture(); var offer = f.Works; Assert.NotNull(offer);
            switch (mutation)
            {
                case "works-biome": f.Manager.WorldMap.Tiles[17, 7] = BiomeType.Beating; break;
                case "works-poi": f.Manager.WorldMap.SetPOI(17, 7, new PointOfInterest(POIType.Village, "another place")); break;
                case "missing-component": f.Factory.Blueprints.Remove("PeatMalletHeadComponent"); break;
                case "manifest": SoddenDistrictIntegrationTests.Version(f.Manager, 13); break;
                case "origin-biome": f.Manager.WorldMap.Tiles[11, 8] = BiomeType.Beating; break;
                case "hostile": f.Speaker.GetPart<BrainPart>().SetPersonallyHostile(f.Player); break;
                case "wrong-node": ConversationManager.CurrentNode = new NodeData { ID = "Start" }; break;
            }
            var before = ExpeditionReportDraftContract.Snapshot(f.Player);
            Assert.False(f.Remember(offer)); ExpeditionReportDraftContract.Unchanged(f.Player, before);
            f.Refresh(); Assert.Null(f.Works);
        }

        [TestCase("refresh")][TestCase("speaker-moves")][TestCase("foreign-world-plan")][TestCase("cached-origin-replaced")]
        public void RevisionAndExactSpeakerWorldGraphRemainAuthorityForRemembering(string mutation)
        {
            using var f = new ExpeditionReportFixture(); var offer = f.Works; Assert.NotNull(offer);
            if (mutation == "refresh") f.Refresh();
            if (mutation == "speaker-moves") Assert.True(f.Zone.MoveEntity(f.Speaker, 13, 10));
            if (mutation == "foreign-world-plan")
            {
                var other = OverworldZoneManager.CreateDetached(f.Factory, 64, true);
                Assert.AreNotEqual(f.Key, other.Exploration.WorldKey, "Same seed does not identify the same world.");
                typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(f.Manager, other.Exploration);
            }
            if (mutation == "cached-origin-replaced") f.Manager.SetActiveZone(new Zone(f.Zone.ZoneID));
            Assert.False(f.Remember(offer)); Assert.IsEmpty(f.Read());
            if (mutation == "refresh") Assert.True(f.Remember(f.Works), "Fresh choice is the paired positive control.");
        }

        [Test]
        public void FullJournalGivesAClearCapacityRefusalAndLeavesPriorNotesIntact()
        {
            using var f = new ExpeditionReportFixture();
            for (int i = 0; i < 32; i++) ExpeditionReportDraftContract.Put(f.Player,
                ExpeditionReportDraftContract.Record($"Overworld.{i % 16}.{i / 16}.0", world: f.Key), f.Key);
            f.Refresh(); var before = ExpeditionReportDraftContract.Snapshot(f.Player); MessageLog.Clear();
            Assert.False(f.Remember(f.Works)); ExpeditionReportDraftContract.Unchanged(f.Player, before);
            Assert.That(string.Join(" ", MessageLog.GetAllEntries().Select(e => e.Text)).ToLowerInvariant(),
                Does.Contain("full"), "Capacity refusal must not masquerade as a stale conversation.");
        }

        [Test]
        public void JournalUsesCurrentWorldIdentityAndReadingNeverChangesSavedOrRemoteState()
        {
            using var f = new ExpeditionReportFixture(); Assert.True(f.Remember(f.Works));
            var before = ExpeditionReportDraftContract.Snapshot(f.Player); int cached = f.Manager.CachedZoneCount;
            var go = new GameObject("expedition-report-journal");
            try
            {
                var ui = go.AddComponent<QuestLogUI>(); ui.Open(); ui.HandleInput(new TabKey());
                Assert.True(ui.NotesVisible);
                var lines = (List<string>)typeof(QuestLogUI).GetField("_noteLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
                Assert.That(string.Join(" ", lines).ToLowerInvariant(), Does.Contain("peat").And.Contain("mallet"));
                ui.Rebuild(); ExpeditionReportDraftContract.Unchanged(f.Player, before); Assert.AreEqual(cached, f.Manager.CachedZoneCount);
                var foreign = OverworldZoneManager.CreateDetached(f.Factory, 64, true);
                var foreignZone = new Zone(f.Zone.ZoneID); foreign.SetActiveZone(foreignZone);
                Assert.True(f.Zone.RemoveEntity(f.Player)); Assert.True(foreignZone.AddEntity(f.Player, 10, 10));
                ui.Rebuild(); Assert.That(string.Join(" ", lines).ToLowerInvariant(), Does.Not.Contain("peat"));
                ExpeditionReportDraftContract.Unchanged(f.Player, before);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        sealed class TabKey : IInputProbe { public bool GetKeyDown(KeyCode key) => key == KeyCode.Tab; }

        [Test]
        public void HistoricalLeadStaysHonestAfterRealSourceTransferPackAssemblyAndEmptyRevisit()
        {
            using var f = new ExpeditionReportFixture(); Assert.True(f.Remember(f.Works));
            string heard = f.Read().Single(); var beforeChoices = f.Reports.Select(c => c.Text).ToArray();
            var works = f.Manager.GetZone(SoddenDistrictPlan.WorksZoneID); Assert.NotNull(works);
            var locker = works.GetReadOnlyEntities().Single(e => e.BlueprintName == "SoddenWorksLocker");
            var pack = locker.GetPart<ContainerPart>(); var original = pack.Contents.ToArray();
            Assert.True(f.Zone.RemoveEntity(f.Player)); var at = works.GetEntityPosition(locker);
            Assert.True(works.AddEntity(f.Player, at.x + 1, at.y)); f.Player.GetPart<InventoryPart>().MaxWeight = 10000;
            foreach (var item in original)
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(locker, item), f.Player, works).Success, item.BlueprintName);
            Assert.IsEmpty(pack.Contents);
            var inventory = f.Player.GetPart<InventoryPart>();
            Entity Item(string id) => inventory.Objects.Single(e => e.BlueprintName == id);
            Assert.True(WeaponForgingService.TryForge(f.Player, f.Factory, Item("PeatMalletHeadComponent"), Item("OakHaftComponent"),
                Item("LeatherBindingComponent"), out var weapon, out var why), why);
            Assert.True(InventorySystem.Equip(f.Player, weapon)); var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Bludgeoning Cudgel", melee.Attributes); Assert.AreEqual("1d4", melee.BaseDamage); Assert.AreEqual(-1, melee.PenBonus);
            Assert.AreEqual(heard, f.Read().Single(), "History does not become an unsupported cleared/current-stock claim.");
            Assert.True(works.RemoveEntity(f.Player)); Assert.True(f.Zone.AddEntity(f.Player, 10, 10));
            f.Refresh(); CollectionAssert.AreEqual(beforeChoices, f.Reports.Select(c => c.Text));
            Assert.True(f.Remember(f.Works)); Assert.AreEqual(heard, f.Read().Single());
            f.Manager.UnloadZone(works.ZoneID); Assert.AreSame(works, f.Manager.GetZone(works.ZoneID)); Assert.IsEmpty(pack.Contents);
        }
    }
}
