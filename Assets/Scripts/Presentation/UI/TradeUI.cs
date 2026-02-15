using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Full-screen trade UI. Two-panel layout:
    /// Left panel: NPC items with buy prices.
    /// Right panel: Player items with sell prices.
    /// Follows InventoryUI's 80x45 grid pattern.
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

        private Entity _trader;
        private double _performance;

        // 0 = left (buy from trader), 1 = right (sell to trader)
        private int _panel;
        private int _leftCursor, _leftScroll;
        private int _rightCursor, _rightScroll;

        private List<TradeRow> _leftRows = new List<TradeRow>();
        private List<TradeRow> _rightRows = new List<TradeRow>();

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
            _leftCursor = _leftScroll = 0;
            _rightCursor = _rightScroll = 0;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
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
                int price = TradeSystem.GetBuyPrice(stock[i], _performance);
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
                        int price = TradeSystem.GetSellPrice(item, _performance);
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
                Weight = physics != null ? physics.Weight : 0
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

            UpdateMouseHover();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            // Tab to switch panels
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _panel = 1 - _panel;
                Render();
                return;
            }

            // Navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                MoveCursor(-1);
                Render();
                return;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                MoveCursor(1);
                Render();
                return;
            }

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
                        ExecuteTrade();
                        return;
                    }
                }
            }

            // Enter to buy/sell
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteTrade();
                return;
            }
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

        private void ExecuteTrade()
        {
            if (_panel == 0)
            {
                if (_leftRows.Count == 0) return;
                TradeSystem.BuyFromTrader(PlayerEntity, _trader, _leftRows[_leftCursor].Item);
            }
            else
            {
                if (_rightRows.Count == 0) return;
                TradeSystem.SellToTrader(PlayerEntity, _trader, _rightRows[_rightCursor].Item);
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
            if (Tilemap == null) return;
            Tilemap.ClearAllTiles();

            int playerDrams = TradeSystem.GetDrams(PlayerEntity);
            int traderDrams = TradeSystem.GetDrams(_trader);
            int perfPct = (int)(_performance * 100);

            // Row 0: panel titles
            string traderName = _trader != null ? _trader.GetDisplayName() : "Trader";
            string leftTitle = "Buy from " + traderName;
            string rightTitle = "Sell your items";
            DrawText(LEFT_START, 0, leftTitle, _panel == 0 ? QudColorParser.White : QudColorParser.Gray);
            DrawText(RIGHT_START, 0, rightTitle, _panel == 1 ? QudColorParser.White : QudColorParser.Gray);

            // Row 1: drams and performance
            DrawText(LEFT_START, 1, "Trader: $" + traderDrams, QudColorParser.BrightYellow);
            string playerInfo = "You: $" + playerDrams + "  Perf:" + perfPct + "%";
            DrawText(RIGHT_START, 1, playerInfo, QudColorParser.BrightYellow);

            // Row 2: separator
            DrawHLine(0, 2, W);

            // Vertical divider
            for (int y = 0; y < H; y++)
                DrawChar(DIVIDER_X, y, '|', QudColorParser.DarkGray);

            // Left panel: trader items
            RenderPanel(_leftRows, LEFT_START, _leftCursor, _leftScroll, _panel == 0, LEFT_W, true);

            // Right panel: player items
            RenderPanel(_rightRows, RIGHT_START, _rightCursor, _rightScroll, _panel == 1, RIGHT_W, false);

            // Row 42: separator
            DrawHLine(0, CONTENT_BOTTOM + 1, W);

            // Row 43: detail line for selected item
            RenderDetailLine();

            // Row 44: action bar
            string actionBar = _panel == 0
                ? " [Enter]buy  [Tab]sell panel  [Esc]close"
                : " [Enter]sell  [Tab]buy panel  [Esc]close";
            DrawText(0, H - 1, actionBar, QudColorParser.DarkGray);
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
                if (name.Length > maxNameLen)
                    name = name.Substring(0, maxNameLen - 1) + "~";

                string line = name.PadRight(maxNameLen);
                line += rows[i].Weight.ToString().PadLeft(4) + "lb ";
                line += rows[i].Price.ToString().PadLeft(5) + "$";

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
            string detail = $" {row.Name}  Wt:{row.Weight}  Value:{baseValue}  {action} price:{row.Price}$";
            if (detail.Length > W)
                detail = detail.Substring(0, W);
            DrawText(0, H - 2, detail, QudColorParser.BrightCyan);
        }

        // ===== Drawing Helpers (same as InventoryUI) =====

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var tilePos = new Vector3Int(x, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
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
                var tile = CP437TilesetGenerator.GetTile(c);
                if (tile == null) continue;
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }

        private void DrawHLine(int x, int y, int width)
        {
            var tile = CP437TilesetGenerator.GetTile('-');
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
