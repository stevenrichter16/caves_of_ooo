using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// EC2 (Docs/ECONOMY-SYSTEM.md §5.1): the one path that moves drams outside a trade transaction.
    /// Trades and pickups already batch their currency through <see cref="Inventory.InventoryTransaction"/>;
    /// everything else (rewards, fees, offerings, contracts, rooms) comes through here, so money only
    /// enters the world through a named faucet and only leaves through a named sink.
    ///
    /// <para>Every operation is all-or-nothing: a null party, a non-positive amount, a purse that cannot
    /// cover a debit, or a credit that would pass <see cref="int.MaxValue"/> refuses and changes nothing.
    /// Diag (category <c>trade</c>): <c>Credited</c>, <c>Debited</c>, <c>Transferred</c>, or
    /// <c>LedgerRefused</c> with the refusal.</para>
    /// </summary>
    public static class Ledger
    {
        public const string MissingParty = "missing_party", InvalidAmount = "invalid_amount",
            Insufficient = "insufficient_currency", Overflow = "currency_overflow";

        /// <summary>A faucet: <paramref name="amount"/> drams enter the world into <paramref name="to"/>'s purse.</summary>
        public static bool TryCredit(Entity to, int amount, string source, out string refusal)
        {
            refusal = Check(null, to, amount, credit: true);
            if (refusal != null) return Refuse("Credited", null, to, amount, source, refusal);
            int after = TradeSystem.GetDrams(to) + amount;
            TradeSystem.SetDrams(to, after);
            Record("Credited", null, to, amount, source, after);
            return true;
        }

        /// <summary>A sink: <paramref name="amount"/> drams leave the world from <paramref name="from"/>'s purse.</summary>
        public static bool TryDebit(Entity from, int amount, string sink, out string refusal)
        {
            refusal = Check(from, null, amount, credit: false);
            if (refusal != null) return Refuse("Debited", from, null, amount, sink, refusal);
            int after = TradeSystem.GetDrams(from) - amount;
            TradeSystem.SetDrams(from, after);
            Record("Debited", from, null, amount, sink, after);
            return true;
        }

        /// <summary>A payment between two purses; the total in the world is unchanged.</summary>
        public static bool TryTransfer(Entity from, Entity to, int amount, string reason, out string refusal)
        {
            refusal = from == null || to == null ? MissingParty
                : amount <= 0 ? InvalidAmount
                : TradeSystem.GetDrams(from) < amount ? Insufficient
                : !ReferenceEquals(from, to) && (long)TradeSystem.GetDrams(to) + amount > int.MaxValue ? Overflow
                : null;
            if (refusal != null) return Refuse("Transferred", from, to, amount, reason, refusal);
            if (!ReferenceEquals(from, to))
            {
                TradeSystem.SetDrams(from, TradeSystem.GetDrams(from) - amount);
                TradeSystem.SetDrams(to, TradeSystem.GetDrams(to) + amount);
            }
            Record("Transferred", from, to, amount, reason, TradeSystem.GetDrams(from));
            return true;
        }

        static string Check(Entity from, Entity to, int amount, bool credit)
        {
            if (credit ? to == null : from == null) return MissingParty;
            if (amount <= 0) return InvalidAmount;
            if (!credit && TradeSystem.GetDrams(from) < amount) return Insufficient;
            if (credit && (long)TradeSystem.GetDrams(to) + amount > int.MaxValue) return Overflow;
            return null;
        }

        static void Record(string kind, Entity from, Entity to, int amount, string reason, int balanceAfter)
        {
            if (!Diag.IsChannelEnabled("trade")) return;
            Diag.Record("trade", kind, actor: from ?? to, target: from != null ? to : null,
                payload: new { currency = TradeSystem.CURRENCY_PROP, amount, reason = reason ?? "", balanceAfter });
        }

        static bool Refuse(string operation, Entity from, Entity to, int amount, string reason, string refusal)
        {
            if (Diag.IsChannelEnabled("trade"))
                Diag.Record("trade", "LedgerRefused", actor: from ?? to, target: from != null ? to : null,
                    payload: new { currency = TradeSystem.CURRENCY_PROP, operation, amount, reason = reason ?? "", refusal });
            return false;
        }
    }
}
