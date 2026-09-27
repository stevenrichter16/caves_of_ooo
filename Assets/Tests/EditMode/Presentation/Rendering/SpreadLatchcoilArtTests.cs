using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadLatchcoilArtTests
    {
        const string Blueprint = "SpreadLatchcoil", Model = "spread-latchcoil-viper";
        static GameObject Prefab() => Resources.Load<GameObject>("SpreadLatchcoil3D/" + Model);
        static SpreadBiomeActorLibrary.Entry Original() => SpreadBiomeActorLibrary.Load().Find("spread-biome-viper");

        [Test]
        public void CurrentChildUsesPersistentOriginalSerpentWithRealPaleGeometry()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = f.Add(Blueprint); f.Refresh(); Assert.AreEqual(8, actor.GetStatValue("Hitpoints"));
                Assert.That(actor.GetPart<InventoryPart>().GetAllEquipped(), Is.Empty);
                Assert.True(f.Find(actor, out var view, out string id)); Assert.AreEqual(Model, id);
                Assert.True(f.Rendered(actor)); Assert.True(f.Pick(actor, out _));
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor, out var proof), proof.Failure);
                var source = Original().Mesh; var mesh = Prefab().GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                Assert.AreSame(mesh, proof.ExpectedMesh); Assert.AreSame(mesh, proof.SubmittedMesh);
                Assert.Greater(mesh.vertexCount, source.vertexCount + 23);
                CollectionAssert.AreEqual(source.vertices, mesh.vertices.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.uv, mesh.uv.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.triangles, mesh.triangles.Take(source.triangles.Length));
                var added = mesh.vertices.Skip(source.vertexCount).ToArray();
                Assert.Greater(added.Max(v => v.y) - added.Min(v => v.y), .015f, "Raised broken bands have volume, not a recolored source.");
            }
        }

        [Test]
        public void AllFiveOriginalClipsMoveActualTwentyThreeBoneBodyAndAddedBands()
        {
            var prefab = Prefab(); Assert.NotNull(prefab); var root = Object.Instantiate(prefab); Mesh bake = null;
            try
            {
                var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(); var source = Original().Mesh;
                Assert.AreEqual(23, skin.bones.Length); Assert.True(skin.bones.All(b => b != null && b.IsChildOf(root.transform)));
                CollectionAssert.AreEqual(source.boneWeights, skin.sharedMesh.boneWeights.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.bindposes, skin.sharedMesh.bindposes);
                Assert.False(root.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith("Equipment.", StringComparison.Ordinal)));
                Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
                var animator = root.GetComponentInChildren<Animator>(); var clips = animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[] { "Idle", "Walk", "Interact", "Attack", "Hit" }, clips.Select(c => c.name));
                bake = new Mesh();
                foreach (var clip in clips)
                {
                    clip.SampleAnimation(animator.gameObject, 0); skin.BakeMesh(bake); var before = bake.vertices;
                    clip.SampleAnimation(animator.gameObject, clip.length * .31f); skin.BakeMesh(bake); var after = bake.vertices;
                    Assert.True(before.Take(source.vertexCount).Where((v, i) => (v - after[i]).sqrMagnitude > .0000001f).Any(), clip.name + " original body");
                    Assert.True(before.Skip(source.vertexCount).Where((v, i) => (v - after[source.vertexCount + i]).sqrMagnitude > .0000001f).Any(), clip.name + " physical bands");
                }
            }
            finally { if (bake != null) Object.DestroyImmediate(bake); Object.DestroyImmediate(root); }
        }

        [TestCase(false)][TestCase(true)]
        public void SleepingAndExplicitlyAwakenedGraphsKeepOneBodyThroughSave(bool wake)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = f.Add(Blueprint); var brain = actor.GetPart<BrainPart>(); var dormant = brain.FindGoal<DormantGoal>(); Assert.NotNull(dormant);
                if (wake) dormant.Wake(); var goals = brain.GetGoalsSnapshot().ToArray(); f.Refresh();
                CollectionAssert.AreEqual(goals, brain.GetGoalsSnapshot()); Assert.AreEqual(wake, dormant.Finished());
                Assert.True(f.Find(actor, out _, out var model)); Assert.AreEqual(Model, model);
                string id = actor.ID; var loaded = f.RoundTrip(); f.BindLoaded(loaded);
                var current = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id); Assert.AreNotSame(actor, current);
                Assert.AreEqual(wake, current.GetPart<BrainPart>().FindGoal<DormantGoal>().Finished());
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(current, out var proof), proof.Failure);
                Assert.AreEqual(Model, proof.ModelId); Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor, out _));
            }
        }

        [Test]
        public void ExactNaturalCorpseShellAndHarvestOddsArePreserved()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = f.Add(Blueprint); var config = actor.GetPart<CorpsePart>(); Assert.NotNull(config);
                Assert.AreEqual("CreatureCorpse", config.CorpseBlueprint); Assert.AreEqual(100, config.CorpseChance);
                Assert.AreEqual("VenomGland", config.HarvestBlueprint); Assert.AreEqual(1, config.HarvestMin); Assert.AreEqual(1, config.HarvestMax); Assert.AreEqual(75, config.HarvestChance);
                var corpse = f.Add(config.CorpseBlueprint); corpse.Properties["SourceID"] = actor.ID; corpse.Properties["SourceBlueprint"] = actor.BlueprintName;
                Assert.True(SpreadPortableRecipes.TryRecipe(corpse, out var model)); Assert.AreEqual("spread-portable-corpse-serpent", model);
                corpse.BlueprintName = "MarlbackCorpse"; Assert.False(SpreadPortableRecipes.TryRecipe(corpse, out _));
            }
        }

        [Test]
        public void CurrentHideMoveAndRemovalRemainNative()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = f.Add(Blueprint); f.Refresh(); Assert.True(f.Find(actor, out var view, out string id)); Assert.AreEqual(Model, id);
                var cell = f.FreeCell(); Assert.True(f.Zone.MoveEntity(actor, cell.x, cell.y)); f.Refresh();
                Assert.AreEqual(Village3DProjection.CellCentre(cell.x, cell.y), view.transform.position);
                f.Zone.GetEntityCell(actor).IsVisible = false; f.Refresh(); Assert.False(f.Rendered(actor));
                f.Zone.GetEntityCell(actor).IsVisible = true; f.Refresh(); Assert.True(f.Rendered(actor));
                Assert.True(f.Zone.RemoveEntity(actor)); f.Refresh(); Assert.False(f.Find(actor, out _, out _));
            }
        }

        [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("foreign-brain")]
        public void MalformedOrDifferentOwnerCannotClaimRareSerpent(string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = f.Add(Blueprint); var render = actor.GetPart<RenderPart>();
                if (mutation == "glyph") render.RenderString = "?";
                if (mutation == "color") render.ColorString = "&R";
                if (mutation == "visual") render.VisualID = "user-other-body";
                if (mutation == "foreign-brain") actor.GetPart<BrainPart>().ParentEntity = f.Player;
                Assert.AreNotEqual(Model, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId);
            }
        }

        [Test]
        public void ForeignBiomeDoesNotClaimScopedRareBody()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            { var actor = f.Add(Blueprint); Assert.AreNotEqual(Model, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId); }
        }

        [Test]
        public void OrdinaryViperKeepsItsOriginalModelAndMesh()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            { var actor = f.Add("Viper"); f.Refresh(); Assert.True(f.Find(actor, out _, out var model)); Assert.AreEqual("spread-biome-viper", model); Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor, out var proof), proof.Failure); Assert.AreSame(Original().Mesh, proof.ExpectedMesh); }
        }
    }
}
