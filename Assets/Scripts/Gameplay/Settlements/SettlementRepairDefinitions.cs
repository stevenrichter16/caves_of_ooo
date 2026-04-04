namespace CavesOfOoo.Core
{
    public static class SettlementRepairDefinitions
    {
        public const int PurifyRelapseTurns = 120;
        public const string WellMaintenanceManualBlueprint = "WellMaintenanceManual";
        public const string SilverSandBlueprint = "SilverSand";

        public const int MendingRelapseTurns = 100;
        public const string OvenBuildersGuideBlueprint = "OvenBuildersGuide";
        public const string FireClayBlueprint = "FireClay";

        public const int KindleRelapseTurns = 80;
        public const string LanternOilRecipeBlueprint = "LanternOilRecipe";
        public const string WardOilBlueprint = "WardOil";

        public static string GetMethodFailureMessage(RepairMethodId method)
        {
            switch (method)
            {
                case RepairMethodId.PurifySpell:
                    return "You need to know a water-purification rite to do that.";
                case RepairMethodId.ManualRepair:
                    return "You'll need the well maintenance manual and some silver sand to repair the filtration ring.";
                case RepairMethodId.TeachCaretaker:
                    return "The villagers need a stable repair before you can teach them upkeep.";
                case RepairMethodId.MendingRite:
                    return "You need to know the mending rite to do that.";
                case RepairMethodId.OvenRebuild:
                    return "You'll need the oven builder's guide and some fire clay to rebuild the firebox.";
                case RepairMethodId.TeachBaker:
                    return "The oven needs a stable repair before you can teach the farmer to maintain it.";
                case RepairMethodId.KindleRite:
                    return "You need to know the kindle rite to relight the lantern.";
                case RepairMethodId.LanternReforge:
                    return "You'll need the lantern oil recipe and some ward oil to reforge the lantern.";
                case RepairMethodId.TeachWarden:
                    return "The lantern needs a stable repair before you can teach the warden to maintain it.";
                default:
                    return "That repair method is not available.";
            }
        }
    }
}
