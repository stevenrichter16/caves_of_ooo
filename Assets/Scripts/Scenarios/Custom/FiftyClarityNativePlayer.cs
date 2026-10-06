using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite keyboard witness for inventory decisions and free readers. Controlled finite
    /// fixtures use ordinary menu input; the trade host entry is explicitly distinguished.</summary>
    public sealed class FiftyClarityNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized; int failures, errors; string fatal;
        MealPreparationBench mealBench; bool mealBenchCompleted, readerContextPreserved;
        FiftyCombatRuntimeBench combatBench; bool combatBenchCompleted;
        FiftyWorldBench worldBench; bool worldBenchCompleted;
        Entity food, easy, heavy, trader, stock; System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (mealBench?.Failures ?? 0) + (combatBench?.Failures ?? 0) + (worldBench?.Failures ?? 0)
            + (Finished && (!mealBenchCompleted || !combatBenchCompleted || !worldBenchCompleted || !readerContextPreserved) ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/FiftyImprovements/NativeClarity", runId));
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
            yield return Tap(Key.N); var menu = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(menu.IsOpen, "build picker"); int classic = menu.Model.Options.ToList().FindIndex(b => b.Id == "classic");
            Require(classic >= 0 && classic < 9, "Classic authored build"); yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (classic + 1)));
            yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(.3f);
            Require(State == "Normal", "normal gameplay");
            Check("ordinary_classic_start", Player.GetProperty(StartingBuildService.PropertyName) == "classic" && !DebugInvincibility.IsEnabled(Player));
            BuildFixture(); var before = Snapshot();
            Check("controlled_finite_fixture", Player.GetPart<InventoryPart>().Contains(food) && input.CurrentZone.GetEntityCell(easy) == input.CurrentZone.GetEntityCell(Player));
            yield return Tap(Key.F1); Require(State == "AnnouncementOpen", "native F1 controls");
            string controls = ReadAllPages(); Check("full_controls_reader", ControlsReference.Bindings.All(row => Normalize(controls).Contains(Normalize(row.What))));
            yield return Capture("01-controls"); yield return Tap(Key.Escape);
            Check("controls_free_return", State == "Normal" && Snapshot() == before);
            yield return Tap(Key.Tab); Require(input.PauseMenuUI.IsOpen, "native pause menu");
            var pause = input.PauseMenuUI.Controller;
            yield return SelectRow(() => pause.SelectedIndex, PauseMenuController.ControlsIndex);
            yield return Tap(Key.Enter); Require(State == "AnnouncementOpen", "native pause controls reader");
            yield return Capture("01b-pause-controls"); yield return Tap(Key.Escape);
            Check("pause_controls_returns_free_to_same_selection", State == "Normal" && pause.IsOpen
                && pause.SelectedIndex == PauseMenuController.ControlsIndex && Snapshot() == before);
            yield return Tap(Key.Tab); Require(!pause.IsOpen && State == "Normal", "native pause close");
            yield return Tap(Key.I); Require(State == "InventoryOpen", "native inventory");
            yield return Tap(Key.F2); Require(State == "AnnouncementOpen", "native F2 effects");
            Check("live_current_effects", ReadAllPages().Contains("Heart Flame") && Snapshot() == before);
            yield return Capture("02-current-effects"); yield return Tap(Key.Escape);
            yield return Tap(Key.Tab); yield return Tap(Key.Slash);
            foreach (char c in "emberwheat") yield return Tap((Key)Enum.Parse(typeof(Key), c.ToString().ToUpperInvariant()));
            yield return Tap(Key.Enter);
            Check("literal_native_inventory_search", input.InventoryUI.SearchQuery == "emberwheat" && !input.InventoryUI.IsSearching
                && input.InventoryUI.SelectedDecisionDetails()?.Contains(food.GetDisplayName()) == true);
            int selected = (int)Field(input.InventoryUI, "_cursorIndex");
            yield return Tap(Key.F1); Require(State == "AnnouncementOpen", "native F1 food"); string details = ReadAllPages();
            Check("food_healing_and_meal_details", Normalize(details).Contains(Normalize(FoodPart.DescribeMeal(food.GetPart<FoodPart>()))) && details.Contains("HP missing"));
            yield return Capture("03-prepared-food"); yield return Tap(Key.Escape);
            Check("inventory_reader_keeps_search_and_selection", State == "InventoryOpen" && input.InventoryUI.SearchQuery == "emberwheat"
                && (int)Field(input.InventoryUI, "_cursorIndex") == selected && Snapshot() == before);
            yield return Tap(Key.Escape); Require(State == "InventoryOpen", "Escape clears search first");
            yield return Tap(Key.Escape); Require(State == "Normal", "second Escape closes inventory");
            yield return Tap(Key.G); Require(State == "PickupOpen", "native G opens loose stack list");
            yield return Tap(Key.F1); Require(State == "AnnouncementOpen", "native loot reader");
            Check("loot_can_be_read_before_taking", ReadAllPages().Contains("Total weight") && Snapshot() == before);
            yield return Tap(Key.Escape); yield return Tap(Key.Tab); Require(State == "PickupOpen", "partial Take All remains open");
            Check("partial_take_all_retains_reason", Player.GetPart<InventoryPart>().Contains(easy) && !Player.GetPart<InventoryPart>().Contains(heavy)
                && ((string)Field(input.PickupUI, "_statusMessage")).Contains("Strength"));
            yield return Tap(Key.F1); Require(State == "AnnouncementOpen", "remaining load full reader"); yield return Capture("04-partial-loot");
            yield return Tap(Key.Escape); yield return Tap(Key.Escape); Require(State == "Normal", "leave loot");
            // Controlled stock opens the ordinary trade screen through its existing host entry.
            // F1 and Escape below are real keyboard input; this is not a dialogue-discovery claim.
            typeof(InputHandler).GetMethod("OpenTrade", Fields).Invoke(input, new object[] { trader });
            var tradeBefore = Snapshot(); yield return Tap(Key.F1); Require(State == "AnnouncementOpen", "native trade reader");
            string quote = ReadAllPages(); Check("whole_stack_trade_quote", quote.Contains("3 units") && quote.Contains("Drams after") && quote.Contains(stock.GetDisplayName()));
            yield return Capture("05-trade-quote"); yield return Tap(Key.Escape);
            Check("trade_reader_free_return", State == "TradeOpen" && Snapshot() == tradeBefore);
            yield return Tap(Key.Escape); Require(State == "Normal", "leave trade");
            before = Snapshot();
            mealBench = new MealPreparationBench();
            mealBench.Apply(new ScenarioContext(input.CurrentZone, input.EntityFactory, Player, input.TurnManager));
            mealBenchCompleted = mealBench.Cases == 20;
            combatBench = new FiftyCombatRuntimeBench();
            combatBench.Apply(new ScenarioContext(input.CurrentZone, input.EntityFactory, Player, input.TurnManager));
            combatBenchCompleted = combatBench.Cases == FiftyCombatRuntimeBench.ExpectedCases;
            worldBench = new FiftyWorldBench();
            worldBench.Apply(new ScenarioContext(input.CurrentZone, input.EntityFactory, Player, input.TurnManager));
            worldBenchCompleted = worldBench.Cases == 15;
            readerContextPreserved = Snapshot() == before && State == "Normal";
            WriteReport();
        }
        void BuildFixture()
        {
            var zone = input.CurrentZone;
            foreach (var entity in zone.GetAllEntities().ToArray())
                if (entity != Player) { input.TurnManager.RemoveEntity(entity); zone.RemoveEntity(entity); }
            zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            { zone.TileState.Clear(x, y); if (x > 0 && y > 0 && x < Zone.Width - 1 && y < Zone.Height - 1) zone.AddEntity(input.EntityFactory.CreateEntity("StoneFloor"), x, y); }
            Require(zone.MoveEntity(Player, 20, 12), "fixture player placement");
            food = input.EntityFactory.CreateEntity("ToastedEmberwheat"); Require(Player.GetPart<InventoryPart>().AddObject(food), "prepared food fixture");
            Require(Player.ApplyEffect(new HeartFlameEffect(2, 9)), "status fixture");
            easy = input.EntityFactory.CreateEntity("SalvagedTimber"); easy.GetPart<RenderPart>().DisplayName = "loose timber";
            heavy = input.EntityFactory.CreateEntity("PhysicalObject"); heavy.SetTag("Item");
            heavy.GetPart<RenderPart>().DisplayName = "heavy test load"; heavy.GetPart<RenderPart>().RenderString = "=";
            heavy.GetPart<PhysicsPart>().Takeable = true; heavy.GetPart<PhysicsPart>().Weight = 500;
            if (heavy.GetPart<HandlingPart>() != null) heavy.RemovePart(heavy.GetPart<HandlingPart>());
            heavy.AddPart(new HandlingPart { Weight = 500, Carryable = true, Throwable = false });
            Require(zone.AddEntity(easy, 20, 12) && zone.AddEntity(heavy, 20, 12), "two loose load fixtures");
            trader = input.EntityFactory.CreateEntity("Player"); stock = input.EntityFactory.CreateEntity("Dagger");
            stock.GetPart<StackerPart>().StackCount = 3; stock.GetPart<RenderPart>().DisplayName = "three trade daggers";
            Require(trader.GetPart<InventoryPart>().AddObject(stock), "finite stock fixture");
            TradeSystem.SetDrams(Player, 1000);
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("FiftyClarityNativeFixture");
            for (int x = 17; x <= 25; x++) for (int y = 9; y <= 15; y++) { var cell = zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        }
        string Snapshot() => JsonConvert.SerializeObject(new { tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player), hp = Player.GetStatValue("Hitpoints"),
            drams = TradeSystem.GetDrams(Player), food = food?.GetPart<StackerPart>()?.StackCount,
            pack = Player.GetPart<InventoryPart>().Objects.Select(e => new { e.ID, count = e.GetPart<StackerPart>()?.StackCount }).ToArray(),
            effects = Player.GetPart<StatusEffectsPart>().GetAllEffects().Select(e => new { e.ClassName, e.Duration }).ToArray() });
        IEnumerator SelectRow(Func<int> cursor, int wanted)
        { Require(wanted >= 0, "selected row exists"); int guard = 150; while (cursor() != wanted && guard-- > 0) yield return Tap(cursor() < wanted ? Key.DownArrow : Key.UpArrow); Require(cursor() == wanted, "native navigation reached row"); }
        string ReadAllPages()
        {
            var ui = input.AnnouncementUI; var text = new List<string>();
            // This observes all copied reader text; page navigation here is an API observation, not native-key evidence.
            for (int i = 0; i < ui.PageCount; i++) { ui.GoToPage(i); text.AddRange(ui.VisibleLines); }
            ui.GoToPage(0); string result = string.Join("\n", text); observations.Add(new { state = State, pages = ui.PageCount, text = result, snapshot = Snapshot() }); return result;
        }
        static string Normalize(string s) => string.Join(" ", (s ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.12f); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.06f);
        }
        IEnumerator Capture(string name)
        { yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); Directory.CreateDirectory(DirectoryPath); string path = Path.Combine(DirectoryPath, name + ".png"); DensityNativeScreenshot.CaptureToFile(path); screenshots.Add(path); WriteReport(); }
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
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == 13 && mealBenchCompleted && combatBenchCompleted && worldBenchCompleted && readerContextPreserved,
                failures = Failures, readerFailures = failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots,
                mealBenchmark = new { runId = mealBench?.RunId, cases = mealBench?.Cases ?? 0, failures = mealBench?.Failures ?? 0,
                    complete = mealBenchCompleted && mealBench.Failures == 0, checks = mealBench?.Audit, readerContextPreserved },
                combatBenchmark = new { runId = combatBench?.RunId, cases = combatBench?.Cases ?? 0, failures = combatBench?.Failures ?? 0,
                    complete = combatBenchCompleted && combatBench.Failures == 0, checks = combatBench?.Audit, observations = combatBench?.Observations },
                worldBenchmark = new { runId = worldBench?.RunId, cases = worldBench?.Cases ?? 0, failures = worldBench?.Failures ?? 0,
                    complete = worldBenchCompleted && worldBench.Failures == 0, checks = worldBench?.Audit },
                fixture = "Ordinary N/Classic start with original stats. Isolated starting zone flattened and player repositioned; finite cooked food, Heart Flame status, light timber and strength-refused test load, controlled three-dagger trader stock and a 1000-dram purse. All reader/search/loot inputs use native keys; the trade host entry is invoked directly. Copied page inspection uses public paging.",
                canVerify = "Native F1/F2/I/search/G/Tab/Escape flow, full current effects and food/meal information, whole-stack quote, partial pickup remaining row/reason, return selection and no reader action costs. Separate meal/combat/world benchmarks have 20/12/15 actual command/owner-clock/save checks.",
                cannotVerify = "Controlled fixtures do not prove ordinary item discovery, dialogue discovery, balance, novice comprehension or visual readability without independent screenshot inspection. The meal/combat/world benchmarks are detached direct command/scheduler/save evidence, not native keyboard food/combat/resource use. The world growth witness deliberately advances its detached clock; it does not prove ordinary exploration or waiting experience."

            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings); Application.runInBackground = oldBackground;
        }
        void OnDestroy() => Cleanup();
    }
}
