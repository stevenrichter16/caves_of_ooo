using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class ExamineLedgerQueueTests
 {
  HotbarSaveFixture scope;
  [SetUp] public void Setup(){scope=new HotbarSaveFixture(false,false);MessageLog.Clear();}
  [TearDown] public void Cleanup(){scope.Dispose();}
  static MethodInfo Put => typeof(MessageLog).GetMethod("AddPresentedAnnouncement",BindingFlags.Public|BindingFlags.Static);
  static MethodInfo Take => typeof(MessageLog).GetMethod("ConsumeAnnouncementWithPresentation",BindingFlags.NonPublic|BindingFlags.Static);
  static void Add(string text,AnnouncementDocument doc){Assert.NotNull(Put,"Shared reader needs metadata carried with the actual queue entry.");Put.Invoke(null,new object[]{text,doc});}
  static object[] Consume(){Assert.NotNull(Take);var args=new object[]{null,null};string text=(string)Take.Invoke(null,args);return new object[]{text,args[0],args[1]};}
  [Test] public void ActualLevelUpPublishesALevelUpSubjectWithoutChangingRewardOrMessage()
  {
   var entity=new Entity();entity.Statistics["Experience"]=new Stat{Name="Experience",BaseValue=115,Max=100000};entity.Statistics["Level"]=new Stat{Name="Level",BaseValue=1};
   LevelingSystem.CheckLevelUp(entity,null);var result=Consume();
   Assert.AreEqual("You advance to level 2!",result[0]);var doc=result[2] as AnnouncementDocument;
   Assert.NotNull(doc);Assert.AreEqual("LEVEL UP",doc.Kind);Assert.AreEqual("Level 2",doc.Title);Assert.AreEqual(2,entity.GetStatValue("Level"));
   LevelingSystem.CheckLevelUp(entity,null);Assert.False(MessageLog.HasPendingAnnouncement);
  }
  static Entity AdvancingActor(int xp=115)
  {
   var entity=new Entity { ID=Guid.NewGuid().ToString("N") };
   entity.Statistics["Experience"]=new Stat{Name="Experience",BaseValue=xp,Max=100000};
   entity.Statistics["Level"]=new Stat{Name="Level",BaseValue=1,Max=99};
   entity.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=4,Max=10};
   entity.Statistics["MP"]=new Stat{Name="MP",BaseValue=2,Max=99};
   entity.Statistics["SP"]=new Stat{Name="SP",BaseValue=4,Max=99};
   return entity;
  }
  static string Fact(AnnouncementDocument document,string label)=>document.Facts.SingleOrDefault(f=>f.Label==label)?.Value;
  [Test] public void ActualLevelUpLedgerShowsGrantedRewardsAndCurrentTotalsWithoutChangingLegacyText()
  {
   var entity=AdvancingActor();LevelingSystem.CheckLevelUp(entity,null);var result=Consume();
   var doc=(AnnouncementDocument)result[2];Assert.NotNull(doc);
   Assert.AreEqual("You advance to level 2!",result[0]);Assert.AreEqual(result[0],MessageLog.GetLast());
   Assert.AreEqual(12,entity.GetStat("Hitpoints").Max);Assert.AreEqual(12,entity.GetStatValue("Hitpoints"));
   Assert.AreEqual(3,entity.GetStatValue("MP"));Assert.AreEqual(5,entity.GetStatValue("SP"));
   Assert.AreEqual("+2; now 12",Fact(doc,"Maximum HP"));Assert.AreEqual("12 / 12",Fact(doc,"Health"));
   Assert.AreEqual("+1; now 3",Fact(doc,"MP gained"));Assert.AreEqual("+1; now 5",Fact(doc,"SP gained"));
   Assert.AreEqual(4,doc.Facts.Count);Assert.False(MessageLog.HasPendingAnnouncement);
  }
  [TestCase("Hitpoints")] [TestCase("MP")] [TestCase("SP")]
  public void LevelUpLedgerDoesNotInventRewardsForMissingStats(string missing)
  {
   var entity=AdvancingActor();entity.Statistics.Remove(missing);LevelingSystem.CheckLevelUp(entity,null);
   var doc=(AnnouncementDocument)Consume()[2];Assert.Null(entity.GetStat(missing));
   Assert.AreEqual(missing=="Hitpoints"?null:"+2; now 12",Fact(doc,"Maximum HP"));
   Assert.AreEqual(missing=="Hitpoints"?null:"12 / 12",Fact(doc,"Health"));
   Assert.AreEqual(missing=="MP"?null:"+1; now 3",Fact(doc,"MP gained"));
   Assert.AreEqual(missing=="SP"?null:"+1; now 5",Fact(doc,"SP gained"));
  }
  [TestCase(0)] [TestCase(5)]
  public void DeadActorRewardLedgerDoesNotClaimHealingEvenWhenModifiersLeavePositiveHP(int bonus)
  {
   var entity=AdvancingActor();entity.GetStat("Hitpoints").Bonus=bonus;
   CombatSystem.HandleDeath(entity,null,null);Assert.True(CombatSystem.IsDeathHandled(entity));MessageLog.Clear();
   LevelingSystem.CheckLevelUp(entity,null);var doc=(AnnouncementDocument)Consume()[2];
   Assert.AreEqual(0,entity.GetStat("Hitpoints").BaseValue);Assert.AreEqual(bonus,entity.GetStatValue("Hitpoints"));
   Assert.AreEqual("+2; now 12",Fact(doc,"Maximum HP"));Assert.AreEqual(bonus+" / 12",Fact(doc,"Health"));
   StringAssert.DoesNotContain("heal",doc.ToPlainText().ToLowerInvariant().Replace("health", ""));
   StringAssert.DoesNotContain("restored",doc.ToPlainText().ToLowerInvariant());
  }
  [Test] public void MultipleLevelUpsKeepIndividualRewardSnapshotsAndExactAnnouncementOrder()
  {
   var entity=AdvancingActor(335);LevelingSystem.CheckLevelUp(entity,null);
   var first=Consume();var second=Consume();var a=(AnnouncementDocument)first[2];var b=(AnnouncementDocument)second[2];
   Assert.AreEqual("You advance to level 2!",first[0]);Assert.AreEqual("You advance to level 3!",second[0]);
   Assert.AreEqual("+2; now 12",Fact(a,"Maximum HP"));Assert.AreEqual("+2; now 14",Fact(b,"Maximum HP"));
   Assert.AreEqual("+1; now 3",Fact(a,"MP gained"));Assert.AreEqual("+1; now 4",Fact(b,"MP gained"));
   Assert.AreEqual("+1; now 5",Fact(a,"SP gained"));Assert.AreEqual("+1; now 6",Fact(b,"SP gained"));
   Assert.AreEqual(3,entity.GetStatValue("Level"));Assert.False(MessageLog.HasPendingAnnouncement);
  }
  [TestCase(true)] [TestCase(false)]
  public void RewardTotalsUseTheActualComputedStatsIncludingCapsAndPenalties(bool capped)
  {
   var entity=AdvancingActor();var hp=entity.GetStat("Hitpoints");var mp=entity.GetStat("MP");var sp=entity.GetStat("SP");
   hp.Penalty=2;
   if(capped){mp.Max=2;sp.Max=4;}else{mp.Penalty=1;sp.Penalty=2;}
   LevelingSystem.CheckLevelUp(entity,null);var doc=(AnnouncementDocument)Consume()[2];
   Assert.AreEqual(3,mp.BaseValue);Assert.AreEqual(5,sp.BaseValue);Assert.AreEqual(10,hp.Value);
   Assert.AreEqual("+1; now "+mp.Value,Fact(doc,"MP gained"));
   Assert.AreEqual("+1; now "+sp.Value,Fact(doc,"SP gained"));Assert.AreEqual("10 / 12",Fact(doc,"Health"));
  }
  sealed class RemoveGrantedStat : Part
  {
   public string StatName;
   public override string Name=>"LedgerRemoveGrantedStat";
   public override bool HandleEvent(GameEvent e){if(e.ID=="GainedMP")ParentEntity.Statistics.Remove(StatName);return true;}
  }
  [TestCase("Hitpoints")] [TestCase("MP")]
  public void RewardCallbacksCannotLeaveFactsForStatsWhichNoLongerExist(string removed)
  {
   var entity=AdvancingActor();entity.AddPart(new RemoveGrantedStat { StatName=removed });
   LevelingSystem.CheckLevelUp(entity,null);var doc=(AnnouncementDocument)Consume()[2];Assert.Null(entity.GetStat(removed));
   Assert.AreEqual(removed=="Hitpoints"?null:"+2; now 12",Fact(doc,"Maximum HP"));
   Assert.AreEqual(removed=="Hitpoints"?null:"12 / 12",Fact(doc,"Health"));
   Assert.AreEqual(removed=="MP"?null:"+1; now 3",Fact(doc,"MP gained"));
   Assert.AreEqual("+1; now 5",Fact(doc,"SP gained"));
  }
  [Test] public void SameTextDifferentSubjectMetadata_RemainsAttachedToActualQueueEntry()
  {
   var a=new AnnouncementDocument("Well","EXAMINE");var b=new AnnouncementDocument("Level 2","LEVEL UP");
   Add("same text",a);Add("same text",b);Assert.AreSame(a,Consume()[2]);Assert.AreSame(b,Consume()[2]);Assert.False(MessageLog.HasPendingAnnouncement);
  }
  [Test] public void StructuredEntryPreservesExactAuthoredLogAndSnapshotText()
  {
   string text="Quoted text\n\nwith  two spaces.";Add(text,new AnnouncementDocument("Title"));
   Assert.AreEqual(text,MessageLog.GetLast());CollectionAssert.AreEqual(new[]{text},MessageLog.GetPendingAnnouncementsSnapshot());Assert.AreEqual(text,Consume()[0]);
  }
  [Test] public void PlainEntryAfterStructuredEntryHasNoStaleSubject()
  {
   Add("first",new AnnouncementDocument("A"));MessageLog.AddAnnouncement("second");Assert.NotNull(Consume()[2]);Assert.Null(Consume()[2]);
  }
  [Test] public void ClearAndRestoreDiscardTransientSubjects()
  {
   Add("first",new AnnouncementDocument("A"));var entries=MessageLog.GetAllEntries();var pending=MessageLog.GetPendingAnnouncementsSnapshot();
   MessageLog.Restore(entries,pending,MessageLog.FlashStamp,100);Assert.Null(Consume()[2]);
   Add("second",new AnnouncementDocument("B"));MessageLog.Clear();MessageLog.AddAnnouncement("new");Assert.Null(Consume()[2]);
  }
  [Test] public void LegacyConsumptionCannotShiftNextStructuredSubject()
  {
   Add("first",new AnnouncementDocument("A"));var b=new AnnouncementDocument("B");Add("second",b);
   Assert.AreEqual("first",MessageLog.ConsumeAnnouncement());Assert.AreSame(b,Consume()[2]);
  }
 }
}
