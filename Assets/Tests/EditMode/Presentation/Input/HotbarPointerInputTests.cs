using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Actual queued native mouse -> InputHandler.Update -> the painted hotbar.
    // The reflected helper is the sole proposed API; no production stub is needed.
    public sealed class HotbarPointerInputTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static object Field(object owner, string name) => owner.GetType().GetField(name, Instance).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Instance).SetValue(owner, value);

        [TestCase(1280, 800)] [TestCase(1920, 1080)]
        public void NativeLeftPressOnPaintedLungeCardSelectsAndTargetsThatExactAbility(int width, int height)
        {
            using (var r = new Rig(width, height))
            {
                Vector2 point = r.CardPoint(1);
                r.Frame(point); r.Frame(point, left: true);
                r.AssertTargeting(1); r.AssertUnspent();
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void HoverAndRightButtonDoNotActivateThePaintedCard(bool right)
        {
            using (var r = new Rig())
            {
                Vector2 point = r.CardPoint(1);
                r.Frame(point, right: right);
                r.AssertNormal(2); r.AssertUnspent();
                r.Frame(point); r.Frame(point, left: true);
                r.AssertTargeting(1); r.AssertUnspent();
            }
        }

        [Test]
        public void OutsideHotbarPressDoesNotBorrowTheNearestCard()
        {
            using (var r = new Rig())
            {
                Vector2 outside = new Vector2(r.Camera.pixelRect.xMax + 8, r.Camera.pixelRect.center.y);
                Assert.False(r.Saved.Renderer.TryGetHotbarSlotAtScreenPosition(outside, out _));
                r.Frame(outside, left: true); r.AssertNormal(2); r.AssertUnspent();
                Vector2 point = r.CardPoint(1);
                r.Frame(point); r.Frame(point, left: true);
                r.AssertTargeting(1);
            }
        }

        [Test]
        public void HeldPressCannotReactivateAfterCancelButReleaseAndRepressCan()
        {
            using (var r = new Rig())
            {
                Vector2 point = r.CardPoint(1);
                r.Frame(point); r.Frame(point, left: true); r.AssertTargeting(1);
                r.Frame(point, left: true, key: Key.Escape); r.AssertNormal(1);
                r.Frame(point, left: true); r.AssertNormal(1);
                r.Frame(point); r.AssertNormal(1);
                r.Frame(point, left: true); r.AssertTargeting(1); r.AssertUnspent();
            }
        }

        [Test]
        public void MouseOnAnotherCardCannotReplaceAnAlreadyPendingKeyboardAbility()
        {
            using (var r = new Rig())
            {
                r.Saved.Restore(1);
                r.Frame(r.CardPoint(1), key: Key.Enter); r.AssertTargeting(1);
                r.Frame(r.CardPoint(2)); r.Frame(r.CardPoint(2), left: true);
                r.AssertTargeting(1); r.AssertUnspent();
            }
        }

        [Test]
        public void CooldownCardCanBeSelectedButCannotStartTargetingOrSpendATurn()
        {
            using (var r = new Rig())
            {
                r.Ability(1).CooldownRemaining = 7; r.Paint();
                Vector2 point = r.CardPoint(1);
                r.Frame(point); r.Frame(point, left: true);
                r.AssertNormal(1); Assert.AreEqual(7, r.Ability(1).CooldownRemaining);
                r.AssertUnspent(expectCooldown: 7);
                r.Ability(1).CooldownRemaining = 0;
                r.Frame(point); r.Frame(point, left: true); r.AssertTargeting(1);
            }
        }

        [Test]
        public void EnabledNativePointerSuppliesBothPositionAndOnlyTheLeftPressEdge()
        {
            using (var r = new Rig())
            {
                Vector2 point = r.CardPoint(1);
                r.Queue(point); AssertPair(point, false);
                r.Queue(point, right: true); AssertPair(point, false);
                r.Queue(point); r.Queue(point, left: true); AssertPair(point, true);
                r.Queue(point, left: true); AssertPair(point, false);
                r.Queue(point); r.Queue(point, left: true); AssertPair(point, true);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void AbsentOrDisabledNativePointerUsesTheActualLegacyPair(bool disabled)
        {
            using (var r = new Rig())
            {
                r.Queue(r.CardPoint(1), left: true);
                Assert.True(r.Mouse.leftButton.wasPressedThisFrame);
                if (disabled)
                { InputSystem.DisableDevice(r.Mouse); Assert.False(r.Mouse.enabled); }
                else
                { InputSystem.RemoveDevice(r.Mouse); Assert.IsNull(Mouse.current); }
                // InputTestFixture cannot synthesize a legacy physical click. This
                // pins fallback pair selection, not actual legacy-device delivery.
                AssertPair((Vector2)UnityEngine.Input.mousePosition, UnityEngine.Input.GetMouseButtonDown(0));
                if (disabled) InputSystem.EnableDevice(r.Mouse);
                else r.Mouse = InputSystem.AddDevice<Mouse>();
                r.Mouse.MakeCurrent();
                r.Queue(r.CardPoint(1)); r.Queue(r.CardPoint(1), left: true);
                AssertPair(r.CardPoint(1), true);
            }
        }

        static void AssertPair(Vector2 expectedPosition, bool expectedPressed)
        {
            var method = typeof(InputHandler).GetMethod("ReadHotbarPointer", Static);
            Assert.NotNull(method, "One coherent pointer backend must supply position and left-press together.");
            var args = new object[] { Vector2.zero, false };
            method.Invoke(null, args);
            Assert.AreEqual(expectedPosition, (Vector2)args[0]);
            Assert.AreEqual(expectedPressed, (bool)args[1]);
        }

        sealed class Rig : IDisposable
        {
            readonly InputTestFixture devices = new InputTestFixture();
            readonly List<Action> restore = new List<Action>();
            bool devicesReady;
            GameObject root;
            RenderTexture target;
            Tilemap foreground, background;
            GameplayHotbarRenderer painter;
            Keyboard keyboard;
            public Mouse Mouse;
            public Camera Camera;
            public HotbarSaveFixture Saved;
            public InputHandler Input => Saved.Input;
            public Entity Player => Input.PlayerEntity;
            public ActivatedAbility Ability(int slot) => Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(slot);

            public Rig(int width = 1280, int height = 800)
            {
                try
                {
                    SnapshotStatics(typeof(MessageLog)); SnapshotStatics(typeof(NativeGamepadInput));
                    SnapshotStatics(typeof(HotbarStateBuilder)); SnapshotStatics(typeof(PerformanceDiagnostics));
                    foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" })
                        Snapshot(typeof(Diag).GetField(name, Static));
                    OwnCache(typeof(HudPixelFont), "atlas", "primitives", "tiles");
                    OwnCache(typeof(HudCardFrame), "texture", "tile");
                    OwnCache(typeof(HudSkillIconAtlas), "atlas", "tiles");
                    devices.Setup(); devicesReady = true;
                    keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                    Mouse = InputSystem.AddDevice<Mouse>(); Mouse.MakeCurrent();
                    Saved = new HotbarSaveFixture();
                    MessageLog.OnMessage = null; MessageLog.Clear();
                    Assert.False(SpellFxSettingsPanel.IsOpen, "The isolated native fixture must not inherit an open settings modal.");
                    var state = HotbarSaveFixture.MakeState(2);
                    var abilities = state.Player.GetPart<ActivatedAbilitiesPart>();
                    BaseSkillPart[] skills = { new LongBlades_EnGarde(), new LongBlades_Lunge(), new LongBlades_FollowThrough() };
                    foreach (var skill in skills)
                    {
                        var spec = skill.DeclareActivatedAbility(state.Player);
                        Guid id = abilities.AddAbility(spec.DisplayName, spec.Command, spec.Class,
                            spec.TargetingMode, spec.Range, skill.Name);
                        abilities.GetAbility(id).MaxCooldown = spec.Cooldown;
                    }
                    Saved.BindOld(state); Input.MoveRepeatDelay = 0;
                    root = new GameObject("Owned native hotbar pointer fixture");
                    var grid = root.AddComponent<Grid>();
                    foreground = Child("painted icons and text").AddComponent<Tilemap>();
                    background = Child("painted card backgrounds").AddComponent<Tilemap>();
                    Camera = Child("owned hotbar camera").AddComponent<Camera>();
                    target = new RenderTexture(width, height, 0); Camera.targetTexture = target;
                    Camera.transform.position = new Vector3(0, 0, -10);
                    var rect = new Rect(0, 0, .735f, .15f);
                    GameplayHotbarLayout.ConfigureOverlayCamera(Camera, rect, width * rect.width / (height * rect.height));
                    grid.cellSize = new Vector3(GameplayHotbarLayout.ComputeCellWidth(Camera.aspect), 1, 0);
                    Set(Saved.Renderer, "_hotbarTilemap", foreground);
                    Set(Saved.Renderer, "_hotbarGrid", grid);
                    Saved.Renderer.SetHotbarCamera(Camera);
                    painter = new GameplayHotbarRenderer(foreground, background); Paint();
                    Frame(new Vector2(-100, -100)); // Prime controller-session quarantine with neutral devices.
                    Assert.True(Input.TurnManager.WaitingForInput); Assert.AreSame(Player, Input.TurnManager.CurrentActor);
                    AssertNormal(2); AssertUnspent();
                }
                catch { Dispose(); throw; }
            }

            GameObject Child(string name)
            { var child = new GameObject(name); child.transform.SetParent(root.transform, false); return child; }
            public void Paint() => painter.Render(HotbarStateBuilder.Build(Player, Saved.Selected,
                (ActivatedAbility)Field(Input, "_pendingAbility")), Camera);
            public Vector2 CardPoint(int slot)
            {
                var cell = new Vector3Int(slot * GameplayHotbarLayout.SlotWidth + 3, 2, 0);
                Assert.NotNull(foreground.GetTile(cell), "Hit point must belong to the actual submitted icon.");
                Vector2 point = Camera.WorldToScreenPoint(foreground.GetCellCenterWorld(cell));
                Assert.True(Saved.Renderer.TryGetHotbarSlotAtScreenPosition(point, out int hit));
                Assert.AreEqual(slot, hit, "Painted geometry and public hit testing must agree before testing input.");
                return point;
            }
            public void Queue(Vector2 point, bool left = false, bool right = false, Key? key = null)
            {
                var state = new MouseState { position = point };
                if (left) state = state.WithButton(MouseButton.Left);
                if (right) state = state.WithButton(MouseButton.Right);
                InputSystem.QueueStateEvent(Mouse, state);
                InputSystem.QueueStateEvent(keyboard, key.HasValue ? new KeyboardState(key.Value) : new KeyboardState());
                InputSystem.Update();
            }
            public void Frame(Vector2 point, bool left = false, bool right = false, Key? key = null)
            {
                Queue(point, left, right, key);
                // Only repeat timing is bypassed: real Update, modal gates,
                // pointer hit testing and ability-targeting transition all run.
                Set(Input, "_lastMoveTime", -999f); Set(Input, "_lastWaitTime", -999f);
                typeof(InputHandler).GetMethod("Update", Instance).Invoke(Input, null);
                Paint();
            }
            public void AssertNormal(int selected)
            {
                Assert.AreEqual("Normal", Field(Input, "_inputState").ToString());
                Assert.IsNull(Field(Input, "_pendingAbility"));
                Assert.AreEqual(selected, Saved.Selected); Assert.AreEqual(selected, Saved.Rendered);
            }
            public void AssertTargeting(int slot)
            {
                Assert.AreEqual("AwaitingDirection", Field(Input, "_inputState").ToString());
                Assert.AreSame(Ability(slot), Field(Input, "_pendingAbility"));
                Assert.AreSame(Ability(slot), Field(Saved.Renderer, "_pendingHotbarAbility"));
                Assert.AreEqual(slot, Saved.Selected); Assert.AreEqual(slot, Saved.Rendered);
            }
            public void AssertUnspent(int expectCooldown = 0)
            {
                Assert.AreEqual(17, Input.TurnManager.TickCount); Assert.AreEqual(1000, Input.TurnManager.GetEnergy(Player));
                Assert.AreEqual(20, Player.GetStatValue("Hitpoints"));
                Assert.AreEqual((3, 4), Input.CurrentZone.GetEntityPosition(Player));
                Assert.AreEqual(expectCooldown, Ability(1).CooldownRemaining);
            }
            public void Dispose()
            {
                try
                {
                    Saved?.Dispose(); Saved = null;
                    if (root != null) Object.DestroyImmediate(root);
                    if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                }
                finally
                {
                    if (devicesReady) { devicesReady = false; devices.TearDown(); }
                    for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                    restore.Clear();
                }
            }
            void OwnCache(Type type, params string[] names)
            {
                var fields = names.Select(name => type.GetField(name, Static)).ToArray();
                var previous = fields.Select(field => field.GetValue(null)).ToArray();
                foreach (var field in fields) field.SetValue(null, null);
                restore.Add(() => { type.GetMethod("Release", Static).Invoke(null, null);
                    for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, previous[i]); });
            }
            void SnapshotStatics(Type type)
            { foreach (var field in type.GetFields(Static)) if (!field.IsLiteral) Snapshot(field); }
            void Snapshot(FieldInfo field)
            {
                Assert.NotNull(field); object value = field.GetValue(null);
                if (value is PerformanceFrameSnapshot frame)
                {
                    var copy = new PerformanceFrameSnapshot(frame);
                    restore.Add(() => typeof(PerformanceFrameSnapshot).GetMethod("CopyFrom", Instance).Invoke(frame, new object[] { copy }));
                }
                else if (value is Array array)
                { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
                else if (value is IDictionary dictionary)
                {
                    var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                    restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
                }
                else if (value is IList list)
                { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
                else if (value is HashSet<KeyCode> set)
                { var copy = set.ToArray(); restore.Add(() => { set.Clear(); set.UnionWith(copy); }); }
                else if (value is IEnumerable enumerable && value.GetType().IsGenericType
                    && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
                {
                    var copy = enumerable.Cast<object>().ToArray();
                    restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                        foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
                }
                if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
            }
        }
    }
}
