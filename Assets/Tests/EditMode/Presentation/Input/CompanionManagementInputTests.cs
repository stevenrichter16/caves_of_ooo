using System;
using System.Reflection;
using System.Linq;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class CompanionManagementInputTests
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
            player.AddPart(new InventoryPart()); follower.AddPart(new InventoryPart());
            var body = new Body(); body.SetBody(AnatomyFactory.CreateHumanoid()); follower.AddPart(body);
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
        {
            InputSystem.QueueStateEvent(pad, state); InputSystem.Update();
            // EditMode state updates do not advance Time.time. Each synthetic
            // gesture represents a later input opportunity, as in the native
            // controller gameplay fixture; preserve the production rate gate.
            typeof(InputHandler).GetField("_lastMoveTime", Private).SetValue(input, -999f);
            Call("Update");
        }
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
        {
            State(new GamepadState().WithButton(GamepadButton.South));
            Assert.False(menu.IsOpen, "Native A must execute and close the actual menu.");
            State(new GamepadState());
        }
        Entity Carry(Entity owner, bool gear = false)
        {
            var item = new Entity { ID = "menu-supplies", BlueprintName = "MenuSupplies" };
            item.AddPart(new RenderPart { DisplayName = "supplies" }); item.AddPart(new PhysicsPart { Takeable = true, Weight = 2 });
            if (gear) item.AddPart(new EquippablePart { Slot = "Hand" });
            Assert.True(owner.GetPart<InventoryPart>().AddObject(item)); return item;
        }
        void Select(string prefix)
        {
            var row = WorldInteractionSystem.GatherActions(follower, player).FirstOrDefault(a => a.Command.StartsWith(prefix, StringComparison.Ordinal));
            Assert.NotNull(row, "A current real companion menu choice must exist."); menu.RestoreHighlight(row); Assert.AreEqual(row.Command, menu.HighlightedAction.Command);
        }
        void AssertCost(int tick,int energy,int speed,int actions)
        {
            Assert.AreEqual(speed,input.TurnManager.GetSpeed(player));
            long spent=(long)(input.TurnManager.TickCount-tick)*speed+energy-input.TurnManager.GetEnergy(player);
            Assert.AreEqual((long)actions*TurnManager.ActionThreshold,spent,"Exact energy accounting must show one committed action, not one scheduler tick.");
        }
        [TestCase(false)] [TestCase(true)] public void NearbyPackGiveUsesNativeConfirmationAndOnePaidAction(bool controller)
        {
            var item = Carry(player); int tick=input.TurnManager.TickCount, energy=input.TurnManager.GetEnergy(player), speed=input.TurnManager.GetSpeed(player);
            Open(controller); Select("CompanionGive|"); Confirm();
            Assert.Contains(item,follower.GetPart<InventoryPart>().Objects); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(item));
            AssertCost(tick,energy,speed,1);
        }
        [Test] public void PackReaderIsFreeAndStaleTransferIsFree()
        {
            var item=Carry(player); int tick=input.TurnManager.TickCount, energy=input.TurnManager.GetEnergy(player), speed=input.TurnManager.GetSpeed(player);
            Open(true); Select("CompanionPack"); Confirm(); Assert.AreEqual(tick,input.TurnManager.TickCount);
            Assert.True(MessageLog.HasPendingAnnouncement); MessageLog.ConsumeAnnouncement();
            Open(true); Select("CompanionGive|"); follower.GetEffect<RecruitedEffect>().Dismiss(player); Confirm();
            Assert.Contains(item,player.GetPart<InventoryPart>().Objects); Assert.AreEqual(tick,input.TurnManager.TickCount);
        }
        [Test] public void DeliberateGearSelectionUsesActualSlotAndPaysOnce()
        {
            var item=Carry(follower,true); int tick=input.TurnManager.TickCount, energy=input.TurnManager.GetEnergy(player), speed=input.TurnManager.GetSpeed(player);
            Open(true); Select("CompanionEquip|"); Confirm();
            Assert.True(InventorySystem.IsEquipped(follower,item)); AssertCost(tick,energy,speed,1);
        }
    }
}
