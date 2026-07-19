using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests.Gameplay.Items
{
    /// <summary>
    /// M3-L3 SM5 — the starter ingredient kit: every production reagent and
    /// weapon component granted to the player at bootstrap, stacked ×2, so
    /// the spawn-area stations are usable from turn one. The catalog lists
    /// live in CraftingStarterKit and are pinned against PRODUCTION
    /// Objects.json — a content rename breaks these tests, not the grant
    /// silently.
    /// </summary>
    public class CraftingStarterKitTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity { ID = "player", BlueprintName = "Player" };
            player.AddPart(new RenderPart { DisplayName = "player" });
            player.AddPart(new InventoryPart());
            return player;
        }

        [Test]
        public void GrantAll_GrantsEveryReagentAndComponentStackedTimesTwo()
        {
            var player = CreatePlayer();

            int granted = CraftingStarterKit.GrantAll(player, _factory);

            var inventory = player.GetPart<InventoryPart>().Objects;
            int expected = CraftingStarterKit.ReagentBlueprints.Length
                + CraftingStarterKit.ComponentBlueprints.Length;
            Assert.AreEqual(expected, granted, "one grant per catalog entry");

            foreach (string name in CraftingStarterKit.ReagentBlueprints)
            {
                Entity item = inventory.Find(e => e.BlueprintName == name);
                Assert.IsNotNull(item, $"reagent '{name}' granted");
                Assert.IsTrue(item.HasPart<ReagentPart>(),
                    $"'{name}' must be a real reagent — catalog/content drift otherwise");
                Assert.AreEqual(CraftingStarterKit.CopiesPerIngredient,
                    item.GetPart<StackerPart>()?.StackCount ?? 1,
                    $"'{name}' granted as a stack of {CraftingStarterKit.CopiesPerIngredient}");
            }

            foreach (string name in CraftingStarterKit.ComponentBlueprints)
            {
                Entity item = inventory.Find(e => e.BlueprintName == name);
                Assert.IsNotNull(item, $"component '{name}' granted");
                Assert.IsTrue(item.HasPart<WeaponComponentPart>(),
                    $"'{name}' must be a real weapon component");
                Assert.AreEqual(CraftingStarterKit.CopiesPerIngredient,
                    item.GetPart<StackerPart>()?.StackCount ?? 1,
                    $"'{name}' granted as a stack of {CraftingStarterKit.CopiesPerIngredient}");
            }
        }

        [Test]
        public void GrantAll_CatalogCoversEverySlotAndABrewableMix()
        {
            // The kit must actually enable the flows: at least one component
            // per forge slot, and the reagent list is the full 13-name
            // production catalog (pinned by count so a trimmed grant fails
            // loudly rather than quietly shipping a half kit).
            var player = CreatePlayer();
            CraftingStarterKit.GrantAll(player, _factory);
            var inventory = player.GetPart<InventoryPart>().Objects;

            bool blade = false, haft = false, binding = false;
            foreach (var item in inventory)
            {
                var comp = item.GetPart<WeaponComponentPart>();
                if (comp == null) continue;
                if (comp.Slot == "Blade") blade = true;
                if (comp.Slot == "Haft") haft = true;
                if (comp.Slot == "Binding") binding = true;
            }

            Assert.IsTrue(blade && haft && binding,
                "kit must cover all three forge slots");
            Assert.AreEqual(13, CraftingStarterKit.ReagentBlueprints.Length,
                "kit carries the full production reagent catalog");
        }

        [Test]
        public void GrantAll_NullInputs_NoThrowNoGrant()
        {
            Assert.DoesNotThrow(() => CraftingStarterKit.GrantAll(null, _factory));
            Assert.DoesNotThrow(() => CraftingStarterKit.GrantAll(CreatePlayer(), null));

            var player = CreatePlayer();
            Assert.AreEqual(0, CraftingStarterKit.GrantAll(player, null));
            Assert.AreEqual(0, player.GetPart<InventoryPart>().Objects.Count);
        }
    }
}
