using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pass 6 §6A — motion-ghost trails. After every ZoneRenderer
    /// redraw, this scans entities in the active zone, detects moves
    /// (current cell differs from last-known cell), and paints a
    /// faded copy of each mover's glyph at its previous cell. Ghosts
    /// fade alpha over a short lifetime then disappear.
    ///
    /// <para>Sits alongside ZoneRenderer and AnimatedEnvironmentRenderer
    /// — pure post-render scan, NO core engine modifications. Last-known
    /// positions are tracked in a private dictionary; comparison happens
    /// each frame.</para>
    ///
    /// <para>Ghosts render to a new overlay tilemap at sortingOrder
    /// halfway between AnimatedEnvironment (2) and FX (3). The
    /// rendering is a static sprite at faded alpha — no shader required.
    /// We use Tilemap.SetColor with reduced alpha; the existing
    /// Sprite-Lit-Default material applies, the lighting is whatever
    /// the LightMap computed for that cell at the moment the ghost
    /// was spawned.</para>
    ///
    /// <para><b>Performance:</b> O(N) entity scan per redraw, where
    /// N is typically &lt; 30 (player + visible NPCs). Bounded.</para>
    ///
    /// <para>Plan + design: <c>Docs/GRAPHICS-PASS6.md</c> §6A.</para>
    /// </summary>
    public class GlyphGhostRenderer : MonoBehaviour
    {
        // Sortingorder between AnimatedEnvironment (2) and FX (3).
        private const int OverlaySortingOrder = 3;

        // Default ghost lifetime in frames. 6 frames @ 60fps = 100ms.
        // Long enough to see, short enough not to clutter the screen
        // when a fast NPC is wandering.
        private const int DefaultLifetimeFrames = 6;

        private Tilemap _ghostTilemap;
        private Zone _zone;

        // Per-entity last-known cell position PLUS the tile + color the
        // main tilemap showed there — captured BEFORE the move, so the
        // ghost is the MOVER's glyph. (Round 3 audit 🟡: sampling the
        // cell AFTER the repaint duplicated the terrain glyph instead —
        // floating CP437 dots over the flowing sprite ground.)
        private struct LastSeen
        {
            public int X, Y;
            public TileBase Tile;
            public Color Color;
        }
        private readonly Dictionary<Entity, LastSeen> _lastKnown =
            new Dictionary<Entity, LastSeen>();

        // Round 3 — prune scratch: entities that vanished (died, were
        // picked up, left the zone) exit _lastKnown instead of pinning
        // their object graphs until zone change.
        private readonly HashSet<Entity> _seenThisScan = new HashSet<Entity>();
        private readonly List<Entity> _pruneScratch = new List<Entity>(32);

        // Per-cell ghost state. Each cell has at most one active ghost
        // at a time (overwriting). Tracks the tile + color + lifetime
        // remaining.
        private struct GhostCell
        {
            public TileBase Tile;
            public Color Color;
            public int FramesRemaining;
        }
        private readonly Dictionary<Vector3Int, GhostCell> _ghosts =
            new Dictionary<Vector3Int, GhostCell>();

        // Reusable scratch list for keys-to-remove this frame (avoids
        // modifying dictionary during enumeration).
        private readonly List<Vector3Int> _keysToRemove = new List<Vector3Int>(16);

        // Round 2 fix — decay snapshot. Writing `_ghosts[pos] = ghost`
        // INSIDE the foreach invalidates the enumerator in this
        // runtime (InvalidOperationException every frame once any
        // ghost decays; latent while the polish gate shipped OFF).
        // Scratch list per PERF-FOUNDATION §Pattern 1 — no per-frame
        // allocation.
        private readonly List<KeyValuePair<Vector3Int, GhostCell>> _decayScratch =
            new List<KeyValuePair<Vector3Int, GhostCell>>(32);

        public bool IsInitialized { get; private set; }

        public void Init(Transform gridParent)
        {
            var go = new GameObject("GlyphGhostTilemap");
            go.transform.SetParent(gridParent, false);
            _ghostTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;
            IsInitialized = true;
        }

        public void SetZone(Zone zone)
        {
            _zone = zone;
            // New zone = no movers from previous zone are valid.
            _lastKnown.Clear();
            ClearAllGhosts();
        }

        /// <summary>
        /// Hook called by ZoneRenderer at the end of each redraw.
        /// Detects entity moves, spawns ghosts at previous cells,
        /// decays existing ghosts, and clears expired ones.
        /// </summary>
        public void PostRender(Tilemap mainTilemap, System.Func<Entity, bool> isSpriteRendered = null)
        {
            if (!IsInitialized || _zone == null || mainTilemap == null) return;

            // 1. Decay existing ghosts. Walk the dict; lifetime--; if
            //    zero, mark for removal. Apply alpha falloff to current
            //    color so the ghost visibly fades.
            _keysToRemove.Clear();
            // Snapshot BEFORE mutating: `_ghosts[pos] = ghost` inside
            // the foreach invalidated the enumerator — every-frame
            // InvalidOperationException once any ghost was decaying
            // (surfaced by the round-2 live sweep; latent while the
            // polish gate shipped OFF).
            _decayScratch.Clear();
            foreach (var kvp in _ghosts) _decayScratch.Add(kvp);
            for (int i = 0; i < _decayScratch.Count; i++)
            {
                var pos = _decayScratch[i].Key;
                var ghost = _decayScratch[i].Value;
                ghost.FramesRemaining--;
                if (ghost.FramesRemaining <= 0)
                {
                    _keysToRemove.Add(pos);
                    _ghostTilemap.SetTile(pos, null);
                    continue;
                }
                // Linear alpha falloff over remaining lifetime.
                float t = (float)ghost.FramesRemaining / DefaultLifetimeFrames;
                var c = ghost.Color;
                c.a = ghost.Color.a * t;
                _ghostTilemap.SetColor(pos, c);
                _ghosts[pos] = ghost;
            }
            for (int i = 0; i < _keysToRemove.Count; i++)
                _ghosts.Remove(_keysToRemove[i]);

            // 2. Scan entities in zone. For each entity, compare its
            //    current cell to last-known. If moved, spawn a ghost at
            //    the previous cell using the tile + color CAPTURED
            //    BEFORE the move (Round 3 — the post-repaint cell shows
            //    the terrain, not the mover). Then re-capture.
            _seenThisScan.Clear();
            foreach (var entity in _zone.GetReadOnlyEntities())
            {
                var cell = _zone.GetEntityCell(entity);
                if (cell == null) continue;
                _seenThisScan.Add(entity);

                if (_lastKnown.TryGetValue(entity, out var prev)
                    && (prev.X != cell.X || prev.Y != cell.Y)
                    && !(isSpriteRendered != null && isSpriteRendered(entity)))
                {
                    // Sprite-rendered actors leave NO ghost.
                    //
                    // The guard below ("Tile stays null and SpawnGhost
                    // skips") was written believing the sprite pass had
                    // already nulled the main tilemap by now. It has not:
                    // RenderZone runs this pass at :907 and the sprite pass
                    // at :915, so the capture above reads the actor's CP437
                    // glyph and a pixel-art player smeared an '@' behind
                    // itself. The renderer cannot infer this from the
                    // tilemap; it has to ask.
                    SpawnGhost(prev.X, prev.Y, prev.Tile, prev.Color);
                }

                // Capture what the main tilemap shows at the CURRENT
                // cell this frame — next frame's ghost source. When the
                // sprite pass claimed the cell (actor sprite → main is
                // null) there is no glyph to ghost: Tile stays null and
                // SpawnGhost skips.
                var curPos = new Vector3Int(cell.X, Zone.Height - 1 - cell.Y, 0);
                _lastKnown[entity] = new LastSeen
                {
                    X = cell.X,
                    Y = cell.Y,
                    Tile = mainTilemap.GetTile(curPos),
                    Color = mainTilemap.GetColor(curPos),
                };
            }

            // Round 3 — prune entries for entities no longer in the
            // zone (dead, consumed, transferred): unpinned memory + a
            // scan set that stops growing monotonically.
            _pruneScratch.Clear();
            foreach (var kvp in _lastKnown)
                if (!_seenThisScan.Contains(kvp.Key)) _pruneScratch.Add(kvp.Key);
            for (int i = 0; i < _pruneScratch.Count; i++)
                _lastKnown.Remove(_pruneScratch[i]);
        }

        /// <summary>
        /// Spawn a ghost at zone cell (x, y) with the pre-captured tile
        /// + color the player last SAW there (the mover's own glyph —
        /// including any status-effect color it wore).
        /// </summary>
        private void SpawnGhost(int x, int y, TileBase tile, Color color)
        {
            if (tile == null) return;
            // Round 2 fix — the R2 mirror-bug class again: (x, y) are
            // ZONE coordinates, but ZoneRenderer paints zone row y at
            // tile row Height-1-y. Unflipped, every ghost spawned on
            // the vertically MIRRORED row and sampled the wrong cell's
            // glyph. Latent for the feature's whole life behind the
            // OFF gate; surfaced writing the round-2 decay pins.
            var pos = new Vector3Int(x, Zone.Height - 1 - y, 0);

            _ghostTilemap.SetTile(pos, tile);
            // Round 3 audit 🟡 fix — fresh cells default to LockColor:
            // without None flags every SetColor here (spawn tint AND
            // the decay fade) was a silent no-op — ghosts rendered
            // full-bright and popped out instead of fading. The same
            // root cause round 2 found in MakeTile.
            _ghostTilemap.SetTileFlags(pos, TileFlags.None);
            _ghostTilemap.SetColor(pos, color);
            _ghosts[pos] = new GhostCell
            {
                Tile = tile,
                Color = color,
                FramesRemaining = DefaultLifetimeFrames,
            };
        }

        /// <summary>Round 3 — public pause hook: ghost glyphs sort above
        /// the fullscreen UIs' tilemap; ZoneRenderer clears them on the
        /// Paused transition. Movement tracking (_lastKnown) survives so
        /// trails resume cleanly on unpause.</summary>
        public void ClearGhosts() => ClearAllGhosts();

        private void ClearAllGhosts()
        {
            if (_ghostTilemap == null) return;
            foreach (var kvp in _ghosts)
                _ghostTilemap.SetTile(kvp.Key, null);
            _ghosts.Clear();
            _keysToRemove.Clear();
        }

        // ── Test seams ───────────────────────────────────────────────────

        public int TestOnly_ActiveGhostCount => _ghosts.Count;

        public bool TestOnly_HasLastKnown(Entity e) => _lastKnown.ContainsKey(e);

        public void TestOnly_ResetTracking() { _lastKnown.Clear(); ClearAllGhosts(); }
    }
}
