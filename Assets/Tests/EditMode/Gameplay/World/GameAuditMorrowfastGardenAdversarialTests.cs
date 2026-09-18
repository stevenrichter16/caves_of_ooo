using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Independent late-cell failure controls: the first five valid cells must
    // remain untouched when the sixth refuses. All mutations are fixture-owned.
    public sealed class GameAuditMorrowfastGardenAdversarialTests
    {
        private static readonly (int x, int y)[] Cells = {
            (41,21), (41,22), (41,23), (42,21), (42,22), (42,23) };
        private static Entity[] Soil(Zone zone) => Cells.Select(p => zone.GetCell(p.x,p.y).Objects
            .Single(e => e.HasTag(MorrowfastSceneRuntime.TerrainTag))).ToArray();
        private static int Ensure(Zone zone)
        {
            var type = typeof(MorrowfastSceneRuntime).Assembly.GetType("CavesOfOoo.Core.MorrowfastStartingGarden");
            Assert.NotNull(type);
            var method = type.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotNull(method); return (int)method.Invoke(null, new object[] { zone });
        }
        private static void AssertPrepared(Zone zone, Entity[] soil)
        {
            Assert.AreEqual(6, Ensure(zone));
            CollectionAssert.AreEquivalent(soil, zone.GetEntitiesWithTag("Plantable"));
            for (int i=0;i<soil.Length;i++)
            {
                Assert.IsTrue(soil[i].HasTag("Plantable"));
                Assert.AreSame(soil[i], zone.GetCell(Cells[i].x, Cells[i].y).Objects.Single(e => e.HasTag(MorrowfastSceneRuntime.TerrainTag)));
                Assert.AreEqual(Cells[i], zone.GetEntityPosition(soil[i]));
            }
        }

        [TestCase("missing")]
        [TestCase("wrong-id")]
        [TestCase("wrong-blueprint")]
        [TestCase("duplicate-terrain")]
        [TestCase("physics-only-solid")]
        [TestCase("water")]
        [TestCase("barren")]
        [TestCase("interior")]
        public void UnsafeSixthCellRefusesWithoutPartiallyPreparingTheFirstFive(string hazard)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); var soil=Soil(f.Zone); var last=soil[5]; var cell=f.Zone.GetCell(42,23);
                Assert.IsTrue(soil.All(e => !e.HasTag("Plantable")));
                Assert.IsEmpty(f.Zone.GetEntitiesWithTag("Plantable"));
                Action repair;
                switch (hazard)
                {
                    case "missing":
                        Assert.IsTrue(f.Zone.RemoveEntity(last));
                        repair=()=>Assert.IsTrue(f.Zone.AddEntity(last,42,23)); break;
                    case "wrong-id":
                        string oldId=last.ID; last.ID="wrong-native-terrain";
                        repair=()=>last.ID=oldId; break;
                    case "wrong-blueprint":
                        string oldBlueprint=last.BlueprintName; last.BlueprintName="Grass";
                        repair=()=>last.BlueprintName=oldBlueprint; break;
                    case "duplicate-terrain":
                        var duplicate=f.Factory.CreateEntity("TepuiStone"); Assert.NotNull(duplicate);
                        Assert.IsTrue(f.Zone.AddEntity(duplicate,42,23));
                        repair=()=>Assert.IsTrue(f.Zone.RemoveEntity(duplicate)); break;
                    case "physics-only-solid":
                        var physics=last.GetPart<PhysicsPart>(); Assert.NotNull(physics);
                        Assert.IsFalse(last.HasTag("Solid")); Assert.IsFalse(physics.Solid); physics.Solid=true;
                        repair=()=>physics.Solid=false; break;
                    case "water":
                        var pool=new Entity { ID="garden-adversarial-pool", BlueprintName="AuditPool" };
                        pool.AddPart(new LiquidPoolPart { LiquidId="water", Volume=1 });
                        Assert.IsTrue(f.Zone.AddEntity(pool,42,23));
                        repair=()=>Assert.IsTrue(f.Zone.RemoveEntity(pool)); break;
                    case "barren":
                        last.SetTag("Barren"); f.Zone.NotifyEntityTagAdded(last,"Barren");
                        repair=()=> { last.Tags.Remove("Barren"); f.Zone.NotifyEntityTagRemoved(last,"Barren"); }; break;
                    case "interior":
                        Assert.IsFalse(cell.IsInterior); cell.IsInterior=true;
                        repair=()=>cell.IsInterior=false; break;
                    default: throw new ArgumentException(hazard);
                }
                var members=f.Zone.GetReadOnlyEntities().ToArray();
                Assert.AreEqual(0, Ensure(f.Zone), hazard);
                Assert.IsTrue(soil.All(e => !e.HasTag("Plantable")), "No partial tags: "+hazard);
                Assert.IsEmpty(f.Zone.GetEntitiesWithTag("Plantable"), "No partial index: "+hazard);
                CollectionAssert.AreEquivalent(members,f.Zone.GetReadOnlyEntities(),"Refusal cannot replace or remove native members.");
                repair(); AssertPrepared(f.Zone,soil); // same-world positive control after only the hazard is removed
            }
        }

        [Test] public void ValidPreviouslyTaggedSoilRepairsOnlyItsMissingZoneIndexMembership()
        {
            using (var f=new MorrowfastStartFixture())
            {
                f.UseActualVillage(); var soil=Soil(f.Zone); foreach(var tile in soil)tile.SetTag("Plantable");
                Assert.IsEmpty(f.Zone.GetEntitiesWithTag("Plantable"),"SetTag alone does not notify the zone index.");
                var members=f.Zone.GetReadOnlyEntities().ToArray(); AssertPrepared(f.Zone,soil); AssertPrepared(f.Zone,soil);
                CollectionAssert.AreEquivalent(members,f.Zone.GetReadOnlyEntities());
                Assert.IsFalse(f.Zone.GetCell(40,23).Objects.Any(e=>e.HasTag("Plantable")));
            }
        }

        [Test] public void RefusalPreservesExistingPlantableStateAndDoesNotPartiallyRepairStaleIndexes()
        {
            using (var f=new MorrowfastStartFixture())
            {
                f.UseActualVillage(); var soil=Soil(f.Zone);
                soil[0].SetTag("Plantable"); f.Zone.NotifyEntityTagAdded(soil[0],"Plantable");
                soil[1].SetTag("Plantable"); // deliberately stale, owned malformed-state control
                var indexed=f.Zone.GetEntitiesWithTag("Plantable").ToArray();
                var cell=f.Zone.GetCell(42,23); cell.IsInterior=true;
                Assert.AreEqual(0,Ensure(f.Zone));
                CollectionAssert.AreEquivalent(indexed,f.Zone.GetEntitiesWithTag("Plantable"));
                for(int i=0;i<soil.Length;i++)Assert.AreEqual(i<2,soil[i].HasTag("Plantable"));
                cell.IsInterior=false; AssertPrepared(f.Zone,soil);
            }
        }

        [Test] public void VisitingNativeResidentAndDroppedOrdinaryItemDoNotInvalidateAuthoredSoil()
        {
            using (var f=new MorrowfastStartFixture())
            {
                f.UseActualVillage(); var soil=Soil(f.Zone);
                var resident=MorrowfastSceneRuntime.FindOwner(f.Zone,"edden-brack");
                Assert.NotNull(resident); Assert.IsTrue(resident.HasTag("Creature"));
                Assert.NotNull(resident.GetPart<MorrowfastPropPart>());
                Assert.IsTrue(f.Zone.MoveEntity(resident,42,23));
                var dagger=f.Factory.CreateEntity("Dagger"); Assert.NotNull(dagger);
                Assert.IsTrue(f.Zone.AddEntity(dagger,42,23));
                AssertPrepared(f.Zone,soil);
                Assert.AreEqual((42,23),f.Zone.GetEntityPosition(resident));
                Assert.AreEqual((42,23),f.Zone.GetEntityPosition(dagger));
                Assert.IsFalse(resident.HasTag("Plantable")); Assert.IsFalse(dagger.HasTag("Plantable"));
            }
        }

        [Test] public void FreshSpawnAndGardenRemainIdenticalWithEitherPresentationMode()
        {
            bool oldMode=Village3DSettings.Enabled,oldDetail=Village3DSettings.LowDetail;
            try
            {
                foreach(bool enabled in new[] {false,true})
                using(var f=new MorrowfastStartFixture())
                {
                    Village3DSettings.Enabled=enabled; Village3DSettings.LowDetail=!enabled;
                    f.Generate(); f.Place(); Assert.AreEqual(MorrowfastSceneRuntime.ZoneID,f.Zone.ZoneID);
                    Assert.AreEqual((40,23),f.Zone.GetEntityPosition(f.Player));
                    var soil=Soil(f.Zone); Assert.IsTrue(soil.All(e=>e.HasTag("Plantable")));
                    CollectionAssert.AreEquivalent(soil,f.Zone.GetEntitiesWithTag("Plantable"));
                    Assert.IsFalse(f.Zone.GetCell(40,23).Objects.Any(e=>e.HasTag("Plantable")));
                }
            }
            finally { Village3DSettings.Enabled=oldMode; Village3DSettings.LowDetail=oldDetail; }
        }
    }
}
