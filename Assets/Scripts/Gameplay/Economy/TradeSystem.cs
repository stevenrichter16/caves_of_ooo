using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles trade between entities (player ↔ merchant).
    /// Mirrors Qud's trade system: value from CommercePart, performance from Ego stat,
    /// currency tracked as "Drams" IntProperty on entities.
    ///
    /// Buy price (player pays): ceil(Value / Performance)  — higher Ego = cheaper
    /// Sell price (player gets): floor(Value * Performance) — higher Ego = better deals
    /// </summary>
    public static class TradeSystem
    {
        public const string CURRENCY_PROP = "Drams";

        /// <summary>
        /// Get the base value of an item from its CommercePart.
        /// Stack-aware: value * stack count.
        /// </summary>
        public static int GetItemValue(Entity item)
        {
            if (item == null) return 0;
            var commerce = item.GetPart<CommercePart>();
            if (commerce == null) return 0;

            int baseValue = commerce.Value;
            var stacker = item.GetPart<StackerPart>();
            if (stacker != null)
                return baseValue * stacker.StackCount;
            return baseValue;
        }

        /// <summary>
        /// Calculate trade performance for an entity based on Ego stat.
        /// Formula: clamp(0.35 + 0.07 * EgoMod, 0.05, 0.95)
        /// Returns a value between 0.05 and 0.95.
        /// </summary>
        public static double GetTradePerformance(Entity entity)
        {
            if (entity == null) return 0.5;
            int egoMod = StatUtils.GetModifier(entity, "Ego");
            double perf = 0.35 + 0.07 * egoMod;
            return Math.Max(0.05, Math.Min(0.95, perf));
        }

        /// <summary>
        /// Price the player pays to buy an item from a trader.
        /// Higher performance = lower price.
        /// </summary>
        public static int GetBuyPrice(Entity item, double performance)
        {
            int value = GetItemValue(item);
            if (value <= 0) return 0;
            if (performance <= 0.01) return value * 20; // safety cap
            return (int)Math.Ceiling(value / performance);
        }

        /// <summary>
        /// Price the player receives selling an item to a trader.
        /// Higher performance = better sell price.
        /// </summary>
        public static int GetSellPrice(Entity item, double performance)
        {
            int value = GetItemValue(item);
            if (value <= 0) return 0;
            return Math.Max(1, (int)Math.Floor(value * performance));
        }

        /// <summary>
        /// Get the drams (currency) an entity has.
        /// </summary>
        public static int GetDrams(Entity entity)
        {
            if (entity == null) return 0;
            return entity.GetIntProperty(CURRENCY_PROP, 0);
        }

        /// <summary>
        /// Set the drams (currency) an entity has.
        /// </summary>
        public static void SetDrams(Entity entity, int amount)
        {
            if (entity == null) return;
            entity.SetIntProperty(CURRENCY_PROP, Math.Max(0, amount));
        }

        /// <summary>
        /// Buy an item from a trader. Transfers item to buyer, drams to trader.
        /// </summary>
        public static bool BuyFromTrader(Entity buyer, Entity trader, Entity item)
        {
            if (buyer == null || trader == null || item == null) return false;

            var traderInv = trader.GetPart<InventoryPart>();
            if (traderInv == null) return false;

            var buyerInv = buyer.GetPart<InventoryPart>();
            if (buyerInv == null) return false;

            double perf = GetTradePerformance(buyer);
            int price = GetBuyPrice(item, perf);

            int buyerDrams = GetDrams(buyer);
            if (buyerDrams < price)
            {
                MessageLog.Add("You can't afford that!");
                return false;
            }

            // Fire BeforeTrade event
            var beforeTrade = GameEvent.New("BeforeTrade");
            beforeTrade.SetParameter("Buyer", (object)buyer);
            beforeTrade.SetParameter("Trader", (object)trader);
            beforeTrade.SetParameter("Item", (object)item);
            beforeTrade.SetParameter("Price", price);
            if (!buyer.FireEvent(beforeTrade))
                return false;

            // Transfer item
            if (!traderInv.RemoveObject(item)) return false;
            if (!buyerInv.AddObject(item))
            {
                traderInv.AddObject(item);
                MessageLog.Add($"You can't carry {item.GetDisplayName()}: too heavy!");
                return false;
            }

            // Transfer currency
            SetDrams(buyer, buyerDrams - price);
            SetDrams(trader, GetDrams(trader) + price);

            MessageLog.Add($"You buy {item.GetDisplayName()} for {price} drams.");
            return true;
        }

        /// <summary>
        /// Sell an item to a trader. Transfers item to trader, drams to seller.
        /// </summary>
        public static bool SellToTrader(Entity seller, Entity trader, Entity item)
        {
            if (seller == null || trader == null || item == null) return false;

            var traderInv = trader.GetPart<InventoryPart>();
            if (traderInv == null) return false;

            var sellerInv = seller.GetPart<InventoryPart>();
            if (sellerInv == null) return false;

            double perf = GetTradePerformance(seller);
            int price = GetSellPrice(item, perf);

            int traderDrams = GetDrams(trader);
            if (traderDrams < price)
            {
                MessageLog.Add("The trader can't afford that!");
                return false;
            }

            // If equipped, unequip first
            if (InventorySystem.IsEquipped(seller, item))
            {
                if (!InventorySystem.UnequipItem(seller, item))
                    return false;
            }

            // Transfer item
            if (!sellerInv.RemoveObject(item)) return false;
            traderInv.AddObject(item);

            // Transfer currency
            SetDrams(seller, GetDrams(seller) + price);
            SetDrams(trader, traderDrams - price);

            MessageLog.Add($"You sell {item.GetDisplayName()} for {price} drams.");
            return true;
        }

        /// <summary>
        /// Get all items a trader has for sale.
        /// </summary>
        public static List<Entity> GetTraderStock(Entity trader)
        {
            var result = new List<Entity>();
            if (trader == null) return result;

            var inv = trader.GetPart<InventoryPart>();
            if (inv == null) return result;

            for (int i = 0; i < inv.Objects.Count; i++)
            {
                var item = inv.Objects[i];
                if (item.GetPart<CommercePart>() != null)
                    result.Add(item);
            }
            return result;
        }
    }
}
