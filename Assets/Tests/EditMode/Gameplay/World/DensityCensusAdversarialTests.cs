using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using Report = CavesOfOoo.Tests.DensityLootCensusTests.Report;
using Row = CavesOfOoo.Tests.DensityLootCensusTests.SourceRow;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCensusAdversarialTests
    {
        static Row R(int seed=1,string source="Wilderness",string zone="Overworld.8.0.0",string biome="Spread")
            => new Row{seed=seed,source=source,zone=zone,biome=biome};
        static Report ReportOf(params Row[] rows) => new Report{sources=rows.ToList()};
        [TestCase(null)] [TestCase("")] [TestCase("spread")] [TestCase("999")] [TestCase("Unknown")]
        public void Adversarial_InvalidBiomeLabelsAreRejected(string biome)
        { Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(ReportOf(R(biome:biome)),1)); }
        [TestCase("Wilderness","Overworld.8.0.1")] [TestCase("Lair","Overworld.8.0.3")] [TestCase("Underground","Overworld.8.0.0")]
        public void Adversarial_SourceAndDepthCannotContradict(string source,string id)
        { Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(ReportOf(R(source:source,zone:id)),1)); }
        [Test] public void Adversarial_NullSourceRowIsRejected()
        { Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(ReportOf(R(),null),1)); }
        [Test] public void Adversarial_InvalidOtherSeedIsRejectedBeforeAnyGeneration()
        { Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(ReportOf(R(),R(seed:64,zone:"bad")),1)); }
        [Test] public void Adversarial_SameZoneInDifferentSeedsIsValid()
        { var report=ReportOf(R(),R(seed:64)); Assert.AreEqual(1,DensityCensusAuditTests.Recorded(report,1).Count); Assert.AreEqual(64,DensityCensusAuditTests.Recorded(report,64).Single().seed); }
        [Test] public void Adversarial_CopiedSelectionDoesNotInheritOldMeasuredContentsOrCounters()
        {
            var row=R(); row.opened=22;row.locked=4;row.items.Add(new DensityLootCensusTests.ItemRow{blueprint="GoldCoin",units=10});
            var result=DensityCensusAuditTests.Recorded(ReportOf(row),1).Single(); Assert.AreEqual(0,result.opened); Assert.AreEqual(0,result.locked); Assert.IsEmpty(result.items);
            Assert.AreEqual(22,row.opened); Assert.AreEqual(10,row.items.Single().units);
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_PlainAndGzipInputsHaveTheSameRecordedMembership(bool gzip)
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+(gzip?".json.gz":".json"));
            string json=JsonUtility.ToJson(ReportOf(R()));
            try
            {
                if(gzip) using(var file=File.Create(path)) using(var zip=new GZipStream(file,CompressionMode.Compress)) using(var writer=new StreamWriter(zip)) writer.Write(json);
                else File.WriteAllText(path,json);
                var result=DensityLootCensusTests.ReadRecordedReport(path); Assert.AreEqual("Overworld.8.0.0",DensityCensusAuditTests.Recorded(result,1).Single().zone);
            }
            finally { if(File.Exists(path))File.Delete(path); }
        }
        [TestCase(null)] [TestCase("")] [TestCase("missing-census-input-20260926.json")]
        public void Adversarial_MissingFileNeverCreatesReplacementMembership(string path)
        { Assert.Throws<InvalidOperationException>(()=>DensityLootCensusTests.ReadRecordedReport(path)); }
        [TestCase("null")] [TestCase("{}")] [TestCase("{\"sources\":[]}")]
        [TestCase("[")] [TestCase("{]")]
        public void Adversarial_EmptySerializedSelectionCannotSilentlyRescan(string json)
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".json");
            try {File.WriteAllText(path,json); Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(DensityLootCensusTests.ReadRecordedReport(path),1));}
            finally {File.Delete(path);}
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_OffsetContainerApproachUsesPhysicalBody(bool bodyBlocked)
        {
            using(var scope=new DensityLootTestScope())
            {
                var player=scope.Factory.CreateEntity("Player"); var chest=scope.Factory.CreateEntity("Chest");
                chest.AddPart(new SpatialFootprintPart{CellsRaw="3,0"}); var zone=new Zone("Overworld.8.0.0"); Assert.IsTrue(zone.AddEntity(chest,10,10));
                foreach(int center in bodyBlocked?new[]{10,13}:new[]{10})
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                        if(dx!=0||dy!=0)Assert.IsTrue(zone.AddEntity(scope.Factory.CreateEntity("Wall"),center+dx,10+dy));
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,1);manager.SetActiveZone(zone);var report=new Report();
                typeof(DensityLootCensusTests).GetMethod("MeasureZone",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{report,scope,manager,player,1,8,0,0,BiomeType.Spread,"Wilderness",0.35});
                Assert.AreEqual(bodyBlocked?1:0,report.sources.Single().noApproach); Assert.AreEqual(bodyBlocked?0:1,report.sources.Single().opened);
                Assert.IsNull(zone.GetEntityCell(player));
            }
        }
        [Test] public void Adversarial_UnexpectedSeedCannotBeSilentlyOmittedFromTheFixedCohort()
        { Assert.Throws<InvalidOperationException>(()=>DensityCensusAuditTests.Recorded(ReportOf(R(),R(seed:999)),1)); }
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_ClosedArchiveBarrierBlocksOnlyApproachWithoutPhysicsSolid(bool closed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var player=scope.Factory.CreateEntity("Player"); var chest=scope.Factory.CreateEntity("Chest"); var zone=new Zone("Overworld.8.0.0"); zone.AddEntity(chest,10,10);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                    if((dx!=0||dy!=0)&&(dx!=-1||dy!=-1))zone.AddEntity(scope.Factory.CreateEntity("Wall"),10+dx,10+dy);
                var barrier=new Entity(); barrier.AddPart(new LockPart{IsLocked=closed}); barrier.AddPart(new SealedLibraryBarrierPart()); Assert.IsTrue(zone.AddEntity(barrier,9,9));
                Assert.AreEqual(closed,zone.GetCell(9,9).BlocksMovement());
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,1);manager.SetActiveZone(zone);var report=new Report();
                typeof(DensityLootCensusTests).GetMethod("MeasureZone",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{report,scope,manager,player,1,8,0,0,BiomeType.Spread,"Wilderness",0.35});
                Assert.AreEqual(closed?1:0,report.sources.Single().noApproach); Assert.AreEqual(closed?0:1,report.sources.Single().opened);
                Assert.IsNull(zone.GetEntityCell(player)); Assert.AreEqual(closed,barrier.GetPart<LockPart>().IsLocked);
            }
        }
    }
}
