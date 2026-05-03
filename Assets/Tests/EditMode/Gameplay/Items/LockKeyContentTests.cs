using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LK.4 tests for the Lock & Key content blueprints + the
    /// `furniture/UnlockAttempted` diag hook. Pins:
    ///   • LockedDoor blueprint instantiates with a LockPart whose
    ///     KeyId="iron" and IsLocked=true.
    ///   • IronKey blueprint instantiates with a KeyPart whose
    ///     KeyId="iron".
    ///   • Bumping a locked door records exactly one
    ///     furniture/UnlockAttempted entry per attempt, with
    ///     succeeded=true on key match and succeeded=false on miss.
    /// </summary>
    [TestFixture]
    public class LockKeyContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. LockedDoor blueprint shape
        // ====================================================================

        [Test]
        public void LockedDoorBlueprint_HasLockPart_WithIronKeyId()
        {
            var door = _harness.Factory.CreateEntity("LockedDoor");
            Assert.IsNotNull(door, "LockedDoor blueprint must exist + instantiate.");

            var lockPart = door.GetPart<LockPart>();
            Assert.IsNotNull(lockPart, "LockedDoor must have a LockPart.");
            Assert.AreEqual("iron", lockPart.KeyId,
                "LockedDoor blueprint defaults to KeyId='iron'.");
            Assert.IsTrue(lockPart.IsLocked,
                "LockedDoor must spawn locked.");

            var physics = door.GetPart<PhysicsPart>();
            Assert.IsNotNull(physics, "LockedDoor must have PhysicsPart.");
            Assert.IsTrue(physics.Solid,
                "LockedDoor must spawn Solid (so PhysicsPart's bump check sees it).");
        }

        // ====================================================================
        // 2. IronKey blueprint shape
        // ====================================================================

        [Test]
        public void IronKeyBlueprint_HasKeyPart_WithIronKeyId()
        {
            var key = _harness.Factory.CreateEntity("IronKey");
            Assert.IsNotNull(key, "IronKey blueprint must exist + instantiate.");

            var keyPart = key.GetPart<KeyPart>();
            Assert.IsNotNull(keyPart, "IronKey must have a KeyPart.");
            Assert.AreEqual("iron", keyPart.KeyId,
                "IronKey blueprint defaults to KeyId='iron'.");

            var physics = key.GetPart<PhysicsPart>();
            Assert.IsNotNull(physics, "IronKey must have PhysicsPart.");
            Assert.IsTrue(physics.Takeable,
                "IronKey must be Takeable (player can pick it up).");
        }

        // ====================================================================
        // 3. LockedChest blueprint shape (counter-check: same lock pattern
        //    works on a different furniture type, not just doors)
        // ====================================================================

        [Test]
        public void LockedChestBlueprint_HasLockPart_WithIronKeyId()
        {
            var chest = _harness.Factory.CreateEntity("LockedChest");
            Assert.IsNotNull(chest, "LockedChest blueprint must exist + instantiate.");

            var lockPart = chest.GetPart<LockPart>();
            Assert.IsNotNull(lockPart, "LockedChest must have a LockPart.");
            Assert.AreEqual("iron", lockPart.KeyId);
            Assert.IsTrue(lockPart.IsLocked);
        }

        // ====================================================================
        // 4. Bump locked door records furniture/UnlockAttempted with
        //    succeeded=true when actor has the matching key
        // ====================================================================

        [Test]
        public void BumpLockedDoor_WithKey_RecordsFurnitureDiagWithSucceededTrue()
        {
            var (actor, door, _, ctx) = SetupActorAndDoor(addKeyToInventory: true);
            Diag.ResetAll();

            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Exactly one UnlockAttempted record per bump. Got {records.Count}.");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"succeeded\":true"),
                $"With matching key, succeeded must be true. Payload: {records[0].PayloadJson}");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"keyId\":\"iron\""),
                $"keyId payload field must echo the LockPart's KeyId='iron'. " +
                $"Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 5. Bump locked door records furniture/UnlockAttempted with
        //    succeeded=false when actor has no matching key
        // ====================================================================

        [Test]
        public void BumpLockedDoor_NoKey_RecordsFurnitureDiagWithSucceededFalse()
        {
            var (actor, door, _, ctx) = SetupActorAndDoor(addKeyToInventory: false);
            Diag.ResetAll();

            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count);
            Assert.IsTrue(records[0].PayloadJson.Contains("\"succeeded\":false"),
                $"Without matching key, succeeded must be false. " +
                $"Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 6. Counter-check: bumping a regular wall (no LockPart) does NOT
        //    produce ANY furniture/UnlockAttempted records — the diag hook
        //    only fires for LockPart-bearing blockers.
        // ====================================================================

        [Test]
        public void BumpRegularWall_NoLockPart_RecordsNoFurnitureDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var actor = ctx.PlayerEntity;
            var actorCell = ctx.Zone.GetEntityCell(actor);

            // Plain Solid wall — no LockPart.
            var wall = new Entity { ID = "test-wall", BlueprintName = "TestWall" };
            wall.AddPart(new PhysicsPart { Solid = true });
            ctx.Zone.AddEntity(wall, actorCell.X + 1, actorCell.Y);

            Diag.ResetAll();
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.AreEqual(0, records.Count,
                $"A plain Solid wall must NOT trigger UnlockAttempted records. " +
                $"Got {records.Count}: " +
                $"[{string.Join(", ", records.Select(r => r.PayloadJson))}]. " +
                $"If non-zero, the LK.3 hook is misfiring on non-LockPart blockers.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build a scenario with the player + a LockedDoor blueprint
        /// adjacent. Optionally add an IronKey blueprint to player
        /// inventory.
        /// </summary>
        private (Entity actor, Entity door, LockPart lockPart, ScenarioContext ctx)
            SetupActorAndDoor(bool addKeyToInventory)
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var actor = ctx.PlayerEntity;
            var actorCell = ctx.Zone.GetEntityCell(actor);

            var door = _harness.Factory.CreateEntity("LockedDoor");
            ctx.Zone.AddEntity(door, actorCell.X + 1, actorCell.Y);

            // Make sure inventory is in a known state.
            var inv = actor.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (actor.GetPart<InventoryPart>() == null) actor.AddPart(inv);
            inv.Objects.Clear();

            if (addKeyToInventory)
            {
                var key = _harness.Factory.CreateEntity("IronKey");
                inv.AddObject(key);
            }

            return (actor, door, door.GetPart<LockPart>(), ctx);
        }
    }
}
