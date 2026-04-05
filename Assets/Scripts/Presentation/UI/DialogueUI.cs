using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
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
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private const int POPUP_W = 56;
        private const int POPUP_MAX_VISIBLE_CHOICES = 10;
        private const int MAX_TEXT_WIDTH = POPUP_W - 6; // 3 chars padding each side (space for tag prefix)

        // Opaque dark fill color for popup background (drawn on BgTilemap behind main glyphs)
        private static readonly Color PopupBgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        private bool _isOpen;
        private int _cursorIndex;
        private List<string> _wrappedTextLines = new List<string>();
        private bool _conversationEnded;

        // Popup world-space anchors (same pattern as PickupUI)
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;

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
            ClearBgRegion();
            if (ConversationManager.IsActive)
                ConversationManager.EndConversation();
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ConversationManager.CurrentNode == null ||
                    ConversationManager.CurrentNode.AllowEscape)
                {
                    _conversationEnded = true;
                    Close();
                    return;
                }
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
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                    _cursorIndex--;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < choices.Count - 1)
                    _cursorIndex++;
                Render();
                return;
            }

            // Enter to select
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (choices.Count > 0)
                    SelectChoice(_cursorIndex);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < choices.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
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
                _isOpen = false;
                return;
            }

            _cursorIndex = 0;
            RebuildText();
            Render();
        }

        private void RebuildText()
        {
            _wrappedTextLines.Clear();
            if (ConversationManager.CurrentNode == null) return;

            string text = ConversationManager.CurrentNode.Text ?? "";
            WrapText(text, MAX_TEXT_WIDTH, _wrappedTextLines);
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
            var cam = Camera.main;
            if (cam == null) return;

            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);
            int textLines = _wrappedTextLines.Count;

            // Layout: top border (with embedded title) + text lines + blank + choices + bottom border + action bar
            // = 1 + textLines + 1 + choiceCount + 1 + 1
            _popupH = 1 + textLines + 1 + choiceCount + 1 + 1;

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            _worldOriginX = Mathf.RoundToInt(camX) - POPUP_W / 2;
            _worldTopY = Mathf.RoundToInt(camY) + _popupH / 2;
        }

        private void Render()
        {
            if (Tilemap == null || !ConversationManager.IsActive) return;

            // Clear any previous-frame bg before layout changes (popup height may change per node)
            ClearBgRegion();

            ComputePopupPosition();

            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);
            int textLines = _wrappedTextLines.Count;

            ClearRegion(0, 0, POPUP_W, _popupH);

            // Opaque bg fill behind everything except the action bar (last row)
            DrawBgFill(0, 0, POPUP_W, _popupH - 1);

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

            // Title segment length: "[ g Name ]" = 4 fixed + glyph(1) + name
            int maxNameLen = POPUP_W - 14; // leave room for corners and some border
            if (speakerName.Length > maxNameLen) speakerName = speakerName.Substring(0, maxNameLen);
            int titleLen = 6 + speakerName.Length; // "[ g NAME ]" = 6 literals + name
            int titleStart = 3; // after corner + 2 horizontal bars

            // Corners (use double-line top corners for emphasis)
            DrawChar(0, 0, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, 0, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);
            // Horizontal bars on each side of title
            for (int i = 1; i < titleStart; i++)
                DrawChar(i, 0, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            for (int i = titleStart + titleLen; i < POPUP_W - 1; i++)
                DrawChar(i, 0, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);

            // Title content: [ g SpeakerName ]
            int tx = titleStart;
            DrawChar(tx, 0, '[', QudColorParser.Gray); tx++;
            DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
            DrawChar(tx, 0, speakerGlyph, speakerGlyphColor); tx++;
            DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
            DrawText(tx, 0, speakerName, QudColorParser.BrightYellow); tx += speakerName.Length;
            DrawChar(tx, 0, ' ', QudColorParser.Gray); tx++;
            DrawChar(tx, 0, ']', QudColorParser.Gray);

            // ----- NPC speech text -----
            int y = 1;
            for (int i = 0; i < textLines; i++)
            {
                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawText(2, y, _wrappedTextLines[i], QudColorParser.White);
                y++;
            }

            // Blank line between text and choices
            DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            y++;

            // ----- Player choices with semantic tags -----
            for (int i = 0; i < choiceCount; i++)
            {
                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);

                bool selected = (i == _cursorIndex);

                if (selected)
                    DrawChar(1, y, '>', QudColorParser.White);

                // Hotkey
                int col = 2;
                if (i < 26)
                {
                    char hotkey = (char)('a' + i);
                    string hk = hotkey + ")";
                    DrawText(col, y, hk, selected ? QudColorParser.White : QudColorParser.Gray);
                    col += 3; // 2 chars + space
                }

                // Semantic tag prefix
                InferChoiceTag(choices[i], out string tag, out Color tagColor);
                if (!string.IsNullOrEmpty(tag))
                {
                    DrawText(col, y, tag, tagColor);
                    col += tag.Length + 1;
                }

                // Choice text
                string choiceText = choices[i].Text ?? "";
                int maxLen = POPUP_W - 2 - col;
                if (choiceText.Length > maxLen)
                    choiceText = choiceText.Substring(0, maxLen - 1) + "~";
                Color textColor = selected ? QudColorParser.BrightCyan : QudColorParser.Gray;
                DrawText(col, y, choiceText, textColor);
                y++;
            }

            // Bottom border
            DrawChar(0, y, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
            y++;

            // Action bar
            string actions = " [Enter]select [a-z]quick select [Esc]close";
            DrawText(0, y, actions, QudColorParser.DarkGray);
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
        }

        /// <summary>
        /// Clears the bg tiles we drew behind the last-rendered popup.
        /// ZoneRenderer's unexplored-fog pass will rewrite these cells as needed on redraw.
        /// </summary>
        private void ClearBgRegion()
        {
            if (!_bgDrawn || BgTilemap == null) return;
            for (int dy = 0; dy < _popupH; dy++)
            {
                for (int dx = 0; dx < POPUP_W; dx++)
                {
                    int wx = _worldOriginX + dx;
                    int wy = _worldTopY - dy;
                    BgTilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }
            _bgDrawn = false;
        }

        // ===== Mouse =====

        private int GetChoiceRowAtMouse()
        {
            var cam = Camera.main;
            if (cam == null) return -1;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            int worldX = Mathf.FloorToInt(world.x);
            int worldY = Mathf.FloorToInt(world.y);

            int gx = worldX - _worldOriginX;
            int gy = _worldTopY - worldY;

            int textLines = _wrappedTextLines.Count;
            int choicesStartY = 1 + textLines + 1; // after top border + text + blank

            var choices = ConversationManager.VisibleChoices;
            int choiceCount = Mathf.Min(choices.Count, POPUP_MAX_VISIBLE_CHOICES);

            if (gx > 0 && gx < POPUP_W - 1
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
