using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Abstracts polling a single key — exists so the controller is
    /// unit-testable without a real keyboard. The MonoBehaviour
    /// adapter wraps <see cref="InputHelper.GetKeyDown"/>.
    /// </summary>
    public interface IInputProbe
    {
        bool GetKeyDown(KeyCode k);
    }

    /// <summary>
    /// Abstracts the save/load file I/O — exists so the controller
    /// is unit-testable without touching <c>persistentDataPath</c>.
    /// The runtime adapter wraps <c>SaveGameService</c>.
    /// </summary>
    public interface ISaveLoadService
    {
        bool QuickSave();
        bool QuickLoad();
        bool HasQuickSave();
    }

    /// <summary>
    /// Pure dispatch logic for the save/load hotkeys. Per
    /// <c>Docs/QUD-PARITY.md §2.1</c> (TDD) and <c>Docs/roadmap.md</c>
    /// Tier-1 #2 (save/load UI on top of the audited SaveSystem).
    ///
    /// <para><b>Default key bindings.</b> F5 = quick save, F6 =
    /// quick load. F9 is already bound to debug-craft and was avoided
    /// to keep this commit additive. Bindings are public-mutable so
    /// integration code (or future settings UI) can rebind them.</para>
    ///
    /// <para><b>Conflict policy.</b> If both keys arrive in the same
    /// frame (rare; only via remap collision or programmatic input),
    /// SAVE wins — never destroy the player's current state by
    /// loading the previous save in a same-frame collision.</para>
    /// </summary>
    public sealed class SaveLoadInputController
    {
        public KeyCode SaveKey = KeyCode.F5;
        public KeyCode LoadKey = KeyCode.F6;

        /// <summary>
        /// Run one polling tick. Call from the host's Update().
        /// </summary>
        public void Tick(IInputProbe input, ISaveLoadService service, Action<string> log)
        {
            bool savePressed = input.GetKeyDown(SaveKey);
            bool loadPressed = input.GetKeyDown(LoadKey);

            // Save wins same-frame collisions — non-destructive op preferred.
            if (savePressed)
            {
                bool ok = service.QuickSave();
                log?.Invoke(ok ? "Game saved." : "Save failed — see console.");
                return;
            }

            if (loadPressed)
            {
                if (!service.HasQuickSave())
                {
                    log?.Invoke("No save to load.");
                    return;
                }

                bool ok = service.QuickLoad();
                log?.Invoke(ok ? "Game loaded." : "Load failed — save may be corrupted.");
            }
        }
    }
}
