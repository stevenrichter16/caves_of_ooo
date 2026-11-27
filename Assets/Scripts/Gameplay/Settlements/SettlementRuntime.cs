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

        internal static void ReconcileActiveStructurePatches(Zone zone, TurnManager clock)
        {
            if (zone == null || clock == null || clock != TurnManager.Active) return;
            var manager = WorldLocationContext.For(zone);
            if (manager?.ActiveZone == zone && manager.SettlementManager.AdvanceActiveStructurePatches(zone.ZoneID, clock.TickCount))
                MarkZoneDirty();
        }

        public static void Reset()
        {
            ActiveZone = null;
            ZoneDirtyCallback = null;
        }
    }
}
