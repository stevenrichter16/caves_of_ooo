using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Rendering
{
    /// <summary>Borrowed persistent scenery art. Gameplay owns harvesting,
    /// collision, visibility, quests and light; this library changes none of them.</summary>
    public sealed class ConnectedSpread3DLibrary : ScriptableObject
    {
        public const string ResourcePath = "ConnectedSpread3D/Library";
        public const string Folder = "Assets/Resources/ConnectedSpread3D";
        public const string ReviewedSourceSha256 = "f02bf7a9c5a8d3f38a20a4379a8edb88b5e801ae384219c007ede49ab98f4069";
        public string SourceSha256;
        public Material Material;
        [Serializable] public sealed class Entry
        {
            public string Id;
            public GameObject Prefab;
            public Mesh Mesh;
            public SpawnRing3DCatalog.Model Spec;
        }
        public Entry[] Entries;
        private Dictionary<string, Entry> index;
        private HashSet<Mesh> meshes;
        public static ConnectedSpread3DLibrary Load() => Resources.Load<ConnectedSpread3DLibrary>(ResourcePath);
        public Entry Find(string id)
        {
            if (!ConnectedSpreadSource.IsModelId(id)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }
        public bool ContainsMesh(Mesh mesh)
        {
            if (mesh == null) return false;
            if (index == null) Validate();
            return meshes.Contains(mesh);
        }
        public void Validate()
        {
            index = null; meshes = null;
            var glade = ReferenceGladeVoxelLibrary.Load();
            if (SourceSha256 != ReviewedSourceSha256 || Entries == null || Entries.Length != ConnectedSpreadSource.ModelIds.Length
                || glade == null || Material == null || Material != glade.Material)
                throw new InvalidOperationException("Incomplete or foreign scenery library.");
            glade.Validate();
            var remaining = new HashSet<string>(ConnectedSpreadSource.ModelIds, StringComparer.Ordinal);
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var nextMeshes = new HashSet<Mesh>(); var prefabs = new HashSet<GameObject>();
            foreach (var entry in Entries)
            {
                if (entry == null || entry.Id == null || !remaining.Remove(entry.Id) || entry.Mesh == null
                    || !nextMeshes.Add(entry.Mesh) || entry.Prefab == null || !prefabs.Add(entry.Prefab) || entry.Spec == null)
                    throw new InvalidOperationException("Invalid scenery model identity.");
                var mesh = entry.Mesh; var root = entry.Prefab; var spec = entry.Spec;
                var filter = root.GetComponent<MeshFilter>(); var renderer = root.GetComponent<MeshRenderer>();
                if (!mesh.isReadable || mesh.vertexCount < 24 || mesh.vertexCount > 100 * 24 || mesh.vertexCount % 24 != 0
                    || mesh.subMeshCount != 1 || mesh.bindposeCount != 0 || mesh.GetTopology(0) != MeshTopology.Triangles
                    || mesh.GetIndexCount(0) != (uint)(mesh.vertexCount / 24 * 36)
                    || filter == null || filter.sharedMesh != mesh || renderer == null || renderer.sharedMaterials.Length != 1
                    || renderer.sharedMaterial != Material || !renderer.enabled || !root.activeSelf || root.transform.childCount != 0
                    || root.GetComponents<Renderer>().Length != 1 || root.GetComponents<Collider>().Length != 0
                    || root.GetComponents<MonoBehaviour>().Length != 0 || root.GetComponent<Animator>() != null
                    || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity
                    || root.transform.localScale != Vector3.one || spec.id != entry.Id || spec.path != Folder + "/" + entry.Id + ".prefab"
                    || spec.kind != "entity" || spec.materialFamily != "reference-glade-palette" || spec.rigged || spec.rigFamily != "none"
                    || spec.clips == null || spec.clips.Length != 0 || spec.sockets == null || spec.sockets.Length != 0
                    || spec.boundsCenter != mesh.bounds.center || spec.boundsSize != mesh.bounds.size
                    || spec.triangles != mesh.vertexCount / 24 * 12)
                    throw new InvalidOperationException("Scenery prefab or geometry contract differs: " + entry.Id);
                var vertices = mesh.vertices; var uv = mesh.uv; var normals = mesh.normals;
                if (uv.Length != vertices.Length || normals.Length != vertices.Length)
                    throw new InvalidOperationException("Incomplete scenery mesh buffers.");
                foreach (var point in vertices)
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)
                        || Mathf.Abs(point.x) > .501f || Mathf.Abs(point.z) > .501f || point.y < -.0351f || point.y > 1.8501f)
                        throw new InvalidOperationException("Scenery mesh leaves native cell envelope.");
                foreach (var point in uv)
                    if (!Finite(point.x) || point.y != .5f || point.x <= 0 || point.x >= 1
                        || Mathf.Abs(point.x * 24f - .5f - Mathf.Round(point.x * 24f - .5f)) > .0001f)
                        throw new InvalidOperationException("Scenery palette cell mismatch.");
                foreach (var normal in normals)
                    if (!Finite(normal.x) || !Finite(normal.y) || !Finite(normal.z) || normal.sqrMagnitude < .99f)
                        throw new InvalidOperationException("Invalid scenery normal.");
                next.Add(entry.Id, entry);
            }
            if (remaining.Count != 0) throw new InvalidOperationException("Missing scenery family.");
            meshes = nextMeshes; index = next;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnValidate() { index = null; meshes = null; }
        public static bool IsPortable(string blueprint) => blueprint == "FieldMeal" || blueprint == "DitchkeepersFootwork";
        public static bool Handles(string blueprint) => IsPortable(blueprint) || blueprint == "ConnectedBatchPan"
            || blueprint == "ConnectedKitchenEscrow" || blueprint == "ConnectedKitchenPickup"
            || blueprint == "GleanersBuckledWicket" || blueprint == "GleanersTimberPallet"
            || blueprint == "ConnectedReserveTray" || blueprint == "BotanicalInkDesk" || blueprint == "ConnectedHeavyFrame";
        public static string PortableModel(Entity owner)
        {
            if (!IsPortable(owner?.BlueprintName) || !owner.HasTag("Item") || owner.HasTag("Creature") || owner.HasTag("Natural")) return null;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>(); var stack = owner.GetPart<StackerPart>();
            if (physics?.ParentEntity != owner || !physics.Takeable || physics.Solid || render?.ParentEntity != owner || !render.Visible
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0 || stack.StackCount > stack.MaxStack)) return null;
            if (owner.BlueprintName == "FieldMeal") return owner.GetPart<FieldMealPart>()?.ParentEntity == owner ? "connected-spread-field-meal" : null;
            return owner.GetPart<GrimoirePart>()?.ParentEntity == owner ? "connected-spread-footwork-manual" : null;
        }
        static bool Contents(Entity holder, string blueprint)
        {
            var container = holder?.GetPart<ContainerPart>();
            return holder != null && container?.ParentEntity == holder && container.Contents.Any(item => item?.BlueprintName == blueprint
                && item.SpatialZone == null && item.GetPart<PhysicsPart>()?.ParentEntity == item && item.GetPart<PhysicsPart>().InInventory == holder
                && item.GetPart<PhysicsPart>().Equipped == null && (item.GetPart<StackerPart>()?.StackCount ?? 1) > 0);
        }
        static bool Finished(KitchenBatchPart batch) => batch.State == "Ready" && batch.Output != null && batch.Output.ID == batch.OutputID
            && batch.Pickup?.ID == batch.PickupID && Contents(batch.Pickup, "FieldMeal")
            && batch.Pickup.GetPart<ContainerPart>().Contents.Contains(batch.Output)
            && batch.Output.GetPart<FieldMealPart>()?.ParentEntity == batch.Output;
        /// <summary>Pure current owner and physical state selection. It never
        /// completes work, invents stock, clears permissions or repairs a save.</summary>
        public static string ResolveModel(Zone zone, Entity owner)
        {
            if (!Handles(owner?.BlueprintName) || !SpreadPresentationScope.IsActive(zone)) return null;
            var cell = zone.GetEntityCell(owner); var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>();
            if (cell?.ParentZone != zone || owner.SpatialZone != zone || !cell.Objects.Contains(owner)
                || physics?.ParentEntity != owner || physics.InInventory != null || physics.Equipped != null
                || render?.ParentEntity != owner || !render.Visible || owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || owner.GetPart<DestructiblePart>() is DestructiblePart destroyed && destroyed.Gone) return null;
            if (IsPortable(owner.BlueprintName)) return PortableModel(owner);
            if (physics.Takeable || owner.HasTag("Item")) return null;
            switch (owner.BlueprintName)
            {
                case "GleanersBuckledWicket":
                    var door = owner.GetPart<DoorPart>(); var wicketRepair = owner.GetPart<RepairablePart>();
                    if (physics.Solid || door?.ParentEntity != owner || wicketRepair?.ParentEntity != owner
                        || wicketRepair.RecipeId != "timber-wicket-hinge" || owner.HasPart<LockPart>()
                        || door.QuarterTurns < 0 || door.QuarterTurns > 3
                        || render.RenderString != (door.IsClosed ? "+" : "/")) return null;
                    return !wicketRepair.Repaired ? "connected-spread-wicket-buckled"
                        : door.IsClosed ? "connected-spread-wicket-closed" : "connected-spread-wicket-open";
                case "GleanersTimberPallet":
                    var pallet = owner.GetPart<HarvestablePart>(); var palletHandling = owner.GetPart<HandlingPart>();
                    return physics.Solid && pallet?.ParentEntity == owner && !pallet.Harvested
                        && pallet.YieldBlueprint == "SalvagedTimber" && pallet.YieldMin == 2 && pallet.YieldMax == 2 && pallet.YieldChance == 100
                        && palletHandling?.ParentEntity == owner && !palletHandling.Carryable && !palletHandling.Throwable
                        && palletHandling.Weight == 90 && palletHandling.BulkClass == "Heavy"
                        ? "connected-spread-timber-pallet" : null;
                case "ConnectedBatchPan":
                    var repair = owner.GetPart<RepairablePart>(); var batch = owner.GetPart<KitchenBatchPart>();
                    if (repair?.ParentEntity != owner || batch?.ParentEntity != owner) return null;
                    if (!repair.Repaired) return "connected-spread-pan-cracked";
                    if (batch.State == "Working" && batch.Configured && batch.StationID == owner.ID) return "connected-spread-pan-covered";
                    return batch.Configured && batch.StationID == owner.ID && Finished(batch) ? "connected-spread-pan-ready" : "connected-spread-pan-empty";
                case "ConnectedKitchenEscrow":
                    if (owner.GetPart<ContainerPart>()?.ParentEntity != owner) return null;
                    return Contents(owner, "Emberwheat") || Contents(owner, "ClaspbeanPulp") ? "connected-spread-pantry-full" : "connected-spread-pantry-empty";
                case "ConnectedKitchenPickup":
                    if (owner.GetPart<ContainerPart>()?.ParentEntity != owner) return null;
                    return Contents(owner, "FieldMeal") ? "connected-spread-pickup-full" : "connected-spread-pickup-empty";
                case "ConnectedReserveTray": return owner.GetPart<ContainerPart>()?.ParentEntity == owner ? "connected-spread-reserve-tray" : null;
                case "BotanicalInkDesk": return owner.GetPart<BotanicalInkDeskPart>()?.ParentEntity == owner ? "connected-spread-ink-desk" : null;
                case "ConnectedHeavyFrame":
                    var handling = owner.GetPart<HandlingPart>();
                    return physics.Solid && handling?.ParentEntity == owner && !handling.Carryable && !handling.Throwable && handling.BulkClass == "Massive" ? "connected-spread-heavy-frame" : null;
            }
            return null;
        }
        /// <summary>Persistent physical cord belongs to the current exact saved
        /// reserve ground, including after its crop is harvested. Public beds and
        /// stale or copied ownership markers never borrow a claim silhouette.</summary>
        public static string ResolveReserveBed(Zone zone, Entity terrain)
        {
            string keeperId = terrain?.GetProperty(SpreadExplorationResidents.ReserveBedKey);
            if (string.IsNullOrEmpty(keeperId) || !SpreadPresentationScope.IsActive(zone)
                || !RepairCultivationRecipes.HasCultivatedSoil(zone, terrain)) return null;
            var keeper = zone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == keeperId);
            var claim = keeper?.GetPart<LocalGatheringClaimPart>();
            return claim?.ParentEntity == keeper && claim != null && claim.Bound(zone)
                && (ReferenceEquals(claim.FirstSoil, terrain) || ReferenceEquals(claim.SecondSoil, terrain))
                ? "connected-spread-reserve-bed" : null;
        }
        internal static SpawnRing3DRecipe Recipe(Zone zone, Entity owner, string model)
        {
            var cell = zone.GetEntityCell(owner); bool portable = IsPortable(owner.BlueprintName);
            return new SpawnRing3DRecipe(owner, model, null, Village3DProjection.CellCentre(cell.X, cell.Y), portable, !portable && owner.BlueprintName != "GleanersTimberPallet" && owner.BlueprintName != "GleanersBuckledWicket",
                quarterTurns: owner.BlueprintName == "GleanersBuckledWicket" ? owner.GetPart<DoorPart>().QuarterTurns : 0);
        }
    }
}
