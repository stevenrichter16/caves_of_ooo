using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What a set-down fixture says about itself in the quick and full Examine, and the derived list of what a player keeps standing (Docs/HOME-BASE-DESIGN.md sections 5.3 and 12, PB3).
    /// Everything here is read-only and derived from the live entities: nothing is saved, nothing is a ledger, and the list is rebuilt only when it is opened.
    /// </summary>
    public static class HeldFixtureReader
    {
        /// <summary>
        /// One plain sentence for a carried kit, a standing fixture the player holds, a fixture no longer held, or a kind that no longer resolves. The wording states the real
        /// consequences (it stays where it is, its contents stay in it, it can be taken up or handed over) and promises nothing about safety. Placeholder text pending voice review.
        /// </summary>
        public static string Describe(Entity fixture, PackableFixturePart part)
        {
            if (fixture == null || part == null) return null;
            var def = PackableFixtureRegistry.Get(part.Kind);
            var installable = fixture.GetPart<InstallablePart>();
            bool standing = fixture.SpatialZone != null;
            if (def == null) return "You cannot tell how this was meant to be moved. It can be left where it is.";
            if (!standing) return "Can be set down on open ground. Once set down it stays where you put it, with whatever is in it, until you take it up again.";
            if (installable?.State == InstallablePart.Installed && fixture.GetPart<InstallationMarkerPart>()?.Origin == HeldFixtureActions.PlayerOrigin)
                return "You set this down. It stays here, with whatever is in it. You can take it up again, or hand it over and leave it for good.";
            return null; // standing but no longer held: an ordinary object of the zone, so no extra line
        }

        /// <summary>One cluster of things the player keeps standing in one zone: a position, a count and the oldest set-down tick.</summary>
        public sealed class HeldPlace
        {
            public string ZoneId;
            public int X, Y, Count, FirstTick;
            public readonly List<Entity> Fixtures = new List<Entity>();
        }

        /// <summary>Chebyshev radius that joins two fixtures into one place (the same radius as the rest-refusal radius).</summary>
        public const int ClusterRadius = 8;

        /// <summary>
        /// Every place the player keeps something standing, derived by scanning the cached zones for markers whose origin is the player's (or a relocated rig). Clusters are
        /// single-link within <see cref="ClusterRadius"/> and ordered by zone, then position. It never generates or loads a zone and it is never authoritative.
        /// </summary>
        public static List<HeldPlace> Enumerate(OverworldZoneManager manager)
        {
            var places = new List<HeldPlace>();
            if (manager == null) return places;
            var zoneIds = new List<string>(manager.CachedZones.Keys); zoneIds.Sort(StringComparer.Ordinal);
            foreach (string zoneId in zoneIds)
            {
                var zone = manager.CachedZones[zoneId]; var held = new List<(Entity e, int x, int y)>();
                var owners = zone.GetReadOnlyEntitiesWithPart<InstallationMarkerPart>();
                for (int i = 0; i < owners.Count; i++)
                {
                    var marker = owners[i].GetPart<InstallationMarkerPart>();
                    if (marker == null || (marker.Origin != "Player" && marker.Origin != "Relocated")) continue;
                    var cell = zone.GetEntityCell(owners[i]); if (cell == null) continue;
                    held.Add((owners[i], cell.X, cell.Y));
                }
                if (held.Count == 0) continue;
                held.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y != b.y ? a.y.CompareTo(b.y) : string.CompareOrdinal(a.e.ID, b.e.ID));
                var parent = new int[held.Count]; for (int i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
                for (int i = 0; i < held.Count; i++)
                    for (int j = i + 1; j < held.Count; j++)
                        if (Math.Max(Math.Abs(held[i].x - held[j].x), Math.Abs(held[i].y - held[j].y)) <= ClusterRadius) parent[Find(j)] = Find(i);
                var byRoot = new Dictionary<int, HeldPlace>(); var order = new List<int>();
                for (int i = 0; i < held.Count; i++)
                {
                    int root = Find(i);
                    if (!byRoot.TryGetValue(root, out var place)) { place = new HeldPlace { ZoneId = zoneId, X = held[i].x, Y = held[i].y, FirstTick = int.MaxValue }; byRoot[root] = place; order.Add(root); }
                    place.Count++; place.Fixtures.Add(held[i].e);
                    int tick = held[i].e.GetPart<PackableFixturePart>()?.SetTick ?? -1;
                    if (tick >= 0 && tick < place.FirstTick) place.FirstTick = tick;
                }
                foreach (int root in order) { var place = byRoot[root]; if (place.FirstTick == int.MaxValue) place.FirstTick = -1; places.Add(place); }
            }
            return places;
        }

        /// <summary>A one-line summary of the derived list, for a log line or a journal-style surface. Empty when the player keeps nothing standing.</summary>
        public static string Summarize(IReadOnlyList<HeldPlace> places)
        {
            if (places == null || places.Count == 0) return "";
            int things = 0; foreach (var p in places) things += p.Count;
            return "You keep " + things + (things == 1 ? " thing" : " things") + " standing in " + places.Count + (places.Count == 1 ? " place." : " places.");
        }
    }
}
