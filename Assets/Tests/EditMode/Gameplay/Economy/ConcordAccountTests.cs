using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC7 (Docs/ECONOMY-SYSTEM.md §12.4): the Concord's account. A Factor extends credit against the player's
    /// standing; any post-keeper takes repayment; a debt paid promptly raises standing and a default costs it.
    /// Canon: Lore/Factions/04_SaccharineConcord.md (the honoured debt; "Concord credit — the ability to carry
    /// debt rather than drams"; standing gained by paying debts promptly, lost by defaulting).
    /// </summary>
    public sealed class ConcordAccountTests
    {
        static EntityFactory factory;
        const string Concord = ConcordAccount.Faction;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            ConversationLoader.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset();
            ConversationManager.EndConversation(); ConversationManager.PendingTradePartner = null;
        }
        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset();
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }

        static Entity Player(int drams = 0) { var p = factory.CreateEntity("Player"); TradeSystem.SetDrams(p, drams); return p; }
        static Entity Factor => factory.CreateEntity("ConcordFactor");
        static Entity Envoy => factory.CreateEntity("SaccharineEnvoy");
        static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = kind, Limit = 50 }).Records.Count;

        [Test] public void TheFactorExtendsCreditAndEveryPostKeepsTheLedger()
        {
            Assert.True(ConcordAccount.ExtendsCredit(Factor)); Assert.True(ConcordAccount.KeepsLedger(Factor));
            Assert.False(ConcordAccount.ExtendsCredit(Envoy)); Assert.True(ConcordAccount.KeepsLedger(Envoy));
            var merchant = factory.CreateEntity("Merchant");
            Assert.False(ConcordAccount.ExtendsCredit(merchant)); Assert.False(ConcordAccount.KeepsLedger(merchant), "counter-check: a merchant keeps no Concord ledger");
            Assert.False(ConcordAccount.KeepsLedger(factory.CreateEntity("GlassScorpion")), "Concord faction is not enough: a scorpion keeps no books");
        }

        [TestCase(0, 20)] [TestCase(30, 35)] [TestCase(50, 45)] [TestCase(150, 95)] [TestCase(200, 120)] [TestCase(-40, 0)] [TestCase(-50, 0)] [TestCase(-150, 0)]
        public void AStrangerGetsASmallLineThatGrowsWithStanding(int standing, int line)
        {
            PlayerReputation.Set(Concord, standing);
            Assert.AreEqual(line, ConcordAccount.Line(Player()));
        }

        [Test] public void BorrowingCreditsTheDramsAndOpensTheAccount()
        {
            var player = Player(10);
            Assert.IsNull(ConcordAccount.Borrow(Factor, player, 20, 100));
            Assert.AreEqual(30, TradeSystem.GetDrams(player));
            Assert.AreEqual(24, ConcordAccount.Debt(player), "principal plus the 20% fee");
            Assert.AreEqual(100 + ConcordAccount.TermTicks, ConcordAccount.Due(player));
            Assert.AreEqual(0, ConcordAccount.Available(player), "one account at a time");
            Assert.AreEqual(1, Count("ConcordCredit"));
            Assert.True(DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Credited", Limit = 5 }).Records.Any(r => r.PayloadJson.Contains("concord-credit")), "through the ledger");
        }

        [TestCase(0, "invalid_amount")] [TestCase(-5, "invalid_amount")] [TestCase(21, "over_line")]
        public void ABadAmountChangesNothing(int amount, string reason)
        {
            var player = Player(10);
            Assert.AreEqual(reason, ConcordAccount.Borrow(Factor, player, amount, 0));
            Assert.AreEqual(10, TradeSystem.GetDrams(player)); Assert.AreEqual(0, ConcordAccount.Debt(player));
            Assert.AreEqual(1, Count("ConcordRefused"));
        }

        [Test] public void OneOpenAccountAtATimeAndOnlyAFactorLends()
        {
            var player = Player();
            Assert.AreEqual("not_a_factor", ConcordAccount.Borrow(Envoy, player, 10, 0));
            Assert.IsNull(ConcordAccount.Borrow(Factor, player, 10, 0));
            Assert.AreEqual("open_account", ConcordAccount.Borrow(Factor, player, 5, 10));
            Assert.AreEqual(10, TradeSystem.GetDrams(player));
        }

        [Test] public void SettlingOnTimeAfterADayIsHonoured()
        {
            var player = Player(100);
            ConcordAccount.Borrow(Factor, player, 20, 0);
            int standing = ConcordAccount.Standing;
            Assert.IsNull(ConcordAccount.Repay(Envoy, player, 24, ConcordAccount.HonorMinTicks + 5), "any post takes payment");
            Assert.AreEqual(96, TradeSystem.GetDrams(player), "borrowed 20, repaid 24: the fee is the Concord's");
            Assert.AreEqual(0, ConcordAccount.Debt(player));
            Assert.AreEqual(standing + ConcordAccount.Honor, ConcordAccount.Standing);
            Assert.AreEqual(1, ConcordAccount.Honored(player)); Assert.AreEqual(1, Count("ConcordHonored"));
            Assert.AreEqual(ConcordAccount.Line(player), ConcordAccount.Available(player), "the account can open again");
        }

        /// <summary>Anti-farm: a debt repaid before it was carried a day earns no standing.</summary>
        [Test] public void RepayingAtOnceEarnsNoHonour()
        {
            var player = Player(100);
            ConcordAccount.Borrow(Factor, player, 20, 0);
            ConcordAccount.Repay(Factor, player, 24, ConcordAccount.HonorMinTicks - 1);
            Assert.AreEqual(0, ConcordAccount.Standing); Assert.AreEqual(0, ConcordAccount.Honored(player)); Assert.AreEqual(0, ConcordAccount.Debt(player));
        }

        [Test] public void PayingWhatYouCarryLeavesTheRestOwed()
        {
            var player = Player(0);
            ConcordAccount.Borrow(Factor, player, 20, 0);
            TradeSystem.SetDrams(player, 15);
            Assert.IsNull(ConcordAccount.Repay(Factor, player, 24, 2000));
            Assert.AreEqual(0, TradeSystem.GetDrams(player)); Assert.AreEqual(9, ConcordAccount.Debt(player));
            Assert.AreEqual(0, ConcordAccount.Standing, "no honour for part of a debt");
            Assert.AreEqual("insufficient_currency", ConcordAccount.Repay(Factor, player, 9, 2001), "nothing to pay with");
        }

        [Test] public void ALateDebtDefaultsOnceAndClosesTheLine()
        {
            var player = Player(0);
            ConcordAccount.Borrow(Factor, player, 20, 0);
            int due = ConcordAccount.Due(player);
            Assert.False(ConcordAccount.Review(player, due), "due today is not late");
            Assert.True(ConcordAccount.Review(player, due + 1));
            Assert.True(ConcordAccount.InDefault(player)); Assert.AreEqual(-ConcordAccount.DefaultPenalty, ConcordAccount.Standing);
            Assert.False(ConcordAccount.Review(player, due + 5000), "the penalty is taken once");
            Assert.AreEqual(-ConcordAccount.DefaultPenalty, ConcordAccount.Standing);
            Assert.AreEqual(0, ConcordAccount.Line(player)); Assert.AreEqual(1, Count("ConcordDefaulted"));
            TradeSystem.SetDrams(player, 0);
            Assert.AreEqual("in_default", ConcordAccount.Borrow(Factor, player, 5, due + 6000));
        }

        [Test] public void SettlingADefaultClearsTheMarkWithoutRestoringStanding()
        {
            var player = Player(0);
            ConcordAccount.Borrow(Factor, player, 20, 0);
            TradeSystem.SetDrams(player, 50);
            Assert.IsNull(ConcordAccount.Repay(Factor, player, 24, ConcordAccount.TermTicks + 10), "repaying late reviews first");
            Assert.False(ConcordAccount.InDefault(player)); Assert.AreEqual(0, ConcordAccount.Debt(player));
            Assert.AreEqual(-ConcordAccount.DefaultPenalty, ConcordAccount.Standing, "a late payment is still a default");
            Assert.AreEqual(0, Count("ConcordHonored"));
            Assert.AreEqual(ConcordAccount.Line(player), ConcordAccount.Available(player));
            Assert.Greater(ConcordAccount.Line(player), 0, "credit reopens at the lowered standing");
        }

        /// <summary>Travel reviews the account: entering a zone after the due day finds the default.</summary>
        [Test] public void EnteringAZoneReviewsTheAccount()
        {
            var player = Player(0); var zone = new Zone("Overworld.10.10.0");
            ConcordAccount.Borrow(Factor, player, 20, 0);
            EconomyUpkeep.OnZoneEntered(player, zone, ConcordAccount.TermTicks);
            Assert.False(ConcordAccount.InDefault(player), "counter-check: not yet late");
            EconomyUpkeep.OnZoneEntered(player, zone, ConcordAccount.TermTicks + 1);
            Assert.True(ConcordAccount.InDefault(player));
        }

        /// <summary>The live zone-entry path calls the upkeep (InputHandler is a MonoBehaviour; this pins the call).</summary>
        [Test] public void TheZoneTransitionCallsTheEconomyUpkeep()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Presentation/Input/InputHandler.cs"));
            int start = text.IndexOf("private void HandleZoneTransition(", System.StringComparison.Ordinal);
            int end = text.IndexOf("\n        private ", start + 10, System.StringComparison.Ordinal);
            int call = text.IndexOf("EconomyUpkeep.OnZoneEntered(PlayerEntity, result.NewZone", start, System.StringComparison.Ordinal);
            Assert.Greater(start, 0); Assert.Greater(call, start, "the transition calls the upkeep");
            Assert.True(end < 0 || call < end, "inside HandleZoneTransition");
            Assert.AreEqual(-1, text.IndexOf("TraderRestockSystem.RestockZone(", System.StringComparison.Ordinal), "restock goes through the upkeep only");
        }

        [Test] public void TheAccountSurvivesSaveAndLoad()
        {
            var player = Player(0);
            ConcordAccount.Borrow(Factor, player, 20, 100);
            ConcordAccount.Review(player, 100 + ConcordAccount.TermTicks + 1);
            var loaded = PartRoundTripHelper.RoundTripEntity(player);
            Assert.AreEqual(ConcordAccount.Debt(player), ConcordAccount.Debt(loaded));
            Assert.AreEqual(ConcordAccount.Due(player), ConcordAccount.Due(loaded));
            Assert.True(ConcordAccount.InDefault(loaded));
            Assert.AreEqual(player.GetIntProperty(ConcordAccount.OpenedProp), loaded.GetIntProperty(ConcordAccount.OpenedProp));
        }

        [Test] public void TheStatementReadsTheAccount()
        {
            var player = Player(0);
            StringAssert.Contains("Nothing owed", ConcordAccount.Statement(player, 0));
            StringAssert.Contains("credit line 20", ConcordAccount.Statement(player, 0));
            ConcordAccount.Borrow(Factor, player, 20, 0);
            StringAssert.Contains("due in 2 days", ConcordAccount.Statement(player, 1));
            StringAssert.Contains("due today", ConcordAccount.Statement(player, ConcordAccount.TermTicks - 10));
            ConcordAccount.Review(player, ConcordAccount.TermTicks + 1);
            StringAssert.Contains("In default", ConcordAccount.Statement(player, ConcordAccount.TermTicks + 1));
        }

        // ════════════════ in conversation ════════════════

        static ConversationData Simple(string id)
        {
            var conv = new ConversationData { ID = id };
            conv.Nodes.Add(new NodeData { ID = "Start", Text = "The post is open.", Choices = new List<ChoiceData> { new ChoiceData { Text = "More.", Target = "More" } } });
            conv.Nodes.Add(new NodeData { ID = "More", Text = "Yes?", Choices = new List<ChoiceData> { new ChoiceData { Text = "Back.", Target = "Start" } } });
            return conv;
        }
        static List<ChoiceData> With(string action) =>
            ConversationManager.VisibleChoices.Where(c => c.Actions != null && c.Actions.Any(a => a.Key == action)).ToList();

        [Test] public void TheFactorOffersCreditAndThenSettlement()
        {
            ConversationLoader.Register(Simple("ConcordFactor_1"));
            var factor = Factor; var player = Player(0);
            Assert.True(ConversationManager.StartConversation(factor, player));
            var offers = With(ConcordAccount.BorrowAction);
            Assert.AreEqual(2, offers.Count, "half the line and all of it");
            StringAssert.Contains("Advance me 10 drams (owe 12", offers[0].Text);
            StringAssert.Contains("Advance me 20 drams (owe 24", offers[1].Text);
            StringAssert.Contains("credit line 20", ConversationManager.CurrentText, "the greeting reads the account");
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(offers[1]));
            Assert.AreEqual(20, TradeSystem.GetDrams(player)); Assert.AreEqual(24, ConcordAccount.Debt(player));
            Assert.IsEmpty(With(ConcordAccount.BorrowAction), "no more credit while the account is open");
            var settle = With(ConcordAccount.RepayAction);
            Assert.AreEqual(1, settle.Count); StringAssert.Contains("Pay 20 of the 24", settle[0].Text);
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "More"));
            Assert.IsEmpty(With(ConcordAccount.RepayAction), "only the greeting carries the ledger");
            StringAssert.DoesNotContain("credit line", ConversationManager.CurrentText);
        }

        [Test] public void AnEnvoyTakesPaymentButLendsNothing()
        {
            ConversationLoader.Register(Simple("SaccharineEnvoy_1"));
            var player = Player(50);
            var envoy = Envoy;
            Assert.True(ConversationManager.StartConversation(envoy, player));
            Assert.IsEmpty(With(ConcordAccount.BorrowAction)); Assert.IsEmpty(With(ConcordAccount.RepayAction), "nothing owed");
            ConversationManager.EndConversation();
            ConcordAccount.Borrow(Factor, player, 20, 0);
            Assert.True(ConversationManager.StartConversation(envoy, player));
            var settle = With(ConcordAccount.RepayAction); Assert.AreEqual(1, settle.Count);
            StringAssert.Contains("Settle my account: 24", settle[0].Text);
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(settle[0]));
            Assert.AreEqual(0, ConcordAccount.Debt(player)); Assert.AreEqual(46, TradeSystem.GetDrams(player));
        }

        /// <summary>The choices never lie: no payment offer to an empty purse, nothing at a merchant.</summary>
        [Test] public void NoChoiceIsOfferedThatCannotBeKept()
        {
            ConversationLoader.Register(Simple("ConcordFactor_1")); ConversationLoader.Register(Simple("Merchant_1"));
            var player = Player(0);
            ConcordAccount.Borrow(Factor, player, 20, 0); TradeSystem.SetDrams(player, 0);
            Assert.True(ConversationManager.StartConversation(Factor, player));
            Assert.IsEmpty(With(ConcordAccount.RepayAction)); Assert.IsEmpty(With(ConcordAccount.BorrowAction));
            ConversationManager.EndConversation();
            Assert.True(ConversationManager.StartConversation(factory.CreateEntity("Merchant"), player));
            Assert.IsEmpty(With(ConcordAccount.RepayAction)); StringAssert.DoesNotContain("credit line", ConversationManager.CurrentText);
        }

        [TestCase("abc")] [TestCase("")] [TestCase("-3")]
        public void AMalformedAmountIsRefused(string argument)
        {
            var player = Player(10);
            Assert.NotNull(ConversationActions.ExecuteRequired(ConcordAccount.BorrowAction, Factor, player, argument));
            Assert.AreEqual(10, TradeSystem.GetDrams(player)); Assert.AreEqual(0, ConcordAccount.Debt(player));
            Assert.NotNull(ConversationActions.ExecuteRequired(ConcordAccount.RepayAction, Factor, player, argument));
        }
    }
}
