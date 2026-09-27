using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityLegendaryLairTests
    {
        DensityLootTestScope scope;
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); EnhancementFactory.ForceReinitialize(); }
        [TearDown] public void Cleanup() { EnhancementFactory.ForceReinitialize(); scope.Dispose(); }
        static Type Service => typeof(Entity).Assembly.GetType("CavesOfOoo.Core.LegendaryLairEncounters");
        static Part Identity(Entity e) => e.GetPart("LegendaryIdentity");
        static string Field(Part part, string name) => (string)part.GetType().GetField(name).GetValue(part);
        static IEnumerable<Entity> Gear(Entity boss) => boss.GetPart<InventoryPart>().EquippedItems.Values.Distinct();
        bool Apply(Zone zone, OverworldZoneManager manager, int chance)
        {
            Assert.NotNull(Service, "Missing legendary final-lair source service");
            var method = Service.GetMethod("TryApplyGeneratedFinal", BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(method);
            return (bool)method.Invoke(null, new object[] { zone, manager, chance });
        }
        (OverworldZoneManager manager, Zone zone, Entity boss) Stage(int seed = 64, BiomeType biome = BiomeType.Sodden, int tier = 2)
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed);
            manager.WorldMap.Tiles[4, 9] = biome;
            manager.WorldMap.SetPOI(4, 9, new PointOfInterest(POIType.Lair, "test source", null, tier, "MarlbackWallkeeper"));
            string id = WorldMap.ToZoneID(4, 9, tier < 3 ? 1 : 2);
            var pipeline = (ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone", Hidden).Invoke(manager, new object[] { id });
            var zone = new Zone(id);
            Assert.True(pipeline.Generate(zone, scope.Factory, new Random(seed)));
            return (manager, zone, zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "MarlbackWallkeeper"));
        }
        [Test] public void ActualKeeperAlreadyHasTheTwoOrdinaryCandidateRewards()
        {
            var boss = scope.Factory.CreateEntity("MarlbackWallkeeper");
            foreach (string id in new[] { "LongSword", "LeatherArmor" })
            {
                var item = Gear(boss).Single(e => e.BlueprintName == id);
                Assert.AreSame(boss, item.GetPart<PhysicsPart>().Equipped);
                Assert.Null(item.GetPart<PhysicsPart>().InInventory, "equipped items use the Equipped backlink only");
                Assert.Zero(ItemEnhancing.CountEnhancements(item));
            }
            Assert.AreEqual("OutlandRaiders", boss.GetTag("Faction"));
            Assert.AreEqual(40, boss.GetStat("Hitpoints").Max);
        }
        [TestCase(0, false)] [TestCase(100, true)]
        public void ChanceEndpointsKeepOrUpgradeExactlyOneActualEquippedEntity(int chance, bool selected)
        {
            var s = Stage(); var before = Gear(s.boss).Select(e => e.ID).OrderBy(x => x).ToArray();
            double value = Gear(s.boss).Sum(TradeSystem.GetItemValue);
            Assert.AreEqual(selected, Apply(s.zone, s.manager, chance));
            Assert.AreEqual(selected ? 1 : 0, Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
            Assert.AreEqual(before, Gear(s.boss).Select(e => e.ID).OrderBy(x => x).ToArray());
            Assert.AreEqual(value, Gear(s.boss).Sum(TradeSystem.GetItemValue));
            Assert.AreEqual(selected, Identity(s.boss) != null);
            Assert.AreEqual(40, s.boss.GetStat("Hitpoints").Max);
        }
        [TestCase(BiomeType.Spread, 1)] [TestCase(BiomeType.Grovelands, 2)]
        [TestCase(BiomeType.Beating, 2)]
        public void EarlyChoirAndWrongBiomeKeepOrdinaryKit(BiomeType biome, int tier)
        {
            var s = Stage(biome: biome, tier: tier);
            Assert.False(Apply(s.zone, s.manager, 100));
            Assert.Null(Identity(s.boss));
            Assert.Zero(Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
        }
        [Test] public void AppliedIdentityAndActualRewardAreStableAndReadable()
        {
            var s = Stage(); Assert.True(Apply(s.zone, s.manager, 100));
            var identity = Identity(s.boss); Assert.NotNull(identity);
            string display = s.boss.GetDisplayName(); string rewardID = Field(identity, "RewardID");
            Assert.That(display, Does.Contain("marlback wallkeeper"));
            Assert.That(display, Does.Contain(Field(identity, "PersonalName")));
            var item = Gear(s.boss).Single(e => e.ID == rewardID);
            Assert.That(s.boss.GetPart<ExaminablePart>().BuildExamineLine(), Does.Contain(item.GetDisplayName()));
            Assert.False(Apply(s.zone, s.manager, 100));
            Assert.AreEqual(display, s.boss.GetDisplayName());
            Assert.AreEqual(rewardID, Field(Identity(s.boss), "RewardID"));
            Assert.AreEqual(1, Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
        }
        [Test] public void BothTemplatesAreReachableWithoutChangingOrdinaryCreatureStats()
        {
            var templates = new HashSet<string>();
            for (int seed = 1; seed <= 16; seed++)
            {
                var s = Stage(seed); Assert.True(Apply(s.zone, s.manager, 100));
                templates.Add(Field(Identity(s.boss), "TemplateID"));
                Assert.AreEqual(22, s.boss.GetStatValue("Strength"));
                Assert.AreEqual("OutlandRaiders", s.boss.GetTag("Faction"));
            }
            Assert.AreEqual(2, templates.Count);
        }
        [Test] public void RealFiveSeedSourceCensusHasEligibleLairsAndNoInventedTierTwoSpread()
        {
            int eligible = 0, spread = 0;
            var rows = new List<string>();
            foreach (int seed in new[] { 1, 64, 1729, 2026, 729490642 })
            {
                var m = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
                {
                    var p = m.WorldMap.GetPOI(x, y); if (p?.Type != POIType.Lair) continue;
                    var b = m.WorldMap.GetBiome(x, y);
                    bool yes = (b == BiomeType.Spread || b == BiomeType.Sodden) && p.Tier >= 2 && p.Tier <= 3 && p.BossBlueprint == "MarlbackWallkeeper";
                    if (yes) eligible++; if (b == BiomeType.Spread) { spread++; Assert.AreEqual(1, p.Tier); }
                    rows.Add(seed + "," + WorldMap.ToZoneID(x, y, 0) + "," + b + "," + p.Tier + "," + p.BossBlueprint + "," + yes);
                }
            }
            Assert.Greater(eligible, 0); Assert.Greater(spread, 0);
            File.WriteAllLines(Path.Combine(Path.GetTempPath(), "coo-legendary-source-census.csv"), new[] { "seed,surface,biome,tier,boss,eligible" }.Concat(rows));
        }
        [Test] public void ActualUnforcedFinalSourceProducesExceptionalAndOrdinaryKeepers()
        {
            int marked = 0, ordinary = 0;
            var rows = new List<string> { "seed,zone,blueprint,template,name,reward" };
            foreach (int seed in Enumerable.Range(1, 32))
            {
                var m = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
                {
                    var p = m.WorldMap.GetPOI(x, y);
                    if (p?.Type != POIType.Lair || m.WorldMap.GetBiome(x, y) != BiomeType.Sodden || p.Tier < 2 || p.Tier > 3) continue;
                    var z = m.GetZone(WorldMap.ToZoneID(x, y, p.Tier < 3 ? 1 : 2)); Assert.NotNull(z);
                    var boss = z.GetReadOnlyEntities().Single(e => e.BlueprintName == p.BossBlueprint);
                    var identity = Identity(boss);
                    if (identity != null) marked++; else ordinary++;
                    rows.Add(seed + "," + z.ZoneID + "," + boss.BlueprintName + "," + (identity == null ? "ordinary" : Field(identity, "TemplateID"))
                        + "," + boss.GetDisplayName().Replace(",", ";") + "," + (identity == null ? "" : Field(identity, "RewardID")));
                }
            }
            File.WriteAllLines(Path.Combine(Path.GetTempPath(), "coo-legendary-actual-source.csv"), rows);
            Assert.Greater(ordinary, 0); Assert.Greater(marked, 0, "helper must be wired into ordinary final-floor generation");
        }

        [TestCase(-1)] [TestCase(101)]
        public void InvalidChanceRefusesWithoutNaming(int chance)
        { var s = Stage(); Assert.False(Apply(s.zone, s.manager, chance)); Assert.Null(Identity(s.boss)); }

        [TestCase("named")] [TestCase("dead-flag")] [TestCase("dead-hp")]
        [TestCase("player")] [TestCase("friendly")] [TestCase("temporary")]
        [TestCase("equipped-backlink")] [TestCase("inventory-backlink")]
        [TestCase("duplicate-reward-id")] [TestCase("missing-boss-id")]
        [TestCase("body-backlink")] [TestCase("renamed-item")] [TestCase("already-enhanced")]
        public void ChangedOwnerAndRewardRefuseWithoutPartialMutation(string change)
        {
            var s = Stage(); var gear = Gear(s.boss).ToArray();
            if (change == "missing-boss-id") s.boss.ID = "";
            else if (change == "named") s.boss.GetPart<RenderPart>().DisplayName = "An existing named keeper";
            else if (change == "dead-flag") s.boss.SetTag("_DeathHandled");
            else if (change == "dead-hp") s.boss.GetStat("Hitpoints").BaseValue = 0;
            else if (change == "player") s.boss.SetTag("Player");
            else if (change == "friendly") s.boss.SetTag("Faction", "Villagers");
            else if (change == "temporary") s.boss.SetTag("Temporary");
            else if (change == "body-backlink") foreach (var part in s.boss.GetPart<Body>().GetParts()) part._Equipped = null;
            else foreach (var item in gear)
            {
                if (change == "duplicate-reward-id") item.ID = "duplicated-reward-id";
                else if (change == "equipped-backlink") item.GetPart<PhysicsPart>().Equipped = new Entity();
                else if (change == "inventory-backlink") item.GetPart<PhysicsPart>().InInventory = s.boss;
                else if (change == "renamed-item") item.GetPart<RenderPart>().DisplayName = "custom " + item.GetDisplayName();
                else if (change == "already-enhanced")
                {
                    string enhancement = item.HasPart<MeleeWeaponPart>() ? nameof(EnhancementSerrated) : nameof(EnhancementLacquered);
                    Assert.True(ItemEnhancing.Apply(item, enhancement, 1, s.boss));
                }
            }
            string name = s.boss.GetDisplayName(); int count = gear.Sum(ItemEnhancing.CountEnhancements);
            Assert.False(Apply(s.zone, s.manager, 100)); Assert.Null(Identity(s.boss));
            Assert.AreEqual(name, s.boss.GetDisplayName()); Assert.AreEqual(count, gear.Sum(ItemEnhancing.CountEnhancements));
        }
        [TestCase(false)] [TestCase(true)]
        public void AReplacementBossCannotBorrowTheStagedOwnersLegendaryClaim(bool reuseID)
        {
            var s = Stage();
            var borrowed = scope.Factory.CreateEntity("MarlbackWallkeeper");
            if (reuseID) borrowed.ID = s.boss.ID;
            var at = s.zone.GetEntityCell(s.boss);
            s.zone.RemoveEntity(s.boss);
            Assert.True(s.zone.AddEntity(borrowed, at.X, at.Y));
            string oldName = s.boss.GetDisplayName(), borrowedName = borrowed.GetDisplayName();
            bool applied = Apply(s.zone, s.manager, 100);
            bool committed = (bool)typeof(OverworldZoneManager).GetMethod("CommitGeneratedZone", Hidden)
                .Invoke(s.manager, new object[] { s.zone, s.zone.ZoneID });
            Assert.False(committed, "C10 must still reject the replaced staged reference");
            Assert.False(applied, "the rejected generation must not enhance a borrowed replacement");
            Assert.AreEqual(oldName, s.boss.GetDisplayName());
            Assert.AreEqual(borrowedName, borrowed.GetDisplayName());
            Assert.Null(Identity(s.boss)); Assert.Null(Identity(borrowed));
            Assert.Zero(Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
            Assert.Zero(Gear(borrowed).Sum(ItemEnhancing.CountEnhancements));
        }
        [TestCase("reward")] [TestCase("up")]
        public void InvalidOtherStagedOwnerRefusesBeforeEnhancingTheActualBoss(string role)
        {
            var s = Stage();
            var invalid = s.zone.GetReadOnlyEntities().Single(e => role == "reward"
                ? e.HasPart<ContainerPart>() && !e.HasPart<AIAmbushPart>() : e.HasPart<StairsUpPart>());
            s.zone.RemoveEntity(invalid);
            Assert.False(Apply(s.zone, s.manager, 100));
            Assert.Null(Identity(s.boss));
            Assert.Zero(Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
        }
        [Test] public void ALookalikeUnstagedZoneDoesNotOwnTheFreshBoss()
        {
            var s = Stage(); var copiedZone = new Zone(s.zone.ZoneID);
            var at = s.zone.GetEntityCell(s.boss); s.zone.RemoveEntity(s.boss);
            Assert.True(copiedZone.AddEntity(s.boss, at.X, at.Y));
            Assert.False(Apply(copiedZone, s.manager, 100));
            Assert.Null(Identity(s.boss));
            Assert.Zero(Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
        }
        [Test] public void InvalidCachedCounterpartRefusesBeforeEnhancingTheActualBoss()
        {
            var s = Stage();
            var surface = s.manager.GetZone("Overworld.4.9.0"); Assert.NotNull(surface);
            var down = surface.GetReadOnlyEntities().Single(e => e.HasPart<StairsDownPart>());
            down.SetTag("Solid");
            Assert.False(Apply(s.zone, s.manager, 100));
            Assert.Null(Identity(s.boss));
            Assert.Zero(Gear(s.boss).Sum(ItemEnhancing.CountEnhancements));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualGenerationCallbackReplacementFailsWithoutMutatingEitherActor(bool reuseID)
        {
            var manager = new DensityLairStackReviewTests.CallbackManager(scope.Factory, 1);
            Entity staged = null, borrowed = null;
            manager.After = zone =>
            {
                staged = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "MarlbackWallkeeper");
                var at = zone.GetEntityCell(staged); zone.RemoveEntity(staged);
                borrowed = scope.Factory.CreateEntity("MarlbackWallkeeper");
                if (reuseID) borrowed.ID = staged.ID;
                Assert.True(zone.AddEntity(borrowed, at.X, at.Y));
            };
            Assert.Null(manager.GetZone("Overworld.18.2.1"));
            Assert.NotNull(staged); Assert.NotNull(borrowed);
            foreach (var actor in new[] { staged, borrowed })
            {
                Assert.Null(Identity(actor));
                Assert.AreEqual("marlback wallkeeper", actor.GetDisplayName());
                Assert.Zero(Gear(actor).Sum(ItemEnhancing.CountEnhancements));
            }
        }
        [Test] public void CachedAndLegacyOwnersNeverAcquireNewIdentity()
        {
            var s = Stage(); LairStacks.Restore(s.manager, null);
            Assert.False(Apply(s.zone, s.manager, 100)); Assert.Null(Identity(s.boss));
            var fresh = Stage(); fresh.manager.SetActiveZone(fresh.zone);
            Assert.False(Apply(fresh.zone, fresh.manager, 100)); Assert.Null(Identity(fresh.boss));
        }
        [Test] public void RemovedRewardDisappearsFromLiveKeeperDescription()
        {
            var s = Stage(); Assert.True(Apply(s.zone, s.manager, 100));
            var part = Identity(s.boss); var item = Gear(s.boss).Single(e => e.ID == Field(part, "RewardID"));
            string name = item.GetDisplayName(); Assert.True(InventorySystem.UnequipItem(s.boss, item));
            Assert.That(s.boss.GetPart<ExaminablePart>().BuildExamineLine(), Does.Contain(name));
            Assert.True(InventorySystem.Drop(s.boss, item, s.zone));
            Assert.That(s.boss.GetPart<ExaminablePart>().BuildExamineLine(), Does.Not.Contain(name));
            Assert.NotNull(s.zone.GetEntityCell(item)); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(item));
        }
        [Test] public void SeededNamesAndEpithetsDoNotCollapseToEightCoupledPairs()
        {
            var pairs = new HashSet<string>();
            for (int seed = 0; seed < 64; seed++)
            {
                var s = Stage(seed); Assert.True(Apply(s.zone, s.manager, 100));
                pairs.Add(s.boss.GetDisplayName());
            }
            Assert.GreaterOrEqual(pairs.Count, 16, "domain choices must mix source bits before small-array modulo");
        }
        GameSessionState SaveLoad(OverworldZoneManager manager, Zone zone, Entity player)
        {
            var old = TurnManager.Active;
            try
            {
                var turns = new TurnManager();
                turns.RestoreSavedState(71, true, player, new List<TurnManager.SavedTurnEntry> {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 1000 } });
                manager.SetActiveZone(zone);
                var state = GameSessionState.Capture("legendary-test", "v", manager, turns, player);
                using (var bytes = new MemoryStream())
                { state.Save(new SaveWriter(bytes)); bytes.Position = 0; return GameSessionState.Load(new SaveReader(bytes, scope.Factory)); }
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, old); }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualSavedGraphKeepsLegendaryRewardThroughInjuryOrDeath(bool kill)
        {
            var m = OverworldZoneManager.CreateDetached(scope.Factory, 1);
            var z = m.GetZone("Overworld.18.2.1"); Assert.NotNull(z);
            var boss = z.GetReadOnlyEntities().Single(e => e.BlueprintName == "MarlbackWallkeeper");
            Assert.NotNull(Identity(boss), "actual unforced source at seed1");
            string bossID = boss.ID, name = boss.GetDisplayName(), rewardID = Field(Identity(boss), "RewardID");
            var item = Gear(boss).Single(e => e.ID == rewardID);
            string itemName = item.GetDisplayName(); int av = item.GetPart<ArmorPart>()?.AV ?? 0;
            var player = scope.Factory.CreateEntity("Player"); Assert.True(z.AddEntity(player, 36, 12));
            boss.GetStat("Hitpoints").BaseValue = 7;
            var ability = boss.GetPart<ActivatedAbilitiesPart>().AbilityList.First(); ability.CooldownRemaining = 9;
            if (kill)
            {
                CombatSystem.HandleDeath(boss, null, z); Assert.True(CombatSystem.IsDeathHandled(boss));
                Assert.NotNull(z.GetEntityCell(item), "real death spill keeps the enhanced entity");
                var at = z.GetEntityCell(item); Assert.True(z.MoveEntity(player, at.X, at.Y));
                Assert.True(InventorySystem.Pickup(player, item, z));
            }
            var loaded = SaveLoad(m, z, player); loaded.ZoneManager.UnloadZone(z.ZoneID);
            var savedZone = loaded.ZoneManager.GetZone(z.ZoneID);
            if (kill)
            {
                Assert.False(savedZone.GetReadOnlyEntities().Any(e => e.ID == bossID));
                var saved = DensityLootTestScope.Gear(loaded.Player).Single(e => e.ID == rewardID);
                Assert.AreEqual(itemName, saved.GetDisplayName()); Assert.AreEqual(1, ItemEnhancing.CountEnhancements(saved));
                Assert.False(Apply(savedZone, loaded.ZoneManager, 100));
            }
            else
            {
                var saved = savedZone.GetReadOnlyEntities().Single(e => e.ID == bossID);
                Assert.AreEqual(name, saved.GetDisplayName()); Assert.AreEqual(7, saved.GetStatValue("Hitpoints"));
                Assert.AreEqual(rewardID, Field(Identity(saved), "RewardID"));
                var reward = Gear(saved).Single(e => e.ID == rewardID); Assert.AreEqual(av, reward.GetPart<ArmorPart>()?.AV ?? 0);
                Assert.AreEqual(9, saved.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(a => a.ID == ability.ID).CooldownRemaining);
                Assert.AreEqual(1, ItemEnhancing.CountEnhancements(reward));
                Assert.False(Apply(savedZone, loaded.ZoneManager, 100));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void DiagnosticsAndInspectionDoNotConsumeGameplayRng(bool enabled)
        {
            var s = Stage(); string cause = Guid.NewGuid().ToString("N");
            bool before = Diag.IsChannelEnabled("worldgen"); var rng = new Random(937); LoadoutPart.Rng = rng;
            try
            {
                Diag.SetChannel("worldgen", enabled);
                using (Diag.WithCause(cause)) Assert.True(Apply(s.zone, s.manager, 100));
                Assert.AreEqual(new Random(937).Next(), rng.Next());
                var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "LegendaryLairRoll", CauseTraceId = cause, Limit = 20 }).Records;
                Assert.AreEqual(enabled ? 1 : 0, records.Count);
                int serial = MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
                string a = s.boss.GetPart<ExaminablePart>().BuildExamineLine();
                string b = s.boss.GetPart<ExaminablePart>().BuildExamineLine();
                Assert.AreEqual(a, b); Assert.AreEqual(serial, MessageLog.GetRecentEntries(1).LastOrDefault().Serial);
            }
            finally { Diag.SetChannel("worldgen", before); }
        }
        sealed class FixedRoll : Random
        {
            readonly int value;
            public FixedRoll(int value) { this.value = value; }
            public override int Next(int maxValue) => Math.Min(value, maxValue - 1);
        }
        [Test] public void RealSerratedRewardUsesItsExistingTenPercentOnHitRule()
        {
            for (int seed = 1; seed <= 16; seed++)
            {
                var s = Stage(seed); Assert.True(Apply(s.zone, s.manager, 100));
                var item = Gear(s.boss).Single(e => e.ID == Field(Identity(s.boss), "RewardID"));
                var serrated = item.GetPart<EnhancementSerrated>(); if (serrated == null) continue;
                Assert.AreEqual(1, serrated.Tier); Assert.AreEqual(10, serrated.ChancePercent);
                var yes = scope.Factory.CreateEntity("Player"); var no = scope.Factory.CreateEntity("Player");
                ItemEnhancementDispatch.DispatchOnHit(item, no, s.boss, new Damage(1), 1, s.zone, new FixedRoll(99));
                ItemEnhancementDispatch.DispatchOnHit(item, yes, s.boss, new Damage(1), 1, s.zone, new FixedRoll(0));
                Assert.False(no.HasEffect<BleedingEffect>()); Assert.True(yes.HasEffect<BleedingEffect>());
                return;
            }
            Assert.Fail("both-template cohort must contain a real serrated reward");
        }
        [Test] public void RealLacqueredRewardAppliesOneArmorAndUnequipsExactlyOnce()
        {
            for (int seed = 1; seed <= 16; seed++)
            {
                var s = Stage(seed); var armor = Gear(s.boss).Single(e => e.BlueprintName == "LeatherArmor");
                int before = armor.GetPart<ArmorPart>().AV, actorAV = CombatSystem.GetAV(s.boss);
                Assert.True(Apply(s.zone, s.manager, 100)); var lacquer = armor.GetPart<EnhancementLacquered>();
                if (lacquer == null) continue;
                Assert.AreEqual(1, lacquer.AvBonus); Assert.True(lacquer.AppliedBonus);
                Assert.AreEqual(before + 1, armor.GetPart<ArmorPart>().AV); Assert.AreEqual(actorAV + 1, CombatSystem.GetAV(s.boss));
                Assert.True(InventorySystem.UnequipItem(s.boss, armor)); Assert.AreEqual(before, armor.GetPart<ArmorPart>().AV);
                Assert.False(lacquer.AppliedBonus);
                Assert.True(InventorySystem.Equip(s.boss, armor)); Assert.AreEqual(before + 1, armor.GetPart<ArmorPart>().AV);
                return;
            }
            Assert.Fail("both-template cohort must contain real lacquered armor");
        }
    }
}
