using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3c — root verifier returned by <see cref="ScenarioContextExtensions.Verify"/>.
    ///
    /// Holds a reference to the test's <see cref="ScenarioContext"/> and offers
    /// two things:
    /// 1. Global state assertions that apply across the whole zone
    ///    (<see cref="EntityCount"/>, <see cref="PlayerIsAlive"/>)
    /// 2. Factories for narrower sub-verifiers (<see cref="Entity"/>,
    ///    <see cref="Player"/>, <see cref="Cell"/>)
    ///
    /// Sub-verifiers each expose <c>Back()</c> returning this root so a single
    /// fluent chain can assert entity state, then player state, then zone state.
    ///
    /// Failure semantics: every assertion method calls NUnit's
    /// <see cref="NUnit.Framework.Assert"/> under the hood. The NUnit runner sees
    /// normal test failures with readable messages — no custom exception types.
    ///
    /// Example:
    /// <code>
    /// ctx.Verify()
    ///     .Entity(warden)
    ///         .IsAt(10, 10)
    ///         .HasHpFraction(0.20f)
    ///     .Back()
    ///     .Player()
    ///         .HasMutation("FireBoltMutation")
    ///     .Back()
    ///     .EntityCount(withTag: "Creature", expected: 3);
    /// </code>
    /// </summary>
    public sealed class ScenarioVerifier
    {
        internal readonly ScenarioContext Ctx;

        internal ScenarioVerifier(ScenarioContext ctx) { Ctx = ctx; }

        // =========================================================
        // Sub-verifier factories
        // =========================================================

        /// <summary>Narrow to a specific entity for entity-level assertions.</summary>
        public EntityVerifier Entity(Entity entity)
        {
            if (entity == null)
                Assert.Fail("Verify.Entity: entity is null. Can't assert on a null entity.");
            return new EntityVerifier(this, entity);
        }

        /// <summary>Narrow to the live player for player-specific assertions.</summary>
        public PlayerVerifier Player() => new PlayerVerifier(this);

        /// <summary>Narrow to a specific zone cell for cell-level assertions.</summary>
        public CellVerifier Cell(int x, int y)
        {
            if (!Ctx.Zone.InBounds(x, y))
                Assert.Fail($"Verify.Cell: ({x},{y}) is out of zone bounds ({Zone.Width}x{Zone.Height}).");
            return new CellVerifier(this, x, y);
        }

        // =========================================================
        // Global assertions
        // =========================================================

        /// <summary>
        /// Assert that the zone contains exactly <paramref name="expected"/>
        /// entities carrying the tag KEY <paramref name="withTag"/>. Remember
        /// tag KEYS (not values) — faction info uses key <c>"Faction"</c>, not
        /// <c>"Snapjaws"</c>.
        /// </summary>
        public ScenarioVerifier EntityCount(string withTag, int expected)
        {
            int actual = Ctx.Zone.GetEntitiesWithTag(withTag).Count;
            Assert.AreEqual(expected, actual,
                $"Verify.EntityCount(withTag=\"{withTag}\"): expected {expected}, got {actual}.");
            return this;
        }

        /// <summary>
        /// Assert the player is alive (in the zone, Hitpoints > 0).
        /// </summary>
        public ScenarioVerifier PlayerIsAlive()
        {
            var zone = Ctx.Zone;
            var player = Ctx.PlayerEntity;
            if (zone.GetEntityCell(player) == null)
                Assert.Fail("Verify.PlayerIsAlive: player is not in the zone (was removed — treated as dead).");
            int hp = player.GetStatValue("Hitpoints", 0);
            Assert.Greater(hp, 0, "Verify.PlayerIsAlive: player HP dropped to 0 or below.");
            return this;
        }

        // TurnCount() deliberately not provided: AdvanceTurns doesn't increment
        // TurnManager.TickCount (only production Tick/ProcessUntilPlayerTurn do),
        // so a Verify().TurnCount() would be useless for the common scenario-test
        // flow and a footgun for anyone expecting symmetry with AdvanceTurns.
        // Tests that need tick counting should drive TurnManager.Tick directly
        // and assert on TickCount inline.
    }

    /// <summary>
    /// Phase 3c — entry-point extension. Add <c>ctx.Verify()</c> as the start of
    /// any assertion chain. Kept as an extension (not a property on
    /// <see cref="ScenarioContext"/>) so runtime builds don't pick up the API.
    /// </summary>
    public static class VerifyExtension
    {
        public static ScenarioVerifier Verify(this ScenarioContext ctx)
        {
            if (ctx == null) throw new System.ArgumentNullException(nameof(ctx));
            return new ScenarioVerifier(ctx);
        }
    }
}
