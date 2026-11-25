using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal sealed class ConnectedKitchenFixture : IDisposable
    {
        internal const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        readonly TurnManager oldClock = TurnManager.Active;
        readonly EntityFactory oldFactory = CropSystem.Factory;
        readonly Zone oldZone = SettlementRuntime.ActiveZone;
        internal readonly EntityFactory Factory;
        internal readonly Zone Zone = new Zone("Overworld.12.11.0");
        internal readonly TurnManager Clock;
        internal readonly Entity Player, Worker, Pan, Escrow, Pickup;
        internal readonly Part Batch;
        internal InventoryPart Pack => Player.GetPart<InventoryPart>();
        internal ContainerPart Stored => Escrow.GetPart<ContainerPart>();
        internal ContainerPart Finished => Pickup.GetPart<ContainerPart>();
        internal string State => Field<string>(Batch, "State");
        internal ConnectedKitchenFixture()
        {
            Type batchType = typeof(Part).Assembly.GetType("CavesOfOoo.Core.KitchenBatchPart");
            Assert.NotNull(batchType, "The repaired pan needs actual paid work, saved ingredients and a physical return result.");
            Assert.NotNull(typeof(Part).Assembly.GetType("CavesOfOoo.Core.FieldMealPart"));
            Clock = new TurnManager();
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            // Isolate runtime contracts from the parallel content integration.
            var mealTemplate = Factory.Blueprints["ToastedEmberwheat"];
            var mealBlueprint = new Blueprint { Name = "FieldMeal", Baked = true };
            foreach (var part in mealTemplate.Parts) mealBlueprint.Parts[part.Key] = new Dictionary<string, string>(part.Value);
            foreach (var tag in mealTemplate.Tags) mealBlueprint.Tags[tag.Key] = tag.Value;
            mealBlueprint.Parts.Remove("Food"); mealBlueprint.Parts["FieldMeal"] = new Dictionary<string, string>();
            mealBlueprint.Parts["Physics"]["Weight"] = "1";
            Factory.Blueprints["FieldMeal"] = mealBlueprint;
            CropSystem.Factory = Factory; SettlementRuntime.ActiveZone = Zone;
            Player = Person("KitchenPlayer", 40); Player.SetTag("Player");
            Worker = Person("SpreadWaysideCook", 20); Worker.AddPart(new BrainPart { Passive = true });
            Player.IntProperties[TradeSystem.CURRENCY_PROP] = 10; Worker.IntProperties[TradeSystem.CURRENCY_PROP] = 35;
            Pan = Ground("ConnectedBatchPan"); Pan.AddPart(new RepairablePart { Repaired = true });
            Pan.AddPart(new CompositionPart { MaterialsRaw = "Masonry" });
            Escrow = Ground("ConnectedKitchenEscrow"); Escrow.AddPart(new ContainerPart { MaxItems = 3, Locked = true });
            Pickup = Ground("ConnectedKitchenPickup"); Pickup.AddPart(new ContainerPart { MaxItems = 1 });
            Zone.AddEntity(Player, 9, 10); Zone.AddEntity(Pan, 10, 10); Zone.AddEntity(Worker, 10, 9);
            Zone.AddEntity(Escrow, 11, 10); Zone.AddEntity(Pickup, 10, 11);
            Batch = (Part)Activator.CreateInstance(batchType); Pan.AddPart(Batch);
            Assert.True((bool)Call(Batch, "Configure", Zone, Worker, Escrow, Pickup));
            Give("Emberwheat", 2); Give("ClaspbeanPulp", 1);
        }
        static Entity Person(string blueprint, int hp)
        {
            var e = Ground(blueprint); e.SetTag("Creature"); e.AddPart(new InventoryPart());
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Max = hp, Owner = e };
            return e;
        }
        internal static Entity Ground(string blueprint)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = blueprint }; e.AddPart(new PhysicsPart { Takeable = false });
            e.AddPart(new RenderPart { Visible = true }); e.AddPart(new ExaminablePart()); return e;
        }
        internal Entity Give(string blueprint, int count)
        {
            var item = Factory.CreateEntity(blueprint); Assert.NotNull(item);
            if (item.GetPart<StackerPart>() is StackerPart stack) stack.StackCount = count;
            else Assert.AreEqual(1, count);
            Assert.True(Pack.AddObject(item)); return item;
        }
        internal int Units(string blueprint) => Pack.Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        internal bool Start() => (bool)Call(Batch, "TryStart", Player, Zone);
        internal void Advance(int ticks) { Clock.AdvanceClock(ticks); Call(Batch, "Reconcile", Zone); }
        internal void Invalidate(Entity owner) => CallStatic("BeforeOwnerInvalidated", owner, Zone);
        internal static object CallStatic(string name, params object[] args)
        { return Invoke(typeof(Part).Assembly.GetType("CavesOfOoo.Core.KitchenBatchPart").GetMethod(name, Flags), null, args); }
        internal static object Call(object value, string name, params object[] args) => Invoke(value.GetType().GetMethod(name, Flags), value, args);
        static object Invoke(MethodInfo method, object value, object[] args)
        {
            Assert.NotNull(method);
            try { return method.Invoke(value, args); }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        internal static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Flags).GetValue(value);
        internal void Unspent()
        {
            Assert.AreEqual(2, Units("Emberwheat")); Assert.AreEqual(1, Units("ClaspbeanPulp"));
            Assert.AreEqual(10, TradeSystem.GetDrams(Player)); Assert.AreEqual(35, TradeSystem.GetDrams(Worker));
            Assert.IsEmpty(Stored.Contents); Assert.IsEmpty(Finished.Contents); Assert.AreEqual("Idle", State);
        }
        public void Dispose()
        {
            CropSystem.Factory = oldFactory; SettlementRuntime.ActiveZone = oldZone;
            typeof(TurnManager).GetProperty("Active", Flags).SetValue(null, oldClock);
        }
    }

    public sealed class ConnectedKitchenTests
    {
        [Test] public void PaidWorkKeepsPhysicalInputsUntilDueThenMakesOneRealMeal()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); Assert.AreEqual("Working", f.State);
                Assert.AreEqual(0, f.Units("Emberwheat")); Assert.AreEqual(0, f.Units("ClaspbeanPulp"));
                Assert.AreEqual(3, f.Stored.Contents.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                Assert.AreEqual(8, TradeSystem.GetDrams(f.Player)); Assert.AreEqual(37, TradeSystem.GetDrams(f.Worker));
                Assert.IsEmpty(f.Worker.GetPart<InventoryPart>().Objects, "Committed food is not merchant stock.");
                f.Advance(119); Assert.AreEqual("Working", f.State); Assert.IsEmpty(f.Finished.Contents);
                f.Advance(1); Assert.AreEqual("Ready", f.State); Assert.IsEmpty(f.Stored.Contents);
                Assert.AreEqual("FieldMeal", f.Finished.Contents.Single().BlueprintName);
                var meal = f.Finished.Contents.Single(); Assert.AreSame(f.Pickup, meal.GetPart<PhysicsPart>().InInventory);
                f.Advance(10000); Assert.AreSame(meal, f.Finished.Contents.Single()); Assert.False(f.Start());
            }
        }
        [Test] public void PanMustBeRepairedAndWorkerMustBePresentBeforePayment()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                f.Pan.GetPart<RepairablePart>().Repaired = false; Assert.False(f.Start()); f.Unspent();
                f.Pan.GetPart<RepairablePart>().Repaired = true; f.Zone.MoveEntity(f.Worker, 30, 20);
                Assert.False(f.Start()); f.Unspent(); f.Zone.MoveEntity(f.Worker, 10, 9); Assert.True(f.Start());
            }
        }

        [Test]
        public void LateCookDepartureRejectsTheStagedStartButKeepsIndependentInjuryAndMovement()
        {
            using var f = new ConnectedKitchenFixture();
            var original = f.Pack.Objects.ToArray();
            var standing = f.Zone.GetEntityPosition(f.Worker);
            var hp = f.Player.GetStat("Hitpoints");
            int beforeHp = hp.BaseValue;
            bool ingredientsStaged = false;
            bool moved = false;
            var observer = new AfterKitchenStart(() =>
            {
                ingredientsStaged = f.State == "Working" && f.Stored.Contents.SequenceEqual(original);
                moved = f.Zone.MoveEntity(f.Worker, 30, 20);
                hp.BaseValue -= 7;
            });
            f.Player.AddPart(observer);
            try { Assert.False(f.Start()); }
            finally { f.Player.RemovePart(observer); }

            Assert.True(ingredientsStaged);
            Assert.True(moved);
            Assert.That(f.Zone.GetEntityPosition(f.Worker), Is.EqualTo((30, 20)));
            Assert.That(f.Player.GetStat("Hitpoints"), Is.SameAs(hp));
            Assert.That(hp.BaseValue, Is.EqualTo(beforeHp - 7));
            f.Unspent();
            Assert.That(f.Pack.Objects, Is.EquivalentTo(original));
            Assert.That(original.All(item => item.GetPart<PhysicsPart>().InInventory == f.Player), Is.True);
            Assert.False(f.Zone.GetReadOnlyEntities().Any(e => original.Contains(e)));

            Assert.True(f.Zone.MoveEntity(f.Worker, standing.x, standing.y));
            Assert.True(f.Start());
            Assert.That(f.Stored.Contents, Is.EquivalentTo(original));
            Assert.That(TradeSystem.GetDrams(f.Player), Is.EqualTo(8));
            Assert.That(TradeSystem.GetDrams(f.Worker), Is.EqualTo(37));
            Assert.That(f.Player.GetStatValue("Hitpoints"), Is.EqualTo(beforeHp - 7));
        }

        [Test]
        public void LateDuplicatePackListingRejectsStartAndRestoresEachOriginalOnlyOnce()
        {
            using var f = new ConnectedKitchenFixture();
            var originals = f.Pack.Objects.ToArray();
            var pulp = originals.Single(e => e.BlueprintName == "ClaspbeanPulp");
            bool staged = false;
            var observer = new AfterKitchenStart(() =>
            {
                staged = f.State == "Working" && f.Stored.Contents.Contains(pulp)
                    && pulp.GetPart<PhysicsPart>().InInventory == f.Escrow;
                f.Pack.Objects.Add(pulp);
            });
            f.Player.AddPart(observer);
            try { Assert.False(f.Start()); }
            finally { f.Player.RemovePart(observer); }

            Assert.True(staged);
            f.Unspent();
            Assert.That(f.Pack.Objects, Is.EquivalentTo(originals));
            foreach (var item in originals)
            {
                Assert.That(f.Pack.Objects.Single(e => e.ID == item.ID), Is.SameAs(item));
                Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(f.Player));
            }
        }

        [Test]
        public void LateReplacementPhysicsPartRetainsTheRefundedIngredientsCurrentBacklink()
        {
            using var f = new ConnectedKitchenFixture();
            var pulp = f.Pack.Objects.Single(e => e.BlueprintName == "ClaspbeanPulp");
            var retired = pulp.GetPart<PhysicsPart>();
            var replacement = new PhysicsPart
            {
                Solid = retired.Solid, Weight = retired.Weight, Takeable = retired.Takeable,
                ThrowBase = retired.ThrowBase, ThrowHeight = retired.ThrowHeight, ThrowIntercept = retired.ThrowIntercept,
                Category = retired.Category, Equipped = retired.Equipped
            };
            bool staged = false;
            bool moved = false;
            var observer = new AfterKitchenStart(() =>
            {
                staged = f.State == "Working" && f.Stored.Contents.Contains(pulp) && retired.InInventory == f.Escrow;
                replacement.InInventory = retired.InInventory;
                pulp.RemovePart(retired);
                pulp.AddPart(replacement);
                moved = f.Zone.MoveEntity(f.Worker, 30, 20);
            });
            f.Player.AddPart(observer);
            try { Assert.False(f.Start()); }
            finally { f.Player.RemovePart(observer); }

            Assert.True(staged);
            Assert.True(moved);
            f.Unspent();
            Assert.That(f.Pack.Objects.Single(e => e.ID == pulp.ID), Is.SameAs(pulp));
            Assert.That(pulp.GetPart<PhysicsPart>(), Is.SameAs(replacement));
            Assert.That(replacement.ParentEntity, Is.SameAs(pulp));
            Assert.That(replacement.InInventory, Is.SameAs(f.Player));
            Assert.That(retired.ParentEntity, Is.Null);
        }

        [Test]
        public void DeathDuringBatchStartReturnsTheUnpaidInputsToTheActualDeathSpill()
        {
            using var f = new ConnectedKitchenFixture();
            var grain = f.Pack.Objects.Single(e => e.BlueprintName == "Emberwheat");
            grain.GetPart<StackerPart>().StackCount = 3;
            var pulp = f.Pack.Objects.Single(e => e.BlueprintName == "ClaspbeanPulp");
            var unrelated = f.Give("SalvagedTimber", 1);
            var deathCell = f.Zone.GetEntityCell(f.Player);
            int payerDrams = TradeSystem.GetDrams(f.Player);
            int cookDrams = TradeSystem.GetDrams(f.Worker);
            bool splitPaymentStaged = false;
            bool nativeSpillObserved = false;
            var observer = new AfterKitchenStart(() =>
            {
                var split = f.Stored.Contents.SingleOrDefault(e => e.BlueprintName == "Emberwheat");
                splitPaymentStaged = f.State == "Working" && split != null && split != grain
                    && split.GetPart<StackerPart>().StackCount == 2 && grain.GetPart<StackerPart>().StackCount == 1
                    && f.Pack.Objects.Contains(grain) && f.Stored.Contents.Contains(pulp);
                CombatSystem.HandleDeath(f.Player, null, f.Zone);
                nativeSpillObserved = f.Zone.GetEntityCell(unrelated) == deathCell
                    && f.Zone.GetEntityCell(grain) == deathCell;
            });
            f.Player.AddPart(observer);
            try { Assert.False(f.Start()); }
            finally { f.Player.RemovePart(observer); }

            Assert.True(splitPaymentStaged);
            Assert.True(nativeSpillObserved);
            Assert.True(CombatSystem.IsDeathHandled(f.Player));
            Assert.That(f.Zone.GetEntityCell(f.Player), Is.Null);
            Assert.That(f.Player.GetStatValue("Hitpoints"), Is.Zero);
            Assert.That(f.State, Is.EqualTo("Idle"));
            Assert.That(f.Stored.Contents, Is.Empty);
            Assert.That(f.Finished.Contents, Is.Empty);
            Assert.That(f.Pack.Objects, Is.Empty);
            Assert.That(TradeSystem.GetDrams(f.Player), Is.EqualTo(payerDrams));
            Assert.That(TradeSystem.GetDrams(f.Worker), Is.EqualTo(cookDrams));

            var ground = f.Zone.GetReadOnlyEntities().ToArray();
            var grainDrops = ground.Where(e => e.BlueprintName == "Emberwheat").ToArray();
            Assert.That(grainDrops.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1), Is.EqualTo(3));
            Assert.That(grainDrops.Single(e => e.ID == grain.ID), Is.SameAs(grain));
            Assert.That(ground.Single(e => e.BlueprintName == "ClaspbeanPulp"), Is.SameAs(pulp));
            Assert.That(pulp.GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(1));
            foreach (var item in grainDrops.Concat(new[] { pulp, unrelated }))
            {
                Assert.That(ground.Single(e => e.ID == item.ID), Is.SameAs(item));
                Assert.That(f.Zone.GetEntityCell(item), Is.SameAs(deathCell));
                Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.Null);
                Assert.That(item.GetPart<PhysicsPart>().Equipped, Is.Null);
            }
        }

        [Test]
        public void StunnedCookCannotStartAnOrderUntilAbleToActAgain()
        {
            using var f = new ConnectedKitchenFixture();
            Assert.True(f.Worker.ApplyEffect(new StunnedEffect(3), f.Player, f.Zone));
            Assert.True(f.Worker.GetPart<StatusEffectsPart>().IsActionBlocked());
            Assert.False(WorldInteractionSystem.GatherActions(f.Pan, f.Player)
                .Any(a => !a.IsDisabled && a.Command == KitchenBatchPart.StartCommand));
            Assert.False(f.Start());
            f.Unspent();

            Assert.True(f.Worker.RemoveEffect<StunnedEffect>());
            Assert.True(f.Start());
            Assert.That(f.State, Is.EqualTo("Working"));
        }

        [Test]
        public void AWorkingOrderWaitsAtItsDueTimeWhileTheCookIsStunned()
        {
            using var f = new ConnectedKitchenFixture();
            Assert.True(f.Start());
            var original = f.Stored.Contents.ToArray();
            f.Advance(119);
            Assert.True(f.Worker.ApplyEffect(new StunnedEffect(3), f.Player, f.Zone));
            Assert.True(f.Worker.GetPart<StatusEffectsPart>().IsActionBlocked());
            f.Advance(1);

            Assert.That(f.State, Is.EqualTo("Working"));
            Assert.That(f.Stored.Contents, Is.EquivalentTo(original));
            Assert.That(original.All(item => item.GetPart<PhysicsPart>().InInventory == f.Escrow), Is.True);
            Assert.That(f.Finished.Contents, Is.Empty);
            Assert.That(TradeSystem.GetDrams(f.Player), Is.EqualTo(8));
            Assert.That(TradeSystem.GetDrams(f.Worker), Is.EqualTo(37));

            Assert.True(f.Worker.RemoveEffect<StunnedEffect>());
            f.Advance(1);
            Assert.That(f.State, Is.EqualTo("Ready"));
            Assert.That(f.Stored.Contents, Is.Empty);
            var meal = f.Finished.Contents.Single();
            Assert.That(meal.BlueprintName, Is.EqualTo("FieldMeal"));
            Assert.That(meal.GetPart<PhysicsPart>().InInventory, Is.SameAs(f.Pickup));
            f.Advance(1);
            Assert.That(f.Finished.Contents.Single(), Is.SameAs(meal));
            Assert.That(TradeSystem.GetDrams(f.Player), Is.EqualTo(8));
            Assert.That(TradeSystem.GetDrams(f.Worker), Is.EqualTo(37));
        }

        private sealed class AfterKitchenStart : Part
        {
            private readonly Action callback;

            public AfterKitchenStart(Action callback) => this.callback = callback;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command") == KitchenBatchPart.StartCommand)
                    callback();
                return true;
            }
        }

        [Test] public void MissingPaymentOrFullOutputRefusesWithoutPartialIngredients()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                f.Player.IntProperties[TradeSystem.CURRENCY_PROP] = 1; Assert.False(f.Start());
                Assert.AreEqual(2, f.Units("Emberwheat")); Assert.AreEqual(1, f.Units("ClaspbeanPulp"));
                f.Player.IntProperties[TradeSystem.CURRENCY_PROP] = 10;
                f.Finished.MaxItems = 0; Assert.False(f.Start()); f.Unspent();
                f.Finished.MaxItems = 1; Assert.True(f.Start());
            }
        }
        [Test] public void TemporaryCompletionCapacityFailurePreservesEscrowAndCanRecover()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Finished.MaxItems = 0; f.Advance(120);
                Assert.AreEqual("Working", f.State); Assert.IsNotEmpty(f.Stored.Contents); Assert.IsEmpty(f.Finished.Contents);
                f.Finished.MaxItems = 1; f.Advance(1); Assert.AreEqual("Ready", f.State);
                Assert.AreEqual(1, f.Finished.Contents.Count); Assert.IsEmpty(f.Stored.Contents);
            }
        }

        [Test]
        public void LockingThePickupRejectsStagedStartAndLaterPausesTheSamePaidOrder()
        {
            using var f = new ConnectedKitchenFixture();
            var ingredients = f.Pack.Objects.ToArray();
            bool staged = false;
            var observer = new AfterKitchenStart(() =>
            {
                staged = f.State == "Working" && f.Stored.Contents.SequenceEqual(ingredients);
                f.Finished.Locked = true;
            });
            f.Player.AddPart(observer);
            try { Assert.False(f.Start()); }
            finally { f.Player.RemovePart(observer); }
            Assert.True(staged);
            Assert.True(f.Finished.IsLocked);
            f.Unspent();
            Assert.That(f.Pack.Objects, Is.EquivalentTo(ingredients));
            Assert.That(ingredients.All(item => item.GetPart<PhysicsPart>().InInventory == f.Player), Is.True);

            f.Finished.Locked = false;
            Assert.True(f.Start());
            f.Finished.Locked = true;
            f.Advance(KitchenBatchPart.PreparationTicks);
            var reader = new List<string>();
            StateReader.Collect(f.Pan, true, f.Zone, f.Zone.GetEntityCell(f.Pan), f.Player, reader);
            Assert.That(string.Join(" ", reader), Does.Contain("waiting").And.Contain("pickup locked"));
            Assert.That(f.State, Is.EqualTo("Working"));
            Assert.That(f.Stored.Contents, Is.EquivalentTo(ingredients));
            Assert.That(ingredients.All(item => item.GetPart<PhysicsPart>().InInventory == f.Escrow), Is.True);
            Assert.That(f.Finished.Contents, Is.Empty);

            f.Finished.Locked = false;
            f.Advance(1);
            Assert.That(f.State, Is.EqualTo("Ready"));
            Assert.That(f.Stored.Contents, Is.Empty);
            var meal = f.Finished.Contents.Single();
            Assert.That(meal.BlueprintName, Is.EqualTo("FieldMeal"));
            Assert.That(meal.GetPart<PhysicsPart>().InInventory, Is.SameAs(f.Pickup));
            f.Advance(KitchenBatchPart.PreparationTicks);
            Assert.That(f.Finished.Contents.Single(), Is.SameAs(meal));
            Assert.That(TradeSystem.GetDrams(f.Player), Is.EqualTo(8));
            Assert.That(TradeSystem.GetDrams(f.Worker), Is.EqualTo(37));
        }

        [TestCase(119, false)][TestCase(120, true)]
        public void PreInvalidationBoundaryFinishesOnlyWorkAlreadyDue(int tick, bool ready)
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Clock.AdvanceClock(tick);
                f.Worker.GetStat("Hitpoints").BaseValue = 0; f.Invalidate(f.Worker);
                Assert.IsEmpty(f.Stored.Contents);
                Assert.AreEqual(ready ? 1 : 0, f.Finished.Contents.Count);
                var salvage = f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat" || e.BlueprintName == "ClaspbeanPulp").ToArray();
                Assert.AreEqual(ready ? 0 : 3, salvage.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                Assert.AreEqual(8, TradeSystem.GetDrams(f.Player), "The stated interruption policy retains the service fee.");
                f.Invalidate(f.Worker); f.Advance(1000); Assert.AreEqual(ready ? 1 : 0, f.Finished.Contents.Count);
            }
        }
        [Test] public void MissingCommittedInputCancelsWithoutReplacingTheStolenUnit()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); var stolen = f.Stored.Contents.First(e => e.BlueprintName == "ClaspbeanPulp");
                f.Stored.RemoveItem(stolen); f.Pack.AddObject(stolen); f.Advance(120);
                Assert.IsEmpty(f.Stored.Contents); Assert.IsEmpty(f.Finished.Contents);
                Assert.AreEqual(1, f.Units("ClaspbeanPulp"));
                Assert.AreEqual(2, f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Emberwheat").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            }
        }
        [Test] public void CollectedOrDestroyedOutputDoesNotRegenerateOrOccupyTheSlotForever()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                Assert.True(f.Start()); f.Advance(120); var meal = f.Finished.Contents.Single();
                f.Finished.RemoveItem(meal); f.Pack.AddObject(meal); f.Advance(1);
                Assert.AreEqual("Idle", f.State); Assert.IsEmpty(f.Finished.Contents);
                f.Give("Emberwheat", 2); f.Give("ClaspbeanPulp", 1); Assert.True(f.Start());
                f.Advance(120); Assert.AreEqual(1, f.Finished.Contents.Count); Assert.AreEqual(1, f.Units("FieldMeal"));
            }
        }
        [Test] public void OneMealHealsAndRemovesOnlyOrdinaryBleeding()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var meal = f.Give("FieldMeal", 1); f.Player.GetStat("Hitpoints").BaseValue = 10;
                var status = new StatusEffectsPart(); f.Player.AddPart(status); status.ApplyEffect(new BleedingEffect());
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(meal, "Eat"), f.Player, f.Zone).Success);
                Assert.That(f.Player.GetStatValue("Hitpoints"), Is.InRange(13, 22)); Assert.False(status.HasEffect<BleedingEffect>());
                Assert.AreEqual(0, f.Units("FieldMeal"));
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(meal, "Eat"), f.Player, f.Zone).Success);
            }
        }
        [Test] public void FullHealthMealStillConsumesOnceWithoutCreatingHealingCredit()
        {
            using (var f = new ConnectedKitchenFixture())
            {
                var meal = f.Give("FieldMeal", 1);
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(meal, "Eat"), f.Player, f.Zone).Success);
                Assert.AreEqual(40, f.Player.GetStatValue("Hitpoints")); Assert.AreEqual(0, f.Units("FieldMeal"));
            }
        }
        [Test] public void ACarriedCookedMealIsNeverTheWorkersTradeStock()
        {
            using (var f = new ConnectedKitchenFixture())
            { Assert.True(f.Start()); f.Advance(120); Assert.IsEmpty(f.Worker.GetPart<InventoryPart>().Objects); Assert.AreEqual(1, f.Finished.Contents.Count); }
        }
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void NativeEntitySavePreservesPendingOrReadyOwnersWithoutAnotherPayment(bool ready, bool separateGrain)
        {
            using (var f = new ConnectedKitchenFixture())
            using (var stream = new MemoryStream())
            {
                if (separateGrain)
                {
                    var grain = f.Pack.Objects.Single(e => e.BlueprintName == "Emberwheat");
                    Assert.True(InventorySystem.ExecuteCommand(new SeparateOneCommand(grain), f.Player, f.Zone).Success);
                }
                Assert.True(f.Start());
                var ingredientIDs = f.Stored.Contents.Select(e => e.ID).ToArray();
                Assert.AreEqual(separateGrain ? 3 : 2, ingredientIDs.Length);
                f.Advance(ready ? 120 : 40);
                var writer = new SaveWriter(stream); writer.WriteEntityReference(f.Pan); writer.WriteEntityReference(f.Player); writer.WriteQueuedEntityBodies();
                stream.Position = 0; var reader = new SaveReader(stream, f.Factory);
                var pan = reader.ReadEntityReference(); var actor = reader.ReadEntityReference(); reader.ReadEntityBodies();
                var batch = pan.Parts.Single(p => p.Name == "KitchenBatch");
                var restored = new Zone(f.Zone.ZoneID); restored.AddEntity(pan, 10, 10); restored.AddEntity(actor, 9, 10);
                foreach (string owner in new[] { "Worker", "Escrow", "Pickup" })
                {
                    var entity = ConnectedKitchenFixture.Field<Entity>(batch, owner);
                    var at = f.Zone.GetEntityPosition(ConnectedKitchenFixture.Field<Entity>(f.Batch, owner));
                    Assert.True(restored.AddEntity(entity, at.x, at.y));
                }
                SettlementRuntime.ActiveZone = restored;
                Assert.AreSame(actor, ConnectedKitchenFixture.Field<Entity>(batch, "Commissioner"));
                Assert.AreEqual(8, TradeSystem.GetDrams(actor));
                if (!ready)
                {
                    var escrow = ConnectedKitchenFixture.Field<Entity>(batch, "Escrow");
                    var held = escrow.GetPart<ContainerPart>().Contents;
                    Assert.That(held.Select(e => e.ID), Is.EqualTo(ingredientIDs));
                    Assert.True(held.All(e => e.GetPart<PhysicsPart>().InInventory == escrow));
                }
                f.Clock.AdvanceClock(80); ConnectedKitchenFixture.Call(batch, "Reconcile", restored);
                var pickup = ConnectedKitchenFixture.Field<Entity>(batch, "Pickup").GetPart<ContainerPart>();
                Assert.AreEqual(1, pickup.Contents.Count); Assert.AreEqual("FieldMeal", pickup.Contents.Single().BlueprintName);
                Assert.AreSame(ConnectedKitchenFixture.Field<Entity>(batch, "Output"), pickup.Contents.Single());
                ConnectedKitchenFixture.Call(batch, "Reconcile", restored); Assert.AreEqual(1, pickup.Contents.Count);
            }
        }
    }
}
