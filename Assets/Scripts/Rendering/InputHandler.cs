using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that converts player key presses into game commands.
    /// Supports WASD, arrow keys, numpad (8-directional), vi keys,
    /// item pickup (G/comma), and ability activation (1-5 + direction).
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
        private enum InputState { Normal, AwaitingDirection }
        private InputState _inputState = InputState.Normal;
        private ActivatedAbility _pendingAbility;

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
                else if (blockedBy != null && blockedBy.HasTag("Creature"))
                {
                    // Bump-to-attack: blocked by a creature, perform melee attack
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
        /// Pick up the first takeable item at the player's feet.
        /// Auto-equips if the item has an EquippablePart and the slot is empty.
        /// </summary>
        private void TryPickupItem()
        {
            var items = InventorySystem.GetTakeableItemsAtFeet(PlayerEntity, CurrentZone);
            if (items.Count == 0)
            {
                Debug.Log("[Inventory] Nothing to pick up here.");
                return;
            }

            var item = items[0];
            if (InventorySystem.Pickup(PlayerEntity, item, CurrentZone))
            {
                // Auto-equip if the item has an EquippablePart and the slot is empty
                var equippable = item.GetPart<EquippablePart>();
                if (equippable != null)
                {
                    var inv = PlayerEntity.GetPart<InventoryPart>();
                    if (inv != null && inv.GetEquipped(equippable.Slot) == null)
                    {
                        InventorySystem.Equip(PlayerEntity, item);
                    }
                }

                EndTurnAndProcess();
                if (ZoneRenderer != null)
                    ZoneRenderer.MarkDirty();
            }
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
    }
}
