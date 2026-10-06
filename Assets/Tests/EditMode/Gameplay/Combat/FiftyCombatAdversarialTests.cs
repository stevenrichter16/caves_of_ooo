using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;
using S = CavesOfOoo.Tests.FiftyCombatStatusTests;

namespace CavesOfOoo.Tests
{
    /// <summary>Dedicated post-RED taxonomy: receipt rollback, reentry, physical
    /// authority, payload isolation, body holes, identity and replacement saves.</summary>
    public sealed class FiftyCombatAdversarialTests
    {
        EntityFactory factory;
        [OneTimeSetUp] public void Content() { factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json"))); }
        static Entity Actor(Zone zone)
        {
            var a=F.Owner(creature:true); a.SetTag("Player"); a.AddPart(new InventoryPart { MaxWeight=1000 }); F.Place(zone,a,5,10); return a;
        }
        Entity Carry(Entity a,string blueprint,int count=1)
        {
            var item=factory.CreateEntity(blueprint); Assert.NotNull(item); if(item.GetPart<StackerPart>()!=null)item.GetPart<StackerPart>().StackCount=count;
            Assert.True(a.GetPart<InventoryPart>().AddObject(item)); return item;
        }
        static Entity Flask(Entity actor, string heal="",string status="",int count=2)
        {
            var item=new Entity { ID=Guid.NewGuid().ToString("N"), BlueprintName="FixtureFlask" };
            item.AddPart(new PhysicsPart { Takeable=true,Weight=1 }); item.AddPart(new HandlingPart()); item.AddPart(new StackerPart { StackCount=count });
            item.AddPart(new TonicPart { Healing=heal }); if(status!="")item.AddPart(new StatusTonicPart { EffectName=status });
            Assert.True(actor.GetPart<InventoryPart>().AddObject(item)); return item;
        }
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
        static Entity Supply(Entity actor,string blueprint="SteelBladeComponent",int quantity=2)
        {
            var e=F.Owner(); e.BlueprintName=blueprint; e.GetPart<PhysicsPart>().Takeable=true; e.AddPart(new StackerPart { StackCount=quantity }); Assert.True(actor.GetPart<InventoryPart>().AddObject(e)); return e;
        }
        public sealed class ReexposeOnCure : Part
        {
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="EffectRemoved" && e.GetParameter<Effect>("Effect") is PoisonedEffect)
                { Calls++; if(Calls<3) ParentEntity.ApplyEffect(new PoisonedEffect()); }
                return true;
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedOuterTonicActionCannotCreateOrRefreshFiniteSurge(bool existing)
        {
            var z=new Zone(); var a=Actor(z); var tonic=Carry(a,"StrengthTonic",2); int before=a.GetStatValue("Strength");
            if(existing) { tonic.GetPart<TonicPart>().ApplyTo(a,a,z); S.Ready(a); S.Tick(a,z); }
            int duration=a.GetEffect<TonicStatSurgeEffect>()?.Duration??0;
            a.AddPart(new MaterialRepairTests.ThrowAfter());
            Assert.False(InventorySystem.PerformAction(a,tonic,"ApplyTonic",z));
            Assert.AreEqual(before+(existing?4:0),a.GetStatValue("Strength")); Assert.AreEqual(duration,a.GetEffect<TonicStatSurgeEffect>()?.Duration??0);
            Assert.AreEqual(2,tonic.GetPart<StackerPart>().StackCount);
        }
        [Test] public void AntidoteDoesNotConsumeNewExposureAddedByRemovalCallback()
        {
            var z=new Zone(); var a=Actor(z); a.ApplyEffect(new PoisonedEffect()); var observer=new ReexposeOnCure(); a.AddPart(observer);
            var tonic=Carry(a,"Antidote",2); Assert.True(InventorySystem.PerformAction(a,tonic,"ApplyTonic",z));
            Assert.AreEqual(1,observer.Calls); Assert.True(a.HasEffect<PoisonedEffect>()); Assert.AreEqual(1,tonic.GetPart<StackerPart>().StackCount);
        }
        [Test] public void DifferentStatSurgesCoexistAndExpireIndependently()
        {
            var z=new Zone(); var a=Actor(z); int speed=a.GetStatValue("Speed"), strength=a.GetStatValue("Strength");
            var quick=Carry(a,"SpeedTonic"); var strong=Carry(a,"StrengthTonic");
            Assert.True(InventorySystem.PerformAction(a,quick,"ApplyTonic",z)); S.Ready(a); for(int i=0;i<5;i++)S.Tick(a,z);
            Assert.True(InventorySystem.PerformAction(a,strong,"ApplyTonic",z)); S.Ready(a);
            for(int i=0;i<15;i++)S.Tick(a,z); Assert.AreEqual(speed,a.GetStatValue("Speed")); Assert.AreEqual(strength+4,a.GetStatValue("Strength"));
            for(int i=0;i<5;i++)S.Tick(a,z); Assert.AreEqual(strength,a.GetStatValue("Strength"));
        }
        public sealed class SplashArrival : Part
        {
            public Zone Zone; public Entity Newcomer; public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="EffectApplied" && e.GetParameter<Effect>("Effect") is WetEffect)
                { Calls++; if(Zone.GetEntityCell(Newcomer)==null) Assert.True(Zone.AddEntity(Newcomer,11,11)); }
                return true;
            }
        }
        [Test] public void SplashCaptureCannotAdmitCallbackArrivalsOrDoseBodyTwice()
        {
            var z=new Zone(); var a=Actor(z); var first=F.Owner("0,0;0,1",true); F.Place(z,first,10,9);
            var late=F.Owner(creature:true); var arrival=new SplashArrival { Zone=z,Newcomer=late }; first.AddPart(arrival);
            var tonic=Flask(a,status:"Wet"); Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(tonic,11,10),a,z).Success);
            Assert.True(first.HasEffect<WetEffect>()); Assert.AreEqual(1,arrival.Calls); Assert.NotNull(z.GetEntityCell(late)); Assert.False(late.HasEffect<WetEffect>());
        }
        [Test] public void SplashHonorsFootprintHolesAndDoesNotHealOutOfRangeAnchor()
        {
            var z=new Zone(); var a=Actor(z); var target=F.Owner("0,0;0,4",true); F.Place(z,target,12,8); target.GetStat("Hitpoints").BaseValue=500;
            // The body bounds cross the splash, but neither occupied contact does.
            var tonic=Flask(a,"1d1+6"); Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(tonic,11,10),a,z).Success);
            Assert.AreEqual(500,target.GetStatValue("Hitpoints"));
        }
        [Test] public void MixedSceneryPayloadAppliesOnlyElementalEffects()
        {
            var z=new Zone(); var a=Actor(z); var item=Flask(a,"1d1+6","Wet"); item.GetPart<TonicPart>().StatBoost="Strength:4";
            item.AddPart(new CureTonicPart { CureEffect="BrokenEffect" }); item.AddPart(new BrewItemPart { EffectsRaw="Poison:3;Frozen:1" });
            var prop=F.Owner(); prop.AddPart(new MaterialPart { MaterialTagsRaw="Metal" }); prop.AddPart(new ThermalPart());
            prop.Statistics["Strength"]=new Stat { Owner=prop,Name="Strength",BaseValue=10,Max=100 };
            prop.Statistics["Hitpoints"]=new Stat { Owner=prop,Name="Hitpoints",BaseValue=20,Max=100 };
            Assert.True(prop.ApplyEffect(new BrokenEffect())); F.Place(z,prop,12,10);
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,11,10),a,z).Success);
            Assert.True(prop.HasEffect<WetEffect>()); Assert.True(prop.HasEffect<FrozenEffect>()); Assert.True(prop.HasEffect<BrokenEffect>());
            Assert.False(prop.HasEffect<PoisonedEffect>()); Assert.AreEqual(10,prop.GetStatValue("Strength")); Assert.AreEqual(20,prop.GetStatValue("Hitpoints"));
        }
        [Test] public void ActualThrownPoisonFinishingTickAwardsItsThrowerNormalKillCredit()
        {
            var z=new Zone(); var a=Actor(z); a.Statistics["Experience"]=new Stat { Owner=a,Name="Experience",Max=100000 };
            var victim=F.Owner(creature:true); victim.GetStat("Hitpoints").BaseValue=1;
            victim.Statistics["XPValue"]=new Stat { Owner=victim,Name="XPValue",BaseValue=7,Max=100 };
            var witness=new S.DeathWitness(); victim.AddPart(witness); F.Place(z,victim,11,10);
            var poison=Flask(a,status:"Poison"); poison.GetPart<StatusTonicPart>().EffectDamageDice="1d1";
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(poison,11,10),a,z).Success);
            Assert.True(victim.HasEffect<PoisonedEffect>()); S.Tick(victim,z,"BeginTakeAction");
            Assert.AreSame(a,witness.Killer); Assert.AreEqual(1,witness.Deaths); Assert.AreEqual(7,a.GetStatValue("Experience"));
            Assert.AreEqual(1,poison.GetPart<StackerPart>().StackCount);
        }
        [Test] public void FailedOuterRepairRestoresMaterialOriginalEffectAndExactPenalty()
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(); Assert.True(a.GetPart<InventoryPart>().AddObject(gear)); var material=Supply(a);
            gear.ApplyEffect(new BrokenEffect(),a,z); var broken=gear.GetEffect<BrokenEffect>(); a.AddPart(new MaterialRepairTests.ThrowAfter());
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
            Assert.AreSame(broken,gear.GetEffect<BrokenEffect>()); Assert.AreSame(gear,broken.Owner); Assert.AreEqual(5,gear.GetPart<MeleeWeaponPart>().HitBonus);
            Assert.AreEqual(2,gear.GetPart<ArmorPart>().AV); Assert.AreEqual(2,material.GetPart<StackerPart>().StackCount); Assert.AreSame(a,material.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase("duplicate-list")] [TestCase("foreign-owner")] [TestCase("equipment-alias")] [TestCase("wrong-part-owner")] [TestCase("wrong-composition")]
        public void PortableRepairRejectsCorruptPhysicalAuthority(string corruption)
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(); var inv=a.GetPart<InventoryPart>(); Assert.True(inv.AddObject(gear)); var material=Supply(a); gear.ApplyEffect(new BrokenEffect(),a,z);
            if(corruption=="duplicate-list")inv.Objects.Add(gear);
            if(corruption=="foreign-owner")gear.GetPart<PhysicsPart>().InInventory=new Entity();
            if(corruption=="equipment-alias")inv.EquippedItems["corrupt"]=gear;
            if(corruption=="wrong-part-owner")gear.GetPart<PhysicsPart>().ParentEntity=a;
            if(corruption=="wrong-composition")gear.GetPart<CompositionPart>().MaterialsRaw="Wood";
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
            Assert.True(gear.HasEffect<BrokenEffect>()); Assert.AreEqual(5,gear.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(2,material.GetPart<StackerPart>().StackCount);
        }
        [Test] public void RepairedInventorySavePreservesPaidUnitsAndExactItemStats()
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(); Assert.True(a.GetPart<InventoryPart>().AddObject(gear)); Supply(a); gear.ApplyEffect(new BrokenEffect(),a,z);
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(a); var items=loaded.GetPart<InventoryPart>().Objects;
            var saved=items.Single(x=>x.BlueprintName==gear.BlueprintName); Assert.AreNotSame(gear,saved); Assert.AreSame(loaded,saved.GetPart<PhysicsPart>().InInventory);
            Assert.False(saved.HasEffect<BrokenEffect>()); Assert.AreEqual(7,saved.GetPart<MeleeWeaponPart>().HitBonus);
            Assert.AreEqual(1,items.Single(x=>x.BlueprintName=="SteelBladeComponent").GetPart<StackerPart>().StackCount);
        }
        [TestCase("Dagger","SteelBladeComponent")] [TestCase("LongSword","SteelBladeComponent")]
        [TestCase("Mace","SteelBladeComponent")] [TestCase("Battleaxe","SteelBladeComponent")]
        [TestCase("IronHelmet","SteelBladeComponent")] [TestCase("LeatherArmor","LeatherBindingComponent")] [TestCase("Cudgel","SalvagedTimber")]
        public void AuthoredCommonGearHasRealFiniteMaterialRepair(string blueprint,string supply)
        {
            RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults();
            var factory=new CavesOfOoo.Data.EntityFactory(); factory.LoadBlueprints(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var z=new Zone(); var a=Actor(z); var gear=factory.CreateEntity(blueprint); Assert.True(a.GetPart<InventoryPart>().AddObject(gear));
            var stock=factory.CreateEntity(supply); Assert.True(a.GetPart<InventoryPart>().AddObject(stock));
            Assert.True(gear.GetPart<RepairablePart>().PortableEquipment); Assert.False(RepairablePart.BlocksFunction(gear));
            Assert.True(gear.ApplyEffect(new BrokenEffect(),a,z)); Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
            Assert.False(gear.HasEffect<BrokenEffect>()); Assert.False(a.GetPart<InventoryPart>().Objects.Contains(stock));
        }
        [Test] public void ForcedZeroDurationDoesNotPretendBurnNaturallyCompleted()
        {
            var z=new Zone(); var a=F.Owner(creature:true); F.Place(z,a,10,10); a.ApplyEffect(new BurningEffect());
            a.GetEffect<BurningEffect>().Duration=0; S.Ready(a); S.Tick(a,z);
            Assert.False(a.HasEffect<BurningEffect>()); Assert.False(a.HasEffect<CharredEffect>());
        }
        public sealed class ImproveThenRejectRepair : Part
        {
            public Entity Gear;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="AfterInventoryAction")
                {
                    Gear.GetPart<MeleeWeaponPart>().HitBonus+=3;
                    Gear.GetPart<ArmorPart>().AV+=2;
                    throw new InvalidOperationException("Independent upgrade survives rejected repair");
                }
                return true;
            }
        }
        [Test] public void RepairUndoPreservesIndependentCallbackStatDeltas()
        {
            var z=new Zone(); var a=Actor(z); var gear=Gear(); Assert.True(a.GetPart<InventoryPart>().AddObject(gear)); var stock=Supply(a);
            gear.ApplyEffect(new BrokenEffect(),a,z); a.AddPart(new ImproveThenRejectRepair { Gear=gear });
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(gear,RepairablePart.RepairCommand),a,z).Success);
            Assert.True(gear.HasEffect<BrokenEffect>()); Assert.AreEqual(8,gear.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(4,gear.GetPart<ArmorPart>().AV);
            Assert.AreEqual(2,stock.GetPart<StackerPart>().StackCount);
            gear.GetPart<StatusEffectsPart>().RemoveEffect<BrokenEffect>(); Assert.AreEqual(10,gear.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreEqual(5,gear.GetPart<ArmorPart>().AV);
        }
    }
}
