using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Builders
{
    /// <summary>
    /// Fluent builder returned by <see cref="ScenarioContext.Spawn(string)"/>.
    /// Chain positioning and modification methods, then invoke a terminal
    /// positioning method (<c>.At</c>, <c>.AtPlayerOffset</c>, <c>.NearPlayer</c>,
    /// <c>.AdjacentToPlayer</c>, <c>.InRing</c>, <c>.OnFirstPassableCell</c>) to
    /// trigger the spawn.
    ///
    /// Pipeline ordering — applied in this exact sequence inside SpawnAt:
    ///
    ///   1. Factory.CreateEntity(blueprint)
    ///   2. Pre-wiring stat + brain-flag mutations
    ///        - WithStatMax (first, so later WithStat won't silently clamp)
    ///        - WithStat
    ///        - Passive / Hostile
    ///   3. BrainPart wiring (CurrentZone, Rng, default StartingCell)
    ///   4. WithStartingCell override (if set)
    ///   5. Zone.AddEntity(entity, x, y)
    ///   6. Post-placement mutations
    ///        - WithHp (fraction or absolute)
    ///        - WithInventory (spawn items + AddObject, no equip)
    ///        - WithEquipment (spawn item + AddObject + InventorySystem.Equip)
    ///        - WithGoal (brain.PushGoal for each)
    ///        - AsPersonalEnemyOf (brain.SetPersonallyHostile)
    ///   7. TurnManager.AddEntity (if Creature-tagged and not suppressed)
    ///
    /// Fail-soft: if positioning fails or any modifier has an unmet precondition
    /// (e.g. WithEquipment on a creature with no InventoryPart), the builder logs
    /// a warning and either drops the spawn entirely (pre-CreateEntity failures)
    /// or skips that specific modifier (post-CreateEntity failures). Partial
    /// scenarios are better than no scenarios.
    /// </summary>
    public sealed class EntityBuilder
    {
        private readonly ScenarioContext _ctx;
        private readonly string _blueprintName;

        // --- Pre-wiring modifications ---
        private readonly List<(string statName, int max)> _statMaxOverrides = new List<(string, int)>();
        private readonly List<(string statName, int value)> _statOverrides = new List<(string, int)>();
        private bool? _passive;

        // --- Post-wiring, pre-placement modification ---
        private (int x, int y)? _startingCellOverride;

        // --- Post-placement modifications ---
        private float? _hpFraction;
        private int? _hpAbsolute;
        private readonly List<string> _inventoryBlueprints = new List<string>();
        private string _equipmentBlueprint;
        private readonly List<GoalHandler> _goalsToAdd = new List<GoalHandler>();
        private Entity _personalEnemy;

        // --- Turn registration flag ---
        private bool _registerForTurns = true;

        internal EntityBuilder(ScenarioContext ctx, string blueprintName)
        {
            _ctx = ctx;
            _blueprintName = blueprintName;
        }

        // =========================================================
        // Modification chain — pre-wiring stats & flags
        // =========================================================

        /// <summary>
        /// Set a stat's <see cref="Stat.BaseValue"/>. Clamped to [Min, Max] at apply
        /// time. If the stat doesn't exist on the blueprint, logs a warning and
        /// skips. For values above 30, also call <see cref="WithStatMax"/> — Stat.Max
        /// defaults to 30 and will silently clamp otherwise.
        /// </summary>
        public EntityBuilder WithStat(string statName, int value)
        {
            if (!string.IsNullOrEmpty(statName))
                _statOverrides.Add((statName, value));
            return this;
        }

        /// <summary>
        /// Raise a stat's <see cref="Stat.Max"/> ceiling. Applied before
        /// <see cref="WithStat"/> so high-value sets don't silently clamp.
        /// Lowering Max is permitted but may clamp BaseValue at apply time.
        /// </summary>
        public EntityBuilder WithStatMax(string statName, int max)
        {
            if (!string.IsNullOrEmpty(statName))
                _statMaxOverrides.Add((statName, max));
            return this;
        }

        /// <summary>
        /// Set <see cref="BrainPart.Passive"/>. Default argument is true — call
        /// <c>.Passive(false)</c> or <c>.Hostile()</c> to explicitly un-passivate
        /// a blueprint that was Passive by default.
        /// </summary>
        public EntityBuilder Passive(bool enabled = true)
        {
            _passive = enabled;
            return this;
        }

        /// <summary>Alias for <c>.Passive(false)</c> — explicit semantic.</summary>
        public EntityBuilder Hostile() => Passive(false);

        // =========================================================
        // Modification chain — post-wiring, pre-placement
        // =========================================================

        /// <summary>
        /// Override the auto-set <see cref="BrainPart.StartingCellX"/>/<c>StartingCellY</c>,
        /// which default to the spawn cell. Use when the creature should "know home
        /// is elsewhere" (e.g., a patrolling Warden spawned mid-patrol).
        /// </summary>
        public EntityBuilder WithStartingCell(int x, int y)
        {
            _startingCellOverride = (x, y);
            return this;
        }

        // =========================================================
        // Modification chain — post-placement
        // =========================================================

        /// <summary>
        /// Set HP to a fraction of Max (0.0 to 1.0). For example, 0.20f = 20% HP.
        /// Applied after spawn so Hitpoints.Max is known.
        /// </summary>
        public EntityBuilder WithHp(float fraction)
        {
            _hpFraction = Mathf.Clamp01(fraction);
            _hpAbsolute = null;
            return this;
        }

        /// <summary>Set HP to an absolute value (clamped to [0, Max]).</summary>
        public EntityBuilder WithHpAbsolute(int hp)
        {
            _hpAbsolute = hp;
            _hpFraction = null;
            return this;
        }

        /// <summary>
        /// Spawn the named item blueprint, add it to the entity's inventory, and
        /// equip it via <see cref="InventorySystem.Equip"/>. Only one equipment
        /// item per builder call — subsequent calls replace the pending blueprint.
        /// Silently skipped (with warning) if the entity lacks an InventoryPart.
        /// </summary>
        public EntityBuilder WithEquipment(string itemBlueprintName)
        {
            _equipmentBlueprint = itemBlueprintName;
            return this;
        }

        /// <summary>
        /// Spawn items from the given blueprints and add to inventory (not equipped).
        /// Stackable items auto-merge via <see cref="StackerPart"/>.
        /// </summary>
        public EntityBuilder WithInventory(params string[] itemBlueprintNames)
        {
            if (itemBlueprintNames != null)
                _inventoryBlueprints.AddRange(itemBlueprintNames);
            return this;
        }

        /// <summary>
        /// Push a goal onto the spawned entity's brain stack. Multiple calls push
        /// multiple goals in the order you chained them. Silently skipped (with
        /// warning) if the entity lacks a BrainPart.
        /// </summary>
        public EntityBuilder WithGoal(GoalHandler goal)
        {
            if (goal != null)
                _goalsToAdd.Add(goal);
            return this;
        }

        /// <summary>
        /// Make this entity personally hostile toward the target, bypassing faction
        /// feelings. One-way source→target, but <see cref="FactionManager.GetFeeling"/>
        /// checks both directions so in practice they become mutually hostile.
        /// Silently skipped (with warning) if the entity lacks a BrainPart.
        /// </summary>
        public EntityBuilder AsPersonalEnemyOf(Entity target)
        {
            _personalEnemy = target;
            return this;
        }

        /// <summary>
        /// Skip registration with the <see cref="TurnManager"/>. Useful for inert
        /// test targets that should exist in the world but not take turns.
        /// </summary>
        public EntityBuilder NotRegisteredForTurns()
        {
            _registerForTurns = false;
            return this;
        }

        // =========================================================
        // Positioning terminals
        // =========================================================

        /// <summary>Spawn at absolute zone cell (x, y).</summary>
        public Entity At(int x, int y) => SpawnAt(x, y);

        /// <summary>Spawn at a cell offset from the live player's current position.</summary>
        public Entity AtPlayerOffset(int dx, int dy)
        {
            var playerPos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
            if (playerPos.x < 0)
            {
                Debug.LogWarning($"[Scenario] AtPlayerOffset: player has no position — skipping spawn of '{_blueprintName}'");
                return null;
            }
            return SpawnAt(playerPos.x + dx, playerPos.y + dy);
        }

        /// <summary>Spawn at a random passable cell within Chebyshev band [min, max] from the player.</summary>
        public Entity NearPlayer(int minRadius = 1, int maxRadius = 8)
        {
            var playerPos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
            if (playerPos.x < 0)
            {
                Debug.LogWarning($"[Scenario] NearPlayer: player has no position — skipping spawn of '{_blueprintName}'");
                return null;
            }
            var candidates = PositionResolver.CollectCellsInRadiusBand(
                _ctx.Zone, playerPos.x, playerPos.y, minRadius, maxRadius);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[Scenario] NearPlayer({minRadius},{maxRadius}): no passable cells for '{_blueprintName}' — skipping.");
                return null;
            }
            var pick = candidates[_ctx.Rng.Next(candidates.Count)];
            return SpawnAt(pick.x, pick.y);
        }

        /// <summary>Spawn on a random passable cell immediately adjacent to the player.</summary>
        public Entity AdjacentToPlayer() => NearPlayer(1, 1);

        /// <summary>Spawn at ring position <paramref name="indexOf"/> of <paramref name="totalOfN"/> around the player.</summary>
        public Entity InRing(int radius, int indexOf, int totalOfN)
        {
            var playerPos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
            if (playerPos.x < 0)
            {
                Debug.LogWarning($"[Scenario] InRing: player has no position — skipping spawn of '{_blueprintName}'");
                return null;
            }
            var (x, y) = PositionResolver.ComputeRingPosition(
                playerPos.x, playerPos.y, radius, indexOf, totalOfN);
            return SpawnAt(x, y);
        }

        /// <summary>Spawn on the first passable cell matching <paramref name="predicate"/> (row-major scan).</summary>
        public Entity OnFirstPassableCell(Func<Cell, bool> predicate)
        {
            if (predicate == null)
            {
                Debug.LogWarning($"[Scenario] OnFirstPassableCell: null predicate — skipping spawn of '{_blueprintName}'");
                return null;
            }
            var match = PositionResolver.FindFirstPassableCell(_ctx.Zone, predicate);
            if (match == null)
            {
                Debug.LogWarning($"[Scenario] OnFirstPassableCell: no cell matched predicate for '{_blueprintName}' — skipping.");
                return null;
            }
            return SpawnAt(match.Value.x, match.Value.y);
        }

        // =========================================================
        // Internals — the one spawn pipeline
        // =========================================================

        private Entity SpawnAt(int x, int y)
        {
            if (!_ctx.Zone.InBounds(x, y))
            {
                Debug.LogWarning($"[Scenario] Spawn target ({x},{y}) is out of zone bounds for '{_blueprintName}' — skipping.");
                return null;
            }

            var cell = _ctx.Zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable())
            {
                Debug.LogWarning($"[Scenario] Spawn target ({x},{y}) is not passable for '{_blueprintName}' — skipping.");
                return null;
            }

            var entity = _ctx.Factory.CreateEntity(_blueprintName);
            if (entity == null)
            {
                Debug.LogWarning($"[Scenario] Blueprint '{_blueprintName}' not found in EntityFactory — skipping spawn.");
                return null;
            }

            // === Stage 1: Pre-wiring stat / flag mutations ===
            // Max first so BaseValue sets don't silently clamp.
            foreach (var (statName, max) in _statMaxOverrides)
            {
                var stat = entity.GetStat(statName);
                if (stat == null)
                {
                    Debug.LogWarning($"[Scenario] WithStatMax('{statName}', {max}): stat not found on '{_blueprintName}'.");
                    continue;
                }
                stat.Max = max;
            }
            foreach (var (statName, value) in _statOverrides)
            {
                var stat = entity.GetStat(statName);
                if (stat == null)
                {
                    Debug.LogWarning($"[Scenario] WithStat('{statName}', {value}): stat not found on '{_blueprintName}'.");
                    continue;
                }
                stat.BaseValue = Mathf.Clamp(value, stat.Min, stat.Max);
            }

            var brain = entity.GetPart<BrainPart>();
            if (_passive.HasValue)
            {
                if (brain != null)
                    brain.Passive = _passive.Value;
                else
                    Debug.LogWarning($"[Scenario] Passive/Hostile called on '{_blueprintName}' which has no BrainPart.");
            }

            // === Stage 2: Brain wiring (mirrors GameBootstrap.RegisterCreaturesForTurns) ===
            if (brain != null)
            {
                brain.CurrentZone = _ctx.Zone;
                brain.Rng = new System.Random(_ctx.Rng.Next());
                brain.StartingCellX = x;
                brain.StartingCellY = y;
            }

            // === Stage 3: StartingCell override ===
            if (_startingCellOverride.HasValue)
            {
                if (brain != null)
                {
                    brain.StartingCellX = _startingCellOverride.Value.x;
                    brain.StartingCellY = _startingCellOverride.Value.y;
                }
                else
                {
                    Debug.LogWarning($"[Scenario] WithStartingCell called on '{_blueprintName}' which has no BrainPart.");
                }
            }

            // === Stage 4: Place in zone ===
            _ctx.Zone.AddEntity(entity, x, y);

            // === Stage 5: Post-placement mutations ===

            // HP (fraction has priority if both set due to mutual-exclusion setters)
            if (_hpFraction.HasValue)
            {
                var hpStat = entity.GetStat("Hitpoints");
                if (hpStat != null)
                {
                    int target = Mathf.Max(1, Mathf.RoundToInt(hpStat.Max * _hpFraction.Value));
                    hpStat.BaseValue = target;
                }
            }
            else if (_hpAbsolute.HasValue)
            {
                var hpStat = entity.GetStat("Hitpoints");
                if (hpStat != null)
                    hpStat.BaseValue = Mathf.Clamp(_hpAbsolute.Value, 0, hpStat.Max);
            }

            // Inventory (spawn items + add to inventory, no equip)
            if (_inventoryBlueprints.Count > 0)
            {
                var inventory = entity.GetPart<InventoryPart>();
                if (inventory == null)
                {
                    Debug.LogWarning($"[Scenario] WithInventory on '{_blueprintName}' which has no InventoryPart — skipping.");
                }
                else
                {
                    foreach (var bp in _inventoryBlueprints)
                    {
                        var item = _ctx.Factory.CreateEntity(bp);
                        if (item == null)
                        {
                            Debug.LogWarning($"[Scenario] WithInventory item blueprint '{bp}' not found — skipping item.");
                            continue;
                        }
                        if (!inventory.AddObject(item))
                            Debug.LogWarning($"[Scenario] WithInventory: AddObject failed for '{bp}' (weight limit?).");
                    }
                }
            }

            // Equipment (spawn item + add + equip)
            if (!string.IsNullOrEmpty(_equipmentBlueprint))
            {
                var inventory = entity.GetPart<InventoryPart>();
                var item = _ctx.Factory.CreateEntity(_equipmentBlueprint);
                if (item == null)
                {
                    Debug.LogWarning($"[Scenario] WithEquipment: blueprint '{_equipmentBlueprint}' not found — skipping.");
                }
                else if (inventory == null)
                {
                    Debug.LogWarning($"[Scenario] WithEquipment on '{_blueprintName}' which has no InventoryPart — skipping.");
                }
                else
                {
                    inventory.AddObject(item);
                    if (!InventorySystem.Equip(entity, item))
                        Debug.LogWarning($"[Scenario] WithEquipment: Equip call failed for '{_equipmentBlueprint}' (no matching slot?).");
                }
            }

            // Goals
            if (_goalsToAdd.Count > 0)
            {
                if (brain != null)
                {
                    foreach (var goal in _goalsToAdd)
                        brain.PushGoal(goal);
                }
                else
                {
                    Debug.LogWarning($"[Scenario] WithGoal called on '{_blueprintName}' which has no BrainPart — {_goalsToAdd.Count} goals dropped.");
                }
            }

            // Personal hostility
            if (_personalEnemy != null)
            {
                if (brain != null)
                    brain.SetPersonallyHostile(_personalEnemy);
                else
                    Debug.LogWarning($"[Scenario] AsPersonalEnemyOf on '{_blueprintName}' which has no BrainPart.");
            }

            // === Stage 6: Turn manager registration ===
            if (_registerForTurns && entity.HasTag("Creature"))
            {
                _ctx.Turns.AddEntity(entity);
            }

            return entity;
        }
    }
}
