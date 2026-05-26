using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// QuestBeaconPart (Docs/QUEST-GIVER-DISCOVERABILITY.md) — tints a
    /// quest-giver NPC a distinct color via the render-event color hook WHILE
    /// the quest it offers is offerable (not started), so players can find
    /// quest-givers among villagers. These tests exercise the Part's render
    /// hook directly (no ZoneRenderer needed): fire a "Render" event carrying a
    /// ColorString and assert whether the Part overrode it.
    /// </summary>
    public class QuestBeaconPartTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletPart.Current = new StoryletPart();
        }

        [TearDown]
        public void TearDown()
        {
            StoryletPart.Current = null;
        }

        private static Entity MakeGiver(string quest, string availableColor = "&Y")
        {
            var e = new Entity { ID = "giver", BlueprintName = "Villager" };
            e.AddPart(new QuestBeaconPart { Quest = quest, AvailableColor = availableColor });
            return e;
        }

        /// <summary>Fire a "Render" event with a starting color; return the
        /// color after the Part has had a chance to mutate it (mirrors how
        /// ZoneRenderer reads ColorString back after FireEvent).</summary>
        private static string RenderColor(Entity e, string startColor)
        {
            var ev = GameEvent.New("Render");
            ev.SetParameter("ColorString", startColor);
            e.FireEvent(ev);
            string result = ev.GetStringParameter("ColorString", startColor);
            ev.Release();
            return result;
        }

        [Test]
        public void Render_QuestNotStarted_TintsAvailableColor()
        {
            // Quest never started/completed → offerable → highlight.
            var giver = MakeGiver("ClearTheWarren");
            Assert.AreEqual("&Y", RenderColor(giver, "&w"),
                "an offerable quest tints the giver the available color (&Y)");
        }

        [Test]
        public void Render_QuestActive_LeavesColorUnchanged()
        {
            // Counter-check: once accepted (active), no highlight — the player
            // already has it; the quest log tracks it.
            StoryletPart.Current.StartQuest(new QuestState { QuestId = "ClearTheWarren" });
            var giver = MakeGiver("ClearTheWarren");
            Assert.AreEqual("&w", RenderColor(giver, "&w"),
                "an active quest must NOT highlight (themed color passes through)");
        }

        [Test]
        public void Render_QuestCompleted_LeavesColorUnchanged()
        {
            // Counter-check: completed → no highlight (nothing to offer).
            StoryletPart.Current.StartQuest(new QuestState { QuestId = "ClearTheWarren" });
            StoryletPart.Current.MarkQuestCompleted("ClearTheWarren");
            var giver = MakeGiver("ClearTheWarren");
            Assert.AreEqual("&w", RenderColor(giver, "&w"),
                "a completed quest must NOT highlight");
        }

        [Test]
        public void Render_CustomAvailableColor_Honored()
        {
            var giver = MakeGiver("ClearTheWarren", availableColor: "&G");
            Assert.AreEqual("&G", RenderColor(giver, "&w"),
                "the AvailableColor field drives the highlight color");
        }

        [Test]
        public void Render_NoStoryletPart_Unchanged_NoThrow()
        {
            // Defensive: pre-bootstrap (no StoryletPart) → no crash, no override.
            StoryletPart.Current = null;
            var giver = MakeGiver("ClearTheWarren");
            string result = null;
            Assert.DoesNotThrow(() => result = RenderColor(giver, "&w"));
            Assert.AreEqual("&w", result, "no storylet system → leave color alone");
        }

        [Test]
        public void NonRenderEvent_Ignored()
        {
            // A non-"Render" event must not be touched (e.g. a stray param).
            var giver = MakeGiver("ClearTheWarren");
            var ev = GameEvent.New("BeforeTakeDamage");
            ev.SetParameter("ColorString", "&w");
            giver.FireEvent(ev);
            Assert.AreEqual("&w", ev.GetStringParameter("ColorString", "&w"),
                "QuestBeaconPart only reacts to the Render event");
            ev.Release();
        }

        [Test]
        public void EmptyQuest_Ignored_NoThrow()
        {
            var giver = MakeGiver("");
            string result = null;
            Assert.DoesNotThrow(() => result = RenderColor(giver, "&w"));
            Assert.AreEqual("&w", result, "no quest id → no override");
        }

        [Test]
        public void SaveLoad_RoundTrips()
        {
            var e = new Entity { ID = "g", BlueprintName = "Villager" };
            e.AddPart(new QuestBeaconPart { Quest = "ClearTheWarren", AvailableColor = "&G" });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            var p = loaded.GetPart<QuestBeaconPart>();
            Assert.IsNotNull(p);
            Assert.AreEqual("ClearTheWarren", p.Quest);
            Assert.AreEqual("&G", p.AvailableColor);
        }
    }
}
