using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastExpeditionAdversarialTests
    {
        HotbarSaveFixture scope; EntityFactory factory; OverworldZoneManager manager;
        Zone field,town; Entity player,farra;
        [SetUp] public void Setup()
        {
            scope=new HotbarSaveFixture(false,false);factory=GrovelandsCompositionTests.Factory();
            manager=OverworldZoneManager.CreateDetached(factory,64);field=manager.GetZone(MorrowfastExpedition.FieldZoneId);
            town=manager.GetZone(MorrowfastSceneRuntime.ZoneID);player=factory.CreateEntity("Player");town.AddEntity(player,36,14);
            farra=MorrowfastSceneRuntime.FindOwner(town,"farra-sprig");StoryletPart.Current=new StoryletPart();
            StoryletPart.LocalPlayer=player;SettlementRuntime.ActiveZone=town;MorrowfastContent.EnsureRegistered();
        }
        [TearDown] public void Teardown(){ConversationManager.EndConversation();scope.Dispose();}
        Entity Cache=>field.GetAllEntities().Single(e=>e.ID==MorrowfastExpedition.CacheId);
        Entity Parcel=>Cache.GetPart<ContainerPart>().Contents.Single(e=>e.ID==MorrowfastExpedition.ParcelId);
        void Accept(){Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"accept"));}
        Entity Carry()
        {
            var p=Parcel;var c=Cache;var at=field.GetEntityCell(c);town.RemoveEntity(player);field.AddEntity(player,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(c,p),player,field).Success);
            field.RemoveEntity(player);town.AddEntity(player,36,14);return p;
        }
        [Test] public void RealContainerTakeAdvancesRecoverObjectiveButDoesNotFinishOrPay()
        {
            Accept();int before=TradeSystem.GetDrams(player);Carry();
            Assert.AreEqual(1,StoryletPart.Current.GetActiveQuests().Single(q=>q.QuestId==MorrowfastExpedition.QuestId).CurrentStageIndex);
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId));Assert.AreEqual(before,TradeSystem.GetDrams(player));
        }
        [Test] public void DetachedCachedTownCannotAcceptOrPay()
        {manager.CachedZones.Remove(town.ZoneID);Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"accept"));}
        [Test] public void ChangedDestinationCannotAdvertiseAnErrand()
        {
            manager.WorldMap.SetPOI(2,6,new PointOfInterest(POIType.Village,"Changed"));
            Assert.IsFalse(MorrowfastExpedition.CanConversation(farra,player,"accept"));Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"accept"));
        }
        [Test] public void FullInventoryDropsRewardOnActualReturnCellWithoutLosingIt()
        {
            Accept();Carry();player.GetPart<InventoryPart>().MaxWeight=0;
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.AreEqual(1,town.GetCell(36,14).Objects.Count(e=>e.BlueprintName=="FireClay"));
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.AreEqual(1,town.GetCell(36,14).Objects.Count(e=>e.BlueprintName=="FireClay"));
        }
        [Test] public void MissingRewardDependencyLeavesParcelQuestAndPurseUntouched()
        {
            Accept();var parcel=Carry();int before=TradeSystem.GetDrams(player);factory.Blueprints.Remove("FireClay");
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.AreSame(player,parcel.GetPart<PhysicsPart>().InInventory);Assert.IsTrue(StoryletPart.Current.IsQuestActive(MorrowfastExpedition.QuestId));
            Assert.AreEqual(before,TradeSystem.GetDrams(player));Assert.AreEqual(0,player.GetIntProperty("MorrowfastDryGoodsRewarded"));
        }
        [Test] public void MissingTableExaminationDoesNotConsumeParcelWithoutResolution()
        {
            Accept();Carry();var table=MorrowfastSceneRuntime.FindOwner(town,"guest-supper-table");table.RemovePart(table.GetPart<ExaminablePart>());
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver"));Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId));
        }
        [TestCase("Render")] [TestCase("Examinable")]
        public void MalformedCarriedParcelRejectsBeforeAnyRewardOrTransfer(string part)
        {
            Accept();var parcel=Carry();parcel.RemovePart(parcel.Parts.Single(p=>p.Name==part));int before=TradeSystem.GetDrams(player);
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));Assert.AreSame(player,parcel.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(before,TradeSystem.GetDrams(player));Assert.AreEqual(0,player.GetIntProperty("MorrowfastDryGoodsRewarded"));
        }
        [TestCase("WovenBasket","Physics")] [TestCase("WovenBasket","Examinable")]
        [TestCase("Sack","Render")] [TestCase("Sack","Examinable")]
        public void MissingAuthoredDependencyDoesNotPartiallyInstallCache(string bp,string part)
        {
            var fresh=new Zone(MorrowfastExpedition.FieldZoneId);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)fresh.AddEntity(factory.CreateEntity("Grass"),x,y);
            factory.Blueprints[bp].Parts.Remove(part);
            Assert.IsFalse(MorrowfastExpedition.TryInstall(fresh,factory));
            Assert.IsFalse(fresh.GetAllEntities().Any(e=>e.ID==MorrowfastExpedition.CacheId));
            Assert.AreEqual(0,fresh.GetCell(0,0).Objects.Single().GetIntProperty("MorrowfastDryGoodsInstalled"));
        }
        [Test] public void RejectedContainerCapacityCannotInstallAnEmptyOneShotCache()
        {
            var fresh=new Zone(MorrowfastExpedition.FieldZoneId);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)fresh.AddEntity(factory.CreateEntity("Grass"),x,y);
            factory.Blueprints["WovenBasket"].Parts["Container"]["MaxItems"]="0";
            Assert.IsFalse(MorrowfastExpedition.TryInstall(fresh,factory));
            Assert.IsFalse(fresh.GetAllEntities().Any(e=>e.ID==MorrowfastExpedition.CacheId));
            Assert.AreEqual(0,fresh.GetCell(0,0).Objects.Single().GetIntProperty("MorrowfastDryGoodsInstalled"));
            factory.Blueprints["WovenBasket"].Parts["Container"]["MaxItems"]="6";
            Assert.IsTrue(MorrowfastExpedition.TryInstall(fresh,factory));
            Assert.AreEqual(1,fresh.GetAllEntities().Single(e=>e.ID==MorrowfastExpedition.CacheId).GetPart<ContainerPart>().Contents.Count);
        }
        [TestCase(1729)] [TestCase(729490642)] [TestCase(9031)]
        public void AlternateSeedsKeepOneSupplyCacheOnTheEasternSide(int seed)
        {
            var other=OverworldZoneManager.CreateDetached(factory,seed);var generated=other.GetZone(MorrowfastExpedition.FieldZoneId);
            var cache=generated.GetAllEntities().Single(e=>e.ID==MorrowfastExpedition.CacheId);
            Assert.GreaterOrEqual(generated.GetEntityCell(cache).X,Zone.Width/2);
            Assert.IsFalse(cache.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(1,cache.GetPart<ContainerPart>().Contents.Count);
            Assert.IsFalse(MorrowfastExpedition.TryInstall(generated,factory));
        }
        [Test] public void DestroyedCacheSpillsRecoverableClothAndDoesNotRegenerate()
        {
            var cache=Cache;var parcel=Parcel;var at=field.GetEntityCell(cache);Accept();
            DestructionSystem.RouteDamage(cache,new Damage(999),player,field);
            Assert.IsNull(field.GetEntityCell(cache));Assert.IsNotNull(field.GetEntityCell(parcel));
            town.RemoveEntity(player);field.AddEntity(player,at.X,at.Y);Assert.IsTrue(InventorySystem.Pickup(player,parcel,field));
            field.RemoveEntity(player);town.AddEntity(player,36,14);
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.IsFalse(manager.GetZone(field.ZoneID).GetAllEntities().Any(e=>e.ID==MorrowfastExpedition.CacheId));
        }
        [Test] public void CompletedGraphRoundTripKeepsRewardLatchAndReturnedCloth()
        {
            Accept();var parcel=Carry();Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            var savedPlayer=PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            var savedCloth=PartRoundTripHelper.RoundTripEntityViaTokenGraph(parcel);
            Assert.AreEqual(1,savedPlayer.GetIntProperty("MorrowfastDryGoodsRewarded"));
            Assert.AreEqual(1,savedCloth.GetIntProperty("MorrowfastDryGoodsReturned"));Assert.IsFalse(savedCloth.GetPart<PhysicsPart>().Takeable);
            town.RemoveEntity(player);player=savedPlayer;town.AddEntity(player,36,14);StoryletPart.LocalPlayer=player;
            StoryletPart.Current=new StoryletPart();Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"accept"));
        }
        [Test] public void ArrivalHintNamesRealEastwardTownOnlyWhenWesternCacheExists()
        {
            var method=typeof(MorrowfastExpedition).GetMethod("ArrivalHint");Assert.NotNull(method);
            var text=method.Invoke(null,new object[]{field}) as string;
            StringAssert.Contains("east",text);StringAssert.Contains("Morrowfast",text);StringAssert.Contains("Farra",text);
            Assert.IsNull(method.Invoke(null,new object[]{town}));field.RemoveEntity(Cache);
            Assert.IsNull(method.Invoke(null,new object[]{field}));
        }
        [Test] public void ActualHaulWithinFieldEndsAtChunkBorderAndClothMustBeCarried()
        {
            var cache=Cache;town.RemoveEntity(player);field.AddEntity(player,78,12);field.MoveEntity(cache,77,12);
            Assert.IsTrue(WorldInteractionSystem.GatherActions(cache,player).Any(a=>a.Command==HandlingPart.HaulCommand));
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,cache,field));
            field.MoveEntity(player,79,12);field.MoveEntity(cache,78,12);
            var moved=ZoneTransitionSystem.TransitionPlayer(player,field,TransitionDirection.East,79,12,manager,manager.WorldMap);
            Assert.IsTrue(moved.Success);Assert.AreEqual(town.ZoneID,moved.NewZone.ZoneID);
            Assert.IsNull(DragSystem.GetDragged(player));Assert.IsNotNull(field.GetEntityCell(cache));
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
        }
        [Test] public void DifferentPlayerOrDeadGiverCannotAcquireErrand()
        {
            var other=factory.CreateEntity("Player");town.AddEntity(other,36,15);
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,other,"accept"));
            farra.GetStat("Hitpoints").BaseValue=0;Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"accept"));
        }
    }
}
