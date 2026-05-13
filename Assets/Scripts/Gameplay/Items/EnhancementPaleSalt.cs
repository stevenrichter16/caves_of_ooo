namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.3 — <b>Pale-Salt</b>. The first
    /// content-mineral-derived enhancement targeting Undead-tagged
    /// defenders.
    ///
    /// <para><b>Filter:</b> melee weapons only (inherits from
    /// <see cref="EnhancementTagBonusBase"/> which extends
    /// <see cref="IMeleeEnhancement"/>).</para>
    ///
    /// <para><b>Mechanic:</b> on a successful melee hit, if the
    /// defender's <see cref="MaterialPart"/> carries the
    /// <c>"Undead"</c> tag, an additional <see cref="EnhancementTagBonusBase.BonusDamage"/>
    /// is applied via <c>CombatSystem.ApplyDamage</c>. Tier 1 → +2,
    /// Tier 4 → +8 bonus.</para>
    ///
    /// <para><b>Source mineral:</b> <c>PaleSalt</c> blueprint (E.3.2).
    /// Applied via Tinker recipe (E.3.4).</para>
    ///
    /// <para><b>IDEAS.md parity (verbatim):</b> "Pale-Salt-edged
    /// weapons inflict bonus damage on Driving Bloom and undead-tier
    /// enemies (salt as preservation/desiccation mechanic)." We pin
    /// the Undead half for v1 (Fungal/Driving Bloom is Choir-Iron's
    /// domain — splitting the two targets per-mineral preserves
    /// design distinctness).</para>
    ///
    /// <para><b>Deferred from full IDEAS.md design:</b> Pale-Salt's
    /// passive "inventory food doesn't spoil" requires a food-
    /// spoilage system that doesn't exist in CoO yet. Documented as
    /// a scope-prune in E.3.1 verification sweep + Docs/ITEM-ENHANCEMENTS.md.</para>
    /// </summary>
    public class EnhancementPaleSalt : EnhancementTagBonusBase
    {
        public override string TargetMaterialTag => "Undead";

        public override string Name => nameof(EnhancementPaleSalt);

        public override string GetDisplayName() => "Pale-Salt-edged";
    }
}
