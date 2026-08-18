using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Kind = CavesOfOoo.Core.ContainerPlacementService.ZoneKind;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LOOT OVERHAUL SM6 — the common placement algorithm.
    ///
    /// Before this, PopulationBuilder placed ZERO containers ever, and a
    /// tier-1 desert zone could not contain a single lootable object by
    /// construction (both its chest stamps were MinTier 2). These tests
    /// pin the budget, the exclusions, and determinism.
    /// </summary>
    [TestFixture]
    public class ContainerPlacementServiceTests
    {
        private const string Blueprints = @"
        {
          ""Objects"": [
            { ""Name"": ""PhysicalObject"", ""Parts"": [] },
            { ""Name"": ""Item"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] } ] },
            { ""Name"": ""GoldCoin"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""gold"" }, { ""Key"": ""RenderString"", ""Value"": ""$"" } ] } ] },
            { ""Name"": ""Floor"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""floor"" }, { ""Key"": ""RenderString"", ""Value"": ""."" } ] } ] },
            { ""Name"": ""Wall"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""wall"" }, { ""Key"": ""RenderString"", ""Value"": ""#"" } ] },
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Solid"", ""Value"": ""true"" } ] } ],
              ""Tags"": [ { ""Key"": ""Solid"", ""Value"": """" } ] },
            { ""Name"": ""StairsDown"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""stairs"" }, { ""Key"": ""RenderString"", ""Value"": "">"" } ] } ] },
            { ""Name"": ""Crate"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""crate"" }, { ""Key"": ""RenderString"", ""Value"": ""0"" } ] },
                { ""Name"": ""Container"", ""Params"": [ { ""Key"": ""MaxItems"", ""Value"": ""8"" } ] } ] },
            { ""Name"": ""Sack"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""Urn"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""StrongBox"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""OreCache"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""BoneCache"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""WovenBasket"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""HollowLog"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""Reliquary"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""Bookshelf"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""WeaponRack"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""AlchemyShelf"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""WoodenBarrel"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""MimicChest"", ""Inherits"": ""Crate"", ""Parts"": [] }
          ]
        }";

        private const string Tables = @"
        { ""Tables"": [
            { ""Name"": ""CrateT1"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""CrateT2"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""CrateT3"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""SackT1"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""SackT2"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""SackT3"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""OreCacheT1"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""OreCacheT2"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""OreCacheT3"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""StrongBoxT1"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""StrongBoxT2"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""StrongBoxT3"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] }
        ] }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(Blueprints);
            LootTableRegistry.Initialize(Tables);
            ContainerPlacementService.Factory = _factory;
        }

        [TearDown]
        public void TearDown() => ContainerPlacementService.Factory = null;

        /// <summary>An open room with a wall border.</summary>
        private Zone MakeRoom()
        {
            var zone = new Zone("T");
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                {
                    bool border = x == 0 || y == 0 || x == Zone.Width - 1 || y == Zone.Height - 1;
                    zone.AddEntity(_factory.CreateEntity(border ? "Wall" : "Floor"), x, y);
                }
            return zone;
        }

        private static int CountContainers(Zone zone)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.GetPart<ContainerPart>() != null) n++;
            return n;
        }

        // ── Budget ───────────────────────────────────────────────

        [Test]
        public void Budget_ScalesWithTier_AndZoneKind()
        {
            var rng = new System.Random(1);
            // base(wilderness)=1 + (tier-1) + jitter[0,1]
            Assert.That(ContainerPlacementService.ComputeBudget(Kind.Wilderness, 1, rng),
                Is.InRange(1, 2));
            Assert.That(ContainerPlacementService.ComputeBudget(Kind.Wilderness, 3, rng),
                Is.InRange(3, 4));
            // underground starts richer
            Assert.That(ContainerPlacementService.ComputeBudget(Kind.Underground, 1, rng),
                Is.InRange(2, 3));
        }

        [Test]
        public void Budget_IsClampedAtBothEnds()
        {
            var rng = new System.Random(2);
            Assert.GreaterOrEqual(ContainerPlacementService.ComputeBudget(Kind.Wilderness, -5, rng), 1,
                "a nonsense tier still yields a playable zone");
            Assert.LessOrEqual(ContainerPlacementService.ComputeBudget(Kind.Underground, 99, rng), 6,
                "no zone becomes a warehouse");
        }

        [Test]
        public void TableTier_ClampsToTheAuthoredRange()
        {
            Assert.AreEqual(1, ContainerPlacementService.ClampTableTier(0));
            Assert.AreEqual(3, ContainerPlacementService.ClampTableTier(8),
                "deep zones reuse the T3 tables rather than looking up a missing T8");
        }

        // ── Placement ────────────────────────────────────────────

        [Test]
        public void Populate_PlacesContainers_InAnOrdinaryWildernessZone()
        {
            // THE headline fix: before SM6 this number was always zero.
            var zone = MakeRoom();
            int placed = ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 1, Kind.Wilderness, new System.Random(11));

            Assert.Greater(placed, 0, "an ordinary wilderness zone finally has loot");
            Assert.AreEqual(placed, CountContainers(zone));
        }

        [Test]
        public void Populate_DesertTier1_IsNoLongerEmptyByConstruction()
        {
            // The absurdity the sweep found: both desert chest stamps
            // were MinTier 2, so a T1 desert zone could not contain a
            // single container.
            var zone = MakeRoom();
            Assert.Greater(ContainerPlacementService.Populate(
                zone, BiomeType.Desert, 1, Kind.Wilderness, new System.Random(3)), 0);
        }

        [Test]
        public void Populate_NeverPlacesOnStairsOrReservedCells()
        {
            var zone = MakeRoom();
            // Reserve the whole interior except a small strip, and put
            // stairs in the strip.
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (y != 5) zone.GenReservedCells.Add((x, y));
            zone.AddEntity(_factory.CreateEntity("StairsDown"), 10, 5);

            ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 3, Kind.Underground, new System.Random(9));

            foreach (var e in zone.GetAllEntities())
            {
                if (e.GetPart<ContainerPart>() == null) continue;
                var c = zone.GetEntityCell(e);
                Assert.AreEqual(5, c.Y, "placement stayed out of reserved cells");
                Assert.IsFalse(c.X == 10 && c.Y == 5, "and never landed on the stairs");
            }
        }

        [Test]
        public void Populate_NeverStacksTwoContainersOnOneCell()
        {
            var zone = MakeRoom();
            ContainerPlacementService.Populate(
                zone, BiomeType.Ruins, 3, Kind.Underground, new System.Random(21));

            var seen = new HashSet<(int, int)>();
            foreach (var e in zone.GetAllEntities())
            {
                if (e.GetPart<ContainerPart>() == null) continue;
                var c = zone.GetEntityCell(e);
                Assert.IsTrue(seen.Add((c.X, c.Y)), "one container per cell");
            }
        }

        [Test]
        public void Populate_StocksWhatItPlaces()
        {
            var zone = MakeRoom();
            ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 1, Kind.Wilderness, new System.Random(5));

            bool anyStocked = false;
            foreach (var e in zone.GetAllEntities())
            {
                var cp = e.GetPart<ContainerPart>();
                if (cp != null && cp.Contents.Count > 0) anyStocked = true;
            }
            Assert.IsTrue(anyStocked, "an empty chest is worse than no chest");
        }

        [Test]
        public void Populate_NeverLeavesAnEmptyContainer()
        {
            // Found LIVE, not by a test: every entry in the low-tier
            // tables is chance-gated, so ~1 container in 5 rolled up
            // completely empty. An empty chest spends the player's walk
            // and their expectation; the fallback floors it.
            for (int seed = 0; seed < 25; seed++)
            {
                var zone = MakeRoom();
                ContainerPlacementService.Populate(
                    zone, BiomeType.Cave, 1, Kind.Wilderness, new System.Random(seed));
                foreach (var e in zone.GetAllEntities())
                {
                    var cp = e.GetPart<ContainerPart>();
                    if (cp == null) continue;
                    Assert.Greater(cp.Contents.Count, 0,
                        $"seed {seed}: {e.BlueprintName} was placed empty");
                }
            }
        }

        [Test]
        public void Populate_IsDeterministicForAGivenSeed()
        {
            // Same seed -> same world. Non-negotiable for a game with
            // save/load and reproducible bug reports.
            var a = MakeRoom(); var b = MakeRoom();
            int pa = ContainerPlacementService.Populate(a, BiomeType.Jungle, 2, Kind.Wilderness, new System.Random(1234));
            int pb = ContainerPlacementService.Populate(b, BiomeType.Jungle, 2, Kind.Wilderness, new System.Random(1234));
            Assert.AreEqual(pa, pb);

            var posA = new List<string>(); var posB = new List<string>();
            foreach (var e in a.GetAllEntities())
                if (e.GetPart<ContainerPart>() != null)
                { var c = a.GetEntityCell(e); posA.Add($"{e.BlueprintName}@{c.X},{c.Y}"); }
            foreach (var e in b.GetAllEntities())
                if (e.GetPart<ContainerPart>() != null)
                { var c = b.GetEntityCell(e); posB.Add($"{e.BlueprintName}@{c.X},{c.Y}"); }
            posA.Sort(); posB.Sort();
            CollectionAssert.AreEqual(posA, posB, "identical seeds place identical containers");
        }

        [Test]
        public void Populate_NullFactoryOrZone_IsAGracefulNoOp()
        {
            var zone = MakeRoom();
            ContainerPlacementService.Factory = null;
            Assert.AreEqual(0, ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 1, Kind.Wilderness, new System.Random(1)));
            ContainerPlacementService.Factory = _factory;
            Assert.AreEqual(0, ContainerPlacementService.Populate(
                null, BiomeType.Cave, 1, Kind.Wilderness, new System.Random(1)));
            Assert.AreEqual(0, ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 1, Kind.Wilderness, null));
        }

        [Test]
        public void Populate_FullyWalledZone_PlacesNothingAndDoesNotHang()
        {
            // Adversarial: no legal cell at all. The attempt loop must
            // terminate rather than spin looking for one.
            var zone = new Zone("T");
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    zone.AddEntity(_factory.CreateEntity("Wall"), x, y);

            Assert.AreEqual(0, ContainerPlacementService.Populate(
                zone, BiomeType.Cave, 3, Kind.Underground, new System.Random(4)));
        }

        // ── Docs/FELLING-W1-W2-PLAN.md SM5 — the Spread's own pool ──

        [Test]
        public void Spread_WildernessContainers_DoNotUseJunglePool()
        {
            // Regression pin: before SM5, BiomeType.Spread fell through
            // to JunglePool — a hollow log or a woven basket in a
            // hedgerow. Sweep several seeds; every placed container must
            // come from {Crate, Sack, StrongBox}, never the jungle-only
            // kinds (WovenBasket, HollowLog, Urn — Sack is shared so it
            // proves nothing on its own, but the jungle-EXCLUSIVE names
            // must never appear).
            var junkFromJungle = new HashSet<string> { "WovenBasket", "HollowLog" };
            bool placedAny = false;

            for (int seed = 0; seed < 15; seed++)
            {
                var zone = MakeRoom();
                ContainerPlacementService.Populate(
                    zone, BiomeType.Spread, 1, Kind.Wilderness, new System.Random(seed));

                foreach (var e in zone.GetAllEntities())
                {
                    if (e.GetPart<ContainerPart>() == null) continue;
                    placedAny = true;
                    CollectionAssert.DoesNotContain(junkFromJungle, e.BlueprintName,
                        $"seed {seed}: '{e.BlueprintName}' is jungle-flavored, not settled-country");
                }
            }
            Assert.IsTrue(placedAny, "no container ever placed across 15 seeds — test can't prove anything");
        }

        [Test]
        public void Spread_VillageInteriorContainers_AreUnaffected()
        {
            // Counter-check: zone-kind must still win before biome is
            // even consulted (PoolFor's existing precedence, unchanged
            // by SM5 — pinned so a future refactor can't invert it).
            var zone = MakeRoom();
            int placed = ContainerPlacementService.Populate(
                zone, BiomeType.Spread, 1, Kind.Village, new System.Random(2));

            Assert.Greater(placed, 0, "Village-kind placement in a Spread zone should behave normally");
        }
    }
}
