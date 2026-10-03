using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class ConnectedProgressFixture
    {
        protected EntityFactory Factory; protected OverworldZoneManager Manager; protected Zone Zone;
        protected Entity Actor, Cache; protected InventoryPart Pack=>Actor.GetPart<InventoryPart>();
        EntityFactory oldFactory; Zone oldActive;
        protected static Type Progress=>typeof(Entity).Assembly.GetType("CavesOfOoo.Core.ConnectedSpreadProgress");
        protected string Key=>(string)Manager.Exploration.GetType().GetProperty("WorldKey").GetValue(Manager.Exploration);
        protected static object Call(string name,params object[] args)
        {
            Assert.NotNull(Progress,"Committed exploration needs a finite saved accomplishment ledger.");
            var method=Progress.GetMethod(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);Assert.NotNull(method,name);
            try{return method.Invoke(null,args);}catch(TargetInvocationException error){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
        }
        [SetUp] public void SetUp()
        {
            oldFactory=CropSystem.Factory;oldActive=SettlementRuntime.ActiveZone;
            Factory=new EntityFactory();Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            Manager=OverworldZoneManager.CreateDetached(Factory,64,true);Zone=new Zone(GleanersCellarBuilder.ZoneID);Manager.SetActiveZone(Zone);SettlementRuntime.ActiveZone=Zone;CropSystem.Factory=Factory;
            Actor=Person();Assert.True(Zone.AddEntity(Actor,4,4));Cache=Factory.CreateEntity("Crate");Assert.True(Zone.AddEntity(Cache,5,4));
            RepairRecipeRegistry.LoadDefaults();
        }
        [TearDown] public void TearDown(){CropSystem.Factory=oldFactory;SettlementRuntime.ActiveZone=oldActive;}
        protected static Entity Person()
        {
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="Player"};actor.SetTag("Player");actor.SetTag("Creature");actor.AddPart(new PhysicsPart());actor.AddPart(new InventoryPart());
            foreach(var stat in new[]{("Hitpoints",10,40),("Experience",0,int.MaxValue),("Level",1,100),("SP",0,1000),("MP",0,1000),("Strength",20,100)})
                actor.Statistics[stat.Item1]=new Stat{Owner=actor,Name=stat.Item1,BaseValue=stat.Item2,Max=stat.Item3};
            return actor;
        }
        protected Entity Clay(int ordinal)
        {Assert.NotNull(Progress,"Missing finite exploration progression.");var e=Factory.CreateEntity("FireClay");Assert.True((bool)Call("BindClay",e,Key,ordinal));return e;}
        protected Entity Stores()
        {var first=Clay(0);var second=Clay(1);Assert.True(Cache.GetPart<ContainerPart>().AddItem(first));Assert.True(Cache.GetPart<ContainerPart>().AddItem(second));Assert.AreEqual(2,first.GetPart<StackerPart>().StackCount);return first;}
        protected bool Take(Entity item)=>InventorySystem.ExecuteCommand(new TakeFromContainerCommand(Cache,item),Actor,Zone).Success;
        protected bool Pick(Entity item)=>InventorySystem.ExecuteCommand(new PickupCommand(item),Actor,Zone).Success;
        protected int XP=>Actor.GetStatValue("Experience");
        protected void Move(string id)
        {Zone.RemoveEntity(Actor);Zone=new Zone(id);Manager.SetActiveZone(Zone);SettlementRuntime.ActiveZone=Zone;Assert.True(Zone.AddEntity(Actor,4,4));}
        protected Entity RepairTarget(bool pan=false)
        {
            Move(pan?KitchenBatchPart.KitchenZoneID:GleanersDistrict.SurfaceID);
            var e=Factory.CreateEntity(pan?"ConnectedBatchPan":"RepairLinedWell");Assert.True(Zone.AddEntity(e,5,4));Assert.True((bool)Call("BindRepair",e,Key));return e;
        }
        protected void Supply(string bp,int count)
        {var e=Factory.CreateEntity(bp);if(e.GetPart<StackerPart>() is StackerPart stack)stack.StackCount=count;Assert.True(Pack.AddObject(e));}
        protected Entity DryRoot()
        {
            Move(GleanersCellarBuilder.ZoneID);var root=Factory.CreateEntity("SootrootCrop");Assert.True(Zone.AddEntity(root,5,4));
            root.GetPart<CropPart>().GrowthStage=0;root.GetPart<CropPart>().MoistureTicks=0;root.GetPart<CropPart>().TicksPerStage=2;
            Assert.True((bool)Call("BindDryCrop",root,Key));return root;
        }
        protected bool Harvest(Entity root)=>InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(root,"HarvestCultivatedCrop"),Actor,Zone).Success;
        public sealed class ThrowAfter:Part
        {public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction"||e.ID=="AfterPickup")throw new InvalidOperationException("progress rollback");return true;}}
        public sealed class ThrowTaken:Part
        {public override bool HandleEvent(GameEvent e){if(e.ID=="Taken")throw new InvalidOperationException("progress rollback");return true;}}
    }
    public sealed class ConnectedSpreadProgressTests:ConnectedProgressFixture
    {
        [Test] public void BothActualCacheUnitsGrantThirtyEvenWhenTheyMergeIntoOrdinaryClay()
        {var clay=Stores();Supply("FireClay",4);Assert.True(Take(clay));Assert.AreEqual(30,XP);Assert.AreEqual(6,Pack.Objects.Single(e=>e.BlueprintName=="FireClay").GetPart<StackerPart>().StackCount);}
        [Test] public void SplitOriginalUnitsMustBothEnterPlayerCustody()
        {
            var clay=Stores();var split=clay.GetPart<StackerPart>().SplitStack(1);Assert.NotNull(split);Assert.True(Zone.AddEntity(split,4,4));
            Assert.True(Pick(split));Assert.AreEqual(0,XP);Assert.True(Take(clay));Assert.AreEqual(30,XP);
        }
        [Test] public void OriginalUnitReacquisitionDoesNotPretendTheOtherUnitWasRecovered()
        {
            var clay=Stores();var split=clay.GetPart<StackerPart>().SplitStack(1);Zone.AddEntity(split,4,4);Assert.True(Pick(split));Assert.AreEqual(0,XP);
            Assert.True(Pack.RemoveObject(split));Assert.True(Zone.AddEntity(split,4,4));Assert.True(Pick(split));Assert.AreEqual(0,XP);
            Assert.True(Take(clay));Assert.AreEqual(30,XP);var combined=Pack.Objects.Single(e=>e.BlueprintName=="FireClay");Pack.RemoveObject(combined);Zone.AddEntity(combined,4,4);Assert.True(Pick(combined));Assert.AreEqual(30,XP);
        }
        [Test] public void FirstRepairOfEitherDistrictServiceSharesOneAward()
        {
            var well=RepairTarget();Supply("FireClay",4);Assert.True(well.GetPart<RepairablePart>().TryRepair(Actor,Zone));Assert.AreEqual(30,XP);
            var pan=RepairTarget(true);Assert.True(pan.GetPart<RepairablePart>().TryRepair(Actor,Zone));Assert.AreEqual(30,XP);
        }
        [Test] public void InitiallyDryExactRootRequiresWaterGrowthAndActualHarvest()
        {
            var root=DryRoot();Assert.False(Harvest(root));Assert.AreEqual(0,XP);var crop=root.GetPart<CropPart>();crop.Water(4);
            for(int i=0;i<4;i++)CropSystem.OnTickEnd(Zone);Assert.AreEqual(2,crop.GrowthStage);Assert.AreEqual(0,XP);
            Assert.True(Harvest(root));Assert.AreEqual(30,XP);Assert.False(Harvest(root));Assert.AreEqual(30,XP);
        }
        [Test] public void FirstCompletedPaidMealAwardsItsCommissionerOnlyOnActualCollection()
        {
            using(var f=new ConnectedKitchenFixture())
            {
                Manager=OverworldZoneManager.CreateDetached(f.Factory,64,true);Manager.SetActiveZone(f.Zone);f.Player.Statistics["Strength"]=new Stat{Owner=f.Player,Name="Strength",BaseValue=10,Max=100};foreach(var key in new[]{"Experience","Level","SP","MP"})f.Player.Statistics[key]=new Stat{Owner=f.Player,Name=key,BaseValue=key=="Level"?1:0,Max=int.MaxValue};
                Assert.True((bool)Call("BindKitchen",f.Pan,Key));Assert.True(f.Start());Assert.AreEqual(0,f.Player.GetStatValue("Experience"));f.Advance(120);Assert.AreEqual("Ready",f.State,"blocked="+f.Pan.GetPart<KitchenBatchPart>().BlockedReason+" tick="+WorldClock.CurrentTick+" start="+f.Pan.GetPart<KitchenBatchPart>().StartTick+" due="+f.Pan.GetPart<KitchenBatchPart>().DueTick);Assert.AreEqual(0,f.Player.GetStatValue("Experience"));
                Assert.True(f.Zone.MoveEntity(f.Player,9,11));var meal=f.Finished.Contents.Single();Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Pickup,meal),f.Player,f.Zone).Success);Assert.AreEqual(30,f.Player.GetStatValue("Experience"));
                f.Give("Emberwheat",2);f.Give("ClaspbeanPulp",1);f.Advance(1);f.Zone.MoveEntity(f.Player,9,10);Assert.True(f.Start());f.Advance(120);f.Zone.MoveEntity(f.Player,9,11);
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Pickup,f.Finished.Contents.Single()),f.Player,f.Zone).Success);Assert.AreEqual(30,f.Player.GetStatValue("Experience"));
            }
        }
        [Test] public void FourThirtyPointOutcomesReachLevelTwoWithTheExistingRewards()
        {
            var clay=Stores();Assert.True(Take(clay));var well=RepairTarget();Assert.True(well.GetPart<RepairablePart>().TryRepair(Actor,Zone));
            var root=DryRoot();root.GetPart<CropPart>().Water(4);for(int i=0;i<4;i++)CropSystem.OnTickEnd(Zone);Assert.True(Harvest(root));Assert.AreEqual(90,XP);
            // Reuse the real paid-job fixture, moving this same player into its physical graph.
            using(var f=new ConnectedKitchenFixture())
            {
                Factory.Blueprints["FieldMeal"]=f.Factory.Blueprints["FieldMeal"];f.Zone.RemoveEntity(f.Player);Zone.RemoveEntity(Actor);Assert.True(f.Zone.AddEntity(Actor,9,10));Manager.SetActiveZone(f.Zone);Zone=f.Zone;SettlementRuntime.ActiveZone=Zone;
                Supply("Emberwheat",2);Supply("ClaspbeanPulp",1);TradeSystem.SetDrams(Actor,10);Assert.True((bool)Call("BindKitchen",f.Pan,Key));
                Assert.True(f.Pan.GetPart<KitchenBatchPart>().TryStart(Actor,Zone));f.Advance(120);Zone.MoveEntity(Actor,9,11);
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Pickup,f.Finished.Contents.Single()),Actor,Zone).Success);
                Assert.AreEqual(2,Actor.GetStatValue("Level"));Assert.AreEqual(5,XP);Assert.AreEqual(42,Actor.GetStat("Hitpoints").Max);Assert.AreEqual(42,Actor.GetStatValue("Hitpoints"));Assert.AreEqual(1,Actor.GetStatValue("SP"));Assert.AreEqual(1,Actor.GetStatValue("MP"));
            }
        }
    }
}
