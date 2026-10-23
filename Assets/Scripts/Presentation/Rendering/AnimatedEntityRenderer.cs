using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pooled SpriteRenderer layer for animated actors. Simulation positions remain
    /// discrete and authoritative; this component only interpolates their presentation.
    /// </summary>
    [ExecuteAlways]
    public sealed partial class AnimatedEntityRenderer : MonoBehaviour
    {
        public const int ShadowSortingOrder = 5;
        public const int BodySortingOrder = 6;
        private const int MaxPooledViews = 96;
        private const float DeathDuration = 0.28f;
        private const float PixelsPerWorldUnit = 16f;

        private sealed class EntityView
        {
            public Entity Entity;
            public EntityVisualAsset Asset;
            public GameObject Root;
            public Transform RootTransform;
            public Transform BodyTransform;
            public SpriteRenderer Body;
            public SpriteRenderer Shadow;
            public int CellX;
            public int CellY;
            public EntityVisualFacing Facing;
            public EntityVisualState State;
            public float StateTime;
            public float StateDuration;
            public float IdlePhase;
            public Vector3 MoveStart;
            public Vector3 MoveEnd;
            public Vector2 AttackDirection;
            public Color LightTint = Color.white;
            public bool Dying;
            public bool Flat;
            public bool ForcedIdle;
            public bool HasPendingReaction;
            public EntityVisualState PendingReaction;

            public bool IsMoving => !Dying && State == EntityVisualState.Walk;
        }

        private readonly Dictionary<Entity, EntityView> _views
            = new Dictionary<Entity, EntityView>();
        private readonly HashSet<Entity> _seen = new HashSet<Entity>();
        private readonly List<Entity> _releaseScratch = new List<Entity>();
        private readonly Stack<EntityView> _pool = new Stack<EntityView>();

        private Zone _zone;
        private Sprite _shadowSprite;
        private Material _sharedMaterial;
        private Func<Entity, int, int, Color> _tintProvider;
        private Func<Entity, bool> _sourceEntityPredicate;
        private Tilemap _worldTilemap;
        private bool _initialized;
        private bool _presentationVisible = true;

        public int ActiveViewCount => _views.Count;
        public int PooledViewCount => _pool.Count;
        public Zone CurrentZone => _zone;

        /// <summary>
        /// Uses the supplied world tilemap for actor foot positions. Omitting it
        /// preserves the original unit-cell projection for standalone presenters.
        /// </summary>
        public void Init(Func<Entity, int, int, Color> tintProvider, Tilemap worldTilemap = null)
        {
            if (_initialized) return;
            _initialized = true;
            _tintProvider = tintProvider;
            _worldTilemap = worldTilemap;
            _shadowSprite = Resources.Load<Sprite>("Sprites/Actors/actor_shadow");

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _sharedMaterial = new Material(shader)
                {
                    name = "AnimatedEntityRenderer (Runtime)",
                    hideFlags = HideFlags.DontSave,
                };
            }

            EntityVisualHooks.MovedCallback += OnMoved;
            EntityVisualHooks.AttackCallback += OnAttack;
            EntityVisualHooks.CastCallback += OnCast;
            EntityVisualHooks.DamageCallback += OnDamage;
            EntityVisualHooks.DeathCallback += OnDeath;
        }

        public bool CanRender(Entity entity)
        {
            return _initialized
                && _presentationVisible
                && (_flatPixelMode ? _flatMotionCatalog != null && _flatMotionCatalog.TryGetAsset(entity, out _)
                    : EntityVisualCatalog.TryGetAsset(entity, out _))
                && !IsOwnedBySource(entity);
        }

        /// <summary>Source component presenters own these bodies while their art is
        /// visible. Re-evaluated at sync so existing ordinary views also release.</summary>
        public void SetSourceEntityPredicate(Func<Entity, bool> predicate) => _sourceEntityPredicate = predicate;

        private bool IsOwnedBySource(Entity entity) => entity != null && _sourceEntityPredicate?.Invoke(entity) == true;

        public void SetZone(Zone zone)
        {
            if (ReferenceEquals(_zone, zone)) return;
            ClearViews();
            _flatRepaintCells.Clear();
            _flatResumeOwners.Clear();
            _zone = zone;
        }

        public void SetPresentationVisible(bool visible)
        {
            if (_presentationVisible == visible) return;
            _presentationVisible = visible;
            if (_flatPixelMode && !visible) { ClearViews(); return; }
            foreach (EntityView view in _views.Values)
                SetViewVisible(view, visible);
        }

        /// <summary>
        /// Reconciles runtime views with the active zone after its tile layers have
        /// rendered. Only the top visible object in a cell receives a view, matching
        /// the existing CP437/static-sprite contract exactly.
        /// </summary>
        public void SyncZone(Zone zone)
        {
            if (!_initialized) return;
            if (!ReferenceEquals(_zone, zone))
                SetZone(zone);
            if (_zone == null || !_presentationVisible)
                return;
            if (_flatPixelMode) { CompleteFlatCellPass(); return; }

            _seen.Clear();
            foreach (Entity entity in _zone.GetReadOnlyEntities())
            {
                // Most zone entities are terrain. Resolve catalog eligibility before
                // consulting detailed source views so terrain does not scan their layers.
                if (!EntityVisualCatalog.TryGetAsset(entity, out EntityVisualAsset asset) || IsOwnedBySource(entity))
                    continue;

                Cell cell = _zone.GetEntityCell(entity);
                RenderPart render = entity.GetPart<RenderPart>();
                if (cell == null || !cell.Explored || !cell.IsVisible
                    || render == null || !render.Visible
                    || !ReferenceEquals(cell.GetTopVisibleObject(), entity))
                    continue;

                _seen.Add(entity);
                if (!_views.TryGetValue(entity, out EntityView view))
                {
                    view = AcquireView(entity, asset, cell.X, cell.Y, render.VisualFacing);
                    _views.Add(entity, view);
                }
                else
                {
                    view.CellX = cell.X;
                    view.CellY = cell.Y;
                    view.Facing = render.VisualFacing;
                    if (!view.IsMoving && !view.Dying)
                        view.RootTransform.position = CellAnchor(cell.X, cell.Y);
                    UpdateSortingDepth(view);
                }

                view.LightTint = ResolveTint(entity, cell.X, cell.Y);
                ApplyBodyColor(view, false);
                SetViewVisible(view, true);
            }

            _releaseScratch.Clear();
            foreach (var pair in _views)
            {
                if (IsOwnedBySource(pair.Key) || (!_seen.Contains(pair.Key) && !pair.Value.Dying))
                    _releaseScratch.Add(pair.Key);
            }
            for (int i = 0; i < _releaseScratch.Count; i++)
                ReleaseView(_releaseScratch[i]);
        }

        public void ClearViews()
        {
            _releaseScratch.Clear();
            foreach (Entity entity in _views.Keys)
                _releaseScratch.Add(entity);
            for (int i = 0; i < _releaseScratch.Count; i++)
                ReleaseView(_releaseScratch[i]);
        }

        private void Update() => AdvancePresentation(Time.unscaledDeltaTime);

        /// <summary>Advances presentation only; gameplay remains turn-discrete.</summary>
        public void AdvancePresentation(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            deltaTime = Mathf.Max(0f, deltaTime);
            ReconcileFlatViews();
            if (!_initialized || !_presentationVisible || _views.Count == 0)
                return;

            _releaseScratch.Clear();
            foreach (var pair in _views)
            {
                EntityView view = pair.Value;
                TickView(view, deltaTime);
                if (view.Dying && view.StateTime >= DeathDuration)
                    _releaseScratch.Add(pair.Key);
            }
            for (int i = 0; i < _releaseScratch.Count; i++)
                ReleaseView(_releaseScratch[i]);
            if (_flatPixelMode) FlushFlatRepaints();
        }

        private void TickView(EntityView view, float deltaTime)
        {
            if (view.Flat) { TickFlatView(view, deltaTime); return; }
            view.StateTime += deltaTime;

            if (view.Dying)
            {
                float normalized = Mathf.Clamp01(view.StateTime / DeathDuration);
                int frame = FrameForNormalized(view, EntityVisualState.Hurt, normalized);
                view.Body.sprite = view.Asset.GetFrame(EntityVisualState.Hurt, view.Facing, frame);
                view.BodyTransform.localPosition = new Vector3(0f, -PixelSnap(normalized * 0.125f), 0f);
                view.Body.enabled = normalized < 0.55f || ((int)(view.StateTime * 32f) & 1) == 0;
                view.Shadow.enabled = _presentationVisible && normalized < 0.8f;
                return;
            }

            switch (view.State)
            {
                case EntityVisualState.Walk:
                    TickMove(view);
                    break;
                case EntityVisualState.Attack:
                    TickAttack(view);
                    break;
                case EntityVisualState.Cast:
                    TickCast(view);
                    break;
                case EntityVisualState.Hurt:
                    TickHurt(view);
                    break;
                default:
                    TickIdle(view);
                    break;
            }
        }

        private void TickMove(EntityView view)
        {
            float normalized = Mathf.Clamp01(view.StateTime / view.StateDuration);
            float eased = normalized * normalized * (3f - 2f * normalized);
            view.RootTransform.position = PixelSnap(Vector3.LerpUnclamped(view.MoveStart, view.MoveEnd, eased));
            view.Body.sprite = view.Asset.GetFrame(
                EntityVisualState.Walk,
                view.Facing,
                FrameForNormalized(view, EntityVisualState.Walk, normalized));

            if (normalized >= 1f)
                BeginState(view, EntityVisualState.Idle, 0f);
        }

        private void TickAttack(EntityView view)
        {
            float normalized = Mathf.Clamp01(view.StateTime / view.StateDuration);
            view.Body.sprite = view.Asset.GetFrame(
                EntityVisualState.Attack,
                view.Facing,
                FrameForNormalized(view, EntityVisualState.Attack, normalized));
            float lunge = PixelSnap(Mathf.Sin(normalized * Mathf.PI) * 0.125f);
            view.BodyTransform.localPosition = new Vector3(
                PixelSnap(view.AttackDirection.x * lunge),
                PixelSnap(-view.AttackDirection.y * lunge),
                0f);

            if (normalized >= 1f)
                BeginState(view, EntityVisualState.Idle, 0f);
        }

        private void TickHurt(EntityView view)
        {
            float normalized = Mathf.Clamp01(view.StateTime / view.StateDuration);
            view.Body.sprite = view.Asset.GetFrame(
                EntityVisualState.Hurt,
                view.Facing,
                FrameForNormalized(view, EntityVisualState.Hurt, normalized));
            float shake = ((int)(view.StateTime * 40f) & 1) == 0 ? -1f / PixelsPerWorldUnit : 1f / PixelsPerWorldUnit;
            view.BodyTransform.localPosition = new Vector3(shake, 0f, 0f);
            ApplyBodyColor(view, normalized < 0.45f);

            if (normalized >= 1f)
                BeginState(view, EntityVisualState.Idle, 0f);
        }

        private void TickCast(EntityView view)
        {
            float normalized = Mathf.Clamp01(view.StateTime / view.StateDuration);
            view.Body.sprite = view.Asset.GetFrame(EntityVisualState.Cast, view.Facing,
                FrameForNormalized(view, EntityVisualState.Cast, normalized));
            // One-pixel gathered gesture also reads on non-humanoid Attack fallback art.
            view.BodyTransform.localPosition = new Vector3(0f,
                normalized > 0.15f && normalized < 0.7f ? 1f / PixelsPerWorldUnit : 0f, 0f);
            if (normalized >= 1f)
                BeginState(view, EntityVisualState.Idle, 0f);
        }

        private void TickIdle(EntityView view)
        {
            int count = view.Asset.Definition.GetFrameCount(EntityVisualState.Idle);
            int frame = Mathf.FloorToInt((view.StateTime + view.IdlePhase)
                * view.Asset.Definition.IdleFPS) % count;
            view.Body.sprite = view.Asset.GetFrame(EntityVisualState.Idle, view.Facing, frame);
        }

        private EntityView AcquireView(
            Entity entity,
            EntityVisualAsset asset,
            int x,
            int y,
            EntityVisualFacing facing)
        {
            EntityView view = _pool.Count > 0 ? _pool.Pop() : CreateView();
            view.Entity = entity;
            view.Asset = asset;
            view.CellX = x;
            view.CellY = y;
            view.Facing = facing;
            view.State = EntityVisualState.Idle;
            view.StateTime = 0f;
            view.StateDuration = 0f;
            view.IdlePhase = StablePhase(entity?.ID ?? entity?.BlueprintName);
            view.Dying = false;
            view.Flat = _flatPixelMode;
            view.ForcedIdle = false;
            view.HasPendingReaction = false;
            view.Body.flipX = false;
            view.Body.sharedMaterial = view.Flat ? _flatMaterial : _sharedMaterial;
            view.BodyTransform.localPosition = Vector3.zero;
            view.RootTransform.position = CellAnchor(x, y);
            view.Root.name = "Visual_" + (entity?.BlueprintName ?? "Entity");
            view.LightTint = view.Flat ? Color.white : ResolveTint(entity, x, y);
            SetFlatFacing(view);
            view.Body.sprite = asset.GetFrame(EntityVisualState.Idle, facing, 0);
            ApplyBodyColor(view, false);
            UpdateSortingDepth(view);
            SetViewVisible(view, _presentationVisible);
            return view;
        }

        private EntityView CreateView()
        {
            var root = new GameObject("Visual_Pooled");
            root.transform.SetParent(transform, false);
            root.layer = gameObject.layer;

            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(root.transform, false);
            shadowObject.layer = gameObject.layer;
            shadowObject.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            var shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = _shadowSprite;
            shadow.sortingOrder = ShadowSortingOrder;
            shadow.color = new Color(0.10f, 0.13f, 0.11f, 0.55f);
            if (_sharedMaterial != null) shadow.sharedMaterial = _sharedMaterial;

            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform, false);
            bodyObject.layer = gameObject.layer;
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = BodySortingOrder;
            if (_sharedMaterial != null) body.sharedMaterial = _sharedMaterial;

            return new EntityView
            {
                Root = root,
                RootTransform = root.transform,
                BodyTransform = bodyObject.transform,
                Body = body,
                Shadow = shadow,
            };
        }

        private void ReleaseView(Entity entity)
        {
            if (entity == null || !_views.TryGetValue(entity, out EntityView view))
                return;

            QueueFlatRepaint(view);
            _views.Remove(entity);
            view.HasPendingReaction = false;
            view.Entity = null;
            view.Asset = null;
            view.Dying = false;
            view.Body.sprite = null;
            view.Body.color = Color.white;
            view.BodyTransform.localPosition = Vector3.zero;
            SetViewVisible(view, false);

            if (_pool.Count < MaxPooledViews)
                _pool.Push(view);
            else
                DestroyOwned(view.Root);
        }

        private void OnMoved(
            Entity entity,
            Zone zone,
            int oldX,
            int oldY,
            int newX,
            int newY,
            bool forced)
        {
            if (_flatPixelMode)
            {
                if (TryFlatEventView(entity, zone, false, out var flatView))
                    MoveFlatView(flatView, oldX, oldY, newX, newY, forced);
                return;
            }
            if (!ReferenceEquals(zone, _zone) || entity == null) return;
            if (!_views.TryGetValue(entity, out EntityView view)) return;

            view.CellX = newX;
            view.CellY = newY;
            view.Facing = entity.GetPart<RenderPart>()?.VisualFacing ?? view.Facing;
            view.MoveStart = oldX >= 0 && oldY >= 0
                ? CellAnchor(oldX, oldY)
                : view.RootTransform.position;
            view.MoveEnd = CellAnchor(newX, newY);
            view.StateDuration = view.Asset.Definition.GetDuration(EntityVisualState.Walk)
                * (forced ? 0.8f : 1f);
            BeginState(view, EntityVisualState.Walk, view.StateDuration);
            UpdateSortingDepth(view);
        }

        private void OnAttack(Entity attacker, Entity defender, Zone zone)
        {
            if (_flatPixelMode)
            {
                if (TryFlatEventView(attacker, zone, false, out var flatView))
                {
                    flatView.Facing = attacker.GetPart<RenderPart>().VisualFacing;
                    SetFlatFacing(flatView);
                    RequestFlatReaction(flatView, EntityVisualState.Attack);
                }
                return;
            }
            if (!ReferenceEquals(zone, _zone) || attacker == null) return;
            if (!_views.TryGetValue(attacker, out EntityView view) || view.Dying) return;

            view.Facing = attacker.GetPart<RenderPart>()?.VisualFacing ?? view.Facing;
            Cell source = zone?.GetEntityCell(attacker);
            Cell target = zone?.GetEntityCell(defender);
            if (source != null && target != null)
            {
                Vector2 delta = new Vector2(target.X - source.X, target.Y - source.Y);
                view.AttackDirection = delta.sqrMagnitude > 0f ? delta.normalized : Vector2.zero;
            }
            else
            {
                view.AttackDirection = Vector2.zero;
            }
            BeginState(view, EntityVisualState.Attack,
                view.Asset.Definition.GetDuration(EntityVisualState.Attack));
        }

        private void OnDamage(Entity target, Entity source, Zone zone, int amount, bool lethal)
        {
            if (_flatPixelMode)
            {
                if (amount > 0 && TryFlatEventView(target, zone, lethal, out var flatView))
                    RequestFlatReaction(flatView, EntityVisualState.Hurt);
                return;
            }
            if (!ReferenceEquals(zone, _zone) || target == null || amount <= 0) return;
            if (!_views.TryGetValue(target, out EntityView view) || view.Dying) return;
            BeginState(view, EntityVisualState.Hurt,
                view.Asset.Definition.GetDuration(EntityVisualState.Hurt));
        }

        private void OnCast(Entity caster, Zone zone, string spellID,
            int sourceX, int sourceY, int targetX, int targetY, float duration)
        {
            if (_flatPixelMode)
            {
                // No casting pose was authored; do not misrepresent it as melee.
                if (TryFlatEventView(caster, zone, false, out var flatView))
                {
                    var cell = zone.GetEntityCell(caster);
                    flatView.CellX = cell.X; flatView.CellY = cell.Y;
                    flatView.RootTransform.position = CellAnchor(cell.X, cell.Y);
                    flatView.Facing = caster.GetPart<RenderPart>().VisualFacing;
                    flatView.HasPendingReaction = false;
                    BeginState(flatView, EntityVisualState.Idle, 0f);
                    UpdateSortingDepth(flatView);
                }
                return;
            }
            if (!_presentationVisible || !ReferenceEquals(zone, _zone) || caster == null) return;
            if (!_views.TryGetValue(caster, out EntityView view) || view.Dying) return;
            Cell source = zone.GetCell(sourceX, sourceY);
            if (source == null || !source.IsVisible || !source.Explored) return;
            view.Facing = caster.GetPart<RenderPart>()?.VisualFacing ?? view.Facing;
            BeginState(view, EntityVisualState.Cast, Mathf.Clamp(duration, 0.01f, 2f));
            TickCast(view);
        }

        private void OnDeath(Entity target, Entity killer, Zone zone, int x, int y)
        {
            if (_flatPixelMode)
            {
                // Native death emits while the dead owner is still in its cell.
                if (!TryFlatEventView(target, zone, true, out var flatView)
                    || !ValidFlatOwner(target, x, y, true)) return;
                flatView.CellX = x; flatView.CellY = y;
                flatView.RootTransform.position = CellAnchor(x, y);
                flatView.HasPendingReaction = false;
                BeginState(flatView, EntityVisualState.Hurt, DeathDuration);
                flatView.Dying = true;
                UpdateSortingDepth(flatView);
                return;
            }
            if (!ReferenceEquals(zone, _zone) || target == null) return;
            if (!_views.TryGetValue(target, out EntityView view)) return;
            view.CellX = x;
            view.CellY = y;
            view.RootTransform.position = CellAnchor(x, y);
            view.State = EntityVisualState.Hurt;
            view.StateTime = 0f;
            view.StateDuration = DeathDuration;
            view.Dying = true;
            UpdateSortingDepth(view);
        }

        private static void BeginState(EntityView view, EntityVisualState state, float duration)
        {
            view.State = state;
            view.StateTime = 0f;
            view.StateDuration = duration;
            view.BodyTransform.localPosition = Vector3.zero;
            view.Body.enabled = true;
            view.Shadow.enabled = !view.Flat;
            if (view.Flat)
            {
                view.ForcedIdle = false;
                view.Body.sprite = view.Asset.GetFrame(state, view.Facing, 0);
                SetFlatFacing(view);
            }
            ApplyBodyColor(view, false);
        }

        private static int FrameForNormalized(
            EntityView view,
            EntityVisualState state,
            float normalized)
        {
            int count = view.Asset.GetFrameCount(state);
            return Mathf.Min(count - 1, Mathf.FloorToInt(Mathf.Clamp01(normalized) * count));
        }

        private Color ResolveTint(Entity entity, int x, int y)
        {
            return _tintProvider != null ? _tintProvider(entity, x, y) : Color.white;
        }

        private static void ApplyBodyColor(EntityView view, bool flash)
        {
            view.Body.color = view.Flat ? Color.white : flash
                ? Color.Lerp(view.LightTint, new Color(1f, 0.48f, 0.38f, 1f), 0.68f)
                : view.LightTint;
        }

        private void SetViewVisible(EntityView view, bool visible)
        {
            view.Root.SetActive(visible);
            if (!visible) return;
            view.Body.enabled = true;
            view.Shadow.enabled = !view.Flat && _shadowSprite != null;
        }

        private Vector3 CellAnchor(int x, int zoneY)
        {
            int tileY = Zone.Height - 1 - zoneY;
            if (_worldTilemap == null)
                return new Vector3(x + 0.5f, tileY, -zoneY * 0.001f);

            var cell = new Vector3Int(x, tileY, 0);
            Vector3 origin = _worldTilemap.CellToWorld(cell);
            // Actor pivots are bottom-center, unlike tile sprites' center anchors.
            Vector3 horizontalStep = _worldTilemap.CellToWorld(cell + Vector3Int.right) - origin;
            Vector3 anchor = origin + horizontalStep * 0.5f;
            anchor.z = -zoneY * 0.001f;
            return anchor;
        }

        private static void UpdateSortingDepth(EntityView view)
        {
            Vector3 position = view.RootTransform.position;
            position.z = -view.CellY * 0.001f;
            view.RootTransform.position = position;
        }

        private static float PixelSnap(float value)
        {
            return Mathf.Round(value * PixelsPerWorldUnit) / PixelsPerWorldUnit;
        }

        private static Vector3 PixelSnap(Vector3 value)
        {
            value.x = PixelSnap(value.x);
            value.y = PixelSnap(value.y);
            return value;
        }

        private static float StablePhase(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                        hash = (hash ^ value[i]) * 16777619;
                }
                return (hash & 1023) / 1024f;
            }
        }

        private void OnDisable()
        {
            if (!_flatPixelMode) return;
            _flatResumeOwners.Clear();
            foreach (var view in _views.Values)
                if (!view.Dying) _flatResumeOwners.Add(view.Entity);
            ClearViews();
        }

        private void OnEnable()
        {
            if (!_initialized || !_flatPixelMode) return;
            // Rediscover only the current cells of previously tracked owners.
            // Component re-enable does not need a whole-zone scan or refresh.
            for (int i = 0; i < _flatResumeOwners.Count; i++)
            {
                var cell = _zone?.GetEntityCell(_flatResumeOwners[i]);
                if (cell != null) QueueFlatRepaint(cell.X, cell.Y);
            }
            _flatResumeOwners.Clear();
            FlushFlatRepaints();
        }

        private static void DestroyOwned(UnityEngine.Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
        }

        private void OnDestroy()
        {
            EntityVisualHooks.MovedCallback -= OnMoved;
            EntityVisualHooks.AttackCallback -= OnAttack;
            EntityVisualHooks.CastCallback -= OnCast;
            EntityVisualHooks.DamageCallback -= OnDamage;
            EntityVisualHooks.DeathCallback -= OnDeath;

            ClearViews();
            while (_pool.Count > 0) DestroyOwned(_pool.Pop().Root);
            _flatMotionCatalog?.Dispose();
            _flatMotionCatalog = null;
            DestroyOwned(_flatMaterial);
            DestroyOwned(_sharedMaterial);
        }
    }
}
