using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FieldWorkToolsTests : FiftyWorldFixture
    {
        EntityFactory oldForge;
        [SetUp] public void PrepareTools()
        {
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void RestoreTools(){TileReactionSystem.ResetForTests();ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Hearth()
        {
            var h=Place("CampHearth",9,10);h.GetPart<FuelPart>().FuelMass=15;h.GetPart<ThermalPart>().Temperature=450;
            if(!h.HasPart<InstallationMarkerPart>())h.AddPart(new InstallationMarkerPart());h.GetPart<InstallationMarkerPart>().Origin=HeldFixtureActions.PlayerOrigin;if(!h.HasPart<InstallablePart>())h.AddPart(new InstallablePart());h.GetPart<InstallablePart>().State=InstallablePart.Installed;h.GetPart<PhysicsPart>().Takeable=false;return h;
        }
        string Row(Entity tool,string prefix,int x=11,int y=10)=>Choice(tool,prefix,a=>a.Command.Contains("|"+x+"|"+y+"|"));
        int Stored(Entity tool)=>Convert.ToInt32(tool.Parts.Single(p=>p.Name=="FieldWorkTool").GetType().GetField("Charge").GetValue(tool.Parts.Single(p=>p.Name=="FieldWorkTool")));
        [TestCase("HearthNozzle")] [TestCase("DischargeRod")] [TestCase("SteamCondenser")]
        public void ReusableToolsHaveExplicitUsesAndDoNotInventLightOrFuel(string blueprint)
        {
            var tool=Carry(blueprint);Assert.True(tool.HasPart("FieldWorkTool"));Assert.AreEqual(ItemCategory.Tools,ItemCategory.GetCategory(tool));
            Assert.False(tool.HasPart<FuelPart>());Assert.False(tool.HasPart<LightSourcePart>());
            Assert.False(tool.HasPart<StackerPart>());Assert.IsNotEmpty(UseGuides.SectionText(tool,Zone,null,Actor));
        }
        [Test] public void NozzleSpendsFiveActualFuelAndThawsThroughSharedTileRules()
        {
            var h=Hearth();var tool=Carry("HearthNozzle");Zone.TileState.WriteCoating(11,10,"ice",5);
            Assert.True(Act(tool,Row(tool,"DirectHearthHeat|")));Assert.AreEqual(10,h.GetPart<FuelPart>().FuelMass);
            Assert.False(Zone.TileState.HasCoating(11,10,"ice"));Assert.AreEqual(0,PoolUnits());
        }
        [TestCase("cold")] [TestCase("empty")] [TestCase("public")] [TestCase("nonplayer")]
        public void NozzleCannotUseUnpaidOrUnownedHeat(string change)
        {
            var h=Hearth();var tool=Carry("HearthNozzle");
            if(change=="cold")h.GetPart<ThermalPart>().Temperature=25;
            if(change=="empty")h.GetPart<FuelPart>().FuelMass=4;
            if(change=="public")h.GetPart<InstallationMarkerPart>().Origin="World";
            if(change=="nonplayer")Actor.Tags.Remove("Player");
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("DirectHearthHeat|")));
        }
        [Test] public void FailedNozzleActionLeavesFuelAndGroundUnchanged()
        {
            var h=Hearth();var tool=Carry("HearthNozzle");Zone.TileState.WriteCoating(11,10,"ice",5);
            string command=Row(tool,"DirectHearthHeat|");FailAfter();Assert.False(Act(tool,command));
            Assert.AreEqual(15,h.GetPart<FuelPart>().FuelMass);Assert.True(Zone.TileState.HasCoating(11,10,"ice"));
        }
        [Test] public void RodLoadsOneRootAndCarriesExactlyOneDischargeAcrossTwoCells()
        {
            var tool=Carry("DischargeRod");Carry("SparkRoot");Place("CopperPipe",12,10);
            Assert.True(Act(tool,Choice(tool,"LoadDischargeRod|")));Assert.AreEqual(0,Count("SparkRoot"));Assert.AreEqual(1,Stored(tool));
            string command=Row(tool,"DischargeRod|",12,10);Assert.True(Act(tool,command));Assert.AreEqual(0,Stored(tool));Assert.False(Act(tool,command));
        }
        [Test] public void LoadedRodRoundTripsWithoutGeneratingCharges()
        {
            var tool=Carry("DischargeRod");Carry("SparkRoot");Assert.True(Act(tool,Choice(tool,"LoadDischargeRod|")));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(tool);Assert.AreEqual(1,Stored(saved));Assert.AreEqual(tool.ID,saved.ID);
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("LoadDischargeRod|")));
        }
        [Test] public void BlockedOrHiddenIntermediateCellCannotBeDischargedThrough()
        {
            var tool=Carry("DischargeRod");Carry("SparkRoot");Place("CopperPipe",12,10);Assert.True(Act(tool,Choice(tool,"LoadDischargeRod|")));
            Place("Rock",11,10);Assert.False(Actions(tool).Any(a=>a.Command.Contains("|12|10|")));
        }
        [Test] public void FailedRodLoadReturnsTheRootWithoutCharging()
        {
            var tool=Carry("DischargeRod");Carry("SparkRoot");string command=Choice(tool,"LoadDischargeRod|");FailAfter();
            Assert.False(Act(tool,command));Assert.AreEqual(1,Count("SparkRoot"));Assert.AreEqual(0,Stored(tool));
        }
        [Test] public void CondenserSharesTheExistingVentBudgetAndFillsOnlyTheChosenFlask()
        {
            var vent=Place("SteamVent");var stock=SourceStockPart.For(Zone,vent,Clock.TickCount);int before=stock.Available;
            var tool=Carry("SteamCondenser");var flask=Flask(0);Assert.True(Act(tool,Choice(tool,"CondenseSteam|")));
            Assert.AreEqual(1,Units(flask));Assert.AreEqual("water",flask.GetPart<LiquidVesselPart>().LiquidId);Assert.AreEqual(before-1,stock.Available);
        }
        [Test] public void LookingAtUntouchedVentCreatesNoResourceStock()
        {
            var vent=Place("SteamVent");var tool=Carry("SteamCondenser");Flask(0);
            Assert.True(Actions(tool).Any(a=>a.Command.StartsWith("CondenseSteam|")));UseGuides.SectionText(tool,Zone,null,Actor);
            Assert.False(vent.HasPart<SourceStockPart>());
        }
        [TestCase("AshBed")] [TestCase("SpreadCookingCoals")]
        public void HeatAloneIsNotPotableWater(string blueprint)
        {Place(blueprint);var tool=Carry("SteamCondenser");Flask(0);Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("CondenseSteam|")));}
        [Test] public void CondenserDoesNotOverwriteTheContentsOfANonemptyFlask()
        {Place("SteamVent");var tool=Carry("SteamCondenser");Flask(1,"oil");Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("CondenseSteam|")));}
        [Test] public void FailedCondensingReturnsSourceStockAndLeavesFlaskEmpty()
        {
            var vent=Place("SteamVent");var stock=SourceStockPart.For(Zone,vent,Clock.TickCount);int before=stock.Available;
            var tool=Carry("SteamCondenser");var flask=Flask(0);string command=Choice(tool,"CondenseSteam|");FailAfter();Assert.False(Act(tool,command));
            Assert.AreEqual(before,stock.Available);Assert.AreEqual(0,Units(flask));
        }
        [TestCase("HearthNozzle","DirectHearthHeat|")] [TestCase("DischargeRod","DischargeRod|")] [TestCase("SteamCondenser","CondenseSteam|")]
        public void MovingAfterSelectionCannotRetargetThePaidAction(string blueprint,string prefix)
        {
            var tool=Carry(blueprint);
            if(blueprint=="HearthNozzle")Hearth();
            if(blueprint=="DischargeRod"){Carry("SparkRoot");Place("CopperPipe");Assert.True(Act(tool,Choice(tool,"LoadDischargeRod|")));}
            if(blueprint=="SteamCondenser"){Place("SteamVent");Flask(0);}
            string command=Actions(tool).First(a=>a.Command.StartsWith(prefix)).Command;
            Zone.MoveEntity(Actor,10,11);Assert.False(Act(tool,command));
        }
        public sealed class Probe : Part
        { public Action<GameEvent> Run;public override bool HandleEvent(GameEvent e){Run?.Invoke(e);return true;} }
        [TestCase("make_hearth_nozzle","HearthNozzle","FireClay",2)]
        [TestCase("make_discharge_rod","DischargeRod","KnotflaxCord",1)]
        [TestCase("make_steam_condenser","SteamCondenser","FireClay",1)]
        public void ActualForgeManifestMakesOnePristineToolFromFiniteMaterials(string recipe,string result,string extra,int count)
        {
            Place("TinkersForge");var spike=Carry("IronSpikeComponent");var material=Carry(extra);material.GetPart<StackerPart>().StackCount=count;
            string command=Choice(spike,"MaterialProcess|"+recipe+"|");UseGuides.SectionText(spike,Zone,null,Actor);
            Actions(spike);Actions(spike);
            Assert.True(Act(spike,command));Assert.AreEqual(0,Count("IronSpikeComponent"));Assert.AreEqual(0,Count(extra));Assert.AreEqual(1,Count(result));
            var tool=Pack.Objects.Single(e=>e.BlueprintName==result);Assert.AreEqual(0,Stored(tool));Assert.False(tool.HasPart<StackerPart>());
        }
        [Test] public void ExplicitComponentPermissionIncludingDenialAlwaysWins()
        {
            Place("TinkersForge");var spike=Carry("IronSpikeComponent");Carry("KnotflaxCord");spike.AddPart(new MaterialProcessInputPart{ProcessIds=""});
            Assert.False(Actions(spike).Any(a=>a.Command.StartsWith("MaterialProcess|")));
        }
        [Test] public void FinalFiveFuelCanBeUsedWithoutCreatingNegativeFuel()
        {
            var h=Hearth();h.GetPart<FuelPart>().FuelMass=5;var tool=Carry("HearthNozzle");
            Assert.True(Act(tool,Row(tool,"DirectHearthHeat|")));Assert.AreEqual(0,h.GetPart<FuelPart>().FuelMass);
        }
        [TestCase(134217728f)] [TestCase(67108864f)]
        public void RoundedFuelCannotPretendToPayFive(float amount)
        {
            var h=Hearth();h.GetPart<FuelPart>().MaxFuel=amount;h.GetPart<FuelPart>().FuelMass=amount;var tool=Carry("HearthNozzle");
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("DirectHearthHeat|")));
        }
        [Test] public void NewlyFrozenReceiverInvalidatesTheCapturedSelection()
        {
            Place("SteamVent");var tool=Carry("SteamCondenser");var flask=Flask(0);flask.AddPart(new StatusEffectsPart());flask.AddPart(new MaterialPart{MaterialTagsRaw="Metal"});
            string command=Choice(tool,"CondenseSteam|");Assert.True(flask.ApplyEffect(new FrozenEffect(1)));
            Assert.False(Act(tool,command));Assert.AreEqual(0,Units(flask));
        }
        [Test] public void LateReceiverFreezeRefundsOnlyTheNewWaterAndSourceCatch()
        {
            var vent=Place("SteamVent");var stock=SourceStockPart.For(Zone,vent,0);int before=stock.Available;
            var tool=Carry("SteamCondenser");var flask=Flask(0);flask.AddPart(new StatusEffectsPart());flask.AddPart(new MaterialPart{MaterialTagsRaw="Metal"});string command=Choice(tool,"CondenseSteam|");
            Actor.AddPart(new Probe{Run=e=>{if(e.ID=="AfterInventoryAction")Assert.True(flask.ApplyEffect(new FrozenEffect(1)));}});
            Assert.False(Act(tool,command));Assert.AreEqual(0,Units(flask));Assert.AreEqual(before,stock.Available);Assert.True(flask.HasEffect<FrozenEffect>());
        }
        [Test] public void FailedFirstCatchDoesNotLeaveAnAlternateFullStock()
        {
            var vent=Place("SteamVent");var tool=Carry("SteamCondenser");var flask=Flask(0);string command=Choice(tool,"CondenseSteam|");FailAfter();
            Assert.False(Act(tool,command));Assert.False(vent.HasPart<SourceStockPart>());Assert.AreEqual(0,Units(flask));
        }
        [Test] public void ExistingConnectedAshBedHolderPaysForTheSelectedSteamVent()
        {
            var ash=Place("AshBed",10,9);var stock=SourceStockPart.For(Zone,ash,0);stock.Available=1;
            var vent=Place("SteamVent");var tool=Carry("SteamCondenser");var flask=Flask(0);Assert.True(Act(tool,Choice(tool,"CondenseSteam|")));
            Assert.AreEqual(1,Units(flask));Assert.AreEqual(0,stock.Available);Assert.False(vent.HasPart<SourceStockPart>());
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("CondenseSteam|")));
        }
        [Test] public void OneGameDayRestoresOnlyOneWaterCatch()
        {
            var vent=Place("SteamVent");var stock=SourceStockPart.For(Zone,vent,0);stock.Available=0;stock.LastTick=-SourceStockPart.RegenTicks;
            var tool=Carry("SteamCondenser");var flask=Flask(0);Assert.True(Act(tool,Choice(tool,"CondenseSteam|")));
            Assert.AreEqual(1,Units(flask));Assert.AreEqual(0,stock.Available);Assert.AreEqual(0,stock.LastTick);
        }
        [Test] public void AStackOfEmptyFlasksMustBeSplitBeforeCatching()
        {
            Place("SteamVent");var tool=Carry("SteamCondenser");var flask=Flask(0);flask.AddPart(new StackerPart{StackCount=3,MaxStack=20});
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("CondenseSteam|")));Assert.AreEqual(0,Units(flask));
        }
        [Test] public void ReaderDoesNotReplaceAnOpenHeatQuote()
        {
            Hearth();var tool=Carry("HearthNozzle");string command=Row(tool,"DirectHearthHeat|");
            UseGuides.SectionText(tool,Zone,null,Actor);Assert.True(Act(tool,command));
        }
        [Test] public void CommittedObserverCannotLaunchHeatAfterMovingTheSource()
        {
            var h=Hearth();var tool=Carry("HearthNozzle");Zone.TileState.WriteCoating(11,10,"ice",5);
            var probe=new Probe{Run=e=>
            {
                if(e.ID!="InventoryAction")return;var tx=e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction");
                typeof(CavesOfOoo.Core.Inventory.InventoryTransaction).GetMethod("AfterCommit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(tx,new object[]{(Action)(()=>Zone.MoveEntity(h,8,10))});
            }};tool.AddPart(probe);tool.Parts.Remove(probe);tool.Parts.Insert(0,probe);string command=Row(tool,"DirectHearthHeat|");
            Assert.True(Act(tool,command));Assert.AreEqual(10,h.GetPart<FuelPart>().FuelMass);Assert.True(Zone.TileState.HasCoating(11,10,"ice"));
        }
        [Test] public void FailedToolWorkDoesNotHealIndependentPlayerDamage()
        {
            Hearth();var tool=Carry("HearthNozzle");string command=Row(tool,"DirectHearthHeat|");int hp=Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new Probe{Run=e=>{if(e.ID=="AfterInventoryAction"){Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("injury survives");}}});
            Assert.False(Act(tool,command));Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));
        }
        [TestCase("HearthNozzle","DirectHearthHeat|")] [TestCase("DischargeRod","DischargeRod|")] [TestCase("SteamCondenser","CondenseSteam|")]
        public void RepeatedMenuGatherRetainsTheSameCurrentChoice(string blueprint,string prefix)
        {
            var tool=Carry(blueprint);
            if(blueprint=="HearthNozzle")Hearth();
            if(blueprint=="DischargeRod"){Carry("SparkRoot");Place("CopperPipe");Assert.True(Act(tool,Choice(tool,"LoadDischargeRod|")));}
            if(blueprint=="SteamCondenser"){Place("SteamVent");Flask(0);}
            string command=Actions(tool).First(a=>a.Command.StartsWith(prefix)).Command;
            for(int i=0;i<3;i++){Actions(tool);UseGuides.SectionText(tool,Zone,null,Actor);}
            Assert.True(Act(tool,command));Assert.False(Act(tool,command));
        }
        [Test] public void SteadySteamSourceDoesNotDependOnItsIncidentalCoolingTemperature()
        {
            var vent=Place("SteamVent");vent.GetPart<ThermalPart>().Temperature=25;
            Assert.Greater(vent.GetPart<TileStateSourcePart>().Heat,0);
            var tool=Carry("SteamCondenser");var flask=Flask(0);
            Assert.True(Act(tool,Choice(tool,"CondenseSteam|")));Assert.AreEqual(1,Units(flask));
        }
        [TestCase("equipped")] [TestCase("quantity")] [TestCase("pack-alias")] [TestCase("effect")]
        public void LastRootMustRemainOneFullyRemovedUnitBeforeChargeCanCommit(string change)
        {
            var tool=Carry("DischargeRod");var root=Carry("SparkRoot");
            if(change=="effect"){root.AddPart(new StatusEffectsPart());root.AddPart(new MaterialPart{MaterialTagsRaw="Metal"});}
            string command=Choice(tool,"LoadDischargeRod|");
            Actor.AddPart(new Probe{Run=e=>
            {
                if(e.ID!="AfterInventoryAction")return;
                if(change=="equipped")root.GetPart<PhysicsPart>().Equipped=Actor;
                if(change=="quantity")root.GetPart<StackerPart>().StackCount=2;
                if(change=="pack-alias")Pack.Objects.Add(root);
                if(change=="effect")Assert.True(root.ApplyEffect(new WetEffect(.5f)));
            }});
            Assert.False(Act(tool,command));Assert.AreEqual(0,Stored(tool));
            Assert.LessOrEqual(Pack.Objects.Count(e=>e==root),1,"rollback must not insert a second reference over an independent alias");
            if(change=="equipped")Assert.AreSame(Actor,root.GetPart<PhysicsPart>().Equipped);
            if(change=="quantity")Assert.AreEqual(2,root.GetPart<StackerPart>().StackCount,"independent quantity change is not rewound");
        }
    }
}
