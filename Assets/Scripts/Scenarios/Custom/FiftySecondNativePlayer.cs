using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite isolated native-key witness plus detached gameplay benches.
    /// Controlled supplies and host entries do not establish ordinary discovery.</summary>
    public sealed class FiftySecondNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>(), benchmarks = new List<object>();
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized, benchesComplete; int failures, errors; string fatal;
        Entity timber, scribe, locker, screen, book, hood; EntityFactory oldExplorationFactory;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (Finished && !benchesComplete ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/FiftyImprovementsII/Native", runId));
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated save launcher");
            clock = System.Diagnostics.Stopwatch.StartNew(); oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings; oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldExplorationFactory = SecondExplorationActions.Factory;
            oldKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(RunSafely(Run()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f); input = FindFirstObjectByType<InputHandler>(); Require(input != null, "ordinary bootstrap");
            Require(((BootMenuController)Field(input, "_bootMenuController")).IsActive, "new-game menu");
            yield return Tap(Key.N); var build = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(build.IsOpen, "build picker"); int classic = build.Model.Options.ToList().FindIndex(b => b.Id == "classic");
            Require(classic >= 0 && classic < 9, "Classic authored build"); yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (classic + 1)));
            yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(.3f);
            Require(State == "Normal", "normal gameplay");
            Check("ordinary_classic_start", Player.GetProperty(StartingBuildService.PropertyName) == "classic" && !DebugInvincibility.IsEnabled(Player));
            BuildFixture(); yield return Capture("01-controlled-exploration-owners");
            int tick = input.TurnManager.TickCount;
            yield return Tap(Key.I); Require(State == "InventoryOpen", "native inventory entry");
            yield return InventoryChoice(timber, "PrepareRecipe|shape_haft", "02-field-haft-recipe");
            Check("native_field_recipe_spends_material_and_one_action", Count("SalvagedTimber") == 0 && Count("FieldHaftComponent") == 1 && input.TurnManager.TickCount == tick + 10);
            tick = input.TurnManager.TickCount; int purse = TradeSystem.GetDrams(Player);
            yield return WorldChoice(scribe, "CopyVolume|" + Uri.EscapeDataString(book.ID), "03-selected-copy-service");
            Check("native_scribe_copies_selected_book_and_pays_once", Count("GrimoireCopy") == 1 && Count("InkVial") == 0
                && Player.GetPart<InventoryPart>().Contains(book) && TradeSystem.GetDrams(Player) == purse - 5 && input.TurnManager.TickCount == tick + 10);
            tick = input.TurnManager.TickCount;
            yield return WorldChoice(locker, "ClaimGuestLocker", "04-guest-locker");
            Check("native_guest_claim_opens_real_chest_once", !locker.GetPart<ContainerPart>().IsLocked && input.TurnManager.TickCount == tick + 10);
            tick = input.TurnManager.TickCount;
            yield return WorldChoice(screen, "StripClothScreen", "05-cover-or-cord");
            Check("native_screen_reclamation_changes_cover_and_supply", input.CurrentZone.GetEntityCell(screen) == null && Count("KnotflaxCord") == 2 && input.TurnManager.TickCount == tick + 10);
            // Exact ordinary equipment service; attachment output is separately covered by native EditMode tests.
            Require(InventorySystem.Equip(Player, hood), "real filter hood Body binding");
            ZoneRenderHooks.MarkFullDirty("FiftySecondNative.Equipped");
            yield return Capture("06-worn-hood-and-open-space");
            string before = Snapshot();
            foreach (string name in new[] { "FiftySecondCombatBench", "FiftySecondPreparationBench", "FiftySecondExplorationBench" })
            {
                Type type = typeof(FiftySecondNativePlayer).Assembly.GetType("CavesOfOoo.Scenarios.Custom." + name);
                Require(type != null && typeof(IScenario).IsAssignableFrom(type), "compiled benchmark " + name);
                object bench = Activator.CreateInstance(type);
                ((IScenario)bench).Apply(new ScenarioContext(input.CurrentZone, input.EntityFactory, Player, input.TurnManager));
                int expected = (int)type.GetField("ExpectedCases", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                int cases = (int)Read(bench, "Cases"), failed = (int)Read(bench, "Failures");
                benchmarks.Add(new { name, runId = Read(bench, "RunId"), expected, cases, failed, audit = Read(bench, "Audit"), observations = Read(bench, "Observations") });
                Check(name, cases == expected && failed == 0);
                Check(name + "_preserves_campaign_context", Snapshot() == before && State == "Normal");
            }
            benchesComplete = benchmarks.Count == 3;
            WriteReport();
        }
        int Count(string bp) => Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == bp).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        void BuildFixture()
        {
            var zone = input.CurrentZone;
            foreach (var entity in zone.GetAllEntities().ToArray())
                if (entity != Player) { input.TurnManager.RemoveEntity(entity); zone.RemoveEntity(entity); }
            zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            { zone.TileState.Clear(x, y); if (x > 0 && y > 0 && x < Zone.Width - 1 && y < Zone.Height - 1) zone.AddEntity(input.EntityFactory.CreateEntity("StoneFloor"), x, y); }
            Require(zone.MoveEntity(Player, 20, 12), "controlled player placement");
            SecondExplorationActions.Factory = input.EntityFactory;
            Entity Carry(string bp) { var e = input.EntityFactory.CreateEntity(bp); Require(e != null && Player.GetPart<InventoryPart>().AddObject(e), "finite carried " + bp); return e; }
            Entity Place(string bp, int x, int y) { var e = input.EntityFactory.CreateEntity(bp); Require(e != null && zone.AddEntity(e, x, y), "finite placed " + bp); return e; }
            timber = Carry("SalvagedTimber"); Carry("DryingBreezeGrimoire"); book = Carry("WardGleamGrimoire"); Carry("InkVial"); hood = Carry("FilterHood");
            scribe = Place("Scribe", 21, 12); if (!scribe.HasPart<ScribeCopyServicePart>()) scribe.AddPart(new ScribeCopyServicePart());
            locker = Place("WellmeetGuestLocker", 20, 13); screen = Place("FrontierClothScreen", 19, 12);
            Player.ApplyEffect(new UnderTheClothEffect()); TradeSystem.SetDrams(Player, 10);
            Place("FrontierPenGate", 18, 10); Place("CounterStoreChest", 22, 10); Place("QuillholdLoanShelf", 23, 12);
            Place("ColdwardCloak", 18, 14); Place("AcidworkerApron", 19, 14); Place("CausticFilm", 23, 14);
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("FiftySecondNative.ControlledFixture");
            for (int x = 16; x <= 25; x++) for (int y = 8; y <= 16; y++) { var cell = zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        }
        IEnumerator InventoryChoice(Entity item, string command, string capture)
        {
            // Public exact-owner host entry; row navigation and execution use real keyboard events.
            Require(input.InventoryUI.ReopenItemActionPopupFor(item), "inventory host entry");
            object popup = Field(input.InventoryUI, "_itemActionPopup"); var actions = (IList)Field(popup, "Actions");
            int wanted = -1; for (int i = 0; i < actions.Count; i++) if ((string)Field(actions[i], "Command") == command) wanted = i;
            Require(wanted >= 0, "inventory command " + command);
            yield return SelectRow(() => (int)Field(popup, "CursorIndex"), wanted); yield return Capture(capture); yield return Tap(Key.Enter);
            Require(State == "Normal", "paid inventory action returns to play");
        }
        IEnumerator WorldChoice(Entity owner, string command, string capture)
        {
            var returnState = typeof(InputHandler).GetField("_worldActionMenuReturnState", Fields); returnState.SetValue(input, Enum.Parse(returnState.FieldType, "Normal"));
            typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Fields).Invoke(input, new object[] { owner, input.CurrentZone.GetEntityCell(owner), false });
            var menu = input.WorldActionMenuUI; Require(menu.IsOpen, "world host entry");
            var actions = (List<InventoryAction>)Field(menu, "_actions"); int wanted = actions.FindIndex(a => a.Command == command);
            Require(wanted >= 0, "world command " + command);
            yield return SelectRow(() => (int)Field(menu, "_cursorIndex"), wanted); yield return Capture(capture); yield return Tap(Key.Enter);
            Require(State == "Normal", "paid world action returns to play");
        }
        string Snapshot() => JsonConvert.SerializeObject(new { tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player), hp = Player.GetStatValue("Hitpoints"),
            drams = TradeSystem.GetDrams(Player), zone = input.CurrentZone.ZoneID, player = Player.ID,
            pack = Player.GetPart<InventoryPart>().Objects.Select(e => new { e.ID, count = e.GetPart<StackerPart>()?.StackCount }).ToArray(),
            effects = Player.GetPart<StatusEffectsPart>().GetAllEffects().Select(e => new { e.ClassName, e.Duration }).ToArray() });
        IEnumerator SelectRow(Func<int> cursor, int wanted)
        { Require(wanted >= 0, "selected row exists"); int guard = 150; while (cursor() != wanted && guard-- > 0) yield return Tap(cursor() < wanted ? Key.DownArrow : Key.UpArrow); Require(cursor() == wanted, "native navigation reached row"); }
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.12f); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.06f);
        }
        IEnumerator Capture(string name)
        { yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); Directory.CreateDirectory(DirectoryPath); string path = Path.Combine(DirectoryPath, name + ".png"); DensityNativeScreenshot.CaptureToFile(path); screenshots.Add(path); observations.Add(new { name, state = State, snapshot = Snapshot() }); WriteReport(); }
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
        static object Read(object owner, string name) => owner.GetType().GetProperty(name, Fields)?.GetValue(owner) ?? owner.GetType().GetField(name, Fields)?.GetValue(owner);
        public void SetUnexpectedErrors(int count) { errors = count; errorsFinalized = true; WriteReport(); }
        public void Abort(string reason) { fatal = reason; failures++; StopAllCoroutines(); Cleanup(); Finished = true; WriteReport(); }
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonConvert.SerializeObject(new {
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == 11 && benchesComplete,
                failures = Failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots, benchmarks,
                fixture = "Ordinary N/Classic start with original stats. Isolated zone flattened, player repositioned, finite timber/books/ink/hood and 10 drams supplied; local scribe, guest-right, locker, screen and model examples placed. Public or existing reflected host entries open exact-owner menus; navigation and confirmation use native keys. Hood uses ordinary equipment service, not native keys.",
                canVerify = "Four real native-key command selections, explicit material/payment consequences and one-action timing, six rendered captures, plus detached combat/preparation/exploration command, scheduler and save observations. Benchmarks must preserve the player context.",
                cannotVerify = "Controlled supplies do not prove ordinary source discovery, dialogue discovery, difficulty, long-term enjoyment or novice comprehension. Benchmarks are direct gameplay evidence, not keyboard combat. Pixel quality requires separate screenshot inspection."
            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            SecondExplorationActions.Factory = oldExplorationFactory;
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings); Application.runInBackground = oldBackground;
        }
        void OnDestroy() => Cleanup();
    }
}
