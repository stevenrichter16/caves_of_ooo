using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
 /// <summary>One draw-frame adapter for the existing legacy FX renderer. It owns
 /// only finite native geometry; existing clocks, copied paths, draw ordering,
 /// RNG, expiry and blocking decisions stay in AsciiFxRenderer.</summary>
 public sealed class SpreadParticleFrame:IDisposable
 {
  public const int MaximumViews=512;
  readonly NativeZone3DRenderSurface surface;readonly Material material;
  readonly Dictionary<int,View> views=new Dictionary<int,View>();readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
  readonly HashSet<int> seen=new HashSet<int>();readonly List<int> remove=new List<int>();
  readonly List<int> priorKeys=new List<int>();int reuseCursor;readonly List<Material> materials=new List<Material>(2);
  readonly MaterialPropertyBlock block=new MaterialPropertyBlock();Zone zone;bool disposed,frame;
  sealed class View{public GameObject Root;public MeshFilter Filter;public MeshRenderer Renderer;public SpreadParticleSample Sample;}
  public SpreadParticleFrame(NativeZone3DRenderSurface surface,Material borrowedMaterial)
  {this.surface=surface??throw new ArgumentNullException(nameof(surface));material=surface.MaterialFor(borrowedMaterial);if(surface.ContentRoot==null)throw new ArgumentException("Native particle surface is unavailable.");}
  public void BeginFrame(Zone current)
  {if(disposed)return;if(!ReferenceEquals(zone,current)){Clear();zone=current;}seen.Clear();frame=surface.IsVisible&&surface.ContentRoot!=null&&SpreadPresentationScope.IsActive(zone);if(!frame){Clear();return;}
   priorKeys.Clear();reuseCursor=0;foreach(int key in views.Keys)priorKeys.Add(key);}
  /// <summary>Called for every resolved visible mark in original draw order.
  /// Returning true proves this exact mark now has a submitted native view.
  /// Refusals remove an earlier native mark at this cell, preserving last draw.</summary>
  public bool TryDraw(int x,int y,char glyph,string color)
  {
   if(disposed||zone==null||!zone.InBounds(x,y))return false;int key=y*Zone.Width+x;
   if(!frame||!SpreadParticleSource.TrySample(zone,x,y,glyph,color,out var sample)){Forget(key);return false;}
   if(!views.TryGetValue(key,out var view))
   {
    if(views.Count>=MaximumViews)
    {
     // Each prior key is considered at most once. A view already drawn this
     // frame is never recycled, even if its old key appears later in the list.
     view=null;
     while(reuseCursor<priorKeys.Count)
     {
      int priorKey=priorKeys[reuseCursor++];
      if(seen.Contains(priorKey)||!views.TryGetValue(priorKey,out var prior))continue;
      views.Remove(priorKey);view=prior;break;
     }
     if(view==null)return false;
    }
    else view=Create();
    views.Add(key,view);
   }
   if(!meshes.TryGetValue(sample.Shape,out var mesh)||mesh==null){mesh=Build(sample.Shape);meshes[sample.Shape]=mesh;}
   if(view.Root==null||view.Filter==null||view.Renderer==null){Forget(key);return false;}
   view.Sample=sample;view.Filter.sharedMesh=mesh;var t=view.Root.transform;t.SetParent(surface.ContentRoot,false);t.position=Village3DProjection.CellCentre(x,y);t.rotation=Quaternion.identity;t.localScale=Vector3.one;view.Root.layer=NativeZone3DRenderSurface.WorldLayer;
   view.Renderer.GetSharedMaterials(materials);if(materials.Count!=1||materials[0]!=material)view.Renderer.sharedMaterials=new[]{material};materials.Clear();
   view.Renderer.SetPropertyBlock(null,0);
   view.Renderer.GetPropertyBlock(block);block.SetTexture("_BaseMap",Texture2D.whiteTexture);block.SetColor("_BaseColor",QudColorParser.Parse(sample.Color).linear);block.SetFloat("_Transient",1);view.Renderer.SetPropertyBlock(block);block.Clear();
   view.Renderer.enabled=true;view.Renderer.forceRenderingOff=false;view.Root.SetActive(true);seen.Add(key);return Valid(view);
  }
  public void EndFrame(){if(disposed)return;remove.Clear();foreach(var p in views)if(!seen.Contains(p.Key))remove.Add(p.Key);foreach(int key in remove)Forget(key);frame=false;}
  public bool TryGet(int x,int y,out GameObject root)
  {root=null;if(disposed||zone==null||!zone.InBounds(x,y)||!views.TryGetValue(y*Zone.Width+x,out var view)||!Valid(view))return false;root=view.Root;return true;}
  bool Valid(View view)
  {
   var s=view.Sample;
   if(!surface.IsVisible||surface.ContentRoot==null||!SpreadParticleSource.TrySample(zone,s.X,s.Y,s.Glyph,s.Color,out _)
      ||view.Root==null||view.Filter==null||view.Renderer==null||!view.Root.activeInHierarchy||view.Root.layer!=NativeZone3DRenderSurface.WorldLayer
      ||view.Root.transform.parent!=surface.ContentRoot||view.Root.transform.position!=Village3DProjection.CellCentre(s.X,s.Y)||view.Root.transform.rotation!=Quaternion.identity||view.Root.transform.localScale!=Vector3.one
      ||!view.Renderer.enabled||view.Renderer.forceRenderingOff||!meshes.TryGetValue(s.Shape,out var mesh)||mesh==null||mesh.vertexCount==0||view.Filter.sharedMesh!=mesh)return false;
   view.Renderer.GetSharedMaterials(materials);bool result=materials.Count==1&&materials[0]==material;materials.Clear();if(!result)return false;
   view.Renderer.GetPropertyBlock(block);result=Matches(s);block.Clear();view.Renderer.GetPropertyBlock(block,0);if(!block.isEmpty)result &= Matches(s);block.Clear();return result;
  }
  bool Matches(SpreadParticleSample s)=>block.GetFloat("_Transient")==1&&block.GetTexture("_BaseMap")==Texture2D.whiteTexture&&block.GetColor("_BaseColor")==QudColorParser.Parse(s.Color).linear;
  View Create()
  {
   var root=new GameObject("Native Spread decorative FX"){hideFlags=HideFlags.DontSave};
   try{root.transform.SetParent(surface.ContentRoot,false);var f=root.AddComponent<MeshFilter>();var r=root.AddComponent<MeshRenderer>();r.sharedMaterial=material;surface.PrepareModel(root,true);r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return new View{Root=root,Filter=f,Renderer=r};}
   catch{root.SetActive(false);Destroy(root);throw;}
  }
  static Mesh Build(string shape)
  {
   var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
   if(shape=="spark"){Box(new Vector3(0,.64f,0),new Vector3(.15f,.15f,.15f),Quaternion.identity,v,n,uv,t);Box(new Vector3(-.13f,.76f,.07f),Vector3.one*.065f,Quaternion.identity,v,n,uv,t);Box(new Vector3(.13f,.53f,-.07f),Vector3.one*.065f,Quaternion.identity,v,n,uv,t);}
   else if(shape=="alert"){Box(new Vector3(0,.64f,.075f),new Vector3(.3f,.07f,.075f),Quaternion.Euler(0,90,0),v,n,uv,t);Box(new Vector3(0,.64f,-.2f),Vector3.one*.075f,Quaternion.identity,v,n,uv,t);}
   else if(shape=="sleep"){Box(new Vector3(0,.64f,.17f),new Vector3(.38f,.07f,.07f),Quaternion.identity,v,n,uv,t);Box(new Vector3(0,.64f,0),new Vector3(.5f,.07f,.07f),Quaternion.Euler(0,SpreadParticleSource.YawDegrees("sleep"),0),v,n,uv,t);Box(new Vector3(0,.64f,-.17f),new Vector3(.38f,.07f,.07f),Quaternion.identity,v,n,uv,t);}
   else if(shape=="down-chevron"){Box(new Vector3(-.08f,.64f,-.02f),new Vector3(.34f,.07f,.07f),Quaternion.Euler(0,60.255f,0),v,n,uv,t);Box(new Vector3(.08f,.64f,-.02f),new Vector3(.34f,.07f,.07f),Quaternion.Euler(0,-60.255f,0),v,n,uv,t);}
   else{float yaw=SpreadParticleSource.YawDegrees(shape);Box(new Vector3(0,.62f,0),new Vector3(.9f,.065f,.065f),Quaternion.Euler(0,yaw,0),v,n,uv,t);}
   var mesh=new Mesh{name="Owned Spread FX "+shape,hideFlags=HideFlags.DontSave};try{mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();return mesh;}catch{Destroy(mesh);throw;}
  }
  static void Box(Vector3 c,Vector3 size,Quaternion q,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<int> t)
  {var x=q*Vector3.right*size.x*.5f;var y=q*Vector3.up*size.y*.5f;var z=q*Vector3.forward*size.z*.5f;Face(c+x,-z,y,q*Vector3.right,v,n,uv,t);Face(c-x,z,y,q*Vector3.left,v,n,uv,t);Face(c+y,x,-z,q*Vector3.up,v,n,uv,t);Face(c-y,x,z,q*Vector3.down,v,n,uv,t);Face(c+z,x,y,q*Vector3.forward,v,n,uv,t);Face(c-z,-x,y,q*Vector3.back,v,n,uv,t);}
  static void Face(Vector3 c,Vector3 r,Vector3 u,Vector3 normal,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<int> t)
  {int start=v.Count;v.Add(c-r-u);v.Add(c+r-u);v.Add(c+r+u);v.Add(c-r+u);for(int i=0;i<4;i++){n.Add(normal);uv.Add(new Vector2(.5f,.5f));}t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start);t.Add(start+2);t.Add(start+3);}
  void Forget(int key){if(!views.TryGetValue(key,out var view))return;if(view.Root!=null){view.Root.SetActive(false);Destroy(view.Root);}views.Remove(key);seen.Remove(key);}
  public void Clear(){foreach(var v in views.Values)if(v.Root!=null){v.Root.SetActive(false);Destroy(v.Root);}views.Clear();seen.Clear();priorKeys.Clear();reuseCursor=0;frame=false;}
  static void Destroy(Object o){if(o==null)return;if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
  public void Dispose(){if(disposed)return;Clear();foreach(var m in meshes.Values)Destroy(m);meshes.Clear();block.Clear();zone=null;disposed=true;}
 }
}
