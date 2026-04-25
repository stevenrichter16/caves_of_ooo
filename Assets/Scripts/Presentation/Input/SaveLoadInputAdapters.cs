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

    /// <summary>
    /// Production <see cref="ISaveLoadService"/> wrapping
    /// <see cref="SaveGameService"/>'s static QuickSave / QuickLoad /
    /// HasQuickSave methods. Stateless; safe as a static singleton.
    /// </summary>
    internal sealed class SaveGameServiceAdapter : ISaveLoadService
    {
        public bool QuickSave() => SaveGameService.QuickSave();
        public bool QuickLoad() => SaveGameService.QuickLoad();
        public bool HasQuickSave() => SaveGameService.HasQuickSave();
    }
}
