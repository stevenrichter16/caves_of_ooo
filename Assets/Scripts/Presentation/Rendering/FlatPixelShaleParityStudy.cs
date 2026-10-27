using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Explicitly enabled, temporary presentation study. Never alters
    /// normal-save rendering or grants authority from a zone name alone.</summary>
    public static class FlatPixelShaleParityStudy
    {
        public const string TargetZoneID = "Overworld.16.13.6";
        public const string ResourcePath = "Sprites/Live16x24/shale-parity";
        public const string RubbleResourcePath = "Sprites/Live16x24/shale-parity-rubble";
        public const int FloorVariants = 8, WallVariants = 4;
        public const int FrameCount = FloorVariants + 16 * WallVariants;
        public static bool Enabled;

        public static bool IsActive(Zone zone, bool flatMode)
            => Enabled && flatMode && !string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride)
                && zone?.ZoneID == TargetZoneID && FlatPixelShaleCavesCatalog.IsSupportedZone(zone);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() => Enabled = false;
    }
}
