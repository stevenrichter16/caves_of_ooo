using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityMaterialUnitTests
    {
        EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();
        }
        [TestCase("TarSeep","Volatility",.3f)]
        [TestCase("PeatBog","Porosity",.8f)]
        [TestCase("SteamVent","Porosity",1f)]
        [TestCase("FrostVent","Brittleness",.7f)]
        [TestCase("IceSheet","Brittleness",.9f)]
        [TestCase("OilSeep","Volatility",.8f)]
        [TestCase("Glowmaw","Volatility",.4f)]
        [TestCase("LeatherArmor","Porosity",.6f)]
        [TestCase("SporeShambler","Porosity",.5f)]
        [TestCase("GlassScorpion","Brittleness",.7f)]
        public void AuthoredFractionalPropertiesMatchTheirExistingConsumers(string name,string field,float value)
        {
            var material=factory.CreateEntity(name).GetPart<MaterialPart>();Assert.NotNull(material);
            Assert.AreEqual(value,(float)typeof(MaterialPart).GetField(field).GetValue(material),.0001f);
        }
        [TestCase(false)][TestCase(true)]
        public void ActualTarRequiresPositiveThresholdCrossingToIgnite(bool heat)
        {
            var target=factory.CreateEntity("TarSeep");var thermal=target.GetPart<ThermalPart>();
            var zone=new Zone("material-units");Assert.True(zone.AddEntity(target,10,10));
            Assert.AreEqual(20,thermal.Temperature);Assert.False(target.HasEffect<BurningEffect>());
            if(heat)
            {
                var e=GameEvent.New("ApplyHeat");e.SetParameter("Joules",160f*thermal.HeatCapacity);e.SetParameter("Zone",zone);
                target.FireEventAndRelease(e);
            }
            Assert.AreEqual(heat,target.HasEffect<BurningEffect>(), "temp="+thermal.Temperature+" volatility="+target.GetPart<MaterialPart>().Volatility+" material="+target.GetPart<MaterialPart>().MaterialTagsRaw);
            Assert.True(zone.GetEntityCell(target)!=null);
            if(heat)Assert.AreEqual(1.3f,target.GetEffect<BurningEffect>().Intensity,.0001f);
        }
        [TestCase(false)][TestCase(true)]
        public void TarIgnitionRequiresAuthoredFlammabilityAsWellAsThermalCrossing(bool flammable)
        {
            var target=factory.CreateEntity("TarSeep");var material=target.GetPart<MaterialPart>();
            if(!flammable){material.MaterialTags.Remove("Flammable");target.Tags.Remove("Flammable");}
            var heat=GameEvent.New("ApplyHeat");heat.SetParameter("Joules",(object)(160f*target.GetPart<ThermalPart>().HeatCapacity));
            target.FireEventAndRelease(heat);Assert.AreEqual(flammable,target.HasEffect<BurningEffect>());
        }
        [TestCase("TarSeep","Volatility",30f)][TestCase("PeatBog","Porosity",80f)]
        [TestCase("SteamVent","Porosity",100f)][TestCase("FrostVent","Brittleness",70f)]
        [TestCase("IceSheet","Brittleness",90f)]
        public void ExistingSerializedMaterialValuesAreNotHeuristicallyMigrated(string name,string field,float legacy)
        {
            var source=factory.CreateEntity(name);var member=typeof(MaterialPart).GetField(field);
            float current=(float)member.GetValue(source.GetPart<MaterialPart>());
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(source);
            Assert.AreEqual(current,(float)member.GetValue(fresh.GetPart<MaterialPart>()));
            member.SetValue(source.GetPart<MaterialPart>(),legacy);
            var old=PartRoundTripHelper.RoundTripEntityViaTokenGraph(source);
            Assert.AreEqual(legacy,(float)member.GetValue(old.GetPart<MaterialPart>()));
            Assert.AreEqual(current,(float)member.GetValue(factory.CreateEntity(name).GetPart<MaterialPart>()));
        }
        [TestCase("PeatBog",25f)][TestCase("PeatBog",100f)]
        [TestCase("SteamVent",25f)][TestCase("SteamVent",100f)]
        [TestCase("LeatherArmor",25f)][TestCase("LeatherArmor",100f)]
        public void ActualPorousMaterialLosesRatherThanCreatesMoisture(string name,float temperature)
        {
            var target=factory.CreateEntity(name);var thermal=target.GetPart<ThermalPart>();
            if(thermal==null){thermal=new ThermalPart();target.AddPart(thermal);}thermal.Temperature=temperature;
            var wet=new WetEffect(.2f);target.ApplyEffect(wet);Assert.AreSame(wet,target.GetEffect<WetEffect>());
            float before=wet.Moisture;Assert.That(before,Is.InRange(.2f,1f));
            wet.OnTurnEnd(target);Assert.That(wet.Moisture,Is.InRange(0f,before));Assert.Less(wet.Moisture,before);
            Assert.AreEqual(temperature,thermal.Temperature);
        }
        [TestCase("FrostVent",false)][TestCase("IceSheet",true)]
        [TestCase("GlassScorpion",false)]
        public void NativeBrittlenessUsesPartialOrCatastrophicExistingConsumer(string name,bool catastrophic)
        {
            var target=factory.CreateEntity(name);
            // Explicit100HP diagnostic isolates the material's existing shatter
            // consumer from unrelated scenery durability/creature resistance.
            target.Statistics["Hitpoints"]=new Stat{Owner=target,Name="Hitpoints",BaseValue=100,Min=0,Max=100};
            var e=GameEvent.New("TryShatter");target.FireEventAndRelease(e);
            if(catastrophic)Assert.AreEqual(0,target.GetStatValue("Hitpoints"));
            else Assert.That(target.GetStatValue("Hitpoints"),Is.InRange(75,85));
        }
    }
}
