using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.3.3 — abstract base for the
    /// "bonus-damage-vs-tagged-defender" pattern used by Pale-Salt
    /// (vs Undead) and Choir-Iron (vs Fungal). Both minerals from
    /// IDEAS.md describe a "weapon does extra damage vs creature
    /// type X" mechanic; this base extracts the shared dispatch and
    /// the per-tier bonus-damage scaling.
    ///
    /// <para><b>Filter:</b> melee-weapon-only (via
    /// <see cref="IMeleeEnhancement"/>). Subclasses can chain
    /// additional filtering in their own <see cref="Applicable"/>
    /// override if needed.</para>
    ///
    /// <para><b>Mechanic:</b> on <see cref="OnAttackerHit"/>, if the
    /// defender carries the subclass-declared
    /// <see cref="TargetMaterialTag"/> in its <see cref="MaterialPart"/>,
    /// an additional <see cref="BonusDamage"/> is applied via
    /// <c>CombatSystem.ApplyDamage</c>. Tier scales the bonus
    /// linearly: <c>BONUS_DAMAGE_PER_TIER * Tier</c>.</para>
    ///
    /// <para><b>Why not include this as a separate Damage object with
    /// its own attributes?</b> Future polish could promote bonus
    /// damage to a real <see cref="Damage"/> with e.g. "Salt" or
    /// "AntiFungal" attributes so resistances apply. For v1 we apply
    /// flat damage — keeps the substrate narrow and lets the tag
    /// system (which lives on MaterialPart) carry the semantic load.
    /// Easy to upgrade later; marked 🟡 in self-review.</para>
    ///
    /// <para><b>Save/load (SL.6):</b> Tier + BonusDamage round-trip
    /// via reflection. <see cref="TargetMaterialTag"/> is a property
    /// (not a field), so subclasses are responsible for keeping it
    /// hardcoded (declarative content, not save-state).</para>
    /// </summary>
    public abstract class EnhancementTagBonusBase : IMeleeEnhancement
    {
        /// <summary>Bonus damage step per tier. Tier 1 → +2, Tier 4 → +8.</summary>
        public const int BONUS_DAMAGE_PER_TIER = 2;

        /// <summary>Subclass-declared MaterialTag string the defender
        /// must carry for the bonus to apply. Hardcoded by the
        /// concrete enhancement class (e.g. <c>"Undead"</c> for
        /// Pale-Salt, <c>"Fungal"</c> for Choir-Iron).</summary>
        public abstract string TargetMaterialTag { get; }

        /// <summary>Computed in <see cref="TierConfigure"/>. Flat
        /// additional damage applied when the defender matches
        /// <see cref="TargetMaterialTag"/>. Round-trips via reflection.</summary>
        public int BonusDamage;

        public override void TierConfigure()
        {
            BonusDamage = Tier * BONUS_DAMAGE_PER_TIER;
        }

        public override void OnAttackerHit(
            Entity defender, Entity attacker, Damage damage,
            int actualDamage, Zone zone, System.Random rng)
        {
            // Mirror OnHitClassEffects's contract: a fully-resisted
            // primary hit doesn't trigger ANY on-hit bonus. (A 0-dmg
            // hit on a fully Undead-resistant zombie shouldn't still
            // light up the Salt-bonus.)
            if (defender == null) return;
            if (actualDamage <= 0) return;

            var mat = defender.GetPart<MaterialPart>();
            if (mat == null) return;
            if (!mat.HasMaterialTag(TargetMaterialTag)) return;

            // Apply flat bonus damage via the canonical CombatSystem
            // path so resistances + lethal-handling all flow correctly.
            CombatSystem.ApplyDamage(defender, BonusDamage, attacker, zone);

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "Triggered",
                    actor: attacker,
                    target: defender,
                    payload: new
                    {
                        enhancement = GetType().Name,
                        tier = Tier,
                        bonusDamage = BonusDamage,
                        targetTag = TargetMaterialTag
                    });
            }
        }
    }
}
