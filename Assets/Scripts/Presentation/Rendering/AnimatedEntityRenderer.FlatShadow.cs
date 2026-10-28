using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public sealed partial class AnimatedEntityRenderer
    {
        [SerializeField] private bool _flatContactShadowEnabled = true;
        private const float ContactShadowOpacity = .18f;
        private static readonly Vector3 ContactShadowOffset = new Vector3(1f / 16f, 2f / 16f, 0f);

        /// <summary>Reversible presentation-only trial: current Sill player contact
        /// shade on fully disclosed ordinary dry ground. No simulation or save state.</summary>
        public bool FlatContactShadowEnabled
        {
            get => _flatContactShadowEnabled;
            set
            {
                _flatContactShadowEnabled = value;
                foreach (var view in _views.Values)
                    if (view.Flat) UpdateFlatContactShadow(view);
            }
        }

        private void ConfigureShadow(EntityView view)
        {
            // The pooled renderer also serves the legacy presentation. Restore
            // every setting when acquired, including after native -> legacy reuse.
            view.Shadow.sprite = _shadowSprite;
            view.Shadow.transform.localPosition = view.Flat ? ContactShadowOffset : new Vector3(0f, .10f, 0f);
            view.Shadow.transform.localScale = Vector3.one;
            view.Shadow.color = view.Flat ? new Color(0f, 0f, 0f, ContactShadowOpacity)
                : new Color(.10f, .13f, .11f, .55f);
            view.Shadow.sharedMaterial = view.Flat ? _flatMaterial : _sharedMaterial;
            view.Shadow.sortingOrder = ShadowSortingOrder;
        }

        private void UpdateFlatContactShadow(EntityView view)
        {
            view.Shadow.enabled = false;
            if (!_flatContactShadowEnabled || !view.Flat || view.Dying || !_flatPixelMode
                || !_presentationVisible || !enabled || _worldTilemap == null || _flatMaterial == null
                || view.Entity?.BlueprintName != "Player" || !view.Entity.HasTag("Player")
                || !view.Root.activeInHierarchy || !view.Body.enabled || !(view.Body.color.a > 0f)
                || !FlatPixelSillEnvelopeCatalog.IsSupportedZone(_zone)
                || !ValidFlatOwner(view.Entity, view.CellX, view.CellY, false)) return;

            var sprite = view.Shadow.sprite;
            if (_shadowSprite == null || sprite != _shadowSprite || sprite.texture == null
                || sprite.rect.size != new Vector2(10, 4) || sprite.pivot != new Vector2(5, 2)
                || sprite.pixelsPerUnit != 16f || sprite.texture.filterMode != FilterMode.Point) return;

            // Check the actual displayed footprint, not just the logical destination.
            // This prevents a moving shadow remaining over a departed fog/water cell.
            Bounds bounds = view.Shadow.bounds;
            var min = _worldTilemap.WorldToCell(bounds.min + new Vector3(ReadoutBoundsEpsilon, ReadoutBoundsEpsilon, 0));
            var max = _worldTilemap.WorldToCell(bounds.max - new Vector3(ReadoutBoundsEpsilon, ReadoutBoundsEpsilon, 0));
            if (min.x > max.x || min.y > max.y || max.x - min.x > 1 || max.y - min.y > 1) return;
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                    if (!FlatPixelSillEnvelopeCatalog.CanReceiveContactShadow(_zone, x, Zone.Height - 1 - y, view.Entity)) return;
            view.Shadow.enabled = true;
        }
    }
}
