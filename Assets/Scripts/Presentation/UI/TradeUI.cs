using System.Collections.Generic;
using UnityEngine.InputSystem;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Merchant exchange controller for whole-stack shops and counted offers.</summary>
    public partial class TradeUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        public string SelectedDecisionDetails()
        {
            var row = SelectedRow();
            if (!_isOpen || _confirmActive || row.Item == null) return null;
            int? count = _deal == null ? null : MarkedCount(row.Item, _panel == 0);
            return InventoryDecisionDetails.Trade(PlayerEntity, _trader, row.Item, _panel == 0, count);
        }

        public void HideForDecisionReader()
        {
            SetTradeViewVisible(false);
            if (Tilemap != null) Tilemap.ClearAllTiles();
        }

        public void RestoreAfterDecisionReader()
        {
            if (!_isOpen) return;
            Rebuild();
            Render();
        }

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
        private TradeRow _confirmRow; // exact item and displayed quote
        private int _confirmCount;
        private int _confirmChoice;  // 0 = Yes, 1 = No

        private struct TradeRow
        {
            public Entity Item;
            public string Name;
            public int Price;
            public int Weight;
            public bool Special;
        }

        private System.IDisposable _stockView;

        public void Open(Entity trader)
        {
            if (PlayerEntity == null || trader == null) return;
            MerchantStockService.ForgetOffer(_deal);
            _stockView?.Dispose();
            MerchantStockService.Reconcile(trader, trader.SpatialZone, TurnManager.Active?.TickCount ?? 0);
            _stockView = MerchantStockService.BeginView(trader);
            _trader = trader;
            _isOpen = true;
            _panel = 0;
            _statusMessage = null;
            _confirmActive = _confirmDeal = false;
            _confirmRow = default;
            _lastPointer = new Vector2(float.NaN, float.NaN);
            ClearWorldTiles();
            _tender = Tender.For(trader);
            _deal = _tender == TenderKind.Barter ? new BarterDeal(PlayerEntity, trader) : null;
            _leftCursor = _leftScroll = 0;
            _rightCursor = _rightScroll = 0;
            Rebuild();
            Render();
        }

        public void Close()
        {
            MerchantStockService.ForgetOffer(_deal);
            _deal = null;
            _stockView?.Dispose(); _stockView = null;
            _isOpen = false;
            SetTradeViewVisible(false);
            _confirmRow = default;
            ClearWorldTiles();
        }

        private void ClearWorldTiles()
        {
            if (Tilemap == null) return;
            Tilemap.ClearAllTiles();
            PerformanceDiagnostics.RecordTilemapClear();
        }

        private void OnDisable() { Close(); }

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
                _leftRows.Add(BuildRow(stock[i], price, MerchantStockService.IsSpecial(_trader, stock[i])));
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

        private static TradeRow BuildRow(Entity item, int price, bool special = false)
        {
            return new TradeRow
            {
                Item = item,
                Name = item.GetDisplayName(includeStackCount: false),
                Price = price,
                Weight = InventoryPart.GetItemWeight(item),
                Special = special
            };
        }

        private TradeRow SelectedRow()
        {
            var rows = _panel == 0 ? _leftRows : _rightRows;
            int cursor = _panel == 0 ? _leftCursor : _rightCursor;
            return cursor >= 0 && cursor < rows.Count ? rows[cursor] : default;
        }

        private int MarkedCount(Entity item, bool buying) => buying ? _deal.Taking(item) : _deal.Giving(item);

        private void ClampCursor(ref int cursor, ref int scroll, int count)
        {
            if (count == 0) { cursor = 0; scroll = 0; return; }
            if (cursor >= count) cursor = count - 1;
            if (cursor < 0) cursor = 0;
            if (cursor < scroll) scroll = cursor;
            if (cursor >= scroll + VisibleRows) scroll = cursor - VisibleRows + 1;
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

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Tab))
            {
                _panel = 1 - _panel;
                Render();
                return;
            }

            int move = 0;
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)) move = -1;
            else if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J)) move = 1;
            else if (InputHelper.GetKeyDown(KeyCode.PageUp)) move = -VisibleRows;
            else if (InputHelper.GetKeyDown(KeyCode.PageDown)) move = VisibleRows;
            if (move != 0)
            {
                MoveCursor(move);
                Render();
                return;
            }

            if (_deal != null)
            {
                foreach (var key in DealKeys)
                    if (InputHelper.GetKeyDown(key)) { DealKey(key); return; }
            }
            else
            {
                if (InputHelper.GetKeyDown(KeyCode.D)) { DealKey(KeyCode.D); return; }
                if (InputHelper.GetKeyDown(KeyCode.Return)) { ShowConfirmation(); return; }
            }
            HandleTradePointer();
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
                MerchantStockService.ForgetOffer(_deal);
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
            _statusMessage = null;
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
            _confirmRow = SelectedRow();
            if (_confirmRow.Item == null) return;
            _confirmCount = BarterDeal.Units(_confirmRow.Item);
            _confirmActive = true;
            _confirmDeal = false;
            _confirmPanel = _panel;
            _confirmChoice = 0;
            Render();
        }

        private void HandleConfirmInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape) || InputHelper.GetKeyDown(KeyCode.N))
            {
                CancelConfirmation();
                return;
            }
            if (InputHelper.GetKeyDown(KeyCode.Y)) { ConfirmTrade(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return))
            {
                if (_confirmChoice == 0) ConfirmTrade();
                else CancelConfirmation();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)
                || InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J)
                || InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H)
                || InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L))
            {
                _confirmChoice = 1 - _confirmChoice;
                Render();
                return;
            }
            HandleTradePointer();
        }

        private void ConfirmTrade()
        {
            _confirmActive = false;
            if (_confirmDeal)
            {
                _confirmDeal = false;
                ExecuteDeal();
                return;
            }

            var row = _confirmRow;
            _confirmRow = default;
            if (row.Item == null) return;
            bool buying = _confirmPanel == 0;
            double performance = TradeSystem.GetTradePerformance(PlayerEntity);
            int price = buying ? TradeSystem.GetBuyPrice(row.Item, performance, _trader)
                : TradeSystem.GetSellPrice(row.Item, performance, _trader);
            if (price != row.Price || BarterDeal.Units(row.Item) != _confirmCount)
            {
                _statusMessage = "The quantity or price changed. Review the current quote before trading.";
                Rebuild();
                Render();
                return;
            }
            ExecuteTradeItem(row.Item, buying);
        }

        private void CancelConfirmation()
        {
            _confirmActive = false;
            _confirmDeal = false;
            _confirmRow = default;
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

        private void ExecuteTradeItem(Entity item, bool buying)
        {
            bool ok = buying ? TradeSystem.BuyFromTrader(PlayerEntity, _trader, item, out string reason)
                : TradeSystem.SellToTrader(PlayerEntity, _trader, item, out reason);
            _statusMessage = ok ? null : reason;
            Rebuild();
            Render();
        }

        private void HandleTradePointer()
        {
            var mouse = Mouse.current;
            if (mouse != null && !mouse.enabled) mouse = null;
            Vector2 position = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
            bool moved = position != _lastPointer;
            _lastPointer = position;
            if (!TryTradePoint(position, out var point)) return;

            bool click = mouse != null ? mouse.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
            if (_confirmActive)
            {
                if (click && TradeConfirmButton.Contains(point)) ConfirmTrade();
                else if (click && TradeCancelButton.Contains(point)) CancelConfirmation();
                return;
            }

            if (click && _tender == TenderKind.Drams && TradeModeButton.Contains(point))
            {
                DealKey(KeyCode.D);
                return;
            }

            int panel = TradeList(0).Contains(point) ? 0 : TradeList(1).Contains(point) ? 1 : -1;
            if (panel < 0) return;
            float wheel = mouse != null ? mouse.scroll.ReadValue().y : Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > .01f)
            {
                _panel = panel;
                MoveCursor(wheel > 0 ? -3 : 3);
                Render();
                return;
            }

            int index = (panel == 0 ? _leftScroll : _rightScroll)
                + Mathf.FloorToInt((point.y - TradeList(panel).y) / TradeRowHeight);
            var rows = panel == 0 ? _leftRows : _rightRows;
            if (index >= rows.Count || (!moved && !click)) return;
            if (click || panel != _panel || index != (panel == 0 ? _leftCursor : _rightCursor))
            {
                _panel = panel;
                SetCursor(index);
                if (!click) Render();
                else if (_deal != null) DealKey(KeyCode.Return);
                else ShowConfirmation();
            }
        }

        private Vector2 _lastPointer;

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

    }
}
