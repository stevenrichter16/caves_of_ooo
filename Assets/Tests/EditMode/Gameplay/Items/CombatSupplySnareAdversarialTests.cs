using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent parser, ownership, callback, spatial and save probes.</summary>
    public sealed class CombatSupplySnareAdversarialTests : FiftyWorldFixture
    {
        string Lay(Entity cord) => Choice(cord, "LayCordSnare|", a => a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10");
        Entity Snare() => Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "KnotflaxSnare");
        static void Contact(Entity snare, Entity actor, Cell cell)
        {
            var e = GameEvent.New("EntityEnteredCell"); e.SetParameter("Actor", actor); e.SetParameter("Cell", cell);
            snare.FireEventAndRelease(e);
        }

        [TestCase("LayCordSnare|")] [TestCase("LayCordSnare|fifty-world-tests|10|10|2147483648|10|1")]
        [TestCase("LayCordSnare|fifty-world-tests|10|10|-2147483648|10|1")]
        [TestCase("LayCordSnare|fifty-world-tests|10|10|11|10|0")]
        [TestCase("LayCordSnare|fifty-world-tests|10|10|11|10|1|extra")]
        [TestCase("LayCordSnare|other-zone|10|10|11|10|1")]
        [TestCase("LayCordSnare|fifty-world-tests|10|10|40|10|1")]
        public void MalformedOrForgedSelectionsCannotPlaceOrConsume(string command)
        {
            var cord = Carry("KnotflaxCord"); Assert.False(Act(cord, command)); Assert.Contains(cord, Pack.Objects);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [TestCase("frozen")] [TestCase("stunned")] [TestCase("dead")]
        public void UnavailableActorCannotUseAlreadyOpenedChoice(string state)
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord);
            if (state == "frozen") Actor.ApplyEffect(new FrozenEffect(1));
            else if (state == "stunned") Actor.ApplyEffect(new StunnedEffect(2));
            else Actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(cord, command)); Assert.Contains(cord, Pack.Objects);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void DroppedAndEquippedAliasesCannotOfferSnarePlacement()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord);
            cord.GetPart<PhysicsPart>().Equipped = Actor;
            Assert.False(Actions(cord).Any(a => a.Command.StartsWith("LayCordSnare|"))); Assert.False(Act(cord, command));
            Assert.Contains(cord, Pack.Objects);
        }

        [Test] public void MissingFactoryRefusesWithoutCreatingInventedFallbackTrap()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord); HarvestablePart.Factory = null;
            Assert.False(Actions(cord).Any(a => a.Command.StartsWith("LayCordSnare|"))); Assert.False(Act(cord, command));
            Assert.Contains(cord, Pack.Objects);
        }

        [Test] public void MissingBlueprintRefusesWithoutCreatingInventedFallbackTrap()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord); Factory.Blueprints.Remove("KnotflaxSnare");
            Assert.False(Act(cord, command)); Assert.Contains(cord, Pack.Objects);
        }

        [Test] public void UnrelatedCreatureAtAnotherCellCannotTriggerForgedEntry()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            Contact(snare, Actor, Zone.GetCell(11, 10)); Assert.True(CordSnarePart.IsArmed(snare));
            Assert.IsNull(Actor.GetEffect<RootedEffect>());
        }

        [Test] public void WrongEventCellCannotTriggerEvenWithActorStandingOnSnare()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            Assert.True(Zone.MoveEntity(Actor, 11, 10)); Contact(snare, Actor, Zone.GetCell(12, 10));
            Assert.True(CordSnarePart.IsArmed(snare)); Assert.IsNull(Actor.GetEffect<RootedEffect>());
        }

        [TestCase(false)] [TestCase(true)]
        public void DetachedOrSpentSnareCannotTriggerFromRetainedReference(bool spent)
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            if (spent) snare.GetPart<CordSnarePart>().Spent = true; else Zone.RemoveEntity(snare);
            Assert.True(Zone.MoveEntity(Actor, 11, 10)); Contact(snare, Actor, Zone.GetCell(11, 10));
            Assert.IsNull(Actor.GetEffect<RootedEffect>()); Assert.False(CordSnarePart.IsArmed(snare));
        }

        [Test] public void MultiCellBodyHoleDoesNotTriggerAdjacentCord()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            Zone.RemoveEntity(Actor); Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1;1,1" });
            Assert.True(Zone.AddEntity(Actor, 9, 10)); Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0));
            Assert.IsNull(Actor.GetEffect<RootedEffect>()); Assert.True(CordSnarePart.IsArmed(snare));
        }

        [Test] public void FarFootOccupancyPreventsPlacingUnderAnAlreadyPresentCreature()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord);
            var other = Factory.CreateEntity("MarlbackScrabbler"); other.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0" });
            Assert.True(Zone.AddEntity(other, 12, 10)); Assert.False(Act(cord, command)); Assert.Contains(cord, Pack.Objects);
        }

        [Test] public void ExpiredHoldDoesNotMakeLaterTrapIneffective()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); Actor.ApplyEffect(new RootedEffect(0));
            Assert.True(MovementSystem.ForceMoveTo(Actor, Zone, 11, 10)); Assert.AreEqual(2, Actor.GetEffect<RootedEffect>().Duration);
        }

        [Test] public void ReplayingAConsumedContactDoesNotStackOrMintACoil()
        {
            var cord = Carry("KnotflaxCord"); Assert.True(Act(cord, Lay(cord))); var snare = Snare();
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Contact(snare, Actor, Zone.GetCell(11, 10));
            Assert.AreEqual(2, Actor.GetEffect<RootedEffect>().Duration); Assert.AreEqual(0, Count("KnotflaxCord"));
            Assert.True(snare.GetPart<CordSnarePart>().Spent);
        }

        [Test] public void RollbackDoesNotGrantAFreeHoldToAnActorMovedByAnAfterActionHook()
        {
            var cord = Carry("KnotflaxCord"); var command = Lay(cord);
            Actor.AddPart(new SnareMoveThenFailPart()); Assert.False(Act(cord, command));
            Assert.IsNull(Actor.GetEffect<RootedEffect>()); Assert.Contains(cord, Pack.Objects);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "KnotflaxSnare"));
        }

        [Test] public void SpentStateRoundTripsWithoutRearming()
        {
            var snare = Factory.CreateEntity("KnotflaxSnare"); snare.GetPart<CordSnarePart>().Spent = true;
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(snare); Assert.True(Zone.AddEntity(copy, 11, 10));
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.IsNull(Actor.GetEffect<RootedEffect>());
            Assert.True(copy.GetPart<CordSnarePart>().Spent);
        }

        [Test] public void BlueprintDefinesInspectionAndSafeHandlingWithoutRecoverableYield()
        {
            var snare = Factory.CreateEntity("KnotflaxSnare"); Assert.NotNull(snare.GetPart<ExaminablePart>());
            Assert.NotNull(snare.GetPart<CordSnarePart>()); Assert.False(snare.GetPart<PhysicsPart>().Takeable);
            Assert.False(snare.HasPart<HarvestablePart>()); Assert.False(snare.HasTag("Item"));
        }
    }
    public sealed class SnareMoveThenFailPart : Part
    {
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "AfterInventoryAction") return true;
            MovementSystem.ForceMoveTo(ParentEntity, SettlementRuntime.ActiveZone, 11, 10);
            throw new InvalidOperationException("snare rollback after forced movement probe");
        }
    }
}
