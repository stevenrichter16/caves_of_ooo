using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class AtomicClayBakingTests : FiftyWorldFixture
    {
        EntityFactory prior;
        [SetUp] public void SetupBake()
        {
            prior=AlchemyStillPart.Factory;AlchemyStillPart.Factory=Factory;Pack.MaxWeight=400;
            Factory.RegisterPartType<BakingCreationProbe>();
            Factory.Blueprints["FireClay"].Parts[nameof(BakingCreationProbe)]=new Dictionary<string,string>();
            BakingCreationProbe.Callback=null;
        }
        [TearDown] public void CleanupBake() {BakingCreationProbe.Callback=null;AlchemyStillPart.Factory=prior;}
        Entity Still()=>Place("AlchemyStill");
        Entity Fire() {var e=Place("SpreadCookingCoals",9,10);e.GetPart<FuelPart>().FuelMass=25f;e.GetPart<ThermalPart>().Temperature=500f;return e;}
        string Bake(Entity s,Entity f)=>Actions(s).Single(a=>a.Name=="BakeClay"&&!a.IsDisabled&&a.Command.Contains("|"+f.ID+"|")).Command;
        int Clay()=>Pack.Objects.Where(e=>e.BlueprintName=="FireClay").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private void UseCanonicalClay()
        {
            // The creation probe is deliberately unsupported by the ordinary stack policy.
            Assert.IsTrue(Factory.Blueprints["FireClay"].Parts.Remove(nameof(BakingCreationProbe)));
        }
        [Test] public void NativeOneBatchKeepsTheRemainderAndPaysItsOwnFuel_Control()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");
            Assert.True(Act(s,Bake(s,f)));Assert.AreEqual(4,Units(f));Assert.AreEqual(1,Clay());Assert.AreEqual(15f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void CreationCallbackCannotBakeTheSameFeedTwice()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");string command=Bake(s,f);bool first=true,nested=false;
            BakingCreationProbe.Callback=e=>{if(!first)return;first=false;nested=Act(s,command);};
            Assert.True(Act(s,command));TestContext.WriteLine("bake receipt: nested="+nested+", clay="+Clay()+", mire="+Units(f)+", fuel="+fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(1,Clay());Assert.False(nested);Assert.AreEqual(4,Units(f));Assert.AreEqual(15f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void SharedTransactionReentryCannotClaimTheSameBatchTwice()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");string command=Bake(s,f);InventoryTransaction captured=null;bool first=true,nested=false;
            var spy=new BakingActionProbe {Capture=e=>captured=e.GetParameter<InventoryTransaction>("InventoryTransaction")};s.AddPart(spy);s.Parts.Remove(spy);s.Parts.Insert(0,spy);
            BakingCreationProbe.Callback=made=>{if(!first)return;first=false;var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)Actor);e.SetParameter("Zone",(object)Zone);e.SetParameter("Command",command);e.SetParameter("InventoryTransaction",(object)captured);s.FireEvent(e);nested=e.Handled;e.Release();};
            Assert.True(Act(s,command));TestContext.WriteLine("bake receipt: nested="+nested+", clay="+Clay()+", mire="+Units(f)+", fuel="+fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(1,Clay());Assert.False(nested);Assert.AreEqual(4,Units(f));Assert.AreEqual(15f,fire.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("liquid")][TestCase("volume")][TestCase("drop")][TestCase("fire-cold")][TestCase("fire-fuel")][TestCase("station-move")]
        public void FactoryCannotSpendAChangedOrDetachedInput(string change)
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");string command=Bake(s,f);
            BakingCreationProbe.Callback=e=>{
                switch(change){case "liquid":f.GetPart<LiquidVesselPart>().LiquidId="acid";break;
                    case "volume":f.GetPart<LiquidVesselPart>().Volume=2;break;
                    case "drop":Pack.RemoveObject(f);Zone.AddEntity(f,10,10);break;
                    case "fire-cold":fire.GetPart<ThermalPart>().Temperature=25f;break;
                    case "fire-fuel":fire.GetPart<FuelPart>().FuelMass=2f;break;
                    case "station-move":Zone.MoveEntity(s,16,10);break;}
            };
            Assert.False(Act(s,command));Assert.AreEqual(0,Clay());Assert.AreEqual(change=="volume"?2:12,Units(f));
            Assert.AreEqual(change=="liquid"?"acid":"bog-mire",f.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(change=="fire-fuel"?2f:25f,fire.GetPart<FuelPart>().FuelMass);
            if(change=="drop") {Assert.False(Pack.Objects.Contains(f));Assert.NotNull(Zone.GetEntityCell(f));}
            if(change=="station-move") Assert.AreEqual(16,Zone.GetEntityCell(s).X);
        }
        [Test] public void FactoryCannotMintAMultiUnitOutputForOneBill()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");string command=Bake(s,f);
            BakingCreationProbe.Callback=e=>e.GetPart<StackerPart>().StackCount=9;
            Assert.False(Act(s,command));Assert.AreEqual(0,Clay());Assert.AreEqual(12,Units(f));Assert.AreEqual(25f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void LateFuelChangeAndInjuryAreNotRewoundByRejectedBake()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");fire.GetPart<FuelPart>().FuelMass=20f;int hp=Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new BakingAfterProbe {After=()=>{fire.GetPart<FuelPart>().FuelMass+=2;Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("outer bake veto");}});
            Assert.False(Act(s,Bake(s,f)));Assert.AreEqual(12,Units(f));Assert.AreEqual(0,Clay());Assert.AreEqual(22f,fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));
        }
        [TestCase(false)][TestCase(true)] public void OutputRollbackRemovesOnlyItsOneUnit(bool existing)
        {
            UseCanonicalClay();
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");
            if(existing) {var old=Carry("FireClay");old.GetPart<StackerPart>().StackCount=5;}
            Actor.AddPart(new BakingAfterProbe {After=()=>{Pack.Objects.Single(e=>e.BlueprintName=="FireClay").GetPart<StackerPart>().StackCount+=2;throw new InvalidOperationException("preserve later clay");}});
            Assert.False(Act(s,Bake(s,f)));Assert.AreEqual(existing?7:2,Clay());Assert.AreEqual(12,Units(f));Assert.AreEqual(25f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void DroppedOutputWithIndependentStockIsNotPulledBack()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");Entity output=null;
            Actor.AddPart(new BakingAfterProbe {After=()=>{output=Pack.Objects.Single(e=>e.BlueprintName=="FireClay");output.GetPart<StackerPart>().StackCount+=2;Pack.RemoveObject(output);Zone.AddEntity(output,10,10);throw new InvalidOperationException("forced output drop");}});
            Assert.False(Act(s,Bake(s,f)));Assert.False(Pack.Objects.Contains(output));Assert.NotNull(Zone.GetEntityCell(output));Assert.AreEqual(2,output.GetPart<StackerPart>().StackCount);Assert.AreEqual(12,Units(f));
        }
        [Test] public void ReplacedOutputStackerIsNotOverwrittenOrRetired()
        {
            var s=Still();Fire();var f=Flask(12,"bog-mire");Entity output=null;StackerPart replacement=null;
            Actor.AddPart(new BakingAfterProbe {After=()=>{output=Pack.Objects.Single(e=>e.BlueprintName=="FireClay");output.RemovePart(output.GetPart<StackerPart>());replacement=new StackerPart {StackCount=8};output.AddPart(replacement);throw new InvalidOperationException("replacement output");}});
            Assert.False(Act(s,Bake(s,f)));Assert.True(Pack.Objects.Contains(output));Assert.AreSame(replacement,output.GetPart<StackerPart>());Assert.AreEqual(8,replacement.StackCount);Assert.AreEqual(12,Units(f));
        }
        [Test] public void ReplacedOutputPhysicsKeepsIndependentStockAndCustody()
        {
            var s=Still();Fire();var f=Flask(12,"bog-mire");Entity output=null;PhysicsPart replacement=null;
            Actor.AddPart(new BakingAfterProbe {After=()=>{output=Pack.Objects.Single(e=>e.BlueprintName=="FireClay");output.GetPart<StackerPart>().StackCount+=2;output.RemovePart(output.GetPart<PhysicsPart>());replacement=new PhysicsPart {Takeable=true,InInventory=Actor,Weight=1};output.AddPart(replacement);throw new InvalidOperationException("replacement physics");}});
            Assert.False(Act(s,Bake(s,f)));Assert.True(Pack.Objects.Contains(output));Assert.AreSame(replacement,output.GetPart<PhysicsPart>());Assert.AreSame(Actor,replacement.InInventory);Assert.AreEqual(2,output.GetPart<StackerPart>().StackCount);Assert.AreEqual(12,Units(f));
        }
        [TestCase(false)][TestCase(true)] public void FactoryCannotUseAnAlreadyOwnedOutput(bool ground)
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");string command=Bake(s,f);var other=Factory.CreateEntity("Player");Entity output=null;
            BakingCreationProbe.Callback=e=>{output=e;if(ground) Zone.AddEntity(e,30,10);else other.GetPart<InventoryPart>().AddObject(e);};
            Assert.False(Act(s,command));Assert.AreEqual(0,Clay());Assert.AreEqual(12,Units(f));Assert.AreEqual(25f,fire.GetPart<FuelPart>().FuelMass);
            if(ground) Assert.NotNull(Zone.GetEntityCell(output));else Assert.True(other.GetPart<InventoryPart>().Objects.Contains(output));
        }
        [TestCase(false)][TestCase(true)] public void ExactLastTenFuelPaysACompleteBatch(bool camp)
        {
            var s=Place(camp?"CampStill":"AlchemyStill");var fire=Place(camp?"CampHearth":"SpreadCookingCoals",9,10);
            if(camp) foreach(var e in new[]{s,fire}) {e.AddPart(new InstallablePart {Mode=InstallablePart.Portable,State=InstallablePart.Installed});e.AddPart(new InstallationMarkerPart {Origin=HeldFixtureActions.PlayerOrigin,InstallationId="player:"+e.ID,RetainZone=true});}
            fire.GetPart<ThermalPart>().Temperature=500f;fire.GetPart<FuelPart>().FuelMass=10f;var f=Flask(12,"bog-mire");
            Assert.True(Act(s,Bake(s,f)));Assert.AreEqual(1,Clay());Assert.AreEqual(4,Units(f));Assert.AreEqual(0f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void PostPaymentSourceDropAndIndependentFuelRemainOnRefusal()
        {
            var s=Still();var fire=Fire();fire.GetPart<FuelPart>().FuelMass=20f;var f=Flask(12,"bog-mire");int hp=Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new BakingAfterProbe {After=()=>{Pack.RemoveObject(f);Zone.AddEntity(f,10,10);fire.GetPart<FuelPart>().FuelMass+=2;Actor.GetStat("Hitpoints").BaseValue--;}});
            Assert.False(Act(s,Bake(s,f)));Assert.AreEqual(12,Units(f));Assert.AreEqual(22f,fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(0,Clay());Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));Assert.False(Pack.Objects.Contains(f));Assert.NotNull(Zone.GetEntityCell(f));
        }
        [Test] public void PostPaymentReplacementLiquidPartIsNotOverwritten()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");LiquidVesselPart replacement=null;
            Actor.AddPart(new BakingAfterProbe {After=()=>{f.RemovePart(f.GetPart<LiquidVesselPart>());replacement=new LiquidVesselPart {LiquidId="water",Volume=5,Capacity=12};f.AddPart(replacement);}});
            Assert.False(Act(s,Bake(s,f)));Assert.AreSame(replacement,f.GetPart<LiquidVesselPart>());Assert.AreEqual("water",replacement.LiquidId);Assert.AreEqual(5,replacement.Volume);Assert.AreEqual(0,Clay());Assert.AreEqual(25f,fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void PostPaymentReplacementFuelPartIsNotOverwritten()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");FuelPart replacement=null;
            Actor.AddPart(new BakingAfterProbe {After=()=>{fire.RemovePart(fire.GetPart<FuelPart>());replacement=new FuelPart {FuelMass=5f};fire.AddPart(replacement);}});
            Assert.False(Act(s,Bake(s,f)));Assert.AreSame(replacement,fire.GetPart<FuelPart>());Assert.AreEqual(5f,replacement.FuelMass);Assert.AreEqual(0,Clay());Assert.AreEqual(12,Units(f));
        }
        [Test] public void FactoryFailurePreservesIndependentInjuryAndReleasesActionLease()
        {
            var s=Still();var fire=Fire();var f=Flask(12,"bog-mire");int hp=Actor.GetStatValue("Hitpoints");string command=Bake(s,f);
            BakingCreationProbe.Callback=e=>{Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("creation interrupted");};
            Assert.False(Act(s,command));Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));Assert.AreEqual(12,Units(f));Assert.AreEqual(25f,fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(0,Clay());
            BakingCreationProbe.Callback=null;Assert.True(Act(s,command));Assert.AreEqual(1,Clay());Assert.AreEqual(4,Units(f));
        }
        [Test] public void ValidCompatibleOutputMergesAndBothInputsPay_Control()
        {
            UseCanonicalClay();
            var s=Still();var fire=Fire();var a=Flask(12,"bog-mire");var b=Flask(12,"bog-mire");
            Assert.True(Act(s,Bake(s,a)));Assert.True(Act(s,Bake(s,b)));Assert.AreEqual(2,Clay());Assert.AreEqual(4,Units(a));Assert.AreEqual(4,Units(b));Assert.AreEqual(5f,fire.GetPart<FuelPart>().FuelMass);Assert.AreEqual(1,Pack.Objects.Count(e=>e.BlueprintName=="FireClay"));Assert.AreEqual(2,Pack.Objects.Single(e=>e.BlueprintName=="FireClay").GetPart<StackerPart>().StackCount);
        }
    }
    public sealed class BakingCreationProbe : Part
    {
        public static Action<Entity> Callback;
        public override bool HandleEvent(GameEvent e) {if(e.ID=="ObjectCreated") Callback?.Invoke(ParentEntity);return true;}
    }
    public sealed class BakingActionProbe : Part
    {
        public Action<GameEvent> Capture;
        public override bool HandleEvent(GameEvent e) {if(e.ID=="InventoryAction")Capture?.Invoke(e);return true;}
    }
    public sealed class BakingAfterProbe : Part
    {
        public Action After;
        public override bool HandleEvent(GameEvent e) {if(e.ID=="AfterInventoryAction")After?.Invoke();return true;}
    }
}
