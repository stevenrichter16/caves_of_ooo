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
    public sealed class FiftyCombatMedicineTests
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
        [TestCase(true,1)] [TestCase(false,0)]
        public void SplashHitsExposedSecondaryBodyExactlyOnce(bool body,int applications)
        {
            var z=new Zone(); var a=Actor(z); var target=F.Owner(body?"0,0;-1,0;-2,0;-2,1":null,true); F.Place(z,target,14,10);
            target.GetStat("Hitpoints").BaseValue=500; var item=Flask(a,"1d1+6");
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,11,10,new S.FixedRandom()),a,z).Success);
            Assert.AreEqual(500+7*applications,target.GetStatValue("Hitpoints")); Assert.AreEqual(1,item.GetPart<StackerPart>().StackCount);
        }
        [TestCase("Wet",true)] [TestCase("Healing",false)]
        public void ElementalSplashCanDampenSceneryButMedicineCannotRepairIt(string kind,bool affected)
        {
            var z=new Zone(); var a=Actor(z); var timber=new Entity(); timber.AddPart(new PhysicsPart()); timber.AddPart(new StatusEffectsPart());
            timber.AddPart(new MaterialPart { MaterialID="Wood", MaterialTagsRaw="Wood,Flammable" }); timber.AddPart(new ThermalPart());
            timber.Statistics["Hitpoints"]=new Stat { Name="Hitpoints",Owner=timber,BaseValue=20,Max=100 }; F.Place(z,timber,12,10);
            var friend=F.Owner(creature:true); F.Place(z,friend,11,11);
            var item=Flask(a,kind=="Healing"?"1d1+6":"",kind=="Wet"?"Wet":"");
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(item,11,10,new S.FixedRandom()),a,z).Success);
            Assert.AreEqual(affected,timber.HasEffect<WetEffect>()); Assert.AreEqual(affected,friend.HasEffect<WetEffect>());
            Assert.AreEqual(20,timber.GetStatValue("Hitpoints"),"Medical healing must not leak to scenery."); Assert.AreEqual(1,item.GetPart<StackerPart>().StackCount);
        }
        [TestCase("SpeedTonic","Speed",20)] [TestCase("StrengthTonic","Strength",4)]
        public void AuthoredSurgeRefreshesThenExpiresWithoutErasingLegacyBoost(string blueprint,string stat,int amount)
        {
            var z=new Zone(); var a=Actor(z); a.GetStat(stat).Boost=3; int before=a.GetStatValue(stat);
            var tonic=Carry(a,blueprint,2); Assert.AreEqual(20,tonic.GetPart<TonicPart>().Duration,"The authored new-use contract is finite.");
            Assert.True(InventorySystem.PerformAction(a,tonic,"ApplyTonic",z)); Assert.AreEqual(before+amount,a.GetStatValue(stat));
            S.Ready(a); for(int i=0;i<7;i++) S.Tick(a,z);
            Assert.True(InventorySystem.PerformAction(a,tonic,"ApplyTonic",z)); Assert.AreEqual(before+amount,a.GetStatValue(stat),"Second bottle refreshes, not stacks.");
            S.Ready(a); var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(a); Assert.AreEqual(before+amount,loaded.GetStatValue(stat));
            for(int i=0;i<19;i++)S.Tick(loaded,null); Assert.AreEqual(before+amount,loaded.GetStatValue(stat));
            S.Tick(loaded,null); Assert.AreEqual(before,loaded.GetStatValue(stat)); Assert.AreEqual(3,loaded.GetStat(stat).Boost);
        }
        [Test] public void ExplicitFiniteStatTonicCannotBankMagnitudeWhileLegacyZeroDurationStillCan()
        {
            var z=new Zone(); var a=Actor(z); var item=Flask(a); var tonic=item.GetPart<TonicPart>(); tonic.StatBoost="Strength:4"; tonic.Duration=2;
            int before=a.GetStatValue("Strength"); Assert.True(tonic.ApplyTo(a,a,z)); Assert.True(tonic.ApplyTo(a,a,z));
            Assert.AreEqual(before+4,a.GetStatValue("Strength")); S.Ready(a); S.Tick(a,z); S.Tick(a,z); Assert.AreEqual(before,a.GetStatValue("Strength"));
            tonic.Duration=0; Assert.True(tonic.ApplyTo(a,a,z)); Assert.True(tonic.ApplyTo(a,a,z)); Assert.AreEqual(before+8,a.GetStatValue("Strength"));
        }
        [TestCase("ordinary")] [TestCase("gas")] [TestCase("both")]
        public void OneAntidoteTreatsPoisonFamilyAndPreservesOtherEffects(string kind)
        {
            var z=new Zone(); var a=Actor(z); if(kind!="gas")a.ApplyEffect(new PoisonedEffect(),null,z); if(kind!="ordinary")a.ApplyEffect(new PoisonedByGasEffect { Duration=5 },null,z);
            a.ApplyEffect(new BurningEffect(),null,z); var item=Carry(a,"Antidote",2);
            Assert.True(InventorySystem.PerformAction(a,item,"ApplyTonic",z)); Assert.False(a.HasEffect<PoisonedEffect>()); Assert.False(a.HasEffect<PoisonedByGasEffect>());
            Assert.True(a.HasEffect<BurningEffect>()); Assert.AreEqual(1,item.GetPart<StackerPart>().StackCount);
            Assert.True(a.ApplyEffect(new PoisonedByGasEffect { Duration=5 },null,z),"Treatment does not grant exposure immunity.");
        }
        [TestCase(800f,25f)] [TestCase(-10f,-10f)]
        public void BurnSalveCoolsExistingThermalStateWithoutHeatingOrCuringOtherAilments(float temperature,float expected)
        {
            var z=new Zone(); var a=Actor(z); var thermal=new ThermalPart { AmbientTemperature=25 }; a.AddPart(thermal);
            a.ApplyEffect(new BurningEffect(),null,z); thermal.Temperature=temperature; a.ApplyEffect(new PoisonedEffect(),null,z); a.ApplyEffect(new CharredEffect(),null,z);
            var salve=Carry(a,"BurnSalve",2); Assert.True(InventorySystem.PerformAction(a,salve,"ApplyTonic",z));
            Assert.False(a.HasEffect<BurningEffect>()); Assert.AreEqual(expected,thermal.Temperature); Assert.True(a.HasEffect<PoisonedEffect>()); Assert.True(a.HasEffect<CharredEffect>());
            Assert.False(a.HasEffect<WetEffect>()); Assert.AreEqual(1,salve.GetPart<StackerPart>().StackCount);
        }
        [Test] public void EarlyBurnSalveDoesNotManufactureCharred()
        {
            var z=new Zone(); var a=Actor(z); a.ApplyEffect(new BurningEffect(),null,z); var item=Carry(a,"BurnSalve");
            Assert.True(InventorySystem.PerformAction(a,item,"ApplyTonic",z)); Assert.False(a.HasEffect<BurningEffect>()); Assert.False(a.HasEffect<CharredEffect>());
        }
        [TestCase(1,5)] [TestCase(2,7)] [TestCase(3,9)] [TestCase(99,9)]
        public void ToxicBrewPotencyBecomesBoundedPoisonDuration(int potency,int expected)
        {
            var resolved=BrewResolver.Resolve(new [] { new BrewPropertyAmount("toxic",potency) }); Assert.AreEqual(BrewOutcomeKind.Brew,resolved.Kind);
            var poison=resolved.Effects.Single(e=>e.Effect=="Poison");
            var effect=(PoisonedEffect)TonicEffectFactory.Create(poison.Effect,0,"",poison.Potency,null);
            Assert.AreEqual(expected,effect.Duration); Assert.AreEqual("1d3",effect.DamageDice);
            var explicitEffect=(PoisonedEffect)TonicEffectFactory.Create("Poison",2,"1d7",potency,null); Assert.AreEqual(2,explicitEffect.Duration); Assert.AreEqual("1d7",explicitEffect.DamageDice);
        }
        [TestCase(1,5,30)] [TestCase(2,7,40)] [TestCase(3,9,50)]
        public void PoisonTemperPreservesPotencyDurationAndSpendsOneQuench(int potency,int expected,int chance)
        {
            var z=new Zone(); var a=Actor(z); var weapon=Carry(a,"Dagger"); var brew=Flask(a);
            brew.AddPart(new BrewItemPart { EffectsRaw="Poison:"+potency, Form="Coating" });
            Assert.True(WeaponTemperingService.TryTemper(a,weapon,brew,out string reason),reason);
            var spec=OnHitEffectSpec.Parse(weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw).Single();
            Assert.AreEqual(expected,spec.DurationTurns); Assert.AreEqual(chance,spec.ChancePercent); Assert.AreEqual(1,brew.GetPart<StackerPart>().StackCount);
        }
    }
}
