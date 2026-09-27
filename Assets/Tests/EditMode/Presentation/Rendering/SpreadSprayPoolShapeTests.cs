#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadSprayPoolShapeTests
    {
        static string Id(int variant) => "ring-spray-pool-" + variant;
        static string PathFor(int variant) => "Assets/Art3D/SpawnRing/Models/" + Id(variant) + ".fbx";
        const float BasinPitch = .0625f;
        static float NativePitch(string path,Vector3 size,float scale,bool skin,bool equipment)
            => Pitch("SelectNativeWorldPitch",new object[]{path,size,scale,skin,equipment});
        static float GenericPitch(Vector3 size,float scale,bool skin,bool equipment)
            => Pitch("SelectWorldPitch",new object[]{size,scale,skin,equipment});
        static float Pitch(string name,object[] args)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldDensity")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Actual editor density policy must be loaded.");
            var method=type.GetMethod(name,BindingFlags.Public|BindingFlags.Static); Assert.NotNull(method);
            try{return (float)method.Invoke(null,args);}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }


        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ExactRigidSpraySourcesRetainBasinAndRimResolution(int variant)
        {
            Assert.AreEqual(BasinPitch, NativePitch(PathFor(variant), new Vector3(.97f,.02f,.97f), 1, false, false));
            Assert.AreEqual(BasinPitch, NativePitch(PathFor(variant), new Vector3(.99f,.16f,.94f), 1, false, false));
        }

        [TestCase("Assets/Art3D/SpawnRing/Models/ring-water-puddle-0.fbx", false, false)]
        [TestCase("Assets/Art3D/SpawnRing/Models/ring-spray-pool-4.fbx", false, false)]
        [TestCase("Assets/Art3D/SpawnRing/Models/ring-spray-pool-0-copy.fbx", false, false)]
        [TestCase("Assets/Art3D/SpawnRing/Models/RING-SPRAY-POOL-0.fbx", false, false)]
        [TestCase("Assets/Art3D/SpawnRing/Models/ring-spray-pool-0.fbx", true, false)]
        [TestCase("Assets/Art3D/SpawnRing/Models/ring-spray-pool-0.fbx", false, true)]
        public void OtherSourceOrCapabilityKeepsGenericDensity(string path, bool skin, bool equipment)
        {
            var size = new Vector3(.97f,.02f,.97f);
            Assert.AreEqual(GenericPitch(size,1,skin,equipment), NativePitch(path,size,1,skin,equipment));
        }

        [Test] public void ExistingMothOverrideAndMalformedBoundsContractRemainIntact()
        {
            Assert.AreEqual(.0625f, NativePitch("Assets/Art3D/SpawnRing/Models/ring-grove-lantern-moth.fbx", new Vector3(.7f,.12f,.4f), 1, true, false));
            Assert.Throws<ArgumentException>(() => NativePitch(PathFor(0),new Vector3(float.NaN,.02f,.97f),1,false,false));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void OriginalRawSourceHasRoundThinWaterAndHigherSeparateRocks(int variant)
        {
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath); Assert.NotNull(ring);
            var original = ring.FindModel(Id(variant)); Assert.NotNull(original);
            var renderers = original.GetComponentsInChildren<MeshRenderer>(true); Assert.AreEqual(2, renderers.Length);
            var water = Points(original, renderers.Single(r=>r.sharedMaterial==ring.WaterMaterial));
            var rocks = Points(original, renderers.Single(r=>r.sharedMaterial==ring.WorldMaterial));
            Assert.Less(BoundsOf(water).size.y,.021f); Assert.Greater(BoundsOf(rocks).max.y,.15f);
            Assert.False(PositionSet(water).SetEquals(PositionSet(rocks)));
            Assert.True(RoundOutline(water),"Raw basin must leave each bounding-square corner outside its round footprint.");
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void AdoptedPoolKeepsRoundLowWaterAndDistinctRaisedRim(int variant)
        {
            var library=SpreadNativeStyle3DLibrary.Load(); Assert.NotNull(library); library.Validate();
            var entry=library.Find(Id(variant)); Assert.NotNull(entry); var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            CollectionAssert.AreEqual(new[]{ring.WorldMaterial,ring.WaterMaterial},entry.Materials); Assert.AreEqual(2,entry.Mesh.subMeshCount);
            var vertices=entry.Mesh.vertices;
            var rocks=entry.Mesh.GetIndices(0).Select(i=>vertices[i]).ToArray();
            var water=entry.Mesh.GetIndices(1).Select(i=>vertices[i]).ToArray();
            Assert.False(PositionSet(water).SetEquals(PositionSet(rocks)),"Separate semantic slots cannot approve duplicate solid boxes.");
            Assert.LessOrEqual(BoundsOf(water).size.y,BasinPitch+.0001f,"A thin water basin must not become a quarter-cell column.");
            Assert.Greater(BoundsOf(rocks).max.y,BoundsOf(water).max.y+.06f,"Rim rocks must rise above the actual water surface.");
            Assert.True(RoundOutline(water),"The basin must retain a stepped round footprint instead of all four square corners.");
            Assert.LessOrEqual(entry.Mesh.vertexCount,12000,"This scoped silhouette repair remains a small static model.");
        }

        static Vector3[] Points(GameObject root,MeshRenderer renderer)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh; Assert.NotNull(mesh);
            var matrix=root.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
            return mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
        }
        static Bounds BoundsOf(Vector3[] points)
        {
            Assert.IsNotEmpty(points); var bounds=new Bounds(points[0],Vector3.zero);
            foreach(var p in points)bounds.Encapsulate(p); return bounds;
        }
        static HashSet<Vector3Int> PositionSet(Vector3[] points) => new HashSet<Vector3Int>(points.Select(p=>new Vector3Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000))));
        static bool RoundOutline(Vector3[] points)
        {
            var b=BoundsOf(points); float cornerX=b.extents.x*.9f,cornerZ=b.extents.z*.9f;
            return !points.Any(p=>Mathf.Abs(p.x-b.center.x)>cornerX&&Mathf.Abs(p.z-b.center.z)>cornerZ);
        }
    }
}
#endif
