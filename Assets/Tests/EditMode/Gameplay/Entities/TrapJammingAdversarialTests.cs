using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Ownership, world identity, transaction rollback, reentry, pure queries and save compatibility.</summary>
    public sealed class TrapJammingAdversarialTests : TrapJammingFixture
    {
        [TestCase("distant")][TestCase("dead")][TestCase("npc")][TestCase("stunned")]
        [TestCase("hidden")][TestCase("unexplored")][TestCase("invisible")][TestCase("target-creature")]
        [TestCase("target-carried")][TestCase("target-equipped")][TestCase("target-takeable")][TestCase("target-gone")]
        [TestCase("target-zero-hp")][TestCase("target-detached")][TestCase("foreign-zone")]
        [TestCase("wrong-render-owner")][TestCase("wrong-physics-owner")][TestCase("wrong-inventory-owner")]
        public void InvalidCurrentContextCannotSpendOrDisable(string fault)
        {
            var timber=Supply();switch(fault)
            {
                case "distant":Zone.MoveEntity(Actor,15,15);break;
                case "dead":Actor.GetStat("Hitpoints").BaseValue=0;break;
                case "npc":Actor.Tags.Remove("Player");break;
                case "stunned":Actor.ApplyEffect(new StunnedEffect());break;
                case "hidden":Zone.GetEntityCell(Target).IsVisible=false;break;
                case "unexplored":Zone.GetEntityCell(Target).Explored=false;break;
                case "invisible":Target.GetPart<RenderPart>().Visible=false;break;
                case "target-creature":Target.SetTag("Creature");break;
                case "target-carried":Target.GetPart<PhysicsPart>().InInventory=Actor;break;
                case "target-equipped":Target.GetPart<PhysicsPart>().Equipped=Actor;break;
                case "target-takeable":Target.GetPart<PhysicsPart>().Takeable=true;break;
                case "target-gone":Target.AddPart(new DestructiblePart{Gone=true});break;
                case "target-zero-hp":Target.AddPart(new DestructiblePart{HP=0});break;
                case "target-detached":Zone.RemoveEntity(Target);break;
                case "foreign-zone":Zone=new Zone("trap-jamming-tests");break;
                case "wrong-render-owner":Target.GetPart<RenderPart>().ParentEntity=Actor;break;
                case "wrong-physics-owner":Target.GetPart<PhysicsPart>().ParentEntity=Actor;break;
                case "wrong-inventory-owner":Inventory.ParentEntity=Target;break;
            }
            Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Records("TrapJammed"));Assert.Greater(Records("TrapJamRejected"),0);
        }
        [TestCase("zero")][TestCase("negative")][TestCase("foreign-owner")][TestCase("missing-owner")]
        [TestCase("equipped")][TestCase("equipped-alias")][TestCase("duplicate-reference")][TestCase("not-portable")]
        [TestCase("wrong-blueprint")][TestCase("wrong-physics-owner")][TestCase("wrong-stack-owner")][TestCase("ground")][TestCase("creature")]
        public void InvalidMaterialCannotBecomePayment(string fault)
        {
            var timber=Supply();var physics=timber.GetPart<PhysicsPart>();switch(fault)
            {
                case "zero":timber.GetPart<StackerPart>().StackCount=0;break;
                case "negative":timber.GetPart<StackerPart>().StackCount=-2;break;
                case "foreign-owner":physics.InInventory=Target;break;
                case "missing-owner":physics.InInventory=null;break;
                case "equipped":physics.Equipped=Actor;break;
                case "equipped-alias":Inventory.EquippedItems["hand"]=timber;break;
                case "duplicate-reference":Inventory.Objects.Add(timber);break;
                case "not-portable":physics.Takeable=false;break;
                case "wrong-blueprint":timber.BlueprintName="FireClay";break;
                case "wrong-physics-owner":physics.ParentEntity=Target;break;
                case "wrong-stack-owner":timber.GetPart<StackerPart>().ParentEntity=Target;break;
                case "ground":Zone.AddEntity(timber,4,4);physics.InInventory=Actor;break;
                case "creature":timber.SetTag("Creature");break;
            }
            int count=timber.GetPart<StackerPart>().StackCount;Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(count,timber.GetPart<StackerPart>().StackCount);
        }
        [Test] public void BeforeVetoLeavesTimberAndMechanismUntouched()
        {var timber=Supply();Actor.AddPart(new BeforeRefusal());Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Records("TrapJammed"));}
        [TestCase(1)][TestCase(3)] public void AfterExceptionRestoresSingletonOrStackAndMechanism(int count)
        {var timber=Supply(count);Actor.AddPart(new AfterAction{Callback=()=>throw new InvalidOperationException("rollback probe")});Assert.False(Act());Assert.False(Jammed);Assert.Contains(timber,Inventory.Objects);Assert.AreEqual(count,timber.GetPart<StackerPart>().StackCount);Assert.AreSame(Actor,timber.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(0,Records("TrapJammed"));}
        [Test] public void UnrelatedTransfersSurviveRollback()
        {var timber=Supply();var unrelated=Supply(1,"Other");var replacement=Make("Replacement",true);Actor.AddPart(new AfterAction{Callback=()=>{Inventory.RemoveObject(unrelated);Inventory.AddObject(replacement);throw new InvalidOperationException("late rollback");}});Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);Assert.False(Inventory.Objects.Contains(unrelated));Assert.Contains(replacement,Inventory.Objects);}
        [Test] public void ReentrantSameTrapCannotDoublePay()
        {var timber=Supply(4);bool? nested=null;Actor.AddPart(new AfterAction{Callback=()=>{if(nested==null){nested=false;nested=Act();}}});Assert.True(Act());Assert.AreEqual(false,nested);Assert.AreEqual(3,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Records("TrapJammed"));}
        [Test] public void BareEventCannotBypassReceipt()
        {var timber=Supply();var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)Actor);e.SetParameter("Zone",(object)Zone);e.SetParameter("Command",Command);Target.FireEvent(e);Assert.False(e.Handled);e.Release();Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);}
        [Test] public void StaleFacadeCannotDispatchReplacementPart()
        {var stale=Jam;Target.RemovePart(stale);stale.ParentEntity=Target;Jam=(Part)Activator.CreateInstance(JamType);Target.AddPart(Jam);var timber=Supply();Assert.False(Facade(stale));Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);}
        [TestCase(false)][TestCase(true)] public void DuplicateMechanismsAreNotSilentlyPaidAsOne(bool duplicateJam)
        {Target.AddPart(duplicateJam?(Part)Activator.CreateInstance(JamType):new FireTrapTriggerPart());var timber=Supply();Assert.False(Offer());Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);}
        [Test] public void PureRepeatedMenuQueriesDoNotPayLogOrDisable()
        {var timber=Supply();int before=Records("TrapJamRejected");for(int i=0;i<20;i++)Assert.True(Offer());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(before,Records("TrapJamRejected"));Assert.AreEqual(0,Records("TrapJammed"));}
        [Test] public void WrongMaterialDoesNotHideLaterValidTimber()
        {var wrong=Supply(3,"FireClay");Supply(1);Assert.True(Act());Assert.AreEqual(3,wrong.GetPart<StackerPart>().StackCount);}
        [Test] public void SeparateOwnerKeepsItsArmedState()
        {var other=Make("SpikeTrap",false);other.AddPart(new SpikeTrapTriggerPart());var otherJam=(Part)Activator.CreateInstance(JamType);other.AddPart(otherJam);Zone.AddEntity(other,7,4);Supply();Assert.True(Act());Assert.AreEqual(false,JamType.GetField("Jammed").GetValue(otherJam));}
        [Test] public void DisablingDoesNotHealDestructibleStructure()
        {Target.AddPart(new DestructiblePart{HP=3,MaxHP=30});Supply();Assert.True(Act());Assert.AreEqual(3,Target.GetPart<DestructiblePart>().HP);}
        [TestCase("duplicate-material")][TestCase("missing-material-id")][TestCase("duplicate-target")][TestCase("missing-target-id")][TestCase("target-actor-id")]
        public void HypothesisAmbiguousSavedIdentitiesCannotPay(string fault)
        {
            var timber=Supply();
            if(fault=="duplicate-material")Supply(3).ID=timber.ID;
            if(fault=="missing-material-id")timber.ID="";
            if(fault=="duplicate-target"){var other=Make("Other",false);other.ID=Target.ID;Assert.True(Zone.AddEntity(other,8,4));}
            if(fault=="missing-target-id")Target.ID="";
            if(fault=="target-actor-id")Target.ID=Actor.ID;
            Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);
        }
        [Test] public void HypothesisTwoLiveTrapsOnOneCellOnlyDisableThePaidOwner()
        {
            var other=Make("SpikeTrap",false);other.AddPart(new SpikeTrapTriggerPart());other.AddPart((Part)Activator.CreateInstance(JamType));
            Assert.True(Zone.AddEntity(other,5,4));Supply();Assert.True(Act());Assert.True(Jammed);
            Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.AreEqual(88,Actor.GetStatValue("Hitpoints"));
            Assert.NotNull(Zone.GetEntityCell(Target));Assert.IsNull(Zone.GetEntityCell(other));
        }
        [TestCase("moved")][TestCase("removed-part")][TestCase("replacement-part")]
        public void HypothesisPostActionOwnerChangesCannotAnimateStaleMechanism(string change)
        {
            Supply();int gestures=0;var previous=EntityVisualHooks.InteractionCallback;
            Actor.AddPart(new AfterAction{Callback=()=>
            {
                if(change=="moved"){Assert.True(Zone.MoveEntity(Target,6,4));Zone.GetCell(6,4).IsVisible=true;Zone.GetCell(6,4).Explored=true;}
                else {Target.RemovePart(Jam);if(change=="replacement-part"){Jam.ParentEntity=Target;var replacement=(Part)Activator.CreateInstance(JamType);Target.AddPart(replacement);JamType.GetField("Jammed").SetValue(replacement,true);}}
            }});
            try
            {
                EntityVisualHooks.InteractionCallback=(actor,target,zone)=>gestures++;
                Assert.True(Act());Assert.AreEqual(0,gestures);
                Assert.AreEqual(0,CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter{Category="event",Kind="InventoryCommitObserverFailed",Limit=20}).Records.Count);
            }
            finally{EntityVisualHooks.InteractionCallback=previous;}
        }
        public sealed class BeforeRefusal:Part {public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeInventoryAction";}
        public sealed class AfterAction:Part {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}}
    }
}
