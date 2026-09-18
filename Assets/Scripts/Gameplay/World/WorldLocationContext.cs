using System.Runtime.CompilerServices;

namespace CavesOfOoo.Core
{
    /// <summary>Derived, nonserialized world authority for a live zone. Attached
    /// on generation/access/restore; queries never generate or mutate a zone.</summary>
    public static class WorldLocationContext
    {
        private sealed class Source { public OverworldZoneManager Manager; }
        private static readonly ConditionalWeakTable<Zone, Source> sources = new ConditionalWeakTable<Zone, Source>();
        internal static void Attach(Zone zone, OverworldZoneManager manager)
        { if (zone != null) sources.GetValue(zone, _ => new Source()).Manager = manager; }
        public static OverworldZoneManager For(Zone zone)
            => zone != null && sources.TryGetValue(zone, out var source) ? source.Manager : null;
    }
}
