using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    // Source provenance only. No new hauling layout, actor mutation or native-key claim.
    public sealed class SpreadHaulableReceiptTests
    {
        DensityLootTestScope scope;
        [SetUp]public void Setup(){scope=new DensityLootTestScope();}
        [TearDown]public void Cleanup(){scope.Dispose();}
        static void Capture(HaulablePropBuilder b,bool value)
        {var f=b.GetType().GetField("CaptureSourceReceipts");Assert.NotNull(f,"Missing optional exact haulable producer capture.");f.SetValue(b,value);}
        static object Receipt(HaulablePropBuilder b)
        {var p=b.GetType().GetProperty("SourceReceipt");Assert.NotNull(p,"Missing exact haulable producer receipt.");return p.GetValue(b);}
        static object Value(object r,string name)
        {Assert.NotNull(r,"Opted-in build must finish with an explicit receipt, including an honest empty roll.");var p=r.GetType().GetProperty(name);Assert.NotNull(p,name);return p.GetValue(r);}
        static bool Current(object r)=>(bool)Value(r,"IsCurrent");
        static Entity[] Owners(object r)=>((IEnumerable)Value(r,"Owners")).Cast<Entity>().ToArray();
        static Zone Zone()=>new Zone("Overworld.8.8.0");
        static string Shape(Zone z)=>string.Join(";",z.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)+":"+e.GetPart<PhysicsPart>().Weight+":"+e.GetPart<HandlingPart>().Weight).OrderBy(s=>s));
        HaulablePropBuilder Builder(int chance=350)=>new HaulablePropBuilder(BiomeType.Spread){ChancePerMille=chance};
        [Test]public void UnmodifiedLegacyProducerStillPlacesAtMostOneRealOrdinarySource()
        {var z=Zone();Assert.True(Builder(1000).BuildZone(z,scope.Factory,new Random(64)));Assert.AreEqual(1,z.EntityCount);CollectionAssert.Contains(new[]{"FallenBeam","HaulBarrel","MillStone"},z.GetReadOnlyEntities().Single().BlueprintName);}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void CapturePreservesActualOrdinaryChancePlacementAndNextRandomValue(int seed)
        {
            var a=Zone();var b=Zone();var ra=new Random(seed);var rb=new Random(seed);
            Assert.True(Builder().BuildZone(a,scope.Factory,ra));var observed=Builder();Capture(observed,true);
            Assert.True(observed.BuildZone(b,scope.Factory,rb));var receipt=Receipt(observed);
            Assert.AreEqual(Shape(a),Shape(b));Assert.AreEqual(ra.Next(),rb.Next());Assert.LessOrEqual(b.EntityCount,1);
            Assert.AreSame(b,Value(receipt,"Zone"));Assert.AreSame(scope.Factory,Value(receipt,"Factory"));Assert.True(Current(receipt));
            CollectionAssert.AreEquivalent(b.GetReadOnlyEntities(),Owners(receipt));
        }
        [Test]public void PreexistingMatchingBeamIsNeverClaimedAsThisProducersRoll()
        {var z=Zone();var foreign=scope.Factory.CreateEntity("FallenBeam");Assert.True(z.AddEntity(foreign,1,1));var b=Builder(1000);Capture(b,true);Assert.True(b.BuildZone(z,scope.Factory,new Random(64)));var r=Receipt(b);Assert.True(Current(r));Assert.AreEqual(1,Owners(r).Length);Assert.False(Owners(r).Contains(foreign));Assert.AreEqual(2,z.EntityCount);}
        [Test]public void FailedOrdinaryRollIsAnExplicitCurrentEmptySourceWithoutASecondTry()
        {var z=Zone();var b=Builder(0);Capture(b,true);var actual=new Random(64);var expected=new Random(64);expected.Next(1000);Assert.True(b.BuildZone(z,scope.Factory,actual));var r=Receipt(b);Assert.True(Current(r));Assert.IsEmpty(Owners(r));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(expected.Next(),actual.Next());}
        [Test]public void MissingRolledBlueprintCannotAuthorizeACompleteNonemptyAllowance()
        {foreach(var bp in new[]{"FallenBeam","HaulBarrel","MillStone"})scope.Factory.Blueprints.Remove(bp);var z=Zone();var b=Builder(1000);Capture(b,true);Assert.True(b.BuildZone(z,scope.Factory,new Random(64)));var r=Receipt(b);Assert.False(Current(r));Assert.IsEmpty(Owners(r));Assert.AreEqual(0,z.EntityCount);}
        [Test]public void RebuildAndOptOutInvalidateOldExactGraphRatherThanBorrowingTheSameAddress()
        {var b=Builder(1000);Capture(b,true);var first=Zone();b.BuildZone(first,scope.Factory,new Random(64));var old=Receipt(b);Assert.True(Current(old));var second=Zone();b.BuildZone(second,scope.Factory,new Random(64));var current=Receipt(b);Assert.False(Current(old));Assert.True(Current(current));Assert.AreSame(second,Value(current,"Zone"));Assert.Greater((int)Value(current,"Revision"),(int)Value(old,"Revision"));Capture(b,false);b.BuildZone(Zone(),scope.Factory,new Random(64));Assert.False(Current(current));Assert.Null(Receipt(b));}
        [TestCase("moved")][TestCase("hidden")][TestCase("handling")][TestCase("foreign-physics")]
        public void ChangedActualSourceCannotRemainAnUntouchedProducerReceipt(string fault)
        {var z=Zone();var b=Builder(1000);Capture(b,true);b.BuildZone(z,scope.Factory,new Random(64));var r=Receipt(b);Assert.True(Current(r));var e=Owners(r).Single();var at=z.GetEntityPosition(e);
            if(fault=="moved")Assert.True(z.MoveEntity(e,at.x==1?2:1,at.y));
            if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;
            if(fault=="handling")e.GetPart<HandlingPart>().Weight++;
            if(fault=="foreign-physics")e.GetPart<PhysicsPart>().ParentEntity=new Entity();
            Assert.False(Current(r));}
    }
}
