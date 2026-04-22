using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3a — fixture-scope test harness for scenario-based EditMode tests.
    ///
    /// Encapsulates the boilerplate every scenario test file was re-implementing:
    /// <list type="bullet">
    /// <item><see cref="FactionManager"/> initialization + reset</item>
    /// <item><see cref="EntityFactory"/> with blueprints loaded from the live JSON</item>
    /// <item><see cref="ScenarioContext"/> creation with a minimal stub player (or a
    /// real Player blueprint) in a fresh <see cref="Zone"/> and <see cref="TurnManager"/></item>
    /// </list>
    ///
    /// Usage pattern — construct once per fixture (blueprint load is expensive),
    /// spin up fresh contexts per test:
    /// <code>
    /// [TestFixture]
    /// public class WoundedWardenTests
    /// {
    ///     private static ScenarioTestHarness _harness;
    ///     [OneTimeSetUp] public void Setup() => _harness = new ScenarioTestHarness();
    ///     [OneTimeTearDown] public void Teardown() => _harness?.Dispose();
    ///
    ///     [Test]
    ///     public void MyScenario_DoesTheThing()
    ///     {
    ///         var ctx = _harness.CreateContext();
    ///         new MyScenario().Apply(ctx);
    ///         // ... assertions ...
    ///     }
    /// }
    /// </code>
    ///
    /// Design boundary: this class lives in the test assembly (not runtime). The
    /// blueprint JSON load relies on <see cref="Application.dataPath"/>, which only
    /// resolves correctly in-editor. Keeping the harness test-only prevents this
    /// limitation from leaking into runtime builds.
    /// </summary>
    public sealed class ScenarioTestHarness : IDisposable
    {
        /// <summary>
        /// Shared blueprint-loaded factory. One instance per harness — amortize the
        /// ~100ms load cost across every test in a fixture.
        /// </summary>
        public EntityFactory Factory { get; }

        /// <summary>
        /// Initialize the harness. Call once per fixture from <c>[OneTimeSetUp]</c>.
        /// Blueprints are loaded from <c>Assets/Resources/Content/Blueprints/Objects.json</c>.
        /// </summary>
        public ScenarioTestHarness()
        {
            FactionManager.Initialize();
            Factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            Factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        /// <summary>
        /// Build a fresh <see cref="ScenarioContext"/> for a single test.
        ///
        /// The zone is empty except for a player placed at <paramref name="playerX"/>,
        /// <paramref name="playerY"/>. By default the player is a minimal stub
        /// (<c>Creature</c> + <c>Player</c> tags, a Hitpoints stat) — sufficient for
        /// most scenario tests. Pass <paramref name="playerBlueprint"/> (e.g. "Player")
        /// when you need the real player blueprint's parts (InventoryPart,
        /// MutationsPart, LightSourcePart, etc.).
        /// </summary>
        /// <param name="rngSeed">Deterministic seed for <see cref="ScenarioContext.Rng"/>.</param>
        /// <param name="playerBlueprint">
        /// If non-null, constructs the player via <see cref="EntityFactory.CreateEntity(string)"/>.
        /// If null, builds a minimal stub entity sufficient for builder-level tests.
        /// </param>
        /// <param name="playerX">Starting X of the player in the fresh zone.</param>
        /// <param name="playerY">Starting Y of the player in the fresh zone.</param>
        /// <param name="zoneId">Optional debug-only zone name.</param>
        public ScenarioContext CreateContext(
            int rngSeed = 12345,
            string playerBlueprint = null,
            int playerX = 40,
            int playerY = 12,
            string zoneId = "TestZone")
        {
            var zone = new Zone(zoneId);

            Entity player = playerBlueprint != null
                ? Factory.CreateEntity(playerBlueprint)
                : BuildStubPlayer();

            if (player == null)
            {
                throw new InvalidOperationException(
                    $"ScenarioTestHarness.CreateContext: blueprint '{playerBlueprint}' " +
                    "could not be resolved by the EntityFactory. Check the blueprint name.");
            }

            zone.AddEntity(player, playerX, playerY);
            var tm = new TurnManager();
            return new ScenarioContext(zone, Factory, player, tm, rngSeed);
        }

        /// <summary>
        /// Release fixture-scope resources. Call from <c>[OneTimeTearDown]</c>.
        /// Currently resets <see cref="FactionManager"/> static state so tests don't
        /// leak faction config into later fixtures.
        /// </summary>
        public void Dispose()
        {
            FactionManager.Reset();
        }

        /// <summary>
        /// Build a minimal player-shaped entity: Creature/Player tags, Hitpoints stat
        /// at 100/100. Deliberately excludes InventoryPart, MutationsPart, BrainPart,
        /// etc. — tests that need those should use <paramref name="playerBlueprint"/>
        /// = "Player" on <see cref="CreateContext"/>.
        /// </summary>
        private static Entity BuildStubPlayer()
        {
            var player = new Entity { BlueprintName = "TestPlayer" };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints",
                BaseValue = 100,
                Max = 100,
                Min = 0
            };
            return player;
        }
    }
}
