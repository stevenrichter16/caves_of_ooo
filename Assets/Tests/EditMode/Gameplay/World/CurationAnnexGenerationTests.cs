using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-scale architecture: an optional gallery, a distinct holding
    /// room and a screened service approach, all governed by actual doors.</summary>
    public sealed class CurationAnnexGenerationTests
    {
        const string Id = "Overworld.12.12.0";
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone = new Zone(Id);
            internal readonly MarrowstyeCompositionBuilder Terrain;
            internal readonly CurationReceivingBuilder Receiving;
            internal MarrowstyeCompositionPlan.Room Room => Terrain.Plan.Rooms.Single(r => r.Role == "DisusedWing");
            internal Fixture(int seed = 64)
            {
                try
                {
                    Scope.Seed(seed); Terrain = new MarrowstyeCompositionBuilder(seed);
                    Assert.True(Terrain.BuildZone(Zone, Factory, new Random(seed)));
                    Assert.True(new MarrowstyeProfileBuilder(Terrain).BuildZone(Zone, Factory, new Random(seed)));
                    Receiving = new CurationReceivingBuilder(Terrain);
                }
                catch { Scope.Dispose(); throw; }
            }
            internal void Build() => Assert.True(Receiving.BuildZone(Zone, Factory, new Random(64)));
            internal Entity Owner(string blueprint) => Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == blueprint);
            internal (int x, int y) At(int x, int y) => (Room.X + x, Room.Y + y);
            internal Cell Cell(int x, int y) => Zone.GetCell(Room.X + x, Room.Y + y);
            public void Dispose() => Scope.Dispose();
        }
        static HashSet<(int x, int y)> Flood(Zone zone, (int x, int y) start, bool ignoreActors = true)
        {
            var seen = new HashSet<(int x, int y)> { start }; var pending = new Queue<(int x, int y)>(); pending.Enqueue(start);
            while (pending.Count != 0)
            {
                var p = pending.Dequeue();
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    var n = (x:p.x + dx, y:p.y + dy); if (!zone.InBounds(n.x,n.y) || seen.Contains(n)) continue;
                    if (zone.GetCell(n.x,n.y).Objects.Any(e => (!ignoreActors || !e.HasTag("Creature")) &&
                        (e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true || e.GetPart<DoorPart>()?.IsClosed == true))) continue;
                    seen.Add(n); pending.Enqueue(n);
                }
            }
            return seen;
        }
        static int Units(ContainerPart container, string blueprint) => container.Contents.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void PlanMakesRoomForAnInspectionGalleryAndHoldingRoomWithoutBlockingThePublicCrossroads(int seed)
        {
            var plan = MarrowstyeCompositionPlan.Create(Id,seed); var room = plan.Rooms.Single(r => r.Role == "DisusedWing");
            Assert.AreEqual(27,room.Width); Assert.AreEqual(9,room.Height); Assert.AreEqual(15,room.Y);
            Assert.AreEqual(room.X + 5,room.DoorX); Assert.Less(room.X + room.Width - 1,40);
            foreach (var p in new[]{(9,1),(9,3),(20,2),(20,4),(11,6),(21,6)}) Assert.AreEqual("SandstoneWall",plan.ObjectAt(room.X+p.Item1,room.Y+p.Item2));
            foreach (var p in new[]{(9,2),(20,3),(15,6),(23,6)}) Assert.IsNull(plan.ObjectAt(room.X+p.Item1,room.Y+p.Item2),"Door apertures belong to the later receiving builder.");
            for(int x=1;x<=25;x++) Assert.IsNull(plan.ObjectAt(room.X+x,room.Y+7),"The service passage must have actual walkable clearance.");
            Assert.IsNull(plan.ObjectAt(40,14)); Assert.IsNull(plan.ObjectAt(40,24));
        }

        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void NewArrivalReachesRepairMaterialsAndHoldingRoomButCannotReachTheLiveGallery(int seed)
        {
            using(var f=new Fixture(seed))
            {
                f.Build(); var publicFloor=Flood(f.Zone,(40,11)); var enemy=f.Owner("CurationHalfSet"); var gallery=Flood(f.Zone,f.Zone.GetEntityPosition(enemy));
                Assert.True(publicFloor.Contains(f.At(3,4))); Assert.True(publicFloor.Contains(f.At(23,7))); Assert.True(publicFloor.Contains(f.At(23,5)));
                Assert.False(publicFloor.Contains(f.Zone.GetEntityPosition(enemy))); Assert.False(gallery.Contains(f.At(23,5))); Assert.False(gallery.Contains((40,11)));
                Assert.That(gallery.Count,Is.GreaterThan(30)); Assert.True(gallery.All(p=>p.x>f.Room.X+9&&p.x<f.Room.X+20&&p.y>f.Room.Y&&p.y<f.Room.Y+6));
                Assert.AreEqual(f.At(9,2),f.Zone.GetEntityPosition(f.Owner("CurationQuarantineGate")));
                Assert.AreEqual(f.At(20,3),f.Zone.GetEntityPosition(f.Owner("CurationTransferGate")));
                Assert.AreEqual(f.At(15,6),f.Zone.GetEntityPosition(f.Owner("CurationGalleryGate")));
                Assert.AreEqual(f.At(23,6),f.Zone.GetEntityPosition(f.Owner("CurationServiceGate")));
                Assert.False(enemy.HasTag("CanOpenDoors")); Assert.True(f.Owner("CurationQuarantineGate").GetPart<LockPart>().IsLocked);
            }
        }

        [Test]
        public void ServiceApproachIsScreenedByRealMasonryAndTheUnrepairedEscapeGateCannotClose()
        {
            using(var f=new Fixture())
            {
                f.Build();var enemy=f.Zone.GetEntityPosition(f.Owner("CurationHalfSet"));var passage=f.At(14,7);
                Assert.False(AIHelpers.HasLineOfSight(f.Zone,enemy.x,enemy.y,passage.x,passage.y));
                var gate=f.Owner("CurationServiceGate"); Assert.True(gate.GetPart<DoorPart>().IsOpen); Assert.True(RepairablePart.BlocksFunction(gate));
                Assert.AreEqual("timber-gate-frame",gate.GetPart<RepairablePart>().RecipeId); Assert.True(gate.GetPart<CompositionPart>().Contains("Wood"));
                var player=f.Factory.CreateEntity("Player"); Assert.True(f.Zone.AddEntity(player,f.Room.X+23,f.Room.Y+7));
                Assert.False(gate.GetPart<DoorPart>().TrySetOpen(player,f.Zone,false)); Assert.True(gate.GetPart<DoorPart>().IsOpen);
                Assert.False(f.Owner("CurationTransferGate").GetPart<DoorPart>().IsOpen); Assert.False(f.Owner("CurationGalleryGate").GetPart<DoorPart>().IsOpen);
            }
        }

        [Test]
        public void AnnexSuppliesAreFinitePhysicalContentsSeparateFromTheExistingCertificationReward()
        {
            using(var f=new Fixture())
            {
                var original=f.Zone.GetReadOnlyEntities().ToDictionary(e=>e,e=>f.Zone.GetEntityPosition(e)); f.Build();
                foreach(var pair in original) Assert.AreEqual(pair.Value,f.Zone.GetEntityPosition(pair.Key),"Enrichment must preserve each prior owner and position.");
                var maintenance=f.Owner("CurationMaintenanceRack").GetPart<ContainerPart>(); Assert.AreEqual(2,Units(maintenance,"SalvagedTimber"));
                var recovery=f.Owner("CurationRecoveryCabinet").GetPart<ContainerPart>();
                foreach(var expected in new[]{("FireClay",2),("SoddenFieldDressing",1),("LeatherGloves",1)}) Assert.AreEqual(expected.Item2,Units(recovery,expected.Item1),expected.Item1);
                var conservation=f.Owner("CurationConservationCase").GetPart<ContainerPart>();
                Assert.AreEqual(2,Units(conservation,"SootrootPulp"));Assert.AreEqual(1,Units(conservation,"PitchpodResin"));
                foreach(var owner in new[]{f.Owner("CurationMaintenanceRack"),f.Owner("CurationRecoveryCabinet"),f.Owner("CurationConservationCase")})
                    foreach(var item in owner.GetPart<ContainerPart>().Contents) Assert.AreSame(owner,item.GetPart<PhysicsPart>().InInventory);
                var old=f.Owner("CurationToolCabinet").GetPart<ContainerPart>(); CollectionAssert.AreEquivalent(new[]{"CurationSaltRake","CurationInspectionKey","CurationTransferDocket","CurationDiscrepancyReport"},old.Contents.Select(e=>e.BlueprintName));
                var taken=recovery.Contents.First(); Assert.True(recovery.RemoveItem(taken)); Assert.False(f.Receiving.BuildZone(f.Zone,f.Factory,new Random(64))); Assert.False(recovery.Contents.Contains(taken));
            }
        }

        [Test]
        public void PhysicalRepairAndDoorsCanEncloseTheSameLivingThreatWithoutCertificationOrACompletionFlag()
        {
            using(var f=new Fixture())
            {
                f.Build();var actor=f.Factory.CreateEntity("Player");Assert.True(f.Zone.AddEntity(actor,f.Room.X+23,f.Room.Y+7));
                var service=f.Owner("CurationServiceGate");var repair=service.GetPart<RepairablePart>();var door=service.GetPart<DoorPart>();
                Assert.False(repair.TryRepair(actor,f.Zone));Assert.False(repair.Repaired);Assert.True(door.IsOpen);
                var rack=f.Owner("CurationMaintenanceRack").GetPart<ContainerPart>();var timber=rack.Contents.Single();
                Assert.True(rack.RemoveItem(timber));Assert.True(actor.GetPart<InventoryPart>().AddObject(timber));
                Assert.True(repair.TryRepair(actor,f.Zone));Assert.True(repair.Repaired);Assert.IsEmpty(rack.Contents);
                Assert.False(actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="SalvagedTimber"));
                var loose=f.Factory.CreateEntity("FireClay");Assert.True(f.Zone.AddEntity(loose,f.Room.X+23,f.Room.Y+6));
                Assert.False(door.TrySetOpen(actor,f.Zone,false),"Loose material really obstructs the threshold; repair does not bypass closure rules.");
                Assert.True(f.Zone.RemoveEntity(loose));Assert.True(door.TrySetOpen(actor,f.Zone,false));
                var enemy=f.Owner("CurationHalfSet");int hp=enemy.GetStatValue("Hitpoints");
                // Geometry fixture: position the same living owner in the holding
                // room. The native-input acceptance route must prove actual luring.
                Assert.True(f.Zone.MoveEntity(enemy,f.Room.X+23,f.Room.Y+3));var held=Flood(f.Zone,f.Zone.GetEntityPosition(enemy));
                Assert.True(held.Contains(f.At(21,1)));Assert.False(held.Contains(f.At(14,3)));Assert.False(held.Contains(f.At(23,7)));
                Assert.AreEqual(hp,enemy.GetStatValue("Hitpoints"));Assert.False(f.Owner("CurationIntakeIndex").GetPart<CurationIntakePart>().Certified);
                Assert.False(repair.TryRepair(actor,f.Zone),"Repaired faults cannot spend materials a second time.");
            }
        }

        [Test]
        public void NativeSaveRetainsRepairedClosureTheSameLivingCaseAndDepletedRecoveryStock()
        {
            using(var f=new Fixture())
            {
                f.Build();var actor=f.Factory.CreateEntity("Player");Assert.True(f.Zone.AddEntity(actor,f.Room.X+23,f.Room.Y+7));
                var rack=f.Owner("CurationMaintenanceRack").GetPart<ContainerPart>();var timber=rack.Contents.Single();
                Assert.True(rack.RemoveItem(timber));Assert.True(actor.GetPart<InventoryPart>().AddObject(timber));
                var service=f.Owner("CurationServiceGate");Assert.True(service.GetPart<RepairablePart>().TryRepair(actor,f.Zone));Assert.True(service.GetPart<DoorPart>().TrySetOpen(actor,f.Zone,false));
                var enemy=f.Owner("CurationHalfSet");Assert.True(f.Zone.MoveEntity(enemy,f.Room.X+23,f.Room.Y+3));
                var recovery=f.Owner("CurationRecoveryCabinet").GetPart<ContainerPart>();var removed=recovery.Contents.First();Assert.True(recovery.RemoveItem(removed));Assert.True(actor.GetPart<InventoryPart>().AddObject(removed));
                var deep=f.Owner("CurationConservationCase").GetPart<ContainerPart>();var deepTaken=deep.Contents.First();Assert.True(deep.RemoveItem(deepTaken));Assert.True(actor.GetPart<InventoryPart>().AddObject(deepTaken));
                var manager=OverworldZoneManager.CreateDetached(f.Factory,64,true);manager.SetActiveZone(f.Zone);
                var saved=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("annex","contained",manager,null,actor));var zone=saved.ZoneManager.ActiveZone;
                var restoredService=zone.GetReadOnlyEntities().Single(e=>e.ID==service.ID);Assert.AreNotSame(service,restoredService);
                Assert.True(restoredService.GetPart<RepairablePart>().Repaired);Assert.True(restoredService.GetPart<DoorPart>().IsClosed);
                var restoredEnemy=zone.GetReadOnlyEntities().Single(e=>e.ID==enemy.ID);Assert.AreNotSame(enemy,restoredEnemy);Assert.AreEqual(14,restoredEnemy.GetStatValue("Hitpoints"));
                Assert.AreEqual(f.At(23,3),zone.GetEntityPosition(restoredEnemy));Assert.False(Flood(zone,zone.GetEntityPosition(restoredEnemy)).Contains((40,11)));
                Assert.IsEmpty(zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="CurationMaintenanceRack").GetPart<ContainerPart>().Contents);
                Assert.False(zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="CurationRecoveryCabinet").GetPart<ContainerPart>().Contents.Any(e=>e.ID==removed.ID));
                Assert.False(zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="CurationConservationCase").GetPart<ContainerPart>().Contents.Any(e=>e.ID==deepTaken.ID));
                saved.ZoneManager.UnloadZone(Id);Assert.AreSame(zone,saved.ZoneManager.GetZone(Id));
            }
        }

        [TestCase("CurationTransferGate")][TestCase("CurationGalleryGate")]
        public void AFactoryOpenedContainmentGateRefusesTheWholeAnnexInsteadOfPublishingAnEscapedEnemy(string blueprint)
        {
            using(var f=new Fixture())
            {
                Assert.True(f.Factory.Blueprints.ContainsKey(blueprint));f.Factory.Blueprints[blueprint].Parts["Door"]["IsOpen"]="true";
                var before=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Receiving.BuildZone(f.Zone,f.Factory,new Random(64)));
                CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());Assert.False(before.Any(e=>e.HasPart<CurationReceivingBodyPart>()));
            }
        }

        [TestCase("CurationTransferGate")][TestCase("CurationServiceGate")][TestCase("CurationGalleryGate")]
        [TestCase("CurationConservationCase")][TestCase("CurationRecoveryCabinet")][TestCase("CurationMaintenanceRack")][TestCase("CurationInspectionSlab")][TestCase("CurationAnnexPlacard")]
        public void MissingAnnexOwnerRefusesBeforePublishingAnyNewStockOrChangingPublicSubjects(string missing)
        {
            using(var f=new Fixture())
            {
                f.Factory.Blueprints.Remove(missing);var before=f.Zone.GetReadOnlyEntities().ToArray();var reservations=f.Zone.GenReservedCells.ToArray();
                Assert.False(f.Receiving.BuildZone(f.Zone,f.Factory,new Random(64))); CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities()); CollectionAssert.AreEquivalent(reservations,f.Zone.GenReservedCells);
                Assert.False(before.Any(e=>e.HasPart<CurationReceivingBodyPart>()));
            }
        }
    }
}
