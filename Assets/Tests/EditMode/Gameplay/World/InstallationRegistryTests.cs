using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Installation registry and validator (Docs/MACHINE-INSTALLATIONS.md, S6). One test per error code with a valid-neighbour control;
    /// one bad optional row never drops the others; syntax loading is separate from factory admission.
    /// </summary>
    public sealed class InstallationRegistryTests
    {
        [SetUp] public void Setup() => InstallationRegistry.ResetForTests();
        [TearDown] public void Cleanup() => InstallationRegistry.ResetForTests();

        static string Row(string id = "gantry-forge", string place = "Gantry", string blueprint = "TinkersForge", string extra = "", int radius = 9, int probe = 48, int revision = 1,
            string near = "[\"Well\"]", string purpose = "A working forge.", string alt = "Carried kits remain.", string basis = "faction row")
            => "{\"Id\":\"" + id + "\",\"Place\":\"" + place + "\",\"Blueprint\":\"" + blueprint + "\",\"Retention\":\"None\",\"Near\":" + near + ",\"Radius\":" + radius
               + ",\"MaxProbe\":" + probe + ",\"Ground\":[\"Grass\"],\"Revision\":" + revision + ",\"Purpose\":\"" + purpose + "\",\"Alternative\":\"" + alt + "\",\"Basis\":\"" + basis + "\"" + extra + "}";
        static string File(string rows, string tail = "", string version = "1") => "{\"Version\":" + version + ",\"ForbiddenFactions\":[\"RotChoir\",\"CatacombFolk\"],\"Installations\":[" + rows + "]" + tail + "}";
        static System.Collections.Generic.IReadOnlyList<string> Load(string json) => InstallationRegistry.InitializeFromJson(json);

        // ── the valid neighbour ──

        [Test] public void AValidRowLoadsWithItsDataAndAWellFallback()
        {
            Assert.IsEmpty(Load(File(Row(near: "[\"Wellhouse\",\"Bench\"]"))));
            var spec = InstallationRegistry.For("Gantry").Single();
            Assert.AreEqual("gantry-forge", spec.Id); Assert.AreEqual("TinkersForge", spec.Blueprint); Assert.False(spec.Retain);
            CollectionAssert.AreEqual(new[] { "Wellhouse", "Bench", "Well" }, spec.Near); CollectionAssert.AreEqual(new[] { "Grass" }, spec.Ground);
            Assert.AreEqual(9, spec.Radius); Assert.AreEqual(48, spec.MaxProbe); Assert.AreEqual(1, spec.Revision);
        }

        [Test] public void NearAlwaysEndsWithWellOnceEvenWhenListed()
        {
            Load(File(Row(near: "[\"Well\",\"Bench\"]"))); CollectionAssert.AreEqual(new[] { "Bench", "Well" }, InstallationRegistry.For("Gantry").Single().Near);
            Load(File(Row(near: "[]"))); CollectionAssert.AreEqual(new[] { "Well" }, InstallationRegistry.For("Gantry").Single().Near);
        }

        [Test] public void MissingGroundFallsBackToTheDefaultBareGround()
        {
            Load(File(Row().Replace("\"Ground\":[\"Grass\"],", "")));
            CollectionAssert.AreEqual(InstallationRegistry.DefaultGround, InstallationRegistry.For("Gantry").Single().Ground);
        }

        // ── E01 file level ──

        [TestCase(null)] [TestCase("")] [TestCase("   ")] [TestCase("not json")] [TestCase("{\"Version\":2,\"Installations\":[]}")] [TestCase("{\"Installations\":[]}")]
        public void E01_UnusableFileClearsEverything(string json)
        {
            Load(File(Row())); Assert.AreEqual(1, InstallationRegistry.All.Count);
            var errors = Load(json); Assert.That(errors, Has.Some.StartsWith("E01")); Assert.AreEqual(0, InstallationRegistry.All.Count);
        }

        [Test] public void E01_MoreThanSixtyFourRowsIsRejectedAndSixtyFourIsNot()
        {
            string Rows(int n) => string.Join(",", Enumerable.Range(0, n).Select(i => Row("row-" + i)));
            Assert.That(Load(File(Rows(65))), Has.Some.StartsWith("E01")); Assert.AreEqual(0, InstallationRegistry.All.Count);
            Assert.That(Load(File(Rows(64))), Has.None.StartsWith("E01"));
        }

        [Test] public void E01_ZeroRowsIsAValidFile() { Assert.IsEmpty(Load(File(""))); Assert.AreEqual(0, InstallationRegistry.All.Count); }

        // ── row level: each code is rejected, its neighbour passes, and one bad row never drops the others ──

        static void Rejects(string code, string badRow, string goodRow)
        {
            var errors = Load(File(goodRow + "," + badRow));
            Assert.That(errors, Has.Some.StartsWith(code), code); Assert.AreEqual(1, InstallationRegistry.All.Count, code + ": the valid row survives");
            Assert.AreEqual("good", InstallationRegistry.All[0].Id);
            Assert.IsEmpty(Load(File(goodRow)), "control: the good row alone is clean");
        }
        string Good => Row("good", "Tine");

        [Test] public void E02_DuplicateId() => Rejects("E02", Row("good", "Gantry"), Good);
        [TestCase("has space")] [TestCase("dot.id")] [TestCase("")] public void E03_IdIsNotAToken(string id) => Rejects("E03", Row(id), Good);
        [Test] public void E03_BlueprintNearAndGroundMustBeTokens()
        {
            Rejects("E03", Row("bad", blueprint: "Tinkers Forge"), Good);
            Rejects("E03", Row("bad", near: "[\"a.b\"]"), Good);
            Rejects("E03", Row("bad").Replace("\"Grass\"", "\"Gr ass\""), Good);
        }
        [TestCase("Atlantis")] [TestCase("")] [TestCase("gantry")] public void E04_PlaceMustBeAnAuthoredPlace(string place) => Rejects("E04", Row("bad", place), Good);
        [Test] public void E05_PlaceWithAnExemptionCannotAlsoHaveARow()
        {
            string tail = ",\"Exemptions\":[{\"Place\":\"Gantry\",\"Reason\":\"r\",\"Source\":\"s\"}]";
            var errors = Load(File(Good + "," + Row("bad", "Gantry"), tail)); Assert.That(errors, Has.Some.StartsWith("E05")); Assert.AreEqual(1, InstallationRegistry.All.Count);
        }
        [Test] public void E06_ForbiddenFactionPlaceIsRejected()
        {
            string json = "{\"Version\":1,\"ForbiddenFactions\":[\"Villagers\"],\"Installations\":[" + Row("bad", "Gantry") + "," + Row("good", "Posy") + "]}";
            Assert.That(Load(json), Has.Some.StartsWith("E06")); Assert.AreEqual("good", InstallationRegistry.All.Single().Id);
        }
        [TestCase("Maybe")] [TestCase("")] [TestCase("zone")] public void E07_RetentionMustBeNoneOrZone(string retention) => Rejects("E07", Row("bad").Replace("\"Retention\":\"None\"", "\"Retention\":\"" + retention + "\""), Good);
        [Test] public void E07_ZoneIsAValidRetention() { Assert.IsEmpty(Load(File(Row().Replace("\"Retention\":\"None\"", "\"Retention\":\"Zone\"")))); Assert.True(InstallationRegistry.All.Single().Retain); }

        [TestCase(0, 48, 1, 4)] [TestCase(13, 48, 1, 4)] [TestCase(9, 0, 1, 4)] [TestCase(9, 129, 1, 4)] [TestCase(9, 48, 0, 4)] [TestCase(9, 48, 1, 5)]
        public void E08_NumericBoundsAreEnforced(int radius, int probe, int revision, int nearCount)
        {
            string near = "[" + string.Join(",", Enumerable.Range(0, nearCount).Select(i => "\"n" + i + "\"")) + "]";
            Rejects("E08", Row("bad", radius: radius, probe: probe, revision: revision, near: near), Good);
        }
        [TestCase(1, 1, 1, 4)] [TestCase(12, 128, 7, 0)] [TestCase(9, 48, 1, 4)]
        public void E08_BoundaryValuesAreAccepted(int radius, int probe, int revision, int nearCount)
        {
            string near = "[" + string.Join(",", Enumerable.Range(0, nearCount).Select(i => "\"n" + i + "\"")) + "]";
            Assert.IsEmpty(Load(File(Row(radius: radius, probe: probe, revision: revision, near: near))));
        }
        [Test] public void E08_MinRadiusIsBoundedByRadiusAndDefaultsToTwo()
        {
            Rejects("E08", Row("bad", extra: ",\"MinRadius\":10"), Good);                 // radius 9
            Rejects("E08", Row("bad", extra: ",\"MinRadius\":-1"), Good);
            Assert.IsEmpty(Load(File(Row(extra: ",\"MinRadius\":9")))); Assert.AreEqual(9, InstallationRegistry.All.Single().MinRadius);
            Assert.IsEmpty(Load(File(Row()))); Assert.AreEqual(2, InstallationRegistry.All.Single().MinRadius, "absent means the default of two");
        }
        [Test] public void E09_PurposeAlternativeAndBasisAreRequiredAndBounded()
        {
            Rejects("E09", Row("bad", purpose: ""), Good); Rejects("E09", Row("bad", alt: "  "), Good); Rejects("E09", Row("bad", basis: new string('x', 401)), Good);
            Assert.IsEmpty(Load(File(Row(basis: new string('x', 400)))), "400 characters is allowed");
        }
        [Test] public void E10_RequiredAbsoluteAndRngAreForbidden()
        {
            Rejects("E10", Row("bad", extra: ",\"Required\":true"), Good); Rejects("E10", Row("bad", extra: ",\"Absolute\":\"12,12\""), Good); Rejects("E10", Row("bad", extra: ",\"Rng\":\"shared\""), Good);
            Assert.IsEmpty(Load(File(Row(extra: ",\"Required\":false"))));
        }
        [Test] public void E11_TermsAreRejectedUntilServiceTermsExist() => Rejects("E11", Row("bad", extra: ",\"Terms\":\"Fee\""), Good);
        [Test] public void E12_CatacombZonesHaveNoAttachPointYet() => Rejects("E12", Row("bad", "Overworld.12.3.2"), Good);
        [Test] public void E13_ExemptionsNeedReasonAndSourceAndPendingNeedsAnOwner()
        {
            string tail = ",\"Exemptions\":[{\"Place\":\"Slip\",\"Reason\":\"r\"}],\"Pending\":[{\"Place\":\"Posy\",\"Note\":\"n\"}]";
            var errors = Load(File(Good, tail)); Assert.That(errors, Has.Some.StartsWith("E13:Slip")); Assert.That(errors, Has.Some.StartsWith("E13:Posy"));
            Assert.Null(InstallationRegistry.ExemptionFor("Slip")); Assert.Null(InstallationRegistry.PendingFor("Posy"));
            Assert.IsEmpty(Load(File(Good, ",\"Exemptions\":[{\"Place\":\"Slip\",\"Reason\":\"r\",\"Source\":\"s\"}],\"Pending\":[{\"Place\":\"Posy\",\"Owner\":\"o\"}]")));
            Assert.NotNull(InstallationRegistry.ExemptionFor("Slip")); Assert.NotNull(InstallationRegistry.PendingFor("Posy"));
        }

        [Test] public void OneInvalidRowDoesNotDropTheOthers()
        {
            var errors = Load(File(Row("a", "Gantry") + "," + Row("b b", "Tine") + "," + Row("c", "Slip") + "," + Row("a", "Posy")));
            Assert.AreEqual(2, errors.Count); CollectionAssert.AreEqual(new[] { "a", "c" }, InstallationRegistry.All.Select(s => s.Id));
        }

        [Test] public void ForReturnsRowsForOnePlaceInDataOrder()
        {
            Load(File(Row("g1", "Gantry") + "," + Row("t1", "Tine") + "," + Row("g2", "Gantry")));
            CollectionAssert.AreEqual(new[] { "g1", "g2" }, InstallationRegistry.For("Gantry").Select(s => s.Id)); Assert.IsEmpty(InstallationRegistry.For("Sill")); Assert.IsEmpty(InstallationRegistry.For(null));
        }

        // ── factory admission (E07 state-bearing, E14 not a machine) ──

        [Test] public void ValidateAdmitsAnExistingForgeAndRejectsNonMachines()
        {
            using (var scope = new HaulingContentScope())
            {
                Load(File(Row("ok", "Gantry", "TinkersForge") + "," + Row("missing", "Tine", "NoSuchThing") + "," + Row("creature", "Slip", "Villager") + "," + Row("stack", "Posy", "KnotflaxCord")
                          + "," + Row("takeable", "Sumphold", "Dagger")));
                var issues = InstallationRegistry.Validate(scope.Factory);
                Assert.That(issues, Has.Some.Contains("E14:missing")); Assert.That(issues, Has.Some.Contains("E14:creature"));
                Assert.That(issues, Has.Some.Contains("E14:stack")); Assert.That(issues, Has.Some.Contains("E14:takeable"));
                Assert.That(issues, Has.None.Contains("ok"), "control: a forge is admissible");
            }
        }

        [Test] public void ValidateRejectsAStateBearingBlueprintWithRetentionNone()
        {
            using (var scope = new HaulingContentScope())
            {
                Load(File(Row("chest", "Gantry", "Chest")));
                Assert.That(InstallationRegistry.Validate(scope.Factory), Has.Some.Contains("E07:chest"));
                Load(File(Row("chest", "Gantry", "Chest").Replace("\"Retention\":\"None\"", "\"Retention\":\"Zone\"")));
                Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory), "control: Retention Zone admits a state-bearing blueprint");
            }
        }

        const string Bench = "{\"Objects\":[{\"Name\":\"TestBench\",\"Parts\":[{\"Name\":\"Render\",\"Params\":[{\"Key\":\"DisplayName\",\"Value\":\"bench\"},{\"Key\":\"RenderString\",\"Value\":\"B\"},{\"Key\":\"ColorString\",\"Value\":\"&y\"},{\"Key\":\"RenderLayer\",\"Value\":\"5\"}]},{\"Name\":\"Physics\",\"Params\":[{\"Key\":\"Solid\",\"Value\":\"false\"}]}EXTRA],\"Stats\":[],\"Tags\":[]}]}";

        [Test] public void AdmissionIsAnAllowlistSoAnUnclassifiedPartForcesRetention()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Factory.LoadBlueprints(Bench.Replace("EXTRA", ",{\"Name\":\"Destructible\",\"Params\":[]}"));
                Load(File(Row("bench", "Gantry", "TestBench")));
                Assert.That(InstallationRegistry.Validate(scope.Factory), Has.Some.Contains("E07:bench"), "a Destructible blueprint carries HP and needs Retention Zone");
                Load(File(Row("bench", "Gantry", "TestBench").Replace("\"Retention\":\"None\"", "\"Retention\":\"Zone\"")));
                Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory), "control: Retention Zone admits it");
                scope.Factory.LoadBlueprints(Bench.Replace("EXTRA", ""));
                Load(File(Row("bench", "Gantry", "TestBench"))); Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory), "control: Render and Physics alone are stateless");
            }
        }

        [Test] public void ALoadoutBlueprintIsRejectedBecauseItDrawsFromAStaticRandomStream()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Factory.LoadBlueprints(Bench.Replace("EXTRA", ",{\"Name\":\"Loadout\",\"Params\":[]}"));
                Load(File(Row("bench", "Gantry", "TestBench").Replace("\"Retention\":\"None\"", "\"Retention\":\"Zone\"")));
                Assert.That(InstallationRegistry.Validate(scope.Factory), Has.Some.Contains("E14:bench"));
            }
        }

        [Test] public void ValidateWithoutAFactoryReportsInsteadOfThrowing() => Assert.That(InstallationRegistry.Validate(null), Has.Some.StartsWith("E14"));

        // ── defaults ──

        [Test] public void RegistryLoadsTheShippedRowsExemptionsAndPending()
        {
            InstallationRegistry.LoadDefaults();
            CollectionAssert.AreEquivalent(new[] { "gantry-forecourt-forge", "tine-forecourt-forge", "slip-forecourt-forge" }, InstallationRegistry.All.Where(s => string.IsNullOrEmpty(s.Site)).Select(s => s.Id), "three forge rows ship; the Last Counter row is pulled until its 3D kit has a forge family");
            CollectionAssert.AreEquivalent(new[] { "cinderhold-mine-pump" }, InstallationRegistry.All.Where(s => !string.IsNullOrEmpty(s.Site)).Select(s => s.Id), "and one site object: the Cinderhold pump, the body of a repair site");
            CollectionAssert.AreEquivalent(new[] { "RotChoir", "CatacombFolk" }, InstallationRegistry.ForbiddenFactions);
            Assert.NotNull(InstallationRegistry.ExemptionFor("the First Tent")); Assert.NotNull(InstallationRegistry.PendingFor("Sumphold")); Assert.NotNull(InstallationRegistry.PendingFor("Tally")); Assert.Null(InstallationRegistry.PendingFor("Gantry"));
        }
    }
}
