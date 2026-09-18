#if UNITY_EDITOR
using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Adopt this file first: absent runtime classes are the deliberate initial RED.
    // No Build call or asset writes. Artifact cases require the separately generated scene.
    [TestFixture] public sealed class Village3DShowcaseCutawayTests
    {
        GameObject root,roof,inside,ordinary,outside;
        Village3DShowcaseCutaway cutaway;
        [SetUp] public void SetUp()
        {
            root=new GameObject("owned art test");roof=Child("roof");inside=Child("inside");ordinary=Child("ordinary");
            outside=new GameObject("unrelated scene object");cutaway=root.AddComponent<Village3DShowcaseCutaway>();
        }
        GameObject Child(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go;}
        [TearDown] public void TearDown(){Object.DestroyImmediate(root);Object.DestroyImmediate(outside);}
        void Configure(bool state=false)=>cutaway.Configure(new[]{roof},new[]{inside},state);
        [Test] public void ClosedShowsRoofAndHidesInterior(){Configure();Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);Assert.That(cutaway.IsCutaway,Is.False);}
        [Test] public void OpenHidesRoofAndShowsInterior(){Configure();cutaway.SetCutaway(true);Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);Assert.That(cutaway.IsCutaway,Is.True);}
        [Test] public void TwoTogglesRestoreClosedState(){Configure();cutaway.Toggle();cutaway.Toggle();Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);}
        [Test] public void ConfigureCanStartOpen(){Configure(true);Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);}
        [Test] public void ToggleDoesNotTouchOrdinaryOrUnrelatedObjects(){ordinary.SetActive(false);Configure();cutaway.Toggle();Assert.That(ordinary.activeSelf,Is.False);Assert.That(outside.activeSelf,Is.True);}
        [Test] public void TargetsAreCopiedRatherThanRetainingCallerArrays(){var a=new[]{roof};var b=new[]{inside};cutaway.Configure(a,b,false);a[0]=outside;b[0]=ordinary;cutaway.Toggle();Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);Assert.That(outside.activeSelf,Is.True);Assert.That(ordinary.activeSelf,Is.True);}
        [Test] public void NullListsAreSafe(){cutaway.Configure(null,null,false);Assert.DoesNotThrow(()=>cutaway.Toggle());Assert.That(outside.activeSelf,Is.True);}
        [Test] public void DestroyedEntryDoesNotBlockRemainingEntries(){var second=Child("second roof");cutaway.Configure(new[]{roof,second},new[]{inside},false);Object.DestroyImmediate(roof);cutaway.Toggle();Assert.That(second.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);}
        [Test] public void SameStateReappliesExpectedAppearance(){Configure();roof.SetActive(false);inside.SetActive(true);cutaway.SetCutaway(false);Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);}
        [Test] public void RejectsForeignTargetBeforeReplacingCurrentConfiguration(){Configure();Assert.Throws<ArgumentException>(()=>cutaway.Configure(new[]{outside},new[]{inside},true));Assert.That(cutaway.IsCutaway,Is.False);cutaway.Toggle();Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);Assert.That(outside.activeSelf,Is.True);}
        [Test] public void RejectsItsOwnRoot(){Configure();Assert.Throws<ArgumentException>(()=>cutaway.Configure(new[]{root},new[]{inside},false));Assert.That(root.activeSelf,Is.True);}
        [Test] public void RejectsSameTargetInBothGroups(){Configure();Assert.Throws<ArgumentException>(()=>cutaway.Configure(new[]{roof},new[]{roof},true));Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);}
        [Test] public void ReparentedTargetIsNoLongerOwned(){Configure();roof.transform.SetParent(outside.transform,false);cutaway.Toggle();Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.True);}
        [Test] public void ReenableRestoresSerializedCutaway(){Configure(true);root.SetActive(false);roof.SetActive(true);inside.SetActive(false);root.SetActive(true);Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);}
        [Test] public void SameSessionJsonRoundTripRetainsOwnedReferences()
        {
            Configure(true);
            // JsonUtility is the same-session instance-ID format. Persistent scene
            // reference proof belongs to the reloaded artifact fixture below.
            var json=JsonUtility.ToJson(cutaway);TestContext.WriteLine(json);
            cutaway.SetCutaway(false);JsonUtility.FromJsonOverwrite(json,cutaway);
            var restored=new SerializedObject(cutaway);
            Assert.That(restored.FindProperty("roofs").GetArrayElementAtIndex(0).objectReferenceValue,Is.SameAs(roof),json);
            Assert.That(restored.FindProperty("interiors").GetArrayElementAtIndex(0).objectReferenceValue,Is.SameAs(inside),json);
            Assert.That(cutaway.IsCutaway,Is.True);
            cutaway.SetCutaway(cutaway.IsCutaway);
            Assert.That(roof.activeSelf,Is.False);Assert.That(inside.activeSelf,Is.True);
            cutaway.SetCutaway(false);
            Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);
        }
        [Test] public void DisablingComponentDoesNotResetExistingAppearance(){Configure();cutaway.enabled=false;Assert.That(roof.activeSelf,Is.True);Assert.That(inside.activeSelf,Is.False);}
        [TestCase(1920,1080)] [TestCase(800,1200)] [TestCase(1500,1020)]
        public void FramingPreservesTargetAspectWithoutStretching(int width,int height){var rect=Village3DShowcaseFrame.CalculateViewport(width,height);Assert.That(rect.width*width/(rect.height*height),Is.EqualTo(37.5f/25.5f).Within(.0001f));Assert.That(rect.x,Is.GreaterThanOrEqualTo(0));Assert.That(rect.y,Is.GreaterThanOrEqualTo(0));Assert.That(rect.xMax,Is.LessThanOrEqualTo(1.00001f));Assert.That(rect.yMax,Is.LessThanOrEqualTo(1.00001f));Assert.That(rect.center,Is.EqualTo(new Vector2(.5f,.5f)));}
        [TestCase(0,1080)] [TestCase(1920,0)] [TestCase(-1,100)]
        public void InvalidViewportIsRejected(int width,int height){Assert.Throws<ArgumentOutOfRangeException>(()=>Village3DShowcaseFrame.CalculateViewport(width,height));}
    }

    // These asset assertions deliberately remain RED until the owned scene has been built.
    // OpenPreviewScene/ClosePreviewScene leave the user's ordinary scenes and active scene alone.
    [TestFixture] public sealed class Village3DShowcaseArtifactTests
    {
        const string Path="Assets/Art3D/Village/Scenes/VillageShowcase.unity";
        Scene preview;int activeHandle;GameObject root;Village3DLibrary library;
        [SetUp] public void SetUp()
        {
            activeHandle=SceneManager.GetActiveScene().handle;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path),Is.Not.Null,"Generate the explicit owned art showcase first.");
            preview=EditorSceneManager.OpenPreviewScene(Path);root=preview.GetRootGameObjects().Single(x=>x.name=="Village Art Showcase — no gameplay");
            library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.That(library,Is.Not.Null);library.Validate();
        }
        [TearDown] public void TearDown(){if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);Assert.That((int)SceneManager.GetActiveScene().handle,Is.EqualTo(activeHandle));}
        Transform Group(string name)=>root.transform.Find(name);
        [Test] public void EveryAuthoredOwnerIsPlacedExactlyOnce(){var group=Group("Owners");Assert.That(group.childCount,Is.EqualTo(71));foreach(var owner in library.Definition.owners){var t=group.Find(owner.ownerId);Assert.That(t,Is.Not.Null,owner.ownerId);Assert.That(Vector3.Distance(t.position,owner.position),Is.LessThan(.00001f));Assert.That(t.localScale,Is.EqualTo(owner.scale));Assert.That(Quaternion.Angle(t.rotation,Quaternion.Euler(0,owner.rotationY,0)),Is.LessThan(.001f));}}
        [Test] public void StaticPlacementsIncludeCompleteGroundAndAccents(){var group=Group("Static art");Assert.That(group.childCount,Is.EqualTo(library.Definition.staticPlacements.Length));foreach(var p in library.Definition.staticPlacements)Assert.That(group.Find(p.id),Is.Not.Null,p.id);Assert.That(group.Cast<Transform>().Count(t=>t.name.StartsWith("ground-")),Is.EqualTo(40));}
        [Test] public void RoofsAndFurnishingsUseOppositeCutawayStates(){var c=root.GetComponent<Village3DShowcaseCutaway>();Assert.That(c,Is.Not.Null);foreach(bool open in new[]{false,true,false}){c.SetCutaway(open);foreach(var o in library.Definition.owners){var obj=Group("Owners").Find(o.ownerId).gameObject;if(o.kind=="roof")Assert.That(obj.activeSelf,Is.EqualTo(!open),o.ownerId);else if(o.visibleWhen=="room-open")Assert.That(obj.activeSelf,Is.EqualTo(open),o.ownerId);else Assert.That(obj.activeSelf,Is.True,o.ownerId);}}}
        [Test] public void FullRevealIsAnOwnedLinearPointTexture(){var mats=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();Assert.That(mats.Length,Is.EqualTo(2));foreach(var m in mats){Assert.That(m,Is.Not.SameAs(library.WorldMaterial));Assert.That(m,Is.Not.SameAs(library.WaterMaterial));var fog=m.GetTexture("_FogLight") as Texture2D;Assert.That(fog,Is.Not.Null);Assert.That(fog.width,Is.EqualTo(80));Assert.That(fog.height,Is.EqualTo(25));Assert.That(fog.mipmapCount,Is.EqualTo(1));Assert.That(fog.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(fog.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));Assert.That(fog.GetPixels32().All(p=>p.r==255&&p.g==255&&p.b==255&&p.a==255),Is.True);Assert.That(UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(fog.graphicsFormat),Is.False);}}
        [Test] public void WaterRenderersRetainWaterShader(){var water=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.EndsWith("__Water",StringComparison.Ordinal)).ToArray();Assert.That(water.Length,Is.GreaterThan(0));foreach(var r in water)Assert.That(r.sharedMaterials.All(m=>m.shader==library.WaterMaterial.shader),Is.True,r.name);}
        [Test] public void CameraUsesDedicatedRendererAndTopDownFraming(){var camera=root.GetComponentInChildren<Camera>(true);Assert.That(camera,Is.Not.Null);Assert.That(camera.orthographic,Is.True);Assert.That(camera.orthographicSize,Is.EqualTo(12.75f));Assert.That(camera.transform.position,Is.EqualTo(new Vector3(40,35,12.5f)));Assert.That(Vector3.Dot(camera.transform.forward,Vector3.down),Is.GreaterThan(.9999f));Assert.That(camera.cullingMask,Is.EqualTo(1<<Village3DPresenter.WorldLayer));Assert.That(new SerializedObject(camera.GetUniversalAdditionalCameraData()).FindProperty("m_RendererIndex").intValue,Is.EqualTo(library.RendererIndex));Assert.That(camera.GetComponent<Village3DShowcaseFrame>(),Is.Not.Null);}
        [Test] public void LightIsSoftAndShowcaseScoped(){var light=root.GetComponentInChildren<Light>(true);Assert.That(light.type,Is.EqualTo(LightType.Directional));Assert.That(light.shadows,Is.EqualTo(LightShadows.Soft));Assert.That(light.cullingMask,Is.EqualTo(1<<Village3DPresenter.WorldLayer));Assert.That(light.GetComponent<UniversalAdditionalLightData>(),Is.Not.Null);}
        [Test] public void PlayerPreviewIsVisualAndHasNoGameplayOrPhysics(){var player=Group("Player preview — art pose only");Assert.That(player,Is.Not.Null);Assert.That(player.position,Is.EqualTo(Village3DProjection.CellCentre(40,23)));Assert.That(player.GetComponentInChildren<Animator>(true),Is.Not.Null);Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Rigidbody>(true),Is.Empty);var types=root.GetComponentsInChildren<MonoBehaviour>(true).Select(c=>c==null?"missing":c.GetType().Name).ToArray();Assert.That(types,Does.Not.Contain("GameBootstrap"));Assert.That(types,Does.Not.Contain("InputHandler"));Assert.That(types,Does.Not.Contain("ZoneRenderer"));Assert.That(types,Does.Not.Contain("Village3DPresenter"));}
    }
}
#endif
