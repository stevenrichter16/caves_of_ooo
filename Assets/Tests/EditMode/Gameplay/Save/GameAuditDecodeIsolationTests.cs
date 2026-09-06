using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public sealed class DecodeObservationPart : Part, ISaveSerializable
    {
        public int Marker;
        public static Action Parsed;
        public static readonly List<string> Hooks=new List<string>();
        public void Save(SaveWriter writer)=>writer.Write(Marker);
        public void Load(SaveReader reader){Marker=reader.ReadInt();Parsed?.Invoke();}
        public override void OnAfterLoad(SaveReader reader)=>Hooks.Add("after"+Marker);
        public override void FinalizeLoad(SaveReader reader)=>Hooks.Add("final"+Marker);
    }
    internal sealed class DecodeIsolationFixture : IDisposable
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        private readonly NewGameSaveFixture _save=new NewGameSaveFixture();
        private readonly Stack<AsciiFxRequest> _pool=(Stack<AsciiFxRequest>)typeof(AsciiFxBus).GetField("Pool",Static).GetValue(null);
        private readonly Queue<AsciiFxRequest> _ascii=(Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests",Static).GetValue(null);
        private readonly List<SpellFxSequence> _spell=(List<SpellFxSequence>)typeof(SpellFxBus).GetField("Pending",Static).GetValue(null);
        private readonly AsciiFxRequest[] _oldPool;
        private readonly SettlementManager _oldSettlement=SettlementManager.Current;
        private readonly Action _oldParsed=DecodeObservationPart.Parsed;
        private readonly string[] _oldHooks=DecodeObservationPart.Hooks.ToArray();
        private AsciiFxRequest[] _liveAscii;
        private object[][] _asciiFields;
        private Point[][] _paths;
        private SpellFxSequence[] _liveSpell;
        private readonly FieldInfo[] _requestFields=typeof(AsciiFxRequest).GetFields(BindingFlags.Public|BindingFlags.Instance);
        private int _clearVersion;
        public readonly GameSessionState Saved,Live;
        public readonly byte[] Bytes;
        public GameSessionState Applied;
        public int ApplyCalls;
        private readonly string _activeID,_pref,_root;
        public DecodeIsolationFixture(bool aura=true,bool observers=false)
        {
            _oldPool=_pool.ToArray();_pool.Clear();DecodeObservationPart.Parsed=null;DecodeObservationPart.Hooks.Clear();
            try
            {
                Saved=State(_save.NewID,11);var npc=new Entity{ID="saved-npc",BlueprintName="DecodeNpc"};npc.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=30,Max=30,Owner=npc};
                Assert.IsTrue(Saved.ZoneManager.ActiveZone.AddEntity(npc,7,8));var status=new StatusEffectsPart();npc.AddPart(status);
                if(aura)status.RestoreEffectsForLoad(new List<Effect>{new PoisonedEffect(5)});
                if(observers){Saved.Player.AddPart(new DecodeObservationPart{Marker=1});npc.AddPart(new DecodeObservationPart{Marker=2});}
                InstallGlobals("saved",11);Bytes=Serialize(Saved);
                Live=State(_save.NewID,33);InstallGlobals("live",33);
                SaveGameService.RegisterRuntime(()=>Live,state=>{Applied=state;ApplyCalls++;},Live.GameID);SaveGameService.SetActiveGameID(Live.GameID);
                _activeID=ActiveID();_pref=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);_root=SaveGameService.SaveRootOverride;
                AsciiFxBus.EmitProjectile(Live.ZoneManager.ActiveZone,new[]{new Point(2,3),new Point(3,3)},AsciiFxTheme.Lightning,true,true,.25f);
                AsciiFxBus.EmitChargeOrbit(Live.ZoneManager.ActiveZone,Live.Player,2,.75f,AsciiFxTheme.Arcane,true,.5f);
                SpellFxBus.Emit(new SpellFxSequence("decode-live-first",Live.ZoneManager.ActiveZone,Live.Player,new Point(3,4),cosmeticSeed:31));
                SpellFxBus.Emit(new SpellFxSequence("decode-live-second",Live.ZoneManager.ActiveZone,Live.Player,new Point(4,5),cosmeticSeed:32,blocksTurnAdvance:false));
                _liveAscii=_ascii.ToArray();_asciiFields=_liveAscii.Select(r=>_requestFields.Select(f=>f.GetValue(r)).ToArray()).ToArray();_paths=_liveAscii.Select(r=>r.Path?.ToArray()).ToArray();_liveSpell=_spell.ToArray();_clearVersion=AsciiFxBus.ClearVersion;
                Assert.AreEqual(2,_liveAscii.Length);Assert.AreEqual(2,_liveSpell.Length);
            }
            catch{Dispose();throw;}
        }
        private static GameSessionState State(string id,int tick)
        {
            var state=HotbarSaveFixture.MakeState(9,5,9);state.GameID=id;state.Player.ID="decode-player-"+tick;
            state.TurnManager.RestoreSavedState(tick,true,state.Player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=state.Player,Energy=tick*100}});return state;
        }
        internal static void InstallGlobals(string label,int value)
        {MessageLog.Restore(new List<MessageLog.Entry>{new MessageLog.Entry(label+"-one",value,value+1),new MessageLog.Entry(label+"-two",value+2,value+3)},new List<string>{label+"-announcement"},value+4,value+5);PlayerReputation.Restore(new Dictionary<string,int>{{label+"-faction",value}});}
        internal static byte[] Serialize(GameSessionState state)
        {using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));return stream.ToArray();}}
        internal static GameSessionState Decode(byte[] bytes)
        {using(var stream=new MemoryStream(bytes))return GameSessionState.Load(new SaveReader(stream,null));}
        internal static byte[] Damage(byte[] bytes,string kind)
        {
            var copy=(byte[])bytes.Clone();
            switch(kind)
            {
                case "empty":return Array.Empty<byte>();
                case "magic":copy[0]^=0xff;return copy;
                case "version":Array.Copy(BitConverter.GetBytes(6),0,copy,4,4);return copy;
                case "header":return copy.Take(6).ToArray();
                case "footer":copy[copy.Length-1]^=0xff;return copy;
                case "truncated-footer":return copy.Take(copy.Length-2).ToArray();
                case "late-body":return copy.Take(copy.Length-15).ToArray();
                default:throw new ArgumentException(kind);
            }
        }
        public void WriteQuick(byte[] bytes,bool compressed=true)
        {
            string dir=Path.Combine(_root,Live.GameID);Directory.CreateDirectory(dir);
            using(var file=File.Create(Path.Combine(dir,"Quick.sav.gz")))
            {if(compressed){using(var gzip=new GZipStream(file,CompressionMode.Compress))gzip.Write(bytes,0,bytes.Length);}else file.Write(bytes,0,bytes.Length);}
        }
        public byte[] EncodeSaved()
        {InstallGlobals("saved",11);try{return Serialize(Saved);}finally{InstallGlobals("live",33);}}
        public static string ActiveID()=>(string)typeof(SaveGameService).GetField("_activeGameID",Static).GetValue(null);
        public void AssertGlobals(string label,int value)
        {
            var entries=MessageLog.GetAllEntries();Assert.AreEqual(2,entries.Count);
            Assert.AreEqual(label+"-one",entries[0].Text);Assert.AreEqual(value,entries[0].Tick);Assert.AreEqual(value+1,entries[0].Serial);
            Assert.AreEqual(label+"-two",entries[1].Text);Assert.AreEqual(value+2,entries[1].Tick);Assert.AreEqual(value+3,entries[1].Serial);
            CollectionAssert.AreEqual(new[]{label+"-announcement"},MessageLog.GetPendingAnnouncementsSnapshot());Assert.AreEqual(value+4,MessageLog.FlashStamp);Assert.AreEqual(value+5,MessageLog.NextSerialValue);
            CollectionAssert.AreEquivalent(new Dictionary<string,int>{{label+"-faction",value}},PlayerReputation.GetAll());
        }
        public void AssertLiveUnchanged()
        {
            {
                Assert.AreSame(Live.ZoneManager.SettlementManager,SettlementManager.Current);Assert.AreSame(Live.TurnManager,TurnManager.Active);Assert.AreSame(Live.Player,Live.TurnManager.CurrentActor);Assert.AreEqual(33,Live.TurnManager.TickCount);Assert.AreEqual(3300,Live.TurnManager.GetEnergy(Live.Player));
                Assert.AreSame(Live.Player,Live.ZoneManager.ActiveZone.GetEntityCell(Live.Player).Objects.Single(o=>o==Live.Player));AssertGlobals("live",33);
                Assert.AreEqual(_clearVersion,AsciiFxBus.ClearVersion);Assert.IsTrue(AsciiFxBus.HasPendingBlocking);Assert.IsTrue(SpellFxBus.HasPendingBlocking);
                var requests=_ascii.ToArray();Assert.AreEqual(_liveAscii.Length,requests.Length);
                for(int i=0;i<Math.Min(_liveAscii.Length,requests.Length);i++)
                {Assert.AreSame(_liveAscii[i],requests[i]);for(int j=0;j<_requestFields.Length;j++)Assert.AreEqual(_asciiFields[i][j],_requestFields[j].GetValue(requests[i]),_requestFields[j].Name);if(_paths[i]!=null)CollectionAssert.AreEqual(_paths[i],requests[i].Path);}
                CollectionAssert.AreEqual(_liveSpell,_spell);Assert.AreEqual(_activeID,ActiveID());Assert.AreEqual(_pref,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));Assert.AreEqual(_root,SaveGameService.SaveRootOverride);Assert.AreEqual(0,ApplyCalls);
            }
        }
        public void AssertPublished(GameSessionState loaded,bool aura=true)
        {
            Assert.AreSame(loaded.ZoneManager.SettlementManager,SettlementManager.Current);Assert.AreSame(loaded.TurnManager,TurnManager.Active);Assert.AreEqual(11,loaded.TurnManager.TickCount);Assert.AreSame(loaded.Player,loaded.TurnManager.CurrentActor);Assert.AreNotSame(Live.Player,loaded.Player);AssertGlobals("saved",11);
            Assert.AreEqual(_clearVersion+1,AsciiFxBus.ClearVersion);Assert.AreEqual(0,_spell.Count);
            var requests=_ascii.ToArray();Assert.AreEqual(aura?1:0,requests.Length);
            if(aura){var npc=loaded.ZoneManager.ActiveZone.GetCell(7,8).Objects.Single(o=>o.ID=="saved-npc");Assert.AreSame(npc,requests[0].Anchor);Assert.AreSame(loaded.ZoneManager.ActiveZone,requests[0].Zone);Assert.AreEqual(AsciiFxRequestType.AuraStart,requests[0].Type);Assert.AreEqual(AsciiFxTheme.Poison,requests[0].Theme);var effect=npc.GetPart<StatusEffectsPart>().GetAllEffects().Single();Assert.AreSame(npc,effect.Owner);Assert.AreEqual(5,effect.Duration);Assert.AreEqual(30,npc.GetStatValue("Hitpoints"));}
        }
        public void Dispose()
        {
            typeof(SettlementManager).GetProperty("Current",Static).SetValue(null,_oldSettlement);
            DecodeObservationPart.Parsed=_oldParsed;DecodeObservationPart.Hooks.Clear();DecodeObservationPart.Hooks.AddRange(_oldHooks);
            _pool.Clear();if(_oldPool!=null)for(int i=_oldPool.Length-1;i>=0;i--)_pool.Push(_oldPool[i]);_save.Dispose();
        }
    }
    public class GameAuditDecodeIsolationTests
    {
        [TestCase("empty")] [TestCase("magic")] [TestCase("version")] [TestCase("footer")] [TestCase("truncated-footer")] [TestCase("late-body")]
        public void RejectedRawDecodePreservesCurrentSession(string kind)
        {using(var f=new DecodeIsolationFixture()){Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,kind)));f.AssertLiveUnchanged();}}
        [TestCase("magic")] [TestCase("footer")] [TestCase("truncated-footer")]
        public void RejectedQuickLoadDoesNotPublishOrApply(string kind)
        {using(var f=new DecodeIsolationFixture()){f.WriteQuick(DecodeIsolationFixture.Damage(f.Bytes,kind));LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));Assert.IsFalse(SaveGameService.QuickLoad());f.AssertLiveUnchanged();}}
        [Test]
        public void ValidRawLoadPublishesGlobalsAndRecreatesAura()
        {using(var f=new DecodeIsolationFixture()){var loaded=DecodeIsolationFixture.Decode(f.Bytes);f.AssertPublished(loaded);}}
        [Test]
        public void ValidQuickLoadPublishesAndAppliesExactlyOnce()
        {using(var f=new DecodeIsolationFixture()){f.WriteQuick(f.Bytes);Assert.IsTrue(SaveGameService.QuickLoad());Assert.AreEqual(1,f.ApplyCalls);f.AssertPublished(f.Applied);}}
        [Test]
        public void InvalidFooterInvokesNeitherHookPhase()
        {using(var f=new DecodeIsolationFixture(observers:true)){Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,"footer")));CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}}
        [Test]
        public void ParserSeesLiveGlobalsBeforeFinalValidation()
        {using(var f=new DecodeIsolationFixture(observers:true)){var observations=new List<bool>();DecodeObservationPart.Parsed=()=>observations.Add(ReferenceEquals(TurnManager.Active,f.Live.TurnManager)&&MessageLog.GetLast()=="live-two"&&PlayerReputation.Get("live-faction")==33&&AsciiFxBus.HasPendingBlocking&&SpellFxBus.HasPendingBlocking);Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,"footer")));Assert.AreEqual(2,observations.Count);Assert.IsTrue(observations.All(v=>v));}}
        [Test]
        public void ValidHookGraphRunsAllAfterHooksBeforeAnyFinalHook()
        {using(var f=new DecodeIsolationFixture(observers:true)){DecodeIsolationFixture.Decode(f.Bytes);CollectionAssert.AreEqual(new[]{"after1","after2","final1","final2"},DecodeObservationPart.Hooks);}}
        [TestCase("footer")] [TestCase("late-body")]
        public void RejectedDecodePreservesLiveSettlementManager(string kind)
        {using(var f=new DecodeIsolationFixture()){var original=SettlementManager.Current;Assert.AreSame(f.Live.ZoneManager.SettlementManager,original);Assert.Catch(()=>DecodeIsolationFixture.Decode(DecodeIsolationFixture.Damage(f.Bytes,kind)));Assert.AreSame(original,SettlementManager.Current);}}
        [Test]
        public void ValidDecodePublishesTheLoadedSettlementManager()
        {using(var f=new DecodeIsolationFixture()){var loaded=DecodeIsolationFixture.Decode(f.Bytes);Assert.AreSame(loaded.ZoneManager.SettlementManager,SettlementManager.Current);Assert.AreNotSame(f.Live.ZoneManager.SettlementManager,SettlementManager.Current);}}
        [Test]
        public void StandaloneOverworldDecodePublishesItsFinalLoadedManager()
        {using(var f=new DecodeIsolationFixture()){using(var stream=new MemoryStream()){SaveGraphSerializer.SaveOverworldZoneManager(f.Saved.ZoneManager,new SaveWriter(stream));stream.Position=0;var loaded=SaveGraphSerializer.LoadOverworldZoneManager(new SaveReader(stream,null));Assert.AreSame(loaded.SettlementManager,SettlementManager.Current);}}}
        [Test]
        public void OrdinaryConstructorsStillActivateManagers()
        {using(var f=new DecodeIsolationFixture()){var settlement=new SettlementManager();Assert.AreSame(settlement,SettlementManager.Current);var turns=new TurnManager();Assert.AreSame(turns,TurnManager.Active);var world=new OverworldZoneManager(null,444);Assert.AreSame(world.SettlementManager,SettlementManager.Current);}}
        [Test]
        public void NullSavedManagersDoNotReplaceExistingGlobals()
        {using(var f=new DecodeIsolationFixture()){var turns=TurnManager.Active;var settlements=SettlementManager.Current;var state=new GameSessionState{GameID=f.Live.GameID};var loaded=DecodeIsolationFixture.Decode(DecodeIsolationFixture.Serialize(state));Assert.IsNull(loaded.TurnManager);Assert.IsNull(loaded.ZoneManager);Assert.AreSame(turns,TurnManager.Active);Assert.AreSame(settlements,SettlementManager.Current);}}
        [Test]
        public void StandaloneTokenGraphStillRunsLoadHooks()
        {using(var f=new DecodeIsolationFixture()){var actor=new Entity{ID="standalone"};actor.AddPart(new DecodeObservationPart{Marker=3});using(var stream=new MemoryStream()){var writer=new SaveWriter(stream);writer.WriteEntityReference(actor);writer.WriteQueuedEntityBodies();stream.Position=0;var reader=new SaveReader(stream,null);var loaded=reader.ReadEntityReference();reader.ReadEntityBodies();Assert.AreNotSame(actor,loaded);Assert.AreSame(loaded,loaded.GetPart<DecodeObservationPart>().ParentEntity);CollectionAssert.AreEqual(new[]{"after3","final3"},DecodeObservationPart.Hooks);}}}
    }
}
