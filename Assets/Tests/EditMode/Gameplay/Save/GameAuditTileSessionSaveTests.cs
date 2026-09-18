// SOURCE-ONLY /tmp draft. Adopt and run RED before production. No execution claimed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public sealed class TileSnapshotObservationPart : Part
    {
        public string ZoneId;
        public int X, Y;
        public static int ObservedTurns, AfterCalls, FinalCalls;
        public override void OnAfterLoad(SaveReader reader)
        { AfterCalls++; ObservedTurns = reader.FindZone(ZoneId)?.TileState.CoatingTurns(X,Y,"water") ?? -1; }
        public override void FinalizeLoad(SaveReader reader) { FinalCalls++; }
    }

    public class GameAuditTileSessionSaveTests
    {
        // Naming is a proposed schema pin; root should settle exact constants before adoption.
        const string Begin = "TileState.Begin", End = "TileState.End";
        static Zone Active(GameSessionState s) => s.ZoneManager.ActiveZone;
        static GameSessionState Round(DecodeIsolationFixture f) => DecodeIsolationFixture.Decode(f.EncodeSaved());
        static List<int> Keys(Zone z) { var k = new List<int>(); z.TileState.CollectWrittenKeys(k);k.Sort();return k; }
        static void EqualTiles(Zone expected, Zone actual)
        {
            CollectionAssert.AreEqual(Keys(expected),Keys(actual));
            foreach(int key in Keys(expected))
            {
                var a=expected.TileState.Get(key%Zone.Width,key/Zone.Width);var b=actual.TileState.Get(key%Zone.Width,key/Zone.Width);
                Assert.NotNull(b,"Missing tile "+key);
                CollectionAssert.AreEqual(a.Coatings.Select(x=>x.Id).ToArray(),b.Coatings.Select(x=>x.Id).ToArray());
                CollectionAssert.AreEqual(a.Coatings.Select(x=>x.Turns).ToArray(),b.Coatings.Select(x=>x.Turns).ToArray());
                CollectionAssert.AreEqual(a.Residues.Select(x=>x.Id).ToArray(),b.Residues.Select(x=>x.Id).ToArray());
                CollectionAssert.AreEqual(a.Residues.Select(x=>x.Turns).ToArray(),b.Residues.Select(x=>x.Turns).ToArray());
                Assert.AreEqual(a.Heat,b.Heat);Assert.AreEqual(a.Cold,b.Cold);Assert.AreEqual(a.Charge,b.Charge);
                Assert.AreEqual(a.Cloud,b.Cloud);Assert.AreEqual(a.CloudTurns,b.CloudTurns);
            }
        }
        static void Rich(Zone z,int x=5,int y=6)
        {
            z.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);z.TileState.WriteCoating(x,y,"oil",3);
            z.TileState.WriteResidue(x,y,"petals",7);z.TileState.WriteResidue(x,y,"ash",ZoneTileState.Permanent);
            z.TileState.AddHeat(x,y,2);z.TileState.AddCold(x,y,1);z.TileState.AddCharge(x,y,2);
            z.TileState.WriteCloud(x,y,"fungal-spores",4);
        }
        [Test]
        public void PermanentWaterOnlyTileSurvivesWithAdjacentEmptyControl()
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);Assert.IsEmpty(z.GetCell(5,6).Objects);z.TileState.WriteCoating(5,6,"water",ZoneTileState.Permanent);
             var loaded=Round(f);EqualTiles(z,Active(loaded));Assert.AreEqual(ZoneTileState.Permanent,Active(loaded).TileState.CoatingTurns(5,6,"water"));Assert.IsNull(Active(loaded).TileState.Get(6,6));}
        }
        [Test]
        public void ActualTendrilFenBuilderWaterMaskAndSavedErasureSurvive()
        {
            using(var content=new EntityEquipmentContentFixture())using(var f=new DecodeIsolationFixture(aura:false))
            {
                var fen=new Zone("Overworld.3.7.0");var builder=new GrovelandsFormationBuilder();
                Assert.AreEqual(Formation.TendrilFen,FormationSelector.For(BiomeType.Grovelands,fen.ZoneID));
                Assert.IsTrue(builder.BuildZone(fen,content.Factory,new System.Random(729490642)));
                Assert.AreEqual(Formation.TendrilFen,builder.LastFormation);
                var water=Keys(fen).Where(k=>fen.TileState.HasCoating(k%80,k/80,"water")&&!fen.GetCell(k%80,k/80).HasObjectWithPart<LiquidPoolPart>()).ToArray();
                Assert.Greater(water.Length,2,"Non-vacuous real builder water-only braid required.");
                int erased=water[0];Assert.IsTrue(fen.TileState.RemoveCoating(erased%80,erased/80,"water"));
                f.Saved.ZoneManager.CachedZones.Add(fen.ZoneID,fen);var loaded=Round(f);
                var copy=loaded.ZoneManager.CachedZones[fen.ZoneID];EqualTiles(fen,copy);
                Assert.IsFalse(copy.TileState.HasCoating(erased%80,erased/80,"water"));
                Assert.AreEqual(water.Length-1,Keys(copy).Count(k=>copy.TileState.HasCoating(k%80,k/80,"water")));
            }
        }
        [Test]
        public void EveryLayerEnergyAndCloudPayloadPreservesOrderAndDuration()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));EqualTiles(Active(f.Saved),Active(Round(f)));}}
        [TestCase("Overworld.4.7.0")][TestCase("Overworld.4.6.2")]
        public void InactiveCachedZoneHasIndependentSameCoordinateState(string id)
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var active=Active(f.Saved);var other=new Zone(id);active.TileState.WriteCoating(5,6,"water",9);other.TileState.WriteCoating(5,6,"acid",ZoneTileState.Permanent);
             f.Saved.ZoneManager.CachedZones.Add(id,other);var loaded=Round(f);Assert.AreEqual(2,loaded.ZoneManager.CachedZones.Count);
             EqualTiles(active,Active(loaded));EqualTiles(other,loaded.ZoneManager.CachedZones[id]);Assert.AreEqual(active.ZoneID,loaded.ActiveZoneID);}
        }
        [TestCase(0,0)][TestCase(79,0)][TestCase(0,24)][TestCase(79,24)]
        public void ExactBoundaryCoordinatesDoNotTransposeOrDisappear(int x,int y)
        {using(var f=new DecodeIsolationFixture(aura:false)){Active(f.Saved).TileState.WriteCoating(x,y,"water",19);var z=Active(Round(f));Assert.AreEqual(19,z.TileState.CoatingTurns(x,y,"water"));Assert.AreEqual(1,z.TileState.WrittenCount);}}
        [TestCase(false)][TestCase(true)]
        public void ErasedTileDoesNotRegenerateEvenWhenPersistentPoolEntityRemains(bool pool)
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);Entity e=null;if(pool){e=new Entity{ID="source-pool",BlueprintName="WaterPuddle"};e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=120});Assert.IsTrue(z.AddEntity(e,5,6));}
             else z.TileState.WriteCoating(5,6,"water",ZoneTileState.Permanent);
             Assert.AreEqual(ZoneTileState.Permanent,z.TileState.CoatingTurns(5,6,"water"));Assert.Greater(z.TileState.Clear(5,6),0);
             var copy=Active(Round(f));Assert.IsNull(copy.TileState.Get(5,6));Assert.AreEqual(pool,copy.GetCell(5,6).HasObjectWithPart<LiquidPoolPart>());}
        }
        [Test]
        public void UnknownNonemptyIdsArePreservedWithoutRegistryFiltering()
        {using(var f=new DecodeIsolationFixture(aura:false)){var z=Active(f.Saved);z.TileState.WriteCoating(5,6,"Future.MixedCase/liquid",27);z.TileState.WriteResidue(5,6,"unknown residue",13);z.TileState.WriteCloud(5,6,"future-gas",2);EqualTiles(z,Active(Round(f)));}}
        [Test]
        public void SavingDoesNotAgeReactOrCallSourceChangeCallback()
        {using(var f=new DecodeIsolationFixture(aura:false)){var z=Active(f.Saved);Rich(z);string before=z.TileState.ToSaveString();int changes=0;z.TileState.OnCellChanged=(x,y)=>changes++;var bytes=f.EncodeSaved();Assert.Greater(bytes.Length,0);Assert.AreEqual(before,z.TileState.ToSaveString());Assert.AreEqual(0,changes);}}
        [Test]
        public void LoadedTransientDecayResumesAndPermanentLayerRemains()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));var z=Active(Round(f));Assert.AreEqual(3,z.TileState.CoatingTurns(5,6,"oil"));z.TileState.Tick();Assert.AreEqual(2,z.TileState.CoatingTurns(5,6,"oil"));Assert.AreEqual(ZoneTileState.Permanent,z.TileState.CoatingTurns(5,6,"water"));Assert.AreEqual(1,z.TileState.Heat(5,6));Assert.AreEqual(3,z.TileState.Get(5,6).CloudTurns);}}
        [Test]
        public void TwoRoundTripsPreserveExactStateAndSessionAliases()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));var once=Round(f);var twice=DecodeIsolationFixture.Decode(DecodeIsolationFixture.Serialize(once));EqualTiles(Active(f.Saved),Active(twice));Assert.AreSame(twice.Player,twice.TurnManager.CurrentActor);Assert.AreSame(twice.Player,Active(twice).GetCell(3,4).Objects.Single(e=>e.ID==twice.Player.ID));}}
        [Test]
        public void OnAfterLoadObservesValidatedSavedTileAndBothHookPhasesRunOnce()
        {
            int old=TileSnapshotObservationPart.ObservedTurns,a=TileSnapshotObservationPart.AfterCalls,b=TileSnapshotObservationPart.FinalCalls;
            try{using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);z.TileState.WriteCoating(5,6,"water",37);f.Saved.Player.AddPart(new TileSnapshotObservationPart{ZoneId=z.ZoneID,X=5,Y=6});
             TileSnapshotObservationPart.AfterCalls=TileSnapshotObservationPart.FinalCalls=0;TileSnapshotObservationPart.ObservedTurns=-1;
             Round(f);Assert.AreEqual(37,TileSnapshotObservationPart.ObservedTurns);Assert.AreEqual(1,TileSnapshotObservationPart.AfterCalls);Assert.AreEqual(1,TileSnapshotObservationPart.FinalCalls);}}
            finally{TileSnapshotObservationPart.ObservedTurns=old;TileSnapshotObservationPart.AfterCalls=a;TileSnapshotObservationPart.FinalCalls=b;}
        }
        [Test]
        public void RuntimeTileRenderCallbackIsNotTransferredToLoadedZone()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));int calls=0;Active(f.Saved).TileState.OnCellChanged=(x,y)=>calls++;var copy=Active(Round(f));Assert.IsNull(copy.TileState.OnCellChanged);copy.TileState.Clear(5,6);Assert.AreEqual(0,calls);}}
        [Test]
        public void EmptyCachedZoneRemainsEmptyWithoutGeneration()
        {using(var f=new DecodeIsolationFixture(aura:false)){var empty=new Zone("Overworld.0.0.3");f.Saved.ZoneManager.CachedZones.Add(empty.ZoneID,empty);var z=Round(f).ZoneManager.CachedZones[empty.ZoneID];Assert.AreEqual(0,z.TileState.WrittenCount);Assert.AreEqual(0,z.EntityCount);}}
        [Test]
        public void NullManagerSessionRemainsValid()
        {using(var f=new DecodeIsolationFixture(aura:false)){f.Saved.ZoneManager=null;f.Saved.ActiveZoneID=null;var copy=Round(f);Assert.IsNull(copy.ZoneManager);Assert.AreEqual(f.Saved.Player.ID,copy.Player.ID);}}
        [Test]
        public void ExactLegacyV7SessionLoadsWithoutConjuringTileSnapshot()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));var old=LegacyBytes(f.Saved);Assert.AreEqual(7,BitConverter.ToInt32(old,4));var copy=DecodeIsolationFixture.Decode(old);Assert.AreEqual(f.Saved.Player.ID,copy.Player.ID);Assert.AreEqual(0,Active(copy).TileState.WrittenCount);}}
        [TestCase(false)][TestCase(true)]
        public void PermittedBytesAfterFinalEndRemainUnread(bool legacy)
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));byte[] bytes=legacy?LegacyBytes(f.Saved):f.EncodeSaved();using(var stream=new MemoryStream(bytes.Concat(new byte[]{9,8,7,6,5}).ToArray())){var copy=GameSessionState.Load(new SaveReader(stream,null));Assert.AreEqual(bytes.Length,stream.Position);Assert.AreEqual(f.Saved.Player.ID,copy.Player.ID);if(!legacy)EqualTiles(Active(f.Saved),Active(copy));}}}
        [Test]
        public void NonseekableStreamLoadsTileStateWithoutLengthPositionOrSeek()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));using(var stream=new NonseekStream(f.EncodeSaved()))EqualTiles(Active(f.Saved),Active(GameSessionState.Load(new SaveReader(stream,null))));}}
        [Test]
        public void ActualQuickSaveQuickLoadPreservesSavedWaterBeforeApply()
        {using(var f=new DecodeIsolationFixture(aura:false)){var live=Active(f.Live);live.TileState.WriteCoating(5,6,"water",ZoneTileState.Permanent);Assert.IsTrue(SaveGameService.QuickSave());live.TileState.Clear(5,6);Assert.IsTrue(SaveGameService.QuickLoad());Assert.AreEqual(1,f.ApplyCalls);Assert.AreEqual(ZoneTileState.Permanent,Active(f.Applied).TileState.CoatingTurns(5,6,"water"));Assert.IsNull(live.TileState.Get(5,6));}}
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void TruncatedOptionalSectionMarkerRejectsBeforePublishing(int count)
        {using(var f=new DecodeIsolationFixture(aura:false,observers:true)){var bytes=LegacyBytes(f.Saved,false).Concat(CheckBytes(Begin).Take(count)).ToArray();Assert.Catch(()=>DecodeIsolationFixture.Decode(bytes));f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}}
        [Test]
        public void UnsupportedOptionalSectionVersionRejectsBeforePublishing()
        {using(var f=new DecodeIsolationFixture(aura:false,observers:true)){using(var stream=new MemoryStream()){byte[] prefix=LegacyBytes(f.Saved,false);stream.Write(prefix,0,prefix.Length);var w=new SaveWriter(stream);w.WriteCheck(Begin);w.Write(int.MaxValue);w.WriteCheck(End);w.WriteCheck("GameSession.End");Assert.Catch(()=>DecodeIsolationFixture.Decode(stream.ToArray()));f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}}}
        [Test]
        public void CorruptExtendedFooterStillRejectsBeforeHooksAndCanRetryValidBytes()
        {using(var f=new DecodeIsolationFixture(aura:false,observers:true)){Rich(Active(f.Saved));var bytes=f.EncodeSaved();var bad=(byte[])bytes.Clone();bad[bad.Length-1]^=0xff;Assert.Catch(()=>DecodeIsolationFixture.Decode(bad));f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);EqualTiles(Active(f.Saved),Active(DecodeIsolationFixture.Decode(bytes)));}}
        [Test]
        public void CorruptCompressedFooterDoesNotApplyOrClearLiveFx()
        {using(var f=new DecodeIsolationFixture(aura:false,observers:true)){Rich(Active(f.Saved));var bytes=f.EncodeSaved();bytes[bytes.Length-1]^=0xff;f.WriteQuick(bytes);LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));Assert.IsFalse(SaveGameService.QuickLoad());f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}}
        [Test]
        public void StandaloneManagerGraphFormatDeliberatelyRemainsWithoutTileExtension()
        {using(var f=new DecodeIsolationFixture(aura:false)){Rich(Active(f.Saved));using(var stream=new MemoryStream()){var w=new SaveWriter(stream);SaveGraphSerializer.SaveOverworldZoneManager(f.Saved.ZoneManager,w);w.WriteQueuedEntityBodies();stream.Position=0;var r=new SaveReader(stream,null);var copy=SaveGraphSerializer.LoadOverworldZoneManager(r);r.ReadEntityBodies();Assert.AreEqual(0,copy.ActiveZone.TileState.WrittenCount);Assert.AreEqual(f.Saved.Player.ID,copy.ActiveZone.GetCell(3,4).Objects.Single().ID);}}}

        // Exact pre-extension writer. This is a compatibility fixture, not production serialization.
        // It must remain fixed to the source-verified v7 prefix, so tests really exercise old files.
        internal static byte[] LegacyBytes(GameSessionState s,bool footer=true)
        {
            using(var stream=new MemoryStream())
            {
                var w=new SaveWriter(stream);w.WriteHeader(s.GameVersion);w.WriteCheck("GameSession.Begin");w.Write(s.SaveVersion);
                w.WriteString(s.GameID);w.WriteString(s.GameVersion);w.Write(s.WorldSeed);w.WriteString(s.ActiveZoneID);w.Write(s.SelectedHotbarSlot);
                w.WriteCheck("Player");w.WriteEntityReference(s.Player);w.WriteCheck("World");w.WriteEntityReference(s.World);
                w.WriteCheck("ZoneManager");SaveGraphSerializer.SaveOverworldZoneManager(s.ZoneManager,w);
                w.WriteCheck("TurnManager");SaveGraphSerializer.SaveTurnManager(s.TurnManager,w);
                w.WriteCheck("MessageLog");SaveGraphSerializer.SaveMessageLog(w);w.WriteCheck("PlayerReputation");SaveGraphSerializer.SavePlayerReputation(w);
                w.WriteQueuedEntityBodies();if(footer)w.WriteCheck("GameSession.End");return stream.ToArray();
            }
        }
        static byte[] CheckBytes(string name){using(var s=new MemoryStream()){new SaveWriter(s).WriteCheck(name);return s.ToArray();}}
        sealed class NonseekStream:Stream
        {
            readonly MemoryStream source;public NonseekStream(byte[] bytes){source=new MemoryStream(bytes);}
            public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
            public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
            public override int Read(byte[] buffer,int offset,int count)=>source.Read(buffer,offset,count);public override int ReadByte()=>source.ReadByte();
            public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long length)=>throw new NotSupportedException();
            public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();public override void Flush(){}
            protected override void Dispose(bool disposing){if(disposing)source.Dispose();base.Dispose(disposing);}
        }
    }
}
