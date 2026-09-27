using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadEquipmentLibraryTests
    {
        [Test] public void ActualTwelvePersistentMeshesUseOneBoneAndApprovedPalette()
        {
            var library=SpreadEquipment3DLibrary.Load();Assert.NotNull(library);Assert.DoesNotThrow(library.Validate);Assert.AreEqual(12,library.Entries.Length);
            Assert.AreSame(SpreadPortable3DLibrary.Load().Material,library.Material);
            foreach(var entry in library.Entries)
            {
                Assert.AreSame(entry,library.Find(entry.Id));Assert.True(library.ContainsMesh(entry.Mesh));
                Assert.AreEqual(Matrix4x4.identity,entry.Mesh.bindposes.Single());Assert.True(entry.Mesh.boneWeights.All(w=>w.weight0==1&&w.boneIndex0==0));
            }
        }
        [Test] public void ExactSourceBoxBuffersAndPaintMatchEveryAdoptedForm()
        {
            var source=JsonUtility.FromJson<SpreadEquipmentSource>(File.ReadAllText(Path.Combine(Application.dataPath,"../ArtSource/SpreadEquipment3D/worn-source.json")));source.Validate();
            var library=SpreadEquipment3DLibrary.Load();Assert.NotNull(library);library.Validate();
            foreach(var model in source.models)
            {
                var mesh=library.Find(model.id).Mesh;Assert.AreEqual(model.boxes.Length*24,mesh.vertexCount);Assert.AreEqual(model.boxes.Length*36,mesh.GetIndexCount(0));
                var vertices=mesh.vertices;var uv=mesh.uv;
                for(int boxIndex=0;boxIndex<model.boxes.Length;boxIndex++)
                {
                    var box=model.boxes[boxIndex];var center=new Vector3(box.center[0],box.center[1],box.center[2]);var size=new Vector3(box.size[0],box.size[1],box.size[2]);
                    for(int i=0;i<24;i++)
                    {
                        var p=vertices[boxIndex*24+i]-center;
                        Assert.AreEqual(size.x*.5f,Mathf.Abs(p.x),.000001f);Assert.AreEqual(size.y*.5f,Mathf.Abs(p.y),.000001f);Assert.AreEqual(size.z*.5f,Mathf.Abs(p.z),.000001f);
                        Assert.AreEqual(new Vector2((box.paint+.5f)/42f,.5f),uv[boxIndex*24+i]);
                    }
                }
            }
        }
        [TestCase("hash")][TestCase("material")][TestCase("slot")][TestCase("duplicate")]
        public void MalformedLibraryCannotPublishIndices(string mode)
        {
            var actual=SpreadEquipment3DLibrary.Load();Assert.NotNull(actual);var copy=UnityEngine.Object.Instantiate(actual);
            try
            {
                copy.Entries=actual.Entries.Select(e=>new SpreadEquipment3DLibrary.Entry{Id=e.Id,Slot=e.Slot,Mesh=e.Mesh}).ToArray();
                if(mode=="hash")copy.SourceSha256="foreign";
                if(mode=="material")copy.Material=null;
                if(mode=="slot")copy.Entries[0].Slot="Hand";
                if(mode=="duplicate")copy.Entries[1].Mesh=copy.Entries[0].Mesh;
                Assert.Throws<InvalidOperationException>(copy.Validate);Assert.DoesNotThrow(actual.Validate);
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        [Test] public void ArbitraryCopiedMeshCannotClaimAdoptedEquipment()
        {
            var library=SpreadEquipment3DLibrary.Load();Assert.NotNull(library);var copy=UnityEngine.Object.Instantiate(library.Entries[0].Mesh);
            try{Assert.False(library.ContainsMesh(copy));Assert.False(library.ContainsMesh(null));Assert.IsNull(library.Find("equipment-helmet"));}
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
    }
}
