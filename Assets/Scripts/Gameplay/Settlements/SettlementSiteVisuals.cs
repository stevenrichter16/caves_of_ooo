namespace CavesOfOoo.Core
{
    public static class SettlementSiteVisuals
    {
        public static void ApplyToEntity(Entity entity, RepairableSiteState site)
        {
            if (entity == null || site == null)
                return;

            // Ground markers get separate visual treatment
            if (entity.HasTag("WellGroundMarker"))
            {
                ApplyWellGroundMarkerVisuals(entity, site);
                return;
            }

            if (entity.HasTag("OvenGroundMarker"))
            {
                ApplyOvenGroundMarkerVisuals(entity, site);
                return;
            }

            if (entity.HasTag("LanternGroundMarker"))
            {
                ApplyLanternGroundMarkerVisuals(entity, site);
                return;
            }

            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            if (site.SiteType == RepairableSiteType.HeatStone)
            {
                ApplyOvenVisuals(entity, render, site);
                return;
            }

            if (site.SiteType == RepairableSiteType.LightBeacon)
            {
                ApplyLanternVisuals(entity, render, site);
                return;
            }

            // Well visuals — graduated display names and base colors per stage
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.DisplayName = "fouled well (cracked ring)";
                    render.ColorString = "&y";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.DisplayName = "freshened well (temporary)";
                    render.ColorString = "&W";
                    break;
                case RepairStage.StableRepair:
                    render.DisplayName = "repaired well";
                    render.ColorString = "&c";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.DisplayName = GetImprovedDisplayName(entity);
                    render.ColorString = "&C";
                    break;
                default:
                    render.DisplayName = "well";
                    render.ColorString = "&c";
                    break;
            }

            // Notify WellSitePart so it can manage aura lifecycle
            var wellPart = entity.GetPart<WellSitePart>();
            if (wellPart != null)
                wellPart.OnStageChanged(site.Stage);
        }

        private static void ApplyOvenVisuals(Entity entity, RenderPart render, RepairableSiteState site)
        {
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.DisplayName = "cracked oven";
                    render.ColorString = "&K";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.DisplayName = "patched oven (temporary)";
                    render.ColorString = "&y";
                    break;
                case RepairStage.StableRepair:
                    render.DisplayName = "repaired oven";
                    render.ColorString = "&R";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.DisplayName = "village bakehouse";
                    render.ColorString = "&Y";
                    break;
                default:
                    render.DisplayName = "oven";
                    render.ColorString = "&y";
                    break;
            }

            var ovenPart = entity.GetPart<OvenSitePart>();
            if (ovenPart != null)
                ovenPart.OnStageChanged(site.Stage);
        }

        private static void ApplyOvenGroundMarkerVisuals(Entity entity, RepairableSiteState site)
        {
            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            int hash = (entity.ID ?? "").GetHashCode();
            bool variant = (hash & 1) != 0;

            render.RenderString = ".";
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.ColorString = variant ? "&K" : "&y";
                    break;
                case RepairStage.TemporarilyPurified:
                case RepairStage.StableRepair:
                    render.ColorString = variant ? "&R" : "&Y";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.ColorString = variant ? "&Y" : "&W";
                    break;
                default:
                    render.ColorString = "&y";
                    break;
            }
        }

        private static void ApplyWellGroundMarkerVisuals(Entity entity, RepairableSiteState site)
        {
            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            // Deterministic variation based on entity position hash
            int hash = (entity.ID ?? "").GetHashCode();
            bool variant = (hash & 1) != 0;

            render.RenderString = ".";
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.ColorString = variant ? "&g" : "&w";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.ColorString = "&c";
                    break;
                case RepairStage.StableRepair:
                    render.ColorString = "&c";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.ColorString = variant ? "&M" : "&C";
                    break;
                default:
                    render.ColorString = "&w";
                    break;
            }
        }

        private static void ApplyLanternVisuals(Entity entity, RenderPart render, RepairableSiteState site)
        {
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.DisplayName = "dimmed watch lantern";
                    render.ColorString = "&K";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.DisplayName = "flickering watch lantern (temporary)";
                    render.ColorString = "&y";
                    break;
                case RepairStage.StableRepair:
                    render.DisplayName = "watch lantern";
                    render.ColorString = "&Y";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.DisplayName = "warden's beacon";
                    render.ColorString = "&W";
                    break;
                default:
                    render.DisplayName = "watch lantern";
                    render.ColorString = "&y";
                    break;
            }

            var lanternPart = entity.GetPart<LanternSitePart>();
            if (lanternPart != null)
                lanternPart.OnStageChanged(site.Stage);
        }

        private static void ApplyLanternGroundMarkerVisuals(Entity entity, RepairableSiteState site)
        {
            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            int hash = (entity.ID ?? "").GetHashCode();
            bool variant = (hash & 1) != 0;

            render.RenderString = ".";
            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.ColorString = variant ? "&K" : "&w";
                    break;
                case RepairStage.TemporarilyPurified:
                case RepairStage.StableRepair:
                    render.ColorString = variant ? "&y" : "&Y";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.ColorString = variant ? "&Y" : "&W";
                    break;
                default:
                    render.ColorString = "&w";
                    break;
            }
        }

        private static string GetImprovedDisplayName(Entity entity)
        {
            string settlementId = entity.GetProperty("SettlementId");
            if (SettlementManager.Current != null && !string.IsNullOrEmpty(settlementId))
            {
                var settlement = SettlementManager.Current.GetSite(settlementId, SettlementSiteDefinitions.MainWellSiteId);
                // Try to get settlement name from POI — for now, use a clean default
            }
            return "maintained well";
        }
    }
}
