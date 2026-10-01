using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class BotanicalProcessingAdversarialTests : BotanicalProcessingTests
    {
        [Test] public void DetachedInventoryActionCannotMintSupplies()
        {
            var e=GameEvent.New("InventoryAction"); e.SetParameter("Actor",Actor);e.SetParameter("Zone",Zone);e.SetParameter("Command",BotanicalProcessingPart.Command);
            Raw.FireEvent(e); Assert.False(e.Handled);e.Release();Assert.AreEqual(3,Units("RawPlant"));Assert.AreEqual(0,Units("ProcessedSupply"));
        }
        [Test] public void DuplicateSourceReferenceCannotPayOnceAndSurviveTwice(){Inventory.Objects.Add(Raw);Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void EquippedAliasCannotBeHiddenByCarriage(){Inventory.EquippedItems["Hand"]=Raw;Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [TestCase(0)][TestCase(21)] public void InvalidRawStackCannotProduce(int count){Raw.GetPart<StackerPart>().StackCount=count;Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void SelfRecipeRefuses(){Recipe.OutputBlueprint="RawPlant";Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void ActorMovedByFactoryCannotFinishRemotePreparation(){Probe(_=>Zone.MoveEntity(Actor,6,6));Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void RecipeChangedByFactoryCannotAlterPayment(){Probe(_=>Recipe.OutputCount=8);Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void RecipeReplacementDuringFactoryCannotFinish(){Probe(_=>{Raw.RemovePart(Recipe);Raw.AddPart(new BotanicalProcessingPart{OutputBlueprint="ProcessedSupply"});});Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void LaterInitializerCannotCorruptEarlierOutput(){Entity first=null;Recipe.OutputCount=2;Probe(e=>{if(first==null)first=e;else first.GetPart<StackerPart>().StackCount=2;});Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void DuplicateOutputIdentityRefusesWholeBatch(){Recipe.OutputCount=2;Probe(e=>e.ID="same-output");Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void OutputCannotAliasAnExistingCarriedIdentity(){Probe(e=>e.ID=Raw.ID);Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void OutputCannotAlreadyBeCarried(){Probe(e=>e.GetPart<PhysicsPart>().InInventory=Actor);Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void OutputCannotBecomeACreature(){Probe(e=>e.SetTag("Creature"));Assert.False(Act());Assert.AreEqual(0,Units("ProcessedSupply"));}
        [Test] public void CorruptMergeDestinationRefuses(){var old=Factory.CreateEntity("ProcessedSupply");Inventory.AddObject(old);old.GetPart<PhysicsPart>().InInventory=new Entity();Assert.False(Act());Assert.AreEqual(1,Units("ProcessedSupply"));Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void OuterFailureRestoresMergedOutputExactly(){var old=Factory.CreateEntity("ProcessedSupply");old.GetPart<StackerPart>().StackCount=4;Inventory.AddObject(old);Actor.AddPart(new ThrowAfter());Assert.False(Act());Assert.AreEqual(4,old.GetPart<StackerPart>().StackCount);Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void FactoryReentryCannotDoubleSpend(){bool? nested=null;Probe(_=>{MutationProbe.Callback=null;nested=Act();});Assert.True(Act());Assert.AreEqual(false,nested);Assert.AreEqual(2,Units("RawPlant"));Assert.AreEqual(1,Units("ProcessedSupply"));}
        [Test] public void StunnedActorCannotPrepare(){Actor.GetPart<StatusEffectsPart>().ApplyEffect(new StunnedEffect(2));Assert.False(Act());Assert.AreEqual(3,Units("RawPlant"));}
        [Test] public void ExactFinalCapacityAllowsWeightFreedByInput(){Inventory.MaxWeight=4;Assert.True(Act());Assert.AreEqual(4,Inventory.GetCarriedWeight());}
        [Test] public void RollbackLeavesTheNextValidAttemptUsable(){Inventory.MaxWeight=3;Assert.False(Act());Inventory.MaxWeight=4;Assert.True(Act());Assert.AreEqual(1,Units("ProcessedSupply"));}
    }
}
