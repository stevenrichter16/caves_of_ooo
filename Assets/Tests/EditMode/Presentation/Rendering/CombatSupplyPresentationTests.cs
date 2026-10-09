using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CombatSupplyPresentationTests
    {
        static Entity Snare(EntityFactory factory)
        {
            var owner = factory.CreateEntity("KnotflaxSnare");
            Assert.NotNull(owner, "The real deployed cord blueprint must exist.");
            return owner;
        }

        static Part SnarePart(Entity owner)
        {
            var part = owner.Parts.FirstOrDefault(p => p.GetType().Name == "CordSnarePart");
            Assert.NotNull(part, "An actual live cord mechanism must own the appearance.");
            return part;
        }

        [TestCase(0)] [TestCase(1)]
        public void DeployedCordUsesDistinctExactLoopGeometryWithoutChangingMechanics(int variant)
        {
            using (var scope = new DensityLootTestScope())
            {
                var owner = Snare(scope.Factory); var parts = owner.Parts.ToArray();
                Assert.NotNull(SnarePart(owner));
                Assert.True(SpreadSceneryRecipes.TryModel(owner, variant, out var model));
                Assert.AreEqual("spread-scenery-knotflaxsnare-" + variant, model);
                Assert.True(SpreadScenerySource.IsModelId(model));
                Assert.AreEqual("entity", SpreadScenerySource.KindForModel(model), "Cord must not replace the cell's real ground.");
                CollectionAssert.AreEqual(parts, owner.Parts);
                Assert.False(owner.GetPart<PhysicsPart>().Solid);
                Assert.False(owner.GetPart<PhysicsPart>().Takeable);
            }
        }

        [TestCase("missing-mechanism")] [TestCase("foreign-mechanism")] [TestCase("spent")]
        [TestCase("hidden")] [TestCase("portable")] [TestCase("carried")] [TestCase("equipped")]
        [TestCase("solid")] [TestCase("solid-tag")] [TestCase("creature")] [TestCase("item")]
        [TestCase("glyph")] [TestCase("visual")] [TestCase("visual-variant")] [TestCase("glyph-variants")]
        public void AlteredCordOwnerCannotBorrowTheArmedSnareAppearance(string change)
        {
            using (var scope = new DensityLootTestScope())
            {
                var owner = Snare(scope.Factory); var mechanism = SnarePart(owner);
                Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out _));
                var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
                switch (change)
                {
                    case "missing-mechanism": owner.RemovePart(mechanism); break;
                    case "foreign-mechanism": mechanism.ParentEntity = new Entity(); break;
                    case "spent": mechanism.GetType().GetField("Spent").SetValue(mechanism, true); break;
                    case "hidden": render.Visible = false; break;
                    case "portable": physics.Takeable = true; break;
                    case "carried": physics.InInventory = new Entity(); break;
                    case "equipped": physics.Equipped = new Entity(); break;
                    case "solid": physics.Solid = true; break;
                    case "solid-tag": owner.Tags["Solid"] = ""; break;
                    case "creature": owner.Tags["Creature"] = ""; break;
                    case "item": owner.Tags["Item"] = ""; break;
                    case "glyph": render.RenderString = "X"; break;
                    case "visual": render.VisualID = "other"; break;
                    case "visual-variant": render.VisualVariant = "other"; break;
                    case "glyph-variants": render.GlyphVariants = "xy"; break;
                }
                Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out var model));
                Assert.Null(model);
            }
        }

        [TestCase(0)] [TestCase(1)]
        public void AuthoredCordHasReadableRaisedStakesAndAnOpenPaleLoop(int variant)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../ArtSource/SpreadScenery3D/kit.json"));
            var source = JsonUtility.FromJson<SpreadScenerySource>(File.ReadAllText(path));
            var model = source.models.SingleOrDefault(m => m.id == "spread-scenery-knotflaxsnare-" + variant);
            Assert.NotNull(model, "Both original source models must be authored before native import.");
            source.Validate();
            Assert.That(model.boxes.Count(b => b.color == 5 || b.color == 22), Is.GreaterThanOrEqualTo(8), "A broad pale loop distinguishes cord from metal traps.");
            Assert.That(model.boxes.Count(b => b.size[1] >= .15f), Is.GreaterThanOrEqualTo(2), "Raised stakes remain legible at the game camera angle.");
            Assert.True(model.boxes.All(b => b.center[1] + b.size[1] / 2 <= .30f), "The trip loop remains low rather than becoming a wall.");
            Assert.False(model.boxes.Any(b => Math.Abs(b.center[0]) < .08f && Math.Abs(b.center[2]) < .08f), "The noose center is open, not an invented metal plate.");
        }

        [TestCase("fog-memory")] [TestCase("removed")]
        public void NativeCordModelUsesRealPaletteAndRespectsStaticMemoryOrConsumption(string change)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("KnotflaxSnare", 20, 10); var parts = owner.Parts.ToArray();
                int version = f.Zone.EntityVersion; string tiles = f.Zone.TileState.ToSaveString();
                f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(owner, out var proof), proof.Failure);
                Assert.True(proof.Batched);
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                Assert.That(recipe.ModelId, Does.StartWith("spread-scenery-knotflaxsnare-"));
                var entry = SpreadScenery3DLibrary.Load().Find(recipe.ModelId);
                Assert.NotNull(entry); Assert.AreSame(entry.Mesh, proof.ExpectedMesh);
                Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material, proof.ExpectedMaterial);
                Assert.Greater(proof.SubmittedMesh.vertexCount, 0);
                Assert.AreEqual(version, f.Zone.EntityVersion); Assert.AreEqual(tiles, f.Zone.TileState.ToSaveString());
                CollectionAssert.AreEqual(parts, owner.Parts);
                if (change == "fog-memory")
                {
                    // Ordinary stationary scenery is remembered on explored
                    // cells, unlike the live gas volumes tested last phase.
                    f.Zone.GetCell(20, 10).IsVisible = false;
                    f.Refresh();
                    Assert.True(presenter.TryGetApprovedStyle(owner, out var remembered), remembered.Failure);
                    Assert.True(f.Rendered(owner), "Previously explored static cord retains ordinary fog memory.");
                    f.Zone.GetCell(20, 10).Explored = false;
                }
                else Assert.True(f.Zone.RemoveEntity(owner));
                f.Refresh(); Assert.False(presenter.TryGetApprovedStyle(owner, out _));
                Assert.False(f.Rendered(owner));
            }
        }

        [Test] public void SavedDeployedCordKeepsItsExactAppearanceOnItsLoadedOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("KnotflaxSnare", 20, 10); string id = owner.ID;
                f.Refresh(); var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                Assert.That(recipe.ModelId, Does.StartWith("spread-scenery-knotflaxsnare-"));
                f.BindLoaded(f.RoundTrip()); owner = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id);
                Assert.NotNull(SnarePart(owner));
                Assert.AreEqual(recipe.ModelId, SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition).ModelId);
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var proof), proof.Failure);
            }
        }
    }
}
