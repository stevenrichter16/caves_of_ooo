using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Setters' bench at Posy (Docs/MACHINE-INVENTED-USES.md 3w): lacquer (a tier of armor for each pitchpod resin) and the Bower sign (one glow-quartz, +5 Bower regard while worn), the two treatments the game had and no way to
    /// get, for someone whose eye the Sorters have seen. The regard the frame earns sets the highest tier worked. A garment holds two treatments at most; the cost is finite; nothing is sold.
    /// </summary>
    public sealed class SettingBenchTests : FiftyWorldFixture
    {
        [SetUp] public void SetupBench() { EnhancementFactory.ForceReinitialize(); Diag.ResetAll(); MessageLog.Clear(); PlayerReputation.Reset(); Pack.MaxWeight = 1000; }
        [TearDown] public void TearBench() { PlayerReputation.Reset(); }

        Entity StandingBench()
        {
            var bench = Factory.CreateEntity("SettingBench"); Assert.NotNull(bench); var me = Zone.GetEntityPosition(Actor); Assert.True(Zone.AddEntity(bench, me.x + 1, me.y));
            Zone.GetCell(me.x + 1, me.y).IsVisible = true; Zone.GetCell(me.x + 1, me.y).Explored = true; return bench;
        }
        InventoryAction[] Rows(Entity bench) => WorldInteractionSystem.GatherActions(bench, Actor).Where(a => a.Name == "SetPiece").ToArray();
        static void Regard(int n) => PlayerReputation.Set("BowerFolk", n);
        void Give(string blueprint, int n) { for (int i = 0; i < n; i++) Carry(blueprint); }
        int Got(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        string Lacquer(Entity item, int tier) => SettingBenchPart.LacquerPrefix + System.Uri.EscapeDataString(item.ID) + "|" + tier;
        string Sign(Entity item) => SettingBenchPart.SignPrefix + System.Uri.EscapeDataString(item.ID);
        EnhancementLacquered Lacquered(Entity e) => e.Parts.OfType<EnhancementLacquered>().FirstOrDefault();
        EnhancementEngraved Signed(Entity e) => e.Parts.OfType<EnhancementEngraved>().FirstOrDefault();
        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, Zone, Zone.GetEntityCell(e), Actor, l); return string.Join(" ", l); }

        [TestCase(false)] [TestCase(true)]
        public void CrowdedBenchKeepsLateGarmentAndItsFiniteCostVisible(bool shortResin)
        {
            var bench = StandingBench(); Regard(8); var items = new List<Entity>();
            for (int i = 0; i < 16; i++)
            {
                var item = Factory.CreateEntity("LeatherArmor"); item.RemovePart(item.GetPart<StackerPart>());
                item.GetPart<RenderPart>().DisplayName = "garment " + i.ToString("D2");
                Assert.True(Pack.AddObject(item)); items.Add(item);
            }
            Give("GlowQuartz", 1); if (!shortResin) Give("PitchpodResin", 1);
            var rows = Rows(bench); Assert.AreEqual(32, rows.Length);
            Assert.False(rows.Any(r => r.DisabledCode == "more"));
            var late = rows.Single(r => r.Display.Contains("garment 15") && r.Display.Contains("lacquer"));
            Assert.AreEqual(shortResin, late.IsDisabled);
            Assert.AreEqual(!shortResin, Act(bench, Lacquer(items[15], 1)));
            Assert.AreEqual(shortResin ? 0 : 1, Lacquered(items[15])?.Tier ?? 0);
            Assert.True(items.Take(15).All(e => Lacquered(e) == null));
            Assert.AreEqual(0, Got("PitchpodResin")); Assert.AreEqual(1, Got("GlowQuartz"));
            Assert.AreEqual(8, PlayerReputation.Get("BowerFolk"));
        }

        // ── the regard ladder ──

        [TestCase(-50, 0)] [TestCase(0, 0)] [TestCase(7, 0)] [TestCase(8, 1)] [TestCase(17, 1)] [TestCase(18, 2)] [TestCase(27, 2)] [TestCase(28, 3)] [TestCase(38, 4)] [TestCase(200, 4)]
        public void TheTierTheSettersWorkRisesWithTheRegardTheFrameEarnsAndStopsAtFour(int regard, int cap) => Assert.AreEqual(cap, SettingBenchPart.TierCap(regard));

        [Test] public void AStrangerGetsOneDisabledRowThatPointsAtTheFrame_AndNothingIsWorked_Counter()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 3); Regard(7); var rows = Rows(bench);
            Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("unknown-eye", rows[0].DisabledCode); StringAssert.Contains("frame by the well", rows[0].DisabledReason);
            Assert.False(Act(bench, Lacquer(armor, 1)), "the command is refused too"); Assert.Null(Lacquered(armor)); Assert.AreEqual(3, Got("PitchpodResin"));
        }

        // ── lacquer ──

        [Test] public void LacquerCostsATierInResinAndAddsThatMuchArmorWornOrNot()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 4); Regard(18); Assert.True(InventorySystem.Equip(Actor, armor)); int before = armor.GetPart<ArmorPart>().AV;
            Assert.True(Rows(bench).Any(r => !r.IsDisabled && r.Display.Contains("tier 2")), string.Join(" / ", Rows(bench).Select(r => r.Display)));
            Assert.True(Act(bench, Lacquer(armor, 2)), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(2, Got("PitchpodResin"), "two resin spent for tier two"); Assert.AreEqual(2, Lacquered(armor).Tier); Assert.AreEqual(before + 2, armor.GetPart<ArmorPart>().AV, "worn: at once");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "PieceLacquered", Limit = 5 }).Records.Count);
        }

        [Test] public void AHigherTierThanTheRegardAllowsIsNotOffered_AndRefusedIfForced_Counter()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 4); Regard(8);
            Assert.False(Rows(bench).Any(r => r.Display.Contains("tier 2")), "regard 8 allows tier one only"); Assert.False(Act(bench, Lacquer(armor, 2))); Assert.AreEqual(4, Got("PitchpodResin")); Assert.Null(Lacquered(armor));
            Assert.False(Act(bench, Lacquer(armor, 5)), "no such tier"); Assert.False(Act(bench, Lacquer(armor, 0)));
        }

        [Test] public void TooLittleResinIsADisabledRowWithTheNumbers_AndSpendsNothing()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 1); Regard(28); var row = Rows(bench).Single(r => r.Display.Contains("tier 3") && r.Display.Contains("lacquer"));
            Assert.True(row.IsDisabled); Assert.AreEqual("short-resin", row.DisabledCode); StringAssert.Contains("3 pitchpod resin", row.DisabledReason); Assert.False(Act(bench, Lacquer(armor, 3))); Assert.AreEqual(1, Got("PitchpodResin"));
        }

        [Test] public void APieceTakesOneLacquerAndTwoTreatmentsAtMost_Counter()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 6); Regard(28);
            Assert.True(Act(bench, Lacquer(armor, 1))); Assert.False(Act(bench, Lacquer(armor, 1)), "already lacquered"); Assert.AreEqual(5, Got("PitchpodResin"), "the refused second lacquer cost nothing");
            var row = Rows(bench).First(r => r.IsDisabled && r.DisabledCode == "already"); StringAssert.Contains("lacquered already", row.DisabledReason);
        }

        [Test] public void APieceAlreadyHoldingTwoTreatmentsIsRefused_Counter()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 2); Regard(28);
            Assert.True(ItemEnhancing.Apply(armor, "EnhancementProofed")); Assert.True(ItemEnhancing.Apply(armor, "EnhancementSteeped")); Assert.AreEqual(2, ItemEnhancing.CountEnhancements(armor));
            var row = Rows(bench).First(r => r.Display.Contains("lacquer")); Assert.True(row.IsDisabled); Assert.AreEqual("slot-cap", row.DisabledCode); Assert.False(Act(bench, Lacquer(armor, 1))); Assert.AreEqual(2, Got("PitchpodResin"));
        }

        [Test] public void OnlyArmorTakesLacquer_ButAnythingWornOrWieldedTakesTheSign_Counter()
        {
            var bench = StandingBench(); var dagger = Carry("Dagger"); Give("PitchpodResin", 2); Give("GlowQuartz", 1); Regard(28);
            Assert.False(Rows(bench).Any(r => r.Display.Contains("lacquer") && r.Display.Contains("dagger")), "no lacquer row for a blade"); MessageLog.Clear(); Assert.False(Act(bench, Lacquer(dagger, 1)), "forced, still refused"); StringAssert.Contains("Only armor takes lacquer", string.Join(" ", MessageLog.GetRecent(3)), "and it says why"); Assert.AreEqual(2, Got("PitchpodResin"));
            Assert.True(Act(bench, Sign(dagger)), string.Join("|", MessageLog.GetRecent(3))); Assert.NotNull(Signed(dagger)); Assert.AreEqual(0, Got("GlowQuartz"));
        }

        // ── the sign ──

        [Test] public void TheSignCostsOneQuartzAndGivesFiveRegardOnlyWhileWorn()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("GlowQuartz", 2); Regard(10); int baseline = PlayerReputation.Get("BowerFolk");
            Assert.True(Act(bench, Sign(armor)), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(1, Got("GlowQuartz")); Assert.AreEqual("BowerFolk", Signed(armor).Faction); Assert.AreEqual(baseline, PlayerReputation.Get("BowerFolk"), "carried, not worn: no regard yet");
            Assert.True(InventorySystem.Equip(Actor, armor)); Assert.AreEqual(baseline + 5, PlayerReputation.Get("BowerFolk"), "worn: the Bower-Folk think better of you"); Assert.True(InventorySystem.UnequipItem(Actor, armor)); Assert.AreEqual(baseline, PlayerReputation.Get("BowerFolk"), "off: exactly reverted");
        }

        [Test] public void ASignSetOnAWornPieceWorksAtOnce_AndItsRegardMayRaiseTheNextTier()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("GlowQuartz", 1); Regard(36); Assert.True(InventorySystem.Equip(Actor, armor)); Assert.AreEqual(3, SettingBenchPart.TierCap(PlayerReputation.Get("BowerFolk")));
            Assert.True(Act(bench, Sign(armor))); Assert.AreEqual(41, PlayerReputation.Get("BowerFolk")); Assert.AreEqual(4, SettingBenchPart.TierCap(PlayerReputation.Get("BowerFolk")), "wearing the sign is what the top tier asks");
        }

        [Test] public void NoQuartzIsADisabledSignRowThatSaysSo_Counter()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Regard(10); var row = Rows(bench).Single(r => r.Display.Contains("Bower sign")); Assert.True(row.IsDisabled); Assert.AreEqual("short-quartz", row.DisabledCode); Assert.False(Act(bench, Sign(armor))); Assert.Null(Signed(armor));
        }

        // ── stacks and safety ──

        [Test] public void OneOfAStackOfGarmentsIsTreatedAndTheRestStayPlain()
        {
            var bench = StandingBench(); var a = Carry("LeatherArmor"); Carry("LeatherArmor"); Give("PitchpodResin", 1); Regard(8); var stack = Pack.Objects.First(o => o.BlueprintName == "LeatherArmor"); Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount);
            Assert.True(Act(bench, Lacquer(stack, 1)), string.Join("|", MessageLog.GetRecent(3))); var all = Pack.Objects.Where(o => o.BlueprintName == "LeatherArmor").ToList(); Assert.AreEqual(2, all.Count, "the treated one is its own item");
            Assert.AreEqual(1, all.Count(o => Lacquered(o) != null)); Assert.AreEqual(1, all.Single(o => Lacquered(o) == null).GetPart<StackerPart>().StackCount);
        }

        [Test] public void AnInterruptedSettingKeepsTheResinAndTheStackAndLeavesThePieceUntreated()
        {
            var bench = StandingBench(); var a = Carry("LeatherArmor"); Carry("LeatherArmor"); Give("PitchpodResin", 2); Regard(18); var stack = Pack.Objects.First(o => o.BlueprintName == "LeatherArmor"); string command = Lacquer(stack, 2); FailAfter();
            Assert.False(Act(bench, command)); Assert.AreEqual(2, Got("PitchpodResin")); Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); Assert.True(Pack.Objects.All(o => Lacquered(o) == null));
        }

        [Test] public void AnInterruptedSettingOfASingleGarmentLeavesNoTreatmentAndTheWornArmorUnchanged()
        {
            var bench = StandingBench(); var armor = Carry("LeatherArmor"); Give("PitchpodResin", 2); Regard(18); Assert.True(InventorySystem.Equip(Actor, armor)); int av = armor.GetPart<ArmorPart>().AV; string command = Lacquer(armor, 2); FailAfter();
            Assert.False(Act(bench, command)); Assert.Null(Lacquered(armor), "no lacquer left on the worn piece"); Assert.AreEqual(av, armor.GetPart<ArmorPart>().AV, "and no armor value left over from the undone work"); Assert.AreEqual(2, Got("PitchpodResin"));
            var sword = Carry("Dagger"); Give("GlowQuartz", 1); string sign = Sign(sword); Assert.False(Act(bench, sign)); Assert.Null(Signed(sword)); Assert.AreEqual(1, Got("GlowQuartz"));
        }

        [Test] public void NothingIsOfferedFromAFar_Counter()
        {
            var bench = StandingBench(); Carry("LeatherArmor"); Give("PitchpodResin", 1); Regard(10); var me = Zone.GetEntityPosition(Actor); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, me.x + 5, me.y)); Assert.IsEmpty(Rows(bench));
        }

        // ── the world side ──

        [Test] public void TheReaderSaysWhatItDoesAndHowWellTheSettersKnowYourEye()
        {
            var bench = StandingBench(); Regard(0); StringAssert.Contains("have not seen your eye", Read(bench)); Regard(18); StringAssert.Contains("tier 2", Read(bench)); Regard(100); StringAssert.Contains("highest tier", Read(bench));
        }

        [Test] public void TheShippedRowPlacesOneBenchInPosyAndTheBenchIsStatelessInTheSave()
        {
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); var spec = InstallationRegistry.All.Single(s => s.Id == "posy-setting-bench"); Assert.AreEqual("Posy", spec.Place); Assert.AreEqual(InstallationRegistry.WorksKind, spec.Kind);
            using (var scope = new HaulingContentScope())
            {
                Assert.IsEmpty(InstallationRegistry.Validate(scope.Factory)); scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true); var zone = manager.GetZone(WorldMap.ToZoneID(5, 9, 0)); Assert.NotNull(zone);
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SettingBench"), "one bench in Posy"); Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SortingFrame"), "beside the one frame");
            }
            InstallationRegistry.ResetForTests(); Assert.NotNull(PartRoundTripHelper.RoundTripEntityViaTokenGraph(Factory.CreateEntity("SettingBench")).GetPart<SettingBenchPart>());
        }
    }
}
