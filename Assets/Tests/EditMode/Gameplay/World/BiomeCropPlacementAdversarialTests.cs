using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeCropPlacementCreationProbe : Part
    {
        public static Action<Entity> Created;
        public override bool HandleEvent(GameEvent e) { if(e.ID=="ObjectCreated")Created?.Invoke(ParentEntity);return true; }
    }
    public sealed class BiomeCropPlacementAdversarialTests
    {
        HaulingContentScope scope;OverworldZoneManager manager;BiomeCropSite site;Zone zone;string crop;int callbacks;
        [SetUp]public void Setup()
        {
            callbacks=0;
            scope=new HaulingContentScope();scope.Seed(64);manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            site=BiomeCropPlan.SurfaceSites(manager).First(s=>s.Biome==BiomeType.Grovelands);zone=new Zone(site.ZoneID);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.AddEntity(scope.Factory.CreateEntity("Grass"),x,y);
            crop=BiomeCropCatalog.ForBiome(site.Biome)[site.SpeciesIndex].CropBlueprint;
        }
        [TearDown]public void Cleanup(){BiomeCropPlacementCreationProbe.Created=null;scope.Dispose();}
        void Hook(Action<Entity> action)
        {
            scope.Factory.RegisterPartType<BiomeCropPlacementCreationProbe>("BiomeCropPlacementCreationProbe");
            BiomeCropPlacementCreationProbe.Created=e=>{callbacks++;action(e);};
            scope.Factory.Blueprints[crop].Parts["BiomeCropPlacementCreationProbe"]=new Dictionary<string,string>();
        }
        bool Install()=>BiomeCropPlacement.TryInstall(manager,zone,site);
        void NoPatch(){Assert.False(BiomeCropPlacement.HasPatch(zone));Assert.False(zone.GetReadOnlyEntities().Any(e=>e.HasPart<CropPart>()||e.HasPart<CultivatedSoilPart>()));}
        [TestCase("interior")][TestCase("liquid")][TestCase("item")][TestCase("barren")]
        public void UnsafeCurrentGroundNeverBecomesAPatch(string fault)
        {
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var c=zone.GetCell(x,y);
                if(fault=="interior")c.IsInterior=true;
                else if(fault=="liquid"){var e=new Entity();e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=1});zone.AddEntity(e,x,y);}
                else if(fault=="item")zone.AddEntity(scope.Factory.CreateEntity("FireClay"),x,y);
                else c.Objects[0].SetTag("Barren");
            }
            var owners=zone.GetReadOnlyEntities().ToArray();Assert.False(Install());NoPatch();CollectionAssert.AreEquivalent(owners,zone.GetReadOnlyEntities());
        }
        [TestCase("untagged")][TestCase("bad-yield")][TestCase("zero-seed")]
        public void InvalidProducedPlantCannotPretendToBeAWorkingCrop(string fault)
        {
            Hook(e=>{if(fault=="untagged")e.Tags.Remove("Crop");if(fault=="bad-yield")e.GetPart<CropPart>().YieldCount=0;if(fault=="zero-seed")e.GetPart<CropPart>().SeedYieldCount=0;});
            bool installed=Install();Assert.Greater(callbacks,0,"Creation probe must run in the separate native test assembly.");Assert.False(installed);NoPatch();
        }
        [TestCase("duplicate-id")][TestCase("later-mutation")][TestCase("foreign-physics")]
        public void WholeStagedPacketIsValidatedAfterEveryFactoryCallback(string fault)
        {
            Entity first=null;int count=0;
            Hook(e=>{if(++count==1){first=e;return;}if(fault=="duplicate-id")e.ID=first.ID;if(fault=="later-mutation")first.GetPart<CropPart>().SeedYieldCount=0;if(fault=="foreign-physics")first.GetPart<PhysicsPart>().ParentEntity=e;});
            bool installed=Install();Assert.AreEqual(2,callbacks);Assert.False(installed);NoPatch();
        }
        [Test] public void ForeignSourceMutationRejectsTheWholeColdGenerationAttempt()
        {
            var old=zone.GetCell(2,2).Objects.Single();Hook(e=>old.ID="changed-by-creation");
            bool accepted=new BiomeCropPlacement(manager,site).BuildZone(zone,scope.Factory,new Random(9));
            Assert.Greater(callbacks,0);Assert.False(accepted);NoPatch();
        }
        [TestCase(false)][TestCase(true)]
        public void DurableReceiptNeverAppearsAsAWorldObjectOrStealsTheSoilAction(bool depleted)
        {
            Assert.True(Install());var record=zone.GetReadOnlyEntities().Single(e=>e.HasPart<BiomeCropPatchPart>());
            var cell=zone.GetEntityCell(record);var plant=cell.Objects.Single(e=>e.HasPart<CropPart>());
            if(depleted)Assert.True(zone.RemoveEntity(plant));
            Assert.False(WorldInteractionSystem.IsPileCell(cell));
            Assert.False(WorldInteractionSystem.DescribeCell(cell).Contains("BiomeCropPatchRecord"));
            Assert.False(WorldInteractionSystem.BuildTargetPickerActions(cell).Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+record.ID));
            Assert.Null(WorldInteractionSystem.FindInCell(cell,record.ID));
            Assert.AreSame(depleted?cell.Objects.Single(e=>e.HasPart<CultivatedSoilPart>()):plant,WorldInteractionSystem.ResolveTarget(cell));
            // Another actual object is still listed; filtering must not hide a
            // legitimate crop/loot pile or silently discard the saved receipt.
            var item=scope.Factory.CreateEntity("FireClay");Assert.True(zone.AddEntity(item,cell.X,cell.Y));
            Assert.AreEqual(!depleted,WorldInteractionSystem.IsPileCell(cell));
            Assert.True(WorldInteractionSystem.BuildTargetPickerActions(cell).Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+item.ID));
            Assert.True(BiomeCropPlacement.HasPatch(zone));
        }
        [TestCase(false)][TestCase(true)]
        public void RemovingMapPoiCannotReclassifyAnAlreadyClaimedLairAsOrdinary(bool claimed)
        {
            var candidate=OverworldZoneManager.CreateDetached(scope.Factory,64);
            var at=WorldMap.FromZoneID(site.ZoneID);
            candidate.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.Lair,"saved crop exclusion",null,2,"JungleStalker"));
            if(claimed)
            {
                Assert.NotNull(candidate.GetZone(site.ZoneID));
                Assert.NotNull(LairStacks.Inspect(candidate,site.ZoneID));
            }
            candidate.WorldMap.SetPOI(at.x,at.y,null);
            Assert.AreEqual(!claimed,BiomeCropPlan.IsOrdinaryCave(candidate,WorldMap.ToZoneID(at.x,at.y,1)));
            if(!claimed)
            {
                var table=typeof(LairStacks).GetField("Ledgers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
                Assert.False((bool)table.GetType().GetMethod("TryGetValue").Invoke(table,new object[]{candidate,null}),"Read-only ecology queries must not initialize a lair ledger.");
                Assert.Zero(candidate.CachedZoneCount);
            }
        }
        [Test] public void ReceiptSurvivesRemovalOfEveryCropAndCultivatedTerrainOwner()
        {
            Assert.True(Install());var receipt=zone.GetReadOnlyEntities().Single(e=>e.HasPart<BiomeCropPatchPart>());
            foreach(var e in zone.GetReadOnlyEntities().Where(e=>e.HasPart<CropPart>()||e.HasPart<CultivatedSoilPart>()).ToArray())Assert.True(zone.RemoveEntity(e));
            Assert.True(BiomeCropPlacement.HasPatch(zone));Assert.False(Install());manager.SetActiveZone(zone);manager.UnloadZone(zone.ZoneID);
            Assert.AreSame(zone,manager.GetZone(zone.ZoneID));Assert.NotNull(zone.GetEntityCell(receipt));
        }
        [Test] public void NativeSaveKeepsDepletedReceiptWithoutRefillingOrChangingCachedGround()
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                Assert.True(Install());var record=zone.GetReadOnlyEntities().Single(e=>e.HasPart<BiomeCropPatchPart>());
                foreach(var e in zone.GetReadOnlyEntities().Where(e=>e.HasPart<CropPart>()).ToArray())Assert.True(zone.RemoveEntity(e));
                manager.SetActiveZone(zone);var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("biome-crop","retained-site",manager,null,null));
                var saved=loaded.ZoneManager.ActiveZone;Assert.NotNull(saved.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==record.ID));
                Assert.True(BiomeCropPlacement.HasPatch(saved));Assert.False(saved.GetReadOnlyEntities().Any(e=>e.HasPart<CropPart>()));
                loaded.ZoneManager.UnloadZone(saved.ZoneID);Assert.AreSame(saved,loaded.ZoneManager.GetZone(saved.ZoneID));
                Assert.False(BiomeCropPlacement.TryInstall(loaded.ZoneManager,saved,BiomeCropPlan.ForZone(loaded.ZoneManager,saved.ZoneID)));
            }
        }
        sealed class ObservedRandom:Random{public int Calls;public override int Next(int max){Calls++;return base.Next(max);}public override int Next(int min,int max){Calls++;return base.Next(min,max);}public override double NextDouble(){Calls++;return base.NextDouble();}}
        [Test] public void PatchNeverAdvancesOrdinaryPopulationOrLootRandomStream()
        {var rng=new ObservedRandom();Assert.True(new BiomeCropPlacement(manager,site).BuildZone(zone,scope.Factory,rng));Assert.AreEqual(0,rng.Calls);}
    }
}
