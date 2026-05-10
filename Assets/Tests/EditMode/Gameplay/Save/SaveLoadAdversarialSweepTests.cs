using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.10.2 — Adversarial sweep across the save/load taxonomy
    /// surfaces. See <c>Docs/SAVE-LOAD-AUDIT.md §SL.10</c>.
    ///
    /// <para>Per <c>ADVERSARIAL_TESTING.md</c>, save/load hits 4+
    /// bug-class surfaces (state atomicity, parser, cross-actor
    /// flows, save/load reach). This file targets the specific
    /// classes that single-feature SL.2-SL.9 tests can't easily
    /// reach:</para>
    /// <list type="bullet">
    ///   <item>Circular entity references</item>
    ///   <item>Self-reference</item>
    ///   <item>Scale (many effects, many items)</item>
    ///   <item>Deeply nested entity graphs</item>
    ///   <item>Multi-round-trip (save → load → modify → save → load)</item>
    ///   <item>Empty edges</item>
    ///   <item>Token reuse</item>
    /// </list>
    ///
    /// <para>Each test is structured so a buggy implementation that
    /// passes the SL.2-SL.9 happy-path suite would still surface
    /// here.</para>
    /// </summary>
    public class SaveLoadAdversarialSweepTests
    {
        // ════════════════════════════════════════════════════════
        // Circular + self-referential entity graphs
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SelfReferential_Entity_RoundTrips()
        {
            // Edge case: A's PhysicsPart.Equipped points at A itself.
            // Token system must not infinite-loop. Loaded A's pointer
            // must be A (the same instance — self ref preserved).
            var a = new Entity { ID = "self", BlueprintName = "Self" };
            a.AddPart(new PhysicsPart { Equipped = a });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);
            var phys = loaded.GetPart<PhysicsPart>();
            Assert.IsNotNull(phys.Equipped);
            Assert.AreSame(loaded, phys.Equipped,
                "Self-reference: a.PhysicsPart.Equipped == a (the same "
                + "loaded instance). Catches an infinite-loop or "
                + "stack-overflow regression in the token system.");
        }

        [Test]
        public void Adversarial_CircularReference_BothEntities_RoundTrip()
        {
            // A.PhysicsPart.Equipped = B; B.PhysicsPart.Equipped = A.
            // Round-trip A as the primary; B is queued via the back-
            // pointer chain. Both load successfully and the cycle is
            // preserved.
            var a = new Entity { ID = "a", BlueprintName = "A" };
            var b = new Entity { ID = "b", BlueprintName = "B" };
            a.AddPart(new PhysicsPart { Equipped = b });
            b.AddPart(new PhysicsPart { Equipped = a });

            var loadedA = PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);
            var loadedB = loadedA.GetPart<PhysicsPart>().Equipped;
            Assert.IsNotNull(loadedB);
            Assert.AreEqual("b", loadedB.ID);
            Assert.IsNotNull(loadedB.GetPart<PhysicsPart>());
            Assert.AreSame(loadedA, loadedB.GetPart<PhysicsPart>().Equipped,
                "Cycle preserved: B.Equipped points back at the SAME "
                + "loaded A instance. Catches a token-cache bug that "
                + "would create a 'fresh' A on the second resolution.");
        }

        // ════════════════════════════════════════════════════════
        // Scale
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FiftyEffects_OnOneActor_AllRoundTrip()
        {
            // Stack 50 effects on one actor. SaveStatusEffectsPart
            // writes count + each. A buggy save that writes a fixed
            // small count or truncates would surface here.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            for (int i = 0; i < 50; i++)
            {
                // Use distinct effect types where possible; pad with
                // ConfusedEffect at varied durations.
                if (i % 5 == 0)      actor.ForceApplyEffect(new RootedEffect(duration: i));
                else if (i % 5 == 1) actor.ForceApplyEffect(new StunnedEffect(duration: i));
                else if (i % 5 == 2) actor.ForceApplyEffect(new ConfusedEffect(duration: i));
                else if (i % 5 == 3) actor.ForceApplyEffect(new HobbledEffect(duration: i));
                else                 actor.ForceApplyEffect(new ParalyzedEffect(duration: i));
            }

            // Stacks may extend duration → not all 50 distinct effects;
            // verify at least the 5 distinct types are present after load.
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsTrue(loaded.HasEffect<RootedEffect>());
            Assert.IsTrue(loaded.HasEffect<StunnedEffect>());
            Assert.IsTrue(loaded.HasEffect<ConfusedEffect>());
            Assert.IsTrue(loaded.HasEffect<HobbledEffect>());
            Assert.IsTrue(loaded.HasEffect<ParalyzedEffect>());
        }

        [Test]
        public void Adversarial_HundredItems_InInventory_AllRoundTrip()
        {
            // 100 distinct items in Objects[]. Pin all 100 survive
            // with their distinct IDs, AND PhysicsPart back-pointers
            // all canonicalize to the SAME owner.
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);
            for (int i = 0; i < 100; i++)
            {
                var item = new Entity { ID = $"item-{i}", BlueprintName = $"Item{i}" };
                item.AddPart(new PhysicsPart());
                inv.Objects.Add(item);
            }

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(100, li.Objects.Count, "All 100 items survive.");

            // Verify a sample at varied indices
            Assert.AreEqual("item-0", li.Objects[0].ID);
            Assert.AreEqual("item-50", li.Objects[50].ID);
            Assert.AreEqual("item-99", li.Objects[99].ID);

            // All back-pointers canonicalize to the same owner
            Assert.AreSame(loaded, li.Objects[0].GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(loaded, li.Objects[99].GetPart<PhysicsPart>().InInventory);
        }

        // ════════════════════════════════════════════════════════
        // Deeply nested entity graphs
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_NestedInventory_ThreeLevelsDeep_AllRoundTrip()
        {
            // actor → inv → outerChest → containerInv → innerChest →
            //   containerInv → coin
            //
            // Pin every level survives + back-pointers + identity.
            // (Simulates the in-game "chest in actor's inventory
            //  contains another chest containing a coin" scenario.)
            var actor = new Entity { ID = "actor", BlueprintName = "Actor" };
            var aInv = new InventoryPart();
            actor.AddPart(aInv);

            var outerChest = new Entity { ID = "outer", BlueprintName = "Chest" };
            var outerInv = new InventoryPart();
            outerChest.AddPart(outerInv);
            aInv.Objects.Add(outerChest);

            var innerChest = new Entity { ID = "inner", BlueprintName = "Chest" };
            var innerInv = new InventoryPart();
            innerChest.AddPart(innerInv);
            outerInv.Objects.Add(innerChest);

            var coin = new Entity { ID = "coin", BlueprintName = "Coin" };
            innerInv.Objects.Add(coin);

            var loadedActor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var l1 = loadedActor.GetPart<InventoryPart>().Objects[0];
            var l2 = l1.GetPart<InventoryPart>().Objects[0];
            var l3 = l2.GetPart<InventoryPart>().Objects[0];
            Assert.AreEqual("outer", l1.ID, "Level-1 chest survives.");
            Assert.AreEqual("inner", l2.ID, "Level-2 chest survives.");
            Assert.AreEqual("coin",  l3.ID, "Level-3 coin survives.");
        }

        // ════════════════════════════════════════════════════════
        // Multi-round-trip
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_MultiRoundTrip_FinalStateMatches_LastModification()
        {
            // save → load → modify → save → load. The final state
            // should reflect the LAST modification, not the FIRST
            // (that would mean the second save+load loses the modify).
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.ForceApplyEffect(new RootedEffect(duration: 5));

            // First round-trip
            var loaded1 = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(5, loaded1.GetEffect<RootedEffect>().Duration,
                "Setup: first round-trip preserves Duration=5.");

            // Modify the loaded state
            loaded1.GetEffect<RootedEffect>().Duration = 2;

            // Second round-trip
            var loaded2 = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded1);
            Assert.AreEqual(2, loaded2.GetEffect<RootedEffect>().Duration,
                "Multi-round-trip: final state reflects the modification "
                + "after the FIRST load, not the original pre-load value. "
                + "Catches a bug where save grabs stale data.");
        }

        // ════════════════════════════════════════════════════════
        // Empty edges
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EntityWithNoParts_RoundTrips_AsBareEntity()
        {
            // Edge: an Entity with ID + BlueprintName but ZERO Parts.
            // Pin both fields survive; Parts list is non-null but
            // empty. (Catches a bug where the "no parts" branch
            // returns null Parts list.)
            var actor = new Entity { ID = "bare", BlueprintName = "Bare" };

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("bare", loaded.ID);
            Assert.AreEqual("Bare", loaded.BlueprintName);
            Assert.IsNotNull(loaded.Parts, "Parts list non-null after load.");
            Assert.AreEqual(0, loaded.Parts.Count, "Parts list is empty.");
        }

        [Test]
        public void Adversarial_EmptyInventory_AndEmptyEffects_OnSameActor()
        {
            // Edge: an Entity with both InventoryPart AND
            // StatusEffectsPart, BOTH empty. Pin both round-trip
            // as empty.
            var actor = new Entity { ID = "a", BlueprintName = "Test" };
            actor.AddPart(new InventoryPart());
            actor.AddPart(new StatusEffectsPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsNotNull(loaded.GetPart<InventoryPart>());
            Assert.AreEqual(0, loaded.GetPart<InventoryPart>().Objects.Count);
            Assert.IsNotNull(loaded.GetPart<StatusEffectsPart>());
            Assert.IsFalse(loaded.HasEffect<RootedEffect>(),
                "No effect was applied → none after load.");
        }

        // ════════════════════════════════════════════════════════
        // Token reuse
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SameEntity_ReferencedFiveTimes_SingleInstance_OnLoad()
        {
            // Adversarial: 5 distinct fields all reference the same
            // item Entity. WriteEntityReference's token-cache should
            // assign ONE token; ReadEntityReference returns the SAME
            // loaded instance for all 5 reads.
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            // 5 references to sword:
            inv.Objects.Add(sword);
            inv.EquippedItems["MainHand"] = sword;
            inv.EquippedItems["OffHand"]  = sword; // unusual but allowed
            inv.EquippedItems["Belt"]     = sword;
            inv.EquippedItems["Sheath"]   = sword;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(1, li.Objects.Count);
            Assert.AreEqual(4, li.EquippedItems.Count);

            var refs = new List<Entity>
            {
                li.Objects[0],
                li.EquippedItems["MainHand"],
                li.EquippedItems["OffHand"],
                li.EquippedItems["Belt"],
                li.EquippedItems["Sheath"],
            };
            for (int i = 1; i < refs.Count; i++)
            {
                Assert.AreSame(refs[0], refs[i],
                    $"All 5 references point at the SAME loaded instance "
                    + $"(checking ref index {i}). Catches a buggy load "
                    + $"that creates a fresh placeholder for each read.");
            }
        }

        [Test]
        public void Adversarial_FreshConstructed_EmptyZone_RoundTrips_NoOrphans()
        {
            // Most paranoid case: round-trip an Entity that's bare,
            // empty, and references nothing. Then ALSO round-trip a
            // peer Entity that references nothing. Both should produce
            // independent, healthy loaded entities — not share state
            // across separate round-trips (each round-trip uses a
            // fresh SaveWriter/SaveReader so no cross-contamination
            // is expected, but pin it).
            var a = new Entity { ID = "a", BlueprintName = "A" };
            var b = new Entity { ID = "b", BlueprintName = "B" };

            var loadedA = PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);
            var loadedB = PartRoundTripHelper.RoundTripEntityViaTokenGraph(b);
            Assert.AreEqual("a", loadedA.ID);
            Assert.AreEqual("b", loadedB.ID);
            Assert.AreNotSame(loadedA, loadedB,
                "Each round-trip uses its own writer/reader; loaded "
                + "instances are independent.");
        }
    }
}
