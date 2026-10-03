using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveAdversarialTests : ConnectedReserveTestBase
    {
        [Test] public void PaidCommandRollbackRestoresPermissionAndBothPurses()
        {
            Player.AddPart(new ConnectedReserveThrowAfterAction()); Assert.False(Act("pay"));
            Assert.AreEqual("Unknown", State()); Assert.AreEqual(40, TradeSystem.GetDrams(Player)); Assert.AreEqual(10, TradeSystem.GetDrams(Keeper));
        }
        [Test] public void HarvestRollbackRestoresCropWithoutRecordingBreach()
        {
            var crop = Crop(); Player.AddPart(new ConnectedReserveThrowAfterAction());
            Assert.False(Harvest(crop).Success); Assert.NotNull(Reserve.GetEntityCell(crop)); Assert.AreEqual("Unknown", State());
        }
        [Test] public void ImmatureCropFailureDoesNotAccuseAnyone()
        { var crop = Crop(); crop.GetPart<CropPart>().GrowthStage = 0; Assert.False(Harvest(crop).Success); Assert.AreEqual("Unknown", State()); }
        [Test] public void PublicCropBesideReserveDoesNotBecomeClaimedByBlueprint()
        { Assert.True(Harvest(Crop(5, 7)).Success); Assert.AreEqual("Unknown", State()); }
        [Test] public void ReplacementSoilDoesNotInheritOldClaimByCoordinates()
        { Reserve.RemoveEntity(SoilA); Soil(Reserve, 4, 5); Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Unknown", State()); }
        [Test] public void DeadKeeperCannotWitnessOrSellPermission()
        { Keeper.GetStat("Hitpoints").BaseValue = 0; Assert.False(Act("pay")); Assert.True(Harvest(Crop()).Success); Assert.AreEqual("Unknown", State()); }
        [Test] public void HostilityIsNotErasedByPayment()
        { Keeper.GetPart<BrainPart>().SetPersonallyHostile(Player, false); Assert.False(Act("pay")); Assert.AreEqual(40, TradeSystem.GetDrams(Player)); }
        [Test] public void ForeignPlayerCannotUseAnotherPlayersPermission()
        { Assert.True(Act("pay")); var other = Person("Player", Reserve, 5, 7, true); Assert.AreEqual("Unknown", State(other)); }
        [Test] public void RepeatedIntroductionCannotUndoWitnessedBreach()
        {
            RepairPan(); Assert.True(Introduce()); MovePlayer(Reserve);
            // Earned testimony is historical; it cannot erase a later breach
            // merely because the player delayed reporting the introduction.
            Assert.True(Harvest(Crop()).Success);
            Assert.AreEqual("Suspended", State()); Assert.False(Act("introduction"));
        }
        [Test] public void MissingRestitutionGrainDoesNotClearBreach()
        { Assert.True(Harvest(Crop()).Success); Give("Emberwheat"); Assert.False(Act("reconcile")); Assert.AreEqual("Suspended", State()); Assert.AreEqual(1, CarriedCount("Emberwheat")); }
        [Test] public void RestitutionRollbackRestoresExactGrainAndSuspension()
        { Assert.True(Harvest(Crop()).Success); Give("Emberwheat", 2); Player.AddPart(new ConnectedReserveThrowAfterAction()); Assert.False(Act("reconcile")); Assert.AreEqual("Suspended", State()); Assert.AreEqual(2, CarriedCount("Emberwheat")); }
        [Test] public void FullPackRefusesTrayTakeWithoutBreach()
        {
            var item = Factory.CreateEntity("FireClay"); Assert.True(Tray.GetPart<ContainerPart>().AddItem(item)); Player.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(Tray, item), Player, Reserve).Success);
            Assert.True(Tray.GetPart<ContainerPart>().Contents.Contains(item)); Assert.AreEqual("Unknown", State());
        }
        [Test] public void ForeignContainerTakeDoesNotAccusePlayer()
        {
            var box = Factory.CreateEntity("ConnectedReserveTray"); Assert.True(Reserve.AddEntity(box, 6, 6));
            var item = Factory.CreateEntity("FireClay"); Assert.True(box.GetPart<ContainerPart>().AddItem(item));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(box, item), Player, Reserve).Success); Assert.AreEqual("Unknown", State());
        }
        [Test] public void CookOutsideSightCannotAcknowledgeRemoteRepair()
        { RepairPan(); Kitchen.MoveEntity(Cook, 25, 5); Assert.False(Introduce()); }
        [Test] public void DeadCookCannotSupplyIntroduction()
        { RepairPan(); Cook.GetStat("Hitpoints").BaseValue = 0; Assert.False(Introduce()); }
        [Test] public void FailedRepairDoesNotLeaveTestimonyOrCommittedCause()
        {
            MovePlayer(Kitchen); Give("FireClay", 2); Player.AddPart(new ConnectedReserveThrowAfterAction());
            Assert.False(Pan.GetPart<RepairablePart>().TryRepair(Player, Kitchen)); Assert.False(Pan.GetPart<RepairablePart>().Repaired); Assert.False(Introduce());
        }
        [Test] public void RelayBeforeAnyIntroductionCannotForgeKnowledge()
        { Assert.False(Act("introduction")); Assert.AreEqual("Unknown", State()); }
        [Test] public void CurrencyOverflowRefusesWithoutPermissionOrPlayerDebit()
        { TradeSystem.SetDrams(Keeper, int.MaxValue); Assert.False(Act("pay")); Assert.AreEqual("Unknown", State()); Assert.AreEqual(40, TradeSystem.GetDrams(Player)); }
        [Test] public void TermsAreReadOnlyAndCannotGrantAccess()
        { Assert.True(Act("terms")); Assert.AreEqual("Unknown", State()); Assert.AreEqual(40, TradeSystem.GetDrams(Player)); }
        [Test] public void RemovedKeeperPartCannotDispatchReplacementAuthority()
        { Keeper.RemovePart(Claim); Assert.False(Act("pay")); Assert.AreEqual(40, TradeSystem.GetDrams(Player)); }
    }
}
