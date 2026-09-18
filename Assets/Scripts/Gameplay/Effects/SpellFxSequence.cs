using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Copied results of one actual target resolution; no live target is needed for playback.</summary>
    public sealed class SpellFxTargetResult
    {
        public Point Cell { get; }
        /// <summary>Original owner anchor, copied before displacement or destruction; it can differ from body contact.</summary>
        public Point AnchorCell { get; }
        public Point FinalCell { get; }
        /// <summary>Explicit path/area selection provenance from EndPathAt, TargetOnPath, or TargetInAffectedCells.
        /// False includes automatic reaction observations and unspecified legacy TargetAt calls.</summary>
        public bool IsDirectTarget { get; }
        public string TargetId { get; }
        public int Damage { get; }
        public bool Resisted { get; }
        public bool Died { get; }
        public int MarksConsumed { get; }
        public IReadOnlyList<string> ConsumedMarks { get; }
        public float Resonance { get; }
        public bool Moved => !Died && FinalCell.X >= 0 && (Cell.X != FinalCell.X || Cell.Y != FinalCell.Y);
        public IReadOnlyList<string> AppliedEffects { get; }
        public IReadOnlyList<string> RejectedEffects { get; }

        public SpellFxTargetResult(string targetId, Point cell, Point finalCell, int damage = 0,
            bool resisted = false, bool died = false, int marksConsumed = 0,
            IEnumerable<string> appliedEffects = null, IEnumerable<string> rejectedEffects = null,
            IEnumerable<string> consumedMarks = null, float resonance = 1f,
            Point? anchorCell = null, bool isDirectTarget = false)
        {
            TargetId = targetId ?? ""; Cell = cell; FinalCell = finalCell;
            AnchorCell = anchorCell ?? cell; IsDirectTarget = isDirectTarget;
            Damage = Math.Max(0, damage); Resisted = resisted; Died = died;
            MarksConsumed = Math.Max(0, marksConsumed);
            ConsumedMarks = new List<string>(consumedMarks ?? Array.Empty<string>()).AsReadOnly();
            Resonance = resonance;
            AppliedEffects = new List<string>(appliedEffects ?? Array.Empty<string>()).AsReadOnly();
            RejectedEffects = new List<string>(rejectedEffects ?? Array.Empty<string>()).AsReadOnly();
        }
    }

    /// <summary>A copied material change or resolved spell outcome at an actual affected cell.</summary>
    public sealed class SpellFxCellResult
    {
        public Point Cell { get; }
        /// <summary>coating, residue, cloud, energy, reaction, healing, cleansing, or consumed-status.</summary>
        public string Kind { get; }
        /// <summary>Authored material/reaction ID or outcome value, for example water, freeze_water, Hitpoints, or Burning.</summary>
        public string Value { get; }
        /// <summary>Material strength/duration, actual healing, number of cleansed conditions, or consumed status duration, according to Kind.</summary>
        public int Amount { get; }
        public SpellFxCellResult(Point cell, string kind, string value, int amount)
        { Cell = cell; Kind = kind ?? ""; Value = value ?? ""; Amount = amount; }
    }

    /// <summary>Transient presentation data. Geometry and outcomes are copied from simulation, never recalculated by playback.</summary>
    public sealed class SpellFxSequence
    {
        public string SpellId { get; }
        public Zone Zone { get; }
        /// <summary>Optional actor pose hook only. Targeting and effects must use the copied coordinates.</summary>
        public Entity Caster { get; }
        public Point Source { get; }
        public IReadOnlyList<Point> Path { get; }
        public IReadOnlyList<Point> AffectedCells { get; }
        public IReadOnlyList<SpellFxTargetResult> Targets { get; }
        public IReadOnlyList<SpellFxCellResult> Reactions { get; }
        public float Intensity { get; }
        public int CosmeticSeed { get; }
        public int MarksConsumed { get; }
        public bool BlocksTurnAdvance { get; }

        public SpellFxSequence(string spellId, Zone zone, Entity caster, Point source,
            IEnumerable<Point> path = null, IEnumerable<Point> affectedCells = null,
            IEnumerable<SpellFxTargetResult> targets = null, float intensity = 1f,
            int cosmeticSeed = 0, int marksConsumed = 0, bool blocksTurnAdvance = true,
            IEnumerable<SpellFxCellResult> reactions = null)
        {
            SpellId = spellId ?? ""; Zone = zone; Caster = caster; Source = source;
            Path = new List<Point>(path ?? Array.Empty<Point>()).AsReadOnly();
            AffectedCells = new List<Point>(affectedCells ?? Array.Empty<Point>()).AsReadOnly();
            Targets = new List<SpellFxTargetResult>(targets ?? Array.Empty<SpellFxTargetResult>()).AsReadOnly();
            Reactions = new List<SpellFxCellResult>(reactions ?? Array.Empty<SpellFxCellResult>()).AsReadOnly();
            Intensity = float.IsNaN(intensity) || float.IsInfinity(intensity) ? 1f : Math.Max(0f, intensity);
            CosmeticSeed = cosmeticSeed; MarksConsumed = Math.Max(0, marksConsumed);
            BlocksTurnAdvance = blocksTurnAdvance;
        }
    }

    /// <summary>Single-consumer, bounded transient queue. It is intentionally outside save data.</summary>
    public static class SpellFxBus
    {
        public const int MaximumPending = 256;
        private static readonly List<SpellFxSequence> Pending = new List<SpellFxSequence>();
        public static int PendingCount => Pending.Count;
        public static bool HasPendingBlocking
        {
            get { for (int i = 0; i < Pending.Count; i++) if (Pending[i].BlocksTurnAdvance) return true; return false; }
        }
        public static void Emit(SpellFxSequence sequence)
        {
            if (sequence == null || sequence.Zone == null || Pending.Count >= MaximumPending) return;
            // Repeated passive events from the same actor in one playback batch share one accent.
            if (!sequence.BlocksTurnAdvance)
                for (int i = 0; i < Pending.Count; i++)
                    if (!Pending[i].BlocksTurnAdvance && Pending[i].SpellId == sequence.SpellId
                        && Pending[i].Caster == sequence.Caster && Pending[i].Zone == sequence.Zone) return;
            Pending.Add(sequence);
        }
        public static List<SpellFxSequence> Drain()
        {
            var result = new List<SpellFxSequence>(Pending);
            Pending.Clear();
            return result;
        }
        public static void Clear() => Pending.Clear();
    }

    /// <summary>
    /// Synchronous, nestable observation scope around a spell. Hooks only record work the simulation
    /// actually performs. Disposing an uncommitted scope discards a refusal or interrupted cast.
    /// </summary>
    public sealed class SpellFxCapture : IDisposable
    {
        [ThreadStatic] private static SpellFxCapture _current;
        private static int _cosmeticSerial;
        private readonly SpellFxCapture _parent;
        private readonly string _spellId;
        private readonly Zone _zone;
        private readonly Entity _caster;
        private readonly Point _source;
        private readonly bool _blocking;
        private readonly List<Point> _path = new List<Point>();
        private readonly List<Point> _cells = new List<Point>();
        private readonly HashSet<Point> _cellSet = new HashSet<Point>();
        private readonly List<TargetSnapshot> _targets = new List<TargetSnapshot>();
        private readonly Dictionary<Entity, TargetSnapshot> _targetMap = new Dictionary<Entity, TargetSnapshot>();
        private readonly List<SpellFxCellResult> _reactions = new List<SpellFxCellResult>();
        private int _marks;
        private float _intensity = 1f;
        private bool _committed;
        private sealed class TargetSnapshot
        {
            public Entity Target; public Point Cell; public Point Anchor; public int Damage; public bool Resisted; public int Marks;
            public bool Direct;
            public float Resonance = 1f;
            public readonly List<string> ConsumedMarks = new List<string>();
            public readonly List<string> Applied = new List<string>();
            public readonly List<string> Rejected = new List<string>();
        }

        public SpellFxCapture(string spellId, Zone zone, Entity caster, bool blocking = true)
        {
            _parent = _current; _current = this;
            _spellId = spellId; _zone = zone; _caster = caster; _blocking = blocking;
            var p = zone != null && caster != null ? zone.GetEntityPosition(caster) : (-1, -1);
            _source = new Point(p.Item1, p.Item2);
        }
        private static SpellFxCapture In(Zone zone) => _current != null && (zone == null || _current._zone == zone) ? _current : null;
        public static void PathCell(Zone zone, int x, int y)
        {
            var c = In(zone); if (c == null || x < 0 || y < 0) return;
            var p = new Point(x, y);
            if (!c._path.Contains(p)) c._path.Add(p);
        }
        public static void SetPath(Zone zone, IEnumerable<Point> path)
        {
            var c = In(zone); if (c == null) return;
            c._path.Clear(); if (path != null) c._path.AddRange(path);
        }
        /// <summary>Clips the observed line at the chosen owner's first occupied cell and captures that contact before damage.</summary>
        public static void EndPathAt(Zone zone, Entity target)
        {
            var c = In(zone); if (c == null || zone == null || target == null) return;
            for (int i = 0; i < c._path.Count; i++)
            {
                if (!TargetAt(zone, target, c._path[i])) continue;
                c._targetMap[target].Direct = true;
                if (i + 1 < c._path.Count) c._path.RemoveRange(i + 1, c._path.Count - i - 1);
                return;
            }
        }
        public static void AffectCell(Zone zone, int x, int y)
        {
            var c = In(zone); if (c == null || x < 0 || y < 0) return;
            var p = new Point(x, y); if (c._cellSet.Add(p)) c._cells.Add(p);
        }
        private TargetSnapshot Observe(Entity target, Point? contact = null)
        {
            if (target == null || _zone == null) return null;
            if (_targetMap.TryGetValue(target, out var existing)) return existing;
            var p = _zone.GetEntityPosition(target);
            if (p.x < 0) return null;
            var anchor = new Point(p.x, p.y);
            var snapshot = new TargetSnapshot { Target = target, Anchor = anchor, Cell = contact ?? anchor };
            _targetMap.Add(target, snapshot); _targets.Add(snapshot);
            AffectCell(_zone, snapshot.Cell.X, snapshot.Cell.Y);
            return snapshot;
        }
        public static void Target(Zone zone, Entity target) => In(zone)?.Observe(target);
        /// <summary>
        /// Records a selected physical contact only when this zone's committed occupancy contains the owner.
        /// Returns false without adding a result for invalid context or empty cells, including footprint holes.
        /// The first observation is retained across later body cells, damage hooks, and displacement.
        /// </summary>
        public static bool TargetAt(Zone zone, Entity target, Point contact)
        {
            var c = In(zone);
            if (c == null || zone == null || target == null || !zone.InBounds(contact.X, contact.Y)
                || !zone.GetOccupants(contact.X, contact.Y).Contains(target)) return false;
            return c.Observe(target, contact) != null;
        }
        /// <summary>Captures the first actual body contact in the already observed path, without changing targeting or the path.</summary>
        public static bool TargetOnPath(Zone zone, Entity target)
        {
            var c = In(zone); if (c == null || zone == null || target == null) return false;
            for (int i = 0; i < c._path.Count; i++)
                if (TargetAt(zone, target, c._path[i])) { c._targetMap[target].Direct = true; return true; }
            return false;
        }
        /// <summary>Captures the first actual body contact in already observed area cells; does not add an inferred anchor hit.</summary>
        public static bool TargetInAffectedCells(Zone zone, Entity target)
        {
            var c = In(zone); if (c == null || zone == null || target == null) return false;
            for (int i = 0; i < c._cells.Count; i++)
                if (TargetAt(zone, target, c._cells[i])) { c._targetMap[target].Direct = true; return true; }
            return false;
        }
        public static void RecordDamage(Zone zone, Entity target, int actualDamage, bool resisted)
        {
            var t = In(zone)?.Observe(target); if (t == null) return;
            t.Damage += Math.Max(0, actualDamage); t.Resisted |= resisted;
        }
        public static void RecordEffect(Zone zone, Entity target, string effect, bool applied)
        {
            var t = In(zone)?.Observe(target); if (t == null || string.IsNullOrEmpty(effect)) return;
            (applied ? t.Applied : t.Rejected).Add(effect);
        }
        /// <summary>
        /// Records a resolved non-damage outcome at a copied anchor cell. The caller supplies the actual
        /// applied amount; playback must not recompute healing caps, condition removals, or consumed duration.
        /// Equipment outcomes use their wearer's cell because carried equipment has no world position.
        /// </summary>
        public static void RecordOutcome(Zone zone, Entity anchor, string kind, string value, int amount)
        {
            var c = In(zone);
            if (c == null || c._zone == null || anchor == null || string.IsNullOrEmpty(kind)) return;
            Point at;
            if (c._targetMap.TryGetValue(anchor, out var target)) at = target.Cell;
            else if (anchor == c._caster) at = c._source;
            else
            {
                var position = c._zone.GetEntityPosition(anchor);
                at = new Point(position.x, position.y);
            }
            if (!c._zone.InBounds(at.X, at.Y)) return;
            AffectCell(c._zone, at.X, at.Y);
            c._reactions.Add(new SpellFxCellResult(at, kind, value, Math.Max(0, amount)));
        }
        public static void RecordGround(Zone zone, int x, int y, string kind, string value, int amount)
        {
            var c = In(zone); if (c == null || zone == null || !zone.InBounds(x, y)) return;
            var state = zone.TileState.Get(x, y);
            switch (kind)
            {
                case "energy":
                    amount = value == "heat" ? zone.TileState.Heat(x, y) :
                        value == "cold" ? zone.TileState.Cold(x, y) : zone.TileState.Charge(x, y);
                    break;
                case "coating": amount = zone.TileState.CoatingTurns(x, y, value); break;
                case "residue":
                    amount = 0;
                    if (state != null)
                        for (int i = 0; i < state.Residues.Count; i++)
                            if (state.Residues[i].Id == value) { amount = state.Residues[i].Turns; break; }
                    break;
                case "cloud": amount = state?.CloudTurns ?? 0; break;
            }
            if ((kind == "coating" || kind == "residue" || kind == "cloud") && amount <= 0) return;
            AffectCell(zone, x, y);
            c._reactions.Add(new SpellFxCellResult(new Point(x, y), kind, value, amount));
        }
        public static void ConsumeMarks(Zone zone, Entity target, int marks, float resonance, IEnumerable<string> consumedMarks = null)
        {
            var c = In(zone); if (c == null) return;
            var t = c.Observe(target);
            if (t != null)
            {
                t.Marks += marks; t.Resonance = resonance;
                if (consumedMarks != null) t.ConsumedMarks.AddRange(consumedMarks);
            }
            c._marks += marks; c._intensity = Math.Max(c._intensity, resonance);
        }
        public void Commit()
        {
            if (_committed || _zone == null || _source.X < 0) return;
            _committed = true;
            var results = new List<SpellFxTargetResult>(_targets.Count);
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i]; var final = _zone.GetEntityPosition(t.Target);
                // Contact and anchor differ for multi-cell bodies. Preserve the contact's offset
                // through real anchor translation; a stationary off-anchor hit is not a shove.
                var finalContact = final.x < 0 ? new Point(-1, -1)
                    : new Point(t.Cell.X + final.x - t.Anchor.X, t.Cell.Y + final.y - t.Anchor.Y);
                var hp = t.Target.GetStat("Hitpoints");
                bool dead = (hp != null && hp.Value <= 0) || (t.Target.GetPart<DestructiblePart>()?.Gone ?? false);
                results.Add(new SpellFxTargetResult(t.Target.ID, t.Cell, finalContact,
                    t.Damage, t.Resisted, dead, t.Marks, t.Applied, t.Rejected, t.ConsumedMarks, t.Resonance,
                    t.Anchor, t.Direct));
            }
            if (_cells.Count == 0)
            {
                var endpoint = _path.Count > 0 ? _path[_path.Count - 1] : _source;
                AffectCell(_zone, endpoint.X, endpoint.Y);
            }
            // Cosmetic serial is independent of every simulation Random instance.
            int seed = unchecked(++_cosmeticSerial * 486187739);
            SpellFxBus.Emit(new SpellFxSequence(_spellId, _zone, _caster, _source,
                _path, _cells, results, _intensity, seed, _marks, _blocking, _reactions));
        }
        public void Dispose() { if (_current == this) _current = _parent; }
    }
}
