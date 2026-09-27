#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityGroveMothRenderingTests
    {
        const string Source="Assets/Art3D/SpawnRing/Models/ring-grove-lantern-moth.fbx";
        static VoxelWorldMeshCatalog.Binding Moth()
        {
            var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);Assert.NotNull(catalog);catalog.Validate();
            return catalog.Bindings.Single(b=>AssetDatabase.GetAssetPath(b.Source)==Source);
        }
        static float Select(string path,Vector3 size,float scale=1,bool skin=true,bool equipment=false)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldDensity")).First(t=>t!=null);
            var method=type.GetMethod("SelectNativeWorldPitch",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method,"One exact native source needs an explicit art-density policy; global thin creatures retain their established pitch.");
            try{return(float)method.Invoke(null,new object[]{path,size,scale,skin,equipment});}
            catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        [Test]public void ActualAdoptedMothKeepsAtLeastElevenVoxelsAcrossItsWings()
        {
            var row=Moth();Assert.AreEqual(.0625f,row.WorldVoxelSize);
            Assert.GreaterOrEqual(row.Source.bounds.size.x/row.VoxelSize,11);
            var fresh=VoxelWorldMeshBaker.Bake(row.Source,row.VoxelSize);
            try{CollectionAssert.AreEqual(fresh.vertices,row.Voxel.vertices);CollectionAssert.AreEqual(fresh.triangles,row.Voxel.triangles);CollectionAssert.AreEqual(fresh.boneWeights,row.Voxel.boneWeights);}
            finally{UnityEngine.Object.DestroyImmediate(fresh);}
        }
        [Test]public void ExactSourcePolicyDoesNotChangeOrdinaryThinCreaturesOrEquipment()
        {
            var size=new Vector3(.715f,.45f,.105f);Assert.AreEqual(.0625f,Select(Source,size));
            foreach(string path in new[]{null,"",Source.ToUpperInvariant(),Source+".other","Assets/Art3D/SpawnRing/Models/ring-other-moth.fbx"})
                Assert.AreEqual(.125f,Select(path,size),path);
            Assert.AreEqual(.25f,Select(Source,size,skin:false));Assert.AreEqual(.125f,Select(Source,size,equipment:true));
            Assert.AreEqual(.1875f,Select("ordinary",Vector3.one));Assert.AreEqual(.25f,Select("ordinary",Vector3.one,skin:false));
        }
        [TestCase(0f)][TestCase(float.NaN)]public void NamedSourceStillRejectsInvalidScaleBeforeChoosingPitch(float scale)
        {Assert.Throws<ArgumentException>(()=>Select(Source,new Vector3(.7f,.45f,.1f),scale));}
        [TestCase(-1)][TestCase(1)]public void ActualBakedWingLobesHaveAnEmptyNotchBetweenSolidFrontAndRear(int side)
        {
            var row=Moth();var mesh=row.Voxel;var v=mesh.vertices;var tri=mesh.triangles;
            // Source FBX geometry stores east/north/up as X/Y/Z. Sample a
            // column off the central body, then demand occupied-empty-occupied
            // along the fore/hind axis. A flat rectangular wafer cannot pass.
            float x=side*.21875f;bool front=false,gap=false,rear=false;
            for(float y=mesh.bounds.min.y+.03125f;y<mesh.bounds.max.y;y+=.0625f)
            {
                bool covered=false;var p=new Vector2(x,y);
                for(int i=0;i<tri.Length&&!covered;i+=3)covered=Inside(p,v[tri[i]],v[tri[i+1]],v[tri[i+2]]);
                if(covered){if(gap)rear=true;front=true;}else if(front)gap=true;
            }
            Assert.True(front&&gap&&rear,"Actual adopted "+side+" wing has no readable fore/hind notch.");
        }
        static bool Inside(Vector2 p,Vector3 va,Vector3 vb,Vector3 vc)
        {
            Vector2 a=va,b=vb,c=vc;float Cross(Vector2 u,Vector2 w)=>u.x*w.y-u.y*w.x;
            if(Mathf.Abs(Cross(b-a,c-a))<.000001f)return false;
            float ab=Cross(b-a,p-a),bc=Cross(c-b,p-b),ca=Cross(a-c,p-c);
            return (ab>=-.000001f&&bc>=-.000001f&&ca>=-.000001f)||(ab<=.000001f&&bc<=.000001f&&ca<=.000001f);
        }
        [Test]public void FinerBodyKeepsTheFiveActualBonesAndTwoColorBudget()
        {
            var row=Moth();Assert.AreEqual(5,row.Source.bindposeCount);CollectionAssert.AreEqual(row.Source.bindposes,row.Voxel.bindposes);
            var before=row.Source.boneWeights.Where(w=>w.weight0>0).Select(w=>w.boneIndex0).Distinct().ToArray();
            var after=row.Voxel.boneWeights.Where(w=>w.weight0>0).Select(w=>w.boneIndex0).Distinct().ToArray();
            CollectionAssert.AreEquivalent(before,after);Assert.That(row.Voxel.uv.Distinct().Count(),Is.InRange(1,2));
        }
    }
}
#endif
