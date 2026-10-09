using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _fairRetreat;
        string _retreatActorId, _retreatDoorId;
        const string FairRetreatIntent = "Controlled native acceptance: original duelist bootstrap followed by one disclosed enclosed lane, open door and 3/15HP factory marlback setup. Real wait acquires Flee; real door closure breaks LOS; real F5/F6 compares north/south hidden player detours from identical active-memory saves; reopening the real door reacquires contact. All actions after setup use native keys and scheduling.";
        const string FairRetreatLimits = "Controlled geometry and NPC health, not an ordinarily generated encounter, organic discovery or a balance/feel test. Setup removes overlapping terrain/owners and clears tile state in one rectangle, adds grass/stone walls/a VillageDoor, and spawns one wounded original enemy. No later placement, health, goal, target, memory, RNG, clock or FOV grants. Hidden test-state observations do not reveal the enemy to gameplay. The independent ordinary water journey covers real content continuity. Pixel/animation quality requires review.";
        static readonly string[] FairRetreatChecks = { "ordinary_start", "retreat_controlled_setup", "retreat_visible_acquisition", "retreat_native_door_hides", "retreat_active_memory_saved", "retreat_hidden_north", "retreat_exact_memory_restored", "retreat_hidden_south_matches", "retreat_native_reacquisition", "retreat_controlled_finish" };

        public void InitializeFairRetreat(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Isolated launcher required before controlled retreat bootstrap.");
            _fairRetreat = true; Initialize(context, connectedBuild: "duelist");
        }
        Entity RetreatActor => Owner(_retreatActorId);
        Entity RetreatDoor => Owner(_retreatDoorId);
        FleeGoal RetreatGoal => RetreatActor?.GetPart<BrainPart>()?.FindGoal<FleeGoal>();
        bool RetreatSeesPlayer() => RetreatActor != null && AIHelpers.TryGetVisibleTargetCell(RetreatActor, Player, Zone,
            RetreatActor.GetPart<BrainPart>().SightRadius, out _);
        bool RetreatMemory(int x, int y, int remaining, bool unseen)
        {
            var actor = RetreatActor; var brain = actor?.GetPart<BrainPart>(); var goal = RetreatGoal;
            return goal != null && goal.ParentBrain == brain && brain.ParentEntity == actor && goal.FleeFrom == Player
                && brain.Target == Player && goal.HasLastSeen && !goal.Abandoned
                && goal.LastSeenX == x && goal.LastSeenY == y && goal.UnseenRemaining == remaining
                && goal.RetreatingUnseen == unseen && !goal.Finished();
        }
        string RetreatState()
        {
            var actor = RetreatActor; var goal = RetreatGoal; var p = Zone.GetEntityPosition(actor);
            return actor.ID + ":" + p.x + "," + p.y + ":hp=" + actor.GetStatValue("Hitpoints")
                + ":age=" + goal.Age + ":max=" + goal.MaxTurns + ":from=" + goal.FleeFrom?.ID
                + ":target=" + actor.GetPart<BrainPart>().Target?.ID + ":seen=" + goal.HasLastSeen
                + ":at=" + goal.LastSeenX + "," + goal.LastSeenY + ":remaining=" + goal.UnseenRemaining
                + ":unseen=" + goal.RetreatingUnseen + ":abandoned=" + goal.Abandoned;
        }
        void RecordRetreat(string phase, string marker = null)
        {
            _observations.Add(new { phase, player = Player.ID, playerAt = Zone.GetEntityPosition(Player),
                actor = RetreatState(), door = RetreatDoor.ID, closed = RetreatDoor.GetPart<DoorPart>().IsClosed,
                seesPlayer = RetreatSeesPlayer(), distance = SpatialQuery.Distance(Zone, RetreatActor, Player),
                tick = Tick, energy = Energy, world = WorldClock.CurrentTick, rows = marker == null ? null : Window(marker),
                bound = "Controlled fixture observer reads hidden state for assertions; native FOV and feedback remain unchanged." });
        }
        bool RetreatOneOpportunity(string marker)
        {
            var rows = Window(marker);
            return rows.Count(r => r.Kind == "Begin" && r.ActorId == _retreatActorId) == 1
                && rows.Count(r => r.Kind == "End" && r.ActorId == _retreatActorId) == 1
                && !rows.Any(r => r.ActorId == _retreatActorId
                    && (r.Kind == "HitRoll" || r.Kind == "MeleeAttackVetoed" || r.Kind == "SpellDamage"));
        }

        IEnumerator FairRetreatJourney()
        {
            int tickBeforeSetup = Tick, energyBeforeSetup = Energy, hp = Player.GetStatValue("Hitpoints");
            string originalGear = Gear(Player), playerId = Player.ID;
            var setup = new FairRetreatDoorScenario();
            setup.Apply(new ScenarioContext(Zone, _input.EntityFactory, Player, _input.TurnManager, 64));
            _retreatActorId = setup.Actor.ID; _retreatDoorId = setup.Door.ID; int x = setup.X, y = setup.Y;
            yield return Settled();
            Check("retreat_controlled_setup", Tick == tickBeforeSetup && Energy == energyBeforeSetup
                && Player.ID == playerId && At.X == x && At.Y == y && Player.GetStatValue("Hitpoints") == hp
                && Gear(Player) == originalGear && RetreatActor.GetStatValue("Hitpoints") == 3
                && Zone.GetEntityPosition(RetreatActor) == (x - 6, y) && RetreatGoal == null
                && !RetreatDoor.GetPart<DoorPart>().IsClosed && RetreatSeesPlayer());
            _observations.Add(new { phase = "controlled-retreat-setup", setup.RemovedOwners, x, y,
                actor = _retreatActorId, door = _retreatDoorId, declaredSetup = FairRetreatLimits });
            yield return Capture("retreat-01-controlled-open-lane");

            // A newly registered native actor starts at zero energy. If player
            // ordering grants the next input first, let one further real wait
            // schedule acquisition instead of priming the actor's energy.
            string marker = null;
            for (int wait = 0; RetreatGoal == null && wait < 2; wait++)
            {
                marker = Mark("retreat-visible-before-wait-" + wait);
                yield return Paid(Tap(Key.Period), "local", "retreat-native-acquisition-wait-" + wait);
            }
            Check("retreat_visible_acquisition", Zone.GetEntityPosition(RetreatActor) == (x - 7, y)
                && RetreatMemory(x, y, 6, false) && RetreatSeesPlayer() && RetreatOneOpportunity(marker));
            RecordRetreat("visible-acquisition", marker);
            marker = Mark("retreat-before-door-close");
            yield return Paid(WorldAction(RetreatDoor, DoorPart.CloseCommand), "local", "retreat-native-close-door");
            Check("retreat_native_door_hides", RetreatDoor.GetPart<DoorPart>().IsClosed
                && Zone.GetEntityPosition(RetreatActor) == (x - 8, y) && !RetreatSeesPlayer()
                && SpatialQuery.Distance(Zone, RetreatActor, Player) <= RetreatActor.GetPart<BrainPart>().SightRadius
                && RetreatMemory(x, y, 5, true) && RetreatOneOpportunity(marker)
                && Window(marker).Count(r => r.Kind == "RetreatLostSight" && r.ActorId == _retreatActorId) == 1);
            RecordRetreat("real-door-lost-sight", marker);
            yield return Capture("retreat-02-native-closed-door");

            string checkpointState = RetreatState(), notes = NoteSignature(), stats = Stats(Player);
            int tick = Tick, energy = Energy, world = WorldClock.CurrentTick, drams = TradeSystem.GetDrams(Player);
            var oldPlayer = Player; var oldActor = RetreatActor; var oldGoal = RetreatGoal;
            yield return Tap(Key.F5); yield return Settled();
            string file = SaveFile(); _checkpointHash = HashFile(file);
            Check("retreat_active_memory_saved", MessageLog.GetLast() == "Game saved."
                && !string.IsNullOrEmpty(_checkpointHash) && RetreatState() == checkpointState
                && Tick == tick && Energy == energy && WorldClock.CurrentTick == world);

            marker = Mark("retreat-before-hidden-north");
            yield return StepTo(x, y - 1);
            Check("retreat_hidden_north", At.X == x && At.Y == y - 1
                && Zone.GetEntityPosition(RetreatActor) == (x - 9, y) && !RetreatSeesPlayer()
                && SpatialQuery.Distance(Zone, RetreatActor, Player) <= RetreatActor.GetPart<BrainPart>().SightRadius
                && RetreatMemory(x, y, 4, true) && RetreatOneOpportunity(marker) && HashFile(file) == _checkpointHash);
            string northState = RetreatState(); RecordRetreat("hidden-north", marker);
            yield return Capture("retreat-03-hidden-north");
            yield return Reload(oldPlayer);
            Check("retreat_exact_memory_restored", Player != oldPlayer && RetreatActor != oldActor && RetreatGoal != oldGoal
                && Player.ID == playerId && At.X == x && At.Y == y && Tick == tick && Energy == energy
                && WorldClock.CurrentTick == world && Stats(Player) == stats && Gear(Player) == originalGear
                && NoteSignature() == notes && TradeSystem.GetDrams(Player) == drams
                && RetreatState() == checkpointState && RetreatMemory(x, y, 5, true)
                && RetreatDoor.GetPart<DoorPart>().IsClosed && !RetreatSeesPlayer() && HashFile(file) == _checkpointHash);

            marker = Mark("retreat-before-hidden-south");
            yield return StepTo(x, y + 1);
            Check("retreat_hidden_south_matches", At.X == x && At.Y == y + 1
                && Zone.GetEntityPosition(RetreatActor) == (x - 9, y) && !RetreatSeesPlayer()
                && SpatialQuery.Distance(Zone, RetreatActor, Player) <= RetreatActor.GetPart<BrainPart>().SightRadius
                && RetreatMemory(x, y, 4, true) && RetreatState() == northState && RetreatOneOpportunity(marker));
            RecordRetreat("hidden-south-same-retreat", marker);
            yield return Capture("retreat-04-hidden-south");

            oldPlayer = Player; oldActor = RetreatActor;
            yield return Reload(oldPlayer);
            Require(Player != oldPlayer && RetreatActor != oldActor && At.X == x && At.Y == y
                && RetreatState() == checkpointState && RetreatMemory(x, y, 5, true)
                && Tick == tick && Energy == energy && WorldClock.CurrentTick == world && HashFile(file) == _checkpointHash,
                "second real F6 restores the same active-memory branch baseline");
            marker = Mark("retreat-before-native-reopen");
            yield return Paid(WorldAction(RetreatDoor, DoorPart.OpenCommand), "local", "retreat-native-open-door");
            Check("retreat_native_reacquisition", !RetreatDoor.GetPart<DoorPart>().IsClosed && RetreatSeesPlayer()
                && Zone.GetEntityPosition(RetreatActor) == (x - 9, y) && RetreatMemory(x, y, 6, false)
                && RetreatOneOpportunity(marker)
                && Window(marker).Count(r => r.Kind == "RetreatReacquired" && r.ActorId == _retreatActorId) == 1);
            RecordRetreat("native-door-reacquisition", marker);
            yield return Capture("retreat-05-reacquired-visible-contact");
            Check("retreat_controlled_finish", State == "Normal" && Player.GetStatValue("Hitpoints") == hp
                && RetreatActor.GetStatValue("Hitpoints") == 3 && Gear(Player) == originalGear
                && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player) && !Player.HasPart<BitLockerPart>());
        }
    }
}
