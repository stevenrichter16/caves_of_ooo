using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LOOT OVERHAUL SM3 — the death loot roll ("random other loot
    /// calculated from a loot algorithm"). Loadouts cover what the
    /// creature visibly carried; this covers the supply line that both
    /// crafting systems were starved of.
    /// </summary>
    [TestFixture]
    public class LootDropSystemTests
    {
        private const string Blueprints = @"
        {
          ""Objects"": [
            { ""Name"": ""PhysicalObject"", ""Parts"": [] },
            { ""Name"": ""Item"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] } ] },
            { ""Name"": ""GoldCoin"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""gold"" }, { ""Key"": ""RenderString"", ""Value"": ""$"" } ] } ] },
            { ""Name"": ""FireMoss"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""fire moss"" }, { ""Key"": ""RenderString"", ""Value"": ""%"" } ] } ] },
            { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Inventory"", ""Params"": [ { ""Key"": ""MaxWeight"", ""Value"": ""150"" } ] } ] },
            { ""Name"": ""Wolf"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""wolf"" }, { ""Key"": ""RenderString"", ""Value"": ""d"" } ] } ],
              ""Tags"": [ { ""Key"": ""Tier"", ""Value"": ""1"" } ] },
            { ""Name"": ""Bandit"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""bandit"" }, { ""Key"": ""RenderString"", ""Value"": ""h"" } ] },
                { ""Name"": ""Loadout"", ""Params"": [ { ""Key"": ""Carry"", ""Value"": ""GoldCoin:0"" } ] } ],
              ""Tags"": [ { ""Key"": ""Tier"", ""Value"": ""2"" } ] },
            { ""Name"": ""Golem"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""golem"" }, { ""Key"": ""RenderString"", ""Value"": ""G"" } ] },
                { ""Name"": ""Material"", ""Params"": [ { ""Key"": ""MaterialID"", ""Value"": ""Stone"" } ] } ],
              ""Tags"": [ { ""Key"": ""Tier"", ""Value"": ""3"" } ] }
          ]
        }";

        private const string Tables = @"
        { ""Tables"": [
            { ""Name"": ""DeathBeastT1"", ""Entries"": [ { ""Blueprint"": ""FireMoss"", ""Chance"": 100 } ] },
            { ""Name"": ""AlwaysGold"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 100 } ] },
            { ""Name"": ""NeverDrops"", ""Entries"": [ { ""Blueprint"": ""GoldCoin"", ""Chance"": 0 } ] }
        ] }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(Blueprints);
            LootTableRegistry.Initialize(Tables);
            LootDropSystem.Factory = _factory;
            LootDropSystem.Rng = new System.Random(7);
            LoadoutPart.Factory = _factory;
            LoadoutPart.Rng = new System.Random(7);
        }

        [TearDown]
        public void TearDown()
        {
            LootDropSystem.Factory = null;
            LootDropSystem.Rng = null;
            LoadoutPart.Factory = null;
            LoadoutPart.Rng = null;
        }

        // ── Table resolution ─────────────────────────────────────

        [Test]
        public void Class_IsDerivedFromTheCreature_NotAuthored()
        {
            // 73 creature blueprints didn't need editing: carrying gear
            // makes you humanoid, being stone makes you a construct.
            Assert.AreEqual(LootDropSystem.ClassBeast,
                LootDropSystem.ResolveClass(_factory.CreateEntity("Wolf")));
            Assert.AreEqual(LootDropSystem.ClassHumanoid,
                LootDropSystem.ResolveClass(_factory.CreateEntity("Bandit")),
                "a creature with a Loadout is humanoid by behaviour");
            Assert.AreEqual(LootDropSystem.ClassConstruct,
                LootDropSystem.ResolveClass(_factory.CreateEntity("Golem")));
        }

        [Test]
        public void TableName_CombinesClassAndTier()
        {
            Assert.AreEqual("DeathBeastT1",
                LootDropSystem.ResolveTableName(_factory.CreateEntity("Wolf")));
            Assert.AreEqual("DeathHumanoidT2",
                LootDropSystem.ResolveTableName(_factory.CreateEntity("Bandit")));
            Assert.AreEqual("DeathConstructT3",
                LootDropSystem.ResolveTableName(_factory.CreateEntity("Golem")));
        }

        [Test]
        public void ExplicitLootTableTag_OverridesTheDerivation()
        {
            var wolf = _factory.CreateEntity("Wolf");
            wolf.Tags["LootTable"] = "AlwaysGold";
            Assert.AreEqual("AlwaysGold", LootDropSystem.ResolveTableName(wolf),
                "an author can always point one creature at one table");
        }

        [Test]
        public void Tier_IsClampedToTheAuthoredRange()
        {
            var wolf = _factory.CreateEntity("Wolf");
            wolf.Tags["Tier"] = "9";
            Assert.AreEqual(3, LootDropSystem.ResolveTier(wolf));
            wolf.Tags["Tier"] = "nonsense";
            Assert.AreEqual(1, LootDropSystem.ResolveTier(wolf),
                "int.TryParse writes 0 on failure — the sentinel guard must catch it");
        }

        // ── Rolling ──────────────────────────────────────────────

        [Test]
        public void Roll_SpawnsTheTableResultOnTheDeathCell()
        {
            var zone = new Zone("T");
            var wolf = _factory.CreateEntity("Wolf");
            zone.AddEntity(wolf, 4, 4);

            int spawned = LootDropSystem.RollDeathLoot(wolf, null, zone);

            Assert.AreEqual(1, spawned);
            var cell = zone.GetCell(4, 4);
            bool found = false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].BlueprintName == "FireMoss") found = true;
            Assert.IsTrue(found, "the rolled item lands where the creature died");
        }

        [Test]
        public void Roll_ZeroChanceTable_DropsNothing()
        {
            // Counter-check: without this, a table that rolls nothing
            // and a broken roller look identical.
            var zone = new Zone("T");
            var wolf = _factory.CreateEntity("Wolf");
            wolf.Tags["LootTable"] = "NeverDrops";
            zone.AddEntity(wolf, 5, 5);

            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(wolf, null, zone));
        }

        [Test]
        public void Roll_RespectsTheNoDropOnDeathSuppression()
        {
            // Same contract as the SM2 equipment guard — a summoned
            // creature is not a loot piñata.
            var zone = new Zone("T");
            var wolf = _factory.CreateEntity("Wolf");
            wolf.Tags["NoDropOnDeath"] = "";
            zone.AddEntity(wolf, 6, 6);

            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(wolf, null, zone));
        }

        [Test]
        public void Roll_UnknownTable_IsAGracefulNoOp()
        {
            var zone = new Zone("T");
            var golem = _factory.CreateEntity("Golem");   // DeathConstructT3 not in fixture
            zone.AddEntity(golem, 7, 7);

            Assert.DoesNotThrow(() => LootDropSystem.RollDeathLoot(golem, null, zone));
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(golem, null, zone));
        }

        [Test]
        public void Roll_NullFactoryOrZone_IsAGracefulNoOp()
        {
            var zone = new Zone("T");
            var wolf = _factory.CreateEntity("Wolf");
            zone.AddEntity(wolf, 8, 8);

            LootDropSystem.Factory = null;
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(wolf, null, zone));
            LootDropSystem.Factory = _factory;
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(wolf, null, null));
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(null, null, zone));
        }

        [Test]
        public void Roll_EntityNotInZone_IsAGracefulNoOp()
        {
            // A creature killed by a path that already removed it from
            // the zone has no death cell to scatter onto.
            var zone = new Zone("T");
            var wolf = _factory.CreateEntity("Wolf");
            Assert.AreEqual(0, LootDropSystem.RollDeathLoot(wolf, null, zone));
        }
    }
}
