using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadResidentReportsTests
    {
        sealed class Fixture : IDisposable
        {
            static readonly FieldInfo Offers = typeof(SpreadDiscoveryReports).GetField("offers", BindingFlags.Static | BindingFlags.NonPublic);
            static readonly FieldInfo Revision = typeof(SpreadDiscoveryReports).GetField("revision", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldOffers = Offers.GetValue(null), oldRevision = Revision.GetValue(null);
            readonly HaulingContentScope scope = new HaulingContentScope();
            readonly StoryletPart oldStory = StoryletPart.Current;
            readonly Entity oldPlayer = StoryletPart.LocalPlayer;
            public readonly OverworldZoneManager Manager;
            public readonly Zone Zone;
            public readonly Entity Player, Speaker;
            public Fixture(string blueprint = "SpreadSeedKeeper")
            {
                PlayerReputation.Reset();
                Manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                Zone = new Zone(blueprint == "SpreadSeedKeeper" ? "Overworld.11.8.0" : "Overworld.12.11.0");
                Manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
                Player = scope.Factory.CreateEntity("Player"); Speaker = scope.Factory.CreateEntity(blueprint);
                Assert.NotNull(Speaker); Assert.True(Zone.AddEntity(Player, 10, 10)); Assert.True(Zone.AddEntity(Speaker, 11, 10));
                StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = Player;
                ConversationManager.Speaker = Speaker; ConversationManager.Listener = Player;
                ConversationManager.CurrentConversation = new ConversationData { ID = Speaker.GetPart<ConversationPart>().ConversationID };
                ConversationManager.CurrentNode = new NodeData { ID = "Nearby", Text = "What else lies nearby?" };
                Refresh();
            }
            public void Refresh() => ConversationManager.RefreshVisibleChoices();
            public ChoiceData[] Reports => ConversationManager.VisibleChoices.Where(c => c.Actions?.Any(a => a.Key == SpreadDiscoveryReports.ActionName) == true).ToArray();
            public bool Remember(ChoiceData choice) => ConversationActions.TryExecuteAll(choice.Actions, Speaker, Player);
            public void Dispose()
            {
                ConversationManager.EndConversation(); StoryletPart.Current = oldStory; StoryletPart.LocalPlayer = oldPlayer; scope.Dispose();
                Offers.SetValue(null, oldOffers); Revision.SetValue(null, oldRevision);
            }
        }

        [TestCase("SpreadSeedKeeper")][TestCase("SpreadWaysideCook")]
        public void ResidentsOfferTwoDifferentNearbyPlacesWithoutCreatingGraphsOrNotes(string blueprint)
        {
            using (var f = new Fixture(blueprint))
            {
                int count = f.Manager.CachedZoneCount;
                Assert.AreEqual(2, f.Reports.Length);
                Assert.AreEqual(2, f.Reports.Select(c => c.Actions.Single(a => a.Key == SpreadDiscoveryReports.ActionName).Value.Split('|')[0]).Distinct().Count());
                foreach (var row in f.Reports) { Assert.That(row.Text, Does.Contain("unconfirmed")); Assert.LessOrEqual(row.Text.Length, 52); }
                Assert.IsEmpty(SpreadDiscoveryNotes.Read(f.Player));
                Assert.IsEmpty(StoryletPart.Current.GetActiveQuests());
                Assert.That(ConversationManager.CurrentText, Does.Contain("unconfirmed when heard"));
                Assert.True(f.Remember(f.Reports[0]));
                Assert.AreEqual(1, SpreadDiscoveryNotes.Read(f.Player).Count);
                Assert.AreEqual(count, f.Manager.CachedZoneCount);
                f.Refresh(); Assert.AreEqual(2, f.Reports.Length);
                Assert.True(f.Remember(f.Reports[0])); Assert.AreEqual(1, SpreadDiscoveryNotes.Read(f.Player).Count);
            }
        }

        [TestCase("node")][TestCase("blueprint")][TestCase("dead")][TestCase("plan")][TestCase("moved")][TestCase("hostile")]
        public void StaleOrIneligibleResidentCannotWriteThePreviouslyOfferedReport(string change)
        {
            using (var f = new Fixture())
            {
                Assert.AreEqual(2, f.Reports.Length); var offer = f.Reports[0];
                if (change == "node") ConversationManager.CurrentNode = new NodeData { ID = "Start" };
                if (change == "blueprint") f.Speaker.BlueprintName = "Villager";
                if (change == "dead") f.Speaker.GetStat("Hitpoints").BaseValue = 0;
                if (change == "moved") Assert.True(f.Zone.MoveEntity(f.Speaker, 14, 10));
                if (change == "hostile") f.Speaker.GetPart<BrainPart>().SetPersonallyHostile(f.Player);
                if (change == "plan")
                    typeof(OverworldZoneManager).GetProperty("Exploration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .SetValue(f.Manager, OverworldZoneManager.CreateDetached(null, 64, true).Exploration);
                Assert.False(f.Remember(offer)); Assert.IsEmpty(SpreadDiscoveryNotes.Read(f.Player));
            }
        }

        [Test] public void RememberedPlaceRemainsHistoricalAfterRemoteStateChangesAndSaveReadback()
        {
            using (var f = new Fixture())
            {
                Assert.AreEqual(2, f.Reports.Length);
                string before = ConversationManager.CurrentText;
                var offer = f.Reports[0]; Assert.True(f.Remember(offer));
                string original = SpreadDiscoveryNotes.Read(f.Player).Single();
                var key = f.Player.Properties.Keys.Single(k => k.StartsWith(SpreadDiscoveryNotes.Prefix, StringComparison.Ordinal));
                string wire = f.Player.Properties[key];
                using var stream = new MemoryStream();
                SaveGraphSerializer.SaveEntityBody(f.Player, new SaveWriter(stream)); stream.Position = 0;
                var copy = new Entity(); SaveGraphSerializer.LoadEntityBody(copy, new SaveReader(stream, null));
                CollectionAssert.AreEqual(SpreadDiscoveryNotes.Read(f.Player), SpreadDiscoveryNotes.Read(copy));
                var remote = new Zone("Overworld.11.9.0"); f.Manager.CachedZones[remote.ZoneID] = remote;
                var ruin = new Entity { BlueprintName = "DestroyedStill" }; Assert.True(remote.AddEntity(ruin, 5, 5));
                f.Refresh(); Assert.AreEqual(before, ConversationManager.CurrentText);
                Assert.AreEqual(original, SpreadDiscoveryNotes.Read(f.Player).Single());
                Assert.AreEqual(wire, f.Player.Properties[key]);
                copy.Properties[key] = "{\"Version\":1,\"Family\":\"field-alembic\"}";
                Assert.IsEmpty(SpreadDiscoveryNotes.Read(copy));
            }
        }

        [TestCase("field-alembic", "field alembic")]
        [TestCase("tempering-shelter", "tempering shelter")]
        [TestCase("trappers-store", "trapper's store")]
        [TestCase("seed-keepers-plot", "seed keeper's plot")]
        [TestCase("wayside-kitchen", "wayside kitchen")]
        public void EachExplicitFieldNoteSurvivesSerializationButRejectsUnknownFormation(string family, string description)
        {
            var player = new Entity();
            string key = SpreadDiscoveryNotes.Prefix + family;
            string wire = "{\"Version\":1,\"Family\":\"" + family + "\",\"OriginZoneID\":\"Overworld.11.8.0\","
                + "\"DestinationZoneID\":\"Overworld.11.9.0\",\"InformantID\":\"keeper\",\"InformantName\":\"seed keeper\",\"Formation\":\"Hedgerow\"}";
            player.Properties[key] = wire;
            Assert.That(SpreadDiscoveryNotes.Read(player).Single(), Does.Contain(description));
            using var stream = new MemoryStream(); SaveGraphSerializer.SaveEntityBody(player, new SaveWriter(stream)); stream.Position = 0;
            var copy = new Entity(); SaveGraphSerializer.LoadEntityBody(copy, new SaveReader(stream, null));
            Assert.AreEqual(wire, copy.Properties[key]);
            CollectionAssert.AreEqual(SpreadDiscoveryNotes.Read(player), SpreadDiscoveryNotes.Read(copy));
            copy.Properties[key] = wire.Replace("Hedgerow", "InventedFormation");
            Assert.IsEmpty(SpreadDiscoveryNotes.Read(copy));
            Assert.AreEqual(wire.Replace("Hedgerow", "InventedFormation"), copy.Properties[key], "Readers do not repair saved bytes.");
        }
    }
}
