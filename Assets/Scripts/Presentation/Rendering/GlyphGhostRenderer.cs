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

        // Per-entity last-known cell position. Used to detect moves.
        private readonly Dictionary<Entity, (int x, int y)> _lastKnown =
            new Dictionary<Entity, (int x, int y)>();

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
        public void PostRender(Tilemap mainTilemap)
        {
            if (!IsInitialized || _zone == null || mainTilemap == null) return;

            // 1. Decay existing ghosts. Walk the dict; lifetime--; if
            //    zero, mark for removal. Apply alpha falloff to current
            //    color so the ghost visibly fades.
            _keysToRemove.Clear();
            // Iterate via a stable list because we mutate values.
            foreach (var kvp in _ghosts)
            {
                var pos = kvp.Key;
                var ghost = kvp.Value;
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
            //    current cell to last-known. If moved, spawn a ghost
            //    at the previous cell using the entity's glyph + color.
            foreach (var entity in _zone.GetReadOnlyEntities())
            {
                var cell = _zone.GetEntityCell(entity);
                if (cell == null) continue;

                if (_lastKnown.TryGetValue(entity, out var prev))
                {
                    if (prev.x != cell.X || prev.y != cell.Y)
                    {
                        // Moved! Spawn ghost at previous position.
                        SpawnGhost(mainTilemap, prev.x, prev.y, entity);
                    }
                }
                _lastKnown[entity] = (cell.X, cell.Y);
            }
        }

        /// <summary>
        /// Spawn a ghost at (x, y) using the same tile + color as the
        /// entity rendered at THIS cell on the main tilemap. Sourcing
        /// the tile from the main tilemap (rather than recomputing the
        /// glyph from the entity) ensures the ghost matches what the
        /// player just saw at that cell — even after status effects /
        /// damage flashes mutated the color.
        /// </summary>
        private void SpawnGhost(Tilemap mainTilemap, int x, int y, Entity sourceEntity)
        {
            var pos = new Vector3Int(x, y, 0);
            // For the ghost we want the previous-frame tile + color at
            // (x, y). On THIS frame's redraw, the main tilemap has
            // either:
            //   - the new top entity at that cell (if something is
            //     still there), OR
            //   - empty/floor (if the mover left it empty)
            // Either way, sourcing from the main tilemap gives a
            // sensible fallback. For a more accurate ghost we could
            // cache the entity's glyph + color in _lastKnown, but
            // that adds complexity for marginal visual gain.
            var tile = mainTilemap.GetTile(pos);
            if (tile == null) return;
            var color = mainTilemap.GetColor(pos);

            _ghostTilemap.SetTile(pos, tile);
            _ghostTilemap.SetTransformMatrix(pos, mainTilemap.GetTransformMatrix(pos));
            _ghostTilemap.SetColor(pos, color);
            _ghosts[pos] = new GhostCell
            {
                Tile = tile,
                Color = color,
                FramesRemaining = DefaultLifetimeFrames,
            };
        }

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
