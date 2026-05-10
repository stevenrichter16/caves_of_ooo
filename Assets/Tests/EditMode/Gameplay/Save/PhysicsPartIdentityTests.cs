using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.8.4 — PhysicsPart back-pointer canonicalization on load.
    /// See <c>Docs/SAVE-LOAD-AUDIT.md §SL.8</c>.
    ///
    /// <para><b>Surprising contract surfaced by SL.8.4:</b> The
    /// <see cref="PhysicsPart.InInventory"/> and <c>Equipped</c>
    /// fields are NOT just blindly reflectively round-tripped — they
    /// are <b>canonicalized</b> on load by
    /// <see cref="InventoryPart.OnAfterLoad"/> /
    /// <c>FinalizeLoad</c> (InventoryPart.cs:282-313). The "ground
    /// truth" is which inventory collection the item sits in:</para>
    /// <list type="bullet">
    ///   <item>Item in <c>Objects[]</c>
    ///         → <c>InInventory = ParentEntity, Equipped = null</c></item>
    ///   <item>Item in <c>EquippedItems[]</c>
    ///         → <c>InInventory = null, Equipped = ParentEntity</c></item>
    /// </list>
    ///
    /// <para>This canonicalization is robust: even if a save file has
    /// stale or corrupt PhysicsPart fields (e.g. from an older save
    /// format), they get rewritten on load to match the live inventory
    /// state. Pinning this contract here so a future refactor that
    /// removes the canonicalization step surfaces here.</para>
    /// </summary>
    public class PhysicsPartIdentityTests
    {
        [Test]
        public void Item_InObjects_PhysicsBackPointers_Canonicalized_ToInInventory()
        {
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new PhysicsPart { InInventory = owner });
            inv.Objects.Add(item);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var loadedItem = loaded.GetPart<InventoryPart>().Objects[0];
            var phys = loadedItem.GetPart<PhysicsPart>();
            Assert.IsNotNull(phys.InInventory);
            Assert.AreSame(loaded, phys.InInventory,
                "Item in Objects[] → InInventory=ParentEntity (the same "
                + "loaded primary). Pinned by InventoryPart.OnAfterLoad's "
                + "RefreshLoadedBackReferences (line 296-302).");
            Assert.IsNull(phys.Equipped,
                "Item in Objects[] → Equipped is force-nulled on load. "
                + "Pinned by RefreshLoadedBackReferences (line 300).");
        }

        [Test]
        public void Item_InEquippedItems_PhysicsBackPointers_Canonicalized_ToEquipped()
        {
            var wearer = new Entity { ID = "wearer", BlueprintName = "Wearer" };
            var inv = new InventoryPart();
            wearer.AddPart(inv);

            var armor = new Entity { ID = "armor", BlueprintName = "Armor" };
            armor.AddPart(new PhysicsPart()); // fresh, fields will be canonicalized
            inv.EquippedItems["Body"] = armor;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(wearer);
            var loadedArmor = loaded.GetPart<InventoryPart>().EquippedItems["Body"];
            var phys = loadedArmor.GetPart<PhysicsPart>();
            Assert.IsNull(phys.InInventory,
                "Item in EquippedItems[] → InInventory is force-nulled. "
                + "Pinned by RefreshLoadedBackReferences (line 309).");
            Assert.AreSame(loaded, phys.Equipped,
                "Item in EquippedItems[] → Equipped=ParentEntity (the "
                + "loaded wearer). Pinned by RefreshLoadedBackReferences "
                + "(line 310).");
        }

        [Test]
        public void Item_InBothCollections_EquippedItems_Wins_Canonicalization()
        {
            // Adversarial: an item that's in BOTH Objects[] AND
            // EquippedItems[] (production-rare but data-shape-allowed).
            // RefreshLoadedBackReferences runs Objects loop first then
            // EquippedItems loop, so the EQUIPPED state wins for the
            // PhysicsPart back-pointers. Pin that.
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            sword.AddPart(new PhysicsPart());
            inv.Objects.Add(sword);
            inv.EquippedItems["Hand"] = sword;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            // Confirm both refer to the same instance (SL.8.2)
            Assert.AreSame(li.Objects[0], li.EquippedItems["Hand"]);
            // Now check the canonicalization
            var phys = li.Objects[0].GetPart<PhysicsPart>();
            Assert.IsNull(phys.InInventory,
                "Both-collection state: Equipped wins → InInventory null.");
            Assert.AreSame(loaded, phys.Equipped,
                "Both-collection state: Equipped wins → Equipped points "
                + "at the loaded owner. The Objects loop (line 296) ran "
                + "first and set InInventory=ParentEntity, but the "
                + "EquippedItems loop (line 304) overwrote it.");
        }

        [Test]
        public void MultipleItems_InObjects_AllBackPointTo_SameOwner()
        {
            // Multi-item variant: each item's InInventory is set to
            // the SAME loaded owner instance after canonicalization.
            // (The canonicalization sets `physics.InInventory = ParentEntity`
            // — same `ParentEntity` reference for every item in the
            // same inventory.)
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var sword  = new Entity { ID = "s", BlueprintName = "Sword" };
            var shield = new Entity { ID = "sh", BlueprintName = "Shield" };
            var helm   = new Entity { ID = "h", BlueprintName = "Helm" };
            sword.AddPart(new PhysicsPart());
            shield.AddPart(new PhysicsPart());
            helm.AddPart(new PhysicsPart());
            inv.Objects.Add(sword);
            inv.Objects.Add(shield);
            inv.Objects.Add(helm);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var li = loaded.GetPart<InventoryPart>();
            Assert.AreEqual(3, li.Objects.Count);

            var ownerFromSword  = li.Objects[0].GetPart<PhysicsPart>().InInventory;
            var ownerFromShield = li.Objects[1].GetPart<PhysicsPart>().InInventory;
            var ownerFromHelm   = li.Objects[2].GetPart<PhysicsPart>().InInventory;

            Assert.AreSame(loaded, ownerFromSword);
            Assert.AreSame(ownerFromSword, ownerFromShield,
                "All items in the same Objects[] back-point to ONE owner "
                + "instance — canonicalization writes ParentEntity which "
                + "is shared.");
            Assert.AreSame(ownerFromShield, ownerFromHelm);
        }

        [Test]
        public void StaleEquippedField_OnSavedItem_GetsResetByLoad()
        {
            // Adversarial: simulate a save with a STALE PhysicsPart
            // state — Equipped points at some random entity that
            // ISN'T the carrier. Item is in Objects[]. After load,
            // canonicalization should null out Equipped regardless
            // of what was saved.
            var owner = new Entity { ID = "owner", BlueprintName = "Owner" };
            var rando = new Entity { ID = "rando", BlueprintName = "Random" };
            var inv = new InventoryPart();
            owner.AddPart(inv);

            var item = new Entity { ID = "item", BlueprintName = "Item" };
            // Stale state: claims to be "equipped on rando" but is in
            // owner's Objects[] list. Canonicalization will normalize.
            item.AddPart(new PhysicsPart { Equipped = rando });
            inv.Objects.Add(item);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            var loadedItem = loaded.GetPart<InventoryPart>().Objects[0];
            var phys = loadedItem.GetPart<PhysicsPart>();
            Assert.IsNull(phys.Equipped,
                "Stale Equipped pointer is force-nulled on load. The "
                + "live inventory state (Objects[]) is the source of "
                + "truth, NOT the saved fields.");
            Assert.AreSame(loaded, phys.InInventory,
                "InInventory is correctly set to the loaded owner.");
        }
    }
}
