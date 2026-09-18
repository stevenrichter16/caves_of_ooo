using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Pure EditMode contract of the shared resource owner. Reflection keeps the
    // tests compilable for an honest missing-helper RED before production exists.
    public sealed class NativeZone3DRenderSurfaceTests
    {
        private const int OrdinaryWorldLayer = 8;
        private sealed class Surface : IDisposable
        {
            readonly object value;
            readonly Type type;
            public Surface(Transform parent, UniversalRendererData renderer, int rendererIndex,
                Material composite, Material[] families, float exposure=2.2f)
            {
                type=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.NativeZone3DRenderSurface");
                Assert.NotNull(type,"Add the shared render-surface helper after recording these missing-type REDs.");
                var ctor=type.GetConstructor(new[] { typeof(Transform),typeof(UniversalRendererData),typeof(int),
                    typeof(Material),typeof(Material[]),typeof(float) });
                Assert.NotNull(ctor,"The agreed constructor is the minimal explicit-resource contract.");
                try { value=ctor.Invoke(new object[] {parent,renderer,rendererIndex,composite,families,exposure}); }
                catch(TargetInvocationException e) { throw e.InnerException ?? e; }
            }
            T Get<T>(string name)
            {
                var property=type.GetProperty(name,BindingFlags.Instance|BindingFlags.Public);
                Assert.NotNull(property,"Required surface diagnostic: "+name); return (T)property.GetValue(value);
            }
            object Call(string name,params object[] args)
            {
                var method=type.GetMethod(name,BindingFlags.Instance|BindingFlags.Public);
                Assert.NotNull(method,"Required surface operation: "+name);
                try { return method.Invoke(value,args); }
                catch(TargetInvocationException e) { throw e.InnerException ?? e; }
            }
            public Transform Content=>Get<Transform>("ContentRoot");
            public Camera Camera=>Get<Camera>("WorldCamera");
            public Texture2D Fog=>Get<Texture2D>("FogTexture");
            public Renderer Composite=>Get<Renderer>("CompositeRenderer");
            public Light Sun=>Get<Light>("Sun");
            public bool Visible=>Get<bool>("IsVisible");
            public Material MaterialFor(Material source)=>(Material)Call("MaterialFor",source);
            public void Prepare(GameObject root,bool transient=false)=>Call("PrepareModel",root,transient);
            public void FogFrom(Zone zone,LightMap light=null,bool reveal=false)=>Call("UpdateFog",zone,light,reveal);
            public void Sync(Camera source,bool visible=true,bool low=false)=>Call("Sync",source,visible,low);
            public void Dispose()=>Call("Dispose");
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly Camera Source;
            public readonly RenderTexture Borrowed;
            public readonly Village3DLibrary Library;
            readonly List<Surface> surfaces=new List<Surface>();
            readonly List<Object> extras=new List<Object>();
            readonly RenderPipelineAsset quality=QualitySettings.renderPipeline, defaults=GraphicsSettings.defaultRenderPipeline;
            public Fixture()
            {
                try
                {
                    Library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(Library);
                    Assert.NotNull(Library.WorldMaterial);Assert.NotNull(Library.WaterMaterial);Assert.NotNull(Library.CompositeMaterial);
                    var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.NotNull(pipeline);
                    Assert.That(Library.RendererIndex,Is.GreaterThanOrEqualTo(0).And.LessThan(pipeline.rendererDataList.Length));
                    Assert.AreSame(Library.Renderer,pipeline.rendererDataList[Library.RendererIndex]);
                    Root=new GameObject("Owned shared-surface fixture");
                    Source=NewCamera("Borrowed ordinary source"); Source.orthographic=true;Source.orthographicSize=12.75f;
                    Source.transform.position=new Vector3(40,12.75f,-10);Source.enabled=false;Source.depth=7;
                    Borrowed=Own(new RenderTexture(960,640,16));Borrowed.Create();Source.targetTexture=Borrowed;
                    Source.rect=new Rect(.05f,.1f,.75f,.8f);Source.aspect=Source.pixelRect.width/Source.pixelRect.height;
                    Source.cullingMask=(1<<8)|1;
                }
                catch { Dispose();throw; }
            }
            public T Own<T>(T value) where T:Object { extras.Add(value);return value; }
            public Camera NewCamera(string name)
            {
                var go=new GameObject(name);go.transform.SetParent(Root.transform,false);return go.AddComponent<Camera>();
            }
            public Surface Create(Material[] families=null)
            {
                var s=new Surface(Root.transform,Library.Renderer,Library.RendererIndex,Library.CompositeMaterial,
                    families??new[] {Library.WorldMaterial,Library.WaterMaterial});surfaces.Add(s);return s;
            }
            public GameObject Model(Surface s,string model="central-well")
            {
                var binding=Library.Models.Single(b=>b.Id==model);Assert.NotNull(binding.Prefab);
                return Object.Instantiate(binding.Prefab,s.Content,false);
            }
            public void Dispose()
            {
                try { for(int i=surfaces.Count-1;i>=0;i--)surfaces[i].Dispose(); }
                finally
                {
                    if(QualitySettings.renderPipeline!=quality)QualitySettings.renderPipeline=quality;
                    if(GraphicsSettings.defaultRenderPipeline!=defaults)GraphicsSettings.defaultRenderPipeline=defaults;
                    if(Root!=null)Object.DestroyImmediate(Root);
                    for(int i=extras.Count-1;i>=0;i--)if(extras[i]!=null)
                    {if(extras[i] is RenderTexture rt)rt.Release();Object.DestroyImmediate(extras[i]);}
                }
            }
        }
        private static void Near(Vector3 a,Vector3 b,string why="")=>Assert.Less(Vector3.Distance(a,b),.001f,why);
        private static void Target(Surface s,Camera source,float scale)
        {
            var rt=s.Camera.targetTexture;Assert.NotNull(rt);Assert.IsTrue(rt.IsCreated());
            Assert.AreEqual(Mathf.Max(1,Mathf.RoundToInt(source.pixelRect.width*scale)),rt.width);
            Assert.AreEqual(Mathf.Max(1,Mathf.RoundToInt(source.pixelRect.height*scale)),rt.height);
        }
        private static void Gone(Object value,string why)=>Assert.IsTrue(value==null,why);
        private static Color Pixel(Texture2D texture,int x,int y)=>texture.GetPixels32()[(24-y)*80+x];
        private static string CameraState(Camera c)=>JsonUtility.ToJson(new CameraReceipt(c));
        [Serializable] private sealed class CameraReceipt
        {
            public bool enabled,orthographic,hdr,msaa;public float size,aspect,depth,near,far;
            public int targetId,mask;public Rect rect;public Vector3 position;public Quaternion rotation;public Color background;
            public CameraReceipt(Camera c)
            {enabled=c.enabled;orthographic=c.orthographic;hdr=c.allowHDR;msaa=c.allowMSAA;size=c.orthographicSize;aspect=c.aspect;depth=c.depth;
             near=c.nearClipPlane;far=c.farClipPlane;targetId=c.targetTexture?.GetInstanceID()??0;mask=c.cullingMask;rect=c.rect;
             position=c.transform.position;rotation=c.transform.rotation;background=c.backgroundColor;}
        }

        [Test] public void ConstructionOwnsHiddenChildrenAndUnrevealedFogUnderBorrowedParent()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();Assert.NotNull(s.Content);Assert.NotNull(s.Camera);Assert.NotNull(s.Composite);Assert.NotNull(s.Sun);
                Assert.IsTrue(s.Content.IsChildOf(f.Root.transform));Assert.IsTrue(s.Camera.transform.IsChildOf(f.Root.transform));
                Assert.IsTrue(s.Composite.transform.IsChildOf(f.Root.transform));Assert.IsTrue(s.Sun.transform.IsChildOf(s.Content));
                Assert.IsFalse(s.Visible);Assert.IsFalse(s.Camera.enabled);Assert.IsFalse(s.Content.gameObject.activeInHierarchy);
                Assert.IsFalse(s.Composite.gameObject.activeInHierarchy);
                Assert.AreEqual(80,s.Fog.width);Assert.AreEqual(25,s.Fog.height);Assert.AreEqual(FilterMode.Point,s.Fog.filterMode);
                Assert.AreEqual(TextureWrapMode.Clamp,s.Fog.wrapMode);Assert.IsTrue(s.Fog.GetPixels32().All(c=>c.a==0));
                s.Sync(f.Source);Assert.IsTrue(s.Visible);Assert.IsTrue(s.Camera.enabled);
            }
        }
        [Test] public void BorrowedSourceCameraAndTargetRemainUnchangedThroughSyncAndDisposal()
        {
            using(var f=new Fixture())
            {
                string before=CameraState(f.Source);var s=f.Create();s.Sync(f.Source);s.Sync(f.Source,true,true);s.Sync(f.Source,false);s.Dispose();
                Assert.AreEqual(before,CameraState(f.Source));Assert.AreSame(f.Borrowed,f.Source.targetTexture);Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void XZCameraProjectsTheSameNativeCellsAsTheBorrowedXYCamera()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);Near(new Vector3(40,35,12.75f-35/Mathf.Tan(56*Mathf.Deg2Rad)),s.Camera.transform.position);
                Near(Quaternion.Euler(56,0,0)*Vector3.forward,s.Camera.transform.forward);Near(Quaternion.Euler(56,0,0)*Vector3.up,s.Camera.transform.up);
                Assert.AreEqual(f.Source.depth-1,s.Camera.depth);Assert.AreEqual(f.Source.orthographicSize,s.Camera.orthographicSize);
                Assert.AreEqual(f.Source.aspect,s.Camera.aspect);Assert.AreEqual(1<<Village3DPresenter.WorldLayer,s.Camera.cullingMask);
                var data=s.Camera.GetUniversalAdditionalCameraData();
                Assert.AreSame(((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).GetRenderer(f.Library.RendererIndex),data.scriptableRenderer);
                Assert.AreEqual(CameraRenderType.Base,data.renderType);Assert.IsFalse(data.renderPostProcessing);
                Assert.AreEqual(CameraOverrideOption.Off,data.requiresColorOption);Assert.AreEqual(CameraOverrideOption.Off,data.requiresDepthOption);
                Assert.IsFalse(s.Camera.allowHDR);Assert.IsFalse(s.Camera.allowMSAA);
                foreach(bool low in new[]{false,true})
                {
                    s.Sync(f.Source,true,low);
                    foreach(var at in new[]{new Vector2Int(40,12),new Vector2Int(29,3),new Vector2Int(55,21)})
                    {
                        var world=Village3DProjection.CellCentre(at.x,at.y);var a=s.Camera.WorldToViewportPoint(world);
                        var b=f.Source.WorldToViewportPoint(new Vector3(world.x,world.z,0));
                        Assert.That(a.x,Is.EqualTo(b.x).Within(.0001f));Assert.That(a.y,Is.EqualTo(b.y).Within(.0001f));
                    }
                }
            }
        }
        [Test] public void CompositeUsesOrdinaryXYFramingLayerAndDepth()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var t=s.Composite.transform;
                Near(new Vector3(f.Source.transform.position.x,f.Source.transform.position.y,2),t.position);
                Near(new Vector3(2*f.Source.orthographicSize*f.Source.aspect,2*f.Source.orthographicSize,1),t.localScale);
                Assert.AreEqual(OrdinaryWorldLayer,t.gameObject.layer);Assert.AreEqual(3,s.Composite.sortingOrder);
                Assert.AreEqual(ShadowCastingMode.Off,s.Composite.shadowCastingMode);Assert.IsFalse(s.Composite.receiveShadows);
                Assert.AreNotSame(f.Library.CompositeMaterial,s.Composite.sharedMaterial);
                Assert.AreSame(f.Library.CompositeMaterial.shader,s.Composite.sharedMaterial.shader);
                Assert.AreSame(s.Camera.targetTexture,s.Composite.sharedMaterial.GetTexture("_MainTex"));
                Assert.AreEqual(new Rect(0,0,1,1),s.Camera.rect);
            }
        }
        [Test] public void TargetUsesTheCroppedPixelRectangleAndActualAllocatedColorDepthBuffer()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);Target(s,f.Source,1);var rt=s.Camera.targetTexture;
                Assert.Less(rt.width,f.Borrowed.width);Assert.Less(rt.height,f.Borrowed.height);
                Assert.AreNotSame(f.Borrowed,rt);Assert.AreEqual(RenderTextureFormat.ARGB32,rt.format);
                Assert.GreaterOrEqual(rt.depth,24,"Backend may promote requested24-bit depth to32.");Assert.AreEqual(1,rt.antiAliasing);Assert.AreEqual(FilterMode.Bilinear,rt.filterMode);
            }
        }
        [Test] public void SteadyStateSyncReusesOwnedResources()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var camera=s.Camera;var rt=camera.targetTexture;var fog=s.Fog;
                var mesh=s.Composite.GetComponent<MeshFilter>().sharedMesh;var material=s.MaterialFor(f.Library.WorldMaterial);
                for(int i=0;i<5;i++)s.Sync(f.Source);
                Assert.AreSame(camera,s.Camera);Assert.AreSame(rt,s.Camera.targetTexture);Assert.AreSame(fog,s.Fog);
                Assert.AreSame(mesh,s.Composite.GetComponent<MeshFilter>().sharedMesh);Assert.AreSame(material,s.MaterialFor(f.Library.WorldMaterial));
            }
        }
        [Test] public void ViewportResizeReleasesOnlyThePreviousOwnedTarget()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var old=s.Camera.targetTexture;var camera=s.Camera;var fog=s.Fog;
                f.Source.rect=new Rect(.1f,.1f,.5f,.6f);f.Source.aspect=f.Source.pixelRect.width/f.Source.pixelRect.height;
                s.Sync(f.Source);Target(s,f.Source,1);Assert.AreNotSame(old,s.Camera.targetTexture);
                Assert.IsTrue(old==null||!old.IsCreated());Assert.AreSame(camera,s.Camera);Assert.AreSame(fog,s.Fog);
                Assert.IsTrue(f.Borrowed.IsCreated());Assert.AreSame(f.Borrowed,f.Source.targetTexture);
            }
        }
        [Test] public void LowDetailChangesOwnedResolutionAndShadowsAndRestoresFullDetail()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var full=s.Camera.targetTexture;Assert.AreEqual(LightShadows.Soft,s.Sun.shadows);
                var position=s.Camera.transform.position;float size=s.Camera.orthographicSize,aspect=s.Camera.aspect;
                s.Sync(f.Source,true,true);Target(s,f.Source,.75f);var low=s.Camera.targetTexture;
                Assert.AreEqual(LightShadows.None,s.Sun.shadows);Assert.IsTrue(full==null||!full.IsCreated());
                s.Sync(f.Source,true,true);Assert.AreSame(low,s.Camera.targetTexture);
                Near(position,s.Camera.transform.position);Assert.AreEqual(size,s.Camera.orthographicSize);Assert.AreEqual(aspect,s.Camera.aspect);
                s.Sync(f.Source);Target(s,f.Source,1);Assert.AreEqual(LightShadows.Soft,s.Sun.shadows);Assert.IsTrue(low==null||!low.IsCreated());
            }
        }
        [Test] public void HideShowDisablesAllOwnedPresentationWithoutResourceChurn()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();var model=f.Model(s);s.Prepare(model);s.Sync(f.Source);var rt=s.Camera.targetTexture;
                for(int i=0;i<3;i++)
                {
                    s.Sync(f.Source,false);Assert.IsFalse(s.Visible);Assert.IsFalse(s.Camera.enabled);
                    Assert.IsFalse(model.activeInHierarchy);Assert.IsFalse(s.Composite.gameObject.activeInHierarchy);
                    s.Sync(f.Source);Assert.IsTrue(s.Visible);Assert.IsTrue(s.Camera.enabled);Assert.IsTrue(model.activeInHierarchy);
                    Assert.IsTrue(s.Composite.gameObject.activeInHierarchy);Assert.AreSame(rt,s.Camera.targetTexture);
                }
            }
        }
        [Test] public void NullSourceHidesAndReplacementSourceRebindsWithoutDestroyingBorrowedCameras()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var camera=s.Camera;string before=CameraState(f.Source);
                s.Sync(null);Assert.IsFalse(s.Visible);Assert.IsFalse(camera.enabled);
                var other=f.NewCamera("Borrowed replacement source");other.enabled=false;other.orthographic=true;other.orthographicSize=9;
                other.transform.position=new Vector3(18,8,-15);other.targetTexture=f.Borrowed;other.aspect=1.5f;
                string replacement=CameraState(other);s.Sync(other);Assert.IsTrue(s.Visible);Assert.AreSame(camera,s.Camera);
                Near(new Vector3(18,35,8-35/Mathf.Tan(56*Mathf.Deg2Rad)),camera.transform.position);Assert.AreEqual(9,camera.orthographicSize);
                s.Dispose();Assert.AreEqual(before,CameraState(f.Source));Assert.AreEqual(replacement,CameraState(other));
            }
        }
        [Test] public void DisposeReleasesCapturedOwnedResourcesAndIsIdempotent()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var camera=s.Camera;var content=s.Content;var composite=s.Composite;var sun=s.Sun;
                var rt=camera.targetTexture;var fog=s.Fog;var mesh=composite.GetComponent<MeshFilter>().sharedMesh;
                var mats=new[]{s.MaterialFor(f.Library.WorldMaterial),s.MaterialFor(f.Library.WaterMaterial),composite.sharedMaterial};
                s.Dispose();s.Dispose();Assert.IsFalse(s.Visible);
                foreach(var value in new Object[]{camera,content,composite,sun,rt,fog,mesh})Gone(value,"Owned resource survived Dispose.");
                foreach(var material in mats)Gone(material,"Owned material survived Dispose.");
                Assert.IsTrue(f.Root!=null&&f.Source!=null&&f.Library!=null);Assert.IsTrue(f.Borrowed.IsCreated());
                Assert.IsTrue(f.Library.WorldMaterial!=null&&f.Library.WaterMaterial!=null&&f.Library.CompositeMaterial!=null);
            }
        }
        [Test] public void ParentDestroyedFirstStillAllowsDisposalOfNonHierarchyResources()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();s.Sync(f.Source);var rt=s.Camera.targetTexture;var fog=s.Fog;
                var mesh=s.Composite.GetComponent<MeshFilter>().sharedMesh;var mat=s.MaterialFor(f.Library.WorldMaterial);
                // The borrowed camera belongs to its caller, outside the owned surface hierarchy.
                f.Own(f.Source.gameObject);f.Source.transform.SetParent(null,true);
                string borrowedBefore=CameraState(f.Source);
                Object.DestroyImmediate(f.Root);Assert.DoesNotThrow(()=>s.Dispose());Assert.IsFalse(s.Visible);
                Assert.IsTrue(f.Source!=null);Assert.AreEqual(borrowedBefore,CameraState(f.Source));
                Gone(rt,"RT ownership must survive the camera becoming Unity-null.");Gone(fog,"Orphaned fog texture.");
                Gone(mesh,"Orphaned composite mesh.");Gone(mat,"Orphaned instance material.");Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void TwoSurfacesKeepFogMaterialsTargetsAndDisposalIndependent()
        {
            using(var f=new Fixture())
            {
                var a=f.Create();var b=f.Create();a.Sync(f.Source);b.Sync(f.Source);
                Assert.AreNotSame(a.Fog,b.Fog);Assert.AreNotSame(a.Camera.targetTexture,b.Camera.targetTexture);
                var am=a.MaterialFor(f.Library.WorldMaterial);var bm=b.MaterialFor(f.Library.WorldMaterial);
                Assert.AreNotSame(am,bm);Assert.AreSame(a.Fog,am.GetTexture("_FogLight"));Assert.AreSame(b.Fog,bm.GetTexture("_FogLight"));
                var zone=new Zone("surface-fog-independence");zone.GetCell(12,7).Explored=zone.GetCell(12,7).IsVisible=true;
                a.FogFrom(zone);Assert.Greater(Pixel(a.Fog,12,7).a,.99f);Assert.AreEqual(0,Pixel(b.Fog,12,7).a);
                a.Dispose();b.Sync(f.Source);Assert.IsTrue(b.Visible);Assert.IsTrue(bm!=null&&b.Fog!=null&&b.Camera.targetTexture.IsCreated());
            }
        }
        [Test] public void ExplicitPaletteFamiliesAndActualWellWaterSurviveRepeatedPreparation()
        {
            using(var f=new Fixture())
            {
                var alternate=f.Own(new Material(f.Library.WorldMaterial));alternate.SetColor("_BaseColor",new Color(.4f,.7f,.2f,1));
                var atlas=f.Own(new Texture2D(2,2));alternate.SetTexture("_BaseMap",atlas);
                var s=f.Create(new[]{f.Library.WorldMaterial,f.Library.WaterMaterial,alternate});var well=f.Model(s);
                bool sawWater=false,sawPalette=false;
                foreach(var r in well.GetComponentsInChildren<Renderer>(true))
                {
                    var slots=r.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {if(slots[i]==f.Library.WaterMaterial)sawWater=true;if(slots[i]==f.Library.WorldMaterial){slots[i]=alternate;sawPalette=true;}}
                    r.sharedMaterials=slots;
                }
                Assert.IsTrue(sawWater&&sawPalette,"Actual imported well needs both real material families.");
                s.Prepare(well);var before=well.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
                s.Prepare(well,true);CollectionAssert.AreEqual(before,well.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray());
                Assert.IsTrue(before.Contains(s.MaterialFor(alternate)));Assert.IsTrue(before.Contains(s.MaterialFor(f.Library.WaterMaterial)));
                Assert.AreSame(atlas,s.MaterialFor(alternate).GetTexture("_BaseMap"));Assert.AreEqual(alternate.GetColor("_BaseColor"),s.MaterialFor(alternate).GetColor("_BaseColor"));
                Assert.AreSame(f.Library.WaterMaterial.shader,s.MaterialFor(f.Library.WaterMaterial).shader);
                Assert.AreSame(s.MaterialFor(alternate),s.MaterialFor(s.MaterialFor(alternate)));
                Assert.AreEqual(2.2f,s.MaterialFor(alternate).GetFloat("_Exposure"));
                Assert.AreNotSame(s.Fog,alternate.GetTexture("_FogLight"),"Borrowed material must retain its original fog binding.");
            }
        }
        [Test] public void UnknownMaterialFamilyIsRejectedInsteadOfUsingTheVillageAtlas()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();var unknown=f.Own(new Material(f.Library.WorldMaterial));var texture=unknown.GetTexture("_FogLight");
                Assert.Throws<InvalidOperationException>(()=>s.MaterialFor(unknown));Assert.Throws<InvalidOperationException>(()=>s.MaterialFor(null));
                var well=f.Model(s);var renderer=well.GetComponentInChildren<Renderer>(true);renderer.sharedMaterials=new[]{unknown};
                Assert.Throws<InvalidOperationException>(()=>s.Prepare(well));Assert.AreSame(texture,unknown.GetTexture("_FogLight"));
                renderer.sharedMaterials=new[]{f.Library.WorldMaterial};Assert.DoesNotThrow(()=>s.Prepare(well));
                Assert.AreSame(s.MaterialFor(f.Library.WorldMaterial),renderer.sharedMaterial);
            }
        }
        [Test] public void PrepareRejectsBorrowedModelsAndPreservesUnrelatedOwnedPropertyBlockData()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();var prefab=f.Library.Models.Single(b=>b.Id=="central-well").Prefab;
                var sourceMaterials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
                Assert.Throws<ArgumentException>(()=>s.Prepare(prefab));
                var sibling=Object.Instantiate(prefab,f.Root.transform,false);Assert.Throws<ArgumentException>(()=>s.Prepare(sibling));
                CollectionAssert.AreEqual(sourceMaterials,prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray());
                var owned=f.Model(s);var r=owned.GetComponentInChildren<Renderer>(true);var block=new MaterialPropertyBlock();
                block.SetFloat("_SurfaceProbeValue",.37f);r.SetPropertyBlock(block);s.Prepare(owned,true);r.GetPropertyBlock(block);
                Assert.AreEqual(.37f,block.GetFloat("_SurfaceProbeValue"));Assert.AreEqual(1,block.GetFloat("_Transient"));
                Assert.IsTrue(owned.GetComponentsInChildren<Transform>(true).All(t=>t.gameObject.layer==Village3DPresenter.WorldLayer));
                s.Prepare(owned,false);r.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_Transient"));Assert.AreEqual(.37f,block.GetFloat("_SurfaceProbeValue"));
            }
        }
        [Test] public void MissingActivePipelineFailsClosedBeforeCreatingOwnedChildren()
        {
            using(var f=new Fixture())
            {
                var positive=f.Create();positive.Sync(f.Source);Assert.IsTrue(positive.Visible);positive.Dispose();
                int children=f.Root.transform.childCount;string source=CameraState(f.Source);
                var quality=QualitySettings.renderPipeline;var defaults=GraphicsSettings.defaultRenderPipeline;
                try
                {
                    QualitySettings.renderPipeline=null;GraphicsSettings.defaultRenderPipeline=null;
                    Assert.IsNull(GraphicsSettings.currentRenderPipeline,"The negative control must actually remove the active pipeline.");
                    Assert.Throws<InvalidOperationException>(()=>f.Create());
                    Assert.AreEqual(children,f.Root.transform.childCount);Assert.AreEqual(source,CameraState(f.Source));
                }
                finally { GraphicsSettings.defaultRenderPipeline=defaults;QualitySettings.renderPipeline=quality; }
            }
        }
        [Test] public void WrongRendererIndexIdentityAndMissingMaterialFailClosed()
        {
            using(var f=new Fixture())
            {
                var positive=f.Create();positive.Sync(f.Source);Assert.IsTrue(positive.Visible);positive.Dispose();int children=f.Root.transform.childCount;
                var alien=f.Own(ScriptableObject.CreateInstance<UniversalRendererData>());
                var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
                Action<UniversalRendererData,int,Material,Material[]> refuses=(renderer,index,composite,families)=>
                {
                    Assert.Throws<InvalidOperationException>(()=>new Surface(f.Root.transform,renderer,index,composite,families));
                    Assert.AreEqual(children,f.Root.transform.childCount,"Invalid resources must not leave owned children.");
                };
                var valid=new[]{f.Library.WorldMaterial,f.Library.WaterMaterial};
                refuses(f.Library.Renderer,-1,f.Library.CompositeMaterial,valid);
                refuses(f.Library.Renderer,pipeline.rendererDataList.Length,f.Library.CompositeMaterial,valid);
                refuses(alien,f.Library.RendererIndex,f.Library.CompositeMaterial,valid);
                refuses(f.Library.Renderer,f.Library.RendererIndex,null,valid);
                refuses(f.Library.Renderer,f.Library.RendererIndex,f.Library.CompositeMaterial,new Material[]{f.Library.WorldMaterial,null});
                Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
        [Test] public void FogUploadMapsNativeRowsAndReadsVisibilityWithoutDiscoveringCells()
        {
            using(var f=new Fixture())
            {
                var s=f.Create();var zone=new Zone("surface-fog-row-control");zone.AmbientLevel=.1f;zone.AmbientTint=Color.white;
                var visible=zone.GetCell(13,2);visible.Explored=visible.IsVisible=true;
                var memory=zone.GetCell(13,22);memory.Explored=true;memory.IsVisible=false;
                s.FogFrom(zone);Assert.That(Pixel(s.Fog,13,2).a,Is.EqualTo(1).Within(1f/255));
                Assert.That(Pixel(s.Fog,13,2).r,Is.EqualTo(.1f).Within(1f/255));
                Assert.That(Pixel(s.Fog,13,22).a,Is.EqualTo(.5f).Within(1f/255));
                Assert.That(Pixel(s.Fog,13,22).r,Is.EqualTo(ZoneRenderer.RememberedBrightnessFor(.1f)).Within(1f/255));
                Assert.AreEqual(0,Pixel(s.Fog,14,2).a);Assert.IsFalse(zone.GetCell(14,2).Explored);
                s.FogFrom(zone,reveal:true);Assert.AreEqual(1,Pixel(s.Fog,14,2).a);Assert.IsFalse(zone.GetCell(14,2).Explored);
                Assert.IsFalse(memory.IsVisible);s.FogFrom(null);Assert.IsTrue(s.Fog.GetPixels32().All(c=>c.a==0));
            }
        }
        [Test] public void SurfaceOperationsDoNotChangeActualNativeWorldTurnsPreferencesOrHooks()
        {
            using(var native=new MorrowfastStartFixture())
            using(var f=new Fixture())
            {
                native.Generate();native.Place();var zone=native.Zone;var members=zone.GetReadOnlyEntities().ToArray();
                var positions=members.Select(zone.GetEntityPosition).ToArray();var ids=members.Select(e=>e.ID).ToArray();
                var tags=members.Select(e=>string.Join("|",e.Tags.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value))).ToArray();
                var flags=new List<(bool,bool)>();for(int y=0;y<25;y++)for(int x=0;x<80;x++)flags.Add((zone.GetCell(x,y).Explored,zone.GetCell(x,y).IsVisible));
                int version=zone.EntityVersion,written=zone.TileState.WrittenCount,hp=native.Player.GetStatValue("Hitpoints");
                var inventory=native.Player.GetPart<InventoryPart>();var carried=inventory.Objects.ToArray();var equipped=inventory.EquippedItems.ToArray();
                var state=MorrowfastSceneRuntime.GetState(zone);string doors=state.OpenDoorIds,roofs=state.LiftedRoofIds,removed=state.RemovedIds;
                var turns=native.Capture().TurnManager;int tick=turns.TickCount,energy=turns.GetEnergy(native.Player);
                var full=ZoneRenderHooks.FullDirtyCallback;var cell=ZoneRenderHooks.CellDirtyCallback;
                var hooks=new Delegate[]{EntityVisualHooks.MovedCallback,EntityVisualHooks.AttackCallback,EntityVisualHooks.CastCallback,EntityVisualHooks.DamageCallback,EntityVisualHooks.DeathCallback};
                string[] keys={Village3DSettings.PreferenceKey,Village3DSettings.LowDetailPreferenceKey};
                var present=keys.Select(PlayerPrefs.HasKey).ToArray();var prefs=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();
                bool mode=Village3DSettings.Enabled,detail=Village3DSettings.LowDetail;
                var s=f.Create();var model=f.Model(s);s.Prepare(model,true);
                for(int i=0;i<3;i++){s.FogFrom(zone,reveal:i==1);s.Sync(f.Source,i!=1,i==2);}s.Dispose();
                CollectionAssert.AreEquivalent(members,zone.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,members.Select(zone.GetEntityPosition));
                CollectionAssert.AreEqual(ids,members.Select(e=>e.ID));CollectionAssert.AreEqual(tags,members.Select(e=>string.Join("|",e.Tags.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value))));
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(written,zone.TileState.WrittenCount);Assert.AreEqual(hp,native.Player.GetStatValue("Hitpoints"));
                Assert.AreEqual(doors,state.OpenDoorIds);Assert.AreEqual(roofs,state.LiftedRoofIds);Assert.AreEqual(removed,state.RemovedIds);
                CollectionAssert.AreEqual(carried,inventory.Objects);CollectionAssert.AreEqual(equipped,inventory.EquippedItems);
                int index=0;for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.AreEqual(flags[index++],(zone.GetCell(x,y).Explored,zone.GetCell(x,y).IsVisible));
                Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(energy,turns.GetEnergy(native.Player));
                Assert.AreSame(full,ZoneRenderHooks.FullDirtyCallback);Assert.AreSame(cell,ZoneRenderHooks.CellDirtyCallback);
                CollectionAssert.AreEqual(hooks,new Delegate[]{EntityVisualHooks.MovedCallback,EntityVisualHooks.AttackCallback,EntityVisualHooks.CastCallback,EntityVisualHooks.DamageCallback,EntityVisualHooks.DeathCallback});
                CollectionAssert.AreEqual(present,keys.Select(PlayerPrefs.HasKey));CollectionAssert.AreEqual(prefs,keys.Select(k=>PlayerPrefs.GetInt(k)));
                Assert.AreEqual(mode,Village3DSettings.Enabled);Assert.AreEqual(detail,Village3DSettings.LowDetail);
            }
        }
    }
}
