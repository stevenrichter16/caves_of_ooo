using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Builders
{
    /// <summary>
    /// Fluent zone-modification builder. Accessed via <see cref="ScenarioContext.World"/> —
    /// a lazy-cached singleton per context. Scenarios use this to place non-creature
    /// objects (furniture, items, decor), clear cells, and bulk-remove entities.
    ///
    /// Phase 2d scope (current):
    /// - <see cref="PlaceObject"/>: fluent placement chain for non-creature blueprints.
    ///   Unlike <see cref="ScenarioContext.Spawn(string)"/>, the result is NEVER
    ///   registered with the TurnManager and never has a BrainPart wired.
    /// - <see cref="ClearCell"/>: remove all non-terrain entities from a cell.
    /// - <see cref="RemoveEntitiesWithTag"/>: remove all entities in the zone carrying
    ///   a tag. Always skips <see cref="ScenarioContext.PlayerEntity"/> so scenarios
    ///   don't have to re-add the player after a mass clear.
    ///
    /// Post-Phase-2 extensions (not here yet):
    /// - <c>PlaceTile(x,y, "oil")</c> — would need a material-tile API on Zone/Cell.
    /// - <c>ApplyEffectToCell(x,y, effect)</c> — Phase 5.
    /// - Cross-zone manipulation (Gap A in the roadmap).
    ///
    /// Fail-soft contract: every method logs a warning and continues rather than
    /// throwing on unmet preconditions. A partial scenario beats a broken playtest.
    /// </summary>
    public sealed class ZoneBuilder
    {
        private readonly ScenarioContext _ctx;

        internal ZoneBuilder(ScenarioContext ctx) { _ctx = ctx; }

        /// <summary>
        /// Begin a placement chain for a non-creature blueprint. The returned
        /// <see cref="ObjectPlacer"/> requires a positioning terminal (<c>.At</c>
        /// or <c>.AtPlayerOffset</c>) to actually spawn.
        ///
        /// "Non-creature" is advisory, not enforced — if you pass a Snapjaw
        /// blueprint here, it'll spawn but WON'T take turns or run AI. That's
        /// almost never what you want; use <see cref="ScenarioContext.Spawn(string)"/>
        /// for creatures. PlaceObject is for chests, walls, furniture, items, decor.
        /// </summary>
        public ObjectPlacer PlaceObject(string blueprintName) =>
            new ObjectPlacer(_ctx, blueprintName);

        /// <summary>
        /// Remove every non-terrain entity from the cell at (x, y). Terrain means
        /// anything carrying the <c>Wall</c>, <c>Floor</c>, or <c>Terrain</c> tag.
        /// The <see cref="ScenarioContext.PlayerEntity"/> is always preserved.
        ///
        /// Use case: "I want to spawn something at (40, 12) but there's an NPC in
        /// the way — clear the cell first." Removed creatures are also de-registered
        /// from the <see cref="TurnManager"/>.
        /// </summary>
        public ZoneBuilder ClearCell(int x, int y)
        {
            if (!_ctx.Zone.InBounds(x, y))
            {
                Debug.LogWarning($"[Scenario] ClearCell({x},{y}): out of zone bounds — skipping.");
                return this;
            }
            var cell = _ctx.Zone.GetCell(x, y);
            if (cell == null) return this;

            // Copy to avoid mutating while iterating.
            var toRemove = new List<Entity>(cell.Objects.Count);
            foreach (var entity in cell.Objects)
            {
                if (ShouldPreserve(entity)) continue;
                toRemove.Add(entity);
            }

            foreach (var entity in toRemove)
                RemoveFromWorld(entity);

            return this;
        }

        /// <summary>
        /// Remove every entity in the zone carrying the given tag. Always preserves
        /// <see cref="ScenarioContext.PlayerEntity"/> and anything with a terrain tag
        /// (<c>Wall</c>, <c>Floor</c>, <c>Terrain</c>) — the latter is a safety net;
        /// normally you wouldn't pass <c>"Wall"</c> here anyway.
        ///
        /// Matches tag KEYS only, not values. Blueprints store faction as
        /// <c>{ Key: "Faction", Value: "Snapjaws" }</c> — so <c>RemoveEntitiesWithTag("Faction")</c>
        /// removes every faction-bearing entity, but <c>RemoveEntitiesWithTag("Snapjaws")</c>
        /// matches nothing. For faction-specific removal, add the faction name
        /// as an extra tag at spawn time, or filter manually via
        /// <c>zone.GetAllEntities()</c>.
        ///
        /// Common uses:
        /// - <c>RemoveEntitiesWithTag("Creature")</c> — empty the zone of all NPCs
        ///   for a controlled-baseline scenario.
        /// - <c>RemoveEntitiesWithTag("Furniture")</c> — strip decor without touching creatures.
        ///
        /// Removed creatures are also de-registered from the <see cref="TurnManager"/>
        /// so they won't tick again.
        /// </summary>
        public ZoneBuilder RemoveEntitiesWithTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                Debug.LogWarning("[Scenario] RemoveEntitiesWithTag: tagName is null/empty — skipping.");
                return this;
            }

            // GetAllEntities allocates a fresh list, which is exactly what we want
            // — we're about to mutate the zone's entity table.
            var matches = _ctx.Zone.GetAllEntities();
            int removed = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var entity = matches[i];
                if (!entity.HasTag(tagName)) continue;
                if (ShouldPreserve(entity)) continue;
                RemoveFromWorld(entity);
                removed++;
            }

            // Silent — the caller almost always knows the rough count already. If
            // they want confirmation, ctx.Log is right there.
            return this;
        }

        // =========================================================
        // Internals
        // =========================================================

        /// <summary>
        /// True if the entity must NEVER be removed by ZoneBuilder operations. Covers:
        /// - The live player entity
        /// - Terrain (Wall, Floor, or generic Terrain tag)
        /// </summary>
        private bool ShouldPreserve(Entity entity)
        {
            if (entity == null) return true;
            if (ReferenceEquals(entity, _ctx.PlayerEntity)) return true;
            if (entity.HasTag("Wall") || entity.HasTag("Floor") || entity.HasTag("Terrain"))
                return true;
            return false;
        }

        /// <summary>
        /// Remove an entity from the zone AND de-register it from the TurnManager
        /// if applicable. TurnManager.RemoveEntity is a no-op if the entity isn't
        /// registered, so it's safe to call unconditionally for creatures.
        /// </summary>
        private void RemoveFromWorld(Entity entity)
        {
            _ctx.Zone.RemoveEntity(entity);
            if (entity.HasTag("Creature"))
                _ctx.Turns.RemoveEntity(entity);
        }
    }

    /// <summary>
    /// Positioning-terminal helper for <see cref="ZoneBuilder.PlaceObject"/>.
    /// Mirrors <see cref="EntityBuilder"/>'s <c>.At</c>/<c>.AtPlayerOffset</c> but
    /// with a narrower surface — no creature-only modifiers (stats, brain wiring,
    /// goals). The spawned entity is NOT registered with the <see cref="TurnManager"/>.
    ///
    /// If you need the richer modifier pipeline (stats, inventory, goals), use
    /// <see cref="ScenarioContext.Spawn(string)"/> instead — it handles non-creature
    /// blueprints fine too, just without the ObjectPlacer type narrowing.
    /// </summary>
    public sealed class ObjectPlacer
    {
        private readonly ScenarioContext _ctx;
        private readonly string _blueprintName;

        internal ObjectPlacer(ScenarioContext ctx, string blueprintName)
        {
            _ctx = ctx;
            _blueprintName = blueprintName;
        }

        /// <summary>Place at absolute zone cell (x, y).</summary>
        public Entity At(int x, int y) => PlaceAt(x, y);

        /// <summary>Place at a cell offset from the live player's current position.</summary>
        public Entity AtPlayerOffset(int dx, int dy)
        {
            var playerPos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
            if (playerPos.x < 0)
            {
                Debug.LogWarning($"[Scenario] PlaceObject.AtPlayerOffset: player has no position — skipping '{_blueprintName}'.");
                return null;
            }
            return PlaceAt(playerPos.x + dx, playerPos.y + dy);
        }

        private Entity PlaceAt(int x, int y)
        {
            if (!_ctx.Zone.InBounds(x, y))
            {
                Debug.LogWarning($"[Scenario] PlaceObject target ({x},{y}) out of zone bounds for '{_blueprintName}' — skipping.");
                return null;
            }

            // Non-creature objects CAN legitimately be placed on non-passable cells
            // (e.g., decorative items on a wall). But anything Solid on a Solid cell
            // is almost certainly a bug, so we log for visibility — but still place.
            var cell = _ctx.Zone.GetCell(x, y);
            if (cell == null)
            {
                Debug.LogWarning($"[Scenario] PlaceObject target ({x},{y}) returned null cell for '{_blueprintName}' — skipping.");
                return null;
            }

            var entity = _ctx.Factory.CreateEntity(_blueprintName);
            if (entity == null)
            {
                Debug.LogWarning($"[Scenario] PlaceObject: blueprint '{_blueprintName}' not found — skipping.");
                return null;
            }

            // A Solid object on a Solid cell is almost always an accident — warn but
            // place. The blueprint might be intentionally a "blocking" prop.
            if (entity.HasTag("Solid") && cell.IsSolid())
            {
                Debug.LogWarning(
                    $"[Scenario] PlaceObject: '{_blueprintName}' is Solid and cell ({x},{y}) already has a Solid entity. Placing anyway.");
            }

            _ctx.Zone.AddEntity(entity, x, y);
            // Intentionally NOT registered for turns. If the blueprint has a Creature
            // tag by accident, it'll still be placed but inert — caller gets a mild
            // footgun but one that can be discovered via a visible motionless NPC.
            return entity;
        }
    }
}
