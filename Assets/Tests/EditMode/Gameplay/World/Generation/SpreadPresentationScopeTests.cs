using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadPresentationScopeTests
    {
        private DensityLootTestScope scope;
        private OverworldZoneManager manager;
        private const string Surface = "Overworld.4.9.0", Bottom = "Overworld.4.9.1";

        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            manager.WorldMap.Tiles[4,9] = BiomeType.Spread;
            manager.WorldMap.SetPOI(4,9,null);
        }
        [TearDown] public void Cleanup() { scope.Dispose(); }
        private Zone Attach(string id)
        {
            var zone = new Zone(id);
            manager.SetActiveZone(zone);
            return zone;
        }
        private void Lair(int tier = 2)
        {
            manager.WorldMap.SetPOI(4,9,new PointOfInterest(POIType.Lair,"scope probe",null,tier,"MarlbackWallkeeper"));
        }
        private static bool HasLedger(OverworldZoneManager owner)
        {
            var table = typeof(LairStacks).GetField("Ledgers", BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            return (bool)table.GetType().GetMethod("TryGetValue").Invoke(table,new object[] { owner,null });
        }
        [Test] public void OrdinaryCurrentSpreadSurfaceIsActiveWithoutGeometryChanges()
        {
            var zone = Attach(Surface);var owner = scope.Factory.CreateEntity("Torch");zone.AddEntity(owner,17,4);
            Assert.True(SpreadPresentationScope.IsActive(zone));
            Assert.AreSame(owner,zone.GetCell(17,4).Objects.Single());Assert.AreEqual(1,zone.EntityCount);
        }
        [TestCase(POIType.Village)][TestCase(POIType.MerchantCamp)][TestCase(POIType.Lair)]
        [TestCase(POIType.Sinkhole)][TestCase(POIType.RiverChunk)]
        public void CurrentSpreadSurfacePoiDoesNotLoseBiomeStyle(POIType type)
        {
            var zone = Attach(Surface);manager.WorldMap.SetPOI(4,9,new PointOfInterest(type,"actual POI",null,1));
            Assert.True(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void CurrentMapMutationAndReplacementAreObservedWithoutReattach()
        {
            var zone = Attach(Surface);Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.Tiles[4,9]=BiomeType.Sodden;Assert.False(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.Tiles[4,9]=BiomeType.Spread;Assert.True(SpreadPresentationScope.IsActive(zone));
            var foreign = OverworldZoneManager.CreateDetached(scope.Factory,64).WorldMap;
            foreign.Tiles[4,9]=BiomeType.Beating;
            manager.ReplaceLoadedOverworldState(foreign,null,null);Assert.False(SpreadPresentationScope.IsActive(zone));
            foreign.Tiles[4,9]=BiomeType.Spread;Assert.True(SpreadPresentationScope.IsActive(zone));
        }
        [TestCase(null)][TestCase("")][TestCase("WorldMap")][TestCase("Overworld.04.9.0")]
        [TestCase("Overworld.4.9.0.extra")][TestCase("Overworld.-1.9.0")][TestCase("Overworld.20.9.0")]
        [TestCase("Other.4.9.0")][TestCase("Overworld.4.9.-1")][TestCase("Overworld.x.9.0")]
        public void DetachedOrMalformedGraphsCannotForgeBiomeAuthority(string id)
        {
            Assert.False(SpreadPresentationScope.IsActive(id==null?null:new Zone(id)));
            if(id!=null)Assert.False(SpreadPresentationScope.IsActive(Attach(id)));
        }
        [Test] public void EvenStandaloneExactGladeUsesOnlyItsSeparateLegacyGeometryContract()
        {
            var zone = new Zone(ReferenceGladePlan.ZoneID);
            Assert.True(ReferenceGladePlan.IsActive(zone));Assert.False(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void CachedOwnershipAndZoneIdMutationsCannotBorrowAuthority()
        {
            var zone=Attach(Surface);Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.CachedZones[Surface]=new Zone(Surface);Assert.False(SpreadPresentationScope.IsActive(zone));
            manager.CachedZones[Surface]=zone;Assert.True(SpreadPresentationScope.IsActive(zone));
            zone.ZoneID="Overworld.5.9.0";Assert.False(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void UnattachedCacheInsertionCannotClaimManagerContext()
        {
            var zone=new Zone(Surface);manager.CachedZones[Surface]=zone;Assert.False(SpreadPresentationScope.IsActive(zone));
            manager.GetZone(Surface);Assert.True(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void SurfaceQueriesNeverCreateLairLedgerOrGenerateAnotherZone()
        {
            Lair();var zone=Attach(Surface);Assert.False(HasLedger(manager));
            for(int i=0;i<5;i++)Assert.True(SpreadPresentationScope.IsActive(zone));
            Assert.False(HasLedger(manager));Assert.AreEqual(1,manager.CachedZoneCount);Assert.Zero(manager.GetConnectionSnapshot().Count);
        }
        [TestCase(1)][TestCase(3)]
        public void OrdinaryUndergroundIsNeverInferredFromSurfaceBiome(int depth)
        {
            var zone=Attach("Overworld.4.9."+depth);Assert.False(SpreadPresentationScope.IsActive(zone));Assert.False(HasLedger(manager));
        }
        [TestCase(1)][TestCase(2)]
        public void ActualLowerFirstLairCommitEstablishesFiniteFloorAuthority(int depth)
        {
            Lair(3);var zone=manager.GetZone("Overworld.4.9."+depth);Assert.NotNull(zone);
            Assert.True(SpreadPresentationScope.IsActive(zone));Assert.AreEqual(1,manager.CachedZoneCount);
            Assert.False(SpreadPresentationScope.IsActive(Attach("Overworld.4.9.3")));
        }
        [Test] public void SameIdReplacementCannotBorrowCommittedLairClaim()
        {
            Lair();var original=manager.GetZone(Bottom);Assert.True(SpreadPresentationScope.IsActive(original));
            var impostor=Attach(Bottom);Assert.False(SpreadPresentationScope.IsActive(impostor));Assert.False(SpreadPresentationScope.IsActive(original));
            manager.SetActiveZone(original);Assert.True(SpreadPresentationScope.IsActive(original));
        }
        [Test] public void CurrentLairMapAndPoiAuthorityIsRecheckedButPlanTierDoesNotRebuild()
        {
            Lair();var zone=manager.GetZone(Bottom);Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.GetPOI(4,9).Tier=5;Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.SetPOI(4,9,null);Assert.False(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.SetPOI(4,9,new PointOfInterest(POIType.Village,"changed",null,1));Assert.False(SpreadPresentationScope.IsActive(zone));
            Lair();Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.Tiles[4,9]=BiomeType.Grovelands;Assert.False(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void LegacySurfaceStaysStyledButItsOrdinaryLowerFloorDoesNotGainNewStackIdentity()
        {
            Lair();var surface=Attach(Surface);manager.GetZone(Surface);Assert.True(LairStacks.Inspect(manager,Surface).Legacy);
            Assert.True(SpreadPresentationScope.IsActive(surface));var lower=manager.GetZone(Bottom);Assert.False(SpreadPresentationScope.IsActive(lower));
        }
        [Test] public void RemovedStairsBossAndRewardDoNotRegenerateOrRevokeCommittedStyle()
        {
            Lair();var zone=manager.GetZone(Bottom);var record=LairStacks.Inspect(manager,Surface);
            foreach(var owner in zone.GetReadOnlyEntities().Where(e=>e.HasPart<StairsUpPart>()||e.ID==record.BossID||e.ID==record.RewardID).ToArray())zone.RemoveEntity(owner);
            int count=zone.EntityCount;Assert.True(SpreadPresentationScope.IsActive(zone));Assert.AreEqual(count,zone.EntityCount);
            Assert.AreEqual(record.GeneratedMask,LairStacks.Inspect(manager,Surface).GeneratedMask);
        }
        [TestCase("null_tiles")][TestCase("small_tiles")][TestCase("null_pois")][TestCase("small_pois")]
        public void CorruptedMapArraysRefuseWithoutThrowingOrRepairing(string fault)
        {
            var zone=Attach(Surface);Assert.True(SpreadPresentationScope.IsActive(zone));
            if(fault=="null_tiles")manager.WorldMap.Tiles=null;
            if(fault=="small_tiles")manager.WorldMap.Tiles=new BiomeType[1,1];
            if(fault=="null_pois")manager.WorldMap.POIs=null;
            if(fault=="small_pois")manager.WorldMap.POIs=new PointOfInterest[1,1];
            Assert.False(SpreadPresentationScope.IsActive(zone));Assert.AreEqual(1,manager.CachedZoneCount);
        }
        [Test] public void SameZoneAttachedToAnotherWorldUsesThatWorldInsteadOfGlobalOrPriorMap()
        {
            var zone=Attach(Surface);Assert.True(SpreadPresentationScope.IsActive(zone));
            var other=OverworldZoneManager.CreateDetached(scope.Factory,1729);
            other.WorldMap.Tiles[4,9]=BiomeType.Sodden;other.SetActiveZone(zone);
            Assert.False(SpreadPresentationScope.IsActive(zone));
            other.WorldMap.Tiles[4,9]=BiomeType.Spread;Assert.True(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.Tiles[4,9]=BiomeType.Sodden;Assert.True(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void ACommittedOtherBiomeLairCannotBeRelabelledAsAnAuthoredSpreadStack()
        {
            Lair();manager.WorldMap.Tiles[4,9]=BiomeType.Sodden;var zone=manager.GetZone(Bottom);
            Assert.False(SpreadPresentationScope.IsActive(zone));
            manager.WorldMap.Tiles[4,9]=BiomeType.Spread;Assert.False(SpreadPresentationScope.IsActive(zone));
        }
        [Test] public void AllCurrentAuthoredSpreadAddressesAreCoveredWithoutStaticWhitelist()
        {
            int count=0;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                var zone=Attach(WorldMap.ToZoneID(x,y,0));
                bool expected=manager.WorldMap.GetBiome(x,y)==BiomeType.Spread;
                Assert.AreEqual(expected,SpreadPresentationScope.IsActive(zone),zone.ZoneID);
                if(expected)count++;
            }
            Assert.AreEqual(142,count);
        }
        private GameSessionState RoundTrip(Zone zone)
        {
            var player=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,36,12));manager.SetActiveZone(zone);
            var old=TurnManager.Active;
            try
            {
                var turns=new TurnManager();turns.RestoreSavedState(71,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
                var state=GameSessionState.Capture("spread-authority","v",manager,turns,player);
                using(var memory=new MemoryStream()){state.Save(new SaveWriter(memory));memory.Position=0;return GameSessionState.Load(new SaveReader(memory,scope.Factory));}
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null,old); }
        }
        [TestCase(false)][TestCase(true)]
        public void RealSaveReconstructsOnlyItsOwnLoadedSurfaceOrLairGraph(bool lair)
        {
            if(lair)Lair();var zone=lair?manager.GetZone(Bottom):Attach(Surface);
            var loaded=RoundTrip(zone);var copy=loaded.ZoneManager.CachedZones[zone.ZoneID];
            Assert.AreNotSame(zone,copy);Assert.True(SpreadPresentationScope.IsActive(copy));Assert.True(SpreadPresentationScope.IsActive(zone));
            var impostor=new Zone(copy.ZoneID);loaded.ZoneManager.SetActiveZone(impostor);
            Assert.False(SpreadPresentationScope.IsActive(copy));Assert.AreEqual(!lair,SpreadPresentationScope.IsActive(impostor));
        }
        [Test] public void IncompleteLoadedLairGraphRefusesRestoreAndCannotBorrowOriginalClaims()
        {
            Lair();var zone=manager.GetZone(Bottom);var world=LairStacks.BindForSave(manager,null);
            Entity clone;
            using(var memory=new MemoryStream())
            {
                var writer=new SaveWriter(memory);writer.WriteEntityReference(world);writer.WriteQueuedEntityBodies();memory.Position=0;
                var reader=new SaveReader(memory,scope.Factory);clone=reader.ReadEntityReference();reader.ReadEntityBodies();
            }
            var other=OverworldZoneManager.CreateDetached(scope.Factory,64);
            Assert.Throws<InvalidDataException>(()=>LairStacks.Restore(other,clone));
            var impostor=new Zone(Bottom);other.SetActiveZone(impostor);Assert.False(SpreadPresentationScope.IsActive(impostor));
            Assert.True(SpreadPresentationScope.IsActive(zone));
        }
    }
}
