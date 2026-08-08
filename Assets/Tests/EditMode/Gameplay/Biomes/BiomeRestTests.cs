using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A4 — rest &amp; reprieve
    /// (Docs/BIOME-OVERHAUL.md §2 A4, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Before this the game had NO recovery loop: no natural regen, no
    /// rest, beds were NPC-only ambience, the Innkeeper was mute
    /// (Innkeeper_1 was never authored), and the shrine explicitly
    /// didn't heal. Rest = full heal + the world clock advancing 60
    /// turns (relapse timers, trader restocks, and crop checks all key
    /// off TickCount — resting has a COST in world-time). User-approved
    /// model: instant, blocked when hostiles are near.
    /// </summary>
    [TestFixture]
    public class BiomeRestTests
    {
        private static EntityFactory _factory;
        private EntityFactory _savedHarvestFactory;
        private TurnManager _tm;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            FactionManager.Initialize();
            _tm = new TurnManager(); // sets TurnManager.Active
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        private Entity MakePlayer(Zone zone, int x, int y, int hp = 10, int maxHp = 40)
        {
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new InventoryPart { MaxWeight = 500 });
            player.AddPart(new StatusEffectsPart());
            player.SetTag("Player");
            var stat = new Stat { Owner = player, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = maxHp };
            player.Statistics["Hitpoints"] = stat;
            var speed = new Stat { Owner = player, Name = "Speed", BaseValue = 100, Min = 0, Max = 200 };
            player.Statistics["Speed"] = speed;
            zone.AddEntity(player, x, y);
            return player;
        }

        private Entity AddHostile(Zone zone, int x, int y)
        {
            var snapjaw = _factory.CreateEntity("Snapjaw");
            zone.AddEntity(snapjaw, x, y);
            return snapjaw;
        }

        // ── 1. RestSystem core ───────────────────────────────────

        [Test]
        public void Rest_HealsToFull_AdvancesClock_EmitsDiag()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10, hp: 10, maxHp: 40);
            int clockBefore = _tm.TickCount;

            Diag.ResetAll();
            bool ok = RestSystem.TryRest(player, zone, "campfire", out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(40, player.GetStat("Hitpoints").Value, "healed to full");
            Assert.AreEqual(clockBefore + RestSystem.RestClockTurns, _tm.TickCount,
                "the world moves while you sleep");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "furniture", Kind = "Rested", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("campfire", records[0].PayloadJson);
        }

        [Test]
        public void Rest_CuresBleeding()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10);
            player.GetPart<StatusEffectsPart>().ForceApplyEffect(new BleedingEffect());
            Assert.IsNotNull(player.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "precondition: bleeding");

            Assert.IsTrue(RestSystem.TryRest(player, zone, "campfire", out _));
            Assert.IsNull(player.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "bandaged up during the rest");
        }

        [Test]
        public void Rest_BlockedByNearbyHostile_NoHealNoClock_CounterCheck()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10, hp: 10, maxHp: 40);
            AddHostile(zone, 12, 10); // within HostileScanRadius
            int clockBefore = _tm.TickCount;

            Diag.ResetAll();
            bool ok = RestSystem.TryRest(player, zone, "campfire", out string reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(10, player.GetStat("Hitpoints").Value, "no heal while in danger");
            Assert.AreEqual(clockBefore, _tm.TickCount, "no time passes");

            var blocked = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "furniture", Kind = "RestBlocked", Limit = 5 }).Records;
            Assert.AreEqual(1, blocked.Count, "the reject path has its own diag record");
        }

        [Test]
        public void Rest_DistantHostile_DoesNotBlock()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 5, 10, hp: 10);
            AddHostile(zone, 70, 10); // far outside scan radius
            Assert.IsTrue(RestSystem.TryRest(player, zone, "campfire", out _),
                "a monster on the far side of the zone is not 'nearby'");
        }

        // ── 2. Campfire world action ─────────────────────────────

        [Test]
        public void Campfire_ExposesRestAction_AndRestWorks()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10, hp: 10, maxHp: 40);
            var campfire = _factory.CreateEntity("Campfire");
            Assert.IsNotNull(campfire);
            zone.AddEntity(campfire, 11, 10);

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "RestAtCampfire");
            e.SetParameter("Actor", (object)player);
            e.SetParameter("Zone", (object)zone);
            campfire.FireEvent(e);
            e.Release();

            Assert.AreEqual(40, player.GetStat("Hitpoints").Value,
                "resting at the fire heals to full");
        }

        // ── 3. Inn (RestAtInn conversation action) ───────────────

        [Test]
        public void RestAtInn_ChargesDrams_HealsAndBuffs()
        {
            var zone = new Zone("T");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var player = MakePlayer(zone, 10, 10, hp: 10, maxHp: 40);
                player.SetIntProperty(TradeSystem.CURRENCY_PROP, 50);

                ConversationActions.Execute("RestAtInn", null, player, "10");

                Assert.AreEqual(40, player.GetStat("Hitpoints").Value, "slept well");
                Assert.AreEqual(40, player.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                    "10 drams for the room — the game's first real dram sink");
                Assert.IsNotNull(player.GetPart<StatusEffectsPart>().GetEffect<WellRestedEffect>(),
                    "a night in a real bed leaves you quick on your feet");
            }
            finally
            {
                SettlementRuntime.ActiveZone = null;
            }
        }

        [Test]
        public void RestAtInn_InsufficientDrams_RefusedWithoutCharge_CounterCheck()
        {
            var zone = new Zone("T");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var player = MakePlayer(zone, 10, 10, hp: 10, maxHp: 40);
                player.SetIntProperty(TradeSystem.CURRENCY_PROP, 3);

                ConversationActions.Execute("RestAtInn", null, player, "10");

                Assert.AreEqual(10, player.GetStat("Hitpoints").Value, "no heal");
                Assert.AreEqual(3, player.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                    "no charge on refusal");
            }
            finally
            {
                SettlementRuntime.ActiveZone = null;
            }
        }

        [Test]
        public void WellRested_BoostsSpeed_AndReverts()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10);
            var effects = player.GetPart<StatusEffectsPart>();

            effects.ForceApplyEffect(new WellRestedEffect());
            Assert.AreEqual(110, player.GetStat("Speed").Value, "+10 while it lasts");

            effects.RemoveEffect<WellRestedEffect>();
            Assert.AreEqual(100, player.GetStat("Speed").Value, "clean revert");
        }

        [Test]
        public void Innkeeper_Conversation_IsAuthoredWithRoomOption()
        {
            // Closes the dangling reference: the Innkeeper blueprint has
            // pointed at "Innkeeper_1" since it shipped, but no such
            // conversation existed — a mute NPC in every village.
            ConversationLoader.Reset();
            try
            {
                var json = File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Conversations/Innkeeper.json"));
                StringAssert.Contains("\"Innkeeper_1\"", json);
                StringAssert.Contains("RestAtInn", json,
                    "the room-for-drams option is the point of the inn");
            }
            finally
            {
                ConversationLoader.Reset();
            }
        }

        // ── 4. Shrine donation ───────────────────────────────────

        [Test]
        public void Shrine_Donate_GrantsBlessingForDrams()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10);
            player.SetIntProperty(TradeSystem.CURRENCY_PROP, 20);
            var shrine = _factory.CreateEntity("Shrine");
            Assert.IsNotNull(shrine);
            zone.AddEntity(shrine, 11, 10);

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "DonateAtShrine");
            e.SetParameter("Actor", (object)player);
            e.SetParameter("Zone", (object)zone);
            shrine.FireEvent(e);
            e.Release();

            Assert.AreEqual(15, player.GetIntProperty(TradeSystem.CURRENCY_PROP, -1),
                "5 drams in the bowl");
            Assert.IsNotNull(player.GetPart<StatusEffectsPart>().GetEffect<StoneskinEffect>(),
                "the shrine's blessing hardens the skin");
        }

        [Test]
        public void Shrine_Donate_NoDrams_Refused_CounterCheck()
        {
            var zone = new Zone("T");
            var player = MakePlayer(zone, 10, 10);
            player.SetIntProperty(TradeSystem.CURRENCY_PROP, 2);
            var shrine = _factory.CreateEntity("Shrine");
            zone.AddEntity(shrine, 11, 10);

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "DonateAtShrine");
            e.SetParameter("Actor", (object)player);
            e.SetParameter("Zone", (object)zone);
            shrine.FireEvent(e);
            e.Release();

            Assert.AreEqual(2, player.GetIntProperty(TradeSystem.CURRENCY_PROP, -1));
            Assert.IsNull(player.GetPart<StatusEffectsPart>().GetEffect<StoneskinEffect>());
        }
    }
}
