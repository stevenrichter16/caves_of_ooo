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
                ApplyGroundMarkerVisuals(entity, site);
                return;
            }

            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            // Graduated display names and base colors per stage
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

        private static void ApplyGroundMarkerVisuals(Entity entity, RepairableSiteState site)
        {
            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            // Deterministic variation based on entity position hash
            int hash = (entity.ID ?? "").GetHashCode();
            bool variant = (hash & 1) != 0;

            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.RenderString = variant ? "," : ".";
                    render.ColorString = variant ? "&g" : "&w";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.RenderString = ".";
                    render.ColorString = "&c";
                    break;
                case RepairStage.StableRepair:
                    render.RenderString = ".";
                    render.ColorString = "&c";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    // One marker variant becomes a Palimpsest mark
                    render.RenderString = variant ? "'" : ".";
                    render.ColorString = variant ? "&M" : "&C";
                    break;
                default:
                    render.RenderString = ".";
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
