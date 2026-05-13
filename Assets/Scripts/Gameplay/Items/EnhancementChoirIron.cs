namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.3 — <b>Choir-Iron</b>. The second
    /// content-mineral-derived enhancement, sibling of
    /// <see cref="EnhancementPaleSalt"/>, targeting Fungal-tagged
    /// defenders.
    ///
    /// <para><b>Filter:</b> melee weapons only (inherits from
    /// <see cref="EnhancementTagBonusBase"/>).</para>
    ///
    /// <para><b>Mechanic:</b> on a successful melee hit, if the
    /// defender's <see cref="MaterialPart"/> carries the
    /// <c>"Fungal"</c> tag, an additional <see cref="EnhancementTagBonusBase.BonusDamage"/>
    /// is applied. Tier 1 → +2, Tier 4 → +8 bonus. Mirrors
    /// EnhancementPaleSalt but flipped to fungal targets — IDEAS.md
    /// frames Choir-Iron's biology as iron's redox toxicity to
    /// mycelial enzymes.</para>
    ///
    /// <para><b>Source mineral:</b> <c>ChoirIron</c> blueprint (E.3.2).
    /// Applied via Tinker recipe (E.3.4).</para>
    ///
    /// <para><b>IDEAS.md parity (verbatim):</b> "high-iron substrates
    /// are genuinely *toxic* to many fungi (iron's redox chemistry
    /// interferes with mycelial enzymes)... Infused: weapons resist
    /// Choir colonization." We express "weapons resist Choir
    /// colonization" as v1 bonus damage vs Fungal-tagged defenders.
    /// Forward-compatible with the full Choir-colonization mechanic
    /// when it ships.</para>
    ///
    /// <para><b>Deferred from full IDEAS.md design:</b> Choir-Iron's
    /// passive "carrying reduces local Choir spore density" + armor
    /// "reduces Bloomed status duration from Driving Bloom infections"
    /// both require Choir spore + Bloomed-effect systems that don't
    /// exist yet. Documented as scope-prune in E.3.1 + Docs/ITEM-ENHANCEMENTS.md.</para>
    /// </summary>
    public class EnhancementChoirIron : EnhancementTagBonusBase
    {
        public override string TargetMaterialTag => "Fungal";

        public override string Name => nameof(EnhancementChoirIron);

        public override string GetDisplayName() => "Choir-Iron-edged";
    }
}
