using System.Collections.Generic;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P4 (Docs/PLACE-WORK-AND-PROGRESSION.md, P4 divergences): zones now play at their
    /// own tier, so tier 4-5 surface chunks gain the upper threats and floors under deep
    /// places roll the deeper underground tables. The previous (3D) presentation has no
    /// models for these owners and draws them as glyphs; the flat 16x24 target is where
    /// their art is owed (P5). Coverage pins for the 3D path excuse exactly this list and
    /// nothing else, so any other unmodeled owner still fails.
    /// </summary>
    public static class PreviousPresentationTierGap
    {
        public static readonly HashSet<string> Blueprints = new HashSet<string>
        {
            // upper-tier surface threats (PopulationTable.WithUpperTierThreats / WithStumpThreats)
            "CanopyStrangler", "AncientGuardian", "PaleStalker", "ObsidianBrute",
            // deeper underground tables under tier 3-5 places, and what they carry or raise
            "IceWight", "SkeletalSentry", "StoneGolem", "CharredHusk", "PaleSaltVein",
            "LongSword", "LeatherArmor",
        };

        public static bool Excuses(string blueprint) => blueprint != null && Blueprints.Contains(blueprint);
    }
}
