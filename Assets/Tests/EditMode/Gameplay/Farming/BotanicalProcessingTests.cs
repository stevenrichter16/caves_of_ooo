using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class BotanicalProcessingTests
    {
        protected Zone Zone; protected Entity Actor, Raw;
        protected EntityFactory Factory; protected BotanicalProcessingPart Recipe;
        EntityFactory previous; protected InventoryPart Inventory => Actor.GetPart<InventoryPart>();
        [SetUp] public void SetUp()
        {
            previous=SeedPart.Factory; Factory=new EntityFactory();
            Factory.LoadBlueprints(@"{""Objects"":[{""Name"":""ProcessedSupply"",""Parts"":[{""Name"":""Physics"",""Params"":[{""Key"":""Takeable"",""Value"":""true""},{""Key"":""Weight"",""Value"":""2""}]},{""Name"":""Stacker"",""Params"":[{""Key"":""MaxStack"",""Value"":""20""}]},{""Name"":""Render"",""Params"":[{""Key"":""RenderString"",""Value"":""~""},{""Key"":""DisplayName"",""Value"":""processed supply""}]}],""Tags"":[{""Key"":""Item"",""Value"":""true""}]}]}");
            SeedPart.Factory=Factory; Zone=new Zone("processing-test");
            Actor=new Entity{ID="actor",BlueprintName="Player"};Actor.SetTag("Player");Actor.AddPart(new PhysicsPart());Actor.AddPart(new InventoryPart());Actor.AddPart(new StatusEffectsPart());
            Actor.Statistics["Hitpoints"]=new Stat{Owner=Actor,BaseValue=20,Max=20};Assert.True(Zone.AddEntity(Actor,4,4));
            Raw=new Entity{ID="raw",BlueprintName="RawPlant"};Raw.SetTag("Item");Raw.AddPart(new PhysicsPart{Takeable=true,Weight=1});Raw.AddPart(new RenderPart{DisplayName="raw plant"});Raw.AddPart(new StackerPart{StackCount=3,MaxStack=20});
            Recipe=new BotanicalProcessingPart{OutputBlueprint="ProcessedSupply",OutputCount=1,ActionText="prepare plant fiber"};Raw.AddPart(Recipe);Assert.True(Inventory.AddObject(Raw));MessageLog.Clear();
        }
        [TearDown] public void TearDown(){SeedPart.Factory=previous;MutationProbe.Callback=null;}
        protected bool Act()=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(Raw,BotanicalProcessingPart.Command),Actor,Zone).Success;
        protected int Units(string bp)=>Inventory.Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        protected void Probe(Action<Entity> action){MutationProbe.Callback=action;Factory.RegisterPartType<MutationProbe>("MutationProbe");Factory.Blueprints["ProcessedSupply"].Parts["MutationProbe"]=new System.Collections.Generic.Dictionary<string,string>();}
        [Test] public void OneCarriedUnitProducesTheDeclaredUsefulSupply(){Assert.True(Act());Assert.AreEqual(2,Units("RawPlant"));Assert.AreEqual(1,Units("ProcessedSupply"));Assert.True(Inventory.Objects.Where(e=>e.BlueprintName=="ProcessedSupply").All(e=>e.GetPart<PhysicsPart>().InInventory==Actor));}
        [Test] public void LastUnitIsRemovedAndRepeatedUseCannotDuplicateOutput(){Raw.GetPart<StackerPart>().StackCount=1;Assert.True(Act());Assert.False(Inventory.Objects.Contains(Raw));Assert.False(Act());Assert.AreEqual(1,Units("ProcessedSupply"));}
        [Test] public void CountAndExistingCompatibleStackAreRespected(){var old=Factory.CreateEntity("ProcessedSupply");old.GetPart<StackerPart>().StackCount=2;Inventory.AddObject(old);Recipe.OutputCount=2;Assert.True(Act());Assert.AreEqual(4,Units("ProcessedSupply"));Assert.AreEqual(2,Units("RawPlant"));}
        [Test] public void InsufficientCapacityPreservesInputAndExistingStacks(){Inventory.MaxWeight=Inventory.GetCarriedWeight();Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void MissingFactoryOrOutputRefusesWithoutConsumption(){SeedPart.Factory=null;Assert.False(Act());SeedPart.Factory=Factory;Recipe.OutputBlueprint="Missing";Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [TestCase(0)][TestCase(-1)][TestCase(9)][TestCase(int.MaxValue)] public void InvalidOutputCountRefuses(int count){Recipe.OutputCount=count;Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void DroppedOrForeignOrEquippedRawCannotBeProcessed(){Inventory.RemoveObject(Raw);Zone.AddEntity(Raw,4,4);Assert.False(Act());Zone.RemoveEntity(Raw);Inventory.AddObject(Raw);Raw.GetPart<PhysicsPart>().InInventory=new Entity();Assert.False(Act());Raw.GetPart<PhysicsPart>().InInventory=Actor;Raw.GetPart<PhysicsPart>().Equipped=Actor;Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void BeforeVetoAndAfterThrowPreserveInputAndDoNotLeaveOutput(){var veto=new Veto();Actor.AddPart(veto);Assert.False(Act());Actor.RemovePart(veto);Actor.AddPart(new ThrowAfter());Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void DeadOrDistantContextCannotProcess(){Actor.GetStat("Hitpoints").BaseValue=0;Assert.False(Act());Actor.GetStat("Hitpoints").BaseValue=20;Zone.RemoveEntity(Actor);Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        public sealed class MutationProbe:Part{public static Action<Entity> Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
        public sealed class Veto:Part{public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeInventoryAction";}
        public sealed class ThrowAfter:Part{public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("processing rollback probe");return true;}}
    }
}
