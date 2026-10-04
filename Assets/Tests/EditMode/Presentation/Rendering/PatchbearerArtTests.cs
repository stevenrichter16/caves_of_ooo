using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>The flask is a view of actual carried medicine, never an invented
    /// item or a permanent decorative promise after that item is gone.</summary>
    public sealed class PatchbearerArtTests
    {
        const string Blueprint = "MarlbackPatchbearer";
        const string Full = "patchbearer-full", Empty = "patchbearer-empty";
        static Mesh Original() => ReferenceGladeVoxelLibrary.Load().ActorPaints.Single(p => p.ModelId == "ring-snapjaw").Painted;
        static GameObject Prefab(string id) => Resources.Load<GameObject>("Patchbearer3D/" + id);
        static Entity Actor(SpawnRing3DIntegrationFixture f)
        { Assert.True(f.Factory.Blueprints.ContainsKey(Blueprint), "Actual child must exist before native art acceptance."); return f.Add(Blueprint); }
        static Entity Medicine(Entity actor) => actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "HealingTonic");

        [Test]
        public void NativeSelfUseGestureSurvivesTheFullToEmptyBodyRefresh()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = Actor(f); f.Refresh();
                var emit = typeof(EntityVisualHooks).GetMethod("EmitSelfUse", BindingFlags.Static | BindingFlags.Public);
                Assert.NotNull(emit, "Self-use is an actual actor gesture, not a fictional spell or world target.");
                var views = (IDictionary)typeof(SpawnRing3DPresenter).GetField("views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Presenter);
                Assert.True(actor.GetPart<InventoryPart>().RemoveObject(Medicine(actor)));
                emit.Invoke(null, new object[] { actor, f.Zone });
                object before = views[actor]; var state = before.GetType().GetField("ActionState"); var until = before.GetType().GetField("ActionUntil");
                Assert.AreEqual("Interact", state.GetValue(before)); float deadline = (float)until.GetValue(before);
                f.Refresh(); Assert.True(f.Find(actor, out _, out var id)); Assert.AreEqual(Empty, id);
                Assert.AreEqual("Interact", state.GetValue(views[actor])); Assert.AreEqual(deadline, until.GetValue(views[actor]));
            }
        }

        [Test]
        public void OrdinarySpreadCaveShowsAdoptedFullAndEmptyArtThroughTheRealPresenter()
        {
            string id;
            using (var content = new EntityEquipmentContentFixture())
            {
                var manager = new OverworldZoneManager(content.Factory, 729490642);
                var column = BiomeCropPlan.CaveColumns(manager).Select(WorldMap.FromZoneID).First(p => manager.WorldMap.GetBiome(p.x, p.y) == BiomeType.Spread);
                id = WorldMap.ToZoneID(column.x, column.y, 3);
            }
            using (var f = new SpawnRing3DIntegrationFixture(id))
            {
                Assert.True(BiomeCropRecipes.IsOrdinaryCave(f.Zone)); Assert.False(SpreadPresentationScope.IsActive(f.Zone));
                var actor = Actor(f); f.Refresh(); var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.AreEqual(Full, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId);
                Assert.True(f.Rendered(actor)); Assert.True(presenter.TryGetApprovedStyle(actor, out var proof), proof.Failure);
                Assert.AreSame(Prefab(Full).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh, proof.SubmittedMesh);
                Assert.True(actor.GetPart<InventoryPart>().RemoveObject(Medicine(actor))); f.Refresh();
                Assert.True(f.Find(actor, out _, out var empty)); Assert.AreEqual(Empty, empty);
                Assert.True(presenter.TryGetApprovedStyle(actor, out proof), proof.Failure);
                var ordinary = f.Add("MarlbackGleaner"); f.Refresh();
                Assert.True(f.Rendered(ordinary));
                Assert.False(presenter.TryGetApprovedStyle(ordinary, out _), "The cave extension approves only the two actual patchbearer forms.");
            }
        }

        [Test]
        public void ActualMedicineShowsOneFullHarnessAndRemovingItShowsEmptyWithoutChangingTheActor()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = Actor(f); var medicine = Medicine(actor); var inventory = actor.GetPart<InventoryPart>();
                var before = inventory.Objects.ToArray(); var position = f.Zone.GetEntityPosition(actor); int hp = actor.GetStatValue("Hitpoints");
                f.Refresh(); Assert.True(f.Find(actor, out var view, out var id)); Assert.AreEqual(Full, id);
                var stature = view.transform.localScale;
                var leg = view.GetComponentInChildren<SkinnedMeshRenderer>().bones.Single(b => b.name == "Leg.L").position;
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor, out var proof), proof.Failure);
                Assert.AreSame(Prefab(Full).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh, proof.SubmittedMesh);
                CollectionAssert.AreEqual(before, inventory.Objects); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
                Assert.AreEqual(position, f.Zone.GetEntityPosition(actor)); Assert.True(f.Pick(actor, out _));
                Assert.True(inventory.RemoveObject(medicine)); f.Refresh();
                Assert.True(f.Find(actor, out var emptyView, out id)); Assert.AreEqual(Empty, id);
                Assert.AreEqual(stature, emptyView.transform.localScale, "A spent bottle cannot change actor stature.");
                Assert.AreEqual(leg, emptyView.GetComponentInChildren<SkinnedMeshRenderer>().bones.Single(b => b.name == "Leg.L").position, "Original foot rig stays planted across stock refresh.");
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor, out var emptyProof), emptyProof.Failure);
                Assert.AreSame(Prefab(Empty).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh, emptyProof.SubmittedMesh);
                Assert.True(inventory.AddObject(medicine)); f.Refresh(); Assert.True(f.Find(actor, out _, out id)); Assert.AreEqual(Full, id);
                Assert.AreEqual(hp, actor.GetStatValue("Hitpoints")); Assert.AreEqual(position, f.Zone.GetEntityPosition(actor));
            }
        }

        [TestCase("foreign-owner")][TestCase("depleted")][TestCase("different-tonic")]
        public void AFalseOrSpentInventoryEntryCannotDisplayAUsableFlask(string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = Actor(f); var medicine = Medicine(actor); f.Refresh();
                Assert.AreEqual(Full, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId);
                if (mutation == "foreign-owner") medicine.GetPart<PhysicsPart>().InInventory = f.Player;
                if (mutation == "depleted") medicine.GetPart<StackerPart>().StackCount = 0;
                if (mutation == "different-tonic") medicine.BlueprintName = "StrengthTonic";
                Assert.AreEqual(Empty, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId);
            }
        }

        [TestCase(Full)][TestCase(Empty)]
        public void HarnessAddsRealRigidGeometryWhilePreservingTheOriginalBodyAndFiveClips(string id)
        {
            var prefab = Prefab(id); Assert.NotNull(prefab, "Persistent original adopted art required.");
            var root = Object.Instantiate(prefab); Mesh baked = null;
            try
            {
                var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(); var mesh = skin.sharedMesh; var source = Original();
                Assert.AreNotSame(source, mesh); Assert.Greater(mesh.vertexCount, source.vertexCount + 23);
                CollectionAssert.AreEqual(source.vertices, mesh.vertices.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.uv, mesh.uv.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.boneWeights, mesh.boneWeights.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.bindposes, mesh.bindposes);
                Assert.AreEqual(9, skin.bones.Length); Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));
                var animator = root.GetComponentInChildren<Animator>(); var clips = animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[] { "Idle", "Walk", "Interact", "Attack", "Hit" }, clips.Select(c => c.name));
                baked = new Mesh();
                foreach (var clip in clips)
                {
                    clip.SampleAnimation(animator.gameObject, 0); skin.BakeMesh(baked); var before = baked.vertices;
                    clip.SampleAnimation(animator.gameObject, clip.length * .31f); skin.BakeMesh(baked);
                    Assert.True(before.Where((v, i) => (v - baked.vertices[i]).sqrMagnitude > .0000001f).Any(), clip.name);
                }
            }
            finally { if (baked != null) Object.DestroyImmediate(baked); Object.DestroyImmediate(root); }
        }

        [Test]
        public void EmptyHarnessRetainsItsActualCradleButHasNoBottleGeometry()
        {
            var full = Prefab(Full); var empty = Prefab(Empty); Assert.NotNull(full); Assert.NotNull(empty);
            var f = full.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh; var e = empty.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            Assert.Greater(e.vertexCount, Original().vertexCount + 23); Assert.Greater(f.vertexCount, e.vertexCount);
            CollectionAssert.AreEqual(e.vertices, f.vertices.Take(e.vertexCount));
            CollectionAssert.AreEqual(e.uv, f.uv.Take(e.vertexCount));
        }

        [TestCase("glyph")][TestCase("appearance")][TestCase("foreign-brain")]
        public void DifferentOrMalformedActorCannotBorrowPatchbearerIdentity(string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = Actor(f); Assert.AreEqual(Full, SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId);
                if (mutation == "glyph") actor.GetPart<RenderPart>().RenderString = "?";
                if (mutation == "appearance") actor.GetPart<RenderPart>().VisualID = "unrelated-body";
                if (mutation == "foreign-brain") actor.GetPart<BrainPart>().ParentEntity = f.Player;
                var id = SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId;
                Assert.That(id, Is.Not.EqualTo(Full).And.Not.EqualTo(Empty));
            }
        }

        [Test]
        public void OriginalGleanerAndScrabblerDoNotAcquireAnInventedMedicineHarness()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
                foreach (string bp in new[] { "MarlbackGleaner", "MarlbackScrabbler" })
                { var actor = f.Add(bp); Assert.AreEqual(bp == "MarlbackGleaner" ? "ring-marlback-gleaner" : "ring-snapjaw", SpawnRing3DRecipes.Resolve(f.Zone, actor, f.Library.Definition).ModelId); }
        }

        [Test]
        public void ActualPatchbearerCorpseKeepsSpeciesFormWithoutACarriedMedicinePromise()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var corpse = f.Factory.CreateEntity("MarlbackCorpse");
                corpse.Properties["SourceBlueprint"] = Blueprint; corpse.Properties["SourceID"] = "actual-test-subject";
                Assert.True(SpreadPortableRecipes.TryRecipe(corpse, out var id)); Assert.AreEqual("spread-portable-marlbackcorpse", id);
                corpse.BlueprintName = "CreatureCorpse"; Assert.False(SpreadPortableRecipes.TryRecipe(corpse, out _));
            }
        }
    }
}
