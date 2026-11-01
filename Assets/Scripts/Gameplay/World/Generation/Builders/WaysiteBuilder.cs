using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Places a wild chunk's waysites (Docs/CHUNK-ACTIVITY.md §3). Runs after the ambient landmarks
    /// (3800), before hazards, population and containers, so creatures and caches arrange around it.
    ///
    /// <para><b>Budget and order:</b> <see cref="WaysiteCatalog.Budget"/> and <see cref="WaysitePlan.Order"/>,
    /// pure functions of the world seed and zone. Anchors come from a generator seeded from the same
    /// plan, never from the pipeline's RNG, so later builders keep their own draws.</para>
    ///
    /// <para><b>Open ground:</b> every footprint cell holds only its ground (<see cref="OpenGround"/>).</para>
    ///
    /// <para><b>Atomic:</b> a site places every owner or none. A placement that leaves fewer cells
    /// reachable from the zone's edges than its own solid footprint accounts for (it cut a route)
    /// is undone exactly, and the next anchor or kind is tried.</para>
    ///
    /// <para>Diag: worldgen/WaysitePlaced, worldgen/WaysiteRefused.</para>
    /// </summary>
    public sealed class WaysiteBuilder : IZoneBuilder
    {
        public string Name => "WaysiteBuilder";
        public int Priority => 3850;
        public const int AnchorAttempts = 60;
        const int EdgeMargin = 2;

        readonly BiomeType biome;
        readonly int tier, worldSeed;
        readonly IReadOnlyList<WaysiteKind> kinds;
        readonly int budgetOverride;

        /// <summary>The kinds placed by the last <see cref="BuildZone"/>, in order.</summary>
        public readonly List<string> Placed = new List<string>();

        public WaysiteBuilder(BiomeType biome, int tier, int worldSeed, IReadOnlyList<WaysiteKind> catalogOverride = null, int budgetOverride = -1)
        {
            this.biome = biome; this.tier = tier; this.worldSeed = worldSeed;
            kinds = catalogOverride ?? WaysiteCatalog.For(biome);
            this.budgetOverride = budgetOverride;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            Placed.Clear();
            if (zone == null || factory == null || kinds == null || kinds.Count == 0) return true;
            int budget = budgetOverride >= 0 ? budgetOverride : WaysiteCatalog.Budget(biome, WaysitePlan.Rank(worldSeed, zone.ZoneID, "budget"));
            if (budget <= 0) return true;
            var local = new Random(unchecked((int)WaysitePlan.Rank(worldSeed, zone.ZoneID, "anchors")));
            foreach (var kind in WaysitePlan.Order(worldSeed, zone.ZoneID, kinds, tier))
            {
                if (Placed.Count >= budget) break;
                if (kind?.Stamp == null || kind.Stamp.Height == 0) continue;
                if (kind.Stamp.Legend == null) { Refuse(zone, kind.Id, "unknown-blueprint"); continue; }
                var stamp = Resolve(kind, factory, local);
                if (stamp == null || !LandmarkBuilder.AllBlueprintsKnown(stamp, factory)) { Refuse(zone, kind.Id, "unknown-blueprint"); continue; }
                if (TryPlace(zone, factory, local, kind.Id, stamp)) Placed.Add(kind.Id);
            }
            return true;
        }

        /// <summary>The stamp as this placement uses it: its note marker resolved to one of the kind's
        /// notes that exists, and never clearing vegetation. A recorded placement cannot fell trees
        /// and put them back, so a clearing stamp would build its owners into them; a waysite takes
        /// open ground or none.</summary>
        static StructureStamp Resolve(WaysiteKind kind, EntityFactory factory, Random local)
        {
            bool wantsNote = false;
            foreach (var kv in kind.Stamp.Legend) if (kv.Value == WaysiteKind.NoteMarker) wantsNote = true;
            string note = null;
            if (wantsNote)
            {
                var notes = new List<string>();
                if (kind.Notes != null) foreach (var n in kind.Notes) if (n != null && factory.Blueprints.ContainsKey(n)) notes.Add(n);
                if (notes.Count == 0) return null;
                note = notes[local.Next(notes.Count)];
            }
            else if (!kind.Stamp.ClearsVegetation) return kind.Stamp;
            var legend = new Dictionary<char, string>();
            foreach (var kv in kind.Stamp.Legend) legend[kv.Key] = kv.Value == WaysiteKind.NoteMarker ? note : kv.Value;
            return new StructureStamp { Name = kind.Stamp.Name, Rows = kind.Stamp.Rows, Legend = legend, Chance = 100, MinTier = kind.Stamp.MinTier, ClearsVegetation = false };
        }

        bool TryPlace(Zone zone, EntityFactory factory, Random local, string kindId, StructureStamp stamp)
        {
            int w = stamp.Width, h = stamp.Height;
            int maxX = Zone.Width - w - EdgeMargin, maxY = Zone.Height - h - EdgeMargin;
            if (maxX <= EdgeMargin || maxY <= EdgeMargin) { Refuse(zone, kindId, "too-large"); return false; }
            int expected = ExpectedOwners(stamp);
            int before = -1, partsBefore = -1;
            string reason = "no-anchor";
            for (int attempt = 0; attempt < AnchorAttempts; attempt++)
            {
                int ax = local.Next(EdgeMargin, maxX), ay = local.Next(EdgeMargin, maxY);
                if (!LandmarkBuilder.CanPlaceAt(zone, stamp, ax, ay) || !OpenGround(zone, stamp, ax, ay)) continue;
                if (before < 0) (before, partsBefore) = Reach(zone);
                var record = LandmarkBuilder.ApplyRecorded(zone, factory, local, stamp, ax, ay);
                if (record.Created.Count != expected) { Undo(zone, record); reason = "incomplete"; continue; }
                int closed = 0;
                foreach (var (x, y) in record.Reserved) if (!zone.GetCell(x, y).IsPassable()) closed++;
                var (after, partsAfter) = Reach(zone);
                if (after < before - closed || partsAfter > partsBefore) { Undo(zone, record); reason = "cuts-route"; continue; }
                if (Diag.IsChannelEnabled("worldgen"))
                    Diag.Record("worldgen", "WaysitePlaced", payload: new { zone = zone.ZoneID, kind = kindId, ax, ay, owners = record.Created.Count });
                return true;
            }
            Refuse(zone, kindId, reason);
            return false;
        }

        /// <summary>Every footprint cell holds its ground and nothing else. Passability alone let a trough
        /// stand in reeds, a berry bush on a crop row and a signpost in a hedge gap; a waysite is a
        /// small clearing someone used, so it takes bare ground or none.</summary>
        static bool OpenGround(Zone zone, StructureStamp stamp, int ax, int ay)
        {
            for (int y = 0; y < stamp.Height; y++)
                for (int x = 0; x < stamp.Rows[y].Length; x++)
                {
                    var cell = zone.GetCell(ax + x, ay + y);
                    if (cell == null || cell.Objects.Count != 1 || !cell.Objects[0].HasTag("Terrain")) return false;
                }
            return true;
        }

        /// <summary>Owners a complete placement creates: every marked cell but interior cells.</summary>
        static int ExpectedOwners(StructureStamp stamp)
        {
            int n = 0;
            foreach (var row in stamp.Rows)
                foreach (char c in row)
                    if (c != '.' && stamp.Legend.TryGetValue(c, out var m) && !string.IsNullOrEmpty(m) && m != "interior") n++;
            return n;
        }

        static void Undo(Zone zone, LandmarkBuilder.PlacementRecord record)
        {
            foreach (var e in record.Created) if (zone.GetEntityCell(e) != null) zone.RemoveEntity(e);
            foreach (var c in record.Reserved) zone.GenReservedCells.Remove(c);
            foreach (var (x, y) in record.MadeInterior) { var cell = zone.GetCell(x, y); if (cell != null) cell.IsInterior = false; }
        }

        /// <summary>Passable cells reachable (4-way) from the zone's passable edge cells.</summary>
        public static int Reachable(Zone zone) => Reach(zone).cells;

        /// <summary>Cells reachable from the zone's edges, and how many separate pieces they form. A
        /// site that splits one road into two halves, each still touching an edge, keeps the cell
        /// count but adds a piece: that is a cut route too.</summary>
        public static (int cells, int parts) Reach(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            int cells = 0, parts = 0;
            void Flood(int sx, int sy)
            {
                if (sx < 0 || sy < 0 || sx >= Zone.Width || sy >= Zone.Height || seen[sx, sy]) return;
                var start = zone.GetCell(sx, sy);
                if (start == null || !start.IsPassable()) return;
                parts++; seen[sx, sy] = true; queue.Enqueue((sx, sy));
                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue(); cells++;
                    Seed(zone, seen, queue, x + 1, y); Seed(zone, seen, queue, x - 1, y);
                    Seed(zone, seen, queue, x, y + 1); Seed(zone, seen, queue, x, y - 1);
                }
            }
            for (int x = 0; x < Zone.Width; x++) { Flood(x, 0); Flood(x, Zone.Height - 1); }
            for (int y = 0; y < Zone.Height; y++) { Flood(0, y); Flood(Zone.Width - 1, y); }
            return (cells, parts);
        }

        static void Seed(Zone zone, bool[,] seen, Queue<(int x, int y)> queue, int x, int y)
        {
            if (x < 0 || y < 0 || x >= Zone.Width || y >= Zone.Height || seen[x, y]) return;
            var cell = zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable()) return;
            seen[x, y] = true; queue.Enqueue((x, y));
        }

        static void Refuse(Zone zone, string kind, string reason)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "WaysiteRefused", payload: new { zone = zone.ZoneID, kind, reason });
        }
    }
}
