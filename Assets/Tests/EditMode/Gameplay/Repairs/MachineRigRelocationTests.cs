using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Rendering;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineRigRelocationTests : FiftyWorldFixture
    {
        static Zone Spatial(Entity e) => (Zone)typeof(Entity).GetField("SpatialZone", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(e);
        void Observe(Action callback)
        {
            var state = new ThrowingStatePart(); Actor.AddPart(state);
            typeof(ThrowingStatePart).GetField("RelocationCommitted", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, callback);
        }
        Zone destination;
        ZoneManager manager;
        Entity rig;
        int dx, dy, ax, ay, bx, by;

        void Prepare(TransitionDirection direction = TransitionDirection.North, bool repaired = true)
        {
            Zone.RemoveEntity(Actor);
            Zone = new Zone("Overworld.3.7.0");
            dx = direction == TransitionDirection.East ? 1 : direction == TransitionDirection.West ? -1 : 0;
            dy = direction == TransitionDirection.South ? 1 : direction == TransitionDirection.North ? -1 : 0;
            ax = dx == 1 ? 79 : dx == -1 ? 0 : 40;
            ay = dy == 1 ? 24 : dy == -1 ? 0 : 12;
            var arrival = ZoneTransitionSystem.GetArrivalPosition(direction, ax, ay);
            bx = arrival.x; by = arrival.y;
            destination = new Zone(WorldMap.GetAdjacentZoneID(Zone.ZoneID, dx, dy));
            Assert.True(Zone.AddEntity(Actor, ax, ay));
            Actor.GetStat("Strength").BaseValue = 18;
            if (!Actor.HasPart<StatusEffectsPart>()) Actor.AddPart(new StatusEffectsPart());
            foreach (var xy in new[] { (ax, ay), (ax - dx, ay - dy) })
                Assert.True(Zone.AddEntity(Factory.CreateEntity("Grass"), xy.Item1, xy.Item2));
            foreach (var xy in new[] { (bx, by), (bx + dx, by + dy) })
                Assert.True(destination.AddEntity(Factory.CreateEntity("Grass"), xy.Item1, xy.Item2));
            rig = Factory.CreateEntity("AbandonedFieldWorkRig");
            rig.GetPart<RepairablePart>().Repaired = repaired;
            Assert.True(Zone.AddEntity(rig, ax - dx, ay - dy));
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(Actor, rig, Zone));
            manager = new ZoneManager(Factory, 144);
            manager.CachedZones[Zone.ZoneID] = Zone;
            manager.CachedZones[destination.ZoneID] = destination;
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
        }

        ZoneTransitionResult Cross(TransitionDirection d = TransitionDirection.North)
            => ZoneTransitionSystem.TransitionPlayer(Actor, Zone, d, ax, ay, manager, null);

        [TestCase(TransitionDirection.North)] [TestCase(TransitionDirection.South)]
        [TestCase(TransitionDirection.East)] [TestCase(TransitionDirection.West)]
        public void BothOwnersCrossExactlyAndKeepTheOriginalGrip(TransitionDirection d)
        {
            Prepare(d); var grip = Actor.GetPart<DragPart>(); var inverse = rig.GetPart<DraggedPart>();
            int penalty = Actor.GetStat("Speed").Penalty;
            var result = Cross(d);
            Assert.True(result.Success, result.ErrorReason);
            Assert.AreEqual((bx + dx, by + dy), destination.GetEntityPosition(Actor));
            Assert.AreEqual((bx, by), destination.GetEntityPosition(rig));
            Assert.Null(Zone.GetEntityCell(Actor)); Assert.Null(Zone.GetEntityCell(rig));
            Assert.AreSame(destination, Spatial(Actor)); Assert.AreSame(destination, Spatial(rig));
            Assert.AreSame(grip, Actor.GetPart<DragPart>()); Assert.AreSame(inverse, rig.GetPart<DraggedPart>());
            Assert.AreEqual(penalty, Actor.GetStat("Speed").Penalty);
            Assert.True(rig.GetPart<RepairablePart>().Repaired);
            Assert.AreEqual(1, destination.GetReadOnlyEntities().Count(e => e == rig));
            Assert.False(Zone.GetEntitiesWithTag("RecoverableWorkRig").Contains(rig));
            Assert.True(destination.GetEntitiesWithTag("RecoverableWorkRig").Contains(rig));
        }

        [TestCase(TransitionDirection.East)] [TestCase(TransitionDirection.North)]
        public void ACommittedCrossingStampsTheRigRelocatedOnceAndKeepsItWorkable(TransitionDirection d)
        {
            Prepare(d); Assert.False(rig.HasPart<InstallationMarkerPart>(), "an uncrossed rig carries no marker");
            Assert.True(Cross(d).Success); var marker = rig.GetPart<InstallationMarkerPart>();
            Assert.NotNull(marker); Assert.AreEqual("Relocated", marker.Origin); Assert.AreEqual("relocated:" + rig.ID, marker.InstallationId); Assert.True(InstallationMarkerPart.Retains(rig));
            Assert.AreEqual(1, rig.Parts.Count(p => p is InstallationMarkerPart)); Assert.True((bool)typeof(ForgePart).Assembly.GetType("CavesOfOoo.Core.FieldWorkRigRules").GetMethod("Payload", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { rig, false }), "the validator tolerates the extra Part");
        }

        [Test] public void ARefusedCrossingStampsNothing_Counter()
        {
            Prepare(); destination.TileState.AddHeat(bx, by, 1);
            AssertRefusedUnchanged(); Assert.False(rig.HasPart<InstallationMarkerPart>());
        }

        [TestCase("source-load")] [TestCase("source-player")]
        [TestCase("receiving-edge")] [TestCase("receiving-inward")]
        public void NonemptyTileStateAtAnySweptCellRefusesWithoutMovingOrReleasing(string cell)
        {
            Prepare();
            if (cell == "source-load") Zone.TileState.AddHeat(ax - dx, ay - dy, 1);
            if (cell == "source-player") Zone.TileState.AddHeat(ax, ay, 1);
            if (cell == "receiving-edge") destination.TileState.AddHeat(bx, by, 1);
            if (cell == "receiving-inward") destination.TileState.AddHeat(bx + dx, by + dy, 1);
            AssertRefusedUnchanged();
        }

        [TestCase("blocker")] [TestCase("excluded")] [TestCase("extra-part")]
        [TestCase("extra-nonsolid")] [TestCase("not-terrain")] [TestCase("missing-ground")]
        public void UnsupportedReceivingGroundNeverSpiralsOrDisclosesAnOwner(string kind)
        {
            Prepare(); var ground = destination.GetCell(bx, by).Objects.Single();
            switch (kind)
            {
                case "blocker": ground.GetPart<PhysicsPart>().Solid = true; break;
                case "excluded": ground.SetTag("ExcludeZoneArrival"); break;
                case "extra-part": ground.AddPart(new RigMoveProbe()); break;
                case "extra-nonsolid": destination.AddEntity(Factory.CreateEntity("FireClay"), bx, by); break;
                case "not-terrain": ground.Tags.Remove("Terrain"); break;
                case "missing-ground": destination.RemoveEntity(ground); break;
            }
            AssertRefusedUnchanged();
        }

        [TestCase("dead")] [TestCase("rooted")] [TestCase("weak")] [TestCase("wrong-inverse")]
        [TestCase("extra-grip")] [TestCase("wrong-offset")] [TestCase("bad-rig")]
        public void ActorAndGripCountercasesCannotFallThroughToOrdinaryTransfer(string kind)
        {
            Prepare();
            switch (kind)
            {
                case "dead": Actor.GetStat("Hitpoints").BaseValue = 0; break;
                case "rooted": Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new RootedEffect(4), null, Zone); break;
                case "weak": Actor.GetStat("Strength").BaseValue = 1; break;
                case "wrong-inverse": rig.GetPart<DraggedPart>().Dragger = new Entity(); break;
                case "extra-grip": Actor.AddPart(new DragPart { Dragged = rig }); break;
                case "wrong-offset": Actor.GetPart<DragPart>().LoadGripX = 1; break;
                case "bad-rig": rig.GetPart<HandlingPart>().Weight = 1; break;
            }
            AssertRefusedUnchanged();
        }

        void AssertRefusedUnchanged()
        {
            var a = Zone.GetEntityCell(Actor); var r = Zone.GetEntityCell(rig);
            var grip = Actor.GetPart<DragPart>(); int penalty = Actor.GetStat("Speed").Penalty;
            var result = Cross();
            Assert.False(result.Success, "Unsupported pair must refuse instead of dropping the rig and moving the player");
            Assert.AreSame(a, Zone.GetEntityCell(Actor)); Assert.AreSame(r, Zone.GetEntityCell(rig));
            Assert.AreSame(grip, Actor.GetPart<DragPart>()); Assert.AreEqual(penalty, Actor.GetStat("Speed").Penalty);
            Assert.False(destination.GetReadOnlyEntities().Contains(Actor));
            Assert.False(destination.GetReadOnlyEntities().Contains(rig));
        }

        [Test] public void BeforeMoveMutationMustBeRevalidatedBeforeEitherOwnerMoves()
        {
            Prepare(); Actor.AddPart(new RigMoveProbe { Before = () => destination.TileState.AddHeat(bx, by, 1) });
            AssertRefusedUnchanged();
        }
        [TestCase(false)] [TestCase(true)] public void DestinationIdentityCannotBeRedirected(bool inCallback)
        {
            Prepare(); Action redirect = () => { destination.ZoneID = "Overworld.12.12.1"; manager.CachedZones[destination.ZoneID] = destination; };
            if (inCallback) Actor.AddPart(new RigMoveProbe { Before = redirect }); else redirect();
            AssertRefusedUnchanged();
        }
        [Test] public void ThrowingRenderObserverCannotHideCommittedTransfer()
        {
            Prepare(); var old = ZoneRenderHooks.CellDirtyCallback; int calls = 0;
            try
            {
                ZoneRenderHooks.CellDirtyCallback = (x,y,reason) => { if (++calls == 1) throw new InvalidOperationException("rig render observer"); };
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                    new System.Text.RegularExpressions.Regex("Rig.*dirty observer"));
                Assert.True(Cross().Success); Assert.AreSame(destination, Spatial(Actor)); Assert.AreSame(destination, Spatial(rig));
                Assert.AreEqual(4, calls);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = old; }
        }
        [Test] public void RigReaderExplainsCrossingEffortBeforeItIsAttempted()
        {
            Prepare(); var description = rig.GetPart<ExaminablePart>().Description;
            StringAssert.Contains("two hauling rounds", description); StringAssert.Contains("plain", description);
            StringAssert.Contains("interrupted", description);
        }
        [TestCase(true)] [TestCase(false)] public void SavedRigInstructionsAreCurrentWithoutRewritingSavedText(bool canonical)
        {
            Prepare(); var target = canonical ? rig : Factory.CreateEntity("TinkersForge");
            var reader = target.GetPart<ExaminablePart>();
            const string oldText = "It can be hauled locally; release it before working."; reader.Text = oldText;
            if (canonical) StringAssert.Contains("two hauling rounds", reader.GetDescriptionText());
            else Assert.AreEqual(oldText, reader.GetDescriptionText());
            Assert.AreEqual(oldText, reader.Text);
        }
        [Test] public void FollowerObserverFailureCannotHideTheCommittedRigPair()
        {
            Prepare(); var brain = Actor.GetPart<BrainPart>();
            if (brain == null) { brain = new BrainPart(); Actor.AddPart(brain); }
            var follower = Factory.CreateEntity("Player"); var followerBrain = follower.GetPart<BrainPart>();
            if (followerBrain == null) { followerBrain = new BrainPart(); follower.AddPart(followerBrain); }
            followerBrain.CurrentZone = Zone;
            Assert.True(Zone.AddEntity(follower, ax - 2, ay + 1)); brain.PartyMembers.Add(follower);
            var state = new ThrowingStatePart(); follower.AddPart(state);
            typeof(ThrowingStatePart).GetField("RelocationCommitted", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(state, (Action)(() => { if (Spatial(follower) == destination) throw new InvalidOperationException("follower arrival observer"); }));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("Rig.*Companion"));
            Assert.True(Cross().Success); Assert.AreSame(destination, Spatial(Actor)); Assert.AreSame(destination, Spatial(rig));
            Assert.AreSame(destination, Spatial(follower));
        }
        [Test] public void RelocationObserverSeesBothOwnersInTheDestination()
        {
            Prepare(); bool observed = false;
            Observe(() => {
                observed = true; Assert.AreSame(destination, Spatial(rig));
                Assert.NotNull(destination.GetEntityCell(rig)); Assert.Null(Zone.GetEntityCell(rig));
            });
            Assert.True(Cross().Success); Assert.True(observed);
        }
        [Test] public void RemovalAfterCoherentArrivalReleasesOnlyTheOwnedPenalty()
        {
            Prepare(); Actor.GetStat("Speed").Penalty += 7;
            Observe(() => destination.RemoveEntity(rig));
            Assert.True(Cross().Success); Assert.Null(Actor.GetPart<DragPart>());
            Assert.Null(Spatial(rig)); Assert.AreEqual(7, Actor.GetStat("Speed").Penalty);
        }
        [Test] public void FaultedRigCanRelocateWithoutBecomingRepaired()
        {
            Prepare(repaired: false); Assert.True(Cross().Success);
            Assert.AreSame(destination, Spatial(rig)); Assert.False(rig.GetPart<RepairablePart>().Repaired);
        }
        [TestCase("source")] [TestCase("other")] [TestCase("removed")]
        public void CommittedResultFollowsActualActorPlacementAfterObservers(string outcome)
        {
            Prepare(); var other = new Zone("Overworld.3.5.0"); manager.CachedZones[other.ZoneID] = other;
            bool once = false;
            Observe(() => {
                if (once) return; once = true;
                if (outcome == "removed") destination.RemoveEntity(Actor);
                else destination.TryTransferEntityTo(Actor, outcome == "source" ? Zone : other, 50, 4);
            });
            var result = Cross(); Assert.True(result.Success);
            Assert.AreSame(outcome == "removed" ? null : outcome == "source" ? Zone : other, result.NewZone);
            Assert.AreSame(destination, Spatial(rig)); Assert.Null(Actor.GetPart<DragPart>());
        }
        [Test] public void ALoadRemovedByFirstObserverDoesNotReceiveAGhostPlacementNotification()
        {
            Prepare(); int calls = 0; var state = new ThrowingStatePart(); rig.AddPart(state);
            typeof(ThrowingStatePart).GetField("RelocationCommitted", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(state, (Action)(() => calls++));
            Observe(() => destination.RemoveEntity(rig));
            Assert.True(Cross().Success); Assert.AreEqual(1, calls); Assert.Null(Spatial(rig));
        }
        [Test] public void FollowersUseTheActualArrivalAfterAnObserverMovesTheActor()
        {
            Prepare(); var leaderBrain=Actor.GetPart<BrainPart>();
            if(leaderBrain==null){leaderBrain=new BrainPart();Actor.AddPart(leaderBrain);}
            var follower=new Entity(); follower.AddPart(new PhysicsPart()); follower.AddPart(new BrainPart{CurrentZone=Zone});
            Assert.True(Zone.AddEntity(follower,ax-2,ay+1)); leaderBrain.PartyMembers.Add(follower);
            bool once=false; Observe(()=>{if(once)return;once=true;destination.MoveEntity(Actor,50,4);});
            Assert.True(Cross().Success); var at=destination.GetEntityCell(follower);
            Assert.NotNull(at); Assert.LessOrEqual(Math.Max(Math.Abs(at.X-50),Math.Abs(at.Y-4)),1);
            Assert.Null(Actor.GetPart<DragPart>(),"A displaced actor cannot retain a stretched original grip");
            Assert.AreSame(destination,Spatial(rig));
        }
        [TestCase(false)] [TestCase(true)] public void FullSaveGraphKeepsOneRigAndExactSymmetricGrip(bool reverseZoneOrder)
        {
            using (var isolated = new HotbarSaveFixture(false, false))
            {
                Prepare(); Assert.True(Cross().Success);
                var grip = Actor.GetPart<DragPart>(); int penalty = Actor.GetStat("Speed").Penalty;
                var saveManager = new OverworldZoneManager(null,144); var zones = new Dictionary<string,Zone>();
                if (reverseZoneOrder) { zones.Add(destination.ZoneID,destination); zones.Add(Zone.ZoneID,Zone); }
                else { zones.Add(Zone.ZoneID,Zone); zones.Add(destination.ZoneID,destination); }
                saveManager.ReplaceLoadedState(zones,destination.ZoneID,new Dictionary<string,List<ZoneConnection>>());
                Clock.RestoreSavedState(4529,true,Actor,new List<TurnManager.SavedTurnEntry> {
                    new TurnManager.SavedTurnEntry { Entity=Actor, Energy=1000 } });
                var state = GameSessionState.Capture("rig-relocation","native roundtrip",saveManager,Clock,Actor);
                GameSessionState loaded;
                using (var stream = new System.IO.MemoryStream())
                { state.Save(new SaveWriter(stream)); stream.Position=0; loaded=GameSessionState.Load(new SaveReader(stream,Factory)); }
                Assert.AreEqual(2,loaded.ZoneManager.CachedZones.Count); Assert.AreEqual(destination.ZoneID,loaded.ZoneManager.ActiveZone.ZoneID);
                var placements = new List<Entity>();
                foreach (var z in loaded.ZoneManager.CachedZones.Values)
                    for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++) for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++) placements.AddRange(z.GetCell(x,y).Objects);
                Assert.AreEqual(1,placements.Count(e=>e.ID==Actor.ID)); Assert.AreEqual(1,placements.Count(e=>e.ID==rig.ID));
                var restored = placements.Single(e=>e.ID==rig.ID); var restoredGrip=loaded.Player.GetPart<DragPart>();
                Assert.AreSame(restored,restoredGrip.Dragged); Assert.AreSame(loaded.Player,restored.GetPart<DraggedPart>().Dragger);
                Assert.AreEqual(1,loaded.Player.Parts.Count(p=>p is DragPart)); Assert.AreEqual(1,restored.Parts.Count(p=>p is DraggedPart));
                Assert.AreEqual((bx,by),loaded.ZoneManager.ActiveZone.GetEntityPosition(restored));
                Assert.AreEqual((bx+dx,by+dy),loaded.ZoneManager.ActiveZone.GetEntityPosition(loaded.Player));
                Assert.AreEqual(grip.SpeedPenalty,restoredGrip.SpeedPenalty); Assert.AreEqual(grip.AppliedPenalty,restoredGrip.AppliedPenalty);
                Assert.AreEqual(grip.LoadGripX,restoredGrip.LoadGripX); Assert.AreEqual(grip.LoadGripY,restoredGrip.LoadGripY);
                Assert.AreEqual(grip.HaulerGripX,restoredGrip.HaulerGripX); Assert.AreEqual(grip.HaulerGripY,restoredGrip.HaulerGripY);
                Assert.AreEqual(penalty,loaded.Player.GetStat("Speed").Penalty); Assert.True(restored.GetPart<RepairablePart>().Repaired);
                Assert.AreSame(loaded.Player,loaded.TurnManager.CurrentActor);
            }
        }
        [Test] public void UnladenOrdinaryCrossingKeepsExistingEdgeArrival()
        {
            Prepare(); DragSystem.Release(Actor); Assert.True(Cross().Success);
            Assert.AreEqual((bx, by), destination.GetEntityPosition(Actor)); Assert.AreSame(Zone, Spatial(rig));
        }
        [Test] public void OtherCargoKeepsTheExistingLeaveBehindBehavior()
        {
            Prepare(); rig.BlueprintName="MillStone"; rig.Tags.Remove("RecoverableWorkRig");
            Assert.True(Cross().Success); Assert.AreEqual((bx,by),destination.GetEntityPosition(Actor));
            Assert.AreSame(Zone,Spatial(rig)); Assert.Null(Actor.GetPart<DragPart>());
        }
        [TestCase("coating")] [TestCase("residue")] [TestCase("cold")] [TestCase("charge")] [TestCase("cloud")]
        public void EveryTileChannelRefusesThePlainGroundManeuver(string channel)
        {
            Prepare(); destination.TileState.AddHeat(bx,by,1); var state=destination.TileState.Get(bx,by); state.Heat=0;
            if(channel=="coating")state.Coatings.Add(new ZoneTileState.Layer{Id="water",Turns=5});
            if(channel=="residue")state.Residues.Add(new ZoneTileState.Layer{Id="salt",Turns=5});
            if(channel=="cold")state.Cold=1; if(channel=="charge")state.Charge=1;
            if(channel=="cloud"){state.Cloud="smoke";state.CloudTurns=5;}
            AssertRefusedUnchanged();
        }

        static void Step(InputHandler input, int x = 0, int y = -1)
            => typeof(InputHandler).GetMethod("ExecuteMovementStep", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(input, new object[] { x, y });
        void InputCase(Action<InputHandler, RigPaymentProbe, NewGameSaveFixture> scenario)
        {
            Prepare();
            using (var saves = new NewGameSaveFixture())
            {
                Clock.AddEntity(Actor); Clock.ProcessUntilPlayerTurn();
                var go = new UnityEngine.GameObject("Native recovered rig crossing");
                try
                {
                    var input = go.AddComponent<InputHandler>(); input.PlayerEntity = Actor; input.CurrentZone = Zone;
                    input.ZoneManager = manager; input.WorldMap = new WorldMap(144); input.TurnManager = Clock;
                    var probe = new RigPaymentProbe(); Actor.AddPart(probe);
                    scenario(input, probe, saves);
                }
                finally { UnityEngine.Object.DestroyImmediate(go); SaveGameService.FlushStableRequests(); SaveGameService.FlushPendingSaves(); }
            }
        }
        [Test] public void InputPaysTwoSourceRoundsBeforeTransferAndCapturesOnlyAfterSettlement()
        {
            InputCase((input, probe, saves) => {
                int tick = Clock.TickCount, energy = Clock.GetEnergy(Actor), speed = Clock.GetSpeed(Actor);
                var captures = new List<int>(); var held = new List<bool>();
                probe.After = n => { SaveGameService.RequestQuickSave(); captures.Add(saves.Captures); held.Add(ActionResolutionGuard.IsUnstable); };
                Step(input);
                Assert.AreEqual(2, probe.Count); CollectionAssert.AreEqual(new[] { Zone, Zone }, probe.Zones);
                Assert.AreEqual((int)Math.Ceiling((3.0 * TurnManager.ActionThreshold - energy) / speed), Clock.TickCount - tick);
                CollectionAssert.AreEqual(new[] { 0, 0 }, captures); Assert.True(held.All(x => x));
                Assert.AreEqual(1, saves.Captures); Assert.False(ActionResolutionGuard.IsUnstable);
                Assert.AreSame(destination, input.CurrentZone); Assert.AreSame(destination, Spatial(rig));
            });
        }
        [TestCase(false)] [TestCase(true)]
        public void InputRejectsObstructionOrRawDiagonalWithoutPayment(bool diagonal)
        {
            InputCase((input, probe, saves) => {
                if (!diagonal) destination.TileState.AddHeat(bx, by, 1);
                int tick = Clock.TickCount; Step(input, diagonal ? 1 : 0);
                Assert.AreEqual(0, probe.Count); Assert.AreEqual(tick, Clock.TickCount);
                Assert.AreSame(Zone, input.CurrentZone); Assert.AreSame(Zone, Spatial(rig));
                Assert.AreEqual(0, saves.Captures);
            });
        }
        [TestCase("rooted", 1)] [TestCase("rooted", 2)] [TestCase("death", 1)]
        [TestCase("release", 1)] [TestCase("ground", 1)] [TestCase("ground", 2)]
        public void InterruptedPreparationPaysItsEffortButNeverTransfers(string interruption, int round)
        {
            InputCase((input, probe, saves) => {
                probe.After = n => {
                    if (n != round) return;
                    if (interruption == "rooted") Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new RootedEffect(4), null, Zone);
                    if (interruption == "death") { Actor.GetStat("Hitpoints").BaseValue = 0; Actor.FireEventAndRelease(GameEvent.New("Died")); }
                    if (interruption == "release") DragSystem.Release(Actor);
                    if (interruption == "ground") destination.TileState.AddHeat(bx, by, 1);
                };
                Step(input); Assert.AreEqual(round, probe.Count);
                Assert.AreSame(Zone, input.CurrentZone); Assert.AreSame(Zone, Spatial(Actor)); Assert.AreSame(Zone, Spatial(rig));
                Assert.False(ActionResolutionGuard.IsUnstable); Assert.AreEqual(0, saves.Captures);
            });
        }
        [Test] public void InputCallbackCannotStartAnotherStepInsidePreparation()
        {
            InputCase((input, probe, saves) => {
                probe.After = n => { if (n == 1) Step(input); };
                Step(input); Assert.AreEqual(2, probe.Count);
                Assert.AreEqual((bx + dx, by + dy), destination.GetEntityPosition(Actor));
                Assert.AreEqual((bx, by), destination.GetEntityPosition(rig));
            });
        }
        [TestCase(1)] [TestCase(2)] public void TurnObserverFailureAbortsUncommittedCrossing(int round)
        {
            InputCase((input, probe, saves) => {
                probe.After = n => { if (n == round) throw new InvalidOperationException("rig effort interruption"); };
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                    new System.Text.RegularExpressions.Regex("Exception during turn processing"));
                Step(input); Assert.AreEqual(round, probe.Count);
                Assert.AreSame(Zone, Spatial(Actor)); Assert.AreSame(Zone, Spatial(rig)); Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
        [TestCase("turns",1)] [TestCase("turns",2)] [TestCase("turns",3)]
        [TestCase("actor",2)] [TestCase("manager",2)]
        public void PermissionCallbackCannotRedirectInputCustody(string changed, int permission)
        {
            InputCase((input, probe, saves) => {
                int checks = 0; var foreign = Factory.CreateEntity("Player"); var foreignProbe = new RigPaymentProbe(); foreign.AddPart(foreignProbe);
                var foreignTurns = new TurnManager(); foreignTurns.AddEntity(foreign); foreignTurns.ProcessUntilPlayerTurn();
                Actor.AddPart(new RigMoveProbe { Before = () => {
                    if (++checks != permission) return;
                    if (changed == "turns") input.TurnManager = foreignTurns;
                    if (changed == "actor") input.PlayerEntity = foreign;
                    if (changed == "manager") input.ZoneManager = new ZoneManager(Factory, 500);
                }});
                Step(input); Assert.AreEqual(permission - 1, probe.Count);
                Assert.AreEqual(0, foreignProbe.Count); Assert.AreSame(Zone, Spatial(Actor)); Assert.AreSame(Zone, Spatial(rig));
            });
        }
        [Test] public void BlockedActorAtRigBorderStillPaysAnOrdinaryThawWait()
        {
            InputCase((input, probe, saves) => {
                Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new StunnedEffect(4), null, Zone);
                int tick = Clock.TickCount; Step(input);
                Assert.Greater(probe.Count,0); Assert.Greater(Clock.TickCount,tick);
                Assert.AreSame(Zone, Spatial(Actor)); Assert.AreSame(Zone, Spatial(rig));
            });
        }
        [TestCase("source")] [TestCase("removed")] [TestCase("context")]
        public void InputDoesNotReactivateAStaleDestinationAfterCommittedCallbacks(string consequence)
        {
            InputCase((input,probe,saves)=>{
                bool once=false; var foreign = new ZoneManager(Factory,500);
                Observe(()=>{
                    if(once)return;once=true;
                    if(consequence=="source") destination.TryTransferEntityTo(Actor,Zone,50,4);
                    if(consequence=="removed") destination.RemoveEntity(Actor);
                    if(consequence=="context") input.ZoneManager=foreign;
                });
                Step(input); Assert.AreEqual(2,probe.Count); Assert.AreSame(Zone,input.CurrentZone);
                Assert.AreEqual(0,saves.Captures); Assert.Null(foreign.ActiveZone);
                Assert.AreSame(destination,Spatial(rig)); Assert.False(ActionResolutionGuard.IsUnstable);
            });
        }
    }
    public sealed class RigMoveProbe : Part
    {
        public Action Before;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeMove") Before?.Invoke(); return true; }
    }
    public sealed class RigPaymentProbe : Part
    {
        public int Count;
        public Action<int> After;
        public readonly List<Zone> Zones = new List<Zone>();
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn") { Count++; Zones.Add(e.GetParameter<Zone>("Zone")); After?.Invoke(Count); }
            return true;
        }
    }
}
