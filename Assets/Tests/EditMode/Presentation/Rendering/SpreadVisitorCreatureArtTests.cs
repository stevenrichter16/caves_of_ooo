using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadVisitorCreatureArtTests
    {
        public static IEnumerable<string> Blueprints => SpreadVisitorCreatureSource.Specs.Select(x=>x.Blueprint);
        [TestCaseSource(nameof(Blueprints))]
        public void CurrentVisitorOwnsExactPersistentBodyAndPaletteWithoutNativeMutation(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);var position=f.Zone.GetEntityPosition(actor);var id=actor.ID;
                var render=actor.GetPart<RenderPart>();string glyph=render.RenderString,color=render.ColorString;
                int goals=actor.GetPart<BrainPart>().GoalCount;
                var expected=SpreadVisitorCreatureSource.ForBlueprint(blueprint);f.Refresh();
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);
                Assert.AreEqual(expected.Id,recipe.ModelId);Assert.True(recipe.Transient);Assert.False(recipe.Batched);
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(actor,out var proof),proof.Failure);
                var library=SpreadVisitorCreatureLibrary.Load();var entry=library.Find(expected.Id);
                Assert.AreSame(entry.Mesh,proof.ExpectedMesh);Assert.AreSame(entry.Mesh,proof.SubmittedMesh);
                Assert.AreSame(library.Material,proof.ExpectedMaterial);Assert.AreSame(library.Palette,proof.SubmittedMaterial.GetTexture("_BaseMap"));
                Assert.True(f.Pick(actor,out _));Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                Assert.AreEqual(position,f.Zone.GetEntityPosition(actor));Assert.AreEqual(id,actor.ID);
                Assert.AreEqual(glyph,render.RenderString);Assert.AreEqual(color,render.ColorString);Assert.AreEqual(goals,actor.GetPart<BrainPart>().GoalCount);
                Assert.True(f.Zone.RemoveEntity(actor));f.Refresh();Assert.False(presenter.TryGetApprovedStyle(actor,out _));Assert.False(f.Rendered(actor));
            }
        }
        [TestCaseSource(nameof(Blueprints))]
        public void EachAuthoredVisitorHasActualFiveMovingNativeClipsAndPreservedSource(string blueprint)
        {
            var spec=SpreadVisitorCreatureSource.ForBlueprint(blueprint);var library=SpreadVisitorCreatureLibrary.Load();Assert.NotNull(library);
            library.Validate();var entry=library.Find(spec.Id);var root=Object.Instantiate(entry.Prefab);Mesh baked=null;
            try
            {
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var animator=root.GetComponentInChildren<Animator>();
                var mesh=skin.sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;var weights=mesh.boneWeights;var poses=mesh.bindposes;
                CollectionAssert.AreEquivalent(spec.Bones,skin.bones.Select(x=>x.name));
                CollectionAssert.AreEquivalent(SpreadVisitorCreatureSource.Clips,animator.runtimeAnimatorController.animationClips.Select(x=>x.name));
                foreach(string socket in spec.Sockets)Assert.AreEqual(1,root.GetComponentsInChildren<Transform>().Count(t=>t.name==socket));
                baked=new Mesh();
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                {
                    clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked,true);var before=baked.vertices.Select(skin.transform.TransformPoint).ToArray();
                    clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked,true);var after=baked.vertices.Select(skin.transform.TransformPoint).ToArray();
                    Assert.True(before.Where((v,i)=>(v-after[i]).sqrMagnitude>.0000001f).Any(),blueprint+"/"+clip.name+" must deform actual weighted vertices.");
                    skin.BakeMesh(baked,true);CollectionAssert.AreEqual(after,baked.vertices.Select(skin.transform.TransformPoint).ToArray(),"Repeated same pose is stationary.");
                }
                CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(uv,mesh.uv);CollectionAssert.AreEqual(weights,mesh.boneWeights);CollectionAssert.AreEqual(poses,mesh.bindposes);
            }
            finally{if(baked!=null)Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
        }
        [TestCase("glyph")][TestCase("portable")][TestCase("hidden")][TestCase("foreign-render")]
        [TestCase("foreign-physics")][TestCase("foreign-brain")][TestCase("visual-override")][TestCase("removed")]
        public void UnknownOrForeignCurrentIdentityCannotClaimAVisitorBody(string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("CaveBear");var render=actor.GetPart<RenderPart>();
                switch(mutation)
                {
                    case "glyph":render.RenderString="?";break;
                    case "portable":actor.GetPart<PhysicsPart>().Takeable=true;break;
                    case "hidden":render.Visible=false;break;
                    case "foreign-render":render.ParentEntity=f.Player;break;
                    case "foreign-physics":actor.GetPart<PhysicsPart>().ParentEntity=f.Player;break;
                    case "foreign-brain":actor.GetPart<BrainPart>().ParentEntity=f.Player;break;
                    case "visual-override":render.VisualID="arbitrary-disguise";break;
                    case "removed":Assert.True(f.Zone.RemoveEntity(actor));break;
                }
                Assert.AreNotEqual(SpreadVisitorCreatureSource.ForBlueprint("CaveBear").Id,SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
            }
        }
        [TestCase("CaveBat")][TestCase("GlassScorpion")][TestCase("SleepingTroll")][TestCase("SkeletalSentry")]
        public void ExistingForeignBiomeDoesNotAcquireScopedVisitorRecipe(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var actor=f.Add(blueprint);Assert.AreNotEqual(SpreadVisitorCreatureSource.ForBlueprint(blueprint).Id,SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);}
        }
        [Test]public void ActualSentryHelmetFollowsItsRealNativeBodyAndOwnedHeadBone()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("SkeletalSentry");var item=actor.GetPart<InventoryPart>().GetAllEquipped().Single(x=>x.BlueprintName=="IronHelmet");
                Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedEquipmentStyle(actor,item,out var proof),proof.Failure);
                Assert.True(presenter.TryGetEquipmentView(actor,item,out var view));
                Assert.True(view.GetComponentsInChildren<SkinnedMeshRenderer>().All(s=>s.bones.All(b=>b.IsChildOf(f.View(actor).transform))));
            }
        }
        [TestCase("nan-y")][TestCase("infinite-y")][TestCase("nan-x")][TestCase("finite-control")]
        public void PaletteValidationRejectsNonfiniteCoordinatesWithoutChangingBorrowedMesh(string mutation)
        {
            var library=Object.Instantiate(SpreadVisitorCreatureLibrary.Load());GameObject clone=null;Mesh owned=null;
            try
            {
                var entry=library.Entries[0];var borrowed=entry.Mesh;var original=borrowed.uv;
                clone=Object.Instantiate(entry.Prefab);entry.Prefab=clone;owned=Object.Instantiate(borrowed);entry.Mesh=owned;
                clone.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh=owned;var uv=owned.uv;
                if(mutation=="nan-y")uv[0].y=float.NaN;
                if(mutation=="infinite-y")uv[0].y=float.PositiveInfinity;
                if(mutation=="nan-x")uv[0].x=float.NaN;
                owned.uv=uv;
                if(mutation=="finite-control")Assert.DoesNotThrow(()=>library.Validate());
                else Assert.Throws<InvalidOperationException>(()=>library.Validate());
                CollectionAssert.AreEqual(original,borrowed.uv);
            }
            finally{if(clone!=null)Object.DestroyImmediate(clone);if(owned!=null)Object.DestroyImmediate(owned);Object.DestroyImmediate(library);}
        }
        [Test]public void MatchingNameForeignBoneCannotValidateAsPersistentVisitorAnatomy()
        {
            var library=Object.Instantiate(SpreadVisitorCreatureLibrary.Load());GameObject clone=null,foreign=null;
            try
            {
                var entry=library.Entries[0];clone=Object.Instantiate(entry.Prefab);entry.Prefab=clone;
                var skin=clone.GetComponentInChildren<SkinnedMeshRenderer>();var bones=skin.bones;foreign=new GameObject(bones[0].name);bones[0]=foreign.transform;skin.bones=bones;
                Assert.Throws<InvalidOperationException>(()=>library.Validate());
            }
            finally{if(foreign!=null)Object.DestroyImmediate(foreign);if(clone!=null)Object.DestroyImmediate(clone);Object.DestroyImmediate(library);}
        }
    }
}
