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
    /// <summary>Isolated native inventory/turn witness for cord, clay and water.
    /// Finite controlled supplies; preserves the real campaign and scene.</summary>
    public sealed class CombatSupplyNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        static readonly string[] Required = { "ordinary_classic_start", "cord_one_unit_one_action", "cord_native_model",
            "native_step_triggers_cord", "root_blocks_movement", "root_expires_and_walk_resumes", "snare_model_removed",
            "clay_one_unit_one_action", "clay_extinguishes_without_wet", "water_one_unit_one_action", "water_extinguishes_and_wets" };
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        CameraFollow framedCamera; float oldCameraZoom; bool cameraFramed;
        bool oldBackground, cleaned, errorsFinalized; int failures, errors; string fatal;
        Entity cord, clay, water;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (Finished && !Required.All(n => checks.Contains("PASS " + n)) ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/CombatSupplyImprovisation/Native", runId));
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
            yield return InventoryChoice(cord, "LayCordSnare|", c => c.Split('|')[4] == "21" && c.Split('|')[5] == "12", "02-cord-action-menu");
            var snare = input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "KnotflaxSnare");
            Check("cord_one_unit_one_action", snare != null && !Player.GetPart<InventoryPart>().Objects.Contains(cord)
                && input.TurnManager.TickCount == tick + 10);
            Require(snare != null, "placed snare");
            yield return Capture("03-deployed-cord-model");
            Check("cord_native_model", input.ZoneRenderer.SpawnRing3D.TryGetApprovedStyle(snare, out var proof)
                && proof.ModelId.StartsWith("spread-scenery-knotflaxsnare-", StringComparison.Ordinal));
            yield return Tap(Key.RightArrow);
            Check("native_step_triggers_cord", input.CurrentZone.GetEntityPosition(Player) == (21, 12)
                && Player.HasEffect<RootedEffect>() && input.CurrentZone.GetEntityCell(snare) == null);
            yield return Capture("04-snare-caught-player");
            Check("snare_model_removed", !input.ZoneRenderer.SpawnRing3D.TryGetEntityView(snare, out _, out _));
            var held = input.CurrentZone.GetEntityPosition(Player);
            yield return Tap(Key.RightArrow);
            Check("root_blocks_movement", input.CurrentZone.GetEntityPosition(Player) == held);
            for (int i = 0; i < 3 && Player.HasEffect<RootedEffect>(); i++) yield return Tap(Key.Period);
            yield return Tap(Key.RightArrow);
            Check("root_expires_and_walk_resumes", !Player.HasEffect<RootedEffect>() && input.CurrentZone.GetEntityPosition(Player) == (22, 12));

            Require(Player.ApplyEffect(new BurningEffect(1)), "controlled burning condition");
            tick = input.TurnManager.TickCount;
            yield return InventoryChoice(clay, "SmotherFire|", c => c.Split('|')[4] == Uri.EscapeDataString(Player.ID), "05-clay-action-menu");
            Check("clay_one_unit_one_action", !Player.GetPart<InventoryPart>().Objects.Contains(clay) && input.TurnManager.TickCount == tick + 10);
            Check("clay_extinguishes_without_wet", !Player.HasEffect<BurningEffect>() && !Player.HasEffect<WetEffect>());

            Require(Player.ApplyEffect(new BurningEffect(1)), "second controlled burning condition");
            tick = input.TurnManager.TickCount;
            yield return InventoryChoice(water, "DrenchCreature|", c => c.Split('|')[4] == Uri.EscapeDataString(Player.ID), "06-water-action-menu");
            Check("water_one_unit_one_action", Player.GetPart<InventoryPart>().Objects.Contains(water) && water.GetPart<WaterskinPart>().Charges == 1
                && input.TurnManager.TickCount == tick + 10);
            Check("water_extinguishes_and_wets", !Player.HasEffect<BurningEffect>() && Player.GetEffect<WetEffect>()?.Moisture > .2f);
            yield return Capture("07-drenched-player");
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
            cord = Carry("KnotflaxCord"); clay = Carry("FireClay"); water = Carry("Waterskin"); water.GetPart<WaterskinPart>().Charges = 2;
            // Frame nearby geometry through the ordinary runtime camera API;
            // do not write user preferences or mutate editor camera settings.
            framedCamera = input.CameraFollow; Require(framedCamera != null, "gameplay camera framing");
            oldCameraZoom = framedCamera.GameplayZoomMultiplier; cameraFramed = true;
            framedCamera.GameplayZoomMultiplier = .42f;
            input.CameraFollow?.SnapToPlayer(); RevealFixture(); ZoneRenderHooks.MarkFullDirty("CombatSupplyNative.ControlledFixture");
        }
        void RevealFixture()
        { for (int x = 13; x <= 25; x++) for (int y = 8; y <= 16; y++) { var cell = input.CurrentZone.GetCell(x, y); cell.IsVisible = cell.Explored = true; } }
        IEnumerator InventoryChoice(Entity item, string prefix, Func<string, bool> match, string capture)
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
                if (command.StartsWith(prefix, StringComparison.Ordinal) && match(command)) wanted = i;
            }
            Require(wanted >= 0, "native action " + prefix);
            int guard = 150; while ((int)Field(popup, "CursorIndex") != wanted && guard-- > 0)
                yield return Tap((int)Field(popup, "CursorIndex") < wanted ? Key.DownArrow : Key.UpArrow);
            Require((int)Field(popup, "CursorIndex") == wanted, "native navigation reached selected tile");
            yield return Capture(capture); yield return Tap(Key.Enter); Require(State == "Normal", "paid action returns to play");
        }
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
                fixture = "Ordinary N/Classic start in an isolated flattened Spread zone. One coil, one clay and a two-charge waterskin are granted. Player is repositioned; burning is applied twice as a controlled precondition. No real campaign loaded. Native I/arrow/Enter actions use the exact-owner public popup entry; movement and waiting use native keys. Runtime camera zoom temporarily0.42 and restored; no camera preferences changed.",
                canVerify = "Three real inventory menu choices, exact payment and ordinary +10-tick action costs, submitted snare geometry, native movement restraint/expiry, dry clay extinguishing and wet water extinguishing. Captures permit separate visual inspection.",
                cannotVerify = "Controlled supplies and status setup do not prove natural acquisition, encounter balance, player understanding or long-term fun. No autonomous NPC supply choice is claimed. Electrical and rescue countercases are covered by EditMode tests, not this native scenario."

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
