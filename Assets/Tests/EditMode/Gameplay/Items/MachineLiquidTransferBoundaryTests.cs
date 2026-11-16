using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class MachineLiquidTransferBoundaryTests : FiftyWorldFixture
    {
        string Transfer(Entity from, Entity to) => Choice(from, "TransferLiquid|", a => a.Command.Split('|')[2] == Id(to));
        [TestCase("brine", 6)] [TestCase("water", 2)] [TestCase("", 0)]
        public void RealInventoryDecisionReaderDisclosesCurrentVesselContents(string liquid, int units)
        {
            var inputObject = new GameObject("reader input"); inputObject.SetActive(false);
            var uiObject = new GameObject("reader UI"); uiObject.SetActive(false);
            try
            {
                var input = inputObject.AddComponent<InputHandler>(); var ui = uiObject.AddComponent<InventoryUI>();
                input.PlayerEntity = Actor; input.CurrentZone = Zone; input.InventoryUI = ui;
                ui.PlayerEntity = Actor; ui.CurrentZone = Zone;
                var flask = Flask(units, liquid);
                typeof(InputHandler).GetMethod("OpenInventory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, null);
                Assert.True(ui.ReopenItemActionPopupFor(flask));
                StringAssert.Contains("Contents: " + (units == 0 ? "empty" : liquid) + " (" + units + "/12 volume)", ui.SelectedDecisionDetails());
                Assert.NotNull(ui.SelectedDecisionPresentation(out var reader));
                StringAssert.Contains("Contents: ", reader);
                StringAssert.Contains("Transfer non-water liquid directly", reader);
            }
            finally { UnityEngine.Object.DestroyImmediate(uiObject); UnityEngine.Object.DestroyImmediate(inputObject); }
        }
        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void MalformedOptionalStackMetadataCannotTransfer(bool source, bool duplicate)
        {
            var from = Flask(6, "brine"); var to = Flask(0); string command = Transfer(from, to);
            var owner = source ? from : to;
            owner.AddPart(new StackerPart { StackCount = 1, MaxStack = duplicate ? 5 : 0 });
            if (duplicate) owner.AddPart(new StackerPart { StackCount = 2, MaxStack = 5 });
            Assert.False(Act(from, command)); Assert.AreEqual(6, Units(from)); Assert.AreEqual(0, Units(to));
        }
        [Test] public void ValidSingleStackMetadataStillAllowsTransfer()
        {
            var from = Flask(6, "brine"); var to = Flask(0);
            from.AddPart(new StackerPart { StackCount = 1, MaxStack = 5 });
            to.AddPart(new StackerPart { StackCount = 1, MaxStack = 1 });
            Assert.True(Act(from, Transfer(from, to))); Assert.AreEqual(6, Units(to));
        }
        [TestCase("self")] [TestCase("skin")] [TestCase("stack")] [TestCase("dual-part")]
        [TestCase("dead")] [TestCase("stunned")] [TestCase("truncated")] [TestCase("negative")]
        public void ForgedCommandsCannotBypassTheMenu(string change)
        {
            var from = Flask(6, "brine"); var to = Flask(0); string command = Transfer(from, to);
            var bits = command.Split('|');
            if (change == "self") bits[2] = Id(from);
            if (change == "skin") bits[2] = Id(Skin());
            if (change == "stack") to.AddPart(new StackerPart { StackCount = 2, MaxStack = 5 });
            if (change == "dual-part") to.AddPart(new WaterskinPart());
            if (change == "dead") Actor.GetStat("Hitpoints").BaseValue = 0;
            if (change == "stunned") Actor.ApplyEffect(new StunnedEffect(3));
            if (change == "negative") bits[4] = "-6";
            command = change == "truncated" ? "TransferLiquid|" : string.Join("|", bits);
            Assert.False(Act(from, command)); Assert.AreEqual(6, Units(from)); Assert.AreEqual(0, Units(to));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualKeyboardInventoryTransferCostsOneTurnOnlyOnCommit(bool stale)
        {
            var objects = new List<GameObject>(); var keys = new InputTestFixture(); keys.Setup();
            try
            {
                T Make<T>() where T : Component
                { var o = new GameObject(typeof(T).Name); o.SetActive(false); objects.Add(o); return o.AddComponent<T>(); }
                var input = Make<InputHandler>(); input.PlayerEntity = Actor; input.CurrentZone = Zone; input.TurnManager = Clock;
                input.InventoryUI = Make<InventoryUI>(); input.InventoryUI.PlayerEntity = Actor; input.InventoryUI.CurrentZone = Zone;
                Clock.AddEntity(Actor); Clock.ProcessUntilPlayerTurn();
                var source = Flask(6, "brine"); var to = Flask(0); string command = Transfer(source, to);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                void Call(string name) => typeof(InputHandler).GetMethod(name, flags).Invoke(input, null);
                Call("OpenInventory"); Assert.True(input.InventoryUI.ReopenItemActionPopupFor(source));
                var popup = typeof(InventoryUI).GetField("_itemActionPopup", flags).GetValue(input.InventoryUI);
                var choices = (IList)popup.GetType().GetField("Actions").GetValue(popup); int selection = -1;
                for (int i = 0; i < choices.Count; i++)
                    if ((string)choices[i].GetType().GetField("Command").GetValue(choices[i]) == command) selection = i;
                Assert.GreaterOrEqual(selection, 0); popup.GetType().GetField("CursorIndex").SetValue(popup, selection);
                Assert.AreEqual(1, selection, "Safe transfer should be visible immediately after Examine, before ground spills.");
                if (stale) Pack.RemoveObject(to);
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); int tick = Clock.TickCount;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update(); Call("HandleInventoryInput");
                Assert.AreEqual(stale ? 6 : 0, Units(source)); Assert.AreEqual(stale ? 0 : 6, Units(to));
                Assert.AreEqual(stale ? tick : tick + 10, Clock.TickCount);
                Assert.AreEqual(stale ? "InventoryOpen" : "Normal", typeof(InputHandler).GetField("_inputState", flags).GetValue(input).ToString());
                Assert.False(input.InventoryUI.ConsumePendingEverydayTurn());
            }
            finally { foreach (var o in objects) UnityEngine.Object.DestroyImmediate(o); keys.TearDown(); }
        }
    }
}
