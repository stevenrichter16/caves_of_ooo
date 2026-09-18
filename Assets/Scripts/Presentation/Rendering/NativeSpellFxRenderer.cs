using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CavesOfOoo.Rendering
{
    /// <summary>Actual cold readiness costs; retained unchanged by idempotent calls.
    /// These seconds describe work moved before casting, not work eliminated.</summary>
    [Serializable]
    public sealed class NativeSpellFxPreparation
    {
        public double LoadSeconds, ValidationSeconds, PoolSeconds;
        public int ViewCount;
        public bool UsedCachedLibrary;
    }

    /// <summary>Plays borrowed Blender pieces against copied resolution geometry. The
    /// coordinator owns the queue and clock; this backend owns only transient views.</summary>
    public sealed class NativeSpellFxRenderer : IDisposable
    {
        public const int MaximumScheduledFragments = 768;
        public const int MaximumFullMeshes = 384;
        public const int MaximumReducedMeshes = 96;
        internal const float LongFrameRecoverySeconds = .4f;
        private const float ContactRecoveryInsetSeconds = .0001f;
        private const int MaximumCasts = 64;
        private static readonly int Emission = Shader.PropertyToID("_Emission");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private NativeSpellFxLibrary _library;
        private NativeZone3DRenderSurface _surface;
        private Zone _zone;
        private Transform _root;
        private Material _material, _luminousMaterial, _glowMaterial;
        private readonly List<Cast> _casts = new List<Cast>(MaximumCasts);
        private readonly List<View> _views = new List<View>(MaximumFullMeshes);
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private bool _disposed, _preparationFailed;
        private int _scheduled, _drawn;
        private SpellFxMode _mode = SpellFxMode.Full;

        private sealed class Cast
        {
            public SpellFxSequence Sequence;
            public NativeSpellFxEntry Entry;
            public Point[] Path;
            public Vector3[] Route;
            public float[] Distances;
            public float Length, Age, Contact, Duration;
            public Quaternion Facing;
            public Vector3 Forward;
            public int Dx, Dy;
            public readonly List<Fragment> Fragments = new List<Fragment>();
        }
        private struct Fragment { public NativeSpellFxPiece Piece; public Point Cell; }
        private sealed class View { public Transform Transform; public MeshFilter Filter; public MeshRenderer Renderer; public Material Material; }

        public NativeSpellFxRenderer(NativeSpellFxLibrary library = null) { _library = library; }
        public Transform Root => _root;
        public int ActiveCount => _scheduled;
        public int ActiveMeshCount => _drawn;
        public int AllocatedViewCount => _views.Count;
        public string Failure { get; private set; }
        /// <summary>Exact contact for the last accepted, filtered visual route.</summary>
        public float LastContactSeconds { get; private set; }
        public Point LastContactCell { get; private set; }
        public NativeSpellFxEntry LastEntry { get; private set; }
        public NativeSpellFxPreparation LastPreparation { get; private set; }
        public bool IsPrepared => !_disposed && SpellFxSettings.Mode != SpellFxMode.Off && SurfaceReady
            && _zone != null && _library != null && _library.IsValidated && _root != null && MaterialsReady && _views.Count >= Budget;
        private bool MaterialsReady => _material != null && _library != null
            && (_library.LuminousMaterial == null || _luminousMaterial != null)
            && (_library.GlowMaterial == null || _glowMaterial != null);
        internal int CancellationVersion { get; private set; }
        public bool HasBlockingFx
        {
            get
            {
                if (_disposed || SpellFxSettings.Mode == SpellFxMode.Off || !SurfaceReady || _root == null) return false;
                for (int i = 0; i < _casts.Count; i++) if (_casts[i].Sequence.BlocksTurnAdvance) return true;
                return false;
            }
        }
        private bool SurfaceReady => _surface != null && _surface.IsVisible && _surface.ContentRoot != null && _surface.FogTexture != null;

        public void SetZone(Zone zone)
        {
            if (_disposed || ReferenceEquals(_zone, zone)) return;
            Cancel("zone-change"); _zone = zone; _preparationFailed = false;
        }
        public void SetSurface(NativeZone3DRenderSurface surface)
        {
            if (_disposed || ReferenceEquals(_surface, surface)) return;
            Cancel("surface-change"); DestroyOwned(); _surface = surface; _preparationFailed = false;
        }

        /// <summary>Readiness work for a visible native surface before commands.
        /// The coordinator calls this while binding/enabling, never by replaying a cast.</summary>
        public bool Prepare()
        {
            if (IsPrepared) return true;
            if (_disposed || _preparationFailed || _zone == null || !SurfaceReady || SpellFxSettings.Mode == SpellFxMode.Off) return false;
            var receipt = new NativeSpellFxPreparation { UsedCachedLibrary = _library != null };
            long started = Stopwatch.GetTimestamp();
            try
            {
                if (_library == null) _library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
                receipt.LoadSeconds = Since(started); started = Stopwatch.GetTimestamp();
                if (_library == null) throw new InvalidOperationException("library-unavailable");
                // Find's cache guard validates once, without rebuilding existing entries.
                _library.Find(null);
                receipt.ValidationSeconds = Since(started); started = Stopwatch.GetTimestamp();
                if (_root == null)
                {
                    // External hierarchy loss must cancel its old waits before rebuilding.
                    if (_casts.Count > 0) Cancel("hierarchy-destroyed");
                    DestroyOwned();
                }
                EnsureRoot();
                while (_views.Count < Budget) CreateView();
                receipt.PoolSeconds = Since(started); receipt.ViewCount = _views.Count;
                LastPreparation = receipt; Failure = null;
                if (Diag.IsChannelEnabled("effect")) Diag.Record("effect", "NativeSpellPrepared", payload: new
                { zoneId = _zone.ZoneID, backend = "native", receipt.LoadSeconds, receipt.ValidationSeconds,
                    receipt.PoolSeconds, receipt.ViewCount, receipt.UsedCachedLibrary });
                return true;
            }
            catch (Exception exception)
            {
                _preparationFailed = true; Failure = exception.Message; LastPreparation = receipt;
                // Never retain a partially constructed readiness pool or an old wait.
                Cancel("preparation-failed"); DestroyOwned();
                if (Diag.IsChannelEnabled("effect")) Diag.Record("effect", "NativeSpellPreparationRejected", payload: new
                { zoneId = _zone.ZoneID, backend = "native", reason = Failure });
                return false;
            }
        }
        private static double Since(long start) => (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;

        public float Play(SpellFxSequence sequence)
        {
            LastEntry = null; Failure = null; LastContactSeconds = 0; LastContactCell = new Point(-1,-1);
            if (_disposed) return Refuse(sequence, "disposed");
            if (sequence == null || _zone == null || sequence.Zone != _zone) return Refuse(sequence, "wrong-zone");
            if (SpellFxSettings.Mode == SpellFxMode.Off) return Refuse(sequence, "mode-off");
            if (!SurfaceReady) return Refuse(sequence, "native-surface-unavailable");
            if (!InBounds(sequence.Source)) return Refuse(sequence, "invalid-source");
            if (_casts.Count >= MaximumCasts || _scheduled >= MaximumScheduledFragments) return Refuse(sequence, "schedule-budget");
            try
            {
                if (_library == null) _library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
                if (_library == null) return Refuse(sequence, "library-unavailable");
                var entry = _library.Find(sequence.SpellId);
                if (entry == null) return Refuse(sequence, "unknown-spell");
                _preparationFailed = false;
                if (_root == null && _casts.Count > 0) { Cancel("hierarchy-destroyed"); DestroyOwned(); }
                if (_root != null && !MaterialsReady && _casts.Count > 0) Cancel("material-destroyed");
                if (_mode != SpellFxSettings.Mode) Cancel("mode-change");
                _mode = SpellFxSettings.Mode;
                var cast = BuildCast(sequence, entry);
                Schedule(cast);
                EnsureRoot();
                int capacity = Math.Min(Budget, _scheduled + cast.Fragments.Count);
                while (_views.Count < capacity) CreateView();
                _casts.Add(cast); _scheduled += cast.Fragments.Count;
                LastEntry = entry;
                LastContactSeconds = cast.Contact;
                LastContactCell = cast.Path.Length > 0 ? cast.Path[cast.Path.Length-1] : new Point(-1,-1);
                Trace("NativeSpellScheduled", sequence, cast.Fragments.Count, "accepted", cast.Duration);
                return cast.Duration;
            }
            catch (Exception exception)
            {
                // Asset rejection uses the diagnostic channel, not a warning that can
                // poison an unrelated gameplay LogAssert. Existing routes remain usable.
                return Refuse(sequence, exception.Message);
            }
        }

        private Cast BuildCast(SpellFxSequence sequence, NativeSpellFxEntry entry)
        {
            var path = new List<Point>();
            var seen = new HashSet<Point>();
            foreach (var p in sequence.Path)
                if (InBounds(p) && !p.Equals(sequence.Source) && seen.Add(p)) path.Add(p);
            Point aim = sequence.Source;
            // Hands records its selected cell first, before resolving pending world
            // reactions. Those reactions may observe an unrelated target elsewhere.
            if (sequence.SpellId == "Pyromancy_FlamingHands" && sequence.AffectedCells.Count > 0
                && InBounds(sequence.AffectedCells[0])) aim = sequence.AffectedCells[0];
            else if (path.Count > 0) aim = path[path.Count - 1];
            else
            {
                foreach (var target in sequence.Targets)
                    if (target != null && InBounds(target.Cell) && !target.Cell.Equals(sequence.Source)) { aim = target.Cell; break; }
                if (aim.Equals(sequence.Source))
                    foreach (var p in sequence.AffectedCells)
                        if (InBounds(p) && !p.Equals(sequence.Source)) { aim = p; break; }
            }
            int dx = Math.Sign(aim.X - sequence.Source.X), dy = Math.Sign(aim.Y - sequence.Source.Y);
            if (dx == 0 && dy == 0) dy = -1;
            var forward = new Vector3(dx, 0, -dy).normalized;
            var cast = new Cast { Sequence = sequence, Entry = entry, Path = path.ToArray(), Dx = dx, Dy = dy,
                Forward = forward, Facing = Quaternion.LookRotation(forward, Vector3.up) };
            float release = entry.ReleaseFrame / entry.SampleRate;
            bool travels = entry.StudyContactFrame > entry.ReleaseFrame;
            cast.Contact = release + (travels ? Mathf.Min(.65f, path.Count * .025f) : 0f);
            cast.Duration = cast.Contact + (entry.StudyClearFrame - entry.StudyContactFrame) / entry.SampleRate;
            // The route contains only recorded geometry. A target-only record can use
            // its copied contact, but no wall/range query or live entity lookup occurs.
            var route = new List<Vector3> { Centre(sequence.Source) };
            foreach (var p in path) route.Add(Centre(p));
            if (route.Count == 1 && !aim.Equals(sequence.Source)) route.Add(Centre(aim));
            cast.Route = route.ToArray(); cast.Distances = new float[route.Count];
            for (int i = 1; i < route.Count; i++)
            { cast.Length += Vector3.Distance(route[i - 1], route[i]); cast.Distances[i] = cast.Length; }
            return cast;
        }

        private void Schedule(Cast cast)
        {
            var sequence = cast.Sequence;
            var affected = new HashSet<Point>(sequence.AffectedCells);
            var targetIds = new HashSet<string>(StringComparer.Ordinal);
            var anonymous = new HashSet<SpellFxTargetResult>();
            var targets = new List<SpellFxTargetResult>();
            foreach (var target in sequence.Targets)
                if (target != null && InBounds(target.Cell) && (string.IsNullOrEmpty(target.TargetId)
                    ? anonymous.Add(target) : targetIds.Add(target.TargetId))) targets.Add(target);
            var reactions = new HashSet<Point>();
            foreach (var reaction in sequence.Reactions)
                if (reaction != null && InBounds(reaction.Cell) && reaction.Kind == "reaction"
                    && reaction.Value == "freeze_water" && reaction.Amount > 0) reactions.Add(reaction.Cell);
            // Reserve bounded schedule capacity for the readable shape before motes.
            for (int priority = 0; priority < 2; priority++)
            foreach (var piece in cast.Entry.Pieces)
            {
                if (piece.ReducedEssential != (priority == 0)) continue;
                if (_mode == SpellFxMode.Reduced && !piece.ReducedEssential) continue;
                switch (piece.Role)
                {
                    case NativeSpellFxRole.SourceGather:
                    case NativeSpellFxRole.ProjectileHead:
                    case NativeSpellFxRole.ProjectileTrail:
                        if (piece.Condition == NativeSpellFxCondition.Always) Add(cast, piece, sequence.Source);
                        break;
                    case NativeSpellFxRole.TargetImpact:
                        foreach (var target in targets)
                        {
                            // These single-body spells finish at the copied first
                            // physical contact. Other targets can be honest ambient
                            // reaction results, but are not extra projectile impacts.
                            if ((sequence.SpellId == "Pyromancy_EmberSpit" || sequence.SpellId == "Cryomancy_RimeGrip")
                                && (cast.Path.Length == 0 || !target.Cell.Equals(cast.Path[cast.Path.Length - 1]))) continue;
                            if (Matches(piece.Condition, target)) Add(cast, piece, target.Cell);
                        }
                        break;
                    case NativeSpellFxRole.CropCell:
                        foreach (var target in targets) if (Matches(piece.Condition, target)) Add(cast, piece, target.Cell);
                        break;
                    case NativeSpellFxRole.GroundCell:
                        // ForwardCell is the authored variant's ordinal, not a world
                        // offset: truncated/blocked native paths stay truncated.
                        int index = piece.ForwardCell - 1;
                        if (index >= 0 && index < cast.Path.Length) Add(cast, piece, cast.Path[index]);
                        break;
                    case NativeSpellFxRole.ConeCell:
                        var cell = new Point(sequence.Source.X + cast.Dx * piece.ForwardCell - cast.Dy * piece.LateralCell,
                            sequence.Source.Y + cast.Dy * piece.ForwardCell + cast.Dx * piece.LateralCell);
                        if (affected.Contains(cell)) Add(cast, piece, cell);
                        break;
                    case NativeSpellFxRole.ReactionCell:
                        if (piece.Condition == NativeSpellFxCondition.FreezeWater)
                            foreach (var cellReaction in reactions) Add(cast, piece, cellReaction);
                        break;
                }
            }
        }
        private void Add(Cast cast, NativeSpellFxPiece piece, Point cell)
        {
            if (InBounds(cell) && _scheduled + cast.Fragments.Count < MaximumScheduledFragments)
                cast.Fragments.Add(new Fragment { Piece = piece, Cell = cell });
        }
        private static bool Matches(NativeSpellFxCondition condition, SpellFxTargetResult target)
        {
            switch (condition)
            {
                case NativeSpellFxCondition.Always: return true;
                case NativeSpellFxCondition.FrozenApplied: return !target.Died && Contains(target.AppliedEffects, "FrozenEffect");
                case NativeSpellFxCondition.PacifiedApplied: return !target.Died && Contains(target.AppliedEffects, "Pacified");
                case NativeSpellFxCondition.Rejected: return target.RejectedEffects.Count > 0;
                case NativeSpellFxCondition.Resisted: return target.Resisted;
                case NativeSpellFxCondition.Died: return target.Died;
                default: return false;
            }
        }
        private static bool Contains(IReadOnlyList<string> items, string value)
        { for (int i = 0; i < items.Count; i++) if (items[i] == value) return true; return false; }

        /// <summary>Choose one native clock step after a long wall frame that would
        /// wholly skip an unseen contact interval. The coordinator applies this same
        /// step to native handles; its raw wall timeout and other backends stay intact.
        /// This does not promise a minimum visible duration under repeated hitches.</summary>
        internal float RecoverContactDelta(float wallDelta, float playbackDelta)
        {
            if (wallDelta < LongFrameRecoverySeconds || wallDelta >= WorldFxPlayback.HardTimeoutSeconds
                || _disposed || _casts.Count == 0 || !SurfaceReady || _root == null || !MaterialsReady)
                return playbackDelta;
            float recovered = playbackDelta;
            for (int i = 0; i < _casts.Count; i++)
            {
                var cast = _casts[i];
                if (cast.Age >= cast.Contact || cast.Age + playbackDelta < cast.Duration
                    || !HasVisibleContactPose(cast)) continue;
                // Land just inside contact so floating-point subtraction/addition
                // cannot leave TargetImpact behind its strict contact gate.
                recovered = Mathf.Min(recovered, cast.Contact - cast.Age + ContactRecoveryInsetSeconds);
            }
            return recovered;
        }

        private bool HasVisibleContactPose(Cast cast)
        {
            float frame = cast.Entry.StudyContactFrame;
            int lo = Mathf.Clamp(Mathf.FloorToInt(frame), 0, NativeSpellFxLibrary.SampleCount - 1);
            int hi = Math.Min(lo + 1, NativeSpellFxLibrary.SampleCount - 1);
            float t = Mathf.Clamp01(frame - lo);
            for (int i = 0; i < cast.Fragments.Count; i++)
            {
                var fragment = cast.Fragments[i]; var piece = fragment.Piece;
                if (piece.Role == NativeSpellFxRole.SourceGather || piece.Mesh == null) continue;
                var a = piece.Poses[lo]; var b = piece.Poses[hi];
                Vector3 scale = Vector3.LerpUnclamped(a.Scale, b.Scale, t);
                if (scale.x <= .0001f || scale.y <= .0001f || scale.z <= .0001f) continue;
                Vector3 position = Vector3.LerpUnclamped(a.Position, b.Position, t);
                Quaternion rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, t);
                PoseInWorld(cast, fragment, lo, hi, t, ref position, ref rotation, ref scale);
                if (Village3DProjection.TryWorldToCell(position, out int x, out int y) && Visible(x, y)
                    && (IsProjectile(piece.Role) || Visible(fragment.Cell.X, fragment.Cell.Y))) return true;
            }
            return false;
        }

        public void Update(float playbackDelta)
        {
            if (_disposed) return;
            if (_casts.Count == 0) return;
            if (_root == null) { Cancel("hierarchy-destroyed"); DestroyOwned(); return; }
            if (!SurfaceReady) { Cancel("surface-unavailable"); return; }
            if (!MaterialsReady) { Cancel("material-destroyed"); return; }
            if (_mode != SpellFxSettings.Mode) { Cancel("mode-change"); return; }
            playbackDelta = WorldFxPlayback.SanitizeDelta(playbackDelta);
            for (int i = _casts.Count - 1; i >= 0; i--)
            {
                var cast = _casts[i]; cast.Age += playbackDelta;
                if (cast.Age >= cast.Duration)
                {
                    _scheduled -= cast.Fragments.Count;
                    _casts.RemoveAt(i);
                    Trace("NativeSpellCompleted", cast.Sequence, cast.Fragments.Count, "clear", cast.Duration);
                }
            }
            int drawn = 0;
            // Every accepted cast's essential shapes precede optional detail across
            // all casts. An older particle cloud cannot consume every draw slot.
            for (int priority = 0; priority < 2 && drawn < Budget; priority++)
            for (int c = 0; c < _casts.Count && drawn < Budget; c++)
            {
                var cast = _casts[c];
                float frame = StudyFrame(cast);
                int lo = Mathf.Clamp(Mathf.FloorToInt(frame), 0, NativeSpellFxLibrary.SampleCount - 1);
                int hi = Math.Min(lo + 1, NativeSpellFxLibrary.SampleCount - 1);
                float t = Mathf.Clamp01(frame - lo);
                for (int i = 0; i < cast.Fragments.Count && drawn < Budget; i++)
                {
                    var fragment = cast.Fragments[i]; var piece = fragment.Piece;
                    if (piece.ReducedEssential != (priority == 0)) continue;
                    if (piece.Role == NativeSpellFxRole.TargetImpact && cast.Age < cast.Contact) continue;
                    var a = piece.Poses[lo]; var b = piece.Poses[hi];
                    Vector3 scale = Vector3.LerpUnclamped(a.Scale, b.Scale, t);
                    if (scale.x <= .0001f || scale.y <= .0001f || scale.z <= .0001f) continue;
                    Vector3 position = Vector3.LerpUnclamped(a.Position, b.Position, t);
                    Quaternion rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, t);
                    PoseInWorld(cast, fragment, lo, hi, t, ref position, ref rotation, ref scale);
                    if (!Village3DProjection.TryWorldToCell(position, out int x, out int y)
                        || !Visible(x, y) || (!IsProjectile(piece.Role) && !Visible(fragment.Cell.X, fragment.Cell.Y))) continue;
                    if (drawn >= _views.Count) break; // Views are prepared ahead of casting; direct fallback users allocate only at acceptance.
                    var view = _views[drawn++];
                    view.Transform.SetPositionAndRotation(position, rotation); view.Transform.localScale = scale;
                    view.Filter.sharedMesh = piece.Mesh;
                    float flash = cast.Age >= cast.Contact && cast.Age < cast.Contact + .05f
                        ? SpellFxSettings.FlashIntensity * .12f : 0f;
                    Color color = Color.LerpUnclamped(piece.Color, Color.white, flash); color.a = piece.Color.a;
                    // Imported swatches and flash interpolation are already linear.
                    // A fresh Vector-typed entry avoids SetColor's extra sRGB conversion.
                    _properties.SetVector(BaseColor, (Vector4)color);
                    _properties.SetFloat(Emission, piece.Emission);
                    var material = piece.Glow ? _glowMaterial : piece.Emission > 0 ? _luminousMaterial : _material;
                    if (view.Material != material) { view.Renderer.sharedMaterial = material; view.Material = material; }
                    view.Renderer.SetPropertyBlock(_properties);
                    view.Renderer.enabled = true;
                }
            }
            for (int i = drawn; i < _drawn && i < _views.Count; i++) if (_views[i].Renderer != null) _views[i].Renderer.enabled = false;
            _drawn = drawn;
        }
        private static float StudyFrame(Cast cast)
        {
            var entry = cast.Entry;
            float release = entry.ReleaseFrame / entry.SampleRate;
            if (cast.Age < release) return cast.Age * entry.SampleRate;
            if (cast.Age < cast.Contact && cast.Contact > release)
                return Mathf.LerpUnclamped(entry.ReleaseFrame, entry.StudyContactFrame, (cast.Age - release) / (cast.Contact - release));
            return entry.StudyContactFrame + (cast.Age - cast.Contact) * entry.SampleRate;
        }
        private static bool IsProjectile(NativeSpellFxRole role) => role == NativeSpellFxRole.ProjectileHead || role == NativeSpellFxRole.ProjectileTrail;
        private static void PoseInWorld(Cast cast, Fragment fragment, int lo, int hi, float t,
            ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            var piece = fragment.Piece;
            Quaternion facing = cast.Facing;
            if (IsProjectile(piece.Role))
            {
                float progress = piece.Role == NativeSpellFxRole.ProjectileHead
                    ? Mathf.LerpUnclamped(cast.Entry.ProjectileProgress[lo], cast.Entry.ProjectileProgress[hi], t)
                    : position.z / cast.Entry.AuthoredDistanceCells;
                Vector3 origin = AlongRoute(cast, progress, out Vector3 tangent);
                facing = Quaternion.LookRotation(tangent, Vector3.up);
                float residual = piece.Role == NativeSpellFxRole.ProjectileHead
                    ? position.z - progress * cast.Entry.AuthoredDistanceCells : 0f;
                position = origin + facing * new Vector3(position.x, position.y, residual);
            }
            else
            {
                bool perCell = piece.Role == NativeSpellFxRole.GroundCell || piece.Role == NativeSpellFxRole.ConeCell
                    || piece.Role == NativeSpellFxRole.CropCell || piece.Role == NativeSpellFxRole.ReactionCell;
                if (perCell && piece.VisualBounds == NativeSpellFxBounds.CellSurface && cast.Dx != 0 && cast.Dy != 0)
                {
                    // A diagonal square rotated 45 degrees needs this horizontal fit
                    // to retain the exporter's cell bounds. Height remains authored.
                    const float diagonalFit = .70710678118f;
                    position.x *= diagonalFit; position.z *= diagonalFit;
                    scale.x *= diagonalFit; scale.z *= diagonalFit;
                }
                position = Centre(fragment.Cell) + facing * position;
            }
            rotation = facing * rotation;
        }
        private static Vector3 AlongRoute(Cast cast, float progress, out Vector3 tangent)
        {
            tangent = cast.Forward;
            float authoredDistance = cast.Entry.AuthoredDistanceCells;
            float launch = Math.Min(cast.Entry.LaunchDistance, authoredDistance);
            float studyDistance = progress * authoredDistance;
            // The hand socket keeps its real metre offset for short and long casts.
            float distance = studyDistance <= launch || authoredDistance <= launch
                ? studyDistance : launch + (studyDistance - launch) / (authoredDistance - launch) * (cast.Length - launch);
            if (cast.Route.Length < 2) return cast.Route[0] + tangent * distance;
            for (int i = 1; i < cast.Route.Length; i++)
            {
                float segment = cast.Distances[i] - cast.Distances[i - 1];
                if (distance <= cast.Distances[i] || i == cast.Route.Length - 1)
                {
                    tangent = (cast.Route[i] - cast.Route[i - 1]) / segment;
                    return cast.Route[i - 1] + tangent * (distance - cast.Distances[i - 1]);
                }
            }
            return cast.Route[cast.Route.Length - 1];
        }

        private int Budget => SpellFxSettings.Mode == SpellFxMode.Reduced ? MaximumReducedMeshes : MaximumFullMeshes;
        private static bool InBounds(Point p) => p.X >= 0 && p.X < Zone.Width && p.Y >= 0 && p.Y < Zone.Height;
        private static Vector3 Centre(Point p) => Village3DProjection.CellCentre(p.X, p.Y);
        private bool Visible(int x, int y) { var cell = _zone?.GetCell(x, y); return cell != null && cell.Explored && cell.IsVisible; }
        private void EnsureRoot()
        {
            // Material loss can occur without hierarchy loss. Repair readiness
            // before accepting another cast, and release an interrupted wait.
            // Surfaced by NativeSpellReadabilityLifecycleTests.
            if (_root != null && !MaterialsReady && _casts.Count > 0) Cancel("material-destroyed");
            if (_material == null) _material = CloneMaterial(_library.Material, "Native spell opaque (owned)");
            if (_luminousMaterial == null && _library.LuminousMaterial != null)
                _luminousMaterial = CloneMaterial(_library.LuminousMaterial, "Native spell luminous (owned)");
            if (_glowMaterial == null && _library.GlowMaterial != null)
                _glowMaterial = CloneMaterial(_library.GlowMaterial, "Native spell glow (owned)");
            if (_root != null) return;
            var root = new GameObject("Native spell effects") { layer = NativeZone3DRenderSurface.WorldLayer, hideFlags = HideFlags.DontSave };
            root.transform.SetParent(_surface.ContentRoot, false); _root = root.transform;
        }
        private Material CloneMaterial(Material source, string name)
        {
            var material = new Material(source) { name = name, hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetTexture("_FogLight", _surface.FogTexture);
            material.SetFloat("_Transient", 1f);
            return material;
        }
        private void CreateView()
        {
            var go = new GameObject("Authored spell fragment", typeof(MeshFilter), typeof(MeshRenderer))
                { layer = NativeZone3DRenderSurface.WorldLayer, hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_root, false);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            _views.Add(new View { Transform = go.transform, Filter = go.GetComponent<MeshFilter>(), Renderer = renderer, Material = _material });
        }
        private float Refuse(SpellFxSequence sequence, string reason)
        { LastEntry = null; Failure = reason; Trace("NativeSpellRejected", sequence, 0, reason, 0); return 0; }
        private static void Trace(string kind, SpellFxSequence sequence, int pieces, string reason, float duration)
        {
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", kind, actor: sequence?.Caster, payload: new
                { spellId = sequence?.SpellId, zoneId = sequence?.Zone?.ZoneID, pieces, reason, duration, backend = "native" });
        }
        public void ClearAll() => Cancel("cancelled");
        private void Cancel(string reason)
        {
            if (_casts.Count > 0) CancellationVersion++;
            for (int i = 0; i < _casts.Count; i++)
                Trace("NativeSpellCancelled", _casts[i].Sequence, _casts[i].Fragments.Count, reason, _casts[i].Duration);
            _casts.Clear(); _scheduled = 0;
            for (int i = 0; i < _views.Count; i++) if (_views[i].Renderer != null) _views[i].Renderer.enabled = false;
            _drawn = 0; LastEntry = null;
        }
        private void DestroyOwned()
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = null; _views.Clear();
            if (_material != null) Destroy(_material);
            if (_luminousMaterial != null) Destroy(_luminousMaterial);
            if (_glowMaterial != null) Destroy(_glowMaterial);
            _material = null; _luminousMaterial = null; _glowMaterial = null;
        }
        private static void Destroy(Object value)
        { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
        public void Dispose()
        {
            if (_disposed) return;
            Cancel("disposed"); DestroyOwned(); _surface = null; _zone = null; _disposed = true;
        }
    }
}
