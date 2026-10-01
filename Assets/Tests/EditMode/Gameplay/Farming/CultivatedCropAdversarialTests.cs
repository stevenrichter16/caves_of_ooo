using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;

namespace CavesOfOoo.Tests
{
    public sealed class CultivatedCropAdversarialTests : CultivatedCropTestBase
    {
        [TestCase("seed")][TestCase("sprout")][TestCase("negative-stage")][TestCase("excess-stage")]
        [TestCase("distant")][TestCase("dead")][TestCase("actor-away")][TestCase("inactive-zone")]
        [TestCase("crop-away")][TestCase("foreign-crop-part")][TestCase("foreign-physics")][TestCase("carried")]
        [TestCase("equipped")][TestCase("legacy")][TestCase("untagged")]
        public void InvalidSourceRefusesHarvestWithoutOutputs(string change)
        {
            Cultivate();var e=Crop(stage:2);var p=e.GetPart<CropPart>();var physics=e.GetPart<PhysicsPart>();
            if(change=="seed")p.GrowthStage=0;if(change=="sprout")p.GrowthStage=1;if(change=="negative-stage")p.GrowthStage=-1;if(change=="excess-stage")p.GrowthStage=3;
            if(change=="distant")Zone.AddEntity(Actor,9,9);if(change=="dead")Actor.GetStat("Hitpoints").BaseValue=0;
            if(change=="actor-away")Zone.RemoveEntity(Actor);if(change=="crop-away")Zone.RemoveEntity(e);
            if(change=="inactive-zone")SettlementRuntime.ActiveZone=new Zone("other");
            if(change=="foreign-crop-part")p.ParentEntity=Actor;if(change=="foreign-physics")physics.ParentEntity=Actor;
            if(change=="carried")physics.InInventory=Actor;if(change=="equipped")physics.Equipped=Actor;
            if(change=="legacy")Set(p,"HarvestAtMaturity",false);if(change=="untagged")e.Tags.Remove("Crop");
            Assert.False(Harvest(e).Success);Assert.AreEqual(0,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));
        }
        [TestCase("zero-yield")][TestCase("negative-yield")][TestCase("excess-yield")][TestCase("negative-seeds")]
        [TestCase("excess-seeds")][TestCase("missing-seed")][TestCase("wrong-seed")][TestCase("missing-yield")]
        public void InvalidRecipeHoldsMaturePlant(string change)
        {
            Cultivate();var e=Crop(stage:2);var p=e.GetPart<CropPart>();
            if(change=="zero-yield")p.YieldCount=0;if(change=="negative-yield")p.YieldCount=-1;if(change=="excess-yield")p.YieldCount=int.MaxValue;
            if(change=="negative-seeds")Set(p,"SeedYieldCount",-1);if(change=="excess-seeds")Set(p,"SeedYieldCount",int.MaxValue);
            if(change=="missing-seed")Set(p,"SeedYieldBlueprint","absent");if(change=="wrong-seed")Set(p,"SeedYieldBlueprint","EmberwheatSeed");
            if(change=="missing-yield")p.YieldBlueprint="absent";
            Assert.False(Harvest(e).Success);Assert.NotNull(Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));
        }
        [TestCase("source-moved")][TestCase("part-replaced")][TestCase("recipe-changed")][TestCase("id-changed")]
        [TestCase("actor-moved")][TestCase("output-carried-later")][TestCase("duplicate-later-id")]
        public void FactoryCallbacksCannotRedirectHarvest(string change)
        {
            Cultivate();var e=Crop(stage:2);var part=e.GetPart<CropPart>();Entity first=null;int made=0;
            HookOutput(output=>{made++;if(made==1)first=output;if(made!=2)return;
                if(change=="source-moved")Zone.AddEntity(e,7,7);
                if(change=="part-replaced"){e.RemovePart(part);e.AddPart(new CropPart());}
                if(change=="recipe-changed")part.YieldCount=5;if(change=="id-changed")e.ID="changed";
                if(change=="actor-moved")Zone.AddEntity(Actor,9,9);
                if(change=="output-carried-later")first.GetPart<PhysicsPart>().InInventory=Actor;
                if(change=="duplicate-later-id")first.ID=output.ID;
            });
            Assert.False(Harvest(e).Success);Assert.NotNull(Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));
        }
        [Test] public void SeedCommandAfterActionThrowRestoresSeedAndRemovesPlant()
        {Cultivate();var seed=Seed();Actor.AddPart(new CultivatedCropAfterActionThrow());Assert.False(Plant(seed).Success);Assert.True(Actor.GetPart<InventoryPart>().Objects.Contains(seed));Assert.AreEqual(0,Count("CandyCarrotCrop"));}
        [Test] public void MatureMoistureDriesWithoutRegrowingOrAutoHarvesting()
        {Cultivate();var e=Crop();var p=e.GetPart<CropPart>();p.Water(8);Tick(4);Assert.AreEqual(2,p.GrowthStage);Assert.AreEqual(4,p.MoistureTicks);Tick(4);Assert.AreEqual(0,p.MoistureTicks);Assert.AreEqual(2,p.GrowthStage);Assert.AreEqual(0,p.TicksInStage);Assert.NotNull(Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));Assert.IsEmpty(e.GetPart<RenderPart>().BackgroundColor);}
        [TestCase("barren")][TestCase("water")][TestCase("not-plantable")][TestCase("foreign-marker")][TestCase("carried-ground")]
        public void UnsafeSoilMarkerDoesNotAuthorizePlanting(string change)
        {
            Cultivate();var seed=Seed();if(change=="barren"){var barren=new Entity();barren.SetTag("Barren");Zone.AddEntity(barren,5,5);}
            if(change=="water"){var pool=new Entity();pool.AddPart(new LiquidPoolPart());Zone.AddEntity(pool,5,5);}
            if(change=="not-plantable")Soil.Tags.Remove("Plantable");
            if(change=="foreign-marker")Soil.Parts.Single(p=>p.GetType()==SoilType).ParentEntity=Actor;
            if(change=="carried-ground")Soil.GetPart<PhysicsPart>().InInventory=Actor;
            Assert.False(Plant(seed).Success);Assert.True(Actor.GetPart<InventoryPart>().Objects.Contains(seed));Assert.AreEqual(0,Count("CandyCarrotCrop"));
        }
        [Test] public void PlantCreationCallbackCannotMoveActorThenPlantAtAbandonedCell()
        {
            Cultivate();var seed=Seed();Factory.Blueprints["CandyCarrotCrop"].Parts["CultivatedCropCreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();
            CultivatedCropCreationProbe.Created=output=>Zone.AddEntity(Actor,9,9);
            Assert.False(Plant(seed).Success);Assert.True(Actor.GetPart<InventoryPart>().Objects.Contains(seed));Assert.AreEqual(0,Count("CandyCarrotCrop"));
        }
        [Test] public void ASecondCommandCannotHarvestAnAlreadyPendingReceipt()
        {Cultivate();var crop=Crop(stage:2);var tx=new InventoryTransaction();var action=GameEvent.New("InventoryAction");action.SetParameter("Command","HarvestCultivatedCrop");action.SetParameter("Actor",Actor);action.SetParameter("Zone",Zone);action.SetParameter("InventoryTransaction",tx);crop.FireEvent(action);Assert.True(action.Handled);Assert.False(Harvest(crop).Success);tx.Rollback();Assert.NotNull(Zone.GetEntityCell(crop));Assert.AreEqual(0,Count("CandyCarrot"));action.Release();}
    }
}
