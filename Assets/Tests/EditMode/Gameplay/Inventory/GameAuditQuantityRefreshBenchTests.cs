using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
using CavesOfOoo.Data;
namespace CavesOfOoo.Tests
{
    public class GameAuditQuantityRefreshBenchTests
    {
        private CavesOfOoo.Data.EntityFactory _oldSeed, _oldStill;
        private static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditQuantityRefreshBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Carry quantity audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup() { _oldSeed = SeedPart.Factory; _oldStill = AlchemyStillPart.Factory; }
        [TearDown] public void Cleanup() { SeedPart.Factory = _oldSeed; AlchemyStillPart.Factory = _oldStill; Diag.ResetAll(); EnhancementFactory.ForceReinitialize(); }
        [Test] public void ArenaUsesActualItemsWithExplicitSupportedHandlingConfiguration()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            CollectionAssert.AreEqual(new[] { "CandyCarrotSeed", "PaleSalt", "Dagger", "FireMoss", "Warhammer" }, inv.Objects.Select(i => i.BlueprintName));
            CollectionAssert.AreEqual(new[] { 3, 3, 3, 3, 1 }, inv.Objects.Select(i => i.GetPart<StackerPart>().StackCount));
            CollectionAssert.AreEqual(new[] { 4, 4, 4, 4, 0 }, inv.Objects.Select(i => i.GetPart<HandlingPart>().CarryMovePenalty));
            Assert.AreEqual(55, ctx.PlayerEntity.GetStat("Speed").Penalty); Assert.AreEqual(45, ctx.PlayerEntity.GetStatValue("Speed"));
            Assert.AreEqual(0, inv.GetAllEquipped().Count); Assert.IsFalse(inv.Objects.Any(i => i.Parts.OfType<IItemEnhancement>().Any()));
        }
        [Test] public void ArenaPlacesPlantableGroundAndAdjacentStillForActualCommands()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            Assert.IsTrue(ctx.Zone.GetCell(20, 12).Objects.Any(i => i.HasTag("Plantable")));
            Assert.IsFalse(ctx.Zone.GetCell(20, 12).HasObjectWithPart<CropPart>());
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(ctx.Zone.GetAllEntities().Single(i => i.BlueprintName == "AlchemyStill")));
            Assert.AreSame(ctx.Factory, SeedPart.Factory); Assert.AreSame(ctx.Factory, AlchemyStillPart.Factory);
        }
        [TestCase("CandyCarrotSeed")] [TestCase("Grass")] [TestCase("AlchemyStill")]
        public void MissingContentRefusesBeforeChangingArena(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player");
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); var sentinel = h.Factory.CreateEntity("Torch");
            Assert.IsTrue(inv.AddObject(sentinel)); var contents = inv.Objects.ToArray();
            ctx.PlayerEntity.GetStat("Speed").Penalty = 11;
            var speed = ctx.PlayerEntity.GetStat("Speed"); var hp = ctx.PlayerEntity.GetStat("Hitpoints"); int health = hp.BaseValue;
            var seedFactory = new EntityFactory(); var stillFactory = new EntityFactory(); SeedPart.Factory = seedFactory; AlchemyStillPart.Factory = stillFactory;
            h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx)); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            CollectionAssert.AreEqual(contents, inv.Objects); Assert.AreSame(ctx.PlayerEntity, sentinel.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(speed, ctx.PlayerEntity.GetStat("Speed")); Assert.AreEqual(11, speed.Penalty);
            Assert.AreSame(hp, ctx.PlayerEntity.GetStat("Hitpoints")); Assert.AreEqual(health, hp.BaseValue);
            Assert.AreSame(seedFactory, SeedPart.Factory); Assert.AreSame(stillFactory, AlchemyStillPart.Factory);
        }
        [TestCase(false)] [TestCase(true)] public void AuditCheckPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench();
            bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "QuantityRefreshNativeAudit" }).Count);
        }
    }
}
