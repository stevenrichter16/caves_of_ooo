using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.7 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the salt economy, live.
    /// Canon chain: "Tent-Right mines it; Pale Curation buys it; the
    /// Concord moves it" (Lore/Factions/04_SaccharineConcord.md:100).
    /// Both halves shipped unwired for months: PaleSaltVein only
    /// spawned underground (W2.1 put it on the pans) and
    /// MineralTradeService.TryTrade had ZERO production callers — the
    /// salt-master's SellMineral dialogue action is its first.
    /// </summary>
    public class SaltEconomyTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));
        }

        [TearDown]
        public void TearDown() => FactionManager.Reset();

        private static Entity Miner(Zone zone, bool withSalt)
        {
            var e = new Entity { ID = "miner", BlueprintName = "Player" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new InventoryPart());
            if (withSalt)
            {
                var salt = _factory.CreateEntity("PaleSalt");
                e.GetPart<InventoryPart>().AddObject(salt);
            }
            zone.AddEntity(e, 10, 10);
            return e;
        }

        [Test]
        public void SellingSalt_ConsumesIt_AndTheTentsRemember()
        {
            var zone = new Zone("Z");
            var miner = Miner(zone, withSalt: true);
            var master = _factory.CreateEntity("SaltMaster");
            zone.AddEntity(master, 11, 10);
            int repBefore = PlayerReputation.Get("TentRight");

            ConversationActions.Execute("SellMineral", master, miner, "PaleSalt");

            Assert.AreEqual(0, miner.GetPart<InventoryPart>().Objects.Count, "the salt is weighed away");
            Assert.Greater(PlayerReputation.Get("TentRight"), repBefore, "standing with the tents rises");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "mineral-trade", Kind = "Traded", Limit = 5 }).Records.Count);
        }

        [Test]
        public void SellingWithNoSalt_IsARefusal_NotACrash()
        {
            var zone = new Zone("Z");
            var miner = Miner(zone, withSalt: false);
            var master = _factory.CreateEntity("SaltMaster");
            zone.AddEntity(master, 11, 10);
            int repBefore = PlayerReputation.Get("TentRight");

            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("SellMineral", master, miner, "PaleSalt"));

            Assert.AreEqual(repBefore, PlayerReputation.Get("TentRight"), "no salt, no standing");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "mineral-trade", Kind = "Rejected", Limit = 5 }).Records.Count);
        }

        [Test]
        public void TheSaltMaster_IsWiredEndToEnd()
        {
            // Content-integrity gate: the blueprint carries the part, the
            // part wants the mineral, the conversation exists and its
            // choice really calls the action — so the loop can never
            // silently disconnect again.
            var master = _factory.CreateEntity("SaltMaster");
            var wants = master.GetPart<WantsMineralPart>();
            Assert.IsNotNull(wants, "the scales are the part");
            Assert.IsTrue(wants.Wants("PaleSalt"));
            Assert.AreEqual("TentRight", wants.Faction);

            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")));
            var conv = ConversationLoader.Get("SaltMaster_1");
            Assert.IsNotNull(conv, "SaltMaster_1 should load");
            bool wired = false;
            foreach (var c in conv.GetStartNode().Choices)
                if (c.Actions != null)
                    foreach (var a in c.Actions)
                        if (a.Key == "SellMineral" && a.Value == "PaleSalt") wired = true;
            Assert.IsTrue(wired, "the dialogue really sells");
            ConversationLoader.Reset();
        }

        [Test]
        public void PaleSalt_HasTradeValue_ForTheConcordLeg()
        {
            // The Concord leg of the chain rides the ordinary trade
            // screen — which prices by Commerce.Value. Pin that salt is
            // worth carrying.
            var salt = _factory.CreateEntity("PaleSalt");
            var commerce = salt.GetPart<CommercePart>();
            Assert.IsNotNull(commerce, "salt without a price is not an economy");
            Assert.Greater(commerce.Value, 0);
        }

        [Test]
        public void TheProfileCamp_SeatsTheSaltMaster()
        {
            var stamp = StampCatalog.TentRightProfileCamp();
            bool hasScales = false;
            foreach (var kvp in stamp.Legend)
                if (kvp.Value == "spawn:SaltMaster") hasScales = true;
            Assert.IsTrue(hasScales, "Wellmeet's camp has the scales");

            // Counter-check: the AMBIENT wilderness camp does not — the
            // trade lives where the towns are.
            bool ambientHasScales = false;
            foreach (var st in StampCatalog.For(BiomeType.Beating))
                if (st.Name == "TentRightCamp")
                    foreach (var kvp in st.Legend)
                        if (kvp.Value == "spawn:SaltMaster") ambientHasScales = true;
            Assert.IsFalse(ambientHasScales);
        }
    }
}
