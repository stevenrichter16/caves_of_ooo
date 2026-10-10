using System.Collections.Generic;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    /// <summary>Player guidance must teach the paid-step boundary and reachable menus.</summary>
    public sealed class QudControllerHelpTests
    {
        [Test]
        public void ReaderSeparatesDirectionFromStepAndDropsObsoleteAbilityChords()
        {
            string text = ControlsReference.BuildReaderText();
            StringAssert.Contains("Left stick: select a direction", text);
            StringAssert.Contains("RT: step", text);
            StringAssert.Contains("neutral stick waits one turn", text);
            StringAssert.Contains("D-pad left/right: select ability", text);
            StringAssert.DoesNotContain("D-pad/left stick move", text);
            StringAssert.DoesNotContain("activate slots 1/2/3", text);
        }

        [Test]
        public void ReaderDistinguishesRecoveryWaitingAndUnavailableSystems()
        {
            string text = ControlsReference.BuildReaderText();
            StringAssert.Contains("B: recover at a nearby bed/campfire", text);
            StringAssert.Contains("LT + B: wait 1/10/100 turns", text);
            StringAssert.Contains("Waiting does not heal", text);
            StringAssert.Contains("one page of ten slots", text);
            StringAssert.Contains("Missile weapons, reload and energy cells are unavailable", text);
            StringAssert.DoesNotContain("RT wait/underfoot", text);
        }

        [Test]
        public void ReaderShowsSeparateCharacterPauseAndHelpPaths()
        {
            string text = ControlsReference.BuildReaderText();
            StringAssert.Contains("Menu: character", text);
            StringAssert.Contains("View: pause", text);
            StringAssert.Contains("LT + Menu: controls", text);
            StringAssert.Contains("LT + RT: item details", text);
            StringAssert.DoesNotContain("Menu pause; View help", text);
            foreach (var row in ControlsReference.Bindings)
            {
                StringAssert.Contains(row.Key, text);
                StringAssert.Contains(row.What, text);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BootGuidanceShowsControllerLayoutOnlyWhenConnected(bool connected)
        {
            var scope = new InputTestFixture();
            scope.Setup();
            try
            {
                if (connected) InputSystem.AddDevice<Gamepad>().MakeCurrent();
                InputSystem.Update();
                var lines = new List<string>();
                ControlsReference.PrintBootSummary(lines.Add);
                string text = string.Join("\n", lines);
                Assert.AreEqual(connected, text.Contains("Controller:"));
                StringAssert.Contains("F1", text);
                StringAssert.Contains("quest", text);
                if (connected)
                {
                    StringAssert.Contains("RT step/wait", text);
                    StringAssert.Contains("Menu character", text);
                    StringAssert.Contains("View pause", text);
                    StringAssert.DoesNotContain("D-pad/stick move", text);
                }
                else Assert.LessOrEqual(lines.Count, 4);
            }
            finally { scope.TearDown(); }
        }
    }
}
