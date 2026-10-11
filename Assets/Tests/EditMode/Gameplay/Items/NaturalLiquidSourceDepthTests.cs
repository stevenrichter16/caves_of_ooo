using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class NaturalLiquidSourceDepthTests
    {
        EntityFactory factory, oldFactory; Entity actor, flask; Zone zone;
        Dictionary<string,LiquidDefinition> saved; bool initialized;
        static FieldInfo Registry => typeof(LiquidRegistry).GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic);
        static FieldInfo Init => typeof(LiquidRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            saved = new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Registry.GetValue(null)); initialized = (bool)Init.GetValue(null);
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/TileReactions/Reactions.json")));
            factory = new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            oldFactory = MaterialReactionResolver.Factory; MaterialReactionResolver.Factory = factory;
            zone = new Zone("natural-liquid-depth"); actor = factory.CreateEntity("Player"); zone.AddEntity(actor,10,10);
            flask = new Entity { ID = Guid.NewGuid().ToString("N") }; flask.SetTag("Item");
            flask.AddPart(new PhysicsPart { Takeable = true }); flask.AddPart(new LiquidVesselPart()); actor.GetPart<InventoryPart>().AddObject(flask);
        }
        [TearDown] public void Cleanup()
        {
            MaterialReactionResolver.Factory = oldFactory; TileReactionSystem.ResetForTests();
            var registry = (Dictionary<string,LiquidDefinition>)Registry.GetValue(null); registry.Clear(); foreach(var pair in saved) registry[pair.Key]=pair.Value; Init.SetValue(null,initialized);
        }
        Entity Pool(string blueprint)
        {
            var pool=factory.CreateEntity(blueprint); Assert.True(zone.AddEntity(pool,11,10)); pool.GetPart<TileStateSourcePart>().Seed(zone,11,10); return pool;
        }
        string Command(Entity pool) => "FillLiquidVessel|" + Uri.EscapeDataString(pool.ID) + "|" + Uri.EscapeDataString(pool.GetPart<LiquidPoolPart>().LiquidId);
        [TestCase("BrinePool","brine")][TestCase("MirePool","bog-mire")]
        public void AuthoredNaturalPoolCanFillAndPourItsActualLiquid(string blueprint,string id)
        {
            var pool=Pool(blueprint); int before=pool.GetPart<LiquidPoolPart>().Volume;
            Assert.True(InventorySystem.GetActions(actor,flask).Any(a=>a.Command==Command(pool)), "collectible source must be offered");
            Assert.True(InventorySystem.PerformAction(actor,flask,Command(pool),zone));
            Assert.AreEqual(id,flask.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(before-12,pool.GetPart<LiquidPoolPart>().Volume);
            var pour=InventorySystem.GetActions(actor,flask).Single(a=>a.Name=="PourLiquid" && a.Command.EndsWith("|10|11"));
            Assert.True(InventorySystem.PerformAction(actor,flask,pour.Command,zone));
            Assert.AreEqual(id,zone.GetCell(10,11).Objects.Single(e=>e.HasPart<LiquidPoolPart>()).GetPart<LiquidPoolPart>().LiquidId);
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void PoolHasOneLiteralCoatingInsteadOfAFalseWaterMixture(string blueprint)
        {
            var pool=Pool(blueprint); Assert.True(zone.TileState.HasCoating(11,10,pool.GetPart<LiquidPoolPart>().LiquidId));
            Assert.False(zone.TileState.HasCoating(11,10,"water")); Assert.True(LiquidSourceSafety.IsUnmixedPool(zone,pool));
        }
        [TestCase("BrinePool","water")][TestCase("MirePool","water")][TestCase("BrinePool","acid")][TestCase("MirePool","oil")]
        public void IndependentUnlikeCoatingStillBlocksSampling(string blueprint,string contaminant)
        {
            var pool=Pool(blueprint); zone.TileState.WriteCoating(11,10,contaminant,5); int before=pool.GetPart<LiquidPoolPart>().Volume;
            Assert.False(InventorySystem.PerformAction(actor,flask,Command(pool),zone)); Assert.AreEqual(before,pool.GetPart<LiquidPoolPart>().Volume); Assert.Zero(flask.GetPart<LiquidVesselPart>().Volume);
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void OuterRollbackConservesNaturalSource(string blueprint)
        {
            var pool=Pool(blueprint); int before=pool.GetPart<LiquidPoolPart>().Volume; var tx=new InventoryTransaction();
            Assert.True(LiquidVesselService.TryAct(actor,flask,zone,Command(pool),tx)); tx.Rollback();
            Assert.AreEqual(before,pool.GetPart<LiquidPoolPart>().Volume); Assert.Zero(flask.GetPart<LiquidVesselPart>().Volume);
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void NaturalChemicalPoolDoesNotFillDrinkingWaterskin(string blueprint)
        {
            Pool(blueprint); var skin=factory.CreateEntity("Waterskin"); actor.GetPart<InventoryPart>().AddObject(skin);
            Assert.False(InventorySystem.PerformAction(actor,skin,"FillWaterskin",zone)); Assert.Zero(skin.GetPart<WaterskinPart>().Charges);
        }
        [TestCase("BrinePool","heat")][TestCase("MirePool","heat")][TestCase("BrinePool","cold")][TestCase("MirePool","cold")]
        public void NaturalPoolRetainsWaterWorldReactions(string blueprint,string energy)
        {
            Pool(blueprint); if(energy=="heat") zone.TileState.AddHeat(11,10,1); else zone.TileState.AddCold(11,10,1);
            Assert.Greater(TileReactionSystem.ResolveZone(zone),0);
            if(energy=="cold") Assert.True(zone.TileState.HasCoating(11,10,"ice"));
            else Assert.True(zone.TileState.Cloud(11,10) == "steam");
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void FrozenNaturalPoolCannotBeCollected(string blueprint)
        {
            var pool=Pool(blueprint); zone.TileState.WriteCoating(11,10,"ice",4);
            Assert.False(InventorySystem.PerformAction(actor,flask,Command(pool),zone)); Assert.Zero(flask.GetPart<LiquidVesselPart>().Volume);
        }

        [TestCase("BrinePool")][TestCase("MirePool")]
        public void ConductingSolutionTriggersOneWetReactionNotTheDryFallback(string blueprint)
        {
            Pool(blueprint); zone.TileState.AddCharge(11,10,2);
            Assert.AreEqual(1,TileReactionSystem.ResolveZone(zone)); Assert.AreEqual(1,zone.TileState.Charge(11,10));
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void ASeparateWaterPoolIsNotConfusedWithTheSourceReactionFamily(string blueprint)
        {
            var pool=Pool(blueprint); var other=new Entity(); other.AddPart(new PhysicsPart()); other.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});
            zone.AddEntity(other,11,10); Assert.False(InventorySystem.PerformAction(actor,flask,Command(pool),zone)); Assert.Zero(flask.GetPart<LiquidVesselPart>().Volume);
        }
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void ColdTemperatureBlocksAnOtherwiseUnmixedSolution(string blueprint)
        {
            var pool=Pool(blueprint); var thermal=pool.GetPart<ThermalPart>(); thermal.Temperature=thermal.FreezeTemperature;
            Assert.False(InventorySystem.PerformAction(actor,flask,Command(pool),zone)); Assert.Zero(flask.GetPart<LiquidVesselPart>().Volume);
        }

        [TestCase("BrinePool")][TestCase("MirePool")]
        public void GroundSolutionStillCarriesChargeAcrossTheWetSheet(string blueprint)
        {
            Pool(blueprint); var next=factory.CreateEntity(blueprint); zone.AddEntity(next,12,10); next.GetPart<TileStateSourcePart>().Seed(zone,12,10);
            zone.TileState.AddCharge(10,10,1); Assert.True(TilePropagationSystem.IsConductive(zone,11,10));
            Assert.Greater(TilePropagationSystem.PropagateCharge(zone),0); Assert.Greater(zone.TileState.Charge(12,10),0);
            Assert.AreEqual(0,zone.TileState.Charge(13,10),"dry empty ground does not inherit the family");
        }
    }
}
