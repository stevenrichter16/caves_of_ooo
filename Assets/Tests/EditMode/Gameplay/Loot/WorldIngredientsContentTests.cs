using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Content-integrity fixture for the M4 first-slice ingredients
    /// (Docs/GATHER-LOOT-SYSTEM.md). Loads the REAL Objects.json and the
    /// REAL LootTables.json — no inline test fixtures — so a JSON typo
    /// (misspelled Part name, a loot table referencing a blueprint that
    /// doesn't exist) fails here instead of silently no-op'ing in play.
    /// </summary>
    public class WorldIngredientsContentTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            var blueprints = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(blueprints, "Real Objects.json must be loadable.");
            _factory.LoadBlueprints(blueprints.text);

            LootTableRegistry.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LootTableRegistry.ResetForTests();
        }

        // ====================================================================
        // New reagent / component items instantiate with the expected parts.
        // ====================================================================

        [Test]
        public void PaleReed_Instantiates_WithReagentProperties()
        {
            var item = _factory.CreateEntity("PaleReed");
            Assert.IsNotNull(item);
            var reagent = item.GetPart<ReagentPart>();
            Assert.IsNotNull(reagent, "PaleReed must carry a ReagentPart.");
            var props = reagent.GetProperties();
            Assert.AreEqual(2, props.Count);
        }

        [Test]
        public void GlowQuartz_Enriched_WithReagentProperties_KeepsExistingCommerce()
        {
            // Counter-check: adding Reagent to an existing item must not
            // clobber its pre-existing Commerce value.
            var item = _factory.CreateEntity("GlowQuartz");
            Assert.IsNotNull(item);
            Assert.IsNotNull(item.GetPart<ReagentPart>(), "GlowQuartz must now carry a ReagentPart.");
            var commerce = item.GetPart<CommercePart>();
            Assert.IsNotNull(commerce);
            Assert.AreEqual(15, commerce.Value, "Enriching GlowQuartz must not change its pre-existing Commerce value.");
        }

        [Test]
        public void SnapjawFangPoint_Instantiates_AsBladeComponent()
        {
            var item = _factory.CreateEntity("SnapjawFangPoint");
            Assert.IsNotNull(item);
            var component = item.GetPart<WeaponComponentPart>();
            Assert.IsNotNull(component, "SnapjawFangPoint must carry a WeaponComponentPart.");
            Assert.AreEqual("Blade", component.Slot);
        }

        // ====================================================================
        // Gather-node blueprints instantiate with GatherNodePart configured.
        // ====================================================================

        [Test]
        public void PaleReedNode_Instantiates_AsRegrowingGatherNode()
        {
            var node = _factory.CreateEntity("PaleReedNode");
            Assert.IsNotNull(node);
            var gather = node.GetPart<GatherNodePart>();
            Assert.IsNotNull(gather);
            Assert.AreEqual("loot_pale_reed_bank", gather.LootTableID);
            Assert.Greater(gather.RegrowTurns, 0, "PaleReedNode should regrow, not vanish after harvest.");
        }

        [Test]
        public void FireMossPatch_Instantiates_AsGatherNode()
        {
            var node = _factory.CreateEntity("FireMossPatch");
            Assert.IsNotNull(node);
            var gather = node.GetPart<GatherNodePart>();
            Assert.IsNotNull(gather);
            Assert.AreEqual("loot_firemoss_patch", gather.LootTableID);
        }

        [Test]
        public void GlowQuartzSeam_Instantiates_AsSolidGatherNode()
        {
            var node = _factory.CreateEntity("GlowQuartzSeam");
            Assert.IsNotNull(node);
            var gather = node.GetPart<GatherNodePart>();
            Assert.IsNotNull(gather);
            Assert.AreEqual("loot_glowquartz_seam", gather.LootTableID);
            var physics = node.GetPart<PhysicsPart>();
            Assert.IsTrue(physics.Solid, "A mineral seam in a wall should be solid (matches Rock/Stalagmite convention).");
        }

        // ====================================================================
        // Creature-drop wiring.
        // ====================================================================

        [Test]
        public void Snapjaw_CorpsePart_HasLootTableID()
        {
            var snapjaw = _factory.CreateEntity("Snapjaw");
            Assert.IsNotNull(snapjaw);
            var corpse = snapjaw.GetPart<CorpsePart>();
            Assert.IsNotNull(corpse);
            Assert.AreEqual("loot_snapjaw_drops", corpse.LootTableID);
            // Counter-check: the pre-existing corpse-blueprint wiring must
            // survive the new field being added alongside it.
            Assert.AreEqual("SnapjawCorpse", corpse.CorpseBlueprint);
            Assert.AreEqual(70, corpse.CorpseChance);
        }

        // ====================================================================
        // Loot table <-> blueprint cross-reference integrity.
        // ====================================================================

        [TestCase("loot_pale_reed_bank")]
        [TestCase("loot_firemoss_patch")]
        [TestCase("loot_glowquartz_seam")]
        [TestCase("loot_snapjaw_drops")]
        public void LootTable_Exists_AndEveryEntryBlueprintResolves(string tableId)
        {
            bool found = LootTableRegistry.TryGetTable(tableId, out LootTable table);
            Assert.IsTrue(found, $"Loot table '{tableId}' must be defined in LootTables.json.");

            foreach (var entry in table.Entries)
            {
                var item = _factory.CreateEntity(entry.BlueprintName);
                Assert.IsNotNull(item,
                    $"Table '{tableId}' references blueprint '{entry.BlueprintName}', which must exist in Objects.json.");
            }
        }

        // ====================================================================
        // Population-table wiring pins (regression: don't silently drop the
        // gather nodes from spawn tables in a future edit).
        // ====================================================================

        [Test]
        public void CaveTier1_IncludesForageGatherNodes()
        {
            var table = PopulationTable.CaveTier1();
            AssertContainsBlueprint(table, "FireMossPatch");
            AssertContainsBlueprint(table, "PaleReedNode");
        }

        [Test]
        public void CaveTier2_IncludesMineralGatherNode()
        {
            var table = PopulationTable.CaveTier2();
            AssertContainsBlueprint(table, "GlowQuartzSeam");
        }

        private static void AssertContainsBlueprint(PopulationTable table, string blueprintName)
        {
            foreach (var entry in table.Entries)
            {
                if (entry.BlueprintName == blueprintName)
                    return;
            }
            Assert.Fail($"PopulationTable '{table.Name}' should contain an entry for '{blueprintName}'.");
        }
    }
}
