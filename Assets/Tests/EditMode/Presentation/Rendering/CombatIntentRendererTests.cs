using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class CombatIntentRendererTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly CombatIntentFixture Core = new CombatIntentFixture();
            public readonly GameObject Root = new GameObject("owned-combat-intent-test");
            public readonly InputHandler Input;
            public object Renderer;
            public Fixture()
            {
                Input = Root.AddComponent<InputHandler>(); Input.WorldActionMenuUI = Root.AddComponent<WorldActionMenuUI>();
                Input.PlayerEntity = Core.Player; Input.CurrentZone = Core.Zone;
                Input.TurnManager = (TurnManager)Activator.CreateInstance(typeof(TurnManager), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { false }, null);
                typeof(TurnManager).GetProperty("CurrentActor").SetValue(Input.TurnManager, Core.Player);
                typeof(TurnManager).GetProperty("WaitingForInput").SetValue(Input.TurnManager, true);
            }
            public void Create()
            {
                var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.CombatIntentRenderer");
                Assert.NotNull(type, "Missing persistent local combat cue renderer.");
                Root.AddComponent<Grid>(); var tiles = new GameObject("owned combat cue tiles"); tiles.transform.SetParent(Root.transform, false);
                Renderer = Activator.CreateInstance(type, new object[] { Root.transform, tiles.AddComponent<Tilemap>(), 0 });
            }
            public void Refresh(bool allowed = true) => Renderer.GetType().GetMethod("Refresh").Invoke(Renderer, new object[] { Core.Player, Core.Zone, allowed });
            public int Count => (int)Renderer.GetType().GetProperty("VisibleCount").GetValue(Renderer);
            public bool Allowed(Zone zone = null) { var m = typeof(InputHandler).GetMethod("CanShowCombatIntent"); Assert.NotNull(m, "Missing actual world-input visibility gate."); return (bool)m.Invoke(Input, new object[] { zone ?? Core.Zone, Core.Player }); }
            public void State(string state) { var f = typeof(InputHandler).GetField("_inputState", BindingFlags.Instance | BindingFlags.NonPublic); f.SetValue(Input, Enum.Parse(f.FieldType, state)); }
            public void Dispose() { (Renderer as IDisposable)?.Dispose(); UnityEngine.Object.DestroyImmediate(Root); Core.Dispose(); }
        }
        [Test] public void TwoVisibleAmberChevronsPersistWithoutAllocatingMoreObjectsAndClearOnRecovery()
        {
            using (var f = new Fixture())
            {
                f.Create(); f.Refresh(); Assert.Zero(f.Count); f.Core.Begin(); f.Refresh(); Assert.AreEqual(2, f.Count);
                var all = f.Root.GetComponentsInChildren<LineRenderer>(true); int objects = f.Root.GetComponentsInChildren<Transform>(true).Length;
                var shown = all.Where(line => line.enabled && line.gameObject.activeInHierarchy).ToArray();
                Assert.AreEqual(2, shown.Length); Assert.True(shown.All(line => line.positionCount == 3 && !line.loop && line.widthMultiplier <= .12f));
                Assert.True(shown.All(line => line.startColor.r > line.startColor.b && line.startColor.g > line.startColor.b));
                Assert.IsEmpty(f.Root.GetComponentsInChildren<Collider>(true));
                for (int i = 0; i < 30; i++) f.Refresh();
                Assert.AreEqual(objects, f.Root.GetComponentsInChildren<Transform>(true).Length);
                Assert.AreEqual(2, f.Count); f.Core.Turn(); f.Refresh(); Assert.Zero(f.Count);
            }
        }
        [TestCase("owner-fog")][TestCase("owner-hidden")][TestCase("ray-fog")][TestCase("ray-unexplored")]
        [TestCase("disabled")][TestCase("dead-player")][TestCase("dead-owner")]
        public void CuesClearWhenSourceRayOrCurrentPlayBecomesUnavailable(string reason)
        {
            using (var f = new Fixture())
            {
                f.Create(); f.Core.Begin(); f.Refresh(); Assert.AreEqual(2, f.Count);
                if (reason == "owner-fog") f.Core.Zone.GetEntityCell(f.Core.Actor).IsVisible = false;
                if (reason == "owner-hidden") f.Core.Actor.GetPart<RenderPart>().Visible = false;
                if (reason == "ray-fog") f.Core.Zone.GetCell(11, 10).IsVisible = false;
                if (reason == "ray-unexplored") f.Core.Zone.GetCell(11, 10).Explored = false;
                if (reason == "dead-player") f.Core.Player.GetStat("Hitpoints").BaseValue = 0;
                if (reason == "dead-owner") f.Core.Actor.GetStat("Hitpoints").BaseValue = 0;
                f.Refresh(reason != "disabled"); Assert.Zero(f.Count, "Never draw a ray beyond a hidden intervening cell.");
            }
        }
        [Test] public void DisposeDestroysOwnedMaterialAndCueChildrenButKeepsOtherPresentation()
        {
            using (var f = new Fixture())
            {
                var unrelated = new GameObject("unrelated"); unrelated.transform.SetParent(f.Root.transform, false);
                f.Create(); f.Core.Begin(); f.Refresh(); var lines = f.Root.GetComponentsInChildren<LineRenderer>(true);
                var material = lines[0].sharedMaterial; ((IDisposable)f.Renderer).Dispose(); f.Renderer = null;
                Assert.True(material == null); Assert.True(lines.All(line => line == null)); Assert.True(unrelated != null);
            }
        }
        [Test] public void NormalAndLookAllowIntentEvenWithoutAdjacentWorldAffordance()
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Allowed()); f.State("LookMode"); Assert.True(f.Allowed());
                Assert.False(f.Allowed(new Zone(f.Core.Zone.ZoneID)));
            }
        }
        [TestCase("InventoryOpen")][TestCase("WorldActionMenuOpen")][TestCase("AwaitingTalkDirection")]
        [TestCase("WaitingForFxResolution")][TestCase("AnnouncementOpen")]
        public void ActualModalInputStatesHideIntent(string state)
        { using (var f = new Fixture()) { Assert.True(f.Allowed()); f.State(state); Assert.False(f.Allowed()); } }
        [TestCase("disabled")][TestCase("not-turn")][TestCase("foreign-actor")]
        public void ActualTurnAndEnabledInputAreRequired(string reason)
        {
            using (var f = new Fixture())
            {
                Assert.True(f.Allowed());
                if (reason == "disabled") f.Input.enabled = false;
                if (reason == "not-turn") typeof(TurnManager).GetProperty("WaitingForInput").SetValue(f.Input.TurnManager, false);
                if (reason == "foreign-actor") typeof(TurnManager).GetProperty("CurrentActor").SetValue(f.Input.TurnManager, f.Core.Actor);
                Assert.False(f.Allowed());
            }
        }
        [Test] public void ZoneRendererActuallyRefreshesAndClearsItsOwnedCueOnMenuPauseAndZoneChange()
        {
            using (var f = new Fixture())
            {
                f.Create(); var zoneRenderer = f.Root.AddComponent<ZoneRenderer>(); f.Input.ZoneRenderer = zoneRenderer;
                zoneRenderer.PlayerEntity = f.Core.Player; typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(zoneRenderer, f.Core.Zone);
                var field = typeof(ZoneRenderer).GetField("_combatIntentRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field, "ZoneRenderer must own the actual cue lifecycle.");
                (field.GetValue(zoneRenderer) as IDisposable)?.Dispose(); field.SetValue(zoneRenderer, f.Renderer);
                zoneRenderer.SetAffordanceInput(f.Input);
                var refresh = typeof(ZoneRenderer).GetMethod("RefreshCombatIntent", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(refresh); f.Core.Begin(); refresh.Invoke(zoneRenderer, null); Assert.AreEqual(2, f.Count);
                f.State("InventoryOpen"); refresh.Invoke(zoneRenderer, null); Assert.Zero(f.Count);
                f.State("Normal"); refresh.Invoke(zoneRenderer, null); Assert.AreEqual(2, f.Count);
                zoneRenderer.Paused = true; refresh.Invoke(zoneRenderer, null); Assert.Zero(f.Count);
                zoneRenderer.Paused = false; refresh.Invoke(zoneRenderer, null); Assert.AreEqual(2, f.Count);
                zoneRenderer.SetZone(new Zone("other")); Assert.Zero(f.Count);
                field.SetValue(zoneRenderer, null);
            }
        }
    }
}
