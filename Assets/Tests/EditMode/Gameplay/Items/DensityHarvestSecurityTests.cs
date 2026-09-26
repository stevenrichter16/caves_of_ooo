using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class DensityHarvestSecurityTests
    {
        DensityLootTestScope scope;
        EntityFactory oldFactory;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); oldFactory = HarvestablePart.Factory; HarvestablePart.Factory = Factory; }
        [TearDown] public void Cleanup() { HarvestablePart.Factory = oldFactory; HarvestCreationProbe.Callback = null; scope.Dispose(); }
        Entity Actor(Zone zone = null, int x = 10) { var actor = Factory.CreateEntity("Player"); if (zone != null) Assert.True(zone.AddEntity(actor, x, 10)); return actor; }
        Entity Source(Entity actor = null, Zone zone = null, int x = 11)
        {
            var source = Factory.CreateEntity("CreatureCorpse"); source.AddPart(new HarvestablePart { YieldBlueprint = "RawMeat", YieldMin = 1, YieldMax = 1 });
            if (actor != null) Assert.True(actor.GetPart<InventoryPart>().AddObject(source));
            if (zone != null) Assert.True(zone.AddEntity(source, x, 10)); return source;
        }
        internal static bool Act(Entity source, Entity actor, Zone zone = null)
        {
            var e = GameEvent.New("InventoryAction"); try { e.SetParameter("Command", "Harvest"); e.SetParameter("Actor", actor); if (zone != null) e.SetParameter("Zone", zone); source.FireEvent(e); return e.Handled; } finally { e.Release(); }
        }
        static int Units(IEnumerable<Entity> entities) => entities.Where(e => e.BlueprintName == "RawMeat").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        static int Packed(Entity actor) => Units(actor.GetPart<InventoryPart>().Objects);
        [Test] public void CarriedHarvestIsSingleUseAndSpentTargetStopsAdvertising()
        {
            var actor = Actor(); var source = Source(actor); Assert.True(Act(source, actor)); Assert.AreEqual(1, Packed(actor));
            Assert.False(Act(source, actor)); Assert.AreEqual(1, Packed(actor));
            Assert.False(WorldInteractionSystem.GatherActions(source, actor).Any(a => a.Command == "Harvest"));
        }
        [Test] public void ForeignCarriedSourceCannotYieldOrBeSpent()
        {
            var actor = Actor(); var owner = Actor(); var source = Source(owner); Assert.False(Act(source, actor)); Assert.AreEqual(0, Packed(actor));
            Assert.That(owner.GetPart<InventoryPart>().Objects, Does.Contain(source)); Assert.True(Act(source, owner)); Assert.AreEqual(1, Packed(owner));
        }
        [TestCase("distant")] [TestCase("detached")] [TestCase("foreign-zone")] [TestCase("dead")]
        public void WorldHarvestRequiresALivingPresentActorWithinReach(string condition)
        {
            var zone = new Zone("harvest"); var actor = Actor(zone); var source = Source(zone: zone);
            if (condition == "distant") { zone.RemoveEntity(actor); zone.AddEntity(actor, 30, 10); }
            if (condition == "detached") zone.RemoveEntity(actor);
            if (condition == "foreign-zone") { zone.RemoveEntity(actor); new Zone("foreign").AddEntity(actor, 10, 10); }
            if (condition == "dead") actor.Statistics["Hitpoints"].BaseValue = 0;
            Assert.False(Act(source, actor, zone)); Assert.AreEqual(0, Packed(actor)); Assert.NotNull(zone.GetEntityCell(source));
            var control = Actor(zone, 12); Assert.True(Act(source, control, zone)); Assert.AreEqual(1, Packed(control));
        }
        [Test] public void MissingActorRefusesWithoutSpending()
        { var actor = Actor(); var source = Source(actor); Assert.False(Act(source, null)); Assert.That(actor.GetPart<InventoryPart>().Objects, Does.Contain(source)); Assert.True(Act(source, actor)); }
        [TestCase(-1, 1, 100)] [TestCase(2, 1, 100)] [TestCase(1, 65, 100)] [TestCase(1, 1, -1)] [TestCase(1, 1, 101)]
        public void MalformedYieldBoundsRefuseWithoutChangingSource(int min, int max, int chance)
        {
            var actor = Actor(); var source = Source(actor); var p = source.GetPart<HarvestablePart>(); p.YieldMin = min; p.YieldMax = max; p.YieldChance = chance;
            Assert.False(Act(source, actor)); Assert.AreEqual(0, Packed(actor)); Assert.That(actor.GetPart<InventoryPart>().Objects, Does.Contain(source));
        }
        [TestCase("no-factory")] [TestCase("unknown-yield")]
        public void MissingYieldInfrastructureRefusesWithoutLosingTheSource(string condition)
        {
            var actor = Actor(); var source = Source(actor);
            if (condition == "no-factory") HarvestablePart.Factory = null; else source.GetPart<HarvestablePart>().YieldBlueprint = "NoSuchHarvestProduct";
            Assert.False(Act(source, actor)); Assert.That(actor.GetPart<InventoryPart>().Objects, Does.Contain(source)); Assert.AreEqual(0, Packed(actor));
        }
        [Test] public void ZeroChanceStillSpendsExactlyOnce()
        {
            var actor = Actor(); var source = Source(actor); source.GetPart<HarvestablePart>().YieldChance = 0;
            Assert.True(Act(source, actor)); Assert.AreEqual(0, Packed(actor)); Assert.False(Act(source, actor));
        }
        [Test] public void CapacityOverflowKeepsEveryProductOnTheSourceCell()
        {
            var zone = new Zone("overflow"); var actor = Actor(zone); actor.GetPart<InventoryPart>().MaxWeight = 0; var source = Source(zone: zone); source.GetPart<HarvestablePart>().YieldMin = source.GetPart<HarvestablePart>().YieldMax = 2;
            var neighbor = Factory.CreateEntity("Bone"); zone.AddEntity(neighbor, 11, 10);
            Assert.True(Act(source, actor, zone)); Assert.AreEqual(0, Packed(actor)); Assert.AreEqual(2, Units(zone.GetAllEntities())); Assert.IsNull(zone.GetEntityCell(source)); Assert.NotNull(zone.GetEntityCell(neighbor));
        }
        [Test] public void NoOverflowDestinationRefusesAtomically()
        {
            var actor = Actor(); var source = Source(actor); source.GetPart<PhysicsPart>().Weight = 0; actor.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(Act(source, actor)); Assert.AreEqual(0, Packed(actor)); Assert.That(actor.GetPart<InventoryPart>().Objects, Does.Contain(source));
        }
        [Test] public void ProductInitializationCannotReenterTheSameHarvest()
        {
            var actor = Actor(); var source = Source(actor); Factory.RegisterPartType<HarvestCreationProbe>(); Factory.Blueprints["RawMeat"].Parts["HarvestCreationProbe"] = new Dictionary<string,string>();
            bool? nested = null; HarvestCreationProbe.Callback = () => nested = Act(source, actor);
            Assert.True(Act(source, actor)); Assert.AreEqual(false, nested); Assert.AreEqual(1, Packed(actor));
        }
        [Test] public void ProductInitializationMovingSourceAwayRejectsTheOuterHarvest()
        {
            var actor = Actor(); var other = Actor(); var source = Source(actor); Factory.RegisterPartType<HarvestCreationProbe>(); Factory.Blueprints["RawMeat"].Parts["HarvestCreationProbe"] = new Dictionary<string,string>();
            HarvestCreationProbe.Callback = () => { actor.GetPart<InventoryPart>().RemoveObject(source); other.GetPart<InventoryPart>().AddObject(source); };
            Assert.False(Act(source, actor)); Assert.AreEqual(0, Packed(actor)); Assert.That(other.GetPart<InventoryPart>().Objects, Does.Contain(source));
            Assert.True(Act(source, other)); Assert.AreEqual(1, Packed(other));
        }
        [TestCase(false)] [TestCase(true)]
        public void OuterCommandRollbackRestoresSourceAndAllYield(bool world)
        {
            var zone = new Zone("rollback"); var actor = Actor(zone); var source = world ? Source(zone: zone) : Source(actor); actor.AddPart(new ThrowAfterHarvest());
            var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source, "Harvest"), actor, zone);
            Assert.False(result.Success); Assert.AreEqual(0, Packed(actor)); Assert.AreEqual(0, Units(zone.GetAllEntities()));
            if (world) Assert.NotNull(zone.GetEntityCell(source)); else Assert.That(actor.GetPart<InventoryPart>().Objects, Does.Contain(source));
            actor.RemovePart(actor.GetPart<ThrowAfterHarvest>()); Assert.True(Act(source, actor, zone)); Assert.AreEqual(1, Packed(actor));
        }
        public class HarvestCreationProbe : Part
        {
            public static Action Callback; public override string Name => "HarvestCreationProbe";
            public override void Initialize() { var callback = Callback; Callback = null; callback?.Invoke(); }
        }
        public class ThrowAfterHarvest : Part
        { public override string Name => "ThrowAfterHarvest"; public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("harvest post-action failure"); return true; } }
    }
}
