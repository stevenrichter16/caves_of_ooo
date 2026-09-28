using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual imported Spread art and real DragSystem/AfterMove path.
    /// Player placement and the one factory beam are explicit fixture setup;
    /// this is not an ordinary acquisition, paid-input or rendered-pixel claim.</summary>
    public sealed class HaulablePresentationIdentityTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SameFallenBeamKeepsItsAuthoredShapeAcrossAnActualDrag(bool grabbed)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var corridor = FindPlainCorridor(f);
                Assert.IsTrue(f.Zone.MoveEntity(f.Player, corridor.x, corridor.y));
                var beam = f.Add("FallenBeam", corridor.x + 1, corridor.y);
                var physics = beam.GetPart<PhysicsPart>();
                var handling = beam.GetPart<HandlingPart>();
                var render = beam.GetPart<RenderPart>();
                Assert.NotNull(physics); Assert.NotNull(handling); Assert.NotNull(render);
                Assert.IsTrue(physics.Solid); Assert.IsFalse(physics.Takeable);
                Assert.IsFalse(handling.Carryable);
                Assert.That(DragRules.WeightOf(beam), Is.LessThanOrEqualTo(DragRules.MaxDragWeight(f.Player)));
                string id = beam.ID;
                Assert.IsNotEmpty(id);
                string glyph = render.RenderString;
                float weight = physics.Weight;
                var ground = f.Zone.GetReadOnlyEntities().First(e => e.BlueprintName == "Grass"
                    && f.Zone.GetEntityPosition(e).y != corridor.y);
                var groundPosition = f.Zone.GetEntityPosition(ground);
                f.Refresh();
                var before = Recipe(f, beam);
                var groundBefore = Recipe(f, ground);
                var styleBefore = Style(f, beam);
                var groundStyleBefore = Style(f, ground);
                StringAssert.StartsWith("ring-fallen-beam-", before.ModelId);
                var sourceBefore = SpreadNativeStyle3DLibrary.Load().ForOwner(f.Zone, before);
                Assert.NotNull(sourceBefore);
                Assert.AreSame(sourceBefore.Mesh, styleBefore.ExpectedMesh);
                var rotationBefore = sourceBefore.Prefab.transform.localRotation;
                var scaleBefore = sourceBefore.Prefab.transform.localScale;
                int speedBefore = f.Player.GetStatValue("Speed");
                try
                {
                    if (grabbed)
                    {
                        Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(f.Player, beam, f.Zone));
                        Assert.AreSame(beam, DragSystem.GetDragged(f.Player));
                        Assert.AreSame(f.Player, DragSystem.GetDragger(beam));
                    }
                    else Assert.IsFalse(DragSystem.IsBeingDragged(beam));

                    // This is the ordinary movement event pipeline. The load is
                    // never moved directly: DragPart follows into the vacated cell.
                    Assert.IsTrue(MovementSystem.TryMoveTo(f.Player, f.Zone, corridor.x - 1, corridor.y));
                    Assert.AreEqual((corridor.x - 1, corridor.y), f.Zone.GetEntityPosition(f.Player));
                    Assert.AreEqual((corridor.x + (grabbed ? 0 : 1), corridor.y), f.Zone.GetEntityPosition(beam));
                    Assert.AreSame(beam, f.Zone.GetEntityCell(beam).Objects.Single(e => ReferenceEquals(e, beam)));
                    Assert.AreEqual(id, beam.ID);
                    Assert.AreSame(physics, beam.GetPart<PhysicsPart>());
                    Assert.AreSame(handling, beam.GetPart<HandlingPart>());
                    Assert.AreSame(render, beam.GetPart<RenderPart>());
                    Assert.AreEqual(glyph, render.RenderString); Assert.AreEqual(weight, physics.Weight);
                    f.Refresh();

                    // An unrelated generated static owner must not reroll when
                    // another patch/source is refreshed. This control precedes
                    // the beam assertion, so it executes in the expected RED too.
                    Assert.AreEqual(groundPosition, f.Zone.GetEntityPosition(ground));
                    var groundAfter = Recipe(f, ground);
                    Assert.AreEqual(groundBefore.ModelId, groundAfter.ModelId);
                    Assert.AreEqual(groundBefore.QuarterTurns, groundAfter.QuarterTurns);
                    Assert.AreEqual(groundBefore.Position, groundAfter.Position);
                    var groundStyleAfter = Style(f, ground);
                    Assert.AreSame(groundStyleBefore.ExpectedMesh, groundStyleAfter.ExpectedMesh);
                    Assert.AreSame(groundStyleBefore.ExpectedMaterial, groundStyleAfter.ExpectedMaterial);

                    var after = Recipe(f, beam);
                    var styleAfter = Style(f, beam);
                    var sourceAfter = SpreadNativeStyle3DLibrary.Load().ForOwner(f.Zone, after);
                    Assert.NotNull(sourceAfter);
                    Assert.AreSame(sourceAfter.Mesh, styleAfter.ExpectedMesh);
                    Assert.AreSame(beam, after.Owner);
                    Assert.AreEqual(before.Position + (grabbed ? Vector3.left : Vector3.zero), after.Position);
                    Assert.AreEqual(before.ModelId, after.ModelId,
                        "Dragging changes location, not the same physical timber's authored variant.");
                    Assert.AreEqual(before.QuarterTurns, after.QuarterTurns,
                        "Drag has no native rotation operation; preserve the recipe orientation.");
                    Assert.AreSame(styleBefore.ExpectedMesh, styleAfter.ExpectedMesh,
                        "The committed approved mesh must remain the same timber shape.");
                    Assert.AreEqual(rotationBefore, sourceAfter.Prefab.transform.localRotation);
                    Assert.AreEqual(scaleBefore, sourceAfter.Prefab.transform.localScale);
                    Assert.AreSame(styleBefore.ExpectedMaterial, styleAfter.ExpectedMaterial);
                }
                finally
                {
                    DragSystem.Release(f.Player);
                    Assert.IsFalse(DragSystem.IsDragging(f.Player));
                    Assert.IsFalse(DragSystem.IsBeingDragged(beam));
                    Assert.AreEqual(speedBefore, f.Player.GetStatValue("Speed"));
                }
            }
        }

        [Test]
        public void ReleasedBeamRetainsItsExactMovedModelAcrossARealSavedReplacementGraph()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var at = FindPlainCorridor(f);
                Assert.True(f.Zone.MoveEntity(f.Player, at.x, at.y));
                var beam = f.Add("FallenBeam", at.x + 1, at.y);
                string id = beam.ID;
                var properties = beam.Properties.OrderBy(p => p.Key).ToArray();
                try
                {
                    Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(f.Player, beam, f.Zone));
                    Assert.True(MovementSystem.TryMoveTo(f.Player, f.Zone, at.x - 1, at.y));
                    Assert.AreEqual((at.x, at.y), f.Zone.GetEntityPosition(beam));
                }
                finally { DragSystem.Release(f.Player); }
                f.Refresh();
                var before = Recipe(f, beam); var source = Style(f, beam);
                var position = f.Zone.GetEntityPosition(beam);
                CollectionAssert.AreEqual(properties, beam.Properties.OrderBy(p => p.Key).ToArray(),
                    "Resolving a stable cosmetic variant must not stamp a new saved visual property.");
                var loaded = f.RoundTrip(); f.BindLoaded(loaded);
                var current = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id);
                Assert.AreNotSame(beam, current); Assert.AreEqual("FallenBeam", current.BlueprintName);
                Assert.AreEqual(position, f.Zone.GetEntityPosition(current));
                Assert.False(DragSystem.IsBeingDragged(current));
                CollectionAssert.AreEqual(properties, current.Properties.OrderBy(p => p.Key).ToArray());
                var after = Recipe(f, current); var restored = Style(f, current);
                Assert.AreEqual(before.ModelId, after.ModelId); Assert.AreEqual(before.QuarterTurns, after.QuarterTurns);
                Assert.AreSame(source.ExpectedMesh, restored.ExpectedMesh); Assert.AreSame(source.ExpectedMaterial, restored.ExpectedMaterial);
                Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(beam, out _),
                    "The pre-load reference cannot claim the replacement owner's rendered proof.");
            }
        }

        [Test]
        public void UnrelatedStaticFamilyRetainsItsExistingCoordinateBasedVariation()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var at = FindPlainCorridor(f); Assert.True(f.Zone.MoveEntity(f.Player, at.x - 1, at.y));
                var stone = f.Add("TepuiStone", at.x + 1, at.y); string id = stone.ID;
                f.Refresh(); var before = Recipe(f, stone); var source = Style(f, stone);
                StringAssert.StartsWith("ring-tepui-stone-", before.ModelId);
                // Diagnostic relocation is explicit for this unrelated static
                // control; it does not claim that the player can haul a stone tile.
                Assert.True(f.Zone.MoveEntity(stone, at.x, at.y)); f.Refresh();
                var after = Recipe(f, stone); var moved = Style(f, stone);
                Assert.AreEqual(id, stone.ID); Assert.AreSame(stone, after.Owner);
                Assert.AreEqual(before.Position + Vector3.left, after.Position);
                Assert.AreNotEqual(before.ModelId, after.ModelId,
                    "The fix must not freeze all ordinary positional scenery variants.");
                Assert.AreNotSame(source.ExpectedMesh, moved.ExpectedMesh);
                Assert.AreSame(source.ExpectedMaterial, moved.ExpectedMaterial);
            }
        }

        static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
            Assert.IsNull(recipe.Failure, recipe.Failure);
            Assert.AreSame(owner, recipe.Owner);
            Assert.IsNotEmpty(recipe.ModelId);
            return recipe;
        }
        static SpreadBiomeStyleEvidence Style(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            Assert.IsTrue(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var evidence), evidence.Failure);
            Assert.NotNull(evidence.ExpectedMesh); Assert.NotNull(evidence.ExpectedMaterial);
            return evidence;
        }
        static (int x, int y) FindPlainCorridor(SpawnRing3DIntegrationFixture f)
        {
            for (int y = 1; y < Zone.Height - 1; y++)
                for (int x = 2; x < Zone.Width - 2; x++)
                    if (Plain(f, x - 1, y) && Plain(f, x, y) && Plain(f, x + 1, y)) return (x, y);
            Assert.Fail("Actual generated zone lacks a three-cell dry grass corridor; do not clear native owners to manufacture one.");
            return (-1, -1);
        }
        static bool Plain(SpawnRing3DIntegrationFixture f, int x, int y)
        {
            var cell = f.Zone.GetCell(x, y);
            var tile = f.Zone.TileState.Get(x, y);
            return cell != null && !cell.BlocksMovement(f.Player) && (tile == null || tile.IsEmpty)
                && cell.Objects.Count > 0
                && cell.Objects.All(e => e.BlueprintName == "Grass" || ReferenceEquals(e, f.Player));
        }
    }
}
