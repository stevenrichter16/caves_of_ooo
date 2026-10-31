namespace CavesOfOoo.Core
{
    public enum RepairMethodId
    {
        None,
        PurifySpell,
        ManualRepair,
        TeachCaretaker,
        MendingRite,
        OvenRebuild,
        TeachBaker,
        KindleRite,
        LanternReforge,
        TeachWarden,
        /// <summary>C1: a stable repair bought from the Saccharine Concord's trade-post
        /// (ContractPlaceRepair). The place owes the Concord its upkeep.</summary>
        Contracted,
        /// <summary>C3c: a data-driven site patched for now (PlaceSiteCatalog Patch); relapses.</summary>
        PatchedRepair
    }
}
