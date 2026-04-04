namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to watch lantern entities that provides dynamic visual behavior:
    /// - Darkness flicker (fouled stage flickers between dark colors)
    /// - Color degradation (temporary kindle fades over time)
    /// - Aura lifecycle (starts/stops particle FX based on repair stage)
    /// - Proximity ambient messages (one-shot per zone visit when player is adjacent)
    /// </summary>
    public class LanternSitePart : Part
    {
        public override string Name => "LanternSite";

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
                    // Dark flicker every 10 frames — guttered lantern
                    if (_renderFrameCounter % 10 == 0)
                        e.SetParameter("ColorString", "&K");
                    break;

                case RepairStage.TemporarilyPurified:
                    string degradedColor = GetDegradationColor(site);
                    e.SetParameter("ColorString", degradedColor);
                    break;

                case RepairStage.StableRepair:
                    // Steady yellow — no flicker
                    break;

                case RepairStage.ImprovedWithCaretaker:
                    // Steady bright white — no flicker
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

            Cell lanternCell = zone.GetEntityCell(ParentEntity);
            if (lanternCell == null)
                return true;

            Entity player = FindPlayer(zone);
            if (player == null)
                return true;

            Cell playerCell = zone.GetEntityCell(player);
            if (playerCell == null)
                return true;

            if (IsAdjacent(lanternCell.X, lanternCell.Y, playerCell.X, playerCell.Y))
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
                return "&Y";

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

            // 100-75%: bright yellow, 75-50%: yellow, 50-25%: gray, 25-0%: dark with flicker
            if (ratio > 0.75f)
                return "&Y";
            if (ratio > 0.50f)
                return "&y";
            if (ratio > 0.25f)
                return "&w";

            if (_renderFrameCounter % 12 == 0)
                return "&K";
            return "&w";
        }

        private static AsciiFxTheme? GetAuraTheme(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return AsciiFxTheme.LanternDark;
                case RepairStage.TemporarilyPurified:
                case RepairStage.StableRepair:
                    return AsciiFxTheme.LanternLit;
                case RepairStage.ImprovedWithCaretaker:
                    return AsciiFxTheme.LanternBright;
                default:
                    return null;
            }
        }

        private static string GetProximityMessage(RepairStage stage)
        {
            switch (stage)
            {
                case RepairStage.Fouled:
                    return "The watch lantern is dark. Shadows press close.";
                case RepairStage.TemporarilyPurified:
                    return "The lantern flickers weakly. It won't last.";
                case RepairStage.StableRepair:
                    return "Steady light pools around the lantern post.";
                case RepairStage.ImprovedWithCaretaker:
                    return "The lantern burns bright and sure. Nothing creeps close while it shines.";
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
