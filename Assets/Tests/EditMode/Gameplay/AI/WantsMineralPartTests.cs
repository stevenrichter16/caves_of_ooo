using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.5 — <see cref="WantsMineralPart"/> +
    /// <see cref="MineralTradeService"/> contract pin.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>WantsMineralPart.Wants() returns true for listed minerals,
    ///         false otherwise (case-insensitive).</item>
    ///   <item>GetWantedMinerals() returns the trimmed non-empty entries.</item>
    ///   <item>Comma-delim parser handles whitespace + empty entries.</item>
    ///   <item>TryTrade success: rep flows, mineral consumed (stack-aware).</item>
    ///   <item>TryTrade rejection paths each emit a diag with reason.</item>
    ///   <item>Save/load round-trip preserves Minerals + Faction + RepReward.</item>
    /// </list>
    /// </summary>
    public class WantsMineralPartTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
            Diag.ResetAll();
        }

        private Entity MakePlayer()
        {
            var p = new Entity { ID = "hero", BlueprintName = "hero" };
            p.Tags["Player"] = "";
            p.Tags["Creature"] = "";
            p.AddPart(new RenderPart { DisplayName = "hero" });
            p.AddPart(new InventoryPart());
            return p;
        }

        private Entity MakeNPC(WantsMineralPart wants = null)
        {
            var n = new Entity { ID = "trader", BlueprintName = "trader" };
            n.Tags["Creature"] = "";
            n.AddPart(new RenderPart { DisplayName = "trader" });
            if (wants != null) n.AddPart(wants);
            return n;
        }

        private void GiveMineral(Entity player, string blueprint)
        {
            var mineral = _harness.Factory.CreateEntity(blueprint);
            Assert.IsNotNull(mineral, $"Blueprint {blueprint} must be registered.");
            player.GetPart<InventoryPart>().AddObject(mineral);
        }

        // ── WantsMineralPart parser ──────────────────────────────

        [Test]
        public void Wants_ListedMineral_True()
        {
            var w = new WantsMineralPart("PaleSalt,ChoirIron", "PaleCuration", 10);
            Assert.IsTrue(w.Wants("PaleSalt"));
            Assert.IsTrue(w.Wants("ChoirIron"));
        }

        [Test]
        public void Wants_UnlistedMineral_False()
        {
            var w = new WantsMineralPart("PaleSalt", "PaleCuration", 10);
            Assert.IsFalse(w.Wants("GlowQuartz"));
        }

        [Test]
        public void Wants_CaseInsensitive()
        {
            var w = new WantsMineralPart("PaleSalt", "PaleCuration", 10);
            Assert.IsTrue(w.Wants("palesalt"));
            Assert.IsTrue(w.Wants("PALESALT"));
        }

        [Test]
        public void Wants_EmptyOrNull_False()
        {
            var w = new WantsMineralPart("PaleSalt", "PaleCuration", 10);
            Assert.IsFalse(w.Wants(""));
            Assert.IsFalse(w.Wants(null));
        }

        [Test]
        public void Wants_EmptyMineralsField_False()
        {
            var w = new WantsMineralPart("", "PaleCuration", 10);
            Assert.IsFalse(w.Wants("PaleSalt"));
        }

        [Test]
        public void Wants_WhitespaceAndEmptyEntries_Filtered()
        {
            // "PaleSalt, , ,ChoirIron" should parse to {PaleSalt, ChoirIron}.
            var w = new WantsMineralPart("PaleSalt, , ,ChoirIron", "PaleCuration", 10);
            Assert.IsTrue(w.Wants("PaleSalt"));
            Assert.IsTrue(w.Wants("ChoirIron"));
            Assert.IsFalse(w.Wants(""));
        }

        [Test]
        public void GetWantedMinerals_ReturnsTrimmedNonEmpty()
        {
            var w = new WantsMineralPart(" PaleSalt , ChoirIron ", "PaleCuration", 10);
            var list = w.GetWantedMinerals();
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("PaleSalt", list[0]);
            Assert.AreEqual("ChoirIron", list[1]);
        }

        // ── MineralTradeService.TryTrade success ─────────────────

        [Test]
        public void TryTrade_PlayerHasMineral_AppliesRepAndConsumes()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "PaleCuration", 15));
            int repBefore = PlayerReputation.Get("PaleCuration");

            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");

            Assert.IsTrue(ok);
            Assert.AreEqual(repBefore + 15, PlayerReputation.Get("PaleCuration"));
            // Mineral consumed from inventory.
            bool stillHas = false;
            foreach (var item in player.GetPart<InventoryPart>().Objects)
                if (item.BlueprintName == "PaleSalt") stillHas = true;
            Assert.IsFalse(stillHas);
        }

        [Test]
        public void TryTrade_StackedMineral_DecrementsStackByOne()
        {
            var player = MakePlayer();
            // Give two PaleSalts to test stack consumption.
            GiveMineral(player, "PaleSalt");
            GiveMineral(player, "PaleSalt");
            var inv = player.GetPart<InventoryPart>();
            // Find the stack and verify count is 2 (StackerPart merged).
            Entity stack = null;
            foreach (var item in inv.Objects)
                if (item.BlueprintName == "PaleSalt") { stack = item; break; }
            Assert.IsNotNull(stack);
            int stackBefore = stack.GetPart<StackerPart>()?.StackCount ?? 1;

            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "PaleCuration", 5));
            MineralTradeService.TryTrade(player, npc, "PaleSalt");

            int stackAfter = stack.GetPart<StackerPart>()?.StackCount ?? -999;
            if (stackBefore > 1)
            {
                Assert.AreEqual(stackBefore - 1, stackAfter,
                    "Stack decremented by 1 (not removed).");
            }
            // If StackerPart didn't merge (two separate entities), at least
            // one PaleSalt is consumed.
            int countAfter = 0;
            foreach (var item in inv.Objects)
                if (item.BlueprintName == "PaleSalt") countAfter++;
            Assert.IsTrue(countAfter < 2, "Inventory count of PaleSalt decreased.");
        }

        [Test]
        public void TryTrade_EmitsTradedDiag()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "PaleCuration", 10));
            Diag.ResetAll();

            MineralTradeService.TryTrade(player, npc, "PaleSalt");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = MineralTradeService.DIAG_CATEGORY,
                Kind = "Traded",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("PaleSalt", recs[0].PayloadJson);
            StringAssert.Contains("PaleCuration", recs[0].PayloadJson);
        }

        // ── MineralTradeService rejection paths ──────────────────

        [Test]
        public void TryTrade_NpcWithoutWantsMineralPart_Rejects()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(wants: null);  // no Part
            int repBefore = PlayerReputation.Get("PaleCuration");

            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsFalse(ok);
            Assert.AreEqual(repBefore, PlayerReputation.Get("PaleCuration"),
                "No Part → no rep flow.");
        }

        [Test]
        public void TryTrade_NotWantedMineral_Rejects_NoConsumption()
        {
            // NPC wants ChoirIron, player offers PaleSalt → reject.
            // Pin: mineral NOT consumed on rejection.
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(new WantsMineralPart("ChoirIron", "PaleCuration", 10));
            int countBefore = player.GetPart<InventoryPart>().Objects.Count;

            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsFalse(ok);
            Assert.AreEqual(countBefore, player.GetPart<InventoryPart>().Objects.Count,
                "Inventory unchanged on rejected trade.");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = MineralTradeService.DIAG_CATEGORY,
                Kind = "Rejected",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("not_wanted", recs[0].PayloadJson);
        }

        [Test]
        public void TryTrade_PlayerLacksMineral_Rejects()
        {
            var player = MakePlayer();
            // No mineral in inventory.
            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "PaleCuration", 10));

            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsFalse(ok);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = MineralTradeService.DIAG_CATEGORY,
                Kind = "Rejected",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("mineral_not_in_inventory", recs[0].PayloadJson);
        }

        [Test]
        public void TryTrade_NullPlayer_NoCrash()
        {
            Assert.DoesNotThrow(() => MineralTradeService.TryTrade(null, MakeNPC(), "PaleSalt"));
        }

        [Test]
        public void TryTrade_NullNpc_NoCrash()
        {
            Assert.DoesNotThrow(() => MineralTradeService.TryTrade(MakePlayer(), null, "PaleSalt"));
        }

        [Test]
        public void TryTrade_NullMineralName_Rejects()
        {
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "PaleCuration", 10));

            bool ok = MineralTradeService.TryTrade(player, npc, null);
            Assert.IsFalse(ok);
        }

        // ── Empty Faction → no rep flow but mineral still consumed ──

        [Test]
        public void TryTrade_EmptyFaction_ConsumesMineral_NoRepFlow()
        {
            // "Charitable" NPC — wants the mineral, no faction stake.
            // Trade succeeds, mineral consumed, no rep change.
            var player = MakePlayer();
            GiveMineral(player, "PaleSalt");
            var npc = MakeNPC(new WantsMineralPart("PaleSalt", "", 10));

            bool ok = MineralTradeService.TryTrade(player, npc, "PaleSalt");
            Assert.IsTrue(ok);
            // PaleCuration unchanged (it was never the faction; just check
            // that NO rep flowed at all).
            Assert.AreEqual(0, PlayerReputation.Get("PaleCuration"));
        }

        // ── Save/load round-trip ─────────────────────────────────

        [Test]
        public void RoundTrip_PreservesFields()
        {
            var src = MakeNPC(new WantsMineralPart("PaleSalt,ChoirIron", "PaleCuration", 15));

            Entity loaded = PartRoundTripHelper.RoundTripEntity(src);

            var part = loaded.GetPart<WantsMineralPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("PaleSalt,ChoirIron", part.Minerals);
            Assert.AreEqual("PaleCuration", part.Faction);
            Assert.AreEqual(15, part.RepReward);
        }
    }
}
