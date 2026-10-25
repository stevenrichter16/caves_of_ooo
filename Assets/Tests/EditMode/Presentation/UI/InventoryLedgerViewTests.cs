using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Real inventory state, retained graphics, and existing input transitions.
    /// CPU ownership/layout assertions do not prove GPU color, pointer feel, or Deck play.</summary>
    public sealed class InventoryLedgerViewTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase(false)] [TestCase(true)]
        public void OpenAndCloseOwnExactlyOneCameraBoundViewAndReturnTheSameWorld(bool flat)
        {
            using (var r = new Rig(flat))
            {
                Assert.IsEmpty(r.UI.GetComponentsInChildren<Canvas>(false));
                var item = r.Carry("HealingTonic");
                var origin = r.Zone.GetEntityCell(r.Player);
                Canvas first = null;
                for (int pass = 0; pass < 2; pass++)
                {
                    r.Open();
                    var canvas = RequireView(r);
                    if (first == null) first = canvas;
                    else Assert.AreSame(first, canvas, "Reopen reuses one retained view rather than leaking another painter.");
                    Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode);
                    Assert.AreSame(r.Camera, canvas.worldCamera);
                    Assert.AreEqual(0, canvas.gameObject.layer);
                    Assert.That(r.Camera.cullingMask & (1 << canvas.gameObject.layer), Is.Not.Zero);
                    Assert.Zero(r.PopupCamera.cullingMask & (1 << canvas.gameObject.layer));
                    Assert.Greater(canvas.planeDistance, r.Camera.nearClipPlane);
                    Assert.Less(canvas.planeDistance, r.Camera.farClipPlane);
                    Assert.True(r.Renderer.Paused);
                    Assert.AreEqual(new Rect(0, 0, 1, 1), r.Camera.rect);
                    foreach (string tab in new[] { "Equipment", "Inventory", "Tinkering", "Abilities", "Crafting" })
                        Assert.That(VisibleText(r), Does.Contain(tab.ToUpperInvariant()), "All five existing modes stay discoverable.");
                    Assert.Zero(r.Main.GetUsedTilesCount(), "Do not retain a duplicate legacy inventory behind the Canvas.");
                    Call(r.Renderer, "LateUpdate");
                    Assert.Zero(r.Main.GetUsedTilesCount(), "The first paused frame must not restore displaced world glyphs.");
                    r.UI.Close(); Call(r.Input, "CloseInventory");
                    Assert.False(canvas.isActiveAndEnabled);
                    Assert.False(r.Renderer.Paused);
                    Assert.AreEqual(flat ? 1.5f : 1f, r.Main.layoutGrid.cellSize.y);
                    Assert.AreSame(flat ? r.Main : null, r.Follow.WorldTilemap);
                    Assert.AreSame(origin, r.Zone.GetEntityCell(r.Player));
                    Assert.True(r.Pack.Contains(item));
                    Assert.IsNull(r.UI.ConsumePendingThrowRequest());
                    Assert.False(r.UI.ConsumePendingEverydayTurn());
                }
            }
        }

        [TestCase(1)] [TestCase(5)]
        public void PackQuantityAndWeightAreSeparateVisibleColumnsFromTheActualItem(int count)
        {
            using (var r = new Rig())
            {
                var food = r.Carry("DriedMeat", count);
                r.Open(); r.Select(food);
                var canvas = RequireView(r);
                var display = InventoryScreenData.Build(r.Player).Categories.SelectMany(c => c.Items).Single(i => i.Item == food);
                var labels = VisibleLabels(r);
                var quantityHeader = labels.Single(t => Normal(t.Content) == "QTY");
                var weightHeader = labels.Single(t => Normal(t.Content) == "WT" || Normal(t.Content) == "WEIGHT");
                Rect quantity = Bounds(canvas, quantityHeader), weight = Bounds(canvas, weightHeader);
                var name = labels.Where(t => Normal(t.Content.TrimStart('>', ' ')) == Normal(display.Name)
                    && Bounds(canvas, t).xMin < quantity.xMin).OrderByDescending(t => Bounds(canvas, t).center.y).FirstOrDefault();
                Assert.NotNull(name, "The real carried owner must have a visible list row, not only a detail title.");
                Rect row = Bounds(canvas, name);
                var quantityCell = labels.SingleOrDefault(t => Normal(t.Content) == display.StackCount.ToString()
                    && SameRow(canvas, name, t) && Bounds(canvas, t).center.x >= quantity.xMin
                    && Bounds(canvas, t).center.x < weight.xMin);
                var weightCell = labels.SingleOrDefault(t => (Normal(t.Content) == display.Weight.ToString()
                    || Normal(t.Content) == display.Weight + " LB") && SameRow(canvas, name, t)
                    && Bounds(canvas, t).center.x >= weight.xMin);
                Assert.NotNull(quantityCell, "Quantity must report stack units in its own column.");
                Assert.NotNull(weightCell, "Weight must use ScreenData's actual weight, never copy quantity.");
                Assert.LessOrEqual(row.xMax, Bounds(canvas, quantityCell).xMin);
                Assert.LessOrEqual(Bounds(canvas, quantityCell).xMax, Bounds(canvas, weightCell).xMin);
                AssertTextFits(name); AssertTextFits(quantityCell); AssertTextFits(weightCell);
                Assert.AreEqual(count, food.GetPart<StackerPart>().StackCount);
                Assert.True(r.Pack.Contains(food));
            }
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void SecondaryModesHaveTheirOwnRetainedContentWithoutLegacyTilePainting(int panel)
        {
            using (var r = new Rig())
            {
                var food = r.Carry("DriedMeat", 3);
                if (r.Player.GetPart<ActivatedAbilitiesPart>() == null) r.Player.AddPart(new ActivatedAbilitiesPart());
                r.Open(); Set(r.UI, "_panel", panel); Call(r.UI, "Render"); RequireView(r);
                string text = VisibleText(r);
                if (panel == 2)
                {
                    StringAssert.Contains("RECIPES", text);
                    StringAssert.Contains("COST", text);
                }
                else if (panel == 3)
                {
                    StringAssert.Contains("HOTBAR", text);
                    foreach (char key in "1234567890")
                        Assert.That(text.Contains("[" + key + "]") || text.Contains("SLOT " + key),
                            Is.True, "All ten actual hotbar slots need an identifiable row: " + key);
                }
                else
                {
                    StringAssert.Contains("FORGE", text);
                    StringAssert.Contains("BREW", text);
                    StringAssert.Contains("RESULT", text);
                }
                Assert.Zero(r.Main.GetUsedTilesCount(), "A secondary mode cannot secretly retain the old tile painter.");
                Assert.AreEqual(panel, Get(r.UI, "_panel"));
                Assert.AreEqual(3, food.GetPart<StackerPart>().StackCount);
                Assert.True(r.Pack.Contains(food));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void ActualBodyHandsAndEquippedOwnersRemainSeparateInTheEquipmentRail()
        {
            using (var r = new Rig())
            {
                var sword = r.Carry("LongSword"); var shield = r.Carry("Buckler");
                var hands = r.Player.GetPart<Body>().GetParts().Where(p => p.Type == "Hand").ToList();
                Assert.AreEqual(2, hands.Count, "Use the actual authored Player body.");
                Assert.True(r.Pack.EquipToBodyPart(sword, hands[0]));
                Assert.True(r.Pack.EquipToBodyPart(shield, hands[1]));
                r.Open(); RequireView(r);
                var slots = InventoryScreenData.Build(r.Player).Equipment.Where(s => s.EquippedItem == sword || s.EquippedItem == shield).ToList();
                Assert.AreEqual(2, slots.Count); Assert.AreNotEqual(slots[0].BodyPartName, slots[1].BodyPartName);
                string text = VisibleText(r);
                foreach (var slot in slots)
                {
                    StringAssert.Contains(slot.BodyPartName.ToUpperInvariant(), text);
                    StringAssert.Contains(slot.ItemName.ToUpperInvariant(), text);
                }
                Assert.AreSame(r.Player, sword.GetPart<PhysicsPart>().Equipped);
                Assert.AreSame(r.Player, shield.GetPart<PhysicsPart>().Equipped);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void SelectedDetailsShowTheFullActualNameAndRefreshItsExistingCombatFacts()
        {
            using (var r = new Rig())
            {
                var sword = r.Carry("LongSword"); r.Carry("Dagger");
                sword.GetPart<RenderPart>().DisplayName = "a weathered long sword with a carefully repaired grip";
                var weapon = sword.GetPart<MeleeWeaponPart>(); weapon.BaseDamage = "3d2+1";
                r.Open(); r.Select(sword); RequireView(r);
                string before = r.UI.SelectedDecisionDetails();
                StringAssert.Contains("3d2+1", before, "Precondition: the existing decision authority supplies this actual stat.");
                Assert.That(VisibleText(r), Does.Contain("3D2+1"));
                var title = VisibleLabels(r).FirstOrDefault(t => Normal(t.Content) == Normal(sword.GetDisplayName()));
                Assert.NotNull(title, "Full selected names belong in details even when a list row needs shortening.");
                AssertTextFits(title);
                weapon.BaseDamage = "1d4";
                Call(r.UI, "Rebuild"); Call(r.UI, "Render");
                StringAssert.Contains("1D4", VisibleText(r));
                StringAssert.DoesNotContain("3D2+1", VisibleText(r));
                Assert.AreSame(sword, Get(r.UI.SelectedDecisionPresentation(out var detailText), "Subject"));
                StringAssert.Contains("1d4", detailText);
                Assert.True(r.Pack.Contains(sword));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void DecisionReaderSuspendsTheViewThenReturnsToTheExactSelectedOwner()
        {
            using (var r = new Rig())
            {
                var sword = r.Carry("LongSword"); r.Carry("DriedMeat", 3);
                r.Open(); r.Select(sword); var canvas = RequireView(r);
                int row = (int)Get(r.UI, "_cursorIndex");
                string details = r.UI.SelectedDecisionDetails();
                Call(r.Input, "OpenInventoryDetailsReader");
                Assert.True(r.Reader.IsOpen); Assert.AreSame(sword, Get(r.Reader.Document, "Subject"));
                Assert.False(canvas.isActiveAndEnabled, "The decision reader explicitly suspends the inventory view.");
                r.Reader.Close(); Call(r.Input, "CloseAnnouncement");
                Assert.AreSame(canvas, RequireView(r));
                Assert.AreEqual(row, Get(r.UI, "_cursorIndex"));
                Assert.AreEqual(1, Get(r.UI, "_panel"));
                Assert.AreEqual(details, r.UI.SelectedDecisionDetails());
                Assert.False(r.PopupCamera.enabled); Assert.True(r.Renderer.Paused);
                Assert.Zero(r.Main.GetUsedTilesCount());
                Assert.True(r.Pack.Contains(sword));
            }
        }

        [Test]
        public void QueuedExamineKeepsTheSameInventoryBelowTheHigherDepthPopupCamera()
        {
            using (var r = new Rig())
            {
                var tonic = r.Carry("HealingTonic"); r.Open(); r.Select(tonic);
                var canvas = RequireView(r); string before = VisibleText(r);
                var document = ExamineDocumentBuilder.BuildInventory(tonic, r.Player);
                Assert.NotNull(document);
                MessageLog.AddPresentedAnnouncement(document.ToPlainText(), document);
                Assert.True((bool)Call(r.Input, "TryOpenAnnouncement"));
                Assert.True(canvas.isActiveAndEnabled); Assert.True(r.PopupCamera.enabled);
                Assert.Greater(r.PopupCamera.depth, r.Camera.depth);
                Assert.AreEqual(CameraClearFlags.Depth, r.PopupCamera.clearFlags);
                Assert.AreSame(document, r.Reader.Document);
                Assert.AreEqual(before, VisibleText(r));
                Assert.Zero(r.Main.GetUsedTilesCount());
                r.Reader.Close(); Call(r.Input, "CloseAnnouncement");
                Assert.AreSame(canvas, RequireView(r)); Assert.AreEqual(before, VisibleText(r));
                Assert.False(r.PopupCamera.enabled); Assert.True(r.Pack.Contains(tonic));
            }
        }

        [Test]
        public void F2StatusReaderPreservesEveryActualStatBeforeCurrentEffectsAndRefreshes()
        {
            using (var r = new Rig())
            {
                // Raw AV is not authoritative defense; the reader must retain the
                // same computed AV/DV as InventoryScreenData, plus arbitrary real stats.
                var armor = r.Player.GetPart<ArmorPart>();
                if (armor == null) { armor = new ArmorPart(); r.Player.AddPart(armor); }
                armor.AV = 3; armor.DV = 2;
                r.Player.Statistics["AV"] = new Stat { Owner = r.Player, Name = "AV", BaseValue = 99, Max = 999 };
                r.Player.Statistics["DV"] = new Stat { Owner = r.Player, Name = "DV", BaseValue = 1, Max = 999 };
                var extra = new Stat { Owner = r.Player, Name = "ZetaCraft", BaseValue = 42, Max = 999 };
                r.Player.Statistics[extra.Name] = extra;
                var effect = new HeartFlameEffect(2, 9);
                Assert.True(r.Player.ApplyEffect(effect));
                r.Open();
                for (int pass = 0; pass < 2; pass++)
                {
                    var expected = InventoryScreenData.Build(r.Player).PlayerStats;
                    Assert.AreEqual("3", expected.Single(stat => stat.Name == "AV").Value);
                    Assert.AreNotEqual(r.Player.Statistics["DV"].Value.ToString(), expected.Single(stat => stat.Name == "DV").Value);
                    Call(r.Input, "OpenPlayerStatusReader");
                    Assert.True(r.Reader.IsOpen, "Exercise the actual inventory F2 reader path.");
                    string text = (string)Get(r.Reader, "_message");
                    Assert.NotNull(text);
                    int effectsStart = text.IndexOf("Current effects", StringComparison.Ordinal);
                    Assert.GreaterOrEqual(effectsStart, 0, "Existing effect details remain available after the complete stat list.");
                    string statText = text.Substring(0, effectsStart);
                    foreach (var stat in expected)
                    {
                        string name = System.Text.RegularExpressions.Regex.Escape(stat.Name);
                        string label = System.Text.RegularExpressions.Regex.Escape(stat.Label);
                        string value = System.Text.RegularExpressions.Regex.Escape(stat.Value);
                        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(statText,
                            @"(?im)^\s*(?:" + name + "|" + label + @")\s*:\s*" + value + @"\s*$"),
                            "F2 must preserve the actual stat and paired value before effects: " + stat.Name + " = " + stat.Value);
                    }
                    StringAssert.Contains(EffectDescriber.Describe(effect), text);
                    Assert.AreEqual(pass == 0 ? 2 : 1, effect.ChargesRemaining);
                    Assert.AreEqual(9, effect.Duration);
                    Assert.False(r.UI.ConsumePendingEverydayTurn());
                    r.Reader.Close(); Call(r.Input, "CloseAnnouncement");
                    Assert.True(r.UI.IsOpen);
                    extra.BaseValue = 43; effect.ChargesRemaining = 1;
                }
            }
        }

        [TestCase("current")] [TestCase("closed")] [TestCase("changed-owner")]
        public void NativeCharacterStatusCommandCrossesInputHandlerAndReturnsToItsInventoryOwner(string context)
        {
            // Native device events drive the real inventory command menu. Private
            // dispatcher calls supply the EditMode frame boundary; no physical Deck claim.
            var inputScope = new InputTestFixture();
            var contextField = typeof(NativeGamepadInput).GetField("context", BindingFlags.Static | BindingFlags.NonPublic);
            var oldContext = (GamepadInputContext)contextField.GetValue(null);
            inputScope.Setup();
            try
            {
                var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                void Frame(GamepadState buttons, params Key[] keys)
                {
                    InputSystem.QueueStateEvent(pad, buttons);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                    InputSystem.Update(); InputHelper.GetKey(KeyCode.None);
                }
                Frame(new GamepadState()); NativeGamepadInput.SetContext(GamepadInputContext.Menu); Frame(new GamepadState());
                using (var r = new Rig())
                {
                    var sword = r.Carry("LongSword");
                    r.Player.Statistics["MettleAudit"] = new Stat { Owner = r.Player, Name = "MettleAudit", BaseValue = 42, Max = 99 };
                    var effect = new HeartFlameEffect(2, 9); Assert.True(r.Player.ApplyEffect(effect));
                    r.Open(); r.Select(sword); var canvas = RequireView(r);
                    int selected = (int)Get(r.UI, "_cursorIndex"); int tick = TurnManager.Active?.TickCount ?? 0;
                    var expected = InventoryScreenData.Build(r.Player).PlayerStats;
                    Frame(new GamepadState().WithButton(GamepadButton.West)); r.UI.HandleInput();
                    Assert.True((bool)Get(r.UI, "_ledgerCommandsOpen"));
                    var commands = (IList)Get(r.UI, "_ledgerCommands"); int command = -1;
                    for (int i = 0; i < commands.Count; i++) if ((string)Get(commands[i], "Label") == "Character status") command = i;
                    Assert.GreaterOrEqual(command, 0, "Discover the real offered command rather than calling its callback.");
                    for (int i = 0; i < command; i++)
                    {
                        Frame(new GamepadState()); Frame(new GamepadState().WithButton(GamepadButton.DpadDown)); r.UI.HandleInput();
                    }
                    Assert.AreEqual(command, Get(r.UI, "_ledgerCommandIndex"));
                    Frame(new GamepadState()); Frame(new GamepadState().WithButton(GamepadButton.South)); r.UI.HandleInput();
                    Assert.True((bool)Get(r.UI, "_ledgerStatusRequested"));
                    Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen")); Assert.False(r.Reader.IsOpen);
                    Frame(new GamepadState());
                    if (context == "closed") r.UI.Close();
                    else if (context == "changed-owner") r.Input.PlayerEntity = r.Factory.CreateEntity("Player");
                    Call(r.Input, "HandleInventoryInput");
                    Assert.False(r.UI.ConsumeStatusRequest(), "The pending request is consumed once or cleared on close.");
                    if (context != "current")
                    {
                        Assert.False(r.Reader.IsOpen); Assert.False(MessageLog.HasPendingAnnouncement);
                        Assert.IsNull(Get(r.Input, "_decisionReaderActor"));
                        r.Input.PlayerEntity = r.Player;
                        if (!r.UI.IsOpen) { r.Open(); r.Select(sword); }
                        Call(r.Input, "HandleInventoryInput");
                        Assert.False(r.Reader.IsOpen, "Repairing context cannot resurrect an invalidated request.");
                    }
                    else
                    {
                        Assert.True(r.Reader.IsOpen); Assert.True(r.PopupCamera.enabled);
                        Assert.False(canvas.isActiveAndEnabled, "The same F1/F2 reader suspension must hide the old inventory canvas.");
                        Assert.IsEmpty(VisibleLabels(r)); Assert.Zero(r.Main.GetUsedTilesCount());
                        string text = (string)Get(r.Reader, "_message");
                        int effectsStart = text.IndexOf("Current effects", StringComparison.Ordinal); Assert.GreaterOrEqual(effectsStart, 0);
                        string stats = text.Substring(0, effectsStart);
                        foreach (var stat in expected)
                        {
                            string name = System.Text.RegularExpressions.Regex.Escape(stat.Name ?? stat.Label);
                            string value = System.Text.RegularExpressions.Regex.Escape(stat.Value);
                            Assert.True(System.Text.RegularExpressions.Regex.IsMatch(stats, @"(?m)^\s*" + name + @"\s*:\s*" + value + @"\s*$"),
                                "Retain the complete current stat/value pair: " + stat.Name + " = " + stat.Value);
                        }
                        StringAssert.Contains(EffectDescriber.Describe(effect), text);
                        Frame(new GamepadState(), Key.Escape); Call(r.Input, "HandleAnnouncementInput"); Frame(new GamepadState());
                        Assert.False(r.Reader.IsOpen); Assert.False(r.PopupCamera.enabled); Assert.AreSame(canvas, RequireView(r));
                        Assert.AreEqual("InventoryOpen", Get(r.Input, "_inputState").ToString());
                        Assert.AreEqual(1, Get(r.UI, "_panel")); Assert.AreEqual(selected, Get(r.UI, "_cursorIndex"));
                        Assert.AreSame(sword, Get(r.UI.SelectedDecisionPresentation(out _), "Subject"));
                        Assert.True(r.Renderer.Paused); Assert.Zero(r.Main.GetUsedTilesCount());
                    }
                    Assert.True(r.Pack.Contains(sword)); Assert.AreEqual(2, effect.ChargesRemaining); Assert.AreEqual(9, effect.Duration);
                    Assert.AreEqual(tick, TurnManager.Active?.TickCount ?? 0); Assert.False(r.UI.ConsumePendingEverydayTurn());
                    Assert.IsNull(r.UI.ConsumePendingThrowRequest());
                }
            }
            finally { NativeGamepadInput.SetContext(oldContext); inputScope.TearDown(); }
        }

        static Canvas RequireView(Rig r)
        {
            Canvas.ForceUpdateCanvases();
            var canvases = r.UI.GetComponentsInChildren<Canvas>(true).Where(c => c.isActiveAndEnabled).ToArray();
            Assert.AreEqual(1, canvases.Length, "An open inventory must own one visible retained ledger Canvas.");
            Assert.IsNotEmpty(VisibleLabels(r), "A blank Canvas is not an inventory view.");
            return canvases[0];
        }
        static List<SkillScreenText> VisibleLabels(Rig r) => r.UI.GetComponentsInChildren<SkillScreenText>(true)
            .Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.Content)).ToList();
        static string VisibleText(Rig r) => Normal(string.Join("\n", VisibleLabels(r).Select(t => t.Content)));
        static string Normal(string value) => string.Join(" ", (value ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        static Rect Bounds(Canvas canvas, SkillScreenText label)
        {
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, label.rectTransform);
            return Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
        }
        static bool SameRow(Canvas c, SkillScreenText a, SkillScreenText b)
            => Mathf.Abs(Bounds(c, a).yMax - Bounds(c, b).yMax) < 1f;
        static void AssertTextFits(SkillScreenText label)
        {
            string[] lines = label.Content.Split('\n');
            Assert.Greater(label.GlyphHeight, 0f);
            Assert.LessOrEqual(lines.Max(line => line.Length) * label.GlyphHeight * .5f, label.rectTransform.rect.width + .1f,
                "The text component must not silently clip the asserted content.");
            Assert.LessOrEqual(lines.Length * label.GlyphHeight + (lines.Length - 1) * 4f, label.rectTransform.rect.height + .1f);
        }
        static object Get(object owner, string field) => owner.GetType().GetField(field, Members)?.GetValue(owner) ?? owner.GetType().GetProperty(field, Members)?.GetValue(owner);
        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Members).SetValue(owner, value);
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Members).Invoke(owner, args);

        sealed class Rig : IDisposable
        {
            readonly GameObject root = new GameObject("Inventory ledger test");
            readonly List<GameObject> detached = new List<GameObject>();
            readonly List<Camera> borrowedCameras = new List<Camera>();
            readonly List<MessageLog.Entry> oldLog = MessageLog.GetAllEntries();
            readonly List<string> oldAnnouncements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int oldFlash = MessageLog.FlashStamp, oldSerial = MessageLog.NextSerialValue;
            readonly int oldEquipmentVersion = EquipmentChangeBus.GlobalVersion;
            readonly long oldRevision = MessageLog.Revision, oldGeneration = MessageLog.HistoryGeneration;
            readonly Action<string> oldMessage = MessageLog.OnMessage;
            readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly object origins = typeof(MessageLog).GetField("AnnouncementOrigins", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            readonly Array oldOrigins;
            readonly FieldInfo boundRenderZone = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBoundRenderZone;
            readonly Zone previousBoundZone;
            readonly Action<int, int> previousCellChanged;
            readonly Action previousSightChanged;
            readonly Action<int, int, string> oldCellDirty = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFullDirty = ZoneRenderHooks.FullDirtyCallback;
            readonly RenderTexture display;
            public readonly Camera Camera, PopupCamera;
            public readonly CameraFollow Follow;
            public readonly Tilemap Main;
            public readonly ZoneRenderer Renderer;
            public readonly InventoryUI UI;
            public readonly AnnouncementUI Reader;
            public readonly InputHandler Input;
            public readonly EntityFactory Factory;
            public readonly Entity Player;
            public readonly Zone Zone;
            public InventoryPart Pack => Player.GetPart<InventoryPart>();
            public Rig(bool flat = true)
            {
                oldBoundRenderZone = boundRenderZone.GetValue(null);
                if (oldBoundRenderZone is WeakReference<Zone> weak && weak.TryGetTarget(out previousBoundZone))
                {
                    previousCellChanged = previousBoundZone.TileState.OnCellChanged;
                    previousSightChanged = previousBoundZone.TileState.OnSightChanged;
                }
                oldOrigins = (Array)origins.GetType().GetMethod("ToArray").Invoke(origins, null);
                MessageLog.OnMessage = null; MessageLog.TickProvider = null; MessageLog.Clear();
                foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (camera.CompareTag("MainCamera")) { borrowedCameras.Add(camera); camera.tag = "Untagged"; }
                var cameraHost = Child("Main camera"); cameraHost.tag = "MainCamera";
                Camera = cameraHost.AddComponent<Camera>(); Camera.orthographic = true;
                Camera.depth = -1; Camera.cullingMask = ((1 << 8) | (1 << 0));
                display = new RenderTexture(1280, 800, 0); Camera.targetTexture = display; Camera.aspect = 1.6f;
                Camera.transform.position = new Vector3(40, 22.5f, -10);
                PopupCamera = Child("Popup camera").AddComponent<Camera>(); PopupCamera.orthographic = true;
                PopupCamera.depth = 0; PopupCamera.cullingMask = (1 << 10); PopupCamera.enabled = false;
                var grid = Child("World grid"); grid.AddComponent<Grid>();
                var world = Child("World tilemap"); world.transform.SetParent(grid.transform, false);
                Main = world.AddComponent<Tilemap>(); world.AddComponent<TilemapRenderer>();
                Renderer = world.AddComponent<ZoneRenderer>();
                if (Renderer.CenteredPopupBgTilemap == null) Call(Renderer, "Awake");
                foreach (string field in new[] { "_fineWaterGridTransform", "_sidebarGridTransform", "_hotbarGridTransform", "_popupOverlayGridTransform" })
                { var t = Get(Renderer, field) as Transform; if (t != null) detached.Add(t.gameObject); }
                Renderer.SetPopupOverlayCamera(PopupCamera);
                Factory = new EntityFactory(); Factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                Player = Factory.CreateEntity("Player"); Zone = new Zone(ReferenceGladePlan.ZoneID);
                Assert.True(Zone.AddEntity(Player, 10, 10));
                Zone.GetCell(10, 10).Explored = Zone.GetCell(10, 10).IsVisible = true;
                Renderer.PlayerEntity = Player; Renderer.SetZone(Zone);
                Follow = cameraHost.AddComponent<CameraFollow>(); Follow.Player = Player; Follow.CurrentZone = Zone;
                Follow.PopupOverlayCamera = PopupCamera;
                if (flat) { Renderer.BindFlatPixelCamera(Follow); Renderer.FlatPixelModeEnabled = true; }
                Follow.SnapToPlayer();
                var controls = Child("UI controls");
                UI = controls.AddComponent<InventoryUI>(); UI.Tilemap = Main; UI.EntityFactory = Factory;
                Reader = controls.AddComponent<AnnouncementUI>(); Reader.Tilemap = Renderer.CenteredPopupFgTilemap;
                Reader.BgTilemap = Renderer.CenteredPopupBgTilemap; Reader.PopupCamera = PopupCamera;
                Input = controls.AddComponent<InputHandler>(); Input.PlayerEntity = Player; Input.CurrentZone = Zone;
                Input.ZoneRenderer = Renderer; Input.CameraFollow = Follow; Input.InventoryUI = UI; Input.AnnouncementUI = Reader;
            }
            public Entity Carry(string blueprint, int count = 1)
            {
                var item = Factory.CreateEntity(blueprint); Assert.NotNull(item);
                item.GetPart<StackerPart>().StackCount = count;
                Assert.True(Pack.AddObject(item)); return item;
            }
            public void Open() => Call(Input, "OpenInventory");
            public void Select(Entity item)
            {
                Set(UI, "_panel", 1); Call(UI, "Rebuild");
                var rows = (IList)Get(UI, "_rows"); int selected = -1;
                for (int i = 0; i < rows.Count; i++)
                    if (Get(rows[i], "Item") is InventoryScreenData.ItemDisplay displayItem && displayItem.Item == item) selected = i;
                Assert.GreaterOrEqual(selected, 0); Set(UI, "_cursorIndex", selected); Call(UI, "Render");
            }
            GameObject Child(string name) { var child = new GameObject(name); child.transform.SetParent(root.transform, false); return child; }
            public void Dispose()
            {
                Reader.Close(); UI.Close(); Camera.targetTexture = null;
                Object.DestroyImmediate(root);
                foreach (var host in detached) if (host != null) Object.DestroyImmediate(host);
                Object.DestroyImmediate(display);
                ZoneRenderHooks.CellDirtyCallback = oldCellDirty; ZoneRenderHooks.FullDirtyCallback = oldFullDirty;
                boundRenderZone.SetValue(null, oldBoundRenderZone);
                if (previousBoundZone != null)
                {
                    previousBoundZone.TileState.OnCellChanged = previousCellChanged;
                    previousBoundZone.TileState.OnSightChanged = previousSightChanged;
                }
                MessageLog.Restore(oldLog, oldAnnouncements, oldFlash, oldSerial);
                origins.GetType().GetMethod("Clear").Invoke(origins, null);
                foreach (object origin in oldOrigins) origins.GetType().GetMethod("Enqueue").Invoke(origins, new[] { origin });
                typeof(MessageLog).GetProperty("Revision").SetValue(null, oldRevision);
                typeof(MessageLog).GetProperty("HistoryGeneration").SetValue(null, oldGeneration);
                MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, oldEquipmentVersion);
                foreach (var camera in borrowedCameras) if (camera != null) camera.tag = "MainCamera";
            }
        }
    }
}
