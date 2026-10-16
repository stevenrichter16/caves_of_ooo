using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ThrowSourcePopupVisibilityTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject host, cameraHost, popupHost;
        InputHandler input;
        AbilityManagerUI manager;
        Camera popup;

        [SetUp] public void SetUpSourceVisibility()
        {
            cameraHost = new GameObject("throw-source-main-camera");
            var main = cameraHost.AddComponent<Camera>(); main.orthographic = true; main.aspect = 16f / 9f;
            popupHost = new GameObject("throw-source-popup-camera");
            popup = popupHost.AddComponent<Camera>(); popup.orthographic = true;
            var follow = cameraHost.AddComponent<CameraFollow>();
            follow.Player = Actor; follow.CurrentZone = Zone; follow.PopupOverlayCamera = popup;
            follow.SnapToPlayer();
            host = new GameObject("throw-source-menu-owner");
            manager = host.AddComponent<AbilityManagerUI>(); input = host.AddComponent<InputHandler>();
            input.PlayerEntity = Actor; input.CurrentZone = Zone; input.TurnManager = Clock;
            input.CameraFollow = follow; input.AbilityManagerUI = manager;
            Actor.GetPart<SkillsPart>().AddSkill(new Throwing_MeasuredThrow());
            Carry("Dagger"); Clock.AddEntity(Actor); Clock.ProcessUntilPlayerTurn();
        }

        [TearDown] public void CleanSourceVisibility()
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(cameraHost);
            UnityEngine.Object.DestroyImmediate(popupHost);
        }

        object Call(string name, params object[] args) => typeof(InputHandler).GetMethod(name, Flags).Invoke(input, args);
        string State => typeof(InputHandler).GetField("_inputState", Flags).GetValue(input).ToString();

        [Test] public void ManagerActivationKeepsNewThrowSourceOverlayVisibleAfterManagerClose()
        {
            int tick = Clock.TickCount, energy = Clock.GetEnergy(Actor);
            Call("OpenAbilityManager"); Assert.True(popup.enabled, "the actual manager opens the centered camera");
            var skill = Actor.GetPart<SkillsPart>().GetSkill("Throwing_MeasuredThrow");
            var rows = AbilityManagerStateBuilder.Build(Actor).Rows;
            int index = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].AbilityID == skill.ActivatedAbilityID) index = i;
            Assert.GreaterOrEqual(index, 0);
            typeof(AbilityManagerUI).GetField("_cursorIndex", Flags).SetValue(manager, index);
            // This is the production callback order in HandleAbilityManagerInput:
            // Close the old menu, activate its selected skill, then finish closure.
            typeof(AbilityManagerUI).GetMethod("ActivateSelected", Flags).Invoke(manager, null);
            Assert.False(manager.IsOpen); Assert.AreEqual("ThrowPopupOpen", State); Assert.True(popup.enabled);
            Call("CloseAbilityManager");
            Assert.AreEqual("ThrowPopupOpen", State);
            Assert.True(popup.enabled, "The manager must not turn off the replacement source menu's camera.");
            Assert.AreEqual(tick, Clock.TickCount); Assert.AreEqual(energy, Clock.GetEnergy(Actor));
            Assert.False(ActionResolutionGuard.IsUnstable);
            Call("CloseThrowPopup"); Assert.AreEqual("Normal", State); Assert.False(popup.enabled);
        }

        [Test] public void ClosingManagerWithoutActivationRestoresGameplayCameraForFree()
        {
            int tick = Clock.TickCount, energy = Clock.GetEnergy(Actor);
            Call("OpenAbilityManager"); Assert.True(popup.enabled);
            manager.Close(); Call("CloseAbilityManager");
            Assert.AreEqual("Normal", State); Assert.False(popup.enabled);
            Assert.AreEqual(tick, Clock.TickCount); Assert.AreEqual(energy, Clock.GetEnergy(Actor));
        }

        [Test] public void DirectionalActivationStillRestoresGameplayCameraWithoutResettingTargetState()
        {
            Call("OpenAbilityManager"); Assert.True(popup.enabled); manager.Close();
            var state = typeof(InputHandler).GetField("_inputState", Flags);
            state.SetValue(input, Enum.Parse(state.FieldType, "AwaitingDirection"));
            Call("CloseAbilityManager");
            Assert.AreEqual("AwaitingDirection", State); Assert.False(popup.enabled);
        }
    }
}
