using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class GridSystemTests
    {
        // ========================
        // Cell Tests
        // ========================

        private Entity MakeEntity(string name, string glyph, string color, int layer = 0, bool solid = false)
        {
            var entity = new Entity { BlueprintName = name };
            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = layer
            });
            if (solid)
                entity.SetTag("Solid");
            return entity;
        }

        [Test]
        public void Cell_AddObject_TracksEntity()
        {
            var cell = new Cell(5, 10);
            var entity = MakeEntity("Sword", "/", "&c");
            cell.AddObject(entity);

            Assert.AreEqual(1, cell.Objects.Count);
            Assert.AreEqual(entity, cell.Objects[0]);
        }

        [Test]
        public void Cell_RemoveObject_RemovesEntity()
        {
            var cell = new Cell(0, 0);
            var entity = MakeEntity("Test", "?", "&y");
            cell.AddObject(entity);
            cell.RemoveObject(entity);

            Assert.AreEqual(0, cell.Objects.Count);
        }

        [Test]
        public void Cell_AddObject_SortsByRenderLayer()
        {
            var cell = new Cell(0, 0);
            var floor = MakeEntity("Floor", ".", "&y", 0);
            var item = MakeEntity("Dagger", "/", "&c", 5);
            var creature = MakeEntity("Player", "@", "&Y", 10);

            // Add in reverse order
            cell.AddObject(creature);
            cell.AddObject(floor);
            cell.AddObject(item);

            Assert.AreEqual(floor, cell.Objects[0]);
            Assert.AreEqual(item, cell.Objects[1]);
            Assert.AreEqual(creature, cell.Objects[2]);
        }

        [Test]
        public void Cell_GetTopVisibleObject_ReturnsHighestLayer()
        {
            var cell = new Cell(0, 0);
            cell.AddObject(MakeEntity("Floor", ".", "&y", 0));
            cell.AddObject(MakeEntity("Dagger", "/", "&c", 5));
            cell.AddObject(MakeEntity("Player", "@", "&Y", 10));

            var top = cell.GetTopVisibleObject();
            Assert.IsNotNull(top);
            Assert.AreEqual("Player", top.BlueprintName);
        }

        [Test]
        public void Cell_IsSolid_WhenHasSolidEntity()
        {
            var cell = new Cell(0, 0);
            Assert.IsFalse(cell.IsSolid());

            cell.AddObject(MakeEntity("Wall", "#", "&K", 0, solid: true));
            Assert.IsTrue(cell.IsSolid());
        }

        [Test]
        public void Cell_IsPassable_WhenNoSolid()
        {
            var cell = new Cell(0, 0);
            cell.AddObject(MakeEntity("Dagger", "/", "&c"));

            Assert.IsTrue(cell.IsPassable());
        }

        [Test]
        public void Cell_IsEmpty_WhenNoObjects()
        {
            var cell = new Cell(0, 0);
            Assert.IsTrue(cell.IsEmpty());

            cell.AddObject(MakeEntity("Test", "?", "&y"));
            Assert.IsFalse(cell.IsEmpty());
        }

        [Test]
        public void Cell_HasObjectWithTag()
        {
            var cell = new Cell(0, 0);
            var entity = MakeEntity("Wall", "#", "&K");
            entity.SetTag("Wall");
            cell.AddObject(entity);

            Assert.IsTrue(cell.HasObjectWithTag("Wall"));
            Assert.IsFalse(cell.HasObjectWithTag("Creature"));
        }

        // ========================
        // Zone Tests
        // ========================

        [Test]
        public void Zone_Dimensions_80x25()
        {
            var zone = new Zone();
            Assert.AreEqual(80, Zone.Width);
            Assert.AreEqual(25, Zone.Height);
        }

        [Test]
        public void Zone_AllCells_Initialized()
        {
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    Assert.IsNotNull(zone.GetCell(x, y));
                    Assert.AreEqual(x, zone.GetCell(x, y).X);
                    Assert.AreEqual(y, zone.GetCell(x, y).Y);
                }
            }
        }

        [Test]
        public void Zone_GetCell_OutOfBounds_ReturnsNull()
        {
            var zone = new Zone();
            Assert.IsNull(zone.GetCell(-1, 0));
            Assert.IsNull(zone.GetCell(80, 0));
            Assert.IsNull(zone.GetCell(0, -1));
            Assert.IsNull(zone.GetCell(0, 25));
        }

        [Test]
        public void Zone_AddEntity_PlacesInCell()
        {
            var zone = new Zone();
            var entity = MakeEntity("Player", "@", "&Y");

            bool added = zone.AddEntity(entity, 10, 5);
            Assert.IsTrue(added);

            var cell = zone.GetCell(10, 5);
            Assert.AreEqual(1, cell.Objects.Count);
            Assert.AreEqual(entity, cell.Objects[0]);
        }

        [Test]
        public void Zone_GetEntityPosition()
        {
            var zone = new Zone();
            var entity = MakeEntity("Player", "@", "&Y");
            zone.AddEntity(entity, 30, 12);

            var pos = zone.GetEntityPosition(entity);
            Assert.AreEqual(30, pos.x);
            Assert.AreEqual(12, pos.y);
        }

        [Test]
        public void Zone_MoveEntity_UpdatesPosition()
        {
            var zone = new Zone();
            var entity = MakeEntity("Player", "@", "&Y");
            zone.AddEntity(entity, 10, 10);

            bool moved = zone.MoveEntity(entity, 11, 10);
            Assert.IsTrue(moved);

            // Old cell should be empty
            Assert.AreEqual(0, zone.GetCell(10, 10).Objects.Count);

            // New cell should have entity
            Assert.AreEqual(1, zone.GetCell(11, 10).Objects.Count);

            // Position should be updated
            var pos = zone.GetEntityPosition(entity);
            Assert.AreEqual(11, pos.x);
            Assert.AreEqual(10, pos.y);
        }

        [Test]
        public void Zone_RemoveEntity()
        {
            var zone = new Zone();
            var entity = MakeEntity("Player", "@", "&Y");
            zone.AddEntity(entity, 5, 5);

            bool removed = zone.RemoveEntity(entity);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, zone.GetCell(5, 5).Objects.Count);
            Assert.AreEqual(0, zone.EntityCount);
        }

        [Test]
        public void Zone_EntityCount_Tracks()
        {
            var zone = new Zone();
            Assert.AreEqual(0, zone.EntityCount);

            zone.AddEntity(MakeEntity("A", "a", "&y"), 1, 1);
            zone.AddEntity(MakeEntity("B", "b", "&y"), 2, 2);
            Assert.AreEqual(2, zone.EntityCount);
        }

        [Test]
        public void Zone_MultipleEntities_SameCell()
        {
            var zone = new Zone();
            var floor = MakeEntity("Floor", ".", "&y", 0);
            var item = MakeEntity("Dagger", "/", "&c", 5);

            zone.AddEntity(floor, 10, 10);
            zone.AddEntity(item, 10, 10);

            var cell = zone.GetCell(10, 10);
            Assert.AreEqual(2, cell.Objects.Count);

            var top = cell.GetTopVisibleObject();
            Assert.AreEqual("Dagger", top.BlueprintName);
        }

        [Test]
        public void Zone_GetEntitiesWithTag()
        {
            var zone = new Zone();
            var wall1 = MakeEntity("Wall", "#", "&K", 0, solid: true);
            wall1.SetTag("Wall");
            var wall2 = MakeEntity("Wall", "#", "&K", 0, solid: true);
            wall2.SetTag("Wall");
            var player = MakeEntity("Player", "@", "&Y");

            zone.AddEntity(wall1, 0, 0);
            zone.AddEntity(wall2, 1, 0);
            zone.AddEntity(player, 5, 5);

            var walls = zone.GetEntitiesWithTag("Wall");
            Assert.AreEqual(2, walls.Count);
        }

        [Test]
        public void Zone_GetCellInDirection()
        {
            var zone = new Zone();

            // North (direction 0): y-1
            var north = zone.GetCellInDirection(5, 5, 0);
            Assert.AreEqual(5, north.X);
            Assert.AreEqual(4, north.Y);

            // East (direction 2): x+1
            var east = zone.GetCellInDirection(5, 5, 2);
            Assert.AreEqual(6, east.X);
            Assert.AreEqual(5, east.Y);

            // Southeast (direction 3): x+1, y+1
            var se = zone.GetCellInDirection(5, 5, 3);
            Assert.AreEqual(6, se.X);
            Assert.AreEqual(6, se.Y);
        }

        [Test]
        public void Zone_InBounds_ChecksBoundaries()
        {
            var zone = new Zone();
            Assert.IsTrue(zone.InBounds(0, 0));
            Assert.IsTrue(zone.InBounds(79, 24));
            Assert.IsFalse(zone.InBounds(-1, 0));
            Assert.IsFalse(zone.InBounds(80, 0));
            Assert.IsFalse(zone.InBounds(0, 25));
        }
    }
}
