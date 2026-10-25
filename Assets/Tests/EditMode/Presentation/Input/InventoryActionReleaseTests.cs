using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class InventoryActionReleaseTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static object Field(object owner, string name) => owner.GetType().GetField(name, Instance).GetValue(owner);
        static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Instance).Invoke(owner, args);

        [Test]
        public void ActualPlantShortcutMustBeReleasedBeforeItCanMoveThePlayer()
        {
            using (var r = new Rig())
            {
                int tick = r.Turns.TickCount, energy = r.Energy;
                r.State(Key.B);
                r.AssertPlantedOnce(tick, energy);
                int paidTick = r.Turns.TickCount, paidEnergy = r.Energy;
                for (int i = 0; i < 3; i++)
                {
                    r.State(Key.B);
                    Assert.AreEqual((4, 4), r.Zone.GetEntityPosition(r.Player),
                        "The held inventory shortcut must not become southwest movement on a later input update.");
                    Assert.AreEqual(paidTick, r.Turns.TickCount);
                    Assert.AreEqual(paidEnergy, r.Energy);
                    Assert.AreEqual(1, r.Seed.GetPart<StackerPart>().StackCount);
                    Assert.AreEqual(1, r.Crops);
                }
                r.State();
                Assert.AreEqual((4, 4), r.Zone.GetEntityPosition(r.Player));
                Assert.AreEqual(paidTick, r.Turns.TickCount, "Release itself costs no turn.");
                Assert.AreEqual(paidEnergy, r.Energy);
                r.State(Key.B);
                Assert.AreEqual((3, 5), r.Zone.GetEntityPosition(r.Player), "A new B press must retain ordinary movement.");
                r.AssertOnePaidAction(paidTick, paidEnergy);
                Assert.AreEqual(1, r.Crops);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RefusedPlantKeepsItsMenuAndDoesNotLeaveAClosingShortcut(bool retryByReturn)
        {
            using (var r = new Rig())
            {
                var veto = new RefusePlant(); r.Player.AddPart(veto);
                object popup = Field(r.UI, "_itemActionPopup");
                int tick = r.Turns.TickCount, energy = r.Energy;
                r.State(Key.B);
                Assert.AreEqual(1, veto.Calls, "The actual inventory command reached its veto seam.");
                Assert.True(r.UI.IsOpen); Assert.AreSame(popup, Field(r.UI, "_itemActionPopup"));
                Assert.AreEqual("InventoryOpen", Field(r.Input, "_inputState").ToString());
                Assert.AreEqual(0, r.Crops); Assert.AreEqual(2, r.Seed.GetPart<StackerPart>().StackCount);
                Assert.AreSame(r.Player, r.Seed.GetPart<PhysicsPart>().InInventory);
                r.State(Key.B);
                Assert.AreEqual(1, veto.Calls, "A held rejected letter is not another selection press.");
                Assert.AreEqual(tick, r.Turns.TickCount); Assert.AreEqual(energy, r.Energy);
                Assert.AreEqual((4, 4), r.Zone.GetEntityPosition(r.Player));
                r.State();
                if (retryByReturn)
                {
                    veto.Refuse = false;
                    popup.GetType().GetField("CursorIndex", Instance).SetValue(popup, r.PlantIndex);
                    r.State(Key.Enter);
                    r.AssertPlantedOnce(tick, energy);
                    Assert.AreEqual(KeyCode.None, Field(r.Input, "_worldActionKeyToRelease"),
                        "Selected-row confirmation must not inherit the earlier rejected alphabetic shortcut.");
                    tick = r.Turns.TickCount; energy = r.Energy;
                    r.State(Key.Enter);
                    Assert.AreEqual(tick, r.Turns.TickCount); Assert.AreEqual(energy, r.Energy);
                    r.State();
                }
                else
                {
                    r.State(Key.Escape); Assert.True(r.UI.IsOpen); Assert.IsNull(Field(r.UI, "_itemActionPopup"));
                    r.State(); r.State(Key.I); Assert.False(r.UI.IsOpen);
                    Assert.AreEqual(tick, r.Turns.TickCount); Assert.AreEqual(energy, r.Energy);
                    r.State();
                }
                r.State(Key.B);
                Assert.AreEqual((3, 5), r.Zone.GetEntityPosition(r.Player));
                r.AssertOnePaidAction(tick, energy);
            }
        }

        [Test]
        public void ReturnConfirmationDoesNotInventAnAlphabeticReleaseRequirement()
        {
            using (var r = new Rig())
            {
                object popup = Field(r.UI, "_itemActionPopup");
                popup.GetType().GetField("CursorIndex", Instance).SetValue(popup, r.PlantIndex);
                int tick = r.Turns.TickCount, energy = r.Energy;
                r.State(Key.Enter); r.AssertPlantedOnce(tick, energy);
                Assert.AreEqual(KeyCode.None, Field(r.Input, "_worldActionKeyToRelease"));
                tick = r.Turns.TickCount; energy = r.Energy;
                r.State(Key.Enter); Assert.AreEqual(tick, r.Turns.TickCount); Assert.AreEqual(energy, r.Energy);
                r.State(); r.State(Key.B);
                Assert.AreEqual((3, 5), r.Zone.GetEntityPosition(r.Player));
                r.AssertOnePaidAction(tick, energy);
            }
        }

        sealed class RefusePlant : Part
        {
            public bool Refuse = true;
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeInventoryAction" && e.GetStringParameter("Command")?.StartsWith("PlantSeedAt|", StringComparison.Ordinal) == true)
                { Calls++; return !Refuse; }
                return true;
            }
        }

        sealed class Rig : IDisposable
        {
            readonly InputTestFixture devices = new InputTestFixture();
            readonly List<(FieldInfo field, object value)> gamepad = new List<(FieldInfo, object)>();
            readonly List<(Array target, Array copy)> arrays = new List<(Array, Array)>();
            readonly List<(HashSet<KeyCode> target, KeyCode[] copy)> sets = new List<(HashSet<KeyCode>, KeyCode[])>();
            readonly Dictionary<PropertyInfo, object> hooks = typeof(EntityVisualHooks).GetProperties(Static)
                .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly long revision = MessageLog.Revision, generation = MessageLog.HistoryGeneration;
            readonly Action<string> oldMessage = MessageLog.OnMessage;
            readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly object origins = typeof(MessageLog).GetField("AnnouncementOrigins", Static).GetValue(null);
            readonly Array oldOrigins;
            readonly int equipmentVersion = EquipmentChangeBus.GlobalVersion;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", Static);
            readonly object oldBound;
            readonly Zone priorBound;
            readonly Action<int, int> priorChanged;
            readonly Action priorSight;
            HotbarSaveFixture saved;
            Keyboard keyboard;
            bool devicesReady;
            public readonly Entity Player, Seed;
            public readonly Zone Zone;
            public readonly TurnManager Turns;
            public readonly InventoryUI UI;
            public readonly int PlantIndex;
            public InputHandler Input => saved.Input;
            public int Energy => Turns.GetEnergy(Player);
            public int Crops => Zone.GetCell(5, 4).Objects.Count(e => e.HasPart<CropPart>());

            public Rig()
            {
                oldOrigins = (Array)origins.GetType().GetMethod("ToArray").Invoke(origins, null);
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out var prior))
                { priorBound = prior; priorChanged = prior.TileState.OnCellChanged; priorSight = prior.TileState.OnSightChanged; }
                foreach (var field in typeof(NativeGamepadInput).GetFields(Static))
                {
                    if (field.IsLiteral) continue;
                    object value = field.GetValue(null);
                    if (!field.IsInitOnly) gamepad.Add((field, value));
                    if (value is Array array) arrays.Add((array, (Array)array.Clone()));
                    if (value is HashSet<KeyCode> set) sets.Add((set, set.ToArray()));
                }
                try
                {
                    devices.Setup(); devicesReady = true;
                    keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                    saved = new HotbarSaveFixture(true, false);
                    EntityVisualHooks.Reset(); ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
                    MessageLog.OnMessage = null; MessageLog.Clear();
                    typeof(SettlementManager).GetProperty("Current", Static).SetValue(null, null);
                    TurnManager.World = null;
                    var factory = DensityReferenceGladeTests.Factory();
                    SeedPart.Factory = CropSystem.Factory = factory;
                    var manager = OverworldZoneManager.CreateDetached(factory, 64, true);
                    Zone = new Zone("Overworld.11.10.0"); manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
                    Player = factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(Player, 4, 4));
                    var terrain = factory.CreateEntity("Grass"); terrain.AddPart(new CultivatedSoilPart());
                    Assert.True(Zone.AddEntity(terrain, 5, 4)); Assert.True(CultivatedSoilPart.IsCultivated(Zone, Zone.GetCell(5, 4)));
                    Seed = factory.CreateEntity("PitchpodSeed"); Seed.GetPart<StackerPart>().StackCount = 2;
                    Assert.True(Player.GetPart<InventoryPart>().AddObject(Seed));
                    Turns = new TurnManager(); Turns.RestoreSavedState(17, true, Player,
                        new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                    MessageLog.TickProvider = () => Turns.TickCount;
                    saved.BindOld(GameSessionState.Capture("inventory-release", "actual-plant", manager, Turns, Player));
                    foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
                    UI = saved.Root.AddComponent<InventoryUI>(); UI.PlayerEntity = Player; UI.CurrentZone = Zone; UI.EntityFactory = factory;
                    Input.InventoryUI = UI; Input.MoveRepeatDelay = 0;
                    State(); Call(Input, "OpenInventory"); Assert.True(UI.ReopenItemActionPopupFor(Seed));
                    var actions = (IList)Field(Field(UI, "_itemActionPopup"), "Actions"); PlantIndex = -1;
                    for (int i = 0; i < actions.Count; i++)
                        if ((string)Field(actions[i], "Command") == "PlantSeedAt|Overworld.11.10.0|5|4") PlantIndex = i;
                    Assert.AreEqual(1, PlantIndex, "The actual gathered adjacent planting action must be displayed as B; do not fake or reorder the menu.");
                    Assert.AreEqual(0, Crops); Assert.AreEqual("InventoryOpen", Field(Input, "_inputState").ToString());
                }
                catch { Dispose(); throw; }
            }

            public void State(params Key[] keys)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
                // EditMode input updates do not advance Time.time. Keep real
                // device/Update/command/turn processing, bypass only repeat timing.
                typeof(InputHandler).GetField("_lastMoveTime", Instance).SetValue(Input, -999f);
                typeof(InputHandler).GetField("_lastWaitTime", Instance).SetValue(Input, -999f);
                Call(Input, "Update");
            }
            public void AssertOnePaidAction(int tick, int energy)
            {
                Assert.Greater(Turns.TickCount, tick);
                Assert.AreEqual(energy - TurnManager.ActionThreshold + (Turns.TickCount - tick)
                    * Player.GetStatValue("Speed", TurnManager.DefaultSpeed), Energy);
            }
            public void AssertPlantedOnce(int tick, int energy)
            {
                Assert.False(UI.IsOpen); Assert.AreEqual("Normal", Field(Input, "_inputState").ToString());
                Assert.True(Turns.WaitingForInput, "The paid action has returned control to the player.");
                Assert.AreSame(Player, Turns.CurrentActor);
                Assert.AreEqual((4, 4), Zone.GetEntityPosition(Player)); Assert.AreEqual(1, Crops);
                Assert.AreEqual("PitchpodCrop", Zone.GetCell(5, 4).Objects.Single(e => e.HasPart<CropPart>()).BlueprintName);
                Assert.AreEqual(1, Seed.GetPart<StackerPart>().StackCount); AssertOnePaidAction(tick, energy);
            }
            public void Dispose()
            {
                try { UI?.Close(); saved?.Dispose(); }
                finally
                {
                    if (devicesReady) { devicesReady = false; devices.TearDown(); }
                    foreach (var entry in gamepad) entry.field.SetValue(null, entry.value);
                    foreach (var entry in arrays) Array.Copy(entry.copy, entry.target, entry.copy.Length);
                    foreach (var entry in sets) { entry.target.Clear(); entry.target.UnionWith(entry.copy); }
                    foreach (var hook in hooks) hook.Key.SetValue(null, hook.Value);
                    bound.SetValue(null, oldBound);
                    if (priorBound != null) { priorBound.TileState.OnCellChanged = priorChanged; priorBound.TileState.OnSightChanged = priorSight; }
                    typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, equipmentVersion);
                    MessageLog.Restore(messages, announcements, flash, serial);
                    origins.GetType().GetMethod("Clear").Invoke(origins, null);
                    foreach (object origin in oldOrigins) origins.GetType().GetMethod("Enqueue").Invoke(origins, new[] { origin });
                    typeof(MessageLog).GetProperty("Revision", Static).SetValue(null, revision);
                    typeof(MessageLog).GetProperty("HistoryGeneration", Static).SetValue(null, generation);
                    MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
