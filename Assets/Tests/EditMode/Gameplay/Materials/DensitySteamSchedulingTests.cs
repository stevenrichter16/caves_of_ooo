using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class DensitySteamSchedulingTests
    {
        DensityLootTestScope scope; CavesOfOoo.Data.EntityFactory oldFactory;
        Zone zone; Entity source,neighbor;
        List<MaterialReactionBlueprint> savedReactions; bool savedInitialized;
        static FieldInfo Reactions => typeof(MaterialReactionResolver).GetField("_reactions",BindingFlags.Static|BindingFlags.NonPublic);
        static FieldInfo Initialized => typeof(MaterialReactionResolver).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            scope=new DensityLootTestScope();oldFactory=MaterialReactionResolver.Factory;
            savedReactions=new List<MaterialReactionBlueprint>((List<MaterialReactionBlueprint>)Reactions.GetValue(null));savedInitialized=(bool)Initialized.GetValue(null);
            MaterialReactionResolver.Factory=scope.Factory;
            MaterialReactionResolver.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions/water_plus_fire.json")));
            zone=new Zone("steam-scheduling");source=scope.Factory.CreateEntity("DryBrush");
            neighbor=new Entity{BlueprintName="thermal-observer"};neighbor.AddPart(new ThermalPart{Temperature=100,HeatCapacity=1,AmbientDecayRate=0});
            // A neighboring creature receives steam here, but its own scheduler turn is
            // deliberately not advanced by this non-creature material tick.
            neighbor.SetTag("Creature");zone.AddEntity(source,10,10);zone.AddEntity(neighbor,11,10);
        }
        [TearDown] public void Cleanup()
        {MaterialReactionResolver.Factory=oldFactory;Reactions.SetValue(null,savedReactions);Initialized.SetValue(null,savedInitialized);scope.Dispose();}
        SteamEffect ReactionThenExtinguish(bool displaced)
        {
            var burning=new BurningEffect(1,null,new System.Random(1));source.ApplyEffect(burning);
            source.ApplyEffect(new WetEffect(0.5f));MaterialReactionResolver.EvaluateReactions(source,zone,burning);
            Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SteamCloud"));
            var steam=source.GetEffect<SteamEffect>();Assert.NotNull(steam);
            source.RemoveEffect<BurningEffect>();source.RemoveEffect<WetEffect>();
            source.GetPart<ThermalPart>().Temperature=displaced?100:25;return steam;
        }
        [TestCase(false)] [TestCase(true)]
        public void RealReactionSteamCoolsAndWetsAfterOriginalPropStopsBurning(bool displaced)
        {
            var steam=ReactionThenExtinguish(displaced);MaterialSimSystem.TickMaterialEntities(zone);
            Assert.Less(neighbor.GetPart<ThermalPart>().Temperature,100);Assert.NotNull(neighbor.GetEffect<WetEffect>());
            Assert.AreEqual(0.08f,neighbor.GetEffect<WetEffect>().Moisture,0.0001f);
        }
        [TestCase(false)] [TestCase(true)]
        public void RealReactionSteamLifetimeAdvancesWithoutDependingOnUnrelatedHeat(bool displaced)
        {
            var steam=ReactionThenExtinguish(displaced);float before=steam.Density;
            MaterialSimSystem.TickMaterialEntities(zone);MaterialSimSystem.TickMaterialEntities(zone);
            Assert.Less(steam.Density,before);
            for(int i=0;i<20;i++)MaterialSimSystem.TickMaterialEntities(zone);
            Assert.IsFalse(source.HasEffect<SteamEffect>());
        }
        [Test] public void RealSpawnedCloudIsALifespanVisualNotTheSteamStatusOwner()
        {
            ReactionThenExtinguish(false);var cloud=zone.GetAllEntities().Single(e=>e.BlueprintName=="SteamCloud");
            Assert.IsFalse(cloud.HasEffect<SteamEffect>());Assert.NotNull(cloud.GetPart<LifespanPart>());
            Assert.AreEqual(120,cloud.GetPart<ThermalPart>().Temperature);
            for(int i=0;i<4;i++)MaterialSimSystem.TickMaterialEntities(zone);
            Assert.IsNull(zone.GetEntityCell(cloud));
        }
        [Test] public void DirectOwnerTurnIsTheWorkingCoolingAndWettingControl()
        {
            ReactionThenExtinguish(false);var e=GameEvent.New("BeginTakeAction");e.SetParameter("Zone",(object)zone);source.FireEventAndRelease(e);
            Assert.AreEqual(98,neighbor.GetPart<ThermalPart>().Temperature,0.0001f);Assert.AreEqual(0.08f,neighbor.GetEffect<WetEffect>().Moisture,0.0001f);
        }
        [Test] public void CreatureSteamIsNotDoubleTickedByMaterialSimulation()
        {
            var steam=ReactionThenExtinguish(false);source.SetTag("Creature");
            MaterialSimSystem.TickMaterialEntities(zone);MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(0.8f,steam.Density,0.0001f);Assert.AreEqual(100,neighbor.GetPart<ThermalPart>().Temperature);Assert.IsNull(neighbor.GetEffect<WetEffect>());
        }
        [TestCase(true)] [TestCase(false)]
        public void SteamDoesNotSuppressExistingPassiveCookingReactions(bool steamPresent)
        {
            MaterialReactionResolver.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions/fire_plus_raw_meat.json")));
            zone.RemoveEntity(source);source=scope.Factory.CreateEntity("RawMeat");zone.AddEntity(source,10,10);
            source.GetPart<ThermalPart>().Temperature=200;
            if(steamPresent)source.ApplyEffect(new SteamEffect(0.5f));
            MaterialSimSystem.TickMaterialEntities(zone);
            Assert.IsNull(zone.GetEntityCell(source));Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="CookedMeat"));
        }
        [Test] public void ClearAmbientPropHasNoSteamSideEffects()
        {
            source.GetPart<ThermalPart>().Temperature=25;MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(100,neighbor.GetPart<ThermalPart>().Temperature);Assert.IsNull(neighbor.GetEffect<WetEffect>());Assert.IsFalse(source.HasEffect<SteamEffect>());
        }
    }
}
