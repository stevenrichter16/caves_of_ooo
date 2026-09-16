using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Discovery regression: the villages already generate, but the displayed
    /// controls describe only stairs. Both help surfaces must teach the actual
    /// surface-to-map-to-settlement journey without prescribing one exact copy.
    /// Native travel/arrival correctness belongs to the separate world audit.
    /// </summary>
    [TestFixture]
    public class NavigationDiscoveryTests
    {
        private static List<string> Read(bool bootSummary)
        {
            var lines = new List<string>();
            if (bootSummary) ControlsReference.PrintBootSummary(lines.Add);
            else ControlsReference.PrintHelp(lines.Add);
            return lines.Select(line => line.ToLowerInvariant()).ToList();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HelpTeachesThatSurfaceAscendOpensTheWorldMap(bool bootSummary)
        {
            Assert.IsTrue(Read(bootSummary).Any(line => line.Contains("<")
                && line.Contains("surface") && line.Contains("world map")),
                "A player roaming ground chunks needs to learn that < opens the world map from the surface, not just that it uses stairs.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HelpTeachesEnteringTheSelectedMapDestination(bool bootSummary)
        {
            Assert.IsTrue(Read(bootSummary).Any(line => line.Contains(">")
                && (line.Contains("enter") || line.Contains("descend"))
                && (line.Contains("selected") || line.Contains("destination") || line.Contains("underfoot"))),
                "The return binding must explain entering the destination chosen on the map, not only going downstairs.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HelpExplainsTheSettlementMarkerPlayersShouldLookFor(bool bootSummary)
        {
            Assert.IsTrue(Read(bootSummary).Any(line => line.Contains("!")
                && (line.Contains("settlement") || line.Contains("town") || line.Contains("village"))),
                "The native map marks villages with !; players should not have to infer that symbol from biome scenery.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HelpMakesThePhysicalShiftCommaAndShiftPeriodKeysDiscoverable(bool bootSummary)
        {
            string text = string.Join(" ", Read(bootSummary));
            Assert.IsTrue(text.Contains("shift+,") || text.Contains("shift + ,")
                || text.Contains("shift+comma") || text.Contains("shift + comma"),
                "< requires Shift+comma on the actual input path.");
            Assert.IsTrue(text.Contains("shift+.") || text.Contains("shift + .")
                || text.Contains("shift+period") || text.Contains("shift + period"),
                "> requires Shift+period on the actual input path.");
        }

        [Test]
        public void FullHelpAllowsAnAbsentOutputSink()
        {
            Assert.DoesNotThrow(() => ControlsReference.PrintHelp(null));
        }

        [Test]
        public void BootSummaryAllowsAnAbsentOutputSink()
        {
            Assert.DoesNotThrow(() => ControlsReference.PrintBootSummary(null));
        }
    }
}
