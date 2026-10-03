using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>One source-guided ordinary expedition, with no generated-owner or inventory setup.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _predatorDiversion;
        const string DiversionZone = "Overworld.12.6.0";
        const string DiversionIntent = "Actual Duelist selection and two original DriedMeat; ordinary world-map travel to the predeclared seed64 hunt, clear-ground one-unit throw from outside hunter sight, native paid NPC approach and two current-owner feeding gestures, exact one-unit consumption, original hunt continuation, native F5/unsaved action/F6 replacement-state conservation. A second actual ration is thrown only after saving to test the spent allowance; F6 restores the retained checkpoint ration.";
        const string DiversionLimits = "One targeted candidate selected from F11 native evidence, not blind or unaided discovery. At most8 safe approach steps in total, including at most2 ordinary withdrawals from a still-active noncombat hunt;16 paid observation waits and2 repeat-probe waits. Read-only landing selection avoids current retrievers that would accept the actual throw. No transfers, grants, clock changes, AI edits, source retries or forced animation. Old F11 coordinates are a source-selection clue, not an assertion about this freshly generated v13 graph. If live geometry, threats or timing prevent the throw or observation, the run fails visibly. Current model submissions and actual animation frames need independent pixel review. No claim that food pacifies a hostile or saves every quarry.";
        static readonly string[] DiversionChecks = { "ordinary_start", "diversion_original_provisions", "diversion_generated_hunt", "diversion_native_single_throw", "diversion_native_approach", "diversion_two_current_feed_hooks", "diversion_actual_interact_pose", "diversion_one_unit_consumed", "diversion_original_hunt_continues", "diversion_saved_checkpoint", "diversion_spent_repeat_refused", "diversion_exact_replacement", "diversion_finish" };
        string _diversionHunterId, _diversionPreyId, _diversionMealId, _diversionRepeatId;
        Entity _diversionObserved;
        Coroutine _diversionFrames;
        int _diversionInteractions;
        bool _diversionInteractPose, _diversionSubscribed;
        string _diversionObserverError;
        readonly HashSet<int> _diversionProgressHooks = new HashSet<int>();

        public void InitializePredatorDiversion(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Isolated launcher required before predator diversion bootstrap.");
            _predatorDiversion = true;
            _oldChannels["ai"] = Diag.IsChannelEnabled("ai"); Diag.SetChannel("ai", true);
            Initialize(context, connectedBuild: "duelist");
        }

        static T DiversionValue<T>(SpreadPredatorPart role, string name) => (T)Field(role, name);
        static string DiversionPhase(SpreadPredatorPart role) => Field(role, "MeatDiversionPhase").ToString();
        Entity DiversionHunter => Owner(_diversionHunterId);
        bool DiversionOutsideThreat(Entity hunter, int x, int y)
        {
            var at = Zone.GetEntityCell(hunter); var brain = hunter?.GetPart<BrainPart>();
            return at != null && brain != null && brain.Target != Player && !brain.IsPersonallyHostileTo(Player)
                && (AIHelpers.ChebyshevDistance(x, y, at.X, at.Y) > brain.SightRadius
                    || !AIHelpers.HasLineOfSight(Zone, at.X, at.Y, x, y));
        }
        bool DiversionEmptyGround(Cell cell)
        {
            // Bait must enter the hunter's sight; player route safety deliberately
            // forbids that. Validate the physical landing without the player guard.
            if (cell == null || !cell.IsVisible || cell.Occupants.Any(e => !DoorPart.IsBareGround(e)
                || e.HasEffect<BurningEffect>() || e.GetPart<ThermalPart>()?.IsAflame == true)) return false;
            var state = Zone.TileState.Get(cell.X, cell.Y);
            return state == null || (state.Heat == 0 && state.Cold == 0 && state.Charge == 0
                && string.IsNullOrEmpty(state.Cloud) && state.Coatings.Count == 0);
        }
        bool DiversionApproachKeepsDistance(Entity hunter, Cell landing, int playerX, int playerY)
        {
            var at = Zone.GetEntityCell(hunter); var brain = hunter.GetPart<BrainPart>();
            var role = hunter.GetPart<SpreadPredatorPart>();
            var path = FindPath.Search(Zone, at.X, at.Y, landing.X, landing.Y, maxNodes: 169, actor: hunter);
            if (!path.Usable || path.Steps.Count < 2 || path.Steps.Count > SpreadPredatorPart.MaximumMeatApproachActions + 1) return false;
            int x = at.X, y = at.Y;
            foreach (var step in path.Steps)
            {
                x += step.dx; y += step.dy;
                if (AIHelpers.ChebyshevDistance(x, y, role.HomeX, role.HomeY) > SpreadPredatorPart.Leash
                    || (AIHelpers.ChebyshevDistance(x, y, playerX, playerY) <= brain.SightRadius
                        && AIHelpers.HasLineOfSight(Zone, x, y, playerX, playerY))) return false;
            }
            return true;
        }
        Entity[] DiversionCurrentRetrievers()
        {
            // Match the real ItemLanded admission. AIRetriever is radius-only;
            // intervening cover does not prevent a fetch notification.
            return Zone.GetReadOnlyEntities().Where(e =>
            {
                var fetch = e.GetPart<AIRetrieverPart>(); var brain = e.GetPart<BrainPart>();
                return e != Player && fetch?.ParentEntity == e && brain?.ParentEntity == e && brain.CurrentZone == Zone
                    && GoFetchGoal.LiveMember(e, Zone) && e.GetPart<InventoryPart>()?.ParentEntity == e
                    && !brain.HasGoal<GoFetchGoal>() && (!fetch.AlliesOnly || FactionManager.IsAllied(e, Player));
            }).ToArray();
        }
        bool DiversionWouldFetch(Entity retriever, Cell landing)
        {
            var at = Zone.GetEntityCell(retriever);
            return at != null && AIHelpers.ChebyshevDistance(at.X, at.Y, landing.X, landing.Y)
                <= retriever.GetPart<AIRetrieverPart>().NoticeRadius;
        }
        Cell DiversionLanding(Entity hunter, Entity meat, int x, int y, bool recordSelection = false)
        {
            if (!DiversionOutsideThreat(hunter, x, y) || !HandlingService.CanThrow(Player, meat, out _)) return null;
            var h = Zone.GetEntityCell(hunter); var role = hunter.GetPart<SpreadPredatorPart>();
            int radius = Math.Min(6, hunter.GetPart<BrainPart>().SightRadius), range = HandlingService.GetThrowRange(Player, meat);
            var choices = new List<Cell>(); var retrievers = DiversionCurrentRetrievers();
            int fetchRefusals = 0; var refusedExamples = new List<object>();
            for (int cy = Math.Max(1, h.Y - radius); cy <= Math.Min(Zone.Height - 2, h.Y + radius); cy++)
            for (int cx = Math.Max(1, h.X - radius); cx <= Math.Min(Zone.Width - 2, h.X + radius); cx++)
            {
                int near = AIHelpers.ChebyshevDistance(cx, cy, h.X, h.Y), player = AIHelpers.ChebyshevDistance(cx, cy, x, y);
                if (near < 3 || near > radius || player < hunter.GetPart<BrainPart>().SightRadius + 2 || player > Math.Min(range, 12)
                    || Math.Abs(cx - x) < Math.Abs(h.X - x) || Math.Abs(cy - y) < Math.Abs(h.Y - y)
                    || AIHelpers.ChebyshevDistance(cx, cy, role.HomeX, role.HomeY) > SpreadPredatorPart.Leash) continue;
                var cell = Zone.GetCell(cx, cy);
                if (!DiversionEmptyGround(cell)
                    || !AIHelpers.HasLineOfSight(Zone, h.X, h.Y, cx, cy)) continue;
                var ray = LineTargeting.TraceFirstImpactToTarget(Zone, Player, x, y, cx, cy, range);
                if (ray.ImpactCell != cell || ray.HitEntity != null || ray.BlockedBySolid
                    || !DiversionApproachKeepsDistance(hunter, cell, x, y)) continue;
                var fetching = retrievers.FirstOrDefault(e => DiversionWouldFetch(e, cell));
                if (fetching != null)
                {
                    fetchRefusals++;
                    if (recordSelection && refusedExamples.Count < 8) refusedExamples.Add(new { landing = new[] { cx, cy }, retriever = fetching.ID,
                        retrieverAt = Zone.GetEntityPosition(fetching), radius = fetching.GetPart<AIRetrieverPart>().NoticeRadius });
                    continue;
                }
                choices.Add(cell);
            }
            // Keep the meal away from the player while minimizing the finite approach.
            var selected = choices.OrderBy(c => AIHelpers.ChebyshevDistance(c.X, c.Y, h.X, h.Y))
                .ThenBy(c => AIHelpers.ChebyshevDistance(c.X, c.Y, x, y)).ThenBy(c => c.Y).ThenBy(c => c.X).FirstOrDefault();
            if (recordSelection) _observations.Add(new { phase = "diversion-read-only-landing-selection", player = new[] { x, y },
                selected = selected == null ? null : new[] { selected.X, selected.Y }, fetchRefusals, refusedExamples,
                retrievers = retrievers.Select(e => new { e.ID, e.BlueprintName, at = Zone.GetEntityPosition(e),
                    radius = e.GetPart<AIRetrieverPart>().NoticeRadius, currentlyVisible = Zone.GetEntityCell(e)?.IsVisible }).ToArray(),
                boundary = "Read-only source-guided selection; real retriever notice uses radius without LOS. No dog command, target clearing or NPC suppression." });
            return selected;
        }
        IEnumerator DiversionThrow(Entity meat, Cell landing, string label)
        {
            var hunter = DiversionHunter; int carried = Packed("DriedMeat");
            Require(landing != null && DiversionOutsideThreat(hunter, At.X, At.Y), "current safe finite meat throw");
            yield return ItemAction(meat, "throw"); Require(State == "ThrowTargeting", "real carried meat throw cursor");
            var pending = Field(_input, "_pendingThrowTarget"); Require(ReferenceEquals(Field(pending, "Item"), meat), "same original ration selected");
            var cursor = (WorldCursorState)Field(_input, "_worldCursorState");
            for (int n = 0; cursor.X != landing.X || cursor.Y != landing.Y; n++)
            { Require(n < 24, "finite real meat aiming"); yield return Tap(Direction(landing.X - cursor.X, landing.Y - cursor.Y)); }
            var ray = LineTargeting.TraceFirstImpactToTarget(Zone, Player, At.X, At.Y, landing.X, landing.Y, HandlingService.GetThrowRange(Player, meat));
            Require(ray.ImpactCell == landing && ray.HitEntity == null && !ray.BlockedBySolid
                && landing.Objects.All(DoorPart.IsBareGround) && DiversionOutsideThreat(hunter, At.X, At.Y)
                && !DiversionCurrentRetrievers().Any(e => DiversionWouldFetch(e, landing)), "actual fresh empty landing outside real fetch admission before native confirmation");
            yield return Paid(Tap(Key.Enter), "local", label);
            Require(Packed("DriedMeat") == carried - 1, "one real carried ration debited by native throw");
        }

        IEnumerator PredatorDiversionJourney()
        {
            Check("diversion_original_provisions", Player.GetProperty(StartingBuildService.PropertyName) == "duelist" && Packed("DriedMeat") == 2);
            yield return Capture("diversion-01-original-duelist-provisions");
            yield return TravelSurface(DiversionZone);
            var hunter = Zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "Furrowstalker" && e.GetPart<SpreadPredatorPart>()?.Configured == true);
            Require(hunter != null, "one actual naturally generated declared hunt; no alternate candidate");
            var role = hunter.GetPart<SpreadPredatorPart>(); _diversionHunterId = hunter.ID; _diversionPreyId = role.PreyID;
            Check("diversion_generated_hunt", Manager.WorldSeed == 64 && Manager.Exploration.Version == 13
                && Manager.Exploration.Find(DiversionZone)?.Family == SpreadExplorationFamily.HuntThroughCover
                && Manager.Exploration.DispositionFor(DiversionZone) == 2 && DiversionValue<bool>(role, "MeatDiversionEnabled")
                && !DiversionValue<bool>(role, "MeatDiversionAttempted") && role.Prey == Owner(_diversionPreyId));
            var arrivalBrain = hunter.GetPart<BrainPart>(); var arrivalCell = Zone.GetEntityCell(hunter);
            _observations.Add(new { phase = "diversion-actual-arrival", hunter = hunter.ID, hunterAt = Zone.GetEntityPosition(hunter), prey = role.PreyID,
                preyAt = Zone.GetEntityPosition(role.Prey), arrival = new[] { At.X, At.Y }, role.HomeX, role.HomeY, huntPhase = role.Phase.ToString(),
                role.PursuitRemaining, role.SearchRemaining, role.HasLastSeen, role.LastSeenX, role.LastSeenY, arrivalBrain.SightRadius,
                target = arrivalBrain.Target?.ID, personallyHostile = arrivalBrain.IsPersonallyHostileTo(Player),
                goals = arrivalBrain.GetGoalsSnapshot().Select(g => new { type = g.GetType().Name, g.Age }).ToArray(),
                distance = SpatialQuery.Distance(Zone, Player, hunter),
                actualLos = AIHelpers.HasLineOfSight(Zone, arrivalCell.X, arrivalCell.Y, At.X, At.Y),
                boundary = "Actual v13 source after the paid arrival turn; old F11 geometry was used only to predeclare this address." });
            yield return Capture("diversion-arrival-before-any-local-action");
            var meat = Player.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "DriedMeat" && Owns(Player, e));
            _guard = hunter; _avoidGuard = true;
            Cell landing = null; int withdrawals = 0;
            for (int steps = 0; steps <= 8; steps++)
            {
                if (!DiversionOutsideThreat(hunter, At.X, At.Y))
                {
                    var brain = hunter.GetPart<BrainPart>(); var h = Zone.GetEntityCell(hunter);
                    Require(steps < 8 && withdrawals < 2
                        && (role.Phase == SpreadHuntPhase.Watching || role.Phase == SpreadHuntPhase.Pursuing || role.Phase == SpreadHuntPhase.Searching)
                        && role.Prey == Owner(_diversionPreyId) && brain.Target != Player && !brain.IsPersonallyHostileTo(Player)
                        && (brain.Target == null || brain.Target == role.Prey) && !brain.HasGoalOtherThan("BoredGoal"),
                        "only a still-active noncombat hunt permits a bounded ordinary withdrawal");
                    var retreat = Steps.Select(d => Zone.GetCell(At.X + d.x, At.Y + d.y))
                        .Where(c => c != null && Safe(Zone, c, ThreatClearance) && Zone.CanPlaceFootprint(Player, c.X, c.Y)
                            && c.Occupants.All(DoorPart.IsBareGround) && DiversionOutsideThreat(hunter, c.X, c.Y))
                        .OrderByDescending(c => AIHelpers.ChebyshevDistance(c.X, c.Y, h.X, h.Y))
                        .ThenByDescending(c => Math.Abs(c.X - h.X) + Math.Abs(c.Y - h.Y)).ThenBy(c => c.Y).ThenBy(c => c.X).FirstOrDefault();
                    Require(retreat != null, "actual safe unoccupied withdrawal cell outside hunter sight");
                    _observations.Add(new { phase = "diversion-ordinary-withdrawal", index = withdrawals + 1, totalApproachStep = steps + 1,
                        from = new[] { At.X, At.Y }, to = new[] { retreat.X, retreat.Y }, hunterAt = Zone.GetEntityPosition(hunter),
                        huntPhase = role.Phase.ToString(), target = brain.Target?.ID, boundary = "One actual paid movement key, not combat suppression or a position grant." });
                    yield return StepTo(retreat.X, retreat.Y); withdrawals++;
                    Require(brain.Target != Player && !brain.IsPersonallyHostileTo(Player) && !brain.HasGoalOtherThan("BoredGoal")
                        && (role.Phase == SpreadHuntPhase.Watching || role.Phase == SpreadHuntPhase.Pursuing || role.Phase == SpreadHuntPhase.Searching),
                        "ordinary withdrawal did not suppress acquired combat or revive an ended hunt");
                    continue;
                }
                landing = DiversionLanding(hunter, meat, At.X, At.Y, recordSelection: true); if (landing != null) break;
                Require(steps < 8, "eight-step ordinary diversion route exhausted without a lawful throw");
                var path = PathTo(c => DiversionLanding(hunter, meat, c.X, c.Y) != null);
                Require(path != null && path.Count > 0, "actual safe outside-sight route to a finite meat throw");
                yield return StepTo(path[0].x, path[0].y);
            }
            string beforeHunt = DiversionHuntHistory(role); var originalPosition = Zone.GetEntityPosition(hunter);
            StartDiversionObservation(hunter);
            try
            {
                yield return DiversionThrow(meat, landing, "diversion-first-original-ration");
                var meal = landing.Objects.SingleOrDefault(e => e.BlueprintName == "DriedMeat" && Units(e) == 1);
                Require(meal != null, "actual thrown singleton remains on its exact clear landing"); _diversionMealId = meal.ID;
                Check("diversion_native_single_throw", Packed("DriedMeat") == 1 && meal.GetPart<PhysicsPart>().InInventory == null
                    && meal.SpatialZone == Zone && Zone.GetEntityCell(meal) == landing && CountGraphId(meal.ID) == 1);
                _observations.Add(new { phase = "diversion-current-ground-meat", visual = CardVisual(meal), hunterVisual = CardVisual(hunter),
                    meatAt = Zone.GetEntityPosition(meal), hunterAt = Zone.GetEntityPosition(hunter),
                    current = JsonConvert.DeserializeObject(DiversionDigest()), target = hunter.GetPart<BrainPart>().Target?.ID,
                    meatTags = meal.Tags, meatProperties = meal.Properties });
                yield return Capture("diversion-02-real-ground-ration-and-hunter");
                bool moved = Zone.GetEntityPosition(hunter) != originalPosition;
                for (int waits = 0; DiversionPhase(role) != "Consumed" && waits < 16; waits++)
                {
                    Require(_diversionObserverError == null, "current-owner native feeding evidence: " + _diversionObserverError);
                    Require(DiversionOutsideThreat(hunter, At.X, At.Y), "threat priority cannot be suppressed by the native observer");
                    Require(DiversionPhase(role) != "Aborted", "actual diversion aborted; no source reset");
                    yield return Paid(Tap(Key.Period), "local", "diversion-paid-wait-" + waits);
                    moved |= Zone.GetEntityPosition(hunter) != originalPosition;
                    _observations.Add(new { phase = "diversion-current-progress", waits, value = JsonConvert.DeserializeObject(DiversionDigest()), originalHunt = DiversionHuntHistory(role),
                        brainTarget = hunter.GetPart<BrainPart>().Target?.ID,
                        targetBlueprint = hunter.GetPart<BrainPart>().Target?.BlueprintName,
                        goals = hunter.GetPart<BrainPart>().GetGoalsSnapshot().Select(g => g.GetType().Name).ToArray(),
                        receipts = Diag.Snapshot(256).Where(e => e.ActorId == hunter.ID && e.Kind.StartsWith("SpreadMeatDiversion", StringComparison.Ordinal)).ToArray() });
                    if (DiversionPhase(role) != "Consumed") Require(DiversionHuntHistory(role) == beforeHunt, "diversion preserves original hunt history without clock refresh");
                    if (DiversionPhase(role) == "Feeding" && DiversionValue<int>(role, "MeatFeedProgress") == 1)
                        yield return Capture("diversion-03-actual-feeding-progress");
                }
                Check("diversion_native_approach", moved && DiversionValue<bool>(role, "MeatDiversionAttempted")
                    && DiversionValue<int>(role, "MeatApproachRemaining") < SpreadPredatorPart.MaximumMeatApproachActions);
                Check("diversion_two_current_feed_hooks", _diversionObserverError == null && _diversionInteractions == 2 && _diversionProgressHooks.SetEquals(new[] { 1, 2 }));
                Check("diversion_actual_interact_pose", _diversionInteractPose);
                Check("diversion_one_unit_consumed", DiversionPhase(role) == "Consumed" && DiversionValue<Entity>(role, "MeatDiversionTarget") == null
                    && DiversionValue<int>(role, "MeatFeedProgress") == 2 && Owner(_diversionMealId) == null && CountGraphId(_diversionMealId) == 0 && Packed("DriedMeat") == 1);
                yield return Capture("diversion-04-consumed-ration");
                int pursuit = role.PursuitRemaining, search = role.SearchRemaining; var history = DiversionHuntHistory(role);
                yield return Paid(Tap(Key.Period), "local", "diversion-original-hunt-resume");
                Check("diversion_original_hunt_continues", role.PreyID == _diversionPreyId && role.PursuitRemaining <= pursuit && role.SearchRemaining <= search
                    && DiversionHuntHistory(role) != history && DiversionPhase(role) == "Consumed"
                    && (role.Phase == SpreadHuntPhase.Pursuing || role.Phase == SpreadHuntPhase.Searching) && role.Prey == Owner(_diversionPreyId));
                yield return PredatorDiversionCheckpoint();
                Check("diversion_finish", Player.GetStatValue("Hitpoints") > 10 && Packed("DriedMeat") == 1 && _localInputs <= 40 && _mapSteps <= 16
                    && !DevMode.Enabled && !DebugInvincibility.IsEnabled(Player) && !Player.HasPart<BitLockerPart>());
                yield return Capture("diversion-07-finished-retained-ration");
            }
            finally { StopDiversionObservation(); _avoidGuard = false; _guard = null; }
        }
        static string DiversionHuntHistory(SpreadPredatorPart role) => string.Join("|", role.Phase, role.PreyID, role.HasLastSeen,
            role.LastSeenX, role.LastSeenY, role.PursuitRemaining, role.SearchRemaining);
        string DiversionDigest()
        {
            var hunter = DiversionHunter; var role = hunter.GetPart<SpreadPredatorPart>();
            return JsonConvert.SerializeObject(new { hunter = hunter.ID, position = Zone.GetEntityPosition(hunter), hunt = DiversionHuntHistory(role),
                enabled = DiversionValue<bool>(role, "MeatDiversionEnabled"), attempted = DiversionValue<bool>(role, "MeatDiversionAttempted"), phase = DiversionPhase(role),
                target = DiversionValue<Entity>(role, "MeatDiversionTarget")?.ID, targetID = DiversionValue<string>(role, "MeatDiversionTargetID"),
                x = DiversionValue<int>(role, "MeatDiversionX"), y = DiversionValue<int>(role, "MeatDiversionY"),
                approach = DiversionValue<int>(role, "MeatApproachRemaining"), progress = DiversionValue<int>(role, "MeatFeedProgress"),
                retained = Packed("DriedMeat"), consumed = CountGraphId(_diversionMealId), prey = Owner(_diversionPreyId)?.ID });
        }
        IEnumerator PredatorDiversionCheckpoint()
        {
            var oldPlayer = Player; var oldZone = Zone; var oldHunter = DiversionHunter;
            string digest = DiversionDigest(), gear = Gear(Player), stats = Stats(Player), file = SaveFile(), old = HashFile(file);
            int tick = Tick, energy = Energy, world = WorldClock.CurrentTick, x = At.X, y = At.Y;
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(file);
            Check("diversion_saved_checkpoint", old != _checkpointHash && MessageLog.GetLast() == "Game saved." && Tick == tick && Energy == energy
                && WorldClock.CurrentTick == world && DiversionDigest() == digest);
            // This last actual ration is an unsaved repeat probe, never a supplied test item.
            var remaining = Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "DriedMeat" && Owns(Player, e));
            var currentRole = oldHunter.GetPart<SpreadPredatorPart>(); var currentBrain = oldHunter.GetPart<BrainPart>();
            Require((currentRole.Phase == SpreadHuntPhase.Pursuing || currentRole.Phase == SpreadHuntPhase.Searching)
                && !currentBrain.HasGoalOtherThan("BoredGoal") && (currentBrain.Target == null || currentBrain.Target == currentRole.Prey),
                "repeat probe begins during authoritative original hunt, not an already ended or preempted role");
            var landing = DiversionLanding(oldHunter, remaining, At.X, At.Y, recordSelection: true);
            Require(landing != null, "current ordinary second-ration clear ray for spent-allowance counter");
            StopDiversionObservation(); yield return DiversionThrow(remaining, landing, "diversion-unsaved-final-ration-probe");
            var probe = landing.Objects.Single(e => e.BlueprintName == "DriedMeat"); _diversionRepeatId = probe.ID;
            for (int i = 0; i < 2; i++)
            { Require(DiversionOutsideThreat(oldHunter, At.X, At.Y), "safe ordinary repeat observation"); yield return Paid(Tap(Key.Period), "local", "diversion-spent-probe-wait-" + i); }
            Check("diversion_spent_repeat_refused", DiversionPhase(oldHunter.GetPart<SpreadPredatorPart>()) == "Consumed" && Owner(probe.ID) == probe
                && Units(probe) == 1 && Packed("DriedMeat") == 0 && HashFile(file) == _checkpointHash && Tick > tick);
            yield return Capture("diversion-05-spent-hunter-ignores-final-ration");
            yield return Reload(oldPlayer);
            Check("diversion_exact_replacement", !ReferenceEquals(Player, oldPlayer) && !ReferenceEquals(Zone, oldZone) && !ReferenceEquals(DiversionHunter, oldHunter)
                && DiversionDigest() == digest && Gear(Player) == gear && Stats(Player) == stats && At.X == x && At.Y == y && Tick == tick && Energy == energy
                && WorldClock.CurrentTick == world && HashFile(file) == _checkpointHash && CountGraphId(_diversionRepeatId) == 0 && Packed("DriedMeat") == 1);
            yield return Capture("diversion-06-exact-native-replacement");
        }
        void StartDiversionObservation(Entity hunter)
        {
            StopDiversionObservation(); _diversionObserved = hunter; EntityVisualHooks.InteractionCallback += DiversionInteraction;
            _diversionSubscribed = true; _diversionFrames = StartCoroutine(DiversionFrames());
        }
        void StopDiversionObservation()
        {
            if (_diversionSubscribed) EntityVisualHooks.InteractionCallback -= DiversionInteraction;
            _diversionSubscribed = false; _diversionObserved = null;
            if (_diversionFrames != null) StopCoroutine(_diversionFrames); _diversionFrames = null;
        }
        void DiversionInteraction(Entity actor, Entity target, Zone zone)
        {
            if (actor != _diversionObserved || zone != Zone) return;
            var role = actor.GetPart<SpreadPredatorPart>(); int progress = DiversionValue<int>(role, "MeatFeedProgress");
            bool valid = DiversionPhase(role) == "Feeding" && DiversionValue<Entity>(role, "MeatDiversionTarget") == target
                && target?.ID == DiversionValue<string>(role, "MeatDiversionTargetID") && zone.GetEntityCell(actor) != null
                && zone.GetEntityCell(target) != null && target.SpatialZone == zone && target.GetPart<PhysicsPart>()?.InInventory == null
                && (progress == 1 || progress == 2) && Units(target) == 1;
            _diversionInteractions++; _diversionProgressHooks.Add(progress);
            _observations.Add(new { phase = "diversion-actual-current-owner-interaction", actor = actor.ID, target = target?.ID, progress, valid, tick = Tick });
            if (!valid) _diversionObserverError = "Feeding Interaction was not emitted with the actual current ground meat and committed progress.";
        }
        IEnumerator DiversionFrames()
        {
            while (_diversionObserved != null)
            {
                yield return new WaitForEndOfFrame();
                try
                {
                    var presenter = _input.ZoneRenderer.SpawnRing3D; var hunter = _diversionObserved;
                    if (_diversionInteractPose || Zone.GetEntityCell(hunter)?.IsVisible != true || presenter == null || !presenter.IsRenderedEntity(hunter)
                        || !presenter.TryGetEntityView(hunter, out var root, out var model)) continue;
                    var animator = root.GetComponentInChildren<Animator>(); if (animator == null) continue;
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (!state.IsName("Interact") || state.normalizedTime < .15f || state.normalizedTime > .85f) continue;
                    Require(presenter.TryGetApprovedStyle(hunter, out var proof) && proof.ModelId == "spread-furrowstalker" && model == proof.ModelId,
                        "actual running original hunter Interact style");
                    _observations.Add(new { phase = "diversion-actual-interact-frame", hunter = hunter.ID, model, state.normalizedTime,
                        boundary = "Observed current Animator and approved native owner style; no sampled clip or visibility grant. Pixels require separate inspection." });
                    string path = Path.Combine(DirectoryPath, "diversion-live-Interact.png"); Directory.CreateDirectory(DirectoryPath);
                    DensityNativeScreenshot.CaptureToFile(path); Require(File.Exists(path) && new FileInfo(path).Length > 0, "actual native feeding screenshot");
                    _screenshots.Add(path); _diversionInteractPose = true; WriteReport();
                }
                catch (Exception error) { _diversionObserverError = error.ToString(); yield break; }
            }
        }
    }
}
