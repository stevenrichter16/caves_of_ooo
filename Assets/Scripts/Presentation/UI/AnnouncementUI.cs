using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a centered modal popup for important game announcements.
    /// Uses the centered popup overlay grid inside the gameplay viewport.
    /// Long announcements are paginated. Enter/Space/click advances or dismisses
    /// the final page; arrows/PageUp/PageDown navigate and Escape dismisses.
    /// </summary>
    public class AnnouncementUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        /// <summary>Explicit current-world binding; portraits never find scene renderers.</summary>
        public ZoneRenderer WorldPresentation;

        private const int POPUP_W = 56;
        private const int MAX_TEXT_WIDTH = POPUP_W - 4; // 2 chars padding each side
        public const int PageLineCapacity = 25;
        private const int PAGE_LINES = PageLineCapacity; // Leave space above the underlying inventory action rows.
        private static readonly Color PopupBgColor = new Color(.035f, .044f, .038f, 1f);
        private static readonly Color Bronze = new Color(.67f, .46f, .23f, 1f);
        private static readonly Color Ivory = new Color(.89f, .87f, .77f, 1f);
        private static readonly Color SeaGreen = new Color(.42f, .69f, .57f, 1f);
        private static readonly Color Amber = new Color(.97f, .72f, .32f, 1f);
        private static readonly Color RuleColor = new Color(.41f, .45f, .29f, 1f);
        private const int BodyTop = 8;
        private readonly List<LedgerLine> _ledgerLines = new List<LedgerLine>();
        private readonly List<string> _titleLines = new List<string>();
        public AnnouncementDocument Document { get; private set; }
        public int CurrentPage => _pageIndex;
        public string FooterText => BuildFooter(NativeGamepadInput.IsConnected);
        private struct LedgerLine
        {
            public string Text; public Color Color; public string Label;
            public LedgerLine(string text, Color color, string label = null)
            { Text = text; Color = color; Label = label; }
        }

        private bool _isOpen;
        private DocumentReadAudio _documentAudio;
        private ExaminePortraitRenderer _portraitRenderer;
        private FlatPixelPortraitDrawer _flatPortraitDrawer;
        private ExaminePortrait _portrait;
        private ZoneRenderer _portraitPresentation;
        private bool _portraitWasFlat;
        private bool _controllerHints;
        private string _message;
        private readonly List<string> _wrappedLines = new List<string>();
        private int _pageIndex;

        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        // Previously-drawn FG region, tracked so Render() can erase the
        // actual tiles from the last draw (even if position/size changed)
        // before painting the new popup. Without this, chaining Open() for
        // differently-sized messages left top-border / action-bar fragments
        // visible outside the new rectangle — 4 grimoires in a row produced
        // 4 ghost top-bars stacked above the active popup.
        private bool _fgDrawn;
        private int _fgDrawnW;
        private int _fgDrawnH;
        private int _fgDrawnOriginX;
        private int _fgDrawnTopY;

        public bool IsOpen => _isOpen;

        /// <summary>Number of pages in the current message, including its final partial page.</summary>
        public int PageCount => (_wrappedLines.Count + PAGE_LINES - 1) / PAGE_LINES;

        /// <summary>A detached snapshot of the current page's wrapped text, in reading order.</summary>
        public IReadOnlyList<string> VisibleLines => _wrappedLines.GetRange(
            _pageIndex * PAGE_LINES, VisibleLineCount);

        private int VisibleLineCount => System.Math.Min(PAGE_LINES,
            _wrappedLines.Count - _pageIndex * PAGE_LINES);

        /// <summary>Shows a zero-based page while open. Invalid indices leave the message unchanged.</summary>
        public bool GoToPage(int page)
        {
            if (!_isOpen || page < 0 || page >= PageCount) return false;
            bool changed=page!=_pageIndex;
            _pageIndex = page;
            Render();
            if(changed)_documentAudio?.TurnPage();
            return true;
        }

        public void Open(string message)=>Open(message,null,null);
        internal void Open(string message,DocumentReadAudio reading)=>Open(message,reading,null);
        public void OpenDocument(AnnouncementDocument document)
        {
            if (document == null) return;
            Open(document.ToPlainText(), null, document);
        }
        internal void Open(string message, DocumentReadAudio reading, AnnouncementDocument document)
        {
            if (string.IsNullOrEmpty(message)) return;
            _documentAudio=reading;
            _message = message;
            Document = document;
            _isOpen = true;
            _wrappedLines.Clear(); _ledgerLines.Clear(); _titleLines.Clear();
            _pageIndex = 0;
            WrapText(document?.Title ?? "Field notes", 32, _titleLines);
            if (document == null) AddLines(message, Ivory);
            else
            {
                foreach (var section in document.Sections)
                {
                    if (string.IsNullOrEmpty(section.Text)) continue;
                    if (_ledgerLines.Count > 0) AddLines("", Ivory);
                    if (!string.IsNullOrEmpty(section.Heading) && section.Heading != "Description")
                    { AddLines(section.Heading.ToUpperInvariant(), SeaGreen); AddLines("", Ivory); }
                    AddLines(section.Text, section.Tone == AnnouncementTone.Warning ? Amber : Ivory);
                }
                if (document.Facts.Count > 0 && _ledgerLines.Count > 0) AddLines("", Ivory);
                foreach (var fact in document.Facts)
                {
                    var values = new List<string>(); WrapText(fact.Value, MAX_TEXT_WIDTH - 15, values);
                    for (int i = 0; i < values.Count; i++)
                    {
                        string label = i == 0 ? fact.Label : "";
                        // Long labels are wrapped as ordinary text, never silently clipped.
                        if (label.Length > 13) { AddLines(label, SeaGreen); label = ""; }
                        _ledgerLines.Add(new LedgerLine(values[i], Ivory, label));
                        _wrappedLines.Add((label.Length == 0 ? "" : label + ": ") + values[i]);
                    }
                }
                if (_ledgerLines.Count == 0) AddLines(message, Ivory);
                if (_titleLines.Count > 2) { AddLines("Full name", SeaGreen); AddLines(document.Title, Ivory); }
            }
            _portrait = default;
            if (Application.isPlaying) RefreshPortrait();
            else _portraitRenderer?.Clear();
            Render();
        }

        // Called only on explicit popup open/refresh, never during repaint.
        private void RefreshPortrait()
        {
            _portrait = default;
            _portraitPresentation = WorldPresentation;
            _portraitWasFlat = WorldPresentation != null && WorldPresentation.FlatPixelModeActive;
            if (!ExaminePortraitRenderer.IsCurrentPresentationSubject(Document, WorldPresentation))
            { _portraitRenderer?.Clear(); return; }
            if (_portraitRenderer == null) _portraitRenderer = new ExaminePortraitRenderer();
            _portrait = _portraitRenderer.GetPortrait(Document, WorldPresentation);
        }

        // State changes require an explicit refresh to choose new art. Losing
        // authority or changing presentation may only hide the old cached image.
        private void ValidatePortrait()
        {
            if (_portrait.Texture == null) return;
            bool flat = WorldPresentation != null && WorldPresentation.FlatPixelModeActive;
            if (_portraitPresentation == WorldPresentation && _portraitWasFlat == flat
                && ExaminePortraitRenderer.IsCurrentPresentationSubject(Document, WorldPresentation)) return;
            _portrait = default;
            _portraitRenderer?.Clear();
        }

        private void AddLines(string text, Color color)
        {
            var lines = new List<string>(); WrapText(text, MAX_TEXT_WIDTH, lines);
            foreach (var line in lines) { _ledgerLines.Add(new LedgerLine(line, color)); _wrappedLines.Add(line); }
        }

        public void Close()
        {
            if (!_isOpen) return;
            ClearFgRegion();
            ClearBgRegion();
            _isOpen = false;
            _documentAudio=null;
            Document=null;
            _portrait=default;
            _portraitRenderer?.Clear();
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.PageUp))
            {
                GoToPage(_pageIndex - 1);
                return;
            }
            if (InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.PageDown))
            {
                GoToPage(_pageIndex + 1);
                return;
            }
            if (Input.GetMouseButtonDown(0) && CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap,
                Input.mousePosition, out int gx, out int gy))
            { HandlePointerCell(gx,gy); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space)
                )
            {
                if (!GoToPage(_pageIndex + 1)) Close();
                return;
            }
        }

        private void HandlePointerCell(int gx,int gy)
        {
            if (!_isOpen || gx < _worldOriginX || gx >= _worldOriginX + POPUP_W || gy > _worldTopY || gy <= _worldTopY - _popupH) return;
            if (gy == _worldTopY - (_popupH - 3))
            {
                string footer=BuildFooter(_controllerHints);int x=gx-_worldOriginX-2;
                int back=footer.IndexOf(_controllerHints?"[B]":"[Esc]");
                int confirm=footer.IndexOf(_controllerHints?"[A]":"[Enter]");
                int next=footer.IndexOf(_controllerHints?"[RB]":"[Right]");
                if(x>=back && back>=0){Close();return;}
                if(PageCount>1 && x>=0 && x<next){GoToPage(_pageIndex-1);return;}
                if(PageCount>1 && x>=next && x<confirm){GoToPage(_pageIndex+1);return;}
                if(x<confirm)return;
            }
            if(!GoToPage(_pageIndex+1))Close();
        }

        private void Update()
        {
            if(_isOpen && _controllerHints!=NativeGamepadInput.IsConnected)Render();
        }
        private void OnDestroy()
        {
            _portraitRenderer?.Dispose();_portraitRenderer=null;
            _flatPortraitDrawer?.Dispose();_flatPortraitDrawer=null;
        }
        private void OnDisable(){Close();}
        private void OnGUI()
        {
            if(!_isOpen || Event.current.type!=EventType.Repaint)return;
            ValidatePortrait();
            if(_portrait.Texture==null || PopupCamera==null || !PopupCamera.enabled || Tilemap==null)return;
            // Convert the header's tile rectangle through the same overlay camera.
            // The texture is captured only on Open, never in this repaint callback.
            var bottom=PopupCamera.WorldToScreenPoint(Tilemap.CellToWorld(new Vector3Int(_worldOriginX+43,_worldTopY-6,0)));
            var top=PopupCamera.WorldToScreenPoint(Tilemap.CellToWorld(new Vector3Int(_worldOriginX+54,_worldTopY,0)));
            var slot=new Rect(bottom.x,Screen.height-top.y,top.x-bottom.x,top.y-bottom.y);
            var rect=MeasurePortraitRect(slot,_portrait);
            if(rect.width<=0 || rect.height<=0)return;
            var color=GUI.color;int depth=GUI.depth;
            try
            {
                GUI.depth=-20;
                if(UsesFlatPixelPortraitDraw(_portrait,_portraitWasFlat))
                {
                    if(_flatPortraitDrawer==null)_flatPortraitDrawer=new FlatPixelPortraitDrawer();
                    _flatPortraitDrawer.Draw(rect,_portrait);
                }
                else
                {
                    GUI.color=_portrait.Tint;
                    GUI.DrawTextureWithTexCoords(rect,_portrait.Texture,_portrait.Uv,true);
                }
            }
            finally{GUI.color=color;GUI.depth=depth;}
        }

        internal static bool UsesFlatPixelPortraitDraw(ExaminePortrait portrait,bool flatPresentation)
            => flatPresentation && !portrait.IsModel && portrait.Texture!=null;

        /// <summary>
        /// Fits the selected atlas rectangle without stretching. Native sprites
        /// use whole source pixels at integer scale/position when space permits;
        /// tiny slots retain aspect. Existing model captures remain square.
        /// </summary>
        public static Rect MeasurePortraitRect(Rect slot, ExaminePortrait portrait)
        {
            if (portrait.Texture == null || slot.width <= 0 || slot.height <= 0
                || !Finite(slot.x) || !Finite(slot.y) || !Finite(slot.width) || !Finite(slot.height)) return Rect.zero;
            if (portrait.IsModel)
            {
                float side = Mathf.Min(slot.width, slot.height);
                return new Rect(slot.center.x - side * .5f, slot.center.y - side * .5f, side, side);
            }
            float width = portrait.Uv.width * portrait.Texture.width;
            float height = portrait.Uv.height * portrait.Texture.height;
            if (!Finite(width) || !Finite(height) || width < 1 || height < 1) return Rect.zero;
            // Sprite rectangles contain whole source pixels; remove normalized
            // UV roundoff before selecting an integer enlargement.
            width = Mathf.Round(width); height = Mathf.Round(height);
            float left = Mathf.Ceil(slot.xMin), top = Mathf.Ceil(slot.yMin);
            float right = Mathf.Floor(slot.xMax), bottom = Mathf.Floor(slot.yMax);
            float scale = Mathf.Floor(Mathf.Min((right - left) / width, (bottom - top) / height));
            if (scale >= 1)
            {
                width *= scale; height *= scale;
                float x = Mathf.Clamp(Mathf.Round(slot.center.x - width * .5f), left, right - width);
                float y = Mathf.Clamp(Mathf.Round(slot.center.y - height * .5f), top, bottom - height);
                return new Rect(x, y, width, height);
            }
            scale = Mathf.Min(slot.width / width, slot.height / height);
            width *= scale; height *= scale;
            return new Rect(slot.center.x - width * .5f, slot.center.y - height * .5f, width, height);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        // ===== Rendering =====

        private void ComputePopupPosition()
        {
            _popupH = BodyTop + System.Math.Max(1, VisibleLineCount) + 4;
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private string BuildFooter(bool controller)
        {
            bool last = _pageIndex + 1 >= PageCount;
            if (controller) return PageCount > 1
                ? "[LB] prev [RB] next [A] " + (last ? "close" : "next") + "  [B] back"
                : "[A] Close   [B] Back";
            return PageCount > 1 ? "[Left] prev [Right] next [Enter] " + (last ? "close" : "next") + "  [Esc] back"
                : "[Enter] Close   [Esc] Back";
        }

        private void Render()
        {
            if (Tilemap == null) return;
            ClearFgRegion(); ClearBgRegion(); ComputePopupPosition();
            _controllerHints=NativeGamepadInput.IsConnected;
            DrawBgFill(0, 0, POPUP_W, _popupH);
            for (int y = 1; y < _popupH - 1; y++)
            {
                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, Bronze);
                DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, Bronze);
            }
            for (int x = 1; x < POPUP_W - 1; x++)
            {
                DrawChar(x, 0, CP437TilesetGenerator.BoxHorizontal, Bronze);
                DrawChar(x, _popupH - 1, CP437TilesetGenerator.BoxHorizontal, Bronze);
            }
            DrawChar(0, 0, CP437TilesetGenerator.BoxTopLeft, Bronze);
            DrawChar(POPUP_W - 1, 0, CP437TilesetGenerator.BoxTopRight, Bronze);
            DrawChar(0, _popupH - 1, CP437TilesetGenerator.BoxBottomLeft, Bronze);
            DrawChar(POPUP_W - 1, _popupH - 1, CP437TilesetGenerator.BoxBottomRight, Bronze);
            DrawText(2, 1, HudHotbarPresentation.Fit(Document?.Kind ?? "NOTICE", 40), SeaGreen);
            for (int i = 0; i < System.Math.Min(2,_titleLines.Count); i++)
                DrawText(2, 3 + i*2, i == 1 && _titleLines.Count > 2 ? HudHotbarPresentation.Fit(_titleLines[i] + ">",32) : _titleLines[i], Amber, 2.1f, 1.25f);
            DrawRule(7);
            for (int i = 0; i < VisibleLineCount; i++)
            {
                var line = _ledgerLines[_pageIndex * PAGE_LINES + i];
                if (line.Label == null) DrawText(2, BodyTop + i, line.Text, line.Color);
                else
                {
                    DrawText(2, BodyTop + i, line.Label, SeaGreen);
                    DrawChar(16, BodyTop + i, CP437TilesetGenerator.BoxVertical, RuleColor);
                    DrawText(17, BodyTop + i, line.Text, line.Color);
                }
            }
            DrawRule(_popupH - 4);
            DrawText(2, _popupH - 3, FooterText, Ivory);
            DrawText(2, _popupH - 2, "READING  /  no turn passes", SeaGreen);
            string pages = (_pageIndex + 1) + " / " + PageCount;
            DrawText(POPUP_W - 2 - pages.Length, _popupH - 2, pages, SeaGreen);
            _fgDrawn = true; _fgDrawnW = POPUP_W; _fgDrawnH = _popupH;
            _fgDrawnOriginX = _worldOriginX; _fgDrawnTopY = _worldTopY;
        }

        private void DrawRule(int y)
        {
            for (int x = 2; x < POPUP_W - 2; x++) DrawChar(x,y,CP437TilesetGenerator.BoxHorizontal,RuleColor);
        }

        // ===== Word Wrap =====

        private static void WrapText(string text, int maxWidth, List<string> lines)
        {
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return;
            }

            string[] paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
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

                    int breakAt = para.LastIndexOf(' ', pos + maxWidth - 1, maxWidth);
                    if (breakAt <= pos)
                        breakAt = pos + maxWidth;

                    lines.Add(para.Substring(pos, breakAt - pos));
                    pos = breakAt;
                    if (pos < para.Length && para[pos] == ' ')
                        pos++;
                }
            }
        }

        // ===== Drawing Helpers (world-space, same as DialogueUI) =====

        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var blockTile = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
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

        /// <summary>
        /// Erase the FG tiles painted by the previous Render() call, using
        /// the origin/size we recorded at that time (not the current ones —
        /// those may already have been reassigned for the NEW popup).
        /// </summary>
        private void ClearFgRegion()
        {
            if (!_fgDrawn || Tilemap == null) return;

            for (int dy = 0; dy < _fgDrawnH; dy++)
            {
                for (int dx = 0; dx < _fgDrawnW; dx++)
                {
                    int wx = _fgDrawnOriginX + dx;
                    int wy = _fgDrawnTopY - dy;
                    Tilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }

            _fgDrawn = false;
        }

        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null) return;
            int wx = _worldOriginX + gx;
            int wy = _worldTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetUiTile(Cp437.Map(c));
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
            Tilemap.SetTransformMatrix(tilePos, Matrix4x4.identity);
        }

        private void DrawText(int gx, int gy, string text, Color color, float scale=1.6f, float advance=1f)
        {
            if (text == null || Tilemap == null) return;
            float cellWidth = Mathf.Abs(Tilemap.GetCellCenterLocal(Vector3Int.right).x - Tilemap.GetCellCenterLocal(Vector3Int.zero).x);
            if (advance > 1) scale = Mathf.Min(scale, cellWidth * advance * 2f);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue;
                var tilePos = new Vector3Int(_worldOriginX + gx + i, _worldTopY - gy, 0);
                Tilemap.SetTile(tilePos, HudPixelFont.GetTile(c));
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
                Tilemap.SetTransformMatrix(tilePos, Matrix4x4.TRS(new Vector3(i*(advance-1)*cellWidth,0,0),Quaternion.identity,new Vector3(scale,scale,1)));
            }
        }
    }
}
