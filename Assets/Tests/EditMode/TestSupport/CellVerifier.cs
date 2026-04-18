using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3c — per-cell assertion chain. Obtained via
    /// <c>ctx.Verify().Cell(x, y)</c>. Every method asserts state on the
    /// wrapped cell and returns <c>this</c> for chaining.
    /// </summary>
    public sealed class CellVerifier
    {
        private readonly ScenarioVerifier _root;
        private readonly int _x;
        private readonly int _y;

        internal CellVerifier(ScenarioVerifier root, int x, int y)
        {
            _root = root;
            _x = x;
            _y = y;
        }

        /// <summary>Return to the root verifier.</summary>
        public ScenarioVerifier Back() => _root;

        private Cell Cell => _root.Ctx.Zone.GetCell(_x, _y);

        // =========================================================
        // Contents
        // =========================================================

        /// <summary>
        /// Assert the cell contains at least one entity matching
        /// <paramref name="blueprintName"/>.
        /// </summary>
        public CellVerifier ContainsBlueprint(string blueprintName)
        {
            var cell = Cell;
            if (cell == null)
                Assert.Fail($"Verify.Cell({_x},{_y}).ContainsBlueprint: cell is null.");
            foreach (var entity in cell.Objects)
                if (entity != null && entity.BlueprintName == blueprintName) return this;
            Assert.Fail(
                $"Verify.Cell({_x},{_y}).ContainsBlueprint('{blueprintName}'): " +
                $"no matching entity in cell ({cell.Objects.Count} other objects present).");
            return this;
        }

        /// <summary>
        /// Assert the cell has no entities at all. Note this fails even if the
        /// only objects in the cell are terrain (Wall/Floor/Terrain tags). For
        /// "no creatures" use <see cref="HasNoEntityWithTag"/> with "Creature".
        /// </summary>
        public CellVerifier IsEmpty()
        {
            var cell = Cell;
            if (cell == null) return this; // null cell is trivially empty
            if (cell.Objects.Count > 0)
                Assert.Fail(
                    $"Verify.Cell({_x},{_y}).IsEmpty: expected empty, " +
                    $"found {cell.Objects.Count} object(s).");
            return this;
        }

        /// <summary>Assert the cell is passable (nothing with <c>Solid</c> tag).</summary>
        public CellVerifier IsPassable()
        {
            var cell = Cell;
            if (cell == null)
                Assert.Fail($"Verify.Cell({_x},{_y}).IsPassable: cell is null.");
            if (!cell.IsPassable())
                Assert.Fail($"Verify.Cell({_x},{_y}).IsPassable: cell is solid.");
            return this;
        }

        /// <summary>Assert the cell is solid (contains an entity with <c>Solid</c> tag).</summary>
        public CellVerifier IsSolid()
        {
            var cell = Cell;
            if (cell == null)
                Assert.Fail($"Verify.Cell({_x},{_y}).IsSolid: cell is null.");
            if (!cell.IsSolid())
                Assert.Fail($"Verify.Cell({_x},{_y}).IsSolid: cell is passable.");
            return this;
        }

        /// <summary>
        /// Assert no entity in this cell has the given tag KEY. Handy negation
        /// for cleared-cell verification.
        /// </summary>
        public CellVerifier HasNoEntityWithTag(string tag)
        {
            var cell = Cell;
            if (cell == null) return this;
            foreach (var entity in cell.Objects)
                if (entity != null && entity.HasTag(tag))
                    Assert.Fail(
                        $"Verify.Cell({_x},{_y}).HasNoEntityWithTag('{tag}'): " +
                        $"found '{entity.BlueprintName}' carrying the tag.");
            return this;
        }
    }
}
