using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;

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
        public static bool BuyFromTrader(Entity buyer, Entity trader, Entity item) =>
            BuyFromTrader(buyer, trader, item, out _);

        /// <summary>Buy with a screen-ready refusal reason. Delivery, exact source
        /// restoration and payment share one transaction; callbacks precede final funds checks.</summary>
        public static bool BuyFromTrader(Entity buyer, Entity trader, Entity item, out string failureReason)
        {
            failureReason = null;
            if (buyer == null || trader == null || item == null)
                return RefuseBuy(buyer, trader, item, "The purchase is missing an item or participant.", out failureReason);
            if (buyer.GetPart<InventoryPart>() == null || trader.GetPart<InventoryPart>() == null)
                return RefuseBuy(buyer, trader, item, "An inventory is missing for this purchase.", out failureReason);
            if (TraderUnableToTrade(trader, out string reason))
                return RefuseBuy(buyer, trader, item, $"The trader {reason}.", out failureReason);
            string itemName = item.GetDisplayName();
            var command = new BuyTransferCommand(trader, item);
            var result = InventorySystem.ExecuteCommand(command, buyer);
            if (!result.Success) return RefuseBuy(buyer, trader, item, result.ErrorMessage, out failureReason);
            int price = command.Price; double perf = command.Performance;
            MessageLog.Add($"You buy {itemName} for {price} drams.");
            Diag.Record("trade", "Bought", actor: buyer, target: trader,
                payload: new { itemName, itemId = item.ID, price, dramsAfter = GetDrams(buyer), perf });
            return true;
        }
        private static bool RefuseBuy(Entity buyer, Entity trader, Entity item, string message, out string failureReason)
        {
            failureReason = message; MessageLog.Add(message);
            Diag.Record("trade", "BuyRejected", actor: buyer, target: trader,
                payload: new { itemId = item?.ID, reason = message });
            return false;
        }
        private sealed class BuyTransferCommand : IInventoryCommand
        {
            private readonly Entity _trader, _item;
            internal int Price { get; private set; }
            internal double Performance { get; private set; }
            public string Name => "BuyFromTrader";
            internal BuyTransferCommand(Entity trader, Entity item) { _trader = trader; _item = item; }
            public InventoryValidationResult Validate(InventoryContext context) => InventoryValidationResult.Valid();
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var buyer = context.Actor; var stock = _trader.GetPart<InventoryPart>();
                if (ReferenceEquals(buyer, _trader))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You cannot buy from yourself.");
                if (!transaction.TryClaim(_item, buyer, Name))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");
                if (!stock.CanConsumeOne(_item))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "That item is no longer in the trader's stock.");
                if (!CanBeTraded(_item, buyer, _trader, "Buy"))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, $"You can't trade {_item.GetDisplayName()}.");
                Performance = GetTradePerformance(buyer); Price = GetBuyPrice(_item, Performance, _trader);
                if (GetDrams(buyer) < Price)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You can't afford that.");
                var before = GameEvent.New("BeforeTrade");
                before.SetParameter("Buyer", (object)buyer); before.SetParameter("Trader", (object)_trader);
                before.SetParameter("Item", (object)_item); before.SetParameter("Price", Price);
                if (!buyer.FireEventAndRelease(before))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The purchase was cancelled.");
                // Independent callbacks may change stock, prices or the purse. Read the
                // actual current state before transfer, preserving their committed work.
                if (!stock.CanConsumeOne(_item))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "That item is no longer in the trader's stock.");
                Performance = GetTradePerformance(buyer); Price = GetBuyPrice(_item, Performance, _trader);
                int buyerDrams = GetDrams(buyer), traderDrams = GetDrams(_trader);
                if (buyerDrams < Price)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You can't afford that.");
                if ((long)traderDrams + Price > int.MaxValue)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The trader cannot carry that much currency.");
                var source = InventoryTransferSnapshot.Capture(stock);
                transaction.Do(apply: null, undo: source.Restore);
                if (!source.Apply(() => stock.RemoveObject(_item)))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The trader could not release that item.");
                var destination = InventoryTransferSnapshot.Capture(context.Inventory, _item);
                transaction.Do(apply: null, undo: destination.Restore);
                if (!destination.Apply(() => context.Inventory.AddObject(_item)))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You cannot carry that much weight.");
                if (!destination.ClaimChanges(transaction, buyer, Name))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "A destination stack is already being transferred.");
                transaction.DeferCurrencyTransfer(buyer, _trader, Price);
                return InventoryCommandResult.Ok();
            }
        }

        /// <summary>
        /// Sell an item to a trader. Transfers item to trader, drams to seller.
        /// </summary>
        public static bool SellToTrader(Entity seller, Entity trader, Entity item) =>
            SellToTrader(seller, trader, item, out _);

        /// <summary>Sell with an explanation suitable for the trade screen on refusal.
        /// Payment follows successful delivery; failed transfer restores exact item and equipment state.</summary>
        public static bool SellToTrader(Entity seller, Entity trader, Entity item, out string failureReason)
        {
            failureReason = null;
            if (seller == null || trader == null || item == null)
                return RefuseSale(seller, trader, item, "The sale is missing an item or participant.", out failureReason);
            if (trader.GetPart<InventoryPart>() == null || seller.GetPart<InventoryPart>() == null)
                return RefuseSale(seller, trader, item, "An inventory is missing for this sale.", out failureReason);
            if (TraderUnableToTrade(trader, out string reason))
                return RefuseSale(seller, trader, item, $"The trader {reason}.", out failureReason);
            // Name/quantity must be captured before destination merging absorbs the input.
            string itemName = item.GetDisplayName();
            var command = new SellTransferCommand(trader, item);
            var result = InventorySystem.ExecuteCommand(command, seller);
            if (!result.Success)
                return RefuseSale(seller, trader, item, result.ErrorMessage, out failureReason);

            int price = command.Price; double perf = command.Performance;
            MessageLog.Add($"You sell {itemName} for {price} drams.");
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record(category: "trade", kind: "Sold", actor: seller, target: trader,
                    payload: new { itemName, itemId = item.ID, price, dramsAfter = GetDrams(seller), perf });
            return true;
        }

        private static bool RefuseSale(Entity seller, Entity trader, Entity item, string message, out string failureReason)
        {
            failureReason = message;
            MessageLog.Add(message);
            Diag.Record("trade", "SaleRejected", actor: seller, target: trader,
                payload: new { itemId = item?.ID, reason = message });
            return false;
        }

        // Equipment, both item lists, and wallets share one command transaction.
        // In particular, failed trader capacity must not commit a separate UnequipItem call.
        private sealed class SellTransferCommand : IInventoryCommand
        {
            private readonly Entity _trader, _item;
            internal int Price { get; private set; }
            internal double Performance { get; private set; }
            public string Name => "SellToTrader";
            internal SellTransferCommand(Entity trader, Entity item)
            { _trader = trader; _item = item; }
            public InventoryValidationResult Validate(InventoryContext context) => InventoryValidationResult.Valid();
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                if (ReferenceEquals(context.Actor, _trader))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You cannot sell to yourself.");
                if (!transaction.TryClaim(_item, context.Actor, Name))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");
                if (!context.Inventory.Contains(_item) || (_item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You no longer carry a positive unit of that item.");
                if (!CanBeTraded(_item, context.Actor, _trader, "Sell"))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, $"You can't trade {_item.GetDisplayName()}.");
                Performance = GetTradePerformance(context.Actor); Price = GetSellPrice(_item, Performance, _trader);
                if (GetDrams(_trader) < Price)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The trader can't afford that.");
                if (UnequipCommand.CaptureEquippedState(context, _item).HasLocation)
                {
                    var unequipped = new UnequipCommand(_item).Execute(context, transaction);
                    if (!unequipped.Success) return unequipped;
                }
                // A permitted independent-item sale in AfterUnequip can spend this
                // trader's purse. Preserve that committed sale and refuse this one.
                if (GetDrams(_trader) < Price)
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The trader can't afford that.");
                var source = InventoryTransferSnapshot.Capture(context.Inventory);
                transaction.Do(apply: null, undo: source.Restore);
                if (!context.Inventory.CanConsumeOne(_item) || !source.Apply(() => context.Inventory.RemoveObject(_item)))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "You no longer carry that item.");

                var traderInventory = _trader.GetPart<InventoryPart>();
                var destination = InventoryTransferSnapshot.Capture(traderInventory, _item);
                transaction.Do(apply: null, undo: destination.Restore);
                if (!destination.Apply(() => traderInventory.AddObject(_item)))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The trader cannot carry that much weight.");

                if (!destination.ClaimChanges(transaction, context.Actor, Name))
                    return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "A destination stack is already being transferred.");
                transaction.DeferCurrencyTransfer(_trader, context.Actor, Price);
                return InventoryCommandResult.Ok();
            }
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
