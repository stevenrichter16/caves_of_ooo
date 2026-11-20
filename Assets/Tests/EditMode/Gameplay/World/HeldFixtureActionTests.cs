using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Set down, take up and hand over (Docs/HOME-BASE-IMPLEMENTATION.md, PB2a on spec S19). One entity moves between the pack and the ground; every step is in the native
    /// inventory transaction. Each positive case has its counter; refusals and forced failures restore the pack, cell, flags, tag and Parts exactly.
    /// </summary>
    public sealed class HeldFixtureActionTests : FiftyWorldFixture
    {
        const string Kind = "test-box";
        const string Definitions = "{\"Version\":1,\"Fixtures\":[{\"Kind\":\"test-box\",\"Class\":\"Carried\",\"Blueprint\":\"Chest\",\"Solid\":true,\"GroundClass\":\"OpenGround\",\"Rounds\":1}]}";
        OverworldZoneManager manager; string wildId; Zone wild;
        const int AX = 20, AY = 12;

        [SetUp] public void SetupHeld()
        {
            PackableFixtureRegistry.InitializeFromJson(Definitions); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = Ground(wildId); Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownHeld() { PackableFixtureRegistry.ResetForTests(); }

        Zone Ground(string id)
        {
            var z = new Zone(id);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(z.AddEntity(Factory.CreateEntity("Grass"), x, y));
            return z;
        }

        /// <summary>A carried kit: a real container made takeable, with the Part that hosts the verbs.</summary>
        Entity Kit(string kind = Kind, bool carry = true)
        {
            var kit = Factory.CreateEntity("Chest"); var p = kit.GetPart<PhysicsPart>(); p.Takeable = true; p.Solid = false; p.Weight = 10;
            kit.AddPart(new PackableFixturePart { Kind = kind }); if (carry) Assert.True(Pack.AddObject(kit)); return kit;
        }
        static string Cmd(Entity kit, string zoneId, int ox, int oy, int x, int y, string id = null)
            => "SetDown|" + Uri.EscapeDataString(zoneId) + "|" + ox + "|" + oy + "|" + x + "|" + y + "|" + (id ?? kit.ID);
        string SetDownRow(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        Entity SetDown(string direction = "east", Entity kit = null)
        {
            kit ??= Kit(); Assert.True(Act(kit, SetDownRow(kit, direction)), string.Join("|", MessageLog.GetRecent(3))); return kit;
        }
        string Tail(Entity fixture) => Uri.EscapeDataString(wildId) + "|" + fixture.ID;
        bool Take(Entity f) => Act(f, "TakeUp|" + Tail(f)); bool Hand(Entity f) => Act(f, "HandOver|" + Tail(f));
        string LastReason() => Diag.Snapshot(50).Last(r => r.Kind == "FixtureRefused").PayloadJson;
        int Held() => PlacementCheck.CountInZone(wild);

        // ── set down ──

        [Test] public void SetDownKeepsTheSameEntityIdAndContents()
        {
            var kit = Kit(); string id = kit.ID; var item = Factory.CreateEntity("Dagger"); Assert.True(kit.GetPart<ContainerPart>().AddItem(item));
            Assert.True(Act(kit, SetDownRow(kit)));
            Assert.AreSame(kit, wild.GetCell(AX + 1, AY).Objects.Single(e => e.ID == id)); Assert.False(Pack.Objects.Contains(kit)); Assert.AreEqual(id, kit.ID);
            CollectionAssert.Contains(kit.GetPart<ContainerPart>().Contents, item); Assert.AreEqual(1, wild.GetReadOnlyEntities().Count(e => e.ID == id));
        }

        [Test] public void SetDownFlipsFlagsTagAndAttachesTheMarkerAndInstallableState()
        {
            var kit = SetDown(); var p = kit.GetPart<PhysicsPart>();
            Assert.False(p.Takeable); Assert.True(p.Solid); Assert.True(kit.HasTag("Owned"));
            var marker = kit.GetPart<InstallationMarkerPart>(); Assert.AreEqual("Player", marker.Origin); Assert.True(marker.RetainZone); Assert.AreEqual("player:" + kit.ID, marker.InstallationId);
            var inst = kit.GetPart<InstallablePart>(); Assert.AreEqual(InstallablePart.Portable, inst.Mode); Assert.AreEqual(InstallablePart.Installed, inst.State);
            Assert.GreaterOrEqual(kit.GetPart<PackableFixturePart>().SetTick, 0); Assert.True(InstallationMarkerPart.Retains(kit)); Assert.AreEqual(1, Held());
        }

        [Test] public void EachLegalDirectionIsARowAndAnIllegalOneIsNot()
        {
            var kit = Kit(); Assert.AreEqual(8, Actions(kit).Count(a => a.Name == "SetDownFixture" && !a.IsDisabled));
            Assert.True(wild.AddEntity(Factory.CreateEntity("SandstoneWall"), AX + 2, AY + 1));   // a wall beside the east and south-east cells
            var rows = Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray(); Assert.Less(rows.Length, 8); Assert.Greater(rows.Length, 0);
        }

        [Test] public void WhenNothingIsLegalOneDisabledRowSaysWhyInsteadOfEight()
        {
            var kit = Kit(); wild.TileState.AddHeat(AX, AY, 1);
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) if (dx != 0 || dy != 0) wild.TileState.AddHeat(AX + dx, AY + dy, 1);
            var rows = Actions(kit).Where(a => a.Name == "SetDownFixture").ToArray();
            Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual(PlacementCheck.Ground, rows[0].DisabledCode); StringAssert.Contains("will not hold", rows[0].DisabledReason);
        }

        [Test] public void AVillageZoneOffersOnlyTheDisabledRowWithItsReason_AndTheWildZoneDoesNot()
        {
            var kit = Kit(); var village = Ground("Overworld.13.7.0"); Zone.RemoveEntity(Actor); Assert.True(village.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { village.ZoneID, village } }, village.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var row = Actions(kit).Single(a => a.Name == "SetDownFixture"); Assert.True(row.IsDisabled); Assert.AreEqual(PlacementCheck.PoiZone, row.DisabledCode);
        }

        [Test] public void AnItemWithoutThePartOrWithAnUnknownKindOffersNothing()
        {
            var plain = Carry("Dagger"); Assert.False(Actions(plain).Any(a => a.Name.Contains("Fixture")));
            var unknown = Kit("never-defined"); Assert.False(Actions(unknown).Any(a => a.Name == "SetDownFixture"));
            Assert.True(Actions(Kit()).Any(a => a.Name == "SetDownFixture"), "the counter: a defined kit offers it");
        }

        // ── take up ──

        [Test] public void TakeUpIsConservingAndReturnsTheSameEntityToThePack()
        {
            var kit = Kit(); string id = kit.ID; var item = Factory.CreateEntity("Dagger"); kit.GetPart<ContainerPart>().AddItem(item); SetDown(kit: kit);
            Assert.True(Take(kit));
            Assert.AreSame(kit, Pack.Objects.Single(e => e.ID == id)); Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.ID == id));
            var p = kit.GetPart<PhysicsPart>(); Assert.True(p.Takeable); Assert.False(p.Solid); Assert.False(kit.HasTag("Owned"));
            Assert.False(kit.HasPart<InstallationMarkerPart>()); Assert.AreEqual(InstallablePart.Loose, kit.GetPart<InstallablePart>().State); Assert.AreEqual(-1, kit.GetPart<PackableFixturePart>().SetTick);
            CollectionAssert.Contains(kit.GetPart<ContainerPart>().Contents, item); Assert.False(InstallationMarkerPart.Retains(kit)); Assert.AreEqual(0, Held());
        }

        [Test] public void ATakenUpKitCanBeSetDownAgainSomewhereElse()
        {
            var kit = SetDown(); Assert.True(Take(kit)); Assert.True(Act(kit, SetDownRow(kit, "west")));
            Assert.AreEqual(1, wild.GetReadOnlyEntities().Count(e => e.ID == kit.ID)); Assert.AreSame(kit, wild.GetCell(AX - 1, AY).Objects.Single(e => e.ID == kit.ID)); Assert.AreEqual(1, Held());
        }

        [Test] public void TakeUpRefusesAnOverweightLoadAndLeavesEverythingStanding()
        {
            var kit = SetDown(); Pack.MaxWeight = Pack.GetCarriedWeight() + 1; // the kit's own weight no longer fits
            Assert.False(Take(kit)); StringAssert.Contains("overweight-load", LastReason());
            Assert.True(kit.HasPart<InstallationMarkerPart>()); Assert.True(kit.HasTag("Owned")); Assert.True(kit.GetPart<PhysicsPart>().Solid); Assert.AreSame(kit, wild.GetCell(AX + 1, AY).Objects.Single(e => e.ID == kit.ID));
            Assert.False(Pack.Objects.Contains(kit)); Assert.AreEqual(InstallablePart.Installed, kit.GetPart<InstallablePart>().State);
            Pack.MaxWeight = 200; Assert.True(Take(kit), "the counter: with room it works");
        }

        [Test] public void AStandingFixtureOffersTakeUpAndHandOverOnlyAdjacentAndOnlyWhenPlayerHeld()
        {
            var kit = SetDown(); Assert.True(Actions(kit).Any(a => a.Name == "TakeUpFixture")); Assert.True(Actions(kit).Any(a => a.Name == "HandOverFixture"));
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX - 3, AY)); Assert.False(Actions(kit).Any(a => a.Name == "TakeUpFixture"), "two cells away");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX, AY));
            kit.GetPart<InstallationMarkerPart>().Origin = "Generated"; Assert.False(Actions(kit).Any(a => a.Name == "TakeUpFixture"), "an authored installation is not the player's to take");
            kit.RemovePart(kit.GetPart<InstallationMarkerPart>()); Assert.False(Actions(kit).Any(a => a.Name == "TakeUpFixture"));
        }

        // ── hand over ──

        [Test] public void HandOverEndsTheHoldAndLeavesTheObjectStandingWithItsContents()
        {
            var kit = Kit(); var item = Factory.CreateEntity("Dagger"); kit.GetPart<ContainerPart>().AddItem(item); SetDown(kit: kit);
            Assert.AreEqual(1, Held()); Assert.True(Hand(kit));
            Assert.False(kit.HasPart<InstallationMarkerPart>()); Assert.False(kit.HasTag("Owned")); Assert.AreSame(kit, wild.GetCell(AX + 1, AY).Objects.Single(e => e.ID == kit.ID));
            CollectionAssert.Contains(kit.GetPart<ContainerPart>().Contents, item); Assert.AreEqual(0, Held(), "the cap slot is returned"); Assert.False(InstallationMarkerPart.Retains(kit));
            Assert.False(Actions(kit).Any(a => a.Name == "TakeUpFixture"), "it is no longer the player's");
            Assert.True(Diag.Snapshot(50).Any(r => r.Kind == "HandedOver" && r.PayloadJson.Contains("\"spoken\":true")));
        }

        [Test] public void HandOverNeedsNoDefinition_AndTakeUpOfAnUnknownKindRefuses()
        {
            var kit = SetDown(); PackableFixtureRegistry.InitializeFromJson("{\"Version\":1,\"Fixtures\":[]}");
            Assert.False(Take(kit)); StringAssert.Contains("unknown-kind", LastReason()); Assert.True(kit.HasPart<InstallationMarkerPart>(), "still retained and standing");
            Assert.True(Hand(kit), "hand over works with no definition");
        }

        // ── vetoes, staleness, cap, occupancy ──

        sealed class VetoPart : Part
        {
            public string Event;
            public override bool HandleEvent(GameEvent e) => !(e.ID == Event);
        }

        [TestCase("BeforeInstall")] [TestCase("BeforeUninstall")]
        public void AnInstallVetoRefusesWithItsReasonAndChangesNothing(string veto)
        {
            var kit = Kit(); if (veto == "BeforeUninstall") SetDown(kit: kit);
            kit.AddPart(new VetoPart { Event = veto });
            bool ok = veto == "BeforeInstall" ? Act(kit, SetDownRow(kit)) : Take(kit);
            Assert.False(ok); StringAssert.Contains("vetoed", LastReason());
            if (veto == "BeforeInstall") { Assert.True(Pack.Objects.Contains(kit)); Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.ID == kit.ID)); Assert.False(kit.HasPart<InstallationMarkerPart>()); }
            else { Assert.False(Pack.Objects.Contains(kit)); Assert.True(kit.HasPart<InstallationMarkerPart>()); }
        }

        [Test] public void AStaleSelectionIsRefused()
        {
            var kit = Kit(); string good = SetDownRow(kit);
            Assert.False(Act(kit, Cmd(kit, wildId, AX + 1, AY, AX + 2, AY))); StringAssert.Contains("stale-selection", LastReason());   // wrong origin
            Assert.False(Act(kit, Cmd(kit, "Overworld.1.1.0", AX, AY, AX + 1, AY))); // wrong zone
            Assert.False(Act(kit, Cmd(kit, wildId, AX, AY, AX + 1, AY, id: "someone-else")));
            Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.ID == kit.ID)); Assert.True(Pack.Objects.Contains(kit));
            Assert.True(Act(kit, good), "the counter: the real row still works");
        }

        [Test] public void TwoFixturesCannotShareACell()
        {
            SetDown("east"); var second = Kit(); Assert.False(Act(second, Cmd(second, wildId, AX, AY, AX + 1, AY)));
            StringAssert.Contains("cell-occupied", LastReason()); Assert.True(Pack.Objects.Contains(second)); Assert.AreEqual(1, Held());
        }

        [Test] public void TheZoneCapRefusesTheNinthFixtureAndTakeUpReturnsTheSlot()
        {
            var placed = new List<Entity>();
            for (int i = 0; i < PlacementCheck.ZoneCap; i++)
            {
                var kit = Kit(); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 8 + i * 6, 6)); placed.Add(SetDown("east", kit));
            }
            Assert.AreEqual(PlacementCheck.ZoneCap, Held());
            Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, AX, 18); var ninth = Kit();
            var row = Actions(ninth).Single(a => a.Name == "SetDownFixture"); Assert.True(row.IsDisabled); Assert.AreEqual(PlacementCheck.CapZone, row.DisabledCode);
            Assert.False(Act(ninth, Cmd(ninth, wildId, AX, 18, AX + 1, 18)));
            Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, 8, 6);
            Assert.True(Take(placed[0])); Assert.AreEqual(PlacementCheck.ZoneCap - 1, Held());
            Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, AX, 18); Assert.True(Actions(ninth).Any(a => a.Name == "SetDownFixture" && !a.IsDisabled), "the counter: a freed slot");
        }

        // ── rollback ──

        sealed class Snapshot
        {
            public string[] Pack, Cell, Parts, Tags; public bool Takeable, Solid; public int Ticks;
            public static Snapshot Of(HeldFixtureActionTests t, Entity kit, int x, int y) => new Snapshot
            {
                Pack = t.Pack.Objects.Select(o => o.ID).ToArray(), Cell = t.wild.GetCell(x, y).Objects.Select(o => o.ID).ToArray(),
                Parts = kit.Parts.Select(p => p.GetType().Name).ToArray(), Tags = kit.Tags.Keys.OrderBy(k => k).ToArray(),
                Takeable = kit.GetPart<PhysicsPart>().Takeable, Solid = kit.GetPart<PhysicsPart>().Solid
            };
            public void AssertSame(Snapshot o)
            {
                CollectionAssert.AreEqual(Pack, o.Pack); CollectionAssert.AreEqual(Cell, o.Cell); CollectionAssert.AreEqual(Parts, o.Parts); CollectionAssert.AreEqual(Tags, o.Tags);
                Assert.AreEqual(Takeable, o.Takeable); Assert.AreEqual(Solid, o.Solid);
            }
        }

        [TestCase("set")] [TestCase("take")] [TestCase("hand")]
        public void AnOuterFailureAfterTheCommandRestoresPackCellFlagsTagsAndParts(string verb)
        {
            var kit = Kit(); if (verb != "set") SetDown(kit: kit);
            var before = Snapshot.Of(this, kit, AX + 1, AY); string tickBefore = kit.GetPart<PackableFixturePart>().SetTick.ToString();
            FailAfter();
            bool ok = verb == "set" ? Act(kit, SetDownRow(kit)) : verb == "take" ? Take(kit) : Hand(kit);
            Assert.False(ok); Snapshot.Of(this, kit, AX + 1, AY).AssertSame(before); Assert.AreEqual(tickBefore, kit.GetPart<PackableFixturePart>().SetTick.ToString());
            if (verb != "set") { Assert.AreEqual(InstallablePart.Installed, kit.GetPart<InstallablePart>().State); Assert.AreEqual("player:" + kit.ID, kit.GetPart<InstallationMarkerPart>().InstallationId); }
        }

        [Test] public void SetDownRollbackLeavesALooseKitWithoutALeakedInstallableOrMarker()
        {
            var kit = Kit(); FailAfter(); Assert.False(Act(kit, SetDownRow(kit)));
            Assert.False(kit.HasPart<InstallablePart>()); Assert.False(kit.HasPart<InstallationMarkerPart>()); Assert.False(kit.HasTag("Owned")); Assert.AreEqual(-1, kit.GetPart<PackableFixturePart>().SetTick);
            Assert.AreEqual(0, Held());
        }

        // ── declared time through the real menus ──

        [TestCase("set")] [TestCase("take")]
        public void EachVerbChargesExactlyOneActionOnCommit(string verb)
        {
            var kit = Kit(); if (verb == "take") SetDown(kit: kit);
            int spent = Spend(verb == "set" ? kit : kit, verb);
            Assert.AreEqual(10, spent);
        }

        [TestCase("set")] [TestCase("take")]
        public void ARefusedVerbChargesNothing(string verb)
        {
            var kit = Kit(); if (verb == "take") SetDown(kit: kit);
            FailAfter(); int spent = Spend(kit, verb);
            Assert.AreEqual(0, spent);
        }

        int Spend(Entity kit, string verb)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            {
                manager.SetActiveZone(wild);
                var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("held-fixture", "menu", manager, turns, Actor));
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                if (verb == "take")
                {
                    var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                    var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", hidden); state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                    typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", hidden).Invoke(ui.Input, new object[] { kit, wild.GetEntityCell(kit), false });
                    var action = ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", hidden).GetValue(menu)).Single(a => a.Name == "TakeUpFixture");
                    int before = turns.TickCount;
                    typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", hidden).Invoke(ui.Input, new object[] { action, kit, wild.GetEntityCell(kit), false });
                    return turns.TickCount - before;
                }
                // set down: the pack popup
                var inv = ui.Root.AddComponent<InventoryUI>(); inv.PlayerEntity = Actor; inv.CurrentZone = wild; inv.EntityFactory = Factory; ui.Input.InventoryUI = inv;
                var inputState = typeof(InputHandler).GetField("_inputState", hidden); inputState.SetValue(ui.Input, Enum.Parse(inputState.FieldType, "InventoryOpen")); inv.Open();
                var display = InventoryScreenData.Build(Actor).Categories.SelectMany(c => c.Items).Single(i => i.Item == kit);
                typeof(InventoryUI).GetMethod("OpenItemActionPopup", hidden).Invoke(inv, new object[] { display });
                var popup = typeof(InventoryUI).GetField("_itemActionPopup", hidden).GetValue(inv); var type = popup.GetType(); var list = (System.Collections.IList)type.GetField("Actions").GetValue(popup);
                int index = Enumerable.Range(0, list.Count).First(i => ((string)list[i].GetType().GetField("Command").GetValue(list[i])).StartsWith("SetDown|") && ((string)list[i].GetType().GetField("Label").GetValue(list[i])).EndsWith("east"));
                type.GetField("CursorIndex").SetValue(popup, index);
                var scope = new UnityEngine.InputSystem.InputTestFixture(); scope.Setup();
                try
                {
                    var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update(); InputHelper.GetKey(UnityEngine.KeyCode.None);
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter)); UnityEngine.InputSystem.InputSystem.Update(); InputHelper.GetKey(UnityEngine.KeyCode.None);
                    int before = turns.TickCount; typeof(InputHandler).GetMethod("HandleInventoryInput", hidden).Invoke(ui.Input, null);
                    return turns.TickCount - before;
                }
                finally { scope.TearDown(); }
            }
        }

        // ── the move picker, driven through the real input handler ──

        [Test] public void ThePickerStartsOnTheKitColoursTheCursorByTheSameCheckTheMoveRunsAndConfirmingMovesItForOneAction()
        {
            var kit = SetDown(); var at = wild.GetEntityCell(kit);
            using (var ui = new HotbarSaveFixture(true, false))
            {
                manager.SetActiveZone(wild);
                var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("held-fixture", "picker", manager, turns, Actor));
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var cursor = (WorldCursorState)typeof(InputHandler).GetField("_worldCursorState", hidden).GetValue(ui.Input);
                Assert.True((bool)typeof(InputHandler).GetMethod("BeginMoveTargeting", hidden).Invoke(ui.Input, new object[] { kit }));
                Assert.True(cursor.Active); Assert.AreEqual(WorldCursorMode.PlaceTarget, cursor.Mode); Assert.AreEqual(at.X, cursor.X); Assert.AreEqual(at.Y, cursor.Y); Assert.AreEqual(HeldFixtureActions.MoveReach, cursor.MaxRange);
                Assert.False(cursor.PlaceValid, "the kit's own cell is red: it is already there");
                var refresh = typeof(InputHandler).GetMethod("RefreshMoveValidity", hidden); var moveCursor = typeof(InputHandler).GetMethod("MoveThrowCursor", hidden);
                // every cell the cursor can reach is coloured exactly as the move's own check says
                for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++)
                {
                    int x = AX + dx, y = AY + dy; cursor.SetPosition(x, y); refresh.Invoke(ui.Input, null);
                    Assert.AreEqual(HeldFixtureActions.CanMoveTo(Actor, kit, wild, x, y, out _), cursor.PlaceValid, x + "," + y);
                }
                // a keyboard step moves the cursor and recolours it
                cursor.SetPosition(AX, AY - 1); moveCursor.Invoke(ui.Input, new object[] { 0, 1 }); Assert.AreEqual(AY, cursor.Y, "one step down, inside the range"); refresh.Invoke(ui.Input, null); Assert.False(cursor.PlaceValid, "the player's own cell is red");
                moveCursor.Invoke(ui.Input, new object[] { 0, 5 }); Assert.AreEqual(AY, cursor.Y, "a step that would leave the range is ignored");
                // red: confirming says why and keeps the picker open, spending nothing
                var confirm = typeof(InputHandler).GetMethod("ConfirmMoveTarget", hidden); int before = turns.TickCount; MessageLog.Clear();
                cursor.SetPosition(AX, AY); refresh.Invoke(ui.Input, null); confirm.Invoke(ui.Input, null);
                Assert.True(cursor.Active, "still choosing"); Assert.AreEqual(before, turns.TickCount); Assert.AreEqual(at.X, wild.GetEntityCell(kit).X); Assert.IsNotEmpty(MessageLog.GetRecent(2));
                // green: confirming moves it as one action and leaves the picker
                cursor.SetPosition(AX, AY - 1); refresh.Invoke(ui.Input, null); Assert.True(cursor.PlaceValid); confirm.Invoke(ui.Input, null);
                Assert.False(cursor.Active); Assert.AreEqual(AX, wild.GetEntityCell(kit).X); Assert.AreEqual(AY - 1, wild.GetEntityCell(kit).Y); Assert.True(HeldFixtureActions.IsPlayerHeld(kit)); Assert.Greater(turns.TickCount, before, "a move costs an action");
            }
        }

        [Test] public void LeavingThePickerChangesNothing()
        {
            var kit = SetDown(); var at = wild.GetEntityCell(kit);
            using (var ui = new HotbarSaveFixture(true, false))
            {
                manager.SetActiveZone(wild); var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("held-fixture", "picker", manager, turns, Actor)); const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var cursor = (WorldCursorState)typeof(InputHandler).GetField("_worldCursorState", hidden).GetValue(ui.Input); typeof(InputHandler).GetMethod("BeginMoveTargeting", hidden).Invoke(ui.Input, new object[] { kit });
                typeof(InputHandler).GetMethod("ExitMoveTargeting", hidden).Invoke(ui.Input, null);
                Assert.False(cursor.Active); Assert.AreEqual(at.X, wild.GetEntityCell(kit).X); Assert.AreEqual(17, turns.TickCount); Assert.True(HeldFixtureActions.IsPlayerHeld(kit));
                Assert.False((bool)typeof(InputHandler).GetMethod("BeginMoveTargeting", hidden).Invoke(ui.Input, new object[] { Kit(carry: false) }), "a kit that is not standing cannot be picked");
            }
        }

        [Test] public void ThePickerClosesItselfWhenTheActorChangesOrEscapeIsPressed_AndNeverLeavesTheKitHalfMoved()
        {
            var kit = SetDown(); var at = wild.GetEntityCell(kit);
            using (var ui = new HotbarSaveFixture(true, false))
            {
                manager.SetActiveZone(wild); var turns = new TurnManager(); turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("held-fixture", "picker", manager, turns, Actor)); const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var cursor = (WorldCursorState)typeof(InputHandler).GetField("_worldCursorState", hidden).GetValue(ui.Input); var begin = typeof(InputHandler).GetMethod("BeginMoveTargeting", hidden); var handle = typeof(InputHandler).GetMethod("HandleMoveTargeting", hidden);
                // another actor takes over (death, a possession, a reload): the picker closes without moving anything
                begin.Invoke(ui.Input, new object[] { kit }); typeof(InputHandler).GetField("_moveActor", hidden).SetValue(ui.Input, Factory.CreateEntity("Villager")); handle.Invoke(ui.Input, null);
                Assert.False(cursor.Active); Assert.AreEqual(at.X, wild.GetEntityCell(kit).X); Assert.AreEqual(at.Y, wild.GetEntityCell(kit).Y);
                // Escape leaves it where it is
                begin.Invoke(ui.Input, new object[] { kit }); Assert.True(cursor.Active); var scope = new UnityEngine.InputSystem.InputTestFixture(); scope.Setup();
                try
                {
                    var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); UnityEngine.InputSystem.InputSystem.Update(); InputHelper.GetKey(UnityEngine.KeyCode.None);
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape)); UnityEngine.InputSystem.InputSystem.Update(); InputHelper.GetKey(UnityEngine.KeyCode.None);
                    handle.Invoke(ui.Input, null);
                }
                finally { scope.TearDown(); }
                Assert.False(cursor.Active, "Escape closes the picker"); Assert.AreEqual(at.X, wild.GetEntityCell(kit).X); Assert.True(HeldFixtureActions.IsPlayerHeld(kit)); Assert.AreEqual(17, turns.TickCount, "and costs nothing");
            }
        }

        // ── save, state and station ──

        [Test] public void BothPartsPassTheSavedPartHygieneAndRoundTripTheirFields()
        {
            var kit = SetDown(); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(kit);
            Assert.AreEqual(Kind, copy.GetPart<PackableFixturePart>().Kind); Assert.AreEqual(kit.GetPart<PackableFixturePart>().SetTick, copy.GetPart<PackableFixturePart>().SetTick);
            Assert.AreEqual(InstallablePart.Installed, copy.GetPart<InstallablePart>().State); Assert.AreEqual(InstallablePart.Portable, copy.GetPart<InstallablePart>().Mode);
            Assert.True(InstallationMarkerPart.Retains(copy)); Assert.True(copy.HasTag("Owned")); Assert.False(copy.GetPart<PhysicsPart>().Takeable);
        }

        [Test] public void SetDownSaveReloadTakeUpDoesNotDuplicate()
        {
            var kit = SetDown(); string id = kit.ID; var item = Factory.CreateEntity("Dagger"); kit.GetPart<ContainerPart>().AddItem(item);
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(kit); Zone.RemoveEntity(kit); Assert.True(Zone.AddEntity(copy, AX + 1, AY));
            Assert.True(Take(copy)); Assert.AreEqual(1, Pack.Objects.Count(e => e.ID == id)); Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.ID == id));
            Assert.AreEqual(1, copy.GetPart<ContainerPart>().Contents.Count);
        }

        [Test] public void AnOldObjectWithoutThePartsIsNotInstallableAndLoadsUnchanged()
        {
            var plain = Factory.CreateEntity("Chest"); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(plain);
            Assert.False(copy.HasPart<PackableFixturePart>()); Assert.False(copy.HasPart<InstallablePart>()); Assert.False(copy.HasPart<InstallationMarkerPart>());
            Assert.False(Actions(copy).Any(a => a.Name.Contains("Fixture")));
        }

        [Test] public void AFreshSessionHasNoNewSaveFootprint()
        {
            Assert.AreEqual(0, wild.GetReadOnlyEntities().Count(e => e.HasPart<PackableFixturePart>() || e.HasPart<InstallablePart>() || e.HasPart<InstallationMarkerPart>()));
            Assert.False(Actor.HasPart<InstallablePart>()); Assert.False(Actor.HasPart<PackableFixturePart>()); Assert.AreEqual(0, Held());
        }

        [Test] public void ALooseInstallableVetoesStationReadinessAndAnInstalledOneDoesNot()
        {
            var forge = Place("TinkersForge"); Assert.True(StationReadiness.Evaluate(forge, wild, "forge", out _));
            var part = new InstallablePart { State = InstallablePart.Loose }; forge.AddPart(part);
            Assert.False(StationReadiness.Evaluate(forge, wild, "forge", out string code)); Assert.AreEqual("loose", code);
            part.State = InstallablePart.Installed; Assert.True(StationReadiness.Evaluate(forge, wild, "forge", out _));
        }

        [Test] public void ASetDownKitStaysARetainingMarkerOnlyWhileInstalled()
        {
            var kit = SetDown(); Assert.True(InstallationMarkerPart.Retains(kit)); Assert.True(Take(kit)); Assert.False(InstallationMarkerPart.Retains(kit));
            Assert.True(Act(kit, SetDownRow(kit, "west"))); Assert.True(InstallationMarkerPart.Retains(kit)); Assert.True(Hand(kit)); Assert.False(InstallationMarkerPart.Retains(kit));
        }

        // ── diagnostics ──

        [Test] public void EveryVerbAndRefusalIsRecorded()
        {
            var kit = SetDown(); Take(kit); Act(kit, SetDownRow(kit, "west")); Hand(kit);
            Act(kit, Cmd(kit, wildId, AX + 5, AY, AX + 6, AY)); // stale: not in the pack
            var kinds = Diag.Snapshot(100).Where(r => r.Category == "furniture").Select(r => r.Kind).ToArray();
            foreach (var k in new[] { "SetDown", "TakenUp", "HandedOver", "FixtureRefused" }) CollectionAssert.Contains(kinds, k);
        }

        // ── adversarial: malformed input never throws and never mutates ──

        [TestCase("SetDown|")] [TestCase("SetDown|zone|x|y|a|b|c")] [TestCase("SetDown|zone|1|2|3|4")] [TestCase("SetDown|zone|1|2|3|4|5|6|7")] [TestCase("TakeUp|")] [TestCase("HandOver|a")] [TestCase("SetDown||||||")]
        public void MalformedCommandsAreRefusedWithoutChanges(string command)
        {
            var kit = Kit(); var before = Snapshot.Of(this, kit, AX + 1, AY);
            Assert.DoesNotThrow(() => Act(kit, command)); Snapshot.Of(this, kit, AX + 1, AY).AssertSame(before);
        }

        [Test] public void ADeadOrBlockedActorCannotSetDownOrTakeUp()
        {
            var kit = Kit(); string row = SetDownRow(kit); Actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(kit, row)); Assert.True(Pack.Objects.Contains(kit)); Assert.AreEqual(0, Held());
        }

        [Test] public void AKitThatIsNoLongerCarriedCannotBeSetDown()
        {
            var kit = Kit(); string row = SetDownRow(kit); Pack.RemoveObject(kit); Assert.True(Zone.AddEntity(kit, AX, AY + 1));
            Assert.False(Act(kit, row)); Assert.AreEqual(0, Held()); Assert.False(kit.HasPart<InstallationMarkerPart>());
        }

        [Test] public void ARegistryRowOfAnotherClassCannotBeSetDown()
        {
            PackableFixtureRegistry.InitializeFromJson("{\"Version\":1,\"Fixtures\":[{\"Kind\":\"test-box\",\"Class\":\"Hauled\",\"Blueprint\":\"Chest\"}]}");
            var kit = Kit(); Assert.False(Actions(kit).Any(a => a.Name == "SetDownFixture"));
        }
    }

    /// <summary>The fixture definition file: structural drops are per row and never cancel the others.</summary>
    public sealed class PackableFixtureRegistryTests
    {
        [TearDown] public void Reset() { PackableFixtureRegistry.ResetForTests(); }

        static string Row(string kind, string cls = "Carried", string blueprint = "Chest", string ground = "OpenGround", int rounds = 1)
            => "{\"Kind\":\"" + kind + "\",\"Class\":\"" + cls + "\",\"Blueprint\":\"" + blueprint + "\",\"GroundClass\":\"" + ground + "\",\"Rounds\":" + rounds + "}";
        static string File(params string[] rows) => "{\"Version\":1,\"Fixtures\":[" + string.Join(",", rows) + "]}";

        [Test] public void AGoodRowLoadsAndABadNeighbourIsDroppedAloneWithItsCode()
        {
            var errors = PackableFixtureRegistry.InitializeFromJson(File(Row("a"), Row("b", cls: "Nonsense"), Row("c")));
            Assert.NotNull(PackableFixtureRegistry.Get("a")); Assert.NotNull(PackableFixtureRegistry.Get("c")); Assert.Null(PackableFixtureRegistry.Get("b")); StringAssert.StartsWith("P03", errors.Single());
        }

        [Test] public void EveryStructuralCodeHasItsCase()
        {
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row(""))).Single(), Does.StartWith("P01"));
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row("a"), Row("a"))).Single(), Does.StartWith("P02"));
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row("a", cls: "x"))).Single(), Does.StartWith("P03"));
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row("a", rounds: -1))).Single(), Does.StartWith("P04"));
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row("a", ground: "Swamp"))).Single(), Does.StartWith("P05"));
            Assert.That(PackableFixtureRegistry.InitializeFromJson(File(Row("a", blueprint: ""))).Single(), Does.StartWith("P06"));
            Assert.IsEmpty(PackableFixtureRegistry.InitializeFromJson(File(Row("a"))), "the valid neighbour");
        }

        [Test] public void MalformedJsonAndAnEmptyFileDropEverythingWithoutThrowing()
        {
            Assert.DoesNotThrow(() => PackableFixtureRegistry.InitializeFromJson("{not json")); Assert.AreEqual(0, PackableFixtureRegistry.All.Count);
            Assert.IsEmpty(PackableFixtureRegistry.InitializeFromJson("{}"));
        }

        [Test] public void TheShippedFileLoadsAndShipsExactlyTheShippedKits()
        {
            PackableFixtureRegistry.ResetForTests(); CollectionAssert.AreEquivalent(new[] { "camp-box", "camp-hearth", "camp-bench", "camp-still", "camp-trough", "camp-fishtrap" }, PackableFixtureRegistry.All.Select(d => d.Kind).ToArray(), "each shipped kit is deliberate; a new one needs its own slice and review");
            Assert.Null(PackableFixtureRegistry.Get("not-a-kit")); Assert.Null(PackableFixtureRegistry.Get(null));
        }
    }
}
