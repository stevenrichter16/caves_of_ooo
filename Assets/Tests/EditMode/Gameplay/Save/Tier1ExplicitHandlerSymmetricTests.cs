using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.7.2 — Tier-1 explicit handlers, symmetric Save/Load contract
    /// pin. See <c>Docs/SAVE-LOAD-AUDIT.md §SL.7</c>.
    ///
    /// <para>The verification sweep classified these three handlers as ⚪
    /// (fully symmetric Save↔Load) — bug yield is low, but the contracts
    /// have never been pinned by a test, so this fixture pins them to
    /// catch any future regression silently.</para>
    ///
    /// <para><b>Targets:</b>
    /// <list type="bullet">
    ///   <item><c>BitLockerPart</c> (SaveSystem.cs:1206-1235) — bits +
    ///         known recipes via snapshot/restore.</item>
    ///   <item><c>InventoryPart</c> (SaveSystem.cs:1237-1264) —
    ///         MaxWeight + Objects + EquippedItems entity-ref dict.</item>
    ///   <item><c>StatusEffectsPart</c> (SaveSystem.cs:1161-1176) —
    ///         effect count + each effect via SaveEffect/LoadEffect
    ///         (Effect-shape contract pinned by SL.6).</item>
    /// </list>
    /// </para>
    /// </summary>
    public class Tier1ExplicitHandlerSymmetricTests
    {
        // ── BitLockerPart ─────────────────────────────────────────

        [Test]
        public void BitLockerPart_Empty_RoundTrips()
        {
            // Fresh BitLockerPart with no bits and no recipes — round-trip
            // must produce the same empty state, NOT leave stale data.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new BitLockerPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var bits = loaded.GetPart<BitLockerPart>();
            Assert.IsNotNull(bits, "BitLockerPart itself round-trips.");
            Assert.AreEqual(0, bits.GetBitsSnapshot().Count,
                "Fresh part has no bits.");
            Assert.AreEqual(0, bits.GetKnownRecipes().Count,
                "Fresh part has no known recipes.");
        }

        [Test]
        public void BitLockerPart_BitCounts_RoundTrip()
        {
            // Three bit kinds with distinct counts. Pin that the
            // (char, int) pairs survive intact — catches a save-order
            // bug that would scramble the dict.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var bits = new BitLockerPart();
            actor.AddPart(bits);
            bits.AddBits("AAA"); // 3 of 'A'
            bits.AddBits("BB");  // 2 of 'B'
            bits.AddBits("C");   // 1 of 'C'

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedBits = loaded.GetPart<BitLockerPart>();
            Assert.AreEqual(3, loadedBits.GetBitCount('A'));
            Assert.AreEqual(2, loadedBits.GetBitCount('B'));
            Assert.AreEqual(1, loadedBits.GetBitCount('C'));
            Assert.AreEqual(0, loadedBits.GetBitCount('Z'),
                "Counter-check: a bit kind never deposited returns 0.");
        }

        [Test]
        public void BitLockerPart_KnownRecipes_RoundTrip()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var bits = new BitLockerPart();
            actor.AddPart(bits);
            bits.LearnRecipe("recipe_iron_helm");
            bits.LearnRecipe("recipe_steel_blade");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedBits = loaded.GetPart<BitLockerPart>();
            Assert.IsTrue(loadedBits.KnowsRecipe("recipe_iron_helm"));
            Assert.IsTrue(loadedBits.KnowsRecipe("recipe_steel_blade"));
            Assert.IsFalse(loadedBits.KnowsRecipe("recipe_never_learned"),
                "Counter-check: an unlearned recipe must NOT be reported known.");
        }

        [Test]
        public void BitLockerPart_BitsAndRecipes_CoSurvive()
        {
            // Adversarial: bits are written first, recipes second.
            // A future refactor that swaps the order would silently
            // corrupt both. Pin the co-survival contract.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var bits = new BitLockerPart();
            actor.AddPart(bits);
            bits.AddBits("XY");
            bits.LearnRecipe("alpha");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor)
                .GetPart<BitLockerPart>();
            Assert.AreEqual(1, loaded.GetBitCount('X'));
            Assert.AreEqual(1, loaded.GetBitCount('Y'));
            Assert.IsTrue(loaded.KnowsRecipe("alpha"));
        }

        // ── InventoryPart ─────────────────────────────────────────

        [Test]
        public void InventoryPart_Empty_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new InventoryPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var inv = loaded.GetPart<InventoryPart>();
            Assert.IsNotNull(inv);
            Assert.AreEqual(0, inv.Objects.Count);
            Assert.AreEqual(0, inv.EquippedItems.Count);
        }

        [Test]
        public void InventoryPart_MaxWeight_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            var inv = new InventoryPart { MaxWeight = 240 };
            actor.AddPart(inv);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(240, loaded.GetPart<InventoryPart>().MaxWeight,
                "MaxWeight is the first field written; pin it survives "
                + "the read-order at SaveSystem.cs:1254.");
        }

        [Test]
        public void InventoryPart_NegativeMaxWeight_RoundTripsAsSentinel()
        {
            // -1 is the "unbounded" sentinel (default value). Pin that
            // it round-trips as -1, NOT as 0 or some other value.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new InventoryPart()); // default MaxWeight = -1

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(-1, loaded.GetPart<InventoryPart>().MaxWeight);
        }

        [Test]
        public void InventoryPart_Objects_RoundTrip()
        {
            // Objects is List<Entity> — entity refs go through
            // WriteEntityReference, so round-trip via TokenGraph helper
            // queues their bodies for a full restoration.
            var actor = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            actor.AddPart(inv);

            var item1 = new Entity { ID = "item-1", BlueprintName = "Sword" };
            var item2 = new Entity { ID = "item-2", BlueprintName = "Shield" };
            inv.Objects.Add(item1);
            inv.Objects.Add(item2);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedInv = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(2, loadedInv.Objects.Count,
                "Object count survives.");
            Assert.AreEqual("item-1", loadedInv.Objects[0]?.ID,
                "Item order preserved (Objects is a List, not a Set).");
            Assert.AreEqual("item-2", loadedInv.Objects[1]?.ID);
            Assert.AreEqual("Sword", loadedInv.Objects[0]?.BlueprintName,
                "Item bodies queued via WriteEntityReference + flushed.");
        }

        [Test]
        public void InventoryPart_EquippedItems_Slots_RoundTrip()
        {
            var actor = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            actor.AddPart(inv);

            var helm = new Entity { ID = "helm", BlueprintName = "IronHelm" };
            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            inv.EquippedItems["Head"] = helm;
            inv.EquippedItems["Hand"] = sword;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedInv = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(2, loadedInv.EquippedItems.Count);
            Assert.IsTrue(loadedInv.EquippedItems.ContainsKey("Head"),
                "Slot key 'Head' round-trips.");
            Assert.IsTrue(loadedInv.EquippedItems.ContainsKey("Hand"));
            Assert.AreEqual("helm", loadedInv.EquippedItems["Head"]?.ID);
            Assert.AreEqual("sword", loadedInv.EquippedItems["Hand"]?.ID);
        }

        [Test]
        public void InventoryPart_OneInventoryItemReferencedAsEquipped_BothSurvive()
        {
            // Adversarial: the same Entity in BOTH Objects[] AND
            // EquippedItems[]. A buggy load that re-resolves the
            // entity twice could create two distinct loaded objects.
            // Pin: the same logical item appears in both, with the
            // same ID.
            var actor = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            actor.AddPart(inv);

            var item = new Entity { ID = "shared", BlueprintName = "Item" };
            inv.Objects.Add(item);
            inv.EquippedItems["Hand"] = item;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedInv = loaded.GetPart<InventoryPart>();
            Assert.AreEqual("shared", loadedInv.Objects[0]?.ID);
            Assert.AreEqual("shared", loadedInv.EquippedItems["Hand"]?.ID,
                "Same logical item in Objects + EquippedItems must round-trip "
                + "with the same ID — same Entity in the saved graph.");
        }

        // ── StatusEffectsPart ─────────────────────────────────────

        [Test]
        public void StatusEffectsPart_Empty_RoundTrips()
        {
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new StatusEffectsPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var fx = loaded.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(fx);
            Assert.IsFalse(fx.HasEffect<RootedEffect>(),
                "Counter-check: a fresh part has no effects.");
        }

        [Test]
        public void StatusEffectsPart_MultipleEffects_AllRoundTrip()
        {
            // Pin that the count + per-effect Save/Load loops mirror
            // exactly. A buggy load that miscounts would lose effects
            // silently.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new RootedEffect(duration: 4));
            actor.ForceApplyEffect(new StunnedEffect(duration: 2));
            actor.ForceApplyEffect(new ConfusedEffect(duration: 3));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsTrue(loaded.HasEffect<RootedEffect>(),  "Rooted survives.");
            Assert.IsTrue(loaded.HasEffect<StunnedEffect>(), "Stunned survives.");
            Assert.IsTrue(loaded.HasEffect<ConfusedEffect>(),"Confused survives.");
            Assert.AreEqual(4, loaded.GetEffect<RootedEffect>().Duration);
            Assert.AreEqual(2, loaded.GetEffect<StunnedEffect>().Duration);
            Assert.AreEqual(3, loaded.GetEffect<ConfusedEffect>().Duration);
        }
    }
}
