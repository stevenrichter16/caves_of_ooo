using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// C3b (Docs/PLACE-WORK-AND-PROGRESSION.md, Confront): a threat at a specific place
    /// near the concern's place. The site is resolved when the concern is undertaken (the
    /// authored offset, or the nearest zone beside it if a place stands there) and kept;
    /// its foes are placed when the player actually arrives there, once; the approach is
    /// finished when every one of them has died, whoever killed them.
    /// </summary>
    public sealed class ConcernConfrontTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, keeper;
        Zone sill, current;
        const string Id = "probe.den";
        static string Q => ConcernRegistry.QuestIdFor(Id);

        static string Json(string offset) => @"{""Concerns"":[{
  ""Id"":""probe.den"",""Place"":""Sill"",""Owner"":""WellKeeper"",""Faction"":""Villagers"",""Tier"":1,
  ""Title"":""Probe Den"",""Pitch"":""Something digs."",""Thanks"":""Quiet now."",
  ""Approaches"":[
    {""Id"":""den"",""Kind"":""Confront"",""Label"":""Clear the den."",""Objective"":""Clear the den two fields west of Sill"",
     ""Confront"":{""Offset"":""" + offset + @""",""Site"":""the den two fields west of Sill"",
                   ""Foes"":[{""Blueprint"":""MarlbackScrabbler"",""Count"":2},{""Blueprint"":""DirtGnome"",""Count"":1}]}},
    {""Id"":""pay"",""Kind"":""Settle"",""Label"":""Pay."",""Objective"":""Pay someone"",
     ""Triggers"":[{""Key"":""IfFact"",""Value"":""probe_paid:>=:1""}]}
  ],
  ""Refusal"":{""Text"":""No."",""Reply"":""Fine.""},""Reward"":{""XP"":10}}]}";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
        }
        void Load(string offset = "-2,0")
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(Json(offset), "probe");
        }
        [SetUp] public void Setup()
        {
            Load(); MessageLog.Clear(); Diag.ResetAll();
            ConversationActions.Factory = factory;
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            sill = new Zone("Overworld.10.10.0");
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            keeper = factory.CreateEntity("WellKeeper");
            sill.AddEntity(player, 10, 10); sill.AddEntity(keeper, 11, 10); current = sill;
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; ConversationActions.Factory = null;
        }

        void Accept() => Assert.IsNull(ConcernConversation.Accept(keeper, player, Id));
        Zone Arrive(string zoneId)
        {
            var zone = new Zone(zoneId);
            current.RemoveEntity(player); zone.AddEntity(player, 2, 12); current = zone;
            ConcernWorld.OnZoneEntered(player, zone, arriving: true);
            return zone;
        }
        static Entity[] Foes(Zone zone) => zone.GetAllEntities().Where(e => e.GetProperty(ConcernWorld.FoeProperty) != null).ToArray();
        int Approach => NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact(Id));

        // ════════════════ the site ════════════════

        [Test] public void UndertakingResolvesTheAuthoredSiteAndKeepsIt()
        {
            Assert.IsNull(ConcernWorld.SiteOf(Id, "den"), "no site before the concern is undertaken");
            Accept();
            Assert.AreEqual("Overworld.8.10.0", ConcernWorld.SiteOf(Id, "den"), "two fields west of Sill");
            ConcernConversation.Accept(keeper, player, Id);
            Assert.AreEqual("Overworld.8.10.0", ConcernWorld.SiteOf(Id, "den"), "kept, not re-resolved");
        }

        [Test] public void ASiteOnAPlaceStepsAsideToTheNearestOpenZone()
        {
            Load("3,-3"); // Sill (10,10) + (3,-3) is Tine (13,7)
            Accept();
            string site = ConcernWorld.SiteOf(Id, "den");
            Assert.NotNull(site); Assert.AreNotEqual("Overworld.13.7.0", site, "never in a place");
            var (x, y, z) = WorldMap.FromZoneID(site);
            Assert.AreEqual(0, z);
            Assert.LessOrEqual(System.Math.Max(System.Math.Abs(x - 13), System.Math.Abs(y - 7)), 1, "right beside it");
        }

        // ════════════════ the foes ════════════════

        [Test] public void ArrivingAtTheSitePlacesItsFoesOnce()
        {
            Assert.AreEqual(0, Foes(Arrive("Overworld.8.10.0")).Length, "not undertaken: nothing there");
            Accept();
            var den = Arrive("Overworld.8.10.0");
            var foes = Foes(den);
            Assert.AreEqual(3, foes.Length);
            Assert.AreEqual(2, foes.Count(f => f.BlueprintName == "MarlbackScrabbler")); Assert.AreEqual(1, foes.Count(f => f.BlueprintName == "DirtGnome"));
            Assert.True(foes.All(f => f.GetPart<AddFactWhenSlain>()?.Fact == ConcernRegistry.SlainFact(Id, "den")));
            Assert.True(foes.All(f => FactionManager.IsHostile(f, player)), "they are a threat to the player");
            ConcernWorld.OnZoneEntered(player, den, arriving: true);
            Assert.AreEqual(3, Foes(den).Length, "re-entry places no more");
            Assert.AreEqual(0, Foes(Arrive("Overworld.9.10.0")).Length, "only at the site");
        }

        [Test] public void FoesAreNotPlacedByAcceptingOnlyByArriving()
        {
            Accept();
            Assert.AreEqual(0, ConcernWorld.OnZoneEntered(player, new Zone("Overworld.8.10.0"), arriving: false));
        }

        [Test] public void TheApproachFinishesWhenEveryFoeHasDied()
        {
            Accept(); var foes = Foes(Arrive("Overworld.8.10.0"));
            foreach (var f in foes.Take(2)) f.FireEvent(GameEvent.New("Died"));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(0, Approach, "two of three is not done");
            foes[2].FireEvent(GameEvent.New("Died"));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(1, Approach, "the den is cleared");
        }

        [Test] public void TheStatusNamesWhereTheSiteIs()
        {
            Accept();
            ConversationManager.Speaker = keeper; ConversationManager.Listener = player;
            var node = ConcernConversation.BuildNode("status|" + Id, keeper, player);
            StringAssert.Contains("the den two fields west of Sill", node.Text);
        }

        // ════════════════ lint ════════════════

        [Test] public void LintRejectsUnplayableConfrontations()
        {
            var c = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(ConcernRegistry.Get(Id)));
            c.Approaches[0].Confront.Foes[0].Blueprint = "NoSuchBeast";
            c.Approaches[0].Confront.Foes[1].Count = 0;
            c.Approaches[0].Confront.Offset = "0,0";
            var lines = string.Join("|", ConcernRegistry.Lint(c, factory));
            StringAssert.Contains("unknown foe", lines); StringAssert.Contains("foe count", lines); StringAssert.Contains("offset", lines);

            var high = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(ConcernRegistry.Get(Id)));
            high.Approaches[0].Confront.Foes[0].Blueprint = "ObsidianBrute";
            StringAssert.Contains("tier", string.Join("|", ConcernRegistry.Lint(high, factory)), "a tier-1 concern cannot send the player at a tier-5 foe");
            Assert.IsEmpty(ConcernRegistry.Lint(ConcernRegistry.Get(Id), factory), "counter-check");
        }
        // ════════════════ adversarial ════════════════

        [Test] public void Adversarial_ARefusedConcernPlacesNoFoes()
        {
            Accept();
            Assert.IsNull(ConcernConversation.Refuse(keeper, player, Id));
            Assert.AreEqual(0, Foes(Arrive("Overworld.8.10.0")).Length);
        }

        [Test] public void Adversarial_AFoeThatCannotBePlacedCannotLockTheApproach()
        {
            var c = ConcernRegistry.Get(Id);
            c.Approaches[0].Confront.Foes[1].Blueprint = "NoSuchBeastAtRuntime"; // bypasses lint, as stale content would
            Accept();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("unknown blueprint 'NoSuchBeastAtRuntime'"));
            var foes = Foes(Arrive("Overworld.8.10.0"));
            Assert.AreEqual(2, foes.Length);
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ConcernRegistry.SlainFact(Id, "den")), "the missing one counts as gone");
            foreach (var f in foes) f.FireEvent(GameEvent.New("Died"));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(1, Approach);
        }

        [Test] public void Adversarial_TheSiteSurvivesAContentReload()
        {
            Accept();
            Load();
            Assert.AreEqual("Overworld.8.10.0", ConcernWorld.SiteOf(Id, "den"), "the site is world state, not content state");
        }
    }
}
