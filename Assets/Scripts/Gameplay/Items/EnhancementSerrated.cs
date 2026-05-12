using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.2.2 — <b>Serrated</b>. The first concrete
    /// offensive enhancement: gives Cutting-class melee weapons an
    /// additional, tier-scaled chance to apply
    /// <see cref="BleedingEffect"/> on a successful hit.
    ///
    /// <para><b>Filter:</b> melee-weapon-only (via
    /// <see cref="IMeleeEnhancement"/>) AND the weapon's
    /// <see cref="MeleeWeaponPart.Attributes"/> must contain
    /// <c>"Cutting"</c>. Rejects clubs, hammers, daggers/spears even
    /// though they're melee — Serrated only makes sense on slashing
    /// blades. Mirrors Qud's <c>ModSerrated.ModificationApplicable</c>
    /// gate (LongBlades / Axe only).</para>
    ///
    /// <para><b>Effect:</b> rolls
    /// <see cref="ChancePercent"/> on every successful melee hit
    /// (called via <see cref="ItemEnhancementDispatch.DispatchOnHit"/>);
    /// on success applies a <see cref="BleedingEffect"/> to the
    /// defender. The roll is independent of any other on-hit chance
    /// (Cutting→Bleed from <c>OnHitClassEffects</c>, weapon-blueprint
    /// on-hit specs from <c>OnHitWeaponEffects</c>) — Serrated STACKS
    /// on top of the baseline Cutting bleed chance.</para>
    ///
    /// <para><b>Tier scaling:</b></para>
    /// <list type="table">
    ///   <listheader><term>Tier</term><description>Chance</description></listheader>
    ///   <item><term>1</term><description>10%</description></item>
    ///   <item><term>2</term><description>20%</description></item>
    ///   <item><term>3</term><description>30%</description></item>
    ///   <item><term>4</term><description>40%</description></item>
    /// </list>
    /// <c>BLEED_CHANCE_PER_TIER * tier</c>. Concretely Tier 4 Serrated
    /// on a Cutting weapon gives ~25% (class) + ~40% (Serrated) =
    /// ~55% bleed probability per swing — meaningfully different from
    /// baseline, without being oppressive.
    ///
    /// <para><b>Qud parity:</b> Qud's <c>ModSerrated</c> rolls a 3%
    /// dismember chance on hit (mirrors hit-event registration +
    /// <c>WeaponHit</c> handler). CoO substitutes bleed for dismember
    /// because: (a) <c>CombatSystem.CheckCombatDismemberment</c> is
    /// private and integrating from outside the file would widen the
    /// blast radius unnecessarily for an E.2 concrete enhancement;
    /// (b) CoO already has a robust bleed pipeline
    /// (<c>BleedingEffect</c> + <c>OnHitClassEffects</c>) that Serrated
    /// naturally extends. Documented as a divergence in
    /// <c>Docs/ITEM-ENHANCEMENTS.md</c> — revisit in E.5+ if
    /// dismember-on-Serrated becomes the right call after playtest.</para>
    ///
    /// <para><b>Save/load (SL.6):</b> Tier + ChancePercent + SaveTarget
    /// + DamageDice are all public simple-typed fields and round-trip
    /// via reflection. <c>TierConfigure</c> re-derives ChancePercent
    /// from Tier on load too, so even if the stored ChancePercent
    /// drifts (e.g. patch tweaks BLEED_CHANCE_PER_TIER), the loaded
    /// instance reflects current tuning.</para>
    /// </summary>
    public class EnhancementSerrated : IMeleeEnhancement
    {
        /// <summary>Bleed-chance step per tier. Tier 1 → 10%, Tier 4 → 40%.</summary>
        public const int BLEED_CHANCE_PER_TIER = 10;

        /// <summary>Baseline save target for the Bleeding effect Serrated
        /// applies. Matches <see cref="OnHitClassEffects.CUTTING_BLEED_SAVE_TARGET"/>
        /// — Serrated is a probability bump, not a "deeper wound" upgrade.
        /// E.5+ can split this off if we want tier to control depth too.</summary>
        public const int DEFAULT_SAVE_TARGET = 15;

        /// <summary>Baseline damage dice for the Bleeding effect Serrated
        /// applies. Matches <see cref="OnHitClassEffects.CUTTING_BLEED_DAMAGE_DICE"/>.</summary>
        public const string DEFAULT_DAMAGE_DICE = "1d2";

        // --- Round-tripped state (public for SL.6 reflection) -----

        /// <summary>Computed by <see cref="TierConfigure"/>. Round-trips
        /// via reflection but re-derived on load via
        /// <see cref="ApplyTier"/> in the factory path — see class doc.</summary>
        public int ChancePercent;

        /// <summary>Save target passed to <see cref="BleedingEffect"/>.
        /// Tunable per instance — Tier doesn't currently change this but
        /// content (a special blueprint) could.</summary>
        public int SaveTarget;

        /// <summary>Damage dice passed to <see cref="BleedingEffect"/>.</summary>
        public string DamageDice;

        /// <summary>Diag category — emissions land in the
        /// <see cref="ItemEnhancing.DIAG_CATEGORY"/> channel ("enhancement")
        /// so a single <c>diag_query category=enhancement</c> reveals
        /// every enhancement-related event.</summary>
        public override string Name => nameof(EnhancementSerrated);

        public override string GetDisplayName() => "Serrated";

        // --- Lifecycle ---------------------------------------------

        public override void Configure()
        {
            // Tier-independent defaults. ChancePercent is derived from
            // Tier in TierConfigure, so we only initialize the stable
            // fields here.
            SaveTarget = DEFAULT_SAVE_TARGET;
            DamageDice = DEFAULT_DAMAGE_DICE;
        }

        public override void TierConfigure()
        {
            // Tier 1 → 10%, Tier 2 → 20%, ..., Tier 4 → 40%.
            ChancePercent = Tier * BLEED_CHANCE_PER_TIER;
        }

        public override bool Applicable(Entity item)
        {
            // base.Applicable already enforces non-null + MeleeWeaponPart.
            if (!base.Applicable(item)) return false;
            var weapon = item.GetPart<MeleeWeaponPart>();
            // Phase C Attributes is a space-delimited string. Cutting
            // is a top-level physical class — straightforward Contains.
            return weapon.Attributes != null && weapon.Attributes.Contains("Cutting");
        }

        // --- Content hook ------------------------------------------

        public override void OnAttackerHit(
            Entity defender, Entity attacker, Damage damage,
            int actualDamage, Zone zone, System.Random rng)
        {
            // Null + dead-target guards. We mirror OnHitClassEffects's
            // "no damage = no on-hit" contract: a fully-resisted hit
            // shouldn't still cause Serrated bleed.
            if (defender == null || rng == null) return;
            if (actualDamage <= 0) return;

            if (rng.Next(100) >= ChancePercent) return;

            // Independent roll — landed. Apply Bleed.
            defender.ApplyEffect(
                new BleedingEffect(SaveTarget, DamageDice, rng),
                attacker, zone);

            // Diag emission so debug + adversarial tests can pin
            // "Serrated triggered at least once across N seeds."
            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY))
            {
                Diag.Record(
                    category: ItemEnhancing.DIAG_CATEGORY,
                    kind: "Triggered",
                    actor: attacker,
                    target: defender,
                    payload: new
                    {
                        enhancement = nameof(EnhancementSerrated),
                        tier = Tier,
                        chance = ChancePercent,
                        effect = "Bleeding"
                    });
            }
        }
    }
}
