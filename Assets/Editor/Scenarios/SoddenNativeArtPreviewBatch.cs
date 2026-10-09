using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Editor
{
    /// <summary>Disposable native generation, owner census and real-camera art
    /// review. Does not manufacture missing owners, save a scene/world, or make
    /// a successful mesh conversion stand in for whole-zone style coverage.</summary>
    public static class SoddenNativeArtPreviewBatch
    {
        [Serializable] public sealed class OwnerRow
        {
            public string id, blueprint, displayName, glyph, category, selectedModel, recipeFailure, styleModel, styleFailure;
            public int x, y, cropStage = -1, moistureTicks = -1, stylePieces;
            public bool authored, rendered, approvedStyle, batched, transient, hasHarvestState, harvested, hasRepairState, repaired;
        }
        [Serializable] public sealed class OwnerReport
        {
            public string zoneId, coverageMeaning;
            public OwnerRow[] owners;
        }
        [Serializable] public sealed class CaptureRow
        {
            public string image, purpose, focusBlueprint, focusId;
            public int focusX, focusY, width, height;
            public float orthographicSize;
        }
        [Serializable] public sealed class Row
        {
            public string site, zoneId, biome, formation, character, ownerCensus;
            public int seed, visibleOwners, authoredOwners, renderedOwners, approvedOwners, renderedUnapprovedOwners, appliedMeshes, missingMeshes;
            public string[] missingOwnerBlueprints, unrenderedOwnerBlueprints, unapprovedOwnerBlueprints, renderedUnapprovedOwnerBlueprints;
            public CaptureRow[] captures;
            public double generationMilliseconds;
        }
        [Serializable] public sealed class Report
        {
            public string honesty, scope;
            public bool completed;
            public int expectedZoneCount, zoneCount, ownerObservations, unmodeledOwnerObservations, unrenderedOwnerObservations, unapprovedOwnerObservations, renderedUnapprovedOwnerObservations;
            public Row[] rows;
        }
        private readonly struct Focus
        {
            public readonly string Purpose;
            public readonly Entity Owner;
            public Focus(string purpose, Entity owner) { Purpose = purpose; Owner = owner; }
        }

        public static void Run()
        {
            string output = Environment.GetEnvironmentVariable("SODDEN_ART_PREVIEW_OUT");
            if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("SODDEN_ART_PREVIEW_OUT required.");
            Capture(output);
        }

        public static void Capture(string output) => Capture(output, false);

        /// <summary>Evidence-only full region census at seed64. Enumerates the
        /// actual current map's Sodden surfaces plus Sumphold, without injecting
        /// owners or claiming that submitted owner proof is pixel review.</summary>
        public static void CensusAllSurfaces(string output) => Capture(output, true);

        private static void Capture(string output, bool allSurfaces)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sodden art capture requires Edit Mode.");
            if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Output directory required.", nameof(output));
            Directory.CreateDirectory(output);
            using (var borrowed = new ContentScope())
            {
                var factory = new EntityFactory();
                factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                LootTableRegistry.InitializeFromJsonSources(Texts("Content/Data/Loot"));
                LiquidRegistry.InitializeFromJsonSources(Texts("Content/Data/LiquidDefinitions"));
                GasRegistry.InitializeFromJsonSources(Texts("Content/Data/GasDefinitions"));
                MaterialReactionResolver.InitializeFromJsonSources(Texts("Content/Data/MaterialReactions"));
                FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
                LoadoutPart.Factory = ContainerPlacementService.Factory = TraderPart.Factory = LootDropSystem.Factory = factory;
                MaterialReactionResolver.Factory = CorpsePart.Factory = HarvestablePart.Factory = factory;
                NarrativeStatePart.Current = new NarrativeStatePart();
                MessageLog.OnMessage = null; MessageLog.TickProvider = null; TurnManager.World = null; ZoneRenderHooks.Reset();
                Village3DSettings.Enabled = true; Village3DSettings.LowDetail = false;
                var errors = LootTableRegistry.Validate(bp => factory.Blueprints.ContainsKey(bp));
                if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
                var rows = new List<Row>();
                foreach (int seed in allSurfaces ? new[] { 64 } : new[] { 64, 1729, 729490642 })
                {
                    LoadoutPart.Rng = new System.Random(seed); TraderPart.Rng = new System.Random(seed ^ 0x7135);
                    LootDropSystem.Rng = new System.Random(seed ^ 0x2143);
                    var manager = OverworldZoneManager.CreateDetached(factory, seed, true);
                    if (allSurfaces)
                    {
                        var addresses = new List<string>();
                        for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
                            if (manager.WorldMap.GetBiome(x, y) == BiomeType.Sodden)
                                addresses.Add(WorldMap.ToZoneID(x, y, 0));
                        var town = WorldMap.FromZoneID(SumpholdCompositionPlan.ZoneID);
                        var townPOI = manager.WorldMap.GetPOI(town.x, town.y);
                        if (townPOI?.Type != POIType.Village || townPOI.Profile != "Boatyard")
                            throw new InvalidOperationException("The actual Sumphold Boatyard source is unavailable.");
                        if (!addresses.Contains(SumpholdCompositionPlan.ZoneID)) addresses.Add(SumpholdCompositionPlan.ZoneID);
                        const string scope = "seed64-all-current-Sodden-surfaces-plus-Sumphold-owner-census";
                        WriteReceipt(output, rows, scope, addresses.Count);
                        foreach (string id in addresses)
                        {
                            var at = WorldMap.FromZoneID(id); var poi = manager.WorldMap.GetPOI(at.x, at.y);
                            string formation = poi == null ? FormationSelector.For(BiomeType.Sodden, id).ToString() : null;
                            string site = "Surface-" + at.x + "-" + at.y + "-" + seed;
                            rows.Add(CaptureZone(output, site, manager, id, formation, poi?.Profile ?? "ordinary surface", false));
                            WriteReceipt(output, rows, scope, addresses.Count);
                        }
                        continue;
                    }
                    foreach (var formation in new[] { Formation.OpenMire, Formation.PeatCuts, Formation.ReedMaze,
                        Formation.DrownedCopse, Formation.Causeway, Formation.BogFace })
                    {
                        string id = FindFormation(manager, formation);
                        var plan = SoddenCompositionPlan.Create(id, seed);
                        rows.Add(CaptureZone(output, formation + "-" + seed, manager, id, formation.ToString(), plan.Condition));
                        WriteReceipt(output, rows);
                    }
                    // The five authored destinations supplement the eighteen
                    // formation/seed samples without silently multiplying runs.
                    if (seed != 64) continue;
                    foreach (string id in new[] { SoddenDistrictPlan.StopZoneID, SoddenDistrictPlan.CrossingZoneID,
                        SoddenDistrictPlan.WorksZoneID, SumpholdCompositionPlan.ZoneID, DrownedLedgerCompositionPlan.ZoneID })
                    {
                        string site = id == SumpholdCompositionPlan.ZoneID ? "Sumphold"
                            : id == DrownedLedgerCompositionPlan.ZoneID ? "DrownedLedger"
                            : id == SoddenDistrictPlan.StopZoneID ? "DressingShelter"
                            : id == SoddenDistrictPlan.CrossingZoneID ? "CutbankCrossing" : "PeatWorks";
                        rows.Add(CaptureZone(output, site + "-" + seed, manager, id, null, site));
                        WriteReceipt(output, rows);
                    }
                }
            }
        }

        private static string FindFormation(OverworldZoneManager manager, Formation formation)
        {
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (SoddenCompositionPlan.IsWildernessZone(id) && !SoddenDistrictPlan.IsSupportedZone(id)
                    && manager.WorldMap.GetBiome(x, y) == BiomeType.Sodden && manager.WorldMap.GetPOI(x, y) == null
                    && FormationSelector.For(BiomeType.Sodden, id) == formation) return id;
            }
            throw new InvalidOperationException("No actual native Sodden formation found: " + formation);
        }

        private static Row CaptureZone(string output, string site, OverworldZoneManager manager, string id, string formation, string character, bool captureImages = true)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var zone = manager.GetZone(id); timer.Stop();
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject root = null, sourceObject = null; RenderTexture sourceTarget = null;
            var previous = RenderTexture.active;
            try
            {
                root = new GameObject("Temporary Sodden art census"); SceneManager.MoveGameObjectToScene(root, scene);
                sourceObject = new GameObject("Temporary Sodden capture source"); SceneManager.MoveGameObjectToScene(sourceObject, scene);
                var source = sourceObject.AddComponent<Camera>();
                source.enabled = false; source.orthographic = true;
                sourceTarget = new RenderTexture(1600, 600, 24) { hideFlags = HideFlags.DontSave };
                sourceTarget.Create(); source.targetTexture = sourceTarget;
                source.aspect = 1600f / 600; source.orthographicSize = 15.5f;
                source.transform.position = new Vector3(40, 12.5f, -10);
                var presenter = root.AddComponent<SpawnRing3DPresenter>(); presenter.FullReveal = true;
                presenter.Bind(zone, source);
                if (!presenter.IsReady || !presenter.VoxelPresentationActive)
                    throw new InvalidOperationException(site + ": " + (presenter.Failure ?? "Native voxel presenter inactive."));
                presenter.Refresh(null);
                presenter.WorldCamera.scene = scene;
                presenter.WorldCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                var owners = Census(zone, presenter);
                string census = site + "-owners.json";
                File.WriteAllText(Path.Combine(output, census), JsonUtility.ToJson(new OwnerReport
                {
                    zoneId = zone.ZoneID,
                    coverageMeaning = "Every current zone owner with Render.Visible=true, full reveal for review. No actors/items/stages injected. Style approval is the presenter's actual API result, independently recorded from model selection and rendered ownership.",
                    owners = owners
                }, true));
                var captures = new List<CaptureRow>();
                if (captureImages)
                {
                    captures.Add(CaptureImage(output, site + "-overview.png", "overview", null, zone, presenter));
                    foreach (var focus in SelectFocus(zone))
                    {
                        var cell = zone.GetEntityCell(focus.Owner);
                        source.orthographicSize = 5.5f;
                        float halfWidth = source.orthographicSize * source.aspect;
                        source.transform.position = new Vector3(Mathf.Clamp(cell.X + .5f, halfWidth, Zone.Width - halfWidth),
                            Mathf.Clamp(Zone.Height - cell.Y - .5f, 5.5f, Zone.Height - 5.5f), -10);
                        presenter.Refresh(null);
                        captures.Add(CaptureImage(output, site + "-" + focus.Purpose + ".png", focus.Purpose, focus.Owner, zone, presenter));
                    }
                }
                var at = WorldMap.FromZoneID(id);
                return new Row
                {
                    site = site, zoneId = id, biome = manager.WorldMap.GetBiome(at.x, at.y).ToString(), formation = formation,
                    character = character, seed = manager.WorldSeed, generationMilliseconds = timer.Elapsed.TotalMilliseconds,
                    ownerCensus = census, visibleOwners = owners.Length, authoredOwners = owners.Count(o => o.authored),
                    renderedOwners = owners.Count(o => o.rendered), approvedOwners = owners.Count(o => o.approvedStyle),
                    renderedUnapprovedOwners = owners.Count(o => o.authored && o.rendered && !o.approvedStyle),
                    appliedMeshes = presenter.VoxelAppliedMeshCount, missingMeshes = presenter.VoxelMissingMeshCount,
                    missingOwnerBlueprints = owners.Where(o => !o.authored).Select(o => o.blueprint).Distinct().OrderBy(s => s).ToArray(),
                    unapprovedOwnerBlueprints = owners.Where(o => !o.approvedStyle).Select(o => o.blueprint).Distinct().OrderBy(s => s).ToArray(),
                    unrenderedOwnerBlueprints = owners.Where(o => !o.rendered).Select(o => o.blueprint).Distinct().OrderBy(s => s).ToArray(),
                    renderedUnapprovedOwnerBlueprints = owners.Where(o => o.authored && o.rendered && !o.approvedStyle).Select(o => o.blueprint).Distinct().OrderBy(s => s).ToArray(),
                    captures = captures.ToArray()
                };
            }
            finally
            {
                RenderTexture.active = previous;
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (sourceObject != null)
                {
                    sourceObject.GetComponent<Camera>().targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(sourceObject);
                }
                if (sourceTarget != null) { sourceTarget.Release(); UnityEngine.Object.DestroyImmediate(sourceTarget); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static OwnerRow[] Census(Zone zone, SpawnRing3DPresenter presenter)
        {
            var catalog = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var pilot = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
            return VisibleOwners(zone).Select(owner =>
            {
                var cell = zone.GetEntityCell(owner); var render = owner.GetPart<RenderPart>();
                var recipe = SpawnRing3DRecipes.Resolve(zone, owner, catalog, pilot?.Definition);
                bool approved = presenter.TryGetApprovedStyle(owner, out var evidence);
                var crop = owner.GetPart<CropPart>(); var harvest = owner.GetPart<HarvestablePart>(); var repair = owner.GetPart<RepairablePart>();
                return new OwnerRow
                {
                    id = owner.ID, blueprint = owner.BlueprintName, displayName = render.DisplayName, glyph = render.RenderString,
                    x = cell.X, y = cell.Y, selectedModel = recipe.ModelId, recipeFailure = recipe.Failure,
                    category = owner.HasTag("Creature") ? "creature" : owner.HasTag("Item") ? "item" : owner.HasTag("Terrain") ? "terrain" : "scenery-or-effect",
                    authored = presenter.IsAuthoredEntity(owner), rendered = presenter.IsRenderedEntity(owner), approvedStyle = approved,
                    styleModel = evidence.ModelId, styleFailure = evidence.Failure, stylePieces = evidence.PieceCount,
                    batched = recipe.Batched, transient = recipe.Transient, cropStage = crop?.GrowthStage ?? -1,
                    moistureTicks = crop?.MoistureTicks ?? -1, hasHarvestState = harvest != null, harvested = harvest?.Harvested ?? false,
                    hasRepairState = repair != null, repaired = repair?.Repaired ?? false
                };
            }).ToArray();
        }

        private static IEnumerable<Entity> VisibleOwners(Zone zone) => zone.GetReadOnlyEntities()
            .Where(e => e.GetPart<RenderPart>()?.Visible == true && zone.GetEntityCell(e) != null)
            .OrderBy(e => zone.GetEntityCell(e).Y).ThenBy(e => zone.GetEntityCell(e).X).ThenBy(e => e.ID, StringComparer.Ordinal);

        private static IEnumerable<Focus> SelectFocus(Zone zone)
        {
            var owners = VisibleOwners(zone).ToArray(); var selected = new HashSet<Entity>();
            string[] priority = { "SoddenDressingBench", "SoddenWorksLocker", "PreFellingBody", "BoatFrame", "PeatBank", "DeadTree", "Duckboard", "Reeds" };
            Entity primary = priority.Select(bp => owners.FirstOrDefault(e => e.BlueprintName == bp)).FirstOrDefault(e => e != null);
            if (primary != null) { selected.Add(primary); yield return new Focus("primary", primary); }
            var edge = owners.FirstOrDefault(e => e.BlueprintName == "MirePool" && DryNeighbor(zone, zone.GetEntityCell(e)));
            if (edge != null && selected.Add(edge)) yield return new Focus("wet-dry", edge);
            var actor = owners.FirstOrDefault(e => e.HasTag("Creature"));
            if (actor != null && selected.Add(actor)) yield return new Focus("actor", actor);
            var crop = owners.FirstOrDefault(e => e.HasPart<CropPart>());
            if (crop != null && selected.Add(crop)) yield return new Focus("crop", crop);
            var item = owners.FirstOrDefault(e => e.GetPart<PhysicsPart>()?.Takeable == true);
            if (item != null && selected.Add(item)) yield return new Focus("item", item);
        }

        private static bool DryNeighbor(Zone zone, Cell wet)
        {
            foreach (var step in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var next = zone.GetCell(wet.X + step.Item1, wet.Y + step.Item2);
                if (next != null && !next.IsSolid() && !SpawnRing3DRecipes.HasPermanentWater(zone, next.X, next.Y)
                    && !next.Objects.Any(e => e.HasPart<LiquidPoolPart>())) return true;
            }
            return false;
        }

        private static CaptureRow CaptureImage(string output, string name, string purpose, Entity focus, Zone zone, SpawnRing3DPresenter presenter)
        {
            Texture2D image = null; var previous = RenderTexture.active;
            try
            {
                var camera = presenter.WorldCamera; camera.Render(); var target = camera.targetTexture;
                RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false) { hideFlags = HideFlags.DontSave };
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG());
                var cell = focus != null ? zone.GetEntityCell(focus) : null;
                return new CaptureRow { image = name, purpose = purpose, focusBlueprint = focus?.BlueprintName, focusId = focus?.ID,
                    focusX = cell?.X ?? -1, focusY = cell?.Y ?? -1, width = target.width, height = target.height, orthographicSize = camera.orthographicSize };
            }
            finally { RenderTexture.active = previous; if (image != null) UnityEngine.Object.DestroyImmediate(image); }
        }

        private static void WriteReceipt(string output, List<Row> rows, string scope = "formation-and-authored-destination-samples", int expectedZoneCount = 23) => File.WriteAllText(Path.Combine(output, "receipt.json"),
            JsonUtility.ToJson(new Report
            {
                honesty = "Actual fresh native graphs; private generation RNGs; full reveal for art review. Counts are owner observations, not unique blueprints. No gameplay actions, save/load, fog fairness or subjective visual approval are proven by this batch. Unapproved style and absent models remain explicit failures/gaps.",
                scope = scope, expectedZoneCount = expectedZoneCount, completed = rows.Count == expectedZoneCount,
                zoneCount = rows.Count, ownerObservations = rows.Sum(r => r.visibleOwners),
                unmodeledOwnerObservations = rows.Sum(r => r.visibleOwners - r.authoredOwners),
                unrenderedOwnerObservations = rows.Sum(r => r.visibleOwners - r.renderedOwners),
                renderedUnapprovedOwnerObservations = rows.Sum(r => r.renderedUnapprovedOwners),
                unapprovedOwnerObservations = rows.Sum(r => r.visibleOwners - r.approvedOwners), rows = rows.ToArray()
            }, true));

        private static IEnumerable<string> Texts(string path) => Resources.LoadAll<TextAsset>(path)
            .OrderBy(a => a.name, StringComparer.Ordinal).Select(a => a.text);

        // Snapshot exactly the static services this detached capture configures,
        // including mutable registry contents and message queues. Restore on
        // failure as well as success; never write preferences or world saves.
        private sealed class ContentScope : IDisposable
        {
            private readonly List<Action> restore = new List<Action>();
            public ContentScope()
            {
                foreach (var type in new[] { typeof(LootTableRegistry), typeof(LiquidRegistry), typeof(GasRegistry),
                    typeof(MaterialReactionResolver), typeof(FactionManager), typeof(PlayerReputation), typeof(LoadoutPart),
                    typeof(ContainerPlacementService), typeof(TraderPart), typeof(LootDropSystem), typeof(CorpsePart),
                    typeof(HarvestablePart), typeof(NarrativeStatePart), typeof(MessageLog), typeof(Village3DSettings), typeof(ZoneRenderHooks) })
                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.IsLiteral) continue;
                        var value = field.GetValue(null);
                        if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
                        if (value is IDictionary dictionary)
                        {
                            var copy = dictionary.Keys.Cast<object>().Select(key => new DictionaryEntry(key, dictionary[key])).ToArray();
                            restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
                        }
                        else if (value is IList list && !list.IsReadOnly && !list.IsFixedSize)
                        {
                            var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); });
                        }
                        else if (value is HashSet<string> set)
                        { var copy = set.ToArray(); restore.Add(() => { set.Clear(); foreach (var item in copy) set.Add(item); }); }
                        else if (value is Queue<string> queue)
                        { var copy = queue.ToArray(); restore.Add(() => { queue.Clear(); foreach (var item in copy) queue.Enqueue(item); }); }
                    }
                var world = TurnManager.World; restore.Add(() => TurnManager.World = world);
            }
            public void Dispose() { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); }
        }
    }
}
