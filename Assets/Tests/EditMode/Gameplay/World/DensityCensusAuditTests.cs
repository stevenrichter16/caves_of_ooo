using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using Report = CavesOfOoo.Tests.DensityLootCensusTests.Report;
using Row = CavesOfOoo.Tests.DensityLootCensusTests.SourceRow;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCensusAuditTests
    {
        DensityLootTestScope scope; Entity player, chest; Zone zone; OverworldZoneManager manager;
        sealed class Interceptor : Part
        {
            public string Mode; public Entity Other;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID != "InventoryAction" || e.GetStringParameter("Command") != "OpenContainer") return true;
                if(Mode == "throw") throw new InvalidOperationException("probe failure");
                if(Mode == "other" || Mode == "matching-unhandled")
                {
                    var open = GameEvent.New("OpenContainer"); open.SetParameter("Container", (object)(Mode == "other" ? Other : ParentEntity));
                    e.GetParameter<Entity>("Actor").FireEventAndRelease(open);
                }
                e.Handled = Mode != "unhandled" && Mode != "matching-unhandled"; return false;
            }
        }
        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope(); player = scope.Factory.CreateEntity("Player");
            chest = scope.Factory.CreateEntity("LockedChest"); chest.GetPart<LockPart>().IsLocked=false;
            zone = new Zone("Overworld.8.0.0"); Assert.IsTrue(zone.AddEntity(chest,10,10));
            manager = OverworldZoneManager.CreateDetached(scope.Factory,1); manager.SetActiveZone(zone);
        }
        [TearDown] public void Cleanup() => scope.Dispose();
        void Intercept(string mode)
        {
            var cp=chest.GetPart<ContainerPart>(); chest.RemovePart(cp);
            chest.AddPart(new Interceptor { Mode=mode, Other=scope.Factory.CreateEntity("Chest") }); chest.AddPart(cp);
        }
        Row Measure()
        {
            var report=new Report(); var method=typeof(DensityLootCensusTests).GetMethod("MeasureZone",BindingFlags.Static|BindingFlags.NonPublic);
            try { method.Invoke(null,new object[]{report,scope,manager,player,1,8,0,0,BiomeType.Spread,"Wilderness",0.35}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
            return report.sources.Single();
        }
        static int Counter(Row row,string name)
        {
            var field=typeof(Row).GetField(name); Assert.NotNull(field,"Explicit access counter: "+name);
            return (int)field.GetValue(row);
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(true,true)]
        public void CombinedLockAuthorityIsSkippedAndGeneratedContentsRemainSeparate(bool key,bool legacy)
        {
            chest.GetPart<LockPart>().IsLocked=key; var cp=chest.GetPart<ContainerPart>(); cp.Locked=legacy;
            var dagger=scope.Factory.CreateEntity("Dagger"); cp.AddItem(dagger); var before=player.Parts.ToArray();
            var row=Measure(); Assert.AreEqual(key||legacy?1:0,row.locked); Assert.AreEqual(key||legacy?0:1,row.opened);
            Assert.AreEqual(1,row.items.Single(r=>r.blueprint=="Dagger").units);
            Assert.AreSame(chest,dagger.GetPart<PhysicsPart>().InInventory); Assert.IsNull(zone.GetEntityCell(player));
            CollectionAssert.AreEqual(before,player.Parts);
        }
        [TestCase("handled")] [TestCase("unhandled")] [TestCase("other")]
        public void RefusalAndWrongContainerEventsDoNotCountAsOpened(string mode)
        {
            Intercept(mode); var row=Measure(); Assert.AreEqual(0,row.opened);
            Assert.AreEqual(1,Counter(row,"openingAttempts")); Assert.AreEqual(1,Counter(row,"refused"));
        }
        [Test] public void MatchingOpenEventCountsEvenWhenTheOuterActionIsNotHandled()
        { Intercept("matching-unhandled"); var row=Measure(); Assert.AreEqual(1,row.opened); Assert.AreEqual(1,Counter(row,"openingAttempts")); Assert.AreEqual(0,Counter(row,"refused")); }
        [Test] public void ActualOpenRecordsOneAttemptAndNoRefusal()
        { var row=Measure(); Assert.AreEqual(1,row.opened); Assert.AreEqual(1,Counter(row,"openingAttempts")); Assert.AreEqual(0,Counter(row,"refused")); }
        [TestCase(true)] [TestCase(false)]
        public void SecondaryBodyOccupancyBlocksTheOnlyApproach(bool occupied)
        {
            for(int dx=-1;dx<=1;dx++) for(int dy=-1;dy<=1;dy++)
                if((dx!=0||dy!=0) && (dx!=-1||dy!=-1)) Assert.IsTrue(zone.AddEntity(scope.Factory.CreateEntity("Wall"),10+dx,10+dy));
            if(occupied)
            {
                var body=new Entity(); body.AddPart(new PhysicsPart{Solid=true}); body.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});
                Assert.IsTrue(zone.AddEntity(body,8,9)); Assert.IsEmpty(zone.GetCell(9,9).Objects); Assert.AreEqual(1,zone.GetCell(9,9).Occupants.Count);
            }
            var row=Measure(); Assert.AreEqual(occupied?1:0,row.noApproach); Assert.AreEqual(occupied?0:1,row.opened);
            Assert.IsNull(zone.GetEntityCell(player));
        }
        [Test] public void ThrowingOpenHandlerCannotLeaveMeasurementActorOrObserverInWorld()
        {
            Intercept("throw"); var parts=player.Parts.ToArray(); Assert.Throws<InvalidOperationException>(()=>Measure());
            Assert.IsNull(zone.GetEntityCell(player)); CollectionAssert.AreEqual(parts,player.Parts);
        }
        public static List<Row> Recorded(Report baseline,int seed)
        {
            var method=typeof(DensityLootCensusTests).GetMethod("RecordedZones",BindingFlags.Static|BindingFlags.Public);
            Assert.NotNull(method,"Recorded cohort selection must replace rescan selection");
            try { return (List<Row>)method.Invoke(null,new object[]{baseline,seed}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static Row R(int seed,string id,string source="Wilderness") => new Row{seed=seed,zone=id,source=source,biome="Spread"};
        [Test] public void RecordedSelectionPreservesExactOrderAndLabelsButCopiesRows()
        {
            var report=new Report(); report.sources.AddRange(new[]{R(1,"Overworld.11.0.0"),R(64,"Overworld.9.0.0"),R(1,"Overworld.8.0.3","Underground"),R(1,"Overworld.12.0.0","Lair")});
            var result=Recorded(report,1); CollectionAssert.AreEqual(new[]{"Overworld.11.0.0","Overworld.8.0.3","Overworld.12.0.0"},result.Select(r=>r.zone));
            CollectionAssert.AreEqual(new[]{"Wilderness","Underground","Lair"},result.Select(r=>r.source));
            Assert.AreNotSame(report.sources[0],result[0]); result[0].biome="changed"; Assert.AreEqual("Spread",report.sources[0].biome);
        }
        [Test] public void StarterAndShopsAreNotReplayedAsZoneRows()
        {
            var report=new Report(); report.sources.AddRange(new[]{R(0,"none","DesignedStarterGrant"),R(1,"Overworld.11.10.0","Shop:Merchant"),R(1,"Overworld.8.0.0")});
            Assert.AreEqual(1,Recorded(report,1).Count);
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.08.0.0")] [TestCase("Overworld.20.0.0")]
        [TestCase("Overworld.8.0.-1")] [TestCase("not-a-zone")]
        public void InvalidRecordedIDsNeverFallBackToRescanning(string id)
        { var report=new Report(); report.sources.Add(R(1,id)); Assert.Throws<InvalidOperationException>(()=>Recorded(report,1)); }
        [Test] public void DuplicateZoneIdentityIsRejectedEvenAcrossSourceLabels()
        { var report=new Report(); report.sources.AddRange(new[]{R(1,"Overworld.8.0.0"),R(1,"Overworld.8.0.0","Lair")}); Assert.Throws<InvalidOperationException>(()=>Recorded(report,1)); }
        [TestCase("NewUnreviewedKind")] [TestCase("")] [TestCase(null)]
        public void UnknownSourceLabelsAreRejected(string label)
        { var report=new Report(); report.sources.Add(R(1,"Overworld.8.0.0",label)); Assert.Throws<InvalidOperationException>(()=>Recorded(report,1)); }
        [Test] public void MissingSeedIsRejected()
        { var report=new Report(); report.sources.Add(R(64,"Overworld.8.0.0")); Assert.Throws<InvalidOperationException>(()=>Recorded(report,1)); }
        [TestCase(true)] [TestCase(false)]
        public void MissingSelectionDataIsRejected(bool nullReport)
        { var report=nullReport?null:new Report{sources=null}; Assert.Throws<InvalidOperationException>(()=>Recorded(report,1)); }
    }
}
