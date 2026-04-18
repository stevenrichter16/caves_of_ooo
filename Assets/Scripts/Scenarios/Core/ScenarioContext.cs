using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios.Builders;
using UnityEngine;

// PlayerBuilder is in CavesOfOoo.Scenarios.Builders (using above).

namespace CavesOfOoo.Scenarios
{
    /// <summary>
    /// The live game context passed to every scenario's <see cref="IScenario.Apply"/>.
    /// Holds references to the real <c>GameBootstrap</c>-owned <see cref="Zone"/>,
    /// <see cref="EntityFactory"/>, player, and <see cref="TurnManager"/> so scenarios
    /// can spawn, modify, and advance state without manual wiring.
    ///
    /// Construction:
    /// - <see cref="FromLiveGame"/> — finds the live <c>GameBootstrap</c> via
    ///   <c>Object.FindObjectsByType</c> and reflects out its private fields. Used by
    ///   <see cref="ScenarioRunner"/> on the after-bootstrap event and by MCP callers.
    ///
    /// Fluent entry points:
    /// - <see cref="Spawn(string)"/> — begin an entity spawn chain
    /// - <see cref="Player"/> — player modification builder (Phase 2c)
    /// - <see cref="World"/> — zone-modification builder (Phase 2d)
    ///
    /// Naming note (Phase 2c rename): <c>Player</c> is now the fluent
    /// <see cref="PlayerBuilder"/>. The raw player Entity — for
    /// <c>AsPersonalEnemyOf(...)</c>, position lookups, etc. — is accessible
    /// as <see cref="PlayerEntity"/>.
    /// </summary>
    public sealed class ScenarioContext
    {
        /// <summary>The live zone the player is in.</summary>
        public Zone Zone { get; }

        /// <summary>The live entity factory with all blueprints loaded.</summary>
        public EntityFactory Factory { get; }

        /// <summary>
        /// The live player <see cref="Entity"/>. Use this when you need the
        /// Entity itself (e.g., <c>.AsPersonalEnemyOf(ctx.PlayerEntity)</c>).
        /// For MODIFYING the player, use <see cref="Player"/> — the fluent builder.
        /// </summary>
        public Entity PlayerEntity { get; }

        /// <summary>The live turn manager driving NPC turns.</summary>
        public TurnManager Turns { get; }

        /// <summary>
        /// Deterministic RNG seeded per-scenario-run. Safe to use for random cell
        /// selection or other scenario-internal randomness without affecting the
        /// game's own RNG streams.
        /// </summary>
        public System.Random Rng { get; }

        public ScenarioContext(Zone zone, EntityFactory factory, Entity player, TurnManager turns, int rngSeed = 0)
        {
            Zone = zone ?? throw new ArgumentNullException(nameof(zone));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            PlayerEntity = player ?? throw new ArgumentNullException(nameof(player));
            Turns = turns ?? throw new ArgumentNullException(nameof(turns));
            Rng = new System.Random(rngSeed != 0 ? rngSeed : Guid.NewGuid().GetHashCode());
        }

        /// <summary>
        /// Construct a context from the currently-running game. Throws if
        /// <c>GameBootstrap</c> isn't present or hasn't completed its <c>Start()</c>.
        /// Call this from the <see cref="ScenarioRunner"/> after-bootstrap hook or
        /// from any injected code (MCP execute_code, PlayMode tests, etc.) that runs
        /// mid-session.
        /// </summary>
        public static ScenarioContext FromLiveGame()
        {
            var bootstraps = UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None);
            if (bootstraps.Length == 0)
                throw new InvalidOperationException(
                    "ScenarioContext.FromLiveGame: no GameBootstrap in the scene. " +
                    "Are you in Play mode?");

            var bs = bootstraps[0];
            var bsType = bs.GetType();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var zone = (Zone)bsType.GetField("_zone", flags)?.GetValue(bs);
            var factory = (EntityFactory)bsType.GetField("_factory", flags)?.GetValue(bs);
            var player = (Entity)bsType.GetField("_player", flags)?.GetValue(bs);
            var turns = (TurnManager)bsType.GetField("_turnManager", flags)?.GetValue(bs);

            if (zone == null || factory == null || player == null || turns == null)
            {
                throw new InvalidOperationException(
                    "ScenarioContext.FromLiveGame: GameBootstrap is present but one or more " +
                    "runtime refs are null. Has Start() finished? " +
                    $"zone={zone != null} factory={factory != null} player={player != null} turns={turns != null}");
            }

            return new ScenarioContext(zone, factory, player, turns);
        }

        /// <summary>
        /// Begin a fluent spawn chain for the given blueprint. The returned builder
        /// requires a positioning call (e.g. <c>.At(x, y)</c>, <c>.AtPlayerOffset(dx, dy)</c>)
        /// to actually spawn. Chain modifications like <c>.WithHp(fraction)</c> before
        /// the positioning terminal.
        /// </summary>
        public EntityBuilder Spawn(string blueprintName) =>
            new EntityBuilder(this, blueprintName);

        /// <summary>
        /// Fluent player-modification builder. Lazy-initialized on first access,
        /// cached for subsequent calls so repeated chaining touches the same instance.
        /// Example: <c>ctx.Player.Teleport(50, 20).SetHpMax().AddMutation("CalmMutation");</c>
        /// </summary>
        public PlayerBuilder Player
        {
            get
            {
                if (_playerBuilder == null)
                    _playerBuilder = new PlayerBuilder(this);
                return _playerBuilder;
            }
        }
        private PlayerBuilder _playerBuilder;

        /// <summary>
        /// Fluent zone-modification builder. Lazy-initialized on first access,
        /// cached for subsequent calls.
        /// Example: <c>ctx.World.PlaceObject("Chest").AtPlayerOffset(3, 0);</c>
        /// Example: <c>ctx.World.RemoveEntitiesWithTag("Creature");</c>
        /// </summary>
        public ZoneBuilder World
        {
            get
            {
                if (_worldBuilder == null)
                    _worldBuilder = new ZoneBuilder(this);
                return _worldBuilder;
            }
        }
        private ZoneBuilder _worldBuilder;

        /// <summary>
        /// Emit a tagged log line with a <c>[Scenario]</c> prefix and also push to the
        /// in-game <see cref="MessageLog"/> so visible feedback shows up in-game.
        /// </summary>
        public void Log(string message)
        {
            Debug.Log($"[Scenario] {message}");
            MessageLog.Add($"[Scenario] {message}");
        }
    }
}
