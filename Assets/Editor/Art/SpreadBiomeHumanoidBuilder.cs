#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Editor
{
 /// <summary>Explicit offline profession forms on the unchanged approved native
 /// humanoid skeleton. Only the new scoped output root is ever written.</summary>
 public static class SpreadBiomeHumanoidBuilder
 {
  const string Folder="Assets/Resources/SpreadBiome3D/Humanoids",LibraryPath="Assets/Resources/SpreadBiome3D/HumanoidLibrary.asset";
  [Serializable]public sealed class Report{public string status,error,sourceHash;public int models;public string[] assets;}
  public static Report Build(string sourcePath,string reportPath)
  {
   var report=new Report();var changed=new List<string>();GameObject primitive=null;
   try
   {
    if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle native editor required for scoped humanoid adoption.");
    var data=JsonUtility.FromJson<SpreadBiomeHumanoidSource>(File.ReadAllText(sourcePath));report.sourceHash=Hash(sourcePath);
    var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);var glade=ReferenceGladeVoxelLibrary.Load();if(ring==null||glade==null)throw new InvalidOperationException("Approved body/rig/palette sources required.");ring.Validate();glade.Validate();
    if(data?.schemaVersion!=1||data.sourceRig!="ring-nam"||data.roles?.Length!=52||data.palette?.Length!=24||data.preserveNativeModelIds==null||!data.preserveNativeModelIds.SequenceEqual(new[]{"ring-player","ring-sien","ring-nam"}))throw new InvalidOperationException("Exact humanoid source contract required.");
    var palette=glade.Material.GetTexture("_BaseMap")as Texture2D;if(palette==null||!palette.isReadable||palette.width!=24||palette.height!=1)throw new InvalidOperationException("Approved palette unavailable.");
    for(int i=0;i<24;i++)if(!ColorUtility.TryParseHtmlString(data.palette[i],out var c)||Vector4.Distance(c,palette.GetPixel(i,0))>.00001f)throw new InvalidOperationException("Humanoid source palette mismatch.");
    var ids=new HashSet<string>(StringComparer.Ordinal);
    foreach(var role in data.roles)
    {
     SpreadBiomeHumanoidSource.ValidateRole(role);
     if(!ids.Add(role.id))throw new InvalidOperationException("Duplicate explicit profession source.");
     Preflight<Mesh>(Folder+"/"+role.id+".asset");Preflight<GameObject>(Folder+"/"+role.id+".prefab");
    }
    Preflight<SpreadBiomeHumanoidLibrary>(LibraryPath);
    var source=ring.FindModel(data.sourceRig);if(source==null)throw new InvalidOperationException("Borrowed source body missing.");
    var sourceSkin=source.GetComponentInChildren<SkinnedMeshRenderer>();var sourceAnimator=source.GetComponentInChildren<Animator>();
    if(sourceSkin==null||sourceAnimator==null||sourceAnimator.runtimeAnimatorController==null||source.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Expected unchanged humanoid source rig.");
    var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);primitive=cube;cube.hideFlags=HideFlags.HideAndDontSave;var cubeMesh=cube.GetComponent<MeshFilter>().sharedMesh;
    // Build/validate every plan before modifying any persistent output.
    var plans=data.roles.Select(role=>Prepare(role,source,sourceSkin,cubeMesh)).ToArray();
    EnsureFolder(Folder);var entries=new List<SpreadBiomeHumanoidLibrary.Entry>();
    for(int i=0;i<data.roles.Length;i++)
    {
     var role=data.roles[i];string meshPath=Folder+"/"+role.id+".asset",prefabPath=Folder+"/"+role.id+".prefab";
     var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(mesh==null){mesh=new Mesh{name=role.id};AssetDatabase.CreateAsset(mesh,meshPath);}plans[i].Fill(mesh);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);changed.Add(meshPath);
     var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;
     try
     {
      instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);instance.name=role.id;
      var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();var animator=instance.GetComponentInChildren<Animator>();var envelope=skin.localBounds;
      skin.sharedMesh=mesh;skin.sharedMaterial=glade.Material;envelope.Encapsulate(mesh.bounds.min);envelope.Encapsulate(mesh.bounds.max);skin.localBounds=envelope;
      animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
      instance.transform.localScale*=role.stature;
      // Additional attachment anchors follow real existing bones. They do not
      // become animation bones and cannot alter source bindposes or rig clips.
      AddAnchor(instance,skin,"Equipment.Body","Spine",new Vector3(0,.99f,0));
      AddAnchor(instance,skin,"Equipment.Feet.L","Leg.L",new Vector3(-.17f,.10f,-.045f));
      AddAnchor(instance,skin,"Equipment.Feet.R","Leg.R",new Vector3(.17f,.10f,-.045f));
      var spec=new SpawnRing3DCatalog.Model{id=role.id,path=prefabPath,sourceBlueprint=role.blueprint,kind="actor",rigFamily="humanoid",rigged=true,materialFamily="reference-glade-palette",boundsCenter=skin.bounds.center,boundsSize=skin.bounds.size,triangles=(int)mesh.GetIndexCount(0)/3,clips=new[]{"Idle","Walk","Interact","Attack","Hit"},sockets=new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back","Equipment.Body","Equipment.Feet.L","Equipment.Feet.R"}};
      var prefab=PrefabUtility.SaveAsPrefabAsset(instance,prefabPath,out bool ok);if(!ok||prefab==null)throw new InvalidOperationException("Scoped humanoid prefab save refused.");
      entries.Add(new SpreadBiomeHumanoidLibrary.Entry{Id=role.id,Blueprint=role.blueprint,Mesh=mesh,Prefab=prefab,Spec=spec});changed.Add(prefabPath);
     }
     finally{if(instance!=null)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);}
    }
    var library=AssetDatabase.LoadAssetAtPath<SpreadBiomeHumanoidLibrary>(LibraryPath);if(library==null){library=ScriptableObject.CreateInstance<SpreadBiomeHumanoidLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
    library.Entries=entries.ToArray();library.Material=glade.Material;library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);changed.Add(LibraryPath);report.models=entries.Count;report.status="passed";
   }
   catch(Exception e){report.status="failed";report.error=e.ToString();throw;}
   finally{if(primitive!=null)Object.DestroyImmediate(primitive);report.assets=changed.ToArray();string full=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(full));File.WriteAllText(full,JsonUtility.ToJson(report,true));}
   return report;
  }
  // Cosmetic metadata is owned by this scoped package. The original three
  // glade bodies and borrowed source geometry have no such channel.
  sealed class ScopedPlan
  {
   internal ReferenceGladeHumanoidMeshBuilder.Plan Geometry;
   internal readonly List<Vector2> CoverMarkers=new List<Vector2>();
   internal void Fill(Mesh ownedMesh)
   {
    if(CoverMarkers.Count!=Geometry.Vertices.Count)throw new InvalidOperationException("Incomplete cosmetic headwear metadata.");
    Geometry.Fill(ownedMesh);
    ownedMesh.SetUVs(1,CoverMarkers);
   }
  }
  static ScopedPlan Prepare(SpreadBiomeHumanoidSource.Role role,GameObject prefab,SkinnedMeshRenderer skin,Mesh cube)
  {
   var plan=ReferenceGladeHumanoidMeshBuilder.Prepare("ring-nam",prefab,skin,skin.sharedMesh,cube);
   for(int i=0;i<plan.Uvs.Count;i++){int previous=Mathf.FloorToInt(plan.Uvs[i].x*24);int color=previous==17?role.skin:role.body;plan.Uvs[i]=new Vector2((color+.5f)/24,.5f);}
   var scoped=new ScopedPlan{Geometry=plan};
   scoped.CoverMarkers.AddRange(Enumerable.Repeat(Vector2.zero,plan.Vertices.Count));
   var names=skin.bones.Select(b=>b.name).ToArray();var cv=cube.vertices;var ct=cube.triangles;var toMesh=skin.transform.worldToLocalMatrix*prefab.transform.localToWorldMatrix;
   float head=prefab.transform.InverseTransformPoint(prefab.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Equipment.Head").position).y;
   void Box(Vector3 center,Vector3 size,string bone,int color,bool coverable=false)
   {
    int start=plan.Vertices.Count,index=Array.IndexOf(names,bone);if(index<0)throw new InvalidOperationException("Missing profession anchor bone.");
    foreach(var v in cv){plan.Vertices.Add(toMesh.MultiplyPoint3x4(center+Vector3.Scale(v,size)));plan.Uvs.Add(new Vector2((color+.5f)/24,.5f));plan.Weights.Add(new BoneWeight{boneIndex0=index,weight0=1});scoped.CoverMarkers.Add(coverable?Vector2.right:Vector2.zero);}
    for(int i=0;i<ct.Length;i+=3){plan.Triangles.Add(start+ct[i]);plan.Triangles.Add(start+ct[i+(toMesh.determinant>0?1:2)]);plan.Triangles.Add(start+ct[i+(toMesh.determinant>0?2:1)]);}
   }
   // Faces stay legible under the actual top-down camera: small eyes and hair
   // leave the approved pale face and separate arms/legs intact.
   Box(new Vector3(-.075f,head-.15f,-.14f),new Vector3(.035f,.035f,.025f),"Head",23);
   Box(new Vector3(.075f,head-.15f,-.14f),new Vector3(.035f,.035f,.025f),"Head",23);
   if(role.headwear=="ears")
   {Box(new Vector3(-.185f,head-.07f,0),new Vector3(.11f,.16f,.09f),"Head",role.skin);Box(new Vector3(.185f,head-.07f,0),new Vector3(.11f,.16f,.09f),"Head",role.skin);Box(new Vector3(0,head-.20f,-.165f),new Vector3(.08f,.08f,.07f),"Head",role.skin);}
   else
   {
    bool hat=role.headwear!="hair"&&role.headwear!="long-hair";int color=hat?role.accent:role.hair;
    Box(new Vector3(0,head-.025f,0),new Vector3(.32f,.08f,.29f),"Head",color,true);
    if(role.headwear=="straw-hat")Box(new Vector3(0,head-.055f,0),new Vector3(.50f,.05f,.46f),"Head",role.accent,true);
    if(role.headwear=="tall-cap")Box(new Vector3(0,head+.09f,0),new Vector3(.24f,.20f,.23f),"Head",role.accent,true);
    if(role.headwear=="hood"||role.headwear=="long-hair")Box(new Vector3(0,head-.20f,.135f),new Vector3(.33f,.38f,.09f),"Head",color,role.headwear=="hood");
    if(role.headwear=="goggles")Box(new Vector3(0,head-.115f,-.16f),new Vector3(.28f,.06f,.04f),"Head",role.accent,true);
   }
   Box(new Vector3(0,.755f,-.14f),new Vector3(.39f,.065f,.035f),"Spine",role.accent);
   switch(role.garment)
   {
    case "apron":Box(new Vector3(0,.95f,-.16f),new Vector3(.31f,.48f,.035f),"Spine",role.accent);break;
    case "sash":Box(new Vector3(-.12f,1.04f,-.16f),new Vector3(.09f,.50f,.035f),"Spine",role.accent);break;
    case "pouches":Box(new Vector3(-.21f,.75f,0),new Vector3(.12f,.17f,.17f),"Spine",role.accent);Box(new Vector3(.21f,.75f,0),new Vector3(.12f,.17f,.17f),"Spine",role.accent);break;
    case "book-pouch":Box(new Vector3(.23f,.76f,.025f),new Vector3(.15f,.26f,.20f),"Spine",role.accent);Box(new Vector3(.23f,.88f,.025f),new Vector3(.12f,.045f,.17f),"Spine",17);break;
    case "cloak":case "coat":Box(new Vector3(0,.91f,.19f),new Vector3(.43f,.66f,.08f),"Spine",role.accent);break;
   }
   return scoped;
  }
  static void AddAnchor(GameObject root,SkinnedMeshRenderer skin,string name,string bone,Vector3 position)
  {if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==name))throw new InvalidOperationException("Duplicate scoped equipment anchor.");var parent=skin.bones.Single(t=>t.name==bone);var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=root.transform.TransformPoint(position);go.transform.rotation=root.transform.rotation;}
  static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  static void Preflight<T>(string path)where T:Object{var value=AssetDatabase.LoadMainAssetAtPath(path);if(value!=null&&!(value is T))throw new InvalidOperationException("Refusing wrong-type scoped output: "+path);}
  static void EnsureFolder(string path){var parent=Path.GetDirectoryName(path).Replace('\\','/');if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
 }
}
#endif
