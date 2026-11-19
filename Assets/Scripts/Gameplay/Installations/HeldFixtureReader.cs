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

        /// <summary>
        /// What the player keeps standing in this zone, as one plain line for the moment they arrive ("You keep here: camp box, camp hearth (x2)."), or null when nothing is.
        /// Derived from the live zone each time; nothing is stored.
        /// </summary>
        public static string ArrivalNote(Zone zone)
        {
            if (zone == null) return null;
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal); var owners = zone.GetReadOnlyEntitiesWithPart<InstallationMarkerPart>();
            for (int i = 0; i < owners.Count; i++)
            {
                var marker = owners[i].GetPart<InstallationMarkerPart>();
                if (marker == null || (marker.Origin != "Player" && marker.Origin != "Relocated")) continue;
                string name = owners[i].GetDisplayName(); counts[name] = counts.TryGetValue(name, out int n) ? n + 1 : 1;
            }
            if (counts.Count == 0) return null;
            var parts = new List<string>(); foreach (var pair in counts) parts.Add(pair.Value == 1 ? pair.Key : pair.Key + " (x" + pair.Value + ")");
            return "You keep here: " + string.Join(", ", parts) + ".";
        }

        /// <summary>Called once when the player arrives in a zone (travel, not load): says what they keep standing there.</summary>
        public static void OnZoneEntered(Zone zone)
        {
            string note = ArrivalNote(zone);
            if (note != null) MessageLog.Add(note);
        }

        /// <summary>
        /// Where the player keeps things, as steps from the world-map cell they stand on: one line per zone, nearest first, at most <paramref name="maxLines"/> lines and then a
        /// count of the rest. It answers "where did I leave my camp" at the moment the player opens the map, when it is useful. Derived from the cached zones on each call; nothing is
        /// stored and nothing is shown that the player did not set down themselves. Placeholder wording pending voice review.
        /// </summary>
        public static List<string> MapNotes(OverworldZoneManager manager, int fromX, int fromY, int maxLines = 4)
        {
            var lines = new List<string>(); if (manager == null || maxLines < 1) return lines;
            var byZone = new Dictionary<string, (int dx, int dy, int z, SortedDictionary<string, int> names)>(StringComparer.Ordinal);
            foreach (var place in Enumerate(manager))
            {
                var (x, y, z) = WorldMap.FromZoneID(place.ZoneId); if (x < 0 || y < 0 || z < 0) continue;
                if (!byZone.TryGetValue(place.ZoneId, out var entry)) entry = (x - fromX, y - fromY, z, new SortedDictionary<string, int>(StringComparer.Ordinal));
                foreach (var fixture in place.Fixtures) { string name = fixture.GetDisplayName(); entry.names[name] = entry.names.TryGetValue(name, out int n) ? n + 1 : 1; }
                byZone[place.ZoneId] = entry;
            }
            if (byZone.Count == 0) return lines;
            var order = new List<string>(byZone.Keys);
            order.Sort((a, b) =>
            {
                int da = Math.Max(Math.Abs(byZone[a].dx), Math.Abs(byZone[a].dy)), db = Math.Max(Math.Abs(byZone[b].dx), Math.Abs(byZone[b].dy));
                return da != db ? da.CompareTo(db) : string.CompareOrdinal(a, b);
            });
            for (int i = 0; i < order.Count && i < maxLines; i++)
            {
                var entry = byZone[order[i]]; var things = new List<string>();
                foreach (var pair in entry.names) things.Add(pair.Value == 1 ? pair.Key : pair.Key + " (x" + pair.Value + ")");
                lines.Add("You keep " + string.Join(", ", things) + ": " + Offset(entry.dx, entry.dy) + (entry.z != 0 ? ", below ground." : "."));
            }
            int rest = order.Count - maxLines;
            if (rest > 0) lines.Add("You keep things in " + rest + " more " + (rest == 1 ? "place." : "places."));
            return lines;
        }

        static string Offset(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return "this place";
            var parts = new List<string>();
            if (dx != 0) parts.Add(Math.Abs(dx) + (dx > 0 ? " east" : " west"));
            if (dy != 0) parts.Add(Math.Abs(dy) + (dy > 0 ? " south" : " north"));
            return string.Join(", ", parts) + " of here";
        }

        /// <summary>Called when the player opens the world map from the surface cell (wx, wy): says where they keep things, as steps from where they now stand.</summary>
        public static void OnMapOpened(OverworldZoneManager manager, int wx, int wy)
        { foreach (var line in MapNotes(manager, wx, wy)) MessageLog.Add(line); }

        /// <summary>A one-line summary of the derived list, for a log line or a journal-style surface. Empty when the player keeps nothing standing.</summary>
        public static string Summarize(IReadOnlyList<HeldPlace> places)
        {
            if (places == null || places.Count == 0) return "";
            int things = 0; foreach (var p in places) things += p.Count;
            return "You keep " + things + (things == 1 ? " thing" : " things") + " standing in " + places.Count + (places.Count == 1 ? " place." : " places.");
        }
    }
}
