using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class SoddenPreparationFixture
    {
        protected Zone Zone;
        protected Entity Actor, Worker, Bench;
        protected SoddenPreparationPart Service;
        protected EntityFactory Factory;
        protected InventoryPart Pack => Actor.GetPart<InventoryPart>();
        protected StatusEffectsPart Status => Actor.GetPart<StatusEffectsPart>();
        EntityFactory previous;
        [SetUp] public void SetUp()
        {
            previous = SeedPart.Factory;
            Factory = new EntityFactory();
            Factory.RegisterPartType<SoddenDressingPart>("SoddenDressing");
            Factory.LoadBlueprints(@"{""Objects"":[{""Name"":""SoddenFieldDressing"",""Parts"":[{""Name"":""Physics"",""Params"":[{""Key"":""Takeable"",""Value"":""true""},{""Key"":""Weight"",""Value"":""1""}]},{""Name"":""Stacker"",""Params"":[{""Key"":""MaxStack"",""Value"":""20""}]},{""Name"":""SoddenDressing""},{""Name"":""Render"",""Params"":[{""Key"":""DisplayName"",""Value"":""sodden field dressing""}]}],""Tags"":[{""Key"":""Item"",""Value"":""true""}]}]}");
            SeedPart.Factory = Factory;
            Zone = new Zone("Overworld.15.7.0");
            Actor = Make("Player", false); Actor.SetTag("Player"); Actor.SetTag("Creature");
            Actor.AddPart(new InventoryPart()); Actor.AddPart(new StatusEffectsPart());
            Actor.Statistics["Hitpoints"] = new Stat { Owner = Actor, BaseValue = 20, Max = 40 };
            Assert.True(Zone.AddEntity(Actor, 4, 4)); TradeSystem.SetDrams(Actor, 10);
            Worker = Make("PeatCutter", false); Worker.SetTag("Creature");
            Worker.AddPart(new BrainPart()); Worker.AddPart(new InventoryPart()); Worker.AddPart(new StatusEffectsPart());
            Worker.AddPart(new ConversationPart { ConversationID = "PeatCutter_1" });
            Worker.Statistics["Hitpoints"] = new Stat { Owner = Worker, BaseValue = 20, Max = 20 };
            Assert.True(Zone.AddEntity(Worker, 6, 4)); TradeSystem.SetDrams(Worker, 7);
            Bench = Make("SoddenDressingBench", false);
            Bench.AddPart(new CompositionPart { MaterialsRaw = "Wood" });
            Bench.AddPart(new RepairablePart { RecipeId = "timber-dressing-bench", Repaired = true });
            Service = new SoddenPreparationPart(); Bench.AddPart(Service);
            Assert.True(Zone.AddEntity(Bench, 5, 4)); Reveal();
            Assert.True(Service.Configure(Zone, Worker)); Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { SeedPart.Factory = previous; OutputProbe.Callback = null; ConversationManager.EndConversation(); ConversationLoader.Reset(); AsciiFxBus.Clear(); }
        protected void Reveal()
        {
            foreach (var owner in new[] { Actor, Worker, Bench })
            { var cell = Zone.GetEntityCell(owner); cell.IsVisible = true; cell.Explored = true; }
        }
        protected static Entity Make(string blueprint, bool takeable)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = blueprint };
            e.AddPart(new PhysicsPart { Takeable = takeable, Weight = 1 });
            e.AddPart(new RenderPart { DisplayName = blueprint, Visible = true });
            if (takeable) e.SetTag("Item"); return e;
        }
        protected Entity Supply(string blueprint, int count = 1)
        {
            var e = Make(blueprint, true); e.AddPart(new StackerPart { StackCount = count, MaxStack = 20 });
            if (blueprint == "SoddenFieldDressing") e.AddPart(new SoddenDressingPart());
            Assert.True(Pack.AddObject(e)); return e;
        }
        protected void Ingredients(int count = 1) { Supply("SumpsievePad", count); Supply("KnotflaxCord", count); }
        protected bool Act(Entity target, string command) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target, command), Actor, Zone).Success;
        protected bool Prepare() => Act(Bench, SoddenPreparationPart.PrepareCommand);
        protected int Units(string blueprint) => Pack.Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        protected int Records(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 100 }).Records.Count;
        protected void Probe(Action<Entity> callback)
        {
            OutputProbe.Callback = callback; Factory.RegisterPartType<OutputProbe>("SoddenOutputProbe");
            Factory.Blueprints["SoddenFieldDressing"].Parts["SoddenOutputProbe"] = new System.Collections.Generic.Dictionary<string, string>();
        }
        public sealed class OutputProbe : Part
        { public static Action<Entity> Callback; public override bool HandleEvent(GameEvent e) { if (e.ID == "ObjectCreated") Callback?.Invoke(ParentEntity); return true; } }
        public sealed class FailAfter : Part
        { public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("sodden rollback probe"); return true; } }
        public sealed class CancelBefore : Part { public override bool HandleEvent(GameEvent e) => e.ID != "BeforeInventoryAction"; }
        public sealed class AfterProbe : Part
        { public Action Callback; public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") Callback?.Invoke(); return true; } }
    }

    public sealed class SoddenPreparationTests : SoddenPreparationFixture
    {
        [Test] public void ConfigureNamesTheActualKeeperAndCannotRebindAnExistingService()
        {
            Assert.AreEqual("Sella, dressing keeper", Worker.GetPart<RenderPart>().DisplayName);
            Assert.False(Service.Configure(Zone, null)); Assert.AreSame(Worker, Service.Worker);
            Assert.False(Service.Configure(Zone, Worker)); Assert.AreEqual(Worker.ID, Service.WorkerID);
        }
        [Test] public void KeeperServiceConversationBindingSurvivesNativeSaveWithoutReconfiguration()
        {
            Assert.AreEqual("SoddenSella_1", Worker.GetPart<ConversationPart>().ConversationID);
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Bench); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, null); var bench = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual("SoddenSella_1", bench.GetPart<SoddenPreparationPart>().Worker.GetPart<ConversationPart>().ConversationID);
            }
        }
        [Test] public void KeeperConversationExplainsActualServiceRouteAndUnresolvedBodyTradeWithoutRewards()
        {
            ConversationLoader.Reset(); var data = ConversationLoader.Get("SoddenSella_1");
            Assert.NotNull(data, "Sella needs her actual service conversation in native Resources."); Assert.AreEqual(4, data.Nodes.Count);
            foreach (var node in data.Nodes)
            {
                Assert.IsNotEmpty(node.Text);
                foreach (var choice in node.Choices)
                {
                    Assert.True(choice.Target == "End" || data.GetNode(choice.Target) != null);
                    Assert.True(choice.Actions == null || choice.Actions.Count == 0, "Talking never grants supplies, payment or quest state.");
                    Assert.True(choice.Predicates == null || choice.Predicates.Count == 0, "The optional service lead does not require a quest.");
                }
            }
            Assert.True(ConversationManager.StartConversation(Worker, Actor));
            foreach (var target in new[] { "Supplies", "Route", "Bodies" })
            {
                int index = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == target); Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.True(ConversationManager.SelectChoice(index)); Assert.AreEqual(target, ConversationManager.CurrentNode.ID);
                Assert.True(ConversationManager.SelectChoice(0)); Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
            }
            StringAssert.Contains("one sumpsieve pad", data.GetNode("Supplies").Text.ToLowerInvariant());
            StringAssert.Contains("one knotflax cord", data.GetNode("Supplies").Text.ToLowerInvariant());
            StringAssert.Contains("two drams", data.GetNode("Supplies").Text.ToLowerInvariant());
            StringAssert.Contains("two salvaged timber", data.GetNode("Supplies").Text.ToLowerInvariant());
            StringAssert.Contains("north", data.GetNode("Route").Text.ToLowerInvariant()); StringAssert.Contains("east", data.GetNode("Route").Text.ToLowerInvariant());
            StringAssert.Contains("Concord", data.GetNode("Bodies").Text);
            Assert.AreEqual(0, Pack.Objects.Count); Assert.AreEqual(10, TradeSystem.GetDrams(Actor)); Assert.AreEqual(7, TradeSystem.GetDrams(Worker));
        }
        [Test] public void ActorlessDressingMenuDoesNotThrowOrConsume()
        {
            var dressing = Supply("SoddenFieldDressing");
            Assert.False(WorldInteractionSystem.GatherActions(dressing).Any(a => a.Command == SoddenDressingPart.ApplyCommand));
            Assert.AreEqual(1, Units("SoddenFieldDressing"));
        }
        [Test] public void RepairedBenchMakesOneActualDressingAndPaysItsAssignedWorker()
        {
            Ingredients(2); Assert.True(Prepare());
            Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(1, Units("KnotflaxCord")); Assert.AreEqual(1, Units("SoddenFieldDressing"));
            Assert.AreEqual(8, TradeSystem.GetDrams(Actor)); Assert.AreEqual(9, TradeSystem.GetDrams(Worker));
            Assert.AreEqual(1, Records("furniture", "SoddenPrepared"));
            Assert.True(Prepare()); Assert.False(Prepare()); Assert.AreEqual(2, Units("SoddenFieldDressing"));
            Assert.AreEqual(6, TradeSystem.GetDrams(Actor)); Assert.AreEqual(11, TradeSystem.GetDrams(Worker));
        }
        [Test] public void BrokenBenchCannotPrepareAndRepairRestoresLastingUse()
        {
            Ingredients(); Bench.GetPart<RepairablePart>().Repaired = false;
            Assert.False(Prepare()); Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
            Supply("SalvagedTimber", 2); Assert.True(Act(Bench, RepairablePart.RepairCommand));
            Assert.True(Bench.GetPart<RepairablePart>().Repaired); Assert.AreEqual(0, Units("SalvagedTimber")); Assert.True(Prepare());
        }
        [TestCase("SumpsievePad")][TestCase("KnotflaxCord")]
        public void MissingIngredientNeverPartlyConsumesOrPays(string missing)
        {
            Ingredients(); foreach (var item in Pack.Objects.Where(e => e.BlueprintName == missing).ToArray()) Pack.RemoveObject(item);
            Assert.False(Prepare()); Assert.AreEqual(0, Units("SoddenFieldDressing")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(7, TradeSystem.GetDrams(Worker)); Assert.AreEqual(1, Units(missing == "SumpsievePad" ? "KnotflaxCord" : "SumpsievePad"));
            Assert.AreEqual(1, Records("furniture", "SoddenPreparationRejected"));
        }
        [Test] public void QueriesNeverConsumeOrCreateAndMissingActorIsSafe()
        {
            Ingredients(); Assert.True(WorldInteractionSystem.GatherActions(Bench, Actor).Any(a => a.Command == SoddenPreparationPart.PrepareCommand));
            Assert.False(WorldInteractionSystem.GatherActions(Bench).Any(a => a.Command == SoddenPreparationPart.PrepareCommand));
            StringAssert.Contains("sumpsieve", Service.Describe().ToLowerInvariant());
            Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(0, Units("SoddenFieldDressing")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
        }
        [Test] public void OuterFailureRestoresExactInputsOutputStackAndBothPurses()
        {
            Ingredients(); var output = Supply("SoddenFieldDressing", 3); Actor.AddPart(new FailAfter());
            Assert.False(Prepare()); Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(1, Units("KnotflaxCord"));
            Assert.AreEqual(3, output.GetPart<StackerPart>().StackCount); Assert.AreSame(Actor, output.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(10, TradeSystem.GetDrams(Actor)); Assert.AreEqual(7, TradeSystem.GetDrams(Worker)); Assert.AreEqual(0, Records("furniture", "SoddenPrepared"));
        }
        [Test] public void NativeSaveGraphKeepsRepairAndExactWorkerBinding()
        {
            Ingredients(); Assert.True(Prepare());
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Actor); writer.WriteEntityReference(Bench); writer.WriteEntityReference(Worker); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, null); var actor = reader.ReadEntityReference(); var bench = reader.ReadEntityReference(); var worker = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreSame(worker, bench.GetPart<SoddenPreparationPart>().Worker); Assert.True(bench.GetPart<RepairablePart>().Repaired);
                Actor = actor; Bench = bench; Worker = worker; Service = bench.GetPart<SoddenPreparationPart>();
                Zone = new Zone("Overworld.15.7.0"); Assert.True(Zone.AddEntity(Actor, 4, 4)); Assert.True(Zone.AddEntity(Bench, 5, 4)); Assert.True(Zone.AddEntity(Worker, 6, 4)); Reveal();
                Ingredients(); Assert.True(Prepare()); Assert.AreEqual(6, TradeSystem.GetDrams(Actor)); Assert.AreEqual(2, Units("SoddenFieldDressing"));
            }
        }
        [TestCase(true, false)][TestCase(false, true)][TestCase(true, true)]
        public void DressingConsumesOneForActualOrdinaryPoisonOrBleedingWithoutHealing(bool poison, bool bleed)
        {
            var dressing = Supply("SoddenFieldDressing", 2);
            if (poison) Status.ApplyEffect(new PoisonedEffect()); if (bleed) Status.ApplyEffect(new BleedingEffect());
            Status.ApplyEffect(new PoisonedByGasEffect { Duration = 5 }); Status.ApplyEffect(new FungalInfectionEffect { Duration = 20 });
            Assert.True(Act(dressing, SoddenDressingPart.ApplyCommand)); Assert.AreEqual(1, Units("SoddenFieldDressing"));
            Assert.False(Status.HasEffect<PoisonedEffect>()); Assert.False(Status.HasEffect<BleedingEffect>());
            Assert.True(Status.HasEffect<PoisonedByGasEffect>()); Assert.True(Status.HasEffect<FungalInfectionEffect>()); Assert.AreEqual(20, Actor.GetStatValue("Hitpoints"));
            Assert.False(Act(dressing, SoddenDressingPart.ApplyCommand)); Assert.AreEqual(1, Units("SoddenFieldDressing")); Assert.AreEqual(1, Records("event", "SoddenDressingApplied"));
        }
        [Test] public void DressingRollbackRestoresSameEffectsAndOnlyTheConsumedUnit()
        {
            var dressing = Supply("SoddenFieldDressing", 2); var poison = new PoisonedEffect(9); var bleed = new BleedingEffect(17);
            Status.ApplyEffect(poison); Status.ApplyEffect(bleed); Actor.AddPart(new FailAfter());
            Assert.False(Act(dressing, SoddenDressingPart.ApplyCommand)); Assert.AreEqual(2, Units("SoddenFieldDressing"));
            Assert.AreSame(poison, Status.GetEffect<PoisonedEffect>()); Assert.AreSame(bleed, Status.GetEffect<BleedingEffect>());
            Assert.AreEqual(9, poison.Duration); Assert.AreEqual(17, bleed.SaveTarget); Assert.AreEqual(0, Records("event", "SoddenDressingApplied"));
        }
    }
}
