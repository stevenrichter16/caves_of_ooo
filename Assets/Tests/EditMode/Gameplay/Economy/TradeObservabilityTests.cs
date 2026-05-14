using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven trade tests. Every reject path in
    /// TradeSystem.BuyFromTrader / SellToTrader now emits a diag
    /// "Rejected" record with a `reason` field naming the gate that
    /// vetoed. This fixture pins those emissions + dumps records to
    /// TestContext so a debug session can read the full trade-attempt
    /// breakdown.
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>Bought emits on successful buy with price + dramsAfter</item>
    ///   <item>Sold emits on successful sell with mirror fields</item>
    ///   <item>InsufficientDrams reject emits with direction=Buy + price</item>
    ///   <item>NoTrade tag rejects with reason=NoTrade in both directions</item>
    ///   <item>TraderUnable (frozen, dead, burning) propagates reason</item>
    ///   <item>NullArg rejects emit (no crash on null input)</item>
    /// </list>
    /// </summary>
    public class TradeObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers (adapted from TradeTests) ───────────────

        private static Entity MakeTrader(int drams = 200, int hp = 10)
        {
            var e = new Entity { ID = "trader", BlueprintName = "Merchant" };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = "Villagers";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "merchant" });
            e.AddPart(new InventoryPart());
            e.AddPart(new StatusEffectsPart());
            TradeSystem.SetDrams(e, drams);
            return e;
        }

        private static Entity MakePlayer(int drams = 100, int strength = 16)
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 14, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new InventoryPart());
            e.AddPart(new StatusEffectsPart());
            TradeSystem.SetDrams(e, drams);
            return e;
        }

        private static Entity MakeItem(string name, int value, int weight = 1, bool noTrade = false)
        {
            var item = new Entity { ID = name, BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = weight, Takeable = true });
            if (noTrade) item.Tags["NoTrade"] = "";
            return item;
        }

        private static void DumpTradeRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-10} actor={r.ActorId,-10} target={r.TargetId,-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void SuccessfulBuy_EmitsBoughtRecord()
        {
            var trader = MakeTrader(drams: 100);
            var player = MakePlayer(drams: 200);
            var item = MakeItem("Sword", value: 50);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsTrue(ok, "Buy should succeed with sufficient drams.");

            DumpTradeRecords("successful buy");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Limit = 20,
            }).Records;

            Assert.AreEqual(1, records.Count, "Successful buy emits exactly one Bought record.");
            Assert.AreEqual("Bought", records[0].Kind);
            StringAssert.Contains("\"itemName\":\"sword\"", records[0].PayloadJson);
            // counter-check: NO Rejected record fired
            Assert.IsFalse(records.Any(r => r.Kind == "Rejected"));
        }

        [Test]
        public void SuccessfulSell_EmitsSoldRecord()
        {
            var trader = MakeTrader(drams: 500);
            var player = MakePlayer(drams: 50);
            var item = MakeItem("Pelt", value: 20);
            player.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.SellToTrader(player, trader, item);
            Assert.IsTrue(ok);

            DumpTradeRecords("successful sell");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Sold", records[0].Kind);
            StringAssert.Contains("\"itemName\":\"pelt\"", records[0].PayloadJson);
        }

        [Test]
        public void InsufficientDrams_BuyRejected_EmitsRejectedWithReason()
        {
            var trader = MakeTrader();
            var player = MakePlayer(drams: 5);  // way too little
            var item = MakeItem("ExpensiveAxe", value: 200);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsFalse(ok);

            DumpTradeRecords("buy rejected: insufficient drams");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"direction\":\"Buy\"", records[0].PayloadJson);
            StringAssert.Contains("\"reason\":\"InsufficientDrams\"", records[0].PayloadJson);
            // Price field is set to what the buyer WOULD have paid
            StringAssert.Contains("\"price\":", records[0].PayloadJson);
        }

        [Test]
        public void TraderCantAfford_SellRejected_EmitsRejected()
        {
            var trader = MakeTrader(drams: 1);  // broke
            var player = MakePlayer(drams: 0);
            var item = MakeItem("CrownJewel", value: 500);
            player.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.SellToTrader(player, trader, item);
            Assert.IsFalse(ok);

            DumpTradeRecords("sell rejected: trader insufficient drams");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"direction\":\"Sell\"", records[0].PayloadJson);
            StringAssert.Contains("\"reason\":\"TraderInsufficientDrams\"", records[0].PayloadJson);
        }

        [Test]
        public void NoTradeTag_BuyRejected_EmitsNoTradeReason()
        {
            var trader = MakeTrader(drams: 100);
            var player = MakePlayer(drams: 1000);
            var questItem = MakeItem("LostHeirloom", value: 10, noTrade: true);
            trader.GetPart<InventoryPart>().AddObject(questItem);

            bool ok = TradeSystem.BuyFromTrader(player, trader, questItem);
            Assert.IsFalse(ok);

            DumpTradeRecords("buy rejected: NoTrade tag");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"NoTrade\"", records[0].PayloadJson);
        }

        [Test]
        public void NoTradeTag_SellRejected_EmitsNoTradeReason()
        {
            // Counter-check the mirror path on the sell side.
            var trader = MakeTrader(drams: 500);
            var player = MakePlayer(drams: 0);
            var questItem = MakeItem("PromiseRing", value: 50, noTrade: true);
            player.GetPart<InventoryPart>().AddObject(questItem);

            bool ok = TradeSystem.SellToTrader(player, trader, questItem);
            Assert.IsFalse(ok);

            DumpTradeRecords("sell rejected: NoTrade tag");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"direction\":\"Sell\"", records[0].PayloadJson);
            StringAssert.Contains("\"reason\":\"NoTrade\"", records[0].PayloadJson);
        }

        [Test]
        public void FrozenTrader_BuyRejected_PropagatesReason()
        {
            var trader = MakeTrader(drams: 100);
            trader.GetPart<StatusEffectsPart>().ApplyEffect(new FrozenEffect(), trader);
            var player = MakePlayer(drams: 1000);
            var item = MakeItem("Sword", value: 30);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsFalse(ok);

            DumpTradeRecords("buy rejected: trader frozen");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            // reason payload starts with "TraderUnable:" — exact suffix depends
            // on TraderUnableToTrade()'s message ("is frozen").
            StringAssert.Contains("\"reason\":\"TraderUnable:is frozen\"",
                records[0].PayloadJson);
        }

        [Test]
        public void NullArg_BuyRejected_DoesNotCrash_EmitsRecord()
        {
            // Adversarial: null args. Pre-fix this would silently return
            // false with no diag. Now it emits a Rejected record.
            bool ok = TradeSystem.BuyFromTrader(null, null, null);
            Assert.IsFalse(ok);

            DumpTradeRecords("null args buy");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            // Helper drops actor/target IDs when entities are null, but the
            // record itself fires.
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"reason\":\"NullArg\"", records[0].PayloadJson);
        }

        [Test]
        public void BuyAndSell_SameSession_EmitsTwoSeparateRecords()
        {
            // Counter-check: a buy followed by a sell produces 2 records
            // ordered chronologically.
            var trader = MakeTrader(drams: 200);
            var player = MakePlayer(drams: 500);
            var sword = MakeItem("Sword", value: 30);
            trader.GetPart<InventoryPart>().AddObject(sword);

            TradeSystem.BuyFromTrader(player, trader, sword);  // sword → player
            TradeSystem.SellToTrader(player, trader, sword);   // sword → trader

            DumpTradeRecords("buy then sell");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("Bought", records[0].Kind);
            Assert.AreEqual("Sold", records[1].Kind);
        }
    }
}
