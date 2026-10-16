using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class DialogueTravelPagingTests : ShortcutFixture
    {
        DialogueUI OpenTravel(int lines = 80, int choices = 13)
        {
            var conversation = new ConversationData { ID = "DialogueTravelPaging" };
            conversation.Nodes.Add(new NodeData { ID = "Start", Text = string.Join("\n", Enumerable.Range(0, lines)
                .Select(i => "Route " + i.ToString("D2") + " remains unconfirmed; ask at the destination.")),
                Choices = Enumerable.Range(0, choices).Select(i => new ChoiceData { Text = "Destination " + i.ToString("D2"), Target = "Chosen" + i }).ToList() });
            for (int i = 0; i < choices; i++) conversation.Nodes.Add(new NodeData { ID = "Chosen" + i, Text = "A short reply.",
                Choices = new List<ChoiceData> { new ChoiceData { Text = "Back.", Target = "Start" } } });
            ConversationLoader.Register(conversation);
            var speaker = new Entity { BlueprintName = "DialogueTravelSpeaker" };
            speaker.AddPart(new ConversationPart { ConversationID = conversation.ID });
            speaker.AddPart(new RenderPart { DisplayName = "Traveler", RenderString = "@" });
            Assert.True(ConversationManager.StartConversation(speaker, Player));
            var ui = Component<DialogueUI>(); ui.Tilemap = Tiles(); ui.BgTilemap = Tiles(); ui.PopupCamera = OutsideCamera();
            ui.PlayerEntity = Player; ui.CurrentZone = Zone; ui.Open(); return ui;
        }

        static bool ContainsText(Tilemap map, string text)
        {
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                bool match = true;
                for (int i = 0; i < text.Length; i++)
                {
                    var tile = map.GetTile(cell + new Vector3Int(i, 0, 0));
                    if (text[i] == ' ' ? tile != null : tile != CP437TilesetGenerator.GetUiTile(text[i])) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }
        static HashSet<Vector3Int> Painted(Tilemap map)
        {
            var result = new HashSet<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) result.Add(p);
            return result;
        }
        static void AssertFits(DialogueUI ui)
        {
            Assert.LessOrEqual((int)Get(ui, "_popupH"), CenteredPopupLayout.GridHeight - 2,
                "Leave one row above and below long dialogue so title glyphs and paging hints do not touch the viewport edge.");
            foreach (var map in new[] { ui.Tilemap, ui.BgTilemap })
                foreach (var p in Painted(map))
                { Assert.That(p.x, Is.InRange(0, 79)); Assert.That(p.y, Is.InRange(0, 44), "No speech, choice or footer may be below the overlay."); }
        }
        static int ChoiceAt(DialogueUI ui, int x, int y)
        {
            var method = typeof(DialogueUI).GetMethod("GetChoiceRowAtCell", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Mouse hit testing must share the current page/window's absolute-row mapping.");
            return (int)method.Invoke(ui, new object[] { x, y });
        }
        static int LocalRowContaining(DialogueUI ui, string text)
        {
            int ox = (int)Get(ui, "_worldOriginX"), top = (int)Get(ui, "_worldTopY");
            for (int y = 0; y < (int)Get(ui, "_popupH"); y++)
                for (int x = 0; x < (int)Get(ui, "_popupW") - text.Length; x++)
                {
                    bool match = true;
                    for (int i = 0; i < text.Length; i++)
                        if (text[i] != ' ' && ui.Tilemap.GetTile(new Vector3Int(ox + x + i, top - y, 0)) != CP437TilesetGenerator.GetUiTile(text[i])) { match = false; break; }
                    if (match) return y;
                }
            Assert.Fail("The actual drawn page does not contain " + text); return -1;
        }

        [TestCase(1280, 800)] [TestCase(1280, 720)]
        public void LongSpeechAndChoicesStayInsideTheSame45RowDeckOverlay(int width, int height)
        {
            var ui = OpenTravel();
            ui.PopupCamera.targetTexture = new RenderTexture(width, height, 0);
            try { Press(Key.Space, ui.HandleInput); AssertFits(ui); }
            finally { var rt = ui.PopupCamera.targetTexture; ui.PopupCamera.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); }
        }
        [Test] public void LongSpeechHasVisiblePagingHintsAndNoHiddenTailOnFirstPage()
        {
            var ui = OpenTravel(); Press(Key.Space, ui.HandleInput);
            Assert.True(ContainsText(ui.Tilemap, "Route 00")); Assert.False(ContainsText(ui.Tilemap, "Route 79"));
            Assert.True(ContainsText(ui.Tilemap, "Left/LB")); Assert.True(ContainsText(ui.Tilemap, "Right/RB"));
        }
        [TestCase(Key.RightArrow, Key.LeftArrow)] [TestCase(Key.PageDown, Key.PageUp)]
        public void SpeechPagesCanReachTheTailAndReturnWithoutChoosingOrChangingText(Key next, Key previous)
        {
            var ui = OpenTravel(); string original = ConversationManager.CurrentText; Press(Key.Space, ui.HandleInput);
            int steps = 0;
            while (!ContainsText(ui.Tilemap, "Route 79") && steps++ < 12)
            { Press(next, ui.HandleInput); Press(Key.Space, ui.HandleInput); AssertFits(ui); }
            Assert.True(ContainsText(ui.Tilemap, "Route 79"), "The last report must be reachable by paging.");
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID); Assert.AreEqual(original, ConversationManager.CurrentText);
            Assert.Null(ConversationManager.PendingAttackTarget); Assert.Null(ConversationManager.PendingTradePartner);
            for (int i = 0; i < steps; i++) { Press(previous, ui.HandleInput); if ((bool)Get(ui, "_revealing")) Press(Key.Space, ui.HandleInput); }
            Assert.True(ContainsText(ui.Tilemap, "Route 00")); Assert.False(ContainsText(ui.Tilemap, "Route 79"));
        }
        [Test] public void NewSpeechPageStillConsumesFirstConfirmToRevealBeforeSelecting()
        {
            var ui = OpenTravel(); Press(Key.Space, ui.HandleInput); Press(Key.RightArrow, ui.HandleInput);
            Assert.True((bool)Get(ui, "_revealing")); Press(Key.Enter, ui.HandleInput);
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID); Assert.False((bool)Get(ui, "_revealing"));
            Press(Key.Enter, ui.HandleInput); Assert.AreEqual("Chosen0", ConversationManager.CurrentNode.ID);
        }
        [TestCase(10)] [TestCase(12)]
        public void CursorFollowsTheVisibleWindowBeforeConfirmingItsAbsoluteChoice(int index)
        {
            var ui = OpenTravel(1); Press(Key.Space, ui.HandleInput);
            for (int i = 0; i < index; i++) Press(Key.DownArrow, ui.HandleInput);
            Assert.AreEqual(index, Get(ui, "_cursorIndex"));
            Assert.True(ContainsText(ui.Tilemap, "Destination " + index.ToString("D2")), "A focused choice must be drawn before Enter can activate it.");
            Assert.False(ContainsText(ui.Tilemap, "Destination 00")); AssertFits(ui);
            Press(Key.Enter, ui.HandleInput); Assert.AreEqual("Chosen" + index, ConversationManager.CurrentNode.ID);
        }
        [Test] public void ScrolledChoiceMouseMappingUsesAbsoluteIndexAndRejectsPortraitAndFooter()
        {
            var ui = OpenTravel(1); Press(Key.Space, ui.HandleInput);
            for (int i = 0; i < 12; i++) Press(Key.DownArrow, ui.HandleInput);
            int row = LocalRowContaining(ui, "Destination 12");
            Assert.AreEqual(12, ChoiceAt(ui, 24, row)); Assert.AreEqual(-1, ChoiceAt(ui, 5, row));
            Assert.AreEqual(-1, ChoiceAt(ui, 24, (int)Get(ui, "_popupH") - 1));
        }
        [Test] public void ReturningCursorToTheTopRestoresTheFirstChoices()
        {
            var ui = OpenTravel(1); Press(Key.Space, ui.HandleInput);
            for (int i = 0; i < 12; i++) Press(Key.DownArrow, ui.HandleInput);
            Assert.True(ContainsText(ui.Tilemap, "Destination 12"));
            for (int i = 0; i < 12; i++) Press(Key.UpArrow, ui.HandleInput);
            Assert.True(ContainsText(ui.Tilemap, "Destination 00")); Assert.False(ContainsText(ui.Tilemap, "Destination 12"));
            Press(Key.Enter, ui.HandleInput); Assert.AreEqual("Chosen0", ConversationManager.CurrentNode.ID);
        }
        [TestCase(false)] [TestCase(true)]
        public void AbsoluteShortcutCanSelectOnlyAChoiceInTheDrawnWindow(bool hidden)
        {
            var ui = OpenTravel(1); Press(Key.Space, ui.HandleInput);
            if (hidden) for (int i = 0; i < 12; i++) Press(Key.DownArrow, ui.HandleInput);
            Press(Key.A, ui.HandleInput);
            Assert.AreEqual(hidden ? "Start" : "Chosen0", ConversationManager.CurrentNode.ID);
            if (hidden) { Assert.AreEqual(12, Get(ui, "_cursorIndex")); Assert.AreEqual(KeyCode.None, ui.ClosingActivationKey); }
        }
        [TestCase(false)] [TestCase(true)]
        public void PagingPastAnEndpointDoesNotRestartRevealOrEatTheNextConfirm(bool lastPage)
        {
            var ui = OpenTravel(); Press(Key.Space, ui.HandleInput);
            if (lastPage)
                for (int i = 0; i < 12 && !ContainsText(ui.Tilemap, "Route 79"); i++)
                { Press(Key.RightArrow, ui.HandleInput); Press(Key.Space, ui.HandleInput); }
            Assert.True(ContainsText(ui.Tilemap, lastPage ? "Route 79" : "Route 00"));
            Press(lastPage ? Key.RightArrow : Key.LeftArrow, ui.HandleInput);
            Assert.False((bool)Get(ui, "_revealing")); Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
            Press(Key.Enter, ui.HandleInput); Assert.AreEqual("Chosen0", ConversationManager.CurrentNode.ID);
        }
        [Test] public void ShortOrdinaryNodeKeepsItsLayoutAndPagingKeysDoNothing()
        {
            var ui = OpenTravel(1, 2); Press(Key.Space, ui.HandleInput);
            int lines = ((List<string>)Get(ui, "_wrappedTextLines")).Count;
            Assert.AreEqual(lines + ConversationManager.VisibleChoices.Count + 4, Get(ui, "_popupH"));
            var before = Painted(ui.Tilemap); Press(Key.RightArrow, ui.HandleInput); Press(Key.PageDown, ui.HandleInput);
            CollectionAssert.AreEquivalent(before, Painted(ui.Tilemap)); Assert.False((bool)Get(ui, "_revealing"));
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID); Assert.AreEqual(0, Get(ui, "_cursorIndex"));
        }
        [Test] public void ShorterReplyClearsPreviousPageForegroundAndBackgroundAndResetsPaging()
        {
            var ui = OpenTravel(); Press(Key.Space, ui.HandleInput); Press(Key.RightArrow, ui.HandleInput); Press(Key.Space, ui.HandleInput);
            var fg = Painted(ui.Tilemap); var bg = Painted(ui.BgTilemap);
            Press(Key.Enter, ui.HandleInput); Assert.AreEqual("Chosen0", ConversationManager.CurrentNode.ID);
            int x = (int)Get(ui, "_worldOriginX"), top = (int)Get(ui, "_worldTopY"), w = (int)Get(ui, "_popupW"), h = (int)Get(ui, "_popupH");
            Func<Vector3Int, bool> outside = p => p.x < x || p.x >= x + w || p.y > top || p.y <= top - h;
            foreach (var p in fg.Where(outside)) Assert.False(ui.Tilemap.HasTile(p));
            foreach (var p in bg.Where(outside)) Assert.False(ui.BgTilemap.HasTile(p));
            Press(Key.Space, ui.HandleInput); Press(Key.Enter, ui.HandleInput); Press(Key.Space, ui.HandleInput);
            Assert.True(ContainsText(ui.Tilemap, "Route 00")); Assert.False(ContainsText(ui.Tilemap, "Route 79"));
            ui.Close(); Assert.IsEmpty(Painted(ui.Tilemap)); Assert.IsEmpty(Painted(ui.BgTilemap));
        }
    }
}
