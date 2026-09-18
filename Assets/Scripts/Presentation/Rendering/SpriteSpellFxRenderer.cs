using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pooled, cell-clipped flipbook playback of an already resolved spell. This
    /// renderer never drains a bus, looks up a live target, or runs combat/targeting.
    /// Scheduled atoms compose sequential phases and simultaneous multi-cell impacts.
    /// </summary>
    public sealed class SpriteSpellFxRenderer : IDisposable
    {
        public const int FullSpriteBudget = 192;
        public const int ReducedSpriteBudget = 48;
        public const int MaximumScheduledAtoms = 1536;
        public const int EffectSortingOrder = 12;
        public const int GroundSortingOrder = 4;
        private static readonly float[] ImpactFragmentOffsets = { .25f, 0f, -.25f };
        private readonly Transform _root;
        private readonly List<Atom> _active = new List<Atom>(128);
        private readonly Stack<Atom> _atomPool = new Stack<Atom>(128);
        private readonly Stack<SpriteRenderer> _spritePool = new Stack<SpriteRenderer>(64);
        private readonly HashSet<long> _impactCells = new HashSet<long>();
        private readonly HashSet<long> _reactionCells = new HashSet<long>();
        private Zone _zone;
        private bool _visible = true;
        private bool _disposed;
        private int _allocatedSprites;
        private int _liveSprites;

        private sealed class Atom
        {
            public SpellFxAsset Asset;
            public SpellFxPrimitive Primitive;
            public SpriteRenderer View;
            public Point Cell, Anchor;
            public Vector2 Offset;
            public float Age, Delay, Duration, Angle;
            public int Fragment, Variant;
            public bool Impact, Resisted, Reverse, Ground, Blocking, HasAnchor;
        }

        public SpriteSpellFxRenderer(Transform parent)
        {
            var root = new GameObject("Sprite Spell FX");
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(parent, false);
            GameplayRenderLayers.SetLayerRecursive(root, GameplayRenderLayers.WorldLayer);
            _root = root.transform;
        }
        public bool HasBlockingFx { get; private set; }
        /// <summary>Scheduled and currently visible atoms; queued phases count immediately.</summary>
        public int ActiveCount => _active.Count;
        public int ActiveSpriteCount => _liveSprites;
        public int PoolCount => _spritePool.Count;
        public int AllocatedSpriteCount => _allocatedSprites;
        public int PeakActiveSpriteCount { get; private set; }
        /// <summary>Sprite-frame attempts suppressed by the live sprite budget.</summary>
        public int DroppedSpriteCount { get; private set; }
        public void SetZone(Zone zone) { ClearAll(); _zone = zone; }
        public void SetPresentationVisible(bool visible)
        {
            _visible = visible;
            if (!visible) ClearAll();
        }

        /// <summary>Contact time computed by the last accepted sprite schedule.</summary>
        public float LastContactSeconds { get; private set; }

        /// <summary>Returns bounded duration, or zero when the coordinator should fall back/release waiting.</summary>
        public float Play(SpellFxSequence sequence, SpellFxDefinition definition)
        {
            if (_disposed || !_visible || _zone == null || sequence == null || sequence.Zone != _zone || definition == null
                || SpellFxSettings.Mode == SpellFxMode.Off || !AnyVisible(sequence)
                || !SpellFxCatalog.TryGetAsset(definition, out SpellFxAsset asset)) return 0f;
            bool reduced = SpellFxSettings.Mode == SpellFxMode.Reduced;
            float cast = SafeDuration(definition.CastDuration, .10f, 0f);
            float charge = SafeDuration(definition.ChargeDuration, .10f, 0f);
            float step = SafeDuration(definition.StepDuration, .025f, .005f);
            float impactDuration = SafeDuration(definition.ImpactDuration, .20f, .03f);
            float aftermath = SafeDuration(definition.AftermathDuration, .15f, 0f);
            float release = cast + charge;
            float travel = Mathf.Min(.65f, sequence.Path.Count * step);
            SpellFxFamily family = definition.AnimationFamily;
            if (family == SpellFxFamily.Lob) travel = Mathf.Min(.75f, travel * 1.6f);
            if (family == SpellFxFamily.Beam) travel = .07f;
            if (family == SpellFxFamily.Inscription || family == SpellFxFamily.Ward || family == SpellFxFamily.Field) travel = 0f;
            float impactAt = release + travel;
            LastContactSeconds = impactAt;
            float duration = impactAt + impactDuration + aftermath;
            int before = _active.Count;
            bool blocking = sequence.BlocksTurnAdvance;
            int variant = ((definition.Variant % SpellFxAsset.FrameCount) + (sequence.CosmeticSeed & 0x7fffffff) % SpellFxAsset.FrameCount) % SpellFxAsset.FrameCount;

            if (cast > 0f) Add(asset, SpellFxPrimitive.Cast, sequence.Source, 0, cast, blocking, variant);
            if (charge > 0f && !reduced) Add(asset, SpellFxPrimitive.Charge, sequence.Source, cast, charge, blocking, variant);

            switch (family)
            {
                case SpellFxFamily.Projectile:
                case SpellFxFamily.Lob:
                    for (int i = 0; i < sequence.Path.Count; i++)
                    {
                        int index = definition.ReverseTravel ? sequence.Path.Count - 1 - i : i;
                        Point cell = sequence.Path[index];
                        Point previous = index > 0 ? sequence.Path[index - 1] : sequence.Source;
                        float at = release + (sequence.Path.Count > 0 ? travel * i / sequence.Path.Count : 0f);
                        Add(asset, SpellFxPrimitive.Head, cell, at, Mathf.Max(.025f, travel / Mathf.Max(1, sequence.Path.Count)), blocking, variant, Angle(previous, cell));
                        if (!reduced) Add(asset, SpellFxPrimitive.Trail, cell, at + .02f, .09f, blocking, variant, Angle(previous, cell));
                    }
                    break;
                case SpellFxFamily.Beam:
                case SpellFxFamily.Chain:
                    for (int i = 0; i < sequence.Path.Count; i++)
                    {
                        Point cell = sequence.Path[i];
                        Point previous = i > 0 ? sequence.Path[i - 1] : sequence.Source;
                        float at = release + (family == SpellFxFamily.Chain ? travel * i / Mathf.Max(1, sequence.Path.Count) : 0f);
                        Add(asset, SpellFxPrimitive.Beam, cell, at, .12f, blocking, variant + i, Angle(previous, cell));
                    }
                    break;
                case SpellFxFamily.GroundLine:
                    for (int i = 0; i < sequence.Path.Count; i++)
                    {
                        int index = definition.ReverseTravel ? sequence.Path.Count - 1 - i : i;
                        Point cell = sequence.Path[index];
                        float at = release + travel * i / Mathf.Max(1, sequence.Path.Count);
                        Add(asset, SpellFxPrimitive.Wave, cell, at, .14f, blocking, variant, 0f, true, definition.ReverseTravel);
                    }
                    break;
                case SpellFxFamily.Cone:
                case SpellFxFamily.Ring:
                case SpellFxFamily.Field:
                    for (int i = 0; i < sequence.AffectedCells.Count; i++)
                    {
                        Point cell = sequence.AffectedCells[i];
                        // Distance controls presentation timing only; the affected set comes from resolution.
                        float distance = Mathf.Max(Mathf.Abs(cell.X - sequence.Source.X), Mathf.Abs(cell.Y - sequence.Source.Y));
                        float at = release + Mathf.Min(.22f, distance * step);
                        SpellFxPrimitive primitive = family == SpellFxFamily.Cone ? SpellFxPrimitive.Beam
                            : family == SpellFxFamily.Field ? SpellFxPrimitive.Sigil : SpellFxPrimitive.Wave;
                        Add(asset, primitive, cell, at, family == SpellFxFamily.Field ? .24f : .15f, blocking, variant,
                            family == SpellFxFamily.Cone ? Angle(sequence.Source, cell) : 0f, family != SpellFxFamily.Cone, definition.School == "ice");
                        duration = Mathf.Max(duration, at + .24f + aftermath);
                    }
                    break;
                case SpellFxFamily.Inscription:
                case SpellFxFamily.Ward:
                    if (sequence.AffectedCells.Count == 0)
                        Add(asset, SpellFxPrimitive.Sigil, sequence.Source, cast, charge + impactDuration, blocking, variant, 0f, true);
                    else
                        for (int i = 0; i < sequence.AffectedCells.Count; i++)
                            Add(asset, SpellFxPrimitive.Sigil, sequence.AffectedCells[i], cast, charge + impactDuration, blocking, variant, 0f, true);
                    break;
            }

            _impactCells.Clear();
            for (int i = 0; i < sequence.Targets.Count; i++)
            {
                SpellFxTargetResult target = sequence.Targets[i];
                if (target == null) continue;
                _impactCells.Add(Key(target.Cell));
                bool expanded = !reduced && !definition.Domestic;
                bool rejectedOnly = target.Damage == 0 && target.AppliedEffects.Count == 0 && target.RejectedEffects.Count > 0;
                AddImpact(asset, target.Cell, impactAt, impactDuration, blocking, target.Resisted || rejectedOnly, expanded);
                if (target.Moved)
                {
                    Add(asset, SpellFxPrimitive.Trail, target.Cell, impactAt, .11f, blocking, variant, Angle(target.Cell, target.FinalCell));
                    Add(asset, SpellFxPrimitive.Wave, target.FinalCell, impactAt + .10f, .14f, blocking, variant, 0f, true);
                    duration = Mathf.Max(duration, impactAt + .24f);
                }
                if (target.Died && !reduced)
                    Add(asset, SpellFxPrimitive.Overlay, target.Cell, impactAt + impactDuration, aftermath, blocking, variant, 0f, true);
                if (definition.IsRite && !reduced && target.Resonance > 1.5f)
                {
                    int echoes = target.Resonance > 2.5f ? 2 : 1;
                    for (int echo = 0; echo < echoes; echo++)
                        Add(asset, SpellFxPrimitive.Wave, target.Cell, impactAt + .05f * (echo + 1), .18f, blocking, variant, 0f, true);
                }
                if (definition.IsRite && target.MarksConsumed > 0)
                {
                    // One crossed-out ink mark per consumed status. A cold rite never gets expenditure ticks.
                    int count = Mathf.Min(target.MarksConsumed, 8);
                    for (int mark = 0; mark < count; mark++)
                    {
                        if (mark < target.ConsumedMarks.Count)
                        {
                            SpellFxAsset material = MaterialAsset(target.ConsumedMarks[mark]);
                            if (material != null) Add(material, SpellFxPrimitive.Sigil, target.Cell, cast + mark * .04f, .04f, blocking, 0, 0f, true);
                        }
                        Add(asset, SpellFxPrimitive.Mark, target.Cell, cast + mark * .04f + .03f, .07f, blocking, 0, 0f, true);
                    }
                    duration = Mathf.Max(duration, cast + count * .04f + .07f);
                }
            }
            for (int i = 0; i < sequence.AffectedCells.Count; i++)
            {
                Point cell = sequence.AffectedCells[i];
                if (_impactCells.Add(Key(cell)))
                    Add(asset, family == SpellFxFamily.Field ? SpellFxPrimitive.Sigil : SpellFxPrimitive.Wave,
                        cell, impactAt, impactDuration, blocking, variant, 0f, family == SpellFxFamily.Field);
            }
            // Aftermath follows actual material writes and reactions, including steam,
            // frozen water, and conducted charge; school alone never asserts a residue.
            if (!reduced && aftermath > 0f)
            {
                _reactionCells.Clear();
                for (int i = sequence.Reactions.Count - 1; i >= 0; i--)
                {
                    SpellFxCellResult reaction = sequence.Reactions[i];
                    if (reaction == null || reaction.Amount <= 0) continue;
                    int outcome = reaction.Kind == "healing" ? 1 : reaction.Kind == "cleansing" ? 2
                        : reaction.Kind == "consumed-status" ? 3 : 0;
                    SpellFxAsset reactionAsset;
                    if (outcome == 1 || outcome == 2)
                        SpellFxCatalog.TryGetAsset(SpellFxCatalog.Find("Spellcraft_WardGleam"), out reactionAsset);
                    else
                        reactionAsset = outcome == 3 && reaction.Value != "Burning" ? null : MaterialAsset(reaction.Value);
                    if (reactionAsset == null || !_reactionCells.Add((Key(reaction.Cell) << 3) | (uint)outcome)) continue;
                    float at = impactAt + impactDuration;
                    if (outcome == 3)
                    {
                        // Actual fuel expenditure contracts first, then leaves a brief soot trace.
                        Add(reactionAsset, SpellFxPrimitive.Wave, reaction.Cell, at, aftermath * .5f, blocking, variant, 0f, true, true);
                        Add(reactionAsset, SpellFxPrimitive.Overlay, reaction.Cell, at + aftermath * .5f, aftermath * .5f, blocking, variant, 0f, true);
                    }
                    else
                    {
                        SpellFxPrimitive primitive = outcome == 1 ? SpellFxPrimitive.Wave : outcome == 2 ? SpellFxPrimitive.Charge
                            : reaction.Kind == "reaction" ? SpellFxPrimitive.Wave : SpellFxPrimitive.Overlay;
                        Add(reactionAsset, primitive, reaction.Cell, at, aftermath, blocking, variant, 0f, outcome == 0);
                    }
                }
            }
            if (_active.Count == before) return 0f;
            HasBlockingFx |= blocking;
            // Include the tails of all scheduled atoms in the returned playback handle.
            for (int i = before; i < _active.Count; i++) duration = Mathf.Max(duration, _active[i].Delay + _active[i].Duration);
            return Mathf.Min(duration, 3f);
        }

        public void Update(float deltaTime)
        {
            if (_disposed) return;
            if (!_visible || SpellFxSettings.Mode == SpellFxMode.Off) { ClearAll(); return; }
            deltaTime = float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ? 0f : Mathf.Max(0f, deltaTime);
            HasBlockingFx = false;
            int budget = SpellFxSettings.Mode == SpellFxMode.Reduced ? ReducedSpriteBudget : FullSpriteBudget;
            for (int i = 0; i < _active.Count;)
            {
                Atom atom = _active[i];
                atom.Age += deltaTime;
                if (atom.Age >= atom.Delay + atom.Duration)
                {
                    Release(atom);
                    int last = _active.Count - 1;
                    _active[i] = _active[last];
                    _active.RemoveAt(last);
                    continue;
                }
                bool visible = IsVisible(atom.Cell) && (!atom.HasAnchor || IsVisible(atom.Anchor));
                HasBlockingFx |= atom.Blocking && visible;
                if (atom.Age < atom.Delay || !visible)
                {
                    ReturnView(atom);
                    i++;
                    continue;
                }
                if (atom.View == null)
                {
                    if (_liveSprites >= budget) { DroppedSpriteCount++; i++; continue; }
                    atom.View = RentSprite();
                }
                float progress = Mathf.Clamp01((atom.Age - atom.Delay) / atom.Duration);
                int frame = Mathf.Min(5, Mathf.FloorToInt(progress * 6f));
                if (atom.Reverse) frame = 5 - frame;
                atom.View.sprite = atom.Impact ? atom.Asset.Impact(frame, atom.Fragment, atom.Resisted)
                    : atom.Asset.Detail(atom.Primitive, (atom.Primitive == SpellFxPrimitive.Head || atom.Primitive == SpellFxPrimitive.Beam || atom.Primitive == SpellFxPrimitive.Trail)
                        ? (frame + atom.Variant) % 6 : frame);
                // The atlas pixels are binary alpha. Flash changes luminance only and is independently optional.
                float brightness = 1f + (atom.Impact && frame == 0 ? Mathf.Clamp01(SpellFxSettings.FlashIntensity) * .12f : 0f);
                atom.View.color = new Color(brightness, brightness, brightness, 1f);
                atom.View.sortingOrder = atom.Ground ? GroundSortingOrder : EffectSortingOrder;
                atom.View.transform.localPosition = CellCenter(atom.Cell) + new Vector3(atom.Offset.x, atom.Offset.y, 0f);
                // A rotated square can overhang. Near fog only quarter-turns are allowed.
                float angle = AllNeighborsVisible(atom.Cell) ? atom.Angle : Mathf.Round(atom.Angle / 90f) * 90f;
                atom.View.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                atom.View.transform.localScale = Vector3.one;
                atom.View.enabled = true;
                i++;
            }
            PeakActiveSpriteCount = Mathf.Max(PeakActiveSpriteCount, _liveSprites);
        }

        public void ClearAll()
        {
            for (int i = 0; i < _active.Count; i++) Release(_active[i]);
            _active.Clear(); _impactCells.Clear(); _reactionCells.Clear(); HasBlockingFx = false;
        }
        public void Dispose()
        {
            if (_disposed) return;
            ClearAll(); _disposed = true;
            _spritePool.Clear(); _atomPool.Clear();
            _allocatedSprites = 0;
            if (_root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_root.gameObject);
                else UnityEngine.Object.DestroyImmediate(_root.gameObject);
            }
        }
        private void Add(SpellFxAsset asset, SpellFxPrimitive primitive, Point cell, float delay, float duration,
            bool blocking, int variant, float angle = 0f, bool ground = false, bool reverse = false)
        {
            if (!IsVisible(cell) || duration <= 0f || _active.Count >= MaximumScheduledAtoms) return;
            Atom atom = _atomPool.Count > 0 ? _atomPool.Pop() : new Atom();
            atom.Asset = asset; atom.Primitive = primitive; atom.Cell = cell; atom.Delay = delay; atom.Duration = duration;
            atom.Blocking = blocking; atom.Variant = variant % 6; atom.Angle = angle; atom.Ground = ground; atom.Reverse = reverse;
            _active.Add(atom);
        }
        private void AddImpact(SpellFxAsset asset, Point cell, float at, float duration, bool blocking, bool resisted, bool expanded)
        {
            if (!IsVisible(cell)) return;
            if (!expanded)
            {
                if (resisted)
                {
                    int before = _active.Count;
                    Add(asset, SpellFxPrimitive.Wave, cell, at, duration, blocking, 0);
                    if (_active.Count > before)
                    {
                        Atom atom = _active[_active.Count - 1];
                        atom.Impact = true; atom.Fragment = 4; atom.Resisted = true;
                    }
                }
                else Add(asset, SpellFxPrimitive.Wave, cell, at, duration, blocking, 0);
                return;
            }
            // Fragment coordinates are bottom-to-top in Unity, while zone Y points down.
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    Point fragmentCell = new Point(cell.X + x - 1, cell.Y - (y - 1));
                    int before = _active.Count;
                    Add(asset, SpellFxPrimitive.Wave, fragmentCell, at, duration, blocking, 0);
                    if (_active.Count == before) continue;
                    Atom atom = _active[_active.Count - 1];
                    atom.Impact = true; atom.Fragment = y * 3 + x; atom.Resisted = resisted;
                    atom.Anchor = cell; atom.HasAnchor = true;
                    atom.Offset = new Vector2(ImpactFragmentOffsets[x], ImpactFragmentOffsets[y]);
                }
        }
        private SpriteRenderer RentSprite()
        {
            SpriteRenderer view;
            if (_spritePool.Count > 0) view = _spritePool.Pop();
            else
            {
                var gameObject = new GameObject("Spell FX Pixel");
                gameObject.hideFlags = HideFlags.DontSave;
                gameObject.transform.SetParent(_root, false);
                GameplayRenderLayers.SetLayerRecursive(gameObject, GameplayRenderLayers.WorldLayer);
                view = gameObject.AddComponent<SpriteRenderer>();
                view.spriteSortPoint = SpriteSortPoint.Pivot;
                _allocatedSprites++;
            }
            _liveSprites++;
            return view;
        }
        private void ReturnView(Atom atom)
        {
            if (atom.View == null) return;
            atom.View.enabled = false;
            atom.View.sprite = null;
            atom.View.color = Color.white;
            atom.View.transform.localPosition = Vector3.zero;
            atom.View.transform.localRotation = Quaternion.identity;
            atom.View.transform.localScale = Vector3.one;
            _spritePool.Push(atom.View);
            atom.View = null;
            _liveSprites--;
        }
        private void Release(Atom atom)
        {
            ReturnView(atom);
            atom.Asset = null;
            atom.Age = atom.Delay = atom.Duration = atom.Angle = 0f;
            atom.Offset = Vector2.zero;
            atom.Fragment = atom.Variant = 0;
            atom.Impact = atom.Resisted = atom.Reverse = atom.Ground = atom.Blocking = atom.HasAnchor = false;
            atom.Cell = atom.Anchor = default;
            _atomPool.Push(atom);
        }
        private static SpellFxAsset MaterialAsset(string value)
        {
            string spellId;
            switch (value)
            {
                case "heat": case "embers": case "Burning": case "ignite_oil_heat": case "ignite_oil_embers":
                    spellId = "Pyromancy_Kindle"; break;
                case "cold": case "ice": case "Frozen": case "Chilled": case "freeze_water":
                    spellId = "Cryomancy_IceLance"; break;
                case "charge": case "Electrified": case "electrify_water": case "electrify_conductor":
                    spellId = "Galvanism_ArcBolt"; break;
                case "water": case "Wet": case "steam": case "melt_ice":
                    spellId = "Hydromancy_Quench"; break;
                case "acid": case "Acidic": case "Poisoned":
                    spellId = "Corrosion_AcidSpray"; break;
                default: return null;
            }
            return SpellFxCatalog.TryGetAsset(SpellFxCatalog.Find(spellId), out SpellFxAsset result) ? result : null;
        }
        private bool AnyVisible(SpellFxSequence sequence)
        {
            if (IsVisible(sequence.Source)) return true;
            for (int i = 0; i < sequence.Path.Count; i++) if (IsVisible(sequence.Path[i])) return true;
            for (int i = 0; i < sequence.AffectedCells.Count; i++) if (IsVisible(sequence.AffectedCells[i])) return true;
            for (int i = 0; i < sequence.Targets.Count; i++) if (sequence.Targets[i] != null && IsVisible(sequence.Targets[i].Cell)) return true;
            return false;
        }
        private bool IsVisible(Point point)
        {
            Cell cell = _zone?.GetCell(point.X, point.Y);
            return cell != null && cell.IsVisible && cell.Explored;
        }
        private bool AllNeighborsVisible(Point cell)
        {
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (!IsVisible(new Point(cell.X + x, cell.Y + y))) return false;
            return true;
        }
        private static long Key(Point point) => ((long)point.X << 32) ^ (uint)point.Y;
        private static Vector3 CellCenter(Point cell) => new Vector3(cell.X + .5f, Zone.Height - cell.Y - .5f, -cell.Y * .001f);
        private static float Angle(Point from, Point to) => Mathf.Atan2(from.Y - to.Y, to.X - from.X) * Mathf.Rad2Deg;
        private static float SafeDuration(float value, float fallback, float minimum)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, .8f);
    }
}
