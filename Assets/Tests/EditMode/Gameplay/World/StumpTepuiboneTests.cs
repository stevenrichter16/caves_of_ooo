using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class StumpTepuiboneTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [Test]
        public void VeinIsRealHarvestableStoneWithAHeavyResourceAndFourSprites()
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey("TepuiboneVein"));
            var vein = _factory.CreateEntity("TepuiboneVein");
            Assert.IsTrue(vein.HasTag("MineralVein"));
            Assert.IsTrue(vein.GetPart<PhysicsPart>().Solid);
            var harvest = vein.GetPart<HarvestablePart>(); Assert.IsNotNull(harvest);
            Assert.AreEqual("Tepuibone", harvest.YieldBlueprint);
            Assert.IsTrue(_factory.Blueprints.ContainsKey(harvest.YieldBlueprint));
            var stone = _factory.CreateEntity("Tepuibone");
            Assert.IsTrue(stone.GetPart<PhysicsPart>().Takeable);
            Assert.Greater(stone.GetPart<PhysicsPart>().Weight, _factory.CreateEntity("ChoirIron").GetPart<PhysicsPart>().Weight);
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r => r.Blueprint == "TepuiboneVein" && r.File == "tepuibone_vein"));
            Assert.AreEqual(4, EnvironmentSpriteRenderer.FixtureVariantCounts["TepuiboneVein"]);
            for (int i = 0; i < 4; i++)
                Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/tepuibone_vein" + (i == 0 ? "" : "_v" + i)));
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r => r.Blueprint == "Tepuibone" && r.File == "tepuibone"));
        }

        private sealed class TwoChipRoll : System.Random
        { public override int Next(int minValue, int maxValue) => maxValue - 1; }

        [TestCase(24)] [TestCase(12)] [TestCase(0)]
        public void RealHarvestPreservesEveryChipWhenThePackFills(int capacity)
        {
            var previous = HarvestablePart.Factory;
            HarvestablePart.Factory = _factory;
            CavesOfOoo.Diagnostics.Diag.ResetAll();
            try
            {
                var zone = new Zone("Overworld.2.2.0");
                var vein = _factory.CreateEntity("TepuiboneVein"); zone.AddEntity(vein, 10, 10);
                var actor = new Entity(); actor.SetTag("Player");
                var pack = new InventoryPart { MaxWeight = capacity }; actor.AddPart(pack); zone.AddEntity(actor, 9, 10);
                var command = GameEvent.New("InventoryAction"); command.SetParameter("Command", "Harvest");
                command.SetParameter("Actor", actor); command.SetParameter("Zone", zone); command.SetParameter("Random", new TwoChipRoll());
                vein.FireEventAndRelease(command);
                int carried = pack.Objects.Where(e => e.BlueprintName == "Tepuibone").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
                int dropped = zone.GetAllEntities().Where(e => e.BlueprintName == "Tepuibone").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
                Assert.AreEqual(2, carried + dropped, "heavy yield must not disappear at capacity");
                Assert.AreEqual(capacity / 12, carried);
                Assert.IsNull(zone.GetEntityCell(vein));
                var records = CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter
                    { Category = "loot", Kind = "Harvested", Limit = 5 }).Records;
                Assert.AreEqual(1, records.Count);
                StringAssert.Contains("\"count\":" + carried, records[0].PayloadJson);
                if (dropped > 0) StringAssert.Contains("\"dropped\":" + dropped, records[0].PayloadJson);
            }
            finally { HarvestablePart.Factory = previous; CavesOfOoo.Diagnostics.Diag.ResetAll(); }
        }

        [TestCase(1)] [TestCase(17)] [TestCase(83)]
        public void SlopeVeinsPreserveConnectedGroundAndReservedCells(int seed)
        {
            var zone = new Zone("Overworld.2.2.0");
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                zone.AddEntity(_factory.CreateEntity("StoneFloor"), x, y);
            for (int x = 34; x < 45; x++) for (int y = 8; y < 16; y++) zone.GenReservedCells.Add((x, y));
            new StumpFormationBuilder().BuildZone(zone, _factory, new System.Random(seed));
            var veins = zone.GetAllEntities().Where(e => e.BlueprintName == "TepuiboneVein").ToList();
            Assert.Greater(veins.Count, 0);
            foreach (var vein in veins) Assert.IsFalse(zone.GenReservedCells.Contains(zone.GetEntityPosition(vein)));
            var reached = FormationReachability.FloodFromWest(zone, out bool crossed);
            Assert.IsTrue(crossed); Assert.IsTrue(FormationReachability.FullyReached(zone, reached));
        }

        [TestCase("Overworld.2.1.0")] [TestCase("Overworld.3.3.0")] [TestCase("Overworld.2.2.1")]
        public void VeinsDoNotLeakOutOfSurfaceSlopes(string zoneID)
        {
            var zone = new Zone(zoneID);
            new StumpFormationBuilder().BuildZone(zone, _factory, new System.Random(7));
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.BlueprintName == "TepuiboneVein"));
        }
    }
}
