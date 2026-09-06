using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// An 80x25 grid of Cells — one screen of the game world.
    /// Mirrors Qud's Zone: entities live in cells, and zones are the
    /// fundamental spatial unit. Qud uses 80x25 (classic terminal size).
    /// </summary>
    public class Zone
    {
        public const int Width = 80;
        public const int Height = 25;

        public string ZoneID;

        /// <summary>
        /// Biome-based ambient color tint applied by the lighting system.
        /// Subtle shift: cave=cool blue, desert=warm amber, etc.
        /// </summary>
        public Color AmbientTint = Color.white;

        /// <summary>
        /// The brightness of a cell this zone does not light itself —
        /// 0.0 pitch black, 1.0 full daylight.
        ///
        /// <para><b>Ambient is a property of the place</b> (Felling W0.2).
        /// It used to be one number on the renderer, which is fine for a
        /// world of meadows and caves and impossible for the canon one:
        /// a catacomb village lit by a single cultivated fungal patch, a
        /// dead zone of true dark where the light-ecology collapsed, and
        /// a salt pan at noon are not the same brightness, and the
        /// difference is gameplay — light is the underground's terrain
        /// (Docs/FELLING-WORLD-DESIGN.md §6).</para>
        ///
        /// <para>The default is load-bearing: <c>OnZoneGenerated</c> runs
        /// only at generation time and only for Overworld IDs, so village
        /// interiors, the world-map zone and every test fixture take this
        /// value and nothing else.</para>
        /// </summary>
        public float AmbientLevel = DefaultAmbientLevel;

        /// <summary>
        /// The brightness of an un-authored zone. Shared with
        /// <see cref="LightMap"/> so the renderer and the world can never
        /// disagree about what "ordinary" looks like.
        /// </summary>
        public const float DefaultAmbientLevel = 0.4f;

        /// <summary>
        /// How strongly Urqu-pressure reaches this zone. 0 = none — the
        /// corpus's own Tier-1 baseline (canon: Urqu-bleed is
        /// "distributed, not a region"; the surface starts clean).
        ///
        /// <para>Mirrors <see cref="AmbientLevel"/>'s shape exactly — a
        /// field seeded now and PERSISTED (SaveSystem v6, beside
        /// AmbientLevel), with a real WRITER arriving in a later phase
        /// (W7's per-cell bleed mask, Docs/FELLING-WORLD-DESIGN.md §7.2).
        /// Nothing sets this above 0 in W1/W2 — the flower-charm
        /// instrument that reads it (<see cref="FlowerCharmPart"/>) is
        /// therefore provably dormant right now, which is thematically
        /// correct: the Thinning has not escalated yet at game start.</para>
        /// </summary>
        public float UrquBleedLevel = 0f;

        /// <summary>
        /// G.10 — prevailing wind strength, 0-100. Biases gas dispersal
        /// (frequency, attempt count, direction, thin-gas dissipation) in
        /// <see cref="GasSystem.ProcessGasBehavior"/>. Mirrors Qud's
        /// <c>Zone.CurrentWindSpeed</c> (Gas.cs:216). Default 0 reproduces
        /// the pre-G.10 no-wind dispersal EXACTLY — it is the implicit
        /// gate (CoO has no <c>WindAffectsGasDispersal</c> global setting).
        /// </summary>
        public int CurrentWindSpeed = 0;

        /// <summary>
        /// G.10 — prevailing wind direction: "N","NE","E","SE","S","SW",
        /// "W","NW". Empty / unrecognized ⇒ no directional bias (spread
        /// stays uniform-random even if <see cref="CurrentWindSpeed"/> &gt; 0).
        /// Mirrors Qud's <c>Zone.CurrentWindDirection</c> (Gas.cs:217).
        /// </summary>
        public string CurrentWindDirection = "";

        /// <summary>
        /// The grid of cells, stored as [x, y].
        /// </summary>
        public Cell[,] Cells = new Cell[Width, Height];

        /// <summary>
        /// Quick lookup: entity -> which cell it's in.
        /// </summary>
        private Dictionary<Entity, Cell> _entityCells = new Dictionary<Entity, Cell>();

        /// <summary>
        /// Tag membership index: tag -> set of entities currently in this
        /// zone with that tag. Drops <see cref="GetEntitiesWithTagNonAlloc"/>
        /// from O(N) full-zone scan to O(matches) iteration.
        ///
        /// <para><b>Sync model.</b> Built up on
        /// <see cref="AddEntity"/> (first-time placement) and torn down on
        /// <see cref="RemoveEntity"/>. Snapshot the entity's tag set at
        /// add time. <see cref="MoveEntity"/> does NOT touch the index —
        /// movement doesn't change tag membership.</para>
        ///
        /// <para><b>Runtime tag mutations.</b> If gameplay code mutates
        /// <c>entity.Tags</c> AFTER the entity is in the zone (e.g. a
        /// mutation grants a "Chimera" tag mid-play), the index won't
        /// see the new tag. Callers performing such mutations should
        /// invoke <see cref="NotifyEntityTagAdded"/> /
        /// <see cref="NotifyEntityTagRemoved"/> to keep the index in
        /// sync. The vast majority of tag queries (most importantly
        /// "Creature" used by AI hostile-scan) hit tags set at blueprint
        /// load — those are captured correctly by the add-time snapshot.
        /// Tier-A scaling fix S1 — see Docs/PERF-SCALING-AUDIT.md.</para>
        /// </summary>
        private readonly Dictionary<string, HashSet<Entity>> _tagIndex
            = new Dictionary<string, HashSet<Entity>>();

        /// <summary>
        /// BIOME-OVERHAUL A2 — generation-time cell claims. Builders
        /// that hand-place structures (LandmarkBuilder stamps) add
        /// their footprint here; PopulationBuilder skips these cells so
        /// random spawns don't land inside authored interiors. Runtime-
        /// only: NOT serialized (population has already run by save
        /// time) and irrelevant to loaded zones.
        /// </summary>
        public readonly HashSet<(int x, int y)> GenReservedCells = new HashSet<(int x, int y)>();
        /// <summary>
        /// PALIMPSEST P2 — durable per-tile state (coatings, residues,
        /// energy, clouds). Sparse: an untouched zone costs one empty
        /// Dictionary against the 2000 Cells the constructor already
        /// allocates.
        ///
        /// <para><b>NOT serialized</b> — like <c>GenReservedCells</c>
        /// above. Persisting it would need SaveWriter.FormatVersion
        /// 4→5, and SaveSystem.cs:133 is a strict-equality check with no
        /// migration path, so every existing save would stop loading.
        /// Tile state decays in 2–8 turns, so losing it across a save is
        /// a small inconsistency against a large cost. Revisit if P7's
        /// Scrape economy makes a tile worth more than a few turns.</para>
        ///
        /// <para>Ticked once per PLAYER turn by
        /// <see cref="ZoneTileStateSystem"/> — deliberately NOT by
        /// TickEnd, which fires once per ACTOR and would make a
        /// coating's lifetime a function of how crowded the zone is.</para>
        /// </summary>
        public readonly ZoneTileState TileState = new ZoneTileState();


        public Zone(string zoneID = null)
        {
            ZoneID = zoneID ?? "Zone";
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y] = new Cell(x, y, this);
                }
            }
        }

        /// <summary>
        /// Get a cell by coordinates. Returns null if out of bounds.
        /// </summary>
        public Cell GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return Cells[x, y];
        }

        /// <summary>
        /// Place an entity at a specific cell position.
        /// If the entity is already in this zone, it is moved.
        /// </summary>
        public bool AddEntity(Entity entity, int x, int y)
        {
            Cell cell = GetCell(x, y);
            if (cell == null || entity == null) return false;
            // Refuse before removing a moved plant from its source cell.
            if (BarrenGroundRules.IsVegetation(entity)
                && (BarrenGroundRules.IsBarren(cell) || entity.HasTag("Barren"))) return false;
            if (entity.HasTag("Barren")) BarrenGroundRules.ClearVegetation(this, cell);

            // Distinguish first-time add from a move so the tag index
            // doesn't double-add. _entityCells is the source of truth
            // for "is this entity currently in this zone."
            bool isFreshAdd = !_entityCells.ContainsKey(entity);

            // Remove from old cell if already placed
            if (_entityCells.TryGetValue(entity, out Cell oldCell))
            {
                oldCell.RemoveObject(entity);
            }

            cell.AddObject(entity);
            _entityCells[entity] = cell;

            // PALIMPSEST P2b — a liquid pool entity PROJECTS into the
            // tile layer, so gameplay has one place to ask "is there
            // water here?". The entity is kept because four systems key
            // off it — the shoreline sprite family, river flow, three
            // worldgen placement vetoes, and MaterialReactionResolver's
            // fire+ice product — but it is no longer the thing that
            // ANSWERS the question.
            ProjectPool(entity, cell);

            if (isFreshAdd)
                IndexEntityTags(entity);

            EntityVersion++;
            return true;
        }

        /// <summary>
        /// Remove an entity from the zone entirely.
        /// </summary>
        public bool RemoveEntity(Entity entity)
        {
            // Un-project first: the cell lookup still works here.
            UnprojectPool(entity);
            if (_entityCells.TryGetValue(entity, out Cell cell))
            {
                cell.RemoveObject(entity);
                _entityCells.Remove(entity);
                UnindexEntityTags(entity);
                EntityVersion++;
                DragSystem.DetachRemovedEntity(entity);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Snapshot every tag the entity has and add it to the per-tag
        /// index. Called once when the entity first enters the zone.
        /// </summary>
        private void IndexEntityTags(Entity entity)
        {
            if (entity.Tags == null) return;
            foreach (var kvp in entity.Tags)
            {
                if (!_tagIndex.TryGetValue(kvp.Key, out var set))
                {
                    set = new HashSet<Entity>();
                    _tagIndex[kvp.Key] = set;
                }
                set.Add(entity);
            }
        }

        /// <summary>
        /// Drop every tag association for an entity. Called on
        /// <see cref="RemoveEntity"/>. Iterating the entity's current
        /// tag set is safe even if it was mutated post-add — we just
        /// remove from whichever sets the entity currently appears in.
        /// Misses (tag exists in dict but entity isn't in it) are no-ops.
        /// </summary>
        private void UnindexEntityTags(Entity entity)
        {
            if (entity.Tags == null) return;
            foreach (var kvp in entity.Tags)
            {
                if (_tagIndex.TryGetValue(kvp.Key, out var set))
                    set.Remove(entity);
            }
            // Defensive: also scan the index for any tag set that still
            // references the entity (handles runtime tag mutations that
            // weren't reported via NotifyEntityTagRemoved). Cheap because
            // _tagIndex.Count is bounded by distinct-tag count, not entity
            // count.
            foreach (var kvp in _tagIndex)
                kvp.Value.Remove(entity);
        }

        /// <summary>
        /// Hook for code that mutates <c>entity.Tags</c> at runtime
        /// (mutations granting/revoking tags, conversation actions,
        /// etc). Keeps the tag index in sync. No-op if the entity isn't
        /// in this zone.
        /// </summary>
        public void NotifyEntityTagAdded(Entity entity, string tag)
        {
            if (entity == null || tag == null) return;
            if (!_entityCells.ContainsKey(entity)) return;
            if (!_tagIndex.TryGetValue(tag, out var set))
            {
                set = new HashSet<Entity>();
                _tagIndex[tag] = set;
            }
            set.Add(entity);
        }

        /// <summary>
        /// Companion to <see cref="NotifyEntityTagAdded"/> — call when
        /// removing a tag from an entity already in the zone.
        /// </summary>
        public void NotifyEntityTagRemoved(Entity entity, string tag)
        {
            if (entity == null || tag == null) return;
            if (_tagIndex.TryGetValue(tag, out var set))
                set.Remove(entity);
        }

        /// <summary>
        /// Move an entity to a new position. Returns false if blocked or out of bounds.
        /// </summary>
        public bool MoveEntity(Entity entity, int newX, int newY)
        {
            Cell target = GetCell(newX, newY);
            if (target == null) return false;

            // Forced displacement may bypass ordinary collision; it must
            // still stop at the sealed archive. AddEntity remains available
            // for content placement and saved-state reconstruction.
            if (target.HasClosedArchiveBarrier()) return false;

            return AddEntity(entity, newX, newY);
        }

        /// <summary>
        /// Get the cell an entity is currently in.
        /// </summary>
        public Cell GetEntityCell(Entity entity)
        {
            _entityCells.TryGetValue(entity, out Cell cell);
            return cell;
        }

        /// <summary>
        /// Get the position of an entity as (x, y). Returns (-1, -1) if not found.
        /// </summary>
        public (int x, int y) GetEntityPosition(Entity entity)
        {
            if (_entityCells.TryGetValue(entity, out Cell cell))
                return (cell.X, cell.Y);
            return (-1, -1);
        }

        /// <summary>
        /// Check if coordinates are within zone bounds.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Get all entities in the zone (allocates a new list — prefer GetReadOnlyEntities for iteration).
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var result = new List<Entity>(_entityCells.Count);
            result.AddRange(_entityCells.Keys);
            return result;
        }

        /// <summary>
        /// Non-allocating read-only view of all entities. Callers must not mutate the zone during iteration.
        /// </summary>
        public Dictionary<Entity, Cell>.KeyCollection GetReadOnlyEntities()
        {
            return _entityCells.Keys;
        }

        /// <summary>
        /// Get all entities with a specific tag (allocates a new list).
        /// O(matches) via <see cref="_tagIndex"/>. Prefer
        /// <see cref="GetEntitiesWithTagNonAlloc"/> for hot paths.
        /// </summary>
        public List<Entity> GetEntitiesWithTag(string tag)
        {
            if (!_tagIndex.TryGetValue(tag, out var set))
                return new List<Entity>();
            var result = new List<Entity>(set.Count);
            foreach (var entity in set)
                result.Add(entity);
            return result;
        }

        /// <summary>
        /// Non-allocating variant: fills an existing list with entities matching the tag.
        /// The list is cleared before filling.
        ///
        /// <para>O(matches) via <see cref="_tagIndex"/>. Pre-S1 this was
        /// O(N) full-zone scan + per-entity HashSet lookup, which became
        /// the AI scan bottleneck at >5k entities. Mostly hot for
        /// <c>"Creature"</c> queries (BoredGoal / GuardGoal /
        /// DormantGoal / FindNearestHostile).</para>
        /// </summary>
        public void GetEntitiesWithTagNonAlloc(string tag, List<Entity> result)
        {
            result.Clear();
            if (!_tagIndex.TryGetValue(tag, out var set)) return;
            foreach (var entity in set)
                result.Add(entity);
        }

        /// <summary>
        /// Iterate over all cells. Callback receives (cell, x, y).
        /// </summary>
        public void ForEachCell(System.Action<Cell, int, int> action)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    action(Cells[x, y], x, y);
                }
            }
        }

        /// <summary>
        /// Get the cell adjacent to (x,y) in a given direction.
        /// Directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        /// </summary>
        public Cell GetCellInDirection(int x, int y, int direction)
        {
            int dx = 0, dy = 0;
            switch (direction)
            {
                case 0: dy = -1; break;           // N
                case 1: dx = 1; dy = -1; break;   // NE
                case 2: dx = 1; break;             // E
                case 3: dx = 1; dy = 1; break;     // SE
                case 4: dy = 1; break;             // S
                case 5: dx = -1; dy = 1; break;    // SW
                case 6: dx = -1; break;            // W
                case 7: dx = -1; dy = -1; break;   // NW
            }
            return GetCell(x + dx, y + dy);
        }

        /// <summary>
        /// Incremented whenever entities are added, removed, or moved.
        /// Used by LightMap to skip recomputation when nothing changed.
        /// </summary>
        public int EntityVersion { get; private set; }

        public int EntityCount => _entityCells.Count;

        public void RebuildEntityCellsFromCells()
        {
            _entityCells.Clear();
            _tagIndex.Clear();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cell cell = Cells[x, y];
                    if (cell == null)
                    {
                        cell = new Cell(x, y, this);
                        Cells[x, y] = cell;
                    }

                    cell.ParentZone = this;
                    BarrenGroundRules.RepairLoadedCell(cell);
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity entity = cell.Objects[i];
                        if (entity != null)
                        {
                            _entityCells[entity] = cell;
                            IndexEntityTags(entity);
                        }
                    }
                }
            }
        }

        public void SetEntityVersionForLoad(int version)
        {
            EntityVersion = version;
        }

        public override string ToString()
        {
            return $"Zone({ZoneID}) [{EntityCount} entities]";
        }

        /// <summary>
        /// PALIMPSEST P2b — mirrors a <see cref="LiquidPoolPart"/>
        /// entity's liquid into <see cref="TileState"/> as a permanent
        /// coating.
        ///
        /// <para>Written with <see cref="ZoneTileState.Permanent"/>
        /// rather than a duration because the ENTITY owns the pool's
        /// lifetime — a river does not evaporate on a six-turn clock.
        /// Decay must not delete a projection out from under its
        /// source.</para>
        /// </summary>
        private void ProjectPool(Entity entity, Cell cell)
        {
            if (entity == null || cell == null) return;
            var pool = entity.GetPart<LiquidPoolPart>();
            if (pool == null || string.IsNullOrEmpty(pool.LiquidId)) return;
            TileState.WriteCoating(cell.X, cell.Y, pool.LiquidId, ZoneTileState.Permanent);
        }

        /// <summary>Removes the projection when its pool entity leaves.</summary>
        private void UnprojectPool(Entity entity)
        {
            if (entity == null) return;
            var pool = entity.GetPart<LiquidPoolPart>();
            if (pool == null || string.IsNullOrEmpty(pool.LiquidId)) return;
            var pos = GetEntityPosition(entity);
            if (pos.x < 0) return;
            TileState.RemoveCoating(pos.x, pos.y, pool.LiquidId);
        }
    }
}
