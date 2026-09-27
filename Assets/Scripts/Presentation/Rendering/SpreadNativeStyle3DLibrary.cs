using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Rendering
{
 /// <summary>Persistent scoped palette copies, not new gameplay or geometry.
 /// Original model IDs, source transforms and native state selection survive.</summary>
 public sealed class SpreadNativeStyle3DLibrary:ScriptableObject
 {
  public const string ResourcePath="SpreadNativeStyle3D/Library",Folder="Assets/Resources/SpreadNativeStyle3D";
  public const int MaximumVerticesPerModel=1000000,MaximumIndicesPerModel=6000000;
  public const string ReviewedSourceSha256="449309b8275175530a35d1ee3affe6daef0f5aad0eb7fb57122c6753e9abf0c9";
  public string SourceSha256,SourceManifestJson;public Entry[] Entries;
  [Serializable] public sealed class Entry
  {
   public string Id,Kind;public bool SemanticColors;
   public GameObject SourcePrefab,Prefab;public Mesh Mesh;public Material Material;public Material[] Materials;
   public Mesh[] SourceMeshes;public Matrix4x4[] SourceTransforms;
  }
  private Dictionary<string,Entry> index;private HashSet<Mesh> meshes;
  public static SpreadNativeStyle3DLibrary Load()=>Resources.Load<SpreadNativeStyle3DLibrary>(ResourcePath);
  public void InvalidateCaches(){index=null;meshes=null;}
  void OnValidate()=>InvalidateCaches();
  internal void EnsureReady(){if(index==null)Validate();}
  public Entry Find(string id){if(id==null)return null;EnsureReady();return index.TryGetValue(id,out var e)?e:null;}
  public bool ContainsMesh(Mesh mesh){if(mesh==null)return false;EnsureReady();return meshes.Contains(mesh);}
  /// <summary>Does not repair failed recipes, allocate geometry or change state.</summary>
  public Entry ForOwner(Zone zone,SpawnRing3DRecipe recipe)
  {
   var owner=recipe.Owner;
   if(!SpreadPresentationScope.IsActive(zone)||owner==null||recipe.Failure!=null||recipe.ModelId==null||recipe.Transient
     ||owner.HasTag("Creature")||owner.HasTag("Player")||owner.HasTag("Item")||owner.HasPart<BrainPart>())return null;
   var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
   if(cell==null||!cell.Objects.Contains(owner)||render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)
     ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null)return null;
   return Find(recipe.ModelId);
  }
  public void Validate()
  {
   InvalidateCaches();var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);var glade=ReferenceGladeVoxelLibrary.Load();
   var voxel=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
   if(SourceSha256!=ReviewedSourceSha256||Entries==null||Entries.Length!=SpreadNativeStyleSource.ModelCount||ring==null||glade==null||voxel==null)
    throw new InvalidOperationException("Incomplete exact native style library.");
   if(SourceManifestJson==null||Hash(SourceManifestJson)!=ReviewedSourceSha256)throw new InvalidOperationException("Source manifest checksum differs.");
   var reviewed=JsonUtility.FromJson<SpreadNativeStyleSource>(SourceManifestJson);reviewed.Validate();
   var required=new Dictionary<string,SpreadNativeStyleSource.Model>(StringComparer.Ordinal);foreach(var row in reviewed.models)required.Add(row.id,row);
   var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var outputs=new HashSet<Mesh>();var prefabs=new HashSet<GameObject>();
   foreach(var e in Entries)
   {
    if(e==null||!SpreadNativeStyleSource.SafeId(e.Id)||next.ContainsKey(e.Id)||e.SourcePrefab==null||e.Prefab==null||e.SourcePrefab==e.Prefab
      ||e.Mesh==null||!outputs.Add(e.Mesh)||!prefabs.Add(e.Prefab)||e.Material==null||e.Materials==null||e.Materials.Length<1||e.Materials.Length>2||e.Material!=e.Materials[0]||e.SourceMeshes==null||e.SourceTransforms==null
      ||e.SourceMeshes.Length==0||e.SourceMeshes.Length!=e.SourceTransforms.Length||e.SourceMeshes.Length>256)
     throw new InvalidOperationException("Invalid native style identity.");
    if(!required.TryGetValue(e.Id,out var row)||row.semanticColors!=e.SemanticColors||row.kind!=e.Kind)
     throw new InvalidOperationException("Entry does not match reviewed source policy.");
    var sourceSpec=ring.Definition.FindModel(e.Id);
    if(sourceSpec==null||sourceSpec.rigged||sourceSpec.kind=="actor"||e.Kind!=sourceSpec.kind||ring.FindModel(e.Id)!=e.SourcePrefab)
     throw new InvalidOperationException("Current native source differs: "+e.Id);
    var root=e.Prefab;var mesh=e.Mesh;var renderer=root.GetComponent<MeshRenderer>();
    if(!mesh.isReadable||mesh.vertexCount<=0||mesh.vertexCount>MaximumVerticesPerModel||mesh.subMeshCount!=e.Materials.Length
      ||mesh.GetIndexCount(0)==0||mesh.GetIndexCount(0)>MaximumIndicesPerModel||mesh.bindposeCount!=0
      ||root.GetComponent<MeshFilter>()?.sharedMesh!=mesh||renderer==null||renderer.sharedMaterials.Length!=e.Materials.Length
      ||!renderer.enabled||renderer.forceRenderingOff||!root.activeSelf||root.transform.childCount!=0||root.GetComponents<Component>().Length!=3||root.GetComponents<Collider>().Length!=0
      ||root.GetComponents<MonoBehaviour>().Length!=0||root.GetComponent<Animator>()!=null||root.transform.localPosition!=Vector3.zero
      ||root.transform.localRotation!=e.SourcePrefab.transform.localRotation||root.transform.localScale!=e.SourcePrefab.transform.localScale)
     throw new InvalidOperationException("Invalid scoped static geometry: "+e.Id);
    for(int sub=0;sub<e.Materials.Length;sub++)if(e.Materials[sub]==null||renderer.sharedMaterials[sub]!=e.Materials[sub]||mesh.GetTopology(sub)!=MeshTopology.Triangles||mesh.GetIndexCount(sub)==0)throw new InvalidOperationException("Invalid exact static material slot.");
    if(!e.SemanticColors&&(e.Materials.Length!=1||e.Material!=glade.Material))throw new InvalidOperationException("Unapproved static palette.");
    int n=0,vertices=0;long indices=0;var sourceMaterials=new List<Material>();
    foreach(var original in e.SourcePrefab.GetComponentsInChildren<Renderer>(true))
    {
     if(!(original is MeshRenderer))throw new InvalidOperationException("Static source contains nonstatic renderer.");
     if(!original.enabled||!ActiveUnder(original.transform,e.SourcePrefab.transform))continue;
     var source=voxel.Resolve(original.GetComponent<MeshFilter>()?.sharedMesh);
     if(n>=e.SourceMeshes.Length||source==null||source==mesh||!source.isReadable||e.SourceMeshes[n]!=source)
      throw new InvalidOperationException("Actual native geometry provenance differs.");
     var matrix=e.SourcePrefab.transform.worldToLocalMatrix*original.transform.localToWorldMatrix;
     for(int k=0;k<16;k++)if(!Finite(matrix[k])||Mathf.Abs(matrix[k]-e.SourceTransforms[n][k])>1e-6f)
      throw new InvalidOperationException("Actual native transform provenance differs.");
     if(original.sharedMaterials.Length!=source.subMeshCount)throw new InvalidOperationException("Ambiguous native material slots.");
     foreach(var material in original.sharedMaterials)
     {
      if(material!=ring.WorldMaterial&&material!=ring.WaterMaterial)throw new InvalidOperationException("Unregistered native semantic material.");
      var outputMaterial=e.SemanticColors?material:glade.Material;if(!sourceMaterials.Contains(outputMaterial))sourceMaterials.Add(outputMaterial);
     }
     vertices=checked(vertices+source.vertexCount);for(int sub=0;sub<source.subMeshCount;sub++)indices+=source.GetIndexCount(sub);n++;
    }
    long actualIndices=0;for(int sub=0;sub<mesh.subMeshCount;sub++)actualIndices+=mesh.GetIndexCount(sub);
    if(sourceMaterials.Count!=e.Materials.Length)throw new InvalidOperationException("Missing native material slot.");for(int sub=0;sub<sourceMaterials.Count;sub++)if(sourceMaterials[sub]!=e.Materials[sub])throw new InvalidOperationException("Native material order differs.");
    if(n!=e.SourceMeshes.Length||vertices!=mesh.vertexCount||indices!=actualIndices||actualIndices>MaximumIndicesPerModel)
     throw new InvalidOperationException("Static copy does not preserve full native source.");
    next.Add(e.Id,e);
   }
   index=next;meshes=outputs;
  }
  // Shared read-only importer/runtime preflight helpers; no asset mutation.
  public static bool ActiveUnder(Transform child,Transform root)
  {for(var at=child;at!=null;at=at.parent){if(!at.gameObject.activeSelf)return false;if(at==root)return true;}return false;}
  public static string Hash(string text){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
  public static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
 }
}
