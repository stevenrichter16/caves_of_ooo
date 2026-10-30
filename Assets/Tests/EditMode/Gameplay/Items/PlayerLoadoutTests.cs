using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PlayerLoadoutTests : FiftyWorldFixture
    {
        SavedEquipmentLoadout Save(string name)
        {
            Assert.True(PlayerLoadoutService.SaveCurrent(Actor, name, out var saved, out string reason), reason);
            return saved;
        }
        Entity Wear(string name)
        {
            var item = Carry(name);
            Assert.True(InventorySystem.Equip(Actor, item));
            return item;
        }

        [Test]
        public void SaveIsReadOnlyForGearAndNeverSpendsOrGrantsSkillPoints()
        {
            var tooth = Wear("QuarryToothTrinket");
            int sp = Actor.GetStatValue("SP");
            var slots = Actor.GetPart<Body>().GetParts().Where(p => p._Equipped == tooth).ToArray();
            var saved = Save("Heavy hand");
            Assert.AreEqual(saved.ID, PlayerLoadoutService.GetPrepared(Actor, 0).ID);
            Assert.True(PlayerLoadoutService.MatchesCurrent(Actor, saved));
            Assert.AreEqual(sp, Actor.GetStatValue("SP"));
            Assert.AreEqual(1, Actor.GetPart<SkillsPart>().GetRank(nameof(CudgelSkill)));
            Assert.AreSame(tooth, slots[0]._Equipped);
            Assert.AreEqual(tooth.ID, saved.Items.Single().ItemID);
        }

        [Test]
        public void SuccessfulSwitchUsesNormalGearBonusesAndOneActionReceipt()
        {
            var tooth = Wear("QuarryToothTrinket"); var first = Save("Quarry");
            Assert.True(InventorySystem.UnequipItem(Actor, tooth));
            var bell = Wear("TonguelessBellTrinket"); var second = Save("Company");
            Assert.False(PlayerLoadoutService.MatchesCurrent(Actor, first));
            var result = PlayerLoadoutService.ApplyPrepared(Actor, 0);
            Assert.True(result.Success, result.ErrorMessage); Assert.True(result.ActionCommitted);
            Assert.True(PlayerLoadoutService.MatchesCurrent(Actor, first));
            Assert.True(InventorySystem.IsEquipped(Actor, tooth)); Assert.False(InventorySystem.IsEquipped(Actor, bell));
            Assert.AreEqual(1, Actor.GetPart<SkillsPart>().GetRank(nameof(CudgelSkill)));
            Assert.AreEqual(0, Actor.GetPart<SkillsPart>().GetRank(nameof(Persuasion_Recruit)));
            Assert.AreEqual(first.ID, PlayerLoadoutService.GetState(Actor).LastAppliedID);
            result = PlayerLoadoutService.ApplyPrepared(Actor, 0);
            Assert.True(result.Success); Assert.False(result.ActionCommitted, "Already worn is a free no-op.");
            result = PlayerLoadoutService.ApplyPrepared(Actor, 1);
            Assert.True(result.Success, result.ErrorMessage); Assert.True(result.ActionCommitted);
            Assert.True(PlayerLoadoutService.MatchesCurrent(Actor, second));
        }

        [Test]
        public void SavedLibraryAndPreparedPairSurviveTwoProductionGraphRoundTrips()
        {
            var tooth = Wear("QuarryToothTrinket"); var first = Save("Quarry");
            InventorySystem.UnequipItem(Actor, tooth);
            var bell = Wear("TonguelessBellTrinket"); var second = Save("Company");
            var archive = Save("Later");
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            var state = PlayerLoadoutService.GetState(loaded);
            Assert.AreEqual(3, state.Saved.Count);
            Assert.AreEqual(first.ID, state.PreparedA); Assert.AreEqual(second.ID, state.PreparedB);
            Assert.AreEqual(2, state.InitialPreparations);
            Assert.True(PlayerLoadoutService.ApplyPrepared(loaded, 0).ActionCommitted);
            Assert.AreEqual(tooth.ID, loaded.GetPart<InventoryPart>().GetAllEquipped().Single().ID);
            Assert.True(PlayerLoadoutService.ApplyPrepared(loaded, 1).ActionCommitted);
            Assert.AreEqual(bell.ID, loaded.GetPart<InventoryPart>().GetAllEquipped().Single().ID);
            Assert.False(PlayerLoadoutService.Prepare(loaded, archive.ID, 0, null, out _));
        }

        [Test]
        public void AdditionalTrinketSlotKeepsOldPresetUsableAndLeavesNewSlotEmpty()
        {
            var tooth = Wear("QuarryToothTrinket"); var saved = Save("Two-slot days");
            Assert.True(TrinketSlots.EnsureMinimum(Actor, 3));
            var bell = Wear("TonguelessBellTrinket");
            var extra = Wear("DuelistsKnotTrinket");
            Assert.True(PlayerLoadoutService.CanApplyPrepared(Actor, 0, out string reason), reason);
            var result = PlayerLoadoutService.ApplyPrepared(Actor, 0);
            Assert.True(result.Success, result.ErrorMessage); Assert.True(result.ActionCommitted);
            Assert.True(PlayerLoadoutService.MatchesCurrent(Actor, saved));
            Assert.True(InventorySystem.IsEquipped(Actor, tooth));
            Assert.False(InventorySystem.IsEquipped(Actor, bell)); Assert.False(InventorySystem.IsEquipped(Actor, extra));
            Assert.AreEqual(3, TrinketSlots.GetCapacity(Actor));
        }

        [Test]
        public void InvalidNamesOrSlotsCannotConsumeInitialPreparationAllowance()
        {
            foreach (var name in new[] { "", "  ", "bad\nname", new string('x', 41) })
                Assert.False(PlayerLoadoutService.SaveCurrent(Actor, name, out _, out _));
            Assert.Null(PlayerLoadoutService.GetState(Actor));
            Assert.False(PlayerLoadoutService.ApplyPrepared(Actor, -1).Success);
            Assert.False(PlayerLoadoutService.ApplyPrepared(Actor, 2).Success);
            var saved = Save("Ready");
            Assert.AreEqual(saved.ID, PlayerLoadoutService.GetPrepared(Actor, 0).ID);
            Assert.AreEqual(1, PlayerLoadoutService.GetState(Actor).InitialPreparations);
        }

        [Test]
        public void ReentrantSavePrepareAndSwapAreRejectedWhileEquipmentHooksRun()
        {
            var tooth = Wear("QuarryToothTrinket"); Save("Quarry");
            InventorySystem.UnequipItem(Actor, tooth); Save("Empty");
            var probe = new ReentrantLoadoutProbe(); Actor.AddPart(probe);
            var result = PlayerLoadoutService.ApplyPrepared(Actor, 0);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.AreEqual(1, probe.Calls);
            Assert.False(probe.SaveSucceeded); Assert.False(probe.SwapSucceeded);
            Assert.AreEqual(2, PlayerLoadoutService.GetState(Actor).Saved.Count);
        }
    }

    public sealed class ReentrantLoadoutProbe : Part
    {
        public int Calls;
        public bool SaveSucceeded, SwapSucceeded;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "BeforeEquip") return true;
            Calls++;
            SaveSucceeded = PlayerLoadoutService.SaveCurrent(ParentEntity, "Nested", out _, out _);
            SwapSucceeded = PlayerLoadoutService.ApplyPrepared(ParentEntity, 1).Success;
            return true;
        }
    }
}
