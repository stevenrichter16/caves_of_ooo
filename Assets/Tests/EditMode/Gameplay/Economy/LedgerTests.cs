using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC2 (Docs/ECONOMY-SYSTEM.md §5.1): the ledger's contract. Every operation is all-or-nothing, a
    /// transfer conserves money, faucets and sinks are named, and every outcome leaves a trade record.
    /// </summary>
    public sealed class LedgerTests
    {
        [SetUp] public void Setup() { Diag.ResetAll(); Diag.SetChannel("trade", true); }
        [TearDown] public void TearDown() { Diag.ResetAll(); }

        static Entity Purse(int drams) { var e = new Entity { ID = "purse" + drams }; TradeSystem.SetDrams(e, drams); return e; }
        static System.Collections.Generic.List<Diag.Entry> Records(string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = kind, Limit = 50 }).Records.ToList();
        static string Field(Diag.Entry r, string name) => Regex.Match(r.PayloadJson, "\"" + name + "\":\"?([^,\"}]*)").Groups[1].Value;

        // ════════════════ faucets and sinks ════════════════

        [Test] public void ACreditEntersThePurseAndNamesItsSource()
        {
            var p = Purse(10);
            Assert.True(Ledger.TryCredit(p, 5, "reward", out var why), why);
            Assert.AreEqual(15, TradeSystem.GetDrams(p));
            var r = Records("Credited").Single();
            Assert.AreEqual("5", Field(r, "amount")); Assert.AreEqual("reward", Field(r, "reason")); Assert.AreEqual("15", Field(r, "balanceAfter"));
        }

        [Test] public void ADebitLeavesThePurseAndNamesItsSink()
        {
            var p = Purse(10);
            Assert.True(Ledger.TryDebit(p, 10, "offering", out var why), why);
            Assert.AreEqual(0, TradeSystem.GetDrams(p), "a purse may be emptied exactly");
            Assert.AreEqual("offering", Field(Records("Debited").Single(), "reason"));
        }

        [Test] public void ADebitThePurseCannotCoverChangesNothing()
        {
            var p = Purse(9);
            Assert.False(Ledger.TryDebit(p, 10, "offering", out var why));
            Assert.AreEqual(Ledger.Insufficient, why);
            Assert.AreEqual(9, TradeSystem.GetDrams(p));
            CollectionAssert.IsEmpty(Records("Debited"));
            Assert.AreEqual(Ledger.Insufficient, Field(Records("LedgerRefused").Single(), "refusal"));
        }

        [Test] public void ACreditPastTheCeilingChangesNothing()
        {
            var p = Purse(int.MaxValue - 4);
            Assert.False(Ledger.TryCredit(p, 5, "reward", out var why));
            Assert.AreEqual(Ledger.Overflow, why);
            Assert.AreEqual(int.MaxValue - 4, TradeSystem.GetDrams(p));
            Assert.True(Ledger.TryCredit(p, 4, "reward", out _), "counter-check: up to the ceiling is fine");
            Assert.AreEqual(int.MaxValue, TradeSystem.GetDrams(p));
        }

        // ════════════════ transfers ════════════════

        [Test] public void ATransferConservesMoney()
        {
            var a = Purse(30); var b = Purse(7);
            Assert.True(Ledger.TryTransfer(a, b, 12, "fee", out var why), why);
            Assert.AreEqual(18, TradeSystem.GetDrams(a)); Assert.AreEqual(19, TradeSystem.GetDrams(b));
            Assert.AreEqual(37, TradeSystem.GetDrams(a) + TradeSystem.GetDrams(b));
            Assert.AreEqual("fee", Field(Records("Transferred").Single(), "reason"));
        }

        [Test] public void ATransferThePayeeCannotHoldLeavesThePayerWhole()
        {
            var a = Purse(30); var b = Purse(int.MaxValue - 1);
            Assert.False(Ledger.TryTransfer(a, b, 12, "fee", out var why));
            Assert.AreEqual(Ledger.Overflow, why);
            Assert.AreEqual(30, TradeSystem.GetDrams(a)); Assert.AreEqual(int.MaxValue - 1, TradeSystem.GetDrams(b));
        }

        [Test] public void ATransferThePayerCannotCoverLeavesBothWhole()
        {
            var a = Purse(3); var b = Purse(0);
            Assert.False(Ledger.TryTransfer(a, b, 4, "fee", out var why));
            Assert.AreEqual(Ledger.Insufficient, why);
            Assert.AreEqual(3, TradeSystem.GetDrams(a)); Assert.AreEqual(0, TradeSystem.GetDrams(b));
        }

        [Test] public void PayingYourselfChangesNothing()
        {
            var a = Purse(30);
            Assert.True(Ledger.TryTransfer(a, a, 12, "self", out _));
            Assert.AreEqual(30, TradeSystem.GetDrams(a));
            Assert.False(Ledger.TryTransfer(a, a, 31, "self", out var why), "counter-check: not more than you have");
            Assert.AreEqual(Ledger.Insufficient, why);
        }

        // ════════════════ boundary inputs ════════════════

        [TestCase(0)] [TestCase(-5)] [TestCase(int.MinValue)]
        public void ANonPositiveAmountIsRefusedEverywhere(int amount)
        {
            var a = Purse(30); var b = Purse(30);
            Assert.False(Ledger.TryCredit(a, amount, "x", out var c)); Assert.AreEqual(Ledger.InvalidAmount, c);
            Assert.False(Ledger.TryDebit(a, amount, "x", out var d)); Assert.AreEqual(Ledger.InvalidAmount, d);
            Assert.False(Ledger.TryTransfer(a, b, amount, "x", out var t)); Assert.AreEqual(Ledger.InvalidAmount, t);
            Assert.AreEqual(30, TradeSystem.GetDrams(a)); Assert.AreEqual(30, TradeSystem.GetDrams(b));
            Assert.AreEqual(3, Records("LedgerRefused").Count);
        }

        [Test] public void AMissingPartyIsRefusedEverywhere()
        {
            var a = Purse(30);
            Assert.False(Ledger.TryCredit(null, 5, "x", out var c)); Assert.AreEqual(Ledger.MissingParty, c);
            Assert.False(Ledger.TryDebit(null, 5, "x", out var d)); Assert.AreEqual(Ledger.MissingParty, d);
            Assert.False(Ledger.TryTransfer(a, null, 5, "x", out var t1)); Assert.AreEqual(Ledger.MissingParty, t1);
            Assert.False(Ledger.TryTransfer(null, a, 5, "x", out var t2)); Assert.AreEqual(Ledger.MissingParty, t2);
            Assert.AreEqual(30, TradeSystem.GetDrams(a));
        }

        /// <summary>Diagnostics never steer money: with the channel off the purses move the same.</summary>
        [Test] public void TheTradeChannelOffChangesNothingButTheRecords()
        {
            Diag.SetChannel("trade", false);
            var a = Purse(30); var b = Purse(0);
            Assert.True(Ledger.TryTransfer(a, b, 10, "fee", out _));
            Assert.False(Ledger.TryDebit(b, 11, "sink", out _));
            Assert.AreEqual(20, TradeSystem.GetDrams(a)); Assert.AreEqual(10, TradeSystem.GetDrams(b));
            Assert.AreEqual(0, Records("Transferred").Count + Records("LedgerRefused").Count);
        }
    }
}
