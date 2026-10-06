using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondPreparationSourceTests
    {
        [TestCase("sodden", "FilterHood")]
        [TestCase("sodden", "AcidworkerApron")]
        [TestCase("cinderhold", "BracedHaftComponent")]
        [TestCase("counter", "GuardLashingComponent")]
        [TestCase("counter", "ColdwardCloak")]
        public void RealRegionalSourceHasOneAcquirableOwnerAndDepletionSurvivesSave(string source, string blueprint)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64);
                OverworldZoneManager coldManager = null; Zone zone; Entity owner; Func<bool> replay;
                if (source == "sodden")
                {
                    // The garments belong to the versioned post-acceptance installer, not the base district builder.
                    coldManager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                    zone = coldManager.GetZone(SoddenDistrictPlan.WorksZoneID);
                    Assert.NotNull(zone, "The actual current cold pipeline must accept the works.");
                    owner = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SoddenWorksLocker");
                    var originalZone = zone;
                    replay = () => ReferenceEquals(originalZone, coldManager.GetZone(originalZone.ZoneID));
                }
                else owner = EquipmentDiscoverySourceTests.Build(source, scope.Factory, 64, out zone, out replay);
                var rows = EquipmentDiscoverySourceTests.Items(owner).Where(e => e.BlueprintName == blueprint).ToArray();
                Assert.AreEqual(1, rows.Length, source + " must expose its finite preparation choice"); var item = rows[0];
                Assert.AreEqual(1, item.GetPart<StackerPart>()?.StackCount ?? 1); Assert.AreSame(owner, item.GetPart<PhysicsPart>().InInventory);
                var actor = scope.Factory.CreateEntity("Player"); actor.GetPart<InventoryPart>().MaxWeight = 10000;
                var at = zone.GetEntityPosition(owner); Assert.True(zone.AddEntity(actor, at.x + 1, at.y));
                if (source == "cinderhold")
                {
                    TradeSystem.SetDrams(actor, 10000); int purse = TradeSystem.GetDrams(actor);
                    Assert.True(TradeSystem.BuyFromTrader(actor, owner, item)); Assert.Less(TradeSystem.GetDrams(actor), purse);
                }
                else
                {
                    owner.GetPart<ContainerPart>().Locked = false;
                    Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(owner, item), actor, zone).Success);
                }
                Assert.True(actor.GetPart<InventoryPart>().Contains(item));
                // A depleted profile may correctly refuse replay. Its stock must remain spent.
                if (coldManager != null) Assert.True(replay(), "Revisit must preserve the original admitted graph.");
                else replay();
                Assert.False(EquipmentDiscoverySourceTests.Items(owner).Any(e => e.BlueprintName == blueprint), "A rebuilt profile must not restock an acquired discovery.");
                var manager = coldManager ?? new OverworldZoneManager(null, 64);
                if (coldManager == null)
                    manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
                else manager.SetActiveZone(zone);
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("preparation-source", "audit", manager, null, actor));
                var savedOwner = loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e => e.ID == owner.ID);
                Assert.False(EquipmentDiscoverySourceTests.Items(savedOwner).Any(e => e.BlueprintName == blueprint));
                Assert.AreEqual(1, loaded.Player.GetPart<InventoryPart>().Objects.Count(e => e.ID == item.ID));
            }
        }
        [Test] public void BracedHaftIsNotRenewedByNormalTraderRestocking()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var owner = EquipmentDiscoverySourceTests.Build("cinderhold", scope.Factory, 64, out var zone, out _);
                Assert.AreEqual(1, EquipmentDiscoverySourceTests.Items(owner).Count(e => e.BlueprintName == "BracedHaftComponent"));
                foreach (var item in owner.GetPart<InventoryPart>().Objects.ToArray()) owner.GetPart<InventoryPart>().RemoveObject(item);
                var old = TraderRestockSystem.Factory;
                try { TraderRestockSystem.Factory = scope.Factory; TraderRestockSystem.RestockZone(zone, TraderRestockSystem.RestockIntervalTurns + 1); }
                finally { TraderRestockSystem.Factory = old; }
                Assert.IsNotEmpty(owner.GetPart<InventoryPart>().Objects); Assert.False(EquipmentDiscoverySourceTests.Items(owner).Any(e => e.BlueprintName == "BracedHaftComponent"));
            }
        }
    }
}
