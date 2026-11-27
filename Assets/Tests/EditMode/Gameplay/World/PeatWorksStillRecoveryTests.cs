using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PeatWorksStillRecoveryTests
    {
        const string CaseBlueprint = "PeatWorksStillCase";
        const string Spike = "IronSpikeComponent";
        const string Cord = "KnotflaxCord";
        const string WorksId = SoddenDistrictPlan.WorksZoneID;
        HaulingContentScope scope;
        OverworldZoneManager manager;
        Zone works, previousActive;
        Entity player;
        InventoryPart Pack => player.GetPart<InventoryPart>();

        [SetUp]
        public void Setup()
        {
            previousActive = SettlementRuntime.ActiveZone;
            RepairRecipeRegistry.ResetForTests();
            scope = new HaulingContentScope();
            scope.Seed(64);
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, enableExploration: true);
        }

        [TearDown]
        public void Cleanup()
        {
            scope?.Dispose();
            RepairRecipeRegistry.ResetForTests();
            SettlementRuntime.ActiveZone = previousActive;
        }

        private Entity ApproachTheCase()
        {
            manager.SetActiveZone(WorksId);
            works = manager.ActiveZone;
            Assert.NotNull(works);
            SettlementRuntime.ActiveZone = works;
            player = scope.Factory.CreateEntity("Player");
            Assert.True(works.AddEntity(player, 63, 7));
            Pack.MaxWeight = 200;
            foreach (var item in Pack.Objects.Where(item => item.BlueprintName == Spike || item.BlueprintName == Cord).ToArray())
                Assert.True(Pack.RemoveObject(item));
            var cell = works.GetCell(63, 6);
            cell.IsVisible = cell.Explored = true;
            var cases = works.GetReadOnlyEntities().Where(entity => entity.BlueprintName == CaseBlueprint).ToArray();
            Assert.That(cases, Has.Length.EqualTo(1));
            Assert.AreSame(cell, works.GetEntityCell(cases[0]));
            return cases[0];
        }

        private Entity Carry(string blueprint)
        {
            var item = scope.Factory.CreateEntity(blueprint);
            Assert.NotNull(item);
            Assert.True(Pack.AddObject(item));
            return item;
        }

        private List<InventoryAction> Menu(Entity owner)
        {
            var cell = works.GetEntityCell(owner);
            return WorldInteractionSystem.GatherCellActions(owner, cell, player);
        }

        private static Entity StillIn(Entity container) => container.GetPart<ContainerPart>().Contents.Single();

        [Test]
        public void FreshWorksKeepsTheOldLockerAndOnePackedStillInASeparateCase()
        {
            var owner = ApproachTheCase();
            var oldLocker = works.GetCell(61, 6).Objects.Single(entity => entity.BlueprintName == "SoddenWorksLocker");
            string[] oldGoods = oldLocker.GetPart<ContainerPart>().Contents.SelectMany(item =>
                Enumerable.Repeat(item.BlueprintName, item.GetPart<StackerPart>()?.StackCount ?? 1)).ToArray();
            CollectionAssert.AreEquivalent(new[] { "LeatherBoots", "Buckler", Cord, Cord, "PeatMalletHeadComponent",
                "GroundwireScreen", "OakHaftComponent", "LeatherBindingComponent", "FilterHood", "AcidworkerApron" }, oldGoods);

            var still = StillIn(owner);
            Assert.AreEqual("CampStill", still.BlueprintName);
            Assert.AreEqual(18, still.GetPart<PhysicsPart>().Weight);
            Assert.AreSame(owner, still.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(works.GetEntityCell(still));
            Assert.True(PackableFixturePart.IsInertKit(still));
            Assert.AreEqual("field-case-catch", owner.GetPart<RepairablePart>().RecipeId);
            Assert.False(owner.GetPart<RepairablePart>().Repaired);
            Assert.AreSame(works, manager.GetZone(WorksId));
            Assert.AreSame(still, StillIn(owner));
        }

        [Test]
        public void JammedCaseRefusesOrdinaryOpenTakePutAndRepairWithoutPayment()
        {
            var owner = ApproachTheCase();
            var still = StillIn(owner);
            var other = Carry("FireClay");
            Assert.False(Menu(owner).Any(row => row.Command == "OpenContainer" && !row.IsDisabled));
            Assert.False(InventorySystem.TakeFromContainer(player, owner, still));
            Assert.AreEqual(0, InventorySystem.TakeAllFromContainer(player, owner));
            Assert.False(InventorySystem.PutInContainer(player, owner, other));
            Assert.False(InventorySystem.PerformAction(player, owner, RepairablePart.RepairCommand, works));
            Assert.False(owner.GetPart<RepairablePart>().Repaired);
            Assert.AreSame(still, StillIn(owner));
            Assert.AreSame(owner, still.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(player, other.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase(Spike)]
        [TestCase(Cord)]
        public void EitherPaidRepairReleasesTheSameStillOnceAndKeepsCustodyOnContinue(string material)
        {
            var owner = ApproachTheCase();
            var still = StillIn(owner);
            var payment = Carry(material);
            var row = Menu(owner).Single(action => !action.IsDisabled && action.Command == RepairablePart.RepairCommand);
            Assert.True(InventorySystem.PerformAction(player, owner, row.Command, works));
            Assert.False(Pack.Objects.Contains(payment));
            Assert.True(owner.GetPart<RepairablePart>().Repaired);
            Assert.True(Menu(owner).Any(action => action.Command == "OpenContainer" && !action.IsDisabled));
            Assert.AreSame(still, StillIn(owner));
            Assert.True(works.MoveEntity(player, 63, 8));
            Assert.False(InventorySystem.TakeFromContainer(player, owner, still));
            Assert.AreSame(still, StillIn(owner));
            Assert.True(works.MoveEntity(player, 63, 7));
            var duplicate = Carry("FireClay");
            duplicate.ID = still.ID;
            Assert.False(InventorySystem.TakeFromContainer(player, owner, still));
            Assert.AreSame(still, StillIn(owner));
            Assert.True(Pack.RemoveObject(duplicate));

            var imitation = scope.Factory.CreateEntity("CampStill");
            imitation.ID = still.ID;
            imitation.GetPart<PhysicsPart>().InInventory = owner;
            Assert.False(InventorySystem.TakeFromContainer(player, owner, imitation));
            Assert.AreSame(still, StillIn(owner));
            Assert.False(Pack.Objects.Contains(imitation));

            int capacity = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight() + 17;
            Assert.False(InventorySystem.TakeFromContainer(player, owner, still));
            Assert.AreSame(still, StillIn(owner));
            Assert.AreSame(owner, still.GetPart<PhysicsPart>().InInventory);
            Pack.MaxWeight = capacity;
            Assert.True(InventorySystem.TakeFromContainer(player, owner, still));
            Assert.AreSame(player, still.GetPart<PhysicsPart>().InInventory);
            Assert.IsEmpty(owner.GetPart<ContainerPart>().Contents);
            Assert.False(InventorySystem.PerformAction(player, owner, row.Command, works));
            Assert.False(InventorySystem.TakeFromContainer(player, owner, still));

            manager.UnloadZone(WorksId);
            Assert.AreSame(works, manager.GetZone(WorksId));
            using var bytes = new MemoryStream();
            GameSessionState.Capture("peat-works-still", "test", manager, null, player).Save(new SaveWriter(bytes));
            bytes.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(bytes, scope.Factory));
            var loadedCase = loaded.ZoneManager.GetZone(WorksId).GetReadOnlyEntities().Single(entity => entity.BlueprintName == CaseBlueprint);
            Assert.AreEqual(owner.ID, loadedCase.ID);
            Assert.True(loadedCase.GetPart<RepairablePart>().Repaired);
            Assert.IsEmpty(loadedCase.GetPart<ContainerPart>().Contents);
            var loadedStill = loaded.Player.GetPart<InventoryPart>().Objects.Single(item => item.BlueprintName == "CampStill");
            Assert.AreEqual(still.ID, loadedStill.ID);
            Assert.AreSame(loaded.Player, loadedStill.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void APreviouslyCachedWorksIsNeverGivenANewCase()
        {
            var old = new Zone(WorksId);
            var marker = scope.Factory.CreateEntity("SoddenRouteNotice");
            Assert.True(old.AddEntity(marker, 8, 11));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { WorksId, old } }, WorksId, null);
            Assert.AreSame(old, manager.GetZone(WorksId));
            CollectionAssert.AreEquivalent(new[] { marker }, old.GetReadOnlyEntities());
        }
    }
}
