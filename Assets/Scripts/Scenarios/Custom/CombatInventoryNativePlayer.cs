using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite isolated native-key ground-utility witness and a detached
    /// production pursuit probe. Controlled supplies do not establish discovery.</summary>
    public sealed class CombatInventoryNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        static readonly string[] Required = { "ordinary_classic_start", "grease_one_unit_one_action", "grease_rendered",
            "grit_one_unit_one_action", "grit_rendered", "grit_preserves_oil", "veil_consumed_direct_command",
            "veil_blocks_sight_has_cold_gas", "veil_rendered", "veil_center_hidden", "pursuit_remembers_old_cell", "pursuit_budget_six", "pursuit_probe_preserves_live_context" };
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        CameraFollow framedCamera; float oldCameraZoom; bool cameraFramed;
        bool oldBackground, cleaned, errorsFinalized; int failures, errors; string fatal;
        Entity oil, sand, veil;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (Finished && !Required.All(n => checks.Contains("PASS " + n)) ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/CombatInventoryImprovisation/Native", runId));
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated save launcher");
            clock = System.Diagnostics.Stopwatch.StartNew(); oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings; oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(RunSafely(Run()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f); input = FindFirstObjectByType<InputHandler>(); Require(input != null, "ordinary bootstrap");
            Require(((BootMenuController)Field(input, "_bootMenuController")).IsActive, "new-game menu");
            yield return Tap(Key.N); var build = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(build.IsOpen, "build picker"); int classic = build.Model.Options.ToList().FindIndex(b => b.Id == "classic");
            Require(classic >= 0 && classic < 9, "Classic authored build"); yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (classic + 1)));
            yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(.3f); Require(State == "Normal", "normal gameplay");
            Check("ordinary_classic_start", Player.GetProperty(StartingBuildService.PropertyName) == "classic" && !DebugInvincibility.IsEnabled(Player));
            BuildFixture(); yield return Capture("01-finite-controlled-supplies");

            int tick = input.TurnManager.TickCount;
            yield return InventoryChoice(oil, "SpreadGrease|", 21, 12, "02-grease-action-menu");
            Check("grease_one_unit_one_action", !Player.GetPart<InventoryPart>().Objects.Contains(oil)
                && input.CurrentZone.TileState.HasCoating(21, 12, "oil") && input.TurnManager.TickCount == tick + 10);
            yield return Capture("03-grease-ground"); Check("grease_rendered", Rendered(21, 12, "coating:oil"));

            tick = input.TurnManager.TickCount;
            yield return InventoryChoice(sand, "ScatterGrit|", 21, 12, "04-grit-action-menu");
            Check("grit_one_unit_one_action", !Player.GetPart<InventoryPart>().Objects.Contains(sand)
                && LiquidSlipSystem.HasGrit(input.CurrentZone, 21, 12) && input.TurnManager.TickCount == tick + 10);
            yield return Capture("05-gritted-grease-ground"); Check("grit_rendered", Rendered(21, 12, "residue:grit"));
            Check("grit_preserves_oil", input.CurrentZone.TileState.HasCoating(21, 12, "oil")
                && LiquidSlipSystem.FindSlipperyLiquid(input.CurrentZone, input.CurrentZone.GetCell(21, 12)) == null);

            tick = input.TurnManager.TickCount;
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(veil, 17, 12, new System.Random(1)), Player, input.CurrentZone);
            Check("veil_consumed_direct_command", result.Success && !Player.GetPart<InventoryPart>().Objects.Contains(veil)
                && input.TurnManager.TickCount == tick);
            Check("veil_blocks_sight_has_cold_gas", input.CurrentZone.TileState.ObscuresSight(17, 12)
                && input.CurrentZone.GetCell(17, 12).Occupants.Any(e => e.GetPart<GasPoolPart>()?.GasId == "cryo-mist"));
            ZoneRenderHooks.MarkFullDirty("CombatInventoryNative.Veil");
            yield return Capture("06-thrown-veil-cover");
            var nearEdge = input.CurrentZone.GetCell(18, 12);
            Check("veil_rendered", nearEdge.IsVisible && (Rendered(18, 12, "veil-mist")
                || nearEdge.Occupants.Any(e => e.GetPart<GasPoolPart>()?.GasId == "cryo-mist"
                    && input.ZoneRenderer.SpawnRing3D.TryGetGasVolume(e, out var root, out _) && Submitted(root))));
            Check("veil_center_hidden", !input.CurrentZone.GetCell(17, 12).IsVisible);

            string before = Snapshot(); ProbePursuit();
            Check("pursuit_probe_preserves_live_context", Snapshot() == before && State == "Normal");
        }
        void BuildFixture()
        {
            var zone = input.CurrentZone;
            foreach (var owner in zone.GetAllEntities().ToArray())
                if (owner != Player) { input.TurnManager.RemoveEntity(owner); zone.RemoveEntity(owner); }
            zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                zone.TileState.Clear(x, y);
                if (x > 0 && y > 0 && x < Zone.Width - 1 && y < Zone.Height - 1) zone.AddEntity(input.EntityFactory.CreateEntity("StoneFloor"), x, y);
            }
            Require(zone.MoveEntity(Player, 20, 12), "controlled player placement");
            Entity Carry(string bp) { var e = input.EntityFactory.CreateEntity(bp); Require(e != null && Player.GetPart<InventoryPart>().AddObject(e), "finite supply " + bp); return e; }
            oil = Carry("FrogOil"); sand = Carry("SilverSand"); veil = Carry("VeilpuffBladder");
            // Frame nearby geometry through the ordinary runtime camera API;
            // do not write user preferences or mutate editor camera settings.
            framedCamera = input.CameraFollow; Require(framedCamera != null, "gameplay camera framing");
            oldCameraZoom = framedCamera.GameplayZoomMultiplier; cameraFramed = true;
            framedCamera.GameplayZoomMultiplier = .42f;
            input.CameraFollow?.SnapToPlayer(); RevealFixture(); ZoneRenderHooks.MarkFullDirty("CombatInventoryNative.ControlledFixture");
        }
        void RevealFixture()
        { for (int x = 13; x <= 25; x++) for (int y = 8; y <= 16; y++) { var cell = input.CurrentZone.GetCell(x, y); cell.IsVisible = cell.Explored = true; } }
        IEnumerator InventoryChoice(Entity item, string prefix, int x, int y, string capture)
        {
            yield return Tap(Key.I); Require(State == "InventoryOpen", "native inventory entry");
            // Exact-owner public host entry; arrow navigation and Enter execute
            // the real menu selection and its ordinary pending-turn handoff.
            Require(input.InventoryUI.ReopenItemActionPopupFor(item), "inventory host entry");
            object popup = Field(input.InventoryUI, "_itemActionPopup"); var actions = (IList)Field(popup, "Actions");
            int wanted = -1;
            for (int i = 0; i < actions.Count; i++)
            {
                string command = (string)Field(actions[i], "Command");
                if (command.StartsWith(prefix, StringComparison.Ordinal) && command.Split('|')[4] == x.ToString() && command.Split('|')[5] == y.ToString()) wanted = i;
            }
            Require(wanted >= 0, "native action " + prefix);
            int guard = 150; while ((int)Field(popup, "CursorIndex") != wanted && guard-- > 0)
                yield return Tap((int)Field(popup, "CursorIndex") < wanted ? Key.DownArrow : Key.UpArrow);
            Require((int)Field(popup, "CursorIndex") == wanted, "native navigation reached selected tile");
            yield return Capture(capture); yield return Tap(Key.Enter); Require(State == "Normal", "paid action returns to play");
        }
        void ProbePursuit()
        {
            // Detached actors use real production command and Brain handlers.
            // Direct AI actions deliberately do not claim normal scheduler timing.
            var zone = new Zone("CombatInventory.PursuitProbe." + runId);
            var observer = input.EntityFactory.CreateEntity("MarlbackScrabbler");
            var target = input.EntityFactory.CreateEntity("Player");
            Require(zone.AddEntity(observer, 10, 10) && zone.AddEntity(target, 16, 10), "detached observed positions");
            var brain = observer.GetPart<BrainPart>(); Require(brain != null, "real observer brain");
            brain.CurrentZone = zone; brain.Wanders = brain.WandersRandomly = false; brain.ClearGoals();
            brain.SetPersonallyHostile(target, alertAllies: false); var goal = new KillGoal(target); brain.PushGoal(goal);
            observer.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Require(goal.HasLastSeen && goal.LastSeenX == 16 && goal.LastSeenY == 10, "visible target recorded first");
            var bladder = input.EntityFactory.CreateEntity("VeilpuffBladder"); Require(target.GetPart<InventoryPart>().AddObject(bladder), "detached finite veil");
            Require(InventorySystem.ExecuteCommand(new ThrowItemCommand(bladder, 13, 10, new System.Random(2)), target, zone).Success, "real detached veil throw");
            Require(zone.MoveEntity(target, 16, 14), "hidden direction change");
            bool visibleBeforeBarrier = AIHelpers.TryGetVisibleTargetCell(observer, target, zone, brain.SightRadius, out _);
            Require(!visibleBeforeBarrier, "thrown veil blocks sight before any fixture barrier exists");
            Require(observer.GetEffect<FrozenEffect>() == null, "observer stays outside the harmful cold cloud");
            var at = zone.GetEntityPosition(observer);
            foreach (var d in new[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1) })
                Require(zone.AddEntity(input.EntityFactory.CreateEntity("StoneWall"), at.x + d.Item1, at.y + d.Item2), "controlled blocked-search boundary");
            // Bound the probe before the first hidden action: entering the real
            // cryo payload would correctly freeze this observer and stop its
            // search actions, which is a different invariant from memory expiry.
            observer.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Check("pursuit_remembers_old_cell", goal.Searching && goal.LastSeenX == 16 && goal.LastSeenY == 10 && goal.SearchRemaining == 5);
            var budgets = new List<int> { goal.SearchRemaining };
            for (int i = 0; i < 5; i++) { observer.FireEventAndRelease(GameEvent.New("TakeTurn")); budgets.Add(goal.SearchRemaining); }
            Check("pursuit_budget_six", goal.Abandoned && goal.Finished() && brain.Target == null
                && budgets.SequenceEqual(new[] { 5, 4, 3, 2, 1, 0 }));
            observations.Add(new { phase = "detached_pursuit_probe", lastSeenX = goal.LastSeenX, lastSeenY = goal.LastSeenY,
                hiddenTarget = zone.GetEntityPosition(target), observer = zone.GetEntityPosition(observer), visibleBeforeBarrier, budgets, goal.Abandoned,
                fixture = "One visible AI action, real thrown veil, hidden target reposition, sight-block assertion before barriers, then eight walls keep the observer outside cold gas for six direct searching actions. The detached zone clock and cloud lifetime are not advanced." });
        }
        bool Rendered(int x, int y, string expected)
        {
            var presenter = input.ZoneRenderer?.SpawnRing3D;
            bool pass = presenter != null && ReferenceEquals(presenter.CurrentZone, input.CurrentZone)
                && presenter.TryGetElementVolume(x, y, out var view, out var sample) && sample.Kind == expected && Submitted(view);
            observations.Add(new { phase = "native_geometry", x, y, expected, submitted = pass }); return pass;
        }
        static bool Submitted(GameObject root) => root != null && root.activeInHierarchy
            && root.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff);
        string Snapshot() => JsonConvert.SerializeObject(new { tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player),
            hp = Player.GetStatValue("Hitpoints"), zone = input.CurrentZone.ZoneID, player = Player.ID,
            pack = Player.GetPart<InventoryPart>().Objects.Select(e => new { e.ID, count = e.GetPart<StackerPart>()?.StackCount }).ToArray(),
            ground = input.CurrentZone.TileState.ToSaveString() });
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.12f); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.06f);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png"); DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "rendered capture"); screenshots.Add(path);
            observations.Add(new { phase = name, state = State, snapshot = Snapshot() }); WriteReport();
        }
        IEnumerator RunSafely(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    bool moved = false; object current = null; Exception error = null;
                    try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception e) { error = e; }
                    if (error != null) { fatal = error.ToString(); Check("precondition", false); break; }
                    if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator child) { stack.Push(child); continue; } yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); Cleanup(); Finished = true; WriteReport(); }
        }
        void Check(string name, bool pass) { checks.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) failures++; WriteReport(); }
        static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        static object Field(object owner, string name) => owner.GetType().GetField(name, Fields).GetValue(owner);
        public void SetUnexpectedErrors(int count) { errors = count; errorsFinalized = true; WriteReport(); }
        public void Abort(string reason) { fatal = reason; failures++; StopAllCoroutines(); Cleanup(); Finished = true; WriteReport(); }
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonConvert.SerializeObject(new {
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == Required.Length,
                failures = Failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots,
                fixture = "Ordinary N/Classic start, unchanged player HP/stats, isolated flattened Spread zone, player repositioned, three finite supplies granted. No real campaign loaded. Exact-owner inventory popup uses a public host entry; arrow/Enter execute its grease/grit selections. Veil is a direct real ThrowItemCommand and does not claim native keyboard throw or its scheduler cost. Runtime camera zoom is temporarily 0.42 and restored; no persistent camera preferences changed. Veil captures use the visible near edge and assert that the center remains hidden.",
                canVerify = "Native-key grease/grit choices, one consumed unit and one ordinary +10-tick action each, visible submitted ground and cloud geometry, real grenade payload, and a detached finite last-seen pursuit probe. Screenshots are supplied for visual inspection.",
                cannotVerify = "Controlled supplies do not prove normal discovery, balance, novelty, long-term fun or novice comprehension. Direct throw/pursuit probes are distinct from player keyboard combat; blocked-observer search holds cloud time fixed and is not a natural escape playtest. Geometry presence alone does not establish screenshot quality."
            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (cameraFramed && framedCamera != null) { framedCamera.GameplayZoomMultiplier = oldCameraZoom; framedCamera.SnapToPlayer(); }
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings); Application.runInBackground = oldBackground;
        }
        void OnDestroy()
        {
            if (!Finished && clock != null) { fatal = "Play stopped before native audit completion."; failures++; Finished = true; WriteReport(); }
            Cleanup();
        }
    }
}
