using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Owner-bound display shortcuts. Bootstrap owns initial preference
    /// loading; neither shortcut executes a game command or consumes a turn.</summary>
    [DisallowMultipleComponent]
    public sealed class Village3DControls : MonoBehaviour
    {
        private ZoneRenderer _renderer;
        private void Awake() => _renderer = GetComponent<ZoneRenderer>();

        private void Update()
        {
            if (!InputHelper.GetKeyDown(KeyCode.F11)) return;
            bool detail = InputHelper.GetKey(KeyCode.LeftShift) || InputHelper.GetKey(KeyCode.RightShift);
            if (detail)
            {
                Village3DSettings.LowDetail = !Village3DSettings.LowDetail;
                string scope = WorldPresentationSettings.Mode == WorldPresentationMode.Flat16x24 ? "Previous 3D detail: " : "World detail: ";
                MessageLog.Add(scope + (Village3DSettings.LowDetail ? "low." : "full."));
                Village3DSettings.Save();
                return;
            }
            if (_renderer == null) return;
            if (WorldPresentationSettings.Toggle(ApplyPresentation))
                MessageLog.Add(WorldPresentationSettings.Mode == WorldPresentationMode.Flat16x24
                    ? "World view: 16x24." : "World view: previous graphics.");
        }

        private void ApplyPresentation() => _renderer.ApplyWorldPresentation(
            WorldPresentationSettings.Mode, WorldPresentationSettings.LegacySpritesEnabled);
    }
}
