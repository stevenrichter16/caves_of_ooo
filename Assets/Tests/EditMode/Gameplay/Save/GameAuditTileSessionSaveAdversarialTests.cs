using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class TileHookMutationPart : Part
    {
        public string ZoneId;
        public bool Mutate;
        public override void OnAfterLoad(SaveReader reader)
        { if(Mutate){var z=reader.FindZone(ZoneId);z.TileState.RemoveCoating(5,6,"water");z.TileState.WriteCoating(7,8,"hook-liquid",61);} }
        public override void FinalizeLoad(SaveReader reader)
        { if(Mutate)reader.FindZone(ZoneId).TileState.WriteCloud(7,8,"hook-cloud",9); }
    }

    public class GameAuditTileSessionSaveAdversarialTests
    {
        const string Other="Overworld.0.0.3";
        const string Begin="TileState.Begin",End="TileState.End";
        static Zone Active(GameSessionState s)=>s.ZoneManager.ActiveZone;
        static void AddOther(DecodeIsolationFixture f)=>f.Saved.ZoneManager.CachedZones.Add(Other,new Zone(Other));
        static GameSessionState Decode(byte[] bytes,EntityFactory factory=null)
        {using(var s=new MemoryStream(bytes))return GameSessionState.Load(new SaveReader(s,factory));}
        static void Text(SaveWriter w,string s){var bytes=Encoding.UTF8.GetBytes(s);w.Write(bytes.Length);foreach(byte b in bytes)RawByte(w,b);}
        // The raw format fixture must not depend on new internal serializer helpers.
        static readonly System.Reflection.FieldInfo BinaryWriterField=typeof(SaveWriter).GetField("_writer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        static void RawByte(SaveWriter w,byte b)=>((BinaryWriter)BinaryWriterField.GetValue(w)).Write(b);
        static byte[] Wire(DecodeIsolationFixture f,string fault=null)
        {
            using(var s=new MemoryStream())
            {
                var prefix=GameAuditTileSessionSaveTests.LegacyBytes(f.Saved,false);s.Write(prefix,0,prefix.Length);var w=new SaveWriter(s);
                w.WriteCheck(Begin);w.Write(1);
                w.Write(fault=="negative-zone-count"?-1:fault=="too-many-zones"?4097:fault=="missing-zone"?1:2);
                if(fault=="zone-id-too-long"){w.Write(1025);return s.ToArray();}
                Text(w,fault=="unknown-zone"?"unknown-zone":fault=="empty-zone"?"":Active(f.Saved).ZoneID);
                w.Write(fault=="negative-tiles"?-1:fault=="too-many-tiles"?2001:2);
                for(int tile=0;tile<2;tile++)
                {
                    int key=tile==0?6*80+5:9*80+8;
                    if(tile==0&&fault=="negative-key")key=-1;if(tile==0&&fault=="out-of-range-key")key=2000;
                    if(tile==1&&fault=="duplicate-key")key=6*80+5;w.Write(key);
                    int coats=tile==0?1:0;if(tile==0&&fault=="negative-layers")coats=-1;if(tile==0&&fault=="too-many-layers")coats=257;
                    if(tile==0&&fault=="duplicate-coating")coats=2;w.Write(coats);
                    if(tile==0)
                    {
                        if(fault=="id-too-long"){w.Write(1025);return s.ToArray();}
                        if(fault=="negative-text-length"){w.Write(-1);return s.ToArray();}
                        if(fault=="invalid-utf8"){w.Write(1);RawByte(w,0xff);}else if(fault=="truncated-text"){w.Write(20);RawByte(w,1);return s.ToArray();}
                        else Text(w,fault=="empty-layer"?"":"water");
                        w.Write(fault=="zero-turns"?0:fault=="negative-turns"?-1:53);
                        if(fault=="duplicate-coating"){Text(w,"water");w.Write(4);}
                    }
                    w.Write(tile==1?(fault=="duplicate-residue"?2:1):0);
                    if(tile==1){Text(w,"ash");w.Write(int.MaxValue);if(fault=="duplicate-residue"){Text(w,"ash");w.Write(3);}}
                    w.Write(tile==0&&fault=="negative-heat"?-1:tile==0&&fault=="too-much-heat"?3:0);
                    w.Write(tile==0&&fault=="too-much-cold"?3:0);w.Write(tile==0&&fault=="too-much-charge"?3:0);
                    Text(w,tile==0&&fault=="cloud-without-turns"?"fungal-spores":"");w.Write(tile==0&&fault=="empty-cloud-with-turns"?4:0);
                }
                Text(w,fault=="duplicate-zone"?Active(f.Saved).ZoneID:Other);w.Write(0);
                w.WriteCheck(fault=="wrong-tile-end"?"wrong":End);w.WriteCheck(fault=="wrong-session-end"?"wrong":"GameSession.End");
                return s.ToArray();
            }
        }

        [TestCase("negative-zone-count")][TestCase("too-many-zones")][TestCase("missing-zone")]
        [TestCase("unknown-zone")][TestCase("duplicate-zone")][TestCase("empty-zone")][TestCase("zone-id-too-long")]
        [TestCase("negative-tiles")][TestCase("too-many-tiles")][TestCase("negative-key")][TestCase("out-of-range-key")][TestCase("duplicate-key")]
        [TestCase("negative-layers")][TestCase("too-many-layers")][TestCase("id-too-long")][TestCase("negative-text-length")]
        [TestCase("invalid-utf8")][TestCase("truncated-text")][TestCase("empty-layer")][TestCase("duplicate-coating")][TestCase("duplicate-residue")]
        [TestCase("zero-turns")][TestCase("negative-turns")][TestCase("negative-heat")][TestCase("too-much-heat")][TestCase("too-much-cold")][TestCase("too-much-charge")]
        [TestCase("cloud-without-turns")][TestCase("empty-cloud-with-turns")][TestCase("wrong-tile-end")][TestCase("wrong-session-end")]
        public void MalformedSectionRejectsBeforePublishingEvenAfterEarlierValidRecords(string fault)
        {
            using(var f=new DecodeIsolationFixture(aura:false,observers:true))
            {AddOther(f);Assert.Catch(()=>Decode(Wire(f,fault)));f.AssertLiveUnchanged();CollectionAssert.IsEmpty(DecodeObservationPart.Hooks);}
        }
        [Test]
        public void IndependentValidWireControlLoadsEveryCachedZoneAndOrderedPayload()
        {
            using(var f=new DecodeIsolationFixture(aura:false,observers:true))
            {AddOther(f);var loaded=Decode(Wire(f));Assert.AreEqual(2,loaded.ZoneManager.CachedZones.Count);Assert.AreEqual(53,Active(loaded).TileState.CoatingTurns(5,6,"water"));Assert.IsTrue(Active(loaded).TileState.HasResidue(8,9,"ash"));Assert.AreEqual(int.MaxValue,Active(loaded).TileState.Get(8,9).Residues[0].Turns);Assert.AreEqual(0,loaded.ZoneManager.CachedZones[Other].TileState.WrittenCount);CollectionAssert.AreEqual(new[]{"after1","after2","final1","final2"},DecodeObservationPart.Hooks);}
        }
        [Test]
        public void SameIdInDifferentFamiliesAndUtf8BoundaryRemainValid()
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);string id=new string('é',512);Assert.AreEqual(1024,Encoding.UTF8.GetByteCount(id));z.TileState.WriteCoating(5,6,id,7);z.TileState.WriteResidue(5,6,id,int.MaxValue);var tile=Active(Decode(f.EncodeSaved())).TileState.Get(5,6);Assert.AreEqual(id,tile.Coatings.Single().Id);Assert.AreEqual(7,tile.Coatings[0].Turns);Assert.AreEqual(id,tile.Residues.Single().Id);Assert.AreEqual(int.MaxValue,tile.Residues[0].Turns);}
        }
        [TestCase(false)][TestCase(true)]
        public void LegitimateLoadHookChangesSurviveLaterNativeRebuild(bool mutate)
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);z.TileState.WriteCoating(5,6,"water",53);f.Saved.Player.AddPart(new TileHookMutationPart{ZoneId=z.ZoneID,Mutate=mutate});var copy=Active(Decode(f.EncodeSaved()));Assert.AreEqual(mutate?0:53,copy.TileState.CoatingTurns(5,6,"water"));Assert.AreEqual(mutate?61:0,copy.TileState.CoatingTurns(7,8,"hook-liquid"));Assert.AreEqual(mutate?"hook-cloud":"",copy.TileState.Cloud(7,8));}
        }
        [TestCase(false)][TestCase(true)]
        public void RealMorrowfastLegacyGeometryRepairRespectsExtensionButLegacyAbsenceKeepsOldSemantics(bool extension)
        {
            using(var content=new EntityEquipmentContentFixture())using(var f=new DecodeIsolationFixture(aura:false))
            {
                var old=new Zone(MorrowfastSceneRuntime.ZoneID);f.Saved.ZoneManager.CachedZones.Add(old.ZoneID,old);
                Assert.AreEqual("Morrowfast",f.Saved.ZoneManager.WorldMap.GetPOI(3,6).Profile);Assert.IsFalse(MorrowfastSceneRuntime.IsActive(old));
                byte[] bytes=extension?f.EncodeSaved():GameAuditTileSessionSaveTests.LegacyBytes(f.Saved);
                var loaded=Decode(bytes,content.Factory);var repaired=loaded.ZoneManager.CachedZones[old.ZoneID];
                Assert.IsTrue(MorrowfastSceneRuntime.IsActive(repaired),"Real existing geometry migration must have happened.");
                var pools=repaired.GetAllEntities().Where(e=>e.GetPart<LiquidPoolPart>()?.LiquidId=="water").ToArray();Assert.Greater(pools.Length,0);
                int written=0;foreach(var pool in pools){var p=repaired.GetEntityPosition(pool);if(repaired.TileState.HasCoating(p.x,p.y,"water"))written++;}
                Assert.AreEqual(extension?0:pools.Length,written,"An explicit empty snapshot wins; a file with no snapshot retains old migration projections.");
            }
        }
        [Test]
        public void TileSectionSortsZonesAndCellsWithoutDependingOnInsertionOrder()
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {
                AddOther(f);var z=Active(f.Saved);z.TileState.WriteCoating(2,3,"water",4);z.TileState.WriteResidue(7,8,"ash",9);
                var before=TileSection(f.EncodeSaved());z.TileState.Clear(2,3);z.TileState.Clear(7,8);
                z.TileState.WriteResidue(7,8,"ash",9);z.TileState.WriteCoating(2,3,"water",4);
                var manager=f.Saved.ZoneManager;var reversed=new Dictionary<string,Zone>();foreach(var pair in manager.CachedZones.Reverse())reversed.Add(pair.Key,pair.Value);
                manager.ReplaceLoadedState(reversed,z.ZoneID,manager.GetConnectionSnapshot());CollectionAssert.AreEqual(before,TileSection(f.EncodeSaved()));
            }
        }
        [Test]
        public void MaximumSupportedLayerCountPreservesEveryLayerInOrder()
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);for(int i=0;i<256;i++)z.TileState.WriteCoating(5,6,"layer-"+i,i+1);var copy=Active(Decode(f.EncodeSaved())).TileState.Get(5,6);Assert.AreEqual(256,copy.Coatings.Count);CollectionAssert.AreEqual(z.TileState.Get(5,6).Coatings.Select(l=>l.Id),copy.Coatings.Select(l=>l.Id));CollectionAssert.AreEqual(z.TileState.Get(5,6).Coatings.Select(l=>l.Turns),copy.Coatings.Select(l=>l.Turns));}
        }
        static byte[] TileSection(byte[] bytes)
        {
            byte[] marker;using(var stream=new MemoryStream()){new SaveWriter(stream).WriteCheck(Begin);marker=stream.ToArray();}
            for(int i=bytes.Length-marker.Length;i>=0;i--)if(marker.Select((b,n)=>bytes[i+n]==b).All(v=>v))return bytes.Skip(i).ToArray();
            Assert.Fail("New-format session has no tile section.");return null;
        }
        [Test]
        public void WriterRejectsInvalidPublicStateInsteadOfSilentlyClampingIt()
        {
            using(var f=new DecodeIsolationFixture(aura:false))
            {var z=Active(f.Saved);z.TileState.WriteCoating(5,6,"water",4);z.TileState.Get(5,6).Heat=3;Assert.Throws<InvalidDataException>(()=>f.EncodeSaved());Assert.AreEqual(3,z.TileState.Get(5,6).Heat);Assert.AreEqual(4,z.TileState.CoatingTurns(5,6,"water"));}
        }
    }
}
