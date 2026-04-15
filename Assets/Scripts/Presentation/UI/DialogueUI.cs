using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a conversation popup overlaid on the game world.
    /// Follows the same world-space popup pattern as PickupUI.
    /// Shows NPC speech text (word-wrapped) and numbered player choices.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private const int POPUP_MAX_VISIBLE_CHOICES = 10;
        private const int POPUP_MIN_W = 32;
        private const int POPUP_MAX_W = 72;

        // Portrait panel: left gutter showing the speaker's glyph, faction,
        // and disposition word. The divider sits at col (PORTRAIT_W - 1), so
        // glyph/text content lives in cols 1..PORTRAIT_W-2 (8 cells wide).
        public bool UsePortraitPanel = true;
        private const int PORTRAIT_W = 10;

        // Opaque dark fill color for popup background (drawn on BgTilemap behind main glyphs)
        private static readonly Color PopupBgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        private bool _isOpen;
        private int _cursorIndex;
        private List<string> _wrappedTextLines = new List<string>();
        private bool _conversationEnded;

        // Typewriter reveal state.
        private const float REVEAL_INTERVAL = 0.015f;
        private bool _revealing;
        private int _revealCount;       // cumulative characters revealed across all wrapped text lines
        private int _totalRevealChars;  // total chars in _wrappedTextLines (sum of lengths)
        private float _revealTimer;

        // Popup overlay-grid anchors (same top-left convention as PickupUI).
        // _popupW is computed per-render so the box shrinks/grows with content.
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private int _popupW;
        private int _maxTextWidth;
        private bool _bgDrawn;
        // Cached at DrawBgFill time so ClearBgRegion clears the rectangle we
        // ACTUALLY drew (popup width/origin may change between renders when
        // the dynamic-width popup shrinks or grows).
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        public bool IsOpen => _isOpen;
        public bool ConversationEnded => _conversationEnded;

        public void Open()
        {
            if (!ConversationManager.IsActive) return;
            _isOpen = true;
            _cursorIndex = 0;
            _conversationEnded = false;
            RebuildText();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            // Clear foreground glyphs (borders, text, choices, action bar)
            ClearRegion(0, 0, _popupW, _popupH);
            ClearBgRegion();
            if (ConversationManager.IsActive)
                ConversationManager.EndConversation();
        }

        /// <summary>
        /// Unity-driven per-frame tick. Advances the typewriter reveal while
        /// the dialog is open. Independent of HandleInput so the stream
        /// doesn't stall between input events.
        /// </summary>
        private void Update()
        {
            if (!_isOpen || !_revealing) return;
            _revealTimer += Time.deltaTime;
            bool rendered = false;
            while (_revealTimer >= REVEAL_INTERVAL && _revealCount < _totalRevealChars)
            {
                _revealTimer -= REVEAL_INTERVAL;
                _revealCount++;
                rendered = true;
            }
            if (_revealCount >= _totalRevealChars)
            {
                _revealing = false;
                _revealCount = _totalRevealChars;
                rendered = true;
            }
            if (rendered) Render();
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            // Mouse hover
            {
                int hoverRow = GetChoiceRowAtMouse();
                if (hoverRow >= 0 && hoverRow != _cursorIndex)
                {
                    _cursorIndex = hoverRow;
                    Render();
                }
            }

            var choices = ConversationManager.VisibleChoices;

            // Escape to close (if allowed)
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                if (ConversationManager.CurrentNode == null ||
                    ConversationManager.CurrentNode.AllowEscape)
                {
                    _conversationEnded = true;
                    Close();
                    return;
                }
            }

            // While text is still streaming, ANY confirm input (Enter/Space/click/hotkey)
            // fast-forwards the reveal instead of selecting a choice. The next press
            // then performs the normal action.
            if (_revealing)
            {
                bool skip = InputHelper.GetKeyDown(KeyCode.Return)
                         || InputHelper.GetKeyDown(KeyCode.Space)
                         || Input.GetMouseButtonDown(0);
                if (!skip)
                {
                    for (int i = 0; i < 26 && i < choices.Count; i++)
                    {
                        if (InputHelper.GetKeyDown(KeyCode.A + i)) { skip = true; break; }
                    }
                }
                if (skip)
                {
                    _revealCount = _totalRevealChars;
                    _revealing = false;
                    Render();
                    return;
                }
                // Arrow keys are allowed to pre-position the cursor; fall through.
            }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetChoiceRowAtMouse();
                if (clickedRow >= 0 && clickedRow < choices.Count)
                {
                    SelectChoice(clickedRow);
                    return;
                }
            }

            // Up/Down navigation
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                    _cursorIndex--;
                Render();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < choices.Count - 1)
                    _cursorIndex++;
                Render();
                return;
            }

            // Enter to select
            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space))
            {
                if (choices.Count > 0)
                    SelectChoice(_cursorIndex);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < choices.Count; i++)
            {
                if (InputHelper.GetKeyDown(KeyCode.A + i))
                {
                    SelectChoice(i);
                    return;
                }
            }
        }

        private void SelectChoice(int index)
        {
            bool continues = ConversationManager.SelectChoice(index);
            if (!continues)
            {
                _conversationEnded = true;
                Close();
                return;
            }

            _cursorIndex = 0;
            RebuildText();
            Render();
        }

        private void RebuildText()
        {
            _wrappedTextLines.Clear();
            if (ConversationManager.CurrentNode == null)
            {
                _popupW = POPUP_MIN_W;
                _maxTextWidth = _popupW - 4;
                return;
            }

            string text = ConversationManager.CurrentNode.Text ?? "";

            // ----- Compute desired popup width -----
            // Layout budget per row:
            //   border(1) + padding(1) + content + padding(1) + border(1) = content + 4
            //
            // Text rows: scan pre-wrap paragraphs for the longest single line.
            int longestTextLine = 0;
            if (!string.IsNullOrEmpty(text))
            {
                string[] paragraphs = text.Split('\n');
                for (int p = 0; p < paragraphs.Length; p++)
                    if (paragraphs[p].Length > longestTextLine)
                        longestTextLine = paragraphs[p].Length;
            }

            // Choice rows: border(1) + cursor(1) + hotkey(2) + space(1) + tag(up to 8) +
            //   space(1) + choiceText + padding(1) + border(1) = choiceText + tagLen + 9
            int longestChoiceRow = 0;
            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);
            for (int i = 0; i < choiceCount; i++)
            {
                InferChoiceTag(choices[i], out string tag, out Color _);
                int tagLen = string.IsNullOrEmpty(tag) ? 0 : tag.Length + 1; // tag + trailing space
                int txt = choices[i].Text != null ? choices[i].Text.Length : 0;
                int row = txt + tagLen + 9; // cursor + hotkey + spacing + borders
                if (row > longestChoiceRow) longestChoiceRow = row;
            }

            // Title: border(1) + horizontal bars(2) + "[ g NAME ]" + horizontal bars(2) + border(1)
            string speakerName = ConversationManager.Speaker != null
                ? (ConversationManager.Speaker.GetDisplayName() ?? "")
                : "";
            // Must match the Render formula: maxNameLen = _popupW - contentOffsetX - 14.
            // So to avoid truncation: _popupW >= 14 + contentOffsetX + nameLen.
            // portraitOverhead is added below, so titleMinW just needs 14 + nameLen.
            int titleMinW = 14 + speakerName.Length;

            int needed = Mathf.Max(titleMinW, longestTextLine + 4, longestChoiceRow);
            // Portrait panel eats PORTRAIT_W columns on the left, so the budget
            // for title/text/choices in the right panel needs that much more.
            int portraitOverhead = UsePortraitPanel ? PORTRAIT_W : 0;
            _popupW = Mathf.Clamp(needed + portraitOverhead, POPUP_MIN_W, POPUP_MAX_W);
            _maxTextWidth = _popupW - 4 - portraitOverhead;
            if (_maxTextWidth < 1) _maxTextWidth = 1;

            WrapText(text, _maxTextWidth, _wrappedTextLines);

            // Reset typewriter reveal for the new node.
            _totalRevealChars = 0;
            for (int i = 0; i < _wrappedTextLines.Count; i++)
                _totalRevealChars += _wrappedTextLines[i].Length;
            _revealCount = 0;
            _revealTimer = 0f;
            _revealing = _totalRevealChars > 0;
        }

        /// <summary>
        /// Word-wrap text into lines of maxWidth characters.
        /// </summary>
        private static void WrapText(string text, int maxWidth, List<string> lines)
        {
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return;
            }

            // Split on explicit newlines first
            string[] paragraphs = text.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p];
                if (para.Length <= maxWidth)
                {
                    lines.Add(para);
                    continue;
                }

                int pos = 0;
                while (pos < para.Length)
                {
                    int remaining = para.Length - pos;
                    if (remaining <= maxWidth)
                    {
                        lines.Add(para.Substring(pos));
                        break;
                    }

                    // Find last space within maxWidth
                    int breakAt = para.LastIndexOf(' ', pos + maxWidth - 1, maxWidth);
                    if (breakAt <= pos)
                        breakAt = pos + maxWidth; // force break

                    lines.Add(para.Substring(pos, breakAt - pos));
                    pos = breakAt;
                    if (pos < para.Length && para[pos] == ' ')
                        pos++; // skip the space
                }
            }
        }

        // ===== Rendering =====

        private void ComputePopupPosition()
        {
            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);
            int textLines = _wrappedTextLines.Count;

            // Layout: top border (with embedded title) + text lines + blank + choices + bottom border + action bar
            // = 1 + textLines + 1 + choiceCount + 1 + 1
            _popupH = 1 + textLines + 1 + choiceCount + 1 + 1;

            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(_popupW);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private void Render()
        {
            using (PerformanceMarkers.Ui.DialogueRender.Auto())
            {
                PerformanceDiagnostics.RecordDialogueRender();
                if (Tilemap == null || !ConversationManager.IsActive)
                    return;

                // Clear any previous-frame bg before layout changes (popup height may change per node)
                ClearBgRegion();

                ComputePopupPosition();

                var choices = ConversationManager.VisibleChoices;
                int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);
                int textLines = _wrappedTextLines.Count;

                ClearRegion(0, 0, _popupW, _popupH);

                // Opaque bg fill behind everything except the action bar (last row)
                DrawBgFill(0, 0, _popupW, _popupH - 1);

                // ----- Top border with embedded title: ╞══[ g Speaker Name ]═════╡ -----
                var speaker = ConversationManager.Speaker;
                char speakerGlyph = '?';
                Color speakerGlyphColor = QudColorParser.White;
                string speakerName = "";
                if (speaker != null)
                {
                    var render = speaker.GetPart<RenderPart>();
                    if (render != null && !string.IsNullOrEmpty(render.RenderString))
                    {
                        speakerGlyph = render.RenderString[0];
                        speakerGlyphColor = QudColorParser.Parse(render.ColorString);
                    }

                    speakerName = speaker.GetDisplayName() ?? "";
                }

                int feeling = (speaker != null && PlayerEntity != null)
                    ? FactionManager.GetFeeling(speaker, PlayerEntity)
                    : 0;
                DispositionInfo(feeling, out Color borderColor, out Color dispoColor, out string dispoWord);

                int contentOffsetX = UsePortraitPanel ? PORTRAIT_W : 0;
                int dividerX = UsePortraitPanel ? (PORTRAIT_W - 1) : -1;

                int maxNameLen = _popupW - contentOffsetX - 14;
                if (maxNameLen < 1)
                    maxNameLen = 1;
                if (speakerName.Length > maxNameLen)
                    speakerName = speakerName.Substring(0, maxNameLen);
                int titleLen = 6 + speakerName.Length;
                int titleStart = contentOffsetX + 3;

                DrawChar(0, 0, CP437TilesetGenerator.BoxTopLeft, borderColor);
                DrawChar(_popupW - 1, 0, CP437TilesetGenerator.BoxTopRight, borderColor);
                for (int i = 1; i < titleStart; i++)
                    DrawChar(i, 0, CP437TilesetGenerator.BoxHorizontal, borderColor);
                for (int i = titleStart + titleLen; i < _popupW - 1; i++)
                    DrawChar(i, 0, CP437TilesetGenerator.BoxHorizontal, borderColor);
                if (dividerX > 0 && dividerX < titleStart)
                    DrawChar(dividerX, 0, CP437TilesetGenerator.BoxTeeDown, borderColor);

                int tx = titleStart;
                DrawChar(tx, 0, '[', QudColorParser.Gray); tx++;
                DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
                DrawChar(tx, 0, speakerGlyph, speakerGlyphColor); tx++;
                DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
                DrawText(tx, 0, speakerName, QudColorParser.BrightYellow); tx += speakerName.Length;
                DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
                DrawChar(tx, 0, ']', QudColorParser.Gray);

                int y = 1;
                int revealedSoFar = 0;
                for (int i = 0; i < textLines; i++)
                {
                    DrawChar(0, y, CP437TilesetGenerator.BoxVertical, borderColor);
                    DrawChar(_popupW - 1, y, CP437TilesetGenerator.BoxVertical, borderColor);
                    if (dividerX > 0)
                        DrawChar(dividerX, y, CP437TilesetGenerator.BoxVertical, borderColor);

                    string line = _wrappedTextLines[i];
                    int drawCount;
                    if (!_revealing)
                    {
                        drawCount = line.Length;
                    }
                    else
                    {
                        int remaining = _revealCount - revealedSoFar;
                        if (remaining <= 0)
                            drawCount = 0;
                        else if (remaining >= line.Length)
                            drawCount = line.Length;
                        else
                            drawCount = remaining;
                    }

                    if (drawCount > 0)
                        DrawText(contentOffsetX + 2, y, line.Substring(0, drawCount), QudColorParser.White);
                    revealedSoFar += line.Length;
                    y++;
                }

                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, borderColor);
                DrawChar(_popupW - 1, y, CP437TilesetGenerator.BoxVertical, borderColor);
                if (dividerX > 0)
                    DrawChar(dividerX, y, CP437TilesetGenerator.BoxVertical, borderColor);
                y++;

                for (int i = 0; i < choiceCount; i++)
                {
                    DrawChar(0, y, CP437TilesetGenerator.BoxVertical, borderColor);
                    DrawChar(_popupW - 1, y, CP437TilesetGenerator.BoxVertical, borderColor);
                    if (dividerX > 0)
                        DrawChar(dividerX, y, CP437TilesetGenerator.BoxVertical, borderColor);

                    bool selected = i == _cursorIndex;
                    if (selected && !_revealing)
                        DrawChar(contentOffsetX + 1, y, '>', QudColorParser.White);

                    int col = contentOffsetX + 2;
                    if (i < 26)
                    {
                        char hotkey = (char)('a' + i);
                        string hk = hotkey + ")";
                        Color hkColor = _revealing
                            ? QudColorParser.DarkGray
                            : (selected ? QudColorParser.White : QudColorParser.Gray);
                        DrawText(col, y, hk, hkColor);
                        col += 3;
                    }

                    InferChoiceTag(choices[i], out string tag, out Color tagColor);
                    if (!string.IsNullOrEmpty(tag))
                    {
                        DrawText(col, y, tag, _revealing ? QudColorParser.DarkGray : tagColor);
                        col += tag.Length + 1;
                    }

                    string choiceText = choices[i].Text ?? "";
                    int maxLen = _popupW - 2 - col;
                    if (maxLen < 1)
                        maxLen = 1;
                    if (choiceText.Length > maxLen)
                        choiceText = choiceText.Substring(0, maxLen - 1) + "~";
                    Color textColor = _revealing
                        ? QudColorParser.DarkGray
                        : (selected ? QudColorParser.BrightCyan : QudColorParser.Gray);
                    DrawText(col, y, choiceText, textColor);
                    y++;
                }

                DrawChar(0, y, CP437TilesetGenerator.BoxBottomLeft, borderColor);
                for (int i = 1; i < _popupW - 1; i++)
                    DrawChar(i, y, CP437TilesetGenerator.BoxHorizontal, borderColor);
                DrawChar(_popupW - 1, y, CP437TilesetGenerator.BoxBottomRight, borderColor);
                if (dividerX > 0)
                    DrawChar(dividerX, y, CP437TilesetGenerator.BoxTeeUp, borderColor);
                y++;

                if (UsePortraitPanel && dividerX > 0)
                {
                    int panelInnerWidth = dividerX - 1;
                    int interiorRows = textLines + 1 + choiceCount;
                    int gap = interiorRows >= 5 ? 2 : 1;

                    int glyphRow = 1;
                    int glyphCol = 1 + (panelInnerWidth - 1) / 2;
                    DrawChar(glyphCol, glyphRow, speakerGlyph, speakerGlyphColor);

                    string faction = speaker != null ? FactionManager.GetFaction(speaker) : null;
                    if (!string.IsNullOrEmpty(faction))
                    {
                        int maxFacLen = panelInnerWidth;
                        string f = faction.Length > maxFacLen ? faction.Substring(0, maxFacLen) : faction;
                        int facRow = glyphRow + gap;
                        int facCol = 1 + (panelInnerWidth - f.Length) / 2;
                        if (facRow <= interiorRows)
                            DrawText(facCol, facRow, f, QudColorParser.Gray);
                    }

                    string dispo = dispoWord;
                    if (!string.IsNullOrEmpty(dispo))
                    {
                        int maxDispoLen = panelInnerWidth;
                        string d = dispo.Length > maxDispoLen ? dispo.Substring(0, maxDispoLen) : dispo;
                        int dispoRow = glyphRow + gap * 2;
                        int dispoCol = 1 + (panelInnerWidth - d.Length) / 2;
                        if (dispoRow <= interiorRows)
                            DrawText(dispoCol, dispoRow, d, dispoColor);
                    }
                }

                string actions = " [Enter]select [a-z]quick [Esc]close";
                if (actions.Length > _popupW)
                    actions = actions.Substring(0, _popupW);
                DrawText(0, y, actions, QudColorParser.DarkGray);
            }
        }

        /// <summary>
        /// Derive border color, disposition color, and disposition word from a feeling value.
        /// Single source of truth — avoids comparing Color structs to infer disposition.
        /// </summary>
        private static void DispositionInfo(int feeling, out Color borderColor, out Color dispoColor, out string word)
        {
            if (feeling <= -10)      { borderColor = QudColorParser.BrightRed;    dispoColor = QudColorParser.BrightRed;    word = "hostile"; }
            else if (feeling >= 50)  { borderColor = QudColorParser.BrightGreen;  dispoColor = QudColorParser.BrightGreen;  word = "allied";  }
            else if (feeling >= 25)  { borderColor = QudColorParser.BrightYellow; dispoColor = QudColorParser.BrightYellow; word = "warm";    }
            else                     { borderColor = QudColorParser.Gray;         dispoColor = QudColorParser.Gray;         word = "neutral"; }
        }

        /// <summary>
        /// Inspect the choice's Actions and Target to produce a short semantic tag.
        /// </summary>
        private static void InferChoiceTag(ChoiceData choice, out string tag, out Color color)
        {
            if (choice != null && choice.Actions != null)
            {
                for (int i = 0; i < choice.Actions.Count; i++)
                {
                    string key = choice.Actions[i].Key;
                    if (key == "StartAttack") { tag = "[Attack]"; color = QudColorParser.BrightRed; return; }
                    if (key == "StartTrade")  { tag = "[Trade]";  color = QudColorParser.BrightGreen; return; }
                    if (key == "GiveItem")    { tag = "[Give]";   color = QudColorParser.BrightYellow; return; }
                    if (key == "TakeItem" || key == "TakeItemWithTag") { tag = "[Take]"; color = QudColorParser.BrightYellow; return; }
                    if (key == "CopyGrimoire") { tag = "[Copy]";  color = QudColorParser.BrightMagenta; return; }
                }
            }
            if (choice != null && choice.Target == "End") { tag = "[Leave]"; color = QudColorParser.DarkGray; return; }
            tag = "[Talk]";
            color = QudColorParser.DarkCyan;
        }

        /// <summary>
        /// Fills the popup rect on BgTilemap with opaque dark blocks.
        /// </summary>
        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var blockTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null) return;
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    var pos = new Vector3Int(wx, wy, 0);
                    BgTilemap.SetTile(pos, blockTile);
                    BgTilemap.SetTileFlags(pos, TileFlags.None);
                    BgTilemap.SetColor(pos, PopupBgColor);
                }
            }
            _bgDrawn = true;
            _bgDrawnW = width;
            _bgDrawnH = height;
            _bgDrawnOriginX = _worldOriginX + gx;
            _bgDrawnTopY = _worldTopY - gy;
        }

        /// <summary>
        /// Clears the bg tiles we drew behind the last-rendered popup.
        /// Uses the rectangle geometry cached at DrawBgFill time because the
        /// popup's width + origin may have changed between renders.
        /// ZoneRenderer's unexplored-fog pass will rewrite these cells as needed on redraw.
        /// </summary>
        private void ClearBgRegion()
        {
            if (!_bgDrawn || BgTilemap == null) return;
            for (int dy = 0; dy < _bgDrawnH; dy++)
            {
                for (int dx = 0; dx < _bgDrawnW; dx++)
                {
                    int wx = _bgDrawnOriginX + dx;
                    int wy = _bgDrawnTopY - dy;
                    BgTilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }
            _bgDrawn = false;
        }

        // ===== Mouse =====

        private int GetChoiceRowAtMouse()
        {
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, Input.mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            int textLines = _wrappedTextLines.Count;
            int choicesStartY = 1 + textLines + 1; // after top border + text + blank

            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);

            // Only count clicks in the right panel (not the portrait column).
            int leftEdge = UsePortraitPanel ? PORTRAIT_W : 1;
            if (gx >= leftEdge && gx < _popupW - 1
                && gy >= choicesStartY && gy < choicesStartY + choiceCount)
            {
                return gy - choicesStartY;
            }
            return -1;
        }

        // ===== Drawing Helpers (world-space, same as PickupUI) =====

        private void ClearRegion(int gx, int gy, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    Tilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }
        }

        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null) return;
            int wx = _worldOriginX + gx;
            int wy = _worldTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, UnityEngine.Tilemaps.TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int gx, int gy, string text, Color color)
        {
            if (text == null || Tilemap == null) return;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue;
                DrawChar(gx + i, gy, c, color);
            }
        }
    }
}
