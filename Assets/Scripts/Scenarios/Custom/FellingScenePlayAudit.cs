using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Ordinary bootstrap, traversal, action menus and save/load exercised
    /// through queued native keys. Reflection observes menus only; no teleport,
    /// direct clear, direct traversal, fog reveal or direct save/load calls.</summary>
    public sealed class FellingScenePlayAudit : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        public bool PreviewOnly { get; private set; }
        public string RunId { get; private set; }
        public IReadOnlyList<string> Audit => _audit;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly (int x, int y)[] Directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _removed = new List<string>();
        private InputHandler _input;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _initialized, _cleaned;
        private string _root, _gameId, _fatal;
        private int _nativeSteps, _clearActions, _clearTicks, _faunaExamined, _dressingActions, _faunaWaits;
        private System.Diagnostics.Stopwatch _elapsed;
        private FellingSceneDefinition _definition;
        private const int ProfileCapacity = 4096;
        private readonly string[] _profileNames = { "COO.ZoneRenderer.LateUpdate", "Main Thread", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders;
        private double _profileStarted, _profileSeconds;
        private Metric[] _profileMetrics = Array.Empty<Metric>();
        private string ReportDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/FellingScene"));

        public void Initialize(bool previewOnly = false)
        {
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride), "Private save root must be owned before bootstrap.");
            _root = SaveGameService.SaveRootOverride;
            PreviewOnly = previewOnly;
            RunId = Guid.NewGuid().ToString("N");
            _elapsed = System.Diagnostics.Stopwatch.StartNew();
            _oldSettings = InputSystem.settings;
            _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _initialized = true;
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null)
                {
                    _fatal = failure.ToString();
                    Check("native_audit_precondition: " + failure.Message, false);
                    Debug.LogError("[FellingScenePlayAudit] " + failure);
                    break;
                }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input = FindFirstObjectByType<InputHandler>();
            Require(_input != null, "Live ordinary bootstrap input exists.");
            var boot = (BootMenuController)typeof(InputHandler).GetField("_bootMenuController", Private).GetValue(_input);
            if (boot.IsActive) yield return Tap(Key.N);
            Require(!boot.IsActive && State() == "Normal", "New game enters ordinary input state.");
            Check("ordinary_start_is_not_felling", _input.CurrentZone.ZoneID != FellingSiteBuilder.ZoneID);
            Check("owned_root_held_through_boot", SaveGameService.SaveRootOverride == _root);

            yield return Tap(Key.LeftShift, Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID), "Native < enters world map.");
            var target = WorldMap.WorldCellToZoneCell(FellingSiteBuilder.WorldX, FellingSiteBuilder.WorldY);
            var start = Position();
            Check("world_map_route_seven_west_five_north", start.x - target.Item1 == 7 && start.y - target.Item2 == 5);
            while (Position().x > target.Item1) yield return Step(Key.A, (-1, 0));
            while (Position().x < target.Item1) yield return Step(Key.D, (1, 0));
            while (Position().y > target.Item2) yield return Step(Key.W, (0, -1));
            while (Position().y < target.Item2) yield return Step(Key.S, (0, 1));
            Check("native_world_map_reaches_felling_poi", Position() == (target.Item1, target.Item2));
            yield return Tap(Key.LeftShift, Key.Period);
            yield return WaitForScene();
            Check("native_descent_enters_authored_scene", FellingSceneRuntime.IsActive(_input.CurrentZone));
            _definition = FellingSceneDefinition.Load();
            Require(_definition != null && _definition.layers.Count(l => l.mutable) == 39, "Exactly 39 source components.");
            Check("all_source_components_initially_present", _definition.layers.All(l => FellingSceneRuntime.IsPresent(_input.CurrentZone, l.id)));
            Check("three_native_fauna_initially_present", Fauna().Count == 3 && Fauna().All(e => e.HasPart<BrainPart>() && e.GetStatValue("Hitpoints") > 0));
            Check("three_native_dressing_owners_initially_present", FellingScenePopulation.DressingSpecs.All(s => FellingScenePopulation.FindDressingOwner(_input.CurrentZone, s.id) != null));
            yield return Capture(PreviewOnly ? "native-preview.png" : "native-initial.png");
            yield return Tap(Key.F5);
            Require(SaveGameService.HasQuickSave(), "Native F5 created the private initial checkpoint.");
            _gameId = SaveGameService.GetSaveInfo("Quick").GameID;
            Check("initial_checkpoint_is_owned", OwnedCheckpoint());
            if (PreviewOnly)
            {
                Check("preview_leaves_all_39_props_intact", _definition.layers.Where(l => l.mutable).All(l => FellingSceneRuntime.IsPresent(_input.CurrentZone, l.id)));
                yield break;
            }

            StartProfile();
            var first = _definition.layers.First(l => l.mutable && l.blocksMovement);
            yield return Clear(first);
            Check("first_rock_collision_opens", !_input.CurrentZone.GetCell(first.anchorX, first.anchorY).BlocksMovement(_input.PlayerEntity));
            yield return WalkTo((first.anchorX, first.anchorY));
            Check("native_step_enters_removed_rock_footprint", Position() == (first.anchorX, first.anchorY));
            yield return Capture("native-clear-first.png");
            yield return Tap(Key.LeftShift, Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID), "Native exit to world map succeeds.");
            yield return Tap(Key.LeftShift, Key.Period);
            yield return WaitForScene();
            Check("leave_and_return_does_not_respawn_prop", RemovedAndHidden(first.id));
            yield return Tap(Key.F5);
            yield return Tap(Key.F6);
            yield return WaitForScene();
            Check("native_f5_f6_preserves_first_removal", RemovedAndHidden(first.id) && OwnedCheckpoint());

            var remaining = _definition.layers.Where(l => l.mutable && l.id != first.id).ToList();
            while (remaining.Count > 0)
            {
                FellingSceneDefinition.Layer selected = null; List<(int x, int y)> shortest = null;
                foreach (var layer in remaining)
                {
                    var path = PathToAdjacent(layer.anchorX, layer.anchorY);
                    if (path != null && (shortest == null || path.Count < shortest.Count)) { selected = layer; shortest = path; }
                }
                if (selected == null)
                {
                    Require(remaining.Any(l => FindPath(AdjacentTargets(l.anchorX, l.anchorY), false, true) != null),
                        "A remaining source object has a terrain route; only mobile occupancy may delay it.");
                    Require(_faunaWaits < 24, "Bounded native waiting for fauna to clear routes.");
                    _faunaWaits++; yield return Tap(Key.Period); continue;
                }
                yield return Clear(selected, shortest);
                remaining.Remove(selected);
            }
            Check("all_39_removed_by_native_menus", _clearActions == 39 && _removed.Count == 39 && _definition.layers.Where(l => l.mutable).All(l => RemovedAndHidden(l.id)));
            Check("all_fixed_source_layers_remain", _definition.layers.Where(l => !l.mutable).All(l => FellingSceneRuntime.IsPresent(_input.CurrentZone, l.id)));
            yield return Capture("native-all-removed.png");
            yield return Tap(Key.F5);
            yield return Tap(Key.F6);
            yield return WaitForScene();
            Check("native_save_reload_preserves_all_39_removals", _definition.layers.Where(l => l.mutable).All(l => RemovedAndHidden(l.id)) && OwnedCheckpoint());

            yield return AuditFaunaAndDressing();

            foreach (var landmark in _definition.landmarks.Where(l => l.kind == "bare"))
            {
                yield return WalkTo((landmark.x, landmark.y));
                Check("native_reaches_" + landmark.id, Position() == (landmark.x, landmark.y));
            }
            var seventh = _definition.landmarks.Single(l => l.kind == "seventh");
            yield return WalkTo((seventh.x, seventh.y), allowSeventh: true);
            Check("native_reaches_seventh_last", Position() == (seventh.x, seventh.y));
            Check("seventh_applies_native_exposure", _input.PlayerEntity.GetPart<StatusEffectsPart>()?.HasEffect<ConfusedEffect>() == true);
            Check("final_save_scope_still_private", SaveGameService.SaveRootOverride == _root && OwnedCheckpoint());
            yield return Capture("native-seventh.png");
        }

        private List<Entity> Fauna() => _input.CurrentZone.GetReadOnlyEntities().Where(e => e.HasTag(FellingScenePopulation.FaunaTag)).ToList();

        private IEnumerator AuditFaunaAndDressing()
        {
            var ids = Fauna().Select(e => e.ID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Require(ids.Length == 3, "Three real living habitat actors are available for native examination.");
            foreach (string id in ids)
            {
                yield return ApproachEntity(id);
                var animal = _input.CurrentZone.GetReadOnlyEntities().Single(e => e.ID == id);
                var body = NativeBody(animal);
                Require(body != null && body.enabled && body.gameObject.activeInHierarchy && body.sprite != null,
                    "Visible native animated sprite for " + id);
                int firstFrame = body.sprite.GetInstanceID(); bool animated = false;
                for (int i = 0; i < 12 && !animated; i++)
                {
                    yield return new WaitForSecondsRealtime(.1f);
                    animated = body != null && body.sprite != null && body.sprite.GetInstanceID() != firstFrame;
                }
                Check("native_animation_" + id, animated);
                int beforeTick = _input.TurnManager.TickCount;
                yield return OpenEntityMenu(animal);
                int firstNewSerial = MessageLog.NextSerialValue;
                yield return SelectCommand("Examine");
                string description = animal.GetPart<ExaminablePart>()?.Text;
                Check("native_examine_" + id, MessageLog.GetAllEntries().Any(entry => entry.Serial >= firstNewSerial
                    && entry.Text.StartsWith("You see", StringComparison.Ordinal)
                    && entry.Text.Contains(animal.GetDisplayName())
                    && (string.IsNullOrWhiteSpace(description) || entry.Text.Contains(description.Trim())))
                    && _input.TurnManager.TickCount == beforeTick && _input.CurrentZone.GetEntityCell(animal) != null);
                _faunaExamined++;
            }
            int tick = _input.TurnManager.TickCount;
            yield return Tap(Key.Period); yield return Tap(Key.Period);
            Check("native_waits_preserve_live_fauna", _input.TurnManager.TickCount > tick
                && Fauna().Count == 3 && Fauna().All(e => e.GetStatValue("Hitpoints") > 0 && e.GetPart<BrainPart>()?.CurrentZone == _input.CurrentZone));

            int mushroomBefore = InventoryUnits("Mushroom"), stoneBefore = InventoryUnits("Tepuibone");
            foreach (var spec in FellingScenePopulation.DressingSpecs)
            {
                var owner = FellingScenePopulation.FindDressingOwner(_input.CurrentZone, spec.id);
                Require(owner != null, "Live supplemental owner for " + spec.id);
                var cell = _input.CurrentZone.GetEntityCell(owner);
                var path = PathToAdjacent(cell.X, cell.Y);
                if (path == null) yield return AwaitRoute(AdjacentTargets(cell.X, cell.Y), false, value => path = value);
                yield return Follow(path, AdjacentTargets(cell.X, cell.Y));
                Require(DressingRendered(owner), "Supplemental native art is visible before action: " + spec.id);
                int unitsBefore = InventoryUnits(spec.blueprint == "MushroomRing" ? "Mushroom" : "Tepuibone");
                yield return OpenEntityMenu(owner);
                yield return SelectCommand(spec.blueprint == "MushroomRing" ? "Harvest" : "Take");
                yield return null;
                int gained = InventoryUnits(spec.blueprint == "MushroomRing" ? "Mushroom" : "Tepuibone") - unitsBefore;
                Check("native_" + (spec.blueprint == "MushroomRing" ? "harvest_" : "take_") + spec.id,
                    spec.blueprint == "MushroomRing" ? gained >= 2 && gained <= 4 : gained == 1);
                Check("consumed_dressing_disappears_" + spec.id,
                    FellingScenePopulation.FindDressingOwner(_input.CurrentZone, spec.id) == null && !DressingRendered(owner) && DressingHidden(spec.id));
                _dressingActions++;
            }
            Check("native_harvest_and_take_inventory_totals", InventoryUnits("Mushroom") - mushroomBefore >= 4
                && InventoryUnits("Mushroom") - mushroomBefore <= 8 && InventoryUnits("Tepuibone") == stoneBefore + 1);
            int mushrooms = InventoryUnits("Mushroom"), stones = InventoryUnits("Tepuibone");
            yield return Capture("native-fauna-and-harvest.png");
            yield return Tap(Key.F5); yield return Tap(Key.F6); yield return WaitForScene();
            Check("native_save_reload_keeps_dressing_consumed", FellingScenePopulation.DressingSpecs.All(s => FellingScenePopulation.FindDressingOwner(_input.CurrentZone, s.id) == null && DressingHidden(s.id))
                && InventoryUnits("Mushroom") == mushrooms && InventoryUnits("Tepuibone") == stones);
            Check("native_save_reload_keeps_fauna_identities", Fauna().Select(e => e.ID).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(ids));
        }
        private IEnumerator ApproachEntity(string id)
        {
            for (int step = 0; step < 180; step++)
            {
                var entity = _input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == id);
                Require(entity != null && entity.GetStatValue("Hitpoints") > 0, "Living native target " + id);
                var cell = _input.CurrentZone.GetEntityCell(entity); var here = Position();
                Require(cell != null, "Native actor is in the active zone.");
                if (Math.Abs(here.x - cell.X) + Math.Abs(here.y - cell.Y) == 1) yield break;
                var path = PathToAdjacent(cell.X, cell.Y);
                if (path == null) yield return AwaitRoute(AdjacentTargets(cell.X, cell.Y), false, value => path = value);
                Require(path.Count > 0, "Current route to moving native actor " + id);
                // Observe its actual new cell after each action instead of chasing
                // an obsolete full path or relocating the actor for the audit.
                var next = path[0]; yield return Step(DirectionKey(next.x - here.x, next.y - here.y), (next.x - here.x, next.y - here.y));
            }
            throw new InvalidOperationException("Moving-fauna approach exceeded 180 native steps: " + id);
        }
        private IEnumerator OpenEntityMenu(Entity owner)
        {
            var target = _input.CurrentZone.GetEntityCell(owner); var p = Position();
            Require(target != null && Math.Abs(target.X - p.x) + Math.Abs(target.Y - p.y) == 1, "Native menu target is cardinally adjacent.");
            yield return Tap(Key.C); yield return Tap(DirectionKey(target.X - p.x, target.Y - p.y));
            Require(State() == "WorldActionMenuOpen", "Actual entity action menu.");
            string pick = WorldInteractionSystem.PickTargetCommandPrefix + owner.ID;
            if (!Actions().Any(a => a.Command == pick) && Actions().Any(a => a.Command == WorldInteractionSystem.PickCellCommand))
                yield return SelectCommand(WorldInteractionSystem.PickCellCommand);
            if (Actions().Any(a => a.Command == pick)) yield return SelectCommand(pick);
            Require(State() == "WorldActionMenuOpen"
                && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, owner)
                && ReferenceEquals(_input.WorldActionMenuUI.SelectedCell, target)
                && !_input.WorldActionMenuUI.SelectedCellIsPile,
                "Native selection resolved the exact individual owner, not its pile summary: " + owner.ID);
        }
        private int InventoryUnits(string blueprint) => _input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        private SpriteRenderer NativeBody(Entity entity)
        {
            var renderer = FindFirstObjectByType<AnimatedEntityRenderer>();
            if (renderer == null) return null;
            var views = (IDictionary)typeof(AnimatedEntityRenderer).GetField("_views", Private).GetValue(renderer);
            if (!views.Contains(entity)) return null;
            var view = views[entity];
            return (SpriteRenderer)view.GetType().GetField("Body", BindingFlags.Instance | BindingFlags.Public).GetValue(view);
        }
        private bool DressingRendered(Entity entity)
        {
            var presenter = FindFirstObjectByType<FellingDressingPresenter>();
            return presenter != null && presenter.IsRenderedEntity(entity);
        }

        private bool DressingHidden(string id)
        {
            var presenter = FindFirstObjectByType<FellingDressingPresenter>();
            if (presenter == null || !presenter.IsReady) return false;
            var view = presenter.GetComponentsInChildren<SpriteRenderer>(true).SingleOrDefault(r => r.gameObject.name == id);
            return view != null && (!view.enabled || !view.gameObject.activeInHierarchy);
        }

        private IEnumerator Clear(FellingSceneDefinition.Layer layer, List<(int x, int y)> path = null)
        {
            path = path ?? PathToAdjacent(layer.anchorX, layer.anchorY);
            if (path == null) yield return AwaitRoute(AdjacentTargets(layer.anchorX, layer.anchorY), false, value => path = value);
            yield return Follow(path, AdjacentTargets(layer.anchorX, layer.anchorY));
            var owner = FellingSceneRuntime.FindOwner(_input.CurrentZone, layer.id);
            Require(owner != null, "Live owner before native clear: " + layer.id);
            var p = Position(); int dx = layer.anchorX - p.x, dy = layer.anchorY - p.y;
            Require(Math.Abs(dx) + Math.Abs(dy) == 1, "Cardinal action reach for " + layer.id);
            int tick = _input.TurnManager.TickCount;
            int energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            int speed = _input.TurnManager.GetSpeed(_input.PlayerEntity);
            Require(speed > 0 && energy >= TurnManager.ActionThreshold, "Player is ready to spend one action.");
            // An action spends 1000 energy; ticks refill the deficit at Speed
            // energy per tick. Preserve carried energy instead of assuming
            // one tick per action or assuming an exact 1000 starting energy.
            int expectedTicks = (int)Math.Max(0L, (2L * TurnManager.ActionThreshold - energy + speed - 1) / speed);
            yield return Tap(Key.C);
            yield return Tap(DirectionKey(dx, dy));
            Require(State() == "WorldActionMenuOpen", "Actual world-action menu for " + layer.id);
            string pick = WorldInteractionSystem.PickTargetCommandPrefix + owner.ID;
            if (!Actions().Any(a => a.Command == pick) && Actions().Any(a => a.Command == WorldInteractionSystem.PickCellCommand))
                yield return SelectCommand(WorldInteractionSystem.PickCellCommand);
            if (Actions().Any(a => a.Command == pick)) yield return SelectCommand(pick);
            yield return SelectCommand(FellingScenePropPart.ClearCommand);
            yield return new WaitForSecondsRealtime(.04f);
            int chargedTicks = _input.TurnManager.TickCount - tick;
            _clearTicks += chargedTicks;
            Check("clear_one_turn_" + layer.id, chargedTicks == expectedTicks
                && _input.TurnManager.GetSpeed(_input.PlayerEntity) == speed
                && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy - TurnManager.ActionThreshold + chargedTicks * speed
                && _input.TurnManager.WaitingForInput && ReferenceEquals(_input.TurnManager.CurrentActor, _input.PlayerEntity));
            Check("clear_owner_sprite_contact_" + layer.id, RemovedAndHidden(layer.id));
            Require(RemovedAndHidden(layer.id), "Native clear removed owner and both visual passes for " + layer.id);
            _removed.Add(layer.id); _clearActions++;
        }

        private IEnumerator WalkTo((int x, int y) target, bool allowSeventh = false)
        {
            var path = FindPath(new HashSet<(int, int)> { target }, allowSeventh);
            if (path == null) yield return AwaitRoute(new HashSet<(int, int)> { target }, allowSeventh, value => path = value);
            yield return Follow(path, new HashSet<(int, int)> { target }, allowSeventh);
        }
        private List<(int x, int y)> PathToAdjacent(int x, int y)
        {
            return FindPath(AdjacentTargets(x, y), false);
        }
        private static HashSet<(int, int)> AdjacentTargets(int x, int y)
        {
            var targets = new HashSet<(int, int)>();
            foreach (var d in Directions) targets.Add((x + d.x, y + d.y));
            return targets;
        }
        private List<(int x, int y)> FindPath(HashSet<(int, int)> targets, bool allowSeventh, bool ignoreMobile = false)
        {
            var start = Position(); var queue = new Queue<(int, int)>(); queue.Enqueue(start);
            var previous = new Dictionary<(int, int), (int, int)> { [start] = start };
            var seventh = _definition?.landmarks.Single(l => l.kind == "seventh");
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (targets.Contains(p))
                {
                    var result = new List<(int, int)>();
                    while (p != start) { result.Add(p); p = previous[p]; }
                    result.Reverse(); return result;
                }
                foreach (var d in Directions)
                {
                    var n = (p.Item1 + d.x, p.Item2 + d.y);
                    if (previous.ContainsKey(n) || !_input.CurrentZone.InBounds(n.Item1, n.Item2)) continue;
                    if (!allowSeventh && seventh != null && n == (seventh.x, seventh.y)) continue;
                    var cell = _input.CurrentZone.GetCell(n.Item1, n.Item2);
                    // IsPassable checks Solid tags only. Native creatures use
                    // PhysicsPart.Solid, so the actual movement gate is required.
                    if (cell.BlocksMovement(_input.PlayerEntity) && !(ignoreMobile && MobileOnlyBlocker(cell))) continue;
                    previous.Add(n, p); queue.Enqueue(n);
                }
            }
            return null;
        }
        private static bool MobileOnlyBlocker(Cell cell)
        {
            if (cell == null || !cell.BlocksMovement()) return false;
            var blockers = cell.Objects.Where(e => e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true).ToArray();
            return blockers.Length > 0 && blockers.All(e => e.HasPart<BrainPart>());
        }
        private IEnumerator AwaitRoute(HashSet<(int, int)> targets, bool allowSeventh, Action<List<(int x, int y)>> receive)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var path = FindPath(targets, allowSeventh);
                if (path != null) { receive(path); yield break; }
                Require(FindPath(targets, allowSeventh, true) != null, "No terrain route; blockage is not solely mobile fauna.");
                Require(_faunaWaits < 24, "Bounded native waiting for mobile fauna.");
                _faunaWaits++; yield return Tap(Key.Period);
            }
            throw new InvalidOperationException("Native fauna did not clear the route after twelve real wait actions.");
        }
        private IEnumerator Follow(List<(int x, int y)> path, HashSet<(int, int)> targets, bool allowSeventh = false)
        {
            int index = 0, replans = 0;
            while (index < path.Count)
            {
                var next = path[index];
                var cell = _input.CurrentZone.GetCell(next.x, next.y);
                // Only living mobile occupancy justifies replacing a route.
                // A rejected key, fixed obstacle or unexpected state fails loudly.
                if (MobileOnlyBlocker(cell))
                {
                    Require(++replans <= 24, "Bounded rerouting around mobile fauna.");
                    path = FindPath(targets, allowSeventh);
                    if (path == null) yield return AwaitRoute(targets, allowSeventh, value => path = value);
                    index = 0;
                    continue;
                }
                var p = Position(); int dx = next.x - p.x, dy = next.y - p.y;
                Require(Math.Abs(dx) + Math.Abs(dy) == 1, "Path stays cardinal and observes current player.");
                yield return Step(DirectionKey(dx, dy), (dx, dy));
                index++;
            }
        }
        private IEnumerator Step(Key key, (int x, int y) direction)
        {
            var before = Position();
            string beforeState = InputSnapshot(before.x + direction.x, before.y + direction.y);
            var targetCell = _input.CurrentZone.GetCell(before.x + direction.x, before.y + direction.y);
            Require(targetCell != null && !targetCell.BlocksMovement(_input.PlayerEntity),
                "Audit route refuses a known native movement blocker before sending " + key + ": " + beforeState);
            yield return Tap(key); _nativeSteps++;
            Require(Position() == (before.x + direction.x, before.y + direction.y),
                "Native movement failed after " + key + "; expected " + (before.x + direction.x, before.y + direction.y)
                + "; got " + Position() + "; before {" + beforeState + "}; after {"
                + InputSnapshot(before.x + direction.x, before.y + direction.y) + "}");
        }
        private string InputSnapshot(int targetX, int targetY)
        {
            var cell = _input.CurrentZone.GetCell(targetX, targetY);
            string blockers = cell == null ? "outside-zone" : string.Join(",", cell.Objects
                .Where(e => e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)
                .Select(e => e.BlueprintName + ":" + e.ID));
            float lastMove = (float)typeof(InputHandler).GetField("_lastMoveTime", Private).GetValue(_input);
            return "state=" + State() + ";tick=" + _input.TurnManager.TickCount + ";energy=" + _input.TurnManager.GetEnergy(_input.PlayerEntity)
                + ";waiting=" + _input.TurnManager.WaitingForInput + ";hp=" + _input.PlayerEntity.GetStatValue("Hitpoints")
                + ";actor=" + _input.TurnManager.CurrentActor?.ID + ";frame=" + Time.frameCount
                + ";time=" + Time.time + ";lastMove=" + lastMove + ";repeatDelay=" + _input.MoveRepeatDelay
                + ";keyboard=" + Keyboard.current?.deviceId + ";ownedKeyboard=" + _keyboard?.deviceId
                + ";blockers=" + blockers + ";messages=" + string.Join(" / ", MessageLog.GetMessages().TakeLast(3));
        }
        private static Key DirectionKey(int x, int y) => x < 0 ? Key.A : x > 0 ? Key.D : y < 0 ? Key.W : Key.S;
        private IEnumerator SelectCommand(string command)
        {
            var rows = Actions(); int index = rows.FindIndex(a => a.Command == command);
            Require(index >= 0, "Native menu contains " + command);
            int cursor = (int)typeof(WorldActionMenuUI).GetField("_cursorIndex", Private).GetValue(_input.WorldActionMenuUI);
            for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
            for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_elapsed.Elapsed.TotalSeconds < 360, "Bounded six-minute native audit.");
            double began = Time.realtimeSinceStartupAsDouble;
            // Respect the production scaled-time input gate before sending a
            // single pulse. This is readiness waiting, never an action retry.
            while (_input != null && Time.time - (float)typeof(InputHandler).GetField("_lastMoveTime", Private).GetValue(_input)
                < _input.MoveRepeatDelay)
            {
                Require(Time.realtimeSinceStartupAsDouble - began < 3, "Native input rate gate did not reopen.");
                yield return null;
            }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            // Coroutine continuation runs after the next Update. Each edge
            // therefore survives one complete InputSystem/MonoBehaviour frame.
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return new WaitForSecondsRealtime(.13f);
        }
        private IEnumerator WaitForScene()
        {
            for (int i = 0; i < 80; i++)
            {
                var presenter = Presenter();
                if (_input.CurrentZone.ZoneID == FellingSiteBuilder.ZoneID && presenter != null && presenter.IsReady
                    && ReferenceEquals(presenter.CurrentZone, _input.CurrentZone)) yield break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            throw new InvalidOperationException("Native Felling presenter failed to become ready: " + Presenter()?.Failure);
        }
        private IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(ReportDirectory);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(ReportDirectory, name));
            yield return new WaitForSecondsRealtime(.25f);
        }
        private FellingScenePresenter Presenter() => FindFirstObjectByType<FellingScenePresenter>();
        private (int x, int y) Position() => _input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
        private string State() => typeof(InputHandler).GetField("_inputState", Private).GetValue(_input).ToString();
        private List<InventoryAction> Actions() => (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(_input.WorldActionMenuUI);
        private bool RemovedAndHidden(string id)
        {
            if (FellingSceneRuntime.IsPresent(_input.CurrentZone, id) || FellingSceneRuntime.FindOwner(_input.CurrentZone, id) != null
                || FellingSceneRuntime.GetState(_input.CurrentZone)?.WasRemoved(id) != true) return false;
            var presenter = Presenter();
            if (presenter == null || !presenter.IsReady) return false;
            var renderers = presenter.GetComponentsInChildren<SpriteRenderer>(true);
            var sprite = renderers.SingleOrDefault(r => r.gameObject.name == id);
            var contact = renderers.SingleOrDefault(r => r.gameObject.name == id + " contact");
            var layer = _definition.layers.Single(l => l.id == id);
            return sprite != null && !sprite.enabled && (string.IsNullOrEmpty(layer.contactResource) || contact != null && !contact.enabled);
        }
        private bool OwnedCheckpoint() => SaveGameService.SaveRootOverride == _root && !string.IsNullOrEmpty(_gameId)
            && SaveGameService.GetSaveInfo("Quick")?.GameID == _gameId
            && File.Exists(Path.Combine(_root, _gameId, "Quick.sav.gz"))
            && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _gameId));
        private void Check(string name, bool pass)
        {
            _audit.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) Failures++;
            Debug.Log("[FellingScenePlayAudit] " + RunId + " " + _audit[_audit.Count - 1]);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        public void Abort(string reason)
        {
            if (Finished) return;
            StopAllCoroutines(); _fatal = reason; Check("native_audit_aborted: " + reason, false); Finish();
        }
        private void Finish()
        {
            FinishProfile();
            CleanupInput();
            Directory.CreateDirectory(ReportDirectory);
            var report = new Report { runId = RunId, failures = Failures, cases = _audit.Count, nativeSteps = _nativeSteps,
                clearActions = _clearActions, clearTicks = _clearTicks, faunaExamined = _faunaExamined, dressingActions = _dressingActions, faunaWaits = _faunaWaits, previewOnly = PreviewOnly, seconds = _elapsed?.Elapsed.TotalSeconds ?? 0, saveRoot = _root, gameId = _gameId,
                finalZone = _input?.CurrentZone?.ZoneID, fatal = _fatal, audit = _audit.ToArray(), removedIds = _removed.ToArray(),
                profileSeconds = _profileSeconds, profileCapacity = ProfileCapacity, metrics = _profileMetrics,
                profileContext = PreviewOnly ? "Preview only; profiling is reserved for the full native audit." :
                    "Read-only editor capture from first component approach through final landmark. Includes native movement, 39 clear menus, three fauna examinations, two harvests, one pickup, world return, save/load, screenshot and audit-observation overhead. Each recorder retains the latest at most 4096 samples; no FPS or global profiler settings changed." };
            File.WriteAllText(Path.Combine(ReportDirectory, PreviewOnly ? "native-preview.json" : "native-audit.json"), JsonUtility.ToJson(report, true));
            Finished = true; Debug.Log("[FellingScenePlayAudit] " + JsonUtility.ToJson(report));
        }
        private void StartProfile()
        {
            _recorders = new ProfilerRecorder[_profileNames.Length];
            _profileStarted = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < _recorders.Length; i++)
            {
                var category = i == 0 ? ProfilerCategory.Scripts : i == 1 ? ProfilerCategory.Internal : ProfilerCategory.Memory;
                var options = ProfilerRecorderOptions.Default;
                if (i == 0) options |= ProfilerRecorderOptions.SumAllSamplesInFrame;
                _recorders[i] = ProfilerRecorder.StartNew(category, _profileNames[i], ProfileCapacity, options);
            }
        }
        private void FinishProfile()
        {
            if (_recorders == null) return;
            _profileSeconds = Time.realtimeSinceStartupAsDouble - _profileStarted;
            _profileMetrics = new Metric[_recorders.Length];
            for (int i = 0; i < _recorders.Length; i++)
            {
                var metric = new Metric { name = _profileNames[i], unit = i == 2 ? "bytes" : "nanoseconds",
                    average = -1, max = -1, p99 = -1 };
                try
                {
                    metric.available = _recorders[i].Valid;
                    if (metric.available)
                    {
                        _recorders[i].Stop();
                        var samples = _recorders[i].ToArray();
                        var values = new long[samples.Length]; double sum = 0;
                        for (int j = 0; j < samples.Length; j++) { values[j] = samples[j].Value; sum += values[j]; }
                        Array.Sort(values); metric.count = values.Length;
                        if (values.Length > 0)
                        {
                            metric.average = sum / values.Length; metric.max = values[values.Length - 1];
                            metric.p99 = values[Math.Max(0, (int)Math.Ceiling(values.Length * .99) - 1)];
                        }
                    }
                }
                catch (Exception error) { metric.available = false; metric.error = error.Message; }
                finally { _recorders[i].Dispose(); }
                _profileMetrics[i] = metric;
            }
            _recorders = null;
            Check("live_profiler_markers_have_samples", _profileMetrics.All(m => m.available && m.count > 0));
        }
        private void CleanupInput()
        {
            if (!_initialized || _cleaned) return;
            _cleaned = true;
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        private void OnDestroy()
        {
            if (_initialized && !Finished) { _fatal = "Play stopped before native audit completed."; Check("interrupted_before_finish", false); Finish(); }
            CleanupInput();
            // NativeSaveIsolation restores the inherited root only AFTER all Play
            // teardown and bootstrap shutdown saves; restoring here would risk a user save.
        }
        [Serializable] private sealed class Metric
        {
            public string name, unit, error;
            public bool available;
            public int count;
            public double average;
            public long max, p99;
        }
        [Serializable] private sealed class Report
        {
            public string runId, saveRoot, gameId, finalZone, fatal, profileContext;
            public int failures, cases, nativeSteps, clearActions, clearTicks, profileCapacity, faunaExamined, dressingActions, faunaWaits;
            public bool previewOnly;
            public double seconds, profileSeconds;
            public Metric[] metrics;
            public string[] audit, removedIds;
        }
    }
}
