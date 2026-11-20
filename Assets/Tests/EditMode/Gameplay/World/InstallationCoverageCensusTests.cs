using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>"A machine in every town" as a measurable ratchet (S6): every authored place is classified exactly once, and Pending may only decrease.</summary>
    public sealed class InstallationCoverageCensusTests
    {
        /// <summary>Committed ratchet. Lower it when a place leaves Pending; raising it is a deliberate owner decision.</summary>
        const int PendingBaseline = 10;

        [SetUp] public void Setup() { InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); }
        [TearDown] public void Cleanup() => InstallationRegistry.ResetForTests();

        [Test] public void CoverageClassifiesAll20PlacesExactlyOnce()
        {
            var report = InstallationRegistry.Coverage();
            Assert.AreEqual(20, report.Entries.Count); Assert.AreEqual(20, report.Entries.Select(e => e.Place).Distinct().Count());
            Assert.IsEmpty(report.Conflicts, string.Join("; ", report.Conflicts));
            Assert.AreEqual(0, report.Count("Unclassified"), report.ToText());
            foreach (var place in WorldMapAuthoring.Places.Select(p => p.Name).Concat(InstallationRegistry.CatacombZones)) Assert.NotNull(report.KindOf(place), place);
        }

        [Test] public void TheCensusMatchesTheDocumentedBaseline()
        {
            var report = InstallationRegistry.Coverage();
            Assert.AreEqual(2, report.Count("Present")); Assert.AreEqual(5, report.Count("Exempt")); Assert.AreEqual(3, report.Count("Installed"));
            Assert.AreEqual(10, report.Count("Pending"));
            foreach (var place in new[] { "Gantry", "Tine", "Slip" }) Assert.AreEqual("Installed", report.KindOf(place), place);
            foreach (var place in new[] { "Tally", "Wellmeet" }) Assert.AreEqual("Pending", report.KindOf(place), place + " waits on its recorded gate");
        }

        [Test] public void PendingCountMayOnlyDecrease() => Assert.LessOrEqual(InstallationRegistry.Coverage().Count("Pending"), PendingBaseline);

        [Test] public void UnclassifiedPlaceFailsTheCensus()
        {
            InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[]}");
            var report = InstallationRegistry.Coverage();
            Assert.AreEqual(18, report.Count("Unclassified"), "only the two Present places are classified when no data is loaded");
        }

        [Test] public void APlaceInTwoClassesIsAConflict()
        {
            InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[],\"Pending\":[{\"Place\":\"Sill\",\"Owner\":\"o\"}]}");
            Assert.That(InstallationRegistry.Coverage().Conflicts, Has.Some.Contains("Sill"));
        }

        [Test] public void AnInstalledPlaceIsClassifiedInstalledAndLeavesPending()
        {
            string json = "{\"Version\":1,\"Installations\":[{\"Id\":\"g\",\"Place\":\"Gantry\",\"Blueprint\":\"TinkersForge\",\"Retention\":\"None\",\"Radius\":9,\"MaxProbe\":48,\"Revision\":1,"
                + "\"Purpose\":\"p\",\"Alternative\":\"a\",\"Basis\":\"b\"}],\"Pending\":[]}";
            Assert.IsEmpty(InstallationRegistry.InitializeFromJson(json));
            Assert.AreEqual("Installed", InstallationRegistry.Coverage().KindOf("Gantry"));
        }

        [Test] public void EveryExemptionCarriesAReasonAndASource()
        {
            foreach (var place in new[] { "the First Tent", "the Quiet's Door", "Overworld.4.6.2", "Overworld.12.3.2", "Overworld.16.4.2" })
            { var ex = InstallationRegistry.ExemptionFor(place); Assert.NotNull(ex, place); Assert.IsNotEmpty(ex.Reason); Assert.IsNotEmpty(ex.Source); }
        }

        [Test] public void EveryPendingEntryCarriesAnOwnerAndAForbiddenFactionNeverHasOne()
        {
            foreach (var place in WorldMapAuthoring.Places)
            {
                var pending = InstallationRegistry.PendingFor(place.Name);
                if (pending != null) Assert.IsNotEmpty(pending.Owner, place.Name);
                if (InstallationRegistry.ForbiddenFactions.Contains(place.Faction)) Assert.IsEmpty(InstallationRegistry.For(place.Name), place.Name);
            }
        }

        [Test] public void ShippedRowsAreAdmissibleAgainstRealContent()
        {
            using (var scope = new HaulingContentScope())
            {
                Assert.AreEqual(6, InstallationRegistry.All.Count, "three forges and three site objects (the pump, the weir and the survey line)");
                Assert.AreEqual(3, InstallationRegistry.All.Count(s => string.IsNullOrEmpty(s.Site)), "forges");
                Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory), "every shipped row passes blueprint admission");
                foreach (var spec in InstallationRegistry.All) { Assert.AreEqual(!string.IsNullOrEmpty(spec.Site), spec.Retain, spec.Id + ": a forge carries no state; a site object carries its place and site"); StringAssert.Contains("nothing requires", spec.Alternative); }
            }
        }

        [Test] public void PresentPlacesActuallyHaveAForge()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                foreach (var id in new[] { "Overworld.10.10.0", "Overworld.6.6.0" })
                {
                    var zone = manager.GetZone(id); Assert.NotNull(zone, id);
                    Assert.True(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "TinkersForge"), id + " should already hold a forge");
                }
            }
        }
    }
}
