using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Sorters' frame at Posy (Docs/MACHINE-INVENTED-USES.md 3s): six places, one for each hue, that take found things chosen by the eye. Bright is worth two, dim one, a plain grey/white/black nothing, and a costly thing
    /// is refused; what is set in stays; each point is a small one-time non-tradable regard from the Bower-Folk, bounded by the six places, so it is earned once and never farmed.
    /// </summary>
    public sealed class SortingFrameTests : FiftyWorldFixture
    {
        [SetUp] public void SetupFrame() { Diag.ResetAll(); MessageLog.Clear(); PlayerReputation.Reset(); Pack.MaxWeight = 100000; }
        [TearDown] public void TearFrame() { PlayerReputation.Reset(); }

        Entity StandingFrame()
        {
            var frame = Factory.CreateEntity("SortingFrame"); Assert.NotNull(frame);
            var me = Zone.GetEntityPosition(Actor); Assert.True(Zone.AddEntity(frame, me.x + 1, me.y)); Zone.GetCell(me.x + 1, me.y).IsVisible = true; Zone.GetCell(me.x + 1, me.y).Explored = true; return frame;
        }
        SortingFramePart Part(Entity frame) => frame.GetPart<SortingFramePart>();
        InventoryAction[] Rows(Entity frame) => WorldInteractionSystem.GatherActions(frame, Actor).Where(a => a.Name == "SortPiece").ToArray();
        string Command(Entity item) => SortingFramePart.Prefix + Uri.EscapeDataString(item.ID);
        int Regard => PlayerReputation.Get("BowerFolk");
        /// <summary>A carried thing of a given colour code and worth, as a test control (the real blueprints differ in both).</summary>
        Entity Thing(string color, int value = 3, string blueprint = "FireMoss")
        {
            var item = Carry(blueprint); item.GetPart<RenderPart>().ColorString = color; var commerce = item.GetPart<CommercePart>(); if (commerce != null) commerce.Value = value; return item;
        }
        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, Zone, Zone.GetEntityCell(e), Actor, l); return string.Join(" ", l); }

        // ── the eye ──

        [TestCase("&r", 0, 1)] [TestCase("&R", 0, 2)] [TestCase("&w", 1, 1)] [TestCase("&W", 1, 2)] [TestCase("&g", 2, 1)] [TestCase("&G", 2, 2)]
        [TestCase("&c", 3, 1)] [TestCase("&C", 3, 2)] [TestCase("&b", 4, 1)] [TestCase("&B", 4, 2)] [TestCase("&m", 5, 1)] [TestCase("&M", 5, 2)]
        public void EachColourCodeIsOneOfSixHuesAndLowerCaseIsDimUpperCaseBright(string color, int hue, int power)
        {
            var item = Thing(color); Assert.True(SortingFramePart.TryHue(item, out int h, out int p)); Assert.AreEqual(hue, h); Assert.AreEqual(power, p);
        }

        [TestCase("&k")] [TestCase("&y")] [TestCase("&K")] [TestCase("&Y")] [TestCase("")] [TestCase("nonsense")]
        public void GreyWhiteBlackAndNoColourArePlain_Counter(string color)
        {
            var item = Thing(color); Assert.False(SortingFramePart.TryHue(item, out _, out _), color);
        }

        [Test] public void OnlyTheFirstColourCodeCountsAndANullThingIsPlain()
        {
            var item = Thing("&R^k"); Assert.True(SortingFramePart.TryHue(item, out int hue, out int power)); Assert.AreEqual(0, hue); Assert.AreEqual(2, power);
            Assert.False(SortingFramePart.TryHue(null, out _, out _));
        }

        // ── rows ──

        [Test] public void OnlyColouredCarriedThingsAreOffered_WithWhatEachIsWorth_AndPlainOnesAreNot_Counter()
        {
            var frame = StandingFrame(); Thing("&R", blueprint: "FireMoss"); Thing("&g", blueprint: "FrostLichen"); Thing("&Y", blueprint: "GlacierSalt"); Thing("&k", blueprint: "SparkRoot");
            var rows = Rows(frame); Assert.AreEqual(2, rows.Length, string.Join(" / ", rows.Select(r => r.Display)));
            Assert.True(rows.Any(r => r.Display.Contains("red place (striking)"))); Assert.True(rows.Any(r => r.Display.Contains("green place (dim)")));
            Assert.True(rows.All(r => !r.IsDisabled)); StringAssert.Contains("stays in the frame for good", rows[0].Hint);
        }

        [Test] public void WithNothingColouredOneDisabledRowSaysWhatItWants()
        {
            var frame = StandingFrame(); Thing("&Y", blueprint: "GlacierSalt"); var rows = Rows(frame); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("nothing-to-set", rows[0].DisabledCode); StringAssert.Contains("red, yellow, green, cyan, blue or magenta", rows[0].DisabledReason);
        }

        [Test] public void NothingIsOfferedFromAFar_Counter()
        {
            var frame = StandingFrame(); Thing("&R"); var me = Zone.GetEntityPosition(Actor); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, me.x + 5, me.y)); Assert.IsEmpty(Rows(frame));
        }

        [Test] public void AWornThingIsNotOffered_Counter()
        {
            var frame = StandingFrame(); var cloak = Carry("Cloak"); cloak.GetPart<RenderPart>().ColorString = "&R"; Assert.True(InventorySystem.Equip(Actor, cloak));
            Assert.IsEmpty(Rows(frame).Where(r => !r.IsDisabled), "what you wear is not a found thing you carry");
        }

        // ── setting a piece ──

        [Test] public void ADimPieceTakesAPlaceSpendsOneOfAStackAndPaysItsRegardOnce()
        {
            var frame = StandingFrame(); var a = Thing("&r"); var b = Thing("&r"); Assert.AreEqual(2, Pack.Objects.Where(o => o.BlueprintName == "FireMoss").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), "fixture: two moss, one stack");
            Assert.True(Act(frame, Command(a)), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual("100000", Part(frame).Places); Assert.AreEqual(1, Part(frame).PaidPoints); Assert.AreEqual(SortingFramePart.RegardPerPoint, Regard);
            Assert.AreEqual(1, Pack.Objects.Where(o => o.BlueprintName == "FireMoss").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), "one went in, one stays");
            StringAssert.Contains("red place", string.Join(" ", MessageLog.GetRecent(5))); Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "FramePieceSet", Limit = 5 }).Records.Count);
        }

        [Test] public void AStrikingPieceBettersADimOneOnce_AndANothingBetterIsRefusedAndKept()
        {
            var frame = StandingFrame(); var dim = Thing("&r", blueprint: "FireMoss"); Assert.True(Act(frame, Command(dim)));
            var bright = Thing("&R", blueprint: "FrostLichen"); Assert.True(Act(frame, Command(bright))); Assert.AreEqual("200000", Part(frame).Places); Assert.AreEqual(2, Part(frame).PaidPoints); Assert.AreEqual(2 * SortingFramePart.RegardPerPoint, Regard, "total two points, paid once each");
            var again = Thing("&R", blueprint: "GlacierSalt"); var row = Rows(frame).Single(r => r.IsDisabled && r.DisabledCode == "no-better"); Assert.True(row.IsDisabled);
            Assert.False(Act(frame, Command(again))); Assert.True(Pack.Objects.Contains(again), "a refused piece is kept"); Assert.AreEqual(2 * SortingFramePart.RegardPerPoint, Regard);
            var dimAgain = Thing("&r", blueprint: "SparkRoot"); Assert.False(Act(frame, Command(dimAgain)), "a dim piece cannot better a striking one");
        }

        [Test] public void AFrameWithEveryPlaceStrikingIsWhole_PaysItsOneTimeBonus_AndNeverAgain()
        {
            var frame = StandingFrame(); var colours = new[] { "&R", "&W", "&G", "&C", "&B", "&M" }; var names = new[] { "FireMoss", "FrostLichen", "GlacierSalt", "SparkRoot", "GlimmerBrine", "EmberFruit" };
            for (int i = 0; i < 6; i++) { var piece = Thing(colours[i], blueprint: names[i]); Assert.True(Act(frame, Command(piece)), i + ": " + string.Join("|", MessageLog.GetRecent(2))); }
            Assert.AreEqual("222222", Part(frame).Places); Assert.True(Part(frame).WholePaid); Assert.AreEqual(12 * SortingFramePart.RegardPerPoint + SortingFramePart.WholeRegard, Regard);
            StringAssert.Contains("whole", Read(frame)); int before = Regard; var extra = Thing("&R", blueprint: "PitchpodResin"); Assert.False(Act(frame, Command(extra))); Assert.AreEqual(before, Regard, "nothing more to earn");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "FramePieceSet", Limit = 20 }).Records.Count(r => r.PayloadJson.Contains("\"whole\":true")), "the whole bonus is paid exactly once");
        }

        [Test] public void AWholeBonusAlreadyPaidIsNeverPaidAgain_EvenIfTheLastPlaceIsFilledLater_Counter()
        {
            var frame = StandingFrame(); Part(frame).Places = "222221"; Part(frame).PaidPoints = 11; Part(frame).WholePaid = true;   // state as a save could hold it: the bonus was paid, then a place was somehow dim again
            var piece = Thing("&M"); Assert.True(Act(frame, Command(piece))); Assert.AreEqual("222222", Part(frame).Places); Assert.AreEqual(SortingFramePart.RegardPerPoint, Regard, "only the one point is paid, never the whole bonus twice");
        }

        [Test] public void NoOrderOfPlacementsCanEarnMoreThanTheFramesBound_ForAnySequence()
        {
            int bound = 12 * SortingFramePart.RegardPerPoint + SortingFramePart.WholeRegard; var rng = new Random(12345); var names = new[] { "FireMoss", "FrostLichen", "GlacierSalt", "SparkRoot", "GlimmerBrine", "EmberFruit" };
            for (int trial = 0; trial < 40; trial++)
            {
                PlayerReputation.Reset(); var frame = StandingFrame();
                for (int step = 0; step < 30; step++)
                {
                    string color = "&" + "rRwWgGcCbBmM"[rng.Next(12)]; var piece = Thing(color, blueprint: names[rng.Next(names.Length)]); Act(frame, Command(piece));
                    Assert.LessOrEqual(Regard, bound, "trial " + trial + " step " + step);
                }
                Assert.AreEqual(Part(frame).PaidPoints * SortingFramePart.RegardPerPoint + (Part(frame).WholePaid ? SortingFramePart.WholeRegard : 0), Regard, "regard is exactly what the frame says it paid");
                Zone.RemoveEntity(frame);
            }
        }

        // ── what is refused ──

        [Test] public void APreciousThingIsRefusedWithAReason_ButOneAtTheLimitIsTaken_Counter()
        {
            var frame = StandingFrame(); var costly = Thing("&R", SortingFramePart.CheapLimit + 1, "FireMoss"); var cheap = Thing("&G", SortingFramePart.CheapLimit, "FrostLichen");
            var row = Rows(frame).Single(r => r.IsDisabled && r.DisabledCode == "precious"); Assert.True(row.IsDisabled); StringAssert.Contains("only itself", row.DisabledReason);
            Assert.False(Act(frame, Command(costly))); Assert.True(Pack.Objects.Contains(costly)); Assert.AreEqual("000000", Part(frame).Places);
            Assert.True(Act(frame, Command(cheap)), "worth exactly the limit is still cheap"); Assert.AreEqual("002000", Part(frame).Places);
        }

        [Test] public void AThingThatIsNotYoursToGiveIsRefused_Counter()
        {
            var frame = StandingFrame(); var key = Thing("&R", 1, "FireMoss"); key.SetTag("NoTrade"); var row = Rows(frame).Single(r => r.IsDisabled && r.DisabledCode == "not-giveable"); Assert.True(row.IsDisabled);
            Assert.False(Act(frame, Command(key))); Assert.AreEqual("000000", Part(frame).Places); Assert.AreEqual(0, Regard);
        }

        [Test] public void StaleOrMalformedCommandsChangeNothing_Counter()
        {
            var frame = StandingFrame(); var plain = Thing("&Y"); Assert.False(Act(frame, Command(plain)), "a plain thing cannot be set"); Assert.False(Act(frame, SortingFramePart.Prefix + "nobody")); Assert.False(Act(frame, SortingFramePart.Prefix));
            Assert.AreEqual("000000", Part(frame).Places); Assert.AreEqual(0, Regard); Assert.True(Pack.Objects.Contains(plain));
        }

        // ── safety ──

        [Test] public void AnInterruptedSettingKeepsThePieceLeavesTheFrameAsItWasAndPaysNothing()
        {
            var frame = StandingFrame(); var piece = Thing("&R"); string command = Command(piece); FailAfter();
            Assert.False(Act(frame, command)); Assert.True(Pack.Objects.Contains(piece), "the piece is still in the pack"); Assert.AreEqual("000000", Part(frame).Places); Assert.AreEqual(0, Part(frame).PaidPoints); Assert.False(Part(frame).WholePaid); Assert.AreEqual(0, Regard);
        }

        [Test] public void TheFramesStateRidesTheSave()
        {
            var frame = StandingFrame(); Assert.True(Act(frame, Command(Thing("&R")))); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(frame).GetPart<SortingFramePart>();
            Assert.AreEqual(Part(frame).Places, copy.Places); Assert.AreEqual(Part(frame).PaidPoints, copy.PaidPoints); Assert.AreEqual(Part(frame).WholePaid, copy.WholePaid);
        }

        [Test] public void AMalformedSavedPlacesStringReadsAsEmptyNotAsACrash_Counter()
        {
            var frame = StandingFrame(); Part(frame).Places = "9"; Assert.DoesNotThrow(() => Read(frame)); Thing("&R"); Assert.DoesNotThrow(() => Rows(frame));
            Part(frame).Places = "x2z"; Assert.DoesNotThrow(() => Read(frame));
        }

        // ── reading ──

        [Test] public void TheReaderSaysWhatIsPlacedWhatIsWantedAndWhatCouldBeBettered_NeverAScore()
        {
            var frame = StandingFrame(); StringAssert.Contains("empty", Read(frame)); StringAssert.Contains("red, yellow, green, cyan, blue, magenta", Read(frame));
            Assert.True(Act(frame, Command(Thing("&r")))); Assert.True(Act(frame, Command(Thing("&G", blueprint: "FrostLichen")))); string line = Read(frame);
            StringAssert.Contains("Placed: red (dim), green (striking).", line); StringAssert.Contains("wants a colour it does not have: yellow, cyan, blue, magenta", line); StringAssert.Contains("A striking piece would better: red", line);
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(line, @"\b\d+ (point|regard)"), "no score is shown");
        }

        // ── the world side ──

        [Test] public void TheShippedRowPlacesOneFrameInPosyBesideTheWellAndKeepsTheVillage()
        {
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults();
            var spec = InstallationRegistry.All.Single(s => s.Id == "posy-sorting-frame"); Assert.AreEqual("Posy", spec.Place); Assert.AreEqual(InstallationRegistry.WorksKind, spec.Kind); Assert.True(spec.Retain, "a stateful part keeps its village"); Assert.AreEqual("", spec.Site);
            using (var scope = new HaulingContentScope())
            {
                Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory)); scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = manager.GetZone(WorldMap.ToZoneID(5, 9, 0)); Assert.NotNull(zone);
                var frames = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "SortingFrame").ToList(); Assert.AreEqual(1, frames.Count, "one frame in Posy");
                Assert.NotNull(frames[0].GetPart<SortingFramePart>()); Assert.AreEqual("000000", frames[0].GetPart<SortingFramePart>().Places);
            }
            InstallationRegistry.ResetForTests();
        }

        [Test] public void OnlyAWorksOrSiteOrForgeRowIsAValidKind_AWorksRowNeverAlsoNamesASite()
        {
            InstallationRegistry.ResetForTests();
            string Row(string extra) => "{\"Version\":1,\"Installations\":[{\"Id\":\"x\",\"Place\":\"Posy\",\"Blueprint\":\"SortingFrame\",\"Retention\":\"Zone\",\"Radius\":9,\"MaxProbe\":48,\"Revision\":1,\"Purpose\":\"p\",\"Alternative\":\"a\",\"Basis\":\"b\"" + extra + "}],\"Pending\":[]}";
            Assert.IsEmpty(InstallationRegistry.InitializeFromJson(Row(",\"Kind\":\"Works\"")));
            Assert.That(InstallationRegistry.InitializeFromJson(Row(",\"Kind\":\"Mill\"")), Has.Some.StartsWith("E16"), "an unknown kind is refused");
            Assert.That(InstallationRegistry.InitializeFromJson(Row(",\"Kind\":\"Works\",\"Site\":\"Pump\"")), Has.Some.StartsWith("E16"), "a works row is not also a site body");
            Assert.IsEmpty(InstallationRegistry.InitializeFromJson(Row("")), "no kind is the default forge row");
            InstallationRegistry.ResetForTests();
        }
    }
}
