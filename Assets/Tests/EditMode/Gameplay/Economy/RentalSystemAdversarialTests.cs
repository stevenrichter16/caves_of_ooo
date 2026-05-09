using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.4 — Adversarial tests for the Weapon Rental System.
    /// Targets bug classes the per-method happy-path tests miss:
    /// <list type="bullet">
    ///   <item>Atomicity (Ink + inventory transfer happen together or
    ///         not at all, with rollback on partial failure)</item>
    ///   <item>Anti-exploit invariants (rented items can't be re-rented,
    ///         can't be sold, can't be returned to wrong lessor)</item>
    ///   <item>Refund precision (floor() math for InkPaid edge cases)</item>
    ///   <item>Equip-then-return edge case (item lives in EquippedItems,
    ///         not Objects, so RemoveObject silently fails — known
    ///         cold-eye bug pattern this fix addresses)</item>
    ///   <item>State purity on rejection paths (no half-mutation when
    ///         a precondition fails)</item>
    /// </list>
    /// </summary>
    public class RentalSystemAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ── Fixture helpers (mirror RentalSystemTests) ────────────────────

        private Entity CreatePlayer(int ink = 100)
        {
            var e = new Entity { BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new InventoryPart());
            RentalSystem.SetInk(e, ink);
            return e;
        }

        private Entity CreateLessor(string blueprintName = "TestQuartermaster")
        {
            var e = new Entity { BlueprintName = blueprintName };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = "Villagers";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = blueprintName });
            e.AddPart(new InventoryPart());
            return e;
        }

        private Entity CreateRentalWeapon(string name = "RentalSword", int value = 100)
        {
            var item = new Entity { BlueprintName = name, ID = name + "_inst" };
            item.Tags["Rentable"] = "";
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            return item;
        }

        // ════════════════════════════════════════════════════════════════
        // A. ATOMICITY — Ink and inventory transfer must happen together
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TryRent_InsufficientInk_NoStateMutated()
        {
            // Renter has 0 ink. TryRent must reject BEFORE any inventory
            // mutation. Adversarial: a buggy impl that removed the item
            // from lessor BEFORE checking ink would orphan the item.
            var renter = CreatePlayer(ink: 0);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 200);
            lessor.GetPart<InventoryPart>().AddObject(item);

            int lessorObjectsBefore = lessor.GetPart<InventoryPart>().Objects.Count;
            int renterObjectsBefore = renter.GetPart<InventoryPart>().Objects.Count;
            int inkBefore = RentalSystem.GetInk(renter);

            Assert.IsFalse(RentalSystem.TryRent(renter, lessor, item),
                "Insufficient ink must reject the rent attempt.");

            Assert.AreEqual(lessorObjectsBefore, lessor.GetPart<InventoryPart>().Objects.Count,
                "Lessor inventory must be unchanged on failed rent.");
            Assert.AreEqual(renterObjectsBefore, renter.GetPart<InventoryPart>().Objects.Count,
                "Renter inventory must be unchanged on failed rent.");
            Assert.AreEqual(inkBefore, RentalSystem.GetInk(renter),
                "Ink must NOT be deducted on failed rent.");
            Assert.IsNull(item.GetPart<RentalPart>(),
                "RentalPart must NOT be added on failed rent.");
        }

        [Test]
        public void Adversarial_TryRent_OverweightRenter_RollsBackToLessor()
        {
            // Set up a tiny renter inventory + heavy item. AddObject on
            // renter's side will fail. The implementation must roll back
            // the lessor's RemoveObject — otherwise the item vanishes.
            var renter = CreatePlayer(ink: 1000);
            var renterInv = renter.GetPart<InventoryPart>();
            renterInv.MaxWeight = 0; // any weight overflows
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            item.GetPart<PhysicsPart>().Weight = 50;
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsFalse(RentalSystem.TryRent(renter, lessor, item),
                "Overweight renter must reject the rent.");
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Has.Member(item),
                "Item must roll back to lessor's inventory after AddObject failure.");
            Assert.That(renter.GetPart<InventoryPart>().Objects, Has.No.Member(item),
                "Item must NOT be in renter's inventory.");
            Assert.AreEqual(1000, RentalSystem.GetInk(renter),
                "Ink must NOT be deducted on rollback path.");
        }

        // ════════════════════════════════════════════════════════════════
        // B. ANTI-EXPLOIT INVARIANTS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TryRent_AlreadyRentedItem_RejectsWithoutMutation()
        {
            // Save-corruption scenario: a rental got back into a lessor's
            // stock somehow. TryRent must refuse rather than double-charge.
            var renter = CreatePlayer(ink: 500);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            // Pre-attach a rental — simulating corrupted state.
            item.AddPart(new RentalPart { InkPaid = 25, LessorBlueprintName = "OtherLessor" });
            lessor.GetPart<InventoryPart>().AddObject(item);

            int inkBefore = RentalSystem.GetInk(renter);
            Assert.IsFalse(RentalSystem.TryRent(renter, lessor, item));
            Assert.AreEqual(inkBefore, RentalSystem.GetInk(renter),
                "Ink unchanged.");
            Assert.AreEqual("OtherLessor", item.GetPart<RentalPart>().LessorBlueprintName,
                "Original RentalPart must be untouched (not overwritten).");
        }

        [Test]
        public void Adversarial_TryReturn_WrongLessor_RejectsWithoutTransfer()
        {
            // Player rented from lessor A but tries to return to lessor B.
            // Must reject — anti-exploit so a hostile actor can't
            // "launder" a rental through a second NPC.
            var renter = CreatePlayer(ink: 1000);
            var lessorA = CreateLessor("LessorA");
            var lessorB = CreateLessor("LessorB");
            var item = CreateRentalWeapon(value: 200);
            lessorA.GetPart<InventoryPart>().AddObject(item);
            Assert.IsTrue(RentalSystem.TryRent(renter, lessorA, item),
                "Setup: rent from lessorA succeeds.");

            int inkBefore = RentalSystem.GetInk(renter);
            Assert.IsFalse(RentalSystem.TryReturn(renter, lessorB, item),
                "Returning to wrong lessor must fail.");
            Assert.That(renter.GetPart<InventoryPart>().Objects, Has.Member(item),
                "Item must STAY with renter.");
            Assert.IsNotNull(item.GetPart<RentalPart>(),
                "RentalPart must NOT be stripped on wrong-lessor return.");
            Assert.AreEqual(inkBefore, RentalSystem.GetInk(renter),
                "No refund issued.");
        }

        [Test]
        public void Adversarial_TryReturn_OnNonRentalItem_RejectsCleanly()
        {
            // Renter holds a regular non-rental item. TryReturn must
            // detect (no RentalPart) and bail before any state change.
            var renter = CreatePlayer(ink: 100);
            var lessor = CreateLessor();
            var regularItem = CreateRentalWeapon();
            // Strip the Rentable tag — simulate normal weapon.
            regularItem.Tags.Remove("Rentable");
            renter.GetPart<InventoryPart>().AddObject(regularItem);

            Assert.IsFalse(RentalSystem.TryReturn(renter, lessor, regularItem),
                "Returning a non-rental item must fail.");
            Assert.That(renter.GetPart<InventoryPart>().Objects, Has.Member(regularItem),
                "Item must stay with renter.");
        }

        [Test]
        public void Adversarial_RentedItem_CannotBeRentedAgain()
        {
            // Once item is rented (has RentalPart), a second TryRent on
            // the same item must fail — even if the renter has plenty
            // of Ink.
            var renter = CreatePlayer(ink: 10000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item));

            // Re-add to lessor's stock somehow (simulating bug or save edit).
            lessor.GetPart<InventoryPart>().AddObject(item);
            int inkBefore = RentalSystem.GetInk(renter);
            Assert.IsFalse(RentalSystem.TryRent(renter, lessor, item),
                "Re-renting an already-rented item must fail.");
            Assert.AreEqual(inkBefore, RentalSystem.GetInk(renter),
                "No double-charge.");
        }

        // ════════════════════════════════════════════════════════════════
        // C. REFUND PRECISION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Refund_PaidOneInk_RefundsZero_FloorMath()
        {
            // InkPaid=1, REFUND_FRACTION=0.5 → floor(0.5) = 0. The
            // smallest possible rental refund is 0, never -1 or 1.
            // Adversarial: a buggy "ceil" or "round" would refund 1.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 4); // small value → cost = ceil(4*0.25*reactor) = ~1
            // Force rental cost = 1 by direct mutation if needed.
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item),
                "Setup: rent the cheap item.");
            var rental = item.GetPart<RentalPart>();
            // Force InkPaid = 1 to test floor math precisely.
            rental.InkPaid = 1;

            int inkBeforeReturn = RentalSystem.GetInk(renter);
            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, item));
            int refund = RentalSystem.GetInk(renter) - inkBeforeReturn;
            Assert.AreEqual(0, refund,
                "InkPaid=1, fraction=0.5 → floor(0.5)=0 refund.");
        }

        [Test]
        public void Adversarial_Refund_PaidThreeInk_RefundsOne_FloorMath()
        {
            // InkPaid=3, fraction=0.5 → floor(1.5) = 1.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item));
            item.GetPart<RentalPart>().InkPaid = 3;

            int inkBeforeReturn = RentalSystem.GetInk(renter);
            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, item));
            int refund = RentalSystem.GetInk(renter) - inkBeforeReturn;
            Assert.AreEqual(1, refund, "InkPaid=3 × 0.5 = 1.5, floor → 1.");
        }

        [Test]
        public void Adversarial_Refund_PaidLargeAmount_RefundsHalf()
        {
            // InkPaid=100 → 50 refund.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item));
            item.GetPart<RentalPart>().InkPaid = 100;

            int inkBeforeReturn = RentalSystem.GetInk(renter);
            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, item));
            int refund = RentalSystem.GetInk(renter) - inkBeforeReturn;
            Assert.AreEqual(50, refund);
        }

        // ════════════════════════════════════════════════════════════════
        // D. EQUIP-THEN-RETURN EDGE CASE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TryReturn_OnEquippedRental_UnequipsBeforeTransfer()
        {
            // The typical user flow: rent → equip → fight → return.
            // An equipped item lives in EquippedItems, not Objects, so
            // a naive RemoveObject would silently fail. The fix unequips
            // first. Adversarial: verify the unequip path is exercised.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            // Need an EquippablePart for unequip to work.
            item.AddPart(new EquippablePart { Slot = "Hand" });
            // Body part for the slot.
            renter.AddPart(new Body());
            renter.GetPart<Body>().SetBody(
                CavesOfOoo.Core.Anatomy.AnatomyFactory.CreateHumanoid());
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item));
            // Equip the rental.
            var hand = renter.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            renter.GetPart<InventoryPart>().EquipToBodyPart(item, hand);
            Assert.IsTrue(InventorySystem.IsEquipped(renter, item),
                "Setup: rental is equipped.");

            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, item),
                "TryReturn on equipped rental must succeed (unequip-first).");
            Assert.IsFalse(InventorySystem.IsEquipped(renter, item),
                "Item must be unequipped.");
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Has.Member(item),
                "Item must be in lessor's inventory.");
            Assert.IsNull(item.GetPart<RentalPart>(),
                "RentalPart must be stripped after successful return.");
        }

        // ════════════════════════════════════════════════════════════════
        // E. MULTI-RENTAL ISOLATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RentTwoItems_ReturnOne_OtherStaysRented()
        {
            // Rent A, rent B, return A. B's RentalPart must remain
            // untouched. Adversarial: a buggy TryReturn that scanned
            // ALL of renter's items might strip B's RentalPart too.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var itemA = CreateRentalWeapon("SwordA");
            var itemB = CreateRentalWeapon("SwordB");
            lessor.GetPart<InventoryPart>().AddObject(itemA);
            lessor.GetPart<InventoryPart>().AddObject(itemB);

            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, itemA));
            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, itemB));

            // Return A only.
            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, itemA));
            Assert.IsNull(itemA.GetPart<RentalPart>(),
                "A's RentalPart stripped.");
            Assert.IsNotNull(itemB.GetPart<RentalPart>(),
                "B's RentalPart MUST remain — only A was returned.");
            Assert.That(renter.GetPart<InventoryPart>().Objects, Has.Member(itemB),
                "B still with renter.");
        }

        // ════════════════════════════════════════════════════════════════
        // F. STACKABLE-REJECTION INVARIANT
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_StackableItem_NotRentable_PreventsAutoMergeOrphan()
        {
            // A Stacker with MaxStack > 1 means InventoryPart.AddObject
            // would auto-merge — orphaning the RentalPart on the consumed
            // entity. The IsRentable gate must reject upstream.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            item.AddPart(new StackerPart { MaxStack = 99 });
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsFalse(RentalSystem.TryRent(renter, lessor, item),
                "Stackable items must NOT be rentable — IsRentable gate "
                + "catches Stacker.MaxStack > 1.");
        }

        // ════════════════════════════════════════════════════════════════
        // G. STATE PURITY ON FULL RENT-RETURN CYCLE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FullCycle_RentReturn_NoLeakage()
        {
            // Rent + return → final state == initial state for both
            // parties (item back with lessor, no RentalPart, ink delta
            // = -cost + refund = -(cost - refund)).
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            lessor.GetPart<InventoryPart>().AddObject(item);

            int inkInitial = RentalSystem.GetInk(renter);
            int lessorInitial = lessor.GetPart<InventoryPart>().Objects.Count;
            int renterInitial = renter.GetPart<InventoryPart>().Objects.Count;

            Assert.IsTrue(RentalSystem.TryRent(renter, lessor, item));
            int rentCost = inkInitial - RentalSystem.GetInk(renter);
            Assert.IsTrue(RentalSystem.TryReturn(renter, lessor, item));

            int inkFinal = RentalSystem.GetInk(renter);
            Assert.AreEqual(lessorInitial, lessor.GetPart<InventoryPart>().Objects.Count,
                "Lessor inventory back to initial.");
            Assert.AreEqual(renterInitial, renter.GetPart<InventoryPart>().Objects.Count,
                "Renter inventory back to initial.");
            Assert.IsNull(item.GetPart<RentalPart>(),
                "RentalPart cleaned up.");

            // Net ink loss = rentCost - refund = rentCost - floor(rentCost*0.5).
            int expectedRefund = (int)System.Math.Floor(rentCost * RentalSystem.REFUND_FRACTION);
            int expectedNetLoss = rentCost - expectedRefund;
            int actualNetLoss = inkInitial - inkFinal;
            Assert.AreEqual(expectedNetLoss, actualNetLoss,
                "Net ink loss must be cost - floor(cost * REFUND_FRACTION).");
        }

        // ════════════════════════════════════════════════════════════════
        // H. NULL-SAFETY
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TryRent_NullArgs_ReturnsFalseNoCrash()
        {
            Assert.IsFalse(RentalSystem.TryRent(null, CreateLessor(), CreateRentalWeapon()));
            Assert.IsFalse(RentalSystem.TryRent(CreatePlayer(), null, CreateRentalWeapon()));
            Assert.IsFalse(RentalSystem.TryRent(CreatePlayer(), CreateLessor(), null));
        }

        [Test]
        public void Adversarial_TryReturn_NullArgs_ReturnsFalseNoCrash()
        {
            var item = CreateRentalWeapon();
            item.AddPart(new RentalPart { InkPaid = 5, LessorBlueprintName = "x" });
            Assert.IsFalse(RentalSystem.TryReturn(null, CreateLessor(), item));
            Assert.IsFalse(RentalSystem.TryReturn(CreatePlayer(), null, item));
            Assert.IsFalse(RentalSystem.TryReturn(CreatePlayer(), CreateLessor(), null));
        }

        [Test]
        public void Adversarial_GetInk_NullEntity_ReturnsZero()
        {
            Assert.AreEqual(0, RentalSystem.GetInk(null));
        }

        [Test]
        public void Adversarial_SetInk_NullEntity_NoCrash()
        {
            Assert.DoesNotThrow(() => RentalSystem.SetInk(null, 100));
        }
    }
}
