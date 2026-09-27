using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairStackTests
    {
        DensityLootTestScope scope; OverworldZoneManager manager; const string Surface = "Overworld.4.9.0";
        [SetUp]
        public void Setup()
        {
            scope = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            SetLair(manager, 2);
        }
        [TearDown]
        public void Cleanup()
        {
            scope.Dispose();
        }
        void SetLair(OverworldZoneManager m, int tier)
        {
            m.WorldMap.Tiles[4, 9] = BiomeType.Spread;
            m.WorldMap.SetPOI(4, 9, new PointOfInterest(POIType.Lair, "ownership probe", null, tier, "MarlbackWallkeeper"));
        }
        Entity Boss(Zone z) => z.GetReadOnlyEntities().Single(e => e.BlueprintName == "MarlbackWallkeeper");
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(5, 2)]
        public void TierChoosesOneOrTwoBelowAndCommitsOnlyGeneratedFloor(int tier, int final)
        {
            SetLair(manager, tier);
            Assert.Null(LairStacks.Inspect(manager, Surface));
            var surface = manager.GetZone(Surface);
            Assert.NotNull(surface);
            var plan = LairStacks.Inspect(manager, Surface);
            Assert.NotNull(plan);
            Assert.AreEqual(final, plan.FinalDepth);
            Assert.AreEqual(1, plan.GeneratedMask);
            Assert.False(plan.Legacy);
            Assert.False(surface.GetReadOnlyEntities().Any(e => e.HasTag("Boss")));
        }
        [Test]
        public void LowerFirstGeneratesOnlyRequestedFloorAndOneBossAndReward()
        {
            SetLair(manager, 3);
            var bottom = manager.GetZone("Overworld.4.9.2");
            Assert.AreEqual(1, manager.CachedZoneCount);
            var plan = LairStacks.Inspect(manager, Surface);
            Assert.AreEqual(4, plan.GeneratedMask);
            Assert.AreEqual(Boss(bottom).ID, plan.BossID);
            Assert.AreEqual(1, bottom.GetReadOnlyEntities().Count(e => e.HasPart<ContainerPart>() && !e.HasPart<AIAmbushPart>()));
            Assert.False(bottom.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
            var middle = manager.GetZone("Overworld.4.9.1");
            var surface = manager.GetZone(Surface);
            Assert.AreEqual(3, manager.CachedZoneCount);
            Assert.AreEqual(7, LairStacks.Inspect(manager, Surface).GeneratedMask);
            Assert.AreSame(bottom, manager.GetZone(bottom.ZoneID));
            Assert.False(middle.GetReadOnlyEntities().Any(e => e.HasTag("Boss")));
            Assert.True(surface.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
        }
        [Test]
        public void UnloadKeepsActualInjuredBossAndDepletedReward()
        {
            var bottom = manager.GetZone("Overworld.4.9.1");
            var boss = Boss(bottom);
            boss.GetStat("Hitpoints").BaseValue = 7;
            var reward = bottom.GetReadOnlyEntities().Single(e => e.ID == LairStacks.Inspect(manager, Surface).RewardID);
            reward.GetPart<ContainerPart>().Contents.Clear();
            manager.UnloadZone(bottom.ZoneID);
            Assert.AreSame(bottom, manager.GetZone(bottom.ZoneID));
            Assert.AreSame(boss, Boss(bottom));
            Assert.AreEqual(7, boss.GetStat("Hitpoints").BaseValue);
            Assert.Zero(reward.GetPart<ContainerPart>().Contents.Count);
        }
        [Test]
        public void RemovedStairsAndRewardStayRemoved()
        {
            var bottom = manager.GetZone("Overworld.4.9.1");
            var plan = LairStacks.Inspect(manager, Surface);
            foreach (var e in bottom.GetReadOnlyEntities().Where(e => e.HasPart<StairsUpPart>() || e.ID == plan.RewardID).ToArray())
                bottom.RemoveEntity(e);
            manager.UnloadZone(bottom.ZoneID);
            Assert.AreSame(bottom, manager.GetZone(bottom.ZoneID));
            Assert.False(bottom.GetReadOnlyEntities().Any(e => e.HasPart<StairsUpPart>() || e.ID == plan.RewardID));
            var surface = manager.GetZone(Surface);
            Assert.False(surface.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
        }
        [Test]
        public void CurrentPlanKeepsRecordedBossAndDepthAfterPoiEdits()
        {
            manager.GetZone(Surface);
            manager.WorldMap.GetPOI(4, 9).Tier = 5;
            manager.WorldMap.GetPOI(4, 9).BossBlueprint = "StoneGolem";
            var bottom = manager.GetZone("Overworld.4.9.1");
            Assert.AreEqual(1, LairStacks.Inspect(manager, Surface).FinalDepth);
            Assert.NotNull(Boss(bottom));
        }
        [Test]
        public void CachedLegacySurfaceIsAdoptedWithoutMovingItsBoss()
        {
            var z = new Zone(Surface);
            var boss = scope.Factory.CreateEntity("MarlbackWallkeeper");
            z.AddEntity(boss, 40, 12);
            manager.SetActiveZone(z);
            Assert.AreSame(z, manager.GetZone(Surface));
            Assert.True(LairStacks.Inspect(manager, Surface).Legacy);
            Assert.AreSame(boss, Boss(z));
            var lower = manager.GetZone("Overworld.4.9.1");
            Assert.False(lower.GetReadOnlyEntities().Any(e => e.HasTag("Boss")));
        }
        [Test]
        public void MissingBossCannotCommitAPlanOrEdges()
        {
            manager.WorldMap.GetPOI(4, 9).BossBlueprint = "missing-boss";
            Assert.Null(manager.GetZone("Overworld.4.9.1"));
            Assert.Null(LairStacks.Inspect(manager, Surface));
            Assert.Zero(manager.GetConnections(Surface).Count);
            Assert.Zero(manager.CachedZoneCount);
        }
        [Test]
        public void UnrelatedZoneStillUnloads()
        {
            var z = new Zone("Overworld.5.9.1");
            manager.SetActiveZone(z);
            manager.UnloadZone(z.ZoneID);
            Assert.False(manager.CachedZones.ContainsKey(z.ZoneID));
            Assert.Null(manager.ActiveZone);
        }
        [Test]
        public void EmptyLedgerIsSavedAndForeignWorldLedgerRefuses()
        {
            var world = LairStacks.BindForSave(manager, null);
            Assert.NotNull(world.GetPart<LairStackLedgerPart>());
            Assert.AreEqual(0, world.GetPart<LairStackLedgerPart>().Count);
            var other = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            Assert.Throws<InvalidOperationException>(() => LairStacks.BindForSave(other, world));
        }
        [Test]
        public void LegacyAbsentLedgerAdoptsSavedMapBeforeFirstLairVisit()
        {
            LairStacks.Restore(manager, null);
            Assert.True(LairStacks.Inspect(manager, Surface).Legacy);
            var surface = manager.GetZone(Surface);
            Assert.NotNull(Boss(surface));
            Assert.False(surface.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
        }
        [Test]
        public void SameSeedManagersNeverShareClaimsOrOwners()
        {
            var a = manager.GetZone("Overworld.4.9.1");
            var other = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            SetLair(other, 2);
            var b = other.GetZone("Overworld.4.9.1");
            Assert.AreNotSame(Boss(a), Boss(b));
            Assert.AreNotEqual(Boss(a).ID, Boss(b).ID);
            Assert.AreNotSame(LairStacks.Inspect(manager, Surface), LairStacks.Inspect(other, Surface));
        }
    }
}
