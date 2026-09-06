using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditReforgeModsBenchTests
    {
        private EntityFactory _oldFactory;
        private static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditReforgeModsBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Reforge modification audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup() { _oldFactory = ForgePart.Factory; }
        [TearDown] public void Cleanup() { ForgePart.Factory = _oldFactory; Diag.ResetAll(); EnhancementFactory.ForceReinitialize(); }
        [Test] public void ArenaStartsWithActualUnspentComponentsAndPaidRecipeResources()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            CollectionAssert.AreEqual(new[] { "SteelBladeComponent", "OakHaftComponent", "LeatherBindingComponent", "PaleSalt", "Dagger" }, inv.Objects.Select(i => i.BlueprintName));
            CollectionAssert.AreEqual(new[] { 1, 2, 1, 1, 1 }, inv.Objects.Select(i => i.GetPart<StackerPart>().StackCount));
            Assert.IsFalse(inv.Objects.Any(i => i.HasPart<WeaponAssemblyPart>() || i.HasTag("ModSharp") || i.Parts.OfType<IItemEnhancement>().Any()));
            var bits = ctx.PlayerEntity.GetPart<BitLockerPart>(); Assert.AreEqual(2, bits.GetBitCount('B')); Assert.AreEqual(2, bits.GetBitCount('C'));
            Assert.IsTrue(bits.KnowsRecipe("mod_sharp_melee")); Assert.IsTrue(bits.KnowsRecipe("mod_palesalt_infuse"));
        }
        [Test] public void ArenaUsesActualAdjacentForgeAndPlayerControls()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(ctx.Zone.GetAllEntities().Single(i => i.BlueprintName == "TinkersForge")));
            Assert.IsTrue(ForgePart.IsNearForge(ctx.PlayerEntity, ctx.Zone)); Assert.AreSame(ctx.Factory, ForgePart.Factory);
        }
        [TestCase("TinkersForge")] [TestCase("ForgedWeapon")] [TestCase("SteelBladeComponent")]
        public void PreflightRefusesBeforeChangingInventoryPositionOrFactory(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player");
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); var item = h.Factory.CreateEntity("Torch"); Assert.IsTrue(inv.AddObject(item)); var contents = inv.Objects.ToArray();
            var factory = new EntityFactory(); ForgePart.Factory = factory; h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx)); CollectionAssert.AreEqual(contents, inv.Objects);
            Assert.AreSame(ctx.PlayerEntity, item.GetPart<PhysicsPart>().InInventory); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreSame(factory, ForgePart.Factory);
        }
        [TestCase(false)] [TestCase(true)] public void AuditObservationPreservesDiagnostics(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench(); bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario")); Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "ReforgeModsNativeAudit" }).Count);
        }
    }
}
