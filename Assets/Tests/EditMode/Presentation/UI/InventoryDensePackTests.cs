using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        static object DenseGeometry(int width, int height, float scale = 1)
        {
            var type = typeof(InventoryUI).Assembly.GetType("CavesOfOoo.Rendering.InventoryPackLayout");
            Assert.NotNull(type, "A shared geometry must govern dense paint, navigation and hit rectangles.");
            return type.GetMethod("Create").Invoke(null, new object[] { (float)width, (float)height, scale });
        }
        static int DenseMove(object layout, int selected, int count, string direction)
        {
            var method = layout.GetType().GetMethod("Move");
            return (int)method.Invoke(layout, new[] { (object)selected, count, Enum.Parse(method.GetParameters()[2].ParameterType, direction) });
        }
        static Entity DenseSelected(Rig r)
        {
            var rows = r.Rows("_rows"); int index = (int)Get(r.UI, "_cursorIndex");
            return index >= 0 && index < rows.Count ? (Get(rows[index], "Item") as InventoryScreenData.ItemDisplay)?.Item : null;
        }
        static Entity[] DenseOwners(Rig r) => r.Rows("_rows").Cast<object>().Select(x => Get(x, "Item"))
            .OfType<InventoryScreenData.ItemDisplay>().Select(x => x.Item).ToArray();

        [TestCase(1280, 720, 36)] [TestCase(1280, 800, 44)]
        [TestCase(1920, 1080, 64)] [TestCase(2560, 1440, 88)]
        public void DenseGeometryMeetsReadableCapacityAndNameWidth(int width, int height, int minimum)
        {
            var g = DenseGeometry(width, height);
            Assert.AreEqual(2, Get(g, "Columns")); Assert.GreaterOrEqual((int)Get(g, "Capacity"), minimum);
            Assert.GreaterOrEqual((float)Get(g, "NameWidth"), 320);
            var list = (Rect)Get(g, "ListBounds");
            for (int i = 0; i < (int)Get(g, "Capacity"); i++)
            {
                var row = (Rect)Call(g, "RowBounds", i);
                Assert.True(list.Contains(row.min + Vector2.one)); Assert.True(list.Contains(row.max - Vector2.one));
                for (int j = 0; j < i; j++) Assert.False(row.Overlaps((Rect)Call(g, "RowBounds", j)));
            }
        }
        [TestCase(640, 480, 1f)] [TestCase(900, 800, 1f)] [TestCase(1280, 800, 1.5f)]
        public void DenseNarrowOrLargeTextFallsBackWithoutLosingReachability(int width, int height, float scale)
        {
            var g = DenseGeometry(width, height, scale);
            Assert.AreEqual(1, Get(g, "Columns")); Assert.Greater((int)Get(g, "Capacity"), 0);
            int at = DenseMove(g, 0, 200, "Last"); Assert.AreEqual(199, at);
            Assert.AreEqual(0, DenseMove(g, at, 200, "First"));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(21)] [TestCase(22)] [TestCase(23)]
        [TestCase(43)] [TestCase(44)] [TestCase(45)] [TestCase(200)]
        public void DenseNavigationKeepsEveryResultBoundedAndEveryOwnerReachable(int count)
        {
            var g = DenseGeometry(1280, 800); var reached = new HashSet<int>();
            var queue = new Queue<int>(); queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(); if (!reached.Add(current)) continue;
                foreach (string direction in new[] { "Up", "Down", "Left", "Right", "PageUp", "PageDown", "First", "Last" })
                {
                    int next = DenseMove(g, current, count, direction);
                    Assert.That(next, Is.InRange(0, Math.Max(0, count - 1)));
                    if (!reached.Contains(next)) queue.Enqueue(next);
                }
            }
            Assert.AreEqual(Math.Max(1, count), reached.Count);
        }
        [Test]
        public void DenseRaggedColumnAndPageEdgesRetainClosestValidRow()
        {
            var g = DenseGeometry(1280, 800); int rows = (int)Get(g, "Rows"), cap = (int)Get(g, "Capacity");
            Assert.AreEqual(rows + 3, DenseMove(g, 3, 200, "Right"));
            Assert.AreEqual(3, DenseMove(g, rows + 3, 200, "Left"));
            Assert.AreEqual(rows + 2, DenseMove(g, rows - 1, rows + 3, "Right"));
            Assert.AreEqual(cap, DenseMove(g, rows - 1, 200, "Down"));
            Assert.AreEqual(rows - 1, DenseMove(g, cap, 200, "Up"));
            Assert.AreEqual(cap + 2, DenseMove(g, rows + 3, cap + 3, "PageDown"));
        }
        [TestCase(1280, 800, 1f, 22, 47, 44)]
        [TestCase(1280, 800, 1f, 23, 47, 45)]
        [TestCase(1280, 800, 1.5f, 10, 23, 20)]
        public void DenseReviewPageKeepsRowWhenDestinationColumnDisappears(int width, int height, float scale, int selected, int count, int expected)
        { Assert.AreEqual(expected, DenseMove(DenseGeometry(width, height, scale), selected, count, "PageDown")); }

        [TestCase(2560, 720, 2f)] [TestCase(1280, 360, 1f)]
        public void DenseReviewShortWideViewUsesDetailsFallbackWithoutNegativeTextBounds(int width, int height, float scale)
        {
            var g = DenseGeometry(width, height, scale); Assert.False((bool)g.GetType().GetProperty("HasDetails").GetValue(g));
            using (var r = new Rig(width, height))
            {
                r.Carry("Dagger"); r.Open(); r.Tab(1); r.UI.GetType().GetField("_denseTextScale", Members).SetValue(r.UI, scale); Call(r.UI, "Render");
                foreach (var label in r.Labels()) Assert.GreaterOrEqual(label.rectTransform.rect.height, 0, label.Content);
                Assert.True(r.Hits().Any(x => Kind(x) == "Details"));
            }
        }
        [TestCase(1920, 1080, 1.6f, 1f)] [TestCase(1280, 800, 1.9f, 1f)] [TestCase(1280, 800, 1.6f, 1f)]
        [TestCase(640, 480, 1.6f, 2f)]
        public void DenseReviewCanvasProjectionFillsTargetUnderPinnedGameplayAspect(int width, int height, float aspect, float textScale)
        {
            using (var mouse = new NativeMouseScope()) using (var r = new Rig(width, height))
            {
                r.Camera.aspect = aspect; var item = r.Carry("Dagger"); r.Open(); r.Tab(1);
                r.UI.GetType().GetField("_denseTextScale", Members).SetValue(r.UI, textScale); Call(r.UI, "Render");
                foreach (float currentAspect in new[] { aspect, aspect * 1.1f })
                {
                    r.Camera.aspect = currentAspect; Canvas.ForceUpdateCanvases(); r.UI.HandleInput(); Canvas.ForceUpdateCanvases();
                    var rect = (RectTransform)Get(r.UI, "_ledgerContent"); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    var lower = RectTransformUtility.WorldToScreenPoint(r.Camera, corners[0]); var upper = RectTransformUtility.WorldToScreenPoint(r.Camera, corners[2]);
                    Assert.That(lower.x, Is.EqualTo(r.Camera.pixelRect.xMin).Within(.5f));
                    Assert.That(lower.y, Is.EqualTo(r.Camera.pixelRect.yMin).Within(.5f));
                    Assert.That(upper.x, Is.EqualTo(r.Camera.pixelRect.xMax).Within(.5f));
                    Assert.That(upper.y, Is.EqualTo(r.Camera.pixelRect.yMax).Within(.5f));
                    var row = Bounds(r.FindHit("Item", r.InventoryRow(item)));
                    float scale = (float)Get(Get(r.UI, "_denseLayout"), "Scale");
                    Assert.That(Mathf.Abs(r.ToScreen(row.min).y - r.ToScreen(row.max).y), Is.EqualTo(24 * scale).Within(.1f));
                    Assert.That(Mathf.Abs(r.ToScreen(row.min).x - r.ToScreen(row.min + new Vector2(10, 0)).x), Is.EqualTo(10 * scale).Within(.1f));
                }
            }
        }
        [TestCase(1280, 800, 44)] [TestCase(1920, 1080, 64)]
        public void DensePaintShowsActualOwnersWithoutSpendingRowsOnHeaders(int width, int height, int minimum)
        {
            using (var r = new Rig(width, height))
            {
                for (int i = 0; i < 100; i++) r.Carry(i % 2 == 0 ? "Dagger" : "HealingTonic", "owner " + i);
                r.Open(); r.Tab(1);
                var hits = r.Hits().Where(x => Kind(x) == "Item").ToArray();
                Assert.GreaterOrEqual(hits.Length, minimum);
                Assert.AreEqual(hits.Length, hits.Select(Index).Distinct().Count());
                foreach (var hit in hits)
                {
                    Assert.NotNull(Get(r.Rows("_rows")[Index(hit)], "Item"));
                    Assert.AreEqual(Index(hit), Index(r.Hits()[r.HitAtScreen(r.ToScreen(Bounds(hit).center))]));
                }
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase(1280, 720, 1f)] [TestCase(1280, 800, 1f)] [TestCase(1920, 1080, 1f)]
        [TestCase(1280, 800, 1.5f)] [TestCase(640, 480, 1f)]
        public void DenseSectionOverviewKeepsCapacityAndCompleteDetailsReachable(int width, int height, float scale)
        {
            using (var r = new Rig(width, height))
            {
                var item = r.Carry("Dagger", "hybrid equipment"); item.AddPart(new ArmorPart { AV = 3, DV = 1 });
                for (int i = 0; i < 100; i++) r.Carry("Dagger", "ordinary owner " + i);
                r.Open(); r.Tab(1);
                r.UI.GetType().GetField("_denseTextScale", Members).SetValue(r.UI, scale); Call(r.UI, "Render");
                // Place the real selection on its page before finding its painted hit.
                r.UI.GetType().GetField("_cursorIndex", Members).SetValue(r.UI, r.InventoryRow(item)); Call(r.UI, "Render");
                Call(r.UI, "SelectLedgerHit", r.FindHit("Item", r.InventoryRow(item))); Call(r.UI, "Render");
                var layout = Get(r.UI, "_denseLayout");
                Assert.AreEqual(Get(DenseGeometry(width, height, scale), "Capacity"), Get(layout, "Capacity"));
                var labels = r.Labels().ToList();
                Assert.True(labels.Any(label => label.Content.Contains("Details") || label.Content.Contains("details")));
                if ((bool)layout.GetType().GetProperty("HasDetails").GetValue(layout))
                {
                    var combat = labels.Single(label => label.Content == "COMBAT");
                    var armor = labels.Single(label => label.Content == "PROTECTION");
                    var damage = labels.Single(label => label.Content.Contains("per penetration"));
                    Assert.AreNotEqual(combat.color, damage.color); Assert.AreEqual(combat.color, armor.color);
                    var pane = (Rect)Get(layout, "DetailBounds");
                    foreach (var label in new[] { combat, armor, damage })
                    {
                        var rect = r.BoundsOf(label);
                        Assert.True(pane.Contains(rect.min + Vector2.one)); Assert.True(pane.Contains(rect.max - Vector2.one));
                        var lines = label.Content.Split('\n');
                        Assert.LessOrEqual(lines.Length * label.GlyphHeight + (lines.Length - 1) * 4, rect.height + .1f);
                        Assert.LessOrEqual(lines.Max(line => line.Length) * label.GlyphHeight * .5f, rect.width + .1f);
                    }
                }
                var document = r.UI.SelectedDecisionPresentation(out string text);
                Assert.NotNull(document);
                Assert.True(document.Sections.Any(section => section.Heading == "Combat"));
                Assert.True(document.Sections.Any(section => section.Heading == "Protection"));
                Assert.AreSame(item, DenseSelected(r)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void DenseRightColumnSelectsExactOwnerAndOuterDirectionsNeverSwitchModes()
        {
            using (var input = new NativeNavigationScope()) using (var r = new Rig())
            {
                for (int i = 0; i < 60; i++) r.Carry("Dagger", "same dagger");
                r.Open(); r.Tab(1); CountedToggle(r); var owners = DenseOwners(r);
                int rows = (int)Get(DenseGeometry(1280, 800), "Rows");
                input.Keys(Key.RightArrow); r.UI.HandleInput();
                Assert.AreEqual(1, Get(r.UI, "_panel")); Assert.AreSame(owners[rows], DenseSelected(r));
                input.Keys(); r.UI.HandleInput(); input.Keys(Key.RightArrow); r.UI.HandleInput();
                Assert.AreEqual(1, Get(r.UI, "_panel")); Assert.AreSame(owners[rows], DenseSelected(r));
                var hit = r.FindHit("Item", r.InventoryRow(owners[rows + 3])); r.ActivatePainted(hit);
                Assert.AreSame(owners[rows + 3], Get(Get(r.UI, "_itemActionPopup"), "Item"));
            }
        }
        [Test]
        public void DenseHomeEndAndPageKeysRemainFreeAndReachTheLastActualOwner()
        {
            using (var input = new NativeNavigationScope()) using (var r = new Rig())
            {
                for (int i = 0; i < 70; i++) r.Carry("Dagger", "owner " + i);
                r.Open(); r.Tab(1); var owners = DenseOwners(r);
                input.Keys(Key.PageDown); r.UI.HandleInput(); Assert.AreSame(owners[44], DenseSelected(r));
                input.Keys(Key.End); r.UI.HandleInput(); Assert.AreSame(owners.Last(), DenseSelected(r));
                input.Keys(Key.Home); r.UI.HandleInput(); Assert.AreSame(owners[0], DenseSelected(r));
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }
        [Test]
        public void DenseRemovalChoosesNearestRemainingOwnerRatherThanFirst()
        {
            using (var r = new Rig())
            {
                for (int i = 0; i < 8; i++) r.Carry("Dagger", "same dagger");
                r.Open(); r.Tab(1); CountedToggle(r); var owners = DenseOwners(r);
                Call(r.UI, "SelectLedgerHit", r.FindHit("Item", r.InventoryRow(owners[5])));
                Assert.True(r.Pack.RemoveObject(owners[5])); Call(r.UI, "Rebuild"); Call(r.UI, "Render");
                Assert.AreSame(owners[6], DenseSelected(r));
            }
        }
        [TestCase(-1, "unlimited")] [TestCase(150, "150")] [TestCase(5000, "5000")]
        public void DenseHeaderUsesCurrentHardLimitAndDistinctSoftLimit(int limit, string caption)
        {
            using (var r = new Rig())
            {
                r.Pack.MaxWeight = limit; r.Carry("Dagger"); r.Open(); r.Tab(1);
                string text = string.Join("\n", r.Labels().Select(x => x.Content));
                StringAssert.Contains("Hard " + caption, text); StringAssert.Contains("Soft " + r.Pack.GetMaxCarryWeight(), text);
            }
        }
        [Test]
        public void DenseNamesRemoveOnlyGeneratedStackSuffixAndRetainStrikerCharges()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("HealingTonic", "cargo (x7)"); item.GetPart<StackerPart>().StackCount = 3;
                var striker = r.Factory.CreateEntity("FlintStriker"); Assert.True(r.Pack.AddObject(striker)); r.Open(); r.Tab(1);
                Assert.True(r.Labels().Any(x => x.Content == "cargo (x7)"));
                Assert.True(r.Labels().Any(x => x.Content == striker.GetDisplayName()));
                Assert.True(r.Labels().Any(x => x.Content == "3"));
            }
        }
        [Test]
        public void DenseFilterCancellationAndRepaintPreserveOwnerAndActionSnapshot()
        {
            using (var input = new NativeNavigationScope()) using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var query = new DenseQueryProbe(); item.AddPart(query);
                r.Open(); r.Tab(1); int before = query.Queries;
                var state = Get(r.UI, "_state"); for (int i = 0; i < 3; i++) Call(r.UI, "Render");
                Assert.AreSame(state, Get(r.UI, "_state")); Assert.AreEqual(before, query.Queries);
                Call(r.UI, "BeginInventorySearch"); Call(r.UI, "SetInventoryQuery", "no such owner");
                Assert.IsNull(DenseSelected(r)); input.Keys(Key.Escape); r.UI.HandleInput();
                Assert.AreSame(item, DenseSelected(r)); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void DenseResizePreservesSelectedOwnerBackingSnapshotAndParkedPointer()
        {
            using (var mouse = new NativeMouseScope()) using (var r = new Rig())
            {
                for (int i = 0; i < 70; i++) r.Carry("Dagger", "same dagger");
                r.Open(); r.Tab(1); CountedToggle(r); Call(r.UI, "MoveDensePack", Enum.Parse(typeof(InventoryUI).Assembly.GetType("CavesOfOoo.Rendering.PackNavigation"), "Last")); Call(r.UI, "Render");
                var selected = DenseSelected(r); var probe = new DenseQueryProbe(); selected.AddPart(probe);
                Call(r.UI, "Rebuild"); Call(r.UI, "Render");
                var state = Get(r.UI, "_state"); var document = Get(r.UI, "_ledgerSelectionDocument"); int queries = probe.Queries;
                mouse.State(r.ToScreen(Bounds(r.FindHit("Item", r.InventoryRow(selected))).center)); r.UI.HandleInput();
                var old = r.Camera.targetTexture;
                foreach (var size in new[] { new Vector2Int(900, 800), new Vector2Int(1920, 1080), new Vector2Int(1280, 800) })
                {
                    var target = new RenderTexture(size.x, size.y, 0);
                    try
                    {
                        r.Camera.targetTexture = target; r.Camera.aspect = (float)size.x / size.y; Canvas.ForceUpdateCanvases();
                        r.UI.HandleInput();
                        Assert.AreSame(selected, DenseSelected(r)); Assert.AreSame(state, Get(r.UI, "_state"));
                        Assert.AreSame(document, Get(r.UI, "_ledgerSelectionDocument")); Assert.AreEqual(queries, probe.Queries);
                        Assert.AreEqual(size.x == 900 ? 1 : 2, Get(Get(r.UI, "_denseLayout"), "Columns"));
                        Assert.AreEqual((float)size.x, ((RectTransform)Get(r.UI, "_ledgerContent")).rect.width);
                    }
                    finally { r.Camera.targetTexture = old; UnityEngine.Object.DestroyImmediate(target); }
                }
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void DenseWheelOnlyMovesTheHoveredPackAndCannotReachThroughAModal()
        {
            using (var mouse = new NativeMouseScope()) using (var r = new Rig())
            {
                for (int i = 0; i < 3; i++) r.Carry("Dagger", "owner " + i);
                r.Open(); r.Tab(1); var owners = DenseOwners(r);
                mouse.State(r.ToScreen(new Vector2(14, 20)), wheel: -120); r.UI.HandleInput(); Assert.AreSame(owners[0], DenseSelected(r));
                mouse.State(r.ToScreen(Bounds(r.FindHit("Item", r.InventoryRow(owners[0]))).center)); r.UI.HandleInput();
                mouse.State(r.ToScreen(Bounds(r.FindHit("Item", r.InventoryRow(owners[0]))).center), wheel: -120); r.UI.HandleInput();
                Assert.AreSame(owners[1], DenseSelected(r));
                r.ActivatePainted(r.FindHit("Item", r.InventoryRow(owners[1]))); var popup = Get(r.UI, "_itemActionPopup"); int cursor = (int)Get(popup, "CursorIndex");
                mouse.State(r.ToScreen(new Vector2(30, 200)), wheel: -120); r.UI.HandleInput();
                Assert.AreEqual(cursor, Get(popup, "CursorIndex")); Assert.AreSame(owners[1], DenseSelected(r));
                mouse.State(r.ToScreen(new Vector2(30, 200)), pressed: true); r.UI.HandleInput();
                Assert.AreSame(popup, Get(r.UI, "_itemActionPopup")); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [TestCase(640, 480)] [TestCase(1920, 1080)]
        public void DenseTranslatedModalKeepsLateAbsoluteIndicesAndIsolatesBackground(int width, int height)
        {
            using (var r = new Rig(width, height))
            {
                var item = r.Carry("Dagger"); r.Open(); r.Tab(1);
                var display = InventoryScreenData.Build(r.Player).Categories.SelectMany(c => c.Items).Single(i => i.Item == item);
                display.Actions = Enumerable.Range(0, 32).Select(i => new InventoryAction("dense" + i, "choice " + i, "dense_test_" + i)).ToList();
                Call(r.UI, "OpenItemActionPopup", display); var popup = Get(r.UI, "_itemActionPopup");
                int last = ((System.Collections.IList)Get(popup, "Actions")).Count - 1;
                popup.GetType().GetField("CursorIndex", Members).SetValue(popup, last); Call(r.UI, "Render");
                Assert.False(r.Hits().Any(x => Kind(x) == "Item" || Kind(x) == "Tab"));
                var hit = r.FindHit("PopupRow", last); int actual = r.HitAtScreen(r.ToScreen(Bounds(hit).center)); Assert.GreaterOrEqual(actual, 0);
                Assert.AreEqual(last, Index(r.Hits()[actual]));
                var frame = (Rect)Get(r.UI, "_ledgerModalBounds"); var content = ((RectTransform)Get(r.UI, "_ledgerContent")).rect;
                Assert.That(frame.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(frame.xMax, Is.LessThanOrEqualTo(content.width));
                Assert.That(frame.yMin, Is.GreaterThanOrEqualTo(0)); Assert.That(frame.yMax, Is.LessThanOrEqualTo(content.height));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        [Test]
        public void DensePackTextControlsKeepTheSameOwnerAndDoNotRequeryActions()
        {
            using (var r = new Rig())
            {
                var item = r.Carry("Dagger"); var probe = new DenseQueryProbe(); item.AddPart(probe); r.Open(); r.Tab(1);
                var state = Get(r.UI, "_state"); int queries = probe.Queries;
                r.ActivatePainted(r.FindHit("PackTextScale", 1));
                Assert.AreEqual(1.25f, Get(Get(r.UI, "_denseLayout"), "RequestedScale"));
                Assert.AreSame(item, DenseSelected(r)); Assert.AreSame(state, Get(r.UI, "_state")); Assert.AreEqual(queries, probe.Queries);
                r.ActivatePainted(r.FindHit("PackTextScale", -1));
                Assert.AreEqual(1f, Get(Get(r.UI, "_denseLayout"), "RequestedScale")); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }
        sealed class DenseQueryProbe : Part
        {
            public int Queries; public override string Name => "DenseQueryProbe";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "GetInventoryActions") Queries++; return true; }
        }
    }
}
