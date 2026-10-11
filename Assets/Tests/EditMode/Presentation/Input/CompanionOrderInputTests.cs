using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class CompanionOrderInputTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly InputTestFixture devices = new InputTestFixture();
        HotbarSaveFixture scope;
        InputHandler input;
        WorldActionMenuUI menu;
        Gamepad pad;
        Entity player, follower;
        Zone zone;
        [SetUp] public void SetUp()
        {
            devices.Setup(); scope = new HotbarSaveFixture(true, false);
            input = scope.Input; player = input.PlayerEntity; zone = input.CurrentZone;
            player.SetTag("Creature"); player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new BrainPart { CurrentZone = zone }); player.AddPart(new RenderPart { DisplayName = "you" });
            follower = new Entity { ID = "menu-companion", BlueprintName = "Companion" };
            follower.SetTag("Creature"); follower.AddPart(new PhysicsPart { Solid = true });
            follower.AddPart(new RenderPart { DisplayName = "companion" });
            follower.AddPart(new BrainPart { CurrentZone = zone }); follower.AddPart(new StatusEffectsPart());
            follower.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Max = 20 };
            Assert.True(zone.AddEntity(follower, 4, 4));
            Assert.True(follower.ApplyEffect(new RecruitedEffect(player), player, zone));
            zone.GetCell(4, 4).IsVisible = zone.GetCell(4, 4).Explored = true;
            menu = scope.Root.AddComponent<WorldActionMenuUI>(); input.WorldActionMenuUI = menu;
            pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent(); State(new GamepadState());
        }
        [TearDown] public void TearDown() { scope?.Dispose(); devices.TearDown(); }
        void State(GamepadState state)
        { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); Call("Update"); }
        object Call(string method, params object[] args) => typeof(InputHandler).GetMethod(method, Private).Invoke(input, args);
        void Open(bool controller)
        {
            State(new GamepadState());
            if (controller) State(new GamepadState { leftStick = Vector2.right, leftTrigger = 1 }.WithButton(GamepadButton.South));
            else Call("InteractInDirection", 1, 0);
            Assert.True(menu.IsOpen); Assert.AreSame(follower, menu.SelectedTarget);
            State(new GamepadState());
        }
        void Confirm()
        { State(new GamepadState().WithButton(GamepadButton.South)); State(new GamepadState()); }
        [TestCase(false)] [TestCase(true)]
        public void OrdinaryDirectionMenuAndNativeConfirmToggleStayWithoutSpendingWorldTime(bool controller)
        {
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(player);
            Open(controller); Assert.AreEqual("stay here", menu.HighlightedAction.Display); Confirm();
            Assert.True(CompanionOrders.IsStaying(follower));
            Open(controller); Assert.AreEqual("follow me", menu.HighlightedAction.Display); Confirm();
            Assert.False(CompanionOrders.IsStaying(follower));
            Assert.AreSame(player, follower.GetPart<BrainPart>().PartyLeader);
            Assert.AreEqual(tick, input.TurnManager.TickCount); Assert.AreEqual(energy, input.TurnManager.GetEnergy(player));
        }
        [Test] public void ControllerCancellationAndStaleOpenChoiceDoNotApplyAnOrder()
        {
            int tick = input.TurnManager.TickCount;
            Open(true); State(new GamepadState().WithButton(GamepadButton.East)); State(new GamepadState());
            Assert.False(CompanionOrders.IsStaying(follower));
            Open(true); follower.GetEffect<RecruitedEffect>().Dismiss(player); Confirm();
            Assert.False(CompanionOrders.IsStaying(follower)); Assert.Null(follower.GetEffect<RecruitedEffect>());
            Assert.AreEqual(tick, input.TurnManager.TickCount);
        }
    }
}
