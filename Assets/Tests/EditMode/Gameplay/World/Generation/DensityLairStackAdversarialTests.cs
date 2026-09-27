using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairStackAdversarialTests
    {
        DensityLootTestScope scope; OverworldZoneManager manager; const string Surface = "Overworld.4.9.0", Bottom = "Overworld.4.9.1";
        [SetUp]
        public void Setup()
        {
            scope = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            SetLair(manager, BiomeType.Spread, 2);
        }
        [TearDown]
        public void Cleanup()
        {
            scope.Dispose();
        }
        void SetLair(OverworldZoneManager m, BiomeType b, int tier)
        {
            m.WorldMap.Tiles[4, 9] = b;
            m.WorldMap.SetPOI(4, 9, new PointOfInterest(POIType.Lair, "persistence probe", null, tier, b == BiomeType.Beating ? "DesertProwler" : b == BiomeType.Grovelands ? "JungleStalker" : "MarlbackWallkeeper"));
        }
        Entity Boss(Zone z) => z.GetReadOnlyEntities().Single(e => e.ID == LairStacks.Inspect(manager, Surface).BossID);
        Entity Player(Zone z)
        {
            var p = scope.Factory.CreateEntity("Player");
            Assert.True(z.AddEntity(p, 36, 12));
            manager.SetActiveZone(z);
            return p;
        }
        GameSessionState RoundTrip(Entity player)
        {
            var old = TurnManager.Active;
            try
            {
                var turns = new TurnManager();
                turns.RestoreSavedState(71, true, player, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = player, Energy = 1000 } });
                var state = GameSessionState.Capture("lair-stack-test", "v", manager, turns, player);
                using (var s = new MemoryStream())
                {
                    state.Save(new SaveWriter(s));
                    s.Position = 0;
                    return GameSessionState.Load(new SaveReader(s, scope.Factory));
                }
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, old); }
        }
        [Test]
        public void FullSaveRetainsWoundedBossEquipmentAndCooldown()
        {
            var z = manager.GetZone(Bottom);
            var b = Boss(z);
            b.GetStat("Hitpoints").BaseValue = 7;
            var ability = b.GetPart<ActivatedAbilitiesPart>().AbilityList.First();
            ability.CooldownRemaining = 11;
            ability.MaxCooldown = 17;
            var gear = b.GetPart<Body>().GetEquippedParts(new List<BodyPart>()).Select(p => p.Equipped.ID).Distinct().OrderBy(x => x).ToArray();
            Assert.Greater(gear.Length, 0);
            string id = b.ID;
            var player = Player(z);
            manager.UnloadZone(Bottom);
            var loaded = RoundTrip(player);
            var saved = loaded.ZoneManager.GetZone(Bottom).GetReadOnlyEntities().Single(e => e.ID == id);
            Assert.AreEqual(7, saved.GetStat("Hitpoints").BaseValue);
            Assert.AreEqual(11, saved.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(a => a.ID == ability.ID).CooldownRemaining);
            CollectionAssert.AreEqual(gear, saved.GetPart<Body>().GetEquippedParts(new List<BodyPart>()).Select(p => p.Equipped.ID).Distinct().OrderBy(x => x).ToArray());
        }
        [Test]
        public void CommittedCombatDeathAndCarriedRewardSurviveUnloadAndSave()
        {
            var z = manager.GetZone(Bottom);
            var boss = Boss(z);
            var plan = LairStacks.Inspect(manager, Surface);
            var reward = z.GetReadOnlyEntities().Single(e => e.ID == plan.RewardID);
            var cp = reward.GetPart<ContainerPart>();
            Assert.Greater(cp.Contents.Count, 0);
            var loot = cp.Contents[0];
            cp.Contents.Remove(loot);
            var player = Player(z);
            Assert.True(player.GetPart<InventoryPart>().AddObject(loot));
            string itemId = loot.ID;
            CombatSystem.HandleDeath(boss, null, z);
            Assert.True(CombatSystem.IsDeathHandled(boss));
            Assert.Null(z.GetEntityCell(boss));
            cp.Contents.Clear();
            manager.UnloadZone(Bottom);
            var loaded = RoundTrip(player);
            var saved = loaded.ZoneManager.GetZone(Bottom);
            Assert.False(saved.GetReadOnlyEntities().Any(e => e.ID == boss.ID || e.HasTag("Boss")));
            Assert.AreEqual(boss.ID, LairStacks.Inspect(loaded.ZoneManager, Surface).BossID);
            Assert.Zero(saved.GetReadOnlyEntities().Single(e => e.ID == plan.RewardID).GetPart<ContainerPart>().Contents.Count);
            Assert.AreEqual(1, loaded.Player.GetPart<InventoryPart>().Objects.Count(e => e.ID == itemId));
            loaded.ZoneManager.UnloadZone(Bottom);
            Assert.AreSame(saved, loaded.ZoneManager.GetZone(Bottom));
        }
        [Test]
        public void SaveBeforeAnyLairVisitRemainsEligibleForFreshStacks()
        {
            var z = new Zone("Overworld.5.9.0");
            manager.SetActiveZone(z);
            var loaded = RoundTrip(Player(z));
            Assert.Null(LairStacks.Inspect(loaded.ZoneManager, Surface));
            var surface = loaded.ZoneManager.GetZone(Surface);
            Assert.False(LairStacks.Inspect(loaded.ZoneManager, Surface).Legacy);
            Assert.True(surface.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
        }
        [Test]
        public void SavedRemovedStairsAreNotRecreatedInNewParent()
        {
            var z = manager.GetZone(Bottom);
            foreach (var e in z.GetReadOnlyEntities().Where(e => e.HasPart<StairsUpPart>()).ToArray())
                z.RemoveEntity(e);
            var loaded = RoundTrip(Player(z));
            var surface = loaded.ZoneManager.GetZone(Surface);
            Assert.False(surface.GetReadOnlyEntities().Any(e => e.HasPart<StairsDownPart>()));
            Assert.False(loaded.ZoneManager.GetZone(Bottom).GetReadOnlyEntities().Any(e => e.HasPart<StairsUpPart>()));
        }
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(1, -1)]
        [TestCase(1, 401)]
        public void MalformedHeadersRejectWithoutDiscardingPriorClaims(int version, int count)
        {
            manager.GetZone(Bottom);
            var l = LairStacks.BindForSave(manager, null).GetPart<LairStackLedgerPart>();
            int before = l.Count;
            using (var s = new MemoryStream())
            {
                var w = new SaveWriter(s);
                w.Write(version);
                w.Write(count);
                s.Position = 0;
                Assert.Throws<InvalidDataException>(() => l.Load(new SaveReader(s, scope.Factory)));
            }
            Assert.AreEqual(before, l.Count);
        }
        [Test]
        public void TruncatedRecordRejectsAtomically()
        {
            manager.GetZone(Bottom);
            var l = LairStacks.BindForSave(manager, null).GetPart<LairStackLedgerPart>();
            using (var s = new MemoryStream())
            {
                l.Save(new SaveWriter(s));
                s.SetLength(s.Length - 1);
                s.Position = 0;
                Assert.Throws<EndOfStreamException>(() => l.Load(new SaveReader(s, scope.Factory)));
            }
            Assert.AreEqual(1, l.Count);
        }
        [TestCase("address")]
        [TestCase("depth")]
        [TestCase("mask")]
        [TestCase("boss")]
        [TestCase("duplicate")]
        public void MalformedRecordsRejectAtomically(string mode)
        {
            var l = new LairStackLedgerPart();
            using (var s = new MemoryStream())
            {
                var w = new SaveWriter(s);
                w.Write(1);
                w.Write(mode == "duplicate" ? 2 : 1);
                WriteRecord(w, mode);
                if (mode == "duplicate")
                    WriteRecord(w, mode);
                s.Position = 0;
                Assert.Throws<InvalidDataException>(() => l.Load(new SaveReader(s, scope.Factory)));
            }
            Assert.Zero(l.Count);
        }
        void WriteRecord(SaveWriter w, string mode)
        {
            w.WriteString(mode == "address" ? "Overworld.99.9.0" : Surface);
            w.Write(false);
            w.Write((int)BiomeType.Spread);
            w.Write(2);
            w.Write(mode == "depth" ? 3 : 1);
            w.WriteString(mode == "boss" ? null : "MarlbackWallkeeper");
            w.Write(mode == "mask" ? 8 : 0);
            w.WriteString(null);
            w.WriteString(null);
            for (int i = 0; i < 3; i++)
            {
                w.WriteString(null);
                w.WriteString(null);
            }
        }
        [Test]
        public void ClaimedFloorMissingFromSavedGraphRefusesRestore()
        {
            manager.GetZone(Bottom);
            var world = LairStacks.BindForSave(manager, null);
            manager.CachedZones.Clear();
            Assert.Throws<InvalidDataException>(() => LairStacks.Restore(manager, world));
        }
        [Test]
        public void ForeignManagerCannotRestoreOwnership()
        {
            manager.GetZone(Bottom);
            var world = LairStacks.BindForSave(manager, null);
            var other = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            Assert.Throws<InvalidOperationException>(() => LairStacks.Restore(other, world));
        }
        [Test]
        public void FailedRequiredContentThenRetryDoesNotBurnClaimOrEdge()
        {
            var bp = scope.Factory.Blueprints["MarlbackWallkeeper"];
            scope.Factory.Blueprints.Remove("MarlbackWallkeeper");
            Assert.Null(manager.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(manager, Surface));
            Assert.Zero(manager.GetConnections(Surface).Count);
            scope.Factory.Blueprints.Add("MarlbackWallkeeper", bp);
            var z = manager.GetZone(Bottom);
            Assert.NotNull(Boss(z));
            Assert.AreEqual(1, manager.GetConnectionsTo(Bottom, "StairsDown").Count);
        }
        [Test]
        public void FailureInFinalGenerationCallbackCommitsNothing()
        {
            var fail = new CallbackFailureManager(scope.Factory, 64);
            SetLair(fail, BiomeType.Spread, 2);
            Assert.Throws<InvalidOperationException>(() => fail.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(fail, Surface));
            Assert.Zero(fail.CachedZoneCount);
            Assert.Zero(fail.GetConnections(Surface).Count);
        }
        public sealed class CallbackFailureManager : OverworldZoneManager
        {
            public CallbackFailureManager(EntityFactory f, int s) : base(f, s) { }
            protected override void OnZoneGenerated(Zone z, string id)
            {
                base.OnZoneGenerated(z, id);
                throw new InvalidOperationException("after builder probe");
            }
        }
        [TestCase(BiomeType.Spread)]
        [TestCase(BiomeType.Sodden)]
        [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Grovelands)]
        public void WholeStackBudgetAndBiomeCacheRemainBounded(BiomeType biome)
        {
            SetLair(manager, biome, 3);
            var zs = Enumerable.Range(0, 3).Select(i => manager.GetZone(WorldMap.ToZoneID(4, 9, i))).ToArray();
            Assert.True(zs.All(z => z != null));
            Assert.AreEqual(1, zs.Sum(z => z.GetReadOnlyEntities().Count(e => e.HasTag("Boss"))));
            Assert.AreEqual(3, zs.Sum(z => z.GetReadOnlyEntities().Count(e => e.HasPart<ContainerPart>() && !e.HasPart<AIAmbushPart>())));
            Assert.LessOrEqual(zs.Sum(z => z.GetEntitiesWithTag("Creature").Count), 10);
            var cache = zs[2].GetReadOnlyEntities().Single(e => e.HasPart<ContainerPart>() && !e.HasPart<AIAmbushPart>());
            CollectionAssert.Contains(ContainerPlacementService.PoolFor(biome, ContainerPlacementService.ZoneKind.Lair).Select(p => p.Blueprint), cache.BlueprintName);
            foreach (var z in zs)
                foreach (var e in z.GetEntitiesWithTag("Creature"))
                    foreach (var c in z.GetOccupiedCells(e))
                        Assert.False(z.GenReservedCells.Contains((c.X, c.Y)));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void RetentionDiagnosticRespectsChannel(bool enabled)
        {
            var z = manager.GetZone(Bottom);
            bool old = Diag.IsChannelEnabled("worldgen");
            try
            {
                Diag.SetChannel("worldgen", enabled);
                int before = DiagQuery.Apply(new DiagQuery.Filter { Kind = "LairZoneRetained", Limit = 10000 }).Records.Count;
                manager.UnloadZone(Bottom);
                int after = DiagQuery.Apply(new DiagQuery.Filter { Kind = "LairZoneRetained", Limit = 10000 }).Records.Count;
                Assert.AreEqual(enabled ? 1 : 0, after - before);
                Assert.AreSame(z, manager.GetZone(Bottom));
            }
            finally { Diag.SetChannel("worldgen", old); }
        }
        [Test]
        public void LowerFirstStairsSupportActualRoundTripWithoutRegeneration()
        {
            var lower = manager.GetZone(Bottom);
            var player = Player(lower);
            var up = ZoneTransitionSystem.TransitionPlayerVertical(player, lower, false, 36, 12, manager);
            Assert.True(up.Success, up.ErrorReason);
            var down = ZoneTransitionSystem.TransitionPlayerVertical(player, up.NewZone, true, 44, 12, manager);
            Assert.True(down.Success, down.ErrorReason);
            Assert.AreSame(lower, down.NewZone);
            Assert.AreEqual(2, manager.CachedZoneCount);
        }
        [Test]
        public void RemovedCounterpartDoesNotLeaveAStaleCommittedConnection()
        {
            var lower = manager.GetZone(Bottom);
            foreach (var e in lower.GetReadOnlyEntities().Where(e => e.HasPart<StairsUpPart>()).ToArray())
                lower.RemoveEntity(e);
            manager.GetZone(Surface);
            Assert.Zero(manager.GetConnectionsTo(Bottom, "StairsDown").Count);
        }
        [TestCase(BiomeType.Spread, "AmbushBandit")]
        [TestCase(BiomeType.Sodden, "SleepingTroll")]
        [TestCase(BiomeType.Beating, "AmbushBandit")]
        [TestCase(BiomeType.Grovelands, "CanopyStrangler")]
        public void ExistingAmbusherKindsRemainSourcedOnceOnFinalFloors(BiomeType biome, string wanted)
        {
            int found = 0, mimics = 0;
            for (int seed = 1; seed <= 12; seed++)
            {
                var m = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                SetLair(m, biome, 2);
                var z = m.GetZone(Bottom);
                found += z.GetReadOnlyEntities().Count(e => e.BlueprintName == wanted);
                mimics += z.GetReadOnlyEntities().Count(e => e.BlueprintName == "MimicChest");
                var surface = m.GetZone(Surface);
                Assert.False(surface.GetReadOnlyEntities().Any(e => e.BlueprintName == wanted || e.BlueprintName == "MimicChest"));
            }
            Assert.Greater(found, 0);
            Assert.Greater(mimics, 0);
        }
        [Test]
        public void FinalFloorRetainsExistingTrapPassAndProtectedStairs()
        {
            var final = manager.GetZone(Bottom);
            var traps = final.GetReadOnlyEntities().Where(e => e.HasPart<TriggerOnStepPart>()).ToArray();
            Assert.That(traps.Length, Is.InRange(1, 2));
            foreach (var trap in traps)
                foreach (var c in final.GetOccupiedCells(trap))
                    Assert.False(final.GenReservedCells.Contains((c.X, c.Y)));
            Assert.False(manager.GetZone(Surface).GetReadOnlyEntities().Any(e => e.HasPart<TriggerOnStepPart>()));
        }
        [TestCase("Grass")]
        [TestCase("VineWall")]
        public void MissingPaletteCannotPublishAnInvisibleLair(string missing)
        {
            scope.Factory.Blueprints.Remove(missing);
            Assert.Null(manager.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(manager, Surface));
            Assert.Zero(manager.CachedZoneCount);
        }
        [TestCase("same-boss-and-reward")]
        [TestCase("unbuilt-endpoint")]
        [TestCase("surface-up")]
        [TestCase("final-down")]
        public void ContradictorySavedOwnershipRejects(string mode)
        {
            var l = new LairStackLedgerPart();
            using (var s = new MemoryStream())
            {
                var w = new SaveWriter(s);
                w.Write(1);
                w.Write(1);
                w.WriteString(Surface);
                w.Write(false);
                w.Write((int)BiomeType.Spread);
                w.Write(2);
                w.Write(1);
                w.WriteString("MarlbackWallkeeper");
                w.Write(mode == "unbuilt-endpoint" ? 0 : 2);
                w.WriteString(mode == "unbuilt-endpoint" ? null : "boss");
                w.WriteString(mode == "unbuilt-endpoint" ? null : mode == "same-boss-and-reward" ? "boss" : "reward");
                for (int i = 0; i < 3; i++)
                {
                    w.WriteString(i == 0 && mode == "surface-up" ? "wrong-up" : i == 1 && mode == "unbuilt-endpoint" ? "premature-up" : null);
                    w.WriteString(i == 1 && mode == "final-down" ? "wrong-down" : null);
                }
                s.Position = 0;
                Assert.Throws<InvalidDataException>(() => l.Load(new SaveReader(s, scope.Factory)));
            }
            Assert.Zero(l.Count);
        }
        [Test]
        public void RemovedReturnEndpointCannotTeleportPlayer()
        {
            var lower = manager.GetZone(Bottom);
            var surface = manager.GetZone(Surface);
            foreach (var e in surface.GetReadOnlyEntities().Where(e => e.HasPart<StairsDownPart>()).ToArray())
                surface.RemoveEntity(e);
            var player = Player(lower);
            var result = ZoneTransitionSystem.TransitionPlayerVertical(player, lower, false, 36, 12, manager);
            Assert.False(result.Success);
            Assert.NotNull(lower.GetEntityCell(player));
            Assert.Null(surface.GetEntityCell(player));
        }
    }
}
