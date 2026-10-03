using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Synthetic lifecycle injection over shipped Harvest/haul APIs.
    /// Uses an existing beam with the proposed finite yield, so RED does not
    /// depend on publishing the new blueprint or fail first on missing content.</summary>
    public sealed class ConnectedHarvestHaulRollbackTests
    {
        [TestCase(35, 7)] [TestCase(100, 7)]
        public void DirectTransactionRestoresOnlyTheAppliedHaulPenalty(int speedBase, int unrelatedPenalty)
        {
            using (var scope = new DensityLootTestScope())
            {
                var old = HarvestablePart.Factory; HarvestablePart.Factory = scope.Factory;
                try
                {
                    var zone = new Zone("pallet-penalty"); var actor = scope.Factory.CreateEntity("Player");
                    var source = scope.Factory.CreateEntity("FallenBeam");
                    source.AddPart(new HarvestablePart { YieldBlueprint = "SalvagedTimber", YieldMin = 2, YieldMax = 2 });
                    Assert.True(zone.AddEntity(actor, 10, 10)); Assert.True(zone.AddEntity(source, 11, 10));
                    actor.GetPart<InventoryPart>().MaxWeight = 0;
                    var speed = actor.GetStat("Speed"); speed.BaseValue = speedBase; speed.Penalty = unrelatedPenalty;
                    int original = speed.Value;
                    Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, source, zone));
                    var grip = actor.GetPart<DragPart>(); var held = source.GetPart<DraggedPart>();
                    int applied = grip.AppliedPenalty;
                    Assert.AreEqual(System.Math.Min(DragSystem.PenaltyFor(source), original - DragSystem.MinimumHaulingSpeed), applied);
                    var transaction = new InventoryTransaction(); var action = GameEvent.New("InventoryAction");
                    try
                    {
                        action.SetParameter("Command", "Harvest"); action.SetParameter("Actor", actor);
                        action.SetParameter("Zone", zone); action.SetParameter("InventoryTransaction", transaction);
                        source.FireEvent(action); Assert.True(action.Handled);
                        Assert.Null(zone.GetEntityCell(source)); Assert.Null(actor.GetPart<DragPart>());
                        Assert.AreEqual(original, speed.Value);
                    }
                    finally { action.Release(); transaction.Rollback(); }
                    Assert.AreSame(grip, actor.GetPart<DragPart>()); Assert.AreSame(held, source.GetPart<DraggedPart>());
                    Assert.AreEqual(applied, grip.AppliedPenalty); Assert.AreEqual(unrelatedPenalty + applied, speed.Penalty);
                    Assert.True(DragSystem.Release(actor)); Assert.AreEqual(original, speed.Value);
                    Assert.AreEqual(unrelatedPenalty, speed.Penalty);
                    Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "SalvagedTimber"));
                }
                finally { HarvestablePart.Factory = old; }
            }
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void OuterRollbackRestoresSourceYieldAndHaulingState(bool held, bool throwsAfterAction)
        {
            using (var scope = new DensityLootTestScope())
            {
                var old = HarvestablePart.Factory; HarvestablePart.Factory = scope.Factory;
                try
                {
                    var zone = new Zone("pallet-rollback"); var actor = scope.Factory.CreateEntity("Player");
                    var pallet = scope.Factory.CreateEntity("FallenBeam");
                    pallet.AddPart(new HarvestablePart { YieldBlueprint = "SalvagedTimber", YieldMin = 2, YieldMax = 2 });
                    Assert.True(zone.AddEntity(actor, 10, 10)); Assert.True(zone.AddEntity(pallet, 11, 10));
                    var inventory = actor.GetPart<InventoryPart>(); inventory.MaxWeight = 0;
                    int originalSpeed = actor.GetStatValue("Speed");
                    if (held) Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, pallet, zone));
                    var grip = actor.GetPart<DragPart>(); var inverse = pallet.GetPart<DraggedPart>();
                    int beforeSpeed = actor.GetStatValue("Speed"); int penalty = grip?.AppliedPenalty ?? 0;
                    if (held) Assert.Greater(penalty, 0);
                    if (throwsAfterAction)
                    {
                        actor.AddPart(new DensityHarvestSecurityTests.ThrowAfterHarvest());
                        Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(pallet, "Harvest"), actor, zone).Success);
                        actor.RemovePart(actor.GetPart<DensityHarvestSecurityTests.ThrowAfterHarvest>());
                    }
                    else
                    {
                        var tx = new InventoryTransaction();
                        try
                        {
                            Assert.True(new PerformInventoryActionCommand(pallet, "Harvest").Execute(new InventoryContext(actor, zone), tx).Success);
                            Assert.Null(zone.GetEntityCell(pallet));
                            Assert.AreEqual(2, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SalvagedTimber"));
                        }
                        finally { tx.Rollback(); }
                    }
                    Assert.AreEqual((11, 10), zone.GetEntityPosition(pallet));
                    Assert.False(pallet.GetPart<HarvestablePart>().Harvested);
                    Assert.False(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "SalvagedTimber"));
                    Assert.False(inventory.Objects.Any(e => e.BlueprintName == "SalvagedTimber"));
                    Assert.AreSame(grip, actor.GetPart<DragPart>(), "Rollback must restore the exact pre-command grip.");
                    Assert.AreSame(inverse, pallet.GetPart<DraggedPart>());
                    Assert.AreEqual(beforeSpeed, actor.GetStatValue("Speed"));
                    if (held)
                    {
                        Assert.AreEqual(penalty, grip.AppliedPenalty);
                        Assert.True(DragSystem.Release(actor));
                    }
                    Assert.AreEqual(originalSpeed, actor.GetStatValue("Speed"));
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(pallet, "Harvest"), actor, zone).Success);
                    Assert.AreEqual(2, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SalvagedTimber"));
                }
                finally { HarvestablePart.Factory = old; }
            }
        }
    }
}
