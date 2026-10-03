using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Exact ownership, callback, transaction, stale service and saved-graph counter-checks.</summary>
    public sealed class ConnectedInkAdversarialTests : ConnectedInkFixture
    {
        [TestCase("missing-backref")][TestCase("equipped-alias")][TestCase("duplicate-id")][TestCase("invalid-stack")]
        public void CorruptVialCannotPayForBookCharges(string fault)
        {
            var vial=Supply("InkVial");
            switch(fault)
            {
                case "missing-backref":vial.GetPart<PhysicsPart>().InInventory=null;break;
                case "equipped-alias":vial.GetPart<PhysicsPart>().Equipped=Actor;break;
                case "duplicate-id":Supply("Unrelated").ID=vial.ID;break;
                case "invalid-stack":vial.GetPart<StackerPart>().StackCount=0;break;
            }
            Assert.False(Act(Book,Reink));Assert.AreEqual(3,Charges.Charges);Assert.Contains(vial,Pack.Objects);
        }
        [Test] public void DuplicateBookPartCannotRefillTheFirstOrSecondPart()
        {
            Supply("InkVial");var second=new GrimoireChargePart{Charges=0};Book.AddPart(second);
            Assert.False(Act(Book,Reink));Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(0,second.Charges);Assert.AreEqual(1,Units("InkVial"));
        }
        [Test] public void BlockedPlayerCannotUseEitherInkAction()
        {
            ConfigureDesk();Ingredients();Supply("InkVial");Actor.GetPart<StatusEffectsPart>().ApplyEffect(new StunnedEffect{Duration=2});
            Assert.False(Act(Book,Reink));Assert.False(Act(Desk,Prepare));Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(2,Units("SootrootPulp"));
        }
        [Test] public void VeryLargePositiveRefillCapsWithoutArithmeticWrap()
        {
            Supply("InkVial");Charges.Charges=int.MaxValue-2;Charges.MaxCharges=int.MaxValue;Charges.ChargesPerVial=int.MaxValue;
            Assert.True(Act(Book,Reink));Assert.AreEqual(int.MaxValue,Charges.Charges);Assert.AreEqual(0,Units("InkVial"));
        }
        [Test] public void SelectedRefillPreservesFirstQualifyingBookCastAuthority()
        {
            var second=Supply("OtherBook");second.AddPart(new GrimoirePart{SkillClassName="Rites_ConsumingGale"});
            var secondInk=new GrimoireChargePart{Charges=0};second.AddPart(secondInk);Supply("InkVial");
            Assert.True(Act(second,Reink));Assert.AreEqual(5,secondInk.Charges);Assert.AreSame(Charges,GrimoireInk.FindInked(Actor));
            Assert.True(GrimoireInk.FindInked(Actor).TrySpend());Assert.AreEqual(2,Charges.Charges);Assert.AreEqual(5,secondInk.Charges);
        }
        [Test] public void ReentrantAfterActionCannotRefillTwice()
        {
            Supply("InkVial",3);bool? nested=null;Actor.AddPart(new AfterProbe{Callback=()=>{if(nested==null){nested=false;nested=Act(Book,Reink);}}});
            Assert.True(Act(Book,Reink));Assert.AreEqual(false,nested);Assert.AreEqual(8,Charges.Charges);Assert.AreEqual(2,Units("InkVial"));
        }
        [TestCase("worker-hostile")][TestCase("worker-frozen")][TestCase("moved-desk")][TestCase("distant-player")]
        public void StaleOrUnavailableServiceRefusesWithoutPayment(string fault)
        {
            ConfigureDesk();Ingredients();
            switch(fault)
            {
                case "worker-hostile":Worker.GetPart<BrainPart>().SetPersonallyHostile(Actor,false);break;
                case "worker-frozen":Worker.AddPart(new StatusEffectsPart());Worker.GetPart<StatusEffectsPart>().ApplyEffect(new FrozenEffect{Duration=2});break;
                case "moved-desk":Assert.True(Zone.MoveEntity(Desk,5,5));break;
                case "distant-player":Assert.True(Zone.MoveEntity(Actor,2,2));break;
            }
            Assert.False(Act(Desk,Prepare));Assert.AreEqual(0,Units("InkVial"));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));
        }
        [TestCase("blueprint")][TestCase("stack-replacement")][TestCase("count")][TestCase("physical")]
        public void OutputInitializerCannotSubstituteAnAlreadyQuotedIngredient(string fault)
        {
            ConfigureDesk();Ingredients();var pulp=Pack.Objects.Single(e=>e.BlueprintName=="SootrootPulp");
            Probe(_=>
            {
                switch(fault)
                {
                    case "blueprint":pulp.BlueprintName="FireClay";break;
                    case "stack-replacement":pulp.RemovePart(pulp.GetPart<StackerPart>());pulp.AddPart(new StackerPart{StackCount=2,MaxStack=20});break;
                    case "count":pulp.GetPart<StackerPart>().StackCount=1;break;
                    case "physical":pulp.GetPart<PhysicsPart>().InInventory=null;break;
                }
            });
            Assert.False(Act(Desk,Prepare));Assert.AreEqual(0,Units("InkVial"));Assert.Contains(pulp,Pack.Objects);Assert.AreEqual(1,Units("PitchpodResin"));
            Assert.AreEqual(10,TradeSystem.GetDrams(Actor));Assert.AreEqual(7,TradeSystem.GetDrams(Worker));
        }
        [TestCase("owned")][TestCase("duplicate-id")][TestCase("wrong-ink")][TestCase("excess-stack")]
        public void InvalidProducedVialDoesNotConsumeInputs(string fault)
        {
            ConfigureDesk();Ingredients();Probe(output=>
            {
                switch(fault)
                {
                    case "owned":output.GetPart<PhysicsPart>().InInventory=Worker;break;
                    case "duplicate-id":output.ID=Actor.ID;break;
                    case "wrong-ink":output.GetPart<InkVialPart>().InkAmount=1000;break;
                    case "excess-stack":output.GetPart<StackerPart>().StackCount=2;break;
                }
            });
            Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(0,Units("InkVial"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));
        }
        [Test] public void ReentrantOutputInitializerCannotChargeForASecondService()
        {
            ConfigureDesk();Supply("SootrootPulp",4);Supply("PitchpodResin",2);bool? nested=null;
            Probe(_=>{if(nested==null){nested=false;nested=Act(Desk,Prepare);}});
            Assert.True(Act(Desk,Prepare));Assert.AreEqual(false,nested);Assert.AreEqual(1,Units("InkVial"));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(7,TradeSystem.GetDrams(Actor));
        }
        [Test] public void AFactoryCallbackCannotMoveTheWorkerAndStillCompleteItsService()
        {ConfigureDesk();Ingredients();Probe(_=>Zone.MoveEntity(Worker,10,10));Assert.False(Act(Desk,Prepare));Assert.AreEqual(2,Units("SootrootPulp"));Assert.AreEqual(0,Units("InkVial"));}
        [Test] public void CapacityRefusalRestoresTheExactInputStacks()
        {
            ConfigureDesk();Ingredients();var pulp=Pack.Objects.Single(e=>e.BlueprintName=="SootrootPulp");
            Factory.Blueprints["InkVial"].Parts["Physics"]["Weight"]="100";Pack.MaxWeight=20;
            Assert.False(Act(Desk,Prepare));Assert.Contains(pulp,Pack.Objects);Assert.AreEqual(2,pulp.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Units("PitchpodResin"));Assert.AreEqual(0,Units("InkVial"));Assert.AreEqual(10,TradeSystem.GetDrams(Actor));
        }
        [Test] public void TheServiceCannotGrantInkOutsideAnOuterTransaction()
        {
            ConfigureDesk();Ingredients();Supply("InkVial");
            foreach(var entry in new[]{(target:Book,command:Reink),(target:Desk,command:Prepare)})
            {
                var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",Actor);e.SetParameter("Zone",Zone);e.SetParameter("Command",entry.command);
                Assert.True(entry.target.FireEvent(e));Assert.False(e.Handled);e.Release();
            }
            Assert.AreEqual(3,Charges.Charges);Assert.AreEqual(1,Units("InkVial"));Assert.AreEqual(2,Units("SootrootPulp"));
        }
        [Test] public void NativeSaveGraphKeepsRefilledBookAndExactAssignedWorker()
        {
            ConfigureDesk();Ingredients();Assert.True(Act(Desk,Prepare));Assert.True(Act(Book,Reink));
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteEntityReference(Actor);writer.WriteEntityReference(Desk);writer.WriteEntityReference(Worker);writer.WriteQueuedEntityBodies();stream.Position=0;
                var reader=new SaveReader(stream,null);var restoredActor=reader.ReadEntityReference();var restoredDesk=reader.ReadEntityReference();var restoredWorker=reader.ReadEntityReference();reader.ReadEntityBodies();
                Assert.AreNotSame(Actor,restoredActor);var restoredBook=restoredActor.GetPart<InventoryPart>().Objects.Single(e=>e.ID==Book.ID);
                Assert.AreEqual(8,restoredBook.GetPart<GrimoireChargePart>().Charges);Assert.AreSame(restoredActor,restoredBook.GetPart<PhysicsPart>().InInventory);
                var restoredPart=restoredDesk.GetPart<BotanicalInkDeskPart>();Assert.AreSame(restoredWorker,restoredPart.Worker);Assert.True(restoredPart.Configured);
                var loadedZone=new Zone(Zone.ZoneID);Assert.True(loadedZone.AddEntity(restoredActor,4,4));Assert.True(loadedZone.AddEntity(restoredDesk,5,4));Assert.True(loadedZone.AddEntity(restoredWorker,6,4));
                Zone=loadedZone;Actor=restoredActor;Book=restoredBook;Desk=restoredDesk;Worker=restoredWorker;Ingredients();Assert.True(Act(Desk,Prepare));Assert.AreEqual(4,TradeSystem.GetDrams(Actor));
            }
        }
        public sealed class AfterProbe:Part
        {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}}
    }
}
