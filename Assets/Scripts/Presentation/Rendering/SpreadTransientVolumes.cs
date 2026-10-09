using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
 /// <summary>Owns ephemeral sparse voxel geometry beneath one borrowed native
 /// surface. Reads current sources only; no world/state/timer/physics writes.
 /// Geometry is shared within this owner and reused across identical density
 /// bands. Resource limits retain fallback instead of claiming absent output.</summary>
 public sealed class SpreadTransientVolumes:IDisposable
 {
  public const int MaximumGasViews=512,MaximumElementViews=512;
  readonly NativeZone3DRenderSurface surface;readonly Material material;readonly Func<int,int,bool> representedWater;
  readonly List<Material> materialScratch=new List<Material>(2);
  readonly Dictionary<Entity,View> gases=new Dictionary<Entity,View>();
  readonly Dictionary<int,View> elements=new Dictionary<int,View>();
  readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
  readonly HashSet<Entity> seenGas=new HashSet<Entity>();readonly HashSet<int> seenElements=new HashSet<int>();
  readonly List<Entity> removedGas=new List<Entity>();readonly List<int> removedElements=new List<int>(),tileKeys=new List<int>();
  readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
  Zone zone;bool disposed;
  sealed class View{public GameObject Root;public MeshFilter Filter;public MeshRenderer Renderer;public SpreadTransientSample Sample;public string Shape;}
  public SpreadTransientVolumes(NativeZone3DRenderSurface surface,Material borrowedMaterial,Func<int,int,bool> representedWater=null)
  {
   this.representedWater=representedWater;this.surface=surface??throw new ArgumentNullException(nameof(surface));material=borrowedMaterial??throw new ArgumentNullException(nameof(borrowedMaterial));
   if(surface.ContentRoot==null)throw new ArgumentException("A live owned native surface is required.",nameof(surface));surface.MaterialFor(material);
  }
  public void Refresh(Zone current)
  {
   if(disposed)return;
   if(!ReferenceEquals(zone,current)){Clear();zone=current;}
   if(!SpreadPresentationScope.IsActive(zone)||surface.ContentRoot==null){Clear();return;}
   // Retired sources must release capacity before this dirty refresh admits
   // their replacements. This reads live source state; it never advances it.
   removedGas.Clear();foreach(var p in gases)if(!SpreadTransientSource.TryGas(zone,p.Key,out _))removedGas.Add(p.Key);
   foreach(var owner in removedGas){DestroyView(gases[owner]);gases.Remove(owner);}
   removedElements.Clear();foreach(var p in elements)if(!TryTileSample(p.Key%Zone.Width,p.Key/Zone.Width,out _))removedElements.Add(p.Key);
   foreach(int key in removedElements){DestroyView(elements[key]);elements.Remove(key);}
   seenGas.Clear();seenElements.Clear();
   foreach(var owner in zone.GetReadOnlyEntities())
   {
    if(!SpreadTransientSource.TryGas(zone,owner,out var sample))continue;
    if(!gases.TryGetValue(owner,out var view)){if(gases.Count>=MaximumGasViews)continue;view=Create(sample);gases.Add(owner,view);}
    Update(view,sample);seenGas.Add(owner);
   }
   removedGas.Clear();foreach(var p in gases)if(!seenGas.Contains(p.Key))removedGas.Add(p.Key);
   foreach(var owner in removedGas){DestroyView(gases[owner]);gases.Remove(owner);}
   tileKeys.Clear();zone.TileState.CollectWrittenKeys(tileKeys);
   foreach(int key in tileKeys)
   {
    int x=key%Zone.Width,y=key/Zone.Width;
    if(!TryTileSample(x,y,out var sample))continue;
    if(!elements.TryGetValue(key,out var view)){if(elements.Count>=MaximumElementViews)continue;view=Create(sample);elements.Add(key,view);}
    Update(view,sample);seenElements.Add(key);
   }
   removedElements.Clear();foreach(var p in elements)if(!seenElements.Contains(p.Key))removedElements.Add(p.Key);
   foreach(int key in removedElements){DestroyView(elements[key]);elements.Remove(key);}
  }
  View Create(SpreadTransientSample sample)
  {
   var root=new GameObject("Native Spread transient "+sample.Kind){hideFlags=HideFlags.DontSave};
   try
   {
    root.transform.SetParent(surface.ContentRoot,false);var filter=root.AddComponent<MeshFilter>();var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
    surface.PrepareModel(root,true);renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    return new View{Root=root,Filter=filter,Renderer=renderer};
   }
   catch{root.SetActive(false);Destroy(root);throw;}
  }
  void Update(View view,SpreadTransientSample sample)
  {
   if(view.Root==null||view.Filter==null||view.Renderer==null)throw new InvalidOperationException("Owned transient hierarchy was removed.");
   string shape=Shape(sample);
   if(!meshes.TryGetValue(shape,out var mesh)||mesh==null){mesh=BuildMesh(sample);meshes[shape]=mesh;}
   view.Filter.sharedMesh=mesh;view.Shape=shape;view.Sample=sample;
   if(view.Root.transform.parent!=surface.ContentRoot)view.Root.transform.SetParent(surface.ContentRoot,false);
   view.Root.layer=NativeZone3DRenderSurface.WorldLayer;view.Root.transform.position=Village3DProjection.CellCentre(sample.X,sample.Y);view.Root.transform.rotation=Quaternion.identity;view.Root.transform.localScale=Vector3.one;
   view.Renderer.GetSharedMaterials(materialScratch);if(materialScratch.Count!=1||materialScratch[0]!=surface.MaterialFor(material))view.Renderer.sharedMaterials=new[]{surface.MaterialFor(material)};materialScratch.Clear();
   view.Renderer.SetPropertyBlock(null,0);
   view.Renderer.GetPropertyBlock(properties);properties.SetTexture("_BaseMap",Texture2D.whiteTexture);properties.SetColor("_BaseColor",Tint(sample));properties.SetFloat("_Transient",1);
   view.Renderer.SetPropertyBlock(properties);properties.Clear();view.Renderer.enabled=true;view.Renderer.forceRenderingOff=false;view.Root.SetActive(true);
  }
  public bool TryGetGas(Entity owner,out GameObject root,out SpreadTransientSample sample)
  {
   root=null;sample=default;
   if(disposed||!SpreadTransientSource.TryGas(zone,owner,out sample)||!gases.TryGetValue(owner,out var view)||!Valid(view,sample))return false;
   root=view.Root;return true;
  }
  public bool TryGetElement(int x,int y,out GameObject root,out SpreadTransientSample sample)
  {
   root=null;sample=default;
   if(disposed||!TryTileSample(x,y,out sample)||!elements.TryGetValue(y*Zone.Width+x,out var view)||!Valid(view,sample))return false;
   root=view.Root;return true;
  }
  bool TryTileSample(int x,int y,out SpreadTransientSample sample)
  {bool water=representedWater?.Invoke(x,y)==true;return SpreadTransientSource.TrySurfaceMark(zone,x,y,out sample,water)||SpreadTransientSource.TryElement(zone,x,y,out sample,water);}
  bool Valid(View view,SpreadTransientSample sample)
  {
   if(!surface.IsVisible||surface.ContentRoot==null||view.Root==null||!view.Root.activeInHierarchy||view.Root.transform.parent!=surface.ContentRoot
      ||view.Root.layer!=NativeZone3DRenderSurface.WorldLayer||view.Filter==null||view.Renderer==null||!view.Renderer.enabled||view.Renderer.forceRenderingOff||view.Renderer.sharedMaterial==null
      ||view.Shape!=Shape(sample)||view.Sample.Color!=sample.Color||view.Sample.X!=sample.X||view.Sample.Y!=sample.Y
      ||view.Root.transform.position!=Village3DProjection.CellCentre(sample.X,sample.Y)||view.Root.transform.localScale!=Vector3.one||view.Root.transform.rotation!=Quaternion.identity
      ||!meshes.TryGetValue(view.Shape,out var mesh)||mesh==null||view.Filter.sharedMesh!=mesh||mesh.vertexCount==0
      ||view.Renderer.sharedMaterial!=surface.MaterialFor(material))return false;
   view.Renderer.GetSharedMaterials(materialScratch);bool oneMaterial=materialScratch.Count==1;materialScratch.Clear();if(!oneMaterial)return false;
   view.Renderer.GetPropertyBlock(properties);bool correct=properties.GetFloat("_Transient")==1&&properties.GetTexture("_BaseMap")==Texture2D.whiteTexture&&properties.GetColor("_BaseColor")==Tint(sample);properties.Clear();
   // Any indexed override replaces the renderer block, so refuse it unless the
   // exact same required properties remain present. Never manufacture one.
   view.Renderer.GetPropertyBlock(properties,0);
   if(!properties.isEmpty)correct &= properties.GetFloat("_Transient")==1&&properties.GetTexture("_BaseMap")==Texture2D.whiteTexture&&properties.GetColor("_BaseColor")==Tint(sample);
   properties.Clear();return correct;
  }
  static string Shape(SpreadTransientSample sample)=>sample.Kind.StartsWith("coating:",StringComparison.Ordinal)?"coating":sample.Kind.StartsWith("residue:",StringComparison.Ordinal)?"residue":(sample.Owner!=null?"gas":sample.Kind)+"-"+sample.Band;
  static Color Tint(SpreadTransientSample sample)
  {
   if(sample.Owner!=null)return QudColorParser.Parse(sample.Color).linear;
   if(sample.Kind=="residue:embers")return new Color(1,.45f,.1f).linear;
   if(sample.Kind=="residue:petals")return new Color(.6f,.6f,.6f).linear;
   if(sample.Kind=="residue:grit")return new Color(.82f,.85f,.73f).linear;
   if(sample.Kind=="veil-mist")return new Color(.65f,.9f,1).linear;
   if(sample.Kind=="coating:oil")return new Color(.35f,.25f,.45f).linear;
   if(sample.Kind=="coating:ice")return new Color(.85f,.95f,1).linear;
   if(sample.Kind.StartsWith("coating:",StringComparison.Ordinal))return new Color(.3f,.55f,.95f).linear;
   // Exact existing native tile-mark RGB, without changing semantic simulation.
   switch(sample.Kind){case "charge":return new Color(1,.95f,.35f).linear;case "heat":return new Color(1,.35f,.15f).linear;case "cold":return new Color(.65f,.9f,1).linear;default:return new Color(.8f,.8f,.85f).linear;}
  }
  static Mesh BuildMesh(SpreadTransientSample sample)
  {
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
   bool cloud=sample.Owner!=null||sample.Kind=="steam"||sample.Kind=="smoke"||sample.Kind=="veil-mist";
   bool groundMark=sample.Kind.StartsWith("coating:",StringComparison.Ordinal)||sample.Kind.StartsWith("residue:",StringComparison.Ordinal);
   int count=groundMark?8:cloud?sample.Band==1?4:sample.Band==2?9:16:4+sample.Band*2;
   for(int i=0;i<count;i++)
   {
    int ix=(i*7)%4,iz=(i*5+i/4)%4;
    float x=(ix-1.5f)*.19f,z=(iz-1.5f)*.19f;
    float y=groundMark?.035f+(i%2)*.015f:cloud?.14f+(i%5)*.13f:sample.Kind=="heat"?.09f+(i%3)*.09f:.045f+(i%2)*.04f;
    float size=groundMark?.085f:cloud?.09f+sample.Band*.012f:.055f;
    AddCube(new Vector3(x,y,z),size,vertices,normals,uv,triangles);
   }
   var mesh=new Mesh{name="Owned Spread transient "+Shape(sample),hideFlags=HideFlags.DontSave};
   try{mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
   catch{Destroy(mesh);throw;}
  }
  static void AddCube(Vector3 c,float size,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<int> t)
  {
   float h=size*.5f;
   Face(c+Vector3.right*h,Vector3.back*h,Vector3.up*h,Vector3.right,v,n,uv,t);
   Face(c+Vector3.left*h,Vector3.forward*h,Vector3.up*h,Vector3.left,v,n,uv,t);
   Face(c+Vector3.up*h,Vector3.right*h,Vector3.back*h,Vector3.up,v,n,uv,t);
   Face(c+Vector3.down*h,Vector3.right*h,Vector3.forward*h,Vector3.down,v,n,uv,t);
   Face(c+Vector3.forward*h,Vector3.right*h,Vector3.up*h,Vector3.forward,v,n,uv,t);
   Face(c+Vector3.back*h,Vector3.left*h,Vector3.up*h,Vector3.back,v,n,uv,t);
  }
  static void Face(Vector3 c,Vector3 r,Vector3 u,Vector3 normal,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<int> t)
  {int start=v.Count;v.Add(c-r-u);v.Add(c+r-u);v.Add(c+r+u);v.Add(c-r+u);for(int i=0;i<4;i++){n.Add(normal);uv.Add(new Vector2(.5f,.5f));}t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start);t.Add(start+2);t.Add(start+3);}
  static void DestroyView(View view){if(view.Root==null)return;view.Root.SetActive(false);Destroy(view.Root);}
  void Clear(){foreach(var view in gases.Values)DestroyView(view);foreach(var view in elements.Values)DestroyView(view);gases.Clear();elements.Clear();seenGas.Clear();seenElements.Clear();}
  static void Destroy(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
  public void Dispose(){if(disposed)return;disposed=true;Clear();foreach(var mesh in meshes.Values)Destroy(mesh);meshes.Clear();properties.Clear();zone=null;}
 }
}
