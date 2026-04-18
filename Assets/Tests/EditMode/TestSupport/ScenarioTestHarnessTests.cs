using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Behavior-verifying tests for <see cref="ScenarioTestHarness"/> itself.
    /// These exist to prevent regressions in the harness from silently breaking
    /// every downstream scenario test fixture.
    /// </summary>
    [TestFixture]
    public class ScenarioTestHarnessTests
    {
        [Test]
        public void Construction_LoadsBlueprintsAndInitsFactionManager()
        {
            using (var harness = new ScenarioTestHarness())
            {
                // Factory resolved — Snapjaw is a known blueprint.
                var snapjaw = harness.Factory.CreateEntity("Snapjaw");
                Assert.IsNotNull(snapjaw, "Factory should resolve known blueprints after construction.");

                // FactionManager initialized — Snapjaws faction exists in the registry.
                var factions = FactionManager.GetAllFactions();
                Assert.Contains("Snapjaws", factions,
                    "FactionManager should be initialized with Snapjaws faction post-construction.");
            }
        }

        [Test]
        public void CreateContext_DefaultProducesStubPlayerAt40_12()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx = harness.CreateContext();

                Assert.IsNotNull(ctx.PlayerEntity);
                Assert.IsTrue(ctx.PlayerEntity.HasTag("Player"));
                Assert.IsTrue(ctx.PlayerEntity.HasTag("Creature"));
                var pos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
                Assert.AreEqual((40, 12), (pos.x, pos.y));
            }
        }

        [Test]
        public void CreateContext_AcceptsCustomPlayerPosition()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx = harness.CreateContext(playerX: 10, playerY: 5);
                var pos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
                Assert.AreEqual((10, 5), (pos.x, pos.y));
            }
        }

        [Test]
        public void CreateContext_WithRealPlayerBlueprint_HasMutationsPart()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx = harness.CreateContext(playerBlueprint: "Player");
                Assert.IsNotNull(ctx.PlayerEntity.GetPart<MutationsPart>(),
                    "Real Player blueprint should include MutationsPart (the stub doesn't).");
                Assert.IsNotNull(ctx.PlayerEntity.GetPart<InventoryPart>(),
                    "Real Player blueprint should include InventoryPart.");
            }
        }

        [Test]
        public void CreateContext_EachCallReturnsFreshZoneAndTurnManager()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx1 = harness.CreateContext();
                var ctx2 = harness.CreateContext();

                Assert.AreNotSame(ctx1.Zone, ctx2.Zone, "Each CreateContext should produce a fresh Zone.");
                Assert.AreNotSame(ctx1.Turns, ctx2.Turns, "Each CreateContext should produce a fresh TurnManager.");
                Assert.AreNotSame(ctx1.PlayerEntity, ctx2.PlayerEntity, "Each CreateContext should produce a fresh player.");
            }
        }

        [Test]
        public void CreateContext_SharesFactoryAcrossCalls()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx1 = harness.CreateContext();
                var ctx2 = harness.CreateContext();

                // The factory is the expensive thing — it MUST be shared.
                Assert.AreSame(ctx1.Factory, ctx2.Factory,
                    "Factory should be shared across contexts from the same harness.");
            }
        }

        [Test]
        public void CreateContext_WithUnknownBlueprint_Throws()
        {
            using (var harness = new ScenarioTestHarness())
            {
                // Unknown blueprint: EntityFactory logs + returns null; harness should
                // fail loudly rather than hand back a context with a null player.
                UnityEngine.TestTools.LogAssert.Expect(
                    UnityEngine.LogType.Error,
                    "EntityFactory: unknown blueprint 'NotARealBlueprint'");
                Assert.Throws<System.InvalidOperationException>(
                    () => harness.CreateContext(playerBlueprint: "NotARealBlueprint"));
            }
        }

        [Test]
        public void RngSeed_IsDeterministic()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var ctx1 = harness.CreateContext(rngSeed: 777);
                var ctx2 = harness.CreateContext(rngSeed: 777);

                int r1 = ctx1.Rng.Next(100000);
                int r2 = ctx2.Rng.Next(100000);
                Assert.AreEqual(r1, r2, "Same seed should produce the same RNG sequence.");
            }
        }
    }
}
