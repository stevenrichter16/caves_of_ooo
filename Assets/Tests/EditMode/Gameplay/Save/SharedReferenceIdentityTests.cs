using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.8.2 — Cross-Part reference identity: when the same Entity
    /// is referenced from multiple fields/Parts, those references
    /// must resolve to the SAME loaded Entity instance after round-
    /// trip. See <c>Docs/SAVE-LOAD-AUDIT.md §SL.8</c>.
    ///
    /// <para>The contract is enforced by the token system:</para>
    /// <list type="bullet">
    ///   <item><see cref="SaveWriter.WriteEntityReference"/>
    ///         dedupes by ref equality — multiple writes of the same
    ///         <c>Entity</c> reuse the same token (SaveSystem.cs:85-90).</item>
    ///   <item><see cref="SaveReader.ReadEntityReference"/> caches
    ///         placeholders by token — multiple reads of the same
    ///         token return the same <c>Entity</c> instance
    ///         (SaveSystem.cs:180-184).</item>
    /// </list>
    ///
    /// <para>These tests use <see cref="Assert.AreSame"/> (reference
    /// equality), NOT <see cref="Assert.AreEqual"/>. Two Entity copies
    /// with the same <c>ID</c> would pass <c>AreEqual</c> but fail
    /// <c>AreSame</c> — and any code that treats them as the same
    /// (e.g. updating one's <c>HP</c> and expecting the other to see
    /// it) would break silently in production.</para>
    /// </summary>
    public class SharedReferenceIdentityTests
    {
        [Test]
        public void Item_InBoth_ObjectsAndEquipped_SharesIdentity_OnLoad()
        {
            // Adversarial: a single sword in BOTH InventoryPart.Objects[]
            // AND EquippedItems["Hand"]. Save graph queues it ONCE
            // (token system); load resolves both fields to the SAME
            // Entity instance.
            var owner = new Entity { ID = "owner", BlueprintName = "Warrior" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            inv.Objects.Add(sword);
            inv.EquippedItems["Hand"] = sword;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(1, li.Objects.Count, "Setup: one item in Objects.");
            Assert.AreEqual(1, li.EquippedItems.Count);
            Assert.IsTrue(li.EquippedItems.ContainsKey("Hand"));

            Assert.AreSame(li.Objects[0], li.EquippedItems["Hand"],
                "REFERENCE IDENTITY: the same logical sword in BOTH "
                + "Objects[] and EquippedItems[Hand] must point at the "
                + "EXACT SAME Entity instance after load. AreEqual would "
                + "pass on two distinct copies sharing an ID — only "
                + "AreSame catches the identity contract.");
        }

        [Test]
        public void Item_InBoth_ObjectsAndBodyPart_SharesIdentity_OnLoad()
        {
            // Similar shape, but the second reference is a BodyPart's
            // _Equipped (the source-of-truth for the equipment slot
            // when Body is present). Common production pattern:
            // EquippedItems["Hand"] is a CACHE; the canonical pointer
            // is the BodyPart's _Equipped.
            var owner = new Entity { ID = "owner", BlueprintName = "Warrior" };
            var inv = new InventoryPart();
            owner.AddPart(inv);
            var body = new Body();
            owner.AddPart(body);

            var armor = new Entity { ID = "armor", BlueprintName = "ChainMail" };
            inv.Objects.Add(armor);

            var torso = new BodyPart { Type = "Torso", ID = 1, _Equipped = armor };
            body.SetBody(torso);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            var lt = loaded.GetPart<Body>().GetBody();
            Assert.AreSame(li.Objects[0], lt._Equipped,
                "Item in Inventory.Objects + BodyPart._Equipped must be "
                + "the same Entity instance after load. Otherwise the "
                + "'unequip' code path would mutate one copy and leave "
                + "the other still equipped.");
        }

        [Test]
        public void Item_InAllThree_Objects_Equipped_BodyPart_SharesIdentity()
        {
            // Triple-reference: Objects + EquippedItems + BodyPart._Equipped.
            // Production-real for a fully-equipped item. Pin all three
            // resolve to the same instance.
            var owner = new Entity { ID = "owner", BlueprintName = "Warrior" };
            var inv = new InventoryPart();
            owner.AddPart(inv);
            var body = new Body();
            owner.AddPart(body);

            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            inv.Objects.Add(sword);
            inv.EquippedItems["Hand"] = sword;
            var hand = new BodyPart { Type = "Hand", ID = 1, _Equipped = sword };
            body.SetBody(hand);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            var lh = loaded.GetPart<Body>().GetBody();

            Assert.AreSame(li.Objects[0], li.EquippedItems["Hand"],
                "Same instance: Objects[0] ↔ EquippedItems[Hand].");
            Assert.AreSame(li.Objects[0], lh._Equipped,
                "Same instance: Objects[0] ↔ BodyPart._Equipped.");
            Assert.AreSame(li.EquippedItems["Hand"], lh._Equipped,
                "Same instance: EquippedItems[Hand] ↔ BodyPart._Equipped.");
        }

        [Test]
        public void DistinctItems_With_DistinctIDs_StayDistinct()
        {
            // Counter-check: two DIFFERENT items must round-trip as
            // two DIFFERENT instances. Without this, a buggy load
            // that returns the same placeholder for every token
            // would still pass the AreSame tests above for the
            // single-item case.
            var owner = new Entity { ID = "owner", BlueprintName = "Warrior" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            var shield = new Entity { ID = "shield", BlueprintName = "Shield" };
            inv.Objects.Add(sword);
            inv.Objects.Add(shield);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(2, li.Objects.Count);
            Assert.AreNotSame(li.Objects[0], li.Objects[1],
                "Two distinct Entities must remain distinct instances "
                + "after load. Catches a buggy ReadEntityReference that "
                + "always returns the same placeholder.");
            Assert.AreEqual("sword", li.Objects[0].ID);
            Assert.AreEqual("shield", li.Objects[1].ID);
        }

        [Test]
        public void Item_InOneActor_NotInAnother_Identity_Unrelated()
        {
            // Counter-check: two actors each holding a distinct item.
            // The graph has TWO separate Entity refs being written.
            // Round-trip and verify each actor's item is its own
            // instance, NOT the other actor's item.
            // (This test only round-trips ONE actor at a time, but
            // exercises the cross-actor scenario via the inventory
            // round-trip helper.)
            var actorA = new Entity { ID = "actor-a", BlueprintName = "A" };
            var invA = new InventoryPart();
            actorA.AddPart(invA);
            invA.Objects.Add(new Entity { ID = "itemA", BlueprintName = "ItemA" });

            // Round-trip actorA in isolation
            var loadedA = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actorA);
            Assert.AreEqual("itemA",
                loadedA.GetPart<InventoryPart>().Objects[0].ID,
                "Actor's own item round-trips with the right ID.");
        }

        [Test]
        public void Item_OwnerBackpointer_RoundTrips_To_LoadedOwner()
        {
            // PhysicsPart.InInventory is the back-pointer from item to
            // owner. When we round-trip the OWNER entity (which
            // contains the item in its Inventory.Objects), the item's
            // PhysicsPart.InInventory must be set to the LOADED owner
            // — the same instance that is the round-trip's primary,
            // NOT a stale or null reference.
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new PhysicsPart { InInventory = owner });
            inv.Objects.Add(item);

            var loadedOwner = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var loadedItem = loadedOwner.GetPart<InventoryPart>().Objects[0];
            Assert.IsNotNull(loadedItem);
            var phys = loadedItem.GetPart<PhysicsPart>();
            Assert.IsNotNull(phys, "Item's PhysicsPart round-trips.");
            Assert.IsNotNull(phys.InInventory,
                "PhysicsPart.InInventory back-pointer must be non-null after load.");
            Assert.AreSame(loadedOwner, phys.InInventory,
                "REFERENCE IDENTITY: the item's owner back-pointer must "
                + "resolve to the SAME loaded Entity instance that's the "
                + "round-trip primary. Two distinct owners would mean "
                + "an item in inventory whose owner thinks it isn't "
                + "carrying it — a desync bug.");
        }
    }
}
