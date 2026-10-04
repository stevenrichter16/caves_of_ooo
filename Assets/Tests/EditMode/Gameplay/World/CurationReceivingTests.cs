using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CurationReceivingTests
    {
        const string Id = "Overworld.12.12.0";
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static Part IndexPart(Zone zone) => zone.GetReadOnlyEntities().SelectMany(e => e.Parts).Single(p => p.Name == "CurationIntake");
        static T Field<T>(Part part, string field) => (T)part.GetType().GetField(field, All).GetValue(part);
        static bool Certify(Part part, Entity actor, Zone zone)
        {
            try { return (bool)part.GetType().GetMethod("TryCertify").Invoke(part, new object[] { actor, zone }); }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        sealed class NoRandom : Random
        {
            public override int Next() => throw new InvalidOperationException("Caller RNG consumed.");
            public override int Next(int max) => Next();
            public override int Next(int min, int max) => Next();
            public override double NextDouble() => Next();
        }
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope;
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone = new Zone(Id);
            internal readonly MarrowstyeCompositionBuilder Terrain = new MarrowstyeCompositionBuilder(64);
            internal IZoneBuilder Builder;
            internal Fixture()
            {
                var type = typeof(Zone).Assembly.GetType("CavesOfOoo.Core.CurationReceivingBuilder");
                Assert.NotNull(type, "Marrowstye needs its real local receiving work, not only the distant courier quest.");
                Scope = new HaulingContentScope();
                try
                {
                    Scope.Seed(64);
                    Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                    Assert.True(new MarrowstyeProfileBuilder(Terrain).BuildZone(Zone, Factory, new Random(64)));
                    Builder = (IZoneBuilder)Activator.CreateInstance(type, Terrain);
                }
                catch { Scope.Dispose(); throw; }
            }
            internal bool Place() => Builder.BuildZone(Zone, Factory, new NoRandom());
            internal Part Index => IndexPart(Zone);
            internal Entity Player()
            {
                var player = Factory.CreateEntity("Player"); player.GetStat("Strength").BaseValue = 16;
                var at = Zone.GetEntityPosition(Index.ParentEntity);
                var next = new[] { (at.x, at.y + 1), (at.x - 1, at.y), (at.x + 1, at.y) }.First(p => !Zone.GetCell(p.Item1, p.Item2).BlocksMovement());
                Assert.True(Zone.AddEntity(player, next.Item1, next.Item2)); return player;
            }
            internal void Arrange()
            {
                foreach (string side in new[] { "First", "Second" })
                {
                    var body = Field<Entity>(Index, side + "Body"); var at = Zone.GetEntityPosition(Field<Entity>(Index, side + "Bay"));
                    Assert.True(Zone.MoveEntity(body, at.x, at.y));
                }
            }
            public void Dispose() => Scope.Dispose();
        }
        [Test] public void EnrichmentKeepsExactOldCargoAndCourierAndAddsDistinctFiniteContents()
        {
            using (var f = new Fixture())
            {
                var before = f.Zone.GetReadOnlyEntities().ToDictionary(e => e, e => f.Zone.GetEntityPosition(e));
                var bodies = before.Keys.Where(e => e.BlueprintName == "SaltCuredBody").OrderBy(e => before[e].x).ToArray();
                Assert.True(f.Place()); Assert.AreEqual(3865, f.Builder.Priority);
                foreach (var pair in before) { Assert.NotNull(f.Zone.GetEntityCell(pair.Key)); Assert.AreEqual(pair.Value, f.Zone.GetEntityPosition(pair.Key)); }
                Assert.AreSame(bodies[0], Field<Entity>(f.Index, "FirstBody")); Assert.AreSame(bodies[1], Field<Entity>(f.Index, "SecondBody"));
                Assert.AreEqual(2, f.Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SaltCuredBody"));
                Assert.AreEqual(2, f.Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "StoneCoffer"));
                Assert.AreEqual("FilerClerk_1", f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "FilerClerk").GetPart<ConversationPart>().ConversationID);
                Assert.False(Field<bool>(f.Index, "Certified"));
                Assert.AreSame(Field<Entity>(f.Index, "Counterfoil"), f.Index.ParentEntity.GetPart<InventoryPart>().Objects.Single());
                var cabinet = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationToolCabinet");
                CollectionAssert.AreEquivalent(new[] { "CurationSaltRake", "CurationInspectionKey", "CurationTransferDocket", "CurationDiscrepancyReport" }, cabinet.GetPart<ContainerPart>().Contents.Select(e => e.BlueprintName));
                foreach (var item in cabinet.GetPart<ContainerPart>().Contents) Assert.AreSame(cabinet, item.GetPart<PhysicsPart>().InInventory);
                Assert.False(f.Place(), "A second pass must not refill or clone this scene.");
            }
        }
        [TestCase("CurationIntakeIndex")][TestCase("CurationReceivingBay")][TestCase("CurationToolCabinet")]
        [TestCase("CurationCounterfoil")][TestCase("CurationHalfSet")][TestCase("CurationQuarantineRail")]
        public void MissingRequiredBlueprintRefusesBeforeChangingOriginalGraph(string blueprint)
        {
            using (var f = new Fixture())
            {
                f.Factory.Blueprints.Remove(blueprint); var before = f.Zone.GetReadOnlyEntities().ToArray(); var reserved = f.Zone.GenReservedCells.ToArray();
                Assert.False(f.Place()); CollectionAssert.AreEquivalent(before, f.Zone.GetReadOnlyEntities()); CollectionAssert.AreEquivalent(reserved, f.Zone.GenReservedCells);
                Assert.False(before.Any(e => e.Parts.Any(p => p.Name == "CurationReceivingBody")));
            }
        }
        [Test] public void MalformedOriginalSubjectRefusesWithoutPartialNewOwnersOrLabels()
        {
            using (var f = new Fixture())
            {
                var body = f.Zone.GetReadOnlyEntities().First(e => e.BlueprintName == "SaltCuredBody"); body.SetTag("Creature");
                var owners = f.Zone.GetReadOnlyEntities().ToArray(); var reserved = f.Zone.GenReservedCells.ToArray();
                string name = body.GetPart<RenderPart>().DisplayName, text = body.GetPart<ExaminablePart>().Text;
                Assert.False(f.Place()); CollectionAssert.AreEquivalent(owners, f.Zone.GetReadOnlyEntities());
                CollectionAssert.AreEquivalent(reserved, f.Zone.GenReservedCells);
                Assert.AreEqual(name, body.GetPart<RenderPart>().DisplayName); Assert.AreEqual(text, body.GetPart<ExaminablePart>().Text);
                Assert.False(body.Parts.Any(p => p.Name == "CurationReceivingBody"));
            }
        }
        [Test] public void ForeignZoneAndOccupiedNewSlotCannotAcquireAnIntake()
        {
            using (var f = new Fixture())
            {
                Assert.False(f.Builder.BuildZone(new Zone(Id), f.Factory, new NoRandom()));
                var hall = f.Terrain.Plan.Rooms.Single(r => r.Role == "IntakeHall");
                var blocker = f.Factory.CreateEntity("Crate"); Assert.True(f.Zone.AddEntity(blocker, hall.X + hall.Width - 7, hall.Y + 2));
                var before = f.Zone.GetReadOnlyEntities().ToArray(); Assert.False(f.Place()); CollectionAssert.AreEquivalent(before, f.Zone.GetReadOnlyEntities());
            }
        }
        [Test] public void LocalFilingTransfersTheExactStoredCounterfoilOnceWithoutCurrencyOrOpinion()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); var player = f.Player(); var foil = Field<Entity>(f.Index, "Counterfoil"); int drams = TradeSystem.GetDrams(player);
                Assert.False(Certify(f.Index, player, f.Zone)); Assert.AreSame(f.Index.ParentEntity, foil.GetPart<PhysicsPart>().InInventory);
                f.Arrange(); Assert.True(Certify(f.Index, player, f.Zone)); Assert.True(Field<bool>(f.Index, "Certified"));
                Assert.AreSame(player, foil.GetPart<PhysicsPart>().InInventory); Assert.AreSame(foil, player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "CurationCounterfoil"));
                Assert.IsEmpty(f.Index.ParentEntity.GetPart<InventoryPart>().Objects); Assert.False(Certify(f.Index, player, f.Zone)); Assert.AreEqual(drams, TradeSystem.GetDrams(player));
                Assert.True(f.Zone.MoveEntity(Field<Entity>(f.Index, "FirstBody"), 46, 14)); Assert.True(Field<bool>(f.Index, "Certified"), "Certification is historical, not an invisible body lock.");
            }
        }
        [TestCase("missing-body")][TestCase("same-id-replacement")][TestCase("wrong-bay")][TestCase("still-hauled")]
        [TestCase("distant")][TestCase("dead-player")][TestCase("dead-filer")][TestCase("foreign-zone")][TestCase("capacity")]
        [TestCase("hostile-filer")][TestCase("hostile-player")]
        public void CounterChecksKeepThePhysicalCounterfoilAndCertificationUnchanged(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); f.Arrange(); var player = f.Player(); var body = Field<Entity>(f.Index, "FirstBody"); var foil = Field<Entity>(f.Index, "Counterfoil"); var zone = f.Zone;
                if (fault == "missing-body" || fault == "same-id-replacement")
                {
                    var at = zone.GetEntityPosition(body); Assert.True(zone.RemoveEntity(body));
                    if (fault == "same-id-replacement") { var copy = f.Factory.CreateEntity("SaltCuredBody"); copy.ID = body.ID; Assert.True(zone.AddEntity(copy, at.x, at.y)); }
                }
                if (fault == "wrong-bay") Assert.True(zone.MoveEntity(body, 46, 14));
                if (fault == "still-hauled") { var grip = f.Factory.CreateEntity("Player"); var at = zone.GetEntityPosition(body); Assert.True(zone.AddEntity(grip, at.x, at.y + 1)); Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(grip, body, zone)); }
                if (fault == "distant") Assert.True(zone.MoveEntity(player, 40, 12));
                if (fault == "dead-player") player.GetStat("Hitpoints").BaseValue = 0;
                if (fault == "dead-filer") Field<Entity>(f.Index, "Filer").GetStat("Hitpoints").BaseValue = 0;
                if (fault == "hostile-filer") Field<Entity>(f.Index, "Filer").GetPart<BrainPart>().SetPersonallyHostile(player, false);
                if (fault == "hostile-player")
                {
                    var brain = player.GetPart<BrainPart>(); if (brain == null) { brain = new BrainPart(); player.AddPart(brain); }
                    brain.SetPersonallyHostile(Field<Entity>(f.Index, "Filer"), false);
                }
                if (fault == "foreign-zone") zone = new Zone(Id);
                if (fault == "capacity") player.GetPart<InventoryPart>().MaxWeight = 0;
                Assert.False(Certify(f.Index, player, zone)); Assert.False(Field<bool>(f.Index, "Certified"));
                Assert.AreSame(f.Index.ParentEntity, foil.GetPart<PhysicsPart>().InInventory); Assert.AreSame(foil, f.Index.ParentEntity.GetPart<InventoryPart>().Objects.Single());
                Assert.False(player.GetPart<InventoryPart>().Objects.Contains(foil));
            }
        }
        sealed class ThrowAfter : Part
        {
            public override string Name => "CurationTestThrowAfter";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("test callback"); return true; }
        }
        [Test] public void FailedOuterInventoryCommandRestoresKeyAndCompletionThenAllowsRetry()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); f.Arrange(); var player = f.Player(); var listener = new ThrowAfter(); player.AddPart(listener);
                var foil = Field<Entity>(f.Index, "Counterfoil"); Assert.False(Certify(f.Index, player, f.Zone)); Assert.False(Field<bool>(f.Index, "Certified"));
                Assert.AreSame(f.Index.ParentEntity, foil.GetPart<PhysicsPart>().InInventory); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(foil));
                player.RemovePart(listener); Assert.True(Certify(f.Index, player, f.Zone));
            }
        }
        [Test] public void QuarantineGateHasAReachablePublicStandingPlaceAndOpensFromThatPlace()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); var gate = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationQuarantineGate");
                var at = f.Zone.GetEntityPosition(gate); var reach = FormationReachability.FloodFromWest(f.Zone);
                Assert.True(reach[at.x - 1, at.y], "A real public approach must reach the west-facing gate without entering the cage.");
                Assert.False(f.Zone.GetCell(at.x - 1, at.y).BlocksMovement()); Assert.AreEqual(1, gate.GetPart<DoorPart>().QuarterTurns);
                var player = f.Factory.CreateEntity("Player"); Assert.True(f.Zone.AddEntity(player, at.x - 1, at.y));
                var door = gate.GetPart<DoorPart>(); Assert.False(door.CanOperate(player, f.Zone));
                Assert.False(door.TrySetOpen(player, f.Zone, true), "Public frontage does not bypass the inspection lock.");
                Assert.True(player.GetPart<InventoryPart>().AddObject(f.Factory.CreateEntity("CurationInspectionKey")));
                Assert.True(InventorySystem.PerformAction(player, gate, "Unlock", f.Zone)); Assert.True(door.CanOperate(player, f.Zone)); Assert.True(door.TrySetOpen(player, f.Zone, true));
                Assert.False(f.Zone.GetCell(at.x, at.y).BlocksMovement(player));
            }
        }
        [Test] public void QuarantineIsActuallyEnclosedWithOnePhysicalLockedDoorAndNoEnemyDoorPermission()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Place()); var room = f.Terrain.Plan.Rooms.Single(r => r.Role == "DisusedWing");
                var enemy = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationHalfSet");
                var gate = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationQuarantineGate");
                Assert.True(gate.GetPart<LockPart>().IsLocked); Assert.True(gate.GetPart<DoorPart>().IsClosed); Assert.False(enemy.HasTag("CanOpenDoors"));
                Assert.False(gate.GetPart<DoorPart>().CanOperate(enemy, f.Zone));
                var start = f.Zone.GetEntityPosition(enemy); var reached = new HashSet<(int x, int y)> { start }; var pending = new Queue<(int x, int y)>(); pending.Enqueue(start);
                while (pending.Count > 0)
                {
                    var p = pending.Dequeue();
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    { var n = (x:p.x + dx,y:p.y + dy); if (f.Zone.InBounds(n.x,n.y) && !f.Zone.GetCell(n.x,n.y).BlocksMovement() && reached.Add(n)) pending.Enqueue(n); }
                }
                Assert.That(reached.Count, Is.InRange(30, 50)); Assert.True(reached.All(p => p.x > room.X + 9 && p.x < room.X + 20 && p.y > room.Y && p.y < room.Y + 6));
                Assert.False(f.Zone.GetCell(room.DoorX, room.DoorY).BlocksMovement());
            }
        }
        [TestCase(false)][TestCase(true)] public void OrdinaryOrUnconfiguredMarrowstyeDoesNotAcquireRetention(bool addIndex)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true); var zone = new Zone(Id);
                if (addIndex) Assert.True(zone.AddEntity(scope.Factory.CreateEntity("CurationIntakeIndex"), 20, 5));
                manager.SetActiveZone(zone); manager.UnloadZone(Id);
                Assert.False(manager.CachedZones.ContainsKey(Id), "The retention exception requires the configured exact native scene.");
            }
        }
        [TestCase(64)][TestCase(1729)] public void NativeGenerationAndSaveRetainBoundBodiesAndNeverReissueOrRefill(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(seed); var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(Id); Assert.NotNull(zone);
                Assert.True(zone.GetReadOnlyEntities().Any(e => e.Parts.Any(p => p.Name == "CurationIntake")), "Actual Marrowstye pipeline must enrich the receiving hall.");
                var index = IndexPart(zone); var player = scope.Factory.CreateEntity("Player"); var at = zone.GetEntityPosition(index.ParentEntity);
                Assert.True(zone.AddEntity(player, at.x, at.y + 1)); manager.SetActiveZone(zone);
                foreach (string side in new[] { "First", "Second" }) { var bay = zone.GetEntityPosition(Field<Entity>(index, side + "Bay")); Assert.True(zone.MoveEntity(Field<Entity>(index, side + "Body"), bay.x, bay.y)); }
                var foil = Field<Entity>(index, "Counterfoil"); Assert.True(Certify(index, player, zone));
                var cabinet = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationToolCabinet"); var item = cabinet.GetPart<ContainerPart>().Contents[0]; Assert.True(cabinet.GetPart<ContainerPart>().RemoveItem(item));
                var saved = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("curation", "local-filing", manager, null, player)); var restored = saved.ZoneManager.ActiveZone; var again = IndexPart(restored);
                Assert.True(Field<bool>(again, "Certified")); Assert.AreNotSame(Field<Entity>(index, "FirstBody"), Field<Entity>(again, "FirstBody"));
                Assert.NotNull(restored.GetEntityCell(Field<Entity>(again, "FirstBody"))); Assert.AreEqual(Field<Entity>(index, "FirstBody").ID, Field<Entity>(again, "FirstBody").ID);
                var restoredPlayer = restored.GetReadOnlyEntities().Single(e => e.ID == player.ID); Assert.AreSame(Field<Entity>(again, "Counterfoil"), restoredPlayer.GetPart<InventoryPart>().Objects.Single(e => e.ID == foil.ID));
                Assert.False(Certify(again, restoredPlayer, restored)); Assert.False(restored.GetReadOnlyEntities().Single(e => e.ID == cabinet.ID).GetPart<ContainerPart>().Contents.Any(e => e.ID == item.ID));
                saved.ZoneManager.UnloadZone(Id); Assert.AreSame(restored, saved.ZoneManager.GetZone(Id));
            }
        }
    }
}
