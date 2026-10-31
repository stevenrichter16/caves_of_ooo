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
    /// C3c (Docs/PLACE-WORK-AND-PROGRESSION.md, Restore): a place's working structures are
    /// data (Content/Data/PlaceSites.json), tracked by the settlement manager beside Sill's
    /// hand-built well, oven and lantern. A lasting repair needs every material and the
    /// tool; a patch holds for a while and relapses; the Concord can contract either; and
    /// a Restore approach of a concern finishes when the site is properly repaired by hand.
    /// </summary>
    public sealed class PlaceSiteTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, keeper;
        Zone tine;
        int turn = 5;
        const string TineZone = "Overworld.13.7.0";

        const string Sites = @"{""Sites"":[{
  ""Place"":""Tine"",""Id"":""Weir"",""Name"":""fishing weir"",
  ""Broken"":""The weir's stakes have rotted through; the fish go past."",
  ""Lasting"":{""Materials"":[{""Blueprint"":""SalvagedTimber"",""Count"":3},{""Blueprint"":""KnotflaxCord"",""Count"":2}],""Tool"":""Hatchet"",
              ""Text"":""You drive new stakes and lash the wattle tight. The weir holds the current.""},
  ""Patch"":{""Materials"":[{""Blueprint"":""KnotflaxCord"",""Count"":1}],""RelapseTurns"":50,
             ""Text"":""You lash the worst gap closed. It will not last.""},
  ""Relapse"":""The weir has gapped again while you were away.""}]}";

        const string Concern = @"{""Concerns"":[{
  ""Id"":""probe.weir"",""Place"":""Tine"",""Owner"":""Villager"",""Faction"":""Villagers"",""Tier"":1,
  ""Title"":""Probe Weir"",""Pitch"":""The weir."",""Thanks"":""Fish again."",
  ""Approaches"":[
    {""Id"":""rebuild"",""Kind"":""Restore"",""Label"":""Rebuild it."",""Objective"":""Rebuild the weir"",""Restore"":{""Site"":""Weir""}},
    {""Id"":""contract"",""Kind"":""Settle"",""Label"":""Pay."",""Objective"":""Pay the Concord"",
     ""Triggers"":[{""Key"":""IfPlaceSiteMethod"",""Value"":""Tine:Weir:Contracted""}],
     ""Offer"":[{""Key"":""ContractPlaceRepair"",""Value"":""Tine:Weir:40""}],""OfferText"":""Pay the Concord""}
  ],
  ""Refusal"":{""Text"":""No."",""Reply"":""Fine.""},""Reward"":{""XP"":10}}]}";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            PlaceSiteCatalog.LoadFromJson(Sites);
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); ConcernRegistry.LoadFromJson(Concern, "probe");
            MessageLog.Clear(); Diag.ResetAll(); ConversationActions.Factory = factory;
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            var poi = new PointOfInterest(POIType.Village, "Tine");
            new SettlementManager(() => turn, id => id == TineZone ? poi : null);
            tine = new Zone(TineZone);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            keeper = factory.CreateEntity("Villager");
            tine.AddEntity(player, 10, 10); tine.AddEntity(keeper, 11, 10);
            TradeSystem.SetDrams(player, 100);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation(); PlaceSiteCatalog.Reset();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent(); ConversationActions.Factory = null;
        }

        static RepairableSiteState Weir => PlaceSites.Site("Tine", "Weir");
        void Give(string bp, int n) { for (int i = 0; i < n; i++) player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(bp)); }
        int Carried(string bp) => ConcernItems.CountCarried(player, bp);
        string Repair(string fix) => ConversationActions.ExecuteRequired("RepairPlaceSite", keeper, player, "Tine:Weir:" + fix);

        // ════════════════ tracking ════════════════

        [Test] public void APlaceWithDataSitesTracksThemFromTheStart()
        {
            Assert.NotNull(Weir); Assert.AreEqual(RepairStage.Fouled, Weir.Stage);
            Assert.AreEqual(RepairableSiteType.Structure, Weir.SiteType);
        }

        [Test] public void ASettlementSavedBeforeItsSitesExistedGainsThem()
        {
            var loaded = new SettlementState { SettlementId = TineZone, SettlementName = "Tine" };
            SettlementManager.Current.RestoreSettlements(new System.Collections.Generic.Dictionary<string, SettlementState> { { TineZone, loaded } });
            Assert.NotNull(Weir, "the weir is added to the loaded settlement");
            Assert.AreEqual(RepairStage.Fouled, Weir.Stage);
        }

        // ════════════════ repairing ════════════════

        [Test] public void ALastingRepairNeedsEveryMaterialAndTheToolAndTakesOnlyMaterials()
        {
            Give("SalvagedTimber", 3); Give("KnotflaxCord", 1); Give("Hatchet", 1);
            Assert.AreEqual("missing_materials", Repair("Lasting"));
            Assert.AreEqual(3, Carried("SalvagedTimber")); Assert.AreEqual(1, Carried("KnotflaxCord"), "a short repair takes nothing");
            Give("KnotflaxCord", 1);
            player.GetPart<InventoryPart>().RemoveObject(player.GetPart<InventoryPart>().Objects.First(o => o.BlueprintName == "Hatchet"));
            Assert.AreEqual("missing_tool", Repair("Lasting"));
            Give("Hatchet", 1);
            Assert.IsNull(Repair("Lasting"));
            Assert.AreEqual(RepairStage.StableRepair, Weir.Stage); Assert.AreEqual(RepairMethodId.ManualRepair, Weir.ResolvedByMethod);
            Assert.AreEqual(0, Carried("SalvagedTimber")); Assert.AreEqual(0, Carried("KnotflaxCord"));
            Assert.AreEqual(1, Carried("Hatchet"), "the tool is kept");
            Assert.AreEqual("already_repaired", Repair("Lasting"));
        }

        [Test] public void APatchHoldsThenRelapsesWithItsOwnMessage()
        {
            Give("KnotflaxCord", 1);
            Assert.IsNull(Repair("Patch"));
            Assert.AreEqual(RepairStage.TemporarilyPurified, Weir.Stage); Assert.AreEqual(RepairMethodId.PatchedRepair, Weir.ResolvedByMethod);
            Assert.AreEqual(5 + 50, Weir.RelapseAtTurn);
            Assert.False(SettlementManager.Current.AdvanceSettlement(TineZone, 54));
            Assert.True(SettlementManager.Current.AdvanceSettlement(TineZone, 55));
            Assert.AreEqual(RepairStage.Fouled, Weir.Stage);
            CollectionAssert.Contains(SettlementManager.Current.ConsumePendingMessages(TineZone), "The weir has gapped again while you were away.");
        }

        [Test] public void TheConcordContractsADataSiteToo()
        {
            Assert.IsNull(ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Tine:Weir:40"));
            Assert.AreEqual(RepairStage.StableRepair, Weir.Stage); Assert.AreEqual(60, TradeSystem.GetDrams(player));
        }

        [TestCase("Tine:Weir")] [TestCase("Tine:Weir:Forever")] [TestCase("Tine:NoSuchSite:Lasting")] [TestCase("Atlantis:Weir:Lasting")] [TestCase(null)]
        public void MalformedRepairsTakeNothing(string arg)
        {
            Give("SalvagedTimber", 3); Give("KnotflaxCord", 2); Give("Hatchet", 1);
            Assert.NotNull(ConversationActions.ExecuteRequired("RepairPlaceSite", keeper, player, arg));
            Assert.AreEqual(3, Carried("SalvagedTimber")); Assert.AreEqual(RepairStage.Fouled, Weir.Stage);
        }

        // ════════════════ the concern's Restore approach ════════════════

        [Test] public void ARestoreApproachOffersTheRepairAndFinishesOnIt()
        {
            Assert.IsNull(ConcernConversation.Accept(keeper, player, "probe.weir"));
            var status = ConcernConversation.BuildNode("status|probe.weir", keeper, player);
            StringAssert.Contains("3 salvaged timber", status.Text); StringAssert.Contains("hatchet", status.Text);
            Assert.False(status.Choices.Any(c => c.Text.Contains("Repair the fishing weir")), "nothing to repair with yet");
            Give("SalvagedTimber", 3); Give("KnotflaxCord", 2); Give("Hatchet", 1);
            status = ConcernConversation.BuildNode("status|probe.weir", keeper, player);
            var repair = status.Choices.First(c => c.Text.Contains("Repair the fishing weir"));
            Assert.True(ConversationActions.TryExecuteAll(repair.Actions, keeper, player));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("probe.weir")));
        }

        [Test] public void APatchDoesNotFinishTheRestoreApproach()
        {
            Assert.IsNull(ConcernConversation.Accept(keeper, player, "probe.weir"));
            Give("KnotflaxCord", 1);
            Assert.IsNull(Repair("Patch"));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("probe.weir")), "a patch is not a repair");
        }

        [Test] public void LintChecksSitesAndRestoreApproaches()
        {
            Assert.IsEmpty(PlaceSiteCatalog.Lint(factory));
            Assert.IsEmpty(ConcernRegistry.Lint(ConcernRegistry.Get("probe.weir"), factory));
            var c = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(ConcernRegistry.Get("probe.weir")));
            c.Approaches[0].Restore.Site = "NoSuchSite";
            StringAssert.Contains("unknown site", string.Join("|", ConcernRegistry.Lint(c, factory)));
            PlaceSiteCatalog.LoadFromJson(Sites.Replace("SalvagedTimber", "NoSuchTimber").Replace("\"Tine\"", "\"Atlantis\""));
            var lines = string.Join("|", PlaceSiteCatalog.Lint(factory));
            StringAssert.Contains("NoSuchTimber", lines); StringAssert.Contains("Atlantis", lines);
        }
    }
}
