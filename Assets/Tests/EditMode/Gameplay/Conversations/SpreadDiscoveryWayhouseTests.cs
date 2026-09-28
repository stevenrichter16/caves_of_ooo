using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadDiscoveryWayhouseTests
    {
        const string Family = "turnbank-wayhouse";

        [TestCase("Scribe_1", "RegionOverview")]
        [TestCase("Innkeeper_1", "Rumors")]
        public void RealFrozenSelectionAddsThirdHistoricalReportWithoutVisitOrReward(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node, includeWayhouse: true);
            Assert.IsNotEmpty(f.Manager.Wayhouse.ZoneID);
            Assert.True(f.Manager.Wayhouse.Selects(f.Manager, f.Manager.Wayhouse.ZoneID));
            string selection = f.Manager.Wayhouse.ZoneID;
            Assert.AreEqual(3, f.Reports.Length);
            var offer = f.Offer(Family);
            StringAssert.Contains("unconfirmed", offer.Text.ToLowerInvariant());
            Assert.LessOrEqual(offer.Text.Length, 52);
            string text = ConversationManager.CurrentText;
            StringAssert.Contains("Turnbank", text);
            var at = WorldMap.FromZoneID(selection);
            StringAssert.Contains(at.x + "," + at.y, text);
            Assert.AreEqual(1, f.Manager.CachedZoneCount);
            Assert.AreEqual(0, f.NoteCount);
            f.Select(offer);
            Assert.AreEqual(1, f.NoteCount);
            Assert.AreEqual(selection, f.Manager.Wayhouse.ZoneID);
            Assert.AreEqual(1, f.Manager.CachedZoneCount);
            StringAssert.Contains("unconfirmed", SpreadDiscoveryFixture.Read(f.Player).Single().ToLowerInvariant());
        }

        [TestCase("missing")][TestCase("empty")][TestCase("malformed")]
        public void OlderOrDisabledWayhouseMetadataDoesNotInventThirdReport(string kind)
        {
            using var f = new SpreadDiscoveryFixture(includeWayhouse: true);
            var world = new Entity();
            if (kind != "missing") world.Properties[SpreadWayhousePlan.PropertyKey] = kind == "empty" ? "1|" : "1|Overworld.010.2.0";
            f.Wayhouse(SpreadWayhousePlan.Restore(world));
            ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(2, f.Reports.Length);
            Assert.False(f.Reports.Any(c => c.Actions.Any(a => a.Value.StartsWith(Family + "|", StringComparison.Ordinal))));
            StringAssert.DoesNotContain("Turnbank", ConversationManager.CurrentText);
            Assert.AreEqual(1, f.Manager.CachedZoneCount); Assert.AreEqual(0, f.NoteCount);
        }

        [Test]
        public void WayhouseReportDoesNotDependOnTheOlderRarePlanBeingInitialized()
        {
            using var f = new SpreadDiscoveryFixture(includeWayhouse: true);
            string id = f.Manager.Wayhouse.ZoneID;
            f.Plan(SpreadRareEncounterPlan.Restore(null));
            Assert.True(f.Manager.Wayhouse.Selects(f.Manager, id));
            ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(1, f.Reports.Length); Assert.True(f.Execute(f.Offer(Family)));
            Assert.AreEqual(1, f.NoteCount); Assert.AreEqual(1, f.Manager.CachedZoneCount);
        }

        [TestCase("plan-replaced")][TestCase("map-changed")]
        public void WayhouseOfferCannotOutliveItsExactSelectionAuthority(string change)
        {
            using var f = new SpreadDiscoveryFixture(includeWayhouse: true);
            var offer = f.Offer(Family);
            if (change == "plan-replaced") f.Wayhouse(SpreadWayhousePlan.Create(f.Manager));
            else
            {
                var at = WorldMap.FromZoneID(f.Manager.Wayhouse.ZoneID);
                f.Manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Beating;
            }
            Assert.False(f.Execute(offer)); Assert.AreEqual(0, f.NoteCount);
        }

        [Test]
        public void UninstalledAndChangedRemoteGraphsProduceTheSameHistoricalText()
        {
            using var f = new SpreadDiscoveryFixture(includeWayhouse: true);
            var initialOffer = f.Offer(Family);
            string text = ConversationManager.CurrentText;
            var remote = new Zone(f.Manager.Wayhouse.ZoneID);
            f.Manager.CachedZones[remote.ZoneID] = remote;
            ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(initialOffer.Text, f.Offer(Family).Text);
            Assert.AreEqual(text, ConversationManager.CurrentText);
            var guard = new Entity { ID = "unseen-wayhouse-owner", BlueprintName = "MarlbackScrabbler" };
            guard.AddPart(new PhysicsPart());
            guard.Properties["SpreadWayhouse.Role"] = "guard";
            guard.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = guard, BaseValue = 15, Max = 15 };
            Assert.True(remote.AddEntity(guard, 7, 7));
            ConversationManager.RefreshVisibleChoices(); Assert.AreEqual(text, ConversationManager.CurrentText);
            guard.GetStat("Hitpoints").BaseValue = 0; guard.SetTag("_DeathHandled");
            ConversationManager.RefreshVisibleChoices(); Assert.AreEqual(text, ConversationManager.CurrentText);
            Assert.AreSame(guard, remote.GetCell(7, 7).Occupants.Single());
            Assert.AreEqual(2, f.Manager.CachedZoneCount); Assert.AreEqual(0, f.NoteCount);
        }

        [Test]
        public void ThirdSavedNotePreservesPriorFamiliesAndSurvivesAbsentCurrentSelection()
        {
            using var f = new SpreadDiscoveryFixture(includeWayhouse: true);
            f.Select(f.Offer("ditch-cutters")); f.Select(f.Offer("chalk-ring-viper"));
            var originals = f.Player.Properties.ToDictionary(p => p.Key, p => p.Value);
            f.Select(f.Offer(Family)); f.Select(f.Offer(Family));
            Assert.AreEqual(3, f.NoteCount);
            foreach (var original in originals) Assert.AreEqual(original.Value, f.Player.Properties[original.Key]);
            string[] expected = SpreadDiscoveryFixture.Read(f.Player); Assert.AreEqual(3, expected.Length);
            using var bytes = new MemoryStream();
            SaveGraphSerializer.SaveEntityBody(f.Player, new SaveWriter(bytes)); bytes.Position = 0;
            var loaded = new Entity(); SaveGraphSerializer.LoadEntityBody(loaded, new SaveReader(bytes, null));
            f.Wayhouse(SpreadWayhousePlan.Restore(null)); f.Zone.RemoveEntity(f.Speaker);
            CollectionAssert.AreEqual(expected, SpreadDiscoveryFixture.Read(loaded));
            // Unknown prefix keys do not extend the fixed three-family bound.
            string key = SpreadDiscoveryFixture.Prefix + Family;
            for (int i = 0; i < 25; i++) loaded.Properties[SpreadDiscoveryFixture.Prefix + "unknown" + i] = loaded.Properties[key];
            Assert.AreEqual(3, SpreadDiscoveryFixture.Read(loaded).Length);
            string corrupt = loaded.Properties[key].Replace("\"Version\":1", "\"Version\":91");
            Assert.AreNotEqual(loaded.Properties[key], corrupt); loaded.Properties[key] = corrupt;
            Assert.AreEqual(2, SpreadDiscoveryFixture.Read(loaded).Length);
            Assert.AreEqual(corrupt, loaded.Properties[key]);
        }
    }
}
