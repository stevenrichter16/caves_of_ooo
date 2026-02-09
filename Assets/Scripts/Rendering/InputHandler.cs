using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that converts player key presses into movement commands.
    /// Supports WASD, arrow keys, numpad (8-directional), and vi keys.
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
        /// Minimum time between moves (seconds) to prevent too-fast input.
        /// </summary>
        public float MoveRepeatDelay = 0.12f;

        private float _lastMoveTime;

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

            int dx = 0, dy = 0;

            // Check movement keys
            if (GetMoveInput(out dx, out dy))
            {
                var oldCell = CurrentZone.GetEntityCell(PlayerEntity);
                int oldX = oldCell?.X ?? -1;
                int oldY = oldCell?.Y ?? -1;

                bool moved = MovementSystem.TryMove(PlayerEntity, CurrentZone, dx, dy);

                if (moved)
                {
                    var newCell = CurrentZone.GetEntityCell(PlayerEntity);

                    // Refresh only the affected cells for efficiency
                    if (ZoneRenderer != null && oldCell != null && newCell != null)
                        ZoneRenderer.RefreshMovement(oldX, oldY, newCell.X, newCell.Y);

                    // End the player's turn and process NPC turns
                    TurnManager.EndTurn(PlayerEntity);
                    TurnManager.ProcessUntilPlayerTurn();

                    // Full refresh after NPCs move
                    if (ZoneRenderer != null)
                        ZoneRenderer.MarkDirty();
                }

                _lastMoveTime = Time.time;
            }

            // Wait/skip turn
            if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                TurnManager.EndTurn(PlayerEntity);
                TurnManager.ProcessUntilPlayerTurn();
                if (ZoneRenderer != null)
                    ZoneRenderer.MarkDirty();
                _lastMoveTime = Time.time;
            }
        }

        /// <summary>
        /// Read directional input. Returns true if a direction was pressed.
        /// Supports WASD, arrows, numpad, and vi keys (hjklyubn).
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
