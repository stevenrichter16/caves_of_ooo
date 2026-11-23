using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WellFlaskFillingTests : FiftyWorldFixture
    {
        Entity Well(int stock=3,bool pool=false)
        {var e=Place(pool?"GroveSeep":"Cistern");e.GetPart<WellPart>().Stock=stock;if(pool)e.GetPart<LiquidPoolPart>().Volume=3;return e;}
        string Row(Entity flask)=>Choice(flask,"FillWellFlask|");
        void After(Action a)=>Actor.AddPart(new BakingAfterProbe{After=a});
        [TestCase("broken")][TestCase("ice")][TestCase("stock")][TestCase("blocked")][TestCase("capacity")][TestCase("pool")]
        public void CommitGuardRejectsNonthrowingSourceOrRecipientChanges(string kind)
        {var w=Well();var f=Flask(0);After(()=>{if(kind=="broken")w.AddPart(new RepairablePart());if(kind=="ice")Zone.TileState.WriteCoating(11,10,"ice",5);if(kind=="stock")w.GetPart<WellPart>().Stock++;if(kind=="blocked")Assert.True(Actor.ApplyEffect(new ParalyzedEffect()));if(kind=="capacity")f.GetPart<LiquidVesselPart>().Capacity++;if(kind=="pool")w.AddPart(new LiquidPoolPart{LiquidId="water",Volume=7});});Assert.False(Act(f,Row(f)));Assert.AreEqual(0,Units(f));Assert.AreEqual(kind=="stock"?4:3,w.GetPart<WellPart>().Stock);if(kind=="pool")Assert.AreEqual(7,w.GetPart<LiquidPoolPart>().Volume);if(kind=="broken")Assert.False(w.GetPart<WellPart>().IsUsable);if(kind=="blocked")Assert.True(Actor.HasEffect<ParalyzedEffect>());}
        [Test] public void CommitGuardRejectsNewOathRefusalWithoutUndoingTheFact()
        {var old=NarrativeStatePart.Current;try{NarrativeStatePart.Current=new NarrativeStatePart();Zone.RemoveEntity(Actor);Zone=new Zone("Overworld.8.16.0");Assert.True(Zone.AddEntity(Actor,10,10));var w=Well();var f=Flask(0);After(()=>NarrativeStatePart.Current.SetFact(TentRightOath.OathbreakerFact,1));Assert.False(Act(f,Row(f)));Assert.True(TentRightOath.RefusesWater(Actor,Zone));Assert.AreEqual(0,Units(f));Assert.AreEqual(3,w.GetPart<WellPart>().Stock);}finally{NarrativeStatePart.Current=old;}}
        [Test] public void PoolBackedWellCannotBypassOathRefusalThroughAFlask()
        {var old=NarrativeStatePart.Current;try{NarrativeStatePart.Current=new NarrativeStatePart();NarrativeStatePart.Current.SetFact(TentRightOath.OathbreakerFact,1);Zone.RemoveEntity(Actor);Zone=new Zone("Overworld.8.16.0");Assert.True(Zone.AddEntity(Actor,10,10));var w=Well(3,true);var f=Flask(0);Assert.False(Act(f,"FillLiquidVessel|"+Id(w)+"|water"));Assert.AreEqual(0,Units(f));Assert.AreEqual(3,w.GetPart<LiquidPoolPart>().Volume);}finally{NarrativeStatePart.Current=old;}}
        [TestCase(false)][TestCase(true)] public void NestedSecondFlaskCannotSpendTheSameActorAction(bool shared)
        {Well(-1);var f=Flask(0);var other=Flask(0);string second=Row(other);InventoryTransaction captured=null;var spy=new BakingActionProbe{Capture=e=>captured=e.GetParameter<InventoryTransaction>("InventoryTransaction")};f.AddPart(spy);f.Parts.Remove(spy);f.Parts.Insert(0,spy);bool once=true,nested=true;After(()=>{if(!once)return;once=false;nested=LiquidVesselService.TryAct(Actor,other,Zone,second,shared?captured:null);});Assert.True(Act(f,Row(f)));Assert.False(nested);Assert.AreEqual(12,Units(f));Assert.AreEqual(0,Units(other));}
        [TestCase(false)][TestCase(true)] public void FiniteWellFillsAndExhaustsWithoutRetiringOwner(bool pool)
        {var w=Well(3,pool);var f=Flask(0);Assert.True(Act(f,Row(f)));Assert.AreEqual(3,Units(f));Assert.AreEqual(0,pool?w.GetPart<LiquidPoolPart>().Volume:w.GetPart<WellPart>().Stock);Assert.NotNull(Zone.GetEntityCell(w));}
        [Test] public void EndlessWellFillsCapacityAndKeepsLegacyPolicy()
        {var w=Well(-1);var f=Flask(0);Assert.True(Act(f,Row(f)));Assert.AreEqual(12,Units(f));Assert.AreEqual(-1,w.GetPart<WellPart>().Stock);}
        [Test] public void PositivePoolWinsOverEndlessStock()
        {var w=Well(-1,true);var f=Flask(0);Assert.True(Act(f,Row(f)));Assert.AreEqual(3,Units(f));Assert.AreEqual(-1,w.GetPart<WellPart>().Stock);Assert.AreEqual(0,w.GetPart<LiquidPoolPart>().Volume);}
        [TestCase(false)][TestCase(true)] public void BrokenWellCannotBeFilledByLegacyPoolCommand(bool pool)
        {var w=Well(3,pool);w.AddPart(new RepairablePart());var f=Flask(0);Assert.False(Act(f,"FillLiquidVessel|"+Id(w)+"|water"));Assert.AreEqual(0,Units(f));Assert.AreEqual(3,pool?w.GetPart<LiquidPoolPart>().Volume:w.GetPart<WellPart>().Stock);}
        [TestCase(0)][TestCase(-2)] public void InvalidOrDryStockOffersNoFill(int stock)
        {Well(stock);var f=Flask(0);Assert.False(Actions(f).Any(a=>a.Command.StartsWith("FillWellFlask|")));}
        [Test] public void DryPoolNeverFallsBackToEndlessStock()
        {var w=Well(-1,true);w.GetPart<LiquidPoolPart>().Volume=0;var f=Flask(0);Assert.False(Actions(f).Any(a=>a.Command.StartsWith("FillWellFlask|")));}
        [Test] public void LiteralWellWithoutPhysicsStillSuppliesWater()
        {var w=Well();w.RemovePart(w.GetPart<PhysicsPart>());var f=Flask(0);Assert.True(Act(f,Row(f)));Assert.AreEqual(3,Units(f));}
        [Test] public void SameWaterTopUpSpendsOnlyFreeCapacity()
        {var w=Well(10);var f=Flask(10);Assert.True(Act(f,Row(f)));Assert.AreEqual(12,Units(f));Assert.AreEqual(8,w.GetPart<WellPart>().Stock);}
        [TestCase("oil")][TestCase("full")][TestCase("stacked")]
        public void IneligibleRecipientHasNoWellFill(string kind)
        {Well();var f=Flask(kind=="oil"?2:kind=="full"?12:0,kind=="oil"?"oil":"water");if(kind=="stacked")f.AddPart(new StackerPart{StackCount=2});Assert.False(Actions(f).Any(a=>a.Command.StartsWith("FillWellFlask|")));}
        [TestCase("ice")][TestCase("mixed")][TestCase("frozen")][TestCase("broken")][TestCase("ambiguous")]
        public void SourceConditionsPreventFilling(string kind)
        {var w=Well(pool:kind=="mixed");if(kind=="ice")Zone.TileState.WriteCoating(11,10,"ice",5);if(kind=="mixed")Zone.TileState.WriteCoating(11,10,"oil",5);if(kind=="frozen"){w.SetTag("Water");Assert.True(w.ApplyEffect(new FrozenEffect(1f)));}if(kind=="broken")w.AddPart(new RepairablePart());if(kind=="ambiguous")w.AddPart(new ContainedLiquidSourcePart());var f=Flask(0);Assert.False(Actions(f).Any(a=>a.Command.StartsWith("FillWellFlask|")));}
        [TestCase("stock")][TestCase("volume")][TestCase("capacity")][TestCase("pool-kind")]
        public void StaleQuotedQuantitiesRefuseFreely(string change)
        {var w=Well();var f=Flask(0);string c=Row(f);if(change=="stock")w.GetPart<WellPart>().Stock++;if(change=="volume"){f.GetPart<LiquidVesselPart>().Volume=1;f.GetPart<LiquidVesselPart>().LiquidId="water";}if(change=="capacity")f.GetPart<LiquidVesselPart>().Capacity++;if(change=="pool-kind")w.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});Assert.False(Act(f,c));Assert.AreEqual(change=="volume"?1:0,Units(f));}
        [TestCase(false)][TestCase(true)] public void OuterVetoRestoresOnlyOwnedDeltas(bool pool)
        {var w=Well(3,pool);var f=Flask(0);int hp=Actor.GetStatValue("Hitpoints");After(()=>{if(pool)w.GetPart<LiquidPoolPart>().Volume+=2;else w.GetPart<WellPart>().Stock+=2;f.GetPart<LiquidVesselPart>().Volume++;Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("veto");});Assert.False(Act(f,Row(f)));Assert.AreEqual(1,Units(f));Assert.AreEqual(5,pool?w.GetPart<LiquidPoolPart>().Volume:w.GetPart<WellPart>().Stock);Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));}
        [Test] public void ForcedRecipientDropSurvivesRejectedFill()
        {var w=Well();var f=Flask(0);After(()=>{Pack.RemoveObject(f);Zone.AddEntity(f,10,10);});Assert.False(Act(f,Row(f)));Assert.False(Pack.Objects.Contains(f));Assert.NotNull(Zone.GetEntityCell(f));Assert.AreEqual(0,Units(f));Assert.AreEqual(3,w.GetPart<WellPart>().Stock);}
        [TestCase("source")][TestCase("vessel")][TestCase("physics")][TestCase("liquid")][TestCase("source-move")]
        public void ReplacementOrMovementRefusesWithoutOverwritingIndependentState(string kind)
        {var w=Well();var f=Flask(0);After(()=>{if(kind=="source"){w.RemovePart(w.GetPart<WellPart>());w.AddPart(new WellPart{Stock=7});}if(kind=="vessel"){f.RemovePart(f.GetPart<LiquidVesselPart>());f.AddPart(new LiquidVesselPart{Volume=5,LiquidId="water"});}if(kind=="physics"){f.RemovePart(f.GetPart<PhysicsPart>());f.AddPart(new PhysicsPart{Takeable=true,InInventory=Actor});}if(kind=="liquid"){f.GetPart<LiquidVesselPart>().LiquidId="oil";f.GetPart<LiquidVesselPart>().Volume=2;}if(kind=="source-move")Zone.MoveEntity(w,20,10);});Assert.False(Act(f,Row(f)));Assert.AreEqual(kind=="vessel"?5:kind=="liquid"?2:0,Units(f));Assert.AreEqual(kind=="source"?7:3,w.GetPart<WellPart>().Stock);if(kind=="source-move")Assert.AreEqual(20,Zone.GetEntityCell(w).X);}
        [Test] public void RealCellMenuNamesRecipientAndDeclaresOneAction()
        {Well();var f=Flask(0);var row=CellMenu(11,10).Single(a=>a.Command.StartsWith("FillWellFlask|"));Assert.AreSame(f,row.Via);Assert.AreEqual(ActionTime.OneAction,row.Time);Assert.True(ChooseRow(row,11,10));Assert.AreEqual(3,Units(f));}
    }
}
