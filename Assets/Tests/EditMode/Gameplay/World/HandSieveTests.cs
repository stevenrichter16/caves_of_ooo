using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SieveWorkMetadataProbe : Part
    {
        public Entity Tool,Station,Provider;public int Count;
        public override bool HandleEvent(GameEvent e)
        {if(e.ID=="BeforeMaterialProcess"){Tool=e.GetParameter<Entity>("Tool");Station=e.GetParameter<Entity>("Station");Provider=e.GetParameter<Entity>("WorkProvider");Count++;}return true;}
    }
    public sealed class HandSieveTests : FiftyWorldFixture
    {
        EntityFactory oldForge;
        Dictionary<string,LootTableData> oldLoot;bool oldLootReady;
        static readonly FieldInfo LootRows=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);
        static readonly FieldInfo LootReady=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void ReadySieve()
        {
            oldForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            oldLoot=new Dictionary<string,LootTableData>((Dictionary<string,LootTableData>)LootRows.GetValue(null));oldLootReady=LootTableRegistry.IsInitialized;
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));
            foreach(var cell in Zone.Cells){cell.IsVisible=true;cell.Explored=true;}
        }
        [TearDown] public void RestoreSieve()
        {
            ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();
            var rows=(Dictionary<string,LootTableData>)LootRows.GetValue(null);rows.Clear();foreach(var pair in oldLoot)rows[pair.Key]=pair.Value;LootReady.SetValue(null,oldLootReady);
        }
        Entity Feed()=>Carry("QuarrySweepings");
        Entity Sieve()=>Carry("HandSieve");
        string Screen(Entity packet)=>Choice(packet,"MaterialProcess|screen_quarry_sweepings|");
        string QuarryState(Entity packet)
        {
            var document = ExamineDocumentBuilder.BuildInventory(packet, Actor);
            Assert.NotNull(document);
            return document.Sections.FirstOrDefault(section => section.Heading == "Current state")?.Text ?? "";
        }

        [Test]
        public void QuarryPacketReadersExplainBothFractionsAndDiscardedMassWithoutASieve()
        {
            var packet = Feed();
            int tick = Clock.TickCount;
            string state = QuarryState(packet);
            string quick = packet.GetPart<ExaminablePart>().BuildExamineLine();

            StringAssert.Contains("1 pale-salt + 2 silver sand", state);
            StringAssert.Contains("1 pale-salt + 2 silver sand", quick);
            Assert.That(state, Does.Match(@"(?i)(2|two)\s*(lb\.?|pounds?)\s*of\s*(unusable\s*)?rock dust"));
            Assert.That(quick, Does.Match(@"(?i)(2|two)\s*(lb\.?|pounds?)\s*of\s*(unusable\s*)?rock dust"));
            Assert.That(Clock.TickCount, Is.EqualTo(tick));
            Assert.That(Count("HandSieve"), Is.Zero);
            Assert.That(Count("QuarrySweepings"), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuarryPacketReaderWithholdsDeclaredFractionsWhenATemplateDoesNotQualify(bool invalidOutput)
        {
            var packet = Feed();
            if (invalidOutput)
                Factory.Blueprints["SilverSand"].Parts["Physics"]["Weight"] = "invalid";
            else
                packet.GetPart<PhysicsPart>().Weight++;

            string state = QuarryState(packet);
            Assert.That(state, Does.Not.Contain("1 pale-salt + 2 silver sand"));
            Assert.That(state, Does.Not.Contain("rock dust"));
            Assert.That(Pack.Objects, Does.Contain(packet));
        }

        [Test]
        public void QuarryPacketReaderAgreesWithTheOpenMenuAndKeepsItsSelectionUsable()
        {
            var sieve = Sieve();
            var packet = Feed();
            var selection = Actions(packet).Single(row => row.Command.StartsWith("MaterialProcess|screen_quarry_sweepings|", StringComparison.Ordinal));
            string state = QuarryState(packet);

            StringAssert.Contains("1 pale-salt + 2 silver sand", state);
            StringAssert.Contains("1 pale-salt + 2 silver sand", selection.Display);
            Assert.True(Act(packet, selection.Command));
            Assert.That(Count("PaleSalt"), Is.EqualTo(1));
            Assert.That(Count("SilverSand"), Is.EqualTo(2));
            Assert.That(Pack.Objects, Does.Contain(sieve));
        }

        [Test] public void OrdinarySeparateMineralsRemainObtainableWithoutScreening()
        {Assert.NotNull(Carry("PaleSalt"));Assert.NotNull(Carry("SilverSand"));Assert.AreEqual(1,Count("PaleSalt"));Assert.AreEqual(1,Count("SilverSand"));}
        [Test] public void OnePacketYieldsItsTwoDeclaredFractionsAndKeepsTheSameSieve()
        {
            var tool=Sieve();var packet=Feed();Assert.True(Act(packet,Screen(packet)));
            Assert.AreEqual(0,Count("QuarrySweepings"));Assert.AreEqual(1,Count("PaleSalt"));Assert.AreEqual(2,Count("SilverSand"));
            Assert.True(Pack.Objects.Contains(tool));Assert.AreSame(Actor,tool.GetPart<PhysicsPart>().InInventory);
        }
        [Test] public void MissingSieveIsAnHonestDisabledReasonNotAForgeRequirement()
        {
            var packet=Feed();var row=Actions(packet).Single(a=>a.Display.Contains("screen quarry sweepings"));
            StringAssert.StartsWith("Disabled:",row.Command);Assert.AreEqual(1,Count("QuarrySweepings"));
        }
        [Test] public void CuttingKnifeDoesNotPretendToBeAMeshScreen()
        {Carry("Dagger");var packet=Feed();Assert.False(Actions(packet).Any(a=>a.Command.StartsWith("MaterialProcess|screen_quarry_sweepings|")));}
        [TestCase("broken")][TestCase("frozen")]
        public void UnusableScreenCannotProcessARealPacket(string mode)
        {
            var tool=Sieve();var packet=Feed();
            if(mode=="broken")Assert.True(tool.ApplyEffect(new BrokenEffect()));
            else {tool.SetTag("Organic");Assert.True(tool.ApplyEffect(new FrozenEffect(1)),"explicit freezable fixture must pass the existing material door");}
            Assert.False(Actions(packet).Any(a=>a.Command.StartsWith("MaterialProcess|screen_quarry_sweepings|")));
        }
        [Test] public void StaleToolQuoteDoesNotSpendInputOrCreateMinerals()
        {
            var tool=Sieve();var packet=Feed();var command=Screen(packet);Assert.True(Pack.RemoveObject(tool));
            Assert.False(Act(packet,command));Assert.True(Pack.Objects.Contains(packet));Assert.AreEqual(0,Count("PaleSalt"));
        }
        [Test] public void PhantomSameIdSieveIsNotAcceptedAsTheActualCarriedTool()
        {
            var tool=Sieve();var packet=Feed();var command=Screen(packet);Assert.True(Pack.RemoveObject(tool));
            var alias=Sieve();alias.ID=tool.ID;tool.GetPart<PhysicsPart>().InInventory=Actor;
            Assert.False(Act(packet,command));Assert.AreEqual(1,Count("QuarrySweepings"));Assert.True(Pack.Objects.Contains(alias));
        }
        [Test] public void RepeatedCurrentGathersRetainTheDisplayedMaterialChoice()
        {Sieve();var packet=Feed();var command=Screen(packet);Actions(packet);Assert.True(Act(packet,command));}
        [Test] public void InterruptedScreeningReturnsPacketAndRetractsOnlyItsOutputs()
        {
            Sieve();var packet=Feed();var command=Screen(packet);FailAfter();Assert.False(Act(packet,command));
            Assert.True(Pack.Objects.Contains(packet));Assert.AreEqual(0,Count("PaleSalt"));Assert.AreEqual(0,Count("SilverSand"));
        }
        [Test] public void VetoedScreeningPreservesAnIndependentActorInjury()
        {
            Sieve();var packet=Feed();var command=Screen(packet);int hp=Actor.GetStat("Hitpoints").BaseValue;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Actor.GetStat("Hitpoints").BaseValue-=3});FailAfter();
            Assert.False(Act(packet,command));Assert.AreEqual(hp-3,Actor.GetStat("Hitpoints").BaseValue);
            Assert.True(Pack.Objects.Contains(packet));Assert.AreEqual(0,Count("PaleSalt"));Assert.AreEqual(0,Count("SilverSand"));
        }
        [Test] public void AlreadyOverburdenedWeightReducingWorkRetainsExistingConvenience()
        {
            Sieve();var packet=Feed();var command=Screen(packet);Pack.MaxWeight=1;int before=Pack.GetCarriedWeight();
            Assert.True(Act(packet,command));Assert.Less(Pack.GetCarriedWeight(),before);
            Assert.AreEqual(0,Count("QuarrySweepings"));Assert.AreEqual(1,Count("PaleSalt"));Assert.AreEqual(2,Count("SilverSand"));
        }
        [Test] public void LateIndependentStockExceedingCapacityRefusesAndStaysIndependent()
        {
            Sieve();var packet=Feed();Pack.MaxWeight=9;var command=Screen(packet);Entity added=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{Pack.MaxWeight=200;added=Carry("SalvagedTimber");added.GetPart<StackerPart>().StackCount=10;Pack.MaxWeight=9;}});
            Assert.False(Act(packet,command));Assert.True(Pack.Objects.Contains(packet));Assert.True(Pack.Objects.Contains(added));
            Assert.AreEqual(10,added.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Count("PaleSalt"));Assert.AreEqual(0,Count("SilverSand"));
        }
        [Test] public void ScreeningPublishesTheSameSieveAsToolAndProviderNotForge()
        {
            var tool=Sieve();var packet=Feed();var probe=new SieveWorkMetadataProbe();Actor.AddPart(probe);
            Assert.True(Act(packet,Screen(packet)));Assert.AreEqual(1,probe.Count);Assert.AreSame(tool,probe.Tool);
            Assert.AreSame(tool,probe.Provider);Assert.IsNull(probe.Station);
        }
        [Test] public void AuthoredMassAndBaseValueDoNotMultiplyThroughScreening()
        {
            Sieve();var packet=Feed();int mass=packet.GetPart<PhysicsPart>().Weight,value=packet.GetPart<CommercePart>().Value;
            Assert.True(Act(packet,Screen(packet)));var products=Pack.Objects.Where(e=>e.BlueprintName=="PaleSalt"||e.BlueprintName=="SilverSand").ToArray();
            Assert.AreEqual(2,mass-products.Sum(e=>e.GetPart<PhysicsPart>().Weight*(e.GetPart<StackerPart>()?.StackCount??1)));
            Assert.GreaterOrEqual(value,products.Sum(e=>e.GetPart<CommercePart>().Value*(e.GetPart<StackerPart>()?.StackCount??1)));
        }
        [Test] public void ScreenedSandWorksInTheExistingTrailMarkSystem()
        {
            Bed(10,10,false);Sieve();var packet=Feed();Assert.True(Act(packet,Screen(packet)));
            var sand=Pack.Objects.First(e=>e.BlueprintName=="SilverSand");Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("water"))));
            Assert.AreEqual(1,Count("SilverSand"));Assert.True(Zone.GetCell(10,10).Objects.Any(e=>e.BlueprintName=="TrailMark"));
        }
        [Test] public void RealColdOreCacheStockingCanProduceFinitePackets()
        {
            int packets=0;
            for(int seed=0;seed<48;seed++)
            {
                var cache=Factory.CreateEntity("OreCache");LootStocker.StockContainer(cache,"OreCacheT1",Factory,new System.Random(seed));
                foreach(var packet in cache.GetPart<ContainerPart>().Contents.Where(e=>e.BlueprintName=="QuarrySweepings"))
                {Assert.AreSame(cache,packet.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(1,packet.GetPart<StackerPart>().StackCount);packets++;}
            }
            Assert.Greater(packets,0);Assert.Less(packets,48);
        }
        [Test] public void LoadingAnOldCacheDoesNotInventSweepingsStock()
        {
            var cache=Factory.CreateEntity("OreCache");var original=Factory.CreateEntity("PaleSalt");Assert.True(cache.GetPart<ContainerPart>().AddItem(original));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(cache);
            Assert.AreEqual(1,saved.GetPart<ContainerPart>().Contents.Count);Assert.AreEqual(original.ID,saved.GetPart<ContainerPart>().Contents.Single().ID);
        }
        [Test] public void OrdinaryForgeCanMakeTheReusableSieveFromExactStock()
        {
            Place("TinkersForge",9,9);var spike=Carry("IronSpikeComponent");
            Carry("SalvagedTimber").GetPart<StackerPart>().StackCount=2;Carry("KnotflaxCord").GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"MaterialProcess|make_hand_sieve|")));Assert.AreEqual(1,Count("HandSieve"));
            Assert.AreEqual(0,Count("IronSpikeComponent"));Assert.AreEqual(0,Count("SalvagedTimber"));Assert.AreEqual(0,Count("KnotflaxCord"));
        }
    }
}
