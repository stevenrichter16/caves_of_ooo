using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeStyleImporterReuseTests
 {
  static MethodInfo Matcher(){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.SpreadNativeStyleBuilder",false)).FirstOrDefault(t=>t!=null);Assert.NotNull(type);var method=type.GetMethod("CanReusePreparedOutput",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method,"Interrupted import must validate completed pairs before rewriting them");return method;}
  [TestCase("unchanged")][TestCase("missing-mesh")][TestCase("missing-prefab")][TestCase("vertex")][TestCase("normal")][TestCase("uv")][TestCase("index")][TestCase("material")][TestCase("position")][TestCase("scale")][TestCase("disabled")][TestCase("collider")][TestCase("bounds")][TestCase("shadow-casting")][TestCase("receive-shadows")]
  public void OnlyAnExactCompleteOutputPairCanBeReused(string change)
  {
   var matcher=Matcher();var source=JsonUtility.FromJson<SpreadNativeStyleSource>(File.ReadAllText(Path.Combine(Application.dataPath,"../ArtSource/SpreadNativeStyle3D/sources.json")));
   string id=source.models[0].id,path=SpreadNativeStyle3DLibrary.Folder+"/"+id;var originalMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+".asset");var originalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(path+".prefab");Assert.NotNull(originalMesh,"Actual completed interrupted pair required");Assert.NotNull(originalPrefab);
   byte[] meshBytes=File.ReadAllBytes(path+".asset"),prefabBytes=File.ReadAllBytes(path+".prefab");Assert.True((bool)matcher.Invoke(null,new object[]{id,originalMesh,originalPrefab}),"completed persistent pair");
   var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var mesh=UnityEngine.Object.Instantiate(originalMesh);var prefab=(GameObject)PrefabUtility.InstantiatePrefab(originalPrefab,preview);prefab.GetComponent<MeshFilter>().sharedMesh=mesh;
   try{
    Assert.True((bool)matcher.Invoke(null,new object[]{id,mesh,prefab}),"same exact content, owned test copy");
    if(change=="vertex"){var a=mesh.vertices;a[0]+=Vector3.up*.1f;mesh.vertices=a;}
    else if(change=="normal"){var a=mesh.normals;a[0]=Vector3.up;mesh.normals=a;if(originalMesh.normals[0]==Vector3.up){a[0]=Vector3.right;mesh.normals=a;}}
    else if(change=="uv"){var a=mesh.uv;a[0]+=Vector2.right*.1f;mesh.uv=a;}
    else if(change=="index"){var a=mesh.GetIndices(0);int q=a[0];a[0]=a[1];a[1]=q;mesh.SetIndices(a,MeshTopology.Triangles,0);}
    else if(change=="material")prefab.GetComponent<MeshRenderer>().sharedMaterial=null;
    else if(change=="position")prefab.transform.localPosition=Vector3.right;
    else if(change=="scale")prefab.transform.localScale*=.8f;
    else if(change=="disabled")prefab.GetComponent<MeshRenderer>().enabled=false;
    else if(change=="collider")prefab.AddComponent<BoxCollider>();
    else if(change=="bounds")mesh.bounds=new Bounds(Vector3.zero,Vector3.zero);
    else if(change=="shadow-casting")prefab.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    else if(change=="receive-shadows")prefab.GetComponent<MeshRenderer>().receiveShadows=false;
    bool actual=(bool)matcher.Invoke(null,new object[]{id,change=="missing-mesh"?null:mesh,change=="missing-prefab"?null:prefab});Assert.AreEqual(change=="unchanged",actual,change);
    CollectionAssert.AreEqual(meshBytes,File.ReadAllBytes(path+".asset"));CollectionAssert.AreEqual(prefabBytes,File.ReadAllBytes(path+".prefab"));
   }finally{UnityEngine.Object.DestroyImmediate(prefab);UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
  }
 }
}
