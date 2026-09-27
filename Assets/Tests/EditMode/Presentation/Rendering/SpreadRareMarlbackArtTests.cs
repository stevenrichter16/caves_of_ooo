using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadRareMarlbackArtTests
    {
        static string Model(string blueprint) => blueprint=="SpreadHurdleCutter"?"spread-rare-hurdle-cutter":"spread-rare-ditch-mate";
        static GameObject Prefab(string blueprint) => Resources.Load<GameObject>("SpreadRareMarlback3D/"+Model(blueprint));
        static Mesh Original() => ReferenceGladeVoxelLibrary.Load().ActorPaints.Single(p=>p.ModelId=="ring-snapjaw").Painted;

        [TestCase("SpreadHurdleCutter")][TestCase("SpreadDitchMate")]
        public void ActualCurrentRareOwnerHasDistinctPersistentBodyAndRealAuthoredGear(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);var before=f.Zone.GetEntityPosition(actor);f.Refresh();
                Assert.That(f.Find(actor,out var view,out var id),Is.True);Assert.That(id,Is.EqualTo(Model(blueprint)));
                Assert.That(f.Rendered(actor),Is.True);Assert.That(f.Pick(actor,out _),Is.True);
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.That(presenter.TryGetApprovedStyle(actor,out var proof),Is.True,proof.Failure);
                Assert.That(proof.SubmittedMesh,Is.SameAs(Prefab(blueprint).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh));
                var equipped=actor.GetPart<InventoryPart>().EquippedItems.Values.Distinct().ToArray();
                CollectionAssert.AreEquivalent(blueprint=="SpreadHurdleCutter"?new[]{"ShortSword","LeatherCap"}:new[]{"Cudgel"},equipped.Select(e=>e.BlueprintName));
                foreach(var item in equipped)
                {
                    Assert.That(item.GetPart<PhysicsPart>().Equipped,Is.SameAs(actor));
                    Assert.That(presenter.TryGetApprovedEquipmentStyle(actor,item,out var gear),Is.True,gear.Failure);
                    Assert.That(gear.SubmittedMesh,Is.SameAs(gear.ExpectedMesh));
                }
                Assert.That(f.Zone.GetEntityPosition(actor),Is.EqualTo(before));
            }
        }

        [TestCase("SpreadHurdleCutter")][TestCase("SpreadDitchMate")]
        public void PhysicalVariantPreservesExactOriginalBodyAndAllFiveMovingClips(string blueprint)
        {
            var prefab=Prefab(blueprint);Assert.That(prefab,Is.Not.Null,"Persistent adopted rare body required.");
            var root=Object.Instantiate(prefab);Mesh baked=null;
            try
            {
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var source=Original();
                Assert.That(mesh,Is.Not.SameAs(source));Assert.That(mesh.vertexCount,Is.GreaterThan(source.vertexCount+23));
                CollectionAssert.AreEqual(source.vertices,mesh.vertices.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.uv,mesh.uv.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.boneWeights,mesh.boneWeights.Take(source.vertexCount));
                CollectionAssert.AreEqual(source.bindposes,mesh.bindposes);
                CollectionAssert.AreEqual(source.triangles,mesh.triangles.Take(source.triangles.Length));
                Assert.That(mesh.boneWeights.All(w=>w.weight0==1&&w.weight1==0&&w.weight2==0&&w.weight3==0&&w.boneIndex0>=0&&w.boneIndex0<skin.bones.Length),Is.True);
                Assert.That(skin.bones.All(b=>b!=null&&b.IsChildOf(root.transform)),Is.True);
                Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
                var animator=root.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},clips.Select(c=>c.name));
                baked=new Mesh();
                foreach(var clip in clips)
                {
                    clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);var before=baked.vertices;
                    clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked);var after=baked.vertices;
                    Assert.That(before.Where((v,i)=>(v-after[i]).sqrMagnitude>.0000001f).Any(),Is.True,clip.name);
                }
            }
            finally {if(baked!=null)Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
        }

        [Test]
        public void TwoVariantsHaveDifferentActualAddedSlateAndRakeGeometry()
        {
            var a=Prefab("SpreadHurdleCutter");var b=Prefab("SpreadDitchMate");Assert.NotNull(a);Assert.NotNull(b);
            var am=a.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var bm=b.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            Assert.That(am.vertices.Skip(Original().vertexCount).SequenceEqual(bm.vertices.Skip(Original().vertexCount)),Is.False);
            Assert.That(am,Is.Not.SameAs(bm));
        }

        [Test]
        public void ActualCutterLeatherCapUsesBroadLowMarlbackFit()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("SpreadHurdleCutter");f.Refresh();var root=f.View(actor);
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.That(presenter.TryGetApprovedStyle(actor,out var bodyProof),Is.True,bodyProof.Failure);
                var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.sharedMesh==bodyProof.SubmittedMesh);
                int head=Array.FindIndex(skin.bones,b=>b.name=="Head");Assert.That(head,Is.GreaterThanOrEqualTo(0));
                var toRoot=root.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
                var headBounds=Bounds(vertices.Where((v,i)=>weights[i].boneIndex0==head).Select(toRoot.MultiplyPoint3x4).ToArray());
                var cap=actor.GetPart<InventoryPart>().EquippedItems.Values.Distinct().Single(e=>e.BlueprintName=="LeatherCap");
                Assert.That(presenter.TryGetApprovedEquipmentStyle(actor,cap,out var gearProof),Is.True,gearProof.Failure);
                Assert.That(presenter.TryGetEquipmentView(actor,cap,out var gearRoot),Is.True);
                var gearSkin=gearRoot.GetComponentInChildren<SkinnedMeshRenderer>();
                var matrix=root.transform.worldToLocalMatrix*gearSkin.bones.Single().localToWorldMatrix*gearSkin.sharedMesh.bindposes.Single();
                var gearBounds=Bounds(gearSkin.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray());
                Assert.That(gearBounds.max.x,Is.GreaterThan(headBounds.max.x));
                Assert.That(gearBounds.min.x,Is.LessThan(headBounds.min.x));
                Assert.That(gearBounds.min.y,Is.GreaterThan(headBounds.center.y),"Face remains below the cap.");
            }
        }

        [TestCase("SpreadHurdleCutter")][TestCase("SpreadDitchMate")]
        public void MovementVisibilityAndRemovalStayOwnedByTheActualActor(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);f.Refresh();Assert.That(f.Find(actor,out var view,out var id),Is.True);Assert.That(id,Is.EqualTo(Model(blueprint)));
                var next=f.FreeCell();Assert.That(f.Zone.MoveEntity(actor,next.x,next.y),Is.True);f.Refresh();
                Assert.That(view.transform.position,Is.EqualTo(Village3DProjection.CellCentre(next.x,next.y)));
                f.Zone.GetEntityCell(actor).IsVisible=false;f.Refresh();Assert.That(f.Rendered(actor),Is.False);
                f.Zone.GetEntityCell(actor).IsVisible=true;f.Refresh();Assert.That(f.Rendered(actor),Is.True);
                Assert.That(f.Zone.RemoveEntity(actor),Is.True);f.Refresh();Assert.That(f.Find(actor,out _,out _),Is.False);
            }
        }

        [TestCase("SpreadHurdleCutter")][TestCase("SpreadDitchMate")]
        public void ExactCorpseProvenanceUsesMarlbackGroundForm(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var source=f.Add(blueprint);var corpse=f.Add("MarlbackCorpse");
                corpse.Properties["SourceBlueprint"]=source.BlueprintName;corpse.Properties["SourceID"]=source.ID;
                Assert.That(SpreadPortableRecipes.TryRecipe(corpse,out var id),Is.True);Assert.That(id,Is.EqualTo("spread-portable-marlbackcorpse"));
                corpse.BlueprintName="CreatureCorpse";
                Assert.That(SpreadPortableRecipes.TryRecipe(corpse,out _),Is.False,"Wrong species shell is not a rare corpse.");
            }
        }

        [TestCase("glyph")][TestCase("visual")][TestCase("foreign-brain")][TestCase("hidden")]
        public void MalformedOrDeliberatelyDifferentOwnerCannotClaimRareBody(string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("SpreadHurdleCutter");var render=actor.GetPart<RenderPart>();
                if(mutation=="glyph")render.RenderString="?";
                if(mutation=="visual")render.VisualID="user-authored-other-body";
                if(mutation=="foreign-brain")actor.GetPart<BrainPart>().ParentEntity=f.Player;
                if(mutation=="hidden")render.Visible=false;
                Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId,Is.Not.EqualTo(Model(actor.BlueprintName)));
            }
        }

        [TestCase("SpreadHurdleCutter")][TestCase("SpreadDitchMate")]
        public void ForeignBiomeDoesNotReceiveTheScopedRareStyle(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var actor=f.Add(blueprint);Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId,Is.Not.EqualTo(Model(blueprint)));}
        }

        [Test]
        public void ExistingOriginalScrabblerBodyAndCurrentStyleRemainUnchanged()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {var actor=f.Add("MarlbackScrabbler");f.Refresh();Assert.That(f.Find(actor,out _,out var id),Is.True);Assert.That(id,Is.EqualTo("ring-snapjaw"));Assert.That(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor,out var proof),Is.True,proof.Failure);Assert.That(proof.ExpectedMesh,Is.SameAs(Original()));}
        }
        static Bounds Bounds(Vector3[] points)
        {Assert.That(points,Is.Not.Empty);var result=new Bounds(points[0],Vector3.zero);foreach(var point in points)result.Encapsulate(point);return result;}
    }
}
