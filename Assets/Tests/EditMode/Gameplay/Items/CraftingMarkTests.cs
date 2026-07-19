using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Items
{
    /// <summary>
    /// M3-L3 SM2 — the "set aside for crafting" flow: CraftingMarkPart (the
    /// save-safe per-item marker), its CollectMarked partition helper (what
    /// the stations read), and ToggleCraftMarkCommand (what the inventory
    /// popup row executes). Counter-checks per §3.4: unmarkable items are
    /// rejected with no state change; unmarked items never leak into a
    /// collection; partitions are mutually exclusive.
    /// </summary>
    public class CraftingMarkTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private static Entity CreateActor()
        {
            var actor = new Entity { ID = "crafter", BlueprintName = "Player" };
            actor.AddPart(new RenderPart { DisplayName = "crafter" });
            actor.AddPart(new InventoryPart());
            return actor;
        }

        private static Entity GiveItem(Entity actor, string name, Part payload = null)
        {
            var item = new Entity { ID = name, BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name });
            if (payload != null)
                item.AddPart(payload);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static InventoryCommandResult Toggle(Entity actor, Entity item)
        {
            return InventorySystem.ExecuteCommand(
                new ToggleCraftMarkCommand(item), actor, null);
        }

        // ════════════════ Toggle semantics ════════════════

        [Test]
        public void Toggle_MarksThenUnmarks()
        {
            var actor = CreateActor();
            var reagent = GiveItem(actor, "fire moss", new ReagentPart { PropertiesRaw = "heat:1" });

            Assert.IsFalse(CraftingMarkPart.IsMarked(reagent), "fresh item starts unmarked");

            Assert.IsTrue(Toggle(actor, reagent).Success);
            Assert.IsTrue(CraftingMarkPart.IsMarked(reagent), "first toggle marks");

            Assert.IsTrue(Toggle(actor, reagent).Success);
            Assert.IsFalse(CraftingMarkPart.IsMarked(reagent), "second toggle unmarks");
        }

        [Test]
        public void Toggle_UnmarkableItem_RejectedAndUnchanged()
        {
            // Counter-check: a plain item (no reagent/component/brew/weapon
            // part) must be rejected, and no mark may appear on it.
            var actor = CreateActor();
            var rock = GiveItem(actor, "plain rock");

            var result = Toggle(actor, rock);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(CraftingMarkPart.IsMarked(rock));
        }

        [Test]
        public void Toggle_ItemNotInInventory_Rejected()
        {
            var actor = CreateActor();
            var stray = new Entity { ID = "stray", BlueprintName = "stray" };
            stray.AddPart(new RenderPart { DisplayName = "stray" });
            stray.AddPart(new ReagentPart { PropertiesRaw = "heat:1" });

            var result = Toggle(actor, stray);

            Assert.IsFalse(result.Success);
            Assert.IsFalse(CraftingMarkPart.IsMarked(stray));
        }

        [Test]
        public void Toggle_NullItem_Rejected()
        {
            var actor = CreateActor();
            Assert.IsFalse(Toggle(actor, null).Success);
        }

        // ════════════════ Eligibility ════════════════

        [Test]
        public void IsMarkable_AllFourCraftingKinds_True()
        {
            var actor = CreateActor();
            Assert.IsTrue(CraftingMarkPart.IsMarkable(
                GiveItem(actor, "reagent", new ReagentPart { PropertiesRaw = "heat:1" })), "reagent");
            Assert.IsTrue(CraftingMarkPart.IsMarkable(
                GiveItem(actor, "blade", new WeaponComponentPart { Slot = "Blade" })), "component");
            Assert.IsTrue(CraftingMarkPart.IsMarkable(
                GiveItem(actor, "coating", new BrewItemPart { Form = "Coating", EffectsRaw = "Burning:1" })), "brew item");
            Assert.IsTrue(CraftingMarkPart.IsMarkable(
                GiveItem(actor, "sword", new MeleeWeaponPart())), "melee weapon");
        }

        [Test]
        public void IsMarkable_PlainItemOrNull_False()
        {
            var actor = CreateActor();
            Assert.IsFalse(CraftingMarkPart.IsMarkable(GiveItem(actor, "plain rock")));
            Assert.IsFalse(CraftingMarkPart.IsMarkable(null));
        }

        // ════════════════ CollectMarked partitioning ════════════════

        [Test]
        public void CollectMarked_PartitionsByKind_AndSkipsUnmarked()
        {
            var actor = CreateActor();

            var reagent = GiveItem(actor, "fire moss", new ReagentPart { PropertiesRaw = "heat:1" });
            var blade = GiveItem(actor, "steel blade", new WeaponComponentPart { Slot = "Blade" });
            var haft = GiveItem(actor, "oak haft", new WeaponComponentPart { Slot = "Haft" });
            var coating = GiveItem(actor, "flame coating", new BrewItemPart { Form = "Coating", EffectsRaw = "Burning:1" });
            var weapon = GiveItem(actor, "old sword", new MeleeWeaponPart());
            var unmarkedReagent = GiveItem(actor, "bog sap", new ReagentPart { PropertiesRaw = "toxic:1" });

            foreach (var item in new[] { reagent, blade, haft, coating, weapon })
                Assert.IsTrue(Toggle(actor, item).Success);

            var selection = CraftingMarkPart.CollectMarked(actor);

            CollectionAssert.AreEquivalent(new[] { reagent }, selection.Reagents,
                "only the MARKED reagent — unmarked bog sap must not leak in");
            CollectionAssert.AreEquivalent(new[] { blade, haft }, selection.Components);
            CollectionAssert.AreEquivalent(new[] { coating }, selection.Coatings);
            CollectionAssert.AreEquivalent(new[] { weapon }, selection.Weapons);
        }

        [Test]
        public void CollectMarked_PartitionsAreMutuallyExclusive()
        {
            // A marked component must appear ONLY in Components — not in
            // Weapons (components are not weapons) and not in Reagents.
            var actor = CreateActor();
            var binding = GiveItem(actor, "leather binding", new WeaponComponentPart { Slot = "Binding" });
            Assert.IsTrue(Toggle(actor, binding).Success);

            var selection = CraftingMarkPart.CollectMarked(actor);

            CollectionAssert.AreEquivalent(new[] { binding }, selection.Components);
            CollectionAssert.IsEmpty(selection.Reagents);
            CollectionAssert.IsEmpty(selection.Coatings);
            CollectionAssert.IsEmpty(selection.Weapons);
        }

        [Test]
        public void CollectMarked_NullOrEmptyActor_EmptySelection()
        {
            var empty = CraftingMarkPart.CollectMarked(null);
            Assert.IsNotNull(empty);
            CollectionAssert.IsEmpty(empty.Reagents);
            CollectionAssert.IsEmpty(empty.Components);
            CollectionAssert.IsEmpty(empty.Coatings);
            CollectionAssert.IsEmpty(empty.Weapons);

            var bare = CraftingMarkPart.CollectMarked(CreateActor());
            CollectionAssert.IsEmpty(bare.Reagents);
        }
    }
}
