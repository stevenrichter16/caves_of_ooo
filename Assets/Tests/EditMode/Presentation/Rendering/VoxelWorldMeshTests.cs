using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Reflection keeps the specification executable before the baker/catalog exist.</summary>
    public sealed class VoxelWorldMeshTests
    {
        readonly List<Object> owned = new List<Object>();
        T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        [TearDown] public void Cleanup()
        { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }
        static Type Production(string name)
        {
            var type = typeof(Village3DLibrary).Assembly.GetType("CavesOfOoo.Rendering." + name);
            Assert.NotNull(type, "Implement " + name + " after recording this RED."); return type;
        }
        Mesh Bake(Mesh mesh, float size = .25f)
        {
            var method = Production("VoxelWorldMeshBaker").GetMethod("Bake", new[] { typeof(Mesh), typeof(float) });
            Assert.NotNull(method);
            try { return Own((Mesh)method.Invoke(null, new object[] { mesh, size })); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        Mesh Box(Vector3 center, Vector3 size, int material = 0)
        {
            var mesh = Own(new Mesh { name = "voxel test box" });
            var min = center - size * .5f; var max = center + size * .5f;
            mesh.vertices = new[] { new Vector3(min.x,min.y,min.z),new Vector3(max.x,min.y,min.z),
                new Vector3(max.x,max.y,min.z),new Vector3(min.x,max.y,min.z),new Vector3(min.x,min.y,max.z),
                new Vector3(max.x,min.y,max.z),new Vector3(max.x,max.y,max.z),new Vector3(min.x,max.y,max.z) };
            mesh.uv = Enumerable.Repeat(new Vector2(.3125f,.6875f),8).ToArray();
            mesh.subMeshCount = material + 1;
            mesh.SetTriangles(new[] {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5},material);
            mesh.RecalculateBounds(); return mesh;
        }
        Mesh Join(params Mesh[] meshes)
        {
            var mesh=Own(new Mesh {name="joined voxel fixture"});
            mesh.CombineMeshes(meshes.Select(m=>new CombineInstance {mesh=m,transform=Matrix4x4.identity}).ToArray(),true,true);
            return mesh;
        }
        static void FlatAndGrid(Mesh mesh,float q)
        {
            foreach(var p in mesh.vertices)
                for(int axis=0;axis<3;axis++) Assert.That(Mathf.Abs(p[axis]/q-Mathf.Round(p[axis]/q)),Is.LessThan(.0001f));
            foreach(var n in mesh.normals)
            { Assert.That(n.magnitude,Is.EqualTo(1).Within(.0001f)); Assert.That(Mathf.Abs(n.x)+Mathf.Abs(n.y)+Mathf.Abs(n.z),Is.EqualTo(1).Within(.0001f)); }
            var v=mesh.vertices;var normals=mesh.normals;
            foreach(int sub in Enumerable.Range(0,mesh.subMeshCount))
            {
                var indices=mesh.GetTriangles(sub);
                for(int i=0;i<indices.Length;i+=3)
                {
                    var normal=Vector3.Cross(v[indices[i+1]]-v[indices[i]],v[indices[i+2]]-v[indices[i]]).normalized;
                    Assert.That(Vector3.Dot(normal,normals[indices[i]]),Is.GreaterThan(.999f),"Outward winding and explicit flat normal agree.");
                }
            }
        }
        [Test] public void ClosedBox_RetainsBoundsPaletteAndOutwardFlatVoxelFaces()
        {
            var source=Box(Vector3.zero,Vector3.one);var before=source.vertices;
            var result=Bake(source);FlatAndGrid(result,.25f);
            Assert.AreEqual(source.bounds,result.bounds);CollectionAssert.AreEqual(before,source.vertices);
            Assert.IsTrue(result.uv.All(uv=>uv==new Vector2(.3125f,.6875f)));
            Assert.That(result.vertexCount,Is.GreaterThan(0).And.LessThanOrEqualTo(96*4));
        }
        [Test] public void TwoSeparatedSolids_DoNotBridgeEmptySpace()
        {
            var source=Join(Box(new Vector3(-1,0,0),Vector3.one*.5f),Box(new Vector3(1,0,0),Vector3.one*.5f));
            var baked=Bake(source);Assert.IsTrue(baked.vertices.All(v=>Mathf.Abs(v.x)>=.75f));
            var adjacent=Bake(Join(Box(new Vector3(-.25f,0,0),Vector3.one*.5f),Box(new Vector3(.25f,0,0),Vector3.one*.5f)));
            Assert.IsTrue(adjacent.vertices.Any(v=>Mathf.Abs(v.x)<.75f));
        }
        [Test] public void OpenThinPlane_HasVoxelThicknessWithoutFillingItsBoundingVolume()
        {
            var source=Own(new Mesh());source.vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,0,1)};
            source.uv=new[]{Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f};source.triangles=new[]{0,2,1};
            var baked=Bake(source);FlatAndGrid(baked,.25f);Assert.That(baked.bounds.size.y,Is.EqualTo(.25f).Within(.0001f));
            Assert.IsFalse(baked.vertices.Any(v=>v.x>.75f&&v.z>.75f),"Triangle fallback must not become a filled rectangular slab.");
        }
        [Test] public void SubmeshIndicesAndPaletteCoordinates_RemainWithOriginalMaterialFamilies()
        {
            var source=Box(Vector3.zero,Vector3.one,1);var result=Bake(source);
            Assert.AreEqual(2,result.subMeshCount);Assert.AreEqual(0,result.GetIndexCount(0));Assert.Greater(result.GetIndexCount(1),0);
            Assert.IsTrue(result.uv.All(uv=>uv==source.uv[0]));
        }
        [Test] public void Bake_IsDeterministicAndDoesNotConsumeUnityRandom()
        {
            var source=Box(new Vector3(.12f,-.08f,.2f),new Vector3(.8f,.65f,.7f));
            var state=UnityEngine.Random.state;var a=Bake(source);Assert.AreEqual(state,UnityEngine.Random.state);var b=Bake(source);
            CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.triangles,b.triangles);CollectionAssert.AreEqual(a.uv,b.uv);
        }
        [TestCase(0f)][TestCase(-1f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void BadVoxelSize_IsRejectedWithoutEditingSource(float q)
        { var source=Box(Vector3.zero,Vector3.one);var old=source.vertices;Assert.Throws<ArgumentException>(()=>Bake(source,q));CollectionAssert.AreEqual(old,source.vertices); }
        [Test] public void EmptyMesh_IsRejected()
        { Assert.Throws<ArgumentException>(()=>Bake(Own(new Mesh()))); }
        [Test] public void UnboundedWork_IsRejectedBeforeAllocatingHugeGrid()
        { Assert.Throws<ArgumentException>(()=>Bake(Box(Vector3.zero,Vector3.one*10000),.125f)); }
        [Test] public void SkinnedVoxel_UsesOneDominantBoneAndCopiesBindposes()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity,Matrix4x4.Translate(Vector3.up)};
            source.boneWeights=Enumerable.Repeat(new BoneWeight {boneIndex0=0,weight0=.2f,boneIndex1=1,weight1=.8f},8).ToArray();
            var result=Bake(source);CollectionAssert.AreEqual(source.bindposes,result.bindposes);
            Assert.AreEqual(result.vertexCount,result.boneWeights.Length);
            Assert.IsTrue(result.boneWeights.All(w=>w.boneIndex0==1&&w.weight0==1&&w.weight1==0&&w.weight2==0&&w.weight3==0));
            Assert.AreEqual(0,Bake(Box(Vector3.zero,Vector3.one)).boneWeights.Length);
        }
        ScriptableObject Catalog(params (Mesh source,Mesh replacement)[] mappings)
        {
            var type=Production("VoxelWorldMeshCatalog");var catalog=Own(ScriptableObject.CreateInstance(type));
            var field=type.GetField("Bindings");Assert.NotNull(field);var rowType=field.FieldType.GetElementType();var rows=Array.CreateInstance(rowType,mappings.Length);
            for(int i=0;i<mappings.Length;i++)
            { var row=Activator.CreateInstance(rowType);rowType.GetField("Source").SetValue(row,mappings[i].source);rowType.GetField("Voxel").SetValue(row,mappings[i].replacement);
              rowType.GetField("VoxelSize").SetValue(row,.25f);rowType.GetField("SourceKey").SetValue(row,"test-"+i);
              rowType.GetField("SourceBindposes")?.SetValue(row,mappings[i].source.bindposes);rows.SetValue(row,i); }
            field.SetValue(catalog,rows);return catalog;
        }
        static object Call(Object value,string method,params object[] args)
        { try{return value.GetType().GetMethod(method).Invoke(value,args);}catch(TargetInvocationException e){throw e.InnerException??e;} }
        [Test] public void CatalogApply_OnlySwapsOwnedInstanceMeshes_AndIsIdempotent()
        {
            var source=Box(Vector3.zero,Vector3.one);var replacement=Box(Vector3.zero,Vector3.one*.75f);var unknown=Box(Vector3.zero,Vector3.one*.5f);
            var root=Own(new GameObject("owned instance"));var child=new GameObject("socket");child.transform.SetParent(root.transform,false);
            var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=source;var renderer=root.AddComponent<MeshRenderer>();
            var material=Own(new Material(Shader.Find("Hidden/InternalErrorShader")));renderer.sharedMaterial=material;
            var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=source;
            var other=child.AddComponent<MeshFilter>();other.sharedMesh=unknown;child.AddComponent<MeshRenderer>();
            var catalog=Catalog((source,replacement));Assert.AreEqual(1,Call(catalog,"Apply",root));Assert.AreSame(replacement,filter.sharedMesh);
            Assert.AreSame(source,collider.sharedMesh);Assert.AreSame(material,renderer.sharedMaterial);Assert.AreSame(unknown,other.sharedMesh);
            Assert.AreEqual(0,Call(catalog,"Apply",root));Assert.AreEqual(1,root.transform.childCount);Assert.AreEqual("socket",child.name);
        }
        [Test] public void CatalogApply_PreservesSkinnedBonesRootBoundsAndAnimator()
        {
            var source=Box(Vector3.zero,Vector3.one);source.bindposes=new[]{Matrix4x4.identity};
            source.boneWeights=Enumerable.Repeat(new BoneWeight {boneIndex0=0,weight0=1},8).ToArray();var replacement=Bake(source);
            var root=Own(new GameObject("rig"));var bone=new GameObject("hand socket");bone.transform.SetParent(root.transform,false);
            var renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=source;renderer.bones=new[]{bone.transform};renderer.rootBone=bone.transform;
            var bounds=new Bounds(Vector3.zero,Vector3.one*4);renderer.localBounds=bounds;var animator=root.AddComponent<Animator>();
            Assert.AreEqual(1,Call(Catalog((source,replacement)),"Apply",root));Assert.AreSame(replacement,renderer.sharedMesh);
            Assert.AreSame(bone.transform,renderer.rootBone);Assert.AreSame(bone.transform,renderer.bones[0]);Assert.AreSame(animator,root.GetComponent<Animator>());Assert.AreEqual(bounds,renderer.localBounds);
        }
        [Test] public void DuplicateOrChainedCatalogMapping_IsRejectedBeforeAnyInstanceMutation()
        {
            var a=Box(Vector3.zero,Vector3.one);var b=Box(Vector3.zero,Vector3.one*.75f);var c=Box(Vector3.zero,Vector3.one*.5f);
            Assert.Throws<InvalidOperationException>(()=>Call(Catalog((a,b),(a,c)),"Validate"));
            Assert.Throws<InvalidOperationException>(()=>Call(Catalog((a,b),(b,c)),"Validate"));
        }
    }
}
