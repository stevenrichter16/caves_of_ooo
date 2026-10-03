using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold real-content admission: connected services must exist on playable
    /// owners, with their original public alternatives and finite stock preserved.</summary>
    public sealed class ConnectedSpreadContentTests
    {
        HaulingContentScope scope; bool oldWorldgen;
        [SetUp] public void Setup(){oldWorldgen=Diag.IsChannelEnabled("worldgen");scope=new HaulingContentScope();scope.Seed(64);Diag.SetChannel("worldgen",true);}
        [TearDown] public void Cleanup(){scope.Dispose();Diag.SetChannel("worldgen",oldWorldgen);}
        static string Evidence()=>string.Join("\n",Diag.Snapshot(150).Where(e=>e.Category=="worldgen"&&(e.Kind.Contains("Rejected")||e.Kind.Contains("Refused"))).TakeLast(8).Select(e=>e.Kind+": "+e.PayloadJson));
        static Entity One(Zone z,string bp){Assert.NotNull(z,Evidence());return z.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName==bp);}
        static void Prepared(Zone z,Entity crop)
        {
            var c=z.GetEntityCell(crop);Assert.NotNull(c);
            Assert.True(c.Objects.Any(e=>e.HasTag("Terrain")&&e.HasTag("Plantable")&&e.HasPart<CultivatedSoilPart>()),"A crop model is not a replantable bed.");
        }
        [TestCase("ConnectedBatchPan","KitchenBatch")]
        [TestCase("BotanicalInkDesk","BotanicalInkDesk")]
        [TestCase("FieldMeal","FieldMeal")]
        [TestCase("ConnectedKitchenEscrow","Container")]
        [TestCase("ConnectedKitchenPickup","Container")]
        public void NewContentHasItsActualActionPart(string blueprint,string part)
        {
            Assert.True(scope.Factory.Blueprints.ContainsKey(blueprint),"Missing usable authored content: "+blueprint);
            var e=scope.Factory.CreateEntity(blueprint);Assert.NotNull(e);
            Assert.True(e.Parts.Any(p=>p.Name==part));Assert.True(e.GetPart<RenderPart>().Visible);
        }
        [Test] public void DitchkeepersManualTeachesActualVaultWithoutBorrowingRiteInk()
        {
            Assert.True(scope.Factory.Blueprints.ContainsKey("DitchkeepersFootwork"));
            var e=scope.Factory.CreateEntity("DitchkeepersFootwork");
            Assert.AreEqual("Acrobatics_Vault",e.GetPart<GrimoirePart>()?.SkillClassName);
            Assert.False(e.HasPart<GrimoireChargePart>());
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void KitchenAddsAnInvestmentAndCropWithoutRemovingFreeOvenOrCot(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var z=manager.GetZone("Overworld.12.11.0");
            var pan=One(z,"ConnectedBatchPan");Assert.NotNull(pan,"Ordinary near-spawn kitchen must host the new service.\n"+Evidence());
            Assert.True(RepairablePart.BlocksFunction(pan));Assert.AreEqual("clay-batch-pan",pan.GetPart<RepairablePart>().RecipeId);
            Assert.NotNull(One(z,"ConnectedKitchenEscrow"));Assert.NotNull(One(z,"ConnectedKitchenPickup"));
            Assert.NotNull(One(z,"Oven")?.GetPart<CampfirePart>());Assert.NotNull(One(z,"Bed")?.GetPart<BedPart>());
            var crop=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ClaspbeanCrop"&&e.GetProperty("ConnectedSpread.Role")=="kitchen-crop");
            Prepared(z,crop);Assert.AreEqual(2,crop.GetPart<CropPart>().GrowthStage);
            Assert.That(One(z,"SpreadWaysideCook").GetPart<RenderPart>().DisplayName,Does.Contain("Orven"));
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void SeedkeeperHasTwoMarkedReserveBedsAndKeepsFourPublicCrops(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var z=manager.GetZone("Overworld.11.8.0");
            Assert.NotNull(z,Evidence());
            var reserve=z.GetReadOnlyEntities().Where(e=>e.GetProperty("ConnectedSpread.Role")=="reserve-crop").ToArray();
            Assert.AreEqual(2,reserve.Length,Evidence());CollectionAssert.AreEquivalent(new[]{"MarlrootCrop","PitchpodCrop"},reserve.Select(e=>e.BlueprintName));
            Assert.AreEqual(1,reserve.Count(e=>e.GetPart<CropPart>().GrowthStage==2));Assert.AreEqual(1,reserve.Count(e=>e.GetPart<CropPart>().GrowthStage==0));
            foreach(var e in reserve)Prepared(z,e);
            Assert.AreEqual(4,z.GetReadOnlyEntities().Count(e=>e.GetProperty(SpreadExplorationResidents.RoleKey)=="crop"));
            Assert.That(One(z,"SpreadSeedKeeper").GetPart<RenderPart>().DisplayName,Does.Contain("Nella"));
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void CellarAddsHarvestableSootrootBedsWithoutReplacingItsOriginalSupplies(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);manager.GetZone("Overworld.11.10.0");var z=manager.GetZone("Overworld.11.10.1");
            var roots=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SootrootCrop").ToArray();Assert.AreEqual(2,roots.Length);
            Assert.AreEqual(1,roots.Count(e=>e.GetPart<CropPart>().GrowthStage==2));Assert.AreEqual(1,roots.Count(e=>e.GetPart<CropPart>().GrowthStage==0));
            foreach(var e in roots)Prepared(z,e);
            var cache=z.GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="supplies").GetPart<ContainerPart>();
            Assert.AreEqual(2,cache.Contents.Where(e=>e.BlueprintName=="FireClay").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
            Assert.AreEqual(1,cache.Contents.Count(e=>e.BlueprintName!="FireClay"));
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void CurationHasPublicInkServiceAndExactlyOnePurchasableBook(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);Zone z=null;Assert.DoesNotThrow(()=>z=manager.GetZone("Overworld.12.12.0"));
            var desk=One(z,"BotanicalInkDesk");Assert.NotNull(desk);var ivrin=One(z,"CurationJuniorIndexer");Assert.NotNull(ivrin?.GetPart<TraderPart>());
            Assert.IsEmpty(ivrin.GetPart<TraderPart>().StockTable??"");
            Assert.AreEqual(1,ivrin.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="ShatteredRimeGrimoire"));
            Assert.NotNull(One(z,"CurationIntakeIndex"));Assert.NotNull(One(z,"CurationToolCabinet"));
            Assert.LessOrEqual(Math.Abs(z.GetEntityPosition(desk).x-z.GetEntityPosition(ivrin).x),1);
            Assert.LessOrEqual(Math.Abs(z.GetEntityPosition(desk).y-z.GetEntityPosition(ivrin).y),1);
        }

        [Test] public void ActualWorldIdentitySurvivesManifestSaveAndDiffersForSameSeedWorlds()
        {
            var first=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var second=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var prop=typeof(SpreadExplorationPlan).GetProperty("WorldKey");Assert.NotNull(prop,"Connected accomplishments need actual saved world identity, not just seed equality.");
            var key=(string)prop.GetValue(first.Exploration);Assert.True(Guid.TryParseExact(key,"N",out _));Assert.AreNotEqual(key,prop.GetValue(second.Exploration));
            var world=SpreadExplorationPlan.BindForSave(first,null);Assert.AreEqual(key,world.GetProperty("SpreadExploration.WorldKey"));
            var restore=typeof(SpreadExplorationPlan).GetMethod("Restore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var loaded=restore.Invoke(null,new object[]{first,world});Assert.AreEqual(key,prop.GetValue(loaded));
        }
        [Test] public void OriginalCellarClayDryRootAndSurfaceWellCarryFiniteWorldSources()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var surface=manager.GetZone(GleanersDistrict.SurfaceID);var cellar=manager.GetZone(GleanersCellarBuilder.ZoneID);
            var well=One(surface,"RepairLinedWell");Assert.NotNull(well.GetPart<ConnectedSpreadSourcePart>());
            Assert.AreEqual(manager.Exploration.WorldKey,well.GetPart<ConnectedSpreadSourcePart>().WorldKey);
            var clay=cellar.GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="supplies")
                .GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="FireClay");
            Assert.NotNull(clay.GetPart<ConnectedClayOriginPart>());Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units);
            var dry=cellar.GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="sootroot-dry");
            Assert.NotNull(dry.GetPart<ConnectedSpreadSourcePart>());
            var ripe=cellar.GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="sootroot-ripe");
            Assert.Null(ripe.GetPart<ConnectedSpreadSourcePart>());
        }
        [Test] public void CachedKitchenGraphIsNotRetrofitted()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var old=new Zone("Overworld.12.11.0");manager.CachedZones[old.ZoneID]=old;
            var e=scope.Factory.CreateEntity("Grass");old.AddEntity(e,40,12);Assert.AreSame(old,manager.GetZone(old.ZoneID));Assert.AreEqual(1,old.EntityCount);
        }
    }
}
