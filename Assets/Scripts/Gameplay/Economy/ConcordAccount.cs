using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// EC7 (Docs/ECONOMY-SYSTEM.md §12.4): the player's account with the Saccharine Concord. Canon
    /// (Lore/Factions/04_SaccharineConcord.md): the Concord's currency of trust is the honoured debt; its
    /// reputation "is literally a ledger"; standing is gained by "paying debts promptly" and lost by
    /// "defaulting on debts"; at standing it offers "Concord credit — the ability to carry debt rather than
    /// drams". A Factor extends credit; any Concord post-keeper takes repayment (one ledger, many posts).
    ///
    /// <para>The account lives in the player's integer properties, so it rides the save graph like drams.
    /// Credit enters through <see cref="Ledger.TryCredit"/> and repayment leaves through
    /// <see cref="Ledger.TryDebit"/>; the fee makes credit a net money sink, and a default is a faucet paid for
    /// in standing. Defaults are found by <see cref="Review"/> (zone transitions, and before any borrowing or
    /// repayment), never per turn. Diag (trade): ConcordCredit, ConcordRepaid, ConcordHonored,
    /// ConcordDefaulted, ConcordRefused (reason).</para>
    /// </summary>
    public static class ConcordAccount
    {
        public const string Faction = "SaccharineConcord";
        /// <summary>Blueprint tags: a Factor extends credit; every post-keeper keeps the ledger.</summary>
        public const string CreditTag = "ConcordCredit", LedgerTag = "ConcordLedger";
        public const string DebtProp = "ConcordDebt", DueProp = "ConcordDue", OpenedProp = "ConcordOpened",
            DefaultProp = "ConcordDefaulted", HonoredProp = "ConcordHonored";
        public const string BorrowAction = "ConcordBorrow", RepayAction = "ConcordRepay";

        public const int BaseLine = 20, MaxLine = 120, FeePercent = 20, Honor = 5, DefaultPenalty = 25;
        public const int TermTicks = 3 * WorldClock.DayLengthTicks;
        /// <summary>A debt honoured must have been carried this long (with one account at a time, this keeps
        /// borrowing-and-repaying from being farmed for standing).</summary>
        public const int HonorMinTicks = WorldClock.DayLengthTicks;

        public static bool ExtendsCredit(Entity speaker) => speaker != null && speaker.HasTag(CreditTag);
        public static bool KeepsLedger(Entity speaker) => speaker != null && (speaker.HasTag(LedgerTag) || speaker.HasTag(CreditTag));

        public static int Debt(Entity player) => player?.GetIntProperty(DebtProp, 0) ?? 0;
        public static int Due(Entity player) => player?.GetIntProperty(DueProp, 0) ?? 0;
        public static bool InDefault(Entity player) => player != null && player.GetIntProperty(DefaultProp, 0) != 0;
        public static int Honored(Entity player) => player?.GetIntProperty(HonoredProp, 0) ?? 0;
        public static int Standing => PlayerReputation.Get(Faction);

        /// <summary>The credit line: 20 drams plus half the player's Concord standing, capped at 120, and nothing
        /// while an earlier debt is in default. The line reaches zero at standing -40, so the Disliked (-50 and
        /// worse) are never offered credit.</summary>
        public static int Line(Entity player)
        {
            if (player == null || InDefault(player)) return 0;
            return Math.Max(0, Math.Min(MaxLine, BaseLine + Standing / 2));
        }

        /// <summary>What the player could borrow now: the whole line, or nothing while an account is open.</summary>
        public static int Available(Entity player) => Debt(player) > 0 ? 0 : Line(player);

        /// <summary>The Concord's fee on a principal, rounded up.</summary>
        public static int Fee(int principal) => principal <= 0 ? 0 : (int)Math.Ceiling(principal * FeePercent / 100.0);

        /// <summary>Open an account: credit <paramref name="principal"/> drams, owe principal plus fee within the
        /// term. Null on success, else the refusal (and nothing changed).</summary>
        public static string Borrow(Entity factor, Entity player, int principal, int now)
        {
            if (factor == null || player == null) return Refuse(factor, player, "missing_participant", null);
            if (!ExtendsCredit(factor)) return Refuse(factor, player, "not_a_factor", "Only a Factor extends the Concord's credit.");
            Review(player, now);
            if (InDefault(player)) return Refuse(factor, player, "in_default", "Your name is in default. Settle it before the Concord lends again.");
            if (Debt(player) > 0) return Refuse(factor, player, "open_account", "Your account is already open. One debt at a time.");
            if (principal <= 0) return Refuse(factor, player, "invalid_amount", null);
            int line = Line(player);
            if (principal > line) return Refuse(factor, player, "over_line", $"Your line with the Concord is {line} drams.");
            if (!Ledger.TryCredit(player, principal, "concord-credit", out string refusal))
                return Refuse(factor, player, refusal, "You cannot carry that much currency.");
            int fee = Fee(principal), debt = principal + fee, due = now + TermTicks;
            player.SetIntProperty(DebtProp, debt);
            player.SetIntProperty(DueProp, due);
            player.SetIntProperty(OpenedProp, now);
            MessageLog.Add($"The Factor enters {principal} drams against your name. Owed: {debt}, within three days.");
            Record("ConcordCredit", factor, player, new { principal, fee, debt, due, line });
            return null;
        }

        /// <summary>Pay up to <paramref name="amount"/> drams toward the open account (never more than is owed
        /// or carried). Settling in full, on time, after carrying the debt a day, raises Concord standing; a
        /// settled default clears the mark without honour. Null on success, else the refusal.</summary>
        public static string Repay(Entity keeper, Entity player, int amount, int now)
        {
            if (keeper == null || player == null) return Refuse(keeper, player, "missing_participant", null);
            if (!KeepsLedger(keeper)) return Refuse(keeper, player, "not_a_ledger", null);
            Review(player, now);
            int debt = Debt(player);
            if (debt <= 0) return Refuse(keeper, player, "no_debt", "You owe the Concord nothing.");
            if (amount <= 0) return Refuse(keeper, player, "invalid_amount", null);
            int pay = Math.Min(amount, Math.Min(debt, TradeSystem.GetDrams(player)));
            if (pay <= 0) return Refuse(keeper, player, "insufficient_currency", "You carry no drams to pay with.");
            if (!Ledger.TryDebit(player, pay, "concord-repayment", out string refusal)) return Refuse(keeper, player, refusal, null);

            int remaining = debt - pay;
            bool wasDefault = InDefault(player), honored = false;
            if (remaining > 0)
            {
                player.SetIntProperty(DebtProp, remaining);
                MessageLog.Add($"The ledger takes {pay} drams. {remaining} still owed.");
            }
            else
            {
                int opened = player.GetIntProperty(OpenedProp, now), due = Due(player);
                honored = !wasDefault && now <= due && now - opened >= HonorMinTicks;
                player.RemoveIntProperty(DebtProp); player.RemoveIntProperty(DueProp); player.RemoveIntProperty(OpenedProp);
                player.RemoveIntProperty(DefaultProp);
                if (honored)
                {
                    player.SetIntProperty(HonoredProp, Honored(player) + 1);
                    PlayerReputation.Modify(Faction, Honor);
                    MessageLog.Add("The account closes, honoured. The Concord remembers prompt payers.");
                    Record("ConcordHonored", keeper, player, new { honor = Honor, honoredCount = Honored(player) });
                }
                else MessageLog.Add(wasDefault ? "The account closes. The default stays in the margin." : "The account closes.");
            }
            Record("ConcordRepaid", keeper, player, new { paid = pay, remaining, honored, wasDefault });
            return null;
        }

        /// <summary>Find a missed due date: mark the account in default and take the standing, once. Returns
        /// whether this review found a new default.</summary>
        public static bool Review(Entity player, int now)
        {
            if (player == null || Debt(player) <= 0 || InDefault(player) || now <= Due(player)) return false;
            player.SetIntProperty(DefaultProp, 1);
            PlayerReputation.Modify(Faction, -DefaultPenalty);
            MessageLog.Add("A Concord debt has passed its day unpaid. The ledger marks your name in default.");
            Record("ConcordDefaulted", null, player, new { owed = Debt(player), due = Due(player), now, penalty = DefaultPenalty });
            return true;
        }

        /// <summary>The account as a post-keeper reads it out: standing, line, and what is owed and when.</summary>
        public static string Statement(Entity player, int now)
        {
            int debt = Debt(player);
            string text = $"Your name in the ledger: standing {Standing}, credit line {Line(player)} drams.";
            if (debt <= 0) return text + " Nothing owed.";
            int left = Due(player) - now;
            string when = left < 0 ? "overdue" : left < WorldClock.DayLengthTicks ? "due today" : $"due in {left / WorldClock.DayLengthTicks} day{(left / WorldClock.DayLengthTicks == 1 ? "" : "s")}";
            text += $" Owed {debt} drams, {when}.";
            if (InDefault(player)) text += " In default: no credit until it is settled.";
            return text;
        }

        // ════════════════ conversation ════════════════

        /// <summary>The live credit and settlement choices a post-keeper's greeting offers. Read-only: amounts are
        /// computed, nothing is reviewed or changed here. A choice never lies: no repayment offer to a player who
        /// carries no drams, no credit while an account is open or in default.</summary>
        public static void AppendChoices(List<ChoiceData> choices, Entity speaker, Entity listener, string nodeId)
        {
            if (choices == null || listener == null || nodeId != "Start" || !KeepsLedger(speaker)) return;
            if (ExtendsCredit(speaker) && !InDefault(listener) && Debt(listener) <= 0)
            {
                int line = Line(listener), half = Math.Max(1, line / 2);
                if (line > 0) choices.Add(CreditChoice(half));
                if (line > half) choices.Add(CreditChoice(line));
            }
            int debt = Debt(listener), drams = TradeSystem.GetDrams(listener);
            if (debt > 0 && drams > 0)
                choices.Add(Choice(drams >= debt ? $"[Ledger] Settle my account: {debt} drams." : $"[Ledger] Pay {drams} of the {debt} drams I owe.",
                    RepayAction, Math.Min(debt, drams)));
        }

        static ChoiceData CreditChoice(int principal) =>
            Choice($"[Credit] Advance me {principal} drams (owe {principal + Fee(principal)} within three days).", BorrowAction, principal);

        static ChoiceData Choice(string text, string action, int amount) => new ChoiceData
        {
            Text = text, Target = "",
            Actions = new List<ConversationParam> { new ConversationParam { Key = action, Value = amount.ToString() } }
        };

        /// <summary>A post-keeper's greeting reads the account aloud.</summary>
        public static string AppendText(string original, Entity speaker, Entity listener, string nodeId, int now)
        {
            if (nodeId != "Start" || listener == null || !KeepsLedger(speaker)) return original;
            string statement = Statement(listener, now);
            return string.IsNullOrEmpty(original) ? statement : original + "\n\n" + statement;
        }

        static string Refuse(Entity keeper, Entity player, string reason, string message)
        {
            if (message != null) MessageLog.Add(message);
            Record("ConcordRefused", keeper, player, new { reason });
            return reason;
        }

        static void Record(string kind, Entity keeper, Entity player, object payload)
        {
            if (Diag.IsChannelEnabled("trade")) Diag.Record("trade", kind, actor: player, target: keeper, payload: payload);
        }
    }
}
