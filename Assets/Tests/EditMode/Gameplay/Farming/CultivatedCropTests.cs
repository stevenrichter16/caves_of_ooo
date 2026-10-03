using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class CultivatedCropTestBase
    {
        protected EntityFactory Factory;
        protected Zone Zone;
        protected Entity Actor, Soil;
        EntityFactory oldCropFactory, oldSeedFactory;
        Zone oldZone;
        protected static Type SoilType => typeof(Part).Assembly.GetType("CavesOfOoo.Core.CultivatedSoilPart");
        protected static void Set(object target,string field,object value)
        {var info=target.GetType().GetField(field);Assert.NotNull(info,"Missing crop API: "+field);info.SetValue(target,value);}
        protected static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field).GetValue(target);
        [SetUp] public void Setup()
        {
            Factory=new EntityFactory();Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            Factory.RegisterPartType<CultivatedCropCreationProbe>();
            oldCropFactory=CropSystem.Factory;oldSeedFactory=SeedPart.Factory;oldZone=SettlementRuntime.ActiveZone;
            CropSystem.Factory=SeedPart.Factory=Factory;Zone=new Zone("cultivation-test");SettlementRuntime.ActiveZone=Zone;
            Actor=new Entity{ID="cultivation-actor",BlueprintName="Player"};Actor.SetTag("Player");Actor.SetTag("Creature");
            Actor.AddPart(new PhysicsPart());Actor.AddPart(new RenderPart{DisplayName="farmer"});Actor.AddPart(new InventoryPart{MaxWeight=200});
            Actor.Statistics["Hitpoints"]=new Stat{Owner=Actor,Name="Hitpoints",BaseValue=30,Max=30};
            Assert.True(Zone.AddEntity(Actor,5,5));Soil=Factory.CreateEntity("Grass");Assert.True(Zone.AddEntity(Soil,5,5));
            Zone.GetCell(5,5).IsVisible=true;CultivatedCropCreationProbe.Created=null;
        }
        [TearDown] public void Cleanup()
        {CropSystem.Factory=oldCropFactory;SeedPart.Factory=oldSeedFactory;SettlementRuntime.ActiveZone=oldZone;CultivatedCropCreationProbe.Created=null;}
        protected void Cultivate(){Assert.NotNull(SoilType,"Cultivated soil marker missing");Soil.AddPart((Part)Activator.CreateInstance(SoilType));}
        protected Entity Crop(bool standing=true,int stage=0)
        {
            var e=Factory.CreateEntity("CandyCarrotCrop");var p=e.GetPart<CropPart>();
            if(standing){Set(p,"HarvestAtMaturity",true);Set(p,"SeedYieldBlueprint","CandyCarrotSeed");Set(p,"SeedYieldCount",1);}
            p.GrowthStage=stage;p.TicksPerStage=2;p.StageGlyphsRaw=".,t,T";p.StageColorsRaw="&w,&g,&G";
            Assert.True(Zone.AddEntity(e,5,5));return e;
        }
        protected InventoryCommandResult Harvest(Entity e)=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(e,"HarvestCultivatedCrop"),Actor,Zone);
        protected Entity Seed(bool cultivated=true)
        {
            var e=Factory.CreateEntity("CandyCarrotSeed");if(cultivated)Set(e.GetPart<SeedPart>(),"RequireCultivatedSoil",true);
            Assert.True(Actor.GetPart<InventoryPart>().AddObject(e));return e;
        }
        protected InventoryCommandResult Plant(Entity e)=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(e,"PlantSeed"),Actor,Zone);
        protected void Tick(int n){for(int i=0;i<n;i++)CropSystem.OnTickEnd(Zone);}
        protected int Count(string bp)=>Zone.GetReadOnlyEntities().Count(e=>e.BlueprintName==bp);
        protected void HookOutput(Action<Entity> action)
        {Factory.Blueprints["CandyCarrot"].Parts["CultivatedCropCreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();CultivatedCropCreationProbe.Created=action;}
    }
    public sealed class CultivatedCropCreationProbe:Part
    {
        public static Action<Entity> Created;
        public override void Initialize()=>Created?.Invoke(ParentEntity);
    }
    public sealed class CultivatedCropAfterActionThrow:Part
    {public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("crop rollback probe");return true;}}

    public sealed class CultivatedCropTests:CultivatedCropTestBase
    {
        [Test] public void NewSeedRequiresCultivatedSoil_GrassAloneRefusesWithoutConsumption()
        {var seed=Seed();Assert.False(Plant(seed).Success);Assert.True(Actor.GetPart<InventoryPart>().Objects.Contains(seed));Cultivate();Assert.True(Plant(seed).Success);Assert.False(Actor.GetPart<InventoryPart>().Objects.Contains(seed));}
        [Test] public void LegacySeedStillPlantsWithoutCultivation()
        {Assert.True(Plant(Seed(false)).Success);Assert.AreEqual(1,Count("CandyCarrotCrop"));}
        [Test] public void StandingCropWaitsForHarvest_ThenYieldsProduceAndSeedOnReusableBed()
        {Cultivate();var e=Crop();var p=e.GetPart<CropPart>();Tick(10);Assert.AreEqual(0,p.TicksInStage);p.Water(4);Tick(3);Assert.AreEqual(1,p.GrowthStage);Tick(1);Assert.AreEqual(2,p.GrowthStage);Assert.AreEqual("T",e.GetPart<RenderPart>().RenderString);Tick(30);Assert.NotNull(Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));Assert.True(Harvest(e).Success);Assert.IsNull(Zone.GetEntityCell(e));Assert.AreEqual(2,Count("CandyCarrot"));Assert.AreEqual(1,Count("CandyCarrotSeed"));Assert.NotNull(Zone.GetEntityCell(Soil));Assert.False(Harvest(e).Success);Assert.AreEqual(2,Count("CandyCarrot"));}
        [Test] public void FullInventoryDoesNotDestroyHarvest_AllOutputsStayOnRow()
        {Cultivate();var e=Crop(stage:2);Actor.GetPart<InventoryPart>().MaxWeight=0;Assert.True(Harvest(e).Success);Assert.AreEqual(2,Count("CandyCarrot"));Assert.AreEqual(1,Count("CandyCarrotSeed"));Assert.AreEqual(0,Actor.GetPart<InventoryPart>().Objects.Count);}
        [Test] public void CommandAfterActionExceptionRestoresExactCropAndCell_NoOutputs()
        {Cultivate();var e=Crop(stage:2);Actor.AddPart(new CultivatedCropAfterActionThrow());Assert.False(Harvest(e).Success);Assert.AreSame(Zone.GetCell(5,5),Zone.GetEntityCell(e));Assert.AreEqual(2,e.GetPart<CropPart>().GrowthStage);Assert.AreEqual(0,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));}
        [Test] public void ExplicitTransactionRollbackRestoresOwnerAndOutputs()
        {Cultivate();var e=Crop(stage:2);var tx=new InventoryTransaction();var action=GameEvent.New("InventoryAction");action.SetParameter("Command","HarvestCultivatedCrop");action.SetParameter("Actor",Actor);action.SetParameter("Zone",Zone);action.SetParameter("InventoryTransaction",tx);e.FireEvent(action);Assert.True(action.Handled);Assert.IsNull(Zone.GetEntityCell(e));tx.Rollback();Assert.AreSame(Zone.GetCell(5,5),Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));action.Release();}
        [TestCase(0)][TestCase(1)][TestCase(2)] public void GrowthStageAndCultivationRoundTrip(int stage)
        {Cultivate();var e=Crop(stage:stage);var p=e.GetPart<CropPart>();p.Water(7);p.TicksInStage=1;var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(e);var q=loaded.GetPart<CropPart>();Assert.True(Get<bool>(q,"HarvestAtMaturity"));Assert.AreEqual("CandyCarrotSeed",Get<string>(q,"SeedYieldBlueprint"));Assert.AreEqual(1,Get<int>(q,"SeedYieldCount"));Assert.AreEqual(stage,q.GrowthStage);Assert.AreEqual(7,q.MoistureTicks);Assert.AreEqual(1,q.TicksInStage);var soil=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Soil);Assert.True(soil.Parts.Any(x=>x.GetType()==SoilType));}
        [Test] public void LegacyMaturityStillDropsAndRemovesCrop()
        {var e=Crop(false);e.GetPart<CropPart>().Water(4);Tick(4);Assert.IsNull(Zone.GetEntityCell(e));Assert.AreEqual(2,Count("CandyCarrot"));Assert.AreEqual(0,Count("CandyCarrotSeed"));}
        [Test] public void MalformedSeedTargetDoesNotConsumeOrSpawnUnrelatedOwner()
        {var seed=Seed(false);seed.GetPart<SeedPart>().CropBlueprint="CandyCarrot";Assert.False(Plant(seed).Success);Assert.True(Actor.GetPart<InventoryPart>().Objects.Contains(seed));Assert.AreEqual(0,Count("CandyCarrot"));}
        [Test] public void LegacyFailedYieldPlacementHoldsCropAndRollsBackPartialProduce()
        {var e=Crop(false);e.GetPart<CropPart>().Water(4);int created=0;HookOutput(output=>{if(++created==2)typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(output,new Zone("foreign"));});Tick(4);Assert.NotNull(Zone.GetEntityCell(e));Assert.AreEqual(0,Count("CandyCarrot"));}
        [Test] public void RealSchedulerPlayerBoundaryUsesElapsedClock_NpcDoesNot()
        {
            var e=Crop();var crop=e.GetPart<CropPart>();crop.Water(6);
            var oldWorld=TurnManager.World;var activeField=typeof(TurnManager).GetField("<Active>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var oldActive=TurnManager.Active;
            try
            {
                var manager=new TurnManager();manager.AddEntity(Actor);var npc=new Entity{ID="crop-clock-npc"};manager.AddEntity(npc);
                var world=new Entity();world.AddPart(new CropSystemPart());TurnManager.World=world;
                // The prior assertion (+1 for EndTurn alone) pinned the old
                // action-based policy. Runtime now advances only elapsed ticks.
                crop.GrowthTimingVersion=0;crop.LastGrowthWorldTick=-1;crop.GrowthWetTickRemainder=0;
                CropTime.ReconcileZone(Zone,manager.TickCount);
                manager.EndTurn(npc,Zone);Assert.AreEqual(0,crop.TicksInStage);Assert.AreEqual(6,crop.MoistureTicks);
                manager.EndTurn(Actor,Zone);Assert.AreEqual(0,crop.TicksInStage);Assert.AreEqual(6,crop.MoistureTicks);
                manager.AdvanceClock(10);
                manager.EndTurn(Actor,Zone);Assert.AreEqual(1,crop.TicksInStage);Assert.AreEqual(5,crop.MoistureTicks);
                manager.AdvanceClock(10);
                manager.EndTurn(Actor,Zone);Assert.AreEqual(1,crop.GrowthStage);Assert.AreEqual(0,crop.TicksInStage);
                manager.EndTurn(npc,Zone);Assert.AreEqual(4,crop.MoistureTicks);
            }
            finally {TurnManager.World=oldWorld;activeField.SetValue(null,oldActive);}
        }
        [Test] public void PlayerRoundGateDoesNotCountOtherActorTurnsOrForeignArea()
        {
            var old=TurnManager.Active;var activeField=typeof(TurnManager).GetField("<Active>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
            var clock=new TurnManager();var tick=GameEvent.New("TickEnd");
            try
            {
                var e=Crop(false);e.GetPart<CropPart>().Water(20);var world=new Entity();world.AddPart(new CropSystemPart());
                clock.AdvanceClock(10);tick.SetParameter("Actor",new Entity());world.FireEvent(tick);Assert.AreEqual(0,e.GetPart<CropPart>().TicksInStage);
                tick.SetParameter("Actor",Actor);world.FireEvent(tick);Assert.AreEqual(1,e.GetPart<CropPart>().TicksInStage);
                SettlementRuntime.ActiveZone=new Zone("away");clock.AdvanceClock(10);world.FireEvent(tick);Assert.AreEqual(1,e.GetPart<CropPart>().TicksInStage);
            }
            finally{tick.Release();activeField.SetValue(null,old);}
        }
    }
}
