using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A deliberate workshop operation retains one physical station
    /// through callbacks. It cannot switch to another nearby forge at commit.</summary>
    internal static class ForgeWorkReceipt
    {
        // One station contract serves every kind (Docs/MACHINE-STATIONS.md, spec C5). These forwarders keep the forge's exact legacy semantics:
        // strict readiness, the legacy 3x3 scan order, and a receipt that retains the exact station through the commit callbacks.
        const string Kind = "forge";
        internal static bool Current(Zone zone) => StationReadiness.CurrentZone(zone);
        internal static bool Ready(Entity owner, Zone zone) => StationReadiness.Evaluate(owner, zone, Kind, out _);
        internal static bool Near(Entity actor, Entity owner, Zone zone) => StationQuery.Near(actor, owner, zone, Kind);
        internal static Entity Resolve(Entity actor, Zone zone) => StationQuery.Resolve(actor, zone, Kind);
        internal static bool Begin(Entity actor, Zone zone, InventoryTransaction tx, string action, Entity selected = null)
            => StationReceipt.Begin(actor, zone, tx, action, Kind, selected) != null;
    }
}
