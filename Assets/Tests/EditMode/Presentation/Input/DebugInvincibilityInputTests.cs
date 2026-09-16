using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Tests
{
    public class DebugInvincibilityInputTests:LookKeyFixture
    {
        [Test] public void ActualF12TogglesOnAndOffWithoutTurnOrMovementEvenWithoutDevMode()
        {
            bool old=DevMode.Enabled;DevMode.Enabled=false;
            try{var input=Input();int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(Player);Press(Key.F12,()=>Tick(input));
                Assert.IsTrue(DebugInvincibility.IsEnabled(Player));Assert.That(MessageLog.GetLast(),Does.Contain("invincibility ON"));AssertHeld(Key.F12);Tick(input);Assert.IsTrue(DebugInvincibility.IsEnabled(Player));
                Press(Key.F12,()=>Tick(input));Assert.IsFalse(DebugInvincibility.IsEnabled(Player));Assert.That(MessageLog.GetLast(),Does.Contain("invincibility OFF"));
                Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(Player));Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));}
            finally{DevMode.Enabled=old;}
        }
        [TestCase(Key.Period)][TestCase(Key.RightArrow)]
        public void FreshToggleWinsHeldGameplayActionAndRateLimit(Key other)
        {
            var input=Input();input.MoveRepeatDelay=100;Set(input,"_lastMoveTime",Time.time);int tick=input.TurnManager.TickCount;
            Keys(other,Key.F12);Tick(input);Assert.IsTrue(DebugInvincibility.IsEnabled(Player));Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual((10,10),Zone.GetEntityPosition(Player));
        }
        [Test] public void LookModeRetainsKeyboardOwnership()
        {var input=Input();Press(Key.L,()=>Tick(input));Assert.AreEqual("LookMode",Mode(input));Press(Key.F12,()=>Tick(input));Assert.IsFalse(DebugInvincibility.IsEnabled(Player));Assert.AreEqual("LookMode",Mode(input));}
        [Test] public void BootMenuRetainsKeyboardOwnership()
        {var input=Input();Assert.IsTrue(input.TryActivateBootMenu(true));Press(Key.F12,()=>Tick(input));Assert.IsFalse(DebugInvincibility.IsEnabled(Player));}
        [Test] public void HelpAdvertisesActualDebugBinding()
        {Assert.IsTrue(ControlsReference.Bindings.Any(r=>r.Key=="F12"&&r.What.Contains("invincibility")));}
    }
}
