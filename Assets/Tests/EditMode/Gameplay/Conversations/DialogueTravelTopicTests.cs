using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DialogueTravelTopicTests
    {
        static ChoiceData[] Settlements => ConversationManager.VisibleChoices
            .Where(c => c.Actions.Any(a => a.Key == RegionalGuidance.ActionName)).ToArray();

        static void OpenAuthored(SpreadDiscoveryFixture f, string node)
        {
            ConversationLoader.LoadAll();
            Assert.True(ConversationManager.StartConversation(f.Speaker, f.Player));
            var choices = ConversationManager.VisibleChoices.ToList();
            int index = choices.FindIndex(c => c.Target == node);
            Assert.GreaterOrEqual(index, 0, "The topic must be reachable from the actual authored Start node.");
            Assert.True(ConversationManager.SelectChoice(index));
            Assert.AreEqual(node, ConversationManager.CurrentNode.ID);
        }

        [TestCase("Scribe_1", "WildernessReports")]
        [TestCase("Innkeeper_1", "Rumors")]
        public void AuthoredRootsExposeTwoDistinctTopicsAndBackPreservesNotes(string conversation, string reports)
        {
            using var f = new SpreadDiscoveryFixture(conversation, "Start", true);
            OpenAuthored(f, "RegionOverview");
            Assert.AreEqual(4, Settlements.Length); Assert.IsEmpty(f.Reports);
            Assert.AreEqual(0, f.NoteCount); Assert.IsEmpty(RegionalTravelNotes.Read(f.Player));
            int back = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Start");
            Assert.GreaterOrEqual(back, 0); Assert.True(ConversationManager.SelectChoice(back));
            int report = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == reports);
            Assert.GreaterOrEqual(report, 0); Assert.True(ConversationManager.SelectChoice(report));
            Assert.AreEqual(3, f.Reports.Length); Assert.IsEmpty(Settlements);
            Assert.AreEqual(0, f.NoteCount); Assert.IsEmpty(RegionalTravelNotes.Read(f.Player));
        }

        [TestCase("Scribe_1")][TestCase("Innkeeper_1")]
        public void SettlementDescriptionsAndChoicesUseSameOrderedDestinations(string conversation)
        {
            using var f = new SpreadDiscoveryFixture(conversation, "RegionOverview", true);
            ConversationManager.CurrentNode.Text = "Directions begin here.";
            var leads = RegionalGuidance.Build(f.Zone, f.Speaker, f.Player);
            Assert.AreEqual(4, leads.Count); Assert.AreEqual(leads.Count, Settlements.Length);
            string text = ConversationManager.CurrentText; int previous = -1;
            for (int i = 0; i < leads.Count; i++)
            {
                var lead = leads[i]; var at = WorldMap.FromZoneID(lead.DestinationZoneID);
                StringAssert.Contains(lead.DestinationName, Settlements[i].Text);
                StringAssert.Contains("(" + at.x + "," + at.y + ")", Settlements[i].Text);
                Assert.AreEqual(lead.DestinationZoneID, Settlements[i].Actions.Single(a => a.Key == RegionalGuidance.ActionName).Value);
                int position = text.IndexOf(lead.Text, StringComparison.Ordinal);
                Assert.Greater(position, previous, "Every offered destination needs its complete matching description in the same order."); previous = position;
            }
            Assert.IsEmpty(f.Reports); StringAssert.DoesNotContain("ditch-cutters", text);
            StringAssert.DoesNotContain("chalk-ring vipers", text); StringAssert.DoesNotContain("Turnbank", text);
            Assert.AreEqual(1, f.Manager.CachedZoneCount); Assert.IsEmpty(StoryletPart.Current.GetActiveQuests());
        }

        [TestCase("Scribe_1", "WildernessReports")][TestCase("Innkeeper_1", "Rumors")]
        public void WildernessReportsMatchChoicesAndExcludeSettlementActions(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node, true);
            Assert.AreEqual(3, f.Reports.Length); Assert.IsEmpty(Settlements);
            string text = ConversationManager.CurrentText; int previous = -1;
            foreach (var offer in f.Reports)
            {
                string name = offer.Text.Replace("Remember ", "").Replace(" (unconfirmed)", "");
                int position = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
                Assert.Greater(position, previous); previous = position;
                var id = offer.Actions.Single(a => a.Key == SpreadDiscoveryReports.ActionName).Value.Split('|')[1];
                var at = WorldMap.FromZoneID(id); StringAssert.Contains("(" + at.x + "," + at.y + ")", text);
            }
            StringAssert.Contains("unconfirmed", text);
            Assert.AreEqual(1, f.Manager.CachedZoneCount); Assert.AreEqual(0, f.NoteCount);
        }

        [TestCase("Scribe_1", "WildernessReports")][TestCase("Innkeeper_1", "Rumors")]
        public void ExplicitNotesRemainInTheirTopicAndSwitchingInvalidatesOldReport(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node, true);
            var report = f.Offer("ditch-cutters"); f.Select(report);
            Assert.AreEqual(1, f.NoteCount); Assert.IsEmpty(RegionalTravelNotes.Read(f.Player));
            var stale = f.Offer("chalk-ring-viper");
            f.Node("RegionOverview"); Assert.IsFalse(f.Execute(stale)); Assert.AreEqual(1, f.NoteCount);
            Assert.AreEqual(4, Settlements.Length); f.Select(Settlements[0]);
            Assert.AreEqual(1, RegionalTravelNotes.Read(f.Player).Count); Assert.AreEqual(1, f.NoteCount);
            Assert.AreEqual("RegionOverview", ConversationManager.CurrentNode.ID);
            Assert.AreEqual(1, f.Manager.CachedZoneCount); Assert.IsEmpty(StoryletPart.Current.GetActiveQuests());
        }

        [TestCase("Scribe_1")][TestCase("Innkeeper_1")]
        public void EmptySettlementSelectionHasNoUnmentionedOrInventedDestination(string conversation)
        {
            using var f = new SpreadDiscoveryFixture(conversation, "RegionOverview", true);
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++) f.Manager.WorldMap.SetPOI(x, y, null);
            ConversationManager.RefreshVisibleChoices();
            Assert.IsEmpty(Settlements); Assert.IsEmpty(f.Reports);
            StringAssert.Contains("no settlement directions", ConversationManager.CurrentText.ToLowerInvariant());
            Assert.AreEqual(1, f.Manager.CachedZoneCount);
        }

        [TestCase("Scribe_1", "Explain")][TestCase("Innkeeper_1", "GossipSeed")]
        public void UnrelatedDialogueKeepsItsOwnTextAndNoTravelOptions(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node, true);
            ConversationManager.CurrentNode.Text = "The authored subject remains intact.";
            Assert.AreEqual(ConversationManager.CurrentNode.Text, ConversationManager.CurrentText);
            Assert.IsEmpty(Settlements); Assert.IsEmpty(f.Reports);
        }
    }
}
