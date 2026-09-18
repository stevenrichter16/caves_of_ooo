using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Rendering
{
    /// <summary>Owns one native-zone XZ render and its XY composite. All input
    /// cameras/materials and native state are borrowed and remain unchanged.
    /// Presenters own model bindings, gameplay hooks and decoration policy.</summary>
    public sealed class NativeZone3DRenderSurface : IDisposable
    {
        public const int WorldLayer = 12;
        public const float CameraAltitude = 35f;
        public const float CameraPitchDegrees = 56f;
        private static readonly float PitchSin = Mathf.Sin(CameraPitchDegrees * Mathf.Deg2Rad);
        private static readonly float PitchCos = Mathf.Cos(CameraPitchDegrees * Mathf.Deg2Rad);
        private static readonly float PitchCot = PitchCos / PitchSin;
        /// <summary>Projection ray through an unchanged legacy XY ground point.
        /// Ground registration is fixed; raised geometry can hit another physical
        /// cell, which the presenter validates for occupancy and visibility.</summary>
        public static Ray GroundPointRay(Vector2 point) => new Ray(
            new Vector3(point.x, CameraAltitude, point.y - CameraAltitude * PitchCot),
            new Vector3(0, -PitchSin, PitchCos));
        private const float CompositeDepth = 2f;
        private readonly Dictionary<Material, Material> families = new Dictionary<Material, Material>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly Color32[] pixels = new Color32[Zone.Width * Zone.Height];
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private Material compositeMaterial;
        private Mesh compositeMesh;
        // Independent ownership survives external destruction of the hierarchy.
        private RenderTexture target;
        private bool disposed;
        public Transform ContentRoot { get; private set; }
        public Camera WorldCamera { get; private set; }
        public Texture2D FogTexture { get; private set; }
        public Renderer CompositeRenderer { get; private set; }
        public Light Sun { get; private set; }
        public bool IsVisible { get; private set; }

        public NativeZone3DRenderSurface(Transform parent, UniversalRendererData renderer, int rendererIndex,
            Material compositeSource, Material[] worldMaterialSources, float exposure)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (parent == null || renderer == null || pipeline == null || rendererIndex < 0
                || rendererIndex >= pipeline.rendererDataList.Length || pipeline.rendererDataList[rendererIndex] != renderer)
                throw new InvalidOperationException("Native 3D renderer is not installed in the active pipeline.");
            ValidateMaterial(compositeSource);
            if (!compositeSource.HasProperty("_MainTex") || worldMaterialSources == null || worldMaterialSources.Length == 0
                || !Village3DProjection.Finite(exposure) || exposure <= 0)
                throw new InvalidOperationException("Native 3D surface resources are incomplete.");
            foreach (var source in worldMaterialSources)
            {
                ValidateMaterial(source);
                if (!source.HasProperty("_FogLight") || !source.HasProperty("_Exposure") || !source.HasProperty("_Transient"))
                    throw new InvalidOperationException("Native 3D material lacks the native fog/light contract.");
            }
            try
            {
                FogTexture = new Texture2D(Zone.Width, Zone.Height, TextureFormat.RGBA32, false, true)
                { name = "Native zone fog and light", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                FogTexture.SetPixels32(pixels); FogTexture.Apply(false, false);
                foreach (var source in worldMaterialSources)
                {
                    if (families.ContainsKey(source)) continue;
                    var material = Clone(source); ownedMaterials.Add(material);
                    material.SetTexture("_FogLight", FogTexture); material.SetFloat("_Exposure", exposure);
                    families.Add(source, material); families.Add(material, material);
                }
                compositeMaterial = Clone(compositeSource);
                ContentRoot = Child("Native 3D world", parent, WorldLayer).transform;
                CreateCamera(parent, rendererIndex); CreateComposite(parent);
                SetVisible(false);
            }
            catch { Dispose(); throw; }
        }
        private static void ValidateMaterial(Material material)
        {
            if (material == null || material.shader == null || !material.shader.isSupported)
                throw new InvalidOperationException("Unsupported native 3D material.");
        }
        private static Material Clone(Material source) => new Material(source) { hideFlags = HideFlags.DontSave };
        private static GameObject Child(string name, Transform parent, int layer)
        {
            var go = new GameObject(name) { layer = layer, hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false); return go;
        }
        /// <summary>Returns this surface's clone for a registered source or its
        /// own clone. Unknown families are rejected rather than changing atlases.</summary>
        public Material MaterialFor(Material sourceOrOwnClone)
        {
            if (disposed || sourceOrOwnClone == null || !families.TryGetValue(sourceOrOwnClone, out var material))
                throw new InvalidOperationException("Unregistered native 3D material family.");
            return material;
        }
        /// <summary>Prepares only an owned instance, preserving unrelated per-
        /// renderer property values. Repeated preparation is idempotent.</summary>
        public void PrepareModel(GameObject model, bool transient)
        {
            if (disposed || model == null || ContentRoot == null || model.transform == ContentRoot
                || !model.transform.IsChildOf(ContentRoot))
                throw new ArgumentException("A model must be an instance below this surface's content root.", nameof(model));
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            // Validate the entire model before touching any renderer or layer.
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials) MaterialFor(material);
            foreach (var transform in model.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = WorldLayer;
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = MaterialFor(materials[i]);
                renderer.sharedMaterials = materials;
                renderer.GetPropertyBlock(properties); properties.SetFloat("_Transient", transient ? 1 : 0);
                renderer.SetPropertyBlock(properties); properties.Clear();
                // Indexed blocks override the renderer-level block. Preserve
                // their other values, but keep the native visibility policy.
                for (int i = 0; i < materials.Length; i++)
                {
                    renderer.GetPropertyBlock(properties, i);
                    if (!properties.isEmpty)
                    {
                        properties.SetFloat("_Transient", transient ? 1 : 0);
                        renderer.SetPropertyBlock(properties, i);
                    }
                    properties.Clear();
                }
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            }
        }
        /// <summary>Uploads current native visibility/light without computing
        /// FOV, discovering cells, or changing the zone. Null clears the view.</summary>
        public void UpdateFog(Zone zone, LightMap light, bool fullReveal)
        {
            if (disposed || FogTexture == null) return;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                pixels[(Zone.Height - 1 - y) * Zone.Width + x] =
                    zone == null ? Color.clear : Village3DVisibility.SampleCell(zone.GetCell(x, y), light, fullReveal);
            FogTexture.SetPixels32(pixels); FogTexture.Apply(false, false);
        }
        private void CreateCamera(Transform parent, int rendererIndex)
        {
            WorldCamera = Child("Native 3D world camera", parent, WorldLayer).AddComponent<Camera>();
            WorldCamera.enabled = false; WorldCamera.orthographic = true;
            WorldCamera.nearClipPlane = .1f; WorldCamera.farClipPlane = 120;
            WorldCamera.cullingMask = 1 << WorldLayer; WorldCamera.clearFlags = CameraClearFlags.SolidColor;
            WorldCamera.backgroundColor = Color.black; WorldCamera.allowHDR = false; WorldCamera.allowMSAA = false;
            WorldCamera.useOcclusionCulling = false;
            var data = WorldCamera.GetUniversalAdditionalCameraData(); data.renderType = CameraRenderType.Base;
            data.SetRenderer(rendererIndex); data.renderPostProcessing = false;
            data.requiresColorOption = CameraOverrideOption.Off; data.requiresDepthOption = CameraOverrideOption.Off;
            Sun = Child("Native soft daylight", ContentRoot, WorldLayer).AddComponent<Light>();
            Sun.type = LightType.Directional; Sun.color = new Color(1, .94f, .82f); Sun.intensity = 1.1f;
            Sun.cullingMask = 1 << WorldLayer; Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = .55f; Sun.shadowBias = .04f; Sun.shadowNormalBias = .15f;
            Sun.transform.rotation = Quaternion.Euler(55, -35, 0);
            Sun.gameObject.AddComponent<UniversalAdditionalLightData>();
        }
        private void CreateComposite(Transform parent)
        {
            var composite = Child("Native 3D map composite", parent, GameplayRenderLayers.WorldLayer);
            compositeMesh = new Mesh { name = "Native map quad", hideFlags = HideFlags.DontSave };
            compositeMesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
            compositeMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            compositeMesh.triangles = new[] { 0,2,1,0,3,2 }; compositeMesh.RecalculateBounds();
            composite.AddComponent<MeshFilter>().sharedMesh = compositeMesh;
            CompositeRenderer = composite.AddComponent<MeshRenderer>(); CompositeRenderer.sharedMaterial = compositeMaterial;
            CompositeRenderer.sortingOrder = 3; CompositeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            CompositeRenderer.receiveShadows = false;
        }
        /// <summary>Mirrors a borrowed orthographic XY camera. Hidden surfaces
        /// retain reusable resources; invalid or null sources fail closed.</summary>
        public void Sync(Camera borrowedSource, bool visible, bool lowDetail)
        {
            if (disposed) return;
            bool valid = visible && borrowedSource != null && borrowedSource.orthographic
                && ContentRoot != null && WorldCamera != null && CompositeRenderer != null;
            Rect viewport = valid ? borrowedSource.pixelRect : default;
            valid &= viewport.width >= 1 && viewport.height >= 1;
            if (valid)
            {
                var p = borrowedSource.transform.position;
                valid = Village3DProjection.Finite(borrowedSource.orthographicSize) && borrowedSource.orthographicSize > 0
                    && Village3DProjection.Finite(borrowedSource.aspect) && borrowedSource.aspect > 0
                    && Village3DProjection.Finite(p.x) && Village3DProjection.Finite(p.y);
            }
            if (!valid) { SetVisible(false); return; }
            float resolution = lowDetail ? .75f : 1f;
            int width = Mathf.Max(1, Mathf.RoundToInt(viewport.width * resolution));
            int height = Mathf.Max(1, Mathf.RoundToInt(viewport.height * resolution));
            if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            { SetVisible(false); return; }
            if (target == null || !target.IsCreated() || target.width != width || target.height != height)
            {
                ReleaseTarget();
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                { name = "Native map texture", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, antiAliasing = 1 };
                if (!target.Create()) { ReleaseTarget(); SetVisible(false); return; }
                WorldCamera.targetTexture = target; compositeMaterial.SetTexture("_MainTex", target);
            }
            Sun.shadows = lowDetail ? LightShadows.None : LightShadows.Soft;
            WorldCamera.orthographicSize = borrowedSource.orthographicSize; WorldCamera.aspect = borrowedSource.aspect;
            WorldCamera.depth = borrowedSource.depth - 1; WorldCamera.rect = new Rect(0, 0, 1, 1);
            Vector3 position = borrowedSource.transform.position;
            WorldCamera.transform.SetPositionAndRotation(
                new Vector3(position.x, CameraAltitude, position.y - CameraAltitude * PitchCot),
                Quaternion.Euler(CameraPitchDegrees, 0, 0));
            // Rebuild from camera parameters each frame: compensating the old
            // custom matrix repeatedly would accumulate scale and skew the grid.
            WorldCamera.ResetProjectionMatrix();
            var projection = WorldCamera.projectionMatrix;
            projection.m11 /= PitchSin;
            WorldCamera.projectionMatrix = projection;
            CompositeRenderer.transform.position = new Vector3(position.x, position.y, CompositeDepth);
            CompositeRenderer.transform.localScale = new Vector3(borrowedSource.orthographicSize * borrowedSource.aspect * 2, borrowedSource.orthographicSize * 2, 1);
            SetVisible(true);
        }
        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (ContentRoot != null) ContentRoot.gameObject.SetActive(visible);
            if (CompositeRenderer != null) CompositeRenderer.gameObject.SetActive(visible);
            if (WorldCamera != null) WorldCamera.enabled = visible;
        }
        private void ReleaseTarget()
        {
            if (WorldCamera != null) WorldCamera.targetTexture = null;
            if (compositeMaterial != null) compositeMaterial.SetTexture("_MainTex", null);
            if (target != null) { target.Release(); DestroyOwned(target); target = null; }
        }
        /// <summary>Releases owned resources, including after external parent
        /// destruction. Borrowed parents, cameras and material assets survive.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; SetVisible(false); ReleaseTarget();
            if (WorldCamera != null) DestroyOwned(WorldCamera.gameObject);
            if (ContentRoot != null) DestroyOwned(ContentRoot.gameObject);
            if (CompositeRenderer != null) DestroyOwned(CompositeRenderer.gameObject);
            DestroyOwned(compositeMesh); DestroyOwned(FogTexture); DestroyOwned(compositeMaterial);
            foreach (var material in ownedMaterials) DestroyOwned(material);
            ownedMaterials.Clear(); families.Clear();
            ContentRoot = null; WorldCamera = null; CompositeRenderer = null; Sun = null;
            compositeMesh = null; FogTexture = null; compositeMaterial = null;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
