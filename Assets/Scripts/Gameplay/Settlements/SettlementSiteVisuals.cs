namespace CavesOfOoo.Core
{
    public static class SettlementSiteVisuals
    {
        public static void ApplyToEntity(Entity entity, RepairableSiteState site)
        {
            if (entity == null || site == null)
                return;

            var render = entity.GetPart<RenderPart>();
            if (render == null)
                return;

            switch (site.Stage)
            {
                case RepairStage.Fouled:
                    render.DisplayName = "fouled well";
                    render.ColorString = "&y";
                    break;
                case RepairStage.TemporarilyPurified:
                    render.DisplayName = "freshened well";
                    render.ColorString = "&W";
                    break;
                case RepairStage.StableRepair:
                    render.DisplayName = "repaired well";
                    render.ColorString = "&c";
                    break;
                case RepairStage.ImprovedWithCaretaker:
                    render.DisplayName = "maintained well";
                    render.ColorString = "&C";
                    break;
                default:
                    render.DisplayName = "well";
                    render.ColorString = "&c";
                    break;
            }
        }
    }
}
