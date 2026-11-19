using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an entity as an installation (a placed machine) and says whether a cached village that holds it may be unloaded for
    /// regeneration (Docs/MACHINE-INSTALLATIONS.md, spec contract C2). A relapsing settlement repair unloads its village and the
    /// next visit regenerates it from the seed, which would reset a state-bearing machine's finite state; one conjunct in
    /// <c>OverworldZoneManager.CanUnloadZone</c> asks <see cref="Retain"/> instead of every machine adding its own predicate.
    /// Public default-constructible fields only: saves reflect them by name and older saves simply lack the Part.
    /// </summary>
    public sealed class InstallationMarkerPart : Part
    {
        public override string Name => "InstallationMarker";

        /// <summary>The spec row id; "player:&lt;entityId&gt;" for later player installs.</summary>
        public string InstallationId = "";
        /// <summary>Generated | Player | Relocated. Only Relocated retains by itself; a Generated or Player marker retains through <see cref="RetainZone"/>,
        /// which the install verbs (S19) set while the fixture stands and clear when it is taken up, so a dropped or carried fixture never pins a village.</summary>
        public string Origin = "Generated";
        /// <summary>The registry revision seen at placement.</summary>
        public int Revision = 1;
        /// <summary>True for a state-bearing kind: a cached village containing this entity is never unloaded for regeneration.</summary>
        public bool RetainZone;

        /// <summary>Shipped finite fixtures that cannot take a Part (their exact-Part validators pin the Part list). The rig is retained
        /// wherever it is hauled, so retention never depends on a later stamp.</summary>
        public static readonly string[] LegacyRetainedBlueprints = { "PeatOilStrainer", "AbandonedFieldWorkRig", "CinderholdOilRecoveryTray" };

        /// <summary>Why this entity retains its zone: "marker", "legacy-blueprint", or "" when it does not.</summary>
        public static string RetentionReason(Entity entity)
        {
            if (entity == null) return "";
            var marker = entity.GetPart<InstallationMarkerPart>();
            if (marker != null && (marker.RetainZone || marker.Origin == "Relocated")) return "marker";
            return System.Array.IndexOf(LegacyRetainedBlueprints, entity.BlueprintName) >= 0 ? "legacy-blueprint" : "";
        }

        public static bool Retains(Entity entity) => RetentionReason(entity).Length > 0;

        /// <summary>True when the zone is cached and holds at least one retaining entity. Never throws; a null manager or an unknown
        /// zone retains nothing. Runs at relapse time only, so a single scan of the zone's entities is cheap.</summary>
        public static bool Retain(ZoneManager manager, string zoneID)
        {
            if (manager == null || string.IsNullOrEmpty(zoneID) || !manager.CachedZones.TryGetValue(zoneID, out var zone) || zone == null) return false;
            int count = 0; string reason = "";
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                string why = RetentionReason(entity);
                if (why.Length == 0) continue;
                if (count++ == 0) reason = why;
            }
            if (count == 0) return false;
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "ZoneRetained", payload: new { zone = zoneID, reason, count });
            return true;
        }
    }
}
