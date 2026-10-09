using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native-menu and movement contracts for a mundane, single-use cord snare.</summary>
    public sealed class CombatSupplySnareTests : FiftyWorldFixture
    {
        string Lay(Entity cord, int x = 11, int y = 10) => Choice(cord, "LayCordSnare|",
            a => a.Command.Split('|')[4] == x.ToString() && a.Command.Split('|')[5] == y.ToString());
        Entity Snare() => Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "KnotflaxSnare");
        static int Amount(Entity cord) => cord.GetPart<StackerPart>()?.StackCount ?? 1;
        static void Amount(Entity cord, int count)
        { if (!cord.HasPart<StackerPart>()) cord.AddPart(new StackerPart()); cord.GetPart<StackerPart>().StackCount = count; }

        [Test] public void OrdinaryCordMenuLaysOneVisibleNonrecoverableSnareAndSpendsOneCoil()
        {
            var cord = Carry("KnotflaxCord"); Amount(cord, 3);
            Assert.True(Act(cord, Lay(cord))); Assert.AreEqual(2, Amount(cord));
            var snare = Snare(); Assert.AreEqual((11, 10), Zone.GetEntityPosition(snare));
            Assert.True(snare.GetPart<RenderPart>().Visible); Assert.False(snare.GetPart<PhysicsPart>().Solid);
            Assert.False(snare.GetPart<PhysicsPart>().Takeable); Assert.False(snare.HasPart<HarvestablePart>());
            Assert.False(Actions(snare).Any(a => a.Command == "Harvest"));
        }

        [TestCase(false)] [TestCase(true)]
        public void FirstCrossingRootsPlayerOrOtherCreatureButLeavesAttackAndSpellActionsAvailable(bool npc)
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            var mover = Actor;
            if (npc) { Zone.RemoveEntity(Actor); mover = Place("MarlbackScrabbler", 10, 10); }
            Assert.True(MovementSystem.TryMove(mover, Zone, 1, 0));
            var root = mover.GetEffect<RootedEffect>(); Assert.NotNull(root); Assert.AreEqual(2, root.Duration);
            Assert.False(root.AllowMovement(mover)); Assert.True(root.AllowAction(mover));
            Assert.False(MovementSystem.TryMove(mover, Zone, 1, 0));
            Assert.IsNull(Zone.GetEntityCell(snare)); Assert.False(Zone.GetReadOnlyEntities().Contains(snare));
            mover.RemoveEffect<RootedEffect>(); Assert.True(MovementSystem.TryMove(mover, Zone, 1, 0));
            Assert.True(MovementSystem.TryMove(mover, Zone, -1, 0)); Assert.IsNull(mover.GetEffect<RootedEffect>());
        }

        [Test] public void AnUnsnaredNeighborDoesNotRootTheSameCreature()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord, 11, 11)));
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.IsNull(Actor.GetEffect<RootedEffect>());
            Assert.NotNull(Snare());
        }

        [Test] public void AlliedFactionDoesNotMakeCordSafe()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); Zone.RemoveEntity(Actor);
            var ally = Place("Player", 10, 10); Assert.True(MovementSystem.TryMove(ally, Zone, 1, 0));
            Assert.NotNull(ally.GetEffect<RootedEffect>());
        }

        [Test] public void AForcedLandingTriggersTheSnareWithoutExtendingAnExistingRoot()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord)));
            var root = new RootedEffect(7); Assert.True(Actor.ApplyEffect(root));
            Assert.True(MovementSystem.ForceMoveTo(Actor, Zone, 11, 10));
            Assert.AreSame(root, Actor.GetEffect<RootedEffect>()); Assert.AreEqual(7, root.Duration);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void MultiCellFarFootTriggersOnceWhenItsAnchorDoesNotEnterTheSnare()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord, 11, 11)));
            Zone.RemoveEntity(Actor); Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0;0,1;1,1" });
            Assert.True(Zone.AddEntity(Actor, 9, 10)); Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0));
            Assert.AreEqual((10, 10), Zone.GetEntityPosition(Actor)); Assert.AreEqual(2, Actor.GetEffect<RootedEffect>().Duration);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void PropsDoNotSpendTheSnare()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            var prop = Place("RawMeat", 12, 10); Assert.True(MovementSystem.ForceMoveTo(prop, Zone, 11, 10));
            Assert.NotNull(Zone.GetEntityCell(snare)); Assert.IsNull(prop.GetEffect<RootedEffect>());
        }

        [TestCase("StoneWall")] [TestCase("MarlbackScrabbler")] [TestCase("RawMeat")]
        public void OccupiedGroundIsNotOfferedAndLateOccupationRejectsWithoutPayment(string occupant)
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord); Place(occupant, 11, 10);
            Assert.False(Actions(cord).Any(a => a.Command == command)); Assert.False(Act(cord, command));
            Assert.Contains(cord, Pack.Objects); Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void HereDuplicateSnareAndUnrelatedMaterialsAreNeverOffered()
        {
            var cord = Carry("KnotflaxCord"); Amount(cord, 3);
            Assert.False(Actions(cord).Any(a => a.Command.StartsWith("LayCordSnare|") && a.Command.Split('|')[4] == "10" && a.Command.Split('|')[5] == "10"));
            Assert.True(Act(cord, Lay(cord))); Assert.False(Actions(cord).Any(a => a.Command.StartsWith("LayCordSnare|") && a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10"));
            Assert.False(Actions(Carry("FireClay")).Any(a => a.Command.StartsWith("LayCordSnare|")));
        }

        [TestCase("moved")] [TestCase("dropped")] [TestCase("quantity")]
        public void StaleMenuRefusesWithoutPlacementOrPayment(string change)
        {
            var cord = Carry("KnotflaxCord"); Amount(cord, 3); string command = Lay(cord);
            if (change == "moved") Assert.True(Zone.MoveEntity(Actor, 9, 10));
            else if (change == "dropped") { Assert.True(Pack.RemoveObject(cord)); Assert.True(Zone.AddEntity(cord, 10, 10)); }
            else Amount(cord, 2);
            Assert.False(Act(cord, command)); Assert.AreEqual(change == "quantity" ? 2 : 3, Amount(cord));
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void OuterInventoryFailureRestoresExactCordAndRemovesOnlyItsStagedSnare()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord); FailAfter();
            Assert.False(Act(cord, command)); Assert.Contains(cord, Pack.Objects);
            Assert.AreSame(Actor, cord.GetPart<PhysicsPart>().InInventory);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void PlacedSnareAndOldSavedCordSurviveNativeEntitySerialization()
        {
            Carry("KnotflaxCord"); var saved = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Zone.RemoveEntity(Actor); Actor = saved; Assert.True(Zone.AddEntity(Actor, 10, 10));
            var cord = Pack.Objects.Single(e => e.BlueprintName == "KnotflaxCord"); Assert.True(Act(cord, Lay(cord)));
            var snare = Snare(); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(snare);
            Zone.RemoveEntity(snare); Assert.True(Zone.AddEntity(copy, 11, 10));
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.NotNull(Actor.GetEffect<RootedEffect>());
            Assert.IsNull(Zone.GetEntityCell(copy));
        }
    }
}
