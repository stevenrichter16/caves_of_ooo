using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Look-popup-menu integration for the Lock & Key system.
    ///
    /// Pins the "Unlock" entry surfaced by <c>LockPart</c> in the
    /// world action menu (the popup that appears when the player
    /// presses 'l' to look, aims at a cell, and selects an entity).
    /// Mirrors the <see cref="ContainerPart"/> pattern that adds an
    /// Open / Unlock entry conditional on its Locked field.
    ///
    /// Two contracts:
    ///   1. Entry visibility — locked entities emit an "Unlock"
    ///      action; unlocked entities don't; non-locked entities
    ///      don't (counter-check rules out global emission bug).
    ///   2. Execution — selecting the Unlock entry fires
    ///      InventoryAction with Command="Unlock"; LockPart routes
    ///      it through the existing AttemptUnlock path so bump-
    ///      unlock and menu-unlock are one codepath. With matching
    ///      key, IsLocked flips false + Solid drops; without key,
    ///      stays locked.
    /// </summary>
    [TestFixture]
    public class LockKeyLookActionTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity BuildLockedDoor(string keyId = "iron")
        {
            var door = new Entity { ID = "test-door", BlueprintName = "TestLockedDoor" };
            door.AddPart(new PhysicsPart { Solid = true });
            door.AddPart(new LockPart { KeyId = keyId, IsLocked = true });
            return door;
        }

        private Entity BuildKey(string keyId)
        {
            var key = new Entity { ID = "test-key-" + keyId, BlueprintName = "TestKey" };
            key.AddPart(new KeyPart { KeyId = keyId });
            return key;
        }

        private Entity BuildPlayerWithKeyOptional(string keyId = null)
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var actor = ctx.PlayerEntity;
            var inv = actor.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (actor.GetPart<InventoryPart>() == null) actor.AddPart(inv);
            inv.Objects.Clear();
            if (keyId != null) inv.AddObject(BuildKey(keyId));
            return actor;
        }

        // ====================================================================
        // 1. Entry visibility — locked entity emits "Unlock"
        // ====================================================================

        [Test]
        public void LockedDoor_GetInventoryActions_EmitsUnlockEntry()
        {
            var door = BuildLockedDoor();
            var actions = WorldInteractionSystem.GatherActions(door);

            var unlock = actions.FirstOrDefault(a => a.Command == "Unlock");
            Assert.IsNotNull(unlock,
                "GatherActions on a locked door must include an Unlock entry.");
            Assert.AreEqual("unlock", unlock.Display,
                "Display label should be 'unlock' (lowercase) — matches " +
                "ContainerPart's existing 'unlock' / 'open' convention.");
            Assert.AreEqual('u', unlock.Key,
                "Hotkey 'u' for Unlock — same as ContainerPart's chest-unlock entry.");
        }

        // ====================================================================
        // 2. Entry visibility — unlocked entity does NOT emit Unlock
        // ====================================================================

        [Test]
        public void UnlockedDoor_GetInventoryActions_NoUnlockEntry()
        {
            var door = BuildLockedDoor();
            // Pre-unlock the door (simulating a successful prior unlock).
            door.GetPart<LockPart>().IsLocked = false;

            var actions = WorldInteractionSystem.GatherActions(door);

            Assert.IsFalse(actions.Any(a => a.Command == "Unlock"),
                "An already-unlocked door must NOT show an Unlock entry " +
                "(prevents the menu being cluttered with no-ops).");
        }

        // ====================================================================
        // 3. Counter-check — regular Solid wall (no LockPart) emits no Unlock
        // ====================================================================

        [Test]
        public void RegularWall_NoLockPart_GetInventoryActions_NoUnlockEntry()
        {
            var wall = new Entity { ID = "test-wall", BlueprintName = "TestWall" };
            wall.AddPart(new PhysicsPart { Solid = true });
            // No LockPart.

            var actions = WorldInteractionSystem.GatherActions(wall);

            Assert.IsFalse(actions.Any(a => a.Command == "Unlock"),
                "Solid entities without a LockPart must NOT emit Unlock — " +
                "rules out a bug that would attach Unlock to every wall.");
        }

        // ====================================================================
        // 4. Execution — Unlock command with matching key flips IsLocked
        //    + drops Solid (so the next move walks through)
        // ====================================================================

        [Test]
        public void UnlockCommand_WithMatchingKey_FlipsLockedAndDropsSolid()
        {
            var door = BuildLockedDoor("iron");
            var actor = BuildPlayerWithKeyOptional("iron");

            // Fire the same event the InputHandler fires on selection.
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Unlock");
            e.SetParameter("Actor", (object)actor);
            door.FireEventAndRelease(e);

            Assert.IsFalse(door.GetPart<LockPart>().IsLocked,
                "Matching key must flip IsLocked to false.");
            Assert.IsFalse(door.GetPart<PhysicsPart>().Solid,
                "Successful unlock must drop Solid so the next move walks through.");
        }

        // ====================================================================
        // 5. Execution — Unlock command without key stays locked + keeps Solid
        // ====================================================================

        [Test]
        public void UnlockCommand_NoMatchingKey_StaysLockedAndKeepsSolid()
        {
            var door = BuildLockedDoor("iron");
            var actor = BuildPlayerWithKeyOptional(null);  // empty inventory

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Unlock");
            e.SetParameter("Actor", (object)actor);
            door.FireEventAndRelease(e);

            Assert.IsTrue(door.GetPart<LockPart>().IsLocked,
                "Without matching key the door must remain locked.");
            Assert.IsTrue(door.GetPart<PhysicsPart>().Solid,
                "Unsuccessful unlock must NOT drop Solid — door still blocks.");
        }

        // ====================================================================
        // 6. Decoration lock — empty KeyId means bump auto-opens. The
        //    look popup STILL emits the Unlock entry (so the player can
        //    pick "Unlock" without needing to bump first); selecting it
        //    auto-unlocks regardless of inventory.
        // ====================================================================

        [Test]
        public void UnlockCommand_DecorationLock_EmptyKeyId_AutoUnlocks()
        {
            var door = BuildLockedDoor(keyId: "");  // decoration lock
            var actor = BuildPlayerWithKeyOptional(null);  // no key

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Unlock");
            e.SetParameter("Actor", (object)actor);
            door.FireEventAndRelease(e);

            Assert.IsFalse(door.GetPart<LockPart>().IsLocked,
                "Decoration lock (empty KeyId) auto-opens regardless of inventory.");
            Assert.IsFalse(door.GetPart<PhysicsPart>().Solid,
                "Solid must drop on a successful auto-unlock.");
        }
    }
}
