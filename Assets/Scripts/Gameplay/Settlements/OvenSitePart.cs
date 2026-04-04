namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to oven entities that provides dynamic visual behavior:
    /// - Smoke/glow flicker (fouled stage flickers to dark gray)
    /// - Color degradation (temporary mending fades over time)
    /// - Aura lifecycle (starts/stops particle FX based on repair stage)
    /// - Proximity ambient messages (one-shot per zone visit when player is adjacent)
    /// </summary>
    public class OvenSitePart : Part
    {
        public override string Name => "OvenSite";

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

            if (site.Stage != _lastAppliedStage)
            {
                OnStageChanged(site.Stage);
                _lastAppliedStage = site.Stage;
            }

            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    // Dark gray flicker every 6 frames — cold, broken firebox
                    if (_renderFrameCounter % 6 == 0)
                        e.SetParameter("ColorString", "&K");
                    break;

                case RepairStage.TemporarilyPurified:
                    string degradedColor = GetDegradationColor(site);
                    e.SetParameter("ColorString", degradedColor);
                    break;

                case RepairStage.StableRepair:
                    // Steady orange — no flicker
                    break;

                case RepairStage.ImprovedWithCaretaker:
                    // Steady bright orange — no flicker
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

            Cell ovenCell = zone.GetEntityCell(ParentEntity);
            if (ovenCell == null)
                return true;

            Entity player = FindPlayer(zone);
            if (player == null)
                return true;

            Cell playerCell = zone.GetEntityCell(player);
            if (playerCell == null)
                return true;

            if (IsAdjacent(ovenCell.X, ovenCell.Y, playerCell.X, playerCell.Y))
            {
                _proximityMessageShown = true;
                string msg = GetProximityMessage(site.Stage);
                if (msg != null)
                    MessageLog.Add(msg);
            }

            return true;
        }

        public void OnStageChanged(RepairStage newStage)
        {
            Zone zone = GetZone();

            if (_auraStarted)
            {
                AsciiFxTheme? oldTheme = GetAuraTheme(_lastAppliedStage);
                if (oldTheme.HasValue)
                    AsciiFxBus.StopAura(ParentEntity, oldTheme.Value);
                _auraStarted = false;
            }

            if (zone != null)
                StartAuraForStage(newStage, zone);

            _lastAppliedStage = newStage;
        }

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
                return "&R";

            int currentTurn = GetCurrentTurn();
            int resolvedAt = site.ResolvedAtTurn;
            int relapseAt = site.RelapseAtTurn.Value;
            int totalDuration = relapseAt - resolvedAt;
            if (totalDuration <= 0)
                return "&K";

            int remaining = relapseAt - currentTurn;
            float ratio = (float)remaining / totalDuration;

            if (ratio <= 0f)
                return "&K";

            // 100-75%: bright orange, 75-50%: red, 50-25%: yellow-gray, 25-0%: dark gray with flicker
            if (ratio > 0.75f)
                return "&R";
            if (ratio > 0.50f)
                return "&Y";
            if (ratio > 0.25f)
                return "&y";

            if (_renderFrameCounter % 16 == 0)
                return "&w";
            return "&K";
        }

        private static AsciiFxTheme? GetAuraTheme(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return AsciiFxTheme.OvenBroken;
                case RepairStage.TemporarilyPurified:
                case RepairStage.StableRepair:
                    return AsciiFxTheme.OvenWorking;
                case RepairStage.ImprovedWithCaretaker:
                    return AsciiFxTheme.OvenImproved;
                default:
                    return null;
            }
        }

        private static string GetProximityMessage(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return "Ash and soot drift from the cracked firebox.";
                case RepairStage.TemporarilyPurified:
                    return "The oven holds, but the mending won't last forever.";
                case RepairStage.StableRepair:
                    return "Warm air rises from the rebuilt firebox.";
                case RepairStage.ImprovedWithCaretaker:
                    return "The oven glows steadily. Fresh bread cools on the ledge.";
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
