using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
    /// <summary>Complete read-only preflight for the original missing-scenery kit.
    /// All coordinates are in one native cell; assets add no collision or light.</summary>
    [Serializable]
    public sealed class SpreadScenerySource
    {
        public int schemaVersion;
        public string[] palette;
        public Model[] models;
        [Serializable] public sealed class Model { public string id; public Box[] boxes; }
        [Serializable] public sealed class Box { public float[] center, size; public int color; }
        public static IReadOnlyList<string> Blueprints { get; } = Array.AsReadOnly(new[] {
            "BerryBush", "Signpost", "HollowStump", "Beehive", "RiverShrine", "FlowerField",
            "StoneFloor", "StoneWall", "Chair", "Bed", "Well", "Oven", "WatchLantern",
            "CampfireGroundMarker", "WellGroundMarker", "OvenGroundMarker", "LanternGroundMarker",
            "Shrine", "AlchemyShelf", "AlchemyStill", "TinkersForge", "OldStump",
            "PressurePlate", "BearTrap", "FireTrap", "SpikeTrap", "WeaponRack"
        });
        public static IReadOnlyList<string> ApprovedPalette { get; } = Array.AsReadOnly(new[] {
            "#082C28", "#103E36", "#1A4B40", "#26594A", "#A0A77C", "#CBC697",
            "#647353", "#235D25", "#40872C", "#65AE3D", "#435A53", "#62786C",
            "#819489", "#16883B", "#45CB4B", "#A0E772", "#207838", "#D2D3B4",
            "#B77B43", "#403D28", "#756C40", "#A39456", "#C4B877", "#243E39"
        });
        public static IReadOnlyList<string> ModelIds { get; } = Array.AsReadOnly(CreateIds());
        private static readonly HashSet<string> KnownIds = new HashSet<string>(ModelIds, StringComparer.Ordinal);
        private static string[] CreateIds()
        {
            var ids = new List<string>(56);
            foreach (string blueprint in Blueprints)
                for (int variant = 0; variant < 2; variant++)
                    ids.Add("spread-scenery-" + blueprint.ToLowerInvariant() + "-" + variant);
            for (int variant = 0; variant < 2; variant++) ids.Add("spread-scenery-watchlantern-unlit-" + variant);
            return ids.ToArray();
        }
        public static bool IsModelId(string id) => id != null && KnownIds.Contains(id);
        public static string KindForModel(string id)
        {
            if (!IsModelId(id)) throw new ArgumentException("Unknown scenery model.", nameof(id));
            return id.StartsWith("spread-scenery-stonefloor-", StringComparison.Ordinal) ? "ground" : "entity";
        }
        public void Validate()
        {
            if (schemaVersion != 1 || palette == null || palette.Length != ApprovedPalette.Count
                || models == null || models.Length != ModelIds.Count)
                throw new InvalidOperationException("Incomplete scenery source pack.");
            for (int i = 0; i < palette.Length; i++)
                if (palette[i] != ApprovedPalette[i]) throw new InvalidOperationException("Unreviewed scenery palette.");
            var remaining = new HashSet<string>(ModelIds, StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null || model.id == null || !remaining.Remove(model.id)
                    || model.boxes == null || model.boxes.Length == 0 || model.boxes.Length > 512)
                    throw new InvalidOperationException("Invalid scenery identity or box count.");
                foreach (var box in model.boxes)
                {
                    if (box == null || box.center == null || box.size == null || box.center.Length != 3
                        || box.size.Length != 3 || box.color < 0 || box.color >= palette.Length)
                        throw new InvalidOperationException("Invalid scenery geometry fields.");
                    for (int axis = 0; axis < 3; axis++)
                        if (!Finite(box.center[axis]) || !Finite(box.size[axis]) || box.size[axis] < .001f)
                            throw new InvalidOperationException("Invalid scenery coordinate.");
                    if (Math.Abs(box.center[0]) + box.size[0] * .5f > .501f
                        || Math.Abs(box.center[2]) + box.size[2] * .5f > .501f
                        || box.center[1] - box.size[1] * .5f < -.0251f
                        || box.center[1] + box.size[1] * .5f > 1.6001f)
                        throw new InvalidOperationException("Scenery geometry leaves its native cell envelope.");
                }
            }
            if (remaining.Count != 0) throw new InvalidOperationException("Missing scenery model.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
