using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadDiscoveryTextTests
    {
        [TestCase("Scribe_1", "RegionOverview")]
        [TestCase("Innkeeper_1", "Rumors")]
        public void FullReportIsReadableBeforeRememberingAndDoesNotRewriteAuthoredNode(string conversation, string node)
        {
            using var f = new SpreadDiscoveryFixture(conversation, node);
            ConversationManager.CurrentNode.Text = "There are other villages along the road.";
            string authored = ConversationManager.CurrentNode.Text;
            string text = ConversationManager.CurrentText;
            StringAssert.Contains(authored, text);
            StringAssert.Contains("ditch-cutters", text.ToLowerInvariant());
            StringAssert.Contains("viper", text.ToLowerInvariant());
            StringAssert.Contains("poison", text.ToLowerInvariant());
            StringAssert.Contains("unconfirmed", text.ToLowerInvariant());
            var pair = WorldMap.FromZoneID(f.Manager.RareEncounters.PairZoneID);
            StringAssert.Contains(pair.x + "," + pair.y, text);
            Assert.AreEqual(authored, ConversationManager.CurrentNode.Text);
            Assert.AreEqual(0, f.NoteCount); Assert.AreEqual(1, f.Manager.CachedZoneCount);
            Assert.True(f.Reports.All(c => c.Text.Length <= 52), "Complete prose belongs in wrapped dialogue, not clipped one-row action labels.");
            f.Node("Start"); ConversationManager.CurrentNode.Text = "Other business?";
            Assert.AreEqual("Other business?", ConversationManager.CurrentText);
        }
    }
}
