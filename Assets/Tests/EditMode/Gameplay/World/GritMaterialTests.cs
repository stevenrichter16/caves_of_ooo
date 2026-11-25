using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class GritPrivatePayloadProbe : Part { public override string Name=>"GritPrivatePayload"; }
    public sealed class GritMaterialTests : FiftyWorldFixture
    {
        EntityFactory oldTrader,oldRestock;System.Random oldRng;
        Dictionary<object,object> oldTables;bool oldInitialized;
        static FieldInfo Tables=>typeof(LootTableRegistry).GetField("_byName",BindingFlags.NonPublic|BindingFlags.Static);
        static FieldInfo Initialized=>typeof(LootTableRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
        [SetUp] public void ReadyGrit()
        {
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            oldTrader=TraderPart.Factory;oldRestock=TraderRestockSystem.Factory;oldRng=TraderPart.Rng;
            TraderPart.Factory=TraderRestockSystem.Factory=Factory;TraderPart.Rng=new System.Random(64);
            oldTables=new Dictionary<object,object>();foreach(DictionaryEntry row in (IDictionary)Tables.GetValue(null))oldTables[row.Key]=row.Value;
            oldInitialized=(bool)Initialized.GetValue(null);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(a=>a.text));
        }
        [TearDown] public void RestoreGrit()
        {
            TraderPart.Factory=oldTrader;TraderRestockSystem.Factory=oldRestock;TraderPart.Rng=oldRng;
            var tables=(IDictionary)Tables.GetValue(null);tables.Clear();foreach(var row in oldTables)tables[row.Key]=row.Value;Initialized.SetValue(null,oldInitialized);
            LiquidSlipSystem.TestRng=null;
        }
        Entity Grit(int units=1){var e=Carry("CoarseGrit");e.GetPart<StackerPart>().StackCount=units;return e;}
        string East(Entity e)=>Choice(e,"ScatterGritMaterial|",a=>a.Display.StartsWith("scatter grit east (",StringComparison.Ordinal));
        bool HasEast(Entity e)=>Actions(e).Any(a=>a.Command.StartsWith("ScatterGritMaterial|",StringComparison.Ordinal) && a.Display.StartsWith("scatter grit east (",StringComparison.Ordinal));
        ZoneTileState.Layer Layer()=>Zone.TileState.Get(11,10)?.Residues.SingleOrDefault(r=>r.Id=="grit");
        static int Stock(Entity e,string bp)=>e.GetPart<InventoryPart>().Objects.Where(i=>i.BlueprintName==bp).Sum(i=>i.GetPart<StackerPart>()?.StackCount??1);
        Entity Mender()=>MorrowfastContent.CreateResident("southwest-craftsperson",Factory);

        [Test] public void LegacySilverSandStillSuppliesItsExistingTwelveTurnTraction()
        {var sand=Carry("SilverSand");Assert.True(Act(sand,Choice(sand,"ScatterGrit|",a=>a.Display=="scatter grit east (1 item)")));Assert.AreEqual(12,Layer().Turns);}
        [Test] public void FineFireClayIsNotQualifiedTractionGrit()
        {Assert.False(HasEast(Carry("FireClay")));Assert.IsNull(Layer());}
        [Test] public void OneActualUnitMakesExistingTractionAndLeavesTheRestOfItsStack()
        {var grit=Grit(3);Assert.True(Act(grit,East(grit)));Assert.AreEqual(2,grit.GetPart<StackerPart>().StackCount);Assert.AreSame(Actor,grit.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(12,Layer().Turns);}
        [Test] public void ConsumingTheLastUnitRemovesTheActualInventoryIdentity()
        {var grit=Grit();Assert.True(Act(grit,East(grit)));Assert.False(Pack.Objects.Contains(grit));Assert.IsNull(grit.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(12,Layer().Turns);}
        [Test] public void RepeatedMenuReadsRetainTheDisplayedChoice()
        {var grit=Grit(2);string command=East(grit);Actions(grit);Assert.True(Act(grit,command));Assert.AreEqual(1,grit.GetPart<StackerPart>().StackCount);}
        [Test] public void ChangedQuantityInvalidatesTheQuoteWithoutSpendingAnotherUnit()
        {var grit=Grit(3);string command=East(grit);grit.GetPart<StackerPart>().StackCount=2;Assert.False(Act(grit,command));Assert.AreEqual(2,grit.GetPart<StackerPart>().StackCount);Assert.IsNull(Layer());}
        [Test] public void RemovingTheActualCapabilityInvalidatesItsQuote()
        {var grit=Grit();string command=East(grit);grit.RemovePart(grit.Parts.Single(p=>p.Name=="GritMaterial"));Assert.False(Act(grit,command));Assert.True(Pack.Objects.Contains(grit));Assert.IsNull(Layer());}
        [Test] public void AnotherCanonicalCommodityWithTheSameCapabilityWorks()
        {
            Grit();var source=Factory.Blueprints["CoarseGrit"];var other=new Blueprint{Name="RedTractionGranules",Baked=true};
            foreach(var p in source.Parts)other.Parts[p.Key]=new Dictionary<string,string>(p.Value);
            foreach(var p in source.Tags)other.Tags[p.Key]=p.Value;
            Factory.Blueprints[other.Name]=other;var grit=Carry(other.Name);
            Assert.True(Act(grit,East(grit)));Assert.AreEqual(12,Layer().Turns);
        }
        [Test] public void UnknownPrivatePayloadDoesNotBecomeDisposablePlainGrit()
        {var grit=Grit();grit.AddPart(new GritPrivatePayloadProbe());Assert.False(HasEast(grit));Assert.True(Pack.Objects.Contains(grit));}
        [Test] public void ActiveBrokenStateRefusesWithoutErasingItsEffect()
        {var grit=Grit();Assert.True(grit.ApplyEffect(new BrokenEffect()));Assert.False(HasEast(grit));Assert.True(grit.HasEffect<BrokenEffect>());}
        [TestCase("hidden")][TestCase("blocked")][TestCase("covered")]
        public void UnsupportedTargetDoesNotOfferAnAction(string kind)
        {
            var grit=Grit();if(kind=="hidden")Zone.GetCell(11,10).IsVisible=false;
            if(kind=="blocked")Place("StoneWall");if(kind=="covered")Zone.TileState.WriteResidue(11,10,"grit",20);
            Assert.False(HasEast(grit));Assert.True(Pack.Objects.Contains(grit));
        }
        [Test] public void LateVetoRetractsOnlyOwnGritAndReturnsTheUnit()
        {var grit=Grit();string command=East(grit);FailAfter();Assert.False(Act(grit,command));Assert.True(Pack.Objects.Contains(grit));Assert.IsNull(Layer());}
        [Test] public void VetoPreservesIndependentActorInjury()
        {
            var grit=Grit();string command=East(grit);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Actor.GetStat("Hitpoints").BaseValue-=3});FailAfter();
            Assert.False(Act(grit,command));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);Assert.True(Pack.Objects.Contains(grit));Assert.IsNull(Layer());
        }
        [Test] public void VetoPreservesIndependentStackConsumption()
        {
            var grit=Grit(5);string command=East(grit);Actor.AddPart(new RouteWorkAfterProbe{Run=()=>grit.GetPart<StackerPart>().StackCount--});FailAfter();
            Assert.False(Act(grit,command));Assert.AreEqual(4,grit.GetPart<StackerPart>().StackCount);Assert.IsNull(Layer());
        }
        [Test] public void AListenerCannotReplaceTheRemainingStackWithASameIdAlias()
        {
            var grit=Grit(3);string command=East(grit);Entity alias=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>
            {
                // Preserve the phantom original owner pointer deliberately;
                // only the new identity is physically listed in the pack.
                Assert.True(Pack.Objects.Remove(grit));alias=Factory.CreateEntity("CoarseGrit");alias.ID=grit.ID;
                alias.GetPart<StackerPart>().StackCount=2;alias.GetPart<PhysicsPart>().InInventory=Actor;Pack.Objects.Add(alias);
            }});
            Assert.False(Act(grit,command));Assert.False(Pack.Objects.Contains(grit));Assert.True(Pack.Objects.Contains(alias));
            Assert.AreEqual(2,alias.GetPart<StackerPart>().StackCount);Assert.IsNull(Layer());
        }
        [Test] public void PlainGritActuallyStacksAndSplitsWithoutLosingItsCapability()
        {
            var grit=Grit(2);Assert.True(Pack.AddObject(Factory.CreateEntity("CoarseGrit")));
            Assert.AreEqual(3,grit.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Pack.Objects.Count(e=>e.BlueprintName=="CoarseGrit"));
            var split=grit.GetPart<StackerPart>().SplitStack(1);Assert.NotNull(split);Assert.True(split.Parts.Any(p=>p.Name=="GritMaterial"));
            Assert.AreEqual(2,grit.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,split.GetPart<StackerPart>().StackCount);
            Assert.True(Pack.AddObject(split));Assert.AreEqual(3,grit.GetPart<StackerPart>().StackCount);
        }
        [Test] public void VetoDoesNotPullAnIndependentlyDroppedStackBackIntoThePack()
        {
            var grit=Grit(3);string command=East(grit);
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Assert.True(Pack.RemoveObject(grit));Assert.True(Zone.AddEntity(grit,10,10));}});FailAfter();
            Assert.False(Act(grit,command));Assert.AreEqual(3,grit.GetPart<StackerPart>().StackCount);Assert.False(Pack.Objects.Contains(grit));Assert.AreSame(Zone.GetCell(10,10),Zone.GetEntityCell(grit));Assert.IsNull(Layer());
        }
        [Test] public void VetoPreservesAnIndependentLongerGritLease()
        {
            var grit=Grit();string command=East(grit);Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Zone.TileState.WriteResidue(11,10,"grit",30)});FailAfter();
            Assert.False(Act(grit,command));Assert.AreEqual(30,Layer().Turns);Assert.True(Pack.Objects.Contains(grit));
        }
        [Test] public void SynchronousTileCallbackReplacementIsNotAdoptedForRollback()
        {
            var grit=Grit();string command=East(grit);ZoneTileState.Layer replacement=null;
            Zone.TileState.OnCellChanged=(x,y)=>{if(x!=11 || y!=10 || Layer()==null)return;Zone.TileState.OnCellChanged=null;Zone.TileState.RemoveResidue(x,y,"grit");Zone.TileState.WriteResidue(x,y,"grit",12);replacement=Layer();};
            Assert.False(Act(grit,command));Assert.NotNull(replacement);Assert.AreSame(replacement,Layer());Assert.AreEqual(12,Layer().Turns);Assert.True(Pack.Objects.Contains(grit));
        }
        [Test] public void ExistingShortGritIsRestoredWhenTheNewApplicationIsVetoed()
        {Zone.TileState.WriteResidue(11,10,"grit",3);var before=Layer();var grit=Grit();string command=East(grit);FailAfter();Assert.False(Act(grit,command));Assert.AreSame(before,Layer());Assert.AreEqual(3,Layer().Turns);}
        [Test] public void TractionDoesNotEraseOilIceOrAcid()
        {
            foreach(string id in new[]{"oil","ice","acid"})Zone.TileState.WriteCoating(11,10,id,30);
            var grit=Grit();Assert.True(Act(grit,East(grit)));foreach(string id in new[]{"oil","ice","acid"})Assert.AreEqual(30,Zone.TileState.CoatingTurns(11,10,id));
        }
        [Test] public void TheSameResidueSteadiesAnOrdinaryNpcThroughTheSharedSlipRule()
        {
            var grit=Grit();Assert.True(Act(grit,East(grit)));Zone.TileState.WriteCoating(11,10,"oil",30);
            var npc=Place("Villager");Assert.True(npc.HasTag("Creature"));Assert.True(LiquidSlipSystem.HasGrit(Zone,11,10));
            Assert.IsNull(LiquidSlipSystem.FindSlipperyLiquid(Zone,Zone.GetCell(11,10)));Assert.False(LiquidSlipSystem.ResolveAfterMove(npc,Zone,Zone.GetCell(11,10)));
            Zone.TileState.RemoveResidue(11,10,"grit");Assert.NotNull(LiquidSlipSystem.FindSlipperyLiquid(Zone,Zone.GetCell(11,10)));
        }
        [Test] public void CommodityAndItsRemainingQuantitySurviveNativeSaveGraph()
        {var grit=Grit(3);Assert.True(Act(grit,East(grit)));var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(grit);Assert.True(saved.Parts.Any(p=>p.Name=="GritMaterial"));Assert.AreEqual(2,saved.GetPart<StackerPart>().StackCount);Assert.AreEqual(ItemCategory.Materials,ItemCategory.GetCategory(saved));}
        [Test] public void LateIndependentMovementIsPreservedWhenTheApplicationRefuses()
        {
            var grit=Grit();string command=East(grit);Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Assert.True(Zone.MoveEntity(Actor,10,11))});
            Assert.False(Act(grit,command));Assert.AreSame(Zone.GetCell(10,11),Zone.GetEntityCell(Actor));Assert.True(Pack.Objects.Contains(grit));Assert.IsNull(Layer());
        }
        [Test] public void SuccessfulUseAndStaleRetryPublishDistinctDiagnostics()
        {
            var grit=Grit(2);string command=East(grit);Diag.ResetAll();Assert.True(Act(grit,command));Assert.False(Act(grit,command));
            Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="event",Kind="GritMaterialUsed",Limit=20}).Records.Count);
            Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="event",Kind="GritMaterialRejected",Limit=20}).Records.Count);
        }
        [Test] public void GritExpiresThroughTheExistingWorldTurnPolicy()
        {var grit=Grit();Assert.True(Act(grit,East(grit)));for(int i=0;i<12;i++)Zone.TileState.Tick();Assert.False(LiquidSlipSystem.HasGrit(Zone,11,10));}
        [Test] public void FreshMenderHasTwoFiniteGritAndUnchangedFourClay()
        {var mender=Mender();Assert.AreEqual(2,Stock(mender,"CoarseGrit"));Assert.AreEqual(4,Stock(mender,"FireClay"));}
        [Test] public void OrdinaryMenderRestockDoesNotRenewTheOpeningGrit()
        {
            var mender=Mender();Assert.AreEqual(2,Stock(mender,"CoarseGrit"));var pack=mender.GetPart<InventoryPart>();foreach(var e in pack.Objects.ToArray())pack.RemoveObject(e);
            Assert.True(Zone.AddEntity(mender,12,10));TraderRestockSystem.RestockZone(Zone,1000);Assert.AreEqual(0,Stock(mender,"CoarseGrit"));Assert.AreEqual(4,Stock(mender,"FireClay"));
        }
        [Test] public void LoadingAnOldMenderNeverSuppliesNewOpeningStock()
        {
            var mender=Mender();foreach(var e in mender.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="CoarseGrit").ToArray())mender.GetPart<InventoryPart>().RemoveObject(e);
            var part=mender.GetPart<TraderPart>();part.OpeningStockBlueprint="";part.OpeningStockUnits=0;part.OpeningStockSupplied=false;
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(mender);Assert.AreEqual(0,Stock(saved,"CoarseGrit"));Assert.AreEqual(4,Stock(saved,"FireClay"));
        }
        [Test] public void FactorylessMenderRetainsOrdinaryFallbackStock()
        {TraderPart.Factory=null;var mender=Mender();Assert.AreEqual(0,Stock(mender,"CoarseGrit"));Assert.AreEqual(4,Stock(mender,"FireClay"));Assert.AreEqual(300,TradeSystem.GetDrams(mender));}
    }
}
