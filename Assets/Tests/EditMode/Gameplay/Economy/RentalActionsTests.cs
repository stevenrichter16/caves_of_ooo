using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2 tests for the three new conversation actions:
    /// GiveInk, RentItem, ReturnRentals.
    ///
    /// Drives ConversationActions.Execute(...) directly so the test
    /// surface matches what dialogue JSON triggers at runtime — no
    /// ConversationManager / dialogue tree needed.
    /// </summary>
    public class RentalActionsTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            ConversationActions.Reset();
            MessageLog.Clear();
        }

        // ── Helpers (mirror RentalSystemTests fixtures) ──────────────

        private Entity CreatePlayer(int ink = 0)
        {
            var p = new Entity { BlueprintName = "Player" };
            p.Tags["Creature"] = "";
            p.Tags["Player"] = "";
            p.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            p.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            p.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            p.AddPart(new RenderPart { DisplayName = "you" });
            p.AddPart(new InventoryPart());
            RentalSystem.SetInk(p, ink);
            return p;
        }

        private Entity CreateLessor(string blueprintName = "Quartermaster")
        {
            var l = new Entity { BlueprintName = blueprintName };
            l.Tags["Creature"] = "";
            l.Tags["Faction"] = "Villagers";
            l.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            l.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            l.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            l.AddPart(new RenderPart { DisplayName = blueprintName });
            l.AddPart(new InventoryPart());
            return l;
        }

        private Entity CreateRentalWeapon(string name)
        {
            var item = new Entity { BlueprintName = name, ID = name + "_inst" };
            item.Tags["Rentable"] = "";
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = 100 });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            return item;
        }

        // ── GiveInk ──────────────────────────────────────────────────

        [Test]
        public void GiveInk_ValidAmount_AddsInk()
        {
            var player = CreatePlayer(ink: 10);
            ConversationActions.Execute("GiveInk", null, player, "25");
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(35));
        }

        [Test]
        public void GiveInk_NegativeAmount_NoOp()
        {
            // Counter-check: a content typo "-50" must not silently
            // remove Ink. Mirrors the GiveDrams defensive pattern.
            var player = CreatePlayer(ink: 100);
            ConversationActions.Execute("GiveInk", null, player, "-50");
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(100));
        }

        [Test]
        public void GiveInk_ZeroAmount_NoOp()
        {
            // Counter-check: "0" is invalid input (the action exists
            // for non-trivial grants).
            var player = CreatePlayer(ink: 7);
            ConversationActions.Execute("GiveInk", null, player, "0");
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(7));
        }

        [Test]
        public void GiveInk_NonNumericArg_NoOp()
        {
            var player = CreatePlayer(ink: 5);
            ConversationActions.Execute("GiveInk", null, player, "lots");
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(5));
        }

        // ── RentItem ─────────────────────────────────────────────────

        [Test]
        public void RentItem_StockPresent_RentsAndDeductsInk()
        {
            var player = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon("RentalDagger");
            lessor.GetPart<InventoryPart>().AddObject(item);

            ConversationActions.Execute("RentItem", lessor, player, "RentalDagger");

            Assert.That(player.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(item.GetPart<RentalPart>(), Is.Not.Null);
            Assert.That(RentalSystem.GetInk(player), Is.LessThan(1000));
        }

        [Test]
        public void RentItem_BlueprintNotInStock_NoOp()
        {
            // Counter-check: requesting a blueprint the lessor doesn't
            // have must not deduct Ink or transfer anything.
            var player = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon("RentalDagger");
            lessor.GetPart<InventoryPart>().AddObject(item);

            ConversationActions.Execute("RentItem", lessor, player, "NotInStock");

            Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(1000));
        }

        [Test]
        public void RentItem_EmptyArg_NoOp()
        {
            var player = CreatePlayer(ink: 1000);
            var lessor = CreateLessor();
            ConversationActions.Execute("RentItem", lessor, player, "");
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(1000));
        }

        [Test]
        public void RentItem_PlayerCannotAfford_NoTransfer()
        {
            // Counter-check: when RentalSystem.TryRent rejects on Ink,
            // the action surface must not mutate state either.
            var player = CreatePlayer(ink: 0);
            var lessor = CreateLessor();
            var item = CreateRentalWeapon("RentalDagger");
            lessor.GetPart<InventoryPart>().AddObject(item);

            ConversationActions.Execute("RentItem", lessor, player, "RentalDagger");

            Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(item.GetPart<RentalPart>(), Is.Null);
        }

        // ── ReturnRentals ────────────────────────────────────────────

        [Test]
        public void ReturnRentals_HappyPath_ReturnsAllMatching()
        {
            var player = CreatePlayer(ink: 1000);
            var lessor = CreateLessor("Quartermaster");
            var d = CreateRentalWeapon("RentalDagger");
            var s = CreateRentalWeapon("RentalSpear");
            lessor.GetPart<InventoryPart>().AddObject(d);
            lessor.GetPart<InventoryPart>().AddObject(s);

            ConversationActions.Execute("RentItem", lessor, player, "RentalDagger");
            ConversationActions.Execute("RentItem", lessor, player, "RentalSpear");
            int inkBeforeReturn = RentalSystem.GetInk(player);

            ConversationActions.Execute("ReturnRentals", lessor, player, "");

            Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
            Assert.That(lessor.GetPart<InventoryPart>().Objects.Count, Is.EqualTo(2));
            Assert.That(RentalSystem.GetInk(player), Is.GreaterThan(inkBeforeReturn));
        }

        [Test]
        public void ReturnRentals_DoesNotTouchOtherLessorsRentals()
        {
            // Counter-check for the LessorBlueprintName match in
            // ReturnRentals: another lessor's rental in the same
            // inventory must not be returned to the speaker.
            var player = CreatePlayer(ink: 1000);
            var quartermasterA = CreateLessor("QuartermasterA");
            var quartermasterB = CreateLessor("QuartermasterB");
            var rentedFromA = CreateRentalWeapon("Item_A");
            var rentedFromB = CreateRentalWeapon("Item_B");
            quartermasterA.GetPart<InventoryPart>().AddObject(rentedFromA);
            quartermasterB.GetPart<InventoryPart>().AddObject(rentedFromB);

            ConversationActions.Execute("RentItem", quartermasterA, player, "Item_A");
            ConversationActions.Execute("RentItem", quartermasterB, player, "Item_B");

            ConversationActions.Execute("ReturnRentals", quartermasterA, player, "");

            // A's item went back to A. B's item stayed with player.
            Assert.That(quartermasterA.GetPart<InventoryPart>().Objects, Does.Contain(rentedFromA));
            Assert.That(player.GetPart<InventoryPart>().Objects, Does.Contain(rentedFromB),
                "Player must still hold B's rental.");
            Assert.That(quartermasterB.GetPart<InventoryPart>().Objects, Is.Empty,
                "B's rental must NOT have been delivered to A.");
        }

        [Test]
        public void ReturnRentals_DoesNotTouchNonRentedItems()
        {
            // Counter-check: a normally-owned item in the player's
            // inventory must not be removed by ReturnRentals.
            var player = CreatePlayer();
            var lessor = CreateLessor();
            var owned = new Entity { BlueprintName = "MyOwnSword", ID = "myown" };
            owned.AddPart(new RenderPart { DisplayName = "my own sword" });
            owned.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            player.GetPart<InventoryPart>().AddObject(owned);

            ConversationActions.Execute("ReturnRentals", lessor, player, "");

            Assert.That(player.GetPart<InventoryPart>().Objects, Does.Contain(owned));
        }

        [Test]
        public void ReturnRentals_NoRentals_StillSafeToCall()
        {
            // Defensive: pressing the dialogue choice when the player
            // hasn't rented anything must not throw.
            var player = CreatePlayer();
            var lessor = CreateLessor();
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("ReturnRentals", lessor, player, ""));
        }
    }
}
