using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineMaterialProcessActionTests : FiftyWorldFixture
    {
        const string Find="ReleasedExtractionTool", Blank="RecoveredIronBlank", Prefix="MaterialProcess|";
        const string Data="{\"Processes\":[{\"Id\":\"dismantle\",\"SourceBlueprint\":\"ReleasedExtractionTool\",\"Mode\":\"Dismantle\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"CinderhookAxeHeadComponent\",\"FieldHaftComponent\"],\"OutputCounts\":[1,1]},{\"Id\":\"reclaim\",\"SourceBlueprint\":\"ReleasedExtractionTool\",\"Mode\":\"Reclaim\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"RecoveredIronBlank\",\"SalvagedTimber\"],\"OutputCounts\":[1,1]},{\"Id\":\"point\",\"SourceBlueprint\":\"RecoveredIronBlank\",\"Mode\":\"Process\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"IronSpikeComponent\"],\"OutputCounts\":[1]}]}";
        EntityFactory oldForge;
        [SetUp] public void SetupProcesses()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;
            foreach(var cell in Zone.Cells){cell.IsVisible=true;cell.Explored=true;}
            // Native authored fixture content, not a world/normal-save injection.
            AddInput(Find,5,46,"dismantle,reclaim");AddInput(Blank,3,8,"point");
            Assert.AreEqual(0,MaterialProcessRegistry.InitializeFromJson(Data).Count);
        }
        void AddInput(string name,int weight,int value,string ids)
        {
            if(Factory.Blueprints.ContainsKey(name))return;
            var basis=Factory.Blueprints["SalvagedTimber"];
            var bp=new Blueprint{Name=name,Baked=true,Tags=new Dictionary<string,string>(basis.Tags),Parts=basis.Parts.ToDictionary(p=>p.Key,p=>new Dictionary<string,string>(p.Value))};
            bp.Parts.Remove("PreparationRecipe");bp.Parts["Physics"]["Weight"]=weight.ToString();bp.Parts["Commerce"]["Value"]=value.ToString();
            bp.Parts["Render"]["DisplayName"]=name;bp.Parts["MaterialProcessInput"]=new Dictionary<string,string>{{"ProcessIds",ids}};
            Factory.Blueprints[name]=bp;
        }
        [TearDown] public void RestoreProcesses(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        string Cmd(Entity item,string id)=>Choice(item,Prefix+id+"|");
        Entity Packed(string bp)=>Pack.Objects.Single(e=>e.BlueprintName==bp);
        [TestCase("ordinary-random-zone")] [TestCase("another-unrelated-place")]
        public void SameRecoveryContractWorksOutsideItsAuthoredSite(string zoneId)
        {
            Zone.RemoveEntity(Actor);Zone=new Zone(zoneId);SettlementRuntime.ActiveZone=Zone;Assert.True(Zone.AddEntity(Actor,10,10));foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            Place("TinkersForge");var input=Carry(Find);string command=Cmd(input,"dismantle");
            Assert.True(Act(input,command));Assert.AreEqual(0,Count(Find));Assert.AreEqual(1,Count("CinderhookAxeHeadComponent"));Assert.AreEqual(1,Count("FieldHaftComponent"));
            Assert.False(Act(input,command));Assert.AreEqual(0,Count(Blank));Assert.AreEqual(0,Count("SalvagedTimber"));
        }
        [Test] public void DestructiveBranchCreatesFiniteStockAndExistingPackComponentOutsideTheForge()
        {
            var forge=Place("TinkersForge");var input=Carry(Find);Assert.True(Act(input,Cmd(input,"reclaim")));
            Assert.AreEqual(1,Count(Blank));Assert.AreEqual(1,Count("SalvagedTimber"));Assert.AreEqual(0,Count("CinderhookAxeHeadComponent"));
            Assert.True(Act(Packed(Blank),Cmd(Packed(Blank),"point")));Assert.AreEqual(1,Count("IronSpikeComponent"));Assert.AreEqual(0,Count(Blank));
            Zone.RemoveEntity(forge);Assert.True(Act(Packed("SalvagedTimber"),"PrepareRecipe|shape_haft"));Assert.AreEqual(1,Count("FieldHaftComponent"));
        }
        [Test] public void PartialSourceStackConsumesExactlyOneAndRetainsItsOtherOptions()
        {
            Place("TinkersForge");var input=Carry(Find);input.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(input,Cmd(input,"reclaim")));Assert.AreEqual(1,input.GetPart<StackerPart>().StackCount);
            Assert.True(Act(input,Cmd(input,"dismantle")));Assert.AreEqual(0,Count(Find));Assert.AreEqual(1,Count(Blank));Assert.AreEqual(1,Count("CinderhookAxeHeadComponent"));
        }
        [TestCase(false)] [TestCase(true)]
        public void MergedOutputConservesRecipientOnSuccessAndRollback(bool fail)
        {
            Place("TinkersForge");var input=Carry(Find);var timber=Carry("SalvagedTimber");timber.GetPart<StackerPart>().StackCount=4;
            string command=Cmd(input,"reclaim");if(fail)FailAfter();Assert.AreEqual(!fail,Act(input,command));
            Assert.AreEqual(fail?4:5,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(fail?1:0,Count(Find));Assert.AreEqual(fail?0:1,Count(Blank));
        }
        public sealed class AfterProbe : Part
        {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}}
        [TestCase("source-ground")] [TestCase("output-ground")] [TestCase("output-container")]
        [TestCase("output-count")] [TestCase("output-part")] [TestCase("output-weight")]
        [TestCase("actor-move")] [TestCase("forge-move")] [TestCase("forge-blueprint")] [TestCase("recipe-reload")]
        public void OuterMutationRollsBackOnlyTheParticipatingGoods(string mutation)
        {
            var forge=Place("TinkersForge");var input=Carry(Find);var independent=Carry("KnotflaxCord");var box=Place("Chest",12,10);
            string command=Cmd(input,"reclaim");Actor.AddPart(new AfterProbe{Callback=()=>
            {
                var output=Packed(Blank);
                if(mutation=="source-ground")Zone.AddEntity(input,10,10);
                if(mutation=="output-ground"){Pack.RemoveObject(output);Zone.AddEntity(output,10,10);}
                if(mutation=="output-container"){Pack.RemoveObject(output);box.GetPart<ContainerPart>().AddItem(output);}
                if(mutation=="output-count")output.GetPart<StackerPart>().StackCount++;
                if(mutation=="output-part")output.AddPart(new ContainerPart());
                if(mutation=="output-weight")output.GetPart<PhysicsPart>().Weight++;
                if(mutation=="actor-move"){Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,10,11);}
                if(mutation=="forge-move"){Zone.RemoveEntity(forge);Zone.AddEntity(forge,10,11);}
                if(mutation=="forge-blueprint")forge.BlueprintName="changed";
                if(mutation=="recipe-reload")MaterialProcessRegistry.InitializeFromJson(Data);
                Pack.RemoveObject(independent);Zone.AddEntity(independent,13,10);
            }});
            Assert.False(Act(input,command));Assert.AreEqual(1,Count(Find));Assert.AreEqual(0,Count(Blank));Assert.AreEqual(0,Count("SalvagedTimber"));
            Assert.Null(Zone.GetEntityCell(input));Assert.AreSame(Actor,input.GetPart<PhysicsPart>().InInventory);
            Assert.False(box.GetPart<ContainerPart>().Contents.Any(e=>e.BlueprintName==Blank));Assert.NotNull(Zone.GetEntityCell(independent));
        }
        [Test] public void CallbackCannotSplitAProtectedMergeRecipientThenKeepARefundedUnit()
        {
            Place("TinkersForge");var input=Carry(Find);var timber=Carry("SalvagedTimber");timber.GetPart<StackerPart>().StackCount=4;
            string command=Cmd(input,"reclaim");Entity split=null;Actor.AddPart(new AfterProbe{Callback=()=>{split=timber.GetPart<StackerPart>().SplitStack(1);throw new InvalidOperationException("after split");}});
            Assert.False(Act(input,command));Assert.Null(split);Assert.AreEqual(4,timber.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Count(Find));
            Assert.NotNull(timber.GetPart<StackerPart>().SplitStack(1),"Custody lease must release after rollback.");
        }
        [Test] public void NestedProcessCannotSpendAnotherSourceWithinTheSameAction()
        {
            Place("TinkersForge");var first=Carry(Find);var second=Carry(Find);first.GetPart<StackerPart>().StackCount=2;
            string command=Cmd(first,"reclaim");bool nested=true;var probe=new AfterProbe();probe.Callback=()=>{probe.Callback=null;nested=Act(first,command);};Actor.AddPart(probe);
            Assert.True(Act(first,command));Assert.False(nested);Assert.AreEqual(1,Count(Blank));
        }
        [Test] public void SavedSourceAndProductsRetainTheirFiniteProcessPermissions()
        {
            Place("TinkersForge");var input=Carry(Find);string id=input.ID;
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(input);Pack.RemoveObject(input);Pack.AddObject(loaded);Assert.AreEqual(id,loaded.ID);
            Assert.True(Act(loaded,Cmd(loaded,"reclaim")));var blank=Packed(Blank);var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(blank);Pack.RemoveObject(blank);Pack.AddObject(saved);
            Assert.True(Act(saved,Cmd(saved,"point")));Assert.AreEqual(1,Count("IronSpikeComponent"));Assert.AreEqual(0,Count(Find));
        }
        [TestCase("recipe")] [TestCase("quantity")] [TestCase("factory")]
        public void MenuSelectionCannotExecuteAChangedQuote(string mutation)
        {
            Place("TinkersForge");var input=Carry(Find);string command=Cmd(input,"reclaim");
            if(mutation=="recipe")MaterialProcessRegistry.InitializeFromJson(Data.Replace("[1,1]","[2,1]"));
            if(mutation=="quantity")input.GetPart<StackerPart>().StackCount=2;
            if(mutation=="factory")ForgePart.Factory=new EntityFactory();
            Assert.False(Act(input,command));Assert.AreEqual(0,Count(Blank));Assert.True(Pack.Objects.Contains(input));
        }
        public sealed class BeforeProcessProbe : Part
        {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMaterialProcess")Callback?.Invoke();return true;}}
        [Test] public void BeforeProcessCannotChangeTheQuotedProductTemplate()
        {
            var forge=Place("TinkersForge");var probe=new BeforeProcessProbe{Callback=()=>Factory.Blueprints[Blank].Parts["Commerce"]["Value"]="999"};forge.AddPart(probe);
            var input=Carry(Find);string command=Cmd(input,"reclaim");
            Assert.False(Act(input,command));Assert.AreEqual(1,Count(Find));Assert.AreEqual(0,Count(Blank));
        }
        [TestCase("properties")] [TestCase("statistics")]
        public void BlueprintAndProductCannotChangeTheirMetadataTogether(string mutation)
        {
            Place("TinkersForge");var input=Carry(Find);string command=Cmd(input,"reclaim");
            Actor.AddPart(new AfterProbe{Callback=()=>
            {
                var output=Packed(Blank);
                if(mutation=="properties"){Factory.Blueprints[Blank].Props["unquoted"]="yes";output.Properties["unquoted"]="yes";}
                if(mutation=="statistics")output.Statistics["unquoted"]=new Stat{Owner=output,Name="unquoted",BaseValue=999};
            }});
            Assert.False(Act(input,command));Assert.AreEqual(1,Count(Find));Assert.AreEqual(0,Count(Blank));
        }
        [Test] public void OrdinaryNonPlayerActorUsesTheSameRules()
        {Place("TinkersForge");var input=Carry(Find);Actor.Tags.Remove("Player");Assert.True(Act(input,Cmd(input,"reclaim")));Assert.AreEqual(1,Count(Blank));}
        public sealed class SourceBehavior : Part
        {public string Mark="one";public int Completed;public override bool HandleEvent(GameEvent e){if(e.ID=="MaterialProcessCompleted")Completed++;return true;}}
        sealed class SourceBehaviorPolicy : IMaterialProcessInstancePartPolicy
        {
            public bool AllowsInstance(Part part)=>part is SourceBehavior;
            public bool ValidTemplate(IReadOnlyDictionary<string,string> p)=>false;
            public bool MatchesTemplate(Part p,IReadOnlyDictionary<string,string> values)=>false;
            public object Capture(Part part)=>((SourceBehavior)part).Mark;
            public bool Matches(Part part,object token)=>((SourceBehavior)part).Mark==(string)token;
        }
        [TestCase(false)] [TestCase(true)]
        public void ARegisteredBehaviorCanAttachToOneInputWithoutChangingItsBlueprint(bool mutate)
        {
            var policy=new SourceBehaviorPolicy();MaterialProcessPayloadPolicies.Register(typeof(SourceBehavior),policy);
            try
            {
                Place("TinkersForge");var input=Carry(Find);var behavior=new SourceBehavior();input.AddPart(behavior);string command=Cmd(input,"reclaim");
                if(mutate)behavior.Mark="changed";
                Assert.AreEqual(!mutate,Act(input,command));Assert.AreEqual(mutate?0:1,behavior.Completed);
                Assert.AreEqual(mutate?1:0,Count(Find));Assert.AreEqual(mutate?0:1,Count(Blank));
            }finally{MaterialProcessPayloadPolicies.Unregister(typeof(SourceBehavior),policy);}
        }
        [TestCase(false)] [TestCase(true)]
        public void PickingUpAnInstanceBehaviorDoesNotSpreadOrEraseIt(bool behaviorFirst)
        {
            var policy=new SourceBehaviorPolicy();MaterialProcessPayloadPolicies.Register(typeof(SourceBehavior),policy);
            try
            {
                var first=Factory.CreateEntity(Find);var second=Factory.CreateEntity(Find);var behavior=new SourceBehavior();
                (behaviorFirst?first:second).AddPart(behavior);Assert.True(Pack.AddObject(first));Assert.True(Pack.AddObject(second));
                Assert.AreEqual(2,Pack.Objects.Count(e=>e.BlueprintName==Find));Assert.AreEqual(1,first.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,second.GetPart<StackerPart>().StackCount);
                Assert.AreSame(behavior,(behaviorFirst?first:second).GetPart<SourceBehavior>());
            }finally{MaterialProcessPayloadPolicies.Unregister(typeof(SourceBehavior),policy);}
        }
        [Test] public void UnsafeInstanceBehaviorCannotBeImplicitlyClonedBySplitting()
        {
            var policy=new SourceBehaviorPolicy();MaterialProcessPayloadPolicies.Register(typeof(SourceBehavior),policy);
            try{var input=Carry(Find);input.AddPart(new SourceBehavior());input.GetPart<StackerPart>().StackCount=2;Assert.Null(input.GetPart<StackerPart>().SplitStack(1));Assert.AreEqual(2,input.GetPart<StackerPart>().StackCount);}
            finally{MaterialProcessPayloadPolicies.Unregister(typeof(SourceBehavior),policy);}
        }
        [Test] public void OrdinaryProcessingInputsRetainConvenientStackingAndSplitting()
        {var first=Carry(Find);Carry(Find);Assert.AreEqual(2,first.GetPart<StackerPart>().StackCount);Assert.NotNull(first.GetPart<StackerPart>().SplitStack(1));}
        public sealed class CreationEffect : Part
        {public static bool Called;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Called=true;return true;}}
        sealed class PayloadOnlyPolicy : IMaterialProcessPartPolicy
        {
            public bool ValidTemplate(IReadOnlyDictionary<string,string> p)=>true;
            public bool MatchesTemplate(Part p,IReadOnlyDictionary<string,string> values)=>true;
            public object Capture(Part p)=>0;
            public bool Matches(Part p,object token)=>true;
        }
        [Test] public void AStatePolicyCannotAuthorizeUnreviewedOutputCreationCallbacks()
        {
            Place("TinkersForge");var input=Carry(Find);var policy=new PayloadOnlyPolicy();MaterialProcessPayloadPolicies.Register(typeof(CreationEffect),policy);
            Factory.RegisterPartType<CreationEffect>("CreationEffect");Factory.Blueprints[Blank].Parts["CreationEffect"]=new Dictionary<string,string>();CreationEffect.Called=false;
            try{Assert.False(Actions(input).Any(a=>a.Command.StartsWith(Prefix+"reclaim|")));Assert.False(CreationEffect.Called);Assert.AreEqual(1,Count(Find));}
            finally{MaterialProcessPayloadPolicies.Unregister(typeof(CreationEffect),policy);CreationEffect.Called=false;}
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(true,true)]
        public void AllSeparationPathsPreserveInstanceBehavior(bool direct,bool behavior)
        {
            var policy=new SourceBehaviorPolicy();MaterialProcessPayloadPolicies.Register(typeof(SourceBehavior),policy);
            try
            {
                var item=Carry(Find);item.GetPart<StackerPart>().StackCount=2;if(behavior)item.AddPart(new SourceBehavior());
                if(direct)
                {if(behavior)Assert.Throws<InvalidOperationException>(()=>item.CloneForStack());else Assert.NotNull(item.CloneForStack());}
                else
                {
                    var command=new CavesOfOoo.Core.Inventory.Commands.SeparateOneCommand(item);
                    var result=InventorySystem.ExecuteCommand(command,Actor,Zone);Assert.AreEqual(!behavior,result.Success);
                    if(behavior)Assert.AreEqual(2,item.GetPart<StackerPart>().StackCount);
                    else Assert.NotNull(command.SeparatedItem);
                }
            }finally{MaterialProcessPayloadPolicies.Unregister(typeof(SourceBehavior),policy);}
        }
        public sealed class ProcessEffect : Part
        {public bool Veto;public int Completed;public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMaterialProcess"&&Veto)return false;if(e.ID=="MaterialProcessCompleted")Completed++;return true;}}
        [TestCase(false)] [TestCase(true)]
        public void PerInstanceMachineBehaviorCanVetoOrObserveCommittedProcess(bool veto)
        {
            var forge=Place("TinkersForge");var behavior=new ProcessEffect{Veto=veto};forge.AddPart(behavior);var input=Carry(Find);string command=Cmd(input,"reclaim");
            Assert.AreEqual(!veto,Act(input,command));Assert.AreEqual(veto?0:1,behavior.Completed);Assert.AreEqual(veto?1:0,Count(Find));
        }
        [Test] public void CompletionEffectsNeverFireForRolledBackWork()
        {var forge=Place("TinkersForge");var behavior=new ProcessEffect();forge.AddPart(behavior);var input=Carry(Find);string command=Cmd(input,"reclaim");FailAfter();Assert.False(Act(input,command));Assert.AreEqual(0,behavior.Completed);}
    }
}
