// execute_code method body; C# 6 compatible. Edit Mode only.
// Writes only /tmp/coo-live16x24-connected-census.json; does not save assets/scenes.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
    throw new System.InvalidOperationException("Run only after Play, compilation and asset refresh have finished.");
var output = "/tmp/coo-live16x24-connected-census.json";
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var restores = new System.Collections.Generic.List<System.Action>();
var restorationErrors = new System.Collections.Generic.List<string>();
var seedRows = new System.Collections.Generic.List<object>();
var watch = System.Diagnostics.Stopwatch.StartNew();
var oldWorld = CavesOfOoo.Core.TurnManager.World;
var oldSettlement = CavesOfOoo.Core.SettlementManager.Current;
var oldRoot = CavesOfOoo.Core.SaveGameService.SaveRootOverride;
var oldEquipmentVersion = CavesOfOoo.Core.EquipmentChangeBus.GlobalVersion;
string failure = null;

// Restore both the exact static references and mutable contents. This includes
// MessageLog's private Queue<AnnouncementOrigin>, preserving popup metadata,
// and Diag's ring-array contents rather than only its counters.
var scopeTypes = new[] {
    typeof(CavesOfOoo.Data.LootTableRegistry), typeof(CavesOfOoo.Core.LiquidRegistry), typeof(CavesOfOoo.Core.GasRegistry),
    typeof(CavesOfOoo.Core.MaterialReactionResolver), typeof(CavesOfOoo.Core.FactionManager), typeof(CavesOfOoo.Core.PlayerReputation),
    typeof(CavesOfOoo.Core.LoadoutPart), typeof(CavesOfOoo.Core.ContainerPlacementService), typeof(CavesOfOoo.Core.TraderPart),
    typeof(CavesOfOoo.Core.LootDropSystem), typeof(CavesOfOoo.Core.CorpsePart), typeof(CavesOfOoo.Core.HarvestablePart),
    typeof(CavesOfOoo.Core.NarrativeStatePart), typeof(CavesOfOoo.Core.MessageLog), typeof(CavesOfOoo.Core.ZoneRenderHooks),
    typeof(CavesOfOoo.Core.EntityVisualHooks), typeof(CavesOfOoo.Core.WorldAudioHooks), typeof(CavesOfOoo.Core.EquipmentChangeBus),
    typeof(CavesOfOoo.Core.TurnManager), typeof(CavesOfOoo.Diagnostics.Diag), typeof(CavesOfOoo.Core.RepairRecipeRegistry)
};
foreach (var type in scopeTypes)
foreach (var field in type.GetFields(flags))
{
    if (field.IsLiteral) continue;
    var value = field.GetValue(null);
    if (!field.IsInitOnly) { var f = field; var v = value; restores.Add(() => f.SetValue(null, v)); }
    var array = value as System.Array;
    var dict = value as System.Collections.IDictionary;
    var list = value as System.Collections.IList;
    if (array != null) { var copy = (System.Array)array.Clone(); restores.Add(() => System.Array.Copy(copy, array, copy.Length)); }
    else if (dict != null)
    {
        var copy = new System.Collections.Generic.List<System.Collections.DictionaryEntry>();
        foreach (System.Collections.DictionaryEntry entry in dict) copy.Add(entry);
        restores.Add(() => { dict.Clear(); foreach (var entry in copy) dict.Add(entry.Key, entry.Value); });
    }
    else if (list != null && !list.IsReadOnly && !list.IsFixedSize)
    {
        var copy = new System.Collections.Generic.List<object>(); foreach (var entry in list) copy.Add(entry);
        restores.Add(() => { list.Clear(); foreach (var entry in copy) list.Add(entry); });
    }
    else if (value != null && value.GetType().IsGenericType)
    {
        var generic = value.GetType().GetGenericTypeDefinition();
        bool queue = generic == typeof(System.Collections.Generic.Queue<>);
        bool set = generic == typeof(System.Collections.Generic.HashSet<>);
        if (queue || set)
        {
            var copy = new System.Collections.Generic.List<object>(); foreach (var entry in (System.Collections.IEnumerable)value) copy.Add(entry);
            var clear = value.GetType().GetMethod("Clear");
            var add = value.GetType().GetMethod(queue ? "Enqueue" : "Add");
            restores.Add(() => { clear.Invoke(value, null); foreach (var entry in copy) add.Invoke(value, new[] { entry }); });
        }
    }
}

