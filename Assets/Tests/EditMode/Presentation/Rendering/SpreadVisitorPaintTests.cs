using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadVisitorPaintTests
    {
        private static object Entry(string id)
        {
            var asset=Resources.Load<ScriptableObject>("SpreadVisitorPaint3D/Library");
            Assert.NotNull(asset,"Explicit persistent13-rig palette library required.");
            var method=asset.GetType().GetMethod("Find");Assert.NotNull(method);
            var entry=method.Invoke(asset,new object[]{id});Assert.NotNull(entry,id);return entry;
        }
        private static T Field<T>(object value,string name)
        {var f=value.GetType().GetField(name);Assert.NotNull(f,name);return(T)f.GetValue(value);}
        private static VoxelWorldMeshCatalog Voxel=>Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
        private static void SameGeometry(Mesh original,Mesh painted)
        {
            Assert.AreNotSame(original,painted);Assert.True(original.isReadable);Assert.True(painted.isReadable);
            Assert.AreEqual(original.bounds,painted.bounds);Assert.AreEqual(original.indexFormat,painted.indexFormat);
            CollectionAssert.AreEqual(original.vertices,painted.vertices);CollectionAssert.AreEqual(original.normals,painted.normals);
            CollectionAssert.AreEqual(original.tangents,painted.tangents);CollectionAssert.AreEqual(original.colors32,painted.colors32);
            CollectionAssert.AreEqual(original.boneWeights,painted.boneWeights);CollectionAssert.AreEqual(original.bindposes,painted.bindposes);
            Assert.AreEqual(original.subMeshCount,painted.subMeshCount);
            for(int n=0;n<original.subMeshCount;n++)
            {Assert.AreEqual(original.GetTopology(n),painted.GetTopology(n));CollectionAssert.AreEqual(original.GetIndices(n),painted.GetIndices(n));}
            Assert.AreEqual(original.vertexCount,painted.uv.Length);
            foreach(var uv in painted.uv)
            {Assert.AreEqual(.5f,uv.y);float at=uv.x*24-.5f;Assert.That(at,Is.InRange(-.00001f,23.00001f));Assert.Less(Math.Abs(at-Math.Round(at)),.00001);}
        }
        [TestCase("ChoirTendril","ring-choir-tendril")]
        [TestCase("Mosshulk","ring-mosshulk")]
        [TestCase("Rotling","ring-rotling")]
        [TestCase("MawToad","ring-maw-toad")]
        [TestCase("SariSnake","ring-sari-snake")]
        [TestCase("Wardline","ring-wardline")]
        [TestCase("CascadeFather","ring-cascade-father")]
        [TestCase("GlasspaneFrog","ring-glasspane-frog")]
        [TestCase("YellowfootWayfarer","ring-yellowfoot-wayfarer")]
        [TestCase("Shambler","ring-shambler")]
        [TestCase("GroveLanternMoth","ring-grove-lantern-moth")]
        [TestCase("SkySari","ring-sky-sari")]
        [TestCase("HelmwoodFrog","ring-helmwood-frog")]
        public void ExistingRigUsesExactPersistentScopedPaintWithoutChangingNativeOwner(string blueprint,string model)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=f.Add(blueprint);var position=f.Zone.GetEntityPosition(owner);string id=owner.ID;
                var render=owner.GetPart<RenderPart>();string glyph=render.RenderString,color=render.ColorString;var parts=owner.Parts.ToArray();
                var original=f.Library.FindModel(model);Assert.NotNull(original);var sourceSkin=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                var source=Voxel.Resolve(sourceSkin.sharedMesh);var vertices=source.vertices;var sourceUv=source.uv;var bounds=sourceSkin.localBounds;
                f.Refresh();Assert.True(f.Find(owner,out var root,out var actual),blueprint);Assert.AreEqual(model,actual);
                Assert.True(f.Rendered(owner));Assert.True(f.Pick(owner,out _));
                var entry=Entry(model);Assert.AreSame(original,Field<GameObject>(entry,"SourcePrefab"));Assert.AreSame(source,Field<Mesh>(entry,"Source"));
                var painted=Field<Mesh>(entry,"Painted");SameGeometry(source,painted);
                var owned=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();Assert.AreSame(painted,owned.sharedMesh);
                var expectedBounds=bounds;expectedBounds.Encapsulate(source.bounds.min);expectedBounds.Encapsulate(source.bounds.max);Assert.AreEqual(expectedBounds,owned.localBounds);
                CollectionAssert.AreEqual(sourceSkin.bones.Select(b=>b.name),owned.bones.Select(b=>b.name));
                Assert.AreEqual(sourceSkin.rootBone.name,owned.rootBone.name);
                var sourceAnimator=original.GetComponentInChildren<Animator>(true);var animator=root.GetComponentInChildren<Animator>(true);
                Assert.AreSame(sourceAnimator.avatar,animator.avatar);Assert.AreSame(sourceAnimator.runtimeAnimatorController,animator.runtimeAnimatorController);
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},animator.runtimeAnimatorController.animationClips.Select(c=>c.name));
                var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.True(presenter.TryGetApprovedStyle(owner,out var proof),proof.Failure);
                Assert.AreSame(painted,proof.ExpectedMesh);Assert.AreSame(painted,proof.SubmittedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(owner));Assert.AreEqual(id,owner.ID);Assert.AreEqual(glyph,render.RenderString);Assert.AreEqual(color,render.ColorString);CollectionAssert.AreEqual(parts,owner.Parts);
                render.Visible=false;f.Refresh();Assert.False(presenter.TryGetApprovedStyle(owner,out _));Assert.False(f.Rendered(owner));
                render.Visible=true;f.Refresh();Assert.True(presenter.TryGetApprovedStyle(owner,out _));
                Assert.True(f.Zone.RemoveEntity(owner));f.Refresh();Assert.False(f.Find(owner,out _,out _));Assert.False(presenter.TryGetApprovedStyle(owner,out _));
                CollectionAssert.AreEqual(vertices,source.vertices);CollectionAssert.AreEqual(sourceUv,source.uv);Assert.AreSame(sourceSkin.sharedMesh,original.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh);
            }
        }
        [TestCase("ChoirTendril","ring-choir-tendril")]
        [TestCase("Mosshulk","ring-mosshulk")]
        [TestCase("Rotling","ring-rotling")]
        [TestCase("MawToad","ring-maw-toad")]
        [TestCase("SariSnake","ring-sari-snake")]
        [TestCase("Wardline","ring-wardline")]
        [TestCase("CascadeFather","ring-cascade-father")]
        [TestCase("GlasspaneFrog","ring-glasspane-frog")]
        [TestCase("YellowfootWayfarer","ring-yellowfoot-wayfarer")]
        [TestCase("Shambler","ring-shambler")]
        [TestCase("GroveLanternMoth","ring-grove-lantern-moth")]
        [TestCase("SkySari","ring-sky-sari")]
        [TestCase("HelmwoodFrog","ring-helmwood-frog")]
        public void SameExistingRigInForeignBiomeKeepsNativeMeshAndPalette(string blueprint,string model)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.5.0"))
            {
                Assert.False(SpreadPresentationScope.IsActive(f.Zone));var owner=f.Add(blueprint);f.Refresh();
                Assert.True(f.Find(owner,out var root,out var actual),blueprint);Assert.AreEqual(model,actual);
                var prefab=f.Library.FindModel(model);var source=prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.AreSame(Voxel.Resolve(source.sharedMesh),root.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh);
                Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
            }
        }
    }
}
