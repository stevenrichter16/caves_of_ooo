using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LK.3 tests for the bump-to-unlock mechanism. Drives the path:
    ///   Actor walks into a locked entity → PhysicsPart sees the
    ///   blocker has a LockPart, fires AttemptUnlock → LockPart
    ///   inspects the actor's inventory for a matching KeyPart →
    ///   on match, flips IsLocked=false and drops Solid; on miss,
    ///   keeps the door locked. Move stays blocked this turn either
    ///   way (unlocking is the action; walk-through is the next turn).
    ///
    /// Pattern: build a minimal zone with two adjacent cells; place
    /// an actor at (1,1) and a locked-door entity at (2,1); call
    /// MovementSystem.TryMove(dx=+1, dy=0); inspect post-state.
    /// </summary>
    [TestFixture]
    public class LockKeyBumpUnlockTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        // ====================================================================
        // Helpers
        // ====================================================================

        private (Entity actor, Entity door, LockPart lockPart, ScenarioContext ctx)
            BuildScenarioWithDoor(string lockKeyId)
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var actor = ctx.PlayerEntity;
            var actorCell = ctx.Zone.GetEntityCell(actor);

            // Build a "locked door" entity at actorCell + (1,0):
            //   PhysicsPart with Solid=true (so PhysicsPart's bump check sees it)
            //   LockPart with the requested KeyId + IsLocked=true
            var door = new Entity { ID = "test-door", BlueprintName = "TestLockedDoor" };
            door.AddPart(new PhysicsPart { Solid = true });
            var lockPart = new LockPart { KeyId = lockKeyId, IsLocked = true };
            door.AddPart(lockPart);

            ctx.Zone.AddEntity(door, actorCell.X + 1, actorCell.Y);

            return (actor, door, lockPart, ctx);
        }

        private static Entity BuildKey(string keyId)
        {
            var key = new Entity { ID = "test-key-" + keyId, BlueprintName = "TestKey" };
            key.AddPart(new KeyPart { KeyId = keyId });
            return key;
        }

        private static void GiveKeyToActor(Entity actor, Entity key)
        {
            var inv = actor.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (actor.GetPart<InventoryPart>() == null) actor.AddPart(inv);
            inv.AddObject(key);
        }

        // ====================================================================
        // 1. Bump locked door with no key — stays locked, move blocked
        // ====================================================================

        [Test]
        public void BumpLockedDoor_NoKey_StaysLockedAndMoveBlocked()
        {
            var (actor, door, lockPart, ctx) = BuildScenarioWithDoor(lockKeyId: "iron");
            // Strip any default-blueprint inventory contents that could
            // conceivably have a KeyPart (defensive).
            var inv = actor.GetPart<InventoryPart>();
            if (inv != null) inv.Objects.Clear();

            var actorCell = ctx.Zone.GetEntityCell(actor);
            int beforeX = actorCell.X;
            bool moved = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            Assert.IsFalse(moved, "Move into locked door must be blocked.");
            Assert.IsTrue(lockPart.IsLocked, "Lock must remain locked without a key.");
            var actorCellAfter = ctx.Zone.GetEntityCell(actor);
            Assert.AreEqual(beforeX, actorCellAfter.X,
                "Actor must not have advanced — bump was rejected.");
        }

        // ====================================================================
        // 2. Bump locked door with matching key — unlocks (still blocked this turn)
        // ====================================================================

        [Test]
        public void BumpLockedDoor_WithMatchingKey_UnlocksButMoveBlockedThisTurn()
        {
            var (actor, door, lockPart, ctx) = BuildScenarioWithDoor(lockKeyId: "iron");
            GiveKeyToActor(actor, BuildKey("iron"));

            var actorCell = ctx.Zone.GetEntityCell(actor);
            int beforeX = actorCell.X;
            bool moved = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            Assert.IsFalse(moved,
                "First bump unlocks but does NOT advance — unlocking IS the action.");
            Assert.IsFalse(lockPart.IsLocked,
                "Matching key in inventory must flip IsLocked to false.");
            var doorPhysics = door.GetPart<PhysicsPart>();
            Assert.IsFalse(doorPhysics.Solid,
                "Successful unlock drops the door's Solid flag so the next " +
                "move can walk through.");

            // Second move SHOULD now succeed.
            bool moved2 = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);
            Assert.IsTrue(moved2, "After unlock, the next move must walk through.");
        }

        // ====================================================================
        // 3. Bump locked door with WRONG key — stays locked
        // ====================================================================

        [Test]
        public void BumpLockedDoor_WithWrongKey_StaysLocked()
        {
            var (actor, door, lockPart, ctx) = BuildScenarioWithDoor(lockKeyId: "iron");
            GiveKeyToActor(actor, BuildKey("brass"));  // mismatched

            bool moved = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            Assert.IsFalse(moved, "Move blocked when key id does not match.");
            Assert.IsTrue(lockPart.IsLocked,
                "Wrong-key bump must NOT unlock — IsLocked stays true.");
        }

        // ====================================================================
        // 4. Bump door with empty KeyId (decoration lock) — auto-unlocks
        // ====================================================================

        [Test]
        public void BumpDoorWithEmptyKeyId_UnlocksWithoutKeyInInventory()
        {
            // KeyId="" = decoration lock; bump auto-opens.
            var (actor, door, lockPart, ctx) = BuildScenarioWithDoor(lockKeyId: "");
            // Empty inventory.
            var inv = actor.GetPart<InventoryPart>();
            if (inv != null) inv.Objects.Clear();

            bool moved = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            Assert.IsFalse(moved, "First bump still blocks (unlocking IS the action).");
            Assert.IsFalse(lockPart.IsLocked,
                "Empty KeyId must auto-unlock on bump.");
        }

        // ====================================================================
        // 5. Master-key model: using a key does NOT remove it from inventory
        // ====================================================================

        [Test]
        public void BumpLockedDoor_WithKey_KeyStaysInInventory()
        {
            var (actor, door, lockPart, ctx) = BuildScenarioWithDoor(lockKeyId: "iron");
            var key = BuildKey("iron");
            GiveKeyToActor(actor, key);
            int invBefore = actor.GetPart<InventoryPart>().Objects.Count;

            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            int invAfter = actor.GetPart<InventoryPart>().Objects.Count;
            Assert.AreEqual(invBefore, invAfter,
                "v1 keys are reusable (master-key model). KeyPart must remain " +
                "in inventory after a successful unlock. If single-use is wanted, " +
                "add a Consumable flag — see Docs/LOCK-AND-KEY.md self-review.");
            Assert.Contains(key, actor.GetPart<InventoryPart>().Objects);
        }

        // ====================================================================
        // 6. Counter-check: a regular Solid wall (no LockPart) blocks
        //    movement just like before — the LK.3 patch must not regress
        //    the existing bump-blocking behavior for non-locked entities.
        // ====================================================================

        [Test]
        public void BumpRegularWall_NoLockPart_BlocksAsBefore()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var actor = ctx.PlayerEntity;
            var actorCell = ctx.Zone.GetEntityCell(actor);

            // Build a plain Solid wall — no LockPart.
            var wall = new Entity { ID = "test-wall", BlueprintName = "TestWall" };
            wall.AddPart(new PhysicsPart { Solid = true });
            ctx.Zone.AddEntity(wall, actorCell.X + 1, actorCell.Y);

            int beforeX = actorCell.X;
            bool moved = MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            Assert.IsFalse(moved, "Move into a regular wall must still block.");
            var actorCellAfter = ctx.Zone.GetEntityCell(actor);
            Assert.AreEqual(beforeX, actorCellAfter.X);
            // Wall stays solid (no LockPart so no auto-unlock).
            Assert.IsTrue(wall.GetPart<PhysicsPart>().Solid,
                "Regular wall must NOT have its Solid dropped — only " +
                "LockPart-bearing entities get the unlock-and-drop-Solid path.");
        }
    }
}
