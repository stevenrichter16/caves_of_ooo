using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Owned instance / borrowed asset boundary and adversarial catalog
    /// inputs. No Resources asset or live native graph is modified by these tests.</summary>
    public sealed class VoxelWorldPresentationAdversarialTests
    {
        readonly List<Object> owned = new List<Object>();
        [TearDown] public void TearDown()
        { for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.DestroyImmediate(owned[i]);owned.Clear(); }
        Mesh Triangle(string name)
        {
            var mesh=new Mesh{name=name,vertices=new[]{Vector3.zero,Vector3.right,Vector3.up},triangles=new[]{0,1,2}};
            mesh.RecalculateBounds();owned.Add(mesh);return mesh;
        }
        GameObject Root(string name="owned instance") {var root=new GameObject(name);owned.Add(root);return root;}
        VoxelWorldMeshCatalog Catalog(int count=1)
        {
            var catalog=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();owned.Add(catalog);
            catalog.Bindings=new VoxelWorldMeshCatalog.Binding[count];
            for(int i=0;i<count;i++)catalog.Bindings[i]=new VoxelWorldMeshCatalog.Binding
                {Source=Triangle("source"+i),Voxel=Triangle("voxel"+i),VoxelSize=.25f,WorldVoxelSize=.25f,SourceKey="owned-source_"+i};
            return catalog;
        }
        static VoxelWorldPresentation Adapter(VoxelWorldMeshCatalog catalog)
        {
            try {return (VoxelWorldPresentation)Activator.CreateInstance(typeof(VoxelWorldPresentation),BindingFlags.Instance|BindingFlags.NonPublic,
                null,new object[]{"Overworld.3.7.0",catalog},null);}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        static MeshFilter Filter(GameObject root,Mesh mesh)
        {var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=mesh;return filter;}

        [Test] public void Adversarial_RepeatedPrepareReplacesOnceAndDoesNotMutateBorrowedVertices()
        {
            var c=Catalog();var a=Adapter(c);var before=c.Bindings[0].Source.vertices;var root=Root();var f=Filter(root,c.Bindings[0].Source);
            Assert.AreEqual(1,a.Apply(root));Assert.AreSame(c.Bindings[0].Voxel,f.sharedMesh);
            Assert.AreEqual(0,a.Apply(root));Assert.AreEqual(1,a.AppliedMeshCount);Assert.AreEqual(0,a.MissingMeshCount);
            CollectionAssert.AreEqual(before,c.Bindings[0].Source.vertices);
        }
        [Test] public void Adversarial_TwoInstancesShareReplacementWithoutChangingTheSourceTemplate()
        {
            var c=Catalog();var a=Adapter(c);var template=Filter(Root("source template"),c.Bindings[0].Source);
            var first=Filter(Root(),template.sharedMesh);var second=Filter(Root(),template.sharedMesh);
            Assert.AreEqual(1,a.Apply(first.gameObject));Assert.AreSame(c.Bindings[0].Source,second.sharedMesh);
            Assert.AreEqual(1,a.Apply(second.gameObject));Assert.AreSame(first.sharedMesh,second.sharedMesh);
            Assert.AreSame(c.Bindings[0].Source,template.sharedMesh);Assert.AreEqual(2,a.AppliedMeshCount);
        }
        [Test] public void Adversarial_InactiveChildrenAreConvertedBeforeTheyBecomeVisible()
        {
            var c=Catalog();var a=Adapter(c);var root=Root();var child=Root();child.transform.SetParent(root.transform,false);child.SetActive(false);
            var f=Filter(child,c.Bindings[0].Source);Assert.AreEqual(1,a.Apply(root));Assert.AreSame(c.Bindings[0].Voxel,f.sharedMesh);Assert.IsFalse(child.activeSelf);
        }
        [Test] public void Adversarial_UnknownMeshesKeepTheirOwnIdentityAndCountByReference()
        {
            var a=Adapter(Catalog());var first=Triangle("same-name");var second=Triangle("same-name");
            Assert.AreSame(first,a.Resolve(first));Assert.AreSame(first,a.Resolve(first));Assert.AreEqual(1,a.MissingMeshCount);
            Assert.AreSame(second,a.Resolve(second));Assert.AreEqual(2,a.MissingMeshCount);Assert.AreEqual(0,a.AppliedMeshCount);
        }
        [Test] public void Adversarial_NullAndAlreadyGeneratedMeshesAreNotMissingContent()
        {
            var c=Catalog();var a=Adapter(c);Assert.IsNull(a.Resolve(null));Assert.AreSame(c.Bindings[0].Voxel,a.Resolve(c.Bindings[0].Voxel));
            Assert.AreEqual(0,a.AppliedMeshCount);Assert.AreEqual(0,a.MissingMeshCount);
        }
        [Test] public void Adversarial_NullInstanceRejectsButAnEmptyOwnedInstanceIsHarmless()
        {var a=Adapter(Catalog());Assert.Throws<ArgumentNullException>(()=>a.Apply(null));Assert.AreEqual(0,a.Apply(Root()));}
        [Test] public void Adversarial_SkinRetainsItsExactBonesRootAndAnimationEnvelope()
        {
            var c=Catalog();c.Bindings[0].SourceBindposes=new[]{Matrix4x4.identity};
            foreach(var mesh in new[]{c.Bindings[0].Source,c.Bindings[0].Voxel})
            {mesh.bindposes=new[]{Matrix4x4.identity};mesh.boneWeights=new[]{new BoneWeight{weight0=1},new BoneWeight{weight0=1},new BoneWeight{weight0=1}};}
            var a=Adapter(c);var root=Root();var bone=Root("borrowed bone");bone.transform.SetParent(root.transform,false);
            var skin=root.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=c.Bindings[0].Source;skin.rootBone=bone.transform;skin.bones=new[]{bone.transform};
            var envelope=new Bounds(new Vector3(0,2,0),new Vector3(8,8,8));skin.localBounds=envelope;
            Assert.AreEqual(1,a.Apply(root));Assert.AreSame(c.Bindings[0].Voxel,skin.sharedMesh);Assert.AreSame(bone.transform,skin.rootBone);
            CollectionAssert.AreEqual(new[]{bone.transform},skin.bones);Assert.AreEqual(envelope,skin.localBounds);Assert.AreEqual(0,a.Apply(root));
        }
        [Test] public void Adversarial_PreexistingCollisionMeshIsNotMistakenForSimulationOwnership()
        {
            var c=Catalog();var a=Adapter(c);var root=Root();var f=Filter(root,c.Bindings[0].Source);
            var collision=root.AddComponent<MeshCollider>();collision.sharedMesh=c.Bindings[0].Source;
            a.Apply(root);Assert.AreSame(c.Bindings[0].Voxel,f.sharedMesh);Assert.AreSame(c.Bindings[0].Source,collision.sharedMesh);
        }
        [Test] public void Adversarial_LateInvalidRowRejectsBeforeAnyOwnedInstanceCanBeChanged()
        {
            var c=Catalog(2);var root=Root();var filter=Filter(root,c.Bindings[0].Source);c.Bindings[1].Voxel=null;
            Assert.Throws<InvalidOperationException>(()=>Adapter(c));Assert.AreSame(c.Bindings[0].Source,filter.sharedMesh);
        }
        [Test] public void Adversarial_IndependentBindingsKeepIndependentCoverageCounters()
        {
            var c=Catalog();var first=Adapter(c);var second=Adapter(c);first.Resolve(c.Bindings[0].Source);
            Assert.AreEqual(1,first.AppliedMeshCount);Assert.AreEqual(0,second.AppliedMeshCount);
            second.Resolve(Triangle("unknown"));Assert.AreEqual(0,first.MissingMeshCount);Assert.AreEqual(1,second.MissingMeshCount);
        }
        [TestCase("../foreign")][TestCase("../../foreign")][TestCase("dir/file")][TestCase("dir\\file")]
        [TestCase("/absolute")][TestCase("\\absolute")][TestCase(".")][TestCase("..")]
        [TestCase("file:name")][TestCase(" name")][TestCase("name ")]
        public void Adversarial_SourceKeysCannotEscapeOrAliasGeneratedAssetNames(string key)
        {
            var c=Catalog();Assert.DoesNotThrow(c.Validate,"Safe single-component key is the identical positive control.");
            c.Bindings[0].SourceKey=key;
            Assert.Throws<InvalidOperationException>(c.Validate,"Toolkit publication concatenates SourceKey into an asset destination.");
        }
        [Test] public void Adversarial_TwoSourcesCannotPublishToTheSameOutputKey()
        {
            var c=Catalog(2);Assert.DoesNotThrow(c.Validate);c.Bindings[1].SourceKey=c.Bindings[0].SourceKey;
            Assert.Throws<InvalidOperationException>(c.Validate,"Duplicate output keys collapse distinct source models during asset publication.");
        }
    }
}
