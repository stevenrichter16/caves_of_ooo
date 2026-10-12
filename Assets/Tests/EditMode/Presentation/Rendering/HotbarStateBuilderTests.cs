using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class HotbarStateBuilderTests
    {
        [Test]
        public void UnboundVaultChangesHintAndCancelRestoresIt()
        {
            var player = new Entity();
            player.AddPart(new ActivatedAbilitiesPart());
            var vault = new ActivatedAbility { Command = "CommandVault" };
            var normal = HotbarStateBuilder.Build(player, -1, null);
            var aiming = HotbarStateBuilder.Build(player, -1, vault);
            Assert.That(aiming.HintText, Does.Contain("Move cursor"));
            Assert.AreEqual(normal.HintText, HotbarStateBuilder.Build(player, -1, null).HintText);
        }

        [Test]
        public void OldSavedVaultMetadataDisplaysCurrentCursorRange()
        {
            var player = new Entity();
            var abilities = new ActivatedAbilitiesPart();
            player.AddPart(abilities);
            abilities.AddAbility("Vault", "CommandVault", "Skills", AbilityTargetingMode.DirectionLine, 2, "Acrobatics_Vault");
            var snapshot = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreEqual(AbilityTargetingMode.CursorCell, snapshot.Slots[0].TargetingMode);
            Assert.AreEqual(2, snapshot.Slots[0].Range);
        }

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
                "Pyromancy_Kindle"); // tooltip row re-keyed to the ported skill (migration M1)
            var quenchId = abilities.AddAbility(
                "Quench Water",
                "CommandQuench",
                "Spell",
                AbilityTargetingMode.AdjacentCell,
                5,
                "Hydromancy_Quench"); // tooltip row re-keyed to the ported skill (migration M1)

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
            Assert.That(snapshot.SummaryText, Does.Contain("AIM | adjacent"));
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
