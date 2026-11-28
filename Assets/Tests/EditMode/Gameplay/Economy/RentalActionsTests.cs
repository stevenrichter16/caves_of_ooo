using System;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests rental actions and the authored Quartermaster conversation.
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

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            ConversationLoader.Reset();
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

        [Test]
        public void ReturnRentals_FindsEquippedRental()
        {
            // Cold-eye-2 Finding 1: the action's pre-fix iteration
            // walked only `inv.Objects`, missing rentals the player
            // had equipped. Selecting "Here you are" with an equipped
            // rental printed "You have no rentals to return here."
            // even though one was held.
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new InventoryPart());
            var body = new Body();
            player.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            RentalSystem.SetInk(player, 1000);

            var lessor = CreateLessor("Quartermaster");
            var item = CreateRentalWeapon("LoanerSword");
            item.AddPart(new EquippablePart { Slot = "Hand" });
            lessor.GetPart<InventoryPart>().AddObject(item);

            ConversationActions.Execute("RentItem", lessor, player, "LoanerSword");
            var hand = player.GetPart<Body>().GetParts().Find(b => b.Type == "Hand");
            player.GetPart<InventoryPart>().EquipToBodyPart(item, hand);
            Assume.That(InventorySystem.IsEquipped(player, item), Is.True);

            ConversationActions.Execute("ReturnRentals", lessor, player, "");

            Assert.That(InventorySystem.IsEquipped(player, item), Is.False);
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(item));
            Assert.That(item.GetPart<RentalPart>(), Is.Null);
        }

        private Entity CreateDialogueLessor(Entity player, string zoneId = TallyCompositionPlan.ZoneID,
            bool hasDesk = true, bool grounded = true, Zone place = null)
        {
            var lessor = CreateLessor();
            PlayerReputation.Set("Villagers", 0);
            lessor.ID = "quartermaster";
            lessor.AddPart(new ConversationPart { ConversationID = "Quartermaster_1" });
            lessor.AddPart(new BrainPart());
            lessor.AddPart(new PhysicsPart());
            if (hasDesk)
                lessor.AddPart(new RentalDeskPart());

            var zone = place ?? new Zone(zoneId);
            player.ID = "player";
            player.AddPart(new PhysicsPart());
            Assert.That(zone.AddEntity(player, 10, 10), Is.True);
            if (grounded)
                Assert.That(zone.AddEntity(lessor, 11, 10), Is.True);
            return lessor;
        }

        private Entity StockLoaner(Entity lessor, string weapon, int value)
        {
            var item = CreateRentalWeapon("Loaner" + weapon);
            item.GetPart<RenderPart>().DisplayName = "loaner " + weapon.ToLowerInvariant();
            item.GetPart<CommercePart>().Value = value;
            Assert.That(lessor.GetPart<InventoryPart>().AddObject(item), Is.True);
            return item;
        }

        private static void OpenQuartermaster(Entity lessor, Entity listener, string node)
        {
            var authored = Resources.Load<TextAsset>("Content/Conversations/Quartermaster");
            Assert.That(authored, Is.Not.Null);
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(authored.text, authored.name);
            Assert.That(ConversationManager.StartConversation(lessor, listener), Is.True);
            Assert.That(ConversationManager.SelectChoice(FindChoice(choice => choice.Target == node)), Is.True);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo(node));
        }

        private static int FindChoice(Func<ChoiceData, bool> matches) => ConversationManager.VisibleChoices
            .Select((choice, index) => (choice, index)).Single(row => matches(row.choice)).index;

        private static int LoanerChoice(string weapon) => FindChoice(choice =>
            choice.Text.IndexOf("loaner " + weapon, StringComparison.OrdinalIgnoreCase) >= 0);

        [TestCase(true)]
        [TestCase(false)]
        public void TallyWares_ShowsWalletPriceAndRefundWithoutRenting(bool hasDesk)
        {
            var player = CreatePlayer(ink: 37);
            var lessor = CreateDialogueLessor(player, hasDesk: hasDesk);
            var dagger = StockLoaner(lessor, "Dagger", value: 29);
            Assert.That(RentalSystem.GetRentalCost(dagger, player, lessor), Is.EqualTo(21));

            OpenQuartermaster(lessor, player, "Wares");

            string row = ConversationManager.VisibleChoices[LoanerChoice("dagger")].Text;
            Assert.That(ConversationManager.CurrentText, Does.Match(@"(?i)\b37\s+Ink\b"));
            Assert.That(row, Does.Match(@"(?i)\b21\s+Ink\b"));
            Assert.That(row, Does.Match(@"(?i)\b10\s+(?:Ink\s+)?(?:back|refund)\b|\brefund\D*10\b"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(37));
            Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Does.Contain(dagger));
            Assert.That(dagger.GetPart<RentalPart>(), Is.Null);
        }

        [TestCase("stock", @"(?i)\bout of stock\b")]
        [TestCase("ink", @"(?i)\b(?:4\s+(?:Ink\s+)?short|short\D*4|need\s+4\s+more)\b")]
        [TestCase("capacity", @"(?i)\b(?:too heavy|can['’]t carry|capacity|no room)\b")]
        public void TallyWares_UnavailableLoanShowsWhyAndKeepsWaresOpen(string reason, string label)
        {
            int ink = reason == "ink" ? 17 : 100;
            var player = CreatePlayer(ink);
            var lessor = CreateDialogueLessor(player);
            var pack = player.GetPart<InventoryPart>();
            if (reason == "capacity")
                pack.MaxWeight = 0;
            var dagger = reason == "stock" ? null : StockLoaner(lessor, "Dagger", value: 29);

            OpenQuartermaster(lessor, player, "Wares");
            int choice = LoanerChoice("dagger");
            Assert.That(ConversationManager.VisibleChoices[choice].Text, Does.Match(label));
            Assert.That(ConversationManager.SelectChoice(choice), Is.True);

            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Wares"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(ink));
            Assert.That(pack.Objects, Is.Empty);
            var stock = lessor.GetPart<InventoryPart>().Objects;
            if (dagger == null)
                Assert.That(stock, Is.Empty);
            else
                Assert.That(stock, Does.Contain(dagger));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TallyWares_StaleStockOrPriceRefreshesBeforeRenting(bool replaceStock)
        {
            var player = CreatePlayer(ink: 100);
            var lessor = CreateDialogueLessor(player);
            var stock = lessor.GetPart<InventoryPart>();
            var oldDagger = StockLoaner(lessor, "Dagger", value: 29);
            OpenQuartermaster(lessor, player, "Wares");
            int staleChoice = LoanerChoice("dagger");
            var dagger = oldDagger;
            if (replaceStock)
            {
                Assert.That(stock.RemoveObject(oldDagger), Is.True);
                dagger = StockLoaner(lessor, "Dagger", value: 29);
                dagger.ID = "replacement-dagger";
            }
            else
            {
                dagger.GetPart<CommercePart>().Value = 41;
            }
            int currentPrice = replaceStock ? 21 : 30;
            Assert.That(RentalSystem.GetRentalCost(dagger, player, lessor), Is.EqualTo(currentPrice));

            Assert.That(ConversationManager.SelectChoice(staleChoice), Is.True);

            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Wares"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(100));
            Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
            Assert.That(stock.Objects, Does.Contain(dagger));
            Assert.That(dagger.GetPart<RentalPart>(), Is.Null);

            int refreshedChoice = LoanerChoice("dagger");
            Assert.That(ConversationManager.VisibleChoices[refreshedChoice].Text,
                Does.Match($@"(?i)\b{currentPrice}\s+Ink\b"));
            Assert.That(ConversationManager.SelectChoice(refreshedChoice), Is.True);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(100 - currentPrice));
            Assert.That(player.GetPart<InventoryPart>().Objects, Does.Contain(dagger));
            Assert.That(dagger.GetPart<RentalPart>()?.InkPaid, Is.EqualTo(currentPrice));
            Assert.That(stock.Objects, Is.Empty);
        }

        [Test]
        public void TallyWares_AfterWalkingAway_RefusesAgainAfterRefreshing()
        {
            var player = CreatePlayer(ink: 100);
            var zone = new Zone(TallyCompositionPlan.ZoneID);
            var lessor = CreateDialogueLessor(player, place: zone);
            var dagger = StockLoaner(lessor, "Dagger", value: 29);
            OpenQuartermaster(lessor, player, "Wares");
            int staleChoice = LoanerChoice("dagger");
            Assert.That(ConversationManager.VisibleChoices[staleChoice].Text, Does.Match(@"(?i)\b21\s+Ink\b"));

            Assert.That(zone.MoveEntity(player, 20, 10), Is.True);
            AssertRefusedWithoutTransfer(staleChoice);
            ConversationManager.RefreshVisibleChoices();
            AssertRefusedWithoutTransfer(FindChoice(choice => choice.GetAction("RentItem") == "LoanerDagger"));

            void AssertRefusedWithoutTransfer(int choice)
            {
                Assert.That(ConversationManager.SelectChoice(choice), Is.True);
                Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Wares"));
                Assert.That(RentalSystem.GetInk(player), Is.EqualTo(100));
                Assert.That(player.GetPart<InventoryPart>().Objects, Is.Empty);
                Assert.That(lessor.GetPart<InventoryPart>().Objects, Is.EqualTo(new[] { dagger }));
                Assert.That(dagger.GetPart<RentalPart>(), Is.Null);
            }
        }

        [Test]
        public void TallyReturn_QuotesThePerItemRecordedRefundForCarriedAndEquippedLoans()
        {
            var player = CreatePlayer(ink: 37);
            var lessor = CreateDialogueLessor(player);
            var body = new Body();
            player.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            var pack = player.GetPart<InventoryPart>();
            var carried = CreateRentalWeapon("LoanerSpear");
            var equipped = CreateRentalWeapon("LoanerDagger");
            var foreign = CreateRentalWeapon("OtherLoan");
            var owned = CreateRentalWeapon("OwnedWeapon");
            carried.AddPart(new RentalPart { LessorBlueprintName = lessor.BlueprintName, InkPaid = 21 });
            equipped.AddPart(new RentalPart { LessorBlueprintName = lessor.BlueprintName, InkPaid = 11 });
            equipped.AddPart(new EquippablePart { Slot = "Hand" });
            foreign.AddPart(new RentalPart { LessorBlueprintName = "OtherLessor", InkPaid = 99 });
            Assert.That(pack.AddObject(carried), Is.True);
            Assert.That(pack.AddObject(equipped), Is.True);
            Assert.That(pack.AddObject(foreign), Is.True);
            Assert.That(pack.AddObject(owned), Is.True);
            Assert.That(pack.EquipToBodyPart(equipped, body.GetParts().Find(part => part.Type == "Hand")), Is.True);

            OpenQuartermaster(lessor, player, "Return");

            int returnChoice = FindChoice(choice => choice.GetAction("ReturnRentals") != null);
            Assert.That(ConversationManager.VisibleChoices[returnChoice].Text, Does.Match(@"(?i)\b15\s+Ink\b"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(37));
            Assert.That(InventorySystem.IsEquipped(player, equipped), Is.True);
            Assert.That(ConversationManager.SelectChoice(returnChoice), Is.True);

            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(52));
            Assert.That(pack.Objects, Is.EquivalentTo(new[] { foreign, owned }));
            Assert.That(pack.EquippedItems, Is.Empty);
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Is.EquivalentTo(new[] { carried, equipped }));

            Assert.That(ConversationManager.SelectChoice(FindChoice(choice => choice.Target == "Return")), Is.True);
            returnChoice = FindChoice(choice => choice.GetAction("ReturnRentals") != null);
            Assert.That(ConversationManager.VisibleChoices[returnChoice].Text,
                Does.Match(@"(?i)\b(?:no loans|no rentals|nothing to return)\b"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(52));
        }

        [Test]
        public void TallyReturn_WhenRackFitsOneLoan_QuotesAndRefundsTheFirstLoanOnly()
        {
            var player = CreatePlayer(ink: 100);
            var lessor = CreateDialogueLessor(player);
            var rack = lessor.GetPart<InventoryPart>();
            var dagger = StockLoaner(lessor, "Dagger", value: 29);
            var spear = StockLoaner(lessor, "Spear", value: 15);
            Assert.That(RentalSystem.TryRent(player, lessor, dagger), Is.True);
            Assert.That(RentalSystem.TryRent(player, lessor, spear), Is.True);
            Assert.That(dagger.GetPart<RentalPart>().InkPaid, Is.EqualTo(21));
            Assert.That(spear.GetPart<RentalPart>().InkPaid, Is.EqualTo(11));
            rack.MaxWeight = InventoryPart.GetItemWeight(dagger);
            int inkBeforeReturn = RentalSystem.GetInk(player);

            OpenQuartermaster(lessor, player, "Return");

            int returnChoice = FindChoice(choice => choice.GetAction("ReturnRentals") != null);
            string row = ConversationManager.VisibleChoices[returnChoice].Text;
            Assert.That(row, Does.Match(@"(?i)\b1\s+(?:of|out of)\s+2\b"));
            Assert.That(row, Does.Match(@"(?i)\b10\s+Ink\b"));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(inkBeforeReturn));
            Assert.That(rack.Objects, Is.Empty);

            Assert.That(ConversationManager.SelectChoice(returnChoice), Is.True);

            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(inkBeforeReturn + 10));
            Assert.That(rack.Objects, Is.EqualTo(new[] { dagger }));
            Assert.That(dagger.GetPart<RentalPart>(), Is.Null);
            Assert.That(player.GetPart<InventoryPart>().Objects, Is.EqualTo(new[] { spear }));
            Assert.That(spear.GetPart<RentalPart>()?.InkPaid, Is.EqualTo(11));
        }

        [TestCase("Overworld.10.14.1", true, true)]
        [TestCase(TallyCompositionPlan.ZoneID, false, true)]
        [TestCase(TallyCompositionPlan.ZoneID, true, false)]
        public void QuartermasterOutsidePreviewScope_PreservesAuthoredDialogue(string zoneId, bool grounded,
            bool playerListener)
        {
            var listener = CreatePlayer(ink: 37);
            if (!playerListener)
                listener.Tags.Remove("Player");
            var lessor = CreateDialogueLessor(listener, zoneId, grounded: grounded);
            StockLoaner(lessor, "Dagger", value: 29);

            OpenQuartermaster(lessor, listener, "Wares");

            Assert.That(ConversationManager.CurrentText, Is.EqualTo(ConversationManager.CurrentNode.Text));
            var authored = ConversationManager.CurrentNode.Choices.Single(choice => choice.GetAction("RentItem") == "LoanerDagger");
            Assert.That(ConversationManager.VisibleChoices[LoanerChoice("dagger")].Text, Is.EqualTo(authored.Text));
        }
    }
}
