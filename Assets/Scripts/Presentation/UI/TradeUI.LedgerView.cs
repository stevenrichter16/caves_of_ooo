using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class TradeUI
    {
        private const float CanvasWidth = 1280;
        private const float CanvasHeight = 800;
        private const float TradeRowHeight = 25;
        private const int VisibleRows = 18;
        private static readonly Rect TradeModeButton = new Rect(1082, 24, 174, 36);
        private static readonly Rect TradeConfirmButton = new Rect(674, 475, 252, 42);
        private static readonly Rect TradeCancelButton = new Rect(354, 475, 252, 42);
        private static Rect TradeList(int side) => new Rect(24 + side * 625, 173, 607, TradeRowHeight * VisibleRows);
        private readonly List<Image> _tradeImages = new List<Image>();
        private readonly List<Text> _tradeLabels = new List<Text>();
        private RectTransform _tradeView;
        private GameObject _tradeRoot;
        private Font _tradeFont;
        private int _tradeImageCount, _tradeLabelCount;

        private void EnsureTradeView()
        {
            if (_tradeRoot != null) return;
            _tradeRoot = new GameObject("MerchantTrade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _tradeRoot.transform.SetParent(transform, false);
            var canvas = _tradeRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _tradeRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = TradeGraphic<Image>("TradeBackground", _tradeRoot.transform);
            background.color = LBackground;
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            _tradeView = new GameObject("TradeContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _tradeView.SetParent(_tradeRoot.transform, false);
            _tradeView.anchorMin = _tradeView.anchorMax = new Vector2(.5f, .5f);
            _tradeView.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _tradeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void SetTradeViewVisible(bool visible)
        {
            if (_tradeRoot != null) _tradeRoot.SetActive(visible);
        }

        private bool TryTradePoint(Vector2 screen, out Vector2 point)
        {
            point = default;
            if (_tradeView == null || !_tradeRoot.activeInHierarchy
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_tradeView, screen, null, out var local))
                return false;
            var rect = _tradeView.rect;
            point = new Vector2(local.x - rect.xMin, rect.yMax - local.y);
            return _tradeView.rect.Contains(local);
        }

        private static T TradeGraphic<T>(string name, Transform parent) where T : Graphic
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            result.transform.SetParent(parent, false);
            result.raycastTarget = false;
            return result;
        }

        private static void PlaceTradeGraphic(Graphic graphic, Rect bounds)
        {
            graphic.gameObject.SetActive(true);
            graphic.transform.SetAsLastSibling();
            var rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
        }

        private void TradeBox(Rect bounds, Color color, Sprite sprite = null)
        {
            if (_tradeImageCount == _tradeImages.Count)
                _tradeImages.Add(TradeGraphic<Image>("Shape", _tradeView));
            var image = _tradeImages[_tradeImageCount++];
            PlaceTradeGraphic(image, bounds);
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
        }

        private void TradeFrame(Rect bounds, Color edge, Color fill)
        {
            TradeBox(bounds, edge);
            TradeBox(new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, bounds.height - 2), fill);
        }

        private void TradeText(Rect bounds, string value, Color color, int size = 18,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft, bool wrap = false)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (_tradeLabelCount == _tradeLabels.Count)
            {
                var label = TradeGraphic<Text>("TradeText", _tradeView);
                label.font = _tradeFont;
                label.supportRichText = false;
                _tradeLabels.Add(label);
            }
            var result = _tradeLabels[_tradeLabelCount++];
            PlaceTradeGraphic(result, bounds);
            result.fontStyle = style;
            result.fontSize = size;
            result.alignment = alignment;
            result.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            result.verticalOverflow = VerticalWrapMode.Truncate;
            result.color = color;
            result.text = value;
            if (!wrap && result.preferredWidth > bounds.width)
                result.text = FitTradeText(result, value, bounds.width);
        }

        private static string FitTradeText(Text label, string text, float width)
        {
            int lower = 0, upper = text.Length;
            while (lower < upper)
            {
                int length = (lower + upper + 1) / 2;
                label.text = text.Substring(0, length) + "…";
                if (label.preferredWidth <= width) lower = length;
                else upper = length - 1;
            }
            return text.Substring(0, lower) + "…";
        }

        private void Render()
        {
            using (PerformanceMarkers.Ui.TradeRender.Auto())
            {
                if (!_isOpen) return;
                PerformanceDiagnostics.RecordTradeRender();
                EnsureTradeView();
                SetTradeViewVisible(true);
                _tradeImageCount = _tradeLabelCount = 0;
                RenderTradeHeader();
                RenderTradeList(0, _leftRows, _leftScroll, _leftCursor);
                RenderTradeList(1, _rightRows, _rightScroll, _rightCursor);
                RenderTradeSelection();
                if (_confirmActive) RenderTradeConfirmation();
                for (int i = _tradeImageCount; i < _tradeImages.Count; i++) _tradeImages[i].gameObject.SetActive(false);
                for (int i = _tradeLabelCount; i < _tradeLabels.Count; i++) _tradeLabels[i].gameObject.SetActive(false);
            }
        }

        private void RenderTradeHeader()
        {
            string title = _tender == TenderKind.Barter ? "Barter" : _deal != null ? "Deal" : "Trade";
            TradeText(new Rect(24, 15, 214, 52), title, LInk, 42, FontStyle.Bold);
            TradeText(new Rect(218, 23, 846, 44), _trader.GetDisplayName(), LGold, 30);
            if (_tender == TenderKind.Drams)
                TradeButton(TradeModeButton, _deal == null ? "Compose deal   [D]" : "Shop   [D]", false);
            TradeBox(new Rect(24, 68, 1232, 1), LBorder);
            string stock = MerchantStockService.Describe(_trader).TrimStart('*', ' ');
            string benefit = MerchantStockService.ProjectBenefit(_trader);
            if (benefit.Length == 0) benefit = PlaceConsequences.WorkingPriceNote(_trader);
            string terms = _tender == TenderKind.Barter ? "Goods and salt" : "Trade performance " + (int)(_performance * 100) + "%";
            TradeText(new Rect(24, 72, 193, 28), terms, LMuted, 16);
            TradeText(new Rect(226, 72, 405, 28), stock, LMuted, 15);
            TradeText(new Rect(649, 69, 607, 37), benefit, LGreen, 15, wrap: true);
        }

        private void RenderTradeList(int side, List<TradeRow> rows, int scroll, int cursor)
        {
            bool buying = side == 0;
            bool active = side == _panel;
            Rect list = TradeList(side);
            float x = list.x;
            TradeFrame(new Rect(x, 106, list.width, list.yMax - 106 + 4), active ? LGold : LBorder, LPanel);
            string title = _deal == null ? (buying ? "Buy from merchant" : "Sell from your pack")
                : (buying ? "Merchant’s goods" : "Your pack");
            TradeText(new Rect(x + 13, 111, 310, 29), title, active ? LGold : LInk, 21, FontStyle.Bold);
            if (_tender == TenderKind.Drams)
            {
                int drams = TradeSystem.GetDrams(buying ? _trader : PlayerEntity);
                TradeText(new Rect(x + 324, 111, 269, 29), drams + " drams", LInk, 19, FontStyle.Normal, TextAnchor.MiddleRight);
            }
            TradeBox(new Rect(x + 12, 143, list.width - 24, 1), LBorder);
            TradeText(new Rect(x + 14, 146, 280, 24), "Item", LMuted, 15);
            TradeText(new Rect(x + 411, 146, 82, 24), _deal != null ? "In offer" : "Qty", LMuted, 15,
                FontStyle.Normal, TextAnchor.MiddleRight);
            string tender = _tender == TenderKind.Drams ? "Drams" : "Value";
            TradeText(new Rect(x + 493, 146, 99, 24), _deal != null ? tender + " / item" : tender, LMuted, 15,
                FontStyle.Normal, TextAnchor.MiddleRight);
            if (rows.Count == 0)
            {
                TradeText(new Rect(x + 13, list.y, 560, 36), buying ? "Nothing for sale." : "Nothing to sell.", LMuted);
                return;
            }

            // The rightmost values have independent columns, including on a long item name.
            for (int i = scroll; i < rows.Count && i < scroll + VisibleRows; i++)
                RenderTradeRow(rows[i], buying, active && i == cursor, x, list.y + (i - scroll) * TradeRowHeight, i);
            if (scroll > 0 || scroll + VisibleRows < rows.Count)
                TradeText(new Rect(x + 14, 626, 580, 17),
                    $"{scroll + 1}–{Mathf.Min(rows.Count, scroll + VisibleRows)} of {rows.Count}   ·   Scroll or PgUp / PgDn", LMuted, 14);
        }

        private void RenderTradeRow(TradeRow row, bool buying, bool selected, float x, float y, int index)
        {
            if (selected)
            {
                TradeBox(new Rect(x + 3, y, 601, TradeRowHeight), LSelected);
                TradeBox(new Rect(x + 3, y + 2, 3, TradeRowHeight - 4), LGold);
            }
            else if ((index & 1) == 0) TradeBox(new Rect(x + 3, y, 601, TradeRowHeight), LBackground);

            var icon = InventoryItemIcons.For(row.Item);
            if (icon != null) TradeBox(new Rect(x + 14, y + 3, 18, 18), Color.white, icon);
            float nameX = x + (icon != null ? 41 : 14);
            float nameRight = x + (row.Special ? 308 : 405);
            TradeText(new Rect(nameX, y, nameRight - nameX, TradeRowHeight), row.Name, LInk, 18,
                selected ? FontStyle.Bold : FontStyle.Normal);
            if (row.Special)
                TradeText(new Rect(x + 312, y, 100, TradeRowHeight), "Special stock", LGold, 14);
            int units = BarterDeal.Units(row.Item);
            int marked = _deal == null ? 0 : MarkedCount(row.Item, buying);
            string count = _deal == null ? "×" + units : marked + " / " + units;
            TradeText(new Rect(x + 411, y, 82, TradeRowHeight), count, marked > 0 ? LGreen : LMuted,
                16, FontStyle.Normal, TextAnchor.MiddleRight);
            string value = (_deal == null ? row.Price : UnitQuote(row.Item, buying)).ToString();
            TradeText(new Rect(x + 496, y, 96, TradeRowHeight), value, LInk, 17, FontStyle.Normal, TextAnchor.MiddleRight);
        }

        private void RenderTradeSelection()
        {
            var row = SelectedRow();
            TradeFrame(new Rect(24, 648, 1232, 85), LBorder, LPanel);
            if (row.Item != null)
            {
                TradeText(new Rect(38, 653, 1178, 29), row.Name, LInk, 21, FontStyle.Bold);
                bool buying = _panel == 0;
                int units = BarterDeal.Units(row.Item);
                string quantity = _deal == null ? (units == 1 ? "1 item" : units + " items · whole stack")
                    : MarkedCount(row.Item, buying) + " / " + units + " in offer";
                string tender = _tender == TenderKind.Drams ? " drams" : " value";
                string quote = _deal == null ? row.Price + tender
                    : UnitQuote(row.Item, buying) + tender + " per item";
                TradeText(new Rect(38, 683, 1178, 23), $"{quantity}   ·   {row.Weight} lb total   ·   {quote}   ·   [F1] Item details", LMuted, 17);
            }
            if (_deal != null)
            {
                string balance = DealSummary(out var color);
                Color tone = color == QudColorParser.BrightRed ? LRed : color == QudColorParser.BrightGreen ? LGreen : LGold;
                TradeText(new Rect(38, 707, 1180, 23), balance, tone, 17, FontStyle.Bold);
            }
            TradeText(new Rect(24, 735, 1232, 26), _statusMessage, LRed, 18, FontStyle.Bold);
            string controls = _deal != null
                ? "[Enter / →] Add one     [←] Remove one     [A] All     [S] Add salt     [O] Offer     [Tab] Switch side     [Esc] Close"
                : (_panel == 0 ? "[Enter] Buy stack" : "[Enter] Sell stack") + "     [Tab] Switch side     [D] Compose deal     [Esc] Close";
            TradeText(new Rect(24, 766, 1232, 28), controls, LMuted, 17);
        }

        private int UnitQuote(Entity item, bool buying) => buying
            ? TradeSystem.GetBuyPrice(item, _performance, _trader, 1)
            : TradeSystem.GetSellPrice(item, _performance, _trader, 1);

        private void RenderTradeConfirmation()
        {
            TradeBox(new Rect(0, 0, CanvasWidth, CanvasHeight), new Color(0, 0, 0, .78f));
            TradeFrame(new Rect(326, 252, 628, 290), LGold, LPanel);
            bool buying = _confirmPanel == 0;
            string title = _confirmDeal ? "Offer this deal" : buying ? "Buy item" : "Sell item";
            TradeText(new Rect(350, 270, 580, 40), title, LGold, 31, FontStyle.Bold);
            TradeBox(new Rect(350, 317, 580, 1), LBorder);
            if (_confirmDeal)
            {
                TradeText(new Rect(350, 326, 580, 70), DealSummary(out _), LInk, 21, wrap: true);
            }
            else
            {
                TradeText(new Rect(350, 325, 580, 58), _confirmRow.Name, LInk, 23, FontStyle.Bold, wrap: true);
                string price = (_confirmCount > 1 ? "Whole stack · " + _confirmCount + " items · " : "") + _confirmRow.Price + " drams";
                TradeText(new Rect(350, 388, 580, 31), price, LGold, 23, FontStyle.Bold);
                long after = (long)TradeSystem.GetDrams(PlayerEntity) + (buying ? -_confirmRow.Price : _confirmRow.Price);
                long weight = (PlayerEntity.GetPart<InventoryPart>()?.GetCarriedWeight() ?? 0L)
                    + (buying ? 1L : -1L) * _confirmRow.Weight;
                TradeText(new Rect(350, 425, 580, 30), $"After trade   ·   {after} drams   ·   {weight} lb carried",
                    after < 0 ? LRed : LMuted, 18);
            }
            TradeButton(TradeCancelButton, "No, cancel   [N / Esc]", _confirmChoice == 1);
            TradeButton(TradeConfirmButton, "Yes, confirm   [Y]", _confirmChoice == 0);
        }

        private void TradeButton(Rect bounds, string caption, bool selected)
        {
            TradeFrame(bounds, selected ? LGold : LBorder, selected ? LSelected : LPanel);
            TradeText(bounds, caption, selected ? LGold : LInk, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
    }
}
