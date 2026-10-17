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
    /// <summary>Finite native-input throwing acceptance scenario. The isolated launcher
    /// owns the save root and scene. Only BuildFixture mutates the controlled game setup;
    /// all subsequent commands, navigation, payment and save/load use native input.</summary>
    public sealed class ThrowingNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        readonly List<float> frameSeconds = new List<float>();
        float frameWindowStart = -1, frameWindowElapsed;
        InputHandler input;
        Keyboard keyboard, oldKeyboard; Gamepad gamepad, oldGamepad;
        InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized, routeComplete, awaitExternalMouse;
        string externalMouseStage = "not requested";
        int failures, errors, routeScreenWidth, routeScreenHeight; string fatal;
        Entity target, equipped, daggers, hammer, water, floorDagger;
        string targetId, equippedId, daggersId, hammerId, waterId, floorDaggerId;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (Finished && !routeComplete ? 1 : 0);
        Entity Player => input.PlayerEntity;
        Zone Zone => input.CurrentZone;
        WorldCursorState Cursor => (WorldCursorState)Field(input, "_worldCursorState");
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/Throwing/Native", runId));

        public void Initialize(ScenarioContext context, bool externalMouse = false)
        {
            awaitExternalMouse = externalMouse;
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "private native save root required");
            clock = System.Diagnostics.Stopwatch.StartNew();
            oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldKeyboard = Keyboard.current; oldGamepad = Gamepad.current;
            keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(Run()));
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f);
            input = FindFirstObjectByType<InputHandler>(); Require(input != null, "ordinary bootstrap input");
            Require(((BootMenuController)Field(input, "_bootMenuController")).IsActive, "ordinary boot menu");
            yield return Tap(Key.N);
            var menu = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(menu.IsOpen, "ordinary build picker");
            int classic = menu.Model.Options.ToList().FindIndex(b => b.Id == "classic");
            Require(classic >= 0 && classic < 9, "authored Classic build exists");
            yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (classic + 1))); yield return Tap(Key.Enter);
            yield return Settled(); BuildFixture(); routeScreenWidth = Screen.width; routeScreenHeight = Screen.height; frameWindowStart = Time.realtimeSinceStartup; yield return new WaitForSecondsRealtime(.3f);
            Check("isolated_fixture_no_invincibility", !DebugInvincibility.IsEnabled(Player) && !ActionResolutionGuard.IsUnstable);

            // Equipped item follows the real inventory action route. A loose dagger
            // between launch and body aim must not turn into a full-height wall.
            var before = new Payment(input); string unchanged = Signature(); int hp = target.GetStatValue("Hitpoints");
            yield return InventoryThrow(equipped); yield return Aim(25, 12);
            Check("equipped_inventory_body_preview_clears_floor_dagger", Cursor.ThrowPreview.Trace.HitEntity == target
                && Cursor.ThrowPreview.Trace.Intent.Kind == ThrowAimKind.Entity && !Cursor.ThrowGroundAim);
            yield return Capture("01-equipped-body-aim"); yield return Tap(Key.Tab);
            Check("keyboard_ground_toggle", Cursor.ThrowGroundAim && Cursor.ThrowPreview.Trace.Intent.Kind == ThrowAimKind.Ground);
            yield return Capture("02-ground-aim"); yield return Tap(Key.Tab); yield return Tap(Key.Escape);
            Check("inventory_cancel_returns_popup_free", State == "InventoryOpen" && Field(input.InventoryUI, "_itemActionPopup") != null
                && Cost(before, 0) && Signature() == unchanged);
            yield return ChooseInventoryThrowAction(); yield return Aim(25, 12); yield return Tap(Key.Enter); yield return Settled();
            Check("equipped_throw_exact_unit_one_action", Cost(before, 1) && Zone.GetEntityCell(equipped) == Zone.GetCell(25, 12)
                && equipped.GetPart<PhysicsPart>().Equipped == null && equipped.GetPart<PhysicsPart>().InInventory == null
                && !Player.GetPart<InventoryPart>().GetAllEquipped().Contains(equipped));
            Check("equipped_throw_damage_and_unmoved_floor_control", target.GetStatValue("Hitpoints") < hp
                && Zone.GetEntityCell(floorDagger) == Zone.GetCell(22, 12) && Count(floorDagger) == 1);

            before = new Payment(input); unchanged = Signature();
            yield return OpenSkill(ThrowingRules.Measured); Require(State == "ThrowPopupOpen", "measured source picker");
            yield return Capture("03-measured-sources"); yield return Tap(Key.Escape);
            Check("skill_source_picker_cancel_free", State == "Normal" && Cost(before, 0) && Signature() == unchanged);
            yield return OpenSkill(ThrowingRules.Measured); yield return ChooseSource(daggers); yield return Aim(25, 12);
            Check("preparation_preview_keeps_item_and_no_payment", Cursor.ThrowPreview.Description.Contains("item kept")
                && Cost(before, 0) && Count(daggers) == 3);
            yield return Capture("04-measured-preparation"); yield return HoldKey(Key.Enter); yield return Settled("ThrowTargeting");
            Check("held_enter_prepares_only_once_and_retains_exact_followup", Cost(before, 1) && Count(daggers) == 3
                && PreparedFollowup(daggers) && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 5);
            yield return Capture("04a-measured-keyboard-ready");
            before = new Payment(input); hp = target.GetStatValue("Hitpoints");
            // A second confirm must launch directly: no inventory, source picker,
            // cursor movement or direct handler invocation between the two actions.
            yield return Tap(Key.Enter); yield return Settled();
            Check("keyboard_second_enter_throws_retained_item_once", Cost(before, 1) && Count(daggers) == 2
                && !Player.GetPart<ThrowingStatePart>().HasPreparation && target.GetStatValue("Hitpoints") < hp
                && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 4);

            // Synthetic device events exercise the production controller mapping,
            // not a direct call to the input handler or command resolver.
            gamepad = InputSystem.AddDevice<Gamepad>(); yield return Pad(new GamepadState());
            yield return WaitForMeasuredCooldown("before_controller_a");
            before = new Payment(input);
            yield return OpenSkill(ThrowingRules.Measured); yield return ChooseSource(daggers); yield return Aim(25, 12, true);
            yield return HoldPad(new GamepadState().WithButton(GamepadButton.South)); yield return Settled("ThrowTargeting");
            Check("held_controller_a_prepares_once_and_retains_exact_followup", Cost(before, 1) && Count(daggers) == 2
                && PreparedFollowup(daggers) && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 5);
            yield return Capture("04b-measured-controller-ready");
            before = new Payment(input); unchanged = Signature();
            yield return PadButton(GamepadButton.East);
            Check("controller_b_cancels_prepared_followup_without_action", State == "Normal" && Cost(before, 0)
                && Signature() == unchanged && !Cursor.Active && Field(input, "_pendingThrowTarget") == null);
            // Cancel is navigation only. Keep the existing real-save acceptance
            // for preparation at a stable Normal boundary, then use the saved aim.
            yield return StableRoundTrip("prepared");
            before = new Payment(input); hp = target.GetStatValue("Hitpoints");
            yield return InventoryThrow(daggers); yield return Aim(25, 12, true);
            yield return Pad(new GamepadState { rightTrigger = 1 }); yield return Settled();
            Check("controller_rt_throws_saved_preparation_once", Cost(before, 1) && Count(daggers) == 1
                && !Player.GetPart<ThrowingStatePart>().HasPreparation && target.GetStatValue("Hitpoints") < hp
                && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 4);
            yield return WaitForMeasuredCooldown("before_controller_rt");
            before = new Payment(input);
            yield return OpenSkill(ThrowingRules.Measured); yield return ChooseSource(daggers); yield return Aim(25, 12, true);
            yield return HoldPad(new GamepadState { rightTrigger = 1 }); yield return Settled("ThrowTargeting");
            Check("held_controller_rt_prepares_once_and_retains_exact_followup", Cost(before, 1) && Count(daggers) == 1
                && PreparedFollowup(daggers) && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 5);
            before = new Payment(input); hp = target.GetStatValue("Hitpoints");
            yield return PadButton(GamepadButton.South); yield return Settled();
            Check("controller_a_directly_throws_retained_last_unit_once", Cost(before, 1)
                && !Player.GetPart<InventoryPart>().Objects.Contains(daggers) && Zone.GetEntityCell(daggers) == Zone.GetCell(25, 12)
                && Count(daggers) == 1 && !Player.GetPart<ThrowingStatePart>().HasPreparation
                && target.GetStatValue("Hitpoints") < hp && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 4);
            before = new Payment(input); unchanged = Signature();
            yield return InventoryThrow(water); yield return Aim(23, 15, true);
            Check("controller_dpad_aims_without_payment", Cursor.X == 23 && Cursor.Y == 15 && Cost(before, 0));
            Check("tonic_defaults_broad", !Cursor.ThrowFocused && Cursor.ThrowPreview.Splash.Count == 9);
            if (awaitExternalMouse) yield return ExternalMouseControls(before);
            yield return PadButton(GamepadButton.North);
            Check("controller_y_selects_ground", Cursor.ThrowGroundAim);
            yield return PadButton(GamepadButton.LeftShoulder);
            Check("controller_lb_focuses_one_cell", Cursor.ThrowFocused && Cursor.ThrowPreview.Splash.Count == 1);
            yield return Capture("05-controller-focused-splash"); yield return PadButton(GamepadButton.East);
            Check("controller_cancel_free_and_restores_inventory", State == "InventoryOpen" && Cost(before, 0) && Signature() == unchanged);
            yield return ChooseInventoryThrowAction(); yield return Aim(23, 15, true);
            Check("reopened_throw_defaults_broad", !Cursor.ThrowFocused && Cursor.ThrowPreview.Splash.Count == 9);
            yield return PadButton(GamepadButton.LeftShoulder); yield return PadButton(GamepadButton.South); yield return Settled();
            var films = Zone.GetReadOnlyEntities().Where(e => e.HasPart<FiniteLiquidFilmPart>()).ToArray();
            Check("controller_a_commits_one_finite_payload_action", Cost(before, 1) && Count(water) == 0
                && !Player.GetPart<InventoryPart>().Objects.Contains(water) && films.Length == 1
                && films[0].GetPart<LiquidPoolPart>().Volume == 9 && Zone.GetEntityCell(films[0]) == Zone.GetCell(23, 15));
            yield return Capture("06-focused-film-result"); yield return StableRoundTrip("film");

            before = new Payment(input); hp = target.GetStatValue("Hitpoints");
            yield return OpenSkill(ThrowingRules.Driving); yield return ChooseSource(hammer); yield return Aim(25, 12);
            Check("driving_preview_labels_conditional_push", Cursor.ThrowPreview.ConditionalPush == Zone.GetCell(26, 12)
                && Cursor.ThrowPreview.Description.Contains("only after damage"));
            yield return Capture("07-driving-preview"); yield return Tap(Key.Enter); yield return Settled();
            Check("driving_damage_push_rest_and_payment", Cost(before, 1) && target.GetStatValue("Hitpoints") < hp
                && Zone.GetEntityCell(target) == Zone.GetCell(26, 12) && Zone.GetEntityCell(hammer) == Zone.GetCell(25, 12)
                && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Driving) == 11);
            Check("landed_unit_marked_for_exact_recovery", hammer.GetPart<ThrownRecoveryPart>()?.ThrowerId == Player.ID
                && Count(hammer) == 1 && !ActionResolutionGuard.IsUnstable);
            before = new Payment(input);
            for (int i = 0; i < 4; i++) { yield return Tap(Key.RightArrow); yield return Settled(); }
            Check("ordinary_walk_to_recovery_is_four_actions", Cost(before, 4) && Zone.GetEntityCell(Player) == Zone.GetCell(24, 12));
            before = new Payment(input);
            yield return OpenSkill(ThrowingRules.Recovery); yield return Capture("08-recovery-source"); yield return ChooseSource(hammer); yield return Settled();
            Check("recovering_step_exact_unit_one_action_no_equip", Cost(before, 1) && Zone.GetEntityCell(Player) == Zone.GetCell(25, 12)
                && Zone.GetEntityCell(hammer) == null && hammer.GetPart<PhysicsPart>().InInventory == Player
                && hammer.GetPart<PhysicsPart>().Equipped == null && Player.GetPart<InventoryPart>().Objects.Count(e => e == hammer) == 1
                && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Recovery) == 5);
            yield return Capture("09-recovered-equipment"); yield return StableRoundTrip("recovered");

            // Finish through the independent Look -> world-object action route.
            // Reuse the real loose dagger preserved by the earlier lane checks.
            before = new Payment(input);
            for (int i = 0; i < 2; i++) { yield return Tap(Key.LeftArrow); yield return Settled(); }
            Check("walk_to_ground_source_is_two_actions", Cost(before, 2) && Zone.GetEntityCell(Player) == Zone.GetCell(23, 12));
            before = new Payment(input); unchanged = Signature(); hp = target.GetStatValue("Hitpoints");
            yield return Tap(Key.L); Require(State == "LookMode", "actual look mode opens");
            yield return Tap(Key.LeftArrow);
            Require(Cursor.X == 22 && Cursor.Y == 12, "look cursor reaches exact adjacent source");
            yield return ChooseWorldThrow(floorDagger); yield return Aim(26, 12);
            yield return Capture("10-ground-source-preview"); yield return Tap(Key.Escape);
            Check("ground_source_cancel_restores_exact_look_cell_free", State == "LookMode" && Cursor.X == 22 && Cursor.Y == 12
                && Zone.GetEntityCell(floorDagger) == Zone.GetCell(22, 12) && Cost(before, 0) && Signature() == unchanged);
            yield return ChooseWorldThrow(floorDagger); yield return Aim(26, 12); yield return Tap(Key.Enter); yield return Settled();
            Check("world_source_throw_one_paid_exact_unit", Cost(before, 1) && Zone.GetEntityCell(floorDagger) == Zone.GetCell(26, 12)
                && floorDagger.GetPart<PhysicsPart>().InInventory == null && floorDagger.GetPart<PhysicsPart>().Equipped == null
                && Count(floorDagger) == 1 && !Zone.GetCell(22, 12).Objects.Contains(floorDagger));
            Check("world_source_body_throw_damages_target", target.GetStatValue("Hitpoints") < hp);
            yield return Capture("11-ground-source-landed"); yield return StableRoundTrip("world_source");
            Check("final_no_pending_action_or_fx", State == "Normal" && !ActionResolutionGuard.IsUnstable && !input.ZoneRenderer.HasBlockingFx);
            routeComplete = true; WriteReport();
        }

        void BuildFixture()
        {
            foreach (var entity in Zone.GetAllEntities().ToArray())
                if (entity != Player) { input.TurnManager.RemoveEntity(entity); Zone.RemoveEntity(entity); }
            Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                Zone.TileState.Clear(x, y); var cell = Zone.GetCell(x, y); cell.IsVisible = cell.Explored = false;
                if (x >= 16 && x <= 32 && y >= 8 && y <= 18)
                { Zone.AddEntity(input.EntityFactory.CreateEntity("StoneFloor"), x, y); cell.IsVisible = cell.Explored = true; }
            }
            Require(Zone.MoveEntity(Player, 20, 12), "fixture origin");
            Player.Statistics["Strength"].BaseValue = 30; Player.Statistics["Agility"].BaseValue = 30;
            Player.GetPart<InventoryPart>().MaxWeight = 500;
            // Controlled real target: remove autonomous actions and raise its HP so
            // all native demonstrations share one visible surviving owner.
            target = input.EntityFactory.CreateEntity("MarlbackGleaner"); Require(target != null, "real target blueprint");
            if (target.GetPart<BrainPart>() is BrainPart brain) target.RemovePart(brain);
            target.Statistics["Hitpoints"].BaseValue = target.Statistics["Hitpoints"].Max = 500;
            if (target.GetPart<ArmorPart>() is ArmorPart armor) { armor.AV = 0; armor.DV = 0; }
            Require(Zone.AddEntity(target, 25, 12), "target placement");
            equipped = Carry("ShortSword"); Require(InventorySystem.Equip(Player, equipped), "controlled equipped sword");
            daggers = Carry("Dagger"); daggers.GetPart<StackerPart>().StackCount = 3;
            hammer = Carry("Warhammer"); water = Carry("WaterTonic");
            floorDagger = input.EntityFactory.CreateEntity("Dagger"); Require(Zone.AddEntity(floorDagger, 22, 12), "low loose item in lane");
            var skills = Player.GetPart<SkillsPart>(); Require(skills != null, "player skill owner");
            foreach (BaseSkillPart skill in new BaseSkillPart[] { new ThrowingSkill(), new Throwing_MeasuredThrow(), new Throwing_DrivingThrow(), new Throwing_RecoveringStep(), new Throwing_FocusedSplash() })
                Require(skills.HasSkill(skill.Name) || skills.AddSkill(skill, "scenario:throwing-native"), "controlled skill " + skill.Name);
            typeof(InputHandler).GetField("_combatRng", Fields).SetValue(input, new System.Random(123));
            targetId = target.ID; equippedId = equipped.ID; daggersId = daggers.ID; hammerId = hammer.ID; waterId = water.ID; floorDaggerId = floorDagger.ID;
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("ControlledThrowingNativeFixture");
        }
        Entity Carry(string blueprint)
        {
            var pack = Player.GetPart<InventoryPart>();
            var existing = pack.Objects.FirstOrDefault(e => e.BlueprintName == blueprint && Count(e) > 0);
            if (existing != null) return existing;
            var item = input.EntityFactory.CreateEntity(blueprint);
            Require(item != null && pack.AddObject(item) && pack.Objects.Contains(item), "controlled carry " + blueprint);
            return item;
        }
        static int Count(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? (item == null ? 0 : 1);
        void Rebind()
        {
            var all = Zone.GetReadOnlyEntities().Concat(Player.GetPart<InventoryPart>().Objects).Concat(Player.GetPart<InventoryPart>().GetAllEquipped()).Distinct().ToArray();
            target = all.Single(e => e.ID == targetId); equipped = all.Single(e => e.ID == equippedId);
            daggers = all.Single(e => e.ID == daggersId); hammer = all.Single(e => e.ID == hammerId);
            water = all.FirstOrDefault(e => e.ID == waterId); floorDagger = all.Single(e => e.ID == floorDaggerId);
        }
        IEnumerator StableRoundTrip(string label)
        {
            Require(State == "Normal" && !ActionResolutionGuard.IsUnstable, "stable native save boundary");
            string signature = Signature(); var originalPlayer = Player;
            yield return Tap(Key.F5);
            float deadline = Time.realtimeSinceStartup + 10;
            while (SaveGameService.HasPendingSave && Time.realtimeSinceStartup < deadline) yield return null;
            Require(!SaveGameService.HasPendingSave, "bounded native save completion");
            yield return Tap(Key.F6); yield return Settled(); Rebind();
            Check(label + "_native_f5_f6_same_stable_graph", !ReferenceEquals(originalPlayer, Player) && Signature() == signature
                && !ActionResolutionGuard.IsUnstable);
        }
        string Signature()
        {
            var state = Player.GetPart<ThrowingStatePart>(); var pack = Player.GetPart<InventoryPart>();
            return JsonConvert.SerializeObject(new {
                actor = Player.ID, zone = Zone.ZoneID, x = Zone.GetEntityCell(Player).X, y = Zone.GetEntityCell(Player).Y,
                tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player), hp = Player.GetStatValue("Hitpoints"),
                state.MeasuredCooldown, state.DrivingCooldown, state.RecoveryCooldown, state.HasPreparation,
                source = state.PreparedSource?.ID, target = state.PreparedTarget?.ID,
                inventory = pack.Objects.OrderBy(e => e.ID).Select(e => new { e.ID, count = Count(e) }).ToArray(),
                equipped = pack.GetAllEquipped().OrderBy(e => e.ID).Select(e => e.ID).ToArray(),
                world = Zone.GetReadOnlyEntities().Where(e => e != Player && e.BlueprintName != "StoneFloor").OrderBy(e => e.ID)
                    .Select(e => new { e.ID, e.BlueprintName, x = Zone.GetEntityCell(e).X, y = Zone.GetEntityCell(e).Y,
                        count = Count(e), hp = e.GetStatValue("Hitpoints"), volume = e.GetPart<LiquidPoolPart>()?.Volume,
                        rounds = e.GetPart<FiniteLiquidFilmPart>()?.RemainingRounds,
                        thrower = e.GetPart<ThrownRecoveryPart>()?.ThrowerId, serial = e.GetPart<ThrownRecoveryPart>()?.Serial }).ToArray()
            });
        }
        readonly struct Payment
        {
            public readonly Entity Actor; public readonly Zone Zone; public readonly int Tick, Energy, Speed;
            public Payment(InputHandler handler) { Actor = handler.PlayerEntity; Zone = handler.CurrentZone;
                Tick = handler.TurnManager.TickCount; Energy = handler.TurnManager.GetEnergy(Actor); Speed = handler.TurnManager.GetSpeed(Actor); }
        }
        bool Cost(Payment before, int actions) => Player == before.Actor && Zone == before.Zone
            && input.TurnManager.GetSpeed(Player) == before.Speed
            && (long)(input.TurnManager.TickCount - before.Tick) * before.Speed + before.Energy - input.TurnManager.GetEnergy(Player)
                == (long)actions * TurnManager.ActionThreshold;
        IEnumerator InventoryThrow(Entity item)
        {
            Require(State == "Normal", "inventory throw begins in gameplay"); yield return Tap(Key.I);
            Require(State == "InventoryOpen", "actual native inventory opens");
            // The full inventory row list exists while Equipment has focus;
            // finding an item in that list does not make its cursor active.
            Require((int)Field(input.InventoryUI, "_panel") == 0, "inventory initially focuses Equipment");
            yield return Tap(Key.Tab);
            Require((int)Field(input.InventoryUI, "_panel") == 1, "native Tab focuses the inventory list");
            int row = InventoryRow(item);
            Require(row >= 0, "exact item visible in inventory");
            yield return SelectRow(() => (int)Field(input.InventoryUI, "_cursorIndex"), row);
            yield return Tap(Key.Enter); yield return ChooseInventoryThrowAction();
        }
        int InventoryRow(Entity item)
        {
            var rows = (IList)Field(input.InventoryUI, "_rows");
            for (int i = 0; i < rows.Count; i++)
                if (ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i], "Item"))?.Item, item)) return i;
            return -1;
        }
        IEnumerator ChooseInventoryThrowAction()
        {
            var popup = Field(input.InventoryUI, "_itemActionPopup"); Require(popup != null, "actual inventory item action popup");
            var actions = (IList)Field(popup, "Actions"); int wanted = -1;
            for (int i = 0; i < actions.Count; i++) if ((string)Field(actions[i], "Command") == "throw") wanted = i;
            Require(wanted >= 0, "actual offered Throw action");
            yield return SelectRow(() => (int)Field(popup, "CursorIndex"), wanted); yield return Tap(Key.Enter);
            Require(State == "ThrowTargeting", "inventory enters throwing selection");
        }
        IEnumerator ChooseWorldThrow(Entity item)
        {
            Require(State == "LookMode", "world throw begins from look"); yield return Tap(Key.Enter);
            Require(State == "WorldActionMenuOpen" && input.WorldActionMenuUI.SelectedTarget == item,
                "world menu owns the exact loose source");
            var actions = (List<InventoryAction>)Field(input.WorldActionMenuUI, "_actions");
            int row = actions.FindIndex(action => action.Command == "Throw");
            yield return SelectRow(() => (int)Field(input.WorldActionMenuUI, "_cursorIndex"), row); yield return Tap(Key.Enter);
            Require(State == "ThrowPopupOpen", "world action opens ordinary throw popup");
            var popup = Field(input, "_throwPopup");
            Require(ReferenceEquals(Field(popup, "SelectedItem"), item), "throw popup preserves exact source identity");
            yield return Tap(Key.Enter); Require(State == "ThrowTargeting", "world source enters shared targeting");
        }
        IEnumerator OpenSkill(string name)
        {
            Require(State == "Normal", "skill begins in gameplay"); yield return Tap(Key.M);
            Require(State == "AbilityManagerOpen", "actual ability manager opens");
            var ability = Player.GetPart<SkillsPart>().GetSkill(name).ActivatedAbilityID;
            int row = AbilityManagerStateBuilder.Build(Player).Rows.ToList().FindIndex(r => r.AbilityID == ability);
            yield return SelectRow(() => input.AbilityManagerUI.CursorIndex, row); yield return Tap(Key.Enter);
            Require(State == "ThrowPopupOpen", "actual skill source selection");
        }
        IEnumerator ChooseSource(Entity item)
        {
            var popup = Field(input, "_throwPopup"); var options = (IList)Field(popup, "Options"); int wanted = -1;
            for (int i = 0; i < options.Count; i++) if (ReferenceEquals(Field(options[i], "Item"), item)) wanted = i;
            Require(wanted >= 0, "exact source offered by skill");
            yield return SelectRow(() => (int)Field(popup, "CursorIndex"), wanted); yield return Tap(Key.Enter);
        }
        IEnumerator SelectRow(Func<int> cursor, int wanted)
        { Require(wanted >= 0, "selected row exists"); int guard = 150;
            while (cursor() != wanted && guard-- > 0) yield return Tap(cursor() < wanted ? Key.DownArrow : Key.UpArrow);
            Require(cursor() == wanted, "bounded native row navigation: state=" + State + ", current=" + cursor() + ", wanted=" + wanted); }
        IEnumerator Aim(int x, int y, bool controller = false)
        {
            Require(State == "ThrowTargeting", "throw targeting for aim"); int guard = 100;
            while ((Cursor.X != x || Cursor.Y != y) && guard-- > 0)
            {
                if (controller) yield return PadButton(Cursor.X < x ? GamepadButton.DpadRight : Cursor.X > x ? GamepadButton.DpadLeft
                    : Cursor.Y < y ? GamepadButton.DpadDown : GamepadButton.DpadUp);
                else yield return Tap(Cursor.X < x ? Key.RightArrow : Cursor.X > x ? Key.LeftArrow : Cursor.Y < y ? Key.DownArrow : Key.UpArrow);
            }
            Require(Cursor.X == x && Cursor.Y == y, "bounded native cursor reaches exact cell");
        }
        bool PreparedFollowup(Entity item)
        {
            var pending = Field(input, "_pendingThrowTarget");
            var intent = pending == null ? null : (ThrowIntent)Field(pending, "Intent");
            return State == "ThrowTargeting" && pending != null && ReferenceEquals(Field(pending, "Item"), item)
                && (ThrowMode)Field(pending, "Mode") == ThrowMode.Ordinary && intent?.Target == target
                && intent.Kind == ThrowAimKind.Entity && Cursor.Active && Cursor.X == 25 && Cursor.Y == 12
                && !Cursor.ThrowGroundAim && Player.GetPart<ThrowingStatePart>().Prepared;
        }
        IEnumerator WaitForMeasuredCooldown(string label)
        {
            int waits = ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured);
            Require(State == "Normal" && waits > 0 && waits <= 6, "bounded real measured cooldown");
            var before = new Payment(input);
            for (int i = 0; i < waits; i++) { yield return Tap(Key.Period); yield return Settled(); }
            Check(label + "_cooldown_uses_" + waits + "_ordinary_wait_actions", Cost(before, waits)
                && ThrowingRules.CooldownRemaining(Player, ThrowingRules.Measured) == 0);
        }
        IEnumerator Settled(string expectedState = "Normal")
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while ((State != expectedState || ActionResolutionGuard.IsUnstable || input.ZoneRenderer?.HasBlockingFx == true) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Require(State == expectedState && !ActionResolutionGuard.IsUnstable && input.ZoneRenderer?.HasBlockingFx != true,
                "native action settled in " + expectedState + " within twelve seconds; actual=" + State);
        }
        IEnumerator HoldKey(Key key)
        {
            yield return new WaitForSecondsRealtime(.13f); keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.08f);
        }
        IEnumerator HoldPad(GamepadState value)
        {
            yield return new WaitForSecondsRealtime(.13f); gamepad.MakeCurrent();
            InputSystem.QueueStateEvent(gamepad, value); yield return new WaitForSecondsRealtime(.5f);
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); yield return null; yield return new WaitForSecondsRealtime(.08f);
        }
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.13f); keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.08f);
        }
        IEnumerator ExternalMouseControls(Payment payment)
        {
            yield return ExternalMouseStage("Click Body to select Ground", true, false, payment);
            yield return ExternalMouseStage("Click Ground to select Body", false, false, payment);
            yield return ExternalMouseStage("Click Broad to select Focused", false, true, payment);
            yield return ExternalMouseStage("Click Focused to select Broad", false, false, payment);
            externalMouseStage = "complete"; WriteReport();
        }
        IEnumerator ExternalMouseStage(string instruction, bool ground, bool focused, Payment payment)
        {
            externalMouseStage = instruction; WriteReport();
            float deadline = Time.realtimeSinceStartup + 90;
            while (State == "ThrowTargeting" && (Cursor.ThrowGroundAim != ground || Cursor.ThrowFocused != focused)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Require(State == "ThrowTargeting" && Cursor.ThrowGroundAim == ground && Cursor.ThrowFocused == focused,
                "external mouse gate: " + instruction);
            Check("external_mouse_" + instruction.Replace(' ', '_') + "_free", Cost(payment, 0) && Cursor.X == 23 && Cursor.Y == 15
                && Cursor.ThrowPreview.Splash.Count == (focused ? 1 : 9));
            var bounds = (Rect)Field(input, "_throwControlsRect");
            observations.Add(new { name = "external-mouse", instruction, ground = Cursor.ThrowGroundAim, focused = Cursor.ThrowFocused,
                pointerX = Input.mousePosition.x, pointerY = Input.mousePosition.y,
                controls = new { bounds.x, bounds.y, bounds.width, bounds.height } });
            WriteReport();
        }
        IEnumerator PadButton(GamepadButton button) => Pad(new GamepadState().WithButton(button));
        IEnumerator Pad(GamepadState value)
        {
            yield return new WaitForSecondsRealtime(.13f); gamepad.MakeCurrent(); InputSystem.QueueStateEvent(gamepad, value); yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); yield return null; yield return new WaitForSecondsRealtime(.08f);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath); string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path); screenshots.Add(path);
            if (State == "ThrowPopupOpen")
            {
                var popupCamera = input.ZoneRenderer?.PopupOverlayCamera;
                var foreground = input.ZoneRenderer?.CenteredPopupFgTilemap;
                var renderer = foreground?.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                int usedTiles = foreground == null ? 0 : foreground.GetUsedTilesCount();
                bool cameraActive = popupCamera != null && popupCamera.isActiveAndEnabled;
                bool tilesActive = foreground != null && foreground.gameObject.activeInHierarchy
                    && renderer != null && renderer.enabled && usedTiles > 0;
                bool layerVisible = popupCamera != null && foreground != null
                    && (popupCamera.cullingMask & (1 << foreground.gameObject.layer)) != 0;
                Check(name + "_source_overlay_camera_and_tiles_visible", cameraActive && tilesActive && layerVisible);
                observations.Add(new { name = name + "-source-overlay", cameraActive, tilesActive, layerVisible, usedTiles });
            }
            observations.Add(new { name, width = Screen.width, height = Screen.height, state = State, tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player),
                guard = ActionResolutionGuard.IsUnstable, preview = Cursor.ThrowPreview?.Description, signature = Signature() }); WriteReport();
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
        static object Field(object owner, string name) => owner.GetType().GetField(name, Fields).GetValue(owner);
        static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
        void Check(string label, bool pass) { checks.Add((pass ? "PASS " : "FAIL ") + label); if (!pass) failures++; WriteReport(); }
        public void SetUnexpectedErrors(int count) { errors = count; errorsFinalized = true; WriteReport(); }
        public void Abort(string reason) { fatal = reason; failures++; StopAllCoroutines(); Cleanup(); Finished = true; WriteReport(); }
        void Update()
        {
            if (frameWindowStart < 0 || Finished) return;
            float elapsed = Time.realtimeSinceStartup - frameWindowStart;
            if (elapsed > 90) return;
            frameSeconds.Add(Time.unscaledDeltaTime); frameWindowElapsed = elapsed;
        }
        object FrameTiming()
        {
            var ordered = frameSeconds.OrderBy(v => v).ToArray();
            float Percentile(double fraction) => ordered.Length == 0 ? 0 : ordered[Math.Min(ordered.Length - 1, (int)Math.Ceiling(ordered.Length * fraction) - 1)] * 1000;
            return new { samples = ordered.Length, elapsedSeconds = frameWindowElapsed,
                p50Milliseconds = Percentile(.50), p95Milliseconds = Percentile(.95), p99Milliseconds = Percentile(.99),
                maxMilliseconds = ordered.Length == 0 ? 0 : ordered[ordered.Length - 1] * 1000,
                window = "First up to90 seconds of actual fixture-ready native route; ends with route if earlier. No padded idle.",
                scope = "Unity unscaled frame duration includes menu input pacing, capture work, save/load and editor overhead; not pure renderer, GC attribution or Steam Deck performance." };
        }
        object ThrowControlBounds()
        {
            if (input == null) return null;
            var bounds = (Rect)Field(input, "_throwControlsRect");
            return new { bounds.x, bounds.y, bounds.width, bounds.height };
        }
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonConvert.SerializeObject(new {
                runId, screen = new { width = routeScreenWidth, height = routeScreenHeight },
                externalMouse = new { requested = awaitExternalMouse, stage = externalMouseStage, controls = ThrowControlBounds() },
                complete = Finished && errorsFinalized && routeComplete && Failures == 0,
                failures = Failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots, frameTiming = FrameTiming(),
                fixture = "Isolated seed64 N/Classic bootstrap. Original zone cleared to a bounded stone floor, player at20,12 with Strength/Agility30 and capacity500. Actual MarlbackGleaner at25,12 has no Brain, HP500 and natural AV/DV0. Actual equipped ShortSword, carried Dagger stack3, Warhammer, WaterTonic; separate floor Dagger22,12. Five throwing skills granted and command RNG seeded123. All modifications occur before observations; gameplay, selectors, saves and loads then use native keyboard/synthetic gamepad input.",
                canVerify = "Equipped/carried real inventory and Look world-source routes, true skill source picker, cancellation/restoration and no payment, body/ground/focus previews, Y/LB/B/A/RT and D-pad dispatch, held Enter/A/RT cannot auto-launch the retained Measured follow-up, direct second-confirm keyboard/controller throws, free B cancellation of the paid preparation UI, rendered mouse mode-button dispatch only when the external mouse gate is requested and completed, one-action scheduler accounting, exact physical item identities, consumed finite tonic, Measured preparation and real cooldown, conditional Driving push, Recovery step and no auto-equip, stable F5/F6 roundtrips with preparation/residue/custody/debts.",
                cannotVerify = "Controlled fixture does not prove natural acquisition, AI behavior, balance, encounter quality, physical Steam Deck controls or performance. Synthetic controller states exercise production mapping but not Steam Input. External mouse mode-button acceptance requires actual GameView clicks while the driver waits for observed transitions; the driver never invokes toggles or changes gameplay state directly. The earlier rejected synthetic-mouse attempt is retained separately. Pixel legibility and animation quality require independent review of the native screenshots. Native launcher restores the preceding scene/save root/preferences; this is not the user's campaign."
            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (keyboard != null && keyboard.added) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (gamepad != null && gamepad.added) { InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.RemoveDevice(gamepad); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldGamepad != null && oldGamepad.added) oldGamepad.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings);
            Application.runInBackground = oldBackground;
        }
        void OnDestroy() => Cleanup();
    }
}
