#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class VoxelSelectedSaveTests
    {
        static void Save(bool selected,Mesh[] meshes,VoxelWorldMeshCatalog catalog,Action<Object> one,Action all)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldMeshBuilder")).FirstOrDefault(t=>t!=null); Assert.NotNull(type);
            var method=type.GetMethod("SaveImportOutputs",BindingFlags.Public|BindingFlags.Static); Assert.NotNull(method,"A selected bake must never save unrelated dirty assets.");
            try{method.Invoke(null,new object[]{selected,meshes,catalog,one,all});}
            catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        [TestCase(false)][TestCase(true)]
        public void SelectedSavesOnlyProducedMeshesAndCatalogWhileFullRetainsGlobalCallback(bool selected)
        {
            var a=new Mesh();var b=new Mesh();var library=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();var calls=new List<Object>();int global=0;
            try
            {
                Save(selected,new[]{a,b},library,x=>calls.Add(x),()=>global++);
                Assert.AreEqual(selected?0:1,global); CollectionAssert.AreEqual(selected?new Object[]{a,b,library}:Array.Empty<Object>(),calls);
            }
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);Object.DestroyImmediate(library);}
        }
        [TestCase("null-array")][TestCase("empty")][TestCase("null-mesh")][TestCase("duplicate")][TestCase("null-catalog")]
        public void InvalidSelectedOutputListRefusesBeforeTheFirstCallback(string fault)
        {
            var mesh=new Mesh();var library=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();int saves=0;
            try
            {
                var values=fault=="null-array"?null:fault=="empty"?Array.Empty<Mesh>():fault=="null-mesh"?new[]{mesh,null}:fault=="duplicate"?new[]{mesh,mesh}:new[]{mesh};
                Assert.Throws<ArgumentException>(()=>Save(true,values,fault=="null-catalog"?null:library,_=>saves++,()=>saves++)); Assert.Zero(saves);
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(library);}
        }
        [Test] public void ForeignDirtyOwnedTestSentinelIsNeitherSelectedNorCleared()
        {
            var output=new Mesh();var foreign=new Mesh{name="Owned test sentinel, never an actual project asset"};var library=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();
            try
            {
                foreign.vertices=new[]{new Vector3(2,3,4)};EditorUtility.SetDirty(foreign);bool dirty=EditorUtility.IsDirty(foreign);var vertices=foreign.vertices;
                Assert.True(dirty);var calls=new List<Object>();Save(true,new[]{output},library,x=>calls.Add(x),()=>Assert.Fail("Selected import cannot globally save."));
                CollectionAssert.DoesNotContain(calls,foreign);Assert.AreEqual(dirty,EditorUtility.IsDirty(foreign));CollectionAssert.AreEqual(vertices,foreign.vertices);
            }
            finally{Object.DestroyImmediate(output);Object.DestroyImmediate(foreign);Object.DestroyImmediate(library);}
        }
    }
}
#endif
