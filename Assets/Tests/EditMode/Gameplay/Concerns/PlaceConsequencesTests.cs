using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// C4 (Docs/PLACE-WORK-AND-PROGRESSION.md, Consequences): a place's conditions change
    /// the place. Each authored condition (Content/Data/PlaceConditions.json) can move its
    /// traders' prices, keep a resident's goods in stock, give residents a line and
    /// neighbouring places the news, and bring someone to live there; concerns set them,
    /// teach powers and open the next concern.
    /// </summary>
    public sealed class PlaceConsequencesTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, farmer;
        Zone sill, gantry, tine;
        int turn = 1000;
        const string SillZone = "Overworld.10.10.0", GantryZone = "Overworld.7.8.0", TineZone = "Overworld.13.7.0";

        const string Conditions = @"{""Conditions"":[{
  ""Place"":""Sill"",""Condition"":""ProbeTithe"",""Price"":1.25,
  ""Remarks"":[""The Concord's man came for the tithe again.""],
  ""HeardIn"":[""Gantry""],""Heard"":""Sill pays the Concord for its own water now."",
  ""Stock"":[{""Owner"":""Farmer"",""Blueprint"":""Waterskin"",""Count"":2}],
  ""Arrivals"":[{""Blueprint"":""ConcordFactor"",""Count"":1}]}]}";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            PlaceConditionCatalog.LoadFromJson(Conditions);
            StoryletRegistry.Reset(); ConcernRegistry.Reset();
            MessageLog.Clear(); Diag.ResetAll(); ConversationActions.Factory = factory;
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            PlayerReputation.Reset();
            new SettlementManager(() => turn, id => PlaceSiteCatalog.PlaceAt(id) is string name ? new PointOfInterest(POIType.Village, name) : null);
            sill = new Zone(SillZone); gantry = new Zone(GantryZone); tine = new Zone(TineZone);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            farmer = factory.CreateEntity("Farmer");
            sill.AddEntity(player, 10, 10); sill.AddEntity(farmer, 11, 10);
        }
        [TearDown] public void Cleanup()
        {
            PlaceConditionCatalog.Reset(); StoryletRegistry.Reset(); ConcernRegistry.Reset();
            StoryletPart.Current = null; StoryletPart.LocalPlayer = null; NarrativeStatePart.Current = null;
            SettlementManager.ResetCurrent(); ConversationActions.Factory = null;
        }

        void Set(bool on = true) => SettlementManager.Current.SetCondition(SillZone, "ProbeTithe", on);
        int Carried(Entity who, string bp) => ConcernItems.CountCarried(who, bp);

        // ════════════════ setting conditions ════════════════

        [Test] public void AConcernOutcomeSetsAPlaceCondition()
        {
            Assert.IsNull(ConversationActions.ExecuteRequired("SetPlaceCondition", farmer, player, "Sill:ProbeTithe"));
            Assert.True(SettlementManager.Current.HasCondition(SillZone, "ProbeTithe"));
            Assert.AreEqual("unknown_place", ConversationActions.ExecuteRequired("SetPlaceCondition", farmer, player, "Atlantis:ProbeTithe"));
            Assert.AreEqual("invalid_argument", ConversationActions.ExecuteRequired("SetPlaceCondition", farmer, player, "Sill"));
        }

        // ════════════════ prices ════════════════

        [Test] public void AConditionMovesThePricesOfItsPlacesTradersOnly()
        {
            var item = factory.CreateEntity("Torch");
            int before = TradeSystem.GetBuyPrice(item, 1.0, farmer), sellBefore = TradeSystem.GetSellPrice(item, 1.0, farmer);
            var elsewhere = factory.CreateEntity("Farmer"); tine.AddEntity(elsewhere, 4, 4);
            int other = TradeSystem.GetBuyPrice(item, 1.0, elsewhere);
            Set();
            Assert.Greater(TradeSystem.GetBuyPrice(item, 1.0, farmer), before, "the tithe raises Sill's prices");
            Assert.LessOrEqual(TradeSystem.GetSellPrice(item, 1.0, farmer), sellBefore, "and lowers what Sill pays");
            Assert.AreEqual(other, TradeSystem.GetBuyPrice(item, 1.0, elsewhere), "counter-check: Tine is not tithed");
            Set(false);
            Assert.AreEqual(before, TradeSystem.GetBuyPrice(item, 1.0, farmer), "counter-check: lifted with the condition");
        }

        // ════════════════ stock ════════════════

        [Test] public void AConditionKeepsAResidentStockedOnArrival()
        {
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.AreEqual(0, Carried(farmer, "Waterskin"), "no condition, no stock");
            Set();
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.AreEqual(2, Carried(farmer, "Waterskin"));
            var inv = farmer.GetPart<InventoryPart>(); inv.RemoveObject(inv.Objects.First(o => o.BlueprintName == "Waterskin"));
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.AreEqual(1, Carried(farmer, "Waterskin"), "not refilled within the restock interval");
            turn += TraderRestockSystem.RestockIntervalTurns + 1;
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.AreEqual(2, Carried(farmer, "Waterskin"), "topped up again after it");
        }

        // ════════════════ arrivals ════════════════

        [Test] public void AConditionBringsSomeoneToLiveThereOnce()
        {
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.False(sill.GetAllEntities().Any(e => e.BlueprintName == "ConcordFactor"));
            Set();
            PlaceConsequences.OnZoneEntered(player, sill, arriving: false);
            Assert.False(sill.GetAllEntities().Any(e => e.BlueprintName == "ConcordFactor"), "only on a real arrival");
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            PlaceConsequences.OnZoneEntered(player, sill, arriving: true);
            Assert.AreEqual(1, sill.GetAllEntities().Count(e => e.BlueprintName == "ConcordFactor"), "once");
            Assert.False(gantry.GetAllEntities().Any(e => e.BlueprintName == "ConcordFactor"));
        }

        // ════════════════ lines ════════════════

        [Test] public void ResidentsSayItAndNeighboursHaveHeard()
        {
            Assert.IsNull(PlaceConsequences.LineFor(SillZone));
            Set();
            Assert.AreEqual("The Concord's man came for the tithe again.", PlaceConsequences.LineFor(SillZone));
            Assert.AreEqual("Sill pays the Concord for its own water now.", PlaceConsequences.LineFor(GantryZone), "Gantry has heard");
            Assert.IsNull(PlaceConsequences.LineFor(TineZone), "Tine has not");
        }

        [Test] public void AResidentSpeaksThePlacesLine()
        {
            Set();
            var said = PlaceConsequences.TryRemark(player, sill, force: true);
            Assert.True(said);
            Assert.True(MessageLog.GetRecent(5).Any(m => m.Contains("came for the tithe again")));
        }

        // ════════════════ taught powers and the next concern ════════════════

        [Test] public void AConcernCanTeachAPowerOnce()
        {
            var skills = player.GetPart<SkillsPart>();
            Assume.That(skills, Is.Not.Null, "precondition: the player has skills");
            SkillRegistry.EnsureInitialized();
            string skill = SkillRegistry.GetAllSkills().First(s => !skills.HasSkill(s.Class)).Class;
            Assert.IsNull(ConversationActions.ExecuteRequired("TeachSkill", farmer, player, skill));
            Assert.True(skills.HasSkill(skill));
            Assert.AreEqual("already_known", ConversationActions.ExecuteRequired("TeachSkill", farmer, player, skill));
            Assert.AreEqual("unknown_skill", ConversationActions.ExecuteRequired("TeachSkill", farmer, player, "NoSuchSkill"));
        }

        [Test] public void TheNextConcernWaitsForTheFirstToBeDone()
        {
            ConcernRegistry.LoadFromJson(@"{""Concerns"":[
 {""Id"":""p.first"",""Place"":""Sill"",""Owner"":""Farmer"",""Tier"":1,""Title"":""First"",""Pitch"":""a"",""Thanks"":""b"",
  ""Approaches"":[{""Id"":""x"",""Kind"":""Settle"",""Label"":""x"",""Objective"":""x"",""Triggers"":[{""Key"":""IfFact"",""Value"":""px:>=:1""}]},
                  {""Id"":""y"",""Kind"":""Settle"",""Label"":""y"",""Objective"":""y"",""Triggers"":[{""Key"":""IfFact"",""Value"":""py:>=:1""}]}],
  ""Refusal"":{""Text"":""n"",""Reply"":""m""},""Next"":[""p.second""]},
 {""Id"":""p.second"",""Place"":""Sill"",""Owner"":""Farmer"",""Tier"":1,""Title"":""Second"",""Pitch"":""a"",""Thanks"":""b"",
  ""Approaches"":[{""Id"":""x"",""Kind"":""Settle"",""Label"":""x"",""Objective"":""x"",""Triggers"":[{""Key"":""IfFact"",""Value"":""qx:>=:1""}]},
                  {""Id"":""y"",""Kind"":""Settle"",""Label"":""y"",""Objective"":""y"",""Triggers"":[{""Key"":""IfFact"",""Value"":""qy:>=:1""}]}],
  ""Refusal"":{""Text"":""n"",""Reply"":""m""}}]}", "probe");
            var second = ConcernRegistry.Get("p.second");
            Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(second, farmer, player), "waits for the first");
            Assert.IsNull(ConcernConversation.Accept(farmer, player, "p.first"));
            NarrativeStatePart.Current.SetFact("px", 1); storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(second, farmer, player), "not while the first awaits its report");
            Assert.IsNull(ConcernConversation.Report(farmer, player, "p.first"));
            Assert.AreEqual(ConcernConversation.State.Offerable, ConcernConversation.StateOf(second, farmer, player));
        }

        [Test] public void ShippedConditionsLintClean()
        {
            PlaceConditionCatalog.Reset();
            var problems = PlaceConditionCatalog.Lint(factory);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            PlaceConditionCatalog.LoadFromJson(Conditions.Replace("Waterskin", "NoSuchSkin").Replace("\"Gantry\"", "\"Atlantis\""));
            var lines = string.Join("|", PlaceConditionCatalog.Lint(factory));
            StringAssert.Contains("NoSuchSkin", lines); StringAssert.Contains("Atlantis", lines);
        }
    }
}
