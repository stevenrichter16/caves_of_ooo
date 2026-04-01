namespace CavesOfOoo.Core
{
    public static class SettlementRepairDefinitions
    {
        public const int PurifyRelapseTurns = 120;
        public const string WellMaintenanceManualBlueprint = "WellMaintenanceManual";
        public const string SilverSandBlueprint = "SilverSand";

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
                default:
                    return "That repair method is not available.";
            }
        }
    }
}
