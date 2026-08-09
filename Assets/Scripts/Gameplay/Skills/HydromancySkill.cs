using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Hydromancy tree-root — the SETUP tree. Water deals almost no
    /// damage of its own; what it does is make a target vulnerable to
    /// everything else.
    ///
    /// <para>SPELLCRAFT SM5 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.3), the
    /// fifth elemental tree.</para>
    ///
    /// <para><b>Why water is not folded into Galvanism.</b> Galvanism's
    /// whole identity is a CONDITIONAL bonus against Wet or Electrified
    /// targets. If a lightning mage could soak targets for free, that
    /// condition would always be met and the tree's identity would
    /// collapse into a flat damage bonus. Keeping water in its own tree
    /// means the soak-then-shock combo costs a real investment — either
    /// two trees, or two characters.</para>
    ///
    /// <para><b>The passive: your water sticks.</b> Rather than another
    /// element-damage modifier (the Pyromancy / Cryomancy / Galvanism
    /// pattern), Hydromancy deepens the moisture its owner applies by
    /// <see cref="MOISTURE_BONUS"/>. That is the mechanically honest
    /// bonus for a tree whose job is priming: a Hydromancer's soaking
    /// stays above the thresholds that matter — 0.2 for
    /// <see cref="ElectrifiedEffect"/>'s charge doubling and 0.35 for
    /// fire suppression — for longer, because moisture only evaporates a
    /// little each turn.</para>
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-original.</b> No Qud
    /// analogue; Qud has no water-priming skill tree.</para>
    /// </summary>
    public class HydromancySkill : BaseSkillPart
    {
        public override string Name => nameof(HydromancySkill);

        /// <summary>Extra moisture added to every soaking its owner
        /// applies. Deliberately modest: it buys duration above the
        /// interaction thresholds, not a different effect.</summary>
        public const float MOISTURE_BONUS = 0.25f;

        /// <summary>
        /// Moisture a soaking should carry when applied by
        /// <paramref name="actor"/>. Any water power calls this rather
        /// than using its base value directly, so the tree bonus can
        /// never be forgotten by a future power.
        /// </summary>
        public static float ApplyMoistureBonus(Entity actor, float baseMoisture)
        {
            if (actor == null) return baseMoisture;
            var skills = actor.GetPart<SkillsPart>();
            if (skills == null || !skills.HasSkill(nameof(HydromancySkill)))
                return baseMoisture;

            float boosted = baseMoisture + MOISTURE_BONUS;
            // WetEffect caps at 1.0 anyway; clamping here keeps the
            // returned value honest for callers that log or test it.
            return boosted > 1.0f ? 1.0f : boosted;
        }
    }
}
