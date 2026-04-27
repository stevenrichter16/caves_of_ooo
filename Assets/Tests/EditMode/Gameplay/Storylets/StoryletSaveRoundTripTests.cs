using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2 TDD tests for StoryletPart riding through the GameSessionState
    /// save/load pipeline (i.e. SaveGraphSerializer.SaveEntityBody/LoadEntityBody
    /// path), and for the FormatVersion 2→3 bump.
    /// </summary>
    public class StoryletSaveRoundTripTests
    {
        // ── FormatVersion bump (M2) ───────────────────────────────────────────

        [Test]
        public void SaveWriter_FormatVersion_IsThree()
        {
            // Bumped 2→3 in M2 to accommodate the new StoryletPart on the
            // world entity. Per pre-1.0 dev policy, v2 saves are intentionally
            // invalidated.
            Assert.AreEqual(3, SaveWriter.FormatVersion);
        }

        // counter-check: a fabricated v2-version header must reject on load
        [Test]
        public void SaveReader_RejectsV2HeaderOnV3Build()
        {
            using var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(SaveWriter.Magic);
            writer.Write(2); // simulate a v2 save
            writer.Write(true);
            writer.Write("test-game");
            writer.Flush();
            stream.Position = 0;

            var reader = new SaveReader(stream, null);
            Assert.Throws<InvalidDataException>(() => reader.ReadHeader());
        }

        // ── World-entity StoryletPart round-trip via GameSessionState ─────────

        [Test]
        public void StoryletPart_OnWorldEntity_SurvivesGameSessionRoundTrip()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");
            var storyletPart = new StoryletPart();
            storyletPart.MarkFired("intro_storylet");
            storyletPart.StartQuest(new QuestState { QuestId = "MainQuest", CurrentStageIndex = 1, EnteredStageAtTurn = 10 });
            world.AddPart(storyletPart);

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null, turnManager: null, player: null,
                world: world);

            GameSessionState loaded = RoundTripState(state);

            Assert.IsNotNull(loaded.World);
            var loadedPart = loaded.World.GetPart<StoryletPart>();
            Assert.IsNotNull(loadedPart, "StoryletPart should survive on the world entity");
            Assert.IsTrue(loadedPart.HasFired("intro_storylet"));
            var quest = loadedPart.GetQuestState("MainQuest");
            Assert.IsNotNull(quest);
            Assert.AreEqual(1, quest.CurrentStageIndex);
            Assert.AreEqual(10, quest.EnteredStageAtTurn);
        }

        // counter-check: world without a StoryletPart loads cleanly with no part
        [Test]
        public void WorldEntity_WithoutStoryletPart_RoundTripsWithoutCrashingOrSynthesizing()
        {
            var world = new Entity { BlueprintName = "World" };
            world.SetTag("WorldEntity");

            var state = GameSessionState.Capture(
                "test-game", "1.0",
                zoneManager: null, turnManager: null, player: null,
                world: world);

            GameSessionState loaded = RoundTripState(state);

            Assert.IsNotNull(loaded.World);
            // World had no StoryletPart at save time → must not appear at load time.
            // (Bootstrap-after-load is responsible for attaching a fresh one if absent.)
            Assert.IsNull(loaded.World.GetPart<StoryletPart>());
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static GameSessionState RoundTripState(GameSessionState state)
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
