// SOURCE-ONLY DRAFT. Root must review/adopt/compile before invoking in a dedicated batch Editor.
// No scene, player, Play Mode, SaveGameService, preference or save-root access.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace CavesOfOoo.Editor
{
    public static class Village3DRingExporter
    {
        public const int Seed = 729490642;
        private const string Centre = "Overworld.3.6.0";
        private static readonly int[,] Coords = { {3,6}, {2,5}, {3,5}, {4,5}, {2,6}, {4,6}, {2,7}, {3,7}, {4,7} };
        private static readonly string[] DirectionNames = { "centre", "NW", "N", "NE", "W", "E", "SW", "S", "SE" };
        private static readonly int[] ExpectedTiers = { 3,4,5,4,3,4,3,3,3 };
        private static readonly string[] ExpectedFormations = { "AuthoredMorrowfast", "CascadeGorge", "AuthoredFellingSite", "ButtressRidge", "CompostingField", "Grove", "Grove", "TendrilFen", "Grove" };

        // The Editor assembly cannot call internal APIs; this is the actual read-only resolver,
        // not a reimplementation whose arrival order could diverge from gameplay.
        private static readonly MethodInfo Arrival = typeof(ZoneTransitionSystem).GetMethod("FindPassableCell",
            BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(Zone), typeof(int), typeof(int), typeof(TransitionDirection) }, null);

        /// <summary>Invoke with -batchmode -executeMethod CavesOfOoo.Editor.Village3DRingExporter.RunFromCommandLine.
        /// Optional -ringExportPath /absolute/external/path. Runs in Edit Mode and exits its owned process.</summary>
        public static void RunFromCommandLine()
        {
            if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Ring export requires a dedicated batch Editor in Edit Mode; never run inside the user's live session.");
            string output = Argument("-ringExportPath") ?? "/tmp/codex-v3d-ring-native-729490642";
            output = Path.GetFullPath(output);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (output == project || output.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Export output must be outside the Unity project.");
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new IOException("Refusing to overwrite an existing nonempty export directory: " + output);
            Directory.CreateDirectory(output);
            var errors = new List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack); };
            Application.logMessageReceived += onLog;
            try
            {
                var report = Export(output);
                report.unexpectedErrors = errors.ToArray();
                report.success = errors.Count == 0;
                File.WriteAllText(Path.Combine(output, "ring-index.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
                if (!report.success) throw new InvalidOperationException("Native exporter logged errors; inspect ring-index.json.");
                Debug.Log("[Village3DRing] Exported nine native zones to " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(output, "FAILED.txt"), e + "\n\n" + string.Join("\n", errors));
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
            finally { Application.logMessageReceived -= onLog; }
        }

        private static Report Export(string output)
        {
            Require(Arrival != null, "Production arrival resolver signature changed.");
            var factory = InitializeShippedContent();
            // Explicit fresh-world narrative flags, matching ordinary new-game semantics.
            NarrativeStatePart.Current = new NarrativeStatePart();
            var manager = new InspectableManager(factory, Seed);
            var report = new Report
            {
                seed = Seed, unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                runtime = Environment.Version.ToString(), generatedUtc = DateTime.UtcNow.ToString("O"),
                coordinateConvention = "80x25; x east, y south; 3D=(x+.5,height,24.5-y)",
                stage = "Fresh manager generation, before any player/turn/FOV; centre includes production fresh-start garden helper.",
                honesty = "Source-derived collector, not gameplay input proof. Private production arrival resolver is queried without moving actors. IDs preserved including null. Public part fields only; bounded complex-field expansion reports opaque/truncated values. No player saves created. Default Loadout/Trader RNG remains unseeded as in normal factory spawning. Elevation is band/formation metadata, not a native per-cell heightmap.",
                worldFlags = new[] { "UrquActive=0", "EcologyDamaged=0" },
                sources = SourceHashes()
            };
            File.WriteAllText(Path.Combine(output, "Morrowfast-native-definition.json"), RequiredResource("SceneArt/Morrowfast/definition").text);
            File.WriteAllText(Path.Combine(output, "Felling-native-definition.json"), RequiredResource("SceneArt/FellingSite/definition").text);
            var zones = new Dictionary<string, Zone>(StringComparer.Ordinal);
            for (int i = 0; i < Coords.GetLength(0); i++)
            {
                int wx = Coords[i,0], wy = Coords[i,1];
                string id = WorldMap.ToZoneID(wx, wy, 0);
                var parsed = WorldMap.FromZoneID(id);
                Require(parsed.x == wx && parsed.y == wy && parsed.z == 0, "Wrong world coordinate mapping: " + id);
                var biome = manager.WorldMap.GetBiome(wx, wy);
                Require(biome == (wy == 5 ? BiomeType.Stump : BiomeType.Grovelands), "Biome changed: " + id);
                Require(WorldMapAuthoring.TierAt(wx, wy) == ExpectedTiers[i], "Tier changed: " + id);
                if (i != 0) Require(!WorldMapAuthoring.IsRoad(wx, wy) && !WorldMapAuthoring.IsRiver(wx, wy), "Road/river contract changed: " + id);
                var poi = manager.WorldMap.GetPOI(wx, wy);
                ValidatePOI(i, poi);
                var zone = manager.GetZone(id);
                Require(zone != null && zone.ZoneID == id, "Generation failed or returned wrong zone: " + id);
                if (i == 0) PrepareRealStartingGarden(zone);
                zones.Add(id, zone);
                var dump = new Collector(zone).Dump();
                dump.wx = wx; dump.wy = wy; dump.depth = 0; dump.direction = DirectionNames[i];
                dump.biome = biome.ToString(); dump.tier = WorldMapAuthoring.TierAt(wx, wy);
                dump.road = WorldMapAuthoring.IsRoad(wx, wy); dump.river = WorldMapAuthoring.IsRiver(wx, wy);
                dump.elevationBand = StumpBands.BandAt(wx, wy).ToString();
                dump.formation = i == 0 || i == 2 ? ExpectedFormations[i] : biome == BiomeType.Stump
                    ? FormationSelector.ForStump(StumpBands.BandAt(wx, wy), id).ToString() : FormationSelector.For(biome, id).ToString();
                Require(dump.formation == ExpectedFormations[i], "Formation changed: " + id + " => " + dump.formation);
                dump.poi = poi == null ? null : new PoiRow { type = poi.Type.ToString(), name = poi.Name, profile = poi.Profile, faction = poi.Faction, tier = poi.Tier };
                dump.pipeline = manager.Inspect(id).Builders.Select(b => new BuilderRow { name = b.Name, priority = b.Priority }).ToArray();
                dump.connections = manager.GetConnections(id).Select(Connection).ToArray();
                dump.authoredDefinitionFile = i == 0 ? "Morrowfast-native-definition.json" : i == 2 ? "Felling-native-definition.json" : null;
                string json = JsonConvert.SerializeObject(dump, Formatting.Indented);
                string file = id + ".json";
                File.WriteAllText(Path.Combine(output, file), json);
                report.zones.Add(new ZoneSummary { id = id, file = file, sha256 = Hash(json), layoutSha256 = LayoutHash(dump), cells = dump.cells.Count,
                    onGroundEntities = zone.EntityCount, reachableEntityGraph = dump.entities.Count, blueprintCounts = dump.entities.GroupBy(e => e.blueprint ?? "<null>")
                        .OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new CountRow { key = g.Key, count = g.Count() }).ToArray() });
            }
            Require(zones.Count == 9 && manager.CachedZones.Count == 9, "Unexpected extra zones generated; inspect parent/floor routing before accepting export.");
            foreach (var entry in zones)
            {
                var p = WorldMap.FromZoneID(entry.Key);
                PairIfPresent(report, zones, entry.Value, WorldMap.ToZoneID(p.x + 1, p.y, 0), TransitionDirection.East);
                PairIfPresent(report, zones, entry.Value, WorldMap.ToZoneID(p.x, p.y + 1, 0), TransitionDirection.South);
            }
            Require(report.borders.Count == 12, "Expected twelve unique cardinal shared borders.");
            // This is an observation, not an assertion that all generated neighbours connect perfectly.
            report.allBordersHaveViableBothDirections = report.borders.All(b => b.forward.Any(a => a.sourceWalkable && a.arrivalExists && a.arrivalWalkable)
                && b.reverse.Any(a => a.sourceWalkable && a.arrivalExists && a.arrivalWalkable));
            report.connections = manager.GetConnectionSnapshot().OrderBy(k => k.Key, StringComparer.Ordinal)
                .SelectMany(k => k.Value).GroupBy(c => c.SourceZoneID + "|" + c.SourceX + "," + c.SourceY + "|" + c.TargetZoneID + "|" + c.TargetX + "," + c.TargetY + "|" + c.Type)
                .Select(g => Connection(g.First())).ToArray();
            return report;
        }

        private static EntityFactory InitializeShippedContent()
        {
            FactionManager.Initialize(RequiredResource("Content/Data/Factions").text);
            MaterialReactionResolver.InitializeFromJsonSources(ResourceTexts("Content/Data/MaterialReactions"));
            LiquidRegistry.InitializeFromJsonSources(ResourceTexts("Content/Data/LiquidDefinitions"));
            GasRegistry.InitializeFromJsonSources(ResourceTexts("Content/Data/GasDefinitions"));
            HouseDramaLoader.LoadAll(); foreach (var drama in HouseDramaLoader.GetAll()) HouseDramaRuntime.RegisterDrama(drama);
            CavesOfOoo.Storylets.StoryletRegistry.LoadAll(); ConversationLoader.LoadAll();
            var factory = new EntityFactory(); factory.LoadBlueprints(RequiredResource("Content/Blueprints/Objects").text);
            // Exact normal bootstrap factory consumers. No ObjectCreated hooks are bypassed.
            ConversationActions.Factory = factory; MaterialReactionResolver.Factory = factory; CorpsePart.Factory = factory;
            LoadoutPart.Factory = factory; LootDropSystem.Factory = factory; ContainerPlacementService.Factory = factory;
            TraderPart.Factory = factory; DestructionSystem.EntityFactoryRef = factory;
            CavesOfOoo.Skills.Cryomancy_GlacialWall.Factory = factory; LayRuneGoal.Factory = factory;
            PricklebrowNestPart.Factory = factory; AlchemyStillPart.Factory = factory; ForgePart.Factory = factory;
            SeedPart.Factory = factory; CropSystem.Factory = factory; HarvestablePart.Factory = factory; TraderRestockSystem.Factory = factory;
            LoadoutPart.Rng = null; TraderPart.Rng = null;
            ResonanceSystem.EnsureInitialized(); TileReactionSystem.EnsureInitialized();
            LootTableRegistry.InitializeFromJsonSources(ResourceTexts("Content/Data/Loot"));
            var errors = LootTableRegistry.Validate(bp => factory.Blueprints.ContainsKey(bp));
            Require(errors.Count == 0, "Invalid shipped loot references: " + string.Join("; ", errors));
            return factory;
        }

        private static void PrepareRealStartingGarden(Zone zone)
        {
            var type = typeof(Zone).Assembly.GetType("CavesOfOoo.Core.MorrowfastStartingGarden", true);
            var method = type.GetMethod("Ensure", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Zone) }, null);
            Require(method != null, "Fresh-start garden production helper not adopted yet.");
            Require((int)method.Invoke(null, new object[] { zone }) == 6, "Production fresh-start garden rejected centre.");
        }
        private static void ValidatePOI(int i, PointOfInterest poi)
        {
            if (i == 0) Require(poi != null && poi.Type == POIType.Village && MorrowfastSceneRuntime.ZoneID == Centre, "Wrong centre POI.");
            else if (i == 2) Require(poi != null && poi.Type == POIType.FellingSite, "Missing Felling POI.");
            else if (i == 5) Require(poi != null && poi.Type == POIType.Sinkhole && poi.Name == "Olderdeep" && poi.Profile == SinkholeSites.FoundingVillageProfile, "Wrong Olderdeep POI.");
            else if (i == 6) Require(poi != null && poi.Type == POIType.Sinkhole && poi.Name == "Ginmere" && SinkholeArchetypes.ForSite(poi) == SinkholeArchetype.DrownedSima, "Wrong Ginmere POI.");
            else Require(poi == null, "Unexpected POI at ring index " + i);
        }
        private sealed class InspectableManager : OverworldZoneManager
        { public InspectableManager(EntityFactory f, int seed) : base(f, seed) { } public ZoneGenerationPipeline Inspect(string id) => base.GetPipelineForZone(id); }

        private static void PairIfPresent(Report report, Dictionary<string, Zone> zones, Zone source, string targetID, TransitionDirection direction)
        {
            if (!zones.TryGetValue(targetID, out var target)) return;
            var reverse = direction == TransitionDirection.East ? TransitionDirection.West : TransitionDirection.North;
            report.borders.Add(new BorderRow { from = source.ZoneID, to = targetID, direction = direction.ToString(),
                forward = Arrivals(source, target, direction), reverse = Arrivals(target, source, reverse) });
        }
        private static ArrivalRow[] Arrivals(Zone source, Zone target, TransitionDirection direction)
        {
            int count = direction == TransitionDirection.East || direction == TransitionDirection.West ? Zone.Height : Zone.Width;
            var rows = new ArrivalRow[count];
            for (int i = 0; i < count; i++)
            {
                int x = direction == TransitionDirection.East ? Zone.Width - 1 : direction == TransitionDirection.West ? 0 : i;
                int y = direction == TransitionDirection.South ? Zone.Height - 1 : direction == TransitionDirection.North ? 0 : i;
                var ideal = ZoneTransitionSystem.GetArrivalPosition(direction, x, y);
                var actual = ((int x, int y))Arrival.Invoke(null, new object[] { target, ideal.x, ideal.y, direction });
                var sourceCell = source.GetCell(x, y); var targetCell = target.GetCell(actual.x, actual.y);
                rows[i] = new ArrivalRow { sourceX = x, sourceY = y, sourceWalkable = !sourceCell.BlocksMovement(),
                    idealX = ideal.x, idealY = ideal.y, arrivalX = actual.x, arrivalY = actual.y,
                    arrivalExists = targetCell != null, arrivalWalkable = targetCell != null && !targetCell.BlocksMovement(),
                    relocated = actual.x != ideal.x || actual.y != ideal.y,
                    arrivalExcluded = targetCell != null && targetCell.HasObjectWithTag("ExcludeZoneArrival") };
            }
            return rows;
        }

        private sealed class Collector
        {
            private readonly Zone zone;
            private readonly Dictionary<Entity, string> tokens = new Dictionary<Entity, string>();
            private readonly List<Entity> queue = new List<Entity>();
            public Collector(Zone z) { zone = z; }
            private string Token(Entity e)
            { if (e == null) return null; if (!tokens.TryGetValue(e, out var token)) { token = "e" + (queue.Count + 1); tokens.Add(e, token); queue.Add(e); } return token; }
            public ZoneDump Dump()
            {
                var dump = new ZoneDump { id = zone.ZoneID, width = Zone.Width, height = Zone.Height, ambient = zone.AmbientLevel,
                    ambientTint = new[] {zone.AmbientTint.r,zone.AmbientTint.g,zone.AmbientTint.b,zone.AmbientTint.a}, urquBleed = zone.UrquBleedLevel };
                int[,] component = Components(zone, out int componentCount); dump.walkableComponentCount = componentCount;
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    var c = zone.GetCell(x, y); Require(c != null && c.X == x && c.Y == y && ReferenceEquals(c.ParentZone, zone), "Invalid cell: " + zone.ZoneID + "/" + x + "," + y);
                    var tile = zone.TileState.Get(x, y);
                    dump.cells.Add(new CellRow { x = x, y = y, solid = c.IsSolid(), wall = c.IsWall(), blocksMovement = c.BlocksMovement(), passable = c.IsPassable(),
                        interior = c.IsInterior, explored = c.Explored, visible = c.IsVisible, excludedArrival = c.HasObjectWithTag("ExcludeZoneArrival"),
                        reservedGenerationOnly = zone.GenReservedCells.Contains((x, y)), walkableComponent = component[x,y],
                        hasPoolWater = c.Objects.Any(e => e?.GetPart<LiquidPoolPart>()?.LiquidId == "water"),
                        hasTileWater = zone.TileState.HasCoating(x, y, "water"),
                        entityTokens = c.Objects.Select(Token).ToArray(), tileState = Shape(tile, 0, new HashSet<object>(RefComparer.Instance)) });
                }
                // The queue grows while inventory/equipment/part references are visited. One row per exact Entity.
                for (int i = 0; i < queue.Count; i++)
                {
                    var e = queue[i]; var pos = zone.GetEntityPosition(e);
                    var row = new EntityRow { token = Token(e), id = e.ID, blueprint = e.BlueprintName, x = pos.x, y = pos.y,
                        tags = Map(e.Tags), properties = Map(e.Properties), intProperties = Map(e.IntProperties),
                        stats = Shape(e.Statistics, 0, new HashSet<object>(RefComparer.Instance)) };
                    foreach (var part in e.Parts)
                    {
                        Require(part != null, "Null Part on " + e.BlueprintName);
                        row.parts.Add(new PartRow { name = part.Name, type = part.GetType().FullName,
                            fields = PublicFields(part, 0, new HashSet<object>(RefComparer.Instance)) });
                    }
                    var body = e.GetPart<Body>();
                    if (body != null) foreach (var p in body.GetParts())
                        row.bodySlots.Add(new BodySlot { type = p.Type, name = p.Name, position = p.Position, flags = p.Flags,
                            laterality = p._Laterality, recipe = p.DefaultBehaviorBlueprint, equipped = Token(p._Equipped),
                            natural = Token(p._DefaultBehavior), cybernetic = Token(p._Cybernetics) });
                    dump.entities.Add(row);
                    Require(queue.Count < 100000, "Unexpected unbounded entity graph.");
                }
                Require(dump.cells.Count == 2000, "Incomplete cell dump.");
                return dump;
            }
            private List<FieldRow> PublicFields(object value, int depth, HashSet<object> seen)
            {
                var fields = new List<FieldRow>();
                foreach (var f in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name, StringComparer.Ordinal))
                {
                    // Nonserialized fields are still useful runtime metadata; entity refs become tokens.
                    fields.Add(new FieldRow { name = f.Name, declaredType = f.FieldType.FullName,
                        value = Shape(f.GetValue(value), depth + 1, seen) });
                }
                return fields;
            }
            private ValueRow Shape(object value, int depth, HashSet<object> seen)
            {
                if (value == null) return new ValueRow { kind = "null" };
                var t = value.GetType(); var r = new ValueRow { type = t.FullName };
                if (value is Entity e) { r.kind = "entityReference"; r.text = Token(e); return r; }
                if (value is Zone z) { r.kind = "zoneReference"; r.text = z.ZoneID; return r; }
                if (value is UnityEngine.Object u) { r.kind = "unityObjectReference"; r.text = u == null ? null : u.name; return r; }
                if (value is Delegate || value is System.Random) { r.kind = "opaqueRuntimeState"; return r; }
                if (t.IsPrimitive || t.IsEnum || value is string || value is decimal)
                { r.kind = "scalar"; r.text = Convert.ToString(value, CultureInfo.InvariantCulture); return r; }
                if (depth > 8) { r.kind = "depthLimit"; return r; }
                if (!t.IsValueType && !seen.Add(value)) { r.kind = "cycle"; return r; }
                try
                {
                    if (value is IDictionary dictionary)
                    {
                        r.kind = "dictionary"; r.entries = new List<FieldRow>();
                        var entries = new List<DictionaryEntry>(); foreach (DictionaryEntry pair in dictionary) entries.Add(pair);
                        foreach (var pair in entries.OrderBy(p => Convert.ToString(p.Key, CultureInfo.InvariantCulture), StringComparer.Ordinal))
                            r.entries.Add(new FieldRow { name = Convert.ToString(pair.Key, CultureInfo.InvariantCulture), value = Shape(pair.Value, depth + 1, seen) });
                    }
                    else if (value is IEnumerable sequence)
                    { r.kind = "sequence"; r.items = new List<ValueRow>(); foreach (var item in sequence) r.items.Add(Shape(item, depth + 1, seen)); }
                    else { r.kind = "publicFields"; r.entries = PublicFields(value, depth, seen); }
                    return r;
                }
                finally { if (!t.IsValueType) seen.Remove(value); }
            }
        }
        private static int[,] Components(Zone zone, out int total)
        {
            int[,] ids = new int[Zone.Width, Zone.Height];
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++) ids[x,y] = -1;
            total=0; var work = new Queue<(int x,int y)>(); int[] dx={-1,1,0,0}, dy={0,0,-1,1};
            for(int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
            {
                if(ids[x,y]>=0 || zone.GetCell(x,y).BlocksMovement()) continue;
                ids[x,y]=total;work.Enqueue((x,y));
                while(work.Count>0) { var p=work.Dequeue(); for(int d=0;d<4;d++) { int nx=p.x+dx[d],ny=p.y+dy[d];
                    if(!zone.InBounds(nx,ny)||ids[nx,ny]>=0||zone.GetCell(nx,ny).BlocksMovement())continue;
                    ids[nx,ny]=total;work.Enqueue((nx,ny)); } }
                total++;
            }
            return ids;
        }
        private static string LayoutHash(ZoneDump dump)
        {
            var bp = dump.entities.ToDictionary(e=>e.token,e=>e.blueprint); var s=new StringBuilder();
            foreach(var c in dump.cells) { s.Append(c.x).Append(',').Append(c.y).Append(':').Append(c.solid).Append(c.wall).Append(c.blocksMovement).Append(c.interior)
                .Append(c.hasPoolWater).Append(c.hasTileWater).Append('|'); foreach(var token in c.entityTokens) s.Append(token==null?"<null>":bp[token]).Append(';'); s.Append('\n'); }
            return Hash(s.ToString());
        }
        private static SourceRow[] SourceHashes()
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")); var paths=new List<string>();
            foreach(var folder in new[]{"Assets/Scripts/Gameplay/World","Assets/Scripts/Data","Assets/Resources/Content","Assets/Resources/SceneArt/Morrowfast","Assets/Resources/SceneArt/FellingSite"})
                paths.AddRange(Directory.GetFiles(Path.Combine(project,folder),"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".cs",StringComparison.Ordinal)||p.EndsWith(".json",StringComparison.Ordinal)));
            paths.Add(Path.Combine(project,"ProjectSettings/ProjectVersion.txt"));
            return paths.OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new SourceRow{path=p.Substring(project.Length+1),sha256=Hash(File.ReadAllBytes(p))}).ToArray();
        }
        private static TextAsset RequiredResource(string path) { var a=Resources.Load<TextAsset>(path); Require(a!=null,"Missing shipped resource: "+path);return a; }
        private static List<string> ResourceTexts(string path) { var a=Resources.LoadAll<TextAsset>(path);Require(a.Length>0,"Missing shipped registry: "+path);return a.Select(x=>x.text).ToList(); }
        private static string Argument(string key) { var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return null; }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        private static string Hash(string text)=>Hash(Encoding.UTF8.GetBytes(text));
        private static string Hash(byte[] bytes) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static KV[] Map<T>(Dictionary<string,T> values)=>values.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new KV{key=p.Key,value=Convert.ToString(p.Value,CultureInfo.InvariantCulture)}).ToArray();
        private static ConnectionRow Connection(ZoneConnection c)=>new ConnectionRow{source=c.SourceZoneID,sx=c.SourceX,sy=c.SourceY,target=c.TargetZoneID,tx=c.TargetX,ty=c.TargetY,type=c.Type};
        private sealed class RefComparer:IEqualityComparer<object> { public static readonly RefComparer Instance=new RefComparer();public new bool Equals(object a,object b)=>ReferenceEquals(a,b);public int GetHashCode(object o)=>RuntimeHelpers.GetHashCode(o); }

        [Serializable] private sealed class Report { public bool success,allBordersHaveViableBothDirections;public int seed;public string unityVersion,platform,runtime,generatedUtc,coordinateConvention,stage,honesty;public string[] worldFlags,unexpectedErrors;public SourceRow[] sources;public List<ZoneSummary> zones=new List<ZoneSummary>();public List<BorderRow>borders=new List<BorderRow>();public ConnectionRow[] connections; }
        [Serializable] private sealed class SourceRow { public string path,sha256; }
        [Serializable] private sealed class ZoneSummary { public string id,file,sha256,layoutSha256;public int cells,onGroundEntities,reachableEntityGraph;public CountRow[] blueprintCounts; }
        [Serializable] private sealed class CountRow { public string key;public int count; }
        [Serializable] private sealed class ZoneDump { public string id,direction,biome,elevationBand,formation,authoredDefinitionFile;public int width,height,wx,wy,depth,tier,walkableComponentCount;public bool road,river;public float ambient,urquBleed;public float[] ambientTint;public PoiRow poi;public BuilderRow[] pipeline;public ConnectionRow[] connections;public List<CellRow> cells=new List<CellRow>();public List<EntityRow> entities=new List<EntityRow>(); }
        [Serializable] private sealed class PoiRow { public string type,name,profile,faction;public int tier; }
        [Serializable] private sealed class BuilderRow { public string name;public int priority; }
        [Serializable] private sealed class CellRow { public int x,y,walkableComponent;public bool solid,wall,blocksMovement,passable,interior,explored,visible,excludedArrival,reservedGenerationOnly,hasPoolWater,hasTileWater;public string[] entityTokens;public ValueRow tileState; }
        [Serializable] private sealed class EntityRow { public string token,id,blueprint;public int x,y;public KV[]tags,properties,intProperties;public ValueRow stats;public List<PartRow>parts=new List<PartRow>();public List<BodySlot>bodySlots=new List<BodySlot>(); }
        [Serializable] private sealed class PartRow { public string name,type;public List<FieldRow>fields; }
        [Serializable] private sealed class FieldRow { public string name,declaredType;public ValueRow value; }
        [Serializable] private sealed class ValueRow { public string kind,type,text;public List<FieldRow>entries;public List<ValueRow>items; }
        [Serializable] private sealed class KV { public string key,value; }
        [Serializable] private sealed class BodySlot { public string type,name,recipe,equipped,natural,cybernetic;public int position,flags,laterality; }
        [Serializable] private sealed class ConnectionRow { public string source,target,type;public int sx,sy,tx,ty; }
        [Serializable] private sealed class BorderRow { public string from,to,direction;public ArrivalRow[]forward,reverse; }
        [Serializable] private sealed class ArrivalRow { public int sourceX,sourceY,idealX,idealY,arrivalX,arrivalY;public bool sourceWalkable,arrivalExists,arrivalWalkable,relocated,arrivalExcluded; }
    }
}
#endif
