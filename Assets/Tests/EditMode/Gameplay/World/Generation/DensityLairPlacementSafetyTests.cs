using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityLairPlacementCreationProbe : Part
    {
        public static Action Callback;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "ObjectCreated") Callback?.Invoke(); return true; }
    }

    public sealed class DensityLairPlacementSafetyTests
    {
        DensityLootTestScope scope;
        CavesOfOoo.Data.EntityFactory Factory => scope.Factory;
        Zone zone;
        bool oldChannel;
        readonly PointOfInterest poi = new PointOfInterest(POIType.Lair, "test lair", tier: 2);
        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope(); zone = new Zone("LairSafety-" + Guid.NewGuid().ToString("N"));
            oldChannel = Diag.IsChannelEnabled("worldgen"); Diag.SetChannel("worldgen", true);
        }
        [TearDown] public void Cleanup()
        { DensityLairPlacementCreationProbe.Callback = null; Diag.SetChannel("worldgen", oldChannel); scope.Dispose(); }

        sealed class FixedRng : System.Random
        {
            public int Calls; public bool Last; public int Match = -1, Index;
            public override int Next(int maxValue)
            { Calls++; return maxValue == Match ? Index : Last ? Math.Max(0, maxValue - 1) : 0; }
            public override int Next(int minValue, int maxValue) => minValue;
        }
        bool Place(string blueprint, List<(int x, int y)> cells, System.Random rng) => (bool)typeof(LairPopulationBuilder)
            .GetMethod("PlaceEntity", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(new LairPopulationBuilder(BiomeType.Spread, poi), new object[] { zone, Factory, rng, cells, blueprint });
        List<(int x, int y)> Cells() => new List<(int, int)> { (5, 5), (10, 10) };
        void Shape(string cells) => Factory.Blueprints["GiantSpider"].Parts["SpatialFootprint"] =
            new Dictionary<string, string> { ["CellsRaw"] = cells };
        Entity Block(string mode, int x, int y)
        {
            if (mode == "reserved") { zone.GenReservedCells.Add((x, y)); return null; }
            string blueprint = mode == "solid" ? "Wall" : mode == "creature" ? "MarlbackWallkeeper"
                : mode == "item" ? "Dagger" : mode == "trap" ? "SpikeTrap"
                : mode == "up" ? "StairsUp" : "StairsDown";
            var entity = Factory.CreateEntity(blueprint); Assert.IsTrue(zone.AddEntity(entity, x, y)); return entity;
        }

        [TestCase(true)] [TestCase(false)]
        public void ActualLairPopulationDoesNotPutGuardOnBoss(bool bossPresent)
        {
            var site = new PointOfInterest(POIType.Lair, "probe", tier: 2, bossBlueprint: bossPresent ? "MarlbackWallkeeper" : null);
            Assert.IsTrue(new LairBuilder(BiomeType.Spread, site).BuildZone(zone, Factory, new System.Random(64)));
            Assert.IsTrue(new ConnectivityBuilder().BuildZone(zone, Factory, new System.Random(64)));
            var cells = Enumerable.Range(0, Zone.Width).SelectMany(x => Enumerable.Range(0, Zone.Height).Select(y => (x, y)))
                .Where(p => zone.GetCell(p.x, p.y).IsPassable()).ToArray();
            int index = Array.FindIndex(cells, p => p.x == 40 && p.y == 12); Assert.GreaterOrEqual(index, 0);
            Assert.IsTrue(new LairPopulationBuilder(BiomeType.Spread, site).BuildZone(zone, Factory,
                new FixedRng { Match = cells.Length, Index = index }));
            Assert.AreEqual(1, zone.GetCell(40, 12).Occupants.Count(e => e.HasTag("Creature")));
            Assert.Greater(zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "GiantSpider"), 0);
        }

        [TestCase("reserved")] [TestCase("solid")] [TestCase("creature")] [TestCase("item")]
        [TestCase("trap")] [TestCase("up")] [TestCase("down")]
        public void StaleCandidateListCannotPlaceOnProtectedAnchor(string mode)
        {
            var original = Block(mode, 5, 5); var cells = Cells();
            Assert.IsTrue(Place("GiantSpider", cells, new FixedRng()));
            var spider = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "GiantSpider");
            Assert.AreEqual((10, 10), zone.GetEntityPosition(spider));
            if (original != null) Assert.AreEqual((5, 5), zone.GetEntityPosition(original));
            CollectionAssert.Contains(cells, (5, 5));
            CollectionAssert.DoesNotContain(cells, (10, 10));
        }

        [TestCase("reserved")] [TestCase("trap")] [TestCase("solid")]
        public void EntireProspectiveBodyMustAvoidProtectedCells(string mode)
        {
            Shape("0,0;1,0"); Block(mode, 6, 5);
            Assert.IsTrue(Place("GiantSpider", Cells(), new FixedRng()));
            var spider = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "GiantSpider");
            Assert.AreEqual((10, 10), zone.GetEntityPosition(spider));
            Assert.AreEqual(2, zone.GetOccupiedCells(spider).Count);
        }

        [TestCase("0,0;1,0", 79, 5)] [TestCase("0,0;-1,0", 0, 5)]
        public void ClippedFirstBodyCandidateDoesNotDiscardAValidLaterPlacement(string shape, int x, int y)
        {
            Shape(shape); var cells = new List<(int, int)> { (x, y), (10, 10) };
            Assert.IsTrue(Place("GiantSpider", cells, new FixedRng()));
            Assert.AreEqual((10, 10), zone.GetEntityPosition(zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "GiantSpider")));
        }

        [TestCase("")] [TestCase("bad")] [TestCase("0,0;0,0")]
        public void InvalidBodyNeverConsumesTheCandidatePool(string shape)
        {
            Shape(shape); var cells = Cells(); var rng = new FixedRng();
            Assert.IsFalse(Place("GiantSpider", cells, rng)); Assert.AreEqual(2, cells.Count);
            Assert.IsFalse(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "GiantSpider"));
            Assert.Zero(rng.Calls);
        }

        [TestCase("GiantSpider")] [TestCase("Dagger")]
        public void NoLegalCellLeavesExistingEntityAndPoolIntact(string blueprint)
        {
            var occupant = Block("creature", 5, 5); var cells = new List<(int, int)> { (5, 5) }; var rng = new FixedRng();
            Assert.IsFalse(Place(blueprint, cells, rng)); Assert.AreEqual(1, cells.Count);
            Assert.AreSame(occupant, zone.GetReadOnlyEntities().Single()); Assert.Zero(rng.Calls);
        }

        [TestCase(false)] [TestCase(true)]
        public void SelectingFirstOrLastLegalCellUsesOneDraw(bool last)
        {
            var cells = Cells(); var rng = new FixedRng { Last = last };
            Assert.IsTrue(Place("GiantSpider", cells, rng)); Assert.AreEqual(1, rng.Calls);
            Assert.AreEqual(last ? (10, 10) : (5, 5), zone.GetEntityPosition(zone.GetReadOnlyEntities().Single()));
        }

        [Test]
        public void FactoryCallbackCannotInvalidateAChosenCellBeforePlacement()
        {
            Factory.RegisterPartType<DensityLairPlacementCreationProbe>();
            Factory.Blueprints["GiantSpider"].Parts[nameof(DensityLairPlacementCreationProbe)] = new Dictionary<string, string>();
            DensityLairPlacementCreationProbe.Callback = () => Block("solid", 5, 5);
            Assert.IsTrue(Place("GiantSpider", Cells(), new FixedRng()));
            Assert.AreEqual((10, 10), zone.GetEntityPosition(zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "GiantSpider")));
        }

        [TestCase(null)] [TestCase("")] [TestCase("MissingLairCreature")]
        public void MissingBlueprintDoesNotConsumePoolOrRandomness(string blueprint)
        {
            var cells = Cells(); var rng = new FixedRng();
            Assert.IsFalse(Place(blueprint, cells, rng)); Assert.AreEqual(2, cells.Count); Assert.Zero(rng.Calls);
            Assert.Zero(zone.GetReadOnlyEntities().Count);
        }

        [TestCase(true)] [TestCase(false)]
        public void SuccessfulAndRejectedPlacementsHaveChannelGatedReceipts(bool enabled)
        {
            Diag.SetChannel("worldgen", enabled);
            Assert.IsTrue(Place("GiantSpider", Cells(), new FixedRng()));
            Assert.IsFalse(Place("GiantSpider", new List<(int, int)> { (5, 5) }, new FixedRng()));
            foreach (string kind in new[] { "LairEntityPlaced", "LairPlacementRejected" })
            {
                var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 500 }).Records
                    .Where(e => e.PayloadJson.Contains(zone.ZoneID)).ToArray();
                Assert.AreEqual(enabled ? 1 : 0, records.Length, kind);
                if (enabled) StringAssert.Contains("GiantSpider", records[0].PayloadJson);
            }
        }
    }
}
