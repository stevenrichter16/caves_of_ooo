using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Full-screen trade UI. Two-panel layout:
    /// Left panel: NPC items with buy prices.
    /// Right panel: Player items with sell prices.
    /// Follows InventoryUI's 80x45 grid pattern.
    ///
    /// <para>EC6c (Docs/ECONOMY-SYSTEM.md §12.3): deal mode. A barter trader (<see cref="Tender"/>) always
    /// opens in deal mode; a dram trader opens in the per-item shop and <c>[D]</c> switches. In deal mode the
    /// player marks units on both sides, row 41 shows the balance, <c>[S]</c> balances with salt and <c>[O]</c>
    /// offers the whole deal in one transaction (<see cref="Barter.TryDeal"/>).</para>
    /// </summary>
    public class TradeUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private const int W = 80;
        private const int H = 45;
        private const int DIVIDER_X = 39;
        private const int LEFT_START = 1;
        private const int LEFT_W = DIVIDER_X - 1; // 38
        private const int RIGHT_START = DIVIDER_X + 1; // 40
        private const int RIGHT_W = W - RIGHT_START - 1; // 39
        private const int CONTENT_TOP = 3;
        private const int CONTENT_BOTTOM = 41; // exclusive
        private const int VISIBLE_ROWS = CONTENT_BOTTOM - CONTENT_TOP; // 38

        private bool _isOpen;
        public bool IsOpen => _isOpen;
        public string SelectedDecisionDetails()
        {
            var rows = _panel == 0 ? _leftRows : _rightRows; int cursor = _panel == 0 ? _leftCursor : _rightCursor;
            return _isOpen && !_confirmActive && cursor >= 0 && cursor < rows.Count
                ? InventoryDecisionDetails.Trade(PlayerEntity, _trader, rows[cursor].Item, _panel == 0) : null;
        }
        public void HideForDecisionReader() { if (Tilemap != null) Tilemap.ClearAllTiles(); }
        public void RestoreAfterDecisionReader() { if (_isOpen) { Rebuild(); Render(); } }


        private Entity _trader;
        private double _performance;

        // EC6c: the deal being composed (null in the per-item shop) and what the trader accepts.
        private BarterDeal _deal;
        private TenderKind _tender;
        private bool _confirmDeal;
        public bool IsDealMode => _isOpen && _deal != null;
        public BarterDeal Deal => _deal;
        public string StatusMessage => _statusMessage;
        private static readonly KeyCode[] DealKeys =
        {
            KeyCode.Return, KeyCode.RightArrow, KeyCode.L, KeyCode.LeftArrow, KeyCode.H,
            KeyCode.A, KeyCode.S, KeyCode.O, KeyCode.D
        };

        // 0 = left (buy from trader), 1 = right (sell to trader)
        private int _panel;
        private int _leftCursor, _leftScroll;
        private int _rightCursor, _rightScroll;

        private List<TradeRow> _leftRows = new List<TradeRow>();
        private List<TradeRow> _rightRows = new List<TradeRow>();

        // Confirmation popup state
        private bool _confirmActive;
        private int _confirmPanel;   // which panel the pending trade is on
        private int _confirmIndex;   // cursor index of the item
        private int _confirmChoice;  // 0 = Yes, 1 = No

        private struct TradeRow
        {
            public Entity Item;
            public string Name;
            public int Price;
            public int Weight;
        }

        public void Open(Entity trader)
        {
            if (PlayerEntity == null || trader == null) return;
            _trader = trader;
            _isOpen = true;
            _panel = 0;
            _statusMessage = null;
            _confirmActive = false;
            _tender = Tender.For(trader);
            _deal = _tender == TenderKind.Barter ? new BarterDeal(PlayerEntity, trader) : null;
            _leftCursor = _leftScroll = 0;
            _rightCursor = _rightScroll = 0;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            if (Tilemap != null)
            {
                Tilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
            }
        }

        // ===== Data =====

        private void Rebuild()
        {
            _leftRows.Clear();
            _rightRows.Clear();
            _performance = TradeSystem.GetTradePerformance(PlayerEntity);

            // Left panel: trader stock (buy prices)
            var stock = TradeSystem.GetTraderStock(_trader);
            for (int i = 0; i < stock.Count; i++)
            {
                int price = TradeSystem.GetBuyPrice(stock[i], _performance, _trader);
                _leftRows.Add(BuildRow(stock[i], price));
            }

            // Right panel: player sellable items
            var playerInv = PlayerEntity.GetPart<InventoryPart>();
            if (playerInv != null)
            {
                for (int i = 0; i < playerInv.Objects.Count; i++)
                {
                    var item = playerInv.Objects[i];
                    if (item.GetPart<CommercePart>() != null)
                    {
                        int price = TradeSystem.GetSellPrice(item, _performance, _trader);
                        _rightRows.Add(BuildRow(item, price));
                    }
                }
            }

            ClampCursor(ref _leftCursor, ref _leftScroll, _leftRows.Count);
            ClampCursor(ref _rightCursor, ref _rightScroll, _rightRows.Count);
        }

        private TradeRow BuildRow(Entity item, int price)
        {
            var physics = item.GetPart<PhysicsPart>();
            return new TradeRow
            {
                Item = item,
                Name = item.GetDisplayName(),
                Price = price,
                Weight = InventoryPart.GetItemWeight(item)
            };
        }

        private void ClampCursor(ref int cursor, ref int scroll, int count)
        {
            if (count == 0) { cursor = 0; scroll = 0; return; }
            if (cursor >= count) cursor = count - 1;
            if (cursor < 0) cursor = 0;
            if (cursor < scroll) scroll = cursor;
            if (cursor >= scroll + VISIBLE_ROWS) scroll = cursor - VISIBLE_ROWS + 1;
        }

        // ===== Input =====

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (_confirmActive)
            {
                HandleConfirmInput();
                return;
            }

            UpdateMouseHover();

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            // Tab to switch panels
            if (InputHelper.GetKeyDown(KeyCode.Tab))
            {
                _panel = 1 - _panel;
                Render();
                return;
            }

            // Navigation
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                MoveCursor(-1);
                Render();
                return;
            }
            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            {
                MoveCursor(1);
                Render();
                return;
            }

            // EC6c: deal mode owns Enter, the side arrows and its letter keys.
            if (_deal != null)
            {
                for (int i = 0; i < DealKeys.Length; i++)
                    if (InputHelper.GetKeyDown(DealKeys[i])) { DealKey(DealKeys[i]); return; }
                if (Input.GetMouseButtonDown(0))
                {
                    var at = MouseToGrid();
                    if (at.x >= 0)
                    {
                        _panel = at.x < DIVIDER_X ? 0 : 1;
                        int row = GetItemRowAtGrid(at);
                        if (row >= 0) { SetCursor(row); DealKey(KeyCode.Return); return; }
                        Render();
                    }
                }
                return;
            }
            if (_tender == TenderKind.Drams && InputHelper.GetKeyDown(KeyCode.D)) { DealKey(KeyCode.D); return; }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                var grid = MouseToGrid();
                if (grid.x >= 0)
                {
                    // Determine which panel was clicked
                    int clickPanel = grid.x < DIVIDER_X ? 0 : 1;
                    if (clickPanel != _panel)
                    {
                        _panel = clickPanel;
                        Render();
                    }

                    int clickedRow = GetItemRowAtGrid(grid);
                    if (clickedRow >= 0)
                    {
                        SetCursor(clickedRow);
                        ShowConfirmation();
                        return;
                    }
                }
            }

            // Enter to buy/sell
            if (InputHelper.GetKeyDown(KeyCode.Return))
            {
                ShowConfirmation();
                return;
            }
        }

        /// <summary>EC6c: one deal-mode key (the input seam; public so tests can drive the screen without a
        /// keyboard). Enter/→/L add one unit of the selected row, ←/H remove one, A toggles the whole stack,
        /// S balances with salt, O offers the deal, D toggles deal mode at a dram trader. Returns whether the key
        /// meant anything here.</summary>
        public bool DealKey(KeyCode key)
        {
            if (!_isOpen || _confirmActive) return false;
            if (key == KeyCode.D)
            {
                if (_tender != TenderKind.Drams) return false;
                _deal = _deal == null ? new BarterDeal(PlayerEntity, _trader) : null;
                _statusMessage = null; Render(); return true;
            }
            if (_deal == null) return false;
            _statusMessage = null;
            switch (key)
            {
                case KeyCode.Return: case KeyCode.RightArrow: case KeyCode.L: AdjustSelected(+1, false); break;
                case KeyCode.LeftArrow: case KeyCode.H: AdjustSelected(-1, false); break;
                case KeyCode.A: AdjustSelected(0, true); break;
                case KeyCode.S:
                    if (_deal.BalanceWithSalt() == 0 && _deal.Balance < 0) _statusMessage = "There is no salt to balance with.";
                    break;
                case KeyCode.O:
                    string refusal = _deal.Refusal();
                    if (refusal != null) _statusMessage = Barter.Explain(refusal);
                    else { _confirmActive = true; _confirmDeal = true; _confirmChoice = 0; }
                    break;
                default: return false;
            }
            Render();
            return true;
        }

        private void AdjustSelected(int delta, bool toggleAll)
        {
            var rows = _panel == 0 ? _leftRows : _rightRows;
            int cursor = _panel == 0 ? _leftCursor : _rightCursor;
            if (rows.Count == 0 || cursor < 0 || cursor >= rows.Count) return;
            var item = rows[cursor].Item;
            int now = _panel == 0 ? _deal.Taking(item) : _deal.Giving(item);
            int units = BarterDeal.Units(item);
            int next = toggleAll ? (now < units ? units : 0) : now + delta;
            bool changed = _panel == 0 ? _deal.SetTake(item, next) : _deal.SetGive(item, next);
            if (!changed && next > now) _statusMessage = $"You can't trade {item.GetDisplayName()}.";
        }

        private void MoveCursor(int delta)
        {
            if (_panel == 0)
            {
                _leftCursor += delta;
                ClampCursor(ref _leftCursor, ref _leftScroll, _leftRows.Count);
            }
            else
            {
                _rightCursor += delta;
                ClampCursor(ref _rightCursor, ref _rightScroll, _rightRows.Count);
            }
        }

        private void SetCursor(int index)
        {
            _statusMessage = null; // stale failure text clears on move
            if (_panel == 0)
            {
                _leftCursor = index;
                ClampCursor(ref _leftCursor, ref _leftScroll, _leftRows.Count);
            }
            else
            {
                _rightCursor = index;
                ClampCursor(ref _rightCursor, ref _rightScroll, _rightRows.Count);
            }
        }

        private void ShowConfirmation()
        {
            var rows = _panel == 0 ? _leftRows : _rightRows;
            int cursor = _panel == 0 ? _leftCursor : _rightCursor;
            if (rows.Count == 0 || cursor < 0 || cursor >= rows.Count) return;

            _confirmActive = true;
            _confirmPanel = _panel;
            _confirmIndex = cursor;
            _confirmChoice = 0; // default to Yes
            Render();
        }

        private void HandleConfirmInput()
        {
            // Mouse click on Yes/No buttons
            if (Input.GetMouseButtonDown(0))
            {
                var grid = MouseToGrid();
                if (grid.x >= 0)
                {
                    int popupRow = GetConfirmButtonRow(grid);
                    if (popupRow == 0) // Yes
                    {
                        _confirmChoice = 0;
                        ConfirmTrade();
                        return;
                    }
                    else if (popupRow == 1) // No
                    {
                        CancelConfirmation();
                        return;
                    }
                }
            }

            if (InputHelper.GetKeyDown(KeyCode.Escape) || InputHelper.GetKeyDown(KeyCode.N))
            {
                CancelConfirmation();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Y))
            {
                if (_confirmChoice == 0)
                    ConfirmTrade();
                else
                    CancelConfirmation();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)
                || InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            {
                _confirmChoice = 1 - _confirmChoice;
                Render();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H)
                || InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L))
            {
                _confirmChoice = 1 - _confirmChoice;
                Render();
                return;
            }
        }

        private int GetConfirmButtonRow(Vector2Int grid)
        {
            // Popup is centered: 40 wide, positioned at row 16-28 area
            int popupX = (W - 40) / 2;
            int popupY = (H - 11) / 2;
            int yesRow = popupY + 7;
            int noRow = popupY + 8;

            if (grid.x < popupX + 2 || grid.x >= popupX + 38) return -1;
            if (grid.y == yesRow) return 0;
            if (grid.y == noRow) return 1;
            return -1;
        }

        private void ConfirmTrade()
        {
            _confirmActive = false;
            if (_confirmDeal) { _confirmDeal = false; ExecuteDeal(); return; }
            ExecuteTrade();
        }

        private void CancelConfirmation()
        {
            _confirmActive = false;
            _confirmDeal = false;
            Render();
        }

        /// <summary>EC6c: offer the composed deal. Success starts a fresh deal; a refusal keeps the deal and
        /// shows its reason on the screen.</summary>
        private void ExecuteDeal()
        {
            if (_deal == null) return;
            bool ok = Barter.TryDeal(_deal, out string reason);
            _statusMessage = ok ? null : reason;
            if (ok) _deal = new BarterDeal(PlayerEntity, _trader);
            Rebuild();
            Render();
        }

        /// <summary>Confirm the pending deal popup, as [Y] would (tests drive the screen through this).</summary>
        public void ConfirmPendingDeal()
        {
            if (_confirmActive && _confirmDeal) ConfirmTrade();
        }

        // BETA AUDIT 🔴 #8 — TradeSystem's failure messages go to the
        // MESSAGE LOG, which is invisible behind this fullscreen UI:
        // a failed buy looked like a dead Enter key. Mirror
        // PickupUI._statusMessage: capture the result, show the reason
        // on the trade screen itself.
        private string _statusMessage;

        private void ExecuteTrade()
        {
            bool ok;
            if (_panel == 0)
            {
                if (_leftRows.Count == 0) return;
                ok = TradeSystem.BuyFromTrader(PlayerEntity, _trader, _leftRows[_leftCursor].Item, out string reason);
                _statusMessage = ok ? null : reason;
            }
            else
            {
                if (_rightRows.Count == 0) return;
                ok = TradeSystem.SellToTrader(PlayerEntity, _trader, _rightRows[_rightCursor].Item, out string reason);
                _statusMessage = ok ? null : reason;
            }
            Rebuild();
            Render();
        }

        // ===== Mouse =====

        private void UpdateMouseHover()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return;

            // Determine panel from mouse X
            int hoverPanel = grid.x < DIVIDER_X ? 0 : 1;
            int row = GetItemRowAtGrid(grid);
            if (row < 0) return;

            if (hoverPanel != _panel)
            {
                _panel = hoverPanel;
                SetCursor(row);
                Render();
            }
            else
            {
                int currentCursor = _panel == 0 ? _leftCursor : _rightCursor;
                if (row != currentCursor)
                {
                    SetCursor(row);
                    Render();
                }
            }
        }

        private int GetItemRowAtGrid(Vector2Int grid)
        {
            if (grid.y < CONTENT_TOP || grid.y >= CONTENT_BOTTOM)
                return -1;

            int visualRow = grid.y - CONTENT_TOP;
            int scroll = _panel == 0 ? _leftScroll : _rightScroll;
            int count = _panel == 0 ? _leftRows.Count : _rightRows.Count;
            int index = scroll + visualRow;

            if (index < 0 || index >= count)
                return -1;
            return index;
        }

        private Vector2Int MouseToGrid()
        {
            var cam = Camera.main;
            if (cam == null) return new Vector2Int(-1, -1);

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            int tx = Mathf.FloorToInt(world.x);
            int ty = Mathf.FloorToInt(world.y);
            int gx = tx;
            int gy = H - 1 - ty;

            if (gx < 0 || gx >= W || gy < 0 || gy >= H)
                return new Vector2Int(-1, -1);
            return new Vector2Int(gx, gy);
        }

        // ===== Rendering =====

        private void Render()
        {
            using (PerformanceMarkers.Ui.TradeRender.Auto())
            {
                PerformanceDiagnostics.RecordTradeRender();
                if (Tilemap == null)
                    return;

                Tilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();

                int playerDrams = TradeSystem.GetDrams(PlayerEntity);
                int traderDrams = TradeSystem.GetDrams(_trader);
                int perfPct = (int)(_performance * 100);

                // Row 0: panel titles
                string traderName = _trader != null ? _trader.GetDisplayName() : "Trader";
                bool barter = _deal != null && _tender == TenderKind.Barter;
                string leftTitle = barter ? "Barter with " + traderName : _deal != null ? "Deal with " + traderName : "Buy from " + traderName;
                string rightTitle = _deal != null ? "Your pack" : "Sell your items";
                DrawText(LEFT_START, 0, leftTitle, _panel == 0 ? QudColorParser.White : QudColorParser.Gray);
                DrawText(RIGHT_START, 0, rightTitle, _panel == 1 ? QudColorParser.White : QudColorParser.Gray);

                // Row 1: drams and performance (EC6: a barter trader takes no drams, so none are shown)
                if (barter)
                {
                    DrawText(LEFT_START, 1, "They take goods and salt, not drams.", QudColorParser.BrightYellow);
                    DrawText(RIGHT_START, 1, "Perf:" + perfPct + "%", QudColorParser.BrightYellow);
                }
                else
                {
                    DrawText(LEFT_START, 1, "Trader: $" + traderDrams, QudColorParser.BrightYellow);
                    string playerInfo = "You: $" + playerDrams + "  Perf:" + perfPct + "%";
                    DrawText(RIGHT_START, 1, playerInfo, QudColorParser.BrightYellow);
                }

                // Row 2: separator
                DrawHLine(0, 2, W);

                // Vertical divider: the panels only. Below them the deal, detail and action lines span the
                // full width (EC6c found the divider painted through their spaces at column 39).
                for (int y = 0; y < CONTENT_BOTTOM; y++)
                    DrawChar(DIVIDER_X, y, '|', QudColorParser.DarkGray);

                // Left panel: trader items
                RenderPanel(_leftRows, LEFT_START, _leftCursor, _leftScroll, _panel == 0, LEFT_W, true);

                // Right panel: player items
                RenderPanel(_rightRows, RIGHT_START, _rightCursor, _rightScroll, _panel == 1, RIGHT_W, false);

                // Row 41: the deal's balance (EC6c)
                if (_deal != null) DrawText(1, CONTENT_BOTTOM, DealSummary(out Color summaryColor), summaryColor);

                // Row 42: separator
                DrawHLine(0, CONTENT_BOTTOM + 1, W);

                // Row 43: detail line for selected item
                RenderDetailLine();

                // Row 44: action bar — or the trade-failure reason
                // (audit #8: log messages are invisible behind this UI).
                if (!string.IsNullOrEmpty(_statusMessage))
                {
                    DrawText(0, H - 1, " " + _statusMessage, QudColorParser.BrightRed);
                }
                else if (_deal != null)
                {
                    string actionBar = " [Enter/>]add [<]remove [A]all [S]salt [O]offer [Tab]side"
                        + (_tender == TenderKind.Drams ? " [D]shop" : "") + " [Esc]close";
                    DrawText(0, H - 1, actionBar, QudColorParser.DarkGray);
                }
                else
                {
                    string actionBar = _panel == 0
                        ? " [Enter]buy  [Tab]sell panel  [D]deal  [Esc]close"
                        : " [Enter]sell  [Tab]buy panel  [D]deal  [Esc]close";
                    DrawText(0, H - 1, actionBar, QudColorParser.DarkGray);
                }

                // Confirmation popup overlay
                if (_confirmActive && _confirmDeal)
                    RenderDealPopup();
                else if (_confirmActive)
                    RenderConfirmPopup();
            }
        }

        /// <summary>EC6c: the deal line on row 41.</summary>
        public string DealSummary(out Color color)
        {
            color = QudColorParser.White;
            if (_deal == null) return "";
            int give = _deal.GiveValue, take = _deal.TakeValue, balance = give - take;
            string text = "Give " + give + "   Take " + take + "   ";
            if (_deal.IsEmpty) { color = QudColorParser.Gray; return text + "Mark goods on either side."; }
            if (_tender == TenderKind.Barter)
            {
                if (balance < 0) { color = QudColorParser.BrightRed; return text + "Short " + (-balance) + ": add goods or salt."; }
                if (balance > 0) { color = QudColorParser.BrightYellow; return text + "They keep " + balance + " over."; }
                color = QudColorParser.BrightGreen; return text + "Even.";
            }
            if (balance < 0) { color = _deal.Refusal() == null ? QudColorParser.BrightYellow : QudColorParser.BrightRed; return text + "You pay " + (-balance) + " drams."; }
            if (balance > 0) { color = _deal.Refusal() == null ? QudColorParser.BrightYellow : QudColorParser.BrightRed; return text + "You receive " + balance + " drams."; }
            color = QudColorParser.BrightGreen; return text + "Even.";
        }

        private void RenderPanel(List<TradeRow> rows, int x0, int cursor, int scroll,
            bool active, int panelW, bool isBuyPanel)
        {
            if (rows.Count == 0)
            {
                string emptyMsg = isBuyPanel ? "(nothing for sale)" : "(nothing to sell)";
                DrawText(x0 + 2, CONTENT_TOP, emptyMsg, QudColorParser.DarkGray);
                return;
            }

            int y = CONTENT_TOP;
            for (int i = scroll; i < rows.Count && y < CONTENT_BOTTOM; i++, y++)
            {
                bool selected = active && (i == cursor);
                Color textColor = selected ? QudColorParser.White : QudColorParser.Gray;

                if (selected)
                    DrawChar(x0 - 1, y, '>', QudColorParser.White);

                // Build row text: "  name            4lb   29$"
                string name = rows[i].Name;
                int maxNameLen = panelW - 14; // room for weight + price
                if (_deal != null)
                {
                    // EC6c: a marked row shows its count in the deal ("2/9", or "*" for a single item).
                    int inDeal = isBuyPanel ? _deal.Taking(rows[i].Item) : _deal.Giving(rows[i].Item);
                    int units = BarterDeal.Units(rows[i].Item);
                    string mark = inDeal == 0 ? "" : units > 1 ? inDeal + "/" + units : "*";
                    name = mark.PadRight(6) + name;
                    if (inDeal > 0) textColor = selected ? QudColorParser.BrightGreen : QudColorParser.DarkGreen;
                }
                if (name.Length > maxNameLen)
                    name = name.Substring(0, maxNameLen - 1) + "~";

                string line = name.PadRight(maxNameLen);
                line += rows[i].Weight.ToString().PadLeft(4) + "lb ";
                // EC6c (found in live play): a barter trader takes no drams, so its values carry no "$".
                line += rows[i].Price.ToString().PadLeft(5) + (_deal != null && _tender == TenderKind.Barter ? " " : "$");

                DrawText(x0 + 1, y, line, textColor);
            }

            // Scroll indicators
            if (scroll > 0)
                DrawChar(x0 + panelW - 1, CONTENT_TOP, '^', QudColorParser.Gray);
            if (scroll + VISIBLE_ROWS < rows.Count)
                DrawChar(x0 + panelW - 1, CONTENT_BOTTOM - 1, 'v', QudColorParser.Gray);
        }

        private void RenderDetailLine()
        {
            var rows = _panel == 0 ? _leftRows : _rightRows;
            int cursor = _panel == 0 ? _leftCursor : _rightCursor;
            if (rows.Count == 0 || cursor < 0 || cursor >= rows.Count) return;

            var row = rows[cursor];
            int baseValue = TradeSystem.GetItemValue(row.Item);
            string action = _panel == 0 ? "Buy" : "Sell";
            string detail = $" [F1]details  {row.Name}  Wt:{row.Weight}  {action} stack:{row.Price}$";
            if (_deal != null)
            {
                int inDeal = _panel == 0 ? _deal.Taking(row.Item) : _deal.Giving(row.Item);
                int units = BarterDeal.Units(row.Item);
                int each = _panel == 0 ? TradeSystem.GetBuyPrice(row.Item, _performance, _trader, 1)
                    : TradeSystem.GetSellPrice(row.Item, _performance, _trader, 1);
                detail = $" [F1]details  {row.Name}  in deal {inDeal} of {units}  each {each}";
            }
            if (detail.Length > W)
                detail = detail.Substring(0, W);
            DrawText(0, H - 2, detail, QudColorParser.BrightCyan);
        }

        private void RenderConfirmPopup()
        {
            var rows = _confirmPanel == 0 ? _leftRows : _rightRows;
            if (_confirmIndex < 0 || _confirmIndex >= rows.Count)
            {
                _confirmActive = false;
                return;
            }

            var row = rows[_confirmIndex];
            string action = _confirmPanel == 0 ? "Buy" : "Sell";
            string itemName = row.Name ?? "item";
            string priceSuffix = " for " + row.Price + "$?";

            // Popup dimensions
            int popupW = 40;
            int popupH = 11;
            int popupX = (W - popupW) / 2;
            int popupY = (H - popupH) / 2;

            // Keep the committed whole-stack price visible even for a long item name.
            int nameBudget = popupW - 4 - action.Length - 1 - priceSuffix.Length;
            if (itemName.Length > nameBudget) itemName = itemName.Substring(0, nameBudget - 1) + "~";
            string priceLine = action + " " + itemName + priceSuffix;

            // Clear popup region
            for (int py = popupY; py < popupY + popupH; py++)
            {
                for (int px = popupX; px < popupX + popupW; px++)
                {
                    var tilePos = new Vector3Int(px, H - 1 - py, 0);
                    Tilemap.SetTile(tilePos, CP437TilesetGenerator.GetUiTile(' '));
                    Tilemap.SetTileFlags(tilePos, TileFlags.None);
                    Tilemap.SetColor(tilePos, QudColorParser.Black);
                }
            }

            // Border
            for (int px = popupX; px < popupX + popupW; px++)
            {
                DrawChar(px, popupY, '-', QudColorParser.White);
                DrawChar(px, popupY + popupH - 1, '-', QudColorParser.White);
            }
            for (int py = popupY; py < popupY + popupH; py++)
            {
                DrawChar(popupX, py, '|', QudColorParser.White);
                DrawChar(popupX + popupW - 1, py, '|', QudColorParser.White);
            }
            DrawChar(popupX, popupY, '+', QudColorParser.White);
            DrawChar(popupX + popupW - 1, popupY, '+', QudColorParser.White);
            DrawChar(popupX, popupY + popupH - 1, '+', QudColorParser.White);
            DrawChar(popupX + popupW - 1, popupY + popupH - 1, '+', QudColorParser.White);

            // Title
            string title = "Confirm " + action;
            int titleX = popupX + (popupW - title.Length) / 2;
            DrawText(titleX, popupY + 1, title, QudColorParser.BrightYellow);

            // Separator
            for (int px = popupX + 1; px < popupX + popupW - 1; px++)
                DrawChar(px, popupY + 2, '-', QudColorParser.DarkGray);

            // Item info
            int infoX = popupX + 2;
            DrawText(infoX, popupY + 3, priceLine, QudColorParser.White);
            int count = row.Item.GetPart<StackerPart>()?.StackCount ?? 1;
            long afterDrams = (long)TradeSystem.GetDrams(PlayerEntity) + (_confirmPanel == 0 ? -row.Price : row.Price);
            long afterWeight = (long)(PlayerEntity.GetPart<InventoryPart>()?.GetCarriedWeight() ?? 0)
                + (_confirmPanel == 0 ? 1 : -1) * InventoryPart.GetItemWeight(row.Item);
            DrawText(infoX, popupY + 4, count + " units; drams after: " + afterDrams, QudColorParser.Gray);
            DrawText(infoX, popupY + 5, "Pack weight after: " + afterWeight, QudColorParser.Gray);

            // Yes / No choices
            int choiceY = popupY + 7;
            Color yesColor = _confirmChoice == 0 ? QudColorParser.White : QudColorParser.Gray;
            Color noColor = _confirmChoice == 1 ? QudColorParser.White : QudColorParser.Gray;

            if (_confirmChoice == 0)
                DrawChar(infoX, choiceY, '>', QudColorParser.White);
            DrawText(infoX + 2, choiceY, "[Y] Yes", yesColor);

            if (_confirmChoice == 1)
                DrawChar(infoX, choiceY + 1, '>', QudColorParser.White);
            DrawText(infoX + 2, choiceY + 1, "[N] No", noColor);
        }

        /// <summary>EC6c: the offer popup: both sides' values and the settlement, then Yes/No (same geometry
        /// as the per-item popup, so <see cref="GetConfirmButtonRow"/> serves both).</summary>
        private void RenderDealPopup()
        {
            int popupW = 40, popupH = 11;
            int popupX = (W - popupW) / 2, popupY = (H - popupH) / 2;
            for (int py = popupY; py < popupY + popupH; py++)
                for (int px = popupX; px < popupX + popupW; px++)
                {
                    var tilePos = new Vector3Int(px, H - 1 - py, 0);
                    Tilemap.SetTile(tilePos, CP437TilesetGenerator.GetUiTile(' '));
                    Tilemap.SetTileFlags(tilePos, TileFlags.None);
                    Tilemap.SetColor(tilePos, QudColorParser.Black);
                }
            for (int px = popupX; px < popupX + popupW; px++)
            {
                DrawChar(px, popupY, '-', QudColorParser.White);
                DrawChar(px, popupY + popupH - 1, '-', QudColorParser.White);
            }
            for (int py = popupY; py < popupY + popupH; py++)
            {
                DrawChar(popupX, py, '|', QudColorParser.White);
                DrawChar(popupX + popupW - 1, py, '|', QudColorParser.White);
            }
            string title = "Offer Deal";
            DrawText(popupX + (popupW - title.Length) / 2, popupY + 1, title, QudColorParser.BrightYellow);
            for (int px = popupX + 1; px < popupX + popupW - 1; px++)
                DrawChar(px, popupY + 2, '-', QudColorParser.DarkGray);
            int infoX = popupX + 2;
            DrawText(infoX, popupY + 3, "You give " + Lines(_deal.Gives) + ", worth " + _deal.GiveValue, QudColorParser.White);
            DrawText(infoX, popupY + 4, "You take " + Lines(_deal.Takes) + ", worth " + _deal.TakeValue, QudColorParser.White);
            string settle = DealSummary(out _);
            int cut = settle.IndexOf("   ", settle.IndexOf("Take", System.StringComparison.Ordinal), System.StringComparison.Ordinal);
            DrawText(infoX, popupY + 5, cut >= 0 ? settle.Substring(cut + 3) : settle, QudColorParser.Gray);
            int choiceY = popupY + 7;
            if (_confirmChoice == 0) DrawChar(infoX, choiceY, '>', QudColorParser.White);
            DrawText(infoX + 2, choiceY, "[Y] Yes", _confirmChoice == 0 ? QudColorParser.White : QudColorParser.Gray);
            if (_confirmChoice == 1) DrawChar(infoX, choiceY + 1, '>', QudColorParser.White);
            DrawText(infoX + 2, choiceY + 1, "[N] No", _confirmChoice == 1 ? QudColorParser.White : QudColorParser.Gray);
        }

        private static string Lines(IReadOnlyList<BarterDeal.Line> lines)
        {
            int units = 0;
            for (int i = 0; i < lines.Count; i++) units += lines[i].Count;
            return units == 0 ? "nothing" : units == 1 ? "1 thing" : units + " things";
        }

        // ===== Drawing Helpers (same as InventoryUI) =====

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var tilePos = new Vector3Int(x, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetUiTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int x, int y, string text, Color color)
        {
            if (text == null || Tilemap == null) return;
            for (int i = 0; i < text.Length && x + i < W; i++)
            {
                char c = text[i];
                if (c == ' ') continue;
                var tilePos = new Vector3Int(x + i, H - 1 - y, 0);
                var tile = CP437TilesetGenerator.GetUiTile(c);
                if (tile == null) continue;
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }

        private void DrawHLine(int x, int y, int width)
        {
            var tile = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.BoxHorizontal);
            if (tile == null) return;
            for (int i = x; i < x + width && i < W; i++)
            {
                var tilePos = new Vector3Int(i, H - 1 - y, 0);
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, QudColorParser.DarkGray);
            }
        }
    }
}
