using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadDiscoveryAdversarialTests
    {
        [TestCase("unchanged")][TestCase("removed")][TestCase("dead")][TestCase("death-handled")]
        [TestCase("moved")][TestCase("foreign-physics")][TestCase("foreign-conversation")]
        [TestCase("foreign-render")][TestCase("hostile")][TestCase("cache-replaced")]
        [TestCase("player-replaced")][TestCase("node-replaced")][TestCase("conversation-ended")]
        [TestCase("map-changed")][TestCase("plan-replaced")]
        public void OfferedChoiceRevalidatesExactCurrentAuthorityBeforeWriting(string mutation)
        {
            using var f = new SpreadDiscoveryFixture(); var offered = f.Offer("ditch-cutters");
            Entity replacement = null;
            switch (mutation)
            {
                case "removed": f.Zone.RemoveEntity(f.Speaker); break;
                case "dead": f.Speaker.GetStat("Hitpoints").BaseValue = 0; break;
                case "death-handled": f.Speaker.SetTag("_DeathHandled"); break;
                case "moved": f.Zone.RemoveEntity(f.Speaker); Assert.True(f.Zone.AddEntity(f.Speaker, 11, 11)); break;
                case "foreign-physics": f.Speaker.GetPart<PhysicsPart>().ParentEntity = new Entity(); break;
                case "foreign-conversation": f.Speaker.GetPart<ConversationPart>().ParentEntity = new Entity(); break;
                case "foreign-render": f.Speaker.GetPart<RenderPart>().ParentEntity = new Entity(); break;
                case "hostile": f.Speaker.SetTag("Faction", "Villagers"); PlayerReputation.Set("Villagers", -200); Assert.True(FactionManager.IsHostile(f.Speaker, f.Player)); break;
                case "cache-replaced": f.Manager.SetActiveZone(new Zone(f.Zone.ZoneID)); break;
                case "node-replaced": ConversationManager.CurrentNode = new NodeData { ID = "RegionOverview" }; break;
                case "conversation-ended": ConversationManager.EndConversation(); break;
                case "map-changed":
                    var at = WorldMap.FromZoneID(f.Manager.RareEncounters.PairZoneID); f.Manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Beating; break;
                case "plan-replaced": f.Plan(SpreadRareEncounterPlan.Create(f.Manager)); break;
                case "player-replaced":
                    f.Zone.RemoveEntity(f.Player);
                    replacement = new Entity { ID = f.Player.ID, BlueprintName = "Player" };
                    replacement.AddPart(new PhysicsPart()); replacement.SetTag("Player");
                    replacement.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 40, Max = 40, Owner = replacement };
                    Assert.True(f.Zone.AddEntity(replacement, 10, 10));
                    ConversationManager.Listener = replacement; StoryletPart.LocalPlayer = replacement; break;
            }
            Assert.AreEqual(mutation == "unchanged", f.Execute(offered));
            Assert.AreEqual(mutation == "unchanged" ? 1 : 0, f.NoteCount);
            if (replacement != null) Assert.IsEmpty(replacement.Properties);
        }

        [Test]
        public void ACallbackBeforeRememberCannotSpendAStaleOffer()
        {
            using var f = new SpreadDiscoveryFixture(); var choice = f.Offer("ditch-cutters");
            ConversationActions.Register("DiscoveryTestSwap", (_, __, ___) => f.Zone.RemoveEntity(f.Speaker));
            choice.Actions.Insert(0, new ConversationParam { Key = "DiscoveryTestSwap", Value = "" });
            Assert.False(f.Execute(choice)); Assert.AreEqual(0, f.NoteCount);
        }

        [TestCase(false)][TestCase(true)]
        public void OldChoiceCannotBeReplayedAfterRefreshButFreshChoiceCan(bool refresh)
        {
            using var f = new SpreadDiscoveryFixture(); var old = f.Offer("ditch-cutters");
            if (refresh) ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(!refresh, f.Execute(old));
            if (refresh) Assert.True(f.Execute(f.Offer("ditch-cutters")));
            Assert.AreEqual(1, f.NoteCount);
        }

        [TestCase("garbage")][TestCase("")][TestCase(null)]
        public void UnofferedActionArgumentsAreRefusedWithoutNote(string argument)
        {
            using var f = new SpreadDiscoveryFixture(); Assert.AreEqual(2, f.Reports.Length);
            Assert.False(ConversationActions.TryExecute(SpreadDiscoveryFixture.Action, f.Speaker, f.Player, argument));
            Assert.AreEqual(0, f.NoteCount);
        }

        [TestCase("json")][TestCase("version")][TestCase("origin")][TestCase("destination")]
        [TestCase("name-size")][TestCase("family")]
        public void OneMalformedSavedRecordDoesNotHideTheValidOtherFamily(string corruption)
        {
            using var f = new SpreadDiscoveryFixture();
            f.Select(f.Offer("ditch-cutters")); f.Select(f.Offer("chalk-ring-viper"));
            string key = SpreadDiscoveryFixture.Prefix + "ditch-cutters", original = f.Player.Properties[key];
            string changed = corruption switch
            {
                "json" => "{broken",
                "version" => original.Replace("\"Version\":1", "\"Version\":91"),
                "origin" => original.Replace("Overworld.10.10.0", "Overworld.010.10.0"),
                "destination" => original.Replace(f.Manager.RareEncounters.PairZoneID, "Overworld.0.0.99"),
                "name-size" => original.Replace("Mera", new string('x', 2049)),
                "family" => original.Replace("ditch-cutters", "invented-family"),
                _ => throw new InvalidOperationException()
            };
            Assert.AreNotEqual(original, changed, "The malformed wire probe must actually change its field.");
            f.Player.Properties[key] = changed;
            var notes = SpreadDiscoveryFixture.Read(f.Player);
            Assert.AreEqual(1, notes.Length); StringAssert.Contains("viper", notes[0].ToLowerInvariant());
            Assert.AreEqual(changed, f.Player.Properties[key], "Reading refuses but does not repair saved data.");
        }

        [Test]
        public void UnknownPrefixFamiliesCannotBypassTwoNoteBoundOrSuppressValidRows()
        {
            using var f = new SpreadDiscoveryFixture(); f.Select(f.Offer("ditch-cutters")); f.Select(f.Offer("chalk-ring-viper"));
            string valid = f.Player.Properties[SpreadDiscoveryFixture.Prefix + "ditch-cutters"];
            for (int i = 0; i < 25; i++) f.Player.Properties[SpreadDiscoveryFixture.Prefix + "unknown" + i] = valid;
            Assert.AreEqual(2, SpreadDiscoveryFixture.Read(f.Player).Length);
            Assert.IsEmpty(SpreadDiscoveryFixture.Read(null));
        }

        [Test]
        public void SameOriginBearingsAreHistoricalAfterInformantRenameAndMapChange()
        {
            using var f = new SpreadDiscoveryFixture(); f.Select(f.Offer("ditch-cutters"));
            string note = SpreadDiscoveryFixture.Read(f.Player).Single();
            f.Speaker.GetPart<RenderPart>().DisplayName = "Different name";
            var at = WorldMap.FromZoneID(f.Manager.RareEncounters.PairZoneID); f.Manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Cave;
            Assert.AreEqual(note, SpreadDiscoveryFixture.Read(f.Player).Single());
            StringAssert.Contains("Mera", note); StringAssert.DoesNotContain("Different name", note);
        }
    }
}
