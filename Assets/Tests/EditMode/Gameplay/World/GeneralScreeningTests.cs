using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GeneralScreeningTests : FiftyWorldFixture
    {
        const string Prefix="MaterialProcess|screen_size_grades|";
        EntityFactory oldForge;
        [SetUp] public void ReadyScreening(){oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}}
        [TearDown] public void RestoreScreening(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Feed(int n=1){var e=Carry("DryClaySpoil");e.GetPart<StackerPart>().StackCount=n;return e;}
        Entity Sieve()=>Carry("HandSieve");
        string Screen(Entity e)=>Choice(e,Prefix);
        int Held(string bp)=>Pack.Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        void Contents(string raw)=>Factory.Blueprints["DryClaySpoil"].Parts["SiftableStock"]["Contents"]=raw;
        static Part Stock(Entity e)=>e.Parts.Single(p=>p.Name=="SiftableStock");
        static void SetContents(Entity e,string raw){var p=Stock(e);p.GetType().GetField("Contents").SetValue(p,raw);}

        [Test] public void ARealSievePartitionsTheActualPacketAndRetainsTheSameTool()
        {
            var sieve=Sieve();var feed=Feed();int mass=Pack.GetCarriedWeight();Assert.True(Act(feed,Screen(feed)));
            Assert.AreEqual(0,Held("DryClaySpoil"));Assert.AreEqual(2,Held("FireClay"));Assert.AreEqual(1,Held("CoarseGrit"));
            Assert.True(Pack.Objects.Contains(sieve));Assert.AreEqual(mass,Pack.GetCarriedWeight());
        }
        [Test] public void DerivedScreeningNeverRegistersAPerBlueprintRecipe()
        {Sieve();var feed=Feed();Assert.IsNull(MaterialProcessRegistry.Get("screen_size_grades"));Assert.True(Act(feed,Screen(feed)));Assert.IsNull(MaterialProcessRegistry.Get("screen_size_grades"));}
        [Test] public void ASecondCanonicalMaterialLotUsesTheSameSizeRule()
        {
            Sieve();Feed();var source=Factory.Blueprints["DryClaySpoil"];var bp=new Blueprint{Name="KnownSaltGradingLot",Baked=true};
            foreach(var p in source.Parts)bp.Parts[p.Key]=new Dictionary<string,string>(p.Value);foreach(var p in source.Tags)bp.Tags[p.Key]=p.Value;
            bp.Parts["SiftableStock"]["Contents"]="PaleSalt:1:0;CoarseGrit:2:2";bp.Parts["Commerce"]["Value"]="16";Factory.Blueprints[bp.Name]=bp;
            var second=Carry(bp.Name);Assert.True(Act(second,Screen(second)));Assert.AreEqual(1,Held("PaleSalt"));Assert.AreEqual(2,Held("CoarseGrit"));Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void NoSieveLeavesTheLotAndItsOrdinaryTradeAlternativeIntact()
        {var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));Assert.Greater(feed.GetPart<CommercePart>().Value,0);}
        [Test] public void ACombatKnifeDoesNotPretendToBeAMesh()
        {Carry("Dagger");var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [Test] public void RepeatedCurrentMenuReadsRetainTheExactDisplayedQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);Actions(feed);Assert.True(Act(feed,command));Assert.AreEqual(2,Held("FireClay"));}
        [Test] public void AChangedLiveManifestCannotReuseTheOldQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);SetContents(feed,"FireClay:1:0;CoarseGrit:2:2");Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));}
        [Test] public void AChangedCanonicalManifestCannotReuseTheOldQuote()
        {Sieve();var feed=Feed();string command=Screen(feed);Contents("FireClay:1:0;CoarseGrit:2:2");Assert.False(Act(feed,command));Assert.AreEqual(0,Held("CoarseGrit"));}
        [Test] public void LostActualSieveRefusesWithoutSpendingThePacket()
        {var sieve=Sieve();var feed=Feed();string command=Screen(feed);Assert.True(Pack.RemoveObject(sieve));Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));}
        [Test] public void ExplicitProcessPermissionDenialWinsOverTheStockAdapter()
        {Sieve();var feed=Feed();feed.AddPart(new MaterialProcessInputPart{ProcessIds=""});Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [Test] public void UnknownPrivateStateCannotBeErasedByScreening()
        {Sieve();var feed=Feed();feed.AddPart(new GritPrivatePayloadProbe());Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(feed.HasPart<GritPrivatePayloadProbe>());}
        [Test] public void ActiveBrokenStateRefusesAndStaysAttached()
        {Sieve();var feed=Feed();Assert.True(feed.ApplyEffect(new BrokenEffect()));Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(feed.HasEffect<BrokenEffect>());}
        [TestCase("FireClay:2:1;CoarseGrit:1:1")]
        [TestCase("FireClay:2:0;CoarseGrit:1:1")]
        [TestCase("FireClay:2:2;CoarseGrit:1:2")]
        [TestCase("FireClay:1:0;FireClay:2:2")]
        public void OverlapOrAnUnchangedSingleGradeDoesNotPretendToSeparateIdentities(string contents)
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents(contents);var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));}
        [TestCase("")][TestCase("FireClay:2.5:0;CoarseGrit:1:2")]
        [TestCase("FireClay:0:0;CoarseGrit:1:2")][TestCase("FireClay:-1:0;CoarseGrit:1:2")]
        [TestCase("FireClay:64:0;CoarseGrit:1:2")][TestCase("FireClay:2:3;CoarseGrit:1:2")]
        [TestCase("DryClaySpoil:2:0;CoarseGrit:1:2")][TestCase("UnknownMineral:2:0;CoarseGrit:1:2")]
        public void MalformedUnboundedOrSelfProducingStockHasNoExecutableQuote(string contents)
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents(contents);var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));Assert.True(Pack.Objects.Contains(feed));}
        [Test] public void AManifestCannotCreateMoreMassThanTheAuthoredPacket()
        {Sieve();Assert.True(Factory.Blueprints.ContainsKey("DryClaySpoil"));Contents("FireClay:3:0;CoarseGrit:1:2");var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));}
        [TestCase("FireClay")][TestCase("DryClaySpoil")]
        public void CanonicalHandlingWeightMustAlsoBalance(string blueprint)
        {
            Sieve();Factory.Blueprints[blueprint].Parts["Handling"]=new Dictionary<string,string>{{"Weight","100"}};
            var feed=Feed();Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)),"The actual handling mass must balance for source and products.");
        }
        [Test] public void TwoPacketsRemainFiniteAcrossTwoPasses()
        {
            Sieve();var feed=Feed(2);Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(1,feed.GetPart<StackerPart>().StackCount);
            Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(0,Held("DryClaySpoil"));Assert.AreEqual(4,Held("FireClay"));Assert.AreEqual(2,Held("CoarseGrit"));
            Assert.False(Actions(feed).Any(a=>a.Command.StartsWith(Prefix)));
        }
        [Test] public void SplittingStockPreservesItsManifestAndTotalYield()
        {
            Sieve();var feed=Feed(2);var split=feed.GetPart<StackerPart>().SplitStack(1);Assert.NotNull(split);
            Assert.AreEqual(Stock(feed).GetType().GetField("Contents").GetValue(Stock(feed)),Stock(split).GetType().GetField("Contents").GetValue(Stock(split)));
            Assert.True(Pack.AddObject(split));Assert.AreEqual(2,Held("DryClaySpoil"));Assert.True(Act(feed,Screen(feed)));Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void PrivatePayloadCannotBeDroppedWhenPermissionIsRemovedBeforeSplitting()
        {
            var feed=Feed(2);feed.RemovePart(Stock(feed));feed.AddPart(new GritPrivatePayloadProbe());
            Assert.False(feed.GetPart<StackerPart>().CanSplit());Assert.IsNull(feed.GetPart<StackerPart>().SplitStack(1));Assert.AreEqual(2,feed.GetPart<StackerPart>().StackCount);
        }
        [Test] public void VetoReturnsTheInputAndRetractsItsProducts()
        {Sieve();var feed=Feed();string command=Screen(feed);FailAfter();Assert.False(Act(feed,command));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));Assert.AreEqual(0,Held("CoarseGrit"));}
        [Test] public void VetoDoesNotHealIndependentInjury()
        {
            Sieve();var feed=Feed();string command=Screen(feed);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Actor.GetStat("Hitpoints").BaseValue-=3});FailAfter();
            Assert.False(Act(feed,command));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);Assert.AreEqual(1,Held("DryClaySpoil"));
        }
        [Test] public void LateCapacityLossPreservesIndependentStockAndRefusesOutputs()
        {
            Sieve();var feed=Feed();int initial=Pack.GetCarriedWeight();Pack.MaxWeight=initial;string command=Screen(feed);Entity added=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Pack.MaxWeight=200;added=Carry("SalvagedTimber");Pack.MaxWeight=initial;}});
            Assert.False(Act(feed,command));Assert.True(Pack.Objects.Contains(added));Assert.AreEqual(1,Held("DryClaySpoil"));Assert.AreEqual(0,Held("FireClay"));
        }
        [Test] public void NativeSaveRetainsTheActualManifestAndRemainingPacket()
        {Sieve();var feed=Feed(2);Assert.True(Act(feed,Screen(feed)));var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(feed);Assert.AreEqual(1,saved.GetPart<StackerPart>().StackCount);Assert.AreEqual(Stock(feed).GetType().GetField("Contents").GetValue(Stock(feed)),Stock(saved).GetType().GetField("Contents").GetValue(Stock(saved)));}
        [Test] public void LegacyQuarryScreeningKeepsItsExistingExactFractions()
        {Sieve();var feed=Carry("QuarrySweepings");Assert.True(Act(feed,Choice(feed,"MaterialProcess|screen_quarry_sweepings|")));Assert.AreEqual(1,Held("PaleSalt"));Assert.AreEqual(2,Held("SilverSand"));}
        [Test] public void SharedMaterialWorkRejectsALateRemainingSourceAlias()
        {
            Sieve();var feed=Carry("QuarrySweepings");feed.GetPart<StackerPart>().StackCount=2;string command=Choice(feed,"MaterialProcess|screen_quarry_sweepings|");Entity alias=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Assert.True(Pack.Objects.Remove(feed));alias=Factory.CreateEntity("QuarrySweepings");alias.ID=feed.ID;alias.GetPart<PhysicsPart>().InInventory=Actor;Pack.Objects.Add(alias);}});
            Assert.False(Act(feed,command),"A replacement with the same ID cannot stand in for the selected source.");
            Assert.True(Pack.Objects.Contains(alias),"Keep the independent callback's replacement.");
            Assert.False(Pack.Objects.Contains(feed),"Do not reinsert a conflicting saved identity during limited rollback.");
            Assert.AreEqual(1,Pack.Objects.Count(e=>e.ID==feed.ID));Assert.AreEqual(0,Held("PaleSalt"));Assert.AreEqual(0,Held("SilverSand"));
        }
    }
}
