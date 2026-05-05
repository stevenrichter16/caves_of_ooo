using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// ST.7b — MonoBehaviour rendering layer for the skills screen popup.
    /// Centered tilemap-based modal listing all registered skills + their
    /// powers, color-coded by per-row state (Owned / Buyable /
    /// InsufficientSP / RequirementsNotMet) per the 2-axis Qud convention.
    /// Pure rendering: state comes from <see cref="SkillsScreenStateBuilder.Build"/>;
    /// purchases route through <see cref="BuySkillAction.Execute"/>.
    ///
    /// <para>Lifecycle mirrors <see cref="ContainerPickerUI"/>: GameBootstrap
    /// adds the component, wires the FG + BG tilemaps + popup camera, and
    /// hands the reference to <c>InputHandler</c>. <c>InputHandler</c>
    /// gates KeyCode.X on InputState.Normal to call <see cref="Open"/>,
    /// then dispatches per-frame <see cref="HandleInput"/> while open.
    /// Closing returns control to the gameplay state machine — pressing
    /// Enter on a row buys the skill but does NOT end the player's turn
    /// (matches Qud's convention; the skills screen is meta-action,
    /// not in-world action).</para>
    ///
    /// <para>Visual layout:
    /// <code>
    ///   ┌──────────────────────────────────────────────────────────┐
    ///   │ Skills                                       SP: 250     │
    ///   ├──────────────────────────────────────────────────────────┤
    ///   │ &gt; Acrobatics                                  100sp     │   ← cursor + tree-root
    ///   │     Dodge                                       50sp     │   ← power (indented)
    ///   │   Long Blades                                   75sp     │
    ///   │ ...                                                      │
    ///   ├──────────────────────────────────────────────────────────┤
    ///   │ Athletic finesse and balance.                            │   ← description (selected)
    ///   └──────────────────────────────────────────────────────────┘
    ///   [Enter]buy [↑↓]nav [X/Esc]close
    /// </code>
    /// </para>
    /// </summary>
    public class SkillsScreenUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;

        // Layout — narrower than container picker (skills screen needs less
        // width since rows are name + cost, not full inventory entries).
        private const int POPUP_W = 60;
        private const int POPUP_MAX_VISIBLE = 14;
        private const int DESC_ROWS = 1;  // single-line description footer
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        /// <summary>
        /// Source-of-truth actor whose skills/SP drive the snapshot. Set by
        /// <see cref="Open"/>. Null when closed.
        /// </summary>
        public Entity PlayerEntity { get; set; }

        private bool _isOpen;
        private SkillsScreenSnapshot _snapshot;
        private int _cursorIndex;
        private int _scrollOffset;

        // Layout state — recomputed per Render() based on current snapshot
        // size (popup grows up to POPUP_MAX_VISIBLE rows).
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        public bool IsOpen => _isOpen;
        public int RowCount => _snapshot.RowCount;
        public int CursorIndex => _cursorIndex;

        /// <summary>
        /// Opens the skills screen for the given actor. Builds the snapshot
        /// fresh from <see cref="SkillsScreenStateBuilder"/>, resets scroll
        /// to top. <b>Cursor preserved</b> across re-opens if still in range,
        /// else clamped to 0 — deliberate divergence from
        /// <see cref="ContainerPickerUI.Open"/> which unconditionally resets
        /// the cursor. Rationale: the buy-and-stay-open flow re-renders via
        /// <see cref="RefreshSnapshot"/> after every purchase; if the user
        /// closes and re-opens to the same row, preserving the cursor is
        /// the lower-friction UX. Safe to call when already open.
        /// </summary>
        public void Open(Entity actor)
        {
            if (actor == null)
                return;

            PlayerEntity = actor;
            _snapshot = SkillsScreenStateBuilder.Build(actor);
            _isOpen = true;
            if (_cursorIndex >= _snapshot.RowCount) _cursorIndex = 0;
            _scrollOffset = 0;
            ScrollIntoView();
            Render();
        }

        /// <summary>
        /// Re-snapshots and re-renders. Called after a successful purchase
        /// so the row that was just bought flips from Buyable → Owned and
        /// the SP header updates immediately.
        /// </summary>
        public void RefreshSnapshot()
        {
            if (!_isOpen || PlayerEntity == null) return;
            _snapshot = SkillsScreenStateBuilder.Build(PlayerEntity);
            if (_cursorIndex >= _snapshot.RowCount && _snapshot.RowCount > 0)
                _cursorIndex = _snapshot.RowCount - 1;
            ScrollIntoView();
            Render();
        }

        public void Close()
        {
            if (!_isOpen) return;
            ClearRegion(0, 0, POPUP_W, _popupH);
            ClearBgRegion();
            _isOpen = false;
            PlayerEntity = null;
        }

        /// <summary>
        /// Per-frame input dispatch. Pressed-this-frame keys only — no
        /// hold-repeat; the skills screen is browse-and-act, not nav-heavy.
        /// </summary>
        public void HandleInput()
        {
            if (!_isOpen) return;

            if (InputHelper.GetKeyDown(KeyCode.X) || InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (_snapshot.RowCount == 0)
                return;  // nothing to navigate

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                {
                    _cursorIndex--;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < _snapshot.RowCount - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                TryPurchaseSelected();
                return;
            }
        }

        private void TryPurchaseSelected()
        {
            if (_snapshot.RowCount == 0 || PlayerEntity == null) return;
            if (_cursorIndex < 0 || _cursorIndex >= _snapshot.RowCount) return;

            var row = _snapshot.Rows[_cursorIndex];
            var result = BuySkillAction.Execute(PlayerEntity, row.Class);
            string display = row.IsObfuscated ? "???" : row.DisplayName;

            if (result.Succeeded)
            {
                MessageLog.Add($"You learn {display} (-{result.CostPaid}sp).");
            }
            else
            {
                MessageLog.Add(FormatFailure(display, result));
            }

            // Re-snapshot regardless of success/failure: the state-builder
            // is the source of truth, so even a failed purchase re-renders
            // (cheap, ensures any side-effects elsewhere don't desync).
            RefreshSnapshot();
        }

        // Translate a BuySkillAction.Result into a player-visible message.
        // The diag channel already carries the structured detail; this is
        // the in-world message-log surface.
        private static string FormatFailure(string display, BuySkillAction.Result r)
        {
            switch (r.Reason)
            {
                case BuySkillAction.FailureReason.AlreadyOwned:
                    return $"You already have {display}.";
                case BuySkillAction.FailureReason.InsufficientSP:
                    return $"You need {r.CostPaid}sp to learn {display} ({r.SpBefore} available).";
                case BuySkillAction.FailureReason.StatMinNotMet:
                    return $"You don't have the {r.Detail} for {display}.";
                case BuySkillAction.FailureReason.MissingPrereq:
                    return $"You must learn {r.Detail} first.";
                case BuySkillAction.FailureReason.Exclusion:
                    return $"{display} conflicts with {r.Detail}.";
                case BuySkillAction.FailureReason.UnknownSkillClass:
                    return $"Unknown skill: {display}.";
                case BuySkillAction.FailureReason.ActorMissingSkillsPart:
                case BuySkillAction.FailureReason.ActorMissingSPStat:
                    return "You can't learn skills.";
                default:
                    return $"You can't learn {display}.";
            }
        }

        private void ScrollIntoView()
        {
            int totalRows = _snapshot.RowCount;
            int visible = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            if (visible <= 0) { _scrollOffset = 0; return; }

            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visible)
                _scrollOffset = _cursorIndex - visible + 1;
        }

        // ─────────────────────────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────────────────────────

        private void Render()
        {
            if (Tilemap == null) return;

            ClearBgRegion();
            ComputePopupPosition();

            int totalRows = _snapshot.RowCount;
            int visibleCount = Mathf.Max(1, Mathf.Min(totalRows, POPUP_MAX_VISIBLE));

            // Border layout: top + title + sep + content + sep + desc + bottom.
            int borderH = visibleCount + 5 + DESC_ROWS;  // 5 = top, title, sep, sep-before-desc, bottom

            ClearRegion(0, 0, POPUP_W, _popupH);
            DrawBgFill(0, 0, POPUP_W, borderH);
            DrawPopupBorder(0, 0, POPUP_W, borderH, visibleCount);

            // Title row
            DrawText(2, 1, "Skills", QudColorParser.BrightYellow);
            string spLabel = $"SP: {_snapshot.CurrentSP}";
            DrawText(POPUP_W - 2 - spLabel.Length, 1, spLabel, QudColorParser.BrightCyan);

            int contentY = 3;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(no skills loaded)", QudColorParser.DarkGray);
            }
            else
            {
                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _scrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    var row = _snapshot.Rows[idx];
                    bool selected = idx == _cursorIndex;

                    if (selected)
                        DrawChar(1, rowY, '>', QudColorParser.White);

                    Color nameColor = NameColor(row.State, selected);

                    // Indent powers under their tree-root; tree-roots flush at col 3.
                    int nameCol = row.IsTreeRoot ? 3 : 5;
                    string display = row.DisplayName;
                    int maxNameLen = POPUP_W - nameCol - 10;  // reserve 10 for "  Nsp" + border
                    if (display.Length > maxNameLen)
                        display = display.Substring(0, maxNameLen - 1) + "~";
                    DrawText(nameCol, rowY, display, nameColor);

                    // Cost on the right, color per state. Owned shows nothing.
                    if (row.State != SkillsScreenRowState.Owned && row.Cost > 0)
                    {
                        string costStr = $"{row.Cost}sp";
                        Color costColor = CostColor(row.State);
                        DrawText(POPUP_W - 2 - costStr.Length, rowY, costStr, costColor);
                    }
                    else if (row.State == SkillsScreenRowState.Owned)
                    {
                        // Show "owned" tag (white) instead of cost.
                        // Right-align via the same formula used for cost text
                        // above so the right margin reads cleanly when rows
                        // mix Owned with Buyable / InsufficientSP states.
                        const string ownedTag = "owned";
                        DrawText(POPUP_W - 2 - ownedTag.Length, rowY, ownedTag, QudColorParser.White);
                    }
                }

                // Scroll indicators
                if (_scrollOffset > 0)
                    DrawChar(POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }

            // Description footer for selected row.
            int descSepY = contentY + visibleCount;
            int descY = descSepY + 1;
            if (totalRows > 0)
            {
                var sel = _snapshot.Rows[Mathf.Clamp(_cursorIndex, 0, totalRows - 1)];
                string desc = sel.IsObfuscated ? "???" : (sel.Description ?? "");
                int maxDescLen = POPUP_W - 4;
                if (desc.Length > maxDescLen)
                    desc = desc.Substring(0, maxDescLen - 1) + "~";
                DrawText(2, descY, desc, QudColorParser.Gray);
            }

            // Hint below the border
            int hintY = borderH;
            DrawText(0, hintY, "[Enter]buy [^v]nav [X/Esc]close", QudColorParser.DarkGray);
        }

        private static Color NameColor(SkillsScreenRowState state, bool selected)
        {
            // Selection highlight: brighten the name color slightly via white
            // for owned/insufficient cases. Match Qud convention otherwise.
            switch (state)
            {
                case SkillsScreenRowState.Owned:
                    return QudColorParser.White;
                case SkillsScreenRowState.Buyable:
                case SkillsScreenRowState.InsufficientSP:
                    return QudColorParser.BrightGreen;
                case SkillsScreenRowState.RequirementsNotMet:
                    return selected ? QudColorParser.Gray : QudColorParser.DarkGray;
                default:
                    return QudColorParser.Gray;
            }
        }

        private static Color CostColor(SkillsScreenRowState state)
        {
            switch (state)
            {
                case SkillsScreenRowState.Buyable:           return QudColorParser.BrightCyan;
                case SkillsScreenRowState.InsufficientSP:    return QudColorParser.BrightRed;
                case SkillsScreenRowState.RequirementsNotMet:return QudColorParser.DarkGray;
                default:                                     return QudColorParser.Gray;
            }
        }

        private void ComputePopupPosition()
        {
            int totalRows = _snapshot.RowCount;
            int visibleCount = Mathf.Max(1, Mathf.Min(totalRows, POPUP_MAX_VISIBLE));
            // Border + hint line (outside border).
            _popupH = visibleCount + 5 + DESC_ROWS + 1;  // +1 for hint line
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        // ─────────────────────────────────────────────────────────────────
        // Tilemap primitives — verbatim shape from ContainerPickerUI; the
        // primitives are pure-Unity, no skill-tree-specific logic.
        // ─────────────────────────────────────────────────────────────────

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

        private void DrawPopupBorder(int x, int y, int w, int h, int contentRows)
        {
            // Top
            DrawChar(x, y, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);

            // Title row
            DrawChar(x, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);

            // Separator under title
            DrawChar(x, y + 2, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            // Content rows
            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            // Separator before description
            int descSepY = y + 3 + contentRows;
            DrawChar(x, descSepY, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, descSepY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, descSepY, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            // Description rows
            for (int r = 0; r < DESC_ROWS; r++)
            {
                DrawChar(x, descSepY + 1 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, descSepY + 1 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            // Bottom
            int botY = descSepY + 1 + DESC_ROWS;
            DrawChar(x, botY, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, botY, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
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
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
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
