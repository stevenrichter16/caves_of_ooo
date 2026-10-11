using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class HudPolishAdversarialTests
    {
        [TestCase(null)] [TestCase("")] [TestCase("   ")]
        [TestCase("UninterruptiblyLongSingleName")]
        [TestCase("One two three four five six")]
        [TestCase("A\nB\tC")]
        [TestCase("A    B")]
        [TestCase("\u2603 \u263a \u2192")]
        public void UntrustedNamesHaveBoundedTwoRowsAndExplicitOverflow(string name)
        {
            HudHotbarPresentation.NameLines(name, out var a, out var b);
            Assert.LessOrEqual(a.Length, 8); Assert.LessOrEqual(b.Length, 8);
            StringAssert.DoesNotContain("\n", a+b); StringAssert.DoesNotContain("\t", a+b);
            if (name == "UninterruptiblyLongSingleName" || name == "One two three four five six")
                Assert.IsTrue(b.EndsWith(">") || a.EndsWith(">"), "Lost suffix must be disclosed.");
            if (string.IsNullOrWhiteSpace(name)) Assert.AreEqual("", a+b);
        }

        [TestCase(-5, "READY")] [TestCase(0, "READY")] [TestCase(1, "CD1")]
        [TestCase(999, "CD999")] [TestCase(1000, "CD999+")] [TestCase(int.MaxValue, "CD999+")]
        public void CooldownLabelNeverWrapsIntoNames(int cooldown, string label)
        {
            var slot = new HotbarSlotSnapshot(0, '1', "x", "x", "", "", 'X', cooldown, true, false, false, cooldown <= 0);
            Assert.AreEqual(label, HudHotbarPresentation.StateLabel(slot)); Assert.LessOrEqual(label.Length, 6);
        }

        [TestCase(AbilityTargetingMode.AdjacentCell, "adjacent")]
        [TestCase(AbilityTargetingMode.DirectionLine, "line 4")]
        [TestCase(AbilityTargetingMode.SelfCentered, "self")]
        public void PendingDescriptionUsesRealInputTargeting(AbilityTargetingMode mode, string expected)
        {
            var player = new Entity(); var part = new ActivatedAbilitiesPart(); player.AddPart(part);
            var id = part.AddAbility("Unknown Power", "CommandUnknown", "Skill", mode, 4, "unknown");
            var ability = part.GetAbility(id); var snap = HotbarStateBuilder.Build(player, 0, ability);
            StringAssert.Contains(expected, snap.SummaryText);
            StringAssert.DoesNotContain("choose a direction", snap.SummaryText);
            Assert.AreEqual(mode, snap.Slots[0].TargetingMode); Assert.AreEqual(4, snap.Slots[0].Range);
        }

        [TestCase(-1)] [TestCase(100)]
        public void InvalidSelectionDoesNotClaimAnotherSkill(int selected)
        {
            var player = new Entity(); var part = new ActivatedAbilitiesPart(); player.AddPart(part); part.AddAbility("Real", "Real", "Skill");
            var snapshot = HotbarStateBuilder.Build(player, selected, null);
            StringAssert.Contains("No rite bound", snapshot.SummaryText);
            Assert.IsFalse(snapshot.Slots.Any(s => s.Selected));
        }

        [Test]
        public void EqualTickDifferentSerialsAndParagraphsKeepChronologicalBodies()
        {
            var entries = new[] { new SidebarLogEntry("new\nparagraph", 9, 1, 8), new SidebarLogEntry("middle", 8, 1, 7), new SidebarLogEntry("old", 9, 1, 6) };
            var lines = SidebarTextFormatter.FormatLog(entries, 12);
            Assert.AreEqual(2, lines.Count(l => l.IsGroupSeparator), "Groups are contiguous, never sorted by tick.");
            var bodies = string.Join(" ", lines.Where(l => !l.IsGroupSeparator).Select(l => l.Text));
            Assert.Less(bodies.IndexOf("old", StringComparison.Ordinal), bodies.IndexOf("middle", StringComparison.Ordinal));
            Assert.Less(bodies.IndexOf("middle", StringComparison.Ordinal), bodies.IndexOf("new", StringComparison.Ordinal));
            StringAssert.Contains("paragraph", bodies);
            foreach (var separator in lines.Where(l => l.IsGroupSeparator))
                Assert.IsFalse(lines.Any(l => !l.IsGroupSeparator && separator.MatchesIdentity(l)), "A separator is never a paragraph anchor.");
        }

        [TestCase(false)] [TestCase(true)]
        public void MiddleEntryMutationRebuildsActualVisibleLogCache(bool changeTickOnly)
        {
            var root = new GameObject("sidebar cache test"); var go = new GameObject("text"); var bg = new GameObject("bg"); var c = new GameObject("camera");
            try
            {
                root.AddComponent<Grid>().cellSize = new Vector3(.5f, 1, 0);
                go.transform.SetParent(root.transform); bg.transform.SetParent(root.transform);
                var text = go.AddComponent<Tilemap>(); var background = bg.AddComponent<Tilemap>();
                var camera = c.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 20; camera.aspect = .425f;
                var renderer = new GameplaySidebarRenderer(text, background, root.transform, 20);
                var entries = new List<SidebarLogEntry> { new SidebarLogEntry("new", 2, 1, 3), new SidebarLogEntry("before", 2, 1, 2), new SidebarLogEntry("old", 2, 1, 1) };
                var snapshot = new SidebarSnapshot(Array.Empty<string>(), "-", null, entries);
                renderer.Render(snapshot, camera, 34, false, 0);
                entries[1] = new SidebarLogEntry(changeTickOnly ? "before" : "after", changeTickOnly ? 1 : 2, 1, 2);
                renderer.Render(snapshot, camera, 34, false, 0);
                var retained = (List<SidebarTextFormatter.LogLine>)typeof(GameplaySidebarRenderer)
                    .GetField("_cachedLogLines", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(renderer);
                if (changeTickOnly) Assert.AreEqual(2, retained.Count(l => l.IsGroupSeparator));
                else { Assert.IsTrue(retained.Any(l => l.Text.Contains("after"))); Assert.IsFalse(retained.Any(l => l.Text.Contains("before"))); }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(c); }
        }

        [Test]
        public void ManyUnicodeMissesDoNotRecreateScopedAtlas()
        {
            var fallback = HudPixelFont.GetTile('?'); var texture = fallback.sprite.texture;
            for (int i = 300; i < 400; i++) Assert.AreSame(fallback, HudPixelFont.GetTile((char)i));
            Assert.AreSame(texture, HudPixelFont.GetTile('A').sprite.texture);
        }

        [Test]
        public void CachedRecentEntriesCannotBeEditedThroughTheirPublicCollectionView()
        {
            MessageLog.Clear();
            try
            {
                MessageLog.Add("retained source");
                var entries = SidebarStateBuilder.Build(null, null, null).LogEntriesNewestFirst;
                var mutable = entries as IList<SidebarLogEntry>;
                Assert.IsNotNull(mutable); Assert.IsTrue(mutable.IsReadOnly);
                Assert.Throws<NotSupportedException>(() => mutable[0] = new SidebarLogEntry("forged", 0, 1));
                Assert.AreEqual("retained source", SidebarStateBuilder.Build(null, null, null).LogEntriesNewestFirst[0].Text);
            }
            finally { MessageLog.Clear(); }
        }

        [Test]
        public void RecentLimitAndClearAtSameCountInvalidateRevisionCache()
        {
            var oldTick = MessageLog.TickProvider;
            try
            {
                MessageLog.Clear(); MessageLog.TickProvider = () => 5;
                MessageLog.Add("one"); MessageLog.Add("two"); MessageLog.Add("three");
                Assert.AreEqual(1, SidebarStateBuilder.Build(null, null, null, 1).LogEntriesNewestFirst.Count);
                Assert.AreEqual(3, SidebarStateBuilder.Build(null, null, null, 3).LogEntriesNewestFirst.Count);
                MessageLog.Clear(); MessageLog.Add("new one"); MessageLog.Add("new two"); MessageLog.Add("new three");
                Assert.AreEqual("new three", SidebarStateBuilder.Build(null, null, null, 3).LogEntriesNewestFirst[0].Text);
                Assert.AreEqual(0, SidebarStateBuilder.Build(null, null, null, 0).LogEntriesNewestFirst.Count);
            }
            finally { MessageLog.Clear(); MessageLog.TickProvider = oldTick; }
        }

        [Test]
        public void SevenRowBudgetDisclosesStatusOverflowAndKeepsEveryVital()
        {
            var source = new[] { "HP 9/10 | MP 3", "LV 2 | XP 1/200", "AV 3 | DV 6", "WT 20/150 | DR 9", "TIME Dawn", "HAUL -10 Sp branch" };
            var status = "poisoned, wet, chilled, tangled, inspired, dazed, soaked";
            var snapshot = new SidebarSnapshot(source, status, null, null);
            var shown = SidebarTextFormatter.FormatVitals(snapshot, 31, 7);
            foreach (var line in source) CollectionAssert.Contains(shown, line);
            Assert.AreEqual(7, shown.Count); Assert.IsTrue(shown.Last().EndsWith(">"));
            Assert.AreEqual(status, snapshot.StatusText, "Overflow changes presentation only.");
        }

        [Test]
        public void AllExistingVitalLinesRemainAvailableAfterPresentation()
        {
            var source = new[] { "HP 9/10 | MP 3", "LV 2 | XP 1/200", "AV 3 | DV 6", "WT 20/150 | DR 9", "TIME Dawn", "HAUL -10 Sp branch" };
            var snapshot = new SidebarSnapshot(source, "poisoned, wet", null, null);
            var shown = SidebarTextFormatter.FormatVitals(snapshot, 31, 9);
            foreach (var line in source) CollectionAssert.Contains(shown, line);
            StringAssert.Contains("poisoned, wet", string.Join(" ", shown));
            CollectionAssert.AreEqual(source, snapshot.VitalLines);
        }
    }
}
