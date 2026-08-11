namespace CavesOfOoo.Core
{
    /// <summary>
    /// What a blade + haft + binding would become, computed without
    /// consuming anything.
    ///
    /// <para>Exists for the Crafting panel's RESULT box
    /// (Docs/CRAFTING-FROM-THE-PACK.md §C1). The panel recomputes this on
    /// every selection change, so it must be free of side effects — no
    /// entity creation, no inventory access, no message log.</para>
    ///
    /// <para><b>The anti-drift rule:</b> the real forge does not compute
    /// stats separately. <c>WeaponForgingService.ApplyComponentStats</c>
    /// calls <c>PreviewForge</c> and copies the answer onto the weapon,
    /// so a preview that disagrees with the product is not a discrepancy
    /// to be fixed — it is impossible. A panel that promises 1d6+2 and
    /// forges a 1d4 would cost the player more trust than the whole
    /// feature is worth.</para>
    /// </summary>
    public struct ForgePreview
    {
        /// <summary>True when all three required slots were supplied.</summary>
        public bool IsComplete;

        /// <summary>Human-readable list of the slots still needed, e.g.
        /// "a haft and a binding". Empty when complete.</summary>
        public string Missing;

        public string DisplayName;
        public string BaseDamage;
        public int PenBonus;
        public int HitBonus;
        public int MaxStrengthBonus;
        public string Attributes;
        public string OnHitEffectsRaw;
    }
}
