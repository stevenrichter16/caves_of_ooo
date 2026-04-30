using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Integration tests for HouseDramaZoneBuilder NPC spawning with real blueprints.
    /// Verifies that living NPC roles are placed in the zone, receive HouseDramaPart
    /// identity data, and have their ConversationPart.ConversationID stamped correctly.
    ///
    /// Uses the same blueprint-loading pattern as ConversationPartActionTests.
    /// </summary>
    public class HouseDramaZoneBuilderNpcTests
    {
        private const string DramaId = "NpcSpawnTestDrama";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();

            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        // ── SilencedHelper → Scribe blueprint ────────────────────────────────

        [Test]
        public void BuildZone_SilencedHelperRole_SpawnsNpcWithHouseDramaPart()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "scribe1", Role = "SilencedHelper", Alive = true }
                }
            };
            HouseDramaLoader.Register(drama);

            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(zone, _factory, new System.Random(42));

            Entity spawnedNpc = FindDramaNpc(zone);
            Assert.IsNotNull(spawnedNpc, "Expected a drama NPC to be placed in the zone.");

            var part = spawnedNpc.GetPart<HouseDramaPart>();
            Assert.IsNotNull(part, "Spawned NPC should have HouseDramaPart.");
            Assert.AreEqual(DramaId,         part.DramaID);
            Assert.AreEqual("SilencedHelper", part.NpcRole);
            Assert.AreEqual("scribe1",        part.NpcId);
        }

        [Test]
        public void BuildZone_SilencedHelperRole_StampsConversationId()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "scribe1", Role = "SilencedHelper", Alive = true }
                }
            };
            HouseDramaLoader.Register(drama);

            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(zone, _factory, new System.Random(42));

            Entity spawnedNpc = FindDramaNpc(zone);
            Assert.IsNotNull(spawnedNpc, "Expected a drama NPC to be placed in the zone.");

            var conv = spawnedNpc.GetPart<ConversationPart>();
            Assert.IsNotNull(conv, "Spawned NPC should have ConversationPart.");
            Assert.AreEqual($"Drama_{DramaId}_SilencedHelper", conv.ConversationID);
        }

        // ── NamedAntagonist → Merchant blueprint ──────────────────────────────

        [Test]
        public void BuildZone_NamedAntagonistRole_SpawnsNpcWithHouseDramaPart()
        {
            var drama = new HouseDramaData
            {
                ID = DramaId,
                NpcRoles = new List<NpcRoleData>
                {
                    new NpcRoleData { Id = "villain", Role = "NamedAntagonist", Alive = true }
                }
            };
            HouseDramaLoader.Register(drama);

            var zone = new Zone("T");
            new HouseDramaZoneBuilder(DramaId)
                .BuildZone(zone, _factory, new System.Random(42));

            Entity spawnedNpc = FindDramaNpc(zone);
            Assert.IsNotNull(spawnedNpc, "Expected a drama NPC to be placed in the zone.");

            var part = spawnedNpc.GetPart<HouseDramaPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("NamedAntagonist", part.NpcRole);
            Assert.AreEqual("villain", part.NpcId);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static Entity FindDramaNpc(Zone zone)
        {
            Entity found = null;
            zone.ForEachCell((cell, x, y) =>
            {
                if (found != null) return;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].GetPart<HouseDramaPart>() != null)
                    {
                        found = cell.Objects[i];
                        return;
                    }
                }
            });
            return found;
        }
    }
}
