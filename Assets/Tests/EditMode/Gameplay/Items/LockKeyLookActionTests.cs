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
        // 0. BLUEPRINT path — the showcase spawns LockedDoor via the
        //    Factory (blueprint loader), not via `new Entity` + AddPart.
        //    This test pins the SAME path the runtime takes, so a
        //    "Unlock entry doesn't appear in-game" report can be
        //    distinguished from a unit-test-only artifact.
        // ====================================================================

        [Test]
        public void LockedDoorShowcase_LiveResolveTarget_GatherActionsIncludesUnlock()
        {
            // Most-faithful runtime simulation: actually run the
            // showcase scenario, then resolve the door's cell exactly
            // the way 'l' look-mode does. If THIS fails but the
            // previous tests pass, the issue is something
            // showcase-spawn-side (e.g., the door cell ends up with
            // a higher-render-layer entity that ResolveTarget picks
            // over the door).
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CavesOfOoo.Scenarios.Custom.LockedDoorShowcase().Apply(ctx);

            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            // Showcase places the locked door 2 cells east of player.
            var doorCell = ctx.Zone.GetCell(p.x + 2, p.y);
            Assert.IsNotNull(doorCell, "Door cell must exist.");

            var target = WorldInteractionSystem.ResolveTarget(doorCell);
            Assert.IsNotNull(target,
                "ResolveTarget on the door cell must return SOMETHING.");
            Assert.AreEqual("LockedDoor", target.BlueprintName,
                "ResolveTarget must pick the LockedDoor (not a different " +
                "co-located entity). Cell occupants: " +
                string.Join(",", doorCell.Objects.Select(o => o?.BlueprintName ?? "null")));

            var actions = WorldInteractionSystem.GatherActions(target);
            Assert.IsTrue(actions.Any(a => a.Command == "Unlock"),
                "Showcase-spawned LockedDoor's GatherActions must include Unlock. " +
                "Got: [" + string.Join(",", actions.Select(a => a.Command)) + "]");
        }

        [Test]
        public void LockedDoor_FromBlueprint_GetInventoryActions_EmitsUnlockEntry()
        {
            // Use the harness factory so we exercise the same JSON-
            // blueprint → CreatePart suffix-lookup path the showcase
            // hits at runtime. If LockPart isn't attached / configured
            // correctly by the loader, this test will fail where the
            // manually-built test (1.) passes.
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var door = _harness.Factory.CreateEntity("LockedDoor");

            Assert.IsNotNull(door, "LockedDoor blueprint must instantiate.");
            var lockPart = door.GetPart<LockPart>();
            Assert.IsNotNull(lockPart,
                "Blueprint loader must attach LockPart to LockedDoor. " +
                "If null, the JSON `{ \"Name\": \"Lock\" }` isn't resolving " +
                "to LockPart via the suffix-lookup path " +
                "(EntityFactory.CreatePart line 230-249).");
            Assert.IsTrue(lockPart.IsLocked,
                "Blueprint LockedDoor must spawn locked.");

            var actions = WorldInteractionSystem.GatherActions(door);
            var unlock = actions.FirstOrDefault(a => a.Command == "Unlock");
            Assert.IsNotNull(unlock,
                "Blueprint-loaded LockedDoor must emit an Unlock entry " +
                "in its GetInventoryActions response. If null, the LockPart " +
                "is attached but its HandleEvent isn't being routed by " +
                "the FireEvent dispatch — check Part registration order.");
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
