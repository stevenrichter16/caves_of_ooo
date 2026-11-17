using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    public sealed class MachineCompositeProcessTests : FiftyWorldFixture
    {
        const string Cord="KnotflaxCord", Binding="LeatherBindingComponent", Output="GuardLashingComponent", Prefix="MaterialProcess|guard_lashing|";
        const string Data="{\"Processes\":[{\"Id\":\"guard_lashing\",\"SourceBlueprint\":\"KnotflaxCord\",\"SourceRecipeId\":\"braid_binding\",\"Mode\":\"Process\",\"InputCount\":1,\"WorkKind\":\"HandCutting\",\"AdditionalInputBlueprints\":[\"LeatherBindingComponent\"],\"AdditionalInputCounts\":[1],\"OutputBlueprints\":[\"GuardLashingComponent\"],\"OutputCounts\":[1],\"ActionText\":\"trim and tension guard lashing\",\"LossText\":\"trimming discarded\"}]}";
        EntityFactory oldForge;
        [SetUp] public void SetupComposite()
        {oldForge=ForgePart.Factory;ForgePart.Factory=Factory;Assert.AreEqual(0,MaterialProcessRegistry.InitializeFromJson(Data).Count);}
        [TearDown] public void RestoreComposite(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Knife()=>Carry("Dagger");
        string Cmd(Entity source)=>Choice(source,Prefix);
        bool Offered(Entity source)=>Actions(source).Any(a=>a.Command.StartsWith(Prefix));
        Entity Extra(string id="a") {var e=Carry(Binding);e.ID=id;return e;}
        Entity SeparateExtra(string id)
        {var e=Factory.CreateEntity(Binding);e.ID=id;Pack.Objects.Add(e);e.GetPart<PhysicsPart>().InInventory=Actor;return e;}
        [TestCase("[1]","[0]")] [TestCase("[1]","[64]")]
        [TestCase("\"AdditionalInputCounts\":[1]","\"AdditionalInputCounts\":[]")]
        [TestCase("\"AdditionalInputBlueprints\":[\"LeatherBindingComponent\"]","\"AdditionalInputBlueprints\":[\"KnotflaxCord\"]")]
        [TestCase("\"AdditionalInputBlueprints\":[\"LeatherBindingComponent\"]","\"AdditionalInputBlueprints\":[\"GuardLashingComponent\"]")]
        public void InvalidCompositeAllocationFailsTheWholeCatalogue(string from,string to)
        {Assert.Greater(MaterialProcessRegistry.InitializeFromJson(Data.Replace(from,to)).Count,0);Assert.Null(MaterialProcessRegistry.Get("guard_lashing"));}
        [TestCase("missing")] [TestCase("shape_haft")]
        public void InheritedPermissionMustNameARealRecipeForThisExactSource(string recipe)
        {Assert.AreEqual(0,MaterialProcessRegistry.InitializeFromJson(Data.Replace("braid_binding",recipe)).Count);Assert.Greater(MaterialProcessRegistry.Validate(Factory).Count,0);}
        [TestCase("plain-region-a")] [TestCase("plain-region-b")]
        public void ExistingRecipePermissionEnablesFiniteCompositeAwayFromStations(string zoneId)
        {
            Zone.RemoveEntity(Actor);Zone=new Zone(zoneId);SettlementRuntime.ActiveZone=Zone;Assert.True(Zone.AddEntity(Actor,10,10));
            var tool=Knife();var cord=Carry(Cord);Extra();Assert.False(cord.HasPart<MaterialProcessInputPart>());
            Assert.True(Act(cord,Cmd(cord)));Assert.AreEqual(0,Count(Cord));Assert.AreEqual(0,Count(Binding));Assert.AreEqual(1,Count(Output));Assert.True(Pack.Objects.Contains(tool));
        }
        [Test] public void MenuDisclosesBothInputsAndLossWithoutSpendingThem()
        {Knife();var cord=Carry(Cord);Extra();var action=Actions(cord).Single(a=>a.Command.StartsWith(Prefix));Assert.That(action.Display,Does.Contain("leather binding"));Assert.That(action.Display,Does.Contain("trimming"));Assert.AreEqual(1,Count(Cord));Assert.AreEqual(1,Count(Binding));}
        [Test] public void NonPlayerUsesTheSameExplicitCapabilityAndIngredientRules()
        {Actor.Tags.Remove("Player");Knife();var cord=Carry(Cord);Extra();Assert.True(Act(cord,Cmd(cord)));Assert.AreEqual(1,Count(Output));}
        [Test] public void AuthoredDefaultAndAdditionalInputListsRemainFiniteAndImmutable()
        {
            MaterialProcessRegistry.ResetForTests();var d=MaterialProcessRegistry.Get("guard_lashing");Assert.NotNull(d);Assert.AreEqual("braid_binding",d.SourceRecipeId);
            Assert.AreEqual(0,MaterialProcessRegistry.Validate(Factory).Count);Assert.AreEqual(Binding,d.AdditionalInputBlueprints.Single());Assert.AreEqual(1,d.AdditionalInputCounts.Single());
            Assert.Throws<NotSupportedException>(()=>((System.Collections.IList)d.AdditionalInputBlueprints)[0]="Dagger");
        }
        [TestCase(false)] [TestCase(true)] public void CatalogueRevocationNeverGrantsPermissionToLaunderKnownInputPayloads(bool additional)
        {
            var item=Carry(additional?Binding:Cord);item.GetPart<StackerPart>().StackCount=2;item.AddPart(new Reaction());
            Assert.Greater(MaterialProcessRegistry.InitializeFromJson("{} ").Count,0);Assert.Null(item.GetPart<StackerPart>().SplitStack(1));
        }
        [TestCase("absent")] [TestCase("foreign")] [TestCase("modified")]
        public void MissingOrUnsupportedAdditionalStockCannotBeSpent(string defect)
        {
            Knife();var cord=Carry(Cord);if(defect!="absent") {var extra=Extra();if(defect=="foreign"){Pack.RemoveObject(extra);Zone.AddEntity(extra,11,10);}else extra.GetPart<WeaponComponentPart>().HitBonus++;}
            Assert.False(Offered(cord));Assert.AreEqual(1,Count(Cord));Assert.AreEqual(0,Count(Output));
        }
        [TestCase("remove-braid")] [TestCase("duplicate-recipe")] [TestCase("explicit-first")] [TestCase("explicit-last")]
        public void InstancePermissionAndAmbiguousProviderRefusalsAreOrderIndependent(string defect)
        {
            Knife();var cord=Carry(Cord);Extra();var permission=cord.GetPart<PreparationRecipePart>();
            if(defect=="remove-braid")permission.RecipeIds="bandage";
            if(defect=="duplicate-recipe")cord.AddPart(new PreparationRecipePart{RecipeIds="braid_binding,bandage"});
            if(defect.StartsWith("explicit")){cord.AddPart(new MaterialProcessInputPart{ProcessIds="guard_lashing"});if(defect=="explicit-first"){cord.RemovePart(permission);cord.AddPart(permission);}}
            Assert.False(Offered(cord));Assert.True(Actions(cord).Any(a=>a.Command=="PrepareRecipe|bandage"));
        }
        [TestCase("count")] [TestCase("drop")] [TestCase("replace")] [TestCase("id")]
        [TestCase("permission-value")] [TestCase("permission-owner")]
        public void RetainedQuoteNeverReselectsOrRevivesChangedParticipants(string mutation)
        {
            Knife();var cord=Carry(Cord);var extra=Extra();SeparateExtra("b");string command=Cmd(cord);
            if(mutation=="count")extra.GetPart<StackerPart>().StackCount++;
            if(mutation=="drop"){Pack.RemoveObject(extra);Zone.AddEntity(extra,11,10);}
            if(mutation=="replace"){Pack.RemoveObject(extra);SeparateExtra("a");}
            if(mutation=="id")extra.ID="changed";
            if(mutation=="permission-value")cord.GetPart<PreparationRecipePart>().RecipeIds="bandage";
            if(mutation=="permission-owner"){var part=cord.GetPart<PreparationRecipePart>();cord.RemovePart(part);cord.AddPart(new PreparationRecipePart{RecipeIds="braid_binding,bandage"});}
            Assert.False(Act(cord,command));Assert.AreEqual(1,Count(Cord));Assert.AreEqual(0,Count(Output));
        }
        [TestCase(false)] [TestCase(true)]
        public void PartialInputsAndOutputMergeHaveExactOuterRollback(bool fail)
        {
            Knife();var cord=Carry(Cord);cord.GetPart<StackerPart>().StackCount=3;var extra=Extra();extra.GetPart<StackerPart>().StackCount=2;
            var output=Carry(Output);output.GetPart<StackerPart>().StackCount=4;string command=Cmd(cord);if(fail)FailAfter();Assert.AreEqual(!fail,Act(cord,command));
            Assert.AreEqual(fail?3:2,cord.GetPart<StackerPart>().StackCount);Assert.AreEqual(fail?2:1,extra.GetPart<StackerPart>().StackCount);Assert.AreEqual(fail?4:5,output.GetPart<StackerPart>().StackCount);
        }
        [Test] public void EquivalentAdditionalStacksSelectTheLowestIdentityOnce()
        {Knife();var cord=Carry(Cord);var first=Extra("b");var second=SeparateExtra("a");Assert.True(Act(cord,Cmd(cord)));Assert.True(Pack.Objects.Contains(first));Assert.False(Pack.Objects.Contains(second));}
        [Test] public void SavedLegacyCordAndNewQuotesWorkWithoutNewPartsOrOldTokens()
        {
            Knife();var cord=Carry(Cord);Extra();string old=Cmd(cord);var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(cord);Pack.RemoveObject(cord);Assert.True(Pack.AddObject(copy));
            Assert.False(copy.HasPart<MaterialProcessInputPart>());Assert.False(Act(copy,old));Assert.True(Act(copy,Cmd(copy)));Assert.AreEqual(1,Count(Output));
        }
        public sealed class Reaction : Part
        {
            public override string Name=>"CompositeReaction";
            public bool Veto;public int Completed;public string Mark="initial";public Action Before;
            public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMaterialProcess"){Before?.Invoke();return !Veto;}if(e.ID=="MaterialProcessCompleted")Completed++;return true;}
        }
        sealed class ReactionPolicy : IMaterialProcessInstancePartPolicy
        {
            public bool ValidTemplate(IReadOnlyDictionary<string,string> p)=>false;
            public bool MatchesTemplate(Part p,IReadOnlyDictionary<string,string> a)=>false;
            public bool AllowsInstance(Part p)=>p is Reaction;
            public object Capture(Part p)=>((Reaction)p).Mark;
            public bool Matches(Part p,object s)=>((Reaction)p).Mark==(string)s;
        }
        [TestCase(1,false)] [TestCase(1,true)] [TestCase(2,false)] [TestCase(2,true)]
        public void BeforeCallbackCannotConsumeAProtectedIngredientThroughTheOrdinaryPrimitive(int quantity,bool veto)
        {
            var policy=new ReactionPolicy();MaterialProcessPayloadPolicies.Register(typeof(Reaction),policy);
            try
            {
                Knife();var cord=Carry(Cord);var extra=Extra();extra.GetPart<StackerPart>().StackCount=quantity;bool spent=true;
                extra.AddPart(new Reaction{Veto=veto,Before=()=>spent=Pack.TryConsumeOne(extra)});
                bool result=Act(cord,Cmd(cord));Assert.False(spent,"A veto/reaction may not spend an ingredient reserved by the enclosing process.");
                Assert.AreEqual(!veto,result);Assert.AreEqual(veto?quantity:quantity-1,Count(Binding));Assert.AreEqual(veto?1:0,Count(Cord));Assert.AreEqual(veto?0:1,Count(Output));
            }finally{MaterialProcessPayloadPolicies.Unregister(typeof(Reaction),policy);}
        }
        [Test] public void MalformedCataloguePermissionCannotBreakExistingBraidOrBandageMenus()
        {
            Knife();var cord=Carry(Cord);Extra();Factory.Blueprints[Cord].Parts["PreparationRecipe"]["RecipeIds"]=null;
            Assert.DoesNotThrow(()=>Assert.Greater(MaterialProcessRegistry.Validate(Factory).Count,0));
            Assert.DoesNotThrow(()=>{var actions=Actions(cord);Assert.True(actions.Any(a=>a.Command=="PrepareRecipe|braid_binding"));Assert.True(actions.Any(a=>a.Command=="PrepareRecipe|bandage"));});
        }
        [TestCase(false)] [TestCase(true)] public void AdditionalInputCanOwnVetoAndCommittedReactions(bool veto)
        {
            var policy=new ReactionPolicy();MaterialProcessPayloadPolicies.Register(typeof(Reaction),policy);
            try{Knife();var cord=Carry(Cord);var extra=Extra();var reaction=new Reaction{Veto=veto};extra.AddPart(reaction);Assert.AreEqual(!veto,Act(cord,Cmd(cord)));Assert.AreEqual(veto?0:1,reaction.Completed);Assert.AreEqual(veto?1:0,Count(Binding));}
            finally{MaterialProcessPayloadPolicies.Unregister(typeof(Reaction),policy);}
        }
        [Test] public void MultipleAdmissibleBehavioralVariantsDoNotGetSilentlySelected()
        {
            var policy=new ReactionPolicy();MaterialProcessPayloadPolicies.Register(typeof(Reaction),policy);
            try{Knife();var cord=Carry(Cord);Extra().AddPart(new Reaction());SeparateExtra("b");Assert.False(Offered(cord));}
            finally{MaterialProcessPayloadPolicies.Unregister(typeof(Reaction),policy);}
        }
        [TestCase(false,"split")] [TestCase(true,"split")] [TestCase(false,"clone")] [TestCase(true,"clone")]
        [TestCase(false,"remove")] [TestCase(true,"remove")] [TestCase(false,"command")] [TestCase(true,"command")]
        public void ParticipatingPayloadsCannotLaunderPrivateStateThroughSplit(bool additional,string route)
        {
            var source=Carry(additional?Binding:Cord);source.GetPart<StackerPart>().StackCount=2;source.AddPart(new Reaction());
            if(route=="split")Assert.Null(source.GetPart<StackerPart>().SplitStack(1));
            if(route=="clone")Assert.Throws<InvalidOperationException>(()=>source.CloneForStack());
            if(route=="remove")Assert.Null(source.GetPart<StackerPart>().RemoveOne());
            if(route=="command")Assert.False(InventorySystem.ExecuteCommand(new CavesOfOoo.Core.Inventory.Commands.SeparateOneCommand(source),Actor).Success);
            Assert.AreEqual(2,source.GetPart<StackerPart>().StackCount);
        }
        [TestCase("valid")] [TestCase("stale-extra")] [TestCase("after")]
        public void NativeInventoryInputPaysOnlyOnceForACommittedComposite(string condition)
        {
            var scope=new InputTestFixture();scope.Setup();try
            {
                var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();using(var ui=new HotbarSaveFixture(true,false))
                {
                    var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);Knife();var cord=Carry(Cord);var extra=Extra();
                    var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                    ui.BindOld(GameSessionState.Capture("composite-input","world-menu",manager,turns,Actor));ForgePart.Factory=Factory;
                    var menu=ui.Root.AddComponent<InventoryUI>();menu.PlayerEntity=Actor;menu.CurrentZone=Zone;menu.EntityFactory=Factory;ui.Input.InventoryUI=menu;
                    const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;var inputState=typeof(InputHandler).GetField("_inputState",hidden);inputState.SetValue(ui.Input,Enum.Parse(inputState.FieldType,"InventoryOpen"));menu.Open();
                    var display=InventoryScreenData.Build(Actor).Categories.SelectMany(c=>c.Items).Single(i=>i.Item==cord);typeof(InventoryUI).GetMethod("OpenItemActionPopup",hidden).Invoke(menu,new object[]{display});
                    var popup=typeof(InventoryUI).GetField("_itemActionPopup",hidden).GetValue(menu);var type=popup.GetType();var actions=(System.Collections.IList)type.GetField("Actions").GetValue(popup);
                    int selected=Enumerable.Range(0,actions.Count).Single(i=>((string)actions[i].GetType().GetField("Command").GetValue(actions[i])).StartsWith(Prefix));type.GetField("CursorIndex").SetValue(popup,selected);
                    if(condition=="stale-extra")Pack.RemoveObject(extra);if(condition=="after")FailAfter();InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputHelper.GetKey(KeyCode.None);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();InputHelper.GetKey(KeyCode.None);typeof(InputHandler).GetMethod("HandleInventoryInput",hidden).Invoke(ui.Input,null);
                    Assert.AreEqual(condition=="valid"?27:17,turns.TickCount);Assert.AreEqual(condition=="valid"?1:0,Count(Output));
                }
            }finally{scope.TearDown();}
        }
    }
}
