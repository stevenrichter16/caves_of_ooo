using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Imports before implementation: missing API is an assertion, not a compiler error.
    /// Once present, every test continues into the real range/model/input behavior.</summary>
    public sealed class WorldLogHistoryBaselineTests
    {
        const BindingFlags PublicStatic=BindingFlags.Public|BindingFlags.Static;
        List<MessageLog.Entry> saved;List<string> pending;List<object> origins;
        int flash,next;Func<int> provider;Action<string> callback;
        [SetUp] public void Setup()
        {
            saved=MessageLog.GetAllEntries();pending=MessageLog.GetPendingAnnouncementsSnapshot();origins=Origins();
            flash=MessageLog.FlashStamp;next=MessageLog.NextSerialValue;provider=MessageLog.TickProvider;callback=MessageLog.OnMessage;
            MessageLog.OnMessage=null;MessageLog.TickProvider=null;MessageLog.Clear();
        }
        [TearDown] public void Cleanup()
        {
            MessageLog.Restore(saved,pending,flash,next);
            var field=typeof(MessageLog).GetField("AnnouncementOrigins",BindingFlags.NonPublic|BindingFlags.Static);
            if(field!=null&&origins!=null)
            {
                object queue=field.GetValue(null);field.FieldType.GetMethod("Clear").Invoke(queue,null);
                foreach(object origin in origins)field.FieldType.GetMethod("Enqueue").Invoke(queue,new[]{origin});
            }
            MessageLog.TickProvider=provider;MessageLog.OnMessage=callback;
        }
        static List<object> Origins()
        {
            var field=typeof(MessageLog).GetField("AnnouncementOrigins",BindingFlags.NonPublic|BindingFlags.Static);
            return field==null?null:((IEnumerable)field.GetValue(null)).Cast<object>().ToList();
        }
        static long Stamp(string name)
        {
            var property=typeof(MessageLog).GetProperty(name,PublicStatic);
            Assert.NotNull(property,"Required public MessageLog."+name);Assert.AreEqual(typeof(long),property.PropertyType);
            Assert.NotNull(property.GetGetMethod());return (long)property.GetValue(null);
        }
        static MethodInfo Range()
        {
            var method=typeof(MessageLog).GetMethod("CopyEntryRange",PublicStatic,null,new[]{typeof(int),typeof(int),typeof(List<MessageLog.Entry>)},null);
            Assert.NotNull(method,"Required bounded public chronological range API");Assert.AreEqual(typeof(int),method.ReturnType);return method;
        }
        static object Model(int width=52,int rows=12)
        {
            var type=typeof(InputHandler).Assembly.GetType("CavesOfOoo.Rendering.WorldLogHistoryModel");
            Assert.NotNull(type,"Required real on-demand full-history model");return Activator.CreateInstance(type,new object[]{width,rows});
        }
        static object Call(object owner,string name,params object[] args)
        {
            var method=owner.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(method,name);return method.Invoke(owner,args);
        }
        static T Value<T>(object owner,string name)
        {
            var property=owner.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance);
            Assert.NotNull(property,name);return (T)property.GetValue(owner);
        }
        static List<string> Body(object model)
        {
            return ((IEnumerable)Value<object>(model,"VisibleLines")).Cast<object>()
                .Where(line=>!(bool)line.GetType().GetField("IsHeader").GetValue(line))
                .Select(line=>(string)line.GetType().GetField("Text").GetValue(line)).ToList();
        }
        static void Add(int count){for(int i=0;i<count;i++)MessageLog.Add("entry-"+i.ToString("D3"));}

        [Test] public void BoundedRangeClampsAndPreservesEveryRawFieldAndPendingOrigin()
        {
            MessageLog.TickProvider=()=>123;Add(100);MessageLog.AddAnnouncement("Quoted\r\n\nexact  spaces.");
            var raw=MessageLog.GetAllEntries();var notices=MessageLog.GetPendingAnnouncementsSnapshot();var priorOrigins=Origins();
            int serial=MessageLog.NextSerialValue,stamp=MessageLog.FlashStamp;var output=new List<MessageLog.Entry>{new MessageLog.Entry("stale",-1,-1)};var method=Range();
            Assert.AreEqual(64,method.Invoke(null,new object[]{-5,int.MaxValue,output}));CollectionAssert.AreEqual(raw.Take(64).ToList(),output);
            Assert.AreEqual(0,method.Invoke(null,new object[]{int.MaxValue,int.MaxValue,output}));Assert.IsEmpty(output);
            Assert.AreEqual(0,method.Invoke(null,new object[]{0,-1,output}));Assert.IsEmpty(output);
            Assert.AreEqual(0,method.Invoke(null,new object[]{0,64,null}));
            CollectionAssert.AreEqual(raw,MessageLog.GetAllEntries());CollectionAssert.AreEqual(notices,MessageLog.GetPendingAnnouncementsSnapshot());
            CollectionAssert.AreEqual(priorOrigins,Origins());Assert.AreEqual(serial,MessageLog.NextSerialValue);Assert.AreEqual(stamp,MessageLog.FlashStamp);
        }
        [Test] public void RevisionPrecedesObserversAndEqualCountRestoreChangesGeneration()
        {
            long revision=Stamp("Revision"),generation=Stamp("HistoryGeneration"),observed=-1;
            MessageLog.OnMessage=_=>observed=Stamp("Revision");MessageLog.Add("old");Assert.Greater(observed,revision);Assert.AreEqual(generation,Stamp("HistoryGeneration"));
            var raw=MessageLog.GetAllEntries();revision=Stamp("Revision");raw[0]=new MessageLog.Entry("replacement",raw[0].Tick,raw[0].Serial);
            MessageLog.Restore(raw,new List<string>(),0,MessageLog.NextSerialValue);
            Assert.Greater(Stamp("Revision"),revision);Assert.Greater(Stamp("HistoryGeneration"),generation);
            generation=Stamp("HistoryGeneration");MessageLog.Clear();Assert.Greater(Stamp("HistoryGeneration"),generation);
        }
        [Test] public void OlderThanThirtyEntriesRemainChronologicalAcrossBoundedPages()
        {
            Add(97);var model=Model();Call(model,"Open");Assert.Greater(Value<int>(model,"FirstEntryIndex"),30);
            Call(model,"Oldest");var actual=new List<string>();int pages=0;
            do{actual.AddRange(Body(model));Assert.LessOrEqual(Value<int>(model,"LastReadCount"),16);Assert.Less(++pages,100);}while((bool)Call(model,"Newer"));
            CollectionAssert.AreEqual(Enumerable.Range(0,97).Select(i=>"entry-"+i.ToString("D3")).ToArray(),actual);
        }
        [Test] public void IdleAndAppendRefreshKeepReadingIdentityWithoutRewrapping()
        {
            Add(90);var model=Model(20,8);Call(model,"Open");Call(model,"Older");
            int index=Value<int>(model,"TopEntryIndex"),offset=Value<int>(model,"TopCharacterOffset"),builds=Value<int>(model,"BuildCount");
            for(int i=0;i<50;i++)Assert.False((bool)Call(model,"Refresh"));Assert.AreEqual(builds,Value<int>(model,"BuildCount"));
            MessageLog.Add("new arrival");Assert.True((bool)Call(model,"Refresh"));Assert.AreEqual(builds,Value<int>(model,"BuildCount"));
            Assert.AreEqual(index,Value<int>(model,"TopEntryIndex"));Assert.AreEqual(offset,Value<int>(model,"TopCharacterOffset"));Assert.Greater(Value<int>(model,"NewerEntryCount"),0);
        }
        [Test] public void SameCountAndSerialReplacementCannotReuseStaleHistoryText()
        {
            Add(40);var model=Model();Call(model,"Open");int builds=Value<int>(model,"BuildCount");var raw=MessageLog.GetAllEntries();
            for(int i=0;i<raw.Count;i++)raw[i]=new MessageLog.Entry("replacement-"+i,77,raw[i].Serial);
            MessageLog.Restore(raw,new List<string>{"pending exact\nnotice"},4,40);Assert.True((bool)Call(model,"Refresh"));
            Assert.Greater(Value<int>(model,"BuildCount"),builds);Assert.True(Body(model).All(s=>s.StartsWith("replacement-")));
            CollectionAssert.AreEqual(raw,MessageLog.GetAllEntries());CollectionAssert.AreEqual(new[]{"pending exact\nnotice"},MessageLog.GetPendingAnnouncementsSnapshot());
        }
        [Test] public void PublicHistoryEntryRefusesMissingCanvasWithoutTrappingReadyPlayer()
        {
            var method=typeof(InputHandler).GetMethod("OpenLogHistory",BindingFlags.Public|BindingFlags.Instance,null,Type.EmptyTypes,null);
            Assert.NotNull(method,"Required keyboard/controller history entry");Assert.AreEqual(typeof(void),method.ReturnType);
            var ui=typeof(InputHandler).GetProperty("LogHistoryUI",BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(ui);
            var oldActive=TurnManager.Active;var oldWorld=TurnManager.World;GameObject go=null;
            try
            {
                TurnManager.World=null;go=new GameObject("history missing canvas");go.SetActive(false);var input=go.AddComponent<InputHandler>();
                var actor=new Entity{BlueprintName="Player"};actor.SetTag("Player");actor.AddPart(new PhysicsPart());
                actor.Statistics["Speed"]=new Stat{Name="Speed",BaseValue=100,Min=25,Max=200};actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};
                var zone=new Zone("HistoryBaseline");zone.AddEntity(actor,10,10);var turns=new TurnManager();turns.AddEntity(actor);turns.ProcessUntilPlayerTurn();
                input.PlayerEntity=actor;input.CurrentZone=zone;input.TurnManager=turns;Assert.True(turns.WaitingForInput);Assert.AreSame(actor,turns.CurrentActor);
                int tick=turns.TickCount,energy=turns.GetEnergy(actor);method.Invoke(input,null);
                var state=typeof(InputHandler).GetField("_inputState",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(input);
                Assert.AreEqual("Normal",state.ToString());Assert.Null(ui.GetValue(input));Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(energy,turns.GetEnergy(actor));
            }
            finally
            {
                if(go!=null)UnityEngine.Object.DestroyImmediate(go);TurnManager.World=oldWorld;
                typeof(TurnManager).GetProperty("Active",BindingFlags.Public|BindingFlags.Static).SetValue(null,oldActive);
            }
        }
    }
}
