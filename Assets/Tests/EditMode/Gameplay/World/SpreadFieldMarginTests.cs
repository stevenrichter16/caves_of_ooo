using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadFieldMarginTests
    {
        const string Id="Overworld.10.15.0";
        DensityLootTestScope scope;
        [SetUp] public void SetUp()=>scope=new DensityLootTestScope();
        [TearDown] public void TearDown()=>scope.Dispose();
        sealed class Fixture
        {
            internal OverworldZoneManager Manager;
            internal Zone Zone;
            internal SpreadCompositionBuilder Land;
            internal IZoneBuilder Margin;
            internal Random Rng=new Random(901);
            internal Fixture(CavesOfOoo.Data.EntityFactory factory,int seed=65,string id=Id)
            {
                Manager=OverworldZoneManager.CreateDetached(factory,seed,true);
                var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Manager,new object[]{id});
                Land=pipeline.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();
                Margin=pipeline.Builders.SingleOrDefault(b=>b.Name=="SpreadFieldMargin");
                Zone=new Zone(id);
                foreach(var b in pipeline.Builders.OrderBy(b=>b.Priority).Where(b=>b.Priority<3950))Assert.True(b.BuildZone(Zone,factory,Rng),b.Name);
            }
            internal void Run(CavesOfOoo.Data.EntityFactory factory)
            {Assert.NotNull(Margin,"Missing optional cold field-margin composition");Assert.True(Margin.BuildZone(Zone,factory,Rng));}
        }
        static string[] Shape(Zone z)=>z.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)).OrderBy(s=>s).ToArray();
        [Test] public void Seed65AddsThreeSmallGroupsWithoutChangingExistingOwnersOrSources()
        {
            var f=new Fixture(scope.Factory);var z=f.Zone;
            var before=z.GetReadOnlyEntities().ToDictionary(e=>e,e=>z.GetEntityPosition(e));
            string state=z.TileState.ToSaveString(),signature=f.Land.Plan.Signature();var reserves=z.GenReservedCells.ToArray();
            var reach=WaysiteBuilder.Reach(z);f.Run(scope.Factory);
            var added=z.GetReadOnlyEntities().Where(e=>!before.ContainsKey(e)).ToArray();
            Assert.That(added.Length,Is.InRange(12,18));Assert.True(added.All(e=>e.BlueprintName=="Tree"||e.BlueprintName=="Bush"));
            Assert.That(added.Count(e=>e.BlueprintName=="Tree"),Is.InRange(2,5));
            foreach(var p in before){Assert.AreEqual(p.Value,z.GetEntityPosition(p.Key));Assert.AreSame(p.Key,z.GetReadOnlyEntities().Single(e=>e.ID==p.Key.ID));}
            foreach(var e in added){var p=z.GetEntityPosition(e);Assert.IsNull(f.Land.Plan.ObjectAt(p.x,p.y));Assert.False(f.Land.Plan.IsApproach(p.x,p.y));for(int i=0;i<f.Land.Plan.ParcelCount;i++)Assert.False(f.Land.Plan.GetParcel(i).Contains(p.x,p.y));}
            Assert.AreEqual(state,z.TileState.ToSaveString());CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);Assert.AreEqual(signature,f.Land.Plan.Signature());
            var after=WaysiteBuilder.Reach(z);Assert.GreaterOrEqual(after.cells,reach.cells-added.Count(e=>e.GetPart<PhysicsPart>()?.Solid==true));Assert.LessOrEqual(after.parts,reach.parts);
            TestContext.WriteLine(string.Join(";",added.Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e))));
        }
        [Test] public void DeterministicWithoutConsumingCallerRandomOrStacking()
        {
            var a=new Fixture(scope.Factory);var b=new Fixture(scope.Factory);
            a.Rng=new Random(118);b.Rng=new Random(118);a.Run(scope.Factory);b.Run(scope.Factory);
            CollectionAssert.AreEqual(Shape(a.Zone),Shape(b.Zone));Assert.AreEqual(new Random(118).Next(),a.Rng.Next());
            var before=Shape(a.Zone);a.Run(scope.Factory);CollectionAssert.AreEqual(before,Shape(a.Zone));
        }
        [TestCase("Tree")][TestCase("Bush")]
        public void MissingResourceDeclinesAtomically(string blueprint)
        {
            var f=new Fixture(scope.Factory);var before=Shape(f.Zone);scope.Factory.Blueprints.Remove(blueprint);f.Run(scope.Factory);CollectionAssert.AreEqual(before,Shape(f.Zone));
        }
        [TestCase("reserved")][TestCase("wet")][TestCase("occupied")][TestCase("interior")]
        public void UnavailableMarginsLeaveOriginalGraphUntouched(string reason)
        {
            var f=new Fixture(scope.Factory);var z=f.Zone;
            for(int y=2;y<23;y++)for(int x=2;x<78;x++)
            {
                if(reason=="reserved")z.GenReservedCells.Add((x,y));
                else if(reason=="wet")z.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);
                else if(reason=="interior")z.GetCell(x,y).IsInterior=true;
                else {var e=new Entity{ID="owned-"+x+"-"+y,BlueprintName="untouched"};z.AddEntity(e,x,y);}
            }
            var before=Shape(z);var state=z.TileState.ToSaveString();var reserves=z.GenReservedCells.ToArray();f.Run(scope.Factory);
            CollectionAssert.AreEqual(before,Shape(z));Assert.AreEqual(state,z.TileState.ToSaveString());CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);
        }
        [Test] public void ForeignFactoryCannotDressTheGraph()
        {var f=new Fixture(scope.Factory);var before=Shape(f.Zone);f.Run(GrovelandsCompositionTests.Factory());CollectionAssert.AreEqual(before,Shape(f.Zone));}
        [Test] public void CachedGraphCannotBeRetrofitted()
        {var f=new Fixture(scope.Factory);f.Manager.CachedZones.Add(Id,f.Zone);var before=Shape(f.Zone);f.Run(scope.Factory);CollectionAssert.AreEqual(before,Shape(f.Zone));}
        [TestCase("row")][TestCase("injected")][TestCase("earlier-barren")][TestCase("reserve")][TestCase("tile")][TestCase("empty-id")][TestCase("duplicate-id")]
        public void CreationCallbacksCannotChangeTheAcceptedBaseline(string mutation)
        {
            var f=new Fixture(scope.Factory);var before=f.Zone.GetReadOnlyEntities().ToArray();Entity first=null,injected=null;
            scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
            foreach(string bp in new[]{"Tree","Bush"})scope.Factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{
                if(first==null){first=e;return;}
                if(mutation=="row")before.First(o=>o.BlueprintName=="RipeCropRow").GetPart<FieldHarvestPart>().Harvested=true;
                else if(mutation=="earlier-barren")first.Tags["Barren"]="true";
                else if(mutation=="empty-id")first.ID="";
                else if(mutation=="duplicate-id")first.ID=e.ID;
                else if(mutation=="reserve")f.Zone.GenReservedCells.Add((3,3));
                else if(mutation=="tile")f.Zone.TileState.WriteCoating(3,3,"water",ZoneTileState.Permanent);
                else if(injected==null){injected=new Entity{ID="foreign-callback",BlueprintName="foreign"};f.Zone.AddEntity(injected,3,3);}
            };
            try{Assert.NotNull(f.Margin);bool ok=f.Margin.BuildZone(f.Zone,scope.Factory,f.Rng);
                Assert.AreEqual(mutation=="earlier-barren"||mutation=="empty-id"||mutation=="duplicate-id",ok,"Foreign graph changes fail the cold attempt; invalid optional payloads simply decline");
                Assert.NotNull(first);CollectionAssert.AreEquivalent(injected==null?before:before.Concat(new[]{injected}),f.Zone.GetReadOnlyEntities());}
            finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
        [TestCase("Barren")][TestCase("FieldHarvest")][TestCase("SpatialFootprint")][TestCase("solid")]
        public void MalformedBushNeverDamagesOriginalGround(string mutation)
        {
            var f=new Fixture(scope.Factory);var before=Shape(f.Zone);var bp=scope.Factory.GetBlueprint("Bush");
            if(mutation=="Barren")bp.Tags[mutation]="true";
            else if(mutation=="solid")bp.Parts["Physics"]["Solid"]="true";
            else bp.Parts[mutation]=new Dictionary<string,string>();
            f.Run(scope.Factory);CollectionAssert.AreEqual(before,Shape(f.Zone));
        }
        [Test] public void FullPipelineRetainsGleaningsRoleAndCachedVisitDoesNotDuplicate()
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,65,true);var z=m.GetZone(Id);Assert.NotNull(z);
            Assert.AreEqual(2,m.Exploration.DispositionFor(Id),"Actual finite-source role must still commit");
            Assert.True(z.GetReadOnlyEntities().Any(e=>e.GetPart("SpreadGrazer")!=null));
            var before=Shape(z);Assert.AreSame(z,m.GetZone(Id));CollectionAssert.AreEqual(before,Shape(z));
        }
        [Test] public void SceneryCannotPublishAThrowingRelocationCallback()
        {
            var f=new Fixture(scope.Factory);var before=Shape(f.Zone);bool invoked=false;
            scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
            scope.Factory.Blueprints["Tree"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{
                var state=new ThrowingStatePart();e.AddPart(state);
                typeof(ThrowingStatePart).GetField("RelocationCommitted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(state,(Action)(()=>invoked=true));
            };
            try{f.Run(scope.Factory);Assert.False(invoked,"Malformed scenery must be refused before AddEntity callbacks");CollectionAssert.AreEqual(before,Shape(f.Zone));}
            finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
        [TestCase(64)][TestCase(777)]
        public void NonSelectedAddressHasNoMarginBuilder(int seed)
        {var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var e=m.Exploration.Entries.First(x=>x.Family!=SpreadExplorationFamily.LastGleanings);var f=new Fixture(scope.Factory,seed,e.ZoneID);Assert.IsNull(f.Margin);}
    }
}
