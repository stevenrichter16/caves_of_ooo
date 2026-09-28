using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Populates a zone with creatures and items from a PopulationTable.
    /// Mirrors Qud's PopTableZoneBuilder: categorizes cells by type,
    /// then places entities in appropriate cells.
    ///
    /// Priority: VERY_LATE (4000) -- after all terrain and connectivity.
    /// </summary>
    public class PopulationBuilder : IZoneBuilder
    {
        public string Name => "PopulationBuilder";
        public int Priority => 4000;
        public PopulationTable Table;
        /// <summary>Opt-in, transient provenance for a later composition pass.</summary>
        public bool CaptureSourceReceipts;
        public SpreadGenerationReceipt SourceReceipt { get; private set; }
        /// <summary>Separate exact rolls; neither receipt grants authority over matching decoys.</summary>
        public SpreadGenerationReceipt LooseSourceReceipt { get; private set; }
        public SpreadGenerationReceipt AmbientSourceReceipt { get; private set; }
        /// <summary>Only the exact Magpie roll changed to the optional grazer, before unrelated ambient stocking.</summary>
        public SpreadGenerationReceipt AmbientReplacementReceipt { get; private set; }
        /// <summary>Opt-in manager authority, rechecked against the captured cold attempt before source replacement.</summary>
        public OverworldZoneManager ExplorationManager;
        private int sourceRevision;
        /// <summary>Only ordinary selected Spread sources may replace their one hostile group.</summary>
        public SpreadRareEncounterBuilder SpreadEncounter;
        /// <summary>Optional cold-generation habitat predicate. A rolled
        /// animal with no eligible unoccupied cell is skipped and diagnosed.</summary>
        public System.Func<string, Cell, bool> HabitatFilter;

        public PopulationBuilder(PopulationTable table)
        {
            Table = table;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            SourceReceipt = LooseSourceReceipt = AmbientSourceReceipt = AmbientReplacementReceipt = null; int revision = ++sourceRevision;
            var receiptOwners = CaptureSourceReceipts ? new List<Entity>() : null;
            var looseOwners = CaptureSourceReceipts ? new List<Entity>() : null;
            var ambientOwners = CaptureSourceReceipts ? new List<Entity>() : null;
            var replacementOwners = CaptureSourceReceipts ? new List<Entity>() : null;
            int replacementIndex = -1;
            if (Table == null) return true;
            bool replaceSpread = Table.Name == "SpreadTier1" && SpreadEncounter != null
                && SpreadEncounter.TryPlace(zone, factory);

            // Categorize open cells (passable and not already occupied by a solid entity).
            // BIOME-OVERHAUL A2: cells claimed by structure stamps
            // (Zone.GenReservedCells) are excluded — no random spawns
            // inside authored interiors.
            var openCells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (!cell.BlocksMovement() && !zone.GenReservedCells.Contains((x, y)))
                    openCells.Add((x, y));
            });

            // Roll the population table
            var toSpawn = Table.Roll(rng, zone.ZoneID);
            var exploration = ExplorationManager?.Exploration;
            SpreadExplorationEntry assignment = null;
            bool optedIn = !replaceSpread && ReferenceEquals(factory, ExplorationManager?.Factory)
                && exploration != null && exploration.TryGetGenerationEntry(ExplorationManager, zone, out assignment);
            if (optedIn)
            {
                var rewritten = SpreadExplorationPopulationPolicy.Rewrite(Table, toSpawn, assignment.Family);
                string required = assignment.Family == SpreadExplorationFamily.OccupiedBank ? "MarlbackScrabbler"
                    : assignment.Family == SpreadExplorationFamily.LastGleanings ? "ReedbackGrazer" : null;
                if (required != null && !toSpawn.SequenceEqual(rewritten) && !factory.Blueprints.ContainsKey(required))
                {
                    // Missing optional content must not swallow a real ordinary
                    // roll. Preserve its original count/order and placement RNG.
                    optedIn = false;
                    if (Diag.IsChannelEnabled("worldgen"))
                        Diag.Record("worldgen", "ExplorationPopulationRewriteRejected", payload: new
                        { zone = zone.ZoneID, table = Table.Name, family = assignment.Family.ToString(), blueprint = required, reason = "missing_replacement_blueprint" });
                }
                else
                {
                    if (assignment.Family == SpreadExplorationFamily.LastGleanings && rewritten.Length == toSpawn.Count)
                    {
                        // Preserve the precise changed roll index. An authored or matching
                        // nearby grazer is not the owner produced by this substitution.
                        var changed = Enumerable.Range(0, toSpawn.Count).Where(i => toSpawn[i] != rewritten[i]).ToArray();
                        if (changed.Length == 1 && toSpawn[changed[0]] == "Magpie" && rewritten[changed[0]] == "ReedbackGrazer")
                            replacementIndex = changed[0];
                    }
                    toSpawn = new List<string>(rewritten);
                }
            }
            // Roll is intentionally unchanged. Ambiguous duplicate blueprint rows
            // cannot identify group provenance and therefore grant no authority.
            var group = receiptOwners == null || Table.Name != "SpreadTier1" || Table.Entries == null ? new HashSet<string>()
                : new HashSet<string>(Table.Entries.Where(e => e != null && e.EncounterGroup == "SpreadTier1Encounter")
                    .Select(e => e.BlueprintName).Where(bp => Table.Entries.All(e => e == null || e.BlueprintName != bp || e.EncounterGroup == "SpreadTier1Encounter")));
            int expected = replaceSpread ? 0 : toSpawn.Count(group.Contains);
            // Exact explicit ordinary rows only. Duplicate or grouped declarations cannot
            // silently promote quest/foreign owners into an ambient or loose allowance.
            var loose = ReceiptBlueprints("Hatchet", "Cudgel", "LeatherBoots");
            var ambient = ReceiptBlueprints("Magpie", "PetDog");
            if (optedIn && assignment.Family == SpreadExplorationFamily.LastGleanings && ambient.Contains("Magpie")) ambient.Add("ReedbackGrazer");
            int looseExpected = toSpawn.Count(loose.Contains), ambientExpected = toSpawn.Count(ambient.Contains);

            // Place each entity in a random open cell
            for (int spawnIndex = 0; spawnIndex < toSpawn.Count; spawnIndex++)
            {
                string blueprintName = toSpawn[spawnIndex];
                if (replaceSpread && Table.Entries.Exists(e => e.EncounterGroup == "SpreadTier1Encounter" && e.BlueprintName == blueprintName)) continue;
                if (openCells.Count == 0) break;

                int idx;
                if (HabitatFilter != null)
                {
                    // Reservoir selection is uniform over eligible cells. A
                    // cyclic scan would favor the first cell after a long gap.
                    int candidate = -1, eligible = 0;
                    for (int test = 0; test < openCells.Count; test++)
                    {
                        var pos = openCells[test];
                        if (HabitatFilter(blueprintName, zone.GetCell(pos.x, pos.y))
                            && rng.Next(++eligible) == 0) candidate = test;
                    }
                    if (candidate < 0)
                    {
                        if (Diag.IsChannelEnabled("worldgen"))
                            Diag.Record("worldgen", "HabitatPopulationRejected", payload: new
                            { blueprint = blueprintName, zone = zone.ZoneID, table = Table.Name,
                                reason = "no_habitat", open = openCells.Count, eligible });
                        continue;
                    }
                    idx = candidate;
                }
                else idx = rng.Next(openCells.Count);
                var (x, y) = openCells[idx];

                // BuilderSpawn rather than CreateEntity directly: a table
                // entry naming a blueprint this factory does not have is a
                // content problem, not a crash-or-log-spam problem. Test
                // fixtures deliberately load reduced blueprint sets, and
                // every other builder in worldgen already guards this way
                // (see BuilderSpawn, added when a stray 'Grass' entry did
                // the same thing). Missing blueprints in SHIPPED content are
                // caught by the per-biome table tests instead.
                var spawned = BuilderSpawn.TryPlace(zone, factory, blueprintName, x, y);
                if (IsStaticObstacle(spawned) && !PreservesStaticPassages(zone, x, y))
                {
                    // Connectivity ran before population. A stalagmite must
                    // not turn its single-cell doorway into a sealed chamber.
                    // Skip this scenery roll without consuming another random
                    // value or removing any of the authored architecture.
                    zone.RemoveEntity(spawned);
                    if (Diag.IsChannelEnabled("worldgen"))
                        Diag.Record("worldgen", "PopulationPlacementRejected", payload: new
                        { blueprint = blueprintName, zone = zone.ZoneID, table = Table.Name,
                            x, y, reason = "blocks_static_passage" });
                }

                if (receiptOwners != null && !replaceSpread && group.Contains(blueprintName)
                    && spawned != null && spawned.BlueprintName == blueprintName && zone.GetEntityCell(spawned) != null)
                    receiptOwners.Add(spawned);

                if (spawned != null && spawned.BlueprintName == blueprintName && zone.GetEntityCell(spawned) != null)
                {
                    if (loose.Contains(blueprintName)) looseOwners?.Add(spawned);
                    if (ambient.Contains(blueprintName)) ambientOwners?.Add(spawned);
                    if (spawnIndex == replacementIndex) replacementOwners?.Add(spawned);
                }

                // Remove used cell to prevent double-placement of solid entities
                openCells.RemoveAt(idx);
            }

            if (receiptOwners != null && CaptureSourceReceipts && revision == sourceRevision)
            {
                SourceReceipt = new SpreadGenerationReceipt(this, zone, factory, revision, receiptOwners, expected);
                LooseSourceReceipt = new SpreadGenerationReceipt(this, zone, factory, revision, looseOwners, looseExpected);
                AmbientSourceReceipt = new SpreadGenerationReceipt(this, zone, factory, revision, ambientOwners, ambientExpected);
                AmbientReplacementReceipt = new SpreadGenerationReceipt(this, zone, factory, revision, replacementOwners, replacementIndex < 0 ? 0 : 1);
            }
            return true;
        }

        private HashSet<string> ReceiptBlueprints(params string[] candidates)
        {
            var result = new HashSet<string>();
            if (!CaptureSourceReceipts || Table?.Name != "SpreadTier1" || Table.Entries == null) return result;
            foreach (string name in candidates)
            {
                var rows = Table.Entries.Where(e => e != null && e.BlueprintName == name).ToArray();
                if (rows.Length == 1 && string.IsNullOrEmpty(rows[0].EncounterGroup)) result.Add(name);
            }
            return result;
        }

        private static bool IsStaticObstacle(Entity entity)
        {
            if (entity == null || entity.HasPart<BrainPart>() || entity.HasTag("Creature")) return false;
            return entity.HasTag("Solid") || entity.GetPart<PhysicsPart>()?.Solid == true
                || entity.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true;
        }

        /// <summary>After inserting scenery, every open neighbor of its cell
        /// must still reach every other neighbor without walking through it.
        /// Those neighbors were connected through this cell before insertion,
        /// so this rejects only a newly split passage, not unrelated existing
        /// pockets. Uses the same eight-way interior topology as formations;
        /// creatures are transient occupants, not permanent architecture.</summary>
        private static bool PreservesStaticPassages(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return true;
            var open = new bool[Zone.Width, Zone.Height];
            for (int cy = 1; cy < Zone.Height - 1; cy++)
                for (int cx = 1; cx < Zone.Width - 1; cx++)
                {
                    var cell = zone.GetCell(cx, cy);
                    bool blocked = MorrowfastSceneRuntime.BlockingOwner(cell) != null;
                    foreach (var entity in cell.Occupants)
                        if (IsStaticObstacle(entity)) { blocked = true; break; }
                    open[cx, cy] = !blocked;
                }

            int neighbors = 0;
            (int x, int y) start = default;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && open[x + dx, y + dy])
                    { neighbors++; start = (x + dx, y + dy); }
            if (neighbors < 2) return true;

            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(start);
            open[start.x, start.y] = false;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (System.Math.Abs(cell.x - x) <= 1 && System.Math.Abs(cell.y - y) <= 1
                    && --neighbors == 0) return true;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cell.x + dx, ny = cell.y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1 || !open[nx, ny]) continue;
                        open[nx, ny] = false;
                        queue.Enqueue((nx, ny));
                    }
            }
            return false;
        }
    }
}
