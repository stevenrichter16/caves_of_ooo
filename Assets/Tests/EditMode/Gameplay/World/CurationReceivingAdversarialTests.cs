using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Authored intake boundary probes: identity, reentry, current-world
    /// authority, partial saves, and separation of certification from containment.</summary>
    public sealed class CurationReceivingAdversarialTests
    {
        const string Id = "Overworld.12.12.0";
        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone = new Zone(Id);
            internal readonly CurationIntakePart Index;
            internal readonly Entity Player;
            internal Fixture()
            {
                try
                {
                    Scope.Seed(64); var terrain = new MarrowstyeCompositionBuilder(64);
                    Assert.True(terrain.BuildZone(Zone, Factory, new Random(64)));
                    Assert.True(new MarrowstyeProfileBuilder(terrain).BuildZone(Zone, Factory, new Random(64)));
                    Assert.True(new CurationReceivingBuilder(terrain).BuildZone(Zone, Factory, new Random(64)));
                    Index = Zone.GetReadOnlyEntities().Select(e => e.GetPart<CurationIntakePart>()).Single(p => p != null);
                    Player = Factory.CreateEntity("Player"); var at = Zone.GetEntityPosition(Index.ParentEntity);
                    Assert.True(Zone.AddEntity(Player, at.x, at.y + 1)); Arrange();
                }
                catch { Scope.Dispose(); throw; }
            }
            internal void Arrange()
            {
                var a = Zone.GetEntityPosition(Index.FirstBay); var b = Zone.GetEntityPosition(Index.SecondBay);
                Assert.True(Zone.MoveEntity(Index.FirstBody, a.x, a.y)); Assert.True(Zone.MoveEntity(Index.SecondBody, b.x, b.y));
            }
            internal void Refused()
            {
                Assert.False(Index.TryCertify(Player, Zone)); Assert.False(Index.Certified);
                Assert.AreSame(Index.ParentEntity, Index.Counterfoil.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(Index.Counterfoil, Index.ParentEntity.GetPart<InventoryPart>().Objects.Single());
                Assert.False(Player.GetPart<InventoryPart>().Objects.Contains(Index.Counterfoil));
            }
            public void Dispose() => Scope.Dispose();
        }
        // Same physical object but a changed persistent identity must not inherit
        // the old file's authority merely because it is still on the expected tile.
        [TestCase("FirstBody")][TestCase("SecondBody")][TestCase("FirstBay")][TestCase("SecondBay")]
        [TestCase("Filer")][TestCase("Indexer")][TestCase("Counterfoil")][TestCase("Index")]
        public void ChangedPersistentIdentityCannotIssueTheOriginalAccessItem(string owner)
        {
            using (var f = new Fixture())
            {
                var entity = owner == "Index" ? f.Index.ParentEntity : (Entity)typeof(CurationIntakePart).GetField(owner).GetValue(f.Index);
                string id = entity.ID; entity.ID = "changed-" + id; f.Refused(); entity.ID = id;
                Assert.True(f.Index.TryCertify(f.Player, f.Zone), "Restoring the exact identity restores this otherwise valid file.");
            }
        }
        [TestCase(true)][TestCase(false)]
        public void MovingALabelAndItsMatchingSubjectTogetherDoesNotMoveTheAssignedBay(bool first)
        {
            using (var f = new Fixture())
            {
                var bay = first ? f.Index.FirstBay : f.Index.SecondBay; var body = first ? f.Index.FirstBody : f.Index.SecondBody;
                var at = f.Zone.GetEntityPosition(bay); Assert.True(f.Zone.MoveEntity(bay, at.x, at.y + 1)); Assert.True(f.Zone.MoveEntity(body, at.x, at.y + 1));
                f.Refused(); Assert.True(f.Zone.MoveEntity(bay, at.x, at.y)); Assert.True(f.Zone.MoveEntity(body, at.x, at.y));
                Assert.True(f.Index.TryCertify(f.Player, f.Zone));
            }
        }
        [TestCase(true)][TestCase(false)]
        public void SameBlueprintAndIDReplacementCannotStandInForEitherNamedWorker(bool filer)
        {
            using (var f = new Fixture())
            {
                var original = filer ? f.Index.Filer : f.Index.Indexer; var at = f.Zone.GetEntityPosition(original);
                var replacement = f.Factory.CreateEntity(original.BlueprintName); replacement.ID = original.ID;
                Assert.True(f.Zone.RemoveEntity(original)); Assert.True(f.Zone.AddEntity(replacement, at.x, at.y)); f.Refused();
                Assert.True(f.Zone.RemoveEntity(replacement)); Assert.True(f.Zone.AddEntity(original, at.x, at.y));
                Assert.True(f.Index.TryCertify(f.Player, f.Zone));
            }
        }
        [TestCase("wrong-stamp")][TestCase("stack-two")][TestCase("foreign-key-part")]
        public void CorruptPhysicalCounterfoilCannotCompleteTheFile(string corruption)
        {
            using (var f = new Fixture())
            {
                var foil = f.Index.Counterfoil; var key = foil.GetPart<KeyPart>();
                if (corruption == "wrong-stamp") key.KeyId = "marrowstye-quarantine";
                if (corruption == "stack-two") foil.GetPart<StackerPart>().StackCount = 2;
                if (corruption == "foreign-key-part") key.ParentEntity = f.Player;
                f.Refused();
            }
        }
        [TestCase("cached-clone")][TestCase("changed-site")]
        public void AnAddressMatchDoesNotOverrideTheCurrentWorldGraph(string mutation)
        {
            using (var f = new Fixture())
            {
                var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone);
                if (mutation == "cached-clone") manager.SetActiveZone(new Zone(Id));
                else manager.WorldMap.SetPOI(12, 12, new PointOfInterest(POIType.Village, "another place", null, profile: "TentCamp"));
                f.Refused();
            }
        }
        sealed class BeforeAction : Part
        {
            public override string Name => "CurationAdversarialBeforeAction";
            internal Action Once; bool ran;
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "BeforeInventoryAction" && !ran) { ran = true; Once(); } return true; }
        }
        [Test] public void WorkerLostBetweenPreflightAndActionLeavesThePhysicalKeyInTheIndex()
        {
            using (var f = new Fixture())
            {
                f.Player.AddPart(new BeforeAction { Once = () => f.Index.Indexer.GetStat("Hitpoints").BaseValue = 0 });
                f.Refused();
            }
        }
        [Test] public void ReentrantCertificationProducesExactlyOneSuccessfulIssue()
        {
            using (var f = new Fixture())
            {
                bool nested = false; f.Player.AddPart(new BeforeAction { Once = () => nested = f.Index.TryCertify(f.Player, f.Zone) });
                bool outer = f.Index.TryCertify(f.Player, f.Zone);
                Assert.AreEqual(1, new[] { outer, nested }.Count(v => v)); Assert.True(f.Index.Certified);
                Assert.AreSame(f.Index.Counterfoil, f.Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "CurationCounterfoil"));
                Assert.IsEmpty(f.Index.ParentEntity.GetPart<InventoryPart>().Objects);
            }
        }
        [Test] public void SavingAHalfFinishedArrangementPreservesWhichExactSubjectStillNeedsMoving()
        {
            using (var f = new Fixture())
            {
                var previous = f.Zone.GetEntityPosition(f.Index.SecondBody); Assert.True(f.Zone.MoveEntity(f.Index.SecondBody, previous.x, previous.y + 1));
                var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone);
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("curation", "unfinished", manager, null, f.Player));
                var zone = loaded.ZoneManager.ActiveZone; var index = zone.GetReadOnlyEntities().Select(e => e.GetPart<CurationIntakePart>()).Single(p => p != null);
                var actor = zone.GetReadOnlyEntities().Single(e => e.ID == f.Player.ID);
                Assert.False(index.Certified); Assert.False(index.TryCertify(actor, zone));
                Assert.AreSame(index.Counterfoil, index.ParentEntity.GetPart<InventoryPart>().Objects.Single());
                Assert.AreEqual(zone.GetEntityPosition(index.FirstBay), zone.GetEntityPosition(index.FirstBody));
                Assert.AreNotEqual(zone.GetEntityPosition(index.SecondBay), zone.GetEntityPosition(index.SecondBody));
                var destination = zone.GetEntityPosition(index.SecondBay); Assert.True(zone.MoveEntity(index.SecondBody, destination.x, destination.y));
                Assert.True(index.TryCertify(actor, zone));
            }
        }
        [Test] public void HistoricalCertificationSurvivesLaterCargoAndKeyTransferWithoutIssuingToASecondActor()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Index.TryCertify(f.Player, f.Zone)); var foil = f.Index.Counterfoil;
                Assert.True(f.Zone.MoveEntity(f.Index.FirstBody, 46, 14)); Assert.True(f.Zone.MoveEntity(f.Index.SecondBody, 47, 14));
                var other = f.Factory.CreateEntity("Player"); var at = f.Zone.GetEntityPosition(f.Index.ParentEntity); Assert.True(f.Zone.AddEntity(other, at.x - 1, at.y));
                Assert.True(f.Player.GetPart<InventoryPart>().RemoveObject(foil)); Assert.True(other.GetPart<InventoryPart>().AddObject(foil));
                Assert.False(f.Index.TryCertify(other, f.Zone)); Assert.False(f.Index.TryCertify(f.Player, f.Zone));
                Assert.True(f.Index.Certified); Assert.IsEmpty(f.Index.ParentEntity.GetPart<InventoryPart>().Objects);
                Assert.AreSame(foil, other.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "CurationCounterfoil"));
            }
        }
        [Test] public void UnlockingDoesNotGiveTheContainedCreatureDoorOperatingPermission()
        {
            using (var f = new Fixture())
            {
                var gate = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationQuarantineGate"); var at = f.Zone.GetEntityPosition(gate);
                Assert.True(f.Zone.MoveEntity(f.Player, at.x - 1, at.y)); Assert.True(f.Player.GetPart<InventoryPart>().AddObject(f.Factory.CreateEntity("CurationInspectionKey")));
                Assert.True(InventorySystem.PerformAction(f.Player, gate, "Unlock", f.Zone)); var door = gate.GetPart<DoorPart>();
                Assert.False(gate.GetPart<LockPart>().IsLocked); Assert.True(door.IsClosed);
                var enemy = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationHalfSet");
                Assert.False(door.CanOperate(enemy, f.Zone)); Assert.False(door.TrySetOpen(enemy, f.Zone, true));
                Assert.True(f.Zone.GetCell(at.x, at.y).BlocksMovement(enemy)); Assert.True(door.TrySetOpen(f.Player, f.Zone, true));
            }
        }
        [Test] public void ContainedEnemyTurnsKeepPlayerDoorRefusalsOutOfThePublicLog()
        {
            using (var f = new Fixture())
            {
                var gate = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationQuarantineGate"); var at = f.Zone.GetEntityPosition(gate);
                Assert.True(f.Zone.MoveEntity(f.Player, at.x - 1, at.y));
                var enemy = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationHalfSet"); var brain = enemy.GetPart<BrainPart>();
                brain.CurrentZone = f.Zone; brain.Rng = new Random(64); brain.SetPersonallyHostile(f.Player, false);
                // Model the already-aggravated threat's ordinary pursuit, then let
                // actual Brain turns and MovementSystem encounter the real barrier.
                brain.PushGoal(new KillGoal(f.Player)); MessageLog.Clear(); int hp = f.Player.GetStatValue("Hitpoints");
                for (int turn = 0; turn < 12; turn++) enemy.FireEventAndRelease(GameEvent.New("TakeTurn"));
                Assert.AreEqual(AIState.Chase, brain.CurrentState); Assert.AreSame(f.Player, brain.Target);
                Assert.Greater(f.Zone.GetEntityPosition(enemy).x, at.x); Assert.True(gate.GetPart<DoorPart>().IsClosed);
                Assert.AreEqual(hp, f.Player.GetStatValue("Hitpoints"));
                Assert.False(MessageLog.GetMessages().Any(m => m.Contains("That door cannot be used right now.")), "Blocked NPC pursuit must not impersonate the player's door action twelve times.");
                MessageLog.Clear(); Assert.False(gate.GetPart<DoorPart>().TrySetOpen(f.Player, f.Zone, true));
                Assert.True(MessageLog.GetMessages().Any(m => m.Contains("That door cannot be used right now.")), "An explicit player attempt still deserves its refusal message.");
            }
        }
        [TestCase(true)][TestCase(false)] public void AbsentActorOrZoneRefusesWithoutClaimingAnyItem(bool missingActor)
        {
            using (var f = new Fixture())
            {
                Assert.False(f.Index.TryCertify(missingActor ? null : f.Player, missingActor ? f.Zone : null));
                Assert.False(f.Index.Certified); Assert.AreSame(f.Index.ParentEntity, f.Index.Counterfoil.GetPart<PhysicsPart>().InInventory);
                Assert.True(f.Index.TryCertify(f.Player, f.Zone));
            }
        }
    }
}
