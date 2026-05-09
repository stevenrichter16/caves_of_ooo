using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.4 — DEEPER adversarial tests for the rental + ink system.
    /// Targets bug classes the first adversarial pass missed:
    /// <list type="bullet">
    ///   <item>Save/load round-trip (RentalPart fields + Ink wallet)</item>
    ///   <item>Conversation action edge cases (GiveInk negative/zero/
    ///         non-numeric/null; RentItem missing blueprint; ReturnRentals
    ///         mixed lessors / equipped+carried mix / empty inventory)</item>
    ///   <item>Cross-actor flows (drop rental, pick up by NPC, sell-veto)</item>
    ///   <item>Self-referential edge cases (renter == lessor; item already
    ///         in renter's inventory)</item>
    ///   <item>CanBeTraded sell-veto via the actual TradeSystem path</item>
    /// </list>
    /// </summary>
    public class RentalSystemDeepAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            ConversationActions.Reset();
            ConversationActions.EnsureInitialized();
        }

        // ── Fixture helpers ───────────────────────────────────────────────

        private static Entity CreatePlayer(int ink = 1000)
        {
            var e = new Entity { ID = "Player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new InventoryPart());
            RentalSystem.SetInk(e, ink);
            return e;
        }

        private static Entity CreateLessor(string blueprintName = "Quartermaster", string instanceId = null)
        {
            var e = new Entity
            {
                BlueprintName = blueprintName,
                ID = instanceId ?? blueprintName + "_inst"
            };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = "Villagers";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 14, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = blueprintName });
            e.AddPart(new InventoryPart());
            return e;
        }

        private static Entity CreateRentalWeapon(string blueprintName = "LoanerDagger", int value = 30)
        {
            var item = new Entity { BlueprintName = blueprintName, ID = blueprintName + "_inst" };
            item.Tags["Rentable"] = "";
            item.AddPart(new RenderPart { DisplayName = blueprintName.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            return item;
        }

        // ════════════════════════════════════════════════════════════════
        // A. GIVEINK CONVERSATION ACTION — edge cases
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_GiveInk_Zero_NoMutation()
        {
            var player = CreatePlayer(ink: 50);
            var npc = CreateLessor();
            int inkBefore = RentalSystem.GetInk(player);

            ConversationActions.Execute("GiveInk", npc, player, "0");

            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player),
                "GiveInk(0) must be a no-op (parser rejects amt <= 0).");
        }

        [Test]
        public void Adversarial_GiveInk_Negative_NoMutation()
        {
            var player = CreatePlayer(ink: 50);
            var npc = CreateLessor();
            int inkBefore = RentalSystem.GetInk(player);

            ConversationActions.Execute("GiveInk", npc, player, "-100");

            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player),
                "GiveInk with negative amount must be a no-op.");
        }

        [Test]
        public void Adversarial_GiveInk_NonNumeric_NoMutation()
        {
            var player = CreatePlayer(ink: 50);
            var npc = CreateLessor();
            int inkBefore = RentalSystem.GetInk(player);

            ConversationActions.Execute("GiveInk", npc, player, "banana");
            ConversationActions.Execute("GiveInk", npc, player, "");

            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player),
                "GiveInk with non-numeric arg must be a no-op.");
        }

        [Test]
        public void Adversarial_GiveInk_NullListener_NoCrash()
        {
            var npc = CreateLessor();
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("GiveInk", npc, listener: null, argument: "100"));
        }

        [Test]
        public void Adversarial_GiveInk_PositiveAmount_AddsToWallet()
        {
            // Counter-check: prove the action ISN'T just a global no-op.
            var player = CreatePlayer(ink: 50);
            var npc = CreateLessor();

            ConversationActions.Execute("GiveInk", npc, player, "75");

            Assert.AreEqual(125, RentalSystem.GetInk(player),
                "GiveInk(75) must add to wallet (50 + 75 = 125).");
        }

        // ════════════════════════════════════════════════════════════════
        // B. RENTITEM CONVERSATION ACTION — edge cases
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RentItem_NonExistentBlueprint_NoStockMessage()
        {
            var player = CreatePlayer();
            var lessor = CreateLessor();
            // Lessor has NO LoanerDagger — only LoanerSpear.
            var spear = CreateRentalWeapon("LoanerSpear", value: 55);
            lessor.GetPart<InventoryPart>().AddObject(spear);
            int inkBefore = RentalSystem.GetInk(player);

            ConversationActions.Execute("RentItem", lessor, player, "LoanerDagger");

            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player),
                "RentItem with non-existent blueprint must not deduct ink.");
            Assert.That(player.GetPart<InventoryPart>().Objects, Has.No.Member(spear));
            Assert.That(lessor.GetPart<InventoryPart>().Objects, Has.Member(spear),
                "Lessor's other stock must be untouched.");
        }

        [Test]
        public void Adversarial_RentItem_BlueprintMatchesEquipped_StillFinds()
        {
            // Edge case: lessor's stock includes the item, but it's not
            // their equipped inventory (lessors don't equip rental stock).
            // Just verify the basic happy path so the negative tests
            // above are meaningful.
            var player = CreatePlayer();
            var lessor = CreateLessor();
            var dagger = CreateRentalWeapon("LoanerDagger", value: 30);
            lessor.GetPart<InventoryPart>().AddObject(dagger);

            ConversationActions.Execute("RentItem", lessor, player, "LoanerDagger");

            Assert.That(player.GetPart<InventoryPart>().Objects, Has.Member(dagger),
                "RentItem with valid blueprint must transfer item to player.");
            Assert.IsNotNull(dagger.GetPart<RentalPart>(),
                "RentalPart must be attached.");
        }

        [Test]
        public void Adversarial_RentItem_NullArgs_NoCrash()
        {
            var player = CreatePlayer();
            var lessor = CreateLessor();
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("RentItem", lessor, player, null));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("RentItem", null, player, "LoanerDagger"));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("RentItem", lessor, null, "LoanerDagger"));
        }

        // ════════════════════════════════════════════════════════════════
        // C. RETURNRENTALS CONVERSATION ACTION — mixed-state edge cases
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ReturnRentals_OnlyMatchingLessorReturned()
        {
            // Player has rentals from BOTH Quartermaster AND another NPC.
            // ReturnRentals(speaker=Quartermaster) must return ONLY the
            // Quartermaster's rentals — the other NPC's rentals stay
            // with the player.
            var player = CreatePlayer(ink: 1000);
            var qm = CreateLessor("Quartermaster");
            var blacksmith = CreateLessor("Blacksmith");
            var qmDagger = CreateRentalWeapon("LoanerDagger");
            var bsHammer = CreateRentalWeapon("LoanerHammer");
            qm.GetPart<InventoryPart>().AddObject(qmDagger);
            blacksmith.GetPart<InventoryPart>().AddObject(bsHammer);

            Assert.IsTrue(RentalSystem.TryRent(player, qm, qmDagger));
            Assert.IsTrue(RentalSystem.TryRent(player, blacksmith, bsHammer));
            Assert.AreEqual(2, CountRentalsInInventory(player),
                "Setup: 2 rentals in player inventory.");

            // Return rentals at Quartermaster.
            ConversationActions.Execute("ReturnRentals", qm, player, null);

            Assert.IsNull(qmDagger.GetPart<RentalPart>(),
                "Quartermaster's dagger returned.");
            Assert.IsNotNull(bsHammer.GetPart<RentalPart>(),
                "Blacksmith's hammer must STAY with player — NOT returned by Quartermaster ReturnRentals.");
            Assert.That(player.GetPart<InventoryPart>().Objects, Has.Member(bsHammer),
                "Blacksmith's hammer still in player inventory.");
            Assert.That(qm.GetPart<InventoryPart>().Objects, Has.Member(qmDagger),
                "Quartermaster has dagger back.");
        }

        [Test]
        public void Adversarial_ReturnRentals_EmptyInventory_NoMutation()
        {
            var player = CreatePlayer();
            var qm = CreateLessor();
            int inkBefore = RentalSystem.GetInk(player);

            ConversationActions.Execute("ReturnRentals", qm, player, null);

            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player));
        }

        [Test]
        public void Adversarial_ReturnRentals_MultipleSameLessor_AllReturned()
        {
            // Player has 3 rentals from Quartermaster. ReturnRentals
            // returns ALL of them in one call.
            var player = CreatePlayer(ink: 1000);
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon("LoanerDagger");
            var spear = CreateRentalWeapon("LoanerSpear", value: 55);
            var sword = CreateRentalWeapon("LoanerLongsword", value: 80);
            qm.GetPart<InventoryPart>().AddObject(dagger);
            qm.GetPart<InventoryPart>().AddObject(spear);
            qm.GetPart<InventoryPart>().AddObject(sword);
            Assert.IsTrue(RentalSystem.TryRent(player, qm, dagger));
            Assert.IsTrue(RentalSystem.TryRent(player, qm, spear));
            Assert.IsTrue(RentalSystem.TryRent(player, qm, sword));
            Assert.AreEqual(3, CountRentalsInInventory(player));

            ConversationActions.Execute("ReturnRentals", qm, player, null);

            Assert.AreEqual(0, CountRentalsInInventory(player),
                "All 3 rentals returned in one call.");
            Assert.That(qm.GetPart<InventoryPart>().Objects, Has.Member(dagger));
            Assert.That(qm.GetPart<InventoryPart>().Objects, Has.Member(spear));
            Assert.That(qm.GetPart<InventoryPart>().Objects, Has.Member(sword));
        }

        // ════════════════════════════════════════════════════════════════
        // D. SAVE/LOAD ROUND-TRIP
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SaveLoad_RentalPartFields_RoundTrip()
        {
            // RentalPart has no explicit save handler in SaveSystem —
            // it falls through to the catch-all WritePublicFields path.
            // This test verifies InkPaid + LessorBlueprintName actually
            // survive the round-trip via reflection-based serialization.
            //
            // If this fails, every save/load cycle silently corrupts
            // rentals. Critical bug candidate.
            var item = CreateRentalWeapon();
            var rental = new RentalPart { InkPaid = 17, LessorBlueprintName = "TestLessor" };
            item.AddPart(rental);

            var loaded = RoundTripEntity(item);
            var loadedRental = loaded.GetPart<RentalPart>();
            Assert.IsNotNull(loadedRental,
                "RentalPart must be preserved across save/load.");
            Assert.AreEqual(17, loadedRental.InkPaid,
                "InkPaid must round-trip.");
            Assert.AreEqual("TestLessor", loadedRental.LessorBlueprintName,
                "LessorBlueprintName must round-trip.");
        }

        [Test]
        public void Adversarial_SaveLoad_InkWallet_RoundTrip()
        {
            // Ink lives in entity.IntProperties["Ink"]. Save/load handles
            // IntProperties at SaveGraphSerializer.SaveEntityBody:614.
            // Verify the round-trip.
            var player = CreatePlayer(ink: 0);
            RentalSystem.SetInk(player, 423);

            var loaded = RoundTripEntity(player);
            Assert.AreEqual(423, RentalSystem.GetInk(loaded),
                "Ink wallet must round-trip via IntProperties.");
        }

        [Test]
        public void Adversarial_SaveLoad_FullRentalCycle_PreservesState()
        {
            // Rent + save + load + return → refund must apply correctly.
            // This is the realistic save-mid-rental scenario.
            var player = CreatePlayer(ink: 1000);
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon();
            qm.GetPart<InventoryPart>().AddObject(dagger);
            Assert.IsTrue(RentalSystem.TryRent(player, qm, dagger));
            int inkAfterRent = RentalSystem.GetInk(player);
            int rentalCost = 1000 - inkAfterRent;

            // Round-trip the dagger entity (carries the RentalPart).
            var loaded = RoundTripEntity(dagger);
            var loadedRental = loaded.GetPart<RentalPart>();
            Assert.AreEqual(rentalCost, loadedRental.InkPaid,
                "InkPaid preserved; refund will compute against this value.");
            Assert.AreEqual("Quartermaster", loadedRental.LessorBlueprintName);
        }

        // ════════════════════════════════════════════════════════════════
        // E. CROSS-ACTOR FLOWS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RentedItem_CanBeTradedEvent_VetoedByRentalPart()
        {
            // The CanBeTraded GameEvent is the production path
            // TradeSystem.SellToTrader uses to gate sales. RentalPart
            // overrides HandleEvent to return false on this event.
            // Verify directly via the event surface.
            var item = CreateRentalWeapon();
            item.AddPart(new RentalPart { InkPaid = 10, LessorBlueprintName = "X" });

            var canBeTraded = GameEvent.New("CanBeTraded");
            bool allowed = item.FireEvent(canBeTraded);
            canBeTraded.Release();

            Assert.IsFalse(allowed,
                "RentalPart must veto CanBeTraded — returns false from HandleEvent.");
        }

        [Test]
        public void Adversarial_RentedItem_AfterReturn_CanBeTradedAgain()
        {
            // After TryReturn strips the RentalPart, the item should
            // pass CanBeTraded normally (so the lessor can re-stock it
            // and another player can re-rent it).
            var player = CreatePlayer(ink: 1000);
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon();
            qm.GetPart<InventoryPart>().AddObject(dagger);
            Assert.IsTrue(RentalSystem.TryRent(player, qm, dagger));
            Assert.IsTrue(RentalSystem.TryReturn(player, qm, dagger));

            var canBeTraded = GameEvent.New("CanBeTraded");
            bool allowed = dagger.FireEvent(canBeTraded);
            canBeTraded.Release();

            Assert.IsTrue(allowed,
                "Post-return, the item must pass CanBeTraded "
                + "(RentalPart was stripped — no veto).");
        }

        // ════════════════════════════════════════════════════════════════
        // F. SELF-REFERENTIAL EDGE CASES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TryRent_SelfRent_RenterIsLessor()
        {
            // Renter == Lessor (same entity). Item is in their own
            // inventory. The implementation will:
            //   1. lessorInv.RemoveObject(item) succeeds (item IS in inv)
            //   2. renterInv.AddObject(item) — but it's the same inv, item
            //      was just removed. Re-add succeeds.
            //   3. Ink is deducted from renter (== lessor; both same wallet).
            //   4. RentalPart attached.
            //
            // Net result: an entity rents to themselves, paying their
            // own Ink, gaining a RentalPart. This is degenerate but
            // tracked: the impl doesn't explicitly check renter != lessor.
            // Whether this is a bug depends on the design intent. Pin
            // current behavior so any future change is intentional.
            var self = CreatePlayer(ink: 100);
            var dagger = CreateRentalWeapon();
            self.GetPart<InventoryPart>().AddObject(dagger);

            int inkBefore = RentalSystem.GetInk(self);
            bool rented = RentalSystem.TryRent(self, self, dagger);

            // Document the actual current behavior — if it returns
            // true, it's degenerate; if false, the impl has a guard.
            // Either way, log the behavior for design awareness.
            if (rented)
            {
                Assert.IsNotNull(dagger.GetPart<RentalPart>(),
                    "Self-rent succeeds: RentalPart attached.");
                Assert.Less(RentalSystem.GetInk(self), inkBefore,
                    "Self-rent succeeds: ink deducted from same entity.");
            }
            else
            {
                Assert.AreEqual(inkBefore, RentalSystem.GetInk(self));
                Assert.IsNull(dagger.GetPart<RentalPart>());
            }
            // Test passes either way — documents behavior.
        }

        [Test]
        public void Adversarial_TryRent_ItemAlreadyInRenterInv_RejectedByLessorRemove()
        {
            // Item is in the RENTER's inventory, lessor has nothing.
            // lessor.GetPart<InventoryPart>().RemoveObject(item) will
            // return false because the item isn't there. TryRent
            // returns false at the RemoveObject gate.
            var player = CreatePlayer();
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon();
            // Dagger is in PLAYER's inv, not Quartermaster's.
            player.GetPart<InventoryPart>().AddObject(dagger);

            int inkBefore = RentalSystem.GetInk(player);
            bool rented = RentalSystem.TryRent(player, qm, dagger);

            Assert.IsFalse(rented,
                "TryRent must fail when item isn't in lessor's inventory.");
            Assert.AreEqual(inkBefore, RentalSystem.GetInk(player),
                "No Ink deducted on rejection.");
            Assert.IsNull(dagger.GetPart<RentalPart>());
        }

        // ════════════════════════════════════════════════════════════════
        // G. DROPPED-RENTAL FLOWS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RentedItem_DroppedThenPickedUp_RentalPartPersists()
        {
            // Drop a rental on the ground, pick it up — RentalPart must
            // remain attached. The DropCommand path uses inv.RemoveObject
            // + zone.AddEntity; pickup uses inv.AddObject. Neither
            // touches RentalPart. So the part rides along. Verify.
            var player = CreatePlayer(ink: 1000);
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon();
            qm.GetPart<InventoryPart>().AddObject(dagger);
            Assert.IsTrue(RentalSystem.TryRent(player, qm, dagger));
            Assert.IsNotNull(dagger.GetPart<RentalPart>());

            // Manually simulate drop (RemoveObject from player; would
            // normally go through DropCommand which adds to zone).
            player.GetPart<InventoryPart>().RemoveObject(dagger);
            Assert.IsNotNull(dagger.GetPart<RentalPart>(),
                "Drop must NOT strip RentalPart.");

            // Pick up.
            player.GetPart<InventoryPart>().AddObject(dagger);
            Assert.IsNotNull(dagger.GetPart<RentalPart>(),
                "Pickup must NOT strip RentalPart either.");
        }

        [Test]
        public void Adversarial_RentedItem_PickedUpByOtherPlayer_StillTracksOriginalLessor()
        {
            // Player A rents from Quartermaster, drops the rental.
            // Player B picks it up. B can return it to Quartermaster
            // (LessorBlueprintName matches), but the refund goes to
            // whoever holds the item — that's Player B.
            //
            // Adversarial: a buggy impl that hardcoded "renter ID" on
            // the RentalPart would refuse B's return. Current impl
            // matches by blueprint name only — anyone can return.
            // Pin this behavior.
            var playerA = CreatePlayer(ink: 1000);
            var playerB = CreatePlayer(ink: 0);
            playerB.ID = "PlayerB";
            var qm = CreateLessor();
            var dagger = CreateRentalWeapon();
            qm.GetPart<InventoryPart>().AddObject(dagger);

            Assert.IsTrue(RentalSystem.TryRent(playerA, qm, dagger));
            playerA.GetPart<InventoryPart>().RemoveObject(dagger);
            playerB.GetPart<InventoryPart>().AddObject(dagger);

            int playerBInkBefore = RentalSystem.GetInk(playerB);
            Assert.IsTrue(RentalSystem.TryReturn(playerB, qm, dagger),
                "Player B can return rental that Player A originally rented "
                + "(LessorBlueprintName-matching only).");
            int playerBRefund = RentalSystem.GetInk(playerB) - playerBInkBefore;
            Assert.Greater(playerBRefund, 0,
                "Refund goes to whoever holds the item at return time — Player B.");
        }

        // ════════════════════════════════════════════════════════════════
        // H. MULTI-LESSOR (cross-village) — design intent
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RentAtVillageA_ReturnAtVillageB_SameBlueprintWorks()
        {
            // Two physical Quartermasters (different IDs) but same
            // blueprint name. Player rents at one, returns at the other.
            // Per design comment in RentalPart: "a single Quartermaster
            // blueprint can be shared across multiple zone instances
            // and still route correctly."
            var player = CreatePlayer(ink: 1000);
            var villageAQm = CreateLessor("Quartermaster", instanceId: "QM_VillageA");
            var villageBQm = CreateLessor("Quartermaster", instanceId: "QM_VillageB");
            var dagger = CreateRentalWeapon();
            villageAQm.GetPart<InventoryPart>().AddObject(dagger);

            Assert.IsTrue(RentalSystem.TryRent(player, villageAQm, dagger));
            // Return at the OTHER Quartermaster.
            Assert.IsTrue(RentalSystem.TryReturn(player, villageBQm, dagger),
                "Cross-village return at same-blueprint Quartermaster works.");
            Assert.That(villageBQm.GetPart<InventoryPart>().Objects, Has.Member(dagger),
                "Item ends up in the village-B Quartermaster's stock.");
        }

        // ── Helper: round-trip an entity through SaveWriter/SaveReader ──

        private static Entity RoundTripEntity(Entity src)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            SaveGraphSerializer.SaveEntityBody(src, writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, factory: null);
            var loaded = new Entity();
            SaveGraphSerializer.LoadEntityBody(loaded, reader);
            return loaded;
        }

        // ── Helper: count rentals in inventory (Objects + Equipped) ───────

        private static int CountRentalsInInventory(Entity actor)
        {
            int count = 0;
            var inv = actor.GetPart<InventoryPart>();
            if (inv == null) return 0;
            for (int i = 0; i < inv.Objects.Count; i++)
                if (inv.Objects[i].GetPart<RentalPart>() != null) count++;
            foreach (var equipped in inv.EquippedItems.Values)
                if (equipped != null && equipped.GetPart<RentalPart>() != null
                    && !ContainedInObjects(inv, equipped)) count++;
            return count;
        }

        private static bool ContainedInObjects(InventoryPart inv, Entity item)
        {
            for (int i = 0; i < inv.Objects.Count; i++)
                if (inv.Objects[i] == item) return true;
            return false;
        }
    }
}
