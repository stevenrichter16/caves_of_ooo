using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MeteredHydrationTests : FiftyWorldFixture
    {
        const string Prefix = "DrawMeteredWaterAtWell|";
        static FieldInfo Flag => typeof(WellPart).GetField("MeteredHydration");
        static void Meter(Entity well, bool value = true) => Flag?.SetValue(well.GetPart<WellPart>(), value);
        Entity Source(int stock = 3, bool pool = false)
        {
            var e = Place(pool ? "GroveSeep" : "Cistern"); e.GetPart<WellPart>().Stock = pool ? -1 : stock;
            if (pool) e.GetPart<LiquidPoolPart>().Volume = stock;
            Meter(e); return e;
        }
        void Thirst(int amount = 3) { if (amount > 0) Assert.True(Actor.ApplyEffect(new ParchedEffect { Stacks = amount })); }
        void Debt(int amount)
        {
            Assert.AreEqual(amount, Actor.GetEffect<ParchedEffect>()?.Stacks ?? 0);
            Assert.AreEqual(amount, Actor.GetStat("Strength").Penalty); Assert.AreEqual(amount, Actor.GetStat("Agility").Penalty);
        }
        InventoryAction Row(Entity well) => Actions(well).Single(a => a.Name == "Draw water");
        static string Quote(int units, int debt) => Prefix + units + "|" + debt;

        [TestCase(1,3,1)] [TestCase(2,3,2)] [TestCase(3,3,3)] [TestCase(7,2,2)] [TestCase(3,0,1)]
        public void FiniteDosesBatchOnlyTheNeededAvailableWater(int stock, int debt, int units)
        {
            var well = Source(stock); Thirst(debt); var row = Row(well);
            Assert.AreEqual(Quote(units,debt), row.Command); Assert.AreEqual(ActionTime.OneAction, row.Time);
            Assert.True(Act(well,row.Command)); Assert.AreEqual(stock-units,well.GetPart<WellPart>().Stock);
            Debt(Math.Max(0,debt-units));
        }
        [TestCase(false)] [TestCase(true)]
        public void TheSameFiniteWaterCostsTheSameForPlayerAndNonPlayer(bool player)
        {
            if(!player) Actor.Tags.Remove("Player");
            var well=Source(2);Thirst();Assert.True(Act(well,Quote(2,3)));Debt(1);Assert.AreEqual(0,well.GetPart<WellPart>().Stock);
        }
        [Test] public void PoolWaterIsFiniteEvenWhenTheWellsStockIsEndless()
        {
            var well=Source(2,true);Thirst();Assert.AreEqual(Quote(2,3),Row(well).Command);
            Assert.True(Act(well,Quote(2,3)));Debt(1);Assert.AreEqual(0,well.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(-1,well.GetPart<WellPart>().Stock);
            Assert.False(Actions(well).Any(a=>a.Name=="Draw water"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LegacyFiniteAndInfiniteRecoveryStayFreeFullAndConvenient(bool infinite)
        {
            var well=Source(infinite?-1:3);if(!infinite)Meter(well,false);Thirst();
            Assert.AreEqual("DrawWaterAtWell",Row(well).Command);Assert.AreEqual(ActionTime.Free,Row(well).Time);
            Assert.True(Act(well,Row(well).Command));Debt(0);Assert.AreEqual(infinite?-1:2,well.GetPart<WellPart>().Stock);
        }
        [Test] public void PartialAndPreventivePreviewsStateTheirRealFinitePriceAndRelief()
        {
            var well=Source(1);Thirst();var row=Row(well);
            StringAssert.Contains("1 water",row.Hint);StringAssert.Contains("1 of 3",row.Hint);
        }
        [Test] public void FillingThenThreeCarriedDrinksBuysTheSameThreeStacksOfRelief()
        {
            var well=Source();var skin=Skin(0);Thirst();
            Assert.True(Act(skin,WaterVesselService.FillSourcePrefix+Id(well)));Assert.AreEqual(0,well.GetPart<WellPart>().Stock);Assert.AreEqual(3,Units(skin));Debt(3);
            for(int n=2;n>=0;n--){Assert.True(Act(skin,"DrinkWaterskin"));Assert.AreEqual(n,Units(skin));Debt(n);}
        }
        [TestCase("opt-in")] [TestCase("opt-out")] [TestCase("finite")]
        [TestCase("infinite")] [TestCase("pool-added")] [TestCase("pool-removed")]
        public void StaleTariffRowsCannotRunUnderAnotherCostMode(string change)
        {
            bool startsFree=change=="opt-in"||change=="finite"||change=="pool-added";
            var well=Source(change=="finite"||change=="pool-added"?-1:3,change=="pool-removed");
            if(change=="opt-in")Meter(well,false);Thirst();var row=Row(well);
            if(change=="opt-in")Meter(well);if(change=="opt-out")Meter(well,false);
            if(change=="finite")well.GetPart<WellPart>().Stock=3;if(change=="infinite")well.GetPart<WellPart>().Stock=-1;
            if(change=="pool-added")well.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});
            if(change=="pool-removed")well.RemovePart(well.GetPart<LiquidPoolPart>());
            Assert.False(Act(well,row.Command));Debt(3);
            Assert.AreEqual(startsFree?ActionTime.OneAction:ActionTime.Free,Row(well).Time);
        }
        [TestCase("stock")] [TestCase("debt")]
        public void StaleQuantityOrReliefQuotesRefuseWithoutTakingWater(string change)
        {
            var well=Source();Thirst();var quote=Row(well).Command;
            if(change=="stock")well.GetPart<WellPart>().Stock=1;else ParchedEffect.ReduceOneStack(Actor);
            Assert.False(Act(well,quote));Assert.AreEqual(change=="stock"?1:3,well.GetPart<WellPart>().Stock);Debt(change=="stock"?3:2);
        }
        [Test] public void PartialRollbackAfterIndependentFullCureRestoresOnlyItsOneStack()
        {
            var well=Source(1);Thirst();
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID=="AfterInventoryAction"){Actor.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>();throw new InvalidOperationException("independent cure");}}});
            Assert.False(Act(well,Quote(1,3)));Debt(1);Assert.AreEqual(1,well.GetPart<WellPart>().Stock);
        }
        [Test] public void FinalRemovalCallbackExposureAndThrowKeepConsistentCappedPenalties()
        {
            var well=Source();Thirst();bool callback=false;
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID=="EffectRemoved"&&e.GetParameter<Effect>("Effect") is ParchedEffect&&!callback){callback=true;Actor.ApplyEffect(new ParchedEffect());throw new InvalidOperationException("new exposure during removal");}}});
            Assert.False(Act(well,Quote(3,3)));Assert.True(callback);Debt(3);Assert.AreEqual(3,well.GetPart<WellPart>().Stock);
        }
        [TestCase("frozen")] [TestCase("moved")] [TestCase("mode")]
        public void AfterActionSourceOrActorChangesMustNotCommitTheOldDraw(string change)
        {
            var well=Source();Thirst();
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID!="AfterInventoryAction")return;if(change=="frozen")Zone.TileState.WriteCoating(11,10,"ice",5);if(change=="moved")Assert.True(Zone.MoveEntity(Actor,9,10));if(change=="mode")Meter(well,false);}});
            Assert.False(Act(well,Quote(3,3)));Assert.AreEqual(3,well.GetPart<WellPart>().Stock);Debt(3);
            if(change=="moved")Assert.AreEqual(9,Zone.GetEntityCell(Actor).X);
        }
        [Test] public void TwoConsumersCannotSpendTheLastDoseTwice()
        {
            var well=Source(1);Thirst();var second=Factory.CreateEntity("Player");second.Tags.Remove("Player");Assert.True(Zone.AddEntity(second,10,11));second.ApplyEffect(new ParchedEffect{Stacks=3});
            Assert.True(Act(well,Quote(1,3)));Debt(2);
            Assert.False(InventorySystem.PerformAction(second,well,Quote(1,3),Zone));Assert.AreEqual(3,second.GetEffect<ParchedEffect>().Stacks);Assert.AreEqual(0,well.GetPart<WellPart>().Stock);
        }
        [TestCase(false)] [TestCase(true)]
        public void LiteralSaveRoundTripKeepsPolicyAndFiniteStock(bool enabled)
        {
            var well=Source(2);Meter(well,enabled);var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(well);
            Assert.NotNull(Flag,"the source policy is a saved public field");Assert.AreEqual(enabled,Flag.GetValue(copy.GetPart<WellPart>()));Assert.AreEqual(2,copy.GetPart<WellPart>().Stock);
        }
        [Test] public void LegacyFieldPayloadWithoutTheNewFlagDefaultsToFalse()
        {
            using(var stream=new MemoryStream())
            {
                var well=Source(2);const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
                typeof(SaveGraphSerializer).GetMethod("WritePublicFields",hidden).Invoke(null,new object[]{well.GetPart<WellPart>(),new SaveWriter(stream),(Func<FieldInfo,bool>)(f=>f.Name!="MeteredHydration")});
                stream.Position=0;var loaded=new WellPart();
                typeof(SaveGraphSerializer).GetMethod("ReadPublicFields",hidden).Invoke(null,new object[]{loaded,new SaveReader(stream,null)});
                Assert.NotNull(Flag);Assert.AreEqual(false,Flag.GetValue(loaded));Assert.AreEqual(2,loaded.Stock);
            }
        }
        [TestCase("valid")] [TestCase("stale")] [TestCase("after")]
        public void RealMenuChargesOneTurnOnlyForACommittedMeteredDraw(string condition)
        {
            using(var ui=new HotbarSaveFixture(true,false))
            {
                var well=Source();Thirst();var manager=OverworldZoneManager.CreateDetached(Factory,64,true);manager.SetActiveZone(Zone);
                var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=TurnManager.ActionThreshold}});
                ui.BindOld(GameSessionState.Capture("metered-input","normal-world-menu",manager,turns,Actor));var menu=ui.Root.AddComponent<WorldActionMenuUI>();ui.Input.WorldActionMenuUI=menu;
                const BindingFlags hidden=BindingFlags.Instance|BindingFlags.NonPublic;var state=typeof(InputHandler).GetField("_worldActionMenuReturnState",hidden);state.SetValue(ui.Input,Enum.Parse(state.FieldType,"Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor",hidden).Invoke(ui.Input,new object[]{well,Zone.GetEntityCell(well),false});
                var row=((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",hidden).GetValue(menu)).Single(a=>a.Name=="Draw water");
                if(condition=="stale")Meter(well,false);if(condition=="after")FailAfter();
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",hidden).Invoke(ui.Input,new object[]{row,well,Zone.GetEntityCell(well),false});
                Assert.AreEqual(condition=="valid"?27:17,turns.TickCount);Assert.AreEqual(condition=="valid"?0:3,well.GetPart<WellPart>().Stock);Debt(condition=="valid"?0:3);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedCommitReturnsItsDebitAlongsideAnIndependentSourceAddition(bool pool)
        {
            var well=Source(3,pool);Thirst();
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID!="AfterInventoryAction")return;if(pool)well.GetPart<LiquidPoolPart>().Volume++;else well.GetPart<WellPart>().Stock++;}});
            Assert.False(Act(well,Quote(3,3)));Debt(3);
            Assert.AreEqual(4,pool?well.GetPart<LiquidPoolPart>().Volume:well.GetPart<WellPart>().Stock);
        }
        [TestCase("zero")] [TestCase("status-owner")] [TestCase("effect-owner")]
        public void MalformedOrForeignDebtCannotAuthorizePartialRelief(string fault)
        {
            var well=Source(1);Thirst();var status=Actor.GetPart<StatusEffectsPart>();var thirst=Actor.GetEffect<ParchedEffect>();
            var foreign=Factory.CreateEntity("Player");
            if(fault=="zero")thirst.Stacks=0;if(fault=="status-owner")status.ParentEntity=foreign;if(fault=="effect-owner")thirst.Owner=foreign;
            Assert.False(Actions(well).Any(a=>a.Name=="Draw water"));
            Assert.False(Act(well,Quote(1,fault=="zero"?0:3)));Assert.AreEqual(1,well.GetPart<WellPart>().Stock);
            Assert.AreSame(thirst,Actor.GetEffect<ParchedEffect>());Assert.AreEqual(fault=="zero"?0:3,thirst.Stacks);
        }
        [Test] public void IsolatedShowcaseSuppliesFiniteTrialOwnersWithoutChangingAnExistingSource()
        {
            var ordinary=Place("Well",15,10);var skinBefore=Pack.Objects.Count(e=>e.HasPart<WaterskinPart>());
            new CavesOfOoo.Scenarios.Custom.MeteredHydrationShowcase().Apply(new CavesOfOoo.Scenarios.ScenarioContext(Zone,Factory,Actor,Clock));
            Debt(3);Assert.AreEqual(skinBefore+1,Pack.Objects.Count(e=>e.HasPart<WaterskinPart>()));
            Assert.AreEqual(-1,ordinary.GetPart<WellPart>().Stock);Assert.AreEqual(false,Flag.GetValue(ordinary.GetPart<WellPart>()));
            var sources=Zone.GetReadOnlyEntities().Where(e=>e!=ordinary&&e.HasPart<WellPart>()).ToArray();
            CollectionAssert.AreEquivalent(new[]{1,3},sources.Select(e=>e.GetPart<WellPart>().Stock));
            Assert.True(sources.All(e=>(bool)Flag.GetValue(e.GetPart<WellPart>())));
            Assert.AreEqual(0,Pack.Objects.Last(e=>e.HasPart<WaterskinPart>()).GetPart<WaterskinPart>().Charges);
        }
        [Test] public void ShowcaseWithNoRoomDoesNotGrantWaterOrThirst()
        {
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(dx!=0||dy!=0)Place("SandstoneWall",10+dx,10+dy);
            int owners=Zone.EntityCount,pack=Pack.Objects.Count;
            new CavesOfOoo.Scenarios.Custom.MeteredHydrationShowcase().Apply(new CavesOfOoo.Scenarios.ScenarioContext(Zone,Factory,Actor,Clock));
            Assert.AreEqual(owners,Zone.EntityCount);Assert.AreEqual(pack,Pack.Objects.Count);Debt(0);
        }
    }
}
