using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class ConnectedInkFixture
    {
        protected const string Reink="ReinkGrimoire", Prepare="PrepareBotanicalInk";
        protected Zone Zone; protected Entity Actor,Book,Worker,Desk; protected EntityFactory Factory;
        protected GrimoireChargePart Charges; protected InventoryPart Pack=>Actor.GetPart<InventoryPart>();
        EntityFactory previous;
        [SetUp] public void SetUp()
        {
            previous=SeedPart.Factory; Factory=new EntityFactory();
            Factory.LoadBlueprints(@"{""Objects"":[{""Name"":""InkVial"",""Parts"":[{""Name"":""Physics"",""Params"":[{""Key"":""Takeable"",""Value"":""true""},{""Key"":""Weight"",""Value"":""1""}]},{""Name"":""Stacker"",""Params"":[{""Key"":""MaxStack"",""Value"":""20""}]},{""Name"":""InkVial""},{""Name"":""Render"",""Params"":[{""Key"":""DisplayName"",""Value"":""ink vial""}]}],""Tags"":[{""Key"":""Item"",""Value"":""true""}]}]}");
            SeedPart.Factory=Factory; Zone=new Zone(MarrowstyeCompositionPlan.ZoneID);
            Actor=Make("Player",false);Actor.SetTag("Player");Actor.SetTag("Creature");
            Actor.AddPart(new InventoryPart());Actor.AddPart(new StatusEffectsPart());
            Actor.Statistics["Hitpoints"]=new Stat{Owner=Actor,BaseValue=40,Max=40};
            Assert.True(Zone.AddEntity(Actor,4,4));TradeSystem.SetDrams(Actor,10);
            Book=Supply("TestGrimoire");Book.AddPart(new GrimoirePart{SkillClassName="Rites_ShatteredRime"});
            Charges=new GrimoireChargePart{Charges=3,MaxCharges=10,ChargesPerVial=5};Book.AddPart(Charges);
            Worker=Make("CurationJuniorIndexer",false);Worker.SetTag("Creature");Worker.AddPart(new BrainPart());
            Worker.Statistics["Hitpoints"]=new Stat{Owner=Worker,BaseValue=20,Max=20};
            Assert.True(Zone.AddEntity(Worker,6,4));TradeSystem.SetDrams(Worker,7);
            Desk=Make("BotanicalInkDesk",false);Assert.True(Zone.AddEntity(Desk,5,4));
            MessageLog.Clear();
        }
        [TearDown] public void TearDown(){SeedPart.Factory=previous;OutputProbe.Callback=null;}
        protected static Entity Make(string blueprint,bool takeable)
        {
            var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName=blueprint};
            e.AddPart(new PhysicsPart{Takeable=takeable,Weight=1});e.AddPart(new RenderPart{DisplayName=blueprint});
            if(takeable)e.SetTag("Item");return e;
        }
        protected Entity Supply(string blueprint,int count=1)
        {
            var e=Make(blueprint,true);if(blueprint!="TestGrimoire")e.AddPart(new StackerPart{StackCount=count,MaxStack=20});
            if(blueprint=="InkVial")e.AddPart(new InkVialPart());
            Assert.True(Pack.AddObject(e));return e;
        }
        protected bool Act(Entity target,string command)=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target,command),Actor,Zone).Success;
        protected int Units(string blueprint)=>Pack.Objects.Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        protected Part ConfigureDesk()
        {
            var type=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.BotanicalInkDeskPart");
            Assert.NotNull(type,"The public ink desk must implement the proposed physical processing action.");
            var part=(Part)Activator.CreateInstance(type);Desk.AddPart(part);
            var configure=type.GetMethod("Configure",BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(configure,"The generated desk binds its actual worker and zone.");
            configure.Invoke(part,new object[]{Zone,Worker});return part;
        }
        protected void Ingredients(){Supply("SootrootPulp",2);Supply("PitchpodResin");}
        protected void Probe(Action<Entity> callback)
        {
            OutputProbe.Callback=callback;Factory.RegisterPartType<OutputProbe>("ConnectedInkOutputProbe");
            Factory.Blueprints["InkVial"].Parts["ConnectedInkOutputProbe"]=new System.Collections.Generic.Dictionary<string,string>();
        }
        public sealed class OutputProbe:Part{public static Action<Entity> Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
        public sealed class CancelBefore:Part{public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeInventoryAction";}
        public sealed class FailAfter:Part{public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("connected ink rollback probe");return true;}}
    }

    public sealed class ConnectedInkTests:ConnectedInkFixture
    {
        [Test] public void OneCarriedVialRefillsOnlyTheSelectedBook()
        {
            Supply("InkVial",2);var other=Supply("OtherBook");var ink=new GrimoireChargePart{Charges=1};other.AddPart(ink);
            int rental=RentalSystem.GetInk(Actor);Assert.True(Act(Book,Reink));
            Assert.AreEqual(8,Charges.Charges);Assert.AreEqual(1,ink.Charges);Assert.AreEqual(1,Units("InkVial"));Assert.AreEqual(rental,RentalSystem.GetInk(Actor));
        }
        [Test] public void NearFullBookSpendsOneVialForTheDisplayedCappedGain()
        {Charges.Charges=9;Supply("InkVial");Assert.True(Act(Book,Reink));Assert.AreEqual(10,Charges.Charges);Assert.AreEqual(0,Units("InkVial"));Assert.False(Act(Book,Reink));}
        [Test] public void ReinkMenuPreviewsExactGainAndOneVialCost()
        {
            Charges.Charges=9;Supply("InkVial");var actions=new InventoryActionList();var e=GameEvent.New("GetInventoryActions");
            e.SetParameter("Actor",Actor);e.SetParameter("Actions",actions);e.SetParameter("Zone",Zone);Book.FireEvent(e);e.Release();
            var action=actions.Actions.SingleOrDefault(a=>a.Command==Reink);Assert.NotNull(action);
            StringAssert.Contains("1",action.Display);StringAssert.Contains("ink",action.Display.ToLowerInvariant());Assert.AreEqual(9,Charges.Charges);
        }
        [Test] public void MissingVialOrFullBookRefusesWithoutChangingEitherWallet()
        {Assert.False(Act(Book,Reink));Supply("InkVial");Charges.Charges=10;Assert.False(Act(Book,Reink));Assert.AreEqual(1,Units("InkVial"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));}
        [TestCase(-1,10,5)][TestCase(11,10,5)][TestCase(0,0,5)][TestCase(1,10,0)][TestCase(1,10,-1)]
        public void MalformedBookRefusesWithoutNormalizingItsPayload(int value,int maximum,int perVial)
        {Charges.Charges=value;Charges.MaxCharges=maximum;Charges.ChargesPerVial=perVial;Supply("InkVial");Assert.False(Act(Book,Reink));Assert.AreEqual(value,Charges.Charges);Assert.AreEqual(1,Units("InkVial"));}
        [Test] public void PositiveConfiguredVialAmountIsRespected()
        {Charges.ChargesPerVial=2;Supply("InkVial");Assert.True(Act(Book,Reink));Assert.AreEqual(5,Charges.Charges);}
        [Test] public void RefillArithmeticCannotOverflowPastTheBookCeiling()
        {Assert.AreEqual(7,Charges.Refill(int.MaxValue));Assert.AreEqual(10,Charges.Charges);}
        [Test] public void GroundBookCannotBorrowTheActorsVial()
        {Supply("InkVial");Assert.True(Pack.RemoveObject(Book));Assert.True(Zone.AddEntity(Book,4,4));Assert.False(Act(Book,Reink));Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(1,Units("InkVial"));}
        [Test] public void CancellationAndOuterFailureRestoreBookAndExactVialOwner()
        {
            var vial=Supply("InkVial");var veto=new CancelBefore();Actor.AddPart(veto);Assert.False(Act(Book,Reink));Actor.RemovePart(veto);
            var fail=new FailAfter();Actor.AddPart(fail);Assert.False(Act(Book,Reink));Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(1,Units("InkVial"));Assert.AreSame(Actor,vial.GetPart<PhysicsPart>().InInventory);
            Actor.RemovePart(fail);Assert.True(Act(Book,Reink));Assert.AreEqual(8,Charges.Charges);
        }
        [Test] public void LastVialCannotAlsoPayTheExistingRentalInkAction()
        {var vial=Supply("InkVial");int before=RentalSystem.GetInk(Actor);Assert.True(Act(Book,Reink));Assert.False(Act(vial,"UseInkVial"));Assert.AreEqual(before,RentalSystem.GetInk(Actor));}
        [Test] public void ExistingRentalInkUseStillGivesTwentyFiveAndCannotAlsoReink()
        {var vial=Supply("InkVial");int before=RentalSystem.GetInk(Actor);Assert.True(Act(vial,"UseInkVial"));Assert.AreEqual(before+25,RentalSystem.GetInk(Actor));Assert.False(Act(Book,Reink));Assert.AreEqual(3,Charges.Charges);}
        [Test] public void OuterTransactionRollbackRestoresEvenAfterSuccessfulCommandExecution()
        {
            Supply("InkVial");var tx=new InventoryTransaction();var command=new PerformInventoryActionCommand(Book,Reink);
            Assert.True(command.Execute(new InventoryContext(Actor,Zone),tx).Success);Assert.AreEqual(8,Charges.Charges);tx.Rollback();
            Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(1,Units("InkVial"));Assert.True(Act(Book,Reink));
        }
    }

    public sealed class ConnectedInkDeskTests:ConnectedInkFixture
    {
        [Test] public void ActualIngredientsBecomeOneVialAndTheFeeReachesTheBoundWorker()
        {ConfigureDesk();Ingredients();Assert.True(Act(Desk,Prepare));Assert.AreEqual(0,Units("SootrootPulp"));Assert.AreEqual(0,Units("PitchpodResin"));Assert.AreEqual(1,Units("InkVial"));Assert.AreEqual(7,TradeSystem.GetDrams(Actor));Assert.AreEqual(10,TradeSystem.GetDrams(Worker));}
        [TestCase("SootrootPulp")][TestCase("PitchpodResin")]
        public void MissingIngredientRefusesWithoutCurrencyOrPartialConsumption(string missing)
        {ConfigureDesk();Ingredients();foreach(var item in Pack.Objects.Where(e=>e.BlueprintName==missing).ToArray())Pack.RemoveObject(item);Assert.False(Act(Desk,Prepare));Assert.AreEqual(0,Units("InkVial"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));Assert.AreEqual(7,TradeSystem.GetDrams(Worker));}
        [Test] public void PoorPlayerAndFullWorkerWalletBothRefuseAtomically()
        {ConfigureDesk();Ingredients();TradeSystem.SetDrams(Actor,2);Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));TradeSystem.SetDrams(Actor,10);TradeSystem.SetDrams(Worker,int.MaxValue);Assert.False(Act(Desk,Prepare));Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(0,Units("InkVial"));}
        [Test] public void DeadOrDistantWorkerCannotSupplyTheService()
        {ConfigureDesk();Ingredients();Worker.GetStat("Hitpoints").BaseValue=0;Assert.False(Act(Desk,Prepare));Worker.GetStat("Hitpoints").BaseValue=20;Assert.True(Zone.MoveEntity(Worker,10,10));Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));}
        [Test] public void IngredientStacksCanBeSeparateAndUnrelatedGoodsRemain()
        {ConfigureDesk();var a=Supply("SootrootPulp");var b=Supply("SootrootPulp");Supply("PitchpodResin",2);var other=Supply("FireClay",3);Assert.True(Act(Desk,Prepare));Assert.AreEqual(0,Units("SootrootPulp"));Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(3,other.GetPart<StackerPart>().StackCount);}
        [Test] public void OuterFailureRestoresInputsMergedInkAndBothWallets()
        {ConfigureDesk();Ingredients();var ink=Supply("InkVial",4);Actor.AddPart(new FailAfter());Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(4,ink.GetPart<StackerPart>().StackCount);Assert.AreEqual(10,TradeSystem.GetDrams(Actor));Assert.AreEqual(7,TradeSystem.GetDrams(Worker));}
        [Test] public void MissingOutputDefinitionRefusesBeforePayment()
        {ConfigureDesk();Ingredients();Factory.Blueprints.Remove("InkVial");Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));}
        [Test] public void UnconfiguredDeskCannotBorrowAnyNearbyIndexer()
        {ConfigureDesk();var part=Desk.Parts.Single(p=>p.Name=="BotanicalInkDesk");Desk.RemovePart(part);var type=part.GetType();Desk.AddPart((Part)Activator.CreateInstance(type));Ingredients();Assert.False(Act(Desk,Prepare));Assert.AreEqual(0,Units("InkVial"));}
        [Test] public void OuterTransactionCanRollBackTheCompleteService()
        {ConfigureDesk();Ingredients();var tx=new InventoryTransaction();var command=new PerformInventoryActionCommand(Desk,Prepare);Assert.True(command.Execute(new InventoryContext(Actor,Zone),tx).Success);tx.Rollback();Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(0,Units("InkVial"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));Assert.AreEqual(7,TradeSystem.GetDrams(Worker));}
    }
}
