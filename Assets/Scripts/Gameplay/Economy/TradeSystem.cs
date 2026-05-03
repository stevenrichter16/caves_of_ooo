using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles trade between entities (player ↔ merchant).
    /// Mirrors Qud's trade system: value from CommercePart, performance from Ego stat,
    /// currency tracked as "Drams" IntProperty on entities.
    ///
    /// Buy price (player pays): ceil(Value / Performance * FactionMod)  — higher Ego = cheaper
    /// Sell price (player gets): floor(Value * Performance / FactionMod) — higher Ego = better deals
    /// Faction modifier: Loved=0.85, Liked=0.95, Neutral=1.0, Disliked=1.10
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
        /// Get the faction price modifier based on player reputation with the trader's faction.
        /// Loved: 0.85 (15% discount), Liked: 0.95 (5% discount),
        /// Neutral: 1.0, Disliked: 1.10 (10% markup).
        /// </summary>
        public static double GetFactionModifier(Entity trader)
        {
            if (trader == null) return 1.0;
            string faction = FactionManager.GetFaction(trader);
            if (string.IsNullOrEmpty(faction) || faction == "Player") return 1.0;

            var attitude = PlayerReputation.GetAttitude(faction);
            switch (attitude)
            {
                case PlayerReputation.Attitude.Loved: return 0.85;
                case PlayerReputation.Attitude.Liked: return 0.95;
                case PlayerReputation.Attitude.Disliked: return 1.10;
                default: return 1.0;
            }
        }

        /// <summary>
        /// Price the player pays to buy an item from a trader.
        /// Higher performance = lower price. Better faction standing = lower price.
        /// </summary>
        public static int GetBuyPrice(Entity item, double performance, Entity trader = null)
        {
            int value = GetItemValue(item);
            if (value <= 0) return 0;
            if (performance <= 0.01) return value * 20; // safety cap
            double factionMod = GetFactionModifier(trader);
            return (int)Math.Ceiling(value / performance * factionMod);
        }

        /// <summary>
        /// Price the player receives selling an item to a trader.
        /// Higher performance = better sell price. Better faction standing = better price.
        /// </summary>
        public static int GetSellPrice(Entity item, double performance, Entity trader = null)
        {
            int value = GetItemValue(item);
            if (value <= 0) return 0;
            double factionMod = GetFactionModifier(trader);
            return Math.Max(1, (int)Math.Floor(value * performance / factionMod));
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

            // SP.3: trader must be in a state that allows trade.
            if (TraderUnableToTrade(trader, out string reason))
            {
                MessageLog.Add($"The trader {reason}.");
                return false;
            }

            // SP.2 (Docs/SHOPPING-PARITY.md): per-item veto. Items with
            // a "NoTrade" tag (e.g., quest items, dungeon keys) refuse
            // to be traded. Listeners can also veto by returning false
            // from CanBeTraded HandleEvent — the firing-on-the-item-
            // first design lets items self-protect without forcing
            // buyer/seller logic to know about every quest state.
            if (!CanBeTraded(item, buyer, trader, "Buy"))
            {
                MessageLog.Add($"You can't trade {item.GetDisplayName()}.");
                return false;
            }

            double perf = GetTradePerformance(buyer);
            int price = GetBuyPrice(item, perf, trader);

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
            if (!buyer.FireEventAndRelease(beforeTrade))
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

            // SP.4 diag hook (Docs/SHOPPING-PARITY.md): every successful
            // buy is recorded so AI debugging can answer "did the player
            // actually trade for X?" and "what was the perf-adjusted price?".
            if (Diag.IsChannelEnabled("trade"))
            {
                Diag.Record(
                    category: "trade", kind: "Bought",
                    actor: buyer, target: trader,
                    payload: new
                    {
                        itemName = item.GetDisplayName(),
                        itemId = item.ID,
                        price,
                        dramsAfter = GetDrams(buyer),
                        perf
                    });
            }
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

            // SP.3: trader-state validation (mirrors buy path).
            if (TraderUnableToTrade(trader, out string reason))
            {
                MessageLog.Add($"The trader {reason}.");
                return false;
            }

            // SP.2: per-item veto on sell side. Same shape as the buy
            // path — quest items refuse to leave the player's bag.
            if (!CanBeTraded(item, seller, trader, "Sell"))
            {
                MessageLog.Add($"You can't trade {item.GetDisplayName()}.");
                return false;
            }

            double perf = GetTradePerformance(seller);
            int price = GetSellPrice(item, perf, trader);

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

            // SP.4 diag hook: mirrors the Buy path.
            if (Diag.IsChannelEnabled("trade"))
            {
                Diag.Record(
                    category: "trade", kind: "Sold",
                    actor: seller, target: trader,
                    payload: new
                    {
                        itemName = item.GetDisplayName(),
                        itemId = item.ID,
                        price,
                        dramsAfter = GetDrams(seller),
                        perf
                    });
            }
            return true;
        }

        /// <summary>
        /// SP.3 (Docs/SHOPPING-PARITY.md): check whether the trader is
        /// in a state that allows trading. Returns true and a reason
        /// string when the trader CAN'T trade. Mirrors Qud's
        /// <c>TradeUI.cs:362-379</c> validation set, narrowed to the
        /// states CoO actually has effects for.
        ///
        /// Refused states (any one):
        ///   • Hitpoints ≤ 0 (dead)
        ///   • <see cref="BurningEffect"/> (on fire — can't speak)
        ///   • <see cref="StunnedEffect"/> (incapacitated)
        ///   • <see cref="FrozenEffect"/> (frozen solid)
        ///
        /// The list is intentionally narrow — Confused, Calmed,
        /// Bleeding etc. don't block trade. If playtest reveals more
        /// states should refuse, add them here. Documented in
        /// Docs/SHOPPING-PARITY.md self-review.
        /// </summary>
        public static bool TraderUnableToTrade(Entity trader, out string reason)
        {
            reason = null;
            if (trader == null) { reason = "is missing"; return true; }

            var hp = trader.GetStat("Hitpoints");
            if (hp != null && hp.BaseValue <= 0) { reason = "is dead"; return true; }

            var effects = trader.GetPart<StatusEffectsPart>();
            if (effects != null)
            {
                if (effects.HasEffect<BurningEffect>())  { reason = "is on fire"; return true; }
                if (effects.HasEffect<StunnedEffect>())  { reason = "is stunned"; return true; }
                if (effects.HasEffect<FrozenEffect>())   { reason = "is frozen"; return true; }
            }

            return false;
        }

        /// <summary>
        /// SP.3: fire <c>StartTradeEvent</c> on the trader. Mirrors
        /// Qud's StartTradeEvent shape — listeners can veto the
        /// session entirely (returning false) or set service flags
        /// for future identify/repair/recharge content.
        ///
        /// Returns false if a listener vetoed; caller should
        /// abort opening the trade UI.
        /// </summary>
        public static bool FireStartTradeEvent(Entity buyer, Entity trader)
        {
            if (trader == null) return false;
            var ev = GameEvent.New("StartTrade");
            ev.SetParameter("Buyer", (object)buyer);
            ev.SetParameter("Trader", (object)trader);
            return trader.FireEventAndRelease(ev);
        }

        /// <summary>
        /// SP.2 (Docs/SHOPPING-PARITY.md): check whether an item can
        /// be traded in the requested direction. Mirrors Qud's
        /// <c>CanBeTradedEvent</c> (XRL.World.Parts.Inventory cite).
        ///
        /// Two veto paths:
        ///   1. Items tagged <c>"NoTrade"</c> are refused outright
        ///      (quest items, dungeon keys, soft-bound rewards).
        ///   2. The <c>CanBeTraded</c> event fires on the item itself;
        ///      any listener returning false from HandleEvent vetoes.
        ///      This lets future Parts (e.g. a "BoundToOwner" Part)
        ///      self-protect without modifying TradeSystem.
        ///
        /// Returns true if the item can be traded.
        /// </summary>
        public static bool CanBeTraded(Entity item, Entity actor, Entity trader, string direction)
        {
            if (item == null) return false;

            // 1. Tag-based fast-path veto.
            if (item.HasTag("NoTrade")) return false;

            // 2. Event-based veto. Lets parts on the item refuse
            // dynamically (a quest part might allow trade after the
            // quest completes, etc.).
            var canBeTraded = GameEvent.New("CanBeTraded");
            canBeTraded.SetParameter("Item", (object)item);
            canBeTraded.SetParameter("Actor", (object)actor);
            canBeTraded.SetParameter("Trader", (object)trader);
            canBeTraded.SetParameter("Direction", direction);
            return item.FireEventAndRelease(canBeTraded);
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
