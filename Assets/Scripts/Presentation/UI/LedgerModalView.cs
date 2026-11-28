using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>Presentation for centered menus. The caller still owns all selection and input state.</summary>
    internal sealed class LedgerModalView : MonoBehaviour
    {
        static readonly Color Dim = new Color(.01f, .025f, .024f, .52f);
        static readonly Color Shadow = new Color(0f, 0f, 0f, .42f);
        static readonly Rect FullViewport = new Rect(0, 0, 1, 1);
        readonly List<Image> _shapes = new List<Image>();
        readonly List<Text> _labels = new List<Text>();
        RectTransform _card;
        Image _backdrop;
        RawImage _portrait;
        bool _portraitPainted;
        Camera _camera;
        Font _font;
        int _shapeCount, _labelCount;

        internal static LedgerModalView Create(Transform parent, string name, int sortingOrder = 100)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var view = root.AddComponent<LedgerModalView>();
            view._font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            view._backdrop = Graphic<Image>(root.transform, "Backdrop");
            view._backdrop.color = Dim;
            view._card = new GameObject("Card", typeof(RectTransform)).GetComponent<RectTransform>();
            view._card.SetParent(root.transform, false);
            root.SetActive(false);
            return view;
        }

        internal void Begin(Camera camera, int width, int height, bool dim = true)
        {
            _camera = camera;
            gameObject.SetActive(true);
            _backdrop.gameObject.SetActive(dim);
            _card.sizeDelta = new Vector2(width, height);
            _shapeCount = _labelCount = 0;
            _portraitPainted = false;
            PositionCard();
        }

        internal void Card(string title, string footer = null)
        {
            float width = _card.rect.width, height = _card.rect.height;
            Box(new Rect(6, 8, width, height), Shadow);
            Frame(new Rect(0, 0, width, height), LBorder, LBackground);
            Box(new Rect(0, 0, 4, 64), LGold);
            Box(new Rect(24, 62, width - 48, 1), LBorder);
            Label(new Rect(24, 11, width - 48, 45), title, LInk, 27, bold: true);
            if (string.IsNullOrEmpty(footer)) return;
            Box(new Rect(24, height - 44, width - 48, 1), LBorder);
            Label(new Rect(24, height - 42, width - 48, 40), footer, LMuted, 16);
        }

        internal void Frame(Rect bounds, Color border, Color fill)
        {
            Box(bounds, border);
            Box(new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, bounds.height - 2), fill);
        }

        internal void Box(Rect bounds, Color color) => PaintImage(bounds, color, null);

        internal void Portrait(Rect bounds, ExaminePortrait portrait)
        {
            if (portrait.Texture == null || bounds.width <= 0 || bounds.height <= 0) return;
            if (_portrait == null) _portrait = Graphic<RawImage>(_card, "Portrait");
            Paint(_portrait, bounds);
            _portrait.texture = portrait.Texture;
            _portrait.uvRect = portrait.Uv;
            _portrait.color = portrait.Tint;
            _portraitPainted = true;
        }

        void PaintImage(Rect bounds, Color color, Sprite sprite)
        {
            if (_shapeCount == _shapes.Count) _shapes.Add(Graphic<Image>(_card, "Shape"));
            var graphic = _shapes[_shapeCount++];
            Paint(graphic, bounds);
            graphic.color = color;
            graphic.sprite = sprite;
            graphic.preserveAspect = sprite != null;
        }

        internal Text Label(Rect bounds, string value, Color color, int size = 20,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false, bool bold = false, bool fit = false)
        {
            if (_labelCount == _labels.Count) _labels.Add(Graphic<Text>(_card, "Label"));
            var label = _labels[_labelCount++];
            Paint(label, bounds);
            label.font = _font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.color = color;
            label.supportRichText = false;
            label.alignment = anchor;
            label.horizontalOverflow = wrap || fit ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = fit;
            if (fit)
            {
                label.resizeTextMinSize = Mathf.Min(size, 12);
                label.resizeTextMaxSize = size;
            }
            label.text = wrap || fit ? value ?? string.Empty : Fit(value, bounds.width, size, label.fontStyle);
            return label;
        }

        readonly TextGenerator _measure = new TextGenerator();

        string Fit(string value, float width, int size, FontStyle style)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            bool Fits(string text) => _measure.GetPreferredWidth(text, new TextGenerationSettings
            {
                font = _font, fontSize = size, fontStyle = style, color = Color.white, lineSpacing = 1f, scaleFactor = 1f,
                horizontalOverflow = HorizontalWrapMode.Overflow
            }) <= width;
            if (Fits(value)) return value;
            int low = 0, high = value.Length;
            while (low < high)
            {
                int length = (low + high + 1) / 2;
                int safe = SafeLength(length);
                if (Fits(value.Substring(0, safe) + "…")) low = length;
                else high = length - 1;
            }
            return value.Substring(0, SafeLength(low)).TrimEnd() + "…";

            int SafeLength(int n) => n > 0 && n < value.Length && char.IsHighSurrogate(value[n - 1]) ? n - 1 : n;
        }

        internal void Row(Rect bounds, string text, bool selected, string key = null, string aside = null,
            bool disabled = false, Sprite icon = null)
        {
            Box(bounds, selected ? LSelected : LPanel);
            if (selected) Box(new Rect(bounds.x, bounds.y, 3, bounds.height), disabled ? LMuted : LGold);
            float x = bounds.x + 12;
            if (!string.IsNullOrEmpty(key))
            {
                float h = Mathf.Min(25, bounds.height - 6);
                var badge = new Rect(x, bounds.center.y - h / 2, 30, h);
                Frame(badge, LBorder, LBackground);
                Label(badge, key, disabled ? LMuted : LGold, 14, TextAnchor.MiddleCenter);
                x += 42;
            }
            if (icon != null)
            {
                float h = Mathf.Min(27, bounds.height - 6);
                PaintImage(new Rect(x, bounds.center.y - h / 2, h, h), Color.white, icon);
                x += h + 10;
            }
            int asideWidth = string.IsNullOrEmpty(aside) ? 0 : 118;
            var label = new Rect(x, bounds.y, bounds.xMax - x - 12 - asideWidth, bounds.height);
            Label(label, text, disabled ? LMuted : LInk, 19, wrap: bounds.height >= 48);
            if (asideWidth > 0)
                Label(new Rect(bounds.xMax - asideWidth - 8, bounds.y, asideWidth, bounds.height),
                    aside, disabled ? LRed : LMuted, 15, TextAnchor.MiddleRight);
        }

        internal void End()
        {
            for (int i = _shapeCount; i < _shapes.Count; i++) _shapes[i].gameObject.SetActive(false);
            for (int i = _labelCount; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
            if (!_portraitPainted) ClearPortrait();
        }

        internal void Hide()
        {
            ClearPortrait();
            gameObject.SetActive(false);
        }

        void ClearPortrait()
        {
            if (_portrait == null) return;
            _portrait.texture = null;
            _portrait.gameObject.SetActive(false);
        }

        internal bool TryPoint(Vector2 screen, out Vector2 local)
        {
            local = default;
            if (!gameObject.activeInHierarchy) return false;
            PositionCard();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_card, screen, null, out var p)) return false;
            local = new Vector2(p.x - _card.rect.xMin, _card.rect.yMax - p.y);
            return new Rect(Vector2.zero, _card.rect.size).Contains(local);
        }

        internal static bool ReadPointer(out Vector2 pointer)
        {
            if (Mouse.current is { enabled: true } mouse)
            {
                pointer = mouse.position.ReadValue();
                return mouse.leftButton.wasPressedThisFrame;
            }
            pointer = Input.mousePosition;
            return Input.GetMouseButtonDown(0);
        }

        void LateUpdate() => PositionCard();

        void PositionCard()
        {
            if (_card == null) return;
            var viewport = _camera != null ? _camera.rect : FullViewport;
            if (viewport.width <= 0 || viewport.height <= 0) viewport = FullViewport;
            Vector2 center = viewport.center;
            if (_card.anchorMin != center || _card.anchorMax != center)
                _card.anchorMin = _card.anchorMax = center;
            var backdrop = _backdrop.rectTransform;
            if (backdrop.anchorMin != viewport.min || backdrop.anchorMax != viewport.max)
            {
                backdrop.anchorMin = viewport.min;
                backdrop.anchorMax = viewport.max;
                backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            }
            Vector2 available = Vector2.Scale(((RectTransform)transform).rect.size, viewport.size);
            Vector2 needed = _card.rect.size + new Vector2(12, 16);
            float scale = Mathf.Clamp01(Mathf.Min(Mathf.Max(1, available.x - 24) / needed.x,
                Mathf.Max(1, available.y - 24) / needed.y));
            var fit = new Vector3(scale, scale, 1);
            if (_card.localScale != fit) _card.localScale = fit;
        }

        static T Graphic<T>(Transform parent, string name) where T : Graphic
        {
            var view = new GameObject(name, typeof(RectTransform)).AddComponent<T>();
            view.transform.SetParent(parent, false);
            view.raycastTarget = false;
            return view;
        }

        static void Paint(Graphic graphic, Rect bounds)
        {
            var rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
            rect.SetAsLastSibling();
            graphic.gameObject.SetActive(true);
        }
    }
}
