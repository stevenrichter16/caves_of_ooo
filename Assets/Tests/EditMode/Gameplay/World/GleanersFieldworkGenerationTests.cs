using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersFieldworkGenerationTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        HaulingContentScope content;
        [SetUp] public void Setup() { content = new HaulingContentScope(); content.Seed(64); }
        [TearDown] public void Cleanup() { content.Dispose(); }

        bool BuildGlade(Zone zone, bool fieldwork, int seed = 64)
        {
            var constructor = typeof(ReferenceGladeBuilder).GetConstructor(new[] { typeof(int), typeof(bool) });
            Assert.NotNull(constructor, "The old constructor must remain legacy; fresh fieldwork requires explicit admission.");
            return ((IZoneBuilder)constructor.Invoke(new object[] { seed, fieldwork })).BuildZone(zone, content.Factory, new Random(1));
        }

        [TestCase(64)]
        [TestCase(1729)]
        [TestCase(729490642)]
        public void NewPlanChangesOnlyTheTwoDeclaredGladeCellsBeforeOwnersExist(int seed)
        {
            var legacy = new Zone(ReferenceGladePlan.ZoneID);
            var fresh = new Zone(ReferenceGladePlan.ZoneID);
            Assert.True(BuildGlade(legacy, false, seed));
            Assert.True(BuildGlade(fresh, true, seed));
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                {
                    if ((x == 46 && y == 4) || (x == 46 && y == 6)) continue;
                    CollectionAssert.AreEqual(legacy.GetCell(x, y).Objects.Select(e => e.BlueprintName).ToArray(),
                        fresh.GetCell(x, y).Objects.Select(e => e.BlueprintName).ToArray(), "Unrelated owner specification changed at " + x + "," + y);
                }
            Assert.AreEqual(legacy.TileState.ToSaveString(), fresh.TileState.ToSaveString());
            CollectionAssert.AreEquivalent(legacy.GenReservedCells, fresh.GenReservedCells);
        }

        [TestCase("GleanersBuckledWicket")]
        [TestCase("DrawgourdCrop")]
        public void MissingNewContentRefusesFreshPacketButLeavesLegacyBuilderUsable(string blueprint)
        {
            content.Factory.Blueprints.Remove(blueprint);
            var fresh = new Zone(ReferenceGladePlan.ZoneID);
            Assert.False(BuildGlade(fresh, true));
            Assert.Zero(fresh.EntityCount);
            Assert.Zero(fresh.GenReservedCells.Count);
            Assert.True(BuildGlade(new Zone(ReferenceGladePlan.ZoneID), false));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ControlledPassageVariantConsumesOnlyItsExactOriginalHedge(bool buckled)
        {
            var zone = new Zone("Overworld.13.11.0");
            var terrain = new SpreadCompositionBuilder(64) { FormationOverride = Formation.Hedgerow, Topology = SpreadExplorationTopology.BrokenEnclosures };
            typeof(SpreadCompositionBuilder).GetField("CapturePassageSources", All).SetValue(terrain, true);
            Assert.True(terrain.BuildZone(zone, content.Factory, new Random(64)));
            var before = zone.GetReadOnlyEntities().ToArray();
            var positions = before.ToDictionary(e => e, zone.GetEntityPosition);
            var method = typeof(SpreadExplorationPassage).GetMethod("TryPlace", All, null,
                new[] { typeof(Zone), typeof(EntityFactory), typeof(SpreadCompositionBuilder), typeof(Entity), typeof(Func<bool>), typeof(bool), typeof(Entity).MakeByRefType(), typeof(Func<bool>).MakeByRefType() }, null);
            Assert.NotNull(method, "The repair variant must share the exact-source passage composer.");
            Entity source = null, gate = null;
            foreach (var hedge in before.Where(e => e.BlueprintName == "Hedge").OrderBy(e => positions[e].y).ThenBy(e => positions[e].x))
            {
                object[] args = { zone, content.Factory, terrain, hedge, new Func<bool>(() => true), buckled, null, null };
                if (!(bool)method.Invoke(null, args)) continue;
                source = hedge; gate = (Entity)args[6];
                Assert.True(((Func<bool>)args[7])());
                break;
            }
            Assert.NotNull(gate, "The same controlled hedgerow used by prior passage tests supplies a useful source.");
            Assert.AreEqual(buckled ? "GleanersBuckledWicket" : "SpreadFieldGate", gate.BlueprintName);
            Assert.AreEqual(buckled, gate.HasPart<RepairablePart>());
            Assert.AreEqual(before.Length, zone.EntityCount);
            Assert.Null(zone.GetEntityCell(source));
            Assert.AreEqual(positions[source], zone.GetEntityPosition(gate));
            foreach (var owner in before.Where(e => e != source))
            {
                Assert.AreSame(zone.GetCell(positions[owner].x, positions[owner].y), zone.GetEntityCell(owner));
                Assert.AreEqual(positions[owner], zone.GetEntityPosition(owner));
            }
        }

        [TestCase(64)]
        [TestCase(1729)]
        public void NewCellarPalletLeavesActualReturnAndBothSourceApproachesReachable(int seed)
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true);
            manager.GetZone(ReferenceGladePlan.ZoneID);
            var zone = manager.GetZone(GleanersCellarBuilder.ZoneID);
            var pallet = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "GleanersTimberPallet");
            Assert.NotNull(pallet);
            var from = zone.GetEntityPosition(pallet);
            Assert.AreEqual((65, seed == 64 ? 8 : 16), from);
            var path = FindPath.Search(zone, 40, 12, from.x + 1, from.y);
            Assert.True(path.Usable, "Recover overflow after dismantling without occupying the original pallet.");
            var actor = content.Factory.CreateEntity("Player");
            actor.GetStat("Strength").BaseValue = 12;
            Assert.True(zone.AddEntity(actor, from.x + 1, from.y));
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, pallet, zone));
            Assert.True(MovementSystem.TryMove(actor, zone, 0, -1));
            DragSystem.Release(actor);
            Assert.AreEqual((from.x + 1, from.y), zone.GetEntityPosition(pallet));
            Assert.False(zone.GetCell(from.x, from.y).BlocksMovement());
            Assert.True(FindPath.Search(zone, 40, 12, from.x, from.y).Usable);
        }

        [Test]
        public void OpenedWicketAndPreparedGardenSurviveRealSaveGraphReplacement()
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            var zone = manager.GetZone(ReferenceGladePlan.ZoneID);
            var gate = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "GleanersBuckledWicket");
            Assert.NotNull(gate);
            var actor = content.Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(actor, 46, 7));
            for (int i = 0; i < 2; i++) Assert.True(actor.GetPart<InventoryPart>().AddObject(content.Factory.CreateEntity("SalvagedTimber")));
            Assert.True(gate.GetPart<RepairablePart>().TryRepair(actor, zone));
            Assert.True(gate.GetPart<DoorPart>().TrySetOpen(actor, zone, true));
            manager.SetActiveZone(zone);
            var session = GameSessionState.Capture("fieldwork", "controlled-native-commands", manager, null, actor);
            GameSessionState saved;
            using (var bytes = new MemoryStream())
            {
                session.Save(new SaveWriter(bytes)); bytes.Position = 0;
                saved = GameSessionState.Load(new SaveReader(bytes, content.Factory));
            }
            var returned = saved.ZoneManager.ActiveZone;
            Assert.AreNotSame(zone, returned);
            var restored = returned.GetReadOnlyEntities().Single(e => e.ID == gate.ID);
            Assert.AreNotSame(gate, restored);
            Assert.True(restored.GetPart<RepairablePart>().Repaired);
            Assert.True(restored.GetPart<DoorPart>().IsOpen);
            Assert.True(CultivatedSoilPart.IsCultivated(returned, returned.GetCell(46, 4)));
            Assert.AreEqual(2, FindPath.Search(returned, 46, 7, 46, 5).Steps.Count);
        }
    }
}
