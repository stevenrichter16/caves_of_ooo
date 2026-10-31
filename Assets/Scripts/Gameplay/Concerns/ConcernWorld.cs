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
    /// Diag: concern/DocumentPlaced, concern/DocumentNotPlaced (reason).
    /// </summary>
    public static class ConcernWorld
    {
        public static string DocumentEntityId(string concernId, string approachId) => "concern:" + concernId + ":" + approachId;

        /// <summary>Returns how many things were placed.</summary>
        public static int OnZoneEntered(Entity player, Zone zone)
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
        static bool FindCell(Zone zone, out int x, out int y)
        {
            int cx = Zone.Width / 2, cy = Zone.Height / 2;
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
            Diag.Record(category: "concern", kind: kind, payload: new { concern = c.Id, approach = a.Id, document = a.Read.Document, detail });
        }
    }
}
