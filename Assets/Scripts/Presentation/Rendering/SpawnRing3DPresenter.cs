using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only presentation of the eight native surface neighbours of
    /// Morrowfast and map-authorized Spread surfaces/lair floors. Simulation membership, collision, equipment and water remain
    /// owned by the native zone. Unknown content retains its native fallback.</summary>
    [ExecuteAlways, DefaultExecutionOrder(900)]
    public sealed class SpawnRing3DPresenter : MonoBehaviour
    {
        private sealed class View
        {
            public Entity Owner;
            public string ModelId;
            public int AuthoredQuarterTurns;
            public GameObject Root, SourcePrefab;
            public Renderer[] Renderers;
            public Collider[] Colliders;
            public Animator Animator;
            public NativeSpellCastPlayer Cast;
            public NativeQuestCueViews.Handle QuestCue;
            public SpreadCollectorCarryView CollectorCarry;
            public bool Transient, Drawn, AuthoredIdleFacing;
            public Vector3 Target, Start;
            public float MoveStart, MoveDuration, ActionUntil;
            // Last resolved ordinary action state, consumed directly by Play; ActionUntil owns its active lifetime.
            public string ActionState;
            public string Clip;
        }
        private readonly Dictionary<Entity, SpawnRing3DRecipe> recipes = new Dictionary<Entity, SpawnRing3DRecipe>();
        private readonly Dictionary<Entity, View> views = new Dictionary<Entity, View>();
        private readonly Dictionary<Entity,bool> staticStyles = new Dictionary<Entity,bool>();
        private readonly Dictionary<Collider, View> byCollider = new Dictionary<Collider, View>();
        private readonly HashSet<Entity> seen = new HashSet<Entity>();
        private readonly List<Entity> removed = new List<Entity>();
        private RaycastHit[] hits = new RaycastHit[64];
        private SpawnRing3DLibrary library;
        private SpreadPortable3DLibrary collectorPortables;
        private VoxelWorldPresentation voxel;
        private MultiCellPilot3DLibrary pilotLibrary;
        private ReferenceGladeVoxelLibrary gladeLibrary;
        private SpreadVisitorPaintLibrary visitorPaintLibrary;
        private SpreadVisitorCreatureLibrary visitorCreatureLibrary;
        private SpreadBiomeHumanoidLibrary humanoidLibrary;
        private SpreadNativeStyle3DLibrary nativeStyleLibrary;
        private SpreadBiomeStyleCatalog approvedStyle;
        private MaterialPropertyBlock styleProperties;
        private readonly List<Material> styleMaterials=new List<Material>(4);
        private SpawnRing3DCatalog definition;
        private NativeZone3DRenderSurface surface;
        private NativeSpellFxLibrary spellLibrary;
        private SpawnRing3DGroundPatches ground;
        private ReferenceGladeGroundContact groundContact;
        private Village3DEquipmentViews equipment;
        private NativeQuestCueViews questCues;
        private SpreadTransientVolumes transientVolumes;
        private Camera source;
        private bool requestedVisible = true, hooks;
        private bool boundReferenceGlade, boundSpreadStyle, boundOrdinaryCave;
        public Zone CurrentZone { get; private set; }
        public Camera WorldCamera => surface?.WorldCamera;
        public bool IsReady { get; private set; }
        public string Failure { get; private set; }
        public bool VoxelPresentationActive => PresentationVisible && voxel != null;
        public int VoxelAppliedMeshCount => voxel?.AppliedMeshCount ?? 0;
        public int VoxelMissingMeshCount => voxel?.MissingMeshCount ?? 0;
        public bool FullReveal { get; set; }
        // Model choice, ground palette and actor scale are captured at Bind. A
        // managed map change must rebuild that owned presentation as one unit.
        private bool GladeAuthorityMatches => CurrentZone == null
            || (boundReferenceGlade == ReferenceGladePlan.IsActive(CurrentZone)
                && boundSpreadStyle == SpreadPresentationScope.IsActive(CurrentZone)
                && boundOrdinaryCave == BiomeCropRecipes.IsOrdinaryCave(CurrentZone));
        private bool PresentationRequested => GladeAuthorityMatches && IsReady && requestedVisible && Village3DSettings.Enabled && source != null && isActiveAndEnabled && AreaCompositionScope.Allows(CurrentZone);
        public bool PresentationVisible => PresentationRequested && surface != null && surface.IsVisible;
        public NativeZone3DRenderSurface ActiveSurface => PresentationVisible ? surface : null;
        public int GroundBuildCount => ground?.GroundBuildCount ?? 0;
        public int GroundPatchCount => ground?.PatchCount ?? 0;
        public int GroundPatchRevision(int x, int y) => ground?.Revision(x, y) ?? 0;
        public IReadOnlyList<Village3DEquipmentFallback> EquipmentFallbacks => equipment?.Fallbacks ?? Array.Empty<Village3DEquipmentFallback>();

        public void Bind(Zone zone, Camera sourceCamera)
        {
            bool referenceGlade = ReferenceGladePlan.IsActive(zone);
            bool spreadStyle = SpreadPresentationScope.IsActive(zone);
            bool ordinaryCave = BiomeCropRecipes.IsOrdinaryCave(zone);
            if(zone!=null&&!AreaCompositionScope.Allows(zone))
            {Release();CurrentZone=zone;source=sourceCamera;boundReferenceGlade=referenceGlade;boundSpreadStyle=spreadStyle;boundOrdinaryCave=ordinaryCave;return;}
            if (ReferenceEquals(CurrentZone, zone) && boundReferenceGlade == referenceGlade && boundSpreadStyle == spreadStyle && boundOrdinaryCave == ordinaryCave && (IsReady || Failure != null))
            { source = sourceCamera; SyncCamera(); return; }
            Release(); CurrentZone = zone; source = sourceCamera; boundReferenceGlade = referenceGlade; boundSpreadStyle = spreadStyle; boundOrdinaryCave = ordinaryCave;
            if (!Village3DSettings.Enabled || zone == null || source == null || !(spreadStyle || ordinaryCave || SupportsZone(zone.ZoneID))) return;
            if (zone.ZoneID == FellingSiteBuilder.ZoneID && !FellingSceneRuntime.IsActive(zone)) return;
            try
            {
                library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                if (library == null) throw new InvalidOperationException("Spawn-ring 3D library is unavailable.");
                library.Validate(); definition = library.Definition;
                voxel = VoxelWorldPresentation.ForZone(zone);
                // Optional in ordinary ring chunks, required at the authored site.
                // Loading once at bind also covers owners arriving after this bind.
                pilotLibrary = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
                if (pilotLibrary == null && MultiCellPilotRuntime.IsActive(zone))
                    throw new InvalidOperationException("Multi-cell pilot 3D library is unavailable.");
                pilotLibrary?.Validate();
                var materials = new List<Material> { library.WorldMaterial, library.WaterMaterial,
                    library.EquipmentLibrary.WorldMaterial, library.EquipmentLibrary.WaterMaterial };
                if (pilotLibrary != null) { materials.Add(pilotLibrary.WorldMaterial); materials.Add(pilotLibrary.TarMaterial); materials.Add(pilotLibrary.GroundMaterial); }
                var poured = PouredLiquid3DLibrary.Load();
                if (poured == null) throw new InvalidOperationException("Poured liquid color library is unavailable.");
                poured.Validate(); materials.AddRange(poured.Materials);
                if (spreadStyle)
                {
                    var portable = SpreadPortable3DLibrary.Load();
                    if (portable == null) throw new InvalidOperationException("Spread portable library is unavailable.");
                    portable.Validate(); collectorPortables=portable; materials.Add(portable.Material);
                }
                // New portable crops and structural repairs retain their palette
                // in other supported biomes without changing regional lighting.
                var botany=BiomeCrop3DLibrary.Load();
                if(botany!=null){botany.Validate();materials.Add(botany.Material);}
                var cultivation=RepairCultivation3DLibrary.Load();
                ReferenceGladeVoxelLibrary cultivationPalette=null;
                if(cultivation!=null){cultivation.Validate();cultivationPalette=ReferenceGladeVoxelLibrary.Load();materials.Add(cultivation.Material);}
                ReferenceGladeVoxelLibrary glade=null;
                if(referenceGlade || spreadStyle)
                {
                    glade=ReferenceGladeVoxelLibrary.Load();if(glade==null)throw new InvalidOperationException("Reference glade kit is unavailable.");
                    glade.Validate();gladeLibrary=glade;materials.Add(glade.Material);
                    if(spreadStyle){humanoidLibrary=SpreadBiomeHumanoidLibrary.Load();if(humanoidLibrary==null)throw new InvalidOperationException("Scoped humanoid library missing.");humanoidLibrary.Validate();}
                }
                if(spreadStyle)
                {
                    // Unity native handles must be created on this main-thread
                    // bind, never in a MonoBehaviour constructor/field initializer.
                    visitorPaintLibrary=SpreadVisitorPaintLibrary.Load();
                    if(visitorPaintLibrary==null)throw new InvalidOperationException("Scoped existing visitor palette library missing.");
                    visitorPaintLibrary.Validate();
                    nativeStyleLibrary=SpreadNativeStyle3DLibrary.Load();if(nativeStyleLibrary==null)throw new InvalidOperationException("Native static style library missing.");nativeStyleLibrary.EnsureReady();
                    visitorCreatureLibrary=SpreadVisitorCreatureLibrary.Load();
                    if(visitorCreatureLibrary==null)throw new InvalidOperationException("Original visitor library missing.");
                    visitorCreatureLibrary.Validate();materials.Add(visitorCreatureLibrary.Material);
                    styleProperties=new MaterialPropertyBlock();
                    approvedStyle=new SpreadBiomeStyleCatalog(glade,SpreadBiomeActorLibrary.Load(),humanoidLibrary,SpreadPortable3DLibrary.Load(),poured,SpreadScenery3DLibrary.Load(),SpreadCreature3DLibrary.Load(),SpreadEnvironment3DLibrary.Load(),visitorPaintLibrary,nativeStyleLibrary,visitorCreatureLibrary);
                }
                surface = new NativeZone3DRenderSurface(transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, materials.ToArray(), 2.2f);
                if (glade != null)
                {
                    surface.ConfigureLighting(1.45f, .65f, .9f, new Color(1f, .98f, .92f),
                        new Vector3(55, -145, 0), .68f, .008f, .018f, 18f, FilterMode.Bilinear, useLocalShadowBias: true);
                    surface.ConfigureAmbientProbe(new Color(.28f, .32f, .30f));
                    // Borrowed source assets and all ordinary palettes remain at
                    // the shader's zero default. Only this owned glade clone varies.
                    surface.MaterialFor(glade.Material).SetFloat("_GroundMottleStrength", .24f);
                }
                questCues = new NativeQuestCueViews(surface, library.WorldMaterial, 16);
                ground = new SpawnRing3DGroundPatches(surface, library, pilotLibrary, voxel, glade??cultivationPalette,nativeStyleLibrary);
                if (glade != null && (referenceGlade || spreadStyle)) groundContact = new ReferenceGladeGroundContact(glade,surface.MaterialFor(glade.Material),spreadStyle?SpreadEnvironment3DLibrary.Load():null);
                if(spreadStyle)transientVolumes=new SpreadTransientVolumes(surface,library.WorldMaterial,(x,y)=>ground.HasWater(x,y));
                equipment = new Village3DEquipmentViews(library.EquipmentLibrary, go => PrepareModel(go, true), spreadStyle);
                IsReady = true; Subscribe(); Refresh(null); SyncCamera();
            }
            catch (Exception e)
            {
                Release(); CurrentZone = zone; source = sourceCamera; Failure = e.Message;
                Debug.LogWarning("[SpawnRing3D] Native presentation retained: " + Failure);
            }
        }
        private static bool SupportsZone(string id)
        {
            switch (id)
            {
                case "Overworld.2.5.0": case "Overworld.3.5.0": case "Overworld.4.5.0":
                case "Overworld.2.6.0": case "Overworld.4.6.0": case "Overworld.2.7.0":
                case "Overworld.3.7.0": case "Overworld.4.7.0": return true;
                default: return (GrovelandsCompositionPlan.IsWildernessZone(id) || SpreadCompositionPlan.IsWildernessZone(id) || SoddenCompositionPlan.IsWildernessZone(id) || BeatingCompositionPlan.IsWildernessZone(id) || StumpCompositionPlan.IsWildernessZone(id) || OverwritCompositionPlan.IsWildernessZone(id) || GinmereCompositionPlan.IsSupportedZone(id) || CathedralCompositionPlan.IsSupportedZone(id) || StillleafCompositionPlan.IsSupportedZone(id) || OlderdeepCompositionPlan.IsSupportedZone(id) || WellmeetCompositionPlan.IsSupportedZone(id) || CinderholdCompositionPlan.IsSupportedZone(id) || SumpholdCompositionPlan.IsSupportedZone(id) || DrownedLedgerCompositionPlan.IsSupportedZone(id) || MarrowstyeCompositionPlan.IsSupportedZone(id) || FirstTentCompositionPlan.IsSupportedZone(id) || LastCounterCompositionPlan.IsSupportedZone(id) || GantryCompositionPlan.IsSupportedZone(id) || TineCompositionPlan.IsSupportedZone(id) || QuillholdCompositionPlan.IsSupportedZone(id) || TallyCompositionPlan.IsSupportedZone(id));
            }
        }
        /// <summary>Refresh native references after cell/FOV invalidation. Null
        /// dirtyCells checks the complete ground fingerprint; no seed is replayed.</summary>
        public void Refresh(LightMap light, HashSet<int> dirtyCells = null)
        {
            using (PerformanceMarkers.Zone.NativeRefresh.Auto())
            {
            if (CurrentZone != null && source != null && !GladeAuthorityMatches)
                Bind(CurrentZone, source);
            if (!IsReady || CurrentZone == null) return;
            if(!AreaCompositionScope.Allows(CurrentZone))
            {var zone=CurrentZone;var camera=source;Release();CurrentZone=zone;source=camera;return;}
            try { RefreshCurrent(light, dirtyCells); }
            catch (Exception e)
            {
                // Later native changes can request a newly unavailable model.
                // Release the whole owned surface, including unregistered partial
                // instances, before restoring the native fallback for this graph.
                var zone = CurrentZone; var camera = source;
                Release(); CurrentZone = zone; source = camera; Failure = e.Message;
                Debug.LogWarning("[SpawnRing3D] Native presentation retained: " + Failure);
            }
            }
        }
        private void RefreshCurrent(LightMap light, HashSet<int> dirtyCells)
        {
            seen.Clear(); removed.Clear();
            foreach (var entity in CurrentZone.GetReadOnlyEntities())
            {
                var recipe = SpawnRing3DRecipes.Resolve(CurrentZone, entity, definition, pilotLibrary?.Definition);
                if (recipe.ModelId == null) continue;
                seen.Add(entity);
                if(nativeStyleLibrary!=null){bool styled=nativeStyleLibrary.ForOwner(CurrentZone,recipe)!=null;
                    if(!staticStyles.TryGetValue(entity,out bool previousStyle)||previousStyle!=styled)Mark(recipe);staticStyles[entity]=styled;}
                if (recipes.TryGetValue(entity, out var previous) && !SameGeometry(previous, recipe))
                { Mark(previous); Mark(recipe); }
                else if (!recipes.ContainsKey(entity)) Mark(recipe);
                recipes[entity] = recipe;
                if (recipe.Batched)
                { if (views.TryGetValue(entity, out var old)) RemoveView(old); continue; }
                if (!views.TryGetValue(entity, out var view) || view.ModelId != recipe.ModelId || view.SourcePrefab != PrefabFor(recipe))
                { if (view != null) RemoveView(view); view = AddView(recipe); }
                // Apply an authored rest-facing only when that recipe changes;
                // ordinary movement, combat and cast facing retain their pose.
                if(view.AuthoredQuarterTurns!=recipe.QuarterTurns)
                {view.Root.transform.localRotation=Quaternion.Euler(0,recipe.QuarterTurns*90,0);view.AuthoredQuarterTurns=recipe.QuarterTurns;}
                var cell = CurrentZone.GetEntityCell(entity);
                if (view.Target != recipe.Position || !Application.isPlaying || !PresentationVisible || view.MoveDuration <= 0)
                { view.Target = recipe.Position; view.Root.transform.position = recipe.Position; view.MoveDuration = 0; }
                bool drawn = recipe.Transient ? AnyBodyKnown(entity, visibleOnly:true) : AnyRemembered(view, cell);
                SetDrawn(view, drawn);
                SyncQuestCue(view);
                SyncCollectorCarry(view);
                if (drawn && view.Cast?.IsActive != true && view.MoveDuration <= 0 && Time.unscaledTime >= view.ActionUntil) Play(view, "Idle");
            }
            foreach (var pair in recipes) if (!seen.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var entity in removed)
            {
                Mark(recipes[entity]); recipes.Remove(entity); staticStyles.Remove(entity);
                if (views.TryGetValue(entity, out var view)) RemoveView(view);
            }
            ground.Refresh(CurrentZone, recipes, dirtyCells);
            groundContact?.Refresh(CurrentZone, recipes, FullReveal);
            surface.UpdateFog(CurrentZone, light, FullReveal);
            transientVolumes?.Refresh(CurrentZone);
            RefreshEquipment(true); SyncCamera();
        }
        private static bool SameGeometry(SpawnRing3DRecipe a, SpawnRing3DRecipe b)
            => a.ModelId == b.ModelId && a.Position == b.Position && a.Batched == b.Batched && a.QuarterTurns == b.QuarterTurns;
        private void Mark(SpawnRing3DRecipe recipe)
        {
            if (recipe.Batched && Village3DProjection.TryWorldToCell(recipe.Position, out int x, out int y)) ground.Mark(x, y);
        }
        private GameObject PrefabFor(SpawnRing3DRecipe recipe)
            => recipe.Owner.HasPart<MultiCellPilotPropPart>() ? pilotLibrary?.FindModel(recipe.ModelId)
                : nativeStyleLibrary?.ForOwner(CurrentZone,recipe)?.Prefab ?? library.FindModel(recipe.ModelId);
        private View AddView(SpawnRing3DRecipe recipe)
        {
            var prefab = PrefabFor(recipe);
            if (prefab == null) throw new InvalidOperationException("Missing ring model " + recipe.ModelId);
            var root = Instantiate(prefab, surface.ContentRoot, false); root.name = recipe.ComponentId ?? recipe.ModelId;
            root.transform.position = recipe.Position;
            if(recipe.QuarterTurns!=0)root.transform.localRotation=Quaternion.Euler(0,recipe.QuarterTurns*90,0);
            PrepareModel(root, recipe.Transient, recipe.ModelId);
            if ((boundReferenceGlade || boundSpreadStyle) && (recipe.Owner.HasTag("Creature") || recipe.Owner.HasTag("Player")))
            {
                // Measure visible geometry for the three authored body forms.
                // Their wider animation/culling envelope is preserved separately.
                // Scaling the root keeps animation, equipment and picking together.
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var bounds = PresentationBounds(renderers[0]);
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(PresentationBounds(renderers[i]));
                    bool marlback = recipe.Owner.BlueprintName?.StartsWith("Marlback", StringComparison.Ordinal) == true
                        || SpreadRareMarlbackLibrary.IsBlueprint(recipe.Owner.BlueprintName);
                    bool inspectionHumanoid = recipe.ModelId == "ring-player" || recipe.ModelId == "ring-sien" || recipe.ModelId == "ring-nam" || humanoidLibrary?.Find(recipe.ModelId)!=null;
                    // Native screenshot review measures these exact three rigs
                    // against the existing compensated camera. Other creatures
                    // retain their smaller profile, including the passive moth.
                    float maxHeight = marlback ? .90f : inspectionHumanoid ? 1.35f : .9f;
                    float maxWidth = marlback ? 1.02f : inspectionHumanoid ? 1.1f : .68f;
                    if(recipe.ModelId.StartsWith("spread-creature-",StringComparison.Ordinal))
                    {maxHeight=recipe.ModelId=="spread-creature-jungle-ape"?1.25f:.9f;maxWidth=recipe.ModelId=="spread-creature-giant-spider"?1.15f:1.12f;}
                    var visitor=SpreadVisitorCreatureSource.Find(recipe.ModelId);
                    if(visitor!=null){maxHeight=visitor.MaxHeight;maxWidth=visitor.MaxWidth;}
                    float scale = Mathf.Min(1f, Mathf.Min(maxHeight / Mathf.Max(.01f,bounds.size.y),
                        maxWidth / Mathf.Max(.01f,bounds.size.x)));
                    root.transform.localScale *= scale;
                }
            }
            var view = new View { Owner = recipe.Owner, ModelId = recipe.ModelId, Root = root, SourcePrefab = prefab, Target = recipe.Position,
                AuthoredQuarterTurns=recipe.QuarterTurns,
                AuthoredIdleFacing=recipe.ModelId.StartsWith("cathedral-elder-",StringComparison.Ordinal),
                Transient = recipe.Transient, Renderers = root.GetComponentsInChildren<Renderer>(true),
                Animator = root.GetComponentInChildren<Animator>(true) };
            if (view.Renderers.Length == 0) { DestroyOwned(root); throw new InvalidOperationException("Empty ring model " + recipe.ModelId); }
            // Irregular Felling owners use their exact mesh for selection, leaving
            // native empty scar cells available. These colliders have no rigidbody.
            if (recipe.ComponentId != null && view.Animator == null)
            {
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null) { var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; }
            }
            else
            {
                Bounds bounds = PresentationBounds(view.Renderers[0]);
                for (int i = 1; i < view.Renderers.Length; i++) bounds.Encapsulate(PresentationBounds(view.Renderers[i]));
                var collider = root.AddComponent<BoxCollider>(); collider.isTrigger = true;
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = Abs(root.transform.InverseTransformVector(new Vector3(bounds.size.x, 0, 0)))
                    + Abs(root.transform.InverseTransformVector(new Vector3(0, bounds.size.y, 0)))
                    + Abs(root.transform.InverseTransformVector(new Vector3(0, 0, bounds.size.z)));
            }
            view.Colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (var collider in view.Colliders) byCollider.Add(collider, view);
            if (view.Animator != null) { view.Animator.applyRootMotion = false; view.Animator.cullingMode = AnimatorCullingMode.CullCompletely; view.Cast = new NativeSpellCastPlayer(view.Animator); }
            if(recipe.ModelId==SpreadCollectorArtLibrary.Actor&&boundSpreadStyle)
                view.CollectorCarry=new SpreadCollectorCarryView(view.Owner,root,collectorPortables,go=>PrepareModel(go,true));
            views.Add(recipe.Owner, view); return view;
        }
        // Only the exact three glade body forms decouple visible proportions
        // and picking from the retained imported animation/culling envelope.
        // Borrowed meshes, bones, renderer.localBounds and other actors stay put.
        private Bounds PresentationBounds(Renderer renderer)
        {
            if(renderer is SkinnedMeshRenderer skin&&(gladeLibrary?.IsAuthoredHumanoidMesh(skin.sharedMesh)==true||humanoidLibrary?.ContainsMesh(skin.sharedMesh)==true||visitorCreatureLibrary?.ContainsMesh(skin.sharedMesh)==true))
            {
                var bounds=skin.sharedMesh.bounds;var matrix=skin.localToWorldMatrix;
                var extents=Abs(matrix.MultiplyVector(new Vector3(bounds.extents.x,0,0)))
                    +Abs(matrix.MultiplyVector(new Vector3(0,bounds.extents.y,0)))
                    +Abs(matrix.MultiplyVector(new Vector3(0,0,bounds.extents.z)));
                return new Bounds(matrix.MultiplyPoint3x4(bounds.center),extents*2);
            }
            return renderer.bounds;
        }
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        private void RemoveView(View view)
        {
            views.Remove(view.Owner); view.Cast?.Dispose(); view.CollectorCarry?.Dispose();
            foreach (var collider in view.Colliders) byCollider.Remove(collider);
            if (view.Root != null) { view.Root.SetActive(false); DestroyOwned(view.Root); }
        }
        private bool AnyBodyKnown(Entity owner, bool visibleOnly)
        {
            if (FullReveal) return true;
            foreach (var cell in CurrentZone.GetOccupiedCells(owner))
                if (cell != null && (visibleOnly ? cell.IsVisible : cell.Explored)) return true;
            return false;
        }
        private bool AnyRemembered(View view, Cell anchor)
        {
            if (view.Owner.HasPart<MultiCellPilotPropPart>()) return AnyBodyKnown(view.Owner, visibleOnly:false);
            if (FullReveal || anchor.Explored) return true;
            foreach (var renderer in view.Renderers)
            {
                Bounds b = renderer.bounds;
                for (int z = Mathf.Max(0, Mathf.FloorToInt(b.min.z)); z <= Mathf.Min(24, Mathf.FloorToInt(b.max.z)); z++)
                    for (int x = Mathf.Max(0, Mathf.FloorToInt(b.min.x)); x <= Mathf.Min(79, Mathf.FloorToInt(b.max.x)); x++)
                        if (CurrentZone.GetCell(x, 24-z).Explored) return true;
            }
            return false;
        }
        private static void SetDrawn(View view, bool drawn)
        {
            view.Drawn = drawn; view.Root.SetActive(drawn);
            if (!drawn) Interrupt(view);
        }
        private void RefreshEquipment(bool force)
        {
            if (equipment == null) return;
            equipment.BeginSync(force);
            foreach (var view in views.Values)
                if (view.Animator != null && CurrentZone.GetEntityCell(view.Owner) != null) equipment.Sync(view.Owner, view.Root);
            equipment.EndSync();
            RefreshHeadwearCover();
        }
        private void RefreshHeadwearCover()
        {
            if (!boundSpreadStyle || humanoidLibrary == null || styleProperties == null) return;
            foreach (var view in views.Values)
            {
                var adopted = humanoidLibrary.Find(view.ModelId);
                if (adopted == null) continue;
                bool covered = false;
                var inventory = view.Owner.GetPart<InventoryPart>();
                if (inventory != null && ReferenceEquals(inventory.ParentEntity, view.Owner))
                    foreach (var item in inventory.GetAllEquipped())
                        if (SpreadEquipmentRecipes.TryRecipe(view.Owner, item, out var recipe)
                            && recipe.Slot == "Head" && TryGetApprovedEquipmentStyle(view.Owner, item, out _))
                        { covered = true; break; }
                foreach (var renderer in view.Renderers)
                {
                    if (!(renderer is SkinnedMeshRenderer skin) || skin.sharedMesh != adopted.Mesh
                        || !skin.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)) continue;
                    // Merge the root block and only existing indexed overrides.
                    // A new indexed block would replace the entire root block,
                    // losing its native visibility, SH and tint properties.
                    renderer.GetPropertyBlock(styleProperties);
                    styleProperties.SetFloat("_CoverHeadwear", covered ? 1 : 0);
                    renderer.SetPropertyBlock(styleProperties);
                    for (int i = 0; i < skin.sharedMesh.subMeshCount; i++)
                    {
                        renderer.GetPropertyBlock(styleProperties, i);
                        if (styleProperties.isEmpty) continue;
                        styleProperties.SetFloat("_CoverHeadwear", covered ? 1 : 0);
                        renderer.SetPropertyBlock(styleProperties, i);
                    }
                }
            }
        }
        public bool TryGetEquipmentView(Entity actor, Entity item, out GameObject root)
        { root = null; return equipment != null && equipment.TryGet(actor, item, out root); }
        /// <summary>Side-effect-free proof of this owner's currently committed
        /// approved mesh/palette. Does not refresh, load art, consume RNG or draw
        /// a proxy. Unknown legacy styles remain explicit coverage gaps.</summary>
        public bool TryGetApprovedStyle(Entity owner,out SpreadBiomeStyleEvidence evidence)
        {
            evidence=new SpreadBiomeStyleEvidence(null,"outside-current-approved-scope",false);
            if(!IsReady||approvedStyle==null||!boundSpreadStyle||!SpreadPresentationScope.IsActive(CurrentZone)||!GladeAuthorityMatches)return false;
            if(owner==null||!recipes.TryGetValue(owner,out var recipe))
            {evidence=new SpreadBiomeStyleEvidence(null,"no-committed-owner",false);return false;}
            var current=SpawnRing3DRecipes.Resolve(CurrentZone,owner,definition,pilotLibrary?.Definition);
            if(current.ModelId==null||!ReferenceEquals(current.Owner,owner)||!SameGeometry(current,recipe)||current.Transient!=recipe.Transient||!IsRenderedEntity(owner))
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"stale-or-hidden-owner",recipe.Batched);return false;}
            if(nativeStyleLibrary?.Find(recipe.ModelId)!=null&&nativeStyleLibrary.ForOwner(CurrentZone,recipe)==null)
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"unapproved-static-owner",recipe.Batched);return false;}
            if(!approvedStyle.TryGet(recipe.ModelId,out var expected))
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"unmapped-style-source",recipe.Batched);return false;}
            if(recipe.Batched)return ground.TryGetApprovedStyle(owner,recipe,expected,styleProperties,styleMaterials,out evidence);
            if(!views.TryGetValue(owner,out var view)||view.Root==null||view.Renderers.Length!=1)
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"unsupported-body-fragment-layout",false,expected.Mesh,expected.Material);return false;}
            var renderer=view.Renderers[0];
            if(renderer==null||!renderer.enabled||renderer.forceRenderingOff||!renderer.gameObject.activeInHierarchy)
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-body-not-drawn",false,expected.Mesh,expected.Material);return false;}
            Mesh submitted=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if(submitted==null||submitted.vertexCount==0||submitted.subMeshCount!=expected.Materials.Length||submitted.GetIndexCount(0)==0||submitted!=expected.Mesh)
            {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-mesh-mismatch",false,expected.Mesh,expected.Material,submitted);return false;}
            SpreadBiomeStylePieceEvidence[] pieces=expected.Materials.Length>1?new SpreadBiomeStylePieceEvidence[expected.Materials.Length]:null;Material first=null;
            for(int slot=0;slot<expected.Materials.Length;slot++){
                var borrowed=expected.Materials[slot];if(submitted.GetIndexCount(slot)==0){evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-mesh-mismatch",false,expected.Mesh,borrowed,submitted);return false;}
                if(!SpreadBiomeStyleCatalog.PaletteMatches(renderer,surface.MaterialFor(borrowed),borrowed,styleProperties,styleMaterials,out var material,slot,expected.Materials.Length))
                {evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,"submitted-palette-mismatch",false,expected.Mesh,borrowed,submitted);return false;}
                if(slot==0)first=material;if(pieces!=null)pieces[slot]=new SpreadBiomeStylePieceEvidence(expected.Mesh,borrowed,submitted,material,slot,slot);
            }
            evidence=new SpreadBiomeStyleEvidence(recipe.ModelId,null,false,expected.Mesh,expected.Material,submitted,first,pieces);return true;
        }

        /// <summary>Read-only proof for one native equipped item. Every visible
        /// child mesh, palette and actual bone is checked; evidence returns one
        /// representative pair after all fitted pieces pass.</summary>
        public bool TryGetApprovedEquipmentStyle(Entity actor, Entity item, out SpreadBiomeStyleEvidence evidence)
        {
            evidence = new SpreadBiomeStyleEvidence(null,"outside-current-equipment-scope",false);
            if (!IsReady || !boundSpreadStyle || !SpreadPresentationScope.IsActive(CurrentZone)
                || !GladeAuthorityMatches || equipment == null || actor == null || !IsRenderedEntity(actor)) return false;
            var current = SpawnRing3DRecipes.Resolve(CurrentZone,actor,definition,pilotLibrary?.Definition);
            if (current.ModelId == null || !ReferenceEquals(current.Owner,actor) || !recipes.TryGetValue(actor,out var committed)
                || !SameGeometry(current,committed))
            { evidence = new SpreadBiomeStyleEvidence(null,"stale-equipment-actor",false); return false; }
            return equipment.TryGetApprovedStyle(actor,item,surface,styleProperties,styleMaterials,out evidence);
        }

        public bool TryGetEntityView(Entity entity, out GameObject root, out string modelId)
        {
            root = null; modelId = null;
            if(TryGetGasVolume(entity,out root,out var gas)){modelId="spread-gas-"+gas.Kind;return true;}
            if (entity == null || CurrentZone?.GetEntityCell(entity) == null || !recipes.TryGetValue(entity, out var recipe)) return false;
            if (recipe.Batched)
            { var cell = CurrentZone.GetEntityCell(entity); root = ground.RootFor(cell.X, cell.Y); }
            else if (views.TryGetValue(entity, out var view)) root = view.Root;
            modelId = recipe.ModelId; return root != null;
        }
        /// <summary>Diagnostic access to presentation owned by the exact live
        /// native entity, without refreshing or mutating quest state.</summary>
        public bool TryGetQuestCue(Entity entity, out GameObject root, out string state)
        {
            root=null;state="None";
            return entity!=null && CurrentZone?.GetEntityCell(entity)!=null && views.TryGetValue(entity,out var view)
                && NativeQuestCueViews.TryGet(view.QuestCue,PresentationVisible,out root,out state);
        }
        private void SyncQuestCue(View view)
        {
            questCues?.Sync(ref view.QuestCue,view.Owner,view.Root,view.Renderers,CurrentZone,
                StoryletPart.LocalPlayer,view.Drawn,FullReveal,surface?.WorldCamera);
        }
        private bool CollectorShown(View view) => PresentationVisible && boundSpreadStyle && GladeAuthorityMatches
            && view.Drawn && view.Owner.GetPart<RenderPart>()?.Visible==true && AnyBodyKnown(view.Owner,true);
        private void SyncCollectorCarry(View view)
        {
            if(view.CollectorCarry==null)return;
            view.CollectorCarry.Sync(CurrentZone,CollectorShown(view),out string gesture);
            if(gesture!=null)Action(view.Owner,CurrentZone,gesture,.6f);
            else if(view.Cast?.IsActive!=true&&view.MoveDuration<=0&&Time.unscaledTime>=view.ActionUntil)Play(view,"Idle");
        }
        /// <summary>Exact real inventory owner, separate from body/equipment and picking.</summary>
        public bool TryGetCollectorCarryView(Entity actor,Entity item,out GameObject root)
        {
            root=null;return IsReady&&actor!=null&&views.TryGetValue(actor,out var view)&&view.CollectorCarry!=null
                &&view.CollectorCarry.TryGet(CurrentZone,CollectorShown(view),item,out root);
        }
        public bool TryGetApprovedCollectorCarryStyle(Entity actor,Entity item,out SpreadBiomeStyleEvidence evidence)
        {
            evidence=new SpreadBiomeStyleEvidence(null,"outside-current-collector-carry",false);
            return IsReady&&actor!=null&&views.TryGetValue(actor,out var view)&&view.CollectorCarry!=null
                &&view.CollectorCarry.TryStyle(CurrentZone,CollectorShown(view),item,surface,styleProperties,styleMaterials,out evidence);
        }
        /// <summary>Read-only exact current gas output. Unknown/custom/stale or
        /// unsubmitted sources never suppress their native fallback.</summary>
        public bool TryGetGasVolume(Entity entity,out GameObject root,out SpreadTransientSample sample)
        {root=null;sample=default;return PresentationVisible&&transientVolumes!=null&&transientVolumes.TryGetGas(entity,out root,out sample);}
        public bool TryGetElementVolume(int x,int y,out GameObject root,out SpreadTransientSample sample)
        {root=null;sample=default;return PresentationVisible&&transientVolumes!=null&&transientVolumes.TryGetElement(x,y,out root,out sample);}
        public bool HasRepresentedCultivatedSoil(Entity entity)
            => PresentationVisible&&IsRenderedEntity(entity)&&RepairCultivationRecipes.HasCultivatedSoil(CurrentZone,entity)&&ground.HasCultivatedSoil(entity);
        public bool IsAuthoredEntity(Entity entity) => PresentationVisible && entity != null && CurrentZone.GetEntityCell(entity) != null && (recipes.ContainsKey(entity)||TryGetGasVolume(entity,out _,out _));
        public bool IsRenderedEntity(Entity entity)
        {
            if(TryGetGasVolume(entity,out _,out _))return true;
            if (!IsAuthoredEntity(entity)) return false;
            if (entity.GetPart<RenderPart>()?.Visible == false) return false;
            if (views.TryGetValue(entity, out var view)) return view.Drawn && view.Root != null && view.Root.activeInHierarchy;
            var cell = CurrentZone.GetEntityCell(entity); return FullReveal || cell.Explored;
        }
        public bool ClaimsCell(int x, int y) => PresentationVisible && x >= 0 && x < Zone.Width && y >= 0 && y < Zone.Height;
        public bool HasRepresentedWater(int x, int y) => ClaimsCell(x, y) && ground.HasWater(x, y);
        public void SetPresentationVisible(bool visible) { requestedVisible = visible; SyncCamera(); }
        private void SyncCamera()
        {
            bool wasVisible = surface != null && surface.IsVisible;
            surface?.Sync(source, PresentationRequested, Village3DSettings.LowDetail);
            groundContact?.SetEnabled(PresentationVisible && !Village3DSettings.LowDetail);
            if (wasVisible && !PresentationVisible) foreach (var view in views.Values) { Interrupt(view); view.CollectorCarry?.Sync(CurrentZone,false,out _); }
        }
        private void LateUpdate()
        {
            if (CurrentZone != null && source != null && !GladeAuthorityMatches)
                Bind(CurrentZone, source);
            if (IsReady && equipment != null && equipment.NeedsRefresh) RefreshEquipment(false);
            SyncCamera(); if (!PresentationVisible) return;
            foreach (var view in views.Values)
            {
                SyncQuestCue(view);
                SyncCollectorCarry(view);
                if (!view.Drawn) continue;
                bool wasCasting = view.Cast?.IsActive == true;
                view.Cast?.Tick(Time.unscaledDeltaTime);
                if (view.Cast?.IsActive == true) continue;
                if (wasCasting) { view.ActionUntil = 0; view.Clip = null; Play(view, "Idle"); }
                if (view.MoveDuration > 0)
                {
                    float t = Mathf.Clamp01((Time.unscaledTime-view.MoveStart)/view.MoveDuration);
                    view.Root.transform.position = Vector3.Lerp(view.Start, view.Target, t*t*(3-2*t));
                    if (t >= 1) { view.MoveDuration = 0; Play(view, "Idle"); }
                }
                else if (view.ActionUntil > 0 && Time.unscaledTime >= view.ActionUntil) { view.ActionUntil = 0; Play(view, "Idle"); }
            }
        }
        /// <summary>Selection only; action range and blocking use native cells.</summary>
        public bool TryPickWorld(Vector2 world, out Entity owner, out int x, out int y)
        {
            owner = null; x = y = -1;
            if (!PresentationVisible || !Village3DProjection.Finite(world.x) || !Village3DProjection.Finite(world.y)) return false;
            bool flatInBounds = Village3DProjection.TryWorldToCell(new Vector3(world.x, 0, world.y), out int px, out int py);
            // Raised bodies must not turn an out-of-chunk coordinate into a hit.
            if (!flatInBounds) return false;
            var flat = CurrentZone.GetCell(px, py);
            bool flatVisible = flat != null && (FullReveal || flat.IsVisible);
            var top = flat?.GetTopVisibleObject();
            // Fallback sprites remain on the ground grid. Only a visible native
            // fallback can override a raised3D body projected across this cell.
            if (flatVisible && top != null && !recipes.ContainsKey(top)) return false;
            var ray = NativeZone3DRenderSurface.GroundPointRay(world);
            int count = Physics.RaycastNonAlloc(ray, hits, 120, 1 << Village3DPresenter.WorldLayer, QueryTriggerInteraction.Collide);
            if (count == hits.Length && hits.Length <= byCollider.Count)
            { hits = new RaycastHit[byCollider.Count + 1]; count = Physics.RaycastNonAlloc(ray, hits, 120, 1 << Village3DPresenter.WorldLayer, QueryTriggerInteraction.Collide); }
            float nearest = float.PositiveInfinity; View best = null; int bestX = -1, bestY = -1; bool flatOwnerWasHit = false;
            for (int i = 0; i < count; i++)
            {
                if (!byCollider.TryGetValue(hits[i].collider, out var view)) continue;
                // A rejected physical hit cannot be rescued through another
                // visible flat cell of that same owner. Other valid hits remain.
                if (ReferenceEquals(top, view.Owner)) flatOwnerWasHit = true;
                if (!IsRenderedEntity(view.Owner)) continue;
                if (!Village3DProjection.TryWorldToCell(hits[i].point, out int hx, out int hy)) continue;
                var contact = CurrentZone.GetCell(hx, hy);
                if (!FullReveal && !contact.IsVisible) continue;
                var anchor = CurrentZone.GetEntityCell(view.Owner);
                if (anchor == null || (view.Transient && !AnyBodyKnown(view.Owner, visibleOnly:true))) continue;
                if (view.Owner.HasPart<MultiCellPilotPropPart>() && !contact.Occupants.Contains(view.Owner)) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; best = view; bestX = hx; bestY = hy; }
            }
            if (best != null)
            {
                owner = best.Owner; var anchor = CurrentZone.GetEntityCell(owner);
                x = owner.HasPart<MultiCellPilotPropPart>() ? bestX : anchor.X;
                y = owner.HasPart<MultiCellPilotPropPart>() ? bestY : anchor.Y; return true;
            }
            if (!flatVisible) return false;
            // This ray intersects y=0 at exactly the legacy input point. A body
            // owns that complete native cell even at a rounded no-mesh corner.
            if (!flatOwnerWasHit && top != null && top.HasPart<MultiCellPilotPropPart>() && IsRenderedEntity(top))
            { owner = top; x = px; y = py; return true; }
            if (top == null || !recipes.TryGetValue(top, out var recipe) || !recipe.Batched || !IsRenderedEntity(top)) return false;
            owner = top; x = px; y = py; return true;
        }
        private void Subscribe()
        {
            if (hooks) return; hooks = true;
            EntityVisualHooks.MovedCallback += OnMoved; EntityVisualHooks.AttackCallback += OnAttack; EntityVisualHooks.InteractionCallback += OnInteraction;
            EntityVisualHooks.DamageCallback += OnDamage; EntityVisualHooks.DeathCallback += OnDeath; EntityVisualHooks.CastCallback += OnCast;
        }
        private void Play(View view, string clip)
        {
            if ((clip=="Idle"||clip=="Walk")&&view.CollectorCarry?.HasCurrentCarry(CurrentZone,CollectorShown(view))==true)
                clip=clip=="Idle"?"CarryIdle":"CarryWalk";
            if (view.Cast?.IsActive == true) { view.Cast.Clear(); view.Clip = null; }
            // Encased elders have an architectural rest pose even without a rig.
            // All gesture/movement expiry paths settle through Idle; other actors
            // keep the facing selected by their own native actions.
            if(clip=="Idle"&&view.AuthoredIdleFacing)
                view.Root.transform.localRotation=Quaternion.Euler(0,view.AuthoredQuarterTurns*90,0);
            if (view.Animator == null || view.Clip == clip || !Application.isPlaying || !view.Root.activeInHierarchy) return;
            if (!view.Animator.HasState(0, Animator.StringToHash(clip))) return;
            view.Animator.CrossFadeInFixedTime(clip, .065f); view.Clip = clip;
        }
        private void OnMoved(Entity entity, Zone zone, int oldX, int oldY, int newX, int newY, bool forced)
        {
            if (!ReferenceEquals(zone, CurrentZone) || !views.TryGetValue(entity, out var view)) return;
            view.Target = Village3DProjection.CellCentre(newX, newY);
            if (forced || !view.Transient || !PresentationVisible || !view.Drawn || !Application.isPlaying || Math.Abs(newX-oldX)>1 || Math.Abs(newY-oldY)>1)
            { Interrupt(view); return; }
            view.Start = view.Root.transform.position; view.MoveStart = Time.unscaledTime; view.MoveDuration = .10f;
            if (!entity.HasPart<MultiCellPilotPropPart>()) view.Root.transform.rotation = Quaternion.LookRotation(new Vector3(newX-oldX, 0, oldY-newY));
            Play(view, "Walk");
        }
        private void Action(Entity entity, Zone zone, string clip, float duration = .22f)
        {
            if (!PresentationVisible || !ReferenceEquals(zone, CurrentZone) || entity == null || !views.TryGetValue(entity, out var view) || !view.Drawn) return;
            var facing = entity.GetPart<RenderPart>()?.VisualFacing ?? EntityVisualFacing.South;
            Vector3 direction = facing == EntityVisualFacing.North ? Vector3.forward : facing == EntityVisualFacing.East ? Vector3.right : facing == EntityVisualFacing.West ? Vector3.left : Vector3.back;
            if (!entity.HasPart<MultiCellPilotPropPart>()) view.Root.transform.rotation = Quaternion.LookRotation(direction); view.ActionUntil = Time.unscaledTime + duration; view.ActionState = clip; Play(view, view.ActionState);
        }
        private void OnInteraction(Entity actor, Entity target, Zone zone)
        {
            // A prior subscriber may have removed or carried either participant.
            if (!EntityVisualHooks.IsCurrentInteraction(actor, target, zone)) return;
            Action(actor, zone, "Interact", .6f);
        }
        private void OnAttack(Entity attacker, Entity defender, Zone zone) => Action(attacker, zone, "Attack");
        private void OnDamage(Entity target, Entity other, Zone zone, int amount, bool lethal) => Action(target, zone, "Hit");
        private void OnCast(Entity caster, Zone zone, string spellId, int sx, int sy, int tx, int ty, float duration)
        {
            if (!PresentationVisible || !ReferenceEquals(zone, CurrentZone) || caster == null || !views.TryGetValue(caster, out var view) || !view.Drawn) return;
            if (spellLibrary == null) spellLibrary = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            AnimationClip clip = null;
            try { clip = spellLibrary?.Find(spellId)?.FindCastClip(view.Animator); }
            catch (InvalidOperationException) { /* The coordinator already records rejected art; keep the ordinary gesture available. */ }
            if (clip == null || view.Cast?.TryPlay(clip, duration) != true) { Action(caster, zone, "Interact"); return; }
            view.MoveDuration = 0; view.ActionUntil = 0; view.Clip = null;
            view.Root.transform.position = view.Target;
            if (!caster.HasPart<MultiCellPilotPropPart>() && (tx != sx || ty != sy))
                view.Root.transform.rotation = Quaternion.LookRotation(new Vector3(tx - sx, 0, sy - ty));
        }
        private void OnDeath(Entity target, Entity killer, Zone zone, int x, int y)
        { if (ReferenceEquals(zone, CurrentZone) && target != null && views.TryGetValue(target, out var view)) { view.CollectorCarry?.Dispose(); SetDrawn(view, false); } }
        private static void Interrupt(View view)
        { view.Cast?.Clear(); view.MoveDuration = 0; view.ActionUntil = 0; view.Clip = null; if (view.Root != null) view.Root.transform.position = view.Target; }
        private void OnDisable() { SyncCamera(); foreach (var view in views.Values) { view.CollectorCarry?.Dispose(); Interrupt(view); } }
        private void OnDestroy() => Release();
        private void Release()
        {
            IsReady = false; Failure = null;
            if (hooks)
            {
                EntityVisualHooks.MovedCallback -= OnMoved; EntityVisualHooks.AttackCallback -= OnAttack; EntityVisualHooks.InteractionCallback -= OnInteraction;
                EntityVisualHooks.DamageCallback -= OnDamage; EntityVisualHooks.DeathCallback -= OnDeath; EntityVisualHooks.CastCallback -= OnCast; hooks = false;
            }
            foreach (var view in views.Values) { view.Cast?.Dispose(); view.CollectorCarry?.Dispose(); }
            spellLibrary = null;
            voxel = null;
            transientVolumes?.Dispose(); transientVolumes=null;
            questCues?.Dispose(); questCues = null;
            groundContact?.Dispose(); groundContact = null;
            equipment?.Dispose(); equipment = null; ground?.Dispose(); ground = null; surface?.Dispose(); surface = null;
            recipes.Clear(); staticStyles.Clear(); views.Clear(); byCollider.Clear(); seen.Clear(); removed.Clear();
            CurrentZone = null; source = null; library = null; collectorPortables = null; pilotLibrary = null; gladeLibrary = null; visitorPaintLibrary = null; visitorCreatureLibrary = null; humanoidLibrary = null; nativeStyleLibrary = null; approvedStyle = null; styleProperties?.Clear(); styleProperties = null; styleMaterials.Clear(); definition = null;
        }
        private void PrepareModel(GameObject root, bool transient, string modelId = null)
        {
            voxel?.Apply(root);
            gladeLibrary?.ApplyActorPaint(root);
            visitorPaintLibrary?.Apply(root, modelId);
            surface.PrepareModel(root, transient);
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
