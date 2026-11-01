using System.Collections.Generic;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P4 (Docs/PLACE-WORK-AND-PROGRESSION.md, P4 divergences), P5 and waysites: zones now play at their
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
            // P5 (Docs/UPPER-TIER-BESTIARY.md B8): the new upper kinds and elders. Their art is the flat
            // 16x24 bodies of B9 (FlatPixelUpperBestiaryTests); the previous presentation has no models.
            "Riverwarden", "OldRiverwarden", "SimaSteward", "BloomedWarden", "BloomedHound", "PaleMatriarch", "OldSkySari",
            // Waysites (Docs/CHUNK-ACTIVITY.md): the new props and wayside notes. Their flat 16x24 art is the
            // prop and item atlases, proven in place by WaysitePresentationTests; the previous presentation
            // has no models. Owners the waysites reuse (campfire, sack, signpost, beds) stay pinned.
            "Bedroll", "DroversTrough", "Cistern", "HitchingPost", "OverturnedCart", "TokenDish", "BoatHookPost",
            "WaysiteNoteDrover1", "WaysiteNoteDrover2", "WaysiteNoteDrover3", "WaysiteNoteCarriage1", "WaysiteNoteCarriage2",
            "WaysiteNotePeatTally1", "WaysiteNotePeatTally2", "WaysiteNoteFerrySlate1", "WaysiteNoteFerrySlate2",
            "WaysiteNoteCistern1", "WaysiteNoteCistern2", "WaysiteNoteWatcher1", "WaysiteNoteWatcher2",
            "WaysiteNoteGatherer1", "WaysiteNoteGatherer2",
        };

        public static bool Excuses(string blueprint) => blueprint != null && Blueprints.Contains(blueprint);
    }
}
