using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M1 TDD tests: singleton world entity exists in GameSessionState and
    /// round-trips through save/load with its "WorldEntity" tag intact.
    /// </summary>
    public class WorldEntityTests
    {
        // --- Capture ---

        [Test]
        public void Capture_WithWorldEntity_SetsWorldOnState()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: world);

            Assert.AreSame(world, state.World);
        }

        [Test]
        public void Capture_WithNullWorld_SetsWorldNullOnState()
        {
            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: null);

            Assert.IsNull(state.World);
        }

        // --- Save / Load round-trip ---

        [Test]
        public void WorldEntity_SurvivesSaveLoadRoundTrip()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: world);

            GameSessionState loaded = RoundTrip(state);

            Assert.IsNotNull(loaded.World);
        }

        [Test]
        public void WorldEntity_TagPreservedAfterRoundTrip()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: world);

            GameSessionState loaded = RoundTrip(state);

            Assert.IsTrue(loaded.World.HasTag("WorldEntity"),
                "World entity should have 'WorldEntity' tag after round-trip");
        }

        [Test]
        public void WorldEntity_BlueprintNamePreservedAfterRoundTrip()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: world);

            GameSessionState loaded = RoundTrip(state);

            Assert.AreEqual("World", loaded.World.BlueprintName);
        }

        [Test]
        public void WorldEntity_NullWorld_RoundTripsAsNull()
        {
            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: null,
                world: null);

            GameSessionState loaded = RoundTrip(state);

            Assert.IsNull(loaded.World);
        }

        // --- Identity: world entity is distinct from player ---

        [Test]
        public void WorldAndPlayer_AreDistinctAfterRoundTrip()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");
            var player = new Entity { BlueprintName = "Player" };

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null,
                turnManager: null,
                player: player,
                world: world);

            GameSessionState loaded = RoundTrip(state);

            Assert.AreNotSame(loaded.World, loaded.Player);
            Assert.IsNotNull(loaded.Player);
        }

        // --- Helper ---

        private static GameSessionState RoundTrip(GameSessionState state)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            state.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            return GameSessionState.Load(reader);
        }
    }
}
