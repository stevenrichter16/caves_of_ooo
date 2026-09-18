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
    public sealed class MorrowfastScenePlayAudit : MonoBehaviour
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
        private int _nativeSteps, _clearActions, _clearTicks, _faunaExamined, _dressingActions, _faunaWaits, _examinedOwners, _doorsEntered;
        private string _containerId, _produceId;
        private (int x, int y) _movedStool;
        private readonly string[] _questIds = { "MorrowfastReturnNotch", "MorrowfastBell", "MorrowfastSupper" };
        private System.Diagnostics.Stopwatch _elapsed;
        private MorrowfastSceneDefinition _definition;
        private const int ProfileCapacity = 4096;
        private readonly string[] _profileNames = { "COO.ZoneRenderer.LateUpdate", "Main Thread", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders;
        private double _profileStarted, _profileSeconds;
        private Metric[] _profileMetrics = Array.Empty<Metric>();
        private string ReportDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/MorrowfastScene"));

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
                    Debug.LogError("[MorrowfastScenePlayAudit] " + failure);
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
            Require(!boot.IsActive && State() == "Normal", "Native New game starts ordinary input.");
            Check("ordinary_start_outside_morrowfast", _input.CurrentZone.ZoneID != MorrowfastSceneRuntime.ZoneID);
            Check("owned_save_root_precedes_bootstrap", SaveGameService.SaveRootOverride == _root);
            yield return Tap(Key.LeftShift, Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID), "Native < enters the world map.");
            var target = WorldMap.WorldCellToZoneCell(FellingSiteBuilder.WorldX, FellingSiteBuilder.WorldY);
            var start = Position();
            Check("ordinary_route_seven_west_five_north", start.x - target.Item1 == 7 && start.y - target.Item2 == 5);
            while (Position().x > target.Item1) yield return Step(Key.A, (-1, 0));
            while (Position().x < target.Item1) yield return Step(Key.D, (1, 0));
            while (Position().y > target.Item2) yield return Step(Key.W, (0, -1));
            while (Position().y < target.Item2) yield return Step(Key.S, (0, 1));
            yield return Tap(Key.LeftShift, Key.Period);
            Require(FellingSceneRuntime.IsActive(_input.CurrentZone), "Native world-map descent reaches Felling first.");
            yield return WalkTo((40, 24));
            yield return CrossEdge(Key.S, MorrowfastSceneRuntime.ZoneID, (40, 0));
            yield return WaitForScene();
            _definition = MorrowfastSceneDefinition.Load();
            Require(_definition.owners.Length == 71 && _definition.buildings.Length == 5, "Full authored owner and building contract.");
            Check("all_71_source_owners_initially_live", _definition.owners.All(o => Owner(o.id) != null && MorrowfastSceneRuntime.IsPresent(_input.CurrentZone, o.id)));
            Check("native_art_has_all_71_owners", Presenter().ComponentCount == 71 && Presenter().PresentationVisible);
            yield return Capture(PreviewOnly ? "native-preview.png" : "native-initial.png");
            yield return Tap(Key.F5);
            Require(SaveGameService.HasQuickSave(), "Native F5 created the first isolated checkpoint.");
            _gameId = SaveGameService.GetSaveInfo("Quick").GameID;
            Check("initial_checkpoint_is_owned", OwnedCheckpoint());
            if (PreviewOnly) yield break;

            StartProfile();
            yield return AuditDoors();
            yield return AuditRoofs();
            yield return ExamineAllSourceOwners();
            yield return AuditNativeResidents();
            yield return AuditWaterAndPantry();
            yield return AuditNativeDropAndPickup();
            yield return AuditTrade("southwest-craftsperson");
            yield return AuditTrade("southern-food-vendor");
            yield return AuditQuestsAndRest();
            yield return Clear("provisioners-stall");
            var identities = _input.CurrentZone.GetReadOnlyEntities().Where(e => e.HasPart<BrainPart>()).Select(e => e.ID).OrderBy(x => x).ToArray();
            var inventory = InventorySnapshot();
            int drams = TradeSystem.GetDrams(_input.PlayerEntity);
            yield return Capture("native-changed.png");
            yield return Tap(Key.F5); yield return Tap(Key.F6); yield return WaitForScene();
            Check("native_save_reload_keeps_owned_checkpoint", OwnedCheckpoint());
            CheckPersistentState("native_save_reload", identities, inventory, drams);
            yield return WalkTo((40, 0));
            yield return CrossEdge(Key.W, FellingSiteBuilder.ZoneID, (40, 24));
            Check("native_north_edge_returns_to_live_felling", FellingSceneRuntime.IsActive(_input.CurrentZone));
            yield return CrossEdge(Key.S, MorrowfastSceneRuntime.ZoneID, (40, 0));
            yield return WaitForScene();
            CheckPersistentState("native_leave_and_reenter", identities, inventory, drams);
            yield return WalkTo((40, 24));
            Check("north_to_south_road_remains_walkable_after_changes", Position() == (40, 24));
            Check("all_71_source_owners_examined_by_exact_native_menu", _examinedOwners == 71);
            Check("all_five_interiors_entered_by_native_movement", _doorsEntered == 5);
            Check("final_save_scope_remains_private", SaveGameService.SaveRootOverride == _root && OwnedCheckpoint());
            yield return Capture("native-final.png");
        }
        private IEnumerator CrossEdge(Key key, string expectedZone, (int x, int y) expectedPosition)
        {
            Require(State() == "Normal", "Normal input is required for edge travel.");
            string before = _input.CurrentZone.ZoneID;
            yield return Tap(key); _nativeSteps++;
            Require(_input.CurrentZone.ZoneID == expectedZone && Position() == expectedPosition,
                "Native edge " + key + " from " + before + " must reach " + expectedZone + " at " + expectedPosition + "; actual " + _input.CurrentZone.ZoneID + " " + Position());
            Check("native_edge_" + before + "_to_" + expectedZone, true);
        }
        private IEnumerator AuditDoors()
        {
            foreach (var b in _definition.buildings)
            {
                var door = Owner(b.doorId); Require(door != null, "Live door " + b.doorId);
                yield return ApproachEntity(door.ID); var outside = Position();
                Check("door_initially_closed_" + b.id, !MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone, b.doorId));
                yield return OpenEntityMenu(door); yield return SelectDoorCommand(true);
                Check("door_opens_actual_state_" + b.id, MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone, b.doorId));
                var targets = new HashSet<(int, int)>(b.interior.Select(c => (c.x, c.y)));
                var path = FindPath(targets, false); Require(path != null && path.Count > 0, "Door opens a reachable interior: " + b.id);
                yield return Follow(path, targets);
                Check("player_enters_room_" + b.id, targets.Contains(Position()) && Presenter().IsRoomRevealed(b.id));
                _doorsEntered++;
                yield return Capture("native-room-" + b.id + ".png");
                yield return WalkTo(outside);
                yield return OpenEntityMenu(Owner(b.doorId)); yield return SelectDoorCommand(false);
                Check("door_closes_after_exit_" + b.id, !MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone, b.doorId));
                yield return OpenEntityMenu(Owner(b.doorId)); yield return SelectDoorCommand(true);
            }
        }
        private IEnumerator SelectDoorCommand(bool open)
        {
            string desired = open ? "OpenMorrowfastDoor" : "CloseMorrowfastDoor";
            yield return SelectCommand(desired);
        }
        private IEnumerator ExamineAllSourceOwners()
        {
            var remaining = new HashSet<string>(_definition.owners.Select(o => o.id));
            int blockedWaits = 0;
            while (remaining.Count > 0)
            {
                string selected = null; int length = int.MaxValue;
                foreach (string id in remaining)
                {
                    var owner = Owner(id); Require(owner != null, "Source owner survived before its examination: " + id);
                    var path = FindPath(InteractionTargets(owner), false);
                    if (path != null && path.Count < length) { selected = id; length = path.Count; }
                }
                if (selected == null)
                {
                    Require(remaining.Any(id => FindPath(InteractionTargets(Owner(id)), false, true) != null),
                        "No remaining owner has a terrain route; the blockage is not solely mobile occupancy.");
                    Require(++blockedWaits <= 12 && _faunaWaits < 24, "Bounded native waiting for a temporarily occupied owner route.");
                    _faunaWaits++; yield return Tap(Key.Period); continue;
                }
                blockedWaits = 0;
                var entity = Owner(selected); yield return Examine(entity, selected); remaining.Remove(selected); _examinedOwners++;
            }
        }
        private IEnumerator AuditRoofs()
        {
            foreach (var b in _definition.buildings)
            {
                var roof = Owner(b.roofId); yield return ApproachEntity(roof.ID); yield return OpenEntityMenu(roof);
                yield return SelectCommand(MorrowfastPropPart.RoofCommand);
                Check("manual_roof_lifts_" + b.id, MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone, b.roofId) && Presenter().IsRoomRevealed(b.id));
                yield return OpenEntityMenu(roof); yield return SelectCommand(MorrowfastPropPart.RoofCommand);
                Check("manual_roof_restores_" + b.id, !MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone, b.roofId));
            }
            var persisted = Owner(_definition.buildings[0].roofId); yield return ApproachEntity(persisted.ID); yield return OpenEntityMenu(persisted);
            yield return SelectCommand(MorrowfastPropPart.RoofCommand);
        }
        private IEnumerator Examine(Entity entity, string label)
        {
            yield return ApproachEntity(entity.ID);
            int tick = _input.TurnManager.TickCount;
            yield return OpenEntityMenu(entity); int serial = MessageLog.NextSerialValue;
            yield return SelectCommand("Examine");
            string description = entity.GetPart<ExaminablePart>()?.Text;
            Check("native_examine_" + label, MessageLog.GetAllEntries().Any(e => e.Serial >= serial
                && e.Text.StartsWith("You see", StringComparison.Ordinal) && e.Text.Contains(entity.GetDisplayName())
                && (string.IsNullOrWhiteSpace(description) || e.Text.Contains(description.Trim())))
                && _input.TurnManager.TickCount == tick && _input.CurrentZone.GetEntityCell(entity) != null);
        }
        private IEnumerator AuditNativeResidents()
        {
            foreach (string id in new[] { "north-guard-west", "north-guard-east", "western-shopkeeper", "southwest-craftsperson", "southern-food-vendor", "east-robed-resident", "farra-sprig", "edden-brack" })
            {
                var resident = Owner(id); Require(resident != null && resident.HasPart<BrainPart>() && resident.HasPart<ConversationPart>(), "Living conversational resident " + id);
                yield return ApproachEntity(resident.ID); yield return OpenEntityMenu(resident); yield return SelectCommand("Chat");
                Check("native_resident_dialogue_" + id, State() == "DialogueOpen" && ReferenceEquals(ConversationManager.Speaker, resident) && ConversationManager.VisibleChoices.Count > 0);
                yield return Tap(Key.Escape); Require(State() == "Normal", "Escape returns from ordinary dialogue.");
            }
            foreach (string id in new[] { "western-bank-frog", "southern-tortoise" })
            {
                var animal = Owner(id); Require(animal != null && animal.HasPart<BrainPart>() && animal.GetStatValue("Hitpoints") > 0, "Real creature owner " + id);
                yield return Examine(animal, "living_" + id);
                Check("creature_uses_native_source_view_" + id, Presenter().IsRenderedEntity(animal)); _faunaExamined++;
            }
            int before = _input.TurnManager.TickCount; yield return Tap(Key.Period);
            Check("native_wait_advances_living_world", _input.TurnManager.TickCount > before);
        }
        private IEnumerator AuditWaterAndPantry()
        {
            var cistern = Owner("central-cistern"); yield return ApproachEntity(cistern.ID); yield return OpenEntityMenu(cistern);
            int serial = MessageLog.NextSerialValue, drams = TradeSystem.GetDrams(_input.PlayerEntity); var beforeWater = InventorySnapshot();
            yield return SelectCommand("DrawWaterAtWell");
            Check("cistern_draws_free_native_water", MessageLog.GetAllEntries().Any(e => e.Serial >= serial && (e.Text.Contains("water") || e.Text.Contains("drink")))
                && TradeSystem.GetDrams(_input.PlayerEntity) == drams && InventorySnapshot() == beforeWater && !_input.PlayerEntity.HasEffect<ParchedEffect>());
            var container = Owner("western-shop-crate");
            Require(container != null, "At least one real stocked source container.");
            _containerId = _definition.owners.Single(o => ReferenceEquals(Owner(o.id), container)).id;
            var grants = container.GetPart<ContainerPart>().Contents.GroupBy(e => e.BlueprintName).ToDictionary(g => g.Key, g => g.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            var before = grants.Keys.ToDictionary(x => x, InventoryUnits);
            yield return ApproachEntity(container.ID); yield return OpenEntityMenu(container); yield return SelectCommand("OpenContainer");
            Require(State() == "PickupOpen", "Native container loot popup opens."); yield return Tap(Key.Tab);
            Check("native_loot_transfers_every_container_unit", container.GetPart<ContainerPart>().Contents.Count == 0 && grants.All(g => InventoryUnits(g.Key) == before[g.Key] + g.Value));
            if (State() != "Normal") yield return Tap(Key.Escape);
            _produceId = "kitchen-produce-barrel-north"; var produce = Owner(_produceId);
            Require(produce?.GetPart<ContainerPart>()?.Contents.Count > 0, "The kitchen has actual native pantry contents.");
            int food = InventoryUnits("Mushroom"), supply = produce.GetPart<ContainerPart>().Contents.Where(e => e.BlueprintName == "Mushroom").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            Require(supply > 0, "A real edible pantry item is present.");
            yield return ApproachEntity(produce.ID); yield return OpenEntityMenu(produce); yield return SelectCommand("OpenContainer");
            Require(State() == "PickupOpen", "Native pantry loot popup opens."); yield return Tap(Key.Tab);
            if (State() != "Normal") yield return Tap(Key.Escape);
            Check("native_pantry_loot_transfers_actual_food", InventoryUnits("Mushroom") == food + supply && produce.GetPart<ContainerPart>().Contents.Count == 0 && Owner(_produceId) == produce);
        }
        private IEnumerator AuditTrade(string id)
        {
            var trader = Owner(id); yield return ApproachEntity(trader.ID); yield return OpenEntityMenu(trader); yield return SelectCommand("Chat");
            yield return ChooseAction("StartTrade", null); Require(State() == "TradeOpen", "Native dialogue opens real shop " + id);
            var rows = (IList)typeof(TradeUI).GetField("_leftRows", Private).GetValue(_input.TradeUI);
            int row = -1, price = 0; Entity item = null;
            for (int i = 0; i < rows.Count; i++)
            {
                object candidate = rows[i]; var t = candidate.GetType(); int p = (int)t.GetField("Price").GetValue(candidate);
                var e = (Entity)t.GetField("Item").GetValue(candidate);
                if (p <= TradeSystem.GetDrams(_input.PlayerEntity) && (row < 0 || p < price)) { row = i; price = p; item = e; }
            }
            Require(row >= 0 && item != null, "Ordinary starting funds afford an actual stock item.");
            string blueprint = item.BlueprintName; int units = InventoryUnits(blueprint), drams = TradeSystem.GetDrams(_input.PlayerEntity);
            int boughtUnits = item.GetPart<StackerPart>()?.StackCount ?? 1;
            int stock = TradeSystem.GetTraderStock(trader).Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
            for (int i = 0; i < row; i++) yield return Tap(Key.DownArrow);
            yield return Tap(Key.Enter); Require((bool)typeof(TradeUI).GetField("_confirmActive", Private).GetValue(_input.TradeUI), "Real buy confirmation."); yield return Tap(Key.Enter);
            Check("native_buy_transfers_item_and_currency_" + id, InventoryUnits(blueprint) == units + boughtUnits && TradeSystem.GetDrams(_input.PlayerEntity) == drams - price
                && TradeSystem.GetTraderStock(trader).Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1) == stock - boughtUnits);
            yield return Tap(Key.Tab); rows = (IList)typeof(TradeUI).GetField("_rightRows", Private).GetValue(_input.TradeUI); row = -1;
            int soldUnits = 0;
            for (int i = 0; i < rows.Count; i++) { var e = (Entity)rows[i].GetType().GetField("Item").GetValue(rows[i]); if (e.BlueprintName == blueprint) { row = i; soldUnits = e.GetPart<StackerPart>()?.StackCount ?? 1; price = (int)rows[i].GetType().GetField("Price").GetValue(rows[i]); break; } }
            Require(row >= 0, "Purchased item appears in the real sell panel."); drams = TradeSystem.GetDrams(_input.PlayerEntity);
            for (int i = 0; i < row; i++) yield return Tap(Key.DownArrow);
            yield return Tap(Key.Enter); yield return Tap(Key.Enter);
            Check("native_sell_transfers_item_and_currency_" + id, InventoryUnits(blueprint) == units + boughtUnits - soldUnits && TradeSystem.GetDrams(_input.PlayerEntity) == drams + price);
            yield return Tap(Key.Escape); Require(State() == "Normal", "Native trade Escape closes shop.");
        }
        private IEnumerator ChooseAction(string key, string value)
        {
            Require(State() == "DialogueOpen", "Native choice needs a real dialogue.");
            var rows = ConversationManager.VisibleChoices;
            int index = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Actions?.Any(a => a.Key == key && (value == null || a.Value == value)) == true) { index = i; break; }
            Require(index >= 0, "Visible native choice with action " + key + "=" + value);
            var ui = _input.DialogueUI;
            if ((bool)typeof(DialogueUI).GetField("_revealing", Private).GetValue(ui)) yield return Tap(Key.Enter);
            int cursor = (int)typeof(DialogueUI).GetField("_cursorIndex", Private).GetValue(ui);
            for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
            for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
        }
        private IEnumerator AuditQuestsAndRest()
        {
            yield return TalkAction("north-guard-east", "What is the unfinished notch?", "accept-return");
            Check("return_notch_accepted_in_real_journal", CavesOfOoo.Storylets.StoryletPart.Current?.IsQuestActive(MorrowfastQuests.ReturnQuestId) == true);
            yield return TalkAction("edden-brack", "What would you like Hesta told?", "eddens-account");
            Check("edden_consents_to_the_actual_account", _input.PlayerEntity.GetIntProperty(MorrowfastQuests.EddenConsent) == 1);
            yield return TalkAction("north-guard-east", null, "report-return");
            Check("return_notch_completed", CavesOfOoo.Storylets.StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.ReturnQuestId));

            yield return TalkAction("north-guard-west", "Why is your bell so muffled?", "accept-bell");
            var cord = Owner("rope-reserve-coil"); yield return ApproachEntity(cord.ID); yield return OpenEntityMenu(cord); yield return SelectCommand(MorrowfastQuests.InspectCordCommand);
            Check("native_cord_inspection_records_evidence", _input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed) == 1);
            var arch = Owner("north-oath-arch"); yield return ApproachEntity(arch.ID); yield return OpenEntityMenu(arch);
            string supplies = InventorySnapshot(); int drams = TradeSystem.GetDrams(_input.PlayerEntity);
            yield return SelectCommand(MorrowfastQuests.LoudBellCommand);
            Check("bare_bell_is_real_free_world_state", arch.GetProperty("MorrowfastBellMode") == "loud" && InventorySnapshot() == supplies && TradeSystem.GetDrams(_input.PlayerEntity) == drams);
            yield return OpenEntityMenu(arch); int serial = MessageLog.NextSerialValue; yield return SelectCommand(MorrowfastQuests.RingBellCommand);
            Check("native_bell_rings_in_world", MessageLog.GetAllEntries().Any(e => e.Serial >= serial && e.Text.Contains("bell") && e.Text.Contains("Morrowfast")));
            yield return TalkAction("north-guard-west", null, "report-bell");
            Check("bell_work_completed", CavesOfOoo.Storylets.StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.BellQuestId));

            yield return TalkAction("farra-sprig", "Does the supper room need a hand?", "accept-supper");
            var stool = Owner("guest-stool-south"); yield return ApproachEntity(stool.ID); var old = _input.CurrentZone.GetEntityPosition(stool);
            yield return OpenEntityMenu(stool); yield return SelectCommand(MorrowfastQuests.MoveStoolCommand);
            _movedStool = _input.CurrentZone.GetEntityPosition(stool);
            Check("native_supper_work_moves_real_furniture", _movedStool != old && _input.PlayerEntity.GetIntProperty(MorrowfastQuests.SupperMoved) == 1);
            yield return TalkAction("farra-sprig", null, "report-supper");
            Check("supper_room_completed", CavesOfOoo.Storylets.StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.SupperQuestId));

            var bed = Owner("guest-bed-west"); yield return ApproachEntity(bed.ID); yield return OpenEntityMenu(bed);
            int tick = _input.TurnManager.TickCount; drams = TradeSystem.GetDrams(_input.PlayerEntity);
            yield return SelectCommand(MorrowfastQuests.RestCommand);
            Check("guest_bed_rest_advances_real_clock_for_free", _input.TurnManager.TickCount - tick == RestSystem.RestClockTurns
                && TradeSystem.GetDrams(_input.PlayerEntity) == drams && !_input.PlayerEntity.HasEffect<BleedingEffect>());
        }
        private IEnumerator TalkAction(string ownerId, string topic, string command)
        {
            var resident = Owner(ownerId); yield return ApproachEntity(resident.ID); yield return OpenEntityMenu(resident); yield return SelectCommand("Chat");
            if (topic != null) yield return ChooseText(topic);
            yield return ChooseAction("MorrowfastAction", command);
            if (State() == "DialogueOpen") yield return Tap(Key.Escape);
            Require(State() == "Normal", "Quest conversation returns to ordinary input: " + command);
        }
        private IEnumerator ChooseText(string text)
        {
            Require(State() == "DialogueOpen", "A real dialogue is open for topic selection.");
            var rows = ConversationManager.VisibleChoices; int index = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Text == text) { index = i; break; }
            Require(index >= 0, "Visible native dialogue topic: " + text);
            var ui = _input.DialogueUI;
            if ((bool)typeof(DialogueUI).GetField("_revealing", Private).GetValue(ui)) yield return Tap(Key.Enter);
            int cursor = (int)typeof(DialogueUI).GetField("_cursorIndex", Private).GetValue(ui);
            for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
            for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
        }
        private IEnumerator AuditNativeDropAndPickup()
        {
            string before = InventorySnapshot(); yield return Tap(Key.I); Require(State() == "InventoryOpen", "Native inventory opens."); yield return Tap(Key.Tab);
            var rows = (IList)typeof(InventoryUI).GetField("_rows", Private).GetValue(_input.InventoryUI); int selected = -1; Entity item = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var display = rows[i].GetType().GetField("Item").GetValue(rows[i]);
                if (display == null) continue;
                item = (Entity)display.GetType().GetField("Item").GetValue(display); if (item != null) { selected = i; break; }
            }
            Require(item != null && selected >= 0, "Real carried item available for a reversible drop/pickup.");
            for (int step = 0; step < rows.Count + 1; step++)
            {
                int cursor = (int)typeof(InventoryUI).GetField("_cursorIndex", Private).GetValue(_input.InventoryUI);
                if (cursor == selected) break;
                Require(cursor < selected, "Inventory selection stays on the intended row."); yield return Tap(Key.DownArrow);
            }
            int units = item.GetPart<StackerPart>()?.StackCount ?? 1, count = InventoryUnits(item.BlueprintName);
            yield return Tap(Key.D);
            Check("native_inventory_drop_places_real_item", _input.CurrentZone.GetEntityCell(item) != null && InventoryUnits(item.BlueprintName) == count - units);
            yield return Tap(Key.Escape); Require(State() == "Normal", "Inventory closes before native pickup."); yield return Tap(Key.G);
            if (State() == "PickupOpen")
            {
                var items = (List<Entity>)typeof(PickupUI).GetField("_items", Private).GetValue(_input.PickupUI); int index = items.FindIndex(e => e.ID == item.ID);
                Require(index >= 0, "Dropped individual item appears in the actual pickup popup.");
                int cursor = (int)typeof(PickupUI).GetField("_cursorIndex", Private).GetValue(_input.PickupUI);
                for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
                for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
                yield return Tap(Key.Enter); if (State() == "PickupOpen") yield return Tap(Key.Escape);
            }
            Check("native_pickup_restores_exact_inventory_units", State() == "Normal" && InventorySnapshot() == before && _input.CurrentZone.GetEntityCell(item) == null);
        }
        private IEnumerator Clear(string id)
        {
            var owner = Owner(id); Require(owner != null, "Live clear fixture " + id); yield return ApproachEntity(owner.ID);
            var spec = _definition.owners.Single(o => o.id == id);
            var blocked = spec.footprint.Where(c => _input.CurrentZone.GetCell(c.x, c.y).BlocksMovement(_input.PlayerEntity)).ToArray();
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(_input.PlayerEntity), speed = _input.TurnManager.GetSpeed(_input.PlayerEntity);
            int expectedTicks = (int)Math.Max(0L, (2L * TurnManager.ActionThreshold - energy + speed - 1) / speed);
            yield return OpenEntityMenu(owner); yield return SelectCommand(MorrowfastPropPart.ClearCommand);
            int charged = _input.TurnManager.TickCount - tick; _clearTicks += charged;
            Check("clear_spends_exactly_one_native_action_" + id, charged == expectedTicks && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy - TurnManager.ActionThreshold + charged * speed);
            Check("clear_hides_owner_and_source_layers_" + id, RemovedAndHidden(id));
            Check("clear_releases_actual_multicell_footprint_" + id, blocked.Length > 1 && blocked.Any(c => !_input.CurrentZone.GetCell(c.x, c.y).BlocksMovement(_input.PlayerEntity)));
            _removed.Add(id); _clearActions++;
        }
        private void CheckPersistentState(string prefix, string[] identities, string inventory, int drams)
        {
            Check(prefix + "_removal", _removed.All(RemovedAndHidden));
            Check(prefix + "_five_doors", _definition.buildings.All(b => MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone, b.doorId)));
            Check(prefix + "_empty_container", Owner(_containerId)?.GetPart<ContainerPart>()?.Contents.Count == 0);
            Check(prefix + "_pantry_stays_empty", Owner(_produceId)?.GetPart<ContainerPart>()?.Contents.Count == 0);
            Check(prefix + "_living_identities", _input.CurrentZone.GetReadOnlyEntities().Where(e => e.HasPart<BrainPart>()).Select(e => e.ID).OrderBy(x => x).SequenceEqual(identities));
            Check(prefix + "_inventory_and_currency", InventorySnapshot() == inventory && TradeSystem.GetDrams(_input.PlayerEntity) == drams);
            Check(prefix + "_three_quests", _questIds.All(q => CavesOfOoo.Storylets.StoryletPart.Current?.IsQuestCompleted(q) == true));
            Check(prefix + "_bell_and_moved_stool", Owner("north-oath-arch")?.GetProperty("MorrowfastBellMode") == "loud"
                && _input.CurrentZone.GetEntityPosition(Owner("guest-stool-south")) == _movedStool);
            Check(prefix + "_manual_roof_state", MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone, _definition.buildings[0].roofId));
        }
        private string InventorySnapshot() => string.Join("|", _input.PlayerEntity.GetPart<InventoryPart>().Objects.GroupBy(e => e.BlueprintName).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1)));
        private Entity Owner(string id) => MorrowfastSceneRuntime.FindOwner(_input.CurrentZone, id);
        private bool RemovedAndHidden(string id)
        {
            if (MorrowfastSceneRuntime.IsPresent(_input.CurrentZone, id) || Owner(id) != null || Presenter()?.IsReady != true) return false;
            var definition = MorrowfastArtDefinition.Load(); var layerIds = new HashSet<string>(definition.layers.Where(l => l.ownerId == id).Select(l => l.id));
            var views = Presenter().GetComponentsInChildren<SpriteRenderer>(true).Where(r => layerIds.Contains(r.gameObject.name)).ToArray();
            return views.Length == layerIds.Count && views.All(r => !r.enabled || !r.gameObject.activeInHierarchy);
        }
        private IEnumerator WaitForScene()
        {
            for (int i = 0; i < 80; i++)
            {
                var presenter = Presenter();
                if (MorrowfastSceneRuntime.IsActive(_input.CurrentZone) && presenter != null && presenter.IsReady && ReferenceEquals(presenter.CurrentZone, _input.CurrentZone)) yield break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            throw new InvalidOperationException("Native Morrowfast presenter did not become ready: " + Presenter()?.Failure);
        }
        private IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(ReportDirectory); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(ReportDirectory, name)); yield return new WaitForSecondsRealtime(.25f);
        }
        private MorrowfastScenePresenter Presenter() => FindFirstObjectByType<MorrowfastScenePresenter>();
        private (int x, int y) Position() => _input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
        private string State() => typeof(InputHandler).GetField("_inputState", Private).GetValue(_input).ToString();
        private List<InventoryAction> Actions() => (List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Private).GetValue(_input.WorldActionMenuUI);
        private IEnumerator ApproachEntity(string id)
        {
            for (int step = 0; step < 180; step++)
            {
                var entity = _input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == id);
                Require(entity != null && (!entity.HasPart<BrainPart>() || entity.GetStatValue("Hitpoints") > 0), "Living native target " + id);
                var cell = _input.CurrentZone.GetEntityCell(entity); var here = Position();
                Require(cell != null, "Native actor is in the active zone.");
                if (InteractionTargetCell(entity, here.x, here.y) != null) yield break;
                var targets = InteractionTargets(entity); var path = FindPath(targets, false);
                if (path == null) yield return AwaitRoute(targets, false, value => path = value);
                Require(path.Count > 0, "Current route to moving native actor " + id);
                // Observe its actual new cell after each action instead of chasing
                // an obsolete full path or relocating the actor for the audit.
                var next = path[0]; yield return Step(DirectionKey(next.x - here.x, next.y - here.y), (next.x - here.x, next.y - here.y));
            }
            throw new InvalidOperationException("Moving-fauna approach exceeded 180 native steps: " + id);
        }
        private IEnumerator OpenEntityMenu(Entity owner)
        {
            var p = Position(); var target = InteractionTargetCell(owner, p.x, p.y);
            Require(target != null && Math.Abs(target.X - p.x) + Math.Abs(target.Y - p.y) == 1, "Native menu target is cardinally adjacent.");
            yield return Tap(Key.C); yield return Tap(DirectionKey(target.X - p.x, target.Y - p.y));
            Require(State() == "WorldActionMenuOpen", "Actual entity action menu.");
            string pick = WorldInteractionSystem.PickTargetCommandPrefix + owner.ID;
            if (!Actions().Any(a => a.Command == pick) && Actions().Any(a => a.Command == WorldInteractionSystem.PickCellCommand))
                yield return SelectCommand(WorldInteractionSystem.PickCellCommand);
            if (Actions().Any(a => a.Command == pick)) yield return SelectCommand(pick);
            Require(State() == "WorldActionMenuOpen"
                && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, owner)
                && !_input.WorldActionMenuUI.SelectedCellIsPile,
                "Native selection resolved the exact individual owner, not its pile summary: " + owner.ID);
        }
        private Cell InteractionTargetCell(Entity owner, int x, int y)
        {
            foreach (var d in Directions)
            {
                var cell = _input.CurrentZone.GetCell(x + d.x, y + d.y);
                if (CellExposesOwner(cell, owner)) return cell;
            }
            return null;
        }
        private HashSet<(int, int)> InteractionTargets(Entity owner)
        {
            var at = _input.CurrentZone.GetEntityPosition(owner); var hits = new HashSet<(int x, int y)> { at };
            var spec = _definition?.owners.FirstOrDefault(o => o.id == owner.GetPart<MorrowfastPropPart>()?.ComponentId);
            if (spec != null && spec.kind != "npc" && spec.kind != "creature")
                foreach (var c in spec.footprint) hits.Add((c.x + at.x - spec.anchorX, c.y + at.y - spec.anchorY));
            var result = new HashSet<(int, int)>();
            foreach (var hit in hits)
            {
                var cell = _input.CurrentZone.GetCell(hit.x, hit.y);
                if (!CellExposesOwner(cell, owner)) continue;
                foreach (var d in Directions) result.Add((hit.x + d.x, hit.y + d.y));
            }
            return result;
        }
        private bool CellExposesOwner(Cell cell, Entity owner)
        {
            if (cell == null) return false;
            if (cell.Objects.Contains(owner)) return true;
            if (!ReferenceEquals(MorrowfastSceneRuntime.BlockingOwner(cell), owner)) return false;
            // A real object actually in this cell retains its own native menu.
            // Approach another exposed side of a larger overlapping footprint.
            return !cell.Objects.Any(e => e != null && !WorldInteractionSystem.IsTerrain(e));
        }
        private int InventoryUnits(string blueprint) => _input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
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
            var seventh = FellingSceneRuntime.IsActive(_input.CurrentZone) ? FellingSceneDefinition.Load().landmarks.Single(l => l.kind == "seventh") : null;
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
            if (MorrowfastSceneRuntime.BlockingOwner(cell) != null) return false;
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
            Require(_elapsed.Elapsed.TotalSeconds < 1200, "Bounded twenty-minute native audit.");
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
        private bool OwnedCheckpoint() => SaveGameService.SaveRootOverride == _root && !string.IsNullOrEmpty(_gameId)
            && SaveGameService.GetSaveInfo("Quick")?.GameID == _gameId
            && File.Exists(Path.Combine(_root, _gameId, "Quick.sav.gz"))
            && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _gameId));
        private void Check(string name, bool pass)
        {
            _audit.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) Failures++;
            Debug.Log("[MorrowfastScenePlayAudit] " + RunId + " " + _audit[_audit.Count - 1]);
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
                clearActions = _clearActions, clearTicks = _clearTicks, faunaExamined = _faunaExamined, dressingActions = _dressingActions, faunaWaits = _faunaWaits, examinedOwners = _examinedOwners, doorsEntered = _doorsEntered, previewOnly = PreviewOnly, seconds = _elapsed?.Elapsed.TotalSeconds ?? 0, saveRoot = _root, gameId = _gameId,
                finalZone = _input?.CurrentZone?.ZoneID, fatal = _fatal, audit = _audit.ToArray(), removedIds = _removed.ToArray(),
                profileSeconds = _profileSeconds, profileCapacity = ProfileCapacity, metrics = _profileMetrics,
                profileContext = PreviewOnly ? "Preview only; profiling is reserved for the full native audit." :
                    "Read-only editor capture of native Morrowfast doors, rooms, individual-owner menus, creatures, shops, quests, containers, water, pantry loot, reciprocal Felling travel, save/load and screenshot/audit-observation overhead. Each recorder retains the latest at most 4096 samples; no FPS or global profiler settings changed." };
            File.WriteAllText(Path.Combine(ReportDirectory, PreviewOnly ? "native-preview.json" : "native-audit.json"), JsonUtility.ToJson(report, true));
            Finished = true; Debug.Log("[MorrowfastScenePlayAudit] " + JsonUtility.ToJson(report));
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
            public int failures, cases, nativeSteps, clearActions, clearTicks, profileCapacity, faunaExamined, dressingActions, faunaWaits, examinedOwners, doorsEntered;
            public bool previewOnly;
            public double seconds, profileSeconds;
            public Metric[] metrics;
            public string[] audit, removedIds;
        }
    }
}
