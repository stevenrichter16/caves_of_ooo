using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DirectLiquidPumpTests : FiftyWorldFixture
    {
        CavesOfOoo.Data.EntityFactory oldForge;
        [SetUp] public void ReadyPump()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            Bed(10,10,false);Bed(11,10,false);Bed(9,10,false);
        }
        [TearDown] public void RestorePump(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();RouteWorkCreationProbe.Run=null;}
        Entity Tool()=>Carry("HandPump");
        Entity Stock(int amount=60,string liquid="acid",int x=11,int y=10)
        {
            var e=Factory.CreateEntity(liquid=="acid"?"AcidPool":"PouredLiquidPool");Assert.NotNull(e);
            var p=e.GetPart<LiquidPoolPart>();p.LiquidId=liquid;p.Volume=amount;p.Initialize();Assert.True(Zone.AddEntity(e,x,y));return e;
        }
        string Stroke(Entity tool)=>Choice(tool,"PumpLiquid|",a=>a.Display.Contains("to 9,10"));
        Entity Outlet()=>Zone.GetCell(9,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>());
        [Test] public void OrdinaryFlaskRemainsAnAlternativeForTheSameFiniteStock()
        {var source=Stock();var flask=Flask(0);Assert.True(Act(flask,Choice(flask,"FillLiquidVessel|")));Assert.AreEqual(48,source.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(12,Units(flask));}
        [Test] public void OneStrokeMovesTwentyRealUnitsAndKeepsTheSamePump()
        {
            var tool=Tool();var source=Stock();Assert.True(Act(tool,Stroke(tool)));
            Assert.AreEqual(40,source.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(20,PoolUnits(9,10));Assert.AreEqual("acid",Outlet().GetPart<LiquidPoolPart>().LiquidId);
            Assert.True(Pack.Objects.Contains(tool));Assert.True(Zone.TileState.HasCoating(9,10,"acid"));
        }
        [Test] public void FinalStrokeRetiresOnlyTheDepletedSourceAndItsProjection()
        {
            var tool=Tool();var source=Stock();for(int i=0;i<3;i++)Assert.True(Act(tool,Stroke(tool)));
            Assert.False(Zone.GetReadOnlyEntities().Contains(source));Assert.False(Zone.TileState.HasCoating(11,10,"acid"));
            Assert.AreEqual(60,PoolUnits(9,10));Assert.True(Zone.TileState.HasCoating(9,10,"acid"));
        }
        [Test] public void ShortFinalStrokeMovesOnlyTheRemainingStock()
        {var tool=Tool();Stock(7);Assert.True(Act(tool,Stroke(tool)));Assert.AreEqual(7,PoolUnits(9,10));Assert.AreEqual(0,PoolUnits(11,10));}
        [Test] public void ExistingOutletKeepsItsIdentityAndOriginalStock()
        {var tool=Tool();Stock();var outlet=Stock(5,"acid",9,10);Assert.True(Act(tool,Stroke(tool)));Assert.AreSame(outlet,Outlet());Assert.AreEqual(25,PoolUnits(9,10));}
        [TestCase("hidden")][TestCase("occupied")][TestCase("different")][TestCase("blocked")]
        public void UnsafeOutletHasNoDisplayedWestStroke(string mode)
        {
            var tool=Tool();Stock();
            if(mode=="hidden")Zone.GetCell(9,10).IsVisible=false;
            if(mode=="occupied")Place("Villager",9,10);
            if(mode=="different")Stock(5,"water",9,10);
            if(mode=="blocked")Place("VillageDoor",9,10);
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")&&a.Display.Contains("to 9,10")));
        }
        [TestCase("source")][TestCase("outlet")]
        public void EvenAnEmptySecondPoolOwnerRefusesAmbiguousProjection(string endpoint)
        {var tool=Tool();Stock();if(endpoint=="outlet")Stock(5,"acid",9,10);Stock(0,"acid",endpoint=="source"?11:9,10);Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [TestCase("unknown")][TestCase("owned")][TestCase("film")][TestCase("renewing")]
        public void UnsupportedSourceDoesNotBecomeGenericFreeDrainage(string mode)
        {
            var tool=Tool();var source=Stock();
            if(mode=="unknown")source.AddPart(new RouteWorkAfterProbe());
            if(mode=="owned")source.Properties["OwnerID"]="resident";
            if(mode=="film")source.AddPart(new FiniteLiquidFilmPart());
            if(mode=="renewing")source.AddPart(new WellPart());
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));Assert.AreEqual(60,source.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void FrozenWaterRefusesRatherThanMovingIceAsLiquid()
        {var tool=Tool();var source=Stock(20,"water");source.AddPart(new ThermalPart{Temperature=-5,FreezeTemperature=0});Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [Test] public void StaleSourceStockDoesNotExecuteAnOldQuote()
        {var tool=Tool();var source=Stock();var cmd=Stroke(tool);source.GetPart<LiquidPoolPart>().Volume--;Assert.False(Act(tool,cmd));Assert.AreEqual(59,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));}
        [Test] public void RepeatedGathersKeepTheActualCurrentChoice()
        {var tool=Tool();Stock();var cmd=Stroke(tool);Actions(tool);Assert.True(Act(tool,cmd));}
        [Test] public void PhantomPumpCannotOperateTheActualCarriedOwner()
        {var phantom=Tool();Stock();Assert.True(Pack.RemoveObject(phantom));var actual=Tool();actual.ID=phantom.ID;phantom.GetPart<PhysicsPart>().InInventory=Actor;Assert.False(Actions(phantom).Any(a=>a.Command.StartsWith("PumpLiquid|")));}
        [Test] public void InterruptedStrokeRestoresQuantitiesAndLeavesNoNewProjection()
        {var tool=Tool();Stock();var cmd=Stroke(tool);FailAfter();Assert.False(Act(tool,cmd));Assert.AreEqual(60,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));Assert.False(Zone.TileState.HasCoating(9,10,"acid"));}
        [Test] public void RollbackPreservesIndependentStocksAndActorInjury()
        {
            var tool=Tool();var source=Stock();var outlet=Stock(5,"acid",9,10);var cmd=Stroke(tool);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{source.GetPart<LiquidPoolPart>().Volume+=3;outlet.GetPart<LiquidPoolPart>().Volume+=7;Actor.GetStat("Hitpoints").BaseValue-=3;}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(63,PoolUnits());Assert.AreEqual(12,PoolUnits(9,10));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);
        }
        [Test] public void ExternallyConsumedUncommittedCreditCannotAlsoRefundItsSource()
        {
            var tool=Tool();Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Outlet().GetPart<LiquidPoolPart>().Volume=0});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(40,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));
        }
        [Test] public void ReplacedSourcePartCannotDestroyTheAlreadyPaidOutletOnRollback()
        {
            var tool=Tool();var source=Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{source.RemovePart(source.GetPart<LiquidPoolPart>());source.AddPart(new LiquidPoolPart{LiquidId="acid",Volume=40});}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(40,PoolUnits());Assert.AreEqual(20,PoolUnits(9,10));
        }
        [Test] public void SaturatedSourceCannotDestroyUnrefundableOutletCredit()
        {
            var tool=Tool();var source=Stock();var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>source.GetPart<LiquidPoolPart>().Volume=int.MaxValue});FailAfter();
            Assert.False(Act(tool,cmd));Assert.AreEqual(int.MaxValue,PoolUnits());Assert.AreEqual(20,PoolUnits(9,10));
        }
        [Test] public void EmptyRollbackOutputKeepsAnIndependentAttachedPayload()
        {
            var tool=Tool();Stock();var cmd=Stroke(tool);Entity output=null;var independent=new RouteWorkAfterProbe();
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{output=Outlet();output.AddPart(independent);}});FailAfter();
            Assert.False(Act(tool,cmd));Assert.True(Zone.GetReadOnlyEntities().Contains(output));Assert.True(output.Parts.Contains(independent));Assert.AreEqual(60,PoolUnits());Assert.AreEqual(0,PoolUnits(9,10));
        }
        [Test] public void FullIntegerOutletRefusesWithoutWrappingOrSpending()
        {var tool=Tool();Stock();Stock(int.MaxValue,"acid",9,10);Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")&&a.Display.Contains("to 9,10")));Assert.AreEqual(60,PoolUnits());}
        [Test] public void UnadmittedFactoryOverridesRefuseBeforeCreatingAPuddle()
        {
            var tool=Tool();Stock();bool created=false;Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");RouteWorkCreationProbe.Run=e=>created=true;
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PumpLiquid|")));Assert.False(created);Assert.AreEqual(60,PoolUnits());
        }
        [Test] public void DepletedOwnerIsNotRemovedIfReplenishedBeforeCommit()
        {
            var tool=Tool();var source=Stock(20);var cmd=Stroke(tool);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>source.GetPart<LiquidPoolPart>().Volume=1});
            Assert.False(Act(tool,cmd));Assert.True(Zone.GetReadOnlyEntities().Contains(source));Assert.AreEqual(21,PoolUnits());Assert.True(Zone.TileState.HasCoating(11,10,"acid"));
        }
        [Test] public void PumpAndActualTransferredPoolSaveWithoutRenewingStock()
        {
            var tool=Tool();Stock();Assert.True(Act(tool,Stroke(tool)));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Outlet());Assert.AreEqual(20,saved.GetPart<LiquidPoolPart>().Volume);
            var savedTool=PartRoundTripHelper.RoundTripEntityViaTokenGraph(tool);Assert.NotNull(savedTool.GetPart("DirectLiquidPump"));Assert.AreEqual(tool.ID,savedTool.ID);
        }
        [Test] public void ForgeMakesReusablePumpFromExactFiniteComponents()
        {
            Place("TinkersForge",10,9);var spike=Carry("IronSpikeComponent");Carry("FireClay").GetPart<StackerPart>().StackCount=2;
            Carry("LeatherBindingComponent").GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"MaterialProcess|make_hand_pump|")));Assert.AreEqual(1,Count("HandPump"));
            Assert.AreEqual(0,Count("FireClay"));Assert.AreEqual(0,Count("LeatherBindingComponent"));Assert.AreEqual(0,Count("IronSpikeComponent"));
        }
    }
}
