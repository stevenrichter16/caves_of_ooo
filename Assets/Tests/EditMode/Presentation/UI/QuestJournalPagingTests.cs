using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class QuestJournalPagingTests
    {
        readonly InputTestFixture input = new InputTestFixture();
        readonly Dictionary<TileBase, char> glyphs = new Dictionary<TileBase, char>();
        Dictionary<string, StoryletData> registry, oldRegistry;
        bool oldLoaded;
        StoryletPart oldStory, story;
        Entity oldPlayer;
        NarrativeStatePart oldNarrative;
        GameObject host;
        QuestLogUI ui;
        Gamepad pad;
        sealed class Keys : IInputProbe
        {
            readonly KeyCode key;
            public Keys(KeyCode key) { this.key = key; }
            public bool GetKeyDown(KeyCode candidate) => candidate == key;
        }
        [SetUp] public void SetUp()
        {
            input.Setup();
            oldStory = StoryletPart.Current; oldPlayer = StoryletPart.LocalPlayer; oldNarrative = NarrativeStatePart.Current;
            registry = (Dictionary<string, StoryletData>)typeof(StoryletRegistry).GetField("_storylets", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            oldRegistry = new Dictionary<string, StoryletData>(registry);
            oldLoaded = (bool)typeof(StoryletRegistry).GetField("_loaded", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            StoryletRegistry.Reset(); StoryletPart.Current = story = new StoryletPart();
            StoryletPart.LocalPlayer = new Entity(); NarrativeStatePart.Current = null;
            host = new GameObject("quest journal paging"); ui = host.AddComponent<QuestLogUI>(); ui.Tilemap = host.AddComponent<Tilemap>();
            for (char c = ' '; c <= '~'; c++) { var tile = CP437TilesetGenerator.GetTextTile(c); Assert.NotNull(tile); glyphs[tile] = c; }
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(host); glyphs.Clear();
            StoryletPart.Current = oldStory; StoryletPart.LocalPlayer = oldPlayer; NarrativeStatePart.Current = oldNarrative;
            registry.Clear(); foreach (var pair in oldRegistry) registry.Add(pair.Key, pair.Value);
            typeof(StoryletRegistry).GetField("_loaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, oldLoaded);
            input.TearDown();
        }
        void LongJournal()
        {
            var objectives = new List<QuestObjectiveData>();
            for (int i = 0; i < 45; i++) objectives.Add(new QuestObjectiveData { ID = "obj" + i,
                Text = "Objective " + i + ": follow the old path beyond the hedge and carry the permitted sample back to the person beside the well. TAIL" + i + "." });
            StoryletRegistry.Register(new StoryletData { ID = "LongJourney", Quest = new QuestData { Stages = new List<QuestStageData> {
                new QuestStageData { ID = "outbound", Objectives = objectives }, new QuestStageData { ID = "FinalStage" } } } });
            story.StartQuest(new QuestState { QuestId = "LongJourney" });
            story.StartQuest(new QuestState { QuestId = "FinalActiveQuest" });
            story.UndertakeAct("closed", "ClosedHistory"); Assert.True(story.CloseAct("closed"));
            story.UndertakeAct("refused", "RefusedHistory"); Assert.True(story.RefuseAct("refused"));
            story.UndertakeAct("unspoken", "UnspokenHistory");
        }
        void Notes()
        {
            for (int i = 0; i < 17; i++) StoryletPart.LocalPlayer.Properties["RegionalTravelNote:" + i] = JsonUtility.ToJson(new RegionalTravelLead {
                DestinationName = "Recorded destination " + i + " beside a long and winding road across the low fields", OriginZoneID = "Overworld.1.1.0", DestinationZoneID = "Overworld.10." + i + ".0" });
        }
        void Key(KeyCode key) => Assert.True(ui.HandleInput(new Keys(key)));
        string Screen()
        {
            var result = new StringBuilder();
            for (int y = 0; y < 45; y++) { for (int x = 0; x < 80; x++) { var t = ui.Tilemap.GetTile(new Vector3Int(x, 44 - y, 0)); result.Append(t != null && glyphs.TryGetValue(t, out char c) ? c : ' '); } result.Append('\n'); }
            return result.ToString();
        }
        [Test] public void EveryWrappedObjectiveAndAllHistorySectionsAreReachableWithoutChangingStateOrTime()
        {
            LongJournal(); int? tick = TurnManager.Active?.TickCount; ui.Open();
            var first = Screen(); StringAssert.DoesNotContain("TAIL44", first);
            var pages = new List<string>();
            for (int i = 0; i < 14; i++) { pages.Add(Screen()); Key(KeyCode.PageDown); }
            string all = string.Join("\n", pages);
            for (int i = 0; i < 45; i++) StringAssert.Contains("TAIL" + i + ".", all);
            foreach (string s in new[] { "FinalStage", "FinalActiveQuest", "ClosedHistory", "UnspokenHistory", "RefusedHistory" }) StringAssert.Contains(s, all);
            Assert.Greater(pages.Distinct().Count(), 1);
            foreach (string page in pages) { StringAssert.Contains("QUEST LOG", page); StringAssert.Contains("1 closed  1 refused  3 open", page); }
            Assert.AreEqual(2, story.GetActiveQuests().Count); Assert.AreEqual(ClosureState.Refused, story.GetClosure("refused").State);
            Assert.AreEqual(tick, TurnManager.Active?.TickCount);
        }
        [Test] public void PageBoundariesClampAndReopeningResetsTheQuestPage()
        {
            LongJournal(); ui.Open(); string first = Screen(); Key(KeyCode.PageUp); Assert.AreEqual(first, Screen());
            Key(KeyCode.PageDown); Assert.AreNotEqual(first, Screen());
            for (int i = 0; i < 15; i++) Key(KeyCode.PageDown);
            string last = Screen(); Key(KeyCode.PageDown); Assert.AreEqual(last, Screen());
            Key(KeyCode.PageUp); Assert.AreNotEqual(last, Screen());
            ui.Close(); ui.Open(); Assert.AreEqual(first, Screen());
        }
        [Test] public void NotesAndQuestPagePositionsRemainIndependentAcrossTabs()
        {
            LongJournal(); Notes(); ui.Open(); string first = Screen(); Key(KeyCode.RightArrow); string questPage = Screen(); Assert.AreNotEqual(first, questPage);
            Key(KeyCode.Tab); Assert.True(ui.NotesVisible); Key(KeyCode.RightArrow); Assert.Greater(ui.NotesPage, 0); string notePage = Screen();
            Key(KeyCode.Tab); Assert.False(ui.NotesVisible); Assert.AreEqual(questPage, Screen());
            Key(KeyCode.Tab); Assert.AreEqual(notePage, Screen());
            Key(KeyCode.LeftArrow); Assert.Zero(ui.NotesPage);
        }
        [Test] public void NativeShouldersReachTheNextQuestPageAndYAndBRetainTheirMeaning()
        {
            LongJournal(); pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            State(new GamepadState()); NativeGamepadInput.SetContext(GamepadInputContext.Menu); State(new GamepadState()); ui.Open();
            string first = Screen(); Press(GamepadButton.RightShoulder); Assert.AreNotEqual(first, Screen());
            Press(GamepadButton.LeftShoulder); Assert.AreEqual(first, Screen());
            Press(GamepadButton.North); Assert.True(ui.NotesVisible);
            Press(GamepadButton.East); Assert.False(ui.IsOpen);
        }
        [TestCase(false)] [TestCase(true)] public void EmptyAndShortJournalsStayOnOnePage(bool shortQuest)
        {
            if (shortQuest) story.StartQuest(new QuestState { QuestId = "ShortQuest" });
            ui.Open(); string first = Screen(); StringAssert.Contains("page 1 / 1", first);
            Key(KeyCode.PageDown); Assert.AreEqual(first, Screen()); Key(KeyCode.PageUp); Assert.AreEqual(first, Screen());
        }
        void State(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); InputHelper.GetKey(KeyCode.None); }
        void Press(GamepadButton button) { State(new GamepadState()); State(new GamepadState().WithButton(button)); Assert.True(ui.HandleInput()); State(new GamepadState()); }
    }
}
