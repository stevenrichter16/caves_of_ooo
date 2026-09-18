using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Topology, rigid skin seams, malformed mappings and transform
    /// counterexamples beyond the initial mesh-conversion happy paths.</summary>
    public sealed class VoxelWorldMeshAdversarialTests
    {
        readonly List<Object> owned=new List<Object>();
        T Own<T>(T value)where T:Object{owned.Add(value);return value;}
        [TearDown]public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.DestroyImmediate(owned[i]);owned.Clear();}
        Mesh Box(Vector3 center,Vector3 size)
        {
            var mesh=Own(new Mesh());var a=center-size*.5f;var b=center+size*.5f;
            mesh.vertices=new[]{new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(a.x,b.y,a.z),
                new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z)};
            mesh.uv=Enumerable.Repeat(new Vector2(.25f,.75f),8).ToArray();
            mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};mesh.RecalculateBounds();return mesh;
        }
        Mesh Join(params Mesh[] meshes)
        {var mesh=Own(new Mesh());mesh.CombineMeshes(meshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),true,true);return mesh;}
        Mesh Bake(Mesh source,float q=.25f)=>Own(VoxelWorldMeshBaker.Bake(source,q));
        static float Volume(Mesh mesh)
        {var vertices=mesh.vertices;float volume=0;var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)volume+=Vector3.Dot(vertices[indices[i]],Vector3.Cross(vertices[indices[i+1]],vertices[indices[i+2]]))/6f;return volume;}
        VoxelWorldMeshCatalog Catalog(params (Mesh source,Mesh replacement)[] pairs)
        {var catalog=Own(ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>());catalog.Bindings=pairs.Select((p,i)=>new VoxelWorldMeshCatalog.Binding{Source=p.source,Voxel=p.replacement,VoxelSize=.25f,SourceKey="adversarial-"+i,SourceBindposes=p.source.bindposes}).ToArray();return catalog;}
        [Test]public void HollowClosedShell_KeepsTheCavity_AndItsInwardFacingWalls()
        {
            var outer=Box(Vector3.zero,Vector3.one*2);var inner=Box(Vector3.zero,Vector3.one);
            var reverse=inner.triangles;for(int i=0;i<reverse.Length;i+=3){int old=reverse[i+1];reverse[i+1]=reverse[i+2];reverse[i+2]=old;}inner.triangles=reverse;
            var hollow=Bake(Join(outer,inner));Assert.That(Volume(hollow),Is.EqualTo(7).Within(.0001f));
            Assert.That(Volume(Bake(outer)),Is.EqualTo(8).Within(.0001f));
            Assert.IsTrue(hollow.vertices.Any(v=>Mathf.Abs(v.x)==.5f&&Mathf.Abs(v.y)<=.5f&&Mathf.Abs(v.z)<=.5f));
        }
        [Test]public void OverlappingClosedSolids_UseUnionRatherThanAnXorHole()
        {
            var union=Bake(Join(Box(Vector3.left*.25f,Vector3.one),Box(Vector3.right*.25f,Vector3.one)));
            Assert.That(Volume(union),Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(Volume(Bake(Box(Vector3.zero,Vector3.one))),Is.EqualTo(1).Within(.0001f));
        }
        [Test]public void BoneBoundary_ContainsBothCaps_WhileIdenticalStaticCellsHaveNoInternalFaces()
        {
            var source=Join(Box(Vector3.left*.25f,Vector3.one*.5f),Box(Vector3.right*.25f,Vector3.one*.5f));
            var flat=Bake(source);Assert.AreEqual(0,InternalFaces(flat));
            source.bindposes=new[]{Matrix4x4.identity,Matrix4x4.identity};
            source.boneWeights=Enumerable.Range(0,16).Select(i=>new BoneWeight{boneIndex0=i<8?0:1,weight0=1}).ToArray();
            var skin=Bake(source);Assert.GreaterOrEqual(InternalFaces(skin),4,"Both rigid bodies need a closed cut when the joint moves.");
            var v=skin.vertices;var weights=skin.boneWeights;var tri=skin.triangles;
            for(int i=0;i<tri.Length;i+=3)
            {Assert.AreEqual(weights[tri[i]].boneIndex0,weights[tri[i+1]].boneIndex0);Assert.AreEqual(weights[tri[i]].boneIndex0,weights[tri[i+2]].boneIndex0);}
        }
        static int InternalFaces(Mesh mesh)
        {var v=mesh.vertices;var t=mesh.triangles;int count=0;for(int i=0;i<t.Length;i+=3)if(v[t[i]].x==0&&v[t[i+1]].x==0&&v[t[i+2]].x==0)count++;return count;}
        [Test]public void NegativeAndNonuniformInstanceScale_RemainsUnchangedByApply()
        {
            var source=Box(Vector3.zero,Vector3.one);var voxel=Bake(source);var catalog=Catalog((source,voxel));
            var root=Own(new GameObject("scaled imported instance"));root.transform.SetPositionAndRotation(new Vector3(3,4,5),Quaternion.Euler(0,90,0));
            root.transform.localScale=new Vector3(-.01f,.02f,.01f);var child=new GameObject("model");child.transform.SetParent(root.transform,false);
            child.transform.localScale=Vector3.one*100;var filter=child.AddComponent<MeshFilter>();filter.sharedMesh=source;
            var oldMatrix=filter.transform.localToWorldMatrix;catalog.Apply(root);Assert.AreEqual(oldMatrix,filter.transform.localToWorldMatrix);
            Assert.AreSame(voxel,filter.sharedMesh);Assert.That(Vector3.Distance(oldMatrix.MultiplyPoint3x4(source.bounds.center),filter.transform.TransformPoint(voxel.bounds.center)),Is.LessThan(.0001f));
        }
        [Test]public void InvalidSkinOverride_IsRejectedBeforeAnEarlierStaticSwapCanPublish()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity};
            source.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},8).ToArray();
            var unskinned=Box(Vector3.zero,Vector3.one);var other=Box(Vector3.zero,Vector3.one*.5f);var replacement=Bake(other);
            var catalog=Catalog((other,replacement),(source,unskinned));
            var root=Own(new GameObject("must remain unchanged"));var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=other;
            Assert.Throws<InvalidOperationException>(()=>catalog.Apply(root));Assert.AreSame(other,filter.sharedMesh);
        }
        [Test]public void DuplicateSourceKeys_AreRejectedEvenWhenMeshReferencesDiffer()
        {
            var a=Box(Vector3.zero,Vector3.one);var b=Box(Vector3.zero,Vector3.one*.5f);var catalog=Catalog((a,Bake(a)),(b,Bake(b)));
            catalog.Bindings[1].SourceKey=catalog.Bindings[0].SourceKey;
            Assert.Throws<InvalidOperationException>(()=>catalog.Validate());
        }
        [Test]public void SameBoneCountWithChangedBindpose_IsNotACompatibleSkinOverride()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity};source.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},8).ToArray();
            var replacement=Bake(source);replacement.bindposes=new[]{Matrix4x4.Translate(Vector3.one)};
            Assert.Throws<InvalidOperationException>(()=>Catalog((source,replacement)).Validate());
        }
        [Test]public void NullMeshAndNullInstance_FailExplicitly()
        {Assert.Throws<ArgumentNullException>(()=>VoxelWorldMeshBaker.Bake(null,.25f));var source=Box(Vector3.zero,Vector3.one);Assert.Throws<ArgumentNullException>(()=>Catalog((source,Bake(source))).Apply(null));}
        [Test]public void UnreadableSourceSkin_UsesStoredContractWithoutReadingItsCpuArrays()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity};
            source.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=0,weight0=1},8).ToArray();
            var replacement=Bake(source);var catalog=Catalog((source,replacement));source.UploadMeshData(true);Assert.IsFalse(source.isReadable);
            Assert.DoesNotThrow(()=>catalog.Validate());
            var root=Own(new GameObject("unreadable source skin"));var bone=new GameObject("root bone");bone.transform.SetParent(root.transform,false);
            var renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.bones=new[]{bone.transform};renderer.rootBone=bone.transform;renderer.sharedMesh=source;
            Assert.AreEqual(1,catalog.Apply(root));Assert.AreSame(replacement,renderer.sharedMesh);
        }
        [Test]public void FractionalSkinWeights_DoNotLeakOntoVoxelFaces()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity,Matrix4x4.identity};
            source.boneWeights=Enumerable.Range(0,8).Select(i=>new BoneWeight{boneIndex0=0,weight0=i%2==0?.8f:.2f,boneIndex1=1,weight1=i%2==0?.2f:.8f}).ToArray();
            var voxel=Bake(source);Assert.IsTrue(voxel.boneWeights.All(w=>w.weight0==1&&w.weight1==0&&w.weight2==0&&w.weight3==0));
            Assert.IsTrue(voxel.boneWeights.Any(w=>w.boneIndex0==0));Assert.IsTrue(voxel.boneWeights.Any(w=>w.boneIndex0==1));
        }
        [Test]public void DegenerateTriangle_IsIgnoredAlongsideValidGeometry()
        {
            var source=Box(Vector3.zero,Vector3.one);var valid=Bake(source);source.triangles=source.triangles.Concat(new[]{0,0,0}).ToArray();
            var actual=Bake(source);CollectionAssert.AreEqual(valid.vertices,actual.vertices);CollectionAssert.AreEqual(valid.triangles,actual.triangles);
        }
        [Test]public void ReversedWindingStillProducesOutwardSolidVoxels()
        {
            var source=Box(Vector3.zero,Vector3.one);var indices=source.triangles;
            for(int i=0;i<indices.Length;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}source.triangles=indices;
            Assert.That(Volume(Bake(source)),Is.EqualTo(1).Within(.0001f));
        }
    }
}
