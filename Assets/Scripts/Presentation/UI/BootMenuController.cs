using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pure dispatch logic for the boot-menu modal — shown at game
    /// startup IF a save exists, offering the player Continue (load
    /// save) or New Game (dismiss menu, keep current bootstrap state).
    /// Per <c>Docs/QUD-PARITY.md §2.1</c> and <c>Docs/roadmap.md</c>
    /// Tier-1 #2.
    ///
    /// <para><b>Lifecycle.</b> Inactive on construct. GameBootstrap
    /// calls <see cref="TryActivate"/> at the end of init; only
    /// activates if a save exists. From there <see cref="Tick"/>
    /// polls each frame; on choice, deactivates.</para>
    ///
    /// <para><b>Default keys.</b> <c>C</c> = continue, <c>N</c> = new
    /// game. Public so a future settings UI can rebind.</para>
    ///
    /// <para><b>Conflict policy.</b> Continue wins same-frame
    /// collisions — lets the player recover into their save rather
    /// than discarding it via New Game.</para>
    /// </summary>
    public sealed class BootMenuController
    {
        public KeyCode ContinueKey = KeyCode.C;
        public KeyCode NewGameKey = KeyCode.N;

        public bool IsActive { get; private set; }

        /// <summary>
        /// Try to show the boot menu. Returns true if activated, false
        /// otherwise. No-op when no save exists (skip menu, go to play).
        /// Idempotent: calling on an already-active controller does
        /// not re-prompt.
        /// </summary>
        public bool TryActivate(bool hasSave, Action<string> log)
        {
            if (IsActive) return true;
            if (!hasSave) return false;

            IsActive = true;
            log?.Invoke("Save detected. Press [C] to continue, [N] for new game.");
            return true;
        }

        /// <summary>
        /// Run one polling tick while the modal is active. Safe to
        /// call every frame; no-op when inactive.
        /// </summary>
        public void Tick(IInputProbe input, ISaveLoadService service, Action<string> log)
        {
            if (!IsActive) return;

            bool continuePressed = input.GetKeyDown(ContinueKey);
            bool newGamePressed = input.GetKeyDown(NewGameKey);

            // Continue wins same-frame collisions — non-destructive op preferred.
            if (continuePressed)
            {
                if (!service.HasQuickSave())
                {
                    // Save vanished between TryActivate and Tick — defensive.
                    log?.Invoke("Save no longer available. Press [N] for new game.");
                    return;  // stay active so the user can pick N
                }

                service.QuickLoad();
                IsActive = false;
                return;
            }

            if (newGamePressed)
            {
                IsActive = false;
                log?.Invoke("Starting a new game.");
            }
        }
    }
}
