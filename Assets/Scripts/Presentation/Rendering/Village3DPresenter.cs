using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only view of the native village. Owns meshes, a separate XZ
    /// camera and its XY composite; never owns simulation entities or saves.</summary>
    [ExecuteAlways, DefaultExecutionOrder(900)]
    public sealed class Village3DPresenter : MonoBehaviour
    {
        public const int WorldLayer = 12;
        // The native daytime illumination is0.4. Grade the material view while
        // retaining actual illumination ratios, darkness and fog ownership.
        private const float GameplayExposure = 2.2f;
        private sealed class View
        {
            public string Id, PilotModelId;
            public Village3DManifest.Owner Spec;
            public Entity Owner;
            public GameObject Root;
            public Renderer[] Renderers;
            public BoxCollider Collider;
            public Animator Animator;
            public NativeSpellCastPlayer Cast;
            public NativeQuestCueViews.Handle QuestCue;
            public bool Transient, Drawn;
            public Vector3 Target, Start;
            public float MoveStart, MoveDuration, ActionUntil;
            public string Clip;
        }
        private readonly List<View> views = new List<View>(80);
        private readonly List<View> actors = new List<View>(16);
        private readonly List<GameObject> decorations = new List<GameObject>(32);
        private readonly Dictionary<string, View> byId = new Dictionary<string, View>(StringComparer.Ordinal);
        private readonly Dictionary<Entity, View> byEntity = new Dictionary<Entity, View>();
        private readonly Dictionary<Collider, View> byCollider = new Dictionary<Collider, View>();
        private readonly Dictionary<string, bool> rooms = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private Village3DLibrary library;
        private VoxelWorldPresentation voxel;
        private MultiCellPilot3DLibrary pilotLibrary;
        private readonly Dictionary<Entity, View> portableViews = new Dictionary<Entity, View>();
        private readonly HashSet<Entity> portableSeen = new HashSet<Entity>();
        private readonly List<Entity> portableRemoved = new List<Entity>();
        private Village3DEquipmentViews equipmentViews;
        private NativeQuestCueViews questCues;
        private Village3DManifest definition;
        private Camera source;
        private NativeZone3DRenderSurface surface;
        private NativeSpellFxLibrary spellLibrary;
        private GameObject content;
        private bool requestedVisible = true, hooks;
        private LightMap lightMap;
        private Entity player;
        public Zone CurrentZone { get; private set; }
        public Camera WorldCamera { get; private set; }
        public bool IsReady { get; private set; }
        public string Failure { get; private set; }
        public bool VoxelPresentationActive => PresentationVisible && voxel != null;
        public int VoxelAppliedMeshCount => voxel?.AppliedMeshCount ?? 0;
        public int VoxelMissingMeshCount => voxel?.MissingMeshCount ?? 0;
        public int EquipmentFallbackCount => equipmentViews?.FallbackCount ?? 0;
        public IReadOnlyList<Village3DEquipmentFallback> EquipmentFallbacks =>
            equipmentViews?.Fallbacks ?? Array.Empty<Village3DEquipmentFallback>();
        /// <summary>Explicit art-showcase mode. Does not alter native exploration.</summary>
        public bool FullReveal { get; set; }
        private bool PresentationRequested => IsReady && requestedVisible && Village3DSettings.Enabled
            && source != null && isActiveAndEnabled;
        public bool PresentationVisible => PresentationRequested && surface != null && surface.IsVisible;
        public NativeZone3DRenderSurface ActiveSurface => PresentationVisible ? surface : null;
        public int RenderedOwnerCount
        {
            get { if (!PresentationVisible) return 0; int n = 0; foreach (var v in views) if (v.Drawn) n++; return n; }
        }

        public void Bind(Zone zone, Camera sourceCamera)
        {
            if (ReferenceEquals(CurrentZone, zone) && (IsReady || Failure != null))
            { source = sourceCamera; ApplyVisibility(); return; }
            Release(); CurrentZone = zone; source = sourceCamera;
            if (!Village3DSettings.Enabled || !MorrowfastSceneRuntime.IsActive(zone) || source == null) return;
            try
            {
                library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                if (library == null) throw new InvalidOperationException("Village 3D library is unavailable.");
                library.Validate(); definition = library.Definition;
                voxel = VoxelWorldPresentation.ForZone(zone);
                pilotLibrary = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
                pilotLibrary?.Validate();
                var materials = new List<Material> { library.WorldMaterial, library.WaterMaterial };
                if (pilotLibrary != null) { materials.Add(pilotLibrary.WorldMaterial); materials.Add(pilotLibrary.TarMaterial); }
                surface = new NativeZone3DRenderSurface(transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, materials.ToArray(), GameplayExposure);
                content = surface.ContentRoot.gameObject; WorldCamera = surface.WorldCamera;
                questCues = new NativeQuestCueViews(surface, library.WorldMaterial, 8);
                equipmentViews = new Village3DEquipmentViews(library, go => PrepareModel(go, true));
                foreach (var p in definition.staticPlacements)
                {
                    var model = MakeModel(p, content.transform);
                    if (p.role == "decoration") decorations.Add(model);
                }
                foreach (var p in definition.owners) AddView(p.ownerId, p, p.modelId);
                foreach (var resident in MorrowfastContent.AdditionalResidents)
                    AddView(resident.Id, null, "character-olive");
                AddView("$player", null, library.PlayerModelId);
                IsReady = true; Subscribe(); Refresh(null); ApplyVisibility();
            }
            catch (Exception e)
            {
                Release(); CurrentZone = zone; source = sourceCamera; Failure = e.Message;
                Debug.LogWarning("[Village3D] Original presentation retained: " + Failure);
            }
        }

        private GameObject MakeModel(Village3DManifest.Placement placement, Transform parent)
        {
            var prefab = library.FindModel(placement.modelId);
            if (prefab == null) throw new InvalidOperationException("Missing village model " + placement.modelId);
            var go = Instantiate(prefab, parent, false); go.name = placement.id ?? placement.modelId;
            go.transform.position = placement.position;
            go.transform.rotation = Quaternion.Euler(0, placement.rotationY, 0);
            go.transform.localScale = placement.scale;
            PrepareModel(go, false); return go;
        }
        private void PrepareModel(GameObject go, bool transient)
        {
            voxel?.Apply(go);
            surface.PrepareModel(go, transient);
        }
        private View AddView(string id, Village3DManifest.Owner spec, string modelId, Entity portableOwner = null)
        {
            bool transient = portableOwner != null
                ? portableOwner.GetPart<MultiCellPilotPropPart>().Role == "actor" || portableOwner.GetPart<PhysicsPart>()?.Takeable == true
                : spec == null || spec.kind == "npc" || spec.kind == "creature";
            var placement = spec ?? new Village3DManifest.Owner { modelId = modelId, scale = Vector3.one };
            GameObject root;
            if (portableOwner == null) root = MakeModel(placement, content.transform);
            else
            {
                var prefab = pilotLibrary.FindModel(modelId);
                if (prefab == null) throw new InvalidOperationException("Missing portable model " + modelId);
                root = Instantiate(prefab, content.transform, false);
            }
            root.name = id;
            PrepareModel(root, transient);
            var view = new View { Id = id, Spec = spec, Root = root, Transient = transient,
                Owner = portableOwner, PilotModelId = portableOwner == null ? null : modelId,
                Renderers = root.GetComponentsInChildren<Renderer>(true), Animator = root.GetComponentInChildren<Animator>(true) };
            if (view.Renderers.Length == 0) throw new InvalidOperationException("Empty owner model " + id);
            Bounds bounds = view.Renderers[0].bounds;
            for (int i = 1; i < view.Renderers.Length; i++) bounds.Encapsulate(view.Renderers[i].bounds);
            view.Collider = root.AddComponent<BoxCollider>(); view.Collider.isTrigger = true;
            // Selection only. Native footprints continue to decide movement and reach.
            view.Collider.center = root.transform.InverseTransformPoint(bounds.center);
            var a = root.transform.InverseTransformVector(new Vector3(bounds.size.x, 0, 0));
            var b = root.transform.InverseTransformVector(new Vector3(0, bounds.size.y, 0));
            var c = root.transform.InverseTransformVector(new Vector3(0, 0, bounds.size.z));
            view.Collider.size = Abs(a) + Abs(b) + Abs(c);
            if (view.Animator != null) { view.Animator.applyRootMotion = false; view.Animator.cullingMode = AnimatorCullingMode.CullCompletely; view.Cast = new NativeSpellCastPlayer(view.Animator); }
            views.Add(view); byId.Add(id, view); byCollider.Add(view.Collider, view);
            if (transient || portableOwner != null) actors.Add(view);
            return view;
        }
        // Only existing native portable owners are considered. No town layout,
        // entity identity, footprint, contents or equipment is authored here.
        private void SyncPortableOwners()
        {
            portableSeen.Clear(); portableRemoved.Clear();
            foreach (var entity in CurrentZone.GetReadOnlyEntities())
            {
                if (!entity.HasPart<MultiCellPilotPropPart>()) continue;
                var recipe = SpawnRing3DRecipes.ResolvePilot(CurrentZone, entity, pilotLibrary?.Definition);
                if (recipe.ModelId == null || string.IsNullOrEmpty(recipe.ComponentId)) continue;
                if (portableViews.TryGetValue(entity, out var view)
                    && (view.PilotModelId != recipe.ModelId || view.Id != recipe.ComponentId))
                { RemovePortableView(entity, view); view = null; }
                // IDs from a different authored scene never replace town owners.
                if (view == null && byId.ContainsKey(recipe.ComponentId)) continue;
                if (view == null)
                { view = AddView(recipe.ComponentId, null, recipe.ModelId, entity); portableViews.Add(entity, view); }
                portableSeen.Add(entity);
            }
            foreach (var pair in portableViews) if (!portableSeen.Contains(pair.Key)) portableRemoved.Add(pair.Key);
            foreach (var entity in portableRemoved) RemovePortableView(entity, portableViews[entity]);
        }
        private void RemovePortableView(Entity owner, View view)
        {
            portableViews.Remove(owner); byId.Remove(view.Id); byEntity.Remove(owner);
            byCollider.Remove(view.Collider); views.Remove(view); actors.Remove(view);
            view.Cast?.Dispose();
            if (view.Root != null)
            { view.Root.SetActive(false); if (Application.isPlaying) Destroy(view.Root); else DestroyImmediate(view.Root); }
        }
        private bool AnyBodyKnown(Entity owner, bool visibleOnly)
        {
            if (FullReveal) return true;
            foreach (var cell in CurrentZone.GetOccupiedCells(owner))
                if (visibleOnly ? cell.IsVisible : cell.Explored) return true;
            return false;
        }
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>Synchronize after native rendering dirties cells/FOV. No gameplay writes.</summary>
        public void Refresh(LightMap currentLight)
        {
            if (!IsReady || CurrentZone == null) return;
            lightMap = currentLight; player = null; byEntity.Clear(); rooms.Clear();
            SyncPortableOwners();
            foreach (var entity in CurrentZone.GetReadOnlyEntities())
                if (entity.HasTag("Player")) { player = entity; break; }
            string roomId = null;
            var playerCell = player == null ? null : CurrentZone.GetEntityCell(player);
            if (playerCell != null) roomId = MorrowfastSceneRuntime.GetRoomAt(CurrentZone, playerCell.X, playerCell.Y);
            foreach (var room in definition.buildings)
                rooms[room.id] = room.id == roomId || MorrowfastSceneRuntime.IsRoofLifted(CurrentZone, room.roofOwnerId);
            surface.UpdateFog(CurrentZone, lightMap, FullReveal);
            foreach (var v in views)
            {
                if (v.PilotModelId == null)
                    v.Owner = v.Id == "$player" ? player : MorrowfastSceneRuntime.FindOwner(CurrentZone, v.Id);
                var cell = v.Owner == null ? null : CurrentZone.GetEntityCell(v.Owner);
                if (cell == null) { v.Owner = null; SetDrawn(v, false); continue; }
                byEntity[v.Owner] = v;
                Vector3 target = Village3DProjection.CellCentre(cell.X, cell.Y);
                if (v.Spec != null) target += v.Spec.visualOffset;
                if (v.Target != target || !Application.isPlaying || !PresentationVisible || v.MoveDuration <= 0)
                { v.Target = target; v.Root.transform.position = target; v.MoveDuration = 0; }
                bool visible = v.Owner.GetPart<RenderPart>()?.Visible != false;
                bool roomOpen = v.Spec != null && v.Spec.roomId != null && rooms.TryGetValue(v.Spec.roomId, out bool open) && open;
                if (v.Spec?.kind == "roof" && roomOpen) visible = false;
                if (v.Spec?.visibleWhen == "room-open" && !roomOpen) visible = false;
                if (v.Spec?.kind == "door")
                    v.Root.transform.rotation = Quaternion.Euler(0, v.Spec.rotationY + (MorrowfastSceneRuntime.IsDoorOpen(CurrentZone, v.Id) ? 90 : 0), 0);
                visible &= v.PilotModelId != null ? AnyBodyKnown(v.Owner, v.Transient)
                    : v.Transient ? FullReveal || cell.IsVisible : AnyRemembered(v, cell);
                SetDrawn(v, visible);
                SyncQuestCue(v);
                if (visible && v.Transient && v.Cast?.IsActive != true && v.MoveDuration <= 0 && Time.unscaledTime >= v.ActionUntil) Play(v, "Idle");
            }
            RefreshEquipment(true);
            SyncCamera();
        }
        private void RefreshEquipment(bool force)
        {
            if (equipmentViews == null || CurrentZone == null) return;
            equipmentViews.BeginSync(force);
            foreach (var actor in actors)
                if (actor.Owner != null && CurrentZone.GetEntityCell(actor.Owner) != null)
                    equipmentViews.Sync(actor.Owner, actor.Root);
            equipmentViews.EndSync();
        }
        private bool AnyRemembered(View view, Cell anchor)
        {
            if (FullReveal || anchor.Explored) return true;
            // Large owners may cross a visible boundary while their anchor is hidden.
            // The material remains responsible for precise per-fragment clipping.
            foreach (var renderer in view.Renderers)
            {
                Bounds b = renderer.bounds;
                for (int z = Mathf.Max(0, Mathf.FloorToInt(b.min.z)); z <= Mathf.Min(24, Mathf.FloorToInt(b.max.z)); z++)
                    for (int x = Mathf.Max(0, Mathf.FloorToInt(b.min.x)); x <= Mathf.Min(79, Mathf.FloorToInt(b.max.x)); x++)
                        if (CurrentZone.GetCell(x, 24-z).Explored) return true;
            }
            return false;
        }
        private static void SetDrawn(View v, bool drawn)
        {
            v.Drawn = drawn; v.Root.SetActive(drawn);
            if (!drawn) Interrupt(v);
        }
        public void SetPresentationVisible(bool visible)
        {
            requestedVisible = visible; ApplyVisibility();
            if (!PresentationVisible) foreach (var v in actors)
            { Interrupt(v); }
        }
        private void ApplyVisibility()
        {
            bool wasVisible = surface != null && surface.IsVisible;
            surface?.Sync(source, PresentationRequested, Village3DSettings.LowDetail);
            if (wasVisible && !PresentationVisible) foreach (var actor in actors) Interrupt(actor);
        }
        private void LateUpdate()
        {
            if (IsReady && equipmentViews != null && equipmentViews.NeedsRefresh) RefreshEquipment(false);
            SyncCamera(); if (!PresentationVisible) return;
            foreach (var v in actors)
            {
                SyncQuestCue(v);
                if (!v.Drawn) continue;
                bool wasCasting = v.Cast?.IsActive == true;
                v.Cast?.Tick(Time.unscaledDeltaTime);
                if (v.Cast?.IsActive == true) continue;
                if (wasCasting) { v.ActionUntil = 0; v.Clip = null; Play(v, "Idle"); }
                if (v.MoveDuration > 0)
                {
                    float t = Mathf.Clamp01((Time.unscaledTime-v.MoveStart)/v.MoveDuration);
                    v.Root.transform.position = Vector3.Lerp(v.Start,v.Target,t*t*(3-2*t));
                    if (t >= 1) { v.MoveDuration = 0; Play(v,"Idle"); }
                }
                else if (v.ActionUntil > 0 && Time.unscaledTime >= v.ActionUntil) { v.ActionUntil = 0; Play(v,"Idle"); }
            }
        }
        private void SyncCamera()
        {
            ApplyVisibility();
            if (!IsReady || !PresentationVisible) return;
            bool lowDetail = Village3DSettings.LowDetail;
            foreach (var decoration in decorations)
                if (decoration != null && decoration.activeSelf == lowDetail) decoration.SetActive(!lowDetail);
        }

        public bool ClaimsCell(int x, int y) => PresentationVisible && x >= 0 && x < Zone.Width && y >= 0 && y < Zone.Height;
        public bool TryGetQuestCue(Entity entity, out GameObject root, out string state)
        {
            root=null;state="None";
            return entity!=null && CurrentZone?.GetEntityCell(entity)!=null && byEntity.TryGetValue(entity,out var view)
                && NativeQuestCueViews.TryGet(view.QuestCue,PresentationVisible,out root,out state);
        }
        private void SyncQuestCue(View view)
        {
            questCues?.Sync(ref view.QuestCue,view.Owner,view.Root,view.Renderers,CurrentZone,
                player,view.Drawn,FullReveal,WorldCamera);
        }
        public bool IsRenderedEntity(Entity entity) => PresentationVisible && entity != null
            && byEntity.TryGetValue(entity,out var v) && v.Drawn && v.Root.activeInHierarchy;
        public bool IsAuthoredEntity(Entity entity) => PresentationVisible && entity != null && CurrentZone.GetEntityCell(entity) != null
            && (byEntity.ContainsKey(entity) || entity.HasTag(MorrowfastSceneRuntime.TerrainTag));
        /// <summary>Diagnostic lookup of the real native reference and its owned view.</summary>
        public bool TryGetOwnerView(string ownerId, out Entity owner, out GameObject root)
        {
            owner = null; root = null;
            if (ownerId == null || !byId.TryGetValue(ownerId,out var view)) return false;
            owner = view.Owner; root = view.Root; return true;
        }
        /// <summary>Read-only diagnostic lookup, including attachments under a hidden actor.</summary>
        public bool TryGetEquipmentView(Entity actor, Entity item, out GameObject view)
        {
            view = null;
            return equipmentViews != null && equipmentViews.TryGet(actor, item, out view);
        }
        /// <summary>Inspect a visible model at legacy XY world coordinates. Selection
        /// colliders cannot change native action reach, terrain blocking or command cells.</summary>
        public bool TryPickWorld(Vector2 legacyXYWorld, out Entity owner, out int x, out int y)
        {
            owner = null; x = y = -1;
            if (!PresentationVisible || !Village3DProjection.Finite(legacyXYWorld.x) || !Village3DProjection.Finite(legacyXYWorld.y)) return false;
            bool flatInBounds = Village3DProjection.TryWorldToCell(new Vector3(legacyXYWorld.x, 0, legacyXYWorld.y), out int px, out int py);
            var flat = flatInBounds ? CurrentZone.GetCell(px, py) : null;
            bool flatVisible = flat != null && (FullReveal || flat.IsVisible);
            var top = flat?.GetTopVisibleObject();
            // Same precedence as ZoneRenderer: native fallback sprites stay on
            // the unchanged ground grid, even when another body projects here.
            if (flatVisible && top != null && !IsAuthoredEntity(top)) return false;
            int count = Physics.RaycastNonAlloc(NativeZone3DRenderSurface.GroundPointRay(legacyXYWorld),hits,120,1<<WorldLayer,QueryTriggerInteraction.Collide);
            float nearest = float.PositiveInfinity; View best = null; int bestX = -1, bestY = -1; bool flatOwnerWasHit = false;
            for (int i = 0; i < count; i++)
            {
                if (!byCollider.TryGetValue(hits[i].collider,out var v)) continue;
                if (ReferenceEquals(top,v.Owner)) flatOwnerWasHit = true;
                if (!v.Drawn || v.Owner == null || !v.Root.activeInHierarchy) continue;
                if (!Village3DProjection.TryWorldToCell(hits[i].point, out int hx, out int hy)) continue;
                var contact = CurrentZone.GetCell(hx, hy);
                if (!FullReveal && !contact.IsVisible) continue;
                if (v.PilotModelId != null && !contact.Occupants.Contains(v.Owner)) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; best = v; bestX = hx; bestY = hy; }
            }
            if (best == null)
            {
                if (flatOwnerWasHit || !flatVisible || top == null || !portableViews.TryGetValue(top, out best) || !IsRenderedEntity(top)) return false;
                bestX = px; bestY = py;
            }
            var cell = CurrentZone.GetEntityCell(best.Owner); if (cell == null) return false;
            owner = best.Owner; x = best.PilotModelId != null ? bestX : cell.X; y = best.PilotModelId != null ? bestY : cell.Y; return true;
        }

        private void Subscribe()
        {
            if (hooks) return; hooks = true;
            EntityVisualHooks.MovedCallback += OnMoved; EntityVisualHooks.AttackCallback += OnAttack;
            EntityVisualHooks.DamageCallback += OnDamage; EntityVisualHooks.DeathCallback += OnDeath;
            EntityVisualHooks.CastCallback += OnCast;
        }
        private static void Play(View v, string clip)
        {
            if (v.Cast?.IsActive == true) { v.Cast.Clear(); v.Clip = null; }
            if (v.Animator == null || v.Clip == clip || !Application.isPlaying || !v.Root.activeInHierarchy) return;
            if (!v.Animator.HasState(0,Animator.StringToHash(clip))) return;
            v.Animator.CrossFadeInFixedTime(clip,.065f); v.Clip = clip;
        }
        private void OnMoved(Entity entity, Zone zone, int oldX, int oldY, int newX, int newY, bool forced)
        {
            if (!ReferenceEquals(zone,CurrentZone) || !byEntity.TryGetValue(entity,out var v)) return;
            v.Target = Village3DProjection.CellCentre(newX,newY) + (v.Spec?.visualOffset ?? Vector3.zero);
            if (forced || !v.Transient || !PresentationVisible || !v.Drawn || !Application.isPlaying || Math.Abs(newX-oldX)>1 || Math.Abs(newY-oldY)>1)
            { Interrupt(v); return; }
            v.Start = v.Root.transform.position; v.MoveStart = Time.unscaledTime; v.MoveDuration = .10f;
            if (v.PilotModelId == null) v.Root.transform.rotation = Quaternion.LookRotation(new Vector3(newX-oldX,0,oldY-newY));
            Play(v,"Walk");
        }
        private void Action(Entity entity, Zone zone, string clip)
        {
            if (!PresentationVisible || !ReferenceEquals(zone,CurrentZone) || entity == null || !byEntity.TryGetValue(entity,out var v) || !v.Drawn) return;
            var facing = entity.GetPart<RenderPart>()?.VisualFacing ?? EntityVisualFacing.South;
            Vector3 direction = facing == EntityVisualFacing.North ? Vector3.forward
                : facing == EntityVisualFacing.East ? Vector3.right
                : facing == EntityVisualFacing.West ? Vector3.left : Vector3.back;
            if (v.PilotModelId == null) v.Root.transform.rotation = Quaternion.LookRotation(direction);
            v.ActionUntil = Time.unscaledTime+.22f; Play(v,clip);
        }
        private void OnAttack(Entity attacker, Entity defender, Zone zone) => Action(attacker,zone,"Attack");
        private void OnDamage(Entity target, Entity sourceEntity, Zone zone, int amount, bool lethal) => Action(target,zone,"Hit");
        private void OnCast(Entity caster, Zone zone, string spellId, int sx, int sy, int tx, int ty, float duration)
        {
            if (!PresentationVisible || !ReferenceEquals(zone, CurrentZone) || caster == null || !byEntity.TryGetValue(caster, out var v) || !v.Drawn) return;
            if (spellLibrary == null) spellLibrary = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            AnimationClip clip = null;
            try { clip = spellLibrary?.Find(spellId)?.FindCastClip(v.Animator); }
            catch (InvalidOperationException) { /* The coordinator already records rejected art; keep the ordinary gesture available. */ }
            if (clip == null || v.Cast?.TryPlay(clip, duration) != true) { Action(caster, zone, "Interact"); return; }
            v.MoveDuration = 0; v.ActionUntil = 0; v.Clip = null;
            v.Root.transform.position = v.Target;
            if (v.PilotModelId == null && (tx != sx || ty != sy))
                v.Root.transform.rotation = Quaternion.LookRotation(new Vector3(tx - sx, 0, sy - ty));
        }
        private void OnDeath(Entity target, Entity killer, Zone zone, int x, int y)
        { if (ReferenceEquals(zone,CurrentZone) && target != null && byEntity.TryGetValue(target,out var v)) SetDrawn(v,false); }
        private static void Interrupt(View view)
        {
            view.Cast?.Clear(); view.MoveDuration = 0; view.ActionUntil = 0; view.Clip = null;
            if (view.Root != null) view.Root.transform.position = view.Target;
        }
        private void OnDisable()
        { ApplyVisibility(); foreach (var view in actors) Interrupt(view); }
        private void OnDestroy() => Release();
        private void Release()
        {
            IsReady = false; Failure = null;
            if (hooks)
            {
                EntityVisualHooks.MovedCallback -= OnMoved; EntityVisualHooks.AttackCallback -= OnAttack;
                EntityVisualHooks.DamageCallback -= OnDamage; EntityVisualHooks.DeathCallback -= OnDeath;
                EntityVisualHooks.CastCallback -= OnCast; hooks = false;
            }
            foreach (var view in views) view.Cast?.Dispose();
            spellLibrary = null;
            voxel = null;
            questCues?.Dispose(); questCues = null;
            equipmentViews?.Dispose(); equipmentViews = null;
            surface?.Dispose(); surface = null;
            WorldCamera = null; content = null; source = null;
            library = null; pilotLibrary = null; definition = null; player = null; lightMap = null; CurrentZone = null;
            portableViews.Clear(); portableSeen.Clear(); portableRemoved.Clear();
            views.Clear(); actors.Clear(); decorations.Clear(); byId.Clear(); byEntity.Clear(); byCollider.Clear(); rooms.Clear();
        }
    }
}
