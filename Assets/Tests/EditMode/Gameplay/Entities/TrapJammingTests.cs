using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class TrapJammingFixture
    {
        protected Entity Actor, Target;
        protected Zone Zone;
        protected Part Jam;
        protected const string Command = "JamTrap";
        protected InventoryPart Inventory => Actor.GetPart<InventoryPart>();
        protected static Type JamType => typeof(Entity).Assembly.GetType("CavesOfOoo.Core.TrapJammingPart");
        [SetUp] public void Setup()
        {
            Diag.ResetAll(); MessageLog.Clear(); Zone = new Zone("trap-jamming-tests");
            Actor = Make("Player", false); Actor.SetTag("Player"); Actor.SetTag("Creature");
            Actor.Statistics["Hitpoints"] = new Stat { BaseValue=100, Max=100, Owner=Actor };
            Actor.AddPart(new InventoryPart()); Actor.AddPart(new StatusEffectsPart());
            Assert.True(Zone.AddEntity(Actor, 4, 4)); SetTarget("SpikeTrap");
        }
        [TearDown] public void Cleanup() { Diag.ResetAll(); MessageLog.Clear(); }
        protected static Entity Make(string blueprint, bool portable)
        {
            var e = new Entity { ID=Guid.NewGuid().ToString("N"), BlueprintName=blueprint };
            e.AddPart(new PhysicsPart { Takeable=portable }); e.AddPart(new RenderPart { DisplayName=blueprint }); return e;
        }
        protected void SetTarget(string blueprint, bool optIn=true)
        {
            if (Target != null) Zone.RemoveEntity(Target);
            Target = Make(blueprint, false);
            Target.AddPart(blueprint == "SpikeTrap" ? (Part)new SpikeTrapTriggerPart()
                : blueprint == "FireTrap" ? new FireTrapTriggerPart()
                : blueprint == "BearTrap" ? new BearTrapTriggerPart()
                : blueprint == "PressurePlate" ? new PressurePlateTriggerPart()
                : blueprint == "TripWire" ? new TripWireTriggerPart() : new RuneFlameTriggerPart());
            if (optIn) { Assert.NotNull(JamType, "The native trap-jamming Part must exist."); Jam=(Part)Activator.CreateInstance(JamType);Target.AddPart(Jam); }
            else Jam=null;
            Assert.True(Zone.AddEntity(Target,5,4)); var cell=Zone.GetEntityCell(Target); cell.IsVisible=true;cell.Explored=true;
            var standing=Zone.GetEntityCell(Actor);standing.IsVisible=true;standing.Explored=true;
        }
        protected Entity Supply(int count=2,string blueprint="SalvagedTimber")
        {var e=Make(blueprint,true);e.AddPart(new StackerPart { StackCount=count });Inventory.Objects.Add(e);e.GetPart<PhysicsPart>().InInventory=Actor;return e;}
        protected bool Act()=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(Target,Command),Actor,Zone).Success;
        protected bool Jammed=>Jam!=null && (bool)JamType.GetField("Jammed").GetValue(Jam);
        protected int Records(string kind)=>DiagQuery.Apply(new DiagQuery.Filter { Category="furniture",Kind=kind,Limit=100 }).Records.Count;
        protected bool Offer()=>WorldInteractionSystem.GatherActions(Target,Actor).Any(a=>a.Command==Command);
        protected bool Facade(Part part)=>(bool)JamType.GetMethod("TryJam").Invoke(part,new object[]{Actor,Zone});
    }
    public sealed class TrapJammingTests : TrapJammingFixture
    {
        [TestCase("SpikeTrap")][TestCase("FireTrap")][TestCase("BearTrap")][TestCase("PressurePlate")]
        public void OneRealTimberMakesCurrentTrapSafeWithoutRemovingIt(string blueprint)
        {
            SetTarget(blueprint);var timber=Supply();var original=Target;
            Assert.True(Offer());Assert.True(Act());Assert.True(Jammed);Assert.AreEqual(1,timber.GetPart<StackerPart>().StackCount);
            Assert.False(Offer());Assert.False(Act());Assert.AreEqual(1,timber.GetPart<StackerPart>().StackCount);
            Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.AreEqual(100,Actor.GetStatValue("Hitpoints"));
            Assert.False(Actor.HasEffect<BurningEffect>());Assert.False(Actor.HasEffect<StunnedEffect>());Assert.False(Actor.HasEffect<BleedingEffect>());
            Assert.AreSame(original,Target);Assert.AreSame(Zone.GetEntityCell(Actor),Zone.GetEntityCell(Target));
            Assert.AreEqual(1,Records("TrapJammed"));
        }
        [TestCase("SpikeTrap")][TestCase("FireTrap")][TestCase("BearTrap")][TestCase("PressurePlate")]
        public void SameUnjammedTrapRetainsItsOriginalDamageAndConsumption(string blueprint)
        {
            SetTarget(blueprint);Supply();Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));
            Assert.Less(Actor.GetStatValue("Hitpoints"),100);Assert.AreEqual(blueprint=="PressurePlate",Zone.GetEntityCell(Target)!=null);
            Assert.False(Jammed);Assert.AreEqual(0,Records("TrapJammed"));
        }
        [TestCase(1)][TestCase(3)] public void SingletonOrStackPaysExactlyOneWithCorrectBacklinks(int count)
        {
            var timber=Supply(count);Assert.True(Act());Assert.AreEqual(count>1,Inventory.Objects.Contains(timber));
            Assert.AreEqual(count>1?Actor:null,timber.GetPart<PhysicsPart>().InInventory);
            if(count>1)Assert.AreEqual(count-1,timber.GetPart<StackerPart>().StackCount);
        }
        [Test] public void RequirementIsDiscoverableWithoutMaterialButAttemptIsFree()
        {Assert.True(Offer());Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(0,Records("TrapJammed"));Assert.Greater(Records("TrapJamRejected"),0);}
        [Test] public void MissingMaterialCanBeFoundAndRetriedOnTheSameTrap()
        {Assert.False(Act());Supply(1);Assert.True(Act());Assert.True(Jammed);Assert.IsEmpty(Inventory.Objects);}
        [Test] public void JammedPlateStaysSafeAfterLeavingAndReentering()
        {SetTarget("PressurePlate");Supply();Assert.True(Act());for(int i=0;i<3;i++){Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.True(MovementSystem.TryMove(Actor,Zone,-1,0));}Assert.AreEqual(100,Actor.GetStatValue("Hitpoints"));Assert.NotNull(Zone.GetEntityCell(Target));}
        [TestCase("TripWire")][TestCase("RuneOfFlame")]
        public void UnsupportedTriggersDoNotBecomeMechanicalByAddingThePart(string blueprint)
        {SetTarget(blueprint);var timber=Supply();Assert.False(Offer());Assert.False(Act());Assert.False(Jammed);Assert.AreEqual(2,timber.GetPart<StackerPart>().StackCount);Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.Less(Actor.GetStatValue("Hitpoints"),100);}
        [TestCase("SpikeTrap")][TestCase("FireTrap")][TestCase("BearTrap")][TestCase("PressurePlate")]
        public void AuthoredBlueprintsOptIn(string blueprint)
        {var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));var entity=factory.CreateEntity(blueprint);Assert.True(entity.Parts.Any(p=>p.GetType()==JamType));}
        [TestCase(false)][TestCase(true)] public void NativeSaveRestoresExactOwnerAndSavedJam(bool jammed)
        {
            if(jammed){Supply();Assert.True(Act());}
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteEntityReference(Target);writer.WriteQueuedEntityBodies();stream.Position=0;
                var reader=new SaveReader(stream,null);var restored=reader.ReadEntityReference();reader.ReadEntityBodies();
                Assert.AreNotSame(Target,restored);var restoredPart=restored.Parts.Single(p=>p.GetType()==JamType);
                Assert.AreSame(restored,restoredPart.ParentEntity);Assert.AreEqual(jammed,JamType.GetField("Jammed").GetValue(restoredPart));
                Zone.RemoveEntity(Target);Target=restored;Jam=restoredPart;Assert.True(Zone.AddEntity(Target,5,4));
                Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.AreEqual(jammed,Actor.GetStatValue("Hitpoints")==100);
            }
        }
        [Test] public void LegacyTrapWithoutOptInKeepsPriorBehaviorThroughSave()
        {
            SetTarget("SpikeTrap",false);
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteEntityReference(Target);writer.WriteQueuedEntityBodies();stream.Position=0;
                var reader=new SaveReader(stream,null);var restored=reader.ReadEntityReference();reader.ReadEntityBodies();
                Assert.False(restored.Parts.Any(p=>p.GetType()==JamType));
                Zone.RemoveEntity(Target);Target=restored;Assert.True(Zone.AddEntity(Target,5,4));
            }
            Assert.False(Offer());Supply();Assert.False(Act());Assert.True(MovementSystem.TryMove(Actor,Zone,1,0));Assert.AreEqual(88,Actor.GetStatValue("Hitpoints"));
        }
    }
}
