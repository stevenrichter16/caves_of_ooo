using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow rendering hypotheses: carried supplies, followers
    /// and native actors retain their body when a cell or depth changes.</summary>
    public sealed class AreaCompositionRenderingAdversarialTests
    {
        // A native expedition torch can be picked up on the terrace, carried
        // downstairs and dropped. Its geometry should not reroll because the
        // same owner has crossed a zone boundary. The old zone must relinquish
        // the recipe, while the destination changes position without changing
        // identity. Within-zone cases are the paired movement control.
        [TestCase("Torch", false)] [TestCase("Torch", true)]
        [TestCase("DriedMeat", false)] [TestCase("DriedMeat", true)]
        [TestCase("HealingTonic", false)] [TestCase("HealingTonic", true)]
        [TestCase("GinFrog", false)] [TestCase("GinFrog", true)]
        [TestCase("PrickleBrowGecko", false)] [TestCase("PrickleBrowGecko", true)]
        public void Adversarial_MovableGinmereFamilyRetainsItsBodyAcrossNativeMovement(string blueprint, bool crossDepth)
            => AssertStableMovement(blueprint, crossDepth);

        // These native identities use the shared catalog rather than the new
        // Ginmere family branch. Keep their behavior symmetric with the new
        // family branch, including current single-model aliases. Tepuibone is
        // an actual takeable shared-catalog item, not a synthetic physics flag.
        [TestCase("SnapjawScavenger", false)] [TestCase("SnapjawScavenger", true)]
        [TestCase("SnapjawHunter", false)] [TestCase("SnapjawHunter", true)]
        [TestCase("Player", false)] [TestCase("Player", true)]
        [TestCase("Tepuibone", false)] [TestCase("Tepuibone", true)]
        public void Adversarial_SharedNativeActorOrItemRetainsItsBodyAcrossNativeMovement(string blueprint, bool crossDepth)
            => AssertStableMovement(blueprint, crossDepth);

        private static void AssertStableMovement(string blueprint, bool crossDepth)
        {
            var factory = GrovelandsCompositionTests.Factory();
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            Assert.NotNull(library);
            var catalog = library.Definition;
            var first = new Zone("Overworld.2.7.1");
            var second = crossDepth ? new Zone("Overworld.2.7.2") : first;
            var owner = factory.CreateEntity(blueprint); Assert.NotNull(owner);
            Assert.IsTrue(owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.GetPart<PhysicsPart>()?.Takeable == true, "The positive subject must really be movable native content.");
            Assert.IsTrue(first.AddEntity(owner, 10, 10));
            var initial = SpawnRing3DRecipes.Resolve(first, owner, catalog);
            Assert.NotNull(initial.ModelId, initial.Failure);
            Assert.IsTrue(initial.Transient); Assert.IsFalse(initial.Batched);
            Assert.AreSame(owner, initial.Owner);
            string id = owner.ID;

            if (crossDepth)
            {
                first.RemoveEntity(owner);
                Assert.IsNull(SpawnRing3DRecipes.Resolve(first, owner, catalog).ModelId,
                    "The old native graph cannot retain a departed owner's visual.");
                Assert.IsTrue(second.AddEntity(owner, 15, 8));
            }
            for (int x = 16; x < 24; x++)
            {
                Assert.IsTrue(second.MoveEntity(owner, x, 8));
                int version = second.EntityVersion;
                var moved = SpawnRing3DRecipes.Resolve(second, owner, catalog);
                Assert.NotNull(moved.ModelId, moved.Failure);
                Assert.AreSame(owner, moved.Owner); Assert.AreEqual(id, owner.ID);
                Assert.AreEqual(Village3DProjection.CellCentre(x, 8), moved.Position);
                Assert.IsTrue(moved.Transient); Assert.IsFalse(moved.Batched);
                Assert.AreEqual(version, second.EntityVersion, "Recipe lookup cannot mutate the native graph.");
                Assert.AreEqual(initial.ModelId, moved.ModelId,
                    "An unchanged native owner must keep its body through movement and the descent.");
            }
            second.RemoveEntity(owner);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(second, owner, catalog).ModelId);
        }

        // Static scenery still needs coordinate variation. An overbroad fix
        // that removes the native cell from every recipe would make these
        // four-variant families collapse to one repeated body per owner.
        [TestCase("SandstoneFloor")] [TestCase("RopeAnchor")]
        public void Adversarial_StaticGinmereSceneryStillUsesItsNativeCoordinate(string blueprint)
        {
            var factory = GrovelandsCompositionTests.Factory();
            var zone = new Zone("Overworld.2.7.1");
            var owner = factory.CreateEntity(blueprint); Assert.NotNull(owner);
            Assert.IsFalse(owner.GetPart<PhysicsPart>().Takeable);
            Assert.IsTrue(zone.AddEntity(owner, 10, 10));
            var catalog = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var variants = new HashSet<string>();
            for (int x = 11; x < 25; x++)
            {
                Assert.IsTrue(zone.MoveEntity(owner, x, 10));
                var recipe = SpawnRing3DRecipes.Resolve(zone, owner, catalog);
                Assert.NotNull(recipe.ModelId, recipe.Failure);
                Assert.AreSame(owner, recipe.Owner); Assert.IsFalse(recipe.Transient); Assert.IsTrue(recipe.Batched);
                Assert.AreEqual(Village3DProjection.CellCentre(x, 10), recipe.Position);
                variants.Add(recipe.ModelId);
            }
            Assert.Greater(variants.Count, 1, "Scenery retains spatial variation while movable owners retain their body.");
        }
    }
}
