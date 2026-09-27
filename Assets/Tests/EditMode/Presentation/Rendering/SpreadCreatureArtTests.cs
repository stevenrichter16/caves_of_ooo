using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadCreatureArtTests
    {
        static string Id(string bp)=>bp=="MimicAwake"?"spread-creature-mimic-awake":bp=="GiantSpider"?"spread-creature-giant-spider":bp=="JungleApe"?"spread-creature-jungle-ape":bp=="MimicChest"?"spread-creature-mimic-closed":"spread-creature-glowmaw";
        static GameObject Prefab(string bp)=>Resources.Load<GameObject>("SpreadCreature3D/Actors/"+Id(bp));
        static Entity Add(SpawnRing3DIntegrationFixture f,string bp)
        {var e=f.Add(bp);if(bp=="Glowmaw"){e.GetPart<GlowmawAmbushPart>().HasDropped=true;e.GetPart<RenderPart>().Visible=true;}return e;}
        [TestCase("GiantSpider")][TestCase("JungleApe")][TestCase("MimicChest")][TestCase("Glowmaw")]
        public void CurrentManagedSpreadAnimalHasItsOwnActualRiggedBody(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,bp);var position=f.Zone.GetEntityPosition(actor);var ownerId=actor.ID;
                f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);
                Assert.AreEqual(Id(bp),recipe.ModelId);Assert.True(recipe.Transient);Assert.False(recipe.Batched);
                Assert.True(f.Find(actor,out var root,out var model));Assert.AreEqual(Id(bp),model);Assert.True(f.Rendered(actor));Assert.True(f.Pick(actor,out _));
                var adopted=Prefab(bp);Assert.NotNull(adopted);var source=adopted.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.AreSame(source.sharedMesh,root.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(actor));Assert.AreEqual(ownerId,actor.ID);
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"),"Already-authored cuboids must not enter a coarser fallback bake.");
            }
        }
        [TestCase("GiantSpider",.30f,.75f)][TestCase("JungleApe",1.05f,1.50f)][TestCase("MimicChest",.40f,.80f)][TestCase("MimicAwake",.55f,.95f)][TestCase("Glowmaw",.40f,.85f)]
        public void PersistentOriginalBodyHasActualFiveMovingClipsAndCorrectAnatomy(string bp,float low,float high)
        {
            var prefab=Prefab(bp);Assert.NotNull(prefab,"Actual adopted persistent body is required, not source JSON.");
            var root=Object.Instantiate(prefab);Mesh baked=null;
            try
            {
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>(true);Assert.NotNull(skin);var mesh=skin.sharedMesh;Assert.NotNull(mesh);Assert.True(mesh.isReadable);
                // Imported culling bounds include animation reach and are deliberately
                // larger than the actual authored body. Pin visible bind geometry.
                float visibleHeight = BindHeight(skin);
                Assert.That(visibleHeight, Is.InRange(low,high));
                var culling = skin.localBounds;
                skin.localBounds = new Bounds(Vector3.zero,Vector3.one*40);
                Assert.AreEqual(visibleHeight,BindHeight(skin),.000001f,"Culling expansion cannot change authored geometry.");
                skin.localBounds = culling;
                Assert.Greater(mesh.vertexCount,100);Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));
                var animator=root.GetComponentInChildren<Animator>(true);Assert.NotNull(animator);Assert.NotNull(animator.avatar);Assert.True(animator.avatar.isValid);Assert.False(animator.applyRootMotion);
                Assert.NotNull(animator.runtimeAnimatorController);var clips=animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},clips.Select(c=>c.name));
                var names=skin.bones.Select(b=>b.name).ToArray();
                if(bp=="GiantSpider")Assert.AreEqual(8,names.Count(n=>n.StartsWith("Leg.",StringComparison.Ordinal)));
                if(bp=="JungleApe"){Assert.Contains("Arm.L",names);Assert.Contains("Hand.R",names);Assert.False(names.Contains("Tail"));}
                if(bp=="MimicChest"||bp=="MimicAwake")Assert.Contains("Head",names);
                if(bp=="Glowmaw")Assert.AreEqual(4,names.Count(n=>n.StartsWith("Arm.",StringComparison.Ordinal)));
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
        private static float BindHeight(SkinnedMeshRenderer skin)
        {
            float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
            foreach (var vertex in skin.sharedMesh.vertices)
            {
                float y=skin.transform.TransformPoint(vertex).y;
                minimum=Mathf.Min(minimum,y);maximum=Mathf.Max(maximum,y);
            }
            return maximum-minimum;
        }
        [TestCase("GiantSpider")][TestCase("JungleApe")][TestCase("MimicChest")][TestCase("Glowmaw")]
        public void ExistingForeignBiomeDoesNotAcquireThisScopedRecipe(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var actor=Add(f,bp);var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);Assert.AreNotEqual(Id(bp),recipe.ModelId);}
        }
        [TestCase("GiantSpider","glyph")][TestCase("JungleApe","glyph")][TestCase("MimicChest","glyph")][TestCase("Glowmaw","glyph")]
        [TestCase("GiantSpider","portable")][TestCase("JungleApe","portable")][TestCase("MimicChest","portable")][TestCase("Glowmaw","portable")]
        [TestCase("GiantSpider","hidden")][TestCase("JungleApe","hidden")][TestCase("MimicChest","hidden")][TestCase("Glowmaw","hidden")]
        public void MalformedOrHiddenCurrentOwnerCannotClaimOriginalBody(string bp,string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,bp);var render=actor.GetPart<RenderPart>();
                if(mutation=="glyph")render.RenderString="?";else if(mutation=="portable")actor.GetPart<PhysicsPart>().Takeable=true;else render.Visible=false;
                Assert.AreNotEqual(Id(bp),SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
            }
        }
        [TestCase("GiantSpider")][TestCase("JungleApe")][TestCase("MimicChest")][TestCase("Glowmaw")]
        public void MovementHideAndRemovalFollowTheActualOwnerWithoutChangingSharedGeometry(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,bp);f.Refresh();Assert.True(f.Find(actor,out var root,out var id));Assert.AreEqual(Id(bp),id);
                var mesh=root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var vertices=mesh.vertices;var next=f.FreeCell();
                Assert.True(f.Zone.MoveEntity(actor,next.x,next.y));f.Refresh();Assert.AreEqual(Village3DProjection.CellCentre(next.x,next.y),root.transform.position);
                var cell=f.Zone.GetEntityCell(actor);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(actor));
                cell.IsVisible=true;f.Refresh();Assert.True(f.Rendered(actor));f.Zone.RemoveEntity(actor);f.Refresh();Assert.False(f.Find(actor,out _,out _));Assert.True(root==null);
                CollectionAssert.AreEqual(vertices,mesh.vertices);
            }
        }
        [Test]public void MimicDisguiseTracksTheActualDormantGoalWithoutMutatingIt()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {var actor=f.Add("MimicChest");var brain=actor.GetPart<BrainPart>();var goal=brain.FindGoal<DormantGoal>();Assert.NotNull(goal);Assert.False(goal.Finished());int count=brain.GoalCount;f.Refresh();
             Assert.AreEqual("spread-creature-mimic-closed",SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);var closed=f.View(actor);Assert.AreEqual(count,brain.GoalCount);Assert.False(goal.Finished());
             goal.Wake();f.Refresh();Assert.AreEqual("spread-creature-mimic-awake",SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);Assert.True(closed==null);Assert.NotNull(f.View(actor));Assert.AreEqual(count,brain.GoalCount);Assert.True(goal.Finished());}
        }
        [TestCase(false)][TestCase(true)]public void UndroppedOrHiddenGlowmawNeverRevealsItsBody(bool visible)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {var actor=f.Add("Glowmaw");actor.GetPart<RenderPart>().Visible=visible;Assert.False(actor.GetPart<GlowmawAmbushPart>().HasDropped);f.Refresh();Assert.AreNotEqual(Id("Glowmaw"),SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);Assert.False(f.Find(actor,out _,out _));Assert.False(actor.GetPart<GlowmawAmbushPart>().HasDropped);}
        }
    }
}
