using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class MachineLeatherFeedTests : FiftyWorldFixture
    {
        const string Input="CuredLeatherOffcut", Output="LeatherBindingComponent", Prefix="MaterialProcess|cut_binding|";
        const string Data="{\"Processes\":[{\"Id\":\"cut_binding\",\"SourceBlueprint\":\"CuredLeatherOffcut\",\"Mode\":\"Process\",\"InputCount\":1,\"WorkKind\":\"HandCutting\",\"OutputBlueprints\":[\"LeatherBindingComponent\"],\"OutputCounts\":[1]}]}";
        EntityFactory oldForge;
        [SetUp] public void SetupCutting()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;
            if(!Factory.Blueprints.ContainsKey(Input))
            {
                var basis=Factory.Blueprints["SalvagedTimber"];
                var bp=new Blueprint{Name=Input,Baked=true,Tags=new Dictionary<string,string>(basis.Tags),Parts=basis.Parts.ToDictionary(p=>p.Key,p=>new Dictionary<string,string>(p.Value))};
                bp.Parts.Remove("PreparationRecipe");bp.Parts["Physics"]["Weight"]="4";bp.Parts["Commerce"]["Value"]="4";
                bp.Parts["MaterialProcessInput"]=new Dictionary<string,string>{{"ProcessIds","cut_binding"}};Factory.Blueprints[Input]=bp;
            }
            Assert.AreEqual(0,MaterialProcessRegistry.InitializeFromJson(Data).Count,"The bounded HandCutting contract must be admitted.");
        }
        [TearDown] public void RestoreCutting(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        static Type CapabilityType=>typeof(Part).Assembly.GetType("CavesOfOoo.Core.CuttingToolPart");
        Part Capability(Entity tool)
        {
            Assert.NotNull(CapabilityType,"Explicit reusable cutting capability is missing.");
            var cap=tool.Parts.FirstOrDefault(p=>p.GetType()==CapabilityType);
            if(cap==null){cap=(Part)Activator.CreateInstance(CapabilityType);tool.AddPart(cap);}return cap;
        }
        Entity Knife(string id="knife") {var tool=Factory.CreateEntity("Dagger");tool.ID=id;tool.GetPart<RenderPart>().DisplayName="fixture tool "+id;Capability(tool);Assert.True(Pack.AddObject(tool));return tool;}
        string Cmd(Entity source)=>Choice(source,Prefix);
        bool HasChoice(Entity source)=>Actions(source).Any(a=>a.Command.StartsWith(Prefix));
        [TestCase("ordinary-one",false)] [TestCase("ordinary-two",false)] [TestCase("ordinary-one",true)]
        public void ExplicitCarriedOrHeldToolWorksAwayFromAnyForge(string zoneId,bool held)
        {
            Zone.RemoveEntity(Actor);Zone=new Zone(zoneId);SettlementRuntime.ActiveZone=Zone;Assert.True(Zone.AddEntity(Actor,10,10));
            var tool=Knife();if(held){Assert.True(InventorySystem.Equip(Actor,tool));Assert.NotNull(Pack.FindEquippedBodyPart(tool));}
            var source=Carry(Input);Assert.True(Act(source,Cmd(source)));Assert.AreEqual(0,Count(Input));Assert.AreEqual(1,Count(Output));
            Assert.AreEqual(held,tool.GetPart<PhysicsPart>().Equipped==Actor);Assert.AreEqual(held?0:1,Pack.Objects.Count(e=>e==tool));
        }
        [Test] public void ACapableNonDaggerUsesTheSameContract()
        {var tool=Carry("Mace");Capability(tool);var source=Carry(Input);Assert.True(Act(source,Cmd(source)));Assert.AreEqual(1,Count(Output));}
        [TestCase("absent")] [TestCase("no-capability")] [TestCase("disabled")] [TestCase("broken")]
        [TestCase("foreign")] [TestCase("duplicate-capability")] [TestCase("duplicate-id")] [TestCase("pack-and-held")]
        public void UnsupportedToolContextDoesNotExposeOrSpendWork(string defect)
        {
            var source=Carry(Input);Entity tool=null;
            if(defect!="absent")tool=Knife();
            if(defect=="no-capability")tool.RemovePart(Capability(tool));
            if(defect=="disabled")CapabilityType.GetField("FlexibleStock").SetValue(Capability(tool),false);
            if(defect=="broken")tool.ApplyEffect(new BrokenEffect(),Actor,Zone);
            if(defect=="foreign"){Pack.RemoveObject(tool);Zone.AddEntity(tool,11,10);}
            if(defect=="duplicate-capability")tool.AddPart((Part)Activator.CreateInstance(CapabilityType));
            if(defect=="duplicate-id")Carry("Mace").ID=tool.ID;
            if(defect=="pack-and-held"){Assert.True(InventorySystem.Equip(Actor,tool));Pack.Objects.Add(tool);}
            Assert.False(HasChoice(source));Assert.AreEqual(1,Count(Input));Assert.AreEqual(0,Count(Output));
        }
        [TestCase("drop")] [TestCase("equip")] [TestCase("disable")] [TestCase("replace")]
        [TestCase("source-count")] [TestCase("tool-id")]
        public void QuoteNeverSwitchesToolsOrReplansAChangedParticipant(string mutation)
        {
            var first=Knife("a");Knife("b");var source=Carry(Input);string command=Cmd(source);
            if(mutation=="drop"){Pack.RemoveObject(first);Zone.AddEntity(first,10,10);}
            if(mutation=="equip")Assert.True(InventorySystem.Equip(Actor,first));
            if(mutation=="disable")CapabilityType.GetField("FlexibleStock").SetValue(Capability(first),false);
            if(mutation=="replace"){Pack.RemoveObject(first);var next=Knife("a");Assert.AreNotSame(first,next);}
            if(mutation=="source-count")source.GetPart<StackerPart>().StackCount=2;
            if(mutation=="tool-id")first.ID="changed";
            Assert.False(Act(source,command));Assert.AreEqual(mutation=="source-count"?2:1,Count(Input));Assert.AreEqual(0,Count(Output));
        }
        public sealed class Reaction : Part
        {
            public Action Before;public bool Veto;public int Completed;
            public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMaterialProcess"){Before?.Invoke();return !Veto;}if(e.ID=="MaterialProcessCompleted")Completed++;return true;}
        }
        [TestCase(false)] [TestCase(true)] public void ToolVetoAndCommittedCompletionShareTheProcessEvents(bool veto)
        {
            var tool=Knife();var probe=new Reaction{Veto=veto};tool.AddPart(probe);var source=Carry(Input);string command=Cmd(source);
            Assert.AreEqual(!veto,Act(source,command));Assert.AreEqual(veto?0:1,probe.Completed);Assert.AreEqual(veto?1:0,Count(Input));
        }
        [Test] public void AToolCallbackCannotSpendTheSameInputOrAnotherProcessBeforeCommit()
        {
            var tool=Knife();var source=Carry(Input);source.GetPart<StackerPart>().StackCount=2;string command=Cmd(source);bool nested=true;
            var probe=new Reaction();tool.AddPart(probe);probe.Before=()=>{probe.Before=null;nested=Act(source,command);};
            // The tool's behavior existed at quoting in ordinary play; gather again.
            command=Cmd(source);Assert.True(Act(source,command));Assert.False(nested);Assert.AreEqual(1,Count(Input));Assert.AreEqual(1,Count(Output));Assert.AreEqual(1,probe.Completed);
        }
        [Test] public void OuterFailureRestoresGoodsAndNeverEmitsCompletion()
        {var tool=Knife();var probe=new Reaction();tool.AddPart(probe);var source=Carry(Input);string command=Cmd(source);FailAfter();Assert.False(Act(source,command));Assert.AreEqual(1,Count(Input));Assert.AreEqual(0,Count(Output));Assert.AreEqual(0,probe.Completed);Assert.True(Pack.Objects.Contains(tool));}
        [TestCase("valid")] [TestCase("stale")] [TestCase("after")]
        public void NativeInputPaysExactlyOnceOnlyForCommittedCutting(string condition)
        {
            var inputScope=new InputTestFixture();inputScope.Setup();
            try
            {
            var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            using(var ui=new HotbarSaveFixture(true,false))
            {
                var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);var tool=Knife();var source=Carry(Input);
                var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("leather-input","world-menu",manager,turns,Actor));ForgePart.Factory=Factory;
                var menu=ui.Root.AddComponent<InventoryUI>();menu.PlayerEntity=Actor;menu.CurrentZone=Zone;menu.EntityFactory=Factory;ui.Input.InventoryUI=menu;
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var state=typeof(InputHandler).GetField("_inputState",hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"InventoryOpen"));menu.Open();
                var display=InventoryScreenData.Build(Actor).Categories.SelectMany(c=>c.Items).Single(i=>i.Item==source);
                typeof(InventoryUI).GetMethod("OpenItemActionPopup",hidden).Invoke(menu,new object[]{display});
                var popup=typeof(InventoryUI).GetField("_itemActionPopup",hidden).GetValue(menu);var popupType=popup.GetType();
                var actions=(System.Collections.IList)popupType.GetField("Actions").GetValue(popup);
                int selected=Enumerable.Range(0,actions.Count).Single(i=>((string)actions[i].GetType().GetField("Command").GetValue(actions[i])).StartsWith(Prefix));
                popupType.GetField("CursorIndex").SetValue(popup,selected);
                if(condition=="stale")Pack.RemoveObject(tool);if(condition=="after")FailAfter();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputHelper.GetKey(KeyCode.None);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();InputHelper.GetKey(KeyCode.None);
                typeof(InputHandler).GetMethod("HandleInventoryInput",hidden).Invoke(ui.Input,null);
                Assert.AreEqual(condition=="valid"?27:17,turns.TickCount);Assert.AreEqual(condition=="valid"?1:0,Count(Output));
            }
            }finally{inputScope.TearDown();}
        }
        [Test] public void SavedToolsAndRemainingFeedKeepTheirCapabilitiesWithoutRestoringSpentUnits()
        {
            var tool=Knife();var source=Carry(Input);source.GetPart<StackerPart>().StackCount=2;Assert.True(Act(source,Cmd(source)));
            var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);Zone.RemoveEntity(Actor);Actor=copy;Assert.True(Zone.AddEntity(Actor,10,10));
            Assert.AreEqual(1,Count(Input));Assert.AreEqual(1,Count(Output));Assert.True(Act(Pack.Objects.Single(e=>e.BlueprintName==Input),Cmd(Pack.Objects.Single(e=>e.BlueprintName==Input))));Assert.AreEqual(0,Count(Input));Assert.AreEqual(2,Count(Output));
        }
        [Test] public void CanonicalOutputEconomicsRemainUnchangedAndTheBaselineCannotBuyProcessResellForProfit()
        {
            var source=Factory.CreateEntity(Input);var output=Factory.CreateEntity(Output);Assert.AreEqual(8,output.GetPart<CommercePart>().Value);Assert.AreEqual(3,output.GetPart<PhysicsPart>().Weight);
            Assert.AreEqual(4,source.GetPart<CommercePart>().Value);Assert.AreEqual(4,source.GetPart<PhysicsPart>().Weight);
            Assert.AreEqual(12,TradeSystem.GetBuyPrice(source,.35,null,1));Assert.AreEqual(1,TradeSystem.GetSellPrice(source,.35,null,1));Assert.AreEqual(2,TradeSystem.GetSellPrice(output,.35,null,1));
        }
        [TestCase("physics")] [TestCase("stack")] [TestCase("foreign-stack")] [TestCase("maximum")] [TestCase("condition")]
        public void AmbiguousOrForeignParticipatingToolPartsFailClosed(string defect)
        {
            var tool=Knife();var source=Carry(Input);
            if(defect=="physics")tool.AddPart(new PhysicsPart{Takeable=true});
            if(defect=="stack")tool.AddPart(new StackerPart{StackCount=2});
            if(defect=="foreign-stack")tool.GetPart<StackerPart>().ParentEntity=source;
            if(defect=="maximum")tool.GetPart<StackerPart>().MaxStack=0;
            if(defect=="condition"){tool.AddPart(new DestructiblePart{HP=1});tool.AddPart(new DestructiblePart{Gone=true});}
            Assert.False(HasChoice(source));Assert.AreEqual(1,Count(Input));
        }
        Entity Reload(Entity source)
        {
            using(var stream=new MemoryStream())
            {var writer=new SaveWriter(stream);writer.WriteEntityReference(source);writer.WriteQueuedEntityBodies();stream.Position=0;var reader=new SaveReader(stream,Factory);var copy=reader.ReadEntityReference();reader.ReadEntityBodies();return copy;}
        }
        [TestCase(false)] [TestCase(true)]
        public void LegacyDeclaredDaggerUpgradesOnceWithoutChangingIdentityDamageOrCustody(bool held)
        {
            var tool=Knife();tool.RemovePart(Capability(tool));tool.IntProperties.Remove("CuttingToolDefinitionSeen");tool.ApplyEffect(new BrokenEffect(),Actor,Zone);
            if(held)Assert.True(InventorySystem.Equip(Actor,tool));var copy=Reload(Actor);var pack=copy.GetPart<InventoryPart>();var saved=pack.Objects.Concat(pack.GetAllEquipped()).Single(e=>e.ID==tool.ID);
            Assert.AreEqual(1,saved.Parts.Count(p=>p.GetType()==CapabilityType));Assert.True(saved.HasEffect<BrokenEffect>());Assert.AreEqual(held,saved.GetPart<PhysicsPart>().Equipped==copy);
            var again=Reload(copy);Assert.AreEqual(1,again.GetPart<InventoryPart>().Objects.Concat(again.GetPart<InventoryPart>().GetAllEquipped()).SelectMany(e=>e.Parts).Count(p=>p.GetType()==CapabilityType));
        }
        [TestCase("disabled")] [TestCase("removed")] [TestCase("not-declared")]
        public void ReloadNeverResurrectsAnExplicitlyDisabledOrUnrelatedCapability(string state)
        {
            var tool=state=="not-declared"?Carry("Mace"):Knife();
            if(state=="disabled")CapabilityType.GetField("FlexibleStock").SetValue(Capability(tool),false);
            if(state=="removed")tool.RemovePart(Capability(tool));
            var copy=Reload(tool);var cap=copy.Parts.FirstOrDefault(p=>p.GetType()==CapabilityType);
            if(state=="disabled"){Assert.NotNull(cap);Assert.False((bool)CapabilityType.GetField("FlexibleStock").GetValue(cap));}else Assert.Null(cap);
        }
        [Test] public void StackingNeverLaundersDisabledCuttingOrBrokenToolState()
        {
            var first=Factory.CreateEntity("Dagger");var second=Factory.CreateEntity("Dagger");Capability(first);Capability(second);
            CapabilityType.GetField("FlexibleStock").SetValue(Capability(second),false);Assert.False(first.GetPart<StackerPart>().CanStackWith(second));Assert.AreEqual(0,first.GetPart<StackerPart>().MergeFrom(second));
            CapabilityType.GetField("FlexibleStock").SetValue(Capability(second),true);second.ApplyEffect(new BrokenEffect(),Actor,Zone);Assert.False(first.GetPart<StackerPart>().CanStackWith(second));
        }
        [TestCase("clone")] [TestCase("split")] [TestCase("remove")] [TestCase("command")]
        public void BrokenCuttingStackCannotBecomeAnUndamagedWorkingSingleton(string route)
        {
            var tool=Knife();tool.GetPart<StackerPart>().StackCount=2;tool.ApplyEffect(new BrokenEffect(),Actor,Zone);
            if(route=="clone")Assert.Throws<InvalidOperationException>(()=>tool.CloneForStack());
            if(route=="split")Assert.Null(tool.GetPart<StackerPart>().SplitStack(1));
            if(route=="remove")Assert.Null(tool.GetPart<StackerPart>().RemoveOne());
            if(route=="command")Assert.False(InventorySystem.ExecuteCommand(new CavesOfOoo.Core.Inventory.Commands.SeparateOneCommand(tool),Actor).Success);
            Assert.AreEqual(2,tool.GetPart<StackerPart>().StackCount);Assert.True(tool.HasEffect<BrokenEffect>());Assert.False(HasChoice(Carry(Input)));
        }
        [TestCase(true)] [TestCase(false)]
        public void PlainCuttingStacksSeparateWithoutLosingEnabledOrDisabledCapability(bool enabled)
        {
            var tool=Knife();tool.GetPart<StackerPart>().StackCount=2;CapabilityType.GetField("FlexibleStock").SetValue(Capability(tool),enabled);
            var command=new CavesOfOoo.Core.Inventory.Commands.SeparateOneCommand(tool);Assert.True(InventorySystem.ExecuteCommand(command,Actor).Success);
            Assert.AreEqual(1,tool.GetPart<StackerPart>().StackCount);Assert.AreEqual(enabled,(bool)CapabilityType.GetField("FlexibleStock").GetValue(Capability(command.SeparatedItem)));
            Assert.AreEqual(enabled,HasChoice(Carry(Input)));
        }
        [Test] public void AToolWithZeroHitpointsCannotPerformWorkEvenWithoutDestructiblePart()
        {var tool=Knife();Assert.False(tool.HasPart<DestructiblePart>());tool.GetStat("Hitpoints").BaseValue=0;Assert.False(HasChoice(Carry(Input)));}
    }
}
