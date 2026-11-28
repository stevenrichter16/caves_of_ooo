using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Phase 4d of the World Action Menu feature. Renders a centered popup
    /// listing actions the player can take on the entity at a clicked cell
    /// in look mode or through ordinary interaction.
    ///
    /// Lifecycle:
    ///   Open(actor, target, cell, actions)  — caller sets state, menu
    ///                                          appears on next Render
    ///   HandleInput()                       — call per frame while open
    ///   Cancel / Select                     — sets flags, closes menu
    ///   SelectionMade + SelectedAction      — caller polls, executes, and
    ///                                          calls ConsumeSelection
    ///
    /// Action execution is deliberately NOT done here — the caller (input
    /// handler) fires the <c>InventoryAction</c> event and handles side
    /// effects like opening a dialogue UI after a Chat action. This keeps
    /// the UI component dumb and testable.
    ///
    /// "Pile" note: when the target cell has 2+ non-terrain entities
    /// (<see cref="WorldInteractionSystem.IsPileCell"/>), the caller may
    /// want the Examine action to log the cell-level pile summary instead
    /// of the single target's description. The caller detects this by
    /// checking <see cref="SelectedCellIsPile"/> on a resolved Examine
    /// command.
    /// </summary>
    public class WorldActionMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        internal bool UseLedgerView;
        private LedgerModalView _ledger;
        private readonly List<Rect> _ledgerRows = new List<Rect>();

        private const int MIN_POPUP_W = 48;
        private const int MAX_POPUP_W = CenteredPopupLayout.GridWidth - 2;
        private int _popupW = MIN_POPUP_W;
        private const int POPUP_MAX_VISIBLE = 12;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        // ---- Open state ----
        private bool _isOpen;
        private Entity _target;
        private Cell _cell;
        private string _titleOverride;
        private readonly List<string> _statusLines = new List<string>();

        /// <summary>Status rows render between the title divider and the
        /// action rows; a second divider closes the block. Every layout
        /// consumer (render, border, hit-test, height) derives from HERE
        /// so the four can never disagree.</summary>
        private int StatusBlockRows => _statusLines.Count > 0 ? _statusLines.Count + 1 : 0;
        private int ContentY => 3 + StatusBlockRows;
        /// <summary>Most lines of small description text the menu reserves under its rows.</summary>
        public const int MaxHintRows = 3;
        private int _hintRows;
        /// <summary>Lines reserved for the highlighted row's description (zero when no row has one, so every other menu keeps its shape), plus the divider above them.</summary>
        private int HintBlockRows => _hintRows > 0 ? _hintRows + 1 : 0;
        /// <summary>How many description lines the menu reserves for these rows at this popup width: the most any one of them needs, at most <see cref="MaxHintRows"/>.</summary>
        public static int HintRowsFor(IEnumerable<InventoryAction> actions, int popupWidth)
        {
            int most = 0;
            if (actions != null) foreach (var a in actions) if (a != null && !string.IsNullOrWhiteSpace(a.Hint)) most = Mathf.Max(most, WrapHint(a.Hint, popupWidth - 4, MaxHintRows).Count);
            return most;
        }
        /// <summary>Wraps a hint at word boundaries into at most <paramref name="maxLines"/> lines of at most <paramref name="width"/> characters; text past the last line ends in "~".</summary>
        public static List<string> WrapHint(string text, int width, int maxLines)
        {
            var lines = new List<string>(); if (string.IsNullOrWhiteSpace(text) || width < 4 || maxLines < 1) return lines;
            var line = new System.Text.StringBuilder(); bool cut = false;
            foreach (string raw in text.Split(' '))
            {
                string word = raw; if (word.Length == 0) continue;
                while (word.Length > width) { if (line.Length > 0) { lines.Add(line.ToString()); line.Clear(); } lines.Add(word.Substring(0, width)); word = word.Substring(width); if (lines.Count >= maxLines) { cut = true; break; } }
                if (cut || lines.Count >= maxLines) { cut = true; break; }
                if (line.Length > 0 && line.Length + 1 + word.Length > width) { lines.Add(line.ToString()); line.Clear(); if (lines.Count >= maxLines) { cut = true; break; } }
                if (line.Length > 0) line.Append(' '); line.Append(word);
            }
            if (!cut && line.Length > 0) lines.Add(line.ToString());
            if (lines.Count > maxLines) { lines.RemoveRange(maxLines, lines.Count - maxLines); cut = true; }
            if (cut && lines.Count > 0) { string last = lines[lines.Count - 1]; lines[lines.Count - 1] = (last.Length >= width ? last.Substring(0, width - 1) : last) + "~"; }
            return lines;
        }
        private const int MAX_STATUS_ROWS = 6;
        private bool _cellIsPile;
        private readonly List<InventoryAction> _actions = new List<InventoryAction>();
        private char[] _shortcuts = System.Array.Empty<char>();
        private int _cursorIndex;
        private int _scrollOffset;
        private Vector3 _lastMousePosition;

        // ---- Selection output ----
        private bool _selectionMade;
        private InventoryAction _selectedAction;
        private bool _selectionCancelled;

        // ---- Popup positioning / bg-fill tracking (copied idioms from
        //      ContainerPickerUI so the tilemap clear logic is identical) ----
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _fgDrawn;
        private int _fgDrawnW;
        private int _fgDrawnH;
        private int _fgDrawnOriginX;
        private int _fgDrawnTopY;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        // ---- Public API ----

        public bool IsOpen => _isOpen;
        public bool SelectionMade => _selectionMade;
        public bool SelectionCancelled => _selectionCancelled;
        public InventoryAction SelectedAction => _selectedAction;
        /// <summary>Actual mapped letter that selected this action, never the authored Key.
        /// Mouse/Enter/cancel produce None. Capture before ConsumeSelection resets it.</summary>
        public KeyCode SelectedActivationKey { get; private set; }
        public Entity SelectedTarget => _target;
        public Cell SelectedCell => _cell;
        /// <summary>True only for the whole-pile summary menu. An individual
        /// target may share a pile cell without inheriting summary actions.</summary>
        public bool SelectedCellIsPile => _cellIsPile;

        /// <summary>Lines of small description text the open menu reserves under its rows (zero when none of its rows has one).</summary>
        public int HintRows => _hintRows;
        /// <summary>What the highlighted row says it does, or an empty string.</summary>
        public string HighlightedHint => HighlightedAction?.Hint ?? "";

        public InventoryAction HighlightedAction => _isOpen && _cursorIndex >= 0 && _cursorIndex < _actions.Count ? _actions[_cursorIndex] : null;
        public bool IsTargetPicker => _actions.Count > 0 && _actions.TrueForAll(a => a != null &&
            a.Command != null && a.Command.StartsWith(WorldInteractionSystem.PickTargetCommandPrefix, System.StringComparison.Ordinal));
        public bool HasBackRow => _actions.Exists(a => a != null && a.Command == WorldInteractionSystem.PickCellCommand);

        /// <summary>The heading of the opened group this menu is showing (it ends in a "back" row that names it), or null for an ordinary menu.</summary>
        public string ToolGroupHeading
        {
            get
            {
                foreach (var a in _actions)
                    if (a?.Command != null && a.Command.StartsWith(WorldInteractionSystem.ToolGroupBackPrefix, System.StringComparison.Ordinal)) return WorldInteractionSystem.ToolGroupHeading(a.Command);
                return null;
            }
        }

        /// <summary>The input owner may layer its reader, then rebuild current actions on return.</summary>
        public void HideForReader() => Close();
        public void RestoreHighlight(InventoryAction identity)
        {
            if (!_isOpen || identity == null) return;
            // A tool row is told from another by the item that runs it too: two flasks fill from the same pool with the same command.
            int found = _actions.FindIndex(a => a != null && a.Name == identity.Name &&
                a.Command == identity.Command && a.FireOnActor == identity.FireOnActor && ReferenceEquals(a.Via, identity.Via));
            if (found >= 0) { _cursorIndex = found; ScrollIntoView(); Render(); }
        }

        /// <summary>
        /// Show the menu. <paramref name="actor"/> is accepted for call-site
        /// symmetry but unused (the menu never needed the actor; the dead
        /// field that stored it was removed in the 2026-08 simplification
        /// pass). <paramref name="zone"/> is REQUIRED — the status section
        /// reads tile state through it. Caller is responsible for having run target
        /// resolution + action gathering (via <see cref="WorldInteractionSystem"/>).
        /// An empty/null actions list means the menu renders a single row
        /// reading "(no actions available)" — caller can check emptiness
        /// beforehand and skip opening if they prefer.
        /// A nonblank <paramref name="title"/> displays generic choices instead
        /// of world-object title/status/pile information. Selection is unchanged.
        /// </summary>
        public void Open(Entity actor, Entity target, Cell cell, List<InventoryAction> actions,
            Zone zone, bool isPileSummary = false, string title = null)
        {
            _isOpen = true;
            _target = target;
            _cell = cell;
            _titleOverride = string.IsNullOrWhiteSpace(title) ? null : title;
            _statusLines.Clear();
            if (_titleOverride == null)
                _statusLines.AddRange(BuildStatusLinesFor(target, cell, zone, actor));
            // Physical occupancy cannot distinguish a pile summary from the
            // selected owner's menu after the player chooses an exact target.
            _cellIsPile = _titleOverride == null && isPileSummary;
            _actions.Clear();
            if (actions != null)
                _actions.AddRange(actions);
            _shortcuts = MenuShortcutMap.ForActions(_actions);
            // Size from every choice once so scrolling cannot move the popup.
            // Leave the established row/hotkey layout intact. Dynamic names
            // beyond the overlay still use the explicit overflow marker/F1 reader.
            _popupW = MIN_POPUP_W;
            foreach (var action in _actions)
            {
                string label = action.Display ?? action.Name ?? "";
                _popupW = Mathf.Max(_popupW, Mathf.Min(label.Length, MAX_POPUP_W - 7) + 7);
            }

            _hintRows = HintRowsFor(_actions, _popupW);
            _cursorIndex = 0;
            _scrollOffset = 0;
            ReadPointer(out _lastMousePosition, out _);
            _selectionMade = false;
            _selectionCancelled = false;
            _selectedAction = null;
            SelectedActivationKey = KeyCode.None;
            // DIAG [Phase4d] — confirm Open ran with usable state.
            UnityEngine.Debug.Log($"[ActionMenu:ui-open] actions={_actions.Count} " +
                $"Tilemap={(Tilemap != null ? "set" : "NULL")} " +
                $"BgTilemap={(BgTilemap != null ? "set" : "NULL")}");
            Render();
        }

        /// <summary>
        /// Clear the selection flags after the caller has consumed the
        /// result. Does NOT reset target/actor/cell — those remain readable
        /// until the next Open() call.
        /// </summary>
        public void ConsumeSelection()
        {
            _selectionMade = false;
            _selectionCancelled = false;
            _selectedAction = null;
            SelectedActivationKey = KeyCode.None;
        }

        /// <summary>
        /// Per-frame input processing. Returns after any action is
        /// taken (cursor move / select / cancel) so the caller can
        /// immediately poll SelectionMade / SelectionCancelled.
        /// </summary>
        public void HandleInput()
        {
            if (!_isOpen) return;
            SelectedActivationKey = KeyCode.None;

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            ReadPointer(out Vector3 mousePosition, out bool mousePressed);
            bool mouseMoved = mousePosition != _lastMousePosition;
            _lastMousePosition = mousePosition;
            // A parked pointer must not undo keyboard/controller navigation.
            // Clicks below still resolve their actual row even without movement.
            if (mouseMoved)
            {
                int hoverRow = GetRowAtMouse();
                if (hoverRow >= 0 && hoverRow != _cursorIndex)
                {
                    _cursorIndex = hoverRow;
                    Render();
                }
            }

            if (mousePressed)
            {
                int clickedRow = GetRowAtMouse();
                if (clickedRow >= 0 && clickedRow < _actions.Count)
                {
                    SelectAction(clickedRow);
                    return;
                }
                if (clickedRow < 0)
                {
                    // Click outside menu area: treat as cancel.
                    Cancel();
                    return;
                }
            }

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
                if (_cursorIndex < _actions.Count - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                SelectAction(_cursorIndex);
                return;
            }

            // Match the same resolved binding that the row displays.
            for (int i = 0; i < _actions.Count; i++)
            {
                var key = MenuShortcutMap.Key(_shortcuts[i]);
                if (key != KeyCode.None && InputHelper.GetKeyDown(key))
                {
                    if (!(_actions[i] != null && _actions[i].IsDisabled)) SelectedActivationKey = key;
                    SelectAction(i);
                    return;
                }
            }
        }

        // ---- Selection helpers ----

        private void SelectAction(int index)
        {
            if (index < 0 || index >= _actions.Count) return;
            // A disabled row says why and nothing else: no selection, no turn, no transaction, and the menu stays open.
            if (_actions[index] != null && _actions[index].IsDisabled)
            {
                ReportDisabled(_actions[index]);
                return;
            }
            _selectionMade = true;
            _selectedAction = _actions[index];
            Close();
        }

        /// <summary>Logs a disabled row's reason and records the refusal (spec S3). Shared by Enter, click, hotkey and controller selection.</summary>
        public static void ReportDisabled(InventoryAction action)
        {
            if (action == null) return;
            MessageLog.Add(action.DisabledReason);
            Diag.Record("event", "ActionDisabled", payload: new { code = action.DisabledCode, command = action.Command, name = action.Name });
        }

        private void Cancel()
        {
            _selectionCancelled = true;
            _selectedAction = null;
            SelectedActivationKey = KeyCode.None;
            Close();
        }

        private void Close()
        {
            _ledger?.Hide();
            ClearFgRegion();
            ClearBgRegion();
            _isOpen = false;
            // Actions kept for the caller; cleared on next Open.
        }

        private void OnDisable()
        {
            if (_isOpen) Cancel();
        }

        private void ScrollIntoView()
        {
            int visible = Mathf.Min(_actions.Count, POPUP_MAX_VISIBLE);
            if (visible <= 0) return;

            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visible)
                _scrollOffset = _cursorIndex - visible + 1;
        }

        // ---- Rendering ----
        //
        // Layout (width = _popupW, height = actions + 4 + 1 title line):
        //
        //   ┌──────────────────────────────────────────────┐
        //   │ {title — cell description}                   │
        //   ├──────────────────────────────────────────────┤
        //   │ > a) open                                    │
        //   │   b) examine                                 │
        //   └──────────────────────────────────────────────┘
        //     [Enter]select [Esc]cancel

        /// <summary>
        /// The title bar: what the cell holds, plus how much health the
        /// thing you are pointing at has left.
        ///
        /// <para>The readout is appended only for a single-target menu.
        /// On a pile cell the description is "A pile of items,
        /// including: …", and one member's hitpoints hanging off the end
        /// of that would read as belonging to the list.</para>
        ///
        /// <para>Public and static rather than private so it can be tested
        /// without a Tilemap — <see cref="Render"/> needs Unity objects
        /// this string does not. (Tests are a separate assembly, so
        /// <c>internal</c> would not be visible to them.)</para>
        /// </summary>
        /// <summary>Title stays short — name + [HP]. Status lives in
        /// its own SECTION (see <see cref="BuildStatusLinesFor"/>): the
        /// title-suffix experiment died on first contact with real
        /// content, because "You see Rotwood Herbalist. [HP 14/20]" is
        /// 38 of the 44-char title budget and the affliction group was
        /// silently fit-dropped every time.</summary>
        public static string BuildTitleFor(Cell cell, Entity target)
        {
            string title = WorldInteractionSystem.DescribeCell(cell);
            if (cell != null && WorldInteractionSystem.IsPileCell(cell)) return title;

            string health = HealthReadout.Describe(target);
            return health.Length > 0 ? title + "  [" + health + "]" : title;
        }

        /// <summary>
        /// The status SECTION rendered between the title and the action
        /// rows — the 'c'-menu view of the same CellStatusReadout that
        /// feeds look mode ("one snapshot, three renderers").
        ///
        /// <para><b>Attribution rule:</b> statuses are never pooled. The
        /// target's afflictions render under an "Afflicted:" header (on
        /// pile cells the header carries the target's NAME, because the
        /// pile title doesn't), and the CELL's tile state renders as its
        /// own "On the ground:" line — an NPC's frozen status and the
        /// tile's water can share a cell without sharing a line.</para>
        ///
        /// <para>Capped at <see cref="MAX_STATUS_ROWS"/> rows with an
        /// "...and N more" tail — Examine carries the full
        /// list.</para>
        /// </summary>
        public static List<string> BuildStatusLinesFor(Entity target, Cell cell, Zone zone, Entity viewer = null)
        {
            var lines = new List<string>();
            CellStatusReadout.AppendAfflictionLines(target, lines);

            if (lines.Count > 0 && cell != null && WorldInteractionSystem.IsPileCell(cell)
                && target != null)
            {
                // Pile titles name the pile, not the target — re-own the
                // header so the afflictions cannot be misattributed.
                lines[0] = target.GetDisplayName() + " - afflicted:";
            }

            // A fire or torch always says its fuel and what that is in turns (not on a pile summary, whose lines are not about one thing).
            string fuelLine = null;
            int fuelRows = 0;
            if (CellStatusReadout.TryFireLines(target, out var fireFuel, out var fireTurns))
            {
                fuelLine = null;
                var fireAnchor = zone?.GetEntityCell(target);
                if (zone != null && cell?.ParentZone == zone && cell.Explored && cell.IsVisible
                    && zone.GetCell(cell.X, cell.Y) == cell && target.SpatialZone == zone
                    && fireAnchor?.ParentZone == zone && zone.GetCell(fireAnchor.X, fireAnchor.Y) == fireAnchor
                    && fireAnchor.Objects.Contains(target)
                    && cell.Occupants.Contains(target) && !WorldInteractionSystem.IsPileCell(cell)
                    && target.GetPart<RenderPart>()?.ParentEntity == target && target.GetPart<RenderPart>().Visible
                    && target.GetPart<PhysicsPart>()?.ParentEntity == target
                    && target.GetPart<PhysicsPart>().InInventory == null && target.GetPart<PhysicsPart>().Equipped == null)
                {
                    lines.InsertRange(0, new[] { fireFuel, fireTurns });
                    fuelRows = 2;
                }
            }
            else
            {
                fuelLine = cell != null && WorldInteractionSystem.IsPileCell(cell) ? null : CellStatusReadout.FuelLine(target);
                if (fuelLine != null) { lines.Insert(0, fuelLine); fuelRows = 1; }
            }

            // A station says the live state of what the menu is for (a still's mix and what it would make). Not on a pile summary, and only for a viewer who can act.
            if (viewer != null && target != null && !(cell != null && WorldInteractionSystem.IsPileCell(cell)))
            {
                var station = new List<string>(); StateReader.CollectMenuStatus(target, zone, cell, viewer, station);
                lines.InsertRange(fuelRows, station);
            }

            if (zone != null && cell != null)
            {
                string air = CellStatusReadout.PoisonGasLine(zone, cell);
                if (!string.IsNullOrEmpty(air))
                    lines.Add(air);
                string ground = CellStatusReadout.GroundLine(zone, cell, includeFishActivity: true, includePoisonGas: false);
                if (!string.IsNullOrEmpty(ground))
                    lines.Add(ground);
            }

            if (lines.Count > MAX_STATUS_ROWS)
            {
                int extra = lines.Count - (MAX_STATUS_ROWS - 1);
                lines.RemoveRange(MAX_STATUS_ROWS - 1, extra);
                lines.Add("...and " + extra + " more (see Examine)");
            }
            return lines;
        }

        private string BuildTitle() => _titleOverride ?? BuildTitleFor(_cell, _target);

        private void Render()
        {
            if (UseLedgerView) { RenderLedger(); return; }
            if (Tilemap == null) return;

            ClearFgRegion();
            ClearBgRegion();
            ComputePopupPosition();

            int totalRows = _actions.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4 + StatusBlockRows + HintBlockRows;

            ClearRegion(0, 0, _popupW, _popupH);
            DrawBgFill(0, 0, _popupW, _popupH);
            DrawPopupBorder(0, 0, _popupW, borderH, _statusLines.Count, visibleCount, HintBlockRows);


            // Generic choices supply their own title; world menus describe the cell.
            string title = BuildTitle();
            int maxTitleLen = _popupW - 4;
            if (!string.IsNullOrEmpty(title) && title.Length > maxTitleLen)
                title = title.Substring(0, maxTitleLen - 1) + "~";
            DrawText(2, 1, title, QudColorParser.BrightYellow);

            // Hint row at the bottom-right of title bar
            bool controller = NativeGamepadInput.IsConnected;
            string hint = controller ? "[A]ok [B]back" : "[Enter]ok [Esc]x";
            int hintX = _popupW - hint.Length - 2;
            if (hintX > title.Length + 4)
                DrawText(hintX, 1, hint, QudColorParser.DarkGray);

            // Status section: the target's afflictions + the cell's
            // ground state, between the title divider and the actions
            // (attribution rule: two owners, two labeled groups).
            for (int si = 0; si < _statusLines.Count; si++)
            {
                string line = _statusLines[si];
                int maxLineLen = _popupW - 4;
                if (line.Length > maxLineLen)
                    line = line.Substring(0, maxLineLen - 1) + "~";
                bool isTerrainCondition = line.StartsWith("On the ground:") || line.StartsWith("Air:");
                DrawText(2, 3 + si, line,
                    isTerrainCondition ? QudColorParser.BrightCyan : QudColorParser.Gray);
            }

            int contentY = ContentY;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(no actions available)", QudColorParser.DarkGray);
            }
            else
            {
                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _scrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    bool selected = idx == _cursorIndex;
                    var action = _actions[idx];

                    if (selected)
                        DrawChar(1, rowY, '>', QudColorParser.White);

                    char hotkey = _shortcuts[idx];
                    if (hotkey != '\0')
                    {
                        DrawText(2, rowY, hotkey + ")",
                            selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    string label = action.Display ?? action.Name ?? "";
                    int maxLabelLen = _popupW - 7;
                    if (label.Length > maxLabelLen)
                        label = label.Substring(0, maxLabelLen - 1) + "~";
                    // A disabled row is listed, but dim even when highlighted.
                    DrawText(5, rowY, label,
                        action.IsDisabled ? QudColorParser.DarkGray : selected ? QudColorParser.White : QudColorParser.Gray);
                }

                if (_scrollOffset > 0)
                    DrawChar(_popupW - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(_popupW - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }
            // The highlighted row says what it does, in small text under the list.
            if (_hintRows > 0 && HighlightedAction != null)
            {
                var described = WrapHint(HighlightedAction.Hint, _popupW - 4, _hintRows);
                for (int hi = 0; hi < described.Count; hi++) DrawText(2, contentY + visibleCount + 1 + hi, described[hi], QudColorParser.Gray);
            }
            string footer = controller
                ? (_titleOverride != null ? "[A]select [B]back" : "[A]select [B]back [LT+RT]details")
                : (_titleOverride != null ? "[Enter]ok [Esc]back" : "[F1]details [Enter]ok [Esc]back");
            DrawText(2, borderH, footer, QudColorParser.Gray);
            _fgDrawn = true;
            _fgDrawnW = _popupW;
            _fgDrawnH = _popupH;
            _fgDrawnOriginX = _worldOriginX;
            _fgDrawnTopY = _worldTopY;
        }

        private void RenderLedger()
        {
            const int width = 940, line = 34, step = 39;
            int count = Mathf.Min(_actions.Count, POPUP_MAX_VISIBLE);
            int statusHeight = _statusLines.Count == 0 ? 0 : _statusLines.Count * 20 + 18;
            int hintLines = Mathf.Max(_hintRows, _actions.Exists(a => a?.IsDisabled == true) ? 2 : 0);
            int hintHeight = hintLines == 0 ? 0 : 24 + hintLines * 23;
            int rangeHeight = _actions.Count > count ? 24 : 0;
            int height = 78 + statusHeight + Mathf.Max(1, count) * step + rangeHeight + hintHeight + 48;
            _ledger ??= LedgerModalView.Create(transform, "WorldActionLedger");
            _ledger.Begin(PopupCamera, width, height);
            bool details = _titleOverride == null || ToolGroupHeading != null;
            string footer = NativeGamepadInput.IsConnected ? "A Select    B Back" : "Enter Select    Esc Back";
            if (details) footer += NativeGamepadInput.IsConnected ? "    LT + RT Details" : "    F1 Details";
            _ledger.Card(BuildTitle(), footer);

            int y = 74;
            if (statusHeight > 0)
            {
                _ledger.Box(new Rect(24, y, width - 48, statusHeight - 7), LPanel);
                y += 5;
                foreach (string status in _statusLines)
                {
                    var color = status.StartsWith("On the ground:") || status.StartsWith("Air:") ? LGreen : LMuted;
                    _ledger.Label(new Rect(36, y, width - 72, 20), status, color, 16, fit: true);
                    y += 20;
                }
                y += 13;
            }

            _ledgerRows.Clear();
            if (count == 0)
            {
                _ledger.Label(new Rect(28, y, width - 56, step), "No actions available", LMuted);
                y += step;
            }
            for (int i = 0; i < count && _scrollOffset + i < _actions.Count; i++)
            {
                int index = _scrollOffset + i;
                var action = _actions[index];
                var row = new Rect(24, y, width - 48, line);
                _ledgerRows.Add(row);
                string key = _shortcuts[index] == '\0' || NativeGamepadInput.IsConnected ? null : _shortcuts[index].ToString();
                bool disabled = action?.IsDisabled == true;
                _ledger.Row(row, action?.Display ?? action?.Name, index == _cursorIndex,
                    key, disabled ? "Unavailable" : null, disabled);
                y += step;
            }
            if (rangeHeight > 0)
            {
                _ledger.Label(new Rect(24, y, width - 48, 20),
                    $"{_scrollOffset + 1}–{Mathf.Min(_scrollOffset + count, _actions.Count)} of {_actions.Count}",
                    LMuted, 15, TextAnchor.MiddleRight);
                y += rangeHeight;
            }
            if (hintHeight > 0 && HighlightedAction is { } selected)
            {
                string hint = selected.IsDisabled ? selected.DisabledReason : selected.Hint;
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    _ledger.Box(new Rect(24, y + 2, width - 48, hintHeight - 3), LPanel);
                    _ledger.Label(new Rect(36, y + 7, width - 72, 21),
                        selected.IsDisabled ? "Unavailable" : "Action details", selected.IsDisabled ? LRed : LGold, 15, bold: true);
                    _ledger.Label(new Rect(36, y + 30, width - 72, hintHeight - 30), hint,
                        LInk, 18, TextAnchor.UpperLeft, wrap: true, fit: true);
                }
            }
            _ledger.End();
        }

        private void ComputePopupPosition()
        {
            int totalRows = _actions.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 5 + StatusBlockRows + HintBlockRows;
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(_popupW);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        // Keep position and edge on the same backend, as inventory/hotbar do.
        // Legacy coordinates can remain stale while native clicks are delivered.
        private static void ReadPointer(out Vector3 position, out bool pressed)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.enabled)
            {
                position = mouse.position.ReadValue();
                pressed = mouse.leftButton.wasPressedThisFrame;
                return;
            }
            position = Input.mousePosition;
            pressed = Input.GetMouseButtonDown(0);
        }

        private int GetRowAtMouse()
        {
            // HandleInput captures this pointer once before hover/click dispatch.
            if (UseLedgerView)
            {
                if (_ledger == null || !_ledger.TryPoint(_lastMousePosition, out var point)) return -1;
                for (int i = 0; i < _ledgerRows.Count; i++)
                    if (_ledgerRows[i].Contains(point)) return _scrollOffset + i;
                return -1;
            }
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, _lastMousePosition,
                    out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            int totalRows = _actions.Count;
            int visibleCount = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            int contentY = ContentY;

            if (gx > 0 && gx < _popupW - 1 && gy >= contentY && gy < contentY + visibleCount)
            {
                int vi = gy - contentY;
                int rowIdx = _scrollOffset + vi;
                if (rowIdx < totalRows)
                    return rowIdx;
            }

            return -1;
        }

        // ---- Tilemap primitives (identical to ContainerPickerUI) ----

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

        // Clear where the previous menu was actually drawn, before a new
        // width/height recenters it. This also works without optional backing.
        private void ClearFgRegion()
        {
            if (!_fgDrawn || Tilemap == null) return;
            for (int y = 0; y < _fgDrawnH; y++)
                for (int x = 0; x < _fgDrawnW; x++)
                    Tilemap.SetTile(new Vector3Int(_fgDrawnOriginX + x, _fgDrawnTopY - y, 0), null);
            _fgDrawn = false;
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

        private void DrawPopupBorder(int x, int y, int w, int h, int statusRows, int contentRows, int hintBlockRows = 0)
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

            int rowY = y + 3;
            for (int r = 0; r < statusRows; r++, rowY++)
            {
                DrawChar(x, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }
            if (statusRows > 0)
            {
                // Second divider closes the status block.
                DrawChar(x, rowY, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
                for (int i = 1; i < w - 1; i++)
                    DrawChar(x + i, rowY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
                DrawChar(x + w - 1, rowY, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);
                rowY++;
            }

            for (int r = 0; r < contentRows; r++, rowY++)
            {
                DrawChar(x, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            if (hintBlockRows > 0)
            {
                // A divider, then the lines the highlighted row's description is written on.
                DrawChar(x, rowY, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
                for (int i = 1; i < w - 1; i++)
                    DrawChar(x + i, rowY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
                DrawChar(x + w - 1, rowY, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);
                rowY++;
                for (int r = 1; r < hintBlockRows; r++, rowY++)
                {
                    DrawChar(x, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                    DrawChar(x + w - 1, rowY, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                }
            }

            int botY = rowY;
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
            {
                char c = text[i];
                if (c == ' ') continue;
                DrawChar(gx + i, gy, c, color);
            }
        }
    }
}
