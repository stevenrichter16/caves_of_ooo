using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadHeadwearCoverTests
    {
        const string Property="_CoverHeadwear";
        [TestCase("Farmer")][TestCase("Scribe")][TestCase("Elder")]
        public void AuthoredCosmeticHeadwearHasExplicitVertexMarkersButFaceAndBodyDoNot(string blueprint)
        {
            var entry=SpreadBiomeHumanoidLibrary.Load().Find(SpreadBiomeHumanoidLibrary.ModelId(blueprint));
            Assert.NotNull(entry);var mesh=entry.Mesh;var markers=mesh.uv2;
            Assert.AreEqual(mesh.vertexCount,markers.Length,"Explicit coverable metadata belongs to the adopted persistent mesh.");
            Assert.True(markers.Any(x=>x.x==1));Assert.True(markers.Any(x=>x.x==0));
            var skin=entry.Prefab.GetComponentInChildren<SkinnedMeshRenderer>();var weights=mesh.boneWeights;
            for(int i=0;i<markers.Length;i++)
            {
                Assert.That(markers[i].x,Is.EqualTo(0).Or.EqualTo(1));Assert.AreEqual(0,markers[i].y);
                if(markers[i].x==1)Assert.AreEqual("Head",skin.bones[weights[i].boneIndex0].name);
            }
            var vertices=mesh.vertices;var matrix=entry.Prefab.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
            var socket=entry.Prefab.GetComponentsInChildren<Transform>().Single(t=>t.name=="Equipment.Head");
            float head=entry.Prefab.transform.InverseTransformPoint(socket.position).y;
            int face=0;
            for(int i=0;i<vertices.Length;i++)
            {
                var v=matrix.MultiplyPoint3x4(vertices[i]);
                if(skin.bones[weights[i].boneIndex0].name=="Head"&&v.y<head-.10f&&v.z<-.13f)
                {face++;Assert.AreEqual(0,markers[i].x,"Lower face and eyes are not cosmetic headwear.");}
            }
            Assert.Greater(face,4);
        }
        [Test]public void AnatomicalEarsCannotBeMarkedAsCoverableHat()
        {
            var entry=SpreadBiomeHumanoidLibrary.Load().Find(SpreadBiomeHumanoidLibrary.ModelId("SootGremlin"));
            Assert.NotNull(entry);var marker=entry.Mesh.uv2;
            Assert.True(marker.Length==0||marker.Length==entry.Mesh.vertexCount);
            Assert.True(marker.All(x=>x==Vector2.zero));
        }
        [TestCase("Farmer")][TestCase("Scribe")][TestCase("Elder")]
        public void ActualHeadEquipCoversOnlyOwnedCosmeticsAndUnequipRestoresThem(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);var neighbor=f.Add(blueprint);f.CleanGear(actor);f.CleanGear(neighbor);f.Refresh();
                var body=Body(f,actor);var other=Body(f,neighbor);var mesh=body.sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;var marker=mesh.uv2;
                var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);block.SetFloat("_CoverReviewSentinel",17);body.SetPropertyBlock(block);
                var indexed=new MaterialPropertyBlock();body.GetPropertyBlock(indexed,0);indexed.SetFloat("_CoverIndexSentinel",23);body.SetPropertyBlock(indexed,0);
                Assert.AreEqual(0,Cover(body));var item=Equip(f,actor,"IronHelmet");f.Refresh();
                Assert.AreEqual(1,Cover(body));Assert.AreEqual(1,Cover(body,0));Assert.AreEqual(0,Cover(other));
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(actor,item,out var proof),proof.Failure);
                body.GetPropertyBlock(block);body.GetPropertyBlock(indexed,0);Assert.AreEqual(17,block.GetFloat("_CoverReviewSentinel"));Assert.AreEqual(23,indexed.GetFloat("_CoverIndexSentinel"));
                Assert.True(InventorySystem.UnequipItem(actor,item));f.Refresh();Assert.AreEqual(0,Cover(body));Assert.AreEqual(0,Cover(body,0));
                CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(uv,mesh.uv);CollectionAssert.AreEqual(marker,mesh.uv2);
                Assert.AreEqual(0,SpreadBiomeHumanoidLibrary.Load().Material.GetFloat(Property));
            }
        }
        [Test]public void OrdinaryAbsentIndexedOverrideStaysAbsentSoNativeRootPropertiesRemainEffective()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("Farmer");f.CleanGear(actor);f.Refresh();var body=Body(f,actor);
                // A legal ordinary renderer has no per-material override. Creating
                // one with only cover would replace, not inherit, the root block.
                body.SetPropertyBlock(null,0);var index=new MaterialPropertyBlock();body.GetPropertyBlock(index,0);Assert.True(index.isEmpty);
                var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);block.SetFloat("_Transient",1);block.SetFloat("_CoverNativeSentinel",31);body.SetPropertyBlock(block);
                Equip(f,actor,"IronHelmet");f.Refresh();body.GetPropertyBlock(block);body.GetPropertyBlock(index,0);
                Assert.AreEqual(1,block.GetFloat(Property));Assert.AreEqual(1,block.GetFloat("_Transient"));Assert.AreEqual(31,block.GetFloat("_CoverNativeSentinel"));
                Assert.True(index.isEmpty,"No cover-only indexed block may override the native root visibility/light block.");
            }
        }
        [TestCase("carried")][TestCase("foreign-physics")][TestCase("foreign-equip")][TestCase("removed-slot")]
        public void InvalidOrCarriedHeadItemCannotHideARealOwnersCosmetics(string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("Farmer");f.CleanGear(actor);var item=Equip(f,actor,"IronHelmet");f.Refresh();var body=Body(f,actor);
                switch(mutation)
                {
                    case "carried":Assert.True(InventorySystem.UnequipItem(actor,item));break;
                    case "foreign-physics":item.GetPart<PhysicsPart>().Equipped=f.Player;break;
                    case "foreign-equip":item.GetPart<EquippablePart>().ParentEntity=f.Player;break;
                    case "removed-slot":actor.GetPart<InventoryPart>().FindEquippedBodyPart(item)._Equipped=null;break;
                }
                f.Refresh();Assert.AreEqual(0,Cover(body));Assert.AreEqual(0,Cover(body,0));
            }
        }
        [Test]public void ForeignProfileAndUnmarkedOriginalPlayerStayUnmasked()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Player;f.CleanGear(actor);Equip(f,actor,"IronHelmet");f.Refresh();
                foreach(var renderer in f.View(actor).GetComponentsInChildren<Renderer>())Assert.AreEqual(0,Cover(renderer));
            }
        }
        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void ActualProductionGpuPassMasksOnlyMarkedCoveredCosmetics(int pass)
        {
            Assert.Greater(Draw(pass,false,false),.05,"Unmarked default geometry actually draws.");
            Assert.Greater(Draw(pass,true,false),.05,"An unworn cosmetic still draws.");
            Assert.Greater(Draw(pass,false,true),.05,"Body/anatomy remains visible when headgear is worn.");
            Assert.Less(Draw(pass,true,true),.00001,"Covered cosmetic is absent in actual forward/shadow/depth production fragment.");
        }
        static float Draw(int pass,bool marker,bool covered)
        {
            var shader=Shader.Find("Hidden/CavesOfOoo/Tests/SpreadHeadwearCoverProbe");Assert.NotNull(shader);Assert.True(shader.isSupported);
            var material=new Material(shader);var fog=new Texture2D(80,25,TextureFormat.RGBA32,false,true);var mesh=new Mesh();
            var target=new RenderTexture(64,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);Texture2D readback=null;var previous=RenderTexture.active;
            string[] globals={"unity_SHAr","unity_SHAg","unity_SHAb","unity_SHBr","unity_SHBg","unity_SHBb","unity_SHC","_MainLightColor"};var old=globals.Select(Shader.GetGlobalVector).ToArray();
            try
            {
                for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],i<3?new Vector4(0,0,0,.6f):Vector4.zero);
                fog.SetPixels(Enumerable.Repeat(Color.white,80*25).ToArray());fog.Apply();fog.filterMode=FilterMode.Point;
                material.SetTexture("_FogLight",fog);material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetFloat(Property,covered?1:0);
                mesh.vertices=new[]{new Vector3(39,0,12),new Vector3(42,0,12),new Vector3(42,3,12),new Vector3(39,3,12)};
                mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();mesh.uv=Enumerable.Repeat(new Vector2(.5f,.5f),4).ToArray();mesh.uv2=Enumerable.Repeat(new Vector2(marker?1:0,0),4).ToArray();mesh.triangles=new[]{0,1,2,0,2,3};
                target.Create();RenderTexture.active=target;GL.Clear(false,true,Color.black);GL.PushMatrix();
                try{GL.LoadProjectionMatrix(Matrix4x4.Ortho(39,42,0,3,-100,100));GL.modelview=Matrix4x4.identity;Assert.True(material.SetPass(pass));Graphics.DrawMeshNow(mesh,Matrix4x4.identity);}
                finally{GL.PopMatrix();}
                readback=new Texture2D(64,64,TextureFormat.RGBA32,false,true);readback.ReadPixels(new Rect(0,0,64,64),0,0);readback.Apply();return readback.GetPixels().Average(c=>(c.r+c.g+c.b)/3);
            }
            finally
            {
                for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],old[i]);RenderTexture.active=previous;
                if(readback!=null)Object.DestroyImmediate(readback);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(mesh);Object.DestroyImmediate(fog);Object.DestroyImmediate(material);
            }
        }
        static SkinnedMeshRenderer Body(SpawnRing3DIntegrationFixture f,Entity actor)
        {var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.True(presenter.TryGetApprovedStyle(actor,out var proof),proof.Failure);return f.View(actor).GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.sharedMesh==proof.SubmittedMesh);}
        static float Cover(Renderer renderer,int index=-1)
        {var b=new MaterialPropertyBlock();if(index<0)renderer.GetPropertyBlock(b);else renderer.GetPropertyBlock(b,index);return b.GetFloat(Property);}
        static Entity Equip(SpawnRing3DIntegrationFixture f,Entity actor,string blueprint)
            =>(Entity)typeof(SpreadEquipmentFitGalleryTests).GetMethod("EquipObserved",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f,actor,blueprint});
    }
}
