using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public abstract class SlotArmorComparisonFixture
    {
        protected EntityFactory Factory; protected Entity Actor; protected Body Body;
        protected InventoryPart Pack => Actor.GetPart<InventoryPart>();
        [SetUp] public void Setup() { Factory=new EntityFactory(); Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json"))); Actor=Factory.CreateEntity("Player");Body=Actor.GetPart<Body>(); }
        protected Entity Carry(string name) { var item=Factory.CreateEntity(name); Assert.True(Pack.AddObject(item));return item; }
        protected string Text(Entity item) { Assert.True(EquipmentComparisonService.TryDescribe(Actor,item,out string text,out string why),why);return text; }
    }
    public sealed class SlotArmorComparisonTests : SlotArmorComparisonFixture
    {
        [Test] public void CloakExplainsItsUntargetableBackWithoutGlobalArmorPromise()
        { string text=Text(Carry("Cloak")); StringAssert.Contains("back: not a normal hit location",text);StringAssert.DoesNotContain("AV change: +1",text);StringAssert.Contains("DV change: +1",text); }
        [TestCase("LeatherArmor","Body",3)] [TestCase("LeatherCap","Head",1)] [TestCase("GripfrondWrap","Handwear",1)]
        public void PhysicalSlotReportsOnlyItsActualArmorChange(string blueprint,string slot,int av)
        { var item=Carry(blueprint);var part=Body.GetPartsByType(slot)[0];StringAssert.Contains(part.GetDisplayName()+": AV 0 -> "+av+" (change: +"+av+")",Text(item));Assert.True(InventorySystem.Equip(Actor,item,part));Assert.AreEqual(av,CombatSystem.GetPartAV(Actor,part)-Actor.GetPart<ArmorPart>().AV); }
        [Test] public void AlreadyWornCloakStillExplainsItsCoverage()
        {var item=Carry("Cloak");Assert.True(InventorySystem.Equip(Actor,item));StringAssert.Contains("back: not a normal hit location",Text(item));}
        [Test] public void AlreadyWornPhysicalArmorNamesItsHitLocation()
        {var item=Carry("IronshodBoots");Assert.True(InventorySystem.Equip(Actor,item));StringAssert.Contains("feet: worn AV +2 applies only when this part is struck",Text(item));}
        [Test] public void TwoSlotReplacementReportsLostShieldProtectionOnItsActualHand()
        {var shield=Carry("Buckler");var hand=Body.GetPartsByType("Hand")[0];Assert.True(InventorySystem.Equip(Actor,shield,hand));StringAssert.Contains(hand.GetDisplayName()+": AV 1 -> 0 (change: -1)",Text(Carry("DissolutionMaul")));}
        [Test] public void BodylessComparisonKeepsHonestUnavailableSlots()
        {var item=Carry("Cloak");Actor.RemovePart(Body);StringAssert.Contains("Slot comparison unavailable",Text(item));}
    }
    public sealed class SlotArmorComparisonAdversarialTests : SlotArmorComparisonFixture
    {
        [TestCase(0,false)] [TestCase(5,true)] [TestCase(-1,false)]
        public void CurrentTargetWeightControlsCoverage(int weight,bool covered)
        {var part=Body.GetPartsByType("Back")[0];part.TargetWeight=weight;string text=Text(Carry("Cloak"));StringAssert.Contains(covered?"back: AV 0 -> 1":"back: not a normal hit location",text);}
        [Test] public void AbstractClaimNeverPromisesPhysicalProtection()
        {var part=Body.GetPartsByType("Head")[0];part.Abstract=true;string text=Text(Carry("LeatherCap"));StringAssert.DoesNotContain(part.GetDisplayName()+": AV 0 ->",text);}
        [Test] public void CoverageMetadataChangedByDescriptionCallbackRefusesStaleSnapshot()
        {var item=Carry("Cloak");item.AddPart(new Mutate{Change=()=>Body.GetPartsByType("Back")[0].TargetWeight=9});Assert.False(EquipmentComparisonService.TryDescribe(Actor,item,out _,out _));}
        [Test] public void NaturalArmorAndEffectsAreNotAddedToItemContribution()
        {Actor.GetPart<ArmorPart>().AV=7;var item=Carry("LeatherArmor");StringAssert.Contains("body: AV 0 -> 3",Text(item));Assert.AreEqual(7,Actor.GetPart<ArmorPart>().AV);}
        [Test] public void InspectionDoesNotEquipOrChangeStats()
        {var item=Carry("LeatherArmor");int speed=Actor.GetStatValue("Speed");Text(item);Assert.Contains(item,Pack.Objects);Assert.Null(Body.GetPartsByType("Body")[0]._Equipped);Assert.AreEqual(speed,Actor.GetStatValue("Speed"));}
        [Test] public void DisplacedMultiSlotArmorLosesCoverageOutsideCandidateClaim()
        {var old=Carry("Buckler");old.GetPart<EquippablePart>().UsesSlots="Hand,Hand";Assert.True(InventorySystem.Equip(Actor,old));var hands=Body.GetPartsByType("Hand");string text=Text(Carry("Dagger"));foreach(var hand in hands)StringAssert.Contains(hand.GetDisplayName()+": AV 1 -> 0",text);}
        [Test] public void ExtremeArmorDeltaDoesNotOverflow()
        {var old=Carry("LeatherCap");old.GetPart<ArmorPart>().AV=int.MinValue;Assert.True(InventorySystem.Equip(Actor,old));var item=Carry("LeatherCap");item.GetPart<ArmorPart>().AV=int.MaxValue;StringAssert.Contains("change: +4294967295",Text(item));}
        public sealed class Mutate:IItemEnhancement {public Action Change;public override string GetEffectDescription(){Change();return "probe";}}
    }
}
