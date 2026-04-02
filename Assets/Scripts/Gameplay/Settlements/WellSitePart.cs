namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to well entities that provides dynamic visual behavior:
    /// - Contamination flash (fouled stage flickers to red)
    /// - Color degradation (temporary purification fades over time)
    /// - Aura lifecycle (starts/stops particle FX based on repair stage)
    /// - Proximity ambient messages (one-shot per zone visit when player is adjacent)
    /// </summary>
    public class WellSitePart : Part
    {
        public override string Name => "WellSite";

        public string SettlementId;
        public string SiteId;

        private int _renderFrameCounter;
        private bool _proximityMessageShown;
        private RepairStage _lastAppliedStage = RepairStage.Fouled;
        private bool _auraStarted;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Render")
                return HandleRender(e);
            if (e.ID == "EndTurn")
                return HandleEndTurn(e);
            return true;
        }

        private bool HandleRender(GameEvent e)
        {
            RepairableSiteState site = GetSiteState();
            if (site == null)
                return true;

            _renderFrameCounter++;

            // Detect stage change for aura management
            if (site.Stage != _lastAppliedStage)
            {
                OnStageChanged(site.Stage);
                _lastAppliedStage = site.Stage;
            }

            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    // Flash to dark red every 8th frame — contamination bleeding through
                    if (_renderFrameCounter % 8 == 0)
                        e.SetParameter("ColorString", "&r");
                    break;

                case RepairStage.TemporarilyPurified:
                    string degradedColor = GetDegradationColor(site);
                    e.SetParameter("ColorString", degradedColor);
                    break;

                case RepairStage.StableRepair:
                    // Stable cyan — no flicker
                    break;

                case RepairStage.ImprovedWithCaretaker:
                    // Stable bright cyan — no flicker
                    break;
            }

            return true;
        }

        private bool HandleEndTurn(GameEvent e)
        {
            if (_proximityMessageShown)
                return true;

            if (ParentEntity == null)
                return true;

            RepairableSiteState site = GetSiteState();
            if (site == null)
                return true;

            Zone zone = GetZone();
            if (zone == null)
                return true;

            Cell wellCell = zone.GetEntityCell(ParentEntity);
            if (wellCell == null)
                return true;

            Entity player = FindPlayer(zone);
            if (player == null)
                return true;

            Cell playerCell = zone.GetEntityCell(player);
            if (playerCell == null)
                return true;

            if (IsAdjacent(wellCell.X, wellCell.Y, playerCell.X, playerCell.Y))
            {
                _proximityMessageShown = true;
                string msg = GetProximityMessage(site.Stage);
                if (msg != null)
                    MessageLog.Add(msg);
            }

            return true;
        }

        /// <summary>
        /// Called when the well's repair stage changes. Manages aura FX lifecycle.
        /// Can also be called externally by SettlementSiteVisuals after a repair.
        /// </summary>
        public void OnStageChanged(RepairStage newStage)
        {
            Zone zone = GetZone();

            // Stop the old aura if one was running
            if (_auraStarted)
            {
                AsciiFxTheme? oldTheme = GetAuraTheme(_lastAppliedStage);
                if (oldTheme.HasValue)
                    AsciiFxBus.StopAura(ParentEntity, oldTheme.Value);
                _auraStarted = false;
            }

            // Start the new aura
            if (zone != null)
                StartAuraForStage(newStage, zone);

            _lastAppliedStage = newStage;
        }

        /// <summary>
        /// Start the appropriate aura for the current stage. Called on zone entry
        /// and after stage changes.
        /// </summary>
        public void StartAuraForStage(RepairStage stage, Zone zone)
        {
            if (zone == null || ParentEntity == null)
                return;

            AsciiFxTheme? theme = GetAuraTheme(stage);
            if (theme.HasValue)
            {
                AsciiFxBus.StartAura(zone, ParentEntity, theme.Value);
                _auraStarted = true;
            }
        }

        public void ResetProximityMessage()
        {
            _proximityMessageShown = false;
        }

        private string GetDegradationColor(RepairableSiteState site)
        {
            if (!site.RelapseAtTurn.HasValue)
                return "&W";

            int currentTurn = GetCurrentTurn();
            int resolvedAt = site.ResolvedAtTurn;
            int relapseAt = site.RelapseAtTurn.Value;
            int totalDuration = relapseAt - resolvedAt;
            if (totalDuration <= 0)
                return "&y";

            int remaining = relapseAt - currentTurn;
            float ratio = (float)remaining / totalDuration;

            if (ratio <= 0f)
                return "&y";

            // 100-75%: bright yellow, 75-50%: white, 50-25%: gray, 25-0%: gray with brown flicker
            if (ratio > 0.75f)
                return "&W";
            if (ratio > 0.50f)
                return "&Y";
            if (ratio > 0.25f)
                return "&y";

            // Near expiry: occasional brown flicker
            if (_renderFrameCounter % 16 == 0)
                return "&w";
            return "&y";
        }

        private static AsciiFxTheme? GetAuraTheme(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return AsciiFxTheme.WellFouled;
                case RepairStage.TemporarilyPurified:
                case RepairStage.StableRepair:
                    return AsciiFxTheme.WellClean;
                case RepairStage.ImprovedWithCaretaker:
                    return AsciiFxTheme.WellImproved;
                default:
                    return null;
            }
        }

        private static string GetProximityMessage(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return "You hear water dripping unevenly.";
                case RepairStage.TemporarilyPurified:
                    return "The water smells clean, for now.";
                case RepairStage.StableRepair:
                    return "Clean water flows steadily below.";
                case RepairStage.ImprovedWithCaretaker:
                    return "Clean water flows steadily. Seasonal marks line the rim.";
                default:
                    return null;
            }
        }

        private RepairableSiteState GetSiteState()
        {
            if (SettlementManager.Current == null)
                return null;
            return SettlementManager.Current.GetSite(SettlementId, SiteId);
        }

        private int GetCurrentTurn()
        {
            return SettlementManager.Current?.GetCurrentTurn() ?? 0;
        }

        private Zone GetZone()
        {
            return SettlementRuntime.ActiveZone;
        }

        private static Entity FindPlayer(Zone zone)
        {
            if (zone == null)
                return null;

            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].HasTag("Player"))
                    return entities[i];
            }
            return null;
        }

        private static bool IsAdjacent(int x1, int y1, int x2, int y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx <= 1 && dy <= 1 && (dx + dy > 0);
        }
    }
}
