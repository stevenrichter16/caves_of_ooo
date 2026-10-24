using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Real menu transaction and renderer LateUpdate in an owned, quiet graph.
    // This is not a keyboard-delivery or ordinary-play acceptance test.
    public sealed class LookFocusRefreshTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic;

        [TestCase(false)] [TestCase(true)]
        public void RealHarvestReplacesRemovedCropFocusAndRefusalDoesNot(bool refuse)
        {
            using (var f = new Fixture("PitchpodCrop"))
            {
                var crop = f.Target.GetPart<CropPart>();
                Assert.AreEqual(2, crop.GrowthStage);
                var before = f.Snapshot;
                Assert.AreSame(f.Target, before.PrimaryEntity);
                Assert.True(ContainsText(f.Sidebar, before.Header), "Initial submitted FOCUS is a real crop.");
                int tick = f.Turns.TickCount, energy = f.Turns.GetEnergy(f.Player);
                f.Harvest(refuse);
                f.Frame(); // No cursor movement, direct snapshot refresh, or test dirty call.
                Assert.AreEqual("LookMode", Get(f.Input, "_inputState").ToString());
                Assert.AreEqual((11, 10), (f.Cursor.X, f.Cursor.Y));
                if (refuse)
                {
                    Assert.AreSame(f.Target, f.Snapshot.PrimaryEntity);
                    Assert.AreSame(before, f.Snapshot, "Rejected transaction did not redraw the focused world cell.");
                    Assert.AreSame(f.Cell, f.Zone.GetEntityCell(f.Target));
                    Assert.AreEqual(tick, f.Turns.TickCount);
                    Assert.AreEqual(energy, f.Turns.GetEnergy(f.Player));
                    Assert.AreEqual(0, f.Cell.Objects.Count(e => e.BlueprintName == "PitchpodResin"));
                }
                else
                {
                    Assert.IsNull(f.Zone.GetEntityCell(f.Target));
                    Assert.AreEqual(2, f.Cell.Objects.Count(e => e.BlueprintName == "PitchpodResin"));
                    Assert.AreEqual(1, f.Cell.Objects.Count(e => e.BlueprintName == "PitchpodSeed"));
                    Assert.NotNull(f.Snapshot);
                    Assert.AreNotSame(before, f.Snapshot);
                    Assert.AreNotSame(f.Target, f.Snapshot.PrimaryEntity);
                    Assert.Contains(f.Snapshot.PrimaryEntity, f.Cell.Objects);
                    var resinName = f.Cell.Objects.First(e => e.BlueprintName == "PitchpodResin").GetDisplayName();
                    Assert.True(f.Snapshot.DetailLines.Any(s => s.Contains(resinName)));
                    Assert.True(ContainsText(f.Sidebar, f.Snapshot.Header), "Already-submitted sidebar must use the current output.");
                    Assert.AreEqual(tick + 10, f.Turns.TickCount);
                    Assert.AreEqual(energy - TurnManager.ActionThreshold + 10 * f.Player.GetStatValue("Speed", 100), f.Turns.GetEnergy(f.Player));
                }
                var settled = f.Snapshot;
                f.Frame();
                Assert.AreSame(settled, f.Snapshot);
            }
        }

        [TestCase("cell")] [TestCase("full")] [TestCase("unrelated")]
        public void DirtyScopeRefreshesOnlyRelevantFocusAndSubmittedHeader(string dirty)
        {
            using (var f = new Fixture())
            {
                var before = f.Snapshot;
                f.Target.GetPart<RenderPart>().DisplayName = "AFTER";
                if (dirty == "full") f.Renderer.MarkDirty("LookFixture.Full");
                else f.Renderer.MarkCellDirty(dirty == "cell" ? 11 : 14, 10, "LookFixture.Cell");
                f.Frame();
                if (dirty == "unrelated")
                {
                    Assert.AreSame(before, f.Snapshot);
                    Assert.True(ContainsText(f.Sidebar, before.Header));
                    f.Renderer.MarkCellDirty(11, 10, "LookFixture.Focused");
                    f.Frame();
                }
                Assert.AreNotSame(before, f.Snapshot);
                Assert.AreEqual("[11,10] AFTER", f.Snapshot.Header);
                Assert.True(ContainsText(f.Sidebar, "[11,10] AFTER"));
                Assert.False(ContainsText(f.Sidebar, before.Header));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void HiddenOrUnexploredFocusIsClearedAndRebuiltOnlyAfterDisclosure(bool unexplored)
        {
            using (var f = new Fixture())
            {
                f.Target.GetPart<RenderPart>().DisplayName = "PRIVATE";
                if (unexplored) f.Cell.Explored = false; else f.Cell.IsVisible = false;
                f.Renderer.MarkCellDirty(11, 10, "LookFixture.Fog");
                f.Frame();
                Assert.IsNull(f.Snapshot, "Do not query hidden live cell contents into FOCUS.");
                Assert.False(ContainsText(f.Sidebar, "PRIVATE"));
                f.Cell.Explored = f.Cell.IsVisible = true;
                f.Renderer.MarkCellDirty(11, 10, "LookFixture.Reveal");
                f.Frame();
                Assert.AreSame(f.Target, f.Snapshot.PrimaryEntity);
                Assert.True(ContainsText(f.Sidebar, "[11,10] PRIVATE"));
            }
        }

        [Test]
        public void FullRedrawRefreshUsesNewFovRatherThanPreviousVisibility()
        {
            using (var f = new Fixture())
            {
                f.Renderer.FovRadius = 2;
                Assert.True(f.Zone.MoveEntity(f.Player, 30, 10));
                Assert.True(f.Cell.IsVisible, "Pre-render visibility is deliberately stale.");
                f.Target.GetPart<RenderPart>().DisplayName = "PRIVATE";
                f.Renderer.MarkDirty("LookFixture.FovMove");
                f.Frame();
                Assert.False(f.Cell.IsVisible);
                Assert.IsNull(f.Snapshot);
                Assert.False(ContainsText(f.Sidebar, "PRIVATE"));
                Assert.True(f.Zone.MoveEntity(f.Player, 10, 10));
                f.Renderer.MarkDirty("LookFixture.FovReturn");
                f.Frame();
                Assert.True(f.Cell.IsVisible);
                Assert.AreSame(f.Target, f.Snapshot.PrimaryEntity);
                Assert.True(ContainsText(f.Sidebar, "[11,10] PRIVATE"));
            }
        }

        [Test]
        public void SameIdZoneReplacementRejectsReboundOldCursorAndSnapshot()
        {
            using (var f = new Fixture())
            {
                var old = f.Snapshot;
                var replacement = new Zone(f.Zone.ZoneID);
                f.Renderer.SetZone(replacement);
                // Delayed UI callback carrying the old graph must not regain authority.
                f.Renderer.SetWorldCursorState(f.Cursor, f.Player);
                f.Renderer.SetLookSnapshot(old);
                f.Frame();
                Assert.IsNull(f.Snapshot);
                Assert.False(ContainsText(f.Sidebar, old.Header));
            }
        }

        [Test]
        public void CanonicalCellReplacementInvalidatesOldSnapshotWithoutCursorMovement()
        {
            using (var f = new Fixture())
            {
                var old = f.Snapshot;
                var current = new Cell(11, 10, f.Zone) { Explored = true, IsVisible = true };
                f.Zone.Cells[11, 10] = current;
                f.Renderer.MarkCellDirty(11, 10, "LookFixture.CellReplacement");
                f.Frame();
                Assert.AreSame(current, f.Snapshot.Cell);
                Assert.IsNull(f.Snapshot.PrimaryEntity);
                Assert.AreEqual("[11,10] empty ground", f.Snapshot.Header);
                Assert.False(ContainsText(f.Sidebar, old.Header));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void InactiveOrOtherModeCursorCannotKeepLookFocus(bool otherMode)
        {
            using (var f = new Fixture())
            {
                var old = f.Snapshot;
                if (otherMode) f.Cursor.Activate(WorldCursorMode.ThrowTarget, f.Zone, 11, 10, 10, 10);
                else f.Cursor.Deactivate();
                f.Frame();
                Assert.IsNull(f.Snapshot);
                Assert.False(ContainsText(f.Sidebar, old.Header));
            }
        }

        [Test]
        public void FullscreenPauseDefersQueryAndResumeRefreshesTheSameLookCell()
        {
            using (var f = new Fixture())
            {
                var before = f.Snapshot;
                f.Target.GetPart<RenderPart>().DisplayName = "AFTER";
                f.Renderer.MarkCellDirty(11, 10, "LookFixture.Pending");
                f.Renderer.Paused = true;
                f.Frame();
                Assert.AreSame(before, f.Snapshot, "Fullscreen UI owns the canvas; no live Look query is needed.");
                f.Renderer.Paused = false;
                f.Frame();
                Assert.AreNotSame(before, f.Snapshot);
                Assert.True(ContainsText(f.Sidebar, "[11,10] AFTER"));
            }
        }

        [Test]
        public void QuietFramesKeepSnapshotIdentityAndDoNotAdvanceSimulation()
        {
            using (var f = new Fixture())
            {
                var before = f.Snapshot;
                int tick = f.Turns.TickCount;
                for (int n = 0; n < 20; n++) f.Frame();
                Assert.AreSame(before, f.Snapshot, "Do not allocate/rebuild a Look snapshot every idle frame.");
                Assert.AreEqual(tick, f.Turns.TickCount);
                Assert.False((bool)Get(f.Renderer, "_fullDirty"));
                Assert.AreEqual(0, ((HashSet<int>)Get(f.Renderer, "_dirtyCells")).Count);
            }
        }

        [Test]
        public void LegacyExplicitSnapshotWithoutWorldCursorStillRenders()
        {
            using (var f = new Fixture())
            {
                var before = f.Snapshot;
                f.Renderer.ClearWorldCursor();
                f.Renderer.SetLookSnapshot(before);
                f.Renderer.MarkDirty("LookFixture.Legacy");
                f.Frame();
                Assert.AreSame(before, f.Snapshot);
                Assert.True(ContainsText(f.Sidebar, before.Header));
            }
        }

        static object Get(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, I).SetValue(owner, value);
        static object Call(object owner, string name, params object[] args)
        {
            try { return owner.GetType().GetMethod(name, I).Invoke(owner, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        static bool ContainsText(Tilemap tilemap, string text)
        {
            foreach (var start in tilemap.cellBounds.allPositionsWithin)
            {
                bool matches = true;
                for (int i = 0; i < text.Length; i++)
                    if (text[i] != ' ' && tilemap.GetTile(start + new Vector3Int(i, 0, 0)) != HudPixelFont.GetTile(text[i])) { matches = false; break; }
                if (matches) return true;
            }
            return false;
        }

        sealed class Fixture : IDisposable
        {
            readonly UnityEngine.Random.State random = UnityEngine.Random.state;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound;
            readonly Zone previous;
            readonly Action<int, int> oldChanged;
            readonly Action oldSight;
            readonly HotbarSaveFixture globals;
            readonly HaulingContentScope content;
            readonly GameObject host;
            readonly List<(FieldInfo field, object value)> metrics = new List<(FieldInfo, object)>();
            readonly PerformanceFrameSnapshot oldCurrent, oldCompleted;
            public readonly Zone Zone;
            public readonly Entity Player, Target;
            public readonly TurnManager Turns;
            public readonly ZoneRenderer Renderer;
            public readonly Tilemap Sidebar;
            public InputHandler Input => globals.Input;
            public WorldCursorState Cursor => (WorldCursorState)Get(Input, "_worldCursorState");
            public Cell Cell => Zone.GetCell(11, 10);
            public LookSnapshot Snapshot => (LookSnapshot)Get(Renderer, "_currentLookSnapshot");
            public Fixture(string blueprint = "HealingTonic")
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out var prior))
                { previous = prior; oldChanged = prior.TileState.OnCellChanged; oldSight = prior.TileState.OnSightChanged; }
                foreach (var field in typeof(PerformanceDiagnostics).GetFields(S | BindingFlags.Public))
                    if (!field.IsInitOnly && !field.IsLiteral) metrics.Add((field, field.GetValue(null)));
                oldCurrent = new PerformanceFrameSnapshot((PerformanceFrameSnapshot)typeof(PerformanceDiagnostics).GetField("CurrentFrame", S).GetValue(null));
                oldCompleted = new PerformanceFrameSnapshot((PerformanceFrameSnapshot)typeof(PerformanceDiagnostics).GetField("CompletedFrame", S).GetValue(null));
                try
                {
                    globals = new HotbarSaveFixture(true, false);
                    content = new HaulingContentScope(); content.Seed(64);
                    var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                    Zone = new Zone("Overworld.11.10.0"); manager.SetActiveZone(Zone);
                    Player = content.Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(Player, 10, 10));
                    var soil = content.Factory.CreateEntity("Grass"); soil.SetTag("Plantable"); soil.AddPart(new CultivatedSoilPart()); Assert.True(Zone.AddEntity(soil, 11, 10));
                    Target = content.Factory.CreateEntity(blueprint); Assert.NotNull(Target);
                    if (Target.GetPart<CropPart>() is CropPart crop) crop.GrowthStage = 2;
                    else Target.GetPart<RenderPart>().DisplayName = "BEFORE";
                    Assert.True(Zone.AddEntity(Target, 11, 10));
                    Turns = new TurnManager(); Turns.RestoreSavedState(17, true, Player, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                    globals.BindOld(GameSessionState.Capture("look-focus-test", "owned-graph", manager, Turns, Player));
                    TurnManager.World = null; CropSystem.Factory = content.Factory; SettlementRuntime.ActiveZone = Zone;
                    host = new GameObject("Owned Look focus renderer"); host.SetActive(false); host.AddComponent<Grid>();
                    var world = CreateTilemap(host, "World"); Renderer = world.gameObject.AddComponent<ZoneRenderer>(); Set(Renderer, "_tilemap", world);
                    var cameraObject = new GameObject("Owned Look camera", typeof(Camera)); cameraObject.transform.SetParent(host.transform, false);
                    var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 20; camera.aspect = 16f / 9f; camera.transform.position = new Vector3(20, 12, -10);
                    Set(Renderer, "_mainCamera", camera); Renderer.SetSidebarCamera(camera);
                    var sidebarGrid = new GameObject("Owned narrow sidebar", typeof(Grid)); sidebarGrid.transform.SetParent(host.transform, false);
                    Sidebar = CreateTilemap(sidebarGrid, "Text"); var bg = CreateTilemap(sidebarGrid, "Background");
                    Set(Renderer, "_sidebarTilemap", Sidebar); Set(Renderer, "_sidebarBgTilemap", bg);
                    Set(Renderer, "_sidebarRenderer", new GameplaySidebarRenderer(Sidebar, bg, sidebarGrid.transform, 20));
                    Renderer.PlayerEntity = Player; Renderer.SetZone(Zone); Input.ZoneRenderer = Renderer;
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Input.WorldActionMenuUI = globals.Root.AddComponent<WorldActionMenuUI>();
                    Frame(); Assert.True(Cell.Explored && Cell.IsVisible);
                    Assert.True((bool)Call(Input, "EnterLookMode")); Call(Input, "MoveLookCursor", 1, 0); Frame();
                    Assert.AreEqual((11, 10), (Cursor.X, Cursor.Y)); Assert.AreSame(Target, Snapshot.PrimaryEntity);
                    Assert.False((bool)Get(Renderer, "_fullDirty")); Assert.AreEqual(0, ((HashSet<int>)Get(Renderer, "_dirtyCells")).Count);
                }
                catch { Dispose(); throw; }
            }
            static Tilemap CreateTilemap(GameObject parent, string name)
            { var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); child.transform.SetParent(parent.transform, false); return child.GetComponent<Tilemap>(); }
            public void Frame() => Call(Renderer, "LateUpdate");
            public void Harvest(bool refuse)
            {
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", I); state.SetValue(Input, Enum.Parse(state.FieldType, "LookMode"));
                var menu = Input.WorldActionMenuUI;
                Call(Input, "OpenWorldActionMenuFor", Target, Cell, false);
                Assert.True(menu.IsOpen);
                var actions = (List<InventoryAction>)Get(menu, "_actions");
                int selected = actions.FindIndex(a => a.Command == "HarvestCultivatedCrop"); Assert.GreaterOrEqual(selected, 0);
                var action = actions[selected]; var target = menu.SelectedTarget; var cell = menu.SelectedCell; bool pile = menu.SelectedCellIsPile;
                if (refuse) Target.GetPart<CropPart>().GrowthStage = 1;
                Call(menu, "SelectAction", selected);
                Call(Input, "ExecuteWorldActionSelection", action, target, cell, pile);
            }
            public void Dispose()
            {
                try { if (host != null) Object.DestroyImmediate(host); }
                finally
                {
                    try { content?.Dispose(); }
                    finally
                    {
                        try { globals?.Dispose(); }
                        finally
                        {
                            bound.SetValue(null, oldBound);
                            if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                            foreach (var saved in metrics) saved.field.SetValue(null, saved.value);
                            var copy = typeof(PerformanceFrameSnapshot).GetMethod("CopyFrom", BindingFlags.Instance | BindingFlags.NonPublic);
                            copy.Invoke(typeof(PerformanceDiagnostics).GetField("CurrentFrame", S).GetValue(null), new object[] { oldCurrent });
                            copy.Invoke(typeof(PerformanceDiagnostics).GetField("CompletedFrame", S).GetValue(null), new object[] { oldCompleted });
                            UnityEngine.Random.state = random;
                        }
                    }
                }
            }
        }
    }
}
