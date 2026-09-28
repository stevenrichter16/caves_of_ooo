using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal sealed class SpreadDiscoveryFixture : IDisposable
    {
        internal const string Action = "RememberSpreadDiscovery";
        internal const string Prefix = "SpreadDiscoveryNote.v1:";
        readonly HotbarSaveFixture scope = new HotbarSaveFixture(false, false);
        readonly List<(FieldInfo field, object original, DictionaryEntry[] entries)> statics = new();
        public readonly OverworldZoneManager Manager;
        public readonly Zone Zone;
        public readonly Entity Player;
        public readonly Entity Speaker;

        public SpreadDiscoveryFixture(string conversation = "Scribe_1", string node = "RegionOverview", bool includeWayhouse = false)
        {
            Snapshot(typeof(ConversationActions));
            var reportType = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadDiscoveryReports");
            if (reportType != null) Snapshot(reportType);
            Manager = OverworldZoneManager.CreateDetached(null, 64);
            if (!includeWayhouse) Wayhouse(SpreadWayhousePlan.Restore(null));
            Zone = new Zone("Overworld.10.10.0");
            Manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Player = Actor("reader", true); Speaker = Actor("Mera", false);
            Speaker.AddPart(new ConversationPart { ConversationID = conversation });
            Assert.True(Zone.AddEntity(Player, 10, 10)); Assert.True(Zone.AddEntity(Speaker, 11, 10));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = Player;
            ConversationManager.Speaker = Speaker; ConversationManager.Listener = Player;
            ConversationManager.CurrentConversation = new ConversationData { ID = conversation };
            Node(node);
            Assert.IsNotEmpty(Manager.RareEncounters.PairZoneID);
            Assert.IsNotEmpty(Manager.RareEncounters.ViperZoneID);
        }

        static Entity Actor(string name, bool player)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = player ? "Player" : "Scribe" };
            e.AddPart(new PhysicsPart()); e.AddPart(new RenderPart { DisplayName = name });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 40, Max = 40, Owner = e };
            if (player) e.SetTag("Player");
            return e;
        }

        void Snapshot(Type type)
        {
            foreach (var f in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.IsLiteral) continue;
                var value = f.GetValue(null);
                DictionaryEntry[] entries = null;
                if (value is IDictionary dictionary)
                {
                    var copy = new List<DictionaryEntry>();
                    foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                    entries = copy.ToArray();
                }
                if (!f.IsInitOnly || entries != null) statics.Add((f, value, entries));
            }
        }

        public void Node(string id)
        {
            ConversationManager.CurrentNode = new NodeData { ID = id };
            ConversationManager.RefreshVisibleChoices();
        }
        public ChoiceData[] Reports => ConversationManager.VisibleChoices.Where(c => c.Actions?.Any(a => a.Key == Action) == true).ToArray();
        public ChoiceData Offer(string family)
        {
            var result = Reports.SingleOrDefault(c => c.Actions.Any(a => a.Key == Action && a.Value.StartsWith(family + "|", StringComparison.Ordinal)));
            Assert.NotNull(result, "The current real dialogue must offer " + family + ".");
            return result;
        }
        public bool Execute(ChoiceData choice) => ConversationActions.TryExecuteAll(choice.Actions, ConversationManager.Speaker, ConversationManager.Listener);
        public void Select(ChoiceData choice)
        {
            int i = ConversationManager.VisibleChoices.ToList().IndexOf(choice); Assert.GreaterOrEqual(i, 0);
            ConversationManager.SelectChoice(i);
        }
        public static string[] Read(Entity player)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadDiscoveryNotes");
            Assert.NotNull(type, "Missing historical discovery-note provider.");
            var method = type.GetMethod("Read", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Entity) }, null);
            Assert.NotNull(method); return ((IEnumerable)method.Invoke(null, new object[] { player })).Cast<string>().ToArray();
        }
        public void Plan(SpreadRareEncounterPlan plan)
            => typeof(OverworldZoneManager).GetProperty("RareEncounters").GetSetMethod(true).Invoke(Manager, new object[] { plan });
        public void Wayhouse(SpreadWayhousePlan plan)
            => typeof(OverworldZoneManager).GetProperty("Wayhouse").GetSetMethod(true).Invoke(Manager, new object[] { plan });
        public int NoteCount => Player.Properties.Keys.Count(k => k.StartsWith(Prefix, StringComparison.Ordinal));
        public void Dispose()
        {
            ConversationManager.EndConversation();
            for (int i = statics.Count - 1; i >= 0; i--)
            {
                var s = statics[i];
                if (s.entries != null)
                {
                    var d = (IDictionary)s.original; d.Clear();
                    foreach (var e in s.entries) d.Add(e.Key, e.Value);
                }
                if (!s.field.IsInitOnly) s.field.SetValue(null, s.original);
            }
            scope.Dispose();
        }
    }

    public class SpreadDiscoveryReportsTests
    {
        [TestCase("Scribe_1", "RegionOverview")]
        [TestCase("Innkeeper_1", "Rumors")]
        public void CurrentDialogueOffersBothFamiliesWithoutAcceptingOrGenerating(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node);
            int cached = f.Manager.CachedZoneCount;
            string pair = f.Manager.RareEncounters.PairZoneID, viper = f.Manager.RareEncounters.ViperZoneID;
            Assert.AreEqual(2, f.Reports.Length);
            foreach (var family in new[] { "ditch-cutters", "chalk-ring-viper" })
            {
                var offer = f.Offer(family); StringAssert.Contains("unconfirmed", offer.Text.ToLowerInvariant());
                Assert.IsFalse(offer.Actions.Any(a => a.Key == "StartQuest"));
            }
            ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(2, f.Reports.Length); Assert.AreEqual(cached, f.Manager.CachedZoneCount);
            Assert.AreEqual(pair, f.Manager.RareEncounters.PairZoneID); Assert.AreEqual(viper, f.Manager.RareEncounters.ViperZoneID);
            Assert.AreEqual(0, f.NoteCount); Assert.IsEmpty(StoryletPart.Current.GetActiveQuests());
            Assert.AreEqual(4, ConversationManager.VisibleChoices.Count(c => c.Actions?.Any(a => a.Key == RegionalGuidance.ActionName) == true));
        }

        [TestCase("Scribe_1", "Start")][TestCase("Innkeeper_1", "RegionOverview")]
        [TestCase("Warden_1", "Threats")][TestCase("Morrowfast_Vennit", "RegionOverview")]
        public void OtherNodesAndRolesDoNotAcquireRareRumorActions(string conversation, string node)
        { using var f = new SpreadDiscoveryFixture(conversation, node); Assert.IsEmpty(f.Reports); Assert.AreEqual(0, f.NoteCount); }

        [TestCase("old")][TestCase("empty")][TestCase("malformed")]
        public void AbsentSelectionCannotBeBackfilledByTalking(string kind)
        {
            using var f = new SpreadDiscoveryFixture(); var world = new Entity();
            if (kind != "old") world.Properties[SpreadRareEncounterPlan.PropertyKey] = kind == "empty" ? "1|" : "1|Overworld.010.2.0";
            f.Plan(SpreadRareEncounterPlan.Restore(world)); ConversationManager.RefreshVisibleChoices();
            Assert.IsEmpty(f.Reports); Assert.AreEqual(0, f.NoteCount); Assert.AreEqual(1, f.Manager.CachedZoneCount);
        }

        [Test]
        public void ExplicitChoiceRecordsEachHistoricalFamilyOnceAndPreservesOtherNotes()
        {
            using var f = new SpreadDiscoveryFixture();
            var village = RegionalGuidance.Build(f.Zone, f.Speaker, f.Player)[0];
            Assert.IsNull(RegionalGuidance.TryRemember(f.Zone, f.Speaker, f.Player, village.DestinationZoneID));
            f.Player.Properties["RegionalSituationNote:existing"] = "An earlier agreement.";
            var before = f.Player.Properties.ToDictionary(p => p.Key, p => p.Value);
            f.Select(f.Offer("ditch-cutters")); Assert.AreEqual(1, f.NoteCount);
            f.Select(f.Offer("ditch-cutters")); Assert.AreEqual(1, f.NoteCount);
            f.Select(f.Offer("chalk-ring-viper")); Assert.AreEqual(2, f.NoteCount);
            foreach (var p in before) Assert.AreEqual(p.Value, f.Player.Properties[p.Key]);
            foreach (var note in SpreadDiscoveryFixture.Read(f.Player))
            { StringAssert.Contains("Mera", note); StringAssert.Contains("10,10", note); StringAssert.Contains("unconfirmed", note.ToLowerInvariant()); }
        }

        [Test]
        public void RemoteEmptyLiveOrDeadGraphCannotChangeReportsOrBeMutated()
        {
            using var f = new SpreadDiscoveryFixture(); var before = f.Reports.Select(c => c.Text).ToArray();
            Assert.AreEqual(2, before.Length);
            var remote = new Zone(f.Manager.RareEncounters.PairZoneID); f.Manager.CachedZones[remote.ZoneID] = remote;
            ConversationManager.RefreshVisibleChoices(); CollectionAssert.AreEqual(before, f.Reports.Select(c => c.Text));
            var actor = new Entity { ID = "unseen-pair", BlueprintName = SpreadRareEncounterPlan.PairLeader };
            actor.AddPart(new PhysicsPart()); actor.Properties[SpreadRareEncounterBuilder.SourceKey] = remote.ZoneID;
            actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 15, Max = 15, Owner = actor };
            Assert.True(remote.AddEntity(actor, 7, 7));
            ConversationManager.RefreshVisibleChoices(); CollectionAssert.AreEqual(before, f.Reports.Select(c => c.Text));
            actor.GetStat("Hitpoints").BaseValue = 0; actor.SetTag("_DeathHandled");
            ConversationManager.RefreshVisibleChoices(); CollectionAssert.AreEqual(before, f.Reports.Select(c => c.Text));
            Assert.AreSame(actor, remote.GetCell(7, 7).Occupants.Single()); Assert.AreEqual(0, f.NoteCount);
        }

        [Test]
        public void EntitySaveRestoresExactHistoricalNotesWithoutConsultingCurrentMapOrSpeaker()
        {
            using var f = new SpreadDiscoveryFixture();
            f.Select(f.Offer("ditch-cutters")); f.Select(f.Offer("chalk-ring-viper"));
            var expected = SpreadDiscoveryFixture.Read(f.Player);
            using var stream = new MemoryStream(); SaveGraphSerializer.SaveEntityBody(f.Player, new SaveWriter(stream)); stream.Position = 0;
            var loaded = new Entity(); SaveGraphSerializer.LoadEntityBody(loaded, new SaveReader(stream, null));
            f.Zone.RemoveEntity(f.Speaker); f.Plan(SpreadRareEncounterPlan.Restore(null));
            CollectionAssert.AreEqual(expected, SpreadDiscoveryFixture.Read(loaded)); Assert.AreEqual(2, expected.Length);
            var elsewhere = new Zone("Overworld.11.10.0"); f.Zone.RemoveEntity(f.Player); elsewhere.AddEntity(f.Player, 10, 10); f.Manager.SetActiveZone(elsewhere);
            CollectionAssert.AreEqual(expected, SpreadDiscoveryFixture.Read(f.Player));
        }

        [Test]
        public void ExistingJournalShowsCompleteReportAndKeepsTabNavigation()
        {
            using var f = new SpreadDiscoveryFixture(); f.Select(f.Offer("ditch-cutters"));
            var go = new GameObject("discovery-note-journal");
            try
            {
                var ui = go.AddComponent<QuestLogUI>(); ui.Open(); ui.HandleInput(new TabKey());
                Assert.True(ui.IsOpen); Assert.True(ui.NotesVisible);
                var lines = (List<string>)typeof(QuestLogUI).GetField("_noteLines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                string joined = string.Join(" ", lines); StringAssert.Contains("Mera", joined); StringAssert.Contains("unconfirmed", joined.ToLowerInvariant());
                StringAssert.Contains("ditch", joined.ToLowerInvariant());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        sealed class TabKey : IInputProbe { public bool GetKeyDown(KeyCode key) => key == KeyCode.Tab; }
    }
}
