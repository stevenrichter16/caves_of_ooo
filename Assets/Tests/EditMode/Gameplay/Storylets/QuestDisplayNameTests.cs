using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The quest display-name seam — the unblocker for the
    /// Adventure-Time name retirement.
    ///
    /// <para><b>Why this had to come first.</b> The retirement follows
    /// the frozen-ID discipline from <c>Lore/TERMS.md</c>: internal IDs
    /// never change (they live in saves, in dispatch switches, in
    /// property keys), only player-visible text does. That plan hit a
    /// wall on quests, because quest IDs <i>were</i> the player-visible
    /// text — <c>StoryletPart.DisplayNameFor</c> returned the raw id,
    /// and the quest log drew <c>e.QuestId</c> straight to the screen.
    /// So the log said "Quest accepted: RootBeerGuyCase." and there was
    /// nowhere to put a real name.</para>
    ///
    /// <para>The fix is the one the code's own comment anticipated:
    /// <c>QuestData.Name</c>, with the id as fallback. That keeps every
    /// id frozen AND gives quests titles — an improvement independent of
    /// the retirement, since "TheMillStone" would have read no better
    /// than "RootBeerGuyCase".</para>
    /// </summary>
    public class QuestDisplayNameTests
    {
        [SetUp]
        public void SetUp() => StoryletRegistry.Reset();

        [TearDown]
        public void TearDown() => StoryletRegistry.Reset();

        private const string NamedQuest = @"{
          ""Storylets"": [{
            ""ID"": ""TestNamedQuest"",
            ""Quest"": {
              ""Name"": ""The Mill-Stone"",
              ""Stages"": [{ ""ID"": ""s1"", ""Objectives"": [] }]
            }
          }]
        }";

        private const string UnnamedQuest = @"{
          ""Storylets"": [{
            ""ID"": ""TestUnnamedQuest"",
            ""Quest"": {
              ""Stages"": [{ ""ID"": ""s1"", ""Objectives"": [] }]
            }
          }]
        }";

        [Test]
        public void QuestWithAName_ReportsTheName()
        {
            StoryletRegistry.LoadFromJson(NamedQuest);
            Assert.AreEqual("The Mill-Stone",
                StoryletPart.QuestDisplayName("TestNamedQuest"));
        }

        [Test]
        public void QuestWithoutAName_FallsBackToItsId()
        {
            // Counter-check, and the compatibility contract: every quest
            // that ships today has no Name field, and none of them may
            // start rendering as an empty string.
            StoryletRegistry.LoadFromJson(UnnamedQuest);
            Assert.AreEqual("TestUnnamedQuest",
                StoryletPart.QuestDisplayName("TestUnnamedQuest"));
        }

        [Test]
        public void UnknownQuest_FallsBackToTheIdItWasAsked()
        {
            // Lifecycle messages fire for quests the registry may not
            // know (a save referencing removed content). Never blank,
            // never a crash.
            Assert.AreEqual("NoSuchQuest",
                StoryletPart.QuestDisplayName("NoSuchQuest"));
            Assert.AreEqual("", StoryletPart.QuestDisplayName(""));
            Assert.IsNull(StoryletPart.QuestDisplayName(null));
        }

        [Test]
        public void NameIsPlayerFacingProse_NotAnIdentifier()
        {
            // The point of the seam: a quest's shown title may contain
            // spaces, punctuation and case that an id never could.
            StoryletRegistry.LoadFromJson(NamedQuest);
            string shown = StoryletPart.QuestDisplayName("TestNamedQuest");
            StringAssert.Contains(" ", shown);
            Assert.AreNotEqual("TestNamedQuest", shown);
        }
    }
}
