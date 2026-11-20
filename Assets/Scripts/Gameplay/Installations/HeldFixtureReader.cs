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
            /// <summary>The name the player gave this camp (empty when none).</summary>
            public string Name => CampNameOf(Fixtures);
        }

        /// <summary>The names "name this camp" cycles through, in order; after the last the name is cleared.</summary>
        public static readonly string[] CampNames = { "Home", "Peat edge", "Dig site", "Lookout", "Cache", "Workshop", "Field camp", "Waystop" };

        /// <summary>The name after <paramref name="current"/> in <see cref="CampNames"/>: the first for no name or an unknown one, and empty (cleared) after the last.</summary>
        public static string NextCampName(string current)
        {
            int at = string.IsNullOrEmpty(current) ? -1 : Array.IndexOf(CampNames, current);
            if (string.IsNullOrEmpty(current) || at < 0) return CampNames[0];
            return at + 1 < CampNames.Length ? CampNames[at + 1] : "";
        }

        /// <summary>A camp's name: the first non-empty name among its fixtures by ID (every fixture of a camp carries the same one, but a piece set down later may not yet).</summary>
        public static string CampNameOf(IEnumerable<Entity> fixtures)
        {
            string best = ""; string bestId = null;
            if (fixtures != null) foreach (var e in fixtures)
            {
                string name = e?.GetPart<PackableFixturePart>()?.CampName; if (string.IsNullOrEmpty(name)) continue;
                if (bestId == null || string.CompareOrdinal(e.ID, bestId) < 0) { best = name; bestId = e.ID; }
            }
            return best;
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
            foreach (string zoneId in zoneIds) places.AddRange(ClustersIn(zoneId, manager.CachedZones[zoneId]));
            return places;
        }

        /// <summary>The camps in one zone: every fixture the player holds, joined single-link within <see cref="ClusterRadius"/>, ordered by position.</summary>
        public static List<HeldPlace> ClustersIn(string zoneId, Zone zone)
        {
            var places = new List<HeldPlace>(); if (zone == null) return places;
            var held = new List<(Entity e, int x, int y)>();
            var owners = zone.GetReadOnlyEntitiesWithPart<InstallationMarkerPart>();
            for (int i = 0; i < owners.Count; i++)
            {
                var marker = owners[i].GetPart<InstallationMarkerPart>();
                if (marker == null || (marker.Origin != "Player" && marker.Origin != "Relocated")) continue;
                var cell = zone.GetEntityCell(owners[i]); if (cell == null) continue;
                held.Add((owners[i], cell.X, cell.Y));
            }
            if (held.Count == 0) return places;
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
            return places;
        }

        /// <summary>The held fixtures that make up the same camp as <paramref name="fixture"/> in this zone (including itself).</summary>
        public static List<Entity> ClusterOf(Zone zone, Entity fixture)
        {
            foreach (var place in ClustersIn(zone?.ZoneID, zone)) if (place.Fixtures.Contains(fixture)) return place.Fixtures;
            return new List<Entity> { fixture };
        }

        /// <summary>
        /// What the fixtures that hold something say about it, as the player left it: a hearth's flame and fuel ("hearth lit, 12 fuel"), a box's contents ("box holds 3"). Empty when
        /// nothing there has anything to report (an empty box, a hearth with no fuel and no flame says "hearth out" only if it ever had a fire, so a bare kit stays quiet). Derived at
        /// the moment of reading; a zone you are not in is not simulated, so this is the state you left it in.
        /// </summary>
        public static string Status(IEnumerable<Entity> fixtures)
        {
            var parts = new List<string>(); if (fixtures == null) return "";
            var list = new List<Entity>(fixtures); list.Sort((a, b) => string.CompareOrdinal(a.ID, b.ID));
            foreach (var e in list)
            {
                string kind = e.GetPart<PackableFixturePart>()?.Kind;
                if (kind == "camp-hearth" && e.GetPart<FuelPart>() is FuelPart fuel && e.GetPart<ThermalPart>() is ThermalPart heat)
                {
                    bool lit = heat.Temperature >= CookingService.MinimumFiniteCookingTemperature && fuel.FuelMass > 0;
                    int amount = (int)Math.Floor(fuel.FuelMass);
                    if (lit) parts.Add("hearth lit, " + amount + " fuel");
                    else if (amount > 0) parts.Add("hearth cold, " + amount + " fuel");
                }
                else if (kind == "camp-box" && e.GetPart<ContainerPart>() is ContainerPart box)
                {
                    int things = 0; foreach (var item in box.Contents) things += item.GetPart<StackerPart>()?.StackCount ?? 1;
                    if (things > 0) parts.Add("box holds " + things);
                }
            }
            return parts.Count == 0 ? "" : " As you left it: " + string.Join(", ", parts) + ".";
        }

        /// <summary>
        /// What the player keeps standing in this zone, as one plain line for the moment they arrive ("You keep here: camp box, camp hearth (x2)."), or null when nothing is.
        /// Derived from the live zone each time; nothing is stored.
        /// </summary>
        public static string ArrivalNote(Zone zone)
        {
            if (zone == null) return null;
            var unnamed = new SortedDictionary<string, int>(StringComparer.Ordinal); var named = new List<string>(); var all = new List<Entity>();
            foreach (var place in ClustersIn(zone.ZoneID, zone))
            {
                all.AddRange(place.Fixtures); string label = place.Name;
                if (label == "") { foreach (var e in place.Fixtures) { string name = e.GetDisplayName(); unnamed[name] = unnamed.TryGetValue(name, out int n) ? n + 1 : 1; } continue; }
                named.Add(label + " (" + Things(place.Fixtures) + ")");
            }
            if (unnamed.Count == 0 && named.Count == 0) return null;
            var parts = new List<string>(named); foreach (var pair in unnamed) parts.Add(pair.Value == 1 ? pair.Key : pair.Key + " (x" + pair.Value + ")");
            return "You keep here: " + string.Join(", ", parts) + "." + Status(all);
        }

        static string Things(IEnumerable<Entity> fixtures)
        {
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var e in fixtures) { string name = e.GetDisplayName(); counts[name] = counts.TryGetValue(name, out int n) ? n + 1 : 1; }
            var parts = new List<string>(); foreach (var pair in counts) parts.Add(pair.Value == 1 ? pair.Key : pair.Key + " (x" + pair.Value + ")");
            return string.Join(", ", parts);
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
            var namedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal); var fixturesBy = new Dictionary<string, List<Entity>>(StringComparer.Ordinal);
            foreach (var place in Enumerate(manager))
            {
                var (x, y, z) = WorldMap.FromZoneID(place.ZoneId); if (x < 0 || y < 0 || z < 0) continue;
                if (!byZone.TryGetValue(place.ZoneId, out var entry)) entry = (x - fromX, y - fromY, z, new SortedDictionary<string, int>(StringComparer.Ordinal));
                if (!fixturesBy.TryGetValue(place.ZoneId, out var all)) fixturesBy[place.ZoneId] = all = new List<Entity>();
                all.AddRange(place.Fixtures); string label = place.Name;
                if (label != "")
                {
                    if (!namedBy.TryGetValue(place.ZoneId, out var list)) namedBy[place.ZoneId] = list = new List<string>();
                    list.Add(label + " (" + Things(place.Fixtures) + ")");
                }
                else foreach (var fixture in place.Fixtures) { string name = fixture.GetDisplayName(); entry.names[name] = entry.names.TryGetValue(name, out int n) ? n + 1 : 1; }
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
                if (namedBy.TryGetValue(order[i], out var camps)) things.AddRange(camps);
                foreach (var pair in entry.names) things.Add(pair.Value == 1 ? pair.Key : pair.Key + " (x" + pair.Value + ")");
                lines.Add("You keep " + string.Join(", ", things) + ": " + Offset(entry.dx, entry.dy) + (entry.z != 0 ? ", below ground." : ".") + Status(fixturesBy[order[i]]));
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
