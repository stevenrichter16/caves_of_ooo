using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public sealed partial class AnimatedEntityRenderer
    {
        private const float MinimumFlatPoseExposure = .06f;
        private const float FlatRecoveryDuration = .04f;
        private const float ReadoutBoundsEpsilon = .0001f;
        private bool _flatPixelMode;
        private FlatPixelActorMotionCatalog _flatMotionCatalog;
        private FlatPixelPatchbearerMotionCatalog _flatPatchbearerMotionCatalog;
        private Material _flatMaterial;
        private readonly HashSet<int> _flatRepaintCells = new HashSet<int>();
        private readonly List<Entity> _flatResumeOwners = new List<Entity>();

        /// <summary>Read-only supplement to logical readout occupancy. An exact
        /// current native walking body can still cover a departed cell. Check
        /// actual visible bounds, including partial overlap, without admitting
        /// hidden actors, discovering new views, or changing their ownership.
        /// Edge contact alone does not occupy another cell.</summary>
        public bool HasDisclosedMovingBodyAt(Zone zone, int x, int y, Entity except)
        {
            if (this == null || !_initialized || !_flatPixelMode || !_presentationVisible || !enabled
                || !ReferenceEquals(zone, _zone) || _worldTilemap == null
                || !FlatPixelFallbackCatalog.IsCurrentGraph(_zone)) return false;
            var cell = _zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != _zone || !cell.Explored || !cell.IsVisible) return false;

            int tileY = Zone.Height - 1 - y;
            Vector3 lower = _worldTilemap.CellToWorld(new Vector3Int(x, tileY, 0));
            Vector3 upper = _worldTilemap.CellToWorld(new Vector3Int(x + 1, tileY + 1, 0));
            float minX = Mathf.Min(lower.x, upper.x), maxX = Mathf.Max(lower.x, upper.x);
            float minY = Mathf.Min(lower.y, upper.y), maxY = Mathf.Max(lower.y, upper.y);
            foreach (var pair in _views)
            {
                var view = pair.Value;
                if (!view.Flat || !view.IsMoving || ReferenceEquals(view.Entity, except)
                    || !ValidFlatView(view) || view.Body == null || !view.Body.enabled
                    || !view.Body.gameObject.activeInHierarchy || view.Body.sprite == null
                    || !(view.Body.color.a > 0f)) continue;
                Bounds body = view.Body.bounds;
                if (body.max.x > minX + ReadoutBoundsEpsilon && body.min.x < maxX - ReadoutBoundsEpsilon
                    && body.max.y > minY + ReadoutBoundsEpsilon && body.min.y < maxY - ReadoutBoundsEpsilon) return true;
            }
            return false;
        }

        public void SetFlatPixelMode(bool active)
        {
            if (_flatPixelMode == active) return;
            ClearViews();
            _flatResumeOwners.Clear();
            _flatPixelMode = active;
            if (active && _flatMotionCatalog == null)
                _flatMotionCatalog = new FlatPixelActorMotionCatalog();
            if (active && _flatPatchbearerMotionCatalog == null)
                _flatPatchbearerMotionCatalog = new FlatPixelPatchbearerMotionCatalog();
            if (active && _flatMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null) _flatMaterial = new Material(shader)
                { name = "Flat opening actors (Runtime)", hideFlags = HideFlags.DontSave };
            }
        }

        /// <summary>Admission during the existing cell pass. Returning true is the
        /// commitment to draw this exact owner, so its static body may be suppressed.</summary>
        public bool ObserveFlatCell(Zone zone, Entity owner, int x, int y)
        {
            if (!_initialized || !_flatPixelMode || !_presentationVisible || !enabled
                || !ReferenceEquals(zone, _zone) || !ValidFlatOwner(owner, x, y, false)
                || !TryGetFlatAsset(owner, out var asset)) return false;
            if (_views.TryGetValue(owner, out var view) && !RefreshFlatAsset(view, asset))
            {
                ReleaseView(owner);
                view = null;
            }
            if (view == null)
            {
                var render = owner.GetPart<RenderPart>();
                view = AcquireView(owner, asset, x, y, render.VisualFacing);
                _views.Add(owner, view);
            }
            else
            {
                view.CellX = x; view.CellY = y;
                view.Facing = owner.GetPart<RenderPart>().VisualFacing;
                if (!view.IsMoving) view.RootTransform.position = CellAnchor(x, y);
                SetFlatFacing(view);
                UpdateSortingDepth(view);
                SetViewVisible(view, true);
            }
            return true;
        }

        private bool ValidFlatOwner(Entity owner, int x, int y, bool allowDead)
        {
            if (owner == null || !FlatPixelFallbackCatalog.IsCurrentGraph(_zone) || owner.SpatialZone != _zone) return false;
            var cell = _zone.GetEntityCell(owner);
            var physical = owner.GetPart<PhysicsPart>();
            if (cell == null || cell.ParentZone != _zone || !ReferenceEquals(_zone.GetCell(x, y), cell)
                || cell.X != x || cell.Y != y || !cell.IsVisible || !cell.Explored
                || !ReferenceEquals(cell.GetTopVisibleObject(), owner) || !cell.Objects.Contains(owner)
                || physical == null || physical.ParentEntity != owner
                || physical.InInventory != null || physical.Equipped != null
                || !TryGetFlatAsset(owner, out _)) return false;
            return allowDead || (!CombatSystem.IsDeathHandled(owner)
                && (owner.GetStat("Hitpoints") == null || owner.GetStatValue("Hitpoints") > 0));
        }

        private bool ValidFlatView(EntityView view)
        {
            if (!_presentationVisible || !enabled || !_flatPixelMode
                || !FlatPixelFallbackCatalog.IsCurrentGraph(_zone)) return false;
            if (!view.Dying)
            {
                var current = _zone.GetEntityCell(view.Entity);
                if (current == null || !ValidFlatOwner(view.Entity, current.X, current.Y, false)
                    || !TryGetFlatAsset(view.Entity, out var currentAsset)) return false;
                return RefreshFlatAsset(view, currentAsset);
            }
            var cell = _zone.GetCell(view.CellX, view.CellY);
            if (cell == null || !cell.IsVisible || !cell.Explored) return false;
            var replacement = cell.GetTopVisibleObject();
            return replacement == null || replacement == view.Entity
                || ((!replacement.HasTag("Creature") && !replacement.HasTag("Player"))
                    || CombatSystem.IsDeathHandled(replacement)
                    || (replacement.GetStat("Hitpoints") != null && replacement.GetStatValue("Hitpoints") <= 0));
        }

        /// <summary>Checks tracked views only, including quiet frames. A lost
        /// owner schedules bounded cells for the ordinary static pass to restore.</summary>
        public void ReconcileFlatViews() => ReconcileFlatViews(true);

        private void ReconcileFlatViews(bool flushRepaints)
        {
            if (!_flatPixelMode) return;
            _releaseScratch.Clear();
            foreach (var pair in _views)
                if (!ValidFlatView(pair.Value)) _releaseScratch.Add(pair.Key);
            for (int i = 0; i < _releaseScratch.Count; i++) ReleaseView(_releaseScratch[i]);
            if (flushRepaints) FlushFlatRepaints();
        }

        /// <summary>Post-pass validation does not mutate the dirty collection that
        /// the caller is about to clear. Any rare loss is flushed next frame.</summary>
        public void CompleteFlatCellPass() => ReconcileFlatViews(false);

        private void QueueFlatRepaint(EntityView view)
        {
            if (!view.Flat) return;
            QueueFlatRepaint(view.CellX, view.CellY);
            var current = _zone?.GetEntityCell(view.Entity);
            if (current != null) QueueFlatRepaint(current.X, current.Y);
        }
        private void QueueFlatRepaint(int x, int y)
        {
            if (x >= 0 && x < Zone.Width && y >= 0 && y < Zone.Height)
                _flatRepaintCells.Add(y * Zone.Width + x);
        }
        private void FlushFlatRepaints()
        {
            foreach (int cell in _flatRepaintCells)
                ZoneRenderHooks.MarkCellDirty(cell % Zone.Width, cell / Zone.Width, "flat actor ownership");
            _flatRepaintCells.Clear();
        }

        private bool TryFlatEventView(Entity entity, Zone zone, bool allowDead, out EntityView view)
        {
            view = null;
            if (!_presentationVisible || !enabled || !ReferenceEquals(zone, _zone)
                || entity == null || !_views.TryGetValue(entity, out view) || view.Dying) return false;
            var cell = zone.GetEntityCell(entity);
            if (cell != null && ValidFlatOwner(entity, cell.X, cell.Y, allowDead)
                && TryGetFlatAsset(entity, out var asset) && RefreshFlatAsset(view, asset))
            {
                return true;
            }
            ReleaseView(entity); view = null; return false;
        }

        private bool TryGetFlatAsset(Entity owner, out EntityVisualAsset asset)
        {
            asset = null;
            return (_flatMotionCatalog != null && _flatMotionCatalog.TryGetAsset(owner, out asset))
                || (_flatPatchbearerMotionCatalog != null && _flatPatchbearerMotionCatalog.TryGetAsset(_zone, owner, out asset));
        }

        /// <summary>Stock changes replace only the pose source. Preserve the
        /// current view, elapsed reaction, pending reaction and interpolated movement.</summary>
        private bool RefreshFlatAsset(EntityView view, EntityVisualAsset asset)
        {
            if (ReferenceEquals(view.Asset, asset)) return true;
            // Only this catalog's finite-supply pair may change in place.
            // Other identity changes retain their existing retire/reacquire policy.
            if (_flatPatchbearerMotionCatalog == null
                || !_flatPatchbearerMotionCatalog.CanRefresh(view.Asset, asset)) return false;
            view.Asset = asset;
            var state = view.State;
            int frame = 0;
            if (state == EntityVisualState.Walk)
            {
                if (view.ForcedIdle) state = EntityVisualState.Idle;
                else frame = Mathf.Min(1, Mathf.FloorToInt(Mathf.Clamp01(view.StateTime / Mathf.Max(.000001f, view.StateDuration)) * 2));
            }
            else if (state == EntityVisualState.Attack || state == EntityVisualState.Hurt)
                frame = view.StateTime < view.StateDuration - FlatRecoveryDuration ? 0 : 1;
            else state = EntityVisualState.Idle;
            view.Body.sprite = asset.GetFrame(state, view.Facing, frame);
            return true;
        }

        private void MoveFlatView(EntityView view, int oldX, int oldY, int newX, int newY, bool forced)
        {
            view.CellX = newX; view.CellY = newY;
            view.Facing = view.Entity.GetPart<RenderPart>().VisualFacing;
            view.MoveStart = view.RootTransform.position;
            view.MoveEnd = CellAnchor(newX, newY);
            view.HasPendingReaction = false;
            if (Mathf.Abs(newX-oldX) > 1 || Mathf.Abs(newY-oldY) > 1)
            {
                view.RootTransform.position = view.MoveEnd;
                BeginState(view, EntityVisualState.Idle, 0f);
            }
            else
            {
                BeginState(view, EntityVisualState.Walk, forced ? .08f : .1f);
                view.ForcedIdle = forced;
                view.Body.sprite = view.Asset.GetFrame(forced ? EntityVisualState.Idle : EntityVisualState.Walk, view.Facing, 0);
            }
            SetFlatFacing(view); UpdateSortingDepth(view);
        }

        private void RequestFlatReaction(EntityView view, EntityVisualState state)
        {
            if (view.State == state) return;
            if ((view.State == EntityVisualState.Attack || view.State == EntityVisualState.Hurt)
                && view.StateTime < MinimumFlatPoseExposure)
            {
                view.PendingReaction = state;
                view.HasPendingReaction = true;
                return;
            }
            var cell = _zone.GetEntityCell(view.Entity);
            view.CellX = cell.X; view.CellY = cell.Y;
            view.RootTransform.position = CellAnchor(cell.X, cell.Y);
            view.HasPendingReaction = false;
            BeginState(view, state, view.Asset.Definition.GetDuration(state));
        }

        private void TickFlatView(EntityView view, float deltaTime)
        {
            if (view.HasPendingReaction && view.StateTime + deltaTime >= MinimumFlatPoseExposure)
            {
                float remaining = Mathf.Max(0f, deltaTime - Mathf.Max(0f, MinimumFlatPoseExposure - view.StateTime));
                var next = view.PendingReaction; view.HasPendingReaction = false;
                BeginState(view, next, view.Asset.Definition.GetDuration(next));
                deltaTime = remaining;
            }
            view.StateTime += deltaTime;
            view.Shadow.enabled = false;
            view.BodyTransform.localPosition = Vector3.zero;
            if (view.Dying)
            {
                view.Body.sprite = view.Asset.GetFrame(EntityVisualState.Hurt, view.Facing, 0);
                view.Body.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(view.StateTime / DeathDuration));
                return;
            }
            view.Body.color = Color.white;
            if (view.State == EntityVisualState.Walk)
            {
                float t = view.StateTime + .000001f >= view.StateDuration
                    ? 1f : Mathf.Clamp01(view.StateTime / view.StateDuration);
                view.RootTransform.position = PixelSnap(Vector3.LerpUnclamped(view.MoveStart, view.MoveEnd, t*t*(3f-2f*t)));
                view.Body.sprite = view.Asset.GetFrame(view.ForcedIdle ? EntityVisualState.Idle : EntityVisualState.Walk,
                    view.Facing, view.ForcedIdle ? 0 : Mathf.Min(1, Mathf.FloorToInt(t*2)));
                if (t >= 1f) BeginState(view, EntityVisualState.Idle, 0f);
            }
            else if (view.State == EntityVisualState.Attack || view.State == EntityVisualState.Hurt)
            {
                view.Body.sprite = view.Asset.GetFrame(view.State, view.Facing,
                    view.StateTime < view.StateDuration - FlatRecoveryDuration ? 0 : 1);
                if (view.StateTime >= view.StateDuration) BeginState(view, EntityVisualState.Idle, 0f);
            }
            else TickIdle(view);
        }

        private static void SetFlatFacing(EntityView view)
        {
            if (!view.Flat) return;
            // All four authored canonical poses face East. North/south retain
            // their last horizontal orientation, without invented direction rows.
            if (view.Facing == EntityVisualFacing.West) view.Body.flipX = true;
            else if (view.Facing == EntityVisualFacing.East) view.Body.flipX = false;
        }
    }
}
