using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadBiomeAnimalArtTests
    {
        static string Id(string bp)=>bp=="Magpie"?"spread-biome-magpie":bp=="PetDog"?"spread-biome-pet-dog":"spread-biome-viper";
        static GameObject Prefab(string bp)=>Resources.Load<GameObject>("SpreadBiome3D/Actors/"+Id(bp));
        [TestCase("Magpie")][TestCase("PetDog")][TestCase("Viper")]
        public void CurrentManagedSpreadAnimalHasItsOwnActualRiggedBody(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(bp);var position=f.Zone.GetEntityPosition(actor);var ownerId=actor.ID;
                f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);
                Assert.AreEqual(Id(bp),recipe.ModelId);Assert.True(recipe.Transient);Assert.False(recipe.Batched);
                Assert.True(f.Find(actor,out var root,out var model));Assert.AreEqual(Id(bp),model);Assert.True(f.Rendered(actor));Assert.True(f.Pick(actor,out _));
                var adopted=Prefab(bp);Assert.NotNull(adopted);var source=adopted.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.AreSame(source.sharedMesh,root.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(actor));Assert.AreEqual(ownerId,actor.ID);
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"),"Already-authored cuboids must not enter a coarser fallback bake.");
            }
        }
        [TestCase("Magpie",.35f,.85f)][TestCase("PetDog",.45f,.90f)][TestCase("Viper",.08f,.35f)]
        public void PersistentOriginalBodyHasActualFiveMovingClipsAndCorrectAnatomy(string bp,float low,float high)
        {
            var prefab=Prefab(bp);Assert.NotNull(prefab,"Actual adopted persistent body is required, not source JSON.");
            var root=Object.Instantiate(prefab);Mesh baked=null;
            try
            {
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>(true);Assert.NotNull(skin);var mesh=skin.sharedMesh;Assert.NotNull(mesh);Assert.True(mesh.isReadable);
                Assert.That(skin.bounds.size.y,Is.InRange(low,high));Assert.Greater(mesh.vertexCount,100);Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));
                var animator=root.GetComponentInChildren<Animator>(true);Assert.NotNull(animator);Assert.NotNull(animator.avatar);Assert.True(animator.avatar.isValid);Assert.False(animator.applyRootMotion);
                Assert.NotNull(animator.runtimeAnimatorController);var clips=animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},clips.Select(c=>c.name));
                var names=skin.bones.Select(b=>b.name).ToArray();
                if(bp=="Magpie"){Assert.Contains("Wing.L",names);Assert.Contains("Wing.R",names);Assert.Contains("Tail",names);}
                if(bp=="PetDog")Assert.AreEqual(4,names.Count(n=>n.StartsWith("Leg",StringComparison.Ordinal)));
                if(bp=="Viper"){Assert.GreaterOrEqual(names.Count(n=>n.StartsWith("Coil.",StringComparison.Ordinal)),8);Assert.False(names.Any(n=>n.StartsWith("Leg")||n.StartsWith("Wing")));}
                var vertices=mesh.vertices;var uvs=mesh.uv;var weights=mesh.boneWeights;var bindposes=mesh.bindposes;
                Assert.AreEqual(mesh.vertexCount,weights.Length);Assert.AreEqual(skin.bones.Length,bindposes.Length);
                foreach(var w in weights){Assert.That(w.boneIndex0,Is.InRange(0,skin.bones.Length-1));Assert.AreEqual(1,w.weight0);}
                baked=new Mesh();
                foreach(var clip in clips)
                {
                    Assert.Greater(clip.length,0);clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);var before=baked.vertices;
                    clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked);var after=baked.vertices;
                    Assert.True(before.Where((v,i)=>(v-after[i]).sqrMagnitude>.0000001f).Any(),bp+"/"+clip.name+" must visibly move its real skinned vertices");
                }
                CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(uvs,mesh.uv);CollectionAssert.AreEqual(weights,mesh.boneWeights);CollectionAssert.AreEqual(bindposes,mesh.bindposes);
            }
            finally{if(baked!=null)Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
        }
        [TestCase("Magpie")][TestCase("PetDog")][TestCase("Viper")]
        public void ExistingForeignBiomeDoesNotAcquireThisScopedRecipe(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var actor=f.Add(bp);var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);Assert.AreNotEqual(Id(bp),recipe.ModelId);}
        }
        [TestCase("Magpie","glyph")][TestCase("PetDog","glyph")][TestCase("Viper","glyph")]
        [TestCase("Magpie","portable")][TestCase("PetDog","portable")][TestCase("Viper","portable")]
        [TestCase("Magpie","hidden")][TestCase("PetDog","hidden")][TestCase("Viper","hidden")]
        public void MalformedOrHiddenCurrentOwnerCannotClaimOriginalBody(string bp,string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(bp);var render=actor.GetPart<RenderPart>();
                if(mutation=="glyph")render.RenderString="?";else if(mutation=="portable")actor.GetPart<PhysicsPart>().Takeable=true;else render.Visible=false;
                Assert.AreNotEqual(Id(bp),SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
            }
        }
        [TestCase("Magpie")][TestCase("PetDog")][TestCase("Viper")]
        public void MovementHideAndRemovalFollowTheActualOwnerWithoutChangingSharedGeometry(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(bp);f.Refresh();Assert.True(f.Find(actor,out var root,out var id));Assert.AreEqual(Id(bp),id);
                var mesh=root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var vertices=mesh.vertices;var next=f.FreeCell();
                Assert.True(f.Zone.MoveEntity(actor,next.x,next.y));f.Refresh();Assert.AreEqual(Village3DProjection.CellCentre(next.x,next.y),root.transform.position);
                var cell=f.Zone.GetEntityCell(actor);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(actor));
                cell.IsVisible=true;f.Refresh();Assert.True(f.Rendered(actor));f.Zone.RemoveEntity(actor);f.Refresh();Assert.False(f.Find(actor,out _,out _));Assert.True(root==null);
                CollectionAssert.AreEqual(vertices,mesh.vertices);
            }
        }
    }
}
