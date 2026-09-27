using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensitySteamSchedulingAdversarialTests
    {
        Zone zone; List<MaterialReactionBlueprint> saved; bool initialized;
        static FieldInfo Reactions=>typeof(MaterialReactionResolver).GetField("_reactions",BindingFlags.Static|BindingFlags.NonPublic);
        static FieldInfo Initialized=>typeof(MaterialReactionResolver).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        sealed class Probe:Part
        {
            public int Begins,Ends;public Action Before,After;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="BeginTakeAction"){Begins++;Before?.Invoke();}if(e.ID=="EndTurn"){Ends++;After?.Invoke();}return true;}
        }
        [SetUp] public void Setup()
        {zone=new Zone("steam-adversarial");saved=new List<MaterialReactionBlueprint>((List<MaterialReactionBlueprint>)Reactions.GetValue(null));initialized=(bool)Initialized.GetValue(null);MaterialReactionResolver.Initialize(null);}
        [TearDown] public void Cleanup(){Reactions.SetValue(null,saved);Initialized.SetValue(null,initialized);MessageLog.Clear();}
        Entity Add(string state,int x=10,bool creature=false)
        {
            var e=new Entity{BlueprintName="steam-probe"};e.AddPart(new Probe());e.AddPart(new MaterialPart{Combustibility=1,MaterialTagsRaw="Organic"});e.AddPart(new ThermalPart{AmbientDecayRate=0,FlameTemperature=9000});
            if(creature)e.SetTag("Creature");
            if(state=="steam"||state=="both")e.ApplyEffect(new SteamEffect(0.8f));
            if(state=="burning"||state=="both"){e.ApplyEffect(new BurningEffect(1,null,new System.Random(1)));Assert.IsTrue(e.HasEffect<BurningEffect>());}
            if(state=="wet")e.ApplyEffect(new WetEffect(0.5f));
            if(state=="thermal")e.GetPart<ThermalPart>().Temperature=100;
            Assert.IsTrue(zone.AddEntity(e,x,10));return e;
        }
        [TestCase("steam")] [TestCase("burning")] [TestCase("both")]
        public void Adversarial_ActivePropsReceiveExactlyOneStartAndEnd(string state)
        {var e=Add(state);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(1,e.GetPart<Probe>().Begins);Assert.AreEqual(1,e.GetPart<Probe>().Ends);}
        [TestCase("steam")] [TestCase("burning")] [TestCase("both")]
        public void Adversarial_CreatureSchedulerRemainsTheOnlyOwner(string state)
        {var e=Add(state,creature:true);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(0,e.GetPart<Probe>().Begins);Assert.AreEqual(0,e.GetPart<Probe>().Ends);}
        [TestCase("wet")] [TestCase("thermal")]
        public void Adversarial_NonSteamPassivePropsKeepTheirEndOnlyContract(string state)
        {var e=Add(state);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(0,e.GetPart<Probe>().Begins);Assert.AreEqual(1,e.GetPart<Probe>().Ends);}
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_DetachedPropsAreNotTicked(bool detached)
        {var e=Add("steam");if(detached)zone.RemoveEntity(e);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(detached?0:1,e.GetPart<Probe>().Begins);}
        [TestCase(true,"steam")] [TestCase(false,"steam")] [TestCase(true,"thermal")] [TestCase(false,"thermal")]
        public void Adversarial_RemovedSnapshotMemberNeverReceivesALaterTurn(bool remove,string state)
        {
            var earlier=Add("steam",8);var later=Add(state,10);if(remove)earlier.GetPart<Probe>().Before=()=>zone.RemoveEntity(later);
            MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(remove||state=="thermal"?0:1,later.GetPart<Probe>().Begins);Assert.AreEqual(remove?0:1,later.GetPart<Probe>().Ends);
        }
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_SelfRemovalAtStartCannotReceiveEndTurn(bool remove)
        {var e=Add("steam");if(remove)e.GetPart<Probe>().Before=()=>zone.RemoveEntity(e);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(1,e.GetPart<Probe>().Begins);Assert.AreEqual(remove?0:1,e.GetPart<Probe>().Ends);}
        [TestCase("0,0;1,0")] [TestCase("2,0")]
        public void Adversarial_PhysicalBodySizeDoesNotMultiplyTurnDispatch(string shape)
        {var e=Add("steam");zone.RemoveEntity(e);e.AddPart(new SpatialFootprintPart{CellsRaw=shape});Assert.IsTrue(zone.AddEntity(e,10,10));MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(1,e.GetPart<Probe>().Begins);Assert.AreEqual(1,e.GetPart<Probe>().Ends);}
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_NewlySpawnedSteamWaitsForTheNextSnapshot(bool create)
        {var e=Add("steam");Entity added=null;if(create)e.GetPart<Probe>().Before=()=>{if(added==null)added=Add("steam",15);};MaterialSimSystem.TickMaterialEntities(zone);if(create){Assert.AreEqual(0,added.GetPart<Probe>().Begins);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(1,added.GetPart<Probe>().Begins);}else Assert.AreEqual(1,e.GetPart<Probe>().Begins);}
        [TestCase(0.05f)] [TestCase(0.8f)]
        public void Adversarial_SteamExpiresAndStopsSchedulingWithoutThermalDisplacement(float density)
        {var e=Add("clear");e.ApplyEffect(new SteamEffect(density));for(int i=0;i<20;i++)MaterialSimSystem.TickMaterialEntities(zone);Assert.IsFalse(e.HasEffect<SteamEffect>());int count=e.GetPart<Probe>().Begins;MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(count,e.GetPart<Probe>().Begins);Assert.Greater(count,0);}
        [Test] public void Adversarial_NullZoneHasNoTurnSideEffects(){var e=Add("steam");Assert.DoesNotThrow(()=>MaterialSimSystem.TickMaterialEntities(null));Assert.AreEqual(0,e.GetPart<Probe>().Begins);}
        [Test] public void Adversarial_AmbientClearPropIsNotAdmitted(){var e=Add("clear");MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(0,e.GetPart<Probe>().Begins);Assert.AreEqual(0,e.GetPart<Probe>().Ends);}
    }
}
