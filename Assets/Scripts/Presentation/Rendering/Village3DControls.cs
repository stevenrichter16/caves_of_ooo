using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Player display preferences. Added by the ordinary game renderer;
    /// neither shortcut executes a game command or consumes a turn.</summary>
    [DisallowMultipleComponent]
    public sealed class Village3DControls : MonoBehaviour
    {
        private void Awake() => Village3DSettings.Load();

        private void Update()
        {
            if (!InputHelper.GetKeyDown(KeyCode.F11)) return;
            bool detail = InputHelper.GetKey(KeyCode.LeftShift) || InputHelper.GetKey(KeyCode.RightShift);
            if (detail)
            {
                Village3DSettings.LowDetail = !Village3DSettings.LowDetail;
                MessageLog.Add(Village3DSettings.LowDetail ? "World detail: low." : "World detail: full.");
            }
            else
            {
                Village3DSettings.Enabled = !Village3DSettings.Enabled;
                MessageLog.Add(Village3DSettings.Enabled ? "World view: 3D." : "World view: original.");
            }
            Village3DSettings.Save();
        }
    }
}
