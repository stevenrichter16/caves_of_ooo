#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Offline coarse scenery overlay. Native owners, prefab transforms,
    /// rigs, material instances and the authored village manifest stay untouched.
    /// Geometry is authored in the existing model frame, never fitted by AABB.</summary>
    public static class MorrowfastCoarseVoxelBuilder
    {
        public const string OutputRoot = "Assets/Art3D/VoxelWorld/Morrowfast";
        const string Ownership = "CavesOfOoo.Morrowfast.CoarseScenery/1";
        const string PalettePath = "Assets/Art3D/Village/Textures/VillagePalette.png";
        static readonly string[] Rooms = { "keeper-gatehouse", "dry-hem-guesthouse", "long-loop-ropeshop", "return-desk-archive", "second-bowl-kitchen" };
        static readonly HashSet<string> Singles = new HashSet<string>(StringComparer.Ordinal)
        { "barrel-side", "bucket-0", "bucket-1", "herb-pot", "bowl", "rope-coil", "table-0", "table-1", "table-2",
          "stool", "bed-0", "bed-1", "bookshelf", "bench", "hearth-0", "hearth-1", "bread-oven", "central-well",
          "oath-arch", "market-stall-0", "market-stall-1", "handcart", "footbridge", "chopping-block", "oak-door" };
        static readonly string[] Families = { "barrel-", "crate-", "planter-", "fence-", "shrub-", "tree-", "rocks-", "grass-", "ground-patch-" };
        [Serializable] public sealed class Row
        {
            public string modelId, sourceKey, sourcePath, outputPath, outputGuid;
            public bool water;
            public int vertices, triangles, colors;
        }
        [Serializable] public sealed class Report
        {
            public string status, error, finishedUtc;
            public int models, meshes, preservedBindings;
            public bool borrowedAssetsUnchanged, generatedGuidsPreserved;
            public Row[] rows;
        }
        sealed class Pending
        {
            public VoxelWorldMeshCatalog.Binding Binding;
            public Mesh Mesh;
            public Row Row;
        }

        public static bool SupportsModel(string id)
        {
            if (id == null) return false;
            if (Singles.Contains(id)) return true;
            foreach (var prefix in Families)
                if (id.Length == prefix.Length + 1 && id.StartsWith(prefix, StringComparison.Ordinal)
                    && id[id.Length - 1] >= '0' && id[id.Length - 1] <= '3') return true;
            foreach (string room in Rooms) if (id == room + "-shell" || id == room + "-roof") return true;
            return TryPatch(id, out _, out _);
        }

        public static Mesh CreateModelMesh(string modelId, MorrowfastSceneDefinition native, bool waterSurface = false)
        {
            if (!SupportsModel(modelId)) throw new ArgumentException("No coarse scenery recipe for " + modelId, nameof(modelId));
            if (native == null) throw new ArgumentNullException(nameof(native));
            native.Validate();
            var shape = new Shape();
            if (TryPatch(modelId, out int patchX, out int patchY))
                Detail(shape, native, patchX, patchY, waterSurface);
            else if (modelId == "central-well") Well(shape, waterSurface);
            else
            {
                if (waterSurface) throw new ArgumentException("This scenery model has no native water renderer.", nameof(waterSurface));
                if (modelId.StartsWith("ground-patch-", StringComparison.Ordinal))
                    shape.Box(-5, -.08f, -2.5f, 5, 0, 2.5f, 0);
                else if (modelId.EndsWith("-shell", StringComparison.Ordinal)) Shell(shape, native, modelId);
                else if (modelId.EndsWith("-roof", StringComparison.Ordinal)) Roof(shape, native, modelId);
                else Furniture(shape, modelId);
            }
            return shape.Mesh("MC_" + modelId + (waterSurface ? "_water" : ""));
        }

        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs(); string report = null;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-morrowfastCoarseReport") report = args[i + 1];
            Build(report);
        }

        public static Report Build(string reportPath = null)
        {
            var report = new Report(); var pending = new List<Pending>();
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    throw new InvalidOperationException("Generate Morrowfast art outside Play and compilation.");
                var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                var catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                if (village == null || catalog == null) throw new InvalidOperationException("Import the original Village and voxel catalog first.");
                village.Validate(); catalog.Validate(); var native = MorrowfastSceneDefinition.Load();
                if (native == null) throw new InvalidOperationException("Native village geometry is unavailable.");
                var borrowed = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(village), true))
                {
                    if (File.Exists(path)) borrowed[path] = Hash(path);
                    if (File.Exists(path + ".meta")) borrowed[path + ".meta"] = Hash(path + ".meta");
                }
                string nativePath = AssetDatabase.GetAssetPath(Resources.Load<TextAsset>("SceneArt/Morrowfast/definition"));
                borrowed[nativePath] = Hash(nativePath);
                var existingGuids = new Dictionary<string, string>(StringComparer.Ordinal);
                var sources = new HashSet<Mesh>();
                foreach (var model in village.Models.OrderBy(m => m.Id, StringComparer.Ordinal))
                {
                    if (!SupportsModel(model.Id)) continue;
                    if (model.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                        throw new InvalidOperationException("Coarse static scenery must not claim a rig: " + model.Id);
                    var filters = model.Prefab.GetComponentsInChildren<MeshFilter>(true);
                    if (filters.Length == 0) throw new InvalidOperationException("Scenery has no source mesh: " + model.Id);
                    foreach (var filter in filters)
                    {
                        var source = filter.sharedMesh; var renderer = filter.GetComponent<MeshRenderer>();
                        if (source == null || renderer == null || source.subMeshCount != 1 || source.bindposeCount != 0
                            || renderer.sharedMaterials.Length != 1 || !sources.Add(source))
                            throw new InvalidOperationException("Ambiguous or animated scenery source: " + model.Id);
                        string path = AssetDatabase.GetAssetPath(source);
                        if (path != "Assets/Art3D/Village/Models/" + model.Id + ".fbx")
                            throw new InvalidOperationException("Coarse override must reference its exact original Village source: " + model.Id);
                        var material = renderer.sharedMaterial;
                        bool water = material != null && material.shader.name == "CavesOfOoo/Village3D/Water";
                        if (material == null || (!water && (material.shader.name != "CavesOfOoo/Village3D/Palette"
                            || AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")) != PalettePath
                            || material.GetColor("_BaseColor") != Color.white
                            || material.GetTextureScale("_BaseMap") != Vector2.one || material.GetTextureOffset("_BaseMap") != Vector2.zero)))
                            throw new InvalidOperationException("Scenery no longer uses its verified native material: " + model.Id);
                        var binding = catalog.Bindings.SingleOrDefault(b => b.Source == source);
                        if (binding == null) throw new InvalidOperationException("Scenery source has no original catalog binding: " + model.Id);
                        var mesh = CreateModelMesh(model.Id, native, water);
                        var row = new Row { modelId = model.Id, sourceKey = binding.SourceKey, sourcePath = path,
                            outputPath = OutputRoot + "/" + binding.SourceKey + ".asset", water = water };
                        pending.Add(new Pending { Binding = binding, Mesh = mesh, Row = row });
                        // Prefab placements/rotations remain unchanged. Compensate
                        // only the imported child transform (including FBX scale).
                        Transform(mesh, filter.transform.worldToLocalMatrix * model.Prefab.transform.localToWorldMatrix);
                        row.vertices = mesh.vertexCount; row.triangles = mesh.triangles.Length / 3;
                        row.colors = water ? 1 : mesh.uv.Distinct().Count();
                        ValidateDestination(row.outputPath);
                        string guid = AssetDatabase.AssetPathToGUID(row.outputPath);
                        if (!string.IsNullOrEmpty(guid)) existingGuids[row.outputPath] = guid;
                    }
                    report.models++;
                }
                if (report.models != 111) throw new InvalidOperationException("The complete 111-model scenery contract changed; revise coverage before exporting.");
                foreach (var item in borrowed) if (Hash(item.Key) != item.Value) throw new InvalidOperationException("Source changed while preparing scenery: " + item.Key);
                EnsureFolder(OutputRoot);
                var updated = catalog.Bindings.Select(Clone).ToArray();
                // Prevalidate the complete proposed table before touching output.
                var candidate = ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();
                try
                {
                    candidate.Bindings = updated.Select(Clone).ToArray();
                    foreach (var entry in pending) candidate.Bindings.Single(b => b.Source == entry.Binding.Source).Voxel = entry.Mesh;
                    candidate.Validate();
                }
                finally { Object.DestroyImmediate(candidate); }
                foreach (var entry in pending)
                {
                    var destination = AssetDatabase.LoadAssetAtPath<Mesh>(entry.Row.outputPath);
                    if (destination == null)
                    {
                        destination = entry.Mesh; AssetDatabase.CreateAsset(destination, entry.Row.outputPath); entry.Mesh = null;
                        var importer = AssetImporter.GetAtPath(entry.Row.outputPath);
                        importer.userData = Ownership; importer.SaveAndReimport();
                    }
                    else
                    {
                        VoxelWorldMeshBuilder.ReplaceGeneratedGeometry(entry.Mesh, destination);
                        // The shared channel copier preserves whole-mesh bounds,
                        // but SetTriangles(false) leaves draw-range bounds empty.
                        // Preserve the validated source descriptors so an in-place
                        // rebake is identical to first creation, including culling.
                        for (int sub = 0; sub < entry.Mesh.subMeshCount; sub++)
                            destination.SetSubMesh(sub, entry.Mesh.GetSubMesh(sub), MeshUpdateFlags.DontRecalculateBounds);
                        EditorUtility.SetDirty(destination);
                    }
                    updated.Single(b => b.Source == entry.Binding.Source).Voxel = destination;
                    entry.Row.outputGuid = AssetDatabase.AssetPathToGUID(entry.Row.outputPath);
                }
                catalog.Bindings = updated; catalog.InvalidateCaches(); catalog.Validate();
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                foreach (var item in existingGuids)
                    if (AssetDatabase.AssetPathToGUID(item.Key) != item.Value) throw new InvalidOperationException("Generated scenery GUID changed: " + item.Key);
                foreach (var item in borrowed)
                    if (Hash(item.Key) != item.Value) throw new InvalidOperationException("Borrowed original art changed: " + item.Key);
                report.meshes = pending.Count; report.preservedBindings = catalog.Bindings.Length - pending.Count;
                report.borrowedAssetsUnchanged = report.generatedGuidsPreserved = true; report.status = "passed";
                Debug.Log("Morrowfast coarse scenery: " + report.models + " models / " + report.meshes + " meshes; native owners, rigs and sources preserved.");
                return report;
            }
            catch (Exception e) { report.status = "failed"; report.error = e.ToString(); throw; }
            finally
            {
                report.rows = pending.Select(p => p.Row).ToArray(); report.finishedUtc = DateTime.UtcNow.ToString("O");
                if (!string.IsNullOrEmpty(reportPath))
                {
                    string full = Path.GetFullPath(reportPath); Directory.CreateDirectory(Path.GetDirectoryName(full));
                    File.WriteAllText(full, JsonUtility.ToJson(report, true));
                }
                foreach (var entry in pending) if (entry.Mesh != null) Object.DestroyImmediate(entry.Mesh);
            }
        }
        static VoxelWorldMeshCatalog.Binding Clone(VoxelWorldMeshCatalog.Binding b)
            => new VoxelWorldMeshCatalog.Binding { Source = b.Source, Voxel = b.Voxel, SourceKey = b.SourceKey,
                VoxelSize = b.VoxelSize, WorldVoxelSize = b.WorldVoxelSize, SourceBindposes = b.SourceBindposes };
        static string Hash(string path)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static void ValidateDestination(string path)
        {
            if (Path.GetDirectoryName(path).Replace('\\', '/') != OutputRoot) throw new ArgumentException("Scenery destination escapes its folder.");
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null && (!(asset is Mesh) || AssetImporter.GetAtPath(path)?.userData != Ownership))
                throw new InvalidOperationException("Refusing to overwrite foreign art: " + path);
            if (asset == null && (File.Exists(path) || Directory.Exists(path))) throw new InvalidOperationException("Unrecognized scenery destination: " + path);
        }
        static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
        static void Transform(Mesh mesh, Matrix4x4 matrix)
        {
            if (Mathf.Abs(matrix.determinant) < .00000001f) throw new ArgumentException("Imported mesh transform is singular.");
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            mesh.vertices = vertices;
            if (matrix.determinant < 0)
            {
                var indices = mesh.triangles;
                for (int i = 0; i < indices.Length; i += 3) { int swap = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = swap; }
                mesh.triangles = indices;
            }
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        static bool TryPatch(string id, out int x, out int y)
        {
            x = y = 0;
            if (id == null || id.Length != 18 || !id.StartsWith("detail-patch-", StringComparison.Ordinal)
                || id[13] != '0' || id[15] != '-' || id[16] != '0') return false;
            // Exact two-digit coordinates are the existing manifest's patch IDs.
            x = id[14] - '0'; y = id[17] - '0';
            return x >= 0 && x < 8 && y >= 0 && y < 5;
        }

        static void Shell(Shape shape, MorrowfastSceneDefinition native, string modelId)
        {
            var room = native.buildings.Single(b => b.id + "-shell" == modelId);
            var owner = native.FindOwner(modelId); var door = native.FindOwner(room.doorId);
            int x0 = room.interior.Min(p => p.x), x1 = room.interior.Max(p => p.x);
            int y0 = room.interior.Min(p => p.y), y1 = room.interior.Max(p => p.y);
            float ox = owner.anchorX + .5f, oz = 24.5f - owner.anchorY;
            shape.Box(x0 - ox, 0, 24 - y1 - oz, x1 + 1 - ox, .12f, 25 - y0 - oz, 6);
            var mask = new bool[Zone.Width, Zone.Height];
            foreach (var cell in native.cells)
                if (cell.solid && cell.x >= x0 - 1 && cell.x <= x1 + 1 && cell.y >= y0 - 1 && cell.y <= y1 + 1
                    && (cell.x < x0 || cell.x > x1 || cell.y < y0 || cell.y > y1)
                    && !(cell.x == door.anchorX && cell.y == door.anchorY)) mask[cell.x, cell.y] = true;
            Rectangles(mask, (x, y, w, h) =>
            {
                shape.Box(x - ox, 0, 25 - y - h - oz, x + w - ox, .62f, 25 - y - oz, 6);
                shape.Box(x - ox, .62f, 25 - y - h - oz, x + w - ox, 1.4f, 25 - y - oz, 4);
            });
        }
        static void Roof(Shape shape, MorrowfastSceneDefinition native, string modelId)
        {
            var room = native.buildings.Single(b => b.id + "-roof" == modelId); var owner = native.FindOwner(modelId);
            float x0 = room.interior.Min(p => p.x) - 1 - owner.anchorX - .5f;
            float x1 = room.interior.Max(p => p.x) + 2 - owner.anchorX - .5f;
            float z0 = owner.anchorY - room.interior.Max(p => p.y) - 1.5f;
            float z1 = owner.anchorY - room.interior.Min(p => p.y) + 1.5f;
            shape.Box(x0, 1.4f, z0, x1, 1.82f, z1, 6);
            shape.Box(x0 + .35f, 1.82f, z0 + .35f, x1 - .35f, 2.02f, z1 - .35f, 40);
            // A single broad ridge distinguishes the long inn without tiny tiles.
            if (room.id == "dry-hem-guesthouse") shape.Box(x0 + .6f, 2.02f, (z0 + z1) / 2 - .18f, x1 - .6f, 2.18f, (z0 + z1) / 2 + .18f, 6);
        }

        static void Detail(Shape shape, MorrowfastSceneDefinition native, int gx, int gy, bool water)
        {
            float ox = gx * 10 + 5, oz = gy * 5 + 2.5f;
            var mask = new bool[Zone.Width, Zone.Height]; var paths = water ? null : RouteCells(native);
            foreach (var cell in native.cells)
                if (cell.x / 10 == gx && (24 - cell.y) / 5 == gy
                    && (water ? cell.water : paths[cell.x, cell.y] && !cell.water)) mask[cell.x, cell.y] = true;
            Rectangles(mask, (x, y, w, h) => shape.Box(x - ox, water ? .014f : .008f, 25 - y - h - oz,
                x + w - ox, water ? .026f : .045f, 25 - y - oz, water ? 24 : 38));
            // Empty existing detail bindings remain valid, fully buried under
            // the ground. No decorative object or water owner is synthesized.
            if (shape.Count == 0) shape.Box(-.25f, -.07f, -.25f, .25f, -.06f, .25f, water ? 24 : 38);
        }
        static bool[,] RouteCells(MorrowfastSceneDefinition native)
        {
            var walkable = new bool[Zone.Width, Zone.Height]; var route = new bool[Zone.Width, Zone.Height];
            foreach (var c in native.cells)
                walkable[c.x, c.y] = !c.solid && !c.water && !c.interior && c.x >= 21 && c.x <= 58
                    && !((c.x == 41 || c.x == 42) && c.y >= 21 && c.y <= 23);
            foreach (var owner in native.owners)
                if (owner.kind != "npc" && owner.kind != "creature" && owner.kind != "door")
                    foreach (var cell in owner.footprint) walkable[cell.x, cell.y] = false;
            // A route can cross the authored bridge's real support cells, but
            // Detail still paints only the dry banks. Removing the bridge owner
            // can never leave a permanent ground slab over the creek.
            foreach (var owner in native.owners.Where(o => o.kind == "bridge"))
                foreach (var cell in owner.bridgeSupport)
                    if (!native.cells.Any(c => c.x == cell.x && c.y == cell.y && (c.solid || c.interior)))
                        walkable[cell.x, cell.y] = true;
            var well = native.FindOwner("central-cistern");
            int left = well.footprint.Min(c => c.x) - 1, right = well.footprint.Max(c => c.x) + 1;
            int north = well.footprint.Min(c => c.y) - 1, south = well.footprint.Max(c => c.y) + 1;
            var goals = new List<Vector2Int>();
            for (int x = left; x <= right; x++) foreach (int y in new[] { north, south })
                if (walkable[x, y]) { route[x, y] = true; goals.Add(new Vector2Int(x, y)); }
            for (int y = north + 1; y < south; y++) foreach (int x in new[] { left, right })
                if (walkable[x, y]) { route[x, y] = true; goals.Add(new Vector2Int(x, y)); }
            var destinations = native.buildings.Select(b => new Vector2Int(b.entryX, b.entryY)).ToList();
            destinations.Add(new Vector2Int(native.FindOwner("north-oath-arch").anchorX, 0));
            destinations.Add(new Vector2Int(40, 24));
            destinations.Add(new Vector2Int(21, native.FindOwner("western-footbridge").anchorY));
            foreach (var start in destinations)
            {
                if (!walkable[start.x, start.y]) continue;
                var queue = new Queue<Vector2Int>(); var previous = new Dictionary<Vector2Int, Vector2Int>();
                queue.Enqueue(start); previous[start] = start; Vector2Int end = start; bool found = false;
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue();
                    if (goals.Contains(at)) { end = at; found = true; break; }
                    foreach (var step in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down })
                    {
                        var next = at + step;
                        if (next.x < 0 || next.x >= Zone.Width || next.y < 0 || next.y >= Zone.Height || !walkable[next.x, next.y] || previous.ContainsKey(next)) continue;
                        previous[next] = at; queue.Enqueue(next);
                    }
                }
                if (!found) continue;
                for (var at = end; ; at = previous[at]) { route[at.x, at.y] = true; if (at == start) break; }
            }
            return route;
        }
        static void Rectangles(bool[,] source, Action<int, int, int, int> emit)
        {
            var remaining = (bool[,])source.Clone(); int width = remaining.GetLength(0), height = remaining.GetLength(1);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (!remaining[x, y]) continue;
                int w = 1; while (x + w < width && remaining[x + w, y]) w++;
                int h = 1;
                while (y + h < height)
                {
                    bool entire = true; for (int dx = 0; dx < w; dx++) if (!remaining[x + dx, y + h]) { entire = false; break; }
                    if (!entire) break; h++;
                }
                for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) remaining[xx, yy] = false;
                emit(x, y, w, h);
            }
        }

        static void Well(Shape s, bool water)
        {
            if (water) { s.Box(-1.3f, .18f, -1.3f, 1.3f, .22f, 1.3f, 24); return; }
            // Two chunky square courses, open interior; water consumes the
            // second whole-object color in its existing separate renderer.
            s.Ring(1.75f, 1.24f, 0, .7f, 4); s.Ring(1.85f, 1.20f, .7f, 1.05f, 4);
        }
        static void Furniture(Shape s, string id)
        {
            int v = id[id.Length - 1] >= '0' && id[id.Length - 1] <= '3' ? id[id.Length - 1] - '0' : 0;
            if (id.StartsWith("barrel-", StringComparison.Ordinal))
            {
                if (id == "barrel-side")
                {
                    s.Box(-.38f, .05f, -.46f, .38f, .81f, .46f, 8);
                    foreach (float z in new[] { -.3f, .24f }) s.Box(-.42f, .01f, z, .42f, .85f, z + .08f, 12);
                }
                else
                {
                    float top = .88f + v * .025f; s.Box(-.4f, 0, -.4f, .4f, top, .4f, 8);
                    foreach (float y in new[] { .12f, top - .2f }) s.Box(-.43f, y, -.43f, .43f, y + .09f, .43f, 12);
                }
            }
            else if (id.StartsWith("crate-", StringComparison.Ordinal))
            {
                float top = .75f + v * .04f; s.Box(-.4f, 0, -.4f, .4f, top, .4f, 8);
                s.Box(-.43f, top - .10f, -.43f, .43f, top, .43f, 9);
                foreach (float x in new[] { -.31f, .23f }) s.Box(x, .1f, -.425f, x + .08f, top - .1f, .425f, 9);
            }
            else if (id.StartsWith("planter-", StringComparison.Ordinal))
            {
                float half = v == 3 ? .55f : 1f;
                s.Box(-half, 0, -.5f, half, .35f, .5f, 8);
                int count = v == 1 || v == 3 ? 2 : 3;
                for (int i = 0; i < count; i++)
                {
                    float x = count == 2 ? (-.5f + i) * half : -.65f + .65f * i;
                    float breadth = v == 1 ? .32f : .22f;
                    s.Box(x - breadth, .35f, -.32f, x + breadth, .59f + (i % 2) * .05f + v * .015f, .32f, 16);
                }
            }
            else if (id.StartsWith("fence-", StringComparison.Ordinal))
            {
                foreach (float x in new[] { -1.27f, 1.03f }) s.Box(x, 0, -.14f, x + .24f, .94f + v * .035f, .14f, 8);
                foreach (float y in new[] { .27f + v * .015f, .65f + v * .025f }) s.Box(-1.15f, y, -.1f, 1.15f, y + .17f, .1f, 9);
            }
            else if (id.StartsWith("tree-", StringComparison.Ordinal))
            {
                s.Box(-.17f, 0, -.17f, .17f, 1.2f, .17f, 8);
                s.Box(-1.25f, .88f, -.98f, 1.25f, 1.5f, 1.02f, 16);
                float shift = (v - 1.5f) * .08f; s.Box(-.85f + shift, 1.5f, -.68f, .92f + shift, 1.92f, .7f, 16);
            }
            else if (id.StartsWith("shrub-", StringComparison.Ordinal))
            {
                s.Box(-.55f, .1f, -.44f, .55f, .45f, .44f, 16);
                s.Box(-.35f + v * .03f, .45f, -.28f, .35f + v * .03f, .72f, .3f, 16);
            }
            else if (id.StartsWith("rocks-", StringComparison.Ordinal))
            {
                s.Box(-.4f, 0, -.5f, .28f, .3f, .42f, 6);
                s.Box(-.24f + v * .02f, .3f, -.25f, .16f + v * .02f, .5f, .24f, 4);
            }
            else if (id.StartsWith("grass-", StringComparison.Ordinal))
            {
                s.Box(-.3f, 0, -.22f, -.12f, .13f, .2f, 16);
                s.Box(-.06f, 0, -.3f, .12f, .18f + v * .015f, .25f, 16);
                s.Box(.18f, 0, -.16f, .32f, .12f, .18f, 16);
            }
            else if (id.StartsWith("bucket-", StringComparison.Ordinal))
            { s.Ring(.32f, .23f, .06f, .57f, 8); s.Box(-.3f, .015f, -.3f, .3f, .065f, .3f, 8); s.Box(-.3f, .7f, -.05f, .3f, .81f, .05f, 12); }
            else if (id == "herb-pot")
            { s.Box(-.32f, 0, -.32f, .32f, .46f, .32f, 51); s.Box(-.25f, .46f, -.2f, .25f, .79f, .22f, 16); }
            else if (id == "bowl")
            { s.Ring(.4f, .28f, .02f, .24f, 8); s.Box(-.29f, .015f, -.29f, .29f, .09f, .29f, 19); }
            else if (id == "rope-coil")
            { s.Ring(.4f, .21f, .02f, .10f, 14); s.Box(.28f, .02f, -.05f, .54f, .10f, .05f, 14); }
            else if (id.StartsWith("table-", StringComparison.Ordinal))
            {
                float half = v == 2 ? .82f : .65f; s.Box(-half, .7f, -.37f, half, .84f, .37f, 9);
                foreach (float x in new[] { -half + .08f, half - .22f }) foreach (float z in new[] { -.3f, .16f }) s.Box(x, 0, z, x + .14f, .7f, z + .14f, 8);
                if (v == 1) s.Box(-.28f, .84f, -.23f, .23f, 1.04f, .2f, 8);
                if (v == 2) s.Box(-.6f, .84f, -.13f, -.05f, .92f, .13f, 8);
            }
            else if (id == "stool")
            { s.Box(-.27f, .38f, -.27f, .27f, .52f, .27f, 9); foreach (float x in new[] { -.2f, .08f }) s.Box(x, 0, -.18f, x + .12f, .38f, .18f, 8); }
            else if (id.StartsWith("bed-", StringComparison.Ordinal))
            { s.Box(-.43f, .07f, -.86f, .43f, .3f, .86f, 8); s.Box(-.4f, .3f, -.6f + v * .08f, .4f, .47f, .75f, 48); s.Box(-.4f, .3f, -.81f, .4f, .56f + v * .05f, -.56f + v * .08f, 8); s.Box(-.43f, .3f, .75f, .43f, .65f, .86f, 8); }
            else if (id == "bookshelf")
            {
                foreach (float x in new[] { -.68f, .54f }) s.Box(x, 0, -.18f, x + .14f, 1.7f, .18f, 8);
                foreach (float y in new[] { .04f, .8f, 1.55f }) s.Box(-.54f, y, -.18f, .54f, y + .15f, .18f, 8);
                s.Box(-.5f, .19f, -.13f, .38f, .7f, .16f, 52); s.Box(-.32f, .95f, -.13f, .5f, 1.48f, .16f, 52);
            }
            else if (id == "bench")
            { s.Box(-.75f, .45f, -.2f, .75f, .6f, .28f, 9); foreach (float x in new[] { -.6f, .42f }) s.Box(x, 0, -.12f, x + .18f, .45f, .19f, 8); s.Box(-.75f, .65f, .16f, .75f, .98f, .3f, 8); }
            else if (id.StartsWith("hearth-", StringComparison.Ordinal) || id == "bread-oven")
            {
                float scale = id == "bread-oven" ? 1.7f : 1; s.Scale = scale;
                s.Box(-.6f, 0, -.4f, -.32f, .83f, .4f, 6); s.Box(.32f, 0, -.4f, .6f, .83f, .4f, 6);
                s.Box(-.32f, 0, .15f, .32f, .83f, .4f, 6); s.Box(-.6f, .83f, -.4f, .6f, 1f, .4f, 4);
                s.Box(-.32f, 0, -.47f, .32f, .08f, .15f, 6); s.Scale = 1;
            }
            else if (id == "oath-arch")
            { foreach (float x in new[] { -2.08f, 1.74f }) s.Box(x, 0, -.26f, x + .34f, 2.15f, .26f, 8); s.Box(-2.08f, 1.88f, -.27f, 2.08f, 2.27f, .27f, 9); }
            else if (id.StartsWith("market-stall-", StringComparison.Ordinal))
            {
                foreach (float x in new[] { -1.45f, 1.27f }) foreach (float z in new[] { -1.42f, 1.07f }) s.Box(x, 0, z, x + .18f, 1.86f, z + .18f, 8);
                s.Box(-1.55f, 1.86f, -1.55f, 1.55f, 2.06f, 1.27f, v == 0 ? 22 : 23);
                s.Box(-1.45f, .61f, -1.57f, 1.45f, .82f, -.93f, 8);
                s.Box(-1.17f, .1f, -.9f, -.28f, .64f, .1f, 8); s.Box(.3f, .1f, -.8f, 1.1f, .48f, .1f, 8);
            }
            else if (id == "handcart")
            {
                s.Box(-.55f, .39f, -.66f, .55f, .55f, .64f, 8);
                foreach (float x in new[] { -.63f, .49f }) s.Box(x, .55f, -.66f, x + .14f, 1.05f, .64f, 8);
                s.Box(-.55f, .55f, .5f, .55f, 1.05f, .66f, 8);
                foreach (float x in new[] { -.77f, .61f }) s.Box(x, .05f, -.3f, x + .16f, .8f, .45f, 12);
                foreach (float x in new[] { -.4f, .26f }) s.Box(x, .44f, -1.85f, x + .14f, .6f, -.6f, 8);
            }
            else if (id == "footbridge")
            {
                for (int i = 0; i < 3; i++) s.Box(-1.5f + i, .15f, -.65f, -.54f + i, .28f, .65f, 9);
                foreach (float z in new[] { -.76f, .62f }) s.Box(-1.44f, .76f, z, 1.44f, .92f, z + .14f, 8);
                foreach (float x in new[] { -1.5f, 1.32f }) foreach (float z in new[] { -.73f, .55f })
                    s.Box(x, 0, z, x + .18f, .76f, z + .18f, 8);
            }
            else if (id == "chopping-block")
            { s.Box(-.38f, 0, -.38f, .38f, .52f, .38f, 8); s.Box(-.4f, .52f, -.4f, .4f, .63f, .4f, 9); }
            else if (id == "oak-door")
            { s.Box(0, 0, -.07f, .9f, 1.66f, .07f, 8); foreach (float y in new[] { .3f, 1.24f }) s.Box(0, y, -.10f, .86f, y + .11f, -.07f, 12); s.Box(.7f, .76f, -.13f, .82f, .91f, -.10f, 12); }
            else throw new ArgumentException("Scenery geometry recipe is missing: " + id);
        }

        sealed class Shape
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uvs = new List<Vector2>();
            readonly List<int> triangles = new List<int>();
            public float Scale = 1;
            public int Count => vertices.Count;
            public void Ring(float outer, float inner, float bottom, float top, int palette)
            {
                Box(-outer, bottom, -outer, outer, top, -inner, palette);
                Box(-outer, bottom, inner, outer, top, outer, palette);
                Box(-outer, bottom, -inner, -inner, top, inner, palette);
                Box(inner, bottom, -inner, outer, top, inner, palette);
            }
            public void Box(float x0, float y0, float z0, float x1, float y1, float z1, int palette)
            {
                if (!(x1 > x0 && y1 > y0 && z1 > z0)) throw new ArgumentException("Coarse boxes must have positive volume.");
                var p = new[] { new Vector3(x0,y0,z0),new Vector3(x1,y0,z0),new Vector3(x1,y1,z0),new Vector3(x0,y1,z0),
                    new Vector3(x0,y0,z1),new Vector3(x1,y0,z1),new Vector3(x1,y1,z1),new Vector3(x0,y1,z1) };
                int[,] faces = { {0,3,2,1}, {4,5,6,7}, {0,4,7,3}, {1,2,6,5}, {3,7,6,2}, {0,1,5,4} };
                var uv = new Vector2((palette % 8 + .5f) / 8f, (palette / 8 + .5f) / 8f);
                for (int face = 0; face < 6; face++)
                {
                    int offset = vertices.Count;
                    for (int corner = 0; corner < 4; corner++) { vertices.Add(p[faces[face, corner]] * Scale); uvs.Add(uv); }
                    triangles.Add(offset); triangles.Add(offset + 1); triangles.Add(offset + 2);
                    triangles.Add(offset); triangles.Add(offset + 2); triangles.Add(offset + 3);
                }
            }
            public Mesh Mesh(string name)
            {
                if (vertices.Count == 0) throw new InvalidOperationException("Scenery recipe produced no geometry.");
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
#endif
