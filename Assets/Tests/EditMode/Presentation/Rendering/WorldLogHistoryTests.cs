using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WorldLogHistoryTests
    {
        List<MessageLog.Entry> saved; List<string> pending; int flash, next;
        Func<int> provider; Action<string> callback;
        [SetUp] public void SetUp()
        {
            saved=MessageLog.GetAllEntries(); pending=MessageLog.GetPendingAnnouncementsSnapshot();
            flash=MessageLog.FlashStamp; next=MessageLog.NextSerialValue;
            provider=MessageLog.TickProvider; callback=MessageLog.OnMessage;
            MessageLog.OnMessage=null; MessageLog.TickProvider=null; MessageLog.Clear();
        }
        [TearDown] public void TearDown()
        { MessageLog.Restore(saved,pending,flash,next); MessageLog.TickProvider=provider; MessageLog.OnMessage=callback; }
        void Add(int count) { for(int i=0;i<count;i++) MessageLog.Add("entry-"+i.ToString("D3")); }
        static string Body(WorldLogHistoryModel m) => string.Join("\n",m.VisibleLines.Where(x=>!x.IsHeader).Select(x=>x.Text));
        [TestCase(-10,3,3)] [TestCase(0,-1,0)] [TestCase(0,0,0)]
        [TestCase(int.MaxValue,int.MaxValue,0)] [TestCase(95,int.MaxValue,5)] [TestCase(0,int.MaxValue,64)]
        public void RangeClampsSafelyClearsOutputAndNeverCopiesOver64(int start,int count,int expected)
        {
            Add(100); var output=new List<MessageLog.Entry>{new MessageLog.Entry("stale",1,1)};
            Assert.AreEqual(expected,MessageLog.CopyEntryRange(start,count,output)); Assert.AreEqual(expected,output.Count);
            if(expected>0) Assert.AreEqual("entry-"+Math.Max(0,start).ToString("D3"),output[0].Text);
        }
        [Test] public void RangeIsDetachedAndDoesNotTouchRawRecordsOrAnnouncementState()
        {
            MessageLog.TickProvider=()=>97; MessageLog.AddAnnouncement("Quoted\r\n\nexact  spaces.");
            var raw=MessageLog.GetAllEntries(); var queue=MessageLog.GetPendingAnnouncementsSnapshot();
            int serial=MessageLog.NextSerialValue,stamp=MessageLog.FlashStamp; long revision=MessageLog.Revision;
            var output=new List<MessageLog.Entry>(); MessageLog.CopyEntryRange(0,64,output); output.Clear();
            CollectionAssert.AreEqual(raw,MessageLog.GetAllEntries()); CollectionAssert.AreEqual(queue,MessageLog.GetPendingAnnouncementsSnapshot());
            Assert.AreEqual(serial,MessageLog.NextSerialValue); Assert.AreEqual(stamp,MessageLog.FlashStamp); Assert.AreEqual(revision,MessageLog.Revision);
        }
        [Test] public void RevisionChangesBeforeObserversWhileGenerationChangesOnlyOnReplacement()
        {
            long revision=MessageLog.Revision,generation=MessageLog.HistoryGeneration; long observed=-1;
            MessageLog.OnMessage=_=>observed=MessageLog.Revision; MessageLog.Add("new");
            Assert.Greater(observed,revision); Assert.AreEqual(generation,MessageLog.HistoryGeneration);
            var raw=MessageLog.GetAllEntries(); revision=MessageLog.Revision;
            MessageLog.Restore(raw,new List<string>(),0,1); Assert.Greater(MessageLog.Revision,revision); Assert.Greater(MessageLog.HistoryGeneration,generation);
            generation=MessageLog.HistoryGeneration; MessageLog.Clear(); Assert.Greater(MessageLog.HistoryGeneration,generation);
        }
        [Test] public void MoreThan30OldEntriesAreReachableChronologicallyInBoundedWindows()
        {
            Add(137); var model=new WorldLogHistoryModel(52,12); model.Open(); Assert.Greater(model.FirstEntryIndex,30);
            Assert.LessOrEqual(model.LastReadCount,WorldLogHistoryModel.WindowEntries);
            model.Oldest(); var texts=new List<string>(); int safety=0;
            do {texts.AddRange(model.VisibleLines.Where(x=>!x.IsHeader).Select(x=>x.Text)); Assert.LessOrEqual(model.LastReadCount,16);} while(model.Newer()&&++safety<200);
            CollectionAssert.AreEqual(Enumerable.Range(0,137).Select(i=>"entry-"+i.ToString("D3")).ToArray(),texts);
        }
        [Test] public void RawTicksGroupOnlyContiguousEntriesAndDoNotMergeRepeatedBodies()
        {
            int tick=3;MessageLog.TickProvider=()=>tick;MessageLog.Add("same");MessageLog.Add("same");tick=4;MessageLog.Add("same");tick=3;MessageLog.Add("different");
            var raw=MessageLog.GetAllEntries();var model=new WorldLogHistoryModel(52,28);model.Open();
            CollectionAssert.AreEqual(new[]{"Tick 3","Tick 4","Tick 3"},model.VisibleLines.Where(x=>x.IsHeader).Select(x=>x.Text).ToArray());
            CollectionAssert.AreEqual(new[]{"same","same","same","different"},model.VisibleLines.Where(x=>!x.IsHeader).Select(x=>x.Text).ToArray());
            CollectionAssert.AreEqual(raw,MessageLog.GetAllEntries());Assert.False(model.VisibleLines.Any(x=>x.Text.StartsWith("Turn ")));
        }
        [Test] public void LongParagraphTailAndBlankParagraphRemainReachableWithoutTruncatingRawText()
        {
            string text=new string('q',3000)+"\n\nTAIL_END";MessageLog.AddAnnouncement(text);
            var model=new WorldLogHistoryModel(17,7);model.Open();model.Oldest();var lines=new List<string>();int safety=0;
            do {lines.AddRange(model.VisibleLines.Where(x=>!x.IsHeader).Select(x=>x.Text));}while(model.Newer()&&++safety<100);
            Assert.AreEqual(3000,lines.Sum(x=>x.Count(c=>c=='q')));Assert.Contains("",lines);Assert.Contains("TAIL_END",lines);
            Assert.AreEqual(text,MessageLog.GetLast());Assert.True(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void RepeatedRefreshDoesNotGatherOrReformatAndAppendKeepsTheReadingAnchor()
        {
            Add(100);var model=new WorldLogHistoryModel(20,8);model.Open();model.Older();int index=model.TopEntryIndex,offset=model.TopCharacterOffset,builds=model.BuildCount;
            for(int i=0;i<100;i++)Assert.False(model.Refresh());Assert.AreEqual(builds,model.BuildCount);
            MessageLog.Add("new arrival");Assert.True(model.Refresh());Assert.AreEqual(index,model.TopEntryIndex);Assert.AreEqual(offset,model.TopCharacterOffset);Assert.AreEqual(builds,model.BuildCount);Assert.Greater(model.NewerEntryCount,0);
            model.Latest();Assert.AreEqual(MessageLog.Count,model.EndEntryIndex);
        }
        [Test] public void EqualCountAndSerialRestoreInvalidatesAllOldWrappedText()
        {
            Add(40);var model=new WorldLogHistoryModel();model.Open();int builds=model.BuildCount;
            var entries=MessageLog.GetAllEntries();for(int i=0;i<entries.Count;i++)entries[i]=new MessageLog.Entry("replacement-"+i,2,entries[i].Serial);
            MessageLog.Restore(entries,new List<string>{"pending"},3,40);Assert.True(model.Refresh());Assert.Greater(model.BuildCount,builds);
            Assert.False(Body(model).Contains("entry-"));StringAssert.Contains("replacement-",Body(model));Assert.True(MessageLog.HasPendingAnnouncement);
        }
        [Test] public void ClearAndSameCountRepopulateCannotResurrectOldSerialAnchors()
        {
            Add(40);var model=new WorldLogHistoryModel();model.Open();model.Older();MessageLog.Clear();
            for(int i=0;i<40;i++)MessageLog.Add("fresh-"+i);Assert.True(model.Refresh());Assert.False(Body(model).Contains("entry-"));
            MessageLog.Clear();Assert.True(model.Refresh());Assert.Zero(model.TotalEntries);Assert.False(model.Older());Assert.False(model.Newer());
        }
        [Test] public void ReflowKeepsSameRawEntryAndCharacterRatherThanSameRowNumber()
        {
            MessageLog.Add(new string('x',900));var model=new WorldLogHistoryModel(20,8);model.Open();model.Oldest();Assert.True(model.Newer());int index=model.TopEntryIndex,offset=model.TopCharacterOffset;
            model.Resize(13,6);Assert.AreEqual(index,model.TopEntryIndex);Assert.LessOrEqual(model.TopCharacterOffset,offset);Assert.Less(offset-model.TopCharacterOffset,13);
            Assert.True(model.VisibleLines.All(x=>x.Text.Length<=13));
        }
        [Test] public void RestoredDuplicateSerialsStayDistinctByRawIndex()
        {
            MessageLog.Restore(new List<MessageLog.Entry>{new MessageLog.Entry(new string('a',250),1,7),new MessageLog.Entry(new string('b',250),1,7)},null,0,8);
            var model=new WorldLogHistoryModel(10,5);model.Open();while(model.TopEntryIndex==0&&model.Newer()){}Assert.AreEqual(1,model.TopEntryIndex);
            model.Resize(9,6);Assert.AreEqual(1,model.TopEntryIndex);Assert.True(Body(model).Contains("b"));
        }
        [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void NarrowReaderDoesNotClipOrMislabelFullRawTickValue(int tick)
        {
            MessageLog.TickProvider=()=>tick;MessageLog.Add("body");var model=new WorldLogHistoryModel(8,26);model.Open();
            Assert.True(model.VisibleLines.All(x=>x.Text.Length<=8));
            Assert.AreEqual("Tick "+tick,string.Concat(model.VisibleLines.Where(x=>x.IsHeader).Select(x=>x.Text)));
        }
        [Test] public void EmptyHistoryAndInvalidLayoutAreFiniteAndStillReadOnly()
        {
            var model=new WorldLogHistoryModel(-1,int.MaxValue);model.Open();Assert.Zero(model.VisibleLines.Count);Assert.False(model.Older());Assert.False(model.Newer());
            model.Resize(int.MaxValue,0);MessageLog.Add("one");model.Latest();Assert.LessOrEqual(model.VisibleLines.Count,40);Assert.AreEqual(1,MessageLog.Count);
        }
    }
}
