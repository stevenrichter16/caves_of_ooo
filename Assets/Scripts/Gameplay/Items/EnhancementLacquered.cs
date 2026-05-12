using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.2.3 — <b>Lacquered</b>. The first concrete
    /// defensive enhancement: adds a tier-scaled flat <see cref="ArmorPart.AV"/>
    /// bonus to a piece of armor while it is equipped, removing the
    /// bonus on unequip.
    ///
    /// <para><b>Filter:</b> requires <see cref="ArmorPart"/>. Rejects
    /// melee weapons, tonics, anything without an Armor part.</para>
    ///
    /// <para><b>Effect:</b> on <see cref="OnEquipped"/> adds
    /// <see cref="AvBonus"/> to <c>ArmorPart.AV</c>; on
    /// <see cref="OnUnequipped"/> subtracts it. Net change is zero
    /// across a full equip/unequip cycle.</para>
    ///
    /// <para><b>Atomicity (Lockdown #3 / F.3.4 lesson):</b> the
    /// <see cref="AppliedBonus"/> flag is set EAGERLY before the AV
    /// mutation in <see cref="OnEquipped"/>, and checked at the top of
    /// the method to prevent double-apply if the dispatch fires twice
    /// for the same equip transaction. Symmetric guard in
    /// <see cref="OnUnequipped"/> — only subtracts if the flag is set.
    /// Carried directly from the F.3.4 GrantsRepAsFollowerPart audit.</para>
    ///
    /// <para><b>Tier scaling:</b></para>
    /// <list type="table">
    ///   <listheader><term>Tier</term><description>AV Bonus</description></listheader>
    ///   <item><term>1</term><description>+1</description></item>
    ///   <item><term>2</term><description>+2</description></item>
    ///   <item><term>3</term><description>+3</description></item>
    ///   <item><term>4</term><description>+4</description></item>
    /// </list>
    ///
    /// <para><b>Qud parity:</b> Qud has TWO related Mods:
    /// <c>ModReinforced</c> grants +1 AV on a Body/Back armor (the +AV
    /// mechanic CoO mirrors). <c>ModLacquered</c> grants liquid-repulsion
    /// + rust-immunity (CoO doesn't have liquid/rust systems yet).
    /// CoO's <c>EnhancementLacquered</c> borrows Qud's "Lacquered"
    /// thematic name for armor enhancement but ships the AV-bonus
    /// mechanic from <c>ModReinforced</c> — Qud's liquid mechanics
    /// will land in a later phase if needed. Documented in
    /// <c>Docs/ITEM-ENHANCEMENTS.md</c>.</para>
    /// </summary>
    public class EnhancementLacquered : IItemEnhancement
    {
        /// <summary>AV bonus per tier. Tier 1 → +1 AV, Tier 4 → +4 AV.</summary>
        public const int AV_BONUS_PER_TIER = 1;

        // --- Round-tripped state -----------------------------------

        /// <summary>Computed by <see cref="TierConfigure"/>. Persisted
        /// independently of <see cref="Part.ParentEntity"/> so save/load
        /// of an in-flight equipped state preserves the right delta to
        /// subtract on unequip.</summary>
        public int AvBonus;

        /// <summary>Atomicity flag (F.3.4 lesson): set EAGERLY in
        /// <see cref="OnEquipped"/> before mutating AV. If the
        /// dispatcher fires twice for the same equip transaction (e.g.
        /// via undo+redo paths), the second call sees the flag and
        /// no-ops. Symmetric guard prevents subtract-without-apply in
        /// <see cref="OnUnequipped"/>. Round-trips via reflection.</summary>
        public bool AppliedBonus;

        public override string Name => nameof(EnhancementLacquered);

        public override string GetDisplayName() => "Lacquered";

        // --- Lifecycle ---------------------------------------------

        public override void TierConfigure()
        {
            AvBonus = Tier * AV_BONUS_PER_TIER;
        }

        public override bool Applicable(Entity item)
        {
            if (!base.Applicable(item)) return false;
            return item.GetPart<ArmorPart>() != null;
        }

        // --- Equip hooks -------------------------------------------

        public override void OnEquipped(Entity actor, Entity item)
        {
            // Atomicity guard FIRST (Lockdown #3 — set the flag before
            // any mutation that could fail mid-flight). Double-equip
            // dispatch is a no-op.
            if (AppliedBonus) return;
            if (item == null) return;
            var armor = item.GetPart<ArmorPart>();
            if (armor == null) return;

            // Eager-flag pattern: set AppliedBonus FIRST so even if the
            // AV mutation throws (it shouldn't, but if a future Part
            // observer does), a retry won't double-add.
            AppliedBonus = true;
            armor.AV += AvBonus;

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusApplied",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementLacquered),
                        tier = Tier,
                        avBonus = AvBonus,
                        avAfter = armor.AV
                    });
            }
        }

        public override void OnUnequipped(Entity actor, Entity item)
        {
            // Symmetric guard: only subtract if we actually added.
            // Protects against unequip-without-equip (shouldn't happen
            // in normal flow, but a stale enhancement loaded onto an
            // already-unequipped item would crash AV otherwise).
            if (!AppliedBonus) return;
            if (item == null) return;
            var armor = item.GetPart<ArmorPart>();
            if (armor == null) return;

            armor.AV -= AvBonus;
            AppliedBonus = false;

            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "BonusRemoved",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        enhancement = nameof(EnhancementLacquered),
                        tier = Tier,
                        avBonus = AvBonus,
                        avAfter = armor.AV
                    });
            }
        }
    }
}
