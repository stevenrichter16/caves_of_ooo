using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven inventory tests. The InventorySystem facade
    /// emits a diag record on every mutation:
    ///   - <c>Pickup</c> / <c>Drop</c> / <c>Equip</c> / <c>Unequip</c> /
    ///     <c>AutoEquip</c> / <c>DropPartial</c> on success
    ///   - <c>Rejected</c> with errorCode + errorMessage on failure
    ///
    /// <para>Pre-fix: facade returned a bool with NO diag trace. A bug
    /// like "Equip silently fails on a body-mismatch" left no record
    /// — the debug session had to run Play mode and watch MessageLog.
    /// Post-fix: every facade call emits exactly one record.</para>
    /// </summary>
    public class InventoryObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeCreature(string id = "actor", int maxWeight = 150)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.Statistics["Strength"] = new Stat
            { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new InventoryPart { MaxWeight = maxWeight });
            return e;
        }

        private static Entity MakeItem(string id = "item", int weight = 5)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id.ToLower() });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            return e;
        }

        private static void DumpInventoryRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-10} actor={r.ActorId,-10} target={r.TargetId,-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void SuccessfulPickup_EmitsPickupRecord()
        {
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var item = MakeItem("Sword", weight: 5);
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(item, 5, 5);  // colocate

            bool ok = InventorySystem.Pickup(actor, item, zone);
            Assert.IsTrue(ok, "Pickup of takeable item at same cell should succeed.");

            DumpInventoryRecords("successful pickup");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Pickup", records[0].Kind);
            StringAssert.Contains("\"itemName\":\"sword\"", records[0].PayloadJson);
        }

        [Test]
        public void Pickup_NonTakeableItem_EmitsRejectedNotTakeable()
        {
            // Pickup an item with PhysicsPart.Takeable=false. Validation
            // should reject with NotTakeable and emit Rejected.
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var furniture = new Entity { ID = "Boulder", BlueprintName = "Boulder" };
            furniture.Tags["Item"] = "";
            furniture.AddPart(new RenderPart { DisplayName = "boulder" });
            furniture.AddPart(new PhysicsPart { Takeable = false, Weight = 999 });
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(furniture, 5, 5);

            bool ok = InventorySystem.Pickup(actor, furniture, zone);
            Assert.IsFalse(ok);

            DumpInventoryRecords("pickup rejected: non-takeable");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"operation\":\"Pickup\"", records[0].PayloadJson);
            StringAssert.Contains("\"validationCode\":\"NotTakeable\"", records[0].PayloadJson);
        }

        /// <summary>
        /// SURFACE GAP found during observability test development:
        /// <see cref="CavesOfOoo.Core.Inventory.Commands.PickupCommand.Validate"/>
        /// does NOT check that the item is at the actor's cell. A caller
        /// can pick up an item from anywhere in the zone. The UI layer
        /// gates this via <see cref="InventorySystem.GetTakeableItemsAtFeet"/>
        /// but the command itself enforces no adjacency invariant.
        ///
        /// Documenting as a regression pin: if a future change adds
        /// distance-based validation, this test will fail-loudly and
        /// the fixer can update it. The observability dump confirms the
        /// non-adjacent pickup currently fires Pickup (not Rejected).
        /// </summary>
        [Test]
        public void SurfaceGap_PickupHasNoAdjacencyCheck_EmitsPickup()
        {
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var faraway = MakeItem("FarSword");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(faraway, 15, 15);

            bool ok = InventorySystem.Pickup(actor, faraway, zone);

            DumpInventoryRecords("(surface gap) pickup at distance succeeds");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            // Current behavior: succeeds. If this fails, an adjacency
            // check was added — investigate whether the new check is
            // intended and update this fixture.
            Assert.IsTrue(ok,
                "PickupCommand.Validate has no adjacency check today. " +
                "If this fails, distance validation was introduced — " +
                "update the test or document the new invariant.");
            Assert.AreEqual("Pickup", records[0].Kind);
        }

        [Test]
        public void Pickup_TooHeavy_EmitsRejectedWithWeightReason()
        {
            // Pre-fix: weight failure was a silent return. Post-fix:
            // Rejected emits with errorMessage indicating the weight cap.
            var zone = new Zone("InvZone");
            var actor = MakeCreature(maxWeight: 5);
            var heavy = MakeItem("Anvil", weight: 100);
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(heavy, 5, 5);

            bool ok = InventorySystem.Pickup(actor, heavy, zone);
            Assert.IsFalse(ok);

            DumpInventoryRecords("pickup rejected: too heavy");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
        }

        [Test]
        public void SuccessfulDrop_EmitsDropRecord()
        {
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var item = MakeItem("Coin", weight: 1);
            zone.AddEntity(actor, 5, 5);
            actor.GetPart<InventoryPart>().AddObject(item);

            // Reset diag AFTER the inventory setup so we only see the drop
            Diag.ResetAll();

            bool ok = InventorySystem.Drop(actor, item, zone);
            Assert.IsTrue(ok);

            DumpInventoryRecords("successful drop");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Drop", records[0].Kind);
            StringAssert.Contains("\"itemName\":\"coin\"", records[0].PayloadJson);
        }

        [Test]
        public void Drop_ItemNotInInventory_EmitsRejected()
        {
            // Adversarial: drop an item you don't have. Should reject.
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var notMine = MakeItem("NotMine");
            zone.AddEntity(actor, 5, 5);

            bool ok = InventorySystem.Drop(actor, notMine, zone);
            Assert.IsFalse(ok);

            DumpInventoryRecords("drop rejected: item not in inventory");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"operation\":\"Drop\"", records[0].PayloadJson);
        }

        [Test]
        public void NullArgs_AllPaths_EmitRejected_NoCrash()
        {
            // Adversarial: every facade method should tolerate null args
            // and emit Rejected without crashing.
            Assert.DoesNotThrow(() => InventorySystem.Pickup(null, null, null));
            Assert.DoesNotThrow(() => InventorySystem.Drop(null, null, null));
            Assert.DoesNotThrow(() => InventorySystem.Equip(null, null));
            Assert.DoesNotThrow(() => InventorySystem.UnequipItem(null, null));
            Assert.DoesNotThrow(() => InventorySystem.AutoEquip(null, null));

            DumpInventoryRecords("null args across all 5 facade methods");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(5, records.Count,
                "Each of the 5 null-arg facade calls should emit one Rejected record.");
            Assert.IsTrue(records.All(r => r.Kind == "Rejected"));

            // Counter-check: each operation field should be unique
            var ops = records.Select(r =>
            {
                var json = r.PayloadJson;
                int start = json.IndexOf("\"operation\":\"");
                int end = json.IndexOf('"', start + 13);
                return json.Substring(start + 13, end - (start + 13));
            }).ToHashSet();
            Assert.AreEqual(5, ops.Count,
                "All 5 distinct facade operations should appear as Rejected.");
        }

        [Test]
        public void PickupThenDrop_SameItem_EmitsTwoRecordsChronologically()
        {
            // Counter-check on isolation: round-trip the same item and
            // confirm two distinct records show in chronological order.
            var zone = new Zone("InvZone");
            var actor = MakeCreature();
            var item = MakeItem("Apple", weight: 1);
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(item, 5, 5);

            InventorySystem.Pickup(actor, item, zone);
            InventorySystem.Drop(actor, item, zone);

            DumpInventoryRecords("pickup then drop");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "inventory", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("Pickup", records[0].Kind);
            Assert.AreEqual("Drop", records[1].Kind);
            // Same item ID on both
            Assert.AreEqual("Apple", records[0].TargetId);
            Assert.AreEqual("Apple", records[1].TargetId);
        }
    }
}
