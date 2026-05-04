namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static notification surface for equipment-state changes. Anything that
    /// mutates an entity's equipped-items dictionary (InventoryPart's Equip /
    /// Unequip / EquipToBodyPart / UnequipFromBodyPart) calls
    /// <see cref="NotifyChanged"/>, which bumps a global monotonic version
    /// counter. Subscribers — currently only <see cref="LightMap"/>, but the
    /// surface is open — read <see cref="GlobalVersion"/> to detect equipment
    /// changes between their cached snapshots and the present moment.
    ///
    /// <para><b>Why a global counter, not per-zone:</b> Entity does not carry
    /// a back-reference to its current Zone, so InventoryPart cannot resolve
    /// "which zone's EntityVersion should I bump?" without invasive plumbing.
    /// A global counter sidesteps this — LightMap is per-zone but reads a
    /// shared version that bumps on equipment events anywhere. The minor
    /// over-trigger (an equip in zone A invalidates zone B's LightMap cache)
    /// is acceptable because LightMap.Compute only runs for the active zone
    /// each frame, and equip events are rare (player-intentional).</para>
    ///
    /// <para><b>Why not an event-callback API:</b> Static C# events have
    /// subscriber-leak risk if subscribers are never unhooked. A simple
    /// integer counter has no leak risk and reads cleanly into LightMap's
    /// existing <c>_lastEntityVersion</c> short-circuit pattern.</para>
    ///
    /// <para><b>Closes T2.2 methodology debt:</b> the LightMap docstring at
    /// <c>LightMap.cs:64-73</c> previously documented a "next entity move"
    /// eventual-consistency limitation as a 🟡 self-review finding. With
    /// this bus in place, equipment changes invalidate the LightMap cache
    /// immediately on the next render call.</para>
    /// </summary>
    public static class EquipmentChangeBus
    {
        /// <summary>
        /// Monotonic counter. Increments by 1 on every NotifyChanged call.
        /// Subscribers compare against their own snapshot to detect change.
        /// Starts at 0; never resets (even on save/load, the version drifts
        /// forward — subscribers re-snapshot on next compute).
        /// </summary>
        public static int GlobalVersion { get; private set; }

        /// <summary>
        /// Called by InventoryPart when an entity's equipment changes.
        /// <paramref name="wielder"/> is the entity whose inventory mutated;
        /// currently unused by subscribers (they invalidate globally) but
        /// retained for future targeted-invalidation work.
        /// </summary>
        public static void NotifyChanged(Entity wielder)
        {
            GlobalVersion++;
        }

        /// <summary>
        /// Test-only reset. Allows fixtures to start from a known version
        /// state without leaking equipment events between tests. Production
        /// code should never call this — the counter is monotonic by design.
        /// </summary>
        public static void ResetForTests()
        {
            GlobalVersion = 0;
        }
    }
}
