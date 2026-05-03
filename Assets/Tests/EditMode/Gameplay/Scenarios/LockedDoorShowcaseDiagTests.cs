using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// LK.5 end-to-end verification for <see cref="LockedDoorShowcase"/>.
    /// Drives the showcase the same way a player would (bump the door
    /// east) and asserts the diag substrate captures the expected
    /// `furniture/UnlockAttempted` records.
    ///
    /// Three pinnable contracts:
    ///   1. Bumping the locked door with the iron key in inventory
    ///      records exactly one UnlockAttempted with succeeded=true.
    ///   2. Same bump after dropping the iron key records an
    ///      UnlockAttempted with succeeded=false (and the door stays
    ///      locked).
    ///   3. Bumping the locked chest after walking past the (now-open)
    ///      door also unlocks via the same key — master-key model
    ///      pins.
    ///
    /// Pattern follows the prior 9 scenario-diag fixtures (OnHit /
    /// Trap / Elemental / CombatHooks / CombatParity / ThrowableTonics).
    /// Real Player blueprint, Diag.ResetAll AFTER scenario setup,
    /// post-action diag_query assertions.
    /// </summary>
    [TestFixture]
    public class LockedDoorShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Bump the locked door with the iron key — succeeds
        // ====================================================================

        [Test]
        public void BumpLockedDoor_WithIronKeyInInventory_RecordsSucceededTrue()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new LockedDoorShowcase().Apply(ctx);

            // Showcase places the door 2 cells east of player.
            // Two TryMove(+1, 0) calls — first into empty cell, second into the door.
            var actor = ctx.PlayerEntity;
            Diag.ResetAll();

            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0); // into empty cell (player+1)
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0); // bump locked door (player+2)

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Bumping the locked door must record exactly one " +
                $"furniture/UnlockAttempted. Got {records.Count}.");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"succeeded\":true"),
                $"With iron key in inventory, succeeded must be true. " +
                $"Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 2. Bump the locked door without the iron key — fails
        // ====================================================================

        [Test]
        public void BumpLockedDoor_AfterDroppingKey_RecordsSucceededFalse()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new LockedDoorShowcase().Apply(ctx);
            var actor = ctx.PlayerEntity;

            // Drop the iron key from inventory before bumping.
            var inv = actor.GetPart<InventoryPart>();
            Assert.IsNotNull(inv, "Showcase must give the player an InventoryPart.");
            var ironKey = inv.Objects.FirstOrDefault(e =>
                e != null && e.BlueprintName == "IronKey");
            Assert.IsNotNull(ironKey,
                "Showcase must place an IronKey in player's inventory.");
            inv.RemoveObject(ironKey);

            Diag.ResetAll();

            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0); // empty
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0); // bump

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count);
            Assert.IsTrue(records[0].PayloadJson.Contains("\"succeeded\":false"),
                $"Without iron key, succeeded must be false. " +
                $"Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 3. Bump locked chest with same key (master-key model)
        // ====================================================================

        [Test]
        public void BumpLockedChest_AfterUnlockingDoor_AlsoUnlocks()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new LockedDoorShowcase().Apply(ctx);
            var actor = ctx.PlayerEntity;

            // Walk into door (bump unlock), then through, then bump the chest.
            // Showcase: door at +2, chest at +4. Sequence:
            //   move +1 → empty
            //   move +1 → door (unlock; blocked this turn)
            //   move +1 → walks through (door now non-Solid)
            //   move +1 → empty (player+3)
            //   move +1 → bump chest (player+4)
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);  // +1 empty
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);  // +2 unlock door
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);  // walks +2
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);  // +3 empty
            // Chest at +4. Reset diag now so we only count chest's record.
            Diag.ResetAll();
            MovementSystem.TryMove(actor, ctx.Zone, dx: 1, dy: 0);  // bump chest

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "furniture",
                Kind = "UnlockAttempted",
                Limit = 10,
            }).Records;

            Assert.GreaterOrEqual(records.Count, 1,
                $"Chest bump must record at least one UnlockAttempted. " +
                $"Got {records.Count}.");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"succeeded\":true"),
                $"Same iron key (master-key model) must unlock the chest. " +
                $"Payload: {records[0].PayloadJson}");
        }
    }
}
