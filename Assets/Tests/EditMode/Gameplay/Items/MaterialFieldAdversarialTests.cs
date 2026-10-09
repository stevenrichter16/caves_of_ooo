using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MaterialFieldAdversarialTests : FiftyWorldFixture
    {
        [SetUp] public void Reveal() { foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true; Diag.ResetAll(); }
        string Pick(Entity item, string verb="Film", Entity target=null, string layer="oil", int x=11, int y=10)
            => Choice(item, "MaterialField|"+verb+"|", a => a.Command.Split('|')[5]==x.ToString() && a.Command.Split('|')[6]==y.ToString()
                && a.Command.Split('|')[7]==(target==null?"":Id(target)) && a.Command.Split('|')[9]==layer);
        static void Stack(Entity item) { if(!item.HasPart<StackerPart>()) item.AddPart(new StackerPart()); item.GetPart<StackerPart>().StackCount=3; }
        void Unspent(Entity item) { Assert.Contains(item,Pack.Objects); Assert.AreSame(Actor,item.GetPart<PhysicsPart>().InInventory); }

        [TestCase("verb")] [TestCase("zone")] [TestCase("origin")] [TestCase("distant")] [TestCase("overflow")]
        [TestCase("target")] [TestCase("amount")] [TestCase("layer")] [TestCase("missing-field")] [TestCase("extra-field")]
        public void Adversarial_ForgedSelectionCannotChangeAnUnrelatedMaterialOrLocation(string fault)
        {
            var item=Carry("LampOil"); Stack(item); var fields=Pick(item).Split('|');
            if(fault=="verb") fields[1]="Charge"; if(fault=="zone") fields[2]="other"; if(fault=="origin") fields[3]="9";
            if(fault=="distant") fields[5]="12"; if(fault=="overflow") fields[5]="2147483648";
            if(fault=="target") fields[7]=Id(Actor); if(fault=="amount") fields[8]="-1"; if(fault=="layer") fields[9]="acid";
            string command=string.Join("|",fields); if(fault=="missing-field") command=string.Join("|",fields.Take(9)); if(fault=="extra-field") command+="|1";
            Assert.False(Act(item,command)); Unspent(item); Assert.AreEqual(3,item.GetPart<StackerPart>().StackCount);
            Assert.False(Zone.TileState.HasCoating(11,10,"oil")); Assert.False(Zone.TileState.HasCoating(11,10,"acid"));
        }
        [TestCase("duplicate-id")] [TestCase("foreign-owner")] [TestCase("equipped")] [TestCase("ground-alias")]
        public void Adversarial_BrokenCarriedOwnershipCannotSpendTheSource(string fault)
        {
            var item=Carry("LampOil"); string command=Pick(item);
            if(fault=="duplicate-id") Carry("FrostLichen").ID=item.ID;
            if(fault=="foreign-owner") item.GetPart<PhysicsPart>().InInventory=new Entity();
            if(fault=="equipped") Pack.EquippedItems["alias"]=item;
            if(fault=="ground-alias") Zone.AddEntity(item,10,10);
            Assert.False(Act(item,command)); Assert.Contains(item,Pack.Objects); Assert.False(Zone.TileState.HasCoating(11,10,"oil"));
        }
        [TestCase("actor-move")] [TestCase("hidden")] [TestCase("blocked")] [TestCase("fresh-layer")]
        public void Adversarial_AfterActionWorldChangesRejectBeforeTheSupplyCommits(string change)
        {
            var item=Carry("LampOil"); string command=Pick(item);
            Actor.AddPart(new Hook("AfterInventoryAction",()=>{
                if(change=="actor-move") Zone.MoveEntity(Actor,9,10);
                if(change=="hidden") Zone.GetCell(11,10).IsVisible=false;
                if(change=="blocked") Place("StoneWall");
                if(change=="fresh-layer") Zone.TileState.WriteCoating(11,10,"oil",40);
                return true;
            }));
            Assert.False(Act(item,command)); Unspent(item);
            Assert.AreEqual(change=="fresh-layer"?40:0,Zone.TileState.CoatingTurns(11,10,"oil"));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_TargetMovementAfterActionCannotLeaveAFreeStickyCoat(bool honey)
        {
            var target=Place("MarlbackScrabbler"); int dv=target.GetStatValue("DV");
            var item=Carry(honey?"Honeycomb":"PitchpodResin"); string command=Pick(item,"Coat",target,honey?"honey":"pitch");
            Actor.AddPart(new Hook("AfterInventoryAction",()=>{Zone.MoveEntity(target,12,10);return true;}));
            Assert.False(Act(item,command)); Unspent(item); Assert.False(target.HasEffect<LiquidCoveredEffect>());
            Assert.AreEqual(dv,target.GetStatValue("DV")); Assert.False(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_CoatApplicationFailureReversesOnlyItsOwnStatsAndPreservesASeparateCondition(bool throwing)
        {
            var target=Place("MarlbackScrabbler"); int dv=target.GetStatValue("DV");
            var item=Carry("PitchpodResin"); string command=Pick(item,"Coat",target,"pitch"); var root=new RootedEffect(2);
            Assert.True(target.ApplyEffect(root));
            if(throwing) target.AddPart(new Hook("EffectApplied",()=>{throw new InvalidOperationException("coat observer");}));
            else target.AddPart(new Hook("BeforeApplyEffect",()=>false));
            Assert.False(Act(item,command)); Unspent(item); Assert.False(target.HasEffect<LiquidCoveredEffect>());
            Assert.AreEqual(dv,target.GetStatValue("DV")); Assert.AreSame(root,target.GetEffect<RootedEffect>());
        }
        [Test] public void Adversarial_PreApplyIndependentCoatIsNotMergedByTheRefundedSmear()
        {
            var target=Place("MarlbackScrabbler"); int dv=target.GetStatValue("DV");
            var item=Carry("PitchpodResin"); string command=Pick(item,"Coat",target,"pitch");
            var independent=new LiquidCoveredEffect("water",11);
            target.AddPart(new Hook("BeforeApplyEffect",()=>{Assert.True(target.ApplyEffect(independent));return true;}));
            Assert.False(Act(item,command)); Unspent(item);
            Assert.AreSame(independent,target.GetEffect<LiquidCoveredEffect>());
            Assert.AreEqual("water",independent.LiquidId); Assert.AreEqual(11,independent.Amount);
            Assert.AreEqual(dv,target.GetStatValue("DV")); Assert.False(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
        }
        [Test] public void Adversarial_ReentrantUseCannotConsumeTheClaimedSourceTwice()
        {
            var item=Carry("LampOil"); Stack(item); string command=Pick(item); bool nested=true;
            Actor.AddPart(new Hook("AfterInventoryAction",()=>{nested=Act(item,command);return true;}));
            Assert.True(Act(item,command)); Assert.False(nested); Assert.AreEqual(2,item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(8,Zone.TileState.CoatingTurns(11,10,"oil"));
        }
        [Test] public void Adversarial_UnrelatedAfterActionLayerSurvivesTheCommittedOilFilm()
        {
            var item=Carry("LampOil"); string command=Pick(item);
            Actor.AddPart(new Hook("AfterInventoryAction",()=>{Zone.TileState.WriteResidue(11,10,"grit",12);return true;}));
            Assert.True(Act(item,command)); Assert.True(Zone.TileState.HasCoating(11,10,"oil")); Assert.True(Zone.TileState.HasResidue(11,10,"grit"));
        }
        [Test] public void Adversarial_FailedWickNeverErasesOrResurrectsAnIndependentReplacement()
        {
            var item=Carry("PrismreedPith"); Zone.TileState.WriteCoating(11,10,"oil",5); string command=Pick(item,"Wick");
            Actor.AddPart(new Hook("AfterInventoryAction",()=>{Zone.TileState.RemoveCoating(11,10,"oil");Zone.TileState.WriteCoating(11,10,"oil",30);return true;}));
            Assert.False(Act(item,command)); Unspent(item); Assert.AreEqual(30,Zone.TileState.CoatingTurns(11,10,"oil"));
        }
        [Test] public void Adversarial_HiddenTargetAndBlockedUserCannotUseAVisibleOldMenu()
        {
            var item=Carry("PitchpodResin"); var target=Place("MarlbackScrabbler"); string command=Pick(item,"Coat",target,"pitch");
            Zone.GetCell(11,10).IsVisible=false; Assert.False(Act(item,command)); Unspent(item);
            Zone.GetCell(11,10).IsVisible=true; Actor.ApplyEffect(new StunnedEffect(2)); Assert.False(Act(item,command)); Unspent(item);
        }
        [Test] public void Adversarial_WideBodyUsesItsActualVisibleNearContactWithoutRemoteRetargeting()
        {
            var target=Factory.CreateEntity("MarlbackScrabbler"); target.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0"}); Assert.True(Zone.AddEntity(target,12,10));
            var item=Carry("PitchpodResin"); string command=Pick(item,"Coat",target,"pitch",12,10);
            Assert.True(Act(item,command)); Assert.NotNull(target.GetEffect<LiquidCoveredEffect>());
        }
        [Test] public void Adversarial_PartySmearDoesNotProvokeAndSavedCoatRetainsItsPenaltyExactlyOnce()
        {
            var target=Place("MarlbackScrabbler"); Assert.True(target.GetPart<BrainPart>().SetPartyLeader(Actor)); int dv=target.GetStatValue("DV");
            var item=Carry("Honeycomb"); Assert.True(Act(item,Pick(item,"Coat",target,"honey"))); Assert.False(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(target); Assert.AreEqual(dv-3,loaded.GetStatValue("DV"));
            Assert.AreEqual("honey",loaded.GetEffect<LiquidCoveredEffect>().LiquidId); loaded.GetPart<StatusEffectsPart>().RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(dv,loaded.GetStatValue("DV"));
        }
        [Test] public void Adversarial_OldSavedMaterialsRetainTheirNewActionsWithoutANewPart()
        {
            Carry("LampOil"); var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor); Zone.RemoveEntity(Actor); Actor=loaded; Assert.True(Zone.AddEntity(Actor,10,10));
            var item=Pack.Objects.Single(e=>e.BlueprintName=="LampOil"); Assert.True(Act(item,Pick(item))); Assert.True(Zone.TileState.HasCoating(11,10,"oil"));
        }
        sealed class Hook:Part
        {
            readonly string when; readonly Func<bool> callback; bool inside;
            public Hook(string when,Func<bool> callback){this.when=when;this.callback=callback;}
            public override bool HandleEvent(GameEvent e)
            {if(e.ID!=when||inside)return true;inside=true;try{return callback();}finally{inside=false;}}
        }
    }
}
