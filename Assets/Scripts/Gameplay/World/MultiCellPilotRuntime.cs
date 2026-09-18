using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Authors one ridge chunk as native, separately destructible
    /// owners. The JSON is the same cell contract used by Blender. Installation
    /// is one-time; subsequent calls preserve damage, movement and absence.</summary>
    public static class MultiCellPilotRuntime
    {
        public const string ZoneID = "Overworld.3.7.0";
        public const int WorldX = 3, WorldY = 7;
        public const string TerrainTag = "MultiCellPilotGround";
        private const string StateId = "multicell-pilot-state";
        private const string OwnerPrefix = "multicell-pilot-owner:";
        private const int Revision = 1;
        private static Layout definition;

        [Serializable] private sealed class Layout
        {
            public int schemaVersion, width, height;
            public string zoneId;
            public Placement[] placements;
        }
        [Serializable] private sealed class Placement
        {
            public string id, blueprint, modelId, cellsRaw, role;
            public int x, y;
            public bool solid, opaque, destructible;
        }

        public static MultiCellPilotStatePart GetState(Zone zone)
        {
            var cell = zone?.GetCell(0, 0);
            if (cell == null) return null;
            foreach (var entity in cell.Objects)
                if (entity.ID == StateId) return entity.GetPart<MultiCellPilotStatePart>();
            return null;
        }

        public static bool IsActive(Zone zone)
            => zone?.ZoneID == ZoneID && GetState(zone)?.Revision == Revision;

        /// <summary>Find the live canonical owner, including adopted legacy
        /// residents whose original entity ID is intentionally preserved.</summary>
        public static Entity FindOwner(Zone zone, string ownerId)
        {
            if (!IsActive(zone) || string.IsNullOrEmpty(ownerId)) return null;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.GetPart<MultiCellPilotPropPart>()?.OwnerId == ownerId
                    && entity.GetPart<DestructiblePart>()?.Gone != true)
                    return entity;
            return null;
        }

        public static bool UpgradeCachedZone(Zone zone, EntityFactory factory)
            => Ensure(zone, factory, preserveExisting: true);

        /// <summary>Stage all required native content before mutation. Fresh
        /// generation replaces the chunk; upgrades keep unrelated actors, loot,
        /// containers and landmark identities at their saved positions. Saved
        /// content wins overlapping authored placements, which stay absent.
        /// Existing tile writing is retained because its origin is not saved.</summary>
        public static bool Ensure(Zone zone, EntityFactory factory, bool preserveExisting = false)
        {
            if (zone == null || zone.ZoneID != ZoneID || factory == null)
                return Refuse(zone, "invalid_destination_or_factory");
            var state = GetState(zone);
            if (state != null)
            {
                if (state.Revision != Revision) return Refuse(zone, "unsupported_revision");
                Record("AlreadyInstalled", zone, new { revision = state.Revision });
                return true;
            }

            var layout = Load();
            if (layout == null) return Refuse(zone, "invalid_layout");
            if (!factory.Blueprints.ContainsKey("TepuiStone")) return Refuse(zone, "missing_ground_blueprint");
            foreach (var placement in layout.placements)
                if (!factory.Blueprints.ContainsKey(placement.blueprint))
                    return Refuse(zone, "missing_blueprint:" + placement.blueprint);

            var before = zone.GetAllEntities();
            Entity existingHermit = null;
            var preservedCells = new HashSet<(int x, int y)>();
            foreach (var entity in before)
            {
                if (entity.HasPart<MultiCellPilotPropPart>()) return Refuse(zone, "owner_without_install_marker");
                if (!preserveExisting || IsLegacyTerrain(entity)) continue;
                if (existingHermit == null && entity.BlueprintName == "CaveHermit") existingHermit = entity;
                foreach (var cell in zone.GetOccupiedCells(entity))
                    if (cell != null) preservedCells.Add((cell.X, cell.Y));
            }

            var ground = new List<(Entity entity, int x, int y)>(Zone.Width * Zone.Height);
            var owners = new List<(Entity entity, Placement placement)>(layout.placements.Length);
            try
            {
                state = new MultiCellPilotStatePart { Revision = Revision };
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    var floor = factory.CreateEntity("TepuiStone");
                    if (floor == null) return Refuse(zone, "ground_factory_failure");
                    floor.ID = "multicell-pilot-ground:" + x + ":" + y;
                    floor.SetTag(TerrainTag);
                    floor.Tags.Remove("Solid"); floor.Tags.Remove("Wall");
                    var physics = floor.GetPart<PhysicsPart>();
                    if (physics == null) { physics = new PhysicsPart(); floor.AddPart(physics); }
                    physics.Solid = false; physics.Takeable = false;
                    if (x == 0 && y == 0) { floor.ID = StateId; floor.AddPart(state); }
                    ground.Add((floor, x, y));
                }
                foreach (var placement in layout.placements)
                {
                    // Adoption happens only after staging succeeds, without
                    // changing the old resident's health, inventory or ID.
                    if (placement.id == "ridge-hermit" && existingHermit != null) continue;
                    var owner = factory.CreateEntity(placement.blueprint);
                    if (owner == null) return Refuse(zone, "owner_factory_failure:" + placement.id);
                    owner.ID = OwnerPrefix + placement.id;
                    Configure(owner, placement);
                    owners.Add((owner, placement));
                }
            }
            catch (Exception exception)
            {
                return Refuse(zone, "factory_exception:" + exception.GetType().Name);
            }

            // The authored layout has already been checked for bounds,
            // duplicate identities and overlapping solid bodies.
            foreach (var entity in before)
                if (!preserveExisting || IsLegacyTerrain(entity)) zone.RemoveEntity(entity);
            zone.GenReservedCells.Clear();
            foreach (var cell in preservedCells) zone.GenReservedCells.Add(cell);
            foreach (var entry in ground)
            {
                zone.AddEntity(entry.entity, entry.x, entry.y);
                zone.GetCell(entry.x, entry.y).IsInterior = false;
            }

            var skipped = new List<string>();
            foreach (var entry in owners)
            {
                bool conflict = false;
                foreach (var cell in zone.GetOccupiedCells(entry.entity, entry.placement.x, entry.placement.y))
                    if (cell == null || preservedCells.Contains((cell.X, cell.Y))) { conflict = true; break; }
                if (conflict || !zone.AddEntity(entry.entity, entry.placement.x, entry.placement.y))
                {
                    skipped.Add(entry.placement.id);
                    continue;
                }
                foreach (var cell in zone.GetOccupiedCells(entry.entity)) zone.GenReservedCells.Add((cell.X, cell.Y));
                SetHome(entry.entity, zone);
            }
            if (existingHermit != null)
            {
                // Old single-cell saves can legally contain solid stacks. Do not
                // partially adopt that resident or throw after installing terrain.
                var anchor = zone.GetEntityCell(existingHermit);
                if (anchor == null || !zone.CanPlaceFootprint(existingHermit, anchor.X, anchor.Y))
                    skipped.Add("ridge-hermit");
                else
                    foreach (var placement in layout.placements)
                        if (placement.id == "ridge-hermit") { Configure(existingHermit, placement); break; }
            }
            state.SkippedOwnerIds = string.Join("|", skipped);
            zone.AmbientTint = StumpBands.BaseTint;
            zone.AmbientLevel = Zone.DefaultAmbientLevel;
            Record("Installed", zone, new { revision = Revision, owners = layout.placements.Length - skipped.Count,
                upgrade = preserveExisting, skipped = state.SkippedOwnerIds });
            return true;
        }

        private static void Configure(Entity owner, Placement placement)
        {
            owner.AddPart(new MultiCellPilotPropPart { OwnerId = placement.id, ModelId = placement.modelId, Role = placement.role });
            if (!owner.HasPart<SpatialFootprintPart>()) owner.AddPart(new SpatialFootprintPart { CellsRaw = placement.cellsRaw });
            var physics = owner.GetPart<PhysicsPart>();
            if (physics == null) { physics = new PhysicsPart(); owner.AddPart(physics); }
            physics.Solid = placement.solid; physics.Takeable = false;
            // Creatures use Physics.Solid. The Solid tag is terrain occlusion
            // in several spell walks and must not turn a creature into a wall.
            if (placement.solid && placement.role != "actor") owner.SetTag("Solid");
            else owner.Tags.Remove("Solid");
            if (placement.opaque) owner.SetTag("Wall"); else owner.Tags.Remove("Wall");

            if (placement.role == "actor") return;
            if (placement.destructible && !owner.HasPart<DestructiblePart>())
            {
                int hp = placement.blueprint == "GrainRidge" ? 80 : placement.blueprint == "CopperPipe" ? 45 : 30;
                owner.AddPart(new DestructiblePart { HP = hp, MaxHP = hp,
                    Hardness = placement.blueprint == "GrainRidge" || placement.blueprint == "TepuiboneVein" ? 3 : 1 });
            }
            if (!owner.HasPart<MaterialPart>()) owner.AddPart(new MaterialPart
            { MaterialID = "Stone", MaterialTagsRaw = "Stone,Mineral", Combustibility = 0f, Brittleness = 0.3f });
            if (!owner.HasPart<ThermalPart>()) owner.AddPart(new ThermalPart
            { FlameTemperature = 1200f, HeatCapacity = 2f });
            if (placement.role == "movable")
            {
                physics.Weight = 60;
                owner.AddPart(new HandlingPart { Weight = 60, Carryable = false, Throwable = false, BulkClass = "Heavy" });
            }
        }

        private static void SetHome(Entity owner, Zone zone)
        {
            var brain = owner.GetPart<BrainPart>();
            if (brain == null) return;
            var cell = zone.GetEntityCell(owner);
            brain.StartingCellX = cell.X;
            brain.StartingCellY = cell.Y;
        }

        private static bool IsLegacyTerrain(Entity entity)
        {
            if (entity.HasTag("Creature") || entity.HasTag("Player")
                || entity.HasPart<ContainerPart>() || entity.HasPart<InventoryPart>() || entity.HasPart<ConversationPart>()
                || entity.HasPart<HandlingPart>() || entity.GetPart<PhysicsPart>()?.Takeable == true) return false;
            if (entity.HasTag("Terrain") || entity.HasTag("Wall") || entity.HasTag(TerrainTag)) return true;
            switch (entity.BlueprintName)
            {
                case "Tree": case "VineWall": case "MycelialColumn": case "FruitingBody":
                case "GroveRedGrowth": case "GroveSeep": case "Rock": return true;
                default: return false;
            }
        }

        private static Layout Load()
        {
            if (definition != null) return definition;
            var text = Resources.Load<TextAsset>("Content/MultiCellPilot/layout");
            if (text == null) return null;
            Layout candidate;
            try { candidate = JsonUtility.FromJson<Layout>(text.text); }
            catch (Exception) { return null; }
            if (candidate == null || candidate.schemaVersion != Revision || candidate.zoneId != ZoneID
                || candidate.width != Zone.Width || candidate.height != Zone.Height || candidate.placements == null) return null;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var solid = new HashSet<(int x, int y)>();
            var occupied = new HashSet<(int x, int y)>();
            foreach (var placement in candidate.placements)
            {
                if (placement == null || string.IsNullOrEmpty(placement.id) || !ids.Add(placement.id)
                    || string.IsNullOrEmpty(placement.modelId) || string.IsNullOrEmpty(placement.blueprint)
                    || placement.x < 0 || placement.x >= Zone.Width || placement.y < 0 || placement.y >= Zone.Height
                    || (placement.role != "actor" && placement.role != "scenery" && placement.role != "movable" && placement.role != "hazard")) return null;
                var offsets = new SpatialFootprintPart { CellsRaw = placement.cellsRaw }.Offsets;
                if (offsets.Length == 0) return null;
                foreach (var offset in offsets)
                {
                    var cell = (x: placement.x + offset.x, y: placement.y + offset.y);
                    if (cell.x < 0 || cell.x >= Zone.Width || cell.y < 0 || cell.y >= Zone.Height
                        || solid.Contains(cell) || (placement.solid && occupied.Contains(cell))) return null;
                    occupied.Add(cell); if (placement.solid) solid.Add(cell);
                }
            }
            definition = candidate;
            return definition;
        }

        private static bool Refuse(Zone zone, string reason)
        { Record("Rejected", zone, new { reason }); return false; }

        private static void Record(string kind, Zone zone, object details)
            => Diag.Record("worldgen", "MultiCellPilot" + kind, payload: new { zoneId = zone?.ZoneID, details });
    }
}
