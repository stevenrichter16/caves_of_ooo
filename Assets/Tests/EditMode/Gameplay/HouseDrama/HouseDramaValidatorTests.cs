using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for HouseDramaData.Validate().
    ///
    /// RED: HouseDramaData.Validate() does not exist yet. All tests in this file
    /// are expected to fail until the method is added to HouseDramaData.
    ///
    /// Validation rules exercised:
    ///   1. Drama with empty ID → error
    ///   2. PressurePoint with no paths → error
    ///   3. NPC with unknown role string → error
    ///   4. Dead-role NPC (FoundationalDead or LostDead) without a matching
    ///      MemorialAct → error
    ///   5. EndState with ID not in the canonical set → error
    ///   6. Fully valid drama → empty error list
    /// </summary>
    public class HouseDramaValidatorTests
    {
        private static readonly List<string> ValidRoles = new List<string>
        {
            "FoundationalDead", "LostDead", "DiminishedHead",
            "RisingInheritor", "NamedAntagonist", "SilencedHelper"
        };

        private static readonly List<string> ValidEndStateIds = new List<string>
        {
            "Restored", "TransformedA", "TransformedB", "Extinct", "Corrupted"
        };

        // Helper: builds a drama that passes all validation rules.
        private static HouseDramaData BuildValidDrama() => new HouseDramaData
        {
            ID = "ValidDrama",
            NpcRoles = new List<NpcRoleData>
            {
                new NpcRoleData { Id = "heir",    Role = "RisingInheritor",  Alive = true  },
                new NpcRoleData { Id = "founder", Role = "FoundationalDead", Alive = false },
            },
            MemorialActs = new List<MemorialActData>
            {
                new MemorialActData { SubjectId = "founder" }
            },
            PressurePoints = new List<PressurePointData>
            {
                new PressurePointData
                {
                    Id = "PP1",
                    Paths = new List<PathData> { new PathData { Id = "PathA" } }
                }
            },
            EndStates = new List<EndStateData>
            {
                new EndStateData { Id = "Restored" }
            }
        };

        // ── Rule 1: empty ID ──────────────────────────────────────────────────

        [Test]
        public void Validate_EmptyId_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.ID = "";

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0, "Expected at least one error for empty ID.");
        }

        [Test]
        public void Validate_NullId_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.ID = null;

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0, "Expected at least one error for null ID.");
        }

        // ── Rule 2: PressurePoint with no paths ───────────────────────────────

        [Test]
        public void Validate_PressurePointWithNoPaths_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.PressurePoints[0].Paths = new List<PathData>();

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error when PressurePoint has an empty Paths list.");
        }

        [Test]
        public void Validate_PressurePointWithNullPaths_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.PressurePoints[0].Paths = null;

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error when PressurePoint has null Paths.");
        }

        // ── Rule 3: unknown NPC role ──────────────────────────────────────────

        [Test]
        public void Validate_NpcWithUnknownRole_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.NpcRoles.Add(new NpcRoleData { Id = "ghost", Role = "UnknownRole", Alive = true });

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error for NPC with an unrecognized role string.");
        }

        [Test]
        public void Validate_NpcWithEmptyRole_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.NpcRoles.Add(new NpcRoleData { Id = "nobody", Role = "", Alive = true });

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error for NPC with an empty role string.");
        }

        // ── Rule 4: dead-role NPC missing MemorialAct ─────────────────────────

        [Test]
        public void Validate_FoundationalDeadWithoutMemorialAct_ReturnsError()
        {
            var drama = BuildValidDrama();
            // Remove the MemorialAct for "founder"
            drama.MemorialActs.Clear();

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error when FoundationalDead NPC has no MemorialAct.");
        }

        [Test]
        public void Validate_LostDeadWithoutMemorialAct_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.NpcRoles.Add(new NpcRoleData { Id = "lost_one", Role = "LostDead", Alive = false });
            // No MemorialAct for "lost_one"

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error when LostDead NPC has no MemorialAct.");
        }

        // ── Rule 5: invalid EndState ID ───────────────────────────────────────

        [Test]
        public void Validate_EndStateWithInvalidId_ReturnsError()
        {
            var drama = BuildValidDrama();
            drama.EndStates.Add(new EndStateData { Id = "NotAValidEndState" });

            var errors = drama.Validate();

            Assert.IsTrue(errors.Count > 0,
                "Expected error for EndState with an ID not in the canonical set.");
        }

        // ── Rule 6: fully valid drama ─────────────────────────────────────────

        [Test]
        public void Validate_ValidDrama_ReturnsEmptyErrorList()
        {
            var drama = BuildValidDrama();

            var errors = drama.Validate();

            Assert.AreEqual(0, errors.Count,
                "Expected no validation errors for a fully-correct drama.");
        }

        [Test]
        public void Validate_ReturnsListNotNull()
        {
            var drama = BuildValidDrama();
            drama.ID = "";

            var errors = drama.Validate();

            Assert.IsNotNull(errors, "Validate() should never return null.");
        }
    }
}
