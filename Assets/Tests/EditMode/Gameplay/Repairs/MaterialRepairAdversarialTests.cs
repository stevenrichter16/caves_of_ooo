using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Taxonomy sweep: ownership, stale references, malformed content,
    /// transaction rollback, reentry, persistence, functional bypasses and diagnostics.</summary>
    public sealed class MaterialRepairAdversarialTests
    {
        Entity actor,target; Zone zone; RepairablePart repair;
        InventoryPart Inventory => actor.GetPart<InventoryPart>();
        [SetUp] public void SetUp()
        {
            RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults(); Diag.ResetAll();
            zone=new Zone("repair-adversarial"); actor=Make("Player",false); actor.SetTag("Player");
            actor.Statistics["Hitpoints"]=new Stat{BaseValue=20,Max=20,Owner=actor}; actor.AddPart(new InventoryPart()); actor.AddPart(new StatusEffectsPart());
            Assert.True(zone.AddEntity(actor,4,4)); target=Make("DamagedWell",false);
            target.AddPart(new CompositionPart{MaterialsRaw="Masonry,Fiber"}); repair=new RepairablePart{RecipeId="clay-well-lining"};target.AddPart(repair);target.AddPart(new WellPart());Assert.True(zone.AddEntity(target,5,4));
        }
        [TearDown] public void TearDown(){RepairRecipeRegistry.ResetForTests();Diag.ResetAll();}
        static Entity Make(string name,bool portable)
        {var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName=name};e.AddPart(new PhysicsPart{Takeable=portable});e.AddPart(new RenderPart{DisplayName=name});return e;}
        Entity Supply(int count=2,string blueprint="FireClay")
        {var e=Make(blueprint,true);e.AddPart(new StackerPart{StackCount=count});Inventory.Objects.Add(e);e.GetPart<PhysicsPart>().InInventory=actor;return e;}
        bool Act()=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target,RepairablePart.RepairCommand),actor,zone).Success;
        int Records(string kind)=>DiagQuery.Apply(new DiagQuery.Filter{Category="furniture",Kind=kind,Limit=50}).Records.Count;
        // Invalid context may neither pay nor restore functionality.
        [TestCase("distant")][TestCase("dead")][TestCase("npc")][TestCase("stunned")][TestCase("target-creature")]
        [TestCase("target-carried")][TestCase("target-equipped")][TestCase("target-takeable")][TestCase("target-gone")]
        [TestCase("target-zero-hp")][TestCase("target-detached")][TestCase("foreign-zone")][TestCase("wrong-composition")]
        [TestCase("missing-composition")][TestCase("wrong-composition-owner")][TestCase("wrong-target-physics")][TestCase("wrong-inventory-owner")]
        public void Adversarial_InvalidContextPreservesMaterialsAndFault(string fault)
        {
            var supply=Supply();
            switch(fault)
            {
                case "distant":zone.MoveEntity(actor,15,15);break;
                case "dead":actor.GetStat("Hitpoints").BaseValue=0;break;
                case "npc":actor.Tags.Remove("Player");break;
                case "stunned":actor.ApplyEffect(new StunnedEffect());break;
                case "target-creature":target.SetTag("Creature");break;
                case "target-carried":target.GetPart<PhysicsPart>().InInventory=actor;break;
                case "target-equipped":target.GetPart<PhysicsPart>().Equipped=actor;break;
                case "target-takeable":target.GetPart<PhysicsPart>().Takeable=true;break;
                case "target-gone":target.AddPart(new DestructiblePart{Gone=true});break;
                case "target-zero-hp":target.AddPart(new DestructiblePart{HP=0});break;
                case "target-detached":zone.RemoveEntity(target);break;
                case "foreign-zone":zone=new Zone("repair-adversarial");break;
                case "wrong-composition":target.GetPart<CompositionPart>().MaterialsRaw="Metal";break;
                case "missing-composition":target.RemovePart(target.GetPart<CompositionPart>());break;
                case "wrong-composition-owner":target.GetPart<CompositionPart>().ParentEntity=actor;break;
                case "wrong-target-physics":target.GetPart<PhysicsPart>().ParentEntity=actor;break;
                case "wrong-inventory-owner":Inventory.ParentEntity=target;break;
            }
            Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(2,supply.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Records("ObjectRepaired"));Assert.Greater(Records("RepairRejected"),0);
        }
        // The native consume helper tests list membership only; repair must also
        // establish physical ownership before trusting material counts.
        [TestCase("negative")][TestCase("zero")][TestCase("short")][TestCase("equipped")][TestCase("foreign-owner")]
        [TestCase("missing-owner")][TestCase("wrong-physics-owner")][TestCase("wrong-stack-owner")][TestCase("not-portable")]
        [TestCase("creature")][TestCase("duplicate-reference")][TestCase("ground")][TestCase("wrong-blueprint")]
        public void Adversarial_ImpossibleSupplyCannotPay(string fault)
        {
            var item=Supply();var physical=item.GetPart<PhysicsPart>();
            switch(fault)
            {
                case "negative":item.GetPart<StackerPart>().StackCount=-2;break;
                case "zero":item.GetPart<StackerPart>().StackCount=0;break;
                case "short":item.GetPart<StackerPart>().StackCount=1;break;
                case "equipped":physical.Equipped=actor;break;
                case "foreign-owner":physical.InInventory=target;break;
                case "missing-owner":physical.InInventory=null;break;
                case "wrong-physics-owner":physical.ParentEntity=target;break;
                case "wrong-stack-owner":item.GetPart<StackerPart>().ParentEntity=target;break;
                case "not-portable":physical.Takeable=false;break;
                case "creature":item.SetTag("Creature");break;
                case "duplicate-reference":Inventory.Objects.Add(item);break;
                case "ground":zone.AddEntity(item,4,4);physical.InInventory=actor;break;
                case "wrong-blueprint":item.BlueprintName="SilverSand";break;
            }
            int count=item.GetPart<StackerPart>().StackCount;
            Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(count,item.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Records("ObjectRepaired"));
        }
        [TestCase(null)][TestCase("")][TestCase("CLAY-WELL-LINING")][TestCase("missing-fault")]
        public void Adversarial_UnknownFaultFailsClosed(string id)
        {repair.RecipeId=id;Supply();Assert.False(Act());Assert.True(RepairablePart.BlocksFunction(target));Assert.False(target.GetPart<WellPart>().IsUsable);}
        [Test] public void Adversarial_DirectEventWithoutTransactionCannotSpend()
        {
            var item=Supply();var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)zone);e.SetParameter("Command",RepairablePart.RepairCommand);
            target.FireEvent(e);Assert.False(e.Handled);e.Release();Assert.False(repair.Repaired);Assert.AreEqual(2,item.GetPart<StackerPart>().StackCount);
        }
        [Test] public void Adversarial_SeparateOwnerWithSameBlueprintKeepsItsFault()
        {
            var second=Make("DamagedWell",false);second.AddPart(new RepairablePart{RecipeId="clay-well-lining"});Supply();Assert.True(Act());Assert.False(second.GetPart<RepairablePart>().Repaired);
        }
        [Test] public void Adversarial_UnrelatedSuppliesChangedAfterPaymentSurviveRollback()
        {
            var clay=Supply();var unrelated=Supply(1,"Other");var replacement=Make("Replacement",true);
            actor.AddPart(new AfterAction{Callback=()=>{Inventory.RemoveObject(unrelated);Inventory.AddObject(replacement);throw new InvalidOperationException("late failure");}});
            Assert.False(Act());Assert.False(repair.Repaired);Assert.Contains(clay,Inventory.Objects);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);
            Assert.False(Inventory.Objects.Contains(unrelated));Assert.Contains(replacement,Inventory.Objects);Assert.AreEqual(0,Records("ObjectRepaired"));
        }
        [Test] public void Adversarial_ReentrantAfterActionCannotRepairTwice()
        {
            var clay=Supply(6);bool? nested=null;actor.AddPart(new AfterAction{Callback=()=>{if(nested==null){nested=false;nested=Act();}}});
            Assert.True(Act());Assert.AreEqual(false,nested);Assert.AreEqual(4,clay.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Records("ObjectRepaired"));
        }
        [Test] public void Adversarial_RefusedBeforeActionCannotConsumeOrRepair()
        {var clay=Supply();actor.AddPart(new BeforeRefusal());Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);}
        [TestCase(false)][TestCase(true)] public void Adversarial_NativeEntitySavePreservesFaultAndComposition(bool repaired)
        {
            if(repaired){Supply();Assert.True(Act());}
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteEntityReference(target);writer.WriteQueuedEntityBodies();stream.Position=0;
                var reader=new SaveReader(stream,null);var restored=reader.ReadEntityReference();reader.ReadEntityBodies();
                Assert.AreEqual(repaired,restored.GetPart<RepairablePart>().Repaired);Assert.AreEqual(repair.RecipeId,restored.GetPart<RepairablePart>().RecipeId);
                Assert.AreEqual("Masonry,Fiber",restored.GetPart<CompositionPart>().MaterialsRaw);Assert.AreSame(restored,restored.GetPart<RepairablePart>().ParentEntity);
                Assert.AreEqual(repaired,restored.GetPart<WellPart>().IsUsable);
            }
        }
        [Test] public void Adversarial_RepairDoesNotHealStructuralHP()
        {target.AddPart(new DestructiblePart{HP=3,MaxHP=30});Supply();Assert.True(Act());Assert.AreEqual(3,target.GetPart<DestructiblePart>().HP);}
        [TestCase("quantity-zero")][TestCase("quantity-negative")][TestCase("quantity-excess")][TestCase("invalid-json")][TestCase("empty")][TestCase("duplicate")]
        public void Adversarial_BadCatalogueInstallsNoPartialRecipes(string fault)
        {
            string json=Resources.Load<TextAsset>("Content/Data/Repairs/Tier1Repairs").text;
            switch(fault)
            {
                case "quantity-zero":json=json.Replace("\"Quantity\": 2","\"Quantity\": 0");break;
                case "quantity-negative":json=json.Replace("\"Quantity\": 2","\"Quantity\": -1");break;
                case "quantity-excess":json=json.Replace("\"Quantity\": 2","\"Quantity\": 2147483647");break;
                case "invalid-json":json="{";break;
                case "empty":json="{}";break;
                case "duplicate":json=json.Replace("timber-gate-frame","clay-well-lining");break;
            }
            Assert.IsNotEmpty(RepairRecipeRegistry.InitializeFromJson(json));Assert.AreEqual(0,RepairRecipeRegistry.Count);Supply();Assert.False(Act());Assert.False(repair.Repaired);
        }
        // Player-flow hypothesis pass after the initial taxonomy suite was green.
        [Test] public void Hypothesis_StalePartFacadeCannotDispatchReplacementFault()
        {
            var stale=repair;target.RemovePart(stale);stale.ParentEntity=target;
            repair=new RepairablePart{RecipeId="clay-well-lining"};target.AddPart(repair);var clay=Supply();
            Assert.False(stale.TryRepair(actor,zone));Assert.False(repair.Repaired);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);
        }
        [Test] public void Hypothesis_EquippedDictionaryAliasIsNotCarriedMaterial()
        {
            var clay=Supply();Inventory.EquippedItems["hand"]=clay;
            Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);
        }
        [Test] public void Hypothesis_DuplicateFaultCannotBeSilentlyPaidAsOne()
        {
            target.AddPart(new RepairablePart{RecipeId="rope-well-line"});Supply();Supply(1,"KnotflaxCord");
            Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(2,Inventory.Objects.Single(e=>e.BlueprintName=="FireClay").GetPart<StackerPart>().StackCount);
        }
        [Test] public void Hypothesis_DuplicateCompositionCannotHideMaterialMismatch()
        {
            target.AddPart(new CompositionPart{MaterialsRaw="Metal"});var clay=Supply();
            Assert.False(Act());Assert.False(repair.Repaired);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);
        }
        [Test] public void Hypothesis_RepairedWellFillsWithoutRemovingItsFaultRecord()
        {
            var vessel=Supply(1,"Waterskin");vessel.AddPart(new WaterskinPart{Capacity=4,Charges=0});
            Assert.False(WaterVesselService.TryAct(actor,vessel,zone,"FillWaterskin"));Supply();Assert.True(Act());
            Assert.True(WaterVesselService.TryAct(actor,vessel,zone,"FillWaterskin"));Assert.AreEqual(4,vessel.GetPart<WaterskinPart>().Charges);Assert.AreSame(repair,target.GetPart<RepairablePart>());
        }
        [Test] public void Hypothesis_RefusedPaymentCanBeRetriedAfterFindingRemainingMaterial()
        {
            Supply(1);Assert.False(Act());Supply(1);Assert.True(Act());Assert.False(Inventory.Objects.Any(e=>e.BlueprintName=="FireClay"));
        }
        [Test] public void Hypothesis_WrongMaterialDoesNotStopLaterValidStacksBeingUsed()
        {
            var wrong=Supply(5,"SilverSand");Supply(1);Supply(2);Assert.True(Act());Assert.AreEqual(5,wrong.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1,Inventory.Objects.Where(e=>e.BlueprintName=="FireClay").Sum(e=>e.GetPart<StackerPart>().StackCount));
        }
        [Test] public void Hypothesis_SavedRepairedFlagDoesNotRequireAnyRemainingSupplies()
        {
            Supply();Assert.True(Act());Assert.IsEmpty(Inventory.Objects);Assert.False(Act());Assert.True(repair.Repaired);Assert.True(target.GetPart<WellPart>().IsUsable);
        }
        public sealed class AfterAction:Part { public Action Callback; public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;} }
        public sealed class BeforeRefusal:Part {public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeInventoryAction";}
    }
}
