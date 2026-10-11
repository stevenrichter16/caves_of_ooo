using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioPhysicalPageTests:WorldAudioWorkFixture
 {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  GameObject uiRoot;InputHandler input;AnnouncementUI reader;Entity book;
  List<MessageLog.Entry> oldMessages;List<string> oldQueue;int oldFlash,oldSerial;
  [SetUp]public void SetupReader()
  {
   oldMessages=MessageLog.GetAllEntries();oldQueue=MessageLog.GetPendingAnnouncementsSnapshot();oldFlash=MessageLog.FlashStamp;oldSerial=MessageLog.NextSerialValue;MessageLog.Clear();
   uiRoot=new GameObject("physical-page fixture");uiRoot.SetActive(false);input=uiRoot.AddComponent<InputHandler>();reader=uiRoot.AddComponent<AnnouncementUI>();input.PlayerEntity=Actor;input.CurrentZone=Zone;input.AnnouncementUI=reader;
   book=Carry("Codex01");Assert.NotNull(book.GetPart<ReadableDocumentPart>());Router.SetWorld(Zone,Actor,false);
  }
  [TearDown]public void CleanupReader(){UnityEngine.Object.DestroyImmediate(uiRoot);MessageLog.Restore(oldMessages,oldQueue,oldFlash,oldSerial);}
  void OpenQueued(){Assert.True((bool)typeof(InputHandler).GetMethod("TryOpenAnnouncement",Private).Invoke(input,null));Assert.True(reader.IsOpen);Assert.Greater(reader.PageCount,1,"actual authored codex has real multiple pages");}
  void Read(){Assert.True(Act(book,"ReadDocument"));OpenQueued();}
  void Flip(){Assert.True(reader.GoToPage(1));Assert.AreEqual(1,Audio.AcceptedSoundCount);CollectionAssert.AreEqual(new[]{"Page take 0"},Assigned());}
  [Test]public void ActualPhysicalReaderSoundsOnlyAChangedVisiblePage()
  {Read();Assert.AreEqual(0,Audio.AcceptedSoundCount,"opening does not invent a page turn");Flip();Audio.Update(1);Assert.True(reader.GoToPage(0));Assert.AreEqual(2,Audio.AcceptedSoundCount);Assert.True(Pack.Contains(book));Assert.AreEqual(16,Audio.AllocatedSources);Assert.AreEqual(0,Audio.CombatFocusCount);}
  [TestCase("same")][TestCase("negative")][TestCase("too-far")][TestCase("close")]
  public void NonTurnDoesNotReplayPaper(string action)
  {Read();if(action=="same")Assert.True(reader.GoToPage(0));if(action=="negative")Assert.False(reader.GoToPage(-1));if(action=="too-far")Assert.False(reader.GoToPage(reader.PageCount));if(action=="close"){reader.Close();Assert.False(reader.GoToPage(1));Read();}Assert.AreEqual(0,Audio.AcceptedSoundCount);Flip();}
  [Test]public void ByteIdenticalGenericAnnouncementDoesNotInheritPhysicalOrigin()
  {var d=ReadableDocumentCatalog.Get(book.GetPart<ReadableDocumentPart>().DocumentId);MessageLog.AddAnnouncement(d.Title+"\n\n"+d.Text);OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);reader.Close();Read();Flip();}
  [TestCase(true)][TestCase(false)]public void QueueOrderCarriesItsOwnReadOrigin(bool documentFirst)
  {var d=ReadableDocumentCatalog.Get(book.GetPart<ReadableDocumentPart>().DocumentId);string text=d.Title+"\n\n"+d.Text;if(documentFirst){Assert.True(Act(book,"ReadDocument"));MessageLog.AddAnnouncement(text);}else{MessageLog.AddAnnouncement(text);Assert.True(Act(book,"ReadDocument"));}OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(documentFirst?1:0,Audio.AcceptedSoundCount);reader.Close();Audio.Update(1);OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(1,Audio.AcceptedSoundCount);}
  [Test]public void RestoredPendingTextHasNoReconstructedPhysicalAudioAuthority()
  {Assert.True(Act(book,"ReadDocument"));var entries=MessageLog.GetAllEntries();var pending=MessageLog.GetPendingAnnouncementsSnapshot();MessageLog.Restore(entries,pending,MessageLog.FlashStamp,MessageLog.NextSerialValue);OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);reader.Close();Read();Flip();}
  [TestCase("item")][TestCase("actor")][TestCase("document-part")][TestCase("document-id")]
  public void StalePhysicalContextCannotSoundAfterTheReaderOpens(string change)
  {Read();if(change=="item")Pack.RemoveObject(book);if(change=="actor")Zone.RemoveEntity(Actor);if(change=="document-part"){book.RemovePart(book.GetPart<ReadableDocumentPart>());book.AddPart(new ReadableDocumentPart{DocumentId="codex-01"});}if(change=="document-id")book.GetPart<ReadableDocumentPart>().DocumentId="codex-02";Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);}
  [Test]public void MutedPageDoesNotQueueAReplay()
  {Read();WorldAudioSettings.SfxVolume=0;Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);WorldAudioSettings.SfxVolume=1;Audio.Update(1);Assert.AreEqual(0,Audio.AcceptedSoundCount);Assert.True(reader.GoToPage(0));Assert.AreEqual(1,Audio.AcceptedSoundCount);}
  [Test]public void ReopeningGenericReaderClearsThePreviousPhysicalContext()
  {Read();string text=(string)typeof(AnnouncementUI).GetField("_message",Private).GetValue(reader);reader.Open(text);Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);reader.Close();Read();Flip();}
  [Test]public void FailedOuterReadKeepsLegacyTextButCannotAuthorizePaperSound()
  {
   FailAfter();Assert.False(Act(book,"ReadDocument"));OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount,"queued legacy text must not bless a failed command");
   reader.Close();Actor.RemovePart(Actor.GetPart<FiftyWorldThrowAfterPart>());Read();Flip();
  }
  [TestCase(false)][TestCase(true)]public void PhysicalOriginArmsOnlyWhenItsActualParentTransactionCommits(bool commit)
  {
   var tx=new InventoryTransaction();Stage(Actor,book,Zone,"ReadDocument",tx);OpenQueued();Assert.True(reader.GoToPage(1));Assert.AreEqual(0,Audio.AcceptedSoundCount);
   if(commit)tx.Commit();else tx.Rollback();Assert.True(reader.GoToPage(0));Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);tx.Commit();Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);
  }
  [Test]public void DirectSuccessfulPublicReadArmsItsPhysicalContextImmediately()
  {Assert.True(book.GetPart<ReadableDocumentPart>().TryRead(Actor,Zone));OpenQueued();Flip();}
  [Test]public void GenericSemanticCannotImitateThePreparedPageRecording()
  {Router.SetWorld(Zone,Actor,true);WorldAudioHooks.Emit(WorldAudioCue.Page,Actor,Zone,10,10);Assert.AreEqual(0,Audio.AcceptedSoundCount);Read();Flip();}
 }
}
