using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    public partial class EnvironmentSpriteRenderer
    {
        [SerializeField] private bool _flatWallShadowEnabled = true;
        private bool _flatWallShadowLastEnabled = true;
        [SerializeField] private bool _flatTreeContactShadowEnabled = true;
        private bool _flatTreeContactShadowLastEnabled = true;
        private Tile _flatTreeContactTile;
        private static readonly Matrix4x4 TreeContactOffset = Matrix4x4.Translate(new Vector3(1f / 16f, -10f / 16f, 0f));
        private static readonly Color TreeContactColor = new Color(0f, 0f, 0f, .18f);
        private Tilemap _flatGroundShadowTilemap;
        private Material _flatGroundShadowMaterial;
        private static readonly Matrix4x4 WallShadowOffset = Matrix4x4.Translate(new Vector3(0f, -2f / 16f, 0f));
        private static readonly Color WallShadowColor = new Color(0f, 0f, 0f, .16f);

        /// <summary>Reversible Sill/Beating/Marrowstye/Gantry/Cinderhold wall trials; no saved gameplay state.
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

        /// <summary>Bounded Spread10.15 root contact only; independent of masonry shading.</summary>
        public bool FlatTreeContactShadowEnabled
        {
            get => _flatTreeContactShadowEnabled;
            set { _flatTreeContactShadowEnabled = value; SyncFlatWallShadowOption(_flatStateOwner); }
        }

        private void SyncFlatWallShadowOption(ZoneRenderer owner)
        {
            if (_flatWallShadowLastEnabled == _flatWallShadowEnabled
                && _flatTreeContactShadowLastEnabled == _flatTreeContactShadowEnabled) return;
            // Shared owned layer: repaint whichever independently enabled pilot remains.
            _flatGroundShadowTilemap?.ClearAllTiles();
            _flatWallShadowLastEnabled = _flatWallShadowEnabled;
            _flatTreeContactShadowLastEnabled = _flatTreeContactShadowEnabled;
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
            var mask = Resources.Load<Sprite>("Sprites/Actors/actor_shadow");
            if (ValidTreeContactMask(mask))
            {
                _flatTreeContactTile = ScriptableObject.CreateInstance<Tile>();
                _flatTreeContactTile.name = "FlatTreeRootContact";
                _flatTreeContactTile.hideFlags = HideFlags.HideAndDontSave;
                _flatTreeContactTile.sprite = mask;
                _flatTreeContactTile.colliderType = Tile.ColliderType.None;
            }
        }

        private static bool ValidTreeContactMask(Sprite sprite)
            => sprite != null && sprite.texture != null && sprite.rect.size == new Vector2(10, 4)
                && sprite.pivot == new Vector2(5, 2) && sprite.pixelsPerUnit == 16f
                && sprite.texture.filterMode == FilterMode.Point;

        private void PaintFlatWallShadow(Zone zone, int x, int y, Vector3Int pos, in FlatPixelGreyboxCell flat)
        {
            if (_flatGroundShadowTilemap == null || _flatGroundShadowMaterial == null
                || flat.Remembered || !flat.NativeForeground) return;
            if (flat.Owner?.BlueprintName == "Tree")
            {
                if (!_flatTreeContactShadowEnabled || _flatTreeContactTile == null
                    || !ValidTreeContactMask(_flatTreeContactTile.sprite) || _flatPixelGreybox == null
                    || !_flatPixelGreybox.CanReceiveTreeContactShadow(zone, x, y, flat.Owner)) return;
                // Borrow the small root mask, never a displaced copy of the canopy.
                // Its entire footprint stays within the disclosed source/grass cell.
                _flatGroundShadowTilemap.SetTile(pos, _flatTreeContactTile);
                _flatGroundShadowTilemap.SetTileFlags(pos, TileFlags.None);
                _flatGroundShadowTilemap.SetColor(pos, TreeContactColor);
                _flatGroundShadowTilemap.SetTransformMatrix(pos, TreeContactOffset);
                Perf.TilemapWrites += 4;
                return;
            }
            if (!_flatWallShadowEnabled) return;
            bool receiver = flat.Owner?.BlueprintName == "StoneWall"
                ? FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone)
                    && FlatPixelSillEnvelopeCatalog.CanReceiveContactShadow(zone, x, y + 1, null)
                : flat.Owner?.BlueprintName == "SandstoneWall"
                    && (FlatPixelBeatingMaterialsCatalog.CanReceiveWallShadow(zone, x, y + 1)
                        || _flatPixelGreybox != null && (_flatPixelGreybox.CanReceiveMarrowstyeWallShadow(zone, x, y + 1)
                            || _flatPixelGreybox.CanReceiveCinderholdWallShadow(zone, x, y + 1)));
            if (!receiver && flat.Owner?.BlueprintName == FlatPixelWildFaunaCatalog.GantryTimberBlueprint)
                receiver = _flatPixelGreybox != null
                    && _flatPixelGreybox.CanReceiveGantryWallShadow(zone, x, y + 1, flat.Owner);
            if (!receiver) return;
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
            if (_flatTreeContactTile != null)
            {
                if (Application.isPlaying) Destroy(_flatTreeContactTile); else DestroyImmediate(_flatTreeContactTile);
                _flatTreeContactTile = null;
            }
            // Wall tiles and all sprites/textures remain borrowed.
        }
    }
}
