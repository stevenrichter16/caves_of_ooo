using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// TRADE STOCK FIX — content audit over the SHIPPED JSON.
    ///
    /// <para>The unit tests in <c>TraderStockTests</c> prove the part
    /// works against a synthetic table. They cannot see the real
    /// content, and the real content is where this feature actually
    /// failed: a live spawn of 20 NPCs found the Innkeeper and Scribe
    /// opening an EMPTY trade window even though both carried a
    /// perfectly good stock table. Every entry in those tables was
    /// chance-gated, so the roll legitimately produced nothing.</para>
    ///
    /// <para>This is the same defect class the container arc hit with
    /// <c>ReliquaryT1</c>: a table that parses, validates and rolls
    /// without error, yet yields an empty result the player reads as a
    /// broken feature. The invariant below is the fix's real contract —
    /// <b>every shop always has at least one thing on the shelf</b> —
    /// and it is pinned here so a future content edit that re-gates the
    /// staple entry breaks the build instead of the shop.</para>
    /// </summary>
    [TestFixture]
    public class TraderStockContentTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown()
        {
            LootTableRegistry.ResetForTests();
        }

        /// <summary>Snapshot of blueprint names — CreateEntity is
        /// called inside the loops, so iterate a copy.</summary>
        private List<string> BlueprintNames()
        {
            return new List<string>(_factory.Blueprints.Keys);
        }

        /// <summary>
        /// Collects every stock table named by a TraderPart in the
        /// shipped blueprints. Walking the real factory keeps this
        /// honest — it audits what the game will actually spawn, not a
        /// list duplicated in the test.
        /// </summary>
        private Dictionary<string, string> CollectTraderTables()
        {
            var found = new Dictionary<string, string>();
            foreach (var name in BlueprintNames())
            {
                Entity e;
                try { e = _factory.CreateEntity(name); }
                catch (System.Exception) { continue; }
                if (e == null) continue;
                var trader = e.GetPart<TraderPart>();
                if (trader == null || string.IsNullOrEmpty(trader.StockTable)) continue;
                found[name] = trader.StockTable;
            }
            return found;
        }

        [Test]
        public void EveryTraderNamesATableThatExists()
        {
            var traders = CollectTraderTables();
            Assert.Greater(traders.Count, 20,
                "the fix assigns a stock table to every talkable NPC");

            var missing = new List<string>();
            foreach (var kv in traders)
                if (LootTableRegistry.Get(kv.Value) == null)
                    missing.Add($"{kv.Key} -> {kv.Value}");

            CollectionAssert.IsEmpty(missing,
                "a trader pointing at a non-existent table opens an empty window: "
                + string.Join(", ", missing));
        }

        [Test]
        public void EveryTraderTableGuaranteesAtLeastOneItem()
        {
            // THE BUG, as an invariant. A table whose every entry is
            // chance-gated can roll nothing — which is exactly what the
            // Innkeeper and Scribe did in the live check. The staple
            // must be a direct Blueprint, not a TableRef: a 100%-chance
            // TableRef can still resolve into a sub-table that itself
            // rolls empty, which would move the bug rather than fix it.
            var traders = CollectTraderTables();
            var canRollEmpty = new List<string>();

            foreach (var kv in traders)
            {
                var table = LootTableRegistry.Get(kv.Value);
                if (table == null) continue;

                // Pick mode always draws MinPicks entries, so it is
                // guaranteed by construction. NOTE the condition is
                // PickOne AND MinPicks — RollInto branches on PickOne
                // alone (LootTables.cs:150) and MinPicks DEFAULTS TO 1,
                // so `PickOne || MinPicks > 0` would skip every table
                // and pass this test vacuously.
                if (table.PickOne && table.MinPicks > 0) continue;

                bool guaranteed = false;
                foreach (var entry in table.Entries)
                {
                    if (entry.Chance < 100) continue;
                    if (string.IsNullOrEmpty(entry.Blueprint)) continue;
                    if (entry.MaxCount < 1) continue;
                    guaranteed = true;
                    break;
                }

                if (!guaranteed) canRollEmpty.Add($"{kv.Key} -> {kv.Value}");
            }

            CollectionAssert.IsEmpty(canRollEmpty,
                "these shops can roll an empty shelf — the reported bug: "
                + string.Join(", ", canRollEmpty));
        }

        [Test]
        public void EveryTraderStaple_IsARealSpawnableBlueprint()
        {
            // Counter-check to the test above: "has an entry at 100%"
            // is worthless if that entry names a blueprint that does not
            // exist. ReliquaryT1 taught this one.
            var traders = CollectTraderTables();
            var broken = new List<string>();

            foreach (var kv in traders)
            {
                var table = LootTableRegistry.Get(kv.Value);
                if (table == null) continue;
                foreach (var entry in table.Entries)
                {
                    if (entry.Chance < 100 || string.IsNullOrEmpty(entry.Blueprint)) continue;
                    Entity item = null;
                    try { item = _factory.CreateEntity(entry.Blueprint); }
                    catch (System.Exception) { }
                    if (item == null) broken.Add($"{kv.Value}:{entry.Blueprint}");
                }
            }

            CollectionAssert.IsEmpty(broken,
                "a guaranteed staple that will not spawn is not guaranteed: "
                + string.Join(", ", broken));
        }

        [Test]
        public void EveryTrader_ActuallySpawnsWithGoodsAndAPurse_AcrossManySeeds()
        {
            // End-to-end over the real content, the way the live check
            // that found the bug did it — but deterministic and over 12
            // seeds so a lucky roll can't hide a re-gated staple.
            TraderPart.Factory = _factory;
            try
            {
                var traders = CollectTraderTables();
                var empties = new List<string>();

                for (int seed = 1; seed <= 12; seed++)
                {
                    TraderPart.Rng = new System.Random(seed);
                    foreach (var kv in traders)
                    {
                        var npc = _factory.CreateEntity(kv.Key);
                        var inv = npc.GetPart<InventoryPart>();
                        if (inv == null || inv.Objects.Count == 0)
                            empties.Add($"{kv.Key}(seed {seed})");
                        else if (TradeSystem.GetDrams(npc) <= 0)
                            empties.Add($"{kv.Key}(seed {seed}, no purse)");
                    }
                }

                CollectionAssert.IsEmpty(empties,
                    "the trade window must never open empty: "
                    + string.Join(", ", empties));
            }
            finally
            {
                TraderPart.Factory = null;
                TraderPart.Rng = null;
            }
        }

        [Test]
        public void ShopStampKeeper_IsNotDoubleStocked()
        {
            // COLD-EYE Q1 (symmetry) — found live, not by a unit test.
            // TraderPart guards against double-stocking with
            // `if (inv.Objects.Count > 0) return`, but that guard sat on
            // the wrong side of the ordering: LandmarkBuilder's shop:
            // marker calls CreateEntity (which fires ObjectCreated, so
            // TraderPart stocks the bare shelf) and THEN rolled the SAME
            // table again — shop:Weaponsmith:WeaponsmithStock names the
            // exact table the Weaponsmith's TraderPart already rolled.
            // Live evidence: the town Weaponsmith carried 11 goods
            // against 8 from a single roll.
            //
            // The assertion counts the GUARANTEED STAPLE rather than
            // total items. Comparing totals against a standalone roll
            // does not work — TraderPart.Rng is a shared static, so the
            // in-zone keeper rolls at a different RNG position and a
            // legitimately different total. The staple is exact: the
            // table grants Dagger at 100%, so one roll leaves exactly
            // one dagger on the shelf and two rolls leave two, whatever
            // the RNG does with the chance-gated rest.
            TraderPart.Factory = _factory;
            TraderPart.Rng = new System.Random(6);
            try
            {
                var stamp = new StructureStamp
                {
                    Name = "DoubleStockProbe",
                    Chance = 100,
                    Rows = new[] { "W" },
                    Legend = new Dictionary<char, string>
                        { { 'W', "shop:Weaponsmith:WeaponsmithStock" } },
                };
                var zone = new Zone("DoubleStock");
                Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new System.Random(6)));
                new LandmarkBuilder(BiomeType.Cave, 1,
                    new List<StructureStamp> { stamp })
                    .BuildZone(zone, _factory, new System.Random(6));

                Entity keeper = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "Weaponsmith") { keeper = e; break; }
                Assert.IsNotNull(keeper, "precondition: the stamp spawned the keeper");

                int daggers = 0;
                foreach (var item in keeper.GetPart<InventoryPart>().Objects)
                    if (item.BlueprintName == "Dagger") daggers++;

                Assert.AreEqual(1, daggers,
                    "WeaponsmithStock grants Dagger at 100%, so the shelf shows one "
                    + $"per roll — {daggers} means the stamp re-rolled a shelf "
                    + "TraderPart had already filled");
                Assert.AreEqual("WeaponsmithStock", keeper.GetProperty("ShopStockTable"),
                    "and the stamp's table still wins for restock");
            }
            finally
            {
                TraderPart.Factory = null;
                TraderPart.Rng = null;
            }
        }

        [Test]
        public void ShopStamp_StillStocksAKeeperWithoutATraderPart()
        {
            // Counter-check: the guard must skip only an ALREADY-FULL
            // shelf. A shopkeeper blueprint with no TraderPart (or one
            // whose roll came up short) must still be stocked by the
            // stamp, or the fix would empty every future shop.
            TraderPart.Factory = null;   // no part-driven stocking at all
            var stamp = new StructureStamp
            {
                Name = "BareKeeperProbe",
                Chance = 100,
                Rows = new[] { "W" },
                Legend = new Dictionary<char, string>
                    { { 'W', "shop:Weaponsmith:WeaponsmithStock" } },
            };
            var zone = new Zone("BareKeeper");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new System.Random(6)));
            new LandmarkBuilder(BiomeType.Cave, 1,
                new List<StructureStamp> { stamp })
                .BuildZone(zone, _factory, new System.Random(6));

            Entity keeper = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "Weaponsmith") { keeper = e; break; }
            Assert.IsNotNull(keeper);
            int daggers = 0;
            foreach (var item in keeper.GetPart<InventoryPart>().Objects)
                if (item.BlueprintName == "Dagger") daggers++;
            Assert.AreEqual(1, daggers,
                "a bare shelf still gets exactly one stamp roll — the guard "
                + "must skip only shelves that are already full");
        }

        [Test]
        public void EveryTalkableNPC_CanActuallyTrade()
        {
            // The user's report in its most direct form: they talked to
            // a merchant and an envoy and got nothing. Any NPC that can
            // be talked to should either be a real trader or not offer
            // the option at all — CanTrade is what decides, so run it
            // over every conversational NPC in the shipped content.
            TraderPart.Factory = _factory;
            TraderPart.Rng = new System.Random(7);
            try
            {
                var liars = new List<string>();
                int talkable = 0;

                foreach (var name in BlueprintNames())
                {
                    Entity e;
                    try { e = _factory.CreateEntity(name); }
                    catch (System.Exception) { continue; }
                    if (e == null || e.GetPart<ConversationPart>() == null) continue;
                    talkable++;

                    if (!ConversationManager.CanTrade(e)) continue;
                    var inv = e.GetPart<InventoryPart>();
                    bool hasSomething = (inv != null && inv.Objects.Count > 0)
                                        || TradeSystem.GetDrams(e) > 0;
                    if (!hasSomething) liars.Add(name);
                }

                Assert.Greater(talkable, 20, "the shipped content has talkable NPCs to audit");
                CollectionAssert.IsEmpty(liars,
                    "offering trade with neither goods nor coin is the bug: "
                    + string.Join(", ", liars));
            }
            finally
            {
                TraderPart.Factory = null;
                TraderPart.Rng = null;
            }
        }
    }
}
