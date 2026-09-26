using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityUrquTests
    {
        NarrativeStatePart state;
        StoryletPart story;
        [SetUp] public void Setup()
        {
            Diag.ResetAll(); ConversationPredicates.Reset(); ConversationActions.Reset();
            StoryletRegistry.LoadAll();
            state = new NarrativeStatePart(); NarrativeStatePart.Current = state;
            story = new StoryletPart(); StoryletPart.Current = story;
            SettlementRuntime.ActiveZone = null;
        }
        [TearDown] public void Cleanup()
        {
            NarrativeStatePart.Current = null; StoryletPart.Current = null;
            StoryletRegistry.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset();
            SettlementRuntime.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }
        void Tick() => story.OnTickEnd(state);

        [Test] public void ShippedSillOmenEnablesIndicatorFaunaOnFollowingTick()
        {
            SettlementRuntime.ActiveZone = new Zone(WorldMap.StartingZoneID);
            Tick(); Assert.AreEqual(1, state.GetFact("sill_sari_heard"));
            Assert.AreEqual(0, state.GetFact("UrquActive"), "snapshot dispatch defers dependent storylet");
            Tick(); Assert.AreEqual(1, state.GetFact("UrquActive"));
            CollectionAssert.Contains(PopulationTable.GetStumpTable(StumpBand.Slopes, 3).Roll(new System.Random(1)), "SariSnake");
        }
        [Test] public void NoOmenKeepsQuietWorldAndSnakesAbsent()
        {
            Tick(); Tick(); Assert.AreEqual(0, state.GetFact("UrquActive"));
            CollectionAssert.DoesNotContain(PopulationTable.GetStumpTable(StumpBand.Slopes, 3).Roll(new System.Random(1)), "SariSnake");
        }
        [Test] public void ExistingOmenFactActivatesWithoutReplayingSill()
        {
            state.SetFact("sill_sari_heard", 1); story.MarkFired("SillHearsIt");
            Tick(); Assert.AreEqual(1, state.GetFact("UrquActive"));
            Assert.IsTrue(story.HasFired("UrquSignsAwaken"));
        }
        [Test] public void ExplicitQuietingDoesNotRearmOneShot()
        {
            state.SetFact("sill_sari_heard", 1); Tick();
            Assert.AreEqual(1, state.GetFact("UrquActive"));
            state.ClearFact("UrquActive"); Tick(); Assert.AreEqual(0, state.GetFact("UrquActive"));
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EnactedEndingQuietsActiveSigns(int ending)
        {
            state.SetFact("UrquActive", 1); state.SetFact("sill_sari_heard", 1);
            state.SetFact("Ending", ending); Tick(); Tick();
            Assert.AreEqual(0, state.GetFact("UrquActive"));
            Assert.IsFalse(story.HasFired("UrquSignsAwaken"), "an ending must not awaken them even for a tick");
        }
        [Test] public void MissingNarrativeStateFailsClosed()
        {
            NarrativeStatePart.Current = null; Tick();
            CollectionAssert.DoesNotContain(PopulationTable.GetStumpTable(StumpBand.Slopes, 3).Roll(new System.Random(1)), "SariSnake");
        }
        [Test] public void FiredActivationAndWorldFactSurviveSaveRoundtrip()
        {
            state.SetFact("sill_sari_heard", 1); Tick();
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); state.Save(writer); story.Save(writer);
                stream.Position = 0; var reader = new SaveReader(stream, null);
                state = new NarrativeStatePart(); state.Load(reader); NarrativeStatePart.Current = state;
                story = new StoryletPart(); story.Load(reader); StoryletPart.Current = story;
            }
            Assert.AreEqual(1, state.GetFact("UrquActive"));
            Assert.IsTrue(story.HasFired("UrquSignsAwaken"));
            state.ClearFact("UrquActive"); Tick(); Assert.AreEqual(0, state.GetFact("UrquActive"));
        }
    }
}
