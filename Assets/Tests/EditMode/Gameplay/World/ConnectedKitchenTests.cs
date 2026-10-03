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
        [TestCase(false)][TestCase(true)]
        public void NativeEntitySavePreservesPendingOrReadyOwnersWithoutAnotherPayment(bool ready)
        {
            using (var f = new ConnectedKitchenFixture())
            using (var stream = new MemoryStream())
            {
                Assert.True(f.Start()); f.Advance(ready ? 120 : 40);
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
                f.Clock.AdvanceClock(80); ConnectedKitchenFixture.Call(batch, "Reconcile", restored);
                var pickup = ConnectedKitchenFixture.Field<Entity>(batch, "Pickup").GetPart<ContainerPart>();
                Assert.AreEqual(1, pickup.Contents.Count); Assert.AreEqual("FieldMeal", pickup.Contents.Single().BlueprintName);
                Assert.AreSame(ConnectedKitchenFixture.Field<Entity>(batch, "Output"), pickup.Contents.Single());
                ConnectedKitchenFixture.Call(batch, "Reconcile", restored); Assert.AreEqual(1, pickup.Contents.Count);
            }
        }
    }
}
