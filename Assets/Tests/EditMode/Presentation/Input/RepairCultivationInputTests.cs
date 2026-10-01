using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native world-menu dispatch owns payment and time. The isolated
    /// cached zone excludes unrelated AI; live acceptance exercises actual travel.</summary>
    public sealed class RepairCultivationInputTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        sealed class ActionProbe:Part
        {
            public string Command;
            public int Before,After;
            public bool RefuseBefore,FailAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.GetStringParameter("Command")!=Command)return true;
                if(e.ID=="BeforeInventoryAction") { Before++;if(RefuseBefore)return false; }
                if(e.ID=="AfterInventoryAction") { After++;if(FailAfter)throw new InvalidOperationException("Repair/cultivation input rollback witness"); }
                return true;
            }
        }
        static void State(InputHandler input,string name,string value)
        {var field=typeof(InputHandler).GetField(name,Private);field.SetValue(input,Enum.Parse(field.FieldType,value));}
        static InventoryAction Open(InputHandler input,WorldActionMenuUI menu,Entity owner,Zone zone,string command)
        {
            State(input,"_worldActionMenuReturnState","Normal");
            typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",Private).Invoke(input,new object[]{owner,zone.GetEntityCell(owner),false});
            Assert.True(menu.IsOpen);Assert.AreSame(owner,menu.SelectedTarget);
            return ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(menu)).Single(a=>a.Command==command);
        }
        static void Select(InputHandler input,WorldActionMenuUI menu,InventoryAction action)
        {typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",Private).Invoke(input,new object[]{action,menu.SelectedTarget,menu.SelectedCell,menu.SelectedCellIsPile});}

        [TestCase("repair","valid")][TestCase("repair","refusal")]
        [TestCase("repair","before-callback")][TestCase("repair","after-callback")]
        [TestCase("crop","valid")][TestCase("crop","refusal")]
        [TestCase("crop","before-callback")][TestCase("crop","after-callback")]
        public void ActualWorldMenuCommitsOneActionOrRollsBackWithoutTime(string feature,string condition)
        {
            using(var ui=new HotbarSaveFixture(true,false))
            using(var content=new HaulingContentScope())
            {
                content.Seed(64);RepairRecipeRegistry.ResetForTests();RepairRecipeRegistry.LoadDefaults();
                try
                {
                    var manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);
                    var zone=new Zone("Overworld.11.10.0");manager.SetActiveZone(zone);
                    var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,4,4));
                    var terrain=content.Factory.CreateEntity("Grass");terrain.SetTag("Plantable");terrain.AddPart(new CultivatedSoilPart());Assert.True(zone.AddEntity(terrain,5,4));
                    bool isRepair=feature=="repair";
                    var target=content.Factory.CreateEntity(isRepair?"RepairLinedWell":"KnotflaxCrop");Assert.True(zone.AddEntity(target,5,4));
                    var crop=target.GetPart<CropPart>();if(crop!=null)crop.GrowthStage=2;
                    Entity clay=null;
                    if(isRepair && condition!="refusal")
                    {
                        clay=content.Factory.CreateEntity("FireClay");var stack=clay.GetPart<StackerPart>();
                        if(stack==null){stack=new StackerPart();clay.AddPart(stack);}stack.StackCount=3;
                        Assert.True(player.GetPart<InventoryPart>().AddObject(clay));
                    }
                    string command=isRepair?RepairablePart.RepairCommand:"HarvestCultivatedCrop";
                    var probe=new ActionProbe{Command=command,RefuseBefore=condition=="before-callback",FailAfter=condition=="after-callback"};player.AddPart(probe);
                    var turns=new TurnManager();turns.RestoreSavedState(17,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=TurnManager.ActionThreshold}});
                    ui.BindOld(GameSessionState.Capture("repair-crop-input","actual-menu",manager,turns,player));
                    CropSystem.Factory=content.Factory;SettlementRuntime.ActiveZone=zone;
                    var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                    int tick=turns.TickCount,energy=turns.GetEnergy(player);
                    var beforeOwners=zone.GetReadOnlyEntities().ToArray();
                    var selected=Open(ui.Input,menu,target,zone,command);
                    // A stale crop menu must recheck ripeness at execution.
                    if(!isRepair && condition=="refusal")crop.GrowthStage=1;
                    Select(ui.Input,menu,selected);
                    Assert.AreEqual(1,probe.Before,"A real selected world action must enter the shared transaction.");
                    bool success=condition=="valid";
                    Assert.AreEqual(success || condition=="after-callback"?1:0,probe.After);
                    if(isRepair)
                    {
                        Assert.AreEqual(success,target.GetPart<RepairablePart>().Repaired);
                        if(clay!=null){Assert.Contains(clay,player.GetPart<InventoryPart>().Objects);Assert.AreSame(player,clay.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(success?1:3,clay.GetPart<StackerPart>().StackCount);}
                        CollectionAssert.AreEquivalent(beforeOwners,zone.GetReadOnlyEntities());
                    }
                    else
                    {
                        Assert.AreEqual(!success,zone.GetReadOnlyEntities().Contains(target));
                        var cord=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==crop.YieldBlueprint).ToArray();
                        var seed=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==crop.SeedYieldBlueprint).ToArray();
                        Assert.AreEqual(success?crop.YieldCount:0,cord.Length);Assert.AreEqual(success?crop.SeedYieldCount:0,seed.Length);
                        foreach(var output in cord.Concat(seed)){Assert.AreEqual((5,4),zone.GetEntityPosition(output));Assert.IsNull(output.GetPart<PhysicsPart>().InInventory);}
                        if(!success){CollectionAssert.AreEquivalent(beforeOwners,zone.GetReadOnlyEntities());Assert.AreEqual(condition=="refusal"?1:2,crop.GrowthStage);}
                    }
                    Assert.AreSame(terrain,zone.GetCell(5,4).Objects.Single(e=>e==terrain),"The prepared bed survives harvest, repair and rollback.");
                    Assert.AreEqual("Normal",typeof(InputHandler).GetField("_inputState",Private).GetValue(ui.Input).ToString());
                    if(success)
                    {
                        Assert.Greater(turns.TickCount,tick);
                        Assert.AreEqual(energy-TurnManager.ActionThreshold+(turns.TickCount-tick)*player.GetStatValue("Speed",TurnManager.DefaultSpeed),turns.GetEnergy(player),"Exactly one normal action is paid, allowing ordinary scheduler refill.");
                    }
                    else {Assert.AreEqual(tick,turns.TickCount,"Refusal/rollback is free.");Assert.AreEqual(energy,turns.GetEnergy(player));}
                }
                finally{RepairRecipeRegistry.ResetForTests();}
            }
        }
    }
}
