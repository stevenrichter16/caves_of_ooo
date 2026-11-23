using System;
using CavesOfOoo.Core.Inventory;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WaterskinTransactionTests : FiftyWorldFixture
    {
        Entity Well(bool pool=false)
        {var e=Place(pool?"GroveSeep":"Cistern");if(pool)e.GetPart<LiquidPoolPart>().Volume=3;return e;}
        void Thirst(int n=3){if(n>0)Assert.True(Actor.ApplyEffect(new ParchedEffect{Stacks=n}));}
        void Debt(int n){Assert.AreEqual(n,Actor.GetEffect<ParchedEffect>()?.Stacks??0);Assert.AreEqual(n,Actor.GetStat("Strength").Penalty);Assert.AreEqual(n,Actor.GetStat("Agility").Penalty);}
        void After(Action action)=>Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID=="AfterInventoryAction")action();}});
        static string Fill(Entity well)=>WaterVesselService.FillSourcePrefix+Uri.EscapeDataString(well.ID);

        [Test] public void IndependentCureAndOuterVetoRestoreOnlyTheOneRelievedLevel()
        {
            var skin=Skin(3);Thirst();After(()=>{Actor.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>();throw new InvalidOperationException("independent cure");});
            Assert.False(Act(skin,"DrinkWaterskin"));Debt(1);Assert.AreEqual(3,Units(skin));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ReplacementExposureSurvivesRollbackWithMatchingPenalties(int initial)
        {
            var skin=Skin(1);Thirst(initial);After(()=>{Actor.ApplyEffect(new ParchedEffect());throw new InvalidOperationException("new exposure");});
            Assert.False(Act(skin,"DrinkWaterskin"));Debt(Math.Min(3,initial+1));Assert.AreEqual(1,Units(skin));
        }
        [Test] public void FinalRemovalCallbackThrowRetainsItsReplacementExposure()
        {
            var skin=Skin(1);Thirst(1);bool once=false;
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID=="EffectRemoved"&&e.GetParameter<Effect>("Effect") is ParchedEffect&&!once){once=true;Actor.ApplyEffect(new ParchedEffect());throw new InvalidOperationException("removed callback");}}});
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.True(once);Debt(2);Assert.AreEqual(1,Units(skin));
        }
        [Test] public void IndependentHealthAndUnrelatedPenaltySurviveFailedDrink()
        {
            var skin=Skin(2);Thirst(2);int hp=Actor.GetStatValue("Hitpoints");
            After(()=>{Actor.GetStat("Hitpoints").BaseValue-=2;Actor.GetStat("Strength").Penalty+=4;throw new InvalidOperationException("independent injury");});
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.AreEqual(hp-2,Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(2,Actor.GetEffect<ParchedEffect>().Stacks);Assert.AreEqual(6,Actor.GetStat("Strength").Penalty);Assert.AreEqual(2,Actor.GetStat("Agility").Penalty);Assert.AreEqual(2,Units(skin));
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void FailedFillReturnsItsSourceDebitAlongsideIndependentWater(bool pool,bool throwAfter)
        {
            var well=Well(pool);var skin=Skin(0);
            After(()=>{if(pool)well.GetPart<LiquidPoolPart>().Volume++;else well.GetPart<WellPart>().Stock++;if(throwAfter)throw new InvalidOperationException("independent water");});
            Assert.False(Act(skin,Fill(well)));Assert.AreEqual(0,Units(skin));Assert.AreEqual(4,pool?well.GetPart<LiquidPoolPart>().Volume:well.GetPart<WellPart>().Stock);
        }
        [Test] public void RecipientCreditUndoPreservesValidIndependentWaterAndCapacity()
        {
            var well=Well();var skin=Skin(0);var p=skin.GetPart<WaterskinPart>();After(()=>{p.Capacity=5;p.Charges++;});
            Assert.False(Act(skin,Fill(well)));Assert.AreEqual(1,p.Charges);Assert.AreEqual(5,p.Capacity);Assert.AreEqual(3,well.GetPart<WellPart>().Stock);
        }
        [Test] public void DrinkDebitUndoPreservesValidIndependentWaterAndCapacity()
        {
            var skin=Skin(3);var p=skin.GetPart<WaterskinPart>();After(()=>{p.Capacity=5;p.Charges++;throw new InvalidOperationException("independent drink water");});
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.AreEqual(4,p.Charges);Assert.AreEqual(5,p.Capacity);
        }
        [TestCase(false)] [TestCase(true)]
        public void AFailedWaterActionDoesNotPullAnIndependentlyDroppedSkinBackIntoThePack(bool drink)
        {
            var well=Well();var skin=Skin(drink?3:0);Thirst();
            After(()=>{Assert.True(Pack.RemoveObject(skin));Assert.True(Zone.AddEntity(skin,12,10));});
            Assert.False(Act(skin,drink?"DrinkWaterskin":Fill(well)));
            Assert.False(Pack.Objects.Contains(skin));Assert.True(Zone.GetCell(12,10).Objects.Contains(skin));
            Assert.IsNull(skin.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(drink?3:0,Units(skin));Debt(3);Assert.AreEqual(3,well.GetPart<WellPart>().Stock);
        }
        [TestCase("actor-zone")] [TestCase("actor-physics")] [TestCase("actor-frozen")]
        [TestCase("actor-blocked")] [TestCase("skin-parent")] [TestCase("skin-physics")]
        [TestCase("pack-parent")] [TestCase("duplicate")] [TestCase("stack")]
        [TestCase("status-parent")] [TestCase("effect-owner")] [TestCase("zero-debt")]
        public void DirectServiceRefusesMalformedOrUnavailableParticipants(string fault)
        {
            var skin=Skin(2);Thirst();var foreign=Factory.CreateEntity("Player");Zone request=Zone;
            if(fault=="actor-zone")request=new Zone(Zone.ZoneID);
            if(fault=="actor-physics")Actor.GetPart<PhysicsPart>().ParentEntity=foreign;
            if(fault=="actor-frozen")Actor.ApplyEffect(new FrozenEffect(1));
            if(fault=="actor-blocked")Actor.ApplyEffect(new ParalyzedEffect());
            if(fault=="skin-parent")skin.GetPart<WaterskinPart>().ParentEntity=foreign;
            if(fault=="skin-physics")skin.GetPart<PhysicsPart>().InInventory=foreign;
            if(fault=="pack-parent")Pack.ParentEntity=foreign;
            if(fault=="duplicate")Pack.Objects.Add(skin);
            if(fault=="stack")skin.AddPart(new StackerPart{StackCount=2});
            if(fault=="status-parent")Actor.GetPart<StatusEffectsPart>().ParentEntity=foreign;
            if(fault=="effect-owner")Actor.GetEffect<ParchedEffect>().Owner=foreign;
            if(fault=="zero-debt")Actor.GetEffect<ParchedEffect>().Stacks=0;
            Assert.False(WaterVesselService.TryAct(Actor,skin,request,"DrinkWaterskin"));Assert.AreEqual(2,Units(skin));
        }
        [TestCase("moved")] [TestCase("frozen")] [TestCase("mixed")]
        [TestCase("broken")] [TestCase("part-owner")] [TestCase("replaced")]
        public void FillRevalidatesItsSourceAfterOuterCallbacks(string fault)
        {
            var well=Well();var skin=Skin(0);var original=well.GetPart<WellPart>();
            // Mixed ground affects an exposed pool, not the separate Stock supply.
            if(fault=="mixed")well.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});
            After(()=>{if(fault=="moved")Assert.True(Zone.MoveEntity(well,20,10));if(fault=="frozen")Zone.TileState.WriteCoating(11,10,"ice",5);if(fault=="mixed")Zone.TileState.WriteCoating(11,10,"oil",5);if(fault=="broken")well.AddPart(new RepairablePart());if(fault=="part-owner")original.ParentEntity=Factory.CreateEntity("Well");if(fault=="replaced"){well.RemovePart(original);well.AddPart(new WellPart{Stock=7});}});
            Assert.False(Act(skin,Fill(well)));Assert.AreEqual(0,Units(skin));Assert.AreEqual(3,original.Stock);
            if(fault=="mixed")Assert.AreEqual(3,well.GetPart<LiquidPoolPart>().Volume);
            if(fault=="replaced")Assert.AreEqual(7,well.GetPart<WellPart>().Stock);
        }
        [TestCase("moved")] [TestCase("replaced")] [TestCase("identity")]
        public void DrinkRevalidatesItsRecipientAfterOuterCallbacks(string fault)
        {
            var skin=Skin(2);var part=skin.GetPart<WaterskinPart>();Thirst();
            After(()=>{if(fault=="moved")Assert.True(Zone.MoveEntity(Actor,9,10));if(fault=="replaced"){skin.RemovePart(part);skin.AddPart(new WaterskinPart{Charges=1,Capacity=4});}if(fault=="identity")skin.ID="independent-new-id";});
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.AreEqual(2,part.Charges);Debt(3);
            if(fault=="identity")Assert.AreEqual("independent-new-id",skin.ID);
            if(fault=="replaced")Assert.AreEqual(1,skin.GetPart<WaterskinPart>().Charges);
        }
        [Test] public void UnsupportedConsumptionOfProvisionalFillCreditIsBoundedAndDiagnosed()
        {
            var well=Well();var skin=Skin(0);After(()=>skin.GetPart<WaterskinPart>().Charges=1);
            Assert.False(Act(skin,Fill(well)));Assert.AreEqual(0,Units(skin));Assert.AreEqual(3,well.GetPart<WellPart>().Stock);
            Assert.True(Diag.Snapshot(200).Any(r=>r.Kind=="WaterUndoBoundary"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ValidExhaustionAndInfiniteSourceFillsRemainAvailable(bool infinite)
        {
            var well=Well();if(infinite)well.GetPart<WellPart>().Stock=-1;var skin=Skin(0);
            Assert.True(Act(skin,Fill(well)));Assert.AreEqual(3,Units(skin));Assert.AreEqual(infinite?-1:0,well.GetPart<WellPart>().Stock);
        }
        [Test] public void PartialReliefAndChargesRoundTripTogether()
        {
            var skin=Skin(3);Thirst();Assert.True(Act(skin,"DrinkWaterskin"));Debt(2);
            var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);Assert.AreEqual(2,copy.GetEffect<ParchedEffect>().Stacks);
            Assert.AreEqual(2,copy.GetStat("Strength").Penalty);Assert.AreEqual(2,copy.GetPart<InventoryPart>().Objects.Single(e=>e.ID==skin.ID).GetPart<WaterskinPart>().Charges);
        }
        [TestCase(false)] [TestCase(true)]
        public void ForeignSpringPartCannotAuthorizeOrCommitAnUnlimitedFill(bool after)
        {
            var source=Place("PhysicalObject");source.GetPart<PhysicsPart>().Takeable=false;
            var spring=new TileStateSourcePart{Coating="water",CoatingTurns=10};source.AddPart(spring);
            var foreign=Factory.CreateEntity("Well");var skin=Skin(0);
            if(after)After(()=>spring.ParentEntity=foreign);else spring.ParentEntity=foreign;
            Assert.False(Act(skin,Fill(source)));Assert.AreEqual(0,Units(skin));Assert.AreSame(foreign,spring.ParentEntity);
        }
        [Test] public void UndoDoesNotStealAnOriginalEffectIndependentlyRehomedToAnotherActor()
        {
            var skin=Skin(1);Thirst(1);var original=Actor.GetEffect<ParchedEffect>();var other=Factory.CreateEntity("Player");Assert.True(Zone.AddEntity(other,15,15));bool moved=false;
            Actor.AddPart(new FiftyWorldEventProbe{Callback=e=>{if(e.ID=="EffectRemoved"&&e.GetParameter<Effect>("Effect")==original&&!moved){moved=true;Assert.True(other.ApplyEffect(original));throw new InvalidOperationException("independent effect recipient");}}});
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.True(moved);Debt(1);
            Assert.AreNotSame(original,Actor.GetEffect<ParchedEffect>());Assert.AreSame(original,other.GetEffect<ParchedEffect>());Assert.AreSame(other,original.Owner);
            Assert.AreEqual(1,other.GetStat("Strength").Penalty);Assert.AreEqual(1,other.GetStat("Agility").Penalty);Assert.AreEqual(1,Units(skin));
        }
        [TestCase("fill")] [TestCase("drink")] [TestCase("drop")]
        public void DifferentTransactionCannotConsumeOrDropTheClaimedSkin(string operation)
        {
            var well=Well();var skin=Skin(2);Thirst();bool nested=true;
            After(()=>nested=operation=="drop"?InventorySystem.Drop(Actor,skin,Zone):Act(skin,operation=="drink"?"DrinkWaterskin":Fill(well)));
            Assert.True(Act(skin,"DrinkWaterskin"));Assert.False(nested);Debt(2);Assert.AreEqual(1,Units(skin));Assert.AreEqual(3,well.GetPart<WellPart>().Stock);Assert.True(Pack.Objects.Contains(skin));
        }
        [Test] public void AnotherActorCannotFillFromTheClaimedLastUnit()
        {
            var well=Well();well.GetPart<WellPart>().Stock=1;var skin=Skin(0);
            var other=Factory.CreateEntity("Player");Assert.True(Zone.AddEntity(other,10,11));var otherSkin=Factory.CreateEntity("Waterskin");Assert.True(other.GetPart<InventoryPart>().AddObject(otherSkin));bool nested=true;
            After(()=>nested=InventorySystem.PerformAction(other,otherSkin,Fill(well),Zone));
            Assert.True(Act(skin,Fill(well)));Assert.False(nested);Assert.AreEqual(1,Units(skin));Assert.AreEqual(0,otherSkin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(0,well.GetPart<WellPart>().Stock);
        }
        [Test] public void SameTransactionFillThenDrinkHasNoAccidentalCompoundCommitContract()
        {
            var well=Well();var skin=Skin(0);Thirst();var tx=new InventoryTransaction();
            try
            {
                Assert.True(WaterVesselService.TryAct(Actor,skin,Zone,Fill(well),tx));Assert.True(WaterVesselService.TryAct(Actor,skin,Zone,"DrinkWaterskin",tx));
                Assert.Throws<InvalidOperationException>(()=>tx.Commit());tx.Rollback();Assert.AreEqual(0,Units(skin));Assert.AreEqual(3,well.GetPart<WellPart>().Stock);Debt(3);
            }
            finally{tx.Rollback();}
        }
        [TestCase(false)] [TestCase(true)]
        public void NamedLegacyHandlerWithoutCurrentSkinKeepsItsBlanketStatFallback(bool foreignPart)
        {
            var target=Place("PhysicalObject");if(foreignPart){var p=new WaterskinPart();target.AddPart(p);p.ParentEntity=Factory.CreateEntity("Well");}
            target.AddPart(new LegacySkinHandler());FailAfter();Assert.False(Act(target,"DrinkWaterskin"));Assert.AreEqual(0,Actor.GetStat("Strength").Penalty);
        }
        [Test] public void InvalidExternalCapacityShrinkIsBoundedAndDiagnosed()
        {
            var skin=Skin(3);After(()=>skin.GetPart<WaterskinPart>().Capacity=1);
            Assert.False(Act(skin,"DrinkWaterskin"));Assert.AreEqual(1,Units(skin));Assert.AreEqual(1,skin.GetPart<WaterskinPart>().Capacity);
            Assert.True(Diag.Snapshot(200).Any(r=>r.Kind=="WaterUndoBoundary"));
        }
        [Test] public void FailedFillPublishesNoOwnedWaterSuccessMessageOrDiagnostic()
        {
            var well=Well();var skin=Skin(0);MessageLog.Clear();bool old=Diag.IsChannelEnabled("event");Diag.SetChannel("event",true);
            try
            {
                var prior=Diag.Snapshot(200).Select(r=>r.TraceId).ToArray();After(()=>Zone.TileState.WriteCoating(11,10,"ice",5));
                Assert.False(Act(skin,Fill(well)));Assert.False(MessageLog.GetRecent(100).Any(s=>s.StartsWith("You fill the waterskin.",StringComparison.Ordinal)));
                Assert.False(Diag.Snapshot(200).Any(r=>!prior.Contains(r.TraceId)&&(r.Kind=="WaterskinFilled"||r.Kind=="WaterskinDrunk")));
            }
            finally{Diag.SetChannel("event",old);}
        }
        public sealed class LegacySkinHandler:Part
        {
            public override bool HandleEvent(GameEvent e)
            {if(e.ID!="InventoryAction"||e.GetStringParameter("Command")!="DrinkWaterskin")return true;e.GetParameter<Entity>("Actor").GetStat("Strength").Penalty+=4;e.Handled=true;return false;}
        }
        [Test] public void ADirectWellAlsoRefusesWhenAnOuterCallbackBlocksTheActor()
        {
            var well=Well();Thirst();After(()=>Actor.ApplyEffect(new ParalyzedEffect()));
            Assert.False(Act(well,"DrawWaterAtWell"));Debt(3);Assert.AreEqual(3,well.GetPart<WellPart>().Stock);Assert.True(Actor.HasEffect<ParalyzedEffect>());
        }
    }
}
