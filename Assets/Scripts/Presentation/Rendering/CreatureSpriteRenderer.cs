using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// 16×24 creature sprite layer (Phase A).
    ///
    /// Each frame, scans the active zone's entities. For every entity
    /// whose <c>BlueprintName</c> resolves to a sprite via
    /// <see cref="CreatureSpriteRegistry"/>, this renderer maintains a
    /// child <see cref="GameObject"/> with a <see cref="SpriteRenderer"/>
    /// positioned at the entity's cell. Sprites are 16×24 (1.0 × 1.5
    /// world units at PPU=16) with bottom-center pivot, so they occupy
    /// their cell's bottom and overflow the row above — the Qud look.
    ///
    /// Entities without a registry mapping are left untouched; their
    /// CP437 glyph still renders normally on the main tilemap. To hide
    /// the glyph behind a sprite, we additionally clear the entity's
    /// cell on the main tilemap each frame for every claimed entity
    /// (post-render scan, same idiom as
    /// <see cref="EnvironmentSpriteRenderer"/>).
    ///
    /// Sorting: <c>SortingOrder = OverlaySortingOrder * 1000 - y * 10</c>
    /// so lower-Y entities draw in front of higher-Y entities (back-to-
    /// front depth on a 2D grid).
    /// </summary>
    public class CreatureSpriteRenderer : MonoBehaviour
    {
        // Sit ABOVE every Pass 7-11 environment overlay (3) and the FX
        // tilemap (4). Pass 8 light hook reads scene Light2Ds and is
        // unaffected by sortingOrder. We use a per-entity SortingOrder
        // base and Y-bias (see below).
        private const int OverlaySortingOrderBase = 6;

        public bool RenderingEnabled = true;

        private Transform _gridParent;
        private Tilemap _mainTilemap;
        private readonly Dictionary<string, SpawnedEntity> _spawned = new();
        private readonly List<string> _toRetire = new();
        private readonly List<Vector3Int> _glyphCellsClearedThisFrame = new();
        private readonly List<TileBase> _restoreTiles = new();

        public bool IsInitialized { get; private set; }

        private struct SpawnedEntity
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public string LastBlueprintName;
        }

        public void Init(Transform gridParent, Tilemap mainTilemap)
        {
            _gridParent = gridParent;
            _mainTilemap = mainTilemap;
            IsInitialized = true;
        }

        public void PostRender(Zone zone)
        {
            if (!IsInitialized || _mainTilemap == null) return;
            if (!RenderingEnabled || zone == null)
            {
                RetireAll();
                return;
            }

            // Restore last frame's cleared glyph tiles before re-clearing
            // anything this frame. (Keeping it simple: we don't actually
            // need to restore — ZoneRenderer paints cells fresh each
            // frame anyway. So the clear-only side is enough.)
            _glyphCellsClearedThisFrame.Clear();

            var seenThisFrame = new HashSet<string>(_spawned.Count);
            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (e == null || string.IsNullOrEmpty(e.ID)) continue;
                if (string.IsNullOrEmpty(e.BlueprintName)) continue;

                if (!CreatureSpriteRegistry.TryGet(e.BlueprintName, out var sprite))
                    continue;
                if (sprite == null) continue;

                var (x, y) = zone.GetEntityPosition(e);
                if (x < 0 || y < 0) continue;

                seenThisFrame.Add(e.ID);

                if (!_spawned.TryGetValue(e.ID, out var sl))
                {
                    sl = SpawnEntity(e.ID);
                }
                if (sl.Renderer == null) continue;

                sl.Renderer.sprite = sprite;
                sl.Renderer.sortingOrder = OverlaySortingOrderBase * 1000 - y * 10;
                sl.Go.transform.localPosition = new Vector3(x + 0.5f, y, 0f);
                sl.LastBlueprintName = e.BlueprintName;
                _spawned[e.ID] = sl;

                // Hide the entity's CP437 glyph by clearing its main-tilemap cell.
                var cellPos = new Vector3Int(x, y, 0);
                _mainTilemap.SetTile(cellPos, null);
                _glyphCellsClearedThisFrame.Add(cellPos);
            }

            // Retire any sprite GameObjects whose entity isn't in the zone anymore.
            _toRetire.Clear();
            foreach (var kvp in _spawned)
                if (!seenThisFrame.Contains(kvp.Key))
                    _toRetire.Add(kvp.Key);
            for (int i = 0; i < _toRetire.Count; i++)
                RetireEntity(_toRetire[i]);
        }

        private SpawnedEntity SpawnEntity(string entityId)
        {
            var go = new GameObject($"CreatureSprite_{entityId}");
            go.transform.SetParent(_gridParent != null ? _gridParent : transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            // Slightly above background; Y-biased per frame.
            r.sortingOrder = OverlaySortingOrderBase * 1000;
            var sl = new SpawnedEntity { Go = go, Renderer = r, LastBlueprintName = null };
            _spawned[entityId] = sl;
            return sl;
        }

        private void RetireEntity(string entityId)
        {
            if (!_spawned.TryGetValue(entityId, out var sl)) return;
            if (sl.Go != null)
            {
                if (Application.isPlaying) Destroy(sl.Go);
                else DestroyImmediate(sl.Go);
            }
            _spawned.Remove(entityId);
        }

        private void RetireAll()
        {
            foreach (var kvp in _spawned)
            {
                if (kvp.Value.Go != null)
                {
                    if (Application.isPlaying) Destroy(kvp.Value.Go);
                    else DestroyImmediate(kvp.Value.Go);
                }
            }
            _spawned.Clear();
        }

        // ── Test seam ─────────────────────────────────────────────

        public int TestOnly_SpawnedCount => _spawned.Count;
    }
}
