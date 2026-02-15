using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that converts player key presses into game commands.
    /// Supports WASD, arrow keys, numpad (8-directional), vi keys,
    /// item pickup (G/comma), ability activation (1-5 + direction),
    /// and debug keys: F6 (grant mutation), F7 (dump body parts), F8 (dismember limb).
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

        /// <summary>
        /// Input state machine for ability targeting.
        /// Normal: standard movement/action input.
        /// AwaitingDirection: waiting for a directional key to target an ability.
        /// </summary>
        private enum InputState { Normal, AwaitingDirection, InventoryOpen, PickupOpen, ContainerPickerOpen, AwaitingTalkDirection, DialogueOpen, TradeOpen, AwaitingAttackConfirm }
        private InputState _inputState = InputState.Normal;
        private ActivatedAbility _pendingAbility;
        private Entity _pendingAttackTarget;

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

        private void Update()
        {
            if (PlayerEntity == null || CurrentZone == null || TurnManager == null)
                return;

            // Only accept input when it's the player's turn
            if (!TurnManager.WaitingForInput)
                return;

            if (TurnManager.CurrentActor != PlayerEntity)
                return;

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

            // Open inventory (I key)
            if (Input.GetKeyDown(KeyCode.I))
            {
                OpenInventory();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: grant a random mutation from the current mutate pool.
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TryDebugGrantRandomMutation();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: dump body part tree to console.
            if (Input.GetKeyDown(KeyCode.F7))
            {
                TryDebugDumpBodyParts();
                _lastMoveTime = Time.time;
                return;
            }

            // Debug: dismember a random non-mortal appendage.
            if (Input.GetKeyDown(KeyCode.F8))
            {
                TryDebugDismember();
                _lastMoveTime = Time.time;
                return;
            }

            // Talk to NPC (C key + direction)
            if (Input.GetKeyDown(KeyCode.C))
            {
                _inputState = InputState.AwaitingTalkDirection;
                MessageLog.Add("Talk — choose a direction.");
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

            // Ability activation (keys 1-5)
            int abilitySlot = GetAbilitySlotInput();
            if (abilitySlot >= 0)
            {
                TryActivateAbility(abilitySlot);
                _lastMoveTime = Time.time;
            }

            // Pickup item (G or comma)
            if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Comma))
            {
                TryPickupItem();
                _lastMoveTime = Time.time;
            }

            // Wait/skip turn
            if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                EndTurnAndProcess();
                _lastMoveTime = Time.time;
            }
        }

        /// <summary>
        /// Handle all side effects of a zone transition:
        /// rewire TurnManager, BrainParts, ZoneRenderer, and CurrentZone.
        /// </summary>
        private void HandleZoneTransition(ZoneTransitionResult result)
        {
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

            // Update camera to follow player in new zone
            if (CameraFollow != null)
            {
                CameraFollow.CurrentZone = result.NewZone;
                CameraFollow.SnapToPlayer();
            }

            Debug.Log($"[Zone] Transitioned to {result.NewZone.ZoneID}");
        }

        private void EndTurnAndProcess()
        {
            TurnManager.EndTurn(PlayerEntity);
            TurnManager.ProcessUntilPlayerTurn();
            if (ZoneRenderer != null)
                ZoneRenderer.MarkDirty();
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

            if (ZoneRenderer != null)
                ZoneRenderer.MarkDirty();
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

            if (ZoneRenderer != null)
                ZoneRenderer.MarkDirty();
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
                            if (ZoneRenderer != null)
                                ZoneRenderer.MarkDirty();
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
                    if (ZoneRenderer != null)
                        ZoneRenderer.MarkDirty();
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

            if (ZoneRenderer != null)
                ZoneRenderer.Paused = true;

            ContainerPickerUI.Open(containers);
            _inputState = InputState.ContainerPickerOpen;
        }

        private void OpenPickup(List<Entity> items)
        {
            if (PickupUI == null) return;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
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
            _inputState = InputState.Normal;
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty();
            }

            // Process a turn if items were picked up
            if (pickedUpAny)
                EndTurnAndProcess();
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
            _inputState = InputState.Normal;
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty();
            }

            if (tookAny)
                EndTurnAndProcess();
        }

        /// <summary>
        /// Check if a number key 1-5 was pressed. Returns 0-4 slot index, or -1 if none.
        /// </summary>
        private int GetAbilitySlotInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) return 4;
            return -1;
        }

        /// <summary>
        /// Try to activate an ability by slot. If the ability is usable,
        /// enter AwaitingDirection state for directional targeting.
        /// </summary>
        private void TryActivateAbility(int slot)
        {
            var abilities = PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null)
            {
                Debug.Log("[Abilities] No activated abilities.");
                return;
            }

            var ability = abilities.GetAbilityBySlot(slot);
            if (ability == null)
            {
                Debug.Log($"[Abilities] No ability in slot {slot + 1}.");
                return;
            }

            if (!ability.IsUsable)
            {
                Debug.Log($"[Abilities] {ability.DisplayName} is on cooldown ({ability.CooldownRemaining} turns remaining).");
                return;
            }

            // Enter targeting mode
            _pendingAbility = ability;
            _inputState = InputState.AwaitingDirection;
            Debug.Log($"[Abilities] {ability.DisplayName} — choose a direction.");
        }

        /// <summary>
        /// Handle input while awaiting a direction for ability targeting.
        /// Escape cancels, directional input fires the ability command.
        /// </summary>
        private void HandleAwaitingDirection()
        {
            // Cancel on Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[Abilities] Cancelled.");
                _inputState = InputState.Normal;
                _pendingAbility = null;
                return;
            }

            int dx = 0, dy = 0;
            if (!GetDirectionKeyDown(out dx, out dy))
                return;

            // Compute target cell
            var playerCell = CurrentZone.GetEntityCell(PlayerEntity);
            if (playerCell == null)
            {
                _inputState = InputState.Normal;
                _pendingAbility = null;
                return;
            }

            int targetX = playerCell.X + dx;
            int targetY = playerCell.Y + dy;
            var targetCell = CurrentZone.GetCell(targetX, targetY);

            if (targetCell == null)
            {
                Debug.Log("[Abilities] Invalid target.");
                _inputState = InputState.Normal;
                _pendingAbility = null;
                return;
            }

            // Fire the ability's command event
            var cmd = GameEvent.New(_pendingAbility.Command);
            cmd.SetParameter("TargetCell", (object)targetCell);
            cmd.SetParameter("Zone", (object)CurrentZone);
            cmd.SetParameter("RNG", (object)_combatRng);
            PlayerEntity.FireEvent(cmd);

            // Reset state
            _inputState = InputState.Normal;
            _pendingAbility = null;

            EndTurnAndProcess();
            _lastMoveTime = Time.time;
        }

        private void OpenInventory()
        {
            if (InventoryUI == null) return;
            InventoryUI.PlayerEntity = PlayerEntity;
            InventoryUI.CurrentZone = CurrentZone;
            InventoryUI.Open();
            _inputState = InputState.InventoryOpen;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            if (CameraFollow != null) CameraFollow.SetUIView(80, 45);
        }

        private void HandleInventoryInput()
        {
            if (InventoryUI == null || !InventoryUI.IsOpen)
            {
                CloseInventory();
                return;
            }

            InventoryUI.HandleInput();

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
                ZoneRenderer.MarkDirty();
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
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.K))
            { dy = -1; return true; }

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.J))
            { dy = 1; return true; }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.H))
            { dx = -1; return true; }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.L))
            { dx = 1; return true; }

            // Diagonals
            if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Y))
            { dx = -1; dy = -1; return true; }

            if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.U))
            { dx = 1; dy = -1; return true; }

            if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.B))
            { dx = -1; dy = 1; return true; }

            if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.N))
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
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Keypad8) || Input.GetKey(KeyCode.K))
            { dy = -1; return true; }

            // South (S, Down, Numpad2, vi j)
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Keypad2) || Input.GetKey(KeyCode.J))
            { dy = 1; return true; }

            // West (A, Left, Numpad4, vi h)
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Keypad4) || Input.GetKey(KeyCode.H))
            { dx = -1; return true; }

            // East (D, Right, Numpad6, vi l)
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Keypad6) || Input.GetKey(KeyCode.L))
            { dx = 1; return true; }

            // Diagonals (numpad + vi keys)
            if (Input.GetKey(KeyCode.Keypad7) || Input.GetKey(KeyCode.Y))
            { dx = -1; dy = -1; return true; }

            if (Input.GetKey(KeyCode.Keypad9) || Input.GetKey(KeyCode.U))
            { dx = 1; dy = -1; return true; }

            if (Input.GetKey(KeyCode.Keypad1) || Input.GetKey(KeyCode.B))
            { dx = -1; dy = 1; return true; }

            if (Input.GetKey(KeyCode.Keypad3) || Input.GetKey(KeyCode.N))
            { dx = 1; dy = 1; return true; }

            return false;
        }

        // ===== Dialogue =====

        private void HandleAwaitingTalkDirection()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
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
                MessageLog.Add("There's nothing there to talk to.");
                return;
            }

            // Find entity with ConversationPart in the target cell
            Entity talkTarget = null;
            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                if (targetCell.Objects[i].GetPart<ConversationPart>() != null)
                {
                    talkTarget = targetCell.Objects[i];
                    break;
                }
            }

            if (talkTarget == null)
            {
                MessageLog.Add("There's nothing there to talk to.");
                return;
            }

            // Try starting conversation
            bool started = ConversationManager.StartConversation(talkTarget, PlayerEntity);
            if (!started) return;

            OpenDialogue();
        }

        private void OpenDialogue()
        {
            if (DialogueUI == null) return;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
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
                    if (ZoneRenderer != null) { ZoneRenderer.Paused = false; ZoneRenderer.MarkDirty(); }
                    return;
                }

                // Friendly NPC — show confirmation popup
                _pendingAttackTarget = attackTarget;
                _inputState = InputState.AwaitingAttackConfirm;
                RenderAttackConfirmation(attackTarget);
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

            _inputState = InputState.Normal;
            if (ZoneRenderer != null)
            {
                ZoneRenderer.Paused = false;
                ZoneRenderer.MarkDirty();
            }
        }

        // ===== Trade =====

        private void OpenTrade(Entity trader)
        {
            if (TradeUI == null) return;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            if (CameraFollow != null) CameraFollow.SetUIView(80, 45);
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
                ZoneRenderer.MarkDirty();
            }
        }

        // ===== Attack Confirmation =====

        private void HandleAttackConfirmInput()
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                ClearAttackConfirmation();
                var target = _pendingAttackTarget;
                _pendingAttackTarget = null;
                _inputState = InputState.Normal;
                if (ZoneRenderer != null) { ZoneRenderer.Paused = false; ZoneRenderer.MarkDirty(); }
                ExecuteAttackOnNPC(target);
            }
            else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
            {
                ClearAttackConfirmation();
                _pendingAttackTarget = null;
                _inputState = InputState.Normal;
                if (ZoneRenderer != null) { ZoneRenderer.Paused = false; ZoneRenderer.MarkDirty(); }
                MessageLog.Add("You decide against it.");
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

        private void RenderAttackConfirmation(Entity target)
        {
            if (DialogueUI == null || DialogueUI.Tilemap == null) return;
            var tilemap = DialogueUI.Tilemap;
            var cam = Camera.main;
            if (cam == null) return;

            string name = target != null ? target.GetDisplayName() : "this creature";
            string prompt = "Really attack " + name + "? (y/n)";

            _confirmW = prompt.Length + 4;
            _confirmH = 3;
            _confirmOriginX = Mathf.RoundToInt(cam.transform.position.x) - _confirmW / 2;
            _confirmTopY = Mathf.RoundToInt(cam.transform.position.y) + _confirmH / 2;

            // Clear only the popup region
            for (int dy = 0; dy < _confirmH; dy++)
                for (int dx = 0; dx < _confirmW; dx++)
                    tilemap.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);

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
            for (int dy = 0; dy < _confirmH; dy++)
                for (int dx = 0; dx < _confirmW; dx++)
                    tilemap.SetTile(new Vector3Int(_confirmOriginX + dx, _confirmTopY - dy, 0), null);
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
    }
}
