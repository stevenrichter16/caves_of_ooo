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
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Layout-only seam: force the captured native-layout flag after normal Open.
    // Separate identity tests prove whether a live conversation may capture it.
    public sealed class FlatPixelDialoguePortraitLayoutTests : ShortcutFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        DialogueUI OpenNode(int lines = 1, int choices = 1)
        {
            var data = new ConversationData { ID = "FlatDialogueLayout" };
            data.Nodes.Add(new NodeData
            {
                ID = "Start", Text = string.Join("\n", Enumerable.Repeat("A short reply.", lines)),
                Choices = Enumerable.Range(0, choices).Select(i => new ChoiceData
                    { Text = "Reply " + i, Target = "Short" }).ToList()
            });
            data.Nodes.Add(new NodeData { ID = "Short", Text = "A short reply.",
                Choices = new List<ChoiceData> { new ChoiceData { Text = "Back.", Target = "Start" } } });
            ConversationLoader.Register(data);
            var speaker = new Entity { BlueprintName = "FlatDialogueLayoutSpeaker" };
            speaker.AddPart(new ConversationPart { ConversationID = data.ID });
            speaker.AddPart(new RenderPart { DisplayName = "Speaker", RenderString = "C" });
            Assert.True(ConversationManager.StartConversation(speaker, Player));
            var ui = Component<DialogueUI>();
            ui.Tilemap = Tiles(); ui.BgTilemap = Tiles(); ui.PopupCamera = OutsideCamera();
            ui.PlayerEntity = Player; ui.CurrentZone = Zone; ui.Open();
            Press(Key.Space, ui.HandleInput);
            return ui;
        }

        static void NativeLayout(DialogueUI ui)
        {
            var field = typeof(DialogueUI).GetField("_nativePortraitLayout", Flags);
            Assert.NotNull(field, "Dialogue must capture native portrait layout without changing legacy nodes.");
            field.SetValue(ui, true);
            Invoke(ui, "Render");
        }

        static object Invoke(DialogueUI ui, string method, params object[] args)
        {
            var found = typeof(DialogueUI).GetMethod(method, Flags);
            Assert.NotNull(found, method + " must serve the real popup path.");
            return found.Invoke(ui, args);
        }

        static int ChoiceAt(DialogueUI ui, int x, int y) => (int)Invoke(ui, "GetChoiceRowAtCell", x, y);
        static int VisibleLines(DialogueUI ui) => (int)typeof(DialogueUI)
            .GetProperty("VisibleTextLineCount", Flags).GetValue(ui);
        static int VisibleChoices => Mathf.Min(ConversationManager.VisibleChoices.Count, 10);
        static HashSet<Vector3Int> Painted(Tilemap map)
        {
            var result = new HashSet<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) result.Add(p);
            return result;
        }

        [Test]
        public void ShortNativeNodePadsAfterChoicesWithoutMovingOrAddingActions()
        {
            var ui = OpenNode();
            int firstChoice = 2 + VisibleLines(ui), count = VisibleChoices;
            var choices = ConversationManager.VisibleChoices.ToArray();
            int oldHeight = (int)Get(ui, "_popupH");
            NativeLayout(ui);
            Assert.AreEqual(10, Get(ui, "_popupH"), "Seven inner rows, two borders and the footer.");
            Assert.Greater((int)Get(ui, "_popupH"), oldHeight, "This short node actually exercises padding.");
            CollectionAssert.AreEqual(choices, ConversationManager.VisibleChoices);
            for (int i = 0; i < count; i++) Assert.AreEqual(i, ChoiceAt(ui, 12, firstChoice + i));
            for (int y = firstChoice + count; y < (int)Get(ui, "_popupH"); y++)
                Assert.AreEqual(-1, ChoiceAt(ui, 12, y), "Padding, bottom border and footer are not choices.");
            Assert.AreEqual(-1, ChoiceAt(ui, 5, firstChoice), "The image gutter remains noninteractive.");
            Assert.AreEqual(-1, ChoiceAt(ui, 12, firstChoice - 1));
            Press(Key.Enter, ui.HandleInput);
            Assert.AreEqual("Short", ConversationManager.CurrentNode.ID, "The original first action still confirms.");
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void LegacyOrDisabledPortraitPanelRetainsItsOriginalShortNodeHeight(bool panel, bool native)
        {
            var ui = OpenNode(); ui.UsePortraitPanel = panel;
            if (native) NativeLayout(ui); else Invoke(ui, "Render");
            Assert.AreEqual(VisibleLines(ui) + VisibleChoices + 4, Get(ui, "_popupH"));
            int first = VisibleLines(ui) + 2;
            Assert.AreEqual(0, ChoiceAt(ui, panel ? 12 : 2, first));
            Assert.AreEqual(-1, ChoiceAt(ui, 0, first));
            var before = Painted(ui.Tilemap);
            Press(Key.PageDown, ui.HandleInput);
            CollectionAssert.AreEquivalent(before, Painted(ui.Tilemap), "An endpoint page key does not relayout a short node.");
        }

        [TestCase(1280, 800)]
        [TestCase(1280, 720)]
        public void NativePortraitFitsActualMapOverlayAtIntegerScaleAndPointerStillSelectsRightColumn(int width, int height)
        {
            var ui = OpenNode(); NativeLayout(ui);
            var target = new RenderTexture(width, height, 0);
            var atlas = new Texture2D(48, 24);
            try
            {
                ui.PopupCamera.targetTexture = target;
                ui.PopupCamera.transform.position = new Vector3(0, 0, -10);
                var layout = GameplayViewportLayout.Measure(width / (float)height, 20f, 20f, 34);
                CenteredPopupLayout.ConfigureOverlayCamera(ui.PopupCamera, layout.MapRect, layout.MapAspect);
                float cellWidth = CenteredPopupLayout.ComputeCellWidth(layout.MapAspect);
                ui.Tilemap.layoutGrid.cellSize = new Vector3(cellWidth, 1, 0);
                ui.BgTilemap.layoutGrid.cellSize = new Vector3(cellWidth, 1, 0);
                var portraitField = typeof(DialogueUI).GetField("_portrait", Flags);
                Assert.NotNull(portraitField, "The layout borrows the selected atlas subrectangle.");
                portraitField.SetValue(ui, new ExaminePortrait(atlas, new Rect(1f / 3f, 0, 1f / 3f, 1), Color.white, false));
                Rect actual = (Rect)Invoke(ui, "MeasurePortraitScreenRect");
                Assert.GreaterOrEqual(actual.width, 32); Assert.GreaterOrEqual(actual.height, 48);
                Assert.That(actual.width / 16, Is.EqualTo(actual.height / 24).Within(.001f));
                foreach (float value in new[] { actual.x, actual.y, actual.width / 16, actual.height / 24 })
                    Assert.That(value, Is.EqualTo(Mathf.Round(value)).Within(.001f));

                int ox = (int)Get(ui, "_worldOriginX"), top = (int)Get(ui, "_worldTopY");
                Vector3 bottom = ui.PopupCamera.WorldToScreenPoint(ui.Tilemap.CellToWorld(new Vector3Int(ox + 1, top - 4, 0)));
                Vector3 upper = ui.PopupCamera.WorldToScreenPoint(ui.Tilemap.CellToWorld(new Vector3Int(ox + 9, top, 0)));
                Rect inner = Rect.MinMaxRect(bottom.x + 2, Screen.height - upper.y + 2,
                    upper.x - 2, Screen.height - bottom.y - 2);
                Assert.GreaterOrEqual(actual.xMin + .001f, inner.xMin);
                Assert.GreaterOrEqual(actual.yMin + .001f, inner.yMin);
                Assert.LessOrEqual(actual.xMax - .001f, inner.xMax);
                Assert.LessOrEqual(actual.yMax - .001f, inner.yMax);
                Vector3 labelTop = ui.PopupCamera.WorldToScreenPoint(ui.Tilemap.CellToWorld(new Vector3Int(ox + 1, top - 5, 0)));
                Assert.Less(actual.yMax, Screen.height - labelTop.y, "Art ends before faction row6.");

                int first = VisibleLines(ui) + 2;
                for (int i = 0; i < VisibleChoices; i++)
                {
                    var cell = new Vector3Int(ox + 12, top - first - i, 0);
                    var screen = ui.PopupCamera.WorldToScreenPoint(ui.Tilemap.GetCellCenterWorld(cell));
                    Assert.True(CenteredPopupLayout.ScreenToGrid(ui.PopupCamera, ui.Tilemap, screen, out int gx, out int gy));
                    Assert.AreEqual(cell.x, gx); Assert.AreEqual(cell.y, gy);
                    Assert.AreEqual(i, ChoiceAt(ui, gx - ox, top - gy));
                }
                var imageCenter = new Vector2(actual.center.x, Screen.height - actual.center.y);
                Assert.True(CenteredPopupLayout.ScreenToGrid(ui.PopupCamera, ui.Tilemap, imageCenter, out int px, out int py));
                Assert.AreEqual(-1, ChoiceAt(ui, px - ox, top - py));
                portraitField.SetValue(ui, default(ExaminePortrait));
                Assert.AreEqual(Rect.zero, Invoke(ui, "MeasurePortraitScreenRect"), "Absent art cannot leave a stale draw rectangle.");
            }
            finally
            {
                ui.PopupCamera.targetTexture = null;
                Object.DestroyImmediate(target); Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void LongToShortNativeNodeClearsTheOldFootprintAndCloseLeavesNoTiles()
        {
            var ui = OpenNode(80, 12); NativeLayout(ui);
            Assert.LessOrEqual((int)Get(ui, "_popupH"), CenteredPopupLayout.GridHeight - 2);
            var fg = Painted(ui.Tilemap); var bg = Painted(ui.BgTilemap);
            int oldHeight = (int)Get(ui, "_popupH");
            Press(Key.Enter, ui.HandleInput);
            Assert.AreEqual("Short", ConversationManager.CurrentNode.ID);
            Assert.AreEqual(10, Get(ui, "_popupH"));
            Assert.Less((int)Get(ui, "_popupH"), oldHeight);
            int x = (int)Get(ui, "_worldOriginX"), top = (int)Get(ui, "_worldTopY");
            int w = (int)Get(ui, "_popupW"), h = (int)Get(ui, "_popupH");
            Func<Vector3Int, bool> outside = p => p.x < x || p.x >= x + w || p.y > top || p.y <= top - h;
            Assert.True(fg.Any(outside), "The old footprint must really extend outside the short node.");
            foreach (var p in fg.Where(outside)) Assert.False(ui.Tilemap.HasTile(p));
            foreach (var p in bg.Where(outside)) Assert.False(ui.BgTilemap.HasTile(p));
            ui.Close(); Assert.IsEmpty(Painted(ui.Tilemap)); Assert.IsEmpty(Painted(ui.BgTilemap));
        }
    }
}
