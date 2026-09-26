using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite native keyboard evidence for examination. Explicitly
    /// stages real factory content in an isolated disposable new game; it does
    /// not demonstrate natural acquisition. Reflection observes UI only.</summary>
    public sealed class DensityExamineNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<Entity> _items = new List<Entity>();
        private readonly List<Description> _descriptions = new List<Description>();
        private InputHandler _input;
        private ScenarioContext _context;
        private Entity _sign;
        private Zone _stagedZone;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors;
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityFollowup/NativeExamine", RunId));

        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Examination audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input = FindFirstObjectByType<InputHandler>();
            Require(_input != null, "ordinary input bootstrap");
            var boot = (BootMenuController)Field(_input, "_bootMenuController");
            Require(boot != null && boot.IsActive, "owned marker presents the native new-game choice");
            yield return Tap(Key.N);
            Require(!boot.IsActive && State() == "Normal", "native N starts the disposable game");
            var manager = _input.ZoneManager as OverworldZoneManager;
            Require(manager != null && manager.WorldSeed == 64, "owned seed 64 bootstrap");
            Require(!DevMode.Enabled, "normal new-game rules; no developer tinkering grants");
            Require(SaveGameService.SaveRootOverride == _ownedRoot, "owned save root remains active");
            StageFactoryContent();
            Check("explicit_factory_staging", _items.Count == 3 && _stagedZone.GetEntityCell(_sign) != null);
            var counts = _items.ToDictionary(e => e, e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            int ticks = _input.TurnManager.TickCount;
            int hp = _input.PlayerEntity.GetStatValue("Hitpoints");
            int effects = (_input.PlayerEntity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0);
            int cached = manager.CachedZoneCount;
            int notes = RegionalTravelNotes.Read(_input.PlayerEntity).Count;
            yield return InspectSign();
            yield return InspectItem(_items[0], "weapon", "examine_item",
                "Damage: 1d10", "Penetration bonus: +2", "Attributes: Cutting Axe", "Serrated: 20%");
            yield return InspectItem(_items[1], "armor", "examine_item",
                "AV: +2", "DV: +0", "Speed: -5", "Equip slots: Feet");
            yield return InspectItem(_items[2], "tonic", "examine_tonic", "4d6+4");
            Check("inspection_spends_no_turn_hp_effect_or_item", _input.TurnManager.TickCount == ticks
                && _input.PlayerEntity.GetStatValue("Hitpoints") == hp && (_input.PlayerEntity.GetPart<StatusEffectsPart>()?.EffectCount ?? 0) == effects
                && _items.All(e => _input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(e)
                    && (e.GetPart<StackerPart>()?.StackCount ?? 1) == counts[e]));
            Check("inspection_does_not_generate_or_record_notes", manager.CachedZoneCount == cached
                && RegionalTravelNotes.Read(_input.PlayerEntity).Count == notes);
            Check("normal_input_restored_after_descriptions", State() == "Normal"
                && !_input.InventoryUI.IsOpen && !_input.AnnouncementUI.IsOpen);
        }

        private void StageFactoryContent()
        {
            _stagedZone = _input.CurrentZone;
            var at = _stagedZone.GetEntityPosition(_input.PlayerEntity);
            Cell place = null;
            foreach (var offset in new[] { (x: 1, y: 0), (x: -1, y: 0), (x: 0, y: -1), (x: 0, y: 1) })
            {
                var cell = _stagedZone.GetCell(at.x + offset.x, at.y + offset.y);
                if (cell != null && !cell.BlocksMovement() && !cell.Objects.Any(e => !WorldInteractionSystem.IsTerrain(e)))
                { place = cell; break; }
            }
            Require(place != null, "clear cardinal neighbor for an explicit signpost fixture");
            _sign = _context.Factory.CreateEntity("Signpost");
            Require(_stagedZone.AddEntity(_sign, place.X, place.Y), "factory signpost placement");
            foreach (string blueprint in new[] { "WarlordCleaver", "IronshodBoots" })
            {
                var item = _context.Factory.CreateEntity(blueprint);
                Require(item != null && _input.PlayerEntity.GetPart<InventoryPart>().AddObject(item), "stage " + blueprint);
                _items.Add(item);
            }
            // Normal new-game loadout already contains two healing tonics.
            // Inspect that actual starter stack, avoiding AddObject's merge of
            // an injected duplicate into an object with a different identity.
            var tonic = _input.PlayerEntity.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == "HealingTonic");
            Require(tonic != null && (tonic.GetPart<StackerPart>()?.StackCount ?? 1) > 0, "real starter healing tonic");
            _items.Add(tonic);
            var serrated = new EnhancementSerrated(); serrated.ApplyTier(2); _items[0].AddPart(serrated);
            MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("DensityExamineNativeFixture");
            Diag.Record("scenario", "DensityExamineNativeStaging", payload: new
            { runId = RunId, zone = _stagedZone.ZoneID, signX = place.X, signY = place.Y,
                stagedItemBlueprints = _items.Take(2).Select(e => e.BlueprintName).ToArray(),
                starterTonic = tonic.BlueprintName, starterTonicCount = tonic.GetPart<StackerPart>()?.StackCount ?? 1,
                enhancement = "Serrated tier 2", naturalAcquisition = false });
        }

        private IEnumerator InspectSign()
        {
            var player = _stagedZone.GetEntityPosition(_input.PlayerEntity);
            var sign = _stagedZone.GetEntityPosition(_sign);
            yield return Tap(Key.L); Require(State() == "LookMode", "native L opens look mode");
            yield return Tap(sign.x > player.x ? Key.D : sign.x < player.x ? Key.A : sign.y > player.y ? Key.S : Key.W);
            yield return Tap(Key.Enter);
            Require(State() == "WorldActionMenuOpen", "native Enter opens world actions");
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, _sign)
                && !_input.WorldActionMenuUI.SelectedCellIsPile, "the native menu targets the staged signpost");
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == "Examine");
            Require(index >= 0, "signpost exposes Examine");
            Check("world_sign_single_examine", actions.Count(a => a.Command == "Examine") == 1);
            var shortcut = MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString();
            Require(Enum.TryParse(shortcut, out Key key), "native world Examine shortcut");
            yield return Tap(key);
            string text = MessageLog.GetLast();
            _descriptions.Add(new Description { subject = "Signpost", source = "staged world fixture", text = text });
            Check("world_sign_directions_reached_through_keys", text.Contains("Carved directions:")
                && text.Split(new[] { "world-map cells." }, StringSplitOptions.None).Length - 1 == 4
                && !text.Contains("Local lead:"));
            Require(State() == "LookMode" || State() == "Normal", "world Examine returns to ordinary view");
            yield return Capture("signpost-log-latest");
            // The sign's complete output may span the small sidebar log. Use
            // its real =/- controls; capture overlapping portions, then restore.
            int prior = _input.ZoneRenderer.SidebarLogScrollOffsetRows;
            for (int step = 0; step < 40; step++)
            {
                yield return Tap(Key.Equals);
                int current = _input.ZoneRenderer.SidebarLogScrollOffsetRows;
                if (current == prior) break;
                prior = current;
                if (step % 4 == 3) yield return Capture("signpost-log-older-" + current);
            }
            if (prior > 0) yield return Capture("signpost-log-oldest");
            for (int step = 0; _input.ZoneRenderer.SidebarLogScrollOffsetRows > 0; step++)
            { Require(step < 40, "bounded sidebar restore"); yield return Tap(Key.Minus); }
            Check("world_log_scroll_restored", _input.ZoneRenderer.SidebarLogScrollOffsetRows == 0);
            if (State() == "LookMode") yield return Tap(Key.Escape);
            Require(State() == "Normal", "native Escape returns from look mode");
        }

        private IEnumerator InspectItem(Entity item, string label, string command, params string[] required)
        {
            yield return Tap(Key.I); Require(State() == "InventoryOpen", "native I opens inventory");
            yield return Tap(Key.Tab); Require(InvField<int>("_panel") == 1, "native Tab selects the item list");
            int row = RowIndex(item); Require(row >= 0, "staged item has a real inventory row");
            for (int step = 0; InvField<int>("_cursorIndex") != row; step++)
            { Require(step < 80, "bounded item navigation"); yield return Tap(InvField<int>("_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            object popup = InvField<object>("_itemActionPopup"); Require(popup != null, "native item action popup");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            var examine = actions.Select((a, i) => (index: i, label: (string)Field(a, "Label"), command: (string)Field(a, "Command")))
                .Where(a => string.Equals(a.label, "Examine", StringComparison.OrdinalIgnoreCase)).ToArray();
            Check(label + "_single_examine", examine.Length == 1 && examine[0].command == command);
            Require(examine.Length == 1 && examine[0].index < 9, "one native Examine shortcut before navigation-key collisions");
            yield return Capture(label + "-actions");
            yield return Tap((Key)Enum.Parse(typeof(Key), ((char)('A' + examine[0].index)).ToString()));
            double began = Time.realtimeSinceStartupAsDouble;
            while (State() != "AnnouncementOpen")
            { Require(Time.realtimeSinceStartupAsDouble - began < 5, "native inventory announcement"); yield return null; }
            string text = (string)Field(_input.AnnouncementUI, "_message") ?? "";
            _descriptions.Add(new Description { subject = item.BlueprintName, source = label == "tonic" ? "designed starter loadout" : "staged equipment fixture", text = text, units = item.GetPart<StackerPart>()?.StackCount ?? 1 });
            Check(label + "_description_visible", _input.AnnouncementUI.IsOpen && required.All(text.Contains));
            yield return Capture(label + "-description");
            yield return Tap(Key.Enter);
            Check(label + "_returns_to_same_row", State() == "InventoryOpen" && InvField<object>("_itemActionPopup") == null
                && InvField<int>("_panel") == 1 && InvField<int>("_cursorIndex") == row);
            yield return Tap(Key.I); Require(State() == "Normal", "native I closes inventory");
        }

        private int RowIndex(Entity item)
        {
            var rows = (IList)Field(_input.InventoryUI, "_rows");
            for (int i = 0; i < rows.Count; i++)
                if (ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i], "Item"))?.Item, item)) return i;
            return -1;
        }
        private T InvField<T>(string name) => (T)Field(_input.InventoryUI, name);
        private string State() => Field(_input, "_inputState").ToString();
        private static object Field(object owner, string name)
        {
            var member = owner.GetType().GetField(name, Private | BindingFlags.Public);
            if (member == null) throw new InvalidOperationException("Missing observed field " + owner.GetType().Name + "." + name);
            return member.GetValue(owner);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds < 150, "finite native examination deadline");
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - began < 3, "input rate gate reopens"); yield return null; }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "screenshot " + name);
            _screenshots.Add(path);
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density examine precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _failures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "DensityExamineNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityExamineNative] " + error); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        public void SetUnexpectedErrors(int errors)
        { _unexpectedErrors = errors; _errorsFinalized = true; WriteReport(); EmitSummary(); }
        public void Abort(string reason)
        { if (Finished) return; StopAllCoroutines(); _fatal = reason; Check("native_aborted", false); Finish(); }
        private void Finish() { Cleanup(); Finished = true; WriteReport(); }
        private void EmitSummary()
        {
            if (_summaryEmitted) return; _summaryEmitted = true;
            Diag.Record("scenario", "DensityExamineNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && _audit.Count == 16 && _screenshots.Count >= 7;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                zone = _stagedZone?.ZoneID, fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = "Native N bootstrap; actual keyboard look/world-action and inventory/action/announcement paths; one visible Examine per item; expected live description strings; same-row return and no turn/HP/effect/item cost; no destination generation or travel note; screenshot files.",
                cannotVerify = "Explicitly staged factory Signpost, WarlordCleaver with Serrated tier 2 and IronshodBoots in an owned disposable game; HealingTonic is the real designed starter stack. No natural acquisition, placement frequency, balance or ordinary progression claim. Reflection observes UI fields only. Screenshots require separate visual inspection for legibility/orientation. Unexpected errors count Application.logMessageReceived only; native backend messages require separate console review."
            }, true));
            Debug.Log("[DensityExamineNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
            if (_sign != null) _stagedZone?.RemoveEntity(_sign);
            var inventory = _input?.PlayerEntity?.GetPart<InventoryPart>();
            foreach (var item in _items.Take(2)) if (inventory?.Objects.Contains(item) == true) inventory.RemoveObject(item);
        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        [Serializable] private sealed class Description
        { public string subject, source, text; public int units; }
        [Serializable] private sealed class Report
        {
            public string runId, zone, fatal, canVerify, cannotVerify;
            public string[] audit, screenshots;
            public Description[] descriptions;
            public int cases, failures, unexpectedErrors;
            public bool complete, errorsFinalized;
            public double seconds;
        }
    }
}
