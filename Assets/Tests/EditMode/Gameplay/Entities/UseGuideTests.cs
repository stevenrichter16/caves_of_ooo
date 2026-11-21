using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The owner (2026-11-21): a machine's or tool's description must say exactly how to use it, what it needs, and whether it can be used right now. One collector builds that block
    /// for every Part that implements <see cref="IDescribeUse"/>: the Part supplies the static facts from its own live constants (how, needs, limits); the live "right now" line is
    /// built from the very rows the thing's menus would show for the viewer, so it can never promise a use the game refuses. Both Examine readers show it ahead of the flavor text.
    /// </summary>
    public sealed class UseGuideTests
    {
        HaulingContentScope scope; Zone zone; Entity viewer;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); Diag.ResetAll(); zone = new Zone("Overworld.1.1.0"); viewer = scope.Factory.CreateEntity("Player"); zone.AddEntity(viewer, 10, 10); Seen(viewer); }
        [TearDown] public void Cleanup() => scope.Dispose();

        /// <summary>A thing that explains itself and offers rows, so every contract of the collector can be driven without a real machine.</summary>
        sealed class Probe : Part, IDescribeUse
        {
            public override string Name => "UseProbe" + Id; public string Id = "";
            public int OrderValue = 50; public string How, Need, Limit, Why; public bool Throws; public int Calls;
            public bool OfferRow, OfferDisabled; public string RowLabel = "probe the thing", Reason = "It is jammed."; public Entity LastActor;
            public int Order => OrderValue;
            public void DescribeUse(DescribeContext context, UseGuide guide)
            {
                Calls++; if (Throws) throw new InvalidOperationException("boom");
                if (How != null) guide.How.Add(How); if (Need != null) guide.Needs.Add(Need); if (Limit != null) guide.Limits.Add(Limit); if (Why != null) guide.WhyNot = Why;
            }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "GetInventoryActions") return true;
                LastActor = e.GetParameter<Entity>("Actor"); var rows = e.GetParameter<InventoryActionList>("Actions");
                if (OfferRow) rows.AddAction("Probe", RowLabel, "ProbeCmd");
                if (OfferDisabled) rows.AddDisabled("Probe", RowLabel, "jam", Reason);
                return true;
            }
        }
        /// <summary>An ordinary Part that adds its own row but does not describe use: its rows are not the thing's own and never appear in the block.</summary>
        sealed class Bystander : Part
        {
            public override string Name => "Bystander";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "GetInventoryActions") e.GetParameter<InventoryActionList>("Actions").AddAction("Other", "do something unrelated", "OtherCmd"); return true; }
        }

        /// <summary>A thing whose rows are produced by a shared service rather than its own event handler: it reports them through <see cref="IDescribeUseRows"/>.</summary>
        sealed class ServiceBacked : Part, IDescribeUse, IDescribeUseRows
        {
            public override string Name => "ServiceBacked"; public int Order => 70; public Entity AskedAs; public Zone AskedIn;
            public void DescribeUse(DescribeContext context, UseGuide guide) => guide.How.Add("Use the service.");
            public void AddUseRows(Entity actor, Zone zone, InventoryActionList rows) { AskedAs = actor; AskedIn = zone; rows.AddAction("Svc", "run the service", "SvcCmd"); }
        }

        void Seen(Entity e) { var c = zone.GetEntityCell(e); c.IsVisible = true; c.Explored = true; }
        Entity Subject() { var e = scope.Factory.CreateEntity("Crate"); Assert.True(zone.AddEntity(e, 11, 10)); Seen(e); return e; }
        Probe Add(Entity owner, string id, Action<Probe> set = null) { var p = new Probe { Id = id }; owner.AddPart(p); set?.Invoke(p); return p; }
        List<string> Collect(Entity owner, Entity who = null, bool carried = false)
        {
            var into = new List<string>(); UseGuides.Collect(owner, carried ? null : zone, carried ? null : zone.GetEntityCell(owner), who, into); return into;
        }

        // ── the composed block ──

        [Test] public void TheBlockHasOneLinePerKindInAFixedOrder()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "Press C beside it and choose probe."; p.Need = "a steady hand"; p.Limit = "It rings."; p.OfferRow = true; });
            CollectionAssert.AreEqual(new[]
            {
                "How to use: Press C beside it and choose probe.", "Needs: a steady hand.", "Cost and limits: It rings.", "Right now you can: probe the thing."
            }, Collect(e, viewer));
        }

        [Test] public void SeveralPartsContributeInPartOrderAndSentencesJoin()
        {
            var e = Subject(); Add(e, "late", p => { p.OrderValue = 30; p.How = "Second."; p.Need = "tar"; }); Add(e, "early", p => { p.OrderValue = 10; p.How = "First."; p.Need = "a rope"; });
            var lines = Collect(e, viewer);
            Assert.That(lines[0], Is.EqualTo("How to use: First. Second."));
            Assert.That(lines[1], Is.EqualTo("Needs: a rope; tar."), "needs are separate items joined with semicolons");
        }

        [Test] public void AnEmptyGuideAddsNothingAndReportsFalse()
        {
            var e = Subject(); Add(e, "a", p => { p.OfferRow = true; });
            var into = new List<string>(); Assert.False(UseGuides.Collect(e, zone, zone.GetEntityCell(e), viewer, into)); Assert.IsEmpty(into, "no facts, so no block, even with rows");
            Assert.False(UseGuides.Collect(Subject(), zone, null, viewer, into), "an entity with no describer"); Assert.IsEmpty(into);
        }

        [Test] public void NullsAreSafe()
        {
            Assert.False(UseGuides.Collect(null, zone, null, viewer, new List<string>())); Assert.False(UseGuides.Collect(Subject(), zone, null, viewer, null));
        }

        [Test] public void AThrowingDescriberIsIsolatedAndTheOthersStillRender()
        {
            var e = Subject(); Add(e, "bad", p => { p.OrderValue = 10; p.Throws = true; }); Add(e, "good", p => { p.OrderValue = 20; p.How = "Still here."; });
            CollectionAssert.AreEqual(new[] { "How to use: Still here." }, Collect(e));
            Assert.That(Diag.Snapshot(20).Select(r => r.PayloadJson), Has.Some.Contains("boom"), "the failure is recorded, not swallowed silently");
        }

        // ── the live "right now" line ──

        [Test] public void RightNowListsTheEnabledRowsTheMenuWouldShow()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.OfferRow = true; });
            Assert.That(Collect(e, viewer), Has.Member("Right now you can: probe the thing."));
        }

        [Test] public void ADisabledRowIsReportedWithItsReason()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.OfferDisabled = true; });
            Assert.That(Collect(e, viewer), Has.Member("Right now you cannot: probe the thing (It is jammed)."), "the reason's own full stop is not doubled");
        }

        [Test] public void EnabledAndDisabledRowsAreBothSaid()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.OfferRow = true; }); Add(e, "b", p => { p.OfferDisabled = true; p.RowLabel = "pry it open"; p.Reason = "You have nothing to pry with."; });
            Assert.That(Collect(e, viewer), Has.Member("Right now you can: probe the thing. Not now: pry it open (You have nothing to pry with)."));
        }

        [Test] public void WithNoRowsTheThingsOwnReasonIsGiven()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.Why = "Nothing is within reach"; });
            Assert.That(Collect(e, viewer), Has.Member("Right now: Nothing is within reach."));
        }

        [Test] public void TheFirstReasonGivenStands_ABroaderConditionIsNotOverruledByANarrowerOne()
        {
            var e = Subject(); Add(e, "late", p => { p.OrderValue = 30; p.How = "x"; p.Why = "the narrow reason"; }); Add(e, "early", p => { p.OrderValue = 10; p.Why = "the broad reason"; });
            Assert.That(Collect(e, viewer), Has.Member("Right now: the broad reason."), "the Part that runs first owns the broadest condition (a kit still being carried)");
        }

        [Test] public void WithNoRowsAndNoReasonThereIsNoRightNowLine()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; });
            Assert.That(Collect(e, viewer).Any(l => l.StartsWith("Right now")), Is.False);
        }

        [Test] public void RowsFromPartsThatDoNotDescribeUseNeverAppear()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.OfferRow = true; }); e.AddPart(new Bystander());
            var now = Collect(e, viewer).Single(l => l.StartsWith("Right now"));
            Assert.That(now, Does.Not.Contain("unrelated"), "the block is the thing's own rows, not every row of the menu"); Assert.That(now, Does.Contain("probe the thing"));
        }

        [Test] public void NoViewerMeansNoRightNowLineButTheFactsRemain()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "Do it."; p.OfferRow = true; });
            var lines = Collect(e, null); Assert.That(lines, Has.Member("How to use: Do it.")); Assert.That(lines.Any(l => l.StartsWith("Right now")), Is.False);
        }

        [Test] public void ManyRowsAreSummarisedNotListedWithoutEnd()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; });
            var many = e.GetPart<ProbeManyRows>() ?? new ProbeManyRows(); e.AddPart(many);
            var now = Collect(e, viewer).Single(l => l.StartsWith("Right now you can"));
            Assert.That(now, Is.EqualTo("Right now you can: go 1, go 2, go 3, go 4 and 4 more."));
        }
        [Test] public void InertHeaderRowsOfAPickerMenuAreLeftOut()
        {
            var e = Subject(); Add(e, "a", p => { p.How = "x"; p.OfferRow = true; });
            e.AddPart(new ProbeInert());
            var now = Collect(e, viewer).Single(l => l.StartsWith("Right now"));
            Assert.That(now, Is.EqualTo("Right now you can: probe the thing."), "section headers and 'none carried' lines do nothing, so they are not something you can do");
        }
        sealed class ProbeInert : Part, IDescribeUse
        {
            public override string Name => "ProbeInert"; public int Order => 70; public void DescribeUse(DescribeContext c, UseGuide g) { }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "GetInventoryActions") return true; var rows = e.GetParameter<InventoryActionList>("Actions");
                rows.AddAction("Section", "== Heads / blades ==", InventoryAction.InertCommand); rows.AddAction("SectionEmpty", "    (none carried)", InventoryAction.InertCommand); return true;
            }
        }

        [Test] public void ACrowdOfRowsUnderOneHeadingIsSaidAsTheHeadingAndTheCount_ButAPairIsListed()
        {
            var e = Subject(); var crowd = new ProbeGrouped { Count = 8, Heading = ToolGroups.Lay, Prefix = "lay it " }; e.AddPart(crowd);
            Assert.That(Collect(e, viewer), Has.Member("Right now you can: lay a device here (8)."), "eight directions are one phrase, as the interact menu shows them");
            crowd.Count = 2; Assert.That(Collect(e, viewer), Has.Member("Right now you can: lay it 1, lay it 2."), "a pair is not a crowd");
            crowd.Count = ToolGroupCrowd; Assert.That(Collect(e, viewer), Has.Member("Right now you can: lay a device here (" + ToolGroupCrowd + ")."), "the menu's own threshold");
            crowd.Count = ToolGroupCrowd - 1; Assert.That(Collect(e, viewer).Single(l => l.StartsWith("Right now")), Does.Not.Contain("device"));
        }
        static int ToolGroupCrowd => WorldInteractionSystem.ToolGroupAt;
        sealed class ProbeGrouped : Part, IDescribeUse
        {
            public override string Name => "ProbeGrouped"; public int Order => 60; public int Count; public string Heading = "", Prefix = "";
            public void DescribeUse(DescribeContext c, UseGuide g) => g.How.Add("Lay them.");
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetInventoryActions") for (int i = 1; i <= Count; i++) e.GetParameter<InventoryActionList>("Actions").AddAction("Lay", Prefix + i, "Lay" + i).InGroup(Heading);
                return true;
            }
        }

        sealed class ProbeManyRows : Part, IDescribeUse
        {
            public override string Name => "ProbeMany"; public int Order => 60; public void DescribeUse(DescribeContext c, UseGuide g) { }
            public override bool HandleEvent(GameEvent e) { if (e.ID == "GetInventoryActions") for (int i = 1; i <= 8; i++) e.GetParameter<InventoryActionList>("Actions").AddAction("Go", "go " + i, "Go" + i); return true; }
        }

        [Test] public void APartWhoseRowsComeFromAServiceReportsThemThroughTheRowsSeam()
        {
            var e = Subject(); var svc = new ServiceBacked(); e.AddPart(svc);
            Assert.That(Collect(e, viewer), Has.Member("Right now you can: run the service."));
            Assert.AreSame(viewer, svc.AskedAs); Assert.AreSame(zone, svc.AskedIn, "the zone the thing is in is passed to the service");
            Assert.That(Collect(e, null).Any(l => l.StartsWith("Right now")), Is.False, "no viewer, no service call");
        }

        // ── who is asking, and for what ──

        [Test] public void ACarriedThingIsAskedAboutItsCarrier()
        {
            var item = scope.Factory.CreateEntity("Crate"); var probe = Add(item, "a", p => { p.How = "x"; p.OfferRow = true; });
            var pack = viewer.GetPart<InventoryPart>(); Assert.True(pack.AddObject(item));
            var lines = Collect(item, null, carried: true);
            Assert.That(lines, Has.Member("Right now you can: probe the thing."), "the carrier is the viewer when nobody is named");
            Assert.AreSame(viewer, probe.LastActor, "the rows were asked for as the carrier would ask");
        }

        [Test] public void AThingInsideAContainerIsNotAskedAboutAsIfTheContainerWereTheActor_Counter()
        {
            var item = scope.Factory.CreateEntity("Crate"); var probe = Add(item, "a", p => { p.How = "x"; p.OfferRow = true; });
            var chest = scope.Factory.CreateEntity("Crate"); Assert.True(zone.AddEntity(chest, 12, 10)); item.GetPart<PhysicsPart>().InInventory = chest;
            var lines = Collect(item, null, carried: true);
            Assert.That(lines, Has.Member("How to use: x")); Assert.That(lines.Any(l => l.StartsWith("Right now")), Is.False, "a chest is not someone who can act");
            Assert.IsNull(probe.LastActor, "no rows were asked of the chest");
        }

        [Test] public void DescribingIsReadOnly()
        {
            var e = Subject(); var p = Add(e, "a", x => { x.How = "x"; x.OfferRow = true; x.OfferDisabled = true; }); int parts = e.Parts.Count; int pack = viewer.GetPart<InventoryPart>().Objects.Count;
            int entities = zone.GetAllEntities().Count; MessageLog.Clear(); Diag.ResetAll();
            Collect(e, viewer); Collect(e, viewer);
            Assert.AreEqual(parts, e.Parts.Count); Assert.AreEqual(pack, viewer.GetPart<InventoryPart>().Objects.Count); Assert.AreEqual(2, p.Calls);
            Assert.AreEqual(entities, zone.GetAllEntities().Count, "no entity made or removed"); Assert.IsEmpty(MessageLog.GetRecent(5), "nothing is said to the player by asking");
            Assert.IsEmpty(Diag.Snapshot(20), "and nothing is recorded");
        }

        // ── both readers show it, ahead of the flavor ──

        [Test] public void TheQuickReaderShowsTheBlockBeforeTheFlavor()
        {
            var e = Subject(); e.GetPart<ExaminablePart>().Text = "A plain wooden crate."; Add(e, "a", p => { p.How = "Press C and choose probe."; p.OfferRow = true; });
            string text = e.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(e), viewer);
            int how = text.IndexOf("How to use: Press C and choose probe.", StringComparison.Ordinal), flavor = text.IndexOf("A plain wooden crate.", StringComparison.Ordinal);
            Assert.That(how, Is.GreaterThanOrEqualTo(0), text); Assert.That(flavor, Is.GreaterThan(how), "the how-to comes before the flavor");
            Assert.That(text, Does.Contain("Right now you can: probe the thing."));
        }

        sealed class StateLine : Part, IDescribeState
        {
            public override string Name => "UseStateProbe"; public ReaderScope Scope => ReaderScope.Both; public int Order => 5;
            public string DescribeState(DescribeContext context) => "It is jammed.";
        }

        [Test] public void TheFullDocumentHasAHowToUseSectionBetweenStateAndDescription()
        {
            var e = Subject(); e.GetPart<ExaminablePart>().Text = "A plain wooden crate."; e.AddPart(new StateLine()); Add(e, "a", p => { p.How = "Press C and choose probe."; p.Need = "a free hand"; });
            var doc = ExamineDocumentBuilder.BuildWorld(e, zone, zone.GetEntityCell(e), viewer); Assert.NotNull(doc);
            var titles = doc.Sections.Select(s => s.Heading).ToList(); int state = titles.IndexOf("Current state"), how = titles.IndexOf("How to use"), desc = titles.IndexOf("Description");
            Assert.That(state, Is.GreaterThanOrEqualTo(0), string.Join(",", titles)); Assert.That(how, Is.GreaterThan(state), "after what is going on right now"); Assert.That(desc, Is.GreaterThan(how), "and before the flavor");
            var body = doc.Sections[how].Text; Assert.That(body, Does.StartWith("Press C and choose probe."), "the heading says how to use; the first line does not repeat it"); Assert.That(body, Does.Not.Contain("How to use:")); Assert.That(body, Does.Contain("Needs: a free hand."));
        }

        [Test] public void AThingWithNoGuideGainsNoLineAndNoSection()
        {
            var e = Subject(); e.GetPart<ExaminablePart>().Text = "A plain wooden crate.";
            string text = e.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(e), viewer);
            Assert.That(text, Does.Not.Contain("How to use")); Assert.That(text, Does.Not.Contain("Right now"));
            var doc = ExamineDocumentBuilder.BuildWorld(e, zone, zone.GetEntityCell(e), viewer); Assert.NotNull(doc);
            Assert.That(doc.Sections.Select(s => s.Heading), Does.Not.Contain("How to use"));
        }
    }
}
