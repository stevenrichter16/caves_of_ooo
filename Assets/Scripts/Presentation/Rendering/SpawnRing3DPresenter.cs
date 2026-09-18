using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only presentation of the eight native surface neighbours of
    /// Morrowfast. Simulation membership, collision, equipment and water remain
    /// owned by the native zone. Unknown content retains its native fallback.</summary>
    [ExecuteAlways, DefaultExecutionOrder(900)]
    public sealed class SpawnRing3DPresenter : MonoBehaviour
    {
        private sealed class View
        {
            public Entity Owner;
            public string ModelId;
            public int AuthoredQuarterTurns;
            public GameObject Root;
            public Renderer[] Renderers;
            public Collider[] Colliders;
            public Animator Animator;
            public NativeSpellCastPlayer Cast;
            public NativeQuestCueViews.Handle QuestCue;
            public bool Transient, Drawn, AuthoredIdleFacing;
            public Vector3 Target, Start;
            public float MoveStart, MoveDuration, ActionUntil;
            public string Clip;
        }
        private readonly Dictionary<Entity, SpawnRing3DRecipe> recipes = new Dictionary<Entity, SpawnRing3DRecipe>();
        private readonly Dictionary<Entity, View> views = new Dictionary<Entity, View>();
        private readonly Dictionary<Collider, View> byCollider = new Dictionary<Collider, View>();
        private readonly HashSet<Entity> seen = new HashSet<Entity>();
        private readonly List<Entity> removed = new List<Entity>();
        private RaycastHit[] hits = new RaycastHit[64];
        private SpawnRing3DLibrary library;
        private VoxelWorldPresentation voxel;
        private MultiCellPilot3DLibrary pilotLibrary;
        private SpawnRing3DCatalog definition;
        private NativeZone3DRenderSurface surface;
        private NativeSpellFxLibrary spellLibrary;
        private SpawnRing3DGroundPatches ground;
        private Village3DEquipmentViews equipment;
        private NativeQuestCueViews questCues;
        private Camera source;
        private bool requestedVisible = true, hooks;
        public Zone CurrentZone { get; private set; }
        public Camera WorldCamera => surface?.WorldCamera;
        public bool IsReady { get; private set; }
        public string Failure { get; private set; }
        public bool VoxelPresentationActive => PresentationVisible && voxel != null;
        public int VoxelAppliedMeshCount => voxel?.AppliedMeshCount ?? 0;
        public int VoxelMissingMeshCount => voxel?.MissingMeshCount ?? 0;
        public bool FullReveal { get; set; }
        private bool PresentationRequested => IsReady && requestedVisible && Village3DSettings.Enabled && source != null && isActiveAndEnabled && AreaCompositionScope.Allows(CurrentZone);
        public bool PresentationVisible => PresentationRequested && surface != null && surface.IsVisible;
        public NativeZone3DRenderSurface ActiveSurface => PresentationVisible ? surface : null;
        public int GroundBuildCount => ground?.GroundBuildCount ?? 0;
        public int GroundPatchCount => ground?.PatchCount ?? 0;
        public int GroundPatchRevision(int x, int y) => ground?.Revision(x, y) ?? 0;
        public IReadOnlyList<Village3DEquipmentFallback> EquipmentFallbacks => equipment?.Fallbacks ?? Array.Empty<Village3DEquipmentFallback>();

        public void Bind(Zone zone, Camera sourceCamera)
        {
            if(zone!=null&&!AreaCompositionScope.Allows(zone))
            {Release();CurrentZone=zone;source=sourceCamera;return;}
            if (ReferenceEquals(CurrentZone, zone) && (IsReady || Failure != null))
            { source = sourceCamera; SyncCamera(); return; }
            Release(); CurrentZone = zone; source = sourceCamera;
            if (!Village3DSettings.Enabled || zone == null || source == null || !SupportsZone(zone.ZoneID)) return;
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
                surface = new NativeZone3DRenderSurface(transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, materials.ToArray(), 2.2f);
                questCues = new NativeQuestCueViews(surface, library.WorldMaterial, 16);
                ground = new SpawnRing3DGroundPatches(surface, library, pilotLibrary, voxel);
                equipment = new Village3DEquipmentViews(library.EquipmentLibrary, go => PrepareModel(go, true));
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
        private void RefreshCurrent(LightMap light, HashSet<int> dirtyCells)
        {
            seen.Clear(); removed.Clear();
            foreach (var entity in CurrentZone.GetReadOnlyEntities())
            {
                var recipe = SpawnRing3DRecipes.Resolve(CurrentZone, entity, definition, pilotLibrary?.Definition);
                if (recipe.ModelId == null) continue;
                seen.Add(entity);
                if (recipes.TryGetValue(entity, out var previous) && !SameGeometry(previous, recipe))
                { Mark(previous); Mark(recipe); }
                else if (!recipes.ContainsKey(entity)) Mark(recipe);
                recipes[entity] = recipe;
                if (recipe.Batched)
                { if (views.TryGetValue(entity, out var old)) RemoveView(old); continue; }
                if (!views.TryGetValue(entity, out var view) || view.ModelId != recipe.ModelId)
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
                if (drawn && view.Cast?.IsActive != true && view.MoveDuration <= 0 && Time.unscaledTime >= view.ActionUntil) Play(view, "Idle");
            }
            foreach (var pair in recipes) if (!seen.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var entity in removed)
            {
                Mark(recipes[entity]); recipes.Remove(entity);
                if (views.TryGetValue(entity, out var view)) RemoveView(view);
            }
            ground.Refresh(CurrentZone, recipes, dirtyCells);
            surface.UpdateFog(CurrentZone, light, FullReveal);
            RefreshEquipment(true); SyncCamera();
        }
        private static bool SameGeometry(SpawnRing3DRecipe a, SpawnRing3DRecipe b)
            => a.ModelId == b.ModelId && a.Position == b.Position && a.Batched == b.Batched && a.QuarterTurns == b.QuarterTurns;
        private void Mark(SpawnRing3DRecipe recipe)
        {
            if (recipe.Batched && Village3DProjection.TryWorldToCell(recipe.Position, out int x, out int y)) ground.Mark(x, y);
        }
        private View AddView(SpawnRing3DRecipe recipe)
        {
            var prefab = recipe.Owner.HasPart<MultiCellPilotPropPart>() ? pilotLibrary?.FindModel(recipe.ModelId) : library.FindModel(recipe.ModelId);
            if (prefab == null) throw new InvalidOperationException("Missing ring model " + recipe.ModelId);
            var root = Instantiate(prefab, surface.ContentRoot, false); root.name = recipe.ComponentId ?? recipe.ModelId;
            root.transform.position = recipe.Position;
            if(recipe.QuarterTurns!=0)root.transform.localRotation=Quaternion.Euler(0,recipe.QuarterTurns*90,0);
            PrepareModel(root, recipe.Transient);
            var view = new View { Owner = recipe.Owner, ModelId = recipe.ModelId, Root = root, Target = recipe.Position,
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
                Bounds bounds = view.Renderers[0].bounds;
                for (int i = 1; i < view.Renderers.Length; i++) bounds.Encapsulate(view.Renderers[i].bounds);
                var collider = root.AddComponent<BoxCollider>(); collider.isTrigger = true;
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = Abs(root.transform.InverseTransformVector(new Vector3(bounds.size.x, 0, 0)))
                    + Abs(root.transform.InverseTransformVector(new Vector3(0, bounds.size.y, 0)))
                    + Abs(root.transform.InverseTransformVector(new Vector3(0, 0, bounds.size.z)));
            }
            view.Colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (var collider in view.Colliders) byCollider.Add(collider, view);
            if (view.Animator != null) { view.Animator.applyRootMotion = false; view.Animator.cullingMode = AnimatorCullingMode.CullCompletely; view.Cast = new NativeSpellCastPlayer(view.Animator); }
            views.Add(recipe.Owner, view); return view;
        }
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        private void RemoveView(View view)
        {
            views.Remove(view.Owner); view.Cast?.Dispose();
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
        }
        public bool TryGetEquipmentView(Entity actor, Entity item, out GameObject root)
        { root = null; return equipment != null && equipment.TryGet(actor, item, out root); }
        public bool TryGetEntityView(Entity entity, out GameObject root, out string modelId)
        {
            root = null; modelId = null;
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
        public bool IsAuthoredEntity(Entity entity) => PresentationVisible && entity != null && CurrentZone.GetEntityCell(entity) != null && recipes.ContainsKey(entity);
        public bool IsRenderedEntity(Entity entity)
        {
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
            if (wasVisible && !PresentationVisible) foreach (var view in views.Values) Interrupt(view);
        }
        private void LateUpdate()
        {
            if (IsReady && equipment != null && equipment.NeedsRefresh) RefreshEquipment(false);
            SyncCamera(); if (!PresentationVisible) return;
            foreach (var view in views.Values)
            {
                SyncQuestCue(view);
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
            EntityVisualHooks.MovedCallback += OnMoved; EntityVisualHooks.AttackCallback += OnAttack;
            EntityVisualHooks.DamageCallback += OnDamage; EntityVisualHooks.DeathCallback += OnDeath; EntityVisualHooks.CastCallback += OnCast;
        }
        private static void Play(View view, string clip)
        {
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
        private void Action(Entity entity, Zone zone, string clip)
        {
            if (!PresentationVisible || !ReferenceEquals(zone, CurrentZone) || entity == null || !views.TryGetValue(entity, out var view) || !view.Drawn) return;
            var facing = entity.GetPart<RenderPart>()?.VisualFacing ?? EntityVisualFacing.South;
            Vector3 direction = facing == EntityVisualFacing.North ? Vector3.forward : facing == EntityVisualFacing.East ? Vector3.right : facing == EntityVisualFacing.West ? Vector3.left : Vector3.back;
            if (!entity.HasPart<MultiCellPilotPropPart>()) view.Root.transform.rotation = Quaternion.LookRotation(direction); view.ActionUntil = Time.unscaledTime + .22f; Play(view, clip);
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
        { if (ReferenceEquals(zone, CurrentZone) && target != null && views.TryGetValue(target, out var view)) SetDrawn(view, false); }
        private static void Interrupt(View view)
        { view.Cast?.Clear(); view.MoveDuration = 0; view.ActionUntil = 0; view.Clip = null; if (view.Root != null) view.Root.transform.position = view.Target; }
        private void OnDisable() { SyncCamera(); foreach (var view in views.Values) Interrupt(view); }
        private void OnDestroy() => Release();
        private void Release()
        {
            IsReady = false; Failure = null;
            if (hooks)
            {
                EntityVisualHooks.MovedCallback -= OnMoved; EntityVisualHooks.AttackCallback -= OnAttack;
                EntityVisualHooks.DamageCallback -= OnDamage; EntityVisualHooks.DeathCallback -= OnDeath; EntityVisualHooks.CastCallback -= OnCast; hooks = false;
            }
            foreach (var view in views.Values) view.Cast?.Dispose();
            spellLibrary = null;
            voxel = null;
            questCues?.Dispose(); questCues = null;
            equipment?.Dispose(); equipment = null; ground?.Dispose(); ground = null; surface?.Dispose(); surface = null;
            recipes.Clear(); views.Clear(); byCollider.Clear(); seen.Clear(); removed.Clear();
            CurrentZone = null; source = null; library = null; pilotLibrary = null; definition = null;
        }
        private void PrepareModel(GameObject root, bool transient)
        {
            voxel?.Apply(root);
            surface.PrepareModel(root, transient);
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
