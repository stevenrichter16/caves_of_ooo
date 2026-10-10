using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Production <see cref="IInputProbe"/> wrapping
    /// <see cref="InputHelper.GetKeyDown"/>. Glue between
    /// <see cref="SaveLoadInputController"/> and the real keyboard.
    /// Stateless; safe as a static singleton.
    /// </summary>
    internal sealed class UnityInputProbeAdapter : IInputProbe
    {
        public bool GetKeyDown(KeyCode k) => InputHelper.GetKeyDown(k);
    }

    /// <summary>Maps A and X only inside a saved-game choice prompt. Keyboard
    /// bindings and modifier-latched controller actions retain their meanings.</summary>
    internal sealed class GamepadChoiceInputProbe : IInputProbe
    {
        readonly IInputProbe fallback;
        readonly KeyCode resume, restart;
        public GamepadChoiceInputProbe(IInputProbe fallback, KeyCode resume, KeyCode restart)
        { this.fallback = fallback; this.resume = resume; this.restart = restart; }
        public bool GetKeyDown(KeyCode key) => fallback.GetKeyDown(key)
            || (key == resume && NativeGamepadInput.GetKeyDown(KeyCode.Return))
            || (key == restart && NativeGamepadInput.GetKeyDown(KeyCode.I));
    }

    /// <summary>
    /// Production <see cref="ISaveLoadService"/> wrapping
    /// <see cref="SaveGameService"/>'s static BeginNewGame / QuickSave / QuickLoad /
    /// HasQuickSave methods. Stateless; safe as a static singleton.
    /// </summary>
    internal sealed class SaveGameServiceAdapter : ISaveLoadService
    {
        public bool BeginNewGame() => SaveGameService.BeginNewGame();
        public bool QuickSave() => SaveGameService.QuickSave();
        public bool QuickLoad() => SaveGameService.QuickLoad();
        public bool HasQuickSave() => SaveGameService.HasQuickSave();
    }
}
