using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Cinderhold mine pump as a world object (Docs/MACHINE-INVENTED-USES.md 3f): the body of the data-defined "Pump" repair site. The repair rules, the materials and the result are the
    /// ones the elder's conversation already used; the object adds reading its state, saying what each repair needs and what you carry, and doing the repair in one action.
    /// </summary>
    public sealed class PlaceSiteObjectTests : FiftyWorldFixture
    {
        const string CinderholdZone = "Overworld.6.6.0";
        int turn = 100;

        [SetUp] public void SetupPump()
        {
            PlaceSiteCatalog.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json")));
            new SettlementManager(() => turn, id => PlaceSiteCatalog.PlaceAt(id) is string name ? new PointOfInterest(POIType.Village, name) : null);
            MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void TearDownPump() { SettlementManager.ResetCurrent(); PlaceSiteCatalog.Reset(); }

        Entity Pump() => Place("MinePump", 11, 10);
        static RepairableSiteState Site => PlaceSites.Site("Cinderhold", "Pump");
        static PlaceSiteData Data => PlaceSiteCatalog.Get("Cinderhold", "Pump");
        void Give(string blueprint, int count) { for (int i = 0; i < count; i++) Carry(blueprint); }
        void GiveLasting() { Give("IronSpikeComponent", 2); Give("OakHaftComponent", 1); Give("LeatherBindingComponent", 1); }
        int Count(string blueprint) => ConcernItems.CountCarried(Actor, blueprint);
        InventoryAction Row(Entity pump, string name) => Actions(pump).SingleOrDefault(a => a.Name == name);
        System.Collections.Generic.List<string> Menu(Entity pump) { var l = new System.Collections.Generic.List<string>(); StateReader.CollectMenuStatus(pump, Zone, Zone.GetEntityCell(pump), Actor, l); return l; }
        string Examine(Entity pump) { var l = new System.Collections.Generic.List<string>(); StateReader.Collect(pump, true, Zone, Zone.GetEntityCell(pump), Actor, l); return string.Join(" | ", l); }
        void Look(Entity pump) { var e = GameEvent.New("Render"); e.SetParameter("ColorString", "&K"); pump.FireEvent(e); }
        Entity PumpAt(RepairStage stage) { var pump = Pump(); Site.Stage = stage; return pump; }

        // ── what it is ──

        [Test] public void ThePumpIsASolidObjectWithTheSitePartAndItsWordsAndItsPlaceRowIsAdmitted()
        {
            var pump = Factory.CreateEntity("MinePump"); Assert.True(pump.GetPart<PhysicsPart>().Solid); var part = pump.GetPart<PlaceSiteObjectPart>(); Assert.NotNull(part);
            Assert.AreEqual("sheared", part.BrokenWord); Assert.AreEqual("running", part.MendedWord); Assert.AreEqual("", part.Place, "the placement names the place and site, not the blueprint");
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); var spec = InstallationRegistry.All.Single(s => s.Id == "cinderhold-mine-pump");
            Assert.AreEqual("Pump", spec.Site); Assert.True(spec.Retain, "a Part with its own fields is stateful: the village is kept"); Assert.IsEmpty(InstallationRegistry.Validate(Factory), "every shipped row is admissible");
            InstallationRegistry.ResetForTests();
        }

        // ── reading it ──

        [Test] public void ABrokenPumpSaysWhatHappenedToItAndWhatEachRepairNeedsWithWhatYouCarry()
        {
            var pump = Pump(); pump.GetPart<PlaceSiteObjectPart>().Place = "Cinderhold"; pump.GetPart<PlaceSiteObjectPart>().SiteId = "Pump"; Give("IronSpikeComponent", 1);
            StringAssert.Contains("sheared at the collar", Examine(pump)); var lines = Menu(pump);
            Assert.AreEqual("The mine pump is sheared.", lines[0]); Assert.True(lines.All(l => l.Length <= 44), "each line fits the narrowest menu: " + string.Join(" / ", lines));
            string joined = string.Join(" ", lines); StringAssert.Contains("Mend for good:", joined); StringAssert.Contains("iron spike 1/2", joined); StringAssert.Contains("oak haft 0/1", joined); StringAssert.Contains("leather binding 0/1", joined); StringAssert.Contains("Patch (600 turns):", joined); StringAssert.Contains("fire clay 0/2", joined);
        }

        [Test] public void ThePumpIsOnlyTheSiteItWasPlacedFor_AUnlinkedOneSaysNothing_Counter()
        {
            var pump = Pump(); Assert.IsEmpty(Menu(pump)); Assert.AreEqual("", Examine(pump)); Assert.IsEmpty(Actions(pump).Where(a => a.Name.StartsWith("PlaceSite")));
        }

        void Link(Entity pump) { var p = pump.GetPart<PlaceSiteObjectPart>(); p.Place = "Cinderhold"; p.SiteId = "Pump"; }

        // ── rows ──

        [Test] public void WithoutTheMaterialsBothRowsAreShownDisabledAndSayWhatIsMissing()
        {
            var pump = Pump(); Link(pump); Give("IronSpikeComponent", 1);
            var mend = Row(pump, "PlaceSiteMend"); var patch = Row(pump, "PlaceSitePatch"); Assert.True(mend.IsDisabled); Assert.True(patch.IsDisabled);
            StringAssert.Contains("1 more iron spike", mend.DisabledReason); StringAssert.Contains("1 oak haft", mend.DisabledReason); StringAssert.Contains("leather binding", mend.DisabledReason); StringAssert.Contains("fire clay", patch.DisabledReason);
            Assert.AreEqual('m', mend.Key); Assert.AreEqual('p', patch.Key);
        }

        [Test] public void MendingForGoodTakesTheMaterialsInOneActionAndSetsTheSameStateTheConversationDoes()
        {
            var pump = Pump(); Link(pump); GiveLasting(); var mend = Row(pump, "PlaceSiteMend"); Assert.False(mend.IsDisabled); Assert.AreEqual(ActionTime.OneAction, mend.Time); StringAssert.Contains("lasts", mend.Hint);
            Assert.True(Act(pump, mend.Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(RepairStage.StableRepair, Site.Stage); Assert.AreEqual(RepairMethodId.ManualRepair, Site.ResolvedByMethod); Assert.AreEqual(0, Count("IronSpikeComponent")); Assert.AreEqual(0, Count("OakHaftComponent")); Assert.AreEqual(0, Count("LeatherBindingComponent"));
            StringAssert.Contains("Mended", Examine(pump)); Assert.IsEmpty(Actions(pump).Where(a => a.Name.StartsWith("PlaceSite")), "a mended pump offers no repair");
            Assert.AreEqual("The mine pump is running.", Menu(pump)[0]);
        }

        [Test] public void APatchHoldsForItsTurnsTellsHowLongAndLeavesTheLastingMendOffered()
        {
            var pump = Pump(); Link(pump); Give("FireClay", 2); Assert.True(Act(pump, Row(pump, "PlaceSitePatch").Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(RepairStage.TemporarilyPurified, Site.Stage); Assert.AreEqual(100 + 600, Site.RelapseAtTurn); Assert.AreEqual(0, Count("FireClay"));
            turn = 400; StringAssert.Contains("it holds about 300 more turns, then fails", Examine(pump)); StringAssert.Contains("(about 300 more turns)", Menu(pump)[0]);
            Assert.IsNull(Row(pump, "PlaceSitePatch"), "already patched"); Assert.NotNull(Row(pump, "PlaceSiteMend"), "making it last is still offered"); Assert.True(Row(pump, "PlaceSiteMend").IsDisabled);
            GiveLasting(); Assert.True(Act(pump, Row(pump, "PlaceSiteMend").Command)); Assert.AreEqual(RepairStage.StableRepair, Site.Stage); Assert.IsNull(Site.RelapseAtTurn);
        }

        [Test] public void NothingIsTakenWhenAnOuterStepFails_AndNothingChangesOnARefusal()
        {
            var pump = Pump(); Link(pump); GiveLasting(); string command = Row(pump, "PlaceSiteMend").Command; FailAfter();
            Assert.False(Act(pump, command)); Assert.AreEqual(RepairStage.Fouled, Site.Stage); Assert.AreEqual(2, Count("IronSpikeComponent")); Assert.AreEqual(1, Count("OakHaftComponent"));
        }

        [Test] public void ForgedAndStaleCommandsAreRefusedAndTakeNothing()
        {
            var pump = Pump(); Link(pump); Give("IronSpikeComponent", 2);
            foreach (var bad in new[] { "PlaceSiteRepair|", "PlaceSiteRepair|Forever", "PlaceSiteRepair|lasting", "PlaceSiteRepair|Lasting|extra", "PlaceSiteRepair|Lasting" }) Assert.False(Act(pump, bad), bad);
            Assert.AreEqual(RepairStage.Fouled, Site.Stage); Assert.AreEqual(2, Count("IronSpikeComponent"), "even the real command refuses without the rest");
            GiveLasting(); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 20, 20)); Assert.IsEmpty(Actions(pump).Where(a => a.Name.StartsWith("PlaceSite")), "no rows from across the town"); Assert.False(Act(pump, "PlaceSiteRepair|Lasting"), "nor by command");
        }

        // ── one rule, two doors ──

        [Test] public void APumpRepairedThroughConversationReadsAsMendedAtTheObject_AndTheOtherWayRound()
        {
            var pump = Pump(); Link(pump); GiveLasting(); Assert.Null(SettlementManager.Current.RepairPlaceSite(CinderholdZone, "Pump", true, Actor));
            Look(pump); Assert.AreEqual("mine pump (running)", pump.GetPart<RenderPart>().DisplayName); StringAssert.Contains("Mended", Examine(pump));
        }

        [Test] public void TheObjectLooksLikeItsState()
        {
            var pump = Pump(); Link(pump); Look(pump); Assert.AreEqual("mine pump (sheared)", pump.GetPart<RenderPart>().DisplayName); string broken = pump.GetPart<RenderPart>().ColorString;
            Site.Stage = RepairStage.TemporarilyPurified; Look(pump); Assert.AreEqual("mine pump (patched)", pump.GetPart<RenderPart>().DisplayName); string patched = pump.GetPart<RenderPart>().ColorString;
            Site.Stage = RepairStage.StableRepair; Look(pump); Assert.AreEqual("mine pump (running)", pump.GetPart<RenderPart>().DisplayName); string mended = pump.GetPart<RenderPart>().ColorString;
            CollectionAssert.AllItemsAreUnique(new[] { broken, patched, mended }, "three states, three colours");
        }

        [Test] public void ThePartSavesItsPlaceAndSite()
        {
            var pump = Pump(); Link(pump); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(pump); var part = copy.GetPart<PlaceSiteObjectPart>(); Assert.AreEqual("Cinderhold", part.Place); Assert.AreEqual("Pump", part.SiteId); Assert.AreEqual("sheared", part.BrokenWord);
        }

        [Test] public void NamesAndMissingSentencesReadPlainly()
        {
            Assert.AreEqual("iron spike", PlaceSiteRepairs.NameOf("IronSpikeComponent")); Assert.AreEqual("fire clay", PlaceSiteRepairs.NameOf("FireClay")); Assert.AreEqual("", PlaceSiteRepairs.NameOf(null));
            var fix = new PlaceSiteFixData(); fix.Materials.Add(new PlaceSiteMaterial { Blueprint = "FireClay", Count = 2 }); Assert.AreEqual("You still need 2 fire clay.", PlaceSiteRepairs.Missing(fix, Actor));
            Give("FireClay", 1); Assert.AreEqual("You still need 1 more fire clay.", PlaceSiteRepairs.Missing(fix, Actor)); Give("FireClay", 1); Assert.AreEqual("", PlaceSiteRepairs.Missing(fix, Actor));
            fix.Tool = "Hatchet"; StringAssert.Contains("a hatchet to work with", PlaceSiteRepairs.Missing(fix, Actor)); Assert.AreEqual("fire clay x2 (you have 2); tool: hatchet (you have 0)", PlaceSiteRepairs.Needs(fix, Actor));
        }

        // ── placed in a fresh Cinderhold ──

        static string ZoneOf(string place) { var p = WorldMapAuthoring.Places.Single(q => q.Name == place); return "Overworld." + p.X + "." + p.Y + ".0"; }

        [Test] public void AFreshCinderholdHasExactlyOnePumpBesideTheWell_AndNoOtherTownHasOne([Values(64, 704860946, 1729)] int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(seed); InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                var zone = manager.GetZone(ZoneOf("Cinderhold")); var pumps = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "MinePump").ToList();
                Assert.AreEqual(1, pumps.Count, "seed " + seed); var pump = pumps[0]; var part = pump.GetPart<PlaceSiteObjectPart>(); Assert.AreEqual("Cinderhold", part.Place); Assert.AreEqual("Pump", part.SiteId);
                var marker = pump.GetPart<InstallationMarkerPart>(); Assert.AreEqual("cinderhold-mine-pump", marker.InstallationId); Assert.True(marker.RetainZone);
                var well = zone.GetReadOnlyEntities().First(e => e.BlueprintName == "Well"); var a = zone.GetEntityCell(pump); var b = zone.GetEntityCell(well); Assert.LessOrEqual(System.Math.Max(System.Math.Abs(a.X - b.X), System.Math.Abs(a.Y - b.Y)), 9);
                foreach (var other in new[] { "Gantry", "Tine", "Slip", "Posy" }) Assert.False(manager.GetZone(ZoneOf(other)).GetReadOnlyEntities().Any(e => e.BlueprintName == "MinePump"), other);
            }
            InstallationRegistry.ResetForTests();
        }

        [Test] public void ThePumpDoesNotCountAsAnInstalledMachineInTheEveryTownCensus_CinderholdStaysPresentNotInstalled()
        {
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); var report = InstallationRegistry.Coverage();
            Assert.AreEqual("Present", report.KindOf("Cinderhold")); Assert.IsEmpty(report.Conflicts, string.Join("; ", report.Conflicts)); InstallationRegistry.ResetForTests();
        }

        [Test] public void ASiteRowNamingAnUnknownSiteIsRefused_AndOneWithoutTheSitePartOmitsItselfWithAReason()
        {
            InstallationRegistry.ResetForTests();
            string row(string site) => "{\"Version\":1,\"Installations\":[{\"Id\":\"x\",\"Place\":\"Cinderhold\",\"Blueprint\":\"MinePump\",\"Site\":\"" + site + "\",\"Retention\":\"Zone\",\"Near\":[\"Well\"],\"Radius\":9,\"MaxProbe\":48,\"Revision\":1,\"Purpose\":\"p\",\"Alternative\":\"a\",\"Basis\":\"b\"}]}";
            Assert.IsEmpty(InstallationRegistry.InitializeFromJson(row("Pump")), "the real site is accepted"); InstallationRegistry.ResetForTests();
            CollectionAssert.AreEqual(new[] { "E15:x" }, InstallationRegistry.InitializeFromJson(row("NoSuchSite"))); InstallationRegistry.ResetForTests();
        }

        // ── quality of life ──

        [Test] public void TheFullReaderAddsWhatYouCouldDoFromWhatYouCarry_TheQuickOneStaysShort()
        {
            var pump = Pump(); Link(pump); var quick = new System.Collections.Generic.List<string>(); StateReader.Collect(pump, false, Zone, Zone.GetEntityCell(pump), Actor, quick);
            StringAssert.DoesNotContain("mend it for good", string.Join(" ", quick)); StringAssert.Contains("it needs 2 iron spike, 1 oak haft, 1 leather binding", Examine(pump)); StringAssert.Contains("to patch it for now, 2 fire clay", Examine(pump));
            Give("FireClay", 2); StringAssert.Contains("You carry what it takes to patch it for now.", Examine(pump)); GiveLasting(); StringAssert.Contains("You carry what it takes to mend it for good.", Examine(pump));
        }


        [Test] public void TheRepairRuleTable_EveryStageAndFixHasItsOwnAnswer()
        {
            Assert.AreEqual(PlaceSiteRepairs.MissingPlayer, PlaceSiteRepairs.Check(Data, Site, true, null)); Assert.AreEqual(PlaceSiteRepairs.UnknownSite, PlaceSiteRepairs.Check(null, Site, true, Actor)); Assert.AreEqual(PlaceSiteRepairs.UnknownSite, PlaceSiteRepairs.Check(Data, null, true, Actor));
            Site.Stage = RepairStage.Fouled; Assert.AreEqual(PlaceSiteRepairs.MissingMaterials, PlaceSiteRepairs.Check(Data, Site, true, Actor)); Assert.AreEqual(PlaceSiteRepairs.MissingMaterials, PlaceSiteRepairs.Check(Data, Site, false, Actor));
            GiveLasting(); Give("FireClay", 2); Assert.IsNull(PlaceSiteRepairs.Check(Data, Site, true, Actor)); Assert.IsNull(PlaceSiteRepairs.Check(Data, Site, false, Actor));
            Site.Stage = RepairStage.TemporarilyPurified; Assert.IsNull(PlaceSiteRepairs.Check(Data, Site, true, Actor), "a patch can be made to last"); Assert.AreEqual(PlaceSiteRepairs.AlreadyPatched, PlaceSiteRepairs.Check(Data, Site, false, Actor));
            Site.Stage = RepairStage.StableRepair; Assert.AreEqual(PlaceSiteRepairs.AlreadyRepaired, PlaceSiteRepairs.Check(Data, Site, true, Actor)); Assert.AreEqual(PlaceSiteRepairs.AlreadyRepaired, PlaceSiteRepairs.Check(Data, Site, false, Actor));
            Site.Stage = RepairStage.ImprovedWithCaretaker; Assert.AreEqual(PlaceSiteRepairs.AlreadyRepaired, PlaceSiteRepairs.Check(Data, Site, true, Actor));
        }

        [Test] public void AForgedRepairOnAMendedPumpIsRefusedAndTakesNothing()
        {
            var pump = PumpAt(RepairStage.StableRepair); Link(pump); GiveLasting(); Assert.False(Act(pump, "PlaceSiteRepair|Lasting")); Assert.False(Act(pump, "PlaceSiteRepair|Patch"));
            Assert.AreEqual(2, Count("IronSpikeComponent")); Assert.AreEqual(1, Count("LeatherBindingComponent")); StringAssert.Contains("already properly repaired", string.Join("|", MessageLog.GetRecent(3)));
        }

        // ── every data-defined repair site has a body ──

        public static readonly object[] Bodies =
        {
            new object[] { "Cinderhold", "Pump", "MinePump", "cinderhold-mine-pump", true },
            new object[] { "Tine", "Weir", "FishingWeir", "tine-fishing-weir", true },
            new object[] { "the Drowned Ledger", "SurveyLine", "SurveyLine", "ledger-survey-line", true },
            new object[] { "Wellmeet", "Scales", "PanScales", "wellmeet-pan-scales", true },
        };

        [Test] public void EveryDataSiteHasABodyRow_ThePlaceSiteCatalogAndTheInstallationRowsAgree()
        {
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults();
            // Gantry's guest pole is the composition-owned GuestClothPole, shared with the Wellmeet and First Tent camps and already the body of that site's concern, so it has no row of its own (a duplicate would put two poles in one town). The pan scales borrow the Wellmeet kit's rack model.
            var exempt = new[] { "Gantry:GuestPole" };
            foreach (var site in PlaceSiteCatalog.All) Assert.AreEqual(!exempt.Contains(site.Place + ":" + site.Id), InstallationRegistry.All.Any(r => r.Place == site.Place && r.Site == site.Id), site.Place + ":" + site.Id);
            Assert.AreEqual(PlaceSiteCatalog.All.Count - exempt.Length, InstallationRegistry.All.Count(r => !string.IsNullOrEmpty(r.Site)), "and no row without a site"); InstallationRegistry.ResetForTests();
        }

        [TestCaseSource(nameof(Bodies))]
        public void EachBodyReadsItsSiteAndMendsItWithTheSitesOwnMaterials(string place, string siteId, string blueprint, string rowId, bool hasPatch)
        {
            Pack.MaxWeight = 1000; var data = PlaceSiteCatalog.Get(place, siteId); string zoneId = PlaceSites.ZoneIdOf(place); Assert.NotNull(data, place); Assert.NotNull(zoneId, place);
            var body = Place(blueprint, 11, 10); var part = body.GetPart<PlaceSiteObjectPart>(); Assert.NotNull(part, blueprint + " carries the site part"); part.Place = place; part.SiteId = siteId; var site = PlaceSites.Site(place, siteId);
            if (blueprint != "MinePump") Assert.False(body.GetPart<PhysicsPart>().Solid, "stands on bare ground without blocking a route"); StringAssert.Contains(data.Broken, Examine(body)); Assert.AreEqual("The " + data.Name + (part.Plural ? " are " : " is ") + part.BrokenWord + ".", Menu(body)[0]);
            Assert.True(Row(body, "PlaceSiteMend").IsDisabled); Assert.AreEqual(hasPatch, Row(body, "PlaceSitePatch") != null, "a patch row only where the site has a patch");
            foreach (var m in data.Lasting.Materials) Give(m.Blueprint, m.Count); if (!string.IsNullOrEmpty(data.Lasting.Tool)) Carry(data.Lasting.Tool);
            var mend = Row(body, "PlaceSiteMend"); Assert.False(mend.IsDisabled, mend.DisabledReason); Assert.True(Act(body, mend.Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(RepairStage.StableRepair, site.Stage); foreach (var m in data.Lasting.Materials) Assert.AreEqual(0, Count(m.Blueprint), m.Blueprint + " used up");
            if (!string.IsNullOrEmpty(data.Lasting.Tool)) Assert.AreEqual(1, Count(data.Lasting.Tool), "the tool is kept"); Look(body); StringAssert.Contains("(" + part.MendedWord + ")", body.GetPart<RenderPart>().DisplayName);
        }

        [TestCaseSource(nameof(Bodies))]
        public void EachBodyIsPlacedOnceInAFreshTownAtEverySeed(string place, string siteId, string blueprint, string rowId, bool hasPatch)
        {
            foreach (int seed in new[] { 64, 704860946, 1729 })
                using (var scope = new HaulingContentScope())
                {
                    scope.Seed(seed); InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                    var zone = manager.GetZone(ZoneOf(place)); var bodies = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == blueprint).ToList(); Assert.AreEqual(1, bodies.Count, place + " seed " + seed + ": " + string.Join(",", Diag.Snapshot(80).Where(r => r.Kind == "InstallationOmitted").Select(r => r.PayloadJson)));
                    var part = bodies[0].GetPart<PlaceSiteObjectPart>(); Assert.AreEqual(place, part.Place); Assert.AreEqual(siteId, part.SiteId); Assert.AreEqual(rowId, bodies[0].GetPart<InstallationMarkerPart>().InstallationId); Assert.True(bodies[0].GetPart<InstallationMarkerPart>().RetainZone);
                }
            InstallationRegistry.ResetForTests();
        }

        [Test] public void ARepairThatThrowsPartWayPutsTheMaterialsAndTheSiteBack()
        {
            var pump = Pump(); Link(pump); GiveLasting(); string command = Row(pump, "PlaceSiteMend").Command; var manager = SettlementManager.Current;
            manager.SiteStateChanged += Boom; try { try { Act(pump, command); } catch (System.InvalidOperationException) { } }
            finally { manager.SiteStateChanged -= Boom; }
            Assert.AreEqual(RepairStage.Fouled, Site.Stage, "the site is as it was"); Assert.AreEqual(2, Count("IronSpikeComponent")); Assert.AreEqual(1, Count("OakHaftComponent")); Assert.AreEqual(1, Count("LeatherBindingComponent"));
        }
        static void Boom(string settlement, RepairableSiteState site) { if (site.Stage != RepairStage.Fouled) throw new System.InvalidOperationException("a listener failed"); }

        [Test] public void ARefusedRepairLeavesTheSiteAndThePackUntouched()
        {
            var pump = Pump(); Link(pump); Give("IronSpikeComponent", 2); var before = Site.Clone(); Assert.False(Act(pump, "PlaceSiteRepair|Lasting")); Assert.AreEqual(before.Stage, Site.Stage); Assert.AreEqual(2, Count("IronSpikeComponent"));
        }

        [Test] public void ASiteRowWhoseBlueprintLacksTheSitePartOmitsItselfWithAReason()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); InstallationRegistry.ResetForTests();
                Assert.IsEmpty(InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[{\"Id\":\"probe\",\"Place\":\"Cinderhold\",\"Blueprint\":\"SoundingRod\",\"Site\":\"Pump\",\"Retention\":\"Zone\",\"Near\":[\"Well\"],\"Radius\":9,\"MaxProbe\":48,\"Revision\":1,\"Purpose\":\"p\",\"Alternative\":\"a\",\"Basis\":\"b\"}]}"));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true); var zone = manager.GetZone(ZoneOf("Cinderhold"));
                Assert.False(zone.GetReadOnlyEntities().Any(e => e.GetPart<InstallationMarkerPart>()?.InstallationId == "probe"), "nothing was placed");
                Assert.True(Diag.Snapshot(80).Any(r => r.Kind == "InstallationOmitted" && r.PayloadJson.Contains("probe")), "and the omission says so");
            }
            InstallationRegistry.ResetForTests();
        }
    }
}
