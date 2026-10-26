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
    // Real draw/clear and keyboard node/page paths. Native-layout forcing below
    // is geometry-only; the existing portrait fixtures prove speaker authority.
    public sealed class DialogueFooterPresentationTests : ShortcutFixture
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly Color ExistingBacking = new Color(.05f, .05f, .08f, .92f);

        DialogueUI OpenNode(int lines = 1, int choices = 1, bool panel = true, bool reveal = false)
        {
            var data = new ConversationData { ID = "DialogueFooterPresentation" };
            data.Nodes.Add(new NodeData
            {
                ID = "Start", Text = string.Join("\n", Enumerable.Repeat("A reply from the keeper.", lines)),
                Choices = Enumerable.Range(0, choices).Select(i => new ChoiceData
                    { Text = "Reply " + i, Target = "Short" }).ToList()
            });
            data.Nodes.Add(new NodeData { ID = "Short", Text = "Yes.",
                Choices = new List<ChoiceData> { new ChoiceData { Text = "Back.", Target = "Start" } } });
            ConversationLoader.Register(data);
            var speaker = new Entity { BlueprintName = "DialogueFooterSpeaker" };
            speaker.AddPart(new ConversationPart { ConversationID = data.ID });
            speaker.AddPart(new RenderPart { DisplayName = "Keeper", RenderString = "C" });
            Assert.True(ConversationManager.StartConversation(speaker, Player));
            var ui = Component<DialogueUI>();
            ui.Tilemap = Tiles(); ui.BgTilemap = Tiles(); ui.PopupCamera = OutsideCamera();
            ui.PlayerEntity = Player; ui.CurrentZone = Zone; ui.UsePortraitPanel = panel;
            Assert.NotNull(CP437TilesetGenerator.GetUiTile('['));
            Assert.NotNull(CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock));
            ui.Open();
            if (!reveal) Press(Key.Space, ui.HandleInput);
            return ui;
        }

        static int Width(DialogueUI ui) => (int)Get(ui, "_popupW");
        static int Height(DialogueUI ui) => (int)Get(ui, "_popupH");
        static int VisibleLines(DialogueUI ui) => (int)typeof(DialogueUI)
            .GetProperty("VisibleTextLineCount", Private).GetValue(ui);
        static Vector3Int Cell(DialogueUI ui, int x, int y) => new Vector3Int(
            (int)Get(ui, "_worldOriginX") + x, (int)Get(ui, "_worldTopY") - y, 0);
        static HashSet<Vector3Int> Painted(Tilemap map)
        {
            var result = new HashSet<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) result.Add(p);
            return result;
        }

        static void AssertBacking(DialogueUI ui)
        {
            int w = Width(ui), h = Height(ui);
            Assert.AreEqual(w * h, Painted(ui.BgTilemap).Count,
                "The existing owned rectangle includes the footer row.");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Assert.True(ui.BgTilemap.HasTile(Cell(ui, x, y)), "Unbacked cell " + x + "," + y);
                    Assert.AreEqual(ExistingBacking, ui.BgTilemap.GetColor(Cell(ui, x, y)),
                        "Reuse the body backing; do not change its established opacity or palette.");
                }
            Assert.False(ui.BgTilemap.HasTile(Cell(ui, 0, h)), "World below the footprint remains uncovered.");
            Assert.False(ui.BgTilemap.HasTile(Cell(ui, -1, h - 1)));
            Assert.False(ui.BgTilemap.HasTile(Cell(ui, w, h - 1)));
            Assert.AreEqual(-1, Call(ui, "GetChoiceRowAtCell", ui.UsePortraitPanel ? 12 : 2, h - 1),
                "A backed footer is still not a dialogue choice.");
        }

        [TestCase(false, false, 1, 1)]
        [TestCase(true, false, 1, 1)]
        [TestCase(true, true, 1, 1)]
        [TestCase(true, false, 80, 12)]
        public void ExistingFootprintIsBackedThroughFooterForLegacyNativeAndPagedLayouts(
            bool panel, bool native, int lines, int choices)
        {
            var ui = OpenNode(lines, choices, panel);
            if (native) { Set(ui, "_nativePortraitLayout", true); Call(ui, "Render"); }
            int inner = VisibleLines(ui) + 1 + Math.Min(ConversationManager.VisibleChoices.Count, 10);
            Assert.AreEqual((native ? Math.Max(7, inner) : inner) + 3, Height(ui),
                "Footer backing must not change established geometry or native portrait padding.");
            AssertBacking(ui);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActionHintsStayReadableDuringAndAfterReveal(bool reveal)
        {
            var ui = OpenNode(reveal: reveal);
            Assert.AreEqual(reveal, Get(ui, "_revealing"));
            var start = Cell(ui, 1, Height(ui) - 1);
            Assert.AreSame(CP437TilesetGenerator.GetUiTile('['), ui.Tilemap.GetTile(start));
            Assert.AreEqual(QudColorParser.Gray, ui.Tilemap.GetColor(start),
                "Existing actionable footer text uses the backed action/pickup modal's light gray.");
            Assert.AreNotEqual(QudColorParser.DarkGray, ui.Tilemap.GetColor(start));
        }

        static void AssertOldFootprintCleared(DialogueUI ui, HashSet<Vector3Int> oldFg, HashSet<Vector3Int> oldBg)
        {
            int x = (int)Get(ui, "_worldOriginX"), top = (int)Get(ui, "_worldTopY");
            int w = Width(ui), h = Height(ui);
            Func<Vector3Int, bool> outside = p => p.x < x || p.x >= x + w || p.y > top || p.y <= top - h;
            Assert.True(oldFg.Any(outside), "The prior footprint must really extend outside the new one.");
            Assert.True(oldBg.Any(outside));
            foreach (var p in oldFg.Where(outside)) Assert.False(ui.Tilemap.HasTile(p), "Old foreground " + p);
            foreach (var p in oldBg.Where(outside)) Assert.False(ui.BgTilemap.HasTile(p), "Old backing " + p);
            AssertBacking(ui);
        }

        [Test]
        public void RealLongToShortNodeSelectionClearsOldFooterAndPreservesChoiceRows()
        {
            var ui = OpenNode(80, 12);
            var fg = Painted(ui.Tilemap); var bg = Painted(ui.BgTilemap);
            int oldH = Height(ui), oldW = Width(ui);
            Press(Key.Enter, ui.HandleInput);
            Assert.AreEqual("Short", ConversationManager.CurrentNode.ID);
            Assert.Less(Height(ui), oldH); Assert.Less(Width(ui), oldW);
            AssertOldFootprintCleared(ui, fg, bg);
            Assert.AreEqual(0, Call(ui, "GetChoiceRowAtCell", 12, VisibleLines(ui) + 2));
            Assert.AreEqual(-1, Call(ui, "GetChoiceRowAtCell", 12, Height(ui) - 2));
        }

        [Test]
        public void RealLastTextPageShrinksBackingWithoutSelectingAnAction()
        {
            var ui = OpenNode(40);
            var choices = ConversationManager.VisibleChoices.ToArray();
            var fg = Painted(ui.Tilemap); var bg = Painted(ui.BgTilemap);
            int oldH = Height(ui);
            Assert.Greater((int)typeof(DialogueUI).GetProperty("TextPageCount", Private).GetValue(ui), 1);
            Press(Key.PageDown, ui.HandleInput);
            Assert.AreEqual(1, Get(ui, "_textPage")); Assert.Less(Height(ui), oldH);
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
            CollectionAssert.AreEqual(choices, ConversationManager.VisibleChoices);
            AssertOldFootprintCleared(ui, fg, bg);
            var settledFg = Painted(ui.Tilemap); var settledBg = Painted(ui.BgTilemap);
            Press(Key.PageDown, ui.HandleInput);
            CollectionAssert.AreEquivalent(settledFg, Painted(ui.Tilemap), "Endpoint paging leaves the footer stable.");
            CollectionAssert.AreEquivalent(settledBg, Painted(ui.BgTilemap));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CloseOrDisableClearsOwnedFooterAndPreservesForeignTiles(bool disable)
        {
            var ui = OpenNode();
            var footer = Cell(ui, 1, Height(ui) - 1);
            Assert.True(ui.Tilemap.HasTile(footer), "There is actual foreground to clear.");
            var sentinel = new Vector3Int(-100, -100, 0);
            var tile = CP437TilesetGenerator.GetUiTile('!');
            ui.Tilemap.SetTile(sentinel, tile); ui.BgTilemap.SetTile(sentinel, tile);
            // Non-ExecuteAlways behaviours do not receive EditMode activation
            // callbacks. Exercise the saved lifecycle method, not dispatch.
            if (disable) Call(ui, "OnDisable"); else Press(Key.Escape, ui.HandleInput);
            Assert.False(ui.IsOpen); Assert.False(ConversationManager.IsActive);
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(ui.Tilemap));
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(ui.BgTilemap));
            ui.Close();
            CollectionAssert.AreEquivalent(new[] { sentinel }, Painted(ui.BgTilemap), "Repeated close is harmless.");
        }

        [Test]
        public void MissingOptionalBackgroundStillRendersAndClearsForeground()
        {
            var ui = OpenNode(); ui.Close(); ui.BgTilemap = null;
            var speaker = new Entity { BlueprintName = "DialogueFooterSpeaker" };
            speaker.AddPart(new ConversationPart { ConversationID = "DialogueFooterPresentation" });
            speaker.AddPart(new RenderPart { DisplayName = "Keeper", RenderString = "C" });
            Assert.True(ConversationManager.StartConversation(speaker, Player)); ui.Open();
            Assert.True(ui.IsOpen);
            Assert.AreSame(CP437TilesetGenerator.GetUiTile('['), ui.Tilemap.GetTile(Cell(ui, 1, Height(ui) - 1)));
            Press(Key.Escape, ui.HandleInput);
            Assert.False(ui.IsOpen); Assert.IsEmpty(Painted(ui.Tilemap));
        }
    }
}
