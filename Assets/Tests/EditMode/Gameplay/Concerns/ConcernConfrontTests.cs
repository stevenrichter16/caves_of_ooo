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

        static string Json(string offset, string gnomeName = "") => @"{""Concerns"":[{
  ""Id"":""probe.den"",""Place"":""Sill"",""Owner"":""WellKeeper"",""Faction"":""Villagers"",""Tier"":1,
  ""Title"":""Probe Den"",""Pitch"":""Something digs."",""Thanks"":""Quiet now."",
  ""Approaches"":[
    {""Id"":""den"",""Kind"":""Confront"",""Label"":""Clear the den."",""Objective"":""Clear the den two fields west of Sill"",
     ""Confront"":{""Offset"":""" + offset + @""",""Site"":""the den two fields west of Sill"",
                   ""Foes"":[{""Blueprint"":""MarlbackScrabbler"",""Count"":2},{""Blueprint"":""DirtGnome"",""Count"":1,""Name"":""" + gnomeName + @"""}]}},
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
        void Load(string offset = "-2,0", string gnomeName = "")
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(Json(offset, gnomeName), "probe");
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

        [Test] public void AFoeCarriesTheConcernsOwnNameButKeepsItsBody()
        {
            Load(gnomeName: "the one that digs");
            Accept();
            var foes = Foes(Arrive("Overworld.8.10.0"));
            var gnome = foes.Single(f => f.BlueprintName == "DirtGnome");
            Assert.AreEqual("the one that digs", gnome.GetDisplayName(), "named as the concern names it");
            string own = factory.CreateEntity("MarlbackScrabbler").GetDisplayName();
            Assert.True(foes.Where(f => f.BlueprintName == "MarlbackScrabbler").All(f => f.GetDisplayName() == own), "an unnamed foe keeps its own name");
            Assert.AreEqual(factory.CreateEntity("DirtGnome").GetPart<RenderPart>().RenderString, gnome.GetPart<RenderPart>().RenderString, "same body");
        }

        [Test] public void AFoeWithoutANameKeepsItsBlueprintName()
        {
            Accept();
            var gnome = Foes(Arrive("Overworld.8.10.0")).Single(f => f.BlueprintName == "DirtGnome");
            Assert.AreEqual(factory.CreateEntity("DirtGnome").GetDisplayName(), gnome.GetDisplayName());
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
        // ════════════════ withheld work ════════════════

        const string GatedJson = @"{""Concerns"":[{
  ""Id"":""probe.gate"",""Place"":""Sill"",""Owner"":""WellKeeper"",""Faction"":""Villagers"",""Tier"":1,
  ""Title"":""Probe Gate"",""Pitch"":""The real trouble."",""Thanks"":""Thank you."",
  ""Withheld"":""Not to you. Not yet."",
  ""Requires"":[{""Key"":""IfFact"",""Value"":""probe_trusted:>=:1""}],
  ""Approaches"":[{""Id"":""pay"",""Kind"":""Settle"",""Label"":""Pay."",""Objective"":""Pay someone"",
     ""Triggers"":[{""Key"":""IfFact"",""Value"":""probe_paid:>=:1""}]},
    {""Id"":""ask"",""Kind"":""Settle"",""Label"":""Ask."",""Objective"":""Ask someone"",
     ""Triggers"":[{""Key"":""IfFact"",""Value"":""probe_asked:>=:1""}]}],
  ""Refusal"":{""Text"":""No."",""Reply"":""Fine.""},""Reward"":{""XP"":10}}]}";

        void Talk()
        {
            ConversationManager.Speaker = keeper; ConversationManager.Listener = player;
            var start = new NodeData { ID = "Start", Text = "authored" };
            start.Choices.Add(new ChoiceData { Text = "[Leave]", Target = "End" });
            ConversationManager.CurrentConversation = new ConversationData { ID = "Probe", Nodes = { start } };
            ConversationManager.CurrentNode = start;
            ConversationManager.RefreshVisibleChoices();
        }
        static int Choice(string text)
        {
            var list = ConversationManager.VisibleChoices;
            for (int i = 0; i < list.Count; i++) if (list[i].Text.Contains(text)) return i;
            return -1;
        }

        [Test] public void AGatedConcernsOwnerSaysWhyAndOffersNothing()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(GatedJson, "probe");
            var c = ConcernRegistry.Get("probe.gate");
            Assert.AreEqual(ConcernConversation.State.Withheld, ConcernConversation.StateOf(c, keeper, player));
            Talk();
            Assert.GreaterOrEqual(Choice("Is something wrong here?"), 0, "the owner still answers");
            ConversationManager.SelectChoice(Choice("Is something wrong here?"));
            StringAssert.Contains("Not to you. Not yet.", ConversationManager.CurrentText);
            StringAssert.DoesNotContain("The real trouble.", ConversationManager.CurrentText);
            Assert.AreEqual(-1, Choice("I'll see to it."), "nothing to accept");
            Assert.AreEqual(QuestCueState.None, ConcernConversation.CueFor(keeper, sill, player), "no cue for withheld work");
            Assert.IsNotNull(ConcernConversation.Accept(keeper, player, "probe.gate"), "and it cannot be accepted by action either");

            NarrativeStatePart.Current.SetFact("probe_trusted", 1);
            Assert.AreEqual(ConcernConversation.State.Offerable, ConcernConversation.StateOf(c, keeper, player), "counter-check: the gate opens");
            Talk(); ConversationManager.SelectChoice(Choice("Is something wrong here?"));
            StringAssert.Contains("The real trouble.", ConversationManager.CurrentText);
        }

        [Test] public void WithoutWithheldTextAGatedConcernStaysHidden()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(GatedJson.Replace(@"""Withheld"":""Not to you. Not yet."",", ""), "probe");
            Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(ConcernRegistry.Get("probe.gate"), keeper, player));
            Talk();
            Assert.AreEqual(-1, Choice("Is something wrong here?"));
        }

        [Test] public void LintRejectsWithheldTextWithNoGate()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(GatedJson, "probe");
            var c = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(ConcernRegistry.Get("probe.gate")));
            Assert.IsEmpty(ConcernRegistry.Lint(c, factory), "counter-check: gated and withheld is fine");
            c.Requires.Clear();
            StringAssert.Contains("withheld", string.Join("|", ConcernRegistry.Lint(c, factory)));
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

        [Test] public void Adversarial_ANamedFoeKeepsItsNameThroughSaveAndLoad()
        {
            Load(gnomeName: "the one that digs");
            Accept();
            var gnome = Foes(Arrive("Overworld.8.10.0")).Single(f => f.BlueprintName == "DirtGnome");
            Assert.AreEqual("the one that digs", PartRoundTripHelper.RoundTripEntity(gnome).GetDisplayName());
        }

        [Test] public void Adversarial_AWhitespaceNameIsNoName()
        {
            Load(gnomeName: "   ");
            Accept();
            var gnome = Foes(Arrive("Overworld.8.10.0")).Single(f => f.BlueprintName == "DirtGnome");
            Assert.AreEqual(factory.CreateEntity("DirtGnome").GetDisplayName(), gnome.GetDisplayName());
        }

        [Test] public void Adversarial_WithheldWorkCannotBeRefusedOrListed()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(GatedJson, "probe");
            Assert.IsNotNull(ConcernConversation.Refuse(keeper, player, "probe.gate"), "nothing offered, nothing to refuse");
            Assert.IsNull(storylets.GetClosure(ConcernRegistry.QuestIdFor("probe.gate")), "no closure written");
            var elder = factory.CreateEntity(ConcernConversation.ElderRole); sill.AddEntity(elder, 12, 10);
            ConversationManager.Speaker = elder; ConversationManager.Listener = player;
            var start = new NodeData { ID = "Start", Text = "authored" };
            ConversationManager.CurrentConversation = new ConversationData { ID = "Probe", Nodes = { start } };
            ConversationManager.CurrentNode = start; ConversationManager.RefreshVisibleChoices();
            Assert.AreEqual(-1, Choice("What troubles this place?"), "the Elder does not list withheld work");
            StringAssert.DoesNotContain("Probe Gate", ConcernConversation.BuildNode("list|Sill", elder, player).Text);
        }

        [Test] public void Adversarial_AConcernWaitingOnAPredecessorIsHiddenNotWithheld()
        {
            StoryletRegistry.Reset(); ConcernRegistry.Reset();
            ConcernRegistry.LoadFromJson(Json("-2,0").Replace(@"""Reward"":{""XP"":10}}", @"""Reward"":{""XP"":10},""Next"":[""probe.gate""]}"), "probe");
            ConcernRegistry.LoadFromJson(GatedJson, "probe");
            Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(ConcernRegistry.Get("probe.gate"), keeper, player),
                "a later chapter is not announced before the earlier one is done");
        }

        [Test] public void Adversarial_TheSiteSurvivesAContentReload()
        {
            Accept();
            Load();
            Assert.AreEqual("Overworld.8.10.0", ConcernWorld.SiteOf(Id, "den"), "the site is world state, not content state");
        }
    }
}
