#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace CavesOfOoo.Editor
{
 /// <summary>Explicit offline palette conversion of reviewed native geometry.
 /// Complete source/output preflight precedes writes. No source importer edits.</summary>
 public static class SpreadNativeStyleBuilder
 {
  const string SourcePath="ArtSource/SpreadNativeStyle3D/sources.json";
  const long MaximumTotalVertices=12000000;
  sealed class Plan
  {
   public SpreadNativeStyleSource.Model Source;public GameObject Original;public Material[] Materials;
   public Vector3[] Vertices,Normals;public Vector2[] UV;public int[][] Indices;public Mesh[] SourceMeshes;public Matrix4x4[] SourceTransforms;
  }
  public static void Run() { RunCore(null, false); }

  /// <summary>Refresh only the named existing reviewed outputs. Every other
  /// accepted output and entry must still match its current native source before
  /// any write. This supports a bounded source-art repair, not arbitrary import.</summary>
  public static void RunSelected(string[] modelIds) { RunCore(modelIds, true); }

  /// <summary>Execute exactly the selected writer's complete preparation and
  /// retained-output checks without creating, saving or changing any asset.</summary>
  public static void ValidateSelectedImport(string[] modelIds) { PrepareImport(modelIds, true); }

  sealed class PreparedImport
  {
   public string Json;
   public List<Plan> Plans;
   public readonly Dictionary<string, SpreadNativeStyle3DLibrary.Entry> Retained =
       new Dictionary<string, SpreadNativeStyle3DLibrary.Entry>(StringComparer.Ordinal);
  }

  static PreparedImport PrepareImport(string[] modelIds, bool selective)
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
    throw new InvalidOperationException("Static import needs idle Edit mode.");
   string json=File.ReadAllText(SourcePath);
   if(SpreadNativeStyle3DLibrary.Hash(json)!=SpreadNativeStyle3DLibrary.ReviewedSourceSha256)
    throw new InvalidOperationException("Reviewed source hash differs.");
   var source=JsonUtility.FromJson<SpreadNativeStyleSource>(json);source.Validate();
   HashSet<string> selected=null;
   if(selective)
   {
    if(modelIds==null||modelIds.Length==0)throw new ArgumentException("Select at least one existing reviewed model.",nameof(modelIds));
    selected=new HashSet<string>(StringComparer.Ordinal);
    var known=new HashSet<string>(Array.ConvertAll(source.models,row=>row.id),StringComparer.Ordinal);
    foreach(string id in modelIds)
     if(id==null||!known.Contains(id)||!selected.Add(id))throw new ArgumentException("Select distinct existing reviewed models.",nameof(modelIds));
   }
   var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
   var glade=ReferenceGladeVoxelLibrary.Load();
   var voxel=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
   if(ring==null||glade==null||voxel==null)throw new InvalidOperationException("Native source libraries unavailable.");
   ring.Validate();glade.Validate();voxel.Validate();
   string folder=SpreadNativeStyle3DLibrary.Folder;
   Preflight<SpreadNativeStyle3DLibrary>(folder+"/Library.asset");
   foreach(var row in source.models){Preflight<Mesh>(folder+"/"+row.id+".asset");Preflight<GameObject>(folder+"/"+row.id+".prefab");}
   var result=new PreparedImport{Json=json,Plans=new List<Plan>(source.models.Length)};
   var snapshots=new Dictionary<Texture,Texture2D>();
   try
   {
    long total=0;
    foreach(var row in source.models)
    {
     var plan=Prepare(row,ring,glade,voxel,snapshots);total+=plan.Vertices.Length;
     if(total>MaximumTotalVertices)throw new InvalidOperationException("Static copy source budget exceeded.");
     result.Plans.Add(plan);
    }
   }
   finally{foreach(var texture in snapshots.Values)UnityEngine.Object.DestroyImmediate(texture);}
   if(!selective)return result;

   // Selected source buffers may legitimately have changed since the last import,
   // so full Library.Validate would refuse those stale copies. Independently
   // verify every retained row against freshly prepared source data instead.
   var accepted=AssetDatabase.LoadAssetAtPath<SpreadNativeStyle3DLibrary>(folder+"/Library.asset");
   if(accepted==null||accepted.SourceSha256!=SpreadNativeStyle3DLibrary.ReviewedSourceSha256
      ||accepted.SourceManifestJson!=json||accepted.Entries==null||accepted.Entries.Length!=source.models.Length)
    throw new InvalidOperationException("Selected import requires the complete accepted source roster.");
   var byId=new Dictionary<string,Plan>(StringComparer.Ordinal);
   foreach(var plan in result.Plans)byId.Add(plan.Source.id,plan);
   var ordered=new List<Plan>(accepted.Entries.Length);
   var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var entry in accepted.Entries)
   {
    if(entry==null||entry.Id==null||!seen.Add(entry.Id)||!byId.TryGetValue(entry.Id,out var plan))
     throw new InvalidOperationException("Selected import cannot retain a malformed source roster.");
    ordered.Add(plan);
    if(selected.Contains(entry.Id))continue;
    if(!RetainedMatches(entry,plan))
     throw new InvalidOperationException("Unselected output no longer matches its actual native source: "+entry.Id);
    result.Retained.Add(entry.Id,entry);
   }
   result.Plans=ordered;
   return result;
  }

  static bool RetainedMatches(SpreadNativeStyle3DLibrary.Entry entry,Plan plan)
  {
   string path=SpreadNativeStyle3DLibrary.Folder+"/"+plan.Source.id;
   return entry.Kind==plan.Source.kind&&entry.SemanticColors==plan.Source.semanticColors
      &&entry.SourcePrefab==plan.Original&&entry.Mesh==AssetDatabase.LoadAssetAtPath<Mesh>(path+".asset")
      &&entry.Prefab==AssetDatabase.LoadAssetAtPath<GameObject>(path+".prefab")
      &&entry.Material==plan.Materials[0]&&Equal(entry.Materials,plan.Materials)
      &&Equal(entry.SourceMeshes,plan.SourceMeshes)&&Equal(entry.SourceTransforms,plan.SourceTransforms)
      &&PreparedMatches(plan,entry.Mesh,entry.Prefab);
  }

  static void RunCore(string[] modelIds,bool selective)
  {
   var prepared=PrepareImport(modelIds,selective);
   string json=prepared.Json,folder=SpreadNativeStyle3DLibrary.Folder;
   var plans=prepared.Plans;
   if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","SpreadNativeStyle3D");
   var preview=EditorSceneManager.NewPreviewScene();var entries=new List<SpreadNativeStyle3DLibrary.Entry>(plans.Count);int reused=0,written=0;
   try
   {
    foreach(var plan in plans)
    {
     if(prepared.Retained.TryGetValue(plan.Source.id,out var retained)){entries.Add(retained);reused++;continue;}
     string path=folder+"/"+plan.Source.id;var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+".asset");
     var existingPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(path+".prefab");
     if(PreparedMatches(plan,mesh,existingPrefab)){entries.Add(EntryFor(plan,mesh,existingPrefab));reused++;continue;}
     if(mesh==null){mesh=new Mesh{name=plan.Source.id+" approved static palette"};AssetDatabase.CreateAsset(mesh,path+".asset");}
     mesh.Clear(false);mesh.indexFormat=plan.Vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16;
     mesh.vertices=plan.Vertices;mesh.normals=plan.Normals;mesh.uv=plan.UV;mesh.subMeshCount=plan.Indices.Length;for(int sub=0;sub<plan.Indices.Length;sub++)mesh.SetIndices(plan.Indices[sub],MeshTopology.Triangles,sub,false);mesh.RecalculateBounds();
     EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
     var root=new GameObject(plan.Source.id);GameObject prefab;SceneManager.MoveGameObjectToScene(root,preview);
     try{
      root.transform.localRotation=plan.Original.transform.localRotation;root.transform.localScale=plan.Original.transform.localScale;
      root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=plan.Materials;
      prefab=PrefabUtility.SaveAsPrefabAsset(root,path+".prefab");
     }finally{UnityEngine.Object.DestroyImmediate(root);}
     if(prefab==null)throw new InvalidOperationException("Static prefab save failed.");
     entries.Add(EntryFor(plan,mesh,prefab));written++;
    }
   }finally{EditorSceneManager.ClosePreviewScene(preview);}
   var library=AssetDatabase.LoadAssetAtPath<SpreadNativeStyle3DLibrary>(folder+"/Library.asset");
   if(library==null){library=ScriptableObject.CreateInstance<SpreadNativeStyle3DLibrary>();AssetDatabase.CreateAsset(library,folder+"/Library.asset");}
   library.SourceSha256=SpreadNativeStyle3DLibrary.ReviewedSourceSha256;library.SourceManifestJson=json;library.Entries=entries.ToArray();library.InvalidateCaches();library.Validate();
   EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
   string report=selective?"Docs/Verification/DensityCompletion/SpreadBiome/Art/Pools/native-selected-import.json":"Docs/Verification/DensityCompletion/SpreadBiome/Art/NativeStyle/native-import.json";Directory.CreateDirectory(Path.GetDirectoryName(report));
   File.WriteAllText(report,JsonUtility.ToJson(new Report{success=true,modelCount=entries.Count,sourceSha256=library.SourceSha256,geometryChanged=selective&&written>0,reusedModels=reused,writtenModels=written,selectedModelIds=selective?(string[])modelIds.Clone():null,copiedCurrentSourceGeometryExactly=true},true));
  }
  [Serializable] sealed class Report{public bool success,geometryChanged,copiedCurrentSourceGeometryExactly;public int modelCount,reusedModels,writtenModels;public string sourceSha256;public string[] selectedModelIds;}
  static SpreadNativeStyle3DLibrary.Entry EntryFor(Plan plan,Mesh mesh,GameObject prefab)
   =>new SpreadNativeStyle3DLibrary.Entry{Id=plan.Source.id,Kind=plan.Source.kind,SemanticColors=plan.Source.semanticColors,SourcePrefab=plan.Original,Prefab=prefab,Mesh=mesh,Material=plan.Materials[0],Materials=plan.Materials,SourceMeshes=plan.SourceMeshes,SourceTransforms=plan.SourceTransforms};
  /// <summary>Read-only editor content verification for interrupted-import pairs.
  /// The writer supplies only exact preflighted owned output paths. No asset is
  /// saved here; native tests may pass owned clones to exercise each mismatch.</summary>
  public static bool CanReusePreparedOutput(string modelId,Mesh mesh,GameObject prefab)
  {
   if(mesh==null||prefab==null)return false;
   string json=File.ReadAllText(SourcePath);if(SpreadNativeStyle3DLibrary.Hash(json)!=SpreadNativeStyle3DLibrary.ReviewedSourceSha256)throw new InvalidOperationException("Reviewed source differs.");
   var source=JsonUtility.FromJson<SpreadNativeStyleSource>(json);source.Validate();var row=Array.Find(source.models,x=>x.id==modelId);if(row==null)return false;
   var snapshots=new Dictionary<Texture,Texture2D>();
   try{return PreparedMatches(Prepare(row,Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath),ReferenceGladeVoxelLibrary.Load(),Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath),snapshots),mesh,prefab);}
   finally{foreach(var texture in snapshots.Values)UnityEngine.Object.DestroyImmediate(texture);}
  }
  static bool PreparedMatches(Plan plan,Mesh mesh,GameObject prefab)
  {
   if(mesh==null||prefab==null||!mesh.isReadable||mesh.bindposeCount!=0||mesh.vertexCount!=plan.Vertices.Length||mesh.subMeshCount!=plan.Indices.Length
    ||mesh.indexFormat!=(plan.Vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16))return false;
   if(!Equal(mesh.vertices,plan.Vertices)||!Equal(mesh.normals,plan.Normals)||!Equal(mesh.uv,plan.UV))return false;
   var expectedBounds=new Bounds(plan.Vertices[0],Vector3.zero);for(int i=1;i<plan.Vertices.Length;i++)expectedBounds.Encapsulate(plan.Vertices[i]);var actualBounds=mesh.bounds;
   if(!Finite(actualBounds.center)||!Finite(actualBounds.size)||(actualBounds.center-expectedBounds.center).sqrMagnitude>1e-10f||(actualBounds.size-expectedBounds.size).sqrMagnitude>1e-10f)return false;
   for(int sub=0;sub<plan.Indices.Length;sub++)if(mesh.GetTopology(sub)!=MeshTopology.Triangles||!Equal(mesh.GetIndices(sub),plan.Indices[sub]))return false;
   var renderer=prefab.GetComponent<MeshRenderer>();
   if(!prefab.activeSelf||prefab.transform.childCount!=0||prefab.transform.localPosition!=Vector3.zero||prefab.transform.localRotation!=plan.Original.transform.localRotation||prefab.transform.localScale!=plan.Original.transform.localScale
    ||prefab.GetComponent<MeshFilter>()?.sharedMesh!=mesh||renderer==null||!renderer.enabled||renderer.forceRenderingOff
    ||renderer.shadowCastingMode!=ShadowCastingMode.On||!renderer.receiveShadows||prefab.GetComponents<Component>().Length!=3||prefab.GetComponents<Collider>().Length!=0||prefab.GetComponents<MonoBehaviour>().Length!=0||prefab.GetComponent<Animator>()!=null||!Equal(renderer.sharedMaterials,plan.Materials))return false;
   return true;
  }
  static bool Equal<T>(T[] actual,T[] expected)
  {if(actual==null||actual.Length!=expected.Length)return false;var compare=EqualityComparer<T>.Default;for(int i=0;i<actual.Length;i++)if(!compare.Equals(actual[i],expected[i]))return false;return true;}
  static Plan Prepare(SpreadNativeStyleSource.Model row,SpawnRing3DLibrary ring,ReferenceGladeVoxelLibrary glade,VoxelWorldMeshCatalog voxel,Dictionary<Texture,Texture2D> snapshots)
  {
   var spec=ring.Definition.FindModel(row.id);var original=ring.FindModel(row.id);
   if(spec==null||original==null||spec.rigged||spec.kind!=row.kind||spec.kind=="actor"||original.GetComponentsInChildren<Animator>(true).Length!=0
     ||original.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||original.GetComponentsInChildren<Collider>(true).Length!=0)
    throw new InvalidOperationException("Unavailable inert exact native source: "+row.id);
   if(original.transform.localPosition!=Vector3.zero||!Finite(original.transform.localScale)||original.transform.localScale.x==0||original.transform.localScale.y==0||original.transform.localScale.z==0)
    throw new InvalidOperationException("Unsupported native root transform: "+row.id);
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<List<int>>();var materialsBySlot=new List<Material>();long indexCount=0;
   var sources=new List<Mesh>();var matrices=new List<Matrix4x4>();
   foreach(var renderer in original.GetComponentsInChildren<Renderer>(true))
   {
    if(!(renderer is MeshRenderer))throw new InvalidOperationException("Static source has a skin or nonmesh renderer: "+row.id);
    if(renderer.forceRenderingOff)throw new InvalidOperationException("Forced-off source renderer is not approved static art.");
    if(!renderer.enabled||!SpreadNativeStyle3DLibrary.ActiveUnder(renderer.transform,original.transform))continue;
    var mesh=voxel.Resolve(renderer.GetComponent<MeshFilter>()?.sharedMesh);var materials=renderer.sharedMaterials;
    if(mesh==null||!mesh.isReadable||mesh.vertexCount==0||mesh.bindposeCount!=0||mesh.subMeshCount!=materials.Length||materials.Length==0)
     throw new InvalidOperationException("Unreadable or ambiguous actual native geometry: "+row.id);
    if((long)vertices.Count+mesh.vertexCount>SpreadNativeStyle3DLibrary.MaximumVerticesPerModel)throw new InvalidOperationException("Static model vertex budget exceeded.");
    var v=mesh.vertices;var n=mesh.normals;var u=mesh.uv;
    if(n.Length!=v.Length||u.Length!=v.Length)throw new InvalidOperationException("Incomplete actual native mesh arrays.");
    var relative=original.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;var normalMatrix=relative.inverse.transpose;
    for(int k=0;k<16;k++)if(!SpreadNativeStyle3DLibrary.Finite(relative[k])||!SpreadNativeStyle3DLibrary.Finite(normalMatrix[k]))throw new InvalidOperationException("Invalid native source transform.");
    if(Mathf.Abs(relative.determinant)<1e-8f)throw new InvalidOperationException("Singular source transform.");
    int offset=vertices.Count;var assigned=new bool[v.Length];var colors=new Color[v.Length];
    for(int sub=0;sub<materials.Length;sub++)
    {
     var material=materials[sub];if(material!=ring.WorldMaterial&&material!=ring.WaterMaterial)throw new InvalidOperationException("Unreviewed source material: "+row.id);
     var outputMaterial=row.semanticColors?material:glade.Material;int slot=materialsBySlot.IndexOf(outputMaterial);if(slot<0){slot=materialsBySlot.Count;materialsBySlot.Add(outputMaterial);indices.Add(new List<int>());}var outputIndices=indices[slot];
     if(mesh.GetTopology(sub)!=MeshTopology.Triangles||mesh.GetIndexCount(sub)==0)throw new InvalidOperationException("Source is not a nonempty triangle mesh.");
     var tris=mesh.GetIndices(sub);if(tris.Length%3!=0)throw new InvalidOperationException("Invalid triangle index count.");if(indexCount+tris.Length>SpreadNativeStyle3DLibrary.MaximumIndicesPerModel)throw new InvalidOperationException("Static index budget exceeded.");
     indexCount+=tris.Length;foreach(int at in tris){if(at<0||at>=v.Length)throw new InvalidOperationException("Source index outside vertices.");
      if(!row.semanticColors){var color=Sample(material,u[at],snapshots);if(assigned[at]&&colors[at]!=color)throw new InvalidOperationException("Shared vertex has ambiguous material color.");colors[at]=color;assigned[at]=true;}}
     for(int i=0;i<tris.Length;i+=3){outputIndices.Add(offset+tris[i]);outputIndices.Add(offset+tris[i+(relative.determinant<0?2:1)]);outputIndices.Add(offset+tris[i+(relative.determinant<0?1:2)]);}
    }
    for(int i=0;i<v.Length;i++)
    {
     var point=relative.MultiplyPoint3x4(v[i]);var normal=normalMatrix.MultiplyVector(n[i]).normalized;
     if(!Finite(point)||!Finite(normal)||normal.sqrMagnitude<.9f||!SpreadNativeStyle3DLibrary.Finite(u[i].x)||!SpreadNativeStyle3DLibrary.Finite(u[i].y))throw new InvalidOperationException("Invalid native vertex/normal/UV.");
     vertices.Add(point);normals.Add(normal);uv.Add(row.semanticColors?u[i]:new Vector2((Nearest(assigned[i]?colors[i]:Color.black)+.5f)/24f,.5f));
    }
    sources.Add(mesh);matrices.Add(relative);
   }
   if(vertices.Count==0||indexCount==0||sources.Count>256)throw new InvalidOperationException("No bounded active native source.");
   return new Plan{Source=row,Original=original,Materials=materialsBySlot.ToArray(),Vertices=vertices.ToArray(),Normals=normals.ToArray(),UV=uv.ToArray(),Indices=indices.ConvertAll(x=>x.ToArray()).ToArray(),SourceMeshes=sources.ToArray(),SourceTransforms=matrices.ToArray()};
  }
  static Color Sample(Material material,Vector2 uv,Dictionary<Texture,Texture2D> snapshots)
  {
   if(!SpreadNativeStyle3DLibrary.Finite(uv.x)||!SpreadNativeStyle3DLibrary.Finite(uv.y))throw new InvalidOperationException("Invalid native palette UV.");
   var tint=material.GetColor("_BaseColor");if(!SpreadNativeStyle3DLibrary.Finite(tint.r)||!SpreadNativeStyle3DLibrary.Finite(tint.g)||!SpreadNativeStyle3DLibrary.Finite(tint.b)||!SpreadNativeStyle3DLibrary.Finite(tint.a))throw new InvalidOperationException("Invalid native material tint.");
   var borrowed=material.GetTexture("_BaseMap");if(borrowed==null)return tint;
   if(borrowed.wrapModeU!=TextureWrapMode.Clamp||borrowed.wrapModeV!=TextureWrapMode.Clamp)throw new InvalidOperationException("Unreviewed palette wrap policy.");
   if(!snapshots.TryGetValue(borrowed,out var texture)){
    string path=AssetDatabase.GetAssetPath(borrowed);if(!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Palette file is not an explicit PNG source.");
    texture=new Texture2D(2,2,TextureFormat.RGBA32,false,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
    try{if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false))throw new InvalidOperationException("Could not decode borrowed palette bytes.");snapshots.Add(borrowed,texture);}catch{UnityEngine.Object.DestroyImmediate(texture);throw;}
   }
   Vector2 scale=material.GetTextureScale("_BaseMap"),offset=material.GetTextureOffset("_BaseMap");uv=Vector2.Scale(uv,scale)+offset;
   if(!SpreadNativeStyle3DLibrary.Finite(uv.x)||!SpreadNativeStyle3DLibrary.Finite(uv.y))throw new InvalidOperationException("Invalid native palette transform.");
   return texture.GetPixelBilinear(Mathf.Clamp01(uv.x),Mathf.Clamp01(uv.y))*tint;
  }
  static readonly Color[] Approved=Colors();
  static Color[] Colors(){var colors=new Color[24];for(int i=0;i<colors.Length;i++)if(!ColorUtility.TryParseHtmlString(SpreadEnvironmentSource.ApprovedPalette[i],out colors[i]))throw new InvalidOperationException("Invalid approved color.");return colors;}
  static int Nearest(Color source){int best=0;float distance=float.PositiveInfinity;for(int i=0;i<Approved.Length;i++){var d=source-Approved[i];float q=2*d.r*d.r+4*d.g*d.g+3*d.b*d.b;if(q<distance){distance=q;best=i;}}return best;}
  static bool Finite(Vector3 v)=>SpreadNativeStyle3DLibrary.Finite(v.x)&&SpreadNativeStyle3DLibrary.Finite(v.y)&&SpreadNativeStyle3DLibrary.Finite(v.z);
  static void Preflight<T>(string path)where T:UnityEngine.Object
  {var value=AssetDatabase.LoadMainAssetAtPath(path);if(value!=null&&!(value is T))throw new InvalidOperationException("Foreign output type: "+path);if(value==null&&(File.Exists(path)||File.Exists(path+".meta")))throw new InvalidOperationException("Unimported existing output: "+path);}
 }
}
#endif
