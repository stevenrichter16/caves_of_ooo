using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ScreeningStartNoOpScenario : IScenario
    {
        public static int Calls;
        public void Apply(ScenarioContext context){Calls++;}
    }
    public sealed class ScreeningStartRequestTests : FiftyWorldFixture
    {
        string Resolve(string scene,bool pending,bool wild,string requested)
        {
            var manager=OverworldZoneManager.CreateDetached(Factory,64,true);
            var method=typeof(FreshGameStart).GetMethod("ResolveStartZone");
            // Before the feature exists, exercise the real legacy decision:
            // it cannot honor the additional explicit scenario request.
            if(method.GetParameters().Length==6)return FreshGameStart.ResolveStartZone(scene,pending,wild,manager);
            return (string)method.Invoke(null,new object[]{scene,pending,wild,manager,BiomeType.Spread,0,requested});
        }
        [Test] public void DeclaredScenarioCanStartAtTheRealFreshAllotment()
        {Assert.AreEqual("Overworld.2.6.0",Resolve("",true,false,"Overworld.2.6.0"));}
        [Test] public void ExplicitSceneDestinationKeepsPrecedence()
        {Assert.AreEqual("scene-owned-destination",Resolve("scene-owned-destination",true,false,"Overworld.2.6.0"));}
        [Test] public void ARequestWithoutAPendingScenarioCannotChangeNormalNewGameSelection()
        {Assert.AreEqual("",Resolve("",false,false,"Overworld.2.6.0"));}
        [TestCase("Overworld.999.999.0")][TestCase("not-a-zone")]
        public void InvalidDeclaredAddressCannotCreateAnArbitraryNewZone(string requested)
        {Assert.AreEqual("",Resolve("",true,false,requested));}
        [Test] public void ExplicitScenarioAddressIsMoreSpecificThanItsWildFallback()
        {Assert.AreEqual("Overworld.2.6.0",Resolve("",true,true,"Overworld.2.6.0"));}
        [Test] public void ApplyingTheScenarioClearsTheOneShotAddress()
        {
            var field=typeof(ScenarioRunner).GetField("PendingStartZoneID",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(field,"The pending address needs a one-shot launch contract.");
            var old=ScenarioRunner.PendingScenario;var previous=field.GetValue(null);var last=ScenarioRunner.LastLaunched;
            bool oldWild=ScenarioRunner.PendingWildStart;var oldBiome=ScenarioRunner.PendingWildBiome;int oldDepth=ScenarioRunner.PendingWildDepth;
            try
            {
                ScreeningStartNoOpScenario.Calls=0;field.SetValue(null,"Overworld.2.6.0");ScenarioRunner.PendingScenario=typeof(ScreeningStartNoOpScenario);
                typeof(ScenarioRunner).GetMethod("HandleAfterBootstrap",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Zone,Factory,Actor,Clock});
                Assert.AreEqual(1,ScreeningStartNoOpScenario.Calls);Assert.True(string.IsNullOrEmpty((string)field.GetValue(null)));Assert.IsNull(ScenarioRunner.PendingScenario);
            }
            finally{ScenarioRunner.PendingScenario=old;ScenarioRunner.LastLaunched=last;field.SetValue(null,previous);ScenarioRunner.PendingWildStart=oldWild;ScenarioRunner.PendingWildBiome=oldBiome;ScenarioRunner.PendingWildDepth=oldDepth;}
        }
    }
}
