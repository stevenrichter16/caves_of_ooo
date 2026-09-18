using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Additional native harvest/pickup objects, separate from the verified
    /// source composition. Entity membership and position remain authoritative.</summary>
    public sealed class FellingDressingPresenter : MonoBehaviour
    {
        private sealed class View
        {
            public FellingScenePopulation.DressingSpec Spec;
            public SpriteRenderer Renderer;
            public Color32[] Pixels;
            public Entity Owner;
        }
        private readonly List<View> _views = new List<View>(3);
        private GameObject _content;
        private Material _material;
        private bool _visible = true;
        public Zone CurrentZone { get; private set; }
        public bool IsReady { get; private set; }
        public bool PresentationVisible => IsReady && _visible;
        public string Failure { get; private set; }

        public void Bind(Zone zone)
        {
            if (ReferenceEquals(CurrentZone, zone) && IsReady) return;
            Unbind(); CurrentZone = zone;
            if (!FellingSceneRuntime.IsActive(zone)) return;
            try
            {
                Shader shader = Resources.Load<Shader>(FellingScenePresenter.ShaderResource);
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("missing scene shader");
                _material = new Material(shader) { name = "Felling supplemental art", hideFlags = HideFlags.DontSave };
                _material.SetVector("_OwnerFog", new Vector4(1, 1, 0, 0));
                _content = new GameObject("Felling supplemental views");
                _content.transform.SetParent(transform, false); _content.layer = gameObject.layer;
                foreach (var spec in FellingScenePopulation.DressingSpecs)
                {
                    Sprite sprite = Resources.Load<Sprite>(spec.resource);
                    if (sprite == null || !sprite.texture.isReadable || sprite.rect.width != spec.width
                        || sprite.rect.height != spec.height || sprite.pivot != Vector2.zero
                        || Mathf.Abs(sprite.pixelsPerUnit - 32) > 0.001f)
                        throw new InvalidOperationException("invalid supplemental sprite: " + spec.resource);
                    var child = new GameObject(spec.id);
                    child.transform.SetParent(_content.transform, false); child.layer = gameObject.layer;
                    var renderer = child.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite; renderer.sharedMaterial = _material;
                    renderer.sortingOrder = AnimatedEntityRenderer.BodySortingOrder;
                    _views.Add(new View { Spec = spec, Renderer = renderer, Pixels = sprite.texture.GetPixels32() });
                }
                IsReady = true; Refresh(); _content.SetActive(_visible);
            }
            catch (Exception exception)
            {
                Unbind(); CurrentZone = zone; Failure = exception.Message;
                Debug.LogWarning("[FellingScene] Supplemental objects use terrain glyphs: " + Failure);
            }
        }

        /// <summary>Only three known owners are resolved on existing dirty/full
        /// invalidations. Inventory membership and exterior drops release fallback.</summary>
        public void Refresh()
        {
            if (!IsReady) return;
            foreach (var view in _views)
            {
                view.Owner = FellingScenePopulation.FindDressingOwner(CurrentZone, view.Spec.id);
                var cell = view.Owner != null ? CurrentZone.GetEntityCell(view.Owner) : null;
                var position = view.Owner != null ? CurrentZone.GetEntityPosition(view.Owner) : (-1, -1);
                bool shown = cell != null && cell.Explored && cell.IsVisible && InArt(position.Item1, position.Item2);
                view.Renderer.enabled = shown;
                if (!shown) continue;
                // The supplied foot coordinate is top-origin within the native PNG.
                // World actors stand at the bottom center of their actual grid cell.
                view.Renderer.transform.position = new Vector3(position.Item1 + 0.5f - view.Spec.footX / 32,
                    Zone.Height - 1 - position.Item2 - (view.Spec.height - view.Spec.footY) / 32,
                    -position.Item2 * 0.001f);
            }
        }

        private static bool InArt(int x, int y) => x >= 16 && x < 64 && y >= 0 && y < Zone.Height;

        public bool IsRenderedEntity(Entity entity)
        {
            if (!PresentationVisible || entity == null) return false;
            foreach (var view in _views)
                if (ReferenceEquals(view.Owner, entity) && view.Renderer.enabled
                    && CurrentZone.GetEntityCell(entity) != null) return true;
            return false;
        }

        public bool TryPickWorld(Vector2 point, out Entity owner, out int x, out int y)
            => TryPickWorld(point, out owner, out x, out y, out _);

        public bool TryPickWorld(Vector2 point, out Entity owner, out int x, out int y, out float depth)
        {
            owner = null; x = y = -1; depth = 0;
            if (!PresentationVisible || float.IsNaN(point.x) || float.IsNaN(point.y)
                || float.IsInfinity(point.x) || float.IsInfinity(point.y)) return false;
            View selected = null;
            foreach (var view in _views)
            {
                if (!IsRenderedEntity(view.Owner)) continue;
                var cell = CurrentZone.GetEntityCell(view.Owner);
                if (!cell.Explored || !cell.IsVisible) continue;
                Vector2 pixel = (point - (Vector2)view.Renderer.transform.position) * 32;
                if (pixel.x < 0 || pixel.y < 0 || pixel.x >= view.Spec.width || pixel.y >= view.Spec.height) continue;
                if (view.Pixels[Mathf.FloorToInt(pixel.y) * view.Spec.width + Mathf.FloorToInt(pixel.x)].a == 0) continue;
                if (selected == null || view.Renderer.transform.position.z <= selected.Renderer.transform.position.z) selected = view;
            }
            if (selected == null) return false;
            owner = selected.Owner; var position = CurrentZone.GetEntityPosition(owner);
            x = position.x; y = position.y; depth = selected.Renderer.transform.position.z;
            return true;
        }

        public void SetPresentationVisible(bool visible)
        {
            _visible = visible;
            if (_content != null) _content.SetActive(visible);
        }
        public void Unbind()
        {
            IsReady = false; Failure = null; _views.Clear();
            if (_content != null) { _content.SetActive(false); DestroyOwned(_content); }
            if (_material != null) DestroyOwned(_material);
            _content = null; _material = null; CurrentZone = null;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Unbind();
    }
}
