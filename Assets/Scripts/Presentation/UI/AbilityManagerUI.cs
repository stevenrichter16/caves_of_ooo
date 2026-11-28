using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Browse owned abilities, activate one, or manage its hotbar binding.
    /// </summary>
    public class AbilityManagerUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        internal bool UseLedgerView;

        // Layout — narrower than container picker, wider than skills
        // screen. Each row: [hotkey] (4) + name (~30) + cooldown (~6) +
        // class (~12) + margins.
        private const int POPUP_W = 60;
        private const int POPUP_MAX_VISIBLE = 14;
        private const int DESC_ROWS = 1;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);
        private const int LedgerWidth = 844;
        private const int LedgerInset = 24;
        private const int LedgerFirstRow = 103;
        private const int LedgerRowStride = 32;
        private LedgerModalView _ledger;
        private bool _ledgerGamepad;
        private Vector2 _lastLedgerPointer;
        private Rect _activateBounds, _closeBounds;

        public Entity PlayerEntity { get; set; }

        /// <summary>Callback fired when the user presses Enter on a row.
        /// The UI closes itself first, then invokes this with the
        /// selected ability's Guid. <see cref="Presentation.Input.InputHandler"/>
        /// hands a callback that re-enters its existing ability-activation
        /// path (direction targeting if needed).</summary>
        public Action<Guid> OnActivateCallback;

        private bool _isOpen;
        private AbilityManagerSnapshot _snapshot;
        private int _cursorIndex;
        private int _scrollOffset;

        // Layout state
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
        public System.Guid SelectedAbilityID => _isOpen && _cursorIndex >= 0 && _cursorIndex < _snapshot.RowCount
            ? _snapshot.Rows[_cursorIndex].AbilityID : System.Guid.Empty;
        private System.Guid _readerSelection = System.Guid.Empty;

        /// <summary>Temporarily remove the menu footprint without closing or losing its cursor/callback.</summary>
        public void HideForReader()
        {
            if (!_isOpen) return;
            _readerSelection = SelectedAbilityID;
            ClearView();
        }

        /// <summary>Revalidate rows after a free reader and retain the selected identity when still present.</summary>
        public void RestoreAfterReader()
        {
            if (!_isOpen || PlayerEntity == null) return;
            _snapshot = AbilityManagerStateBuilder.Build(PlayerEntity);
            for (int i = 0; i < _snapshot.RowCount; i++)
                if (_snapshot.Rows[i].AbilityID == _readerSelection) { _cursorIndex = i; break; }
            _cursorIndex = System.Math.Max(0, System.Math.Min(_cursorIndex, _snapshot.RowCount - 1));
            ScrollIntoView();
            if (UseLedgerView) _lastLedgerPointer = ReadLedgerPointer(out _, out _);
            Render();
        }


        /// <summary>Open the manager for the given actor. Builds snapshot,
        /// resets cursor to 0. Safe to call when already open (re-snapshots).</summary>
        public void Open(Entity actor, Action<Guid> onActivate = null)
        {
            if (actor == null) return;

            PlayerEntity = actor;
            OnActivateCallback = onActivate;
            _snapshot = AbilityManagerStateBuilder.Build(actor);
            _isOpen = true;
            if (_cursorIndex >= _snapshot.RowCount) _cursorIndex = 0;
            _scrollOffset = 0;
            ScrollIntoView();
            if (UseLedgerView) _lastLedgerPointer = ReadLedgerPointer(out _, out _);
            Render();
        }

        /// <summary>Re-snapshots and re-renders. Called after a successful
        /// rebind so the row's hotkey column updates immediately.</summary>
        public void RefreshSnapshot()
        {
            if (!_isOpen || PlayerEntity == null) return;
            _snapshot = AbilityManagerStateBuilder.Build(PlayerEntity);
            if (_cursorIndex >= _snapshot.RowCount && _snapshot.RowCount > 0)
                _cursorIndex = _snapshot.RowCount - 1;
            ScrollIntoView();
            Render();
        }

        public void Close()
        {
            if (!_isOpen) return;
            ClearView();
            _isOpen = false;
            PlayerEntity = null;
            OnActivateCallback = null;
        }

        private void OnDisable() => Close();

        private void ClearView()
        {
            _ledger?.Hide();
            if (Tilemap != null) ClearRegion(0, 0, POPUP_W, _popupH);
            ClearBgRegion();
        }

        /// <summary>Per-frame input dispatch. Returns void; check
        /// <see cref="IsOpen"/> after to see if the modal closed itself.</summary>
        public void HandleInput()
        {
            if (!_isOpen) return;

            // Close on M or Escape.
            if (InputHelper.GetKeyDown(KeyCode.M) ||
                InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (UseLedgerView && HandleLedgerPointer())
                return;

            if (_snapshot.RowCount == 0)
                return;

            // Navigation.
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) ||
                InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                {
                    _cursorIndex--;
                    ScrollIntoView();
                    Render();
                }
                return;
            }
            if (InputHelper.GetKeyDown(KeyCode.DownArrow) ||
                InputHelper.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < _snapshot.RowCount - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            // Activate (Enter / Return / KeypadEnter / Space).
            if (InputHelper.GetKeyDown(KeyCode.Return) ||
                InputHelper.GetKeyDown(KeyCode.KeypadEnter) ||
                InputHelper.GetKeyDown(KeyCode.Space))
            {
                ActivateSelected();
                return;
            }

            // Bind to slot 1-9 / 0.
            int targetSlot = ReadSlotKey();
            if (targetSlot >= 0)
            {
                BindSelectedToSlot(targetSlot);
                return;
            }

            // Unbind selected (R).
            if (InputHelper.GetKeyDown(KeyCode.R))
            {
                UnbindSelected();
                return;
            }
        }

        private static int ReadSlotKey()
        {
            if (InputHelper.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (InputHelper.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (InputHelper.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (InputHelper.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (InputHelper.GetKeyDown(KeyCode.Alpha5)) return 4;
            if (InputHelper.GetKeyDown(KeyCode.Alpha6)) return 5;
            if (InputHelper.GetKeyDown(KeyCode.Alpha7)) return 6;
            if (InputHelper.GetKeyDown(KeyCode.Alpha8)) return 7;
            if (InputHelper.GetKeyDown(KeyCode.Alpha9)) return 8;
            if (InputHelper.GetKeyDown(KeyCode.Alpha0)) return 9;
            return -1;
        }

        private void ActivateSelected()
        {
            if (_cursorIndex < 0 || _cursorIndex >= _snapshot.RowCount) return;
            var row = _snapshot.Rows[_cursorIndex];
            // Stash callback + ID before Close() clears them.
            var callback = OnActivateCallback;
            var abilityID = row.AbilityID;
            Close();
            callback?.Invoke(abilityID);
        }

        private void BindSelectedToSlot(int slot)
        {
            if (_cursorIndex < 0 || _cursorIndex >= _snapshot.RowCount) return;
            if (PlayerEntity == null) return;

            var abilities = PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return;

            var row = _snapshot.Rows[_cursorIndex];
            abilities.AssignAbilityToSlot(row.AbilityID, slot);
            // AssignAbilityToSlot's swap-on-conflict semantics handle the
            // case where another ability was already in that slot — it
            // gets unbound first. So a single call here is enough.
            RefreshSnapshot();
        }

        private void UnbindSelected()
        {
            if (_cursorIndex < 0 || _cursorIndex >= _snapshot.RowCount) return;
            if (PlayerEntity == null) return;

            var row = _snapshot.Rows[_cursorIndex];
            if (row.SlotIndex < 0) return; // already unbound

            var abilities = PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return;

            // Clear the slot via Guid.Empty (per AssignAbilityToSlot's
            // documented contract: empty Guid clears the slot).
            abilities.AssignAbilityToSlot(Guid.Empty, row.SlotIndex);
            RefreshSnapshot();
        }

        private void ScrollIntoView()
        {
            int totalRows = _snapshot.RowCount;
            int visibleCount = Mathf.Max(1, Mathf.Min(totalRows, POPUP_MAX_VISIBLE));
            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visibleCount)
                _scrollOffset = _cursorIndex - visibleCount + 1;
        }

        // ─────────────────────────────────────────────────────────────────
        // Rendering — same primitives + border shape as SkillsScreenUI;
        // copied inline rather than refactored into a base class to keep
        // each modal self-contained per the existing CoO convention.
        // ─────────────────────────────────────────────────────────────────

        private void Render()
        {
            if (UseLedgerView)
            {
                RenderLedger();
                return;
            }
            if (Tilemap == null) return;

            ClearRegion(0, 0, POPUP_W, _popupH);
            ClearBgRegion();
            ComputePopupPosition();

            int totalRows = _snapshot.RowCount;
            int visibleCount = Mathf.Max(1, Mathf.Min(totalRows, POPUP_MAX_VISIBLE));
            int borderH = visibleCount + 5 + DESC_ROWS;

            ClearRegion(0, 0, POPUP_W, _popupH);
            DrawBgFill(0, 0, POPUP_W, _popupH);
            DrawPopupBorder(0, 0, POPUP_W, borderH, visibleCount);

            // Title.
            DrawText(2, 1, "Abilities", QudColorParser.BrightYellow);
            string countLabel = totalRows == 0
                ? "(none)"
                : $"{totalRows} known";
            DrawText(POPUP_W - 2 - countLabel.Length, 1, countLabel,
                QudColorParser.BrightCyan);

            int contentY = 3;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(you know no rites)", QudColorParser.DarkGray);
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

                    // Hotkey column: [N] or [-]
                    string hotkeyStr = "[" + row.Hotkey + "]";
                    DrawText(3, rowY, hotkeyStr, HotkeyColor(row));

                    // Name column.
                    Color nameColor = NameColor(row, selected);
                    int nameCol = 7;
                    string display = row.DisplayName;
                    int maxNameLen = 28;
                    if (display.Length > maxNameLen)
                        display = display.Substring(0, maxNameLen - 1) + "~";
                    DrawText(nameCol, rowY, display, nameColor);

                    // Cooldown / ready column.
                    string cdLabel = row.IsUsable ? "ready" : (row.CooldownRemaining + "T");
                    Color cdColor = row.IsUsable
                        ? QudColorParser.BrightGreen
                        : QudColorParser.DarkGray;
                    int cdCol = nameCol + maxNameLen + 1;
                    DrawText(cdCol, rowY, cdLabel, cdColor);

                    // Source class column (right-aligned).
                    string source = TruncateClass(row.SourceClass);
                    DrawText(POPUP_W - 2 - source.Length, rowY, source,
                        QudColorParser.Gray);
                }

                // Scroll indicators.
                if (_scrollOffset > 0)
                    DrawChar(POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(POPUP_W - 2, contentY + visibleCount - 1, 'v',
                        QudColorParser.Gray);
            }

            // Description row — synthesized per-selection contextual
            // help. ActivatedAbility doesn't carry a Description field
            // today (CoO simplification of Qud's ActivatedAbilityEntry),
            // so we generate a useful 1-line status string from the
            // selected row's binding + cooldown state. Polishes the
            // formerly-blank description row into a useful affordance.
            int descSepY = contentY + visibleCount;
            int descY = descSepY + 1;
            if (totalRows > 0)
            {
                var sel = _snapshot.Rows[Mathf.Clamp(_cursorIndex, 0, totalRows - 1)];
                // Description synthesis lives in the state-builder so
                // the format is testable without MonoBehaviour setup
                // (see AbilityManagerStateBuilderTests).
                string desc = AbilityManagerStateBuilder.BuildRowDescription(sel);
                int maxDescLen = POPUP_W - 4;
                if (desc.Length > maxDescLen)
                    desc = desc.Substring(0, maxDescLen - 1) + "~";
                DrawText(2, descY, desc, QudColorParser.Gray);
            }

            // Footer hint.
            int hintY = borderH;
            DrawText(0, hintY, NativeGamepadInput.IsConnected
                ? "[A]use [Y]bind/clear [LT+RT]details [B]close"
                : "[Enter]use [D]details [P]rite [0-9]bind [R]clear [Esc]close",
                QudColorParser.Gray);
        }

        private void RenderLedger()
        {
            _ledger ??= LedgerModalView.Create(transform, "Abilities Ledger");
            _ledgerGamepad = NativeGamepadInput.IsConnected;
            int total = _snapshot.RowCount;
            int visibleCount = Mathf.Clamp(total, 1, POPUP_MAX_VISIBLE);
            _scrollOffset = Mathf.Clamp(_scrollOffset, 0, Mathf.Max(0, total - visibleCount));
            int detailY = LedgerFirstRow + LedgerRowStride * visibleCount + 10;
            string footer = _ledgerGamepad
                ? "Y Bind / clear    ·    LT + RT Details"
                : "D Details    ·    P Rite preview    ·    0–9 Bind    ·    R Clear binding";
            _ledger.Begin(PopupCamera, LedgerWidth, detailY + (total > 0 ? 112 : 68));
            _ledger.Card("Abilities", total > 0 ? footer : null);

            if (total == 0)
                _ledger.Row(LedgerRowBounds(0), "You know no activated abilities.", false, disabled: true);
            else
            {
                string count = total > visibleCount
                    ? $"{_scrollOffset + 1}–{_scrollOffset + visibleCount} of {total} known"
                    : $"{total} known";
                _ledger.Label(new Rect(624, 15, 196, 38), count, LMuted, 17, TextAnchor.MiddleRight);
                DrawLedgerHeader();
                for (int row = 0; row < visibleCount; row++)
                    DrawLedgerAbilityRow(row);

                AbilityManagerRow selected = _snapshot.Rows[_cursorIndex];
                var detail = new Rect(LedgerInset, detailY, 518, 55);
                _ledger.Frame(detail, LBorder, LPanel);
                _ledger.Label(new Rect(detail.x + 12, detail.y + 3, detail.width - 24, 25),
                    selected.DisplayName, LGold, 18, bold: true);
                string description = _ledgerGamepad && selected.SlotIndex < 0 && selected.IsUsable
                    ? "Unbound. Press Y to assign a slot."
                    : AbilityManagerStateBuilder.BuildRowDescription(selected);
                _ledger.Label(new Rect(detail.x + 12, detail.y + 27, detail.width - 24, 24),
                    description, LMuted, 15);
            }

            _activateBounds = new Rect(558, detailY + 10, 128, 35);
            _closeBounds = new Rect(694, detailY + 10, 126, 35);
            if (total > 0)
                DrawLedgerButton(_activateBounds, _ledgerGamepad ? "A  Activate" : "Enter  Activate",
                    _snapshot.Rows[_cursorIndex].IsUsable, primary: true);
            DrawLedgerButton(_closeBounds, _ledgerGamepad ? "B  Close" : "Esc  Close", true);
            _ledger.End();
        }

        private void DrawLedgerHeader()
        {
            _ledger.Label(new Rect(30, 76, 46, 23), "Slot", LMuted, 15, TextAnchor.MiddleCenter);
            _ledger.Label(new Rect(91, 76, 376, 23), "Ability", LMuted, 15);
            _ledger.Label(new Rect(475, 76, 173, 23), "Source", LMuted, 15);
            _ledger.Label(new Rect(655, 76, 147, 23), "Readiness", LMuted, 15, TextAnchor.MiddleRight);
        }

        private void DrawLedgerAbilityRow(int row)
        {
            int index = _scrollOffset + row;
            AbilityManagerRow ability = _snapshot.Rows[index];
            Rect bounds = LedgerRowBounds(row);
            bool selected = index == _cursorIndex;
            _ledger.Box(bounds, selected ? LSelected : LPanel);
            if (selected) _ledger.Box(new Rect(bounds.x, bounds.y, 3, bounds.height), LGold);

            var hotkey = new Rect(35, bounds.y + 3, 35, bounds.height - 6);
            _ledger.Frame(hotkey, LBorder, LBackground);
            _ledger.Label(hotkey, ability.SlotIndex >= 0 ? ability.Hotkey.ToString() : "–",
                ability.SlotIndex >= 0 ? LGold : LMuted, 15, TextAnchor.MiddleCenter);
            _ledger.Label(new Rect(91, bounds.y, 376, bounds.height), ability.DisplayName, LInk, 19);
            _ledger.Label(new Rect(475, bounds.y, 173, bounds.height), ability.SourceClass, LMuted, 16);
            string units = ability.CooldownRemaining == 1 ? "turn" : "turns";
            string readiness = ability.IsUsable ? "Ready" : $"{ability.CooldownRemaining} {units}";
            _ledger.Label(new Rect(655, bounds.y, 147, bounds.height), readiness,
                ability.IsUsable ? LGreen : LGold, 16, TextAnchor.MiddleRight);
        }

        private void DrawLedgerButton(Rect bounds, string label, bool enabled, bool primary = false)
        {
            bool emphasized = enabled && primary;
            _ledger.Frame(bounds, emphasized ? LGold : LBorder, emphasized ? LSelected : LPanel);
            Color ink = emphasized ? LGold : enabled ? LInk : LMuted;
            _ledger.Label(bounds, label, ink, 17, TextAnchor.MiddleCenter);
        }

        private static Rect LedgerRowBounds(int row)
            => new Rect(LedgerInset, LedgerFirstRow + row * LedgerRowStride, LedgerWidth - 2 * LedgerInset, 28);

        private bool HandleLedgerPointer()
        {
            if (_ledgerGamepad != NativeGamepadInput.IsConnected) Render();
            Vector2 mouse = ReadLedgerPointer(out bool pressed, out float wheel);
            bool moved = mouse != _lastLedgerPointer;
            _lastLedgerPointer = mouse;
            if ((!moved && !pressed && Mathf.Abs(wheel) < .01f) || _ledger == null
                || !_ledger.TryPoint(mouse, out Vector2 point)) return false;

            if (pressed && _closeBounds.Contains(point))
            {
                Close();
                return true;
            }
            int total = _snapshot.RowCount;
            if (total == 0) return false;
            if (pressed && _activateBounds.Contains(point))
            {
                if (_snapshot.Rows[_cursorIndex].IsUsable) ActivateSelected();
                return true;
            }

            int row = Mathf.FloorToInt((point.y - LedgerFirstRow) / LedgerRowStride);
            int visibleCount = Mathf.Min(total, POPUP_MAX_VISIBLE);
            if (row < 0 || row >= visibleCount || !LedgerRowBounds(row).Contains(point)) return false;
            int next = Mathf.Abs(wheel) >= .01f
                ? Mathf.Clamp(_cursorIndex + (wheel > 0 ? -1 : 1), 0, total - 1)
                : _scrollOffset + row;
            if (next == _cursorIndex) return pressed;
            _cursorIndex = next;
            ScrollIntoView();
            Render();
            return pressed || Mathf.Abs(wheel) >= .01f;
        }

        private static Vector2 ReadLedgerPointer(out bool pressed, out float wheel)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.enabled)
            {
                pressed = mouse.leftButton.wasPressedThisFrame;
                wheel = mouse.scroll.ReadValue().y;
                return mouse.position.ReadValue();
            }
            pressed = Input.GetMouseButtonDown(0);
            wheel = Input.mouseScrollDelta.y;
            return Input.mousePosition;
        }

        // BuildDescription was moved to AbilityManagerStateBuilder.BuildRowDescription
        // so it's testable as pure-data formatting without MonoBehaviour setup.
        // See AbilityManagerStateBuilderTests for the per-row format pins.

        private static Color HotkeyColor(AbilityManagerRow row)
        {
            if (row.SlotIndex < 0) return QudColorParser.DarkGray; // unbound
            return row.IsUsable ? QudColorParser.BrightYellow : QudColorParser.Gray;
        }

        private static Color NameColor(AbilityManagerRow row, bool selected)
        {
            if (!row.IsUsable) return QudColorParser.Gray;
            return selected ? QudColorParser.White : QudColorParser.BrightCyan;
        }

        private static string TruncateClass(string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            const int maxLen = 12;
            if (source.Length > maxLen)
                return source.Substring(0, maxLen - 1) + "~";
            return source;
        }

        private void ComputePopupPosition()
        {
            int totalRows = _snapshot.RowCount;
            int visibleCount = Mathf.Max(1, Mathf.Min(totalRows, POPUP_MAX_VISIBLE));
            _popupH = visibleCount + 5 + DESC_ROWS + 1; // +1 for hint line
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        // ─────────────────────────────────────────────────────────────────
        // Tilemap primitives — copied verbatim from SkillsScreenUI. Self-
        // contained so this UI doesn't depend on a fragile base-class
        // refactor; if a third modal needs the primitives, refactor then.
        // ─────────────────────────────────────────────────────────────────

        private void ClearRegion(int gx, int gy, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    Tilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
        }

        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var blockTile = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null) return;

            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    var pos = new Vector3Int(wx, wy, 0);
                    BgTilemap.SetTile(pos, blockTile);
                    BgTilemap.SetTileFlags(pos, TileFlags.None);
                    BgTilemap.SetColor(pos, PopupBgColor);
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
                for (int dx = 0; dx < _bgDrawnW; dx++)
                {
                    int wx = _bgDrawnOriginX + dx;
                    int wy = _bgDrawnTopY - dy;
                    BgTilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            _bgDrawn = false;
        }

        private void DrawPopupBorder(int x, int y, int w, int h, int contentRows)
        {
            DrawChar(x, y, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);

            DrawChar(x, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);

            DrawChar(x, y + 2, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            int descSepY = y + 3 + contentRows;
            DrawChar(x, descSepY, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, descSepY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, descSepY, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            for (int r = 0; r < DESC_ROWS; r++)
            {
                DrawChar(x, descSepY + 1 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, descSepY + 1 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

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
            var tile = CP437TilesetGenerator.GetUiTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int gx, int gy, string text, Color color)
        {
            if (text == null || Tilemap == null) return;
            for (int i = 0; i < text.Length; i++)
                DrawChar(gx + i, gy, text[i], color);
        }
    }
}
