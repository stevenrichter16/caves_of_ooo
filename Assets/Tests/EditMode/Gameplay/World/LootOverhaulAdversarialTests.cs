using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Kind = CavesOfOoo.Core.ContainerPlacementService.ZoneKind;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LOOT OVERHAUL SM8 — the dedicated adversarial sweep
    /// (CLAUDE.md §Adversarial test sweep; playbook in
    /// ADVERSARIAL_TESTING.md).
    ///
    /// <para>Five taxonomy surfaces apply to this feature, so the gate
    /// is mandatory: a <b>parser</b> (Loadout's three string formats),
    /// <b>state atomicity</b> (a spawn that fails mid-loadout),
    /// <b>save/load reach</b> (stocked containers must survive a
    /// round-trip), <b>boundary inputs</b> (0/100/negative chance,
    /// container MaxItems overflow), and <b>determinism</b> (same seed,
    /// same world).</para>
    ///
    /// <para>These probe bug CLASSES the per-milestone tests can't see,
    /// rather than re-asserting the spec.</para>
    /// </summary>
    [TestFixture]
    public class LootOverhaulAdversarialTests
    {
        private const string Blueprints = @"
        {
          ""Objects"": [
            { ""Name"": ""PhysicalObject"", ""Parts"": [] },
            { ""Name"": ""Item"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""2"" } ] } ] },
            { ""Name"": ""GoldCoin"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""gold"" }, { ""Key"": ""RenderString"", ""Value"": ""$"" } ] } ] },
            { ""Name"": ""Boulder"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""boulder"" }, { ""Key"": ""RenderString"", ""Value"": ""o"" } ] },
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""900"" } ] } ] },
            { ""Name"": ""ShortSword"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""short sword"" }, { ""Key"": ""RenderString"", ""Value"": ""/"" } ] },
                { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" } ] },
                { ""Name"": ""Equippable"", ""Params"": [ { ""Key"": ""Slot"", ""Value"": ""Hand"" } ] } ] },
            { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Body"", ""Params"": [ { ""Key"": ""Anatomy"", ""Value"": ""Humanoid"" } ] },
                { ""Name"": ""Inventory"", ""Params"": [ { ""Key"": ""MaxWeight"", ""Value"": ""150"" } ] } ] },
            { ""Name"": ""Hoarder"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""hoarder"" }, { ""Key"": ""RenderString"", ""Value"": ""h"" } ] },
                { ""Name"": ""Loadout"", ""Params"": [ { ""Key"": ""Carry"", ""Value"": ""Boulder:100x9-9"" } ] } ],
              ""Tags"": [ { ""Key"": ""Tier"", ""Value"": ""1"" } ] },
            { ""Name"": ""Crate"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""crate"" }, { ""Key"": ""RenderString"", ""Value"": ""0"" } ] },
                { ""Name"": ""Container"", ""Params"": [ { ""Key"": ""MaxItems"", ""Value"": ""2"" } ] } ] },
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
            { ""Name"": ""MimicChest"", ""Inherits"": ""Crate"", ""Parts"": [] },
            { ""Name"": ""Floor"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""floor"" }, { ""Key"": ""RenderString"", ""Value"": ""."" } ] } ] },
            { ""Name"": ""Wall"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""wall"" }, { ""Key"": ""RenderString"", ""Value"": ""#"" } ] } ],
              ""Tags"": [ { ""Key"": ""Solid"", ""Value"": """" } ] }
          ]
        }";

        // Every table a pool can request, so stocking never silently
        // no-ops on a missing table (the ReliquaryT1 defect, pinned).
        private const string Tables = @"
        { ""Tables"": [
            { ""Name"": ""Bulk"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100, ""MinCount"": 9, ""MaxCount"": 9 } ] },
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
            LoadoutPart.Factory = _factory;
            LoadoutPart.Rng = new System.Random(99);
            LootDropSystem.Factory = _factory;
            LootDropSystem.Rng = new System.Random(99);
            ContainerPlacementService.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            LoadoutPart.Factory = null; LoadoutPart.Rng = null;
            LootDropSystem.Factory = null; LootDropSystem.Rng = null;
            ContainerPlacementService.Factory = null;
        }

        private Zone MakeRoom()
        {
            var zone = new Zone("A");
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                {
                    bool border = x == 0 || y == 0 || x == Zone.Width - 1 || y == Zone.Height - 1;
                    zone.AddEntity(_factory.CreateEntity(border ? "Wall" : "Floor"), x, y);
                }
            return zone;
        }

        // ═══════════════ PARSER (malformed inputs) ═══════════════

        [Test]
        public void Adversarial_Parser_GibberishNeverThrowsAndNeverInvents()
        {
            string[] evil =
            {
                null, "", "   ", ";", ";;;;;;", ":", ":::", "::50",
                "Dagger:", "Dagger::", ":Dagger", "Dagger:abc",
                "Dagger:50x", "Dagger:50x-", "Dagger:50x-5",
                "Dagger:50xa-b", "Dagger:2147483648",
                "Dagger:50x2147483647-2147483647",
                ";Dagger:100;", "Dagger:100;;;Sword:50",
                "\t\n", "Dagger : 100", "DAGGER:100",
            };
            foreach (var raw in evil)
            {
                List<LoadoutPart.GrantSpec> specs = null;
                Assert.DoesNotThrow(() => specs = LoadoutPart.ParseCarry(raw), $"input: '{raw}'");
                Assert.IsNotNull(specs);
                foreach (var sp in specs)
                {
                    Assert.IsNotEmpty(sp.Blueprint, $"'{raw}' produced a nameless grant");
                    Assert.That(sp.Chance, Is.InRange(0, 100), $"'{raw}' chance out of range");
                    Assert.LessOrEqual(sp.MinCount, sp.MaxCount, $"'{raw}' inverted range survived");
                    Assert.GreaterOrEqual(sp.MinCount, 1, $"'{raw}' produced a zero-count grant");
                }
            }
        }

        [Test]
        public void Adversarial_Parser_WhitespaceIsToleratedAroundNames()
        {
            var specs = LoadoutPart.ParseCarry("  Dagger : 100 ;  Sword:50  ");
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual("Dagger", specs[0].Blueprint, "names are trimmed");
        }

        // ═══════════ STATE ATOMICITY / WEIGHT LIMITS ═════════════

        [Test]
        public void Adversarial_Loadout_OverweightGrant_LeavesACoherentCreature()
        {
            // 9 boulders at 900 weight each vs MaxCarryWeight. The
            // creature must still exist, still be spawnable, and not
            // hold a phantom item that AddObject refused.
            Entity hoarder = null;
            Assert.DoesNotThrow(() => hoarder = _factory.CreateEntity("Hoarder"));
            Assert.IsNotNull(hoarder);

            var inv = hoarder.GetPart<InventoryPart>();
            foreach (var obj in inv.Objects)
                Assert.IsNotNull(obj, "no null slots left behind by a refused add");
        }

        [Test]
        public void Adversarial_DeathDrop_RunsAfterAPartialLoadout()
        {
            // The two systems compose: an overweight creature still
            // dies cleanly and still drops whatever it did manage to
            // hold.
            var zone = MakeRoom();
            var hoarder = _factory.CreateEntity("Hoarder");
            zone.AddEntity(hoarder, 10, 10);

            // Count what was on the floor BESIDES the dying creature —
            // the creature itself is supposed to be removed.
            var cell = zone.GetCell(10, 10);
            int bystandersBefore = 0;
            foreach (var o in cell.Objects) if (o != hoarder) bystandersBefore++;

            Assert.DoesNotThrow(() => CombatSystem.HandleDeath(hoarder, null, zone));

            int bystandersAfter = 0;
            foreach (var o in cell.Objects) if (o != hoarder) bystandersAfter++;
            Assert.GreaterOrEqual(bystandersAfter, bystandersBefore,
                "death never REMOVES pre-existing floor contents");
            Assert.IsFalse(cell.Objects.Contains(hoarder),
                "and the corpse-maker itself is gone from the cell");
        }

        // ═══════════════ BOUNDARY INPUTS ═════════════════════════

        [Test]
        public void Adversarial_ContainerOverflow_IsSilentlyCapped_NotCorrupting()
        {
            // MaxItems = 2 in this fixture, table rolls 9. StockContainer
            // must cap without throwing and without reporting phantom
            // successes.
            var crate = _factory.CreateEntity("Crate");
            int added = LootStocker.StockContainer(crate, "Bulk", _factory, new System.Random(1));
            var cp = crate.GetPart<ContainerPart>();

            Assert.AreEqual(cp.Contents.Count, added,
                "the returned count matches what actually went in");
            Assert.LessOrEqual(cp.Contents.Count, 2, "MaxItems is respected");
        }

        [Test]
        public void Adversarial_Budget_ExtremeTiers_StayInBand()
        {
            var rng = new System.Random(5);
            for (int tier = -100; tier <= 100; tier += 7)
            {
                foreach (Kind k in System.Enum.GetValues(typeof(Kind)))
                {
                    int b = ContainerPlacementService.ComputeBudget(k, tier, rng);
                    Assert.That(b, Is.InRange(1, 6), $"tier {tier}, kind {k}");
                }
            }
        }

        [Test]
        public void Adversarial_DeathLoot_TierTagGarbage_FallsBackSafely()
        {
            var zone = MakeRoom();
            var e = _factory.CreateEntity("Hoarder");
            zone.AddEntity(e, 5, 5);
            foreach (var junk in new[] { "", "  ", "abc", "-9", "999999999999999999999", "1.5" })
            {
                e.Tags["Tier"] = junk;
                Assert.That(LootDropSystem.ResolveTier(e), Is.InRange(1, 3), $"tier tag '{junk}'");
                Assert.DoesNotThrow(() => LootDropSystem.ResolveTableName(e));
            }
        }

        // ═══════════════ SAVE / LOAD REACH ═══════════════════════

        [Test]
        public void Adversarial_StockedContainer_SurvivesASaveLoadRoundTrip()
        {
            // Containers are placed at worldgen and must still hold
            // their loot after the player saves and reloads — the whole
            // feature is worthless if a reload empties the world.
            var crate = _factory.CreateEntity("Crate");
            LootStocker.StockContainer(crate, "CrateT1", _factory, new System.Random(3));
            int before = crate.GetPart<ContainerPart>().Contents.Count;
            Assert.Greater(before, 0, "precondition: the crate holds something");

            var stream = new System.IO.MemoryStream();
            var writer = new SaveWriter(stream);
            SaveGraphSerializer.SaveEntityBody(crate, writer);
            stream.Position = 0;
            var loaded = new Entity();
            SaveGraphSerializer.LoadEntityBody(loaded, new SaveReader(stream, _factory));

            var cp = loaded.GetPart<ContainerPart>();
            Assert.IsNotNull(cp, "the Container part round-trips");
            Assert.AreEqual(before, cp.Contents.Count,
                "and so do its contents — a reload must not empty the world");
        }

        // ═══════════════ DETERMINISM ═════════════════════════════

        [Test]
        public void Adversarial_SameSeed_SameWorld_AcrossEveryBiome()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                var a = MakeRoom(); var b = MakeRoom();
                int pa = ContainerPlacementService.Populate(a, biome, 2, Kind.Wilderness, new System.Random(77));
                int pb = ContainerPlacementService.Populate(b, biome, 2, Kind.Wilderness, new System.Random(77));
                Assert.AreEqual(pa, pb, $"biome {biome}: same seed, same count");
            }
        }

        [Test]
        public void Adversarial_DifferentSeeds_ActuallyDiffer()
        {
            // Counter-check for determinism: if Populate ignored the RNG
            // entirely, the test above would pass vacuously.
            var layouts = new HashSet<string>();
            for (int seed = 0; seed < 12; seed++)
            {
                var z = MakeRoom();
                ContainerPlacementService.Populate(z, BiomeType.Cave, 2, Kind.Wilderness, new System.Random(seed));
                var sig = new List<string>();
                foreach (var e in z.GetAllEntities())
                    if (e.GetPart<ContainerPart>() != null)
                    { var c = z.GetEntityCell(e); sig.Add($"{e.BlueprintName}@{c.X},{c.Y}"); }
                sig.Sort();
                layouts.Add(string.Join("|", sig));
            }
            Assert.Greater(layouts.Count, 1, "different seeds must produce different worlds");
        }

        // ═══════════ CROSS-SYSTEM / RE-ENTRANCY ══════════════════

        [Test]
        public void Adversarial_ManySpawns_DoNotLeakAcrossCreatures()
        {
            // Two creatures of the same blueprint must not share item
            // instances — a classic factory-caching bug.
            var a = _factory.CreateEntity("Hoarder");
            var b = _factory.CreateEntity("Hoarder");
            var ia = a.GetPart<InventoryPart>();
            var ib = b.GetPart<InventoryPart>();
            foreach (var x in ia.Objects)
                Assert.IsFalse(ib.Objects.Contains(x),
                    "two creatures never share the same item instance");
        }

        [Test]
        public void Adversarial_PopulateTwice_DoesNotStackContainers()
        {
            // Re-running placement on an already-populated zone (a
            // regenerate, a double-registered builder) must not pile
            // containers onto occupied cells.
            var zone = MakeRoom();
            ContainerPlacementService.Populate(zone, BiomeType.Cave, 3, Kind.Underground, new System.Random(8));
            ContainerPlacementService.Populate(zone, BiomeType.Cave, 3, Kind.Underground, new System.Random(9));

            var seen = new HashSet<(int, int)>();
            foreach (var e in zone.GetAllEntities())
            {
                if (e.GetPart<ContainerPart>() == null) continue;
                var c = zone.GetEntityCell(e);
                Assert.IsTrue(seen.Add((c.X, c.Y)),
                    "a second pass never stacks a container on an existing one");
            }
        }
    }
}
