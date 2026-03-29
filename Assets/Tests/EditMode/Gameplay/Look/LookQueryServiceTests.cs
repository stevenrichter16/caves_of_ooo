using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class LookQueryServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        [Test]
        public void EmptyCell_ReturnsEmptyGround()
        {
            var zone = new Zone("LookZone");
            var player = CreateCreature("Player", "@", "&Y", isPlayer: true);
            zone.AddEntity(player, 10, 10);

            LookSnapshot snapshot = LookQueryService.BuildSnapshot(player, zone, 1, 1);

            Assert.AreEqual("[1,1] empty ground", snapshot.Header);
            Assert.AreEqual("You see empty ground.", snapshot.Summary);
            Assert.IsNull(snapshot.PrimaryEntity);
        }

        [Test]
        public void CreatureCell_IncludesHpAndRelation()
        {
            var zone = new Zone("LookZone");
            var player = CreateCreature("Player", "@", "&Y", isPlayer: true);
            var snapjaw = CreateCreature("Snapjaw", "s", "&g");
            snapjaw.SetTag("Faction", "Snapjaws");
            snapjaw.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 6, Min = 0, Max = 10 };

            zone.AddEntity(player, 10, 10);
            zone.AddEntity(snapjaw, 11, 10);

            LookSnapshot snapshot = LookQueryService.BuildSnapshot(player, zone, 11, 10);

            Assert.AreEqual("[11,10] Snapjaw", snapshot.Header);
            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("HP 6/10")));
            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("hostile")));
        }

        [Test]
        public void CellFlags_ReportTakeableContainerAndStairs()
        {
            var zone = new Zone("LookZone");
            var player = CreateCreature("Player", "@", "&Y", isPlayer: true);
            var satchel = CreateWorldObject("Satchel", "[", "&y");
            satchel.AddPart(new PhysicsPart { Takeable = true });
            var chest = CreateWorldObject("Chest", "C", "&Y");
            chest.AddPart(new ContainerPart());
            var stairs = CreateWorldObject("StairsDown", ">", "&W");
            stairs.AddPart(new StairsDownPart());

            zone.AddEntity(player, 10, 10);
            zone.AddEntity(satchel, 12, 10);
            zone.AddEntity(chest, 12, 10);
            zone.AddEntity(stairs, 12, 10);

            LookSnapshot snapshot = LookQueryService.BuildSnapshot(player, zone, 12, 10);

            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("takeable")));
            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("container")));
            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("stairs")));
            Assert.IsTrue(snapshot.DetailLines.Any(line => line.Contains("Contents:")));
        }

        private static Entity CreateCreature(string name, string glyph, string color, bool isPlayer = false)
        {
            var entity = new Entity { BlueprintName = name };
            entity.SetTag("Creature");
            if (isPlayer)
                entity.SetTag("Player");

            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = 10
            });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            return entity;
        }

        private static Entity CreateWorldObject(string name, string glyph, string color)
        {
            var entity = new Entity { BlueprintName = name };
            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = 5
            });
            return entity;
        }
    }
}
