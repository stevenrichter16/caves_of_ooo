using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Builders
{
    /// <summary>
    /// Fluent builder returned by <see cref="ScenarioContext.Spawn(string)"/>.
    /// Chain positioning and modification methods, then either invoke a terminal
    /// positioning method (<c>.At</c>, <c>.AtPlayerOffset</c>) to trigger the spawn,
    /// or leave the builder dangling — it will log a warning since no position was set.
    ///
    /// Spawn lifecycle:
    /// 1. Instantiate blueprint via <see cref="CavesOfOoo.Data.EntityFactory.CreateEntity"/>
    /// 2. Apply pre-placement modifications (HP fraction etc. — deferred until we know max stats)
    /// 3. Wire <see cref="BrainPart.CurrentZone"/>, <c>Rng</c>, <c>StartingCell</c>
    ///    (same wiring <see cref="GameBootstrap.RegisterCreaturesForTurns"/> does for real NPCs)
    /// 4. Add to zone at the resolved cell
    /// 5. Apply post-placement modifications (HP if deferred)
    /// 6. Optionally register with <see cref="TurnManager"/> so it actually takes turns
    ///
    /// If positioning resolution fails (blocked cell, off-map, etc.), the builder
    /// logs a warning and returns null — scenarios continue rather than crash.
    /// </summary>
    public sealed class EntityBuilder
    {
        private readonly ScenarioContext _ctx;
        private readonly string _blueprintName;

        // Pending modifications applied after spawn
        private float? _hpFraction;
        private int? _hpAbsolute;
        private bool _registerForTurns = true;

        internal EntityBuilder(ScenarioContext ctx, string blueprintName)
        {
            _ctx = ctx;
            _blueprintName = blueprintName;
        }

        // ---------- Modification chain (applied at spawn time) ----------

        /// <summary>
        /// Set HP to a fraction of Max (0.0 to 1.0). For example, <c>0.20f</c> = 20% HP.
        /// Applied after spawn so <c>Hitpoints.Max</c> is known.
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
        /// If called, the spawned entity will NOT be registered with the TurnManager.
        /// Useful for inert test targets (e.g., a creature you want to exist visually
        /// but not take turns, for pose/visualization scenarios). Default: register.
        /// </summary>
        public EntityBuilder NotRegisteredForTurns()
        {
            _registerForTurns = false;
            return this;
        }

        // ---------- Positioning terminals (trigger spawn + return Entity) ----------

        /// <summary>
        /// Spawn at absolute zone cell (x, y). Returns the spawned entity or null if
        /// the spawn failed (blueprint missing, cell out of bounds, etc.).
        /// </summary>
        public Entity At(int x, int y) => SpawnAt(x, y);

        /// <summary>
        /// Spawn at a cell offset from the live player's current position.
        /// For example, <c>AtPlayerOffset(3, 0)</c> spawns 3 cells east of the player.
        /// Returns the spawned entity or null on failure.
        /// </summary>
        public Entity AtPlayerOffset(int dx, int dy)
        {
            var playerPos = _ctx.Zone.GetEntityPosition(_ctx.Player);
            if (playerPos.x < 0)
            {
                Debug.LogWarning($"[Scenario] AtPlayerOffset: player has no position in zone — skipping spawn of '{_blueprintName}'");
                return null;
            }
            return SpawnAt(playerPos.x + dx, playerPos.y + dy);
        }

        // ---------- Internals ----------

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

            // Mirror GameBootstrap.RegisterCreaturesForTurns wiring BEFORE zone placement
            // so goals pushed via Initialize (e.g., AIAmbushPart) have a live Brain context.
            var brain = entity.GetPart<BrainPart>();
            if (brain != null)
            {
                brain.CurrentZone = _ctx.Zone;
                brain.Rng = new System.Random(_ctx.Rng.Next());
                brain.StartingCellX = x;
                brain.StartingCellY = y;
            }

            _ctx.Zone.AddEntity(entity, x, y);

            // Post-placement modifications
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

            // TurnManager registration: lets the creature actually take turns under
            // the live turn loop, same as if GameBootstrap had spawned it.
            if (_registerForTurns && entity.HasTag("Creature"))
            {
                _ctx.Turns.AddEntity(entity);
            }

            return entity;
        }
    }
}
