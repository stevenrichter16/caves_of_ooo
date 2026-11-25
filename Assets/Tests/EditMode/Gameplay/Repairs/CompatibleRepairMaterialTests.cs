using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CompatibleRepairMaterialTests : FiftyWorldFixture
    {
        const string Prepared = "PreparedLiningClay";

        [TestCase("RepairLinedWell")]
        [TestCase("ConnectedBatchPan")]
        public void TwoPreparedMeasuresRestoreTheSameStructuralFunction(string blueprint)
        {
            var fixture = Place(blueprint);
            Carry(Prepared);
            Carry(Prepared);
            var repair = fixture.GetPart<RepairablePart>();

            Assert.That(RepairablePart.BlocksFunction(fixture), Is.True);
            Assert.That(repair.TryRepair(Actor, Zone), Is.True);
            Assert.That(Count(Prepared), Is.Zero);
            Assert.That(RepairablePart.BlocksFunction(fixture), Is.False);
            Assert.That(repair.RepairedBy, Is.SameAs(Actor));
            Assert.That(repair.TryRepair(Actor, Zone), Is.False);
        }

        [Test]
        public void APartialBillCanCombineLegacyAndPreparedClay()
        {
            var well = Place("RepairLinedWell");
            Carry("FireClay");
            Carry(Prepared);

            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.True);
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count(Prepared), Is.Zero);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.True);
        }

        [Test]
        public void TheBillWithholdsFunctionFromIncompleteOrUnpreparedStock()
        {
            var well = Place("RepairLinedWell");
            var clay = Carry(Prepared);
            Carry("DryClaySpoil");
            Carry("MarlrootClod");
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(Count(Prepared), Is.EqualTo(1));
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);

            clay.GetPart<RepairMaterialPart>().Grade = "raw";
            Carry("FireClay");
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(Count("FireClay"), Is.EqualTo(1));
        }

        [Test]
        public void ExactOnlyStructuralFaultDoesNotAcceptTheNewGrade()
        {
            var well = Place("RepairLinedWell");
            well.GetPart<RepairablePart>().RecipeId = "clay-field-work-rig";
            Carry(Prepared);
            Carry(Prepared);
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(Count(Prepared), Is.EqualTo(2));
        }

        [Test]
        public void DifferentGradesCannotMergeIntoAUsableStack()
        {
            var plain = Carry(Prepared);
            var altered = Factory.CreateEntity(Prepared);
            altered.GetPart<RepairMaterialPart>().Grade = "raw";
            Assert.That(plain.GetPart<StackerPart>().CanStackWith(altered), Is.False);
        }

        [Test]
        public void LateFailureRefundsTheClayAndPreservesIndependentInjury()
        {
            var well = Place("RepairLinedWell");
            Carry(Prepared);
            Carry(Prepared);
            var hp = Actor.GetStat("Hitpoints");
            var before = hp.BaseValue;
            Actor.AddPart(new AfterAction(() =>
            {
                hp.BaseValue -= 1;
                throw new InvalidOperationException("interrupted repair");
            }));
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(Count(Prepared), Is.EqualTo(2));
            Assert.That(hp.BaseValue, Is.EqualTo(before - 1));
            Assert.That(Actor.GetStat("Hitpoints"), Is.SameAs(hp));
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
        }

        [Test]
        public void LateAliasCannotFinishOrDuplicateARefundedOwner()
        {
            var well = Place("RepairLinedWell");
            var clay = Carry(Prepared);
            Carry(Prepared);
            var alias = Factory.CreateEntity(Prepared);
            alias.ID = clay.ID;
            Actor.AddPart(new AfterAction(() => Pack.AddObject(alias)));
            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(Pack.Objects.Single(e => e.ID == alias.ID), Is.SameAs(alias));
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
        }

        [TestCase(Prepared, 2)]
        [TestCase(Prepared, 3)]
        [TestCase("FireClay", 2)]
        [TestCase("FireClay", 3)]
        public void LateDeathKeepsTheUnpaidRepairMaterialsWithTheGroundDrops(string blueprint, int quantity)
        {
            var well = Place("RepairLinedWell");
            var clay = Carry(blueprint);
            var stack = clay.GetPart<StackerPart>();
            stack.StackCount = quantity;
            ConnectedClayOriginPart origin = null;
            if (blueprint == "FireClay")
            {
                origin = new ConnectedClayOriginPart
                {
                    WorldKey = Guid.NewGuid().ToString("N"),
                    OwnerID = clay.ID,
                    Units = 3
                };
                clay.AddPart(origin);
            }
            var dropCell = Zone.GetEntityCell(Actor);
            Actor.AddPart(new AfterAction(() => CombatSystem.HandleDeath(Actor, null, Zone)));

            Assert.That(well.GetPart<RepairablePart>().TryRepair(Actor, Zone), Is.False);
            Assert.That(CombatSystem.IsDeathHandled(Actor), Is.True);
            Assert.That(well.GetPart<WellPart>().IsUsable, Is.False);
            Assert.That(Zone.GetEntityCell(clay), Is.SameAs(dropCell));
            Assert.That(Pack.Objects.Contains(clay), Is.False);
            Assert.That(stack.StackCount, Is.EqualTo(quantity));
            if (origin != null) Assert.That(origin.Units, Is.EqualTo(3));
        }

        private sealed class AfterAction : Part
        {
            private readonly Action callback;
            public AfterAction(Action callback) { this.callback = callback; }

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction") callback();
                return true;
            }
        }

        [Test]
        public void FreshCookSellsTwoMeasuresAndRepeatedCreationDoesNotRefill()
        {
            var oldFactory = TraderPart.Factory;
            try
            {
                TraderPart.Factory = Factory;
                var cook = Factory.CreateEntity("SpreadWaysideCook");
                var inventory = cook.GetPart<InventoryPart>();
                int Measures() => inventory.Objects.Where(e => e.BlueprintName == Prepared)
                    .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
                Assert.That(Measures(), Is.EqualTo(2));
                foreach (var clay in inventory.Objects.Where(e => e.BlueprintName == Prepared).ToArray())
                    inventory.RemoveObject(clay);
                cook.FireEventAndRelease(GameEvent.New("ObjectCreated"));
                Assert.That(Measures(), Is.Zero);
            }
            finally
            {
                TraderPart.Factory = oldFactory;
            }
        }
    }
}
