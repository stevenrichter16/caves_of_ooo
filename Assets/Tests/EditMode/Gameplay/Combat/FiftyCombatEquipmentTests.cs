using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;
using S = CavesOfOoo.Tests.FiftyCombatStatusTests;

namespace CavesOfOoo.Tests
{
    public sealed class FiftyCombatEquipmentTests
    {
        [SetUp] public void Recipes()
        {
            RepairRecipeRegistry.ResetForTests();
            Assert.IsEmpty(RepairRecipeRegistry.InitializeFromJson("{\"Recipes\":[{\"Id\":\"metal-equipment-brace\",\"Composition\":\"Metal\",\"MaterialBlueprint\":\"SteelBladeComponent\",\"MaterialName\":\"steel blade component\",\"Quantity\":1,\"Diagnosis\":\"Bent working edge\",\"ActionText\":\"Repair equipment\",\"RepairedText\":\"The edge is sound\"}]}"));
        }
        [TearDown] public void Reset() { RepairRecipeRegistry.ResetForTests(); }
        static Entity Gear(bool optIn=true,int armor=3)
        {
            var e=F.Owner(); e.GetPart<PhysicsPart>().Takeable=true; e.BlueprintName="FixtureRepairSword";
            e.AddPart(new MeleeWeaponPart { HitBonus=7,BaseDamage="1d6",Attributes="Slashing" }); e.AddPart(new ArmorPart { AV=armor }); e.AddPart(new EquippablePart { Slot="Hand" });
            if(optIn)
            {
                e.AddPart(new CompositionPart { MaterialsRaw="Metal" }); var repair=new RepairablePart { RecipeId="metal-equipment-brace" };
                // Reflection keeps the RED fixture compilable before the small opt-in field exists.
                typeof(RepairablePart).GetField("PortableEquipment")?.SetValue(repair,true); e.AddPart(repair);
            }
            return e;
        }
        static Entity Actor(Zone z) { var a=F.Owner(creature:true); a.SetTag("Player"); a.AddPart(new InventoryPart()); F.Place(z,a,10,10); return a; }
        static Entity Supply(Entity actor,string blueprint="SteelBladeComponent",int quantity=2)
        {
            var e=F.Owner(); e.BlueprintName=blueprint; e.GetPart<PhysicsPart>().Takeable=true; e.AddPart(new StackerPart { StackCount=quantity }); Assert.True(actor.GetPart<InventoryPart>().AddObject(e)); return e;
        }
        [TestCase(true,3,5,2)] [TestCase(true,0,5,0)] [TestCase(false,3,7,3)]
        public void BrokenPenaltiesAreBoundedOptInAndRestoreOnce(bool optIn,int armor,int expectedHit,int expectedArmor)
        {
            var item=Gear(optIn,armor); Assert.True(item.ApplyEffect(new BrokenEffect())); Assert.True(item.ApplyEffect(new BrokenEffect()));
            Assert.AreEqual(expectedHit,item.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(expectedArmor,item.GetPart<ArmorPart>().AV);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(item);
            Assert.AreEqual(expectedHit,loaded.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(expectedArmor,loaded.GetPart<ArmorPart>().AV);
            loaded.GetPart<StatusEffectsPart>().RemoveEffect<BrokenEffect>(); Assert.AreEqual(7,loaded.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(armor,loaded.GetPart<ArmorPart>().AV);
        }
        [Test] public void HammerProcActuallyImpairsOptedInEquippedGear()
        {
            var z=new Zone(); var attacker=F.ArmedActor("Bludgeoning Cudgel"); var victim=F.ArmedActor("Slashing"); F.Place(z,attacker,10,10); F.Place(z,victim,11,10);
            var body=victim.GetPart<Body>(); foreach(var p in body.GetParts().Where(p=>p.Equipped!=null).ToArray()) victim.GetPart<InventoryPart>().UnequipFromBodyPart(p);
            var item=Gear(); victim.GetPart<InventoryPart>().EquipToBodyPart(item,body.GetParts().First(p=>p.Type=="Hand"));
            var damage=new Damage(1); damage.AddAttribute("Cudgel");
            new Cudgel_Hammer().OnAttackerAfterAttack(new SkillEventContext { Attacker=attacker,Defender=victim,Damage=damage,ActualDamage=1,Zone=z,Rng=new S.FixedRandom() });
            Assert.True(item.HasEffect<BrokenEffect>()); Assert.AreEqual(5,item.GetPart<MeleeWeaponPart>().HitBonus);
        }
        [Test] public void PortableRepairSpendsOneRealMaterialAndCanRepairLaterDamageAgain()
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(); Assert.True(a.GetPart<InventoryPart>().AddObject(gear)); var stock=Supply(a,quantity:3);
            for(int i=0;i<2;i++)
            {
                Assert.True(gear.ApplyEffect(new BrokenEffect(),a,z));
                Assert.True(InventorySystem.GetActions(a,gear).Exists(x=>x.Command==RepairablePart.RepairCommand));
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
                Assert.False(gear.HasEffect<BrokenEffect>()); Assert.AreEqual(7,gear.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(2-i,stock.GetPart<StackerPart>().StackCount);
            }
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success); Assert.AreEqual(1,stock.GetPart<StackerPart>().StackCount);
        }
        [TestCase("wrong-material")] [TestCase("not-broken")] [TestCase("stacked")] [TestCase("not-owned")] [TestCase("no-opt-in")] [TestCase("equipped")]
        public void InvalidPortableRepairRefusesWithoutSpendingMaterial(string invalid)
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(invalid!="no-opt-in"); Assert.True(a.GetPart<InventoryPart>().AddObject(gear));
            var stock=Supply(a,invalid=="wrong-material"?"SalvagedTimber":"SteelBladeComponent"); if(invalid!="not-broken")gear.ApplyEffect(new BrokenEffect(),a,z);
            if(invalid=="stacked")gear.AddPart(new StackerPart { StackCount=2 });
            if(invalid=="not-owned") { a.GetPart<InventoryPart>().Objects.Remove(gear); gear.GetPart<PhysicsPart>().InInventory=null; F.Place(z,gear,11,10); }
            if(invalid=="equipped") { var body=new Body(); a.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid()); a.GetPart<InventoryPart>().EquipToBodyPart(gear,body.GetParts().First(p=>p.Type=="Hand")); }
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success); Assert.AreEqual(2,stock.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(invalid!="not-broken",gear.HasEffect<BrokenEffect>());
        }
        [TestCase(2,true,1,true)] [TestCase(1,true,1,false)] [TestCase(2,false,1,false)] [TestCase(2,true,0,false)]
        public void ActualTwoHandAxeHasSixPercentWindowWithoutChangingOtherHits(int hands,bool equipped,int actualDamage,bool severed)
        {
            var z=new Zone(); var actor=Actor(z); var body=new Body(); actor.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid());
            var axe=Gear(false); axe.GetPart<MeleeWeaponPart>().Attributes="Axe"; axe.GetPart<EquippablePart>().UsesSlots=hands==2?"Hand,Hand":"Hand";
            axe.AddPart(new HandlingPart { GripType=hands==2?GripType.TwoHand:GripType.OneHand }); actor.GetPart<InventoryPart>().AddObject(axe);
            if(equipped)actor.GetPart<InventoryPart>().EquipToBodyParts(axe,body.GetParts().Where(p=>p.Type=="Hand").Take(hands).ToList());
            var target=F.ArmedActor("Piercing"); F.Place(z,target,11,10); int count=target.GetPart<Body>().GetParts().Count;
            var damage=new Damage(actualDamage); damage.AddAttribute("Axe");
            new Axe_Dismember().OnAttackerAfterAttack(new SkillEventContext { Attacker=actor,Defender=target,Weapon=axe.GetPart<MeleeWeaponPart>(),WeaponEntity=axe,Damage=damage,ActualDamage=actualDamage,Zone=z,Rng=new S.FixedRandom(4) });
            Assert.AreEqual(severed,target.GetPart<Body>().GetParts().Count<count);
        }
    }
}
