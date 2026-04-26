using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Abstraction for "restart the game from scratch" — exists so
    /// the controller is unit-testable without actually reloading
    /// the Unity scene. The production adapter wraps
    /// <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene"/>.
    /// </summary>
    public interface ISceneRestarter
    {
        void Restart();
    }

    /// <summary>
    /// Pure dispatch logic for the death-screen modal. Per
    /// <c>Docs/QUD-PARITY.md §2.1</c> (TDD) and <c>Docs/roadmap.md</c>
    /// Tier-1 #2 (death-screen continue from autosave).
    ///
    /// <para><b>Lifecycle.</b> Starts inactive. The player-Died
    /// listener calls <see cref="Activate"/>; from there
    /// <see cref="Tick"/> polls each frame until the player picks
    /// Load or Restart. Dispatch deactivates the modal — Tick is a
    /// no-op until reactivated.</para>
    ///
    /// <para><b>Default keys.</b> <c>L</c> = load (if save exists),
    /// <c>R</c> = restart (always available). Keys are public so a
    /// future settings UI can rebind.</para>
    ///
    /// <para><b>Conflict policy.</b> If both keys arrive same-frame,
    /// LOAD wins — Load is recoverable (player can press R later if
    /// they regret loading), Restart is not (it wipes the save-load
    /// opportunity by reloading the scene).</para>
    /// </summary>
    public sealed class DeathScreenController
    {
        public KeyCode LoadKey = KeyCode.L;
        public KeyCode RestartKey = KeyCode.R;

        public bool IsActive { get; private set; }

        /// <summary>
        /// Show the death-screen modal. No-op if already active so
        /// re-firing the player-Died event (defensive against a
        /// double-fire bug elsewhere) doesn't double-log the prompt.
        /// </summary>
        public void Activate(Action<string> log)
        {
            if (IsActive) return;
            IsActive = true;
            log?.Invoke("You are dead. Press [L] to load last save, [R] to restart.");
        }

        /// <summary>
        /// Run one polling tick while the modal is active. Returns
        /// silently if inactive — safe to call every frame
        /// unconditionally.
        /// </summary>
        public void Tick(IInputProbe input, ISaveLoadService service, ISceneRestarter restarter, Action<string> log)
        {
            if (!IsActive) return;

            bool loadPressed = input.GetKeyDown(LoadKey);
            bool restartPressed = input.GetKeyDown(RestartKey);

            // Load wins same-frame collisions — non-destructive op preferred.
            if (loadPressed)
            {
                if (!service.HasQuickSave())
                {
                    log?.Invoke("No save to load. Press [R] to restart.");
                    return;  // stay active so the player can pick Restart
                }

                service.QuickLoad();
                IsActive = false;
                return;
            }

            if (restartPressed)
            {
                restarter.Restart();
                IsActive = false;
            }
        }
    }
}
