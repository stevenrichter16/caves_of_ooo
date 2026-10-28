using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    public partial class EnvironmentSpriteRenderer
    {
        [SerializeField] private bool _flatWallShadowEnabled = true;
        private bool _flatWallShadowLastEnabled = true;
        private Tilemap _flatGroundShadowTilemap;
        private Material _flatGroundShadowMaterial;
        private static readonly Matrix4x4 WallShadowOffset = Matrix4x4.Translate(new Vector3(0f, -2f / 16f, 0f));
        private static readonly Color WallShadowColor = new Color(0f, 0f, 0f, .16f);

        /// <summary>Reversible Sill masonry trial; no saved gameplay state.
        /// Fixed screen-north light convention, not simulated solar/torch lighting.</summary>
        public bool FlatWallShadowEnabled
        {
            get => _flatWallShadowEnabled;
            set
            {
                _flatWallShadowEnabled = value;
                SyncFlatWallShadowOption(_flatStateOwner);
            }
        }

        private void SyncFlatWallShadowOption(ZoneRenderer owner)
        {
            if (_flatWallShadowLastEnabled == _flatWallShadowEnabled) return;
            if (!_flatWallShadowEnabled) _flatGroundShadowTilemap?.ClearAllTiles();
            _flatWallShadowLastEnabled = _flatWallShadowEnabled;
            owner?.MarkDirty("Presentation.FlatWallShadow");
        }

        private void InitFlatWallShadow(Transform gridParent)
        {
            var go = new GameObject("FlatGroundShadowTilemap");
            go.transform.SetParent(gridParent, false);
            go.layer = GameplayRenderLayers.WorldLayer;
            _flatGroundShadowTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0; // Ground -1, physical twine 1, wall 3, actor 6.
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return; // Optional visual layer; keep existing bodies.
            _flatGroundShadowMaterial = new Material(shader)
                { name = "FlatWallShadow-Unlit", hideFlags = HideFlags.HideAndDontSave };
            renderer.sharedMaterial = _flatGroundShadowMaterial;
        }

        private void PaintFlatWallShadow(Zone zone, int x, int y, Vector3Int pos, in FlatPixelGreyboxCell flat)
        {
            if (!_flatWallShadowEnabled || _flatGroundShadowTilemap == null || _flatGroundShadowMaterial == null
                || flat.Remembered || !flat.NativeForeground || flat.Owner?.BlueprintName != "StoneWall"
                || !FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone)
                || !FlatPixelSillEnvelopeCatalog.CanReceiveContactShadow(zone, x, y + 1, null)) return;
            var tile = flat.ForegroundTile;
            var sprite = tile == null ? null : tile.sprite;
            if (sprite == null || sprite.texture == null || sprite.rect.size != new Vector2(16, 24)
                || sprite.pivot != new Vector2(8, 12) || sprite.pixelsPerUnit != 16f
                || sprite.texture.filterMode != FilterMode.Point) return;

            // The selected approved opaque wall covers its own duplicate. Only
            // two native pixels extend south onto the fully disclosed receiver.
            // Source anchoring avoids painting an entire wall-sized dark receiver.
            _flatGroundShadowTilemap.SetTile(pos, tile);
            _flatGroundShadowTilemap.SetTileFlags(pos, TileFlags.None);
            _flatGroundShadowTilemap.SetColor(pos, WallShadowColor);
            _flatGroundShadowTilemap.SetTransformMatrix(pos, WallShadowOffset);
            Perf.TilemapWrites += 4;
        }

        private void ClearFlatWallShadow(Vector3Int pos)
        {
            if (_flatGroundShadowTilemap == null || !_flatGroundShadowTilemap.HasTile(pos)) return;
            _flatGroundShadowTilemap.SetTile(pos, null);
            _flatGroundShadowTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
            _flatGroundShadowTilemap.SetColor(pos, Color.white);
            Perf.TilemapWrites += 3;
        }

        private void DisposeFlatWallShadow()
        {
            if (_flatGroundShadowTilemap != null)
            {
                var go = _flatGroundShadowTilemap.gameObject;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                _flatGroundShadowTilemap = null;
            }
            if (_flatGroundShadowMaterial != null)
            {
                if (Application.isPlaying) Destroy(_flatGroundShadowMaterial); else DestroyImmediate(_flatGroundShadowMaterial);
                _flatGroundShadowMaterial = null;
            }
            // Tile/sprite/texture are borrowed from the existing catalog.
        }
    }
}
