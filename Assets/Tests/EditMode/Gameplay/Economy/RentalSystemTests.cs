using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M1 tests for the weapon rental system.
    ///
    /// Each invariant is paired with a counter-check (CLAUDE.md §3.4) so
    /// the test suite would still fire RED if the implementation were
    /// hard-coded to "always allow" or "always refund the same amount".
    ///
    /// Fixture style mirrors TradeTests.cs: in-memory entities with
    /// manual Tags / Statistics / Parts. No save-system, no Zone.
    /// </summary>
    public class RentalSystemTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ── Helpers (mirrors TradeTests) ─────────────────────────────

        private Entity CreatePlayer(int ink = 100)
        {
            var entity = new Entity { BlueprintName = "Player" };
            entity.Tags["Creature"] = "";
            entity.Tags["Player"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "you" });
            entity.AddPart(new InventoryPart());
            RentalSystem.SetInk(entity, ink);
            return entity;
        }

        private Entity CreateLessor(string blueprintName = "TestQuartermaster")
        {
            var entity = new Entity { BlueprintName = blueprintName };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = blueprintName });
            entity.AddPart(new InventoryPart());
            return entity;
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

        private Entity CreateNonRentalWeapon(string name = "Sword", int value = 100)
        {
            var item = new Entity { BlueprintName = name, ID = name + "_inst" };
            // No "Rentable" tag.
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            return item;
        }

        // ── 1. Ink wallet ─────────────────────────────────────────────

        [Test]
        public void GetInk_NewEntity_DefaultsToZero()
        {
            var e = new Entity { BlueprintName = "Whatever" };
            Assert.That(RentalSystem.GetInk(e), Is.EqualTo(0));
        }

        [Test]
        public void SetInk_RoundTrips()
        {
            var p = CreatePlayer(ink: 0);
            RentalSystem.SetInk(p, 42);
            Assert.That(RentalSystem.GetInk(p), Is.EqualTo(42));
        }

        [Test]
        public void SetInk_NegativeClampsToZero()
        {
            // Counter-check for the "round-trips" invariant: prove the
            // clamp is real, not just a paraphrase of SetInt/GetInt.
            var p = CreatePlayer(ink: 0);
            RentalSystem.SetInk(p, -10);
            Assert.That(RentalSystem.GetInk(p), Is.EqualTo(0));
        }

        [Test]
        public void AddInk_AddsDelta()
        {
            var p = CreatePlayer(ink: 25);
            RentalSystem.AddInk(p, 50);
            Assert.That(RentalSystem.GetInk(p), Is.EqualTo(75));
        }

        [Test]
        public void AddInk_NegativeBeyondZeroClampsToZero()
        {
            // Counter-check: AddInk must not allow negative balances.
            var p = CreatePlayer(ink: 10);
            RentalSystem.AddInk(p, -1000);
            Assert.That(RentalSystem.GetInk(p), Is.EqualTo(0));
        }

        // ── 2. Pricing ────────────────────────────────────────────────

        [Test]
        public void GetRentalCost_IsFractionOfBuyPrice()
        {
            var renter = CreatePlayer();
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);

            int buyPrice = TradeSystem.GetBuyPrice(item,
                TradeSystem.GetTradePerformance(renter), lessor);
            int rentalCost = RentalSystem.GetRentalCost(item, renter, lessor);

            // Cost = ceil(buyPrice * 0.25). Don't hard-code — the buy
            // price depends on Ego/faction modifiers and could shift.
            int expected = (int)System.Math.Ceiling(buyPrice * RentalSystem.RENTAL_FRACTION);
            Assert.That(rentalCost, Is.EqualTo(expected));
            Assert.That(rentalCost, Is.LessThan(buyPrice),
                "Rental should always be cheaper than purchase.");
        }

        [Test]
        public void GetRentalCost_ItemWithoutCommercePart_IsZero()
        {
            // Counter-check: a buggy impl that hard-coded "cost = max(1, ...)"
            // would fail this — caller relies on 0 to short-circuit.
            var renter = CreatePlayer();
            var lessor = CreateLessor();
            var item = new Entity { BlueprintName = "NoCommerceItem" };
            item.Tags["Rentable"] = "";
            item.AddPart(new RenderPart { DisplayName = "junk" });

            Assert.That(RentalSystem.GetRentalCost(item, renter, lessor), Is.EqualTo(0));
        }

        // ── 3. IsRentable / IsRented ──────────────────────────────────

        [Test]
        public void IsRentable_TaggedItemWithCommerce_True()
        {
            Assert.That(RentalSystem.IsRentable(CreateRentalWeapon()), Is.True);
        }

        [Test]
        public void IsRentable_MissingTag_False()
        {
            // Counter-check: tag MUST be required.
            Assert.That(RentalSystem.IsRentable(CreateNonRentalWeapon()), Is.False);
        }

        [Test]
        public void IsRentable_MissingCommerce_False()
        {
            // Counter-check: commerce part MUST be required.
            var item = new Entity { BlueprintName = "TaggedNoCommerce" };
            item.Tags["Rentable"] = "";
            item.AddPart(new RenderPart { DisplayName = "tagged" });
            Assert.That(RentalSystem.IsRentable(item), Is.False);
        }

        [Test]
        public void IsRented_DefaultFalse_TrueAfterRent()
        {
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.That(RentalSystem.IsRented(item), Is.False);
            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.True);
            Assert.That(RentalSystem.IsRented(item), Is.True);
        }

        // ── 4. TryRent ────────────────────────────────────────────────

        [Test]
        public void TryRent_HappyPath_TransfersItemAndDeductsInk()
        {
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            lessor.GetPart<InventoryPart>().AddObject(item);
            int costBefore = RentalSystem.GetRentalCost(item, renter, lessor);
            int inkBefore = RentalSystem.GetInk(renter);

            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.True);

            // Item moved
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Not.Contain(item));
            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Contain(item));
            // Ink deducted by exactly the rental cost
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(inkBefore - costBefore));
            // RentalPart records the transaction
            var rental = item.GetPart<RentalPart>();
            Assert.That(rental, Is.Not.Null);
            Assert.That(rental.InkPaid, Is.EqualTo(costBefore));
            Assert.That(rental.LessorBlueprintName, Is.EqualTo(lessor.BlueprintName));
        }

        [Test]
        public void TryRent_InsufficientInk_FailsAndDoesNotMutate()
        {
            // Counter-check: a buggy impl that mutated state before the
            // affordability check would fail this assertion.
            var renter = CreatePlayer(ink: 0);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.False);

            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Not.Contain(item));
            Assert.That(item.GetPart<RentalPart>(), Is.Null);
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(0));
        }

        [Test]
        public void TryRent_NonRentableItem_Fails()
        {
            // Counter-check for the IsRentable gate: same flow but with
            // a non-tagged item must refuse.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateNonRentalWeapon();
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.False);
            Assert.That(item.GetPart<RentalPart>(), Is.Null);
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(1000));
        }

        [Test]
        public void TryRent_AlreadyRentedItem_Fails()
        {
            // Counter-check: the "already rented" guard prevents
            // double-charging if a save-corrupted lessor inventory
            // contains a rental.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon();
            item.AddPart(new RentalPart { InkPaid = 5, LessorBlueprintName = "Other" });
            lessor.GetPart<InventoryPart>().AddObject(item);

            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.False);
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(1000));
        }

        // ── 5. TryReturn ──────────────────────────────────────────────

        [Test]
        public void TryReturn_HappyPath_RemovesItemAndRefunds()
        {
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon(value: 100);
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.True);

            int paid = item.GetPart<RentalPart>().InkPaid;
            int inkBeforeReturn = RentalSystem.GetInk(renter);
            int expectedRefund = (int)System.Math.Floor(paid * RentalSystem.REFUND_FRACTION);

            Assert.That(RentalSystem.TryReturn(renter, lessor, item), Is.True);

            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Not.Contain(item));
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(item.GetPart<RentalPart>(), Is.Null,
                "RentalPart must be removed so the item is rentable again.");
            Assert.That(RentalSystem.GetInk(renter),
                Is.EqualTo(inkBeforeReturn + expectedRefund));
        }

        [Test]
        public void TryReturn_WrongLessorBlueprint_Fails()
        {
            // Counter-check: a buggy impl that ignored
            // RentalPart.LessorBlueprintName would let the player
            // shop-hop for refunds. Make sure it doesn't.
            var renter = CreatePlayer(ink: 1000);
            var lessorA = CreateLessor("QuartermasterA");
            var lessorB = CreateLessor("QuartermasterB");
            var item = CreateRentalWeapon();
            lessorA.GetPart<InventoryPart>().AddObject(item);
            Assert.That(RentalSystem.TryRent(renter, lessorA, item), Is.True);

            int inkBefore = RentalSystem.GetInk(renter);

            Assert.That(RentalSystem.TryReturn(renter, lessorB, item), Is.False);

            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Contain(item),
                "Item must NOT have left the renter's inventory.");
            Assert.That(item.GetPart<RentalPart>(), Is.Not.Null,
                "RentalPart must NOT have been stripped.");
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(inkBefore),
                "No refund must have been issued.");
        }

        // ── 6. Anti-exploit: rented items cannot be sold ────────────

        [Test]
        public void RentedItem_CannotBeSold()
        {
            // Without the CanBeTraded veto on RentalPart, a player
            // could rent for cheap Ink, sell to a normal merchant for
            // full Drams, and pocket both. TradeSystem.SellToTrader
            // routes through TradeSystem.CanBeTraded (line 343), which
            // fires the CanBeTraded event on the item — RentalPart
            // returns false and SellToTrader bails.
            var renter = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var merchant = CreateLessor("UnrelatedMerchant");
            TradeSystem.SetDrams(merchant, 10000);
            var item = CreateRentalWeapon(value: 100);
            lessor.GetPart<InventoryPart>().AddObject(item);
            Assert.That(RentalSystem.TryRent(renter, lessor, item), Is.True);

            int dramsBefore = TradeSystem.GetDrams(renter);

            Assert.That(TradeSystem.CanBeTraded(item, renter, merchant, "Sell"), Is.False);
            Assert.That(TradeSystem.SellToTrader(renter, merchant, item), Is.False);
            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(TradeSystem.GetDrams(renter), Is.EqualTo(dramsBefore));
        }

        [Test]
        public void NonRentedItem_CanStillBeSold()
        {
            // Counter-check: the CanBeTraded veto must be specific to
            // RentalPart presence, not a blanket refusal.
            var renter = CreatePlayer();
            var merchant = CreateLessor("UnrelatedMerchant");
            var item = CreateNonRentalWeapon();

            Assert.That(TradeSystem.CanBeTraded(item, renter, merchant, "Sell"), Is.True);
        }

        [Test]
        public void TryReturn_NonRentedItem_Fails()
        {
            // Counter-check: only items with a RentalPart can be
            // returned. A normal possession must not yield a refund.
            var renter = CreatePlayer(ink: 100);
            var lessor = CreateLessor();
            var owned = CreateNonRentalWeapon();
            renter.GetPart<InventoryPart>().AddObject(owned);

            Assert.That(RentalSystem.TryReturn(renter, lessor, owned), Is.False);
            Assert.That(renter.GetPart<InventoryPart>().Objects, Does.Contain(owned));
            Assert.That(RentalSystem.GetInk(renter), Is.EqualTo(100));
        }
    }
}
