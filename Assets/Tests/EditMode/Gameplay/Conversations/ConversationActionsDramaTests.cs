using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for the House Drama conversation actions, focusing on the
    /// idempotency fix (Fix 3): StartDrama must not overwrite the runtime state
    /// of a drama that is already active.
    /// </summary>
    public class ConversationActionsDramaTests
    {
        private const string DramaId = "ActionTestDrama";

        private static HouseDramaData BuildTestDrama() => new HouseDramaData
        {
            ID = DramaId,
            Name = "Action Test Drama",
            PressurePoints = new List<PressurePointData>
            {
                new PressurePointData { Id = "PP1" }
            }
        };

        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            ConversationActions.Reset(); // forces RegisterDefaults on next Execute call
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();
        }

        [TearDown]
        public void Teardown()
        {
            FactionManager.Reset();
        }

        // ── StartDrama: happy path ────────────────────────────────────────────

        [Test]
        public void StartDrama_RegistersAndActivatesDramaFromLoader()
        {
            HouseDramaLoader.Register(BuildTestDrama());

            ConversationActions.Execute("StartDrama", null, null, DramaId);

            Assert.IsTrue(HouseDramaRuntime.IsDramaActive(DramaId));
        }

        [Test]
        public void StartDrama_WhenRegisteredByBootstrapButNotActive_ActivatesWithoutReRegistering()
        {
            // Simulate bootstrap: drama in both loader and runtime, but not yet active.
            var drama = BuildTestDrama();
            HouseDramaLoader.Register(drama);
            HouseDramaRuntime.RegisterDrama(drama);
            // Seed pre-activation state (e.g., from a future save/load path)
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "suspended");
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive(DramaId));

            ConversationActions.Execute("StartDrama", null, null, DramaId);

            // Drama is now active and the pre-existing state was preserved (not re-registered)
            Assert.IsTrue(HouseDramaRuntime.IsDramaActive(DramaId));
            Assert.AreEqual("suspended", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
        }

        // ── StartDrama: idempotency (Fix 3) ───────────────────────────────────

        [Test]
        public void StartDrama_WhenAlreadyActive_DoesNotResetPressurePointState()
        {
            HouseDramaLoader.Register(BuildTestDrama());
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved");

            // Fire StartDrama a second time — must be a no-op
            ConversationActions.Execute("StartDrama", null, null, DramaId);

            Assert.AreEqual("resolved", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
        }

        [Test]
        public void StartDrama_WhenAlreadyActive_DoesNotResetCorruptionScore()
        {
            HouseDramaLoader.Register(BuildTestDrama());
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AddCorruption(DramaId, 5);

            ConversationActions.Execute("StartDrama", null, null, DramaId);

            Assert.AreEqual(5, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void StartDrama_WhenAlreadyActive_DoesNotResetWitnessKnowledge()
        {
            HouseDramaLoader.Register(BuildTestDrama());
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.RevealWitnessFact(DramaId, "npc1", "revealed_fact");

            ConversationActions.Execute("StartDrama", null, null, DramaId);

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "revealed_fact"));
        }

        // ── StartDrama: graceful failure ──────────────────────────────────────

        [Test]
        public void StartDrama_WithUnknownDramaId_DoesNotActivateAndDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("StartDrama", null, null, "nonexistent_drama"));

            Assert.IsFalse(HouseDramaRuntime.IsDramaActive("nonexistent_drama"));
        }

        [Test]
        public void StartDrama_WithEmptyArg_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("StartDrama", null, null, ""));
        }

        [Test]
        public void StartDrama_WithNullArg_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("StartDrama", null, null, null));
        }
    }
}
