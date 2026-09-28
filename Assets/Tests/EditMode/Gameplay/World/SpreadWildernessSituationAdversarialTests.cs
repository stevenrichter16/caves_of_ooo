using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadWildernessSituationAdversarialTests
    {
        DensityLootTestScope scope;OverworldZoneManager manager;
        [SetUp]public void SetUp(){scope=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(scope.Factory,64);}
        [TearDown]public void TearDown(){scope.Dispose();}
        static IEnumerable<string> IDs(){for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)yield return WorldMap.ToZoneID(x,y);}
        static PopulationTable Group()=>new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName="MarlbackScrabbler",EncounterGroup="SpreadTier1Encounter",MinCount=2,MaxCount=2}}};
        (Zone z,SpreadCompositionBuilder terrain,PopulationBuilder population,ContainerBuilder containers,SpreadWildernessSituationBuilder composer) Build(string kind="shelter")
        {
            string id=IDs().First(i=>SpreadWildernessSituationPlan.Select(manager,i,manager.Wayhouse?.ZoneID)==kind);var z=new Zone(id);var terrain=new SpreadCompositionBuilder(64);var p=new PopulationBuilder(Group());var c=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);var b=new SpreadWildernessSituationBuilder(manager,terrain,p,c,manager.Wayhouse?.ZoneID);
            Assert.True(terrain.BuildZone(z,scope.Factory,new Random(1)));var rng=new Random(14);p.BuildZone(z,scope.Factory,rng);c.BuildZone(z,scope.Factory,rng);new HaulablePropBuilder(BiomeType.Spread).BuildZone(z,scope.Factory,rng);
            Assert.True(p.SourceReceipt.IsCurrent);Assert.True(c.SourceReceipt.IsCurrent);return(z,terrain,p,c,b);
        }
        static string Graph(Zone z)=>string.Join(";",z.GetReadOnlyEntities().Select(e=>e.ID+"/"+e.BlueprintName+"@"+z.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(e)).Select(i=>i.ID+"/"+i.BlueprintName+"/"+(i.GetPart<StackerPart>()?.StackCount??1)).OrderBy(s=>s))).OrderBy(s=>s))+z.TileState.ToSaveString();
        [TestCase("actor-moved")][TestCase("actor-dead")][TestCase("actor-removed")][TestCase("stock-removed")][TestCase("stock-quantity")][TestCase("terrain-rebuilt")][TestCase("capture-disabled")][TestCase("map-changed")][TestCase("population-rebuilt")]
        public void LateStaleStateRefusesWithoutMovingAnyOtherOwner(string mutation)
        {
            var b=Build();var actor=b.population.SourceReceipt.Owners[0];var cache=b.containers.SourceReceipt.Owners[0];
            if(mutation=="actor-moved"){var p=b.z.GetEntityPosition(actor);Assert.True(b.z.MoveEntity(actor,p.x+1,p.y));Assert.AreNotEqual(p,b.z.GetEntityPosition(actor));}
            if(mutation=="actor-dead")actor.GetStat("Hitpoints").BaseValue=0;
            if(mutation=="actor-removed")b.z.RemoveEntity(actor);
            if(mutation=="stock-removed")cache.GetPart<ContainerPart>().RemoveItem(cache.GetPart<ContainerPart>().Contents[0]);
            if(mutation=="stock-quantity"){var item=cache.GetPart<ContainerPart>().Contents[0];var stack=item.GetPart<StackerPart>();if(stack==null){stack=new StackerPart();item.AddPart(stack);}stack.StackCount++;}
            if(mutation=="terrain-rebuilt")Assert.True(b.terrain.BuildZone(new Zone(b.z.ZoneID),scope.Factory,new Random(1)));
            if(mutation=="capture-disabled")b.population.CaptureSourceReceipts=false;
            if(mutation=="map-changed"){var p=WorldMap.FromZoneID(b.z.ZoneID);manager.WorldMap.Tiles[p.x,p.y]=BiomeType.Beating;}
            if(mutation=="population-rebuilt")b.population.BuildZone(new Zone(b.z.ZoneID),scope.Factory,new Random(14));
            string before=Graph(b.z);Assert.True(b.composer.BuildZone(b.z,scope.Factory,new Random(711)));Assert.AreNotEqual("shelter",b.composer.LastResult);Assert.AreEqual(before,Graph(b.z));
        }
        [TestCase("cargo")][TestCase("shelter")]
        public void FirstQualifyingGeneratedCacheMovesWithExactStockAndNoSourceEvents(string kind)
        {
            var b=Build(kind);var expected=b.containers.SourceReceipt.Owners.First(e=>(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&!e.GetPart<ContainerPart>().IsLocked);
            var before=b.z.GetReadOnlyEntities().ToDictionary(e=>e,e=>b.z.GetEntityPosition(e));var stock=expected.GetPart<ContainerPart>().Contents.ToArray();var actors=b.population.SourceReceipt.Owners.ToArray();
            Assert.True(b.composer.BuildZone(b.z,scope.Factory,new Random(77)));Assert.AreEqual(kind,b.composer.LastResult);Assert.AreNotEqual(before[expected],b.z.GetEntityPosition(expected));CollectionAssert.AreEqual(stock,expected.GetPart<ContainerPart>().Contents);
            foreach(var e in before.Keys.Where(e=>e!=expected&&(kind=="cargo"||!actors.Contains(e))))Assert.AreEqual(before[e],b.z.GetEntityPosition(e),e.BlueprintName);
            var all=new[]{expected}.Concat(kind=="shelter"?actors:Array.Empty<Entity>()).ToArray();Assert.AreEqual(all.Length,all.Select(e=>b.z.GetEntityPosition(e)).Distinct().Count());
            foreach(var e in all){var p=b.z.GetEntityPosition(e);Assert.False(b.z.GenReservedCells.Contains(p));Assert.True(b.z.TileState.Get(p.x,p.y)?.IsEmpty!=false);Assert.False(b.z.GetCell(p.x,p.y).Occupants.Any(o=>o!=e&&o.BlueprintName!="Grass"&&o.BlueprintName!="RoadStone"));}
            Assert.False(b.containers.SourceReceipt.IsCurrent);Assert.AreEqual(kind=="cargo",b.population.SourceReceipt.IsCurrent);
        }
        [Test]public void CompetingComposerCannotReuseConsumedSource()
        {var b=Build();var other=new SpreadWildernessSituationBuilder(manager,b.terrain,b.population,b.containers,"");b.composer.BuildZone(b.z,scope.Factory,new Random(1));Assert.AreEqual("shelter",b.composer.LastResult);string before=Graph(b.z);other.BuildZone(b.z,scope.Factory,new Random(1));Assert.AreEqual(before,Graph(b.z));Assert.AreNotEqual("shelter",other.LastResult);}
        [Test]public void NewOrdinaryUnrelatedCreatureAndPhysicsOnlyFurnitureAreNeverMoved()
        {var b=Build();var foreign=scope.Factory.CreateEntity("MarlbackScrabbler");var furniture=scope.Factory.CreateEntity("Chair");Assert.True(b.z.AddEntity(foreign,0,0));Assert.True(b.z.AddEntity(furniture,1,0));b.composer.BuildZone(b.z,scope.Factory,new Random(1));Assert.AreEqual("shelter",b.composer.LastResult);Assert.AreEqual((0,0),b.z.GetEntityPosition(foreign));Assert.AreEqual((1,0),b.z.GetEntityPosition(furniture));}
        [TestCase("coating")][TestCase("cold")][TestCase("charge")]
        public void PhysicalHazardEverywhereCannotBeUsedAsAMissingSituation(string kind)
        {var b=Build();b.z.ForEachCell((c,x,y)=>{if(kind=="coating")b.z.TileState.WriteCoating(x,y,"water",3);if(kind=="cold")b.z.TileState.AddCold(x,y,1);if(kind=="charge")b.z.TileState.AddCharge(x,y,1);});string before=Graph(b.z);b.composer.BuildZone(b.z,scope.Factory,new Random(1));Assert.AreEqual(before,Graph(b.z));Assert.AreNotEqual("shelter",b.composer.LastResult);}


        [TestCase(true)][TestCase(false)]
        public void CurrentWayhouseReplacementCannotSpendThisZonesOrdinarySources(bool selectsHere)
        {
            var b=Build("cargo");string old=manager.Wayhouse.ZoneID;Assert.AreNotEqual(b.z.ZoneID,old);
            var world=new Entity();world.Properties[SpreadWayhousePlan.PropertyKey]="1|"+(selectsHere?b.z.ZoneID:old);
            var replacement=SpreadWayhousePlan.Restore(world);typeof(OverworldZoneManager).GetProperty("Wayhouse").SetValue(manager,replacement);
            Assert.AreEqual(selectsHere,replacement.Selects(manager,b.z.ZoneID));string before=Graph(b.z);
            b.composer.BuildZone(b.z,scope.Factory,new Random(1));
            if(selectsHere){Assert.AreEqual(before,Graph(b.z));Assert.AreNotEqual("cargo",b.composer.LastResult);Assert.True(b.containers.SourceReceipt.IsCurrent);}
            else Assert.AreEqual("cargo",b.composer.LastResult);
        }
        [Test]public void NoBypassHasAnExplicitFinitePlanBudget()
        {
            var property=typeof(SpreadWildernessSituationBuilder).GetProperty("LayoutTrials");Assert.NotNull(property,"Full physical trials need a bounded per-build budget.");
            scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");scope.Factory.Blueprints["MarlbackScrabbler"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>e.GetPart<BrainPart>().SightRadius=1000;
            try{var b=Build();string before=Graph(b.z);b.composer.BuildZone(b.z,scope.Factory,new Random(3));Assert.AreEqual(before,Graph(b.z));Assert.AreEqual("refused:layout-budget",b.composer.LastResult);Assert.AreEqual(256,(int)property.GetValue(b.composer));}
            finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
        [Test]public void Seed64DoesNotAliasSituationChoiceToOneFormationBucket()
        {var kinds=IDs().Select(id=>SpreadWildernessSituationPlan.Select(manager,id)).Where(s=>s.Length>0).Distinct().ToArray();CollectionAssert.Contains(kinds,"cargo");CollectionAssert.Contains(kinds,"shelter");}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ThreeSeedRealSourceCensusKeepsEveryOwnerAndCallerRng(int seed)
        {
            manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);var lines=new List<string>();int selected=0,committed=0;
            foreach(var id in IDs())
            {
                string kind=SpreadWildernessSituationPlan.Select(manager,id,manager.Wayhouse?.ZoneID);if(kind!="cargo"&&kind!="shelter")continue;selected++;
                var z=new Zone(id);var t=new SpreadCompositionBuilder(seed);var p=new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Spread,1));var c=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);var b=new SpreadWildernessSituationBuilder(manager,t,p,c,manager.Wayhouse?.ZoneID);
                Assert.True(t.BuildZone(z,scope.Factory,new Random(seed)));var r=new Random(unchecked(seed+FormationSelector.StableIndex(id,int.MaxValue)));p.BuildZone(z,scope.Factory,r);c.BuildZone(z,scope.Factory,r);new HaulablePropBuilder(BiomeType.Spread).BuildZone(z,scope.Factory,r);
                var owners=z.GetReadOnlyEntities().ToArray();var snapshots=owners.Select(e=>(e,e.GetStatValue("Hitpoints"),items:(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(e)).ToArray())).ToArray();var a=new Random(771);var control=new Random(771);b.BuildZone(z,scope.Factory,a);Assert.AreEqual(control.Next(),a.Next());CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
                foreach(var old in snapshots){Assert.AreEqual(old.Item2,old.e.GetStatValue("Hitpoints"));CollectionAssert.AreEqual(old.items,(old.e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(old.e)));}
                if(b.LastResult==kind)committed++;lines.Add(id+" "+kind+" "+b.LastResult+" actors="+string.Join(",",p.SourceReceipt.Owners.Select(e=>e.BlueprintName))+" containers="+string.Join(",",c.SourceReceipt.Owners.Select(e=>e.BlueprintName)));
            }
            Assert.Greater(selected,0);TestContext.WriteLine("seed="+seed+" selected="+selected+" committed="+committed+"\n"+string.Join("\n",lines));
        }
    }
}
