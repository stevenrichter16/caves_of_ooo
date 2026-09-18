#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastCoarseVoxelAdversarialTests
    {
        static object Call(string name,params object[] args)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.MorrowfastCoarseVoxelBuilder")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type);
            try{return type.GetMethod(name,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);}
            catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static MorrowfastSceneDefinition Native()=>MorrowfastSceneDefinition.Parse(Resources.Load<TextAsset>("SceneArt/Morrowfast/definition").text);
        static Mesh Make(string id,MorrowfastSceneDefinition d,bool water=false)=>(Mesh)Call("CreateModelMesh",id,d,water);

        [TestCase("detail-patch-0-00")][TestCase("detail-patch-00-0")]
        [TestCase("detail-patch-08-00")][TestCase("detail-patch-00-05")]
        [TestCase("detail-patch-00-00 ")][TestCase("Detail-patch-00-00")]
        [TestCase("detail-patch-000-00")][TestCase("detail-patch-00-00/water")]
        [TestCase("fence-4")][TestCase("fence-00")]
        public void AlmostValidSourceIdsNeverAliasAnOwnedModel(string id)
        {
            Assert.IsFalse((bool)Call("SupportsModel",id));
            Assert.Throws<ArgumentException>(()=>Make(id,Native()));
            Assert.IsTrue((bool)Call("SupportsModel","detail-patch-07-04"));
            Assert.IsTrue((bool)Call("SupportsModel","fence-3"));
        }
        [TestCase("keeper-gatehouse-shell")][TestCase("keeper-gatehouse-roof")]
        [TestCase("ground-patch-0")][TestCase("footbridge")][TestCase("barrel-0")]
        [TestCase("tree-0")][TestCase("oak-door")]
        public void SceneryWithoutNativeWaterCannotAcquireAWaterSurface(string id)
        {
            var native=Native();Assert.Throws<ArgumentException>(()=>Make(id,native,true));
            var normal=Make(id,native);try{Assert.Greater(normal.vertexCount,0);}finally{Object.DestroyImmediate(normal);}
        }
        [Test] public void MissingNativeDefinitionIsNotSubstitutedByCachedWorldData()
        {
            Assert.Throws<ArgumentNullException>(()=>Make("central-well",null));
            var mesh=Make("central-well",Native());try{Assert.Greater(mesh.vertexCount,0);}finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void ArtGenerationDoesNotChangeNativeOwnerAndCellPayloads()
        {
            var native=Native();string before=JsonUtility.ToJson(native);
            foreach(string id in new[]{"keeper-gatehouse-shell","keeper-gatehouse-roof","detail-patch-03-02","central-well"})
            {var mesh=Make(id,native);Object.DestroyImmediate(mesh);}
            Assert.AreEqual(before,JsonUtility.ToJson(native));
            Assert.Greater(native.owners.Length,0);Assert.Greater(native.cells.Count(c=>c.water),0);
        }
        [Test] public void WaterPatchGenerationIsIndependentOfNeighborPatchWaterPayload()
        {
            var native=Native();var neighbor=native.cells.First(c=>c.water);
            int nx=neighbor.x/10,ny=(24-neighbor.y)/5;
            int tx=(nx+1)%8;string target="detail-patch-"+tx.ToString("00")+"-"+ny.ToString("00");
            string changed="detail-patch-"+nx.ToString("00")+"-"+ny.ToString("00");
            var original=Make(target,native,true);var wet=Make(changed,native,true);
            neighbor.water=false;
            Mesh after=null,dry=null;
            try
            {
                after=Make(target,native,true);dry=Make(changed,native,true);
                CollectionAssert.AreEqual(original.vertices,after.vertices);
                CollectionAssert.AreEqual(original.triangles,after.triangles);
                Assert.IsFalse(wet.vertices.SequenceEqual(dry.vertices),"The changed patch must actually respond to its native water cell.");
            }
            finally{Object.DestroyImmediate(original);Object.DestroyImmediate(wet);if(after!=null)Object.DestroyImmediate(after);if(dry!=null)Object.DestroyImmediate(dry);}
        }
    }
}
#endif
