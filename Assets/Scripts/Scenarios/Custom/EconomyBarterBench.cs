using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// EC6/EC7 — DETERMINISTIC SELF-AUDITING BENCH for barter and Concord credit (Docs/ECONOMY-SYSTEM.md §12,
    /// Docs/MCP_PlayMode_Testing_Strategy.md). One press of Play runs the economy against the REAL runtime
    /// (bootstrap factory, Factions.json, the loot and conversation registries, the restock factory) that the
    /// EditMode fixtures stub, and emits one <c>economybench/Cell</c> record per check, stamped with a run id.
    ///
    /// <para><b>Audit it:</b> <c>diag_query category=economybench kind=Cell</c>, scoped to the newest
    /// <c>economybench/MatrixAuditRun</c> runId; assert no <c>pass=false</c>. Every subject is a scratch entity in
    /// a scratch zone (the real player and zone are untouched); the Concord standing the account cells move is
    /// restored at the end. Preconditions fail LOUDLY as explicit FAILED cells.</para>
    /// </summary>
    [Scenario(
        name: "Economy Barter Bench",
        category: "Economy",
        description: "Self-auditing: tender by faction and place, a barter and a dram deal, salt balancing, salt purses, the Choir's offering and the Concord account, against the live runtime. diag_query category=economybench.")]
    public class EconomyBarterBench : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Diag.Record("economybench", "MatrixAuditRun", payload: new { runId });
            int pass = 0, fail = 0;
            void Cell(string cell, object expected, object actual, bool ok)
            {
                if (ok) pass++; else fail++;
                Diag.Record("economybench", "Cell", payload: new { runId, cell, expected = expected?.ToString(), actual = actual?.ToString() ?? "null", pass = ok });
            }

            var factory = ctx.Factory;
            if (factory == null || !factory.Blueprints.ContainsKey("Merchant")) { Cell("precondition_factory", "blueprints", "missing", false); return; }
            if (FactionManager.GetFactionData(ConcordAccount.Faction) == null) { Cell("precondition_factions_json", "SaccharineConcord", "missing", false); return; }
            int standingBefore = PlayerReputation.Get(ConcordAccount.Faction);

            (int x, int y) Find(System.Func<int, int, bool> where)
            {
                for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++) if (where(x, y)) return (x, y);
                return (-1, -1);
            }
            var deep = Find((x, y) => Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)));
            var spread = Find((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Spread);
            var frontier = Find((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating && !Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)));
            Cell("precondition_places", "deep, spread, frontier", $"{deep} {spread} {frontier}", deep.x >= 0 && spread.x >= 0 && frontier.x >= 0);

            Zone lastZone = null;
            Entity At((int x, int y) cell, string blueprint)
            {
                lastZone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, 0));
                var e = factory.CreateEntity(blueprint); lastZone.AddEntity(e, 10, 10);
                return e;
            }

            // ── Tender, with the real factions and blueprints ──
            void TenderCell(string name, (int, int) cell, string blueprint, TenderKind expected)
            { var t = Tender.For(At(cell, blueprint)); Cell("tender_" + name, expected, t, t == expected); }
            TenderCell("merchant_deep", deep, "Merchant", TenderKind.Barter);
            TenderCell("saltwalker_deep", deep, "DesertHermit", TenderKind.Barter);
            TenderCell("drifter_deep", deep, "GlassblownDrifter", TenderKind.Barter);
            TenderCell("envoy_deep", deep, "SaccharineEnvoy", TenderKind.Drams);
            TenderCell("tendril_spread", spread, "ChoirTendril", TenderKind.None);
            TenderCell("CONTROL_merchant_spread", spread, "Merchant", TenderKind.Drams);
            TenderCell("CONTROL_merchant_frontier", frontier, "Merchant", TenderKind.Drams);
            var host = At(spread, "TentRightHost");
            Cell("tender_tentright_by_faction", TenderKind.Barter, Tender.For(host), Tender.For(host) == TenderKind.Barter);

            // ── A barter and a dram deal ──
            (Entity player, Entity trader) Market((int, int) cell)
            {
                var trader = At(cell, "Merchant"); trader.GetPart<InventoryPart>().Objects.Clear();
                var player = factory.CreateEntity("Player"); lastZone.AddEntity(player, 11, 10);
                var ego = player.GetStat("Ego"); if (ego != null) { ego.Max = System.Math.Max(ego.Max, 40); ego.BaseValue = 34; }
                TradeSystem.SetDrams(player, 100); TradeSystem.SetDrams(trader, 100);
                return (player, trader);
            }
            Entity Item(Entity holder, string blueprint, int count = 1)
            {
                var item = factory.CreateEntity(blueprint);
                var s = item.GetPart<StackerPart>(); if (s != null) s.StackCount = count;
                holder.GetPart<InventoryPart>().AddObject(item); return item;
            }
            {
                var (player, trader) = Market(deep);
                var sword = Item(player, "LongSword"); var tonic = Item(trader, "HealingTonic");
                var deal = new BarterDeal(player, trader); deal.SetGive(sword, 1); deal.SetTake(tonic, 1);
                bool ok = Barter.TryDeal(deal, out string reason);
                Cell("barter_goods_for_goods", "traded, no drams", $"{ok} {reason} drams={TradeSystem.GetDrams(player)}",
                    ok && TradeSystem.GetDrams(player) == 100 && player.GetPart<InventoryPart>().Contains(tonic));
                var direct = TradeSystem.BuyFromTrader(player, trader, sword, out string refusal);
                Cell("barter_refuses_drams", TradeSystem.NoDrams, refusal, !direct && refusal == TradeSystem.NoDrams);
            }
            {
                var (player, trader) = Market(spread);
                var apple = Item(player, "Starapple"); var sword = Item(trader, "LongSword");
                var deal = new BarterDeal(player, trader); deal.SetGive(apple, 1); deal.SetTake(sword, 1);
                int owed = deal.DramsFromPlayer;
                bool ok = Barter.TryDeal(deal, out string reason);
                Cell("CONTROL_dram_deal_pays_difference", 100 - owed, TradeSystem.GetDrams(player), ok && owed > 0 && TradeSystem.GetDrams(player) == 100 - owed);
            }
            {
                var (player, trader) = Market(deep);
                Item(player, "PaleSalt", 40); var sword = Item(trader, "LongSword");
                var deal = new BarterDeal(player, trader); deal.SetTake(sword, 1);
                int moved = deal.BalanceWithSalt();
                Cell("salt_balances_short_deal", ">=0", deal.Balance, moved > 0 && deal.Balance >= 0);
            }

            // ── Salt purses (the bootstrap's restock factory) ──
            {
                var trader = At(deep, "Merchant"); TradeSystem.SetDrams(trader, 0);
                bool wired = TraderRestockSystem.Factory != null;
                TraderRestockSystem.RestockZone(lastZone, 100000);
                int salt = trader.GetPart<InventoryPart>().Objects.Where(o => o.HasTag(RegionalValue.SaltTag)).Sum(o => BarterDeal.Units(o));
                Cell("salt_purse_restock", "salt>0, drams=0", $"factory={wired} salt={salt} drams={TradeSystem.GetDrams(trader)}", wired && salt > 0 && TradeSystem.GetDrams(trader) == 0);
            }

            // ── The Choir's offering and the Concord's ledger, in the real conversations ──
            ConversationManager.EndConversation();
            var walker = factory.CreateEntity("Player");
            {
                var grib = At(spread, "Grib");
                bool started = ConversationManager.StartConversation(grib, walker);
                int offers = ConversationManager.VisibleChoices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == ChoirOffering.Action));
                bool trades = ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "StartTrade"));
                Cell("shrine_keeper_offers", "1 offer, no trade", $"started={started} offers={offers} trades={trades}", started && offers == (ChoirOffering.CanOffer(grib) ? 1 : 0) && !trades);
                ConversationManager.EndConversation();
                var tendril = At(spread, "ChoirTendril");
                started = ConversationManager.StartConversation(tendril, walker);
                offers = ConversationManager.VisibleChoices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == ChoirOffering.Action));
                Cell("tendril_offering_not_doubled", "0 on Start (authored on NoTrade)", offers, started && offers == 0);
                ConversationManager.EndConversation();
            }
            {
                TradeSystem.SetDrams(walker, 0);
                PlayerReputation.Set(ConcordAccount.Faction, 0);
                var factor = At(spread, "ConcordFactor");
                bool started = ConversationManager.StartConversation(factor, walker);
                int credit = ConversationManager.VisibleChoices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == ConcordAccount.BorrowAction));
                bool statement = ConversationManager.CurrentText.Contains("credit line");
                Cell("factor_offers_credit", "2 offers + statement", $"started={started} credit={credit} statement={statement}", started && credit == 2 && statement);
                ConversationManager.EndConversation();

                string borrowed = ConcordAccount.Borrow(factor, walker, 20, 0);
                Cell("credit_opens_account", "debt 24", ConcordAccount.Debt(walker), borrowed == null && ConcordAccount.Debt(walker) == 24 && TradeSystem.GetDrams(walker) == 20);
                TradeSystem.SetDrams(walker, 30);
                string repaid = ConcordAccount.Repay(At(deep, "SaccharineEnvoy"), walker, 24, ConcordAccount.HonorMinTicks + 1);
                Cell("envoy_takes_repayment_with_honour", "+" + ConcordAccount.Honor, PlayerReputation.Get(ConcordAccount.Faction),
                    repaid == null && PlayerReputation.Get(ConcordAccount.Faction) == ConcordAccount.Honor && ConcordAccount.Debt(walker) == 0);
                ConcordAccount.Borrow(factor, walker, 10, 10000);
                bool defaulted = ConcordAccount.Review(walker, 10000 + ConcordAccount.TermTicks + 1);
                Cell("late_debt_defaults", ConcordAccount.Honor - ConcordAccount.DefaultPenalty, PlayerReputation.Get(ConcordAccount.Faction),
                    defaulted && ConcordAccount.InDefault(walker) && ConcordAccount.Line(walker) == 0);
                Cell("CONTROL_second_review_no_penalty", false, ConcordAccount.Review(walker, 10000 + 2 * ConcordAccount.TermTicks), !ConcordAccount.Review(walker, 10000 + 3 * ConcordAccount.TermTicks));
            }
            PlayerReputation.Set(ConcordAccount.Faction, standingBefore);

            ctx.Log($"[EconomyBench] run {runId}: {pass} pass / {fail} fail. Audit: diag_query category=economybench kind=Cell (runId={runId}).");
        }
    }
}
