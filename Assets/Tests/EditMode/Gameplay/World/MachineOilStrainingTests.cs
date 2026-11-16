using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineOilStrainingTests : FiftyWorldFixture
    {
        const string Blueprint="PeatOilStrainer", Prefix="StrainFrogOil|";
        Entity Bench()
        {
            // Before production content exists this is an inert real native owner.
            var e=Factory.CreateEntity(Factory.Blueprints.ContainsKey(Blueprint)?Blueprint:"PhysicalObject");
            e.BlueprintName=Blueprint; e.GetPart<PhysicsPart>().Solid=true;e.GetPart<PhysicsPart>().Takeable=false;
            Assert.True(Zone.AddEntity(e,11,10));Zone.GetCell(11,10).IsVisible=true;Zone.GetCell(11,10).Explored=true;return e;
        }
        string Cmd(Entity a,Entity b)=>Prefix+Id(a)+"|"+Id(b);
        [Test] public void ActualMenuQuotesTwoExactOwnersAndLeavesThirdForFieldUse()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var spare=Carry("FrogOil");int weight=Pack.GetCarriedWeight();
            var command=Choice(bench,Prefix);Assert.AreEqual(Cmd(a,b),command);
            Assert.True(Act(bench,command));Assert.AreEqual(1,Count("FrogOil"));Assert.True(Pack.Objects.Contains(spare));
            Assert.AreEqual(1,Count("LampOil"));Assert.AreEqual(weight-1,Pack.GetCarriedWeight());
            Assert.False(Act(bench,command));Assert.AreEqual(1,Count("LampOil"));
        }
        [Test] public void ProductUsesExistingPortableTorchConsumerAwayFromBench()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var torch=Carry("Torch");
            torch.GetPart<LightSourcePart>().Enabled=false;torch.GetPart<FuelPart>().FuelMass=11;
            Assert.True(Act(bench,Cmd(a,b)));var oil=Pack.Objects.Single(e=>e.BlueprintName=="LampOil");
            Zone.RemoveEntity(Actor);Assert.True(Zone.AddEntity(Actor,40,20));
            Assert.True(Act(oil,"RefuelTorch|"+Id(torch)));Assert.AreEqual(36,torch.GetPart<FuelPart>().FuelMass);Assert.AreEqual(0,Count("LampOil"));
        }
        [Test] public void DecreasingWeightExchangeWorksWhileAlreadyOverweight()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");Pack.MaxWeight=0;
            Assert.True(Act(bench,Cmd(a,b)));Assert.AreEqual(1,Count("LampOil"));
        }
        [TestCase("missing")] [TestCase("same")] [TestCase("alias")] [TestCase("ground")] [TestCase("equipped")] [TestCase("stack")] [TestCase("weight")] [TestCase("extra")]
        public void InvalidInputNeverSpendsEitherOwner(string defect)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");string cmd=Cmd(a,b);
            if(defect=="missing")cmd=Prefix+Id(a)+"|absent";
            if(defect=="same")cmd=Cmd(a,a);
            if(defect=="alias")b.ID=a.ID;
            if(defect=="ground"){Pack.RemoveObject(b);Zone.AddEntity(b,10,10);}
            if(defect=="equipped")b.GetPart<PhysicsPart>().Equipped=Actor;
            if(defect=="stack")b.AddPart(new StackerPart{StackCount=2,MaxStack=5});
            if(defect=="weight")b.GetPart<PhysicsPart>().Weight=0;
            if(defect=="extra")b.AddPart(new ContainerPart());
            Assert.False(Act(bench,cmd));Assert.AreEqual(0,Count("LampOil"));Assert.True(Pack.Objects.Contains(a));
        }
        [TestCase("far")] [TestCase("hidden")] [TestCase("removed")] [TestCase("extra")] [TestCase("duplicate")]
        public void InvalidStationCannotPerformWork(string defect)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");
            if(defect=="far"){Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,40,20);}
            if(defect=="hidden")Zone.GetCell(11,10).IsVisible=false;
            if(defect=="removed")Zone.RemoveEntity(bench);
            if(defect=="extra")bench.AddPart(new ContainerPart());
            if(defect=="duplicate")bench.AddPart(new PhysicsPart());
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));
        }
        [Test] public void OuterCallbackFailureRestoresBothOriginalOwners()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");FailAfter();
            Assert.False(Act(bench,Cmd(a,b)));Assert.True(Pack.Objects.Contains(a));Assert.True(Pack.Objects.Contains(b));Assert.AreEqual(0,Count("LampOil"));
        }
        [TestCase(1)] [TestCase(64)] [TestCase(704860946)]
        public void FreshWorksPlacesOnlyOneBenchAndPreservesServices(int seed)
        {
            RepairRecipeRegistry.ResetForTests();var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(seed);
            Assert.True(builder.BuildZone(zone,Factory,new Random(seed)));Assert.True(builder.ValidateFinal(zone));
            var benches=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==Blueprint).ToArray();Assert.AreEqual(1,benches.Length);
            Assert.AreEqual((59,5),zone.GetEntityPosition(benches[0]));Assert.True(zone.GenReservedCells.Contains((59,5)));
            Assert.False(zone.GetCell(59,6).BlocksMovement());Assert.False(zone.GetCell(54,10).BlocksMovement());
            Assert.AreEqual(1,zone.GetCell(56,5).Objects.Count(e=>e.BlueprintName=="AbandonedFieldWorkRig"));
            Assert.AreEqual(8,zone.GetCell(61,6).Objects.Single(e=>e.BlueprintName=="SoddenWorksLocker").GetPart<ContainerPart>().Contents.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
        }
        [Test] public void MissingOptionalContentPreservesWholeBaseDistrictAndCache()
        {
            Factory.Blueprints.Remove(Blueprint);RepairRecipeRegistry.ResetForTests();var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(64);
            Assert.True(builder.BuildZone(zone,Factory,new Random(64)));Assert.True(builder.ValidateFinal(zone));int count=zone.EntityCount;
            Assert.False(new SoddenDistrictBuilder(64).BuildZone(zone,Factory,new Random(64)));Assert.AreEqual(count,zone.EntityCount);
            Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName==Blueprint));
        }

        public sealed class AfterProbe : Part
        {
            public Action Callback;
            public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}
        }
        public sealed class CreateProbe : Part
        {
            public static Action<Entity> Callback;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="ObjectCreated"){var owner=ParentEntity;owner.RemovePart(this);Callback?.Invoke(owner);}return true;
            }
        }
        [TearDown] public void ResetProbe(){CreateProbe.Callback=null;RepairRecipeRegistry.ResetForTests();}
        [TestCase("actor-move")] [TestCase("bench-move")] [TestCase("bench-id")] [TestCase("output-ground")] [TestCase("input-ground")]
        [TestCase("output-weight")] [TestCase("output-part")] [TestCase("output-count")] [TestCase("actor-death")]
        public void OuterMutationRefusesWithoutCreatingExtraOil(string mutation)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");
            Actor.AddPart(new AfterProbe{Callback=()=>
            {
                var output=Pack.Objects.Single(o=>o.BlueprintName=="LampOil");
                if(mutation=="actor-move"){Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,40,20);}
                if(mutation=="bench-move"){Zone.RemoveEntity(bench);Zone.AddEntity(bench,12,10);}
                if(mutation=="bench-id")bench.ID="changed-bench";
                if(mutation=="output-ground"){Pack.RemoveObject(output);Zone.AddEntity(output,10,10);}
                if(mutation=="input-ground")Zone.AddEntity(a,10,10);
                if(mutation=="output-weight")output.GetPart<PhysicsPart>().Weight=2;
                if(mutation=="output-part")output.AddPart(new ContainerPart());
                if(mutation=="output-count")output.GetPart<StackerPart>().StackCount=2;
                if(mutation=="actor-death")Actor.GetStat("Hitpoints").BaseValue=0;
            }});
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(0,Count("LampOil"));Assert.AreEqual(2,Count("FrogOil"));
            Assert.Null(Zone.GetEntityCell(a));Assert.Null(Zone.GetEntityCell(b));Assert.AreSame(Actor,a.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false)] [TestCase(true)]
        public void MergeUsesCanonicalRecipientAndRollsBackOnlyThisUnit(bool fail)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var old=Carry("LampOil");old.GetPart<StackerPart>().StackCount=4;
            if(fail)FailAfter();Assert.AreEqual(!fail,Act(bench,Cmd(a,b)));Assert.AreEqual(fail?4:5,old.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(fail?2:0,Count("FrogOil"));Assert.AreSame(old,Pack.Objects.Single(o=>o.BlueprintName=="LampOil"));
        }
        [Test] public void ReentrantBatchCannotSpendOtherSuppliesDuringAnUnfinishedAction()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var c=Carry("FrogOil");var d=Carry("FrogOil");bool nested=true;
            var probe=new AfterProbe();probe.Callback=()=>{probe.Callback=null;nested=Act(bench,Cmd(c,d));};Actor.AddPart(probe);
            Assert.True(Act(bench,Cmd(a,b)));Assert.False(nested);Assert.AreEqual(1,Count("LampOil"));Assert.AreEqual(2,Count("FrogOil"));
        }
        [TestCase("valid")] [TestCase("stale")] [TestCase("after")]
        public void RealWorldMenuPaysExactlyOneActionOnlyOnCommit(string condition)
        {
            using(var ui=new HotbarSaveFixture(true,false))
            {
                var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);
                var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");
                var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("oil-input","normal-world-menu",manager,turns,Actor));SeedPart.Factory=Factory;
                var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var state=typeof(InputHandler).GetField("_worldActionMenuReturnState",hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",hidden).Invoke(ui.Input,new object[]{bench,Zone.GetEntityCell(bench),false});
                var action=((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",hidden).GetValue(menu)).Single(row=>row.Command.StartsWith(Prefix));
                if(condition=="stale")Pack.RemoveObject(b);if(condition=="after")FailAfter();
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",hidden).Invoke(ui.Input,new object[]{action,bench,Zone.GetEntityCell(bench),false});
                Assert.AreEqual(condition=="valid"?27:17,turns.TickCount);Assert.AreEqual(condition=="valid"?1:0,Count("LampOil"));
            }
        }
        [Test] public void SavedBenchKeepsItsNativeFunctionWithoutAJobOrHiddenStock()
        {
            var bench=Bench();string id=bench.ID;var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(bench);
            Zone.RemoveEntity(bench);Assert.True(Zone.AddEntity(copy,11,10));Assert.AreEqual(id,copy.ID);
            var a=Carry("FrogOil");var b=Carry("FrogOil");Assert.True(Act(copy,Cmd(a,b)));Assert.AreEqual(1,Count("LampOil"));
            var savedPlayer=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(1,savedPlayer.GetPart<InventoryPart>().Objects.Where(o=>o.BlueprintName=="LampOil").Sum(o=>o.GetPart<StackerPart>().StackCount));
            Assert.False(savedPlayer.GetPart<InventoryPart>().Objects.Any(o=>o.BlueprintName=="FrogOil"));
        }
        [Test] public void ReaderDeclaresRecipeLossAndExistingConsumerWithoutGrantingStock()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var before=Pack.Objects.ToArray();
            string text=bench.GetPart<ExaminablePart>().BuildWorldExamineLine(Zone,Zone.GetEntityCell(bench),Actor);
            StringAssert.Contains("discarded",text);StringAssert.Contains("25 fuel",text);StringAssert.Contains("one action",text);
            CollectionAssert.AreEqual(before,Pack.Objects);Assert.AreEqual(0,Count("LampOil"));
        }
        [Test] public void CallbackCannotMoveTheFunctionToAnotherBench()
        {
            var bench=Bench();var other=Factory.CreateEntity(Blueprint);Assert.True(Zone.AddEntity(other,10,11));
            Zone.GetCell(10,11).IsVisible=true;Zone.GetCell(10,11).Explored=true;
            var a=Carry("FrogOil");var b=Carry("FrogOil");var part=bench.GetPart<OilStrainingPart>();
            Actor.AddPart(new AfterProbe{Callback=()=>{other.RemovePart(other.GetPart<OilStrainingPart>());other.AddPart(part);}});
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));
        }

        [TestCase(false)] [TestCase(true)]
        public void CallbackCannotStashRefundedMaterialInAContainer(bool input)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var box=Place("Chest",12,10);var contents=box.GetPart<ContainerPart>();
            Actor.AddPart(new AfterProbe{Callback=()=>{var item=input?a:Pack.Objects.Single(o=>o.BlueprintName=="LampOil");Pack.RemoveObject(item);Assert.True(contents.AddItem(item));}});
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));Assert.False(contents.Contents.Any(o=>o==a||o.BlueprintName=="LampOil"));
        }
        [Test] public void CallbackCannotKeepProductAsForcedEquipment()
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");Entity output=null;
            Actor.AddPart(new AfterProbe{Callback=()=>{output=Pack.Objects.Single(o=>o.BlueprintName=="LampOil");Pack.RemoveObject(output);Pack.EquippedItems["test"]=output;output.GetPart<PhysicsPart>().Equipped=Actor;}});
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));Assert.False(Pack.EquippedItems.ContainsValue(output));
        }
        [Test] public void HiddenInputsAreNotSelectedAndHiddenOutputCannotReceive()
        {
            var bench=Bench();var hidden=Carry("FrogOil");hidden.GetPart<RenderPart>().Visible=false;var a=Carry("FrogOil");var b=Carry("FrogOil");
            Assert.AreEqual(Cmd(a,b),Choice(bench,Prefix));Assert.False(Act(bench,Cmd(hidden,a)));
            var output=Carry("LampOil");output.GetPart<RenderPart>().Visible=false;
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(3,Count("FrogOil"));Assert.AreEqual(1,Count("LampOil"));
        }
        [Test] public void MalformedOptionalTemplateOmitsOnlyTheNewStation()
        {
            Factory.Blueprints[Blueprint].Parts["Physics"]["Weight"]="1";RepairRecipeRegistry.ResetForTests();
            var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(64);Assert.True(builder.BuildZone(zone,Factory,new Random(64)));
            Assert.True(builder.ValidateFinal(zone));Assert.False(zone.GetReadOnlyEntities().Any(o=>o.BlueprintName==Blueprint));Assert.True(zone.GetReadOnlyEntities().Any(o=>o.BlueprintName=="AbandonedFieldWorkRig"));
        }
        [Test] public void RealActionF1DeclaresLossAndConsumerWithoutProcessing()
        {
            using(var ui=new HotbarSaveFixture(true,false))
            {
                var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);var bench=Bench();Carry("FrogOil");Carry("FrogOil");
                var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("oil-preview","reader",manager,turns,Actor));SeedPart.Factory=Factory;
                var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                var reader=ui.Root.AddComponent<AnnouncementUI>();ui.Input.AnnouncementUI=reader;
                var grid=new UnityEngine.GameObject("Oil reader grid");grid.transform.SetParent(ui.Root.transform);grid.AddComponent<UnityEngine.Grid>();
                UnityEngine.Tilemaps.Tilemap Tiles(string n){var go=new UnityEngine.GameObject(n);go.transform.SetParent(grid.transform);return go.AddComponent<UnityEngine.Tilemaps.Tilemap>();}
                reader.Tilemap=Tiles("Text");reader.BgTilemap=Tiles("Background");menu.Tilemap=Tiles("Menu text");menu.BgTilemap=Tiles("Menu background");
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;
                var state=typeof(InputHandler).GetField("_worldActionMenuReturnState",hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",hidden).Invoke(ui.Input,new object[]{bench,Zone.GetEntityCell(bench),false});
                var rows=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",hidden).GetValue(menu);
                typeof(WorldActionMenuUI).GetField("_cursorIndex",hidden).SetValue(menu,rows.FindIndex(a=>a.Command.StartsWith(Prefix)));
                typeof(InputHandler).GetMethod("OpenWorldActionDetailsReader",hidden).Invoke(ui.Input,null);
                Assert.True(reader.IsOpen);var lines=new List<string>();for(int i=0;i<reader.PageCount;i++){reader.GoToPage(i);lines.AddRange(reader.VisibleLines);}
                string text=string.Join(" ",lines);StringAssert.Contains("discarded",text);StringAssert.Contains("25 fuel",text);StringAssert.Contains("one action",text);
                Assert.AreEqual(17,turns.TickCount);Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));
            }
        }
        [Test] public void NativeAppearanceUsesExistingBenchAndRefusesFalseServicePayload()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SoddenDistrictPlan.WorksZoneID))
            {
                f.Set("FullReveal",true);var bench=f.Add(Blueprint);var recipe=SpawnRing3DRecipes.Resolve(f.Zone,bench,f.Library.Definition);
                Assert.AreEqual(SoddenDistrictArtLibrary.WorkingBench,recipe.ModelId);Assert.IsNull(recipe.Failure);f.Refresh(f.Dirty(bench));Assert.True(f.Rendered(bench));
                bench.AddPart(new ContainerPart());Assert.Null(SoddenDistrictRecipes.ResolveModel(f.Zone,bench));
            }
        }
        [TestCase("ground")] [TestCase("container")] [TestCase("merge")]
        public void UnrecognizedCreationBehaviorIsRejectedBeforeCallback(string destination)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");var old=Carry("LampOil");old.GetPart<StackerPart>().StackCount=4;
            var box=Place("Chest",12,10);bool invoked=false;
            Factory.RegisterPartType<CreateProbe>("OilCreation");Factory.Blueprints["LampOil"].Parts["OilCreation"]=new Dictionary<string,string>();
            CreateProbe.Callback=output=>{invoked=true;if(destination=="container")box.GetPart<ContainerPart>().AddItem(output);else if(destination=="merge")Pack.AddObject(output);else Zone.AddEntity(output,12,11);};
            Assert.False(Act(bench,Cmd(a,b)));Assert.False(invoked);Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(4,Count("LampOil"));
            Assert.False(box.GetPart<ContainerPart>().Contents.Any(o=>o.BlueprintName=="LampOil"));
        }
        [TestCase("Physics","Weight","0")] [TestCase("Physics","Weight","NaN")] [TestCase("Stacker","StackCount","2")] [TestCase("Reagent","PropertiesRaw","heat:99")]
        public void NoncanonicalOutputTemplateCannotChangeTheRecipe(string part,string field,string value)
        {
            var bench=Bench();var a=Carry("FrogOil");var b=Carry("FrogOil");Factory.Blueprints["LampOil"].Parts[part][field]=value;
            Assert.False(Act(bench,Cmd(a,b)));Assert.AreEqual(2,Count("FrogOil"));Assert.AreEqual(0,Count("LampOil"));
        }
        [Test] public void ExistingRigArtDebtReproducesWithoutTheNewStrainerContent()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SoddenDistrictPlan.WorksZoneID))
            {
                f.Factory.Blueprints.Remove(Blueprint);var rig=f.Add("AbandonedFieldWorkRig");
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,rig,f.Library.Definition);
                Assert.AreEqual("unmodeled-native-blueprint",recipe.Failure);
            }
        }
    }
}
