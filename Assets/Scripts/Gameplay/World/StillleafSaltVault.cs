using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Stillleaf Archive, SA.3 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): the
    /// Pale Curation Indexer at the Salt-Vault and the file that holds the
    /// keeper's key.
    ///
    /// <para>Installed once on fresh generation of the Salt-Vault surface: the
    /// Indexer on bare interior floor, the salt file (a locked container with
    /// the keeper's key inside) on an adjacent interior cell that keeps the
    /// building connected and leaves a second free neighbour for the player to
        /// stand at. The index answers only to the keeper's last words;
    /// with them the file is read and the journal advances. Curation's price
    /// is a request, not a toll: agree or bargain and the file is unlocked for
    /// the player to take the key through the ordinary container path; refuse
    /// and it stays locked, without penalty, and the offer stands on return.
    /// Terms are one-time once the file is released.</para>
    /// </summary>
    public static class StillleafSaltVault
    {
        public const string ZoneId = "Overworld.15.15.0";
        public const string IndexerBlueprint = "StillleafIndexer", IndexerId = "stillleaf-archive:indexer";
        public const string ConversationId = "StillleafIndexer_1";
        public const string CabinetBlueprint = "StillleafFileCabinet", CabinetId = "stillleaf-archive:file";
        public const string KeyId = "stillleaf-archive:key";
        /// <summary>Player int property: 0 none, 1 agreed (file it unread), 2 bargained (bring it here, custody not promised), 3 refused.</summary>
        public const string Terms = "StillleafCurationTerms";
        public const int TermsAgreed = 1, TermsBargained = 2, TermsRefused = 3;
        /// <summary>Player int property: 1 once the keeper's file has been read.</summary>
        public const string FileFound = "StillleafFileFound";
        private const string Installed = "StillleafIndexerInstalled";

        public static bool Owns(string command) => command == "retrieve" || command == "agree" || command == "bargain" || command == "refuse";

        public static Entity FindCabinet(Zone zone)
        {
            if (zone == null) return null;
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == CabinetId) return e;
            return null;
        }

        /// <summary>Called only by fresh generation of the Salt-Vault surface.</summary>
        public static bool TryInstall(Zone zone, EntityFactory factory)
        {
            if (zone?.ZoneID != ZoneId || factory == null) return Refuse(zone, "not_salt_vault");
            var anchor = Anchor(zone); if (anchor == null) return Refuse(zone, "no_anchor");
            if (anchor.GetIntProperty(Installed) == 1) return Refuse(zone, "already_installed");
            foreach (var bp in new[] { IndexerBlueprint, CabinetBlueprint, StillleafArchive.KeyBlueprint })
                if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone, "missing_blueprint:" + bp);

            // Candidate pairs: a bare interior cell for the file with as much
            // wall around it as possible, and a bare interior neighbour for
            // the Indexer. The file is solid, so a pair is accepted only if
            // every other passable cell stays reachable from the zone edge.
            var pairs = new List<(int walls, int x, int y, int ix, int iy)>();
            for (int y = 1; y < Zone.Height - 1; y++) for (int x = 1; x < Zone.Width - 1; x++)
            {
                if (!Bare(zone, x, y)) continue;
                int walls = 0; foreach (var (dx, dy) in Four) { var n = zone.GetCell(x + dx, y + dy); if (n != null && !n.IsPassable()) walls++; }
                var open = new List<(int x, int y)>();
                foreach (var (dx, dy) in Four) if (Bare(zone, x + dx, y + dy)) open.Add((x + dx, y + dy));
                // The Indexer takes one neighbour; a player must be able to stand at
                // another to open the file (found by the SA.6 native run).
                if (open.Count < 2) continue;
                foreach (var seat in open) pairs.Add((walls, x, y, seat.x, seat.y));
            }
            if (pairs.Count == 0) return Refuse(zone, "no_free_interior");
            pairs.Sort((a, b) => a.walls != b.walls ? b.walls.CompareTo(a.walls) : a.y != b.y ? a.y.CompareTo(b.y) : a.x != b.x ? a.x.CompareTo(b.x) : a.iy != b.iy ? a.iy.CompareTo(b.iy) : a.ix.CompareTo(b.ix));
            int before = Reachable(zone, -1, -1);
            foreach (var p in pairs)
            {
                if (Reachable(zone, p.x, p.y) != before - 1) continue;
                var cabinet = factory.CreateEntity(CabinetBlueprint);
                var key = factory.CreateEntity(StillleafArchive.KeyBlueprint);
                var indexer = factory.CreateEntity(IndexerBlueprint);
                var container = cabinet?.GetPart<ContainerPart>();
                if (container == null || !container.Locked || cabinet.GetPart<PhysicsPart>()?.Solid != true || cabinet.GetPart<ExaminablePart>() == null
                    || key?.GetPart<KeyPart>()?.KeyId != SealedLibraryBuilder.KeyID
                    || indexer?.GetPart<ConversationPart>() == null || indexer.GetPart<BrainPart>() == null
                    || indexer.GetPart<StillleafResidentPart>() == null || indexer.GetStatValue("Hitpoints") <= 0)
                    return Refuse(zone, "content_incomplete");
                cabinet.ID = CabinetId; key.ID = KeyId; indexer.ID = IndexerId;
                key.AddPart(new CompleteObjectiveOnTaken { Quest = StillleafArchiveContent.QuestId, Objective = "key" });
                if (!container.AddItem(key)) return Refuse(zone, "file_refused_key");
                if (!zone.AddEntity(cabinet, p.x, p.y)) return Refuse(zone, "placement_failed");
                if (!zone.AddEntity(indexer, p.ix, p.iy)) { zone.RemoveEntity(cabinet); return Refuse(zone, "placement_failed"); }
                anchor.SetIntProperty(Installed, 1);
                StillleafArchiveContent.EnsureRegistered();
                Diag.Record("worldgen", "StillleafIndexerPlaced", target: indexer,
                    payload: new { zone = zone.ZoneID, x = p.ix, y = p.iy, fileX = p.x, fileY = p.y, walls = p.walls });
                return true;
            }
            return Refuse(zone, "no_connected_seat");
        }

        private static readonly (int dx, int dy)[] Four = { (0, -1), (-1, 0), (1, 0), (0, 1) };

        private static bool Bare(Zone zone, int x, int y)
        {
            var c = zone.GetCell(x, y);
            if (c == null || !c.IsPassable() || zone.GenReservedCells.Contains((x, y))) return false;
            bool floor = false;
            foreach (var o in c.Objects)
            {
                if (o.BlueprintName == "StoneFloor") { floor = true; continue; }
                return false;
            }
            return floor;
        }

        /// <summary>Passable cells reachable from the zone edge, treating
        /// (blockX, blockY) as solid; creatures do not count as blocks.</summary>
        private static int Reachable(Zone zone, int blockX, int blockY)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var q = new Queue<(int x, int y)>();
            for (int x = 0; x < Zone.Width; x++) foreach (int y in new[] { 0, Zone.Height - 1 }) Seed(zone, x, y, blockX, blockY, seen, q);
            for (int y = 0; y < Zone.Height; y++) foreach (int x in new[] { 0, Zone.Width - 1 }) Seed(zone, x, y, blockX, blockY, seen, q);
            int count = q.Count;
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var (dx, dy) in Four)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Zone.Width || ny >= Zone.Height || seen[nx, ny]) continue;
                    if (!Open(zone, nx, ny, blockX, blockY)) continue;
                    seen[nx, ny] = true; q.Enqueue((nx, ny)); count++;
                }
            }
            return count;
        }
        private static void Seed(Zone zone, int x, int y, int bx, int by, bool[,] seen, Queue<(int x, int y)> q)
        { if (!seen[x, y] && Open(zone, x, y, bx, by)) { seen[x, y] = true; q.Enqueue((x, y)); } }
        private static bool Open(Zone zone, int x, int y, int bx, int by)
        {
            if (x == bx && y == by) return false;
            var c = zone.GetCell(x, y); if (c == null) return false;
            foreach (var o in c.Objects)
                if (!o.HasTag("Creature") && o.GetPart<PhysicsPart>()?.Solid == true) return false;
            return true;
        }

        private static Entity Anchor(Zone zone)
        {
            var origin = zone.GetCell(0, 0); if (origin == null) return null;
            foreach (var e in origin.Objects) if (e.GetPart<PhysicsPart>()?.Takeable == false) return e;
            return null;
        }

        public static bool CanConversation(Entity speaker, Entity player, string command)
        {
            if (!StillleafArchiveContent.ValidConversation(speaker, player, ZoneId, IndexerId, IndexerBlueprint, out var zone)) return false;
            bool found = player.GetIntProperty(FileFound) == 1;
            int terms = player.GetIntProperty(Terms);
            var cabinet = FindCabinet(zone);
            bool locked = cabinet?.GetPart<ContainerPart>()?.Locked == true;
            switch (command)
            {
                case "retrieve": return player.GetIntProperty(StillleafArchiveContent.WordsKnown) == 1 && !found;
                case "agree": case "bargain": return found && locked && terms != TermsAgreed && terms != TermsBargained;
                case "refuse": return found && locked && terms == 0;
                default: return false;
            }
        }

        public static bool TryConversation(Entity speaker, Entity player, string command)
        {
            if (!CanConversation(speaker, player, command)) return Reject(speaker, player, command, "unavailable");
            var zone = SettlementRuntime.ActiveZone;
            switch (command)
            {
                case "retrieve":
                    player.SetIntProperty(FileFound, 1);
                    // The journal advances only if the errand is open at its first stage.
                    if (StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId))
                        StoryletPart.Current.FinishObjective(StillleafArchiveContent.QuestId, "file", player);
                    MessageLog.Add("Halm runs a gloved finger down a column and stops. 'Entered: \"" + StillleafArchiveContent.LastWords + "\" Keeper, Stillleaf; salt-cured; effects: one iron key, filed beside. Status: continuing.'");
                    break;
                case "agree":
                case "bargain":
                    player.SetIntProperty(Terms, command == "agree" ? TermsAgreed : TermsBargained);
                    var cabinet = FindCabinet(zone); cabinet.GetPart<ContainerPart>().Locked = false;
                    var c = zone.GetEntityCell(cabinet); if (c != null) ZoneRenderHooks.MarkCellDirty(c.X, c.Y, "StillleafFileReleased");
                    MessageLog.Add(command == "agree"
                        ? "Halm: 'Entered: released, to the bearer of the words, under terms: to be filed, not read.' The salt file beside the desk unlocks. 'Take the key yourself. I do not hand things; I file them.'"
                        : "Halm: 'Entered: released, to the bearer of the words, under terms: custody not promised.' A pause the length of a disapproving line. The salt file beside the desk unlocks. 'Take the key yourself. The file will say what you said.'");
                    break;
                default:
                    player.SetIntProperty(Terms, TermsRefused);
                    MessageLog.Add("Halm: 'Refused, and filed as refused. A clean no. The key stays with its keeper; the words will still open the file if you return.'");
                    break;
            }
            Diag.Record("quest", "StillleafArchiveApplied", actor: player, target: speaker, payload: new { command, questId = StillleafArchiveContent.QuestId });
            return true;
        }

        private static bool Reject(Entity speaker, Entity player, string command, string reason)
        { Diag.Record("quest", "StillleafArchiveRejected", actor: player, target: speaker, payload: new { command, reason }); return false; }
        private static bool Refuse(Zone zone, string reason)
        { Diag.Record("worldgen", "StillleafIndexerRefused", payload: new { zone = zone?.ZoneID, reason }); return false; }
    }
}
