using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditPositivePaymentBenchTests
    {
        EntityFactory _seed, _conversation; NarrativeStatePart _state; Zone _zone;
        static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditPositivePaymentBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Positive payment audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup()
        {
            _seed = SeedPart.Factory; _conversation = ConversationActions.Factory; _state = NarrativeStatePart.Current; _zone = SettlementRuntime.ActiveZone;
            NarrativeStatePart.Current = new NarrativeStatePart(); PlayerReputation.Reset(); FactionManager.Initialize();
            ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            foreach (string name in new[] { "FriendlyNPCs", "FoundingVillage" })
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Conversations/" + name + ".json")), name);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            SeedPart.Factory = _seed; ConversationActions.Factory = _conversation; NarrativeStatePart.Current = _state; SettlementRuntime.ActiveZone = _zone;
            PlayerReputation.Reset(); FactionManager.Reset(); Diag.ResetAll();
        }
        [Test] public void ArenaStagesActualItemsAndBothAuthoredConversations()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); var bench = Bench(); bench.Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            CollectionAssert.AreEqual(new[] { "CandyCarrotSeed", "PaleSalt", "Tepuibone" }, inv.Objects.Select(i => i.BlueprintName));
            CollectionAssert.AreEqual(new[] { 2, 1, 2 }, inv.Objects.Select(i => i.GetPart<StackerPart>().StackCount));
            Assert.AreEqual(0, inv.GetAllEquipped().Count); Assert.AreEqual(0, (int)bench.GetType().GetProperty("Cases").GetValue(bench));
            var salt = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "SaltMaster");
            var tender = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "FoundingPlaqueTender");
            Assert.AreEqual((21, 13), ctx.Zone.GetEntityPosition(salt)); Assert.AreEqual((19, 13), ctx.Zone.GetEntityPosition(tender));
            Assert.AreEqual("SaltMaster_1", salt.GetPart<ConversationPart>().ConversationID); Assert.AreEqual("FoundingPlaqueTender_1", tender.GetPart<ConversationPart>().ConversationID);
            Assert.AreEqual(0, PlayerReputation.Get("TentRight")); Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(FoundingTrustService.OfferedFact));
        }
        [Test] public void ArenaStartsOnRefusingFloorBesidePlantableControl()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            Assert.IsFalse(ctx.Zone.GetCell(20, 12).Objects.Any(e => e.HasTag("Plantable")));
            Assert.IsTrue(ctx.Zone.GetCell(20, 13).Objects.Any(e => e.HasTag("Plantable")));
            Assert.IsFalse(ctx.Zone.GetAllEntities().Any(e => e.GetPart<CropPart>() != null));
            Assert.AreSame(ctx.Factory, SeedPart.Factory); Assert.AreSame(ctx.Factory, ConversationActions.Factory); Assert.AreSame(ctx.Zone, SettlementRuntime.ActiveZone);
        }
        [TestCase("CandyCarrotCrop")] [TestCase("Grass")] [TestCase("SaltMaster")]
        public void MissingBlueprintRefusesBeforeAnyArenaMutation(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player");
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); var sentinel = h.Factory.CreateEntity("Torch"); Assert.IsTrue(inv.AddObject(sentinel)); var before = inv.Objects.ToArray();
            h.Factory.Blueprints.Remove(missing); Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx));
            Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); CollectionAssert.AreEqual(before, inv.Objects);
            Assert.AreSame(ctx.PlayerEntity, sentinel.GetPart<PhysicsPart>().InInventory); Assert.AreSame(_seed, SeedPart.Factory);
        }
        [Test] public void MissingConversationRefusesBeforeArenaMutation()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); ConversationLoader.Reset(); ConversationLoader.LoadFromJson("{\"Conversations\":[]}", "empty fixture");
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx)); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)] public void CheckPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench(); bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario")); Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "PositivePaymentNativeAudit" }).Count);
        }
    }
}
