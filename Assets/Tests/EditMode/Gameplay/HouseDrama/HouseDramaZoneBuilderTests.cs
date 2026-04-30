using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for HouseDramaZoneBuilder, focusing on the idempotency guard
    /// introduced in Fix 1: BuildZone must not reset an already-active drama's
    /// runtime state when the zone is rebuilt or re-entered.
    /// </summary>
    public class HouseDramaZoneBuilderTests
    {
        private const string DramaId = "ZoneBuilderTestDrama";

        private static HouseDramaData BuildDramaWithNoNpcs() => new HouseDramaData
        {
            ID = DramaId,
            Name = "Zone Builder Test Drama",
            PressurePoints = new List<PressurePointData>
            {
                new PressurePointData { Id = "PP1" }
            }
            // NpcRoles intentionally absent — builder exits before any zone/factory access
        };

        [SetUp]
        public void Setup()
        {
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();
        }

        // ── Activation (first call) ───────────────────────────────────────────

        [Test]
        public void BuildZone_ActivatesDramaWhenNotYetActive()
        {
            HouseDramaLoader.Register(BuildDramaWithNoNpcs());

            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(HouseDramaRuntime.IsDramaActive(DramaId));
        }

        [Test]
        public void BuildZone_ActivatesDramaWhenRegisteredByBootstrapButNotActive()
        {
            // Simulates GameBootstrap: drama is registered in runtime but never activated.
            var drama = BuildDramaWithNoNpcs();
            HouseDramaLoader.Register(drama);
            HouseDramaRuntime.RegisterDrama(drama);
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive(DramaId));

            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(HouseDramaRuntime.IsDramaActive(DramaId));
        }

        [Test]
        public void BuildZone_ReturnsTrue()
        {
            HouseDramaLoader.Register(BuildDramaWithNoNpcs());

            bool result = new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(result);
        }

        // ── Idempotency guard (Fix 1) ─────────────────────────────────────────

        [Test]
        public void BuildZone_WhenDramaAlreadyActive_PreservesPressurePointState()
        {
            var drama = BuildDramaWithNoNpcs();
            HouseDramaLoader.Register(drama);
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved");

            // Simulate zone re-entry: BuildZone is called again.
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.AreEqual("resolved", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
        }

        [Test]
        public void BuildZone_WhenDramaAlreadyActive_PreservesWitnessKnowledge()
        {
            var drama = BuildDramaWithNoNpcs();
            HouseDramaLoader.Register(drama);
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.RevealWitnessFact(DramaId, "npc1", "secret_fact");

            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "secret_fact"));
        }

        [Test]
        public void BuildZone_WhenDramaAlreadyActive_PreservesCorruptionScore()
        {
            var drama = BuildDramaWithNoNpcs();
            HouseDramaLoader.Register(drama);
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AddCorruption(DramaId, 7);

            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.AreEqual(7, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void BuildZone_WhenCalledTwice_SecondCallPreservesAdvancedPressurePoint()
        {
            HouseDramaLoader.Register(BuildDramaWithNoNpcs());
            var builder = new HouseDramaZoneBuilder(DramaId);

            // First call: activates drama
            builder.BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved");

            // Second call (zone reload): must not reset the resolved state
            builder.BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.AreEqual("resolved", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
        }

        // ── Edge cases ────────────────────────────────────────────────────────

        [Test]
        public void BuildZone_WithEmptyDramaId_ReturnsTrueWithoutCrash()
        {
            bool result = new HouseDramaZoneBuilder("")
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(result);
        }

        [Test]
        public void BuildZone_WithUnknownDramaId_ReturnsTrueWithoutCrash()
        {
            bool result = new HouseDramaZoneBuilder("ghost_drama")
                .BuildZone(new Zone("T"), new EntityFactory(), new System.Random(0));

            Assert.IsTrue(result);
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive("ghost_drama"));
        }

        // ── NPC role filtering ────────────────────────────────────────────────

        [Test]
        public void BuildZone_WithOnlyDeadRoles_SpawnsNoEntities()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "founder", Role = "FoundationalDead", Alive = false },
                    new NpcRoleData { Id = "lost",    Role = "LostDead",          Alive = true  },
                }
            };
            HouseDramaLoader.Register(drama);

            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(zone, new EntityFactory(), new System.Random(0));

            int dramaEntityCount = 0;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.GetPart<HouseDramaPart>() != null)
                    dramaEntityCount++;

            Assert.AreEqual(0, dramaEntityCount);
        }

        [Test]
        public void BuildZone_WithNotAliveRole_SkipsThatNpc()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "heir", Role = "RisingInheritor", Alive = false },
                }
            };
            HouseDramaLoader.Register(drama);

            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(zone, new EntityFactory(), new System.Random(0));

            int dramaEntityCount = 0;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.GetPart<HouseDramaPart>() != null)
                    dramaEntityCount++;

            Assert.AreEqual(0, dramaEntityCount);
        }
    }
}
