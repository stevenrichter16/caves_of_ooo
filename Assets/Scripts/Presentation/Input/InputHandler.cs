using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that converts player key presses into game commands.
    /// Supports WASD, arrow keys, numpad (8-directional), vi keys,
    /// item pickup (G/comma), ability activation (1-9 + direction/immediate cast),
    /// and debug keys: F6 (grant mutation), F7 (dump body parts),
    /// F8 (dismember limb), F9 (debug craft recipe), P (cycle well state).
    /// This is the input boundary — the only place Unity input touches the simulation.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        /// <summary>
        /// The player entity to move.
        /// </summary>
        public Entity PlayerEntity { get; set; }

        /// <summary>
        /// The zone the player is in.
        /// </summary>
        public Zone CurrentZone { get; set; }

        /// <summary>
        /// The turn manager to report actions to.
        /// </summary>
        public TurnManager TurnManager { get; set; }

        /// <summary>
        /// The renderer to refresh after movement.
        /// </summary>
        public ZoneRenderer ZoneRenderer { get; set; }

        /// <summary>
        /// The zone manager for zone transitions.
        /// </summary>
        public ZoneManager ZoneManager { get; set; }

        /// <summary>
        /// The world map for zone adjacency lookups.
        /// </summary>
        public WorldMap WorldMap { get; set; }

        /// <summary>
        /// Camera follow component to update on zone transitions.
        /// </summary>
        public CameraFollow CameraFollow { get; set; }

        /// <summary>
        /// Minimum time between moves (seconds) to prevent too-fast input.
        /// </summary>
        public float MoveRepeatDelay = 0.12f;

        private float _lastMoveTime;
        private System.Random _combatRng = new System.Random();
        private const int FullscreenUiGridWidth = 80;
        private const int FullscreenUiGridHeight = 45;

        /// <summary>
        /// Input state machine for ability targeting.
        /// Normal: standard movement/action input.
        /// AwaitingDirection: waiting for a directional key to target an ability.
        /// </summary>
        private enum InputState
        {
            Normal,
            LookMode,
            ThrowTargeting,
            ThrowPopupOpen,
            AwaitingDirection,
            WaitingForFxResolution,
            InventoryOpen,
            PickupOpen,
            ContainerPickerOpen,
            AwaitingTalkDirection,
            DialogueOpen,
            TradeOpen,
            AwaitingAttackConfirm,
            FactionOpen,
            AnnouncementOpen
        }
        private InputState _inputState = InputState.Normal;
        private ActivatedAbility _pendingAbility;
        private Entity _pendingAttackTarget;
        private int _selectedHotbarSlot = -1;
        private readonly WorldCursorState _worldCursorState = new WorldCursorState();
        private Vector3 _lastLookMousePosition;
        private ThrowTargetState _pendingThrowTarget;
        private ThrowPopupState _throwPopup;

        private enum ThrowOriginKind
        {
            Inventory,
            LookWorld
        }

        private enum ThrowPopupKind
        {
            SourcePicker,
            ActionPicker
        }

        private sealed class ThrowTargetState
        {
            public Entity Item;
            public ThrowOriginKind Origin;
            public int SourceTileX;
            public int SourceTileY;
        }

        private sealed class ThrowPopupState
        {
            public ThrowPopupKind Kind;
            public string Title;
            public List<ThrowPopupOption> Options = new List<ThrowPopupOption>();
            public int CursorIndex;
            public int ScrollOffset;
            public int SourceTileX;
            public int SourceTileY;
            public Entity SelectedItem;
        }

        private sealed class ThrowPopupOption
        {
            public string Label;
            public Entity Item;
        }

        /// <summary>
        /// The dialogue UI component. Set by GameBootstrap.
        /// </summary>
        public DialogueUI DialogueUI { get; set; }

        /// <summary>
        /// The trade UI component. Set by GameBootstrap.
        /// </summary>
        public TradeUI TradeUI { get; set; }

        /// <summary>
        /// The inventory UI component. Set by GameBootstrap.
        /// </summary>
        public InventoryUI InventoryUI { get; set; }

        /// <summary>
        /// The pickup UI component. Set by GameBootstrap.
        /// </summary>
        public PickupUI PickupUI { get; set; }

        /// <summary>
        /// The container picker UI component. Set by GameBootstrap.
        /// </summary>
        public ContainerPickerUI ContainerPickerUI { get; set; }

        /// <summary>
        /// The faction standings UI component. Set by GameBootstrap.
        /// </summary>
        public FactionUI FactionUI { get; set; }

        public AnnouncementUI AnnouncementUI { get; set; }

        /// <summary>
        /// Entity factory used by debug crafting flows.
        /// </summary>
        public EntityFactory EntityFactory { get; set; }
        public ScreenFade ScreenFade { get; set; }

        private void Update()
        {
            using (PerformanceMarkers.Input.Update.Auto())
            {
                if (PlayerEntity == null || CurrentZone == null || TurnManager == null)
                    return;

                // Only accept input when it's the player's turn
                if (!TurnManager.WaitingForInput)
                    return;

                if (TurnManager.CurrentActor != PlayerEntity)
                    return;

            EnsureHotbarSelectionValid();
            SyncHotbarState();

            if (_inputState == InputState.WaitingForFxResolution)
            {
                HandleWaitingForFxResolution();
                return;
            }

            if (_inputState == InputState.LookMode)
            {
                HandleLookModeInput();
                return;
            }

            if (_inputState == InputState.ThrowTargeting)
            {
                HandleThrowTargetingInput();
                return;
            }

            if (_inputState == InputState.ThrowPopupOpen)
            {
                HandleThrowPopupInput();
                return;
            }

            // Rate limit
            if (Time.time - _lastMoveTime < MoveRepeatDelay)
                return;

            if (_inputState == InputState.AwaitingDirection)
            {
                HandleAwaitingDirection();
                return;
            }

            if (_inputState == InputState.InventoryOpen)
            {
                HandleInventoryInput();
                return;
            }

            if (_inputState == InputState.PickupOpen)
            {
                HandlePickupInput();
                return;
            }

            if (_inputState == InputState.ContainerPickerOpen)
            {
                HandleContainerPickerInput();
                return;
            }

            if (_inputState == InputState.AwaitingTalkDirection)
            {
                HandleAwaitingTalkDirection();
                return;
            }

            if (_inputState == InputState.DialogueOpen)
            {
                HandleDialogueInput();
                return;
            }

            if (_inputState == InputState.AwaitingAttackConfirm)
            {
                HandleAttackConfirmInput();
                return;
            }

            if (_inputState == InputState.TradeOpen)
            {
                HandleTradeInput();
                return;
            }

            if (_inputState == InputState.FactionOpen)
            {
                HandleFactionInput();
                return;
            }

            if (_inputState == InputState.AnnouncementOpen)
            {
                HandleAnnouncementInput();
                return;
            }

            // Check for pending announcements before normal input
            if (TryOpenAnnouncement())
                return;

            if (TryHandleSidebarLogScrollInput())
                return;

            if (TryHandleHotbarInput())
                return;

            if (InputHelper.GetKeyDown(KeyCode.L))
            {
                EnterLookMode();
                _lastMoveTime = Time.time;
                return;
            }

            // Open inventory (I key)
            if (InputHelper.GetKeyDown(KeyCode.I))
            {
                OpenInventory();
                _lastMoveTime = Time.time;
                return;
            }

            // Open faction standings (F key)
            if (InputHelper.GetKeyDown(KeyCode.F))
            {
                OpenFaction();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: grant a random mutation from the current mutate pool.
            if (InputHelper.GetKeyDown(KeyCode.F6))
            {
                TryDebugGrantRandomMutation();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: dump body part tree to console.
            if (InputHelper.GetKeyDown(KeyCode.F7))
            {
                TryDebugDumpBodyParts();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: dismember a random non-mortal appendage.
            if (InputHelper.GetKeyDown(KeyCode.F8))
            {
                TryDebugDismember();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: craft one known recipe through the command pipeline.
            if (InputHelper.GetKeyDown(KeyCode.F9))
            {
                TryDebugCraftKnownRecipe();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: cycle well repair stage (Fouled → Purified → Repaired → Maintained → Fouled).
            if (InputHelper.GetKeyDown(KeyCode.P))
            {
                TryDebugCycleWellState();
                _lastMoveTime = Time.time;
                return;
            }

            // Interact with an adjacent entity (C key + direction).
            // Dispatches by what's in the target cell:
            //   - ConversationPart  → talk to NPC
            //   - ContainerPart     → open chest/container
            //   - neither           → friendly "nothing there" log
            if (InputHelper.GetKeyDown(KeyCode.C))
            {
                _inputState = InputState.AwaitingTalkDirection;
                MessageLog.Add("Interact — choose a direction.");
                _lastMoveTime = Time.time;
                return;
            }

            int dx = 0, dy = 0;

            // Check movement keys
            if (GetMoveInput(out dx, out dy))
            {
                var oldCell = CurrentZone.GetEntityCell(PlayerEntity);
                int oldX = oldCell?.X ?? -1;
                int oldY = oldCell?.Y ?? -1;

                var (moved, blockedBy) = MovementSystem.TryMoveEx(PlayerEntity, CurrentZone, dx, dy);

                if (moved)
                {
                    var newCell = CurrentZone.GetEntityCell(PlayerEntity);

                    // Refresh only the affected cells for efficiency
                    if (ZoneRenderer != null && oldCell != null && newCell != null)
                        ZoneRenderer.RefreshMovement(oldX, oldY, newCell.X, newCell.Y);

                    EndTurnAndProcess();
                }
                else if (blockedBy != null && blockedBy.HasTag("Creature")
                    && FactionManager.IsHostile(PlayerEntity, blockedBy))
                {
                    // Bump-to-attack: blocked by a hostile creature, perform melee attack
                    CombatSystem.PerformMeleeAttack(PlayerEntity, blockedBy, CurrentZone, _combatRng);
                    EndTurnAndProcess();
                }
                else if (blockedBy == null && ZoneManager != null && WorldMap != null)
                {
                    // Out-of-bounds move — zone transition (no border walls, like Qud)
                    var direction = ZoneTransitionSystem.GetTransitionDirection(oldX, oldY, dx, dy);
                    if (direction.HasValue)
                    {
                        var result = ZoneTransitionSystem.TransitionPlayer(
                            PlayerEntity, CurrentZone, direction.Value,
                            oldX, oldY, ZoneManager, WorldMap);

                        if (result.Success)
                        {
                            HandleZoneTransition(result);
                            EndTurnAndProcess();
                        }
                    }
                }

                _lastMoveTime = Time.time;
            }

            // Ability activation (keys 1-9)
            int abilitySlot = GetAbilitySlotInput();
            if (abilitySlot >= 0)
            {
                _selectedHotbarSlot = abilitySlot;
                SyncHotbarState();
                TryActivateAbility(abilitySlot);
                _lastMoveTime = Time.time;
                return;
            }

            // Pickup item (G or comma)
            if (InputHelper.GetKeyDown(KeyCode.G) || InputHelper.GetKeyDown(KeyCode.Comma))
            {
                TryPickupItem();
                _lastMoveTime = Time.time;
            }

            // Descend stairs (> key = Shift+Period)
            if ((InputHelper.GetKey(KeyCode.LeftShift) || InputHelper.GetKey(KeyCode.RightShift))
                && InputHelper.GetKeyDown(KeyCode.Period))
            {
                TryUseStairs(goingDown: true);
                _lastMoveTime = Time.time;
                return;
            }

            // Ascend stairs (< key = Shift+Comma)
            if ((InputHelper.GetKey(KeyCode.LeftShift) || InputHelper.GetKey(KeyCode.RightShift))
                && InputHelper.GetKeyDown(KeyCode.Comma))
            {
                TryUseStairs(goingDown: false);
                _lastMoveTime = Time.time;
                return;
            }

            // Wait/skip turn
                if (InputHelper.GetKeyDown(KeyCode.Period) || InputHelper.GetKeyDown(KeyCode.Keypad5))
                {
                    EndTurnAndProcess();
                    _lastMoveTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Handle all side effects of a zone transition:
        /// rewire TurnManager, BrainParts, ZoneRenderer, and CurrentZone.
        /// </summary>
        private void HandleZoneTransition(ZoneTransitionResult result)
        {
            if (_inputState == InputState.LookMode)
                ExitLookMode();

            // Remove old zone's non-player creatures from TurnManager
            var oldCreatures = CurrentZone.GetEntitiesWithTag("Creature");
            foreach (var creature in oldCreatures)
            {
                if (creature != PlayerEntity)
                    TurnManager.RemoveEntity(creature);
            }

            // Switch to new zone
            CurrentZone = result.NewZone;
            ZoneManager.SetActiveZone(result.NewZone);

            // Register new zone's creatures in TurnManager
            var newCreatures = result.NewZone.GetEntitiesWithTag("Creature");
            foreach (var creature in newCreatures)
            {
                if (creature != PlayerEntity)
                {
                    TurnManager.AddEntity(creature);
                    var brain = creature.GetPart<BrainPart>();
                    if (brain != null)
                    {
                        brain.CurrentZone = result.NewZone;
                        brain.Rng = new System.Random();
                    }
                }
            }

            // Update renderer
            if (ZoneRenderer != null)
                ZoneRenderer.SetZone(result.NewZone);

            SettlementRuntime.ActiveZone = result.NewZone;

            // Update camera to follow player in new zone
            if (CameraFollow != null)
            {
                CameraFollow.ClearOverrideTarget();
                CameraFollow.CurrentZone = result.NewZone;
                CameraFollow.SnapToPlayer();
            }

            var overworldZoneManager = ZoneManager as OverworldZoneManager;
            if (overworldZoneManager != null && overworldZoneManager.SettlementManager != null)
            {
                overworldZoneManager.SettlementManager.RefreshActiveZonePresentation(result.NewZone);
                var pendingMessages = overworldZoneManager.SettlementManager.ConsumePendingMessages(result.NewZone.ZoneID);
                for (int i = 0; i < pendingMessages.Count; i++)
                    MessageLog.Add(pendingMessages[i]);
            }

            // Trigger fade-from-black visual transition
            if (ScreenFade != null)
                ScreenFade.FadeFromBlack(0.3f);

            Debug.Log($"[Zone] Transitioned to {result.NewZone.ZoneID}");
        }

        private void EndTurnAndProcess()
        {
            TurnManager.EndTurn(PlayerEntity, CurrentZone);
            TurnManager.ProcessUntilPlayerTurn();
            MaterialSimSystem.TickMaterialEntities(CurrentZone);
            RequestZoneRedraw("Turn.Advance");
        }

        private void TryUseStairs(bool goingDown)
        {
            var cell = CurrentZone.GetEntityCell(PlayerEntity);
            if (cell == null) return;

            // Check for stairs entity in current cell
            bool hasStairs = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (goingDown && cell.Objects[i].GetPart<StairsDownPart>() != null)
                { hasStairs = true; break; }
                if (!goingDown && cell.Objects[i].GetPart<StairsUpPart>() != null)
                { hasStairs = true; break; }
            }

            if (!hasStairs)
            {
                MessageLog.Add(goingDown
                    ? "There are no stairs leading down here."
                    : "There are no stairs leading up here.");
                return;
            }

            if (ZoneManager == null) return;

            var result = ZoneTransitionSystem.TransitionPlayerVertical(
                PlayerEntity, CurrentZone, goingDown, cell.X, cell.Y, ZoneManager);

            if (result.Success)
            {
                HandleZoneTransition(result);
                EndTurnAndProcess();
            }
            else
            {
                MessageLog.Add(result.ErrorReason ?? "Cannot use stairs.");
            }
        }

        /// <summary>
        /// Debug utility: grant one random valid mutation to the player and log details.
        /// </summary>
        private void TryDebugGrantRandomMutation()
        {
            var mutations = PlayerEntity.GetPart<MutationsPart>();
            if (mutations == null)
            {
                Debug.LogWarning("[Mutations/Debug] F6 pressed, but player has no MutationsPart.");
                return;
            }

            var pool = mutations.GetMutatePool();
            Debug.Log($"[Mutations/Debug] F6 pressed. Pool size: {pool.Count}. Current mutations: {GetMutationListSummary(mutations)}");

            var granted = mutations.RandomlyMutate(_combatRng);
            if (granted == null)
            {
                Debug.LogWarning("[Mutations/Debug] No mutation granted. Pool may be empty or all options may be blocked by exclusions/ownership.");
                return;
            }

            var grantedPart = mutations.GetMutation(granted.ClassName);
            int level = grantedPart?.Level ?? 0;
            bool affectsBody = grantedPart?.AffectsBodyParts ?? false;
            bool generatesEquipment = grantedPart?.GeneratesEquipment ?? false;

            Debug.Log(
                "[Mutations/Debug] Granted mutation: " +
                $"{granted.DisplayName} ({granted.ClassName}), " +
                $"Level={level}, Category={granted.Category}, Cost={granted.Cost}, " +
                $"AffectsBodyParts={affectsBody}, GeneratesEquipment={generatesEquipment}");

            Debug.Log(
                "[Mutations/Debug] Post-grant state: " +
                $"Mutations={GetMutationListSummary(mutations)} | " +
                $"GeneratedEquipmentTracked={mutations.MutationGeneratedEquipment.Count}");

            RequestZoneRedraw("Debug.GrantMutation");
        }

        /// <summary>
        /// Debug utility: dump the player's full body part tree to the console.
        /// Press F7 in-game to inspect anatomy, equipped items, and dismemberment status.
        /// </summary>
        private void TryDebugDumpBodyParts()
        {
            var body = PlayerEntity.GetPart<Body>();
            if (body == null)
            {
                Debug.Log("[Body/Debug] F7 pressed, but player has no Body part.");
                return;
            }

            var root = body.GetBody();
            if (root == null)
            {
                Debug.Log("[Body/Debug] F7 pressed, but body tree is not initialized.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Body/Debug] === Body Part Tree ===");
            DumpBodyPartTree(sb, root, 0);

            var dismembered = body.DismemberedParts;
            if (dismembered.Count > 0)
            {
                sb.AppendLine($"  --- Dismembered ({dismembered.Count}) ---");
                for (int i = 0; i < dismembered.Count; i++)
                {
                    var dp = dismembered[i];
                    sb.AppendLine($"    [{dp.Part.Type}] {dp.Part.GetDisplayName()} (was on part #{dp.ParentPartID})");
                }
            }

            var allParts = body.GetParts();
            int hands = 0, arms = 0, equipped = 0;
            for (int i = 0; i < allParts.Count; i++)
            {
                if (allParts[i].Type == "Hand") hands++;
                if (allParts[i].Type == "Arm") arms++;
                if (allParts[i]._Equipped != null && allParts[i].FirstSlotForEquipped) equipped++;
            }
            sb.AppendLine($"  --- Summary: {allParts.Count} parts, {arms} arms, {hands} hands, {equipped} items equipped ---");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Debug utility: dismember a random non-mortal appendage from the player.
        /// Press F8 in-game to test severed limb creation and body part loss.
        /// </summary>
        private void TryDebugDismember()
        {
            var body = PlayerEntity.GetPart<Body>();
            if (body == null)
            {
                Debug.Log("[Body/Debug] F8 pressed, but player has no Body part.");
                return;
            }

            var parts = body.GetParts();
            CavesOfOoo.Core.Anatomy.BodyPart target = null;

            // Find the first non-mortal, non-abstract appendage to dismember
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (!p.Mortal && !p.Abstract && p.Appendage)
                {
                    target = p;
                    break;
                }
            }

            if (target == null)
            {
                Debug.Log("[Body/Debug] F8: No non-mortal appendage left to dismember!");
                return;
            }

            Debug.Log($"[Body/Debug] F8: Dismembering \"{target.GetDisplayName()}\" ({target.Type})...");

            bool success = body.Dismember(target, CurrentZone);

            if (success)
            {
                body.UpdateBodyParts();
                Debug.Log($"[Body/Debug] F8: Successfully dismembered \"{target.GetDisplayName()}\". Severed limb placed in zone.");
                Debug.Log("[Body/Debug] F8: Press F7 to inspect updated body tree.");
            }
            else
            {
                Debug.Log($"[Body/Debug] F8: Dismember failed for \"{target.GetDisplayName()}\".");
            }

            RequestZoneRedraw("Debug.Dismember");
        }

        /// <summary>
        /// Debug utility: craft one known recipe through InventoryCommandExecutor.
        /// If no known recipe exists, learns the first recipe from registry and grants exact bits.
        /// </summary>
        private void TryDebugCraftKnownRecipe()
        {
            if (PlayerEntity == null)
            {
                Debug.LogWarning("[Tinkering/Debug] F9 pressed, but PlayerEntity is null.");
                return;
            }

            if (EntityFactory == null)
            {
                Debug.LogWarning("[Tinkering/Debug] F9 pressed, but EntityFactory is null.");
                return;
            }

            var bitLocker = PlayerEntity.GetPart<BitLockerPart>();
            if (bitLocker == null)
            {
                PlayerEntity.AddPart(new BitLockerPart());
                bitLocker = PlayerEntity.GetPart<BitLockerPart>();
                Debug.Log("[Tinkering/Debug] F9: Added BitLockerPart to player.");
            }

            string recipeId = null;
            TinkerRecipe recipe = null;

            foreach (var knownRecipeId in bitLocker.GetKnownRecipes())
            {
                if (TinkerRecipeRegistry.TryGetRecipe(knownRecipeId, out recipe))
                {
                    if (!string.Equals(recipe.Type, "Build", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (EntityFactory.GetBlueprint(recipe.Blueprint) == null)
                        continue;
                    recipeId = knownRecipeId;
                    break;
                }
            }

            if (recipe == null)
            {
                foreach (var candidate in TinkerRecipeRegistry.GetAllRecipes())
                {
                    if (candidate == null || string.IsNullOrWhiteSpace(candidate.Blueprint))
                        continue;

                    if (!string.Equals(candidate.Type, "Build", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (EntityFactory.GetBlueprint(candidate.Blueprint) == null)
                        continue;

                    recipe = candidate;
                    break;
                }

                if (recipe == null || string.IsNullOrWhiteSpace(recipe.ID))
                {
                    Debug.LogWarning("[Tinkering/Debug] F9: No tinkering recipes are loaded.");
                    return;
                }

                recipeId = recipe.ID;
                bitLocker.LearnRecipe(recipeId);
                Debug.Log($"[Tinkering/Debug] F9: Learned fallback recipe '{recipeId}'.");
            }

            string cost = BitCost.Normalize(recipe.Cost);
            if (!string.IsNullOrEmpty(cost) && !bitLocker.HasBits(cost))
            {
                bitLocker.AddBits(cost);
                Debug.Log($"[Tinkering/Debug] F9: Granted bits '{cost}' for debug craft.");
            }

            var result = InventorySystem.ExecuteCommand(
                new CraftFromRecipeCommand(recipeId, EntityFactory),
                PlayerEntity,
                CurrentZone);

            if (!result.Success)
            {
                Debug.LogWarning(
                    "[Tinkering/Debug] F9 craft command failed. " +
                    $"Code={result.ErrorCode}, Message={result.ErrorMessage}");
                return;
            }

            Debug.Log($"[Tinkering/Debug] F9: Crafted recipe '{recipeId}' successfully.");
            RequestZoneRedraw("Debug.Craft");
        }

        /// <summary>
        /// Debug utility: cycle the main well through all repair stages.
        /// F10 advances: Fouled → TemporarilyPurified → StableRepair → ImprovedWithCaretaker → Fouled.
        /// Updates all related state: site stage, settlement conditions, visuals, auras, and ground markers.
        /// </summary>
        private void TryDebugCycleWellState()
        {
            if (CurrentZone == null)
            {
                Debug.LogWarning("[Settlement/Debug] F10 pressed, but CurrentZone is null.");
                return;
            }

            string settlementId = CurrentZone.ZoneID;
            if (SettlementManager.Current == null)
            {
                Debug.LogWarning("[Settlement/Debug] F10 pressed, but no SettlementManager active.");
                return;
            }

            RepairableSiteState site = SettlementManager.Current.GetSite(
                settlementId, SettlementSiteDefinitions.MainWellSiteId);
            if (site == null)
            {
                Debug.LogWarning("[Settlement/Debug] F10 pressed, but no MainWell site in this zone.");
                return;
            }

            // Determine next stage in cycle
            RepairStage nextStage;
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    nextStage = RepairStage.TemporarilyPurified;
                    break;
                case RepairStage.TemporarilyPurified:
                    nextStage = RepairStage.StableRepair;
                    break;
                case RepairStage.StableRepair:
                    nextStage = RepairStage.ImprovedWithCaretaker;
                    break;
                default:
                    nextStage = RepairStage.Fouled;
                    break;
            }

            int currentTurn = SettlementManager.Current.GetCurrentTurn();

            // Apply the new stage with appropriate metadata
            site.Stage = nextStage;
            site.ResolvedAtTurn = currentTurn;
            switch (nextStage)
            {
                case RepairStage.Fouled:
                    site.OutcomeTier = RepairOutcomeTier.None;
                    site.ResolvedByMethod = RepairMethodId.None;
                    site.RelapseAtTurn = null;
                    site.ResolvedAtTurn = -1;
                    break;
                case RepairStage.TemporarilyPurified:
                    site.OutcomeTier = RepairOutcomeTier.Temporary;
                    site.ResolvedByMethod = RepairMethodId.PurifySpell;
                    site.RelapseAtTurn = currentTurn + SettlementRepairDefinitions.PurifyRelapseTurns;
                    break;
                case RepairStage.StableRepair:
                    site.OutcomeTier = RepairOutcomeTier.Stable;
                    site.ResolvedByMethod = RepairMethodId.ManualRepair;
                    site.RelapseAtTurn = null;
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    site.OutcomeTier = RepairOutcomeTier.Improved;
                    site.ResolvedByMethod = RepairMethodId.TeachCaretaker;
                    site.RelapseAtTurn = null;
                    break;
            }

            // Sync settlement conditions (e.g. ImprovedWell condition)
            bool isImproved = nextStage == RepairStage.ImprovedWithCaretaker;
            SettlementManager.Current.SetCondition(
                settlementId, SettlementSiteDefinitions.ImprovedWellCondition, isImproved);

            // Refresh all well-related entity visuals (well + ground markers)
            SettlementManager.Current.RefreshActiveZonePresentation(CurrentZone);

            // Reset proximity messages so the player sees the new ambient text
            foreach (var entity in CurrentZone.GetAllEntities())
            {
                var wellPart = entity.GetPart<WellSitePart>();
                if (wellPart != null)
                    wellPart.ResetProximityMessage();
            }

            // Mark zone dirty so renderer picks up changes
            RequestZoneRedraw("Debug.WellCycle");
            SettlementRuntime.MarkZoneDirty();

            MessageLog.Add($"[Debug] Well stage set to: {nextStage}");
            Debug.Log($"[Settlement/Debug] F10: Cycled well to {nextStage} " +
                $"(OutcomeTier={site.OutcomeTier}, Method={site.ResolvedByMethod}, " +
                $"RelapseAt={site.RelapseAtTurn?.ToString() ?? "none"}, " +
                $"Condition:ImprovedWell={isImproved})");
        }

        private void DumpBodyPartTree(System.Text.StringBuilder sb, CavesOfOoo.Core.Anatomy.BodyPart part, int depth)
        {
            string indent = new string(' ', depth * 2);
            string laterality = part.GetLaterality() != 0
                ? $" [{CavesOfOoo.Core.Anatomy.Laterality.GetAdjective(part.GetLaterality())}]"
                : "";
            string equip = "";
            if (part._Equipped != null)
                equip = $" <- {part._Equipped.GetDisplayName()}{(part.FirstSlotForEquipped ? "" : " (secondary slot)")}";
            string defaultBehavior = "";
            if (part._DefaultBehavior != null)
            {
                var wpn = part._DefaultBehavior.GetPart<CavesOfOoo.Core.MeleeWeaponPart>();
                string dmg = wpn != null ? $" {wpn.BaseDamage}" : "";
                defaultBehavior = $" (natural: {part._DefaultBehavior.GetDisplayName()}{dmg})";
            }
            string flags = "";
            if (part.Primary || part.DefaultPrimary) flags += " *primary*";
            if (part.Mortal) flags += " *mortal*";
            if (part.Appendage) flags += " appendage";
            if (part.Abstract) flags += " abstract";
            if (part.Dynamic) flags += " dynamic";
            if (!string.IsNullOrEmpty(part.Manager)) flags += $" mgr={part.Manager}";

            sb.AppendLine($"  {indent}{part.Type}: \"{part.GetDisplayName()}\"{laterality}{flags}{equip}{defaultBehavior}");

            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    DumpBodyPartTree(sb, part.Parts[i], depth + 1);
            }
        }

        private static string GetMutationListSummary(MutationsPart mutations)
        {
            if (mutations == null || mutations.MutationList.Count == 0)
                return "(none)";

            string summary = "";
            for (int i = 0; i < mutations.MutationList.Count; i++)
            {
                BaseMutation mutation = mutations.MutationList[i];
                if (mutation == null)
                    continue;

                if (summary.Length > 0)
                    summary += ", ";
                summary += mutation.DisplayName + " L" + mutation.Level;
            }

            return string.IsNullOrEmpty(summary) ? "(none)" : summary;
        }

        /// <summary>
        /// Pick up items at the player's feet.
        /// Single item: auto-pickup immediately.
        /// Multiple items: open the pickup popup for individual selection.
        /// </summary>
        private void TryPickupItem()
        {
            var items = InventorySystem.GetTakeableItemsAtFeet(PlayerEntity, CurrentZone);
            if (items.Count == 0)
            {
                var containers = InventorySystem.GetContainersAtFeet(PlayerEntity, CurrentZone);
                if (containers.Count == 0)
                {
                    Debug.Log("[Inventory] Nothing to pick up here.");
                }
                else
                {
                    if (containers.Count == 1)
                    {
                        if (TryTakeAllFromContainerViaCommand(containers[0]))
                        {
                            EndTurnAndProcess();
                            RequestZoneRedraw("Inventory.TakeAllFromContainer");
                        }
                    }
                    else
                    {
                        OpenContainerPicker(containers);
                    }
                }
                return;
            }

            if (items.Count == 1)
            {
                // Single item: auto-pickup
                var item = items[0];
                if (TryPickupViaCommand(item))
                {
                    EndTurnAndProcess();
                    RequestZoneRedraw("Inventory.Pickup");
                }
            }
            else
            {
                // Multiple items: open pickup popup
                OpenPickup(items);
            }
        }

        /// <summary>
        /// Command-first pickup seam.
        /// </summary>
        private bool TryPickupViaCommand(Entity item)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new PickupCommand(item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;

            Debug.LogWarning(
                "[Inventory/Refactor] Pickup command failed. " +
                $"Code={result.ErrorCode}, Message={result.ErrorMessage}");
            return false;
        }

        /// <summary>
        /// Try taking all contents from one container through command execution.
        /// </summary>
        private bool TryTakeAllFromContainerViaCommand(Entity container)
        {
            if (container == null)
                return false;
            var containerPart = container.GetPart<ContainerPart>();
            if (containerPart == null)
                return false;
            if (containerPart.Contents.Count == 0)
            {
                MessageLog.Add($"The {container.GetDisplayName()} is empty.");
                return false;
            }

            int taken = 0;
            var snapshot = new List<Entity>(containerPart.Contents);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var item = snapshot[i];
                var result = InventorySystem.ExecuteCommand(
                    new TakeFromContainerCommand(container, item),
                    PlayerEntity,
                    CurrentZone);

                if (result.Success)
                {
                    taken++;
                    continue;
                }

                Debug.LogWarning(
                    "[Inventory/Refactor] TakeFromContainer command failed. " +
                    $"Code={result.ErrorCode}, Message={result.ErrorMessage}");

                // Match InventorySystem.TakeAllFromContainer parity: stop on first failure.
                break;
            }

            return taken > 0;
        }

        private void OpenContainerPicker(List<Entity> containers)
        {
            if (ContainerPickerUI == null || containers == null || containers.Count == 0)
                return;

            EnterCenteredPopupOverlayView();
            ContainerPickerUI.Open(containers);
            _inputState = InputState.ContainerPickerOpen;
        }

        private void OpenPickup(List<Entity> items)
        {
            if (PickupUI == null) return;
            EnterCenteredPopupOverlayView();
            PickupUI.PlayerEntity = PlayerEntity;
            PickupUI.CurrentZone = CurrentZone;
            PickupUI.Open(items);
            _inputState = InputState.PickupOpen;
        }

        private void HandlePickupInput()
        {
            if (PickupUI == null || !PickupUI.IsOpen)
            {
                ClosePickup();
                return;
            }

            PickupUI.HandleInput();

            if (!PickupUI.IsOpen)
                ClosePickup();
        }

        private void ClosePickup()
        {
            bool pickedUpAny = PickupUI != null && PickupUI.PickedUpAny;

            if (pickedUpAny)
                EndTurnAndProcess();

            _inputState = InputState.Normal;

            if (TryOpenAnnouncement())
                return;

            ExitCenteredPopupOverlayViewToGameplay();
        }

        private void HandleContainerPickerInput()
        {
            if (ContainerPickerUI == null || !ContainerPickerUI.IsOpen)
            {
                CloseContainerPicker(false);
                return;
            }

            ContainerPickerUI.HandleInput();

            if (ContainerPickerUI.IsOpen)
                return;

            bool tookAny = false;
            if (ContainerPickerUI.SelectionMade)
                tookAny = TryTakeAllFromContainerViaCommand(ContainerPickerUI.SelectedContainer);

            CloseContainerPicker(tookAny);
        }

        private void CloseContainerPicker(bool tookAny)
        {
            if (tookAny)
                EndTurnAndProcess();

            _inputState = InputState.Normal;

            if (TryOpenAnnouncement())
                return;

            ExitCenteredPopupOverlayViewToGameplay();
        }

        /// <summary>
        /// Check if a number key 1-0 was pressed. Returns a 0-based slot index, or -1 if none.
        /// </summary>
        private int GetAbilitySlotInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (InputHelper.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (InputHelper.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (InputHelper.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (InputHelper.GetKeyDown(KeyCode.Alpha5)) return 4;
            if (InputHelper.GetKeyDown(KeyCode.Alpha6)) return 5;
            if (InputHelper.GetKeyDown(KeyCode.Alpha7)) return 6;
            if (InputHelper.GetKeyDown(KeyCode.Alpha8)) return 7;
            if (InputHelper.GetKeyDown(KeyCode.Alpha9)) return 8;
            if (InputHelper.GetKeyDown(KeyCode.Alpha0)) return 9;
            return -1;
        }

        private bool EnterLookMode()
        {
            if (_inputState != InputState.Normal || CurrentZone == null || PlayerEntity == null)
                return false;

            Cell playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null)
                return false;

            _worldCursorState.Activate(
                WorldCursorMode.Look,
                CurrentZone,
                playerCell.X,
                playerCell.Y,
                playerCell.X,
                playerCell.Y,
                followMouse: true);

            _lastLookMousePosition = Input.mousePosition;
            _inputState = InputState.LookMode;
            ClampLookCursorToVisibleFrame();
            RefreshLookSnapshot();

            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
            return true;
        }

        private void ExitLookMode()
        {
            _worldCursorState.Deactivate();
            _inputState = InputState.Normal;
            ZoneRenderer?.ClearWorldCursor();
            ZoneRenderer?.ClearLookSnapshot();
            if (CameraFollow != null)
            {
                CameraFollow.ClearOverrideTarget();
            }
        }

        private void HandleLookModeInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                ExitLookMode();
                _lastMoveTime = Time.time;
                return;
            }

            bool shiftHeld = InputHelper.GetKey(KeyCode.LeftShift) || InputHelper.GetKey(KeyCode.RightShift);
            if (InputHelper.GetKeyDown(KeyCode.Period) && !shiftHeld)
            {
                RecenterLookCursor();
                _lastMoveTime = Time.time;
                return;
            }

            if (TryHandleSidebarLogScrollInput())
                return;

            int dx = 0;
            int dy = 0;
            if (GetDirectionKeyDown(out dx, out dy))
            {
                MoveLookCursor(dx, dy);
                _lastMoveTime = Time.time;
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                TryOpenWorldThrowPopup(_worldCursorState.X, _worldCursorState.Y);
                _lastMoveTime = Time.time;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (ZoneRenderer != null &&
                    ZoneRenderer.ScreenToZoneCell(Input.mousePosition, Camera.main, out int clickX, out int clickY))
                {
                    _worldCursorState.SetPosition(clickX, clickY);
                    ClampLookCursorToVisibleFrame();
                    RefreshLookSnapshot();
                    TryOpenWorldThrowPopup(_worldCursorState.X, _worldCursorState.Y);
                    _lastMoveTime = Time.time;
                    return;
                }
            }

            TryUpdateLookCursorFromMouse();
        }

        private bool TryHandleSidebarLogScrollInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Equals))
                return TryHandleSidebarLogScrollCommand(older: true);

            if (InputHelper.GetKeyDown(KeyCode.Minus))
                return TryHandleSidebarLogScrollCommand(older: false);

            return false;
        }

        private bool TryHandleHotbarInput()
        {
            if (_inputState != InputState.Normal)
                return false;

            if (InputHelper.GetKeyDown(KeyCode.LeftBracket))
            {
                CycleHotbarSelection(-1);
                return true;
            }

            if (InputHelper.GetKeyDown(KeyCode.RightBracket))
            {
                CycleHotbarSelection(1);
                return true;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                ActivateSelectedHotbarSlot();
                return true;
            }

            if (Input.GetMouseButtonDown(0) &&
                ZoneRenderer != null &&
                ZoneRenderer.TryGetHotbarSlotAtScreenPosition(Input.mousePosition, out int clickedSlot))
            {
                _selectedHotbarSlot = clickedSlot;
                SyncHotbarState();
                TryActivateAbility(clickedSlot);
                return true;
            }

            return false;
        }

        private bool TryHandleSidebarLogScrollCommand(bool older)
        {
            if ((_inputState != InputState.Normal && _inputState != InputState.LookMode) || ZoneRenderer == null)
                return false;

            return older
                ? ZoneRenderer.ScrollSidebarLogOlder()
                : ZoneRenderer.ScrollSidebarLogNewer();
        }

        private void RecenterLookCursor()
        {
            Cell playerCell = CurrentZone?.GetEntityCell(PlayerEntity);
            if (playerCell == null)
                return;

            _worldCursorState.SetPosition(playerCell.X, playerCell.Y);
            ClampLookCursorToVisibleFrame();
            RefreshLookSnapshot();
        }

        private void MoveLookCursor(int dx, int dy)
        {
            if (!_worldCursorState.Active)
                return;

            _worldCursorState.MoveBy(dx, dy);
            ClampLookCursorToVisibleFrame();
            RefreshLookSnapshot();
        }

        private void TryUpdateLookCursorFromMouse()
        {
            if (!_worldCursorState.Active ||
                !_worldCursorState.FollowMouse ||
                ZoneRenderer == null)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            if (mouse == _lastLookMousePosition)
                return;

            _lastLookMousePosition = mouse;

            if (!ZoneRenderer.ScreenToZoneCell(mouse, Camera.main, out int x, out int y))
                return;

            if (x == _worldCursorState.X && y == _worldCursorState.Y)
                return;

            _worldCursorState.SetPosition(x, y);
            ClampLookCursorToVisibleFrame();
            RefreshLookSnapshot();
        }

        private void RefreshLookSnapshot()
        {
            if (!_worldCursorState.Active)
                return;

            LookSnapshot snapshot = LookQueryService.BuildSnapshot(
                PlayerEntity,
                _worldCursorState.Zone,
                _worldCursorState.X,
                _worldCursorState.Y);

            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
            ZoneRenderer?.SetLookSnapshot(snapshot);
        }

        private void ClampLookCursorToVisibleFrame()
        {
            if (!_worldCursorState.Active || ZoneRenderer == null)
                return;

            if (!ZoneRenderer.TryGetVisibleZoneBounds(Camera.main, out int minX, out int maxX, out int minY, out int maxY))
                return;

            int clampedX = _worldCursorState.X;
            int clampedY = _worldCursorState.Y;

            if (clampedX < minX)
                clampedX = minX;
            else if (clampedX > maxX)
                clampedX = maxX;

            if (clampedY < minY)
                clampedY = minY;
            else if (clampedY > maxY)
                clampedY = maxY;

            if (clampedX != _worldCursorState.X || clampedY != _worldCursorState.Y)
                _worldCursorState.SetPosition(clampedX, clampedY);
        }

        private bool BeginThrowTargeting(
            Entity item,
            ThrowOriginKind origin,
            int sourceTileX,
            int sourceTileY,
            bool startAtPlayerCell)
        {
            if (item == null || CurrentZone == null || PlayerEntity == null)
                return false;

            Cell playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null)
                return false;

            int startX = startAtPlayerCell ? playerCell.X : sourceTileX;
            int startY = startAtPlayerCell ? playerCell.Y : sourceTileY;
            if (!CurrentZone.InBounds(startX, startY))
            {
                startX = playerCell.X;
                startY = playerCell.Y;
            }

            _pendingThrowTarget = new ThrowTargetState
            {
                Item = item,
                Origin = origin,
                SourceTileX = sourceTileX,
                SourceTileY = sourceTileY
            };

            int maxRange = HandlingService.GetThrowRange(PlayerEntity, item);
            _worldCursorState.Activate(
                WorldCursorMode.ThrowTarget,
                CurrentZone,
                startX,
                startY,
                playerCell.X,
                playerCell.Y,
                maxRange,
                followMouse: true);

            _inputState = InputState.ThrowTargeting;
            _lastLookMousePosition = Input.mousePosition;
            ZoneRenderer?.ClearLookSnapshot();
            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
            MessageLog.Add($"Throw {item.GetDisplayName()} - choose a target (range {maxRange}).");
            return true;
        }

        private void HandleThrowTargetingInput()
        {
            if (_pendingThrowTarget == null)
            {
                ExitThrowTargetingToNormal();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                CancelThrowTargeting();
                _lastMoveTime = Time.time;
                return;
            }

            int dx = 0;
            int dy = 0;
            if (GetDirectionKeyDown(out dx, out dy))
            {
                MoveThrowCursor(dx, dy);
                _lastMoveTime = Time.time;
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmThrowTarget();
                _lastMoveTime = Time.time;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (TryUpdateThrowCursorFromMouse(applyOnHover: false))
                {
                    ConfirmThrowTarget();
                    _lastMoveTime = Time.time;
                    return;
                }
            }

            TryUpdateThrowCursorFromMouse(applyOnHover: true);
        }

        private void MoveThrowCursor(int dx, int dy)
        {
            if (!_worldCursorState.Active)
                return;

            int nextX = _worldCursorState.X + dx;
            int nextY = _worldCursorState.Y + dy;
            if (!CurrentZone.InBounds(nextX, nextY))
                return;

            if (!IsWithinThrowRange(nextX, nextY))
                return;

            _worldCursorState.SetPosition(nextX, nextY);
            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
        }

        private bool TryUpdateThrowCursorFromMouse(bool applyOnHover)
        {
            if (!_worldCursorState.Active || ZoneRenderer == null)
                return false;

            Vector3 mouse = Input.mousePosition;
            if (applyOnHover && mouse == _lastLookMousePosition)
                return false;

            _lastLookMousePosition = mouse;

            if (!ZoneRenderer.ScreenToZoneCell(mouse, Camera.main, out int x, out int y))
                return false;

            if (!IsWithinThrowRange(x, y))
                return false;

            if (_worldCursorState.X == x && _worldCursorState.Y == y)
                return true;

            _worldCursorState.SetPosition(x, y);
            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
            return true;
        }

        private bool IsWithinThrowRange(int x, int y)
        {
            if (!_worldCursorState.Active)
                return false;

            int maxRange = _worldCursorState.MaxRange ?? 0;
            return AIHelpers.ChebyshevDistance(_worldCursorState.AnchorX, _worldCursorState.AnchorY, x, y) <= maxRange;
        }

        private void ConfirmThrowTarget()
        {
            if (_pendingThrowTarget == null)
                return;

            var result = InventorySystem.ExecuteCommand(
                new ThrowItemCommand(_pendingThrowTarget.Item, _worldCursorState.X, _worldCursorState.Y),
                PlayerEntity,
                CurrentZone);

            if (!result.Success)
            {
                MessageLog.Add(result.ErrorMessage);
                return;
            }

            ExitThrowTargetingToNormal();
            _inputState = InputState.WaitingForFxResolution;
        }

        private void CancelThrowTargeting()
        {
            if (_pendingThrowTarget == null)
            {
                ExitThrowTargetingToNormal();
                return;
            }

            var cancelled = _pendingThrowTarget;
            ExitThrowTargetingToNormal();

            if (cancelled.Origin == ThrowOriginKind.Inventory)
            {
                OpenInventory();
                InventoryUI?.ReopenItemActionPopupFor(cancelled.Item);
                return;
            }

            RestoreLookModeAt(cancelled.SourceTileX, cancelled.SourceTileY);
        }

        private void ExitThrowTargetingToNormal()
        {
            _pendingThrowTarget = null;
            _worldCursorState.Deactivate();
            ZoneRenderer?.ClearWorldCursor();
            ZoneRenderer?.ClearLookSnapshot();
            _inputState = InputState.Normal;
        }

        private void RestoreLookModeAt(int x, int y)
        {
            if (CurrentZone == null || PlayerEntity == null)
                return;

            Cell playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null)
                return;

            if (!CurrentZone.InBounds(x, y))
            {
                x = playerCell.X;
                y = playerCell.Y;
            }

            _worldCursorState.Activate(
                WorldCursorMode.Look,
                CurrentZone,
                x,
                y,
                playerCell.X,
                playerCell.Y,
                followMouse: true);

            _lastLookMousePosition = Input.mousePosition;
            _inputState = InputState.LookMode;
            ClampLookCursorToVisibleFrame();
            RefreshLookSnapshot();
            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
        }

        private bool TryOpenWorldThrowPopup(int tileX, int tileY)
        {
            Cell actorCell = CurrentZone?.GetEntityCell(PlayerEntity);
            if (actorCell == null)
                return false;

            if (!IsSameOrCardinalAdjacent(actorCell.X, actorCell.Y, tileX, tileY))
                return false;

            var throwableItems = GetVisibleThrowableWorldItems(tileX, tileY);
            if (throwableItems.Count == 0)
            {
                MessageLog.Add("Nothing here can be thrown.");
                return true;
            }

            if (throwableItems.Count == 1)
            {
                OpenWorldThrowActionPopup(throwableItems[0], tileX, tileY);
                return true;
            }

            OpenWorldThrowSourcePicker(throwableItems, tileX, tileY);
            return true;
        }

        private List<Entity> GetVisibleThrowableWorldItems(int tileX, int tileY)
        {
            var result = new List<Entity>();
            Cell cell = CurrentZone?.GetCell(tileX, tileY);
            if (cell == null || !cell.IsVisible)
                return result;

            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                Entity item = cell.Objects[i];
                if (item == null || item == PlayerEntity)
                    continue;

                var render = item.GetPart<RenderPart>();
                if (render != null && !render.Visible)
                    continue;

                if (HandlingService.CanThrow(PlayerEntity, item, out _))
                    result.Add(item);
            }

            return result;
        }

        private static bool IsSameOrCardinalAdjacent(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x2 - x1) + Math.Abs(y2 - y1) <= 1;
        }

        private void OpenWorldThrowSourcePicker(List<Entity> items, int tileX, int tileY)
        {
            _throwPopup = new ThrowPopupState
            {
                Kind = ThrowPopupKind.SourcePicker,
                Title = "Choose an object",
                SourceTileX = tileX,
                SourceTileY = tileY,
                CursorIndex = 0,
                ScrollOffset = 0
            };

            for (int i = 0; i < items.Count; i++)
            {
                _throwPopup.Options.Add(new ThrowPopupOption
                {
                    Item = items[i],
                    Label = items[i].GetDisplayName()
                });
            }

            OpenThrowPopup();
        }

        private void OpenWorldThrowActionPopup(Entity item, int tileX, int tileY)
        {
            if (item == null)
                return;

            _throwPopup = new ThrowPopupState
            {
                Kind = ThrowPopupKind.ActionPicker,
                Title = item.GetDisplayName(),
                SourceTileX = tileX,
                SourceTileY = tileY,
                SelectedItem = item,
                CursorIndex = 0,
                ScrollOffset = 0
            };

            _throwPopup.Options.Add(new ThrowPopupOption
            {
                Item = item,
                Label = "throw"
            });

            OpenThrowPopup();
        }

        private void OpenThrowPopup()
        {
            if (_throwPopup == null)
                return;

            _inputState = InputState.ThrowPopupOpen;
            EnterCenteredPopupOverlayView();
            RenderThrowPopup();
        }

        private void HandleThrowPopupInput()
        {
            if (_throwPopup == null)
            {
                CloseThrowPopup();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                CloseThrowPopup();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetThrowPopupRowAtMouse();
                if (clickedRow >= 0)
                {
                    _throwPopup.CursorIndex = clickedRow;
                    ExecuteThrowPopupSelection();
                    return;
                }

                CloseThrowPopup();
                return;
            }

            if ((InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)) && _throwPopup.CursorIndex > 0)
            {
                _throwPopup.CursorIndex--;
                ClampThrowPopupScroll();
                RenderThrowPopup();
                return;
            }

            if ((InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J)) && _throwPopup.CursorIndex < _throwPopup.Options.Count - 1)
            {
                _throwPopup.CursorIndex++;
                ClampThrowPopupScroll();
                RenderThrowPopup();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                ExecuteThrowPopupSelection();
                return;
            }
        }

        private void ExecuteThrowPopupSelection()
        {
            if (_throwPopup == null || _throwPopup.CursorIndex < 0 || _throwPopup.CursorIndex >= _throwPopup.Options.Count)
                return;

            if (_throwPopup.Kind == ThrowPopupKind.SourcePicker)
            {
                Entity selectedItem = _throwPopup.Options[_throwPopup.CursorIndex].Item;
                int sourceX = _throwPopup.SourceTileX;
                int sourceY = _throwPopup.SourceTileY;
                OpenWorldThrowActionPopup(selectedItem, sourceX, sourceY);
                return;
            }

            Entity item = _throwPopup.SelectedItem ?? _throwPopup.Options[_throwPopup.CursorIndex].Item;
            int tileX = _throwPopup.SourceTileX;
            int tileY = _throwPopup.SourceTileY;
            ClearThrowPopupTiles();
            ExitCenteredPopupOverlayViewToGameplay();
            _throwPopup = null;
            BeginThrowTargeting(item, ThrowOriginKind.LookWorld, tileX, tileY, startAtPlayerCell: false);
        }

        private void CloseThrowPopup()
        {
            ClearThrowPopupTiles();
            ExitCenteredPopupOverlayViewToGameplay();

            if (_throwPopup != null)
                RestoreLookModeAt(_throwPopup.SourceTileX, _throwPopup.SourceTileY);
            else
                _inputState = InputState.LookMode;

            _throwPopup = null;
        }

        private void ClampThrowPopupScroll()
        {
            if (_throwPopup == null)
                return;

            const int visibleCount = 10;
            if (_throwPopup.CursorIndex < _throwPopup.ScrollOffset)
                _throwPopup.ScrollOffset = _throwPopup.CursorIndex;
            else if (_throwPopup.CursorIndex >= _throwPopup.ScrollOffset + visibleCount)
                _throwPopup.ScrollOffset = _throwPopup.CursorIndex - visibleCount + 1;
        }

        private void RenderThrowPopup()
        {
            if (_throwPopup == null || ZoneRenderer?.CenteredPopupFgTilemap == null)
                return;

            ClearThrowPopupTiles();

            const int popupWidth = 36;
            const int maxVisible = 10;
            int totalRows = _throwPopup.Options.Count;
            int visibleRows = Math.Min(Math.Max(totalRows, 1), maxVisible);
            ClampThrowPopupScroll();
            int popupHeight = visibleRows + 4;
            int popupX = CenteredPopupLayout.GetCenteredOriginX(popupWidth);
            int popupTopY = CenteredPopupLayout.GetCenteredTopY(popupHeight);
            DrawThrowPopupBackground(popupX, popupTopY, popupWidth, popupHeight);
            DrawThrowPopupBorder(popupX, popupTopY, popupWidth, popupHeight);

            string title = _throwPopup.Title ?? "Throw";
            if (title.Length > popupWidth - 4)
                title = title.Substring(0, popupWidth - 4);
            DrawThrowPopupText(popupX + 2, popupTopY - 1, title, QudColorParser.BrightYellow);

            int contentTopY = popupTopY - 3;
            if (totalRows == 0)
            {
                DrawThrowPopupText(popupX + 2, contentTopY, "(no actions available)", QudColorParser.DarkGray);
                return;
            }

            for (int i = 0; i < visibleRows; i++)
            {
                int rowIndex = _throwPopup.ScrollOffset + i;
                if (rowIndex >= totalRows)
                    break;

                int rowY = contentTopY - i;
                bool selected = rowIndex == _throwPopup.CursorIndex;
                if (selected)
                    DrawThrowPopupChar(popupX + 1, rowY, '>', QudColorParser.White);

                if (rowIndex < 26)
                {
                    char hotkey = (char)('a' + rowIndex);
                    DrawThrowPopupText(popupX + 2, rowY, hotkey + ")", selected ? QudColorParser.White : QudColorParser.Gray);
                }

                string label = _throwPopup.Options[rowIndex].Label ?? string.Empty;
                if (label.Length > popupWidth - 8)
                    label = label.Substring(0, popupWidth - 9) + "~";
                DrawThrowPopupText(popupX + 5, rowY, label, selected ? QudColorParser.White : QudColorParser.Gray);
            }
        }

        private void DrawThrowPopupBorder(int popupX, int popupTopY, int popupWidth, int popupHeight)
        {
            for (int dx = 0; dx < popupWidth; dx++)
            {
                DrawThrowPopupChar(popupX + dx, popupTopY, dx == 0 || dx == popupWidth - 1 ? '+' : '-', QudColorParser.Gray);
                DrawThrowPopupChar(popupX + dx, popupTopY - popupHeight + 1, dx == 0 || dx == popupWidth - 1 ? '+' : '-', QudColorParser.Gray);
            }

            for (int dy = 1; dy < popupHeight - 1; dy++)
            {
                DrawThrowPopupChar(popupX, popupTopY - dy, '|', QudColorParser.Gray);
                DrawThrowPopupChar(popupX + popupWidth - 1, popupTopY - dy, '|', QudColorParser.Gray);
            }
        }

        private void DrawThrowPopupBackground(int popupX, int popupTopY, int popupWidth, int popupHeight)
        {
            var bgTilemap = ZoneRenderer?.CenteredPopupBgTilemap;
            if (bgTilemap == null)
                return;

            var blockTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null)
                return;

            Color bgColor = new Color(0f, 0f, 0f, 1f);
            for (int dy = 0; dy < popupHeight; dy++)
            {
                for (int dx = 0; dx < popupWidth; dx++)
                {
                    var pos = new Vector3Int(popupX + dx, popupTopY - dy, 0);
                    bgTilemap.SetTile(pos, blockTile);
                    bgTilemap.SetTileFlags(pos, UnityEngine.Tilemaps.TileFlags.None);
                    bgTilemap.SetColor(pos, bgColor);
                }
            }
        }

        private void DrawThrowPopupText(int x, int y, string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                    continue;
                DrawThrowPopupChar(x + i, y, text[i], color);
            }
        }

        private void DrawThrowPopupChar(int x, int y, char c, Color color)
        {
            var tilemap = ZoneRenderer?.CenteredPopupFgTilemap;
            if (tilemap == null)
                return;

            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null)
                return;

            var pos = new Vector3Int(x, y, 0);
            tilemap.SetTile(pos, tile);
            tilemap.SetTileFlags(pos, UnityEngine.Tilemaps.TileFlags.None);
            tilemap.SetColor(pos, color);
        }

        private void ClearThrowPopupTiles()
        {
            ZoneRenderer?.CenteredPopupFgTilemap?.ClearAllTiles();
            ZoneRenderer?.CenteredPopupBgTilemap?.ClearAllTiles();
        }

        private int GetThrowPopupRowAtMouse()
        {
            if (_throwPopup == null ||
                ZoneRenderer?.PopupOverlayCamera == null ||
                ZoneRenderer.CenteredPopupFgTilemap == null ||
                !CenteredPopupLayout.ScreenToGrid(
                    ZoneRenderer.PopupOverlayCamera,
                    ZoneRenderer.CenteredPopupFgTilemap,
                    Input.mousePosition,
                    out int gridX,
                    out int gridY))
            {
                return -1;
            }

            const int popupWidth = 36;
            const int maxVisible = 10;
            int totalRows = _throwPopup.Options.Count;
            int visibleRows = Math.Min(Math.Max(totalRows, 1), maxVisible);
            int popupHeight = visibleRows + 4;
            int popupX = CenteredPopupLayout.GetCenteredOriginX(popupWidth);
            int popupTopY = CenteredPopupLayout.GetCenteredTopY(popupHeight);
            int contentTopY = popupTopY - 3;

            if (gridX < popupX || gridX >= popupX + popupWidth)
                return -1;

            if (gridY > contentTopY || gridY <= contentTopY - visibleRows)
                return -1;

            int visibleIndex = contentTopY - gridY;
            int rowIndex = _throwPopup.ScrollOffset + visibleIndex;
            return rowIndex >= 0 && rowIndex < totalRows ? rowIndex : -1;
        }

        /// <summary>
        /// Try to activate an ability by slot. Directional abilities enter targeting mode;
        /// self-centered abilities resolve immediately.
        /// </summary>
        private void TryActivateAbility(int slot)
        {
            var abilities = PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null)
            {
                MessageLog.Add("You know no rites.");
                return;
            }

            var ability = abilities.GetAbilityBySlot(slot);
            if (ability == null)
            {
                MessageLog.Add("No rite bound to [" + HotbarStateBuilder.SlotToHotkey(slot) + "].");
                return;
            }

            if (!ability.IsUsable)
            {
                MessageLog.Add(GetAbilityDisplayName(ability) + " is on cooldown (" + ability.CooldownRemaining + " turns remaining).");
                return;
            }

            Cell playerCell = CurrentZone?.GetEntityCell(PlayerEntity);
            if (playerCell == null)
            {
                MessageLog.Add("You have no valid source cell.");
                return;
            }

            if (ability.TargetingMode == AbilityTargetingMode.SelfCentered)
            {
                ResolveAbilityCommand(ability, playerCell, 0, 0, null);
                return;
            }

            // Enter targeting mode
            _pendingAbility = ability;
            _inputState = InputState.AwaitingDirection;
            SyncHotbarState();
            MessageLog.Add(GetAbilityDisplayName(ability) + " - choose a direction.");
        }

        /// <summary>
        /// Handle input while awaiting a direction for ability targeting.
        /// Escape cancels, directional input fires the ability command.
        /// </summary>
        private void HandleAwaitingDirection()
        {
            // Cancel on Escape
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                MessageLog.Add("Cancelled.");
                _inputState = InputState.Normal;
                _pendingAbility = null;
                SyncHotbarState();
                return;
            }

            int dx = 0, dy = 0;
            if (!GetDirectionKeyDown(out dx, out dy))
                return;

            var playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null)
            {
                _inputState = InputState.Normal;
                _pendingAbility = null;
                SyncHotbarState();
                return;
            }

            int targetX = playerCell.X + dx;
            int targetY = playerCell.Y + dy;
            if (!CurrentZone.InBounds(targetX, targetY))
            {
                MessageLog.Add("Invalid target.");
                _inputState = InputState.Normal;
                _pendingAbility = null;
                SyncHotbarState();
                return;
            }

            var targetCell = _pendingAbility.TargetingMode == AbilityTargetingMode.AdjacentCell
                ? CurrentZone.GetCell(targetX, targetY)
                : null;
            if (_pendingAbility.TargetingMode == AbilityTargetingMode.AdjacentCell && targetCell == null)
            {
                MessageLog.Add("Invalid target.");
                _inputState = InputState.Normal;
                _pendingAbility = null;
                SyncHotbarState();
                return;
            }

            ResolveAbilityCommand(_pendingAbility, playerCell, dx, dy, targetCell);
        }

        private void HandleWaitingForFxResolution()
        {
            if (ZoneRenderer != null && ZoneRenderer.HasBlockingFx)
                return;

            _inputState = InputState.Normal;
            _pendingAbility = null;
            EndTurnAndProcess();
            _lastMoveTime = Time.time;
        }

        private void ResolveAbilityCommand(ActivatedAbility ability, Cell sourceCell, int dx, int dy, Cell targetCell)
        {
            if (ability == null || sourceCell == null || CurrentZone == null)
                return;

            var cmd = GameEvent.New(ability.Command);
            cmd.SetParameter("Zone", (object)CurrentZone);
            cmd.SetParameter("RNG", (object)_combatRng);
            cmd.SetParameter("SourceCell", (object)sourceCell);
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            cmd.SetParameter("Range", ability.Range);

            if (ability.TargetingMode == AbilityTargetingMode.AdjacentCell && targetCell != null)
                cmd.SetParameter("TargetCell", (object)targetCell);

            PlayerEntity.FireEvent(cmd);

            bool handled = cmd.Handled;
            _pendingAbility = null;
            _inputState = InputState.Normal;

            if (!handled)
            {
                MessageLog.Add("The rite fails to resolve.");
                _lastMoveTime = Time.time;
                SyncHotbarState();
                return;
            }

            if (cmd.GetParameter<bool>("BlocksTurnAdvance"))
            {
                _inputState = InputState.WaitingForFxResolution;
                _lastMoveTime = Time.time;
                SyncHotbarState();
                return;
            }

            EndTurnAndProcess();
            _lastMoveTime = Time.time;
            SyncHotbarState();
        }

        private void OpenInventory()
        {
            if (InventoryUI == null) return;
            InventoryUI.PlayerEntity = PlayerEntity;
            InventoryUI.CurrentZone = CurrentZone;
            InventoryUI.Open();
            _inputState = InputState.InventoryOpen;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            if (CameraFollow != null) CameraFollow.SetUIView(FullscreenUiGridWidth, FullscreenUiGridHeight);
        }

        private void HandleInventoryInput()
        {
            if (InventoryUI == null || !InventoryUI.IsOpen)
            {
                CloseInventory();
                return;
            }

            InventoryUI.HandleInput();

            var throwRequest = InventoryUI.ConsumePendingThrowRequest();
            if (throwRequest != null && throwRequest.Item != null)
            {
                CloseInventory();
                BeginThrowTargeting(
                    throwRequest.Item,
                    ThrowOriginKind.Inventory,
                    sourceTileX: -1,
                    sourceTileY: -1,
                    startAtPlayerCell: true);
                return;
            }

            if (!InventoryUI.IsOpen)
                CloseInventory();
        }

        private void CloseInventory()
        {
            _inputState = InputState.Normal;
            if (CameraFollow != null) CameraFollow.RestoreGameView();
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty("UI.Inventory.Close");
            }
        }

        private void OpenFaction()
        {
            if (FactionUI == null) return;
            FactionUI.PlayerEntity = PlayerEntity;
            FactionUI.Open();
            _inputState = InputState.FactionOpen;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            if (CameraFollow != null) CameraFollow.SetUIView(FullscreenUiGridWidth, FullscreenUiGridHeight);
        }

        private void HandleFactionInput()
        {
            if (FactionUI == null || !FactionUI.IsOpen)
            {
                CloseFaction();
                return;
            }

            FactionUI.HandleInput();

            if (!FactionUI.IsOpen)
                CloseFaction();
        }

        private void CloseFaction()
        {
            _inputState = InputState.Normal;
            if (CameraFollow != null) CameraFollow.RestoreGameView();
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty("UI.Faction.Close");
            }
        }

        /// <summary>
        /// Read directional input (KeyDown only, for single-press targeting).
        /// Returns true if a direction was pressed.
        /// </summary>
        private bool GetDirectionKeyDown(out int dx, out int dy)
        {
            dx = 0;
            dy = 0;

            // Cardinal
            if (InputHelper.GetKeyDown(KeyCode.W) || InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.Keypad8) || InputHelper.GetKeyDown(KeyCode.K))
            { dy = -1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.S) || InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.Keypad2) || InputHelper.GetKeyDown(KeyCode.J))
            { dy = 1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.A) || InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.Keypad4) || InputHelper.GetKeyDown(KeyCode.H))
            { dx = -1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.D) || InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.Keypad6) || InputHelper.GetKeyDown(KeyCode.L))
            { dx = 1; return true; }

            // Diagonals
            if (InputHelper.GetKeyDown(KeyCode.Keypad7) || InputHelper.GetKeyDown(KeyCode.Y))
            { dx = -1; dy = -1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.Keypad9) || InputHelper.GetKeyDown(KeyCode.U))
            { dx = 1; dy = -1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.Keypad1) || InputHelper.GetKeyDown(KeyCode.B))
            { dx = -1; dy = 1; return true; }

            if (InputHelper.GetKeyDown(KeyCode.Keypad3) || InputHelper.GetKeyDown(KeyCode.N))
            { dx = 1; dy = 1; return true; }

            return false;
        }

        /// <summary>
        /// Read directional input. Returns true if a direction was pressed.
        /// Supports WASD, arrows, numpad, and vi keys (hjklyubn).
        /// Uses GetKey (held) for movement auto-repeat.
        /// </summary>
        private bool GetMoveInput(out int dx, out int dy)
        {
            dx = 0;
            dy = 0;

            // Cardinal directions
            // North (W, Up, Numpad8, vi k)
            if (InputHelper.GetKey(KeyCode.W) || InputHelper.GetKey(KeyCode.UpArrow) || InputHelper.GetKey(KeyCode.Keypad8) || InputHelper.GetKey(KeyCode.K))
            { dy = -1; return true; }

            // South (S, Down, Numpad2, vi j)
            if (InputHelper.GetKey(KeyCode.S) || InputHelper.GetKey(KeyCode.DownArrow) || InputHelper.GetKey(KeyCode.Keypad2) || InputHelper.GetKey(KeyCode.J))
            { dy = 1; return true; }

            // West (A, Left, Numpad4, vi h)
            if (InputHelper.GetKey(KeyCode.A) || InputHelper.GetKey(KeyCode.LeftArrow) || InputHelper.GetKey(KeyCode.Keypad4) || InputHelper.GetKey(KeyCode.H))
            { dx = -1; return true; }

            // East (D, Right, Numpad6, vi l)
            if (InputHelper.GetKey(KeyCode.D) || InputHelper.GetKey(KeyCode.RightArrow) || InputHelper.GetKey(KeyCode.Keypad6) || InputHelper.GetKey(KeyCode.L))
            { dx = 1; return true; }

            // Diagonals (numpad + vi keys)
            if (InputHelper.GetKey(KeyCode.Keypad7) || InputHelper.GetKey(KeyCode.Y))
            { dx = -1; dy = -1; return true; }

            if (InputHelper.GetKey(KeyCode.Keypad9) || InputHelper.GetKey(KeyCode.U))
            { dx = 1; dy = -1; return true; }

            if (InputHelper.GetKey(KeyCode.Keypad1) || InputHelper.GetKey(KeyCode.B))
            { dx = -1; dy = 1; return true; }

            if (InputHelper.GetKey(KeyCode.Keypad3) || InputHelper.GetKey(KeyCode.N))
            { dx = 1; dy = 1; return true; }

            return false;
        }

        // ===== Dialogue =====

        private void HandleAwaitingTalkDirection()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                _inputState = InputState.Normal;
                return;
            }

            int dx = 0, dy = 0;
            if (!GetDirectionKeyDown(out dx, out dy))
                return;

            _inputState = InputState.Normal;
            _lastMoveTime = Time.time;

            var playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null) return;

            int tx = playerCell.X + dx;
            int ty = playerCell.Y + dy;
            var targetCell = CurrentZone.GetCell(tx, ty);
            if (targetCell == null)
            {
                MessageLog.Add("There's nothing there to interact with.");
                return;
            }

            // Dispatch by what's in the target cell. Priority order:
            //   1. ConversationPart → talk (matches original 'c' behavior)
            //   2. ContainerPart    → open chest
            // We loop once and keep the first match of each kind; talk wins
            // if the same cell somehow has both (unlikely but well-defined).
            Entity talkTarget = null;
            Entity containerTarget = null;
            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                var obj = targetCell.Objects[i];
                if (talkTarget == null && obj.GetPart<ConversationPart>() != null)
                    talkTarget = obj;
                else if (containerTarget == null && obj.GetPart<ContainerPart>() != null)
                    containerTarget = obj;
            }

            if (talkTarget != null)
            {
                // Start conversation
                bool started = ConversationManager.StartConversation(talkTarget, PlayerEntity);
                if (!started) return;
                OpenDialogue();
                return;
            }

            if (containerTarget != null)
            {
                // Fire the InventoryAction event on the container with the OpenContainer
                // command — reuses ContainerPart.HandleEvent's existing open flow
                // (locked-check, contents message, OpenContainer event broadcast).
                // End the turn so the open action consumes a tick, matching talk.
                var openAction = GameEvent.New("InventoryAction");
                openAction.SetParameter("Command", "OpenContainer");
                openAction.SetParameter("Actor", (object)PlayerEntity);
                containerTarget.FireEventAndRelease(openAction);
                EndTurnAndProcess();
                return;
            }

            MessageLog.Add("There's nothing there to interact with.");
        }

        private void OpenDialogue()
        {
            if (DialogueUI == null) return;
            EnterCenteredPopupOverlayView();
            DialogueUI.PlayerEntity = PlayerEntity;
            DialogueUI.CurrentZone = CurrentZone;
            DialogueUI.Open();
            _inputState = InputState.DialogueOpen;
        }

        private void HandleDialogueInput()
        {
            if (DialogueUI == null || !DialogueUI.IsOpen)
            {
                CloseDialogue();
                return;
            }

            DialogueUI.HandleInput();

            if (!DialogueUI.IsOpen)
                CloseDialogue();
        }

        private void CloseDialogue()
        {
            // Check if conversation triggered an attack
            var attackTarget = ConversationManager.PendingAttackTarget;
            if (attackTarget != null)
            {
                ConversationManager.PendingAttackTarget = null;

                if (FactionManager.IsHostile(PlayerEntity, attackTarget))
                {
                    // Already hostile — attack immediately, no confirmation
                    ExecuteAttackOnNPC(attackTarget);
                    _inputState = InputState.Normal;
                    if (TryOpenAnnouncement())
                        return;

                    ExitCenteredPopupOverlayViewToGameplay();
                    return;
                }

                // Friendly NPC — show confirmation popup
                OpenAttackConfirmation(attackTarget);
                return;
            }

            // Check if conversation triggered a trade
            var tradePartner = ConversationManager.PendingTradePartner;
            if (tradePartner != null)
            {
                ConversationManager.PendingTradePartner = null;
                OpenTrade(tradePartner);
                return;
            }

            // Check for pending announcements before returning to normal
            if (TryOpenAnnouncement())
                return;

            _inputState = InputState.Normal;
            ExitCenteredPopupOverlayViewToGameplay();
        }

        // ===== Trade =====

        private void OpenTrade(Entity trader)
        {
            if (TradeUI == null) return;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            if (CameraFollow != null) CameraFollow.SetUIView(FullscreenUiGridWidth, FullscreenUiGridHeight);
            TradeUI.PlayerEntity = PlayerEntity;
            TradeUI.CurrentZone = CurrentZone;
            TradeUI.Open(trader);
            _inputState = InputState.TradeOpen;
        }

        private void HandleTradeInput()
        {
            if (TradeUI == null || !TradeUI.IsOpen)
            {
                CloseTrade();
                return;
            }

            TradeUI.HandleInput();

            if (!TradeUI.IsOpen)
                CloseTrade();
        }

        private void CloseTrade()
        {
            _inputState = InputState.Normal;
            if (CameraFollow != null) CameraFollow.RestoreGameView();
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty("UI.Trade.Close");
            }
        }

        private void RequestZoneRedraw(string source)
        {
            if (ZoneRenderer != null)
                ZoneRenderer.MarkDirty(source);
        }

        // ===== Attack Confirmation =====

        private void HandleAttackConfirmInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Y))
            {
                ResolveAttackConfirmation(true);
            }
            else if (InputHelper.GetKeyDown(KeyCode.N) || InputHelper.GetKeyDown(KeyCode.Escape))
            {
                ResolveAttackConfirmation(false);
            }
        }

        private void ExecuteAttackOnNPC(Entity target)
        {
            if (target == null) return;

            var brain = target.GetPart<BrainPart>();
            if (brain != null)
                brain.SetPersonallyHostile(PlayerEntity);

            CombatSystem.PerformMeleeAttack(PlayerEntity, target, CurrentZone, _combatRng);
            EndTurnAndProcess();
        }

        private int _confirmOriginX, _confirmTopY, _confirmW, _confirmH;
        private static readonly Color ConfirmPopupBgColor = new Color(0f, 0f, 0f, 1f);

        private void OpenAttackConfirmation(Entity target)
        {
            _pendingAttackTarget = target;
            _inputState = InputState.AwaitingAttackConfirm;
            EnterCenteredPopupOverlayView();
            RenderAttackConfirmation(target);
        }

        private void ResolveAttackConfirmation(bool confirmed)
        {
            ClearAttackConfirmation();
            var target = _pendingAttackTarget;
            _pendingAttackTarget = null;

            if (confirmed)
                ExecuteAttackOnNPC(target);
            else
                MessageLog.Add("You decide against it.");

            _inputState = InputState.Normal;
            if (TryOpenAnnouncement())
                return;

            ExitCenteredPopupOverlayViewToGameplay();
        }

        private void RenderAttackConfirmation(Entity target)
        {
            if (DialogueUI == null || DialogueUI.Tilemap == null) return;
            var tilemap = DialogueUI.Tilemap;
            var bgTilemap = DialogueUI.BgTilemap;

            string name = target != null ? target.GetDisplayName() : "this creature";
            string prompt = "Really attack " + name + "? (y/n)";

            _confirmW = prompt.Length + 4;
            _confirmH = 3;
            _confirmOriginX = CenteredPopupLayout.GetCenteredOriginX(_confirmW);
            _confirmTopY = CenteredPopupLayout.GetCenteredTopY(_confirmH);

            // Clear only the popup region
            for (int dy = 0; dy < _confirmH; dy++)
                for (int dx = 0; dx < _confirmW; dx++)
                {
                    tilemap.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);
                    bgTilemap?.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);
                }

            DrawConfirmBackground(bgTilemap);

            // Border
            DrawConfirmChar(0, 0, '+', QudColorParser.Gray);
            DrawConfirmChar(_confirmW - 1, 0, '+', QudColorParser.Gray);
            DrawConfirmChar(0, 2, '+', QudColorParser.Gray);
            DrawConfirmChar(_confirmW - 1, 2, '+', QudColorParser.Gray);
            for (int i = 1; i < _confirmW - 1; i++)
            {
                DrawConfirmChar(i, 0, '-', QudColorParser.Gray);
                DrawConfirmChar(i, 2, '-', QudColorParser.Gray);
            }
            DrawConfirmChar(0, 1, '|', QudColorParser.Gray);
            DrawConfirmChar(_confirmW - 1, 1, '|', QudColorParser.Gray);

            // Text
            for (int i = 0; i < prompt.Length; i++)
            {
                if (prompt[i] == ' ') continue;
                DrawConfirmChar(2 + i, 1, prompt[i], QudColorParser.BrightYellow);
            }
        }

        private void ClearAttackConfirmation()
        {
            if (DialogueUI == null || DialogueUI.Tilemap == null) return;
            var tilemap = DialogueUI.Tilemap;
            var bgTilemap = DialogueUI.BgTilemap;
            for (int dy = 0; dy < _confirmH; dy++)
                for (int dx = 0; dx < _confirmW; dx++)
                {
                    tilemap.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);
                    bgTilemap?.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);
                }
        }

        private void DrawConfirmBackground(UnityEngine.Tilemaps.Tilemap bgTilemap)
        {
            if (bgTilemap == null) return;

            var blockTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null) return;

            for (int dy = 0; dy < _confirmH - 1; dy++)
            {
                for (int dx = 0; dx < _confirmW; dx++)
                {
                    var pos = new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0);
                    bgTilemap.SetTile(pos, blockTile);
                    bgTilemap.SetTileFlags(pos, UnityEngine.Tilemaps.TileFlags.None);
                    bgTilemap.SetColor(pos, ConfirmPopupBgColor);
                }
            }
        }

        private void DrawConfirmChar(int gx, int gy, char c, Color color)
        {
            if (DialogueUI == null || DialogueUI.Tilemap == null) return;
            var tilemap = DialogueUI.Tilemap;
            int wx = _confirmOriginX + gx;
            int wy = _confirmTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            tilemap.SetTile(tilePos, tile);
            tilemap.SetTileFlags(tilePos, UnityEngine.Tilemaps.TileFlags.None);
            tilemap.SetColor(tilePos, color);
        }

        // ===== Announcement Modal =====

        private bool TryOpenAnnouncement()
        {
            if (AnnouncementUI == null || !MessageLog.HasPendingAnnouncement)
                return false;

            string msg = MessageLog.ConsumeAnnouncement();
            if (msg == null)
                return false;

            EnterCenteredPopupOverlayView();
            AnnouncementUI.Open(msg);
            _inputState = InputState.AnnouncementOpen;
            return true;
        }

        private void HandleAnnouncementInput()
        {
            if (AnnouncementUI == null || !AnnouncementUI.IsOpen)
            {
                CloseAnnouncement();
                return;
            }

            AnnouncementUI.HandleInput();

            if (!AnnouncementUI.IsOpen)
                CloseAnnouncement();
        }

        private void CloseAnnouncement()
        {
            // Check for more pending announcements
            if (TryOpenAnnouncement())
                return;

            _inputState = InputState.Normal;
            ExitCenteredPopupOverlayViewToGameplay();
        }

        private void EnterCenteredPopupOverlayView()
        {
            if (CameraFollow != null)
                CameraFollow.SetCenteredPopupOverlayView();
        }

        private void ExitCenteredPopupOverlayViewToGameplay()
        {
            if (CameraFollow != null)
                CameraFollow.RestoreGameView();
        }

        private void ActivateSelectedHotbarSlot()
        {
            EnsureHotbarSelectionValid();
            if (_selectedHotbarSlot < 0)
            {
                MessageLog.Add("No rite bound.");
                return;
            }

            TryActivateAbility(_selectedHotbarSlot);
        }

        private void CycleHotbarSelection(int direction)
        {
            var abilities = PlayerEntity?.GetPart<ActivatedAbilitiesPart>();
            int nextSlot = FindNextOccupiedHotbarSlot(abilities, _selectedHotbarSlot, direction);
            if (nextSlot < 0)
            {
                MessageLog.Add("No rite bound.");
                return;
            }

            _selectedHotbarSlot = nextSlot;
            SyncHotbarState();
        }

        private void EnsureHotbarSelectionValid()
        {
            var abilities = PlayerEntity?.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null)
            {
                _selectedHotbarSlot = -1;
                return;
            }

            if (IsOccupiedHotbarSlot(abilities, _selectedHotbarSlot))
                return;

            _selectedHotbarSlot = FindNextOccupiedHotbarSlot(abilities, -1, 1);
        }

        private void SyncHotbarState()
        {
            ZoneRenderer?.SetHotbarState(_selectedHotbarSlot, _pendingAbility);
        }

        private static bool IsOccupiedHotbarSlot(ActivatedAbilitiesPart abilities, int slot)
        {
            return abilities != null &&
                   slot >= 0 &&
                   slot < ActivatedAbilitiesPart.SlotCount &&
                   abilities.GetAbilityBySlot(slot) != null;
        }

        private static int FindNextOccupiedHotbarSlot(ActivatedAbilitiesPart abilities, int startSlot, int direction)
        {
            if (abilities == null)
                return -1;

            int delta = direction >= 0 ? 1 : -1;
            for (int step = 1; step <= ActivatedAbilitiesPart.SlotCount; step++)
            {
                int slot = Mathf.FloorToInt(Mathf.Repeat(startSlot + step * delta, ActivatedAbilitiesPart.SlotCount));
                if (abilities.GetAbilityBySlot(slot) != null)
                    return slot;
            }

            return -1;
        }

        private static string GetAbilityDisplayName(ActivatedAbility ability)
        {
            if (ability == null)
                return "That rite";

            GrimoireTooltip tooltip = GrimoireTooltipData.GetOrDefault(ability.SourceMutationClass);
            return !string.IsNullOrEmpty(tooltip.DisplayName)
                ? tooltip.DisplayName
                : ability.DisplayName;
        }
    }
}
