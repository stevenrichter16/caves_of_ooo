using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CompatibleRepairMaterialTests : FiftyWorldFixture
    {
        const string Prepared = "PreparedLiningClay";
        const string Timber = "SalvagedTimber";
        const string FieldHaft = "FieldHaftComponent";

        [TestCase("ConnectedBatchPan")]
        [TestCase("RepairLinedWell")]
        [TestCase("AbandonedFieldWorkRig")]
        public void GradeBillFitsTheActionAndItsNarrowThreeLineHint(string blueprint)
        {
            var fixture = Place(blueprint);
            var position = Zone.GetEntityPosition(fixture);
            var row = CellMenu(position.x, position.y).Single(a => a.Command == RepairablePart.RepairCommand);
            const string bill = "2 measures of fire clay or prepared lining clay, in any mix";

            Assert.That(row.Display.Length, Is.LessThanOrEqualTo(41));
            Assert.That(row.Display, Does.EndWith("(2 measures)"));
            var lines = WorldActionMenuUI.WrapHint(row.Hint, 44, WorldActionMenuUI.MaxHintRows);
            Assert.That(string.Join(" ", lines), Does.Contain(bill));
            Assert.That(fixture.GetPart<RepairablePart>().Describe(), Does.Contain(bill));
            Assert.False(fixture.GetPart<RepairablePart>().Repaired);
        }

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

        [TestCase("FireClay", "FireClay")]
        [TestCase(Prepared, Prepared)]
        [TestCase("FireClay", Prepared)]
        public void FieldWorkRigUsesEitherLiningClayGradeForTheSameWorkshop(string first, string second)
        {
            var rig = Place("AbandonedFieldWorkRig");
            var repair = rig.GetPart<RepairablePart>();
            Carry(first);
            Carry(second);
            Assert.That(repair.RecipeId, Is.EqualTo("clay-field-work-rig"));
            Assert.False(ForgePart.IsNearForge(Actor, Zone));

            var position = Zone.GetEntityPosition(rig);
            var row = CellMenu(position.x, position.y).Single(a => a.Command == RepairablePart.RepairCommand);
            Assert.True(ChooseRow(row, position.x, position.y));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count(Prepared), Is.Zero);
            Assert.That(repair.RepairedBy, Is.SameAs(Actor));
            Assert.That(Zone.GetEntityPosition(rig), Is.EqualTo(position));
            Assert.That(RepairablePart.BlocksFunction(rig), Is.False);
            Assert.That(StationQuery.Resolve(Actor, Zone, "forge"), Is.SameAs(rig));
            Assert.False(ChooseRow(row, position.x, position.y));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldWorkRigRefusesSpoilAndInvalidPreparedGradeWithoutTakingTheGoodMeasure(bool invalidGrade)
        {
            var rig = Place("AbandonedFieldWorkRig");
            var available = Carry(invalidGrade ? "FireClay" : Prepared);
            var spoil = Carry("DryClaySpoil");
            var stock = new[] { available, spoil };
            if (invalidGrade)
            {
                var altered = Carry(Prepared);
                altered.GetPart<RepairMaterialPart>().Grade = "raw";
                stock = stock.Append(altered).ToArray();
            }
            var position = Zone.GetEntityPosition(rig);
            var row = CellMenu(position.x, position.y).Single(a => a.Command == RepairablePart.RepairCommand);

            Assert.False(ChooseRow(row, position.x, position.y));
            Assert.That(RepairablePart.BlocksFunction(rig), Is.True);
            Assert.That(rig.GetPart<RepairablePart>().RepairedBy, Is.Null);
            Assert.False(ForgePart.IsNearForge(Actor, Zone));
            foreach (var item in stock)
            {
                Assert.That(Pack.Objects.Single(e => e.ID == item.ID), Is.SameAs(item));
                Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
                Assert.That(item.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
                Assert.That(Zone.GetEntityCell(item), Is.Null);
            }
            if (invalidGrade) Assert.That(stock.Last().GetPart<RepairMaterialPart>().Grade, Is.EqualTo("raw"));
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
        public void TimberDressingBenchPrefersTwoTimberAndRefusesClay()
        {
            var bench = Place("SoddenDressingBench");
            var repair = bench.GetPart<RepairablePart>();
            var clay = Carry(Prepared);
            Carry(Prepared);
            var timber = Carry("SalvagedTimber");
            Assert.That(repair.RecipeId, Is.EqualTo("timber-dressing-bench"));
            var position = Zone.GetEntityPosition(bench);
            var row = CellMenu(position.x, position.y).Single(a => a.Command == RepairablePart.RepairCommand);
            Assert.False(ChooseRow(row, position.x, position.y));
            Assert.That(RepairablePart.BlocksFunction(bench), Is.True);
            Assert.That(Count("SalvagedTimber"), Is.EqualTo(1));
            Assert.That(Count(Prepared), Is.EqualTo(2));
            Assert.That(Pack.Objects, Does.Contain(timber));
            Assert.That(Pack.Objects, Does.Contain(clay));

            var haft = Carry(FieldHaft);
            Carry(Timber);
            Assert.True(ChooseRow(row, position.x, position.y));
            Assert.That(RepairablePart.BlocksFunction(bench), Is.False);
            Assert.That(Count("SalvagedTimber"), Is.Zero);
            Assert.That(Pack.Objects, Does.Contain(haft));
            Assert.That(clay.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
        }

        [Test]
        public void TimberDressingBenchSpendsOneExactHaftFromAStackInAMixedBill()
        {
            var bench = Place("SoddenDressingBench");
            var repair = bench.GetPart<RepairablePart>();
            var timber = Carry(Timber);
            var haft = Carry(FieldHaft);
            var stack = haft.GetPart<StackerPart>();
            stack.StackCount = 3;
            var position = Zone.GetEntityPosition(bench);
            var row = CellMenu(position.x, position.y).Single(a => a.Command == RepairablePart.RepairCommand);
            string quote = row.Display + " " + string.Join(" ", WorldActionMenuUI.WrapHint(row.Hint, 44, WorldActionMenuUI.MaxHintRows));
            string details = repair.Describe();
            bool applied = ChooseRow(row, position.x, position.y);

            Assert.True(applied, "The normal world action accepts the mixed physical bill.");
            Assert.That(quote, Does.Contain("salvaged timber").And.Contain("rough field haft"));
            Assert.That(details, Does.Contain("salvaged timber").And.Contain("rough field haft"));
            Assert.That(RepairablePart.BlocksFunction(bench), Is.False);
            Assert.That(repair.RepairedBy, Is.SameAs(Actor));
            CollectionAssert.DoesNotContain(Pack.Objects, timber);
            Assert.That(Pack.Objects.Single(item => item.BlueprintName == FieldHaft), Is.SameAs(haft));
            Assert.That(stack.StackCount, Is.EqualTo(2));
            Assert.False(ChooseRow(row, position.x, position.y));

            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(bench).GetPart<RepairablePart>();
            Assert.That(restored.Repaired, Is.True);
            Assert.That(restored.RepairedBy.ID, Is.EqualTo(Actor.ID));
            var restoredHaft = restored.RepairedBy.GetPart<InventoryPart>().Objects.Single(item => item.ID == haft.ID);
            Assert.That(restoredHaft.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
        }

        [TestCase("only-hafts")]
        [TestCase("altered")]
        [TestCase("assembled")]
        [TestCase("equipped")]
        public void TimberDressingBenchRefusesIneligibleSubstitutionWithoutPayment(string condition)
        {
            var bench = Place("SoddenDressingBench");
            var haft = Carry(FieldHaft);
            if (condition != "only-hafts") Carry(Timber);
            switch (condition)
            {
                case "only-hafts": haft.GetPart<StackerPart>().StackCount = 2; break;
                case "altered": haft.GetPart<WeaponComponentPart>().MaxStrengthBonus++; break;
                case "assembled":
                    Assert.True(WeaponForgingService.TryForge(Actor, Factory, Carry("PeatMalletHeadComponent"),
                        haft, Carry("LeatherBindingComponent"), out var assembled, out var reason), reason);
                    haft = assembled;
                    break;
                case "equipped": haft.GetPart<PhysicsPart>().Equipped = Actor; break;
            }
            var before = Pack.Objects.ToArray();
            int quantity = haft.GetPart<StackerPart>()?.StackCount ?? 1;
            var repair = bench.GetPart<RepairablePart>();

            Assert.False(repair.TryRepair(Actor, Zone));
            CollectionAssert.AreEqual(before, Pack.Objects);
            Assert.That(haft.GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(quantity));
            Assert.That(RepairablePart.BlocksFunction(bench), Is.True);
            Assert.That(repair.RepairedBy, Is.Null);
        }

        [Test]
        public void TimberDressingBenchReadsCurrentBillWithLegacySavedFlavor()
        {
            var bench = Place("SoddenDressingBench");
            var examine = bench.GetPart<ExaminablePart>();
            const string legacy = "A split-legged dressing bench. Sella needs two sound timber lengths to brace it. The abandoned cutting works lie two stretches east.";
            examine.Text = legacy;

            string flavor = examine.GetDescriptionText();

            StringAssert.DoesNotContain("Sella needs two sound timber lengths", flavor);
            StringAssert.Contains("The abandoned cutting works lie two stretches east", flavor);
            StringAssert.Contains("rough field haft", bench.GetPart<RepairablePart>().Describe());
            Assert.AreEqual(legacy, examine.Text);
        }

        [Test]
        public void TimberDressingBenchMixedPaymentRollsBackAfterLateFailure()
        {
            var bench = Place("SoddenDressingBench");
            var timber = Carry(Timber);
            var haft = Carry(FieldHaft);
            haft.GetPart<StackerPart>().StackCount = 2;
            var hp = Actor.GetStat("Hitpoints");
            int healthBefore = hp.BaseValue;
            bool interrupted = false;
            Actor.AddPart(new AfterAction(() =>
            {
                interrupted = true;
                hp.BaseValue--;
                throw new InvalidOperationException("interrupted mixed bench repair");
            }));

            Assert.False(bench.GetPart<RepairablePart>().TryRepair(Actor, Zone));
            Assert.True(interrupted, "The mixed payment must reach the post-action callback.");
            CollectionAssert.Contains(Pack.Objects, timber);
            CollectionAssert.Contains(Pack.Objects, haft);
            Assert.That(haft.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(hp.BaseValue, Is.EqualTo(healthBefore - 1));
            Assert.That(RepairablePart.BlocksFunction(bench), Is.True);
            Assert.That(bench.GetPart<RepairablePart>().RepairedBy, Is.Null);
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
