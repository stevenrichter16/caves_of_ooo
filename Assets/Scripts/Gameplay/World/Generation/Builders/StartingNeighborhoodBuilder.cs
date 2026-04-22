using CavesOfOoo.Data;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Elemental Crossroads dispatcher. Runs on every surface zone but only
    /// does work for the four cardinal neighbours of the starting village:
    ///
    ///   North  — Overworld.10.9.0  — Ruins of Sparkwright (Lightning)
    ///   East   — Overworld.11.10.0 — Saltglass Dunes      (Fire / Oil)
    ///   South  — Overworld.10.11.0 — Verdant Rotbog       (Acid / Fungal)
    ///   West   — Overworld.9.10.0  — Frostfang Grotto     (Cold / Ice)
    ///
    /// Each chunk gets one hand-authored "reaction puzzle" set piece plus a
    /// sprinkle of themed enemies and signature loot. The builder runs at
    /// Priority 3900 — after terrain/connectivity/cave entrances but before
    /// <see cref="PopulationBuilder"/> (4000), so the set piece gets first
    /// claim on its cells.
    /// </summary>
    public class StartingNeighborhoodBuilder : IZoneBuilder
    {
        public string Name => "StartingNeighborhoodBuilder";
        public int Priority => 3900;

        private const string ZoneNorth = "Overworld.10.9.0";
        private const string ZoneEast  = "Overworld.11.10.0";
        private const string ZoneSouth = "Overworld.10.11.0";
        private const string ZoneWest  = "Overworld.9.10.0";

        private const int PlacementAttempts = 80;

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null) return true;

            switch (zone.ZoneID)
            {
                case ZoneNorth:
                    BuildRuinsOfSparkwright(zone, factory, rng);
                    return true;
                case ZoneEast:
                    BuildSaltglassDunes(zone, factory, rng);
                    return true;
                case ZoneSouth:
                    BuildVerdantRotbog(zone, factory, rng);
                    return true;
                case ZoneWest:
                    BuildFrostfangGrotto(zone, factory, rng);
                    return true;
                default:
                    return true;
            }
        }

        // ============================================================
        // North — Ruins of Sparkwright (Lightning)
        // ============================================================
        //
        // Set piece: "Chain Reaction Courtyard". 3x3 grid alternating
        // BrokenCapacitor / WaterPuddle around a central WoodenBarrel.
        // Arc Bolt at any capacitor chains through the water grid and
        // ignites the barrel.
        //
        //   C  W  C
        //   W  B  W
        //   C  W  C
        //
        private static readonly (string blueprint, int dx, int dy)[] ChainReactionCourtyardLayout =
        {
            ("BrokenCapacitor", 0, 0), ("WaterPuddle",  1, 0), ("BrokenCapacitor", 2, 0),
            ("WaterPuddle",     0, 1), ("WoodenBarrel", 1, 1), ("WaterPuddle",     2, 1),
            ("BrokenCapacitor", 0, 2), ("WaterPuddle",  1, 2), ("BrokenCapacitor", 2, 2),
        };

        private void BuildRuinsOfSparkwright(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return;

            PlaceSetPiece(zone, factory, rng, openCells, ChainReactionCourtyardLayout);

            // Themed enemies scattered outside the set piece.
            for (int i = 0; i < 3; i++)
                PlaceEntity(zone, factory, rng, openCells, "BrassHusk");

            // Signature loot stashed nearby.
            PlaceEntity(zone, factory, rng, openCells, "ChainMail");
            PlaceEntity(zone, factory, rng, openCells, "OldWorldPipe");
        }

        // ============================================================
        // East — Saltglass Dunes (Fire / Oil)
        // ============================================================
        //
        // Set piece: "Oil Seep Canyon". 5-cell line of OilSeep with
        // RawMeat / Starapple on the parallel row. Igniting one end
        // cooks the whole row through fire propagation.
        //
        //   .  M  M  S  .
        //   O  O  O  O  O
        //
        private static readonly (string blueprint, int dx, int dy)[] OilSeepCanyonLayout =
        {
            ("RawMeat",   1, 0), ("RawMeat",   2, 0), ("Starapple", 3, 0),
            ("OilSeep",   0, 1), ("OilSeep",   1, 1), ("OilSeep",   2, 1),
            ("OilSeep",   3, 1), ("OilSeep",   4, 1),
        };

        private void BuildSaltglassDunes(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return;

            PlaceSetPiece(zone, factory, rng, openCells, OilSeepCanyonLayout);

            for (int i = 0; i < 4; i++)
                PlaceEntity(zone, factory, rng, openCells, "GlassScorpion");

            PlaceEntity(zone, factory, rng, openCells, "GlassblownStiletto");
            PlaceEntity(zone, factory, rng, openCells, "LanternOil");
        }

        // ============================================================
        // South — Verdant Rotbog (Acid / Fungal)
        // ============================================================
        //
        // Set piece: "Fungal Grove". 3 SporeShamblers in a row flanked
        // by 2 AcidPond. Kindle Flame on any Shambler triggers
        // fire_plus_fungal and chains across all three.
        //
        //   .  P  .
        //   F  F  F
        //   .  P  .
        //
        private static readonly (string blueprint, int dx, int dy)[] FungalGroveLayout =
        {
            ("AcidPond",      1, 0),
            ("SporeShambler", 0, 1), ("SporeShambler", 1, 1), ("SporeShambler", 2, 1),
            ("AcidPond",      1, 2),
        };

        private void BuildVerdantRotbog(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return;

            PlaceSetPiece(zone, factory, rng, openCells, FungalGroveLayout);

            // One extra wandering Shambler outside the grove.
            PlaceEntity(zone, factory, rng, openCells, "SporeShambler");

            PlaceEntity(zone, factory, rng, openCells, "Sporeblade");
            PlaceEntity(zone, factory, rng, openCells, "FirstRootGlaive");
        }

        // ============================================================
        // West — Frostfang Grotto (Cold / Ice)
        // ============================================================
        //
        // Set piece: "Brittle Ceiling Corridor". 7-tile corridor lined
        // with IceStalactite on both sides with an IceWight at each end.
        // Fire grimoires cast into the corridor melt stalactites into
        // puddles and drop the wights via fire_plus_ice.
        //
        //   |  |  |  |  |  |  |
        //   W  .  .  .  .  .  W
        //   |  |  |  |  |  |  |
        //
        private static readonly (string blueprint, int dx, int dy)[] BrittleCeilingCorridorLayout =
        {
            ("IceStalactite", 0, 0), ("IceStalactite", 1, 0), ("IceStalactite", 2, 0),
            ("IceStalactite", 3, 0), ("IceStalactite", 4, 0), ("IceStalactite", 5, 0),
            ("IceStalactite", 6, 0),
            ("IceWight",      0, 1), ("IceWight",      6, 1),
            ("IceStalactite", 0, 2), ("IceStalactite", 1, 2), ("IceStalactite", 2, 2),
            ("IceStalactite", 3, 2), ("IceStalactite", 4, 2), ("IceStalactite", 5, 2),
            ("IceStalactite", 6, 2),
        };

        private void BuildFrostfangGrotto(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return;

            PlaceSetPiece(zone, factory, rng, openCells, BrittleCeilingCorridorLayout);

            // Two extra wandering wights outside the corridor.
            PlaceEntity(zone, factory, rng, openCells, "IceWight");
            PlaceEntity(zone, factory, rng, openCells, "IceWight");

            PlaceEntity(zone, factory, rng, openCells, "TemporalShard");
            PlaceEntity(zone, factory, rng, openCells, "EchoKnife");
        }

        // ============================================================
        // Helpers — mirrors the placement pattern used by
        // VillagePopulationBuilder.PlaceDebugMaterialSandbox.
        // ============================================================

        private static void PlaceSetPiece(
            Zone zone,
            EntityFactory factory,
            System.Random rng,
            List<(int x, int y)> openCells,
            (string blueprint, int dx, int dy)[] layout)
        {
            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                if (openCells.Count == 0) return;

                int idx = rng.Next(openCells.Count);
                var (anchorX, anchorY) = openCells[idx];

                if (!CanPlaceLayoutAt(zone, anchorX, anchorY, layout))
                    continue;

                for (int i = 0; i < layout.Length; i++)
                {
                    var entry = layout[i];
                    int x = anchorX + entry.dx;
                    int y = anchorY + entry.dy;

                    Entity entity = TryCreateEntity(factory, entry.blueprint);
                    if (entity != null)
                        zone.AddEntity(entity, x, y);

                    openCells.Remove((x, y));
                }

                return;
            }
        }

        private static bool CanPlaceLayoutAt(
            Zone zone,
            int anchorX,
            int anchorY,
            (string blueprint, int dx, int dy)[] layout)
        {
            for (int i = 0; i < layout.Length; i++)
            {
                var entry = layout[i];
                int x = anchorX + entry.dx;
                int y = anchorY + entry.dy;

                if (!zone.InBounds(x, y))
                    return false;

                Cell cell = zone.GetCell(x, y);
                if (cell == null || !cell.IsPassable())
                    return false;
            }

            return true;
        }

        private static Entity PlaceEntity(
            Zone zone,
            EntityFactory factory,
            System.Random rng,
            List<(int x, int y)> openCells,
            string blueprint)
        {
            if (openCells.Count == 0) return null;

            int idx = rng.Next(openCells.Count);
            var (x, y) = openCells[idx];

            Entity entity = TryCreateEntity(factory, blueprint);
            if (entity != null)
                zone.AddEntity(entity, x, y);

            openCells.RemoveAt(idx);
            return entity;
        }

        private static Entity TryCreateEntity(EntityFactory factory, string blueprint)
        {
            if (factory == null || string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint))
                return null;

            return factory.CreateEntity(blueprint);
        }

        private static List<(int x, int y)> GatherOpenCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                    cells.Add((x, y));
            });
            return cells;
        }
    }
}
