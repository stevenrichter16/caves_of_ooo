using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityTorchAdversarialTests
    {
        EntityFactory factory;Entity actor,torch;InventoryPart inv;Zone zone;
        [SetUp]public void Setup()
        {factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));actor=new Entity{ID="torch-adversary"};actor.SetTag("Player");inv=new InventoryPart();actor.AddPart(inv);actor.AddPart(new PhysicsPart());zone=new Zone("TorchAdversarial"){AmbientLevel=0.1f};zone.AddEntity(actor,10,10);torch=factory.CreateEntity("Torch");Assert.True(inv.AddObject(torch));MessageLog.Clear();}
        void Hold()=>inv.Equip(torch,"Hand");bool Act(string s)=>InventorySystem.PerformAction(actor,torch,s,zone);
        void Fire(){zone.AddEntity(factory.CreateEntity("Campfire"),11,10);}
        void End(){var e=GameEvent.New("EndTurn");e.SetParameter("Zone",(object)zone);actor.FireEventAndRelease(e);}
        [Test]public void WetAndDryTorches_DoNotMergeTheirEnvironmentalState()
        {var wet=factory.CreateEntity("Torch");wet.ApplyEffect(new WetEffect(1));Assert.False(torch.GetPart<StackerPart>().CanStackWith(wet));Assert.False(wet.GetPart<StackerPart>().CanStackWith(torch));}
        [Test]public void SplittingWetTorch_DoesNotEraseWetness()
        {torch.GetPart<StackerPart>().StackCount=2;torch.ApplyEffect(new WetEffect(1));Assert.True(InventorySystem.Equip(actor,torch));var split=inv.EquippedItems["Hand"];Assert.NotNull(split.GetEffect<WetEffect>());Assert.AreEqual(torch.GetEffect<WetEffect>().Moisture,split.GetEffect<WetEffect>().Moisture);Assert.AreNotSame(torch.GetEffect<WetEffect>(),split.GetEffect<WetEffect>());}
        [Test]public void DestructiveBurning_LooseTorch_ConsumesFuelOnceAndStillDamagesTool()
        {inv.RemoveObject(torch);zone.AddEntity(torch,11,10);torch.GetStat("Hitpoints").BaseValue=1000;torch.GetStat("Hitpoints").Max=1000;torch.ApplyEffect(new BurningEffect(1,null,new System.Random(1)));MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(49.7f,torch.GetPart<FuelPart>().FuelMass,0.0001f);Assert.Less(torch.GetStatValue("Hitpoints"),1000);}
        [Test]public void FrozenTorch_RefusesRelightUntilThawed()
        {Hold();Assert.True(Act("ExtinguishTorch"));torch.ApplyEffect(new FrozenEffect(1));Fire();Assert.False(Act("LightTorch"));Assert.False(torch.GetPart<LightSourcePart>().Enabled);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)][TestCase(0f)]
        public void InvalidFuel_RefusesRelightWithoutChangingState(float amount)
        {Hold();Assert.True(Act("ExtinguishTorch"));torch.GetPart<FuelPart>().FuelMass=amount;Fire();Assert.False(Act("LightTorch"));Assert.False(torch.GetPart<LightSourcePart>().Enabled);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)][TestCase(0f)]
        public void InvalidBurnRate_TickExtinguishesWithoutAddingFuel(float amount)
        {Hold();torch.GetPart<FuelPart>().BurnRate=amount;End();Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);Assert.False(torch.GetPart<LightSourcePart>().Enabled);}
        [Test]public void WetHeldTorch_ExtinguishesWithoutBurningAwayWaterOrFuel()
        {Hold();torch.ApplyEffect(new WetEffect(1));End();Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);Assert.False(torch.GetPart<LightSourcePart>().Enabled);Assert.AreEqual(25,torch.GetPart<ThermalPart>().Temperature);}
        [Test]public void ObserverThrows_CommittedTorchAndSuccessReceiptSurvive()
        {Hold();var old=MessageLog.OnMessage;try{MessageLog.OnMessage=x=>{if(x=="You extinguish the torch.")throw new InvalidOperationException("observer");};Assert.True(Act("ExtinguishTorch"));Assert.False(torch.GetPart<LightSourcePart>().Enabled);}finally{MessageLog.OnMessage=old;}}
        [Test]public void OtherEquippedLights_AreNotExtinguishedByTorch()
        {Hold();var sword=factory.CreateEntity("FlamingSword");inv.AddObject(sword);inv.Equip(sword,"OtherHand");Assert.True(Act("ExtinguishTorch"));var map=new LightMap();map.Compute(zone);Assert.Greater(map.GetBrightness(10,10),0.1f);Assert.True(sword.GetPart<LightSourcePart>().Enabled);}
        [Test]public void RemoteHotSource_CannotIgniteViaFootprintlessAnchor()
        {Hold();Assert.True(Act("ExtinguishTorch"));zone.AddEntity(factory.CreateEntity("Campfire"),20,10);Assert.False(Act("LightTorch"));}
        [Test]public void LooseStack_ToggleRefusedWithoutSplittingOrChangingState()
        {inv.RemoveObject(torch);zone.AddEntity(torch,11,10);torch.GetPart<StackerPart>().StackCount=2;Assert.False(Act("ExtinguishTorch"));Assert.AreEqual(2,torch.GetPart<StackerPart>().StackCount);Assert.True(torch.GetPart<LightSourcePart>().Enabled);}
        [Test]public void SourceWithEmptyFuel_IsNotAnInfiniteIgnitionSource()
        {Hold();Assert.True(Act("ExtinguishTorch"));var fire=factory.CreateEntity("Campfire");zone.AddEntity(fire,11,10);fire.GetPart<FuelPart>().FuelMass=0;Assert.False(Act("LightTorch"));}
        [TestCase("equip")][TestCase("partial")][TestCase("remove")]
        public void UnknownEffect_SplitRefusesWithoutLosingCountOrState(string route)
        {
            torch.GetPart<StackerPart>().StackCount=2;var effect=new UnknownTorchEffect();torch.ApplyEffect(effect);
            if(route=="equip")Assert.False(InventorySystem.Equip(actor,torch));
            else if(route=="partial")Assert.Null(torch.GetPart<StackerPart>().SplitStack(1));
            else Assert.Null(torch.GetPart<StackerPart>().RemoveOne());
            Assert.AreEqual(2,torch.GetPart<StackerPart>().StackCount);Assert.AreSame(effect,torch.GetEffect<UnknownTorchEffect>());
            Assert.True(inv.Objects.Contains(torch));Assert.AreEqual(0,inv.EquippedItems.Count);
        }
        [TestCase("wet")][TestCase("frozen")][TestCase("burning")]
        public void SupportedEnvironmentSplit_PreservesIndependentEffectOwnerAndLifetime(string kind)
        {
            torch.GetPart<StackerPart>().StackCount=2;Effect original=kind=="wet"?(Effect)new WetEffect(0.7f):kind=="frozen"?new FrozenEffect(0.8f):new BurningEffect(1.2f,actor,new System.Random(4));
            torch.ApplyEffect(original);original.Duration=7;original.JustApplied=true;original.LastRemovalCause="split-test";
            var split=torch.GetPart<StackerPart>().SplitStack(1);Assert.NotNull(split);var all=split.GetPart<StatusEffectsPart>()?.GetAllEffects();Assert.NotNull(all);Assert.AreEqual(1,all.Count);
            var copy=all[0];Assert.AreNotSame(original,copy);Assert.AreSame(torch,original.Owner);Assert.AreSame(split,copy.Owner);Assert.AreEqual(7,copy.Duration);Assert.True(copy.JustApplied);Assert.AreEqual("split-test",copy.LastRemovalCause);
            if(kind=="wet")Assert.AreEqual(((WetEffect)original).Moisture,((WetEffect)copy).Moisture);
            if(kind=="frozen")Assert.AreEqual(((FrozenEffect)original).Cold,((FrozenEffect)copy).Cold);
            if(kind=="burning"){Assert.AreEqual(((BurningEffect)original).Intensity,((BurningEffect)copy).Intensity);Assert.AreSame(actor,((BurningEffect)copy).IgnitionSource);}
        }
        [Test]public void UnsupportedDirectClone_FailsBeforeMutatingSource()
        {torch.ApplyEffect(new UnknownTorchEffect());Assert.Throws<InvalidOperationException>(()=>torch.CloneForStack());Assert.True(inv.Objects.Contains(torch));Assert.NotNull(torch.GetEffect<UnknownTorchEffect>());}
        [Test]public void FrozenLitTorch_ExtinguishesOnNextHeldTick()
        {Hold();torch.ApplyEffect(new FrozenEffect(1));End();Assert.False(torch.GetPart<LightSourcePart>().Enabled);Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);}
        public sealed class UnknownTorchEffect:Effect{public override string DisplayName=>"unknown torch state";public UnknownTorchEffect(){Duration=-1;}}
        [Test]public void TorchOffField_IsSavedWithoutModifyingUnrelatedLights()
        {Hold();Assert.True(Act("ExtinguishTorch"));var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(torch);Assert.False(copy.GetPart<LightSourcePart>().Enabled);Assert.True(factory.CreateEntity("FlamingSword").GetPart<LightSourcePart>().Enabled);}
    }
}
