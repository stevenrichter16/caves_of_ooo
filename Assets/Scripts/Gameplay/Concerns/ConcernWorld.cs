using System;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C3a: what concerns place in the world when the player arrives. A Read approach's
    /// document stands in its place while the concern is being worked; it is placed on
    /// the player's arrival (never by regenerating a cached zone), once, with a stable id
    /// (<see cref="DocumentEntityId"/>), so re-entry, save and load keep the one copy.
    /// Called from the zone transition beside WorldTravellers.OnZoneEntered.
    /// A Confront approach's site is fixed when the concern is undertaken and its foes are
    /// placed on the player's first real arrival there, away from where they came in.
    /// Diag: concern/DocumentPlaced, DocumentNotPlaced, SiteResolved, SiteNotResolved, FoesPlaced.
    /// </summary>
    public static class ConcernWorld
    {
        public static string DocumentEntityId(string concernId, string approachId) => "concern:" + concernId + ":" + approachId;
        /// <summary>Set on every foe a Confront approach places: "concern|approach".</summary>
        public const string FoeProperty = "ConcernFoe";
        static string SiteFact(string concernId, string approachId) => ConcernRegistry.QuestPrefix + concernId + "." + approachId + ".site";
        static string PlacedFact(string concernId, string approachId) => ConcernRegistry.QuestPrefix + concernId + "." + approachId + ".placed";

        /// <summary>The zone a Confront approach's foes wait in, once the concern has been
        /// undertaken; null before. Stored as a fact (x*100+y+1), so it survives save/load.</summary>
        public static string SiteOf(string concernId, string approachId)
        {
            int v = NarrativeStatePart.Current?.GetFact(SiteFact(concernId, approachId)) ?? 0;
            return v <= 0 ? null : WorldMap.ToZoneID((v - 1) / 100, (v - 1) % 100, 0);
        }

        /// <summary>Called when a concern is undertaken: fix each Confront site. The authored
        /// offset from the place, or if a place or point of interest stands there, the first
        /// open zone in the rings around it. Never re-resolved.</summary>
        public static void ResolveSites(ConcernData c, Zone context)
        {
            var ns = NarrativeStatePart.Current;
            if (c == null || ns == null) return;
            WorldMapAuthoring.Place? home = null;
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == c.Place) home = p;
            if (!home.HasValue) return;
            var map = context != null ? WorldLocationContext.For(context)?.WorldMap : null;
            foreach (var a in c.Approaches)
            {
                if (!a.HasConfront || SiteOf(c.Id, a.Id) != null || !a.Confront.TryOffset(out int dx, out int dy)) continue;
                int tx = home.Value.X + dx, ty = home.Value.Y + dy;
                if (!FindOpenZone(tx, ty, map, out int sx, out int sy)) { Record("SiteNotResolved", c, a, tx + "," + ty); continue; }
                ns.SetFact(SiteFact(c.Id, a.Id), sx * 100 + sy + 1);
                Record("SiteResolved", c, a, WorldMap.ToZoneID(sx, sy, 0));
            }
        }

        static bool FindOpenZone(int tx, int ty, WorldMap map, out int x, out int y)
        {
            for (int r = 0; r <= 3; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    x = tx + dx; y = ty + dy;
                    if (!WorldMapAuthoring.InBounds(x, y) || IsPlace(x, y) || map?.GetPOI(x, y) != null) continue;
                    return true;
                }
            x = y = -1; return false;
        }

        static bool IsPlace(int x, int y)
        {
            foreach (var p in WorldMapAuthoring.Places) if (p.X == x && p.Y == y) return true;
            return false;
        }

        /// <summary>Returns how many things were placed. <paramref name="arriving"/> is true only
        /// for a real zone transition (the caller registers new creatures with the scheduler
        /// afterwards); foes are placed only then. Documents are placed either way.</summary>
        public static int OnZoneEntered(Entity player, Zone zone, bool arriving = true)
        {
            int foes = arriving ? PlaceFoes(player, zone) : 0;
            // C4: what the place's conditions keep there (stock, new residents).
            int kept = PlaceConsequences.OnZoneEntered(player, zone, arriving);
            return foes + kept + PlaceDocuments(player, zone);
        }

        static int PlaceFoes(Entity player, Zone zone)
        {
            if (player == null || zone == null || StoryletPart.Current == null || NarrativeStatePart.Current == null) return 0;
            var factory = ConversationActions.Factory;
            int placed = 0;
            foreach (var c in ConcernRegistry.All)
            {
                if (ConcernConversation.StateOf(c, null, player) != ConcernConversation.State.Working) continue;
                foreach (var a in c.Approaches)
                {
                    if (!a.HasConfront || SiteOf(c.Id, a.Id) != zone.ZoneID) continue;
                    if (NarrativeStatePart.Current.GetFact(PlacedFact(c.Id, a.Id)) > 0) continue;
                    var at = zone.GetEntityPosition(player);
                    int ax = Zone.Width - 1 - Math.Max(0, at.x), ay = Zone.Height - 1 - Math.Max(0, at.y);
                    int here = 0, missed = 0;
                    string key = ConcernItems.CargoKey(c.Id, a.Id);
                    foreach (var f in a.Confront.Foes)
                        for (int i = 0; i < f.Count; i++)
                        {
                            var foe = factory?.CreateEntity(f.Blueprint);
                            if (foe == null || !FindCellNear(zone, ax, ay, out int x, out int y)) { missed++; continue; }
                            foe.Properties[FoeProperty] = key;
                            if (!string.IsNullOrWhiteSpace(f.Name) && foe.GetPart<RenderPart>() is RenderPart render) render.DisplayName = f.Name;
                            foe.AddPart(new AddFactWhenSlain { Fact = ConcernRegistry.SlainFact(c.Id, a.Id), Amount = 1 });
                            if (zone.AddEntity(foe, x, y)) here++; else missed++;
                        }
                    // A foe that could not be placed counts as gone, so the approach can never lock.
                    if (missed > 0) NarrativeStatePart.Current.AddFact(ConcernRegistry.SlainFact(c.Id, a.Id), missed);
                    NarrativeStatePart.Current.SetFact(PlacedFact(c.Id, a.Id), 1);
                    placed += here;
                    Record("FoesPlaced", c, a, here + " placed, " + missed + " missed");
                }
            }
            return placed;
        }

        static int PlaceDocuments(Entity player, Zone zone)
        {
            if (player == null || zone == null || StoryletPart.Current == null) return 0;
            string place = PlaceAt(zone.ZoneID);
            if (place == null) return 0;
            var factory = ConversationActions.Factory;
            int placed = 0;
            foreach (var c in ConcernRegistry.All)
            {
                if (ConcernConversation.StateOf(c, null, player) != ConcernConversation.State.Working) continue;
                foreach (var a in c.Approaches)
                {
                    if (!a.HasRead || a.Read.Place != place) continue;
                    if ((NarrativeStatePart.Current?.GetFact(ReadableDocumentPart.ReadFact(a.Read.Document)) ?? 0) > 0) continue;
                    string id = DocumentEntityId(c.Id, a.Id);
                    if (Contains(zone, id)) continue;
                    var doc = ReadableDocumentCatalog.Get(a.Read.Document);
                    var entity = doc != null ? factory?.CreateEntity(doc.Blueprint) : null;
                    if (entity == null) { Record("DocumentNotPlaced", c, a, "missing_document_or_factory"); continue; }
                    if (!FindCell(zone, out int x, out int y)) { Record("DocumentNotPlaced", c, a, "no_free_cell"); continue; }
                    entity.ID = id;
                    if (!zone.AddEntity(entity, x, y)) { Record("DocumentNotPlaced", c, a, "add_refused"); continue; }
                    placed++;
                    Record("DocumentPlaced", c, a, x + "," + y);
                }
            }
            return placed;
        }

        static string PlaceAt(string zoneId)
        {
            foreach (var p in WorldMapAuthoring.Places)
                if (WorldMap.ToZoneID(p.X, p.Y, 0) == zoneId) return p.Name;
            return null;
        }

        static bool Contains(Zone zone, string id)
        {
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == id) return true;
            return false;
        }

        // Rings outward from the middle of the zone: the first open, unreserved ground.
        static bool FindCell(Zone zone, out int x, out int y) => FindCellNear(zone, Zone.Width / 2, Zone.Height / 2, out x, out y);

        static bool FindCellNear(Zone zone, int cx, int cy, out int x, out int y)
        {
            for (int r = 0; r <= Math.Max(Zone.Width, Zone.Height) / 2; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    x = cx + dx; y = cy + dy;
                    if (Open(zone, x, y)) return true;
                }
            x = y = -1; return false;
        }

        static bool Open(Zone zone, int x, int y)
        {
            if (x < 2 || y < 2 || x >= Zone.Width - 2 || y >= Zone.Height - 2 || zone.GenReservedCells.Contains((x, y))) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.BlocksMovement() || zone.TileState.Has(x, y)) return false;
            foreach (var item in cell.Occupants)
                if (!item.HasTag("Terrain") || item.HasPart<StairsDownPart>() || item.HasPart<StairsUpPart>() || item.HasPart<LiquidPoolPart>()) return false;
            return true;
        }

        static void Record(string kind, ConcernData c, ConcernApproachData a, string detail)
        {
            if (!Diag.IsChannelEnabled("concern")) return;
            Diag.Record(category: "concern", kind: kind, payload: new { concern = c.Id, approach = a.Id, document = a.HasRead ? a.Read.Document : null, detail });
        }
    }
}
