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
    /// <summary>Finite keyboard audit of actual purchase/ability readers. The original Classic
    /// player receives a documented controlled rite/book/visible-mark fixture; all UI uses native keys.</summary>
    public sealed class AbilityClarityNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized; int failures, errors; string fatal;
        SpellCastReliabilityBench spellBench; bool spellBenchCompleted, readerContextPreserved;
        Entity target; Rites_HangingBolt rite; GrimoireChargePart ink; System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (spellBench?.Failures ?? 0)
            + (Finished && (!spellBenchCompleted || !readerContextPreserved) ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/SkillsEngagement/NativeReaders", runId));
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
            Check("finite_visible_mark_fixture", ink.Charges > 0 && target.HasEffect<WetEffect>() && CombatIntentReadout.IsVisibleActor(target, input.CurrentZone));

            yield return Tap(Key.X); Require(State == "SkillsScreenOpen", "native X menu");
            var skillRows = SkillsScreenStateBuilder.Build(Player).Rows;
            int desired = skillRows.ToList().FindIndex(r => r.Class == "Pyromancy_HeartFlame"); Require(desired >= 0, "purchase row present");
            yield return SelectRow(() => input.SkillsScreenUI.CursorIndex, desired);
            int skillsScroll = (int)Field(input.SkillsScreenUI, "_scrollOffset");
            yield return Tap(Key.D); Require(State == "AnnouncementOpen", "native D purchase reader");
            string purchaseText = ReadAllPages(); SkillRegistry.TryGetPowerByClass("Pyromancy_HeartFlame", out var power);
            Check("full_purchase_prose", Normalize(purchaseText).Contains(Normalize(power.Description)) && purchaseText.Contains("Cooldown:"));
            yield return Capture("01-purchase-details"); yield return Tap(Key.Escape);
            Check("purchase_reader_restores_selection", State == "SkillsScreenOpen" && input.SkillsScreenUI.CursorIndex == desired
                && (int)Field(input.SkillsScreenUI, "_scrollOffset") == skillsScroll && Snapshot() == before);
            yield return Tap(Key.Escape); yield return Tap(Key.M); Require(State == "AbilityManagerOpen", "native M menu");
            int row = AbilityManagerStateBuilder.Build(Player).Rows.ToList().FindIndex(r => r.AbilityID == rite.ActivatedAbilityID);
            yield return SelectRow(() => input.AbilityManagerUI.CursorIndex, row); int abilityScroll = (int)Field(input.AbilityManagerUI, "_scrollOffset");
            yield return Tap(Key.D); Require(State == "AnnouncementOpen", "native D ability reader");
            string details = ReadAllPages(); Check("owned_details_live_spec_and_ink", details.Contains("Range: 6") && details.Contains("Cooldown: 30") && details.Contains("ink remaining"));
            yield return Capture("02-ability-details"); yield return Tap(Key.Escape);
            Check("ability_reader_restores_selection", State == "AbilityManagerOpen" && input.AbilityManagerUI.CursorIndex == row
                && (int)Field(input.AbilityManagerUI, "_scrollOffset") == abilityScroll && Snapshot() == before);
            yield return Tap(Key.P); Require(State == "AwaitingRitePreviewDirection", "optional P direction"); yield return Tap(Key.RightArrow);
            Require(State == "AnnouncementOpen", "native direction opens reading only");
            string preview = ReadAllPages(); Check("actual_visible_marks_previewed", preview.Contains(target.GetDisplayName()) && preview.Contains("Spend: Wet") && preview.Contains("2 turns"));
            yield return Capture("03-visible-rite-preview");
            Check("preview_is_read_only", Snapshot() == before);
            yield return Tap(Key.Escape); Check("preview_returns_to_manager", State == "AbilityManagerOpen" && input.AbilityManagerUI.CursorIndex == row);
            // Ordinary activation still commits to its usual direction chooser immediately.
            yield return Tap(Key.Enter); Require(State == "AwaitingDirection", "ordinary activation remains immediate"); yield return Tap(Key.Escape);
            Check("fast_cast_path_has_no_new_confirmation", State == "Normal" && Snapshot() == before);
            // Separate controlled command/scheduler/save proof, after all ordinary reader checks.
            spellBench = new SpellCastReliabilityBench();
            spellBench.Apply(new ScenarioContext(input.CurrentZone, input.EntityFactory, Player, input.TurnManager));
            spellBenchCompleted = spellBench.Cases == 6;
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
            target = input.EntityFactory.CreateEntity("MarlbackGleaner"); Require(target != null, "actual target blueprint");
            var brain = target.GetPart<BrainPart>(); if (brain != null) target.RemovePart(brain);
            Require(zone.AddEntity(target, 22, 12), "visible target position"); Require(target.ApplyEffect(new WetEffect()), "actual wet mark");
            var skills = Player.GetPart<SkillsPart>(); Require(skills != null, "starter skills");
            Require(skills.AddSkill(new Rites_HangingBolt(), "scenario:clarity-reader"), "fixture rite learned"); rite = Player.GetPart<Rites_HangingBolt>();
            var book = input.EntityFactory.CreateEntity("HangingBoltGrimoire"); Require(book != null && Player.GetPart<InventoryPart>().AddObject(book), "finite fixture book");
            ink = book.GetPart<GrimoireChargePart>(); Require(ink != null, "ink source");
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("AbilityClarityNativeFixture");
            // The UI observer fixture deliberately establishes current visibility without spending a turn.
            for (int x = 17; x <= 25; x++) for (int y = 9; y <= 15; y++) { var cell = zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        }
        string Snapshot() => JsonConvert.SerializeObject(new { tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player), hp = Player.GetStatValue("Hitpoints"),
            sp = Player.GetStatValue("SP"), targetHp = target.GetStatValue("Hitpoints"), ink = ink.Charges, wet = target.GetPart<StatusEffectsPart>().GetAllEffects().OfType<WetEffect>().Single().Moisture,
            abilities = Player.GetPart<ActivatedAbilitiesPart>().AbilityList.Select(a => new { a.ID, a.CooldownRemaining }).ToArray(),
            slots = Player.GetPart<ActivatedAbilitiesPart>().SlotAssignments, skills = Player.GetPart<SkillsPart>().SkillList.Select(s => s.Name).ToArray() });
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
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == 10 && spellBenchCompleted && readerContextPreserved,
                failures = Failures, readerFailures = failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots,
                spellBenchmark = new { runId = spellBench?.RunId, cases = spellBench?.Cases ?? 0, failures = spellBench?.Failures ?? 0,
                    complete = spellBenchCompleted && spellBench.Failures == 0, checks = spellBench?.Audit, readerContextPreserved },
                fixture = "Ordinary N/Classic start with original stats. Isolated starting zone flattened to floor, player repositioned; stationary actual factory target at east2 with Wet, one granted Hanging Bolt skill and original finite charged grimoire, bounded visibility set. All reader/open/close/navigation/preview/activation inputs use native keyboard; copied text inspection uses public reader paging.",
                canVerify = "Actual X/M/D/P/direction/Escape/Enter input, complete prose, actual range/cooldown/ink and visible mark text, row+scroll restoration, no turn/energy/HP/SP/ink/cooldown/effect/binding changes, unchanged quick-cast direction flow.",
                cannotVerify = "Controlled UI fixture does not prove ordinary rite discovery, enemy choices, balance, novice understanding or visual readability without independent screenshot inspection. Final spell damage is deliberately not predicted. The separate six-case spell benchmark uses direct command/scheduler/save fixtures, not native player input."
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
