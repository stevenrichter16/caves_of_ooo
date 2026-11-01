using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Dresses a sparse wild chunk's empty sectors (Docs/CHUNK-ACTIVITY.md §9). Runs at 3860, after the
    /// ambient landmarks (3800) and waysites (3850), so authored set pieces keep first claim on open
    /// ground and count as structure; before hazards (3900) and population (4000), so creatures arrange
    /// around it. Reads the generated zone, not a composition plan.
    ///
    /// <para><b>Empty sectors only:</b> the playable area is divided into <see cref="SectorCount"/>
    /// sectors; one whose structure (cells that are not <see cref="Bare"/> ground) is below
    /// <see cref="MinStructure"/> receives one feature from <see cref="ChunkDensityCatalog"/>.
    /// Dressed sectors are left alone, so open fields stay open and density never stacks.</para>
    ///
    /// <para><b>Bare ground, atomic, route-safe:</b> a feature takes only cells holding their ground
    /// and nothing else (<see cref="Buildable"/>), never a road, an interior or a reserved cell; it is
    /// undone exactly if it leaves fewer cells reachable from the edges than its own solids account
    /// for, or splits the reachable area.</para>
    ///
    /// <para>Anchors and shapes come from a generator seeded from the world seed and zone, never the
    /// pipeline's RNG. Diag: worldgen/DensityFeaturePlaced, worldgen/DensityFeatureRefused.</para>
    /// </summary>
    public sealed class ChunkDensityBuilder : IZoneBuilder
    {
        public string Name => "ChunkDensityBuilder";
        public int Priority => 3860;

        public const int Columns = 4, Rows = 2, SectorCount = Columns * Rows;
        /// <summary>A sector with fewer structure cells than this reads as bare ground.</summary>
        public const int MinStructure = 6;
        const int Left = 2, Right = 77, Top = 2, Bottom = 22, AnchorAttempts = 30, KindsTried = 3;

        readonly BiomeType biome;
        readonly Formation formation;
        readonly int worldSeed;

        /// <summary>The features placed by the last <see cref="BuildZone"/>, as "sector:feature".</summary>
        public readonly List<string> Placed = new List<string>();

        public ChunkDensityBuilder(BiomeType biome, Formation formation, int worldSeed)
        { this.biome = biome; this.formation = formation; this.worldSeed = worldSeed; }

        /// <summary>The sector holding the cell, or -1 outside the playable area.</summary>
        public static int SectorOf(int x, int y)
        {
            if (x < Left || x > Right || y < Top || y > Bottom) return -1;
            int col = 0, row = 0;
            while (col < Columns - 1 && x > X1(col)) col++;
            while (row < Rows - 1 && y > Y1(row)) row++;
            return row * Columns + col;
        }
        public static int SectorColumn(int sector) => sector % Columns;
        public static int SectorRow(int sector) => sector / Columns;
        static int X0(int col) => Left + col * (Right - Left + 1) / Columns;
        static int X1(int col) => Left + (col + 1) * (Right - Left + 1) / Columns - 1;
        static int Y0(int row) => Top + row * (Bottom - Top + 1) / Rows;
        static int Y1(int row) => Top + (row + 1) * (Bottom - Top + 1) / Rows - 1;

        /// <summary>A cell holding open, dry ground and nothing else. Solid ground (a dune crest, a dead tree,
        /// a wall), a pool, a well or a liquid source is structure in its own right, and so is ground under
        /// a coating (flood water).</summary>
        public static bool Bare(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.Objects.Count != 1) return false;
            var ground = cell.Objects[0];
            if (!ground.HasTag("Terrain") || !cell.IsPassable()) return false;
            if (ground.HasPart<LiquidPoolPart>() || ground.HasPart<WellPart>() || ground.HasPart<TileStateSourcePart>()) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || coatings.Count == 0;
        }

        /// <summary>Bare ground a feature may take: not a road (a hedge across the old road is a wall,
        /// whatever the route check says) and not indoors.</summary>
        public static bool Buildable(Zone zone, int x, int y)
            => Bare(zone, x, y) && zone.GetCell(x, y).Objects[0].BlueprintName != "RoadStone" && !zone.GetCell(x, y).IsInterior;

        /// <summary>Cells in the sector that are not bare ground. A road or a reserved cell is bare: a sector
        /// beside the old road is still empty country.</summary>
        public static int StructureIn(Zone zone, int sector)
        {
            int col = SectorColumn(sector), row = SectorRow(sector), n = 0;
            for (int y = Y0(row); y <= Y1(row); y++)
                for (int x = X0(col); x <= X1(col); x++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell != null && cell.Objects.Count > 0 && !Bare(zone, x, y)) n++;
                }
            return n;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            Placed.Clear();
            var kinds = ChunkDensityCatalog.For(biome, formation);
            if (zone == null || factory == null || kinds.Count == 0) return true;
            var local = new Random(unchecked((int)WaysitePlan.Rank(worldSeed, zone.ZoneID, "density")));
            int lee = (WaysitePlan.Rank(worldSeed, zone.ZoneID, "lee") & 1) == 0 ? -1 : 1;
            for (int sector = 0; sector < SectorCount; sector++)
            {
                if (StructureIn(zone, sector) >= MinStructure) continue;
                var pool = new List<(DensityFeature feature, int weight)>(kinds);
                string reason = "no-anchor"; DensityFeature last = pool[0].feature; bool placed = false;
                for (int tried = 0; tried < KindsTried && pool.Count > 0 && !placed; tried++)
                {
                    int i = Draw(pool, local); last = pool[i].feature; pool.RemoveAt(i);
                    if (!AllKnown(last, factory)) { reason = "unknown-blueprint"; continue; }
                    placed = TryPlace(zone, factory, local, lee, sector, last, ref reason);
                }
                if (placed) Placed.Add(sector + ":" + last);
                else if (Diag.IsChannelEnabled("worldgen"))
                    Diag.Record("worldgen", "DensityFeatureRefused", payload: new { zone = zone.ZoneID, sector, feature = last.ToString(), reason });
            }
            return true;
        }

        static int Draw(List<(DensityFeature feature, int weight)> pool, Random rng)
        {
            int total = 0; foreach (var p in pool) total += p.weight;
            int roll = rng.Next(total), i = 0;
            while (roll >= pool[i].weight) { roll -= pool[i].weight; i++; }
            return i;
        }

        static bool AllKnown(DensityFeature feature, EntityFactory factory)
        {
            foreach (var (_, _, bp) in ChunkDensityCatalog.Shape(feature, new Random(0), 1))
                if (!factory.Blueprints.ContainsKey(bp)) return false;
            return true;
        }

        bool TryPlace(Zone zone, EntityFactory factory, Random local, int lee, int sector, DensityFeature feature, ref string reason)
        {
            int col = SectorColumn(sector), row = SectorRow(sector);
            int before = -1, partsBefore = -1;
            for (int attempt = 0; attempt < AnchorAttempts; attempt++)
            {
                var shape = ChunkDensityCatalog.Shape(feature, local, lee);
                int ax = local.Next(X0(col), X1(col) + 1), ay = local.Next(Y0(row), Y1(row) + 1);
                bool fits = true;
                foreach (var (dx, dy, _) in shape)
                {
                    int x = ax + dx, y = ay + dy;
                    if (x < Left || x > Right || y < Top || y > Bottom || zone.GenReservedCells.Contains((x, y)) || !Buildable(zone, x, y)) { fits = false; break; }
                }
                if (!fits) continue;
                if (before < 0) (before, partsBefore) = WaysiteBuilder.Reach(zone);
                var created = new List<Entity>(); int solids = 0;
                foreach (var (dx, dy, bp) in shape)
                {
                    var e = factory.CreateEntity(bp);
                    if (e == null) continue;
                    zone.AddEntity(e, ax + dx, ay + dy); created.Add(e);
                }
                foreach (var (dx, dy, _) in shape) if (!zone.GetCell(ax + dx, ay + dy).IsPassable()) solids++;
                var (after, partsAfter) = WaysiteBuilder.Reach(zone);
                if (created.Count != shape.Count || after < before - solids || partsAfter > partsBefore)
                {
                    foreach (var e in created) if (zone.GetEntityCell(e) != null) zone.RemoveEntity(e);
                    reason = created.Count != shape.Count ? "incomplete" : "cuts-route";
                    continue;
                }
                if (Diag.IsChannelEnabled("worldgen"))
                    Diag.Record("worldgen", "DensityFeaturePlaced", payload: new { zone = zone.ZoneID, sector, feature = feature.ToString(), ax, ay, cells = shape.Count });
                return true;
            }
            return false;
        }
    }
}
