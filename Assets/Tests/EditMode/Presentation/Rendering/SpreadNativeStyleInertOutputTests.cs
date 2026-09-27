using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeStyleInertOutputTests
 {
  [TestCase(false)][TestCase(true)]
  public void ImportReuseRequiresAnInertOutputWithoutNativePhysics(bool addPhysics)
  {
   var original=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(original);original.Validate();var entry=original.Find("ring-sack");Assert.NotNull(entry);
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.SpreadNativeStyleBuilder",false)).FirstOrDefault(t=>t!=null);Assert.NotNull(type);
   var matcher=type.GetMethod("CanReusePreparedOutput",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(matcher);
   var preview=EditorSceneManager.NewPreviewScene();var root=(GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab,preview);
   try {
    Assert.True((bool)matcher.Invoke(null,new object[]{entry.Id,entry.Mesh,root}),"exact owned clone first passes");
    if(addPhysics)root.AddComponent<Rigidbody>();
    Assert.AreEqual(!addPhysics,(bool)matcher.Invoke(null,new object[]{entry.Id,entry.Mesh,root}));
    Assert.IsNull(entry.Prefab.GetComponent<Rigidbody>(),"borrowed persistent prefab unchanged");
   } finally { UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview); }
  }
  [TestCase(false)][TestCase(true)]
  public void LoadedLibraryRequiresAnInertOutputWithoutNativePhysics(bool addPhysics)
  {
   var original=SpreadNativeStyle3DLibrary.Load();Assert.NotNull(original);original.Validate();var copy=UnityEngine.Object.Instantiate(original);
   var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;
   try {
    var entry=copy.Entries.Single(e=>e.Id=="ring-sack");Assert.AreNotSame(original.Find(entry.Id),entry);
    root=(GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab,preview);entry.Prefab=root;
    Assert.DoesNotThrow(()=>copy.Validate(),"exact owned clone first passes");
    if(addPhysics)root.AddComponent<Rigidbody>();
    if(addPhysics)Assert.Throws<InvalidOperationException>(()=>copy.Validate());else Assert.DoesNotThrow(()=>copy.Validate());
    Assert.IsNull(original.Find(entry.Id).Prefab.GetComponent<Rigidbody>(),"borrowed persistent prefab unchanged");original.Validate();
   } finally { if(root!=null)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(copy);EditorSceneManager.ClosePreviewScene(preview); }
  }
 }
}
