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
    /// <summary>Finite native keyboard evidence for content-completion. Explicitly
    /// uses actual generated stock in an isolated disposable new game. It Travel shortcuts reposition the unchanged actor; purchases and actions use real keys.</summary>
    public sealed class DensityCompletionNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<Description> _descriptions = new List<Description>();
        private InputHandler _input;
        private ScenarioContext _context;
        private Zone _stagedZone;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors;
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/NativeEveryday", RunId));

        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
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
            Require(boot != null && boot.IsActive, "owned new-game menu");
            yield return Tap(Key.N);
            Require(!boot.IsActive && State() == "Normal", "native N starts disposable game");
            var manager = _input.ZoneManager as OverworldZoneManager;
            Require(manager != null && manager.WorldSeed == 64 && !DevMode.Enabled, "ordinary rules and owned seed");
            _stagedZone = _input.CurrentZone;
            int originalMaxHp = _input.PlayerEntity.GetStat("Hitpoints").Max;
            var morrowfast = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
            var seller = MorrowfastSceneRuntime.FindOwner(morrowfast, "southern-food-vendor");
            Require(seller != null && seller.BlueprintName == "MorrowfastSella", "actual authored Sella");
            var skin = seller.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "Waterskin");
            yield return Buy(seller, skin, "waterskin");
            var well = _input.CurrentZone.GetAllEntities().FirstOrDefault(e => e.HasPart<WellPart>());
            Require(well != null, "authored renewable well");
            PositionForAudit(well.SpatialZone, well);
            int ticks = _input.TurnManager.TickCount; int energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(skin, "FillWaterskin");
            Check("fill_spends_one_turn_and_fills_three", State() == "Normal"
                && SingleTurnSince(ticks, energy) && skin.GetPart<WaterskinPart>().Charges == 3);
            ticks = _input.TurnManager.TickCount; energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(skin, "FillWaterskin");
            Check("full_fill_refusal_is_free", State() == "InventoryOpen"
                && _input.TurnManager.TickCount == ticks && skin.GetPart<WaterskinPart>().Charges == 3);
            yield return Tap(Key.Escape); if (State() == "InventoryOpen") yield return Tap(Key.I);
            Require(State() == "Normal", "return from refused action");
            ticks = _input.TurnManager.TickCount; energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(skin, "DrinkWaterskin");
            Check("drink_spends_one_turn_preserves_skin", State() == "Normal"
                && SingleTurnSince(ticks, energy) && skin.GetPart<WaterskinPart>().Charges == 2
                && _input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(skin));
            yield return Capture("water-and-clock");

            // Source zone generation is ordinary. Positioning shortcuts only travel;
            // no items, stats, source rates or NPC schedules are changed by this audit.
            var forageZone = manager.GetZone(FellingSiteBuilder.ZoneID);
            var ring = FellingScenePopulation.FindDressingOwner(forageZone, "mushroom-pink-tall");
            Require(ring != null && ring.BlueprintName == "MushroomRing", "authored forage source");
            yield return WorldAction(ring, "Harvest");
            var food = _input.PlayerEntity.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Mushroom");
            int foodUnits = food.GetPart<StackerPart>()?.StackCount ?? 1;
            Check("harvested_actual_source", foodUnits >= 2 && foodUnits <= 4);
            if (State() == "LookMode") yield return Tap(Key.Escape);
            var sill = manager.GetZone(WorldMap.StartingZoneID);
            var fire = sill.GetAllEntities().FirstOrDefault(e => e.HasPart<CampfirePart>());
            Require(fire != null, "generated Sill cooking fire");
            PositionForAudit(sill, fire);
            int beforeCook = Count("RoastedMushroom"); ticks = _input.TurnManager.TickCount; energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(food, "Cook");
            Check("cooks_actual_harvested_stack_in_one_turn", State() == "Normal"
                && SingleTurnSince(ticks, energy) && Count("RoastedMushroom") == beforeCook + foodUnits
                && !_input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(food));
            yield return Capture("cooked-supplies");
            var scribe = sill.GetAllEntities().FirstOrDefault(e => e.BlueprintName == "Scribe");
            Require(scribe != null, "generated Sill scribe");
            var book = scribe.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Codex04");
            yield return Buy(scribe, book, "canonical-copy");
            ticks = _input.TurnManager.TickCount; energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return ItemAction(book, "ReadDocument");
            double began = Time.realtimeSinceStartupAsDouble;
            while (State() != "AnnouncementOpen")
            { Require(Time.realtimeSinceStartupAsDouble - began < 5, "native Read opens document"); yield return null; }
            var doc = ReadableDocumentCatalog.Get("codex-04");
            Check("natural_shop_book_has_full_canonical_text", (string)Field(_input.AnnouncementUI, "_message") == doc.Title + "\n\n" + doc.Text);
            int pages = _input.AnnouncementUI.PageCount;
            Require(pages > 1, "actual found text exercises pagination");
            var all = new List<string>();
            for (int page = 0; page < pages; page++)
            {
                all.AddRange(_input.AnnouncementUI.VisibleLines);
                yield return Capture("document-page-" + (page + 1));
                if (page + 1 < pages) yield return Tap(Key.RightArrow);
            }
            var wrapped = (List<string>)Field(_input.AnnouncementUI, "_wrappedLines");
            Check("all_pages_reached_with_keyboard", all.SequenceEqual(wrapped));
            yield return Tap(Key.LeftArrow);
            Check("keyboard_previous_page", _input.AnnouncementUI.VisibleLines.SequenceEqual(wrapped.Skip((pages - 2) * 33).Take(33)));
            yield return Capture("document-previous-page");
            yield return Tap(Key.Escape);
            Check("read_is_free_and_returns_inventory", State() == "InventoryOpen"
                && _input.TurnManager.TickCount == ticks && _input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(book));
            yield return Tap(Key.I);
            Check("ordinary_stats_and_no_tinkering_grant", _input.PlayerEntity.GetStat("Hitpoints").Max == originalMaxHp
                && !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>());
            Check("normal_input_restored", State() == "Normal" && !_input.AnnouncementUI.IsOpen && !_input.InventoryUI.IsOpen);
        }

        private bool SingleTurnSince(int ticks, int energy)
        {
            int elapsed = _input.TurnManager.TickCount - ticks;
            int after = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            int speed = _input.PlayerEntity.GetStatValue("Speed", TurnManager.DefaultSpeed);
            _descriptions.Add(new Description { subject = "turn-cost", source = "scheduler energy accounting", text =
                "ticks=" + elapsed + "; before=" + energy + "; after=" + after + "; speed=" + speed });
            return after == energy - TurnManager.ActionThreshold + elapsed * speed;
        }

        private int Count(string blueprint) => _input.PlayerEntity.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        private void PositionForAudit(Zone zone, Entity target)
        {
            Require(State() == "Normal", "positioning occurs outside menus");
            var player = _input.PlayerEntity;
            Cell chosen = null;
            foreach (var occupied in zone.GetOccupiedCells(target))
                foreach (var d in new[] { (x: 1, y: 0), (x: -1, y: 0), (x: 0, y: -1), (x: 0, y: 1), (x: 1, y: 1), (x: 1, y: -1), (x: -1, y: 1), (x: -1, y: -1) })
                {
                    var c = zone.GetCell(occupied.X + d.x, occupied.Y + d.y);
                    if (c != null && !c.BlocksMovement(player) && !c.Objects.Any(e => e.HasTag("Trap"))) { chosen = c; break; }
                }
            Require(chosen != null, "legal adjacent standing space: " + target.BlueprintName);
            var old = _input.CurrentZone;
            Require(old.TryTransferEntityTo(player, zone, chosen.X, chosen.Y), "audit travel shortcut preserves actor");
            if (!ReferenceEquals(old, zone))
                typeof(InputHandler).GetMethod("HandleZoneTransition", Private).Invoke(_input, new object[] {
                    new ZoneTransitionResult { Success = true, NewZone = zone, NewPlayerX = chosen.X, NewPlayerY = chosen.Y } });
            _input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("DensityCompletionAuditTravel");
            MessageLog.Clear();
            _descriptions.Add(new Description { subject = target.BlueprintName, source = "generated source; actor travel shortcut to " + zone.ZoneID, text = target.ID });
        }

        private IEnumerator WorldAction(Entity target, string command)
        {
            PositionForAudit(target.SpatialZone, target);
            var from = _input.CurrentZone.GetEntityCell(_input.PlayerEntity);
            var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
            yield return Tap(Key.L); Require(State() == "LookMode", "native look");
            if (to.X != from.X) yield return Tap(to.X > from.X ? Key.D : Key.A);
            if (to.Y != from.Y) yield return Tap(to.Y > from.Y ? Key.S : Key.W);
            yield return Tap(Key.Enter);
            Require(State() == "WorldActionMenuOpen" && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "real source world menu " + target.BlueprintName);
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == command); Require(index >= 0, command + " action");
            string shortcut = MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString();
            yield return Tap((Key)Enum.Parse(typeof(Key), shortcut));
        }

        private IEnumerator Buy(Entity seller, Entity item, string label)
        {
            int coins = TradeSystem.GetDrams(_input.PlayerEntity);
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(_input.PlayerEntity), seller);
            Require(coins >= price, "ordinary starting money affords " + label + ": " + coins + " / " + price);
            yield return WorldAction(seller, "Chat");
            Require(ConversationManager.IsActive, "actual authored conversation");
            if ((bool)Field(_input.DialogueUI, "_revealing")) yield return Tap(Key.Enter);
            int trade = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions?.Any(a => a.Key == "StartTrade") == true);
            Require(trade >= 0, "real trade choice");
            yield return Tap((Key)Enum.Parse(typeof(Key), MenuShortcutMap.Key(MenuShortcutMap.Positional(trade)).ToString()));
            Require(_input.TradeUI.IsOpen, "native trade UI");
            var rows = (IList)Field(_input.TradeUI, "_leftRows");
            int row = -1; for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Field(rows[i], "Item"), item)) row = i;
            Require(row >= 0, "generated item offered for sale");
            for (int step = 0; (int)Field(_input.TradeUI, "_leftCursor") != row; step++)
            { Require(step < 80, "bounded purchase navigation"); yield return Tap(Key.DownArrow); }
            yield return Capture(label + "-actual-stock");
            yield return Tap(Key.Enter); yield return Tap(Key.Enter);
            Check(label + "_bought_actual_instance", _input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(item)
                && !seller.GetPart<InventoryPart>().Objects.Contains(item) && TradeSystem.GetDrams(_input.PlayerEntity) == coins - price);
            yield return Tap(Key.Escape); Require(State() == "Normal", "trade closes to play");
        }

        private IEnumerator ItemAction(Entity item, string command)
        {
            yield return Tap(Key.I); Require(State() == "InventoryOpen", "native I opens inventory");
            yield return Tap(Key.Tab); Require(InvField<int>("_panel") == 1, "native item list");
            int row = RowIndex(item); Require(row >= 0, "owned item row");
            for (int step = 0; InvField<int>("_cursorIndex") != row; step++)
            { Require(step < 80, "bounded item navigation"); yield return Tap(InvField<int>("_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            object popup = InvField<object>("_itemActionPopup"); Require(popup != null, "native item actions");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            int action = Array.FindIndex(actions, a => (string)Field(a, "Command") == command);
            Require(action >= 0 && action < 9, "native action shortcut " + command);
            yield return Tap((Key)Enum.Parse(typeof(Key), ((char)('A' + action)).ToString()));
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
            Require(_clock.Elapsed.TotalSeconds < 240, "finite native content-completion deadline");
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
        { if (!condition) throw new InvalidOperationException("Density completion precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _failures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "DensityCompletionNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityCompletionNative] " + error); break; }
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
            Diag.Record("scenario", "DensityCompletionNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && _audit.Count == 13 && _screenshots.Count >= 7;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                zone = _stagedZone?.ZoneID, fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = "Native new game; actual generated Sella/Scribe stock bought through Chat/Trade keyboard UI and authored Felling mushrooms harvested; ordinary starting money and HP; real well/fire fill/drink/cook actions; action turns/refusal; canonical book read with previous/next keys; screenshots.",
                cannotVerify = "Actor positioning shortcuts travel to legal cells beside already generated sources; no ordinary walking route, natural encounter pacing, combat balance or frequency claim. No source rates, inventory, money, HP or NPC scheduling are altered. Screenshots require visual inspection."
            }, true));
            Debug.Log("[DensityCompletionNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;

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
