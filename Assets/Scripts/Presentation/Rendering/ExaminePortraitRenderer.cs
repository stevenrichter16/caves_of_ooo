using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>A borrowed image for the current popup. Its lifetime ends when
    /// the renderer clears or captures another subject. UVs also support an
    /// honest current sprite fallback without copying a texture atlas.</summary>
    public readonly struct ExaminePortrait
    {
        public readonly Texture Texture;
        public readonly Rect Uv;
        public readonly Color Tint;
        public readonly bool IsModel;
        public ExaminePortrait(Texture texture, Rect uv, Color tint, bool isModel)
        { Texture = texture; Uv = uv; Tint = tint; IsModel = isModel; }
    }

    /// <summary>One on-demand, static view of the actual world art. Call on
    /// authorized popup open/refresh, never per frame. No gameplay parts or
    /// combined terrain patches are copied; native meshes remain borrowed.</summary>
    public sealed class ExaminePortraitRenderer : IDisposable
    {
        public const int PortraitLayer = 30;
        public const int PortraitSize = 192;
        readonly Action<Camera, RenderTexture> capture;
        readonly List<Material> ownedMaterials = new List<Material>();
        GameObject stage;
        Texture2D fog;
        bool disposed;
        int currentTurns;
        ExaminePortrait current;
        public string CurrentModelId { get; private set; }
        internal RenderTexture OwnedTarget { get; private set; }
        internal GameObject PreviewRoot { get; private set; }
        internal Camera PreviewCamera { get; private set; }
        internal GameObject CurrentSourcePrefab { get; private set; }
        internal int CaptureCount { get; private set; }
        public ExaminePortraitRenderer() : this(Capture) { }
        internal ExaminePortraitRenderer(Action<Camera, RenderTexture> capture)
        { this.capture = capture ?? throw new ArgumentNullException(nameof(capture)); }

        /// <summary>
        /// Uses the explicitly bound world presentation. Active flat mode owns
        /// both successful and rejected results, so it can never fall through to
        /// an old model. Native sprites and textures remain world-catalog owned.
        /// </summary>
        public ExaminePortrait GetPortrait(AnnouncementDocument document, ZoneRenderer presentation)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ExaminePortraitRenderer));
            if (!IsCurrentPresentationSubject(document, presentation)) { Clear(); return default; }
            if (presentation != null && presentation.TryGetFlatExaminePortrait(document, out var portrait))
            {
                // DropModel alone retains the stage, camera and render target.
                // Release that entire previous preview before borrowing flat art.
                Clear();
                return portrait;
            }
            return GetPortrait(document);
        }

        /// <summary>
        /// Cheap disclosure/context validation for open and repaint. This never
        /// resolves art, creates a preview, simulates state or rebuilds prose.
        /// </summary>
        internal static bool IsCurrentPresentationSubject(AnnouncementDocument document, ZoneRenderer presentation)
        {
            if (!ExamineDocumentBuilder.IsCurrentSubject(document)) return false;
            if (presentation == null || !presentation.FlatPixelModeActive) return true;
            var zone = document.SubjectZone ?? CurrentPresentationZone(document.SubjectViewer);
            if (zone == null || !ReferenceEquals(zone, presentation.CurrentZone)) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || manager.CachedZones != null
                && manager.CachedZones.TryGetValue(zone.ZoneID, out var currentZone)
                && ReferenceEquals(currentZone, zone);
        }

        /// <summary>Open a current authorized document. Carried items borrow the
        /// owner's real presentation context without becoming world occupants.</summary>
        public ExaminePortrait GetPortrait(AnnouncementDocument document)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ExaminePortraitRenderer));
            if (!ExamineDocumentBuilder.IsCurrentSubject(document)) { DropModel(); return default; }
            if (document.SubjectZone != null) return GetPortrait(document.SubjectZone, document.Subject);
            return GetPortrait(CurrentPresentationZone(document.SubjectViewer), document.Subject, true);
        }

        public ExaminePortrait GetPortrait(Zone zone, Entity target) => GetPortrait(zone, target, false);

        ExaminePortrait GetPortrait(Zone zone, Entity target, bool carried)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ExaminePortraitRenderer));
            var render = target?.GetPart<RenderPart>();
            if (render == null || !render.Visible || render.ParentEntity != target)
            { DropModel(); return default; }
            if (zone != null && !carried)
            {
                bool visible = false;
                foreach (var cell in zone.GetOccupiedCells(target))
                    if (cell != null && cell.Explored && cell.IsVisible && cell.Occupants.Contains(target)) { visible = true; break; }
                if (!visible) { DropModel(); return default; }
            }

            var library = zone == null ? null : Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            SpawnRing3DRecipe recipe = default;
            GameObject source = null;
            if (library != null && library.Definition.SupportsZone(zone) && AreaCompositionScope.Allows(zone))
            {
                var pilot = target.HasPart<MultiCellPilotPropPart>() ? Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath) : null;
                recipe = carried
                    ? new SpawnRing3DRecipe(target, PortableModel(zone, target), null, Vector3.zero, true, false)
                    : SpawnRing3DRecipes.Resolve(zone, target, library.Definition, pilot?.Definition);
                if (recipe.ModelId != null && recipe.Failure == null)
                    source = pilot != null ? pilot.FindModel(recipe.ModelId)
                        : (SoddenPresentationScope.IsActive(zone) ? SoddenNativeArtLibrary.Load()?.Find(recipe.ModelId)?.Prefab : null)
                        ?? (SpreadPresentationScope.IsActive(zone) ? SpreadNativeStyle3DLibrary.Load()?.ForOwner(zone, recipe)?.Prefab : null)
                        ?? library.FindModel(recipe.ModelId);
            }
            if (source == null) { DropModel(); return Fallback(target, render); }
            if (source == CurrentSourcePrefab && recipe.ModelId == CurrentModelId && recipe.QuarterTurns == currentTurns
                && OwnedTarget != null && OwnedTarget.IsCreated()) return current;
            DropModel();
            try
            {
                EnsureResources(library);
                PreviewRoot = Object.Instantiate(source, stage.transform, false);
                PreviewRoot.name = "Examine subject " + recipe.ModelId;
                // Parent stays inactive until all presentation-only preparation
                // is complete, preventing accidental prefab behaviours running.
                foreach (var behaviour in PreviewRoot.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                foreach (var collider in PreviewRoot.GetComponentsInChildren<Collider>(true)) DestroyOwned(collider);
                foreach (var script in PreviewRoot.GetComponentsInChildren<MonoBehaviour>(true)) DestroyOwned(script);
                if (VoxelWorldPresentation.IsEnabledFor(zone)) Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath)?.Apply(PreviewRoot);
                if (ReferenceGladePlan.IsActive(zone) || SpreadPresentationScope.IsActive(zone) || SoddenPresentationScope.IsActive(zone))
                {
                    ReferenceGladeVoxelLibrary.Load()?.ApplyActorPaint(PreviewRoot);
                    SpreadVisitorPaintLibrary.Load()?.Apply(PreviewRoot, recipe.ModelId);
                }
                PreviewRoot.transform.localRotation = Quaternion.Euler(0, recipe.QuarterTurns * 90, 0) * source.transform.localRotation;
                // Flat art shares the actual world source but faces this capture's
                // camera; disabled prefab behaviours cannot perform billboarding.
                if (SeedKeeperFlatLibrary.IsModelId(recipe.ModelId) && PreviewRoot.GetComponent<MeshFilter>() != null)
                    PreviewRoot.transform.rotation = Quaternion.Euler(32, -35, 0);
                PrepareMaterials(); Frame();
                stage.SetActive(true);
                var previousTarget = RenderTexture.active;
                try { capture(PreviewCamera, OwnedTarget); CaptureCount++; }
                finally { RenderTexture.active = previousTarget; stage.SetActive(false); }
                CurrentSourcePrefab = source; CurrentModelId = recipe.ModelId; currentTurns = recipe.QuarterTurns;
                current = new ExaminePortrait(OwnedTarget, new Rect(0, 0, 1, 1), Color.white, true);
                return current;
            }
            catch (InvalidOperationException error)
            {
                DropModel();
                Debug.LogWarning("[ExaminePortrait] " + error.Message);
                return Fallback(target, render);
            }
        }

        static Zone CurrentPresentationZone(Entity viewer)
        {
            var zone = viewer?.SpatialZone; if (zone == null) return null;
            var cell = zone.GetEntityCell(viewer); var physical = viewer.GetPart<PhysicsPart>();
            if (cell?.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell || !cell.Objects.Contains(viewer)
                || physical?.ParentEntity != viewer || physical.InInventory != null || physical.Equipped != null) return null;
            var manager = WorldLocationContext.For(zone);
            return manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone ? zone : null;
        }

        static string PortableModel(Zone zone, Entity target)
        {
            if (target.HasTag("Creature") || target.HasTag("Natural") || target.HasPart<SpatialFootprintPart>()
                || target.HasPart<MultiCellPilotPropPart>() || target.HasPart<FellingScenePropPart>()) return null;
            string model = RepairCultivationRecipes.PortableModel(target) ?? BiomeCropRecipes.PortableModel(target);
            if (model != null) return model;
            return (SpreadPresentationScope.IsActive(zone) || SoddenPresentationScope.IsActive(zone))
                && SpreadPortableWorldRecipes.ValidConstructedQuest(target) && SpreadPortableRecipes.TryRecipe(target, out model) ? model : null;
        }

        static ExaminePortrait Fallback(Entity target, RenderPart render)
        {
            if (string.IsNullOrEmpty(render.RenderString)) return default;
            Sprite sprite;
            if (EntityVisualCatalog.TryGetAsset(target, out var asset))
                sprite = asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.South, 0);
            else
            {
                // The world bank rebuilds on an unknown Unicode codepoint.
                // Its existing '?' is the world's honest unmapped fallback.
                char glyph = render.RenderString[0];
                sprite = CP437TilesetGenerator.GetTile(glyph <= 255 ? glyph : '?').sprite;
            }
            if (sprite == null || sprite.texture == null) return default;
            var texture = sprite.texture; var r = sprite.rect;
            return new ExaminePortrait(texture, new Rect(r.x / texture.width, r.y / texture.height,
                r.width / texture.width, r.height / texture.height), QudColorParser.Parse(render.ColorString), false);
        }

        void EnsureResources(SpawnRing3DLibrary library)
        {
            if (stage == null)
            {
                stage = new GameObject("Examine portrait stage") { hideFlags = HideFlags.HideAndDontSave, layer = PortraitLayer };
                stage.SetActive(false); stage.transform.position = new Vector3(40, 0, 12);
                var cameraObject = new GameObject("Examine portrait camera") { hideFlags = HideFlags.HideAndDontSave, layer = PortraitLayer };
                cameraObject.transform.SetParent(stage.transform, false);
                PreviewCamera = cameraObject.AddComponent<Camera>(); PreviewCamera.enabled = false;
                PreviewCamera.orthographic = true; PreviewCamera.aspect = 1;
                PreviewCamera.cullingMask = 1 << PortraitLayer; PreviewCamera.clearFlags = CameraClearFlags.SolidColor;
                PreviewCamera.backgroundColor = Color.clear; PreviewCamera.allowHDR = false; PreviewCamera.allowMSAA = false;
                PreviewCamera.useOcclusionCulling = false;
                var data = PreviewCamera.GetUniversalAdditionalCameraData(); data.SetRenderer(library.RendererIndex);
                data.renderPostProcessing = false; data.renderShadows = false;
                data.requiresDepthOption = CameraOverrideOption.Off; data.requiresColorOption = CameraOverrideOption.Off;
                fog = new Texture2D(80, 25, TextureFormat.RGBA32, false, true)
                    { name = "Examine owned visibility", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[80 * 25]; for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                fog.SetPixels32(pixels); fog.Apply(false, false);
            }
            if (OwnedTarget == null)
                OwnedTarget = new RenderTexture(PortraitSize, PortraitSize, 24, RenderTextureFormat.ARGB32)
                    { name = "Examine reusable portrait", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, antiAliasing = 1 };
            if (!OwnedTarget.IsCreated()) OwnedTarget.Create();
            PreviewCamera.targetTexture = OwnedTarget;
        }

        void PrepareMaterials()
        {
            var borrowed = new Dictionary<Material, Material>();
            var probe = new SphericalHarmonicsL2(); probe.AddAmbientLight(new Color(.48f, .48f, .48f));
            probe.AddDirectionalLight(new Vector3(-.4f, .8f, -.5f).normalized, new Color(1, .95f, .82f), .65f);
            var block = new MaterialPropertyBlock(); block.CopySHCoefficientArraysFrom(new[] { probe });
            foreach (var transform in PreviewRoot.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = PortraitLayer;
            foreach (var renderer in PreviewRoot.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i]; if (source == null) continue;
                    if (!borrowed.TryGetValue(source, out var owned))
                    {
                        owned = new Material(source) { hideFlags = HideFlags.HideAndDontSave }; ownedMaterials.Add(owned); borrowed.Add(source, owned);
                        if (owned.HasProperty("_FogLight")) owned.SetTexture("_FogLight", fog);
                        Set(owned, "_Transient", 0); Set(owned, "_Exposure", 1.15f); Set(owned, "_AmbientStrength", 1);
                        Set(owned, "_SunStrength", 0); Set(owned, "_GroundContactStrength", 0); Set(owned, "_GroundMottleStrength", 0); Set(owned, "_WaveStrength", 0);
                    }
                    materials[i] = owned;
                }
                renderer.sharedMaterials = materials; renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false; renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                renderer.SetPropertyBlock(block);
            }
        }
        static void Set(Material material, string property, float value) { if (material.HasProperty(property)) material.SetFloat(property, value); }
        void Frame()
        {
            var renderers = PreviewRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("The selected world model has no renderer.");
            var bounds = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float radius = Mathf.Max(.1f, bounds.extents.magnitude);
            var rotation = Quaternion.Euler(32, -35, 0); var direction = rotation * Vector3.forward;
            PreviewCamera.transform.SetPositionAndRotation(bounds.center - direction * (radius * 3 + 1), rotation);
            PreviewCamera.orthographicSize = radius * 1.12f; PreviewCamera.nearClipPlane = .01f; PreviewCamera.farClipPlane = radius * 6 + 3;
        }
        static void Capture(Camera camera, RenderTexture target)
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("URP portrait capture unavailable; using the current sprite.");
            RenderPipeline.SubmitRenderRequest(camera, request);
        }
        void DropModel()
        {
            if (stage != null) stage.SetActive(false);
            DestroyOwned(PreviewRoot); PreviewRoot = null; CurrentSourcePrefab = null; CurrentModelId = null; current = default;
            foreach (var material in ownedMaterials) DestroyOwned(material); ownedMaterials.Clear();
        }
        public void Clear()
        {
            DropModel();
            if (PreviewCamera != null) PreviewCamera.targetTexture = null;
            if (OwnedTarget != null) { OwnedTarget.Release(); DestroyOwned(OwnedTarget); OwnedTarget = null; }
            DestroyOwned(stage); stage = null; PreviewCamera = null; DestroyOwned(fog); fog = null;
        }
        public void Dispose() { if (disposed) return; Clear(); disposed = true; }
        static void DestroyOwned(Object value) { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
