using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class WellKeeperPurificationDepthTests
    {
        const string Id = SettlementSiteDefinitions.StartingVillageZoneId;
        const string Condition = "WellKeeperKnowsPurification";
        EntityFactory factory; Entity keeper,player; SettlementManager manager,prior; int turn; Zone zone;
        [SetUp] public void Setup()
        {
            prior=SettlementManager.Current; ConversationActions.Reset(); ConversationPredicates.Reset();
            factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            keeper=factory.CreateEntity("WellKeeper"); keeper.Properties["SettlementId"]=Id;
            player=factory.CreateEntity("Player");
            zone=new Zone(Id); Assert.True(zone.AddEntity(player,10,10)); Assert.True(zone.AddEntity(keeper,11,10));
            manager=new SettlementManager(()=>turn,_=>new PointOfInterest(POIType.Village,"Sill","Villagers",1));
            manager.GetOrCreateSettlement(Id,new PointOfInterest(POIType.Village,"Sill","Villagers",1));
        }
        [TearDown] public void Cleanup()
        {
            ConversationActions.Reset(); ConversationPredicates.Reset();
            typeof(SettlementManager).GetProperty("Current").SetValue(null,prior);
        }
        Entity Copy(string knowledge="KnowsPurifyWater")
        {
            var copy=factory.CreateEntity("GrimoireCopy"); copy.GetPart<GrimoirePart>().KnowledgeProperty=knowledge;
            player.GetPart<InventoryPart>().AddObject(copy); return copy;
        }
        bool Teach()=>ConversationActions.TryExecute("TeachWellPurification",keeper,player,"");
        RepairableSiteState Site=>manager.GetSite(Id,SettlementSiteDefinitions.MainWellSiteId);
        [Test] public void CorrectCopyIsPaidOnceAndEstablishesPersistentCare()
        {
            var copy=Copy(); Assert.True(Teach()); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(copy));
            Assert.True(manager.HasCondition(Id,Condition)); Assert.AreEqual(RepairStage.TemporarilyPurified,Site.Stage);
            int deadline=Site.RelapseAtTurn.Value; turn=deadline+500; Assert.True(manager.AdvanceSettlement(Id,turn));
            Assert.AreEqual(RepairStage.TemporarilyPurified,Site.Stage); Assert.Greater(Site.RelapseAtTurn.Value,turn);
            Assert.False(manager.HasCondition(Id,SettlementSiteDefinitions.ImprovedWellCondition),"rite is not structural repair");
        }
        [TestCase("")][TestCase("KnowsMendingRite")][TestCase("KnowsKindleRite")]
        public void WrongCopyIsNotConsumed(string knowledge)
        {
            var copy=Copy(knowledge); Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(copy)); Assert.False(manager.HasCondition(Id,Condition));
        }
        [Test] public void OriginalBookIsNotPayment()
        {
            var book=factory.CreateEntity("PurifyWaterGrimoire"); Assert.NotNull(book); player.GetPart<InventoryPart>().AddObject(book);
            Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(book));
        }
        [Test] public void RepeatTeachingDoesNotConsumeAnotherCopy()
        {
            Copy(); Assert.True(Teach()); var second=Copy(); Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(second));
        }
        [Test] public void SelectsEligibleCopyBehindAnUnrelatedCopy()
        {
            var wrong=Copy("KnowsMendingRite"); var right=Copy(); Assert.True(Teach());
            Assert.True(player.GetPart<InventoryPart>().Objects.Contains(wrong)); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(right));
        }
        [TestCase(RepairStage.StableRepair)][TestCase(RepairStage.ImprovedWithCaretaker)]
        public void RepairedWellDoesNotTakeAnUnneededCopy(RepairStage stage)
        {
            Site.Stage=stage; var copy=Copy(); Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(copy)); Assert.AreEqual(stage,Site.Stage);
        }
        [Test] public void UntrainedTemporaryWellStillRelapses()
        {
            player.Properties["KnowsPurifyWater"]="true"; Assert.True(manager.ApplyRepairMethod(Id,SettlementSiteDefinitions.MainWellSiteId,RepairMethodId.PurifySpell,player));
            turn=Site.RelapseAtTurn.Value; Assert.True(manager.AdvanceSettlement(Id,turn)); Assert.AreEqual(RepairStage.Fouled,Site.Stage);
        }
        [Test] public void TrainingDoesNotLeakToAnotherSettlement()
        {
            Copy(); Assert.True(Teach()); string other="Overworld.11.11.0";
            var town=manager.GetOrCreateSettlement(other,new PointOfInterest(POIType.Village,"Other","Villagers",1)); Assert.False(town.HasCondition(Condition));
        }
        [Test] public void UnknownSettlementOrUnrelatedSpeakerCannotTakeCopy()
        {
            var copy=Copy(); keeper.Properties["SettlementId"]="missing"; Assert.False(Teach());
            keeper.Properties["SettlementId"]=Id; keeper.BlueprintName="Merchant"; Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(copy));
        }
        [Test] public void PredicateOffersOnlyEffectiveTeaching()
        {
            Assert.False(ConversationPredicates.Evaluate("IfCanTeachWellPurification",keeper,player,"")); Copy();
            Assert.True(ConversationPredicates.Evaluate("IfCanTeachWellPurification",keeper,player,"")); Assert.True(Teach());
            Assert.False(ConversationPredicates.Evaluate("IfCanTeachWellPurification",keeper,player,""));
        }

        [Test] public void SavedSettlementRetainsCareAndRenewal()
        {
            Copy(); Assert.True(Teach());
            using (var stream=new MemoryStream())
            {
                var flags=BindingFlags.Static|BindingFlags.NonPublic;
                typeof(SaveGraphSerializer).GetMethod("SaveSettlementManager",flags).Invoke(null,new object[]{manager,new SaveWriter(stream)});
                stream.Position=0;
                var loaded=(SettlementManager)typeof(SaveGraphSerializer).GetMethod("LoadSettlementManager",flags).Invoke(null,new object[]{new SaveReader(stream,null)});
                Assert.True(loaded.HasCondition(Id,Condition)); var site=loaded.GetSite(Id,SettlementSiteDefinitions.MainWellSiteId);
                Assert.True(loaded.AdvanceSettlement(Id,site.RelapseAtTurn.Value+1000));
                Assert.AreEqual(RepairStage.TemporarilyPurified,site.Stage); Assert.AreNotSame(Site,site);
            }
        }
        [Test] public void PaidCopyKeepsOtherIdenticalCopiesInTheStack()
        {
            var first=Copy(); Copy(); Assert.AreEqual(2,first.GetPart<StackerPart>().StackCount);
            Assert.True(Teach()); Assert.AreEqual(1,first.GetPart<StackerPart>().StackCount); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(first));
        }
        [TestCase("knowledge")][TestCase("skill")][TestCase("learn")][TestCase("known")]
        public void DifferentBookPayloadsNeverMerge(string field)
        {
            var first=Copy(); var other=factory.CreateEntity("GrimoireCopy"); var a=first.GetPart<GrimoirePart>(); var b=other.GetPart<GrimoirePart>();
            b.KnowledgeProperty=a.KnowledgeProperty;
            if(field=="knowledge") b.KnowledgeProperty="Different"; if(field=="skill") b.SkillClassName="Different";
            if(field=="learn") b.LearnMessage="Different"; if(field=="known") b.AlreadyKnownMessage="Different";
            Assert.False(first.GetPart<StackerPart>().CanStackWith(other)); Assert.False(other.GetPart<StackerPart>().CanStackWith(first));
        }
        [TestCase("empty")][TestCase("equipped")][TestCase("wrong-owner")][TestCase("no-copy-tag")][TestCase("skill-overrides")]
        [TestCase("equipment-cache")][TestCase("body-equipment")]
        public void UnavailableOrAmbiguousCopyCannotPay(string mode)
        {
            var copy=Copy(); if(mode=="empty")copy.GetPart<StackerPart>().StackCount=0;
            if(mode=="equipped") copy.GetPart<PhysicsPart>().Equipped=player;
            if(mode=="equipment-cache") player.GetPart<InventoryPart>().EquippedItems["Hand"]=copy;
            if(mode=="body-equipment") player.GetPart<Body>().GetPartsByType("Hand").First()._Equipped=copy;
            if(mode=="wrong-owner") copy.GetPart<PhysicsPart>().InInventory=keeper;
            if(mode=="no-copy-tag") copy.Tags.Remove("GrimoireCopy");
            if(mode=="skill-overrides") copy.GetPart<GrimoirePart>().SkillClassName="Cryomancy_Frostbind";
            Assert.False(Teach()); Assert.True(player.GetPart<InventoryPart>().Objects.Contains(copy)); Assert.False(manager.HasCondition(Id,Condition));
        }
        [TestCase("keeper-removed")][TestCase("player-removed")][TestCase("different-zone")][TestCase("distant")]
        [TestCase("foreign-settlement")]
        public void StaleConversationCannotTeachOrTakePayment(string mode)
        {
            var copy=Copy(); Assert.True(WellKeeperPurification.CanTeach(keeper,player));
            if(mode=="keeper-removed") zone.RemoveEntity(keeper);
            if(mode=="player-removed") zone.RemoveEntity(player);
            if(mode=="different-zone") {zone.RemoveEntity(keeper); new Zone("elsewhere").AddEntity(keeper,11,10);}
            if(mode=="distant") {zone.RemoveEntity(keeper); zone.AddEntity(keeper,20,10);}
            if(mode=="foreign-settlement") zone.ZoneID="elsewhere";
            Assert.False(WellKeeperPurification.CanTeach(keeper,player)); Assert.False(Teach());
            Assert.True(player.GetPart<InventoryPart>().Objects.Contains(copy)); Assert.False(manager.HasCondition(Id,Condition));
        }
        [Test] public void ManualRepairAfterTrainingRemainsPermanentAndDistinct()
        {
            Copy(); Assert.True(Teach()); var inv=player.GetPart<InventoryPart>();
            inv.AddObject(factory.CreateEntity(SettlementRepairDefinitions.WellMaintenanceManualBlueprint)); inv.AddObject(factory.CreateEntity(SettlementRepairDefinitions.SilverSandBlueprint));
            Assert.True(manager.ApplyRepairMethod(Id,SettlementSiteDefinitions.MainWellSiteId,RepairMethodId.ManualRepair,player));
            Assert.AreEqual(RepairStage.StableRepair,Site.Stage); Assert.Null(Site.RelapseAtTurn);
            Assert.False(manager.AdvanceSettlement(Id,100000)); Assert.AreEqual(RepairStage.StableRepair,Site.Stage);
        }
    }
}
