using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // FIRST IMPORT: uses only existing production APIs, so these reach assertions
    // before either new HUD types or the history MessageLog API is introduced.
    public sealed class HudPolishBehaviorTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        Func<int> oldTick;
        [SetUp] public void Setup() { oldTick = MessageLog.TickProvider; MessageLog.Clear(); }
        [TearDown] public void Cleanup()
        { foreach (var go in objects) Object.DestroyImmediate(go); objects.Clear(); MessageLog.Clear(); MessageLog.TickProvider = oldTick; }

        [Test]
        public void HeaderNamesTheActualLineRangeAndInvalidatesItsMutableValue()
        {
            var p = new Entity(); var a = new ActivatedAbilitiesPart(); p.AddPart(a);
            var id = a.AddAbility("Exact spell", "Exact", "Skill", AbilityTargetingMode.DirectionLine, 9, "unlisted");
            var first = HotbarStateBuilder.Build(p, 0, null);
            StringAssert.Contains("line 9", first.SummaryText);
            a.GetAbility(id).Range = 2;
            var changed = HotbarStateBuilder.Build(p, 0, null);
            Assert.AreNotSame(first, changed); StringAssert.Contains("line 2", changed.SummaryText);
            Assert.AreSame(changed, HotbarStateBuilder.Build(p, 0, null));
        }

        [Test]
        public void SelfCenteredPendingHeaderDoesNotAskForAnImpossibleDirection()
        {
            var p = new Entity(); var a = new ActivatedAbilitiesPart(); p.AddPart(a);
            var id = a.AddAbility("Exact spell", "Exact", "Skill", AbilityTargetingMode.SelfCentered, 0, "unlisted");
            var pending = HotbarStateBuilder.Build(p, 0, a.GetAbility(id));
            StringAssert.DoesNotContain("choose a direction", pending.SummaryText);
            StringAssert.Contains("self", pending.SummaryText);
            StringAssert.Contains("AIM", pending.SummaryText);
        }

        [Test]
        public void ExactDuplicateBodiesAcrossTicksRemainSeparateButSameTickPairsCollapse()
        {
            int tick = 10; MessageLog.TickProvider = () => tick;
            MessageLog.Add("repeat"); MessageLog.Add("repeat"); tick = 11; MessageLog.Add("repeat");
            var snapshot = SidebarStateBuilder.Build(null, null, null);
            Assert.AreEqual(2, snapshot.LogEntriesNewestFirst.Count);
            Assert.AreEqual(1, snapshot.LogEntriesNewestFirst[0].Count); Assert.AreEqual(2, snapshot.LogEntriesNewestFirst[1].Count);
            Assert.AreEqual(3, MessageLog.GetRecentEntries(10).Count, "Presentation never rewrites stored history.");
        }

        [Test]
        public void ContiguousTickBoundaryAddsOneQuietRowWithoutFakeTurnProse()
        {
            var same = new[] { new SidebarLogEntry("new", 2, 1, 3), new SidebarLogEntry("old", 2, 1, 2) };
            var different = new[] { new SidebarLogEntry("new", 3, 1, 3), same[1] };
            var before = SidebarTextFormatter.FormatLog(same, 31); var after = SidebarTextFormatter.FormatLog(different, 31);
            Assert.AreEqual(before.Count + 1, after.Count);
            StringAssert.DoesNotContain("Turn", string.Join(" ", after.Select(l => l.Text)));
            StringAssert.Contains("old", after[0].Text); StringAssert.Contains("new", after.Last().Text);
        }

        [Test]
        public void SevenRowsKeepHaulAndDiscloseOverflowingStatus()
        {
            var rows = new[] { "HP 8/10 | MP 3", "LV 2 | XP 10/220", "AV 2 | DV 6", "WT 7/150 | DR 27", "TIME Dawn", "HAUL -10 Sp branch" };
            var snapshot = new SidebarSnapshot(rows, "poisoned, wet, chilled, tangled, inspired, dazed, soaked", null, null);
            var result = SidebarTextFormatter.FormatVitals(snapshot, 31, 7);
            foreach (var line in rows) CollectionAssert.Contains(result, line);
            Assert.AreEqual(7, result.Count); Assert.IsTrue(result.Last().EndsWith(">"), "Overflow is visible at the actual renderer's seven-row budget.");
        }

        [Test]
        public void CooldownMutationNeverOverwritesEitherRenderedNameRow()
        {
            var p = new Entity(); var a = new ActivatedAbilitiesPart(); p.AddPart(a);
            var id = a.AddAbility("Flaming Hands", "Exact", "Skill", AbilityTargetingMode.AdjacentCell, 1, "Pyromancy_FlamingHands");
            var r = Hotbar(out var text, out var camera); r.Render(HotbarStateBuilder.Build(p, 0, null), camera);
            var first = Row(text, 1); var second = Row(text, 0);
            a.GetAbility(id).CooldownRemaining = 12; r.Render(HotbarStateBuilder.Build(p, 0, null), camera);
            CollectionAssert.AreEqual(first, Row(text, 1)); CollectionAssert.AreEqual(second, Row(text, 0));
        }

        [Test]
        public void RenderedHudGlyphPixelsUseNormalAspectInsteadOfDoubleHeight()
        {
            var globalBefore = CP437TilesetGenerator.GetTextTile('A');
            var r = Sidebar(out var text, out var camera);
            r.Render(new SidebarSnapshot(new[] { "A" }, "-", null, null), camera, 34, false, 0);
            var positions = text.cellBounds.allPositionsWithin;
            Sprite sprite = null;
            foreach (var p in positions) { var candidate = text.GetSprite(p); if (candidate != null && candidate.name == "Text_41") { sprite = candidate; break; } }
            Assert.NotNull(sprite, "Actual rendered A, not a synthetic font-only assertion.");
            var rect = sprite.rect; var pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            int minX = 999, minY = 999, maxX = -1, maxY = -1;
            for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++) if (pixels[y * (int)rect.width + x].a > .5f)
            { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
            Assert.Greater(maxX, minX); Assert.LessOrEqual((float)(maxY - minY + 1) / (maxX - minX + 1), 1.7f);
            Assert.AreSame(globalBefore, CP437TilesetGenerator.GetTextTile('A'), "HUD rendering leaves the global atlas intact.");
        }

        [Test]
        public void LaterFocusEffectsRemainReadableWhileDividerStaysMuted()
        {
            var r = Sidebar(out var text, out var camera);
            var focus = new LookSnapshot(1, 1, "Target", "", new[] { "HP 9/10", "Afflicted", "ZZZ poison" }, null, null);
            r.Render(new SidebarSnapshot(new[] { "HP 9/10" }, "-", focus, null), camera, 34, false, 0);
            int details = 0, mutedDividers = 0;
            foreach (var p in text.cellBounds.allPositionsWithin)
            {
                var sprite = text.GetSprite(p);
                if (sprite != null && sprite.name == "Text_5A")
                { details++; Assert.AreEqual(QudColorParser.Gray, text.GetColor(p), "Later actual focus effects must not fade into the frame."); }
                if (sprite != null && sprite.name == "Text_B3")
                { mutedDividers++; Assert.AreEqual(QudColorParser.DarkGray, text.GetColor(p), "Decorative borders remain visually quieter than data."); }
            }
            Assert.AreEqual(3, details); Assert.Greater(mutedDividers, 0);
        }

        [Test]
        public void ActualTickRuleAndHudBackgroundContainVisiblePixelsWithoutChangingGlobalAtlas()
        {
            var global = CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.BoxHorizontal);
            var r = Sidebar(out var text, out var camera);
            var entries = new[] { new SidebarLogEntry("new", 2, 1, 2), new SidebarLogEntry("old", 1, 1, 1) };
            r.Render(new SidebarSnapshot(new[] { "HP 9/10" }, "-", null, entries), camera, 34, false, 0);
            int rules = 0;
            foreach (var p in text.cellBounds.allPositionsWithin)
            {
                if (text.GetColor(p) != new Color(.28f, .45f, .49f, 1)) continue;
                var sprite = text.GetSprite(p); Assert.NotNull(sprite);
                var rect = sprite.rect; var pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                Assert.IsTrue(pixels.Any(pixel => pixel.a > .5f), "A tile identity alone does not make the actual group rule visible."); rules++;
            }
            Assert.Greater(rules, 1);
            var hotbar = Hotbar(out _, out var hotbarCamera);
            hotbar.Render(HotbarStateBuilder.Build(null, 0, null), hotbarCamera);
            var background = (Tilemap)typeof(GameplayHotbarRenderer).GetField("_backgroundTilemap", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hotbar);
            var block = background.GetSprite(new Vector3Int(0, 5, 0)); Assert.NotNull(block);
            var area = block.rect;
            Assert.IsTrue(block.texture.GetPixels((int)area.x, (int)area.y, (int)area.width, (int)area.height).All(pixel => pixel.a > .5f));
            Assert.AreSame(global, CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.BoxHorizontal), "World and menu caches are untouched.");
        }

        [Test]
        public void ActualSidebarRefreshDoesNotKeepAStaleMiddleParagraph()
        {
            var r = Sidebar(out _, out var camera);
            var entries = new List<SidebarLogEntry> { new SidebarLogEntry("new", 2, 1, 3), new SidebarLogEntry("before", 2, 1, 2), new SidebarLogEntry("old", 2, 1, 1) };
            var snap = new SidebarSnapshot(new[] { "HP 9/10" }, "-", null, entries); r.Render(snap, camera, 34, false, 0);
            entries[1] = new SidebarLogEntry("after", 2, 1, 2); r.Render(snap, camera, 34, false, 0);
            var cache = (List<SidebarTextFormatter.LogLine>)typeof(GameplaySidebarRenderer).GetField("_cachedLogLines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(r);
            Assert.IsTrue(cache.Any(l => l.Text.Contains("after"))); Assert.IsFalse(cache.Any(l => l.Text.Contains("before")));
        }

        [Test]
        public void ExplicitSidebarClearThenSameSnapshotReallyRestoresItsTiles()
        {
            var r = Sidebar(out var text, out var camera); var snapshot = new SidebarSnapshot(new[] { "HP 9/10" }, "-", null, null);
            r.Render(snapshot, camera, 34, false, 0); Assert.Greater(text.GetUsedTilesCount(), 0);
            r.Clear(); Assert.AreEqual(0, text.GetUsedTilesCount()); r.Render(snapshot, camera, 34, false, 0);
            Assert.Greater(text.GetUsedTilesCount(), 0);
        }

        [Test]
        public void SupportedUnicodePunctuationAndDoubleRuleReuseVisibleHudGlyphs()
        {
            foreach (var pair in new[] { new[] { '—', '-' }, new[] { '–', '-' }, new[] { '…', '.' }, new[] { '“', '"' }, new[] { '”', '"' }, new[] { '‘', '\'' }, new[] { '’', '\'' } })
                Assert.AreSame(HudPixelFont.GetTile(pair[1]), HudPixelFont.GetTile(pair[0]), "Supported punctuation " + pair[0]);
            var rule = HudPixelFont.GetTile((char)0xCD);
            Assert.AreNotSame(HudPixelFont.GetTile('?'), rule);
            Assert.AreSame(rule, HudPixelFont.GetTile('═'));
            var rect = rule.sprite.rect;
            var pixels = rule.sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            var fullRows = Enumerable.Range(0, (int)rect.height).Where(y => Enumerable.Range(0, (int)rect.width).All(x => pixels[y * (int)rect.width + x].a > .5f)).ToArray();
            Assert.AreEqual(2, fullRows.Length, "Two real full-width strokes, not merely a named tile.");
            Assert.Greater(fullRows[1] - fullRows[0], 1);
            Assert.AreSame(HudPixelFont.GetTile('?'), HudPixelFont.GetTile('☃'), "Unknown glyph fallback remains bounded.");
        }

        [TestCase('\u00CD', 1)]
        [TestCase('\u00CD', 2)]
        [TestCase('\u00CD', 3)]
        [TestCase('═', 2)]
        public void ExactStoredTurnDividerBecomesOneRuleWithItsOwnChronologicalIdentity(char glyph, int fenceTick)
        {
            var entries = new[] { new SidebarLogEntry("new", 3, 1, 33), new SidebarLogEntry(new string(glyph, 40), fenceTick, 1, 22), new SidebarLogEntry("old", 1, 1, 11) };
            var lines = SidebarTextFormatter.FormatLog(entries, 31);
            Assert.AreEqual(3, lines.Count, "Explicit fences replace adjacent inferred tick rules and never wrap as text.");
            StringAssert.Contains("old", lines[0].Text); StringAssert.Contains("new", lines[2].Text);
            Assert.IsTrue(lines[1].IsGroupSeparator); Assert.AreEqual(22, lines[1].EntryNewestSerial);
            Assert.AreEqual(0, lines[1].RowIndexWithinEntry); Assert.AreEqual(1, lines[1].EntryRowCount);
            Assert.IsTrue(lines[1].MatchesIdentity(SidebarTextFormatter.FormatLog(entries, 12)[1]));
            Assert.IsFalse(lines[1].MatchesIdentity(lines[2]));
        }

        [TestCase("short")]
        [TestCase("long")]
        [TestCase("mixed")]
        [TestCase("prefix")]
        public void SimilarLookingOrdinaryMessagesAreNotDiscardedAsTurnDividers(string variant)
        {
            string body = variant == "short" ? new string((char)0xCD, 39)
                : variant == "long" ? new string('═', 41)
                : variant == "mixed" ? new string((char)0xCD, 39) + "═"
                : "x" + new string((char)0xCD, 40);
            var lines = SidebarTextFormatter.FormatLog(new[] { new SidebarLogEntry(body, 1, 1, 7) }, 100);
            Assert.AreEqual(1, lines.Count); Assert.IsFalse(lines[0].IsGroupSeparator);
            Assert.AreEqual(":: " + body, lines[0].Text); Assert.AreEqual(7, lines[0].EntryNewestSerial);
        }

        [Test]
        public void ActualSidebarRendersStoredFenceAndPunctuationWithoutQuestionMarksOrRewritingHistory()
        {
            int tick = 1; MessageLog.TickProvider = () => tick;
            MessageLog.Add("old"); tick = 2; MessageLog.Add(new string((char)0xCD, 40)); MessageLog.Add("Character—‘ready’…");
            var stored = MessageLog.GetRecentEntries(10).ToArray();
            var snapshot = SidebarStateBuilder.Build(null, null, null);
            var r = Sidebar(out var text, out var camera); r.Render(snapshot, camera, 34, false, 0);
            var ruleRows = new HashSet<int>(); int dashes = 0;
            foreach (var p in text.cellBounds.allPositionsWithin)
            {
                var sprite = text.GetSprite(p); if (sprite == null) continue;
                Assert.AreNotEqual("Text_3F", sprite.name, "Actual displayed punctuation/fence must not be an unknown glyph.");
                if (sprite.name == "Text_2D") dashes++;
                if (text.GetColor(p) == new Color(.28f, .45f, .49f, 1))
                {
                    ruleRows.Add(p.y); var rect = sprite.rect;
                    Assert.IsTrue(sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height).Any(pixel => pixel.a > .5f));
                }
            }
            Assert.AreEqual(1, ruleRows.Count); Assert.Greater(dashes, 0);
            var after = MessageLog.GetRecentEntries(10).ToArray();
            Assert.AreEqual(stored.Length, after.Length);
            for (int i = 0; i < stored.Length; i++)
            { Assert.AreEqual(stored[i].Text, after[i].Text); Assert.AreEqual(stored[i].Tick, after[i].Tick); Assert.AreEqual(stored[i].Serial, after[i].Serial); }
        }

        [TestCase(false)] [TestCase(true)]
        public void StableThoughtsWithChangedHiddenLogDoNoIdleRepaintAndReturnToCurrentLog(bool prewarmLog)
        {
            var r=Sidebar(out _,out var camera);
            var entries=new List<SidebarLogEntry>{new SidebarLogEntry("old hidden log",1,1,1)};
            var log=new SidebarSnapshot(new[]{"HP 9/10"},"-",null,entries);
            if(prewarmLog)r.Render(log,camera,34,false,0);
            var thoughts=new[]{new SidebarThoughtEntry("rat","watching")};
            var thinking=new SidebarSnapshot(log.VitalLines,"-",null,entries,thoughts);
            r.Render(thinking,camera,34,false,0);
            entries[0]=new SidebarLogEntry("current hidden log",2,1,2);
            r.Render(thinking,camera,34,false,0);

            CavesOfOoo.Diagnostics.PerformanceDiagnostics.BeginFrame(0,false,false);
            for(int i=0;i<10;i++)r.Render(thinking,camera,34,false,0);
            CavesOfOoo.Diagnostics.PerformanceDiagnostics.EndFrame(0);
            var frame=CavesOfOoo.Diagnostics.PerformanceDiagnostics.LastCompletedFrameSnapshot;
            Assert.AreEqual(0,frame.SidebarRenderCount,"Hidden log cache cannot force unchanged THOUGHTS frames to repaint.");
            Assert.AreEqual(0,frame.TilemapClearCount);

            thoughts[0]=new SidebarThoughtEntry("rat","fleeing");
            CavesOfOoo.Diagnostics.PerformanceDiagnostics.BeginFrame(0,false,false);
            r.Render(thinking,camera,34,false,0);
            CavesOfOoo.Diagnostics.PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(1,CavesOfOoo.Diagnostics.PerformanceDiagnostics.LastCompletedFrameSnapshot.SidebarRenderCount,
                "An actual visible thought mutation still redraws.");
            r.Render(log,camera,34,false,0);
            var wrapped=(List<SidebarTextFormatter.LogLine>)typeof(GameplaySidebarRenderer).GetField("_cachedLogLines",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(r);
            Assert.IsTrue(wrapped.Any(line=>line.Text.Contains("current hidden log")),"Returning to LOG refreshes its independent cache.");
            Assert.IsFalse(wrapped.Any(line=>line.Text.Contains("old hidden log")));
        }

        GameplayHotbarRenderer Hotbar(out Tilemap text, out Camera camera)
        { Maps(out text, out var bg, out camera, out _); camera.aspect = 8; return new GameplayHotbarRenderer(text, bg); }
        GameplaySidebarRenderer Sidebar(out Tilemap text, out Camera camera)
        { Maps(out text, out var bg, out camera, out var grid); camera.aspect = .425f; return new GameplaySidebarRenderer(text, bg, grid, 20); }
        void Maps(out Tilemap text, out Tilemap bg, out Camera camera, out Transform transform)
        {
            var root = Go("HUD behavior"); root.AddComponent<Grid>().cellSize = new Vector3(.5f, 1, 0); transform = root.transform;
            text = Go("text").AddComponent<Tilemap>(); text.transform.SetParent(root.transform);
            bg = Go("bg").AddComponent<Tilemap>(); bg.transform.SetParent(root.transform);
            camera = Go("camera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 20;
        }
        GameObject Go(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        static TileBase[] Row(Tilemap map, int y) => Enumerable.Range(0, 8).Select(x => map.GetTile(new Vector3Int(x, y, 0))).ToArray();
    }
}
