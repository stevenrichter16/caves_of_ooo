using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Laid devices and the hush strip (Docs/MACHINE-INVENTED-USES.md 3t). A strip is felt laid flat: whoever steps onto its cell is not heard by a hunter that perceives by sound (the pale stalkers), but a blow, a cast and
    /// the next step off the strip still are. The laying and the taking up are generic (any item that names a device blueprint), atomic, and cost only the item, which comes back whole when the strip is taken up.
    /// </summary>
    public sealed class HushStripTests : FiftyWorldFixture
    {
        [SetUp] public void SetupHush() { Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; }

        InventoryAction[] LayRows(Entity item) => Actions(item).Where(a => a.Name == "LayDevice").ToArray();
        InventoryAction Row(Entity item, string direction) => LayRows(item).Single(r => r.Display.EndsWith(" " + direction));
        int Strips() => Pack.Objects.Where(o => o.BlueprintName == "HushStrip").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        Entity Bundle(int n) { Entity first = null; for (int i = 0; i < n; i++) { var e = Carry("HushStrip"); first ??= e; } return Pack.Objects.First(o => o.BlueprintName == "HushStrip"); }
        Entity LaidAt(int x, int y) => Zone.GetCell(x, y).Objects.FirstOrDefault(e => e.BlueprintName == "HushStripLaid");
        Entity Lay(int x, int y) { var strip = Factory.CreateEntity("HushStripLaid"); Assert.True(Zone.AddEntity(strip, x, y)); Zone.GetCell(x, y).IsVisible = true; Zone.GetCell(x, y).Explored = true; return strip; }
        void Quiet() => Actor.IntProperties.Remove(Noise.Property);
        bool Heard() => Noise.Recent(Actor, 1);
        void Step(int dx = 1) => Assert.True(MovementSystem.TryMove(Actor, Zone, dx, 0));

        // ── laying ──

        [Test] public void AStackOffersOneRowPerOpenDirectionAndLayingSpendsExactlyOne()
        {
            var item = Bundle(4); Assert.AreEqual(4, Strips()); var rows = LayRows(item); Assert.AreEqual(8, rows.Length, string.Join(" / ", rows.Select(r => r.Display)));
            StringAssert.Contains("lay hush strip east", rows[0].Display + string.Join(",", rows.Select(r => r.Display))); StringAssert.Contains("take it up again", rows[0].Hint);
            Assert.True(Act(item, Row(item, "east").Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(3, Strips()); Assert.NotNull(LaidAt(11, 10), "the strip lies on the cell to the east"); Assert.NotNull(LaidAt(11, 10).GetPart<HushStripPart>());
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "DeviceLaid", Limit = 5 }).Records.Count);
        }

        [Test] public void OneDevicePerCell_ACellWithAStripIsNotOfferedAgain_Counter()
        {
            var item = Bundle(3); Assert.True(Act(item, Row(item, "east").Command)); var again = LayRows(Pack.Objects.First(o => o.BlueprintName == "HushStrip"));
            Assert.AreEqual(7, again.Length); Assert.False(again.Any(r => r.Display.EndsWith(" east")), "the east cell already holds a strip");
        }

        [Test] public void ACreatureOrASolidThingOnACellRefusesIt_Counter()
        {
            Place("CaveSlime", 11, 10); Place("StoneWall", 9, 10); var item = Bundle(2); var dirs = LayRows(item).Select(r => r.Display.Substring(r.Display.LastIndexOf(' ') + 1)).ToList();
            CollectionAssert.DoesNotContain(dirs, "east", "a creature is there"); CollectionAssert.DoesNotContain(dirs, "west", "a wall is there"); Assert.AreEqual(6, dirs.Count);
        }

        [Test] public void ALaidStripIsAnObjectWithNoStackAndNothingElseSpent()
        {
            var item = Bundle(1); Assert.True(Act(item, Row(item, "north").Command)); Assert.AreEqual(0, Strips(), "the single strip was the cost"); Assert.False(Pack.Objects.Any(o => o.BlueprintName == "HushStrip"));
            Assert.IsEmpty(Pack.Objects.Where(o => o.BlueprintName == "HushStripLaid"), "the laid strip is not in the pack");
        }

        [Test] public void AnInterruptedLayingLeavesTheStackWholeAndNoStripOnTheGround()
        {
            var item = Bundle(3); string command = Row(item, "east").Command; FailAfter();
            Assert.False(Act(item, command)); Assert.AreEqual(3, Strips()); Assert.IsNull(LaidAt(11, 10), "no strip was left behind");
        }

        [Test] public void AStaleOrMalformedLayCommandChangesNothing_Counter()
        {
            var item = Bundle(2); string good = Row(item, "east").Command;
            Assert.False(Act(item, "LayDevice|nowhere|1|1|2|2|" + item.ID)); Assert.False(Act(item, "LayDevice|")); Assert.False(Act(item, good.Replace("|" + item.ID, "|other")));
            var me = Zone.GetEntityPosition(Actor); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, me.x, me.y + 3)); Assert.False(Act(item, good), "you moved since the row was made");
            Assert.AreEqual(2, Strips()); Assert.IsNull(LaidAt(11, 10));
        }

        [Test] public void AnItemNotCarriedOffersNoRows_Counter()
        {
            var item = Factory.CreateEntity("HushStrip"); Assert.True(Zone.AddEntity(item, 12, 10)); Assert.IsEmpty(LayRows(item));
        }

        // ── the quiet ──

        [Test] public void AStepOntoAStripIsNotHeardAndTheNextStepOffItIs()
        {
            Lay(11, 10); Quiet(); Step(); Assert.False(Heard(), "the footfall on the felt is not heard"); Quiet(); Step(); Assert.True(Heard(), "the next step is on bare ground and is heard");
        }

        [Test] public void EveryWayOfLandingOnAStripIsQuiet_ATeleportingMoveAShoveAndASwap()
        {
            Lay(12, 10); Quiet(); Assert.True(MovementSystem.TryMoveTo(Actor, Zone, 12, 10)); Assert.False(Heard(), "a move to a chosen cell");
            var bare = Lay(13, 10); Quiet(); Assert.True(MovementSystem.ForceMoveTo(Actor, Zone, 13, 10)); Assert.False(Heard(), "a knock or a shove onto the strip"); Zone.RemoveEntity(bare);   // the swap below should land the other on bare ground
            var other = Place("CaveSlime", 14, 10); Lay(14, 10); Quiet(); other.IntProperties.Remove(Noise.Property); Assert.True(MovementSystem.TrySwap(Actor, other, Zone));
            Assert.False(Heard(), "the one who swaps onto the strip is not heard"); Assert.True(Noise.Recent(other, 1), "and the one swapped onto bare ground is");
        }

        [Test] public void TheNativeInputAndNavigationPathIsQuietOnAStripToo_AndHeardOffIt()
        {
            Lay(11, 10); Quiet(); var onto = MovementSystem.TryMoveDetailed(Actor, Zone, 1, 0); Assert.True(onto.Moved); Assert.False(Heard(), "the path the keyboard and NPC navigation use");
            Quiet(); var off = MovementSystem.TryMoveDetailed(Actor, Zone, 1, 0); Assert.True(off.Moved); Assert.True(Heard(), "and the next step is heard");
            Quiet(); Lay(13, 10); Assert.True(MovementSystem.TryMoveEx(Actor, Zone, 1, 0).moved); Assert.False(Heard(), "the compatibility entry point goes the same way");
        }

        [Test] public void ALaneOfStripsIsQuietForExactlyItsLength_Counter()
        {
            for (int x = 11; x <= 13; x++) Lay(x, 10); Quiet();
            for (int i = 0; i < 3; i++) { Step(); Assert.False(Heard(), "step " + i + " on the lane"); Quiet(); }
            Step(); Assert.True(Heard(), "the fourth step leaves the lane");
        }

        [Test] public void ABlowOrACastFromAStripIsStillHeard_Counter()
        {
            Lay(11, 10); Quiet(); Step(); Assert.False(Heard()); Noise.Mark(Actor); Assert.True(Heard(), "what CombatSystem and the spells do on every strike and cast");
        }

        [Test] public void ASoundHuntingStalkerDoesNotPerceiveAWalkerOnTheStripButDoesOnceTheyStepOff()
        {
            var stalker = Factory.CreateEntity("PaleStalker"); var hears = stalker.GetPart<HuntsBySoundPart>(); Assert.NotNull(hears);
            Lay(11, 10); Quiet(); Step(); hears.TryPerceive(Actor, Zone, out bool onStrip); Assert.False(onStrip);
            Step(); hears.TryPerceive(Actor, Zone, out bool off); Assert.True(off);
        }

        [Test] public void EveryoneIsQuietOnAStrip_NotOnlyThePlayer()
        {
            var walker = Place("CaveSlime", 12, 10); Lay(13, 10); walker.IntProperties.Remove(Noise.Property);
            Assert.True(MovementSystem.TryMove(walker, Zone, 1, 0)); Assert.False(Noise.Recent(walker, 1));
        }

        [Test] public void NoiseMarkStepWithNoCellIsTheOrdinaryHeardStep_Counter()
        {
            Quiet(); Noise.MarkStep(Actor, null); Assert.True(Heard()); Quiet(); Noise.MarkStep(Actor, Zone.GetCell(11, 10)); Assert.True(Heard(), "an empty cell is not felt");
        }

        // ── taking up ──

        [Test] public void TheStripCanBeTakenUpFromBesideItAndComesBackAsTheSameItem()
        {
            var strip = Lay(11, 10); var rows = WorldInteractionSystem.GatherActions(strip, Actor).Where(a => a.Name == "TakeUpDevice").ToArray(); Assert.AreEqual(1, rows.Length);
            Assert.True(Act(strip, rows[0].Command), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(1, Strips()); Assert.IsNull(LaidAt(11, 10), "the strip left the world");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "DeviceTakenUp", Limit = 5 }).Records.Count);
            var again = Pack.Objects.First(o => o.BlueprintName == "HushStrip"); Assert.True(Act(again, Row(again, "east").Command), "and it can be laid again");
        }

        [Test] public void TakingUpFromFarIsRefusedAndAFullPackKeepsTheStripWhereItLies_Counter()
        {
            var far = Lay(14, 10); Assert.IsEmpty(WorldInteractionSystem.GatherActions(far, Actor).Where(a => a.Name == "TakeUpDevice"), "three cells off is out of reach");
            var near = Lay(11, 10); Pack.MaxWeight = Pack.GetCarriedWeight(); var row = WorldInteractionSystem.GatherActions(near, Actor).Single(a => a.Name == "TakeUpDevice");
            Assert.False(Act(near, row.Command)); Assert.NotNull(LaidAt(11, 10), "the strip stays where it lies"); Assert.AreEqual(0, Strips());
        }

        [Test] public void AnInterruptedTakingUpLeavesTheStripAndAnEmptyPack()
        {
            var strip = Lay(11, 10); string command = WorldInteractionSystem.GatherActions(strip, Actor).Single(a => a.Name == "TakeUpDevice").Command; FailAfter();
            Assert.False(Act(strip, command)); Assert.NotNull(LaidAt(11, 10)); Assert.AreEqual(0, Strips());
        }

        [Test] public void ASpentDeviceHasNoTakeUpRowAndSaysSo_Counter()
        {
            var device = Factory.CreateEntity("HushStripLaid"); device.GetPart<LaidDevicePart>().ItemBlueprint = ""; Assert.True(Zone.AddEntity(device, 11, 10)); Zone.GetCell(11, 10).IsVisible = true; Zone.GetCell(11, 10).Explored = true;
            Assert.IsEmpty(WorldInteractionSystem.GatherActions(device, Actor).Where(a => a.Name == "TakeUpDevice")); var l = new List<string>(); StateReader.Collect(device, true, Zone, Zone.GetEntityCell(device), Actor, l); StringAssert.Contains("cannot be taken up", string.Join(" ", l));
        }

        // ── data and saves ──

        [Test] public void TheItemAndItsDeviceAgreeAndRideTheSave()
        {
            var item = Factory.CreateEntity("HushStrip"); var layable = item.GetPart<LayableDevicePart>(); Assert.AreEqual("HushStripLaid", layable.Device); var device = Factory.CreateEntity(layable.Device); Assert.AreEqual("HushStrip", device.GetPart<LaidDevicePart>().ItemBlueprint, "the device gives back the item that made it");
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(device); Assert.AreEqual("HushStrip", copy.GetPart<LaidDevicePart>().ItemBlueprint); Assert.NotNull(copy.GetPart<HushStripPart>());
            Assert.AreEqual("HushStripLaid", PartRoundTripHelper.RoundTripEntityViaTokenGraph(item).GetPart<LayableDevicePart>().Device);
        }

        [Test] public void TheReadersSayWhatItDoesAndWhatItDoesNot()
        {
            var item = Carry("HushStrip"); var l = new List<string>(); StateReader.Collect(item, true, Zone, null, Actor, l); StringAssert.Contains("one hush strip to a cell", string.Join(" ", l));
            var strip = Lay(11, 10); l.Clear(); StateReader.Collect(strip, true, Zone, Zone.GetEntityCell(strip), Actor, l); string text = string.Join(" ", l);
            StringAssert.Contains("Footfalls on it are not heard", text); StringAssert.Contains("Blows, spells and the sounding rod still are", text); StringAssert.Contains("take it up", text);
        }

        [Test] public void TheTinkerCanStockItAndItIsCheap()
        {
            Assert.True(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"HushStrip\""));
            Assert.LessOrEqual(Factory.CreateEntity("HushStrip").GetPart<CommercePart>().Value, 6, "strips are cheap; the lane is the cost");
        }
    }
}
