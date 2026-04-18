using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3c — per-entity assertion chain. Obtained via
    /// <c>ctx.Verify().Entity(e)</c>. Every method asserts state on the wrapped
    /// entity and returns <c>this</c> for chaining. Use <see cref="Back"/> to
    /// step back to the root verifier.
    ///
    /// Naming helper: assertion failures reference the entity by DisplayName
    /// (falling back to BlueprintName) so "Expected warden at (10,10), got
    /// (13,10)" reads naturally without the caller having to supply labels.
    /// </summary>
    public sealed class EntityVerifier
    {
        private readonly ScenarioVerifier _root;
        private readonly Entity _entity;

        internal EntityVerifier(ScenarioVerifier root, Entity entity)
        {
            _root = root;
            _entity = entity;
        }

        /// <summary>Return to the root <see cref="ScenarioVerifier"/> for continued chaining.</summary>
        public ScenarioVerifier Back() => _root;

        /// <summary>Human-readable label used in failure messages.</summary>
        private string Label =>
            _entity.GetPart<RenderPart>()?.DisplayName
            ?? _entity.BlueprintName
            ?? "entity";

        // =========================================================
        // Position
        // =========================================================

        public EntityVerifier IsAt(int x, int y)
        {
            var pos = _root.Ctx.Zone.GetEntityPosition(_entity);
            if (pos.x == -1 && pos.y == -1)
                Assert.Fail($"Verify.Entity({Label}).IsAt({x},{y}): entity is not in the zone.");
            if (pos.x != x || pos.y != y)
                Assert.Fail(
                    $"Verify.Entity({Label}).IsAt({x},{y}): expected ({x},{y}), got ({pos.x},{pos.y}).");
            return this;
        }

        // =========================================================
        // Health / life
        // =========================================================

        /// <summary>
        /// Assert Hitpoints is approximately <paramref name="fraction"/> of Max.
        /// Default tolerance is 0.05 (5 percentage points) — wider than you'd
        /// think necessary, but required to absorb integer-rounding drift on
        /// small-Max entities (e.g., a Snapjaw with Max=15 rounds HalfHP to 8,
        /// effective fraction 0.533, drift 0.033).
        /// </summary>
        public EntityVerifier HasHpFraction(float fraction, float tolerance = 0.05f)
        {
            var stat = _entity.GetStat("Hitpoints");
            if (stat == null)
                Assert.Fail($"Verify.Entity({Label}).HasHpFraction: no Hitpoints stat.");
            float actual = (float)stat.BaseValue / stat.Max;
            if (System.Math.Abs(actual - fraction) > tolerance)
                Assert.Fail(
                    $"Verify.Entity({Label}).HasHpFraction({fraction}): " +
                    $"expected ~{fraction:F2} (±{tolerance:F2}), got {actual:F3} " +
                    $"({stat.BaseValue}/{stat.Max} HP).");
            return this;
        }

        /// <summary>Assert entity is alive (in zone AND Hitpoints > 0).</summary>
        public EntityVerifier IsAlive()
        {
            if (_root.Ctx.Zone.GetEntityCell(_entity) == null)
                Assert.Fail($"Verify.Entity({Label}).IsAlive: entity is not in the zone.");
            int hp = _entity.GetStatValue("Hitpoints", 0);
            if (hp <= 0)
                Assert.Fail($"Verify.Entity({Label}).IsAlive: Hitpoints dropped to {hp}.");
            return this;
        }

        // =========================================================
        // Stats
        // =========================================================

        public EntityVerifier HasStat(string statName, int value)
        {
            var stat = _entity.GetStat(statName);
            if (stat == null)
                Assert.Fail($"Verify.Entity({Label}).HasStat('{statName}'): stat not present on entity.");
            if (stat.BaseValue != value)
                Assert.Fail(
                    $"Verify.Entity({Label}).HasStat('{statName}', {value}): " +
                    $"expected {value}, got {stat.BaseValue}.");
            return this;
        }

        public EntityVerifier HasStatAtLeast(string statName, int min)
        {
            var stat = _entity.GetStat(statName);
            if (stat == null)
                Assert.Fail($"Verify.Entity({Label}).HasStatAtLeast('{statName}'): stat not present on entity.");
            if (stat.BaseValue < min)
                Assert.Fail(
                    $"Verify.Entity({Label}).HasStatAtLeast('{statName}', {min}): " +
                    $"expected ≥ {min}, got {stat.BaseValue}.");
            return this;
        }

        // =========================================================
        // Parts & goals & tags
        // =========================================================

        public EntityVerifier HasPartOfType<T>() where T : Part
        {
            if (_entity.GetPart<T>() == null)
                Assert.Fail($"Verify.Entity({Label}).HasPartOfType<{typeof(T).Name}>: part not attached.");
            return this;
        }

        public EntityVerifier HasGoalOnStack<T>() where T : GoalHandler
        {
            var brain = _entity.GetPart<BrainPart>();
            if (brain == null)
                Assert.Fail($"Verify.Entity({Label}).HasGoalOnStack<{typeof(T).Name}>: entity has no BrainPart.");
            if (!brain.HasGoal<T>())
                Assert.Fail(
                    $"Verify.Entity({Label}).HasGoalOnStack<{typeof(T).Name}>: " +
                    $"goal not found on stack (count = {brain.GoalCount}).");
            return this;
        }

        public EntityVerifier HasTag(string tag)
        {
            if (!_entity.HasTag(tag))
                Assert.Fail($"Verify.Entity({Label}).HasTag('{tag}'): tag not present.");
            return this;
        }
    }
}
