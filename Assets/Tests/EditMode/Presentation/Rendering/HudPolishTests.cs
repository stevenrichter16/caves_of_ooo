using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class HudPolishTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        Func<int> previousTick;
        [SetUp] public void Setup() { previousTick = MessageLog.TickProvider; MessageLog.Clear(); PerformanceDiagnostics.ResetAll(); }
        [TearDown] public void Cleanup()
        {
            foreach (var go in objects) Object.DestroyImmediate(go);
            objects.Clear(); MessageLog.Clear(); MessageLog.TickProvider = previousTick; PerformanceDiagnostics.ResetAll();
        }

        [TestCase("Pyromancy_EmberSpit", SkillHudIcon.EmberSpit)]
        [TestCase("Pyromancy_FlamingHands", SkillHudIcon.FlamingHands)]
        [TestCase("Hydromancy_JetBlast", SkillHudIcon.JetBlast)]
        [TestCase("Galvanism_GroundSurge", SkillHudIcon.GroundSurge)]
        [TestCase("Cryomancy_RimeGrip", SkillHudIcon.RimeGrip)]
        [TestCase("Spellcraft_Calm", SkillHudIcon.Calm)]
        public void ActualPowerIdentitySelectsDistinctIcon(string power, SkillHudIcon expected)
        {
            var player = Player("Wrong display name", power, out var ability);
            var slot = HotbarStateBuilder.Build(player, 0, null).Slots[0];
            Assert.AreEqual(expected, slot.Icon);
            ability.SourcePowerClass = "Unrelated_" + power;
            Assert.AreEqual(SkillHudIcon.Fallback, HotbarStateBuilder.Build(player, 0, null).Slots[0].Icon,
                "Display-name imitation must not claim authored art.");
        }

        [TestCase("Ember Spit", "Ember", "Spit")]
        [TestCase("Flaming Hands", "Flaming", "Hands")]
        [TestCase("Jet Blast", "Jet", "Blast")]
        [TestCase("Ground Surge", "Ground", "Surge")]
        [TestCase("Rime Grip", "Rime", "Grip")]
        [TestCase("Calm", "Calm", "")]
        [TestCase("En Garde", "En", "Garde")]
        [TestCase("  En\t Garde\r\n", "En", "Garde")]
        [TestCase("Lunge", "Lunge", "")]
        [TestCase("Follow Through", "Follow", "Through")]
        [TestCase("Conjure Rain", "Conjure", "Rain")]
        [TestCase("ABCDEFG", "ABCDEFG", "")]
        [TestCase("ABCDEFGH", "ABCDEFG", "H")]
        [TestCase("ABCDEFGHIJKLMN", "ABCDEFG", "HIJKLMN")]
        [TestCase("ABCDEFGHIJKLMNO", "ABCDEFG", "HIJKLM>")]
        [TestCase(null, "", "")]
        [TestCase("", "", "")]
        [TestCase(" \t\r\n", "", "")]
        public void CardsKeepRealWordsAcrossTwoRows(string name, string first, string second)
        {
            HudHotbarPresentation.NameLines(name, out string a, out string b);
            Assert.AreEqual(first, a); Assert.AreEqual(second, b);
            // Reserve one cell of name capacity; word wrap gives En/Garde inset.
            Assert.LessOrEqual(a.Length, 7);
            Assert.LessOrEqual(b.Length, 7);
        }

        [Test]
        public void HeaderTracksActualMutableTargetRangeWithoutFiringAbility()
        {
            var player = Player("Exact Power", "unlisted", out var ability);
            ability.TargetingMode = AbilityTargetingMode.DirectionLine; ability.Range = 9;
            var first = HotbarStateBuilder.Build(player, 0, null);
            StringAssert.Contains("line 9", first.SummaryText);
            Assert.AreSame(first, HotbarStateBuilder.Build(player, 0, null));
            ability.Range = 2; ability.TargetingMode = AbilityTargetingMode.SelfCentered;
            var second = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreNotSame(first, second); StringAssert.Contains("self", second.SummaryText);
            StringAssert.DoesNotContain("line 9", second.SummaryText);
            Assert.AreEqual(9, first.Slots[0].Range);
        }

        [Test]
        public void PendingSelectedCooldownAndEmptyAreDifferentTextStates()
        {
            var player = Player("Flaming Hands", "Pyromancy_FlamingHands", out var ability);
            Assert.AreEqual("READY", HudHotbarPresentation.StateLabel(HotbarStateBuilder.Build(player, 0, null).Slots[0]));
            ability.CooldownRemaining = 12;
            Assert.AreEqual("CD12", HudHotbarPresentation.StateLabel(HotbarStateBuilder.Build(player, 0, null).Slots[0]));
            Assert.AreEqual("AIM", HudHotbarPresentation.StateLabel(HotbarStateBuilder.Build(player, 0, ability).Slots[0]));
            Assert.AreEqual("EMPTY", HudHotbarPresentation.StateLabel(HotbarStateBuilder.Build(player, 0, null).Slots[9]));
        }

        [Test]
        public void RendererShowsAllTenCardsAndCooldownDoesNotReplaceNames()
        {
            var player = Player("Flaming Hands", "Pyromancy_FlamingHands", out var ability);
            var renderer = Renderer(out var text, out _, out var camera);
            var first = HotbarStateBuilder.Build(player, 0, null); renderer.Render(first, camera);
            Assert.AreEqual(6, GameplayHotbarLayout.GridHeight); Assert.AreEqual(80, GameplayHotbarLayout.GridWidth);
            for (int i = 0; i < 10; i++) Assert.AreSame(HudPixelFont.GetTile(HotbarStateBuilder.SlotToHotkey(i)), text.GetTile(new Vector3Int(i * 8 + 1, 4, 0)));
            var firstRow = Row(text, 0, 1, 8); var secondRow = Row(text, 0, 0, 8);
            Assert.Greater(firstRow.Count(t => t != null), 3); Assert.Greater(secondRow.Count(t => t != null), 3);
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.FlamingHands), text.GetTile(new Vector3Int(3, 2, 0)));
            ability.CooldownRemaining = 999;
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            CollectionAssert.AreEqual(firstRow, Row(text, 0, 1, 8)); CollectionAssert.AreEqual(secondRow, Row(text, 0, 0, 8));
        }

        [Test]
        public void EqualFrameDoesZeroHotbarWorkAndChangedPowerRepaintsOnlyItsCard()
        {
            var player = Player("Same name", "Pyromancy_EmberSpit", out var ability);
            var renderer = Renderer(out var text, out var bg, out var camera);
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            var sentinel = new Vector3Int(17, 4, 0); text.SetColor(sentinel, Color.magenta); bg.SetColor(new Vector3Int(16, 0, 0), Color.green);
            Frame(renderer, HotbarStateBuilder.Build(player, 0, null), camera);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            ability.SourcePowerClass = "Hydromancy_JetBlast";
            Frame(renderer, HotbarStateBuilder.Build(player, 0, null), camera);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(Color.magenta, text.GetColor(sentinel)); Assert.AreEqual(Color.green, bg.GetColor(new Vector3Int(16, 0, 0)));
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.JetBlast), text.GetTile(new Vector3Int(3, 2, 0)));
        }

        [Test]
        public void SidebarMergesOnlySameTickDuplicatesAndNeverEditsRawHistory()
        {
            int tick = 3; MessageLog.TickProvider = () => tick;
            MessageLog.AddAnnouncement("Exact\nparagraph"); MessageLog.AddAnnouncement("Exact\nparagraph"); tick = 4;
            MessageLog.AddAnnouncement("Exact\nparagraph"); var raw = MessageLog.GetRecentEntries(9);
            var snap = SidebarStateBuilder.Build(null, null, null);
            Assert.AreEqual(2, snap.LogEntriesNewestFirst.Count); Assert.AreEqual(1, snap.LogEntriesNewestFirst[0].Count);
            Assert.AreEqual(2, snap.LogEntriesNewestFirst[1].Count); Assert.AreEqual(3, snap.LogEntriesNewestFirst[1].Tick);
            var lines = SidebarTextFormatter.FormatLog(snap.LogEntriesNewestFirst, 31);
            Assert.AreEqual(1, lines.Count(l => l.IsGroupSeparator));
            CollectionAssert.AreEqual(raw.Select(e => e.Text), MessageLog.GetRecentEntries(9).Select(e => e.Text));
            CollectionAssert.AreEqual(raw.Select(e => e.Serial), MessageLog.GetRecentEntries(9).Select(e => e.Serial));
            Assert.AreEqual("Exact\nparagraph", MessageLog.ConsumeAnnouncement());
        }

        [Test]
        public void SameTickDifferentBodiesRemainOneChronologicalGroup()
        {
            var entries = new[] { new SidebarLogEntry("newer", 7, 1, 2), new SidebarLogEntry("older", 7, 1, 1) };
            var lines = SidebarTextFormatter.FormatLog(entries, 31);
            Assert.AreEqual(0, lines.Count(l => l.IsGroupSeparator));
            StringAssert.Contains("older", lines[0].Text); StringAssert.Contains("newer", lines.Last().Text);
        }

        [Test]
        public void AllSixIconsAreCachedDifferentPixelsAndUnknownIsBoundedFallback()
        {
            var hashes = new HashSet<string>();
            foreach (SkillHudIcon icon in Enum.GetValues(typeof(SkillHudIcon)))
            {
                var tile = HudSkillIconAtlas.GetTile(icon); Assert.AreSame(tile, HudSkillIconAtlas.GetTile(icon));
                Assert.AreEqual(FilterMode.Point, tile.sprite.texture.filterMode);
                var r = tile.sprite.rect; var pixels = tile.sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                Assert.Greater(pixels.Count(p => p.a > .5f), 10);
                Assert.IsTrue(hashes.Add(string.Join("|", pixels.Select(p => ((Color32)p).ToString()))), icon + " must have authored distinct pixels.");
            }
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), HudSkillIconAtlas.GetTile((SkillHudIcon)999));
        }

        [Test]
        public void HudFontIsScopedCachedAndHasReadableUnknownFallback()
        {
            var old = CP437TilesetGenerator.GetTextTile('A'); var tile = HudPixelFont.GetTile('A');
            Assert.AreNotSame(old, tile); Assert.AreSame(old, CP437TilesetGenerator.GetTextTile('A'));
            Assert.AreSame(tile, HudPixelFont.GetTile('A'));
            Assert.AreSame(HudPixelFont.GetTile('?'), HudPixelFont.GetTile('\u2603'));
            var globalRule = CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.BoxHorizontal);
            var hudRule = HudPixelFont.GetTile(CP437TilesetGenerator.BoxHorizontal);
            Assert.AreNotSame(globalRule, hudRule); Assert.AreSame(hudRule, HudPixelFont.GetTile(CP437TilesetGenerator.BoxHorizontal));
            Assert.AreSame(globalRule, CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.BoxHorizontal));
            var rect = hudRule.sprite.rect;
            Assert.IsTrue(hudRule.sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height).Any(pixel => pixel.a > .5f));
            Assert.AreEqual(tile.transform.m00, tile.transform.m11, "Pixel glyphs must not stretch in one axis.");
        }

        static Entity Player(string name, string source, out ActivatedAbility ability)
        {
            var p = new Entity(); var part = new ActivatedAbilitiesPart(); p.AddPart(part);
            var id = part.AddAbility(name, "CommandTest", "Spell", AbilityTargetingMode.DirectionLine, 4, source);
            ability = part.GetAbility(id); return p;
        }
        GameplayHotbarRenderer Renderer(out Tilemap text, out Tilemap background, out Camera camera)
        {
            var grid = Go("Hud polish grid"); grid.AddComponent<Grid>().cellSize = new Vector3(.5f, 1, 0);
            text = Go("text").AddComponent<Tilemap>(); text.transform.SetParent(grid.transform);
            background = Go("background").AddComponent<Tilemap>(); background.transform.SetParent(grid.transform);
            camera = Go("camera").AddComponent<Camera>(); camera.orthographic = true; camera.aspect = 8;
            return new GameplayHotbarRenderer(text, background);
        }
        GameObject Go(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        static TileBase[] Row(Tilemap map, int x, int y, int n) => Enumerable.Range(x, n).Select(i => map.GetTile(new Vector3Int(i, y, 0))).ToArray();
        static void Frame(GameplayHotbarRenderer renderer, HotbarSnapshot snapshot, Camera camera)
        { PerformanceDiagnostics.BeginFrame(0, false, false); renderer.Render(snapshot, camera); PerformanceDiagnostics.EndFrame(0); }
    }
}
