using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.3 — Save/Load Round-Trip Audit, Tier-3 Parts with Entity
    /// references. See <c>Docs/SAVE-LOAD-AUDIT.md</c> for the audit plan.
    ///
    /// <para>Targets Tier-3 (reflection-serialized) Parts that hold
    /// public <see cref="Entity"/> fields or generic collections of
    /// Entity. Per the audit plan: this is the "highest-suspicion
    /// surface" because the token-based serialization requires both
    /// <c>WriteQueuedEntityBodies</c> and <c>ReadEntityBodies</c> to be
    /// called — without those, Entity refs round-trip as empty
    /// placeholders.</para>
    ///
    /// <para><b>SL.3 in-scope</b> (Tier-3 reflection path):
    /// <list type="bullet">
    ///   <item><c>PhysicsPart.InInventory</c> + <c>Equipped</c>
    ///         (Entity refs deferred from SL.2)</item>
    ///   <item><c>ContainerPart.Contents</c> (List&lt;Entity&gt;)</item>
    /// </list></para>
    ///
    /// <para><b>Out of SL.3 scope</b> (covered elsewhere):
    /// <list type="bullet">
    ///   <item>Tier-1 explicit handlers (<c>BrainPart.Target</c>,
    ///         <c>BrainPart.PersonalEnemies</c>, <c>InventoryPart</c>)
    ///         → SL.7</item>
    ///   <item>Effect Entity refs (<c>BurningEffect.IgnitionSource</c>,
    ///         <c>HookedEffect.Hooker</c>, <c>SittingEffect.Furniture</c>,
    ///         base <c>Effect.Owner</c>) → SL.6</item>
    /// </list></para>
    ///
    /// <para><b>Bug-class probes</b> (16-surface taxonomy from audit doc):
    /// <list type="bullet">
    ///   <item>SL-4 Entity reference round-trip (token + body queue)</item>
    ///   <item>SL-5 Generic collection of Entity</item>
    ///   <item>SL-7 Object reference (verifies non-null but-empty
    ///         placeholders without bodies)</item>
    /// </list></para>
    /// </summary>
    public class Tier3EntityReferenceRoundTripTests
    {
        // ── A. Bare-helper contract (no body queue/read) ────────────────
        //
        // The bare RoundTripEntity helper writes the primary entity's
        // body but does NOT call WriteQueuedEntityBodies. Without the
        // matching ReadEntityBodies on load, Entity refs round-trip as
        // placeholder Entities (created on demand by
        // ReadEntityReference, never populated). Pinning this contract
        // documents what the bare helper does and does NOT do — and
        // catches a future change that silently makes it the full
        // helper without updating callers.

        [Test]
        public void Adversarial_PhysicsPart_InInventory_BareHelper_LoadsAsPlaceholder()
        {
            // The owner entity has data; via the bare helper, the
            // loaded.InInventory should be non-null (placeholder
            // created by ReadEntityReference) but its ID and Parts
            // are empty because no body round-trip happened.
            var owner = new Entity { ID = "owner-id", BlueprintName = "OwnerNpc" };
            owner.AddPart(new RenderPart { DisplayName = "owner display" });

            var item = new Entity { ID = "item-id", BlueprintName = "TestItem" };
            item.AddPart(new PhysicsPart { Solid = false, InInventory = owner });

            var loaded = PartRoundTripHelper.RoundTripEntity(item);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics, "PhysicsPart itself round-trips.");
            Assert.IsNotNull(physics.InInventory,
                "Entity ref reads as a non-null placeholder via token.");
            Assert.IsTrue(string.IsNullOrEmpty(physics.InInventory.ID),
                "Bare helper does NOT round-trip referenced-entity body, "
                + "so the placeholder's ID is empty.");
            Assert.AreEqual(0, physics.InInventory.Parts.Count,
                "Placeholder has no Parts because no body round-trip ran.");
        }

        // ── B. Full-helper contract (with body queue + read) ────────────
        //
        // RoundTripEntityWithBodies calls WriteQueuedEntityBodies and
        // ReadEntityBodies, so referenced entities round-trip with
        // their full body (ID, BlueprintName, Tags, Parts).

        [Test]
        public void Adversarial_PhysicsPart_InInventory_FullHelper_RoundTripsBody()
        {
            var owner = new Entity { ID = "owner-id", BlueprintName = "OwnerNpc" };
            owner.AddPart(new RenderPart { DisplayName = "owner display" });

            var item = new Entity { ID = "item-id", BlueprintName = "TestItem" };
            item.AddPart(new PhysicsPart { Solid = false, InInventory = owner });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(item);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics);
            Assert.IsNotNull(physics.InInventory,
                "Full helper resolves the Entity ref to a populated entity.");
            Assert.AreEqual("owner-id", physics.InInventory.ID,
                "Owner's ID round-trips through the body queue.");
            Assert.AreEqual("OwnerNpc", physics.InInventory.BlueprintName,
                "Owner's BlueprintName round-trips through the body queue.");
            Assert.AreEqual("owner display",
                physics.InInventory.GetPart<RenderPart>()?.DisplayName,
                "Owner's Parts round-trip through the body queue.");
        }

        [Test]
        public void Adversarial_PhysicsPart_Equipped_FullHelper_RoundTripsBody()
        {
            // Counter-checks that BOTH Entity-ref fields on PhysicsPart
            // round-trip — not just the first.
            var wearer = new Entity { ID = "wearer-id", BlueprintName = "WearerNpc" };

            var armor = new Entity { ID = "armor-id", BlueprintName = "TestArmor" };
            armor.AddPart(new PhysicsPart { Equipped = wearer });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(armor);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics?.Equipped);
            Assert.AreEqual("wearer-id", physics.Equipped.ID);
            Assert.AreEqual("WearerNpc", physics.Equipped.BlueprintName);
            Assert.IsNull(physics.InInventory,
                "InInventory was never set; round-trips as null.");
        }

        // ── C. Null-Entity-ref counter-check ────────────────────────────

        [Test]
        public void Adversarial_PhysicsPart_NullEntityRefs_RoundTripAsNull()
        {
            // Counter-check: a buggy impl that fabricated placeholders
            // for token=0 (null) would create a non-null InInventory
            // here. The token=0 sentinel must round-trip as null.
            var item = new Entity { ID = "item", BlueprintName = "TestItem" };
            item.AddPart(new PhysicsPart
            {
                InInventory = null,
                Equipped = null,
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(item);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics);
            Assert.IsNull(physics.InInventory, "Null token=0 round-trips as null.");
            Assert.IsNull(physics.Equipped, "Null token=0 round-trips as null.");
        }

        // ── D. Token deduplication: same entity, two refs ───────────────

        [Test]
        public void Adversarial_PhysicsPart_SameEntityRefTwice_DedupesToSameInstanceOnLoad()
        {
            // ADVERSARIAL: pin the token-dedup contract. If the same
            // entity is referenced from TWO Part fields, load must
            // return the same Entity INSTANCE for both — not two
            // separately-loaded copies. A buggy impl that wrote unique
            // tokens per call site (instead of per entity) would fail
            // this.
            var sharedEntity = new Entity { ID = "shared-id", BlueprintName = "Shared" };

            var item = new Entity { ID = "item", BlueprintName = "TestItem" };
            item.AddPart(new PhysicsPart
            {
                InInventory = sharedEntity,
                Equipped = sharedEntity,  // same instance, two refs
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(item);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics?.InInventory);
            Assert.AreSame(physics.InInventory, physics.Equipped,
                "Two refs to the same entity must dedupe to a single "
                + "loaded Entity instance via the token cache.");
            Assert.AreEqual("shared-id", physics.InInventory.ID);
        }

        [Test]
        public void Adversarial_PhysicsPart_DifferentEntityRefs_LoadAsDistinctInstances()
        {
            // Counter-check to the dedup test: two DIFFERENT entities
            // must get distinct tokens and load as distinct instances.
            var alpha = new Entity { ID = "alpha-id", BlueprintName = "Alpha" };
            var beta = new Entity { ID = "beta-id", BlueprintName = "Beta" };

            var item = new Entity { ID = "item", BlueprintName = "TestItem" };
            item.AddPart(new PhysicsPart
            {
                InInventory = alpha,
                Equipped = beta,
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(item);
            var physics = loaded.GetPart<PhysicsPart>();

            Assert.AreNotSame(physics.InInventory, physics.Equipped,
                "Distinct source entities load as distinct instances.");
            Assert.AreEqual("alpha-id", physics.InInventory.ID);
            Assert.AreEqual("beta-id", physics.Equipped.ID);
        }

        // ── E. ContainerPart.Contents (List<Entity>) ────────────────────

        [Test]
        public void Adversarial_ContainerPart_Contents_FullHelper_RoundTripsAllItems()
        {
            // List<Entity> elements use WriteEntityReference too
            // (per WriteCollectionElement in SaveSystem.cs:1767-1770).
            // All three items' bodies must round-trip via the queue.
            var apple = new Entity { ID = "apple-id", BlueprintName = "Apple" };
            apple.AddPart(new RenderPart { DisplayName = "an apple" });
            var bread = new Entity { ID = "bread-id", BlueprintName = "Bread" };
            bread.AddPart(new RenderPart { DisplayName = "a loaf of bread" });
            var coin = new Entity { ID = "coin-id", BlueprintName = "Coin" };

            var chest = new Entity { ID = "chest", BlueprintName = "Chest" };
            chest.AddPart(new ContainerPart
            {
                Contents = new List<Entity> { apple, bread, coin },
                Preposition = "in",
                Locked = false,
                MaxItems = -1,
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(chest);
            var container = loaded.GetPart<ContainerPart>();

            Assert.IsNotNull(container);
            Assert.AreEqual(3, container.Contents.Count, "All three items round-trip.");
            Assert.AreEqual("apple-id", container.Contents[0].ID);
            Assert.AreEqual("Apple", container.Contents[0].BlueprintName);
            Assert.AreEqual("an apple",
                container.Contents[0].GetPart<RenderPart>()?.DisplayName);
            Assert.AreEqual("bread-id", container.Contents[1].ID);
            Assert.AreEqual("a loaf of bread",
                container.Contents[1].GetPart<RenderPart>()?.DisplayName);
            Assert.AreEqual("coin-id", container.Contents[2].ID);
        }

        [Test]
        public void Adversarial_ContainerPart_EmptyContents_RoundTripsEmpty()
        {
            // Counter-check: a List<Entity> count=0 must NOT collapse
            // to null on load. The list with zero elements must
            // round-trip as an empty (non-null) list.
            var chest = new Entity { ID = "chest", BlueprintName = "Chest" };
            chest.AddPart(new ContainerPart
            {
                Contents = new List<Entity>(),
                Preposition = "in",
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(chest);
            var container = loaded.GetPart<ContainerPart>();

            Assert.IsNotNull(container);
            Assert.IsNotNull(container.Contents, "Empty list round-trips as non-null.");
            Assert.AreEqual(0, container.Contents.Count);
        }

        [Test]
        public void Adversarial_ContainerPart_SimpleFields_RoundTrip()
        {
            // SL.2-style test for ContainerPart's non-Entity fields,
            // bare helper. Verifies Preposition, Locked, MaxItems
            // round-trip.
            var chest = new Entity { ID = "chest", BlueprintName = "Chest" };
            chest.AddPart(new ContainerPart
            {
                Preposition = "on",
                Locked = true,
                MaxItems = 7,
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(chest);
            var container = loaded.GetPart<ContainerPart>();

            Assert.IsNotNull(container);
            Assert.AreEqual("on", container.Preposition);
            Assert.IsTrue(container.Locked,
                "Counter-check: a buggy impl returning default (false) would fail here.");
            Assert.AreEqual(7, container.MaxItems);
        }

        // ── F. Cross-collection dedup: same entity in list AND PhysicsPart
        //
        // Adversarial: Entity-ref dedup must work ACROSS Part
        // boundaries. If an item is in a chest's Contents AND also
        // referenced by some other Part on the chest, both refs must
        // resolve to the same loaded Entity instance.

        [Test]
        public void Adversarial_ContainerWithItemAlsoReferencedByPhysicsParent_DedupesAcrossParts()
        {
            // The "owner" entity is BOTH:
            //   - the chest's PhysicsPart.InInventory back-ref, AND
            //   - the chest's ContainerPart.Contents[0] (weird shape,
            //     but the dedup contract still applies).
            // After load, both refs must point to the same Entity.
            var sharedEntity = new Entity { ID = "shared", BlueprintName = "Shared" };

            var weirdItem = new Entity { ID = "weird", BlueprintName = "WeirdItem" };
            weirdItem.AddPart(new PhysicsPart { InInventory = sharedEntity });
            weirdItem.AddPart(new ContainerPart
            {
                Contents = new List<Entity> { sharedEntity },
            });

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(weirdItem);
            var physics = loaded.GetPart<PhysicsPart>();
            var container = loaded.GetPart<ContainerPart>();

            Assert.IsNotNull(physics?.InInventory);
            Assert.AreEqual(1, container.Contents.Count);
            Assert.AreSame(physics.InInventory, container.Contents[0],
                "Same source entity referenced from two different Parts "
                + "must load as a single Entity instance — token dedup "
                + "spans the whole save graph, not per-Part.");
        }
    }
}
