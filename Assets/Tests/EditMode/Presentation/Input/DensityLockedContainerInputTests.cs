using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityLockedContainerInputTests : ShortcutFixture
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void StaleOpenSelectionCannotRevealEitherKindOfLockedContainer(bool partLock, bool legacyLock)
        {
            var chest = Item("LockedChest");
            var container = chest.GetPart<ContainerPart>();
            var keyLock = chest.GetPart<LockPart>();
            keyLock.IsLocked = false;
            container.Locked = false;
            var item = Item("Dagger");
            Assert.IsTrue(container.AddItem(item));
            Assert.IsTrue(Zone.AddEntity(chest, 11, 10));
            var action = WorldInteractionSystem.GatherActions(chest, Player)
                .Single(a => a.Command == "OpenContainer");

            // Simulate a lock changing while the previously gathered menu is open.
            keyLock.IsLocked = partLock;
            container.Locked = legacyLock;
            var input = Component<InputHandler>();
            input.PlayerEntity = Player;
            input.CurrentZone = Zone;
            input.PickupUI = Component<PickupUI>();
            input.PickupUI.Tilemap = Tiles();
            input.PickupUI.PopupCamera = OutsideCamera();
            State(input, "_worldActionMenuReturnState", "Normal");
            State(input, "_inputState", "WorldActionMenuOpen");
            Call(input, "ExecuteWorldActionSelection", action, chest, Zone.GetEntityCell(chest), false);

            bool locked = partLock || legacyLock;
            Assert.AreEqual(!locked, input.PickupUI.IsOpen,
                "A stale menu must obey the current authoritative lock.");
            Assert.AreEqual(locked ? "Normal" : "PickupOpen", Get(input, "_inputState").ToString());
            Assert.AreSame(item, container.Contents.Single());
            Assert.IsFalse(Inventory.Contains(item));
            Assert.AreEqual(partLock, keyLock.IsLocked);
            Assert.AreEqual(legacyLock, container.Locked);
        }
    }
}
