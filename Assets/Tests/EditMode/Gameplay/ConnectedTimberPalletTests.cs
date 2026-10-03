using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Real blueprint and ordinary inventory command coverage. Actor/source
    /// placement is fixture setup; actual travel and paid input are separate evidence.</summary>
    public sealed class ConnectedTimberPalletTests
    {
        DensityLootTestScope scope;
        EntityFactory oldFactory;
        Zone zone;
        Entity actor;
        const string Pallet = "GleanersTimberPallet";
        const string Product = "SalvagedTimber";
        const string ActionLabel = "dismantle for timber (2)";
        EntityFactory Factory => scope.Factory;
        InventoryPart Inventory => actor.GetPart<InventoryPart>();

        [SetUp] public void SetUp()
        {
            scope = new DensityLootTestScope();
            oldFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = Factory;
            zone = new Zone("Overworld.11.10.1");
            actor = Factory.CreateEntity("Player");
            actor.Statistics["Strength"].BaseValue = 12;
            Assert.True(zone.AddEntity(actor, 10, 10));
        }
        [TearDown] public void TearDown()
        { HarvestablePart.Factory = oldFactory; scope?.Dispose(); }

        Entity Place(string blueprint = Pallet, int x = 11)
        {
            Assert.True(Factory.Blueprints.ContainsKey(blueprint), "Required authored finite source: " + blueprint);
            var owner = Factory.CreateEntity(blueprint);
            Assert.NotNull(owner);
            Assert.True(zone.AddEntity(owner, x, 10));
            return owner;
        }
        bool Harvest(Entity owner) => InventorySystem.ExecuteCommand(
            new PerformInventoryActionCommand(owner, "Harvest"), actor, zone).Success;
        static int Units(IEnumerable<Entity> owners) => owners.Where(e => e.BlueprintName == Product)
            .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        int Packed => Units(Inventory.Objects);
        int Ground => Units(zone.GetReadOnlyEntities());

        [Test]
        public void RealPalletOffersOneExplicitDismantleAlongsideHaulingAndExistingHarvestStaysNamed()
        {
            var control = Place("RepairTimberPile", 12);
            var oldAction = WorldInteractionSystem.GatherActions(control, actor).Single(a => a.Command == "Harvest");
            Assert.AreEqual("harvest", oldAction.Display);
            var pallet = Place();
            var physics = pallet.GetPart<PhysicsPart>();
            var handling = pallet.GetPart<HandlingPart>();
            var harvest = pallet.GetPart<HarvestablePart>();
            Assert.NotNull(physics); Assert.NotNull(handling); Assert.NotNull(harvest);
            Assert.True(physics.Solid); Assert.False(physics.Takeable);
            Assert.False(handling.Carryable); Assert.False(handling.Throwable);
            Assert.AreEqual(90, physics.Weight); Assert.AreEqual(90, handling.Weight);
            Assert.AreEqual("Heavy", handling.BulkClass);
            Assert.Null(pallet.GetPart<DestructiblePart>(), "Dismantling is the one authored consumption/yield path.");
            Assert.AreEqual(Product, harvest.YieldBlueprint);
            Assert.AreEqual(2, harvest.YieldMin); Assert.AreEqual(2, harvest.YieldMax);
            Assert.AreEqual(100, harvest.YieldChance); Assert.False(harvest.Harvested);
            var actions = WorldInteractionSystem.GatherActions(pallet, actor);
            var action = actions.Single(a => a.Command == "Harvest");
            Assert.AreEqual("Harvest", action.Name); Assert.AreEqual(ActionLabel, action.Display);
            Assert.AreEqual('h', action.Key); Assert.False(action.FireOnActor);
            Assert.True(actions.Any(a => a.Command == HandlingPart.HaulCommand));
        }

        [TestCase(11, false)] [TestCase(12, true)]
        public void HaulingHasAnHonestStrengthBoundaryButDismantlingDoesNot(int strength, bool canHaul)
        {
            actor.Statistics["Strength"].BaseValue = strength;
            var pallet = Place();
            Assert.AreEqual(canHaul, DragRules.CanDrag(actor, pallet) == DragVerdict.Ok);
            Assert.AreEqual(canHaul, WorldInteractionSystem.GatherActions(pallet, actor)
                .Any(a => a.Command == HandlingPart.HaulCommand));
            Assert.True(Harvest(pallet)); Assert.AreEqual(2, Packed);
            Assert.Null(zone.GetEntityCell(pallet)); Assert.True(pallet.GetPart<HarvestablePart>().Harvested);
            Assert.False(Harvest(pallet)); Assert.AreEqual(2, Packed);
            Assert.False(WorldInteractionSystem.GatherActions(pallet, actor).Any(a => a.Command == "Harvest"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EveryTimberUnitIsPackedOrLeftAtTheConsumedPalletCell(int freeWeight)
        {
            var pallet = Place();
            var unrelated = Factory.CreateEntity("Bone"); Assert.True(zone.AddEntity(unrelated, 11, 10));
            int initial = Packed;
            Inventory.MaxWeight = Inventory.GetCarriedWeight() + freeWeight;
            Assert.True(Harvest(pallet));
            Assert.AreEqual(initial + freeWeight, Packed); Assert.AreEqual(2 - freeWeight, Ground);
            Assert.AreEqual(2, Packed - initial + Ground);
            Assert.Null(zone.GetEntityCell(pallet));
            Assert.AreSame(zone.GetCell(11, 10), zone.GetEntityCell(unrelated));
            Assert.That(zone.GetCell(11, 10).Objects, Does.Contain(unrelated));
            foreach (var timber in zone.GetReadOnlyEntities().Where(e => e.BlueprintName == Product))
                Assert.AreEqual((11, 10), zone.GetEntityPosition(timber));
            Assert.False(Harvest(pallet)); Assert.AreEqual(2, Packed - initial + Ground);
        }

        [TestCase(false)] [TestCase(true)]
        public void DismantlingHeldPalletReleasesGripAndNeverPullsConsumedOwnerBack(bool held)
        {
            var pallet = Place();
            int speed = actor.GetStatValue("Speed");
            if (held)
            {
                Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, pallet, zone));
                Assert.Less(actor.GetStatValue("Speed"), speed);
                Assert.AreSame(pallet, DragSystem.GetDragged(actor));
            }
            Assert.True(Harvest(pallet));
            Assert.Null(actor.GetPart<DragPart>()); Assert.Null(pallet.GetPart<DraggedPart>());
            Assert.AreEqual(speed, actor.GetStatValue("Speed"));
            Assert.True(MovementSystem.TryMoveTo(actor, zone, 9, 10));
            Assert.Null(zone.GetEntityCell(pallet)); Assert.AreEqual(2, Packed);
            Assert.AreEqual(speed, actor.GetStatValue("Speed"));
        }

        [Test]
        public void ConsumedSourceAndExactGroundTimberStayConsumedAcrossASessionReplacement()
        {
            var pallet = Place(); string id = pallet.ID;
            Inventory.MaxWeight = 0;
            Assert.True(Harvest(pallet)); Assert.AreEqual(2, Ground);
            var timberIds = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == Product)
                .Select(e => e.ID).ToArray();
            var manager = new OverworldZoneManager(null, 729490642);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager();
            turns.RestoreSavedState(17, true, actor, new List<TurnManager.SavedTurnEntry>
                { new TurnManager.SavedTurnEntry { Entity = actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("pallet-save", "pallet", manager, turns, actor));
            var restored = loaded.ZoneManager.ActiveZone;
            Assert.AreNotSame(zone, restored);
            Assert.False(restored.GetReadOnlyEntities().Any(e => e.ID == id));
            Assert.AreEqual(2, Units(restored.GetReadOnlyEntities()));
            CollectionAssert.AreEquivalent(timberIds, restored.GetReadOnlyEntities()
                .Where(e => e.BlueprintName == Product).Select(e => e.ID));
            foreach (var timber in restored.GetReadOnlyEntities().Where(e => e.BlueprintName == Product))
                Assert.AreEqual((11, 10), restored.GetEntityPosition(timber));
            var spent = PartRoundTripHelper.RoundTripEntity(pallet);
            Assert.True(restored.AddEntity(spent, 11, 10));
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(spent, "Harvest"),
                loaded.Player, restored).Success, "Explicit stale-reference probe cannot mint new output.");
            Assert.AreEqual(2, Units(restored.GetReadOnlyEntities()));
        }
    }
}
