using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Energy-based turn system faithful to Qud's design.
    ///
    /// Each entity has a Speed stat (default 100). Every tick, each entity gains
    /// energy equal to its Speed. When energy reaches the threshold (1000), the
    /// entity gets to take an action. Faster entities act more often.
    ///
    /// Qud's actual formula: entities gain Speed energy per tick, act at 1000.
    /// Speed 100 = normal. Speed 200 = twice as fast. Speed 50 = half speed.
    /// </summary>
    public class TurnManager
    {
        public const int ActionThreshold = 1000;
        public const int DefaultSpeed = 100;

        /// <summary>
        /// All entities participating in the turn order.
        /// </summary>
        private List<TurnEntry> _entries = new List<TurnEntry>();

        /// <summary>
        /// The entity currently taking a turn (null between turns).
        /// </summary>
        public Entity CurrentActor { get; private set; }

        /// <summary>
        /// Total ticks elapsed.
        /// </summary>
        public int TickCount { get; private set; }

        /// <summary>
        /// True if we're waiting for the current actor to choose an action.
        /// </summary>
        public bool WaitingForInput { get; private set; }

        private class TurnEntry
        {
            public Entity Entity;
            public int Energy;
        }

        /// <summary>
        /// Register an entity to participate in turns.
        /// </summary>
        public void AddEntity(Entity entity)
        {
            if (FindEntry(entity) != null) return;
            _entries.Add(new TurnEntry { Entity = entity, Energy = 0 });
        }

        /// <summary>
        /// Remove an entity from the turn order (e.g., on death).
        /// </summary>
        public void RemoveEntity(Entity entity)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Entity == entity)
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Get the speed of an entity (from its Speed stat, or default).
        /// </summary>
        public int GetSpeed(Entity entity)
        {
            return entity.GetStatValue("Speed", DefaultSpeed);
        }

        /// <summary>
        /// Get the current energy of an entity.
        /// </summary>
        public int GetEnergy(Entity entity)
        {
            var entry = FindEntry(entity);
            return entry?.Energy ?? 0;
        }

        /// <summary>
        /// Advance the simulation by one tick. All entities gain energy equal to
        /// their Speed. Returns the first entity that has enough energy to act,
        /// or null if no one is ready (shouldn't happen with standard speeds).
        /// </summary>
        public Entity Tick()
        {
            TickCount++;

            // Grant energy to all entities
            for (int i = 0; i < _entries.Count; i++)
            {
                int speed = GetSpeed(_entries[i].Entity);
                _entries[i].Energy += speed;
            }

            // Find the entity with the most energy that meets the threshold
            return FindNextActor();
        }

        /// <summary>
        /// Process turns until we need player input or run out of actors.
        /// Call this in the game loop. Returns the entity that needs input
        /// (typically the player), or null if all NPC turns were processed.
        /// </summary>
        public Entity ProcessUntilPlayerTurn()
        {
            // Keep processing until we hit a player or exhaust ready actors
            while (true)
            {
                Entity actor = FindNextActor();

                if (actor == null)
                {
                    // No one has enough energy — tick to grant more
                    if (_entries.Count == 0) return null;
                    actor = Tick();
                    if (actor == null) continue;
                }

                CurrentActor = actor;

                // Qud-style pre-action event seam: status effects and other parts can
                // block the action before AI/input executes.
                Zone actorZone = ResolveActorZone(actor);
                var beginTakeAction = GameEvent.New("BeginTakeAction");
                if (actorZone != null)
                    beginTakeAction.SetParameter("Zone", (object)actorZone);

                if (!actor.FireEvent(beginTakeAction))
                {
                    EndTurn(actor, actorZone);
                    continue;
                }

                // If it's the player, pause and wait for input
                if (actor.HasTag("Player"))
                {
                    WaitingForInput = true;
                    return actor;
                }

                // NPC: fire a TakeTurn event so AI parts can decide actions
                var turnEvent = GameEvent.New("TakeTurn");
                turnEvent.SetParameter("BeginTakeActionProcessed", true);
                if (actorZone != null)
                    turnEvent.SetParameter("Zone", (object)actorZone);
                actor.FireEvent(turnEvent);

                EndTurn(actor, actorZone);
            }
        }

        /// <summary>
        /// Called after the player (or any actor) completes their action.
        /// Spends their energy and resumes processing.
        /// </summary>
        public void EndTurn(Entity actor, Zone zone = null)
        {
            // Fire EndTurn event so parts can react (cooldown ticking, regeneration, etc.)
            var endTurn = GameEvent.New("EndTurn");
            if (zone != null)
                endTurn.SetParameter("Zone", (object)zone);
            actor.FireEvent(endTurn);

            SpendEnergy(actor);
            CurrentActor = null;
            WaitingForInput = false;
        }

        /// <summary>
        /// Spend the action threshold worth of energy from an entity.
        /// </summary>
        private void SpendEnergy(Entity entity)
        {
            var entry = FindEntry(entity);
            if (entry != null)
                entry.Energy -= ActionThreshold;
        }

        /// <summary>
        /// Find the entity with the highest energy that meets the action threshold.
        /// Ties broken by speed (faster acts first), then by insertion order.
        /// </summary>
        private Entity FindNextActor()
        {
            TurnEntry best = null;
            int bestEnergy = ActionThreshold - 1;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Energy > bestEnergy)
                {
                    bestEnergy = entry.Energy;
                    best = entry;
                }
                else if (entry.Energy == bestEnergy && best != null)
                {
                    // Tie-break: higher speed acts first
                    if (GetSpeed(entry.Entity) > GetSpeed(best.Entity))
                        best = entry;
                }
            }

            return best?.Entity;
        }

        private TurnEntry FindEntry(Entity entity)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Entity == entity)
                    return _entries[i];
            }
            return null;
        }

        private Zone ResolveActorZone(Entity actor)
        {
            if (actor == null)
                return null;

            var brain = actor.GetPart<BrainPart>();
            if (brain != null && brain.CurrentZone != null)
                return brain.CurrentZone;

            return null;
        }

        /// <summary>
        /// Number of registered entities.
        /// </summary>
        public int EntityCount => _entries.Count;
    }
}
