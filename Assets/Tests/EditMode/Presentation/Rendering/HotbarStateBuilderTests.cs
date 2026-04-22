using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class HotbarStateBuilderTests
    {
        [Test]
        public void Build_UsesActivatedAbilitySlots_AndTooltipDisplayData()
        {
            var player = new Entity { BlueprintName = "Player" };
            var abilities = new ActivatedAbilitiesPart();
            player.AddPart(abilities);

            var kindleId = abilities.AddAbility(
                "Kindle Flame",
                "CommandKindle",
                "Spell",
                AbilityTargetingMode.AdjacentCell,
                5,
                nameof(KindleMutation));
            var quenchId = abilities.AddAbility(
                "Quench Water",
                "CommandQuench",
                "Spell",
                AbilityTargetingMode.AdjacentCell,
                5,
                nameof(QuenchMutation));

            abilities.CooldownAbility(kindleId, 3);

            HotbarSnapshot snapshot = HotbarStateBuilder.Build(player, 0, abilities.GetAbility(quenchId));

            Assert.AreEqual(GameplayHotbarLayout.SlotCount, snapshot.Slots.Count);
            Assert.AreEqual(0, snapshot.SelectedSlot);
            Assert.AreEqual(1, snapshot.PendingSlot);

            HotbarSlotSnapshot kindle = snapshot.Slots[0];
            Assert.IsTrue(kindle.Occupied);
            Assert.IsTrue(kindle.Selected);
            Assert.AreEqual('1', kindle.Hotkey);
            Assert.AreEqual("Kindle", kindle.DisplayName);
            Assert.AreEqual(3, kindle.CooldownRemaining);
            Assert.AreEqual("&R", kindle.AccentColorCode);

            HotbarSlotSnapshot quench = snapshot.Slots[1];
            Assert.IsTrue(quench.Occupied);
            Assert.IsTrue(quench.Pending);
            Assert.AreEqual("Quench", quench.DisplayName);
            Assert.That(snapshot.SummaryText, Does.Contain("choose a direction"));
        }

        [Test]
        public void Build_ProducesEmptyState_WhenNoSlotsAreBound()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.AddPart(new ActivatedAbilitiesPart());

            HotbarSnapshot snapshot = HotbarStateBuilder.Build(player, -1, null);

            Assert.That(snapshot.SummaryText, Does.Contain("No rite bound"));
            for (int i = 0; i < snapshot.Slots.Count; i++)
            {
                Assert.IsFalse(snapshot.Slots[i].Occupied);
                Assert.AreEqual("empty", snapshot.Slots[i].ShortName);
            }
        }
    }
}