System.Func<string, string[]> texts = path => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
    System.Linq.Enumerable.OrderBy(UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>(path), a => a.name, System.StringComparer.Ordinal), a => a.text));
System.Func<object, object> scalarFields = obj => {
    if (obj == null) return null;
    var row = new System.Collections.Generic.SortedDictionary<string, object>(System.StringComparer.Ordinal);
    foreach (var field in obj.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
    {
        var type = field.FieldType;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        { var v = field.GetValue(obj); row[field.Name] = type.IsEnum && v != null ? v.ToString() : v; }
    }
    return row;
};
System.Func<CavesOfOoo.Core.Entity, string> roles = owner => {
    var values = new System.Collections.Generic.List<string>();
    foreach (var p in owner.Properties)
        if (p.Key.EndsWith(".Role", System.StringComparison.Ordinal) || p.Key == "ReferenceGlade.Role") values.Add(p.Key + "=" + p.Value);
    values.Sort(System.StringComparer.Ordinal); return string.Join(";", values.ToArray());
};
var spatialZoneField = typeof(CavesOfOoo.Core.Entity).GetField("SpatialZone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
var stateParts = new System.Collections.Generic.HashSet<string>(new[] {
    "Crop", "CultivatedSoil", "Harvestable", "Container", "Repairable", "Composition", "Material", "Door", "Thermal", "Combustible",
    "TorchLight", "LightSource", "Campfire", "LiquidPool", "WaterSource", "Waterskin", "LiquidVessel", "KitchenBatch", "LocalGatheringClaim",
    "CookIntroduction", "ConnectedClayOrigin", "ConnectedSpreadSource", "FieldMedicine", "TacticalSupply", "Grimoire", "GrimoireCharge",
    "Armor", "MeleeWeapon", "Handling", "Trader", "Brain", "MeleeTraumaProfile", "Destructible", "StairsUp", "StairsDown"
}, System.StringComparer.Ordinal);
System.Func<CavesOfOoo.Core.Entity, object> describe = owner => {
    var render = owner.GetPart<CavesOfOoo.Core.RenderPart>(); var physics = owner.GetPart<CavesOfOoo.Core.PhysicsPart>();
    var states = new System.Collections.Generic.List<object>();
    foreach (var part in owner.Parts)
        if (stateParts.Contains(part.Name)) states.Add(new { part = part.Name, owned = object.ReferenceEquals(part.ParentEntity, owner), fields = scalarFields(part) });
    return new { id = owner.ID, blueprint = owner.BlueprintName, displayName = render == null ? null : render.DisplayName,
        visualID = render == null ? null : render.VisualID, role = roles(owner), properties = new System.Collections.Generic.Dictionary<string,string>(owner.Properties),
        renderOwned = render != null && object.ReferenceEquals(render.ParentEntity, owner), renderVisible = render != null && render.Visible,
        glyph = render == null ? null : render.RenderString, color = render == null ? null : render.ColorString, layer = render == null ? -1 : render.RenderLayer,
        physicalOwned = physics != null && object.ReferenceEquals(physics.ParentEntity, owner), solid = physics != null && physics.Solid,
        takeable = physics != null && physics.Takeable, creature = owner.HasTag("Creature"), terrain = owner.HasTag("Terrain"), plantable = owner.HasTag("Plantable"),
        units = owner.GetPart<CavesOfOoo.Core.StackerPart>() == null ? 1 : owner.GetPart<CavesOfOoo.Core.StackerPart>().StackCount,
        parts = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(owner.Parts, p => p.Name)), state = states };
};
try
{
    // No live subscribers, world, narrative, stock factories or RNG are borrowed.
    CavesOfOoo.Core.ZoneRenderHooks.Reset(); CavesOfOoo.Core.EntityVisualHooks.Reset(); CavesOfOoo.Core.WorldAudioHooks.Callback = null;
    CavesOfOoo.Core.MessageLog.OnMessage = null; CavesOfOoo.Core.MessageLog.TickProvider = null;
    CavesOfOoo.Core.TurnManager.World = null;
    typeof(CavesOfOoo.Core.TurnManager).GetProperty("Active", flags).SetValue(null, null, null);
    CavesOfOoo.Diagnostics.Diag.SetChannel("worldgen", true);
    CavesOfOoo.Data.LootTableRegistry.InitializeFromJsonSources(texts("Content/Data/Loot"));
    CavesOfOoo.Core.LiquidRegistry.InitializeFromJsonSources(texts("Content/Data/LiquidDefinitions"));
    CavesOfOoo.Core.GasRegistry.InitializeFromJsonSources(texts("Content/Data/GasDefinitions"));
    CavesOfOoo.Core.MaterialReactionResolver.InitializeFromJsonSources(texts("Content/Data/MaterialReactions"));
    CavesOfOoo.Core.FactionManager.Initialize(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Data/Factions").text);
    var ids = new[] { "Overworld.11.10.0", "Overworld.11.10.1", "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0" };
    foreach (int seed in new[] { 64, 65 })
    {
        var factory = new CavesOfOoo.Data.EntityFactory(); factory.LoadBlueprints(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Blueprints/Objects").text);
        var validation = CavesOfOoo.Data.LootTableRegistry.Validate(bp => factory.Blueprints.ContainsKey(bp));
        if (validation.Count != 0) throw new System.InvalidOperationException(string.Join("; ", validation.ToArray()));
        CavesOfOoo.Core.LoadoutPart.Factory = CavesOfOoo.Core.TraderPart.Factory = CavesOfOoo.Core.LootDropSystem.Factory = factory;
        CavesOfOoo.Core.ContainerPlacementService.Factory = CavesOfOoo.Core.HarvestablePart.Factory = CavesOfOoo.Core.CorpsePart.Factory = factory;
        CavesOfOoo.Core.MaterialReactionResolver.Factory = factory;
        CavesOfOoo.Core.LoadoutPart.Rng = new System.Random(seed); CavesOfOoo.Core.TraderPart.Rng = new System.Random(seed ^ 0x7135);
        CavesOfOoo.Core.LootDropSystem.Rng = new System.Random(seed ^ 0x2143);
        CavesOfOoo.Core.NarrativeStatePart.Current = new CavesOfOoo.Core.NarrativeStatePart();
        var manager = CavesOfOoo.Core.OverworldZoneManager.CreateDetached(factory, seed, true);
        var zoneRows = new System.Collections.Generic.List<object>();
        foreach (string id in ids)
        {
            var before = new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Select(CavesOfOoo.Diagnostics.Diag.Snapshot(8192), d => d.TraceId));
            var timer = System.Diagnostics.Stopwatch.StartNew(); var zone = manager.GetZone(id); timer.Stop();
            var address = CavesOfOoo.Core.WorldMap.FromZoneID(id); var poi = manager.WorldMap.GetPOI(address.x, address.y);
            var entry = manager.Exploration.Find(id);
            var diagnostics = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(CavesOfOoo.Diagnostics.Diag.Snapshot(8192), d => d.Category == "worldgen" && !before.Contains(d.TraceId)),
                d => new { kind = d.Kind, payload = d.PayloadJson }));
            if (zone == null) { zoneRows.Add(new { zoneID = id, generated = false, generationMs = timer.Elapsed.TotalMilliseconds, diagnostics = diagnostics }); continue; }
            var owners = System.Linq.Enumerable.ToArray(zone.GetReadOnlyEntities());
            var detailed = new System.Collections.Generic.List<object>();
            var goods = new System.Collections.Generic.List<object>();
            foreach (var owner in owners)
            {
                var at = zone.GetEntityCell(owner);
                bool plainTerrain = (owner.BlueprintName == "Grass" || owner.BlueprintName == "Floor" || owner.BlueprintName == "StoneWall" || owner.BlueprintName == "RoadStone")
                    && roles(owner).Length == 0 && !owner.HasPart<CavesOfOoo.Core.CultivatedSoilPart>();
                if (!plainTerrain) detailed.Add(new { x = at == null ? -1 : at.X, y = at == null ? -1 : at.Y, owner = describe(owner) });
                var seen = new System.Collections.Generic.HashSet<CavesOfOoo.Core.Entity>();
                System.Action<CavesOfOoo.Core.Entity, string, int> descend = null;
                descend = (parent, path, depth) => {
                    if (depth > 4) return;
                    var lists = new System.Collections.Generic.List<System.Tuple<string,System.Collections.Generic.IEnumerable<CavesOfOoo.Core.Entity>>>();
                    var pack = parent.GetPart<CavesOfOoo.Core.InventoryPart>(); var box = parent.GetPart<CavesOfOoo.Core.ContainerPart>();
                    if (pack != null && object.ReferenceEquals(pack.ParentEntity, parent)) {
                        lists.Add(System.Tuple.Create("carried", (System.Collections.Generic.IEnumerable<CavesOfOoo.Core.Entity>)pack.Objects));
                        lists.Add(System.Tuple.Create("equipped", (System.Collections.Generic.IEnumerable<CavesOfOoo.Core.Entity>)pack.EquippedItems.Values)); }
                    if (box != null && object.ReferenceEquals(box.ParentEntity, parent)) lists.Add(System.Tuple.Create("container", (System.Collections.Generic.IEnumerable<CavesOfOoo.Core.Entity>)box.Contents));
                    foreach (var list in lists) foreach (var child in list.Item2)
                    {
                        if (child == null || !seen.Add(child)) continue;
                        var p = child.GetPart<CavesOfOoo.Core.PhysicsPart>();
                        var childPath = path + "/" + list.Item1;
                        goods.Add(new { worldOwnerID = owner.ID, worldOwnerBlueprint = owner.BlueprintName, worldOwnerRole = roles(owner),
                            x = at == null ? -1 : at.X, y = at == null ? -1 : at.Y, parentID = parent.ID, custody = childPath,
                            physicallyHeldByParent = p != null && object.ReferenceEquals(p.ParentEntity, child) && spatialZoneField.GetValue(child) == null &&
                                (list.Item1 == "equipped" ? object.ReferenceEquals(p.Equipped, parent) : object.ReferenceEquals(p.InInventory, parent)), item = describe(child) });
                        descend(child, childPath, depth + 1);
                    }
                };
                descend(owner, "", 0);
            }
            var groups = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
                System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.GroupBy(owners, e => e.BlueprintName + "|" + (e.GetPart<CavesOfOoo.Core.RenderPart>() == null ? "" : e.GetPart<CavesOfOoo.Core.RenderPart>().VisualID) + "|" + roles(e)), g => g.Key, System.StringComparer.Ordinal),
                g => new { identity = g.Key, owners = g.Count(), units = g.Sum(e => e.GetPart<CavesOfOoo.Core.StackerPart>() == null ? 1 : e.GetPart<CavesOfOoo.Core.StackerPart>().StackCount) }));
            var selected = new System.Collections.Generic.List<CavesOfOoo.Core.Entity>(); var terrainMap = new System.Collections.Generic.List<string>();
            int visibleCells = 0, exploredCells = 0, blockedCells = 0;
            for (int y = 0; y < CavesOfOoo.Core.Zone.Height; y++) {
                var line = new System.Text.StringBuilder();
                for (int x = 0; x < CavesOfOoo.Core.Zone.Width; x++) {
                    var cell = zone.GetCell(x, y); if (cell.IsVisible) visibleCells++; if (cell.Explored) exploredCells++; if (cell.BlocksMovement()) blockedCells++;
                    var top = cell.GetTopVisibleObject(); if (top != null) selected.Add(top);
                    bool wall = cell.Objects.Any(e => e.BlueprintName == "StoneWall"); bool floor = cell.Objects.Any(e => e.BlueprintName == "Floor");
                    bool bed = cell.Objects.Any(e => e.HasPart<CavesOfOoo.Core.CultivatedSoilPart>());
                    line.Append(bed ? ':' : wall ? '#' : floor ? '.' : ' ');
                } terrainMap.Add(line.ToString());
            }
            var topCounts = selected.GroupBy(e => e.BlueprintName + "|" + (e.GetPart<CavesOfOoo.Core.RenderPart>() == null ? "" : e.GetPart<CavesOfOoo.Core.RenderPart>().VisualID) + "|" + roles(e))
                .OrderBy(g => g.Key, System.StringComparer.Ordinal).Select(g => new { identity = g.Key, cells = g.Count(), distinctOwners = g.Distinct().Count() }).ToArray();
            zoneRows.Add(new { zoneID = id, generated = true, generationMs = timer.Elapsed.TotalMilliseconds, width = CavesOfOoo.Core.Zone.Width, height = CavesOfOoo.Core.Zone.Height,
                biome = manager.WorldMap.GetBiome(address.x, address.y).ToString(), poi = scalarFields(poi),
                attachedToThisManager = object.ReferenceEquals(CavesOfOoo.Core.WorldLocationContext.For(zone), manager), cachedExactGraph = object.ReferenceEquals(manager.CachedZones[id], zone),
                referenceGladeScope = CavesOfOoo.Core.ReferenceGladePlan.IsActive(zone), broadSpreadScope = CavesOfOoo.Core.SpreadPresentationScope.IsActive(zone),
                currentFlatScope = CavesOfOoo.Rendering.FlatPixelGreyboxCatalog.IsAdmitted(zone),
                explorationFamily = entry == null ? null : entry.Family.ToString(), explorationTopology = entry == null ? null : entry.Topology.ToString(),
                placementEligible = entry != null && entry.PlacementEligible, disposition = manager.Exploration.DispositionFor(id),
                entityCount = zone.EntityCount, visibleCells = visibleCells, exploredCells = exploredCells, blockedCells = blockedCells,
                placedCounts = groups, potentialTopCountsIgnoringFog = topCounts, detailedOwners = detailed, heldGoods = goods,
                cellarTerrainRows = id == "Overworld.11.10.1" ? terrainMap : null, diagnostics = diagnostics });
        }
        seedRows.Add(new { seed = seed, explorationEnabled = manager.Exploration.Enabled, explorationVersion = manager.Exploration.Version,
            actualGeneratedOrder = ids, cachedZones = manager.CachedZones.Keys.OrderBy(x => x, System.StringComparer.Ordinal).ToArray(),
            activeZoneRemainedNull = manager.ActiveZone == null, zones = zoneRows,
            connections = manager.GetConnectionSnapshot().SelectMany(p => p.Value).GroupBy(c => c.SourceZoneID + ":" + c.SourceX + "," + c.SourceY + "->" + c.TargetZoneID + ":" + c.TargetX + "," + c.TargetY + ":" + c.Type)
                .Select(g => g.First()).OrderBy(c => c.SourceZoneID).Select(c => scalarFields(c)).ToArray() });
    }
}
catch (System.Exception error) { failure = error.ToString(); }
finally {
    for (int i = restores.Count - 1; i >= 0; i--) try { restores[i](); } catch (System.Exception error) { restorationErrors.Add(error.ToString()); }
}
watch.Stop();
var report = new { schema = 1, observedUtc = System.DateTime.UtcNow.ToString("o"), runtime = System.Environment.Version.ToString(), unity = UnityEngine.Application.unityVersion,
    seeds = seedRows, elapsedMs = watch.Elapsed.TotalMilliseconds, failure = failure, restorationErrors = restorationErrors,
    cleanup = new { worldRestored = object.ReferenceEquals(oldWorld, CavesOfOoo.Core.TurnManager.World), settlementUnchanged = object.ReferenceEquals(oldSettlement, CavesOfOoo.Core.SettlementManager.Current),
        saveRootUnchanged = oldRoot == CavesOfOoo.Core.SaveGameService.SaveRootOverride, equipmentVersionRestored = oldEquipmentVersion == CavesOfOoo.Core.EquipmentChangeBus.GlobalVersion },
    bounds = new[] { "One-shot detached generation only; no player, no live graph mutation, no Play/input/render/capture, no gameplay actions or saves.",
        "Exact zone order and private seeded loadout/trader/death RNGs are explicit; not a claim of arbitrary live-session global RNG replay.",
        "Placed counts include actual terrain and hidden entities as authoring diagnostics. Potential top counts ignore fog deliberately; do not interpret them as currently player-visible owners.",
        "Held goods include carried/equipped/container state separately; a held item is not a decorative world spawn. Unique child references are counted once per root, including multi-slot equipment.",
        "Cellar terrain rows: # StoneWall, . Floor, : actual CultivatedSoil owner, space other. Detailed nonplain owners supply exact coordinates and owned-part scalar state.",
        "No catalog textures or live renderer are constructed. CurrentFlatScope is today's admission only, not a new six-zone integration or native art coverage claim." } };
System.IO.File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return new { path = output, seedsCompleted = seedRows.Count, elapsedMs = watch.Elapsed.TotalMilliseconds, failure = failure, restorationErrors = restorationErrors, cleanup = report.cleanup };
