using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Source-aligned scene views. Simulation owns presence, collision and actions;
    /// this adapter owns only native sprites, a cell-sized fog texture and visual picking.</summary>
    public sealed class FellingScenePresenter : MonoBehaviour
    {
        public const string ShaderResource = "SceneArt/FellingSite/FellingScene";
        private const int ArtColumns = 48, ArtRows = 32;
        private sealed class View
        {
            public string Id;
            public int X, Y, Width, Height, AnchorX, AnchorY;
            public bool Mutable, Foreground;
            public float SortFootY;
            public SpriteRenderer Sprite, Contact;
            public Color32[] Pixels;
            public byte[] Coverage;
        }
        private readonly List<View> _views = new List<View>(55);
        private readonly HashSet<Entity> _owners = new HashSet<Entity>();
        private readonly int[] _overhangRows = new int[ArtColumns];
        private readonly Color32[] _fogPixels = new Color32[ArtColumns * ArtRows];
        private MaterialPropertyBlock _properties;
        private Texture2D _fog;
        private Material _material;
        private GameObject _content;
        private bool _presentationVisible = true;
        private string _failure;
        public Zone CurrentZone { get; private set; }
        public bool IsReady { get; private set; }
        public bool PresentationVisible => _presentationVisible && IsReady;
        public int ComponentCount => _views.Count;
        public string Failure => _failure;

        public static Vector2 ImageToWorld(Vector2 point) => new Vector2(16 + point.x / 32, 32 - point.y / 32);
        public static Vector2 WorldToImage(Vector2 point) => new Vector2((point.x - 16) * 32, (32 - point.y) * 32);
        public static bool TryImageToCell(Vector2 point, out int x, out int y)
        {
            x = y = -1;
            if (!Finite(point.x) || !Finite(point.y) || point.x < 0 || point.x >= 1536 || point.y < 224 || point.y >= 1024) return false;
            x = 16 + Mathf.FloorToInt(point.x / 32);
            y = Mathf.FloorToInt((point.y - 224) / 32);
            return true;
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        /// <summary>Each column projects its visible vertical cliff face from the art's
        /// top through that column's front opaque boundary. This is the representative
        /// owner; Refresh also observes the face from three cells immediately in front
        /// of it. Ground below the face retains its exact cell.</summary>
        public static Vector2Int FogOwnerForImageCell(int x, int row, int[] overhangRows)
        {
            if (x < 0 || x >= ArtColumns || row < 0 || row >= ArtRows || overhangRows == null || overhangRows.Length != ArtColumns)
                throw new ArgumentOutOfRangeException();
            return new Vector2Int(16 + x, Mathf.Max(overhangRows[x], row - 7));
        }
        public static float DepthForFootPixel(float y) => -((y - 224) / 32 - 1) * 0.001f;
        public static float CameraHalfHeight(float aspect) => Finite(aspect) && aspect > 0 ? Mathf.Max(16, 24 / aspect) : 16;

        public bool ClaimsCell(int x, int y) => PresentationVisible && x >= 16 && x < 64 && y >= 0 && y < 25;
        public bool IsAuthoredEntity(Entity entity) => entity != null && (_owners.Contains(entity)
            || entity.HasTag("FellingAuthoredTerrain") || entity.BlueprintName == "FellingScar"
            || entity.BlueprintName == "FellingBarePosition" || entity.BlueprintName == "SeventhPosition"
            || entity.BlueprintName == "FellingSceneState");

        public void Bind(Zone zone)
        {
            if (ReferenceEquals(CurrentZone, zone) && IsReady) return;
            Unbind();
            CurrentZone = zone;
            if (zone == null || !FellingSceneRuntime.IsActive(zone)) return;
            try
            {
                var definition = FellingSceneDefinition.Load();
                if (definition == null) throw new InvalidOperationException("missing definition");
                Shader shader = Resources.Load<Shader>(ShaderResource);
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("missing or unsupported scene shader");
                _fog = new Texture2D(ArtColumns, ArtRows, TextureFormat.RGBA32, false, true)
                { name = "Felling cell fog", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                _material = new Material(shader) { name = "Felling scene runtime", hideFlags = HideFlags.DontSave };
                _properties = new MaterialPropertyBlock();
                _material.SetTexture("_FellingFog", _fog);
                _content = new GameObject("Felling layered art");
                _content.transform.SetParent(transform, false);
                _content.layer = gameObject.layer;

                Array.Clear(_overhangRows, 0, _overhangRows.Length);
                // These rows are frozen from authored geometry, not dynamic visibility.
                // Later removed props cannot transfer ownership of the monumental trunk.
                foreach (var cell in definition.cells)
                    if (cell.opaque && cell.x >= 16 && cell.x < 64)
                        _overhangRows[cell.x - 16] = Mathf.Max(_overhangRows[cell.x - 16], cell.y);

                MakeSprite("Backing", definition.baseResource, new[] { 0, 0, 1536, 1024 }, 3, 0.5f);
                var layers = new List<FellingSceneDefinition.Layer>(definition.layers);
                layers.Sort((a, b) => a.depth != b.depth ? a.depth.CompareTo(b.depth) : string.CompareOrdinal(a.id, b.id));
                foreach (var layer in layers)
                {
                    bool foreground = layer.mutable || layer.id == "southwest-foreground-rock"
                        || layer.id == "southeast-foreground-root" || layer.id == "southwest-root-arch";
                    int order = foreground ? AnimatedEntityRenderer.BodySortingOrder : 3;
                    float depth = foreground ? DepthForFootPixel(layer.footPixelY) : 0.4f - layer.depth * 0.00001f;
                    var view = new View
                    {
                        Id = layer.id, X = layer.bounds[0], Y = layer.bounds[1], Width = layer.bounds[2], Height = layer.bounds[3],
                        AnchorX = layer.anchorX, AnchorY = layer.anchorY, Mutable = layer.mutable,
                        Foreground = foreground, SortFootY = layer.footPixelY,
                        Sprite = MakeSprite(layer.id, layer.resource, layer.bounds, order, depth),
                    };
                    view.Pixels = view.Sprite.sprite.texture.GetPixels32();
                    if (!string.IsNullOrEmpty(layer.contactResource))
                        view.Contact = MakeSprite(layer.id + " contact", layer.contactResource, layer.bounds, order, depth);
                    view.Coverage = new byte[view.Pixels.Length];
                    Color32[] contactPixels = view.Contact != null ? view.Contact.sprite.texture.GetPixels32() : null;
                    for (int i = 0; i < view.Coverage.Length; i++)
                        view.Coverage[i] = (byte)(view.Pixels[i].a | (contactPixels != null ? contactPixels[i].a : 0));
                    _views.Add(view);
                }
                ResolveForegroundDepths();
                IsReady = true;
                Refresh();
                _content.SetActive(_presentationVisible);
            }
            catch (Exception exception)
            {
                Unbind();
                CurrentZone = zone;
                _failure = exception.Message;
                Debug.LogWarning("[FellingScene] Terrain fallback: " + _failure);
                MessageLog.Add("The Felling scene artwork is unavailable; terrain view remains playable.");
            }
        }

        /// <summary>Source compositing is an explicit precedence constraint: plants
        /// painted over a foreground root must stay above that root. A shared alpha
        /// pixel transfers only visual depth, never the object's interaction anchor or
        /// collision cell. This runs once when binding, not during movement or refresh.</summary>
        private void ResolveForegroundDepths()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                View view = _views[i];
                if (!view.Foreground) continue;
                for (int j = 0; j < i; j++)
                {
                    View earlier = _views[j];
                    if (earlier.Foreground && earlier.SortFootY > view.SortFootY && CoverageOverlaps(earlier, view))
                        view.SortFootY = earlier.SortFootY;
                }
                // Stable source rank breaks equal-foot ties. Contact belongs just
                // behind its owner, still above every preceding overlapping source.
                float depth = DepthForFootPixel(view.SortFootY) - i * 0.0000001f;
                SetDepth(view.Sprite, depth);
                if (view.Contact != null) SetDepth(view.Contact, depth + 0.00000001f);
            }
        }

        private static void SetDepth(SpriteRenderer renderer, float depth)
        {
            Vector3 position = renderer.transform.position;
            position.z = depth;
            renderer.transform.position = position;
        }

        private static bool CoverageOverlaps(View a, View b)
        {
            int left = Mathf.Max(a.X, b.X), top = Mathf.Max(a.Y, b.Y);
            int right = Mathf.Min(a.X + a.Width, b.X + b.Width), bottom = Mathf.Min(a.Y + a.Height, b.Y + b.Height);
            for (int y = top; y < bottom; y++)
                for (int x = left; x < right; x++)
                {
                    int ai = (a.Height - 1 - (y - a.Y)) * a.Width + x - a.X;
                    int bi = (b.Height - 1 - (y - b.Y)) * b.Width + x - b.X;
                    if (a.Coverage[ai] != 0 && b.Coverage[bi] != 0) return true;
                }
            return false;
        }

        private SpriteRenderer MakeSprite(string name, string resource, int[] bounds, int order, float depth)
        {
            Sprite sprite = Resources.Load<Sprite>(resource);
            if (sprite == null || sprite.texture == null || !sprite.texture.isReadable
                || sprite.rect.width != bounds[2] || sprite.rect.height != bounds[3]
                || Mathf.Abs(sprite.pixelsPerUnit - 32) > 0.001f || sprite.pivot != Vector2.zero)
                throw new InvalidOperationException("invalid 32 PPU native sprite: " + resource);
            var go = new GameObject(name);
            go.transform.SetParent(_content.transform, false);
            go.layer = gameObject.layer;
            Vector2 bottomLeft = ImageToWorld(new Vector2(bounds[0], bounds[1] + bounds[3]));
            go.transform.position = new Vector3(bottomLeft.x, bottomLeft.y, depth);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>Called on existing full/cell invalidations, never as a per-frame scan.</summary>
        public void Refresh()
        {
            if (!IsReady || CurrentZone == null) return;
            _owners.Clear();
            float remembered = ZoneRenderer.RememberedBrightnessFor(CurrentZone.AmbientLevel);
            for (int x = 0; x < ArtColumns; x++)
            {
                // Shadowcasting sees a vertical face from the local ground in front
                // of it, even when its opaque footprint hides the footprint's center.
                // This bounded observation band never transfers visibility sideways,
                // to ground below the face, or to mutable object anchors.
                byte facade = 0;
                int boundary = _overhangRows[x];
                for (int offset = 0; offset <= 3 && boundary + offset < Zone.Height; offset++)
                    facade = (byte)Mathf.Max(facade, FogValue(CurrentZone.GetCell(16 + x, boundary + offset), remembered));
                for (int row = 0; row < ArtRows; row++)
                {
                    byte value = row <= 7 + boundary ? facade
                        : FogValue(CurrentZone.GetCell(16 + x, row - 7), remembered);
                    _fogPixels[(ArtRows - 1 - row) * ArtColumns + x] = new Color32(value, value, value, 255);
                }
            }
            _fog.SetPixels32(_fogPixels);
            _fog.Apply(false, false);
            foreach (var view in _views)
            {
                Entity owner = FellingSceneRuntime.FindOwner(CurrentZone, view.Id);
                if (owner != null) _owners.Add(owner);
                bool present = FellingSceneRuntime.IsPresent(CurrentZone, view.Id);
                view.Sprite.enabled = present;
                if (view.Contact != null) view.Contact.enabled = present;
                float fog = FogValue(CurrentZone.GetCell(view.AnchorX, view.AnchorY), remembered) / 255f;
                _properties.Clear();
                _properties.SetVector("_OwnerFog", new Vector4(view.Mutable ? 1 : 0, fog, 0, 0));
                view.Sprite.SetPropertyBlock(_properties);
                if (view.Contact != null) view.Contact.SetPropertyBlock(_properties);
            }
        }

        private static byte FogValue(Cell cell, float remembered) => cell == null || !cell.Explored ? (byte)0
            : cell.IsVisible ? (byte)255 : (byte)Mathf.RoundToInt(remembered * 255);

        public void SetPresentationVisible(bool visible)
        {
            _presentationVisible = visible;
            if (_content != null) _content.SetActive(visible);
        }

        public bool TryPickImage(Vector2 point, out int x, out int y) => TryPickImage(point, out x, out y, out _);

        /// <summary>Only live visible owner pixels are interactive. Transparency falls
        /// through to the next component, never to a sprite's bounding rectangle.</summary>
        public bool TryPickImage(Vector2 point, out int x, out int y, out Entity owner)
            => TryPickImage(point, out x, out y, out owner, out _, out _);

        public bool TryPickImage(Vector2 point, out int x, out int y, out Entity owner, out int sortingOrder, out float depth)
        {
            x = y = -1; owner = null; sortingOrder = 0; depth = 0;
            if (!PresentationVisible || !Finite(point.x) || !Finite(point.y)) return false;
            View selected = null;
            foreach (var view in _views)
            {
                if (!view.Sprite.enabled || point.x < view.X || point.x >= view.X + view.Width
                    || point.y < view.Y || point.y >= view.Y + view.Height) continue;
                byte fog = view.Mutable ? FogValue(CurrentZone.GetCell(view.AnchorX, view.AnchorY), 0)
                    : _fogPixels[(ArtRows - 1 - Mathf.FloorToInt(point.y / 32)) * ArtColumns + Mathf.FloorToInt(point.x / 32)].r;
                if (fog != 255) continue;
                int px = Mathf.FloorToInt(point.x) - view.X;
                int py = view.Height - 1 - (Mathf.FloorToInt(point.y) - view.Y);
                if (view.Pixels[py * view.Width + px].a == 0) continue;
                if (selected == null || view.Sprite.sortingOrder > selected.Sprite.sortingOrder
                    || (view.Sprite.sortingOrder == selected.Sprite.sortingOrder
                        && view.Sprite.transform.position.z <= selected.Sprite.transform.position.z)) selected = view;
            }
            if (selected == null) return false;
            owner = FellingSceneRuntime.FindOwner(CurrentZone, selected.Id);
            if (owner == null) return false;
            x = selected.AnchorX; y = selected.AnchorY;
            sortingOrder = selected.Sprite.sortingOrder; depth = selected.Sprite.transform.position.z;
            return true;
        }

        public void Unbind()
        {
            IsReady = false;
            _failure = null;
            _views.Clear(); _owners.Clear();
            if (_content != null) { _content.SetActive(false); DestroyOwned(_content); }
            if (_material != null) DestroyOwned(_material);
            if (_fog != null) DestroyOwned(_fog);
            _content = null; _material = null; _fog = null; CurrentZone = null;
        }
        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Unbind();
    }
}
