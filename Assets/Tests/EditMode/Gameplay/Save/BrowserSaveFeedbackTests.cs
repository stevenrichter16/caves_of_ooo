using System.Collections.Generic;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class BrowserSaveFeedbackTests
    {
        private sealed class Probe : IInputProbe
        {
            public KeyCode Pressed;
            public bool GetKeyDown(KeyCode key) => key == Pressed;
        }
        private sealed class Service : ISaveLoadService, ISaveLoadStatus
        {
            public bool IsAsynchronous => true;
            public bool IsBusy { get; set; }
            public int Loads, Starts;
            public bool BeginNewGame() { Starts++; IsBusy = true; return true; }
            public bool QuickSave() { IsBusy = true; return true; }
            public bool QuickLoad() { Loads++; return true; }
            public bool HasQuickSave() => true;
        }
        [Test]
        public void SaveAcceptanceSaysSaving_AndPendingLoadDoesNotEnterGameplay()
        {
            var service = new Service(); var messages = new List<string>();
            var controller = new SaveLoadInputController();
            controller.Tick(new Probe { Pressed = KeyCode.F5 }, service, messages.Add);
            Assert.That(messages[0], Is.EqualTo("Saving…"));
            controller.Tick(new Probe { Pressed = KeyCode.F6 }, service, messages.Add);
            Assert.That(service.Loads, Is.Zero);
            Assert.That(messages[1], Does.Contain("storage"));
        }
        [Test]
        public void BusyBootChoiceStaysOpenAndDoesNotStartAnotherGame()
        {
            var service = new Service { IsBusy = true }; var messages = new List<string>();
            var menu = new BootMenuController(); menu.TryActivate(true, messages.Add);
            menu.Tick(new Probe { Pressed = KeyCode.N }, service, messages.Add);
            Assert.That(menu.IsActive, Is.True); Assert.That(service.Starts, Is.Zero);
            service.IsBusy = false;
            menu.Tick(new Probe { Pressed = KeyCode.N }, service, messages.Add);
            Assert.That(menu.IsActive, Is.False);
            Assert.That(messages[messages.Count - 1], Does.Contain("Saving"));
        }
    }
}
