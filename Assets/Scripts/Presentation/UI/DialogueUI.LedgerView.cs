using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.UI;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class DialogueUI
    {
        private const int LedgerVisibleChoiceLimit = 10;
        private const int LedgerMaxHeight = 736;
        private const int LedgerWidth = 976;
        private const int LedgerInset = 28;
        private const int LedgerInnerWidth = LedgerWidth - 2 * LedgerInset;
        private const int LedgerSpeechY = 109;
        private const int LedgerSpeechLineHeight = 27;
        private const int LedgerPagingHeight = 34;
        private const int LedgerResponsesGap = 13;
        private const int LedgerResponsesHeadingHeight = 28;
        private const int LedgerFooterGap = 12;
        private const int LedgerFooterHeight = 46;
        private const int LedgerFixedHeight = LedgerSpeechY + LedgerPagingHeight + LedgerResponsesGap
            + LedgerResponsesHeadingHeight + LedgerFooterGap + LedgerFooterHeight;
        private const int LedgerSpeechSize = 23;
        private const int LedgerChoiceSize = 19;
        private const int LedgerChoiceLineHeight = 21;
        private const int LedgerChoiceGap = 4;
        private const int LedgerChoiceTextInset = 51;
        private const int LedgerChoiceTagWidth = 100;
        private const float LedgerViewportMargin = 12f;

        private static readonly Color LedgerBackdrop = new Color(.01f, .025f, .024f, .62f);
        private static readonly Color LedgerShadow = new Color(0f, 0f, 0f, .35f);
        private static readonly Vector2 LedgerShadowOffset = new Vector2(7, 9);
        internal Camera GameplayCamera;
        private readonly List<Image> _ledgerShapes = new List<Image>();
        private readonly List<Text> _ledgerLabels = new List<Text>();
        private readonly List<Text> _ledgerSpeechLabels = new List<Text>();
        private readonly List<LedgerChoice> _ledgerChoices = new List<LedgerChoice>();
        private readonly List<Rect> _ledgerChoiceBounds = new List<Rect>();
        private readonly List<string> _ledgerWrapBuffer = new List<string>();
        private TextGenerator _ledgerMeasure;
        private GameObject _ledgerRoot;
        private RectTransform _ledgerContent;
        private RawImage _ledgerPortraitImage;
        private Font _ledgerFont;
        private Rect _ledgerChoiceArea, _ledgerPreviousPage, _ledgerNextPage;
        private int _ledgerShapeCount, _ledgerLabelCount, _ledgerChoiceBudget;
        private int _ledgerVisibleChoices;
        private int _ledgerDrawnPage, _ledgerDrawnCursor, _ledgerDrawnChoiceOffset;
        private bool _ledgerDrawnRevealing, _ledgerLayoutDirty = true;

        private int LedgerTextCapacity => Mathf.Clamp(
            (LedgerMaxHeight - LedgerFixedHeight - _ledgerChoiceBudget) / LedgerSpeechLineHeight, 1, 12);

        private readonly struct LedgerChoice
        {
            public readonly string FirstLine, SecondLine, Tag;
            public readonly Color TextColor, TagColor;
            public LedgerChoice(string firstLine, string secondLine, Color textColor, string tag, Color tagColor)
            {
                FirstLine = firstLine;
                SecondLine = secondLine;
                TextColor = textColor;
                Tag = tag;
                TagColor = tagColor;
            }

            public int Height => SecondLine == null ? 34 : 50;
            public int TextWidth => ChoiceTextWidth(Tag);
        }

        private static int ChoiceTextWidth(string tag) => LedgerInnerWidth - LedgerChoiceTextInset
            - (tag.Length == 0 ? 14 : LedgerChoiceTagWidth);

        private void WrapLedgerSpeech(string speech)
        {
            _ledgerLayoutDirty = true;
            MeasureLedgerChoices();
            WrapLedgerLines(speech, LedgerInnerWidth, LedgerSpeechSize, _wrappedTextLines);
        }

        private void MeasureLedgerChoices()
        {
            _ledgerChoices.Clear();
            foreach (var choice in ConversationManager.VisibleChoices)
            {
                InferChoiceTag(choice, out string tag, out _);
                tag = tag == "[Talk]" ? string.Empty : tag.Trim('[', ']');
                Color tagColor = tag switch
                {
                    "Attack" => LRed,
                    "Trade" => LGreen,
                    "Leave" => LMuted,
                    _ => LGold
                };
                string text = choice?.Text;
                Color textColor = LInk;
                if ((tag == "Attack" && text == "[Attack]") || (tag == "Trade" && text == "[Let's trade.]"))
                {
                    text = tag;
                    textColor = tagColor;
                    tag = string.Empty;
                }
                int width = ChoiceTextWidth(tag);
                _ledgerWrapBuffer.Clear();
                WrapLedgerLines(text, width, LedgerChoiceSize, _ledgerWrapBuffer);
                string secondLine = _ledgerWrapBuffer.Count > 1 ? _ledgerWrapBuffer[1] : null;
                if (_ledgerWrapBuffer.Count > 2)
                    secondLine = FitLedgerText(secondLine + "…", width, LedgerChoiceSize, FontStyle.Normal);
                var row = new LedgerChoice(_ledgerWrapBuffer[0], secondLine, textColor, tag, tagColor);
                _ledgerChoices.Add(row);
            }

            _ledgerVisibleChoices = Mathf.Min(LedgerVisibleChoiceLimit, _ledgerChoices.Count);
            int replyHeightLimit = LedgerMaxHeight - LedgerFixedHeight - 3 * LedgerSpeechLineHeight;
            _ledgerChoiceBudget = MeasureLedgerReplyWindow(_ledgerVisibleChoices);
            while (_ledgerVisibleChoices > 1 && _ledgerChoiceBudget > replyHeightLimit)
            {
                _ledgerVisibleChoices--;
                _ledgerChoiceBudget = MeasureLedgerReplyWindow(_ledgerVisibleChoices);
            }
        }

        // Reserve the tallest scroll window so speech paging stays put when replies scroll.
        private int MeasureLedgerReplyWindow(int count)
        {
            int window = 0, tallest = 0;
            for (int i = 0; i < _ledgerChoices.Count; i++)
            {
                window += _ledgerChoices[i].Height + LedgerChoiceGap;
                if (i >= count) window -= _ledgerChoices[i - count].Height + LedgerChoiceGap;
                tallest = Mathf.Max(tallest, window);
            }
            return tallest;
        }

        private void WrapLedgerLines(string text, float width, int size, List<string> lines)
        {
            foreach (string raw in (text ?? string.Empty).Split('\n'))
            {
                string paragraph = raw.TrimEnd('\r');
                if (paragraph.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                for (int start = 0; start < paragraph.Length;)
                {
                    int length = LedgerPrefixLength(paragraph, start, width, size, FontStyle.Normal, string.Empty);
                    int end = start + Mathf.Max(1, length);
                    if (end < paragraph.Length)
                    {
                        int lastSpace = paragraph.LastIndexOf(' ', end, end - start + 1);
                        if (lastSpace > start) end = lastSpace;
                    }
                    lines.Add(paragraph.Substring(start, end - start));
                    start = end;
                    while (start < paragraph.Length && paragraph[start] == ' ') start++;
                }
            }
        }

        private string FitLedgerText(string value, float width, int size, FontStyle style)
        {
            if (MeasureLedgerText(value, size, style) <= width) return value;
            int length = LedgerPrefixLength(value, 0, width, size, style, "…");
            return value.Substring(0, length).TrimEnd() + "…";
        }

        private int LedgerPrefixLength(string value, int start, float width, int size, FontStyle style, string suffix)
        {
            int lower = 0, upper = value.Length - start;
            while (lower < upper)
            {
                int length = (lower + upper + 1) / 2;
                int end = start + length;
                int sampleLength = end < value.Length && char.IsHighSurrogate(value[end - 1]) ? length - 1 : length;
                if (MeasureLedgerText(value.Substring(start, sampleLength) + suffix, size, style) <= width) lower = length;
                else upper = length - 1;
            }
            if (lower > 0 && start + lower < value.Length && char.IsHighSurrogate(value[start + lower - 1])) lower--;
            return lower;
        }

        private float MeasureLedgerText(string value, int size, FontStyle style)
        {
            if (_ledgerFont == null) _ledgerFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _ledgerMeasure ??= new TextGenerator();
            return _ledgerMeasure.GetPreferredWidth(value, new TextGenerationSettings
            {
                font = _ledgerFont,
                fontSize = size,
                fontStyle = style,
                color = Color.white,
                lineSpacing = 1f,
                scaleFactor = 1f,
                horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow
            });
        }

        private void EnsureLedgerView()
        {
            if (_ledgerRoot != null) return;
            if (_ledgerFont == null) _ledgerFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _ledgerRoot = new GameObject("ConversationLedger", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _ledgerRoot.transform.SetParent(transform, false);
            var canvas = _ledgerRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _ledgerRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var backdrop = LedgerGraphic<Image>("DialogueBackdrop", _ledgerRoot.transform);
            backdrop.color = LedgerBackdrop;
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
            _ledgerContent = new GameObject("DialogueCard", typeof(RectTransform)).GetComponent<RectTransform>();
            _ledgerContent.SetParent(_ledgerRoot.transform, false);
            _ledgerContent.anchorMin = _ledgerContent.anchorMax = new Vector2(.5f, .5f);
            _ledgerPortraitImage = LedgerGraphic<RawImage>("DialoguePortrait", _ledgerContent);
            _ledgerPortraitImage.gameObject.SetActive(false);
        }

        private void HideLedgerView()
        {
            if (_ledgerRoot == null) return;
            _ledgerRoot.SetActive(false);
            _ledgerPortraitImage.texture = null;
            _ledgerPortraitImage.gameObject.SetActive(false);
            _ledgerChoiceBounds.Clear();
            _ledgerLayoutDirty = true;
        }

        private void RefreshLedgerPortrait()
        {
            if (_ledgerPortraitImage == null || !_ledgerPortraitImage.gameObject.activeSelf) return;
            ValidatePortrait();
            if (_portrait.Texture != null) return;
            _portrait = default;
            RenderLedgerView();
        }

        private void LateUpdate()
        {
            if (_isOpen && UseLedgerView && _ledgerContent != null) PositionLedgerCard();
        }

        private void PositionLedgerCard()
        {
            // The gameplay camera already accounts for the sidebar, hotbar and letterbox.
            // Its viewport is normalized in the same space as the fullscreen canvas anchors.
            Camera camera = GameplayCamera != null ? GameplayCamera : PopupCamera;
            Rect viewport = camera != null ? camera.rect : new Rect(0, 0, 1, 1);
            if (viewport.width <= 0f || viewport.height <= 0f) viewport = new Rect(0, 0, 1, 1);
            Vector2 center = viewport.center;
            if (_ledgerContent.anchorMin != center || _ledgerContent.anchorMax != center)
                _ledgerContent.anchorMin = _ledgerContent.anchorMax = center;

            Vector2 available = Vector2.Scale(((RectTransform)_ledgerRoot.transform).rect.size, viewport.size);
            Vector2 needed = _ledgerContent.rect.size + 2f * LedgerShadowOffset;
            float widthFit = Mathf.Max(1f, available.x - 2f * LedgerViewportMargin) / needed.x;
            float heightFit = Mathf.Max(1f, available.y - 2f * LedgerViewportMargin) / needed.y;
            float fit = Mathf.Clamp01(Mathf.Min(widthFit, heightFit));
            var scale = new Vector3(fit, fit, 1f);
            if (_ledgerContent.localScale != scale) _ledgerContent.localScale = scale;
        }

        private void RenderLedgerView()
        {
            EnsureLedgerView();
            ValidatePortrait();
            if (_ledgerLayoutDirty || _ledgerDrawnPage != _textPage || _ledgerDrawnCursor != _cursorIndex
                || _ledgerDrawnChoiceOffset != _choiceScrollOffset || _ledgerDrawnRevealing != _revealing
                || !ReferenceEquals(_ledgerPortraitImage.texture, _portrait.Texture))
            {
                _ledgerRoot.SetActive(true);
                DrawLedgerCard();
                _ledgerDrawnPage = _textPage;
                _ledgerDrawnCursor = _cursorIndex;
                _ledgerDrawnChoiceOffset = _choiceScrollOffset;
                _ledgerDrawnRevealing = _revealing;
                _ledgerLayoutDirty = false;
            }
            UpdateLedgerSpeech();
        }

        private void DrawLedgerCard()
        {
            _ledgerShapeCount = _ledgerLabelCount = 0;
            _ledgerSpeechLabels.Clear();
            _ledgerChoiceBounds.Clear();
            _ledgerChoiceArea = _ledgerPreviousPage = _ledgerNextPage = Rect.zero;

            bool paged = TextPageCount > 1;
            int speechSlots = Mathf.Clamp(_wrappedTextLines.Count, 1, LedgerTextCapacity);
            int pageY = LedgerSpeechY + speechSlots * LedgerSpeechLineHeight;
            int responsesY = pageY + (paged ? LedgerPagingHeight : 0) + LedgerResponsesGap;
            int choicesY = responsesY + (VisibleChoiceCount > 0 ? LedgerResponsesHeadingHeight : 0);
            int footerY = choicesY + _ledgerChoiceBudget + LedgerFooterGap;
            int height = footerY + LedgerFooterHeight;
            _ledgerContent.sizeDelta = new Vector2(LedgerWidth, height);

            LedgerBox(new Rect(LedgerShadowOffset, _ledgerContent.sizeDelta), LedgerShadow);
            LedgerFrame(new Rect(0, 0, LedgerWidth, height), LBorder, LBackground);
            DrawLedgerHeader();

            for (int i = 0; i < VisibleTextLineCount; i++)
            {
                _ledgerSpeechLabels.Add(LedgerLabel(new Rect(LedgerInset, LedgerSpeechY + i * LedgerSpeechLineHeight,
                    LedgerInnerWidth, LedgerSpeechLineHeight), string.Empty, LInk, LedgerSpeechSize));
            }
            if (paged) DrawLedgerPages(pageY + 4);
            if (VisibleChoiceCount > 0) DrawLedgerChoices(responsesY, choicesY);
            DrawLedgerControls(footerY);

            for (int i = _ledgerShapeCount; i < _ledgerShapes.Count; i++) _ledgerShapes[i].gameObject.SetActive(false);
            for (int i = _ledgerLabelCount; i < _ledgerLabels.Count; i++) _ledgerLabels[i].gameObject.SetActive(false);
            PositionLedgerCard();
        }

        private void DrawLedgerHeader()
        {
            LedgerBox(new Rect(1, 1, LedgerWidth - 2, 92), LPanel);
            LedgerBox(new Rect(1, 93, LedgerWidth - 2, 1), LBorder);
            float nameX = LedgerInset;
            var slot = new Rect(LedgerInset, 13, 72, 67);
            Rect portrait = AnnouncementUI.MeasurePortraitRect(new Rect(slot.x + 5, slot.y + 4, 62, 59), _portrait);
            _ledgerPortraitImage.texture = _portrait.Texture;
            _ledgerPortraitImage.gameObject.SetActive(portrait.width > 0);
            if (portrait.width > 0)
            {
                LedgerFrame(slot, LBorder, LBackground);
                _ledgerPortraitImage.uvRect = _portrait.Uv;
                _ledgerPortraitImage.color = _portrait.Tint;
                PlaceLedgerGraphic(_ledgerPortraitImage, portrait);
                nameX = slot.xMax + 16;
            }

            var speaker = ConversationManager.Speaker;
            string name = speaker?.GetDisplayName();
            float nameWidth = LedgerWidth - LedgerInset - nameX;
            LedgerLabel(new Rect(nameX, 9, nameWidth, 49), string.IsNullOrEmpty(name) ? "Conversation" : name,
                LGold, 37, FontStyle.Bold, fit: true);
            if (speaker == null) return;

            string factionId = FactionManager.GetFaction(speaker);
            string faction = string.IsNullOrEmpty(factionId) ? null : FactionManager.GetDisplayName(factionId);
            int feeling = PlayerEntity == null ? 0 : FactionManager.GetFeeling(speaker, PlayerEntity);
            DispositionInfo(feeling, out _, out _, out string disposition);
            Color dispositionColor = disposition switch
            {
                "allied" => LGreen,
                "warm" => LGold,
                "hostile" => LRed,
                _ => LMuted
            };
            if (!string.IsNullOrWhiteSpace(faction))
            {
                string factionLabel = FitLedgerText(faction + "   ·", nameWidth - 104, 17, FontStyle.Normal);
                float factionWidth = Mathf.Ceil(MeasureLedgerText(factionLabel, 17, FontStyle.Normal)) + 10;
                LedgerLabel(new Rect(nameX, 57, factionWidth, 23), factionLabel, LMuted, 17);
                nameX += factionWidth;
            }
            string feelingLabel = char.ToUpperInvariant(disposition[0]) + disposition.Substring(1);
            LedgerLabel(new Rect(nameX, 57, 90, 23), feelingLabel, dispositionColor, 17);
        }

        private void UpdateLedgerSpeech()
        {
            int remaining = _revealing ? _revealCount : int.MaxValue;
            for (int i = 0; i < _ledgerSpeechLabels.Count; i++)
            {
                string line = _wrappedTextLines[TextStart + i];
                int count = Mathf.Min(remaining, line.Length);
                remaining -= count;
                if (count > 0 && count < line.Length && char.IsHighSurrogate(line[count - 1])) count--;
                var label = _ledgerSpeechLabels[i];
                if (label.text.Length != count) label.text = count == line.Length ? line : line.Substring(0, count);
            }
        }

        private void DrawLedgerPages(int y)
        {
            bool previous = _textPage > 0;
            bool next = _textPage < TextPageCount - 1;
            Rect previousBounds = new Rect(LedgerWidth - LedgerInset - 297, y, 100, 27);
            Rect nextBounds = new Rect(LedgerWidth - LedgerInset - 100, y, 100, 27);
            DrawLedgerPageButton(previousBounds, "‹  Previous", previous);
            DrawLedgerPageButton(nextBounds, "Next  ›", next);
            _ledgerPreviousPage = previousBounds;
            _ledgerNextPage = nextBounds;
            LedgerLabel(new Rect(previousBounds.xMax, y, nextBounds.x - previousBounds.xMax, 27),
                $"{_textPage + 1} / {TextPageCount}", LMuted, 15, align: TextAnchor.MiddleCenter);
            LedgerLabel(new Rect(LedgerInset, y, 265, 27), "← / → or LB / RB   Change page", LMuted, 15);
        }

        private void DrawLedgerPageButton(Rect bounds, string label, bool enabled)
        {
            LedgerFrame(bounds, LBorder, enabled ? LSelected : LBackground);
            LedgerLabel(bounds, label, enabled ? LInk : LMuted, 16, align: TextAnchor.MiddleCenter);
        }

        private void DrawLedgerChoices(int headingY, int choiceY)
        {
            LedgerBox(new Rect(LedgerInset, headingY, LedgerInnerWidth, 1), LBorder);
            LedgerLabel(new Rect(LedgerInset, headingY + 1, 275, 25), "Responses", LMuted, 16, FontStyle.Bold);
            int end = _choiceScrollOffset + VisibleChoiceCount;
            if (_ledgerChoices.Count > VisibleChoiceCount)
            {
                LedgerLabel(new Rect(LedgerWidth - LedgerInset - 225, headingY + 1, 225, 25),
                    $"{_choiceScrollOffset + 1}–{end} of {_ledgerChoices.Count}", LMuted, 16,
                    align: TextAnchor.MiddleRight);
            }
            for (int index = _choiceScrollOffset; index < end; index++)
            {
                var row = _ledgerChoices[index];
                var bounds = new Rect(LedgerInset, choiceY, LedgerInnerWidth, row.Height);
                DrawLedgerChoice(bounds, row, index);
                _ledgerChoiceBounds.Add(bounds);
                choiceY += row.Height + LedgerChoiceGap;
            }
            _ledgerChoiceArea = new Rect(LedgerInset, headingY, LedgerInnerWidth, choiceY - LedgerChoiceGap - headingY);
        }

        private void DrawLedgerChoice(Rect bounds, LedgerChoice row, int index)
        {
            bool selected = index == _cursorIndex;
            LedgerBox(bounds, selected ? LSelected : LPanel, $"DialogueChoiceRow {index}");
            if (selected) LedgerBox(new Rect(bounds.x, bounds.y, 3, bounds.height), LGold);
            char shortcut = MenuShortcutMap.Positional(index);
            if (shortcut != '\0')
            {
                var key = new Rect(bounds.x + 12, bounds.center.y - 11, 23, 22);
                LedgerFrame(key, selected ? LGold : LBorder, LBackground);
                LedgerLabel(key, shortcut.ToString(), selected ? LGold : LMuted, 16, align: TextAnchor.MiddleCenter);
            }

            int lines = row.SecondLine == null ? 1 : 2;
            float textY = bounds.center.y - lines * LedgerChoiceLineHeight * .5f;
            var text = new Rect(bounds.x + LedgerChoiceTextInset, textY, row.TextWidth, LedgerChoiceLineHeight);
            LedgerLabel(text, row.FirstLine, row.TextColor, LedgerChoiceSize);
            if (lines > 1)
            {
                text.y += LedgerChoiceLineHeight;
                LedgerLabel(text, row.SecondLine, row.TextColor, LedgerChoiceSize);
            }
            if (row.Tag.Length > 0)
                LedgerLabel(new Rect(bounds.xMax - LedgerChoiceTagWidth, bounds.y, LedgerChoiceTagWidth - 15, bounds.height),
                    row.Tag, row.TagColor, 15, FontStyle.Bold, TextAnchor.MiddleRight);
        }

        private void DrawLedgerControls(int y)
        {
            LedgerBox(new Rect(1, y + 1, LedgerWidth - 2, LedgerFooterHeight - 2), LPanel);
            LedgerBox(new Rect(1, y, LedgerWidth - 2, 1), LBorder);
            if (VisibleChoiceCount > 0)
                LedgerLabel(new Rect(LedgerInset, y, 285, LedgerFooterHeight), "↑ / ↓ or D-pad   Navigate", LMuted, 16);
            if (_revealing || VisibleChoiceCount > 0)
                LedgerLabel(new Rect(328, y, 320, LedgerFooterHeight),
                    _revealing ? "Enter / A   Finish text" : "Enter / A   Select response", LInk, 16, align: TextAnchor.MiddleCenter);
            if (ConversationManager.CurrentNode?.AllowEscape != false)
                LedgerLabel(new Rect(LedgerWidth - LedgerInset - 230, y, 230, LedgerFooterHeight),
                    "Esc / B   Close", LMuted, 16, align: TextAnchor.MiddleRight);
        }

        private int GetLedgerChoiceRowAtScreen(Vector2 screen)
        {
            if (!TryLedgerPoint(screen, out var point)) return -1;
            for (int i = 0; i < _ledgerChoiceBounds.Count; i++)
                if (_ledgerChoiceBounds[i].Contains(point)) return _ledgerDrawnChoiceOffset + i;
            return -1;
        }

        private bool IsLedgerChoiceAreaAtScreen(Vector2 screen)
            => TryLedgerPoint(screen, out var point) && _ledgerChoiceArea.Contains(point);

        private bool TryLedgerTextPageAtScreen(Vector2 screen, out int delta)
        {
            delta = 0;
            if (!TryLedgerPoint(screen, out var point)) return false;
            if (_ledgerPreviousPage.Contains(point)) delta = -1;
            if (_ledgerNextPage.Contains(point)) delta = 1;
            return delta != 0;
        }

        private bool TryLedgerPoint(Vector2 screen, out Vector2 point)
        {
            point = default;
            if (_ledgerContent == null || !_ledgerRoot.activeInHierarchy
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_ledgerContent, screen, null, out var local))
                return false;
            Rect bounds = _ledgerContent.rect;
            point = new Vector2(local.x - bounds.xMin, bounds.yMax - local.y);
            return bounds.Contains(local);
        }

        private static T LedgerGraphic<T>(string name, Transform parent) where T : Graphic
        {
            var graphic = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            graphic.transform.SetParent(parent, false);
            graphic.raycastTarget = false;
            return graphic;
        }

        private static void PlaceLedgerGraphic(Graphic graphic, Rect bounds)
        {
            graphic.gameObject.SetActive(true);
            graphic.transform.SetAsLastSibling();
            var rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
        }

        private void LedgerBox(Rect bounds, Color color, string name = "DialogueShape")
        {
            if (_ledgerShapeCount == _ledgerShapes.Count)
                _ledgerShapes.Add(LedgerGraphic<Image>(name, _ledgerContent));
            var box = _ledgerShapes[_ledgerShapeCount++];
            if (box.gameObject.name != name) box.gameObject.name = name;
            PlaceLedgerGraphic(box, bounds);
            box.color = color;
        }

        private void LedgerFrame(Rect bounds, Color border, Color fill)
        {
            LedgerBox(bounds, border);
            LedgerBox(new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, bounds.height - 2), fill);
        }

        private Text LedgerLabel(Rect bounds, string value, Color color, int size, FontStyle style = FontStyle.Normal,
            TextAnchor align = TextAnchor.MiddleLeft, bool fit = false)
        {
            if (_ledgerLabelCount == _ledgerLabels.Count)
            {
                var label = LedgerGraphic<Text>("DialogueText", _ledgerContent);
                label.font = _ledgerFont;
                label.supportRichText = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                _ledgerLabels.Add(label);
            }
            var result = _ledgerLabels[_ledgerLabelCount++];
            PlaceLedgerGraphic(result, bounds);
            result.fontSize = size;
            result.fontStyle = style;
            result.alignment = align;
            result.color = color;
            result.text = fit ? FitLedgerText(value, bounds.width, size, style) : value;
            return result;
        }
    }
}
