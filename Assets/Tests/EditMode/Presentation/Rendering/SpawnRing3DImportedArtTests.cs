#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Adopt ONLY after the first real complete export is imported. These verify
    // actual imported resources; the separate catalog fixture is not used here.
    [Category("SpawnRing3DRealArt")]
    public sealed class SpawnRing3DImportedArtTests
    {
        static object Library()
        {
            var type=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.SpawnRing3DLibrary");Assert.NotNull(type);
            var library=Resources.Load("SpawnRing3D/Library",type);Assert.NotNull(library,"Run the explicit importer on the completed real export.");
            Call(library,"Validate");return library;
        }
        static object Call(object target,string name,params object[] args)
        {
            var m=target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public);Assert.NotNull(m);
            try{return m.Invoke(target,args);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        static T Field<T>(object target,string name)
        {var f=target.GetType().GetField(name);Assert.NotNull(f);return (T)f.GetValue(target);}
        static SpawnRing3DCatalogTests.Doc Doc(object library)=>JsonUtility.FromJson<SpawnRing3DCatalogTests.Doc>(Field<TextAsset>(library,"Catalog").text);
        static GameObject Prefab(object library,string id)=>(GameObject)Call(library,"FindModel",id);
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static Mesh MeshOf(Renderer renderer)=>renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;

        [Test] public void EveryCompletedModelIsAnActualImportedFbxAndPersistentPrefab()
        {
            var library=Library();var doc=Doc(library);Assert.AreEqual(218,doc.models.Length);
            var catalog=Field<TextAsset>(library,"Catalog");
            Assert.AreEqual("Assets/Art3D/SpawnRing/Definitions/catalog.json",AssetDatabase.GetAssetPath(catalog));
            foreach(var model in doc.models)
            {
                var prefab=Prefab(library,model.id);Assert.NotNull(prefab,model.id);
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabAsset(prefab),model.id);
                Assert.AreEqual("Assets/Art3D/SpawnRing/Prefabs/"+model.id+".prefab",AssetDatabase.GetAssetPath(prefab));
                string fbx="Assets/Art3D/SpawnRing/Models/"+model.id+".fbx";
                Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>(fbx),model.id);
                var importer=AssetImporter.GetAtPath(fbx) as ModelImporter;Assert.NotNull(importer);
                Assert.AreEqual(model.rigged,importer.importAnimation,model.id);Assert.IsFalse(importer.optimizeGameObjects,model.id);
                Assert.IsFalse(importer.importCameras);Assert.IsFalse(importer.importLights);Assert.IsFalse(importer.addCollider);
                if(!model.rigged)Assert.IsTrue(importer.isReadable,model.id+" runtime ground patch import policy");
                Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0),model.id);
            }
        }
        [Test] public void ActualImportedGeometryHasPositiveTrianglesAndStaticAxesMatchExportedBounds()
        {
            var library=Library();var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var model in Doc(library).models)
                {
                    GameObject root=null;
                    try
                    {
                        root=(GameObject)PrefabUtility.InstantiatePrefab(Prefab(library,model.id),preview);
                        Assert.NotNull(root,model.id);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
                        var renderers=root.GetComponentsInChildren<Renderer>(true);Assert.That(renderers.Length,Is.GreaterThan(0),model.id);
                        var bounds=renderers[0].bounds;long triangles=0;
                        foreach(var r in renderers)
                        {
                            bounds.Encapsulate(r.bounds);var mesh=MeshOf(r);Assert.NotNull(mesh,model.id+"/"+r.name);Assert.Greater(mesh.vertexCount,0);
                            if(!model.rigged)Assert.IsTrue(mesh.isReadable,model.id+" actual mesh must support runtime CombineMeshes");
                            for(int i=0;i<mesh.subMeshCount;i++)triangles+=mesh.GetIndexCount(i)/3;
                        }
                        Assert.Greater(triangles,0,model.id);Assert.AreEqual((long)model.triangles,triangles,model.id+" exported triangle metadata");
                        Assert.IsTrue(Finite(bounds.center.x)&&Finite(bounds.center.y)&&Finite(bounds.center.z),model.id);
                        Assert.IsTrue(Finite(bounds.size.x)&&Finite(bounds.size.y)&&Finite(bounds.size.z),model.id);
                        Assert.Greater(bounds.size.x,0);Assert.Greater(bounds.size.y,0);Assert.Greater(bounds.size.z,0);
                        if(!model.rigged)
                        {
                            float tolerance=Mathf.Max(.035f,model.boundsSize.magnitude*.02f);
                            Assert.LessOrEqual((bounds.center-model.boundsCenter).magnitude,tolerance,model.id+" center/axis");
                            Assert.LessOrEqual((bounds.size-model.boundsSize).magnitude,tolerance,model.id+" size/scale");
                        }
                        Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true),model.id+" must use native picking/collision");
                        Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>(true),model.id);
                    }
                    finally{if(root!=null)Object.DestroyImmediate(root);}
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        [Test] public void EverySpeciesRigHasActualSkinnedBonesFiveClipsAndOnlyHumanoidsHaveEquipmentSockets()
        {
            var library=Library();int rigs=0,nonhuman=0;
            foreach(var model in Doc(library).models)
            {
                var prefab=Prefab(library,model.id);var animators=prefab.GetComponentsInChildren<Animator>(true);
                if(!model.rigged){Assert.IsEmpty(animators,model.id);continue;}
                rigs++;if(model.rigFamily!="humanoid")nonhuman++;
                Assert.AreEqual(1,animators.Length,model.id);var animator=animators[0];
                Assert.NotNull(animator.avatar,model.id);Assert.IsTrue(animator.avatar.isValid,model.id);
                Assert.NotNull(animator.runtimeAnimatorController,model.id);Assert.IsFalse(animator.applyRootMotion,model.id);
                var clips=animator.runtimeAnimatorController.animationClips;
                foreach(string name in new[]{"Idle","Walk","Interact","Attack","Hit"})
                {var matches=clips.Where(c=>c!=null&&c.name==name).ToArray();Assert.AreEqual(1,matches.Length,model.id+"/"+name);Assert.Greater(matches[0].length,0);}
                var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.IsNotEmpty(skins,model.id);
                foreach(var skin in skins){Assert.NotNull(skin.sharedMesh);Assert.IsNotEmpty(skin.bones);Assert.IsTrue(skin.bones.All(b=>b!=null));}
                var sockets=prefab.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Equipment.",StringComparison.Ordinal)).Select(t=>t.name).ToArray();
                if(model.rigFamily=="humanoid")CollectionAssert.AreEquivalent(new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},sockets,model.id);
                else Assert.IsEmpty(sockets,model.id+" must not acquire fake hands");
            }
            Assert.AreEqual(21,rigs);Assert.AreEqual(12,nonhuman);
        }
        [Test] public void ActualRingPaletteAndWaterAreIndependentAndEveryImportedMaterialSlotIsMapped()
        {
            var library=Library();var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(village);
            var world=Field<Material>(library,"WorldMaterial");var water=Field<Material>(library,"WaterMaterial");
            Assert.AreNotSame(world,water);Assert.AreNotSame(village.WorldMaterial,world);Assert.AreNotSame(village.WaterMaterial,water);
            Assert.AreSame(village.WorldMaterial.shader,world.shader);Assert.AreSame(village.WaterMaterial.shader,water.shader);
            var palette=world.GetTexture("_BaseMap") as Texture2D;Assert.NotNull(palette);Assert.Greater(palette.width,0);Assert.Greater(palette.height,0);
            Assert.AreEqual("Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png",AssetDatabase.GetAssetPath(palette));Assert.AreNotSame(village.WorldMaterial.GetTexture("_BaseMap"),palette);
            foreach(var material in new[]{world,water})
            {
                var fog=material.GetTexture("_FogLight") as Texture2D;Assert.NotNull(fog);Assert.AreEqual(80,fog.width);Assert.AreEqual(25,fog.height);
                Assert.AreEqual(FilterMode.Point,fog.filterMode);Assert.IsTrue(fog.GetPixels32().All(c=>c.a==0),"Unbound material fails closed.");
            }
            foreach(var model in Doc(library).models)
                foreach(var renderer in Prefab(library,model.id).GetComponentsInChildren<Renderer>(true))
                {Assert.IsNotEmpty(renderer.sharedMaterials,model.id);foreach(var m in renderer.sharedMaterials)Assert.IsTrue(m==world||m==water,model.id+"/"+renderer.name);}
        }
        [Test] public void SharedEquipmentResolvesExactlyTheExistingSixRealPrefabsWithoutAssetDuplication()
        {
            var library=Library();var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(village);
            Assert.AreSame(village,Field<Village3DLibrary>(library,"EquipmentLibrary"));
            foreach(string id in Doc(library).externalEquipment.models)
            {
                var model=(GameObject)Call(library,"FindEquipmentModel",id);Assert.NotNull(model,id);Assert.AreSame(village.FindModel(id),model,id);
                Assert.That(model.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0),id);
                Assert.IsTrue(AssetDatabase.GetAssetPath(model).StartsWith("Assets/Art3D/Village/Prefabs/",StringComparison.Ordinal));
            }
            Assert.IsNull(Call(library,"FindEquipmentModel","character-teal"));Assert.IsNull(Call(library,"FindModel","unmodeled-drop"));
        }
    }
}
#endif
