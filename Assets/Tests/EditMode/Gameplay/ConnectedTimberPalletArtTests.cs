using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual presenter/command graph; fixture placement is not native travel.</summary>
    public sealed class ConnectedTimberPalletArtTests
    {
        [TestCase(false)] [TestCase(true)]
        public void FinitePalletUsesAMutableViewAndDismantlingRemovesOnlyItsActualOwner(bool dismantle)
        {
            using (var f = new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey("GleanersTimberPallet"));
                var pallet = f.Add("GleanersTimberPallet"); f.Approach(pallet);
                var cell = f.Zone.GetEntityCell(pallet);
                var ground = cell.Objects.Where(e => e != pallet).ToArray();
                Assert.IsNotEmpty(ground, "Preservation counter-check needs real existing ground.");
                f.Refresh();
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, pallet, f.Library.Definition);
                Assert.Null(recipe.Failure); Assert.IsNotEmpty(recipe.ModelId);
                Assert.False(recipe.Batched, "A removable harvest owner must not remain baked into static ground.");
                Assert.True(f.Rendered(pallet));
                Assert.True(f.Find(pallet, out var view, out _)); Assert.NotNull(view);
                if (dismantle)
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(pallet, "Harvest"), f.Player, f.Zone).Success);
                // Normal empty-dirty refresh must observe the owner's removal;
                // the control proves a still-present owner is not hidden wholesale.
                f.Refresh(new HashSet<int>());
                Assert.AreEqual(!dismantle, f.Rendered(pallet));
                if (dismantle)
                {
                    SpawnRing3DIntegrationFixture.Hidden(view);
                    Assert.Null(SpawnRing3DRecipes.Resolve(f.Zone, pallet, f.Library.Definition).ModelId);
                }
                foreach (var owner in ground) Assert.True(cell.Objects.Contains(owner));
            }
        }
    }
}
