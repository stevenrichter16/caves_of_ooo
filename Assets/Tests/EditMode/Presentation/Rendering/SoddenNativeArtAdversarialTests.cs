using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Receiving-world and mutable native-state counters for regional
    /// coverage. These exercise actual owner recipes/presenter submissions,
    /// independently of the imported environment mesh shape tests.</summary>
    public sealed class SoddenNativeArtAdversarialTests
    {
        const string Bog = "Overworld.15.7.0";
        sealed class NamedImpostor : Part
        {
            readonly string name;
            public NamedImpostor(string name) { this.name = name; }
            public override string Name => name;
        }
        static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f, Entity owner)
            => SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
        static void Modeled(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            var recipe = Recipe(f, owner);
            Assert.NotNull(recipe.ModelId, owner.BlueprintName + ": " + recipe.Failure);
            Assert.Null(recipe.Failure); Assert.AreSame(owner, recipe.Owner);
        }
        static void Refused(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            Assert.Null(Recipe(f, owner).ModelId, "Changed native owner cannot borrow the original form.");
            Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out _));
            f.Refresh(); Assert.False(f.Rendered(owner), "Refresh must retire a stale visual contribution.");
        }

        [TestCase("foreign-snare")] [TestCase("impostor-snare")] [TestCase("foreign-damage")]
        [TestCase("gone")] [TestCase("no-hp")] [TestCase("solid")]
        [TestCase("inventory")] [TestCase("visual")] [TestCase("variant")]
        public void GreatdewArtRequiresTheCurrentRealPlantHazard(string corruption)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var plant = f.Add("Greatdew"); var snare = plant.GetPart<GreatdewSnarePart>();
                Assert.NotNull(snare); Assert.False(plant.HasTag("Creature"));
                Assert.False(plant.GetPart<PhysicsPart>().Solid);
                Assert.Greater(snare.GrabChance, 0); Assert.Greater(snare.HoldTurns, 0); Assert.Greater(snare.Corrosion, 0);
                Modeled(f, plant); StringAssert.StartsWith("sodden-native-greatdew-", Recipe(f, plant).ModelId);
                f.Refresh(); Assert.True(f.Rendered(plant));
                if (corruption == "foreign-snare") snare.ParentEntity = new Entity();
                else if (corruption == "impostor-snare")
                {
                    string name = snare.Name; Assert.True(plant.RemovePart(snare)); plant.AddPart(new NamedImpostor(name));
                }
                else if (corruption == "foreign-damage") plant.GetPart<DestructiblePart>().ParentEntity = new Entity();
                else if (corruption == "gone") plant.GetPart<DestructiblePart>().Gone = true;
                else if (corruption == "no-hp") plant.GetPart<DestructiblePart>().HP = 0;
                else if (corruption == "solid") plant.GetPart<PhysicsPart>().Solid = true;
                else if (corruption == "inventory") plant.GetPart<PhysicsPart>().InInventory = f.Player;
                else if (corruption == "visual") plant.GetPart<RenderPart>().VisualID = "changed-live-plant";
                else plant.GetPart<RenderPart>().VisualVariant = "changed-live-plant";
                Refused(f, plant);
            }
        }

        [TestCase("InkVial", "carried")] [TestCase("InkVial", "equipped")]
        [TestCase("Reedfrog", "foreign-brain")] [TestCase("Viper", "glyph")]
        [TestCase("Reedfrog", "visual")] [TestCase("InkVial", "visual")]
        public void NewlyAdmittedSharedOwnersKeepTheirRealOwnershipAndAppearanceGuards(string blueprint, string change)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add(blueprint); Modeled(f, owner); f.Refresh(); Assert.True(f.Rendered(owner));
                if (change == "carried") owner.GetPart<PhysicsPart>().InInventory = f.Player;
                else if (change == "equipped") owner.GetPart<PhysicsPart>().Equipped = f.Player;
                else if (change == "foreign-brain") owner.GetPart<BrainPart>().ParentEntity = new Entity();
                else if (change == "glyph") owner.GetPart<RenderPart>().RenderString = "changed";
                else owner.GetPart<RenderPart>().VisualID = "changed-live-identity";
                Refused(f, owner);
            }
        }

        [TestCase("KnotflaxSnare")] [TestCase("CrackedGlowQuartz")] [TestCase("BerryBush")]
        public void DepletedObjectsCannotKeepTheirReadyOrLitArt(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add(blueprint); Modeled(f, owner); f.Refresh(); Assert.True(f.Rendered(owner));
                if (blueprint == "KnotflaxSnare") owner.GetPart<CordSnarePart>().Spent = true;
                else if (blueprint == "CrackedGlowQuartz") owner.GetPart<LifespanPart>().TurnsRemaining = 0;
                else owner.GetPart<HarvestablePart>().Harvested = true;
                Refused(f, owner);
            }
        }

        [TestCase("Overworld.15.07.0")] [TestCase("Overworld.15.7.1")]
        public void CurrentCachedGraphStillNeedsACanonicalSurfaceAddress(string replacement)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                Assert.True(SoddenPresentationScope.IsActive(f.Zone)); string original = f.Zone.ZoneID;
                f.Zone.ZoneID = replacement; f.Manager.CachedZones[replacement] = f.Zone;
                try { Assert.False(SoddenPresentationScope.IsActive(f.Zone)); }
                finally { f.Manager.CachedZones.Remove(replacement); f.Zone.ZoneID = original; }
                Assert.True(SoddenPresentationScope.IsActive(f.Zone));
            }
        }

        [Test] public void MissingActualMapCannotRetainPlausibleAddressAuthority()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                Assert.True(SoddenPresentationScope.IsActive(f.Zone)); var tiles = f.Manager.WorldMap.Tiles;
                try { f.Manager.WorldMap.Tiles = null; Assert.False(SoddenPresentationScope.IsActive(f.Zone)); }
                finally { f.Manager.WorldMap.Tiles = tiles; }
                Assert.True(SoddenPresentationScope.IsActive(f.Zone));
            }
        }

        [Test] public void FutureUnknownCreatureRetainsNativeFallbackRatherThanBorrowingAFrog()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("Reedfrog"); Modeled(f, owner); f.Refresh(); Assert.True(f.Rendered(owner));
                owner.BlueprintName = "FutureBogSpeciesNotInAnyArtLibrary";
                Refused(f, owner);
            }
        }

        [Test] public void RemovedVisitorLosesEvidenceBeforeAndAfterRefresh()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("GinFrog"); Modeled(f, owner); f.Refresh(); Assert.True(f.Rendered(owner));
                Assert.True(f.Zone.RemoveEntity(owner)); Refused(f, owner);
            }
        }

        [Test] public void BeatingDoesNotAcquireTheNewBogPlantOrSharedFrogAdmission()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.14.15.0"))
            {
                Assert.AreEqual(BiomeType.Beating, f.Manager.WorldMap.GetBiome(14, 15));
                Assert.False(SoddenPresentationScope.IsActive(f.Zone)); Assert.False(SpreadPresentationScope.IsActive(f.Zone));
                foreach (string blueprint in new[] { "Greatdew", "Reedfrog" })
                {
                    var owner = f.Add(blueprint); Assert.Null(Recipe(f, owner).ModelId);
                    f.Refresh(); Assert.False(f.Authored(owner));
                }
            }
        }

        [TestCase("Reedfrog")] [TestCase("GinFrog")] [TestCase("Bandfrog")] [TestCase("Viper")]
        public void OriginalBogBodiesKeepRealAnimationAndSkinAfterRegionalAdmission(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add(blueprint); var parts = owner.Parts.ToArray(); Modeled(f, owner); f.Refresh();
                Assert.True(f.Find(owner, out var view, out var model)); Assert.True(f.Rendered(owner));
                string expected = blueprint == "Viper" ? SpreadBiomeActorLibrary.ModelId(blueprint)
                    : SpreadVisitorCreatureSource.ForBlueprint(blueprint).Id;
                Assert.AreEqual(expected, model);
                var skins = view.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.AreEqual(1, skins.Length); Assert.Greater(skins[0].bones.Length, 1);
                Assert.AreEqual(skins[0].bones.Length, skins[0].sharedMesh.bindposeCount);
                var animator = view.GetComponentInChildren<Animator>(true);
                Assert.NotNull(animator); Assert.False(animator.applyRootMotion);
                var clips = animator.runtimeAnimatorController.animationClips;
                foreach (string clip in new[] { "Idle", "Walk", "Interact", "Attack", "Hit" })
                    Assert.AreEqual(1, clips.Count(c => c.name == clip && c.length > 0), blueprint + ": " + clip);
                CollectionAssert.AreEqual(parts, owner.Parts, "Art cannot change the actor's native parts.");
            }
        }
    }
}
