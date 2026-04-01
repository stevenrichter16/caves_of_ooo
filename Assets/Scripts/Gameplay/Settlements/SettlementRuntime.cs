using System;

namespace CavesOfOoo.Core
{
    public static class SettlementRuntime
    {
        public static Zone ActiveZone { get; set; }
        public static Action ZoneDirtyCallback { get; set; }

        public static void MarkZoneDirty()
        {
            ZoneDirtyCallback?.Invoke();
        }

        public static void Reset()
        {
            ActiveZone = null;
            ZoneDirtyCallback = null;
        }
    }
}
