using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 4c tests — the WorldInteractionSystem dispatcher.
    /// Pure-function semantics; no side effects on MessageLog or the game state.
    /// </summary>
    [TestFixture]
    public class WorldInteractionSystemTests
    {
        private EntityFactory _factory;
        private Zone _zone;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
            _zone = new Zone("TestZone");
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        // =========================================================
        // ResolveTarget
        // =========================================================

        [Test]
        public void ResolveTarget_NullCell_ReturnsNull()
        {
            Assert.IsNull(WorldInteractionSystem.ResolveTarget(null));
        }

        [Test]
        public void ResolveTarget_EmptyCell_ReturnsNull()
        {
            var cell = new Cell(0, 0, _zone);
            Assert.IsNull(WorldInteractionSystem.ResolveTarget(cell));
        }

        [Test]
        public void ResolveTarget_OnlyFloor_ReturnsFloor()
        {
            var floor = _factory.CreateEntity("Floor");
            _zone.AddEntity(floor, 5, 5);
            var cell = _zone.GetCell(5, 5);

            var target = WorldInteractionSystem.ResolveTarget(cell);

            Assert.AreSame(floor, target,
                "Only-terrain cell should return the terrain itself, not null " +
                "(terrain now has ExaminablePart so it's a legit target).");
        }

        [Test]
        public void ResolveTarget_OnlyWall_ReturnsWall()
        {
            var wall = _factory.CreateEntity("Wall");
            _zone.AddEntity(wall, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreSame(wall, WorldInteractionSystem.ResolveTarget(cell));
        }

        [Test]
        public void ResolveTarget_SingleNonTerrainEntity_ReturnsIt()
        {
            var chest = _factory.CreateEntity("Chest");
            _zone.AddEntity(chest, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreSame(chest, WorldInteractionSystem.ResolveTarget(cell));
        }

        [Test]
        public void ResolveTarget_NonTerrainOverTerrain_ReturnsNonTerrain()
        {
            var floor = _factory.CreateEntity("Floor");
            var chest = _factory.CreateEntity("Chest");
            _zone.AddEntity(floor, 5, 5);
            _zone.AddEntity(chest, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreSame(chest, WorldInteractionSystem.ResolveTarget(cell),
                "Non-terrain should outrank terrain regardless of render layer.");
        }

        [Test]
        public void ResolveTarget_MultipleNonTerrain_ReturnsTopRenderLayer()
        {
            // Cell.AddObject inserts in ascending render-layer order, so the
            // last-inserted entity ends up at the highest index if layers tie.
            // For the test, use blueprints with explicit different render layers.
            // Snapjaw (RenderLayer=10 via Creature) vs HealingTonic (item).
            var snapjaw = _factory.CreateEntity("Snapjaw");
            var tonic = _factory.CreateEntity("HealingTonic");
            _zone.AddEntity(tonic, 5, 5);
            _zone.AddEntity(snapjaw, 5, 5);
            var cell = _zone.GetCell(5, 5);

            // Snapjaw has higher RenderLayer (10) than HealingTonic (items
            // usually render at layer 1-5), so Snapjaw is the visual top.
            Assert.AreSame(snapjaw, WorldInteractionSystem.ResolveTarget(cell),
                "Among multiple non-terrain, the higher render-layer entity wins.");
        }

        // =========================================================
        // GatherActions
        // =========================================================

        [Test]
        public void GatherActions_NullTarget_ReturnsEmptyList()
        {
            var actions = WorldInteractionSystem.GatherActions(null);
            Assert.IsNotNull(actions);
            Assert.AreEqual(0, actions.Count);
        }

        [Test]
        public void GatherActions_Chest_ReturnsOpenAndExamine_Sorted()
        {
            var chest = _factory.CreateEntity("Chest");
            var actions = WorldInteractionSystem.GatherActions(chest);

            Assert.AreEqual(3, actions.Count, "Chest declares Open + Break + Examine.");
            // Sort is priority DESC: Open (30), Break (5), Examine (0).
            // Break sits deliberately below Open — smashing a chest must
            // never be the row the cursor lands on when opening it is an
            // option. See DestructiblePart.HandleEvent.
            Assert.AreEqual("OpenContainer", actions[0].Command);
            Assert.AreEqual(DestructiblePart.BreakCommand, actions[1].Command);
            Assert.AreEqual("Examine", actions[2].Command);
        }

        [Test]
        public void GatherActions_Snapjaw_ReturnsExamineOnly()
        {
            // Snapjaw has Creature + items but no ConversationPart (not talk-
            // able) and no ContainerPart (not openable). Only Examinable
            // cascades from PhysicalObject.
            var snapjaw = _factory.CreateEntity("Snapjaw");
            var actions = WorldInteractionSystem.GatherActions(snapjaw);

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Examine", actions[0].Command);
        }

        [Test]
        public void GatherActions_Scribe_ReturnsChatAndExamine_ChatFirst()
        {
            // Scribe has ConversationPart + Examinable. Chat(10) > Examine(0).
            var scribe = _factory.CreateEntity("Scribe");
            var actions = WorldInteractionSystem.GatherActions(scribe);

            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual("Chat", actions[0].Command);
            Assert.AreEqual("Examine", actions[1].Command);
        }

        // =========================================================
        // DescribeCell
        // =========================================================

        [Test]
        public void DescribeCell_NullCell_ReturnsNothingHere()
        {
            Assert.AreEqual("You see nothing here.",
                WorldInteractionSystem.DescribeCell(null));
        }

        [Test]
        public void DescribeCell_EmptyCell_ReturnsNothingHere()
        {
            var cell = new Cell(0, 0, _zone);
            Assert.AreEqual("You see nothing here.",
                WorldInteractionSystem.DescribeCell(cell));
        }

        [Test]
        public void DescribeCell_OnlyFloor_ReturnsFloorLine()
        {
            var floor = _factory.CreateEntity("Floor");
            _zone.AddEntity(floor, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreEqual("You see the floor.",
                WorldInteractionSystem.DescribeCell(cell));
        }

        [Test]
        public void DescribeCell_OnlyWall_ReturnsWallLine()
        {
            var wall = _factory.CreateEntity("Wall");
            _zone.AddEntity(wall, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreEqual("You see the wall.",
                WorldInteractionSystem.DescribeCell(cell));
        }

        [Test]
        public void DescribeCell_SingleItemOnFloor_ReturnsItemLineIgnoringFloor()
        {
            var floor = _factory.CreateEntity("Floor");
            var tonic = _factory.CreateEntity("HealingTonic");
            _zone.AddEntity(floor, 5, 5);
            _zone.AddEntity(tonic, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreEqual("You see a healing tonic.",
                WorldInteractionSystem.DescribeCell(cell),
                "Single non-terrain entity is described individually, " +
                "ignoring the floor beneath.");
        }

        [Test]
        public void DescribeCell_MultipleItems_ReturnsPileSummary()
        {
            var floor = _factory.CreateEntity("Floor");
            var tonic = _factory.CreateEntity("HealingTonic");
            var sword = _factory.CreateEntity("ShortSword");
            _zone.AddEntity(floor, 5, 5);
            _zone.AddEntity(tonic, 5, 5);
            _zone.AddEntity(sword, 5, 5);
            var cell = _zone.GetCell(5, 5);

            string desc = WorldInteractionSystem.DescribeCell(cell);
            StringAssert.StartsWith("A pile of items, including: ", desc);
            // Order within the pile depends on Cell.AddObject render-layer
            // sort; just verify all items are referenced by name.
            StringAssert.Contains("healing tonic", desc);
            StringAssert.Contains("short sword", desc);
            StringAssert.DoesNotContain("floor", desc,
                "Terrain should not appear in pile summaries.");
            StringAssert.EndsWith(".", desc);
        }

        [Test]
        public void DescribeCell_TwoItemsNoFloor_StillPileSummary()
        {
            var tonic = _factory.CreateEntity("HealingTonic");
            var sword = _factory.CreateEntity("ShortSword");
            _zone.AddEntity(tonic, 5, 5);
            _zone.AddEntity(sword, 5, 5);
            var cell = _zone.GetCell(5, 5);

            string desc = WorldInteractionSystem.DescribeCell(cell);
            StringAssert.StartsWith("A pile of items, including: ", desc);
        }

        [Test]
        public void DescribeCell_ItemWithVowelName_UsesAn()
        {
            // "apple" isn't a real blueprint; use an entity with an explicit
            // vowel-starting display name to verify article logic is wired.
            var entity = new Entity { BlueprintName = "Apple" };
            entity.AddPart(new RenderPart { DisplayName = "apple" });
            _zone.AddEntity(entity, 5, 5);
            var cell = _zone.GetCell(5, 5);

            Assert.AreEqual("You see an apple.",
                WorldInteractionSystem.DescribeCell(cell));
        }

        // =========================================================
        // IsPileCell
        // =========================================================

        [Test]
        public void IsPileCell_NullCell_ReturnsFalse()
        {
            Assert.IsFalse(WorldInteractionSystem.IsPileCell(null));
        }

        [Test]
        public void IsPileCell_EmptyCell_ReturnsFalse()
        {
            var cell = new Cell(0, 0, _zone);
            Assert.IsFalse(WorldInteractionSystem.IsPileCell(cell));
        }

        [Test]
        public void IsPileCell_FloorPlusOneItem_ReturnsFalse()
        {
            var floor = _factory.CreateEntity("Floor");
            var tonic = _factory.CreateEntity("HealingTonic");
            _zone.AddEntity(floor, 5, 5);
            _zone.AddEntity(tonic, 5, 5);

            Assert.IsFalse(WorldInteractionSystem.IsPileCell(_zone.GetCell(5, 5)),
                "One non-terrain item is not a pile.");
        }

        [Test]
        public void IsPileCell_TwoItems_ReturnsTrue()
        {
            var tonic = _factory.CreateEntity("HealingTonic");
            var sword = _factory.CreateEntity("ShortSword");
            _zone.AddEntity(tonic, 5, 5);
            _zone.AddEntity(sword, 5, 5);

            Assert.IsTrue(WorldInteractionSystem.IsPileCell(_zone.GetCell(5, 5)));
        }

        // =========================================================
        // IsTerrain
        // =========================================================

        [Test]
        public void IsTerrain_Null_ReturnsFalse()
        {
            Assert.IsFalse(WorldInteractionSystem.IsTerrain(null));
        }

        [TestCase("Wall")]
        [TestCase("StoneWall")]
        [TestCase("Floor")]
        public void IsTerrain_TerrainBlueprint_ReturnsTrue(string blueprintName)
        {
            var entity = _factory.CreateEntity(blueprintName);
            Assert.IsTrue(WorldInteractionSystem.IsTerrain(entity));
        }

        [TestCase("Snapjaw")]
        [TestCase("Warden")]
        [TestCase("Chest")]
        [TestCase("HealingTonic")]
        public void IsTerrain_NonTerrainBlueprint_ReturnsFalse(string blueprintName)
        {
            var entity = _factory.CreateEntity(blueprintName);
            Assert.IsFalse(WorldInteractionSystem.IsTerrain(entity));
        }

        // ════════════════ "What's here" picker (look-mode click) ════════════════

        private Entity Place(string name, int x, int y, bool terrain = false)
        {
            var e = new Entity { ID = name + "-id", BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name });
            if (terrain)
                e.SetTag("Terrain");
            Assert.IsTrue(_zone.AddEntity(e, x, y));
            return e;
        }

        [Test]
        public void BuildTargetPickerActions_ListsEveryObject_NonTerrainFirstTerrainLast()
        {
            // User request (2026-07-19): clicking a stacked tile in look
            // mode must list ALL objects on it — each row picks that entity
            // for its own action menu. Non-terrain objects lead (top-most
            // first); terrain (the floor) is still reachable at the bottom.
            var floor = Place("stone floor", 3, 3, terrain: true);
            var corpse = Place("snapjaw corpse", 3, 3);
            var hunter = Place("snapjaw hunter", 3, 3);
            var cell = _zone.GetCell(3, 3);

            var rows = WorldInteractionSystem.BuildTargetPickerActions(cell);

            Assert.AreEqual(3, rows.Count, "every object gets a row");
            Assert.AreEqual(WorldInteractionSystem.PickTargetCommandPrefix + hunter.ID,
                rows[0].Command, "top-most non-terrain (added last) leads");
            Assert.AreEqual(WorldInteractionSystem.PickTargetCommandPrefix + corpse.ID,
                rows[1].Command);
            Assert.AreEqual(WorldInteractionSystem.PickTargetCommandPrefix + floor.ID,
                rows[2].Command, "terrain listed last");
            StringAssert.Contains("snapjaw hunter", rows[0].Display);
        }

        [Test]
        public void BuildTargetPickerActions_EmptyOrNullCell_EmptyList()
        {
            Assert.IsEmpty(WorldInteractionSystem.BuildTargetPickerActions(null));
            Assert.IsEmpty(WorldInteractionSystem.BuildTargetPickerActions(
                new Cell(0, 0, _zone)));
        }

        [Test]
        public void FindInCell_ResolvesByIdAndRejectsUnknown()
        {
            var corpse = Place("snapjaw corpse", 4, 4);
            var cell = _zone.GetCell(4, 4);

            Assert.AreSame(corpse, WorldInteractionSystem.FindInCell(cell, corpse.ID));
            Assert.IsNull(WorldInteractionSystem.FindInCell(cell, "nope"));
            Assert.IsNull(WorldInteractionSystem.FindInCell(null, corpse.ID));
        }
    }
}
