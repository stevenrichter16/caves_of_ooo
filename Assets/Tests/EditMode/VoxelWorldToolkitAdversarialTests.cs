using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Pure importer boundary tests; never publishes AssetDatabase files.
    /// Each rejection keeps an identical valid-quad positive control.</summary>
    public sealed class VoxelWorldToolkitAdversarialTests
    {
        const string Fixture="{\"schemaVersion\":1,\"assets\":[{\"id\":\"test\",\"vertices\":[[0,0,0],[1,0,0],[1,1,0],[0,1,0]],\"quads\":[[0,1,2,3]],\"quadMaterials\":[0]}]}";
        static Mesh Build(string json,Matrix4x4 transform,Vector2[] palette=null)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldToolkitImporter")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type);var method=type.GetMethod("CreateMesh",BindingFlags.Static|BindingFlags.Public);Assert.NotNull(method);
            try {return (Mesh)method.Invoke(null,new object[]{json,"test",transform,palette??new[]{new Vector2(.3f,.7f)}});}
            catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        static void ValidControl()
        {var mesh=Build(Fixture,Matrix4x4.identity);try{Assert.AreEqual(6,mesh.triangles.Length);}finally{Object.DestroyImmediate(mesh);}}
        static void Reject(string json,Matrix4x4 transform)
        {
            ValidControl();Mesh unexpected=null;
            try {Assert.Throws<ArgumentException>(()=>unexpected=Build(json,transform));}
            finally {if(unexpected!=null)Object.DestroyImmediate(unexpected);}
        }

        [TestCase("secondTriangle")][TestCase("firstTriangle")][TestCase("bowTie")]
        public void Adversarial_DegenerateOrFoldedQuadCannotPublishHalfValidGeometry(string mutation)
        {
            string quad=mutation=="secondTriangle"?"[0,1,2,0]":mutation=="firstTriangle"?"[0,0,2,3]":"[0,2,1,3]";
            Reject(Fixture.Replace("[0,1,2,3]",quad),Matrix4x4.identity);
        }
        [TestCase("faceFraction")][TestCase("materialFraction")][TestCase("faceString")][TestCase("materialString")]
        public void Adversarial_IndicesRequireIntegerTokensRatherThanSilentNumericCoercion(string mutation)
        {
            string json=mutation=="faceFraction"?Fixture.Replace("[0,1,2,3]","[0,1.1,2,3]"):
                mutation=="materialFraction"?Fixture.Replace("quadMaterials\":[0]","quadMaterials\":[0.1]"):
                mutation=="faceString"?Fixture.Replace("[0,1,2,3]","[0,\"1\",2,3]"):
                Fixture.Replace("quadMaterials\":[0]","quadMaterials\":[\"0\"]");
            Reject(json,Matrix4x4.identity);
        }
        [TestCase("negativeW")][TestCase("doubleW")][TestCase("projectiveX")][TestCase("projectiveY")][TestCase("projectiveZ")]
        public void Adversarial_MultiplyPoint3x4RequiresAnAffineMatrix(string mutation)
        {
            var matrix=Matrix4x4.identity;
            if(mutation=="negativeW")matrix.m33=-1;
            else if(mutation=="doubleW")matrix.m33=2;
            else if(mutation=="projectiveX")matrix.m30=.5f;
            else if(mutation=="projectiveY")matrix.m31=.5f;
            else matrix.m32=.5f;
            Reject(Fixture,matrix);
        }
        [TestCase(-1f)][TestCase(1f)]
        public void Adversarial_AffineReflectionAndTranslationKeepCorrectNormalOrientation(float sign)
        {
            var matrix=Matrix4x4.TRS(new Vector3(2,3,4),Quaternion.Euler(0,30,0),new Vector3(sign*2,2,2));
            var mesh=Build(Fixture,matrix);
            try
            {
                var expected=matrix.inverse.transpose.MultiplyVector(Vector3.forward).normalized;
                foreach(var normal in mesh.normals)Assert.Greater(Vector3.Dot(normal,expected),.999f);
                Assert.That(mesh.bounds.min.y,Is.EqualTo(3).Within(.0001f));
                Assert.That(Vector3.Distance(mesh.vertices[0],mesh.vertices[1]),Is.EqualTo(2).Within(.0001f));
            }
            finally {Object.DestroyImmediate(mesh);}
        }
        [TestCase("nan")][TestCase("infinity")][TestCase("singular")]
        public void Adversarial_NonfiniteOrSingularTransformRejectsBeforeMeshAllocation(string mutation)
        {
            var matrix=Matrix4x4.identity;matrix.m11=mutation=="nan"?float.NaN:mutation=="infinity"?float.PositiveInfinity:0;
            Reject(Fixture,matrix);
        }
        [Test] public void Adversarial_FaceMaterialsRetainDistinctUvOwnershipUnderReflection()
        {
            var json=Fixture.Replace("\"quads\":[[0,1,2,3]],\"quadMaterials\":[0]","\"quads\":[[0,1,2,3],[3,2,1,0]],\"quadMaterials\":[0,1]");
            var first=new Vector2(.1f,.2f);var second=new Vector2(.8f,.9f);
            var mesh=Build(json,Matrix4x4.Scale(new Vector3(-1,1,1)),new[]{first,second});
            try {CollectionAssert.AreEqual(new[]{first,first,first,first,second,second,second,second},mesh.uv);}
            finally {Object.DestroyImmediate(mesh);}
        }
        [Test] public void Adversarial_NonfiniteMaterialUvCannotContaminateVertexChannels()
        {
            ValidControl();Mesh unexpected=null;
            try {Assert.Throws<ArgumentException>(()=>unexpected=Build(Fixture,Matrix4x4.identity,new[]{new Vector2(float.NaN,0)}));}
            finally {if(unexpected!=null)Object.DestroyImmediate(unexpected);}
        }
        [Test] public void Adversarial_FailedDecodeDoesNotChangeASeparateSuccessfulMesh()
        {
            var good=Build(Fixture,Matrix4x4.identity);var vertices=good.vertices;var triangles=good.triangles;
            try {Reject(Fixture.Replace("[0,1,2,3]","[0,1,2,99]"),Matrix4x4.identity);CollectionAssert.AreEqual(vertices,good.vertices);CollectionAssert.AreEqual(triangles,good.triangles);}
            finally {Object.DestroyImmediate(good);}
        }
    }
}
