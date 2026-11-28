using UnityEngine;
using UnityEngine.UI;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    internal sealed class GameplayHotbarLedgerView
    {
        private const float ReferenceHeight = 116f;
        private const float MinimumSlotWidth = 80f;
        private const float IconTop = 27f;
        private const float NameBottomInset = 4f;
        private const string KeyboardCastHint = "[] cycle  [Enter] cast";

        private readonly Transform _owner;
        private readonly SlotView[] _cards = new SlotView[GameplayHotbarLayout.SlotCount];
        private Canvas _canvas;
        private RectTransform _strip;
        private Image _header;
        private Image _headerRule;
        private Text _summary;
        private Text _controls;
        private Font _font;
        private HotbarSnapshot _lastSnapshot;
        private Rect _lastViewport;
        private bool _lastController;

        internal GameplayHotbarLedgerView(Transform owner) => _owner = owner;

        internal bool Render(HotbarSnapshot snapshot, Camera camera)
        {
            if (camera == null || !camera.isActiveAndEnabled || camera.pixelWidth <= 0 || camera.pixelHeight <= 0)
            {
                Hide();
                return false;
            }

            EnsureView();
            Rect viewport = camera.pixelRect;
            bool controller = NativeGamepadInput.IsConnected;
            bool layoutChanged = !_canvas.gameObject.activeSelf || _canvas.worldCamera != camera || _lastViewport != viewport;
            if (!layoutChanged && ReferenceEquals(snapshot, _lastSnapshot) && controller == _lastController)
                return false;

            _canvas.gameObject.SetActive(true);
            _canvas.worldCamera = camera;
            _canvas.planeDistance = Mathf.Clamp(1f, camera.nearClipPlane + .01f, camera.farClipPlane - .01f);
            if (layoutChanged) Layout(viewport);

            string summary = snapshot?.SummaryText;
            SetFittedText(_summary, string.IsNullOrEmpty(summary) ? snapshot?.Title ?? "Grimoires"
                : summary.Replace(" | ", "   ·   "));
            SetFittedText(_controls, Controls(snapshot, controller));

            if (layoutChanged || !ReferenceEquals(snapshot, _lastSnapshot))
            {
                // The gameplay builder publishes an immutable, ordered ten-slot snapshot.
                bool aiming = snapshot != null && snapshot.PendingSlot >= 0;
                for (int i = 0; i < _cards.Length; i++)
                    DrawCard(_cards[i], snapshot != null && i < snapshot.Slots.Count ? snapshot.Slots[i] : default, i, aiming);
            }

            _lastSnapshot = snapshot;
            _lastViewport = viewport;
            _lastController = controller;
            return true;
        }

        internal void Hide()
        {
            if (_canvas != null && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
            _lastSnapshot = null;
        }

        internal void Dispose()
        {
            if (_canvas == null) return;
            Hide();
            if (Application.isPlaying) Object.Destroy(_canvas.gameObject);
            else Object.DestroyImmediate(_canvas.gameObject);
            _canvas = null;
        }

        private void EnsureView()
        {
            if (_canvas != null) return;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("GameplayHotbarLedger", typeof(RectTransform), typeof(Canvas));
            root.layer = GameplayRenderLayers.HotbarLayer;
            root.transform.SetParent(_owner, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.pixelPerfect = true;

            var background = Box("Hotbar Background", root.transform, LBackground);
            _strip = background.rectTransform;
            _strip.anchorMin = Vector2.zero;
            _strip.anchorMax = Vector2.one;
            _strip.offsetMin = _strip.offsetMax = Vector2.zero;
            _header = Box("Hotbar Header", _strip, LPanel);
            _headerRule = Box("Hotbar Rule", _strip, LBorder);
            _summary = Label("Selected Ability", _strip, 15, LInk, TextAnchor.MiddleLeft);
            _controls = Label("Ability Controls", _strip, 14, LMuted, TextAnchor.MiddleRight);

            for (int i = 0; i < _cards.Length; i++)
            {
                var frame = Box($"Hotbar Slot {HotbarStateBuilder.SlotToHotkey(i)}", _strip, LBorder);
                var fill = Box("Card Background", frame.transform, LPanel);
                var rail = Box("Current Slot", frame.transform, LGold);
                var keyBox = Box("Key Background", frame.transform, LBackground);
                var key = Label("Binding", keyBox.transform, 14, LMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
                var state = Label("Ability State", frame.transform, 12, LMuted, TextAnchor.MiddleRight);
                var icon = Graphic<Image>("Skill Icon", frame.transform);
                icon.preserveAspect = true;
                var name = Label("Ability Name", frame.transform, 15, LInk, TextAnchor.MiddleCenter);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 12;
                name.resizeTextMaxSize = 15;
                _cards[i] = new SlotView(frame, fill, rail, keyBox, key, state, icon, name);
            }

            root.SetActive(false);
        }

        private void Layout(Rect viewport)
        {
            float scale = Mathf.Min(viewport.height / ReferenceHeight,
                viewport.width / (GameplayHotbarLayout.SlotCount * MinimumSlotWidth));
            _canvas.scaleFactor = scale;
            float width = viewport.width / scale;
            float height = viewport.height / scale;
            float line = Mathf.Max(1f, 1f / scale);
            float headerHeight = height / GameplayHotbarLayout.GridHeight;
            float laneWidth = width * GameplayHotbarLayout.SlotWidth / GameplayHotbarLayout.GridWidth;
            float cardWidth = laneWidth - 4f;
            float cardHeight = height - headerHeight - 4f;
            float headerInset = 12f;
            float controlsWidth = width * .44f;

            Place(_header, new Rect(0, 0, width, headerHeight));
            Place(_headerRule, new Rect(0, 0, width, line));
            Place(_summary, new Rect(headerInset, 1, width - controlsWidth - 3 * headerInset, headerHeight - 1));
            Place(_controls, new Rect(width - controlsWidth - headerInset, 1, controlsWidth, headerHeight - 1));

            // Camera columns divide at the same edges as the ten lanes used by
            // GameplayHotbarLayout's pointer mapping. Insets sit within each lane.
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                Place(card.Frame, new Rect(i * laneWidth + 2f, headerHeight + 2f, cardWidth, cardHeight));
                Place(card.Fill, new Rect(line, line, cardWidth - 2f * line, cardHeight - 2f * line));
                Place(card.Rail, new Rect(0, 0, Mathf.Max(3f, line), cardHeight));
                Place(card.KeyBox, new Rect(8, 6, 20, 20));
                Place(card.Key, new Rect(0, 0, 20, 20));
                Place(card.State, new Rect(29, 6, cardWidth - 35, 20));
                // Long ability names can use two fitted lines even at the
                // narrowest card width, with no overlap with the icon.
                float iconSize = Mathf.Min(36f, cardWidth - 14f, cardHeight - IconTop - NameBottomInset - 30f);
                Place(card.Icon, new Rect((cardWidth - iconSize) * .5f, IconTop, iconSize, iconSize));
            }
        }

        private static void DrawCard(SlotView card, HotbarSlotSnapshot slot, int index, bool aiming)
        {
            bool focused = aiming ? slot.Pending : slot.Selected;
            Color stateColor = slot.Pending ? LGold : slot.CooldownRemaining > 0 ? LRed : LMuted;
            card.Frame.color = focused ? LGold : LBorder;
            card.Fill.color = focused ? LSelected : slot.Occupied ? LPanel : LBackground;
            card.Rail.gameObject.SetActive(focused);
            card.Key.color = focused ? LGold : LMuted;
            card.Key.text = (slot.Hotkey == '\0' ? HotbarStateBuilder.SlotToHotkey(index) : slot.Hotkey).ToString();
            card.State.color = stateColor;
            card.State.text = slot.Occupied ? CardState(slot) : string.Empty;
            card.State.fontSize = 12;
            if (card.State.preferredWidth > card.State.rectTransform.sizeDelta.x) card.State.fontSize = 10;

            card.Icon.sprite = slot.Occupied ? HudSkillIconAtlas.GetTile(slot.Icon).sprite : null;
            card.Icon.color = slot.Usable || slot.Pending ? Color.white : new Color(1f, 1f, 1f, .55f);
            card.Icon.gameObject.SetActive(slot.Occupied);
            card.Name.color = slot.Occupied && (slot.Usable || slot.Pending) ? LInk : LMuted;
            card.Name.fontStyle = focused ? FontStyle.Bold : FontStyle.Normal;
            card.Name.text = slot.Occupied ? slot.DisplayName : "Empty";

            Vector2 cardSize = card.Frame.rectTransform.sizeDelta;
            float nameTop = slot.Occupied ? IconTop + card.Icon.rectTransform.sizeDelta.y : 33f;
            Place(card.Name, new Rect(6f, nameTop, cardSize.x - 12f, cardSize.y - nameTop - NameBottomInset));
        }

        private static string CardState(HotbarSlotSnapshot slot)
        {
            if (slot.Pending) return "Aiming";
            if (slot.CooldownRemaining <= 0) return "Ready";
            return slot.CooldownRemaining > 999 ? "CD 999+" : $"CD {slot.CooldownRemaining}";
        }

        private static string Controls(HotbarSnapshot snapshot, bool controller)
        {
            if (snapshot == null) return string.Empty;
            if (controller)
                return snapshot.PendingSlot >= 0 ? "Left stick  Aim   ·   A / RT  Confirm   ·   B  Cancel"
                    : "D-pad ← / →  Cycle   ·   X  Cast";
            if (snapshot.HintText != KeyboardCastHint) return snapshot.HintText;
            return snapshot.PendingSlot >= 0 ? "Direction keys  Aim   ·   Esc  Cancel" : "[ / ]  Cycle   ·   Enter  Cast";
        }

        private static void SetFittedText(Text label, string text)
        {
            label.text = text;
            float width = label.rectTransform.sizeDelta.x;
            if (string.IsNullOrEmpty(text) || label.preferredWidth <= width) return;
            int low = 0, high = text.Length;
            while (low < high)
            {
                int length = (low + high + 1) / 2;
                label.text = text.Substring(0, length) + "…";
                if (label.preferredWidth <= width) low = length;
                else high = length - 1;
            }
            if (low > 0 && char.IsHighSurrogate(text[low - 1])) low--;
            label.text = text.Substring(0, low).TrimEnd() + "…";
        }

        private Text Label(string name, Transform parent, int size, Color color, TextAnchor alignment,
            FontStyle style = FontStyle.Normal)
        {
            var text = Graphic<Text>(name, parent);
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image Box(string name, Transform parent, Color color)
        {
            var box = Graphic<Image>(name, parent);
            box.color = color;
            return box;
        }

        private static T Graphic<T>(string name, Transform parent) where T : Graphic
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(T)) { layer = GameplayRenderLayers.HotbarLayer };
            child.transform.SetParent(parent, false);
            var graphic = child.GetComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private static void Place(Graphic graphic, Rect bounds)
        {
            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
        }

        private readonly struct SlotView
        {
            internal readonly Image Frame, Fill, Rail, KeyBox, Icon;
            internal readonly Text Key, State, Name;

            internal SlotView(Image frame, Image fill, Image rail, Image keyBox, Text key, Text state, Image icon, Text name)
            {
                Frame = frame;
                Fill = fill;
                Rail = rail;
                KeyBox = keyBox;
                Key = key;
                State = state;
                Icon = icon;
                Name = name;
            }
        }
    }
}
