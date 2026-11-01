using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// EC6b (Docs/ECONOMY-SYSTEM.md §12.3): a deal between the player and one trader. The player gives lines
    /// from their pack (each valued at its sell price) and takes lines from the trader's shelf (each valued at
    /// its buy price); a line may hold part of a stack, so salt can be small change. Prices come from the whole
    /// existing pipeline (performance, standing, place, regional value).
    ///
    /// <para>Settlement follows the trader's <see cref="Tender"/>: a barter trader accepts when what is given
    /// is worth at least what is taken, moves no drams and keeps any surplus; a dram trader settles the
    /// difference in drams either way. A deal is a mutable model for the trade screen; <see cref="Barter.TryDeal"/>
    /// executes it in one transaction and re-reads every value there.</para>
    /// </summary>
    public sealed class BarterDeal
    {
        public readonly struct Line
        {
            public readonly Entity Item; public readonly int Count;
            public Line(Entity item, int count) { Item = item; Count = count; }
        }

        public Entity Player { get; }
        public Entity Trader { get; }
        /// <summary>What the trader accepts, read when the deal is opened (the offer re-reads it).</summary>
        public TenderKind Tender { get; }
        /// <summary>The player's trade performance, read live so the screen never shows a stale price.</summary>
        public double Performance => TradeSystem.GetTradePerformance(Player);

        readonly List<Line> gives = new List<Line>(), takes = new List<Line>();

        public BarterDeal(Entity player, Entity trader)
        {
            Player = player; Trader = trader;
            Tender = trader == null || player == null ? TenderKind.None : Core.Tender.For(trader);
        }

        /// <summary>Units in an item: its stack count, or 1 for an unstackable item.</summary>
        public static int Units(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 1;

        public IReadOnlyList<Line> Gives => gives;
        public IReadOnlyList<Line> Takes => takes;
        public bool IsEmpty => gives.Count == 0 && takes.Count == 0;
        public int Giving(Entity item) => CountIn(gives, item);
        public int Taking(Entity item) => CountIn(takes, item);

        /// <summary>Put <paramref name="count"/> units of an item from the player's pack into the deal (clamped to
        /// the stack; 0 removes the line). False when nothing changed or the item may not join: it is not in the
        /// pack, it may not be traded, or the trader keeps no shop.</summary>
        public bool SetGive(Entity item, int count) => Set(gives, item, count, Player, "Sell");

        /// <summary>As <see cref="SetGive"/>, for an item on the trader's shelf.</summary>
        public bool SetTake(Entity item, int count) => Set(takes, item, count, Trader, "Buy");

        public int GiveValue
        {
            get { long sum = 0; foreach (var l in gives) sum += TradeSystem.GetSellPrice(l.Item, Performance, Trader, l.Count); return Clamp(sum); }
        }
        public int TakeValue
        {
            get { long sum = 0; foreach (var l in takes) sum += TradeSystem.GetBuyPrice(l.Item, Performance, Trader, l.Count); return Clamp(sum); }
        }
        /// <summary>What is given less what is taken; negative when the player is short.</summary>
        public int Balance => GiveValue - TakeValue;
        public int DramsFromPlayer => Tender == TenderKind.Drams ? Math.Max(0, -Balance) : 0;
        public int DramsToPlayer => Tender == TenderKind.Drams ? Math.Max(0, Balance) : 0;
        /// <summary>What a barter trader keeps over what it gives.</summary>
        public int Surplus => Tender == TenderKind.Barter ? Math.Max(0, Balance) : 0;

        /// <summary>The screen's preview: null when the deal is acceptable as it stands, else the reason: <c>no_shop</c>, <c>empty</c>,
        /// <c>short</c> (barter), <c>cannot_afford</c> or <c>trader_cannot_afford</c> (drams).</summary>
        public string Refusal()
        {
            if (Tender == TenderKind.None) return "no_shop";
            if (IsEmpty) return "empty";
            int balance = Balance;
            if (Tender == TenderKind.Barter) return balance < 0 ? "short" : null;
            if (balance < 0 && TradeSystem.GetDrams(Player) < -balance) return "cannot_afford";
            if (balance > 0 && TradeSystem.GetDrams(Trader) < balance) return "trader_cannot_afford";
            return null;
        }

        /// <summary>Close the gap with salt (goods tagged <see cref="RegionalValue.SaltTag"/>). Short: first return
        /// the trader's salt already taken, then add the fewest units of the player's salt that cover the gap.
        /// Over: first take back the player's own salt, then take as many of the trader's salt units as keep the
        /// deal acceptable. Returns the units moved; 0 when there is no salt to move.</summary>
        public int BalanceWithSalt()
        {
            if (Tender == TenderKind.None) return 0;
            int moved = 0;
            if (Balance < 0)
            {
                moved += Step(takes, Trader, -1, () => Balance < 0);
                moved += Step(gives, Player, +1, () => Balance < 0);
            }
            else if (Balance > 0)
            {
                moved += Step(gives, Player, -1, () => Balance > 0, keepAcceptable: true);
                moved += Step(takes, Trader, +1, () => Balance > 0, keepAcceptable: true);
            }
            return moved;
        }

        // One unit at a time: prices round per line, so the exact fewest units are found by trying them.
        int Step(List<Line> side, Entity holder, int delta, Func<bool> more, bool keepAcceptable = false)
        {
            int moved = 0;
            foreach (var salt in SaltOf(holder))
            {
                while (more())
                {
                    int now = CountIn(side, salt), next = now + delta;
                    if (next < 0 || next > Units(salt)) break;
                    Set(side, salt, next, holder, side == gives ? "Sell" : "Buy");
                    if (keepAcceptable && Balance < 0) { Set(side, salt, now, holder, side == gives ? "Sell" : "Buy"); break; }
                    moved++;
                }
            }
            return moved;
        }

        static List<Entity> SaltOf(Entity holder)
        {
            var salt = new List<Entity>();
            var inv = holder?.GetPart<InventoryPart>();
            if (inv == null) return salt;
            foreach (var item in inv.Objects) if (item != null && item.HasTag(RegionalValue.SaltTag)) salt.Add(item);
            return salt;
        }

        bool Set(List<Line> side, Entity item, int count, Entity holder, string direction)
        {
            if (item == null || Tender == TenderKind.None) return false;
            var inv = holder?.GetPart<InventoryPart>();
            if (inv == null || !inv.Objects.Contains(item) || item.GetPart<CommercePart>() == null) return false;
            if (!TradeSystem.CanBeTraded(item, Player, Trader, direction)) return false;
            count = Math.Max(0, Math.Min(count, Units(item)));
            int index = side.FindIndex(l => ReferenceEquals(l.Item, item));
            int before = index < 0 ? 0 : side[index].Count;
            if (before == count) return false;
            if (count == 0) side.RemoveAt(index);
            else if (index < 0) side.Add(new Line(item, count));
            else side[index] = new Line(item, count);
            return true;
        }

        static int CountIn(List<Line> side, Entity item)
        {
            foreach (var l in side) if (ReferenceEquals(l.Item, item)) return l.Count;
            return 0;
        }
        static int Clamp(long v) => (int)Math.Min(int.MaxValue, v);
    }

    /// <summary>
    /// EC6b: executes a <see cref="BarterDeal"/>. Every line in both directions, any stack splits and the dram
    /// settlement share one <see cref="InventoryTransaction"/>: a refused line, a missing item, a short purse or
    /// a pack too heavy leaves nothing changed. Values are re-read inside the transaction, so a deal composed
    /// earlier cannot carry stale prices. Diag: trade/Bartered or trade/BarterRejected (reason).
    /// </summary>
    public static class Barter
    {
        public static bool TryDeal(BarterDeal deal, out string failureReason)
        {
            failureReason = null;
            if (deal?.Player == null || deal.Trader == null) return Refuse(deal, "missing_participant", "The deal is missing a participant.", out failureReason);
            if (deal.Player.GetPart<InventoryPart>() == null || deal.Trader.GetPart<InventoryPart>() == null)
                return Refuse(deal, "missing_inventory", "An inventory is missing for this deal.", out failureReason);
            if (TradeSystem.TraderUnableToTrade(deal.Trader, out string state))
                return Refuse(deal, "trader_unable", $"The trader {state}.", out failureReason);
            // Only structure is checked here. Value and payment are judged once, inside the transaction, at
            // the moment of offer (EC6 mutation sweep: a second judge here hid the transaction's own check).
            if (deal.Tender == TenderKind.None) return Refuse(deal, "no_shop", Explain("no_shop"), out failureReason);
            if (deal.IsEmpty) return Refuse(deal, "empty", Explain("empty"), out failureReason);

            var command = new DealCommand(deal);
            var result = InventorySystem.ExecuteCommand(command, deal.Player);
            if (!result.Success) return Refuse(deal, command.Reason ?? "transfer_failed", result.ErrorMessage, out failureReason);

            MessageLog.Add(Receipt(command));
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record("trade", "Bartered", actor: deal.Player, target: deal.Trader, payload: new
                {
                    tender = command.Tender.ToString(), given = command.GivenNames, taken = command.TakenNames,
                    giveValue = command.GiveValue, takeValue = command.TakeValue, dramsPaid = command.DramsPaid,
                    dramsReceived = command.DramsReceived, surplus = command.Surplus
                });
            return true;
        }

        /// <summary>A screen-ready sentence for a <see cref="BarterDeal.Refusal"/> reason.</summary>
        public static string Explain(string refusal)
        {
            switch (refusal)
            {
                case "no_shop": return "Nothing here is for trade.";
                case "empty": return "Put something in the deal first.";
                case "short": return "Not enough in exchange. Add goods or salt.";
                case "cannot_afford": return "You can't afford the difference.";
                case "trader_cannot_afford": return "The trader can't pay the difference.";
                default: return "The deal fell through.";
            }
        }

        static string Receipt(DealCommand c)
        {
            string text = "You trade " + (c.GivenNames.Length > 0 ? c.GivenNames : "nothing")
                + " for " + (c.TakenNames.Length > 0 ? c.TakenNames : "nothing") + ".";
            if (c.DramsPaid > 0) text += $" You pay {c.DramsPaid} drams.";
            if (c.DramsReceived > 0) text += $" You receive {c.DramsReceived} drams.";
            if (c.Surplus > 0) text += $" They keep {c.Surplus} worth over.";
            return text;
        }

        static bool Refuse(BarterDeal deal, string reason, string message, out string failureReason)
        {
            failureReason = message; MessageLog.Add(message);
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record("trade", "BarterRejected", actor: deal?.Player, target: deal?.Trader, payload: new { reason, message });
            return false;
        }

        sealed class DealCommand : IInventoryCommand
        {
            readonly BarterDeal _deal;
            internal TenderKind Tender { get; private set; }
            internal int GiveValue, TakeValue, DramsPaid, DramsReceived, Surplus;
            internal string GivenNames = "", TakenNames = "", Reason;
            public string Name => "Barter";
            internal DealCommand(BarterDeal deal) { _deal = deal; }
            public InventoryValidationResult Validate(InventoryContext context) => InventoryValidationResult.Valid();

            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var player = context.Actor; var trader = _deal.Trader;
                var pack = context.Inventory; var shelf = trader.GetPart<InventoryPart>();
                if (ReferenceEquals(player, trader)) return Fail("self", "You cannot trade with yourself.");
                Tender = Core.Tender.For(trader);
                if (Tender == TenderKind.None) return Fail("no_shop", Explain("no_shop"));

                double perf = TradeSystem.GetTradePerformance(player);
                long give = 0, take = 0;
                foreach (var line in _deal.Gives)
                {
                    var check = Check(transaction, player, trader, line, pack, "Sell");
                    if (check != null) return check;
                    give += TradeSystem.GetSellPrice(line.Item, perf, trader, line.Count);
                }
                foreach (var line in _deal.Takes)
                {
                    var check = Check(transaction, player, trader, line, shelf, "Buy");
                    if (check != null) return check;
                    take += TradeSystem.GetBuyPrice(line.Item, perf, trader, line.Count);
                }
                GiveValue = (int)Math.Min(int.MaxValue, give); TakeValue = (int)Math.Min(int.MaxValue, take);
                long net = take - give; // positive: the player owes the difference
                if (Tender == TenderKind.Barter)
                {
                    if (net > 0) return Fail("short", Explain("short"));
                    Surplus = (int)Math.Min(int.MaxValue, -net);
                }
                else
                {
                    if (net > 0 && TradeSystem.GetDrams(player) < net) return Fail("cannot_afford", Explain("cannot_afford"));
                    if (net < 0 && TradeSystem.GetDrams(trader) < -net) return Fail("trader_cannot_afford", Explain("trader_cannot_afford"));
                    if (net > 0 && (long)TradeSystem.GetDrams(trader) + net > int.MaxValue) return Fail("currency_overflow", "The trader cannot carry that much currency.");
                    if (net < 0 && (long)TradeSystem.GetDrams(player) - net > int.MaxValue) return Fail("currency_overflow", "You cannot carry that much currency.");
                }

                // Gives first, so what leaves the pack frees its weight for what arrives.
                var given = new List<string>(); var taken = new List<string>();
                foreach (var line in _deal.Gives)
                {
                    var moved = Move(transaction, player, line, pack, shelf, "The trader cannot carry that much weight.", given);
                    if (moved != null) return moved;
                }
                foreach (var line in _deal.Takes)
                {
                    var moved = Move(transaction, player, line, shelf, pack, "You cannot carry that much weight.", taken);
                    if (moved != null) return moved;
                }
                if (Tender == TenderKind.Drams && net != 0)
                {
                    if (net > 0) { transaction.DeferCurrencyTransfer(player, trader, (int)net); DramsPaid = (int)net; }
                    else { transaction.DeferCurrencyTransfer(trader, player, (int)-net); DramsReceived = (int)-net; }
                }
                GivenNames = string.Join(", ", given); TakenNames = string.Join(", ", taken);
                return InventoryCommandResult.Ok();
            }

            InventoryCommandResult Check(InventoryTransaction transaction, Entity player, Entity trader, BarterDeal.Line line, InventoryPart holder, string direction)
            {
                if (line.Item == null || line.Count <= 0) return Fail("invalid_line", "The deal holds an empty line.");
                if (!transaction.TryClaim(line.Item, player, Name)) return Fail("in_transfer", "Item transfer is already in progress.");
                if (!holder.Objects.Contains(line.Item) || BarterDeal.Units(line.Item) < line.Count)
                    return Fail("item_gone", $"The {line.Item.GetDisplayName()} is no longer there.");
                if (!TradeSystem.CanBeTraded(line.Item, player, trader, direction))
                    return Fail("not_tradeable", $"You can't trade {line.Item.GetDisplayName()}.");
                return null;
            }

            InventoryCommandResult Move(InventoryTransaction transaction, Entity player, BarterDeal.Line line, InventoryPart from, InventoryPart to,
                string tooHeavy, List<string> names)
            {
                string name = line.Count > 1 || line.Item.GetPart<StackerPart>() != null && line.Count != BarterDeal.Units(line.Item)
                    ? line.Count + " " + line.Item.GetDisplayName() : line.Item.GetDisplayName();
                Entity moving = line.Item;
                var source = InventoryTransferSnapshot.Capture(from);
                transaction.Do(apply: null, undo: source.Restore);
                if (line.Count < BarterDeal.Units(line.Item))
                {
                    Entity split = null;
                    if (!source.Apply(() => (split = line.Item.GetPart<StackerPart>()?.SplitStack(line.Count)) != null))
                        return Fail("split_refused", $"The {line.Item.GetDisplayName()} cannot be divided.");
                    moving = split;
                    from.RefreshHandlingCarryPenalty();
                }
                else if (!source.Apply(() => from.RemoveObject(line.Item)))
                    return Fail("release_refused", $"The {line.Item.GetDisplayName()} could not be released.");

                var destination = InventoryTransferSnapshot.Capture(to, moving);
                transaction.Do(apply: null, undo: destination.Restore);
                if (!destination.Apply(() => to.AddObject(moving))) return Fail("overweight", tooHeavy);
                if (!destination.ClaimChanges(transaction, player, Name))
                    return Fail("in_transfer", "A destination stack is already being transferred.");
                names.Add(name);
                return null;
            }

            InventoryCommandResult Fail(string reason, string message)
            {
                Reason = reason;
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, message);
            }
        }
    }
}
