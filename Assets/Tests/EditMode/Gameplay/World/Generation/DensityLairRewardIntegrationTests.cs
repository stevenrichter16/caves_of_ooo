using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairRewardIntegrationTests
    {
        const string Surface="Overworld.4.9.0", Bottom="Overworld.4.9.2", Marker="LairLooseRewardSurface";
        DensityLootTestScope scope;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){scope.Dispose();}
        OverworldZoneManager Manager(int seed,BiomeType biome=BiomeType.Sodden)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed);Configure(m,biome);return m;
        }
        void Configure(OverworldZoneManager m,BiomeType biome)
        {m.WorldMap.Tiles[4,9]=biome;m.WorldMap.SetPOI(4,9,new PointOfInterest(POIType.Lair,"reward probe",null,3,biome==BiomeType.Grovelands?"JungleStalker":biome==BiomeType.Beating?"DesertProwler":"MarlbackWallkeeper"));}
        (OverworldZoneManager manager,Zone zone,Entity item) WithLoose()
        {
            for(int seed=1;seed<33;seed++){var m=Manager(seed);var z=m.GetZone(Bottom);var item=z.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(Marker)==Surface);if(item!=null)return(m,z,item);}
            Assert.Fail("Real Sodden final floors must sometimes retain a loose tier find.");return default;
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void ActualFinalOwnerReceivesConsolidatedBudgetOnlyOnce(BiomeType biome)
        {
            var m=Manager(64,biome);var z=m.GetZone(Bottom);var plan=LairStacks.Inspect(m,Surface);
            var owner=z.GetReadOnlyEntities().Single(e=>e.ID==plan.RewardID);
            Assert.AreEqual(32,owner.GetPart<ContainerPart>().MaxItems);
            string[] ids=owner.GetPart<ContainerPart>().Contents.Select(e=>e.ID).ToArray();
            m.UnloadZone(Bottom);Assert.AreSame(z,m.GetZone(Bottom));
            CollectionAssert.AreEqual(ids,owner.GetPart<ContainerPart>().Contents.Select(e=>e.ID));
            for(int depth=0;depth<2;depth++)Assert.False(m.GetZone(WorldMap.ToZoneID(4,9,depth)).GetReadOnlyEntities().Any(e=>e.GetProperty(Marker)!=null));
        }
        [Test]
        public void OptionalSoddenFindUsesRealTierPoolAwayFromReservedRoutesAndIsNotUniversal()
        {
            int with=0,without=0;
            for(int seed=1;seed<=24;seed++)
            {
                var m=Manager(seed);var z=m.GetZone(Bottom);var items=z.GetReadOnlyEntities().Where(e=>e.GetProperty(Marker)==Surface).ToArray();
                Assert.LessOrEqual(items.Length,1);
                if(items.Length==0){without++;continue;}with++;
                Assert.True(items[0].HasPart<MeleeWeaponPart>()||items[0].HasPart<ArmorPart>());
                foreach(var c in z.GetOccupiedCells(items[0]))Assert.False(z.GenReservedCells.Contains((c.X,c.Y)));
            }
            Assert.Greater(with,0);Assert.Greater(without,0);
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void OtherBiomesNeverGainSoddenLooseEquipment(BiomeType biome)
        {
            var m=Manager(17,biome);var z=m.GetZone(Bottom);
            Assert.False(z.GetReadOnlyEntities().Any(e=>e.GetProperty(Marker)!=null));
        }
        [TestCase(false)] [TestCase(true)]
        public void PickedUpOrDestroyedLooseFindNeverReappearsAfterRealSave(bool destroyed)
        {
            var pair=WithLoose();var m=pair.manager;var z=pair.zone;string id=pair.item.ID;
            var player=scope.Factory.CreateEntity("Player");Assert.True(z.AddEntity(player,36,12));m.SetActiveZone(z);
            z.RemoveEntity(pair.item);
            if(!destroyed)Assert.True(player.GetPart<InventoryPart>().AddObject(pair.item));
            var old=TurnManager.Active;
            try
            {
                var turns=new TurnManager();turns.RestoreSavedState(71,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
                var state=GameSessionState.Capture("loose-reward-test","v",m,turns,player);
                using(var stream=new MemoryStream())
                {
                    state.Save(new SaveWriter(stream));stream.Position=0;
                    var loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));
                    loaded.ZoneManager.UnloadZone(Bottom);var saved=loaded.ZoneManager.GetZone(Bottom);
                    Assert.False(saved.GetReadOnlyEntities().Any(e=>e.ID==id||e.GetProperty(Marker)!=null));
                    Assert.AreEqual(destroyed?0:1,loaded.Player.GetPart<InventoryPart>().Objects.Count(e=>e.ID==id));
                }
            }
            finally{typeof(TurnManager).GetProperty("Active").SetValue(null,old);}
        }
        [TestCase("remove")] [TestCase("rename")] [TestCase("part")]
        public void CallbackCannotInvalidateStagedLooseOwner(string mode)
        {
            var pair=WithLoose();int seed=pair.manager.WorldSeed;
            var m=new DensityLairStackReviewTests.CallbackManager(scope.Factory,seed);Configure(m,BiomeType.Sodden);bool exercised=false;
            m.After=z=>{var item=z.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(Marker)==Surface);if(item==null)return;exercised=true;if(mode=="remove")z.RemoveEntity(item);else if(mode=="rename")item.ID="foreign-after-stage";else item.GetPart<PhysicsPart>().Takeable=false;};
            Assert.Null(m.GetZone(Bottom));Assert.True(exercised);Assert.Null(LairStacks.Inspect(m,Surface));Assert.Zero(m.CachedZoneCount);
        }
    }
}
