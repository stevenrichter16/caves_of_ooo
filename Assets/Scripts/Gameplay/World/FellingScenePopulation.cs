using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Small, once-only habitat population around the authored scene.
    /// Native blueprints own AI, damage, harvesting and inventory behavior;
    /// the scene only owns initial placement and durable population revisions.</summary>
    public static class FellingScenePopulation
    {
        public const int CurrentRevision = 1;
        public const string FaunaTag = "FellingSceneFauna";
        public const string DressingTag = "FellingSupplementalDressing";
        private const string FaunaPrefix = "felling-fauna:";
        private const string DressingPrefix = "felling-dressing:";

        public sealed class DressingSpec
        {
            public readonly string id, blueprint, resource;
            public readonly int x, y, width, height;
            public readonly float footX, footY;
            internal DressingSpec(string id, string blueprint, int x, int y,
                int width, int height, float footX, float footY)
            {
                this.id = id; this.blueprint = blueprint; this.x = x; this.y = y;
                this.width = width; this.height = height; this.footX = footX; this.footY = footY;
                resource = "SceneArt/FellingSite/Art/extras/" + id;
            }
        }
        public static readonly DressingSpec[] DressingSpecs =
        {
            new DressingSpec("mushroom-pink-tall", "MushroomRing", 24, 15, 37, 63, 20, 62),
            new DressingSpec("mushroom-cyan-cluster", "MushroomRing", 54, 14, 63, 36, 31, 35),
            new DressingSpec("rubble-cluster", "Tepuibone", 55, 23, 75, 81, 38.5f, 80),
        };
        private static readonly (string id, string blueprint, int x, int y)[] Animals =
        {
            ("glasspane-spray-bank", "GlasspaneFrog", 22, 2),
            ("glasspane-stream-bank", "GlasspaneFrog", 23, 8),
            ("yellowfoot-moss-verge", "YellowfootWayfarer", 57, 20),
        };

        public static bool EnsurePopulation(Zone zone, EntityFactory factory)
        {
            if (!CanPopulate(zone, factory, out var state)) return false;
            if (state.PopulationRevision >= CurrentRevision) return true;
            foreach (var spec in Animals)
                if (!factory.Blueprints.ContainsKey(spec.blueprint)) return Refuse(zone, spec.blueprint);
            var staged = new List<(Entity entity, int x, int y)>();
            var occupied = new HashSet<(int, int)>();
            foreach (var spec in Animals)
            {
                var position = FindHabitat(zone, spec.blueprint, spec.x, spec.y, occupied, requireOpenRoutes: true);
                // A saved structure may occupy an entire habitat patch. Do not
                // move that structure or create a delayed respawn when it moves.
                if (position.x < 0) continue;
                var animal = factory.CreateEntity(spec.blueprint);
                if (animal == null) return Refuse(zone, spec.blueprint);
                animal.ID = FaunaPrefix + spec.id; animal.SetTag(FaunaTag);
                var brain = animal.GetPart<BrainPart>();
                if (brain != null)
                {
                    brain.CurrentZone = zone;
                    brain.Rng = new Random(7141 + spec.x * 101 + spec.y);
                    brain.StartingCellX = position.x; brain.StartingCellY = position.y;
                }
                staged.Add((animal, position.x, position.y)); occupied.Add(position);
            }
            foreach (var entry in staged) zone.AddEntity(entry.entity, entry.x, entry.y);
            state.PopulationRevision = CurrentRevision;
            Record(zone, "FellingFaunaPopulated", staged.Count);
            return true;
        }

        public static bool EnsureDressing(Zone zone, EntityFactory factory)
        {
            if (!CanPopulate(zone, factory, out var state)) return false;
            if (state.DressingRevision >= CurrentRevision) return true;
            foreach (var spec in DressingSpecs)
                if (!factory.Blueprints.ContainsKey(spec.blueprint)) return Refuse(zone, spec.blueprint);
            // The native harvest must have actual output content as well.
            if (!factory.Blueprints.ContainsKey("Mushroom")) return Refuse(zone, "Mushroom");
            var staged = new List<(Entity entity, int x, int y)>();
            var occupied = new HashSet<(int, int)>();
            foreach (var spec in DressingSpecs)
            {
                var position = FindHabitat(zone, spec.blueprint, spec.x, spec.y, occupied);
                if (position.x < 0) continue;
                var entity = factory.CreateEntity(spec.blueprint);
                if (entity == null) return Refuse(zone, spec.blueprint);
                entity.ID = DressingPrefix + spec.id; entity.SetTag(DressingTag);
                if (!entity.HasPart<ExaminablePart>())
                    entity.AddPart(new ExaminablePart { Text = spec.blueprint == "MushroomRing"
                        ? "A small cluster of mushrooms grows in the damp ground between old roots. They can be harvested."
                        : "A loose piece of tepuibone rests among the roots. It can be picked up." });
                staged.Add((entity, position.x, position.y)); occupied.Add(position);
            }
            foreach (var entry in staged) zone.AddEntity(entry.entity, entry.x, entry.y);
            state.DressingRevision = CurrentRevision;
            Record(zone, "FellingDressingPopulated", staged.Count);
            return true;
        }

        /// <summary>Current world ownership, including a picked-up stone later
        /// dropped at another cell. Inventory membership is deliberately hidden.</summary>
        public static Entity FindDressingOwner(Zone zone, string id)
        {
            var state = FellingSceneRuntime.GetState(zone);
            if (state == null || string.IsNullOrEmpty(id)) return null;
            if (state.DressingOwners == null || state.DressingOwnerVersion != zone.EntityVersion)
            {
                if (state.DressingOwners == null) state.DressingOwners = new Dictionary<string, Entity>(StringComparer.Ordinal);
                else state.DressingOwners.Clear();
                foreach (var entity in zone.GetReadOnlyEntities())
                {
                    if (!entity.HasTag(DressingTag) || entity.ID == null || !entity.ID.StartsWith(DressingPrefix, StringComparison.Ordinal)) continue;
                    state.DressingOwners[entity.ID.Substring(DressingPrefix.Length)] = entity;
                }
                state.DressingOwnerVersion = zone.EntityVersion;
            }
            if (!state.DressingOwners.TryGetValue(id, out var owner)) return null;
            var cell = zone.GetEntityCell(owner);
            return cell != null && cell.Objects.Contains(owner) ? owner : null;
        }

        /// <summary>Old active saves predate these actors in their saved turn
        /// queue. Inactive cached zones must remain unscheduled until entered.</summary>
        public static void RegisterActiveFauna(Zone zone, TurnManager turns)
        {
            if (turns == null || !FellingSceneRuntime.IsActive(zone)) return;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.HasTag(FaunaTag) && entity.GetStatValue("Hitpoints") > 0)
                    turns.AddEntity(entity);
        }

        private static bool CanPopulate(Zone zone, EntityFactory factory, out FellingSceneStatePart state)
        {
            state = FellingSceneRuntime.GetState(zone);
            return factory != null && FellingSceneRuntime.IsActive(zone) && state != null;
        }
        private static (int x, int y) FindHabitat(Zone zone, string blueprint, int x, int y, HashSet<(int, int)> occupied, bool requireOpenRoutes = false)
        {
            var definition = FellingSceneDefinition.Load();
            for (int radius = 0; radius <= 3; radius++)
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                        int px = x + dx, py = y + dy;
                        var cell = zone.GetCell(px, py);
                        if (cell == null || cell.BlocksMovement() || occupied.Contains((px, py))) continue;
                        if (px >= 30 && px <= 50 && py >= 8 && py <= 24) continue;
                        bool reserved = false;
                        foreach (var layer in definition.layers)
                            if (Math.Abs(layer.anchorX - px) + Math.Abs(layer.anchorY - py) <= 1) { reserved = true; break; }
                        if (reserved) continue;
                        foreach (var spec in definition.cells)
                            if (spec.x == px && spec.y == py && (spec.solid || spec.water)) { reserved = true; break; }
                        if (reserved || !StumpFaunaHabitat.Allows(blueprint, cell)) continue;
                        // Non-solid items/unknown saved scenery also keep their
                        // exact position; do not bury them under new decoration.
                        foreach (var entity in cell.Objects)
                            if (!entity.HasTag(FellingSceneRuntime.TerrainTag)) { reserved = true; break; }
                        if (!reserved && (!requireOpenRoutes || PreservesOpenRoutes(zone, px, py, occupied))) return (px, py);
                    }
            return (-1, -1);
        }
        // A solid animal must not initially occupy a one-cell crossing. Test
        // connectivity between all open neighbors with this proposed cell
        // removed; this also protects fallback placements around saved objects.
        private static bool PreservesOpenRoutes(Zone zone, int x, int y, HashSet<(int, int)> occupied)
        {
            var directions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            var neighbors = new HashSet<(int x, int y)>();
            foreach (var d in directions)
            {
                var p = (x + d.Item1, y + d.Item2);
                if (!occupied.Contains(p) && zone.GetCell(p.Item1, p.Item2)?.BlocksMovement() == false)
                    neighbors.Add(p);
            }
            if (neighbors.Count < 2) return true;
            var queue = new Queue<(int x, int y)>();
            var seen = new HashSet<(int, int)>();
            foreach (var p in neighbors) { queue.Enqueue(p); seen.Add(p); break; }
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); neighbors.Remove(p);
                if (neighbors.Count == 0) return true;
                foreach (var d in directions)
                {
                    var next = (p.x + d.Item1, p.y + d.Item2);
                    if (next == (x, y) || occupied.Contains(next) || seen.Contains(next)) continue;
                    if (zone.GetCell(next.Item1, next.Item2)?.BlocksMovement() != false) continue;
                    seen.Add(next); queue.Enqueue(next);
                }
            }
            return false;
        }
        private static bool Refuse(Zone zone, string blueprint)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "FellingPopulationRefused", payload: new { zone = zone?.ZoneID, blueprint });
            return false;
        }
        private static void Record(Zone zone, string kind, int count)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", kind, payload: new { zone = zone.ZoneID, revision = CurrentRevision, count });
        }
    }
}
